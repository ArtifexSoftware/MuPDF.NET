Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim myList As List(Of (String, Integer, Integer, Integer)) = Utils.GetColorInfoList()

        Dim w As Integer = 800
        Dim h As Integer = 600
        Dim rw As Integer = 80
        Dim rh As Integer = 60
        Dim numColors As Integer = myList.Count

        Dim black() As Single = {0.0F, 0.0F, 0.0F}
        Dim white() As Single = {1.0F, 1.0F, 1.0F}
        Dim fsize As Single = 8
        Dim lheight As Single = fsize * 1.2F
        Dim idx As Integer = 0
        Dim doc As New Document()

        While idx < numColors
            doc.InsertPage(-1, w, h)
            Dim page As Page = doc(-1)
            For i As Integer = 0 To 9
                If idx >= numColors Then
                    Exit For
                End If
                For j As Integer = 0 To 9
                    Dim rect As New Rect(rw * j, rh * i, rw * j + rw, rh * i + rh)
                    Dim cname As String = myList(idx).Item1.ToLower()
                    Dim col() As Single = {myList(idx).Item2 / 255.0F, myList(idx).Item3 / 255.0F, myList(idx).Item4 / 255.0F}

                    page.DrawRect(rect, col, col)
                    Dim pnt1 As Point = rect.TopLeft + New Point(0, rh * 0.3F)
                    Dim pnt2 As Point = pnt1 + New Point(0, lheight)
                    page.InsertText(pnt1, cname, fsize, color:=white, fontName:="Atop", fontFile:="e://res/apo.ttf")
                    page.InsertText(pnt2, cname, fsize, color:=black, fontName:="Atop", fontFile:="e://res/apo.ttf")
                    idx += 1
                    If idx >= numColors Then
                        Exit For
                    End If
                Next j
            Next i
        End While

        Dim m As New Dictionary(Of String, String) From {
    {"author", "Green"},
    {"producer", "MuPDF.NET"},
    {"creator", "Examples/PrintHSV"},
    {"creationDate", Utils.GetPdfNow()},
    {"modDate", Utils.GetPdfNow()},
    {"title", "MuPDF.NET Color Database"},
    {"subject", "Sorted down by HSV values"}
}

        doc.SetMetadata(m)
        doc.Save("e://res/output.pdf")
    End Sub
End Module
