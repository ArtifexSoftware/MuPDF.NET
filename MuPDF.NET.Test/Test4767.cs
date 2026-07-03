// Regression test for embed-extract path traversal (issue 4767).
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Checks path traversal safety of the embed-extract CLI.
    /// MuPDF.NET has no equivalent CLI; when <c>PYMUPDF_TEST_4767_PYTHON</c> points at
    /// <c>python.exe</c>, runs the Python test subprocess against a generated PDF.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class Test4767
    {
        private const string TestClassName = nameof(Test4767);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_4767()
        {
            // Check handling of unsafe paths in embed-extract.
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PYODIDE_ROOT")))
            {
                Console.WriteLine("test_4767(): not running on Pyodide - cannot run child processes.");
                return;
            }

            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true"
                && Environment.GetEnvironmentVariable("CIBUILDWHEEL") == "1"
                && OperatingSystem.IsWindows())
            {
                Console.WriteLine("test_4767(): not running because known to fail on Github/Windows/Cibuildwheel.");
                return;
            }

            string? python = Environment.GetEnvironmentVariable("PYMUPDF_TEST_4767_PYTHON");
            if (string.IsNullOrWhiteSpace(python))
            {
                Console.WriteLine("test_4767(): skipping — set PYMUPDF_TEST_4767_PYTHON to run pymupdf embed-extract CLI test.");
                return;
            }

            string pyTest = Path.GetFullPath(Path.Combine(
                _Path.ResolveSolutionRoot(), "..", "PyMuPDF-1.28.0", "tests", "test_4767.py"));
            if (!File.Exists(pyTest))
            {
                Console.WriteLine($"test_4767(): not running because missing: {pyTest}");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = python,
                Arguments = $"-c \"import importlib.util, sys; spec=importlib.util.spec_from_file_location('t4767', r'{pyTest.Replace("'", "''")}'); m=importlib.util.module_from_spec(spec); spec.loader.exec_module(m); m.test_4767()\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var proc = Process.Start(psi)!;
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            Console.WriteLine(stdout);
            if (!string.IsNullOrEmpty(stderr))
                Console.WriteLine(stderr);
            Assert.Equal(0, proc.ExitCode);
        }
    }
}