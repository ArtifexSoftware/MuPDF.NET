Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim doc As New Document("../../../../example.pdf")
        Dim tocs As List(Of Toc) = doc.GetToc(False)

        For i As Integer = 0 To tocs.Count - 1
            Dim item As Toc = tocs(i)
            Dim dest As LinkInfo = item.Link
            dest.Collapse = False
            If item.Level = 1 Then
                dest.Color = New Single(2) {1.0F, 0F, 0F}
                dest.Bold = True
                dest.Italic = False
            ElseIf item.Level = 2 Then
                dest.Color = New Single(2) {0F, 0F, 1.0F}
                dest.Bold = False
                dest.Italic = True
            Else
                dest.Color = New Single(2) {0F, 1.0F, 0F}
                dest.Bold = False
                dest.Italic = False
            End If
            doc.SetTocItem(i, dest)
        Next
        doc.Save("../../../../new-toc.pdf")
    End Sub
End Module
