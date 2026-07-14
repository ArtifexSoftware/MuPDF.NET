using System;
using System.Collections.Generic;
using MuPDF.NET;
using PDF4LLM.Helpers;
using PDF4LLM.Layout;
using PDF4LLM.Llama;

namespace PDF4LLM
{
    /// <summary>PDF extraction and layout-to-markdown API.</summary>
    public static partial class PdfExtractor
    {
        static PdfExtractor()
        {
            string bind = MuPDF.NET.Utils.VersionBind.Split('-')[0];
            string required = VersionInfo.RequiredPyMuPDF.Split('-')[0];
            if (!string.Equals(bind, required, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"PDF4LLM {VersionInfo.Version} requires PyMuPDF {VersionInfo.RequiredPyMuPDF}, " +
                    $"but MuPDF.NET reports VersionBind {MuPDF.NET.Utils.VersionBind}.");
            }

            // Always attempt to use Layout by default.
            if (PyMuPdfLayout.IsAvailable)
                SetUseLayout(true);
            else
                SetUseLayout(false);
        }

        /// <summary>Package version string.</summary>
        public static string Version => VersionInfo.Version;

        /// <summary>Semantic version as (major, minor, patch).</summary>
        public static (int major, int minor, int patch) VersionTuple
        {
            get
            {
                string[] parts = Version.Split('-')[0].Split('.');
                return (
                    int.Parse(parts[0]),
                    parts.Length > 1 ? int.Parse(parts[1]) : 0,
                    parts.Length > 2 ? int.Parse(parts[2]) : 0);
            }
        }

        /// <summary>When true, export methods use the layout pipeline.</summary>
        public static bool UseLayout { get; set; }

        /// <summary>Returns a LlamaIndex-compatible PDF markdown reader.</summary>
        /// <param name="metaFilter">Optional callback to transform per-page metadata before documents are returned.</param>
        public static PDFMarkdownReader LlamaMarkdownReader(
            Func<Dictionary<string, object>, Dictionary<string, object>> metaFilter = null)
        {
            return new PDFMarkdownReader(metaFilter);
        }

