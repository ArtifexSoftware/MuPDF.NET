using System;
using System.IO;
using System.Runtime.InteropServices;
using mupdf;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestTesseract/</c>; outputs: <c>TestDocuments/_Output/TestTesseract/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestTesseract
    {
        private const string TestClassName = nameof(TestTesseract);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        /// <summary>
        /// This checks that MuPDF has been built with tesseract support.
        /// By default we don't supply a valid tessdata directory, and just assert
        /// that attempting to use Tesseract raises the expected error (which checks
        /// that MuPDF is built with Tesseract support).
        /// But if TESSDATA_PREFIX is set in the environment, we assert that
        /// Page.GetTextPageOcr succeeds.
        /// </summary>
        [Fact]
        public void test_tesseract()
        {

            string path = Doc("2.pdf");

            using var doc = new Document(path);

            var page = doc[5];
            const string tail = "Tesseract language initialisation failed";
            string eExpected = $"code=3: {tail}";
            Type eExpectedType;
            if (RuntimeInformation.OSDescription.Contains("OpenBSD", StringComparison.OrdinalIgnoreCase))
            {
                // 2023-12-12: For some reason the SWIG catch code only catches
                // the exception as FzErrorBase.
                eExpectedType = typeof(FzErrorBase);
                Console.WriteLine("OpenBSD workaround - expecting FzErrorBase, not FzErrorLibrary.");
            }
            else
            {
                eExpectedType = typeof(FzErrorLibrary);
            }

            string tessdataPrefix = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
            if (!string.IsNullOrEmpty(tessdataPrefix))
            {
                TextPage tp = page.GetTextPageOcr(full: true);
                Console.WriteLine(tp.ExtractText());
                Console.WriteLine("test_tesseract(): page.GetTextPageOcr() succeeded");
            }
            else
            {
                try
                {
                    TextPage tp = page.GetTextPageOcr(full: true, tessdata: "/foo/bar");
                    Console.WriteLine(tp.ExtractText());
                    Assert.Fail($"Expected exception {eExpected}");
                }
                catch (Exception e)
                {
                    string eText = e.Message;
                    Console.WriteLine("Received exception as expected.");
                    Console.WriteLine($"type={e.GetType()}");
                    Console.WriteLine($"e_text={eText}");
                    Assert.Equal(eExpected, eText);
                    Console.WriteLine($"e_expected_type={eExpectedType}");
                    AssertTesseractExceptionType(e, eExpectedType);
                }
            }
        }

        /// <summary>Check Tesseract failure when given a bogus language.</summary>
        [Fact]
        public void test_3842b()
        {
            // Note that Tesseract seems to output its own diagnostics.

            string path = Doc("test_3842.pdf");
            using var document = new Document(path);

            var page = document[6];
            try
            {
                _ = page.GetTextPageOcr(flags: 0, full: false, language: "qwerty");
            }
            catch (Exception e)
            {
                Console.WriteLine($"test_3842b(): received exception: {e}");
                string msg = e.Message;
                if (msg.Contains("No tessdata specified and Tesseract is not installed"))
                    return;
                Assert.Contains("Tesseract language initialisation failed", msg);
            }
        }

        [Fact]
        public void test_3842()
        {
            string path = Doc("test_3842.pdf");
            string pathTextExpected;
            string pathTextExpectedAlternative = null;
            if (_Version.mupdf_version_tuple_at_least(1, 28, 0))
            {
                pathTextExpected = Doc("test_3842_expected_1.28.txt");
                pathTextExpectedAlternative = Doc("test_3842_expected_1.28_older_tesseract.txt");
            }
            else
            {
                pathTextExpected = Doc("test_3842_partial.txt");
            }

            string textExpected = NormalizeNewlines(File.ReadAllText(pathTextExpected));
            string textExpectedAlternative = pathTextExpectedAlternative != null
                ? NormalizeNewlines(File.ReadAllText(pathTextExpectedAlternative))
                : null;

            using var document = new Document(path);

            var page = document[6];
            TextPage partialTp;
            try
            {
                partialTp = page.GetTextPageOcr(flags: 0, full: false, dpi: 300);
            }
            catch (Exception e)
            {
                Console.WriteLine($"test_3842(): received exception: {e}");
                string msg = e.Message;
                if (msg.Contains("No tessdata specified and Tesseract is not installed"))
                    return;
                if (msg.Contains("Tesseract language initialisation failed"))
                    return;
                Assert.Fail($"Unexpected exception text: {msg}");
                return;
            }

            string text = NormalizeNewlines((string)page.GetText(textpage: partialTp));
            File.WriteAllText(Out("test_3842_out"), text);
            Console.WriteLine();
            Console.WriteLine(text);
            Console.WriteLine($"text:\n{text}");
            Console.WriteLine($"text_expected:\n{textExpected}");
            if (textExpectedAlternative != null)
                Console.WriteLine($"text_expected_alternative:\n{textExpectedAlternative}");
            Assert.True(
                text == textExpected || text == textExpectedAlternative,
                "OCR text did not match either 1.28 expected file.");
        }

        static string NormalizeNewlines(string s) =>
            (s ?? "").Replace("\r\n", "\n").Replace('\r', '\n');

        /// <summary>
        /// SWIG often surfaces the same fz error as <see cref="ApplicationException"/>.
        /// </summary>
        static void AssertTesseractExceptionType(Exception e, Type expectedType)
        {
            if (expectedType.IsInstanceOfType(e) || e.GetType() == expectedType)
                return;
            if (e is ApplicationException)
                return;
            Assert.Fail($"Unexpected exception type: {e.GetType().FullName}, expected {expectedType.FullName}");
        }
    }
}
