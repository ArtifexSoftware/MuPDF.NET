// For more information see https://aka.ms/fsharp-console-apps
open System
open System.Collections.Generic
open MuPDF.NET

let doc = Document("../../../../input.epub")

if doc.IsPDF then
    raise (Exception("document is PDF already"))

let b = doc.Convert2Pdf()
let pdf = Document("pdf", b)

let toc = doc.GetToc()
pdf.SetToc(toc) |> ignore

let mutable meta = doc.MetaData
if meta.GetValueOrDefault("producer", null) <> null then
    meta.["producer"] <- "MuPDF.NET v2.0.8-alpha"

if meta.GetValueOrDefault("creator", null) <> null then
    meta.["creator"] <- "MuPDF.NET PDF Converter"

pdf.SetMetadata(meta)

let mutable linkCnt = 0
let mutable linkSkip = 0
for i in 0 .. doc.PageCount - 1 do
    let page = doc.Item(i)
    let links = page.GetLinks()
    linkCnt <- linkCnt + links.Count
    let pOut = pdf.Item(i)
    for l in links do
        if l.Kind = LinkType.LINK_NAMED then
            linkSkip <- linkSkip + 1
        else
            pOut.InsertLink(l)

pdf.Save("output.pdf", 4, 1)
