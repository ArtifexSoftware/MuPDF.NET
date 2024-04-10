using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class MuPDFDocumentTest : PdfTestBase
    {
        [SetUp]
        public void Setup()
        {
            doc = new MuPDFDocument("input.pdf");
        }

        [Test]
        public void CopyFullPage()
        {
            int oldLen = doc.Len;
            doc.CopyFullPage(0);
            doc.Save("output.pdf");

            Assert.AreEqual(doc.Len, oldLen + 1);
        }

        [Test]
        public void CopyPage()
        {
            int oldLen = doc.Len;
            doc.CopyPage(2);
            doc.Save("output.pdf");

            Assert.AreEqual(doc.Len, oldLen + 1);
        }
    }
}
