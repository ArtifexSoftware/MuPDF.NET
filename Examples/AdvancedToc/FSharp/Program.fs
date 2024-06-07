// For more information see https://aka.ms/fsharp-console-apps

open MuPDF.NET

let doc = new Document("../../../../example.pdf")
let toc = doc.GetToc(false)
let mutable i = -1

for item in toc do
    i <- i + 1
    let lvl = item.Level
    let ddict = item.Link
    ddict.Collapse <- false
    match lvl with
    | 1 -> 
        ddict.Color <- [|1f; 0f; 0f|]
        ddict.Bold <- true
        ddict.Italic <- false
    | 2 -> 
        ddict.Color <- [|0f; 0f; 1f|]
        ddict.Bold <- false
        ddict.Italic <- true
    | _ -> 
        ddict.Color <- [|0f; 1f; 0f|]
        ddict.Bold <- false
        ddict.Italic <- false

    printfn "%A" ddict
    doc.SetTocItem(i, ddict) // assuming that set_toc_item's first argument is a optional destination and second is dictionary
doc.Save("../../../../new-toc.pdf") 
