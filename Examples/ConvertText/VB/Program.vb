Imports System.IO
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim lineCtr As Integer = 0
        Dim totalCtr As Integer = 0
        Dim outCtr As Integer = 0
        Dim outBuf As String = ""

        Dim doc As New Document()

        Dim size As (width As Single, height As Single) = Utils.PageSize("a4")
        Dim fontsize As Integer = 10
        Dim lineHeight As Single = fontsize * 1.2F
        Dim nlines As Integer = CInt((size.height - 108.0F) / lineHeight)

        For Each line As String In File.ReadLines("e://res/input.txt")
            outBuf &= line + "\n"
            lineCtr += 1
            totalCtr += 1
            If lineCtr = nlines Then
                outCtr += PageOut(doc, outBuf)
                outBuf = ""
                lineCtr = 0
            End If
        Next

        If outBuf.Length > 0 Then
            outCtr += PageOut(doc, outBuf)
        End If

        Dim hFontsz As Integer = 16
        Dim fFontsz As Integer = 8
        Dim blue() As Single = {0, 0, 1.0F}
        Dim pspace As Integer = 500

        For i As Integer = 0 To doc.PageCount - 1
            Dim page = doc(i)
            Dim footer As String = $"{page.Number + 1} ({doc.PageCount})"
            Dim plenftr As Single = Utils.GetTextLength(footer, fontsize:=fFontsz, fontname:="Kenpixel")
            page.InsertText(New Point(50, 50), "input.txt", color:=blue, fontSize:=hFontsz, fontFile:="e://res/kenpixel.ttf", fontName:="Kenpixel")
            page.DrawLine(New Point(50, 60), New Point(50 + pspace, 60), color:=blue, width:=0.5F)
            page.DrawLine(New Point(50, size.height - 33), New Point(50 + pspace, size.height - 33), color:=blue, width:=0.5F)
            page.InsertText(New Point(50 + pspace - plenftr, size.height - 33 + fFontsz * 1.2F), footer, fontSize:=fFontsz, color:=blue, fontFile:="e://res/kenpixel.ttf", fontName:="Kenpixel")
            page.CleanContetns()
        Next

        doc.SetMetadata(New Dictionary(Of String, String)() From {
            {"creationDate", Utils.GetPdfNow()},
            {"modDate", Utils.GetPdfNow()},
            {"creator", "convert"},
            {"producer", "MuPDF.NET"}
        })

        doc.Save("e://res/output.pdf", garbage:=4, pretty:=1)
    End Sub

    Function PageOut(doc As Document, b As String) As Integer
        Dim size As (width As Single, height As Single) = Utils.PageSize("a4")
        Dim fontsize As Integer = 10
        Dim page = doc.NewPage(width:=Size.Width, height:=Size.Height)
        Return page.InsertText(New Point(50, 72), text:=b, fontSize:=fontsize, fontFile:="e://res/kenpixel.ttf")
    End Function
End Module
