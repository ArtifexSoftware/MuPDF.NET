using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class TestRelease
    {
        private static string SolutionRoot => _Path.ResolveSolutionRoot();

        private static (string mupdf, string mupdfNet) ReadVersionsProps()
        {
            string path = Path.Combine(SolutionRoot, "Versions.props");
            var doc = XDocument.Load(path);
            string? Get(string name) =>
                doc.Descendants(name).FirstOrDefault()?.Value?.Trim();
            return (Get("ArtifexMuPDFVersion")!, Get("ArtifexMuPDFNetVersion")!);
        }

        [Fact]
        public void test_release_changelog_version()
        {
            // In CHANGELOG.md, first item must match ArtifexMuPDFNetVersion.
            var (_, versionNet) = ReadVersionsProps();
            string path = Path.Combine(SolutionRoot, "MuPDF.NET\\CHANGELOG.md");
            string text = File.ReadAllText(path).Replace("\r\n", "\n");
            var m = Regex.Match(text, @"\n### \[([0-9.]+)\] - [0-9]{4}-[0-9]{2}-[0-9]{2}\n");
            Assert.True(m.Success, $"Cannot parse {path}.");
            Assert.Equal(versionNet, m.Groups[1].Value);
        }
    }
}
