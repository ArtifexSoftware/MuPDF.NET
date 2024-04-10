using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using mupdf;
using MuPDF.NET;

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

            MuPDFDocument doc = new MuPDFDocument("1.pdf");
            Console.WriteLine(doc.Name);

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
