// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

string highlight = "this text is highlighted";
string underline = "this text is underlined";
string strikeout = "this text is striked out";
string squiggled = "this text is zigzag-underlined";

float[] red = { 1f, 0f, 0f };
float[] blue = { 0, 0, 1f };
float[] gold = { 1f, 1f, 0 };
float[] green = { 0, 1f, 0 };

Rect disp = new Rect(0, 50f, 0, 50f);
Rect r = new Rect(72f, 72f, 220f, 100f);
Font font = new Font(fontName: "Atop", fontFile: "e:/res/apo.ttf");

Document doc = new Document();
Page page = doc.NewPage();

page.SetRotation(0);

Rect pageRect = page.Rect * page.DerotationMatrix;

void PrintDescription(Annot annot)
{
    Rect rect = annot.Rect;
    Page page = annot.GetParent();
    MuPDF.NET.TextWriter writer = new MuPDF.NET.TextWriter(pageRect, color: red);
    writer.Append(rect.BottomRight + new Point(10, -5), $"{annot.Type.Item1} annotation", font: font);
    writer.WriteText(page);
}

string t1 = "têxt üsès Lätiñ charß,\nEUR: €, mu: µ, super scripts: ²³!";

Annot annot = page.AddCaretAnnot(r.TopLeft);
PrintDescription(annot);

r = r + disp;
annot = page.AddFreeTextAnnot(r, t1, fontSize: 10, rotate: 90, textColor: blue, fillColor: gold, align: (int)TextAlign.TEXT_ALIGN_CENTER);
annot.SetBorder(null, width: 0.3f, dashes: [2]);
annot.Update(textColor: blue, fillColor: gold);
PrintDescription(annot);

r = annot.Rect + disp;
annot = page.AddTextAnnot(r.TopLeft, t1);
PrintDescription(annot);

Point pos = annot.Rect.TopLeft + disp.TopLeft;
Matrix mat = new Matrix(-15);
MuPDF.NET.TextWriter writer = new MuPDF.NET.TextWriter(pageRect);
writer.Append(pos, underline, font: font);
writer.WriteText(page, morph: new Morph() { P= pos, M= mat});
writer.TextRect.X0 = pos.X;
writer.TextRect.X1 = writer.LastPoint.X;
Quad quadHighlight= writer.TextRect.Morph(pos, ~mat);                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           
pos = quadHighlight.Rect.BottomLeft;

annot = page.AddHighlightAnnot(quadHighlight);
PrintDescription(annot);

doc.Save("output.pdf");
