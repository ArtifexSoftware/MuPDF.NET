Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim pvon As Func(Of Double, (Single, Single)) = Function(a As Double) (CSng(Math.Cos(a)), CSng(Math.Sin(a)))

        Dim pbis As Func(Of Double, (Single, Single)) = Function(a As Double) (CSng(Math.Cos(3 * a - Math.PI)), CSng(Math.Sin(3 * a - Math.PI)))

        Dim prefix As String = "output"
        Dim coffee As Single() = Utils.GetColor("coffee")
        Dim yellow As Single() = Utils.GetColor("yellow")
        Dim blue As Single() = Utils.GetColor("blue")

        Dim doc As New Document()
        Dim page As Page = doc.NewPage(-1, 800, 800)
        Dim center As New Point(page.Rect.Width / 2, page.Rect.Height / 2)

        Dim radius As Single = page.Rect.Width / 2

        Dim img As Shape = page.NewShape()
        img.DrawCircle(center, radius)
        img.Finish(color:=coffee, fill:=coffee)

        Dim count As Integer = 200
        Dim interval As Double = Math.PI / count
        For i As Integer = 1 To count - 1
            Dim a As Double = -Math.PI / 2 + i * interval

            Dim von As (x As Single, y As Single) = pvon(a)
            Dim vonPoint As New Point(von.x, von.y)
            vonPoint = vonPoint * radius + center

            Dim bis As (x As Single, y As Single) = pbis(a)
            Dim bisPoint As New Point(bis.x, bis.y)
            bisPoint = bisPoint * radius + center
            img.DrawLine(vonPoint, bisPoint)
        Next

        img.Finish(width:=1, color:=yellow, closePath:=True)

        img.DrawCircle(center, radius)
        img.Finish(color:=blue)
        page.SetCropBox(img.Rect)
        img.Commit()

        doc.Save(prefix & ".pdf")
    End Sub
End Module
