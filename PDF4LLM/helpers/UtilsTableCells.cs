using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MuPDF.NET;
using mupdf;

namespace PDF4LLM.Helpers
{
    /// <summary>Table cell extraction and markdown formatting.</summary>
    public static partial class Utils
    {
        /// <summary>Extract plain or Markdown text from a table cell region.</summary>
        /// <param name="tableBlocks">Text blocks overlapping the table region.</param>
        /// <param name="cell">Cell bounding box as <c>[x0, y0, x1, y1]</c>.</param>
        /// <param name="markdown">When <see langword="true"/>, emit inline Markdown styling and <c>&lt;br&gt;</c> line breaks.</param>
        /// <param name="ocrPage">When <see langword="true"/>, use span text directly instead of per-character clipping.</param>
        public static string ExtractCells(
            List<Block> tableBlocks,
            float[] cell,
            bool markdown = false,
            bool ocrPage = false)
        {
            if (tableBlocks == null || cell == null || cell.Length < 4)
                return "";

            Rect cellRect = CellRect(cell);
            var text = new StringBuilder();
            foreach (Block block in tableBlocks)
            {
                if (block?.Bbox == null || OutsideCell(block.Bbox, cellRect))
                    continue;
                if (block.Lines == null)
                    continue;

                foreach (Line line in block.Lines)
                {
                    if (line?.Bbox == null || OutsideCell(line.Bbox, cellRect))
                        continue;
                    if (text.Length > 0)
                        text.Append(markdown ? "<br>" : "\n");

                    bool horizontal = line.Dir != null
                        && ((Math.Abs(line.Dir.X) < 1e-3 && Math.Abs(line.Dir.Y - 1) < 1e-3)
                            || (Math.Abs(line.Dir.X - 1) < 1e-3 && Math.Abs(line.Dir.Y) < 1e-3));

                    if (line.Spans == null)
                        continue;

                    foreach (Span span in line.Spans)
                    {
                        if (span?.Bbox == null || OutsideCell(span.Bbox, cellRect))
                            continue;

                        string spanText;
                        if (ocrPage)
                        {
                            spanText = span.Text ?? "";
                        }
                        else if (span.Chars != null && span.Chars.Count > 0)
                        {
                            var sb = new StringBuilder();
                            foreach (MuPDF.NET.Char ch in span.Chars)
                            {
                                Rect charBbox = new Rect(ch.Bbox);
                                if (AlmostInBbox(charBbox, cellRect, portion: 0.5f))
                                    sb.Append(ch.C);
                                else if (WHITE_CHARS.Contains(ch.C))
                                    sb.Append(' ');
                            }

                            spanText = sb.ToString();
                        }
                        else
                            spanText = span.Text ?? "";

                        if (string.IsNullOrEmpty(spanText))
                            continue;

                        if (!markdown)
                        {
                            text.Append(spanText);
                            continue;
                        }

                        bool superscript = ((int)span.Flags & Constants.TextFontSuperscript) != 0;
                        bool mono = ((int)span.Flags & Constants.TextFontMonospaced) != 0
                            && !IsOcrText(span);
                        bool bold = ((int)span.Flags & Constants.TextFontBold) != 0
                            || (span.CharFlags & (uint)mupdf.mupdf.FZ_STEXT_BOLD) != 0;
                        bool italic = ((int)span.Flags & Constants.TextFontItalic) != 0;
                        bool strikeout = (span.CharFlags & (uint)mupdf.mupdf.FZ_STEXT_STRIKEOUT) != 0;

                        var prefixParts = new List<string>();
                        var suffixParts = new List<string>();
                        if (superscript) { prefixParts.Add("<sup>"); suffixParts.Add("</sup>"); }
                        if (bold) { prefixParts.Add("**"); suffixParts.Add("**"); }
                        if (italic) { prefixParts.Add("_"); suffixParts.Add("_"); }
                        if (horizontal && strikeout) { prefixParts.Add("~~"); suffixParts.Add("~~"); }
                        if (mono) { prefixParts.Add("`"); suffixParts.Add("`"); }

                        string prefix = string.Concat(prefixParts);
                        string suffix = string.Concat(suffixParts.AsEnumerable().Reverse());

                        if (spanText.Length > 2)
                            spanText = spanText.TrimEnd();

                        string trimmed = spanText.Trim();
                        string current = text.ToString();
                        if (suffix.Length > 0 && current.EndsWith(suffix, StringComparison.Ordinal))
                        {
                            text.Length -= suffix.Length;
                            text.Append(trimmed + suffix);
                        }
                        else if (string.IsNullOrWhiteSpace(trimmed))
                            text.Append(' ');
                        else
                            text.Append(prefix + trimmed + suffix);
                    }
                }
            }

            string result = text.ToString()
                .Replace("$<br>", "$ ")
                .Replace(" $ <br>", "$ ")
                .Replace("$\n", "$ ")
                .Replace(" $ \n", "$ ");
            return result.Trim();
        }

