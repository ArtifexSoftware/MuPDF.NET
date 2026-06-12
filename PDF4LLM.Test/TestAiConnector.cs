using Microsoft.Extensions.AI;
using PDF4LLM.AI;
using PDF4LLM.AI.Models;
using PDF4LLM.AI.Options;
using PDF4LLM.AI.Services;

namespace PDF4LLM.Test
{
    /// <summary>Tests for PdfExtractor.LoadAiAsync / MicrosoftAIConnector RAG pipeline.</summary>
    [TestFixture]
    public class TestAiConnector : LLMTestBase
    {
        private string? FindSamplePdf()
        {
            foreach (string name in new[] { "test_tablulate_bug.pdf", "test_137.pdf" })
            {
                string path = FixturePath(name);
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        [Test]
        public void TextChunkingService_splits_long_text_with_overlap()
        {
            string text = new string('a', 500) + "\n" + new string('b', 500);
            IReadOnlyList<string> parts = TextChunkingService.SplitWithOverlap(text, maxChars: 400, overlapChars: 50);
            Assert.That(parts.Count, Is.GreaterThan(1));
            Assert.That(parts.All(p => p.Length <= 400), Is.True);
        }

        [Test]
        public async Task LoadAiAsync_indexes_pdf_with_in_memory_pipeline()
        {
            string? pdf = FindSamplePdf();
            if (pdf == null)
            {
                Assert.Ignore("No sample PDF found in repo.");
                return;
            }

            var options = MicrosoftAIConnectorOptions.CreateForDevelopment();
            AiDocumentCollection docs = await PdfExtractor.LoadAiAsync(new[] { pdf }, options);

            Assert.That(docs.ChunkCount, Is.GreaterThan(0));
            Assert.That(docs.SourceFiles, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task SearchAsync_returns_ranked_chunks()
        {
            string? pdf = FindSamplePdf();
            if (pdf == null)
            {
                Assert.Ignore("No sample PDF found in repo.");
                return;
            }

            var options = MicrosoftAIConnectorOptions.CreateForDevelopment();
            AiDocumentCollection docs = await PdfExtractor.LoadAiAsync(new[] { pdf }, options);

            IReadOnlyList<SearchResult> hits = await docs.SearchAsync("table", topK: 3);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0].Score, Is.GreaterThanOrEqualTo(hits[^1].Score));
        }

        [Test]
        public void AskAsync_requires_chat_client()
        {
            var options = MicrosoftAIConnectorOptions.CreateForDevelopment(chatClient: null);
            Assert.That(options.ChatClient, Is.Null);
        }

        [Test]
        public async Task AskAsync_works_with_fake_chat_client()
        {
            string? pdf = FindSamplePdf();
            if (pdf == null)
            {
                Assert.Ignore("No sample PDF found in repo.");
                return;
            }

            var options = MicrosoftAIConnectorOptions.CreateForDevelopment(chatClient: new EchoChatClient());
            AiDocumentCollection docs = await PdfExtractor.LoadAiAsync(new[] { pdf }, options);

            string answer = await docs.AskAsync("What is in this document?");
            Assert.That(answer, Does.Contain("What is in this document?"));
        }

        private sealed class EchoChatClient : IChatClient
        {
            public ChatClientMetadata Metadata { get; } = new("echo");

            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions options = null,
                CancellationToken cancellationToken = default)
            {
                string lastUser = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "";
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, $"Echo: {lastUser}")));
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
