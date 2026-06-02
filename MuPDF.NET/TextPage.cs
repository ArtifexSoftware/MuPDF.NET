using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace MuPDF.NET
{
    /// <summary>
    /// Text and images on a document page (PyMuPDF <c>TextPage</c>).
    /// </summary>
    /// <remarks>
    /// <para>Create with <see cref="Page.GetTextPage"/> or <see cref="DisplayList.GetTextPage"/>.
    /// Most callers use <see cref="Page"/> helpers (<c>GetText</c>, <see cref="Page.SearchFor"/>) instead of calling this class directly.</para>
    /// <para>Structured output uses blocks → lines → spans (and per-character detail in raw mode).
    /// See <see cref="ExtractDict"/>, <see cref="ExtractRawDict"/>, and typed <see cref="PageInfo"/>.</para>
    /// </remarks>
    public partial class TextPage : IDisposable
    {
        private mupdf.FzStextPage _nativeStp;
        private bool _disposed;
        /// <summary>Cached block views; native memory is owned by <see cref="_nativeStp"/>.</summary>
        private List<mupdf.FzStextBlock> _stextBlocks;
        internal Page Parent { get; set; }
        public bool ThisOwn { get; set; } = true;

        internal mupdf.FzStextPage NativeStextPage
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(TextPage));
                return _nativeStp;
            }
        }

        public TextPage(mupdf.FzStextPage stp)
        {
            _nativeStp = stp;
        }

        /// <summary>Creates an empty text page with the given mediabox.</summary>
        /// <param name="rect">Page or clip rectangle.</param>
        public TextPage(mupdf.FzRect rect)
        {
            _nativeStp = new mupdf.FzStextPage(rect);
            Parent = null;
        }

        /// <summary>Shares the native text page with another instance (non-owning copy).</summary>
        public TextPage(TextPage stPage)
        {
            if (stPage == null)
                throw new ArgumentNullException(nameof(stPage));
            _nativeStp = stPage._nativeStp;
            Parent = stPage.Parent;
            ThisOwn = false;
        }

        /// <summary>
        /// Clip rectangle for this text page (page mediabox or <see cref="Page.GetTextPage"/> clip).
        /// </summary>
        /// <remarks>Search and most extractions respect this rectangle; HTML/XML still reflect full page content.</remarks>
        public Rect Rect
        {
            get
            {
                var r = _nativeStp.m_internal.mediabox;
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
        }

        // ─── Text Extraction ────────────────────────────────────────────

        /// <summary>
        /// Plain UTF-8 text in document order (Page <c>GetText("text")</c>).
        /// </summary>
        /// <param name="sort">When true, order blocks by vertical then horizontal position for natural reading order.</param>
        public string ExtractText(bool sort = false)
        {
            if (!sort)
            {
                // res = mupdf.fz_new_buffer(1024)
                using var res = mupdf.mupdf.fz_new_buffer(1024);
                // JM_print_stext_page_as_text(res, this_tpage)
                JM_print_stext_page_as_text(res, NativeStextPage);
                return Helpers.JmEscapeStrFromBuffer(res);
            }
            var blocks = ExtractBlockTuples();
            blocks.Sort((a, b) =>
            {
                int cmp = a.y1.CompareTo(b.y1);
                return cmp != 0 ? cmp : a.x0.CompareTo(b.x0);
            });
            var sb = new StringBuilder();
            foreach (var bl in blocks) sb.Append(bl.text);
            return sb.ToString();
        }

        /// <summary>
        /// Text block list (PyMuPDF <c>TextPage.extractBLOCKS</c>); tuples are <c>(x0, y0, x1, y1, text, block_no, block_type)</c>.
        /// Ported from <c>extra.i</c> <c>extractBLOCKS</c>.
        /// </summary>
        internal List<(float x0, float y0, float x1, float y1, string text, int blockNo, int blockType)> ExtractBlockTuples()
        {
            var lines = new List<(float, float, float, float, string, int, int)>();
            int block_n = -1;
            var tp_rect = new mupdf.FzRect(NativeStextPage.m_internal.mediabox);

            foreach (var block in StextBlocks)
            {
                block_n++;
                var blockrect = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);

                if (block.m_internal.type == mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                {
                    var res = mupdf.mupdf.fz_new_buffer(1024);
                    int last_char = 0;

                    for (mupdf.fz_stext_line line = FirstStextLine(block);
                         line != null;
                         line = line.next)
                    {
                        var linerect = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);

                        for (mupdf.fz_stext_char ch = line.first_char;
                             ch != null;
                             ch = ch.next)
                        {
                            var cbbox = Helpers.JM_char_bbox(line, ch);
                            if (!Helpers.JM_rects_overlap(tp_rect, cbbox)
                                && mupdf.mupdf.fz_is_infinite_rect(tp_rect) == 0)
                            {
                                continue;
                            }
                            Helpers.JmAppendRune(res, ch.c);
                            last_char = ch.c;
                            linerect = mupdf.mupdf.fz_union_rect(linerect, cbbox);
                        }
                        if (last_char != 10 && mupdf.mupdf.fz_is_empty_rect(linerect) == 0)
                        {
                            mupdf.mupdf.fz_append_byte(res, 10);
                        }
                        blockrect = mupdf.mupdf.fz_union_rect(blockrect, linerect);
                    }

                    if (mupdf.mupdf.fz_is_empty_rect(blockrect) == 0)
                    {
                        string text = Helpers.JmEscapeStrFromBuffer(res);
                        lines.Add((blockrect.x0, blockrect.y0, blockrect.x1, blockrect.y1,
                            text, block_n, block.m_internal.type));
                    }
                }
                else
                {
                    if (Helpers.JM_rects_overlap(tp_rect, new mupdf.FzRect(block.m_internal.bbox))
                        || mupdf.mupdf.fz_is_infinite_rect(tp_rect) != 0)
                    {
                        var img = block.i_image();
                        var cs = img.colorspace();
                        string text = $"<image: {mupdf.mupdf.fz_colorspace_name(cs)}, width: {img.w()}, height: {img.h()}, bpc: {img.bpc()}>";
                        blockrect = mupdf.mupdf.fz_union_rect(blockrect, new mupdf.FzRect(block.m_internal.bbox));
                        if (mupdf.mupdf.fz_is_empty_rect(blockrect) == 0)
                        {
                            lines.Add((blockrect.x0, blockrect.y0, blockrect.x1, blockrect.y1,
                                text, block_n, block.m_internal.type));
                        }
                    }
                }
            }
            return lines;
        }

        /// <summary>
        /// Word list (PyMuPDF <c>TextPage.extractWORDS</c>); tuples are <c>(x0, y0, x1, y1, word, block_no, line_no, word_no)</c>.
        /// Ported from <c>extra.i</c> <c>extractWORDS</c>.
        /// </summary>
        public List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> ExtractWordTuples(string delimiters = null)
        {
            var lines = new List<(float, float, float, float, string, int, int, int)>();
            int block_n = -1;
            var tp_rect = new mupdf.FzRect(NativeStextPage.m_internal.mediabox);
            var buff = mupdf.mupdf.fz_new_buffer(64);
            var wbbox = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);
            int buflen = 0;
            int last_char_rtl = 0;

            foreach (var block in StextBlocks)
            {
                block_n++;
                if (block.m_internal.type != mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                    continue;

                int line_n = -1;
                for (mupdf.fz_stext_line line = FirstStextLine(block);
                     line != null;
                     line = line.next)
                {
                    line_n++;
                    int word_n = 0;
                    mupdf.mupdf.fz_clear_buffer(buff);
                    buflen = 0;

                    for (mupdf.fz_stext_char ch = line.first_char;
                         ch != null;
                         ch = ch.next)
                    {
                        var cbbox = Helpers.JM_char_bbox(line, ch);
                        if (!Helpers.JM_rects_overlap(tp_rect, cbbox)
                            && mupdf.mupdf.fz_is_infinite_rect(tp_rect) == 0)
                        {
                            continue;
                        }
                        if (buflen == 0 && ch.c == 0x200d)
                            continue;

                        bool word_delimiter = JM_is_word_delimiter(ch.c, delimiters);
                        int this_char_rtl = JM_is_rtl_char(ch.c) ? 1 : 0;

                        if (word_delimiter || this_char_rtl != last_char_rtl)
                        {
                            if (buflen == 0 && word_delimiter)
                                continue;
                            if (mupdf.mupdf.fz_is_empty_rect(wbbox) == 0)
                            {
                                string w = Helpers.JmEscapeStrFromBuffer(buff);
                                lines.Add((wbbox.x0, wbbox.y0, wbbox.x1, wbbox.y1, w, block_n, line_n, word_n));
                                word_n++;
                                wbbox = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);
                            }
                            mupdf.mupdf.fz_clear_buffer(buff);
                            buflen = 0;
                            if (word_delimiter)
                                continue;
                        }
                        Helpers.JmAppendRune(buff, ch.c);
                        last_char_rtl = this_char_rtl;
                        buflen++;
                        wbbox = mupdf.mupdf.fz_union_rect(wbbox, cbbox);
                    }
                    if (buflen > 0 && mupdf.mupdf.fz_is_empty_rect(wbbox) == 0)
                    {
                        string w = Helpers.JmEscapeStrFromBuffer(buff);
                        lines.Add((wbbox.x0, wbbox.y0, wbbox.x1, wbbox.y1, w, block_n, line_n, word_n));
                        word_n++;
                        wbbox = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);
                    }
                    buflen = 0;
                }
            }
            return lines;
        }

        /// <summary>HTML with layout, fonts, and base64 images (Page <c>GetText("html")</c>).</summary>
        public string ExtractHtml()
        {
            var res = mupdf.mupdf.fz_new_buffer(1024);
            var output = new mupdf.FzOutput(res);
            mupdf.mupdf.fz_print_stext_page_as_html(output, NativeStextPage, 0);
            output.fz_close_output();
            return Encoding.UTF8.GetString(res.fz_buffer_extract());
        }

        /// <summary>XHTML with text and base64 images; no visual fidelity guarantee (Page <c>GetText("xhtml")</c>).</summary>
        public string ExtractXhtml()
        {
            var res = mupdf.mupdf.fz_new_buffer(1024);
            var output = new mupdf.FzOutput(res);
            mupdf.mupdf.fz_print_stext_page_as_xhtml(output, NativeStextPage, 0);
            output.fz_close_output();
            return Encoding.UTF8.GetString(res.fz_buffer_extract());
        }

        /// <summary>Legacy name for <see cref="ExtractXhtml"/>.</summary>
        public string ExtractXHtml() => ExtractXhtml();

        /// <summary>XML with per-character font and position detail; no images (Page <c>GetText("xml")</c>).</summary>
        public string ExtractXml()
        {
            var res = mupdf.mupdf.fz_new_buffer(1024);
            var output = new mupdf.FzOutput(res);
            mupdf.mupdf.fz_print_stext_page_as_xml(output, NativeStextPage, 0);
            output.fz_close_output();
            return Encoding.UTF8.GetString(res.fz_buffer_extract());
        }

        /// <summary>Legacy name for <see cref="ExtractXml"/>.</summary>
        public string ExtractXML() => ExtractXml();

        private void _getNewBlockList(Dictionary<string, object> page_dict, bool raw)
        {
            JM_make_textpage_dict(NativeStextPage, page_dict, raw);
        }

        private Dictionary<string, object> _textpage_dict(bool raw = false)
        {
            var page_dict = new Dictionary<string, object>
            {
                ["width"] = Rect.Width,
                ["height"] = Rect.Height,
            };
            _getNewBlockList(page_dict, raw);
            return page_dict;
        }

        /// <summary>
        /// Hierarchical page content: blocks, lines, spans with text (Page <c>GetText("dict")</c>).
        /// </summary>
        /// <param name="sort">Sort blocks by <c>(y1, x0)</c> when true.</param>
        /// <returns>Dictionary with <c>width</c>, <c>height</c>, and <c>blocks</c>; use <see cref="ToPageInfo"/> for <see cref="PageInfo"/>.</returns>
        public Dictionary<string, object> ExtractDict(bool sort = false)
        {
            var page_dict = _textpage_dict(raw: false);
            if (sort)
            {
                var block_list = (List<Dictionary<string, object>>)page_dict["blocks"];
                block_list.Sort((a, b) =>
                {
                    var ba = (float[])a["bbox"];
                    var bb = (float[])b["bbox"];
                    int cmp = ba[3].CompareTo(bb[3]);
                    return cmp != 0 ? cmp : ba[0].CompareTo(bb[0]);
                });
            }
            return page_dict;
        }

        /// <summary>
        /// Like <see cref="ExtractDict"/> with per-character <c>chars</c> on each span (Page <c>GetText("rawdict")</c>).
        /// </summary>
        /// <param name="sort">Sort blocks by <c>(y1, x0)</c> when true.</param>
        public Dictionary<string, object> ExtractRawDict(bool sort = false)
        {
            var page_dict = _textpage_dict(raw: true);
            if (sort)
            {
                var block_list = (List<Dictionary<string, object>>)page_dict["blocks"];
                block_list.Sort((a, b) =>
                {
                    var ba = (float[])a["bbox"];
                    var bb = (float[])b["bbox"];
                    int cmp = ba[3].CompareTo(bb[3]);
                    return cmp != 0 ? cmp : ba[0].CompareTo(bb[0]);
                });
            }
            return page_dict;
        }

        /// <summary>JSON for <see cref="ExtractDict"/> (Page <c>GetText("json")</c>); image bytes are base64 in JSON.</summary>
        /// <param name="sort">Passed to <see cref="ExtractDict"/>.</param>
        public string ExtractJson(bool sort = false)
        {
            var dict = ExtractDict(sort);
            return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>Legacy name for <see cref="ExtractJson"/>.</summary>
        public string ExtractJSON(bool sort = false) => ExtractJson(sort);

        /// <summary>JSON for <see cref="ExtractRawDict"/> (Page <c>GetText("rawjson")</c>).</summary>
        /// <param name="sort">Passed to <see cref="ExtractRawDict"/>.</param>
        public string ExtractRawJson(bool sort = false)
        {
            var dict = ExtractRawDict(sort);
            return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>Legacy name for <see cref="ExtractRawJson"/>.</summary>
        public string ExtractRawJSON(bool sort = false) => ExtractRawJson(sort);

        /// <summary>Legacy overload; <paramref name="cropbox"/> adjusts reported width/height in JSON only.</summary>
        public string ExtractRawJSON(Rect cropbox, bool sort = false)
        {
            if (cropbox == null)
                return ExtractRawJson(sort);
            var dict = ExtractRawDict(sort);
            dict["width"] = cropbox.Width;
            dict["height"] = cropbox.Height;
            return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>Image metadata list (PyMuPDF <c>TextPage.extractIMGINFO</c>).</summary>
        public List<Dictionary<string, object>> ExtractImgInfo(bool hashes = false)
        {
            int block_n = -1;
            var rc = new List<Dictionary<string, object>>();
            foreach (var block in StextBlocks)
            {
                block_n++;
                if (block.m_internal.type == mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                    continue;

                var img = block.i_image();
                int img_size = 0;
                var mask = img.mask();
                bool has_mask = mask.m_internal != null;

                var compr_buff = mupdf.mupdf.fz_compressed_image_buffer(img);
                if (compr_buff.m_internal != null)
                    img_size = (int)compr_buff.fz_compressed_buffer_size();

                byte[] digest = null;
                if (hashes)
                {
                    var r = new mupdf.FzIrect(mupdf.mupdf.fz_infinite_irect);
                    var m = new mupdf.FzMatrix(img.w(), 0, 0, img.h(), 0, 0);
                    var pix = img.fz_get_pixmap_from_image(r, m, null, null);
                    var md5 = pix.fz_md5_pixmap2();
                    digest = new byte[md5.Count];
                    for (int i = 0; i < md5.Count; i++)
                        digest[i] = md5[i];
                    if (img_size == 0)
                        img_size = img.w() * img.h() * img.n();
                }

                var cs = img.colorspace();
                var bbox = block.m_internal.bbox;
                var matrix = block.i_transform();
                var block_dict = new Dictionary<string, object>
                {
                    ["number"] = block_n,
                    ["bbox"] = new float[] { bbox.x0, bbox.y0, bbox.x1, bbox.y1 },
                    ["transform"] = new float[] { matrix.a, matrix.b, matrix.c, matrix.d, matrix.e, matrix.f },
                    ["width"] = img.w(),
                    ["height"] = img.h(),
                    ["colorspace"] = mupdf.mupdf.fz_colorspace_n(cs),
                    ["cs-name"] = mupdf.mupdf.fz_colorspace_name(cs),
                    ["xres"] = img.xres(),
                    ["yres"] = img.yres(),
                    ["bpc"] = img.bpc(),
                    ["size"] = img_size,
                    ["has-mask"] = has_mask
                };
                if (hashes)
                    block_dict["digest"] = digest;
                rc.Add(block_dict);
            }
            return rc;
        }

        // --- Legacy MuPDF.NET typed extractions --------------------------------

        /// <summary>
        /// Text lines grouped by block (Page <c>GetText("blocks")</c>).
        /// </summary>
        /// <returns>Each <see cref="TextBlock"/> has bbox, text, block number, and type (0 text, 1 image).</returns>
        public List<TextBlock> ExtractBlocks()
            => Utils.TextBlocksFromTuples(ExtractBlockTuples());

        /// <summary>
        /// Words with bbox and block/line/word indices (Page <c>GetText("words")</c>).
        /// </summary>
        /// <param name="delimiters">Extra word separators (e.g. <c>"@."</c> to split email addresses).</param>
        public List<WordBlock> ExtractWords(char[] delimiters = null)
        {
            string delim = delimiters == null ? null : new string(delimiters);
            return ExtractWordTuples(delim)
                .Select(w => new WordBlock
                {
                    X0 = w.x0,
                    Y0 = w.y0,
                    X1 = w.x1,
                    Y1 = w.y1,
                    Text = w.word,
                    BlockNum = w.blockNo,
                    LineNum = w.lineNo,
                    WordNum = w.wordNo
                })
                .ToList();
        }

        /// <summary>Typed <see cref="PageInfo"/> from dict extraction (legacy overload).</summary>
        /// <param name="cropbox">Optional override for reported width/height.</param>
        /// <param name="sort">Sort blocks when true.</param>
        public PageInfo ExtractDict(Rect cropbox, bool sort = false)
        {
            var page = TextPage2Dict(raw: false);
            if (cropbox != null)
            {
                page.Width = (float)cropbox.Width;
                page.Height = (float)cropbox.Height;
            }
            if (sort)
                SortPageInfoBlocks(page);
            page.SyncDict(raw: false);
            return page;
        }

        /// <summary>Typed raw <see cref="PageInfo"/> with character-level spans (legacy API).</summary>
        /// <param name="sort">Sort blocks when true.</param>
        public PageInfo ExtractRAWDict(bool sort = false) => ExtractRAWDict(null, sort);

        /// <summary>Typed raw <see cref="PageInfo"/> with character-level spans.</summary>
        /// <param name="cropbox">Optional override for reported width/height.</param>
        /// <param name="sort">Sort blocks when true.</param>
        public PageInfo ExtractRAWDict(Rect cropbox, bool sort = false)
        {
            var page = TextPage2Dict(raw: true);
            if (cropbox != null)
            {
                page.Width = (float)cropbox.Width;
                page.Height = (float)cropbox.Height;
            }
            if (sort)
                SortPageInfoBlocks(page);
            page.SyncDict(raw: true);
            return page;
        }

        public List<Block> ExtractImageInfo(int hashes = 0)
        {
            var items = ExtractImgInfo(hashes != 0);
            return items.Select(i =>
            {
                var bbox = i.TryGetValue("bbox", out var b) ? (float[])b : new float[] { 0, 0, 0, 0 };
                var tr = i.TryGetValue("transform", out var t) ? (float[])t : new float[] { 1, 0, 0, 1, 0, 0 };
                return new Block
                {
                    Number = i.TryGetValue("number", out var n) ? Convert.ToInt32(n) : 0,
                    Bbox = new Rect(bbox[0], bbox[1], bbox[2], bbox[3]),
                    Transform = new Matrix(tr[0], tr[1], tr[2], tr[3], tr[4], tr[5]),
                    Width = i.TryGetValue("width", out var w) ? Convert.ToInt32(w) : 0,
                    Height = i.TryGetValue("height", out var h) ? Convert.ToInt32(h) : 0,
                    ColorSpace = i.TryGetValue("colorspace", out var cs) ? Convert.ToInt32(cs) : 0,
                    CsName = i.TryGetValue("cs-name", out var csn) ? csn?.ToString() : null,
                    Xres = i.TryGetValue("xres", out var xr) ? Convert.ToInt32(xr) : 0,
                    Yres = i.TryGetValue("yres", out var yr) ? Convert.ToInt32(yr) : 0,
                    Bpc = i.TryGetValue("bpc", out var bpc) ? Convert.ToByte(bpc) : (byte)0,
                    Size = i.TryGetValue("size", out var sz) ? Convert.ToUInt32(sz) : 0
                };
            }).ToList();
        }

        public static int Canon(int c)
        {
            if (c == 0xA0 || c == 0x2028 || c == 0x2029) return ' ';
            if (c == '\r' || c == '\n' || c == '\t') return ' ';
            if (c >= 'A' && c <= 'Z') return c - 'A' + 'a';
            return c;
        }

        public int MatchString(string h0, string n0)
        {
            if (h0 == null || n0 == null) return -1;
            int h = 0, n = 0, e = h;
            var hc = Char2Canon(h0, h);
            h += hc.step;
            var nc = Char2Canon(n0, n);
            n += nc.step;
            while (hc.ch == nc.ch)
            {
                e = h;
                if (hc.ch == ' ')
                {
                    do { hc = Char2Canon(h0, h); h += hc.step; } while (hc.ch == ' ');
                }
                else
                {
                    hc = Char2Canon(h0, h); h += hc.step;
                }

                if (nc.ch == ' ')
                {
                    do { nc = Char2Canon(n0, n); n += nc.step; } while (nc.ch == ' ');
                }
                else
                {
                    nc = Char2Canon(n0, n); n += nc.step;
                }
            }
            return nc.ch != 0 ? -1 : e;
        }

        public mupdf.FzQuad GetCharQuad(mupdf.FzStextLine line, mupdf.FzStextChar ch) => new mupdf.FzQuad(ch.m_internal.quad);
        public mupdf.FzRect GetCharBbox(mupdf.FzStextLine line, mupdf.FzStextChar ch) => Helpers.JM_char_bbox(line.m_internal, ch.m_internal);
        public bool IsRectsOverlap(mupdf.FzRect a, mupdf.FzRect b) => Helpers.JM_rects_overlap(a, b);
        public uint PoolSize() => new mupdf.FzPool(NativeStextPage.m_internal.pool).fz_pool_size();

        // ─── Search ─────────────────────────────────────────────────────

        /// <summary>
        /// Search for <paramref name="needle"/> (Page <see cref="Page.SearchFor"/>).
        /// </summary>
        /// <param name="needle">Text to find; ASCII letters are case-insensitive; supports dehyphenation.</param>
        /// <param name="quads">When true, return precise quads; when false, merged axis-aligned <see cref="Quad"/> from rectangles on the same line.</param>
        /// <returns>All matches within <see cref="Rect"/> (no hit limit).</returns>
        public List<Quad> Search(string needle, bool quads)
        {
            if (quads)
                return Search(needle, 0);
            var rects = SearchRects(needle, 0);
            var result = new List<Quad>(rects.Count);
            foreach (var r in rects)
                result.Add(new Quad(r));
            return result;
        }

        /// <summary>
        /// Search for <paramref name="needle"/>; returns character quads from MuPDF.
        /// </summary>
        /// <param name="maxHits">Maximum hits, or 0 for all hits.</param>
        public List<Quad> Search(string needle, int maxHits = 0)
        {
            if (string.IsNullOrEmpty(needle))
                return new List<Quad>();
            var result = StextSearch.JM_search_stext_page(NativeStextPage, needle);
            if (maxHits > 0 && result.Count > maxHits)
                return result.GetRange(0, maxHits);
            return result;
        }

        /// <summary>Static search on a text page instance (legacy helper).</summary>
        public static List<Quad> Search(TextPage stPage, string needle, int hitMax = 0)
        {
            if (stPage == null || string.IsNullOrEmpty(needle))
                return new List<Quad>();
            return stPage.Search(needle, hitMax);
        }

        /// <summary>
        /// Search returning merged axis-aligned rectangles (overlapping hits on one baseline are joined).
        /// </summary>
        /// <param name="maxHits">Maximum hits, or 0 for all hits.</param>
        public List<Rect> SearchRects(string needle, int maxHits = 0)
        {
            var quads = Search(needle, maxHits);
            if (quads.Count == 0)
                return new List<Rect>();
            var val = new List<Rect>(quads.Count);
            foreach (var q in quads)
                val.Add(q.Rect);
            int items = val.Count;
            int i = 0;
            while (i < items - 1)
            {
                var v1 = val[i];
                var v2 = val[i + 1];
                if (v1.Y1 != v2.Y1 || (v1 & v2).IsEmpty)
                {
                    i++;
                    continue;
                }
                val[i] = v1 | v2;
                val.RemoveAt(i + 1);
                items--;
            }
            return val;
        }

        /// <summary>Plain text inside a rectangle.</summary>
        public string ExtractTextbox(Rect rect)
        {
            // this_tpage = self.this
            mupdf.FzStextPage this_tpage = NativeStextPage;
            // assert isinstance(this_tpage, mupdf.FzStextPage)
            // area = JM_rect_from_py(rect)
            mupdf.FzRect area = Helpers.JM_rect_from_py(rect);
            // found = JM_copy_rectangle(this_tpage, area)
            string found = Helpers.JM_copy_rectangle(this_tpage, area);
            // rc = PyUnicode_DecodeRawUnicodeEscape(found)
            string rc = Helpers.PyUnicode_DecodeRawUnicodeEscape(found);
            return rc;
        }

        // Legacy casing aliases (MuPDF.NET compatibility).
        public string ExtractTextBox(mupdf.FzRect area) => ExtractTextbox(new Rect(area));

        /// <summary>Text between two points (selection).</summary>
        /// <param name="a">Start point.</param>
        /// <param name="b">End point.</param>
        public string ExtractSelection(Point a, Point b) => ExtractSelection((object)a, (object)b);

        /// <summary>Text between two points (accepts <see cref="Point"/> or coordinate sequences).</summary>
        public string ExtractSelection(object pointa, object pointb)
        {
            mupdf.FzPoint a = Helpers.JM_point_from_py(pointa);
            mupdf.FzPoint b = Helpers.JM_point_from_py(pointb);
            return NativeStextPage.fz_copy_selection(a, b, 0);
        }

        // ─── Internal: Faithful port of extra.i _as_dict ────────────────

        /// <summary>
        /// Stable block views for the lifetime of this text page (MuPDF.NET <c>Blocks</c> pattern).
        /// Walk via <c>first_block</c>/<c>next</c>; do not use iterator <c>__ref__()</c> (owning wrappers).
        /// </summary>
        private IReadOnlyList<mupdf.FzStextBlock> StextBlocks
        {
            get
            {
                if (_stextBlocks == null)
                {
                    _stextBlocks = new List<mupdf.FzStextBlock>();
                    for (var b = NativeStextPage.m_internal.first_block; b != null; b = b.next)
                        _stextBlocks.Add(new mupdf.FzStextBlock(b));
                }
                return _stextBlocks;
            }
        }

        private static mupdf.fz_stext_line FirstStextLine(mupdf.FzStextBlock block)
        {
            var iter = block.begin();
            try
            {
                return iter.__deref__()?.m_internal;
            }
            finally
            {
                iter.Dispose();
            }
        }

        /// <summary>Build typed <see cref="PageInfo"/> (MuPDF.NET <c>TextPage2Dict</c>).</summary>
        internal PageInfo TextPage2Dict(bool raw = false)
        {
            var pageDict = new PageInfo
            {
                Width = Rect.Width,
                Height = Rect.Height,
                Blocks = new List<Block>(),
            };
            MakeTextPage2Dict(pageDict, raw);
            pageDict.SyncDict(raw);
            return pageDict;
        }

        /// <summary>
        /// Walk stext via cached blocks and linked lists (no iterator <c>__ref__()</c>).
        /// </summary>
        internal void MakeTextPage2Dict(PageInfo pageDict, bool raw)
        {
            using var textBuffer = mupdf.mupdf.fz_new_buffer(128);
            var stPageRect = new mupdf.FzRect(NativeStextPage.m_internal.mediabox);
            bool pageFinite = mupdf.mupdf.fz_is_infinite_rect(stPageRect) == 0;
            int blockNum = -1;
            ushort syntheticMask = (ushort)mupdf.mupdf.FZ_STEXT_SYNTHETIC;

            foreach (var block in StextBlocks)
            {
                int btype = block.m_internal.type;
                if (btype == mupdf.mupdf.FZ_STEXT_BLOCK_STRUCT)
                    continue;

                var blockBboxNative = block.m_internal.bbox;
                if (pageFinite)
                {
                    if (btype == mupdf.mupdf.FZ_STEXT_BLOCK_IMAGE)
                    {
                        if (mupdf.mupdf.fz_contains_rect(stPageRect, new mupdf.FzRect(blockBboxNative)) == 0)
                            continue;
                    }
                    else if (btype == mupdf.mupdf.FZ_STEXT_BLOCK_TEXT
                        || btype == mupdf.mupdf.FZ_STEXT_BLOCK_VECTOR
                        || btype == mupdf.mupdf.FZ_STEXT_BLOCK_GRID)
                    {
                        if (!Helpers.JM_rects_overlap(stPageRect, new mupdf.FzRect(blockBboxNative)))
                            continue;
                    }
                }

                blockNum++;
                var blockDict = new Block { Number = blockNum, Type = btype };

                if (btype == mupdf.mupdf.FZ_STEXT_BLOCK_IMAGE)
                {
                    blockDict.Bbox = new Rect(blockBboxNative);
                    FillImageBlockFields(block, blockDict);
                }
                else if (btype == mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                {
                    var lineList = new List<Line>();
                    var blockRect = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);

                    for (mupdf.fz_stext_line fzLine = FirstStextLine(block);
                         fzLine != null;
                         fzLine = fzLine.next)
                    {
                        var intersect = mupdf.mupdf.fz_intersect_rect(stPageRect, new mupdf.FzRect(fzLine.bbox));
                        if (mupdf.mupdf.fz_is_empty_rect(intersect) != 0 && pageFinite)
                            continue;

                        var lineDict = new Line();
                        var spanList = new List<Span>();
                        mupdf.mupdf.fz_clear_buffer(textBuffer);
                        var spanRect = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);
                        var lineRect = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);

                        float oldSize = -1;
                        uint oldFlags = 0;
                        uint oldCharFlags = 0;
                        string oldFont = "";
                        uint oldArgb = 0;
                        ushort oldBidi = 0;
                        float spanOriginX = 0, spanOriginY = 0;

                        Span span = null;
                        List<Char> charList = null;

                        for (mupdf.fz_stext_char fzCh = fzLine.first_char;
                             fzCh != null;
                             fzCh = fzCh.next)
                        {
                            var r = Helpers.JM_char_bbox(fzLine, fzCh);
                            if (!Helpers.JM_rects_overlap(stPageRect, r) && pageFinite)
                                continue;

                            int flags = Helpers.JM_char_font_flags(fzCh.font, fzLine, fzCh);
                            float size = fzCh.size;
                            uint charFlags = (uint)(fzCh.flags & ~mupdf.mupdf.FZ_STEXT_SYNTHETIC);
                            string fontName = Helpers.JM_font_name(fzCh.font);
                            uint argb = fzCh.argb;
                            float asc = Helpers.JM_font_ascender(fzCh.font);
                            float desc = Helpers.JM_font_descender(fzCh.font);
                            ushort bidi = fzCh.bidi;

                            bool styleChanged =
                                size != oldSize
                                || (uint)flags != oldFlags
                                || charFlags != oldCharFlags
                                || argb != oldArgb
                                || fontName != oldFont
                                || bidi != oldBidi;

                            if (styleChanged)
                            {
                                if (oldSize >= 0)
                                {
                                    if (raw)
                                    {
                                        span.Chars = charList;
                                        charList = null;
                                    }
                                    else
                                    {
                                        span.Text = Helpers.JmEscapeStrFromBuffer(textBuffer);
                                        mupdf.mupdf.fz_clear_buffer(textBuffer);
                                    }
                                    span.Origin = new Point(spanOriginX, spanOriginY);
                                    span.Bbox = new Rect(spanRect);
                                    lineRect = mupdf.mupdf.fz_union_rect(lineRect, spanRect);
                                    spanList.Add(span);
                                    span = null;
                                }

                                span = new Span();
                                float spanAsc = asc, spanDesc = desc;
                                if (asc < 1e-3f)
                                {
                                    spanAsc = 0.9f;
                                    spanDesc = -0.1f;
                                }
                                span.Size = size;
                                span.Flags = flags;
                                span.CharFlags = charFlags;
                                span.Bidi = bidi;
                                span.Font = fontName;
                                span.Color = (int)(argb & 0xffffffu);
                                span.Alpha = (int)(argb >> 24);
                                span.Asc = spanAsc;
                                span.Desc = spanDesc;

                                oldSize = size;
                                oldFlags = (uint)flags;
                                oldCharFlags = charFlags;
                                oldFont = fontName;
                                oldArgb = argb;
                                oldBidi = bidi;
                                spanRect = r;
                                spanOriginX = fzCh.origin.x;
                                spanOriginY = fzCh.origin.y;
                            }

                            spanRect = mupdf.mupdf.fz_union_rect(spanRect, r);

                            if (raw)
                            {
                                if (charList == null)
                                    charList = new List<Char>();
                                charList.Add(new Char
                                {
                                    Origin = new mupdf.FzPoint(fzCh.origin),
                                    Bbox = r,
                                    C = fzCh.c <= 0xffff ? (char)fzCh.c : char.ConvertFromUtf32(fzCh.c)[0],
                                    Synthetic = (fzCh.flags & syntheticMask) != 0,
                                });
                            }
                            else
                            {
                                Helpers.JmAppendRune(textBuffer, fzCh.c);
                            }
                        }

                        if (span != null)
                        {
                            if (raw)
                                span.Chars = charList;
                            else
                            {
                                span.Text = Helpers.JmEscapeStrFromBuffer(textBuffer);
                                mupdf.mupdf.fz_clear_buffer(textBuffer);
                            }
                            span.Origin = new Point(spanOriginX, spanOriginY);
                            span.Bbox = new Rect(spanRect);

                            if (mupdf.mupdf.fz_is_empty_rect(spanRect) == 0)
                            {
                                spanList.Add(span);
                                lineRect = mupdf.mupdf.fz_union_rect(lineRect, spanRect);
                            }
                        }

                        blockRect = mupdf.mupdf.fz_union_rect(blockRect, lineRect);
                        lineDict.Spans = spanList;
                        lineDict.WMode = (int)fzLine.wmode;
                        lineDict.Dir = new Point(fzLine.dir.x, fzLine.dir.y);
                        lineDict.Bbox = new Rect(lineRect);
                        lineList.Add(lineDict);
                    }

                    blockDict.Bbox = new Rect(blockRect);
                    blockDict.Lines = lineList;
                }
                else if (btype == mupdf.mupdf.FZ_STEXT_BLOCK_VECTOR
                    || btype == mupdf.mupdf.FZ_STEXT_BLOCK_GRID)
                {
                    blockDict.Bbox = new Rect(blockBboxNative);
                }

                pageDict.Blocks.Add(blockDict);
            }
        }

        private static void FillImageBlockFields(mupdf.FzStextBlock block, Block blockDict)
        {
            var image = block.i_image();
            var transform = block.i_transform();

            int w = image.w();
            int h = image.h();
            int n = image.n();
            int imgType = mupdf.mupdf.FZ_IMAGE_UNKNOWN;

            var compressedBuffer = image.fz_compressed_image_buffer();
            if (compressedBuffer?.m_internal != null)
                imgType = compressedBuffer.m_internal.params_.type;
            if (imgType < mupdf.mupdf.FZ_IMAGE_BMP || imgType == mupdf.mupdf.FZ_IMAGE_JBIG2)
                imgType = mupdf.mupdf.FZ_IMAGE_UNKNOWN;

            byte[] imageBytes = Array.Empty<byte>();
            string ext = "png";
            mupdf.FzBuffer ownedBuf = null;
            try
            {
                mupdf.FzBuffer buf;
                if (compressedBuffer?.m_internal == null || imgType == mupdf.mupdf.FZ_IMAGE_UNKNOWN)
                {
                    ownedBuf = mupdf.mupdf.fz_new_buffer_from_image_as_png(
                        image, new mupdf.FzColorParams(mupdf.mupdf.fz_default_color_params));
                    buf = ownedBuf;
                    ext = "png";
                }
                else if (n == 4 && JM_image_extension(imgType) == "jpeg")
                {
                    ownedBuf = image.fz_new_buffer_from_image_as_jpeg(
                        new mupdf.FzColorParams(mupdf.mupdf.fz_default_color_params), 95, 1);
                    buf = ownedBuf;
                    ext = "jpeg";
                }
                else
                {
                    ownedBuf = new mupdf.FzBuffer(
                        mupdf.mupdf.ll_fz_keep_buffer(compressedBuffer.m_internal.buffer));
                    buf = ownedBuf;
                    ext = JM_image_extension(imgType);
                }
                imageBytes = Helpers.BinFromBuffer(buf);
            }
            catch
            {
                imageBytes = Array.Empty<byte>();
                ext = "png";
            }
            finally
            {
                ownedBuf?.Dispose();
            }

            blockDict.Width = w;
            blockDict.Height = h;
            blockDict.Ext = ext;
            blockDict.ColorSpace = n;
            blockDict.Xres = image.xres();
            blockDict.Yres = image.yres();
            blockDict.Bpc = image.bpc();
            blockDict.Transform = new Matrix(transform.a, transform.b, transform.c, transform.d, transform.e, transform.f);
            blockDict.Size = (uint)imageBytes.Length;
            blockDict.Image = imageBytes;

            var maskImg = image.mask();
            if (maskImg.m_internal != null)
            {
                mupdf.FzBuffer maskOwned = null;
                try
                {
                    maskOwned = mupdf.mupdf.fz_new_buffer_from_image_as_png(
                        maskImg, new mupdf.FzColorParams(mupdf.mupdf.fz_default_color_params));
                    blockDict.Mask = Helpers.BinFromBuffer(maskOwned);
                }
                catch
                {
                    blockDict.Mask = null;
                }
                finally
                {
                    maskOwned?.Dispose();
                }
            }
        }

        private static void SortPageInfoBlocks(PageInfo page)
        {
            page.Blocks.Sort((b1, b2) =>
            {
                if (b1.Bbox == null || b2.Bbox == null)
                    return 0;
                if (Math.Abs(b1.Bbox.Y1 - b2.Bbox.Y1) < 1e-6f)
                    return b1.Bbox.X0.CompareTo(b2.Bbox.X0);
                return b1.Bbox.Y1.CompareTo(b2.Bbox.Y1);
            });
        }

        private void JM_make_textpage_dict(
            mupdf.FzStextPage tp,
            Dictionary<string, object> page_dict,
            bool raw)
        {
            using var text_buffer = mupdf.mupdf.fz_new_buffer(128);
            var block_list = new List<Dictionary<string, object>>();
            var tp_rect = new mupdf.FzRect(tp.m_internal.mediabox);
            int block_n = -1;

            foreach (var block in StextBlocks)
            {
                int blockType = block.m_internal.type;

                if (blockType == mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                {
                    if (Helpers.JM_rects_overlap(tp_rect, new mupdf.FzRect(block.m_internal.bbox))
                        || mupdf.mupdf.fz_is_infinite_rect(tp_rect) != 0)
                    {
                        var block_dict = new Dictionary<string, object>();
                        block_n++;
                        block_dict["type"] = blockType;
                        block_dict["number"] = block_n;
                        JM_make_text_block(block, block_dict, raw, text_buffer, tp_rect);
                        block_list.Add(block_dict);
                    }
                }
                else if (blockType == mupdf.mupdf.FZ_STEXT_BLOCK_IMAGE)
                {
                    if (mupdf.mupdf.fz_contains_rect(tp_rect, new mupdf.FzRect(block.m_internal.bbox)) != 0
                        || mupdf.mupdf.fz_is_infinite_rect(tp_rect) != 0)
                    {
                        var block_dict = new Dictionary<string, object>();
                        block_n++;
                        block_dict["type"] = blockType;
                        block_dict["number"] = block_n;
                        block_dict["bbox"] = new float[]
                        {
                            block.m_internal.bbox.x0, block.m_internal.bbox.y0,
                            block.m_internal.bbox.x1, block.m_internal.bbox.y1,
                        };
                        JM_make_image_block(block, block_dict);
                        block_list.Add(block_dict);
                    }
                }
            }

            page_dict["blocks"] = block_list;
        }

        /// <summary>
        /// Port of extra.i JM_make_text_block().
        /// </summary>
        private void JM_make_text_block(
            mupdf.FzStextBlock block,
            Dictionary<string, object> block_dict,
            bool raw,
            mupdf.FzBuffer buff,
            mupdf.FzRect tp_rect)
        {
            var line_list = new List<Dictionary<string, object>>();
            var block_rect = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);

            for (mupdf.fz_stext_line fzLine = FirstStextLine(block);
                 fzLine != null;
                 fzLine = fzLine.next)
            {
                var intersect = mupdf.mupdf.fz_intersect_rect(tp_rect, new mupdf.FzRect(fzLine.bbox));
                if (mupdf.mupdf.fz_is_empty_rect(intersect) != 0
                    && mupdf.mupdf.fz_is_infinite_rect(tp_rect) == 0)
                {
                    continue;
                }

                var line_dict = new Dictionary<string, object>();
                var line_rect = JM_make_spanlist(line_dict, fzLine, raw, buff, tp_rect);

                block_rect = mupdf.mupdf.fz_union_rect(block_rect, line_rect);
                line_dict["wmode"] = (int)fzLine.wmode;
                line_dict["dir"] = new float[] { fzLine.dir.x, fzLine.dir.y };
                line_dict["bbox"] = new float[] { line_rect.x0, line_rect.y0, line_rect.x1, line_rect.y1 };
                line_list.Add(line_dict);
            }

            block_dict["bbox"] = new float[] { block_rect.x0, block_rect.y0, block_rect.x1, block_rect.y1 };
            block_dict["lines"] = line_list;
        }

        /// <summary>
        /// Port of extra.i JM_make_spanlist().
        /// Builds span list for a single line, splitting spans by style changes.
        /// </summary>
        private mupdf.FzRect JM_make_spanlist(
            Dictionary<string, object> line_dict,
            mupdf.fz_stext_line fzLine,
            bool raw,
            mupdf.FzBuffer buff,
            mupdf.FzRect tp_rect)
        {
            var span_list = new List<Dictionary<string, object>>();
            mupdf.mupdf.fz_clear_buffer(buff);
            var span_rect = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);
            var line_rect = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);
            float span_origin_x = 0, span_origin_y = 0;

            float old_size = -1;
            uint old_flags = 0;
            uint old_char_flags = 0;
            string old_font = "";
            uint old_argb = 0;
            ushort old_bidi = 0;

            Dictionary<string, object> span = null;
            List<Dictionary<string, object>> char_list = null;

            for (mupdf.fz_stext_char fzCh = fzLine.first_char;
                 fzCh != null;
                 fzCh = fzCh.next)
            {
                var r = Helpers.JM_char_bbox(fzLine, fzCh);
                if (!Helpers.JM_rects_overlap(tp_rect, r)
                    && mupdf.mupdf.fz_is_infinite_rect(tp_rect) == 0)
                {
                    continue;
                }

                int flags = Helpers.JM_char_font_flags(fzCh.font, fzLine, fzCh);
                float size = fzCh.size;
                uint char_flags = (uint)(fzCh.flags & ~mupdf.mupdf.FZ_STEXT_SYNTHETIC);
                string fontName = Helpers.JM_font_name(fzCh.font);
                uint argb = fzCh.argb;
                float asc = Helpers.JM_font_ascender(fzCh.font);
                float desc = Helpers.JM_font_descender(fzCh.font);
                ushort bidi = fzCh.bidi;

                bool style_changed =
                    size != old_size
                    || (uint)flags != old_flags
                    || char_flags != old_char_flags
                    || argb != old_argb
                    || fontName != old_font
                    || bidi != old_bidi;

                if (style_changed)
                {
                    if (old_size >= 0)
                    {
                        if (raw)
                        {
                            span["chars"] = char_list;
                            char_list = null;
                        }
                        else
                        {
                            span["text"] = Helpers.JmEscapeStrFromBuffer(buff);
                            mupdf.mupdf.fz_clear_buffer(buff);
                        }
                        span["origin"] = new float[] { span_origin_x, span_origin_y };
                        span["bbox"] = new float[] { span_rect.x0, span_rect.y0, span_rect.x1, span_rect.y1 };
                        line_rect = mupdf.mupdf.fz_union_rect(line_rect, span_rect);
                        span_list.Add(span);
                        span = null;
                    }

                    span = new Dictionary<string, object>();
                    float spanAsc = asc, spanDesc = desc;
                    if (asc < 1e-3f)
                    {
                        spanAsc = 0.9f;
                        spanDesc = -0.1f;
                    }
                    span["size"] = size;
                    span["flags"] = (uint)flags;
                    span["bidi"] = (uint)bidi;
                    span["char_flags"] = char_flags;
                    span["font"] = fontName;
                    span["color"] = argb & 0xffffffu;
                    span["alpha"] = argb >> 24;
                    span["ascender"] = spanAsc;
                    span["descender"] = spanDesc;

                    old_size = size;
                    old_flags = (uint)flags;
                    old_char_flags = char_flags;
                    old_font = fontName;
                    old_argb = argb;
                    old_bidi = bidi;
                    span_rect = r;
                    span_origin_x = fzCh.origin.x;
                    span_origin_y = fzCh.origin.y;
                }

                span_rect = mupdf.mupdf.fz_union_rect(span_rect, r);

                if (raw)
                {
                    var char_dict = new Dictionary<string, object>();
                    char_dict["origin"] = new float[] { fzCh.origin.x, fzCh.origin.y };
                    char_dict["bbox"] = new float[] { r.x0, r.y0, r.x1, r.y1 };
                    char_dict["c"] = char.ConvertFromUtf32(fzCh.c);
                    char_dict["synthetic"] = (fzCh.flags & mupdf.mupdf.FZ_STEXT_SYNTHETIC) != 0;
                    if (char_list == null)
                        char_list = new List<Dictionary<string, object>>();
                    char_list.Add(char_dict);
                }
                else
                {
                    Helpers.JmAppendRune(buff, fzCh.c);
                }
            }

            if (span != null)
            {
                if (raw)
                {
                    span["chars"] = char_list;
                    char_list = null;
                }
                else
                {
                    span["text"] = Helpers.JmEscapeStrFromBuffer(buff);
                    mupdf.mupdf.fz_clear_buffer(buff);
                }
                span["origin"] = new float[] { span_origin_x, span_origin_y };
                span["bbox"] = new float[] { span_rect.x0, span_rect.y0, span_rect.x1, span_rect.y1 };

                if (mupdf.mupdf.fz_is_empty_rect(span_rect) == 0)
                {
                    span_list.Add(span);
                    line_rect = mupdf.mupdf.fz_union_rect(line_rect, span_rect);
                }
                span = null;
            }

            line_dict["spans"] = span_list;
            return line_rect;
        }

        /// <summary>
        /// Port of extra.i JM_make_image_block().
        /// </summary>
        private void JM_make_image_block(
            mupdf.FzStextBlock block,
            Dictionary<string, object> block_dict)
        {
            var image = block.i_image();
            var transform = block.i_transform();

            int w = image.w();
            int h = image.h();
            int n = image.n();
            int imgType = mupdf.mupdf.FZ_IMAGE_UNKNOWN;

            var compressedBuffer = image.fz_compressed_image_buffer();
            if (compressedBuffer?.m_internal != null)
                imgType = compressedBuffer.m_internal.params_.type;
            if (imgType < mupdf.mupdf.FZ_IMAGE_BMP || imgType == mupdf.mupdf.FZ_IMAGE_JBIG2)
                imgType = mupdf.mupdf.FZ_IMAGE_UNKNOWN;

            byte[] imageBytes = Array.Empty<byte>();
            string ext = "png";
            mupdf.FzBuffer ownedBuf = null;
            try
            {
                mupdf.FzBuffer buf;
                if (compressedBuffer?.m_internal == null || imgType == mupdf.mupdf.FZ_IMAGE_UNKNOWN)
                {
                    ownedBuf = mupdf.mupdf.fz_new_buffer_from_image_as_png(
                        image, new mupdf.FzColorParams(mupdf.mupdf.fz_default_color_params));
                    buf = ownedBuf;
                    ext = "png";
                }
                else if (n == 4 && JM_image_extension(imgType) == "jpeg")
                {
                    ownedBuf = image.fz_new_buffer_from_image_as_jpeg(
                        new mupdf.FzColorParams(mupdf.mupdf.fz_default_color_params), 95, 1);
                    buf = ownedBuf;
                    ext = "jpeg";
                }
                else
                {
                    ownedBuf = new mupdf.FzBuffer(
                        mupdf.mupdf.ll_fz_keep_buffer(compressedBuffer.m_internal.buffer));
                    buf = ownedBuf;
                    ext = JM_image_extension(imgType);
                }
                imageBytes = Helpers.BinFromBuffer(buf);
            }
            catch
            {
                imageBytes = Array.Empty<byte>();
                ext = "png";
            }
            finally
            {
                ownedBuf?.Dispose();
            }

            block_dict["width"] = w;
            block_dict["height"] = h;
            block_dict["ext"] = ext;
            block_dict["colorspace"] = n;
            block_dict["xres"] = image.xres();
            block_dict["yres"] = image.yres();
            block_dict["bpc"] = image.bpc();
            block_dict["transform"] = new float[] { transform.a, transform.b, transform.c, transform.d, transform.e, transform.f };
            block_dict["size"] = imageBytes.Length;
            block_dict["image"] = imageBytes;

            var maskImg = image.mask();
            if (maskImg.m_internal != null)
            {
                mupdf.FzBuffer maskOwned = null;
                try
                {
                    maskOwned = mupdf.mupdf.fz_new_buffer_from_image_as_png(
                        maskImg, new mupdf.FzColorParams(mupdf.mupdf.fz_default_color_params));
                    block_dict["mask"] = Helpers.BinFromBuffer(maskOwned);
                }
                catch
                {
                    block_dict["mask"] = null;
                }
                finally
                {
                    maskOwned?.Dispose();
                }
            }
            else
            {
                block_dict["mask"] = null;
            }
        }

        // ─── Private helpers ────────────────────────────────────────────

        private static bool JM_is_word_delimiter(int c, string delimiters)
        {
            if (c <= 32 || c == 160 || (0x202a <= c && c <= 0x202e))
                return true;
            if (string.IsNullOrEmpty(delimiters))
                return false;
            foreach (char d in delimiters)
            {
                if (d == c) return true;
            }
            return false;
        }

        private static bool JM_is_rtl_char(int c)
        {
            return c >= 0x590 && c <= 0x900;
        }

        private static (int step, int ch) Char2Canon(string s, int offset)
        {
            if (string.IsNullOrEmpty(s) || offset >= s.Length) return (1, 0);
            int rune = char.ConvertToUtf32(s, offset);
            int step = char.IsSurrogatePair(s, offset) ? 2 : 1;
            return (step, Canon(rune));
        }

        private static Rect DictBboxToRect(object bboxObj)
        {
            if (bboxObj is float[] bb && bb.Length >= 4)
                return new Rect(bb[0], bb[1], bb[2], bb[3]);
            return null;
        }

        private static Point DictPointFromFloats(object obj)
        {
            if (obj is float[] v && v.Length >= 2)
                return new Point(v[0], v[1]);
            return null;
        }

        private static Matrix DictMatrixFromFloats(object obj)
        {
            if (obj is float[] tr && tr.Length >= 6)
                return new Matrix(tr[0], tr[1], tr[2], tr[3], tr[4], tr[5]);
            return null;
        }

        private static List<Char> DictCharsToList(object charsObj)
        {
            if (charsObj is not List<Dictionary<string, object>> rows)
                return null;
            var result = new List<Char>(rows.Count);
            foreach (var c in rows)
            {
                var origin = c.TryGetValue("origin", out var o) ? o as float[] : null;
                var cbbox = c.TryGetValue("bbox", out var bb) ? bb as float[] : null;
                result.Add(new Char
                {
                    Origin = origin != null && origin.Length >= 2
                        ? new mupdf.FzPoint(origin[0], origin[1])
                        : default,
                    Bbox = cbbox != null && cbbox.Length >= 4
                        ? new mupdf.FzRect(cbbox[0], cbbox[1], cbbox[2], cbbox[3])
                        : default,
                    C = c.TryGetValue("c", out var ch) && ch is string s && s.Length > 0 ? s[0] : '\0',
                    Synthetic = c.TryGetValue("synthetic", out var syn) && Convert.ToBoolean(syn),
                });
            }
            return result;
        }

        private static Span DictToSpan(Dictionary<string, object> s) =>
            new Span
            {
                Text = s.TryGetValue("text", out var t) ? t?.ToString() : null,
                Size = s.TryGetValue("size", out var sz) ? Convert.ToSingle(sz) : 0f,
                Flags = s.TryGetValue("flags", out var fl) ? Convert.ToSingle(fl) : 0f,
                CharFlags = s.TryGetValue("char_flags", out var cf) ? Convert.ToUInt32(cf) : 0u,
                Bidi = s.TryGetValue("bidi", out var bi) ? Convert.ToUInt16(bi) : (ushort)0,
                Font = s.TryGetValue("font", out var f) ? f?.ToString() : null,
                Color = s.TryGetValue("color", out var c) ? Convert.ToInt32(c) : 0,
                Alpha = s.TryGetValue("alpha", out var a) ? Convert.ToInt32(a) : 0,
                Asc = s.TryGetValue("ascender", out var asc) ? Convert.ToSingle(asc) : 0f,
                Desc = s.TryGetValue("descender", out var desc) ? Convert.ToSingle(desc) : 0f,
                Bbox = DictBboxToRect(s.TryGetValue("bbox", out var bb) ? bb : null),
                Origin = DictPointFromFloats(s.TryGetValue("origin", out var o) ? o : null),
                Chars = s.TryGetValue("chars", out var ch) ? DictCharsToList(ch) : null,
            };

        private static Line DictToLine(Dictionary<string, object> l)
        {
            var line = new Line
            {
                WMode = l.TryGetValue("wmode", out var wm) ? Convert.ToInt32(wm) : 0,
                Dir = DictPointFromFloats(l.TryGetValue("dir", out var d) ? d : null),
                Bbox = DictBboxToRect(l.TryGetValue("bbox", out var bb) ? bb : null),
                Spans = new List<Span>(),
            };
            if (l.TryGetValue("spans", out var spansObj) && spansObj is List<Dictionary<string, object>> spans)
            {
                foreach (var sd in spans)
                    line.Spans.Add(DictToSpan(sd));
            }
            return line;
        }

        private static Block DictToBlock(Dictionary<string, object> b)
        {
            var block = new Block
            {
                Number = b.TryGetValue("number", out var number) ? Convert.ToInt32(number) : 0,
                Type = b.TryGetValue("type", out var type) ? Convert.ToInt32(type) : 0,
                Bbox = DictBboxToRect(b.TryGetValue("bbox", out var bb) ? bb : null),
            };

            if (block.Type == mupdf.mupdf.FZ_STEXT_BLOCK_IMAGE)
            {
                block.Width = b.TryGetValue("width", out var w) ? Convert.ToInt32(w) : 0;
                block.Height = b.TryGetValue("height", out var h) ? Convert.ToInt32(h) : 0;
                block.Ext = b.TryGetValue("ext", out var ex) ? ex?.ToString() : null;
                block.ColorSpace = b.TryGetValue("colorspace", out var cs) ? Convert.ToInt32(cs) : 0;
                block.Xres = b.TryGetValue("xres", out var xr) ? Convert.ToInt32(xr) : 0;
                block.Yres = b.TryGetValue("yres", out var yr) ? Convert.ToInt32(yr) : 0;
                block.Bpc = b.TryGetValue("bpc", out var bpc) ? Convert.ToByte(bpc) : (byte)0;
                block.Transform = DictMatrixFromFloats(b.TryGetValue("transform", out var tr) ? tr : null);
                block.Size = b.TryGetValue("size", out var sz) ? Convert.ToUInt32(sz) : 0u;
                block.Image = b.TryGetValue("image", out var img) ? img as byte[] : null;
                block.Mask = b.TryGetValue("mask", out var mask) ? mask as byte[] : null;
            }
            else if (b.TryGetValue("lines", out var linesObj) && linesObj is List<Dictionary<string, object>> lines)
            {
                block.Lines = new List<Line>(lines.Count);
                foreach (var ld in lines)
                    block.Lines.Add(DictToLine(ld));
            }

            return block;
        }

        internal static PageInfo ToPageInfo(Dictionary<string, object> pageDict)
        {
            if (pageDict is PageInfo pageInfo)
                return pageInfo;

            var page = new PageInfo
            {
                Width = pageDict.TryGetValue("width", out var w) ? Convert.ToSingle(w) : 0,
                Height = pageDict.TryGetValue("height", out var h) ? Convert.ToSingle(h) : 0,
                Blocks = new List<Block>()
            };

            if (!pageDict.TryGetValue("blocks", out var blocksObj) || blocksObj is not List<Dictionary<string, object>> blocks)
            {
                page.SyncDict(raw: false);
                return page;
            }

            foreach (var b in blocks)
                page.Blocks.Add(DictToBlock(b));

            page.SyncDict(raw: DictListLooksRaw(blocks));
            return page;
        }

        private static bool DictListLooksRaw(List<Dictionary<string, object>> blocks)
        {
            foreach (var block in blocks)
            {
                if (!block.TryGetValue("lines", out var linesObj)
                    || linesObj is not List<Dictionary<string, object>> lines)
                    continue;
                foreach (var line in lines)
                {
                    if (!line.TryGetValue("spans", out var spansObj)
                        || spansObj is not List<Dictionary<string, object>> spans)
                        continue;
                    foreach (var span in spans)
                    {
                        if (span.ContainsKey("chars"))
                            return true;
                    }
                }
            }
            return false;
        }

        internal static void SyncPageInfoDict(PageInfo page, bool raw)
        {
            page["width"] = page.Width;
            page["height"] = page.Height;
            page["blocks"] = BlocksToDictList(page.Blocks, raw);
        }

        private static List<Dictionary<string, object>> BlocksToDictList(List<Block> blocks, bool raw)
        {
            if (blocks == null)
                return new List<Dictionary<string, object>>();
            var result = new List<Dictionary<string, object>>(blocks.Count);
            foreach (var block in blocks)
                result.Add(BlockToDict(block, raw));
            return result;
        }

        private static float[] RectToFloats(Rect r) =>
            r == null ? null : new float[] { r.X0, r.Y0, r.X1, r.Y1 };

        private static float[] PointToFloats(Point p) =>
            p == null ? null : new float[] { p.X, p.Y };

        private static float[] MatrixToFloats(Matrix m) =>
            m == null ? null : new float[] { m.A, m.B, m.C, m.D, m.E, m.F };

        private static Dictionary<string, object> CharToDict(Char ch)
        {
            var dict = new Dictionary<string, object>
            {
                ["origin"] = new float[] { ch.Origin.x, ch.Origin.y },
                ["bbox"] = new float[] { ch.Bbox.x0, ch.Bbox.y0, ch.Bbox.x1, ch.Bbox.y1 },
                ["c"] = ch.C.ToString(),
                ["synthetic"] = ch.Synthetic,
            };
            return dict;
        }

        private static Dictionary<string, object> SpanToDict(Span span, bool raw)
        {
            var dict = new Dictionary<string, object>
            {
                ["size"] = span.Size,
                ["flags"] = (uint)span.Flags,
                ["bidi"] = (uint)span.Bidi,
                ["char_flags"] = span.CharFlags,
                ["font"] = span.Font,
                ["color"] = span.Color,
                ["alpha"] = span.Alpha,
                ["ascender"] = span.Asc,
                ["descender"] = span.Desc,
                ["origin"] = PointToFloats(span.Origin),
                ["bbox"] = RectToFloats(span.Bbox),
            };
            if (raw)
                dict["chars"] = span.Chars?.ConvertAll(CharToDict) ?? new List<Dictionary<string, object>>();
            else
                dict["text"] = span.Text;
            return dict;
        }

        private static Dictionary<string, object> LineToDict(Line line, bool raw)
        {
            var spans = new List<Dictionary<string, object>>();
            if (line.Spans != null)
            {
                foreach (var span in line.Spans)
                    spans.Add(SpanToDict(span, raw));
            }
            return new Dictionary<string, object>
            {
                ["spans"] = spans,
                ["wmode"] = line.WMode,
                ["dir"] = PointToFloats(line.Dir),
                ["bbox"] = RectToFloats(line.Bbox),
            };
        }

        private static Dictionary<string, object> BlockToDict(Block block, bool raw)
        {
            var dict = new Dictionary<string, object>
            {
                ["number"] = block.Number,
                ["type"] = block.Type,
                ["bbox"] = RectToFloats(block.Bbox),
            };
            if (block.Type == mupdf.mupdf.FZ_STEXT_BLOCK_IMAGE)
            {
                dict["width"] = block.Width;
                dict["height"] = block.Height;
                dict["ext"] = block.Ext;
                dict["colorspace"] = block.ColorSpace;
                dict["xres"] = block.Xres;
                dict["yres"] = block.Yres;
                dict["bpc"] = block.Bpc;
                dict["transform"] = MatrixToFloats(block.Transform);
                dict["size"] = block.Size;
                dict["image"] = block.Image;
                dict["mask"] = block.Mask;
            }
            else if (block.Lines != null)
            {
                var lines = new List<Dictionary<string, object>>(block.Lines.Count);
                foreach (var line in block.Lines)
                    lines.Add(LineToDict(line, raw));
                dict["lines"] = lines;
            }
            return dict;
        }

        private static string JM_image_extension(int type)
        {
            switch (type)
            {
                case 1: return "png";
                case 2: return "jpeg";
                case 3: return "jxr";
                case 4: return "jpx";
                case 5: return "bmp";
                case 6: return "gif";
                case 7: return "tiff";
                case 8: return "pnm";
                default: return "unknown";
            }
        }



        // ─── PyMuPDF API names (internal, same assembly) ─────────────────

        internal Rect rect => Rect;
        internal string extractText(bool sort = false) => ExtractText(sort);
        internal string extractTEXT(bool sort = false) => ExtractText(sort);
        internal List<(float x0, float y0, float x1, float y1, string text, int blockNo, int blockType)> extractBLOCKS()
            => ExtractBlockTuples();
        internal List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> extractWORDS(string delimiters = null)
            => ExtractWordTuples(delimiters);
        internal string extractHTML() => ExtractHtml();
        internal string extractXHTML() => ExtractXhtml();
        internal string extractXML() => ExtractXml();
        internal Dictionary<string, object> extractDICT(bool sort = false) => ExtractDict(sort);
        internal Dictionary<string, object> extractRAWDICT(bool sort = false) => ExtractRawDict(sort);
        internal string extractJSON(bool sort = false) => ExtractJson(sort);
        internal string extractRAWJSON(bool sort = false) => ExtractRawJson(sort);
        internal List<Dictionary<string, object>> extractIMGINFO(bool hashes = false) => ExtractImgInfo(hashes);
        internal string extractSelection(object pointa, object pointb) => ExtractSelection(pointa, pointb);
        internal string extractTextbox(Rect rect) => ExtractTextbox(rect);
        internal List<Quad> search(string needle, int hit_max = 16) => Search(needle, hit_max);
        internal List<Rect> search_rects(string needle, int hit_max = 16) => SearchRects(needle, hit_max);

        //-----------------------------------------------------------------------------
        // Plain text output. An identical copy of fz_print_stext_page_as_text,
        // but lines within a block are concatenated by space instead a new-line
        // character (which else leads to 2 new-lines).
        //-----------------------------------------------------------------------------

        /// <summary>PyMuPDF <c>JM_print_stext_page_as_text</c> (<c>extra.i</c>).</summary>
        private static void JM_print_stext_page_as_text(mupdf.FzBuffer res, mupdf.FzStextPage page)
        {
            // fz_stext_block *block = page.m_internal->first_block;
            // _as_text(block, res, page);
            AsText(page, res);
        }

        /// <summary>PyMuPDF <c>_as_text</c> (<c>extra.i</c>).</summary>
        private static void AsText(mupdf.FzStextPage page, mupdf.FzBuffer res)
        {
            var rect = page.m_internal.mediabox;
            using var pageRect = new mupdf.FzRect(rect);

            for (mupdf.fz_stext_block block = page.m_internal.first_block;
                 block != null;
                 block = block.next)
            {
                if (block.type == mupdf.mupdf.FZ_STEXT_BLOCK_STRUCT)
                    continue;

                if (block.type != mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                    continue;

                int last_char = 0;
                for (mupdf.fz_stext_line line = Helpers.FirstStextLinePtr(block);
                     line != null;
                     line = line.next)
                {
                    for (mupdf.fz_stext_char ch = line.first_char;
                         ch != null;
                         ch = ch.next)
                    {
                        var cbbox = Helpers.JM_char_bbox(line, ch);
                        if (mupdf.mupdf.fz_is_infinite_rect(pageRect) != 0
                            || Helpers.JM_rects_overlap(pageRect, cbbox))
                        {
                            last_char = ch.c;
                            Helpers.JmAppendRune(res, last_char);
                        }
                    }
                    if (last_char != 10 && last_char > 0)
                    {
                        res.fz_append_string("\n");
                        last_char = 10;
                    }
                }
                if (last_char != 10 && last_char > 0)
                {
                    res.fz_append_string("\n");
                    last_char = 10;
                }
            }
        }

        // ─── IDisposable ────────────────────────────────────────────────

        /// <summary>Releases the native text page when <see cref="ThisOwn"/> is true.</summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _stextBlocks = null;
                if (ThisOwn && _nativeStp != null)
                {
                    _nativeStp.Dispose();
                    _nativeStp = null;
                }
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~TextPage() => Dispose();
    }
}
