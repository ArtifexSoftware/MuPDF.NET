using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class MuPDFAnnotTest
    {
        [Test]
        public void Annot_CleanContents()
        {
            MuPDFDocument doc = new MuPDFDocument();
            MuPDFPage page = doc.NewPage();
            MuPDFAnnot annot = page.AddHighlightAnnot(new Rect(10, 10, 20, 20));

            annot.CleanContents();

            Assert.That(Encoding.UTF8.GetString(annot.GetAP()).StartsWith("q"), Is.EqualTo(true));
        }

        [Test]
        public void Test_PdfString()
        {
            Utils.GetPdfNow();
            Utils.GetPdfString("Beijing, chinesisch 北京");
            Utils.GetTextLength("Beijing, chinesisch 北京", fontname: "china-s");
            Utils.GetPdfString("Latin characters êßöäü");
        }

        [Test]
        public void TestCaret()
        {
            MuPDFDocument doc = new MuPDFDocument();
            MuPDFPage page = doc.NewPage();
            Rect r = new Rect(72, 72, 220, 100);
            MuPDFAnnot annot = page.AddCaretAnnot(r.TopLeft);

            Assert.That(annot.Type.Item2, Is.EqualTo("Caret"));
            Assert.That((int)annot.Type.Item1, Is.EqualTo(14));

            annot.Update(rotate: 20);

            page.GetAnnotNames();
            page.GetAnnotXrefs();
        }

        [Test]
        public void TestFreeTest()
        {
            MuPDFDocument doc = new MuPDFDocument();
            MuPDFPage page = doc.NewPage();
            Rect r = new Rect(72, 72, 220, 100);
            string t1 = "têxt üsès Lätiñ charß,\nEUR: €, mu: µ, super scripts: ²³!";
            MuPDFAnnot annot = page.AddFreeTextAnnot(r, t1, fontSize: 10, rotate: 90, textColor: new float[] { 0, 0, 1 }, align: (int)TextAlign.TEXT_ALIGN_CENTER);

            annot.SetBorder(border: null, width: 0.3f, dashes: new int[] { 2 });
            annot.Update(textColor: new float[] { 0, 0, 1 }, fillColor: new float[] { 0, 1, 1 });
            
            Assert.That((int)annot.Type.Item1, Is.EqualTo(2));
            Assert.That(annot.Type.Item2, Is.EqualTo("FreeText"));
        }

        [Test]
        public void AddPolyLine()
        {
            MuPDFDocument doc = new MuPDFDocument();
            MuPDFPage page = doc.NewPage();
            Rect r = new Rect(72, 72, 220, 100);
            MuPDFAnnot annot = page.AddFileAnnot(r.TopLeft, Encoding.UTF8.GetBytes("just anything for testing"), "testdata.txt");

            Assert.That((int)annot.Type.Item1, Is.EqualTo(17));
        }
    }
}
