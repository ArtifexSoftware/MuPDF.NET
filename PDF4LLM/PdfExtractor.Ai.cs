#if NET8_0_OR_GREATER
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PDF4LLM.AI;
using PDF4LLM.AI.Options;

namespace PDF4LLM
{
    public static partial class PdfExtractor
    {
        /// <summary>
        /// Load, chunk, embed, and index PDFs for AI/RAG workflows
        /// (<see cref="AiDocumentCollection.AskAsync"/>, <see cref="AiDocumentCollection.SearchAsync"/>).
        /// Requires <c>net8.0</c> (Microsoft.Extensions.AI).
        /// </summary>
        public static Task<AiDocumentCollection> LoadAiAsync(
            IEnumerable<string> pdfPaths,
            MicrosoftAIConnectorOptions? options = null,
            CancellationToken cancellationToken = default)
            => MsAIConnector.LoadAsync(pdfPaths, options, cancellationToken);
    }
}
#endif
