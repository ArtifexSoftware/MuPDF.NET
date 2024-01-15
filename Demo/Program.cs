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
            Console.WriteLine($"Page Count : {doc.GetPageCount()}");

            MuPDFSTextPage page0 = doc.GetStextPage(0);
            Console.Write("Page 1 : ");
            Console.WriteLine(page0.ExtractText());

            MuPDFSTextPage page1 = doc.GetStextPage(1);
            Console.Write("Page 2 : ");
            Console.WriteLine(page1.ExtractBlocks().Count);

            MuPDFSTextPage page2 = doc.GetStextPage(1);
            Console.WriteLine("Page 2 : ");
            Console.WriteLine(MuPDFSTextPage.Search(page2, "F").Count);
        }
    }
}
