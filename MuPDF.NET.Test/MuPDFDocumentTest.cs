using System;
using System.Collections.Generic;
using System.Data.Common;
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

            Assert.AreEqual(doc.Len, oldLen + 1);
        }

        [Test]
        public void CopyPage()
        {
            int oldLen = doc.Len;
            doc.CopyPage(2);

            Assert.AreEqual(doc.Len, oldLen + 1);
        }

        [Test]
        public void DeletePage()
        {
            int oldLen = doc.Len;
            doc.DeletePage(1);
            Assert.AreEqual(doc.Len, oldLen - 1);
        }

        [Test]
        public void DeletePages()
        {
            int oldLen = doc.Len;

            doc.DeletePages(1, 2); // delete one page

            doc.DeletePages(new int[] { 2, 3 }); // delete 2 pages

            Assert.AreEqual(doc.Len, oldLen - 3);
        }

        [Test]
        public void XmlMetadata()
        {
            doc.DeleteXmlMetadata();

            doc.Save("output.pdf");

            MuPDFDocument updated = new MuPDFDocument("output.pdf");

            Assert.AreEqual(updated.GetXmlMetadata(), "");
        }

        [Test]
        public void GetXrefLen()
        {
            doc.GetXrefLength();
            Assert.Pass();
        }

        [Test]
        public void GetPageImage_ExtractImage()
        {
            int n = doc.GetPageImages(0).Count;

            Assert.AreEqual(n, 1); // in case of current input pdf, if other file, real count should be fixed

            n = doc.ExtractImage(doc.GetPageImages(0)[0].Xref).Image.Length;

            Assert.Pass();
        }

    }
}