        /// <summary>Convert a document to Markdown.</summary>
        /// <param name="doc">PDF document to convert (required for in-memory or non-path sources).</param>
        /// <param name="header">When <see langword="true"/>, include page-header regions in the output.</param>
        /// <param name="footer">When <see langword="true"/>, include page-footer regions in the output.</param>
        /// <param name="pages">0-based page indices to process; <see langword="null"/> processes all pages.</param>
        /// <param name="writeImages">When <see langword="true"/>, write image files and emit Markdown image references.</param>
        /// <param name="embedImages">When <see langword="true"/>, embed images as base64 in the Markdown (mutually exclusive with <paramref name="writeImages"/>).</param>
        /// <param name="imagePath">Folder for written images when <paramref name="writeImages"/> is <see langword="true"/>.</param>
        /// <param name="imageFormat">Image file extension/format (for example <c>png</c> or <c>jpg</c>).</param>
        /// <param name="filename">Base file name used for image output when <paramref name="doc"/> has no path.</param>
        /// <param name="forceText">When <see langword="true"/>, extract text even from picture regions (after image references).</param>
        /// <param name="pageChunks">When <see langword="true"/>, return JSON page-chunk structures instead of one Markdown string (layout mode).</param>
        /// <param name="pageSeparators">When <see langword="true"/>, insert debug separators between pages.</param>
        /// <param name="dpi">Resolution in dots per inch for extracted images.</param>
        /// <param name="ocrDpi">Resolution in dots per inch for OCR page rendering (layout mode).</param>
        /// <param name="pageWidth">Virtual page width for reflowable documents (legacy mode only).</param>
        /// <param name="pageHeight">Virtual page height for reflowable documents; <see langword="null"/> means unlimited height (legacy mode only).</param>
        /// <param name="ignoreCode">When <see langword="true"/>, do not apply monospace/code-block formatting.</param>
        /// <param name="showProgress">When <see langword="true"/>, print a progress indicator while converting.</param>
        /// <param name="useOcr">When <see langword="true"/>, apply OCR where the pipeline decides it is beneficial (layout mode).</param>
        /// <param name="ocrLanguage">Tesseract language code(s), for example <c>eng</c> or <c>eng+deu</c> (layout mode).</param>
        /// <param name="forceOcr">When <see langword="true"/>, OCR every page regardless of heuristics (layout mode).</param>
        /// <param name="ocrFunction">Custom per-page OCR callback; <see langword="null"/> uses the built-in engine (layout mode).</param>
        public static string ToMarkdown(
            Document doc,
            bool header = true,
            bool footer = true,
            List<int> pages = null,
            bool writeImages = false,
            bool embedImages = false,
            string imagePath = "",
            string imageFormat = "png",
            string filename = "",
            bool forceText = true,
            bool pageChunks = false,
            bool pageSeparators = false,
            int dpi = 150,
            int ocrDpi = 300,
            float pageWidth = 612,
            float? pageHeight = null,
            bool ignoreCode = false,
            bool showProgress = false,
            bool useOcr = true,
            string ocrLanguage = "eng",
            bool forceOcr = false,
            OcrPageFunction ocrFunction = null)
        {
            if (writeImages && embedImages)
                throw new ArgumentException("Cannot both write_images and embed_images");

            if (!UseLayout)
            {
                return MuPdfRag.ToMarkdown(
                    doc,
                    pages: pages,
                    hdrInfo: null,
                    writeImages: writeImages,
                    embedImages: embedImages,
                    ignoreImages: false,
                    ignoreGraphics: false,
                    detectBgColor: true,
                    imagePath: imagePath,
                    imageFormat: imageFormat,
                    imageSizeLimit: 0.05f,
                    filename: string.IsNullOrEmpty(filename) ? doc.Name : filename,
                    forceText: forceText,
                    pageChunks: pageChunks,
                    pageSeparators: pageSeparators,
                    margins: null,
                    dpi: dpi,
                    pageWidth: pageWidth,
                    pageHeight: pageHeight,
                    tableStrategy: "lines_strict",
                    graphicsLimit: null,
                    fontsizeLimit: 3.0f,
                    ignoreCode: ignoreCode,
                    extractWords: false,
                    showProgress: showProgress,
                    useGlyphs: false,
                    ignoreAlpha: false);
            }

            var parsedDoc = DocumentLayout.ParseDocument(
                doc,
                filename: filename,
                imageDpi: dpi,
                imageFormat: imageFormat,
                imagePath: imagePath,
                pages: pages,
                ocrDpi: ocrDpi,
                writeImages: writeImages,
                embedImages: embedImages,
                showProgress: showProgress,
                forceText: forceText,
                useOcr: useOcr,
                ocrLanguage: ocrLanguage,
                forceOcr: forceOcr,
                ocrFunction: ocrFunction);

            return parsedDoc.ToMarkdown(
                header: header,
                footer: footer,
                writeImages: writeImages,
                embedImages: embedImages,
                ignoreCode: ignoreCode,
                showProgress: showProgress,
                pageSeparators: pageSeparators,
                pageChunks: pageChunks);
        }

