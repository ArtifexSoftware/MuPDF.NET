Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim doc As New Document()
        Dim page As Page = doc.NewPage(width:=500, height:=500)
        Dim center As Point = (page.Rect.TopLeft + page.Rect.BottomRight) / 2.0F
        Dim radius As Single = 200.0F
        Dim n As Integer = 523
        Dim curve As Integer = 2

        Dim p0 As Point = center - New Point(radius, 0)
        Dim theta As Single = -360.0F / n

        Dim stroke As Single() = New Single(2) {1, 0, 0}
        Dim fill As Single() = New Single(2) {0, 1, 0}
        Dim border As Single() = New Single(2) {0, 0, 1}

        Dim shape As Shape = page.NewShape()
        shape.DrawCircle(center, radius)
        shape.Finish(color:=border, fill:=fill, width:=1)

        Dim points As New List(Of Point)(New Point() {p0})
        Dim point As Point = p0
        For i As Integer = 1 To n - 1
            point = shape.DrawSector(center, point, theta, True)
            points.Add(point)
        Next

        shape.DrawCont = ""

        For i As Integer = 0 To n - 1
            Dim tar As Integer = curve * i Mod n
            shape.DrawLine(points(i), points(tar))
        Next

        shape.Finish(color:=stroke, width:=0.2F)
        shape.Commit()
        doc.Save("output.pdf", deflate:=1)
    End Sub
End Module
