using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MuPDF.NET;
using PDF4LLM.Ocr;
using Newtonsoft.Json;

namespace PDF4LLM.Helpers
{
    /// <summary>
    /// Optional hook for per-page OCR (<c>ocr_function</c>-style signature: page, dpi, language, keep OCR text).
    /// When supplied and OCR runs, the callback is invoked and text is re-extracted from the page.
    /// </summary>
    /// <param name="page">Page to OCR.</param>
    /// <param name="ocrDpi">Resolution hint (same role as <c>ocr_dpi</c> in the reference API).</param>
    /// <param name="ocrLanguage">Tesseract / engine language code.</param>
    /// <param name="keepOcrText">When <c>true</c>, preserve existing OCR text (<c>keep_ocr_text</c> flag).</param>
    public delegate void OcrPageFunction(Page page, int ocrDpi, string ocrLanguage, bool keepOcrText);

    /// <summary>
    /// Layout box representing a content region on a page
    /// (<c>LayoutBox</c> in the layout document model).
    /// </summary>
    [JsonConverter(typeof(LayoutBoxJsonConverter))]
    public class LayoutBox
    {
        public float X0 { get; set; }
        public float Y0 { get; set; }
        public float X1 { get; set; }
        public float Y1 { get; set; }
        public string BoxClass { get; set; } // e.g. 'text', 'picture', 'table', etc.
        // If boxclass == 'picture' or 'formula', store image bytes
        public byte[] Image { get; set; }
        /// <summary>When <see cref="ParsedDocument.WriteImages"/> is used, Markdown-safe path for <c>![](...)</c> (string stored in <c>layoutbox.image</c> JSON).</summary>
        public string ImageMarkdownRef { get; set; }
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
        [JsonProperty("bbox")]
        public Rect Bbox { get; set; }
        [JsonProperty("spans")]
        public List<ExtendedSpan> Spans { get; set; }
    }

    /// <summary>
    /// Page layout information
    /// </summary>
    public class PageLayout
    {
        [JsonProperty("page_number")]
        public int PageNumber { get; set; }
        [JsonProperty("width")]
        public float Width { get; set; }
        [JsonProperty("height")]
        public float Height { get; set; }
        [JsonProperty("boxes")]
        public List<LayoutBox> Boxes { get; set; }
        [JsonProperty("full_ocred")]
        public bool FullOcred { get; set; } // Whether the page is an OCR page
        [JsonProperty("text_ocred")]
        public bool TextOcred { get; set; } // Whether the page text only is OCR'd
        [JsonProperty("fulltext")]
        public List<Block> FullText { get; set; } // Full page text in extractDICT format
        [JsonProperty("words")]
        public List<object> Words { get; set; } // List of words with bbox (not yet activated)
        [JsonProperty("links")]
        public List<LinkInfo> Links { get; set; }
    }

    /// <summary>
    /// Parsed document structure and layout serialization helpers.
    /// Ported and adapted from LLM helpers.
    /// </summary>
    public class ParsedDocument
    {
        [JsonProperty("filename")]
        public string Filename { get; set; } // Source file name
        [JsonProperty("page_count")]
        public int PageCount { get; set; }
        [JsonProperty("toc")]
        public List<object> Toc { get; set; } // e.g. [{'title': 'Intro', 'page': 1}]
        [JsonProperty("pages")]
        public List<PageLayout> Pages { get; set; }
        [JsonProperty("metadata")]
        public Dictionary<string, string> Metadata { get; set; }
        [JsonProperty("form_fields")]
        public Dictionary<string, Dictionary<string, object>> FormFields { get; set; }
        [JsonProperty("from_bytes")]
        public bool FromBytes { get; set; } // Whether loaded from bytes
        [JsonProperty("image_dpi")]
        public int ImageDpi { get; set; } = 150; // Image resolution
        [JsonProperty("image_format")]
        public string ImageFormat { get; set; } = "png"; // 'png' or 'jpg'
        [JsonProperty("image_path")]
        public string ImagePath { get; set; } = ""; // Path to save images
        [JsonIgnore]
        public bool UseOcr { get; set; } = true; // If beneficial invoke OCR (implied by use_ocr / OCRMode)
        [JsonProperty("force_text")]
        public bool ForceText { get; set; }
        [JsonProperty("embed_images")]
        public bool EmbedImages { get; set; }
        [JsonProperty("write_images")]
        public bool WriteImages { get; set; }

        /// <summary>Effective OCR policy after <c>parse_document</c> resolution (<c>use_ocr</c> as <c>OCRMode</c>).</summary>
        [JsonProperty("use_ocr")]
        public OcrMode OcrMode { get; set; } = OcrMode.Never;

        private const string GraphicsTextMd = "\n![]({0})\n";
        private const string OcrFontName = "GlyphLessFont";

        /// <summary>
        /// Serialize the parsed document into Markdown text, closely following
        /// <c>ParsedDocument.to_markdown</c> equivalent.
        /// When <paramref name="pageChunks"/> is true, returns JSON (array of page chunk dicts), matching MuPdfRag layout output style.
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
            IEnumerator<object> progressBar = null;
            var pages = Pages ?? new List<PageLayout>();
            if (showProgress && pages.Count > 5)
            {
                Console.WriteLine("Generating markdown text...");
                progressBar = ProgressBar.Create(pages.Cast<object>().ToList());
            }

            object documentOutput;
            if (!pageChunks)
                documentOutput = new StringBuilder();
            else
                documentOutput = new List<Dictionary<string, object>>();

