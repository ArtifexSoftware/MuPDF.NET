// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET
open System
open System.IO

[<EntryPoint>]
let main(args) =
    let doc = new Document(args.[0])
    let embed = doc.GetEmbfile(0)
    File.WriteAllBytes("../../../../output.jpg", embed)
    0