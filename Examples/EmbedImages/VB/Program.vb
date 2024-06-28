Imports System
Imports System.IO
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim doc As New Document()
        Dim pageSize As (Width As Single, Height As Single) = Utils.PageSize("a4")
        Dim rect As New Rect(0, 0, pageSize.Width, pageSize.Height)
        rect += New Rect(36, 36, -36, -36)

        Dim list As String() = Directory.GetFiles("img")
        Dim n As Integer = list.Length

        For i As Integer = 0 To n - 1
            If Not File.Exists(list(i)) Then
                Continue For
            End If

            Dim img As Byte() = File.ReadAllBytes(list(i))
            doc.AddEmbfile(list(i), img, filename:=list(i), ufilename:=list(i), desc:=list(i))
        Next

        Dim page As Page = doc.NewPage()
        doc.Save("output.pdf")
    End Sub
End Module
