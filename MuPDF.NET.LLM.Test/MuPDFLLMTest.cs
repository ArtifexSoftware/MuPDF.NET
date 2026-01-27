using System;
using System.Collections.Generic;
using MuPDF.NET;
using MuPDF.NET.LLM;
using MuPDF.NET.LLM.Llama;

namespace MuPDF.NET.LLM.Test
{
    [TestFixture]
    public class MuPDFLLMTest : LLMTestBase
    {
        [Test]
        public void Version_ReturnsValidVersion()
        {
            string version = MuPDFLLM.Version;
            Assert.That(version, Is.Not.Null);
            Assert.That(version, Is.Not.Empty);
            Assert.That(version.Split('.').Length, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void VersionTuple_ReturnsValidTuple()
        {
            var (major, minor, patch) = MuPDFLLM.VersionTuple;
            Assert.That(major, Is.GreaterThanOrEqualTo(0));
            Assert.That(minor, Is.GreaterThanOrEqualTo(0));
            Assert.That(patch, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void LlamaMarkdownReader_ReturnsReader()
        {
            var reader = MuPDFLLM.LlamaMarkdownReader();
            Assert.That(reader, Is.Not.Null);
            Assert.That(reader, Is.InstanceOf<PDFMarkdownReader>());
        }

        [Test]
        public void LlamaMarkdownReader_WithMetaFilter_ReturnsReader()
        {
            Func<Dictionary<string, object>, Dictionary<string, object>> filter = 
                (meta) => { meta["custom"] = "value"; return meta; };
            
            var reader = MuPDFLLM.LlamaMarkdownReader(filter);
            Assert.That(reader, Is.Not.Null);
            Assert.That(reader.MetaFilter, Is.EqualTo(filter));
        }

        [Test]
        public void ToMarkdown_WithValidDocument_ReturnsMarkdown()
        {
            var doc = OpenTestDocument("Magazine.pdf");
            try
            {
                string markdown = MuPDFLLM.ToMarkdown(
                    doc,
                    header: false,
                    footer: false,
                    showProgress: false,
                    useOcr: false
                );

                Assert.That(markdown, Is.Not.Null);
                Assert.That(markdown, Is.Not.Empty);
            }
            finally
            {
                doc.Close();
            }
        }

        [Test]
        public void ToMarkdown_WithSpecificPages_ReturnsMarkdown()
        {
            var doc = OpenTestDocument("Magazine.pdf");
            try
            {
                string markdown = MuPDFLLM.ToMarkdown(
                    doc,
                    pages: new List<int> { 0 },
                    header: false,
                    footer: false,
                    showProgress: false,
                    useOcr: false
                );

                Assert.That(markdown, Is.Not.Null);
            }
            finally
            {
                doc.Close();
            }
        }

        [Test]
        public void ToMarkdown_WriteImagesAndEmbedImages_ThrowsException()
        {
            var doc = OpenTestDocument("columns.pdf");
            try
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    MuPDFLLM.ToMarkdown(
                        doc,
                        writeImages: true,
                        embedImages: true,
                        showProgress: false,
                        useOcr: false
                    );
                });
            }
            finally
            {
                doc.Close();
            }
        }

        [Test]
        public void ToJson_WithValidDocument_ReturnsJson()
        {
            var doc = OpenTestDocument("columns.pdf");
            try
            {
                string json = MuPDFLLM.ToJson(
                    doc,
                    showProgress: false,
                    useOcr: false
                );

                Assert.That(json, Is.Not.Null);
                Assert.That(json, Is.Not.Empty);
                Assert.That(json.TrimStart(), Does.StartWith("{"));
            }
            finally
            {
                doc.Close();
            }
        }

        [Test]
        public void ToText_WithValidDocument_ReturnsText()
        {
            var doc = OpenTestDocument("columns.pdf");
            try
            {
                string text = MuPDFLLM.ToText(
                    doc,
                    header: false,
                    footer: false,
                    showProgress: false,
                    useOcr: false
                );

                Assert.That(text, Is.Not.Null);
                Assert.That(text, Is.Not.Empty);
            }
            finally
            {
                doc.Close();
            }
        }

        [Test]
        public void ParseDocument_WithValidDocument_ReturnsParsedDocument()
        {
            var doc = OpenTestDocument("columns.pdf");
            try
            {
                var parsedDoc = MuPDFLLM.ParseDocument(
                    doc,
                    showProgress: false,
                    useOcr: false
                );

                Assert.That(parsedDoc, Is.Not.Null);
            }
            finally
            {
                doc.Close();
            }
        }

        [Test]
        public void GetKeyValues_WithNonFormPDF_ReturnsEmptyDictionary()
        {
            var doc = OpenTestDocument("Magazine.pdf");
            try
            {
                var keyValues = MuPDFLLM.GetKeyValues(doc);
                Assert.That(keyValues, Is.Not.Null);
                Assert.That(keyValues, Is.Empty);
            }
            finally
            {
                doc.Close();
            }
        }
    }
}
