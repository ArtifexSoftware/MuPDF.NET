using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;

namespace MuPDF.NET.PDF4LLM.Helpers.TableHtml
{
    /// <summary>
    /// HTML table reconstruction for MuPDF.NET.PDF4LLM (pymupdf4llm <c>helpers.table_html</c>).
    /// </summary>
    public static class TableHtml
    {
        /// <summary>
        /// FindTables entry used by HTML reconstruction. Tests replace this to observe
        /// <c>useLayout</c>/<c>union</c>/<c>refine</c> (Python monkeypatches
        /// <c>Page.find_tables</c>).
        /// </summary>
        internal static Func<Page, bool, bool, bool, TableFinder> FindTablesHook { get; set; } =
            (page, useLayout, union, refine) =>
                page.FindTables(useLayout: useLayout, union: union, refine: refine);

        static TableFinder FindTablesForHtml(Page page) =>
            FindTablesHook(page, true, true, true);

        /// <summary>
        /// Concatenate per-table HTML fragments into one document string.
        /// </summary>
        public static string HtmlDocument(IEnumerable<Dictionary<string, object>> tables)
        {
            if (tables == null)
                return "";
            return string.Join(
                "\n\n",
                tables
                    .Select(t =>
                        t != null && t.TryGetValue("html", out object h) && h != null
                            ? h.ToString()
                            : null)
                    .Where(s => !string.IsNullOrEmpty(s)));
        }

        /// <summary>
        /// Derive <c>(rowCount, colCount, cells, extract)</c> from a placement grid.
        /// </summary>
        public static (int rowCount, int colCount, List<List<object>> cells, List<List<object>> extract)
            PlacementGridMatrices(List<List<SpanCell>> placements)
        {
            if (placements == null)
                return (0, 0, new List<List<object>>(), new List<List<object>>());

            int rowCount = placements.Count;
            var occupied = new HashSet<(int, int)>();
            int colCount = 0;
            for (int rowIdx = 0; rowIdx < placements.Count; rowIdx++)
            {
                List<SpanCell> row = placements[rowIdx] ?? new List<SpanCell>();
                int colIdx = 0;
                foreach (SpanCell cell in row)
                {
                    if (cell == null)
                        continue;
                    while (occupied.Contains((rowIdx, colIdx)))
                        colIdx++;
                    for (int dr = 0; dr < cell.Rowspan; dr++)
                    {
                        for (int dc = 0; dc < cell.Colspan; dc++)
                            occupied.Add((rowIdx + dr, colIdx + dc));
                    }
                    colIdx += cell.Colspan;
                    colCount = Math.Max(colCount, colIdx);
                }
            }

            var bboxGrid = new List<List<object>>();
            var textGrid = new List<List<object>>();
            for (int r = 0; r < rowCount; r++)
            {
                bboxGrid.Add(Enumerable.Repeat<object>(null, colCount).ToList());
                textGrid.Add(Enumerable.Repeat<object>(null, colCount).ToList());
            }

            var covered = new HashSet<(int, int)>();
            for (int rowIdx = 0; rowIdx < placements.Count; rowIdx++)
            {
                List<SpanCell> row = placements[rowIdx] ?? new List<SpanCell>();
                int colIdx = 0;
                foreach (SpanCell cell in row)
                {
                    if (cell == null)
                        continue;
                    while (covered.Contains((rowIdx, colIdx)))
                        colIdx++;
                    if (colIdx >= colCount)
                        break;
                    if (cell.Bbox.HasValue)
                    {
                        var b = cell.Bbox.Value;
                        bboxGrid[rowIdx][colIdx] = new List<float> { b.x0, b.y0, b.x1, b.y1 };
                    }
                    else
                        bboxGrid[rowIdx][colIdx] = null;
                    textGrid[rowIdx][colIdx] = cell.Text;
                    for (int dr = 0; dr < cell.Rowspan; dr++)
                    {
                        for (int dc = 0; dc < cell.Colspan; dc++)
                        {
                            if (dr != 0 || dc != 0)
                                covered.Add((rowIdx + dr, colIdx + dc));
                        }
                    }
                    colIdx += cell.Colspan;
                }
            }

            return (rowCount, colCount, bboxGrid, textGrid);
        }

        /// <summary>
        /// Reconstruct tables on one PDF page as concatenated HTML.
        /// </summary>
        public static string ToHtml(object pdf, int pageIndex = 0)
        {
            bool ownsDoc = !(pdf is Document);
            Document doc = ownsDoc ? new Document(pdf.ToString()) : (Document)pdf;
            try
            {
                Page page = doc[pageIndex];
                page.RemoveRotation();
                TableFinder tf = FindTablesForHtml(page);
                var tables = (tf?.Tables ?? new List<Table>())
                    .Select(tab => new Dictionary<string, object> { ["html"] = tab.ToHtml() })
                    .ToList();
                return HtmlDocument(tables);
            }
            finally
            {
                if (ownsDoc)
                    doc.Close();
            }
        }

        /// <summary>
        /// Per-page HTML table payload: <c>(bbox, html, rows, cols, cells, extract)</c>.
        /// </summary>
        public static List<(Rect bbox, string html, int rows, int cols, List<List<object>> cells, List<List<object>> extract)>
            PageHtmlTables(Page page)
        {
            var result =
                new List<(Rect bbox, string html, int rows, int cols, List<List<object>> cells, List<List<object>> extract)>();
            if (page == null)
                return result;

            TableFinder tf = FindTablesForHtml(page);
            foreach (Table tab in tf?.Tables ?? new List<Table>())
            {
                if (tab == null)
                    continue;
                (int rowCount, int colCount, List<List<object>> cells, List<List<object>> extract) =
                    PlacementGridMatrices(tab.Placements);
                result.Add(
                    (
                        tab.bbox,
                        tab.ToHtml(),
                        rowCount,
                        colCount,
                        cells,
                        extract
                    ));
            }

            return result;
        }
    }
}
