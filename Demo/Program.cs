using System;
using System.Collections.Generic;
using mupdf;
using MuPDF.NET;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {

            MuPDFDocument doc = new MuPDFDocument("1.pdf");

            /*MuPDFPage page = new MuPDFPage(doc.GetPage(0), doc);

            page.InsertHtmlBox(new Rect(0, 0, 400, 400), text: "<b>hello world</b>", rotate: 90);
            *//*List<Quad> matches = page.SearchFor("the");

            if (matches.Count > 0)
                page.AddStrikeoutAnnot(matches);*//*

            //doc.InsertPage(3, text: "hello", fontName: "kenpixel", fontFile: "./kenpixel.ttf");

            doc.Save("output.pdf");*/

            List<List<dynamic>> fonts = doc.GetPageFonts(0);
            foreach (List<dynamic> font in fonts)
            {
                foreach (dynamic e in  font)
                {
                    Console.WriteLine(e);
                }
            }
        }

    }
}
