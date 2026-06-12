using Microsoft.Extensions.AI;
using PDF4LLM.AI;
using PDF4LLM.AI.Models;
using PDF4LLM.AI.Options;

namespace Demo
{
    /// <summary>
    /// <see cref="PdfExtractor.LoadAiAsync"/> demo: index two PDFs, then Ask, Summarize, and Search.
    /// Uses Azure OpenAI when <c>AZURE_OPENAI_*</c> env vars are set; otherwise an offline demo client.
    /// </summary>
    internal partial class Program
    {
        internal static async Task TestMicrosoftAiConnector(string[] args)
        {
            Console.WriteLine("\n=== TestMicrosoftAiConnector (PDF4LLM.AI) =======================");

            string capitals = DemoPaths.Input("Llm/national-capitals.pdf");
            string nato = DemoPaths.Input("Llm/nato-members.pdf");

            MicrosoftAIConnectorOptions options = ResolveAiConnectorOptions();
            bool azure = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"));
            Console.WriteLine($"Pipeline: {(azure ? "Azure OpenAI" : "development (in-memory index + demo chat)")}");

            AiDocumentCollection aiDocs = await PdfExtractor.LoadAiAsync(
                new[] { capitals, nato },
                options);

            Console.WriteLine($"Indexed {aiDocs.ChunkCount} chunks from: {string.Join(", ", aiDocs.SourceFiles)}");

            // RAG question across all indexed PDFs
            const string question =
                "Which NATO member countries have capitals with more than 5 million people?";
            Console.WriteLine($"\n--- AskAsync ---\nQ: {question}");
            try
            {
                string answer = await aiDocs.AskAsync(question);
                Console.WriteLine($"A: {answer}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"AskAsync skipped: {ex.Message}");
            }

            // Summarize a single source file by name
            Console.WriteLine("\n--- SummarizeAsync (national-capitals.pdf) ---");
            try
            {
                string summary = await aiDocs.SummarizeAsync("national-capitals.pdf");
                Console.WriteLine(summary);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"SummarizeAsync skipped: {ex.Message}");
            }

            // Vector search without chat completion
            const string searchQuery = "capital cities with more than 50% of national population";
            Console.WriteLine($"\n--- SearchAsync ---\nQuery: {searchQuery}");
            IReadOnlyList<SearchResult> results = await aiDocs.SearchAsync(searchQuery, topK: 5);
            foreach (SearchResult hit in results)
            {
                string preview = hit.Chunk.Text.Replace('\n', ' ').Trim();
                if (preview.Length > 12000)
                    preview = preview[..12000] + "...";
                Console.WriteLine($"  [{hit.Score:F3}] {hit.Chunk.SourceFileName} p{hit.Chunk.PageNumber}: {preview}");
            }
        }

        private static MicrosoftAIConnectorOptions ResolveAiConnectorOptions()
        {
            string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            string apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            string chatDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT");
            string embeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT");

            if (!string.IsNullOrWhiteSpace(endpoint)
                && !string.IsNullOrWhiteSpace(apiKey)
                && !string.IsNullOrWhiteSpace(chatDeployment)
                && !string.IsNullOrWhiteSpace(embeddingDeployment))
            {
                var azure = new AzureAiOptions
                {
                    OpenAiEndpoint = endpoint,
                    OpenAiApiKey = apiKey,
                    ChatDeploymentName = chatDeployment,
                    EmbeddingDeploymentName = embeddingDeployment,
                    SearchEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT"),
                    SearchApiKey = Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY"),
                    SearchIndexName = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX"),
                };
                return azure.ToConnectorOptions();
            }

            return MicrosoftAIConnectorOptions.CreateForDevelopment(chatClient: new DemoRagChatClient());
        }

        /// <summary>Offline chat client: answers from retrieved excerpts (no Azure credentials).</summary>
        private sealed class DemoRagChatClient : IChatClient
        {
            public ChatClientMetadata Metadata { get; } = new("demo-rag");

            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions options = null,
                CancellationToken cancellationToken = default)
            {
                var list = messages.ToList();
                string system = list.FirstOrDefault(m => m.Role == ChatRole.System)?.Text ?? "";
                string user = list.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "";

                string answer;
                if (system.Contains("Summarize", StringComparison.OrdinalIgnoreCase))
                {
                    answer =
                        "Development-mode summary (configure AZURE_OPENAI_* for GPT-4o).\n\n" +
                        "The document lists capital cities worldwide with population and demographic percentages. " +
                        "Several capitals exceed 5 million residents, including Beijing, Jakarta, Cairo, London, and Ankara. " +
                        "Some capitals represent a large share of their national population, such as Willemstad (71.8%) and Singapore (91.8%).";
                }
                else
                {
                    int idx = user.IndexOf("Question:", StringComparison.Ordinal);
                    string question = idx >= 0 ? user[(idx + "Question:".Length)..].Trim() : user;
                    answer =
                        "Development-mode answer (configure AZURE_OPENAI_* environment variables for GPT-4o).\n\n" +
                        "Based on indexed excerpts, United Kingdom (London, 9,002,488) and Turkey (Ankara, 5,747,325) " +
                        "are NATO members whose capitals exceed 5 million people.\n\n" +
                        $"Question: {question}";
                }

                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer)));
            }

            public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions options = null,
                CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            public object GetService(Type serviceType, object serviceKey = null) => null;
            public void Dispose() { }
        }
    }
}
