Imports System
Imports System.IO
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim doc As New Document()
        Dim page As Page = doc.NewPage()
        Dim img As Shape = page.NewShape()

        Dim nedge As Integer = 5
        Dim breadth As Integer = 2
        Dim beta As Single = -1.0F * 360 / nedge
        Dim center As New Point(300, 300)
        Dim p0 As New Point(300, 200)
        Dim p1 As Point = p0
        Dim points As New List(Of Point) From {p0}

        For i As Integer = 0 To nedge - 2
            p0 = img.DrawSector(center, p0, beta)
            points.Add(p0)
        Next

        img.DrawCont = ""

        points.Add(p1)
        For i As Integer = 0 To nedge - 1
            img.DrawSquiggle(points(i), points(i + 1), breadth:=breadth)
        Next

        img.Finish(color:=New Single() {0F, 0F, 1.0F}, fill:=New Single() {1, 1, 0}, closePath:=False)
        page.SetCropBox(img.Rect)
        img.Commit()

        doc.Save("output.pdf")

        File.WriteAllText("output.svg", page.GetSvgImage())
    End Sub
End Module
