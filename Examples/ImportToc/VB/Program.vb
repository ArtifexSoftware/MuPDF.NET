Imports System
Imports mupdf.NET
Imports System.IO

Module Program
    Sub Main(args As String())
        Dim doc As New Document("input.pdf")

        Dim lines() As String = File.ReadAllLines("input.csv")
        Dim toc As New List(Of Toc)()

        For Each line As String In lines
            Dim row() As String = line.Split(";")
            Dim p4 As Single = Single.Parse(row(3))
            Dim t As New Toc() With {
                .Level = Integer.Parse(row(0)),
                .Title = row(1),
                .Page = Integer.Parse(row(2)),
                .Link = p4
            }
            toc.Add(t)
        Next

        doc.SetToc(toc)
        doc.SaveIncremental()
    End Sub
End Module