        /// <summary>Convert a PDF file path to Markdown.</summary>
        /// <param name="path">Path to the PDF file.</param>
        /// <param name="header">When <see langword="true"/>, include page-header regions in the output.</param>
        /// <param name="footer">When <see langword="true"/>, include page-footer regions in the output.</param>
        /// <param name="pages">0-based page indices to process; <see langword="null"/> processes all pages.</param>
        /// <param name="writeImages">When <see langword="true"/>, write image files and emit Markdown image references.</param>
        /// <param name="embedImages">When <see langword="true"/>, embed images as base64 in the Markdown (mutually exclusive with <paramref name="writeImages"/>).</param>
        /// <param name="imagePath">Folder for written images when <paramref name="writeImages"/> is <see langword="true"/>.</param>
        /// <param name="imageFormat">Image file extension/format (for example <c>png</c> or <c>jpg</c>).</param>
        /// <param name="filename">Base file name used for image output.</param>
        /// <param name="forceText">When <see langword="true"/>, extract text even from picture regions (after image references).</param>
        /// <param name="pageChunks">When <see langword="true"/>, return JSON page-chunk structures instead of one Markdown string (layout mode).</param>
        /// <param name="pageSeparators">When <see langword="true"/>, insert debug separators between pages.</param>
        /// <param name="dpi">Resolution in dots per inch for extracted images.</param>
        /// <param name="ocrDpi">Resolution in dots per inch for OCR page rendering (layout mode).</param>
        /// <param name="pageWidth">Virtual page width for reflowable documents (legacy mode only).</param>
        /// <param name="pageHeight">Virtual page height for reflowable documents; <see langword="null"/> means unlimited height (legacy mode only).</param>
        /// <param name="ignoreCode">When <see langword="true"/>, do not apply monospace/code-block formatting.</param>
        /// <param name="showProgress">When <see langword="true"/>, print a progress indicator while converting.</param>
        /// <param name="useOcr">When <see langword="true"/>, apply OCR where the pipeline decides it is beneficial (layout mode).</param>
        /// <param name="ocrLanguage">Tesseract language code(s), for example <c>eng</c> or <c>eng+deu</c> (layout mode).</param>
        /// <param name="forceOcr">When <see langword="true"/>, OCR every page regardless of heuristics (layout mode).</param>
        /// <param name="ocrFunction">Custom per-page OCR callback; <see langword="null"/> uses the built-in engine (layout mode).</param>
        public static string ToMarkdown(
            string path,
            bool header = true,
            bool footer = true,
            List<int> pages = null,
            bool writeImages = false,
            bool embedImages = false,
            string imagePath = "",
            string imageFormat = "png",
            string filename = "",
            bool forceText = true,
            bool pageChunks = false,
            bool pageSeparators = false,
            int dpi = 150,
            int ocrDpi = 300,
            float pageWidth = 612,
            float? pageHeight = null,
            bool ignoreCode = false,
            bool showProgress = false,
            bool useOcr = true,
            string ocrLanguage = "eng",
            bool forceOcr = false,
            OcrPageFunction ocrFunction = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be null or whitespace.", nameof(path));
            using (var doc = new Document(path))
                return ToMarkdown(
                    doc,
                    header,
                    footer,
                    pages,
                    writeImages,
                    embedImages,
                    imagePath,
                    imageFormat,
                    filename,
                    forceText,
                    pageChunks,
                    pageSeparators,
                    dpi,
                    ocrDpi,
                    pageWidth,
                    pageHeight,
                    ignoreCode,
                    showProgress,
                    useOcr,
                    ocrLanguage,
                    forceOcr,
                    ocrFunction);
        }

        /// <summary>Convert a document to layout JSON.</summary>
        /// <param name="doc">PDF document to convert.</param>
        /// <param name="imageDpi">Resolution in dots per inch for extracted images.</param>
        /// <param name="imageFormat">Image file extension/format (for example <c>png</c>).</param>
        /// <param name="imagePath">Folder for written images when <paramref name="writeImages"/> is <see langword="true"/>.</param>
        /// <param name="pages">0-based page indices to process; <see langword="null"/> processes all pages.</param>
        /// <param name="ocrDpi">Resolution in dots per inch for OCR page rendering.</param>
        /// <param name="writeImages">When <see langword="true"/>, write image files during parsing.</param>
        /// <param name="embedImages">When <see langword="true"/>, embed images as base64 in the JSON model.</param>
        /// <param name="showProgress">When <see langword="true"/>, print a progress indicator while converting.</param>
        /// <param name="forceText">When <see langword="true"/>, extract text even from picture regions.</param>
        /// <param name="useOcr">When <see langword="true"/>, apply OCR where the pipeline decides it is beneficial.</param>
        /// <param name="ocrLanguage">Tesseract language code(s), for example <c>eng</c> or <c>eng+deu</c>.</param>
        /// <param name="forceOcr">When <see langword="true"/>, OCR every page regardless of heuristics.</param>
        /// <param name="ocrFunction">Custom per-page OCR callback; <see langword="null"/> uses the built-in engine.</param>
        public static string ToJson(
            Document doc,
            int imageDpi = 150,
            string imageFormat = "png",
            string imagePath = "",
            List<int> pages = null,
            int ocrDpi = 300,
            bool writeImages = false,
            bool embedImages = false,
            bool showProgress = false,
            bool forceText = true,
            bool useOcr = true,
            string ocrLanguage = "eng",
            bool forceOcr = false,
            OcrPageFunction ocrFunction = null)
        {
            if (!UseLayout)
                throw new NotSupportedException(
                    "ToJson requires UseLayout=true.");

            var parsedDoc = DocumentLayout.ParseDocument(
                doc,
                filename: doc.Name,
                imageDpi: imageDpi,
                ocrDpi: ocrDpi,
                imageFormat: imageFormat,
                imagePath: imagePath,
                pages: pages,
                showProgress: showProgress,
                embedImages: embedImages,
                writeImages: writeImages,
                forceText: forceText,
                useOcr: useOcr,
                ocrLanguage: ocrLanguage,
                forceOcr: forceOcr,
                ocrFunction: ocrFunction);

            return parsedDoc.ToJson(showProgress: showProgress);
        }

