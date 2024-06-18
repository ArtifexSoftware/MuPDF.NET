Imports System
Imports System.IO
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim doc As New Document(args(0))
        Dim content As Byte() = doc.GetEmbfile(0)
        File.WriteAllBytes("../../../../output.jpg", content)
    End Sub
End Module
