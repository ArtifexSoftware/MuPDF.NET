using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PDF4LLM.Helpers
{
    /// <summary>
    /// Plain-text table formatting aligned with the <c>tabulate</c>-style layouts used for document text export.
    /// </summary>
    internal static class LayoutTabulate
    {
        /// <summary>Known <c>tabulate</c> table format names (0.9.x set).</summary>
        internal static readonly HashSet<string> TabulateFormatNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "asciidoc", "double_grid", "double_outline", "fancy_grid", "fancy_outline", "github", "grid",
            "heavy_grid", "heavy_outline", "html", "jira", "latex", "latex_booktabs", "latex_longtable",
            "latex_raw", "mediawiki", "mixed_grid", "mixed_outline", "moinmoin", "orgtbl", "outline", "pipe",
            "plain", "presto", "pretty", "psql", "rounded_grid", "rounded_outline", "rst", "simple",
            "simple_grid", "simple_outline", "textile", "tsv", "unsafehtml", "youtrack"
        };

        /// <summary>Invalid <paramref name="tableFormat"/> yields a warning and <c>grid</c>.</summary>
        internal static string NormalizeTableFormat(string tableFormat)
        {
            if (string.IsNullOrWhiteSpace(tableFormat))
                return "grid";
            string f = tableFormat.Trim();
            if (!TabulateFormatNames.Contains(f))
            {
                Console.WriteLine($"Warning: invalid table format '{tableFormat}', using 'grid'.");
                return "grid";
            }

            return f;
        }

        /// <summary>Pre-wrap table cells to column width budgets.</summary>
        internal static List<List<string>> WrapTableForTabulate(
            List<List<string>> table,
            int maxWidth = 100,
            int minColWidth = 10)
        {
            if (table == null || table.Count == 0)
                return table ?? new List<List<string>>();

            int numCols = table.Max(r => r?.Count ?? 0);
            if (numCols == 0)
                return table;

            int baseWidth = Math.Max(minColWidth, maxWidth / numCols);
            var colWidths = Enumerable.Repeat(baseWidth, numCols).ToList();
            var wrappedTable = new List<List<string>>();
            foreach (List<string> row in table)
            {
                var newRow = new List<string>();
                for (int colIdx = 0; colIdx < numCols; colIdx++)
                {
                    string cell = colIdx < (row?.Count ?? 0) ? (row[colIdx] ?? "") : "";
                    int width = colWidths[colIdx];
                    IReadOnlyList<string> lines = WrapCellToWidth(cell, width);
                    newRow.Add(lines.Count > 0 ? string.Join("\n", lines) : "");
                }

                wrappedTable.Add(newRow);
            }

            return wrappedTable;
        }

        /// <summary>Word-wrap a single cell to a maximum width.</summary>
        private static List<string> WrapCellToWidth(string cell, int width)
        {
            if (cell == null)
                cell = "";
            if (width < 1)
                width = 1;
            if (cell.Length <= width)
                return new List<string> { cell };

            var lines = new List<string>();
            var current = new StringBuilder();
            var words = cell.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in words)
            {
                if (word.Length > width)
                {
                    if (current.Length > 0)
                    {
                        lines.Add(current.ToString().TrimEnd());
                        current.Clear();
                    }

                    for (int i = 0; i < word.Length; i += width)
                        lines.Add(word.Substring(i, Math.Min(width, word.Length - i)));
                    continue;
                }

                string trial = current.Length == 0 ? word : current + " " + word;
                if (trial.Length <= width)
                    current.Clear().Append(trial);
                else
                {
                    if (current.Length > 0)
                        lines.Add(current.ToString().TrimEnd());
                    current.Clear().Append(word);
                }
            }

            if (current.Length > 0)
                lines.Add(current.ToString().TrimEnd());
            if (lines.Count == 0)
                lines.Add("");
            return lines;
        }

        /// <summary>
        /// Format a rectangular table; <paramref name="uniformMaxColWidth"/> applies a uniform
        /// <c>maxcolwidths</c>-style cap for fallback tables.
        /// </summary>
        internal static string Tabulate(
            List<List<string>> table,
            string tablefmt,
            int? uniformMaxColWidth = null)
        {
            if (table == null || table.Count == 0)
                return "";

            string fmt = (tablefmt ?? "grid").ToLowerInvariant();
            if (uniformMaxColWidth.HasValue && uniformMaxColWidth.Value > 0)
            {
                int mw = uniformMaxColWidth.Value;
                table = table.Select(row =>
                {
                    var r = new List<string>();
                    for (int i = 0; i < row.Count; i++)
                    {
                        string c = row[i] ?? "";
                        r.Add(string.Join("\n", WrapCellToWidth(c, mw)));
                    }

                    return r;
                }).ToList();
            }

            switch (fmt)
            {
                case "plain":
                    return FormatPlain(table);
                case "simple":
                    return FormatSimple(table);
                case "tsv":
                    return FormatTsv(table);
                case "pipe":
                case "github":
                    return FormatPipe(table);
                default:
                    return FormatGrid(table);
            }
        }

        private static int NumCols(List<List<string>> table) =>
            table.Max(r => r?.Count ?? 0);

        private static List<List<string>> PadRows(List<List<string>> table, int cols)
        {
            var rows = new List<List<string>>();
            foreach (List<string> row in table)
            {
                var r = new List<string>();
                for (int i = 0; i < cols; i++)
                    r.Add(i < (row?.Count ?? 0) ? (row[i] ?? "") : "");
                rows.Add(r);
            }

            return rows;
        }

        private static List<int> ColumnWidths(List<List<string>> rows, int cols)
        {
            var widths = Enumerable.Repeat(0, cols).ToList();
            foreach (List<string> row in rows)
            {
                for (int c = 0; c < cols; c++)
                {
                    string cell = c < row.Count ? row[c] : "";
                    foreach (string line in (cell ?? "").Split('\n'))
                    {
                        int len = new StringInfo(line ?? "").LengthInTextElements;
                        if (len > widths[c])
                            widths[c] = len;
                    }
                }
            }

            for (int c = 0; c < cols; c++)
            {
                if (widths[c] == 0)
                    widths[c] = 0;
            }

            return widths;
        }

        private static string FormatGrid(List<List<string>> table)
        {
            int cols = NumCols(table);
            if (cols == 0)
                return "";
            List<List<string>> rows = PadRows(table, cols);
            List<int> widths = ColumnWidths(rows, cols);

            var sb = new StringBuilder();
            sb.Append(HorizontalRule(widths)).Append('\n');
            for (int ri = 0; ri < rows.Count; ri++)
            {
                sb.Append(DataRowLine(rows[ri], widths));
                sb.Append('\n');
                sb.Append(HorizontalRule(widths));
                sb.Append('\n');
            }

            return sb.ToString().TrimEnd();
        }

        private static string HorizontalRule(List<int> widths)
        {
            var parts = new StringBuilder("+");
            foreach (int w in widths)
                parts.Append(new string('-', w + 2)).Append('+');
            return parts.ToString();
        }

        private static string DataRowLine(List<string> cells, List<int> widths)
        {
            var lineCells = new List<string>[cells.Count];
            int maxLines = 1;
            for (int c = 0; c < cells.Count; c++)
            {
                string[] parts = (cells[c] ?? "").Split('\n');
                if (parts.Length == 0)
                    parts = new[] { "" };
                lineCells[c] = parts.ToList();
                if (lineCells[c].Count > maxLines)
                    maxLines = lineCells[c].Count;
            }

            var sb = new StringBuilder();
            for (int ln = 0; ln < maxLines; ln++)
            {
                sb.Append('|');
                for (int c = 0; c < cells.Count; c++)
                {
                    string piece = ln < lineCells[c].Count ? lineCells[c][ln] : "";
                    sb.Append(' ').Append(piece.PadRight(widths[c])).Append(' ').Append('|');
                }

                if (ln < maxLines - 1)
                    sb.Append('\n');
            }

            return sb.ToString();
        }

        private static string FormatPlain(List<List<string>> table)
        {
            int cols = NumCols(table);
            if (cols == 0)
                return "";
            List<List<string>> rows = PadRows(table, cols);
            List<int> widths = ColumnWidths(rows, cols);
            var sb = new StringBuilder();
            foreach (List<string> row in rows)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (c > 0)
                        sb.Append(' ');
                    string cell = c < row.Count ? row[c] ?? "" : "";
                    string firstLine = cell.Split('\n')[0];
                    sb.Append(firstLine.PadRight(widths[c]));
                }

                sb.Append('\n');
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatSimple(List<List<string>> table)
        {
            int cols = NumCols(table);
            if (cols == 0)
                return "";
            List<List<string>> rows = PadRows(table, cols);
            List<int> widths = ColumnWidths(rows, cols);
            var sb = new StringBuilder();
            for (int ri = 0; ri < rows.Count; ri++)
            {
                List<string> row = rows[ri];
                for (int c = 0; c < cols; c++)
                {
                    if (c > 0)
                        sb.Append("  ");
                    string cell = row[c] ?? "";
                    string firstLine = cell.Split('\n')[0];
                    sb.Append(firstLine.PadRight(widths[c]));
                }

                sb.Append('\n');
                if (ri == 0 && rows.Count > 1)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (c > 0)
                            sb.Append("  ");
                        sb.Append(new string('-', widths[c]));
                    }

                    sb.Append('\n');
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatPipe(List<List<string>> table)
        {
            int cols = NumCols(table);
            if (cols == 0)
                return "";
            List<List<string>> rows = PadRows(table, cols);
            List<int> widths = ColumnWidths(rows, cols);
            var sb = new StringBuilder();
            foreach (List<string> row in rows)
            {
                sb.Append('|');
                for (int c = 0; c < cols; c++)
                {
                    string cell = row[c] ?? "";
                    string firstLine = cell.Split('\n')[0];
                    sb.Append(' ').Append(firstLine.PadRight(widths[c])).Append(' ').Append('|');
                }

                sb.Append('\n');
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatTsv(List<List<string>> table)
        {
            var sb = new StringBuilder();
            foreach (List<string> row in table)
            {
                for (int c = 0; c < row.Count; c++)
                {
                    if (c > 0)
                        sb.Append('\t');
                    sb.Append((row[c] ?? "").Replace("\t", " ").Replace("\n", " "));
                }

                sb.Append('\n');
            }

            return sb.ToString().TrimEnd();
        }
    }
}
