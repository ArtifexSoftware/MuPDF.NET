using System;
using System.Threading.Tasks;
using MuPDF.NET.PDF4LLM.AI;
using MuPDF.NET.PDF4LLM.AI.Evaluation;
using MuPDF.NET.PDF4LLM.AI.Services;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
    public class TestAiEvaluation
    {
        [Fact]
        public async Task Dataset_meets_recall_abstention_and_claim_checks()
        {
            var embedder = new HashEmbeddingGenerator(128);
            await EmbeddingIndexer.EmbedAsync(DefaultQaDataset.Chunks, embedder, batchSize: 4);
            var index = new InMemoryVectorIndex();
            await index.IndexAsync(DefaultQaDataset.Chunks);
            var chat = new QaEvaluator.EvidenceOracleChat(DefaultQaDataset.Cases);
            var docs = new AiDocumentCollection(
                index, embedder, chat, DefaultQaDataset.Chunks, defaultTopK: 8, askExcerptCount: 2);

            QaReport report = await QaEvaluator.RunAsync(docs, DefaultQaDataset.Cases);

            Assert.Equal(DefaultQaDataset.Cases.Count, report.Total);
            Assert.Equal(report.Total, report.Passed);
            Assert.Equal(1, report.RetrievalRecall);
            Assert.Equal(1, report.AbstentionAccuracy);
            Assert.Equal(1, report.ClaimAccuracy);
        }

        [Fact]
        public void Live_ollama_eval_is_opt_in()
        {
            string? flag = Environment.GetEnvironmentVariable("PDF4LLM_OLLAMA_EVAL");
            if (!string.Equals(flag, "1", StringComparison.Ordinal))
                return;

            Assert.Fail("PDF4LLM_OLLAMA_EVAL=1 is reserved for a live Ollama run; wire it in Demo, not the default test suite.");
        }
    }
}
