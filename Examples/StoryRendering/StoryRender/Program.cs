using MuPDF.NET;

PDFRenderer renderer = new PDFRenderer();
renderer.GeneratePDF();

public class PDFRenderer
{
    private readonly string _HTML = "<html><body><p>The Start</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>Lorem Ipsum</p><p>The End</p></body></html>";

    public void GeneratePDF()
    {
        Console.WriteLine("GeneratePDF");

        Rect mediaBox = MuPDF.NET.Utils.PaperRect("A4");
        Rect where = mediaBox + new Rect(36, 36, -36, -36);

        var html = _HTML;
        Story story = new(html);
        var docWriter = new DocumentWriter("my-writer-pdf.pdf");

        bool more = true;

        while (more)
        {
            DeviceWrapper device = docWriter.BeginPage(mediaBox);
            (bool, Rect) moreFilled = story.Place(where);
            more = moreFilled.Item1;
            story.Draw(device);
            docWriter.EndPage();
            Console.WriteLine("more" + more);
        }
        docWriter.Close();
    }

}
