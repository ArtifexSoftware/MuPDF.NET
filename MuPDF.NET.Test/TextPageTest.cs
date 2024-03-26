using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class TextPageTest : PageTestBase
    {
        [SetUp]
        public void Setup()
        {
            doc = new MuPDFDocument("input.pdf");

            textPage = doc.LoadPage(0).GetSTextPage();
        }

        [Test]
        public void Test_Constructor()
        {
            textPage = new MuPDFSTextPage(new mupdf.FzRect());
            Assert.Pass();

            textPage = new MuPDFSTextPage(doc.LoadPage(0).GetSTextPage());
            Assert.Pass();
        }

        [Test]
        public void Test_ExtractHtml()
        {
            textPage.ExtractHtml();
            Assert.Pass();
        }

        [Test]
        public void Test_ExtractText()
        {
            textPage.ExtractText();
            Assert.Pass();
        }

        [Test]
        public void Test_ExtractXml()
        {
            textPage.ExtractXML();
            Assert.Pass();
        }

        [Test]
        public void Test_ExtractBlocks()
        {
            List<TextBlock> blocks = textPage.ExtractBlocks();
            Assert.NotZero(blocks.Count);
        }

        [Test]
        public void Test_ExtractXHtml()
        {
            textPage.ExtractXHtml();
            Assert.Pass();
        }

        [Test]
        public void Test_ExtractDict()
        {
            textPage.ExtractDict(new Rect(0, 0, 300, 300));
            Assert.Pass();
        }

        [Test]
        public void Test_ExtractJson()
        {
            textPage.ExtractJSON(new Rect(0, 0, 300, 300));
            Assert.Pass();
        }

        [Test]
        public void Test_ExtractRAWDict()
        {
            textPage.ExtractRAWDict(new Rect(0, 0, 300, 300));
            Assert.Pass();

            textPage.ExtractRAWDict(null);
            Assert.Pass();
        }

        [Test]
        public void Test_ExtractSelection()
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
