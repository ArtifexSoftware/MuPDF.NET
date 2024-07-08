Imports System
Imports mupdf.NET
Imports System.Text

Module Program
    Sub Main(args As String())
        Dim doc As New Document("input.pdf")
        Dim page As Page = doc(0)

        Dim images As List(Of Entry) = page.GetImages()
        Dim oldXref As Integer = images(0).Xref

        Dim pix As New Pixmap(Utils.csGRAY, New IRect(0, 0, 1, 1), 1)
        pix.ClearWith()

        Dim newXref As Integer = page.InsertImage(page.Rect, pixmap:=pix)
        doc.CopyXref(newXref, oldXref)

        Dim contents As List(Of Integer) = page.GetContents()
        Dim lastXref As Integer = contents.Last()

        doc.UpdateStream(lastXref, Encoding.UTF8.GetBytes(" "))

        doc.Save("e://res/output.pdf")
    End Sub
End Module
