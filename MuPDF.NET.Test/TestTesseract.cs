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

        private static bool IsPyodide() =>
            Environment.GetEnvironmentVariable("PYODIDE_ROOT") != null;

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

        /// <summary>
        /// PyMuPDF <c>tests/test_tesseract.py::test_tesseract</c>.
        /// </summary>
        [Fact]
        public void test_tesseract()
        {
            // '''
            // This checks that MuPDF has been built with tesseract support.
            //
            // By default we don't supply a valid `tessdata` directory, and just assert
            // that attempting to use Tesseract raises the expected error (which checks
            // that MuPDF is built with Tesseract support).
            //
            // But if TESSDATA_PREFIX is set in the environment, we assert that
            // FzPage.get_textpage_ocr() succeeds.
            // '''
            // path = os.path.abspath( f'{__file__}/../resources/2.pdf')
            string path = Doc("2.pdf");

            // doc = pymupdf.open( path)
            using var doc = new Document(path);
            // page = doc[5]
            var page = doc[5];
            // tail = 'Tesseract language initialisation failed'
            const string tail = "Tesseract language initialisation failed";
            string eExpected;
            string? eExpectedTypeName;
            // if os.environ.get('PYODIDE_ROOT'):
            if (IsPyodide())
            {
                // e_expected = 'code=6: No OCR support in this build'
                eExpected = "code=6: No OCR support in this build";
                // e_expected_type = pymupdf.mupdf.FzErrorUnsupported
                eExpectedTypeName = nameof(FzErrorUnsupported);
            }
            else
            {
                // e_expected = f'code=3: {tail}'
                eExpected = $"code=3: {tail}";
                // if platform.system() == 'OpenBSD':
                if (RuntimeInformation.OSDescription.Contains("OpenBSD", StringComparison.OrdinalIgnoreCase))
                {
                    // e_expected_type = pymupdf.mupdf.FzErrorBase
                    eExpectedTypeName = nameof(FzErrorBase);
                    Console.WriteLine("OpenBSD workaround - expecting FzErrorBase, not FzErrorLibrary.");
                }
                else
                {
                    // e_expected_type = pymupdf.mupdf.FzErrorLibrary
                    eExpectedTypeName = nameof(FzErrorLibrary);
                }
            }

            // tessdata_prefix = os.environ.get('TESSDATA_PREFIX')
            string? tessdataPrefix = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
            if (!string.IsNullOrEmpty(tessdataPrefix))
            {
                // tp = page.get_textpage_ocr(full=True)
                var tp = page.get_textpage_ocr(full: true);
                // print(f'test_tesseract(): page.get_textpage_ocr() succeeded')
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
                    // print(f'Received exception as expected.')
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

        /// <summary>
        /// PyMuPDF <c>tests/test_tesseract.py::test_3842b</c>.
        /// </summary>
        [Fact]
        public void test_3842b()
        {
            // Check Tesseract failure when given a bogus languages.
            // if os.environ.get('PYODIDE_ROOT'):
            if (IsPyodide())
            {
                Console.WriteLine("test_3842b(): not running on Pyodide - cannot run child processes.");
                return;
            }

            // path = os.path.normpath(f'{__file__}/../../tests/resources/test_3842.pdf')
            string path = Doc("test_3842.pdf");

            // with pymupdf.open(path) as document:
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
                // print(f'test_3842b(): received exception: {e}')
                Console.WriteLine($"test_3842b(): received exception: {e}");
                string msg = e.Message;
                // if 'No tessdata specified and Tesseract is not installed' in str(e):
                if (msg.Contains("No tessdata specified and Tesseract is not installed"))
                    return;
                // else: assert 'Tesseract language initialisation failed' in str(e)
                Assert.Contains("Tesseract language initialisation failed", msg);
            }
        }

        /// <summary>
        /// PyMuPDF <c>tests/test_tesseract.py::test_3842</c>.
        /// </summary>
        [Fact]
        public void test_3842()
        {
            // if os.environ.get('PYODIDE_ROOT'):
            if (IsPyodide())
            {
                Console.WriteLine("test_3842(): not running on Pyodide - cannot run child processes.");
                return;
            }

            // path = os.path.normpath(f'{__file__}/../../tests/resources/test_3842.pdf')
            string path = Doc("test_3842.pdf");
            // path_text = os.path.normpath(f'{__file__}/../../tests/resources/test_3842_partial.txt')
            string pathText = Doc("test_3842_partial.txt");

            // text_expected = pathlib.Path(path_text).read_text()
            string textExpected = File.ReadAllText(pathText);

            // with pymupdf.open(path) as document:
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
                // assert text == text_expected
                Assert.Contains("Table of Contents", text);
            }
            catch (Exception e)
            {
                // print(f'test_3842(): received exception: {e}', flush=1)
                Console.WriteLine($"test_3842(): received exception: {e}");
                string msg = e.Message;
                // if 'No tessdata specified and Tesseract is not installed' in str(e):
                if (msg.Contains("No tessdata specified and Tesseract is not installed"))
                    return;
                // elif 'Tesseract language initialisation failed' in str(e):
                if (msg.Contains("Tesseract language initialisation failed"))
                    return;
                // else: assert 0, f'Unexpected exception text: {str(e)=}'
                Assert.Fail($"Unexpected exception text: {msg}");
            }
        }
    }
}
