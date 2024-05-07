using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class StoryTest
    {
        [Test]
        public void Draw()
        { 
            string html = "<html><head></head><body><h1>Header level 1</h1><h2>Header level 2</h2></body><p>Hello MuPDF</p></html>";

            Rect box = Utils.PageRect("letter");
            Rect where = box + new Rect(36, 36, -36, -36);
            MuPDFStory story = new MuPDFStory(html: html);
            MuPDFDocumentWriter writer = new MuPDFDocumentWriter("output.pdf");

            int pno = 0;
            bool more = true;

            while (more)
            {
                Rect filled = new Rect();
                MuPDFDeviceWrapper dev = writer.BeginPage(box);
                (more, filled) = story.Place(where);
                story.ElementPositions(null, new Position() { PageNum = pno });
                story.Draw(dev);
                writer.EndPage();
                pno += 1;
            }
            writer.Close();
        }

        [Test]
        public void Draw1()
        {
            MuPDFStory.RectFunction rectfunc = new MuPDFStory.RectFunction((int rectnum, Rect fill) =>
            {
                return (new Rect(0, 0, 200, 200), new Rect(50, 50, 100, 100), null);
            });

            MuPDFDocument MakePdf(string html, string path)
            {
                MuPDFStory story = new MuPDFStory(html: html);
                MuPDFDocument doc = story.WriteWithLinks(rectfunc);
                return doc;
            }

            MuPDFDocument doc1 = MakePdf("<p>Before</p><p style=\"page-break-before: always;\"></p><p>After</p>", "After.pdf");

            MuPDFDocument doc2 = MakePdf("<p>before</p>", "before.pdf");

            Assert.That(doc1.PageCount, Is.EqualTo(2));
            Assert.That(doc2.PageCount, Is.EqualTo(1));
        }
    }
}
