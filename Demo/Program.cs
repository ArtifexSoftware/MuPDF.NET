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

            /*MuPDFDocument doc = new MuPDFDocument("1.pdf");
            
            MuPDFPage page = doc.LoadPage(1);

            page.InsertHtmlBox(new Rect(0, 0, 100, 100), "<h1>Hello World</h1>");

            doc.Save("output.pdf");*/
            Matrix m1 = new Matrix(1, 2, 3, 4, 5, 6);
            Matrix m2 = new Matrix(2, 3, 4, 5, 6, 7);
            m2.Invert(m1);
            Console.WriteLine($"{m2.A} {m2.B} {m2.C} {m2.D} {m2.E} {m2.F} ");

        }

    }
}
