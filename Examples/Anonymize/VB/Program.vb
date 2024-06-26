Imports System
Imports mupdf.NET
Imports System.Text

Module Program
    Sub Main(args As String())
        Dim doc As New Document("input.pdf")
        doc.SetMetadata()
        doc.DeleteXmlMetadata()

        For i As Integer = 0 To doc.PageCount - 1
            Dim page As Page = doc(i)
            Dim xrefList As List(Of Integer) = page.GetContents()
            For Each xref As Integer In xrefList
                Dim cont() As Byte = doc.GetXrefStream(xref)
                Dim ncont As String = RemoveTxt(Encoding.UTF8.GetString(cont))
                doc.UpdateStream(xref, Encoding.UTF8.GetBytes(ncont))
            Next
        Next
    End Sub

    Function RemoveTxt(cont As String) As String
        Dim cont1 As String = cont.Replace("\n", " ")
        Dim ct() As String = cont1.Split(" "c)
        Dim intext As Boolean = False
        Dim nct As New List(Of String)
        For Each word As String In ct
            If word = "ET" Then
                intext = False
                Continue For
            End If
            If word = "BT" Then
                intext = True
                Continue For
            End If
            If intext Then Continue For
            nct.Add(word)
        Next
        Dim ncont As String = String.Join(" ", nct)
        Return ncont
    End Function
End Module
