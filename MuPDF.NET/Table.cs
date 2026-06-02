// Copyright (C) 2023 Artifex Software, Inc.
//
// Portions of this code have been ported from pdfplumber / PyMuPDF table.py.
// pdfplumber is under the MIT License, Copyright (c) 2015 Jeremy Singer-Vine.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MuPDF.NET
{
    // ────────────────────────────────────────────────────────────────────
    // Constants
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Default constants used by the table-detection algorithms.
    /// </summary>
    internal static class TableConstants
    {
        public const float DefaultSnapTolerance = 3;
        public const float DefaultJoinTolerance = 3;
        public const float DefaultMinWordsVertical = 3;
        public const float DefaultMinWordsHorizontal = 1;
        public const float DefaultXTolerance = 3;
        public const float DefaultYTolerance = 3;
        public const float DefaultXDensity = 7.25f;
        public const float DefaultYDensity = 13;

        public static readonly string[] TableStrategies =
            { "lines", "lines_strict", "text", "explicit" };

        public static readonly Dictionary<string, string> Ligatures = new Dictionary<string, string>
        {
            { "\uFB00", "ff" },
            { "\uFB03", "ffi" },
            { "\uFB04", "ffl" },
            { "\uFB01", "fi" },
            { "\uFB02", "fl" },
            { "\uFB06", "st" },
            { "\uFB05", "st" },
        };

        public static readonly string[] NonNegativeSettings =
        {
            "snap_tolerance", "snap_x_tolerance", "snap_y_tolerance",
            "join_tolerance", "join_x_tolerance", "join_y_tolerance",
            "edge_min_length", "min_words_vertical", "min_words_horizontal",
            "intersection_tolerance", "intersection_x_tolerance", "intersection_y_tolerance",
        };

        public static readonly HashSet<char> WhiteSpaces = new HashSet<char>
        {
            ' ', '\t', '\n', '\r', '\f', '\v'
        };

        /// <summary>PyMuPDF <c>table.TABLE_DETECTOR_FLAGS</c>.</summary>
        public static readonly int TableDetectorFlags =
            mupdf.mupdf.FZ_STEXT_ACCURATE_BBOXES
            | mupdf.mupdf.FZ_STEXT_SEGMENT
            | mupdf.mupdf.FZ_STEXT_COLLECT_VECTORS
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP;
    }

    /// <summary>PyMuPDF <c>table.py</c> module globals (<c>EDGES</c>, <c>CHARS</c>, <c>TEXTPAGE</c>).</summary>
    internal static class TableModule
    {
        // EDGES = []  # vector graphics from PyMuPDF
        internal static List<Dictionary<string, object>> EDGES = new List<Dictionary<string, object>>();
        // CHARS = []  # text characters from PyMuPDF
        internal static List<Dictionary<string, object>> CHARS = new List<Dictionary<string, object>>();
        // TEXTPAGE = None  # textpage for cell text extraction
        internal static TextPage TEXTPAGE = null;
    }

    // ────────────────────────────────────────────────────────────────────
    // CellGroup / TableRow
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A group of cells forming a contiguous region with a bounding box.
    /// </summary>
    public class CellGroup
    {
        /// <summary>
        /// Individual cell bounding boxes.  A null entry means the cell is
        /// spanned by a neighbour (row-/column-span).
        /// </summary>
        public List<(float x0, float y0, float x1, float y1)?> Cells { get; set; }

        /// <summary>Legacy MuPDF.NET: cell boxes as <see cref="Rect"/> (null = span).</summary>
        public List<Rect> cells =>
            Cells?.Select(c => c.HasValue
                ? new Rect(c.Value.x0, c.Value.y0, c.Value.x1, c.Value.y1)
                : null).ToList() ?? new List<Rect>();

        /// <summary>
        /// Bounding box enclosing all non-null cells.
        /// </summary>
        public (float x0, float y0, float x1, float y1) Bbox { get; set; }

        public CellGroup(List<(float x0, float y0, float x1, float y1)?> cells)
        {
            Cells = cells;
            var valid = cells.Where(c => c.HasValue).Select(c => c!.Value).ToList();
            if (valid.Count == 0)
            {
                Bbox = (0, 0, 0, 0);
            }
            else
            {
                Bbox = (
                    valid.Min(c => c.x0),
                    valid.Min(c => c.y0),
                    valid.Max(c => c.x1),
                    valid.Max(c => c.y1)
                );
            }
        }
    }

    /// <summary>
    /// One row of a <see cref="Table"/>.
    /// </summary>
    public class TableRow : CellGroup
    {
        public TableRow(List<(float x0, float y0, float x1, float y1)?> cells)
            : base(cells) { }
    }

    // ────────────────────────────────────────────────────────────────────
    // TableHeader
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detected table header (PyMuPDF <c>TableHeader</c> extension in <c>table.py</c>).
    /// </summary>
    public class TableHeader
    {
        /// <summary>Header bounding box (Python <c>bbox</c>).</summary>
        public (float x0, float y0, float x1, float y1) Bbox { get; }

        /// <summary>Per-column header cell boxes (null = spanned).</summary>
        public List<(float x0, float y0, float x1, float y1)?> Cells { get; }

        /// <summary>Column header text (Python <c>names</c>).</summary>
        public List<string?> Names { get; }

        /// <summary>
        /// True when header is outside table cells (Python <c>above</c> / external);
        /// false when the first table row is the header.
        /// </summary>
        public bool External { get; }

        public TableHeader(
            (float x0, float y0, float x1, float y1) bbox,
            List<(float x0, float y0, float x1, float y1)?> cells,
            List<string?> names,
            bool external)
        {
            Bbox = bbox;
            Cells = cells;
            Names = names;
            External = external;
        }

        /// <summary>Legacy MuPDF.NET aliases.</summary>
        public bool external => External;
        public List<string?> names => Names;
        public Rect bbox => new Rect(Bbox.x0, Bbox.y0, Bbox.x1, Bbox.y1);
        public List<Rect> cells =>
            Cells?.Select(c => c.HasValue
                ? new Rect(c.Value.x0, c.Value.y0, c.Value.x1, c.Value.y1)
                : null).ToList() ?? new List<Rect>();
    }

    // ────────────────────────────────────────────────────────────────────
    // Table
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single table on a PDF page (PyMuPDF <c>table.Table</c> / pdfplumber port in <c>src/table.py</c>).
    /// </summary>
    /// <remarks>
    /// PyMuPDF adds <see cref="Header"/> via <c>_get_header</c>. Public members use PascalCase;
    /// <c>internal</c> snake_case aliases match Python for same-assembly tests.
    /// </remarks>
    public class Table
    {
        internal Page Page;
        internal TextPage TextPage;

        /// <summary>PyMuPDF <c>CHARS</c> — page characters from <c>make_chars</c>.</summary>
        internal List<Dictionary<string, object>> Chars;

        /// <summary>
        /// All cell bounding boxes that compose this table.
        /// Tuple is <c>(x0, top, x1, bottom)</c> — same as PyMuPDF/pdfplumber (<c>y0</c> = top, <c>y1</c> = bottom).
        /// </summary>
        public List<(float x0, float y0, float x1, float y1)> Cells { get; set; }

        // Legacy aliases from MuPDF.NET public API.
        public List<Rect> cells =>
            Cells?.Select(c => new Rect(c.x0, c.y0, c.x1, c.y1)).ToList();
        public Page page => Page;
        public TextPage textpage => TextPage;

        /// <summary>Detected header for this table.</summary>
        public TableHeader Header { get; internal set; }

        /// <summary>Legacy MuPDF.NET alias for <see cref="Header"/>.</summary>
        public TableHeader header => Header;

        /// <summary>Bounding box of the entire table.</summary>
        public (float x0, float y0, float x1, float y1) Bbox
        {
            get
            {
                return (
                    Cells.Min(c => c.x0),
                    Cells.Min(c => c.y0),
                    Cells.Max(c => c.x1),
                    Cells.Max(c => c.y1)
                );
            }
        }

        /// <summary>Legacy alias: table bbox as <see cref="Rect"/> (MuPDF.NET <c>bbox</c>).</summary>
        public Rect bbox
        {
            get
            {
                var b = Bbox;
                return new Rect(b.x0, b.y0, b.x1, b.y1);
            }
        }

        /// <summary>
        /// Table rows, each containing a list of cell bounding boxes
        /// aligned to the column grid.
        /// </summary>
        public List<TableRow> Rows
        {
            get
            {
                var sorted = Cells.OrderBy(c => c.y0).ThenBy(c => c.x0).ToList();
                var xs = Cells.Select(c => c.x0).Distinct().OrderBy(x => x).ToList();
                var rows = new List<TableRow>();
                foreach (var grp in sorted.GroupBy(c => c.y0))
                {
                    var xdict = new Dictionary<float, (float x0, float y0, float x1, float y1)>();
                    foreach (var cell in grp)
                        xdict[cell.x0] = cell;
                    var rowCells = xs.Select(x =>
                        xdict.ContainsKey(x)
                            ? ((float x0, float y0, float x1, float y1)?)xdict[x]
                            : null
                    ).ToList();
                    rows.Add(new TableRow(rowCells));
                }
                return rows;
            }
        }

        /// <summary>Number of rows (PyMuPDF <c>row_count</c>).</summary>
        public int RowCount => Rows.Count;

        /// <summary>Number of columns, widest row (PyMuPDF <c>col_count</c>).</summary>
        public int ColCount => Rows.Max(r => r.Cells.Count);

        /// <summary>Legacy MuPDF.NET aliases.</summary>
        public List<TableRow> rows => Rows;
        public int row_count => RowCount;
        public int col_count => ColCount;

        internal Table(Page page, List<(float x0, float y0, float x1, float y1)> cells)
        {
            Page = page;
            TextPage = null;
            Cells = cells;
            Header = GetHeader();
        }

        /// <summary>
        /// Extract cell text row-by-row (PyMuPDF <c>Table.extract</c>).
        /// Uses module-level <c>CHARS</c>; optional <c>x_shift</c>, <c>y_shift</c>, <c>layout*</c> kwargs per cell.
        /// </summary>
        public List<List<string?>> Extract(Dictionary<string, object> kwargs = null)
        {
            var chars = Chars ?? TableModule.CHARS;
            var tableArr = new List<List<string?>>();

            // def char_in_bbox(char, bbox) -> bool:
            bool CharInBbox(Dictionary<string, object> ch, (float x0, float y0, float x1, float y1) bbox)
            {
                // v_mid = (char["top"] + char["bottom"]) / 2
                float vMid = (TableHelpers.F(ch, "top") + TableHelpers.F(ch, "bottom")) / 2f;
                // h_mid = (char["x0"] + char["x1"]) / 2
                float hMid = (TableHelpers.F(ch, "x0") + TableHelpers.F(ch, "x1")) / 2f;
                // x0, top, x1, bottom = bbox
                float x0 = bbox.x0, top = bbox.y0, x1 = bbox.x1, bottom = bbox.y1;
                // return bool(
                //     (h_mid >= x0) and (h_mid < x1) and (v_mid >= top) and (v_mid < bottom)
                // )
                return hMid >= x0 && hMid < x1 && vMid >= top && vMid < bottom;
            }

            kwargs ??= new Dictionary<string, object>();

            // for row in self.rows:
            foreach (var row in Rows)
            {
                // arr = []
                var arr = new List<string?>();
                // row_chars = [char for char in chars if char_in_bbox(char, row.bbox)]
                var rowChars = chars.Where(ch => CharInBbox(ch, row.Bbox)).ToList();

                // for cell in row.cells:
                foreach (var cell in row.Cells)
                {
                    string cellText;
                    // if cell is None:
                    if (cell == null)
                    {
                        // cell_text = None
                        cellText = null;
                    }
                    else
                    {
                        var cellChars = rowChars.Where(ch => CharInBbox(ch, cell.Value)).ToList();

                        if (cellChars.Count > 0)
                        {
                            var cellKwargs = new Dictionary<string, object>(kwargs);
                            cellKwargs["x_shift"] = cell.Value.x0;
                            cellKwargs["y_shift"] = cell.Value.y0;
                            if (cellKwargs.ContainsKey("layout"))
                            {
                                cellKwargs["layout_width"] = cell.Value.x1 - cell.Value.x0;
                                cellKwargs["layout_height"] = cell.Value.y1 - cell.Value.y0;
                            }
                            cellText = TableHelpers.ExtractText(cellChars, cellKwargs);
                        }
                        else
                        {
                            // cell_text = ""
                            cellText = "";
                        }
                    }
                    // arr.append(cell_text)
                    arr.Add(cellText);
                }
                // table_arr.append(arr)
                tableArr.Add(arr);
            }

            // return table_arr
            return tableArr;
        }

        /// <summary>
        /// GitHub-flavoured Markdown for this table (PyMuPDF <c>Table.to_markdown</c>).
        /// </summary>
        /// <param name="clean">When true, escape Markdown-sensitive characters in cells and header.</param>
        /// <param name="fillEmpty">When true, copy content into null cells from left/top to approximate spans.</param>
        public string ToMarkdown(bool clean = false, bool fillEmpty = true)
        {
            int rows = RowCount;
            int cols = ColCount;

            var cellBoxes = Rows.Select(r => r.Cells.ToList()).ToList();

            var cells = new string[rows][];
            for (int i = 0; i < rows; i++)
                cells[i] = new string[cols];

            PageInfo rawPage = TextPage?.ExtractRAWDict(null, false);

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cellBoxes[i].Count; j++)
                    if (cellBoxes[i][j] != null)
                        cells[i][j] = TableHelpers.ExtractCells(
                            TextPage, cellBoxes[i][j].Value, markdown: true, rawPageInfo: rawPage);

            if (fillEmpty)
            {
                for (int j = 0; j < rows; j++)
                    for (int i = 0; i < cols - 1; i++)
                        if (cells[j][i + 1] == null)
                            cells[j][i + 1] = cells[j][i];

                for (int i = 0; i < cols; i++)
                    for (int j = 0; j < rows - 1; j++)
                        if (cells[j + 1][i] == null)
                            cells[j + 1][i] = cells[j][i];
            }

            var sb = new StringBuilder();
            sb.Append('|');
            for (int i = 0; i < Header.Names.Count; i++)
            {
                string? name = Header.Names[i];
                if (string.IsNullOrEmpty(name))
                    name = $"Col{i + 1}";
                name = name.Replace("\n", "<br>");
                if (clean)
                    name = WebUtility.HtmlEncode(name.Replace("-", "&#45;"));
                sb.Append(name);
                sb.Append('|');
            }
            sb.AppendLine();

            sb.Append('|');
            for (int i = 0; i < cols; i++)
                sb.Append("---|");
            sb.AppendLine();

            int startRow = Header.External ? 0 : 1;
            for (int r = startRow; r < rows; r++)
            {
                sb.Append('|');
                for (int c = 0; c < cols; c++)
                {
                    string cell = cells[r][c] ?? "";
                    if (clean)
                        cell = WebUtility.HtmlEncode(cell.Replace("-", "&#45;"));
                    sb.Append(cell);
                    sb.Append('|');
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            return sb.ToString();
        }

        // ─── PyMuPDF API names (internal, same assembly) ─────────────────

        internal List<List<string?>> extract(Dictionary<string, object> kwargs = null) => Extract(kwargs);
        internal string to_markdown(bool clean = false, bool fillEmpty = true) => ToMarkdown(clean, fillEmpty);

        // ── Header detection (PyMuPDF extension) ────────────────────────

        /// <summary>
        /// Identify the table header (PyMuPDF <c>Table._get_header</c>).
        /// One-line or single-column tables use the first row; returns null if there are no rows.
        /// </summary>
        private TableHeader GetHeader(float yTolerance = 3)
        {
            TableRow row;
            try
            {
                row = Rows[0];
            }
            catch
            {
                return null;
            }

            var rowCells = row.Cells;
            var bbox = row.Bbox;

            var headerTopRow = new TableHeader(
                bbox, rowCells, Extract()[0], false);

            if (Rows.Count < 2) return headerTopRow;
            if (rowCells.Count < 2) return headerTopRow;

            var row2 = Rows[1];
            if (row2.Cells.All(c => c == null))
                return headerTopRow;

            return headerTopRow;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // TableSettings
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Configuration for the table-detection algorithm.
    /// Mirrors the Python <c>TableSettings</c> dataclass.
    /// </summary>
    public class TableSettings
    {
        /// <summary>Strategy for detecting vertical lines: lines, lines_strict, text, explicit.</summary>
        public string VerticalStrategy { get; set; } = "lines";

        /// <summary>Strategy for detecting horizontal lines.</summary>
        public string HorizontalStrategy { get; set; } = "lines";

        /// <summary>Explicit vertical lines when VerticalStrategy == "explicit".</summary>
        public List<float> ExplicitVerticalLines { get; set; }

        /// <summary>Explicit horizontal lines when HorizontalStrategy == "explicit".</summary>
        public List<float> ExplicitHorizontalLines { get; set; }

        public float SnapTolerance { get; set; } = TableConstants.DefaultSnapTolerance;
        public float SnapXTolerance { get; set; } = float.NaN;
        public float SnapYTolerance { get; set; } = float.NaN;
        public float JoinTolerance { get; set; } = TableConstants.DefaultJoinTolerance;
        public float JoinXTolerance { get; set; } = float.NaN;
        public float JoinYTolerance { get; set; } = float.NaN;
        public float EdgeMinLength { get; set; } = 3;
        public float MinWordsVertical { get; set; } = TableConstants.DefaultMinWordsVertical;
        public float MinWordsHorizontal { get; set; } = TableConstants.DefaultMinWordsHorizontal;
        public float IntersectionTolerance { get; set; } = 3;
        public float IntersectionXTolerance { get; set; } = float.NaN;
        public float IntersectionYTolerance { get; set; } = float.NaN;
        public Dictionary<string, object> TextSettings { get; set; }

        /// <summary>
        /// Validate and fill in defaults for tolerance values.
        /// </summary>
        public void Validate()
        {
            if (SnapTolerance < 0 || JoinTolerance < 0 || EdgeMinLength < 0 ||
                MinWordsVertical < 0 || MinWordsHorizontal < 0 || IntersectionTolerance < 0)
                throw new ArgumentException("Table tolerance settings cannot be negative.");

            if (!TableConstants.TableStrategies.Contains(VerticalStrategy))
                throw new ArgumentException(
                    $"vertical_strategy must be one of {string.Join(",", TableConstants.TableStrategies)}");
            if (!TableConstants.TableStrategies.Contains(HorizontalStrategy))
                throw new ArgumentException(
                    $"horizontal_strategy must be one of {string.Join(",", TableConstants.TableStrategies)}");

            TextSettings ??= new Dictionary<string, object>();

            if (!TextSettings.ContainsKey("x_tolerance"))
                TextSettings["x_tolerance"] = TextSettings.ContainsKey("tolerance")
                    ? TextSettings["tolerance"] : 3f;
            if (!TextSettings.ContainsKey("y_tolerance"))
                TextSettings["y_tolerance"] = TextSettings.ContainsKey("tolerance")
                    ? TextSettings["tolerance"] : 3f;
            TextSettings.Remove("tolerance");

            if (float.IsNaN(SnapXTolerance)) SnapXTolerance = SnapTolerance;
            if (float.IsNaN(SnapYTolerance)) SnapYTolerance = SnapTolerance;
            if (float.IsNaN(JoinXTolerance)) JoinXTolerance = JoinTolerance;
            if (float.IsNaN(JoinYTolerance)) JoinYTolerance = JoinTolerance;
            if (float.IsNaN(IntersectionXTolerance)) IntersectionXTolerance = IntersectionTolerance;
            if (float.IsNaN(IntersectionYTolerance)) IntersectionYTolerance = IntersectionTolerance;
        }

        /// <summary>
        /// Create a validated <see cref="TableSettings"/> from an existing
        /// instance, a dictionary, or null (defaults).
        /// </summary>
        public static TableSettings Resolve(TableSettings? settings = null)
        {
            var ts = settings ?? new TableSettings();
            ts.Validate();
            return ts;
        }

        /// <summary>
        /// Resolve settings from a key/value dictionary.  Keys prefixed with
        /// <c>text_</c> are placed into <see cref="TextSettings"/>.
        /// </summary>
        public static TableSettings Resolve(Dictionary<string, object> dict)
        {
            if (dict == null)
            {
                var ts0 = new TableSettings();
                ts0.Validate();
                return ts0;
            }

            var ts = new TableSettings();
            var textSettings = new Dictionary<string, object>();
            foreach (var kv in dict)
            {
                if (kv.Key.StartsWith("text_"))
                {
                    textSettings[kv.Key.Substring(5)] = kv.Value;
                    continue;
                }
                switch (kv.Key)
                {
                    case "vertical_strategy": ts.VerticalStrategy = (string)kv.Value; break;
                    case "horizontal_strategy": ts.HorizontalStrategy = (string)kv.Value; break;
                    case "snap_tolerance": ts.SnapTolerance = Convert.ToSingle(kv.Value); break;
                    case "snap_x_tolerance": ts.SnapXTolerance = Convert.ToSingle(kv.Value); break;
                    case "snap_y_tolerance": ts.SnapYTolerance = Convert.ToSingle(kv.Value); break;
                    case "join_tolerance": ts.JoinTolerance = Convert.ToSingle(kv.Value); break;
                    case "join_x_tolerance": ts.JoinXTolerance = Convert.ToSingle(kv.Value); break;
                    case "join_y_tolerance": ts.JoinYTolerance = Convert.ToSingle(kv.Value); break;
                    case "edge_min_length": ts.EdgeMinLength = Convert.ToSingle(kv.Value); break;
                    case "min_words_vertical": ts.MinWordsVertical = Convert.ToSingle(kv.Value); break;
                    case "min_words_horizontal": ts.MinWordsHorizontal = Convert.ToSingle(kv.Value); break;
                    case "intersection_tolerance": ts.IntersectionTolerance = Convert.ToSingle(kv.Value); break;
                    case "intersection_x_tolerance": ts.IntersectionXTolerance = Convert.ToSingle(kv.Value); break;
                    case "intersection_y_tolerance": ts.IntersectionYTolerance = Convert.ToSingle(kv.Value); break;
                    case "explicit_vertical_lines":
                        ts.ExplicitVerticalLines = ((IEnumerable<object>)kv.Value)
                            .Select(v => Convert.ToSingle(v)).ToList();
                        break;
                    case "explicit_horizontal_lines":
                        ts.ExplicitHorizontalLines = ((IEnumerable<object>)kv.Value)
                            .Select(v => Convert.ToSingle(v)).ToList();
                        break;
                }
            }
            if (textSettings.Count > 0) ts.TextSettings = textSettings;
            ts.Validate();
            return ts;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // find_tables  (module-level in table.py; methods on TableHelpers here)
    // ────────────────────────────────────────────────────────────────────

    public static partial class TableHelpers
    {
        private static bool _recommendLayout = true;

        /// <summary>PyMuPDF <c>_warn_layout_once</c>.</summary>
        internal static void WarnLayoutOnce()
        {
            // """Check if we should recommend installing the layout package."""
            const string msg =
                "Consider using the pymupdf_layout package for a greatly improved page layout analysis.";
            if (_recommendLayout && Page.GetLayoutProvider == null
                && Environment.GetEnvironmentVariable("PYMUPDF_SUGGEST_LAYOUT_ANALYZER") != "0")
            {
                Helpers.message(msg);
                _recommendLayout = false;
            }
        }

        /// <summary>
        /// Find tables on a page. Port of <c>table.find_tables</c> (<c>src/table.py</c>).
        /// </summary>
        public static TableFinder FindTables(
            Page page,
            TableSettings settings = null,
            Rect clip = null,
            string verticalStrategy = "lines",
            string horizontalStrategy = "lines",
            List<float> verticalLines = null,
            List<float> horizontalLines = null,
            float snapTolerance = TableConstants.DefaultSnapTolerance,
            float? snapXTolerance = null,
            float? snapYTolerance = null,
            float joinTolerance = TableConstants.DefaultJoinTolerance,
            float? joinXTolerance = null,
            float? joinYTolerance = null,
            float edgeMinLength = 3,
            float minWordsVertical = TableConstants.DefaultMinWordsVertical,
            float minWordsHorizontal = TableConstants.DefaultMinWordsHorizontal,
            float intersectionTolerance = 3,
            float? intersectionXTolerance = null,
            float? intersectionYTolerance = null,
            float textTolerance = 3,
            float textXTolerance = 3,
            float textYTolerance = 3,
            string strategy = null,
            IList<(Point p1, Point p2)> addLines = null,
            IList<Rect> addBoxes = null,
            IList<Dictionary<string, object>> paths = null)
        {
            WarnLayoutOnce();
            // CHARS.clear()
            TableModule.CHARS.Clear();
            // EDGES.clear()
            TableModule.EDGES.Clear();
            // TEXTPAGE = None
            TableModule.TEXTPAGE = null;
            TextPage textpage = null;
            bool oldSmall = Tools.set_small_glyph_heights(); // save old value
            Tools.set_small_glyph_heights(true); // we need minimum bboxes
            Page workPage = page;
            int? oldXref = null;
            int? oldRot = null;
            Rect oldMediabox = null;
            if (page.Rotation != 0)
            {
                var derot = TableHelpers.PageRotationSet0(page);
                workPage = derot.page;
                oldXref = derot.xref;
                oldRot = derot.rot;
                oldMediabox = derot.mediabox;
            }

            if (snapXTolerance == null)
                snapXTolerance = float.NaN;
            if (snapYTolerance == null)
                snapYTolerance = float.NaN;
            if (joinXTolerance == null)
                joinXTolerance = float.NaN;
            if (joinYTolerance == null)
                joinYTolerance = float.NaN;
            if (intersectionXTolerance == null)
                intersectionXTolerance = float.NaN;
            if (intersectionYTolerance == null)
                intersectionYTolerance = float.NaN;
            if (strategy != null)
            {
                verticalStrategy = strategy;
                horizontalStrategy = strategy;
            }

            TableSettings tset;
            if (settings != null)
                tset = TableSettings.Resolve(settings);
            else
            {
                tset = new TableSettings
                {
                    VerticalStrategy = verticalStrategy,
                    HorizontalStrategy = horizontalStrategy,
                    ExplicitVerticalLines = verticalLines,
                    ExplicitHorizontalLines = horizontalLines,
                    SnapTolerance = snapTolerance,
                    SnapXTolerance = snapXTolerance.Value,
                    SnapYTolerance = snapYTolerance.Value,
                    JoinTolerance = joinTolerance,
                    JoinXTolerance = joinXTolerance.Value,
                    JoinYTolerance = joinYTolerance.Value,
                    EdgeMinLength = edgeMinLength,
                    MinWordsVertical = minWordsVertical,
                    MinWordsHorizontal = minWordsHorizontal,
                    IntersectionTolerance = intersectionTolerance,
                    IntersectionXTolerance = intersectionXTolerance.Value,
                    IntersectionYTolerance = intersectionYTolerance.Value,
                    TextSettings = new Dictionary<string, object>
                    {
                        ["x_tolerance"] = textXTolerance,
                        ["y_tolerance"] = textYTolerance,
                        ["tolerance"] = textTolerance,
                    },
                };
                tset.Validate();
            }

            bool oldQuadCorrections = Helpers.SkipQuadCorrections;
            TableFinder tbf = null;
            try
            {
                workPage.GetLayout();
                List<Rect> boxes;
                if (workPage.LayoutInformation != null)
                {
                    Helpers.SkipQuadCorrections = true;
                    boxes = TableHelpers.LayoutTableBoxes(workPage.LayoutInformation);
                }
                else
                    boxes = new List<Rect>();

                if (boxes.Count > 0)
                {
                    // layout did find some tables
                }
                else if (workPage.LayoutInformation != null)
                {
                    // layout was executed but found no tables
                    // make sure we exit quickly with an empty TableFinder
                    return new TableFinder(workPage);
                }

                workPage.TableSettings = tset;

                // TEXTPAGE = make_chars(page, clip=clip)  # create character list of page
                textpage = TableHelpers.MakeChars(workPage, TableModule.CHARS, clip: clip);
                // make_edges(page, clip=clip, tset=tset, paths=paths, add_lines=add_lines, add_boxes=add_boxes)  # create lines and curves
                TableHelpers.MakeEdges(
                    workPage,
                    TableModule.EDGES,
                    TableModule.CHARS,
                    tset,
                    clip,
                    paths,
                    addLines,
                    addBoxes);

                tbf = TableFinder.FromPrepared(workPage, tset, TableModule.CHARS, TableModule.EDGES, textpage);
                // tbf.textpage = TEXTPAGE  # store textpage for later use
                tbf.TextPage = textpage;
                TableModule.TEXTPAGE = textpage;
                if (boxes.Count > 0)
                {
                    // only keep Finder tables that match a layout box
                    tbf.Tables = tbf.Tables
                        .Where(tab => boxes.Any(r => TableHelpers.Iou(tab.Bbox, ToTuple(r)) >= 0.6f))
                        .ToList();
                }
                // build the complementary list of layout table boxes
                var myBoxes = boxes
                    .Where(r => tbf.Tables.All(tab => TableHelpers.Iou(ToTuple(r), tab.Bbox) < 0.6f))
                    .ToList();
                if (myBoxes.Count > 0)
                {
                    var wordRects = textpage.ExtractWords()
                        .Select(w => new Rect(w.X0, w.Y0, w.X1, w.Y1))
                        .ToList();
                    var tp2 = workPage.GetTextPage(TableConstants.TableDetectorFlags);
                    foreach (var rect in myBoxes)
                    {
                        var cells = TableHelpers.MakeTableFromBbox(tp2, wordRects, rect);
                        if (cells.Count > 0)
                            tbf.Tables.Add(new Table(workPage, cells));
                    }
                }
            }
            catch (Exception e)
            {
                Helpers.message($"find_tables: exception occurred: {e}");
                return null;
            }
            finally
            {
                Tools.set_small_glyph_heights(oldSmall);
                if (oldXref != null && oldMediabox != null)
                    TableHelpers.PageRotationReset(workPage, oldXref.Value, oldRot.Value, oldMediabox);
                Helpers.SkipQuadCorrections = oldQuadCorrections;
            }
            // for table in tbf.tables:
            //     table.textpage = TEXTPAGE
            foreach (var table in tbf.Tables)
            {
                table.TextPage = TableModule.TEXTPAGE;
                table.Chars = new List<Dictionary<string, object>>(TableModule.CHARS);
            }
            return tbf;

            static (float x0, float y0, float x1, float y1) ToTuple(Rect r)
                => ((float)r.X0, (float)r.Y0, (float)r.X1, (float)r.Y1);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // TableFinder
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds all tables on a PDF page using pdfplumber-style algorithms.
    /// </summary>
    public class TableFinder
    {
        /// <summary>The page being analysed.</summary>
        public Page Page { get; }

        /// <summary>Detected tables, sorted top-to-bottom then left-to-right.</summary>
        public List<Table> Tables { get; internal set; }

        /// <summary>Active settings used during detection.</summary>
        public TableSettings Settings { get; }

        /// <summary>PyMuPDF <c>TableFinder.textpage</c>.</summary>
        public TextPage TextPage { get; internal set; }

        internal List<Dictionary<string, object>> Edges;
        internal List<Dictionary<string, object>> Chars;
        internal int ResolvedEdgeCount;
        internal int IntersectionCount;
        internal int CellCount;

        // Legacy aliases from MuPDF.NET public API.
        public List<Table> tables
        {
            get => Tables;
            set => Tables = value;
        }
        public Page page => Page;
        public TextPage textpage
        {
            get => TextPage;
            set => TextPage = value;
        }
        public List<(float x0, float y0, float x1, float y1)> cells
            => Tables?.SelectMany(t => t.Cells).ToList() ?? new List<(float x0, float y0, float x1, float y1)>();
        public List<Dictionary<string, object>> edges
            => Edges ?? new List<Dictionary<string, object>>();

        /// <summary>Empty finder when layout ran but found no tables.</summary>
        public TableFinder(Page page)
        {
            Page = page;
            Settings = TableSettings.Resolve((TableSettings)null);
            Tables = new List<Table>();
            Chars = new List<Dictionary<string, object>>();
            Edges = new List<Dictionary<string, object>>();
        }

        // Legacy entrypoint alias for table detection.
        public static TableFinder FindTables(Page page, TableSettings settings = null)
            => page?.FindTables(settings) ?? new TableFinder(page);

        /// <summary>
        /// Create a <see cref="TableFinder"/> and immediately detect all tables
        /// on the given page.
        /// </summary>
        public TableFinder(Page page, TableSettings settings = null)
        {
            Page = page;
            Settings = TableSettings.Resolve(settings);
            Chars = new List<Dictionary<string, object>>();
            Edges = new List<Dictionary<string, object>>();

            var textpage = TableHelpers.CreateTextPage(page);
            TableHelpers.MakeChars(page, Chars, textpage);
            TableHelpers.MakeEdges(page, Edges, Chars, Settings);
            CompleteDetection(textpage);
        }

        /// <summary>PyMuPDF <c>find_tables</c> after <c>make_chars</c> / <c>make_edges</c>.</summary>
        internal static TableFinder FromPrepared(
            Page page,
            TableSettings settings,
            List<Dictionary<string, object>> chars,
            List<Dictionary<string, object>> edges,
            TextPage textpage)
        {
            var tbf = new TableFinder(page, settings, chars, edges, textpage);
            return tbf;
        }

        private TableFinder(
            Page page,
            TableSettings settings,
            List<Dictionary<string, object>> chars,
            List<Dictionary<string, object>> edges,
            TextPage textpage)
        {
            Page = page;
            Settings = TableSettings.Resolve(settings);
            Chars = chars;
            Edges = edges;
            CompleteDetection(textpage);
        }

        private void CompleteDetection(TextPage textpage)
        {
            TextPage = textpage;
            var resolvedEdges = GetEdges();
            ResolvedEdgeCount = resolvedEdges.Count;

            var intersections = TableHelpers.EdgesToIntersections(
                resolvedEdges,
                Settings.IntersectionXTolerance,
                Settings.IntersectionYTolerance);
            IntersectionCount = intersections.Count;

            var cells = TableHelpers.IntersectionsToCells(intersections);
            CellCount = cells.Count;

            var cellGroups = TableHelpers.CellsToTables(Page, cells, textpage);

            Tables = cellGroups.Select(g => new Table(Page, g)).ToList();
            foreach (var table in Tables)
            {
                table.TextPage = textpage;
                table.Chars = new List<Dictionary<string, object>>(Chars);
            }
        }

        /// <summary>
        /// Index into the tables list.
        /// </summary>
        public Table this[int i]
        {
            get
            {
                int count = Tables.Count;
                if (i >= count)
                    throw new IndexOutOfRangeException("table not on page");
                while (i < 0)
                    i += count;
                return Tables[i];
            }
        }

        /// <summary>
        /// Resolve and merge all edges for the current page according to the
        /// active strategy settings (lines, lines_strict, text, or explicit).
        /// </summary>
        private List<Dictionary<string, object>> GetEdges()
        {
            var settings = Settings;

            foreach (var orientation in new[] { "vertical", "horizontal" })
            {
                string strategy = orientation == "vertical"
                    ? settings.VerticalStrategy : settings.HorizontalStrategy;
                if (strategy == "explicit")
                {
                    var lines = orientation == "vertical"
                        ? settings.ExplicitVerticalLines
                        : settings.ExplicitHorizontalLines;
                    if (lines == null || lines.Count < 2)
                        throw new ArgumentException(
                            $"If {orientation}_strategy == 'explicit', " +
                            $"explicit_{orientation}_lines must have at least 2 entries.");
                }
            }

            string vStrat = settings.VerticalStrategy;
            string hStrat = settings.HorizontalStrategy;

            List<Dictionary<string, object>> words;
            if (vStrat == "text" || hStrat == "text")
                words = TableHelpers.ExtractWords(Chars, settings.TextSettings);
            else
                words = new List<Dictionary<string, object>>();

            var v = new List<Dictionary<string, object>>();

            if (settings.ExplicitVerticalLines != null)
            {
                var pageRect = Page.Rect;
                foreach (float desc in settings.ExplicitVerticalLines)
                {
                    v.Add(new Dictionary<string, object>
                    {
                        ["x0"] = desc,
                        ["x1"] = desc,
                        ["top"] = (float)pageRect.Y0,
                        ["bottom"] = (float)pageRect.Y1,
                        ["height"] = (float)(pageRect.Y1 - pageRect.Y0),
                        ["orientation"] = "v",
                    });
                }
            }

            if (vStrat == "lines")
                v.AddRange(TableHelpers.FilterEdges(Edges, "v"));
            else if (vStrat == "lines_strict")
                v.AddRange(TableHelpers.FilterEdges(Edges, "v", edgeType: "line"));
            else if (vStrat == "text")
                v.AddRange(TableHelpers.WordsToEdgesV(words, (int)settings.MinWordsVertical));

            var h = new List<Dictionary<string, object>>();

            if (settings.ExplicitHorizontalLines != null)
            {
                var pageRect = Page.Rect;
                foreach (float desc in settings.ExplicitHorizontalLines)
                {
                    h.Add(new Dictionary<string, object>
                    {
                        ["x0"] = (float)pageRect.X0,
                        ["x1"] = (float)pageRect.X1,
                        ["width"] = (float)(pageRect.X1 - pageRect.X0),
                        ["top"] = desc,
                        ["bottom"] = desc,
                        ["orientation"] = "h",
                    });
                }
            }

            if (hStrat == "lines")
                h.AddRange(TableHelpers.FilterEdges(Edges, "h"));
            else if (hStrat == "lines_strict")
                h.AddRange(TableHelpers.FilterEdges(Edges, "h", edgeType: "line"));
            else if (hStrat == "text")
                h.AddRange(TableHelpers.WordsToEdgesH(words, (int)settings.MinWordsHorizontal));

            var edges = new List<Dictionary<string, object>>();
            edges.AddRange(v);
            edges.AddRange(h);

            edges = TableHelpers.MergeEdges(
                edges,
                settings.SnapXTolerance,
                settings.SnapYTolerance,
                settings.JoinXTolerance,
                settings.JoinYTolerance);

            return TableHelpers.FilterEdges(edges, minLength: settings.EdgeMinLength);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // TableHelpers  – static helper functions
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Static helper methods implementing the pdfplumber-style table-detection
    /// algorithms: edge snapping, intersection detection, cell enumeration,
    /// text extraction, etc.
    /// </summary>
    public static partial class TableHelpers
    {
        // ── Geometry primitives ─────────────────────────────────────────

        /// <summary>
        /// Check whether rectangle <paramref name="inner"/> is fully inside
        /// rectangle <paramref name="outer"/>.
        /// </summary>
        public static bool RectInRect(
            (float x0, float y0, float x1, float y1) inner,
            (float x0, float y0, float x1, float y1) outer)
        {
            return inner.x0 >= outer.x0 && inner.y0 >= outer.y0
                && inner.x1 <= outer.x1 && inner.y1 <= outer.y1;
        }

        /// <summary>
        /// Check whether any character in <paramref name="chars"/> lies inside
        /// rectangle <paramref name="rect"/>.
        /// </summary>
        public static bool CharsInRect(
            List<Dictionary<string, object>> chars,
            (float x0, float y0, float x1, float y1) rect)
        {
            return chars.Any(c =>
                rect.x0 <= F(c, "x0") && F(c, "x1") <= rect.x1 &&
                rect.y0 <= F(c, "top") && F(c, "bottom") >= rect.y1);
        }

        /// <summary>
        /// Compute Intersection over Union (IoU) of two rectangles.
        /// </summary>
        public static float Iou(
            (float x0, float y0, float x1, float y1) r1,
            (float x0, float y0, float x1, float y1) r2)
        {
            float ix = Math.Max(0, Math.Min(r1.x1, r2.x1) - Math.Max(r1.x0, r2.x0));
            float iy = Math.Max(0, Math.Min(r1.y1, r2.y1) - Math.Max(r1.y0, r2.y0));
            float intersection = ix * iy;
            if (intersection == 0) return 0;
            float area1 = (r1.x1 - r1.x0) * (r1.y1 - r1.y0);
            float area2 = (r2.x1 - r2.x0) * (r2.y1 - r2.y0);
            return intersection / (area1 + area2 - intersection);
        }

        // ── Edge conversion helpers ─────────────────────────────────────

        /// <summary>
        /// Convert a line dictionary to an edge dictionary by adding an
        /// <c>orientation</c> key.
        /// </summary>
        public static Dictionary<string, object> LineToEdge(Dictionary<string, object> line)
        {
            var edge = new Dictionary<string, object>(line);
            edge["orientation"] = F(line, "top") == F(line, "bottom") ? "h" : "v";
            return edge;
        }

        /// <summary>
        /// Decompose a rectangle dictionary into its four constituent edges.
        /// </summary>
        public static List<Dictionary<string, object>> RectToEdges(Dictionary<string, object> rect)
        {
            var result = new List<Dictionary<string, object>>();

            var top = new Dictionary<string, object>(rect);
            top["object_type"] = "rect_edge";
            top["height"] = 0f;
            top["y0"] = F(rect, "y1");
            top["bottom"] = F(rect, "top");
            top["orientation"] = "h";

            var bottom = new Dictionary<string, object>(rect);
            bottom["object_type"] = "rect_edge";
            bottom["height"] = 0f;
            bottom["y1"] = F(rect, "y0");
            bottom["top"] = F(rect, "top") + F(rect, "height");
            bottom["doctop"] = F(rect, "doctop") + F(rect, "height");
            bottom["orientation"] = "h";

            var left = new Dictionary<string, object>(rect);
            left["object_type"] = "rect_edge";
            left["width"] = 0f;
            left["x1"] = F(rect, "x0");
            left["orientation"] = "v";

            var right = new Dictionary<string, object>(rect);
            right["object_type"] = "rect_edge";
            right["width"] = 0f;
            right["x0"] = F(rect, "x1");
            right["orientation"] = "v";

            result.Add(top);
            result.Add(bottom);
            result.Add(left);
            result.Add(right);
            return result;
        }

        /// <summary>
        /// Convert any object (line, rect, curve_edge) to a list of edges.
        /// </summary>
        public static List<Dictionary<string, object>> ObjToEdges(Dictionary<string, object> obj)
        {
            string t = (string)obj["object_type"];
            if (t.Contains("_edge"))
                return new List<Dictionary<string, object>> { obj };
            if (t == "line")
                return new List<Dictionary<string, object>> { LineToEdge(obj) };
            if (t == "rect")
                return RectToEdges(obj);
            return new List<Dictionary<string, object>>();
        }

        // ── Filtering ───────────────────────────────────────────────────

        /// <summary>
        /// Filter edges by orientation, type, and minimum length.
        /// </summary>
        public static List<Dictionary<string, object>> FilterEdges(
            List<Dictionary<string, object>> edges,
            string orientation = null,
            string edgeType = null,
            float minLength = 1)
        {
            if (orientation != null && orientation != "v" && orientation != "h")
                throw new ArgumentException("Orientation must be 'v' or 'h'");

            return edges.Where(e =>
            {
                string dim = (string)e["orientation"] == "v" ? "height" : "width";
                bool etOk = edgeType == null || (string)e["object_type"] == edgeType;
                bool orientOk = orientation == null || (string)e["orientation"] == orientation;
                return etOk && orientOk && F(e, dim) >= minLength;
            }).ToList();
        }

        // ── Clustering ──────────────────────────────────────────────────

        /// <summary>
        /// Cluster a list of float values so that consecutive values within
        /// <paramref name="tolerance"/> of each other are in the same cluster.
        /// </summary>
        public static List<List<float>> ClusterList(List<float> xs, float tolerance = 0)
        {
            if (xs.Count < 2)
                return xs.Select(x => new List<float> { x }).ToList();

            var sorted = xs.OrderBy(x => x).ToList();
            var groups = new List<List<float>>();
            var current = new List<float> { sorted[0] };
            float last = sorted[0];

            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i] <= last + tolerance)
                    current.Add(sorted[i]);
                else
                {
                    groups.Add(current);
                    current = new List<float> { sorted[i] };
                }
                last = sorted[i];
            }
            groups.Add(current);
            return groups;
        }

        /// <summary>
        /// Build a mapping from value → cluster index.
        /// </summary>
        internal static Dictionary<float, int> MakeClusterDict(
            IEnumerable<float> values, float tolerance)
        {
            var clusters = ClusterList(
                new List<float>(new HashSet<float>(values)), tolerance);
            var dict = new Dictionary<float, int>();
            for (int i = 0; i < clusters.Count; i++)
                foreach (float v in clusters[i])
                    dict[v] = i;
            return dict;
        }

        /// <summary>
        /// Group objects by a float key, clustering keys within
        /// <paramref name="tolerance"/>.
        /// </summary>
        internal static List<List<T>> ClusterObjects<T>(
            List<T> xs, Func<T, float> keyFn, float tolerance)
        {
            var values = xs.Select(keyFn);
            var clusterDict = MakeClusterDict(values, tolerance);
            var tuples = xs.Select(x => (obj: x, cluster: clusterDict[keyFn(x)]))
                           .OrderBy(t => t.cluster)
                           .ToList();
            var result = new List<List<T>>();
            foreach (var grp in tuples.GroupBy(t => t.cluster))
                result.Add(grp.Select(t => t.obj).ToList());
            return result;
        }

        // ── Edge snapping / merging ─────────────────────────────────────

        private static Dictionary<string, object> MoveObject(
            Dictionary<string, object> obj, string axis, float value)
        {
            var n = new Dictionary<string, object>(obj);
            if (axis == "h")
            {
                n["x0"] = F(obj, "x0") + value;
                n["x1"] = F(obj, "x1") + value;
            }
            else
            {
                n["top"] = F(obj, "top") + value;
                n["bottom"] = F(obj, "bottom") + value;
                if (obj.ContainsKey("doctop"))
                    n["doctop"] = F(obj, "doctop") + value;
                if (obj.ContainsKey("y0"))
                {
                    n["y0"] = F(obj, "y0") - value;
                    n["y1"] = F(obj, "y1") - value;
                }
            }
            return n;
        }

        private static List<Dictionary<string, object>> SnapObjects(
            List<Dictionary<string, object>> objs, string attr, float tolerance)
        {
            string axis = (attr == "x0" || attr == "x1") ? "h" : "v";
            var clusters = ClusterObjects(objs, o => F(o, attr), tolerance);
            var snapped = new List<Dictionary<string, object>>();
            foreach (var cluster in clusters)
            {
                float avg = cluster.Sum(o => F(o, attr)) / cluster.Count;
                foreach (var obj in cluster)
                    snapped.Add(MoveObject(obj, axis, avg - F(obj, attr)));
            }
            return snapped;
        }

        /// <summary>
        /// Snap edges within tolerance of each other to their positional average.
        /// </summary>
        public static List<Dictionary<string, object>> SnapEdges(
            List<Dictionary<string, object>> edges,
            float xTolerance = TableConstants.DefaultSnapTolerance,
            float yTolerance = TableConstants.DefaultSnapTolerance)
        {
            var byOrientation = new Dictionary<string, List<Dictionary<string, object>>>
            {
                ["v"] = new List<Dictionary<string, object>>(),
                ["h"] = new List<Dictionary<string, object>>(),
            };
            foreach (var e in edges)
                byOrientation[(string)e["orientation"]].Add(e);

            var snappedV = SnapObjects(byOrientation["v"], "x0", xTolerance);
            var snappedH = SnapObjects(byOrientation["h"], "top", yTolerance);
            var result = new List<Dictionary<string, object>>(snappedV);
            result.AddRange(snappedH);
            return result;
        }

        private static Dictionary<string, object> ResizeObject(
            Dictionary<string, object> obj, string key, float value)
        {
            var n = new Dictionary<string, object>(obj);
            float old = F(obj, key);
            float diff = value - old;
            n[key] = value;
            switch (key)
            {
                case "x0":
                    n["width"] = F(obj, "x1") - value;
                    break;
                case "x1":
                    n["width"] = value - F(obj, "x0");
                    break;
                case "top":
                    n["doctop"] = F(obj, "doctop") + diff;
                    n["height"] = F(obj, "height") - diff;
                    if (obj.ContainsKey("y1"))
                        n["y1"] = F(obj, "y1") - diff;
                    break;
                case "bottom":
                    n["height"] = F(obj, "height") + diff;
                    if (obj.ContainsKey("y0"))
                        n["y0"] = F(obj, "y0") - diff;
                    break;
            }
            return n;
        }

        private static List<Dictionary<string, object>> JoinEdgeGroup(
            List<Dictionary<string, object>> edges, string orientation,
            float tolerance = TableConstants.DefaultJoinTolerance)
        {
            string minProp, maxProp;
            if (orientation == "h")
            { minProp = "x0"; maxProp = "x1"; }
            else
            { minProp = "top"; maxProp = "bottom"; }

            var sorted = edges.OrderBy(e => F(e, minProp)).ToList();
            var joined = new List<Dictionary<string, object>> { sorted[0] };
            for (int i = 1; i < sorted.Count; i++)
            {
                var e = sorted[i];
                var last = joined[joined.Count - 1];
                if (F(e, minProp) <= F(last, maxProp) + tolerance)
                {
                    if (F(e, maxProp) > F(last, maxProp))
                        joined[joined.Count - 1] = ResizeObject(last, maxProp, F(e, maxProp));
                }
                else
                {
                    joined.Add(e);
                }
            }
            return joined;
        }

        /// <summary>
        /// Snap then join a list of edges into a more seamless set.
        /// </summary>
        public static List<Dictionary<string, object>> MergeEdges(
            List<Dictionary<string, object>> edges,
            float snapXTolerance,
            float snapYTolerance,
            float joinXTolerance,
            float joinYTolerance)
        {
            if (edges.Count == 0) return edges;

            if (snapXTolerance > 0 || snapYTolerance > 0)
                edges = SnapEdges(edges, snapXTolerance, snapYTolerance);

            Func<Dictionary<string, object>, (string, float)> getGroup = edge =>
            {
                string o = (string)edge["orientation"];
                return o == "h" ? ("h", F(edge, "top")) : ("v", F(edge, "x0"));
            };

            var sorted = edges.OrderBy(e => getGroup(e)).ToList();
            var result = new List<Dictionary<string, object>>();
            foreach (var grp in sorted.GroupBy(e => getGroup(e)))
            {
                float tol = grp.Key.Item1 == "h" ? joinXTolerance : joinYTolerance;
                result.AddRange(JoinEdgeGroup(grp.ToList(), grp.Key.Item1, tol));
            }
            return result;
        }

        // ── Intersection detection ──────────────────────────────────────

        /// <summary>
        /// Given a list of edges, find all intersection points (within tolerance).
        /// Returns a dictionary keyed by (x, y) vertex with lists of
        /// vertical and horizontal edges that meet there.
        /// </summary>
        public static Dictionary<(float x, float y), Dictionary<string, List<Dictionary<string, object>>>>
            EdgesToIntersections(
                List<Dictionary<string, object>> edges,
                float xTolerance = 1,
                float yTolerance = 1)
        {
            var intersections = new Dictionary<(float, float),
                Dictionary<string, List<Dictionary<string, object>>>>();

            var vEdges = edges.Where(e => (string)e["orientation"] == "v")
                .OrderBy(e => F(e, "x0")).ThenBy(e => F(e, "top")).ToList();
            var hEdges = edges.Where(e => (string)e["orientation"] == "h")
                .OrderBy(e => F(e, "top")).ThenBy(e => F(e, "x0")).ToList();

            foreach (var v in vEdges)
            {
                foreach (var h in hEdges)
                {
                    if (F(v, "top") <= F(h, "top") + yTolerance
                        && F(v, "bottom") >= F(h, "top") - yTolerance
                        && F(v, "x0") >= F(h, "x0") - xTolerance
                        && F(v, "x0") <= F(h, "x1") + xTolerance)
                    {
                        var vertex = (F(v, "x0"), F(h, "top"));
                        if (!intersections.ContainsKey(vertex))
                            intersections[vertex] = new Dictionary<string, List<Dictionary<string, object>>>
                            {
                                ["v"] = new List<Dictionary<string, object>>(),
                                ["h"] = new List<Dictionary<string, object>>(),
                            };
                        intersections[vertex]["v"].Add(v);
                        intersections[vertex]["h"].Add(h);
                    }
                }
            }
            return intersections;
        }

        // ── Cell enumeration ────────────────────────────────────────────

        private static (float, float, float, float) ObjToBbox(Dictionary<string, object> obj)
        {
            return (F(obj, "x0"), F(obj, "top"), F(obj, "x1"), F(obj, "bottom"));
        }

        /// <summary>
        /// Given intersection points, enumerate all rectangular cells formed
        /// by connected edges.
        /// </summary>
        public static List<(float x0, float y0, float x1, float y1)> IntersectionsToCells(
            Dictionary<(float x, float y), Dictionary<string, List<Dictionary<string, object>>>> intersections)
        {
            bool EdgeConnects((float x, float y) p1, (float x, float y) p2)
            {
                HashSet<(float, float, float, float)> EdgesToSet(List<Dictionary<string, object>> el)
                    => new HashSet<(float, float, float, float)>(el.Select(ObjToBbox));

                if (p1.x == p2.x)
                {
                    var common = EdgesToSet(intersections[p1]["v"])
                        .Intersect(EdgesToSet(intersections[p2]["v"]));
                    if (common.Any()) return true;
                }
                if (p1.y == p2.y)
                {
                    var common = EdgesToSet(intersections[p1]["h"])
                        .Intersect(EdgesToSet(intersections[p2]["h"]));
                    if (common.Any()) return true;
                }
                return false;
            }

            // points = list(sorted(intersections.keys()))
            var points = intersections.Keys.OrderBy(p => p.x).ThenBy(p => p.y).ToList();
            int nPoints = points.Count;
            var cells = new List<(float x0, float y0, float x1, float y1)>();

            // def find_smallest_cell(points, i: int):
            for (int i = 0; i < nPoints; i++)
            {
                if (i == nPoints - 1)
                    continue;
                var pt = points[i];
                var rest = points.Skip(i + 1).ToList();
                // below = [x for x in rest if x[0] == pt[0]]
                var below = rest.Where(p => p.x == pt.x).ToList();
                // right = [x for x in rest if x[1] == pt[1]]
                var right = rest.Where(p => p.y == pt.y).ToList();

                foreach (var belowPt in below)
                {
                    if (!EdgeConnects(pt, belowPt))
                        continue;

                    foreach (var rightPt in right)
                    {
                        if (!EdgeConnects(pt, rightPt))
                            continue;

                        var bottomRight = (rightPt.x, belowPt.y);

                        if (intersections.ContainsKey(bottomRight)
                            && EdgeConnects(bottomRight, rightPt)
                            && EdgeConnects(bottomRight, belowPt))
                        {
                            // return (pt[0], pt[1], bottom_right[0], bottom_right[1])
                            cells.Add((pt.x, pt.y, bottomRight.x, bottomRight.y));
                            goto nextPoint;
                        }
                    }
                }
                nextPoint:;
            }
            // cell_gen = (find_smallest_cell(points, i) for i in range(len(points)))
            // return list(filter(None, cell_gen))
            return cells;
        }

        // ── Cells → Tables ──────────────────────────────────────────────

        /// <summary>PyMuPDF <c>white_spaces.issuperset(text)</c> for table filtering.</summary>
        private static bool IsWhitespaceSuperset(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;
            foreach (char ch in text)
            {
                if (!TableConstants.WhiteSpaces.Contains(ch))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Group cells into contiguous tables based on shared corners.
        /// </summary>
        public static List<List<(float x0, float y0, float x1, float y1)>> CellsToTables(
            Page page,
            List<(float x0, float y0, float x1, float y1)> cells,
            TextPage textpage)
        {
            (float, float)[] BboxToCorners((float x0, float y0, float x1, float y1) bbox)
                => new[]
                {
                    (bbox.x0, bbox.y0), (bbox.x0, bbox.y1),
                    (bbox.x1, bbox.y0), (bbox.x1, bbox.y1)
                };

            var remaining = new List<(float x0, float y0, float x1, float y1)>(cells);
            var currentCorners = new HashSet<(float, float)>();
            var currentCells = new List<(float x0, float y0, float x1, float y1)>();
            var tables = new List<List<(float x0, float y0, float x1, float y1)>>();

            while (remaining.Count > 0)
            {
                int initialCount = currentCells.Count;
                // for cell in list(remaining_cells):
                foreach (var cell in remaining.ToList())
                {
                    var corners = BboxToCorners(cell);
                    if (currentCells.Count == 0)
                    {
                        foreach (var c in corners) currentCorners.Add(c);
                        currentCells.Add(cell);
                        remaining.Remove(cell);
                    }
                    else
                    {
                        int cornerCount = corners.Count(c => currentCorners.Contains(c));
                        if (cornerCount > 0)
                        {
                            foreach (var c in corners) currentCorners.Add(c);
                            currentCells.Add(cell);
                            remaining.Remove(cell);
                        }
                    }
                }
                if (currentCells.Count == initialCount)
                {
                    tables.Add(new List<(float, float, float, float)>(currentCells));
                    currentCorners.Clear();
                    currentCells.Clear();
                }
            }
            if (currentCells.Count > 0)
                tables.Add(new List<(float, float, float, float)>(currentCells));

            for (int i = tables.Count - 1; i >= 0; i--)
            {
                var x1Vals = new HashSet<float>(tables[i].Select(c => c.x1));
                var x0Vals = new HashSet<float>(tables[i].Select(c => c.x0));
                var r = new Rect();
                foreach (var c in tables[i])
                    r = r.IsEmpty ? new Rect(c.x0, c.y0, c.x1, c.y1) : r.IncludeRect(new Rect(c.x0, c.y0, c.x1, c.y1));
                if (x1Vals.Count < 2
                    || x0Vals.Count < 2
                    || IsWhitespaceSuperset(page.GetTextbox(r, textpage)))
                {
                    tables.RemoveAt(i);
                }
            }

            tables.Sort((a, b) =>
            {
                float ay = a.Min(c => c.y0);
                float ax = a.Min(c => c.x0);
                float by = b.Min(c => c.y0);
                float bx = b.Min(c => c.x0);
                int cmp = ay.CompareTo(by);
                return cmp != 0 ? cmp : ax.CompareTo(bx);
            });

            return tables;
        }

        // ── Word-based edge generation ──────────────────────────────────

        private static (float x0, float top, float x1, float bottom) ObjectsToBbox(
            List<Dictionary<string, object>> objects)
        {
            return (
                objects.Min(o => F(o, "x0")),
                objects.Min(o => F(o, "top")),
                objects.Max(o => F(o, "x1")),
                objects.Max(o => F(o, "bottom"))
            );
        }

        private static Dictionary<string, object> ObjectsToRect(
            List<Dictionary<string, object>> objects)
        {
            var bb = ObjectsToBbox(objects);
            return new Dictionary<string, object>
            {
                ["x0"] = bb.x0, ["top"] = bb.top,
                ["x1"] = bb.x1, ["bottom"] = bb.bottom,
            };
        }

        /// <summary>
        /// Find imaginary horizontal edges connecting tops of word clusters.
        /// </summary>
        public static List<Dictionary<string, object>> WordsToEdgesH(
            List<Dictionary<string, object>> words, int wordThreshold = 1)
        {
            var byTop = ClusterObjects(words, w => F(w, "top"), 1);
            var largeClusters = byTop.Where(c => c.Count >= wordThreshold).ToList();
            var rects = largeClusters.Select(ObjectsToRect).ToList();
            if (rects.Count == 0) return new List<Dictionary<string, object>>();

            float minX0 = rects.Min(r => F(r, "x0"));
            float maxX1 = rects.Max(r => F(r, "x1"));
            var edges = new List<Dictionary<string, object>>();

            foreach (var r in rects)
            {
                edges.Add(new Dictionary<string, object>
                {
                    ["x0"] = minX0, ["x1"] = maxX1,
                    ["top"] = F(r, "top"), ["bottom"] = F(r, "top"),
                    ["width"] = maxX1 - minX0,
                    ["orientation"] = "h",
                });
                edges.Add(new Dictionary<string, object>
                {
                    ["x0"] = minX0, ["x1"] = maxX1,
                    ["top"] = F(r, "bottom"), ["bottom"] = F(r, "bottom"),
                    ["width"] = maxX1 - minX0,
                    ["orientation"] = "h",
                });
            }
            return edges;
        }

        /// <summary>
        /// Find imaginary vertical edges connecting left/right/centre of word clusters.
        /// </summary>
        public static List<Dictionary<string, object>> WordsToEdgesV(
            List<Dictionary<string, object>> words, int wordThreshold = 3)
        {
            var byX0 = ClusterObjects(words, w => F(w, "x0"), 1);
            var byX1 = ClusterObjects(words, w => F(w, "x1"), 1);
            var byCenter = ClusterObjects(words, w => (F(w, "x0") + F(w, "x1")) / 2f, 1);

            var clusters = new List<List<Dictionary<string, object>>>();
            clusters.AddRange(byX0);
            clusters.AddRange(byX1);
            clusters.AddRange(byCenter);

            var sorted = clusters.OrderByDescending(c => c.Count).ToList();
            var large = sorted.Where(c => c.Count >= wordThreshold).ToList();
            var bboxes = large.Select(c => ObjectsToBbox(c)).ToList();

            var condensed = new List<(float x0, float top, float x1, float bottom)>();
            foreach (var bb in bboxes)
            {
                bool overlap = condensed.Any(c => GetBboxOverlap(
                    (bb.x0, bb.top, bb.x1, bb.bottom),
                    (c.x0, c.top, c.x1, c.bottom)) != null);
                if (!overlap) condensed.Add(bb);
            }

            if (condensed.Count == 0) return new List<Dictionary<string, object>>();

            float maxX1 = condensed.Max(c => c.x1);
            float minTop = condensed.Min(c => c.top);
            float maxBottom = condensed.Max(c => c.bottom);

            var edges = condensed.Select(b => new Dictionary<string, object>
            {
                ["x0"] = b.x0, ["x1"] = b.x0,
                ["top"] = minTop, ["bottom"] = maxBottom,
                ["height"] = maxBottom - minTop,
                ["orientation"] = "v",
            }).ToList();

            edges.Add(new Dictionary<string, object>
            {
                ["x0"] = maxX1, ["x1"] = maxX1,
                ["top"] = minTop, ["bottom"] = maxBottom,
                ["height"] = maxBottom - minTop,
                ["orientation"] = "v",
            });
            return edges;
        }

        /// <summary>
        /// Get the overlapping bbox of two bounding boxes, or null if none.
        /// </summary>
        public static (float x0, float y0, float x1, float y1)? GetBboxOverlap(
            (float x0, float y0, float x1, float y1) a,
            (float x0, float y0, float x1, float y1) b)
        {
            float oLeft = Math.Max(a.x0, b.x0);
            float oRight = Math.Min(a.x1, b.x1);
            float oBottom = Math.Min(a.y1, b.y1);
            float oTop = Math.Max(a.y0, b.y0);
            float oWidth = oRight - oLeft;
            float oHeight = oBottom - oTop;
            if (oHeight >= 0 && oWidth >= 0 && oHeight + oWidth > 0)
                return (oLeft, oTop, oRight, oBottom);
            return null;
        }

        // ── Text extraction (word-level) ────────────────────────────────

        /// <summary>PyMuPDF <c>WordExtractor.char_begins_new_word</c>.</summary>
        private static bool CharBeginsNewWord(
            Dictionary<string, object> prevChar,
            Dictionary<string, object> currChar,
            float xTolerance,
            float yTolerance,
            bool horizontalLtr = true,
            bool verticalTtb = true)
        {
            bool upright = currChar.TryGetValue("upright", out var uo) && (bool)uo;
            float x, y, ay, cy, ax, bx, cx;
            if (upright)
            {
                x = xTolerance;
                y = yTolerance;
                ay = F(prevChar, "top");
                cy = F(currChar, "top");
                if (horizontalLtr)
                {
                    ax = F(prevChar, "x0");
                    bx = F(prevChar, "x1");
                    cx = F(currChar, "x0");
                }
                else
                {
                    ax = -F(prevChar, "x1");
                    bx = -F(prevChar, "x0");
                    cx = -F(currChar, "x1");
                }
            }
            else
            {
                x = yTolerance;
                y = xTolerance;
                ay = F(prevChar, "x0");
                cy = F(currChar, "x0");
                if (verticalTtb)
                {
                    ax = F(prevChar, "top");
                    bx = F(prevChar, "bottom");
                    cx = F(currChar, "top");
                }
                else
                {
                    ax = -F(prevChar, "bottom");
                    bx = -F(prevChar, "top");
                    cx = -F(currChar, "bottom");
                }
            }
            return cx < ax || cx > bx + x || cy > ay + y;
        }

        /// <summary>PyMuPDF <c>WordExtractor.iter_sort_chars</c>.</summary>
        private static IEnumerable<Dictionary<string, object>> IterSortChars(
            List<Dictionary<string, object>> chars,
            float yTolerance,
            bool horizontalLtr = true,
            bool verticalTtb = true)
        {
            // for upright_cluster in cluster_objects(list(chars), upright_key, 0):
            var uprightClusters = ClusterObjects(chars, c => (c.TryGetValue("upright", out var u) && (bool)u) ? 1f : 0f, 0);
            foreach (var uprightCluster in uprightClusters)
            {
                bool upright = uprightCluster[0].TryGetValue("upright", out var uo) && (bool)uo;
                // cluster_key = "doctop" if upright else "x0"
                var subclusters = upright
                    ? ClusterObjects(uprightCluster, c => F(c, "doctop"), yTolerance)
                    : ClusterObjects(uprightCluster, c => F(c, "x0"), yTolerance);

                foreach (var sc in subclusters)
                {
                    // sort_key = "x0" if upright else "doctop"
                    var sorted = upright
                        ? sc.OrderBy(c => F(c, "x0")).ToList()
                        : sc.OrderBy(c => F(c, "doctop")).ToList();

                    bool forward = upright ? horizontalLtr : verticalTtb;
                    if (!forward)
                        sorted.Reverse();
                    foreach (var c in sorted)
                        yield return c;
                }
            }
        }

        /// <summary>
        /// Extract words from character dictionaries (PyMuPDF <c>WordExtractor.extract_words</c>).
        /// </summary>
        public static List<Dictionary<string, object>> ExtractWords(
            List<Dictionary<string, object>> chars,
            Dictionary<string, object> settings = null)
        {
            float xTol = TableConstants.DefaultXTolerance;
            float yTol = TableConstants.DefaultYTolerance;
            // horizontal_ltr=True, vertical_ttb=False  (WordExtractor defaults in table.py)
            bool horizontalLtr = true;
            bool verticalTtb = false;
            if (settings != null)
            {
                if (settings.TryGetValue("x_tolerance", out var xt))
                    xTol = Convert.ToSingle(xt);
                if (settings.TryGetValue("y_tolerance", out var yt))
                    yTol = Convert.ToSingle(yt);
                if (settings.TryGetValue("horizontal_ltr", out var hl))
                    horizontalLtr = Convert.ToBoolean(hl);
                if (settings.TryGetValue("vertical_ttb", out var vt))
                    verticalTtb = Convert.ToBoolean(vt);
            }

            if (chars.Count == 0)
                return new List<Dictionary<string, object>>();

            var words = new List<Dictionary<string, object>>();
            var current = new List<Dictionary<string, object>>();

            // ordered_chars = self.iter_sort_chars(chars)
            foreach (var ch in IterSortChars(chars, yTol, horizontalLtr, verticalTtb))
            {
                string text = (string)ch["text"];
                if (string.IsNullOrWhiteSpace(text))
                {
                    if (current.Count > 0)
                    {
                        words.Add(MergeWord(current));
                        current.Clear();
                    }
                    continue;
                }
                if (current.Count > 0
                    && CharBeginsNewWord(current[current.Count - 1], ch, xTol, yTol, horizontalLtr, verticalTtb))
                {
                    words.Add(MergeWord(current));
                    current.Clear();
                }
                current.Add(ch);
            }
            if (current.Count > 0)
                words.Add(MergeWord(current));
            return words;
        }

        private static Dictionary<string, object> MergeWord(
            List<Dictionary<string, object>> orderedChars)
        {
            var chars = orderedChars;
            if (!chars[0].TryGetValue("upright", out var upObj) || !(bool)upObj)
            {
                var matrix = chars[0]["matrix"] as float[];
                if (matrix != null && matrix.Length >= 2 && matrix[1] < 0)
                    chars = chars.AsEnumerable().Reverse().ToList();
            }

            var sb = new StringBuilder();
            foreach (var c in chars)
            {
                string t = (string)c["text"];
                if (TableConstants.Ligatures.ContainsKey(t))
                    sb.Append(TableConstants.Ligatures[t]);
                else
                    sb.Append(t);
            }
            float x0 = chars.Min(c => F(c, "x0"));
            float x1 = chars.Max(c => F(c, "x1"));
            float top = chars.Min(c => F(c, "top"));
            float bottom = chars.Max(c => F(c, "bottom"));
            float doctop = top + (F(chars[0], "doctop") - F(chars[0], "top"));
            bool upright = chars[0].TryGetValue("upright", out var uo) && (bool)uo;
            int rotation = 0;
            if (chars[0]["matrix"] is float[] m && m.Length >= 4)
            {
                if (!upright && m.Length >= 2 && m[1] < 0)
                    rotation = 270;
                if (m[0] < 0 && m[3] < 0)
                    rotation = 180;
                else if (m[1] > 0)
                    rotation = 90;
            }
            return new Dictionary<string, object>
            {
                ["text"] = sb.ToString(),
                ["x0"] = x0, ["x1"] = x1,
                ["top"] = top, ["bottom"] = bottom,
                ["doctop"] = doctop,
                ["upright"] = upright,
                ["direction"] = 1,
                ["rotation"] = rotation,
            };
        }

        private static readonly string[] WordExtractorKwargKeys =
        {
            "x_tolerance", "y_tolerance", "keep_blank_chars", "use_text_flow",
            "horizontal_ltr", "vertical_ttb", "extra_attrs", "split_at_punctuation", "expand_ligatures",
        };

        /// <summary>PyMuPDF <c>chars_to_textmap</c> (layout path; returns string like <c>as_string</c>).</summary>
        internal static string CharsToTextmapString(
            List<Dictionary<string, object>> chars,
            Dictionary<string, object> kwargs)
        {
            // kwargs.update({"presorted": True})
            var mapKw = new Dictionary<string, object>(kwargs) { ["presorted"] = true };
            var wordKw = new Dictionary<string, object>();
            foreach (var key in WordExtractorKwargKeys)
                if (mapKw.ContainsKey(key))
                    wordKw[key] = mapKw[key];
            // wordmap = extractor.extract_wordmap(chars); textmap = wordmap.to_textmap(...)
            // Simplified: layout uses same word clustering as non-layout extract_text.
            var layoutKw = new Dictionary<string, object>(mapKw);
            layoutKw.Remove("layout");
            return ExtractText(chars, layoutKw);
        }

        /// <summary>PyMuPDF <c>extract_text(chars, **kwargs)</c>.</summary>
        public static string ExtractText(
            List<Dictionary<string, object>> chars,
            Dictionary<string, object> kwargs = null)
        {
            // chars = to_list(chars)
            if (chars == null || chars.Count == 0)
                return "";

            kwargs ??= new Dictionary<string, object>();

            if (kwargs.TryGetValue("layout", out var layoutObj) && layoutObj != null)
            {
                // return chars_to_textmap(chars, **kwargs).as_string
                return CharsToTextmapString(chars, kwargs);
            }

            // y_tolerance = kwargs.get("y_tolerance", DEFAULT_Y_TOLERANCE)
            float yTolerance = kwargs.TryGetValue("y_tolerance", out var yt)
                ? Convert.ToSingle(yt)
                : TableConstants.DefaultYTolerance;

            // extractor = WordExtractor(**{k: kwargs[k] for k in WORD_EXTRACTOR_KWARGS if k in kwargs})
            var wordKw = new Dictionary<string, object>();
            foreach (var key in WordExtractorKwargKeys)
                if (kwargs.ContainsKey(key))
                    wordKw[key] = kwargs[key];

            // words = extractor.extract_words(chars)
            var words = ExtractWords(chars, wordKw);

            int rotation = 0;
            if (words.Count > 0)
                rotation = Convert.ToInt32(words[0]["rotation"]); // rotation cannot change within a cell
            else
                rotation = 0;

            if (rotation == 90)
            {
                // words.sort(key=lambda w: (w["x1"], -w["top"]))
                words = words.OrderBy(w => F(w, "x1")).ThenByDescending(w => F(w, "top")).ToList();
                // lines = " ".join([w["text"] for w in words])
                return string.Join(" ", words.Select(w => (string)w["text"]));
            }
            if (rotation == 270)
            {
                // words.sort(key=lambda w: (-w["x1"], w["top"]))
                words = words.OrderByDescending(w => F(w, "x1")).ThenBy(w => F(w, "top")).ToList();
                return string.Join(" ", words.Select(w => (string)w["text"]));
            }

            // lines = cluster_objects(words, itemgetter("doctop"), y_tolerance)
            var lineGroups = ClusterObjects(words, w => F(w, "doctop"), yTolerance);
            // lines = "\n".join(" ".join(word["text"] for word in line) for line in lines)
            string lines = string.Join("\n",
                lineGroups.Select(line => string.Join(" ", line.Select(w => (string)w["text"]))));
            if (rotation == 180) // needs extra treatment
            {
                // lines = "".join([(c if c != "\n" else " ") for c in reversed(lines)])
                var chars180 = lines.Select(c => c == '\n' ? ' ' : c).Reverse();
                lines = new string(chars180.ToArray());
            }
            return lines;
        }

        // ── Cell text extraction ────────────────────────────────────────

        /// <summary>PyMuPDF <c>table.py</c> <c>extract_cells</c> horizontal line check.</summary>
        private static bool ExtractCellsHorizontal(float[] dir)
        {
            if (dir == null || dir.Length < 2)
                return false;
            float dx = (float)Math.Round(dir[0], 4);
            float dy = (float)Math.Round(dir[1], 4);
            return (dx == 0 && dy == 1) || (dx == 1 && dy == 0);
        }

        /// <summary>
        /// Extract text from a single cell bbox using the TextPage,
        /// optionally with Markdown styling.
        /// Port of PyMuPDF <c>extract_cells</c> (<c>table.py</c>).
        /// </summary>
        public static string ExtractCells(
            TextPage textpage,
            (float x0, float y0, float x1, float y1) cell,
            bool markdown = false,
            PageInfo rawPageInfo = null)
        {
            if (textpage == null)
                return "";

            var text = new StringBuilder();
            var pageInfo = rawPageInfo ?? textpage.ExtractRAWDict(null, false);
            if (pageInfo?.Blocks == null)
                return "";

            var cellRect = new Rect(cell.x0, cell.y0, cell.x1, cell.y1);

            foreach (var block in pageInfo.Blocks)
            {
                if (block.Type != 0)
                    continue;
                var blockBbox = block.Bbox;
                if (blockBbox == null)
                    continue;
                if (blockBbox.X0 > cell.x1 || blockBbox.X1 < cell.x0
                    || blockBbox.Y0 > cell.y1 || blockBbox.Y1 < cell.y0)
                    continue;

                if (block.Lines == null)
                    continue;

                foreach (var line in block.Lines)
                {
                    var lbbox = line.Bbox;
                    if (lbbox == null)
                        continue;
                    if (lbbox.X0 > cell.x1 || lbbox.X1 < cell.x0
                        || lbbox.Y0 > cell.y1 || lbbox.Y1 < cell.y0)
                        continue;

                    if (text.Length > 0)
                        text.Append(markdown ? "<br>" : "\n");

                    var lineDir = line.Dir;
                    bool horizontal = lineDir != null
                        && (lineDir.X == 0 && lineDir.Y == 1 || lineDir.X == 1 && lineDir.Y == 0);

                    if (line.Spans == null)
                        continue;

                    foreach (var span in line.Spans)
                    {
                        var sbbox = span.Bbox;
                        if (sbbox == null)
                            continue;
                        if (sbbox.X0 > cell.x1 || sbbox.X1 < cell.x0
                            || sbbox.Y0 > cell.y1 || sbbox.Y1 < cell.y0)
                            continue;

                        var spanText = new StringBuilder();
                        if (span.Chars != null)
                        {
                            foreach (var ch in span.Chars)
                            {
                                if (ch.Bbox == null)
                                    continue;
                                var charRect = new Rect(ch.Bbox);
                                var intersection = charRect & cellRect;
                                if (intersection != null && !intersection.IsEmpty
                                    && intersection.Width * intersection.Height
                                        > 0.5 * charRect.Width * charRect.Height)
                                {
                                    spanText.Append(ch.C);
                                }
                                else if (TableConstants.WhiteSpaces.Contains(ch.C))
                                {
                                    spanText.Append(' ');
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(span.Text))
                        {
                            spanText.Append(span.Text);
                        }

                        if (spanText.Length == 0)
                            continue;

                        string spanStr = spanText.ToString();
                        if (!markdown)
                        {
                            text.Append(spanStr);
                            continue;
                        }

                        string prefix = "";
                        string suffix = "";
                        uint charFlags = span.CharFlags;
                        uint spanFlags = (uint)span.Flags;
                        if (horizontal && (charFlags & (uint)mupdf.mupdf.FZ_STEXT_STRIKEOUT) != 0)
                        {
                            prefix += "~~";
                            suffix = "~~" + suffix;
                        }
                        if ((charFlags & (uint)mupdf.mupdf.FZ_STEXT_BOLD) != 0)
                        {
                            prefix += "**";
                            suffix = "**" + suffix;
                        }
                        if ((spanFlags & Constants.TextFontItalic) != 0)
                        {
                            prefix += "_";
                            suffix = "_" + suffix;
                        }
                        if ((spanFlags & Constants.TextFontMonospaced) != 0)
                        {
                            prefix += "`";
                            suffix = "`" + suffix;
                        }

                        if (span.Chars != null && span.Chars.Count > 2)
                            spanStr = spanStr.TrimEnd();

                        int suffixLen = suffix.Length;
                        if (suffixLen > 0 && text.Length >= suffixLen
                            && text.ToString(text.Length - suffixLen, suffixLen) == suffix)
                        {
                            text.Remove(text.Length - suffixLen, suffixLen);
                            text.Append(spanStr);
                            text.Append(suffix);
                        }
                        else if (string.IsNullOrWhiteSpace(spanStr))
                            text.Append(' ');
                        else
                            text.Append(prefix).Append(spanStr).Append(suffix);
                    }
                }
            }

            return text.ToString().Trim();
        }

        // ── Page-level character / edge extraction ──────────────────────

        /// <summary>
        /// Fill the <paramref name="chars"/> list with per-character
        /// dictionaries extracted from the page's text.
        /// </summary>
        internal static TextPage MakeChars(
            Page page,
            List<Dictionary<string, object>> chars,
            TextPage textpage = null,
            Rect clip = null)
        {
            // """Extract text as "rawdict" to fill CHARS."""
            int pageNumber = page.Number + 1;
            float pageHeight = page.Height;
            float doctopBase = pageHeight * page.Number;
            var stp = textpage ?? CreateTextPage(page, clip);
            var raw = stp.ExtractRawDict(sort: false);
            if (raw == null || raw["blocks"] is not List<Dictionary<string, object>> blocks)
                return stp;
            // ctm = page.transformation_matrix
            var ctm = page.TransformationMatrix;

            // for block in blocks:
            foreach (var block in blocks)
            {
                if (block["lines"] is not List<Dictionary<string, object>> lines)
                    continue;
                // for line in block["lines"]:
                foreach (var line in lines)
                {
                    // ldir = line["dir"]  # = (cosine, sine) of angle
                    var ldir = (float[])line["dir"];
                    float dirX = (float)Math.Round(ldir[0], 4);
                    float dirY = (float)Math.Round(ldir[1], 4);
                    // matrix = pymupdf.Matrix(ldir[0], -ldir[1], ldir[1], ldir[0], 0, 0)
                    var matrix = new Matrix(dirX, -dirY, dirY, dirX, 0, 0);
                    // if ldir[1] == 0: upright = True else: upright = False
                    bool upright = dirY == 0;

                    // for span in sorted(line["spans"], key=lambda s: s["bbox"][0]):
                    var spans = ((List<Dictionary<string, object>>)line["spans"])
                        .OrderBy(s => ((float[])s["bbox"])[0])
                        .ToList();
                    foreach (var span in spans)
                    {
                        string fontname = span.TryGetValue("font", out var fn) ? fn?.ToString() ?? "" : "";
                        float fontsize = Convert.ToSingle(span["size"]);
                        bool spanBold = false;
                        if (span.TryGetValue("flags", out var fl))
                            spanBold |= (Convert.ToUInt32(fl) & Constants.TextFontBold) != 0;
                        if (span.TryGetValue("char_flags", out var cfl))
                            spanBold |= (Convert.ToUInt32(cfl) & 8) != 0;

                        if (span["chars"] is not List<Dictionary<string, object>> spanChars)
                            continue;
                        // for char in sorted(span["chars"], key=lambda c: c["bbox"][0]):
                        foreach (var ch in spanChars.OrderBy(c => ((float[])c["bbox"])[0]))
                        {
                            var cb = (float[])ch["bbox"];
                            // bbox = pymupdf.Rect(char["bbox"])
                            var bbox = new Rect(cb[0], cb[1], cb[2], cb[3]);
                            // bbox_ctm = bbox * ctm
                            var bboxCtm = bbox * ctm;
                            // origin = pymupdf.Point(char["origin"]) * ctm
                            var orig = (float[])ch["origin"];
                            var origin = new Point(orig[0], orig[1]) * ctm;
                            matrix.E = origin.X;
                            matrix.F = origin.Y;
                            string text = (string)ch["c"];

                            chars.Add(new Dictionary<string, object>
                            {
                                ["adv"] = upright ? bbox.Width : bbox.Height,
                                ["bottom"] = bbox.Y1,
                                ["doctop"] = bbox.Y0 + doctopBase,
                                ["fontname"] = fontname,
                                ["height"] = bbox.Height,
                                ["matrix"] = new float[]
                                {
                                    (float)matrix.A, (float)matrix.B, (float)matrix.C,
                                    (float)matrix.D, (float)matrix.E, (float)matrix.F,
                                },
                                ["object_type"] = "char",
                                ["page_number"] = pageNumber,
                                ["size"] = upright ? fontsize : bbox.Height,
                                ["bold"] = spanBold,
                                ["text"] = text,
                                ["top"] = bbox.Y0,
                                ["upright"] = upright,
                                ["width"] = bbox.Width,
                                ["x0"] = bbox.X0,
                                ["x1"] = bbox.X1,
                                ["y0"] = bboxCtm.Y0,
                                ["y1"] = bboxCtm.Y1,
                            });
                        }
                    }
                }
            }
            return stp;
        }

        /// <summary>PyMuPDF <c>table.py</c> <c>FLAGS</c> for <c>get_textpage</c> / <c>get_textbox</c>.</summary>
        internal static readonly int TableTextPageFlags =
            Constants.TextFlagsText
            | mupdf.mupdf.FZ_STEXT_COLLECT_STYLES
            | mupdf.mupdf.FZ_STEXT_ACCURATE_BBOXES
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP;

        /// <summary>
        /// Create a TextPage for the given page, optionally clipped to a rectangle.
        /// </summary>
        internal static TextPage CreateTextPage(Page page, Rect clip = null)
        {
            return page.GetTextPage(TableTextPageFlags, clip?.IRect);
        }

        /// <summary>
        /// Fill the <paramref name="edges"/> list with vector-graphic line
        /// dictionaries extracted from the page's drawings.
        /// Page.GetDrawingsDict() returns paths as List&lt;Dictionary&lt;string,object&gt;&gt;.
        /// Each path has "items" (list of drawing commands), "width", "rect", etc.
        /// Each item is a tuple-like list: ["l", p1, p2] or ["re", rect].
        /// </summary>
        internal static void MakeEdges(
            Page page,
            List<Dictionary<string, object>> edges,
            List<Dictionary<string, object>> chars,
            TableSettings tset,
            Rect clip = null,
            IList<Dictionary<string, object>> paths = null,
            IList<(Point p1, Point p2)> addLines = null,
            IList<Rect> addBoxes = null)
        {
            float snapX = tset.SnapXTolerance;
            float snapY = tset.SnapYTolerance;
            float minLength = tset.EdgeMinLength;
            float pageHeight = page.Height;
            float doctopBasis = page.Number * pageHeight;
            int pageNumber = page.Number + 1;
            var prect = page.Rect;
            if (page.Rotation == 90 || page.Rotation == 270)
            {
                float w = (float)prect.Width;
                float h = (float)prect.Height;
                prect = new Rect(0, 0, h, w);
            }
            var clipRect = clip != null ? new Rect(clip) : prect;

            bool IsParallel(float x1, float y1, float x2, float y2)
                => Math.Abs(x1 - x2) <= snapX || Math.Abs(y1 - y2) <= snapY;

            Dictionary<string, object> MakeLineDict(
                float lw, float p1x, float p1y, float p2x, float p2y)
            {
                if (!IsParallel(p1x, p1y, p2x, p2y)) return null;
                float x0 = Math.Min(p1x, p2x);
                float x1m = Math.Max(p1x, p2x);
                float y0 = Math.Min(p1y, p2y);
                float y1m = Math.Max(p1y, p2y);

                if (x0 > clipRect.X1 || x1m < clipRect.X0 ||
                    y0 > clipRect.Y1 || y1m < clipRect.Y0)
                    return null;

                x0 = Math.Max(x0, (float)clipRect.X0);
                x1m = Math.Min(x1m, (float)clipRect.X1);
                y0 = Math.Max(y0, (float)clipRect.Y0);
                y1m = Math.Min(y1m, (float)clipRect.Y1);

                float w = x1m - x0;
                float h = y1m - y0;
                if (w == 0 && h == 0) return null;

                return new Dictionary<string, object>
                {
                    ["x0"] = x0,
                    ["y0"] = pageHeight - y0,
                    ["x1"] = x1m,
                    ["y1"] = pageHeight - y1m,
                    ["width"] = w,
                    ["height"] = h,
                    ["linewidth"] = lw,
                    ["object_type"] = "line",
                    ["page_number"] = pageNumber,
                    ["top"] = y0,
                    ["bottom"] = y1m,
                    ["doctop"] = y0 + doctopBasis,
                    ["orientation"] = (h == 0) ? "h" : "v",
                };
            }

            bool linesStrict = tset.VerticalStrategy == "lines_strict"
                || tset.HorizontalStrategy == "lines_strict";

            bool AreNeighbors(Rect r1, Rect r2)
            {
                if ((r2.X0 - snapX <= r1.X0 && r1.X0 <= r2.X1 + snapX
                     || r2.X0 - snapX <= r1.X1 && r1.X1 <= r2.X1 + snapX)
                    && (r2.Y0 - snapY <= r1.Y0 && r1.Y0 <= r2.Y1 + snapY
                        || r2.Y0 - snapY <= r1.Y1 && r1.Y1 <= r2.Y1 + snapY))
                    return true;

                if ((r1.X0 - snapX <= r2.X0 && r2.X0 <= r1.X1 + snapX
                     || r1.X0 - snapX <= r2.X1 && r2.X1 <= r1.X1 + snapX)
                    && (r1.Y0 - snapY <= r2.Y0 && r2.Y0 <= r1.Y1 + snapY
                        || r1.Y0 - snapY <= r2.Y1 && r2.Y1 <= r1.Y1 + snapY))
                    return true;

                return false;
            }

            static bool TryPathDictRect(Dictionary<string, object> pathDict, out Rect rect)
            {
                if (pathDict != null && pathDict.TryGetValue("rect", out var r) && r is Rect pathRect)
                {
                    rect = pathRect;
                    return !pathRect.IsEmpty;
                }
                rect = null;
                return false;
            }

            (List<Rect> graphicsBboxes, List<Dictionary<string, object>> pathList) CleanGraphics(
                List<Dictionary<string, object>> allPaths)
            {
                var cleanedPaths = new List<Dictionary<string, object>>();
                foreach (var pathDict in allPaths)
                {
                    string pathType = pathDict.TryGetValue("type", out var ptObj) ? ptObj as string : null;
                    if (linesStrict && pathType == "f")
                    {
                        if (TryPathDictRect(pathDict, out var pathRect)
                            && pathRect.Width > snapX && pathRect.Height > snapY)
                            continue;
                    }
                    cleanedPaths.Add(pathDict);
                }

                var pendingRects = new List<Rect>();
                foreach (var path in cleanedPaths)
                {
                    if (TryPathDictRect(path, out var pathRect))
                        pendingRects.Add(pathRect);
                }
                pendingRects = pendingRects
                    .Distinct()
                    .OrderBy(r => r.Y1)
                    .ThenBy(r => r.X0)
                    .ToList();
                var joinedRects = new List<Rect>();

                while (pendingRects.Count > 0)
                {
                    var current = pendingRects[0];
                    bool repeat = true;
                    while (repeat)
                    {
                        repeat = false;
                        for (int i = pendingRects.Count - 1; i > 0; i--)
                        {
                            if (AreNeighbors(current, pendingRects[i]))
                            {
                                current = current | pendingRects[i];
                                pendingRects.RemoveAt(i);
                                repeat = true;
                            }
                        }
                    }

                    if (CharsInRect(chars,
                            ((float)current.X0, (float)current.Y0, (float)current.X1, (float)current.Y1)))
                        joinedRects.Add(current);
                    pendingRects.RemoveAt(0);
                }

                return (joinedRects, cleanedPaths);
            }

            var rawPathList = paths != null
                ? paths.ToList()
                : page.GetDrawingsDict() ?? new List<Dictionary<string, object>>();
            var (graphicsBboxes, pathList) = CleanGraphics(rawPathList);

            void AddLine(float lw, float p1x, float p1y, float p2x, float p2y)
            {
                var ld = MakeLineDict(lw, p1x, p1y, p2x, p2y);
                if (ld != null)
                    edges.Add(LineToEdge(ld));
            }

            void AddRectEdges(float lw, Rect rect)
            {
                rect = rect.Normalize();
                float rW = (float)rect.Width;
                float rH = (float)rect.Height;
                if (rW <= minLength && rW < rH)
                {
                    float mx = (float)((rect.X0 + rect.X1) / 2);
                    AddLine(lw, mx, (float)rect.Y0, mx, (float)rect.Y1);
                    return;
                }
                if (rH <= minLength && rH < rW)
                {
                    float my = (float)((rect.Y0 + rect.Y1) / 2);
                    AddLine(lw, (float)rect.X0, my, (float)rect.X1, my);
                    return;
                }
                AddLine(lw, (float)rect.TopLeft.X, (float)rect.TopLeft.Y, (float)rect.BottomLeft.X, (float)rect.BottomLeft.Y);
                AddLine(lw, (float)rect.BottomLeft.X, (float)rect.BottomLeft.Y, (float)rect.BottomRight.X, (float)rect.BottomRight.Y);
                AddLine(lw, (float)rect.BottomRight.X, (float)rect.BottomRight.Y, (float)rect.TopRight.X, (float)rect.TopRight.Y);
                AddLine(lw, (float)rect.TopRight.X, (float)rect.TopRight.Y, (float)rect.TopLeft.X, (float)rect.TopLeft.Y);
            }

            static bool TryPoint(object o, out float x, out float y)
            {
                x = y = 0;
                if (o is Point p)
                {
                    x = (float)p.X;
                    y = (float)p.Y;
                    return true;
                }
                if (o is float[] fa && fa.Length >= 2)
                {
                    x = fa[0];
                    y = fa[1];
                    return true;
                }
                if (Helpers.TryCoercePoint(o, out var pt))
                {
                    x = (float)pt.X;
                    y = (float)pt.Y;
                    return true;
                }
                return false;
            }

            static bool TryRect(object o, out Rect rect)
            {
                rect = null;
                if (o is Rect r)
                {
                    rect = r;
                    return true;
                }
                if (Helpers.TryCoerceRect(o, out var rc))
                {
                    rect = rc;
                    return true;
                }
                return false;
            }

            foreach (var pathDict in pathList)
            {
                float lw = pathDict.ContainsKey("width") ? Convert.ToSingle(pathDict["width"]) : 1f;
                if (!pathDict.ContainsKey("items")) continue;
                if (pathDict["items"] is not IEnumerable<object> itemsEnum) continue;
                var items = itemsEnum.ToList();

                bool closePath = pathDict.ContainsKey("closePath") && Convert.ToBoolean(pathDict["closePath"]);
                if (closePath && items.Count >= 2
                    && items[0] is object[] first && first.Length >= 3 && first[0] is string fc && fc == "l"
                    && items[items.Count - 1] is object[] last && last.Length >= 3 && last[0] is string lc && lc == "l"
                    && TryPoint(last[2], out float lx, out float ly)
                    && TryPoint(first[1], out float fx, out float fy))
                {
                    items.Add(new object[] { "l", new Point(lx, ly), new Point(fx, fy) });
                }

                foreach (var rawItem in items)
                {
                    if (rawItem is object[] oa && oa.Length > 0 && oa[0] is string cmd)
                    {
                        if (cmd == "l" && oa.Length >= 3
                            && TryPoint(oa[1], out float p1x, out float p1y)
                            && TryPoint(oa[2], out float p2x, out float p2y))
                        {
                            AddLine(lw, p1x, p1y, p2x, p2y);
                        }
                        else if (cmd == "re" && oa.Length >= 2 && TryRect(oa[1], out Rect r))
                        {
                            AddRectEdges(lw, r);
                        }
                        else if (cmd == "qu" && oa.Length >= 2 && oa[1] is Quad q)
                        {
                            AddLine(lw, (float)q.UL.X, (float)q.UL.Y, (float)q.LL.X, (float)q.LL.Y);
                            AddLine(lw, (float)q.LL.X, (float)q.LL.Y, (float)q.LR.X, (float)q.LR.Y);
                            AddLine(lw, (float)q.LR.X, (float)q.LR.Y, (float)q.UR.X, (float)q.UR.Y);
                            AddLine(lw, (float)q.UR.X, (float)q.UR.Y, (float)q.UL.X, (float)q.UL.Y);
                        }
                    }
                    else if (rawItem is Dictionary<string, object> itemDict)
                    {
                        string itemType = itemDict.ContainsKey("type") ? (string)itemDict["type"] : "";
                        if (itemType == "l" && itemDict.ContainsKey("p1") && itemDict.ContainsKey("p2"))
                        {
                            var p1 = (float[])itemDict["p1"];
                            var p2 = (float[])itemDict["p2"];
                            AddLine(lw, p1[0], p1[1], p2[0], p2[1]);
                        }
                        else if (itemType == "re" && itemDict.ContainsKey("rect"))
                        {
                            var r = (float[])itemDict["rect"];
                            AddRectEdges(lw, new Rect(r[0], r[1], r[2], r[3]));
                        }
                    }
                }
            }

            foreach (var bbox in graphicsBboxes)
            {
                AddLine(1, (float)bbox.X0, (float)bbox.Y0, (float)bbox.X1, (float)bbox.Y0);
                AddLine(1, (float)bbox.X0, (float)bbox.Y1, (float)bbox.X1, (float)bbox.Y1);
                AddLine(1, (float)bbox.X0, (float)bbox.Y0, (float)bbox.X0, (float)bbox.Y1);
                AddLine(1, (float)bbox.X1, (float)bbox.Y0, (float)bbox.X1, (float)bbox.Y1);
            }

            if (addLines == null)
                addLines = Array.Empty<(Point, Point)>();
            foreach (var (p1, p2) in addLines)
                AddLine(1, (float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);

            if (addBoxes == null)
                addBoxes = Array.Empty<Rect>();
            foreach (var box in addBoxes)
            {
                var r = box.Normalize();
                AddLine(1, (float)r.TopLeft.X, (float)r.TopLeft.Y, (float)r.BottomLeft.X, (float)r.BottomLeft.Y);
                AddLine(1, (float)r.BottomLeft.X, (float)r.BottomLeft.Y, (float)r.BottomRight.X, (float)r.BottomRight.Y);
                AddLine(1, (float)r.BottomRight.X, (float)r.BottomRight.Y, (float)r.TopRight.X, (float)r.TopRight.Y);
                AddLine(1, (float)r.TopRight.X, (float)r.TopRight.Y, (float)r.TopLeft.X, (float)r.TopLeft.Y);
            }
        }

        /// <summary>PyMuPDF <c>table.page_rotation_set0</c>.</summary>
        internal static (Page page, int xref, int rot, Rect mediabox) PageRotationSet0(Page page)
        {
            var mediabox = new Rect(page.MediaBox);
            int rot = page.Rotation;
            var mb = new Rect(page.MediaBox);
            Matrix mat0;
            if (rot == 90)
                mat0 = new Matrix(1, 0, 0, 1, mb.Y1 - mb.X1 - mb.X0 - mb.Y0, 0);
            else if (rot == 270)
                mat0 = new Matrix(1, 0, 0, 1, 0, mb.X1 - mb.Y1 - mb.Y0 - mb.X0);
            else
                mat0 = new Matrix(1, 0, 0, 1, -2 * mb.X0, -2 * mb.Y0);

            var mat = mat0 * page.DerotationMatrix;
            string cmd = Helpers.FormatPdfReals(mat.A, mat.B, mat.C, mat.D, mat.E, mat.F) + " cm ";
            int xref = Tools.InsertContents(page, cmd, overlay: false);

            if (rot == 90 || rot == 270)
                page.SetMediaBox(new Rect(mb.Y0, mb.X0, mb.Y1, mb.X1));

            page.SetRotation(0);
            var doc = page.Parent;
            return (doc[page.Number], xref, rot, mediabox);
        }

        /// <summary>PyMuPDF <c>table.page_rotation_reset</c>.</summary>
        internal static Page PageRotationReset(Page page, int xref, int rot, Rect mediabox)
        {
            var doc = page.Parent;
            doc.UpdateStream(xref, new byte[] { 0x20 }, 0, 0);
            page.SetMediaBox(mediabox);
            page.SetRotation(rot);
            return doc[page.Number];
        }

        /// <summary>Layout boxes tagged <c>table</c> (PyMuPDF <c>find_tables</c>).</summary>
        internal static List<Rect> LayoutTableBoxes(object layoutInformation)
        {
            var boxes = new List<Rect>();
            if (layoutInformation is IEnumerable<object> rows)
            {
                foreach (var row in rows)
                {
                    if (row is object[] arr && arr.Length >= 5 && arr[arr.Length - 1]?.ToString() == "table")
                        boxes.Add(new Rect(
                            Convert.ToSingle(arr[0]),
                            Convert.ToSingle(arr[1]),
                            Convert.ToSingle(arr[2]),
                            Convert.ToSingle(arr[3])));
                }
            }
            return boxes;
        }

        /// <summary>PyMuPDF <c>make_table_from_bbox</c>.</summary>
        internal static List<(float x0, float y0, float x1, float y1)> MakeTableFromBbox(
            TextPage textpage,
            List<Rect> wordRects,
            Rect rect)
        {
            var cells = new List<(float x0, float y0, float x1, float y1)>();
            try
            {
                using var block = textpage.NativeStextPage.fz_find_table_within_bounds(rect.ToFzRect());
                if (block?.m_internal == null
                    || block.m_internal.type != mupdf.mupdf.FZ_STEXT_BLOCK_GRID)
                    return cells;
                // Grid block detected; full cell decomposition requires extra.make_table_dict (not bound in .NET).
            }
            catch
            {
                // match Python: skip on failure
            }
            return cells;
        }

        // ── Convenience accessor ────────────────────────────────────────

        /// <summary>
        /// Safely read a float value from a string-keyed dictionary.
        /// </summary>
        internal static float F(Dictionary<string, object> d, string key)
        {
            return Convert.ToSingle(d[key]);
        }
    }

    /// <summary>
    /// Legacy entry point for table detection (MuPDF.NET <c>TableFinderHelper</c>).
    /// Forwards to <see cref="TableHelpers.FindTables"/>.
    /// </summary>
    public static class TableFinderHelper
    {
        public static TableFinder FindTables(
            Page page,
            Rect clip = null,
            string vertical_strategy = "lines",
            string horizontal_strategy = "lines",
            List<object> vertical_lines = null,
            List<object> horizontal_lines = null,
            float snap_tolerance = TableConstants.DefaultSnapTolerance,
            float? snap_x_tolerance = null,
            float? snap_y_tolerance = null,
            float join_tolerance = TableConstants.DefaultJoinTolerance,
            float? join_x_tolerance = null,
            float? join_y_tolerance = null,
            float edge_min_length = 3.0f,
            float min_words_vertical = TableConstants.DefaultMinWordsVertical,
            float min_words_horizontal = TableConstants.DefaultMinWordsHorizontal,
            float intersection_tolerance = 3.0f,
            float? intersection_x_tolerance = null,
            float? intersection_y_tolerance = null,
            float text_tolerance = 3.0f,
            float text_x_tolerance = 3.0f,
            float text_y_tolerance = 3.0f,
            string strategy = null,
            List<Tuple<Point, Point>> add_lines = null,
            List<Rect> add_boxes = null,
            List<PathInfo> paths = null)
        {
            List<float> verticalCoords = null;
            if (vertical_lines != null)
            {
                verticalCoords = new List<float>(vertical_lines.Count);
                foreach (var line in vertical_lines)
                {
                    if (line is float f)
                        verticalCoords.Add(f);
                    else if (line is Edge e)
                        verticalCoords.Add(e.x0);
                    else if (line is Dictionary<string, object> d && d.TryGetValue("x0", out var x0))
                        verticalCoords.Add(Convert.ToSingle(x0));
                }
            }

            List<float> horizontalCoords = null;
            if (horizontal_lines != null)
            {
                horizontalCoords = new List<float>(horizontal_lines.Count);
                foreach (var line in horizontal_lines)
                {
                    if (line is float f)
                        horizontalCoords.Add(f);
                    else if (line is Edge e)
                        horizontalCoords.Add(e.y0);
                    else if (line is Dictionary<string, object> d && d.TryGetValue("top", out var top))
                        horizontalCoords.Add(Convert.ToSingle(top));
                }
            }

            IList<(Point p1, Point p2)> addLines = add_lines?
                .Select(t => (t.Item1, t.Item2))
                .ToList();

            return TableHelpers.FindTables(
                page,
                clip: clip,
                verticalStrategy: vertical_strategy,
                horizontalStrategy: horizontal_strategy,
                verticalLines: verticalCoords,
                horizontalLines: horizontalCoords,
                snapTolerance: snap_tolerance,
                snapXTolerance: snap_x_tolerance,
                snapYTolerance: snap_y_tolerance,
                joinTolerance: join_tolerance,
                joinXTolerance: join_x_tolerance,
                joinYTolerance: join_y_tolerance,
                edgeMinLength: edge_min_length,
                minWordsVertical: min_words_vertical,
                minWordsHorizontal: min_words_horizontal,
                intersectionTolerance: intersection_tolerance,
                intersectionXTolerance: intersection_x_tolerance,
                intersectionYTolerance: intersection_y_tolerance,
                textTolerance: text_tolerance,
                textXTolerance: text_x_tolerance,
                textYTolerance: text_y_tolerance,
                strategy: strategy,
                addLines: addLines,
                addBoxes: add_boxes,
                paths: null);
        }
    }

}
