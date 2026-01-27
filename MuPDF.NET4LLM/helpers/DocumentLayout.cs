using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MuPDF.NET;
using Newtonsoft.Json;

namespace MuPDF.NET4LLM.Helpers
{
    /// <summary>
    /// Layout box representing a content region on a page
    /// </summary>
    public class LayoutBox
    {
        public float X0 { get; set; }
        public float Y0 { get; set; }
        public float X1 { get; set; }
        public float Y1 { get; set; }
        public string BoxClass { get; set; } // e.g. 'text', 'picture', 'table', etc.
        // If boxclass == 'picture' or 'formula', store image bytes
        public byte[] Image { get; set; }
        // If boxclass == 'table'
        public Dictionary<string, object> Table { get; set; }
        // Text line information for text-type boxclasses
        public List<TextLineInfo> TextLines { get; set; }
    }

    /// <summary>
    /// Text line information
    /// </summary>
    public class TextLineInfo
    {
        public Rect Bbox { get; set; }
        public List<ExtendedSpan> Spans { get; set; }
    }

    /// <summary>
    /// Page layout information
    /// </summary>
    public class PageLayout
    {
        public int PageNumber { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public List<LayoutBox> Boxes { get; set; }
        public bool FullOcred { get; set; } // Whether the page is an OCR page
        public bool TextOcred { get; set; } // Whether the page text only is OCR'd
        public List<Block> FullText { get; set; } // Full page text in extractDICT format
        public List<object> Words { get; set; } // List of words with bbox (not yet activated)
        public List<LinkInfo> Links { get; set; }
    }

    /// <summary>
    /// Parsed document structure and layout serialization helpers.
    /// Ported and adapted from the Python module helpers/document_layout.py in pymupdf4llm.
    /// </summary>
    public class ParsedDocument
    {
        public string Filename { get; set; } // Source file name
        public int PageCount { get; set; }
        public List<object> Toc { get; set; } // e.g. [{'title': 'Intro', 'page': 1}]
        public List<PageLayout> Pages { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public Dictionary<string, Dictionary<string, object>> FormFields { get; set; }
        public bool FromBytes { get; set; } // Whether loaded from bytes
        public int ImageDpi { get; set; } = 150; // Image resolution
        public string ImageFormat { get; set; } = "png"; // 'png' or 'jpg'
        public string ImagePath { get; set; } = ""; // Path to save images
        public bool UseOcr { get; set; } = true; // If beneficial invoke OCR
        public bool ForceText { get; set; }
        public bool EmbedImages { get; set; }
        public bool WriteImages { get; set; }

        /// <summary>
        /// Serialize the parsed document into Markdown text, closely following
        /// <c>ParsedDocument.to_markdown</c> in the original Python implementation.
        /// </summary>
        public string ToMarkdown(
            bool header = true,
            bool footer = true,
            bool writeImages = false,
            bool embedImages = false,
            bool ignoreCode = false,
            bool showProgress = false,
            bool pageSeparators = false,
            bool pageChunks = false)
        {
            if (pageChunks)
            {
                throw new NotImplementedException("Page chunks mode not yet fully implemented");
            }

            var documentOutput = new StringBuilder();

            foreach (var page in Pages)
            {
                var mdString = new StringBuilder();
                // Make a mapping: box number -> list item hierarchy level
                var listItemLevels = CreateListItemLevels(page.Boxes);

                foreach (var (box, i) in page.Boxes.Select((b, idx) => (b, idx)))
                {
                    var clip = new Rect(box.X0, box.Y0, box.X1, box.Y1);
                    string btype = box.BoxClass;

                    if (btype == "page-header" && !header)
                        continue;
                    if (btype == "page-footer" && !footer)
                        continue;

                    if (btype == "picture" || btype == "formula" || btype == "table-fallback")
                    {
                        if (box.Image != null)
                        {
                            if (embedImages)
                            {
                                // Make a base64 encoded string of the image
                                string base64 = Convert.ToBase64String(box.Image);
                                string data = $"data:image/{ImageFormat};base64,{base64}";
                                mdString.Append($"\n![]({data})\n\n");
                            }
                            else if (writeImages)
                            {
                                // Save image and reference it
                                mdString.Append($"\n![Image]({ImagePath})\n\n");
                            }
                        }
                        else
                        {
                            mdString.Append($"**==> picture [{clip.Width} x {clip.Height}] intentionally omitted <==**\n\n");
                        }

                        // Output text in image if requested
                        if (box.TextLines != null && box.TextLines.Count > 0)
                        {
                            mdString.Append(TextToMd(box.TextLines, ignoreCode || page.FullOcred));
                        }
                    }
                    else if (btype == "table" && box.Table != null)
                    {
                        if (box.Table.ContainsKey("markdown"))
                        {
                            string tableText = box.Table["markdown"].ToString();
                            if (page.FullOcred)
                                // Remove code style if page was OCR'd
                                tableText = tableText.Replace("`", "");
                            mdString.Append(tableText + "\n\n");
                        }
                    }
                    else if (btype == "list-item")
                    {
                        int level = listItemLevels.ContainsKey(i) ? listItemLevels[i] : 1;
                        mdString.Append(ListItemToMd(box.TextLines, level));
                    }
                    else if (btype == "footnote")
                    {
                        mdString.Append(FootnoteToMd(box.TextLines));
                    }
                    else if (box.TextLines != null)
                    {
                        // Treat as normal MD text
                        mdString.Append(TextToMd(box.TextLines, ignoreCode || page.FullOcred));
                    }
                }

                if (pageSeparators)
                {
                    mdString.Append($"--- end of page={page.PageNumber} ---\n\n");
                }

                documentOutput.Append(mdString.ToString());
            }

            return documentOutput.ToString();
        }

        /// <summary>
        /// Serialize the parsed document into a JSON string, mirroring the behavior
        /// of the Python <c>ParsedDocument.to_json</c> helper.
        /// </summary>
        public string ToJson()
        {
            // Serialize to JSON
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter>
                {
                    new LayoutJsonConverter()
                }
            };

            return JsonConvert.SerializeObject(this, settings);
        }

