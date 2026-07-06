using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// MuPDF runs <c>docs/samples/*.py</c> scripts. MuPDF.NET has no equivalent sample tree in-repo;
    /// this test documents that gap with the same exclusions as Python.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestDocsSamples
    {
        private static readonly HashSet<string> ExcludedBasenames = new(StringComparer.OrdinalIgnoreCase)
        {
            "make-bold.py",
            "multiprocess-gui.py",
            "multiprocess-render.py",
            "text-lister.py",
        };

        [Fact]
        public void test_docs_samples()
        {
            string root = Path.GetFullPath(Path.Combine(_Path.ResolveSolutionRoot(), "..", "PyMuPDF-1.28.0"));
            string samplesDir = Path.Combine(root, "docs", "samples");
            if (!Directory.Exists(samplesDir))
            {
                Console.WriteLine($"test_docs_samples(): not running because samples dir missing: {samplesDir}");
                return;
            }

            var samples = Directory.GetFiles(samplesDir, "*.py")
                .Select(Path.GetFileName)
                .Where(n => n != null && !ExcludedBasenames.Contains(n))
                .OrderBy(n => n)
                .ToList();

            Console.WriteLine($"test_docs_samples(): {samples.Count} PyMuPDF sample scripts found (not executed on .NET).");
            foreach (string sample in samples)
                Console.WriteLine($"    Not testing on .NET: {sample}");

            Assert.NotEmpty(samples);
        }
    }
}