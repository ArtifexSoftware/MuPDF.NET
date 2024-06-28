Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim doc As New Document("../../../../input.epub")

        If doc.IsPDF Then
            Throw New Exception("document is PDF already")
        End If

        Dim b As Byte() = doc.Convert2Pdf()
        Dim pdf As New Document("pdf", b)

        Dim toc As List(Of Toc) = doc.GetToc()
        Console.WriteLine(toc(0).ToString())
        pdf.SetToc(toc)

        Dim meta As Dictionary(Of String, String) = doc.MetaData
        If meta.GetValueOrDefault("producer", Nothing) IsNot Nothing Then
            meta("producer") = "MuPDF.NET v2.0.8-alpha"
        End If

        If meta.GetValueOrDefault("creator", Nothing) IsNot Nothing Then
            meta("creator") = "MuPDF.NET PDF Converter"
        End If

        pdf.SetMetadata(meta)

        Dim linkCnt As Integer = 0
        Dim linkSkip As Integer = 0
        For i As Integer = 0 To doc.PageCount - 1
            Dim page As Page = doc(i)
            Dim links As List(Of LinkInfo) = page.GetLinks()
            linkCnt += links.Count
            Dim pOut As Page = pdf(i)
            For Each l As LinkInfo In links
                If l.Kind = LinkType.LINK_NAMED Then
                    linkSkip += 1
                    Continue For
                End If
                pOut.InsertLink(l)
            Next
        Next

        pdf.Save("output.pdf", garbage:=4, deflate:=1)
    End Sub
End Module
