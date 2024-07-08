// For more information see https://aka.ms/fsharp-console-apps

open MuPDF.NET

let mutable src = Document("logo.png")

if not src.IsPDF then
    let pdfbytes = src.Convert2Pdf()
    src.Close()
    src <- Document("pdf", pdfbytes)

let rect = src.[0].Rect
printfn "%A" rect
let factor = 25.0f / rect.Height
rect = rect * factor

let doc = Document("input.pdf")
let mutable xref = 0
for i in 0 .. doc.PageCount - 1 do
    xref <- doc.[i].ShowPdfPage(rect, src, 0, overlay = false)

doc.Save("output.pdf", garbage = 4)