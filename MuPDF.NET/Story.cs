using mupdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MuPDF.NET
{
    public class Story
    {
        static Story()
        {
            Utils.InitApp();
        }

        private FzStory _nativeStory;

        public delegate string ContentFunction(List<Position> positions);
        public delegate (Rect, Rect, Matrix) RectFunction(int rectN, Rect filled); // Define the delegate signature according to actual use

        /// <summary>
        /// the story's underlying Body
        /// </summary>
        public Xml Body
        {
            get
            {
                Xml dom = GetDocument();
                return dom.GetBodyTag();
            }
        }

        public Story(string html = "", string userCss = null, float em = 12, Archive archive = null)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(html);
            FzBuffer buf = Utils.fz_new_buffer_from_data(bytes);

            FzArchive arch = archive != null ? archive.ToFzArchive() : new FzArchive();
            _nativeStory = new FzStory(buf, userCss, em, arch);
        }

        /// <summary>
        /// Look for `<h1..6>` items in `self` and adds unique `id` attributes if not already present.
        /// </summary>
        public void AddHeaderIds()
        {
            Xml dom = Body;
            int i = 0;
            Xml x = dom.Find(null, null, null);
            while (x != null)
            {
                string name = x.TagName;
                if (name.Length == 2 && name[0] == 'h' && "123456".Contains(name[1].ToString()))
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

        /// <summary>
        /// Get Document object
        /// </summary>
        /// <returns></returns>
        public Xml GetDocument()
        {
            FzXml dom = _nativeStory.fz_story_document();
            return new Xml(dom);
        }

        /// <summary>
        /// Write the content part prepared by Story.place() to the page.
        /// </summary>
        /// <param name="device">the Device created by dev = writer.begin_page(mediabox). The device knows how to call all MuPDF functions needed to write the content.</param>
        /// <param name="matrix">a matrix for transforming content when writing to the page. An example may be writing rotated text. The default means no transformation (i.e. the Identity matrix).</param>
        public void Draw(DeviceWrapper device, Matrix matrix = null)
        {
            FzMatrix ctm2 = (matrix != null) ? matrix.ToFzMatrix() : new FzMatrix();
            if (ctm2 == null)
                ctm2 = new FzMatrix();
            FzDevice dev = device == null ? new FzDevice() : device.ToFzDevice();
            _nativeStory.fz_draw_story(dev, ctm2);
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

        /// <summary>
        /// Finds smallest value scale from scale_min to scale_max where scale * rect is large enough to contain the story
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="scaleMin">Minimum scale to consider; must be >= 0</param>
        /// <param name="scaleMax">Maximum scale to consider, must be >= scale_min or None for infinite</param>
        /// <param name="delta">Maximum error in returned scale</param>
        /// <param name="verbose">If true we output diagnostics</param>
        /// <returns></returns>
        public FitResult FitScale(Rect rect, float scaleMin = 0, float scaleMax = 0, float delta = 0.001f, bool verbose = false)
        {
            return Fit(ScaleFn, rect, scaleMin, scaleMax, delta, verbose);
        }

        /// <summary>
        /// Finds optimal rect that contains the story
        /// </summary>
        /// <param name="fn">A callable taking a floating point `parameter` and returning a `fitz.Rect()`.
        /// <br/> If the rect is empty, we assume the story will not fit and do not call `self.place()`.
        /// </param>
        /// <param name="rect"></param>
        /// <param name="pmin">Minimum parameter to consider</param>
        /// <param name="pmax">Maximum parameter to consider</param>
        /// <param name="delta">Maximum error in returned parameter.</param>
        /// <param name="verbose">If true we output diagnostics.</param>
        /// <returns>Returns a `Story.FitResult` instance.</returns>
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

            FitResult Ret(Rect _rect, State _state)
            {
                bool bigEnough = false;
                FitResult result = null;
                if (_state.Pmax != 0)
                {
                    if (_state.LastP != _state.Pmax)
                    {
                        if (verbose)
                            Log($"Calling update() with pmax, because was overwritten by later calls.");
                        bigEnough = Update(_rect, _state.Pmax);
                    }
                    result = _state.PmaxResult;
                }
                else
                {
                    result = _state.PminResult != null ? _state.PminResult : new FitResult(numcalls: _state.Numcalls);
                }

                if (verbose)
                    Log($"finished. {_state.Pmin0} {_state.Pmax0} {_state.Pmax}: returning {result}");
                return result;
            }

            bool Update(Rect _rect, float parameter)
            {
                Rect r = fn(_rect, parameter);
                bool bigEnough;
                FitResult result;
                if (r.IsEmpty)
                {
                    bigEnough = false;
                    result = new FitResult(parameter: parameter, numcalls: state.Numcalls);
                    if (verbose)
                        Log("update(): not calling self.place() because _rect is empty.");
                }
                else
                {
                    (bool more, Rect filled) = Place(_rect);
                    state.Numcalls += 1;
                    bigEnough = !more;

                    result = new FitResult(
                        filled: filled,
                        more: more,
                        numcalls: state.Numcalls,
                        parameter: parameter,
                        rect: _rect,
                        bigEnough: bigEnough
                        );
                    if (verbose)
                        Log($"Update(): called self.place(): {state.Numcalls}: {more} {parameter} {_rect}.");
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

            if (verbose)
                Log($"doing binary search with {state.Pmin} {state.Pmax}.");
            while (true)
            {
                if (state.Pmax - state.Pmin < delta)
                    return Ret(rect, state);
                float parameter = (state.Pmin + state.Pmax) / 2;
                Update(rect, parameter);
            }
        }

        /// <summary>
        /// Adds links to PDF document
        /// </summary>
        /// <param name="stream">A PDF Document or raw PDF content, for example an io.BytesIO instance</param>
        /// <param name="positions">List of Position's for stream</param>
        /// <returns>Document if a `Document` instance, otherwise a new `Document` instance</returns>
        /// <exception cref="Exception"></exception>
        public static Document AddPdfLinks(MemoryStream stream, List<Position> positions)
        {
            Document document = new Document("pdf", stream.ToArray());
            Dictionary<string, Position> id2Position = new Dictionary<string, Position>();
            foreach (Position position in positions)
            {
                if ((position.OpenClose & true) && position.Id != null)
                {
                    if (id2Position.ContainsKey(position.Id))
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
                    LinkInfo link = new LinkInfo();
                    link.From = new Rect(positionFrom.Rect);
                    Position positionTo;
                    if (positionFrom.Href.StartsWith("#"))
                    {
                        string targetId = positionFrom.Href.Substring(1);
                        if (!id2Position.TryGetValue(targetId, out positionTo))
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

        /// <summary>
        /// Finds smallest height in range `height_min..height_max` where a rect with size `(width, height)` is large enough to contain the story
        /// </summary>
        /// <param name="width">width of rect</param>
        /// <param name="heightMin">Minimum height to consider; must be >= 0.</param>
        /// <param name="heightMax">Maximum height to consider, must be >= height_min or `None` for infinite.</param>
        /// <param name="origin">point of rect.</param>
        /// <param name="delta">Maximum error in returned height.</param>
        /// <param name="verbose">If true we output diagnostics.</param>
        /// <returns>Returns a `Story.FitResult` instance.</returns>
        public FitResult FitHeight(float width, float heightMin = 0, float heightMax = 0, Point origin = null, float delta = 0.001f, bool verbose = false)
        {
            if (origin != null)
                origin = new Point(0, 0);
            Rect rect = new Rect(origin.X, origin.Y, origin.X + width, 0);
            return Fit(HeightFn, rect, heightMin, heightMax, delta, verbose);
        }

        public Rect WidthFn(Rect rect, float width)
        {
            return new Rect(rect.X0, rect.Y0, rect.X0 + width, rect.Y1);
        }

        public FitResult FitWidth(float height, float widthMin = 0, float widthMax = 0, Point origin = null, float delta = 0.001f, bool verbose = false)
        {
            Rect rect = new Rect(origin.X, origin.Y, 0, origin.Y + height);
            return Fit(WidthFn, rect, widthMin, widthMax, delta, verbose);
        }

        /// <summary>
        /// Similar to Write() except that we don’t have a writer arg and we return a PDF Document in which links have been created for each internal html link
        /// </summary>
        /// <param name="rectFn"></param>
        /// <param name="positionFn"></param>
        /// <param name="pageFn"></param>
        /// <returns>a Document</returns>
        public Document WriteWithLinks(RectFunction rectFn = null, Action<Position> positionFn = null, Action<int, Rect, DeviceWrapper, bool> pageFn = null)
        {
            MemoryStream stream = new MemoryStream(100);
            DocumentWriter writer = new DocumentWriter(stream);
            List<Position> positions = new List<Position>();

            Action<Position> positionFn2 = position =>
            {
                positions.Add(position);
                positionFn(position);
            };

            Write(writer, rectFn, positionFn: positionFn2, pageFn);
            writer.Close();
            stream.Seek(0, (SeekOrigin)1);
            return Story.AddPdfLinks(stream, positions);
        }

        /// <summary>
        /// Places and draws Story to a DocumentWriter. Avoids the need for calling code to implement a loop that calls Story.place() and Story.draw() etc,
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="rectFn"></param>
        /// <param name="positionFn"></param>
        /// <param name="pageFn"></param>
        public void Write(DocumentWriter writer, RectFunction rectFn, Action<Position> positionFn, Action<int, Rect, DeviceWrapper, bool> pageFn)
        {
            DeviceWrapper dev = null;
            int pageNum = 0;
            int rectNum = 0;
            Rect filled = new Rect(0, 0, 0, 0);
            while (true)
            {
                (Rect mediabox, Rect rect, Matrix ctm) = rectFn(rectNum, filled);
                rectNum += 1;
                if (mediabox != null)
                    pageNum += 1;
                (bool more, Rect _filled) = Place(rect);
                filled = _filled;
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
                    Draw(dev, ctm);
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

        /// <summary>
        /// Similar to WriteStabilized() except that we don’t have a writer arg and instead return a PDF Document in which links have been created for each internal html link
        /// </summary>
        /// <param name="contentfn"></param>
        /// <param name="rectfn"></param>
        /// <param name="userCss"></param>
        /// <param name="em"></param>
        /// <param name="positionFn"></param>
        /// <param name="pagefn"></param>
        /// <param name="archive"></param>
        /// <param name="addHeaderIds"></param>
        /// <returns></returns>
        public static Document WriteStabilizedWithLinks(
            ContentFunction contentfn,
            RectFunction rectfn,
            string userCss = null,
            int em = 12,
            Action<Position> positionFn = null,
            Action<int, Rect, DeviceWrapper, bool> pagefn = null,
            Archive archive = null,
            bool addHeaderIds = true
            )
        {
            MemoryStream stream = new MemoryStream();
            DocumentWriter writer = new DocumentWriter(stream);
            List<Position> positions = new List<Position>();

            Action<Position> positionFn2 = position =>
            {
                positions.Add(position);
                positionFn(position);
            };

            Story.WriteStabilized(writer, contentfn, rectfn, userCss, em, positionFn2, pagefn, archive, addHeaderIds);
            writer.Close();
            stream.Seek(0, (SeekOrigin)1);
            return Story.AddPdfLinks(stream, positions);
        }

        /// <summary>
        /// Trigger a callback function to record where items have been placed.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="args"></param>
        public void ElementPositions(Action<Position> function, Position arg = null)
        {
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
                if (arg != null)
                {
                    position2 = new Position(arg); // copy position
                }
                function(position2);
            };

        }

        /// <summary>
        /// Static method that does iterative layout of html content to a DocumentWriter
        /// </summary>
        /// <param name="writer">A DocumentWriter</param>
        /// <param name="contentfn">A function taking a list of ElementPositions and returning a string containing html</param>
        /// <param name="rectfn">A callable taking (RectNum: int, Filled: Rect) and returning (Mediabox, Rect, Ctm)</param>
        /// <param name="userCss"></param>
        /// <param name="em"></param>
        /// <param name="positionFn"></param>
        /// <param name="pageFn"> None, or a callable taking (page_num, medibox, dev, after)</param>
        /// <param name="archive"></param>
        /// <param name="addHeaderIds">If true, we add unique ids to all header tags that don't already have an id</param>
        public static void WriteStabilized(
            DocumentWriter writer, // Assuming Writer is a defined class
            ContentFunction contentfn,
            RectFunction rectfn,
            string userCss = null,
            int em = 12,
            Action<Position> positionFn = null,
            Action<int, Rect, DeviceWrapper, bool> pageFn = null,
            Archive archive = null, // Assuming Archive is a defined class
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
                Story story = new Story(content2, userCss, em, archive); // Assuming Story is a defined class
                if (addHeaderIds)
                {
                    story.AddHeaderIds(); // Assuming AddHeaderIds is a method of Story
                }
                positions.Clear();
                void PositionFn2(Position position)
                {
                    positions.Add(position);
                    if (stable && positionFn != null)
                    {
                        positionFn(position);
                    }
                }
                story.Write(
                    stable ? writer : null,
                    rectfn,
                    PositionFn2,
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
        public float Pmin { get; set; }

        public float Pmax { get; set; }

        public FitResult PminResult { get; set; }

        public FitResult PmaxResult { get; set; }

        public int Result { get; set; }

        public int Numcalls { get; set; }

        public float Pmin0 { get; set; }

        public float Pmax0 { get; set; }

        public float LastP { get; set; }
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
        /// <summary>
        /// True if the fit succeeded
        /// </summary>
        public bool BigEnough { get; set; } = false;

        /// <summary>
        /// From the last call to Story.place()
        /// </summary>
        public dynamic Filled { get; set; }

        /// <summary>
        /// False if the fit succeeded
        /// </summary>
        public bool More { get; set; }

        /// <summary>
        /// Number of calls made to self.place()
        /// </summary>
        public int NumCalls { get; set; }

        /// <summary>
        /// The successful parameter value, or the largest failing value
        /// </summary>
        public float Parameter { get; set; }

        /// <summary>
        /// The rect created from parameter
        /// </summary>
        public Rect Rect { get; set; }

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
