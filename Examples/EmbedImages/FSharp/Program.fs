// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET
open System.IO

let doc = Document()
let struct (width, height) = Utils.PageSize("a4")
let rect = Rect(0.0f, 0.0f, (float32)width, (float32)height) + Rect(36.0f, 36.0f, -36.0f, -36.0f)

let list = Directory.GetFiles("img")
let n = list.Length

for i in 0 .. n-1 do
    if (File.Exists(list.[i])) then   
        let img = File.ReadAllBytes(list.[i])
        doc.AddEmbfile(list.[i], img, filename = list.[i], ufilename = list.[i], desc = list.[i]) |> ignore

let page = doc.NewPage()
doc.Save("output.pdf")
