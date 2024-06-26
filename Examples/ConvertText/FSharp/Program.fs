// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET
open System.IO
open System.Collections.Generic;

let mutable lineCtr = 0
let mutable totalCtr = 0
let mutable outCtr = 0
let mutable outBuf = ""

let doc = Document()

let struct (width, height) = Utils.PageSize("a4")

let fontsize = 10f
let lineHeight = fontsize * 1.2f
let nlines = (int)(((float32)height - 108.0f) / lineHeight)

let PageOut b =
    let page = doc.NewPage(width = (float32)width, height = (float32)height)
    page.InsertText(Point(50f, 72f), text = b, fontSize = (float32)fontsize, fontFile = "e://res/kenpixel.ttf", fontName = "Kenpixel")

for line in File.ReadLines("e://res/input.txt") do
    outBuf <- outBuf + line + "\n"
    lineCtr <- lineCtr + 1
    totalCtr <- totalCtr + 1
    if lineCtr = nlines then
        outCtr <- outCtr + PageOut outBuf
        outBuf <- ""
        lineCtr <- 0

if outBuf.Length > 0 then
    outCtr <- outCtr + PageOut outBuf

let hFontsz = 16f
let fFontsz = 8f
let blue = [|0.0f; 0.0f; 1.0f|]
let pspace = 500f

for i in 0 .. doc.PageCount - 1 do
    let page: Page = doc.[i]
    let footer = $"{(page.Number + 1)} ({doc.PageCount})"
    let plenftr = Utils.GetTextLength(footer, fontSize = fFontsz, fontName = "Kenpixel")
    let _ = page.InsertText(Point(50f, 50f), "input.txt", color = blue, fontSize = hFontsz, fontFile = "e://res/kenpixel.ttf", fontName = "Kenpixel")
    let _ = page.DrawLine(Point(50f, 60f), Point(50f + pspace, 60f), color = blue, width = 0.5f)
    let _ = page.DrawLine(Point(50f, (float32)height - 33f), Point(50f + pspace, (float32)height - 33f), color = blue, width = 0.5f)
    let _ = page.InsertText(Point(50f + pspace - plenftr, (float32)height - 33f + fFontsz * 1.2f), footer, fontSize = fFontsz, color = blue, fontFile = "e://res/kenpixel.ttf", fontName = "Kenpixel")
    page.CleanContetns()

let metadata: Dictionary<string, string> = Dictionary<string, string>(dict [
    ("creationDate", Utils.GetPdfNow())
    ("modDate", Utils.GetPdfNow())
    ("creator", "convert")
    ("producer", "MuPDF.NET")
])

doc.SetMetadata(metadata)

doc.Save("e://res/output.pdf", garbage = 4, pretty = 1)
