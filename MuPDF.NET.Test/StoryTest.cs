using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            Rect box = Utils.PaperRect("letter");
            Rect where = box + new Rect(36, 36, -36, -36);
            Story story = new Story(html: html);
            DocumentWriter writer = new DocumentWriter("output.pdf");

            int pno = 0;
            bool more = true;

            while (more)
            {
                Rect filled = new Rect();
                DeviceWrapper dev = writer.BeginPage(box);
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
            Story.RectFunction rectfunc = new Story.RectFunction((int rectnum, Rect fill) =>
            {
                return (new Rect(0, 0, 200, 200), new Rect(50, 50, 100, 100), null);
            });

            Document MakePdf(string html, string path)
            {
                Story story = new Story(html: html);
                Document doc = story.WriteWithLinks(rectfunc);
                return doc;
            }

            Document doc1 = MakePdf("<p>Before</p><p style=\"page-break-before: always;\"></p><p>After</p>", "After.pdf");

            Document doc2 = MakePdf("<p>before</p>", "before.pdf");

            Assert.That(doc1.PageCount, Is.EqualTo(2));
            Assert.That(doc2.PageCount, Is.EqualTo(1));
        }

        [Test]
        public void GetPixmap()
        {
            Document pdf = new Document("../../../resources/test_3450.pdf");
            Page page = pdf[0];
            Stopwatch stopwatch = Stopwatch.StartNew(); // Start timing

            // Assuming GetPixmap is a method that retrieves a pixel map from the page
            var pix = page.GetPixmap(alpha: false, dpi: 150); // Replace with your actual method to get the pixel map

            stopwatch.Stop(); // Stop timing

            TimeSpan timeTaken = stopwatch.Elapsed; // Get elapsed time

            Console.WriteLine(timeTaken.ToString());
        }
    }
}
