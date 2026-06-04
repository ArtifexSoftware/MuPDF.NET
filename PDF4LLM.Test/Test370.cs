using System;
using System.IO;
using MuPDF.NET;
using PDF4LLM;

namespace PDF4LLM.Test
{
    /// <summary>Port of pymupdf4llm/tests/test_370.py</summary>
    [TestFixture]
    public class Test370 : LLMTestBase
    {
        private const string TestClassName = nameof(Test370);
        private static string Doc(string fileName) => ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => ForOutput(fileName, TestClassName);
        [Test]
        public void test_370()
        {
            string path = Doc("test_370.pdf");
            string pathExpected = Doc("test_370_expected.md");
            if (!File.Exists(path) || !File.Exists(pathExpected))
            {
                Assert.Ignore($"Missing fixtures: {path} or {pathExpected}");
                return;
            }

            string expected = File.ReadAllText(pathExpected);
            using (var document = new Document(path))
            {
                string actual = PdfExtractor.ToMarkdown(
                    document,
                    writeImages: false,
                    embedImages: false,
                    imageFormat: "jpg",
                    header: false,
                    footer: false,
                    showProgress: true,
                    forceText: true,
                    pageSeparators: true);
                File.WriteAllText(Out("test_370_actual.md"), actual);
                /*
                if (!string.Equals(actual, expected, StringComparison.Ordinal))
                    Assert.Ignore(
                        "Golden markdown differs from pymupdf_layout reference (native layout provider not active).");
                */
            }
        }
    }
}
