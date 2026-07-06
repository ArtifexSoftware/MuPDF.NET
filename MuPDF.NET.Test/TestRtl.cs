using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestRtl/</c>; outputs: <c>TestDocuments/_Output/TestRtl/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestRtl
    {
        private const string TestClassName = nameof(TestRtl);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_rtl()
        {
            string path = Doc("test-E+A.pdf");
            using var doc = new Document(path);
            // page = doc[0]
            var page = doc[0];
            // rtl_chars = set([chr(i) for i in range(0x590, 0x901)])
            var rtlChars = new HashSet<char>();
            for (int i = 0x590; i < 0x901; i++)
                rtlChars.Add((char)i);

            foreach (var w in page.get_text_words())
            {
                // every word string must either ONLY contain RTL chars
                // cond1 = rtl_chars.issuperset(w[4])
                bool cond1 = rtlChars.IsSupersetOf(w.word);
                // ... or NONE.
                // cond2 = rtl_chars.intersection(w[4]) == set()
                bool cond2 = !w.word.Any(c => rtlChars.Contains(c));
                Assert.True(cond1 || cond2);
            }
            doc.Save(Out("test_rtl.pdf"));
        }
    }
}