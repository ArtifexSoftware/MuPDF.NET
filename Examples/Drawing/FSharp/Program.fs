// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET
open System

let doc = Document()
let red = [| 1.0f; 0.0f; 0.0f |]
let blue = [| 0.0f; 0.0f; 1.0f |]
let page = doc.NewPage(width = 400f, height = 300f)
let r = page.Rect + Rect(4.0f, 4.0f, -4.0f, -4.0f)
let q = r.Quad
let f = 0.0f / 100.0f

let u, o =
    if f >= 0.0f then f, 0.0f
    else 0.0f, -f

let q1 = Quad(
    (q.UpperLeft + (q.UpperRight - q.UpperLeft) * o),
    (q.UpperLeft + (q.UpperRight - q.UpperLeft) * (1.0f - o)),
    (q.LowerLeft + (q.LowerRight - q.LowerLeft) * u),
    (q.LowerLeft + (q.LowerRight - q.LowerLeft) * (1.0f - u))
)

let c1 = Math.Min(1.0f, Math.Max(o, u))
let c3 = Math.Min(1.0f, Math.Max(1.0f - u, 1.0f - o))
let fill = [| c1; 0.0f; c3 |]
let img = page.NewShape()
let _ = img.DrawOval(q1)
img.Finish(color = blue, fill = fill, width = 0.3f)

let _ = img.DrawCircle(q1.LowerLeft, 4.0f)
let _ = img.DrawCircle(q1.UpperLeft, 4.0f)
img.Finish(fill = red)

let _ = img.DrawCircle(q1.UpperRight, 4.0f)
let _ = img.DrawCircle(q1.LowerRight, 4.0f)
img.Finish(fill = blue)
img.Commit()

doc.Save("output.pdf")
