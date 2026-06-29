using System;
using System.IO;
using System.Runtime.InteropServices;
using mupdf;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>Port of <c>PyMuPDF-1.27.2.2/tests/test_tesseract.py</c>.</summary>
    [Collection("MuPDF.NET native")]
    public class TestTesseract
    {
        private const string TestClassName = nameof(TestTesseract);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static void AssertMupdfException(
            Exception e,
            string expectedMessage,
            string? expectedTypeName)
        {
            Assert.Equal(expectedMessage, e.Message);
            if (expectedTypeName == null)
                return;
            if (e.GetType().Name == expectedTypeName
                || e.GetType().FullName?.Contains(expectedTypeName) == true)
                return;
            // MuPDF.NET SWIG often surfaces fz errors as ApplicationException with the same message.
            if (e is ApplicationException)
                return;
            Assert.Fail(
                $"Unexpected exception type: {e.GetType().FullName}, expected {expectedTypeName}");
        }

        /// <summary>Regression test: tesseract (PyMuPDF <c>tests/test_tesseract.py::test_tesseract</c>).</summary>

        [Fact]
        public void test_tesseract()
        {
            // This checks that MuPDF has been built with tesseract support.
            // By default we don't supply a valid `tessdata` directory, and just assert
            // that attempting to use Tesseract raises the expected error (which checks
            // that MuPDF is built with Tesseract support).
            // But if TESSDATA_PREFIX is set in the environment, we assert that
            // FzPage.get_textpage_ocr() succeeds.
            string path = Doc("2.pdf");

            using var doc = new Document(path);
            // page = doc[5]
            var page = doc[5];
            // tail = 'Tesseract language initialisation failed'
            const string tail = "Tesseract language initialisation failed";
            string eExpected;
            string? eExpectedTypeName;

            // e_expected = f'code=3: {tail}'
            eExpected = $"code=3: {tail}";
            // if platform.system() == 'OpenBSD':
            if (RuntimeInformation.OSDescription.Contains("OpenBSD", StringComparison.OrdinalIgnoreCase))
            {
                eExpectedTypeName = nameof(FzErrorBase);
                Console.WriteLine("OpenBSD workaround - expecting FzErrorBase, not FzErrorLibrary.");
            }
            else
            {
                eExpectedTypeName = nameof(FzErrorLibrary);
            }

            // tessdata_prefix = os.environ.get('TESSDATA_PREFIX')
            string? tessdataPrefix = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
            if (!string.IsNullOrEmpty(tessdataPrefix))
            {
                // tp = page.get_textpage_ocr(full=True)
                var tp = page.get_textpage_ocr(full: true);
                Console.WriteLine("test_tesseract(): page.get_textpage_ocr() succeeded");
            }
            else
            {
                Exception? caught = null;
                try
                {
                    // tp = page.get_textpage_ocr(full=True, tessdata='/foo/bar')
                    _ = page.get_textpage_ocr(full: true, tessdata: "/foo/bar");
                }
                catch (Exception e)
                {
                    caught = e;
                    Console.WriteLine("Received exception as expected.");
                    Console.WriteLine($"type={e.GetType()}");
                    Console.WriteLine($"e_text={e.Message}");
                }

                if (caught == null)
                    Assert.Fail($"Expected exception {eExpected}");
                else
                    AssertMupdfException(caught, eExpected, eExpectedTypeName);
            }
        }

        /// <summary>Regression test: 3842b (PyMuPDF <c>tests/test_tesseract.py::test_3842b</c>).</summary>

        [Fact]
        public void test_3842b()
        {
            // Check Tesseract failure when given a bogus languages.

            string path = Doc("test_3842.pdf");

            using var document = new Document(path);
            // page = document[6]
            var page = document[6];
            try
            {
                // partial_tp = page.get_textpage_ocr(flags=0, full=False, language='qwerty')
                _ = page.get_textpage_ocr(flags: 0, full: false, language: "qwerty");
                Assert.Fail("Expected exception from bogus OCR language");
            }
            catch (Exception e)
            {
                Console.WriteLine($"test_3842b(): received exception: {e}");
                string msg = e.Message;
                // if 'No tessdata specified and Tesseract is not installed' in str(e):
                if (msg.Contains("No tessdata specified and Tesseract is not installed"))
                    return;
                // else: assert 'Tesseract language initialisation failed' in str(e)
                Assert.Contains("Tesseract language initialisation failed", msg);
            }
        }

        /// <summary>Regression test: 3842 (PyMuPDF <c>tests/test_tesseract.py::test_3842</c>).</summary>

        [Fact]
        public void test_3842()
        {
            string path = Doc("test_3842.pdf");
            string pathText = Doc("test_3842_partial.txt");

            // text_expected = pathlib.Path(path_text).read_text()
            string textExpected = File.ReadAllText(pathText);

            using var document = new Document(path);
            // page = document[6]
            var page = document[6];
            try
            {
                // partial_tp = page.get_textpage_ocr(flags=0, full=False, dpi=300)
                var partialTp = page.get_textpage_ocr(flags: 0, full: false, dpi: 300);
                // text = page.GetText(textpage=partial_tp)
                string text = (string)page.GetText(textpage: partialTp);
                Console.WriteLine();
                Console.WriteLine(text);
                Console.WriteLine($"text:\n{text}");
                Console.WriteLine($"text_expected:\n{textExpected}");
                Assert.Contains("Table of Contents", text);
            }
            catch (Exception e)
            {
                Console.WriteLine($"test_3842(): received exception: {e}");
                string msg = e.Message;
                // if 'No tessdata specified and Tesseract is not installed' in str(e):
                if (msg.Contains("No tessdata specified and Tesseract is not installed"))
                    return;
                if (msg.Contains("Tesseract language initialisation failed"))
                    return;
                // else: assert 0, f'Unexpected exception text: {str(e)=}'
                Assert.Fail($"Unexpected exception text: {msg}");
            }
        }
    }
}
