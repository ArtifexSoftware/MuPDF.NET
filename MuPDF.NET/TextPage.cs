using System.Diagnostics;
using mupdf;
using Newtonsoft.Json;

namespace MuPDF.NET
{
    public class TextPage
    {
        static TextPage()
        {
            Utils.InitApp();
        }

        internal FzStextPage _nativeTextPage;

        public bool ThisOwn { get; set; }

        public Page Parent = null;

        /// <summary>
        /// Rect of Stext Page
        /// </summary>
        private FzRect MediaBox
        {
            get { return new FzRect(_nativeTextPage.m_internal.mediabox); }
        }

        /// <summary>
        /// Block List of Text
        /// </summary>
        public List<FzStextBlock> Blocks
        {
            get
            {
                List<FzStextBlock> blocks = new List<FzStextBlock>();
                for (
                    fz_stext_block block = _nativeTextPage.m_internal.first_block;
                    block != null;
                    block = block.next
                )
                {
                    blocks.Add(new FzStextBlock(block));
                }
                return blocks;
            }
        }

        /// <summary>
        /// MuPDFStextPage Constructor
        /// </summary>
        /// <param name="rect">Page Rectangle Size</param>
        public TextPage(FzRect rect)
        {
            _nativeTextPage = new FzStextPage(rect);
            Parent = null;
        }

        public TextPage(FzStextPage stPage)
        {
            _nativeTextPage = stPage;
            Parent = null;
        }

        /// <summary>
        /// MuPDFStextPage Contructor
        /// </summary>
        /// <param name="stPage">MuPDFStextPage object</param>
        public TextPage(TextPage stPage)
        {
            _nativeTextPage = stPage._nativeTextPage;
            Parent = stPage.Parent;
        }

        public TextPage(FzPage page)
        {
            _nativeTextPage = new FzStextPage(page, new FzStextOptions());
        }

        /// <summary>
        /// Extract Stext Page
        /// </summary>
        /// <param name="format">format of return value</param>
        /// <returns>string of Text, HTML, XHTML, .. according to format</returns>
        public string ExtractText(ExtractFormat format)
        {
            FzStextPage stPage = _nativeTextPage;
            FzBuffer buffer = new FzBuffer(1024);
            FzOutput output = new mupdf.FzOutput(buffer);

            if (format == ExtractFormat.HTML)
            {
                output.fz_print_stext_page_as_html(stPage, 0);
            }
            else if (format == ExtractFormat.XML)
            {
                output.fz_print_stext_page_as_xml(stPage, 0);
            }
            else if (format == ExtractFormat.XHTML)
            {
                output.fz_print_stext_page_as_xhtml(stPage, 0);
            }
            else
            {
                output.fz_print_stext_page_as_text(stPage);
            }

            string ret = buffer.fz_string_from_buffer();

            return ret;
        }

        /// <summary>
        /// Extract Blocks in StextPage
        /// </summary>
        /// <returns>Return List of TextBlock</returns>
        public List<TextBlock> ExtractBlocks()
        {
            int blockNum = -1;
            FzRect stPageRect = new FzRect(_nativeTextPage.m_internal.mediabox);
            FzBuffer res = new FzBuffer(1024);
            List<TextBlock> lines = new List<TextBlock>();

            foreach (FzStextBlock block in Blocks)
            {
                blockNum += 1;
                FzRect blockRect = new FzRect(FzRect.Fixed.Fixed_EMPTY);
                string text = "";
                if (block.m_internal.type == (int)STextBlockType.FZ_STEXT_BLOCK_TEXT)
                {
                    res.fz_clear_buffer();
                    int lineNum = -1;
                    int lastChar = 0;
                    for (
                        fz_stext_line line = block.begin().__ref__().m_internal;
                        line != null;
                        line = line.next
                    )
                    {
                        lineNum += 1;
                        FzRect lineRect = new FzRect(FzRect.Fixed.Fixed_EMPTY);
                        for (fz_stext_char ch = line.first_char; ch != null; ch = ch.next)
                        {
                            FzRect cbbox = GetCharBbox(new FzStextLine(line), new FzStextChar(ch));
                            if (
                                !IsRectsOverlap(stPageRect, cbbox)
                                && stPageRect.fz_is_infinite_rect() == 0
                            )
                                continue;

                            res.fz_append_rune(ch.c);
                            lastChar = ch.c;
                            lineRect = FzRect.fz_union_rect(lineRect, cbbox);
                        }

                        if (lastChar != 10 && lineRect.fz_is_empty_rect() != 0)
                            res.fz_append_rune(10);

                        blockRect = FzRect.fz_union_rect(blockRect, lineRect);
                    }
                    text = Utils.EscapeStrFromBuffer(res);
                }
                else if (
                    IsRectsOverlap(stPageRect, new FzRect(block.m_internal.bbox))
                    || stPageRect.fz_is_infinite_rect() != 0
                )
                {
                    FzImage img = block.i_image();
                    FzColorspace cs = img.colorspace();
                    text = string.Format(
                        "<image: {0}, width: {1}, height: {2}, bpc: {3}>",
                        cs.fz_colorspace_name(),
                        img.w(),
                        img.h(),
                        img.bpc()
                    );
                    blockRect = FzRect.fz_union_rect(blockRect, new FzRect(block.m_internal.bbox));
                }
                if (blockRect.fz_is_empty_rect() == 0)
                {
                    TextBlock line = new TextBlock();
                    line.X0 = blockRect.x0;
                    line.Y0 = blockRect.y0;
                    line.X1 = blockRect.x1;
                    line.Y1 = blockRect.y1;
                    line.BlockNum = blockNum;
                    line.Text = text;
                    line.Type = block.m_internal.type;

                    lines.Add(line);
                }
            }
            return lines;
        }

