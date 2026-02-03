using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MuPDF.NET;

namespace MuPDF.NET4LLM.Helpers
{
    /// <summary>
    /// Header identification based on font sizes
    /// </summary>
    public class IdentifyHeaders
    {
        private Dictionary<int, string> _headerId = new Dictionary<int, string>();
        private float _bodyLimit;

        public IdentifyHeaders(
            object doc, // Document or string
            List<int> pages = null,
            float bodyLimit = 12.0f, // Force this to be body text
            int maxLevels = 6) // Accept this many header levels
        {
            if (maxLevels < 1 || maxLevels > 6)
                throw new ArgumentException("max_levels must be between 1 and 6");

            Document mydoc = doc as Document;
            if (mydoc == null)
            {
                mydoc = new Document(doc.ToString());
            }

            // Remove StructTreeRoot to avoid possible performance degradation
            // We will not use the structure tree anyway.
            if (pages == null) // Use all pages if omitted
                pages = Enumerable.Range(0, mydoc.PageCount).ToList();

            Dictionary<int, int> fontSizes = new Dictionary<int, int>();

            foreach (int pno in pages)
            {
                Page page = mydoc.LoadPage(pno);
                // Use TEXTFLAGS_TEXT for proper text extraction (matches Python TEXTFLAGS_TEXT)
                int textFlags = (int)TextFlagsExtension.TEXTFLAGS_TEXT;
                TextPage textPage = page.GetTextPage(flags: textFlags);
                PageInfo pageInfo = textPage.ExtractDict(null, false);

                // Look at all non-empty horizontal spans
                foreach (var block in pageInfo.Blocks ?? new List<Block>())
                {
                    if (block.Type != 0) continue;
                    if (block.Lines == null) continue;

                    foreach (var line in block.Lines)
                    {
                        if (line.Spans == null) continue;
                        foreach (var span in line.Spans)
                        {
                            string text = span.Text ?? "";
                            if (Utils.IsWhite(text)) continue;

                            int fontSz = (int)Math.Round(span.Size); // Compute rounded fontsize
                            if (!fontSizes.ContainsKey(fontSz))
                                fontSizes[fontSz] = 0;
                            fontSizes[fontSz] += text.Trim().Length; // Add character count
                        }
                    }
                }

                textPage.Dispose();
                page.Dispose();
            }

            if (mydoc != doc as Document)
                // If opened here, close it now
                mydoc.Close();

            // Maps a fontsize to a string of multiple # header tag characters
            // If not provided, choose the most frequent font size as body text.
            // If no text at all on all pages, just use body_limit.
            // In any case all fonts not exceeding
            var sorted = fontSizes.OrderBy(kvp => (kvp.Value, kvp.Key)).ToList();
            if (sorted.Count > 0)
            {
                // Most frequent font size
                _bodyLimit = Math.Max(bodyLimit, sorted[sorted.Count - 1].Key);
            }
            else
            {
                _bodyLimit = bodyLimit;
            }

            // Identify up to 6 font sizes as header candidates
            var sizes = fontSizes.Keys
                .Where(f => f > _bodyLimit)
                .OrderByDescending(f => f)
                .Take(maxLevels)
                .ToList();

            // Make the header tag dictionary
            for (int i = 0; i < sizes.Count; i++)
            {
                _headerId[sizes[i]] = new string('#', i + 1) + " ";
            }

            if (_headerId.Count > 0)
                _bodyLimit = _headerId.Keys.Min() - 1;
        }

        /// <summary>
        /// Return appropriate markdown header prefix.
        /// Given a text span from a "dict"/"rawdict" extraction, determine the
        /// markdown header prefix string of 0 to n concatenated '#' characters.
        /// </summary>
        public string GetHeaderId(ExtendedSpan span, Page page = null)
        {
            int fontsize = (int)Math.Round(span.Size); // Compute fontsize
            if (fontsize <= _bodyLimit)
                return "";
            string hdrId = _headerId.ContainsKey(fontsize) ? _headerId[fontsize] : "";
            return hdrId;
        }
    }

    /// <summary>
    /// Header identification based on Table of Contents
    /// </summary>
    public class TocHeaders
    {
        private List<Toc> _toc;

        /// <summary>
        /// Read and store the TOC of the document.
        /// </summary>
        public TocHeaders(object doc)
        {
            Document mydoc = doc as Document;
            if (mydoc == null)
            {
                mydoc = new Document(doc.ToString());
            }

            _toc = mydoc.GetToc();

            if (mydoc != doc as Document)
                // If opened here, close it now
                mydoc.Close();
        }

        /// <summary>
        /// Return appropriate markdown header prefix.
        /// Given a text span from a "dict"/"rawdict" extraction, determine the
        /// markdown header prefix string of 0 to n concatenated '#' characters.
        /// </summary>
        public string GetHeaderId(ExtendedSpan span, Page page = null)
        {
            if (page == null)
                return "";
            // Check if this page has TOC entries with an actual title
            var myToc = _toc.Where(t => !string.IsNullOrEmpty(t.Title) && t.Page == page.Number + 1).ToList();
            if (myToc.Count == 0) // No TOC items present on this page
                return "";
            // Check if the span matches a TOC entry. This must be done in the
            // most forgiving way: exact matches are rare animals.
            string text = (span.Text ?? "").Trim(); // Remove leading and trailing whitespace
            foreach (var t in myToc)
            {
                string title = t.Title.Trim(); // Title of TOC entry
                int lvl = t.Level; // Level of TOC entry
                if (text.StartsWith(title) || title.StartsWith(text))
                {
                    // Found a match: return the header tag
                    return new string('#', lvl) + " ";
                }
            }
            return "";
        }
    }

    /// <summary>
    /// Parameters class to store page-specific information (matches Python dataclass)
    /// </summary>
    public class Parameters
    {
        public Page Page { get; set; }
        public string Filename { get; set; }
        public string MdString { get; set; } = "";
        public List<object> Images { get; set; } = new List<object>();
        public List<object> Tables { get; set; } = new List<object>();
        public List<object> Graphics { get; set; } = new List<object>();
        public List<object> Words { get; set; } = new List<object>();
        public List<Rect> LineRects { get; set; } = new List<Rect>();
        public bool AcceptInvisible { get; set; }
        public float[] BgColor { get; set; }
        public Rect Clip { get; set; }
        public List<LinkInfo> Links { get; set; } = new List<LinkInfo>();
        public List<Rect> AnnotRects { get; set; } = new List<Rect>();
        public TextPage TextPage { get; set; }
        public List<Rect> ImgRects { get; set; } = new List<Rect>();
        public List<Rect> TabRects0 { get; set; } = new List<Rect>();
        public Dictionary<int, Rect> TabRects { get; set; } = new Dictionary<int, Rect>();
        public List<Table> Tabs { get; set; } = new List<Table>();
        public List<int> WrittenTables { get; set; } = new List<int>();
        public List<int> WrittenImages { get; set; } = new List<int>();
        public List<PathInfo> ActualPaths { get; set; } = new List<PathInfo>();
        public List<Rect> VgClusters0 { get; set; } = new List<Rect>();
        public Dictionary<int, Rect> VgClusters { get; set; } = new Dictionary<int, Rect>();
    }

