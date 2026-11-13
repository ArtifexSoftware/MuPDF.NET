using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for PDF EmbeddedFiles functions.
    /// </summary>
    /// <remarks>
    /// In-memory only (no <c>TestDocuments</c> inputs or outputs).
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestEmbeddedFiles
    {
        private const string TestClassName = nameof(TestEmbeddedFiles);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);
        /// <summary>Regression test: embedded1.</summary>
        [Fact]
        public void test_embedded1()
        {
            using var doc = new Document();
            Page page = doc.NewPage();
            byte[] buffer = Encoding.ASCII.GetBytes("123456678790qwexcvnmhofbnmfsdg4589754uiofjkb-");
            doc.AddEmbeddedFile(
                "file1",
                buffer,
                filename: "testfile.txt",
                uFileName: "testfile-u.txt",
                desc: "Description of some sort");
            Assert.Equal(1, doc.EmbeddedFileCount);
            Assert.Equal(new List<string> { "file1" }, doc.GetEmbeddedFileNames());
            Assert.Equal("file1", doc.GetEmbeddedFileInfo(0)["name"]);
            doc.UpdateEmbeddedFile(0, filename: "new-filename.txt");
            Assert.Equal("new-filename.txt", doc.GetEmbeddedFileInfo(0)["filename"]);
            Assert.Equal(buffer, doc.GetEmbeddedFile(0));
            doc.Save(Out("test_embedded1.pdf"));
            doc.DeleteEmbeddedFile(0);
            Assert.Equal(0, doc.EmbeddedFileCount);
        }

        /// <summary>Regression test: 4050.</summary>
        [Fact]
        public void test_4050()
        {
            using var document = new Document();
            Page page = document.NewPage();
            document.AddEmbeddedFile("test", Encoding.ASCII.GetBytes("foobar"), desc: "some text");
            var d = new Dictionary<string, object>(document.GetEmbeddedFileInfo("test"));
            d.Remove("creationDate");
            d.Remove("modDate");
            var expected = new Dictionary<string, object>
            {
                ["name"] = "test",
                ["collection"] = 0,
                ["filename"] = "test",
                ["ufilename"] = "test",
                ["description"] = "some text",
                ["size"] = 6,
                ["length"] = 6,
            };
            Assert.Equal(expected.Count, d.Count);
            foreach (var kv in expected)
            {
                Assert.True(d.ContainsKey(kv.Key), $"missing key {kv.Key}");
                Assert.Equal(kv.Value, d[kv.Key]);
            }
            document.Save(Out("test_4050.pdf"));
        }
    }
}