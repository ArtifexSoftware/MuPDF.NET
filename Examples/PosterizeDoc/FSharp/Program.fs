// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET

let src = Document("input.pdf")
let doc = Document()

for i in 0 .. src.PageCount - 1 do
    let spage = src.[i]
    let mutable xref = 0
    let r = spage.Rect
    let d = Rect(spage.CropBoxPosition, spage.CropBoxPosition)

    let r1 = r * 0.5f
    let r2 = r1 + Rect(r1.Width, 0.0f, r1.Width, 0.0f)
    let r3 = r1 + Rect(0.0f, r1.Height, 0.0f, r1.Height)
    let r4 = Rect(r1.BottomRight, r.BottomRight)
    let rectList = [| r1; r2; r3; r4 |]

    for rr in rectList do
        let rx = rr + d
        let page = doc.NewPage(-1, rx.Width, rx.Height)
        xref <- page.ShowPdfPage(page.Rect, src, spage.Number, clip = rx)

doc.Save("output.pdf", garbage = 4, deflate = 1)
