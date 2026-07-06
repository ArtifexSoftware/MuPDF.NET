using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class TestRelease
    {
        private static string SolutionRoot => _Path.ResolveSolutionRoot();

        private static (string pymupdf, string mupdf, string mupdfNet) ReadVersionsProps()
        {
            string path = Path.Combine(SolutionRoot, "Versions.props");
            var doc = XDocument.Load(path);
            string? Get(string name) =>
                doc.Descendants(name).FirstOrDefault()?.Value?.Trim();
            return (Get("ArtifexPyMuPDFVersion")!, Get("ArtifexMuPDFVersion")!, Get("ArtifexMuPDFNetVersion")!);
        }

        private static (int major, int minor) VersionMajorMinor(string version)
        {
            Assert.False(string.IsNullOrWhiteSpace(version), "version must not be empty");
            var parts = version.Split('.');
            Assert.True(parts.Length >= 2, $"version must have major.minor: {version}");
            return (int.Parse(parts[0]), int.Parse(parts[1]));
        }

        [Fact]
        public void test_release_versions()
        {
            // MuPDF and default MuPDF must have same major.minor version.
            var (versionPymupdf, versionMupdf, _) = ReadVersionsProps();
            var pymupdfTuple = VersionMajorMinor(versionPymupdf);
            var mupdfTuple = VersionMajorMinor(versionMupdf);
            Assert.True(
                pymupdfTuple == mupdfTuple,
                $"PyMuPDF and MuPDF major.minor versions do not match. version_pymupdf={versionPymupdf} version_mupdf={versionMupdf}.");
        }

        [Fact]
        public void test_release_changelog_version()
        {
            // In CHANGELOG.md, first item must match ArtifexMuPDFNetVersion.
            var (_, _, versionNet) = ReadVersionsProps();
            string path = Path.Combine(SolutionRoot, "CHANGELOG.md");
            string text = File.ReadAllText(path).Replace("\r\n", "\n");
            var m = Regex.Match(text, @"\n### \[([0-9.]+)\] - [0-9]{4}-[0-9]{2}-[0-9]{2}\n");
            Assert.True(m.Success, $"Cannot parse {path}.");
            Assert.Equal(versionNet, m.Groups[1].Value);
        }

        [Fact]
        public void test_release_bug_template()
        {
            // Bug report template must list current MuPDF version.
            var (versionPymupdf, _, _) = ReadVersionsProps();
            string path = Path.Combine(SolutionRoot, "..", "PyMuPDF-1.28.0", ".github", "ISSUE_TEMPLATE", "bug_report.yml");
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
            {
                Console.WriteLine($"test_release_bug_template(): not running — missing {path}");
                return;
            }
            string expected = $"\n        - {versionPymupdf}\n";
            string text = File.ReadAllText(path);
            Assert.Contains(expected, text);
        }

        [Fact]
        public void test_release_changelog_mupdf_version()
        {
            // In MuPDF changes.txt, first MuPDF mention must match ArtifexMuPDFVersion.
            var (_, versionMupdf, _) = ReadVersionsProps();
            string path = Path.Combine(SolutionRoot, "..", "PyMuPDF-1.28.0", "changes.txt");
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
            {
                Console.WriteLine($"test_release_changelog_mupdf_version(): not running — missing {path}");
                return;
            }
            string text = File.ReadAllText(path);
            var m = Regex.Match(text, @"\n\* Use MuPDF-([0-9.]+(-rc[0-9])?)\.\n");
            Assert.True(m.Success, $"Cannot parse {path}.");
            Assert.Equal(versionMupdf, m.Groups[1].Value);
        }
    }
}