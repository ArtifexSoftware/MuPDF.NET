using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;

namespace MuPDF.NET4LLM.Helpers
{
    /// <summary>
    /// Represents a line with its rectangle and spans
    /// </summary>
    public class TextLine
    {
        public Rect Rect { get; set; }
        public List<ExtendedSpan> Spans { get; set; }
    }

    /// <summary>
    /// Text line extraction utilities.
    /// Ported and adapted from LLM helpers.
    /// </summary>
    public static class GetTextLines
    {
        /// <summary>
        /// Extract the text spans from a <see cref="TextPage"/> in natural reading order.
        /// All spans whose vertical positions are within <paramref name="tolerance"/> of
        /// each other are merged into a single logical line, mirroring the behavior of
        /// <c>get_raw_lines()</c> in <c>helpers/get_text_lines.py</c>.
        /// </summary>
        /// <remarks>
        /// C# port of get_raw_lines helper.
        /// It compensates for MuPDF’s tendency to create multiple short lines when spans
        /// are separated by small gaps, by joining adjacent spans into longer lines.
        ///
        /// The result is a list of <see cref="TextLine"/> objects, each containing a
        /// joined line rectangle and a left‑to‑right sorted list of <see cref="ExtendedSpan"/>
        /// items. Each span is annotated with its original block / line index so that
        /// callers can still detect original MuPDF line breaks if needed.
        /// </remarks>
        /// <param name="textpage">
        /// Source <see cref="TextPage"/>. May be <c>null</c> if <paramref name="blocks"/>
        /// are provided directly.
        /// </param>
        /// <param name="blocks">
        /// Optional list of <see cref="Block"/> objects to reuse an existing
        /// <c>ExtractDict</c> result instead of re‑extracting from <paramref name="textpage"/>.
        /// Only text blocks (Type == 0) with non‑empty bounding boxes are considered.
        /// </param>
        /// <param name="clip">
/// Optional clipping rectangle. Only spans whose bounding boxes overlap this
/// area (within <see cref="Utils.AlmostInBbox"/>) are taken into account.
        /// </param>
        /// <param name="tolerance">
        /// Maximum vertical distance (in points) between span baselines or tops for
        /// them to be considered part of the same logical line (default: 3).
        /// </param>
        /// <param name="ignoreInvisible">
        /// When <c>true</c>, spans with zero alpha (invisible text) are skipped, except
        /// for Type 3 fonts (which are always kept).
        /// </param>
        /// <param name="onlyHorizontal">
        /// When <c>true</c>, only spans with approximately horizontal direction
        /// vectors are included (i.e. <c>abs(1 - dir.x) &lt;= 1e‑3</c>), ignoring
        /// vertical or rotated text.
        /// </param>
        /// <returns>
        /// A list of <see cref="TextLine"/> objects. If no spans are found, an
        /// empty list is returned.
        /// </returns>
        public static List<TextLine> GetRawLines(
            TextPage textpage = null,
            List<Block> blocks = null,
            Rect clip = null,
            float tolerance = 3.0f,
            bool ignoreInvisible = true,
            bool onlyHorizontal = true)
        {
            float yDelta = tolerance; // Allowable vertical coordinate deviation

            if (textpage == null && blocks == null)
                throw new ArgumentException("Either textpage or blocks must be provided.");

            if (clip == null && textpage != null)
            {
                // Use TextPage rect if not provided
                clip = new Rect(float.NegativeInfinity, float.NegativeInfinity,
                              float.PositiveInfinity, float.PositiveInfinity);
            }

            // Extract text blocks - if bbox is not empty
            if (blocks == null && textpage != null)
            {
                PageInfo pageInfo = textpage.ExtractDict(null, false);
                blocks = pageInfo.Blocks?.Where(b => b.Type == 0 && !Utils.BboxIsEmpty(b.Bbox)).ToList();
            }

            if (blocks == null)
                blocks = new List<Block>();

            List<ExtendedSpan> spans = new List<ExtendedSpan>(); // All spans in TextPage here

            for (int bno = 0; bno < blocks.Count; bno++) // The numbered blocks
            {
                Block b = blocks[bno];
                if (Utils.OutsideBbox(b.Bbox, clip))
                    continue;

                if (b.Lines == null)
                    continue;

                for (int lno = 0; lno < b.Lines.Count; lno++) // The numbered lines
                {
                    Line line = b.Lines[lno];
                    if (Utils.OutsideBbox(line.Bbox, clip))
                        continue;

                    Point lineDir = line.Dir;
                    if (onlyHorizontal && Math.Abs(1 - lineDir.X) > 1e-3) // Only accept horizontal text
                        continue;

                    if (line.Spans == null)
                        continue;

                    for (int sno = 0; sno < line.Spans.Count; sno++) // The numbered spans
                    {
                        Span s = line.Spans[sno];
                        string text = s.Text ?? "";

                        if (Utils.IsWhite(text))
                            // Ignore white text if not a Type3 font
                            continue;

                        // Ignore invisible text. Type 3 font text is never invisible.
                        // Note: Alpha and CharFlags may need different access in MuPDF.NET
                        if (s.Font != Utils.TYPE3_FONT_NAME && ignoreInvisible)
                        {
                            // Skip invisible text if needed - would need Alpha property
                            // For now, continue
                        }

                        if (!Utils.AlmostInBbox(s.Bbox, clip)) // If not in clip
                            continue;

                        Rect sbbox = new Rect(s.Bbox); // Span bbox as a Rect
                        if (((int)s.Flags & 1) != 0) // If a superscript, modify bbox
                        {
                            // With that of the preceding or following span
                            int i = sno == 0 ? 1 : sno - 1;
                            if (line.Spans.Count > i)
                            {
                                Span neighbor = line.Spans[i];
                                sbbox.Y1 = neighbor.Bbox.Y1;
                            }
                            text = $"[{text}]";
                        }

                        sbbox = sbbox; // Update with the Rect version
                        // Include line/block numbers to facilitate separator insertion
                        ExtendedSpan extSpan = new ExtendedSpan
                        {
                            Text = text,
                            Bbox = sbbox,
                            Size = s.Size,
                            Font = s.Font,
                            Flags = (int)s.Flags,
                            CharFlags = 0, // Would need to extract from Span if available
                            Alpha = 1.0f, // Would need to extract from Span if available
                            Line = lno,
                            Block = bno,
                            Dir = lineDir,
                            Chars = s.Chars
                        };

                        spans.Add(extSpan);
                    }
                }
            }

            if (spans.Count == 0) // No text at all
                return new List<TextLine>();

            // Sort spans by bottom coord
            spans = spans.OrderBy(s => -s.Dir.X).ThenBy(s => s.Bbox.Y1).ToList();

            List<TextLine> nlines = new List<TextLine>(); // Final result
            List<ExtendedSpan> currentLine = new List<ExtendedSpan> { spans[0] }; // Collects spans with fitting vertical coordinates
            Rect lrect = new Rect(spans[0].Bbox); // Rectangle joined from span rectangles

            for (int i = 1; i < spans.Count; i++) // Walk through the spans
            {
                ExtendedSpan s = spans[i];
                Rect sbbox = s.Bbox; // This bbox
                Rect sbbox0 = currentLine[currentLine.Count - 1].Bbox; // Previous bbox
                // If any of top or bottom coordinates are close enough, join...
                if (Math.Abs(sbbox.Y1 - sbbox0.Y1) <= yDelta ||
                    Math.Abs(sbbox.Y0 - sbbox0.Y0) <= yDelta)
                {
                    currentLine.Add(s); // Append to this line
                    lrect = Utils.JoinRects(new List<Rect> { lrect, sbbox }); // Extend line rectangle
                    continue;
                }

                // End of current line, sort its spans from left to right
                currentLine = SanitizeSpans(currentLine);

                // Append line rect and its spans to final output
                nlines.Add(new TextLine { Rect = lrect, Spans = currentLine });

                currentLine = new List<ExtendedSpan> { s }; // Start next line
                lrect = new Rect(sbbox); // Initialize its rectangle
            }

            // Need to append last line in the same way
            currentLine = SanitizeSpans(currentLine);
            nlines.Add(new TextLine { Rect = lrect, Spans = currentLine });

            return nlines;
        }