        /// <summary>Format a detected table as a Markdown pipe table.</summary>
        /// <param name="tableBlocks">Text blocks overlapping the table region.</param>
        /// <param name="tableItem">Detected table structure with cell rectangles.</param>
        /// <param name="markdown">When <see langword="true"/>, apply inline Markdown in cell text.</param>
        /// <param name="ocrPage">When <see langword="true"/>, treat page text as OCR output.</param>
        public static string TableToMarkdown(
            List<Block> tableBlocks,
            Table tableItem,
            bool markdown = true,
            bool ocrPage = false)
        {
            if (tableItem == null)
                return "";

            int rowCount = tableItem.row_count;
            int colCount = tableItem.col_count;
            var cells = new string[rowCount][];
            for (int j = 0; j < rowCount; j++)
            {
                cells[j] = new string[colCount];
                for (int i = 0; i < colCount; i++)
                    cells[j][i] = null;
            }

            for (int j = 0; j < rowCount; j++)
            {
                for (int i = 0; i < colCount - 1; i++)
                {
                    if (cells[j][i + 1] == null)
                        cells[j][i + 1] = cells[j][i];
                }
            }

            for (int i = 0; i < colCount; i++)
            {
                for (int j = 0; j < rowCount - 1; j++)
                {
                    if (cells[j + 1][i] == null)
                        cells[j + 1][i] = cells[j][i];
                }
            }

            for (int i = 0; i < tableItem.rows.Count; i++)
            {
                TableRow row = tableItem.rows[i];
                for (int j = 0; j < row.cells.Count; j++)
                {
                    Rect cell = row.cells[j];
                    if (cell != null)
                    {
                        cells[i][j] = ExtractCells(
                            tableBlocks,
                            new[] { cell.X0, cell.Y0, cell.X1, cell.Y1 },
                            markdown: markdown,
                            ocrPage: ocrPage);
                    }
                }
            }

            for (int i = 0; i < cells[0].Length; i++)
            {
                if (cells[0][i] == null)
                    cells[0][i] = i > 0 ? cells[0][i - 1] : "";
            }

            var output = new StringBuilder();
            output.Append('|').Append(string.Join("|", cells[0])).Append("|\n");
            output.Append('|').Append(string.Join("|", Enumerable.Repeat("---", colCount))).Append("|\n");

            for (int j = 1; j < rowCount; j++)
            {
                output.Append('|');
                for (int i = 0; i < colCount; i++)
                    output.Append(cells[j][i] ?? "").Append('|');
                output.Append('\n');
            }

            return output.Append('\n').ToString();
        }

