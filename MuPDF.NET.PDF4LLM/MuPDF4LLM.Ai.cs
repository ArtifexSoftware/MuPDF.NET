#if NET8_0_OR_GREATER
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MuPDF.NET.PDF4LLM.AI;
using MuPDF.NET.PDF4LLM.AI.Options;

namespace MuPDF.NET.PDF4LLM
{
    public static partial class MuPDF4LLM
    {
        /// <summary>
        /// Load, chunk, embed, and index PDFs for AI/RAG workflows
        /// (<see cref="AiDocumentCollection.AskAsync"/>, <see cref="AiDocumentCollection.SearchAsync"/>).
        /// Requires <c>net8.0</c> (Microsoft.Extensions.AI).
        /// </summary>
        /// <param name="pdfPaths">Paths to one or more PDF files to index.</param>
        /// <param name="options">Pipeline configuration; uses in-memory development defaults when <see langword="null"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Task<AiDocumentCollection> LoadAiAsync(
            IEnumerable<string> pdfPaths,
            MicrosoftAIConnectorOptions? options = null,
            CancellationToken cancellationToken = default)
            => MsAIConnector.LoadAsync(pdfPaths, options, cancellationToken);
    }
}
#endif