        /// <summary>
        /// Extract Stext Page
        /// </summary>
        /// <param name="cropbox">Rectangle to extract in StextPage</param>
        /// <param name="sort"></param>
        /// <returns>Page information of CropBox</returns>
        public PageInfo ExtractDict(Rect cropbox, bool sort = false)
        {
            PageInfo pageDict = TextPage2Dict(false);
            if (cropbox != null)
            {
                pageDict.Width = cropbox.Width;
                pageDict.Height = cropbox.Height;
            }
            if (sort is true)
            {
                List<Block> blocks = pageDict.Blocks;
                blocks.Sort(
                    (b1, b2) =>
                    {
                        if (b1.Bbox.Y1 == b2.Bbox.Y1)
                        {
                            return b1.Bbox.X0.CompareTo(b2.Bbox.X0);
                        }
                        else
                        {
                            return b1.Bbox.Y1.CompareTo(b2.Bbox.Y1);
                        }
                    }
                );

                pageDict.Blocks = blocks;
            }
            return pageDict;
        }

        /// <summary>
        /// Extract Stext Page in HTML
        /// </summary>
        /// <returns>string of HTML</returns>
        public string ExtractHtml()
        {
            return ExtractText(ExtractFormat.HTML);
        }

        /// <summary>
        /// Extract Stext Page with Image Information
        /// </summary>
        /// <param name="hashes">Encode image with given hash value</param>
        public List<Block> ExtractImageInfo(int hashes = 0)
        {
            int blockNum = -1;
            List<Block> rc = new List<Block>();
            foreach (FzStextBlock block in Blocks)
            {
                blockNum += 1;

                if (block.m_internal.type == (int)STextBlockType.FZ_STEXT_BLOCK_TEXT)
                    continue;
                FzImage img = block.i_image();
                vectoruc digest = new vectoruc();
                if (hashes != 0)
                {
                    FzIrect r = new FzIrect(
                        Utils.FZ_MIN_INF_RECT,
                        Utils.FZ_MIN_INF_RECT,
                        Utils.FZ_MAX_INF_RECT,
                        Utils.FZ_MAX_INF_RECT
                    );

                    Debug.Assert(r.fz_is_infinite_irect() != 0, "Rect is infinite");

                    FzMatrix m = new FzMatrix(img.w(), 0, 0, img.h(), 0, 0);

                    SWIGTYPE_p_int swigW = new SWIGTYPE_p_int(new IntPtr(), false);
                    SWIGTYPE_p_int swigH = new SWIGTYPE_p_int(new IntPtr(), false);
                    FzPixmap pixmap = img.fz_get_pixmap_from_image(r, m, swigW, swigH);
                    digest = pixmap.fz_md5_pixmap2();
                }
                FzColorspace cs = new FzColorspace(
                    mupdf.mupdf.ll_fz_keep_colorspace(img.m_internal.colorspace)
                );
                Block blockDict = new Block();
                blockDict.Number = blockNum;
                blockDict.Bbox = new Rect(new FzRect(block.m_internal.bbox));
                blockDict.Transform = new Matrix(block.i_transform());
                blockDict.Width = img.w();
                blockDict.Height = img.h();
                blockDict.ColorSpace = cs.fz_colorspace_n();
                blockDict.CsName = cs.fz_colorspace_name();
                blockDict.Xres = img.xres();
                blockDict.Yres = img.yres();
                blockDict.Bpc = img.bpc();
                blockDict.Size = img.fz_image_size();
                if (hashes != 0)
                {
                    blockDict.Digest = digest;
                }
                rc.Add(blockDict);
            }
            return rc;
        }

        /// <summary>
        /// Extract StextPage in JSON format
        /// </summary>
        /// <param name="cropbox">Rectangle area to extract</param>
        /// <param name="sort"></param>
        public string ExtractJSON(Rect cropbox = null, bool sort = false)
        {
            PageInfo pageDict = TextPage2Dict(false);
            if (cropbox != null)
            {
                pageDict.Width = cropbox.Width;
                pageDict.Height = cropbox.Height;
            }

            if (sort)
            {
                List<Block> blocks = pageDict.Blocks;
                blocks.Sort(
                    (b1, b2) =>
                    {
                        if (b1.Bbox.Y1 == b2.Bbox.Y1)
                        {
                            return b1.Bbox.X0.CompareTo(b2.Bbox.X0);
                        }
                        else
                        {
                            return b1.Bbox.Y1.CompareTo(b2.Bbox.Y1);
                        }
                    }
                );

                pageDict.Blocks = blocks;
            }
            string ret = JsonConvert.SerializeObject(pageDict, Formatting.Indented);
            return ret;
        }

        public string ExtractXML()
        {
            return ExtractText(ExtractFormat.XML);
        }

