using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Confirm image mask detection in TextPage extractions.
    /// </summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestImagemasks/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestImagemasks
    {
        private const string TestClassName = nameof(TestImagemasks);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        /// <summary>Regular image has no mask.</summary>
        [Fact]
        public void test_imagemask1()
        {
            using var doc = new Document(Doc("img-regular.pdf"));
            var page = doc[0];
            var blocks = (List<Dictionary<string, object>>)((Dictionary<string, object>)page.GetText("dict"))["blocks"];
            var img = blocks[0];
            Assert.Null(img["mask"]);
            img = page.GetImageInfoDict()[0];
            Assert.False((bool)img["has-mask"]);
        }

        /// <summary>Transparent image reports a mask.</summary>
        [Fact]
        public void test_imagemask2()
        {
            using var doc = new Document(Doc("img-transparent.pdf"));
            var page = doc[0];
            var blocks = (List<Dictionary<string, object>>)((Dictionary<string, object>)page.GetText("dict"))["blocks"];
            var img = blocks[0];
            Assert.IsType<byte[]>(img["mask"]);
            img = page.GetImageInfoDict()[0];
            Assert.True((bool)img["has-mask"]);
        }
    }
}
