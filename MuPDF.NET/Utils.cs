using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MuPDF.NET
{
    /// <summary>
    /// Miscellaneous low-level helpers (legacy MuPDF.NET <c>Utils</c> / PyMuPDF <c>utils</c>).
    /// </summary>
    /// <remarks>
    /// <para>Includes PDF string encoding, paper sizes, text-extraction helpers, quad recovery, colors,
    /// image metadata, tables, barcodes, and related helpers used across the library.</para>
    /// <para>Public members use PascalCase; <c>internal</c> snake_case aliases match Python for same-assembly tests.</para>
    /// <para><b>Legacy readthedocs <c>Utils</c> vs <see cref="Tools"/> (PyMuPDF <c>TOOLS</c>)</b> — prefer <see cref="Tools"/>
    /// for new code. These legacy <c>Utils</c> entry points remain as thin forwards:</para>
    /// <list type="table">
    /// <listheader><term>Legacy <c>Utils</c></term><description>Use instead (<see cref="Tools"/>)</description></listheader>
    /// <item><term><see cref="GetId"/></term><description><see cref="Tools.GenId"/></description></item>
    /// <item><term><see cref="GetAllContents"/></term><description><see cref="Tools.GetAllContents"/></description></item>
    /// <item><term><see cref="InsertContents"/></term><description><see cref="Tools.InsertContents"/></description></item>
    /// </list>
    /// <para>Other runtime tuning from PyMuPDF <c>TOOLS</c> (not on legacy <c>Utils</c>):</para>
    /// <list type="bullet">
    /// <item><description><see cref="Tools.MupdfWarnings"/> / <see cref="Tools.ResetMupdfWarnings"/></description></item>
    /// <item><description><see cref="Tools.SetAaLevel"/> / <see cref="Tools.ShowAaLevel"/></description></item>
    /// <item><description><see cref="Tools.SetSmallGlyphHeights"/> (affects <see cref="RecoverQuad"/> family when enabled)</description></item>
    /// <item><description><see cref="Tools.GlyphCacheEmpty"/>, <see cref="Tools.StoreShrink"/>, <see cref="Tools.SetFontWidth"/></description></item>
    /// </list>
    /// <para>Low-level <c>JM_*</c> implementations live on <c>Helpers</c> (internal). PDF merge/link helpers are on
    /// <see cref="MergeRange"/> and <see cref="DoLinks"/> in <c>Utils.LegacyApi</c>.</para>
    /// </remarks>
    public static partial class Utils
    {
        /// <summary>PyMuPDF <c>pymupdf.pymupdf_version</c> / MuPDF.NET <c>Utils.pymupdf_version</c>.</summary>
        public static string pymupdf_version = "1.27.2.2";

        /// <summary>PyMuPDF <c>pymupdf.VersionBind</c> / MuPDF.NET <c>Utils.VersionBind</c>.</summary>
        public static string VersionBind = "1.27.2.2";

        public static (string, string) VERSION = ("1.26.1", "3.2.8-rc.6");

        public static int FZ_MIN_INF_RECT = (int)(-0x80000000);

        public static int FZ_MAX_INF_RECT = (int)0x7fffff80;

        public static float FLT_EPSILON = 1e-5f;

        /// <summary>MuPDF.NET <c>Utils.MUPDF_WARNINGS_STORE</c>.</summary>
        public static List<string> MUPDF_WARNINGS_STORE => Helpers.JM_mupdf_warnings_store;

        public static bool IsInitialized = false;

        /// <summary>RGB colorspace type id for <see cref="ColorSpace"/> / <see cref="Colorspace"/> (value 1).</summary>
        public static int CS_RGB = 1;

        /// <summary>GRAY colorspace type id for <see cref="ColorSpace"/> / <see cref="Colorspace"/> (value 2).</summary>
        public static int CS_GRAY = 2;

        /// <summary>CMYK colorspace type id for <see cref="ColorSpace"/> / <see cref="Colorspace"/> (value 3).</summary>
        public static int CS_CMYK = 3;

        /// <summary>
        /// Global lock for thread-safe access to native MuPDF library.
        /// MuPDF is not thread-safe, so all P/Invoke calls must be synchronized with this lock.
        /// </summary>
        public static readonly object MuPDFLock = new object();

        public static string ANNOT_ID_STEM = "fitz";

        /// <summary>MuPDF.NET / PyMuPDF <c>TOOLS.set_annot_stem</c>: get or set annotation /NM name prefix.</summary>
        public static string SetAnnotStem(string stem = null)
        {
            if (stem == null)
                return ANNOT_ID_STEM;
            int len = stem.Length;
            if (len > 50)
                len = 50;
            ANNOT_ID_STEM = stem.Substring(0, len);
            return ANNOT_ID_STEM;
        }

        public static int SigFlag_SignaturesExist = 1;
        public static int SigFlag_AppendOnly = 2;

        public static bool SmallGlyphHeights = false;

        public static int UNIQUE_ID = 0;

        public static Dictionary<string, int> AdobeUnicodes = new Dictionary<string, int>();

        public static Dictionary<int, string> AdobeGlyphs = new Dictionary<int, string>();

        /// <summary>
        /// Copy of the <c>TESSDATA_PREFIX</c> environment variable; <c>null</c> when Tesseract language data is unavailable.
        /// </summary>
        /// <remarks>OCR entry points check this before invoking MuPDF OCR to avoid verbose warnings.</remarks>
        public static string? TESSDATA_PREFIX = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");

        public static int trace_device_FILL_PATH = 1;
        public static int trace_device_STROKE_PATH = 2;
        public static int trace_device_CLIP_PATH = 3;
        public static int trace_device_CLIP_STROKE_PATH = 4;

        // ─── decimal/string Functions ─────────────────────────────────────────────

        /// <summary>
        /// Converts a float to string with dot as decimal separator, without scientific notation.
        /// </summary>
        public static string FloatToString(float value)
        {
            return value.ToString("0.#######", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a double to string with dot as decimal separator, without scientific notation.
        /// </summary>
        public static string DoubleToString(double value)
        {
            return value.ToString("0.#################", CultureInfo.InvariantCulture);
        }

        // ─── Color Functions ─────────────────────────────────────────────

        /// <summary>
        /// Returns a list of upper-case colour names from the wx colour database.
        /// </summary>
        public static List<string> GetColorList()
        {
            return WxColors.ColorList.Select(c => c.Name).ToList();
        }

        /// <summary>
        /// Returns a list of (name, red, green, blue) tuples where RGB values
        /// are integers in the range 0–255.
        /// </summary>
        public static List<(string name, int r, int g, int b)> GetColorInfoList()
        {
            return WxColors.ColorList.ToList();
        }

        /// <summary>
        /// Retrieve an RGB color in PDF format (0–1 range) by name.
        /// Returns white (1, 1, 1) when the name is not found.
        /// </summary>
        /// <param name="name">Colour name (case-insensitive).</param>
        public static ColorRgb GetColor(string name)
        {
            if (WxColors.PdfColorDict.TryGetValue(name.ToLower(), out var c))
                return new ColorRgb(c.r, c.g, c.b);
            return new ColorRgb(1f, 1f, 1f);
        }

        /// <summary>
        /// Retrieve the Hue / Saturation / Value triple for a named colour.
        /// Hue is in degrees (0–360), Saturation in percent (0–100),
        /// Value in percent (0–100). Returns (-1, -1, -1) when the name is
        /// not found.
        /// </summary>
        /// <param name="name">Colour name (case-insensitive).</param>
        public static (int H, int S, float V) GetColorHSV(string name)
        {
            string upper = name.ToUpper();
            var list = WxColors.ColorList;
            int idx = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Name == upper)
                {
                    idx = i;
                    break;
                }
            }
            if (idx < 0)
                return (-1, -1, -1f);

            var entry = list[idx];
            float r = entry.R / 255f;
            float g = entry.G / 255f;
            float b = entry.B / 255f;

            float cmax = Math.Max(r, Math.Max(g, b));
            float V = (float)Math.Round(cmax * 100, 1);
            float cmin = Math.Min(r, Math.Min(g, b));
            float delta = cmax - cmin;

            float hue;
            if (delta == 0f)
                hue = 0f;
            else if (cmax == r)
                hue = 60f * (((g - b) / delta) % 6f);
            else if (cmax == g)
                hue = 60f * (((b - r) / delta) + 2f);
            else
                hue = 60f * (((r - g) / delta) + 4f);

            int H = (int)Math.Round(hue);

            float sat = cmax == 0f ? 0f : delta / cmax;
            int S = (int)Math.Round(sat * 100);

            return (H, S, V);
        }

        // ─── Page Label Functions ────────────────────────────────────────

        /// <summary>
        /// Convert a PDF page-label rule (page number + raw rule string) into a
        /// dictionary with keys: startpage, prefix, style, firstpagenum.
        /// </summary>
        /// <param name="item">Tuple of (page number, raw rule string like "&lt;&lt;/S/D…&gt;&gt;").</param>
        public static Dictionary<string, object> RuleDict((int pno, string rule) item)
        {
            string rule = item.rule;
            rule = rule.Substring(2, rule.Length - 4);
            string[] parts = rule.Split('/');
            string[] entries = parts.Skip(1).ToArray();

            var d = new Dictionary<string, object>
            {
                ["startpage"] = item.pno,
                ["prefix"] = "",
                ["firstpagenum"] = 1
            };

            bool skip = false;
            for (int i = 0; i < entries.Length; i++)
            {
                if (skip)
                {
                    skip = false;
                    continue;
                }

                string entry = entries[i];
                if (entry == "S")
                {
                    d["style"] = entries[i + 1];
                    skip = true;
                }
                else if (entry.StartsWith("P"))
                {
                    string x = entry.Substring(1).Replace("(", "").Replace(")", "");
                    d["prefix"] = x;
                }
                else if (entry.StartsWith("St"))
                {
                    int x = int.Parse(entry.Substring(2));
                    d["firstpagenum"] = x;
                }
            }
            return d;
        }

        /// <summary>
        /// Return the page label string for a given 0-based page number.
        /// </summary>
        /// <param name="pageNo">0-based page number.</param>
        /// <param name="labels">List of (page-number, rule-string) pairs from the document.</param>
        public static string GetLabelPno(int pageNo, List<(int pno, string rule)> labels)
        {
            var item = labels.Last(x => x.pno <= pageNo);
            var rule = RuleDict(item);
            string prefix = rule.ContainsKey("prefix") ? (string)rule["prefix"] : "";
            string style = rule.ContainsKey("style") ? (string)rule["style"] : "";
            int delta = (style == "a" || style == "A") ? -1 : 0;
            int pagenumber = pageNo - (int)rule["startpage"] + (int)rule["firstpagenum"] + delta;
            return ConstructLabel(style, prefix, pagenumber);
        }

        /// <summary>
        /// Build a page label string from style, prefix and page number.
        /// </summary>
        /// <param name="style">One of "D" (decimal), "r"/"R" (roman), "a"/"A" (letter), or empty.</param>
        /// <param name="prefix">Prefix string prepended to the number part.</param>
        /// <param name="pno">Logical page number.</param>
        public static string ConstructLabel(string style, string prefix, int pno)
        {
            string nStr = "";
            switch (style)
            {
                case "D":
                    nStr = pno.ToString();
                    break;
                case "r":
                    nStr = IntegerToRoman(pno).ToLower();
                    break;
                case "R":
                    nStr = IntegerToRoman(pno).ToUpper();
                    break;
                case "a":
                    nStr = IntegerToLetter(pno).ToLower();
                    break;
                case "A":
                    nStr = IntegerToLetter(pno).ToUpper();
                    break;
            }
            return prefix + nStr;
        }

        /// <summary>
        /// Convert a positive integer to an alphabetic sequence (legacy <c>Integer2Letter</c>: 0→A, 1→B, …).
        /// </summary>
        public static string IntegerToLetter(int i) => Integer2Letter(i);

        /// <inheritdoc cref="IntegerToLetter"/>
        public static string Integer2Letter(int i)
        {
            const string ls = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int n = 1;
            int a = i;
            while (Math.Pow(26, n) <= a)
            {
                a -= (int)Math.Pow(26, n);
                n++;
            }

            string result = "";
            for (int j = n - 1; j >= 0; j--)
            {
                int power = (int)Math.Pow(26, j);
                int f = a / power;
                int g = a % power;
                result += ls[f];
                a = g;
            }
            return result;
        }

        /// <summary>Convert a positive integer to a Roman numeral (legacy <c>Integer2Roman</c>).</summary>
        public static string IntegerToRoman(int num) => Integer2Roman(num);

        /// <inheritdoc cref="IntegerToRoman"/>
        public static string Integer2Roman(int num)
        {
            var romanValues = new (int value, string numeral)[]
            {
                (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
                (100, "C"),  (90, "XC"),  (50, "L"),  (40, "XL"),
                (10, "X"),   (9, "IX"),   (5, "V"),   (4, "IV"),
                (1, "I")
            };

            string result = "";
            foreach (var (value, numeral) in romanValues)
            {
                int count = num / value;
                for (int k = 0; k < count; k++)
                    result += numeral;
                num -= value * count;
                if (num <= 0)
                    break;
            }
            return result;
        }

        // ─── Quad Recovery Functions ─────────────────────────────────────

        /// <summary>
        /// Compute the quadrilateral located inside a bounding box.
        /// The bbox may be the span bbox or any character bbox within the span.
        /// </summary>
        /// <param name="lineDir">The line's direction vector (cos, sin).</param>
        /// <param name="span">The span dictionary from text extraction.</param>
        /// <param name="bbox">The bounding box as a <see cref="Rect"/>.</param>
        /// <param name="smallGlyphHeights">
        /// When true, use only font-size as glyph height (matching TOOLS.set_small_glyph_heights()).
        /// </param>
        public static Quad RecoverBboxQuad(
            (float cos, float sin) lineDir,
            Dictionary<string, object> span,
            Rect bbox,
            bool smallGlyphHeights = false)
        {
            float cos = lineDir.cos;
            float sin = lineDir.sin;

            float d;
            if (smallGlyphHeights)
                d = 1.0f;
            else
                d = (float)Convert.ToDouble(span["ascender"]) - (float)Convert.ToDouble(span["descender"]);

            float height = d * (float)Convert.ToDouble(span["size"]);

            float hs = height * sin;
            float hc = height * cos;

            Point ul, ur, ll, lr;
            if (hc >= 0 && hs <= 0)
            {
                ul = bbox.BL - new Point(0, hc);
                ur = bbox.TR + new Point(hs, 0);
                ll = bbox.BL - new Point(hs, 0);
                lr = bbox.TR + new Point(0, hc);
            }
            else if (hc <= 0 && hs <= 0)
            {
                ul = bbox.BR + new Point(hs, 0);
                ur = bbox.TL - new Point(0, hc);
                ll = bbox.BR + new Point(0, hc);
                lr = bbox.TL - new Point(hs, 0);
            }
            else if (hc <= 0 && hs >= 0)
            {
                ul = bbox.TR - new Point(0, hc);
                ur = bbox.BL + new Point(hs, 0);
                ll = bbox.TR - new Point(hs, 0);
                lr = bbox.BL + new Point(0, hc);
            }
            else
            {
                ul = bbox.TL + new Point(hs, 0);
                ur = bbox.BR - new Point(0, hc);
                ll = bbox.TL + new Point(0, hc);
                lr = bbox.BR - new Point(hs, 0);
            }
            return new Quad(ul, ur, ll, lr);
        }

        /// <summary>
        /// Compute the quadrilateral located inside a bounding box (typed span).
        /// </summary>
        public static Quad RecoverBboxQuad(
            (float cos, float sin) lineDir,
            Span span,
            Rect bbox,
            bool smallGlyphHeights = false)
        {
            if (span == null)
                throw new ArgumentNullException(nameof(span));

            float cos = lineDir.cos;
            float sin = lineDir.sin;

            float d = smallGlyphHeights ? 1.0f : span.Asc - span.Desc;
            float height = d * span.Size;

            float hs = height * sin;
            float hc = height * cos;

            Point ul, ur, ll, lr;
            if (hc >= 0 && hs <= 0)
            {
                ul = bbox.BL - new Point(0, hc);
                ur = bbox.TR + new Point(hs, 0);
                ll = bbox.BL - new Point(hs, 0);
                lr = bbox.TR + new Point(0, hc);
            }
            else if (hc <= 0 && hs <= 0)
            {
                ul = bbox.BR + new Point(hs, 0);
                ur = bbox.TL - new Point(0, hc);
                ll = bbox.BR + new Point(0, hc);
                lr = bbox.TL - new Point(hs, 0);
            }
            else if (hc <= 0 && hs >= 0)
            {
                ul = bbox.TR - new Point(0, hc);
                ur = bbox.BL + new Point(hs, 0);
                ll = bbox.TR - new Point(hs, 0);
                lr = bbox.BL + new Point(0, hc);
            }
            else
            {
                ul = bbox.TL + new Point(hs, 0);
                ur = bbox.BR - new Point(0, hc);
                ll = bbox.TL + new Point(0, hc);
                lr = bbox.BR - new Point(hs, 0);
            }
            return new Quad(ul, ur, ll, lr);
        }

        /// <summary>
        /// Recover the quadrilateral of a text span from its bbox and direction.
        /// </summary>
        /// <param name="lineDir">The line's direction vector (cos, sin).</param>
        /// <param name="span">The span dictionary (must contain "bbox").</param>
        /// <param name="smallGlyphHeights">Use font-size only as glyph height.</param>
        public static Quad RecoverQuad(
            (float cos, float sin) lineDir,
            Dictionary<string, object> span,
            bool smallGlyphHeights = false)
        {
            Rect bbox = DictToRect(span["bbox"]);
            return RecoverBboxQuad(lineDir, span, bbox, smallGlyphHeights);
        }

        /// <summary>
        /// Recover the quadrilateral of a typed text span from its bbox and direction.
        /// </summary>
        public static Quad RecoverQuad(
            (float cos, float sin) lineDir,
            Span span,
            bool smallGlyphHeights = false)
        {
            if (span == null)
                throw new ArgumentNullException(nameof(span));
            return RecoverBboxQuad(lineDir, span, span.Bbox, smallGlyphHeights);
        }

        /// <summary>
        /// Calculate the quad covering a text line (or a subset of its spans)
        /// from "dict" / "rawdict" text extraction output.
        /// </summary>
        /// <param name="line">The line dictionary (must contain "dir" and "spans").</param>
        /// <param name="spans">Optional sub-list of spans; defaults to all spans in the line.</param>
        /// <param name="smallGlyphHeights">Use font-size only as glyph height.</param>
        public static Quad RecoverLineQuad(
            Dictionary<string, object> line,
            List<Dictionary<string, object>> spans = null,
            bool smallGlyphHeights = false)
        {
            if (spans == null)
                spans = (List<Dictionary<string, object>>)line["spans"];
            if (spans.Count == 0)
                throw new ArgumentException("bad span list");

            var lineDir = DictToDir(line["dir"]);

            Quad q0 = RecoverQuad(lineDir, spans[0], smallGlyphHeights);
            Quad q1 = spans.Count > 1
                ? RecoverQuad(lineDir, spans[spans.Count - 1], smallGlyphHeights)
                : q0;

            Point lineLl = q0.LL;
            Point lineLr = q1.LR;

            Matrix mat0 = PlanishLineCore(lineLl, lineLr);

            Point xLr = new Point(lineLr);
            xLr.Transform(mat0);

            float h = 0;
            foreach (var s in spans)
            {
                float size = (float)Convert.ToDouble(s["size"]);
                float spanH;
                if (smallGlyphHeights)
                    spanH = size;
                else
                    spanH = size * ((float)Convert.ToDouble(s["ascender"]) - (float)Convert.ToDouble(s["descender"]));
                if (spanH > h) h = spanH;
            }

            var lineRect = new Rect(0, -h, xLr.X, 0);
            var lineQuad = lineRect.Quad;

            Matrix inv = mat0.Inverted();
            if (inv != null)
                lineQuad.Transform(inv);

            return lineQuad;
        }

        /// <summary>
        /// Calculate the quad covering a typed text line (or a subset of its spans)
        /// from structured text extraction output.
        /// </summary>
        /// <param name="line">The line object (must contain <see cref="Line.Dir"/> and <see cref="Line.Spans"/>).</param>
        /// <param name="spans">Optional sub-list of spans; defaults to all spans in the line.</param>
        /// <param name="smallGlyphHeights">Use font-size only as glyph height.</param>
        public static Quad RecoverLineQuad(
            Line line,
            List<Span> spans = null,
            bool smallGlyphHeights = false)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));
            if (spans == null)
                spans = line.Spans;
            if (spans == null || spans.Count == 0)
                throw new ArgumentException("bad span list");

            var lineDir = (line.Dir.X, line.Dir.Y);

            Quad q0 = RecoverQuad(lineDir, spans[0], smallGlyphHeights);
            Quad q1 = spans.Count > 1
                ? RecoverQuad(lineDir, spans[spans.Count - 1], smallGlyphHeights)
                : q0;

            Point lineLl = q0.LL;
            Point lineLr = q1.LR;

            Matrix mat0 = PlanishLineCore(lineLl, lineLr);

            Point xLr = new Point(lineLr);
            xLr.Transform(mat0);

            float h = 0;
            foreach (var s in spans)
            {
                float spanH = smallGlyphHeights
                    ? s.Size
                    : s.Size * (s.Asc - s.Desc);
                if (spanH > h) h = spanH;
            }

            var lineRect = new Rect(0, -h, xLr.X, 0);
            var lineQuad = lineRect.Quad;

            Matrix inv = mat0.Inverted();
            if (inv != null)
                lineQuad.Transform(inv);

            return lineQuad;
        }

        /// <summary>
        /// Calculate the quad of a text span (or a sub-selection of its characters)
        /// from "dict" / "rawdict" text extraction output.
        /// When <paramref name="chars"/> is null the full span quad is returned.
        /// Sub-selecting characters requires the "rawdict" extraction option.
        /// </summary>
        /// <param name="lineDir">The line's direction vector (cos, sin).</param>
        /// <param name="span">The span dictionary.</param>
        /// <param name="chars">Optional sub-list of character dictionaries.</param>
        /// <param name="smallGlyphHeights">Use font-size only as glyph height.</param>
        public static Quad RecoverSpanQuad(
            (float cos, float sin) lineDir,
            Dictionary<string, object> span,
            List<Dictionary<string, object>> chars = null,
            bool smallGlyphHeights = false)
        {
            if (chars == null)
                return RecoverQuad(lineDir, span, smallGlyphHeights);
            if (!span.ContainsKey("chars"))
                throw new ArgumentException("need 'rawdict' option to sub-select chars");

            Quad q0 = RecoverCharQuad(lineDir, span, chars[0], smallGlyphHeights);
            Quad q1 = chars.Count > 1
                ? RecoverCharQuad(lineDir, span, chars[chars.Count - 1], smallGlyphHeights)
                : q0;

            Point spanLl = q0.LL;
            Point spanLr = q1.LR;
            Matrix mat0 = PlanishLineCore(spanLl, spanLr);

            Point xLr = new Point(spanLr);
            xLr.Transform(mat0);

            float size = (float)Convert.ToDouble(span["size"]);
            float h;
            if (smallGlyphHeights)
                h = size;
            else
                h = size * ((float)Convert.ToDouble(span["ascender"]) - (float)Convert.ToDouble(span["descender"]));

            var spanRect = new Rect(0, -h, xLr.X, 0);
            var spanQuad = spanRect.Quad;

            Matrix inv = mat0.Inverted();
            if (inv != null)
                spanQuad.Transform(inv);

            return spanQuad;
        }

        /// <summary>
        /// Recover the quadrilateral of a single text character.
        /// Requires the "rawdict" extraction option.
        /// </summary>
        /// <param name="lineDir">The line's direction vector (cos, sin).</param>
        /// <param name="span">The span dictionary.</param>
        /// <param name="charDict">The character dictionary (must contain "bbox").</param>
        /// <param name="smallGlyphHeights">Use font-size only as glyph height.</param>
        public static Quad RecoverCharQuad(
            (float cos, float sin) lineDir,
            Dictionary<string, object> span,
            Dictionary<string, object> charDict,
            bool smallGlyphHeights = false)
        {
            Rect bbox = DictToRect(charDict["bbox"]);
            return RecoverBboxQuad(lineDir, span, bbox, smallGlyphHeights);
        }

        // ─── Font Helper ────────────────────────────────────────────────

        /// <summary>
        /// Retrieve font properties (name, file extension, subtype, ascender,
        /// descender) for the font at the given xref.
        /// Ascender and descender default to 0.8 / -0.2 when the font cannot
        /// be inspected.
        /// </summary>
        /// <param name="doc">The parent document.</param>
        /// <param name="xref">The font object's xref number.</param>
        public static (string fontname, string ext, string stype, float asc, float dsc) GetFontProperties(
            Document doc, int xref)
        {
            var (fontname, ext, stype, buffer) = doc.ExtractFont(xref);
            float asc = 0.8f;
            float dsc = -0.2f;

            if (string.IsNullOrEmpty(ext))
                return (fontname, ext, stype, asc, dsc);

            if (buffer != null && buffer.Length > 0)
            {
                try
                {
                    using var font = new Font(fontBuffer: buffer);
                    asc = font.Ascender;
                    dsc = font.Descender;
                    var bbox = font.BBox;
                    if (asc - dsc < 1)
                    {
                        if (bbox.Y0 < dsc)
                            dsc = (float)bbox.Y0;
                        asc = 1 - dsc;
                    }
                }
                catch
                {
                    asc *= 1.2f;
                    dsc *= 1.2f;
                }
                return (fontname, ext, stype, asc, dsc);
            }

            if (ext != "n/a")
            {
                try
                {
                    using var font = new Font(fontName: fontname);
                    asc = font.Ascender;
                    dsc = font.Descender;
                }
                catch
                {
                    asc *= 1.2f;
                    dsc *= 1.2f;
                }
            }
            else
            {
                asc *= 1.2f;
                dsc *= 1.2f;
            }
            return (fontname, ext, stype, asc, dsc);
        }

        /// <summary>Current timestamp in PDF date format (PyMuPDF <c>get_pdf_now</c>).</summary>
        public static string GetPdfNow() => Helpers.GetPdfNow();

        /// <summary>
        /// Encode a string for PDF (PyMuPDF <c>get_pdf_str</c>): parentheses, octal escapes, or UTF-16BE hex.
        /// </summary>
        public static string GetPdfStr(string s) => Helpers.GetPdfStr(s);

        /// <summary>
        /// Return a PDF string depending on its coding
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string GetPdfString(string s) => Helpers.GetPdfStr(s);

        /// <summary>
        /// Calculate length of a string for a built-in font (PyMuPDF <c>get_text_length</c>).
        /// </summary>
        /// <param name="fontname">Name of the font.</param>
        /// <param name="fontsize">Font size points.</param>
        /// <param name="encoding">Encoding to use, 0=Latin (default), 1=Greek, 2=Cyrillic.</param>
        /// <returns>Length of text.</returns>
        public static float GetTextLength(string text, string fontname = "helv", float fontsize = 11, int encoding = 0)
        {
            // fontname = fontname.lower()
            fontname = (fontname ?? "helv").ToLowerInvariant();
            // basename = Base14_fontdict.get(fontname, None)
            Constants.Base14FontDict.TryGetValue(fontname, out string? basename);

            // glyphs = None
            (int glyph, double width)[]? glyphs = null;
            // if basename == "Symbol":
            //     glyphs = symbol_glyphs
            if (basename == "Symbol")
                glyphs = (Constants.SymbolGlyphs);
            // if basename == "ZapfDingbats":
            //     glyphs = zapf_glyphs
            if (basename == "ZapfDingbats")
                glyphs = Constants.ZapfGlyphs;
            // if glyphs is not None:
            if (glyphs != null)
            {
                // w = sum([glyphs[ord(c)][1] if ord(c) < 256 else glyphs[183][1] for c in text])
                double w = 0;
                foreach (char ch in text)
                {
                    int oc = ch;
                    if (oc < 256)
                        w += glyphs[oc].width;
                    else
                        w += glyphs[183].width;
                }
                // return w * fontsize
                return (float)(w * fontsize);
            }

            // if fontname in Base14_fontdict.keys():
            if (Constants.Base14FontDict.ContainsKey(fontname))
            {
                // return util_measure_string(
                //     text, Base14_fontdict[fontname], fontsize, encoding
                // )
                return Helpers.UtilMeasureString(
                    text,
                    Constants.Base14FontDict[fontname],
                    fontsize,
                    encoding);
            }

            // if fontname in (
            //     "china-t",
            //     "china-s",
            //     "china-ts",
            //     "china-ss",
            //     "japan",
            //     "japan-s",
            //     "korea",
            //     "korea-s",
            // ):
            //     return len(text) * fontsize
            if (fontname is "china-t" or "china-s" or "china-ts" or "china-ss" or
                "japan" or "japan-s" or "korea" or "korea-s")
                return text.Length * fontsize;

            // raise ValueError(f"Font '{fontname}' is unsupported")
            throw new ValueErrorException($"Font '{fontname}' is unsupported");
        }

        /// <summary>
        /// Calculate length of a string for a built-in font.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fontname">name of the font.</param>
        /// <param name="fontsize">font size points.</param>
        /// <param name="encoding">encoding to use, 0=Latin (default), 1=Greek, 2=Cyrillic.</param>
        /// <returns>length of text.</returns>
        /// <exception cref="Exception"></exception>
        public static float GetTextLength(
            string text,
            string fontFile,
            string fontName = "helv",
            float fontSize = 11,
            int encoding = 0
        ) => GetTextLength(text, fontname: fontName, fontsize: fontSize, encoding: encoding);

        /// <summary>
        /// Basic image metadata (PyMuPDF <c>image_profile</c>): width, height, colorspace, bpc, ext, etc.
        /// </summary>
        /// <param name="keep_image">When non-zero, include the native <c>fz_image</c> handle under key <c>image</c>.</param>
        public static Dictionary<string, object> ImageProperties(object img, int keep_image = 0)
        {
            if (img == null)
                throw new ArgumentException("bad argument 'img'");

            byte[] stream;
            // if type(img) is io.BytesIO: stream = img.getvalue()
            if (img != null && img.GetType() == typeof(MemoryStream))
            {
                var ms = (MemoryStream)img;
                stream = ms.ToArray();
            }
            // elif hasattr(img, "read"): stream = img.read()
            else if (img is Stream s)
            {
                using (var mem = new MemoryStream())
                {
                    s.CopyTo(mem);
                    stream = mem.ToArray();
                }
            }
            // elif type(img) in (bytes, bytearray): stream = img
            else if (img is byte[] bytes)
            {
                stream = bytes;
            }
            // else: raise ValueError("bad argument 'img'")
            else
            {
                throw new ArgumentException("bad argument 'img'");
            }

            // JM_image_profile: if not imagedata: return None
            if (stream.Length == 0)
                return null;
            // len_ = len(imagedata)
            // JM_image_profile: if len_ < 8 -> return None.
            // message("bad image data")
            if (stream.Length < 8)
                return null;

            // c = imagedata
            // if keep_image:
            //     res = mupdf.fz_new_buffer_from_copied_data(c, len_)
            // else:
            //     res = mupdf.fz_new_buffer_from_shared_data(c, len_)
            var buf = Helpers.BufferFromBytes(stream);
            int type = RecognizeImageType(buf);
            // if type_ == mupdf.FZ_IMAGE_UNKNOWN: return None
            if (type == mupdf.mupdf.FZ_IMAGE_UNKNOWN)
                return null;

            string ext = ImageExtensionFromType(type);
            // image = mupdf.fz_new_image_from_buffer(res)
            var image = mupdf.mupdf.fz_new_image_from_buffer(buf);
            if (image == null || image.m_internal == null)
                return null;

            // ctm = mupdf.fz_image_orientation_matrix(image)
            var orientationMatrix = image.fz_image_orientation_matrix();
            // xres, yres = mupdf.fz_image_resolution(image)
            var resolution = image.fz_image_resolution();
            // orientation = mupdf.fz_image_orientation(image)
            byte orientation = image.fz_image_orientation();
            // cs_name = mupdf.fz_colorspace_name(image.colorspace())
            var cs = image.colorspace();
            string csName = cs != null && cs.m_internal != null ? mupdf.mupdf.fz_colorspace_name(cs) : null;
            // result = dict()

            var result = new Dictionary<string, object>
            {
                // result[dictkey_width] = image.w()
                ["width"] = image.w(),
                // result[dictkey_height] = image.h()
                ["height"] = image.h(),
                // result["orientation"] = orientation
                ["orientation"] = orientation,
                // result[dictkey_matrix] = JM_py_from_matrix(ctm)
                ["transform"] = new Matrix(orientationMatrix),
                // result[dictkey_xres] = xres
                ["xres"] = resolution.xres,
                // result[dictkey_yres] = yres
                ["yres"] = resolution.yres,
                // result[dictkey_colorspace] = image.n()
                ["colorspace"] = image.n(),
                // result[dictkey_bpc] = image.bpc()
                ["bpc"] = image.bpc(),
                // result[dictkey_ext] = JM_image_extension(type_)
                ["ext"] = ext,
                // result[dictkey_cs_name] = cs_name
                ["cs-name"] = csName,
            };

            // if keep_image: result[dictkey_image] = image
            if (keep_image != 0)
                result["image"] = image;
            return result;
        }

        /// <summary>Paper rectangle at 72 dpi (PyMuPDF <c>paper_rect</c>).</summary>
        public static Rect PaperRect(string s) => Helpers.PaperRect(s);

        /// <summary>Paper (width, height) at 72 dpi; <c>A4-L</c> is landscape (PyMuPDF <c>paper_size</c>).</summary>
        public static (float w, float h) PaperSize(string s) => Helpers.PaperSize(s);

        /// <summary>All known paper sizes (PyMuPDF <c>paper_sizes</c>).</summary>
        public static Dictionary<string, (float w, float h)> PaperSizes() => Helpers.GetPaperSizes();

        /// <summary>
        /// Map line <paramref name="p1"/>–<paramref name="p2"/> to the x-axis; <paramref name="p1"/> becomes origin (PyMuPDF <c>planish_line</c>).
        /// </summary>
        public static Matrix PlanishLine(Point p1, Point p2) => PlanishLineCore(p1, p2);

        private static Matrix PlanishLineCore(Point p1, Point p2)
        {
            // p1 = Point(p1)
            // p2 = Point(p2)
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < Constants.Epsilon)
                return new Matrix(1, 0, 0, 1, -p1.X, -p1.Y);

            float cos = dx / length;
            float sin = dy / length;

            var translate = new Matrix(1, 0, 0, 1, -p1.X, -p1.Y);
            var rotate = new Matrix(cos, -sin, sin, cos, 0, 0);
            return translate * rotate;
        }

        private static int RecognizeImageType(mupdf.FzBuffer buffer)
        {
            // #log('calling mfz_recognize_image_format with {c!r=}')
            var outparams = new mupdf.ll_fz_buffer_storage_outparams();
            mupdf.mupdf.ll_fz_buffer_storage_outparams_fn(buffer.m_internal, outparams);
            return mupdf.mupdf.fz_recognize_image_format(outparams.datap);
        }

        /// <summary>Map MuPDF image type constant to file extension (PyMuPDF <c>JM_image_extension</c>).</summary>
        public static string GetImageExtension(int type) => ImageExtensionFromType(type);

        private static string ImageExtensionFromType(int type) =>
            type switch
            {
                int t when t == mupdf.mupdf.FZ_IMAGE_FAX => "fax",
                int t when t == mupdf.mupdf.FZ_IMAGE_RAW => "raw",
                int t when t == mupdf.mupdf.FZ_IMAGE_FLATE => "flate",
                int t when t == mupdf.mupdf.FZ_IMAGE_RLD => "rld",
                int t when t == mupdf.mupdf.FZ_IMAGE_LZW => "lzw",
                int t when t == mupdf.mupdf.FZ_IMAGE_BMP => "bmp",
                int t when t == mupdf.mupdf.FZ_IMAGE_GIF => "gif",
                int t when t == mupdf.mupdf.FZ_IMAGE_JBIG2 => "jb2",
                int t when t == mupdf.mupdf.FZ_IMAGE_JPEG => "jpeg",
                int t when t == mupdf.mupdf.FZ_IMAGE_JPX => "jpx",
                int t when t == mupdf.mupdf.FZ_IMAGE_JXR => "jxr",
                int t when t == mupdf.mupdf.FZ_IMAGE_PNG => "png",
                int t when t == mupdf.mupdf.FZ_IMAGE_PNM => "pnm",
                int t when t == mupdf.mupdf.FZ_IMAGE_TIFF => "tiff",
                _ => "n/a",
            };

        /// <summary>
        /// Convert a direction value (tuple or array stored in a dictionary)
        /// to a (cos, sin) tuple.
        /// </summary>
        private static (float cos, float sin) DictToDir(object dirObj)
        {
            if (dirObj is ValueTuple<float, float> tuple)
                return (tuple.Item1, tuple.Item2);
            if (dirObj is IList<float> list && list.Count >= 2)
                return (list[0], list[1]);
            if (dirObj is float[] arr && arr.Length >= 2)
                return (arr[0], arr[1]);
            if (dirObj is float[] farr && farr.Length >= 2)
                return (farr[0], farr[1]);
            if (dirObj is IList<object> olist && olist.Count >= 2)
                return ((float)Convert.ToDouble(olist[0]), (float)Convert.ToDouble(olist[1]));
            throw new ArgumentException("cannot extract direction from object");
        }

        /// <summary>
        /// Convert a bbox value stored in a dictionary to a <see cref="Rect"/>.
        /// Accepts Rect, float[], float[], or IList&lt;float&gt;.
        /// </summary>
        private static Rect DictToRect(object bboxObj)
        {
            if (bboxObj is Rect r)
                return r;
            if (bboxObj is float[] darr && darr.Length >= 4)
                return new Rect(darr[0], darr[1], darr[2], darr[3]);
            if (bboxObj is float[] farr && farr.Length >= 4)
                return new Rect(farr[0], farr[1], farr[2], farr[3]);
            if (bboxObj is IList<float> dlist && dlist.Count >= 4)
                return new Rect(dlist[0], dlist[1], dlist[2], dlist[3]);
            if (bboxObj is IList<object> olist && olist.Count >= 4)
                return new Rect(
                    (float)Convert.ToDouble(olist[0]),
                    (float)Convert.ToDouble(olist[1]),
                    (float)Convert.ToDouble(olist[2]),
                    (float)Convert.ToDouble(olist[3]));
            throw new ArgumentException("cannot extract Rect from object");
        }

        public static List<List<Rect>> MakeTable(
            Rect rect,
            int cols = 1,
            int rows = 1)
        {
            /*
                Return a list of (rows x cols) equal sized rectangles.
            */

            // ensure valid rect
            if (rect.IsEmpty || rect.IsInfinite)
                throw new ArgumentException("rect must be finite and not empty");

            Point tl = rect.TopLeft;

            float height = rect.Height / rows; // height of one table cell
            float width = rect.Width / cols;   // width of one table cell

            // deltas
            Rect deltaH = new Rect(width, 0, width, 0);
            Rect deltaV = new Rect(0, height, 0, height);

            // first rectangle
            Rect r = new Rect(
                tl.X,
                tl.Y,
                tl.X + width,
                tl.Y + height);

            // make the first row
            List<Rect> row = new List<Rect> { r };

            for (int i = 1; i < cols; i++)
            {
                r += deltaH; // build next rect to the right
                row.Add(r);
            }

            // make result, starts with first row
            List<List<Rect>> rects = new List<List<Rect>>
            {
                row
            };

            for (int i = 1; i < rows; i++)
            {
                List<Rect> prevRow = rects[i - 1];
                List<Rect> newRow = new List<Rect>();

                // for each previous cell add its downward copy
                foreach (Rect cell in prevRow)
                {
                    newRow.Add(cell + deltaV);
                }

                rects.Add(newRow);
            }

            return rects;
        }

        /// <summary>Extract plain text avoiding unacceptable line breaks (PyMuPDF <c>utils.get_sorted_text</c>).</summary>
        public static string GetSortedText(
            Page page,
            IRect clip = null,
            int? flags = null,
            TextPage textpage = null,
            float tolerance = 3)
        {
            string LineText(Rect clipRect, List<(Rect wr, string text)> line)
            {
                // Create the string of one text line.
                line.Sort((a, b) => a.wr.X0.CompareTo(b.wr.X0));
                var sb = new System.Text.StringBuilder();
                float x1 = clipRect.X0;
                foreach (var (r, t) in line)
                {
                    float width = (float)Math.Max(r.Width, 1e-6);
                    int dist = Math.Max(
                        (int)Math.Round((r.X0 - x1) / width * t.Length),
                        (x1 == clipRect.X0 || r.X0 <= x1) ? 0 : 1);
                    sb.Append(' ', dist);
                    sb.Append(t);
                    x1 = r.X1;
                }
                return sb.ToString();
            }

            // Extract words in correct sequence first.
            var words = new List<(Rect wr, string text)>();
            foreach (var w in page.GetTextWords(clip, flags, textpage, sort: true, tolerance: tolerance))
                words.Add((new Rect(w.x0, w.y0, w.x1, w.y1), w.word));

            if (words.Count == 0)
                return "";

            var totalbox = new Rect(words[0].wr);
            foreach (var (wr, _) in words)
                totalbox |= wr;

            var lines = new List<(Rect lrect, string ltext)>();
            var line = new List<(Rect wr, string text)> { words[0] };
            var lrect = words[0].wr;

            for (int i = 1; i < words.Count; i++)
            {
                var (wr, text) = words[i];
                if (Math.Abs(lrect.Y0 - wr.Y0) <= tolerance || Math.Abs(lrect.Y1 - wr.Y1) <= tolerance)
                {
                    line.Add((wr, text));
                    lrect |= wr;
                }
                else
                {
                    lines.Add((lrect, LineText(totalbox, line)));
                    line = new List<(Rect wr, string text)> { (wr, text) };
                    lrect = wr;
                }
            }

            lines.Add((lrect, LineText(totalbox, line)));
            lines.Sort((a, b) => a.lrect.Y1.CompareTo(b.lrect.Y1));

            var result = new System.Text.StringBuilder(lines[0].ltext);
            float y1 = lines[0].lrect.Y1;
            for (int i = 1; i < lines.Count; i++)
            {
                var (lrect2, ltext) = lines[i];
                float height = (float)Math.Max(lrect2.Height, 1e-6);
                int distance = Math.Min((int)Math.Round((lrect2.Y0 - y1) / height), 5);
                result.Append('\n', distance + 1);
                result.Append(ltext);
                y1 = lrect2.Y1;
            }

            return result.ToString();
        }

        /// <summary>
        /// Extract text from a page or an annotation (PyMuPDF <c>utils.get_text</c>).
        /// </summary>
        /// <remarks>
        /// This is a unifying wrapper for various methods of the pymupdf.TextPage class.
        /// </remarks>
        public static dynamic GetText(
            Page page,
            string option = "text",
            IRect clip = null,
            int? flags = null,
            TextPage textpage = null,
            bool sort = false,
            object delimiters = null,
            float tolerance = 3)
        {
            // formats = {
            //     "text": pymupdf.TEXTFLAGS_TEXT,
            //     "html": pymupdf.TEXTFLAGS_HTML,
            //     "json": pymupdf.TEXTFLAGS_DICT,
            //     "rawjson": pymupdf.TEXTFLAGS_RAWDICT,
            //     "xml": pymupdf.TEXTFLAGS_XML,
            //     "xhtml": pymupdf.TEXTFLAGS_XHTML,
            //     "dict": pymupdf.TEXTFLAGS_DICT,
            //     "rawdict": pymupdf.TEXTFLAGS_RAWDICT,
            //     "words": pymupdf.TEXTFLAGS_WORDS,
            //     "blocks": pymupdf.TEXTFLAGS_BLOCKS,
            // }
            var formats = new Dictionary<string, int>
            {
                ["text"] = Constants.TextFlagsText,
                ["html"] = Constants.TextFlagsHtml,
                ["json"] = Constants.TextFlagsDict,
                ["rawjson"] = Constants.TextFlagsRawDict,
                ["xml"] = Constants.TextFlagsXml,
                ["xhtml"] = Constants.TextFlagsXhtml,
                ["dict"] = Constants.TextFlagsDict,
                ["rawdict"] = Constants.TextFlagsRawDict,
                ["words"] = Constants.TextFlagsWords,
                ["blocks"] = Constants.TextFlagsBlocks,
            };
            // option = option.lower()
            option = (option ?? "text").ToLowerInvariant();
            // assert option in formats
            Debug.Assert(formats.ContainsKey(option));
            // if option not in formats:
            //     option = "text"
            if (!formats.ContainsKey(option))
                option = "text";
            // if flags is None:
            //     flags = formats[option]
            if (flags == null)
                flags = formats[option];

            // if option == "words":
            //     return get_text_words(
            //         page,
            //         clip=clip,
            //         flags=flags,
            //         textpage=textpage,
            //         sort=sort,
            //         delimiters=delimiters,
            //     )
            if (option == "words")
                return WordBlocksFromTuples(page.GetTextWords(clip, flags, textpage, sort, DelimitersToString(delimiters), tolerance));
            // if option == "blocks":
            //     return get_text_blocks(
            //         page, clip=clip, flags=flags, textpage=textpage, sort=sort
            //     )
            if (option == "blocks")
                return TextBlocksFromTuples(page.GetTextBlocks(clip, flags, textpage, sort));

            // if option == "text" and sort:
            //     return get_sorted_text(
            //         page,
            //         clip=clip,
            //         flags=flags,
            //         textpage=textpage,
            //         tolerance=tolerance,
            //     )
            if (option == "text" && sort)
                return GetSortedText(page, clip, flags, textpage, tolerance);

            // pymupdf.CheckParent(page)
            page.RequireParent();
            // cb = None
            Rect cb = null;
            // if option in ("html", "xml", "xhtml"):  # no clipping for MuPDF functions
            //     clip = page.cropbox
            if (option == "html" || option == "xml" || option == "xhtml")
                clip = new IRect(page.CropBox);
            // if clip is not None:
            //     clip = pymupdf.Rect(clip)
            //     cb = None
            // elif type(page) is pymupdf.Page:
            //     cb = page.cropbox
            if (clip != null)
            {
                clip = new IRect(clip);
                cb = null;
            }
            else
                cb = page.CropBox;
            // pymupdf.TextPage with or without images
            // tp = textpage
            TextPage tp = textpage;
            //pymupdf.exception_info()
            // if tp is None:
            //     tp = page.get_textpage(clip=clip, flags=flags)
            if (tp == null)
                tp = page.NewTextPageForGetText(clip, flags.Value);
            // elif getattr(tp, "parent") != page:
            //     raise ValueError("not a textpage of this page")
            else if (tp.Parent != page)
                throw new ValueErrorException("not a textpage of this page");
            //pymupdf.log( '{option=}')
            object t;
            // if option == "json":
            //     t = tp.extractJSON(cb=cb, sort=sort)
            if (option == "json")
                t = ExtractJsonWithCb(tp, cb, sort);
            // elif option == "rawjson":
            //     t = tp.extractRAWJSON(cb=cb, sort=sort)
            else if (option == "rawjson")
                t = ExtractRawJsonWithCb(tp, cb, sort);
            // elif option == "dict":
            //     t = tp.extractDICT(cb=cb, sort=sort)
            else if (option == "dict")
                t = ExtractDictWithCb(tp, cb, sort);
            // elif option == "rawdict":
            //     t = tp.extractRAWDICT(cb=cb, sort=sort)
            else if (option == "rawdict")
                t = ExtractRawDictWithCb(tp, cb, sort);
            // elif option == "html":
            //     t = tp.extractHTML()
            else if (option == "html")
                t = tp.ExtractHtml();
            // elif option == "xml":
            //     t = tp.extractXML()
            else if (option == "xml")
                t = tp.ExtractXml();
            // elif option == "xhtml":
            //     t = tp.extractXHTML()
            else if (option == "xhtml")
                t = tp.ExtractXhtml();
            // else:
            //     t = tp.extractText(sort=sort)
            else
                t = tp.ExtractText(sort);

            // if textpage is None:
            //     del tp
            if (textpage == null)
                tp = null;
            return t;
        }

        /// <summary>
        /// Extract text from an annotation (PyMuPDF <c>utils.get_text</c> when called on <c>Annot</c>).
        /// </summary>
        public static dynamic GetText(
            Annot annot,
            string option = "text",
            IRect clip = null,
            int? flags = null,
            TextPage textpage = null,
            bool sort = false,
            object delimiters = null,
            float tolerance = 3)
        {
            if (annot.Parent == null)
                throw new ArgumentException("annotation has no parent page");

            var formats = new Dictionary<string, int>
            {
                ["text"] = Constants.TextFlagsText,
                ["html"] = Constants.TextFlagsHtml,
                ["json"] = Constants.TextFlagsDict,
                ["rawjson"] = Constants.TextFlagsRawDict,
                ["xml"] = Constants.TextFlagsXml,
                ["xhtml"] = Constants.TextFlagsXhtml,
                ["dict"] = Constants.TextFlagsDict,
                ["rawdict"] = Constants.TextFlagsRawDict,
                ["words"] = Constants.TextFlagsWords,
                ["blocks"] = Constants.TextFlagsBlocks,
            };
            option = (option ?? "text").ToLowerInvariant();
            if (!formats.ContainsKey(option))
                option = "text";
            if (flags == null)
                flags = formats[option];

            if (option == "words")
                return WordBlocksFromTuples(GetAnnotTextWords(annot, clip, flags, textpage, sort, DelimitersToString(delimiters), tolerance));
            if (option == "blocks")
                return TextBlocksFromTuples(GetAnnotTextBlocks(annot, clip, flags, textpage, sort));
            if (option == "text" && sort)
                return GetSortedTextAnnot(annot, clip, flags, textpage, tolerance);

            annot.Parent.RequireParent();
            // Python: only Page sets cb = page.cropbox; Annot leaves cb None.
            Rect cb = null;
            if (option == "html" || option == "xml" || option == "xhtml")
                clip = new IRect(annot.Parent.CropBox);
            if (clip != null)
                clip = new IRect(clip);

            TextPage tp = textpage;
            if (tp == null)
                tp = annot.GetTextPage(flags.Value, clip);
            else if (tp.Parent != annot.Parent)
                throw new ValueErrorException("not a textpage of this page");

            object t;
            if (option == "json")
                t = ExtractJsonWithCb(tp, cb, sort);
            else if (option == "rawjson")
                t = ExtractRawJsonWithCb(tp, cb, sort);
            else if (option == "dict")
                t = ExtractDictWithCb(tp, cb, sort);
            else if (option == "rawdict")
                t = ExtractRawDictWithCb(tp, cb, sort);
            else if (option == "html")
                t = tp.ExtractHtml();
            else if (option == "xml")
                t = tp.ExtractXml();
            else if (option == "xhtml")
                t = tp.ExtractXhtml();
            else
                t = tp.ExtractText(sort);

            if (textpage == null)
                tp.Dispose();
            return t;
        }

        static List<(float x0, float y0, float x1, float y1, string text, int blockNo, int blockType)> GetAnnotTextBlocks(
            Annot annot,
            IRect clip,
            int? flags,
            TextPage textpage,
            bool sort)
        {
            if (flags == null)
                flags = Constants.TextFlagsBlocks;
            TextPage tp = textpage;
            bool owned = false;
            if (tp == null)
            {
                tp = annot.GetTextPage(flags.Value, clip);
                owned = true;
            }
            else if (tp.Parent != annot.Parent)
                throw new ValueErrorException("not a textpage of this page");

            var blocks = tp.ExtractBlockTuples();
            if (owned)
                tp.Dispose();
            if (sort)
            {
                blocks.Sort((a, b) =>
                {
                    int c = a.y1.CompareTo(b.y1);
                    return c != 0 ? c : a.x0.CompareTo(b.x0);
                });
            }
            return blocks;
        }

        static List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> GetAnnotTextWords(
            Annot annot,
            IRect clip,
            int? flags,
            TextPage textpage,
            bool sort,
            string delimiters,
            float tolerance)
        {
            if (flags == null)
                flags = Constants.TextFlagsWords;
            TextPage tp = textpage;
            bool owned = false;
            if (tp == null)
            {
                tp = annot.GetTextPage(flags.Value, clip);
                owned = true;
            }
            else if (tp.Parent != annot.Parent)
                throw new ValueErrorException("not a textpage of this page");

            var words = tp.ExtractWordTuples(delimiters);
            if (owned)
                tp.Dispose();

            if (!sort || words.Count == 0)
                return words;

            words.Sort((a, b) =>
            {
                int c = a.y1.CompareTo(b.y1);
                return c != 0 ? c : a.x0.CompareTo(b.x0);
            });
            var nwords = new List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)>();
            var line = new List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> { words[0] };
            var lrect = new Rect(words[0].x0, words[0].y0, words[0].x1, words[0].y1);
            for (int i = 1; i < words.Count; i++)
            {
                var w = words[i];
                var wrect = new Rect(w.x0, w.y0, w.x1, w.y1);
                if (Math.Abs(wrect.Y0 - lrect.Y0) <= tolerance || Math.Abs(wrect.Y1 - lrect.Y1) <= tolerance)
                {
                    line.Add(w);
                    lrect |= wrect;
                }
                else
                {
                    line.Sort((a, b) => a.x0.CompareTo(b.x0));
                    nwords.AddRange(line);
                    line = new List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> { w };
                    lrect = wrect;
                }
            }
            line.Sort((a, b) => a.x0.CompareTo(b.x0));
            nwords.AddRange(line);
            return nwords;
        }

        static string GetSortedTextAnnot(
            Annot annot,
            IRect clip,
            int? flags,
            TextPage textpage,
            float tolerance)
        {
            var words = new List<(Rect wr, string text)>();
            foreach (var w in GetAnnotTextWords(annot, clip, flags, textpage, sort: true, delimiters: null, tolerance))
                words.Add((new Rect(w.x0, w.y0, w.x1, w.y1), w.word));
            if (words.Count == 0)
                return "";

            var totalbox = new Rect(words[0].wr);
            foreach (var (wr, _) in words)
                totalbox |= wr;

            string LineText(Rect clipRect, List<(Rect wr, string text)> line)
            {
                line.Sort((a, b) => a.wr.X0.CompareTo(b.wr.X0));
                var sb = new StringBuilder();
                float x1 = clipRect.X0;
                foreach (var (r, t) in line)
                {
                    float width = (float)Math.Max(r.Width, 1e-6f);
                    int dist = Math.Max(
                        (int)Math.Round((r.X0 - x1) / width * t.Length),
                        (x1 == clipRect.X0 || r.X0 <= x1) ? 0 : 1);
                    sb.Append(' ', dist);
                    sb.Append(t);
                    x1 = r.X1;
                }
                return sb.ToString();
            }

            var lines = new List<(Rect lrect, string ltext)>();
            var line = new List<(Rect wr, string text)> { words[0] };
            var lrect = words[0].wr;
            for (int i = 1; i < words.Count; i++)
            {
                var (wr, text) = words[i];
                if (Math.Abs(lrect.Y0 - wr.Y0) <= tolerance || Math.Abs(lrect.Y1 - wr.Y1) <= tolerance)
                {
                    line.Add((wr, text));
                    lrect |= wr;
                }
                else
                {
                    lines.Add((lrect, LineText(totalbox, line)));
                    line = new List<(Rect wr, string text)> { (wr, text) };
                    lrect = wr;
                }
            }
            lines.Add((lrect, LineText(totalbox, line)));
            lines.Sort((a, b) => a.lrect.Y1.CompareTo(b.lrect.Y1));

            var result = new StringBuilder(lines[0].ltext);
            float y1 = lines[0].lrect.Y1;
            for (int i = 1; i < lines.Count; i++)
            {
                var (lrect2, ltext) = lines[i];
                float height = (float)Math.Max(lrect2.Height, 1e-6f);
                int distance = Math.Min((int)Math.Round((lrect2.Y0 - y1) / height), 5);
                result.Append('\n', distance + 1);
                result.Append(ltext);
                y1 = lrect2.Y1;
            }
            return result.ToString();
        }

        /// <summary>MuPDF.NET <c>GetText("words")</c> / <c>GetTextWords</c> return type.</summary>
        internal static List<WordBlock> WordBlocksFromTuples(
            List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> rows) =>
            rows.Select(t => new WordBlock
            {
                X0 = t.x0,
                Y0 = t.y0,
                X1 = t.x1,
                Y1 = t.y1,
                Text = t.word,
                BlockNum = t.blockNo,
                LineNum = t.lineNo,
                WordNum = t.wordNo,
            }).ToList();

        /// <summary>MuPDF.NET <c>GetText("blocks")</c> / <c>GetTextBlocks</c> return type.</summary>
        internal static List<TextBlock> TextBlocksFromTuples(
            List<(float x0, float y0, float x1, float y1, string text, int blockNo, int blockType)> rows) =>
            rows.Select(t => new TextBlock
            {
                X0 = t.x0,
                Y0 = t.y0,
                X1 = t.x1,
                Y1 = t.y1,
                Text = t.text,
                BlockNum = t.blockNo,
                Type = t.blockType,
            }).ToList();

        static string DelimitersToString(object delimiters)
        {
            if (delimiters == null)
                return null;
            if (delimiters is string s)
                return s;
            if (delimiters is char[] ca)
                return new string(ca);
            if (delimiters is IEnumerable<char> chars)
                return new string(chars.ToArray());
            return delimiters.ToString();
        }

        static void ApplyCbToTextpageDict(Dictionary<string, object> val, Rect cb)
        {
            // if cb is not None:
            if (cb != null)
            {
                // val["width"] = cb.width
                // val["height"] = cb.height
                val["width"] = cb.Width;
                val["height"] = cb.Height;
            }
        }

        static PageInfo ExtractDictWithCb(TextPage tp, Rect cb, bool sort)
            => tp.ExtractDict(cb, sort);

        static PageInfo ExtractRawDictWithCb(TextPage tp, Rect cb, bool sort)
            => tp.ExtractRAWDict(cb, sort);

        static string ExtractJsonWithCb(TextPage tp, Rect cb, bool sort)
        {
            // Return 'extractDICT' converted to JSON format.
            var val = tp.ExtractDict(sort);
            ApplyCbToTextpageDict(val, cb);
            return JsonSerializer.Serialize(val, new JsonSerializerOptions { WriteIndented = true });
        }

        static string ExtractRawJsonWithCb(TextPage tp, Rect cb, bool sort)
        {
            var val = tp.ExtractRawDict(sort);
            ApplyCbToTextpageDict(val, cb);
            return JsonSerializer.Serialize(val, new JsonSerializerOptions { WriteIndented = true });
        }

        ///////////////////// Barcode
        /// <summary>
        /// Read barcodes from page.
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="decodeEmbeddedOnly">Decode barcodes only from embedded images in the PDF resources.</param>
        /// <param name="barcodeFormat">Barcode format to decode.</param>
        /// <param name="tryHarder">Spend more time to try to find a barcode; optimize for accuracy, not speed.</param>
        /// <param name="tryInverted">Try to decode as inverted image.</param>
        /// <param name="pureBarcode">Image is a pure monochrome image of a barcode.</param>
        /// <param name="multi">Try to read multi barcodes on page.</param>
        /// <param name="autoRotate">Indicate whether the image should be automatically rotated.
        ///                          Rotation is supported for 90, 180 and 270 degrees.</param>
        public static List<Barcode> ReadBarcodes(
            Page page,
            Rect clip = null,
            bool decodeEmbeddedOnly = false,
            BarcodeFormat barcodeFormat = BarcodeFormat.ALL,
            bool tryHarder = true,
            bool tryInverted = false,
            bool pureBarcode = false,
            bool multi = true,
            bool autoRotate = true
            )
        {
            // if clip is null, use the whole page rect
            if (clip == null)
            {
                clip = new Rect(0, 0, page.Rect.Width, page.Rect.Height);
            }

            List<Barcode> barcodes = new List<Barcode>();

            List<Rect> blockRects = new List<Rect>();

            if (!decodeEmbeddedOnly)
            {
                blockRects.Add(clip); // add the clip rect to the list
            }
            else
            {
                foreach (Block block in page.GetImageInfo())
                {
                    Rect blockRectItem = block.Bbox;
                    if (blockRectItem == null)
                        continue;
                    if (clip.Contains(blockRectItem))
                    {
                        blockRects.Add(blockRectItem);
                    }
                }
            }

            foreach (Rect blockRect in blockRects)
            {
                Rect newRect = blockRect;
                // make space around of clip
                if (blockRect.X0 > 5 && blockRect.Y0 > 5 &&
                    blockRect.X1 < page.Rect.Width - 5 && blockRect.Y1 < page.Rect.Height - 5)
                {
                    newRect = new Rect(blockRect.X0 - 5, blockRect.Y0 - 5, blockRect.X1 + 5, blockRect.Y1 + 5);
                }

                // save the start x and y of the clip
                float startX = (float)newRect.X0;
                float startY = (float)newRect.Y0;

                Config config = new Config();

                Pixmap pxmp = page.GetPixmap(dpi: 200, clip: newRect.IRect);
                byte[] pmBuf = pxmp.ToBytes();

                // Calculate Rect ratio between PDF page and image.
                float imageWidth = pxmp.IRect.Width;
                float imageHeight = pxmp.IRect.Height + 0.01f;
                float pageWidth = (float)newRect.Width;
                float pageHeight = (float)newRect.Height + 0.01f;
                float widthRatio = imageWidth / pageWidth;
                float heightRatio = imageHeight / pageHeight;

                pxmp.Dispose(); // free pixmap memory

                SKBitmap bitmap;
                try
                {
                    bitmap = SKBitmap.Decode(pmBuf);
                }
                catch (Exception e)
                {
                    throw new FileNotFoundException("Resource invalid: " + "(" + e.Message + ")");
                }

                List<Barcode> barcodes2 = ReadBarcodes2(bitmap, barcodeFormat);

                bitmap.Dispose(); // free bitmap memory

                foreach (var result in barcodes2)
                {
                    BarcodePoint[] points = new BarcodePoint[result.ResultPoints.Length];
                    for (int i = 0; i < result.ResultPoints.Length; i++)
                    {
                        // revert the original pdf page ratio
                        points[i] = new BarcodePoint(startX + result.ResultPoints[i].X / widthRatio, startY + result.ResultPoints[i].Y / heightRatio);
                    }

                    Barcode newBarcode = new Barcode(
                        result.Text,
                        result.RawBytes,
                        result.NumBits,
                        points,
                        (BarcodeFormat)result.BarcodeFormat,
                        result.Timestamp
                    );
                    barcodes.Add(newBarcode);
                }
            }

            return barcodes;
        }

        public static bool Intersect(ref SKRectI rect1, SKRectI rect2)
        {
            int left = Math.Max(rect1.Left, rect2.Left);
            int top = Math.Max(rect1.Top, rect2.Top);
            int right = Math.Min(rect1.Right, rect2.Right);
            int bottom = Math.Min(rect1.Bottom, rect2.Bottom);

            if (left < right && top < bottom)
            {
                rect1 = new SKRectI(left, top, right, bottom);
                return true;
            }
            else
            {
                rect1 = new SKRectI(); // Empty rectangle
                return false;
            }
        }

        /// <summary>
        /// Read barcodes from image file.
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="barcodeFormat">Barcode format to decode.</param>
        /// <param name="tryHarder">Spend more time to try to find a barcode; optimize for accuracy, not speed.</param>
        /// <param name="tryInverted">Try to decode as inverted image.</param>
        /// <param name="pureBarcode">Image is a pure monochrome image of a barcode.</param>
        /// <param name="multi">Try to read multi barcodes on page.</param>
        /// <param name="autoRotate">Indicate whether the image should be automatically rotated.
        ///                          Rotation is supported for 90, 180 and 270 degrees.</param>
        public static List<Barcode> ReadBarcodes(
            string imageFile,
            Rect clip = null,
            BarcodeFormat barcodeFormat = BarcodeFormat.ALL,
            bool tryHarder = true,
            bool tryInverted = false,
            bool pureBarcode = false,
            bool multi = true,
            bool autoRotate = true
            )
        {
            SKBitmap bitmap = SKBitmap.Decode(imageFile);

            if (clip != null)
            {
                // Define the region you want to crop (x, y, width, height)
                SKRectI cropRect = new SKRectI((int)clip.X0, (int)clip.Y0, (int)clip.X1, (int)clip.Y1);

                // Ensure cropRect is within bounds
                bool didIntersect = Intersect(ref cropRect, new SKRectI(0, 0, bitmap.Width, bitmap.Height));

                // Create a new bitmap to hold the clipped region
                var clippedBitmap = new SKBitmap(cropRect.Width, cropRect.Height);

                // Copy the subset into the new bitmap
                if (bitmap.ExtractSubset(clippedBitmap, cropRect))
                {
                    List<Barcode> clippedResult = ReadBarcodes2(clippedBitmap, barcodeFormat);
                    clippedBitmap.Dispose(); // Dispose of the clipped bitmap
                    bitmap.Dispose(); // Dispose of the original bitmap
                    return clippedResult;
                }
            }

            List<Barcode> barcodes = ReadBarcodes2(bitmap, barcodeFormat);

            bitmap.Dispose();

            return barcodes;
        }

        public static List<Barcode> ReadBarcodes2(SKBitmap bitmap, BarcodeFormat barcodeFormat = BarcodeFormat.ALL)
        {
            List<string> barcodeTypeList = new List<string>();
            string barcodeType = null;

            List<Barcode> barcodes = new List<Barcode>();

            switch (barcodeFormat)
            {
                case BarcodeFormat.AZTEC: barcodeType = "AZTEC"; break;
                case BarcodeFormat.BOXES: barcodeType = "BOXES"; break;
                case BarcodeFormat.CODABAR: barcodeType = "CODABAR"; break;
                case BarcodeFormat.CODABLOCKF: barcodeType = "CODABLOCKF"; break;
                case BarcodeFormat.CODE128: barcodeType = "CODE128"; break;
                case BarcodeFormat.CODE16K: barcodeType = "CODE16K"; break;
                case BarcodeFormat.CODE39: barcodeType = "CODE39"; break;
                case BarcodeFormat.CODE39_LINEARREADER: barcodeType = "CODE39_LINEARREADER"; break;
                case BarcodeFormat.CODE39_EX: barcodeType = "CODE39_EX"; break;
                case BarcodeFormat.CODE39_NOISE1: barcodeType = "CODE39_NOISE1"; break;
                case BarcodeFormat.CODE93: barcodeType = "CODE93"; break;
                case BarcodeFormat.DM: barcodeType = "DM"; break;
                case BarcodeFormat.DM_DPM: barcodeType = "DM_DPM"; break;
                case BarcodeFormat.EAN13: barcodeType = "EAN13"; break;
                case BarcodeFormat.EAN2: barcodeType = "EAN2"; break;
                case BarcodeFormat.EAN5: barcodeType = "EAN5"; break;
                case BarcodeFormat.EAN8: barcodeType = "EAN8"; break;
                case BarcodeFormat.EAN_UPC_OLD: barcodeType = "EAN_UPC_OLD"; break;
                case BarcodeFormat.GS1DATABAREXP: barcodeType = "GS1DATABAREXP"; break;
                case BarcodeFormat.GS1DATABAREXPSTACKED: barcodeType = "GS1DATABAREXPSTACKED"; break;
                case BarcodeFormat.GS1DATABAROMNI: barcodeType = "GS1DATABAROMNI"; break;
                case BarcodeFormat.GS1DATABARSTACKED: barcodeType = "GS1DATABARSTACKED"; break;
                case BarcodeFormat.GS1DATABARSTACKEDOMNI: barcodeType = "GS1DATABARSTACKEDOMNI"; break;
                case BarcodeFormat.GS1DATABARLIMITED: barcodeType = "GS1DATABARLIMITED"; break;
                case BarcodeFormat.HORIZONTALLINES: barcodeType = "HORIZONTALLINES"; break;
                case BarcodeFormat.I2OF5: barcodeType = "I2OF5"; break;
                case BarcodeFormat.IM: barcodeType = "IM"; break;
                case BarcodeFormat.KIX: barcodeType = "KIX"; break;
                case BarcodeFormat.LINETABLES: barcodeType = "LINETABLES"; break;
                case BarcodeFormat.MAXICODE: barcodeType = "MAXICODE"; break;
                case BarcodeFormat.MICR: barcodeType = "MICR"; break;
                case BarcodeFormat.MICROPDF: barcodeType = "MICROPDF"; break;
                case BarcodeFormat.MSI: barcodeType = "MSI"; break;
                case BarcodeFormat.OMRCIRCLE: barcodeType = "OMRCIRCLE"; break;
                case BarcodeFormat.OMRCIRCLE_EXT: barcodeType = "OMRCIRCLE_EXT"; break;
                case BarcodeFormat.OMROVAL: barcodeType = "OMROVAL"; break;
                case BarcodeFormat.OMROVAL_EXT: barcodeType = "OMROVAL_EXT"; break;
                case BarcodeFormat.OMRSQUARE: barcodeType = "OMRSQUARE"; break;
                case BarcodeFormat.OMRSQUARE_EXT: barcodeType = "OMRSQUARE_EXT"; break;
                case BarcodeFormat.OMRSQUARELPATTERN: barcodeType = "OMRSQUARELPATTERN"; break;
                case BarcodeFormat.OMRRECTANGLE: barcodeType = "OMRRECTANGLE"; break;
                case BarcodeFormat.OMRRECTANGLE_EXT: barcodeType = "OMRRECTANGLE_EXT"; break;
                case BarcodeFormat.OMRRECTANGLELPATTERNVERT: barcodeType = "OMRRECTANGLELPATTERNVERT"; break;
                case BarcodeFormat.OMRRECTANGLELPATTERNHORIZ: barcodeType = "OMRRECTANGLELPATTERNHORIZ"; break;
                case BarcodeFormat.PATCH: barcodeType = "PATCH"; break;
                case BarcodeFormat.PHARMA: barcodeType = "PHARMA"; break;
                case BarcodeFormat.PDF417: barcodeType = "PDF417"; break;
                case BarcodeFormat.POSTCODE: barcodeType = "POSTCODE"; break;
                case BarcodeFormat.POSTNET: barcodeType = "POSTNET"; break;
                case BarcodeFormat.QR: barcodeType = "QR"; break;
                case BarcodeFormat.RAWOMR: barcodeType = "RAWOMR"; break;
                case BarcodeFormat.RM: barcodeType = "RM"; break;
                case BarcodeFormat.VERTICALLINES: barcodeType = "VERTICALLINES"; break;
                case BarcodeFormat.UPC_A: barcodeType = "UPC_A"; break;
                case BarcodeFormat.UPC_E: barcodeType = "UPC_E"; break;
                case BarcodeFormat.TRIOPTIC: barcodeType = "TRIOPTIC"; break;
                case BarcodeFormat.ALL: barcodeType = ""; break;
                default:
                    throw new NotSupportedException($"Barcode format {barcodeFormat} is not supported.");
            }

            if (string.IsNullOrEmpty(barcodeType))
            {
                // Add all supported formats when ALL is selected
                foreach (BarcodeFormat val in Enum.GetValues(typeof(BarcodeFormat)))
                {
                    if (val != BarcodeFormat.ALL &&
                        val != BarcodeFormat.BOXES &&
                        val != BarcodeFormat.RAWOMR &&
                        val != BarcodeFormat.HORIZONTALLINES &&
                        val != BarcodeFormat.VERTICALLINES)
                        barcodeTypeList.Add(val.ToString());
                }
            }
            else
            {
                barcodeTypeList.Add(barcodeType);
            }

            foreach (string barcodetype in barcodeTypeList)
            {
                BarcodeReader reader = new BarcodeReader(barcodetype, null);

                try
                {
                    string[] foundBarcodes = null;
                    SKRect[] foundBarcodesRectangles = null;

                    // string with all barcode results for this image
                    // 2nd and further barcodes are added separated by the new line
                    StringBuilder decodingResults = new StringBuilder();

                    bool decoderSuccess = reader.Decode(bitmap);

                    foundBarcodes = reader.GetFoundBarcodesAsStrings();
                    foundBarcodesRectangles = reader.GetFoundBarcodesAsRectangles();

                    if (decoderSuccess && foundBarcodes != null && foundBarcodes.Length > 0)
                    {
                        for (int i = 0; i < foundBarcodes.Length; i++)
                        {
                            string text = foundBarcodes[i];
                            byte[] rawBytes = Encoding.UTF8.GetBytes(text);
                            SKRect rect = foundBarcodesRectangles[i];
                            BarcodePoint[] resultPoints = new BarcodePoint[]
                                {
                                new BarcodePoint(rect.Left, rect.Left),
                                new BarcodePoint(rect.Left + rect.Width, rect.Top),
                                new BarcodePoint(rect.Left + rect.Width, rect.Top + rect.Height),
                                new BarcodePoint(rect.Left, rect.Top + rect.Height)
                                };

                            BarcodeFormat resultFormat = (BarcodeFormat)Enum.Parse(typeof(BarcodeFormat), barcodetype);
                            Barcode barcode = new Barcode(text, rawBytes, resultPoints, resultFormat);

                            barcodes.Add(barcode);
                        }
                    }
                    else
                    {
                        decodingResults.AppendLine("No barcodes found or decoding failed.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error decoding barcode: " + ex.Message);
                }
            }

            return barcodes;
        }

        /// <summary>
        /// Write barcode to pdf page.
        /// </summary>
        /// <param name="clip">Rect area on page to write</param>
        /// <param name="text">Contents to write</param>
        /// <param name="barcodeFormat">Barcode format to encode; Supported types: QR_CODE, EAN_8, EAN_13, UPC_A, CODE_39, CODE_128, ITF, PDF_417, CODABAR</param>
        /// <param name="characterSet">Use a specific character set for binary encoding (if supported by the selected barcode format)</param>
        /// <param name="disableEci">Don't generate ECI segment if non-default character set is used</param>
        /// <param name="forceFitToRect">Resize output barcode image width/height into clip region</param>
        /// <param name="pureBarcode">Don't put the content string into the output image</param>
        /// <param name="marginLeft">Specifies margin left, in pixels, to use when generating the barcode</param>
        /// <param name="marginTop">Specifies margin top, in pixels, to use when generating the barcode</param>
        /// <param name="marginRight">Specifies margin right, in pixels, to use when generating the barcode</param>
        /// <param name="marginBottom">Specifies margin bottom, in pixels, to use when generating the barcode</param>
        /// <param name="narrowBarWidth">The width of the narrow bar in pixels</param>
        public static void WriteBarcode(
            Page page,
            Rect clip,
            string text,
            BarcodeFormat barcodeFormat,
            string characterSet = null,
            bool disableEci = false,
            bool forceFitToRect = false,
            bool pureBarcode = false,
            int marginLeft = 0,
            int marginTop = 0,
            int marginRight = 0,
            int marginBottom = 0,
            int narrowBarWidth = 0
            )
        {
            if (clip == null)
            {
                throw new Exception("Rect is required");
            }
            if (text == null)
            {
                throw new Exception("Text is required");
            }

            int width = (int)clip.Width;
            int height = (int)clip.Height;

            if (width <= 0)
            {
                throw new Exception("Invalid width");
            }

            // get image format from file extension
            SKEncodedImageFormat imageFormat = SKEncodedImageFormat.Png;

            // barcode format
            string barcodeType = null;
            switch (barcodeFormat)
            {
                case BarcodeFormat.AZTEC: barcodeType = "AZTEC"; break;
                case BarcodeFormat.CODABAR: barcodeType = "CODABAR"; break;
                case BarcodeFormat.CODE128: barcodeType = "CODE128"; break;
                case BarcodeFormat.CODE39: barcodeType = "CODE39"; break;
                case BarcodeFormat.CODE93: barcodeType = "CODE93"; break;
                case BarcodeFormat.DM: barcodeType = "DM"; break;
                case BarcodeFormat.EAN2: barcodeType = "EAN2"; break;
                case BarcodeFormat.EAN5: barcodeType = "EAN5"; break;
                case BarcodeFormat.EAN8: barcodeType = "EAN8"; break;
                case BarcodeFormat.EAN13: barcodeType = "EAN14"; break;
                case BarcodeFormat.GS1DATABAREXP: barcodeType = "GS1DATABAREXP"; break;
                case BarcodeFormat.GS1DATABAREXPSTACKED: barcodeType = "GS1DATABAREXPSTACKED"; break;
                case BarcodeFormat.GS1DATABAROMNI: barcodeType = "GS1DATABAROMNI"; break;
                case BarcodeFormat.GS1DATABARSTACKED: barcodeType = "GS1DATABARSTACKED"; break;
                case BarcodeFormat.GS1DATABARSTACKEDOMNI: barcodeType = "GS1DATABARSTACKEDOMNI"; break;
                case BarcodeFormat.GS1DATABARLIMITED: barcodeType = "GS1DATABARLIMITED"; break;
                case BarcodeFormat.I2OF5: barcodeType = "I2OF5"; break;
                case BarcodeFormat.IM: barcodeType = "IM"; break;
                case BarcodeFormat.MAXICODE: barcodeType = "MAXICODE"; break;
                case BarcodeFormat.MSI: barcodeType = "MSI"; break;
                case BarcodeFormat.PHARMA: barcodeType = "PHARMA"; break;
                case BarcodeFormat.PDF417: barcodeType = "PDF417"; break;
                case BarcodeFormat.POSTNET: barcodeType = "POSTNET"; break;
                case BarcodeFormat.QR: barcodeType = "QR"; break;
                case BarcodeFormat.RM: barcodeType = "RM"; break;
                case BarcodeFormat.UPC_A: barcodeType = "UPC_A"; break;
                case BarcodeFormat.UPC_E: barcodeType = "UPC_E"; break;
                default:
                    throw new NotSupportedException($"Barcode format {barcodeFormat} is not supported.");
            }

            BarcodeWriter barcodeWriter = new BarcodeWriter(barcodeType);

            SKBitmap barcodeImage = barcodeWriter.Encode(
                text,
                imageFormat,
                width,
                height,
                characterSet,
                disableEci,
                false,
                pureBarcode,
                marginLeft, marginTop, marginRight, marginBottom, 0, narrowBarWidth);

            // resize image to fit into clip region
            if (forceFitToRect)
            {
                int newHeigth = barcodeImage.Height * width / barcodeImage.Width;
                SKBitmap resizedBitmap = new SKBitmap(width, newHeigth);
                using (SKCanvas canvas = new SKCanvas(resizedBitmap))
                {
                    canvas.DrawBitmap(barcodeImage, new SKRect(0, 0, width, newHeigth));
                }
                barcodeImage.Dispose();
                barcodeImage = resizedBitmap;
            }

            Rect rect = new Rect(clip.X0, clip.Y0, clip.X0 + barcodeImage.Width, clip.Y0 + barcodeImage.Height);

            MemoryStream ms = new MemoryStream();
            using (SKData data = barcodeImage.Encode(SKEncodedImageFormat.Png, 100))
            {
                data.SaveTo(ms);
                ms.Position = 0; // Reset stream position
                page.InsertImage(rect, stream: ms.ToArray());
            }

            barcodeImage.Dispose();
        }

        /// <summary>
        /// Return pixmap of barcode image.
        /// </summary>
        /// <param name="text">Contents to write</param>
        /// <param name="barcodeFormat">Barcode format to encode; Supported types: QR_CODE, EAN_8, EAN_13, UPC_A, CODE_39, CODE_128, ITF, PDF_417, CODABAR</param>
        /// <param name="width">Width of barcode</param>
        /// <param name="characterSet">Use a specific character set for binary encoding (if supported by the selected barcode format)</param>
        /// <param name="disableEci">Don't generate ECI segment if non-default character set is used</param>
        /// <param name="pureBarcode">Don't put the content string into the output image</param>
        /// <param name="marginLeft">Specifies margin left, in pixels, to use when generating the barcode</param>
        /// <param name="marginTop">Specifies margin top, in pixels, to use when generating the barcode</param>
        /// <param name="marginRight">Specifies margin right, in pixels, to use when generating the barcode</param>
        /// <param name="marginBottom">Specifies margin bottom, in pixels, to use when generating the barcode</param>
        /// <param name="narrowBarWidth">The width of the narrow bar in pixels</param>
        public static Pixmap GetBarcodePixmap(
            string text,
            BarcodeFormat barcodeFormat,
            int width = 0,
            string characterSet = null,
            bool disableEci = false,
            bool pureBarcode = false,
            int marginLeft = 0,
            int marginTop = 0,
            int marginRight = 0,
            int marginBottom = 0,
            int narrowBarWidth = 0
            )
        {
            if (text == null)
            {
                throw new Exception("Text is required");
            }

            // get image format from file extension
            SKEncodedImageFormat imageFormat = SKEncodedImageFormat.Png;

            // barcode format
            string barcodeType = null;
            switch (barcodeFormat)
            {
                case BarcodeFormat.AZTEC: barcodeType = "AZTEC"; break;
                case BarcodeFormat.CODABAR: barcodeType = "CODABAR"; break;
                case BarcodeFormat.CODE128: barcodeType = "CODE128"; break;
                case BarcodeFormat.CODE39: barcodeType = "CODE39"; break;
                case BarcodeFormat.CODE93: barcodeType = "CODE93"; break;
                case BarcodeFormat.DM: barcodeType = "DM"; break;
                case BarcodeFormat.EAN2: barcodeType = "EAN2"; break;
                case BarcodeFormat.EAN5: barcodeType = "EAN5"; break;
                case BarcodeFormat.EAN8: barcodeType = "EAN8"; break;
                case BarcodeFormat.EAN13: barcodeType = "EAN14"; break;
                case BarcodeFormat.GS1DATABAREXP: barcodeType = "GS1DATABAREXP"; break;
                case BarcodeFormat.GS1DATABAREXPSTACKED: barcodeType = "GS1DATABAREXPSTACKED"; break;
                case BarcodeFormat.GS1DATABAROMNI: barcodeType = "GS1DATABAROMNI"; break;
                case BarcodeFormat.GS1DATABARSTACKED: barcodeType = "GS1DATABARSTACKED"; break;
                case BarcodeFormat.GS1DATABARSTACKEDOMNI: barcodeType = "GS1DATABARSTACKEDOMNI"; break;
                case BarcodeFormat.GS1DATABARLIMITED: barcodeType = "GS1DATABARLIMITED"; break;
                case BarcodeFormat.I2OF5: barcodeType = "I2OF5"; break;
                case BarcodeFormat.IM: barcodeType = "IM"; break;
                case BarcodeFormat.MAXICODE: barcodeType = "MAXICODE"; break;
                case BarcodeFormat.MSI: barcodeType = "MSI"; break;
                case BarcodeFormat.PHARMA: barcodeType = "PHARMA"; break;
                case BarcodeFormat.PDF417: barcodeType = "PDF417"; break;
                case BarcodeFormat.POSTNET: barcodeType = "POSTNET"; break;
                case BarcodeFormat.QR: barcodeType = "QR"; break;
                case BarcodeFormat.RM: barcodeType = "RM"; break;
                case BarcodeFormat.UPC_A: barcodeType = "UPC_A"; break;
                case BarcodeFormat.UPC_E: barcodeType = "UPC_E"; break;
                default:
                    throw new NotSupportedException($"Barcode format {barcodeFormat} is not supported.");
            }

            BarcodeWriter barcodeWriter = new BarcodeWriter(barcodeType);

            SKBitmap barcodeImage = barcodeWriter.Encode(
                text,
                imageFormat,
                width,
                0,
                characterSet,
                disableEci,
                false,
                pureBarcode,
                marginLeft, marginTop, marginRight, marginBottom, 0, narrowBarWidth);

            // resize image to fit into clip region
            if (width > 0)
            {
                int newHeight = barcodeImage.Height * width / barcodeImage.Width;
                SKBitmap resizedBitmap = new SKBitmap(width, newHeight);
                using (SKCanvas canvas = new SKCanvas(resizedBitmap))
                {
                    canvas.DrawBitmap(barcodeImage, new SKRect(0, 0, width, newHeight));
                }
                barcodeImage.Dispose();
                barcodeImage = resizedBitmap;
            }

            MemoryStream ms = new MemoryStream();
            using (SKData data = barcodeImage.Encode(SKEncodedImageFormat.Png, 100))
            {
                data.SaveTo(ms);
            }

            // Reset position if you want to read from stream
            ms.Position = 0;

            // Example: convert to byte[]
            byte[] bytes = ms.ToArray();

            Pixmap pixmap = new Pixmap(bytes);

            barcodeImage.Dispose();

            return pixmap;
        }

        /// <summary>
        /// Write barcode to image file.
        /// </summary>
        /// <param name="imageFile">Full path of being created barcode image file</param>
        /// <param name="text">Contents to write</param>
        /// <param name="barcodeFormat">Format to encode; Supported formats: QR_CODE, EAN_8, EAN_13, UPC_A, CODE_39, CODE_128, ITF, PDF_417, CODABAR</param>
        /// <param name="width">width of image</param>
        /// <param name="height">height of image</param>
        /// <param name="characterSet">Use a specific character set for binary encoding (if supported by the selected barcode format)</param>
        /// <param name="disableEci">don't generate ECI segment if non-default character set is used</param>
        /// <param name="forceFitToRect">Resize output barcode image width/height with params</param>
        /// <param name="pureBarcode">Don't put the content string into the output image</param>
        /// <param name="marginLeft">Specifies margin left, in pixels, to use when generating the barcode</param>
        /// <param name="marginTop">Specifies margin top, in pixels, to use when generating the barcode</param>
        /// <param name="marginRight">Specifies margin right, in pixels, to use when generating the barcode</param>
        /// <param name="marginBottom">Specifies margin bottom, in pixels, to use when generating the barcode</param>
        /// <param name="narrowBarWidth">The width of the narrow bar in pixels</param>
        public static void WriteBarcode(
            string imageFile,
            string text,
            BarcodeFormat barcodeFormat,
            int width = 300,
            int height = 300,
            string characterSet = null,
            bool disableEci = false,
            bool forceFitToRect = false,
            bool pureBarcode = false,
            int marginLeft = 0,
            int marginTop = 0,
            int marginRight = 0,
            int marginBottom = 0,
            int narrowBarWidth = 0
            )
        {
            if (width <= 0)
            {
                throw new Exception("Invalid width");
            }
            if (text == null)
            {
                throw new Exception("Text is required");
            }

            // get image format from file extension
            SKEncodedImageFormat imageFormat = SKEncodedImageFormat.Png;
            string extension = System.IO.Path.GetExtension(imageFile).ToLower();
            switch (extension)
            {
                case ".bmp":
                    imageFormat = SKEncodedImageFormat.Bmp;
                    break;
                case ".gif":
                    imageFormat = SKEncodedImageFormat.Gif;
                    break;
                case ".ico":
                case ".icon":
                    imageFormat = SKEncodedImageFormat.Ico;
                    break;
                case ".jpeg":
                case ".jpg":
                    imageFormat = SKEncodedImageFormat.Jpeg;
                    break;
                case ".png":
                    imageFormat = SKEncodedImageFormat.Png;
                    break;
                case ".webp":
                    imageFormat = SKEncodedImageFormat.Webp;
                    break;
                default:
                    throw new Exception("Unsupported image format");
            }

            // barcode format
            string barcodeType = null;
            switch (barcodeFormat)
            {
                case BarcodeFormat.AZTEC: barcodeType = "AZTEC"; break;
                case BarcodeFormat.CODABAR: barcodeType = "CODABAR"; break;
                case BarcodeFormat.CODE128: barcodeType = "CODE128"; break;
                case BarcodeFormat.CODE39: barcodeType = "CODE39"; break;
                case BarcodeFormat.CODE93: barcodeType = "CODE93"; break;
                case BarcodeFormat.DM: barcodeType = "DM"; break;
                case BarcodeFormat.EAN2: barcodeType = "EAN2"; break;
                case BarcodeFormat.EAN5: barcodeType = "EAN5"; break;
                case BarcodeFormat.EAN8: barcodeType = "EAN8"; break;
                case BarcodeFormat.EAN13: barcodeType = "EAN14"; break;
                case BarcodeFormat.GS1DATABAREXP: barcodeType = "GS1DATABAREXP"; break;
                case BarcodeFormat.GS1DATABAREXPSTACKED: barcodeType = "GS1DATABAREXPSTACKED"; break;
                case BarcodeFormat.GS1DATABAROMNI: barcodeType = "GS1DATABAROMNI"; break;
                case BarcodeFormat.GS1DATABARSTACKED: barcodeType = "GS1DATABARSTACKED"; break;
                case BarcodeFormat.GS1DATABARSTACKEDOMNI: barcodeType = "GS1DATABARSTACKEDOMNI"; break;
                case BarcodeFormat.GS1DATABARLIMITED: barcodeType = "GS1DATABARLIMITED"; break;
                case BarcodeFormat.I2OF5: barcodeType = "I2OF5"; break;
                case BarcodeFormat.IM: barcodeType = "IM"; break;
                case BarcodeFormat.MAXICODE: barcodeType = "MAXICODE"; break;
                case BarcodeFormat.MSI: barcodeType = "MSI"; break;
                case BarcodeFormat.PHARMA: barcodeType = "PHARMA"; break;
                case BarcodeFormat.PDF417: barcodeType = "PDF417"; break;
                case BarcodeFormat.POSTNET: barcodeType = "POSTNET"; break;
                case BarcodeFormat.QR: barcodeType = "QR"; break;
                case BarcodeFormat.RM: barcodeType = "RM"; break;
                case BarcodeFormat.UPC_A: barcodeType = "UPC_A"; break;
                case BarcodeFormat.UPC_E: barcodeType = "UPC_E"; break;
                default:
                    throw new NotSupportedException($"Barcode format {barcodeFormat} is not supported.");
            }

            BarcodeWriter barcodeWriter = new BarcodeWriter(barcodeType);

            barcodeWriter.Encode(
                imageFile,
                text,
                imageFormat,
                width,
                height,
                characterSet,
                disableEci,
                forceFitToRect,
                pureBarcode,
                marginLeft, marginTop, marginRight, marginBottom, 0, narrowBarWidth);
        }

        /// <summary>PyMuPDF <c>utils.getLinkText</c> (<c>src/utils.py</c>).</summary>
        public static string GetLinkText(Page page, Dictionary<string, object> lnk)
            => Helpers.GetLinkText(page, lnk);

        // ─── src/__init__.py module functions ─────────────────────────────

        /// <summary>
        /// Build <c>@font-face</c> CSS for pymupdf-fonts (PyMuPDF <c>css_for_pymupdf_font</c>).
        /// </summary>
        public static string CssForPymupdfFont(
            string fontcode,
            string CSS = null,
            Archive archive = null,
            string name = null)
        {
            // @font-face template string
            // CSSFONT = "\n@font-face {font-family: %s; src: url(%s);%s%s}\n"
            const string CSSFONT = "\n@font-face {{font-family: {0}; src: url({1});{2}{3}}}\n";

            // if not type(archive) is Archive:
            if (archive is not Archive)
                throw new ValueErrorException("'archive' must be an Archive");
            // if CSS is None:
            if (CSS == null)
                CSS = "";

            // select font codes starting with the pass-in string
            // font_keys = [k for k in fitz_fontdescriptors.keys() if k.startswith(fontcode)]
            var font_keys = new List<string>();
            foreach (string k in MupdfFonts.FitzFontDescriptors.Keys)
            {
                if (k.StartsWith(fontcode, StringComparison.Ordinal))
                    font_keys.Add(k);
            }
            // if font_keys == []:
            if (font_keys.Count == 0)
                throw new ValueErrorException($"No font code '{fontcode}' found in pymupdf-fonts.");
            // if len(font_keys) > 4:
            if (font_keys.Count > 4)
                throw new ValueErrorException("fontcode too short");
            // if name is None:  # use this name for font-family
            if (name == null)
                name = fontcode;

            // for fkey in font_keys:
            foreach (string fkey in font_keys)
            {
                // font = fitz_fontdescriptors[fkey]
                var font = MupdfFonts.FitzFontDescriptors[fkey];
                // bold = font["bold"]  # determine font property
                bool bold = font.Bold;
                // italic = font["italic"]  # determine font property
                bool italic = font.Italic;
                // fbuff = font["loader"]()  # load the fontbuffer
                byte[] fbuff = font.Loader();
                // archive.add(fbuff, fkey)  # update the archive
                archive.Add(fbuff, fkey);
                // bold_text = "font-weight: bold;" if bold else ""
                string bold_text = bold ? "font-weight: bold;" : "";
                // italic_text = "font-style: italic;" if italic else ""
                string italic_text = italic ? "font-style: italic;" : "";
                // CSS += CSSFONT % (name, fkey, bold_text, italic_text)
                CSS += string.Format(CultureInfo.InvariantCulture, CSSFONT, name, fkey, bold_text, italic_text);
            }
            // return CSS
            return CSS;
        }

        /// <summary>Map Adobe glyph name to Unicode code point (PyMuPDF helper).</summary>
        public static int GlyphNameToUnicode(string name)
        {
            // Convenience function accessing unicodedata.
            // import unicodedata
            int unc;
            try
            {
                unc = ord(unicodedata.Lookup(name));
            }
            catch (Exception)
            {
                unc = 65533;
            }
            return unc;
        }

        /// <summary>Map Unicode code point to Adobe glyph name (PyMuPDF helper).</summary>
        public static string UnicodeToGlyphName(int ch)
        {
            // Convenience function accessing unicodedata.
            // import unicodedata
            string name;
            try
            {
                name = unicodedata.Name(chr(ch));
            }
            catch (ValueErrorException)
            {
                name = ".notdef";
            }
            return name;
        }

        /// <summary>Adobe Glyph List: glyph name to Unicode (legacy <c>GlyphName2Unicode</c>).</summary>
        public static int GlyphName2Unicode(string name) => GlyphNameToUnicode(name);

        /// <summary>Adobe Glyph List: Unicode to glyph name (legacy <c>Unicode2GlyphName</c>).</summary>
        public static string Unicode2GlyphName(int ch) => UnicodeToGlyphName(ch);

        internal static int glyph_name_to_unicode(string name) => GlyphNameToUnicode(name);

        internal static string unicode_to_glyph_name(int ch) => UnicodeToGlyphName(ch);

        private static int ord(string s)
        {
            if (s.Length == 1)
                return s[0];
            return char.ConvertToUtf32(s, 0);
        }

        private static string chr(int ch) => char.ConvertFromUtf32(ch);

        // mirrors: import unicodedata
        private static class unicodedata
        {
            private static readonly Lazy<(Dictionary<string, int> nameToCp, Dictionary<int, string> cpToName)> Data =
                new Lazy<(Dictionary<string, int>, Dictionary<int, string>)>(Load);

            public static string Lookup(string name)
            {
                if (!Data.Value.nameToCp.TryGetValue(name, out int cp))
                    throw new KeyNotFoundException(name);
                return chr(cp);
            }

            public static string Name(string c)
            {
                if (!Data.Value.cpToName.TryGetValue(ord(c), out string? n))
                    throw new ValueErrorException("no name defined");
                return n;
            }

            private static (Dictionary<string, int>, Dictionary<int, string>) Load()
            {
                var nameToCp = new Dictionary<string, int>(StringComparer.Ordinal);
                var cpToName = new Dictionary<int, string>();

                using (var stream = OpenGzipResource("UnicodeData.txt.gz"))
                using (var reader = new StreamReader(stream))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length == 0 || line[0] == '#')
                            continue;
                        string[] parts = line.Split(';');
                        if (parts.Length < 2)
                            continue;
                        if (parts[0].IndexOf("..", StringComparison.Ordinal) >= 0)
                            continue;
                        if (parts[0].IndexOf(' ') >= 0)
                            continue;
                        if (!int.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int cp))
                            continue;

                        string entryName = parts[1];
                        if (entryName.Length > 0 && entryName[0] != '<')
                        {
                            cpToName[cp] = entryName;
                            if (!nameToCp.ContainsKey(entryName))
                                nameToCp[entryName] = cp;
                        }
                        else if (parts.Length > 10 && parts[10].Length > 0)
                        {
                            cpToName[cp] = parts[10];
                        }
                    }
                }

                using (var stream = OpenGzipResource("NameAliases.txt.gz"))
                using (var reader = new StreamReader(stream))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length == 0 || line[0] == '#')
                            continue;
                        string[] parts = line.Split(';');
                        if (parts.Length < 3)
                            continue;
                        if (!int.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int cp))
                            continue;
                        string alias = parts[1];
                        if (alias.Length == 0)
                            continue;
                        if (!nameToCp.ContainsKey(alias))
                            nameToCp[alias] = cp;
                    }
                }

                return (nameToCp, cpToName);
            }

            private static Stream OpenGzipResource(string fileName)
            {
                Assembly asm = typeof(Utils).Assembly;
                foreach (string resourceName in asm.GetManifestResourceNames())
                {
                    if (!resourceName.EndsWith(fileName, StringComparison.Ordinal))
                        continue;
                    Stream? raw = asm.GetManifestResourceStream(resourceName);
                    if (raw == null)
                        break;
                    return new GZipStream(raw, CompressionMode.Decompress);
                }
                throw new InvalidOperationException($"Embedded resource not found: {fileName}");
            }
        }

        /// <summary>Standard empty / invalid rectangle used in text extraction (PyMuPDF <c>EMPTY_RECT</c>).</summary>
        public static Rect EMPTY_RECT() =>
            new Rect(FZ_MAX_INF_RECT, FZ_MAX_INF_RECT, FZ_MIN_INF_RECT, FZ_MIN_INF_RECT);

        /// <summary>Standard empty / invalid quad (PyMuPDF <c>EMPTY_QUAD</c>).</summary>
        public static Quad EMPTY_QUAD() => EMPTY_RECT().Quad;

        static bool IsRtl(string text)
        {
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
                    continue;

                var direction = CharUnicodeInfo.GetUnicodeCategory(c);
                if (direction == UnicodeCategory.OtherLetter ||
                    direction == UnicodeCategory.LetterNumber ||
                    direction == UnicodeCategory.LowercaseLetter ||
                    direction == UnicodeCategory.UppercaseLetter)
                {
                    // Check if it's Hebrew, Arabic, or other RTL scripts
                    if ((c >= '\u0590' && c <= '\u08FF') || // Hebrew, Arabic ranges
                        (c >= '\uFB1D' && c <= '\uFEFC'))   // Hebrew presentation forms, Arabic presentation forms
                    {
                        return true; // RTL
                    }
                    else
                    {
                        return false; // LTR
                    }
                }
            }

            // Default: assume LTR if no strong character found
            return false;
        }

        public static bool IsSameLine(Rect rect0, Rect rect1, float tolerance = 5.0f)
        {
            if (rect0.IsEmpty || rect1.IsEmpty)
                return false;

            if (rect0.Y0 <= rect1.Y0)
            {
                if (rect0.Y1 <= rect1.Y0)
                {
                    return false;
                }
                else
                {
                    if (rect0.Y1 >= rect1.Y1)
                        return true;
                    else
                    {
                        float overlappedHeight = rect0.Y1 - rect1.Y0;
                        if (overlappedHeight < rect1.Height / 2)
                            return false;
                        else
                            return true;
                    }
                }
            }
            else
            {
                if (rect1.Y1 <= rect0.Y0)
                {
                    return false;
                }
                else
                {
                    if (rect1.Y1 >= rect0.Y1)
                        return true;
                    else
                    {
                        float overlappedHeight = rect1.Y1 - rect0.Y0;
                        if (overlappedHeight < rect1.Height / 2)
                            return false;
                        else
                            return true;
                    }
                }
            }
            return false;
        }

        public static int GetBlankLines(Rect rect0, Rect rect1)
        {
            float distanceBetweenRect = 0f;
            float minLineHeight = Math.Min(rect0.Height, rect1.Height);

            if (rect0.Y1 < rect1.Y0)
                distanceBetweenRect = rect1.Y0 - rect0.Y1;
            else if (rect1.Y1 < rect0.Y0)
                distanceBetweenRect = rect0.Y0 - rect1.Y1;

            if (minLineHeight > 0f)
                return (int)(distanceBetweenRect / minLineHeight);

            return 0;
        }

        public static bool IsHorizontalNeighbors(Rect rect0, Rect rect1, float yTolerance = 5.0f, float xGapTolerance = 10.0f)
        {
            // Check if they're on the same line (vertically aligned)
            bool sameLine = IsSameLine(rect0, rect1, yTolerance);

            // Check if they are next to each other (b is to the right of a, or vice versa)
            bool aBeforeB = rect0.X1 <= rect1.X0 && (rect1.X0 - rect0.X1) < xGapTolerance;
            bool bBeforeA = rect1.X1 <= rect0.X0 && (rect0.X0 - rect1.X1) < xGapTolerance;

            return sameLine && (aBeforeB || bBeforeA);
        }

        public static string GetTextWithLayout(Page page, Rect clip = null, int flags = 0, int tolerance = 5)
        {
            string LineText(Rect _clip, List<(Rect, string)> _line, int _tolerance)
            {
                _line.Sort((l1, l2) =>
                {
                    return (int)((l1.Item1.X0 - l2.Item1.X0) * 10);
                });
                string _ltext = "";
                Rect _prevRect = Utils.EMPTY_RECT();

                string LRM = "\u200E"; // Left-to-Right Mark
                string RLM = "\u200F"; // Right-to-Left Mark
                bool prevIsRLM = false;

                foreach ((Rect r, string t) in _line)
                {
                    int spaceCount = 0;
                    if (_prevRect.IsEmpty == true)
                        spaceCount = (int)Math.Max(0, (r.X0) / _tolerance);
                    else
                    {
                        float dist = r.X0 - _prevRect.X1;
                        spaceCount = (int)Math.Max(0, dist / _tolerance);
                        if (spaceCount > 1)
                            spaceCount = (int)(r.X0) / _tolerance - _ltext.Length;
                        else if (dist > 1)
                            spaceCount = 1;
                    }
                    if (spaceCount < 0)
                    {
                        spaceCount = 0;
                    }
                    _prevRect = r;
                    _ltext += new string(' ', spaceCount);
                    if (Utils.IsRtl(t))
                    {
                        if (!prevIsRLM)
                            _ltext += RLM;
                        else
                            _ltext += LRM;
                        prevIsRLM = true;
                    }
                    else
                    {
                        if (prevIsRLM)
                            _ltext += LRM;
                        prevIsRLM = false;
                    }


                    // Replace all groups of 1 or more spaces with a thin space
                    string t1 = t.Replace("     ", " ");
                    _ltext += t1;
                }

                return _ltext;
            }

            // check parameters
            if (flags == 0)
            {
                flags = (int)(
                    TextFlags.TEXT_PRESERVE_WHITESPACE
                    | TextFlags.TEXT_PRESERVE_LIGATURES
                    | TextFlags.TEXT_MEDIABOX_CLIP
                    //| TextFlags.TEXT_PRESERVE_IMAGES
                    | TextFlags.TEXT_INHIBIT_SPACES
                    //| TextFlags.TEXT_DEHYPHENATE
                    | TextFlags.TEXT_PRESERVE_SPANS
                    | TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE
                    | TextFlags.TEXT_ACCURATE_BBOXES
                );
            }
            if (clip == null)
                clip = page.Rect;
            if (tolerance <= 0)
                tolerance = 5;

            List<(Rect, string)>[] words = new List<(Rect, string)>[2];
            words[0] = new List<(Rect, string)>();
            words[1] = new List<(Rect, string)>();

            foreach (Block block in page.GetText("dict", clip: clip, flags: flags, sort: true).Blocks)
            {
                if (block.Lines != null)
                {
                    foreach (Line _line in block.Lines)
                    {
                        if (_line == null)
                            continue;
                        foreach (Span span in _line.Spans)
                        {
                            // horizontal
                            Rect spanRect = new Rect(span.Bbox.X0, span.Bbox.Y0, span.Bbox.X1, span.Bbox.Y1);
                            if (page.Rotation != 0)
                                spanRect = spanRect.Transform(page.RotationMatrix);
                            if (_line.Dir.Y == 0)
                                words[0].Add((spanRect, span.Text));
                            else
                                words[1].Add((spanRect, span.Text));
                        }
                    }
                }
            }

            // sort again.
            words[0].Sort((w1, w2) =>
            {
                if (Utils.IsSameLine(w1.Item1, w2.Item1))
                {
                    return w1.Item1.X0.CompareTo(w2.Item1.X0);
                }
                else
                {
                    float center1 = (w1.Item1.Y0 + w1.Item1.Y1) / 2;
                    float center2 = (w2.Item1.Y0 + w2.Item1.Y1) / 2;

                    return center1.CompareTo(center2);
                }
            });
            words[1].Sort((w1, w2) =>
            {
                if (Utils.IsSameLine(w1.Item1, w2.Item1))
                {
                    return w1.Item1.X0.CompareTo(w2.Item1.X0);
                }
                else
                {
                    float center1 = (w1.Item1.Y0 + w1.Item1.Y1) / 2;
                    float center2 = (w2.Item1.Y0 + w2.Item1.Y1) / 2;

                    return center1.CompareTo(center2);
                }
            });

            string ret = "";
            for (int i = 0; i < 2; i++)
            {
                ret += "\n";
                if (words[i].Count == 0)
                {
                    continue;
                }
                Rect totalBox = Utils.EMPTY_RECT();
                foreach ((Rect wr, string text) in words[i])
                    totalBox = totalBox | wr;

                List<(Rect, string)> lines = new List<(Rect, string)>();
                List<(Rect, string)> line = new List<(Rect, string)>() { words[i][0] };
                Rect lrect = new Rect(words[i][0].Item1);
                string ltext = "";

                foreach ((Rect wr, string text) in words[i].Skip(1))
                {
                    (Rect w0r, string _) = line[line.Count - 1];

                    if (w0r.EqualTo(wr))
                    {
                        continue;
                    }

                    if (w0r.X0 < wr.X0 && Utils.IsSameLine(w0r, wr))
                    {
                        line.Add((wr, text));
                        lrect |= wr;
                    }
                    else
                    {
                        ltext = LineText(totalBox, line, tolerance);
                        lines.Add((lrect, ltext));
                        line = new List<(Rect, string)>() { (wr, text) };
                        lrect = new Rect(wr);
                    }
                }

                ltext = LineText(totalBox, line, tolerance);
                lines.Add((lrect, ltext));

                lines.Sort((l1, l2) => { return (int)((l1.Item1.Y1 - l2.Item1.Y1) * 10); });

                ret += lines[0].Item2;
                float x0 = lines[0].Item1.X0;
                float y1 = lines[0].Item1.Y1;

                foreach ((Rect lr, string lt) in lines.Skip(1))
                {
                    int distance = Math.Min((int)(Math.Round((lr.Y0 - y1) / lr.Height)), tolerance);
                    if (distance < 0)
                    {
                        distance = 0;
                    }
                    string breaks = new String('\n', distance + 1);
                    ret += breaks + lt;
                    x0 = lr.X0;
                    y1 = lr.Y1;
                }
            }

            return ret;
        }

        /// <summary>
        /// Find tables on a page (MuPDF.NET <c>Utils.GetTables</c> / PyMuPDF <c>utils.get_tables</c>).
        /// </summary>
        public static List<Table> GetTables(
            Page page,
            Rect clip = null,
            string vertical_strategy = "lines",
            string horizontal_strategy = "lines",
            List<Edge> vertical_lines = null,
            List<Edge> horizontal_lines = null,
            float snap_tolerance = TableFlags.TABLE_DEFAULT_SNAP_TOLERANCE,
            float snap_x_tolerance = 0.0f,
            float snap_y_tolerance = 0.0f,
            float join_tolerance = TableFlags.TABLE_DEFAULT_JOIN_TOLERANCE,
            float join_x_tolerance = 0.0f,
            float join_y_tolerance = 0.0f,
            float edge_min_length = 3.0f,
            float min_words_vertical = TableFlags.TABLE_DEFAULT_MIN_WORDS_VERTICAL,
            float min_words_horizontal = TableFlags.TABLE_DEFAULT_MIN_WORDS_HORIZONTAL,
            float intersection_tolerance = 3.0f,
            float intersection_x_tolerance = 0.0f,
            float intersection_y_tolerance = 0.0f,
            float text_tolerance = 3.0f,
            float text_x_tolerance = 3.0f,
            float text_y_tolerance = 3.0f,
            string strategy = null,
            List<Line> add_lines = null)
        {
            if (page == null)
                return new List<Table>();

            var finder = TableHelpers.FindTables(
                page,
                clip: clip,
                verticalStrategy: vertical_strategy,
                horizontalStrategy: horizontal_strategy,
                verticalLines: vertical_lines?.Select(v => v.x0).ToList(),
                horizontalLines: horizontal_lines?.Select(h => h.y0).ToList(),
                snapTolerance: snap_tolerance,
                snapXTolerance: snap_x_tolerance == 0.0f ? (float?)null : snap_x_tolerance,
                snapYTolerance: snap_y_tolerance == 0.0f ? (float?)null : snap_y_tolerance,
                joinTolerance: join_tolerance,
                joinXTolerance: join_x_tolerance == 0.0f ? (float?)null : join_x_tolerance,
                joinYTolerance: join_y_tolerance == 0.0f ? (float?)null : join_y_tolerance,
                edgeMinLength: edge_min_length,
                minWordsVertical: min_words_vertical,
                minWordsHorizontal: min_words_horizontal,
                intersectionTolerance: intersection_tolerance,
                intersectionXTolerance: intersection_x_tolerance == 0.0f ? (float?)null : intersection_x_tolerance,
                intersectionYTolerance: intersection_y_tolerance == 0.0f ? (float?)null : intersection_y_tolerance,
                textTolerance: text_tolerance,
                textXTolerance: text_x_tolerance,
                textYTolerance: text_y_tolerance,
                strategy: strategy);

            return finder?.Tables ?? new List<Table>();
        }

        // ─── PyMuPDF API names (internal, same assembly) ─────────────────

        internal static string get_pdf_now() => GetPdfNow();
        internal static string getPDFnow() => GetPdfNow();
        internal static string get_pdf_str(string s) => GetPdfStr(s);
        internal static string getPDFstr(string s) => GetPdfStr(s);
        internal static float get_text_length(string text, string fontname = "helv", float fontsize = 11, int encoding = 0)
            => GetTextLength(text, fontname, fontsize, encoding);
        internal static float getTextlength(string text, string fontname = "helv", float fontsize = 11, int encoding = 0)
            => GetTextLength(text, fontname, fontsize, encoding);
        internal static Dictionary<string, object> image_profile(object img, int keep_image = 0) => ImageProperties(img, keep_image);
        internal static Rect paper_rect(string s) => PaperRect(s);
        internal static (float w, float h) paper_size(string s) => PaperSize(s);
        internal static Dictionary<string, (float w, float h)> paper_sizes() => PaperSizes();
        internal static Matrix planish_line(Point p1, Point p2) => PlanishLine(p1, p2);
        internal static Rect empty_rect() => EMPTY_RECT();
        internal static Quad empty_quad() => EMPTY_QUAD();
        internal static string css_for_pymupdf_font(string fontcode, string CSS = null, Archive archive = null, string name = null)
            => CssForPymupdfFont(fontcode, CSS, archive, name);
        internal static object get_text(
            Page page,
            string option = "text",
            IRect clip = null,
            int? flags = null,
            TextPage textpage = null,
            bool sort = false,
            object delimiters = null,
            float tolerance = 3)
            => GetText(page, option, clip, flags, textpage, sort, delimiters, tolerance);

        internal static object get_text(
            Annot annot,
            string option = "text",
            IRect clip = null,
            int? flags = null,
            TextPage textpage = null,
            bool sort = false,
            object delimiters = null,
            float tolerance = 3)
            => GetText(annot, option, clip, flags, textpage, sort, delimiters, tolerance);

        /// <summary>Follow a chain of dictionary keys (e.g. Root → AcroForm → Fields).</summary>
        public static mupdf.PdfObj pdf_dict_getl(mupdf.PdfObj obj, string[] keys)
        {
            if (keys == null)
                return obj;
            foreach (string key in keys)
            {
                if (obj?.m_internal == null)
                    break;
                obj = obj.pdf_dict_get(new mupdf.PdfObj(key));
            }
            return obj;
        }

        /// <summary>MuPDF.NET helper for PDF name/string values.</summary>
        public static string UnicodeFromStr(object s)
        {
            if (s == null)
                return string.Empty;
            if (s is byte[] bytes)
                return Encoding.UTF8.GetString(bytes);
            if (s is string str)
                return str;
            return s.ToString() ?? string.Empty;
        }
    }
}
