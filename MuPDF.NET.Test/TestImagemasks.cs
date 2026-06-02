using System;
using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Port of <c>PyMuPDF-1.27.2.2/tests/test_imagemasks.py</c>.
    /// </summary>
    /// <remarks>
    /// Confirm image mask detection in TextPage extractions.
    /// Inputs: <c>TestDocuments/TestImagemasks/</c>; outputs: <c>TestDocuments/_Output/TestImagemasks/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestImagemasks
    {
        private const string TestClassName = nameof(TestImagemasks);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static Dictionary<string, object> FirstImageBlock(Page page)
        {
            var pageDict = page.GetText("dict") as Dictionary<string, object>
                ?? throw new InvalidOperationException("get_text(dict) did not return a dictionary");
            var blocks = pageDict["blocks"] as List<Dictionary<string, object>>
                ?? throw new InvalidOperationException("blocks missing from dict text");
            if (blocks.Count == 0)
                throw new InvalidOperationException("no text blocks on page");
            return blocks[0];
        }

        /// <summary>PyMuPDF <c>tests/test_imagemasks.py::test_imagemask1</c>.</summary>
        [Fact]
        public void test_imagemask1()
        {
            using var doc = new Document(Doc("img-regular.pdf"));
            var page = doc[0];

            var img = FirstImageBlock(page);
            Assert.Null(img["mask"]);

            var imgInfo = page.GetImageInfoDict()[0];
            Assert.False(Convert.ToBoolean(imgInfo["has-mask"]));
        }

        /// <summary>PyMuPDF <c>tests/test_imagemasks.py::test_imagemask2</c>.</summary>
        [Fact]
        public void test_imagemask2()
        {
            using var doc = new Document(Doc("img-transparent.pdf"));
            var page = doc[0];

            var img = FirstImageBlock(page);
            Assert.IsType<byte[]>(img["mask"]);

            var imgInfo = page.GetImageInfoDict()[0];
            Assert.True(Convert.ToBoolean(imgInfo["has-mask"]));
        }
    }
}
