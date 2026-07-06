// 1. Read metadata and compare with stored expected result.
// 2. Erase metadata and assert object has indeed been deleted.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestMetadata/</c>; outputs: <c>TestDocuments/_Output/TestMetadata/</c>.
    /// Tests run in order and share <see cref="doc"/> (see <see cref="TestMetadataCollection"/>).
    /// </remarks>
    [Collection("TestMetadata")]
    [TestCaseOrderer("MuPDF.NET.Test.TestMetadataCollectionOrderer", "MuPDF.NET.Test")]
    public class TestMetadata
    {
        private const string TestClassName = nameof(TestMetadata);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static readonly Document doc = new Document(Doc("001003ED.pdf"));

        private static string JsonDumpsMetadata(Dictionary<string, string> metadata)
        {
            // json.dumps(doc.metadata) — spaces after ',' and ':', encryption is null
            var parts = new List<string>();
            foreach (var key in MetadataKeyOrder)
            {
                if (!metadata.TryGetValue(key, out string? value))
                    value = "";
                if (key == "encryption" && (string.IsNullOrEmpty(value) || value == "None"))
                    parts.Add($"\"{key}\": null");
                else
                    parts.Add($"\"{key}\": {JsonEncodeString(value)}");
            }
            return "{" + string.Join(", ", parts) + "}";
        }

        private static string JsonEncodeString(string value) =>
            "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        private static readonly string[] MetadataKeyOrder =
        {
            "format", "title", "author", "subject", "keywords", "creator", "producer",
            "creationDate", "modDate", "trapped", "encryption",
        };

        private static byte[] ReprEncodeUtf8(Dictionary<string, string> metadata)
        {
            // repr(metadata).encode('utf8') — Python 3 dict repr
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var key in MetadataKeyOrder)
            {
                if (!first)
                    sb.Append(", ");
                first = false;
                sb.Append('\'');
                sb.Append(key);
                sb.Append("': ");
                if (key == "encryption")
                {
                    string? enc = metadata.TryGetValue(key, out string? v) ? v : "";
                    if (string.IsNullOrEmpty(enc) || enc == "None")
                        sb.Append("None");
                    else
                        sb.Append('\'').Append(PythonReprString(enc)).Append('\'');
                }
                else if (!metadata.TryGetValue(key, out string? value))
                    sb.Append("''");
                else
                    sb.Append(PythonReprValue(value));
            }
            sb.Append('}');
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string PythonReprValue(string value)
        {
            if (value.Contains('\'') && !value.Contains('"'))
                return "\"" + value + "\"";
            return "'" + PythonReprString(value) + "'";
        }

        private static string PythonReprString(string value) =>
            value.Replace("\\", "\\\\").Replace("'", "\\'");

        /// <summary>Regression test: metadata.</summary>
        [Fact]
        public void test_metadata()
        {
            Assert.Equal(File.ReadAllText(Doc("metadata.txt")), JsonDumpsMetadata(doc.Metadata));
        }

        /// <summary>Regression test: erase meta.</summary>
        [Fact]
        public void test_erase_meta()
        {
            doc.SetMetadata(new Dictionary<string, string>());
            // Check PDF trailer and assert that there is no more /Info object
            // or is set to "null".
            // statement1 = doc.XrefGetKey(-1, "Info")[1] == "null"
            bool statement1 = doc.XrefGetKey(-1, "Info").value == "null";
            // statement2 = "Info" not in doc.xref_get_keys(-1)
            bool statement2 = !doc.xref_get_keys(-1).Contains("Info");
            Assert.True(statement1);
            Assert.Contains("Info", doc.xref_get_keys(-1));
        }

        /// <summary>Regression test: 3237.</summary>
        [Fact]
        public void test_3237()
        {
            using (var doc3237 = new Document(Doc("001003ED.pdf")))
            {
                // We need to explicitly encode in utf8 on windows.
                // metadata1 = doc.metadata
                var metadata1 = doc3237.Metadata;
                // metadata1 = repr(metadata1).encode('utf8')
                byte[] metadata1Bytes = ReprEncodeUtf8(metadata1);
                doc3237.SetMetadata(new Dictionary<string, string>());

                // metadata2 = doc.metadata
                var metadata2 = doc3237.Metadata;
                // metadata2 = repr(metadata2).encode('utf8')
                byte[] metadata2Bytes = ReprEncodeUtf8(metadata2);
                Console.WriteLine($"metadata1={Encoding.UTF8.GetString(metadata1Bytes)}");
                Console.WriteLine($"metadata2={Encoding.UTF8.GetString(metadata2Bytes)}");
                Assert.Equal(
                    Encoding.UTF8.GetBytes("{'format': 'PDF 1.6', 'title': 'RUBRIK_Editorial_01-06.indd', 'author': 'Natalie Schaefer', 'subject': '', 'keywords': '', 'creator': '', 'producer': 'Acrobat Distiller 7.0.5 (Windows)', 'creationDate': \"D:20070113191400+01'00'\", 'modDate': \"D:20070120104154+01'00'\", 'trapped': '', 'encryption': None}"),
                    metadata1Bytes);
                Assert.Equal(
                    Encoding.UTF8.GetBytes("{'format': 'PDF 1.6', 'title': '', 'author': '', 'subject': '', 'keywords': '', 'creator': '', 'producer': '', 'creationDate': '', 'modDate': '', 'trapped': '', 'encryption': None}"),
                    metadata2Bytes);
            }
        }
    }

    [CollectionDefinition("TestMetadata", DisableParallelization = true)]
    public class TestMetadataCollection
    {
    }

    public class TestMetadataCollectionOrderer : ITestCaseOrderer
    {
        private static readonly Dictionary<string, int> Order = new(StringComparer.Ordinal)
        {
            ["test_metadata"] = 0,
            ["test_erase_meta"] = 1,
            ["test_3237"] = 2,
        };

        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase
        {
            return testCases.OrderBy(tc =>
                Order.TryGetValue(tc.TestMethod.Method.Name, out int o) ? o : int.MaxValue);
        }
    }
}