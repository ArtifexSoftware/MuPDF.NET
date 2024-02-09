using System;
using System.Collections.Generic;
using MuPDF.NET;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {

            /*MuPDFDocument doc = new MuPDFDocument("1.pdf");

            MuPDFPage page = new MuPDFPage(doc.GetPage(0), doc);

            List<Quad> matches = page.SearchFor("the");

            if (matches.Count > 0)
                page.AddStrikeoutAnnot(matches);

            //doc.InsertPage(3, text: "hello", fontName: "kenpixel", fontFile: "./kenpixel.ttf");

            doc.Save("output.pdf", userPW: "hello", encryption: 3);*/

            MuPDFStory s = new MuPDF.NET.MuPDFStory(html: "hello world");

        }
    }
}