        /// <summary>
        /// Extract Stext Page in format of PageStruct
        /// </summary>
        /// <param name="cropbox">Rectangle Area to Extract</param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public PageInfo ExtractRAWDict(Rect cropbox, bool sort = false)
        {
            PageInfo pageDict = TextPage2Dict(true);
            if (cropbox != null)
            {
                pageDict.Width = cropbox.Width;
                pageDict.Height = cropbox.Height;
            }
            if (sort is true)
            {
                List<Block> blocks = pageDict.Blocks;
                blocks.Sort(
                    (b1, b2) =>
                    {
                        if (b1.Bbox.Y1 == b2.Bbox.Y1)
                        {
                            return b1.Bbox.X0.CompareTo(b2.Bbox.X0);
                        }
                        else
                        {
                            return b1.Bbox.X0.CompareTo(b2.Bbox.X0);
                        }
                    }
                );

                pageDict.Blocks = blocks;
            }
            return pageDict;
        }

        /// <summary>
        /// Extract selection in format of string
        /// </summary>
        /// <param name="a">begin point of selection</param>
        /// <param name="b">end point of selection</param>
        /// <returns>returns text in format of string</returns>
        public string ExtractSelection(Point a, Point b)
        {
            return mupdf.mupdf.fz_copy_selection(_nativeTextPage, a.ToFzPoint(), b.ToFzPoint(), 0);
        }

        /// <summary>
        /// Extract Stext Page in format of Text
        /// </summary>
        /// <param name="sort"></param>
        /// <returns>Return String</returns>
        public string ExtractText(bool sort = false)
        {
            if (sort is false)
            {
                return ExtractText(ExtractFormat.TEXT);
            }

            List<TextBlock> blocks = ExtractBlocks();
            blocks.Sort(
                (b1, b2) =>
                {
                    if (b1.Y1 == b2.Y1)
                    {
                        return b1.X0.CompareTo(b2.X0);
                    }
                    else
                    {
                        return b1.Y1.CompareTo(b2.Y1);
                    }
                }
            );

            string ret = "";
            foreach (TextBlock b in blocks)
            {
                ret += b.Text;
            }

            return ret;
        }

        /// <summary>
        /// Extract TextBoxes
        /// </summary>
        /// <param name="area">Rectangle Area to extract</param>
        /// <returns>Returns string in area</returns>
        public string ExtractTextBox(FzRect area)
        {
            bool isNeedNewLine = false;
            string ret = "";

            foreach (FzStextBlock block in Blocks)
            {
                if (block.m_internal.type != (int)STextBlockType.FZ_STEXT_BLOCK_TEXT)
                    continue;

                for (
                    fz_stext_line line = block.begin().__ref__().m_internal;
                    line != null;
                    line = line.next
                )
                {
                    bool isLineHadText = false;
                    for (fz_stext_char ch = line.first_char; ch != null; ch = ch.next)
                    {
                        FzRect r = GetCharBbox(new FzStextLine(line), new FzStextChar(ch));
                        if (IsRectsOverlap(area, r))
                        {
                            isLineHadText = true;
                            if (isNeedNewLine)
                            {
                                ret += "\n";
                                isNeedNewLine = false;
                            }

                            ret += MakeEscape(ch.c);
                        }
                    }
                    if (isLineHadText)
                        isNeedNewLine = true;
                }
            }

            return Utils.DecodeRawUnicodeEscape(ret);
        }

        /// <summary>
        /// Extract Words
        /// </summary>
        /// <param name="delimiters"></param>
        /// <returns></returns>
        public List<WordBlock> ExtractWords(char[] delimiters = null)
        {
            int bufferLen;
            int blockNum = -1;
            FzRect wordBox = new FzRect(FzRect.Fixed.Fixed_EMPTY);
            FzRect stPageRect = MediaBox;

            List<WordBlock> lines = new List<WordBlock>();
            FzBuffer buf = new FzBuffer(64);
            foreach (FzStextBlock block in Blocks)
            {
                blockNum += 1;
                if (block.m_internal.type != (int)STextBlockType.FZ_STEXT_BLOCK_TEXT)
                    continue;

                int lineNum = -1;
                for (
                    fz_stext_line line = block.begin().__ref__().m_internal;
                    line != null;
                    line = line.next
                )
                {
                    lineNum += 1;
                    int wordNum = 0;
                    buf.fz_clear_buffer();
                    bufferLen = 0;
                    for (fz_stext_char ch = line.first_char; ch != null; ch = ch.next)
                    {
                        FzRect cbBox = GetCharBbox(new FzStextLine(line), new FzStextChar(ch));
                        if (
                            !IsRectsOverlap(stPageRect, cbBox)
                            && stPageRect.fz_is_empty_rect() == 0
                        )
                            continue;

                        bool isWordDelimiter = IsWordDelimiter(ch.c, delimiters);
                        if (isWordDelimiter)
                        {
                            if (bufferLen == 0)
                                continue;
                            if (wordBox.fz_is_empty_rect() == 0)
                            {
                                wordNum = AppendWord(
                                    lines,
                                    buf,
                                    wordBox,
                                    blockNum,
                                    lineNum,
                                    wordNum
                                );
                                wordBox = new FzRect(FzRect.Fixed.Fixed_EMPTY);
                            }
                            buf.fz_clear_buffer();
                            bufferLen = 0;
                            continue;
                        }
                        buf.fz_append_rune(ch.c);
                        bufferLen += 1;
                        wordBox = FzRect.fz_union_rect(
                            wordBox,
                            GetCharBbox(new FzStextLine(line), new FzStextChar(ch))
                        );
                    }
                    if (bufferLen != 0 && wordBox.fz_is_empty_rect() == 0)
                    {
                        wordNum = AppendWord(lines, buf, wordBox, blockNum, lineNum, wordNum);
                        wordBox = new FzRect(FzRect.Fixed.Fixed_EMPTY);
                    }
                    bufferLen = 0;
                }
            }
            return lines;
        }

