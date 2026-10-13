using System.Collections.Generic;
using System.Linq;
using MuPDF.NET.PDF4LLM.AI.Models;
using MuPDF.NET.PDF4LLM.AI.Services;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
    public class TestAiChunking
    {
        [Fact]
        public void Empty_input_yields_no_chunks()
        {
            Assert.Empty(TextChunker.SplitPage("", 400, 40));
            Assert.Empty(TextChunker.Chunk(System.Array.Empty<ExtractedPage>(), 400, 40));
            Assert.Empty(TextChunker.Chunk(new[]
            {
                new ExtractedPage
                {
                    SourceFilePath = "blank.pdf",
                    SourceFileName = "blank.pdf",
                    PageNumber = 1,
                    Text = "   \n\n",
                },
            }, 400, 40));
        }

        [Fact]
        public void Long_prose_splits_and_keeps_overlap_without_duplicate_slices()
        {
            string page = string.Join(" ", Enumerable.Range(0, 80).Select(i => $"Sentence {i} ends here."));
            List<ChunkPart> parts = TextChunker.SplitPage(page, 180, 30);
            Assert.True(parts.Count >= 3, $"got {parts.Count}");
            Assert.Contains(parts, p => p.Text.Contains("Sentence 0"));
            Assert.Contains(parts, p => p.Text.Contains("Sentence 79"));
            Assert.Equal(parts.Count, parts.Select(p => p.Text).Distinct().Count());
        }

        [Fact]
        public void Chunk_metadata_keeps_file_page_heading_and_stable_ids()
        {
            var pages = new List<ExtractedPage>
            {
                new()
                {
                    SourceFilePath = @"C:\docs\notes.pdf",
                    SourceFileName = "notes.pdf",
                    PageNumber = 2,
                    Text = "# Release notes\nProduct Contoso CLI version 4.1.0 shipped on 12 March 2024.",
                    Metadata = new Dictionary<string, object> { ["file_path"] = @"C:\docs\notes.pdf" },
                },
            };

            List<AiChunk> first = TextChunker.Chunk(pages, 400, 40);
            List<AiChunk> second = TextChunker.Chunk(pages, 400, 40);

            Assert.NotEmpty(first);
            Assert.Equal("notes.pdf", first[0].SourceFileName);
            Assert.Equal(2, first[0].PageNumber);
            Assert.Equal("Release notes", first[0].Metadata["heading"]);
            Assert.Equal(first.Select(c => c.Id), second.Select(c => c.Id));
            Assert.Equal("notes.pdf|p2|c0", first[0].Id);
        }

        [Fact]
        public void Lexical_matches_versions_dates_and_identifiers()
        {
            const string passage =
                "Contoso CLI version 4.1.0 shipped on 12 March 2024. SKU W-9 shelf life 18 months.";
            Assert.True(LexicalScore.Score("version 4.1.0", passage) > 0.5);
            Assert.True(LexicalScore.Score("12 March 2024", passage) > 0.5);
            Assert.True(LexicalScore.Score("SKU W-9", passage) > 0.5);
            Assert.True(LexicalScore.Score("version 4.1.0", passage) >
                        LexicalScore.Score("version 4.1.0", "The warehouse is in Lyon."));
        }

        [Fact]
        public void Hybrid_score_prefers_identifier_overlap_over_vector_only()
        {
            double identified = LexicalScore.Combine(lexical: 1, vector: 0.1);
            double semanticOnly = LexicalScore.Combine(lexical: 0, vector: 0.9);
            Assert.True(identified > semanticOnly, $"id={identified} vec={semanticOnly}");
        }
    }
}
