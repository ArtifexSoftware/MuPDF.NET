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

        public delegate string ContentFunction(List<Position> positions);
        public delegate (Rect, Rect, IdentityMatrix) RectFunction(int rectN, Rect filled); // Define the delegate signature according to actual use

        public MuPDFXml Body
        {
            get
            {
                MuPDFXml dom = GetDocument();
                return dom.GetBodyTag();
            }
        }

        public MuPDFStory(string html = "", string userCss = null, float em = 12, MuPDFArchive archive = null)
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

        public MuPDFXml GetDocument()
        {
            FzXml dom = _nativeStory.fz_story_document();
            return new MuPDFXml(dom);
        }

        /// <summary>
        /// Write the content part prepared by Story.place() to the page.
        /// </summary>
        /// <param name="device">the Device created by dev = writer.begin_page(mediabox). The device knows how to call all MuPDF functions needed to write the content.</param>
        /// <param name="matrix">a matrix for transforming content when writing to the page. An example may be writing rotated text. The default means no transformation (i.e. the Identity matrix).</param>
        public void Draw(FzDevice device, Matrix matrix = null)
        {
            FzMatrix ctm2 = matrix.ToFzMatrix();
            if (ctm2 == null)
                ctm2 = new FzMatrix();
            FzDevice dev = device == null ? new FzDevice() : device;
            _nativeStory.fz_draw_story(device, ctm2);
        }

        /// <summary>
        /// Rewind the story’s document to the beginning for starting over its output.
        /// </summary>
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

        /// <summary>
        /// Finds optimal rect that contains the story
        /// </summary>
        /// <param name="fn"></param>
        /// <param name="rect"></param>
        /// <param name="pmin">Minimum parameter to consider</param>
        /// <param name="pmax">Maximum parameter to consider</param>
        /// <param name="delta">Maximum error in returned parameter.</param>
        /// <param name="verbose">If true we output diagnostics.</param>
        /// <returns></returns>
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

        public static MuPDFDocument AddPdfLinks(MemoryStream stream, List<Position> positions)
        {
            MuPDFDocument document = new MuPDFDocument("pdf", stream.ToArray());
            Dictionary<string, Position> id2Position = new Dictionary<string, Position>();
            foreach (Position position in positions)
            {
                if ((position.OpenClose & true) && position.Id != null)
                {
                    if (id2Position.Keys.Contains(position.Id))
                    {
                        // pass
                    }
                    else
                        id2Position.Add(position.Id, position);
                }
            }

            foreach (Position positionFrom in positions)
            {
                if ((positionFrom.OpenClose & true) && positionFrom.Href != null)
                {
                    LinkStruct link = new LinkStruct();
                    link.From = new Rect(positionFrom.Rect);
                    Position positionTo;
                    if (positionFrom.Href.StartsWith("#"))
                    {
                        string targetId = positionFrom.Href.Substring(1);
                        try
                        {
                            positionTo = id2Position.GetValueOrDefault(targetId);
                        }
                        catch (Exception e)
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
                    document[positionFrom.PageNum - 1].InsertLink(link);
                }
            }

            return document;
        }

        /// <summary>
        /// Calculate that part of the story’s content, that will fit in the provided rectangle. The method maintains a pointer which part of the story’s content has already been written and upon the next invocation resumes from that pointer’s position.
        /// </summary>
        /// <param name="where">layout the current part of the content to fit into this rectangle. This must be a sub-rectangle of the page’s MediaBox.</param>
        /// <returns>a bool (int) more and a rectangle filled. If more == 0, all content of the story has been written, otherwise more is waiting to be written to subsequent rectangles / pages. Rectangle filled is the part of where that has actually been filled.</returns>
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

        public MuPDFDocument WriteWithLinks(RectFunction rectFn, Action<Position> positionfn, Action<int, Rect, MuPDFDeviceWrapper, bool> pageFn)
        {
            MemoryStream stream = new MemoryStream();
            MuPDFDocumentWriter writer = new MuPDFDocumentWriter(stream);
            List<Position> positions = new List<Position>();

            Action<Position> positionfn2 = position =>
            {
                positions.Add(position);
                positionfn(position);
            };

            Write(writer, rectFn, positionFn: positionfn2, pageFn);
            writer.Close();
            stream.Seek(0, SeekOrigin.Begin);
            return MuPDFStory.AddPdfLinks(stream, positions);
        }

        /// <summary>
        /// Places and draws Story to a DocumentWriter. Avoids the need for calling code to implement a loop that calls Story.place() and Story.draw() etc,
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="fit"></param>
        public void Write(MuPDFDocumentWriter writer, RectFunction rectFn, Action<Position> positionFn, Action<int, Rect, MuPDFDeviceWrapper, bool> pageFn) // issue
        {
            MuPDFDeviceWrapper dev = null;
            int pageNum = 0;
            int rectNum = 0;
            Rect filled = new Rect(0, 0, 0, 0);
            while (true)
            {
                (Rect mediabox, Rect rect, IdentityMatrix ctm) = rectFn(rectNum, filled);// issue
                rectNum += 1;
                if (mediabox != null)
                    pageNum += 1;
                (bool more, filled) = Place(rect);
                if (positionFn != null) // if (positionFn)
                {
                    Action<Position> positionFn2 = position =>
                    {
                        position.PageNum = pageNum;
                        positionFn(position);
                    };

                    ElementPositions(positionFn);
                }

                if (writer != null)
                {
                    if (mediabox != null)
                    {
                        if (dev != null)
                        {
                            if (pageFn != null)
                            {
                                pageFn(pageNum, mediabox, dev, true);
                            }
                            writer.EndPage();
                        }
                        dev = writer.BeginPage(mediabox);
                        if (pageFn != null)
                        {
                            pageFn(pageNum, mediabox, dev, false);
                        }
                    }
                    Draw(dev.ToFzDevice(), ctm);
                    if (!more)
                    {
                        if (pageFn != null)
                        {
                            pageFn(pageNum, mediabox, dev, true);
                        }
                        writer.EndPage();
                    }
                }
                else
                    Draw(null, ctm);

                if (!more)
                    break;
            }
        }

        public static MuPDFDocument WriteStabilizedWithLinks(
            ContentFunction contentfn,
            RectFunction rectfn,
            string userCss = null,
            int em = 12,
            Action<Position> positionfn = null,
            Action<int, Rect, MuPDFDeviceWrapper, bool> pagefn = null,
            MuPDFArchive archive = null,
            bool addHeaderIds = true
            )
        {
            MemoryStream stream = new MemoryStream();
            MuPDFDocumentWriter writer = new MuPDFDocumentWriter(stream);
            List<Position> positions = new List<Position>();

            Action<Position> positionfn2 = position =>
            {
                positions.Add(position);
                positionfn(position);
            };

            MuPDFStory.WriteStabilized(writer, contentfn, rectfn, userCss, em, positionfn2, pagefn, archive, addHeaderIds);
            writer.Close();
            stream.Seek(0, SeekOrigin.Begin);
            return MuPDFStory.AddPdfLinks(stream, positions);
        }

        public void ElementPositions(Action<Position> function, Dictionary<string, dynamic> args = null)
        {
            if (args == null)
            {
                args = new Dictionary<string, dynamic>();
            }

            Action<Position> function2 = position =>
            {
                Position position2 = new Position
                {
                    Depth = position.Depth,
                    Heading = position.Heading,
                    Id = position.Id,
                    Rect = position.Rect,
                    Text = position.Text,
                    OpenClose = position.OpenClose,
                    RectNum = position.RectNum,
                    Href = position.Href
                };
                if (args != null)
                {
                    foreach ((string k, var v) in args)
                    {
                        // position2
                    }
                }
                function(position2);
            };
            
        }

        public static void WriteStabilized(
            MuPDFDocumentWriter writer, // Assuming Writer is a defined class
            ContentFunction contentfn,
            RectFunction rectfn,
            string userCss = null,
            int em = 12,
            Action<Position> positionfn = null,
            Action<int, Rect, MuPDFDeviceWrapper, bool> pageFn = null,
            MuPDFArchive archive = null, // Assuming Archive is a defined class
            bool addHeaderIds = true
            )
        {
            List<Position> positions = new List<Position>();
            string content = null;

            while (true)
            {
                string contentPrev = content;
                content = contentfn(positions);
                bool stable = false;
                if (content == contentPrev)
                {
                    stable = true;
                }
                string content2 = content;
                MuPDFStory story = new MuPDFStory(content2, userCss, em, archive); // Assuming Story is a defined class
                if (addHeaderIds)
                {
                    story.AddHeaderIds(); // Assuming AddHeaderIds is a method of Story
                }
                positions.Clear();
                void Positionfn2(Position position)
                {
                    positions.Add(position);
                    if (stable && positionfn != null)
                    {
                        positionfn(position);
                    }
                }
                story.Write(
                    stable ? writer : null,
                    rectfn,
                    Positionfn2,
                    pageFn
                );
                if (stable)
                {
                    break;
                }
            }
        }
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
