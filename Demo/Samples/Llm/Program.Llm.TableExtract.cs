using Newtonsoft.Json.Linq;

namespace Demo
{
    internal partial class Program
    {
        internal static void TestTableExtract1()
        {
            JArray pages = GetPagesFromJson(PdfExtractor.ToJson(@"..\..\..\TestDocuments\national-capitals.pdf"));

            foreach (JObject page in pages)
            {
                int pageNum = page["page_number"]!.Value<int>();
                Console.WriteLine($"\nPage {pageNum}");

                foreach (JObject box in (page["boxes"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                {
                    if (!string.Equals(box["boxclass"]?.Value<string>(), "table", StringComparison.Ordinal)) continue;

                    var rows = ParseTableRows(box["table"]);
                    int rowCount = rows.Count;
                    int columnCount = rowCount > 0 ? rows.Max(r => r?.Count ?? 0) : 0;
                    Console.WriteLine($"Table: {rowCount} rows x {columnCount} columns");

                    foreach (var row in rows)
                        Console.WriteLine(string.Join(" | ", row ?? []));
                }
            }
        }

        internal static void TestTableExtract2()
        {
            JArray pages = GetPagesFromJson(PdfExtractor.ToJson(@"..\..\..\TestDocuments\national-capitals.pdf"));
            var csvLines = new List<string>();

            foreach (JObject page in pages)
            {
                foreach (JObject box in (page["boxes"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                {
                    if (!string.Equals(box["boxclass"]?.Value<string>(), "table", StringComparison.Ordinal)) continue;

                    var rows = ParseTableRows(box["table"]);
                    foreach (var row in rows)
                    {
                        var escaped = (row ?? []).Select(cell =>
                            cell.Contains(',') || cell.Contains('"')
                                ? $"\"{cell.Replace("\"", "\"\"")}\""
                                : cell
                        );
                        csvLines.Add(string.Join(",", escaped));
                    }

                    csvLines.Add(string.Empty);
                }
            }

            File.WriteAllLines("tables.csv", csvLines, Encoding.UTF8);
        }

        internal static void TestTableExtract3()
        {
            JArray pages = GetPagesFromJson(PdfExtractor.ToJson(@"..\..\..\TestDocuments\national-capitals.pdf"));
            var mergedRows = new List<List<string>>();
            int? prevColCount = null;

            foreach (JObject page in pages)
            {
                foreach (JObject box in (page["boxes"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                {
                    if (!string.Equals(box["boxclass"]?.Value<string>(), "table", StringComparison.Ordinal)) continue;

                    var rows = ParseTableRows(box["table"]);
                    if (rows.Count == 0)
                    {
                        prevColCount = null;
                        continue;
                    }

                    int colCount = rows.Max(r => r?.Count ?? 0);
                    if (colCount > 0 && colCount == prevColCount)
                        mergedRows.AddRange(rows.Skip(1));
                    else
                        mergedRows.AddRange(rows);

                    prevColCount = colCount > 0 ? colCount : null;
                }
            }

            Console.WriteLine($"Merged table: {mergedRows.Count} rows");
            foreach (var row in mergedRows)
                Console.WriteLine(string.Join(" | ", row ?? []));
        }

        internal static void TestOcr()
        {
            PdfExtractor.ToMarkdown(@"..\..\..\TestDocuments\Ocr.pdf", useOcr: true, writeImages: false, embedImages: false);
            string text = PdfExtractor.ToText(@"..\..\..\TestDocuments\Ocr.pdf", useOcr: true);
            Console.WriteLine(text);
        }

        internal static void TestLLM2()
        {
            var reader = PdfExtractor.LlamaMarkdownReader();
            var chunks = reader.LoadData(@"..\..\..\TestDocuments\magazine.pdf");

            Directory.CreateDirectory("Output");
            foreach (var chunk in chunks)
            {
                int pageNum = (int)chunk.ExtraInfo["page"];
                Console.WriteLine(pageNum);
                string filePath = $"output/page-{pageNum}.md";
                File.WriteAllText(filePath, chunk.Text, Encoding.UTF8);
            }
        }

        private static JArray GetPagesFromJson(string json)
        {
            JToken root = JToken.Parse(json);
            return root switch
            {
                JArray arr => arr,
                JObject obj when obj["pages"] is JArray arr => arr,
                _ => throw new InvalidOperationException("Expected a JSON array or an object containing a 'pages' array.")
            };
        }

        private static List<List<string>> ParseTableRows(JToken tableToken) =>
            tableToken switch
            {
                JArray arr => arr.ToObject<List<List<string>>>() ?? [],
                JObject obj when obj["extract"] is JArray extract => extract.ToObject<List<List<string>>>() ?? [],
                _ => []
            };
    }
}
