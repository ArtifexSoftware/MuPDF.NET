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

            MuPDFPage page = new MuPDFPage(doc.GetPage(0), doc);

            Console.WriteLine(stpage.ExtractText());

            Console.WriteLine("Page 0 : ");
            List<Quad> matches = MuPDFSTextPage.Search(stpage, "Pixmap");

            Console.WriteLine(matches.Count);

            page.AddHighlightAnnot(matches, new Point(0, 0), new Point(page.MEDIABOX.Width, page.MEDIABOX.Height), page.MEDIABOX);
        }
    }
}
