using System;
using System.Collections.Generic;
using MuPDF.NET;
using MuPDF.NET.LLM.Helpers;
using NUnit.Framework;

namespace MuPDF.NET.LLM.Test
{
    [TestFixture]
    public class MuPdfRagTest : LLMTestBase
    {
        [Test]
        public void ToMarkdown_BasicWithDefaultSettings_ReturnsMarkdown()
        {
            var doc = OpenTestDocument("national-capitals.pdf");
            try
            {
                string markdown = MuPdfRag.ToMarkdown(
                    doc,
                    pages: null, // All pages
                    hdrInfo: null, // Auto-detect headers
                    writeImages: false,
                    embedImages: false,
                    ignoreImages: false,
                    ignoreGraphics: false,
                    detectBgColor: true,
                    imagePath: "",
                    imageFormat: "png",
                    imageSizeLimit: 0.05f,
                    filename: GetResourcePath("national-capitals.pdf"),
                    forceText: true,
                    pageChunks: false,
                    pageSeparators: false,
                    margins: null,
                    dpi: 150,
                    pageWidth: 612,
                    pageHeight: null,
                    tableStrategy: "lines_strict",
                    graphicsLimit: null,
                    fontsizeLimit: 3.0f,
                    ignoreCode: false,
                    extractWords: false,
                    showProgress: false,
                    useGlyphs: false,
                    ignoreAlpha: false
                );

                Assert.That(markdown, Is.Not.Null);
                Assert.That(markdown, Is.Not.Empty);
                Assert.That(markdown.Length, Is.GreaterThan(0));
            }
            finally
            {
                doc.Close();
            }
        }