            try
            {
                for (int pi = 0; pi < pages.Count; pi++)
                {
                    if (progressBar != null && !progressBar.MoveNext())
                        break;
                    var page = pages[pi];

                    var mdString = new StringBuilder();
                    var stringLengths = new List<int>();
                    var listItemLevels = CreateListItemLevels(page.Boxes);
                    bool fullOcred = page.FullOcred;

                    foreach (var (box, i) in page.Boxes.Select((b, idx) => (b, idx)))
                    {
                        var clip = new Rect(box.X0, box.Y0, box.X1, box.Y1);
                        string btype = box.BoxClass ?? "";

                        if (btype == "page-header" && !header)
                        {
                            stringLengths.Add(mdString.Length);
                            continue;
                        }

                        if (btype == "page-footer" && !footer)
                        {
                            stringLengths.Add(mdString.Length);
                            continue;
                        }

                        if (btype == "picture" || btype == "formula" || btype == "table-fallback")
                        {
                            if (box.Image != null && box.Image.Length > 0)
                            {
                                if (embedImages)
                                {
                                    string base64 = Convert.ToBase64String(box.Image);
                                    string data = $"data:image/{ImageFormat};base64,{base64}";
                                    mdString.Append(string.Format(GraphicsTextMd, data));
                                    mdString.Append("\n\n");
                                }
                                else if (writeImages)
                                {
                                    string mdRef = !string.IsNullOrEmpty(box.ImageMarkdownRef)
                                        ? box.ImageMarkdownRef
                                        : ImagePath;
                                    mdString.Append(string.Format(GraphicsTextMd, mdRef));
                                    mdString.Append("\n\n");
                                }
                            }
                            else if (writeImages && !string.IsNullOrEmpty(box.ImageMarkdownRef))
                            {
                                mdString.Append(string.Format(GraphicsTextMd, box.ImageMarkdownRef));
                                mdString.Append("\n\n");
                            }
                            else
                            {
                                mdString.Append($"**==> picture [{clip.Width} x {clip.Height}] intentionally omitted <==**\n\n");
                            }

                            if (box.TextLines != null && box.TextLines.Count > 0)
                            {
                                if (btype == "picture")
                                    mdString.Append(PictureTextToMd(box.TextLines));
                                else if (btype == "table-fallback")
                                    mdString.Append(FallbackTextToMd(box.TextLines));
                            }

                            stringLengths.Add(mdString.Length);
                            continue;
                        }

                        if (btype == "table" && box.Table != null)
                        {
                            if (box.Table.TryGetValue("markdown", out object mdObj) && mdObj != null)
                            {
                                string tableText = mdObj.ToString();
                                if (fullOcred)
                                    tableText = tableText.Replace("`", "");
                                mdString.Append(tableText + "\n\n");
                            }

                            stringLengths.Add(mdString.Length);
                            continue;
                        }

                        if (box.TextLines == null || box.TextLines.Count == 0)
                        {
                            Console.WriteLine($"Warning: box {btype} has no textlines");
                            stringLengths.Add(mdString.Length);
                            continue;
                        }

                        if (btype == "title")
                            mdString.Append(TitleToMd(box.TextLines));
                        else if (btype == "section-header")
                            mdString.Append(SectionHdrToMd(box.TextLines));
                        else if (btype == "list-item")
                        {
                            int level = listItemLevels.ContainsKey(i) ? listItemLevels[i] : 1;
                            mdString.Append(ListItemToMd(box.TextLines, level));
                        }
                        else if (btype == "footnote")
                            mdString.Append(FootnoteToMd(box.TextLines));
                        else
                            mdString.Append(TextToMd(box.TextLines, ignoreCode || fullOcred));

                        stringLengths.Add(mdString.Length);
                    }

                    int pageNumberOneBased = page.PageNumber + 1;
                    if (pageSeparators)
                        mdString.Append($"--- end of page.page_number={pageNumberOneBased} ---\n\n");

                    string pageMd = mdString.ToString();
                    if (!pageChunks)
                        ((StringBuilder)documentOutput).Append(pageMd);
                    else
                    {
                        if (stringLengths.Count != page.Boxes.Count)
                            throw new InvalidOperationException("Internal error: string_lengths must match page.boxes count.");
                        var chunk = MakePageChunk(this, page, pageMd, stringLengths);
                        ((List<Dictionary<string, object>>)documentOutput).Add(chunk);
                    }
                }
            }
            finally
            {
                progressBar?.Dispose();
            }

            if (!pageChunks)
                return ((StringBuilder)documentOutput).ToString();

            return JsonConvert.SerializeObject(documentOutput, Formatting.Indented);
        }

        /// <summary><c>make_page_chunk</c> equivalent.</summary>
        private static Dictionary<string, object> MakePageChunk(
            ParsedDocument doc,
            PageLayout page,
            string text,
            List<int> stringLengths)
        {
            int pageNumberOneBased = page.PageNumber + 1;
            var pageTocs = new List<object>();
            if (doc.Toc != null)
            {
                foreach (object o in doc.Toc)
                {
                    if (o is Toc t && t.Page == pageNumberOneBased)
                        pageTocs.Add(new Dictionary<string, object>
                        {
                            ["level"] = t.Level,
                            ["title"] = t.Title ?? "",
                            ["page"] = t.Page
                        });
                }
            }

            var metadata = new Dictionary<string, object>();
            if (doc.Metadata != null)
            {
                foreach (var kvp in doc.Metadata)
                    metadata[kvp.Key] = kvp.Value;
            }

            metadata["file_path"] = doc.Filename ?? "";
            metadata["page_count"] = doc.PageCount;
            metadata["page_number"] = pageNumberOneBased;

            var pageBoxes = new List<Dictionary<string, object>>();
            for (int i = 0; i < page.Boxes.Count; i++)
            {
                var b = page.Boxes[i];
                int start = i > 0 ? stringLengths[i - 1] : 0;
                int stop = stringLengths[i];
                string cls = b.BoxClass == "table-fallback" ? "table" : (b.BoxClass ?? "text");
                pageBoxes.Add(new Dictionary<string, object>
                {
                    ["index"] = i,
                    ["class"] = cls,
                    ["bbox"] = new List<int>
                    {
                        (int)Math.Floor(b.X0),
                        (int)Math.Floor(b.Y0),
                        (int)Math.Ceiling(b.X1),
                        (int)Math.Ceiling(b.Y1)
                    },
                    ["pos"] = new List<int> { start, stop }
                });
            }

            return new Dictionary<string, object>
            {
                ["metadata"] = metadata,
                ["toc_items"] = pageTocs,
                ["page_boxes"] = pageBoxes,
                ["text"] = text
            };
        }

        /// <summary>
        /// Serialize the parsed document into JSON (<c>ParsedDocument.to_json</c> behavior: compact serialization,
        /// <c>ensure_ascii=False</c>, <c>LayoutEncoder</c> rules).
        /// </summary>
        /// <param name="showProgress">Reserved for progress reporting (currently unused).</param>
        public string ToJson(bool showProgress = false)
        {
            _ = showProgress;

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Include,
                StringEscapeHandling = StringEscapeHandling.Default,
                Converters = new List<JsonConverter> { new LayoutJsonConverter() }
            };