        /// <summary>Convert a PDF file path to layout JSON.</summary>
        /// <param name="path">Path to the PDF file.</param>
        /// <param name="imageDpi">Resolution in dots per inch for extracted images.</param>
        /// <param name="imageFormat">Image file extension/format (for example <c>png</c>).</param>
        /// <param name="imagePath">Folder for written images when <paramref name="writeImages"/> is <see langword="true"/>.</param>
        /// <param name="pages">0-based page indices to process; <see langword="null"/> processes all pages.</param>
        /// <param name="ocrDpi">Resolution in dots per inch for OCR page rendering.</param>
        /// <param name="writeImages">When <see langword="true"/>, write image files during parsing.</param>
        /// <param name="embedImages">When <see langword="true"/>, embed images as base64 in the JSON model.</param>
        /// <param name="showProgress">When <see langword="true"/>, print a progress indicator while converting.</param>
        /// <param name="forceText">When <see langword="true"/>, extract text even from picture regions.</param>
        /// <param name="useOcr">When <see langword="true"/>, apply OCR where the pipeline decides it is beneficial.</param>
        /// <param name="ocrLanguage">Tesseract language code(s), for example <c>eng</c> or <c>eng+deu</c>.</param>
        /// <param name="forceOcr">When <see langword="true"/>, OCR every page regardless of heuristics.</param>
        /// <param name="ocrFunction">Custom per-page OCR callback; <see langword="null"/> uses the built-in engine.</param>
        public static string ToJson(
            string path,
            int imageDpi = 150,
            string imageFormat = "png",
            string imagePath = "",
            List<int> pages = null,
            int ocrDpi = 300,
            bool writeImages = false,
            bool embedImages = false,
            bool showProgress = false,
            bool forceText = true,
            bool useOcr = true,
            string ocrLanguage = "eng",
            bool forceOcr = false,
            OcrPageFunction ocrFunction = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be null or whitespace.", nameof(path));
            using (var doc = new Document(path))
                return ToJson(
                    doc,
                    imageDpi,
                    imageFormat,
                    imagePath,
                    pages,
                    ocrDpi,
                    writeImages,
                    embedImages,
                    showProgress,
                    forceText,
                    useOcr,
                    ocrLanguage,
                    forceOcr,
                    ocrFunction);
        }

        /// <summary>Convert a document to plain text.</summary>
        /// <param name="doc">PDF document to convert.</param>
        /// <param name="filename">Logical file name stored in the parsed model metadata.</param>
        /// <param name="header">When <see langword="true"/>, include page-header regions in the output.</param>
        /// <param name="footer">When <see langword="true"/>, include page-footer regions in the output.</param>
        /// <param name="pages">0-based page indices to process; <see langword="null"/> processes all pages.</param>
        /// <param name="ignoreCode">When <see langword="true"/>, do not apply monospace/code-block formatting.</param>
        /// <param name="showProgress">When <see langword="true"/>, print a progress indicator while converting.</param>
        /// <param name="forceText">When <see langword="true"/>, extract text even from picture regions.</param>
        /// <param name="ocrDpi">Resolution in dots per inch for OCR page rendering.</param>
        /// <param name="useOcr">When <see langword="true"/>, apply OCR where the pipeline decides it is beneficial.</param>
        /// <param name="ocrLanguage">Tesseract language code(s), for example <c>eng</c> or <c>eng+deu</c>.</param>
        /// <param name="tableFormat">Table rendering style (for example <c>grid</c>).</param>
        /// <param name="pageChunks">When <see langword="true"/>, return JSON page-chunk structures instead of one text string.</param>
        /// <param name="tableMaxWidth">Maximum table width in characters for plain-text tables.</param>
        /// <param name="tableMinColWidth">Minimum column width in characters for plain-text tables.</param>
        /// <param name="forceOcr">When <see langword="true"/>, OCR every page regardless of heuristics.</param>
        /// <param name="ocrFunction">Custom per-page OCR callback; <see langword="null"/> uses the built-in engine.</param>
        public static string ToText(
            Document doc,
            string filename = "",
            bool header = true,
            bool footer = true,
            List<int> pages = null,
            bool ignoreCode = false,
            bool showProgress = false,
            bool forceText = true,
            int ocrDpi = 300,
            bool useOcr = true,
            string ocrLanguage = "eng",
            string tableFormat = "grid",
            bool pageChunks = false,
            int tableMaxWidth = 100,
            int tableMinColWidth = 10,
            bool forceOcr = false,
            OcrPageFunction ocrFunction = null)
        {
            if (!UseLayout)
                throw new NotSupportedException(
                    "ToText requires UseLayout=true.");

            var parsedDoc = DocumentLayout.ParseDocument(
                doc,
                filename: filename,
                pages: pages,
                ocrDpi: ocrDpi,
                embedImages: false,
                writeImages: false,
                showProgress: showProgress,
                forceText: forceText,
                useOcr: useOcr,
                ocrLanguage: ocrLanguage,
                forceOcr: forceOcr,
                ocrFunction: ocrFunction);

            return parsedDoc.ToText(
                header: header,
                footer: footer,
                ignoreCode: ignoreCode,
                showProgress: showProgress,
                tableFormat: tableFormat,
                pageChunks: pageChunks,
                tableMaxWidth: tableMaxWidth,
                tableMinColWidth: tableMinColWidth);
        }

