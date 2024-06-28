Imports System
Imports System.IO
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim dimlimit As Single = 0
        Dim relsize As Single = 0
        Dim abssize As Single = 0

        Dim doc As New Document(args(0))
        Dim pageCount As Integer = doc.PageCount

        Dim xrefList As New List(Of Integer)()
        Dim imgList As New List(Of Integer)()
        For i As Integer = 0 To pageCount - 1
            Dim il As List(Of Entry) = doc.GetPageImages(i)
            imgList = il.Select(Function(e) e.Xref).ToList()
            For Each img As Entry In il
                Dim xref As Integer = img.Xref
                If xrefList.Contains(xref) Then
                    Continue For
                End If
                Dim width As Single = img.Width
                Dim height As Single = img.Height
                If Math.Min(width, height) <= dimlimit Then
                    Continue For
                End If

                Dim image As ImageInfo = RecoverPix(doc, img)
                Dim n As Integer = image.ColorSpace
                Dim imgData As Byte() = image.Image

                If imgData.Length <= abssize Then
                    Continue For
                End If
                If imgData.Length / (width * height * n) <= relsize Then
                    Continue For
                End If

                File.WriteAllBytes(String.Format("img{0}.png", xref), imgData)
            Next
        Next
    End Sub

    Function RecoverPix(doc As Document, item As Entry) As ImageInfo
        Dim xref As Integer = item.Xref
        Dim smask As Integer = item.Smask

        If smask > 0 Then
            Dim pix0 As New Pixmap(doc.ExtractImage(xref).Image)

            If pix0.Alpha <> 0 Then
                pix0 = New Pixmap(pix0, 0)
            End If

            Dim mask As New Pixmap(doc.ExtractImage(smask).Image)
            Dim pix As New Pixmap(doc.ExtractImage(xref).Image)

            Dim ext As String = ""
            If pix0.N > 3 Then
                ext = "pam"
            Else
                ext = "png"
            End If

            Return New ImageInfo() With {
                .ext = ext,
                .ColorSpace = pix.ColorSpace.N,
                .image = pix.ToBytes(ext)
            }
        End If

        If doc.GetXrefObject(xref, compressed:=1).Contains("/ColorSpace") Then
            Dim pix As New Pixmap(doc, xref)
            pix = New Pixmap(Utils.csRGB, pix)
            Return New ImageInfo() With {
                .Ext = "png",
                .ColorSpace = 3,
                .image = pix.ToBytes("png")
            }
        End If

        Return doc.ExtractImage(xref)
    End Function
End Module
