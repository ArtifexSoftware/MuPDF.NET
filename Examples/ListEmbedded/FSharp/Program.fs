// For more information see https://aka.ms/fsharp-console-apps
open System
open MuPDF.NET

let doc = Document("input.pdf")

let mutable nameLen = 0
let mutable filenameLen = 0
let mutable totalLen = 0
let mutable totalSize = 0

let mutable efList: (string * string * int * int) list = []

for i in 0 .. doc.GetEmbfileCount() - 1 do
    let info = doc.GetEmbfileInfo(i)
    efList = efList @ [(info.Name, info.FileName, info.Length, info.Size)] |> ignore
    nameLen <- max info.Name.Length nameLen
    filenameLen <- max info.FileName.Length filenameLen
    totalLen <- totalLen + info.Length
    totalSize <- totalSize + info.Size

if efList.Length < 1 then
    Console.WriteLine("no embedded files in input.pdf")

let ratio = float totalSize / float totalLen
let saves = 1.0 - ratio

let header = String.Format("{0,-" + (nameLen + 4).ToString() + "}{1,-" + (filenameLen + 4).ToString() + "}{2,10}{3,11}",
    "Name", "Filename", "Length", "Size")

let line = String('-', header.Length)

for info in efList do
    Console.WriteLine(String.Format("{0,-" + (nameLen + 3).ToString() + "}{1,-" + (filenameLen + 3).ToString() + "}{2,10}{3,10}",
        info.Item1, info.Item2, info.Item3, info.Item4))

printfn "%A embedded files in 'input.pdf'. Totals:" efList.Length

printfn "File lengths: %A, compressed: %A, ratio: %A%% (savings: %A%%)." totalLen totalSize (Math.Round(ratio * 100.0, 2)) (Math.Round(saves * 100.0, 2))

