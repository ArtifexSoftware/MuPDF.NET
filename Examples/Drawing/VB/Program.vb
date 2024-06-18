Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim doc As New Document()
        Dim red As Single() = New Single(2) {1, 0, 0}
        Dim blue As Single() = New Single(2) {0, 0, 1}
        Dim page As Page = doc.NewPage(width:=400, height:=300)
        Dim r As Rect = page.Rect + New Rect(4, 4, -4, -4)
        Dim q As Quad = r.Quad
        Dim f As Single = 0.0F / 100.0F

        Dim u As Single, o As Single
        If f >= 0 Then
            u = f
            o = 0
        Else
            u = 0
            o = -f
        End If

        Dim q1 As Quad = New Quad(
        q.UpperLeft + (q.UpperRight - q.UpperLeft) * o,
        q.UpperLeft + (q.UpperRight - q.UpperLeft) * (1 - o),
        q.LowerLeft + (q.LowerRight - q.LowerLeft) * u,
        q.LowerLeft + (q.LowerRight - q.LowerLeft) * (1 - u))

        Dim c1 As Single = Math.Min(1, Math.Max(o, u))
        Dim c3 As Single = Math.Min(1, Math.Max(1 - u, 1 - o))
        Dim fill As Single() = New Single(2) {c1, 0, c3}

        Dim img As Shape = page.NewShape()
        img.DrawOval(q1)
        img.Finish(color:=blue, fill:=fill, width:=0.3F)

        img.DrawCircle(q1.LowerLeft, 4)
        img.DrawCircle(q1.UpperLeft, 4)
        img.Finish(fill:=red)

        img.DrawCircle(q1.UpperRight, 4)
        img.DrawCircle(q1.LowerRight, 4)
        img.Finish(fill:=blue)
        img.Commit()

        doc.Save("output.pdf")
    End Sub
End Module
