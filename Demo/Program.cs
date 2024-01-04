using System;
using System.Collections.Generic;
using CSharpMuPDF;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            MuPDFDocument doc = new MuPDFDocument("1.pdf");
            Console.WriteLine(doc.GetPageCount());

            MuPDFSTextPage page = doc.GetStextPage(0);
            page.

        }
    }
}
