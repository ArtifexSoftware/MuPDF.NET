Imports System
Imports System.Text
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim src As New Document("input.pdf")
        Dim outfile As String = "output.pdf"

        Dim doc As New Document()
        Dim total As Integer = 0
        Dim xrefs As New List(Of Integer)()

        For i As Integer = 0 To src.PageCount - 1
            Dim count As Integer = 0
            Dim xobjs As List(Of Entry) = src.GetPageXObjects(i)
            For Each xobj As Entry In xobjs
                If xobj.StreamXref <> 0 Then
                    Continue For
                End If
                Dim bbox As Rect = xobj.Bbox
                If bbox.IsInfinite Then
                    Continue For
                End If
                If xrefs.Contains(xobj.Xref) Then
                    Continue For
                End If
                xrefs.Add(xobj.Xref)

                doc.InsertPdf(src, fromPage:=i, toPage:=i, rotate:=0)
                Dim refName As String = xobj.RefName
                Dim refcmd As Byte() = Encoding.UTF8.GetBytes("/" & refName & " Do")
                Dim page As Page = doc(doc.PageCount - 1)
                page.SetMediaBox(bbox)
                page.CleanContetns()
                Dim xref As Integer = page.GetContents()(0)
                doc.UpdateStream(xref, refcmd)
                count += 1
            Next
            If count > 0 Then
                Console.WriteLine(count)
            End If
            total += count
        Next

        If total > 0 Then
            doc.Save("output.pdf", garbage:=4, deflate:=1)
        End If
    End Sub
End Module
