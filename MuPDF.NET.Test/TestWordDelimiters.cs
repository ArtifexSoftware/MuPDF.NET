// Port of PyMuPDF-1.27.2.2/tests/test_word_delimiters.py
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
        private const string TestClassName = nameof(TestWordDelimiters);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_delimiters()
        {
            // Test changing word delimiting characters.
            using var doc = new Document();
            var page = doc.NewPage();
            const string text = "word1,word2 - word3. word4?word5.";
            page.InsertText(new Point(50, 50), text);

            // Standard words extraction: only spaces and line breaks start a new word.
            var words0 = page.GetTextWords()
                .Select(w => w.word)
                .ToList();
            Assert.Equal(
                new List<string> { "word1,word2", "-", "word3.", "word4?word5." },
                words0);

            // Extract words again with punctuation as delimiters.
            var words1 = page.GetTextWords(delimiters: Punctuation)
                .Select(w => w.word)
                .ToList();
            Assert.NotEqual(words0, words1);
            Assert.Equal("word1 word2 word3 word4 word5", string.Join(" ", words1));

            // Confirm default extraction is unchanged.
            Assert.Equal(
                words0,
                page.GetTextWords().Select(w => w.word).ToList());

            doc.save(Out("test_delimiters.pdf"));
        }

        // Python string.punctuation
        private const string Punctuation = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
    }
}
