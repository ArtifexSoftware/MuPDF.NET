using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MuPDF.NET
{
    /// <summary>
    /// Prepares PDF text spans for deferred output on pages of matching size (PyMuPDF <c>TextWriter</c>).
    /// </summary>
    /// <remarks>
    /// <para>PDF only. Decouples text preparation from <see cref="WriteText"/> on a <see cref="Page"/>.
    /// Alternative to <see cref="Page.InsertText"/> with per-span fonts, justified boxes, opacity, and morph rotation.</para>
    /// <para>Workflow: construct with a fixed page <see cref="Rect"/> → <see cref="Append"/> / <see cref="Appendv"/> /
    /// <see cref="FillTextbox"/> → <see cref="WriteText"/> (reusable on compatible pages).</para>
    /// </remarks>
    public class TextWriter : IDisposable
    {
        private bool _disposed;
        private Rect _rect;
        private float _opacity;
        private float[] _color;
        private mupdf.FzText _fzText;
        private Matrix _ctm;
        private Matrix _ictm;
        private int _spanCount;
        private readonly HashSet<Font> _usedFonts = new HashSet<Font>();
        /// <summary>Keep append fonts alive (Release GC can collect locals before <c>write_text</c>).</summary>
        private readonly HashSet<Font> _heldAppendFonts = new HashSet<Font>();
        private readonly List<AppendRecord> _appendRecords = new List<AppendRecord>();
        public bool ThisOwn { get; set; } = true;

        /// <summary>Page rectangle used for positioning; do not modify after construction.</summary>
        public Rect Rect => _rect;

        /// <summary>Default text opacity for <see cref="WriteText"/> (0 = opaque, 1 = fully transparent).</summary>
        public float Opacity
        {
            get => _opacity;
            set { if (value >= 0 && value <= 1) _opacity = value; }
        }

        /// <summary>Default text color (gray float, or RGB/CMYK component array, each 0–1).</summary>
        public float[] Color { get => _color; set => _color = value; }

        /// <summary>Maps page coordinates to internal text space.</summary>
        public Matrix Ctm { get => _ctm; set => _ctm = value; }

        /// <summary>Inverse of <see cref="Ctm"/>.</summary>
        public Matrix ICtm { get => _ictm; set => _ictm = value; }

        /// <summary>Monospaced fonts used on the page (for width repair after write).</summary>
        public List<Font> UsedFonts => _usedFonts.ToList();

        /// <summary>Bounding box of all text stored so far.</summary>
        public Rect TextRect { get; private set; }

        /// <summary>Cursor after the last character (bottom-right of last glyph).</summary>
        public Point LastPoint { get; private set; } = new Point(0, 0);

        /// <summary>Number of spans appended so far.</summary>
        public int SpanCount => _spanCount;

        /// <summary>
        /// Creates a writer bound to a page-sized rectangle.
        /// </summary>
        /// <param name="pageRect">Reference rectangle (must match target pages in <see cref="WriteText"/>).</param>
        /// <param name="opacity">Default transparency; ignored if outside 0..1.</param>
        /// <param name="color">Default color components (single gray or RGB/CMYK array).</param>
        public TextWriter(Rect pageRect, float opacity = 1, float[] color = null)
        {
            _rect = pageRect;
            _opacity = (opacity >= 0 && opacity <= 1) ? opacity : 1;
            _color = color ?? new float[] { 0 };
            _fzText = mupdf.mupdf.fz_new_text();
            _ctm = new Matrix(1, 0, 0, -1, 0, pageRect.Height);
            _ictm = _ctm.Inverted() ?? throw new InvalidOperationException("singular TextWriter ctm");
            TextRect = new Rect();
        }

        /// <summary>
        /// Appends horizontal text at <paramref name="pos"/>.
        /// </summary>
        /// <param name="pos">Bottom-left of the first character.</param>
        /// <param name="text">UTF-8 string to store.</param>
        /// <param name="font">Font (default Helvetica-like built-in).</param>
        /// <param name="fontSize">Font size in points.</param>
        /// <param name="language">ISO 639 language tag (reserved; currently unused by MuPDF).</param>
        /// <param name="rightToLeft">Reverse string for RTL scripts after <see cref="CleanRtl"/>.</param>
        /// <param name="smallCaps">Use small-cap glyphs when available in the font.</param>
        /// <returns>Pixel width of <paramref name="text"/> at <paramref name="fontSize"/>.</returns>
        /// <exception cref="ValueErrorException">Font is not writable (<see cref="Font.IsWritable"/>).</exception>
        public float Append(Point pos, string text, Font font = null, float fontSize = 11,
            string language = null, bool rightToLeft = false, int smallCaps = 0,
            string fontname = null, string fontfile = null)
            => Append(pos, text, font, fontSize, language, rightToLeft ? 1 : 0, smallCaps, fontname, fontfile);

        /// <summary>Legacy overload with integer RTL flag.</summary>
        public float Append(Point pos, string text, Font font, float fontSize,
            string language, int rightToLeft, int smallCaps = 0,
            string fontname = null, string fontfile = null)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var f = font ?? new Font(fontname ?? "helv", fontfile);
            if (!f.IsWritable)
                throw new ValueErrorException($"Unsupported font '{f.Name}'.");

            if (rightToLeft != 0)
            {
                text = CleanRtl(text);
                char[] chars = text.ToCharArray();
                Array.Reverse(chars);
                text = new string(chars);
            }

            _appendRecords.Add(new AppendRecord(pos, text, f, fontSize, language, smallCaps, fontname, fontfile));
            return AppendCore(pos, text, f, fontSize, language, smallCaps);
        }

        private float AppendCore(Point pos, string text, Font f, float fontsize, string language, int smallCaps)
        {
            var p = pos * _ictm;
            var trm = mupdf.mupdf.fz_make_matrix(fontsize, 0, 0, fontsize, (float)p.X, (float)p.Y);
            int lang = (int)mupdf.mupdf.fz_text_language_from_string(language);
            mupdf.FzMatrix newTrm = smallCaps == 0
                ? _fzText.fz_show_string(
                    f.NativeFont, trm, text, 0, 0,
                    mupdf.fz_bidi_direction.FZ_BIDI_LTR, (mupdf.fz_text_language)lang)
                : ShowStringSmallCaps(_fzText, f, trm, text, lang);
            _heldAppendFonts.Add(f);

            LastPoint = new Point(newTrm.e, newTrm.f) * _ctm;
            using var stroke = new mupdf.FzStrokeState();
            var br = _fzText.fz_bound_text(stroke, new mupdf.FzMatrix());
            TextRect = new Rect(br.x0, br.y0, br.x1, br.y1) * _ctm;
            _spanCount++;
            if (f.IsMonospaced)
                _usedFonts.Add(f);

            return f.TextLength(text, fontsize, language, smallCaps: smallCaps);
        }

        /// <summary>
        /// Appends vertical top-to-bottom text (one glyph per line step).
        /// </summary>
        /// <param name="pos">Start position (bottom-left of first character).</param>
        /// <param name="text">Characters to write downward.</param>
        /// <param name="font">Font to use.</param>
        /// <param name="fontSize">Font size in points.</param>
        /// <param name="language">ISO 639 language tag (reserved).</param>
        /// <param name="smallCaps">Use small-cap glyphs when available.</param>
        public (Rect TextRect, Point LastPoint) Appendv(Point pos, string text, Font font = null, float fontSize = 11,
            string language = null, bool smallCaps = false)
        {
            if (string.IsNullOrEmpty(text))
                return (TextRect, LastPoint);
            float lineHeight = fontSize * 1.2f;
            int sc = smallCaps ? 1 : 0;
            foreach (char c in text)
            {
                Append(pos, c.ToString(), font, fontSize, language, 0, sc);
                pos = new Point(pos.X, pos.Y + lineHeight);
            }
            return (TextRect, LastPoint);
        }

        /// <summary>Obsolete name for <see cref="Appendv"/>.</summary>
        [Obsolete("Use Appendv for vertical text.")]
        public (Rect TextRect, Point LastPoint) AppendLine(Point pos, string text, Font font = null, float fontSize = 11,
            string fontname = null)
            => Appendv(pos, text, font, fontSize);

        /// <summary>Published call pattern: <c>FillTextbox(rect, text, font, pos: …)</c> (e.g. barcode labels).</summary>
        /// <returns>Overflow lines as <c>(text, length)</c> tuples that did not fit.</returns>
        public List<(string text, float length)> FillTextbox(Rect rect, string text, Font font, Point pos) =>
            FillTextbox(rect, text, font, pos, fontsize: 11);

        /// <summary>
        /// Fills a rectangle with wrapped horizontal text (convenience over <see cref="Append"/>).
        /// </summary>
        /// <returns>Overflow lines as <c>(text, length)</c> tuples that did not fit.</returns>
        public List<(string text, float length)> FillTextbox(Rect rect, string text, Font font = null, Point pos = null,
            float fontsize = 11, int align = 0, bool rightToLeft = false, bool rtl = false,
            bool? warn = null, bool smallCaps = false, string fontname = null, float? lineheight = null,
            float lineHeight = 0)
        {
            float? lh = lineheight ?? (lineHeight > 0 ? lineHeight : (float?)null);
            bool rtlFlag = rightToLeft || rtl;
            return FillTextboxLines(rect, text, pos, font, fontsize, align, rtlFlag, warn, fontname, lh, smallCaps ? 1 : 0)
                .Select(x => (x.line, x.tl))
                .ToList();
        }

        /// <summary>Published overload when <paramref name="fontSize"/> is specified without <paramref name="fontsize"/>.</summary>
        public List<(string text, float length)> FillTextbox(Rect rect, string text, Font font, float fontSize,
            Point pos = null, int align = 0, bool rightToLeft = false, bool rtl = false,
            bool? warn = null, bool smallCaps = false, string fontname = null, float? lineheight = null,
            float lineHeight = 0) =>
            FillTextbox(rect, text, font, pos, fontSize, align, rightToLeft, rtl, warn, smallCaps, fontname, lineheight, lineHeight);

        /// <summary>Same as primary <see cref="FillTextbox"/> when <paramref name="pos"/> precedes <paramref name="font"/>.</summary>
        public List<(string text, float length)> FillTextbox(Rect rect, string text, Point pos, Font font,
            float fontsize = 11, int align = 0, bool rightToLeft = false, bool rtl = false,
            bool? warn = null, bool smallCaps = false, string fontname = null, float? lineheight = null,
            float lineHeight = 0) =>
            FillTextbox(rect, text, font, pos, fontsize, align, rightToLeft, rtl, warn, smallCaps, fontname, lineheight, lineHeight);

        private List<(string line, float tl)> FillTextboxLines(Rect rect, string text, Point pos,
            Font font, float fontsize, int align, bool rightToLeft,
            bool? warn, string fontname, float? lineheight, int smallCaps)
        {
            if (string.IsNullOrEmpty(text)) return new List<(string line, float tl)>();

            rect = new Rect(rect);
            if (rect.IsEmpty)
                throw new ValueErrorException("fill rect must not empty.");

            var f = font ?? new Font(fontName: fontname ?? "helv");
            _heldAppendFonts.Add(f);

            float Textlen(string x) => f.TextLength(x, fontsize);
            float[] CharLengths(string x) => f.CharLengths(x, fontsize);

            // tolerance = fontsize * 0.2  # extra distance to left border
            float tolerance = fontsize * 0.2f;
            // space_len = textlen(" ")
            float spaceLen = Textlen(" ");
            // std_width = rect.width - tolerance
            float stdWidth = (float)rect.Width - tolerance;
            // std_start = rect.x0 + tolerance
            float stdStart = (float)rect.X0 + tolerance;

            void AppendThis(Point start, string t)
            {
                Append(start, t, f, fontsize, null, 0, smallCaps);
            }

            void OutputJustify(Point start, string line)
            {
                // """Justified output of a line."""
                // words = [w for w in line.split(" ") if w != ""]
                string[] words = line.Split(' ').Where(w => w != "").ToArray();
                int nwords = words.Length;
                if (nwords == 0)
                    return;
                if (nwords == 1)
                {
                    // append_this(start, words[0])
                    AppendThis(start, words[0]);
                    return;
                }
                // tl = sum([textlen(w) for w in words])
                float tl = words.Sum(Textlen);
                // gaps = nwords - 1
                int gaps = nwords - 1;
                // gapl = (std_width - tl) / gaps
                float gapl = (stdWidth - tl) / gaps;
                foreach (string w in words)
                {
                    // _, lp = append_this(start, w)
                    AppendThis(start, w);
                    // start.x = lp.x + gapl
                    start.X = LastPoint.X + gapl;
                }
            }

            (List<string> nwords, List<float> wordLengths) NormWords(float width, List<string> words)
            {
                // """Cut any word in pieces no longer than 'width'."""
                var nwordsOut = new List<string>();
                var wordLengthsOut = new List<float>();
                foreach (string w in words)
                {
                    var wlLst = new List<float>(CharLengths(w));
                    float wl = wlLst.Sum();
                    if (wl <= width)
                    {
                        nwordsOut.Add(w);
                        wordLengthsOut.Add(wl);
                        continue;
                    }
                    int n = wlLst.Count;
                    string remain = w;
                    while (n > 0)
                    {
                        wl = wlLst.Take(n).Sum();
                        if (wl <= width)
                        {
                            nwordsOut.Add(remain.Substring(0, n));
                            wordLengthsOut.Add(wl);
                            remain = remain.Substring(n);
                            wlLst = wlLst.Skip(n).ToList();
                            n = wlLst.Count;
                        }
                        else
                            n--;
                    }
                }
                return (nwordsOut, wordLengthsOut);
            }

            float asc = f.Ascender;
            float dsc = f.Descender;
            float lheight;
            // if not lineheight:
            if (lineheight == null)
            {
                // if asc - dsc <= 1: lheight = 1.2
                // else: lheight = asc - dsc
                lheight = (asc - dsc <= 1) ? 1.2f : asc - dsc;
            }
            else
                lheight = lineheight.Value;

            // LINEHEIGHT = fontsize * lheight
            float LINEHEIGHT = fontsize * lheight;
            // width = std_width
            float width = stdWidth;

            // starting point of text
            Point start;
            if (pos != null)
                start = new Point(pos);
            else
                // pos = rect.tl + (tolerance, fontsize * asc)
                start = rect.TopLeft + new Point(tolerance, fontsize * asc);
            if (!rect.Contains(start))
                throw new ValueErrorException("Text must start in rectangle.");

            // calculate displacement factor for alignment
            float factor;
            if (align == Constants.TextAlignCenter)
                factor = 0.5f;
            else if (align == Constants.TextAlignRight)
                factor = 1.0f;
            else
                factor = 0;

            // split in lines if just a string was given
            // textlines = text.splitlines()
            string[] textlines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            // max_lines = int((rect.y1 - pos.y) / LINEHEIGHT) + 1
            int maxLines = (int)(((float)rect.Y1 - start.Y) / LINEHEIGHT) + 1;

            // new_lines = []
            var newLines = new List<(string line, float tl)>();
            // no_justify = []
            var noJustify = new List<int>();

            for (int i = 0; i < textlines.Length; i++)
            {
                string line = textlines[i];
                // if line in ("", " "):
                if (line == "" || line == " ")
                {
                    newLines.Add((line, spaceLen));
                    width = (float)rect.Width - tolerance;
                    noJustify.Add(newLines.Count - 1);
                    continue;
                }
                // if i == 0: width = rect.x1 - pos.x
                if (i == 0)
                    width = (float)(rect.X1 - start.X);
                else
                    width = (float)rect.Width - tolerance;

                // if right_to_left:  # reverses Arabic / Hebrew text front to back
                if (rightToLeft)
                    line = CleanRtl(line);
                float tl = Textlen(line);
                if (tl <= width)
                {
                    newLines.Add((line, tl));
                    noJustify.Add(newLines.Count - 1);
                    continue;
                }

                // words = line.split(" ")
                var words = line.Split(' ').ToList();
                // words, word_lengths = norm_words(width, words)
                (words, var wordLengths) = NormWords(width, words);

                int n = words.Count;
                while (true)
                {
                    // line0 = " ".join(words[:n])
                    string line0 = string.Join(" ", words.Take(n));
                    // wl = sum(word_lengths[:n]) + space_len * (n - 1)
                    float wl = wordLengths.Take(n).Sum() + spaceLen * (n - 1);
                    if (wl <= width)
                    {
                        newLines.Add((line0, wl));
                        words = words.Skip(n).ToList();
                        wordLengths = wordLengths.Skip(n).ToList();
                        n = words.Count;
                    }
                    else
                        n--;

                    if (words.Count == 0)
                        break;
                    if (n == 0)
                        throw new InvalidOperationException("fill_textbox: cannot fit word");
                }
            }

            // nlines = len(new_lines)
            int nlines = newLines.Count;
            if (nlines > maxLines)
            {
                string msg = $"Only fitting {maxLines} of {nlines} lines.";
                if (warn == true)
                    Helpers.message("Warning: " + msg);
                else if (warn == false)
                    throw new ValueErrorException(msg);
            }

            // no_justify += [len(new_lines) - 1]
            noJustify.Add(newLines.Count - 1);

            for (int i = 0; i < maxLines; i++)
            {
                if (newLines.Count == 0)
                    break;

                // line, tl = new_lines.pop(0)
                var (line, tl) = newLines[0];
                newLines.RemoveAt(0);

                // if right_to_left:  # Arabic, Hebrew
                if (rightToLeft)
                    line = new string(line.Reverse().ToArray());

                // if i == 0: start = pos
                if (i == 0)
                    start = pos != null ? new Point(pos) : rect.TopLeft + new Point(tolerance, fontsize * asc);

                // if align == TEXT_ALIGN_JUSTIFY and i not in no_justify and tl < std_width:
                if (align == Constants.TextAlignJustify && !noJustify.Contains(i) && tl < stdWidth)
                {
                    OutputJustify(start, line);
                    start.X = stdStart;
                    start.Y += LINEHEIGHT;
                    continue;
                }

                // if i > 0 or pos.x == std_start:
                if (i > 0 || Math.Abs(start.X - stdStart) < 1e-6)
                    start.X += (width - tl) * factor;

                AppendThis(start, line);
                start.X = stdStart;
                start.Y += LINEHEIGHT;
            }

            return newLines;
        }

        /// <summary>
        /// Writes prepared text onto a PDF page of the same size as <see cref="Rect"/>.
        /// </summary>
        /// <param name="page">Target page.</param>
        /// <param name="color">Override <see cref="Color"/> for this output.</param>
        /// <param name="opacity">Override <see cref="Opacity"/>; use -1 for the writer default.</param>
        /// <param name="overlay">1 = foreground (default), 0 = background.</param>
        /// <param name="morph">Rotation/transform as fixpoint plus matrix.</param>
        /// <param name="matrix">Direct transform (mutually exclusive with <paramref name="morph"/>).</param>
        /// <param name="renderMode">PDF <c>Tr</c> operator (0 fill, 3 invisible, etc.).</param>
        /// <param name="oc">Optional content group xref.</param>
        /// <param name="morphFix">Morph fixpoint when not using <see cref="Morph"/>.</param>
        /// <param name="morphMat">Morph matrix when not using <see cref="Morph"/>.</param>
        public void WriteText(Page page, float[] color = null, float opacity = -1, int overlay = 1,
            Morph morph = null, Matrix matrix = null, int renderMode = 0, int oc = 0,
            Point morphFix = null, Matrix morphMat = null)
        {
            (Point, Matrix)? morphTuple = null;
            if (morph != null)
                morphTuple = (morph.P, morph.M);
            else if (morphFix != null)
                morphTuple = (morphFix, morphMat);
            WriteTextCore(page, color, opacity, overlay, morphTuple, matrix, renderMode, oc);
        }

        private void WriteTextCore(Page page, float[] color, float opacity, int overlay,
            (Point, Matrix)? morph, Matrix matrix, int render_mode, int oc)
        {
            // CheckParent(page)
            Document doc = page.RequireParent();
            if (Math.Abs(_rect.Width - page.Rect.Width) > 1e-3
                || Math.Abs(_rect.Height - page.Rect.Height) > 1e-3
                || Math.Abs(_rect.X0 - page.Rect.X0) > 1e-3
                || Math.Abs(_rect.Y0 - page.Rect.Y0) > 1e-3)
                throw new ValueErrorException("incompatible page rect");
            if (morph != null)
            {
                var morphVal = morph.Value;
                if (morphVal.Item1 == null || morphVal.Item2 == null)
                    throw new ValueErrorException("morph must be (Point, Matrix) or None");
            }
            if (matrix != null && morph != null)
                throw new ValueErrorException("only one of matrix, morph is allowed");
            // if getattr(opacity, "__float__", None) is None or opacity == -1:
            if (opacity < 0 || Math.Abs(opacity + 1) < 1e-9f)
                opacity = _opacity;
            if (color == null)
                color = _color;

            // Use the document's borrowed PdfDocument and a fresh page wrapper (never dispose
            // pdfpage.doc() — that drops the shared pdf_document while the Document is open).
            mupdf.PdfDocument pdfDoc = doc.NativePdfDocument;
            mupdf.PdfPage pdfpage = Helpers.AsPdfPageFresh(page);
            try
            {
            (int maxAlp, int maxFonts) max_nums;
            string content;
            {
                float alpha = 1;
                if (opacity >= 0 && opacity < 1)
                    alpha = opacity;
                int ncol = 1;
                float[] dev_color = { 0, 0, 0, 0 };
                if (color != null && color.Length > 0)
                {
                    var (n, cseq) = Helpers.ColorFromSequence(color);
                    ncol = n;
                    dev_color = new float[4];
                    for (int i = 0; i < cseq.Length; i++)
                        dev_color[i] = (cseq[i] < 0 || cseq[i] > 1) ? 1f : cseq[i];
                }
                mupdf.FzColorspace colorspace = Helpers.DeviceColorspace(ncol);

                mupdf.PdfObj resources = mupdf.mupdf.pdf_new_dict(pdfDoc, 5);
                using mupdf.FzBuffer contents = mupdf.mupdf.fz_new_buffer(1024);
                using mupdf.FzDevice dev = mupdf.mupdf.pdf_new_pdf_device(pdfDoc, new mupdf.FzMatrix(), resources, contents);
                //log( '=== {dev_color!r=}')
                GCHandle colorPin = GCHandle.Alloc(dev_color, GCHandleType.Pinned);
                try
                {
                    var colorPtr = new mupdf.SWIGTYPE_p_float(colorPin.AddrOfPinnedObject(), false);
                    foreach (Font held in _heldAppendFonts)
                        GC.KeepAlive(held);
                    dev.fz_fill_text(
                        _fzText,
                        new mupdf.FzMatrix(),
                        colorspace,
                        colorPtr,
                        alpha,
                        new mupdf.FzColorParams(mupdf.mupdf.fz_default_color_params));
                    mupdf.mupdf.fz_close_device(dev);
                }
                finally
                {
                    colorPin.Free();
                }

                // copy generated resources into the one of the page
                max_nums = Helpers.JmMergeResources(pdfpage, resources);
                content = Helpers.JmEscapeStrFromBuffer(contents);
            }

            int max_alp = max_nums.maxAlp;
            int max_font = max_nums.maxFonts;
            string[] old_cont_lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            string optcont = page._get_optional_content(oc);
            string bdc, emc;
            if (optcont != null)
            {
                bdc = $"/OC /{optcont} BDC";
                emc = "EMC";
            }
            else
                bdc = emc = "";

            var new_cont_lines = new List<string> { "q" };
            if (!string.IsNullOrEmpty(bdc))
                new_cont_lines.Add(bdc);

            Point cb = page.CropBoxPosition;
            float delta;
            if (page.Rotation == 90 || page.Rotation == 270)
                delta = page.Rect.Height - page.Rect.Width;
            else
                delta = 0;
            Rect mb = page.MediaBox;
            if (cb.X != 0 || cb.Y != 0 || mb.Y0 != 0 || Math.Abs(delta) > 1e-9)
                new_cont_lines.Add("1 0 0 1 " + Helpers.FormatPdfReals(cb.X, cb.Y + mb.Y0 - delta) + " cm");

            if (morph != null)
            {
                Point p = morph.Value.Item1 * _ictm;
                Matrix deltaM = new Matrix(1, 1);
                deltaM.Pretranslate(p.X, p.Y);
                Matrix invDelta = deltaM.Inverted() ?? throw new ValueErrorException("singular morph matrix");
                matrix = invDelta * morph.Value.Item2 * deltaM;
            }
            if (morph != null || matrix != null)
                new_cont_lines.Add(Helpers.FormatPdfReals(matrix.A, matrix.B, matrix.C, matrix.D, matrix.E, matrix.F) + " cm");

            foreach (string rawLine in old_cont_lines)
            {
                string line = rawLine;
                if (line.EndsWith(" cm", StringComparison.Ordinal))
                    continue;
                if (line == "BT")
                {
                    new_cont_lines.Add(line);
                    new_cont_lines.Add($"{render_mode} Tr");
                    continue;
                }
                if (line.EndsWith(" gs", StringComparison.Ordinal))
                {
                    int alp = int.Parse(line.Split()[0].Substring(4), CultureInfo.InvariantCulture) + max_alp;
                    line = $"/Alp{alp} gs";
                }
                else if (line.EndsWith(" Tf", StringComparison.Ordinal))
                {
                    string[] temp = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    float fsize = float.Parse(temp[1], CultureInfo.InvariantCulture);
                    string w;
                    if (render_mode != 0)
                        w = Helpers.FormatPdfReal(fsize * 0.05f) + " w";
                    else
                        w = "1 w";
                    new_cont_lines.Add(w);
                    int font = int.Parse(temp[0].Substring(2), CultureInfo.InvariantCulture) + max_font;
                    line = string.Join(" ", new[] { $"/F{font}" }.Concat(temp.Skip(1)));
                }
                else if (line.EndsWith(" rg", StringComparison.Ordinal))
                    new_cont_lines.Add(line.Replace("rg", "RG"));
                else if (line.EndsWith(" g", StringComparison.Ordinal))
                    new_cont_lines.Add(line.Replace(" g", " G"));
                else if (line.EndsWith(" k", StringComparison.Ordinal))
                    new_cont_lines.Add(line.Replace(" k", " K"));
                new_cont_lines.Add(line);
            }
            if (!string.IsNullOrEmpty(emc))
                new_cont_lines.Add(emc);
            new_cont_lines.Add("Q\n");
            byte[] contentBytes = Encoding.UTF8.GetBytes(string.Join("\n", new_cont_lines));
            Tools.InsertContents(page, contentBytes, overlay == 1);
            foreach (Font font in _usedFonts)
                RepairMonoFont(page, font);
            }
            finally
            {
                pdfpage.Dispose();
                page.DisposeCachedPdfPage();
                doc.InvalidatePageTree();
            }
        }

        private sealed class AppendRecord
        {
            public AppendRecord(Point pos, string text, Font font, float fontsize, string language,
                int smallCaps, string fontname, string fontfile)
            {
                Pos = pos;
                Text = text;
                Font = font;
                Fontsize = fontsize;
                Language = language;
                SmallCaps = smallCaps;
                Fontname = fontname;
                Fontfile = fontfile;
            }

            public Point Pos { get; }
            public string Text { get; }
            public Font Font { get; }
            public float Fontsize { get; }
            public string Language { get; }
            public int SmallCaps { get; }
            public string Fontname { get; }
            public string Fontfile { get; }
        }

        /// <summary>PyMuPDF <c>repair_mono_font</c> (<c>src/__init__.py</c>).</summary>
        internal static void RepairMonoFont(Page page, Font font)
        {
            if (!font.IsMonospaced)  // font not flagged as monospaced
                return;
            Document doc = page.Parent;
            if (doc == null)
                return;
            var fontlist = page.GetFonts();  // list of fonts on page
            var xrefs = new HashSet<int>();  // list of objects referring to font
            foreach (var f in fontlist)
            {
                if (f.baseName == font.Name && f.name.StartsWith("F") && f.encoding.StartsWith("Identity"))
                    xrefs.Add(f.xref);
            }
            if (xrefs.Count == 0)  // our font does not occur
                return;
            int width = (int)Math.Round(font.GlyphAdvance(32) * 1000);
            foreach (int xref in xrefs)
            {
                if (!Tools.SetFontWidth(doc, xref, width))
                {
                    // log(f"Cannot set width for '{font.Name}' in xref {xref}")
                }
            }
        }

        /// <summary>Clears all stored spans and resets <see cref="TextRect"/> / <see cref="LastPoint"/>.</summary>
        public void Reset()
        {
            _fzText?.Dispose();
            _fzText = mupdf.mupdf.fz_new_text();
            _spanCount = 0;
            _appendRecords.Clear();
            _heldAppendFonts.Clear();
            _usedFonts.Clear();
            TextRect = new Rect();
            LastPoint = new Point(0, 0);
        }

        /// <summary>Append text using small-cap glyphs (PyMuPDF <c>JM_show_string_cs</c>).</summary>
        private static mupdf.FzMatrix ShowStringSmallCaps(
            mupdf.FzText text, Font userFont, mupdf.FzMatrix trm, string s, int lang)
        {
            int i = 0;
            var language = (mupdf.fz_text_language)lang;
            while (i < s.Length)
            {
                using var outparams = new mupdf.ll_fz_chartorune_outparams();
                int step = mupdf.mupdf.ll_fz_chartorune_outparams_fn(s.Substring(i), outparams);
                i += step;
                int rune = outparams.rune;
                int gid = userFont.NativeFont.fz_encode_character_sc(rune);
                float adv;
                if (gid == 0)
                {
                    using var fallbackFont = new mupdf.FzFont();
                    int fallbackGid = userFont.NativeFont.fz_encode_character_with_fallback(rune, 0, lang, fallbackFont);
                    text.fz_show_glyph(
                        fallbackFont, trm, fallbackGid, rune, 0, 0,
                        mupdf.fz_bidi_direction.FZ_BIDI_LTR, language);
                    adv = mupdf.mupdf.fz_advance_glyph(fallbackFont, fallbackGid, 0);
                }
                else
                {
                    var font = userFont.NativeFont;
                    text.fz_show_glyph(font, trm, gid, rune, 0, 0,
                        mupdf.fz_bidi_direction.FZ_BIDI_LTR, language);
                    adv = mupdf.mupdf.fz_advance_glyph(font, gid, 0);
                }
                trm = trm.fz_pre_translate(adv, 0);
            }
            return trm;
        }

        /// <summary>Reorders Latin runs inside RTL text for PDF output (PyMuPDF <c>clean_rtl</c>).</summary>
        public static string CleanRtl(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string[] words = text.Split(' ');
            var idx = new List<int>();
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];
                if (!(w.Length < 2 || w.Max(c => c) > 255))
                {
                    char[] chars = w.ToCharArray();
                    Array.Reverse(chars);
                    words[i] = new string(chars);
                    idx.Add(i);
                }
            }
            var idx2 = new List<int>();
            for (int i = 0; i < idx.Count; i++)
            {
                if (idx2.Count == 0)
                    idx2.Add(idx[i]);
                else if (idx[i] > idx2[idx2.Count - 1] + 1)
                {
                    if (idx2.Count > 1)
                    {
                        int start = idx2[0], end = idx2[idx2.Count - 1];
                        Array.Reverse(words, start, end - start + 1);
                    }
                    idx2.Clear();
                    idx2.Add(idx[i]);
                }
                else
                    idx2.Add(idx[i]);
            }
            // Python clean_rtl does not reverse the final idx2 group here.
            return string.Join(" ", words);
        }

        // ─── IDisposable ────────────────────────────────────────────────

        /// <summary>Releases native text storage.</summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _fzText?.Dispose();
                _fzText = null;
                _heldAppendFonts.Clear();
                _appendRecords.Clear();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~TextWriter() => Dispose();

        public override string ToString() => $"TextWriter(spans={_spanCount}, rect={TextRect})";

        // ─── PyMuPDF API names (internal, same assembly) ─────────────────

        internal Rect text_rect => TextRect;
        internal Point last_point => LastPoint;
        internal (Rect text_rect, Point last_point) append(Point pos, string text, Font font = null, float fontsize = 11,
            string language = null, int rightToLeft = 0, int smallCaps = 0,
            string fontname = null, string fontfile = null)
        {
            Append(pos, text, font, fontsize, language, rightToLeft, smallCaps, fontname, fontfile);
            return (TextRect, LastPoint);
        }
        internal (Rect text_rect, Point last_point) appendv(Point pos, string text, Font font = null, float fontsize = 11,
            string language = null, bool small_caps = false)
            => Appendv(pos, text, font, fontsize, language, small_caps);
        internal List<string> fill_textbox(Rect rect, string text, Point pos = null,
            Font font = null, float fontsize = 11, int align = 0, bool rtl = false, bool rightToLeft = false,
            bool warn = false, float[] color = null, string fontname = null, float? lineheight = null)
        {
            bool? warnFlag = warn ? true : (bool?)null;
            return FillTextboxLines(rect, text, pos, font, fontsize, align, rtl || rightToLeft, warnFlag, fontname, lineheight, 0)
                .Select(x => x.line)
                .ToList();
        }
        internal void write_text(Page page, float[] color = null, float opacity = -1, int overlay = 1,
            (Point, Matrix)? morph = null, Matrix matrix = null, int render_mode = 0, int oc = 0)
            => WriteTextCore(page, color, opacity, overlay, morph, matrix, render_mode, oc);
        internal void writeText(Page page, float[] color = null, float opacity = -1, int overlay = 1,
            Morph morph = null, Matrix matrix = null, int renderMode = 0, int oc = 0,
            Point morphFix = null, Matrix morphMat = null)
            => WriteText(page, color, opacity, overlay, morph, matrix, renderMode, oc, morphFix, morphMat);
    }
}
