using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MuPDF.NET.PDF4LLM.AI;
using MuPDF.NET.PDF4LLM.AI.Abstractions;
using MuPDF.NET.PDF4LLM.AI.Models;
using MuPDF.NET.PDF4LLM.AI.Options;
using MuPDF.NET.PDF4LLM.AI.Services;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
    public class TestAiPersistence
    {
        [Fact]
        public void Validate_rejects_out_of_range_options()
        {
            var options = new MicrosoftAIConnectorOptions { DefaultTopK = 0 };
            Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        }

        [Fact]
        public void Validate_rejects_partial_azure_settings()
        {
            var options = new MicrosoftAIConnectorOptions { AzureSearchEndpoint = "https://example.search.windows.net" };
            Assert.Throws<InvalidOperationException>(() => options.Validate());
        }

        [Fact]
        public async Task File_index_round_trips_chunks()
        {
            string path = Path.Combine(Path.GetTempPath(), "pdf4llm-ai-" + Guid.NewGuid().ToString("n") + ".json");
            var chunks = new List<AiChunk>
            {
                new()
                {
                    Id = "a.pdf|p1|c0",
                    Text = "Protocol ACME-1. Amount due 1200 EUR.",
                    SourceFilePath = "a.pdf",
                    SourceFileName = "a.pdf",
                    PageNumber = 1,
                    Embedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f },
                },
            };

            try
            {
                var first = new FileVectorIndex(path);
                await first.IndexAsync(chunks);
                var second = new FileVectorIndex(path);
                Assert.Equal("a.pdf", second.Snapshot().Single().SourceFileName);
                Assert.Contains("1200 EUR", second.Snapshot().Single().Text);

                await second.RemoveBySourceAsync("a.pdf");
                Assert.Empty(new FileVectorIndex(path).Snapshot());
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public async Task Collection_can_add_and_remove_a_document()
        {
            var notes = new ExtractedPage
            {
                SourceFilePath = "notes.pdf",
                SourceFileName = "notes.pdf",
                PageNumber = 1,
                Text = "Contoso CLI version 4.1.0 shipped on 12 March 2024.",
            };
            var stock = new ExtractedPage
            {
                SourceFilePath = "stock.pdf",
                SourceFileName = "stock.pdf",
                PageNumber = 1,
                Text = "SKU W-9. Shelf life 18 months.",
            };

            var options = MicrosoftAIConnectorOptions.CreateForDevelopment();
            options.DocumentExtractor = new MapExtractor(notes);
            AiDocumentCollection docs = await MsAIConnector.LoadAsync(new[] { "notes.pdf" }, options);
            Assert.Contains("notes.pdf", docs.SourceFiles);

            options.DocumentExtractor = new MapExtractor(stock);
            await docs.AddDocumentsAsync(new[] { "stock.pdf" });
            Assert.Contains("stock.pdf", docs.SourceFiles);
            Assert.Contains("notes.pdf", docs.SourceFiles);

            await docs.RemoveDocumentAsync("notes.pdf");
            Assert.DoesNotContain("notes.pdf", docs.SourceFiles);
            Assert.Contains("stock.pdf", docs.SourceFiles);
        }

        [Fact]
        public async Task Load_honors_cancellation()
        {
            var options = MicrosoftAIConnectorOptions.CreateForDevelopment();
            options.DocumentExtractor = new MapExtractor(new ExtractedPage
            {
                SourceFilePath = "n.pdf",
                SourceFileName = "n.pdf",
                PageNumber = 1,
                Text = "x",
            });
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                MsAIConnector.LoadAsync(new[] { "n.pdf" }, options, cts.Token));
        }

        sealed class MapExtractor : IDocumentExtractor
        {
            readonly ExtractedPage _page;
            public MapExtractor(ExtractedPage page) => _page = page;
            public IReadOnlyList<ExtractedPage> Extract(string pdfPath, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new[] { _page };
            }
        }
    }
}
