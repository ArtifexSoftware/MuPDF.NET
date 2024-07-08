// For more information see https://aka.ms/fsharp-console-apps
open System.IO
open MuPDF.NET

let doc = Document()

let list = Directory.GetFiles("./img")

for file in list do
    if not (File.Exists(file)) then
        ()
    else
        let img = Document(file)
        let rect = img.[0].Rect
        printfn "%d" img.PageCount
        let pdfbytes = img.Convert2Pdf()
        img.Close()

        let imgPdf = Document("pdf", pdfbytes)
        let page = doc.NewPage(width = rect.Width, height = rect.Height)
        page.ShowPdfPage(rect, imgPdf, 0) |> ignore

doc.Save("output.pdf")
