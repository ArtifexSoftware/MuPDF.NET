Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim doc As New Document("input.pdf")

        Dim nameLen As Integer = 0
        Dim filenameLen As Integer = 0
        Dim totalLen As Integer = 0
        Dim totalSize As Integer = 0

        Dim efList As New List(Of (String, String, Integer, Integer))

        For i As Integer = 0 To doc.GetEmbfileCount() - 1
            Dim info As EmbfileInfo = doc.GetEmbfileInfo(i)
            efList.Add((info.Name, info.FileName, info.Length, info.Size))
            nameLen = Math.Max(info.Name.Length, nameLen)
            filenameLen = Math.Max(info.FileName.Length, filenameLen)
            totalLen += info.Length
            totalSize += info.Size
        Next

        If efList.Count < 1 Then
            Console.WriteLine("no embedded files in input.pdf")
        End If

        Dim ratio As Single = totalSize / CSng(totalLen)
        Dim saves As Single = 1 - ratio

        Dim header As String = String.Format("{0,-" & (nameLen + 4) & "}{1,-" & (filenameLen + 4) & "}{2,10}{3,11}",
                                     "Name", "Filename", "Length", "Size")

        Dim line As String = New String("-"c, header.Length)

        For Each info In efList
            Console.WriteLine(String.Format("{0,-" & (nameLen + 3) & "}{1,-" & (filenameLen + 3) & "}{2,10}{3,10}",
                                    info.Item1, info.Item2, info.Item3, info.Item4))
        Next

        Console.WriteLine($"{efList.Count} embedded files in 'input.pdf'. Totals:")

        Console.WriteLine($"File lengths: {totalLen}, compressed: {totalSize}, ratio: {Math.Round(ratio * 100, 2)}% (savings: {Math.Round(saves * 100, 2)}%).")
    End Sub
End Module
