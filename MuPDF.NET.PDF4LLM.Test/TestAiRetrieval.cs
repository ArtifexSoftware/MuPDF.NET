using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using MuPDF.NET.PDF4LLM;
using MuPDF.NET.PDF4LLM.AI;
using MuPDF.NET.PDF4LLM.AI.Models;
using MuPDF.NET.PDF4LLM.AI.Options;
using MuPDF.NET.PDF4LLM.AI.Services;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
    public class TestAiRetrieval
    {
        [Fact]
        public void Prose_is_packed_to_the_character_limit()
        {
            string page = string.Join("\n", Enumerable.Range(0, 40)
                .Select(i => $"Sentence {i} of a report. These findings are independent of any sample PDF."));
            List<ChunkPart> parts = TextChunker.SplitPage(page, 200, 20);
            Assert.True(parts.Count > 1, $"Expected several prose chunks, got {parts.Count}.");
            Assert.Contains(parts, p => p.Text.Contains("Sentence 0"));
            Assert.Contains(parts, p => p.Text.Contains("Sentence 39"));
        }

        [Fact]
        public void Connector_defaults_are_for_any_pdf_not_a_sample()
        {
            var options = new MicrosoftAIConnectorOptions();
            Assert.Equal(8, options.DefaultTopK);
            Assert.Equal(2, options.AskExcerptCount);
            Assert.Equal(0.35, options.AskMinScore);
            Assert.True(options.EmbedAskQuery);
            Assert.Equal(1024, options.MaxOutputTokens);
            Assert.False(options.AskAllContext);
            Assert.Null(options.LocalIndexPath);
        }

        [Fact]
        public void Table_rows_keep_the_header_and_title()
        {
            const string page =
                "NATO Members\n" +
                "Country;Status;Year\n" +
                "Turkey;Joined;1952\n" +
                "United Kingdom;Founder;1949\n" +
                "\n" +
                "World Capital Cities\n" +
                "| Country | Capital | Population |\n" +
                "| --- | --- | --- |\n" +
                "| South Sudan | Juba | 1200000 |\n" +
                "| France | Paris | 2100000 |\n";

            List<ChunkPart> parts = TextChunker.SplitPage(page, 1000, 100);

            Assert.Contains(parts, p => p.Text.Contains("NATO Members") && p.Text.Contains("Turkey"));
            Assert.Contains(parts, p => p.Text.Contains("South Sudan") && p.Text.Contains("Juba") && p.Text.Contains("Capital"));
            Assert.DoesNotContain(parts, p => p.Text.Contains("---"));
        }

        [Fact]
        public void Lexical_ranks_by_query_words()
        {
            const string query = "what is the capital of South Sudan?";
            double juba = LexicalScore.Score(query,
                "Country | Capital | Population\nSouth Sudan | Juba | 525953");
            double paris = LexicalScore.Score(query,
                "Country | Capital | Population\nFrance | Paris | 2139907");
            Assert.True(juba > paris, $"juba={juba} paris={paris}");
        }

        [Fact]
        public async Task Search_finds_a_row_by_the_words_in_the_question()
        {
            string path = Path.Combine(Path.GetTempPath(), "pdf4llm-ai-rows.pdf");
            var lines = new List<string> { "Country;Capital;Population" };
            lines.Add("France;Paris;2100000");
            lines.Add("South Sudan;Juba;1200000");
            lines.Add("Germany;Berlin;3677472");
            for (int i = 0; i < 30; i++)
                lines.Add($"Country{i};City{i};{1000 + i}");

            using (var doc = new MuPDF.NET.Document())
            {
                MuPDF.NET.Page page = doc.NewPage();
                float y = 800;
                foreach (string line in lines)
                {
                    if (y < 72)
                    {
                        page = doc.NewPage();
                        y = 800;
                    }

                    page.InsertText(new MuPDF.NET.Point(72, y), line, fontSize: 9);
                    y -= 16;
                }

                doc.Save(path);
            }

            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(false);
                var pages = PageExtractor.Extract(path, useLayout: false);
                string preview = string.Join("\n---\n", pages.Select(p => $"p{p.PageNumber} [{p.Text.Length}] {p.Text}"));
                Assert.True(pages.Count > 0 && pages.Any(p => p.Text.Contains("Juba")), preview);

                AiDocumentCollection ai = await MuPDF4LLM.LoadAiAsync(
                    new[] { path },
                    MicrosoftAIConnectorOptions.CreateForDevelopment(chatClient: new EchoChat()));

                Assert.True(ai.ChunkCount > 5, $"Expected row chunks, got {ai.ChunkCount}.");
                IReadOnlyList<SearchResult> hits = await ai.SearchAsync("what is the capital of South Sudan?", topK: 1);
                Assert.Contains("Juba", hits[0].Chunk.Text);

                string echoed = await ai.AskAsync("what is the capital of South Sudan?");
                Assert.Contains("Juba", echoed);
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(prior);
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public async Task Embed_retries_when_the_connection_is_reset()
        {
            var chunks = new List<AiChunk>
            {
                Chunk("a"),
                Chunk("b"),
                Chunk("c"),
                Chunk("d"),
            };
            var embedder = new DroppingEmbedder(failFirst: 2);

            await EmbeddingIndexer.EmbedAsync(chunks, embedder, batchSize: 4);

            Assert.Equal(3, embedder.Calls);
            Assert.All(chunks, chunk => Assert.Equal(2, chunk.Embedding.Length));
        }

        [Fact]
        public async Task Search_retries_http_500_then_ranks_by_words()
        {
            var chunks = new List<AiChunk>
            {
                Chunk("France;Paris;2100000"),
                Chunk("South Sudan;Juba;1200000"),
            };
            var embedder = new HashEmbeddingGenerator(128);
            await EmbeddingIndexer.EmbedAsync(chunks, embedder, batchSize: 4);
            var index = new InMemoryVectorIndex();
            await index.IndexAsync(chunks);

            var failing = new DroppingEmbedder(failFirst: 2, statusCode: HttpStatusCode.InternalServerError);
            var ai = new AiDocumentCollection(index, failing, chatClient: null, chunks, defaultTopK: 8);

            IReadOnlyList<SearchResult> hits = await ai.SearchAsync("what is the capital of South Sudan?", topK: 1);

            Assert.Equal(3, failing.Calls);
            Assert.Contains("Juba", hits[0].Chunk.Text);
        }

        [Fact]
        public async Task Search_ranks_by_words_when_embed_keeps_failing()
        {
            var chunks = new List<AiChunk>
            {
                Chunk("France;Paris;2100000"),
                Chunk("South Sudan;Juba;1200000"),
            };
            var embedder = new HashEmbeddingGenerator(128);
            await EmbeddingIndexer.EmbedAsync(chunks, embedder, batchSize: 4);
            var index = new InMemoryVectorIndex();
            await index.IndexAsync(chunks);

            var failing = new DroppingEmbedder(failFirst: 99, statusCode: HttpStatusCode.InternalServerError);
            var ai = new AiDocumentCollection(index, failing, chatClient: null, chunks, defaultTopK: 8);

            IReadOnlyList<SearchResult> hits = await ai.SearchAsync("what is the capital of South Sudan?", topK: 1);

            Assert.Contains("Juba", hits[0].Chunk.Text);
        }

        [Fact]
        public async Task Ask_instructs_the_model_from_excerpts_not_a_sample_schema()
        {
            var chunks = new List<AiChunk>
            {
                Chunk("Invoice 441. Amount due 1200. Payment terms net 30."),
            };
            var embedder = new HashEmbeddingGenerator(128);
            await EmbeddingIndexer.EmbedAsync(chunks, embedder, batchSize: 4);
            var index = new InMemoryVectorIndex();
            await index.IndexAsync(chunks);

            var chat = new CapturingChat();
            var ai = new AiDocumentCollection(index, embedder, chat, chunks, defaultTopK: 8);

            await ai.AskAsync("What is the amount due?");

            Assert.NotNull(chat.SystemText);
            Assert.Contains("excerpts", chat.SystemText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("untrusted", chat.SystemText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("table rows", chat.SystemText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("populations", chat.SystemText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Invoice 441", chat.UserText);
        }

        [Fact]
        public async Task Ask_retries_when_the_connection_is_reset()
        {
            var chunks = new List<AiChunk>
            {
                Chunk("South Sudan;Juba;1200000"),
            };
            var embedder = new HashEmbeddingGenerator(128);
            await EmbeddingIndexer.EmbedAsync(chunks, embedder, batchSize: 4);
            var index = new InMemoryVectorIndex();
            await index.IndexAsync(chunks);

            var chat = new DroppingChat(failFirst: 2);
            var ai = new AiDocumentCollection(index, embedder, chat, chunks, defaultTopK: 8);

            string answer = await ai.AskAsync("what is the capital of South Sudan?");

            Assert.Equal(3, chat.Calls);
            Assert.Contains("Juba", answer);
        }

        static AiChunk Chunk(string text) => new AiChunk
        {
            Id = text,
            Text = text,
            SourceFilePath = text + ".pdf",
            SourceFileName = text + ".pdf",
            PageNumber = 1,
        };

        sealed class DroppingEmbedder : IEmbeddingGenerator<string, Embedding<float>>
        {
            readonly int _failFirst;
            readonly HttpStatusCode? _statusCode;

            public DroppingEmbedder(int failFirst, HttpStatusCode? statusCode = null)
            {
                _failFirst = failFirst;
                _statusCode = statusCode;
            }

            public int Calls { get; private set; }

            public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
                IEnumerable<string> values,
                EmbeddingGenerationOptions options = null,
                CancellationToken cancellationToken = default)
            {
                Calls++;
                if (Calls <= _failFirst)
                {
                    if (_statusCode is HttpStatusCode status)
                    {
                        throw new HttpRequestException(
                            $"Response status code does not indicate success: {(int)status} ({status}).",
                            inner: null,
                            status);
                    }

                    throw new HttpRequestException(
                        "An error occurred while sending the request.",
                        new IOException("An existing connection was forcibly closed by the remote host."));
                }

                var vectors = values
                    .Select(text => new Embedding<float>(new[] { (float)text.Length, 1f }))
                    .ToList();
                return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(vectors));
            }

            public object GetService(System.Type serviceType, object serviceKey = null) => null;

            public void Dispose() { }
        }

        sealed class DroppingChat : IChatClient
        {
            readonly int _failFirst;

            public DroppingChat(int failFirst) => _failFirst = failFirst;

            public int Calls { get; private set; }

            public ChatClientMetadata Metadata { get; } = new("drop");

            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions options = null,
                CancellationToken cancellationToken = default)
            {
                Calls++;
                if (Calls <= _failFirst)
                {
                    throw new HttpRequestException(
                        "An error occurred while sending the request.",
                        new IOException("An existing connection was forcibly closed by the remote host."));
                }

                string user = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "";
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, user)));
            }

            public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions options = null,
                CancellationToken cancellationToken = default) =>
                throw new System.NotSupportedException();

            public object GetService(System.Type serviceType, object serviceKey = null) => null;

            public void Dispose() { }
        }

        sealed class CapturingChat : IChatClient
        {
            public ChatClientMetadata Metadata { get; } = new("capture");

            public string SystemText { get; private set; } = "";
            public string UserText { get; private set; } = "";

            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions options = null,
                CancellationToken cancellationToken = default)
            {
                SystemText = messages.FirstOrDefault(m => m.Role == ChatRole.System)?.Text ?? "";
                UserText = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "";
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
            }

            public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions options = null,
                CancellationToken cancellationToken = default) =>
                throw new System.NotSupportedException();

            public object GetService(System.Type serviceType, object serviceKey = null) => null;

            public void Dispose() { }
        }

        sealed class EchoChat : IChatClient
        {
            public ChatClientMetadata Metadata { get; } = new("echo");

            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions options = null,
                CancellationToken cancellationToken = default)
            {
                string user = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "";
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, user)));
            }

            public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions options = null,
                CancellationToken cancellationToken = default) =>
                throw new System.NotSupportedException();

            public object GetService(System.Type serviceType, object serviceKey = null) => null;

            public void Dispose() { }
        }
    }
}
