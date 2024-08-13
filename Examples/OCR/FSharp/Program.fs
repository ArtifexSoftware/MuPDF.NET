// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET
open System.Text


let GetTessOCR (page:Page) (bbox:Rect) =
    let mat = new IdentityMatrix(4f, 4f)
    let pix = page.GetPixmap(colorSpace = Utils.csGRAY.Name, matrix = mat, clip = bbox)
    let text = pix.PdfOCR2Bytes()
    Encoding.UTF8.GetString(text)

let doc = new Document("v110-changes.pdf")
let mutable ocrCount = 0
for i in 0 .. doc.PageCount - 1 do
    let page = doc.[i]
    let blocks = (page.GetText("dict", flags = 0) :?> PageInfo).Blocks
    for block in blocks do
        for line in block.Lines do
            for span in line.Spans do
                let mutable text = span.Text
                if text.Contains(char 65533) then
                    ocrCount <- ocrCount + 1
                    let text1 = text.TrimStart()
                    let sb = new string(' ', text.Length - text1.Length)
                    let text1 = text.TrimEnd()
                    let sa = new string(' ', text.Length - text1.Length)
                    let newText = sb + GetTessOCR page span.Bbox + sa
                    0
                else 0
