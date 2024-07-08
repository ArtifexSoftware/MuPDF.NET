// See https://aka.ms/new-console-template for more information
using MuPDF.NET;
using System.Text;

Document doc = new Document("input.pdf");
Page page = doc[0];

List<Entry> images = page.GetImages();
int oldXref = images[0].Xref;

Pixmap pix = new Pixmap(Utils.csGRAY, new IRect(0, 0, 1, 1), 1);
pix.ClearWith();

int newXref = page.InsertImage(page.Rect, pixmap: pix);
doc.CopyXref(newXref, oldXref);

List<int> contents = page.GetContents();
int lastXref = contents.Last();

doc.UpdateStream(lastXref, Encoding.UTF8.GetBytes(" "));

doc.Save("e://res/output.pdf");