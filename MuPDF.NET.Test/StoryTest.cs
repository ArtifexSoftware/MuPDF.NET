using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
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
    }
}
