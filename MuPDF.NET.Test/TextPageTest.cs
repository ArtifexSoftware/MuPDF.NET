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
            doc = new MuPDFDocument("test.pdf");

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
    }
}