    /// <summary>
    /// Main markdown conversion utilities.
    /// Ported and adapted from the Python module helpers/pymupdf_rag.py in pymupdf4llm.
    /// </summary>
    public static class MuPdfRag
    {
        private const string GRAPHICS_TEXT = "\n![]({0})\n";

        /// <summary>
        /// Convert a document to Markdown, closely following the behavior of
        /// <c>pymupdf4llm.helpers.pymupdf_rag.ToMarkdown</c>.
        /// </summary>
        /// <param name="doc">Input <see cref="Document"/> to convert.</param>
        /// <param name="pages">
        /// Page numbers (0‑based) to process. When <c>null</c>, all pages are processed.
        /// </param>
        /// <param name="hdrInfo">
        /// Optional header resolver used to create Markdown headings. This can be
        /// an <see cref="IdentifyHeaders"/> instance, a <see cref="TocHeaders"/> instance,
        /// or <c>null</c> to auto‑detect headers.
        /// </param>
        /// <param name="writeImages">
        /// When <c>true</c>, images are written to disk and referenced by relative path.
        /// </param>
        /// <param name="embedImages">
        /// When <c>true</c>, images are embedded as <c>data:</c> URLs in the Markdown.
        /// Cannot be combined with <paramref name="writeImages"/>.
        /// </param>
        /// <param name="ignoreImages">
        /// When <c>true</c>, image regions are ignored entirely (no image and no OCR text).
        /// </param>
        /// <param name="ignoreGraphics">
        /// When <c>true</c>, vector graphics are ignored (no layout‑based table / column hints).
        /// </param>
        /// <param name="detectBgColor">
        /// When <c>true</c>, tries to detect a uniform page background to filter
        /// out large background rectangles from graphics analysis.
        /// </param>
        /// <param name="imagePath">
        /// Target directory for written images when <paramref name="writeImages"/> is <c>true</c>.
        /// </param>
        /// <param name="imageFormat">Image file format, e.g. <c>&quot;png&quot;</c> or <c>&quot;jpg&quot;</c>.</param>
        /// <param name="imageSizeLimit">
        /// Minimum relative size (\(0 \leq v &lt; 1\)) of images with respect to the page
        /// before they are considered for output.
        /// </param>
        /// <param name="filename">
        /// Logical filename used in image names and metadata; defaults to <see cref="Document.Name"/>.
        /// </param>
        /// <param name="forceText">
        /// When <c>true</c>, attempts to also extract text from image regions (e.g. diagrams)
        /// in addition to placing images.
        /// </param>
        /// <param name="pageChunks">
        /// When <c>true</c>, returns a JSON string describing per‑page “chunks” instead of raw Markdown.
        /// </param>
        /// <param name="pageSeparators">
        /// When <c>true</c>, appends an explicit <c>--- end of page=...</c> marker after each page.
        /// </param>
        /// <param name="margins">
        /// Optional margins in points. One value applies to all sides, two values to
        /// top/bottom and left/right, and four values to left, top, right, bottom.
        /// </param>
        /// <param name="dpi">
        /// Resolution used for image extraction where a <see cref="Pixmap"/> is rendered.
        /// </param>
        /// <param name="pageWidth">
        /// Page width used for reflowable documents when <see cref="Document.IsReflowable"/> is <c>true</c>.
        /// </param>
        /// <param name="pageHeight">
        /// Optional page height for reflowable documents. If <c>null</c>, a single tall page
        /// covering the whole document is created.
        /// </param>
        /// <param name="tableStrategy">
        /// Table detection strategy passed to <c>Page.GetTables</c>, e.g. <c>&quot;lines_strict&quot;</c>
        /// to mimic the Python default.
        /// </param>
        /// <param name="graphicsLimit">
        /// Optional upper bound on the number of path objects before graphics are ignored
        /// for layout analysis (similar to <c>graphics_limit</c> in Python).
        /// </param>
        /// <param name="fontsizeLimit">
        /// Minimum font size considered as “normal” text when computing some heuristics.
        /// </param>
        /// <param name="ignoreCode">
        /// When <c>true</c>, code blocks (mono‑spaced text) are not emitted as fenced code blocks.
        /// </param>
        /// <param name="extractWords">
        /// When <c>true</c>, the return value is a JSON description of page “chunks” with
        /// word positions, matching the Python <c>extract_words</c> mode.
        /// </param>
        /// <param name="showProgress">
        /// When <c>true</c>, prints a simple progress bar while processing pages.
        /// </param>
        /// <param name="useGlyphs">
        /// When <c>true</c>, uses glyph IDs for unknown Unicode characters, similar to
        /// <c>FZ_STEXT_USE_GID_FOR_UNKNOWN_UNICODE</c> in the C API.
        /// </param>
        /// <param name="ignoreAlpha">
        /// When <c>true</c>, treats fully transparent text as visible (affects OCR heuristics).
        /// </param>
        /// <returns>
        /// Markdown text for the selected pages, or a JSON string describing page chunks
        /// when <paramref name="pageChunks"/> / <paramref name="extractWords"/> is enabled.
        /// </returns>
        public static string ToMarkdown(
            Document doc,
            List<int> pages = null,
            object hdrInfo = null, // Can be IdentifyHeaders, TocHeaders, or null
            bool writeImages = false,
            bool embedImages = false,
            bool ignoreImages = false,
            bool ignoreGraphics = false,
            bool detectBgColor = true,
            string imagePath = "",
            string imageFormat = "png",
            float imageSizeLimit = 0.05f,
            string filename = null,
            bool forceText = true,
            bool pageChunks = false,
            bool pageSeparators = false,
            List<float> margins = null,
            int dpi = 150,
            float pageWidth = 612,
            float? pageHeight = null,
            string tableStrategy = "lines_strict",
            int? graphicsLimit = null,
            float fontsizeLimit = 3.0f,
            bool ignoreCode = false,
            bool extractWords = false,
            bool showProgress = false,
            bool useGlyphs = false,
            bool ignoreAlpha = false)
        {
            if (!writeImages && !embedImages && !forceText)
                throw new ArgumentException("Images and text on images cannot both be suppressed.");
            if (embedImages)
            {
                writeImages = false;
                imagePath = string.Empty;
            }
            if (imageSizeLimit < 0 || imageSizeLimit >= 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(imageSizeLimit),
                    "'imageSizeLimit' must be non-negative and less than 1.");
            }

            int DPI = dpi;
            bool IGNORE_CODE = ignoreCode;
            string IMG_EXTENSION = imageFormat;
            bool EXTRACT_WORDS = extractWords;
            if (EXTRACT_WORDS)
            {
                pageChunks = true;
                ignoreCode = true;
            }
            string IMG_PATH = imagePath;
            if (!string.IsNullOrEmpty(IMG_PATH) && writeImages && !Directory.Exists(IMG_PATH))
                Directory.CreateDirectory(IMG_PATH);

