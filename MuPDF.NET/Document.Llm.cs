using System;
using System.Collections.Generic;
using MuPDF.NET.PDF4LLM;
using MuPDF.NET.PDF4LLM.Helpers;

namespace MuPDF.NET
{

    /// <c>Document.to_markdown</c> / <c>to_json</c> / <c>to_text</c> / <c>to_chunks</c>
    /// <summary>
    /// wrappers. Core 4llm sources compile into this assembly.
    /// </summary>

    public partial class Document
    {
        /// <summary>Convert this document to Markdown (<c>Document.to_markdown</c>).</summary>
        public string ToMarkdown(
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
            int ocrDpi = 150,
            float pageWidth = 612,
            float? pageHeight = null,
            bool ignoreCode = false,
            bool showProgress = false,
            bool useOcr = true,
            string ocrLanguage = "eng",
            bool forceOcr = false,
            OcrPageFunction ocrFunction = null,
            string tableOutput = "markdown",
            float? edgeThreshold = null)
        {
            return MuPDF4LLM.ToMarkdown(
                this,
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
                ocrFunction,
                tableOutput,
                edgeThreshold);
        }

        /// <summary>Convert this document to layout JSON (<c>Document.to_json</c>).</summary>
        public string ToJson(
            int imageDpi = 150,
            string imageFormat = "png",
            string imagePath = "",
            List<int> pages = null,
            int ocrDpi = 150,
            bool writeImages = false,
            bool embedImages = false,
            bool showProgress = false,
            bool forceText = true,
            bool useOcr = true,
            string ocrLanguage = "eng",
            bool forceOcr = false,
            OcrPageFunction ocrFunction = null,
            string tableOutput = "markdown",
            float? edgeThreshold = null)
        {
            return MuPDF4LLM.ToJson(
                this,
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
                ocrFunction,
                tableOutput,
                edgeThreshold);
        }

        /// <summary>Convert this document to plain text (<c>Document.to_text</c>).</summary>
        public string ToText(
            string filename = "",
            bool header = true,
            bool footer = true,
            List<int> pages = null,
            bool ignoreCode = false,
            bool showProgress = false,
            bool forceText = true,
            int ocrDpi = 150,
            bool useOcr = true,
            string ocrLanguage = "eng",
            string tableFormat = "grid",
            bool pageChunks = false,
            int tableMaxWidth = 100,
            int tableMinColWidth = 10,
            bool forceOcr = false,
            OcrPageFunction ocrFunction = null,
            float? edgeThreshold = null)
        {
            return MuPDF4LLM.ToText(
                this,
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
                ocrFunction,
                edgeThreshold);
        }

        /// <summary>Chunk this document into retrieval-ready pieces (<c>Document.to_chunks</c>).</summary>
        public ChunkedDocument ToChunks(
            int maxTokens = 400,
            int minTokens = 120,
            float breakpointThreshold = 0.5f,
            bool mergeSmallChunks = true,
            string tableMode = "preserve",
            bool respectSectionStarts = true,
            string headerFooterMode = "exclude",
            string sentenceSplitter = "default",
            object tokenizer = null,
            Dictionary<string, float> weights = null,
            List<int> pages = null,
            string tableOutput = "markdown",
            bool extractImages = false,
            int dpi = 150,
            float? edgeThreshold = null,
            bool forceText = true,
            bool useOcr = true,
            bool forceOcr = false,
            OcrPageFunction ocrFunction = null)
        {
            return MuPDF4LLM.ToChunks(
                this,
                maxTokens,
                minTokens,
                breakpointThreshold,
                mergeSmallChunks,
                tableMode,
                respectSectionStarts,
                headerFooterMode,
                sentenceSplitter,
                tokenizer,
                weights,
                pages,
                tableOutput,
                extractImages,
                dpi,
                edgeThreshold: edgeThreshold,
                forceText: forceText,
                useOcr: useOcr,
                forceOcr: forceOcr,
                ocrFunction: ocrFunction);
        }

        /// <summary>Extract form fields and their values from this PDF.</summary>
        public Dictionary<string, Dictionary<string, object>> GetKeyValues(bool includeXrefs = false)
        {
            return MuPDF4LLM.GetKeyValues(this, includeXrefs);
        }
    }
}
