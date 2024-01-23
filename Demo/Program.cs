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

            MuPDFPage page = new MuPDFPage(doc.GetPage(0), doc);

            List<Quad> matches = page.SearchFor("Pixmap");
            if (matches.Count > 0)
                page.AddHighlightAnnot(matches);

            doc.Save("output.pdf");
        }
    }
}
