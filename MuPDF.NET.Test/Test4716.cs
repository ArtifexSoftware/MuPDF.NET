using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test4716
    {
        private static readonly string testDocPath = _Path.ForTestClass("test_4716.pdf", nameof(Test4716));
        private static readonly string outDocPath = _Path.ForOutput("test_4716.pdf", nameof(Test4716));

        [Fact]
        public void test_4716()
        {
            // Confirm that ZERO WIDTH JOINER will never start a word.
            // script_dir = os.path.dirname(__file__)
            // filename = os.path.join(script_dir, "resources", "test_4716.pdf")
            using var doc = new Document(testDocPath);
            var expected = new HashSet<string> { "+25.00", "Любимый", "-10.00" };
            var word_text = new HashSet<string>();
            foreach (Page page in doc)
            {
                // MuPDF: page.get_text("words") -> list of tuples; MuPDF.NET -> List<WordBlock>
                foreach (WordBlock w in (List<WordBlock>)page.GetText("words"))
                    word_text.Add(w.Text);
            }
            Assert.Equal(expected, word_text);
            doc.Save(outDocPath);
        }
    }
}