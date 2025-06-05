/*
 * -------------------------------------------------------------------------------
 * Demo script showing how annotations can be added to a PDF using MuPDF.NET.
 * 
 * It contains the following annotation types:
 * Caret, Text, FreeText, text markers (underline, strike-out, highlight,
 * squiggle), Circle, Square, Line, PolyLine, Polygon, FileAttachment, Stamp
 * and Redaction.
 * There is some effort to vary appearances by adding colors, line ends,
 * opacity, rotation, dashed lines, etc.
 * 
-------------------------------------------------------------------------------
*/

using MuPDF.NET;
using System.Collections.Generic;
using System.Text;

namespace Demo
{
    public static class NewAnnots
    {
        private static void print_descr(Annot annot)
        {
            // Print a short description to the right of each annot rect.
            string description = annot.Type.Item2 + " annotation";
            annot.GetParent().InsertText(
                annot.Rect.BottomRight + new Point(10, -5), description, color: Constants.red);
        }

        // Use rich text for FreeText annotations
        public static void Run(string[] args)
        {
            Rect r = Constants.r;  // use the rectangle defined in Constants.cs

            Document doc = new Document();
            Page page = doc.NewPage();

            page.SetRotation(0);  // no rotation

            Annot annot = page.AddCaretAnnot(r.TopLeft);
            print_descr(annot);

            r = r + Constants.displ;
            annot = page.AddFreeTextAnnot(
                r,
                Constants.t1,
                fontSize: 10,
                rotate: 90,
                textColor: Constants.blue,
                fillColor: Constants.gold,
                align: (int)TextAlign.TEXT_ALIGN_CENTER
                );
            annot.SetBorder(width: 0.3f, dashes: new int[] { 2 });
            annot.Update(textColor: Constants.blue, fillColor: Constants.gold);
            print_descr(annot);

            r = r + Constants.displ;
            annot = page.AddTextAnnot(
                r.TopLeft,
                Constants.t1
                );
            print_descr(annot);

            // Adding text marker annotations:
            // first insert a unique text, then search for it, then mark it
            Point pos = annot.Rect.TopLeft + Constants.displ.TopLeft;
            page.InsertText(
                pos, // insertion point
                Constants.highlight, // inserted text
                morph: new Morph(pos, new Matrix(-5)) // rotate around insertion point
                );
            List<Quad> rl = page.SearchFor(Constants.highlight, quads: true); // need a quad b/o tilted text
            annot = page.AddHighlightAnnot(rl[0]);
            print_descr(annot);

            pos = annot.Rect.BottomLeft;  // next insertion point
            page.InsertText(pos, Constants.underline, morph: new Morph(pos, new Matrix(-10)));
            rl = page.SearchFor(Constants.underline, quads: true);
            annot = page.AddUnderlineAnnot(rl[0]);
            print_descr(annot);

            pos = annot.Rect.BottomLeft;
            page.InsertText(pos, Constants.strikeout, morph: new Morph(pos, new Matrix(-15)));
            rl = page.SearchFor(Constants.strikeout, quads: true);
            annot = page.AddStrikeoutAnnot(rl[0]);
            print_descr(annot);

            pos = annot.Rect.BottomLeft;
            page.InsertText(pos, Constants.squiggled, morph: new Morph(pos, new Matrix(-20)));
            rl = page.SearchFor(Constants.squiggled, quads: true);
            annot = page.AddSquigglyAnnot(rl[0]);
            print_descr(annot);

            pos = annot.Rect.BottomLeft;
            r = new Rect(pos, pos.X + 75, pos.Y + 35) + new Rect(0, 20, 0, 20);
            annot = page.AddPolylineAnnot(new List<Point> { r.BottomLeft, r.TopRight, r.BottomRight, r.TopLeft });  // 'Polyline'
            annot.SetBorder(width: 0.3f, dashes: new int[] { 2 });
            annot.SetColors(stroke: Constants.blue, fill: Constants.green);
            annot.SetLineEnds(PdfLineEnding.PDF_ANNOT_LE_CLOSED_ARROW, PdfLineEnding.PDF_ANNOT_LE_R_CLOSED_ARROW);
            annot.Update(fillColor: new float[] { 1, 1, 0 });
            print_descr(annot);

            r += Constants.displ;
            annot = page.AddPolygonAnnot(new List<Point> { r.BottomLeft, r.TopRight, r.BottomRight, r.TopLeft });  // 'Polygon'
            annot.SetBorder(width: 0.3f, dashes: new int[] { 2 });
            annot.SetColors(stroke: Constants.blue, fill: Constants.gold);
            annot.SetLineEnds(PdfLineEnding.PDF_ANNOT_LE_DIAMOND, PdfLineEnding.PDF_ANNOT_LE_CIRCLE);
            annot.Update();
            print_descr(annot);

            r += Constants.displ;
            annot = page.AddLineAnnot(r.TopRight, r.BottomLeft);  // 'Line'
            annot.SetBorder(width: 0.3f, dashes: new int[] { 2 });
            annot.SetColors(stroke: Constants.blue, fill: Constants.gold);
            annot.SetLineEnds(PdfLineEnding.PDF_ANNOT_LE_DIAMOND, PdfLineEnding.PDF_ANNOT_LE_CIRCLE);
            annot.Update();
            print_descr(annot);

            r += Constants.displ;
            annot = page.AddRectAnnot(r);  // 'Square'
            annot.SetBorder(width: 1f, dashes: new int[] { 1, 2 });
            annot.SetColors(stroke: Constants.blue, fill: Constants.gold);
            annot.Update(opacity: 0.5f);
            print_descr(annot);

            r += Constants.displ;
            annot = page.AddCircleAnnot(r);  // 'Circle'
            annot.SetBorder(width: 0.3f, dashes: new int[] { 2 });
            annot.SetColors(stroke: Constants.blue, fill: Constants.gold);
            annot.Update();
            print_descr(annot);

            r += Constants.displ;
            annot = page.AddFileAnnot(
                r.TopLeft, Encoding.UTF8.GetBytes("just anything for testing"), "testdata.txt");  // 'FileAttachment'
            print_descr(annot);  // annot.rect

            r += Constants.displ;
            annot = page.AddStampAnnot(r, stamp: 10);  // 'Stamp'
            annot.SetColors(stroke: Constants.green);
            annot.Update();
            print_descr(annot);

            r += Constants.displ + new Rect(0, 0, 50, 10);
            float rc = page.InsertTextbox(
                r,
                "This content will be removed upon applying the redaction.",
                color: Constants.blue,
                align: (int)TextAlign.TEXT_ALIGN_CENTER
            );
            annot = page.AddRedactAnnot(r.Quad);
            print_descr(annot);

            doc.Save(typeof(NewAnnots).Name + ".pdf", deflate:1);

            doc.Close();
        }
    }
}
