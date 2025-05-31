using MuPDF.NET;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Security.Policy;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Demo
{
    public static class AnnotationsFreeText2
    {
        // Use rich text for FreeText annotations
        public static void Run(string[] args)
        {
            // define an overall styling
            string ds = "font-size: 11pt; font-family: sans-serif;";
            // some special characters
            string bullet = "\u2610\u2611\u2612"; // Output: ☐☑☒

            // the annotation text with HTML and styling syntax
            string text = $@"<p style=""text-align:justify;margin-top:-25px;"">
PyMuPDF <span style=""color: red;"">འདི་ ཡིག་ཆ་བཀྲམ་སྤེལ་གྱི་དོན་ལུ་ པའི་ཐོན་ཐུམ་སྒྲིལ་དྲག་ཤོས་དང་མགྱོགས་ཤོས་ཅིག་ཨིན།</span>
<span style=""color:blue;"">Here is some <b>bold</b> and <i>italic</i> text, followed by <b><i>bold-italic</i></b>. Text-based check boxes: {bullet}.</span>
 </p>";

            Document doc = new Document();
            Page page = doc.NewPage();

            // 3 rectangles, same size, above each other
            Rect rect = new Rect(100, 100, 350, 200);

            // define some points for callout lines
            Point p2 = rect.TopRight + new Point(50, 30);
            Point p3 = p2 + new Point(0, 30);

            // define the annotation
            Annot annot = page.AddFreeTextAnnot(
                rect,
                text,
                fillColor: Constants.gold,  // fill color
                opacity: 1,  // non-transparent
                rotate: 0,  // no rotation
                borderWidth: 1,  // border and callout line width
                dashes: null,  // no dashing
                richtext: true,  // this is rich text
                style: ds,  // my styling default
                callout: new Point[]{ p3, p2, rect.TopRight },  // define end, knee, start points
                lineEnd: PdfLineEnding.PDF_ANNOT_LE_OPEN_ARROW,  // symbol shown at p3
                borderColor: Constants.green
            );

            doc.Save(typeof(AnnotationsFreeText2).Name + ".pdf", pretty:1);

            doc.Close();
        }
    }
}
