// Port of PyMuPDF-1.27.2.2/tests/test_2548.py
using System;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test2548
    {
        private static readonly string testDoc = _Path.ForTestClass("test_2548.pdf", nameof(Test2548));

        [Fact]
        public void test_2548()
        {
            // Text extraction should fail because of PDF structure cycle.
            // Old MuPDF version did not detect the loop.
            Console.WriteLine($"test_2548(): mupdf_version_tuple={_Version.mupdf_version_tuple()}");
            Tools.MupdfWarnings(reset: true);
            using var doc = new Document(testDoc);
            bool e = false;
            foreach (var page in doc)
            {
                try
                {
                    _ = page.GetText();
                }
                catch (Exception ee)
                {
                    Console.WriteLine($"test_2548: ee={ee}");
                    string expected = "RuntimeError('code=2: cycle in structure tree')";
                    Assert.Equal(expected, $"{ee.GetType().Name}('{ee.Message}')");
                    e = true;
                }
            }
            string wt = Tools.MupdfWarnings();
            Console.WriteLine($"test_2548(): wt={wt}");

            // This checks that PyMuPDF 1.23.7 fixes this bug, and also that earlier
            // versions with updated MuPDF also fix the bug.
            string expectedWt;
            var ver = _Version.mupdf_version_tuple();
            if (ver.CompareTo((1, 27, 1)) >= 0)
                expectedWt = "";
            else if (ver.CompareTo((1, 27, 0)) >= 0)
            {
                expectedWt = "format error: No common ancestor in structure tree\nstructure tree broken, assume tree is missing";
                expectedWt = string.Join("\n", Enumerable.Repeat(expectedWt, 5));
            }
            else
                expectedWt = "format error: cycle in structure tree\nstructure tree broken, assume tree is missing";
            Assert.Equal(expectedWt, wt);
            Assert.False(e);
        }
    }
}
