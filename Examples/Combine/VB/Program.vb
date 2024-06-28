Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim src As New Document("input.pdf")
        Dim doc As New Document()

        Dim pageSize As (width As Integer, height As Integer) = Utils.PageSize("a4")
        Dim r As New Rect(0, 0, pageSize.width, pageSize.height)

        Dim r1 As Rect = r * 0.5F
        Console.WriteLine(r.BottomRight.ToString())
        Console.WriteLine(r1.BottomRight.ToString())
        Dim r2 As Rect = r1 + New Rect(r1.Width, 0, r1.Width, 0)
        Dim r3 As Rect = r1 + New Rect(0, r1.Height, 0, r1.Height)
        Dim r4 As New Rect(r1.BottomRight, r.BottomRight)

        Dim rTab As Rect() = {r1, r2, r3, r4}
        Dim page As Page = Nothing
        For i As Integer = 0 To src.PageCount - 1
            Dim spage As Page = src(i)
            If spage.Number Mod 4 = 0 Then
                page = doc.NewPage(width:=pageSize.width, height:=pageSize.height)
            End If
            Console.WriteLine($"{i}  " & rTab(spage.Number Mod 4).ToString())
            page.ShowPdfPage(
                   rTab(spage.Number Mod 4),
                   src,
                   spage.Number)
        Next

        doc.Save("output.pdf", garbage:=4, deflate:=1)
    End Sub
End Module
