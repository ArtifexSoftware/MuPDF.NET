// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET
open System
open System.Text
open System.Collections.Generic

let infile = "input.pdf"
let outfile = "output.pdf"

let src = Document(infile)
let doc = Document()

let mutable total = 0
let xrefs = List<int>()

for i in 0 .. src.PageCount - 1 do
    let mutable count = 0
    let xobjs = src.GetPageXObjects(i)
    for xobj in xobjs do
        if xobj.StreamXref = 0 then
            let bbox = xobj.Bbox
            if bbox.IsInfinite = false then
                if xrefs.Contains(xobj.Xref) = false then
                    xrefs.Add(xobj.Xref)

                    doc.InsertPdf(src, fromPage=i, toPage=i, rotate=0)
                    let refName = xobj.RefName
                    let refcmd = Encoding.UTF8.GetBytes(sprintf "/%s Do" refName)
                    let page = doc.[doc.PageCount - 1]
                    page.SetMediaBox(bbox)
                    page.CleanContetns()
                    let xref = page.GetContents().[0]
                    doc.UpdateStream(xref, refcmd)
                    count <- count + 1
        
    if count > 0 then
        Console.WriteLine(count)
    total <- total + count

if total > 0 then
    doc.Save(outfile, garbage=4, deflate=1)