        /// <summary>
        /// Extract Stext Page in format of XHtml
        /// </summary>
        /// <returns>Returns string in format of XHtml</returns>
        public string ExtractXHtml()
        {
            return ExtractText(ExtractFormat.XHTML);
        }

        public uint PoolSize()
        {
            FzPool pool = new FzPool(_nativeTextPage.m_internal.pool);
            uint size = pool.fz_pool_size();
            pool.m_internal = null;
            return size;
        }

        /// <summary>
        /// Extract Hits of Matching in format of FzQuad List
        /// </summary>
        /// <param name="stPage">Stext Page</param>
        /// <param name="needle">Text to search</param>
        /// <returns></returns>
        public static List<Quad> Search(
            TextPage stPage,
            string needle,
            int hitMax = 0,
            bool quad = true
        )
        {
            FzRect rect = stPage.MediaBox;
            List<Quad> quads = new List<Quad>();
            if (string.IsNullOrEmpty(needle))
                return quads;

            Hits hits = new Hits();

            hits.Len = 0;
            hits.Quads = quads;
            hits.HFuzz = 0.2f;
            hits.VFuzz = 0.1f;

            FzBuffer buffer = stPage.GetBufferFromStextPage();
            string hayStackString = buffer.fz_string_from_buffer();

            int hayStack = 0;
            int begin = 0;
            int end = 0;

            int inside = 0;

            foreach (FzStextBlock block in stPage.Blocks)
            {
                if (block.m_internal.type != (int)STextBlockType.FZ_STEXT_BLOCK_TEXT)
                    continue;
                for (
                    fz_stext_line line = block.begin().__ref__().m_internal;
                    line != null;
                    line = line.next
                )
                {
                    for (fz_stext_char ch = line.first_char; ch != null; ch = ch.next)
                    {
                        if (rect.fz_is_infinite_rect() == 0)
                        {
                            FzRect r = stPage.GetCharBbox(
                                new FzStextLine(line),
                                new FzStextChar(ch)
                            );
                            if (!stPage.IsRectsOverlap(rect, r))
                                continue;
                        }
                        while (true)
                        {
                            if (inside == 0)
                            {
                                if (hayStack >= begin)
                                {
                                    inside = 1;
                                }
                            }
                            if (inside != 0)
                            {
                                if (hayStack < end)
                                {
                                    stPage.OnHighlightChar(
                                        hits,
                                        new FzStextLine(line),
                                        new FzStextChar(ch)
                                    );
                                    break;
                                }
                                else
                                {
                                    inside = 0;
                                    (begin, end) = stPage.FindString(
                                        hayStackString.Substring(hayStack),
                                        needle
                                    );

                                    if (begin == -1)
                                    {
                                        goto no_more_matches;
                                    }
                                    else
                                    {
                                        begin += hayStack;
                                        end += hayStack;
                                        continue;
                                    }
                                }
                            }
                            break;
                        }
                        Tuple<int, int> res = TextPage.Char2Canon(
                            hayStackString.Substring(hayStack)
                        );
                        hayStack += res.Item1;
                    }
                    hayStack += 1;
                }
                hayStack += 1;
            }
            no_more_matches:
            ;
            buffer.fz_clear_buffer();

            int items = quads.Count;
            if (items == 0)
                return quads;
            int i = 0;
            List<Rect> ret = new List<Rect>();
            for (i = 0; i < items; i++)
            {
                if (!quad)
                    ret.Add(quads[i].Rect);
            }

            if (quad)
                return quads;

            i = 0;
            while (i < items - 1)
            {
                Quad v1 = quads[i];
                Quad v2 = quads[i + 1];
                if ((v1.Rect.Y1 != v2.Rect.Y1) || (v1.Rect & v2.Rect).IsEmpty)
                {
                    i += 1;
                    continue;
                }
                quads[i] = (v1.Rect | v2.Rect).Quad;
                quads.RemoveAt(i + 1);
                items -= 1;
            }

            return quads;
        }

        internal void OnHighlightChar(Hits hits, FzStextLine line, FzStextChar ch)
        {
            float vFuzz = ch.m_internal.size * hits.VFuzz;
            float hFuzz = ch.m_internal.size * hits.HFuzz;
            FzQuad chQuad = GetCharQuad(line, ch);

            if (hits.Len > 0)
            {
                FzQuad end = hits.Quads[hits.Len - 1].ToFzQuad();
                if (
                    true
                    && HDist(
                        new FzPoint(line.m_internal.dir),
                        new FzPoint(end.lr),
                        new FzPoint(chQuad.ll)
                    ) < hFuzz
                    && VDist(
                        new FzPoint(line.m_internal.dir),
                        new FzPoint(end.lr),
                        new FzPoint(chQuad.ll)
                    ) < vFuzz
                    && HDist(
                        new FzPoint(line.m_internal.dir),
                        new FzPoint(end.ur),
                        new FzPoint(chQuad.ul)
                    ) < hFuzz
                    && VDist(
                        new FzPoint(line.m_internal.dir),
                        new FzPoint(end.ur),
                        new FzPoint(chQuad.ll)
                    ) < vFuzz
                )
                {
                    end.ur = chQuad.ur;
                    end.lr = chQuad.lr;
                    Debug.Assert(hits.Quads[-1].ToFzQuad() == end);
                    return;
                }
            }

            hits.Quads.Add(new Quad(chQuad));
            hits.Len += 1;
        }

