using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class MuPDFTextPageTest : PageTestBase
    {
        [SetUp]
        public void Setup()
        {
            doc = new MuPDFDocument("input.pdf");

            textPage = doc.LoadPage(0).GetSTextPage();
        }

        [Test]
        public void Constructor()
        {
            textPage = new MuPDFSTextPage(new mupdf.FzRect());
            Assert.Pass();

            textPage = new MuPDFSTextPage(doc.LoadPage(0).GetSTextPage());
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
            Assert.NotZero(blocks.Count);
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
        public void ExtractJson()
        {
            textPage.ExtractJSON(new Rect(0, 0, 300, 300));
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
            Console.WriteLine(ret);
            Assert.Pass();
        }
    }
}
