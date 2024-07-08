Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim pdfOut As New Document()
        Dim _cdate As String = Utils.GetPdfNow()

        Dim pdfDict As New Dictionary(Of String, String) From {
            {"creator", "PDF Joiner"},
            {"producer", "PyMuPDF"},
            {"creationDate", _cdate},
            {"modDate", _cdate},
            {"title", "Pdf Joiner"},
            {"author", "Green"},
            {"subject", "pdf joiner"},
            {"keywords", "mupdf doc join"}
        }

        pdfOut.SetMetadata(pdfDict)
        Dim totalToc As New List(Of Toc)()

        Dim doc As New Document("thinkpython2.pdf")
        Dim von As Integer = 2
        Dim bis As Integer = 100
        Dim rot As Integer = 90
        Dim ausNR As Integer = 0

        pdfOut.InsertPdf(doc, fromPage:=von, toPage:=bis, rotate:=rot)

        totalToc.Add(New Toc() With {
            .Level = 1,
            .Title = $"{von + 1}-{bis + 1}",
            .Page = 7
        })

        Dim toc As List(Of Toc) = doc.GetToc(simple:=False)
        Dim lastLvl As Integer = 1

        Dim pageRange As New List(Of Integer)()
        For i As Integer = von To bis + 1
            pageRange.Add(i)
        Next

        For Each t As Toc In toc
            Dim pno As Integer = 0
            Dim lnkType As LinkType = 0
            Try
                lnkType = CType(t.Link, LinkInfo).Kind
            Catch ex As Exception
                Throw New Exception("invalid data format")
            End Try

            If Not pageRange.Contains(t.Page - 1) AndAlso lnkType = LinkType.LINK_GOTO Then
                Continue For
            End If

            If lnkType = LinkType.LINK_GOTO Then
                pno = pageRange.IndexOf(t.Page - 1) + ausNR + 1
            End If

            While t.Level > lastLvl + 1
                totalToc.Add(New Toc() With {
                    .Level = lastLvl + 1,
                    .Title = "<>",
                    .Page = pno,
                    .Link = t.Link
                })
                lastLvl += 1
            End While

            lastLvl = t.Level
            t.Page = pno
            totalToc.Add(t)
        Next

        ausNR += pageRange.Count
        doc.Close()

        If totalToc.Count <> 0 Then
            pdfOut.SetToc(totalToc)
        End If

        pdfOut.Save("e://res/output1.pdf")
    End Sub
End Module
