namespace Demo
{
    internal partial class Program
    {
        /// <summary>Three stacked FreeText annotations (plain text, fonts, rotation).</summary>
        internal static void TestAnnotationsFreeText1(string[] args)
        {
            _ = args;
            Console.WriteLine("\n=== TestAnnotationsFreeText1 =======================");

            Document doc = new Document();
            Page page = doc.NewPage();

            Rect r1 = new Rect(100, 100, 200, 150);
            Rect r2 = r1 + new Rect(0, 75, 0, 75);
            Rect r3 = r2 + new Rect(0, 75, 0, 75);

            string t = "¡Un pequeño texto para practicar!";

            Annot a1 = page.AddFreeTextAnnot(r1, t, textColor: Constants.red);
            Annot a2 = page.AddFreeTextAnnot(r2, t, fontName: "Ti", textColor: Constants.blue);
            Annot a3 = page.AddFreeTextAnnot(r3, t, fontName: "Co", textColor: Constants.blue, rotate: 90);
            a3.SetBorder(width: 0);
            a3.Update(fontSize: 8, fillColor: Constants.gold);

            doc.Save("a-freetext.pdf");
            doc.Close();

            Console.WriteLine("Saved to a-freetext.pdf");
        }

        /// <summary>FreeText with rich text, styling, and callout line.</summary>
        internal static void TestAnnotationsFreeText2(string[] args)
        {
            _ = args;
            Console.WriteLine("\n=== TestAnnotationsFreeText2 =======================");

            string ds = "font-size: 11pt; font-family: sans-serif;";
            string bullet = "\u2610\u2611\u2612";

            string text = $@"<p style=""text-align:justify;margin-top:-25px;"">
MuPDF.NET <span style=""color: red;"">འདི་ ཡིག་ཆ་བཀྲམ་སྤེལ་གྱི་དོན་ལུ་ པའི་ཐོན་ཐུམ་སྒྲིལ་དྲག་ཤོས་དང་མགྱོགས་ཤོས་ཅིག་ཨིན།</span>
<span style=""color:blue;"">Here is some <b>bold</b> and <i>italic</i> text, followed by <b><i>bold-italic</i></b>. Text-based check boxes: {bullet}.</span>
 </p>";

            Document doc = new Document();
            Page page = doc.NewPage();

            Rect rect = new Rect(100, 100, 350, 200);
            Point p2 = rect.TopRight + new Point(50, 30);
            Point p3 = p2 + new Point(0, 30);

            Annot annot = page.AddFreeTextAnnot(
                rect,
                text,
                fillColor: Constants.gold,
                opacity: 1,
                rotate: 0,
                borderWidth: 1,
                dashes: null,
                richtext: true,
                style: ds,
                callout: new Point[] { p3, p2, rect.TopRight },
                lineEnd: PdfLineEnding.PDF_ANNOT_LE_OPEN_ARROW,
                borderColor: Constants.green
            );

            const string outName = "AnnotationsFreeText2.pdf";
            doc.Save(outName, pretty: 1);
            doc.Close();

            Console.WriteLine("Saved to " + outName);
        }
    }
}
