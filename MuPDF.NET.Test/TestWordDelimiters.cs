// Test changing word delimiting characters.
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestWordDelimiters/</c>; outputs: <c>TestDocuments/_Output/TestWordDelimiters/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestWordDelimiters
    {
        private const string Punctuation = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";

        private static List<string> WordTexts(object words) =>
            ((List<WordBlock>)words).Select(w => w.Text).ToList();

        [Fact]
        public void test_delimiters()
        {
            // Test changing word delimiting characters.
            using var doc = new Document();
            var page = doc.NewPage();
            const string text = "word1,word2 - word3. word4?word5.";
            page.InsertText(new Point(50, 50), text);

            // Standard words extraction:
            // only spaces and line breaks start a new word
            var words0 = WordTexts(page.GetText("words"));
            Assert.Equal(new[] { "word1,word2", "-", "word3.", "word4?word5." }, words0);

            // extract words again
            var words1 = WordTexts(page.GetText("words", delimiters: Punctuation));
            Assert.NotEqual(words0, words1);
            Assert.Equal("word1 word2 word3 word4 word5", string.Join(" ", words1));

            // confirm we will be getting old extraction
            Assert.Equal(words0, WordTexts(page.GetText("words")));
        }
    }
}
