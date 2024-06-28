// For more information see https://aka.ms/fsharp-console-apps
open System
open MuPDF.NET

let src = new Document("input.pdf")
let doc = new Document()

let struct (width, height) = Utils.PageSize("a4")
let r = new Rect(0f, 0f, (float32)width, (float32)height)

let r1 = r * 0.5f
printfn "%s" (r.BottomRight.ToString())
printfn "%s" (r1.BottomRight.ToString())
let r2 = r1 + new Rect(r1.Width, 0f, r1.Width, 0f)
let r3 = r1 + new Rect(0f, r1.Height, 0f, r1.Height)
let r4 = new Rect(r1.BottomRight, r.BottomRight)

let rTab = [| r1; r2; r3; r4 |]
let mutable page = null
for i in 0 .. src.PageCount - 1 do
    let spage = src.[i]
    if spage.Number % 4 = 0 then
        page <- doc.NewPage(width = (float32)width, height = (float32)height)
    printfn "%d  %s" i (rTab.[spage.Number % 4].ToString())
    page.ShowPdfPage(
        rTab.[spage.Number % 4],
        src,
        spage.Number)
    |> ignore

doc.Save("output.pdf", garbage = 4, deflate = 1)