        internal float HDist(FzPoint dir, FzPoint a, FzPoint b)
        {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            return Math.Abs(dx * dir.x + dy * dir.y);
        }

        internal float VDist(FzPoint dir, FzPoint a, FzPoint b)
        {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            return Math.Abs(dx * dir.y + dy * dir.x);
        }

        /// <summary>
        /// Get Buffer from Stext Page
        /// </summary>
        /// <returns>Buffer</returns>
        internal FzBuffer GetBufferFromStextPage()
        {
            FzRect r = MediaBox;
            FzBuffer buf = new FzBuffer(256);
            foreach (FzStextBlock block in Blocks)
            {
                if (block.m_internal.type == (int)STextBlockType.FZ_STEXT_BLOCK_TEXT)
                {
                    for (
                        fz_stext_line line = block.begin().__ref__().m_internal;
                        line != null;
                        line = line.next
                    )
                    {
                        for (fz_stext_char ch = line.first_char; ch != null; ch = ch.next)
                        {
                            if (
                                !IsRectsOverlap(
                                    r,
                                    GetCharBbox(new FzStextLine(line), new FzStextChar(ch))
                                )
                                && r.fz_is_infinite_rect() == 0
                            )
                            {
                                continue;
                            }
                            buf.fz_append_rune(ch.c);
                        }
                        buf.fz_append_byte(Convert.ToInt32('\n'));
                    }
                    buf.fz_append_byte(Convert.ToInt32('\n'));
                }
            }
            return buf;
        }

        public (int, int) FindString(string s, string needle)
        {
            for (int index = 0; index < s.Length - needle.Length; index++)
            {
                int end = MatchString(s.Substring(index), needle);
                if (end != -1)
                {
                    end += index;
                    return (index, end);
                }
            }

            return (-1, -1);
        }

        public int MatchString(string h0, string n0)
        {
            int h = 0;
            int n = 0;
            int e = h;
            Tuple<int, int> hc = Char2Canon(h0.Substring(h));
            h += hc.Item1;
            Tuple<int, int> nc = Char2Canon(n0.Substring(n));
            n += nc.Item1;
            while (hc.Item2 == nc.Item2)
            {
                e = h;
                if (hc.Item2 == Convert.ToInt32(' '))
                {
                    while (true)
                    {
                        hc = Char2Canon(h0.Substring(h));
                        h += hc.Item1;
                        if (hc.Item2 != Convert.ToInt32(' '))
                        {
                            break;
                        }
                    }
                }
                else
                {
                    hc = Char2Canon(h0.Substring(h));
                    h += hc.Item1;
                }
                if (nc.Item2 == Convert.ToInt32(' '))
                {
                    while (true)
                    {
                        nc = Char2Canon(n0.Substring(n));
                        n += nc.Item1;
                        if (nc.Item2 != Convert.ToInt32(' '))
                        {
                            break;
                        }
                    }
                }
                else
                {
                    nc = Char2Canon(n0.Substring(n));
                    n += nc.Item1;
                }
            }

            return n <= n0.Length ? -1 : e;
        }

        public static int Canon(int c)
        {
            if (c == 0xA0 || c == 0x2028 || c == 0x2029)
            {
                return Convert.ToInt32(' ');
            }
            if (
                c == Convert.ToInt32('\r')
                || c == Convert.ToInt32('\n')
                || c == Convert.ToInt32('\t')
            )
            {
                return Convert.ToInt32(' ');
            }

            int A = Convert.ToInt32('A');
            if (c >= A && c <= Convert.ToInt32('Z'))
            {
                return c - A + Convert.ToInt32('a');
            }

            return c;
        }

        public static Tuple<int, int> Char2Canon(string s)
        {
            ll_fz_chartorune_outparams outparams = new ll_fz_chartorune_outparams();

            int n = mupdf.mupdf.ll_fz_chartorune_outparams_fn(s, outparams);
            int c = Canon(outparams.rune);
            /*if (s.Length == 0)
                return new Tuple<int, int>(1, -1);*/
            return new Tuple<int, int>(n, c);
        }

        internal int AppendWord(
            List<WordBlock> lines,
            FzBuffer buf,
            FzRect wordBox,
            int blockNum,
            int lineNum,
            int wordNum
        )
        {
            string s = Utils.EscapeStrFromBuffer(buf);
            WordBlock item = new WordBlock()
            {
                X0 = wordBox.x0,
                Y0 = wordBox.y0,
                X1 = wordBox.x1,
                Y1 = wordBox.y1,
                Text = s,
                BlockNum = blockNum,
                LineNum = lineNum,
                WordNum = wordNum
            };

            lines.Add(item);
            return wordNum + 1;
        }

