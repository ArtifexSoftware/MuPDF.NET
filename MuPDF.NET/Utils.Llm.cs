using System;
using System.Collections.Generic;
#if NET8_0_OR_GREATER
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using MuPDF.NET.PDF4LLM.AI;
using MuPDF.NET.PDF4LLM.AI.Options;
#endif
using MuPDF.NET.PDF4LLM;
using MuPDF.NET.PDF4LLM.Helpers;
using MuPDF.NET.PDF4LLM.Helpers.TableHtml;
using MuPDF.NET.PDF4LLM.Layout;
using MuPDF.NET.PDF4LLM.Ocr;
using TableHtmlApi = MuPDF.NET.PDF4LLM.Helpers.TableHtml.TableHtml;

namespace MuPDF.NET
{
    /// <summary>
    /// Module-level layout / LLM helpers (<c>use_layout</c>, <c>to_markdown</c>, …).
    /// Prefer <see cref="Document.ToMarkdown"/> / <see cref="Document.ToJson"/> /
    /// <see cref="Document.ToText"/> / <see cref="Document.ToChunks"/> on an open document.
    /// </summary>
    public static partial class Utils
    {
        /// <summary>Enable or disable the layout analysis pipeline (<c>use_layout</c>).</summary>
        public static void UseLayout(bool yes) => MuPDF4LLM.SetUseLayout(yes);

        /// <summary>Whether the layout analysis pipeline is enabled.</summary>
        public static bool LayoutEnabled => MuPDF4LLM.UseLayout;

        /// <summary>
        /// Raw layout flag without registering or clearing the page provider.
        /// Prefer <see cref="UseLayout"/> for normal enable/disable.
        /// </summary>
        public static bool LayoutFlag
        {
            get => MuPDF4LLM.UseLayout;
            set => MuPDF4LLM.UseLayout = value;
        }

        /// <summary>Whether a layout provider is currently registered.</summary>
        public static bool LayoutAvailable => MuPDF4LLM.LayoutAvailable;

        /// <summary>Whether the layout Python worker can be imported.</summary>
        public static bool LayoutPythonAvailable => PyMuPdfLayout.IsAvailable;

        /// <summary>Whether a layout provider is currently registered.</summary>
        public static bool LayoutActivated => PyMuPdfLayout.IsActivated;

        /// <summary>Start layout analysis and register the page layout provider.</summary>
        public static bool ActivateLayout() => PyMuPdfLayout.Activate();

        /// <summary>Stop layout analysis and clear the layout provider.</summary>
        public static void DeactivateLayout() => PyMuPdfLayout.Deactivate();

        /// <summary>True when RapidOCR initialized successfully on this runtime.</summary>
        public static bool RapidOcrAvailable => RapidOcrSupport.IsAvailable;

        /// <summary>U+FFFD placeholder used when OCR cannot read a glyph.</summary>
        public static char OcrReplacementChar => TesseractApi.ReplacementUnicode;

        /// <summary>Convert a batch of document paths (<c>convert_batch</c>).</summary>
        public static List<string> ConvertBatch(
            IReadOnlyList<string> inputs,
            string backend = "md")
        {
            return MuPDF4LLM.ConvertBatch(inputs, backend);
        }

        /// <summary>Returns a LlamaIndex-compatible PDF markdown reader.</summary>
        public static PDFMarkdownReader LlamaMarkdownReader(
            Func<Dictionary<string, object>, Dictionary<string, object>> metaFilter = null)
        {
            return new PDFMarkdownReader(metaFilter);
        }

        /// <summary>Extract form fields and their values from a PDF document.</summary>
        public static Dictionary<string, Dictionary<string, object>> GetKeyValues(
            Document doc,
            bool includeXrefs = false)
        {
            return MuPDF4LLM.GetKeyValues(doc, includeXrefs);
        }

        /// <summary>Extract form fields from a PDF file path.</summary>
        public static Dictionary<string, Dictionary<string, object>> GetKeyValues(
            string path,
            bool includeXrefs = false)
        {
            return MuPDF4LLM.GetKeyValues(path, includeXrefs);
        }

        /// <summary>Render a Markdown file to PDF (Story).</summary>
        public static Document MarkdownToPdf(
            string mdPath,
            string userCss = null,
            Rect pageRect = default,
            float[] margins = null,
            Archive archive = null,
            string outputPath = null)
        {
            return MuPDF4LLM.MarkdownToPdf(mdPath, userCss, pageRect, margins, archive, outputPath);
        }

