Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim src As New Document("logo.png")

        If Not src.IsPDF Then
            Dim pdfbytes As Byte() = src.Convert2Pdf()
            src.Close()
            src = New Document("pdf", pdfbytes)
        End If

        Dim rect As Rect = src(0).Rect
        Console.WriteLine(rect.ToString())
        Dim factor As Single = 25.0F / rect.Height
        rect *= factor

        Dim doc As New Document("input.pdf")
        Dim xref As Integer = 0
        For i As Integer = 0 To doc.PageCount - 1
            xref = doc(i).ShowPdfPage(rect, src, 0, overlay:=False)
        Next

        doc.Save("output.pdf", garbage:=4)
    End Sub
End Module