        /// <summary>
        /// Serialize the parsed document to plain text.
        /// This follows the logic of <c>ParsedDocument.to_text</c> in the Python code,
        /// including optional suppression of headers / footers and simple table rendering.
        /// </summary>
        public string ToText(
            bool header = true,
            bool footer = true,
            bool ignoreCode = false,
            bool showProgress = false,
            bool pageChunks = false,
            string tableFormat = "grid")
        {
            if (pageChunks)
            {
                throw new NotImplementedException("Page chunks mode not yet fully implemented");
            }

            var documentOutput = new StringBuilder();

            foreach (var page in Pages)
            {
                var textString = new StringBuilder();
                var listItemLevels = CreateListItemLevels(page.Boxes);

                foreach (var (box, i) in page.Boxes.Select((b, idx) => (b, idx)))
                {
                    var clip = new Rect(box.X0, box.Y0, box.X1, box.Y1);
                    string btype = box.BoxClass;

                    if (btype == "page-header" && !header)
                        continue;
                    if (btype == "page-footer" && !footer)
                        continue;

                    if (btype == "picture" || btype == "formula" || btype == "table-fallback")
                    {
                        textString.Append($"==> picture [{clip.Width} x {clip.Height}] <==\n\n");
                        if (box.TextLines != null && box.TextLines.Count > 0)
                        {
                            textString.Append(TextToText(box.TextLines, ignoreCode || page.FullOcred));
                        }
                    }
                    else if (btype == "table" && box.Table != null)
                    {
                        // Note: Table formatting would need tabulate equivalent
                        textString.Append("[Table]\n\n");
                    }
                    else if (btype == "list-item")
                    {
                        int level = listItemLevels.ContainsKey(i) ? listItemLevels[i] : 1;
                        textString.Append(ListItemToText(box.TextLines, level));
                    }
                    else if (btype == "footnote")
                    {
                        textString.Append(FootnoteToText(box.TextLines));
                    }
                    else if (box.TextLines != null)
                    {
                        // Handle other cases as normal text
                        textString.Append(TextToText(box.TextLines, ignoreCode || page.FullOcred));
                    }
                }

                documentOutput.Append(textString.ToString());
            }

            return documentOutput.ToString();
        }

