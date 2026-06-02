using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class StoryTest
    {
        private const string TestClassName = nameof(StoryTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void Draw()
        {
            string html = "<html><head></head><body><h1>Header level 1</h1><h2>Header level 2</h2></body><p>Hello MuPDF</p></html>";

            Rect box = Utils.PaperRect("letter");
            Rect where = box + new Rect(36, 36, -36, -36);
            Story story = new Story(html: html);
            DocumentWriter writer = new DocumentWriter(Out("Draw.pdf"));

            int pno = 0;
            bool more = true;

            while (more)
            {
                Rect filled = new Rect();
                DeviceWrapper dev = writer.BeginPage(box);
                (more, filled) = story.Place(where);
                story.ElementPositions(null, new Position() { PageNum = pno });
                story.Draw(dev);
                dev.Dispose();
                writer.EndPage();
                pno += 1;
            }
            writer.Close();
        }

        [Fact]
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

            Document doc1 = MakePdf("<p>Before</p><p style=\"page-break-before: always;\"></p><p>After</p>", Out("Draw1-After.pdf"));

            Document doc2 = MakePdf("<p>before</p>", Out("Draw1-before.pdf"));

            Assert.Equal(2, doc1.PageCount);
            Assert.Equal(1, doc2.PageCount);

            doc1.Save(Out("Draw1-After.pdf"));
            doc2.Save(Out("Draw1-before.pdf"));
        }

        /*
        [Fact]
        public void GetPixmap()
        {
            Document pdf = new Document(Doc("test_3450.pdf"));
            Page page = pdf[0];
            Stopwatch stopwatch = Stopwatch.StartNew(); // Start timing

            // Assuming GetPixmap is a method that retrieves a pixel map from the page
            var pix = page.GetPixmap(alpha: false, dpi: 150); // Replace with your actual method to get the pixel map

            stopwatch.Stop(); // Stop timing

            TimeSpan timeTaken = stopwatch.Elapsed; // Get elapsed time

            Console.WriteLine(timeTaken.ToString());
        }
        */
    }
}
