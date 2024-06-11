// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET

let doc = new Document("../../../../input.pdf")
let dimlimit = 0
let relsize = 0
let abssize = 0

let RecoverFix (doc: Document, item: Entry) = 
    let xref = item.Xref
    let smask = item.Smask

    if smask > 0 then
        let mutable pix0 = new Pixmap(doc.ExtractImage(xref).Image)
        if pix0.Alpha <> 0 then
            pix0 <- new Pixmap(pix0, 0)
        let mask = new Pixmap(doc.ExtractImage(smask).Image)
        let pix = new Pixmap(doc.ExtractImage(xref).Image)

        let ext = 
            if pix0.N > 3 then "pam"
            else "png"

        ImageInfo(Ext = ext, ColorSpace = pix.ColorSpace.N, Image = pix.ToBytes(ext))
    elif doc.GetXrefObject(xref, compressed = 1).Contains("/ColorSpace") then
        let mutable pix = new Pixmap(doc, xref)
        pix <- new Pixmap(Utils.csRGB, pix)
        ImageInfo(Ext = "png", ColorSpace = 3, Image = pix.ToBytes("png"))
    else doc.ExtractImage(xref)

let pageCount = doc.PageCount
let xrefList = []
let imgList = new List<int>()

for i in 0 .. pageCount - 1 do
    let il = doc.GetPageImages(i)
    imgList