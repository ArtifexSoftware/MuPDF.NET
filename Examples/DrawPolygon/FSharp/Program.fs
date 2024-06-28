// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET
open System.IO

let doc = Document()
let page = doc.NewPage()
let img = page.NewShape()

let nedge = 5
let breadth = 2
let beta = -1.0f * 360.0f / (float32)nedge
let center = Point(300f, 300f)
let p0 = Point(300f, 200f)
let mutable p1 = p0
let points = ResizeArray<Point>() 
points.Add p0

for i in 0..nedge - 2 do
    p1 <- img.DrawSector(center, p1, beta)
    points.Add p1

img.DrawCont <- ""

points.Add p1
for i in 0..nedge - 1 do
    img.DrawSquiggle(points.[i], points.[i + 1], (float32)breadth) |> ignore

img.Finish(color = [|0f; 0f; 1f|], fill = [|1f; 1f; 0f|], closePath = false)
page.SetCropBox(img.Rect)
img.Commit()

doc.Save("output.pdf")

File.WriteAllText("output.svg", page.GetSvgImage())
