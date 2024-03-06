using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using mupdf;
using MuPDF.NET;

namespace Demo
{
    class Program
    {
        /*public (Rect, Rect, IdentityMatrix) RectFunction(int rectN, Rect filled)
        {
            return (new Rect(), new Rect(), new IdentityMatrix());
        }

        public string ContentFunction(List<Position> positions)
        {
            return string.Format("Hello world {0}", positions.Count);
        }

        public Rect Fn(Rect r, float f)
        {
            return new Rect(0, 0, 1, 1);
        }*/

        static void Main(string[] args)
        {
            /*string fileName = "2.pdf";
            byte[] buff = null;
            FileStream fs = new FileStream(fileName,
                                           FileMode.Open,
                                           FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            long numBytes = new FileInfo(fileName).Length;
            buff = br.ReadBytes((int)numBytes);

            MuPDFDocument doc = new MuPDFDocument(filename: "pdf", buff);

            MuPDFPage page = new MuPDFPage(doc.GetPage(0), doc);*/

            //doc.InsertPage(3, text: "hello", fontName: "kenpixel", fontFile: "./kenpixel.ttf");

            /*doc.Save("output.pdf");*/

            ////////////////////////////////
            /*MuPDFDocument doc = new MuPDFDocument("1.pdf");
            MuPDFPage page = new MuPDFPage(doc.GetPage(0), doc);

            byte[] buf = File.ReadAllBytes("kenpixel.ttf");
            page.InsertFont(fontName: "kenpixel", "kenpixel.ttf", fontBuffer: buf);*/

            ////////////////////////////////////
            /*ZipArchive arch = ZipFile.OpenRead("1.zip");
            MuPDFArchive a = new MuPDFArchive();
            a.Add(arch, "./");

            foreach (SubArchiveStruct e in a.EntryList)
            {
                foreach (string ee in e.Entries)
                {
                    Console.WriteLine(ee);
                }
            }*/

            ////////----------------------------Xml
            /*MuPDFXml xml = new MuPDFXml("<html><head><title>Hello</title></head><body><p>Hellow world!</p></body></html>");

            MuPDFXml a = xml.AddParagraph();

            xml.InsertAfter(a);

            a.AddClass("first_element");

            a.AddText("hello, first element");

            Console.WriteLine(xml.Text);*/

            ///////---------------Story---------------
            /*MuPDFArchive archive = new MuPDFArchive("./");
            string html = "<p>Hello world</p>";
            string css = "* {display: hidden;}";

            MuPDFStory story = new MuPDFStory(html: html, userCss: css, archive: archive);
            (bool more, Rect filled) = story.Place(new Rect(0, 0, 100, 100));
            Console.WriteLine(filled.ToString());

            Program p = new Program();

            // MuPDFStory.WriteStabilizedWithLinks(contentfn: p.ContentFunction, rectfn: p.RectFunction);

            FitResult ret = story.Fit(p.Fn, new Rect(0, 0, 100, 100), 0.3f, 0.7f);
            Console.WriteLine(ret.ToString());*/


            MuPDFDocument doc = new MuPDFDocument("1.pdf");
            MuPDFPage page = doc.LoadPage(1);
            Pixmap pix = page.GetPixmap();
            byte[] bytes = pix.PdfOCR2Bytes();

            Console.WriteLine(bytes.Length);
        }

    }
}
