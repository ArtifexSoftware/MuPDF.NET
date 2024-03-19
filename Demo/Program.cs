using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using mupdf;
using MuPDF.NET;
using System.Diagnostics;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            /*Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // start
            MuPDFDocument doc = new MuPDFDocument("pandas.pdf");
            for (int i = 0; i < doc.GetPageCount(); i++)
            {
                MuPDFSTextPage stpage = new MuPDFSTextPage(doc.GetPage(i));
                stpage.ExtractHtml();
            }

            // end
            stopWatch.Stop();

            TimeSpan ts = stopWatch.Elapsed;

            Console.WriteLine($"Elapsed Time is {ts.TotalMilliseconds}");*/

            MuPDFDocument src = new MuPDFDocument("1.pdf");

            MuPDFDocument doc = new MuPDFDocument();
            MuPDFPage page = doc.NewPage();
            Rect r1 = new Rect(0, 0, page.Rect.Width, page.Rect.Height / 2.0f);
            Rect r2 = r1 + new Rect(0, page.Rect.Height / 2.0f, 0, page.Rect.Height / 2.0f);

            page.ShowPdfPage(r1, src, 0, rotate: 90);
            page.ShowPdfPage(r2, src, 0, rotate: -90);

            doc.Save("output.pdf");
            /*for (int i = 0; i < doc.GetPageCount(); i++)
            {
                MuPDFPage page = doc.LoadPage(i);
                foreach (var k in page.GetCDrawings(true)[0].Keys)
                    Console.WriteLine(k);
            }
            *//*foreach (Rect r in page.GetCDrawings())
            {
                Console.WriteLine(r.ToString());
            }*/

        }

    }
}