        /// <summary>Convert a document to Markdown.</summary>
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
                doc, header, footer, pages, writeImages, embedImages, imagePath, imageFormat,
                filename, forceText, pageChunks, pageSeparators, dpi, ocrDpi, pageWidth, pageHeight,
                ignoreCode, showProgress, useOcr, ocrLanguage, forceOcr, ocrFunction, tableOutput,
                edgeThreshold);
        }

        /// <summary>Convert a supported document path to Markdown.</summary>
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
                path, header, footer, pages, writeImages, embedImages, imagePath, imageFormat,
                filename, forceText, pageChunks, pageSeparators, dpi, ocrDpi, pageWidth, pageHeight,
                ignoreCode, showProgress, useOcr, ocrLanguage, forceOcr, ocrFunction, tableOutput,
                edgeThreshold);
        }

        /// <summary>Convert a document to layout JSON.</summary>
        public static string ToJson(
            Document doc,
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
                doc, imageDpi, imageFormat, imagePath, pages, ocrDpi, writeImages, embedImages,
                showProgress, forceText, useOcr, ocrLanguage, forceOcr, ocrFunction, tableOutput,
                edgeThreshold);
        }

        /// <summary>Convert a supported document path to layout JSON.</summary>
        public static string ToJson(
            string path,
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
                path, imageDpi, imageFormat, imagePath, pages, ocrDpi, writeImages, embedImages,
                showProgress, forceText, useOcr, ocrLanguage, forceOcr, ocrFunction, tableOutput,
                edgeThreshold);
        }

        /// <summary>Convert a document to plain text.</summary>
        public static string ToText(
            Document doc,
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
                doc, filename, header, footer, pages, ignoreCode, showProgress, forceText, ocrDpi,
                useOcr, ocrLanguage, tableFormat, pageChunks, tableMaxWidth, tableMinColWidth,
                forceOcr, ocrFunction, edgeThreshold);
        }

        /// <summary>Convert a supported document path to plain text.</summary>
        public static string ToText(
            string path,
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
                path, filename, header, footer, pages, ignoreCode, showProgress, forceText, ocrDpi,
                useOcr, ocrLanguage, tableFormat, pageChunks, tableMaxWidth, tableMinColWidth,
                forceOcr, ocrFunction, edgeThreshold);
        }

        /// <summary>Chunk a document into retrieval-ready pieces.</summary>
        public static ChunkedDocument ToChunks(
            Document doc,
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
                doc, maxTokens, minTokens, breakpointThreshold, mergeSmallChunks, tableMode,
                respectSectionStarts, headerFooterMode, sentenceSplitter, tokenizer, weights, pages,
                tableOutput, extractImages, dpi, edgeThreshold: edgeThreshold, forceText: forceText,
                useOcr: useOcr, forceOcr: forceOcr, ocrFunction: ocrFunction);
        }

        /// <summary>Chunk a document path into retrieval-ready pieces.</summary>
        public static ChunkedDocument ToChunks(
            string path,
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
                path, maxTokens, minTokens, breakpointThreshold, mergeSmallChunks, tableMode,
                respectSectionStarts, headerFooterMode, sentenceSplitter, tokenizer, weights, pages,
                tableOutput, extractImages, dpi, edgeThreshold: edgeThreshold, forceText: forceText,
                useOcr: useOcr, forceOcr: forceOcr, ocrFunction: ocrFunction);
        }

        /// <summary>Reconstruct HTML tables for one page of a PDF path or <see cref="Document"/>.</summary>
        public static string ToHtmlTable(object pdf, int pageIndex = 0) =>
            TableHtmlApi.ToHtml(pdf, pageIndex);

        /// <summary>Per-page HTML table payload used by the layout HTML table path.</summary>
        public static List<(Rect bbox, string html, int rows, int cols, List<List<object>> cells, List<List<object>> extract)>
            PageHtmlTables(Page page) =>
            TableHtmlApi.PageHtmlTables(page);

        /// <summary>
        /// FindTables entry used by HTML reconstruction. Tests replace this to observe
        /// <c>useLayout</c>/<c>union</c>/<c>refine</c>.
        /// </summary>
        public static Func<Page, bool, bool, bool, TableFinder> HtmlFindTablesHook
        {
            get => TableHtmlApi.FindTablesHook;
            set => TableHtmlApi.FindTablesHook = value;
        }

        /// <summary>Observed named arguments from layout <c>parse_document</c> (test hook).</summary>
        public static Action<ParseDocumentCall> ParseDocumentObserver
        {
            get => _parseDocumentObserver;
            set
            {
                _parseDocumentObserver = value;
                DocumentLayout.ParseDocumentObserver = value == null
                    ? null
                    : info => value(new ParseDocumentCall(info));
            }
        }

        static Action<ParseDocumentCall> _parseDocumentObserver;

        /// <summary>Validate layout chunker parameters (<c>to_chunks</c>).</summary>
        public static void ValidateChunkParams(Dictionary<string, object> parameters) =>
            global::MuPDF.NET.PDF4LLM.Helpers.Chunking.LayoutChunker.ValidateParams(parameters);

        /// <summary>Horizontal overlap of two <c>[x0,y0,x1,y1]</c> boxes (layout chunker).</summary>
        public static double HorizontalOverlapRatio(double[] a, double[] b) =>
            global::MuPDF.NET.PDF4LLM.Helpers.Chunking.ChunkGeometry.HorizontalOverlapRatio(a, b);

