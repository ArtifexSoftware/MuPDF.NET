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
            doc = new Document("../../../resources/toc.pdf");
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
            doc.CopyPage(0);

            Assert.AreEqual(doc.PageCount, oldLen + 1);
        }

        [Test]
        public void DeletePage()
        {
            int oldLen = doc.PageCount;
            doc.DeletePage(0);
            Assert.AreEqual(doc.PageCount, oldLen - 1);
        }

        [Test]
        public void DeletePages()
        {
            int oldLen = doc.PageCount;

            doc.DeletePages(0); // delete one page

            doc.DeletePages(new int[] { 0, }); // delete 2 pages

            Assert.AreEqual(doc.PageCount, oldLen - 1);
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

            Assert.That(n, Is.EqualTo(15)); // in case of current input pdf, if other file, real count should be fixed

            n = doc.ExtractImage(doc.GetPageImages(0)[0].Xref).Image.Length;

            Assert.Pass();
        }

        [Test]
        public void GetToc()
        {
            Document doc = new Document("../../../resources/toc.pdf");
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
            Document doc = new Document();
            byte[] buffer = Encoding.UTF8.GetBytes("123456678790qwexcvnmhofbnmfsdg4589754uiofjkb-");
            doc.AddEmbfile("file1", buffer, "testfile.txt", "testfile-u.txt", "Description of some sort");
        }

        [Test]
        public void Test_IsNoPDF()
        {
            Document doc = new Document("../../../resources/Bezier.epub");
            Assert.That(doc.IsPDF, Is.False);
        }

        [Test]
        public void Test_PageIds()
        {
            Document doc = new Document("../../../resources/Bezier.epub");

            Assert.That(doc.ChapterCount, Is.EqualTo(7));
            Assert.That(doc.LastLocation.Item1, Is.EqualTo(6));
        }

        [Test]
        public void OC1()
        {
            Document doc = new Document();
            int ocg1 = doc.AddOcg("ocg1");
            int ocg2 = doc.AddOcg("ocg2");
            doc.SetOCMD(xref: 0, ocgs: new int[] { ocg1, ocg2 });
            doc.SetLayer(-1);
            doc.AddLayer("layer1");

            doc.GetLayer();
            doc.GetLayers();
            doc.GetOcgs();
            doc.LayerUIConfigs();
            doc.SwitchLayer(0);
        }
    }
}
