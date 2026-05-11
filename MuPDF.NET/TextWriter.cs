using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Stores text spans for later output on compatible PDF pages.
    /// </summary>
    public class TextWriter : IDisposable
    {
        private bool _disposed;
        private Rect _rect;
        private float _opacity;
        private float[] _color;
        private List<(Point pos, string text, Font font, float fontsize)> _textSpans;

        /// <summary>
        /// Rectangle of the TextWriter.
        /// </summary>
        public Rect Rect => _rect;
        /// <summary>
        /// Text opacity.
        /// </summary>
        public float Opacity { get => _opacity; set => _opacity = value; }
        /// <summary>
        /// Text color.
        /// </summary>
        public float[] Color { get => _color; set => _color = value; }

        /// <summary>
        /// Rectangle of the written text.
        /// </summary>
        public Rect TextRect { get; private set; }
        /// <summary>
        /// Last written point.
        /// </summary>
        public Point LastPoint { get; private set; } = new Point(0, 0);

        /// <summary>
        /// Number of text spans.
        /// </summary>
        public int SpanCount => _textSpans.Count;

        /// <summary>
        /// Create a TextWriter for a given page rectangle.
        /// </summary>
        public TextWriter(Rect pageRect, float opacity = 1, float[] color = null)
        {
            _rect = pageRect;
            _opacity = opacity;
            _color = color ?? new float[] { 0 };
            _textSpans = new List<(Point, string, Font, float)>();
            TextRect = new Rect();
        }

        /// <summary>
        /// Add text at a given point.
        /// <para>Returns the pixel width of the text.</para>
        /// </summary>
        public float Append(Point pos, string text, Font font = null, float fontsize = 11,
            string language = null, int rightToLeft = 0, int smallCaps = 0,
            string fontname = null, string fontfile = null)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var f = font ?? new Font(fontname ?? "helv");
            _textSpans.Add((pos, text, f, fontsize));

            float advance = f.TextLength(text, fontsize);
            LastPoint = new Point(pos.X + advance, pos.Y);

            var spanRect = new Rect(pos.X, pos.Y - fontsize, pos.X + advance, pos.Y + fontsize * 0.3);
            if (TextRect.IsEmpty)
                TextRect = spanRect;
            else
                TextRect = TextRect.IncludeRect(spanRect);

            return advance;
        }

        /// <summary>
        /// Add text and advance to the next line.
        /// </summary>
        public float AppendLine(Point pos, string text, Font font = null, float fontsize = 11,
            string fontname = null)
        {
            float advance = Append(pos, text, font, fontsize, fontname: fontname);
            LastPoint = new Point(pos.X, pos.Y + fontsize * 1.2);
            return advance;
        }

        /// <summary>
        /// Fill a rectangle with text, wrapping words that exceed the width.
        /// <para>Returns unwritten lines that did not fit in the rectangle.</para>
        /// </summary>
        public List<string> FillTextbox(Rect rect, string text, Point pos = null,
            Font font = null, float fontsize = 11, int align = 0, bool rightToLeft = false,
            bool warn = false, float[] color = null, string fontname = null,
            float? lineheight = null)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();

            var f = font ?? new Font(fontname ?? "helv");

            float tolerance = fontsize * 0.2f;
            float spaceLen = f.TextLength(" ", fontsize);
            float stdWidth = (float)rect.Width - tolerance;
            float stdStart = (float)rect.X0 + tolerance;

            float asc = f.Ascender;
            float dsc = f.Descender;
            float lheight;
            if (lineheight == null)
                lheight = (asc - dsc <= 1) ? 1.2f : asc - dsc;
            else
                lheight = lineheight.Value;
            float LINEHEIGHT = fontsize * lheight;

            float startY;
            float startX;
            if (pos != null)
            {
                startX = (float)pos.X;
                startY = (float)pos.Y;
            }
            else
            {
                startX = stdStart;
                startY = (float)rect.Y0 + fontsize * asc;
            }

            float factor;
            switch (align)
            {
                case 1: factor = 0.5f; break;
                case 2: factor = 1.0f; break;
                default: factor = 0; break;
            }

            string[] textlines = text.Split('\n');
            int maxLines = (int)(((float)rect.Y1 - startY) / LINEHEIGHT) + 1;

            var newLines = new List<(string text, float tl)>();
            var noJustify = new HashSet<int>();

            for (int i = 0; i < textlines.Length; i++)
            {
                string line = textlines[i];
                if (string.IsNullOrEmpty(line) || line == " ")
                {
                    newLines.Add((line, spaceLen));
                    noJustify.Add(newLines.Count - 1);
                    continue;
                }

                float width = (i == 0) ? (float)rect.X1 - startX : stdWidth;

                float tl = f.TextLength(line, fontsize);
                if (tl <= width)
                {
                    newLines.Add((line, tl));
                    noJustify.Add(newLines.Count - 1);
                    continue;
                }

                string[] words = line.Split(' ');
                var wordLengths = new List<float>();
                foreach (string w in words)
                    wordLengths.Add(f.TextLength(w, fontsize));

                int n = words.Length;
                var wordsList = new List<string>(words);
                var wlList = new List<float>(wordLengths);

                while (wordsList.Count > 0)
                {
                    n = wordsList.Count;
                    while (n > 0)
                    {
                        float wl = 0;
                        for (int j = 0; j < n; j++) wl += wlList[j];
                        wl += spaceLen * (n - 1);
                        if (wl <= width)
                        {
                            string line0 = string.Join(" ", wordsList.GetRange(0, n));
                            newLines.Add((line0, wl));
                            wordsList.RemoveRange(0, n);
                            wlList.RemoveRange(0, n);
                            width = stdWidth;
                            break;
                        }
                        n--;
                    }
                    if (n == 0 && wordsList.Count > 0)
                    {
                        newLines.Add((wordsList[0], wlList[0]));
                        wordsList.RemoveAt(0);
                        wlList.RemoveAt(0);
                        width = stdWidth;
                    }
                }
            }

            int nlines = newLines.Count;
            if (nlines > maxLines && warn)
                throw new InvalidOperationException($"Only fitting {maxLines} of {nlines} lines.");

            noJustify.Add(newLines.Count - 1);
            float x = startX;
            float y = startY;

            var rest = new List<string>();
            for (int i = 0; i < Math.Min(maxLines, newLines.Count); i++)
            {
                var (lineText, tl2) = newLines[i];
                float lineX = (i == 0) ? startX : stdStart;
                float lineWidth = (i == 0) ? (float)rect.X1 - startX : stdWidth;

                if (align == 3 && !noJustify.Contains(i) && tl2 < stdWidth)
                {
                    string[] jwords = lineText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (jwords.Length > 1)
                    {
                        float totalWordLen = 0;
                        foreach (string w in jwords) totalWordLen += f.TextLength(w, fontsize);
                        float gapLen = (stdWidth - totalWordLen) / (jwords.Length - 1);
                        float jx = lineX;
                        foreach (string w in jwords)
                        {
                            Append(new Point(jx, y), w, f, fontsize);
                            jx += f.TextLength(w, fontsize) + gapLen;
                        }
                        y += LINEHEIGHT;
                        continue;
                    }
                }

                lineX += (lineWidth - tl2) * factor;
                Append(new Point(lineX, y), lineText, f, fontsize);
                y += LINEHEIGHT;
            }

            for (int i = maxLines; i < newLines.Count; i++)
                rest.Add(newLines[i].text);

            return rest;
        }

        /// <summary>
        /// Write all accumulated text to a PDF page.
        /// </summary>
        public void WriteText(Page page, float opacity = -1, float[] color = null, int overlay = 1, int oc = 0, int renderMode = 0)
        {
            if (_textSpans.Count == 0) return;

            var pdf = page.Parent.NativePdfDocument;
            var pdfPage = page.NativePdfPage;

            // Register fonts under /Resources/Font (names F<xref>) as we encounter them.
            var fontResByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Build content stream
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("q");
            float op = opacity >= 0 ? opacity : _opacity;
            var col = color ?? _color;

            if (col != null && col.Length > 0)
            {
                if (col.Length == 1) sb.AppendLine($"{col[0]:F4} g");
                else if (col.Length == 3) sb.AppendLine($"{col[0]:F4} {col[1]:F4} {col[2]:F4} rg");
                else if (col.Length == 4) sb.AppendLine($"{col[0]:F4} {col[1]:F4} {col[2]:F4} {col[3]:F4} k");
            }

            foreach (var (pos, text, font, fontsize) in _textSpans)
            {
                var fname = font?.Name;
                if (string.IsNullOrEmpty(fname)) fname = "helv";
                if (!fontResByName.TryGetValue(fname, out var resKey))
                {
                    int xref = page.InsertFont(fname);
                    resKey = $"F{xref}";
                    fontResByName[fname] = resKey;
                }

                sb.AppendLine("BT");
                sb.AppendLine($"/{resKey} {fontsize:F1} Tf");
                sb.AppendLine($"{pos.X:F2} {_rect.Height - pos.Y:F2} Td");
                // GetPdfStr already returns a parenthesized PDF string literal.
                sb.AppendLine($"{Helpers.GetPdfStr(text)} Tj");
                sb.AppendLine("ET");
            }
            sb.AppendLine("Q");

            var content = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var buf = Helpers.BufferFromBytes(content);
            Helpers.JM_insert_contents(pdf, pdfPage.obj(), buf, overlay == 1);
        }

        /// <summary>
        /// Empty the TextWriter.
        /// </summary>
        public void Reset()
        {
            _textSpans.Clear();
            TextRect = new Rect();
            LastPoint = new Point(0, 0);
        }

        // ─── IDisposable ────────────────────────────────────────────────

        public void Dispose()
        {
            if (!_disposed) { _disposed = true; }
            GC.SuppressFinalize(this);
        }

        ~TextWriter() { Dispose(); }

        public override string ToString() => $"TextWriter(spans={_textSpans.Count}, rect={TextRect})";

        // Python/legacy compatibility aliases (mirrors _alias(TextWriter, ...)).
        public List<string> fill_textbox(Rect rect, string text, Point pos = null,
            Font font = null, float fontsize = 11, int align = 0, bool rightToLeft = false,
            bool warn = false, float[] color = null, string fontname = null, float? lineheight = null)
            => FillTextbox(rect, text, pos, font, fontsize, align, rightToLeft, warn, color, fontname, lineheight);
        public List<string> fillTextbox(Rect rect, string text, Point pos = null,
            Font font = null, float fontsize = 11, int align = 0, bool rightToLeft = false,
            bool warn = false, float[] color = null, string fontname = null, float? lineheight = null)
            => fill_textbox(rect, text, pos, font, fontsize, align, rightToLeft, warn, color, fontname, lineheight);
        public void write_text(Page page, float opacity = -1, float[] color = null, int overlay = 1, int oc = 0, int renderMode = 0)
            => WriteText(page, opacity, color, overlay, oc, renderMode);
        public void writeText(Page page, float opacity = -1, float[] color = null, int overlay = 1, int oc = 0, int renderMode = 0)
            => write_text(page, opacity, color, overlay, oc, renderMode);
    }
}
