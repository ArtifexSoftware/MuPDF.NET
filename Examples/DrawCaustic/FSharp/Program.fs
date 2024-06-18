// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET
open System

let pvon (a:float) =
    ((float)(Math.Cos(a)), (float)(Math.Sin(a)))

let pbis (a:float) =
    ((float)(Math.Cos(3.0 * a - Math.PI)), (float)(Math.Sin(3.0 * a - Math.PI)))

let prefix = "output"
let coffee = Utils.GetColor("coffee")
let yellow = Utils.GetColor("yellow")
let blue = Utils.GetColor("blue")

let doc = new Document()
let page = doc.NewPage(-1, 800f, 800f)
let center = new Point(page.Rect.Width / 2f, page.Rect.Height / 2f)

let radius = page.Rect.Width / 2f

let img = page.NewShape()
img.DrawCircle(center, radius) |> ignore
img.Finish(color = coffee, fill = coffee)

let count = 200
let interval = Math.PI / float count
for i in 1 .. count do
    let a = -Math.PI / 2.0 + float i * interval 

    let (x, y) = pvon a
    let von = new Point((float32)x, (float32)y) * radius + center

    let (x, y) = pbis a
    let bis = new Point((float32)x, (float32)y) * radius + center
    img.DrawLine(von, bis) |> ignore

img.Finish(width = 1f, color = yellow, closePath = true)

img.DrawCircle(center, radius) |> ignore
img.Finish(color = blue)
page.SetCropBox(img.Rect)
img.Commit()

doc.Save(prefix + ".pdf")