        /// <summary>Convert a PDF file path to plain text.</summary>
        /// <param name="path">Path to the PDF file.</param>
        /// <param name="filename">Logical file name stored in the parsed model metadata.</param>
        /// <param name="header">When <see langword="true"/>, include page-header regions in the output.</param>
        /// <param name="footer">When <see langword="true"/>, include page-footer regions in the output.</param>
        /// <param name="pages">0-based page indices to process; <see langword="null"/> processes all pages.</param>
        /// <param name="ignoreCode">When <see langword="true"/>, do not apply monospace/code-block formatting.</param>
        /// <param name="showProgress">When <see langword="true"/>, print a progress indicator while converting.</param>
        /// <param name="forceText">When <see langword="true"/>, extract text even from picture regions.</param>
        /// <param name="ocrDpi">Resolution in dots per inch for OCR page rendering.</param>
        /// <param name="useOcr">When <see langword="true"/>, apply OCR where the pipeline decides it is beneficial.</param>
        /// <param name="ocrLanguage">Tesseract language code(s), for example <c>eng</c> or <c>eng+deu</c>.</param>
        /// <param name="tableFormat">Table rendering style (for example <c>grid</c>).</param>
        /// <param name="pageChunks">When <see langword="true"/>, return JSON page-chunk structures instead of one text string.</param>
        /// <param name="tableMaxWidth">Maximum table width in characters for plain-text tables.</param>
        /// <param name="tableMinColWidth">Minimum column width in characters for plain-text tables.</param>
        /// <param name="forceOcr">When <see langword="true"/>, OCR every page regardless of heuristics.</param>
        /// <param name="ocrFunction">Custom per-page OCR callback; <see langword="null"/> uses the built-in engine.</param>
        public static string ToText(
            string path,
            string filename = "",
            bool header = true,
            bool footer = true,
            List<int> pages = null,
            bool ignoreCode = false,
            bool showProgress = false,
            bool forceText = true,
            int ocrDpi = 300,
            bool useOcr = true,
            string ocrLanguage = "eng",
            string tableFormat = "grid",
            bool pageChunks = false,
            int tableMaxWidth = 100,
            int tableMinColWidth = 10,
            bool forceOcr = false,
            OcrPageFunction ocrFunction = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be null or whitespace.", nameof(path));
            using (var doc = new Document(path))
                return ToText(
                    doc,
                    filename,
                    header,
                    footer,
                    pages,
                    ignoreCode,
                    showProgress,
                    forceText,
                    ocrDpi,
                    useOcr,
                    ocrLanguage,
                    tableFormat,
                    pageChunks,
                    tableMaxWidth,
                    tableMinColWidth,
                    forceOcr,
                    ocrFunction);
        }

