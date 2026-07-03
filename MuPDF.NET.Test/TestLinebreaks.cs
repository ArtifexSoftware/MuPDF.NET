using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Test avoidance of linebreaks.
    /// Inputs: <c>TestDocuments/TestLinebreaks/</c>; outputs: <c>TestDocuments/_Output/TestLinebreaks/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestLinebreaks
    {
        private const string TestClassName = nameof(TestLinebreaks);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static int SplitLineCount(string text)
        {
            // Python len(str.splitlines())
            var lines = new List<string>();
            using (var reader = new StringReader(text))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                    lines.Add(line);
            }
            return lines.Count;
        }

        /// <summary>Regression test: linebreaks.</summary>
        [Fact]
        public void test_linebreaks()
        {
            using var doc = new Document(Doc("test-linebreaks.pdf"));
            var page = doc[0];
            var tp = page.GetTextPage(flags: Constants.TextFlagsWords);
            var words = page.GetTextWords(textpage: tp);
            int word_count = words.Count;
            int line_count1 = SplitLineCount((string)page.GetText(textpage: tp));
            int line_count2 = SplitLineCount((string)page.GetText(textpage: tp, sort: true));
            Assert.Equal(word_count, line_count1);
            Assert.True(line_count2 < line_count1 / 2);
        }
    }
}