// Ports of MuPDF-1.28.0 dev/lint tests (N/A on MuPDF.NET — parity stubs with Python skip messages).
using System;
using System.IO;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class TestDevInfrastructure
    {
        private static string SolutionRoot => _Path.ResolveSolutionRoot();

        [Fact]
        public void test_codespell()
        {
            // Check Python code with codespell.
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PYODIDE_ROOT")))
            {
                Console.WriteLine("test_codespell(): not running on Pyodide - cannot run child processes.");
                return;
            }
            Console.WriteLine("test_codespell(): Not running on MuPDF.NET - codespell targets PyMuPDF Python sources.");
        }

        [Fact]
        public void test_flake8()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PYODIDE_ROOT")))
            {
                Console.WriteLine("test_flake8(): not running on Pyodide - cannot run child processes.");
                return;
            }
            Console.WriteLine("test_flake8(): Not running on MuPDF.NET - flake8 targets PyMuPDF Python sources.");
        }

        [Fact]
        public void test_pylint()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PYODIDE_ROOT")))
            {
                Console.WriteLine("test_pylint(): not running on Pyodide - cannot run child processes.");
                return;
            }
            Console.WriteLine("test_pylint(): Not running on MuPDF.NET - pylint targets PyMuPDF Python sources.");
        }

        [Fact]
        public void test_py_typed()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PYODIDE_ROOT")))
            {
                Console.WriteLine("test_py_typed(): not running on Pyodide - cannot run child processes.");
                return;
            }
            // MuPDF.NET ships as a .NET assembly; no py.typed marker.
            Console.WriteLine("test_py_typed(): Not running on MuPDF.NET - mypy/py.typed applies to the Python package only.");
        }
    }
}