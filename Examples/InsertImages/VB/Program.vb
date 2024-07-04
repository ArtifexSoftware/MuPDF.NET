Imports System
Imports System.IO
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim doc As New Document()

        Dim list As String() = Directory.GetFiles("./img")

        For Each file As String In list
            If Not System.IO.File.Exists(file) Then
                Continue For
            End If

            Dim img As New Document(file)
            Dim rect As Rect = img(0).Rect
            Console.WriteLine(img.PageCount)
            Dim pdfbytes As Byte() = img.Convert2Pdf()
            img.Close()

            Dim imgPdf As New Document("pdf", pdfbytes)
            Dim page As Page = doc.NewPage(width:=rect.Width, height:=rect.Height)
            page.ShowPdfPage(rect, imgPdf, 0)
        Next

        doc.Save("output.pdf")
    End Sub
End Module
