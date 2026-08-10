using Newtonsoft.Json.Linq;

namespace Demo
{
    internal partial class Program
    {
        private static string NationalCapitalsPdf => DemoPaths.Input("Llm/national-capitals.pdf");

        internal static void TestTableExtract1()
        {
            MuPDF4LLM.UseLayout = true;
            JArray pages = GetPagesFromJson(MuPDF4LLM.ToJson(NationalCapitalsPdf));

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
                    {
                        Console.WriteLine(string.Join(" | ", row ?? []));
                    }
                }
            }
        }

        internal static void TestTableExtract2()
        {
            MuPDF4LLM.UseLayout = true;
            JArray pages = GetPagesFromJson(MuPDF4LLM.ToJson(NationalCapitalsPdf));
            var csvLines = new List<string>();

            foreach (JObject page in pages)
            {
                foreach (JObject box in (page["boxes"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                {
                    if (!string.Equals(box["boxclass"]?.Value<string>(), "table", StringComparison.Ordinal)) continue;

                    var rows = ParseTableRows(box["table"]);
                    foreach (var row in rows)
                    {
                        var escaped = (row ?? []).Select(EscapeCsvCell);
                        csvLines.Add(string.Join(",", escaped));
                    }

                    csvLines.Add(string.Empty);
                }
            }

            string outPath = DemoPaths.Output("tables.csv");
            File.WriteAllLines(outPath, csvLines, Encoding.UTF8);

            Console.WriteLine($"Write to {outPath}");
        }

        internal static void TestTableExtract3()
        {
            MuPDF4LLM.UseLayout = true;
            JArray pages = GetPagesFromJson(MuPDF4LLM.ToJson(NationalCapitalsPdf));
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
            {
                Console.WriteLine(string.Join(" | ", row ?? []));
            }
        }

        internal static void TestOcr()
        {
            MuPDF4LLM.UseLayout = true;
            string ocrPdf = DemoPaths.Input("Ocr.pdf");
            string md = MuPDF4LLM.ToMarkdown(ocrPdf, useOcr: true, writeImages: false, embedImages: false);
            Console.WriteLine(md);
            string text = MuPDF4LLM.ToText(ocrPdf, useOcr: true);
            Console.WriteLine(text);
        }

        internal static void TestLLM2()
        {
            MuPDF4LLM.UseLayout = true;
            var reader = MuPDF4LLM.LlamaMarkdownReader();
            var chunks = reader.LoadData(DemoPaths.Input("Magazine.pdf"));

            string outDir = DemoPaths.Output("pages");
            Directory.CreateDirectory(outDir);
            foreach (var chunk in chunks)
            {
                int pageNum = (int)chunk.ExtraInfo["page"];
                Console.WriteLine(pageNum);
                string filePath = Path.Combine(outDir, $"page-{pageNum}.md");
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

        private static string EscapeCsvCell(string cell)
        {
            cell ??= "";
            return cell.Contains(',') || cell.Contains('"')
                ? $"\"{cell.Replace("\"", "\"\"")}\""
                : cell;
        }
    }
}