#if NET8_0_OR_GREATER
        /// <summary>Load, chunk, embed, and index PDFs for AI/RAG workflows.</summary>
        public static Task<AiDocumentCollection> LoadAiAsync(
            IEnumerable<string> pdfPaths,
            MicrosoftAIConnectorOptions options = null,
            CancellationToken cancellationToken = default)
            => MuPDF4LLM.LoadAiAsync(pdfPaths, options, cancellationToken);

        /// <summary>In-memory AI pipeline for tests and samples (hash embeddings).</summary>
        public static MicrosoftAIConnectorOptions CreateDevelopmentAiOptions(
            IChatClient chatClient = null,
            int embeddingDimensions = 128)
            => MicrosoftAIConnectorOptions.CreateForDevelopment(chatClient, embeddingDimensions);

        /// <summary>AI pipeline options for a provided chat client and embedding generator.</summary>
        public static MicrosoftAIConnectorOptions CreateAiOptions(
            IChatClient chatClient = null,
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = null,
            bool useLayout = false,
            int askExcerptCount = 2,
            double askMinScore = 0.35,
            int embedBatchSize = 4,
            bool embedAskQuery = true,
            int maxOutputTokens = 1024,
            string localIndexPath = null,
            Action<string> trace = null,
            int defaultTopK = 8)
        {
            var options = new MicrosoftAIConnectorOptions
            {
                ChatClient = chatClient,
                EmbeddingGenerator = embeddingGenerator,
                UseLayout = useLayout,
                AskExcerptCount = askExcerptCount,
                AskMinScore = askMinScore,
                EmbedBatchSize = embedBatchSize,
                EmbedAskQuery = embedAskQuery,
                MaxOutputTokens = maxOutputTokens,
                LocalIndexPath = localIndexPath,
                Trace = trace,
                DefaultTopK = defaultTopK,
            };
            return options;
        }

        /// <summary>Azure OpenAI (+ optional Azure AI Search) AI pipeline options.</summary>
        public static MicrosoftAIConnectorOptions CreateAzureAiOptions(
            string endpoint,
            string apiKey,
            string chatDeployment,
            string embeddingDeployment,
            string searchEndpoint = null,
            string searchApiKey = null,
            string searchIndexName = null,
            bool useLayout = false)
        {
            return new AzureAiOptions
            {
                OpenAiEndpoint = endpoint,
                OpenAiApiKey = apiKey,
                ChatDeploymentName = chatDeployment,
                EmbeddingDeploymentName = embeddingDeployment,
                SearchEndpoint = searchEndpoint,
                SearchApiKey = searchApiKey,
                SearchIndexName = searchIndexName,
                UseLayout = useLayout,
            }.ToConnectorOptions();
        }
#endif
    }

    /// <summary>Observed named arguments from layout parse (test hook).</summary>
    public sealed class ParseDocumentCall
    {
        internal ParseDocumentCall(ParseDocumentCallInfo info)
        {
            Kwargs = info.Kwargs;
            RenderHtmlTables = info.RenderHtmlTables;
        }

        public Dictionary<string, object> Kwargs { get; }
        public bool? RenderHtmlTables { get; }
    }
}
