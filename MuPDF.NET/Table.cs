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
    /// Describes the detected header of a <see cref="Table"/>.
    /// </summary>
    public class TableHeader
    {
        /// <summary>Bounding box of the header region.</summary>
        public (float x0, float y0, float x1, float y1) Bbox { get; }

        /// <summary>Per-column header cell bounding boxes (null = spanned).</summary>
        public List<(float x0, float y0, float x1, float y1)?> Cells { get; }

        /// <summary>Column names extracted from the header cells (null = spanned cell).</summary>
        public List<string?> Names { get; }

        /// <summary>
        /// True when the header sits above the first data row (external),
        /// false when the first data row <em>is</em> the header.
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
    }

    // ────────────────────────────────────────────────────────────────────
    // Table
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Represents a single table detected on a PDF page, with methods to
    /// extract its content as text or Markdown.
    /// </summary>
    public class Table
    {
        internal Page Page;
        internal TextPage TextPage;

        /// <summary>All cell bounding boxes that compose this table.</summary>
        public List<(float x0, float y0, float x1, float y1)> Cells { get; set; }

        /// <summary>Detected header for this table.</summary>
        public TableHeader Header { get; internal set; }

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

        /// <summary>Number of rows.</summary>
        public int RowCount => Rows.Count;

        /// <summary>Number of columns (widest row).</summary>
        public int ColCount => Rows.Max(r => r.Cells.Count);

        internal Table(Page page, List<(float x0, float y0, float x1, float y1)> cells)
        {
            Page = page;
            TextPage = null;
            Cells = cells;
            Header = GetHeader();
        }

        /// <summary>
        /// Extract text from every cell using the pdfplumber-style
        /// character-clustering algorithm.
        /// </summary>
        /// <param name="markdown">
        /// When true, cell text is extracted with Markdown styling
        /// (bold, italic, strikeout, monospace).
        /// </param>
        /// <returns>
        /// A list of rows, each row being a list of cell-text strings.
        /// A null entry means the cell was spanned (no bbox).
        /// </returns>
        public List<List<string?>> Extract(bool markdown = false)
        {
            var tableArr = new List<List<string?>>();
            foreach (var row in Rows)
            {
                var arr = new List<string?>();
                foreach (var cell in row.Cells)
                {
                    if (cell == null)
                    {
                        arr.Add(null);
                    }
                    else
                    {
                        arr.Add(TextPage != null
                            ? TableHelpers.ExtractCells(TextPage, cell.Value, markdown)
                            : "");
                    }
                }
                tableArr.Add(arr);
            }
            return tableArr;
        }

        /// <summary>
        /// Render the table as a GitHub-flavoured Markdown string.
        /// </summary>
        /// <param name="clean">
        /// When true, HTML-escape characters that would interfere with Markdown.
        /// </param>
        /// <param name="fillEmpty">
        /// When true, null cells are filled from neighbours to approximate spans.
        /// </param>
        public string ToMarkdown(bool clean = false, bool fillEmpty = true)
        {
            int rows = RowCount;
            int cols = ColCount;

            var cellBoxes = Rows.Select(r => r.Cells.ToList()).ToList();

            var cells = new string[rows][];
            for (int i = 0; i < rows; i++)
                cells[i] = new string[cols];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cellBoxes[i].Count; j++)
                    if (cellBoxes[i][j] != null)
                        cells[i][j] = TableHelpers.ExtractCells(
                            TextPage, cellBoxes[i][j].Value, markdown: true);

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

        // ── Header detection (PyMuPDF extension) ────────────────────────

        /// <summary>
        /// Identify the table header (PyMuPDF extension).
        /// Starting from the first row, determine whether it qualifies as the
        /// header. A one-line table or single-column table always uses the first
        /// row. Returns null if the table has no rows.
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

        internal List<Dictionary<string, object>> Edges;
        internal List<Dictionary<string, object>> Chars;

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

            TableHelpers.MakeChars(page, Chars);
            TableHelpers.MakeEdges(page, Edges, Chars, Settings);

            var resolvedEdges = GetEdges();

            var intersections = TableHelpers.EdgesToIntersections(
                resolvedEdges,
                Settings.IntersectionXTolerance,
                Settings.IntersectionYTolerance);

            var cells = TableHelpers.IntersectionsToCells(intersections);

            var cellGroups = TableHelpers.CellsToTables(page, cells, Chars);

            Tables = cellGroups.Select(g => new Table(page, g)).ToList();
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
    public static class TableHelpers
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
                rect.y0 <= F(c, "y0") && rect.y1 >= F(c, "y1"));
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

            var points = intersections.Keys.OrderBy(p => p.x).ThenBy(p => p.y).ToList();
            int n = points.Count;
            var cells = new List<(float x0, float y0, float x1, float y1)>();

            for (int i = 0; i < n; i++)
            {
                var pt = points[i];
                var rest = points.Skip(i + 1).ToList();
                var below = rest.Where(p => p.x == pt.x && p.y > pt.y)
                    .OrderBy(p => p.y).ToList();
                var right = rest.Where(p => p.y == pt.y && p.x > pt.x)
                    .OrderBy(p => p.x).ToList();

                foreach (var belowPt in below)
                {
                    if (!EdgeConnects(pt, belowPt)) continue;
                    foreach (var rightPt in right)
                    {
                        if (!EdgeConnects(pt, rightPt)) continue;
                        var bottomRight = (rightPt.x, belowPt.y);
                        if (intersections.ContainsKey(bottomRight)
                            && EdgeConnects(bottomRight, rightPt)
                            && EdgeConnects(bottomRight, belowPt))
                        {
                            cells.Add((pt.x, pt.y, bottomRight.x, bottomRight.y));
                            goto nextBelow;
                        }
                    }
                    nextBelow:;
                }
            }
            return cells;
        }

        // ── Cells → Tables ──────────────────────────────────────────────

        /// <summary>
        /// Group cells into contiguous tables based on shared corners.
        /// </summary>
        public static List<List<(float x0, float y0, float x1, float y1)>> CellsToTables(
            Page page,
            List<(float x0, float y0, float x1, float y1)> cells,
            List<Dictionary<string, object>> chars)
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
                for (int i = remaining.Count - 1; i >= 0; i--)
                {
                    var cell = remaining[i];
                    var corners = BboxToCorners(cell);
                    if (currentCells.Count == 0)
                    {
                        foreach (var c in corners) currentCorners.Add(c);
                        currentCells.Add(cell);
                        remaining.RemoveAt(i);
                    }
                    else
                    {
                        int cornerCount = corners.Count(c => currentCorners.Contains(c));
                        if (cornerCount > 0)
                        {
                            foreach (var c in corners) currentCorners.Add(c);
                            currentCells.Add(cell);
                            remaining.RemoveAt(i);
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
                if (x1Vals.Count < 2 || x0Vals.Count < 2)
                {
                    tables.RemoveAt(i);
                    continue;
                }
                float rx0 = tables[i].Min(c => c.x0);
                float ry0 = tables[i].Min(c => c.y0);
                float rx1 = tables[i].Max(c => c.x1);
                float ry1 = tables[i].Max(c => c.y1);
                if (!CharsInRect(chars, (rx0, ry0, rx1, ry1)))
                    tables.RemoveAt(i);
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

        /// <summary>
        /// Extract words from character dictionaries (simplified version
        /// of pdfplumber's WordExtractor).
        /// </summary>
        public static List<Dictionary<string, object>> ExtractWords(
            List<Dictionary<string, object>> chars,
            Dictionary<string, object> settings = null)
        {
            float xTol = TableConstants.DefaultXTolerance;
            float yTol = TableConstants.DefaultYTolerance;
            if (settings != null)
            {
                if (settings.ContainsKey("x_tolerance"))
                    xTol = Convert.ToSingle(settings["x_tolerance"]);
                if (settings.ContainsKey("y_tolerance"))
                    yTol = Convert.ToSingle(settings["y_tolerance"]);
            }

            if (chars.Count == 0)
                return new List<Dictionary<string, object>>();

            var uprightChars = chars.Where(c => c.ContainsKey("upright") && (bool)c["upright"]).ToList();
            var lines = ClusterObjects(uprightChars, c => F(c, "doctop"), yTol);

            var words = new List<Dictionary<string, object>>();
            foreach (var lineChars in lines)
            {
                var sorted = lineChars.OrderBy(c => F(c, "x0")).ToList();
                var current = new List<Dictionary<string, object>>();
                foreach (var ch in sorted)
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
                    if (current.Count > 0)
                    {
                        float prevX1 = F(current[current.Count - 1], "x1");
                        if (F(ch, "x0") > prevX1 + xTol)
                        {
                            words.Add(MergeWord(current));
                            current.Clear();
                        }
                    }
                    current.Add(ch);
                }
                if (current.Count > 0)
                    words.Add(MergeWord(current));
            }
            return words;
        }

        private static Dictionary<string, object> MergeWord(
            List<Dictionary<string, object>> chars)
        {
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
            return new Dictionary<string, object>
            {
                ["text"] = sb.ToString(),
                ["x0"] = x0, ["x1"] = x1,
                ["top"] = top, ["bottom"] = bottom,
                ["doctop"] = doctop,
                ["upright"] = true,
                ["direction"] = 1,
                ["rotation"] = 0,
            };
        }

        // ── Cell text extraction ────────────────────────────────────────

        /// <summary>
        /// Extract text from a single cell bbox using the TextPage,
        /// optionally with Markdown styling.
        /// </summary>
        public static string ExtractCells(
            TextPage textpage,
            (float x0, float y0, float x1, float y1) cell,
            bool markdown = false)
        {
            if (textpage == null) return "";

            var sb = new StringBuilder();
            var stp = textpage.NativeStextPage.m_internal;
            var block = stp.first_block;

            while (block != null)
            {
                if (block.type != 0) { block = block.next; continue; }
                float bx0 = block.bbox.x0, by0 = block.bbox.y0;
                float bx1 = block.bbox.x1, by1 = block.bbox.y1;
                if (bx0 > cell.x1 || bx1 < cell.x0 || by0 > cell.y1 || by1 < cell.y0)
                { block = block.next; continue; }

                {
                    var line = new mupdf.FzStextBlock(block).begin().m_internal;
                    while (line != null)
                    {
                        float lx0 = line.bbox.x0, ly0 = line.bbox.y0;
                        float lx1 = line.bbox.x1, ly1 = line.bbox.y1;
                        if (lx0 > cell.x1 || lx1 < cell.x0 || ly0 > cell.y1 || ly1 < cell.y0)
                        { line = line.next; continue; }

                        if (sb.Length > 0)
                            sb.Append(markdown ? "<br>" : "\n");

                        var ch = line.first_char;
                        while (ch != null)
                        {
                            float cx0 = ch.quad.ul.x, cy0 = ch.quad.ul.y;
                            float cx1 = ch.quad.lr.x, cy1 = ch.quad.lr.y;

                            float overlapX0 = Math.Max(cx0, cell.x0);
                            float overlapY0 = Math.Max(cy0, cell.y0);
                            float overlapX1 = Math.Min(cx1, cell.x1);
                            float overlapY1 = Math.Min(cy1, cell.y1);
                            float overlapArea = Math.Max(0, overlapX1 - overlapX0) *
                                                Math.Max(0, overlapY1 - overlapY0);
                            float charArea = Math.Max(0.0001f, (cx1 - cx0) * (cy1 - cy0));

                            if (overlapArea > 0.5f * charArea)
                                sb.Append((char)ch.c);
                            else if (ch.c == 32 || ch.c == 9)
                                sb.Append(' ');

                            ch = ch.next;
                        }
                        line = line.next;
                    }
                }
                block = block.next;
            }
            return sb.ToString().Trim();
        }

        // ── Page-level character / edge extraction ──────────────────────

        /// <summary>
        /// Fill the <paramref name="chars"/> list with per-character
        /// dictionaries extracted from the page's text.
        /// </summary>
        internal static void MakeChars(
            Page page,
            List<Dictionary<string, object>> chars,
            Rect clip = null)
        {
            int pageNumber = page.Number + 1;
            float pageHeight = page.Height;
            float doctopBase = pageHeight * page.Number;
            var stp = CreateTextPage(page, clip);
            var block = stp.NativeStextPage.m_internal.first_block;

            while (block != null)
            {
                if (block.type != 0) { block = block.next; continue; }
                {
                    var line = new mupdf.FzStextBlock(block).begin().m_internal;
                    while (line != null)
                    {
                        bool upright = Math.Abs(line.dir.x - 1.0f) < 0.01f
                            || Math.Abs(line.dir.y - 1.0f) < 0.01f;
                        float dirX = (float)Math.Round(line.dir.x, 4);
                        float dirY = (float)Math.Round(line.dir.y, 4);

                        var ch = line.first_char;
                        while (ch != null)
                        {
                            float x0 = ch.quad.ul.x, y0q = ch.quad.ul.y;
                            float x1 = ch.quad.lr.x, y1q = ch.quad.lr.y;
                            float bTop = Math.Min(y0q, y1q);
                            float bBottom = Math.Max(y0q, y1q);
                            float width = x1 - x0;
                            float height = bBottom - bTop;
                            float adv = upright ? width : height;

                            var charDict = new Dictionary<string, object>
                            {
                                ["adv"] = adv,
                                ["bottom"] = bBottom,
                                ["doctop"] = bTop + doctopBase,
                                ["fontname"] = "",
                                ["height"] = height,
                                ["matrix"] = new float[] { dirX, -dirY, dirY, dirX, x0, y0q },
                                ["object_type"] = "char",
                                ["page_number"] = pageNumber,
                                ["size"] = upright ? ch.size : height,
                                ["bold"] = false,
                                ["text"] = ((char)ch.c).ToString(),
                                ["top"] = bTop,
                                ["upright"] = upright,
                                ["width"] = width,
                                ["x0"] = x0,
                                ["x1"] = x1,
                                ["y0"] = pageHeight - bBottom,
                                ["y1"] = pageHeight - bTop,
                            };
                            chars.Add(charDict);
                            ch = ch.next;
                        }
                        line = line.next;
                    }
                }
                block = block.next;
            }
        }

        /// <summary>
        /// Create a TextPage for the given page, optionally clipped to a rectangle.
        /// </summary>
        internal static TextPage CreateTextPage(Page page, Rect clip = null)
        {
            return page.GetTextPage(0, clip?.IRect);
        }

        /// <summary>
        /// Fill the <paramref name="edges"/> list with vector-graphic line
        /// dictionaries extracted from the page's drawings.
        /// Page.GetDrawings() returns paths as List&lt;Dictionary&lt;string,object&gt;&gt;.
        /// Each path has "items" (list of drawing commands), "width", "rect", etc.
        /// Each item is a tuple-like list: ["l", p1, p2] or ["re", rect].
        /// </summary>
        internal static void MakeEdges(
            Page page,
            List<Dictionary<string, object>> edges,
            List<Dictionary<string, object>> chars,
            TableSettings tset,
            Rect clip = null)
        {
            float snapX = tset.SnapXTolerance;
            float snapY = tset.SnapYTolerance;
            float minLength = tset.EdgeMinLength;
            float pageHeight = page.Height;
            float doctopBasis = page.Number * pageHeight;
            int pageNumber = page.Number + 1;
            var prect = page.Rect;
            var clipRect = clip ?? prect;

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

            var paths = page.GetDrawings();
            if (paths == null || paths.Count == 0) return;

            foreach (var pathDict in paths)
            {
                float lw = pathDict.ContainsKey("width") ? Convert.ToSingle(pathDict["width"]) : 1f;
                if (!pathDict.ContainsKey("items")) continue;
                var items = pathDict["items"] as IEnumerable<object>;
                if (items == null) continue;

                var pathRect = pathDict.ContainsKey("rect") ? pathDict["rect"] : null;
                bool closePath = pathDict.ContainsKey("closePath") && Convert.ToBoolean(pathDict["closePath"]);

                foreach (var rawItem in items)
                {
                    if (rawItem is Dictionary<string, object> itemDict)
                    {
                        string itemType = itemDict.ContainsKey("type") ? (string)itemDict["type"] : "";
                        if (itemType == "l" && itemDict.ContainsKey("p1") && itemDict.ContainsKey("p2"))
                        {
                            var p1 = (float[])itemDict["p1"];
                            var p2 = (float[])itemDict["p2"];
                            var ld = MakeLineDict(lw, p1[0], p1[1], p2[0], p2[1]);
                            if (ld != null) edges.Add(LineToEdge(ld));
                        }
                        else if (itemType == "re" && itemDict.ContainsKey("rect"))
                        {
                            var r = (float[])itemDict["rect"];
                            float rX0 = r[0], rY0 = r[1], rX1 = r[2], rY1 = r[3];
                            float rW = Math.Abs(rX1 - rX0), rH = Math.Abs(rY1 - rY0);
                            if (rW <= minLength && rW < rH)
                            {
                                float mx = (rX0 + rX1) / 2f;
                                var ld = MakeLineDict(lw, mx, rY0, mx, rY1);
                                if (ld != null) edges.Add(LineToEdge(ld));
                                continue;
                            }
                            if (rH <= minLength && rH < rW)
                            {
                                float my = (rY0 + rY1) / 2f;
                                var ld = MakeLineDict(lw, rX0, my, rX1, my);
                                if (ld != null) edges.Add(LineToEdge(ld));
                                continue;
                            }
                            var l1 = MakeLineDict(lw, rX0, rY0, rX0, rY1);
                            if (l1 != null) edges.Add(LineToEdge(l1));
                            var l2 = MakeLineDict(lw, rX0, rY1, rX1, rY1);
                            if (l2 != null) edges.Add(LineToEdge(l2));
                            var l3 = MakeLineDict(lw, rX1, rY1, rX1, rY0);
                            if (l3 != null) edges.Add(LineToEdge(l3));
                            var l4 = MakeLineDict(lw, rX1, rY0, rX0, rY0);
                            if (l4 != null) edges.Add(LineToEdge(l4));
                        }
                    }
                }
            }
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

}