        /// <summary>Extract plain text for each cell in a detected table.</summary>
        /// <param name="tableBlocks">Text blocks overlapping the table region.</param>
        /// <param name="tableItem">Detected table structure with cell rectangles.</param>
        /// <param name="ocrPage">When <see langword="true"/>, treat page text as OCR output.</param>
        public static List<List<string>> TableExtract(
            List<Block> tableBlocks,
            Table tableItem,
            bool ocrPage = false)
        {
            if (tableItem == null)
                return new List<List<string>>();

            int rowCount = tableItem.row_count;
            int colCount = tableItem.col_count;
            var cells = new List<List<string>>();
            for (int j = 0; j < rowCount; j++)
            {
                var row = new List<string>();
                for (int i = 0; i < colCount; i++)
                    row.Add(null);
                cells.Add(row);
            }

            for (int i = 0; i < tableItem.rows.Count; i++)
            {
                TableRow row = tableItem.rows[i];
                for (int j = 0; j < row.cells.Count; j++)
                {
                    Rect cell = row.cells[j];
                    if (cell != null)
                    {
                        cells[i][j] = ExtractCells(
                            tableBlocks,
                            new[] { cell.X0, cell.Y0, cell.X1, cell.Y1 },
                            markdown: false,
                            ocrPage: ocrPage);
                    }
                }
            }

            return cells;
        }

        /// <summary>Extract plain text for each cell in a layout table box.</summary>
        /// <param name="tableBlocks">Text blocks overlapping the table region.</param>
        /// <param name="layoutBox">Layout box whose <see cref="LayoutBox.Table"/> property holds the table data.</param>
        /// <param name="ocrPage">When <see langword="true"/>, treat page text as OCR output.</param>
        public static List<List<string>> TableExtract(
            List<Block> tableBlocks,
            LayoutBox layoutBox,
            bool ocrPage = false)
        {
            if (layoutBox?.Table == null)
                return new List<List<string>>();
            return TableExtract(tableBlocks, layoutBox.Table, ocrPage);
        }

        /// <summary>Extract plain text for each cell from a table dictionary.</summary>
        /// <param name="tableBlocks">Text blocks overlapping the table region.</param>
        /// <param name="table">Table dictionary with row/column counts and cell bounding boxes.</param>
        /// <param name="ocrPage">When <see langword="true"/>, treat page text as OCR output.</param>
        public static List<List<string>> TableExtract(
            List<Block> tableBlocks,
            Dictionary<string, object> table,
            bool ocrPage = false)
        {
            if (table == null)
                return new List<List<string>>();

            int rowCount = Convert.ToInt32(table["row_count"]);
            int colCount = Convert.ToInt32(table["col_count"]);
            var cellBoxes = table["cells"] as List<List<float[]>>;
            if (cellBoxes == null)
                return new List<List<string>>();

            var cells = new List<List<string>>();
            for (int j = 0; j < rowCount; j++)
            {
                var row = new List<string>();
                for (int i = 0; i < colCount; i++)
                    row.Add(null);
                cells.Add(row);
            }

            for (int i = 0; i < cellBoxes.Count && i < rowCount; i++)
            {
                List<float[]> row = cellBoxes[i];
                for (int j = 0; j < row.Count && j < colCount; j++)
                {
                    float[] cell = row[j];
                    if (cell != null && cell.Length >= 4)
                    {
                        cells[i][j] = ExtractCells(
                            tableBlocks, cell, markdown: false, ocrPage: ocrPage);
                    }
                }
            }

            return cells;
        }

        /// <summary>Format a layout table box as a Markdown pipe table.</summary>
        /// <param name="tableBlocks">Text blocks overlapping the table region.</param>
        /// <param name="layoutBox">Layout box whose <see cref="LayoutBox.Table"/> property holds the table data.</param>
        /// <param name="markdown">When <see langword="true"/>, apply inline Markdown in cell text.</param>
        /// <param name="ocrPage">When <see langword="true"/>, treat page text as OCR output.</param>
        public static string TableToMarkdown(
            List<Block> tableBlocks,
            LayoutBox layoutBox,
            bool markdown = true,
            bool ocrPage = false)
        {
            if (layoutBox?.Table == null)
                return "";
            return TableToMarkdown(tableBlocks, layoutBox.Table, markdown, ocrPage);
        }

