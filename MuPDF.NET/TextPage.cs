using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents the text content of a page, created by Page.GetTextPage().
    /// Port of the Python TextPage class from __init__.py.
    /// </summary>
    public partial class TextPage : IDisposable
    {
        private mupdf.FzStextPage _nativeStp;
        private bool _disposed;
        internal Page Parent { get; set; }

        internal mupdf.FzStextPage NativeStextPage
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(TextPage));
                return _nativeStp;
            }
        }

        internal TextPage(mupdf.FzStextPage stp)
        {
            _nativeStp = stp;
        }

        /// <summary>
        /// Page rectangle of the text page.
        /// Corresponds to Python TextPage.rect property.
        /// </summary>
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
        /// Extract plain text from the page.
        /// Corresponds to Python TextPage._extractText(format_=0) / extractText().
        /// </summary>
        public string ExtractText(bool sort = false)
        {
            if (!sort)
            {
                var buf = mupdf.mupdf.fz_new_buffer_from_stext_page(NativeStextPage);
                return Encoding.UTF8.GetString(buf.fz_buffer_extract());
            }
            var blocks = ExtractBlocks();
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
        /// Extract text blocks as list of (x0, y0, x1, y1, text, blockNo, blockType).
        /// Corresponds to Python TextPage.extractBLOCKS().
        /// Faithfully ported from extra.i extractBLOCKS.
        /// </summary>
        public List<(float x0, float y0, float x1, float y1, string text, int blockNo, int blockType)> ExtractBlocks()
        {
            var lines = new List<(float, float, float, float, string, int, int)>();
            int block_n = -1;
            var tp_rect = new mupdf.FzRect(NativeStextPage.m_internal.mediabox);

            for (var block_iter = NativeStextPage.begin();
                 block_iter.m_internal != NativeStextPage.end().m_internal;
                 block_iter = block_iter.__increment__())
            {
                var block = block_iter.__ref__();
                block_n++;
                var blockrect = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);

                if (block.m_internal.type == mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                {
                    var res = mupdf.mupdf.fz_new_buffer(1024);
                    int last_char = 0;

                    for (var line_iter = block.begin();
                         line_iter.m_internal != block.end().m_internal;
                         line_iter = line_iter.__increment__())
                    {
                        var line = line_iter.__ref__();
                        var linerect = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);

                        for (var ch_iter = line.begin();
                             ch_iter.m_internal != line.end().m_internal;
                             ch_iter = ch_iter.__increment__())
                        {
                            var ch = ch_iter.__ref__();
                            var cbbox = Helpers.JM_char_bbox(line.m_internal, ch.m_internal);
                            if (!Helpers.JM_rects_overlap(tp_rect, cbbox)
                                && mupdf.mupdf.fz_is_infinite_rect(tp_rect) == 0)
                            {
                                continue;
                            }
                            mupdf.mupdf.fz_append_rune(res, ch.m_internal.c);
                            last_char = ch.m_internal.c;
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
                        string text = Encoding.UTF8.GetString(res.fz_buffer_extract());
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
        /// Extract text words as list of (x0, y0, x1, y1, word, blockNo, lineNo, wordNo).
        /// Corresponds to Python TextPage.extractWORDS().
        /// Faithfully ported from extra.i extractWORDS.
        /// </summary>
        public List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> ExtractWords(string delimiters = null)
        {
            var lines = new List<(float, float, float, float, string, int, int, int)>();
            int block_n = -1;
            var tp_rect = new mupdf.FzRect(NativeStextPage.m_internal.mediabox);
            var buff = mupdf.mupdf.fz_new_buffer(64);
            var wbbox = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);
            int buflen = 0;
            int last_char_rtl = 0;

            for (var block_iter = NativeStextPage.begin();
                 block_iter.m_internal != NativeStextPage.end().m_internal;
                 block_iter = block_iter.__increment__())
            {
                var block = block_iter.__ref__();
                block_n++;
                if (block.m_internal.type != mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                    continue;

                int line_n = -1;
                for (var line_iter = block.begin();
                     line_iter.m_internal != block.end().m_internal;
                     line_iter = line_iter.__increment__())
                {
                    var line = line_iter.__ref__();
                    line_n++;
                    int word_n = 0;
                    mupdf.mupdf.fz_clear_buffer(buff);
                    buflen = 0;

                    for (var ch_iter = line.begin();
                         ch_iter.m_internal != line.end().m_internal;
                         ch_iter = ch_iter.__increment__())
                    {
                        var ch = ch_iter.__ref__();
                        var cbbox = Helpers.JM_char_bbox(line.m_internal, ch.m_internal);
                        if (!Helpers.JM_rects_overlap(tp_rect, cbbox)
                            && mupdf.mupdf.fz_is_infinite_rect(tp_rect) == 0)
                        {
                            continue;
                        }
                        if (buflen == 0 && ch.m_internal.c == 0x200d)
                            continue;

                        bool word_delimiter = JM_is_word_delimiter(ch.m_internal.c, delimiters);
                        int this_char_rtl = JM_is_rtl_char(ch.m_internal.c) ? 1 : 0;

                        if (word_delimiter || this_char_rtl != last_char_rtl)
                        {
                            if (buflen == 0 && word_delimiter)
                                continue;
                            if (mupdf.mupdf.fz_is_empty_rect(wbbox) == 0)
                            {
                                string w = Encoding.UTF8.GetString(buff.fz_buffer_extract());
                                lines.Add((wbbox.x0, wbbox.y0, wbbox.x1, wbbox.y1, w, block_n, line_n, word_n));
                                word_n++;
                                wbbox = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);
                            }
                            mupdf.mupdf.fz_clear_buffer(buff);
                            buflen = 0;
                            if (word_delimiter)
                                continue;
                        }
                        mupdf.mupdf.fz_append_rune(buff, ch.m_internal.c);
                        last_char_rtl = this_char_rtl;
                        buflen++;
                        wbbox = mupdf.mupdf.fz_union_rect(wbbox, cbbox);
                    }
                    if (buflen > 0 && mupdf.mupdf.fz_is_empty_rect(wbbox) == 0)
                    {
                        string w = Encoding.UTF8.GetString(buff.fz_buffer_extract());
                        lines.Add((wbbox.x0, wbbox.y0, wbbox.x1, wbbox.y1, w, block_n, line_n, word_n));
                        word_n++;
                        wbbox = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);
                    }
                    buflen = 0;
                }
            }
            return lines;
        }

        /// <summary>
        /// Extract text in HTML format.
        /// Corresponds to Python TextPage.extractHTML().
        /// </summary>
        public string ExtractHtml()
        {
            var res = mupdf.mupdf.fz_new_buffer(1024);
            var output = new mupdf.FzOutput(res);
            mupdf.mupdf.fz_print_stext_page_as_html(output, NativeStextPage, 0);
            output.fz_close_output();
            return Encoding.UTF8.GetString(res.fz_buffer_extract());
        }

        /// <summary>
        /// Extract text in XHTML format.
        /// Corresponds to Python TextPage.extractXHTML().
        /// </summary>
        public string ExtractXhtml()
        {
            var res = mupdf.mupdf.fz_new_buffer(1024);
            var output = new mupdf.FzOutput(res);
            mupdf.mupdf.fz_print_stext_page_as_xhtml(output, NativeStextPage, 0);
            output.fz_close_output();
            return Encoding.UTF8.GetString(res.fz_buffer_extract());
        }

        /// <summary>
        /// Extract text in XML format.
        /// Corresponds to Python TextPage.extractXML().
        /// </summary>
        public string ExtractXml()
        {
            var res = mupdf.mupdf.fz_new_buffer(1024);
            var output = new mupdf.FzOutput(res);
            mupdf.mupdf.fz_print_stext_page_as_xml(output, NativeStextPage, 0);
            output.fz_close_output();
            return Encoding.UTF8.GetString(res.fz_buffer_extract());
        }

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
        /// Extract text as a dictionary (page level) with full block/line/span detail.
        /// Corresponds to Python TextPage.extractDICT(raw=False).
        /// Faithfully ported from extra.i _as_dict / JM_make_text_block / JM_make_spanlist.
        /// </summary>
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
        /// Extract text as a raw dictionary with character-level detail (no span text merging).
        /// Corresponds to Python TextPage.extractRAWDICT(raw=True).
        /// </summary>
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

        /// <summary>
        /// Extract text in JSON format.
        /// Corresponds to Python TextPage.extractJSON().
        /// </summary>
        public string ExtractJson(bool sort = false)
        {
            var dict = ExtractDict(sort);
            return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Extract raw dictionary in JSON format.
        /// Corresponds to Python TextPage.extractRAWJSON().
        /// </summary>
        public string ExtractRawJson(bool sort = false)
        {
            var dict = ExtractRawDict(sort);
            return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Return a list with image meta information.
        /// Corresponds to Python TextPage.extractIMGINFO().
        /// </summary>
        public List<Dictionary<string, object>> ExtractImgInfo(bool hashes = false)
        {
            int block_n = -1;
            var rc = new List<Dictionary<string, object>>();
            for (var block_iter = NativeStextPage.begin();
                 block_iter.m_internal != NativeStextPage.end().m_internal;
                 block_iter = block_iter.__increment__())
            {
                var block = block_iter.__ref__();
                block_n++;
                if (block.m_internal.type == mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                    continue;

                var wrappedBlock = block;
                var img = wrappedBlock.i_image();
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
                var matrix = wrappedBlock.i_transform();
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

        // ─── Search ─────────────────────────────────────────────────────

        /// <summary>
        /// Search for a string. Returns list of Quad hit rectangles.
        /// Corresponds to Python TextPage.search(..., quads=True).
        /// </summary>
        public List<Quad> Search(string needle, int maxHits = 16)
        {
            var result = new List<Quad>();
            if (string.IsNullOrEmpty(needle)) return result;
            var quads = NativeStextPage.search_stext_page(needle, null, maxHits);
            foreach (var q in quads)
            {
                result.Add(new Quad(
                    new Point(q.ul.x, q.ul.y),
                    new Point(q.ur.x, q.ur.y),
                    new Point(q.ll.x, q.ll.y),
                    new Point(q.lr.x, q.lr.y)));
                if (result.Count >= maxHits) break;
            }
            return result;
        }

        /// <summary>
        /// Search for a string; returns axis-aligned rectangles with the same merge pass as PyMuPDF
        /// <c>TextPage.search(..., quads=False)</c> (join overlapping hits on the same baseline).
        /// </summary>
        public List<Rect> SearchRects(string needle, int maxHits = 16)
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
                if (Math.Abs(v1.Y1 - v2.Y1) > Constants.Epsilon || (v1 & v2).IsEmpty)
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

        /// <summary>
        /// Extract text inside a rectangle.
        /// Corresponds to Python TextPage.extractTextbox().
        /// </summary>
        public string ExtractTextbox(Rect rect)
        {
            var area = new mupdf.FzRect((float)rect.X0, (float)rect.Y0, (float)rect.X1, (float)rect.Y1);
            return NativeStextPage.fz_copy_rectangle(area, 0);
        }

        /// <summary>
        /// Extract text between two selection points.
        /// Corresponds to Python TextPage.extractSelection().
        /// </summary>
        public string ExtractSelection(Point pointA, Point pointB)
        {
            var a = new mupdf.FzPoint((float)pointA.X, (float)pointA.Y);
            var b = new mupdf.FzPoint((float)pointB.X, (float)pointB.Y);
            return NativeStextPage.fz_copy_selection(a, b, 0);
        }

        // ─── Internal: Faithful port of extra.i _as_dict ────────────────

        private void JM_make_textpage_dict(
            mupdf.FzStextPage tp,
            Dictionary<string, object> page_dict,
            bool raw)
        {
            var text_buffer = mupdf.mupdf.fz_new_buffer(128);
            var block_list = new List<Dictionary<string, object>>();
            var tp_rect = new mupdf.FzRect(tp.m_internal.mediabox);
            var block = tp.m_internal.first_block;
            int block_n = -1;
            JM_as_dict(block_list, block, text_buffer, raw, tp_rect, block_n);
            page_dict["blocks"] = block_list;
        }

        /// <summary>
        /// Recursive function for output by blocks as identified by MuPDF SEGMENT logic.
        /// Port of extra.i _as_dict().
        /// </summary>
        private int JM_as_dict(
            List<Dictionary<string, object>> block_list,
            mupdf.fz_stext_block block,
            mupdf.FzBuffer text_buffer,
            bool raw,
            mupdf.FzRect tp_rect,
            int block_n)
        {
            while (block != null)
            {
                if (block.type == mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                {
                    if (Helpers.JM_rects_overlap(tp_rect, new mupdf.FzRect(block.bbox))
                        || mupdf.mupdf.fz_is_infinite_rect(tp_rect) != 0)
                    {
                        var block_dict = new Dictionary<string, object>();
                        block_n++;
                        block_dict["type"] = block.type;
                        block_dict["number"] = block_n;
                        JM_make_text_block(block, block_dict, raw, text_buffer, tp_rect);
                        block_list.Add(block_dict);
                    }
                }
                else if (block.type == mupdf.mupdf.FZ_STEXT_BLOCK_IMAGE)
                {
                    if (mupdf.mupdf.fz_contains_rect(tp_rect, new mupdf.FzRect(block.bbox)) != 0
                        || mupdf.mupdf.fz_is_infinite_rect(tp_rect) != 0)
                    {
                        var block_dict = new Dictionary<string, object>();
                        block_n++;
                        block_dict["type"] = block.type;
                        block_dict["number"] = block_n;
                        block_dict["bbox"] = new float[] { block.bbox.x0, block.bbox.y0, block.bbox.x1, block.bbox.y1 };
                        JM_make_image_block(block, block_dict);
                        block_list.Add(block_dict);
                    }
                }
                block = block.next;
            }
            return block_n;
        }

        /// <summary>
        /// Port of extra.i JM_make_text_block().
        /// </summary>
        private void JM_make_text_block(
            mupdf.fz_stext_block block,
            Dictionary<string, object> block_dict,
            bool raw,
            mupdf.FzBuffer buff,
            mupdf.FzRect tp_rect)
        {
            var line_list = new List<Dictionary<string, object>>();
            var block_rect = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_EMPTY);

            var wrappedBlock = new mupdf.FzStextBlock(block);
            for (var line_iter = wrappedBlock.begin();
                 line_iter.m_internal != wrappedBlock.end().m_internal;
                 line_iter = line_iter.__increment__())
            {
                var line = line_iter.__ref__();
                var fzLine = line.m_internal;

                var intersect = mupdf.mupdf.fz_intersect_rect(tp_rect, new mupdf.FzRect(fzLine.bbox));
                if (mupdf.mupdf.fz_is_empty_rect(intersect) != 0
                    && mupdf.mupdf.fz_is_infinite_rect(tp_rect) == 0)
                {
                    continue;
                }

                var line_dict = new Dictionary<string, object>();
                var line_rect = JM_make_spanlist(line_dict, line, raw, buff, tp_rect);

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
            mupdf.FzStextLine line,
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

            for (var ch_iter = line.begin();
                 ch_iter.m_internal != line.end().m_internal;
                 ch_iter = ch_iter.__increment__())
            {
                var ch = ch_iter.__ref__();
                var fzCh = ch.m_internal;
                var fzLine = line.m_internal;

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
                            span["text"] = Encoding.UTF8.GetString(
                                buff.fz_buffer_extract());
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
                    mupdf.mupdf.fz_append_rune(buff, fzCh.c);
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
                    span["text"] = Encoding.UTF8.GetString(
                        buff.fz_buffer_extract());
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
            mupdf.fz_stext_block block,
            Dictionary<string, object> block_dict)
        {
            var wrappedBlock = new mupdf.FzStextBlock(block);
            var image = wrappedBlock.i_image();
            var transform = wrappedBlock.i_transform();

            int w = image.w();
            int h = image.h();
            var cs = image.colorspace();
            int n = mupdf.mupdf.fz_colorspace_n(cs);

            byte[] imageBytes;
            string ext;
            try
            {
                var compBuf = mupdf.mupdf.fz_compressed_image_buffer(image);
                if (compBuf.m_internal != null)
                {
                    int imgType = compBuf.m_internal.params_.type;
                    ext = JM_image_extension(imgType);
                    if (imgType < mupdf.mupdf.FZ_IMAGE_BMP || imgType == mupdf.mupdf.FZ_IMAGE_JBIG2)
                    {
                        var pngBuf = mupdf.mupdf.fz_new_buffer_from_image_as_png(image, new mupdf.FzColorParams(mupdf.mupdf.fz_default_color_params));
                        imageBytes = pngBuf.fz_buffer_extract();
                        ext = "png";
                    }
                    else if (n == 4 && ext == "jpeg")
                    {
                        var jpgBuf = mupdf.mupdf.fz_new_buffer_from_image_as_jpeg(image, new mupdf.FzColorParams(mupdf.mupdf.fz_default_color_params), 95, 1);
                        imageBytes = jpgBuf.fz_buffer_extract();
                    }
                    else
                    {
                        var rawBuf = new mupdf.FzBuffer(compBuf.m_internal.buffer);
                        imageBytes = rawBuf.fz_buffer_extract();
                    }
                }
                else
                {
                    var pngBuf = mupdf.mupdf.fz_new_buffer_from_image_as_png(image, new mupdf.FzColorParams(mupdf.mupdf.fz_default_color_params));
                    imageBytes = pngBuf.fz_buffer_extract();
                    ext = "png";
                }
            }
            catch
            {
                imageBytes = Array.Empty<byte>();
                ext = "png";
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

        // ─── IDisposable ────────────────────────────────────────────────

        public void Dispose()
        {
            if (!_disposed)
            {
                _nativeStp?.Dispose();
                _nativeStp = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~TextPage() { Dispose(); }
    }
}
