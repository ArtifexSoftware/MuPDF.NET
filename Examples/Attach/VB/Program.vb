Imports System
Imports System.IO
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim doc As New Document()
        Dim pageSize As (Integer, Integer) = Utils.PageSize("a6-l")
        Dim width As Integer = pageSize.Item1
        Dim height As Integer = pageSize.Item2
        Dim page As Page = doc.NewPage(width:=width, height:=height)
        Dim rect As New Rect(36, 36, width - 36, height - 36)

        Dim imgList() As String = Directory.GetFiles("input")
        Dim imgCount As Integer = imgList.Length

        Dim perPage As Integer = (((width - 72) / 25) * ((height - 36 - 56) / 35))

        Dim pages As Integer = Math.Round(imgCount / CType(perPage, Single) + 0.5)

        Dim text As String = $"Contains the following {imgCount} files from img:{vbCrLf}{vbCrLf}"

        Dim pno As Integer = 1

        page.InsertText(rect.TopLeft, text, fontFile:="kenpixel.ttf", fontName:="Kenpixel")
        page.InsertText(rect.BottomLeft, $"Page {pno} of {pages}", fontFile:="kenpixel.ttf", fontName:="Kenpixel")

        Dim point As Point = rect.TopLeft + New Point(0, 20)
        For i As Integer = 0 To imgList.Length - 1
            Dim path As String = imgList(i)
            Console.WriteLine(path)
            If Not File.Exists(path) Then
                Console.WriteLine("skipping non-file")
                Continue For
            End If
            Dim img() As Byte = File.ReadAllBytes(path)
            page.AddFileAnnot(point, img, filename:=imgList(i))

            point += New Point(25, 0)
            If point.X >= rect.Width Then
                point = New Point(rect.X0, point.Y + 35)
            End If
            If point.Y >= rect.Height AndAlso i < imgCount - 1 Then
                page = doc.NewPage(width:=width, height:=height)
                pno += 1
                page.InsertText(rect.TopLeft, text, fontFile:="kenpixel.ttf", fontName:="Kenpixel")
                page.InsertText(rect.BottomLeft, $"Page {pno} of {pages}", fontFile:="kenpixel.ttf", fontName:="Kenpixel")
                point = rect.TopLeft + New Point(0, 20)
            End If
        Next
        doc.Save("output.pdf")
    End Sub
End Module