        /// <summary>Format a table dictionary as a Markdown pipe table.</summary>
        /// <param name="tableBlocks">Text blocks overlapping the table region.</param>
        /// <param name="table">Table dictionary with row/column counts and cell bounding boxes.</param>
        /// <param name="markdown">When <see langword="true"/>, apply inline Markdown in cell text.</param>
        /// <param name="ocrPage">When <see langword="true"/>, treat page text as OCR output.</param>
        public static string TableToMarkdown(
            List<Block> tableBlocks,
            Dictionary<string, object> table,
            bool markdown = true,
            bool ocrPage = false)
        {
            if (table == null)
                return "";

            int rowCount = Convert.ToInt32(table["row_count"]);
            int colCount = Convert.ToInt32(table["col_count"]);
            var cellBoxes = table["cells"] as List<List<float[]>>;
            if (cellBoxes == null)
                return "";

            var cells = new string[rowCount][];
            for (int j = 0; j < rowCount; j++)
            {
                cells[j] = new string[colCount];
                for (int i = 0; i < colCount; i++)
                    cells[j][i] = null;
            }

            for (int j = 0; j < rowCount; j++)
            {
                for (int i = 0; i < colCount - 1; i++)
                {
                    if (cells[j][i + 1] == null)
                        cells[j][i + 1] = cells[j][i];
                }
            }

            for (int i = 0; i < colCount; i++)
            {
                for (int j = 0; j < rowCount - 1; j++)
                {
                    if (cells[j + 1][i] == null)
                        cells[j + 1][i] = cells[j][i];
                }
            }

            for (int i = 0; i < cellBoxes.Count && i < rowCount; i++)
            {
                List<float[]> row = cellBoxes[i];
                for (int j = 0; j < row.Count && j < colCount; j++)
                {
                    float[] cell = row[j];
                    if (cell != null && cell.Length >= 4)
                    {
                        cells[i][j] = ExtractCells(
                            tableBlocks, cell, markdown: markdown, ocrPage: ocrPage);
                    }
                }
            }

            for (int i = 0; i < cells[0].Length; i++)
            {
                if (cells[0][i] == null)
                    cells[0][i] = i > 0 ? cells[0][i - 1] : "";
            }

            var output = new StringBuilder();
            output.Append('|').Append(string.Join("|", cells[0])).Append("|\n");
            output.Append('|').Append(string.Join("|", Enumerable.Repeat("---", colCount))).Append("|\n");

            for (int j = 1; j < rowCount; j++)
            {
                output.Append('|');
                for (int i = 0; i < colCount; i++)
                    output.Append(cells[j][i] ?? "").Append('|');
                output.Append('\n');
            }

            return output.Append('\n').ToString();
        }

        /// <summary>Format pre-filled cell text rows as a Markdown pipe table.</summary>
        public static string TableToMarkdown(List<List<string>> cells)
        {
            if (cells == null || cells.Count == 0)
                return "";

            var output = new StringBuilder();
            output.Append('|').Append(string.Join("|", cells[0] ?? new List<string>())).Append("|\n");
            output.Append('|').Append(string.Join("|", Enumerable.Repeat("---", cells[0].Count))).Append("|\n");

            for (int j = 1; j < cells.Count; j++)
            {
                output.Append('|');
                List<string> row = cells[j] ?? new List<string>();
                for (int i = 0; i < row.Count; i++)
                    output.Append(row[i] ?? "").Append('|');
                output.Append('\n');
            }

            return output.Append('\n').ToString();
        }

        private static Rect CellRect(float[] cell) =>
            new Rect(cell[0], cell[1], cell[2], cell[3]);

        private static bool OutsideCell(Rect bbox, Rect cell) =>
            Utils.OutsideBbox(bbox, cell);
    }
}