using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class PixmapTest
    {
        [Test]
        public void GetPixamp_Save()
        {
            MuPDFDocument doc = new MuPDFDocument("input.pdf");

            MuPDFPage page = doc[0];

            Pixmap source = page.GetPixmap();

            Pixmap p = new Pixmap(source.ColorSpace, source.W, source.H, source.SAMPLES, 0);

            p.Save("1.jpg", "JPEG");

            Assert.Pass();
        }
    }
}
