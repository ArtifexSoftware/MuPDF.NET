// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET

let w = 150f
let h = 0.5 * sqrt 3.0 * float w

let doc = Document()
let page = doc.NewPage(-1, w, (float32)h)
let color = [| 0f; 0f; 1f |]
let fill = Utils.GetColor("papayawhip")
let shape = page.NewShape()

let rec triangle (shape:Shape, a:Point, b: Point,  c: Point, fill, tc) =
    if abs(a.X - b.X) + abs(b.Y - a.Y) < 1.0f then tc
    else
        let ab = a + (b - a) * 0.5f
        let ac = a + (c - a) * 0.5f
        let bc = b + (c - b) * 0.5f
        let _ = shape.DrawPolyline([| ab; ac; bc |])
        shape.Finish(fill=fill, closePath=true)
  
        let tc = tc + 1
        let tc = triangle (shape, a, ab, ac, fill, tc)
        let tc = triangle (shape, ab, b, bc, fill, tc)
        let tc = triangle (shape, ac, bc, c, fill, tc)
        tc

let a: Point = page.Rect.BottomLeft + Point(5f, -5f)
let b: Point = page.Rect.BottomRight + Point(-5f, -5f)
let x = (b.X - a.X) * 0.5f
let y = a.Y - x * sqrt 3.0f
let c: Point = Point(x, y)

let _ = shape.DrawPolyline([| a; b; c |])
shape.Finish(fill=color, closePath=true)

let mutable tc = 0
tc <- triangle (shape, a, b, c, fill, tc)

shape.Commit()
doc.Save("output.pdf", deflate=1)