            string FILENAME = filename ?? doc.Name;
            // Assign configuration
            int? GRAPHICS_LIMIT = graphicsLimit;
            double FONTSIZE_LIMIT = fontsizeLimit;
            bool IGNORE_IMAGES = ignoreImages;
            bool IGNORE_GRAPHICS = ignoreGraphics;
            bool DETECT_BG_COLOR = detectBgColor;

            if (filename == null)
                filename = doc.Name;

            // Handle form PDFs and documents with annotations
            if (doc.IsFormPDF > 0 || (doc.IsPDF && doc.HasAnnots()))
            {
                doc.Bake();
            }

            // For reflowable documents, allow making 1 page for the whole document
            if (doc.IsReflowable)
            {
                if (pageHeight.HasValue)
                {
                    // Accept user page dimensions
                    doc.SetLayout(width: pageWidth, height: pageHeight.Value);
                }
                else
                {
                    // No page height limit given: make 1 page for whole document
                    doc.SetLayout(width: pageWidth, height: 792);
                    int pageCount = doc.PageCount;
                    float height = 792 * pageCount; // Height that covers full document
                    doc.SetLayout(width: pageWidth, height: height);
                }
            }

            if (pages == null) // Use all pages if no selection given
                pages = Enumerable.Range(0, doc.PageCount).ToList();

            // Process margins: convert to 4-element list
            if (margins == null)
                margins = new List<float> { 0, 0, 0, 0 };
            else if (margins.Count == 1)
                margins = new List<float> { margins[0], margins[0], margins[0], margins[0] };
            else if (margins.Count == 2)
                margins = new List<float> { 0, margins[0], 0, margins[1] };
            else if (margins.Count != 4)
                throw new ArgumentException("margins must be one, two or four floats");

            // If "hdr_info" is not an object with a method "get_header_id", scan the
            // document and use font sizes as header level indicators.
            Func<ExtendedSpan, Page, string> getHeaderId;

            if (hdrInfo is IdentifyHeaders idHdr)
                getHeaderId = idHdr.GetHeaderId;
            else if (hdrInfo is TocHeaders tocHdr)
                getHeaderId = tocHdr.GetHeaderId;
            else if (hdrInfo == null)
            {
                var idHdr2 = new IdentifyHeaders(doc, pages);
                getHeaderId = idHdr2.GetHeaderId;
            }
            else
                getHeaderId = (s, p) => "";

            // Initialize output based on page_chunks mode
            object documentOutput;
            if (!pageChunks)
            {
                documentOutput = new StringBuilder();
            }
            else
            {
                documentOutput = new List<Dictionary<string, object>>();
            }

            // Read the Table of Contents
            List<Toc> toc = doc.GetToc();

            // Text extraction flags: omit clipped text, collect styles
            int textFlags = (int)TextFlags.TEXT_MEDIABOX_CLIP | 
                           (int)mupdf.mupdf.FZ_STEXT_COLLECT_STYLES;
            
            // Optionally replace REPLACEMENT_CHARACTER by glyph number
            if (useGlyphs)
            {
                textFlags |= (int)mupdf.mupdf.FZ_STEXT_USE_GID_FOR_UNKNOWN_UNICODE;
            }

            // Note: Table FLAGS would be set here if we had access to pymupdf.table.FLAGS
            // In C#, this would need to be handled differently if table extraction uses flags

            var progressBar = showProgress && pages.Count > 5
                ? ProgressBar.Create(pages.Cast<object>().ToList())
                : null;

            try
            {
                if (showProgress)
                {
                    Console.WriteLine($"Processing {FILENAME}...");
                }

                foreach (int pno in pages)
                {
                    if (progressBar != null && !progressBar.MoveNext())
                        break;

                    Parameters pageParms = GetPageOutput(
                        doc, pno, margins, getHeaderId, writeImages, embedImages, ignoreImages,
                        imagePath, imageFormat, filename, forceText, dpi, ignoreCode,
                        ignoreGraphics, tableStrategy, detectBgColor, graphicsLimit,
                        ignoreAlpha, extractWords, pageSeparators, imageSizeLimit, textFlags);

                    if (!pageChunks)
                    {
                        ((StringBuilder)documentOutput).Append(pageParms.MdString);
                    }
                    else
                    {
                        // Build subset of TOC for this page
                        var pageTocs = toc.Where(t => t.Page == pno + 1).ToList();

                        var metadata = GetMetadata(doc, pno, FILENAME);
                        
                        var pageChunk = new Dictionary<string, object>
                        {
                            ["metadata"] = metadata,
                            ["toc_items"] = pageTocs,
                            ["tables"] = pageParms.Tables,
                            ["images"] = pageParms.Images,
                            ["graphics"] = pageParms.Graphics,
                            ["text"] = pageParms.MdString,
                            ["words"] = pageParms.Words
                        };
                        
                        ((List<Dictionary<string, object>>)documentOutput).Add(pageChunk);
                    }
                }
            }
            finally
            {
                progressBar?.Dispose();
            }

            if (!pageChunks)
            {
                return ((StringBuilder)documentOutput).ToString();
            }
            else
            {
                // For page_chunks mode, we need to return a structured format
                // Since System.Text.Json may not be available in all .NET versions,
                // we'll use Newtonsoft.Json if available, or return a simple string representation
                try
                {
                    return Newtonsoft.Json.JsonConvert.SerializeObject(documentOutput, Newtonsoft.Json.Formatting.Indented);
                }
                catch
                {
                    // Fallback: return a simple string representation
                    var sb = new StringBuilder();
                    foreach (var chunk in (List<Dictionary<string, object>>)documentOutput)
                    {
                        sb.AppendLine("--- Page Chunk ---");
                        foreach (var kvp in chunk)
                        {
                            sb.AppendLine($"{kvp.Key}: {kvp.Value}");
                        }
                        sb.AppendLine();
                    }
                    return sb.ToString();
                }
            }
        }

        /// <summary>
        /// Get maximum header ID from spans (matches Python max_header_id)
        /// </summary>
        private static string MaxHeaderId(
            List<ExtendedSpan> spans,
            Page page,
            Func<ExtendedSpan, Page, string> getHeaderId)
        {
            var hdrIds = spans
                .Select(s => getHeaderId(s, page))
                .Where(h => !string.IsNullOrEmpty(h))
                .Select(h => h.Trim().Length)
                .Where(l => l > 0)
                .Distinct()
                .OrderBy(l => l)
                .ToList();

            if (hdrIds.Count == 0)
                return "";

            // Return header tag with one less '#' than the minimum found
            return new string('#', hdrIds[0] - 1) + " ";
        }

