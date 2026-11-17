using MuPDF.NET;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET.layout")]
    public class Test5030
    {
        private const string TestClassName = nameof(Test5030);

        private static string? Doc(string fileName) => _Path.TryForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_5030()
        {
            // Eight short text fragments scattered like an OCR'd slide. The layout model
            // reads the region as a table, but the grid finder extracts no cells from it.
            var placements = new (float x, float y, string text, float size)[]
            {
                (84, 620, "Cost", 10),
                (214, 280, "Net", 12),
                (88, 505, "12%", 9),
                (213, 378, "Margin", 11),
                (130, 245, "Margin", 10),
                (373, 156, "South", 8),
                (67, 222, "North", 11),
                (140, 475, "3.4", 11),
            };

            byte[] data;
            using (var doc = new Document())
            {
                Page page = doc.NewPage(); // default A4
                foreach (var (x, y, text, size) in placements)
                    page.InsertText(new Point(x, y), text, fontSize: size);
                data = doc.Write(garbage: true);
                doc.Save(Out("test_5030.pdf"));
            }

            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(true);
                using (var doc = new Document(data, "pdf"))
                {
                    bool passed = true;
                    try
                    {
                        string md = Utils.ToMarkdown(doc);
                        Console.WriteLine(md);
                    }
                    catch
                    {
                        passed = false;
                    }
                    Assert.True(passed);
                }
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }
    }
}
