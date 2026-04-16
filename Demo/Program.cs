using System.Text.Json;
using System.Text.Json.Serialization;

namespace Demo
{
    /// <summary>
    /// GitHub samples entry point. With no arguments, all samples run; see <see cref="SampleMenu"/>.
    /// </summary>
    internal partial class Program
    {
        /// <summary>Repository root on disk — change this if your clone is elsewhere.</summary>
        private const string RepoRoot = @"E:\MuPDF.NET\Github\10apr2026-maksym-pdf4llm";

        private const string TestLlmInputPdf = RepoRoot + @"\PDF4LLM.Test\resources\national-capitals.pdf";
        //private const string TestLlmInputPdf = RepoRoot + @"\PDF4LLM.Test\resources\140.pdf";
        //private const string TestLlmInputPdf = RepoRoot + @"\PDF4LLM.Test\resources\columns.pdf";
        //private const string TestLlmInputPdf = RepoRoot + @"\PDF4LLM.Test\resources\Magazine.pdf";

        private const string TestLlmOutputDir = RepoRoot + @"\Demo";

        private const string TestIssueInputPdf = TestLlmInputPdf;
        private const string TestIssueOutputDir = RepoRoot + @"\Demo\PDF4LLM_smoke_output";

        private static void Main(string[] args)
        {
            //SampleMenu.Run(args);
            TestLLM();
        }

        /// <summary>
        /// Port of <c>Demo/test.py</c> <c>write_all_pymupdf4llm_outputs</c> (fixed paths: <see cref="TestLlmInputPdf"/>, <see cref="TestLlmOutputDir"/>).
        /// </summary>
        internal static void TestLLM()
        {
            Directory.CreateDirectory(TestLlmOutputDir);

            if (!File.Exists(TestLlmInputPdf))
                throw new FileNotFoundException($"TestLLM PDF not found: {TestLlmInputPdf}");

            Dictionary<string, string> written = WriteAllPdf4LlmOutputs(TestLlmInputPdf, TestLlmOutputDir);
            foreach (KeyValuePair<string, string> kv in written.OrderBy(k => k.Key, StringComparer.Ordinal))
                Console.WriteLine($"{kv.Key}: {(string.IsNullOrEmpty(kv.Value) ? "(skipped)" : kv.Value)}");
        }

        /// <summary>Same outputs as <c>test.py</c> <c>write_all_pymupdf4llm_outputs</c>.</summary>
        /// <remarks>Empty string means that output was skipped (e.g. not supported).</remarks>
        private static Dictionary<string, string> WriteAllPdf4LlmOutputs(string pdfPath, string outDir)
        {
            var written = new Dictionary<string, string>();

            using (Document doc = new Document(pdfPath))
            {
                string md = ToMarkdown(doc, showProgress: false, useOcr: false);
                string pMd = Path.Combine(outDir, "out_to_markdown.md");
                WriteSmokeFile(outDir, "out_to_markdown.md", md);
                written["to_markdown"] = pMd;

                try
                {
                    string js = ToJson(doc, showProgress: false, useOcr: false);
                    string pJs = Path.Combine(outDir, "out_to_json.json");
                    WriteSmokeFile(outDir, "out_to_json.json", js);
                    written["to_json"] = pJs;
                }
                catch (NotSupportedException)
                {
                    written["to_json"] = "";
                }

                try
                {
                    string tx = ToText(doc, showProgress: false, useOcr: false);
                    string pTx = Path.Combine(outDir, "out_to_text.txt");
                    WriteSmokeFile(outDir, "out_to_text.txt", tx);
                    written["to_text"] = pTx;
                }
                catch (NotSupportedException)
                {
                    written["to_text"] = "";
                }

                var kv = GetKeyValues(doc, xrefs: false);
                string pKv = Path.Combine(outDir, "get_key_values_result.json");
                WriteKeyValuesFile(outDir, "get_key_values_result.json", kv);
                written["get_key_values"] = pKv;
            }

            (int maj, int min, int pat) = VersionTuple;
            var verPayload = new
            {
                __version__ = global::PDF4LLM.PdfExtractor.Version,
                version = global::PDF4LLM.PdfExtractor.Version,
                version_tuple = new[] { maj, min, pat }
            };
            string pVer = Path.Combine(outDir, "pymupdf4llm_version.json");
            File.WriteAllText(
                pVer,
                JsonSerializer.Serialize(verPayload, new JsonSerializerOptions { WriteIndented = true }),
                Encoding.UTF8);
            written["version"] = pVer;

            try
            {
                var hdr = new IdentifyHeaders(pdfPath);
                string sample = hdr.GetHeaderId(new ExtendedSpan { Size = 8f, Text = "body" });
                var hdrPayload = new Dictionary<string, string> { ["get_header_id_body_span"] = sample ?? "" };
                string pHdr = Path.Combine(outDir, "identify_headers_sample.json");
                File.WriteAllText(
                    pHdr,
                    JsonSerializer.Serialize(hdrPayload, new JsonSerializerOptions { WriteIndented = true }),
                    Encoding.UTF8);
                written["IdentifyHeaders"] = pHdr;
            }
            catch
            {
                written["IdentifyHeaders"] = "";
            }

            try
            {
                var reader = LlamaMarkdownReader(meta => meta);
                List<LlamaIndexDocument> docs = reader.LoadData(pdfPath);
                string preview = docs.Count > 0
                    ? (docs[0].Text.Length <= 500 ? docs[0].Text : docs[0].Text.Substring(0, 500))
                    : "";
                var llPayload = new { num_documents = docs.Count, first_text_preview = preview };
                string pLl = Path.Combine(outDir, "llama_markdown_reader_result.json");
                File.WriteAllText(
                    pLl,
                    JsonSerializer.Serialize(llPayload, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }),
                    Encoding.UTF8);
                written["LlamaMarkdownReader"] = pLl;
            }
            catch
            {
                written["LlamaMarkdownReader"] = "";
            }

            return written;
        }

        private static void WriteSmokeFile(string outDir, string fileName, string content)
        {
            string path = Path.Combine(outDir, fileName);
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        private static void WriteKeyValuesFile(string outDir, string fileName, Dictionary<string, Dictionary<string, object>> kv)
        {
            try
            {
                string json = JsonSerializer.Serialize(
                    kv,
                    new JsonSerializerOptions { WriteIndented = true });
                WriteSmokeFile(outDir, fileName, json);
            }
            catch
            {
                var sb = new StringBuilder();
                foreach (KeyValuePair<string, Dictionary<string, object>> outer in kv)
                {
                    sb.AppendLine($"[{outer.Key}]");
                    if (outer.Value == null)
                        continue;
                    foreach (KeyValuePair<string, object> inner in outer.Value)
                        sb.AppendLine($"  {inner.Key} = {inner.Value}");
                }

                WriteSmokeFile(outDir, Path.ChangeExtension(fileName, ".txt"), sb.ToString());
            }
        }
    }
}