        /// <summary>
        /// Accept a span and return a markdown link string.
        /// A link should overlap at least 70% of the span.
        /// </summary>
        private static string ResolveLinks(List<LinkInfo> links, ExtendedSpan span)
        {
            if (links == null || links.Count == 0 || span == null || span.Bbox == null)
                return null;

            Rect spanBbox = span.Bbox; // Span bbox

            foreach (var link in links)
            {
                // Only process URI links
                if (link.Kind != LinkType.LINK_URI || string.IsNullOrEmpty(link.Uri))
                    continue;

                if (link.From == null)
                    continue;

                // The hot area of the link
                // Middle point of hot area
                float middleX = (link.From.TopLeft.X + link.From.BottomRight.X) / 2;
                float middleY = (link.From.TopLeft.Y + link.From.BottomRight.Y) / 2;

                // Does not touch the bbox
                if (!(middleX >= spanBbox.X0 && middleX <= spanBbox.X1 &&
                    middleY >= spanBbox.Y0 && middleY <= spanBbox.Y1))
                    continue;

                string text = (span.Text ?? "").Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    return $"[{text}]({link.Uri})";
                }
            }

            return null;
        }

        /// <summary>
        /// Optionally render the rect part of a page.
        /// We will ignore images that are empty or that have an edge smaller
        /// than x% of the corresponding page edge.
        /// </summary>
        private static string SaveImage(
            Page page,
            Rect rect,
            int imageIndex,
            bool writeImages,
            bool embedImages,
            string imagePath,
            string imageFormat,
            string filename,
            int dpi,
            float imageSizeLimit)
        {
            // Check if image is too small
            if (rect.Width < page.Rect.Width * imageSizeLimit ||
                rect.Height < page.Rect.Height * imageSizeLimit)
            {
                return "";
            }

            if (!writeImages && !embedImages)
                return "";

            Pixmap pix = page.GetPixmap(clip: rect, dpi: dpi);
            try
            {
                if (pix.H <= 0 || pix.W <= 0)
                    return "";

                if (writeImages)
                {
                    // Ensure image path exists
                    if (!string.IsNullOrEmpty(imagePath) && !Directory.Exists(imagePath))
                    {
                        Directory.CreateDirectory(imagePath);
                    }

                    string safeFilename = Path.GetFileName(filename ?? "document").Replace(" ", "-");
                    string indexPart = imageIndex >= 0 ? imageIndex.ToString() : "full";
                    string imageFilename = string.IsNullOrEmpty(imagePath)
                        ? $"{safeFilename}-{page.Number}-{indexPart}.{imageFormat}"
                        : Path.Combine(imagePath, $"{safeFilename}-{page.Number}-{indexPart}.{imageFormat}");
                    pix.Save(imageFilename);
                    return imageFilename.Replace("\\", "/");
                }
                else if (embedImages)
                {
                    // Make a base64 encoded string of the image
                    byte[] imageBytes = pix.ToBytes(imageFormat);
                    string base64 = Convert.ToBase64String(imageBytes);
                    return $"data:image/{imageFormat};base64,{base64}";
                }
            }
            finally
            {
                pix.Dispose();
            }

            return "";
        }

        /// <summary>
        /// Check if page exclusively contains OCR text.
        /// For this to be true, all text must be written as "ignore-text".
        /// </summary>
        private static bool PageIsOcr(Page page)
        {
            try
            {
                var bboxLog = page.GetBboxlog();
                var textTypes = new HashSet<string>(bboxLog
                    .Where(b => b.Type != null && b.Type.Contains("text"))
                    .Select(b => b.Type)
                    .Distinct());

                return textTypes.Count == 1 && textTypes.Contains("ignore-text");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get metadata for a page (matches Python get_metadata)
        /// </summary>
        private static Dictionary<string, object> GetMetadata(Document doc, int pno, string filename)
        {
            var meta = new Dictionary<string, object>();
            if (doc.MetaData != null)
            {
                foreach (var kvp in doc.MetaData)
                {
                    meta[kvp.Key] = kvp.Value;
                }
            }
            meta["file_path"] = filename;
            meta["page_count"] = doc.PageCount;
            meta["page"] = pno + 1;
            return meta;
        }

        /// <summary>
        /// Reorder words in lines.
        /// The argument list must be presorted by bottom, then left coordinates.
        /// Words with similar top / bottom coordinates are assumed to belong to
        /// the same line and will be sorted left to right within that line.
        /// </summary>
        private static List<WordBlock> SortWords(List<WordBlock> words)
        {
            if (words == null || words.Count == 0)
                return new List<WordBlock>();

            List<WordBlock> nwords = new List<WordBlock>();
            List<WordBlock> line = new List<WordBlock> { words[0] };
            Rect lrect = new Rect(words[0].X0, words[0].Y0, words[0].X1, words[0].Y1);

            for (int i = 1; i < words.Count; i++)
            {
                var word = words[i];
                var wrect = new Rect(word.X0, word.Y0, word.X1, word.Y1);
                if (Math.Abs(wrect.Y0 - lrect.Y0) <= 3 || Math.Abs(wrect.Y1 - lrect.Y1) <= 3)
                {
                    line.Add(word);
                    lrect = Utils.JoinRects(new List<Rect> { lrect, wrect });
                }
                else
                {
                    line = line.OrderBy(w => w.X0).ToList();
                    nwords.AddRange(line);
                    line = new List<WordBlock> { word };
                    lrect = new Rect(word.X0, word.Y0, word.X1, word.Y1);
                }
            }

            line = line.OrderBy(w => w.X0).ToList();
            nwords.AddRange(line);
            return nwords;
        }

        /// <summary>
        /// Output tables above given text rectangle (matches Python output_tables)
        /// </summary>
        private static string OutputTables(Parameters parms, Rect textRect, bool extractWords)
        {
            StringBuilder thisMd = new StringBuilder(); // Markdown string for table(s) content
            
            if (textRect != null) // Select tables above the text block
            {
                var tabCandidates = parms.TabRects
                    .Where(kvp => kvp.Value.Y1 <= textRect.Y0 && !parms.WrittenTables.Contains(kvp.Key) &&
                        (textRect.X0 <= kvp.Value.X0 && kvp.Value.X0 < textRect.X1 ||
                         textRect.X0 < kvp.Value.X1 && kvp.Value.X1 <= textRect.X1 ||
                         kvp.Value.X0 <= textRect.X0 && textRect.X1 <= kvp.Value.X1))
                    .OrderBy(kvp => kvp.Value.Y1)
                    .ThenBy(kvp => kvp.Value.X0)
                    .ToList();
                
                foreach (var kvp in tabCandidates)
                {
                    int i = kvp.Key;
                    thisMd.Append("\n" + parms.Tabs[i].ToMarkdown(clean: false) + "\n");
                    
                    if (extractWords)
                    {
                        // For "words" extraction, add table cells as line rects
                        var cells = new List<Rect>();
                        if (parms.Tabs[i].header != null && parms.Tabs[i].header.cells != null)
                        {
                            foreach (var c in parms.Tabs[i].header.cells)
                            {
                                if (c != null)
                                    cells.Add(c);
                            }
                        }
                        if (parms.Tabs[i].cells != null)
                        {
                            foreach (var c in parms.Tabs[i].cells)
                            {
                                if (c != null)
                                    cells.Add(c);
                            }
                        }
                        cells = cells.Distinct()
                            .OrderBy(c => c.Y1)
                            .ThenBy(c => c.X0)
                            .ToList();
                        parms.LineRects.AddRange(cells);
                    }
                    parms.WrittenTables.Add(i); // Do not touch this table twice
                }
            }
            else // Output all remaining tables
            {
                foreach (var kvp in parms.TabRects)
                {
                    int i = kvp.Key;
                    if (parms.WrittenTables.Contains(i))
                        continue;
                    
                    thisMd.Append("\n" + parms.Tabs[i].ToMarkdown(clean: false) + "\n");
                    
                    if (extractWords)
                    {
                        // For "words" extraction, add table cells as line rects
                        var cells = new List<Rect>();
                        if (parms.Tabs[i].header != null && parms.Tabs[i].header.cells != null)
                        {
                            foreach (var c in parms.Tabs[i].header.cells)
                            {
                                if (c != null)
                                    cells.Add(c);
                            }
                        }
                        if (parms.Tabs[i].cells != null)
                        {
                            foreach (var c in parms.Tabs[i].cells)
                            {
                                if (c != null)
                                    cells.Add(c);
                            }
                        }
                        cells = cells.Distinct()
                            .OrderBy(c => c.Y1)
                            .ThenBy(c => c.X0)
                            .ToList();
                        parms.LineRects.AddRange(cells);
                    }
                    parms.WrittenTables.Add(i); // Do not touch this table twice
                }
            }
            
            return thisMd.ToString();
        }

        /// <summary>
        /// Output images and graphics above text rectangle (matches Python output_images)
        /// </summary>
        private static string OutputImages(Parameters parms, Rect textRect, bool forceText, 
            bool writeImages, bool embedImages, string imagePath, string imageFormat, 
            string filename, int dpi, float imageSizeLimit, Func<ExtendedSpan, Page, string> getHeaderId)
        {
            if (parms.ImgRects == null || parms.ImgRects.Count == 0)
                return "";
            
            StringBuilder thisMd = new StringBuilder(); // Markdown string
            
            if (textRect != null) // Select images above the text block
            {
                for (int i = 0; i < parms.ImgRects.Count; i++)
                {
                    if (parms.WrittenImages.Contains(i))
                        continue;
                    
                    Rect imgRect = parms.ImgRects[i];
                    if (imgRect.Y0 > textRect.Y0)
                        continue;
                    if (imgRect.X0 >= textRect.X1 || imgRect.X1 <= textRect.X0)
                        continue;
                    
                    string pathname = SaveImage(parms.Page, imgRect, i, writeImages, embedImages, 
                        imagePath, imageFormat, filename, dpi, imageSizeLimit);
                    parms.WrittenImages.Add(i); // Do not touch this image twice
                    
                    if (!string.IsNullOrEmpty(pathname))
                    {
                        thisMd.AppendFormat(GRAPHICS_TEXT, pathname);
                    }
                    
                    if (forceText)
                    {
                        // Recursive invocation
                        string imgTxt = WriteText(parms, imgRect, getHeaderId, forceText: true, 
                            ignoreCode: false, extractWords: false);
                        if (!Utils.IsWhite(imgTxt)) // Was there text at all?
                        {
                            thisMd.Append(imgTxt);
                        }
                    }
                }
            }
            else // Output all remaining images
            {
                for (int i = 0; i < parms.ImgRects.Count; i++)
                {
                    if (parms.WrittenImages.Contains(i))
                        continue;
                    
                    string pathname = SaveImage(parms.Page, parms.ImgRects[i], i, writeImages, embedImages, 
                        imagePath, imageFormat, filename, dpi, imageSizeLimit);
                    parms.WrittenImages.Add(i); // Do not touch this image twice
                    
                    if (!string.IsNullOrEmpty(pathname))
                    {
                        thisMd.AppendFormat(GRAPHICS_TEXT, pathname);
                    }
                    
                    if (forceText)
                    {
                        string imgTxt = WriteText(parms, parms.ImgRects[i], getHeaderId, forceText: true, 
                            ignoreCode: false, extractWords: false);
                        if (!Utils.IsWhite(imgTxt))
                        {
                            thisMd.Append(imgTxt);
                        }
                    }
                }
            }
            
            return thisMd.ToString();
        }

        /// <summary>
        /// Output the text found inside the given clip.
        /// This is an alternative for plain text in that it outputs
        /// text enriched with markdown styling.
        /// The logic is capable of recognizing headers, body text, code blocks,
        /// inline code, bold, italic and bold-italic styling.
        /// There is also some effort for list supported (ordered / unordered) in
        /// that typical characters are replaced by respective markdown characters.
        /// 'tables'/'images' indicate whether this execution should output these
        /// objects.
        /// </summary>
        private static string WriteText(Parameters parms, Rect clip, 
            Func<ExtendedSpan, Page, string> getHeaderId, bool forceText, bool ignoreCode, bool extractWords)
        {
            if (clip == null)
                clip = parms.Clip;
            
            StringBuilder outString = new StringBuilder();
            
            // This is a list of tuples (linerect, spanlist)
            var nlines = GetTextLines.GetRawLines(parms.TextPage, null, clip, tolerance: 3, 
                ignoreInvisible: !parms.AcceptInvisible);
            
            // Filter out lines that intersect with tables
            nlines = nlines
                .Where(l => Utils.OutsideAllBboxes(l.Rect, parms.TabRects.Values))
                .ToList();
            
            parms.LineRects.AddRange(nlines.Select(l => l.Rect)); // Store line rectangles
            
            Rect prevLrect = null; // Previous line rectangle
            int prevBno = -1; // Previous block number of line
            bool code = false; // Mode indicator: outputting code
            string prevHdrString = null;
            
            foreach (var line in nlines)
            {
                Rect lrect = line.Rect;
                var spans = line.Spans;
                
                // Skip if line intersects with images
                if (!Utils.OutsideAllBboxes(lrect, parms.ImgRects))
                    continue;
                
                // Pick up tables ABOVE this text block
                var tabCandidates = parms.TabRects
                    .Where(kvp => kvp.Value.Y1 <= lrect.Y0 && !parms.WrittenTables.Contains(kvp.Key) &&
                        (lrect.X0 <= kvp.Value.X0 && kvp.Value.X0 < lrect.X1 ||
                         lrect.X0 < kvp.Value.X1 && kvp.Value.X1 <= lrect.X1 ||
                         kvp.Value.X0 <= lrect.X0 && lrect.X1 <= kvp.Value.X1))
                    .ToList();
                
                foreach (var kvp in tabCandidates)
                {
                    int i = kvp.Key;
                    outString.Append("\n" + parms.Tabs[i].ToMarkdown(clean: false) + "\n");
                    
                    if (extractWords)
                    {
                        var cells = new List<Rect>();
                        if (parms.Tabs[i].header != null && parms.Tabs[i].header.cells != null)
                        {
                            foreach (var c in parms.Tabs[i].header.cells)
                            {
                                if (c != null)
                                    cells.Add(c);
                            }
                        }
                        if (parms.Tabs[i].cells != null)
                        {
                            foreach (var c in parms.Tabs[i].cells)
                            {
                                if (c != null)
                                    cells.Add(c);
                            }
                        }
                        parms.LineRects.AddRange(cells.OrderBy(c => c.Y1).ThenBy(c => c.X0));
                    }
                    parms.WrittenTables.Add(i);
                    prevHdrString = null;
                }
                
                // Pick up images/graphics ABOVE this text block
                for (int i = 0; i < parms.ImgRects.Count; i++)
                {
                    if (parms.WrittenImages.Contains(i))
                        continue;
                    
                    Rect r = parms.ImgRects[i];
                    if (Math.Max(r.Y0, lrect.Y0) < Math.Min(r.Y1, lrect.Y1) &&
                        (lrect.X0 <= r.X0 && r.X0 < lrect.X1 ||
                         lrect.X0 < r.X1 && r.X1 <= lrect.X1 ||
                         r.X0 <= lrect.X0 && lrect.X1 <= r.X1))
                    {
                        string pathname = SaveImage(parms.Page, r, i, false, false, "", "", "", 150, 0.05f);
                        if (!string.IsNullOrEmpty(pathname))
                        {
                            outString.AppendFormat(GRAPHICS_TEXT, pathname);
                        }
                        
                        if (forceText)
                        {
                            string imgTxt = WriteText(parms, r, getHeaderId, forceText: true, 
                                ignoreCode: false, extractWords: false);
                            if (!Utils.IsWhite(imgTxt))
                            {
                                outString.Append(imgTxt);
                            }
                        }
                        parms.WrittenImages.Add(i);
                        prevHdrString = null;
                    }
                }
                
                parms.LineRects.Add(lrect);
                
                // If line rect is far away from previous one, add line break
                if (parms.LineRects.Count > 1)
                {
                    var prevRect = parms.LineRects[parms.LineRects.Count - 2];
                    if (lrect.Y1 - prevRect.Y1 > lrect.Height * 1.5f)
                    {
                        outString.Append("\n");
                    }
                }
                
                // Make text string for the full line
                string text = string.Join(" ", spans.Select(s => s.Text ?? "").Where(t => !string.IsNullOrWhiteSpace(t))).Trim();
                
                // Check formatting flags
                bool allStrikeout = spans.All(s => ((int)s.CharFlags & 1) != 0);
                bool allItalic = spans.All(s => ((int)s.Flags & 2) != 0);
                bool allBold = spans.All(s => (((int)s.Flags & 16) != 0) || (((int)s.CharFlags & 8) != 0));
                bool allMono = spans.All(s => ((int)s.Flags & 8) != 0);
                
                // Get header string
                string hdrString = MaxHeaderId(spans, parms.Page, getHeaderId);
                
                if (!string.IsNullOrEmpty(hdrString))
                {
                    // Header line
                    if (allMono)
                        text = "`" + text + "`";
                    if (allItalic)
                        text = "_" + text + "_";
                    if (allBold)
                        text = "**" + text + "**";
                    if (allStrikeout)
                        text = "~~" + text + "~~";
                    
                    if (hdrString != prevHdrString)
                    {
                        outString.Append(hdrString + text + "\n");
                    }
                    else
                    {
                        // Header text broken across multiple lines
                        while (outString.Length > 0 && outString[outString.Length - 1] == '\n')
                            outString.Length--;
                        outString.Append(" " + text + "\n");
                    }
                    prevHdrString = hdrString;
                    continue;
                }
                
                prevHdrString = hdrString;
                
                // Start or extend code block
                if (allMono && !ignoreCode)
                {
                    if (!code)
                    {
                        outString.Append("```\n");
                        code = true;
                    }
                    float delta = (lrect.X0 - clip.X0) / (spans[0].Size * 0.5f);
                    string indent = new string(' ', Math.Max(0, (int)delta));
                    outString.Append(indent + text + "\n");
                    continue;
                }
                
                if (code && !allMono)
                {
                    outString.Append("```\n");
                    code = false;
                }
                
                ExtendedSpan span0 = spans[0];
                int bno = span0.Block;
                if (bno != prevBno)
                {
                    outString.Append("\n");
                    prevBno = bno;
                }
                
                // Check if we need another line break
                if ((prevLrect != null && lrect.Y1 - prevLrect.Y1 > lrect.Height * 1.5f) ||
                    (span0.Text != null && (span0.Text.StartsWith("[") || Utils.StartswithBullet(span0.Text))) ||
                    ((int)span0.Flags & 1) != 0) // Superscript
                {
                    outString.Append("\n");
                }
                prevLrect = lrect;
                
                // Switch off code mode if not all mono
                if (code)
                {
                    outString.Append("```\n");
                    code = false;
                }
                
                // Process each span
                foreach (var s in spans)
                {
                    bool mono = ((int)s.Flags & 8) != 0;
                    bool bold = ((int)s.Flags & 16) != 0 || ((int)s.CharFlags & 8) != 0;
                    bool italic = ((int)s.Flags & 2) != 0;
                    bool strikeout = ((int)s.CharFlags & 1) != 0;
                    
                    string prefix = "";
                    string suffix = "";
                    
                    if (mono)
                    {
                        prefix = "`" + prefix;
                        suffix += "`";
                    }
                    if (bold)
                    {
                        prefix = "**" + prefix;
                        suffix += "**";
                    }
                    if (italic)
                    {
                        prefix = "_" + prefix;
                        suffix += "_";
                    }
                    if (strikeout)
                    {
                        prefix = "~~" + prefix;
                        suffix += "~~";
                    }
                    
                    // Convert intersecting link to markdown syntax
                    string ltext = ResolveLinks(parms.Links, s);
                    if (ltext != null)
                    {
                        text = hdrString + prefix + ltext + suffix + " ";
                    }
                    else
                    {
                        text = hdrString + prefix + (s.Text ?? "").Trim() + suffix + " ";
                    }
                    
                    if (Utils.StartswithBullet(text))
                    {
                        text = "- " + text.Substring(1);
                        text = text.Replace("  ", " ");
                        float dist = span0.Bbox.X0 - clip.X0;
                        float cwidth = (span0.Bbox.X1 - span0.Bbox.X0) / Math.Max(1, (span0.Text ?? "").Length);
                        if (cwidth == 0.0f)
                            cwidth = span0.Size * 0.5f;
                        int indentCount = (int)Math.Round(dist / cwidth);
                        text = new string(' ', Math.Max(0, indentCount)) + text;
                    }
                    
                    outString.Append(text);
                }
                
                if (!code)
                    outString.Append("\n");
            }
            
            outString.Append("\n");
            if (code)
            {
                outString.Append("```\n");
                code = false;
            }
            outString.Append("\n\n");
            
            string result = outString.ToString();
            result = result.Replace(" \n", "\n").Replace("  ", " ");
            while (result.Contains("\n\n\n"))
                result = result.Replace("\n\n\n", "\n\n");
            
            return result;
        }

        private static Parameters GetPageOutput(
            Document doc,
            int pno,
            List<float> margins,
            Func<ExtendedSpan, Page, string> getHeaderId,
            bool writeImages,
            bool embedImages,
            bool ignoreImages,
            string imagePath,
            string imageFormat,
            string filename,
            bool forceText,
            int dpi,
            bool ignoreCode,
            bool ignoreGraphics,
            string tableStrategy,
            bool detectBgColor,
            int? graphicsLimit,
            bool ignoreAlpha,
            bool extractWords,
            bool pageSeparators,
            float imageSizeLimit,
            int textFlags)
        {
            Page page = doc[pno];
            // Remove rotation to ensure we work on rotation=0
            page.RemoveRotation();

            // Create Parameters object to store page information
            Parameters parms = new Parameters
            {
                Page = page,
                Filename = filename,
                MdString = "",
                Images = new List<object>(),
                Tables = new List<object>(),
                Graphics = new List<object>(),
                Words = new List<object>(),
                LineRects = new List<Rect>(),
                AcceptInvisible = PageIsOcr(page) || ignoreAlpha
            };

            // Determine background color
            if (detectBgColor)
            {
                parms.BgColor = Utils.GetBgColor(page);
            }

            // Process margins
            float left = 0, top = 0, right = 0, bottom = 0;
            if (margins != null && margins.Count > 0)
            {
                if (margins.Count == 1)
                {
                    left = top = right = bottom = margins[0];
                }
                else if (margins.Count == 2)
                {
                    top = bottom = margins[0];
                    left = right = margins[1];
                }
                else if (margins.Count == 4)
                {
                    left = margins[0];
                    top = margins[1];
                    right = margins[2];
                    bottom = margins[3];
                }
            }

            // Set clip with margins: page.rect + (left, top, -right, -bottom)
            parms.Clip = new Rect(page.Rect);
            parms.Clip.X0 += left;
            parms.Clip.Y0 += top;
            parms.Clip.X1 -= right;
            parms.Clip.Y1 -= bottom;

            // Extract external links on page
            parms.Links = page.GetLinks()
                .Where(l => l.Kind == LinkType.LINK_URI && !string.IsNullOrEmpty(l.Uri))
                .ToList();

            // Extract annotation rectangles on page
            try
            {
                var annots = page.GetAnnots();
                parms.AnnotRects = annots
                    .Where(a => a.Rect != null)
                    .Select(a => a.Rect)
                    .ToList();
            }
            catch
            {
                parms.AnnotRects = new List<Rect>();
            }

            // Make a TextPage for all later extractions (textFlags passed from ToMarkdown)
            parms.TextPage = page.GetTextPage(flags: textFlags, clip: parms.Clip);

            // Extract and process tables if not ignoring graphics
            List<Table> tables = new List<Table>();
            Dictionary<int, Rect> tabRects = new Dictionary<int, Rect>();
            List<int> writtenTables = new List<int>();

            if (!ignoreGraphics && !string.IsNullOrEmpty(tableStrategy))
            {
                try
                {
                    var foundTables = page.GetTables(clip: page.Rect, strategy: tableStrategy);
                    for (int i = 0; i < foundTables.Count; i++)
                    {
                        var t = foundTables[i];
                        // Remove tables with too few rows or columns
                        if (t.row_count < 2 || t.col_count < 2)
                            continue;
                        tables.Add(t);
                        // Combine table bbox with header bbox
                        Rect tabRect = t.bbox;
                        if (t.header != null && t.header.bbox != null)
                        {
                            Rect headerRect = t.header.bbox;
                            tabRect = Utils.JoinRects(new List<Rect> { tabRect, headerRect });
                        }
                        tabRects[tables.Count - 1] = tabRect;
                    }
                    // Sort tables by position (top to bottom, left to right)
                    var sortedIndices = Enumerable.Range(0, tables.Count)
                        .OrderBy(i => tabRects[i].Y0)
                        .ThenBy(i => tabRects[i].X0)
                        .ToList();
                    var sortedTables = sortedIndices.Select(i => tables[i]).ToList();
                    var sortedRects = sortedIndices.ToDictionary(
                        idx => sortedIndices.IndexOf(idx),
                        idx => tabRects[idx]
                    );
                    tables = sortedTables;
                    tabRects = sortedRects;
                }
                catch
                {
                    // If table extraction fails, continue without tables
                }
            }


            // Extract and process images if not ignored
            List<Rect> imgRects = new List<Rect>();
            if (!ignoreImages)
            {
                try
                {
                    List<Block> imgInfo = page.GetImageInfo();

                    // Filter and process images (use clip with margins, not full page rect)
                    var validImages = imgInfo
                        .Where(img => img.Bbox != null)
                        .Select(img => new { Bbox = new Rect(img.Bbox), Block = img })
                        .Where(img =>
                            img.Bbox.Width >= imageSizeLimit * parms.Clip.Width &&
                            img.Bbox.Height >= imageSizeLimit * parms.Clip.Height &&
                            img.Bbox.Intersects(parms.Clip) &&
                            img.Bbox.Width > 3 &&
                            img.Bbox.Height > 3)
                        .OrderByDescending(img => Math.Abs(img.Bbox.Width * img.Bbox.Height))
                        .ToList();

                    // Subset of images truly inside the clip (exclude near-full-page images; then output full page image)
                    if (validImages.Count > 0)
                    {
                        float imgMaxSize = parms.Clip.Width * parms.Clip.Height * 0.9f;
                        var sane = validImages
                            .Where(img =>
                            {
                                Rect inter = Utils.IntersectRects(img.Bbox, parms.Clip);
                                return inter.Width * inter.Height < imgMaxSize;
                            })
                            .ToList();
                        if (sane.Count < validImages.Count)
                        {
                            validImages = sane;
                            string pathname = SaveImage(parms.Page, parms.Clip, -1, writeImages, embedImages,
                                imagePath, imageFormat, filename, dpi, imageSizeLimit);
                            if (!string.IsNullOrEmpty(pathname))
                                parms.MdString += string.Format(GRAPHICS_TEXT, pathname);
                        }
                    }

                    validImages = validImages.Take(30).ToList(); // Only accept the largest up to 30 images

                    // Remove images contained in larger images (run from back to front = small to large)
                    for (int i = validImages.Count - 1; i >= 0; i--)
                    {
                        Rect r = validImages[i].Bbox;
                        if (r.IsEmpty)
                        {
                            validImages.RemoveAt(i);
                            continue;
                        }
                        for (int j = 0; j < i; j++)
                        {
                            if (Utils.BboxInBbox(r, validImages[j].Bbox))
                            {
                                validImages.RemoveAt(i);
                                break;
                            }
                        }
                    }

                    parms.ImgRects = validImages.Select(img => img.Bbox).ToList();
                    parms.Images = validImages.Select(img => (object)img.Block).ToList();
                }
                catch
                {
                    // If image extraction fails, continue without images
                }
            }
            else
            {
                parms.ImgRects = new List<Rect>();
            }

            // Store tables in parms
            parms.Tabs = tables;
            parms.TabRects = tabRects;
            parms.WrittenTables = writtenTables;
            parms.TabRects0 = tabRects.Values.ToList();

            // Check graphics limit and set too_many_graphics flag
            bool tooManyGraphics = false;
            int graphicsCount = 0;
            if (!ignoreGraphics && graphicsLimit.HasValue)
            {
                try
                {
                    var bboxLog = page.GetBboxlog();
                    graphicsCount = bboxLog.Count(b => b.Type != null && b.Type.Contains("path"));
                    if (graphicsCount > graphicsLimit.Value)
                    {
                        ignoreGraphics = true;
                        tooManyGraphics = true;
                    }
                }
                catch
                {
                    // If bboxlog extraction fails, continue
                }
            }

            // Get paths for graphics and multi-column detection
            List<PathInfo> paths = new List<PathInfo>();
            List<Rect> vgClusters0 = new List<Rect>();
            
            if (!ignoreGraphics)
            {
                try
                {
                    paths = page.GetDrawings()
                        .Where(p => p.Rect != null && 
                               Utils.BboxInBbox(p.Rect, parms.Clip) &&
                               p.Rect.Width < parms.Clip.Width && 
                               p.Rect.Height < parms.Clip.Height &&
                               (p.Rect.Width > 3 || p.Rect.Height > 3) &&
                               !(p.Type == "f" && p.Fill != null && parms.BgColor != null && 
                                 p.Fill.Length >= 3 && parms.BgColor.Length >= 3 &&
                                 Math.Abs(p.Fill[0] - parms.BgColor[0]) < 0.01f &&
                                 Math.Abs(p.Fill[1] - parms.BgColor[1]) < 0.01f &&
                                 Math.Abs(p.Fill[2] - parms.BgColor[2]) < 0.01f) &&
                               Utils.OutsideAllBboxes(p.Rect, parms.TabRects0) &&
                               Utils.OutsideAllBboxes(p.Rect, parms.AnnotRects))
                        .ToList();

                    // Cluster drawings
                    if (paths.Count > 0)
                    {
                        var clusters = page.ClusterDrawings(clip: parms.Clip, drawings: paths);
                        foreach (var bbox in clusters)
                        {
                            if (Utils.IsSignificant(bbox, paths))
                            {
                                vgClusters0.Add(bbox);
                            }
                        }

                        // Get paths that are in significant graphics
                        parms.ActualPaths = paths
                            .Where(p => Utils.BboxInAnyBbox(p.Rect, vgClusters0))
                            .ToList();
                    }
                }
                catch
                {
                    paths = new List<PathInfo>();
                }
            }

            // Also add image rectangles to the list and vice versa
            vgClusters0.AddRange(parms.ImgRects);
            parms.ImgRects.AddRange(vgClusters0);
            parms.ImgRects = parms.ImgRects
                .Distinct()
                .OrderBy(r => r.Y1)
                .ThenBy(r => r.X0)
                .ToList();
            parms.WrittenImages = new List<int>();

            // Refine graphics clusters
            parms.VgClusters0 = Utils.RefineBoxes(vgClusters0);
            parms.VgClusters = parms.VgClusters0
                .Select((r, i) => new { Index = i, Rect = r })
                .ToDictionary(x => x.Index, x => x.Rect);

            // Calculate character density for text rectangle determination
            int blockCount = parms.TextPage.ExtractBlocks().Count;
            float charDensity = blockCount > 0 
                ? parms.TextPage.ExtractText().Length / (float)blockCount 
                : 0;

            // Use multi-column detection to get text rectangles
            List<Rect> textRects;
            if (tooManyGraphics && charDensity < 20)
            {
                // This page has too many isolated text pieces for meaningful layout analysis
                textRects = new List<Rect> { parms.Clip };
            }
            else
            {
                try
                {
                    textRects = MultiColumn.ColumnBoxes(
                        page,
                        footerMargin: bottom,
                        headerMargin: top,
                        noImageText: !forceText,
                        textpage: parms.TextPage,
                        paths: parms.ActualPaths,
                        avoid: parms.TabRects0.Concat(parms.VgClusters0).ToList(),
                        ignoreImages: ignoreImages);
                    
                    // If no columns detected, use the full clip
                    if (textRects == null || textRects.Count == 0)
                    {
                        textRects = new List<Rect> { parms.Clip };
                    }
                }
                catch
                {
                    // Fallback to full page if column detection fails
                    textRects = new List<Rect> { parms.Clip };
                }
            }

            // Process each text rectangle
            StringBuilder mdOutput = new StringBuilder();
            foreach (Rect textRect in textRects)
            {
                // Output tables above this text rectangle
                mdOutput.Append(OutputTables(parms, textRect, extractWords));
                
                // Output images above this text rectangle
                mdOutput.Append(OutputImages(parms, textRect, forceText, writeImages, embedImages, 
                    imagePath, imageFormat, filename, dpi, imageSizeLimit, getHeaderId));
                
                // Output text inside this rectangle
                mdOutput.Append(WriteText(parms, textRect, getHeaderId, forceText, ignoreCode, extractWords));
            }
            
            // Write any remaining tables and images
            mdOutput.Append(OutputTables(parms, null, extractWords));
            mdOutput.Append(OutputImages(parms, null, forceText, writeImages, embedImages, 
                imagePath, imageFormat, filename, dpi, imageSizeLimit, getHeaderId));
            
            // Clean up the output
            parms.MdString = mdOutput.ToString();
            parms.MdString = parms.MdString.Replace(" ,", ",").Replace("-\n", "");
            
            while (parms.MdString.StartsWith("\n"))
            {
                parms.MdString = parms.MdString.Substring(1);
            }
            
            parms.MdString = parms.MdString.Replace('\0', Utils.REPLACEMENT_CHARACTER);
            
            // Handle extract_words mode
            if (extractWords)
            {
                var rawWords = parms.TextPage.ExtractWords();
                rawWords = rawWords.OrderBy(w => w.Y1).ThenBy(w => w.X0).ToList();
                
                List<WordBlock> words = new List<WordBlock>();
                foreach (var lrect in parms.LineRects)
                {
                    var lwords = rawWords
                        .Where(w => 
                        {
                            var wrect = new Rect(w.X0, w.Y0, w.X1, w.Y1);
                            return Utils.BboxInBbox(wrect, lrect);
                        })
                        .ToList();
                    words.AddRange(SortWords(lwords));
                }
                
                // Remove duplicates
                List<WordBlock> nwords = new List<WordBlock>();
                foreach (var w in words)
                {
                    if (!nwords.Any(nw => nw.X0 == w.X0 && nw.Y0 == w.Y0 && nw.X1 == w.X1 && nw.Y1 == w.Y1 && nw.Text == w.Text))
                    {
                        nwords.Add(w);
                    }
                }
                parms.Words = nwords.Cast<object>().ToList();
            }
            else
            {
                parms.Words = new List<object>();
            }
            
            // Add page separators
            if (pageSeparators)
            {
                parms.MdString += $"\n\n--- end of page={page.Number} ---\n\n";
            }
            
            return parms;
        }
    }
}
