using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace MuPDF.NET
{
    /// <summary>
    /// Placement record for one HTML element after <see cref="Story.Place"/> .
    /// </summary>
    /// <remarks>
    /// Populated by <see cref="Story.ElementPositions"/> for headings (<c>h1</c>–<c>h6</c>),
    /// <c>id</c>, and <c>href</c> attributes. <see cref="Position"/> is the legacy callback DTO with the same fields.
    /// </remarks>
    public class StoryElementPositionInfo
    {
        /// <summary>Nesting depth in the box tree.</summary>
        public int Depth { get; set; }

        /// <summary>Header level 0 (none) or 1–6 for <c>h1</c>–<c>h6</c>.</summary>
        public int Heading { get; set; }

        /// <summary>Value of the HTML <c>id</c> attribute, if any.</summary>
        public string Id { get; set; }

        /// <summary>Bounding rectangle of the element on the page.</summary>
        public Rect Rect { get; set; }

        /// <summary>Immediate text content of the element.</summary>
        public string Text { get; set; }

        /// <summary>Bit 0: element opens; bit 1: element closes (structure tracking).</summary>
        public int OpenClose { get; set; }

        /// <summary>Count of rectangles filled by the story so far.</summary>
        public int RectNum { get; set; }

        /// <summary>Value of the HTML <c>href</c> attribute, if any.</summary>
        public string Href { get; set; }

        /// <summary>1-based page number (set during <see cref="Story.Write"/> and stabilized writers).</summary>
        public int PageNum { get; set; }

        internal int depth { get => Depth; set => Depth = value; }
        internal int heading { get => Heading; set => Heading = value; }
        internal string id { get => Id; set => Id = value; }
        internal Rect rect { get => Rect; set => Rect = value; }
        internal string text { get => Text; set => Text = value; }
        internal int open_close { get => OpenClose; set => OpenClose = value; }
        internal int rect_num { get => RectNum; set => RectNum = value; }
        internal string href { get => Href; set => Href = value; }
        internal int page_num { get => PageNum; set => PageNum = value; }
    }

    /// <summary>
    /// Lays out HTML/CSS into PDF pages using an internal DOM .
    /// </summary>
    /// <remarks>
    /// <para>Parse HTML (and optional CSS), optionally modify via <see cref="Xml"/> on <see cref="Body"/>,
    /// then place content with <see cref="Place"/> and render with <see cref="Draw"/>, or use
    /// <see cref="Write"/> / <see cref="WriteStabilized"/> for callback-driven pagination.</para>
    /// <para>Images and fonts resolve through an <see cref="Archive"/> or path-like archive argument.
    /// Multiple stories can target the same page (headers, footers, body, etc.).</para>
    /// </remarks>
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
        /// Pagination callback: returns mediabox (or null), next placement rect, and optional CTM.
        /// </summary>
        /// <param name="rectNum">Zero-based rectangle index on the current page.</param>
        /// <param name="filled">Area filled by the previous placement.</param>
        /// <returns><c>mediabox</c> non-null starts a new page; <c>rect</c> is where to place next content; <c>ctm</c> transforms drawing.</returns>
        public delegate (Rect mediabox, Rect rect, Matrix ctm) StoryRectFn(int rectNum, Rect filled);

        /// <summary>Legacy MuPDF.NET name for <see cref="StoryRectFn"/>.</summary>
        public delegate (Rect mediabox, Rect rect, Matrix ctm) RectFunction(int rectNum, Rect filled);

        /// <summary>
        /// Builds HTML for stabilized layout from collected element positions (legacy <c>contentfn</c>).
        /// </summary>
        public delegate string ContentFunction(List<StoryElementPositionInfo> positions);

        /// <summary>
        /// Creates a story from HTML and optional CSS; builds the internal DOM.
        /// </summary>
        /// <param name="html">HTML source or fragment; empty yields minimal document.</param>
        /// <param name="userCss">Optional user stylesheet (valid CSS).</param>
        /// <param name="em">Default font size in points.</param>
        /// <param name="archive"><see cref="Archive"/>, folder path, or other archive constructor argument.</param>
        public Story(string html = "", string userCss = null, float em = 12, object archive = null)
        {
            var buffer_ = Helpers.BufferFromBytes(Encoding.UTF8.GetBytes(html ?? ""));
            Archive arch = null;
            if (archive != null)
            {
                if (archive is Archive a)
                    arch = a;
                else
                    arch = new Archive(archive);
            }
            var archNative = arch?.NativeArchive ?? new mupdf.FzArchive();
            _nativeStory = new mupdf.FzStory(buffer_, userCss, em, archNative);
        }

        internal Story(mupdf.FzStory story)
        {
            _nativeStory = story;
        }

        /// <summary>
        /// Assigns unique <c>id</c> attributes to <c>h1</c>–<c>h6</c> elements that do not already have one.
        /// </summary>
        /// <remarks>Used by <see cref="WriteStabilized"/> when <c>add_header_ids</c> is true to support table-of-contents links.</remarks>
        public void AddHeaderIds()
        {
            // Look for `<h1..6>` items in `self` and adds unique `id`
            // attributes if not already present.
            var dom = Body;
            // i = 0
            int i = 0;
            // x = dom.find(None, None, None)
            var x = dom.Find(null, null, null);
            // while x:
            while (x != null)
            {
                // name = x.tagname
                string? name = x.TagName;
                if (name != null && name.Length == 2 && name[0] == 'h' && "123456".IndexOf(name[1]) >= 0)
                {
                    // attr = x.get_attribute_value("id")
                    var attr = x.GetAttributeValue("id");
                    if (string.IsNullOrEmpty(attr))
                    {
                        // id_ = f"h_id_{i}"
                        string id_ = $"h_id_{i}";
                        //log(f"{name=}: setting {id_=}")
                        // x.set_attribute("id", id_)
                        x.SetAttribute("id", id_);
                        // i += 1
                        i += 1;
                    }
                }
                // x = x.find_next(None, None, None)
                x = x.FindNext(null, null, null);
            }
        }

        /// <summary>
        /// Add links to a PDF from element positions .
        /// </summary>
        /// <param name="documentOrStream">A <see cref="Document"/>, PDF bytes, or stream.</param>
        /// <param name="positions">Positions from <see cref="ElementPositions"/>; duplicate ids are ignored for targets.</param>
        /// <returns>The input document, or a new document when <paramref name="documentOrStream"/> is bytes/stream.</returns>
        /// <exception cref="InvalidOperationException">Internal <c>#name</c> href with no matching id.</exception>
        public static Document AddPdfLinks(object documentOrStream, List<StoryElementPositionInfo> positions)
        {
            Document document;
            if (documentOrStream is Document doc)
                document = doc;
            else if (documentOrStream is byte[] bytes)
                document = new Document(bytes, "pdf");
            else if (documentOrStream is Stream stream)
                document = new Document(stream, "pdf");
            else
                throw new ArgumentException("document_or_stream must be Document, byte[], or Stream", nameof(documentOrStream));

            return AddPdfLinks(document, positions);
        }

        /// <inheritdoc cref="AddPdfLinks(object, List{StoryElementPositionInfo})"/>
        public static Document AddPdfLinks(Document document, List<StoryElementPositionInfo> positions)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (positions == null) return document;

            // id_to_position = dict()
            var id_to_position = new Dictionary<string, StoryElementPositionInfo>();
            foreach (var position in positions)
            {
                if ((position.OpenClose & 1) != 0 && !string.IsNullOrEmpty(position.Id))
                {
                    if (!id_to_position.ContainsKey(position.Id))
                        id_to_position[position.Id] = position;
                }
            }

            foreach (var position_from in positions)
            {
                if ((position_from.OpenClose & 1) == 0 || string.IsNullOrEmpty(position_from.Href))
                    continue;

                // link = dict()
                var link = new Dictionary<string, object>
                {
                    // link['from'] = Rect(position_from.rect)
                    ["from"] = new Rect(position_from.Rect),
                };
                if (position_from.Href.StartsWith("#", StringComparison.Ordinal))
                {
                    // target_id = position_from.href[1:]
                    string target_id = position_from.Href.Substring(1);
                    // position_to = id_to_position[ target_id]
                    if (!id_to_position.TryGetValue(target_id, out var position_to))
                        throw new InvalidOperationException($"No destination with id={target_id}, required by position_from: {position_from}");
                    // link["kind"] = LINK_GOTO
                    link["kind"] = Constants.LinkGoto;
                    // link["to"] = Point(x0, y0)
                    link["to"] = new Point(position_to.Rect.X0, position_to.Rect.Y0);
                    // link["page"] = position_to.page_num - 1
                    link["page"] = position_to.PageNum - 1;
                }
                else
                {
                    if (position_from.Href.StartsWith("name:", StringComparison.Ordinal))
                    {
                        link["kind"] = Constants.LinkNamed;
                        link["name"] = position_from.Href.Substring(5);
                    }
                    else
                    {
                        link["kind"] = Constants.LinkUri;
                        link["uri"] = position_from.Href;
                    }
                }

                // document[position_from.page_num - 1].insert_link(link)
                document[position_from.PageNum - 1].InsertLinkVoid(link);
            }
            return document;
        }

        /// <summary>
        /// The story DOM body node; main PDF content lives between body tags.
        /// </summary>
        public Xml Body
        {
            get
            {
                var dom = GetDocument();
                return dom.BodyTag() ?? throw new InvalidOperationException("Story HTML has no body");
            }
        }

        /// <summary>Returns the root DOM of the parsed HTML document.</summary>
        public Xml GetDocument()
        {
            var dom = NativeStory.fz_story_document();
            return Xml.FromDomNode(dom) ?? throw new InvalidOperationException("Story has no document");
        }

        /// <summary>
        /// Draws content prepared by the last <see cref="Place"/> to a page device.
        /// </summary>
        /// <param name="device">Device from <c>writer.BeginPage(mediabox)</c>.</param>
        /// <param name="matrix">Optional transform (default identity).</param>
        public void Draw(DeviceWrapper device = null, Matrix matrix = null)
        {
            Draw(device?.ToFzDevice(), matrix);
        }

        /// <summary>Draws content to a native <c>fz_device</c> (advanced interop).</summary>
        /// <param name="device">MuPDF device, or null for measuring-only draw.</param>
        /// <param name="matrix">Optional transform applied when drawing.</param>
        public void Draw(mupdf.FzDevice device = null, Matrix matrix = null)
        {
            // ctm2 = JM_matrix_from_py( matrix)
            var ctm2 = Helpers.MatrixToFz(matrix);
            var dev = device ?? new mupdf.FzDevice();
            NativeStory.fz_draw_story(dev, ctm2);
        }

        /// <summary>
        /// Invokes a callback with placement info for headings, ids, and links (call after <see cref="Place"/>).
        /// </summary>
        /// <param name="function">Receives a <see cref="Position"/> per element; null is allowed.</param>
        /// <param name="arg">Extra fields copied onto each record (e.g. <c>PageNum</c>).</param>
        public void ElementPositions(Action<Position> function, Position arg = null)
        {
            Action<StoryElementPositionInfo> callback = function == null
                ? null
                : info => function(PositionFromInfo(info));
            ElementPositions(callback, PositionToArgs(arg));
        }

        /// <summary>
        /// Invokes a callback with placement info for headings, ids, and links (call after <see cref="Place"/>).
        /// </summary>
        /// <param name="function">Receives one <see cref="StoryElementPositionInfo"/> per interesting element.</param>
        /// <param name="args">Optional extra fields merged onto each record (keys must be valid C# identifiers).</param>
        public void ElementPositions(Action<StoryElementPositionInfo> function, Dictionary<string, object> args = null)
        {
            if (args != null)
            {
                foreach (var k in args.Keys)
                {
                    if (string.IsNullOrEmpty(k) || !IsIdentifier(k))
                        throw new ArgumentException($"invalid key '{k}'");
                }
            }
            else
            {
                // args = {}
                args = new Dictionary<string, object>();
            }
            if (function == null)
                function = _ => { };

            var cb = new StoryPositionCollector(function, args);
            mupdf.mupdf.ll_fz_story_positions_director(NativeStory.m_internal, cb);
        }

        /// <summary>
        /// Layouts the next portion of story content into <paramref name="where"/>.
        /// </summary>
        /// <param name="where">Target rectangle on the page (typically inside the mediabox).</param>
        /// <param name="flags">Native placement flags (e.g. <c>FZ_PLACE_STORY_FLAG_NO_OVERFLOW</c>).</param>
        /// <returns><c>more</c> is true if content remains; <c>filled</c> is the area actually used.</returns>
        public (bool more, Rect filled) Place(Rect where, int flags = 0)
        {
            // where = JM_rect_from_py( where)
            var whereFz = where.ToFzRect();
            var filled = new mupdf.FzRect();
            int more = NativeStory.fz_place_story_flags(whereFz, filled, flags);
            return (more != 0, RectFromFz(filled));
        }

        /// <summary>Rewinds the story so output can start again from the beginning.</summary>
        public void Reset()
        {
            NativeStory.fz_reset_story();
        }

        /// <summary>
        /// Places and draws the story using <paramref name="rectfn"/> until content is exhausted.
        /// </summary>
        /// <param name="writer"><see cref="DocumentWriter"/> target, or null to layout without writing pages.</param>
        /// <param name="rectfn">Supplies mediabox, placement rect, and CTM per rectangle.</param>
        /// <param name="positionfn">Optional callback; receives <see cref="StoryElementPositionInfo"/> with <see cref="StoryElementPositionInfo.PageNum"/> set.</param>
        /// <param name="pagefn">Optional page hook: <c>(pageNum, mediabox, device, after)</c> where <c>after</c> is 0 at page start, 1 at page end.</param>
        public void Write(DocumentWriter writer, StoryRectFn rectfn, Action<StoryElementPositionInfo> positionfn = null, Action<int, Rect, mupdf.FzDevice, int> pagefn = null)
        {
            DeviceWrapper dev = null;
            // page_num = 0
            int page_num = 0;
            // rect_num = 0
            int rect_num = 0;
            var filled = new Rect(0, 0, 0, 0);
            // while 1:
            while (true)
            {
                // mediabox, rect, ctm = rectfn(rect_num, filled)
                var (mediabox, rect, ctm) = rectfn(rect_num, filled);
                // rect_num += 1
                rect_num += 1;
                if (PyRectBool(mediabox))
                {
                    // new page.
                    // page_num += 1
                    page_num += 1;
                }
                // more, filled = self.place( rect)
                var (more, filledOut) = Place(rect);
                filled = filledOut;
                if (positionfn != null)
                {
                    void positionfn2(StoryElementPositionInfo position)
                    {
                        // position.page_num = page_num
                        position.PageNum = page_num;
                        // positionfn(position)
                        positionfn(position);
                    }
                    // self.element_positions(positionfn2)
                    ElementPositions(positionfn2);
                }
                if (writer != null)
                {
                    if (PyRectBool(mediabox))
                    {
                        // new page.
                        if (dev != null)
                        {
                            if (pagefn != null)
                                // pagefn(page_num, mediabox, dev, 1)
                                pagefn(page_num, mediabox, dev?.ToFzDevice(), 1);
                            // writer.end_page()
                            writer.EndPage();
                        }
                        dev = writer.BeginPage(mediabox);
                        if (pagefn != null)
                            // pagefn(page_num, mediabox, dev, 0)
                            pagefn(page_num, mediabox, dev?.ToFzDevice(), 0);
                    }
                    // self.draw( dev, ctm)
                    Draw(dev, ctm);
                    if (!more)
                    {
                        if (pagefn != null)
                            // pagefn( page_num, mediabox, dev, 1)
                            pagefn(page_num, mediabox, dev?.ToFzDevice(), 1);
                        // writer.end_page()
                        writer.EndPage();
                    }
                }
                else
                {
                    // self.draw(None, ctm)
                    Draw((mupdf.FzDevice)null, ctm);
                }
                if (!more)
                    // break
                    break;
            }
        }

        /// <summary>Legacy overload using <see cref="RectFunction"/> and <see cref="Position"/> callbacks.</summary>
        public void Write(DocumentWriter writer, RectFunction rectfn, Action<Position> positionfn = null, Action<int, Rect, DeviceWrapper, bool> pagefn = null)
        {
            Action<StoryElementPositionInfo> positionfn2 = positionfn == null
                ? null
                : info => positionfn(PositionFromInfo(info));
            Action<int, Rect, mupdf.FzDevice, int> pagefn2 = pagefn == null
                ? null
                : (pageNum, mediabox, device, after) =>
                    pagefn(pageNum, mediabox, device == null ? null : new DeviceWrapper(device), after != 0);
            Write(writer, (rectNum, filled) => rectfn(rectNum, filled), positionfn2, pagefn2);
        }

        /// <summary>
        /// Rebuilds HTML from <paramref name="contentfn"/> until stable, then writes with <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">Final output writer; null during intermediate passes.</param>
        /// <param name="contentfn">Returns HTML from prior <see cref="StoryElementPositionInfo"/> list (e.g. table of contents). Use <see cref="ContentFunction"/> for legacy naming.</param>
        /// <param name="rectfn">Pagination callback.</param>
        /// <param name="user_css">User CSS for each iteration.</param>
        /// <param name="em">Base font size.</param>
        /// <param name="positionfn">Called when layout has stabilized.</param>
        /// <param name="pagefn">Per-page hook (see <see cref="Write"/>).</param>
        /// <param name="archive">Resource archive for images/fonts.</param>
        /// <param name="add_header_ids">When true, assign ids to header tags without ids.</param>
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
            // content = None
            string content = null;
            // while 1:
            while (true)
            {
                // content_prev = content
                string content_prev = content;
                // content = contentfn( positions)
                content = contentfn(positions);
                // stable = False
                bool stable = false;
                if (content == content_prev)
                    // stable = True
                    stable = true;
                // content2 = content
                string content2 = content;
                // story = Story(content2, user_css, em, archive)
                using var story = new Story(content2, user_css, em, archive);
                if (add_header_ids)
                    // story.add_header_ids()
                    story.AddHeaderIds();

                // positions = list()
                positions = new List<StoryElementPositionInfo>();
                void positionfn2(StoryElementPositionInfo position)
                {
                    // positions.append(position)
                    positions.Add(position);
                    if (stable && positionfn != null)
                        // positionfn(position)
                        positionfn(position);
                }
                // story.write(
                story.Write(
                    // writer if stable else None,
                    stable ? writer : null,
                    rectfn,
                    positionfn2,
                    pagefn
                );
                if (stable)
                    // break
                    break;
            }
        }

        /// <summary>
        /// Like <see cref="WriteStabilized"/> but returns a PDF <see cref="Document"/> with internal links.
        /// </summary>
        /// <param name="contentfn">HTML builder from element positions.</param>
        /// <param name="rectfn">Pagination callback.</param>
        /// <param name="user_css">User CSS.</param>
        /// <param name="em">Base font size.</param>
        /// <param name="positionfn">Optional position callback on final pass.</param>
        /// <param name="pagefn">Optional page hook.</param>
        /// <param name="archive">Resource archive.</param>
        /// <param name="add_header_ids">Assign header ids when missing.</param>
        /// <returns>PDF document with goto links for internal <c>href</c> anchors.</returns>
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
            // stream = io.BytesIO()
            var stream = mupdf.mupdf.fz_new_buffer(1024);
            // writer = DocumentWriter(stream)
            using var writer = new DocumentWriter(stream);
            // positions = []
            var positions = new List<StoryElementPositionInfo>();
            void positionfn2(StoryElementPositionInfo position)
            {
                // positions.append(position)
                positions.Add(position);
                if (positionfn != null)
                    // positionfn(position)
                    positionfn(position);
            }
            // Story.write_stabilized(writer, contentfn, rectfn, user_css, em, positionfn2, pagefn, archive, add_header_ids)
            WriteStabilized(writer, contentfn, rectfn, user_css, em, positionfn2, pagefn, archive, add_header_ids);
            // writer.close()
            byte[] pdf = writer.Close();
            // stream.seek(0)
            return AddPdfLinks(pdf, positions);
        }

        /// <summary>
        /// Like <see cref="Write"/> but returns an in-memory PDF <see cref="Document"/> with internal links.
        /// </summary>
        public Document WriteWithLinks(RectFunction rectfn, Action<Position> positionfn = null, Action<int, Rect, DeviceWrapper, bool> pagefn = null)
        {
            StoryRectFn wrapped = (rectNum, filled) => rectfn(rectNum, filled);
            Action<StoryElementPositionInfo> positionfn2 = positionfn == null
                ? null
                : info => positionfn(PositionFromInfo(info));
            Action<int, Rect, mupdf.FzDevice, int> pagefn2 = pagefn == null
                ? null
                : (pageNum, mediabox, device, after) =>
                    pagefn(pageNum, mediabox, device == null ? null : new DeviceWrapper(device), after != 0);
            return WriteWithLinks(wrapped, positionfn2, pagefn2);
        }

        /// <summary>
        /// Like <see cref="Write"/> but returns an in-memory PDF <see cref="Document"/> with internal links.
        /// </summary>
        /// <param name="rectfn">Pagination callback.</param>
        /// <param name="positionfn">Collects element positions for link targets.</param>
        /// <param name="pagefn">Optional page hook (see <see cref="Write"/>).</param>
        public Document WriteWithLinks(StoryRectFn rectfn, Action<StoryElementPositionInfo> positionfn = null, Action<int, Rect, mupdf.FzDevice, int> pagefn = null)
        {
            // stream = io.BytesIO()
            var stream = mupdf.mupdf.fz_new_buffer(1024);
            // writer = DocumentWriter(stream)
            using var writer = new DocumentWriter(stream);
            // positions = []
            var positions = new List<StoryElementPositionInfo>();
            void positionfn2(StoryElementPositionInfo position)
            {
                // positions.append(position)
                positions.Add(position);
                if (positionfn != null)
                    // positionfn(position)
                    positionfn(position);
            }
            // self.write(writer, rectfn, positionfn=positionfn2, pagefn=pagefn)
            Write(writer, rectfn, positionfn2, pagefn);
            // writer.close()
            byte[] pdf = writer.Close();
            // stream.seek(0)
            return AddPdfLinks(pdf, positions);
        }

        /// <summary>
        /// Result of <see cref="Fit"/>, <see cref="FitScale"/>, <see cref="FitHeight"/>, or <see cref="FitWidth"/>.
        /// </summary>
        public class FitResult
        {
            /// <summary><c>true</c> when the story fit in the computed rectangle.</summary>
            public bool? BigEnough { get; set; }

            /// <summary>Area filled by the last <see cref="Place"/> call.</summary>
            public Rect Filled { get; set; }

            /// <summary><c>false</c> when the fit succeeded (no remaining content).</summary>
            public bool? More { get; set; }

            /// <summary>How many <see cref="Place"/> calls were made during the search.</summary>
            public int? NumCalls { get; set; }

            /// <summary>Parameter value that fit, or the largest value that still failed.</summary>
            public float? Parameter { get; set; }

            /// <summary>Rectangle built from <see cref="Parameter"/> via the fit function.</summary>
            public Rect Rect { get; set; }

            internal bool? big_enough { get => BigEnough; set => BigEnough = value; }
            internal Rect filled { get => Filled; set => Filled = value; }
            internal bool? more { get => More; set => More = value; }
            internal int? numcalls { get => NumCalls; set => NumCalls = value; }
            internal float? parameter { get => Parameter; set => Parameter = value; }
            internal Rect rect { get => Rect; set => Rect = value; }

            public override string ToString()
            {
                return $" big_enough={BigEnough} filled={Filled} more={More} numcalls={NumCalls} parameter={Parameter} rect={Rect}";
            }
        }

        /// <summary>
        /// Find an optimal rectangle parameter that contains the story .
        /// On success, the last <see cref="Place"/> used the returned rectangle so <see cref="Draw"/> can be called directly.
        /// </summary>
        /// <param name="fn">Maps parameter to a rectangle; empty rect means “does not fit” without calling <see cref="Place"/>. Width and height must not shrink as parameter increases.</param>
        /// <param name="pmin">Minimum parameter, or null for unbounded search downward.</param>
        /// <param name="pmax">Maximum parameter, or null for unbounded search upward.</param>
        /// <param name="delta">Maximum error in the returned parameter.</param>
        /// <param name="verbose">Log search steps via <see cref="Helpers.message"/>.</param>
        /// <param name="flags">Flags passed to each <see cref="Place"/> during the search.</param>
        /// <returns>On success, the last <see cref="Place"/> used the returned rectangle so <see cref="Draw"/> can follow immediately.</returns>
        public FitResult Fit(Func<float, Rect> fn, float? pmin = null, float? pmax = null, float delta = 0.001f, bool verbose = false, int flags = 0)
        {
            if (fn == null) throw new ArgumentNullException(nameof(fn));

            void Log(string text)
            {
                if (verbose)
                    // message(f'fit(): {text}')
                    Helpers.message($"fit(): {text}");
            }


            float? state_pmin = pmin;
            float? state_pmax = pmax;
            FitResult state_pmin_result = null;
            FitResult state_pmax_result = null;
            int state_numcalls = 0;
            float? state_last_p = null;
            float? state_pmin0 = verbose ? pmin : null;
            float? state_pmax0 = verbose ? pmax : null;

            if (verbose)
                Log($"starting. state_pmin={state_pmin} state_pmax={state_pmax}.");

            // self.reset()
            Reset();

            FitResult ret()
            {
                if (state_pmax.HasValue)
                {
                    if (!state_last_p.HasValue || state_last_p.Value != state_pmax.Value)
                    {
                        if (verbose)
                            Log("Calling update() with pmax, because was overwritten by later calls.");
                        bool big_enough = update(state_pmax.Value);
                        if (!big_enough)
                            throw new InvalidOperationException("fit(): internal state error: expected pmax to be big enough.");
                    }
                    return state_pmax_result;
                }
                return state_pmin_result ?? new FitResult { NumCalls = state_numcalls };
            }

            bool update(float parameter)
            {
                var rect = fn(parameter);
                if (rect == null)
                    throw new InvalidOperationException($"fit(): fn(parameter) returned null.");

                bool big_enough;
                FitResult result;

                if (rect.IsEmpty)
                {
                    big_enough = false;
                    result = new FitResult
                    {
                        Parameter = parameter,
                        NumCalls = state_numcalls,
                    };
                    if (verbose)
                        Log("update(): not calling self.place() because rect is empty.");
                }
                else
                {
                    var (more, filled) = Place(rect, flags);
                    state_numcalls += 1;
                    big_enough = !more;
                    result = new FitResult
                    {
                        Filled = filled,
                        More = more,
                        NumCalls = state_numcalls,
                        Parameter = parameter,
                        Rect = rect,
                        BigEnough = big_enough,
                    };
                    if (verbose)
                        Log($"update(): called self.place(): {state_numcalls,2}: more={more} parameter={parameter} rect={rect}.");
                }

                if (big_enough)
                {
                    state_pmax = parameter;
                    state_pmax_result = result;
                }
                else
                {
                    state_pmin = parameter;
                    state_pmin_result = result;
                }

                state_last_p = parameter;
                return big_enough;
            }

            float opposite(float? p, int direction)
            {
                if (!p.HasValue || p.Value == 0)
                    return direction;
                if (direction * p.Value > 0)
                    return 2 * p.Value;
                return -p.Value;
            }

            if (!state_pmin.HasValue)
            {
                if (verbose) Log("finding pmin.");
                float parameter = opposite(state_pmax, -1);
                while (true)
                {
                    if (!update(parameter))
                        break;
                    parameter *= 2;
                }
            }
            else
            {
                if (update(state_pmin.Value))
                {
                    if (verbose) Log($"{state_pmin} is big enough.");
                    return ret();
                }
            }

            if (!state_pmax.HasValue)
            {
                if (verbose) Log("finding pmax.");
                float parameter = opposite(state_pmin, +1);
                while (true)
                {
                    if (update(parameter))
                        break;
                    parameter *= 2;
                }
            }
            else
            {
                if (!update(state_pmax.Value))
                {
                    state_pmax = null;
                    if (verbose) Log($"No solution possible state_pmax={state_pmax}.");
                    return ret();
                }
            }

            if (verbose) Log($"doing binary search with state_pmin={state_pmin} state_pmax={state_pmax}.");
            while (true)
            {
                if (!state_pmax.HasValue || !state_pmin.HasValue || state_pmax.Value - state_pmin.Value < delta)
                    return ret();
                float parameter = (state_pmin.Value + state_pmax.Value) / 2;
                update(parameter);
            }
        }

        /// <summary>
        /// Finds the smallest scale in <paramref name="scale_min"/>..<paramref name="scale_max"/> so the scaled <paramref name="rect"/> contains the story.
        /// </summary>
        /// <param name="rect">Base rectangle (origin and size).</param>
        /// <param name="scale_min">Minimum scale (≥ 0).</param>
        /// <param name="scale_max">Maximum scale, or null for no upper bound.</param>
        /// <param name="delta">Maximum error in returned scale.</param>
        /// <param name="verbose">Log search diagnostics.</param>
        /// <param name="flags">Placement flags for internal <see cref="Place"/> calls.</param>
        public FitResult FitScale(Rect rect, float scale_min = 0, float? scale_max = null, float delta = 0.001f, bool verbose = false, int flags = 0)
        {
            float x0 = rect.X0;
            float y0 = rect.Y0;
            float width = rect.Width;
            float height = rect.Height;
            Rect fn(float scale) => new Rect(x0, y0, x0 + scale * width, y0 + scale * height);
            return Fit(fn, scale_min, scale_max, delta, verbose, flags);
        }

        /// <summary>
        /// Finds the smallest height for a fixed <paramref name="width"/> rectangle that contains the story.
        /// </summary>
        /// <param name="width">Rectangle width.</param>
        /// <param name="height_min">Minimum height (≥ 0).</param>
        /// <param name="height_max">Maximum height, or null for no upper bound.</param>
        /// <param name="origin">Top-left corner of the rectangle (default 0, 0).</param>
        /// <param name="delta">Maximum error in returned height.</param>
        /// <param name="verbose">Log search diagnostics.</param>
        public FitResult FitHeight(float width, float height_min = 0, float? height_max = null, Point origin = null, float delta = 0.001f, bool verbose = false)
        {
            origin ??= new Point(0, 0);
            float x0 = origin.X;
            float y0 = origin.Y;
            float x1 = x0 + width;
            Rect fn(float height) => new Rect(x0, y0, x1, y0 + height);
            return Fit(fn, height_min, height_max, delta, verbose, 0);
        }

        /// <summary>
        /// Finds the smallest width for a fixed <paramref name="height"/> rectangle that contains the story.
        /// </summary>
        /// <param name="height">Rectangle height.</param>
        /// <param name="width_min">Minimum width (≥ 0).</param>
        /// <param name="width_max">Maximum width, or null for no upper bound.</param>
        /// <param name="origin">Top-left corner of the rectangle (default 0, 0).</param>
        /// <param name="delta">Maximum error in returned width.</param>
        /// <param name="verbose">Log search diagnostics.</param>
        public FitResult FitWidth(float height, float width_min = 0, float? width_max = null, Point origin = null, float delta = 0.001f, bool verbose = false)
        {
            origin ??= new Point(0, 0);
            float x0 = origin.X;
            float y0 = origin.Y;
            float y1 = y0 + height;
            Rect fn(float width) => new Rect(x0, y0, x0 + width, y1);
            return Fit(fn, width_min, width_max, delta, verbose, 0);
        }

        // ─── MuPDF API names (internal, same assembly) ─────────────────

        internal void add_header_ids() => AddHeaderIds();
        internal Xml body => Body;
        internal Xml document() => GetDocument();
        internal void reset() => Reset();
        internal void draw(DeviceWrapper device = null, Matrix matrix = null) => Draw(device, matrix);
        internal void draw(mupdf.FzDevice device = null, Matrix matrix = null) => Draw(device, matrix);
        internal void element_positions(Action<StoryElementPositionInfo> function, Dictionary<string, object> args = null) => ElementPositions(function, args);
        internal (bool more, Rect filled) place(Rect where, int flags = 0) => Place(where, flags);
        internal void write(DocumentWriter writer, StoryRectFn rectfn, Action<StoryElementPositionInfo> positionfn = null, Action<int, Rect, mupdf.FzDevice, int> pagefn = null)
            => Write(writer, rectfn, positionfn, pagefn);
        internal static Document add_pdf_links(object document_or_stream, List<StoryElementPositionInfo> positions) => AddPdfLinks(document_or_stream, positions);
        internal static void write_stabilized(DocumentWriter writer, Func<List<StoryElementPositionInfo>, string> contentfn, StoryRectFn rectfn, string user_css = null, float em = 12, Action<StoryElementPositionInfo> positionfn = null, Action<int, Rect, mupdf.FzDevice, int> pagefn = null, Archive archive = null, bool add_header_ids = true)
            => WriteStabilized(writer, contentfn, rectfn, user_css, em, positionfn, pagefn, archive, add_header_ids);
        internal static Document write_stabilized_with_links(Func<List<StoryElementPositionInfo>, string> contentfn, StoryRectFn rectfn, string user_css = null, float em = 12, Action<StoryElementPositionInfo> positionfn = null, Action<int, Rect, mupdf.FzDevice, int> pagefn = null, Archive archive = null, bool add_header_ids = true)
            => WriteStabilizedWithLinks(contentfn, rectfn, user_css, em, positionfn, pagefn, archive, add_header_ids);
        internal Document write_with_links(StoryRectFn rectfn, Action<StoryElementPositionInfo> positionfn = null, Action<int, Rect, mupdf.FzDevice, int> pagefn = null)
            => WriteWithLinks(rectfn, positionfn, pagefn);
        internal FitResult fit(Func<float, Rect> fn, float? pmin = null, float? pmax = null, float delta = 0.001f, bool verbose = false, int flags = 0)
            => Fit(fn, pmin, pmax, delta, verbose, flags);
        internal FitResult fit_scale(Rect rect, float scale_min = 0, float? scale_max = null, float delta = 0.001f, bool verbose = false, int flags = 0)
            => FitScale(rect, scale_min, scale_max, delta, verbose, flags);
        internal FitResult fit_height(float width, float height_min = 0, float? height_max = null, Point origin = null, float delta = 0.001f, bool verbose = false)
            => FitHeight(width, height_min, height_max, origin, delta, verbose);
        internal FitResult fit_width(float height, float width_min = 0, float? width_max = null, Point origin = null, float delta = 0.001f, bool verbose = false)
            => FitWidth(height, width_min, width_max, origin, delta, verbose);

        private static bool PyRectBool(Rect? r)
        {
            if (r == null) return false;
            float max = Math.Max(Math.Max(r.X0, r.Y0), Math.Max(r.X1, r.Y1));
            float min = Math.Min(Math.Min(r.X0, r.Y0), Math.Min(r.X1, r.Y1));
            return max != 0 || min != 0;
        }

        private static Rect RectFromFz(mupdf.FzRect filled) => new Rect(filled.x0, filled.y0, filled.x1, filled.y1);

        private static Position PositionFromInfo(StoryElementPositionInfo info)
        {
            if (info == null)
                return new Position();
            return new Position
            {
                Depth = info.Depth,
                Heading = info.Heading,
                Href = info.Href,
                Id = info.Id,
                Rect = info.Rect,
                Text = info.Text,
                OpenClose = info.OpenClose != 0,
                RectNum = info.RectNum,
                PageNum = info.PageNum,
            };
        }

        private static Dictionary<string, object> PositionToArgs(Position arg)
        {
            if (arg == null)
                return new Dictionary<string, object>();
            return new Dictionary<string, object>
            {
                ["Depth"] = arg.Depth,
                ["Heading"] = arg.Heading,
                ["Href"] = arg.Href,
                ["Id"] = arg.Id,
                ["Rect"] = arg.Rect,
                ["Text"] = arg.Text,
                ["OpenClose"] = arg.OpenClose ? 1 : 0,
                ["RectNum"] = arg.RectNum,
                ["PageNum"] = arg.PageNum,
            };
        }

        private static bool IsIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!(char.IsLetter(name[0]) || name[0] == '_')) return false;
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                    return false;
            }
            return true;
        }

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
                var position2 = new StoryElementPositionInfo
                {
                    Depth = position.depth,
                    Heading = position.heading,
                    Id = position.id,
                    Rect = new Rect(position.rect.x0, position.rect.y0, position.rect.x1, position.rect.y1),
                    Text = position.text,
                    OpenClose = position.open_close,
                    RectNum = position.rectangle_num,
                    Href = position.href,
                };
                if (_args != null && _args.Count > 0)
                {
                    foreach (var kv in _args)
                    {
                        var prop = typeof(StoryElementPositionInfo).GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (prop != null && prop.CanWrite)
                        {
                            object val = kv.Value;
                            if (val != null && !prop.PropertyType.IsInstanceOfType(val))
                                val = Convert.ChangeType(val, prop.PropertyType);
                            prop.SetValue(position2, val);
                        }
                    }
                }
                _callback(position2);
            }
        }

        /// <summary>Releases the native <c>fz_story</c> handle.</summary>
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

        ~Story() => Dispose();

        /// <inheritdoc />
        public override string ToString() => "Story()";
    }
}