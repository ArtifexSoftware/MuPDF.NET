Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim fn As String = "input.pdf"
        Dim pattern As String = "input"

        Dim src As New Document(fn)
        For i As Integer = 0 To src.PageCount - 1
            Dim doc As New Document()
            doc.InsertPdf(src, fromPage:=i, toPage:=i)
            doc.Save($"./output/{pattern}-{i}.pdf")
            doc.Close()
        Next
    End Sub
End Module
