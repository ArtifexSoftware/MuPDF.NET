using System;
using System.IO;
using MuPDF.NET;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class DisposePatternTest
    {
        private const string TestClassName = nameof(DisposePatternTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);
        private string TocPath = Doc("toc.pdf");

        private static void AssertDisposeDoesNotThrow(Action dispose)
        {
            Assert.Null(Record.Exception(dispose));
        }

        [Fact]
        public void Document_Dispose_MultipleTimes_DoesNotThrow()
        {
            var doc = new Document(TocPath);

            doc.Dispose();

            AssertDisposeDoesNotThrow(() => doc.Dispose());
        }

        [Fact]
        public void Page_Dispose_MultipleTimes_DoesNotThrow()
        {
            var doc = new Document(TocPath);
            var page = doc[0];

            page.Dispose();

            AssertDisposeDoesNotThrow(() => page.Dispose());

            doc.Dispose();
        }

        [Fact]
        public void TextPage_Dispose_MultipleTimes_DoesNotThrow()
        {
            var doc = new Document(TocPath);
            var page = doc[0];
            var textPage = page.GetTextPage();

            textPage.Dispose();

            AssertDisposeDoesNotThrow(() => textPage.Dispose());

            page.Dispose();
            doc.Dispose();
        }

        [Fact]
        public void Story_Dispose_MultipleTimes_DoesNotThrow()
        {
            var story = new Story("<p>Hello</p>");

            story.Dispose();

            AssertDisposeDoesNotThrow(() => story.Dispose());
        }

        [Fact]
        public void DisplayList_Dispose_MultipleTimes_DoesNotThrow()
        {
            var rect = new Rect(0, 0, 100, 100);
            var dl = new DisplayList(rect);

            dl.Dispose();

            AssertDisposeDoesNotThrow(() => dl.Dispose());
        }

        [Fact]
        public void DocumentWriter_Dispose_MultipleTimes_DoesNotThrow()
        {
            string path = Path.GetTempFileName();

            try
            {
                var writer = new DocumentWriter(path);

                writer.Dispose();

                AssertDisposeDoesNotThrow(() => writer.Dispose());
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void Font_Dispose_MultipleTimes_DoesNotThrow()
        {
            var font = new Font();

            font.Dispose();

            AssertDisposeDoesNotThrow(() => font.Dispose());
        }

        [Fact]
        public void GraftMap_Dispose_MultipleTimes_DoesNotThrow()
        {
            var doc = new Document(TocPath);
            var map = new GraftMap(doc);

            map.Dispose();

            AssertDisposeDoesNotThrow(() => map.Dispose());

            doc.Dispose();
        }

        // Outline is constructed internally from native MuPDF outline structures and
        // not exposed as a public constructor. Its disposal semantics are exercised
        // indirectly via Document/Document.GetToc tests, so we skip a direct idempotency test.
    }
}
