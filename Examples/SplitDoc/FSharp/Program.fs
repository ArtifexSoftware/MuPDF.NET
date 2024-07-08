// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET

let fn = "input.pdf"
let pattern = "input"

let src = Document(fn)
for i in 0..src.PageCount-1 do
    let doc = Document()
    doc.InsertPdf(src, fromPage = i, toPage = i)
    doc.Save(sprintf "./output/%s-%d.pdf" pattern i)
    doc.Close()
