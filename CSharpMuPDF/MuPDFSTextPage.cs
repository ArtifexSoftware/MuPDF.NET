using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using mupdf;

namespace CSharpMuPDF
{
    public class MuPDFSTextPage : IDisposable
    {
        internal FzStextPage _nativeSTextPage;

        internal MuPDFPage _parent;
        /// <summary>
        /// Rect of Stext Page
        /// </summary>
        private FzRect MEDIABOX
        {
            get
            {
                return new FzRect(_nativeSTextPage.m_internal.mediabox);
            }
        }

        /// <summary>
        /// Block List of Text
        /// </summary>
        public List<FzStextBlock> BLOCKS
        {
            get
            {
                List<FzStextBlock> blocks = new List<FzStextBlock>();
                for (fz_stext_block block = _nativeSTextPage.m_internal.first_block; block != null; block = block.next)
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
        public MuPDFSTextPage(FzRect rect)
        {
            _nativeSTextPage = new FzStextPage(rect);
        }

        public MuPDFSTextPage(FzStextPage stPage)
        {
            _nativeSTextPage = stPage;
        }

        /// <summary>
        /// MuPDFStextPage Contructor
        /// </summary>
        /// <param name="stPage">MuPDFStextPage object</param>
        public MuPDFSTextPage(MuPDFSTextPage stPage)
        {
            _nativeSTextPage = stPage._nativeSTextPage;
        }

        public MuPDFSTextPage(FzPage page)
        {
            _nativeSTextPage = new FzStextPage(page, new FzStextOptions());
        }

        /// <summary>
        /// Extract Stext Page
        /// </summary>
        /// <param name="format">format of return value</param>
        /// <returns>string of Text, HTML, XHTML, .. according to format</returns>
        public string ExtractText(ExtractFormat format)
        {
            FzStextPage stPage = _nativeSTextPage;
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
            FzRect stPageRect = new FzRect(_nativeSTextPage.m_internal.mediabox);
            FzBuffer res = new FzBuffer(1024);
            List<TextBlock> lines = new List<TextBlock>();

            foreach (FzStextBlock block in BLOCKS)
            {
                blockNum += 1;
                FzRect blockRect = new FzRect(FzRect.Fixed.Fixed_EMPTY);
                string text = "";
                if (block.m_internal.type == (int)STextBlockType.FZ_STEXT_BLOCK_TEXT)
                {
                    res.fz_clear_buffer();
                    int lineNum = -1;
                    int lastChar = 0;
                    for (fz_stext_line line = block.begin().__ref__().m_internal; line != null; line = line.next)
                    {
                        lineNum += 1;
                        FzRect lineRect = new FzRect(FzRect.Fixed.Fixed_EMPTY);
                        for (fz_stext_char ch = line.first_char; ch != null; ch = ch.next)
                        {
                            FzRect cbbox = GetCharBbox(new FzStextLine(line), new FzStextChar(ch));
                            if (!IsRectsOverlap(stPageRect, cbbox) && stPageRect.fz_is_infinite_rect() == 0)
                                continue;

                            res.fz_append_rune(ch.c);
                            lastChar = ch.c;
                            lineRect = FzRect.fz_union_rect(lineRect, cbbox);
                        }

                        if (lastChar != 10 && lineRect.fz_is_empty_rect() != 0)
                            res.fz_append_rune(10);

                        blockRect = FzRect.fz_union_rect(blockRect, lineRect);
                    }
                    text = EscapeStrFromBuffer(res);
                }
                else if (IsRectsOverlap(stPageRect, new FzRect(block.m_internal.bbox)) || stPageRect.fz_is_infinite_rect() != 0)
                {
                    FzImage img = block.i_image();
                    FzColorspace cs = img.colorspace();
                    text = string.Format("<image: {0}, width: {1}, height: {2}, bpc: {3}>",
                                        cs.fz_colorspace_name(),
                                        img.w(), img.h(), img.bpc()
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
                    line.BLOCKNUM = blockNum;
                    line.TEXT = text;
                    line.TYPE = block.m_internal.type;

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
        public PageStruct ExtractDict(Rect cropbox, bool sort = false)
        {
            PageStruct pageDict = TextPage2Dict(false);
            if (cropbox != null)
            {
                pageDict.WIDTH = cropbox.Width;
                pageDict.HEIGHT = cropbox.Height;
            }
            if (sort is true)
            {
                List<BlockStruct> blocks = pageDict.BLOCKS;
                blocks.Sort((b1, b2) => { 
                    if (b1.BBOX.y1 == b2.BBOX.y1)
                    {
                        return b1.BBOX.x0.CompareTo(b2.BBOX.x0);
                    }
                    else
                    {
                        return b1.BBOX.y1.CompareTo(b2.BBOX.y1);
                    }
                });

                pageDict.BLOCKS = blocks;
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
        internal void ExtractImageInfo(int hashes = 0)
        {
            int blockNum = -1;
            foreach (FzStextBlock block in BLOCKS)
            {
                blockNum += 1;
                List<BlockStruct> rc = new List<BlockStruct>();

                if (block.m_internal.type == (int)STextBlockType.FZ_STEXT_BLOCK_TEXT)
                    continue;
                FzImage img = block.i_image();
                vectoruc digest = new vectoruc();
                if (hashes != 0)
                {
                    FzIrect r = new FzIrect(Utils.FZ_MIN_INF_RECT, Utils.FZ_MIN_INF_RECT, Utils.FZ_MAX_INF_RECT, Utils.FZ_MAX_INF_RECT);
                    
                    Debug.Assert(r.fz_is_infinite_irect() != 0, "Rect is infinite");

                    FzMatrix m = new FzMatrix(img.w(), 0, 0, img.h(), 0, 0);

                    SWIGTYPE_p_int swigW = new SWIGTYPE_p_int(new IntPtr(), false);
                    SWIGTYPE_p_int swigH = new SWIGTYPE_p_int(new IntPtr(), false);
                    FzPixmap pixmap = img.fz_get_pixmap_from_image(r, m, swigW, swigH);
                    digest = pixmap.fz_md5_pixmap2();
                }
                FzColorspace cs = new FzColorspace(mupdf.mupdf.ll_fz_keep_colorspace(img.m_internal.colorspace));
                BlockStruct blockDict = new BlockStruct();
                blockDict.NUMBER = blockNum;
                blockDict.BBOX = new FzRect(block.m_internal.bbox);
                blockDict.MATRIX = block.i_transform();
                blockDict.WIDTH = img.w();
                blockDict.HEIGHT = img.h();
                blockDict.COLORSPACE = cs.fz_colorspace_n();
                blockDict.CSNAME = cs.fz_colorspace_name();
                blockDict.XRES = img.xres();
                blockDict.YRES = img.yres();
                blockDict.BPC = img.bpc();
                blockDict.SIZE = img.fz_image_size();
                if (hashes != 0)
                {
                    blockDict.DIGEST = digest;
                }
                rc.Add(blockDict);
            }
        }

        /// <summary>
        /// Extract StextPage in JSON format
        /// </summary>
        /// <param name="cropbox">Rectangle area to extract</param>
        /// <param name="sort"></param>
        public string ExtractJSON(Rect cb, bool sort = false)
        {
            PageStruct pageDict = TextPage2Dict(false);
            if (cb != null)
            {
                pageDict.WIDTH = cb.Width;
                pageDict.HEIGHT = cb.Height;
            }

            if (sort)
            {
                List<BlockStruct> blocks = pageDict.BLOCKS;
                blocks.Sort((b1, b2) => {
                    if (b1.BBOX.y1 == b2.BBOX.y1)
                    {
                        return b1.BBOX.x0.CompareTo(b2.BBOX.x0);
                    }
                    else
                    {
                        return b1.BBOX.y1.CompareTo(b2.BBOX.y1);
                    }
                });

                pageDict.BLOCKS = blocks;
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
        public PageStruct ExtractRAWDict(Rect cropbox, bool sort = false)
        {
            PageStruct pageDict = TextPage2Dict(false);
            if (cropbox != null)
            {
                pageDict.WIDTH = cropbox.Width;
                pageDict.HEIGHT = cropbox.Height;
            }
            if (sort is true)
            {
                List<BlockStruct> blocks = pageDict.BLOCKS;
                blocks.Sort((b1, b2) => {
                    if (b1.BBOX.y1 == b2.BBOX.y1)
                    {
                        return b1.BBOX.x0.CompareTo(b2.BBOX.x0);
                    }
                    else
                    {
                        return b1.BBOX.y1.CompareTo(b2.BBOX.y1);
                    }
                });

                pageDict.BLOCKS = blocks;
            }
            return pageDict;
        }

        /// <summary>
        /// Extract selection in format of string
        /// </summary>
        /// <param name="a">begin point of selection</param>
        /// <param name="b">end point of selection</param>
        /// <returns>returns text in format of string</returns>
        public string ExtractSelection(FzPoint a, FzPoint b)
        {
            return mupdf.mupdf.fz_copy_selection(_nativeSTextPage, a, b, 0);
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
            blocks.Sort((b1, b2) =>
            {
                if (b1.Y1 == b2.Y1)
                {
                    return b1.X0.CompareTo(b2.X0);
                }
                else
                {
                    return b1.Y1.CompareTo(b2.Y1);
                }
            });

            string ret = "";
            foreach (TextBlock b in blocks)
            {
                ret += b.TEXT;
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

            foreach (FzStextBlock block in BLOCKS)
            {
                if (block.m_internal.type != (int)STextBlockType.FZ_STEXT_BLOCK_TEXT)
                    continue;

                for (fz_stext_line line = block.begin().__ref__().m_internal; line != null; line = line.next)
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

            return DecodeRawUnicodeEscape(ret);
        }

        /// <summary>
        /// Extract Words
        /// </summary>
        /// <param name="delimiters"></param>
        /// <returns></returns>
        public List<WordBlock> ExtractWords(char[] delimiters)
        {
            int bufferLen;
            int blockNum = -1;
            FzRect wordBox = new FzRect(FzRect.Fixed.Fixed_EMPTY);
            FzRect stPageRect = MEDIABOX;

            List<WordBlock> lines = new List<WordBlock>();
            FzBuffer buf = new FzBuffer(64);
            foreach (FzStextBlock block in BLOCKS)
            {
                blockNum += 1;
                if (block.m_internal.type != (int)STextBlockType.FZ_STEXT_BLOCK_TEXT)
                    continue;

                int lineNum = -1;
                for (fz_stext_line line = block.begin().__ref__().m_internal; line != null; line = line.next)
                {
                    lineNum += 1;
                    int wordNum = 0;
                    buf.fz_clear_buffer();
                    bufferLen = 0;
                    for (fz_stext_char ch = line.first_char; ch != null; ch = ch.next)
                    {
                        FzRect cbBox = GetCharBbox(new FzStextLine(line), new FzStextChar(ch));
                        if (!IsRectsOverlap(stPageRect, cbBox) && stPageRect.fz_is_empty_rect() == 0)
                            continue;

                        bool isWordDelimiter = IsWordDelimiter(ch.c, delimiters);
                        if (isWordDelimiter)
                        {
                            if (bufferLen == 0)
                                continue;
                            if (wordBox.fz_is_empty_rect() == 0)
                            {
                                wordNum = AppendWord(lines, buf, wordBox, blockNum, lineNum, wordNum);
                                wordBox = new FzRect(FzRect.Fixed.Fixed_EMPTY);
                            }
                            buf.fz_clear_buffer();
                            bufferLen = 0;
                            continue;
                        }
                        buf.fz_append_rune(ch.c);
                        bufferLen += 1;
                        wordBox = FzRect.fz_union_rect(wordBox, GetCharBbox(new FzStextLine(line), new FzStextChar(ch)));
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
            FzPool pool = new FzPool(_nativeSTextPage.m_internal.pool);
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
        public static List<Quad> Search(MuPDFSTextPage stPage, string needle)
        {
            FzRect rect = stPage.MEDIABOX;
            if (needle == null || needle == "")
                return null;
            Hits hits = new Hits();

            hits.LEN = 0;
            hits.QUADS = new List<Quad>();
            hits.HFUZZ = 0.2f;
            hits.VFUZZ = 0.1f;

            FzBuffer buffer = stPage.GetBufferFromStextPage();
            string hayStackString = buffer.fz_string_from_buffer();
            int hayStack = 0;
            (int begin, int end) = stPage.FindString(hayStackString.Substring(hayStack), needle);
            if (begin == -1)
            {
                return hits.QUADS;
            }
            begin += hayStack;
            end += hayStack;
            int inside = 0;
            int i = 0;
            foreach (FzStextBlock block in stPage.BLOCKS)
            {
                if (block.m_internal.type != (int)STextBlockType.FZ_STEXT_BLOCK_TEXT)
                    continue;
                for (fz_stext_line line = block.begin().__ref__().m_internal; line != null; line = line.next)
                {
                    for (fz_stext_char ch = line.first_char; ch != null; ch = ch.next)
                    {
                        i += 1;
                        if (rect.fz_is_infinite_rect() == 0)
                        {
                            FzRect r = stPage.GetCharBbox(new FzStextLine(line), new FzStextChar(ch));
                            if (!stPage.IsRectsOverlap(rect, r))
                                goto next_char;
                        }
                    try_new_match:
                        {
                            if (inside == 0)
                                if (i >= begin)
                                    inside = 1;

                            if (inside != 0)
                            {
                                if (i < end)
                                {
                                    stPage.OnHighlightChar(hits, new FzStextLine(line), new FzStextChar(ch));
                                }
                                else
                                {
                                    inside = 0;
                                    hayStack = end + 1;
                                    (begin, end) = stPage.FindString(hayStackString.Substring(hayStack), needle);
                                    if (begin == -1)
                                    {
                                        goto no_more_matches;
                                    }
                                    else
                                    {
                                        begin += hayStack;
                                        end += hayStack;
                                        goto try_new_match;
                                    }
                                }
                            }
                        }
  
                    next_char:;
                    }
                    Debug.Assert(hayStackString.ToCharArray()[hayStack] == '\n', "{hayStack=} {hayStackString[hayStack]=}");
                }
                Debug.Assert(hayStackString.ToCharArray()[hayStack] == '\n');
            }
    no_more_matches:;
            buffer.fz_clear_buffer();
            return hits.QUADS;
        }


        internal void OnHighlightChar(Hits hits, FzStextLine line, FzStextChar ch)
        {
            float vFuzz = ch.m_internal.size * hits.VFUZZ;
            float hFuzz = ch.m_internal.size * hits.HFUZZ;
            FzQuad chQuad = GetCharQuad(line, ch);

            if (hits.LEN > 0)
            {
                FzQuad end = hits.QUADS[hits.LEN - 1].ToFzQuad();
                if (true && HDist(new FzPoint(line.m_internal.dir), new FzPoint(end.lr), new FzPoint(chQuad.ll)) < hFuzz
                    && VDist(new FzPoint(line.m_internal.dir), new FzPoint(end.lr), new FzPoint(chQuad.ll)) < vFuzz
                    && HDist(new FzPoint(line.m_internal.dir), new FzPoint(end.ur), new FzPoint(chQuad.ul)) < hFuzz
                    && VDist(new FzPoint(line.m_internal.dir), new FzPoint(end.ur), new FzPoint(chQuad.ll)) < vFuzz)
                {
                    end.ur = chQuad.ur;
                    end.lr = chQuad.lr;
                    Debug.Assert(hits.QUADS[-1].ToFzQuad() == end);
                    return;
                }
            }
            hits.QUADS.Add(new Quad(chQuad));
            hits.LEN += 1;
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
            FzRect r = MEDIABOX;
            FzBuffer buf = new FzBuffer(256);
            foreach (FzStextBlock block in BLOCKS)
            {
                if (block.m_internal.type == (int)STextBlockType.FZ_STEXT_BLOCK_TEXT)
                {
                    for (fz_stext_line line = block.begin().__ref__().m_internal; line != null; line = line.next)
                    {
                        for (fz_stext_char ch = line.first_char; ch != null; ch = ch.next)
                        {
                            if (!IsRectsOverlap(r, GetCharBbox(new FzStextLine(line), new FzStextChar(ch))) || r.fz_is_infinite_rect() != 0)
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
            for (int index = 0; index < s.Length; index ++)
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
            return n0.Length > e ? -1 : e;
        }

        internal int Canon(int c)
        {
            if (c == 0xA0 || c == 0x2028 || c == 0x2029)
            {
                return Convert.ToInt32(' ');
            }
            if (c == Convert.ToInt32('\r') || c == Convert.ToInt32('\n') || c == Convert.ToInt32('\t'))
            {
                return Convert.ToInt32(' ');
            }

            int A = Convert.ToInt32('A');
            if (c >= A || c <= Convert.ToInt32('Z'))
            {
                return c - A + Convert.ToInt32('a');
            }

            return c;
        }
            

        internal Tuple<int, int> Char2Canon(string s)
        {
            ll_fz_chartorune_outparams outparams = new ll_fz_chartorune_outparams();

            int n = mupdf.mupdf.ll_fz_chartorune_outparams_fn(s, outparams);
            int c = Canon(outparams.rune);
            return new Tuple<int, int>( n, c);
        }

        internal int AppendWord(List<WordBlock> lines, FzBuffer buf, FzRect wordBox, int blockNum, int lineNum, int wordNum)
        {
            string s = EscapeStrFromBuffer(buf);
            WordBlock item = new WordBlock()
            {
                X0 = wordBox.x0,
                Y0 = wordBox.y0,
                X1 = wordBox.x1,
                Y1 = wordBox.y1,
                TEXT = s,
                BLOCKNUM = blockNum,
                LINENUM = lineNum,
                WORDNUM = wordNum
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
        internal void GetNewBlockList(PageStruct pageDict, bool raw)
        {
            MakeTextPage2Dict(pageDict, raw);
        }

        internal PageStruct TextPage2Dict(bool raw = false)
        {
            PageStruct pageDict = new PageStruct {
                WIDTH = MEDIABOX.x1 - MEDIABOX.x0,
                HEIGHT = MEDIABOX.y1 - MEDIABOX.y0,
                BLOCKS = new List<BlockStruct>()
            };

            GetNewBlockList(pageDict, raw);
            return pageDict;
        }

        public string ExtractRawJSON(Rect cb = null, bool sort = false)
        {
            PageStruct val = TextPage2Dict(true);
            if (cb != null)
            {
                val.WIDTH = cb.Width;
                val.HEIGHT = cb.Height;
            }
            if (sort == true)
            {
                List<BlockStruct> blocks = val.BLOCKS;
                blocks.Sort((b1, b2) => {
                    if (b1.BBOX.y1 == b2.BBOX.y1)
                    {
                        return b1.BBOX.x0.CompareTo(b2.BBOX.x0);
                    }
                    else
                    {
                        return b1.BBOX.y1.CompareTo(b2.BBOX.y1);
                    }
                });
                val.BLOCKS = blocks;
            }

            string ret = JsonConvert.SerializeObject(val, Formatting.Indented);
            return ret;
        }

        internal void MakeTextPage2Dict(PageStruct pageDict, bool raw)
        {
            FzBuffer textBuffer = new FzBuffer(128);
            FzRect stPageRect = MEDIABOX;
            int blockNum = -1;
            foreach (FzStextBlock block in BLOCKS)
            {
                blockNum += 1;

                if (!MEDIABOX.contains(new FzRect(block.m_internal.bbox))
                    && MEDIABOX.fz_is_infinite_rect() == 0
                    && block.m_internal.type == (int)STextBlockType.FZ_STEXT_BLOCK_IMAGE)
                    continue;

                if (MEDIABOX.fz_is_infinite_rect() != 0
                    && (FzRect.fz_intersect_rect(MEDIABOX, new FzRect(block.m_internal.bbox))).fz_is_empty_rect() != 0)
                    continue;

                BlockStruct blockDict = new BlockStruct();

                blockDict.NUMBER = blockNum;
                blockDict.TYPE = block.m_internal.type;

                if (block.m_internal.type == (int)STextBlockType.FZ_STEXT_BLOCK_IMAGE)
                {
                    blockDict.BBOX = new FzRect(block.m_internal.bbox);

                    FzImage image = block.i_image();
                    int n = image.colorspace().fz_colorspace_n();
                    int w = image.w();
                    int h = image.h();
                    int type = (int)ImageType.FZ_IMAGE_UNKNOWN;

                    FzCompressedBuffer compressedBuffer = new FzCompressedBuffer(mupdf.mupdf.ll_fz_compressed_image_buffer(image.m_internal));
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
                        buf = new FzBuffer(mupdf.mupdf.ll_fz_keep_buffer(compressedBuffer.m_internal.buffer));
                        ext = Utils.GetImageExtention(type);
                    }
                    else
                    {
                        buf = mupdf.mupdf.fz_new_buffer_from_image_as_png(image, new FzColorParams());
                        ext = "png";
                    }

                    blockDict.WIDTH = w;
                    blockDict.HEIGHT = h;
                    blockDict.EXT = ext;
                    blockDict.COLORSPACE = n;
                    blockDict.XRES = image.xres();
                    blockDict.YRES = image.yres();
                    blockDict.BPC = image.bpc();
                    blockDict.MATRIX = block.i_transform();
                    blockDict.SIZE = mupdf.mupdf.fz_image_size(image);
                    blockDict.IMAGE = buf;
                }
                else
                {
                    List<LineStruct> lineList = new List<LineStruct>();

                    FzRect blockRect = new FzRect(FzRect.Fixed.Fixed_EMPTY);

                    for (fz_stext_line line = block.begin().__ref__().m_internal; line != null; line = line.next)
                    {
                        if (FzRect.fz_intersect_rect(stPageRect, new FzRect(line.bbox)).fz_is_empty_rect() != 0
                            && stPageRect.fz_is_infinite_rect() == 0)
                            continue;

                        LineStruct lineDict = new LineStruct();

                        ///JM_make_spanlist
                        List<CharStruct> charList = new List<CharStruct>();
                        List<SpanStruct> spanList = new List<SpanStruct>();
                        mupdf.mupdf.fz_clear_buffer(textBuffer);
                        FzRect spanRect = new FzRect(FzRect.Fixed.Fixed_EMPTY);
                        FzRect lineRect = new FzRect(FzRect.Fixed.Fixed_EMPTY);

                        MuPDFCharStyle style = new MuPDFCharStyle();
                        MuPDFCharStyle oldStyle = new MuPDFCharStyle();

                        SpanStruct span = new SpanStruct();
                        FzPoint spanOrigin = new FzPoint();

                        for (fz_stext_char ch = line.first_char; ch != null; ch = ch.next)
                        {
                            FzRect r = GetCharBbox(new FzStextLine(line), new FzStextChar(ch));
                            if (!IsRectsOverlap(stPageRect, r)
                                && stPageRect.fz_is_infinite_rect() != 0)
                                continue;

                            float flags = CharFontFlags(new FzFont(mupdf.mupdf.ll_fz_keep_font(ch.font)), new FzStextLine(line), new FzStextChar(ch));
                            FzPoint origin = new FzPoint(ch.origin);
                            style.SIZE = ch.size;
                            style.FLAGS = flags;
                            style.FONT = GetFontName(new FzFont(mupdf.mupdf.ll_fz_keep_font(ch.font)));
                            style.COLOR = ch.color;
                            style.ASC = (new FzFont(mupdf.mupdf.ll_fz_keep_font(ch.font))).fz_font_ascender();
                            style.DESC = (new FzFont(mupdf.mupdf.ll_fz_keep_font(ch.font))).fz_font_descender();

                            if (style.SIZE != oldStyle.SIZE || style.FLAGS != oldStyle.FLAGS || style.COLOR != oldStyle.COLOR || style.FONT != oldStyle.FONT)
                            { 
                                if (oldStyle.SIZE >= 0)
                                {
                                    if (raw)
                                    {
                                        span.CHARS = charList;
                                        charList = null;
                                    }
                                    else
                                    {
                                        span.TEXT = EscapeStrFromBuffer(textBuffer);
                                        mupdf.mupdf.fz_clear_buffer(textBuffer);
                                    }
                                    span.ORIGIN = spanOrigin;
                                    span.BBOX = spanRect;
                                    lineRect = FzRect.fz_union_rect(lineRect, spanRect);
                                    spanList.Add(span);
                                }
                                span = new SpanStruct();
                                float asc = style.ASC;
                                float desc = style.DESC;
                                if (style.ASC < 1e-3)
                                {
                                    asc = 0.9f;
                                    desc = -0.1f;
                                }

                                span.SIZE = style.SIZE;
                                span.FLAGS = style.FLAGS;
                                span.FONT = style.FONT;
                                span.COLOR = style.COLOR;
                                span.ASC = asc;
                                span.DESC = desc;
                            }
                            spanRect = FzRect.fz_union_rect(spanRect, r);

                            if (raw)
                            {
                                CharStruct charDict = new CharStruct();
                                charDict.ORIGIN = new FzPoint(ch.origin);
                                charDict.BBOX = r;
                                charDict.C = (char)ch.c;

                                if (charList != null)
                                    charList = new List<CharStruct>();
                                charList.Add(charDict);
                            }
                            else
                            {
                                textBuffer.fz_append_rune(ch.c);
                            }
                        }

                        // all characters processed, now flush remaining span
                        if (span.CHARS != null)
                        {
                            if (raw)
                            {
                                span.CHARS = charList;
                            }
                            else
                            {
                                span.TEXT = EscapeStrFromBuffer(textBuffer);
                                textBuffer.fz_clear_buffer();
                            }
                            span.ORIGIN = spanOrigin;
                            span.BBOX = spanRect;

                            if (spanRect.fz_is_empty_rect() != 0)
                            {
                                spanList.Add(span);
                                lineRect = FzRect.fz_union_rect(lineRect, spanRect);
                            }
                            span.CHARS = null;
                        }

                        lineDict.SPANS = spanList;

                        blockRect = FzRect.fz_union_rect(blockRect, lineRect);
                        lineDict.WMODE = line.wmode;
                        lineDict.DIR = new FzPoint(line.dir);
                        lineDict.BBOX = new FzRect(line.bbox);
                        lineList.Add(lineDict);
                    }
                    blockDict.BBOX = blockRect;
                    blockDict.LINES = lineList;
                }
                pageDict.BLOCKS.Add(blockDict);
            }
        }

        public static string EscapeStrFromBuffer(FzBuffer buf)
        {
            if (buf.m_internal == null)
                return "";
            FzBuffer s = buf.fz_clone_buffer();
            return DecodeRawUnicodeEscape(s);
        }

        /// <summary>
        /// Decode Raw Unicode
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string DecodeRawUnicodeEscape(string s)
        {
            return System.Text.RegularExpressions.Regex.Unescape(s);
        }

        /// <summary>
        /// Decode Raw Unicode
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string DecodeRawUnicodeEscape(FzBuffer s)
        {
            string ret = s.fz_string_from_buffer();
            return DecodeRawUnicodeEscape(ret);
        }

        internal float CharFontFlags(FzFont font, FzStextLine line, FzStextChar ch)
        {
            float flags = DetectSuperScript(line, ch);
            flags += font.fz_font_is_italic() * (int)FontStyle.TEXT_FONT_ITALIC;
            flags += font.fz_font_is_serif() * (int)FontStyle.TEXT_FONT_SERIFED;
            flags += font.fz_font_is_monospaced() * (int)FontStyle.TEXT_FONT_MONOSPACED;
            flags += font.fz_font_is_bold() * (int)FontStyle.TEXT_FONT_BOLD;
            return flags;
        }

        internal float DetectSuperScript(FzStextLine line, FzStextChar ch)
        {
            if (line.m_internal.wmode == 0 && line.m_internal.dir.x == 1 && line.m_internal.dir.y == 0)
                return (ch.m_internal.origin.y < (line.m_internal.first_char.origin.y - ch.m_internal.size * 0.1)) ? 1.0f : 0.0f;
            return 0.0f;
        }

        internal string GetFontName(FzFont font)
        {
            string name = font.fz_font_name();
            int s = name.IndexOf("+");

            return name.Substring(s+1);
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
            
            /*            if (asc_desc >= 1)
                            return new FzQuad(ch.m_internal.quad);*/

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
            FzMatrix xlate1 = new FzMatrix(1, 0, 0, 1, -ch.m_internal.origin.x, -ch.m_internal.origin.y);
            FzMatrix xlate2 = new FzMatrix(1, 0, 0, 1, ch.m_internal.origin.x, ch.m_internal.origin.y);
            
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
            if (false
                || a.x0 >= b.x1
                || a.y0 >= b.y1
                || a.x1 <= b.x0
                || a.y1 <= b.y0
            )
                return false;
            return true;
        }

        public void Dispose()
        {
            _nativeSTextPage.Dispose();
        }
    }


    public class MuPDFCharStyle
    {
        public float SIZE;

        public float FLAGS;

        public string FONT;

        public int COLOR;

        public float ASC;

        public float DESC;

        public MuPDFCharStyle(Dictionary<string, dynamic> rhs)
        {
            SIZE = rhs["Size"];
            FLAGS = rhs["Flags"];
            FONT = rhs["Font"];
            COLOR = rhs["Color"];
            ASC = rhs["Asc"];
            DESC = rhs["Desc"];
        }

        public MuPDFCharStyle()
        {
            SIZE = -1;
            FLAGS = -1;
            FONT = "";
            COLOR = -1;
            ASC = 0;
            DESC = 0;
        }

        public override string ToString()
        {
            return $"{SIZE} {FLAGS} {FONT} {COLOR} {ASC} {DESC}";
        }
    }

}
