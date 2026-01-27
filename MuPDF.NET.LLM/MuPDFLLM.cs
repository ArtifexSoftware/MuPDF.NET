using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;
using MuPDF.NET.LLM.Helpers;
using MuPDF.NET.LLM.Llama;

namespace MuPDF.NET.LLM
{
    /// <summary>
    /// Main entry point for MuPDF.NET.LLM functionality.
    /// Provides a C# facade over the helpers ported from the Python pymupdf4llm package.
    /// </summary>
    public static class MuPDFLLM
    {
        public static string Version => VersionInfo.Version;

        public static (int major, int minor, int patch) VersionTuple
        {
            get
            {
                string[] parts = Version.Split('.');
                return (
                    int.Parse(parts[0]),
                    parts.Length > 1 ? int.Parse(parts[1]) : 0,
                    parts.Length > 2 ? int.Parse(parts[2]) : 0
                );
            }
        }

        /// <summary>
        /// Get a LlamaIndex‑compatible PDF reader that uses <see cref="MuPdfRag"/>
        /// under the hood to produce Markdown text per page.
        /// </summary>
        public static PDFMarkdownReader LlamaMarkdownReader(
            Func<Dictionary<string, object>, Dictionary<string, object>> metaFilter = null)
        {
            return new PDFMarkdownReader(metaFilter);
        }