            return JsonConvert.SerializeObject(this, settings);
        }

        /// <summary>
        /// Serialize the parsed document to plain text (<c>ParsedDocument.to_text</c> behavior).
        /// </summary>
        public string ToText(
            bool header = true,
            bool footer = true,
            bool ignoreCode = false,
            bool showProgress = false,
            bool pageChunks = false,
            string tableFormat = "grid",
            int tableMaxWidth = 100,
            int tableMinColWidth = 10)
        {
            string normalizedTableFormat = LayoutTabulate.NormalizeTableFormat(tableFormat);

            IEnumerator<object> progressBar = null;
            var pages = Pages ?? new List<PageLayout>();
            if (showProgress && pages.Count > 5)
            {
                Console.WriteLine("Generating plain text ..");
                progressBar = ProgressBar.Create(pages.Cast<object>().ToList());
            }

            object documentOutput;
            if (!pageChunks)
                documentOutput = new StringBuilder();
            else
                documentOutput = new List<Dictionary<string, object>>();

            try
            {
                for (int pi = 0; pi < pages.Count; pi++)
                {
                    if (progressBar != null && !progressBar.MoveNext())
                        break;
                    PageLayout page = pages[pi];
                    var textString = new StringBuilder();
                    var stringLengths = new List<int>();
                    Dictionary<int, int> listItemLevels = CreateListItemLevels(page.Boxes);
                    bool fullOcred = page.FullOcred;

                    foreach (var (box, i) in page.Boxes.Select((b, idx) => (b, idx)))
                    {
                        var clip = new Rect(box.X0, box.Y0, box.X1, box.Y1);
                        string btype = box.BoxClass ?? "";

                        if (btype == "page-header" && !header)
                        {
                            stringLengths.Add(textString.Length);
                            continue;
                        }

                        if (btype == "page-footer" && !footer)
                        {
                            stringLengths.Add(textString.Length);
                            continue;
                        }

                        if (btype == "picture" || btype == "formula" || btype == "table-fallback")
                        {
                            textString.Append($"==> picture [{clip.Width} x {clip.Height}] <==\n\n");
                            if (box.TextLines != null && box.TextLines.Count > 0)
                            {
                                if (btype == "picture")
                                    textString.Append(PictureTextToText(box.TextLines, ignoreCode || fullOcred, clip));
                                else if (btype == "table-fallback")
                                    textString.Append(FallbackTextToText(box.TextLines, clip, tableMaxWidth));
                            }

                            stringLengths.Add(textString.Length);
                            continue;
                        }

                        if (btype == "table" && box.Table != null)
                        {
                            List<List<string>> extract = GetTableExtract(box.Table);
                            if (extract != null && extract.Count > 0)
                            {
                                List<List<string>> wrapped = LayoutTabulate.WrapTableForTabulate(
                                    extract,
                                    maxWidth: tableMaxWidth,
                                    minColWidth: tableMinColWidth);
                                textString.Append(
                                    LayoutTabulate.Tabulate(wrapped, normalizedTableFormat, uniformMaxColWidth: null));
                                textString.Append("\n\n");
                            }

                            stringLengths.Add(textString.Length);
                            continue;
                        }

                        if (btype == "list-item")
                        {
                            int level = listItemLevels.ContainsKey(i) ? listItemLevels[i] : 1;
                            textString.Append(ListItemToText(box.TextLines, level));
                            stringLengths.Add(textString.Length);
                            continue;
                        }

                        if (btype == "footnote")
                        {
                            textString.Append(FootnoteToText(box.TextLines));
                            stringLengths.Add(textString.Length);
                            continue;
                        }

                        textString.Append(TextToText(box.TextLines, ignoreCode || fullOcred));
                        stringLengths.Add(textString.Length);
                    }

                    string pageText = textString.ToString();
                    if (!pageChunks)
                        ((StringBuilder)documentOutput).Append(pageText);
                    else
                    {
                        if (stringLengths.Count != page.Boxes.Count)
                            throw new InvalidOperationException("Internal error: string_lengths must match page.boxes count.");
                        var chunk = MakePageChunk(this, page, pageText, stringLengths);
                        ((List<Dictionary<string, object>>)documentOutput).Add(chunk);
                    }
                }
            }
            finally
            {
                progressBar?.Dispose();
            }

            if (!pageChunks)
                return ((StringBuilder)documentOutput).ToString();

            return JsonConvert.SerializeObject(documentOutput, Formatting.Indented);
        }

        private static List<List<string>> GetTableExtract(Dictionary<string, object> table)
        {
            if (table == null || !table.TryGetValue("extract", out object ex) || ex == null)
                return null;
            return ex as List<List<string>>;
        }

        // --- Markdown / text helpers (document layout pipeline) ---

        private static List<ExtendedSpan> CollectSpans(List<TextLineInfo> textLines)
        {
            var spans = new List<ExtendedSpan>();
            if (textLines == null)
                return spans;
            foreach (var line in textLines)
            {
                if (line?.Spans == null)
                    continue;
                foreach (var s in line.Spans)
                    spans.Add(s);
            }
            return spans;
        }

        private static string OmitIfPuaChar(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length != 1)
                return text;
            int o = text[0];
            if ((o >= 0xE000 && o <= 0xF8FF) || (o >= 0xF0000 && o <= 0xFFFFD) || (o >= 0x100000 && o <= 0x10FFFD))
                return "";
            return text;
        }

        private static bool SpanIsMono(ExtendedSpan s) =>
            (s.Flags & 8) != 0 && !string.Equals(s.Font, OcrFontName, StringComparison.Ordinal);

        private static bool IsMonospaced(List<TextLineInfo> textLines)
        {
            if (textLines == null || textLines.Count == 0)
                return false;
            int mono = 0;
            foreach (var line in textLines)
            {
                if (line.Spans == null || line.Spans.Count == 0)
                    return false;
                if (line.Spans.All(SpanIsMono))
                    mono++;
            }
            return mono == textLines.Count;
        }

        private static bool IsSuperscripted(TextLineInfo line)
        {
            if (line?.Spans == null || line.Spans.Count == 0)
                return false;
            var spans = line.Spans;
            if ((spans[0].Flags & 1) != 0)
                return true;
            if (spans.Count < 2)
                return false;
            var span0 = spans[0];
            var span1 = spans[1];
            if (span0.Bbox != null && span1.Bbox != null
                && span0.Bbox.Y0 < span1.Bbox.Y0 && span0.Size < span1.Size)
                return true;
            return false;
        }

        private static int LastSplitWordLength(string output)
        {
            string t = output.TrimEnd();
            if (t.Length == 0)
                return 0;
            int end = t.Length;
            int i = end - 1;
            while (i >= 0 && !char.IsWhiteSpace(t[i]))
                i--;
            return end - 1 - i;
        }

        private static string GetPlainText(List<ExtendedSpan> spans)
        {
            string output = "";
            for (int i = 0; i < spans.Count; i++)
            {
                ExtendedSpan s = spans[i];
                bool superscript = (s.Flags & 1) != 0;
                string spanText = (s.Text ?? "").Trim();
                if (superscript)
                {
                    if (i == 0)
                        spanText = $"[{spanText}] ";
                    else if (output.EndsWith(" ", StringComparison.Ordinal))
                        output = output.Substring(0, output.Length - 1);
                }

                if (output.EndsWith("- ", StringComparison.Ordinal) && LastSplitWordLength(output) > 2)
                    output = output.Substring(0, output.Length - 2);

                output += spanText + " ";
            }

            return output;
        }

        private static string GetStyledText(IList<ExtendedSpan> spans, bool ignoreCode)
        {
            if (spans == null || spans.Count == 0)
                return "";
            string output = "";
            int oldLine = 0;
            int oldBlock = 0;

            for (int i = 0; i < spans.Count; i++)
            {
                ExtendedSpan s = spans[i];
                string prefix = "";
                bool superscript = (s.Flags & 1) != 0;
                bool mono = !ignoreCode && (s.Flags & 8) != 0
                    && !string.Equals(s.Font, OcrFontName, StringComparison.Ordinal);
                bool bold = (s.Flags & 16) != 0 || (s.CharFlags & 8) != 0;
                bool italic = (s.Flags & 2) != 0;
                bool strikeout = (s.CharFlags & 1) != 0;

                if (mono)
                    prefix = "`" + prefix;
                if (bold)
                    prefix = "**" + prefix;
                if (italic)
                    prefix = "_" + prefix;
                if (strikeout)
                    prefix = "~~" + prefix;

                char[] pa = prefix.ToCharArray();
                Array.Reverse(pa);
                string suffix = new string(pa);

                string spanText = (s.Text ?? "").Trim();
                string text = $"{prefix}{spanText}{suffix} ";

                if (output.EndsWith($"{suffix} ", StringComparison.Ordinal))
                {
                    output = output.Substring(0, output.Length - suffix.Length - 1);
                    bool hyphenFix = (oldBlock != s.Block || oldLine != s.Line)
                        && output.EndsWith("-", StringComparison.Ordinal)
                        && LastSplitWordLength(output) > 2;
                    if (hyphenFix)
                    {
                        output = output.Substring(0, output.Length - 1);
                        text = spanText + suffix + " ";
                    }
                    else if (superscript)
                        text = spanText + suffix + " ";
                    else
                        text = " " + spanText + suffix + " ";
                }

                oldLine = s.Line;
                oldBlock = s.Block;
                output += text;
            }

            return output;
        }

        private static string CodeBlockToMd(List<TextLineInfo> textLines)
        {
            var sb = new StringBuilder("```\n");
            foreach (var line in textLines)
            {
                if (line.Spans == null)
                    continue;
                string lineText = string.Concat(line.Spans.Select(s => s.Text ?? ""));
                sb.Append(lineText.TrimEnd()).Append("\n");
            }

            sb.Append("```\n\n");
            return sb.ToString();
        }

        private static string CodeBlockToText(List<TextLineInfo> textLines)
        {
            var sb = new StringBuilder();
            foreach (var line in textLines)
            {
                if (line.Spans == null)
                    continue;
                string lineText = string.Concat(line.Spans.Select(s => s.Text ?? ""));
                sb.Append(lineText.TrimEnd()).Append("\n");
            }

            return sb.Append("\n").ToString();
        }

        private static string PictureTextToMd(List<TextLineInfo> textLines)
        {
            var sb = new StringBuilder("**----- Start of picture text -----**<br>\n");
            foreach (var tl in textLines)
            {
                if (tl.Spans == null)
                    continue;
                string lineText = string.Join(" ", tl.Spans.Select(s => s.Text ?? ""));
                sb.Append(lineText.TrimEnd()).Append("<br>");
            }

            sb.Append("**----- End of picture text -----**<br>\n");
            return sb.Append("\n\n").ToString();
        }

        private static string FallbackTextToMd(List<TextLineInfo> textLines)
        {
            if (textLines == null || textLines.Count == 0)
                return "";
            int spanCount = textLines.Max(tl => tl.Spans?.Count ?? 0);
            if (spanCount == 0)
                return PictureTextToMd(textLines);

            var sb = new StringBuilder("**----- Start of picture text -----**<br>\n");
            sb.Append('|', spanCount + 1).Append('\n');
            sb.Append('|').Append(string.Join("|", Enumerable.Repeat("---", spanCount))).Append("|\n");
            foreach (var tl in textLines)
            {
                var spans = tl.Spans ?? new List<ExtendedSpan>();
                sb.Append('|').Append(string.Join("|", spans.Select(s => (s.Text ?? "").Trim()))).Append("|\n");
            }

            sb.Append("\n**----- End of picture text -----**<br>\n");
            return sb.Append("\n\n").ToString();
        }

        /// <summary><c>picture_text_to_text</c> (parameters match signature; <c>ignore_code</c> unused).</summary>
        private static string PictureTextToText(List<TextLineInfo> textLines, bool ignoreCode = false, Rect clip = null)
        {
            _ = ignoreCode;
            _ = clip;
            var sb = new StringBuilder("----- Start of picture text -----\n");
            foreach (var tl in textLines)
            {
                if (tl.Spans == null)
                    continue;
                string lineText = string.Join(" ", tl.Spans.Select(s => s.Text ?? ""));
                sb.Append(lineText.TrimEnd()).Append("\n");
            }

            sb.Append("----- End of picture text -----\n");
            return sb.ToString() + "\n";
        }

        /// <summary><c>fallback_text_to_text</c> (<c>tabulate</c> grid + <c>maxcolwidths</c>).</summary>
        private static string FallbackTextToText(List<TextLineInfo> textLines, Rect clip, int tableMaxWidth = 100)
        {
            if (textLines == null || textLines.Count == 0)
                return "";
            int spanCount = textLines.Max(tl => tl.Spans?.Count ?? 0);
            if (spanCount == 0)
                return PictureTextToText(textLines);
            float clipX0 = clip?.X0 ?? 0f;
            var lines = new List<List<string>>();
            foreach (TextLineInfo tl in textLines)
            {
                var spans = tl.Spans ?? new List<ExtendedSpan>();
                var line = new List<string>();
                for (int k = 0; k < spanCount; k++)
                    line.Add("");
                int i0 = spans.Count < spanCount && spans.Count > 0 && spans[0].Bbox != null && spans[0].Bbox.X0 > clipX0 + 10f
                    ? 1
                    : 0;
                for (int j = 0; j < spans.Count; j++)
                {
                    int col = i0 + j;
                    if (col < spanCount)
                        line[col] = (spans[j].Text ?? "").Trim();
                }

                lines.Add(line);
            }

            int maxCol = Math.Max(1, tableMaxWidth / spanCount);
            return LayoutTabulate.Tabulate(lines, "grid", uniformMaxColWidth: maxCol) + "\n\n";
        }

        /// <summary><c>title</c> layout box → level-1 heading (<c>title_to_md</c>).</summary>
        private static string TitleToMd(List<TextLineInfo> textLines)
        {
            var spans = CollectSpans(textLines);
            if (spans.Count == 0)
                return "";
            string output = GetStyledText(spans, ignoreCode: false);
            return $"# {output}\n\n";
        }

        /// <summary><c>section-header</c> layout box → level-2 heading (<c>section_hdr_to_md</c>).</summary>
        private static string SectionHdrToMd(List<TextLineInfo> textLines)
        {
            var spans = CollectSpans(textLines);
            if (spans.Count == 0)
                return "";
            string output = GetStyledText(spans, ignoreCode: false);
            return $"## {output}\n\n";
        }

        private static string ListItemToMd(List<TextLineInfo> textLines, int level)
        {
            if (textLines == null || textLines.Count == 0)
                return "";
            string indent = new string(' ', Math.Max(0, level - 1) * 3);

            var firstLine = textLines[0];
            float x0 = firstLine.Bbox?.X0 ?? 0f;
            var spans = new List<ExtendedSpan>();
            if (firstLine.Spans != null)
                spans.AddRange(firstLine.Spans);
            if (spans.Count == 0)
                return "";

            string span0Text = (spans[0].Text ?? "").Trim();
            string starter = "- ";
            if (Utils.StartswithBullet(span0Text))
            {
                span0Text = span0Text.Length > 1 ? span0Text.Substring(1).Trim() : "";
                spans[0].Text = span0Text;
            }
            else if (span0Text.EndsWith(".", StringComparison.Ordinal) && span0Text.Length > 1
                     && span0Text.Substring(0, span0Text.Length - 1).All(char.IsDigit))
                starter = "";
            else if (span0Text.Contains(' '))
            {
                string firstWord = span0Text.Split(' ')[0];
                if (firstWord.EndsWith(".", StringComparison.Ordinal) && firstWord.Length > 1
                    && firstWord.Substring(0, firstWord.Length - 1).All(char.IsDigit))
                    starter = "";
            }

            if (string.IsNullOrEmpty(OmitIfPuaChar((spans[0].Text ?? "").Trim())))
            {
                spans.RemoveAt(0);
                if (spans.Count > 0)
                    x0 = spans[0].Bbox?.X0 ?? x0;
            }

            var sb = new StringBuilder();
            sb.Append(indent).Append(starter);

            for (int li = 1; li < textLines.Count; li++)
            {
                var line = textLines[li];
                float thisX0 = line.Bbox?.X0 ?? 0f;
                if (thisX0 < x0 - 2f)
                {
                    sb.Append(GetStyledText(spans, false));
                    sb.Append("\n\n").Append(indent).Append(starter);
                    spans = line.Spans != null ? new List<ExtendedSpan>(line.Spans) : new List<ExtendedSpan>();
                    if (spans.Count > 0 && string.IsNullOrEmpty(OmitIfPuaChar((spans[0].Text ?? "").Trim())))
                    {
                        spans.RemoveAt(0);
                        if (spans.Count > 0)
                            x0 = spans[0].Bbox?.X0 ?? thisX0;
                    }
                }
                else if (line.Spans != null)
                    spans.AddRange(line.Spans);

                x0 = thisX0;
            }

            sb.Append(GetStyledText(spans, false));
            return sb.Append("\n\n").ToString();
        }

        private static string FootnoteToMd(List<TextLineInfo> textLines)
        {
            if (textLines == null || textLines.Count == 0)
                return "";
            var spans = new List<ExtendedSpan>();
            if (textLines[0].Spans != null)
                spans.AddRange(textLines[0].Spans);
            var sb = new StringBuilder("> ");
            for (int li = 1; li < textLines.Count; li++)
            {
                var line = textLines[li];
                if (IsSuperscripted(line))
                {
                    sb.Append(GetStyledText(spans, false));
                    sb.Append("\n\n> ");
                    spans = line.Spans != null ? new List<ExtendedSpan>(line.Spans) : new List<ExtendedSpan>();
                }
                else if (line.Spans != null)
                    spans.AddRange(line.Spans);
            }

            sb.Append(GetStyledText(spans, false));
            return sb.Append("\n\n").ToString();
        }

        private static string TextToMd(List<TextLineInfo> textLines, bool ignoreCode)
        {
            if (textLines == null || textLines.Count == 0)
                return "";
            if (IsSuperscripted(textLines[0]))
                return FootnoteToMd(textLines);
            if (!ignoreCode && IsMonospaced(textLines))
                return CodeBlockToMd(textLines);
            var spans = CollectSpans(textLines);
            if (spans.Count == 0)
                return "\n\n";
            string output = GetStyledText(spans, ignoreCode);
            return output + "\n\n";
        }

        private static string ListItemToText(List<TextLineInfo> textLines, int level)
        {
            if (textLines == null || textLines.Count == 0)
                return "";
            string indent = new string(' ', Math.Max(0, level - 1) * 3);
            var firstLine = textLines[0];
            float x0 = firstLine.Bbox?.X0 ?? 0f;
            var spans = new List<ExtendedSpan>();
            if (firstLine.Spans != null)
                spans.AddRange(firstLine.Spans);
            if (spans.Count == 0)
                return "";

            if (string.IsNullOrEmpty(OmitIfPuaChar((spans[0].Text ?? "").Trim())))
            {
                spans.RemoveAt(0);
                if (spans.Count > 0)
                    x0 = spans[0].Bbox?.X0 ?? x0;
            }

            var sb = new StringBuilder();
            sb.Append(indent);
            for (int li = 1; li < textLines.Count; li++)
            {
                var line = textLines[li];
                float thisX0 = line.Bbox?.X0 ?? 0f;
                if (thisX0 < x0 - 2f)
                {
                    sb.Append(GetPlainText(spans));
                    sb.Append("\n\n").Append(indent);
                    spans = line.Spans != null ? new List<ExtendedSpan>(line.Spans) : new List<ExtendedSpan>();
                    if (spans.Count > 0 && string.IsNullOrEmpty(OmitIfPuaChar((spans[0].Text ?? "").Trim())))
                    {
                        spans.RemoveAt(0);
                        if (spans.Count > 0)
                            x0 = spans[0].Bbox?.X0 ?? thisX0;
                    }
                }
                else if (line.Spans != null)
                    spans.AddRange(line.Spans);
                x0 = thisX0;
            }

            sb.Append(GetPlainText(spans));
            return sb.ToString().TrimEnd() + "\n\n";
        }

        private static string FootnoteToText(List<TextLineInfo> textLines)
        {
            if (textLines == null || textLines.Count == 0)
                return "";
            var spans = new List<ExtendedSpan>();
            if (textLines[0].Spans != null)
                spans.AddRange(textLines[0].Spans);
            var sb = new StringBuilder("> ");
            for (int li = 1; li < textLines.Count; li++)
            {
                var line = textLines[li];
                if (IsSuperscripted(line))
                {
                    sb.Append(GetPlainText(spans));
                    sb.Append("\n\n> ");
                    spans = line.Spans != null ? new List<ExtendedSpan>(line.Spans) : new List<ExtendedSpan>();
                }
                else if (line.Spans != null)
                    spans.AddRange(line.Spans);
            }

            sb.Append(GetPlainText(spans));
            return sb.ToString().TrimEnd() + "\n\n";
        }

        private static string TextToText(List<TextLineInfo> textLines, bool ignoreCode)
        {
            if (textLines == null || textLines.Count == 0)
                return "";
            if (IsSuperscripted(textLines[0]))
                return FootnoteToText(textLines);
            if (!ignoreCode && IsMonospaced(textLines))
                return CodeBlockToText(textLines);
            var spans = CollectSpans(textLines);
            return GetPlainText(spans) + "\n\n";
        }

        /// <summary>
        /// Map the layout box index of each list item to its hierarchy level.
        ///
        /// This post-layout heuristic walks contiguous segments of <c>list-item</c>
        /// boxes and assigns increasing levels when the left coordinate moves
        /// sufficiently to the right, mirroring
        /// create_list_item_levels equivalent.
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
        /// Equivalent of parse_document helper.
        /// </summary>
    public static class DocumentLayout
    {
        /// <summary>
        /// Normalizes page indices like <c>parse_document</c>: dedupe, sort, negative indices, bounds check.
        /// </summary>
        public static List<int> NormalizePageIndices(int pageCount, List<int> pages)
        {
            if (pageCount < 0)
                throw new ArgumentOutOfRangeException(nameof(pageCount));
            if (pages == null)
                return Enumerable.Range(0, pageCount).ToList();
            if (pages.Count == 0)
                return new List<int>();

            var set = new SortedSet<int>();
            foreach (int p in pages)
            {
                int q = p;
                while (q < 0)
                    q += pageCount;
                if (q < 0 || q >= pageCount)
                    throw new ArgumentOutOfRangeException(nameof(pages),
                        $"'pages' must contain indices in [0, {pageCount}) (after normalizing negatives); got {p}.");
                set.Add(q);
            }

            return set.ToList();
        }

        /// <summary>
        /// Parse document into <see cref="ParsedDocument"/> (<c>parse_document</c> pipeline).
        /// Pipeline: optional PDF <c>StructTreeRoot</c> removal, <c>OCRMode</c> + optional <see cref="OcrPageFunction"/>,
        /// <c>extractDICT</c>, synthetic layout (stext blocks + <see cref="TableFinder"/>), <c>clean_pictures</c> /
        /// <c>add_image_orphans</c> / <c>clean_tables</c>, <c>find_reading_order</c>, <c>find_tables</c>, then box materialization.
        /// </summary>
        /// <remarks>
        /// The native layout API is not available in this .NET port; layout regions are derived from stext blocks plus table detection.
        /// <c>complete_table_structure</c> virtual lines are not generated yet (empty <c>add_lines</c>).
        /// </remarks>
        public static ParsedDocument ParseDocument(
            Document doc,
            string filename = "",
            int imageDpi = 150,
            string imageFormat = "png",
            string imagePath = "",
            int ocrDpi = 300,
            List<int> pages = null,
            bool writeImages = false,
            bool embedImages = false,
            bool showProgress = false,
            bool forceText = false,
            bool useOcr = true,
            string ocrLanguage = "eng",
            bool forceOcr = false,
            bool keepOcrText = false,
            OcrPageFunction ocrFunction = null)
        {
            if (embedImages && writeImages)
                throw new ArgumentException("Cannot both embed and write images.");

            LayoutParseHelpers.TryRemovePdfStructTreeRoot(doc);

            string docBaseName = Path.GetFileNameWithoutExtension(
                string.IsNullOrEmpty(doc?.Name) ? "document" : doc.Name);
            if (string.IsNullOrEmpty(docBaseName))
                docBaseName = "document";

            string docFileLabel = string.IsNullOrEmpty(doc?.Name)
                ? (string.IsNullOrEmpty(filename) ? docBaseName : Path.GetFileName(filename))
                : Path.GetFileName(doc.Name);
            if (string.IsNullOrEmpty(docFileLabel))
                docFileLabel = docBaseName;

            OcrMode effectiveOcr = OcrMode.Never;
            if (forceOcr)
                effectiveOcr = keepOcrText ? OcrMode.AlwaysPreservingOld : OcrMode.AlwaysRemovingOld;
            else if (useOcr)
                effectiveOcr = keepOcrText ? OcrMode.SelectPreservingOld : OcrMode.SelectRemovingOld;

            OcrPageFunction ocrImpl = ocrFunction ?? LayoutParseHelpers.SelectOcrFunction();
            if (effectiveOcr == OcrMode.AlwaysRemovingOld || effectiveOcr == OcrMode.AlwaysPreservingOld)
            {
                if (ocrImpl == null)
                    throw new ArgumentException("Always OCR is requested but no OCR function is available.");
            }
            else if (effectiveOcr != OcrMode.Never && ocrImpl == null)
                effectiveOcr = OcrMode.Never;

            var document = new ParsedDocument
            {
                Filename = !string.IsNullOrEmpty(doc?.Name) ? doc.Name : filename,
                PageCount = doc.PageCount,
                Toc = doc.GetToc().Cast<object>().ToList(),
                Metadata = doc.MetaData,
                FormFields = Utils.ExtractFormFieldsWithPages(doc),
                ImageDpi = imageDpi,
                ImageFormat = imageFormat,
                ImagePath = imagePath,
                UseOcr = effectiveOcr != OcrMode.Never,
                OcrMode = effectiveOcr,
                ForceText = forceText,
                EmbedImages = embedImages,
                WriteImages = writeImages,
                Pages = new List<PageLayout>()
            };

            List<int> pageIndices = pages == null
                ? Enumerable.Range(0, doc.PageCount).ToList()
                : LayoutParseHelpers.ResolvePageFilter(doc.PageCount, pages);

            var progressBar = showProgress && pageIndices.Count >= 5
                ? ProgressBar.Create(pageIndices.Cast<object>().ToList())
                : null;

            try
            {
                foreach (int pno in pageIndices)
                {
                    if (progressBar != null && !progressBar.MoveNext())
                        break;

                    Page page = doc.LoadPage(pno);
                    TextPage textPage = null;
                    try
                    {
                        page.RemoveRotation();

                        bool pageFullOcred = false;
                        bool pageTextOcred = false;

                        textPage = page.GetTextPage(
                            clip: new Rect(float.NegativeInfinity, float.NegativeInfinity,
                                float.PositiveInfinity, float.PositiveInfinity),
                            flags: Utils.FLAGS);
                        PageInfo pageInfo = textPage.ExtractDict(null, false);
                        List<Block> blocks = pageInfo.Blocks ?? new List<Block>();

                        Dictionary<string, object> pageAnalysis = new Dictionary<string, object>
                        {
                            ["needs_ocr"] = false,
                        };
                        if (effectiveOcr == OcrMode.SelectRemovingOld || effectiveOcr == OcrMode.SelectPreservingOld)
                            pageAnalysis = Utils.AnalyzePage(page, blocks);

                        bool keepOcrTextRun = effectiveOcr == OcrMode.SelectPreservingOld
                            || effectiveOcr == OcrMode.AlwaysPreservingOld;

                        bool runOcr = false;
                        if (effectiveOcr == OcrMode.AlwaysRemovingOld || effectiveOcr == OcrMode.AlwaysPreservingOld)
                            runOcr = true;
                        else if (
                            (effectiveOcr == OcrMode.SelectRemovingOld || effectiveOcr == OcrMode.SelectPreservingOld)
                            && pageAnalysis.TryGetValue("needs_ocr", out object n) && n is bool nb && nb)
                            runOcr = true;

                        if (runOcr && ocrImpl != null)
                        {
                            ocrImpl(page, ocrDpi, ocrLanguage, keepOcrText: keepOcrTextRun);
                            textPage.Dispose();
                            textPage = page.GetTextPage(
                                clip: new Rect(float.NegativeInfinity, float.NegativeInfinity,
                                    float.PositiveInfinity, float.PositiveInfinity),
                                flags: Utils.FLAGS);
                            pageInfo = textPage.ExtractDict(null, false);
                            blocks = pageInfo.Blocks ?? new List<Block>();
                            pageFullOcred = true;
                        }

                        List<Block> fulltext = blocks.Where(b => b.Type == 0).ToList();

                        List<LayoutInfoEntry> layout = LayoutParseHelpers.BuildLayoutInformation(page, blocks);
                        bool tablesExist = LayoutParseHelpers.TablesExist(layout);

                        if (!pageFullOcred)
                        {
                            LayoutParseHelpers.CleanPictures(page, blocks, layout);
                            LayoutParseHelpers.AddImageOrphans(page, blocks, layout);
                        }

                        if (tablesExist)
                            LayoutParseHelpers.CleanTables(page, blocks, layout);

                        layout = LayoutParseHelpers.FindReadingOrder(page.Rect, blocks, layout);

                        List<Tuple<Point, Point>> addLines = null;
                        List<Rect> addBoxes = null;
                        if (tablesExist && !pageFullOcred)
                        {
                            var complete = LayoutParseHelpers.CompleteTableStructure(page, layout);
                            addLines = complete.lines;
                            addBoxes = complete.boxes;
                        }

                        TableFinder tbf = null;
                        if (tablesExist)
                        {
                            try
                            {
                                tbf = TableFinderHelper.FindTables(
                                    page,
                                    strategy: "lines_strict",
                                    add_lines: addLines,
                                    add_boxes: addBoxes);
                            }
                            catch
                            {
                                tbf = null;
                            }
                        }

                        List<Block> tableBlocks;
                        if (tablesExist && !(pageFullOcred || pageTextOcred))
                        {
                            PageInfo rawInfo = textPage.ExtractRAWDict(null, false);
                            tableBlocks = (rawInfo.Blocks ?? new List<Block>()).Where(b => b.Type == 0).ToList();
                        }
                        else
                            tableBlocks = fulltext;

                        var pageLayout = new PageLayout
                        {
                            PageNumber = pno + 1,
                            Width = page.Rect.Width,
                            Height = page.Rect.Height,
                            Boxes = new List<LayoutBox>(),
                            FullOcred = pageFullOcred,
                            TextOcred = pageTextOcred,
                            FullText = fulltext,
                            Words = new List<object>(),
                            Links = page.GetLinks()
                        };

                        foreach (LayoutInfoEntry le in layout)
                        {
                            if (le?.Bbox == null || Utils.BboxIsEmpty(le.Bbox))
                                continue;

                            Rect clip = new Rect(le.Bbox);
                            string bClass = le.Class ?? "text";

                            if (bClass == "picture" || bClass == "formula")
                            {
                                var picBox = new LayoutBox
                                {
                                    X0 = clip.X0,
                                    Y0 = clip.Y0,
                                    X1 = clip.X1,
                                    Y1 = clip.Y1,
                                    BoxClass = bClass,
                                    Image = null,
                                    ImageMarkdownRef = null
                                };

                                if (embedImages || writeImages)
                                {
                                    using (Pixmap pix = page.GetPixmap(clip: clip, dpi: imageDpi))
                                    {
                                        if (pix != null && !pix.IRect.IsEmpty)
                                        {
                                            if (embedImages)
                                                picBox.Image = pix.ToBytes(imageFormat);
                                            else if (writeImages)
                                            {
                                                string imgFile =
                                                    $"{docFileLabel}-{pno + 1:0000}-{pageLayout.Boxes.Count:00}.{imageFormat}";
                                                var (mdRef, savePath) = Utils.MdPath(imagePath, imgFile);
                                                picBox.ImageMarkdownRef = mdRef;
                                                pix.Save(savePath, imageFormat);
                                            }
                                        }
                                    }
                                }

                                if (forceText)
                                {
                                    var picLines = GetTextLines.GetRawLines(
                                        textpage: null,
                                        blocks: fulltext,
                                        clip: clip,
                                        ignoreInvisible: !pageFullOcred,
                                        onlyHorizontal: false);
                                    if (picLines.Count > 0)
                                    {
                                        picBox.TextLines = picLines
                                            .Select(l => new TextLineInfo { Bbox = l.Rect, Spans = l.Spans })
                                            .ToList();
                                    }
                                }

                                pageLayout.Boxes.Add(picBox);
                            }
                            else if (bClass == "table")
                            {
                                var layoutbox = new LayoutBox
                                {
                                    X0 = clip.X0,
                                    Y0 = clip.Y0,
                                    X1 = clip.X1,
                                    Y1 = clip.Y1,
                                    BoxClass = "table"
                                };

                                try
                                {
                                    Table table = tbf?.tables?.FirstOrDefault(tab =>
                                        tab?.bbox != null
                                        && LayoutParseHelpers.IntersectionOverUnion(tab.bbox, clip) > 0.6f);

                                    if (table != null)
                                    {
                                        int rc = table.row_count;
                                        int cc = table.col_count;
                                        var cellsOut = new List<List<float[]>>();
                                        foreach (TableRow row in table.rows)
                                        {
                                            var line = new List<float[]>();
                                            foreach (Rect c in row.cells)
                                            {
                                                if (c == null)
                                                {
                                                    line.Add(null);
                                                    continue;
                                                }

                                                line.Add(new[] { c.X0, c.Y0, c.X1, c.Y1 });
                                            }

                                            cellsOut.Add(line);
                                        }

                                        if (table.header?.external == true && table.header.cells != null)
                                        {
                                            var headerLine = table.header.cells
                                                .Select(c => c == null ? null : new[] { c.X0, c.Y0, c.X1, c.Y1 })
                                                .ToList();
                                            cellsOut.Insert(0, headerLine);
                                            rc += 1;
                                        }

                                        List<List<string>> extract = table.Extract();
                                        string md = table.ToMarkdown();

                                        layoutbox.Table = new Dictionary<string, object>
                                        {
                                            ["bbox"] = new List<float> { table.bbox.X0, table.bbox.Y0, table.bbox.X1, table.bbox.Y1 },
                                            ["row_count"] = rc,
                                            ["col_count"] = cc,
                                            ["cells"] = cellsOut,
                                            ["extract"] = extract,
                                            ["markdown"] = md
                                        };
                                    }
                                    else
                                        throw new InvalidOperationException("no matching table");
                                }
                                catch
                                {
                                    layoutbox.BoxClass = "table-fallback";
                                    if (embedImages || writeImages)
                                    {
                                        using (Pixmap pix = page.GetPixmap(clip: clip, dpi: imageDpi))
                                        {
                                            if (pix != null && !pix.IRect.IsEmpty)
                                            {
                                                if (embedImages)
                                                    layoutbox.Image = pix.ToBytes(imageFormat);
                                                else if (writeImages)
                                                {
                                                    string imgFile =
                                                        $"{docFileLabel}-{pno + 1:0000}-{pageLayout.Boxes.Count:00}.{imageFormat}";
                                                    var (mdRef, savePath) = Utils.MdPath(imagePath, imgFile);
                                                    layoutbox.ImageMarkdownRef = mdRef;
                                                    pix.Save(savePath, imageFormat);
                                                }
                                            }
                                        }
                                    }

                                    if (forceText)
                                    {
                                        var fbLines = GetTextLines.GetRawLines(
                                            textpage: null,
                                            blocks: fulltext,
                                            clip: clip,
                                            ignoreInvisible: !pageFullOcred,
                                            onlyHorizontal: false);
                                        if (fbLines.Count > 0)
                                        {
                                            layoutbox.TextLines = fbLines
                                                .Select(l => new TextLineInfo { Bbox = l.Rect, Spans = l.Spans })
                                                .ToList();
                                            if (fbLines.Count == 1 || fbLines.Max(l => l.Spans?.Count ?? 0) < 2)
                                                layoutbox.BoxClass = "text";
                                        }
                                    }
                                }

                                pageLayout.Boxes.Add(layoutbox);
                            }
                            else
                            {
                                var textLines = GetTextLines.GetRawLines(
                                    textpage: null,
                                    blocks: fulltext,
                                    clip: clip,
                                    ignoreInvisible: !pageFullOcred,
                                    onlyHorizontal: false);
                                if (textLines.Count == 0)
                                    continue;

                                Rect boxRect = Utils.JoinRects(textLines.Select(l => l.Rect).ToList());
                                if (Utils.BboxIsEmpty(boxRect))
                                    boxRect = clip;

                                pageLayout.Boxes.Add(new LayoutBox
                                {
                                    X0 = boxRect.X0,
                                    Y0 = boxRect.Y0,
                                    X1 = boxRect.X1,
                                    Y1 = boxRect.Y1,
                                    BoxClass = bClass == "table-fallback" ? "table-fallback" : "text",
                                    TextLines = textLines
                                        .Select(l => new TextLineInfo { Bbox = l.Rect, Spans = l.Spans })
                                        .ToList()
                                });
                            }
                        }

                        document.Pages.Add(pageLayout);
                    }
                    finally
                    {
                        textPage?.Dispose();
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
    /// <c>LayoutBox</c> JSON: <c>image</c> is base64 bytes or a path string (see <c>LayoutEncoder</c>).
    /// </summary>
    public class LayoutBoxJsonConverter : JsonConverter<LayoutBox>
    {
        public override void WriteJson(JsonWriter writer, LayoutBox value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("x0");
            writer.WriteValue(value.X0);
            writer.WritePropertyName("y0");
            writer.WriteValue(value.Y0);
            writer.WritePropertyName("x1");
            writer.WriteValue(value.X1);
            writer.WritePropertyName("y1");
            writer.WriteValue(value.Y1);
            writer.WritePropertyName("boxclass");
            writer.WriteValue(value.BoxClass);
            writer.WritePropertyName("image");
            if (value.Image != null && value.Image.Length > 0)
                writer.WriteValue(Convert.ToBase64String(value.Image));
            else if (!string.IsNullOrEmpty(value.ImageMarkdownRef))
                writer.WriteValue(value.ImageMarkdownRef);
            else
                writer.WriteNull();
            writer.WritePropertyName("table");
            if (value.Table != null)
                serializer.Serialize(writer, value.Table);
            else
                writer.WriteNull();
            writer.WritePropertyName("textlines");
            if (value.TextLines != null)
                serializer.Serialize(writer, value.TextLines);
            else
                writer.WriteNull();
            writer.WriteEndObject();
        }

        public override LayoutBox ReadJson(
            JsonReader reader,
            Type objectType,
            LayoutBox existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override bool CanRead => false;
    }

    /// <summary>
    /// Custom JSON converter for MuPDF geometry and raw bytes, matching <c>LayoutEncoder</c> rules.
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
