Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim w As Single = 150.0F
        Dim h As Double = 0.5 * Math.Sqrt(3) * w

        Dim doc As New Document()
        Dim page As Page = doc.NewPage(-1, w, CSng(h))
        Dim color() As Single = {0, 0, 1}
        Dim fill() As Single = Utils.GetColor("papayawhip")
        Dim shape As Shape = page.NewShape()


        Dim a As Point = Page.Rect.BottomLeft + New Point(5, -5)
        Dim b As Point = Page.Rect.BottomRight + New Point(-5, -5)
        Dim x As Single = (b.X - a.X) * 0.5F
        Dim y As Single = CSng((a.Y - x * Math.Sqrt(3)))
        Dim c As New Point(x, y)

        shape.DrawPolyline(New Point(2) {a, b, c})
        shape.Finish(fill:=color, closePath:=True)

        Dim tc As Integer = 0
        tc = triangle(shape, a, b, c, fill, tc)

        shape.Commit()
        Console.WriteLine(shape.DrawCont)
        doc.Save("output.pdf", deflate:=1)
    End Sub

    Function triangle(shape As Shape, a As Point, b As Point, c As Point, fill() As Single, tc As Integer) As Integer
        If Math.Abs(a.X - b.X) + Math.Abs(b.Y - a.Y) < 1.0F Then
            Return tc
        End If
        Dim ab As Point = a + (b - a) * 0.5F
        Dim ac As Point = a + (c - a) * 0.5F
        Dim bc As Point = b + (c - b) * 0.5F
        shape.DrawPolyline(New Point(2) {ab, ac, bc})
        shape.Finish(fill:=fill, closePath:=True)

        tc += 1
        tc = triangle(shape, a, ab, ac, fill, tc)
        tc = triangle(shape, ab, b, bc, fill, tc)
        tc = triangle(shape, ac, bc, c, fill, tc)
        Return tc
    End Function
End Module
