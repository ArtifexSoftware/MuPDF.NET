using System;
using Xunit;
using MuPDF.NET;
namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class TestConfigurations
    {
        [Fact]
        public void test_configuration_layout()
        {
            if (Environment.GetEnvironmentVariable("MUPDF_NET_TEST_USE_LAYOUT") == "0" || !Utils.LayoutEnabled)
            {
                // xUnit 2.9 has no Assert.Skip. Same as the test: do not fail when layout is off.
                Console.WriteLine("not testing: layout feature not available (Utils.LayoutEnabled / PyMuPdfLayout)");
                return;
            }

            byte[] pdf;
            using (var built = new Document())
            {
                var page = built.NewPage();
                page.InsertText(new Point(72, 72), "Hallo Welt!\nSeid umschlungen, Millionen!");
                pdf = built.ToBytes();
            }

            string markdown1;
            string markdown2;
            using (var doc = new Document(pdf))
                markdown1 = doc.ToMarkdown();
            using (var doc = new Document(pdf))
                markdown2 = Utils.ToMarkdown(doc);
            Assert.Equal(markdown1, markdown2);
            Console.WriteLine(markdown1);

            string text1;
            string text2;
            using (var doc = new Document(pdf))
                text1 = doc.ToText();
            using (var doc = new Document(pdf))
                text2 = Utils.ToText(doc);
            Assert.Equal(text1, text2);
            Console.WriteLine(text1);

            string chunks1;
            string chunks2;
            using (var doc = new Document(pdf))
                chunks1 = doc.ToChunks().ToJson();
            using (var doc = new Document(pdf))
                chunks2 = Utils.ToChunks(doc).ToJson();
            Assert.Equal(chunks1, chunks2);
            Console.WriteLine(chunks1);

            string json1;
            string json2;
            using (var doc = new Document(pdf))
                json1 = doc.ToJson();
            using (var doc = new Document(pdf))
                json2 = Utils.ToJson(doc);
            Assert.Equal(json1, json2);
            Console.WriteLine(json1);
        }
    }
}
