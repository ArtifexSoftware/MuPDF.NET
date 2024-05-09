using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    internal class GeneralTest
    {
        [Test]
        public void Test_Opacity()
        {
            MuPDFDocument doc = new MuPDFDocument();
            MuPDFPage page = doc.NewPage();

            MuPDFAnnot annot1 = page.AddCircleAnnot(new Rect(50, 50, 100, 100));
            annot1.SetColors(fill: new float[] { 1, 0, 0 }, stroke: new float[] { 1, 0, 0 });
            annot1.SetOpacity(2 / 3.0f);
            annot1.Update(blendMode: "Multiply");

            MuPDFAnnot annot2 = page.AddCircleAnnot(new Rect(75, 75, 125, 125));
            annot2.SetColors(fill: new float[] { 0, 0, 1 }, stroke: new float[] { 0, 0, 1 });
            annot2.SetOpacity(1 / 3.0f);
            annot2.Update(blendMode: "Multiply");

            doc.Save("output.pdf", expand: 1, pretty: 1);                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               
        }

        [Test]
        public void TestWrapContents()
        {
            MuPDFDocument doc = new MuPDFDocument("resources/001003ED.pdf");
            MuPDFPage page = doc[0];
            page.WrapContents();
            int xref = page.GetContents()[0];
            byte[] cont = page.ReadContents();

            doc.UpdateStream(xref, cont);
            page.SetContents(xref);
            Assert.That(page.GetContents().Count, Is.EqualTo(1));;
            page.CleanContetns();
        }

        [Test]
        public void TestPageCleanContents()
        {
            MuPDFDocument doc = new MuPDFDocument();
            MuPDFPage page = doc.NewPage();
            page.DrawRect(new Rect(10, 10, 20, 20));
            page.DrawRect(new Rect(20, 20, 30, 30));
            Assert.That(page.GetContents().Count, Is.EqualTo(2));
            Assert.That(Encoding.UTF8.GetString(page.ReadContents()).StartsWith("q"), Is.False);

            page.CleanContetns();
            Assert.That(page.GetContents().Count, Is.EqualTo(1));
            Assert.That(Encoding.UTF8.GetString(page.ReadContents()).StartsWith("q"), Is.True);
        }

        [Test]
        public void TestCorrectChar()
        {
            
        }
    }
}
