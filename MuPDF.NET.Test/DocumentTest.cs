using mupdf;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class DocumentTest : PdfTestBase
    {
        private const string TestClassName = nameof(DocumentTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void CopyFullPage()
        {
            Document doc = new Document(Doc("toc.pdf"));
            int oldLen = doc.PageCount;
            doc.CopyFullPage(0);

            Assert.True(doc.PageCount == oldLen + 1);
            doc.Close();
        }

        [Fact]
        public void CopyPage()
        {
            Document doc = new Document(Doc("toc.pdf"));
            int oldLen = doc.PageCount;
            doc.CopyPage(0);

            Assert.True(doc.PageCount == oldLen + 1);
            doc.Close();
        }

        [Fact]
        public void ColorTest()
        {
            string testFilePath = Doc("Color.pdf");
            Document doc = new Document(testFilePath);
            List<Entry> images = doc.GetPageImages(0);
            Assert.True(images[0].CsName == "DeviceRGB");

            doc.Recolor(0, 4);
            images = doc.GetPageImages(0);
            Assert.True(images[0].CsName == "ICCBased");

            doc.Close();
        }

        [Fact]
        public void DeletePage()
        {
            Document doc = new Document(Doc("toc.pdf"));
            int oldLen = doc.PageCount;
            doc.DeletePage(0);
            Assert.True(doc.PageCount == oldLen - 1);
            doc.Close();
        }

        [Fact]
        public void DeletePages()
        {
            Document doc = new Document(Doc("toc.pdf"));
            int oldLen = doc.PageCount;

            doc.DeletePages(0); // delete one page

            Assert.Throws<MuPDF.NET.ValueErrorException>(() => doc.DeletePages(new int[] { 0, }));

            doc.Close();
        }

        [Fact]
        public void XmlMetadata()
        {
            Document doc = new Document(Doc("toc.pdf"));
            doc.DeleteXmlMetadata();

            Assert.True(doc.GetXmlMetadata() == "");
            doc.Close();
        }

        [Fact]
        public void GetXrefLen()
        {
            Document doc = new Document(Doc("toc.pdf"));
            Assert.True(doc.GetXrefLength() == 201);
            doc.Close();
        }

        [Fact]
        public void GetPageImage_ExtractImage()
        {
            Document doc = new Document(Doc("toc.pdf"));
            int n = doc.GetPageImages(0).Count;

            Assert.True(n == 15); // in case of current input pdf, if other file, real count should be fixed

            n = doc.ExtractImage(doc.GetPageImages(0)[0].Xref).Image.Length;

            //Assert.Pass();
            doc.Close();
        }

        [Fact]
        public void GetToc()
        {
            Document doc = new Document(Doc("toc.pdf"));
            doc.GetToc(true);
        }

        [Fact]
        public void EraseToc()
        {
            Document doc = new Document(Doc("toc.pdf"));
            doc.SetToc(null);
            Assert.True(doc.GetToc().Count == 0);
            doc.Close();
        }

        [Fact]
        public void Embedded()
        {
            Document doc = new Document();
            byte[] buffer = Encoding.UTF8.GetBytes("123456678790qwexcvnmhofbnmfsdg4589754uiofjkb-");
            doc.AddEmbfile("file1", buffer, "testfile.txt", "testfile-u.txt", "Description of some sort");
        }

        [Fact]
        public void Test_IsNoPDF()
        {
            Document doc = new Document(Doc("Bezier.epub"));
            Assert.True(doc.IsPDF == false);
        }

        [Fact]
        public void Test_PageIds()
        {
            Document doc = new Document(Doc("Bezier.epub"));

            Assert.True(doc.ChapterCount == 7);
            Assert.True(doc.LastLocation.Item1 == 6);
        }

        [Fact]
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

        [Fact]
        public void OpenUnicodeDocument()
        {
            Document doc = new Document(Doc("你好.pdf"));
            Assert.True(doc.PageCount == 1);
            doc.Close();
        }

        [Fact]
        public void TestRewriteImages()
        {
            // Example for decreasing file size by more than 30%.
            string filePath = Doc("test-rewrite-images.pdf");
            Document doc = new Document(filePath);
            int size0 = File.ReadAllBytes(filePath).Length;
            doc.RewriteImage(dpiThreshold: 100, dpiTarget: 72, quality: 33);
            byte[] data = doc.Write(garbage: true, deflate: true);
            int size1 = data.Length;

            Assert.True((1-(size1/size0)) > 0.3);
        }

        [Fact]
        public void TestJoinPdfPages()
        {
            string testFilePath1 = Doc("Widget.pdf");
            Document doc1 = new Document(testFilePath1);
            string testFilePath2 = Doc("Color.pdf");
            Document doc2 = new Document(testFilePath2);

            doc1.InsertPdf(doc2, 0, 0, 2);

            doc1.Save(Out("Joined.pdf"), pretty: 1);

            Assert.True(doc1.PageCount == 7);

            doc2.Close();
            doc1.Close();
        }

        [Fact]
        public void TestMoveFile()
        {
            string testFilePath1 = Doc("Widget.pdf");
            string testFilePath2 = Out("TestMoveOrig.pdf");
            string testFilePath3 = Out("TestMoveNew.pdf");

            File.Copy(testFilePath1, testFilePath2, true);

            Document doc = new Document(testFilePath2);
            ///*
            Page page = doc[0];
            
            Point tl = new Point(100, 120);
            Point br = new Point(300, 150);

            Rect rect = new Rect(tl, br);
            TextWriter pw = new TextWriter(page.TrimBox);
            Font font = new Font(fontName: "tiro");
            List<(string, float)> ret = pw.FillTextbox(rect, "This is a test to overwrite the original file and move it", font, fontSize: 24);

            pw.WriteText(page);
            page.Dispose();
            //*/            

            MemoryStream tmp = new MemoryStream();

            doc.Save(tmp, garbage: 3, deflateFonts: 1, deflate: 1);
            doc.Close();

            File.WriteAllBytes(testFilePath2, tmp.ToArray());

            tmp.Dispose();

            File.Move(testFilePath2, testFilePath3, true);

            Document newDoc = new Document(testFilePath3);
            Assert.True(newDoc.PageCount == 6);
            newDoc.Close();
        }
    }
}
