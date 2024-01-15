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
            Console.WriteLine(doc.GetPageCount());

            MuPDFSTextPage page = doc.GetStextPage(0);
            
            FzQuad quad = new FzQuad(new FzRect(3f, 4f, 5f, 6f));
            Console.WriteLine(quad.ul.x);
            Console.WriteLine(quad.ul.y);
        }
    }
}