        /// <summary>
        /// Process the document and return the text of the selected pages.
        /// </summary>
        /// <param name="doc">Input <see cref="Document"/> to convert.</param>
        /// <param name="header">Include page headers in output.</param>
        /// <param name="footer">Include page footers in output.</param>
        /// <param name="pages">List of page numbers to consider (0-based).</param>
        /// <param name="writeImages">Save images / graphics as files.</param>
        /// <param name="embedImages">Embed images in markdown text (base64 encoded).</param>
        /// <param name="imagePath">Store images in this folder.</param>
        /// <param name="imageFormat">Use this image format. Choose a supported one (e.g. "png", "jpg").</param>
        /// <param name="filename">Logical filename used in image names and metadata.</param>
        /// <param name="forceText">Output text despite of image background.</param>
        /// <param name="pageChunks">Whether to segment output by page.</param>
        /// <param name="pageSeparators">Whether to include page separators in output.</param>
        /// <param name="dpi">Desired resolution for generated images.</param>
        /// <param name="ocrDpi">DPI for OCR operations.</param>
        /// <param name="pageWidth">Assumption if page layout is variable (reflowable documents).</param>
        /// <param name="pageHeight">Assumption if page layout is variable (reflowable documents). If null, a single tall page is created.</param>
        /// <param name="ignoreCode">Suppress code-like formatting (mono-space fonts).</param>
        /// <param name="showProgress">Print progress as each page is processed.</param>
        /// <param name="useOcr">If beneficial invoke OCR.</param>
        /// <param name="ocrLanguage">Language for OCR.</param>
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
            int ocrDpi = 400,
            float pageWidth = 612,
            float? pageHeight = null,
            bool ignoreCode = false,
            bool showProgress = false,
            bool useOcr = true,
            string ocrLanguage = "eng")
        {
            if (writeImages && embedImages)
                throw new ArgumentException("Cannot both write_images and embed_images");

            var parsedDoc = Helpers.DocumentLayout.ParseDocument(
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
                ocrLanguage: ocrLanguage);

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

        /// <summary>
        /// High‑level helper to convert a <see cref="Document"/> to a JSON representation
        /// of its layout (pages, boxes, metadata). Wraps
        /// <see cref="Helpers.DocumentLayout.ParseDocument"/> and
        /// <see cref="Helpers.ParsedDocument.ToJson"/>.
        /// </summary>
        /// <param name="doc">Input <see cref="Document"/> to convert.</param>
        /// <param name="imageDpi">Desired resolution for generated images.</param>
        /// <param name="imageFormat">Use this image format.</param>
        /// <param name="imagePath">Store images in this folder.</param>
        /// <param name="pages">List of page numbers to consider (0-based).</param>
        /// <param name="ocrDpi">DPI for OCR operations.</param>
        /// <param name="writeImages">Save images / graphics as files.</param>
        /// <param name="embedImages">Embed images in JSON (base64 encoded).</param>
        /// <param name="showProgress">Print progress as each page is processed.</param>
        /// <param name="forceText">Output text despite of image background.</param>
        /// <param name="useOcr">If beneficial invoke OCR.</param>
        /// <param name="ocrLanguage">Language for OCR.</param>
        public static string ToJson(
            Document doc,
            int imageDpi = 150,
            string imageFormat = "png",
            string imagePath = "",
            List<int> pages = null,
            int ocrDpi = 400,
            bool writeImages = false,
            bool embedImages = false,
            bool showProgress = false,
            bool forceText = true,
            bool useOcr = true,
            string ocrLanguage = "eng")
        {
            var parsedDoc = Helpers.DocumentLayout.ParseDocument(
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
                ocrLanguage: ocrLanguage);

            return parsedDoc.ToJson();
        }

        /// <summary>
        /// High‑level helper to convert a <see cref="Document"/> to plain text, using the
        /// same layout analysis as the Markdown conversion but omitting Markdown syntax.
        /// Wraps <see cref="Helpers.DocumentLayout.ParseDocument"/> and
        /// <see cref="Helpers.ParsedDocument.ToText"/>.
        /// </summary>
        /// <param name="doc">Input <see cref="Document"/> to convert.</param>
        /// <param name="filename">Logical filename used in metadata.</param>
        /// <param name="header">Include page headers in output.</param>
        /// <param name="footer">Include page footers in output.</param>
        /// <param name="pages">List of page numbers to consider (0-based).</param>
        /// <param name="ignoreCode">Suppress code-like formatting.</param>
        /// <param name="showProgress">Print progress as each page is processed.</param>
        /// <param name="forceText">Output text despite of image background.</param>
        /// <param name="ocrDpi">DPI for OCR operations.</param>
        /// <param name="useOcr">If beneficial invoke OCR.</param>
        /// <param name="ocrLanguage">Language for OCR.</param>
        /// <param name="tableFormat">Table format for text output (e.g. "grid").</param>
        /// <param name="pageChunks">Whether to segment output by page.</param>
        public static string ToText(
            Document doc,
            string filename = "",
            bool header = true,
            bool footer = true,
            List<int> pages = null,
            bool ignoreCode = false,
            bool showProgress = false,
            bool forceText = true,
            int ocrDpi = 400,
            bool useOcr = true,
            string ocrLanguage = "eng",
            string tableFormat = "grid",
            bool pageChunks = false)
        {
            var parsedDoc = Helpers.DocumentLayout.ParseDocument(
                doc,
                filename: filename,
                pages: pages,
                embedImages: false,
                writeImages: false,
                showProgress: showProgress,
                forceText: forceText,
                useOcr: useOcr,
                ocrLanguage: ocrLanguage);

            return parsedDoc.ToText(
                header: header,
                footer: footer,
                ignoreCode: ignoreCode,
                showProgress: showProgress,
                tableFormat: tableFormat,
                pageChunks: pageChunks);
        }

        /// <summary>
        /// Parse the logical layout of a <see cref="Document"/> into a
        /// <see cref="Helpers.ParsedDocument"/> object, exposing pages, layout boxes,
        /// tables, images and metadata. This is the C# equivalent of the Python
        /// <c>parse_document</c> helper and is the common basis for Markdown / JSON / text output.
        /// </summary>
        /// <param name="doc">Input <see cref="Document"/> to convert.</param>
        /// <param name="filename">Logical filename used in metadata.</param>
        /// <param name="imageDpi">Desired resolution for generated images.</param>
        /// <param name="imageFormat">Use this image format.</param>
        /// <param name="imagePath">Store images in this folder.</param>
        /// <param name="ocrDpi">DPI for OCR operations.</param>
        /// <param name="pages">List of page numbers to consider (0-based).</param>
        /// <param name="writeImages">Save images / graphics as files.</param>
        /// <param name="embedImages">Embed images (base64 encoded).</param>
        /// <param name="showProgress">Print progress as each page is processed.</param>
        /// <param name="forceText">Output text despite of image background.</param>
        /// <param name="useOcr">If beneficial invoke OCR.</param>
        /// <param name="ocrLanguage">Language for OCR.</param>
        public static Helpers.ParsedDocument ParseDocument(
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
            return Helpers.DocumentLayout.ParseDocument(
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
                ocrLanguage: ocrLanguage);
        }

        /// <summary>
        /// Extract key / value information from interactive form fields, including
        /// the pages each field appears on, similar to the Python
        /// <c>utils.extract_form_fields_with_pages</c> helper.
        /// Traverse /AcroForm/Fields hierarchy and return a dict:
        /// fully qualified field name -> {"value": ..., "pages": [...]}
        /// Optionally, the xref of the field is included.
        /// </summary>
        /// <param name="doc">Input <see cref="Document"/>.</param>
        /// <param name="xrefs">Include the xref of the field.</param>
        public static Dictionary<string, Dictionary<string, object>> GetKeyValues(
            Document doc,
            bool xrefs = false)
        {
            if (doc.IsFormPDF != 0)
            {
                return Helpers.Utils.ExtractFormFieldsWithPages(doc, xrefs);
            }
            return new Dictionary<string, Dictionary<string, object>>();
        }
    }
}
