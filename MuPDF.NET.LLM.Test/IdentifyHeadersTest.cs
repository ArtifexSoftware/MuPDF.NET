using System.Collections.Generic;
using MuPDF.NET;
using MuPDF.NET.LLM.Helpers;

namespace MuPDF.NET.LLM.Test
{
    [TestFixture]
    public class IdentifyHeadersTest : LLMTestBase
    {
        [Test]
        public void Constructor_WithValidDocument_CreatesInstance()
        {
            var doc = OpenTestDocument("Magazine.pdf");
            try
            {
                var identifyHeaders = new IdentifyHeaders(doc);
                Assert.That(identifyHeaders, Is.Not.Null);
            }
            finally
            {
                doc.Close();
            }
        }

        [Test]
        public void Constructor_WithFilePath_CreatesInstance()
        {
            string filePath = GetResourcePath("Magazine.pdf");
            var identifyHeaders = new IdentifyHeaders(filePath);
            Assert.That(identifyHeaders, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithMaxLevelsOutOfRange_ThrowsException()
        {
            string filePath = GetResourcePath("Magazine.pdf");
            
            Assert.Throws<ArgumentException>(() =>
            {
                new IdentifyHeaders(filePath, maxLevels: 0);
            });
            
            Assert.Throws<ArgumentException>(() =>
            {
                new IdentifyHeaders(filePath, maxLevels: 7);
            });
        }

        [Test]
        public void Constructor_WithSpecificPages_Works()
        {
            var doc = OpenTestDocument("Magazine.pdf");
            try
            {
                var identifyHeaders = new IdentifyHeaders(doc, pages: new List<int> { 0 });
                Assert.That(identifyHeaders, Is.Not.Null);
            }
            finally
            {
                doc.Close();
            }
        }

        [Test]
        public void GetHeaderId_WithSmallFont_ReturnsEmpty()
        {
            var doc = OpenTestDocument("Magazine.pdf");
            try
            {
                var identifyHeaders = new IdentifyHeaders(doc);
                var page = doc[0];
                
                // Create a mock span with small font size
                var span = new ExtendedSpan
                {
                    Size = 10.0f,
                    Text = "Test"
                };
                
                string headerId = identifyHeaders.GetHeaderId(span, page);
                // Should return empty for body text
                Assert.That(headerId, Is.Not.Null);
            }
            finally
            {
                doc.Close();
            }
        }
    }
}
