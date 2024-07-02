Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim src As New Document("input.pdf")
        Dim doc As New Document()

        For i As Integer = 0 To src.PageCount - 1
            Dim spage As Page = src(i)
            Dim xref As Integer = 0
            Dim r As Rect = spage.Rect
            Dim d As New Rect(spage.CropBoxPosition, spage.CropBoxPosition)

            Dim r1 As Rect = r * 0.5F
            Dim r2 As Rect = r1 + New Rect(r1.Width, 0, r1.Width, 0)
            Dim r3 As Rect = r1 + New Rect(0, r1.Height, 0, r1.Height)
            Dim r4 As Rect = New Rect(r1.BottomRight, r.BottomRight)
            Dim rectList As Rect() = {r1, r2, r3, r4}

            For Each rr As Rect In rectList
                Dim rx As Rect = rr + d
                Dim page As Page = doc.NewPage(-1, rx.Width, rx.Height)
                xref = page.ShowPdfPage(page.Rect, src, spage.Number, clip:=rx)
            Next
        Next

        doc.Save("output.pdf", 4, 1)
    End Sub
End Module
