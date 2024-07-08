// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET
open System.Text

let doc = new Document("input.pdf")
let page = doc.[0]

let images = page.GetImages()
let oldXref = images.[0].Xref

let pix = new Pixmap(Utils.csGRAY, new IRect(0f, 0f, 1f, 1f), 1)
pix.ClearWith()

let newXref = page.InsertImage(page.Rect, pixmap = pix)
doc.CopyXref(newXref, oldXref)

let contents = page.GetContents()
let lastXref = contents[contents.Count - 1];

doc.UpdateStream(lastXref, Encoding.UTF8.GetBytes(" "))

doc.Save("e://res/output.pdf")