        internal bool IsWordDelimiter(int ch, char[] delimiters)
        {
            if (ch <= 32 || ch == 160)
                return true;
            if (delimiters == null)
                return false;

            char _ch = (char)ch;
            foreach (char _c in delimiters)
            {
                if (_c == _ch)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Utf8 to Unicode
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        internal string MakeEscape(int ch)
        {
            if (ch == 92)
            {
                return "\\u005c";
            }
            else if (32 <= ch && ch <= 127 || ch == 10)
            {
                return ((char)ch).ToString();
            }
            else if (0xd800 <= ch && ch <= 0xdfff)
            {
                return "\\ufffd";
            }
            else if (ch <= 0xffff)
            {
                return "\\u" + ch.ToString("x4");
            }
            else
            {
                return "\\U" + ch.ToString("x8");
            }
        }

        /// <summary>
        /// Make Block List of Stext Page
        /// </summary>
        /// <param name="pageDict">PageStruct including BlockList</param>
        /// <param name="raw"></param>
        internal void GetNewBlockList(PageInfo pageDict, bool raw)
        {
            MakeTextPage2Dict(pageDict, raw);
        }

        internal PageInfo TextPage2Dict(bool raw = false)
        {
            PageInfo pageDict = new PageInfo
            {
                Width = MediaBox.x1 - MediaBox.x0,
                Height = MediaBox.y1 - MediaBox.y0,
                Blocks = new List<Block>()
            };

            GetNewBlockList(pageDict, raw);
            return pageDict;
        }

        public string ExtractRawJSON(Rect cropbox = null, bool sort = false)
        {
            PageInfo val = TextPage2Dict(true);
            if (cropbox != null)
            {
                val.Width = cropbox.Width;
                val.Height = cropbox.Height;
            }
            if (sort == true)
            {
                List<Block> blocks = val.Blocks;
                blocks.Sort(
                    (b1, b2) =>
                    {
                        if (b1.Bbox.Y1 == b2.Bbox.Y1)
                        {
                            return b1.Bbox.X0.CompareTo(b2.Bbox.X0);
                        }
                        else
                        {
                            return b1.Bbox.Y1.CompareTo(b2.Bbox.Y1);
                        }
                    }
                );
                val.Blocks = blocks;
            }

            string ret = JsonConvert.SerializeObject(val, Formatting.Indented);
            return ret;
        }

        internal void MakeTextPage2Dict(PageInfo pageDict, bool raw)
        {
            FzBuffer textBuffer = new FzBuffer(128);
            FzRect stPageRect = MediaBox;
            int blockNum = -1;
            foreach (FzStextBlock block in Blocks)
            {
                blockNum += 1;

                if (
                    stPageRect.fz_contains_rect(new FzRect(block.m_internal.bbox)) == 0
                    && stPageRect.fz_is_infinite_rect() == 0
                    && block.m_internal.type == (int)STextBlockType.FZ_STEXT_BLOCK_IMAGE
                )
                    continue;

                if (
                    stPageRect.fz_is_infinite_rect() != 0
                    && (
                        FzRect.fz_intersect_rect(stPageRect, new FzRect(block.m_internal.bbox))
                    ).fz_is_empty_rect() != 0
                )
                    continue;

                Block blockDict = new Block();

                blockDict.Number = blockNum;
                blockDict.Type = block.m_internal.type;
                if (block.m_internal.type == (int)STextBlockType.FZ_STEXT_BLOCK_IMAGE)
                {
                    blockDict.Bbox = new Rect(new FzRect(block.m_internal.bbox));
                    FzImage image = block.i_image();
                    int n = image.colorspace().fz_colorspace_n();
                    int w = image.w();
                    int h = image.h();
                    int type = (int)ImageType.FZ_IMAGE_UNKNOWN;

                    FzCompressedBuffer compressedBuffer = new FzCompressedBuffer(
                        mupdf.mupdf.ll_fz_compressed_image_buffer(image.m_internal)
                    );
                    if (compressedBuffer != null)
                    {
                        type = compressedBuffer.m_internal.params_.type;
                    }

                    if (type < (int)ImageType.FZ_IMAGE_BMP || type == (int)ImageType.FZ_IMAGE_JBIG2)
                    {
                        type = (int)ImageType.FZ_IMAGE_UNKNOWN;
                    }

                    FzBuffer buf;
                    string ext;

                    if (compressedBuffer != null && type != (int)ImageType.FZ_IMAGE_UNKNOWN)
                    {
                        buf = new FzBuffer(
                            mupdf.mupdf.ll_fz_keep_buffer(compressedBuffer.m_internal.buffer)
                        );
                        ext = Utils.GetImageExtension(type);
                    }
                    else
                    {
                        buf = mupdf.mupdf.fz_new_buffer_from_image_as_png(
                            image,
                            new FzColorParams()
                        );
                        ext = "png";
                    }

                    blockDict.Width = w;
                    blockDict.Height = h;
                    blockDict.Ext = ext;
                    blockDict.ColorSpace = n;
                    blockDict.Xres = image.xres();
                    blockDict.Yres = image.yres();
                    blockDict.Bpc = image.bpc();
                    blockDict.Transform = new Matrix(block.i_transform());
                    blockDict.Size = mupdf.mupdf.fz_image_size(image);
                    blockDict.Image = Utils.BinFromBuffer(buf);
                }
                else
                {
                    List<Line> lineList = new List<Line>();

                    FzRect blockRect = new FzRect(FzRect.Fixed.Fixed_EMPTY);

                    for (
                        fz_stext_line line = block.begin().__ref__().m_internal;
                        line != null;
                        line = line.next
                    )
                    {
                        if (
                            FzRect
                                .fz_intersect_rect(stPageRect, new FzRect(line.bbox))
                                .fz_is_empty_rect() != 0
                            && stPageRect.fz_is_infinite_rect() == 0
                        )
                            continue;

                        Line lineDict = new Line();

                        ///JM_make_spanlist
                        List<Char> charList = new List<Char>();
                        List<Span> spanList = new List<Span>();
                        mupdf.mupdf.fz_clear_buffer(textBuffer);
                        FzRect spanRect = new FzRect(FzRect.Fixed.Fixed_EMPTY);
                        FzRect lineRect = new FzRect(FzRect.Fixed.Fixed_EMPTY);

                        MuPDFCharStyle style = new MuPDFCharStyle();
                        MuPDFCharStyle oldStyle = new MuPDFCharStyle();

                        Span span = new Span();
                        FzPoint spanOrigin = new FzPoint();

                        for (fz_stext_char ch = line.first_char; ch != null; ch = ch.next)
                        {
                            FzRect r = GetCharBbox(new FzStextLine(line), new FzStextChar(ch));
                            if (
                                !IsRectsOverlap(stPageRect, r)
                                && stPageRect.fz_is_infinite_rect() != 0
                            )
                                continue;
                            float flags = CharFontFlags(
                                new FzFont(mupdf.mupdf.ll_fz_keep_font(ch.font)),
                                new FzStextLine(line),
                                new FzStextChar(ch)
                            );
                            FzPoint origin = new FzPoint(ch.origin);
                            style.Size = ch.size;
                            style.Flags = flags;
                            style.Font = GetFontName(
                                new FzFont(mupdf.mupdf.ll_fz_keep_font(ch.font))
                            );
                            style.Color = ch.color;
                            style.Asc = (
                                new FzFont(mupdf.mupdf.ll_fz_keep_font(ch.font))
                            ).fz_font_ascender();
                            style.Desc = (
                                new FzFont(mupdf.mupdf.ll_fz_keep_font(ch.font))
                            ).fz_font_descender();
                            if (
                                style.Size != oldStyle.Size
                                || style.Flags != oldStyle.Flags
                                || style.Color != oldStyle.Color
                                || style.Font != oldStyle.Font
                            )
                            {
                                if (oldStyle.Size >= 0)
                                {
                                    if (raw)
                                    {
                                        span.Chars = charList;
                                        charList = null;
                                    }
                                    else
                                    {
                                        span.Text = Utils.EscapeStrFromBuffer(textBuffer);
                                        mupdf.mupdf.fz_clear_buffer(textBuffer);
                                    }
                                    span.Origin = new Point(spanOrigin);
                                    span.Bbox = new Rect(spanRect);
                                    lineRect = FzRect.fz_union_rect(lineRect, spanRect);
                                    spanList.Add(span);
                                }
                                span = new Span();
                                float asc = style.Asc;
                                float desc = style.Desc;
                                if (style.Asc < 1e-3)
                                {
                                    asc = 0.9f;
                                    desc = -0.1f;
                                }

                                span.Size = style.Size;
                                span.Flags = style.Flags;
                                span.Font = style.Font;
                                span.Color = style.Color;
                                span.Asc = asc;
                                span.Desc = desc;

                                oldStyle = new MuPDFCharStyle(style);
                                spanRect = r;
                                spanOrigin = origin;
                            }
                            spanRect = FzRect.fz_union_rect(spanRect, r);

                            if (raw)
                            {
                                Char charDict = new Char();
                                charDict.Origin = new FzPoint(ch.origin);
                                charDict.Bbox = r;
                                charDict.C = (char)ch.c;

                                if (charList == null)
                                    charList = new List<Char>();
                                charList.Add(charDict);
                            }
                            else
                            {
                                textBuffer.fz_append_rune(ch.c);
                            }
                        }

                        // all characters processed, now flush remaining span
                        if (span != null)
                        {
                            if (raw)
                            {
                                span.Chars = charList;
                            }
                            else
                            {
                                span.Text = Utils.EscapeStrFromBuffer(textBuffer);
                                textBuffer.fz_clear_buffer();
                            }
                            span.Origin = new Point(spanOrigin);
                            span.Bbox = new Rect(spanRect);

                            if (spanRect.fz_is_empty_rect() == 0)
                            {
                                spanList.Add(span);
                                lineRect = FzRect.fz_union_rect(lineRect, spanRect);
                            }
                            span = null;
                        }

                        lineDict.Spans = new List<Span>(spanList);

                        blockRect = FzRect.fz_union_rect(blockRect, lineRect);
                        lineDict.WMode = line.wmode;
                        lineDict.Dir = new Point(new FzPoint(line.dir));
                        lineDict.Bbox = new Rect(new FzRect(line.bbox));
                        lineList.Add(lineDict);
                    }
                    blockDict.Bbox = new Rect(blockRect);
                    blockDict.Lines = lineList;
                }
                pageDict.Blocks.Add(blockDict);
            }
        }

        /// <summary>
        /// Make flags according to font style or type
        /// </summary>
        /// <param name="font">source font</param>
        /// <param name="line">target line</param>
        /// <param name="ch">target char</param>
        /// <returns></returns>
        internal float CharFontFlags(FzFont font, FzStextLine line, FzStextChar ch)
        {
            float flags = DetectSuperScript(line, ch); // detect super string
            flags += font.fz_font_is_italic() * (int)FontStyle.TEXT_FONT_ITALIC;
            flags += font.fz_font_is_serif() * (int)FontStyle.TEXT_FONT_SERIFED;
            flags += font.fz_font_is_monospaced() * (int)FontStyle.TEXT_FONT_MONOSPACED;
            flags += font.fz_font_is_bold() * (int)FontStyle.TEXT_FONT_BOLD;
            return flags;
        }

        /// <summary>
        /// Detect super string
        /// </summary>
        /// <param name="line">target line</param>
        /// <param name="ch">target char</param>
        /// <returns></returns>
        internal float DetectSuperScript(FzStextLine line, FzStextChar ch)
        {
            if (
                line.m_internal.wmode == 0
                && line.m_internal.dir.x == 1
                && line.m_internal.dir.y == 0
            )
                return (
                    ch.m_internal.origin.y
                    < (line.m_internal.first_char.origin.y - ch.m_internal.size * 0.1)
                )
                    ? 1.0f
                    : 0.0f;
            return 0.0f;
        }

        /// <summary>
        /// Get the font name from FzFont object
        /// </summary>
        /// <param name="font"></param>
        /// <returns></returns>
        internal string GetFontName(FzFont font)
        {
            string name = font.fz_font_name();
            int s = name.IndexOf("+");

            return name.Substring(s + 1);
        }

        public FzQuad GetCharQuad(FzStextLine line, FzStextChar ch)
        {
            if (line.m_internal.wmode != 0)
                return new FzQuad(ch.m_internal.quad);

            FzFont font = new FzFont(mupdf.mupdf.ll_fz_keep_font(ch.m_internal.font));
            float asc = font.fz_font_ascender();
            float desc = font.fz_font_descender();
            float fontSize = ch.m_internal.size;
            float asc_desc = asc - desc + (float)Utils.FLT_EPSILON;

            if (asc_desc >= 1)
            {
                return new FzQuad(ch.m_internal.quad);
            }

            FzRect bbox = mupdf.mupdf.fz_font_bbox(font);
            float fontWidth = bbox.y1 - bbox.y0;

            if (asc < 1e-3)
            {
                desc = -0.1f;
                asc = 0.9f;
                asc_desc = 1.0f;
            }

            asc_desc = asc - desc;
            asc = asc * fontSize / asc_desc;
            desc = desc * fontSize / asc_desc;

            float c = line.m_internal.dir.x;
            float s = line.m_internal.dir.y;
            FzMatrix trm1 = new FzMatrix(c, -s, s, c, 0, 0);
            FzMatrix trm2 = new FzMatrix(c, s, -s, c, 0, 0);
            if (c == -1)
            {
                trm1.d = 1;
                trm2.d = 1;
            }
            FzMatrix xlate1 = new FzMatrix(
                1,
                0,
                0,
                1,
                -ch.m_internal.origin.x,
                -ch.m_internal.origin.y
            );
            FzMatrix xlate2 = new FzMatrix(
                1,
                0,
                0,
                1,
                ch.m_internal.origin.x,
                ch.m_internal.origin.y
            );

            FzQuad quad = mupdf.mupdf.fz_transform_quad(new FzQuad(ch.m_internal.quad), xlate1);
            quad = mupdf.mupdf.fz_transform_quad(quad, trm1);

            if (c == 1 && quad.ul.y > 0)
            {
                quad.ul.y = asc;
                quad.ur.y = asc;
                quad.ll.y = desc;
                quad.lr.y = desc;
            }
            else
            {
                quad.ul.y = -asc;
                quad.ur.y = -asc;
                quad.ll.y = -desc;
                quad.lr.y = -desc;
            }

            if (quad.ll.x < 0)
            {
                quad.ll.x = 0;
                quad.ul.x = 0;
            }

            float cwidth = quad.lr.x - quad.ll.x;
            if (cwidth < Utils.FLT_EPSILON)
            {
                int glyph = mupdf.mupdf.fz_encode_character(font, ch.m_internal.c);
                if (glyph != 0)
                {
                    fontWidth = mupdf.mupdf.fz_advance_glyph(font, glyph, line.m_internal.wmode);
                    quad.lr.x = quad.ll.x + fontWidth * fontSize;
                    quad.ur.x = quad.lr.x;
                }
            }
            quad = mupdf.mupdf.fz_transform_quad(quad, trm2);
            quad = mupdf.mupdf.fz_transform_quad(quad, xlate2);

            return quad;
        }

        public FzRect GetCharBbox(FzStextLine line, FzStextChar ch)
        {
            FzQuad q = GetCharQuad(line, ch);
            FzRect r = q.fz_rect_from_quad();
            if (line.m_internal.wmode != 0)
                return r;
            if (r.y1 < r.y0 + ch.m_internal.size)
                r.y0 = r.y1 - ch.m_internal.size;

            return r;
        }

        public bool IsRectsOverlap(FzRect a, FzRect b)
        {
            if (false || a.x0 >= b.x1 || a.y0 >= b.y1 || a.x1 <= b.x0 || a.y1 <= b.y0)
                return false;
            return true;
        }
    }

    public class MuPDFCharStyle
    {
        public float Size { get; set; }

        public float Flags { get; set; }

        public string Font { get; set; }

        public int Color { get; set; }

        public float Asc { get; set; }

        public float Desc { get; set; }

        public MuPDFCharStyle(Dictionary<string, dynamic> rhs)
        {
            Size = rhs["Size"];
            Flags = rhs["Flags"];
            Font = rhs["Font"];
            Color = rhs["Color"];
            Asc = rhs["Asc"];
            Desc = rhs["Desc"];
        }

        public MuPDFCharStyle(MuPDFCharStyle rhs)
        {
            Size = rhs.Size;
            Flags = rhs.Flags;
            Font = rhs.Font;
            Color = rhs.Color;
            Asc = rhs.Asc;
            Desc = rhs.Desc;
        }

        public MuPDFCharStyle()
        {
            Size = -1;
            Flags = -1;
            Font = "";
            Color = -1;
            Asc = 0;
            Desc = 0;
        }

        public override string ToString()
        {
            return $"{Size} {Flags} {Font} {Color} {Asc} {Desc}";
        }
    }
}