        /// <summary>Parse a document into a structured layout model.</summary>
        /// <param name="doc">PDF document to parse.</param>
        /// <param name="filename">Logical file name stored in the parsed model metadata.</param>
        /// <param name="imageDpi">Resolution in dots per inch for extracted images.</param>
        /// <param name="imageFormat">Image file extension/format (for example <c>png</c>).</param>
        /// <param name="imagePath">Folder for written images when <paramref name="writeImages"/> is <see langword="true"/>.</param>
        /// <param name="ocrDpi">Resolution in dots per inch for OCR page rendering.</param>
        /// <param name="pages">0-based page indices to process; <see langword="null"/> processes all pages.</param>
        /// <param name="writeImages">When <see langword="true"/>, write image files during parsing.</param>
        /// <param name="embedImages">When <see langword="true"/>, embed images as base64 in the parsed model.</param>
        /// <param name="showProgress">When <see langword="true"/>, print a progress indicator while parsing.</param>
        /// <param name="forceText">When <see langword="true"/>, extract text even from picture regions.</param>
        /// <param name="useOcr">When <see langword="true"/>, apply OCR where the pipeline decides it is beneficial.</param>
        /// <param name="ocrLanguage">Tesseract language code(s), for example <c>eng</c> or <c>eng+deu</c>.</param>
        /// <param name="forceOcr">When <see langword="true"/>, OCR every page regardless of heuristics.</param>
        /// <param name="keepOcrText">When <see langword="true"/>, retain OCR-generated text spans on the page.</param>
        /// <param name="ocrFunction">Custom per-page OCR callback; <see langword="null"/> uses the built-in engine.</param>
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
            bool forceText = true,
            bool useOcr = true,
            string ocrLanguage = "eng",
            bool forceOcr = false,
            bool keepOcrText = false,
            OcrPageFunction ocrFunction = null)
        {
            if (!UseLayout)
                throw new NotSupportedException(
                    "ParseDocument requires UseLayout=true.");

            return DocumentLayout.ParseDocument(
                doc,
                filename: filename,
                imageDpi: imageDpi,
                imageFormat: imageFormat,
                imagePath: imagePath,
                ocrDpi: ocrDpi,
                pages: pages,
                writeImages: writeImages,
                embedImages: embedImages,
                showProgress: showProgress,
                forceText: forceText,
                useOcr: useOcr,
                ocrLanguage: ocrLanguage,
                forceOcr: forceOcr,
                keepOcrText: keepOcrText,
                ocrFunction: ocrFunction);
        }

        /// <summary>
        /// Extract form fields and their values from a PDF document.
        /// </summary>
        /// <param name="doc">The document to read.</param>
        /// <param name="includeXrefs">
        /// When true, include xref numbers of form fields (useful with <see cref="Page.LoadWidget"/>).
        /// </param>
        public static Dictionary<string, Dictionary<string, object>> GetKeyValues(
            Document doc,
            bool includeXrefs = false)
        {
            if (doc.IsFormPDF != 0)
                return Helpers.Utils.ExtractFormFieldsWithPages(doc, includeXrefs);
            return new Dictionary<string, Dictionary<string, object>>();
        }

        /// <summary>Extract form fields from a PDF file path.</summary>
        /// <param name="path">Path to the PDF file.</param>
        /// <param name="includeXrefs">When <see langword="true"/>, include widget xref numbers in the result.</param>
        public static Dictionary<string, Dictionary<string, object>> GetKeyValues(string path, bool includeXrefs = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be null or whitespace.", nameof(path));
            using (var doc = new Document(path))
                return GetKeyValues(doc, includeXrefs);
        }

        /// <summary>Extract form fields; logs ignored optional argument names.</summary>
        /// <param name="doc">PDF document containing interactive form fields.</param>
        /// <param name="includeXrefs">When <see langword="true"/>, include widget xref numbers in the result.</param>
        /// <param name="ignoredKeywordArgumentNames">Names of unsupported keyword arguments (logged as a warning).</param>
        public static Dictionary<string, Dictionary<string, object>> GetKeyValues(
            Document doc,
            bool includeXrefs,
            IReadOnlyCollection<string> ignoredKeywordArgumentNames)
        {
            if (ignoredKeywordArgumentNames != null && ignoredKeywordArgumentNames.Count > 0)
                Console.WriteLine($"Warning: keyword arguments ignored: {{{string.Join(", ", ignoredKeywordArgumentNames)}}}");
            return GetKeyValues(doc, includeXrefs);
        }
    }
}
