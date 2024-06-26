// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET;
open System.IO;
open System.Collections.Generic;

let doc = new Document("input.pdf")
let lines: string[] = File.ReadAllLines("input.csv")
let mutable toc: List<Toc> = new List<Toc>()

for line in lines do
    let rows: string[] = line.Split(';')

    let mutable t: Toc = new Toc()
    t.Level <- System.Int32.Parse(rows[0])
    t.Title <- rows[1]
    t.Page <- System.Int32.Parse(rows[2])
    t.Link <- System.Single.Parse(rows[3])
    toc.Add(t)

doc.SetToc(toc)
doc.SaveIncremental()
