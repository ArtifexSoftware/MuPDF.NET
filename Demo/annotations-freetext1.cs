using MuPDF.NET;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Demo
{
    public static class AnnotationsFreeText1
    {
        public static void Run(string[] args)
        {
            Console.WriteLine("\n=== AnnotationsFreeText1 =======================");
            Document doc = new Document();
            Page page = doc.NewPage();

            // 3 rectangles, same size, above each other
            Rect r1 = new Rect(100, 100, 200, 150);
            Rect r2 = r1 + new Rect(0, 75, 0, 75);
            Rect r3 = r2 + new Rect(0, 75, 0, 75);

            // the text, Latin alphabet
            string t = "¡Un pequeño texto para practicar!";

            // add 3 annots, modify the last one somewhat
            Annot a1 = page.AddFreeTextAnnot(r1, t, textColor: Constants.red);
            Annot a2 = page.AddFreeTextAnnot(r2, t, fontName: "Ti", textColor: Constants.blue);
            Annot a3 = page.AddFreeTextAnnot(r3, t, fontName: "Co", textColor: Constants.blue, rotate: 90);
            a3.SetBorder(width: 0);
            a3.Update(fontSize: 8, fillColor: Constants.gold);

            doc.Save("a-freetext.pdf");

            doc.Close();

            Console.WriteLine("Saved to a-freetext.pdf");
        }
    }
}
