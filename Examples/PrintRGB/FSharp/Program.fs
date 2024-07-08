// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET
open System.Collections.Generic

let myList = Utils.GetColorInfoList()

let w = 800.0
let h = 600.0
let rw = 80.0
let rh = 60.0

let numColors = myList.Count
let black = Utils.GetColor "black"
let white = Utils.GetColor "white"
let fsize = 8.0f
let lheight = float32 (fsize * 1.2f)
let mutable idx = 0

let doc = Document()

while idx < numColors do
    let page = doc.NewPage(-1, float32 w, float32 h)
    for i in 0 .. 9 do
        if idx < numColors then
            for j in 0 .. 9 do
                if idx < numColors then
                    let rect = Rect(float32 (rw * float j), float32 (rh * float i), float32 (rw * float j + rw), float32 (rh * float i + rh))
                    let struct (cname, r, g, b) = myList[idx]
                    let col = [| (float32(r)) / 255.0f; (float32 g) / 255.0f; (float32 b) / 255.0f |]
                    page.DrawRect(rect, col, col) |> ignore
                    let pnt1 = rect.TopLeft + Point(0.0f, float32 (rh * 0.3))
                    let pnt2 = pnt1 + Point(0.0f, lheight)
                    let _ = page.InsertText(pnt1, cname, fsize, lheight, "Atop", "e:/res/apo.ttf", color = white)
                    let _ = page.InsertText(pnt2, cname, fsize, lheight, "Atop", "e:/res/apo.ttf", color = black)
                    idx <- idx + 1
            done

    done

let m = dict [
    ("author", "Green")
    ("producer", "MuPDF.NET")
    ("creator", "PrintRGB")
    ("creationDate", Utils.GetPdfNow())
    ("modDate", Utils.GetPdfNow())
    ("title", "MuPDF.NET Color Database")
    ("subject", "RGB values")
]

doc.SetMetadata(new Dictionary<string, string>(m))
doc.Save("output.pdf")
