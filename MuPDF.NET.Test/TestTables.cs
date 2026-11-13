using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using MuPDF.NET;
namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestTables/</c>; outputs: <c>TestDocuments/_Output/TestTables/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestTables
    {
        private const string TestClassName = nameof(TestTables);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static readonly string filename = Doc("chinese-tables.pdf");
        private static readonly string pickle_file = Doc("chinese-tables.pickle");

        private static bool HasFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Required test document not found: {path}");
            return true;
        }

        private static bool CellsEqual(
            IReadOnlyList<(float x0, float y0, float x1, float y1)> a,
            IReadOnlyList<(float x0, float y0, float x1, float y1)> b,
            float tol = 1e-3f)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (Math.Abs(a[i].x0 - b[i].x0) > tol ||
                    Math.Abs(a[i].y0 - b[i].y0) > tol ||
                    Math.Abs(a[i].x1 - b[i].x1) > tol ||
                    Math.Abs(a[i].y1 - b[i].y1) > tol)
                    return false;
            }
            return true;
        }

        private static bool NullableCellsEqual(
            IReadOnlyList<(float x0, float y0, float x1, float y1)?> a,
            IReadOnlyList<(float x0, float y0, float x1, float y1)?> b,
            float tol = 1e-3f)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] is null != b[i] is null) return false;
                if (a[i] is null) continue;
                var ca = a[i]!.Value;
                var cb = b[i]!.Value;
                if (Math.Abs(ca.x0 - cb.x0) > tol ||
                    Math.Abs(ca.y0 - cb.y0) > tol ||
                    Math.Abs(ca.x1 - cb.x1) > tol ||
                    Math.Abs(ca.y1 - cb.y1) > tol)
                    return false;
            }
            return true;
        }

        private static string PprintExtract(List<List<string?>> extract)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < extract.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('[');
                var row = extract[i];
                for (int j = 0; j < row.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    var item = row[j];
                    if (item is null)
                        sb.Append("None");
                    else
                        sb.Append('\'').Append(item.Replace("'", "\\'")).Append('\'');
                }
                sb.Append(']');
            }
            sb.Append(']');
            sb.AppendLine();
            return sb.ToString();
        }

        private static string Dedent(string text)
        {
            var lines = text.Replace("\r\n", "\n").Trim('\n').Split('\n');
            if (lines.Length == 0)
                return "";
            int minIndent = int.MaxValue;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                int indent = line.Length - line.TrimStart(' ').Length;
                if (indent < minIndent)
                    minIndent = indent;
            }
            if (minIndent == int.MaxValue)
                minIndent = 0;
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                    sb.Append('\n');
                var line = lines[i];
                if (line.Length >= minIndent)
                    sb.Append(line.Substring(minIndent));
                else
                    sb.Append(line);
            }
            return sb.ToString();
        }

        private static void AssertExtractRowsEqual(
            List<List<string?>> expected,
            List<List<string?>> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            for (int r = 0; r < expected.Count; r++)
                Assert.Equal(expected[r], actual[r]);
        }

        /// <summary><c>util._use_layout</c>.</summary>
        private static bool UseLayout() =>
            Environment.GetEnvironmentVariable("MUPDF_NET_TEST_USE_LAYOUT") != "0";

        /// <summary><c>util._use_4llm</c>.</summary>
        private static bool Use4llm() =>
            Environment.GetEnvironmentVariable("MUPDF_NET_TEST_USE_4LLM") != "0";

        private static List<List<List<string?>>> ExtractAll(TableFinder tabs) =>
            tabs.Tables.Select(t => t.Extract()).ToList();

        private static void CheckTables(
            List<List<List<string?>>> expected,
            List<List<List<string?>>> actual)
        {
            if (TablesEqual(expected, actual))
            return;
            Console.WriteLine("Table different from expected.");
            Console.WriteLine("Expected:");
            Console.WriteLine(FormatTables(expected));
            Console.WriteLine("Actual:");
            Console.WriteLine(FormatTables(actual));
            Assert.Fail("tables != expected");
        }

        private static bool TablesEqual(
            List<List<List<string?>>> a,
            List<List<List<string?>>> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Count != b[i].Count) return false;
                for (int r = 0; r < a[i].Count; r++)
                {
                    if (a[i][r].Count != b[i][r].Count) return false;
                    for (int c = 0; c < a[i][r].Count; c++)
                    {
                        if (a[i][r][c] != b[i][r][c])
                            return false;
                    }
                }
            }
            return true;
        }

        private static string FormatTables(List<List<List<string?>>> tables)
        {
            var sb = new StringBuilder();
            sb.AppendLine("    [");
            foreach (var t in tables)
            {
                sb.AppendLine("        [");
                foreach (var row in t)
                {
                    sb.Append("            [");
                    foreach (var cell in row)
                    {
                        if (cell is null)
                            sb.Append("null, ");
                        else
                            sb.Append('\'').Append(cell.Replace("\n", "\\n").Replace("'", "\\'")).Append("', ");
                    }
                    sb.AppendLine("],");
                }
                sb.AppendLine("        ],");
            }
            sb.AppendLine("    ]");
            return sb.ToString();
        }

        private static void CheckTableCellsMatch(
            List<List<float[]>> expected,
            IReadOnlyList<IReadOnlyList<(float x0, float y0, float x1, float y1)>> actual,
            float epsilon = 0.5f)
        {
            if (TableCellsMatch(expected, actual, epsilon))
                return;
            Console.WriteLine("Table cells differ too much.");
            Console.WriteLine($"expected tables={expected.Count} actual={actual.Count}");
            for (int t = 0; t < Math.Max(expected.Count, actual.Count); t++)
            {
                int en = t < expected.Count ? expected[t].Count : -1;
                int an = t < actual.Count ? actual[t].Count : -1;
                Console.WriteLine($"  table {t}: expected cells={en} actual={an}");
                if (en > 0 && an > 0)
                {
                    int n = Math.Min(3, Math.Min(en, an));
                    for (int i = 0; i < n; i++)
                    {
                        var e = expected[t][i];
                        var a = actual[t][i];
                        Console.WriteLine($"    [{i}] expected=[{e[0]}, {e[1]}, {e[2]}, {e[3]}]");
                        Console.WriteLine($"        actual  =[{a.x0}, {a.y0}, {a.x1}, {a.y1}]");
                    }
                }
            }
            Assert.Fail("table cells differ too much");
        }

        private static bool TableCellsMatch(
            List<List<float[]>> expected,
            IReadOnlyList<IReadOnlyList<(float x0, float y0, float x1, float y1)>> actual,
            float epsilon)
        {
            if (expected.Count != actual.Count) return false;
            for (int t = 0; t < expected.Count; t++)
            {
                if (expected[t].Count != actual[t].Count) return false;
                for (int i = 0; i < expected[t].Count; i++)
                {
                    var e = expected[t][i];
                    var a = actual[t][i];
                    if (Math.Abs(e[0] - a.x0) >= epsilon ||
                        Math.Abs(e[1] - a.y0) >= epsilon ||
                        Math.Abs(e[2] - a.x1) >= epsilon ||
                        Math.Abs(e[3] - a.y1) >= epsilon)
                        return false;
                }
            }
            return true;
        }

        private static List<List<List<string?>>> DottedGridLayoutExtract() =>
            new()
            {
                new()
                {
                    new() { "REGIONE", "PROVINCIA/\nCITTA'\nMETROPOLITANA", "COMUNE", "NUMERO\nCOMUNI", "COMUNI\nCAPOLUOGO\n(CAP)", "COMUNI\nSUPERIORI\n15000\n.\nabitanti\n(SUP)", "COMUNI\nPARIO\nINFERIORI\n15000\n.\nabitanti\n(INF)", "COMUNIAL\nRINNOVOPER\nMOTIVIDIVERSI\nDASCADENZA\nNATURALE(*)", "COMUNI\nSCIOLTIAL\nRINNOVOPER\nSCADENZA\nNATURALE(#)", "POPOLAZIONE\nal31/12/2021\nDPR20/01/2023\n...", "SEZIONI\nRilevazioneal\n31/12/2023", "ELETTORI\nRilevazioneal\n31/12/2023" },
                    new() { "", "", "CORVARA", "1", "", "", "INF", "", "", "206", "1", "510" },
                    new() { "", "", "FARINDOLA", "1", "", "", "INF", "", "", "1357\n.", "2", "1578\n." },
                    new() { "", "", "LETTOMANOPPELLO", "1", "", "", "INF", "", "", "2713\n.", "3", "2710\n." },
                    new() { "", "", "MONTEBELLODIBERTONA", "1", "", "", "INF", "", "", "883", "2", "1139\n." },
                    new() { "", "", "MONTESILVANO", "1", "", "SUP", "", "", "", "53402\n.", "52", "44600\n." },
                    new() { "", "", "MOSCUFO", "1", "", "", "INF", "", "", "3092\n.", "3", "2851\n." },
                    new() { "", "", "ROSCIANO", "1", "", "", "INF", "", "", "4038\n.", "6", "3697\n." },
                    new() { "", "", "SALLE", "1", "", "", "INF", "", "", "270", "1", "698" },
                    new() { "", "", "VICOLI", "1", "", "", "INF", "", "", "381", "1", "495" },
                    new() { "", "Totale", "", "17", "1", "2", "15", "0", "0", "209303\n.", "267", "185620\n." },
                    new() { "", "", "", "", "", "", "", "", "", "", "", "" },
                    new() { "", "TERAMO", "", "", "", "", "", "", "", "", "", "" },
                    new() { "", "", "ANCARANO", "1", "", "", "INF", "", "", "1811\n.", "2", "1620\n." },
                    new() { "", "", "ARSITA", "1", "", "", "INF", "", "", "756", "1", "874" },
                    new() { "", "", "ATRI", "1", "", "", "INF", "*", "", "10064\n.", "14", "9986\n." },
                    new() { "", "", "CAMPLI", "1", "", "", "INF", "", "", "6630\n.", "11", "6568\n." },
                    new() { "", "", "CANZANO", "1", "", "", "INF", "", "", "1794\n.", "3", "1653\n." },
                    new() { "", "", "CASTIGLIONEMESSERRAIMONDO", "1", "", "", "INF", "", "", "2052\n.", "4", "1917\n." },
                    new() { "", "", "CELLINOATTANASIO", "1", "", "", "INF", "", "", "2274\n.", "3", "2531\n." },
                    new() { "", "", "CERMIGNANO", "1", "", "", "INF", "", "", "1459\n.", "3", "1839\n." },
                    new() { "", "", "COLLEDARA", "1", "", "", "INF", "", "", "2097\n.", "4", "2281\n." },
                    new() { "", "", "CORROPOLI", "1", "", "", "INF", "", "", "5108\n.", "5", "4467\n." },
                    new() { "", "", "FANOADRIANO", "1", "", "", "INF", "", "", "257", "2", "277" },
                    new() { "", "", "GIULIANOVA", "1", "", "SUP", "", "", "", "23442\n.", "23", "21806\n." },
                    new() { "", "", "MONTEFINO", "1", "", "", "INF", "", "", "967", "2", "915" },
                    new() { "", "", "MORROD'ORO", "1", "", "", "INF", "", "", "3560\n.", "3", "3232\n." },
                    new() { "", "", "MOSCIANOSANT'ANGELO", "1", "", "", "INF", "", "", "9088\n.", "9", "8365\n." },
                    new() { "", "", "PENNASANT'ANDREA", "1", "", "", "INF", "", "", "1635\n.", "2", "1778\n." },
                    new() { "", "", "PINETO", "1", "", "", "INF", "", "#", "14538\n.", "11", "13363\n." },
                    new() { "", "", "ROCCASANTAMARIA", "1", "", "", "INF", "", "", "477", "1", "537" },
                    new() { "", "", "SANT'EGIDIOALLAVIBRATA", "1", "", "", "INF", "", "", "9804\n.", "7", "8244\n." },
                    new() { "", "", "SANT'OMERO", "1", "", "", "INF", "", "", "5112\n.", "7", "4767\n." },
                    new() { "", "", "TORANONUOVO", "1", "", "", "INF", "", "", "1490\n.", "2", "1280\n." },
                    new() { "", "", "TORRICELLASICURA", "1", "", "", "INF", "", "", "2460\n.", "4", "2658\n." },
                    new() { "", "", "TOSSICIA", "1", "", "", "INF", "", "#", "1258\n.", "3", "1388\n." },
                    new() { "", "Totale", "", "23", "0", "1", "22", "1", "2", "108133\n.", "126", "102346\n." },
                    new() { "", "", "", "", "", "", "", "", "", "", "", "" },
                    new() { "BRUZZO", "", "", "98", "1", "3", "95", "2", "2", "424264\n.", "537", "405785\n." },
                },
            };

        private static List<List<List<string?>>> Expected4017Layout() =>
            new()
            {
                new()
                {
                    new() { "ClassA/BOvercollateralization", "13144%\n.", ">=", "12260%\n.", "", "PASS" },
                    new() { null, null, null, null, null, "PASS" },
                    new() { "ClassDOvercollateralization", "11224%\n.", ">=", "10640%\n.", "", "PASS" },
                    new() { null, null, null, null, null, "PASS" },
                    new() { "EventofDefault", "15608%\n.", ">=", "10250%\n.", "", "PASS" },
                    new() { null, null, null, null, null, "PASS" },
                    new() { "ClassA/BInterestCoverage", "N/A", ">=", "12000%\n.", "", "N/A" },
                    new() { null, null, null, null, null, "N/A" },
                    new() { "ClassDInterestCoverage", "N/A", ">=", "10500%\n.", "", "N/A" },
                },
                new()
                {
                    new() { "Moody'sMaximumRatingFactorTest", "2577\n,", "<=", "3250\n,", "", "PASS", "2581\n," },
                    new() { null, null, null, null, null, "PASS", null },
                    new() { "MinimumFloatingSpread", "35006%\n.", ">=", "20000%\n.", "", "PASS", "34871%\n." },
                    new() { null, null, null, null, null, "PASS", null },
                    new() { "MinimumWeightedAverageS&PRecovery\nRateTest", "4050%\n.", ">=", "4000%\n.", "", "PASS", "4040%\n." },
                    new() { null, null, null, null, null, "PASS", null },
                    new() { "WeightedAverageLife", "483\n.", "<=", "900\n.", "", "PASS", "492\n." },
                },
                new()
                {
                    new() { "Issuer", "OCPCLO202432 LTD\n- , .", "", "", "Count", "", "", "Current" },
                    new() { "Co-Issuer", "OCPCLO202432LLC\n-", "DelayedDrawLoan", "", "1", "", "", "6500000\n, ." },
                    new() { "CollateralTrustee", "Citibank N .A\n.", "RevolvingLoan", "", "0", "", "", "000\n." },
                    new() { "CollateralManager", "Onex Credit Partners, LLC", "TermLoan", "", "293", "", "", "37486898542\n, , ." },
                    new() { "RatingAgencies\nCollateralAdministrator", "S&P\nSiepeLLC", "MiscellaneousInformation", null, null, null, null, "" },
                    new() { "RelationshipManager", "SabrinaSchmidt", "AggregatePrincipalBalance", null, "", "", "", "38603398542\n, , ." },
                    new() { "", "sschmidt@siepe.com", "PrincipalCash", "", "", "", "", "1281594162\n, , ." },
                    new() { "", "2818704754\n- -", "PFAI", "", "", "", "", "72757624\n, ." },
                    new() { "ClosingDate", "04/23/2024", "Totals", "", "", "", "", "39957750328\n, , ." },
                    new() { "FirstPaymentDate", "10/23/2024", "", "", "", "", "", "" },
                    new() { "ReinvestmentPeriod\nEffectiveDate", "04/23/2024 04/23/2029\n-\n04/25/2024", "AccountBalances", null, "", "", "", "" },
                    new() { "NextPaymentDate\nPriorPaymentDate", "10/23/2024\n-", "PrincipalCash\nInterestCash", "", "", "", "", "1281594162\n, , .\n1300009090\n, , ." },
                    new() { "CollectionPeriod", "04/23/2024 10/08/2024\n-", "", "", "", "", "", "" },
                },
                new()
                {
                    new() { "Notes", "OriginalBalance", "CurrentBalance", "Spread", "Coupon", "Interest" },
                    new() { "Class A -1 Notes", "256000000\n, ,", "256000000\n, ,", "152000%\n.", "682458%\n.", "888105344\n, , ." },
                    new() { "Class A -2 Notes", "16000000\n, ,", "16000000\n, ,", "172000%\n.", "702458%\n.", "57133251\n, ." },
                    new() { "Class B -1 Notes", "24000000\n, ,", "24000000\n, ,", "200000%\n.", "730458%\n.", "89115876\n, ." },
                    new() { "Class B -2 Notes", "8000000\n, ,", "8000000\n, ,", "584100%\n.", "584100%\n.", "23364000\n, ." },
                    new() { "ClassCNotes", "24000000\n, ,", "24000000\n, ,", "250000%\n.", "780458%\n.", "95215876\n, ." },
                    new() { "Class D -1 Notes", "24000000\n, ,", "24000000\n, ,", "375000%\n.", "905458%\n.", "110465876\n, , ." },
                    new() { "Class D -2 Notes", "4000000\n, ,", "4000000\n, ,", "905000%\n.", "905000%\n.", "18100000\n, ." },
                    new() { "ClassENotes", "12000000\n, ,", "12000000\n, ,", "676000%\n.", "1206458%\n.", "73593938\n, ." },
                    new() { "", "36800000000\n, , .", "36800000000\n, , .", "", "", "1355094161\n, , ." },
                },
            };

        private static List<List<List<string?>>> SmallTableLayoutExtract() =>
            new()
            {
                new()
                {
                    new() { "Boiling Points °C", "min", "max", "avg" },
                    new() { "Noble gases", "269\n-", "62\n-", "170 5\n- ." },
                    new() { "Nonmetals", "253\n-", "4827", "414 1\n." },
                    new() { "Metalloids", "335", "3900", "741 5\n." },
                    new() { "Metals", "357", ">5000", "2755 9\n." },
                },
            };

        private static List<List<List<string?>>> ChineseTablesExpectedExtract() =>
            new()
            {
                new()
                {
                    new() { "年份", "主体信用\n评级", "涵义", "评级机构", "评级展望" },
                    new() { "2023", "AA+", "偿还债务的能力很强 受不利经济\n，\n环境的影响不大 违约风险很低\n，", "中诚信国际信\n用评级有限责\n任公司 联合资\n、\n信评估有限公\n司", "稳定" },
                    new() { "2022", "AA+", "偿还债务的能力很强 受不利经济\n，\n环境的影响不大 违约风险很低\n，", "中诚信国际信\n用评级有限责\n任公司 联合资\n、\n信评估有限公\n司", "稳定" },
                    new() { "2021", "AA+", "偿还债务的能力很强 受不利经济\n，\n环境的影响不大 违约风险很低\n，", "联合资信评估\n有限公司", "稳定" },
                    new() { "2020", "AA+", "偿还债务的能力很强 受不利经济\n，\n环境的影响不大 违约风险很低\n，", "联合资信评估\n有限公司", "稳定" },
                },
                new()
                {
                    new() { "序号", "金融机构名称", "授信总额", "已使用情况", "剩余额度" },
                    new() { "1", "工商银行", "33043530\n, .", "23485000\n, .", "9558530\n, ." },
                    new() { "2", "徽商银行南京中山北路支行", "33043530\n, .", "25673530\n, .", "7370000\n, ." },
                    new() { "3", "兴业银行盐城分行", "30260000\n, .", "11060000\n, .", "19200000\n, ." },
                    new() { "4", "民生银行盐城分行", "26200000\n, .", "8850000\n, .", "17350000\n, ." },
                    new() { "5", "中信银行盐城分行", "20508800\n, .", "20508800\n, .", "000\n." },
                    new() { "6", "南京银行盐城分行", "17517000\n, .", "17517000\n, .", "000\n." },
                    new() { "7", "杭州银行", "10900000\n, .", "3269000\n, .", "7631000\n, ." },
                    new() { "8", "江苏银行", "21859700\n, .", "21859700\n, .", "000\n." },
                    new() { "9", "渤海银行", "9800000\n, .", "000\n.", "9800000\n, ." },
                    new() { "10", "恒丰银行南京江北支行", "9390000\n, .", "4350000\n, .", "5040000\n, ." },
                    new() { "11", "上海银行盐城分行", "4626500\n, .", "4626500\n, .", "000\n." },
                },
            };

        private static List<List<float[]>> ChineseTablesExpectedCells()
        {
            return new()
            {
                new()
                {
                    new float[] { 89.76499938964844f, 172.41000366210938f, 147.75f, 201.11000061035156f },
                    new float[] { 89.76499938964844f, 201.11000061035156f, 147.75f, 269.9599914550781f },
                    new float[] { 89.76499938964844f, 269.9599914550781f, 147.75f, 338.80999755859375f },
                    new float[] { 89.76499938964844f, 338.80999755859375f, 147.75f, 366.760009765625f },
                    new float[] { 89.76499938964844f, 366.760009765625f, 147.75f, 395.47999572753906f },
                    new float[] { 147.75f, 172.41000366210938f, 204.5f, 201.11000061035156f },
                    new float[] { 147.75f, 201.11000061035156f, 204.5f, 269.9599914550781f },
                    new float[] { 147.75f, 269.9599914550781f, 204.5f, 338.80999755859375f },
                    new float[] { 147.75f, 338.80999755859375f, 204.5f, 366.760009765625f },
                    new float[] { 147.75f, 366.760009765625f, 204.5f, 395.47999572753906f },
                    new float[] { 204.5f, 172.41000366210938f, 374.20001220703125f, 201.11000061035156f },
                    new float[] { 204.5f, 201.11000061035156f, 374.20001220703125f, 269.9599914550781f },
                    new float[] { 204.5f, 269.9599914550781f, 374.20001220703125f, 338.80999755859375f },
                    new float[] { 204.5f, 338.80999755859375f, 374.20001220703125f, 366.760009765625f },
                    new float[] { 204.5f, 366.760009765625f, 374.20001220703125f, 395.47999572753906f },
                    new float[] { 374.20001220703125f, 172.41000366210938f, 448.2250061035156f, 201.11000061035156f },
                    new float[] { 374.20001220703125f, 201.11000061035156f, 448.2250061035156f, 269.9599914550781f },
                    new float[] { 374.20001220703125f, 269.9599914550781f, 448.2250061035156f, 338.80999755859375f },
                    new float[] { 374.20001220703125f, 338.80999755859375f, 448.2250061035156f, 366.760009765625f },
                    new float[] { 374.20001220703125f, 366.760009765625f, 448.2250061035156f, 395.47999572753906f },
                    new float[] { 448.2250061035156f, 172.41000366210938f, 505.510009765625f, 201.11000061035156f },
                    new float[] { 448.2250061035156f, 201.11000061035156f, 505.510009765625f, 269.9599914550781f },
                    new float[] { 448.2250061035156f, 269.9599914550781f, 505.510009765625f, 338.80999755859375f },
                    new float[] { 448.2250061035156f, 338.80999755859375f, 505.510009765625f, 366.760009765625f },
                    new float[] { 448.2250061035156f, 366.760009765625f, 505.510009765625f, 395.47999572753906f },
                },
                new()
                {
                    new float[] { 89.76499938964844f, 556.760009765625f, 113.25f, 573.7899780273438f },
                    new float[] { 89.76499938964844f, 573.7899780273438f, 113.25f, 590.0399780273438f },
                    new float[] { 89.76499938964844f, 590.0399780273438f, 113.25f, 606.2899780273438f },
                    new float[] { 89.76499938964844f, 606.2899780273438f, 113.25f, 622.5399780273438f },
                    new float[] { 89.76499938964844f, 622.5399780273438f, 113.25f, 638.7899780273438f },
                    new float[] { 89.76499938964844f, 638.7899780273438f, 113.25f, 655.0399780273438f },
                    new float[] { 89.76499938964844f, 655.0399780273438f, 113.25f, 671.2899780273438f },
                    new float[] { 89.76499938964844f, 671.2899780273438f, 113.25f, 687.5399780273438f },
                    new float[] { 89.76499938964844f, 687.5399780273438f, 113.25f, 703.7899780273438f },
                    new float[] { 89.76499938964844f, 703.7899780273438f, 113.25f, 720.0399780273438f },
                    new float[] { 89.76499938964844f, 720.0399780273438f, 113.25f, 736.2899780273438f },
                    new float[] { 89.76499938964844f, 736.2899780273438f, 113.25f, 753.2799987792969f },
                    new float[] { 113.25f, 556.760009765625f, 325.1499938964844f, 573.7899780273438f },
                    new float[] { 113.25f, 573.7899780273438f, 325.1499938964844f, 590.0399780273438f },
                    new float[] { 113.25f, 590.0399780273438f, 325.1499938964844f, 606.2899780273438f },
                    new float[] { 113.25f, 606.2899780273438f, 325.1499938964844f, 622.5399780273438f },
                    new float[] { 113.25f, 622.5399780273438f, 325.1499938964844f, 638.7899780273438f },
                    new float[] { 113.25f, 638.7899780273438f, 325.1499938964844f, 655.0399780273438f },
                    new float[] { 113.25f, 655.0399780273438f, 325.1499938964844f, 671.2899780273438f },
                    new float[] { 113.25f, 671.2899780273438f, 325.1499938964844f, 687.5399780273438f },
                    new float[] { 113.25f, 687.5399780273438f, 325.1499938964844f, 703.7899780273438f },
                    new float[] { 113.25f, 703.7899780273438f, 325.1499938964844f, 720.0399780273438f },
                    new float[] { 113.25f, 720.0399780273438f, 325.1499938964844f, 736.2899780273438f },
                    new float[] { 113.25f, 736.2899780273438f, 325.1499938964844f, 753.2799987792969f },
                    new float[] { 325.1499938964844f, 556.760009765625f, 383.1000061035156f, 573.7899780273438f },
                    new float[] { 325.1499938964844f, 573.7899780273438f, 383.1000061035156f, 590.0399780273438f },
                    new float[] { 325.1499938964844f, 590.0399780273438f, 383.1000061035156f, 606.2899780273438f },
                    new float[] { 325.1499938964844f, 606.2899780273438f, 383.1000061035156f, 622.5399780273438f },
                    new float[] { 325.1499938964844f, 622.5399780273438f, 383.1000061035156f, 638.7899780273438f },
                    new float[] { 325.1499938964844f, 638.7899780273438f, 383.1000061035156f, 655.0399780273438f },
                    new float[] { 325.1499938964844f, 655.0399780273438f, 383.1000061035156f, 671.2899780273438f },
                    new float[] { 325.1499938964844f, 671.2899780273438f, 383.1000061035156f, 687.5399780273438f },
                    new float[] { 325.1499938964844f, 687.5399780273438f, 383.1000061035156f, 703.7899780273438f },
                    new float[] { 325.1499938964844f, 703.7899780273438f, 383.1000061035156f, 720.0399780273438f },
                    new float[] { 325.1499938964844f, 720.0399780273438f, 383.1000061035156f, 736.2899780273438f },
                    new float[] { 325.1499938964844f, 736.2899780273438f, 383.1000061035156f, 753.2799987792969f },
                    new float[] { 383.1000061035156f, 556.760009765625f, 448.2250061035156f, 573.7899780273438f },
                    new float[] { 383.1000061035156f, 573.7899780273438f, 448.2250061035156f, 590.0399780273438f },
                    new float[] { 383.1000061035156f, 590.0399780273438f, 448.2250061035156f, 606.2899780273438f },
                    new float[] { 383.1000061035156f, 606.2899780273438f, 448.2250061035156f, 622.5399780273438f },
                    new float[] { 383.1000061035156f, 622.5399780273438f, 448.2250061035156f, 638.7899780273438f },
                    new float[] { 383.1000061035156f, 638.7899780273438f, 448.2250061035156f, 655.0399780273438f },
                    new float[] { 383.1000061035156f, 655.0399780273438f, 448.2250061035156f, 671.2899780273438f },
                    new float[] { 383.1000061035156f, 671.2899780273438f, 448.2250061035156f, 687.5399780273438f },
                    new float[] { 383.1000061035156f, 687.5399780273438f, 448.2250061035156f, 703.7899780273438f },
                    new float[] { 383.1000061035156f, 703.7899780273438f, 448.2250061035156f, 720.0399780273438f },
                    new float[] { 383.1000061035156f, 720.0399780273438f, 448.2250061035156f, 736.2899780273438f },
                    new float[] { 383.1000061035156f, 736.2899780273438f, 448.2250061035156f, 753.2799987792969f },
                    new float[] { 448.2250061035156f, 556.760009765625f, 505.510009765625f, 573.7899780273438f },
                    new float[] { 448.2250061035156f, 573.7899780273438f, 505.510009765625f, 590.0399780273438f },
                    new float[] { 448.2250061035156f, 590.0399780273438f, 505.510009765625f, 606.2899780273438f },
                    new float[] { 448.2250061035156f, 606.2899780273438f, 505.510009765625f, 622.5399780273438f },
                    new float[] { 448.2250061035156f, 622.5399780273438f, 505.510009765625f, 638.7899780273438f },
                    new float[] { 448.2250061035156f, 638.7899780273438f, 505.510009765625f, 655.0399780273438f },
                    new float[] { 448.2250061035156f, 655.0399780273438f, 505.510009765625f, 671.2899780273438f },
                    new float[] { 448.2250061035156f, 671.2899780273438f, 505.510009765625f, 687.5399780273438f },
                    new float[] { 448.2250061035156f, 687.5399780273438f, 505.510009765625f, 703.7899780273438f },
                    new float[] { 448.2250061035156f, 703.7899780273438f, 505.510009765625f, 720.0399780273438f },
                    new float[] { 448.2250061035156f, 720.0399780273438f, 505.510009765625f, 736.2899780273438f },
                    new float[] { 448.2250061035156f, 736.2899780273438f, 505.510009765625f, 753.2799987792969f },
                },
            };
        }

        [Fact]
        public void test_table1()
        {
            if (!HasFile(filename)) return;
            if (UseLayout())
            {
                using var document = new Document(filename);
                var page = document[0];
                var tables = page.FindTables();
                CheckTables(ChineseTablesExpectedExtract(), ExtractAll(tables));
                var cells = tables.Tables.Select(t => (IReadOnlyList<(float x0, float y0, float x1, float y1)>)t.Cells).ToList();
                Console.WriteLine("cells are:");
                CheckTableCellsMatch(ChineseTablesExpectedCells(), cells);
            }
            else
            {
                Console.WriteLine("test_table1(): pickle compare skipped (Python pickle).");
            }
        }

        [Fact]
        public void test_table2()
        {
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            var page = doc[0];
            if (UseLayout())
            {
                var tables = page.FindTables();
                CheckTables(ChineseTablesExpectedExtract(), ExtractAll(tables));
            }
            else
            {
                var tab1 = page.FindTables().Tables[0];
                var tab2 = page.FindTables().Tables[1];
            Assert.False(tab1.Header.External);
            Assert.True(NullableCellsEqual(tab1.Header.Cells, tab1.Rows[0].Cells));
            Assert.False(tab2.Header.External);
            Assert.True(NullableCellsEqual(tab2.Header.Cells, tab2.Rows[0].Cells));
            }
        }

        [Fact]
        public void test_2812()
        {
            // Make 4 pages with rotations 0, 90, 180 and 270 degrees respectively.
            // Each page shows the same 8x5 table.
            // We will check that each table is detected and delivers the same content.
            using var doc = new Document();
            // Page 0: rotation 0

            var page = doc.NewPage(width: 842, height: 595);
            Rect rect = page.Rect + new Rect(72, 72, -72, -72);
            int cols = 5;
            int rows = 8;
            // define the cells, draw the grid and insert unique text in each cell.
            var cells = Utils.MakeTable(rect, rows: rows, cols: cols);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    page.DrawRect(cells[i][j]);
            }
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    page.InsertTextbox(
                        cells[i][j],
                        $"cell[{i}][{j}]",
                        align: Constants.TextAlignCenter);
                }
            }
            page.CleanContents();
            // Page 1: rotation 90 degrees

            page = doc.NewPage();
            rect = page.Rect + new Rect(72, 72, -72, -72);
            cols = 8;
            rows = 5;
            cells = Utils.MakeTable(rect, rows: rows, cols: cols);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    page.DrawRect(cells[i][j]);
            }
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    page.InsertTextbox(
                        cells[i][j],
                        $"cell[{j}][{rows - i - 1}]",
                        rotate: 90,
                        align: Constants.TextAlignCenter);
                }
            }
            page.SetRotation(90);
            page.CleanContents();

            // Page 2: rotation 180 degrees

            page = doc.NewPage(width: 842, height: 595);
            rect = page.Rect + new Rect(72, 72, -72, -72);
            cols = 5;
            rows = 8;
            cells = Utils.MakeTable(rect, rows: rows, cols: cols);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    page.DrawRect(cells[i][j]);
            }
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    page.InsertTextbox(
                        cells[i][j],
                        $"cell[{rows - i - 1}][{cols - j - 1}]",
                        rotate: 180,
                        align: Constants.TextAlignCenter);
                }
            }
            page.SetRotation(180);
            page.CleanContents();

            // Page 3: rotation 270 degrees

            page = doc.NewPage();
            rect = page.Rect + new Rect(72, 72, -72, -72);
            cols = 8;
            rows = 5;
            cells = Utils.MakeTable(rect, rows: rows, cols: cols);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    page.DrawRect(cells[i][j]);
            }
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    page.InsertTextbox(
                        cells[i][j],
                        $"cell[{cols - j - 1}][{i}]",
                        rotate: 270,
                        align: Constants.TextAlignCenter);
                }
            }
            page.SetRotation(270);
            page.CleanContents();

            byte[] pdfdata = doc.ToBytes();
            doc.Close();

            // -------------------------------------------------------------------------
            // Test PDF prepared. Extract table on each page and
            // ensure identical extracted table data.
            // -------------------------------------------------------------------------
            using var doc2 = new Document(pdfdata, "pdf");
            var extracts = new List<string>();
            foreach (var page2 in doc2)
            {
                var tabs = page2.FindTables();
                Assert.Single(tabs.Tables);
                var tab = tabs[0];
                extracts.Add(PprintExtract(tab.Extract()));
                Assert.Equal(8, tab.RowCount);
                Assert.Equal(5, tab.ColCount);
            }
            string e0 = extracts[0];
            foreach (var e in extracts.Skip(1))
            {
                Assert.Equal(e0, e);
            }
            doc2.Save(Out("test_2812.pdf"));
        }

        [Fact]
        public void test_2979()
        {
            // 2979: identical cell count for each row
            // 3001: no change of global glyph heights

            string filename = Doc("test_2979.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);

            var page = doc[0];
            var tab = page.FindTables()[0];
            var lengths = new HashSet<int>();
            foreach (var e in tab.Extract())
            {
                lengths.Add(e.Count);
            }

            // test 2979
            Assert.Single(lengths);

            // test 3001
            Assert.False(Tools.SetSmallGlyphHeights());

            string wt = Tools.MupdfWarnings();
            if (_Version.mupdf_version_tuple_at_least(1, 28, 0))
            {
                Assert.Equal("", wt);
            }
            else
            {
                // wt
                // == "bogus font ascent/descent values (3117 / -2463)\n... repeated 2 times..."
                Assert.Equal(
                    "bogus font ascent/descent values (3117 / -2463)\n... repeated 2 times...",
                    wt);
            }
        }

        [Fact]
        public void test_3062()
        {
            // After table extraction, a rotated page should behave and look
            // like as before."""
            // return


            string filename = Doc("test_3062.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);

            var page = doc[0];
            var tab0 = page.FindTables()[0];
            var cells0 = tab0.Cells;


            page = null;

            page = doc[0];
            var tab1 = page.FindTables()[0];
            var cells1 = tab1.Cells;
            Assert.True(CellsEqual(cells1, cells0));
        }

        [Fact]
        public void test_strict_lines()
        {

            string filename = Doc("strict-yes-no.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);

            var page = doc[0];

            var tab1 = page.FindTables()[0];
            var tab2 = page.FindTables(new TableSettings
            {
                VerticalStrategy = "lines_strict",
                HorizontalStrategy = "lines_strict",
            })[0];
            Assert.True(tab2.RowCount < tab1.RowCount);
            Assert.True(tab2.ColCount < tab1.ColCount);
        }

        [Fact]
        public void test_add_lines()
        {
            string filename = Doc("small-table.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            var page = doc[0];
            var tabs1 = page.FindTables();
            var moreLines = new (Point p1, Point p2)[]
            {
                (new Point(238.9949951171875f, 200f), new Point(238.9949951171875f, 300f)),
                (new Point(334.5559997558594f, 200f), new Point(334.5559997558594f, 300f)),
                (new Point(433.1809997558594f, 200f), new Point(433.1809997558594f, 300f)),
            };
            var tab2 = page.FindTables(addLines: moreLines)[0];
            if (UseLayout())
            {
                CheckTables(SmallTableLayoutExtract(), ExtractAll(tabs1));
            Assert.Equal(4, tab2.ColCount);
            Assert.Equal(5, tab2.RowCount);
            }
            else
            {
                Assert.Empty(tabs1.Tables);
                Assert.Equal(4, tab2.ColCount);
                Assert.Equal(5, tab2.RowCount);
            }
        }

        [Fact]
        public void test_3148()
        {
            using var doc = new Document();

            var page = doc.NewPage();
            Rect rect = new Rect(100, 100, 300, 300);
            string[] text =
            {
                "rotation 0 degrees",
                "rotation 90 degrees",
                "rotation 180 degrees",
                "rotation 270 degrees",
            };
            int[] degrees = { 0, 90, 180, 270 };
            Rect delta = new Rect(2, 2, -2, -2);
            var cells = Utils.MakeTable(rect, cols: 3, rows: 4);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    page.DrawRect(cells[j][i]);
                    int k = (i + j) % 4;
                    page.InsertTextbox(cells[j][i] + delta, text[k], rotate: degrees[k]);
                }
            }
            doc.Save(Out("test_3148.pdf"));
            var tabs = page.FindTables();
            if (UseLayout() && Use4llm())
            {
                Assert.Empty(tabs.Tables);
            }
            else
            {
                var tab = tabs[0];
                foreach (var extract in tab.Extract())
                {
                    foreach (var item in extract)
                    {
                        string s = (item ?? "").Replace("\n", " ");
                        Assert.Contains(s, text);
                    }
                }
            }
        }

        [Fact]
        public void test_3179()
        {

            string filename = Doc("test_3179.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);

            var page = doc[0];
            var tabs = page.FindTables();
            Assert.Equal(3, tabs.Tables.Count);
        }

        [Fact]
        public void test_battery_file()
        {
            // Earlier versions erroneously tried to identify table headers
            // where there existed no table at all.

            string filename = Doc("battery-file-22.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);

            var page = doc[0];
            var tabs = page.FindTables();
            Assert.Empty(tabs.Tables);
        }

        [Fact]
        public void test_markdown()
        {

            string filename = Doc("strict-yes-no.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);

            var page = doc[0];
            var tab = page.FindTables(new TableSettings
            {
                VerticalStrategy = "lines_strict",
                HorizontalStrategy = "lines_strict",
            })[0];
            string md_expected =
                    "|Header1|Header2|Header3|\n" +
                    "|---|---|---|\n" +
                    "|Col11<br>Col12|Col21<br>Col22|Col31<br>Col32<br>Col33|\n" +
                    "|Col13|Col23|Col34<br>Col35|\n" +
                    "|Col14|Col24|Col36|\n" +
                    "|Col15|Col25<br>Col26||\n\n";
            string md = tab.ToMarkdown();
            Assert.Equal(md_expected, md.Replace("\r\n", "\n"));
        }

        [Fact]
        public void test_paths_param()
        {

            string filename = Doc("strict-yes-no.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);

            var page = doc[0];
            var tabs = page.FindTables(paths: Array.Empty<Dictionary<string, object>>());
            if (UseLayout())
            {
                CheckTables(new List<List<List<string?>>>
                {
                    new()
                    {
                        new() { "Header1", "", "Header2", null, "Header3" },
                        new() { "Col11", "", "Col21", null, "Col31" },
                        new() { "Col12", "", "Col22", null, "Col32" },
                        new() { "", "", "", "", "Col33" },
                        new() { "Col13", "", "Col23", null, "Col34" },
                        new() { "", "", "", "", "Col35" },
                        new() { "Col14", "", "Col24", null, "Col36" },
                        new() { "Col15", "", "Col25", null, "" },
                        new() { "", "", "Col26", null, "" },
                    },
                }, ExtractAll(tabs));
            }
            else
            {
            Assert.Empty(tabs.Tables);
            }
        }

        [Fact]
        public void test_boxes_param()
        {

            string filename = Doc("small-table.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);

            var page = doc[0];
            var paths = page.GetDrawingsDict();
            var box0 = page.cluster_drawings(drawings: paths)[0];
            var boxes = new List<Rect> { box0 };
            var words = (List<WordBlock>)page.GetText("words");
            foreach (var w in words.Where(w => w.word is "min" or "max" or "avg"))
            {
                var r = +box0;
                r.X1 = w.x0 - 5;
                boxes.Add(r);
            }

            var yVals = words.Select(w => (int)Math.Round(w.y1)).Distinct().OrderBy(y => y).ToList();
            foreach (var y in yVals.Take(yVals.Count - 1))
            {
                var r = +box0;
                r.Y1 = y;
                boxes.Add(r);
            }

            var tabs = page.FindTables(
                paths: Array.Empty<Dictionary<string, object>>(),
                addBoxes: boxes);
            if (UseLayout())
            {
                CheckTables(SmallTableLayoutExtract(), new List<List<List<string?>>> { tabs.Tables[0].Extract() });
            }
            else
            {
            AssertExtractRowsEqual(
                new List<List<string?>>
                {
                    new List<string?> { "Boiling Points °C", "min", "max", "avg" },
                    new List<string?> { "Noble gases", "-269", "-62", "-170.5" },
                    new List<string?> { "Nonmetals", "-253", "4827", "414.1" },
                    new List<string?> { "Metalloids", "335", "3900", "741.5" },
                    new List<string?> { "Metals", "357", ">5000", "2755.9" },
                },
                    tabs.Tables[0].Extract());
            }
        }

        [Fact]
        public void test_dotted_grid()
        {

            string filename = Doc("dotted-gridlines.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);

            var page = doc[0];
            var tabs = page.FindTables();
            if (UseLayout())
            {
                CheckTables(DottedGridLayoutExtract(), ExtractAll(tabs));
            }
            else
            {
            Assert.Equal(3, tabs.Tables.Count);
            var t0 = tabs[0];
            var t1 = tabs[1];
            var t2 = tabs[2];
            Assert.Equal(11, t0.RowCount);
            Assert.Equal(12, t0.ColCount);
            Assert.Equal(25, t1.RowCount);
            Assert.Equal(11, t1.ColCount);
            Assert.Equal(1, t2.RowCount);
            Assert.Equal(10, t2.ColCount);
            }
        }

        [Fact]
        public void test_4017()
        {
            string path = Doc("test_4017.pdf");
            if (!HasFile(path)) return;
            using (var document = new Document(path))
            {

                var page = document[0];

                var tables = page.FindTables();
                Console.WriteLine($"len(tables.tables)={tables.Tables.Count}.");
                for (int i = 0; i < tables.Tables.Count; i++)
                {
                    Console.WriteLine($"## i={i}.");
                    var t = tables[i].Extract();
                    foreach (var tt in t)
                    {
                        Console.WriteLine($"    {tt}");
                    }

                }

                if (UseLayout())
                {
                    CheckTables(Expected4017Layout(), ExtractAll(tables));
                }
                else
                {
                var expected_a = new List<List<string?>>
                {
                    new() { "Class A/B Overcollateralization", "131.44%", ">=", "122.60%", "", "PASS" },
                    new() { null, null, null, null, null, "PASS" },
                    new() { "Class D Overcollateralization", "112.24%", ">=", "106.40%", "", "PASS" },
                    new() { null, null, null, null, null, "PASS" },
                    new() { "Event of Default", "156.08%", ">=", "102.50%", "", "PASS" },
                    new() { null, null, null, null, null, "PASS" },
                    new() { "Class A/B Interest Coverage", "N/A", ">=", "120.00%", "", "N/A" },
                    new() { null, null, null, null, null, "N/A" },
                    new() { "Class D Interest Coverage", "N/A", ">=", "105.00%", "", "N/A" },
                };
                AssertExtractRowsEqual(expected_a, tables[tables.Tables.Count - 2].Extract());
                var expected_b = new List<List<string?>>
                {
                        new() { "Moody's Maximum Rating Factor Test", "2,577", "<=", "3,250", "", "PASS", "2,581" },
                    new() { null, null, null, null, null, "PASS", null },
                        new() { "Minimum Floating Spread", "3.5006%", ">=", "2.0000%", "", "PASS", "3.4871%" },
                    new() { null, null, null, null, null, "PASS", null },
                    new()
                    {
                        "Minimum Weighted Average S&P Recovery\nRate Test",
                        "40.50%", ">=", "40.00%", "", "PASS", "40.40%",
                    },
                    new() { null, null, null, null, null, "PASS", null },
                    new() { "Weighted Average Life", "4.83", "<=", "9.00", "", "PASS", "4.92" },
                };
                AssertExtractRowsEqual(expected_b, tables[tables.Tables.Count - 1].Extract());
                }
            }
        }

        [Fact]
        public void test_md_styles()
        {

            string filename = Doc("test-styled-table.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);

            var page = doc[0];
            var tabs = page.FindTables()[0];
            string text;
            if (UseLayout())
            {
                text =
                    "|Column1|Column2|Column3|\n" +
                    "|---|---|---|\n" +
                    "|Zelle (0,0)|**Bold (0,1)**|Zelle (0,2)|\n" +
                    "|~~Strikeout (1,0), Zeile 1~~<br>~~Hier kommt Zeile 2.~~|Zelle (1,1)|~~Strikeout (1,2)~~|\n" +
                    "|**`Bold-monospaced`**<br>**`(2,0)`**|_Italic (2,1)_|**_Bold-italic_**<br>**_(2,2)_**|\n" +
                    "|Zelle (3,0)|~~**Bold-strikeout**~~<br>~~**(3,1)**~~|Zelle (3,2)|\n\n";
            }
            else
            {
                text =
                "|Column 1|Column 2|Column 3|\n" +
                "|---|---|---|\n" +
                "|Zelle (0,0)|**Bold (0,1)**|Zelle (0,2)|\n" +
                "|~~Strikeout (1,0), Zeile 1~~<br>~~Hier kommt Zeile 2.~~|Zelle (1,1)|~~Strikeout (1,2)~~|\n" +
                "|**`Bold-monospaced`**<br>**`(2,0)`**|_Italic (2,1)_|**_Bold-italic_**<br>**_(2,2)_**|\n" +
                "|Zelle (3,0)|~~**Bold-strikeout**~~<br>~~**(3,1)**~~|Zelle (3,2)|\n\n";
            }
            Console.WriteLine($"text:\n{text}");
            Console.WriteLine($"tabs.to_markdown():\n{tabs.ToMarkdown()}");
            Assert.Equal(text, tabs.ToMarkdown().Replace("\r\n", "\n"));
        }

        static byte[] _make_find_tables_state_doc()
        {
            using var doc = new Document();
            var page = doc.NewPage(width: 360, height: 220);
            var rect = new Rect(40, 40, 320, 180);
            var cells = Utils.MakeTable(rect, rows: 3, cols: 3);
            for (int rowIndex = 0; rowIndex < cells.Count; rowIndex++)
            {
                for (int colIndex = 0; colIndex < cells[rowIndex].Count; colIndex++)
                {
                    page.DrawRect(cells[rowIndex][colIndex]);
                    page.InsertTextbox(
                        cells[rowIndex][colIndex],
                        $"r{rowIndex}c{colIndex}",
                        align: Constants.TextAlignCenter);
                }
            }
            return doc.Write();
        }

        static (int rowCount, int colCount, List<List<string?>> extract) _find_tables_use_layout_false_signature(byte[] pdfBytes)
        {
            using var doc = new Document(pdfBytes, "pdf");
            var table = TableHelpers.FindTables(doc[0], strategy: "lines_strict", useLayout: false)[0];
            return (table.RowCount, table.ColCount, table.Extract());
        }

        [Fact]
        public void test_find_tables_use_layout_false_does_not_call_get_layout()
        {
            byte[] pdfBytes = _make_find_tables_state_doc();
            using var doc = new Document(pdfBytes, "pdf");
            var page = doc[0];
            var original = Page.GetLayoutProvider;
            Page.GetLayoutProvider = _ => throw new InvalidOperationException("get_layout() should not be called");
            try
            {
                var table = TableHelpers.FindTables(page, strategy: "lines_strict", useLayout: false)[0];
                Assert.Equal(3, table.RowCount);
                Assert.Equal(3, table.ColCount);
                Assert.Equal("r1c1", table.Extract()[1][1]);
            }
            finally
            {
                Page.GetLayoutProvider = original;
            }
        }

        [Fact]
        public void test_find_tables_state_is_call_local_for_threads()
        {
            // Concurrent FindTables calls must not mix text/vector extraction state.
            byte[] pdfBytes = _make_find_tables_state_doc();
            var expected = _find_tables_use_layout_false_signature(pdfBytes);

            // FindTables uses thread-local TOOLS-flag overrides; snapshot globals
            // so the suite's global-state checks stay clean.
            bool smallBefore = Tools.SetSmallGlyphHeights();
            bool quadBefore = Helpers.SkipQuadCorrections;
            try
            {
                var results = new (int, int, List<List<string?>>)[32];
                Parallel.For(0, 32, i =>
                {
                    results[i] = _find_tables_use_layout_false_signature(pdfBytes);
                });
                foreach (var r in results)
                {
                    Assert.Equal(expected.rowCount, r.Item1);
                    Assert.Equal(expected.colCount, r.Item2);
                    Assert.Equal(expected.extract, r.Item3);
                }
            }
            finally
            {
                Tools.SetSmallGlyphHeights(smallBefore);
                Helpers.SkipQuadCorrections = quadBefore;
            }
        }

        static Document _make_marker_table_doc(string marker)
        {
            // Build a 1-page doc with a small drawn table whose cells embed `marker`.
            var doc = new Document();
            var page = doc.NewPage(width: 360, height: 220);
            var rect = new Rect(40, 40, 320, 180);
            var cells = Utils.MakeTable(rect, rows: 2, cols: 2);
            for (int rowIndex = 0; rowIndex < cells.Count; rowIndex++)
            {
                for (int colIndex = 0; colIndex < cells[rowIndex].Count; colIndex++)
                {
                    page.DrawRect(cells[rowIndex][colIndex]);
                    page.InsertTextbox(
                        cells[rowIndex][colIndex],
                        $"{marker}-{rowIndex}{colIndex}",
                        align: Constants.TextAlignCenter);
                }
            }
            page.CleanContents();
            return doc;
        }

        [Fact]
        public void test_table_extract_stable_after_second_find_tables()
        {
            // Regression test for the stale-CHARS bug.

            // FindTables snapshots table._chars right after each call so that an
            // already-returned Table's extract cannot silently pick up a later,
            // unrelated FindTables call's live (ContextVar-backed) CHARS content.
            using var doc1 = _make_marker_table_doc("PAGE1");
            using var doc2 = _make_marker_table_doc("PAGE2");
            Exception? e = null;
            try
            {
            var table1 = TableHelpers.FindTables(doc1[0], strategy: "lines_strict")[0];
            var first = table1.Extract();

                // An unrelated FindTables call on a different page/doc resets and
            // repopulates the shared CHARS state used during text extraction.
            _ = TableHelpers.FindTables(doc2[0], strategy: "lines_strict");

            Assert.Equal(first, table1.Extract());
            string flatText = string.Join(" ", first.SelectMany(row => row).Where(c => c != null));
            Assert.Contains("PAGE1", flatText);  // guard against both-empty passes
            Assert.DoesNotContain("PAGE2", flatText);
            }
            catch (Exception ee)
            {
                e = ee;
            }
            if (UseLayout())
                Assert.NotNull(e);
            else
                Assert.Null(e);
        }

        [Fact]
        public void test_find_tables_use_layout_true_without_layout_is_line_based()
        {
            // available: get_layout becomes a no-op, page.layout_information stays
            byte[] pdfBytes = _make_find_tables_state_doc();
            using var doc = new Document(pdfBytes, "pdf");
            var page = doc[0];
            var original = Page.GetLayoutProvider;
            Page.GetLayoutProvider = null;  // simulate: layout wheel not installed
            try
            {
                var tablesTrue = TableHelpers.FindTables(page, strategy: "lines_strict", useLayout: true);
                Assert.Null(page.LayoutInformation);

                var tablesFalse = TableHelpers.FindTables(page, strategy: "lines_strict", useLayout: false);

                Assert.Single(tablesTrue.Tables);
                Assert.Equal(
                    tablesFalse.Tables.Select(t => t.Extract()).ToList(),
                    tablesTrue.Tables.Select(t => t.Extract()).ToList());
                var table = tablesTrue[0];
                Assert.Equal(3, table.RowCount);
                Assert.Equal(3, table.ColCount);
                Assert.Equal("r1c1", table.Extract()[1][1]);
            }
            finally
            {
                Page.GetLayoutProvider = original;
            }
        }

        static (Document doc, Page page) _make_overmerged_page()
        {
            // A page whose line grid detects one tall body row that actually holds
            // three record lines -- an under-segmented (over-merged) grid the refinement
            // is meant to repair. Needs no layout, so it exercises the standalone benefit.
            var doc = new Document();
            var page = doc.NewPage(width: 400, height: 300);
            // 2-column grid: header row 100-120, a single tall body row 120-200.
            foreach (float y in new float[] { 100, 120, 200 })
                page.DrawLine(new Point(100, y), new Point(300, y));
            foreach (float x in new float[] { 100, 200, 300 })
                page.DrawLine(new Point(x, 100), new Point(x, 200));
            page.InsertText(new Point(130, 114), "A");
            page.InsertText(new Point(230, 114), "B");
            // Three record lines crammed into the one body row.
            int i = 1;
            foreach (float y in new float[] { 140, 160, 180 })
            {
                page.InsertText(new Point(130, y), i.ToString());
                page.InsertText(new Point(230, y), (i * 10).ToString());
                i++;
            }
            return (doc, page);
        }

        [Fact]
        public void test_refine_grid_splits_overmerged_body()
        {
            // refine_grid splits an over-merged body row into one row per record.
            // *** PyMuPDF extension (opt-in grid refinement). ***
            var (doc, page) = _make_overmerged_page();
            try
            {
                var grid = new List<List<(float x0, float y0, float x1, float y1)?>>
                {
                    new() { (100, 100, 200, 120), (200, 100, 300, 120) },  // header row
                    new() { (100, 120, 200, 200), (200, 120, 300, 200) },  // one over-merged body row
                };
                var refined = TableRefine.RefineGrid(page, grid, headerRowCount: 1);
                // header kept, body row split into the three record rows
                Assert.Equal(2, grid.Count);  // input untouched
                Assert.Equal(4, refined.Count);
                Assert.Equal(grid[0], refined[0]);  // header preserved verbatim
                Assert.All(refined, r => Assert.Equal(2, r.Count));
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Fact]
        public void test_find_tables_refine_splits_rows_default_unchanged()
        {
            // is unchanged -- refinement is strictly opt-in.
            // *** PyMuPDF extension. ***
            var (doc, page) = _make_overmerged_page();
            try
            {
                var defaultTf = page.FindTables(useLayout: false);
                var refined = page.FindTables(useLayout: false, refine: true);
                Assert.Single(defaultTf.Tables);
                Assert.Single(refined.Tables);

                // Default detects the merged 2-row grid (unchanged behaviour).
                Assert.Equal(2, defaultTf.Tables[0].RowCount);
                Assert.Equal(2, defaultTf.Tables[0].ColCount);

                var t = refined.Tables[0];
                Assert.Equal(4, t.RowCount);
                Assert.Equal(2, t.ColCount);
                Assert.Equal(
                    new List<List<string?>>
                    {
                        new() { "A", "B" },
                        new() { "1", "10" },
                        new() { "2", "20" },
                        new() { "3", "30" },
                    },
                    t.Extract());
            }
            finally
            {
                doc.Dispose();
            }
        }

        static (Document doc, Page page) _make_merged_header_page()
        {
            // A page whose line grid detects a header cell that spans both body columns.

            // The middle vertical divider is drawn only in the body (below the header
            // separator), so the top row is one wide cell over two columns while each body
            // row has two cells -- a merged header cell FindTables detects on its own.
            // Needs no layout, so it exercises the standalone benefit.
            var doc = new Document();
            var page = doc.NewPage(width: 400, height: 300);
            foreach (float y in new float[] { 100, 120, 140, 160 })
                page.DrawLine(new Point(100, y), new Point(300, y));
            page.DrawLine(new Point(100, 100), new Point(100, 160));  // left border
            page.DrawLine(new Point(300, 100), new Point(300, 160));  // right border
            page.DrawLine(new Point(200, 120), new Point(200, 160));  // middle divider: body only
            page.InsertText(new Point(150, 114), "Merged Header");
            page.InsertText(new Point(120, 134), "a");
            page.InsertText(new Point(220, 134), "b");
            page.InsertText(new Point(120, 154), "c");
            page.InsertText(new Point(220, 154), "d");
            return (doc, page);
        }

        [Fact]
        public void test_resolve_spans_merged_header()
        {
            // resolve_spans surfaces a merged header cell as a colspan-2 SpanCell.
            // *** PyMuPDF extension (opt-in span resolution). ***
            var (doc, page) = _make_merged_header_page();
            try
            {
                var grid = new List<List<(float x0, float y0, float x1, float y1)?>>
                {
                    new() { (100, 100, 300, 120) },  // header spanning both columns
                    new() { (100, 120, 200, 140), (200, 120, 300, 140) },
                    new() { (100, 140, 200, 160), (200, 140, 300, 160) },
                };
                var placements = TableSpans.ResolveSpans(page, grid);
                Assert.Equal(3, placements.Count);
                // header is one placement spanning both columns
                Assert.Single(placements[0]);
                var head = placements[0][0];
                Assert.Equal((2, 1), (head.Colspan, head.Rowspan));
                Assert.Equal((100.0f, 100.0f, 300.0f, 120.0f), head.Bbox);
                Assert.Contains("Merged Header", head.Text);
                // resolve_spans leaves the HTML tag at its default; tagging td/th is the
                Assert.Equal("td", head.Tag);
                // body cells stay 1x1
                Assert.Equal(new[] { (1, 1), (1, 1) }, placements[1].Select(c => (c.Colspan, c.Rowspan)).ToArray());
                Assert.Equal(new[] { "c", "d" }, placements[2].Select(c => c.Text).ToArray());
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Fact]
        public void test_find_tables_refine_exposes_placements_default_none()
        {
            // structure; the default result exposes no placements and is otherwise unchanged.
            // *** PyMuPDF extension. ***
            var (doc, page) = _make_merged_header_page();
            try
            {
                var defaultTf = page.FindTables(useLayout: false);
                var refined = page.FindTables(useLayout: false, refine: true);
                Assert.Single(defaultTf.Tables);
                Assert.Single(refined.Tables);

                // Default detects the merged-header grid but resolves no spans.
                var dt = defaultTf.Tables[0];
                Assert.Equal((3, 2), (dt.RowCount, dt.ColCount));
                Assert.Null(dt.Placements);

                var t = refined.Tables[0];
                Assert.NotNull(t.Placements);
                Assert.Equal(2, t.Placements[0][0].Colspan);
                Assert.Equal(1, t.Placements[0][0].Rowspan);
                Assert.Equal(new[] { 1, 1 }, t.Placements[1].Select(c => c.Colspan).ToArray());
                // placements are tagged: the top header row is th, body rows are td.
                Assert.Equal("th", t.Placements[0][0].Tag);
                Assert.Equal(new[] { "td", "td" }, t.Placements[1].Select(c => c.Tag).ToArray());
                Assert.Equal(new[] { "td", "td" }, t.Placements[2].Select(c => c.Tag).ToArray());
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Fact]
        public void test_find_tables_refine_to_html_merged_header()
        {
            // <th colspan=2>, with body rows as <td>.
            // *** PyMuPDF extension (opt-in header tagging + HTML serialization). ***
            var (doc, page) = _make_merged_header_page();
            try
            {
                var t = page.FindTables(useLayout: false, refine: true).Tables[0];
                string html = t.ToHtml();
                Assert.Equal(
                    "<table>" +
                    "<tr><th colspan=\"2\">Merged Header</th></tr>" +
                    "<tr><td>a</td><td>b</td></tr>" +
                    "<tr><td>c</td><td>d</td></tr>" +
                    "</table>",
                    html);
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Fact]
        public void test_find_tables_refine_header_meta()
        {
            // on the Table; the default path leaves the conservative defaults.
            // *** PyMuPDF extension. ***
            var (doc, page) = _make_merged_header_page();
            try
            {
                var defaultT = page.FindTables(useLayout: false).Tables[0];
                Assert.Equal(0, defaultT.HeaderRows);
                Assert.Empty(defaultT.SectionRows);

                var t = page.FindTables(useLayout: false, refine: true).Tables[0];
                Assert.Equal(1, t.HeaderRows);
                Assert.Empty(t.SectionRows);
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Fact]
        public void test_table_to_html_fallback_flat()
        {
            // Table.to_html on a default (non-refined) table returns a well-formed,
            // td-only flat <table> built from extract -- no placements needed.
            // *** PyMuPDF extension. ***
            var (doc, page) = _make_merged_header_page();
            try
            {
                var t = page.FindTables(useLayout: false).Tables[0];
                Assert.Null(t.Placements);
                string html = t.ToHtml();
                Assert.StartsWith("<table>", html);
                Assert.EndsWith("</table>", html);
                Assert.DoesNotContain("<th", html);  // flat fallback is td-only
                Assert.Equal(t.RowCount, System.Text.RegularExpressions.Regex.Matches(html, "<tr>").Count);
                // every cell is a plain <td>; the merged header text lands in one cell
                Assert.Contains("<td>Merged Header</td>", html);
                Assert.Contains("<td>a</td>", html);
                Assert.Contains("<td>d</td>", html);
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Fact]
        public void test_render_table_html_section_row_collapse()
        {
            // The core serializer collapses a section-label row (a lone centered label)
            // to a single <th colspan=N>, and honours per-cell td/th tags + colspan.
            // *** PyMuPDF extension (HTML serialization). ***
            SpanCell Cell(string text, int colspan = 1, int rowspan = 1, string tag = "td") =>
                new SpanCell(null, text, colspan, rowspan, tag);

            var rows = new List<List<SpanCell>>
            {
                new() { Cell("Group", colspan: 3, tag: "th") },
                new() { Cell(""), Cell("Section", tag: "th"), Cell("") },  // centered section label
                new() { Cell("x"), Cell("1"), Cell("2") },
            };
            string html = TableHeaders.RenderTableHtml(rows, sectionHeaderRows: new[] { 1 });
            Assert.Equal(
                "<table>" +
                "<tr><th colspan=\"3\">Group</th></tr>" +
                "<tr><th colspan=\"3\">Section</th></tr>" +
                "<tr><td>x</td><td>1</td><td>2</td></tr>" +
                "</table>",
                html);
            // escaping (& <>) and <br/> line joins, quotes left literal
            Assert.Equal(
                "<table><tr><td>a &amp; b &lt; c &gt; \"d\"<br/>second</td></tr></table>",
                TableHeaders.RenderTableHtml(new List<List<SpanCell>>
                {
                    new() { Cell("a & b < c > \"d\"\nsecond") },
                }));
        }

        static List<List<SpanCell>> SpanGrid(object[][] rows)
        {
            var grid = new List<List<SpanCell>>();
            foreach (var row in rows)
            {
                var cells = new List<SpanCell>();
                foreach (var cell in row)
                {
                    if (cell is string text)
                        cells.Add(new SpanCell(null, text, 1, 1));
                    else if (cell is ValueTuple<string, int, int> t)
                        cells.Add(new SpanCell(null, t.Item1, t.Item2, t.Item3));
                    else
                        throw new ArgumentException($"unexpected cell {cell}");
                }
                grid.Add(cells);
            }
            return grid;
        }

        [Fact]
        public void test_header_leaf_labels_under_a_spanning_header()
        {
            // Port of test_tables.extend_header_leaf_labels cases.
            var grid = SpanGrid(new object[][]
            {
                new object[] { ("Product", 1, 2), ("Units sold", 2, 1) },
                new object[] { "Online", "In store" },
                new object[] { "Laptop", "120", "45" },
                new object[] { "Phone", "300", "80" },
            });
            Assert.Equal(2, TableHeaders.ExtendHeaderLeafLabels(grid, 1));
            Assert.Equal(2, TableHeaders.ExtendHeaderLeafLabels(grid, 2));
        }

        [Fact]
        public void test_header_leaf_labels_keep_a_numeric_body_row()
        {
            var repeatedShape = SpanGrid(new object[][]
            {
                new object[] { "Region", ("Revenue", 2, 1) },
                new object[] { "East", "10", "12" },
                new object[] { "West", "11", "13" },
            });
            Assert.Equal(1, TableHeaders.ExtendHeaderLeafLabels(repeatedShape, 1));

            var numberUnderLabel = SpanGrid(new object[][]
            {
                new object[] { "Year", ("Sales", 2, 1) },
                new object[] { "2023", "North", "South" },
                new object[] { "2024", "10", "12" },
            });
            Assert.Equal(1, TableHeaders.ExtendHeaderLeafLabels(numberUnderLabel, 1));
        }

        [Fact]
        public void test_header_leaf_labels_need_a_spanning_header_cell()
        {
            var singleCells = SpanGrid(new object[][]
            {
                new object[] { "Product", "Online", "In store" },
                new object[] { "", "units", "units" },
                new object[] { "Laptop", "120", "45" },
            });
            Assert.Equal(1, TableHeaders.ExtendHeaderLeafLabels(singleCells, 1));

            var fullWidthTitle = SpanGrid(new object[][]
            {
                new object[] { ("Units sold", 3, 1) },
                new object[] { "Product", "Online", "In store" },
                new object[] { "Laptop", "120", "45" },
            });
            Assert.Equal(1, TableHeaders.ExtendHeaderLeafLabels(fullWidthTitle, 1));
        }

        [Fact]
        public void test_header_leaf_labels_never_take_the_last_row()
        {
            var singleRecord = SpanGrid(new object[][]
            {
                new object[] { "Product", ("Units sold", 2, 1) },
                new object[] { "Laptop", "120", "45" },
            });
            Assert.Equal(1, TableHeaders.ExtendHeaderLeafLabels(singleRecord, 1));
        }

        [Fact]
        public void test_find_tables_refine_tags_leaf_labels_under_a_spanning_header()
        {
            // The row naming the columns of a header cell that spans some of them joins
            // unchanged.
            string[][] texts =
            {
                new[] { "Product", "Availability", null },
                new[] { "", "Online", "In store" },
                new[] { "Laptop", "Yes", "No" },
                new[] { "Phone", "No", "Yes" },
                new[] { "Tablet", "Yes", "Yes" },
            };
            using var doc = new Document();
            var page = doc.NewPage(width: 400, height: 300);
            float[] xValues = { 60, 160, 240, 320 };
            float y0 = 60, rowHeight = 20;
            float y1 = y0 + texts.Length * rowHeight;
            for (int row = 0; row <= texts.Length; row++)
            {
                float y = y0 + row * rowHeight;
                page.DrawLine(new Point(xValues[0], y), new Point(xValues[xValues.Length - 1], y));
            }
            foreach (float x in new float[] { 60, 160, 320 })
                page.DrawLine(new Point(x, y0), new Point(x, y1));
            page.DrawLine(new Point(240, y0 + rowHeight), new Point(240, y1));
            for (int row = 0; row < texts.Length; row++)
            {
                for (int column = 0; column < texts[row].Length; column++)
                {
                    string value = texts[row][column];
                    if (!string.IsNullOrEmpty(value))
                        page.InsertText(new Point(xValues[column] + 4, y0 + row * rowHeight + 14), value);
                }
            }

            var defaultT = page.FindTables(useLayout: false).Tables[0];
            Assert.Null(defaultT.Placements);
            Assert.Equal(0, defaultT.HeaderRows);
            Assert.Equal(
                texts.Select(r => r.Select(c => c ?? "").ToArray()).ToArray(),
                defaultT.Extract().Select(r => r.Select(c => c ?? "").ToArray()).ToArray());

            var t = page.FindTables(useLayout: false, refine: true).Tables[0];
            Assert.Equal(2, t.HeaderRows);
            Assert.Equal(
                "<table>" +
                "<tr><th>Product</th><th colspan=\"2\">Availability</th></tr>" +
                "<tr><th></th><th>Online</th><th>In store</th></tr>" +
                "<tr><td>Laptop</td><td>Yes</td><td>No</td></tr>" +
                "<tr><td>Phone</td><td>No</td><td>Yes</td></tr>" +
                "<tr><td>Tablet</td><td>Yes</td><td>Yes</td></tr>" +
                "</table>",
                t.ToHtml());
        }

        static void _make_bordered_table(Page page, float x0, float y0, string[][] texts)
        {
            // Draw a bordered 2x2 table (cells 100 wide, 20 tall) at (x0, y0), with the
            // 2x2 texts grid inserted into its cells; returns nothing (mutates page).
            float x1 = x0 + 100, x2 = x0 + 200;
            float y1 = y0 + 20, y2 = y0 + 40;
            foreach (float y in new float[] { y0, y1, y2 })
                page.DrawLine(new Point(x0, y), new Point(x2, y));
            foreach (float x in new float[] { x0, x1, x2 })
                page.DrawLine(new Point(x, y0), new Point(x, y2));
            float[] rys = { y0, y1 };
            float[] cxs = { x0, x1 };
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 2; c++)
                    page.InsertText(new Point(cxs[c] + 5, rys[r] + 14), texts[r][c]);
        }

        [Fact]
        public void test_find_tables_union_fuses_layout_grid_with_line_candidate()
        {
            // the line-based finder's candidates: a layout table with no matching line
            // candidate is kept from its GNN grid, and a disjoint line-detected table is
            // appended -- layout order first, then appended candidates.
            // *** PyMuPDF extension (opt-in layout/candidate union). ***
            using var doc = new Document();
            var page = doc.NewPage(width: 500, height: 500);
            // Table B: a real bordered 2x2 table the line finder detects (disjoint from A).
            _make_bordered_table(page, 80, 300, new[] { new[] { "b00", "b01" }, new[] { "b10", "b11" } });
            // Table A: only a layout (GNN) grid, no drawn lines. Inject the raw layout
            // form union reads: a "table" group whose
            // table_grid carries interior h_lines/v_lines offsets.
            page.LayoutInformation = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["class_name"] = "table",
                    ["group_bbox"] = new List<float> { 80.0f, 80.0f, 280.0f, 120.0f },
                    ["table_grid"] = new Dictionary<string, object>
                    {
                        ["h_lines"] = new List<float> { 20.0f },
                        ["v_lines"] = new List<float> { 100.0f },
                    },
                },
            };
            var tf = page.FindTables(useLayout: true, union: true);
            var tables = tf.Tables;
            Assert.Equal(2, tables.Count);

            // A first (layout order): a 2x2 grid built from group_bbox + interior lines.
            var a = tables[0];
            Assert.Equal((2, 2), (a.RowCount, a.ColCount));
            Assert.Equal((80.0f, 80.0f, 280.0f, 120.0f), a.Bbox);
            var aCells = a.Rows.Select(row => row.Cells.ToList()).ToList();
            Assert.Equal((80.0f, 80.0f, 180.0f, 100.0f), aCells[0][0]);
            Assert.Equal((180.0f, 100.0f, 280.0f, 120.0f), aCells[1][1]);

            // B appended after the layout table: the line-detected grid, extractable.
            var b = tables[1];
            Assert.Equal((2, 2), (b.RowCount, b.ColCount));
            Assert.Equal("b00", b.Extract()[0][0]);
        }

        [Fact]
        public void test_find_tables_union_no_layout_degrades_to_line_candidates()
        {
            // analyzer is unavailable: get_layout is a no-op, layout_information stays
            // matching the plain line-based FindTables result.
            // *** PyMuPDF extension. ***
            using var doc = new Document();
            var page = doc.NewPage(width: 400, height: 400);
            _make_bordered_table(page, 80, 80, new[] { new[] { "a", "b" }, new[] { "c", "d" } });
            var original = Page.GetLayoutProvider;
            Page.GetLayoutProvider = null;  // simulate: layout wheel not installed
            try
            {
                var union = page.FindTables(useLayout: true, union: true);
                Assert.Null(page.LayoutInformation);

                var line = TableHelpers.FindTables(page, strategy: "lines_strict", useLayout: false);
                Assert.Single(union.Tables);
                // Same table as the pure line-based path, just routed through the union.
                Assert.Equal(
                    line.Tables.Select(t => t.Extract()).ToList(),
                    union.Tables.Select(t => t.Extract()).ToList());
                var t = union.Tables[0];
                Assert.Equal((2, 2), (t.RowCount, t.ColCount));
                Assert.Equal("a", t.Extract()[0][0]);
            }
            finally
            {
                Page.GetLayoutProvider = original;
            }
        }

        [Fact]
        public void test_strict_lines_accepts_thin_rects_batched_in_large_fill_path()
        {
            // Line-like rect items survive a large fill-only path's bbox filter.
            using var doc = new Document();
            var page = doc.NewPage(width: 240, height: 180);
            using var shape = page.NewShape();
            float[] xValues = { 40, 120, 200 };
            float[] yValues = { 30, 90, 150 };
            foreach (float x in xValues)
                shape.DrawRect(new Rect(x - 0.4f, yValues[0], x + 0.4f, yValues[yValues.Length - 1]));
            foreach (float y in yValues)
                shape.DrawRect(new Rect(xValues[0], y - 0.4f, xValues[xValues.Length - 1], y + 0.4f));
            shape.Finish(color: null, fill: new float[] { 0, 0, 0 });
            shape.Commit();
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 2; col++)
                {
                    page.InsertText(
                        new Point(xValues[col] + 10, yValues[row] + 25),
                        $"r{row}c{col}");
                }
            }

            var paths = page.GetDrawingsDict();
            Assert.Single(paths);
            Assert.Equal("f", paths[0]["type"]);
            var pathRect = (Rect)paths[0]["rect"];
            Assert.True(pathRect.Width > 3 && pathRect.Height > 3);

            var tables = TableHelpers.FindTables(page, strategy: "lines_strict", useLayout: false).Tables;
            Assert.Single(tables);
            Assert.Equal(2, tables[0].RowCount);
            Assert.Equal(2, tables[0].ColCount);
        }

        [Fact]
        public void test_strict_lines_ignores_a_dot_screen_batched_in_a_large_fill_path()
        {
            // Short thin rects in a large fill path are a dot screen, not grid rules.

            // This page batches 196 rects of 1.1 x 0.9 pt into one fill path: a dotted
            // few hundred of them at one y snap and join into a single long edge, which
            // turns an empty region into a table. Only rects long enough to be a rule are
            // kept (see _is_batched_rule).

            string filename = Doc("test_3594.pdf");
            HasFile(filename);
            using var doc = new Document(filename);
            var page = doc[14];
            var paths = page.GetDrawingsDict()
                .Where(p =>
                    p.TryGetValue("type", out var type) && type as string == "f"
                    && p.TryGetValue("rect", out var rectObj) && rectObj is Rect rect
                    && rect.Width > 3 && rect.Height > 3)
                .ToList();
            Assert.Contains(
                paths,
                p => p.TryGetValue("items", out var items)
                    && items is System.Collections.ICollection coll
                    && coll.Count > 100);
            Assert.Empty(TableHelpers.FindTables(page, strategy: "lines_strict", useLayout: false).Tables);
        }

        [Fact]
        public void test_get_table_drawings_is_shared_only_inside_one_call()
        {
            // The drawings cache lives for one FindTables call; copies stay isolated.
            using var doc = new Document();
            var page = doc.NewPage(width: 200, height: 200);
            page.DrawRect(new Rect(20, 20, 120, 60));
            // Without the cache every consumer extracts afresh, exactly as before.
            Assert.NotSame(
                TableHelpers._get_table_drawings(page),
                TableHelpers._get_table_drawings(page));

            page.TableDrawingsCache = new List<List<Dictionary<string, object>>>(); // what FindTables installs
            var first = TableHelpers._get_table_drawings(page);
            Assert.Same(first, TableHelpers._get_table_drawings(page));
            var fresh = page.GetDrawingsDict();
            Assert.Equal(fresh.Count, first.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.Equal(fresh[i]["type"], first[i]["type"]);
                Assert.Equal((Rect)fresh[i]["rect"], (Rect)first[i]["rect"]);
                Assert.Equal(DrawingItemCount(fresh[i]["items"]), DrawingItemCount(first[i]["items"]));
            }
            var rect = new Rect((Rect)first[0]["rect"]);
            int itemCount = DrawingItemCount(first[0]["items"]);

            var native = TableHelpers._get_table_drawings(page, native: true);
            Assert.NotSame(native, first);
            native[0]["rect"] = (Rect)native[0]["rect"] | new Point(180, 180);
            ((System.Collections.IList)native[0]["items"]).Add(
                new object[] { "l", new Point(0, 0), new Point(1, 1) });
            // The edge builder's in-place edits must not reach the other consumers.
            var cached = TableHelpers._get_table_drawings(page);
            Assert.Equal(rect, new Rect((Rect)cached[0]["rect"]));
            Assert.Equal(itemCount, DrawingItemCount(cached[0]["items"]));
            Assert.NotEqual(rect, (Rect)native[0]["rect"]); // the copy really changed

            page.TableDrawingsCache = null;
            page.FindTables(useLayout: false); // must not leave the cache behind
            Assert.Null(page.TableDrawingsCache);

            static int DrawingItemCount(object items)
            {
                if (items is System.Collections.ICollection coll)
                    return coll.Count;
                throw new InvalidOperationException("drawing items are not a collection");
            }
        }

        [Fact]
        public void test_table_header_is_computed_on_first_access()
        {
            // Table.header is lazy but keeps its value, so to_markdown still works.
            using var doc = new Document(filename);
            var page = doc[0];
            // The second table: nothing above it can be taken for an external
            var tab = page.FindTables().Tables[1];
            // C# assigns Header in the Table constructor; there is no _NO_HEADER_YET sentinel.
            var header = tab.header;
            Assert.Same(header, tab.header); // computed once
            Assert.False(header.External);
            Assert.Equal(tab.Extract()[0], header.Names);
            Assert.StartsWith("|", tab.ToMarkdown());
            tab.Header = null; // assignable, as before
            Assert.Null(tab.header);
        }

        [Fact]
        public void test_refine_repeated_leading_header_cuts_exact_signature()
        {
            // A leading header row that recurs verbatim splits stacked tables.
            static List<List<string>> Grid(string[][] source) =>
                source.Select(row => row.ToList()).ToList();

            var rows = Grid(new[]
            {
                new[] { "First table", "", "" },
                new[] { "Effective Date", "BI", "PD" },
                new[] { "2024-01-01", "1", "2" },
                new[] { "2024-02-01", "3", "4" },
                new[] { "2024-03-01", "5", "6" },
                new[] { "Second table", "", "" },
                new[] { " effective   date ", "bi", "PD" },
                new[] { "2023-01-01", "7", "8" },
                new[] { "2023-02-01", "9", "10" },
                new[] { "2023-03-01", "11", "12" },
            });
            Assert.Equal(new[] { 6 }, TableRefine.RepeatedLeadingHeaderCuts(rows, 2));
            // The header finder is conservative: a title row above the header can leave it
            // reporting one header row, and the signature is still a leading row.
            Assert.Equal(new[] { 6 }, TableRefine.RepeatedLeadingHeaderCuts(rows, 1));
            Assert.Equal(new[] { 6 }, TableRefine.RepeatedLeadingHeaderCuts(rows, 0));
            // A two-column stack splits too: the rule reads the grid's own width.
            var narrow = rows.Select(row => row.Take(2).ToList()).ToList();
            Assert.Equal(new[] { 6 }, TableRefine.RepeatedLeadingHeaderCuts(narrow, 2));
        }

        [Fact]
        public void test_refine_repeated_leading_header_cuts_rejects_body_and_short_segments()
        {
            // Repeated body data and adjacent duplicate headers leave the table intact.
            static List<List<string>> Grid(string[][] source) =>
                source.Select(row => row.ToList()).ToList();

            var repeatedBody = Grid(new[]
            {
                new[] { "Report", "", "" },
                new[] { "Name", "Value", "Note" },
                new[] { "a", "1", "x" },
                new[] { "same", "record", "value" },
                new[] { "b", "2", "y" },
                new[] { "c", "3", "z" },
                new[] { "d", "4", "w" },
                new[] { "same", "record", "value" },
                new[] { "e", "5", "q" },
                new[] { "f", "6", "r" },
            });
            var adjacentDuplicate = Grid(new[]
            {
                new[] { "Name", "Value", "Note" },
                new[] { "a", "1", "x" },
                new[] { "b", "2", "y" },
                new[] { "c", "3", "z" },
                new[] { "Name", "Value", "Note" },
                new[] { "Name", "Value", "Note" },
                new[] { "d", "4", "w" },
                new[] { "e", "5", "q" },
            });
            // A two-row header, as on IRS form 940-B: the narrow sub-header under the
            // eight-column header recurs inside the one table.
            var spanningSubheader = new List<List<string>>
            {
                new() { "State", "Reporting No.", "Payroll", "Rate period", "Rate", "Paid", "Due", "Total" },
                new() { "From-", "To-" },
                new() { "", "", "", "", "", "", "", "" },
                new() { "State agency" },
                new() { "Fax", "Mail to", "Other" },
                new() { "State", "Rate", "Taxable", "Rate", "Paid", "Due", "Total" },
                new() { "From-", "To-" },
                new() { "", "", "", "", "", "", "", "" },
            };
            Assert.Empty(TableRefine.RepeatedLeadingHeaderCuts(repeatedBody, 2));
            // Two spanning cells do not carry an eight-column header's structure.
            Assert.Empty(TableRefine.RepeatedLeadingHeaderCuts(spanningSubheader, 1));
            // A repeated all-numeric leading row is data too.
            Assert.Empty(TableRefine.RepeatedLeadingHeaderCuts(
                Grid(new[]
                {
                    new[] { "1", "2" },
                    new[] { "a", "b" },
                    new[] { "1", "2" },
                    new[] { "c", "d" },
                }),
                1));
            // Two header rows in a row are one header, not a cut.
            Assert.Empty(TableRefine.RepeatedLeadingHeaderCuts(adjacentDuplicate, 1));
            Assert.Empty(TableRefine.RepeatedLeadingHeaderCuts(
                Grid(new[]
                {
                    new[] { "H", "H2" },
                    new[] { "a", "b" },
                    new[] { "H", "H2" },
                }),
                1));
        }

        [Fact]
        public void test_find_tables_refine_splits_a_repeated_leading_header()
        {

            // Nothing about a repeated header is union-specific, so the split applies
            // whenever refine is true. The default path still reports the single grid.

            // *** extension. ***
            // No "-" or "." in the cells: with quad corrections off (layout sets
            // this globally) such small glyphs are extracted as a line of their own.
            string[][] texts =
            {
                new[] { "Date", "BI", "PD" },
                new[] { "2024/01", "1", "2" },
                new[] { "2024/02", "3", "4" },
                new[] { "Date", "BI", "PD" },
                new[] { "2023/01", "7", "8" },
                new[] { "2023/02", "9", "10" },
            };
            using var doc = new Document();
            var page = doc.NewPage(width: 400, height: 400);
            float[] xValues = { 60, 160, 230, 300 };
            float y0 = 60, rowHeight = 22;
            for (int row = 0; row <= texts.Length; row++)
            {
                float y = y0 + row * rowHeight;
                page.DrawLine(new Point(xValues[0], y), new Point(xValues[xValues.Length - 1], y));
            }
            foreach (float x in xValues)
                page.DrawLine(new Point(x, y0), new Point(x, y0 + texts.Length * rowHeight));
            for (int row = 0; row < texts.Length; row++)
            {
                for (int column = 0; column < texts[row].Length; column++)
                {
                    page.InsertText(
                        new Point(xValues[column] + 4, y0 + row * rowHeight + 15),
                        texts[row][column]);
                }
            }

            var defaultTables = page.FindTables(useLayout: false).Tables;
            Assert.Single(defaultTables);
            Assert.Equal(6, defaultTables[0].RowCount);

            var refined = page.FindTables(useLayout: false, refine: true).Tables;
            Assert.True(
                refined.Count == 2,
                string.Concat(refined.Select(t => PprintExtract(t.Extract()))));
            AssertExtractRowsEqual(
                texts.Take(3).Select(r => r.Select(c => (string?)c).ToList()).ToList(),
                refined[0].Extract());
            AssertExtractRowsEqual(
                texts.Skip(3).Select(r => r.Select(c => (string?)c).ToList()).ToList(),
                refined[1].Extract());
            // Each segment reports its own region, not the parent's.
            Assert.True(refined[0].Bbox.y1 <= refined[1].Bbox.y0 + 1);
        }

        static void _make_bordered_grid(Page page, (float x0, float y0, float x1, float y1) bbox, string[][] texts)
        {
            // Draw a uniformly divided grid with the same shape as ``texts``.
            float x0 = bbox.x0, y0 = bbox.y0, x1 = bbox.x1, y1 = bbox.y1;
            int rowCount = texts.Length;
            int colCount = texts[0].Length;
            float rowHeight = (y1 - y0) / rowCount;
            float colWidth = (x1 - x0) / colCount;
            for (int row = 0; row <= rowCount; row++)
            {
                float y = y0 + row * rowHeight;
                page.DrawLine(new Point(x0, y), new Point(x1, y));
            }
            for (int column = 0; column <= colCount; column++)
            {
                float x = x0 + column * colWidth;
                page.DrawLine(new Point(x, y0), new Point(x, y1));
            }
            for (int row = 0; row < rowCount; row++)
            {
                string[] values = texts[row];
                for (int column = 0; column < values.Length; column++)
                {
                    string value = values[column];
                    if (!string.IsNullOrEmpty(value))
                    {
                        page.InsertText(
                            new Point(
                                x0 + column * colWidth + 5,
                                y0 + row * rowHeight + Math.Min(14f, rowHeight - 3f)),
                            value);
                    }
                }
            }
        }

        [Fact]
        public void test_find_tables_union_forwards_virtual_lines_to_candidates()
        {
            // Virtual raster-style rules reach the nested line finder in union mode.
            using var doc = new Document();
            var page = doc.NewPage(width: 400, height: 400);
            float[] ys = { 100, 140 };
            float[] xs = { 80, 180 };
            for (int row = 0; row < ys.Length; row++)
            {
                for (int col = 0; col < xs.Length; col++)
                    page.InsertText(new Point(xs[col] + 8, ys[row] + 24), $"r{row}c{col}");
            }
            page.LayoutInformation = new List<object>();
            var lines = new List<(Point p1, Point p2)>();
            foreach (float y in new float[] { 100, 140, 180 })
                lines.Add((new Point(80, y), new Point(280, y)));
            foreach (float x in new float[] { 80, 180, 280 })
                lines.Add((new Point(x, 100), new Point(x, 180)));
            var tables = page.FindTables(useLayout: true, union: true, addLines: lines).Tables;
            Assert.Single(tables);
            Assert.Equal((2, 2), (tables[0].RowCount, tables[0].ColCount));
            Assert.Equal("r1c1", tables[0].Extract()[1][1]);
        }

        [Fact]
        public void test_find_tables_union_rejects_multiline_single_row_panel()
        {
            using var doc = new Document();
            var page = doc.NewPage(width: 500, height: 500);
            _make_bordered_grid(
                page,
                (80f, 80f, 380f, 240f),
                new[] { new[] { "left\naxis\nlabels", "right\naxis\nlabels" } });
            page.LayoutInformation = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["class_name"] = "picture",
                    ["group_bbox"] = new List<float> { 80f, 80f, 380f, 240f },
                },
            };
            Assert.Empty(page.FindTables(useLayout: true, union: true).Tables);
        }

        [Fact]
        public void test_find_tables_union_inside_picture_needs_table_shaped_text()
        {
            // Inside a picture region a grid is admitted only on table-shaped text.

            // The Layout model called the region a picture, so a line grid found almost
            // entirely inside one is as likely to be a chart's rules as a table. A chart
            // labels its two axes, so its text sits in exactly one row and one column;
            // a table -- even a sparse matrix of scattered marks -- has a header row and a
            // label column that are each at least half full. Outside a picture nothing
            // changes.
            var picture = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["class_name"] = "picture",
                    ["group_bbox"] = new List<float> { 120f, 80f, 440f, 360f },
                },
            };
            // Chart shape: only the first row and the first column carry text.
            string[][] axes =
            {
                new[] { "", "x0", "x1", "x2" },
                new[] { "y0", "", "", "" },
                new[] { "y1", "", "", "" },
                new[] { "y2", "", "", "" },
            };
            string[][] dense =
            {
                new[] { "a0", "b0", "c0" },
                new[] { "a1", "b1", "c1" },
            };
            // Sparse matrix: header row, label column and a few scattered marks.
            string[][] matrix =
            {
                new[] { "use", "z1", "z2", "z3", "z4", "z5" },
                new[] { "r1", "P", "P", "", "", "" },
                new[] { "r2", "P", "P", "", "", "" },
                new[] { "r3", "", "", "", "", "" },
                new[] { "r4", "", "", "", "", "" },
                new[] { "r5", "", "", "", "", "" },
            };

            List<Table> Find(string[][] cellTexts, (float x0, float y0, float x1, float y1) bbox, List<object> layout)
            {
                using var doc = new Document();
                var page = doc.NewPage(width: 500, height: 500);
                _make_bordered_grid(page, bbox, cellTexts);
                page.LayoutInformation = new List<object>(layout);
                return page.FindTables(useLayout: true, union: true).Tables;
            }

            Assert.Empty(Find(axes, (160f, 160f, 400f, 300f), picture));
            Assert.Single(Find(axes, (160f, 160f, 400f, 300f), new List<object>())); // only the picture gates it

            var tables = Find(dense, (160f, 180f, 380f, 240f), picture);
            Assert.Single(tables);
            Assert.Equal((2, 3), (tables[0].RowCount, tables[0].ColCount));
            Assert.Equal("c1", tables[0].Extract()[1][2]);

            tables = Find(matrix, (140f, 140f, 420f, 320f), picture);
            Assert.Single(tables);
            Assert.Equal((6, 6), (tables[0].RowCount, tables[0].ColCount));
            Assert.Equal("P", tables[0].Extract()[1][1]);
        }

        [Fact]
        public void test_find_tables_union_keeps_a_table_holding_a_picture_in_one_cell()
        {
            // A table larger than the picture contains it and keeps the ordinary rule.
            using var doc = new Document();
            var page = doc.NewPage(width: 500, height: 500);
            // A 200x200 grid whose upper-left cell is the picture; the right column has text.
            page.DrawRect(new Rect(100, 100, 300, 300));
            page.DrawLine(new Point(100, 200), new Point(300, 200));
            page.DrawLine(new Point(200, 100), new Point(200, 300));
            page.InsertText(new Point(210, 130), "right top");
            page.InsertText(new Point(210, 230), "right bottom");
            page.InsertText(new Point(110, 230), "left bottom");
            page.LayoutInformation = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["class_name"] = "picture",
                    ["group_bbox"] = new List<float> { 100f, 100f, 200f, 200f },
                },
            };
            var tables = page.FindTables(useLayout: true, union: true).Tables;
            Assert.Single(tables);
            Assert.Equal((2, 2), (tables[0].RowCount, tables[0].ColCount));
            Assert.Equal("right top", tables[0].Extract()[0][1]);
        }

        [Fact]
        public void test_find_tables_union_keeps_one_coherent_table_group_without_content_support()
        {
            using var doc = new Document();
            var page = doc.NewPage(width: 500, height: 500);
            _make_bordered_grid(
                page,
                (80f, 80f, 380f, 240f),
                new[] { new[] { "left\naxis\nlabels", "right\naxis\nlabels" } });
            page.LayoutInformation = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["class_name"] = "table",
                    ["group_bbox"] = new List<float> { 80f, 80f, 380f, 240f },
                    // types.SimpleNamespace(h_lines=..., v_lines=...); the union reader
                    // accepts the same keys on a dictionary.
                    ["table_grid"] = new Dictionary<string, object>
                    {
                        ["h_lines"] = new List<float> { 50f, 100f },
                        ["v_lines"] = new List<float> { 150f },
                    },
                },
            };
            var tables = page.FindTables(useLayout: true, union: true).Tables;
            Assert.Single(tables);
            Assert.Equal((1, 2), (tables[0].RowCount, tables[0].ColCount));
        }

    }
}
