// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET
open System.Text

let src = Document("input.pdf")
let dst = Document("output.pdf")

for i in 0 .. (src.GetEmbfileCount() - 1) do
    let d = src.GetEmbfileInfo(i)
    let b = src.GetEmbfile(i)
    dst.AddEmbfile(Encoding.UTF8.GetString(b), Encoding.UTF8.GetBytes(d.FileName), d.UFileName, d.Desc)


dst.SaveIncremental()
