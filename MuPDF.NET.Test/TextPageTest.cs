using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static System.Net.Mime.MediaTypeNames;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class TextPageTest : PdfTestBase
    {
        private const string TestClassName = nameof(TextPageTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        public TextPageTest()
        {
            doc = new Document(Doc("cython.pdf"));

            textPage = doc.LoadPage(0).GetTextPage();
        }

        [Fact]
        public void Constructor()
        {
            textPage = new TextPage(new mupdf.FzRect());
            Assert.Equal(0, textPage.Rect.Width);
            Assert.Equal(0, textPage.Rect.Height);
            //Assert.Pass();

            textPage = new TextPage(doc.LoadPage(0).GetTextPage());
            Assert.Equal(612, textPage.Rect.Width);
            Assert.Equal(792, textPage.Rect.Height);
            //Assert.Pass();
        }

        [Fact]
        public void ExtractHtml()
        {
            string txt = textPage.ExtractHtml();
            Assert.Equal(1114, txt.Length);
            //Assert.Pass();
        }

        [Fact]
        public void ExtractText()
        {
            string txt = textPage.ExtractText();
            Assert.Equal(164, txt.Length);
            //Assert.Pass();
        }

        [Fact]
        public void ExtractXml()
        {
            string txt = textPage.ExtractXML();
            Assert.Equal(28768, txt.Length);
            //Assert.Pass();
        }

        [Fact]
        public void ExtractBlocks()
        {
            List<TextBlock> blocks = textPage.ExtractBlocks();
            Assert.Equal(4, blocks.Count);
        }

        [Fact]
        public void ExtractXHtml()
        {
            string txt = textPage.ExtractXHtml();
            Assert.Equal(257, txt.Length);
            //Assert.Pass();
        }

        [Fact]
        public void ExtractDict()
        {
            var pageInfo = textPage.ExtractDict(new Rect(0, 0, 300, 300));
            Assert.Equal(4, pageInfo.Blocks.Count);
            //Assert.Pass();
        }

        [Fact]
        public void ExtractRAWDict()
        {
            var pageInfo1 = textPage.ExtractRAWDict(new Rect(0, 0, 300, 300));
            Assert.Equal(300, pageInfo1.Width);
            Assert.Equal(300, pageInfo1.Height);
            //Assert.Pass();

            var pageInfo2 = textPage.ExtractRAWDict(null);
            Assert.Equal(612, pageInfo2.Width);
            Assert.Equal(792, pageInfo2.Height);
            //Assert.Pass();
        }

        [Fact]
        public void ExtractSelection()
        {
            string t1 = textPage.ExtractSelection(new Point(20, 20), new Point(100, 100));
            Assert.Equal(0, t1.Length);
            //Assert.Pass();

            string t2 = textPage.ExtractSelection(null, null);
            Assert.Equal(0, t2.Length);
            //Assert.Pass();

            string t3 = textPage.ExtractSelection(new Point(-5, -15), null);
            Assert.Equal(0, t3.Length);
            //Assert.Pass();
        }

        [Fact]
        public void SearchTest()
        {
            //Document doc = new Document("input.pdf");

            Page page = doc[0];

            TextPage tpage = page.GetTextPage(page.Rect);

            List<Quad> matches = TextPage.Search(textPage, "2018");

            if (matches.Count > 0)
            {
                page.AddHighlightAnnot(matches);
            }

            Assert.Equal(PdfAnnotType.PDF_ANNOT_HIGHLIGHT, page.FirstAnnot.Type.Item1);
        }

        [Fact]
        public void GetText()
        {
            Document doc = new Document(Doc("test_3650.pdf"));
            List<TextBlock> blocks = doc[0].GetText("blocks");

            string t = "";
            foreach (TextBlock bt in blocks)
                t += bt.Text;

            Assert.Equal("RECUEIL DES ACTES ADMINISTRATIFS\nn° 78 du 28 avril 2023\n", t);
        }
    }
}
