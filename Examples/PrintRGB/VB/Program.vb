Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim myList As List(Of (String, Integer, Integer, Integer)) = Utils.GetColorInfoList()

        Dim w As Single = 800
        Dim h As Single = 600
        Dim rw As Single = 80
        Dim rh As Single = 60

        Dim numColors As Integer = myList.Count
        Dim black() As Single = Utils.GetColor("black")
        Dim white() As Single = Utils.GetColor("white")
        Dim fsize As Single = 8
        Dim lheight As Single = fsize * 1.2F
        Dim idx As Integer = 0

        Dim doc As New Document()

        While idx < numColors
            Dim page As Page = doc.NewPage(-1, w, h)
            For i As Integer = 0 To 9
                If idx >= numColors Then
                    Exit For
                End If
                For j As Integer = 0 To 9
                    Dim rect As New Rect(rw * j, rh * i, rw * j + rw, rh * i + rh)
                    Dim cname As String = myList(idx).Item1.ToLower()
                    Dim col() As Single = New Single(2) {myList(idx).Item2 / 255.0F, myList(idx).Item3 / 255.0F, myList(idx).Item4 / 255.0F}
                    page.DrawRect(rect, col, col)
                    Dim pnt1 As Point = rect.TopLeft + New Point(0, rh * 0.3F)
                    Dim pnt2 As Point = pnt1 + New Point(0, lheight)
                    page.InsertText(pnt1, cname, fsize, lheight, "Atop", "e:/res/apo.ttf", color:=white)
                    page.InsertText(pnt2, cname, fsize, lheight, "Atop", "e:/res/apo.ttf", color:=black)
                    idx += 1

                    If idx >= numColors Then
                        Exit For
                    End If
                Next
            Next
        End While

        Dim m As New Dictionary(Of String, String) From {
            {"author", "Green"},
            {"producer", "MuPDF.NET"},
            {"creator", "PrintRGB"},
            {"creationDate", Utils.GetPdfNow()},
            {"modDate", Utils.GetPdfNow()},
            {"title", "MuPDF.NET Color Database"},
            {"subject", "RGB values"}
        }

        doc.SetMetadata(m)
        doc.Save("output.pdf")
    End Sub
End Module
