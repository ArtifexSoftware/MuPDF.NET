using System;
using System.IO;
using Xunit;
using MuPDF.NET;
namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET.layout")]
    public class TestDocumentLlm
    {
        [Fact]
        public void to_chunks_requires_layout()
        {
            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(false);
                using var doc = new Document();
                Assert.Throws<NotSupportedException>(() => doc.ToChunks());
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }

        [Fact]
        public void document_to_markdown_legacy_path()
        {
            string path = _Path.ForTestClass("test_natural.pdf", nameof(TestPixmap));
            Assert.True(File.Exists(path));
            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(false);
                using var doc = new Document(path);
                string md = doc.ToMarkdown();
                Assert.False(string.IsNullOrEmpty(md));
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }

        [Fact]
        public void to_markdown_layout_when_available()
        {
            if (!Utils.LayoutPythonAvailable)
                return;
            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(true);
                string path = _Path.ForTestClass("test_natural.pdf", nameof(TestPixmap));
                using var doc = new Document(path);
                string md = doc.ToMarkdown(useOcr: false);
                Assert.False(string.IsNullOrWhiteSpace(md));
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }

        [Fact]
        public void to_chunks_layout_when_available()
        {
            if (!Utils.LayoutPythonAvailable)
                return;
            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(true);
                string path = _Path.ForTestClass("test_natural.pdf", nameof(TestPixmap));
                using var doc = new Document(path);
                var chunks = doc.ToChunks(useOcr: false, maxTokens: 200, minTokens: 40);
                Assert.NotNull(chunks);
                Assert.NotNull(chunks.Diagnostics);
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }

        [Fact]
        public void utils_use_layout_toggles()
        {
            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(false);
                Assert.False(Utils.LayoutEnabled);
                Utils.UseLayout(true);
                Assert.True(Utils.LayoutEnabled);
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }
    }
}
