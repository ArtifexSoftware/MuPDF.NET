using System;
using System.Collections.Generic;
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

            MuPDFArchive arch = new MuPDFArchive("../net7.0");
            FzStream stream = new FzStream("./1.zip");
            Console.WriteLine(stream.fz_is_zip_archive());
            FzArchive ar = new FzArchive(stream, 435);

            arch.Add(ar, "");

        }

    }
}
