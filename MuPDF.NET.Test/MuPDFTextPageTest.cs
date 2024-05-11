using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class MuPDFTextPageTest : PdfTestBase
    {
        [SetUp]
        public void Setup()
        {
            doc = new MuPDFDocument("../../../resources/cython.pdf");

            textPage = doc.LoadPage(0).GetTextPage();
        }

        [Test]
        public void Constructor()
        {
            textPage = new MuPDFTextPage(new mupdf.FzRect());
            Assert.Pass();

            textPage = new MuPDFTextPage(doc.LoadPage(0).GetTextPage());
            Assert.Pass();
        }

        [Test]
        public void ExtractHtml()
        {
            textPage.ExtractHtml();
            Assert.Pass();
        }

        [Test]
        public void ExtractText()
        {
            textPage.ExtractText();
            Assert.Pass();
        }

        [Test]
        public void ExtractXml()
        {
            textPage.ExtractXML();
            Assert.Pass();
        }

        [Test]
        public void ExtractBlocks()
        {
            List<TextBlock> blocks = textPage.ExtractBlocks();
            Assert.That(blocks.Count, Is.EqualTo(4));
        }

        [Test]
        public void ExtractXHtml()
        {
            textPage.ExtractXHtml();
            Assert.Pass();
        }

        [Test]
        public void ExtractDict()
        {
            textPage.ExtractDict(new Rect(0, 0, 300, 300));
            Assert.Pass();
        }

        [Test]
        public void ExtractRAWDict()
        {
            textPage.ExtractRAWDict(new Rect(0, 0, 300, 300));
            Assert.Pass();

            textPage.ExtractRAWDict(null);
            Assert.Pass();
        }

        [Test]
        public void ExtractSelection()
        {
            textPage.ExtractSelection(new mupdf.FzPoint(20, 20), new mupdf.FzPoint(100, 100));
            Assert.Pass();

            textPage.ExtractSelection(null, null);
            Assert.Pass();

            string ret = textPage.ExtractSelection(new mupdf.FzPoint(-5, -15), null);
            Assert.Pass();
        }

        [Test]
        public void SearchTest()
        {
            MuPDFDocument doc = new MuPDFDocument("input.pdf");

            MuPDFPage page = new MuPDFPage(doc.GetPage(0), doc);

            MuPDFTextPage tpage = page.GetTextPage();

            List<Quad> matches = MuPDFTextPage.Search(tpage, "pixmap");

            if (matches.Count > 0)
            {
                page.AddHighlightAnnot(matches);
            }
        }
    }
}
