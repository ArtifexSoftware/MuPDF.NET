using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Shared helpers for locating test resources and common assertions.
    /// </summary>
    internal static class TestHelper
    {
        private static readonly string ProjectDir = FindProjectDir();

        /// <summary>
        /// Returns the absolute path to a file inside tests/resources.
        /// </summary>
        internal static string GetResource(string filename)
        {
            return Path.Combine(ProjectDir, "tests", "resources", filename);
        }

        /// <summary>
        /// Check that two floating-point values are approximately equal.
        /// </summary>
        internal static bool IsClose(double a, double b, double eps = 1e-5)
        {
            return Math.Abs(a - b) < eps;
        }

        private static string FindProjectDir()
        {
            // Walk up from the output directory to find the repo root (contains tests/)
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 10; i++)
            {
                if (Directory.Exists(Path.Combine(dir, "tests", "resources")))
                    return dir;
                var parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            // Fallback: use current directory
            return Directory.GetCurrentDirectory();
        }
    }
}
