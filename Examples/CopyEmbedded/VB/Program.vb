Imports System
Imports mupdf.NET
Imports System.Text

Module Program
    Sub Main(args As String())
        Dim src As New Document("input.pdf")
        Dim dst As New Document("output.pdf")

        For i As Integer = 0 To src.GetEmbfileCount() - 1
            Dim d As EmbfileInfo = src.GetEmbfileInfo(i)
            Dim b() As Byte = src.GetEmbfile(i)
            dst.AddEmbfile(Encoding.UTF8.GetString(b), Encoding.UTF8.GetBytes(d.FileName), d.UFileName, d.Desc)
        Next
    End Sub
End Module
