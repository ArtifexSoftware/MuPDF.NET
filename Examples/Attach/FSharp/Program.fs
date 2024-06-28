open System
open System.IO
open MuPDF.NET

let doc = new Document()
let struct (width, height) = Utils.PageSize "a6-l"
let page = doc.NewPage(width = (float32)width, height = (float32)height)
let rect = new Rect(36f, 36f, (float32)(width - 36), (float32)(height - 36))

let imgList = Directory.GetFiles("input")
let imgCount = imgList.Length

let perPage = (((width - 72) / 25) * ((height - 36 - 56) / 35))

let pages = int (Math.Round(float imgCount / float perPage + 0.5))

let text = sprintf "Contains the following %d files from img:\n\n" imgCount

let mutable pno = 1

page.InsertText(rect.TopLeft, text, fontName = "Kenpixel", fontFile = "kenpixel.ttf") |> ignore
page.InsertText(rect.BottomLeft, sprintf "Page %d of %d" pno pages, fontFile = "kenpixel.ttf", fontName = "Kenpixel") |> ignore

let mutable point = rect.TopLeft + new Point(0f, 20f)
for path in imgList do
    printfn "%s" path
    if not (File.Exists(path)) then
        printfn "skipping non-file"
    else
        let img = File.ReadAllBytes(path)
        page.AddFileAnnot(point, img, filename = path) |> ignore

        point <- point + new Point(25f, 0f)
        if point.X >= rect.Width then
            point <- new Point(rect.X0, point.Y + 35f)
        if point.Y >= rect.Height && path <> imgList.[imgCount - 1] then
            let page = doc.NewPage(width = (float32)width, height = (float32)height)
            pno <- pno + 1
            page.InsertText(rect.TopLeft, text, fontFile = "kenpixel.ttf", fontName = "Kenpixel") |> ignore
            page.InsertText(rect.BottomLeft, sprintf "Page %d of %d" pno pages, fontFile = "kenpixel.ttf", fontName = "Kenpixel") |> ignore
            point <- rect.TopLeft + new Point(0f, 20f)
doc.Save("output.pdf")