// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET
open System.Text

let RemoveTxt (cont: string) =
    let cont1 = cont.Replace("\n", " ")
    let ct = cont1.Split(' ')
    let mutable intext = false
    let nct = ResizeArray<string>()
    for word in ct do
        if word = "ET" then
            intext <- false
            ()
        elif word = "BT" then
            intext <- true
            ()
        elif intext then
            ()
        else
            nct.Add(word)
    let ncont = String.concat " " nct
    ncont

let doc = Document("input.pdf")
doc.SetMetadata()
doc.DeleteXmlMetadata()

for i in 0 .. doc.PageCount - 1 do
    let page = doc.[i]
    let xrefList = page.GetContents()
    for xref in xrefList do
        let cont = doc.GetXrefStream(xref)
        let ncont = RemoveTxt(Encoding.UTF8.GetString(cont))
        doc.UpdateStream(xref, Encoding.UTF8.GetBytes(ncont))

doc.Save("output.pdf", 1, 4)
