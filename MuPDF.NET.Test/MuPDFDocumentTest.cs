using mupdf;
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
            int oldLen = doc.PageCount;
            doc.CopyFullPage(0);

            Assert.AreEqual(doc.PageCount, oldLen + 1);
        }

        [Test]
        public void CopyPage()
        {
            int oldLen = doc.PageCount;
            doc.CopyPage(2);

            Assert.AreEqual(doc.PageCount, oldLen + 1);
        }

        [Test]
        public void DeletePage()
        {
            int oldLen = doc.PageCount;
            doc.DeletePage(1);
            Assert.AreEqual(doc.PageCount, oldLen - 1);
        }

        [Test]
        public void DeletePages()
        {
            int oldLen = doc.PageCount;

            doc.DeletePages(1, 2); // delete one page

            doc.DeletePages(new int[] { 2, 3 }); // delete 2 pages

            Assert.AreEqual(doc.PageCount, oldLen - 3);
        }

        [Test]
        public void XmlMetadata()
        {
            doc.DeleteXmlMetadata();

            Assert.That(doc.GetXmlMetadata(), Is.EqualTo(""));
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

            Assert.That(n, Is.EqualTo(1)); // in case of current input pdf, if other file, real count should be fixed

            n = doc.ExtractImage(doc.GetPageImages(0)[0].Xref).Image.Length;

            Assert.Pass();
        }

        [Test]
        public void GetToc()
        {
            MuPDFDocument doc = new MuPDFDocument("resources/001003ED.pdf");
            doc.GetToc(true);
        }

        [Test]
        public void EraseToc()
        {
            doc.SetToc(null);
            Assert.That(doc.GetToc().Count, Is.EqualTo(0));
        }

        [Test]
        public void Embedded()
        {
            MuPDFDocument doc = new MuPDFDocument();
            byte[] buffer = Encoding.UTF8.GetBytes("123456678790qwexcvnmhofbnmfsdg4589754uiofjkb-");
            doc.AddEmbfile("file1", buffer, "testfile.txt", "testfile-u.txt", "Description of some sort");

            Console.WriteLine(doc.GetEmbfileCount());
        }

        [Test]
        public void Test_IsNoPDF()
        {
            MuPDFDocument doc = new MuPDFDocument("resources/Bezier.epub");
            Assert.That(doc.IsPDF, Is.False);
        }

        [Test]
        public void Test_PageIds()
        {
            MuPDFDocument doc = new MuPDFDocument("resources/Bezier.epub");

            Assert.That(doc.ChapterCount, Is.EqualTo(7));
            Assert.That(doc.LastLocation.Item1, Is.EqualTo(6));
        }
    }
}