        [Test]
        public void ToMarkdown_WithIdentifyHeaders_ReturnsMarkdown()
        {
            var doc = OpenTestDocument("national-capitals.pdf");
            try
            {
                var identifyHeaders = new IdentifyHeaders(doc, pages: null, bodyLimit: 12.0f, maxLevels: 6);
                
                string markdown = MuPdfRag.ToMarkdown(
                    doc,
                    pages: new List<int> { 0 }, // First page only
                    hdrInfo: identifyHeaders,
                    writeImages: false,
                    embedImages: false,
                    ignoreImages: false,
                    filename: GetResourcePath("national-capitals.pdf"),
                    forceText: true,
                    showProgress: false
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
        public void ToMarkdown_WithTocHeaders_ReturnsMarkdown()
        {
            var doc = OpenTestDocument("national-capitals.pdf");
            try
            {
                var tocHeaders = new TocHeaders(doc);
                
                string markdown = MuPdfRag.ToMarkdown(
                    doc,
                    pages: new List<int> { 0 }, // First page only
                    hdrInfo: tocHeaders,
                    writeImages: false,
                    embedImages: false,
                    ignoreImages: false,
                    filename: GetResourcePath("national-capitals.pdf"),
                    forceText: true,
                    showProgress: false
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
        public void ToMarkdown_WithPageSeparators_ReturnsMarkdown()
        {
            var doc = OpenTestDocument("national-capitals.pdf");
            try
            {
                string markdown = MuPdfRag.ToMarkdown(
                    doc,
                    pages: null, // All pages
                    hdrInfo: null,
                    writeImages: false,
                    embedImages: false,
                    ignoreImages: false,
                    filename: GetResourcePath("national-capitals.pdf"),
                    forceText: true,
                    pageSeparators: true, // Add page separators
                    showProgress: false
                );

                Assert.That(markdown, Is.Not.Null);
                Assert.That(markdown, Is.Not.Empty);
                
                // Verify page separators are present
                Assert.That(markdown, Does.Contain("--- end of page="));
            }
            finally
            {
                doc.Close();
            }
        }

        [Test]
        public void ToMarkdown_WithSpecificPages_ReturnsMarkdown()
        {
            var doc = OpenTestDocument("national-capitals.pdf");
            try
            {
                string markdown = MuPdfRag.ToMarkdown(
                    doc,
                    pages: new List<int> { 0, 1 }, // First two pages
                    hdrInfo: null,
                    writeImages: false,
                    embedImages: false,
                    ignoreImages: false,
                    filename: GetResourcePath("national-capitals.pdf"),
                    forceText: true,
                    showProgress: false
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
        public void ToMarkdown_WithInvalidImageSizeLimit_ThrowsException()
        {
            var doc = OpenTestDocument("national-capitals.pdf");
            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    MuPdfRag.ToMarkdown(
                        doc,
                        imageSizeLimit: 1.5f, // Invalid: >= 1
                        filename: GetResourcePath("national-capitals.pdf"),
                        forceText: true,
                        showProgress: false
                    );
                });
            }
            finally
            {
                doc.Close();
            }
        }

        [Test]
        public void ToMarkdown_WithMargins_ReturnsMarkdown()
        {
            var doc = OpenTestDocument("national-capitals.pdf");
            try
            {
                string markdown = MuPdfRag.ToMarkdown(
                    doc,
                    pages: new List<int> { 0 },
                    hdrInfo: null,
                    writeImages: false,
                    embedImages: false,
                    ignoreImages: false,
                    filename: GetResourcePath("national-capitals.pdf"),
                    forceText: true,
                    margins: new List<float> { 10.0f, 20.0f, 10.0f, 20.0f }, // left, top, right, bottom
                    showProgress: false
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
        public void ToMarkdown_WithInvalidMargins_ThrowsException()
        {
            var doc = OpenTestDocument("national-capitals.pdf");
            try
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    MuPdfRag.ToMarkdown(
                        doc,
                        margins: new List<float> { 10.0f, 20.0f, 30.0f }, // Invalid: 3 elements
                        filename: GetResourcePath("national-capitals.pdf"),
                        forceText: true,
                        showProgress: false
                    );
                });
            }
            finally
            {
                doc.Close();
            }
        }

        [Test]
        public void ToMarkdown_WithTableStrategy_ReturnsMarkdown()
        {
            var doc = OpenTestDocument("national-capitals.pdf");
            try
            {
                string markdown = MuPdfRag.ToMarkdown(
                    doc,
                    pages: new List<int> { 0 },
                    hdrInfo: null,
                    writeImages: false,
                    embedImages: false,
                    ignoreImages: false,
                    filename: GetResourcePath("national-capitals.pdf"),
                    forceText: true,
                    tableStrategy: "lines",
                    showProgress: false
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
        public void ToMarkdown_WithIgnoreImages_ReturnsMarkdown()
        {
            var doc = OpenTestDocument("national-capitals.pdf");
            try
            {
                string markdown = MuPdfRag.ToMarkdown(
                    doc,
                    pages: new List<int> { 0 },
                    hdrInfo: null,
                    writeImages: false,
                    embedImages: false,
                    ignoreImages: true,
                    filename: GetResourcePath("national-capitals.pdf"),
                    forceText: true,
                    showProgress: false
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
        public void ToMarkdown_WithIgnoreGraphics_ReturnsMarkdown()
        {
            var doc = OpenTestDocument("national-capitals.pdf");
            try
            {
                string markdown = MuPdfRag.ToMarkdown(
                    doc,
                    pages: new List<int> { 0 },
                    hdrInfo: null,
                    writeImages: false,
                    embedImages: false,
                    ignoreImages: false,
                    ignoreGraphics: true,
                    filename: GetResourcePath("national-capitals.pdf"),
                    forceText: true,
                    showProgress: false
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
        public void ToMarkdown_WithPageChunks_ReturnsJson()
        {
            var doc = OpenTestDocument("national-capitals.pdf");
            try
            {
                string result = MuPdfRag.ToMarkdown(
                    doc,
                    pages: new List<int> { 0 },
                    hdrInfo: null,
                    writeImages: false,
                    embedImages: false,
                    ignoreImages: false,
                    filename: GetResourcePath("national-capitals.pdf"),
                    forceText: true,
                    pageChunks: true,
                    showProgress: false
                );

                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.Not.Empty);
                // In page_chunks mode, result should be JSON or structured text
                Assert.That(result.Length, Is.GreaterThan(0));
            }
            finally
            {
                doc.Close();
            }
        }
    }
}
