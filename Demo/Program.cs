using System;
using System.Collections.Generic;
using CSharpMuPDF;
using mupdf;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            MuPDFDocument doc = new MuPDFDocument("1.pdf");

            MuPDFSTextPage stpage = new MuPDFSTextPage(doc.GetPage(0));

            //Console.WriteLine(stpage.ExtractText());

            MuPDFPage page = new MuPDFPage(doc.GetPage(0), doc);

            List<Quad> matches = page.SearchFor("Pixmap");
            
            foreach (Quad match in matches)
            {
                page.AddHighlightAnnot(match.Rect);
            }

            Console.WriteLine(stpage.Char2Canon("dabcdf"));

            Console.WriteLine(Convert.ToInt32('d'));

            doc.Save("output.pdf");
        }
    }
}
