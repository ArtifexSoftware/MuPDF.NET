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

            /*MuPDFDocument doc = new MuPDFDocument("1.pdf");
            MuPDFPage page = new MuPDFPage(doc.GetPage(0), doc);

            byte[] buf = File.ReadAllBytes("kenpixel.ttf");
            page.InsertFont(fontName: "kenpixel", "kenpixel.ttf", fontBuffer: buf);*/

            ZipArchive arch = ZipFile.OpenRead("1.zip");
            MuPDFArchive a = new MuPDFArchive();
            a.Add(arch, "./");

            foreach (SubArchiveStruct e in a.EntryList)
            {
                foreach (string ee in e.Entries)
                {
                    Console.WriteLine(ee);
                }
            }
        }

    }
}
