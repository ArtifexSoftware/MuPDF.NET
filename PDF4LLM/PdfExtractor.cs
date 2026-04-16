using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;
using PDF4LLM.Helpers;
using PDF4LLM.Llama;

namespace PDF4LLM
{
    /// <summary>
    /// Main entry point for PDF4LLM, aligned with pdf4llm package API (to_markdown / to_json / to_text / parse_document / get_key_values / use_layout).
    /// </summary>
    public static class PdfExtractor
    {
        /// <summary>Package version string (Python: <c>__version__</c> / <c>version</c>).</summary>
        public static string Version => VersionInfo.Version;

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

        /// <summary>
        /// When <c>true</c> (default), <see cref="ToMarkdown"/>, <see cref="ToJson"/>, <see cref="ToText"/> use the layout pipeline
        /// (<see cref="DocumentLayout.ParseDocument"/>). When <c>false</c>, <see cref="ToMarkdown"/> uses the legacy
        /// <see cref="MuPdfRag.ToMarkdown"/> path (Python: <c>pdf4llm.helpers.MuPdf_rag</c>).
        /// <see cref="ToJson"/> and <see cref="ToText"/> require layout mode in this port.
        /// </summary>
        public static bool UseLayout { get; set; } = true;

        /// <summary>Python-compatible: <c>pdf4llm.use_layout(yes)</c> (sets <see cref="UseLayout"/>).</summary>
        public static void SetUseLayout(bool yes) => UseLayout = yes;

        /// <summary>
        /// LlamaIndex reader (Python: <c>LlamaMarkdownReader</c> / <c>pdf_markdown_reader.PDFMarkdownReader</c>).
        /// </summary>
        public static PDFMarkdownReader LlamaMarkdownReader(
            Func<Dictionary<string, object>, Dictionary<string, object>> metaFilter = null)
        {
            return new PDFMarkdownReader(metaFilter);
        }

        /// <summary>Convert document to Markdown (Python: <c>to_markdown</c>).</summary>
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

        /// <summary>Layout JSON export (Python: <c>to_json</c> with <c>_use_layout</c>).</summary>
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
                    "PdfExtractor.ToJson with UseLayout=false is not supported; the legacy MuPdf_rag path only implements ToMarkdown in this port. Set PdfExtractor.UseLayout = true.");

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

        /// <summary>Plain text export (Python: <c>to_text</c> with layout).</summary>
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
                    "PdfExtractor.ToText with UseLayout=false is not supported; set PdfExtractor.UseLayout = true.");

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

        /// <summary>Python: <c>parse_document</c>.</summary>
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
                    "PdfExtractor.ParseDocument requires UseLayout=true (legacy rag path has no ParsedDocument).");

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

        /// <summary>Python: <c>get_key_values</c> for form PDFs.</summary>
        public static Dictionary<string, Dictionary<string, object>> GetKeyValues(
            Document doc,
            bool xrefs = false)
        {
            if (doc.IsFormPDF != 0)
                return Helpers.Utils.ExtractFormFieldsWithPages(doc, xrefs);
            return new Dictionary<string, Dictionary<string, object>>();
        }

        /// <summary>
        /// Same as <see cref="GetKeyValues(Document,bool)"/> but logs ignored keyword names like Python <c>get_key_values(..., **kwargs)</c>.
        /// </summary>
        public static Dictionary<string, Dictionary<string, object>> GetKeyValues(
            Document doc,
            bool xrefs,
            IReadOnlyCollection<string> ignoredKeywordArgumentNames)
        {
            if (ignoredKeywordArgumentNames != null && ignoredKeywordArgumentNames.Count > 0)
                Console.WriteLine($"Warning: keyword arguments ignored: {{{string.Join(", ", ignoredKeywordArgumentNames)}}}");
            return GetKeyValues(doc, xrefs);
        }
    }
}
