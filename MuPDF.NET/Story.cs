using System;
using System.Collections.Generic;
using System.Text;

namespace MuPDF.NET
{
    public class StoryElementPositionInfo
    {
        public int depth { get; set; }
        public int heading { get; set; }
        public string id { get; set; }
        public Rect rect { get; set; }
        public string text { get; set; }
        public int open_close { get; set; }
        public int rect_num { get; set; }
        public string href { get; set; }
        public int page_num { get; set; }
    }

    /// <summary>
    /// Represents a Story for rich-content layout driven by HTML and CSS.
    /// </summary>
    public class Story : IDisposable
    {
        private mupdf.FzStory _nativeStory;
        private bool _disposed;

        internal mupdf.FzStory NativeStory
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Story));
                return _nativeStory;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Story"/> class.
        /// </summary>
        public Story(string html = "", string userCss = "", float em = 12, Archive archive = null)
        {
            var buf = Helpers.BufferFromBytes(Encoding.UTF8.GetBytes(html ?? ""));
            _nativeStory = new mupdf.FzStory(buf, userCss ?? "", em,
                archive?.NativeArchive ?? new mupdf.FzArchive());
        }

        internal Story(mupdf.FzStory story)
        {
            _nativeStory = story;
        }

        public delegate (Rect mediabox, Rect rect, Matrix ctm) StoryRectFn(int rectNum, Rect filled);

        /// <summary>
        /// Compute layout into a given rectangle.
        ///
        /// Returns (more, filledRect) where more indicates unplaced content remains.
        /// </summary>
        public (bool more, Rect filledRect) Place(Rect where)
        {
            var fzRect = where.ToFzRect();
            var filled = new mupdf.FzRect();
            int more = NativeStory.fz_place_story(fzRect, filled);
            return (more != 0, new Rect(filled.x0, filled.y0, filled.x1, filled.y1));
        }

        /// <summary>
        /// Wrapper for fz_place_story_flags().
        /// </summary>
        public (bool more, Rect filledRect) Place(Rect where, int flags)
        {
            var fzRect = where.ToFzRect();
            var filled = new mupdf.FzRect();
            int more = NativeStory.fz_place_story_flags(fzRect, filled, flags);
            return (more != 0, new Rect(filled.x0, filled.y0, filled.x1, filled.y1));
        }

        /// <summary>
        /// Draw the placed content to a device.
        /// </summary>
        public void Draw(mupdf.FzDevice dev, Matrix ctm = null)
        {
            NativeStory.fz_draw_story(dev, (ctm ?? Matrix.Identity).ToFzMatrix());
        }

        /// <summary>
        /// Reset the story to allow re-placement.
        /// </summary>
        public void Reset()
        {
            NativeStory.fz_reset_story();
        }

        /// <summary>
        /// Get the story's DOM body text.
        /// </summary>
        public string Body
        {
            get
            {
                try
                {
                    var dom = NativeStory.fz_story_document();
                    if (dom.m_internal == null) return "";
                    var body = dom.fz_dom_body();
                    if (body.m_internal == null) return "";
                    return body.fz_xml_text() ?? "";
                }
                catch
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Set an attribute on an element found by id.
        /// </summary>
        public void ElementSetAttribute(string id, string att, string value)
        {
            var dom = NativeStory.fz_story_document();
            var body = dom.fz_dom_body();
            var el = body.fz_dom_find(null, "id", id);
            if (el.m_internal != null)
                el.fz_dom_add_attribute(att, value);
        }

        /// <summary>
        /// Look for h1..h6 items and add unique id attributes if absent.
        /// </summary>
        public void AddHeaderIds()
        {
            var dom = NativeStory.fz_story_document();
            var body = dom.fz_dom_body();
            int i = 0;
            var x = body.fz_dom_find(null, null, null);
            while (x != null && x.m_internal != null)
            {
                var name = x.fz_xml_tag();
                if (!string.IsNullOrEmpty(name) && name.Length == 2 && name[0] == 'h' && "123456".IndexOf(name[1]) >= 0)
                {
                    var attr = x.fz_dom_attribute("id");
                    if (string.IsNullOrEmpty(attr))
                    {
                        string id = $"h_id_{i}";
                        x.fz_dom_add_attribute("id", id);
                        i++;
                    }
                }
                x = x.fz_dom_find_next(null, null, null);
            }
        }

        /// <summary>
        /// Trigger a callback function to record where items have been placed.
        /// </summary>
        public void ElementPositions(Action<StoryElementPositionInfo> function, Dictionary<string, object> args = null)
        {
            if (function == null)
                throw new ArgumentException("callback 'function' must be a callable with exactly one argument");

            var cb = new StoryPositionCollector(function, args);
            mupdf.mupdf.ll_fz_story_positions_director(NativeStory.m_internal, cb);
        }

        /// <summary>
        /// Python-compatible story writing loop.
        /// </summary>
        public void Write(DocumentWriter writer, StoryRectFn rectfn, Action<StoryElementPositionInfo> positionfn = null, Action<int, Rect, mupdf.FzDevice, int> pagefn = null)
        {
            mupdf.FzDevice dev = null;
            int page_num = 0;
            int rect_num = 0;
            var filled = new Rect(0, 0, 0, 0);

            while (true)
            {
                var (mediabox, rect, ctm) = rectfn(rect_num, filled);
                rect_num += 1;
                bool newPage = mediabox != null && !mediabox.IsEmpty;
                if (newPage)
                    page_num += 1;

                var placed = Place(rect);
                bool more = placed.more;
                filled = placed.filledRect;

                if (positionfn != null)
                {
                    ElementPositions(position =>
                    {
                        position.page_num = page_num;
                        positionfn(position);
                    });
                }

                if (writer != null)
                {
                    if (newPage)
                    {
                        if (dev != null)
                        {
                            pagefn?.Invoke(page_num, mediabox, dev, 1);
                            writer.EndPage();
                        }
                        dev = writer.BeginPage(mediabox);
                        pagefn?.Invoke(page_num, mediabox, dev, 0);
                    }
                    if (dev != null)
                        Draw(dev, ctm);
                    if (!more)
                    {
                        pagefn?.Invoke(page_num, mediabox, dev, 1);
                        writer.EndPage();
                    }
                }
                else
                {
                    if (dev != null)
                        Draw(dev, ctm);
                }
                if (!more)
                    break;
            }
        }

        /// <summary>
        /// Write stabilized multi-page output. Iterates until the content function
        /// returns the same HTML, then produces the final document.
        /// </summary>
        public static byte[] WriteStabilized(Func<int, string> contentfn, string userCss = "", float em = 12,
            string mediabox = "letter", Archive archive = null, Action<Story> addHeaderCb = null,
            Action<Story> addFooterCb = null)
        {
            var paperRect = Helpers.PaperRect(mediabox);
            byte[] result = null;
            string previousHtml = null;

            for (int iteration = 0; ; iteration++)
            {
                string html = contentfn(iteration);
                bool stable = (html == previousHtml);
                previousHtml = html;

                var story = new Story(html, userCss, em, archive);
                var writer = stable ? new DocumentWriter(paperRect) : null;

                bool more = true;
                int pageNum = 0;
                while (more)
                {
                    var contentRect = new Rect(paperRect.X0 + 36, paperRect.Y0 + 36,
                                               paperRect.X1 - 36, paperRect.Y1 - 36);
                    (more, _) = story.Place(contentRect);

                    if (writer != null)
                    {
                        var dev = writer.BeginPage(paperRect);
                        story.Draw(dev);
                        addHeaderCb?.Invoke(story);
                        addFooterCb?.Invoke(story);
                        writer.EndPage();
                    }
                    pageNum++;
                }

                if (writer != null)
                    result = writer.Close();

                story.Dispose();

                if (stable)
                    break;
            }

            return result ?? Array.Empty<byte>();
        }

        /// <summary>
        /// Write multi-page output with links preserved.
        /// </summary>
        public static byte[] WriteWithLinks(Func<int, string> contentfn, string userCss = "", float em = 12,
            string mediabox = "letter")
        {
            return WriteStabilized(contentfn, userCss, em, mediabox);
        }

        /// <summary>
        /// Adds links to a PDF document from collected story element positions.
        /// </summary>
        public static Document AddPdfLinks(Document document, List<StoryElementPositionInfo> positions)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (positions == null) return document;

            var id_to_position = new Dictionary<string, StoryElementPositionInfo>();
            foreach (var position in positions)
            {
                if ((position.open_close & 1) != 0 && !string.IsNullOrEmpty(position.id) && !id_to_position.ContainsKey(position.id))
                    id_to_position[position.id] = position;
            }

            foreach (var position_from in positions)
            {
                if ((position_from.open_close & 1) == 0 || string.IsNullOrEmpty(position_from.href))
                    continue;

                var link = new Dictionary<string, object>
                {
                    ["from"] = new Rect(position_from.rect),
                };

                if (position_from.href.StartsWith("#", StringComparison.Ordinal))
                {
                    string target_id = position_from.href.Substring(1);
                    if (!id_to_position.TryGetValue(target_id, out var position_to))
                        throw new InvalidOperationException($"No destination with id={target_id}, required by position_from");
                    link["kind"] = Constants.LINK_GOTO;
                    link["to"] = new Point(position_to.rect.X0, position_to.rect.Y0);
                    link["page"] = position_to.page_num - 1;
                }
                else
                {
                    if (position_from.href.StartsWith("name:", StringComparison.Ordinal))
                    {
                        link["kind"] = Constants.LINK_NAMED;
                        link["name"] = position_from.href.Substring(5);
                    }
                    else
                    {
                        link["kind"] = Constants.LINK_URI;
                        link["uri"] = position_from.href;
                    }
                }

                document[position_from.page_num - 1].InsertLinkVoid(link);
            }

            return document;
        }

        /// <summary>
        /// Static stabilized writer loop matching Python's write_stabilized.
        /// </summary>
        public static void WriteStabilized(
            DocumentWriter writer,
            Func<List<StoryElementPositionInfo>, string> contentfn,
            StoryRectFn rectfn,
            string user_css = null,
            float em = 12,
            Action<StoryElementPositionInfo> positionfn = null,
            Action<int, Rect, mupdf.FzDevice, int> pagefn = null,
            Archive archive = null,
            bool add_header_ids = true)
        {
            var positions = new List<StoryElementPositionInfo>();
            string content = null;
            while (true)
            {
                string content_prev = content;
                content = contentfn(positions);
                bool stable = content == content_prev;

                var story = new Story(content, user_css, em, archive);
                if (add_header_ids)
                    story.AddHeaderIds();

                positions = new List<StoryElementPositionInfo>();
                story.Write(
                    stable ? writer : null,
                    rectfn,
                    position =>
                    {
                        positions.Add(position);
                        if (stable && positionfn != null)
                            positionfn(position);
                    },
                    pagefn
                );
                story.Dispose();

                if (stable)
                    break;
            }
        }

        /// <summary>
        /// Static stabilized writer + link insertion matching Python write_stabilized_with_links.
        /// </summary>
        public static Document WriteStabilizedWithLinks(
            Func<List<StoryElementPositionInfo>, string> contentfn,
            StoryRectFn rectfn,
            string user_css = null,
            float em = 12,
            Action<StoryElementPositionInfo> positionfn = null,
            Action<int, Rect, mupdf.FzDevice, int> pagefn = null,
            Archive archive = null,
            bool add_header_ids = true)
        {
            var writer = new DocumentWriter(new Rect(0, 0, 595, 842));
            var positions = new List<StoryElementPositionInfo>();
            WriteStabilized(
                writer,
                contentfn,
                rectfn,
                user_css,
                em,
                position =>
                {
                    positions.Add(position);
                    positionfn?.Invoke(position);
                },
                pagefn,
                archive,
                add_header_ids
            );
            byte[] pdf = writer.Close();
            writer.Dispose();

            var document = new Document(pdf, "pdf");
            return AddPdfLinks(document, positions);
        }

        /// <summary>
        /// Instance writer + link insertion matching Python write_with_links.
        /// </summary>
        public Document WriteWithLinks(StoryRectFn rectfn, Action<StoryElementPositionInfo> positionfn = null, Action<int, Rect, mupdf.FzDevice, int> pagefn = null)
        {
            var writer = new DocumentWriter(new Rect(0, 0, 595, 842));
            var positions = new List<StoryElementPositionInfo>();
            Write(
                writer,
                rectfn,
                position =>
                {
                    positions.Add(position);
                    positionfn?.Invoke(position);
                },
                pagefn
            );
            byte[] pdf = writer.Close();
            writer.Dispose();

            var document = new Document(pdf, "pdf");
            return AddPdfLinks(document, positions);
        }

        /// <summary>
        /// The result from a Story fit* method.
        /// </summary>
        public class FitResult
        {
            public bool? big_enough { get; set; }
            public Rect filled { get; set; }
            public bool? more { get; set; }
            public int? numcalls { get; set; }
            public double? parameter { get; set; }
            public Rect rect { get; set; }

            public override string ToString()
            {
                return $" big_enough={big_enough} filled={filled} more={more} numcalls={numcalls} parameter={parameter} rect={rect}";
            }
        }

        /// <summary>
        /// Finds optimal rect that contains this story.
        /// </summary>
        public FitResult Fit(Func<double, Rect> fn, double? pmin = null, double? pmax = null, double delta = 0.001, bool verbose = false, int flags = 0)
        {
            if (fn == null) throw new ArgumentNullException(nameof(fn));

            double? statePmin = pmin;
            double? statePmax = pmax;
            FitResult statePminResult = null;
            FitResult statePmaxResult = null;
            int numcalls = 0;
            double? lastParameter = null;

            void Log(string text)
            {
                if (verbose) System.Diagnostics.Debug.WriteLine($"fit(): {text}");
            }

            Reset();

            FitResult Ret()
            {
                if (statePmax.HasValue)
                {
                    if (!lastParameter.HasValue || lastParameter.Value != statePmax.Value)
                    {
                        Log("Calling update() with pmax, because was overwritten by later calls.");
                        bool bigEnough = Update(statePmax.Value);
                        if (!bigEnough)
                            throw new InvalidOperationException("fit(): internal state error: expected pmax to be big enough.");
                    }
                    return statePmaxResult;
                }
                return statePminResult ?? new FitResult { numcalls = numcalls };
            }

            bool Update(double parameter)
            {
                var rect = fn(parameter);
                if (rect == null) throw new InvalidOperationException("fit(): fn(parameter) returned null.");

                bool bigEnough;
                FitResult result;

                if (rect.IsEmpty)
                {
                    bigEnough = false;
                    result = new FitResult
                    {
                        parameter = parameter,
                        numcalls = numcalls
                    };
                    Log("update(): not calling self.place() because rect is empty.");
                }
                else
                {
                    var placed = Place(rect, flags);
                    numcalls += 1;
                    bigEnough = !placed.more;
                    result = new FitResult
                    {
                        filled = placed.filledRect,
                        more = placed.more,
                        numcalls = numcalls,
                        parameter = parameter,
                        rect = rect,
                        big_enough = bigEnough
                    };
                    Log($"update(): called self.place(): {numcalls}: more={placed.more} parameter={parameter} rect={rect}.");
                }

                if (bigEnough)
                {
                    statePmax = parameter;
                    statePmaxResult = result;
                }
                else
                {
                    statePmin = parameter;
                    statePminResult = result;
                }

                lastParameter = parameter;
                return bigEnough;
            }

            double Opposite(double? p, int direction)
            {
                if (!p.HasValue || p.Value == 0)
                    return direction;
                if (direction * p.Value > 0)
                    return 2 * p.Value;
                return -p.Value;
            }

            if (!statePmin.HasValue)
            {
                Log("finding pmin.");
                double parameter = Opposite(statePmax, -1);
                while (true)
                {
                    if (!Update(parameter))
                        break;
                    parameter *= 2;
                }
            }
            else
            {
                if (Update(statePmin.Value))
                {
                    Log($"statePmin={statePmin} is big enough.");
                    return Ret();
                }
            }

            if (!statePmax.HasValue)
            {
                Log("finding pmax.");
                double parameter = Opposite(statePmin, +1);
                while (true)
                {
                    if (Update(parameter))
                        break;
                    parameter *= 2;
                }
            }
            else
            {
                if (!Update(statePmax.Value))
                {
                    statePmax = null;
                    Log("No solution possible statePmax=null.");
                    return Ret();
                }
            }

            Log($"doing binary search with statePmin={statePmin} statePmax={statePmax}.");
            while (true)
            {
                if (!statePmax.HasValue || !statePmin.HasValue || statePmax.Value - statePmin.Value < delta)
                    return Ret();
                double parameter = (statePmin.Value + statePmax.Value) / 2;
                Update(parameter);
            }
        }

        /// <summary>
        /// Finds smallest scale where scale*rect contains this story.
        /// </summary>
        public FitResult FitScale(Rect rect, double scale_min = 0, double? scale_max = null, double delta = 0.001, bool verbose = false, int flags = 0)
        {
            double x0 = rect.X0;
            double y0 = rect.Y0;
            double width = rect.Width;
            double height = rect.Height;
            Rect Fn(double scale) => new Rect(x0, y0, x0 + scale * width, y0 + scale * height);
            return Fit(Fn, scale_min, scale_max, delta, verbose, flags);
        }

        /// <summary>
        /// Finds smallest height where width x height contains this story.
        /// </summary>
        public FitResult FitHeight(double width, double height_min = 0, double? height_max = null, Point origin = null, double delta = 0.001, bool verbose = false)
        {
            origin ??= new Point(0, 0);
            double x0 = origin.X;
            double y0 = origin.Y;
            double x1 = x0 + width;
            Rect Fn(double height) => new Rect(x0, y0, x1, y0 + height);
            return Fit(Fn, height_min, height_max, delta, verbose, 0);
        }

        /// <summary>
        /// Finds smallest width where width x height contains this story.
        /// </summary>
        public FitResult FitWidth(double height, double width_min = 0, double? width_max = null, Point origin = null, double delta = 0.001, bool verbose = false)
        {
            origin ??= new Point(0, 0);
            double x0 = origin.X;
            double y0 = origin.Y;
            double y1 = y0 + height;
            Rect Fn(double width) => new Rect(x0, y0, x0 + width, y1);
            return Fit(Fn, width_min, width_max, delta, verbose, 0);
        }

        // Python naming wrappers
        public void add_header_ids() => AddHeaderIds();
        public (bool more, Rect filledRect) place(Rect where, int flags = 0) => Place(where, flags);
        public void element_positions(Action<StoryElementPositionInfo> function, Dictionary<string, object> args = null) => ElementPositions(function, args);
        public void write(DocumentWriter writer, StoryRectFn rectfn, Action<StoryElementPositionInfo> positionfn = null, Action<int, Rect, mupdf.FzDevice, int> pagefn = null)
            => Write(writer, rectfn, positionfn, pagefn);
        public static Document add_pdf_links(Document document, List<StoryElementPositionInfo> positions) => AddPdfLinks(document, positions);
        public static void write_stabilized(DocumentWriter writer, Func<List<StoryElementPositionInfo>, string> contentfn, StoryRectFn rectfn, string user_css = null, float em = 12, Action<StoryElementPositionInfo> positionfn = null, Action<int, Rect, mupdf.FzDevice, int> pagefn = null, Archive archive = null, bool add_header_ids = true)
            => WriteStabilized(writer, contentfn, rectfn, user_css, em, positionfn, pagefn, archive, add_header_ids);
        public static Document write_stabilized_with_links(Func<List<StoryElementPositionInfo>, string> contentfn, StoryRectFn rectfn, string user_css = null, float em = 12, Action<StoryElementPositionInfo> positionfn = null, Action<int, Rect, mupdf.FzDevice, int> pagefn = null, Archive archive = null, bool add_header_ids = true)
            => WriteStabilizedWithLinks(contentfn, rectfn, user_css, em, positionfn, pagefn, archive, add_header_ids);
        public Document write_with_links(StoryRectFn rectfn, Action<StoryElementPositionInfo> positionfn = null, Action<int, Rect, mupdf.FzDevice, int> pagefn = null)
            => WriteWithLinks(rectfn, positionfn, pagefn);
        public FitResult fit(Func<double, Rect> fn, double? pmin = null, double? pmax = null, double delta = 0.001, bool verbose = false, int flags = 0)
            => Fit(fn, pmin, pmax, delta, verbose, flags);
        public FitResult fit_scale(Rect rect, double scale_min = 0, double? scale_max = null, double delta = 0.001, bool verbose = false, int flags = 0)
            => FitScale(rect, scale_min, scale_max, delta, verbose, flags);
        public FitResult fit_height(double width, double height_min = 0, double? height_max = null, Point origin = null, double delta = 0.001, bool verbose = false)
            => FitHeight(width, height_min, height_max, origin, delta, verbose);
        public FitResult fit_width(double height, double width_min = 0, double? width_max = null, Point origin = null, double delta = 0.001, bool verbose = false)
            => FitWidth(height, width_min, width_max, origin, delta, verbose);

        private sealed class StoryPositionCollector : mupdf.StoryPositionsCallback
        {
            private readonly Action<StoryElementPositionInfo> _callback;
            private readonly Dictionary<string, object> _args;

            internal StoryPositionCollector(Action<StoryElementPositionInfo> callback, Dictionary<string, object> args)
            {
                _callback = callback;
                _args = args;
            }

            public override void call(mupdf.fz_story_element_position position)
            {
                var p = new StoryElementPositionInfo
                {
                    depth = position.depth,
                    heading = position.heading,
                    id = position.id,
                    rect = new Rect(position.rect.x0, position.rect.y0, position.rect.x1, position.rect.y1),
                    text = position.text,
                    open_close = position.open_close,
                    rect_num = position.rectangle_num,
                    href = position.href,
                };
                if (_args != null)
                {
                    // Keep Python behavior "args injection" best-effort by copying known keys.
                    if (_args.TryGetValue("page_num", out var pageNumObj) && pageNumObj is int pg)
                        p.page_num = pg;
                }
                _callback(p);
            }
        }

        // ─── IDisposable ────────────────────────────────────────────────

        /// <summary>
        /// Releases all resources used by the <see cref="Story"/>.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _nativeStory?.Dispose();
                _nativeStory = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~Story() { Dispose(); }

        /// <summary>
        /// Returns a string that represents the current story.
        /// </summary>
        public override string ToString() => "Story()";
    }
}