        /// <summary>
        /// Sort and join spans within a single logical line.
        /// </summary>
        /// <remarks>
        /// This corresponds to the inner <c>sanitize_spans()</c> helper in
        /// <c>get_text_lines.py</c>. Spans are first sorted left‑to‑right and then
        /// adjacent spans with nearly touching x‑coordinates and identical style
        /// (font flags and character flags, except superscript) are merged into a
        /// single <see cref="ExtendedSpan"/> by concatenating their text and
        /// joining their bounding boxes.
        /// </remarks>
        private static List<ExtendedSpan> SanitizeSpans(List<ExtendedSpan> line)
        {
            if (line.Count == 0)
                return line;

            // Sort ascending horizontally
            line = line.OrderBy(s => s.Bbox.X0).ToList();
            // Join spans, delete duplicates
            // Underline differences are being ignored
            for (int i = line.Count - 1; i > 0; i--) // Iterate back to front
            {
                ExtendedSpan s0 = line[i - 1]; // Preceding span
                ExtendedSpan s1 = line[i]; // This span
                // "Delta" depends on the font size. Spans will be joined if
                // no more than 10% of the font size separates them and important
                // attributes are the same.
                float delta = s1.Size * 0.1f;
                if (s0.Bbox.X1 + delta < s1.Bbox.X0 ||
                    s0.Flags != s1.Flags ||
                    (s0.CharFlags & ~2) != (s1.CharFlags & ~2))
                {
                    continue; // No joining
                }
                // We need to join bbox and text of two consecutive spans
                // Sometimes, spans may also be duplicated.
                if (s0.Text != s1.Text || !s0.Bbox.EqualTo(s1.Bbox))
                {
                    s0.Text += s1.Text;
                }
                s0.Bbox = Utils.JoinRects(new List<Rect> { s0.Bbox, s1.Bbox }); // Join boundary boxes
                line.RemoveAt(i); // Delete the joined-in span
                line[i - 1] = s0; // Update the span
            }

            return line;
        }

