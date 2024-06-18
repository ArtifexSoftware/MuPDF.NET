// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET

let doc = Document()
let page: Page = doc.NewPage(width = 500f, height = 500f)
let center = (page.Rect.TopLeft + page.Rect.BottomRight) / 2.0f
let radius = 200.0f
let n = 523
let curve = 2

let p0 = center - Point(radius, 0f)
let theta = -360.0f / (float32)n

let stroke = [| 1.0f; 0.0f; 0.0f |]
let fill = [| 0.0f; 1.0f; 0.0f |]
let border = [| 0.0f; 0.0f; 1.0f |]

let shape = page.NewShape()
shape.DrawCircle(center, radius) |> ignore
shape.Finish(color = border, fill = fill, width = 1f) |> ignore

let points = new ResizeArray<Point>([| p0 |])
let mutable point = p0
for i in 1 .. n - 1 do
    point <- shape.DrawSector(center, point, theta, true)
    points.Add(point) |> ignore

shape.DrawCont <- ""

for i in 0 .. n - 1 do
    let tar = curve * i % n
    shape.DrawLine(points.[i], points.[tar]) |> ignore

shape.Finish(color = stroke, width = 0.2f) |> ignore
shape.Commit() |> ignore
doc.Save("e://res/output.pdf", deflate = 1) |> ignore
