using System;
using System.IO;
using NUnit.Framework;
using MuPDF.NET;

namespace MuPDF.NET.Test
{
    public class DisposePatternTest
    {
        private const string TocPath = "../../../resources/toc.pdf";

        [Test]
        public void Document_Dispose_MultipleTimes_DoesNotThrow()
        {
            var doc = new Document(TocPath);

            doc.Dispose();

            Assert.DoesNotThrow(() => doc.Dispose());
        }

        [Test]
        public void Page_Dispose_MultipleTimes_DoesNotThrow()
        {
            var doc = new Document(TocPath);
            var page = doc[0];

            page.Dispose();

            Assert.DoesNotThrow(() => page.Dispose());

            doc.Dispose();
        }

        [Test]
        public void TextPage_Dispose_MultipleTimes_DoesNotThrow()
        {
            var doc = new Document(TocPath);
            var page = doc[0];
            var textPage = page.GetTextPage();

            textPage.Dispose();

            Assert.DoesNotThrow(() => textPage.Dispose());

            page.Dispose();
            doc.Dispose();
        }

        [Test]
        public void Story_Dispose_MultipleTimes_DoesNotThrow()
        {
            var story = new Story("<p>Hello</p>");

            story.Dispose();

            Assert.DoesNotThrow(() => story.Dispose());
        }

        [Test]
        public void DisplayList_Dispose_MultipleTimes_DoesNotThrow()
        {
            var rect = new Rect(0, 0, 100, 100);
            var dl = new DisplayList(rect);

            dl.Dispose();

            Assert.DoesNotThrow(() => dl.Dispose());
        }

        [Test]
        public void DocumentWriter_Dispose_MultipleTimes_DoesNotThrow()
        {
            string path = Path.GetTempFileName();

            try
            {
                var writer = new DocumentWriter(path);

                writer.Dispose();

                Assert.DoesNotThrow(() => writer.Dispose());
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void Font_Dispose_MultipleTimes_DoesNotThrow()
        {
            var font = new Font();

            font.Dispose();

            Assert.DoesNotThrow(() => font.Dispose());
        }

        [Test]
        public void GraftMap_Dispose_MultipleTimes_DoesNotThrow()
        {
            var doc = new Document(TocPath);
            var map = new GraftMap(doc);

            map.Dispose();

            Assert.DoesNotThrow(() => map.Dispose());

            doc.Dispose();
        }

        // Outline is constructed internally from native MuPDF outline structures and
        // not exposed as a public constructor. Its disposal semantics are exercised
        // indirectly via Document/Document.GetToc tests, so we skip a direct idempotency test.
    }
}