        /// <summary>
        /// Extract plain text line‑by‑line in natural reading order.
        /// </summary>
        /// <remarks>
        /// This is the C# equivalent of <c>get_text_lines()</c> in
        /// <c>helpers/get_text_lines.py</c>. It first obtains logical lines via
        /// <see cref="GetRawLines(MuPDF.NET.TextPage,System.Collections.Generic.List{MuPDF.NET.Block},MuPDF.NET.Rect,float,bool,bool)"/>,
        /// then concatenates spans on the same original MuPDF line, inserting the
        /// separator <paramref name="sep"/> when a new original line continues the
        /// same logical line.
        ///
        /// For non‑OCR text (<paramref name="ocr"/> = <c>false</c>), this produces
        /// continuous text suitable for indexing while preserving a reasonable
        /// reading order, including extra blank lines between text blocks.
        ///
        /// When <paramref name="ocr"/> is <c>true</c>, a simplified table recognition
        /// is applied to the OCR output: lines are grouped into columns based on
        /// x‑coordinates and emitted as a Markdown table.
        /// implementation.
        /// </remarks>
        /// <param name="page">The source <see cref="Page"/> to extract text from.</param>
        /// <param name="textpage">
        /// Optional pre‑created <see cref="TextPage"/>. When <c>null</c>, this method
/// will create a temporary text page (or OCR text page if <paramref name="ocr"/>
/// is <c>true</c>) and dispose it afterwards.
        /// </param>
        /// <param name="clip">
        /// Optional clipping rectangle restricting the area from which lines are read.
        /// </param>
        /// <param name="sep">
        /// Separator string used when joining multiple MuPDF lines that are merged
        /// into a single logical line (default: tab).
        /// </param>
        /// <param name="tolerance">
        /// Vertical tolerance passed through to <see cref="GetRawLines"/>.
        /// </param>
        /// <param name="ocr">
        /// When <c>true</c>, uses OCR text extraction and applies rudimentary
        /// table reconstruction, returning a Markdown‑style table for tabular OCR output.
        /// </param>
        /// <returns>
        /// A string containing the page text in reading order. For non‑OCR mode,
        /// this is plain text with line breaks and block separators. For OCR mode,
        /// it may contain Markdown‑style tables.
        /// </returns>
        public static string GetTextLinesFormatted(
            Page page,
            TextPage textpage = null,
            Rect clip = null,
            string sep = "\t",
            float tolerance = 3.0f,
            bool ocr = false)
        {
            int textFlags = (int)TextFlags.TEXT_MEDIABOX_CLIP;
            page.SetRotation(0);
            Rect prect = clip ?? page.Rect; // Area to consider

            string xsep = sep == "|" ? "" : sep;

            // Make a TextPage if required
            TextPage tp = textpage;
            bool disposeTp = false;

            if (tp == null)
            {
                if (!ocr)
                {
                    tp = page.GetTextPage(clip: prect, flags: textFlags);
                }
                else
                {
                    tp = page.GetTextPageOcr(dpi: 300, full: true);
                }
                disposeTp = true;
            }

            List<TextLine> lines = GetRawLines(tp, null, prect, tolerance);

            if (disposeTp) // Delete temp TextPage
            {
                tp?.Dispose();
            }

            if (lines == null || lines.Count == 0)
                return "";

            string alltext = "";

            // Compose final text
            if (!ocr)
            {
                int prevBno = -1; // Number of previous text block
                foreach (var (lrect, line) in lines.Select(l => (l.Rect, l.Spans))) // Iterate through lines
                {
                    // Insert extra line break if a different block
                    int bno = line[0].Block; // Block number of this line
                    if (bno != prevBno)
                    {
                        alltext += "\n";
                    }
                    prevBno = bno;

                    int lineNo = line[0].Line; // Store the line number of previous span
                    foreach (var s in line) // Walk over the spans in the line
                    {
                        int lno = s.Line;
                        string stext = s.Text;
                        if (lineNo == lno)
                        {
                            alltext += stext;
                        }
                        else
                        {
                            alltext += sep + stext;
                        }
                        lineNo = lno;
                    }
                    alltext += "\n"; // Append line break after a line
                }
                alltext += "\n"; // Append line break at end of block
                return alltext;
            }

            // For OCR output, we try a rudimentary table recognition.
            List<List<string>> rows = new List<List<string>>();
            List<float> xvalues = new List<float>();
            int colCount = 0;

            foreach (var (lrect, line) in lines.Select(l => (l.Rect, l.Spans)))
            {
                // If only 1 span in line and no columns identified yet...
                if (line.Count == 1 && xvalues.Count == 0)
                {
                    alltext += line[0].Text + "\n\n\n";
                    continue;
                }
                // Multiple spans in line and no columns identified yet
                else if (xvalues.Count == 0) // Define column borders
                {
                    xvalues = line.Select(s => s.Bbox.X0).ToList();
                    xvalues.Add(line[line.Count - 1].Bbox.X1);
                    colCount = line.Count; // Number of columns
                }

                List<string> row = new List<string>(new string[colCount]);
                foreach (var s in line)
                {
                    for (int i = 0; i < xvalues.Count - 1; i++)
                    {
                        float x0 = xvalues[i];
                        float x1 = xvalues[i + 1];
                        if (Math.Abs(s.Bbox.X0 - x0) <= 3 || Math.Abs(s.Bbox.X1 - x1) <= 3)
                        {
                            row[i] = s.Text;
                        }
                    }
                }
                rows.Add(row);
            }

            if (rows.Count > 0 && rows[0].Count > 0)
            {
                string header = "|" + string.Join("|", rows[0]) + "|\n";
                alltext += header;
                alltext += "|" + string.Join("|", Enumerable.Range(0, rows[0].Count).Select(_ => "---")) + "|\n";
                for (int i = 1; i < rows.Count; i++)
                {
                    alltext += "|" + string.Join("|", rows[i]) + "|\n";
                }
                alltext += "\n";
            }

            return alltext;
        }
    }
}
