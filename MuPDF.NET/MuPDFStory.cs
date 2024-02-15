using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class MuPDFStory
    {
        private FzStory _nativeStory;

        public MuPDFXml Body
        {
            get
            {
                MuPDFXml dom = GetDocument();
                return dom.GetBodyTag();
            }
        }



        public MuPDFStory(string html = "", string userCss = null, int em = 12, MuPDFArchive archive = null)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(html);
            
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
            // Call unmanaged code
            SWIGTYPE_p_unsigned_char s = new SWIGTYPE_p_unsigned_char(unmanagedPointer, false);
            FzBuffer buf = mupdf.mupdf.fz_new_buffer_from_copied_data(s, (uint)bytes.Length);
            Marshal.FreeHGlobal(unmanagedPointer);

            FzArchive arch = archive != null ? archive.ToFzArchive() : new FzArchive();
            _nativeStory = new FzStory(buf, userCss, em, arch);
        }

        public void AddHeaderIds()
        {
            MuPDFXml dom = Body;
            int i = 0;
            MuPDFXml x = dom.Find(null, null, null);
            while (x != null)
            {
                string name = x.TagName;
                if (name.Length == 2 && name[0] == 'h' && "123456".Contains(name[1]))
                {
                    string attr = x.GetAttributeValue("id");
                    if (attr == null)
                    {
                        string id_ = $"h_id_{i}";
                        x.SetAttribute("id", id_);
                        i += 1;
                    }
                }
                x = x.FindNext(null, null, null);
            }
        }

        public static void AddHeaderIds(dynamic docOrStream, List<Position> positions)
        {
            MuPDFDocument doc;
            if (docOrStream is MuPDFDocument)
                doc = docOrStream;
            else
                doc = new MuPDFDocument("pdf", stream: docOrStream); // docOrStream is byte[]

            Dictionary<string, Position> idToPosition = new Dictionary<string, Position>();

            foreach (Position position in positions)
            {
                if (position.OpenClose & true && position.Id != null)
                {
                    if (idToPosition.ContainsKey(position.Id))
                    {
                        // pass
                    }
                    else
                        idToPosition.Add(position.Id, position);
                }
            }

            foreach (Position positionFrom in positions)
            {
                if ((positionFrom.OpenClose & true) && positionFrom.Href != null)
                {
                    LinkStruct link = new LinkStruct();
                    link.From = positionFrom.Rect;

                    string targetId = "";
                    Position positionTo = new Position();
                    if (positionFrom.Href.StartsWith("#"))
                    {
                        targetId = positionFrom.Href.Substring(1);
                        try
                        {
                            positionTo = idToPosition[targetId];
                        }
                        catch(Exception e)
                        {
                            throw new Exception($"No destination with id={targetId}, required by position_from: {positionFrom}");
                        }

                        link.Kind = LinkType.LINK_GOTO;
                        link.To = new Point(positionTo.Rect.X0, positionTo.Rect.Y0);
                        link.Page = positionTo.PageNum - 1;
                    }
                    else
                    {
                        if (positionFrom.Href.StartsWith("name:"))
                        {
                            link.Kind = LinkType.LINK_NAMED;
                            link.Name = positionFrom.Href.Substring(5);
                        }
                        else
                        {
                            link.Kind = LinkType.LINK_URI;
                            link.Uri = positionFrom.Href;
                        }
                    }

                    MuPDFPage page = new MuPDFPage(doc[positionFrom.PageNum - 1], doc);//issue
                    
                }
            }
        }

        public MuPDFXml GetDocument()
        {
            FzXml dom = _nativeStory.fz_story_document();
            return new MuPDFXml(dom);
        }

        public void Draw(FzDevice device, Matrix matrix = null)
        {
            FzMatrix ctm2 = matrix.ToFzMatrix();
            if (ctm2 == null)
                ctm2 = new FzMatrix();
            FzDevice dev = device == null ? new FzDevice() : device;
            _nativeStory.fz_draw_story(device, ctm2);
        }

        public void Reset()
        {
            _nativeStory.fz_reset_story();
        }

        public Rect ScaleFn(Rect rect, float scale)
        {
            return new Rect(rect.X0, rect.Y0, rect.X0 + scale * rect.Width, scale * rect.Height);
        }

        public FitResult FitScale(Rect rect, float scaleMin = 0, float scaleMax = 0, float delta = 0.001f, bool verbose = false)
        {
            return Fit(ScaleFn, rect, scaleMin, scaleMax, delta, verbose);
        }

        public FitResult Fit(Func<Rect, float, Rect> fn, Rect rect, float pmin, float pmax, float delta = 0.001f, bool verbose = false)
        {
            void Log(string text)
            {
                Console.WriteLine($"Fit(): {text}");
            }

            State state = new State(pmin, pmax, verbose);
            
            if (verbose)
                Log($"starting. {state.Pmin} {state.Pmax}.");

            Reset();

            FitResult Ret(Rect rect, State state)
            {
                bool bigEnough = false;
                FitResult result = null;
                if (state.Pmax != 0)
                {
                    if (state.LastP != state.Pmax)//issue
                    {
                        if (verbose)
                            Log($"Calling update() with pmax, because was overwritten by later calls.");
                        bigEnough = Update(rect, state.Pmax);
                    }
                    result = state.PmaxResult;
                }
                else
                {
                    result = state.PminResult != null ? state.PminResult : new FitResult(numcalls: state.Numcalls);
                }

                if (verbose)
                    Log($"finished. {state.Pmin0} {state.Pmax0} {state.Pmax}: returning {result}");
                return result;
            }

            bool Update(Rect rect, float parameter)
            {
                Rect r = fn(rect, parameter);
                bool bigEnough;
                FitResult result;
                if (r.IsEmpty)
                {
                    bigEnough = false;
                    result = new FitResult(parameter: parameter, numcalls: state.Numcalls);
                    if (verbose)
                        Log("update(): not calling self.place() because rect is empty.");
                }
                else
                {
                    (bool more, Rect filled) = Place(rect);
                    state.Numcalls += 1;
                    bigEnough = !more;

                    result = new FitResult(
                        filled: filled,
                        more: more,
                        numcalls: state.Numcalls,
                        parameter: parameter,
                        rect: rect,
                        bigEnough: bigEnough
                        );
                    if (verbose)
                        Log($"Update(): called self.place(): {state.Numcalls}: {more} {parameter} {rect}.");
                }

                if (bigEnough)
                {
                    state.Pmax = parameter;
                    state.PmaxResult = result;
                }
                else
                {
                    state.Pmin = parameter;
                    state.PminResult = result;
                }
                state.LastP = parameter;
                return bigEnough;
            }

            float Opposite(float p, int direction)
            {
                if (p == 0)
                    return direction;
                if (direction * p > 0)
                    return 2 * p;
                return -p;
            }

            if (state.Pmin == 0)
            {
                if (verbose) Log("finding Pmin.");
                float parameter = Opposite(state.Pmax, -1);
                while (true)
                {
                    if (!Update(rect, parameter))
                        break;
                    parameter *= 2;
                }
            }
            else
            {
                if (Update(rect, state.Pmin))
                {
                    if (verbose) Log($"{state.Pmin} is big enough.");
                    FitResult ret = Ret(rect, state);
                    return ret;
                }
            }

            if (state.Pmax == 0)
            {
                if (verbose) Log("Finding Pmax");
                float parameter = Opposite(state.Pmin, 1);
                while (true)
                {
                    if (Update(rect, parameter))
                        break;
                    parameter *= 2;
                }
            }
            else
            {
                if (!Update(rect, state.Pmax))
                {
                    state.Pmax = 0;
                    if (verbose) Log($"No solution possible {state.Pmax}.");
                    FitResult ret = Ret(rect, state);
                    return ret;
                }
            }

            /*if (verbose)
                Log($"doing binary search with {{state.pmin=}} {state.Pmax}.");
            while (true)
            {
                if (state.Pmax - state.Pmin < delta)
                    return Ret(rect, state);
                int parameter = (state.Pmin + state.Pmax) / 2;
                Update(rect, parameter);
            }*/
            return null;
        }

        public (bool, Rect) Place(Rect where)
        {
            FzRect filled = new FzRect();
            bool more = _nativeStory.fz_place_story(where.ToFzRect(), filled) != 0;
            return (more, new Rect(filled));
        }

        public Rect HeightFn(Rect rect, float height)
        {
            return new Rect(rect.X0, rect.Y0, rect.X1, rect.Y0 + height);
        }

        public void FitHeight(float width, float heightMin = 0, float heightMax = 0, Point origin = null, float delta = 0.001f, bool verbose = false)
        {
            if (origin != null)
                origin = new Point(0, 0);
            Rect rect = new Rect(origin.X, origin.Y, origin.X + width, 0);
            Fit(HeightFn, rect, heightMin, heightMax, delta, verbose);
        }

        public Rect WidthFn(Rect rect, float width)
        {
            return new Rect(rect.X0, rect.Y0, rect.X0 + width, rect.Y1);
        }

        public void FitWidth(float height, float widthMin = 0, float widthMax = 0, Point origin = null, float delta = 0.001f, bool verbose = false)
        {
            Rect rect = new Rect(origin.X, origin.Y, 0, origin.Y + height);
            Fit(WidthFn, rect, widthMin, widthMax, delta, verbose);
        }

        public void WriteWithLinks()
        {
            MemoryStream stream = new MemoryStream();
            MuPDFDocumentWriter writer = new MuPDFDocumentWriter(stream);
            List<Position> positions = new List<Position>();
        }

        /*public void Write(MuPDFDocumentWriter writer)
        {
            int pageNum = 0;
            int rectNum = 0;
            Rect filled = new Rect(0, 0, 0, 0);
            while (true)
            {
                (Rect mediabox, Rect rect, IdentityMatrix ctm) = Utils.RectFunction();
                rectNum += 1;
                if (mediabox != null)
                    pageNum += 1;
                (bool more, filled) = Place(rect);
                if (positionFn)
            }
        }*/
    }

    internal class State
    {
        public float Pmin;

        public float Pmax;

        public FitResult PminResult;

        public FitResult PmaxResult;

        public int Result;

        public int Numcalls;

        public float Pmin0;

        public float Pmax0;

        public float LastP;
        public State(float pmin, float pmax, bool verbose)
        {
            Pmin = pmin;
            Pmax = pmax;
            PminResult = null;
            PmaxResult = null;
            Result = 0;
            Numcalls = 0;
            if (verbose)
            {
                Pmin0 = pmin;
                Pmax0 = pmax;
            }
        }
    }

    public class FitResult
    {
        public bool BigEnough;

        public dynamic Filled;

        public bool More;

        public int NumCalls;

        public float Parameter;

        public Rect Rect;

        public FitResult(bool bigEnough = false, dynamic filled = null, bool more = false, int numcalls = 0, float parameter = 0, Rect rect = null)
        {
            BigEnough = bigEnough;
            Filled = filled;
            More = more;
            NumCalls = numcalls;
            Parameter = parameter;
            Rect = rect;
        }

        public override string ToString()
        {
            return $"BigEnough={BigEnough}, Filled={Filled}, More={More}, NumCalls={NumCalls}, Parameter={Parameter}, Rect={Rect}";
        }
    }
}