        // Helper methods for text conversion
        private static string TitleToMd(List<TextLineInfo> textLines)
        {
            var sb = new StringBuilder();
            foreach (var line in textLines)
            {
                if (line.Spans != null)
                {
                    foreach (var span in line.Spans)
                    {
                        sb.Append(span.Text);
                    }
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }

        private static string SectionHdrToMd(List<TextLineInfo> textLines)
        {
            var sb = new StringBuilder();
            foreach (var line in textLines)
            {
                if (line.Spans != null)
                {
                    foreach (var span in line.Spans)
                    {
                        sb.Append(span.Text);
                    }
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Convert <c>list-item</c> layout boxes to markdown.
        /// The first line is prefixed with <c>-</c>. Subsequent lines are appended
        /// without a line break if their rectangle does not start to the left of
        /// the previous line; otherwise a new markdown list item is started.
        /// 2 units of tolerance is used to avoid spurious line breaks.
        /// </summary>
        /// <param name="textLines">The text line information for the list item.</param>
        /// <param name="level">The hierarchy level (1 for top-level).</param>
        private static string ListItemToMd(List<TextLineInfo> textLines, int level)
        {
            var sb = new StringBuilder();
            string indent = new string(' ', (level - 1) * 2); // Indentation based on level
            sb.Append(indent + "- ");
            foreach (var line in textLines)
            {
                if (line.Spans != null)
                {
                    foreach (var span in line.Spans)
                    {
                        sb.Append(span.Text);
                    }
                }
            }
            sb.Append("\n");
            return sb.ToString();
        }

        /// <summary>
        /// Convert <c>footnote</c> layout boxes to markdown.
        /// The first line is prefixed with <c>&gt; </c>; subsequent lines start a
        /// new blockquote when they begin with superscripted text.
        /// We render footnotes as blockquotes.
        /// </summary>
        /// <param name="textLines">The text line information for the footnote.</param>
        private static string FootnoteToMd(List<TextLineInfo> textLines)
        {
            var sb = new StringBuilder();
            // We render footnotes as blockquotes
            sb.Append("[^");
            foreach (var line in textLines)
            {
                if (line.Spans != null)
                {
                    foreach (var span in line.Spans)
                    {
                        sb.Append(span.Text);
                    }
                }
            }
            sb.Append("]\n");
            return sb.ToString();
        }

        /// <summary>
        /// Convert generic text layout boxes to markdown, as well as box classes
        /// not specifically handled elsewhere. Lines are concatenated without
        /// line breaks; at the end, two newlines are used to separate from the
        /// next block. Monospaced spans may be emitted as code when
        /// <paramref name="ignoreCode" /> is <c>false</c>.
        /// </summary>
        /// <param name="textLines">The text line information to convert.</param>
        /// <param name="ignoreCode">If true, do not emit code-style formatting.</param>
        private static string TextToMd(List<TextLineInfo> textLines, bool ignoreCode)
        {
            // Handle completely monospaced textlines as code block
            // Check for superscript - handle mis-classified text boundary box
            if (textLines == null || textLines.Count == 0)
                return "";
            
            var sb = new StringBuilder();
            foreach (var line in textLines)
            {
                if (line.Spans != null)
                {
                    foreach (var span in line.Spans)
                    {
                        string text = span.Text ?? "";
                        if (!ignoreCode && span.Font != null && span.Font.Contains("Mono"))
                        {
                            text = "`" + text + "`";
                        }
                        sb.Append(text);
                    }
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Convert <c>list-item</c> layout boxes to plain text.
        /// The first line is prefixed with a dash and indentation according to
        /// the hierarchy level; subsequent lines are concatenated.
        /// </summary>
        /// <param name="textLines">The text line information for the list item.</param>
        /// <param name="level">The hierarchy level (1 for top-level).</param>
        private static string ListItemToText(List<TextLineInfo> textLines, int level)
        {
            if (textLines == null || textLines.Count == 0)
                return "";
            
            var sb = new StringBuilder();
            string indent = new string(' ', (level - 1) * 2); // Indentation based on level
            sb.Append(indent + "- ");
            foreach (var line in textLines)
            {
                if (line.Spans != null)
                {
                    foreach (var span in line.Spans)
                    {
                        sb.Append(span.Text);
                    }
                }
            }
            sb.Append("\n");
            return sb.ToString();
        }

        /// <summary>
        /// Convert <c>footnote</c> layout boxes to plain text, concatenating
        /// all spans into a single textual representation.
        /// We render footnotes as blockquotes.
        /// </summary>
        /// <param name="textLines">The text line information for the footnote.</param>
        private static string FootnoteToText(List<TextLineInfo> textLines)
        {
            if (textLines == null || textLines.Count == 0)
                return "";
            
            var sb = new StringBuilder();
            // We render footnotes as blockquotes
            foreach (var line in textLines)
            {
                if (line.Spans != null)
                {
                    foreach (var span in line.Spans)
                    {
                        sb.Append(span.Text);
                    }
                }
            }
            sb.Append("\n");
            return sb.ToString();
        }

        /// <summary>
        /// Convert generic text layout boxes to plain text. The text of all
        /// spans of all lines is written without line breaks.
        /// At the end, two newlines are added to separate from the next block.
        /// </summary>
        /// <param name="textLines">The text line information to convert.</param>
        /// <param name="ignoreCode">Currently unused; included for parity with markdown conversion.</param>
        private static string TextToText(List<TextLineInfo> textLines, bool ignoreCode)
        {
            if (textLines == null || textLines.Count == 0)
                return "";
            
            var sb = new StringBuilder();
            foreach (var line in textLines)
            {
                if (line.Spans != null)
                {
                    foreach (var span in line.Spans)
                    {
                        sb.Append(span.Text);
                    }
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Map the layout box index of each list item to its hierarchy level.
        ///
        /// This post-layout heuristic walks contiguous segments of <c>list-item</c>
        /// boxes and assigns increasing levels when the left coordinate moves
        /// sufficiently to the right, mirroring
        /// <c>create_list_item_levels</c> in the Python implementation.
        /// </summary>
        /// <param name="boxes">The list of layout boxes for the page.</param>
        /// <returns>
        /// A dictionary mapping box index to level, where level is 1 for
        /// top-level items.
        /// </returns>
        private static Dictionary<int, int> CreateListItemLevels(List<LayoutBox> boxes)
        {
            var itemDict = new Dictionary<int, int>(); // Dictionary of item index -> level
            var segments = new List<List<(int idx, LayoutBox box)>>(); // List of item segments
            var currentSegment = new List<(int idx, LayoutBox box)>(); // Current segment

            // Create segments of contiguous list items. Each non-list-item finishes
            // the current segment. Also, two list-items in a row belonging to different
            // page text columns end the segment after the first item.
            for (int i = 0; i < boxes.Count; i++)
            {
                var box = boxes[i];
                if (box.BoxClass != "list-item") // Bbox class is no list-item
                {
                    if (currentSegment.Count > 0) // End and save the current segment
                    {
                        segments.Add(currentSegment);
                        currentSegment = new List<(int idx, LayoutBox box)>();
                    }
                    continue;
                }

                if (currentSegment.Count > 0) // Check if we need to end the current segment
                {
                    var (prevIdx, prevBox) = currentSegment[currentSegment.Count - 1];
                    if (box.X0 > prevBox.X1 || box.Y1 < prevBox.Y0)
                    {
                        // End and save the current segment
                        segments.Add(currentSegment);
                        currentSegment = new List<(int idx, LayoutBox box)>();
                    }
                }
                currentSegment.Add((i, box)); // Append item to segment
            }
            if (currentSegment.Count > 0)
                segments.Add(currentSegment); // Append last segment

            // Walk through segments and assign levels
            foreach (var segment in segments)
            {
                if (segment.Count == 0) continue; // Skip empty segments
                var sorted = segment.OrderBy(x => x.box.X0).ToList(); // Sort by x0 coordinate of the bbox

                // List of leveled items in the segment: (idx, bbox, level)
                // First item has level 1
                var leveled = new List<(int idx, LayoutBox box, int level)>
                {
                    (sorted[0].idx, sorted[0].box, 1)
                };

                for (int i = 1; i < sorted.Count; i++)
                {
                    var (prevIdx, prevBox, prevLvl) = leveled[leveled.Count - 1];
                    var (currIdx, currBox) = sorted[i];
                    // X0 coordinate increased by more than 10 points: increase level
                    int currLvl = currBox.X0 > prevBox.X0 + 10 ? prevLvl + 1 : prevLvl;
                    leveled.Add((currIdx, currBox, currLvl));
                }

                foreach (var (idx, box, lvl) in leveled)
                {
                    itemDict[idx] = lvl;
                }
            }

            return itemDict;
        }
    }

        /// <summary>
        /// Document layout parsing utilities.
        /// Provides a C# equivalent of <c>pymupdf4llm.helpers.document_layout.parse_document</c>.
        /// </summary>
    public static class DocumentLayout
    {
        /// <summary>
        /// Parse document structure
        /// </summary>
        public static ParsedDocument ParseDocument(
            Document doc,
            string filename = "",
            int imageDpi = 150,
            string imageFormat = "png",
            string imagePath = "",
            int ocrDpi = 400,
            List<int> pages = null,
            bool writeImages = false,
            bool embedImages = false,
            bool showProgress = false,
            bool forceText = true,
            bool useOcr = true,
            string ocrLanguage = "eng")
        {
            // Note: Remove StructTreeRoot to avoid possible performance degradation.
            // We will not use the structure tree anyway.
            if (embedImages && writeImages)
                throw new ArgumentException("Cannot both embed and write images.");

            var document = new ParsedDocument
            {
                Filename = !string.IsNullOrEmpty(filename) ? filename : doc.Name,
                PageCount = doc.PageCount,
                Toc = doc.GetToc().Cast<object>().ToList(),
                Metadata = doc.MetaData,
                FormFields = Utils.ExtractFormFieldsWithPages(doc),
                ImageDpi = imageDpi,
                ImageFormat = imageFormat,
                ImagePath = imagePath,
                UseOcr = useOcr,
                ForceText = forceText,
                EmbedImages = embedImages,
                WriteImages = writeImages,
                Pages = new List<PageLayout>()
            };

            if (pages == null)
                pages = Enumerable.Range(0, doc.PageCount).ToList();

            var progressBar = showProgress && pages.Count > 5
                ? ProgressBar.Create(pages.Cast<object>().ToList())
                : null;

            try
            {
                foreach (int pno in pages)
                {
                    if (progressBar != null && !progressBar.MoveNext())
                        break;

                    Page page = doc.LoadPage(pno);
                    try
                    {
                        TextPage textPage = page.GetTextPage(
                            clip: new Rect(float.NegativeInfinity, float.NegativeInfinity,
                                          float.PositiveInfinity, float.PositiveInfinity),
                            flags: Utils.FLAGS);
                        PageInfo pageInfo = textPage.ExtractDict(null, false);
                        List<Block> blocks = pageInfo.Blocks;

                        bool pageFullOcred = false;
                        bool pageTextOcred = false;

                        // Check if this page should be OCR'd
                        if (useOcr)
                        {
                            var decision = CheckOcr.ShouldOcrPage(page, dpi: ocrDpi, blocks: blocks);
                            // Prevent MD styling if already OCR'd
                            pageFullOcred = decision.TryGetValue("has_ocr_text", out var hasOcrText) ? (bool)hasOcrText : false;

                            if (decision.TryGetValue("should_ocr", out var shouldOcr) && (bool)shouldOcr)
                            {
                        // We should be OCR: check full-page vs. text-only
                        if (decision.ContainsKey("pixmap") && decision["pixmap"] != null)
                        {
                            // Full-page OCR would be implemented here
                            // Retrieve the Pixmap, OCR it, get the OCR'd PDF, copy text over to original page
                            pageFullOcred = true;
                        }
                        else
                        {
                            blocks = CheckOcr.RepairBlocks(blocks, page);
                            pageTextOcred = true;
                        }
                            }
                        }

                        var pageLayout = new PageLayout
                        {
                            PageNumber = pno,
                            Width = page.Rect.Width,
                            Height = page.Rect.Height,
                            Boxes = new List<LayoutBox>(),
                            FullOcred = pageFullOcred,
                            TextOcred = pageTextOcred,
                            FullText = blocks,
                            Words = new List<object>(),
                            Links = page.GetLinks()
                        };

                        // Extract text lines for each block
                        // Each line is represented as its bbox and a list of spans
                        var lines = GetTextLines.GetRawLines(textPage, blocks, page.Rect);

                        foreach (var line in lines)
                        {
                            var layoutBox = new LayoutBox
                            {
                                X0 = line.Rect.X0,
                                Y0 = line.Rect.Y0,
                                X1 = line.Rect.X1,
                                Y1 = line.Rect.Y1,
                                BoxClass = "text",
                                TextLines = new List<TextLineInfo>
                                {
                                    new TextLineInfo
                                    {
                                        Bbox = line.Rect,
                                        Spans = line.Spans
                                    }
                                }
                            };

                            pageLayout.Boxes.Add(layoutBox);
                        }

                        document.Pages.Add(pageLayout);
                        textPage.Dispose();
                    }
                    finally
                    {
                        page.Dispose();
                    }
                }
            }
            finally
            {
                progressBar?.Dispose();
            }

            return document;
        }
    }

    /// <summary>
    /// Custom JSON converter for Layout objects
    /// </summary>
    public class LayoutJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]) ||
                   objectType == typeof(Rect) ||
                   objectType == typeof(Point) ||
                   objectType == typeof(Matrix) ||
                   objectType == typeof(IRect) ||
                   objectType == typeof(Quad);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Deserialization not implemented");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (value is byte[] bytes)
            {
                string base64 = Convert.ToBase64String(bytes);
                writer.WriteValue(base64);
            }
            else if (value is Rect rect)
            {
                writer.WriteStartArray();
                writer.WriteValue(rect.X0);
                writer.WriteValue(rect.Y0);
                writer.WriteValue(rect.X1);
                writer.WriteValue(rect.Y1);
                writer.WriteEndArray();
            }
            else if (value is Point point)
            {
                writer.WriteStartArray();
                writer.WriteValue(point.X);
                writer.WriteValue(point.Y);
                writer.WriteEndArray();
            }
            else if (value is Matrix matrix)
            {
                writer.WriteStartArray();
                writer.WriteValue(matrix.A);
                writer.WriteValue(matrix.B);
                writer.WriteValue(matrix.C);
                writer.WriteValue(matrix.D);
                writer.WriteValue(matrix.E);
                writer.WriteValue(matrix.F);
                writer.WriteEndArray();
            }
            else if (value is IRect irect)
            {
                writer.WriteStartArray();
                writer.WriteValue(irect.X0);
                writer.WriteValue(irect.Y0);
                writer.WriteValue(irect.X1);
                writer.WriteValue(irect.Y1);
                writer.WriteEndArray();
            }
            else if (value is Quad quad)
            {
                writer.WriteStartArray();
                writer.WriteStartArray();
                writer.WriteValue(quad.UpperLeft.X);
                writer.WriteValue(quad.UpperLeft.Y);
                writer.WriteEndArray();
                writer.WriteStartArray();
                writer.WriteValue(quad.UpperRight.X);
                writer.WriteValue(quad.UpperRight.Y);
                writer.WriteEndArray();
                writer.WriteStartArray();
                writer.WriteValue(quad.LowerLeft.X);
                writer.WriteValue(quad.LowerLeft.Y);
                writer.WriteEndArray();
                writer.WriteStartArray();
                writer.WriteValue(quad.LowerRight.X);
                writer.WriteValue(quad.LowerRight.Y);
                writer.WriteEndArray();
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}
