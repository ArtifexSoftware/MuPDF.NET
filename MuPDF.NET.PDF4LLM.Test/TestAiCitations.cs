using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using MuPDF.NET.PDF4LLM.AI;
using MuPDF.NET.PDF4LLM.AI.Models;
using MuPDF.NET.PDF4LLM.AI.Services;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
    public class TestAiCitations
    {
        [Fact]
        public void Parser_reads_fenced_json()
        {
            ParsedModelAnswer? parsed = AnswerParser.TryParse(
                "Here.\n```json\n{\"answer\":\"1200 EUR\",\"insufficientEvidence\":false," +
                "\"citations\":[{\"sourceId\":\"inv-1\",\"page\":1,\"quote\":\"1200 EUR\"}]}\n```");
            Assert.NotNull(parsed);
            Assert.Equal("1200 EUR", parsed!.Answer);
            Assert.False(parsed.InsufficientEvidence);
            Assert.Equal("inv-1", parsed.Citations[0].SourceId);
            Assert.Equal(1, parsed.Citations[0].Page);
        }

        [Fact]
        public void Validator_rejects_unknown_source_wrong_page_and_missing_quote()
        {
            var excerpts = new List<RetrievedPassage>
            {
                Passage("inv-1", "invoice.pdf", 1, "Invoice 441. Amount due 1200 EUR."),
            };

            ParsedModelAnswer unknown = Parse(
                "{\"answer\":\"x\",\"citations\":[{\"sourceId\":\"nope\",\"page\":1,\"quote\":\"1200 EUR\"}]}");
            Assert.False(CitationValidator.Validate(unknown, excerpts).Ok);

            ParsedModelAnswer page = Parse(
                "{\"answer\":\"x\",\"citations\":[{\"sourceId\":\"inv-1\",\"page\":9,\"quote\":\"1200 EUR\"}]}");
            Assert.False(CitationValidator.Validate(page, excerpts).Ok);

            ParsedModelAnswer quote = Parse(
                "{\"answer\":\"x\",\"citations\":[{\"sourceId\":\"inv-1\",\"page\":1,\"quote\":\"not in the excerpt\"}]}");
            Assert.False(CitationValidator.Validate(quote, excerpts).Ok);

            ParsedModelAnswer ok = Parse(
                "{\"answer\":\"1200 EUR\",\"citations\":[{\"sourceId\":\"inv-1\",\"page\":1,\"quote\":\"Amount due 1200 EUR\"}]}");
            CitationCheck check = CitationValidator.Validate(ok, excerpts);
            Assert.True(check.Ok);
            Assert.Equal("invoice.pdf", check.Citations[0].SourceFileName);
        }

        [Fact]
        public async Task Ask_accepts_json_with_a_quote_from_the_excerpt()
        {
            const string id = "notes.pdf|p1|c0";
            var chunks = new List<AiChunk> { Chunk(id, "Contoso CLI version 4.1.0 shipped on 12 March 2024.", "notes.pdf") };
            string json =
                "{\"answer\":\"Contoso CLI 4.1.0 shipped on 12 March 2024.\"," +
                "\"insufficientEvidence\":false,\"citations\":[{" +
                "\"sourceId\":\"" + id + "\",\"page\":1," +
                "\"quote\":\"version 4.1.0 shipped on 12 March 2024\"}]}";
            var chat = new SequenceChat(json);
            AiDocumentCollection ai = await CollectionAsync(chunks, chat);

            GroundedAnswer answer = await ai.AskDetailedAsync("Which Contoso CLI version shipped on 12 March 2024?");

            Assert.Equal(1, chat.Calls);
            Assert.True(answer.CitationsValid);
            Assert.False(answer.InsufficientEvidence);
            Assert.Equal("Contoso CLI 4.1.0 shipped on 12 March 2024.", answer.Text);
            Assert.Equal(id, answer.Citations[0].SourceId);
            Assert.Equal(1, answer.Citations[0].PageNumber);
        }

        [Fact]
        public async Task Ask_keeps_the_answer_when_citations_are_invalid()
        {
            const string id = "notes.pdf|p1|c0";
            var chunks = new List<AiChunk> { Chunk(id, "Contoso CLI version 4.1.0 shipped on 12 March 2024.", "notes.pdf") };
            string bad =
                "{\"answer\":\"4.1.0\",\"citations\":[{\"sourceId\":\"invented\",\"page\":99,\"quote\":\"4.1.0\"}]}";
            var chat = new SequenceChat(bad);
            AiDocumentCollection ai = await CollectionAsync(chunks, chat);

            GroundedAnswer answer = await ai.AskDetailedAsync("Which Contoso CLI version shipped?");

            Assert.Equal(1, chat.Calls);
            Assert.False(answer.InsufficientEvidence);
            Assert.False(answer.CitationsValid);
            Assert.Equal("4.1.0", answer.Text);
            Assert.NotNull(answer.Warning);
        }

        static ParsedModelAnswer Parse(string json)
        {
            ParsedModelAnswer parsed = AnswerParser.TryParse(json);
            Assert.NotNull(parsed);
            return parsed;
        }

        static RetrievedPassage Passage(string id, string file, int page, string text) => new()
        {
            SourceId = id,
            SourceFileName = file,
            SourceFilePath = file,
            PageNumber = page,
            Text = text,
        };

        static AiChunk Chunk(string id, string text, string file) => new()
        {
            Id = id,
            Text = text,
            SourceFilePath = file,
            SourceFileName = file,
            PageNumber = 1,
        };

        static async Task<AiDocumentCollection> CollectionAsync(List<AiChunk> chunks, IChatClient chat)
        {
            var embedder = new HashEmbeddingGenerator(128);
            await EmbeddingIndexer.EmbedAsync(chunks, embedder, batchSize: 4);
            var index = new InMemoryVectorIndex();
            await index.IndexAsync(chunks);
            return new AiDocumentCollection(index, embedder, chat, chunks, defaultTopK: 8, askExcerptCount: 1);
        }

        sealed class SequenceChat : IChatClient
        {
            readonly string[] _replies;
            public SequenceChat(params string[] replies) => _replies = replies;
            public int Calls { get; private set; }
            public ChatClientMetadata Metadata { get; } = new("seq");

            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions options = null,
                CancellationToken cancellationToken = default)
            {
                string reply = _replies[Math.Min(Calls, _replies.Length - 1)];
                Calls++;
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
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
