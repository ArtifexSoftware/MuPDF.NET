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
            string fileName = "1.pdf";
            byte[] buff = null;
            FileStream fs = new FileStream(fileName,
                                           FileMode.Open,
                                           FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            long numBytes = new FileInfo(fileName).Length;
            buff = br.ReadBytes((int)numBytes);

            MuPDFDocument doc = new MuPDFDocument(filename: "pdf", buff);

            MuPDFPage page = new MuPDFPage(doc.GetPage(0), doc);

            /*List<Quad> matches = page.SearchFor("the");

            if (matches.Count > 0)
                page.AddStrikeoutAnnot(matches);*/

            page.InsertHtmlBox(new Rect(0, 0, 400, 400), text: "<b>hello world</b>", rotate: 90);
            

            //doc.InsertPage(3, text: "hello", fontName: "kenpixel", fontFile: "./kenpixel.ttf");

            doc.Save("output.pdf");
        }

    }
}
