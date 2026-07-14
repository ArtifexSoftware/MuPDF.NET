using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MuPDF.NET;
using mupdf;

namespace PDF4LLM.Helpers
{
    /// <summary>Utility functions for PDF processing and layout analysis.</summary>
    public static partial class Utils
    {
        // Constants
        public static readonly HashSet<char> WHITE_CHARS = new HashSet<char>(
            Enumerable.Range(0, 33).Select(i => (char)i)
            .Concat(new[]
            {
                '\u00a0',  // Non-breaking space
                '\u2000',  // En quad
                '\u2001',  // Em quad
                '\u2002',  // En space
                '\u2003',  // Em space
                '\u2004',  // Three-per-em space
                '\u2005',  // Four-per-em space
                '\u2006',  // Six-per-em space
                '\u2007',  // Figure space
                '\u2008',  // Punctuation space
                '\u2009',  // Thin space
                '\u200a',  // Hair space
                '\u202f',  // Narrow no-break space
                '\u205f',  // Medium mathematical space
                '\u3000',  // Ideographic space
            })
        );

        public const char REPLACEMENT_CHARACTER = '\uFFFD';
        public const string TYPE3_FONT_NAME = "Type3";
        public const string TESSERACT_FONT_NAME = "GlyphLessFont";

        public static readonly HashSet<char> BULLETS = new HashSet<char>(
            new[]
            {
                '\u002A',  // *
                '\u002D',  // -
                '\u003E',  // >
                '\u006F',  // o
                '\u00B6',  // ¶
                '\u00B7',  // ·
                '\u2010',  // ‐
                '\u2011',  // ‑
                '\u2012',  // ‒
                '\u2013',  // –
                '\u2014',  // —
                '\u2015',  // ―
                '\u2020',  // †
                '\u2021',  // ‡
                '\u2022',  // •
                '\u2212',  // −
                '\u2219',  // ∙
                '\uF0A7',  // Private use
                '\uF0B7',  // Private use
                REPLACEMENT_CHARACTER,
            }
            .Concat(Enumerable.Range(0x25A0, 0x2600 - 0x25A0).Select(i => (char)i))
        );

        public static int FLAGS = (int)(
            mupdf.mupdf.FZ_STEXT_COLLECT_STYLES |
            mupdf.mupdf.FZ_STEXT_COLLECT_VECTORS |
            (int)TextFlags.TEXT_PRESERVE_IMAGES |
            (int)TextFlags.TEXT_ACCURATE_BBOXES |
            (int)TextFlags.TEXT_MEDIABOX_CLIP |
            mupdf.mupdf.FZ_STEXT_IGNORE_ACTUALTEXT
        );

        /// <summary>
        /// Traverse /AcroForm/Fields hierarchy and return a dict:
        /// fully qualified field name -> {"value": ..., "pages": [...]}
        /// Optionally, the xref of the field is included.
        /// Extract form fields with page references (interactive PDFs).
        /// </summary>
        /// <param name="doc">PDF document to scan for AcroForm fields.</param>
        /// <param name="includeXrefs">When <c>true</c>, include each field's PDF xref in the result.</param>
        public static Dictionary<string, Dictionary<string, object>> ExtractFormFieldsWithPages(Document doc, bool includeXrefs = false)
        {
            var result = new Dictionary<string, Dictionary<string, object>>();
            if (doc == null || doc.IsClosed)
                return result;

            PdfDocument pdf = null;
            try
            {
                pdf = Document.AsPdfDocument(doc);
                if (pdf?.m_internal == null)
                    return result;

                PdfObj fields = MuPDF.NET.Utils.pdf_dict_getl(
                    pdf.pdf_trailer(),
                    new[] { "Root", "AcroForm", "Fields" });
                if (fields == null || fields.m_internal == null || fields.pdf_is_array() == 0
                    || fields.pdf_array_len() == 0)
                    return result;

                var pageXrefs = new Dictionary<int, int>();
                for (int i = 0; i < doc.PageCount; i++)
                {
                    try
                    {
                        int xr = doc.GetPageXref(i);
                        if (xr != 0)
                            pageXrefs[xr] = i;
                    }
                    catch
                    {
                        // skip page
                    }
                }

                void WalkField(PdfObj field, string prefix)
                {
                    if (field == null || field.m_internal == null)
                        return;

                    int fieldXref = field.pdf_to_num();
                    PdfObj nameX = field.pdf_dict_get(new PdfObj("T"));
                    string name = nameX != null && nameX.m_internal != null
                        ? nameX.pdf_to_text_string()
                        : null;
                    if (string.IsNullOrEmpty(name))
                        return;

                    string fqName = string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";

                    object value;
                    PdfObj valueX = field.pdf_dict_get(new PdfObj("V"));
                    if (valueX != null && valueX.m_internal != null)
                    {
                        if (valueX.pdf_is_name() != 0)
                            value = MuPDF.NET.Utils.UnicodeFromStr(valueX.pdf_to_name());
                        else
                            value = valueX.pdf_to_text_string();
                    }
                    else
                    {
                        value = null;
                    }

                    var pages = new List<int>();
                    PdfObj kids = field.pdf_dict_get(new PdfObj("Kids"));
                    bool hasKidsArray = kids != null && kids.m_internal != null && kids.pdf_is_array() != 0;
                    int kidsLen = hasKidsArray ? kids.pdf_array_len() : 0;

                    if (hasKidsArray && kidsLen > 0)
                    {
                        for (int ki = 0; ki < kidsLen; ki++)
                        {
                            PdfObj kid = kids.pdf_array_get(ki);
                            if (kid == null || kid.m_internal == null)
                                continue;
                            fieldXref = kid.pdf_to_num();
                            PdfObj pageXrefX = kid.pdf_dict_get(new PdfObj("P"));
                            if (pageXrefX != null && pageXrefX.m_internal != null)
                            {
                                int pageXref = pageXrefX.pdf_to_num();
                                if (pageXref != 0 && pageXrefs.TryGetValue(pageXref, out int pno))
                                    pages.Add(pno);
                            }

                            WalkField(kid, fqName);
                        }
                    }

                    if (kidsLen == 0)
                    {
                        PdfObj pageRefX = field.pdf_dict_get(new PdfObj("P"));
                        if (pageRefX != null && pageRefX.m_internal != null)
                        {
                            int pageXref = pageRefX.pdf_to_num();
                            if (pageXref != 0 && pageXrefs.TryGetValue(pageXref, out int pno))
                                pages.Add(pno);
                        }
                    }

                    var valueDict = new Dictionary<string, object>
                    {
                        ["value"] = value,
                        ["pages"] = pages.Distinct().OrderBy(p => p).ToList()
                    };
                    if (includeXrefs)
                        valueDict["xref"] = fieldXref;
                    result[fqName] = valueDict;
                }

                int nFields = fields.pdf_array_len();
                for (int fi = 0; fi < nFields; fi++)
                {
                    PdfObj field = fields.pdf_array_get(fi);
                    WalkField(field, "");
                }

                return result;
            }
            catch
            {
                return result;
            }
            finally
            {
                pdf?.Dispose();
            }
        }

        /// <summary>
        /// Normalize a folder path ("" = script folder), ensure it exists,
        /// and return a Markdown-safe file reference using forward slashes.
        /// Prefers relative paths to avoid Windows drive-letter issues.
        /// </summary>
        /// <param name="folder">Target directory; empty string uses the current working directory.</param>
        /// <param name="filename">Image file name (only the base name is used).</param>
        public static (string mdRef, string actualPath) MdPath(string folder, string filename)
        {
            // 1. Use current working directory as script dir.
            string scriptDir = Directory.GetCurrentDirectory();
            string basePath;

            if (string.IsNullOrWhiteSpace(folder))
            {
                basePath = scriptDir;
            }
            else
            {
                basePath = Environment.ExpandEnvironmentVariables(folder);
                basePath = Path.GetFullPath(basePath);
            }

            // 2. Create folder if it doesn't exist
            Directory.CreateDirectory(basePath);

            // 3. Build full file path
            string fullPath = Path.Combine(basePath, Path.GetFileName(filename));
            string mdRef;

            // 4. Try to compute a relative path (best for Markdown)
            // Calculate relative path manually for compatibility with .NET Standard 2.0
            // Path.GetRelativePath is only available in .NET Core 2.1+ and .NET Standard 2.1+
            if (fullPath.StartsWith(scriptDir, StringComparison.OrdinalIgnoreCase))
            {
                string relative = fullPath.Substring(scriptDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                mdRef = relative.Replace("\\", "/");
                if (!string.IsNullOrEmpty(mdRef) && !mdRef.StartsWith("."))
                    mdRef = "./" + mdRef;
            }
            else
            {
                // Not relative → fall back to POSIX path
                mdRef = fullPath.Replace("\\", "/");
            }
            // 5. Escape Markdown-sensitive characters
            // Escaping bracket is for MD references only, not for actual file saving.
            // The first item is the MD-safe form,
            // the second is the actual path to use in pixmap saving.
            mdRef = mdRef.Replace("(", "-").Replace(")", "-")
                         .Replace("[", "-").Replace("]", "-")
                         .Replace(" ", "_")
                         .Replace("\u2010", "-").Replace("\u2011", "-").Replace("\u2012", "-")
                         .Replace("\u2013", "-").Replace("\u2014", "-").Replace("\u2015", "-")
                         .Replace("\u2212", "-");

            return (mdRef, mdRef);
        }

        /// <summary><c>is_ocr_text(span)</c></summary>
        /// <param name="span">Text span to inspect for OCR origin.</param>
        public static bool IsOcrText(ExtendedSpan span)
        {
            if (span == null)
                return false;
            if (span.Font == TESSERACT_FONT_NAME)
                return true;
            if ((span.CharFlags & (int)mupdf.mupdf.FZ_STEXT_STROKED) != 0
                || (span.CharFlags & (int)mupdf.mupdf.FZ_STEXT_FILLED) != 0)
                return false;
            return true;
        }

        /// <summary><c>is_ocr_text(span)</c> for table-cell <see cref="Span"/> objects.</summary>
        public static bool IsOcrText(Span span)
        {
            if (span == null)
                return false;
            if (span.Font == TESSERACT_FONT_NAME)
                return true;
            if ((span.CharFlags & (uint)mupdf.mupdf.FZ_STEXT_STROKED) != 0
                || (span.CharFlags & (uint)mupdf.mupdf.FZ_STEXT_FILLED) != 0)
                return false;
            return true;
        }

        /// <summary>Blocks list from <c>TextPage.extractDICT()</c>.</summary>
        internal static List<Dictionary<string, object>> StextDictBlocks(TextPage textPage)
        {
            if (textPage == null)
                return new List<Dictionary<string, object>>();
            Dictionary<string, object> pageDict = textPage.ExtractDict(false);
            if (pageDict != null
                && pageDict.TryGetValue("blocks", out object blocksObj)
                && blocksObj is List<Dictionary<string, object>> blocks)
                return blocks;
            return new List<Dictionary<string, object>>();
        }

        /// <summary>
        /// Check if text starts with a bullet character
        /// </summary>
        /// <param name="text">Text to check for a leading bullet character.</param>
        public static bool StartswithBullet(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            if (!BULLETS.Contains(text[0]))
                return false;
            if (text.Length == 1)
                return true;
            if (text[1] == ' ')
                return true;
            return false;
        }

        /// <summary>
        /// Identify white text
        /// </summary>
        /// <param name="text">Text to test for whitespace-only content.</param>
        public static bool IsWhite(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;
            return text.All(c => WHITE_CHARS.Contains(c));
        }

        /// <summary>
        /// Return the (standard) empty / invalid rectangle (EMPTY_RECT semantics).
        /// Rect(2147483520, 2147483520, -2147483648, -2147483648).
        /// Joining this with another rect yields that rect; used as neutral element for union.
        /// </summary>
        public static Rect EmptyRect()
        {
            return MuPDF.NET.Utils.EMPTY_RECT();
        }

        /// <summary>
        /// Check if bounding box is empty
        /// </summary>
        /// <param name="bbox">Rectangle to test.</param>
        public static bool BboxIsEmpty(Rect bbox)
        {
            if (bbox == null)
                return true;
            return bbox.X0 >= bbox.X1 || bbox.Y0 >= bbox.Y1;
        }

        /// <summary>
        /// Intersect two rectangles
        /// </summary>
        /// <param name="r1">First rectangle.</param>
        /// <param name="r2">Second rectangle.</param>
        /// <param name="bboxOnly">Reserved; kept for API parity with layout package.</param>
        public static Rect IntersectRects(Rect r1, Rect r2, bool bboxOnly = false)
        {
            if (r1 == null || r2 == null)
                return EmptyRect();
            
            float x0 = Math.Max(r1.X0, r2.X0);
            float y0 = Math.Max(r1.Y0, r2.Y0);
            float x1 = Math.Min(r1.X1, r2.X1);
            float y1 = Math.Min(r1.Y1, r2.Y1);
            
            if (x0 >= x1 || y0 >= y1)
                return EmptyRect();
            
            return new Rect(x0, y0, x1, y1);
        }

        /// <summary>
        /// Join a list of rectangles into their bounding rectangle
        /// </summary>
        /// <param name="rects">Rectangles to unite into one bounding box.</param>
        /// <param name="bboxOnly">Reserved; kept for API parity with layout package.</param>
        public static Rect JoinRects(List<Rect> rects, bool bboxOnly = false)
        {
            if (rects == null || rects.Count == 0)
                return EmptyRect();
            
            float x0 = rects.Min(r => r.X0);
            float y0 = rects.Min(r => r.Y0);
            float x1 = rects.Max(r => r.X1);
            float y1 = rects.Max(r => r.Y1);
            
            return new Rect(x0, y0, x1, y1);
        }

        /// <summary>
        /// Check if bbox is almost entirely within clip
        /// </summary>
        /// <param name="bbox">Rectangle to test.</param>
        /// <param name="clip">Clipping rectangle.</param>
        /// <param name="portion">Minimum fraction of <paramref name="bbox"/> area that must lie inside <paramref name="clip"/>.</param>
        public static bool AlmostInBbox(Rect bbox, Rect clip, float portion = 0.8f)
        {
            if (bbox == null || clip == null)
                return false;
            
            float x0 = Math.Max(bbox.X0, clip.X0);
            float y0 = Math.Max(bbox.Y0, clip.Y0);
            float x1 = Math.Min(bbox.X1, clip.X1);
            float y1 = Math.Min(bbox.Y1, clip.Y1);
            
            float interArea = Math.Max(0, x1 - x0) * Math.Max(0, y1 - y0);
            float boxArea = (bbox.X1 - bbox.X0) * (bbox.Y1 - bbox.Y0);
            
            // If intersection area is greater than portion of box area
            return interArea > boxArea * portion;
        }

        /// <summary>
        /// Check if bbox is outside cell
        /// </summary>
        /// <param name="bbox">Rectangle to test.</param>
        /// <param name="cell">Reference cell rectangle.</param>
        /// <param name="strict">When <c>true</c>, touching edges count as outside.</param>
        public static bool OutsideBbox(Rect bbox, Rect cell, bool strict = false)
        {
            if (bbox == null || cell == null)
                return true;
            
            if (!strict)
            {
                return bbox.X0 >= cell.X1 || bbox.X1 <= cell.X0 ||
                       bbox.Y0 >= cell.Y1 || bbox.Y1 <= cell.Y0;
            }
            else
            {
                return bbox.X0 > cell.X1 || bbox.X1 < cell.X0 ||
                       bbox.Y0 > cell.Y1 || bbox.Y1 < cell.Y0;
            }
        }

        /// <summary>
        /// Check if inner rectangle is contained within outer rectangle
        /// </summary>
        /// <param name="inner">Candidate inner rectangle.</param>
        /// <param name="outer">Candidate outer rectangle.</param>
        public static bool BboxInBbox(Rect inner, Rect outer)
        {
            if (inner == null || outer == null)
                return false;
            
            return outer.X0 <= inner.X0 && outer.Y0 <= inner.Y0 &&
                   outer.X1 >= inner.X1 && outer.Y1 >= inner.Y1;
        }

        /// <summary>
        /// Check if rect is contained in any rect of the list
        /// </summary>
        /// <param name="rect">Rectangle to test.</param>
        /// <param name="rectList">Rectangles to search.</param>
        public static bool BboxInAnyBbox(Rect rect, IEnumerable<Rect> rectList)
        {
            if (rect == null || rectList == null)
                return false;
            
            return rectList.Any(r => BboxInBbox(rect, r));
        }

        /// <summary>
        /// Check if rect is outside all rects in the list
        /// </summary>
        /// <param name="rect">Rectangle to test.</param>
        /// <param name="rectList">Rectangles to compare against.</param>
        public static bool OutsideAllBboxes(Rect rect, IEnumerable<Rect> rectList)
        {
            if (rect == null || rectList == null)
                return true;
            
            return rectList.All(r => OutsideBbox(rect, r));
        }

        /// <summary>
        /// Check if middle of rect is contained in any rect of the list
        /// </summary>
        /// <param name="rect">Rectangle to test (slightly enlarged before comparison).</param>
        /// <param name="rectList">Rectangles to search.</param>
        /// <param name="portion">Minimum overlap fraction passed to <see cref="AlmostInBbox"/>.</param>
        public static bool AlmostInAnyBbox(Rect rect, IEnumerable<Rect> rectList, float portion = 0.5f)
        {
            if (rect == null || rectList == null)
                return false;
            
            // Enlarge rect slightly
            Rect enlarged = new Rect(
                rect.X0 - 1,
                rect.Y0 - 1,
                rect.X1 + 1,
                rect.Y1 + 1
            );
            
            return rectList.Any(r => AlmostInBbox(enlarged, r, portion));
        }

        /// <summary>
        /// Join any rectangles with a pairwise non-empty overlap.
        /// Accepts and returns a list of Rect items.
        /// Note that rectangles that only "touch" each other (common point or edge)
        /// are not considered as overlapping.
        /// Use a positive "enlarge" parameter to enlarge rectangle by these many
        /// points in every direction.
        /// TODO: Consider using a sweeping line algorithm for this.
        /// </summary>
        /// <param name="boxes">Input rectangles to merge where they overlap.</param>
        /// <param name="enlarge">Points to expand each rectangle before overlap testing.</param>
        public static List<Rect> RefineBoxes(List<Rect> boxes, float enlarge = 0)
        {
            if (boxes == null || boxes.Count == 0)
                return new List<Rect>();

            List<Rect> newRects = new List<Rect>();
            // List of all vector graphic rectangles
            List<Rect> prects = boxes.Select(b => new Rect(b)).ToList();

            while (prects.Count > 0) // The algorithm will empty this list
            {
                Rect r = new Rect(prects[0]); // Copy of first rectangle
                r.X0 -= enlarge;
                r.Y0 -= enlarge;
                r.X1 += enlarge;
                r.Y1 += enlarge;

                bool repeat = true; // Initialize condition
                while (repeat)
                {
                    repeat = false; // Set false as default
                    for (int i = prects.Count - 1; i > 0; i--) // From back to front
                    {
                        if (r.Intersects(prects[i])) // Enlarge first rect with this
                        {
                            r = Utils.JoinRects(new List<Rect> { r, prects[i] });
                            prects.RemoveAt(i); // Delete this rect
                            repeat = true; // Indicate must try again
                        }
                    }
                }

                // First rect now includes all overlaps
                newRects.Add(r);
                prects.RemoveAt(0);
            }

            return newRects
                .OrderBy(r => r.X0)
                .ThenBy(r => r.Y0)
                .ToList(); // Sort by left, top
        }

        /// <summary>
        /// Determine the background color of the page
        /// </summary>
        /// <param name="page">Page whose corner pixels are sampled for a uniform background.</param>
        public static float[] GetBgColor(Page page)
        {
            if (page == null)
                return null;

            try
            {
                // Check upper left corner
                Rect ulRect = new Rect(page.Rect.X0, page.Rect.Y0, page.Rect.X0 + 10, page.Rect.Y0 + 10);
                Pixmap pixUL = page.GetPixmap(clip: ulRect);
                if (pixUL == null || pixUL.SAMPLES == null || !pixUL.IsUniColor)
                {
                    pixUL?.Dispose();
                    return null;
                }
                var pixelUL = pixUL.GetPixel(0, 0);
                pixUL.Dispose();

                // Check upper right corner
                Rect urRect = new Rect(page.Rect.X1 - 10, page.Rect.Y0, page.Rect.X1, page.Rect.Y0 + 10);
                Pixmap pixUR = page.GetPixmap(clip: urRect);
                if (pixUR == null || pixUR.SAMPLES == null || !pixUR.IsUniColor)
                {
                    pixUR?.Dispose();
                    return null;
                }
                var pixelUR = pixUR.GetPixel(0, 0);
                pixUR.Dispose();

                if (pixelUL.Length != pixelUR.Length || 
                    !pixelUL.SequenceEqual(pixelUR))
                    return null;

                // Check lower left corner
                Rect llRect = new Rect(page.Rect.X0, page.Rect.Y1 - 10, page.Rect.X0 + 10, page.Rect.Y1);
                Pixmap pixLL = page.GetPixmap(clip: llRect);
                if (pixLL == null || pixLL.SAMPLES == null || !pixLL.IsUniColor)
                {
                    pixLL?.Dispose();
                    return null;
                }
                var pixelLL = pixLL.GetPixel(0, 0);
                pixLL.Dispose();

                if (pixelUL.Length != pixelLL.Length || 
                    !pixelUL.SequenceEqual(pixelLL))
                    return null;

                // Check lower right corner
                Rect lrRect = new Rect(page.Rect.X1 - 10, page.Rect.Y1 - 10, page.Rect.X1, page.Rect.Y1);
                Pixmap pixLR = page.GetPixmap(clip: lrRect);
                if (pixLR == null || pixLR.SAMPLES == null || !pixLR.IsUniColor)
                {
                    pixLR?.Dispose();
                    return null;
                }
                var pixelLR = pixLR.GetPixel(0, 0);
                pixLR.Dispose();

                if (pixelUL.Length != pixelLR.Length || 
                    !pixelUL.SequenceEqual(pixelLR))
                    return null;

                // All corners match - return normalized RGB
                if (pixelUL.Length >= 3)
                {
                    return new float[] 
                    { 
                        pixelUL[0] / 255f, 
                        pixelUL[1] / 255f, 
                        pixelUL[2] / 255f 
                    };
                }
            }
            catch
            {
                // If background detection fails, return null
            }

            return null;
        }

        /// <summary>
        /// Check whether the rectangle contains significant drawings
        /// </summary>
        /// <param name="box">Region to inspect for non-trivial vector graphics.</param>
        /// <param name="paths">Drawing paths on the page.</param>
        public static bool IsSignificant(Rect box, List<PathInfo> paths)
        {
            if (box == null || paths == null || paths.Count == 0)
                return false;

            // Build a sub-box of 90% of the original box
            // To this end, we build a sub-box of 90% of the original box and check
            // whether this still contains drawing paths.
            float d;
            if (box.Width > box.Height)
                d = box.Width * 0.025f;
            else
                d = box.Height * 0.025f;

            Rect nbox = new Rect(
                box.X0 + d,
                box.Y0 + d,
                box.X1 - d,
                box.Y1 - d
            ); // Nbox covers 90% of box interior

            // Paths contained in, but not equal to box
            var myPaths = paths
                .Where(p => p.Rect != null && 
                       BboxInBbox(p.Rect, box) && 
                       !p.Rect.EqualTo(box))
                .ToList();

            if (myPaths.Count == 0)
                return false;

            // Check if all paths are horizontal or vertical lines
            var widths = myPaths.Select(p => (int)Math.Round(p.Rect.Width))
                .Concat(new[] { (int)Math.Round(box.Width) })
                .Distinct()
                .ToList();
            var heights = myPaths.Select(p => (int)Math.Round(p.Rect.Height))
                .Concat(new[] { (int)Math.Round(box.Height) })
                .Distinct()
                .ToList();

            if (widths.Count == 1 || heights.Count == 1)
                return false; // All paths are horizontal or vertical lines / rectangles

            // Check if any path intersects the interior
            foreach (var p in myPaths)
            {
                Rect rect = p.Rect;
                if (!(
                    BboxIsEmpty(rect) || BboxIsEmpty(IntersectRects(rect, nbox))
                )) // Intersects interior: significant!
                {
                    return true;
                }
                // Remaining case: a horizontal or vertical line
                // Horizontal line:
                if (
                    true
                    && Math.Abs(rect.Y0 - rect.Y1) < 0.1f
                    && nbox.Y0 <= rect.Y0 && rect.Y0 <= nbox.Y1
                    && rect.X0 < nbox.X1
                    && rect.X1 > nbox.X0
                )
                {
                    return true;
                }
                // Vertical line
                if (
                    true
                    && Math.Abs(rect.X0 - rect.X1) < 0.1f
                    && nbox.X0 <= rect.X0 && rect.X0 <= nbox.X1
                    && rect.Y0 < nbox.Y1
                    && rect.Y1 > nbox.Y0
                )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Expand bbox to include all points
        /// </summary>
        /// <param name="bbox">Starting bounding box as <c>(x0, y0, x1, y1)</c>.</param>
        /// <param name="points">Points to include in the expanded box.</param>
        public static (float x0, float y0, float x1, float y1) ExpandBboxByPoints(
            (float x0, float y0, float x1, float y1) bbox,
            List<Point> points)
        {
            if (points == null || points.Count == 0)
                return bbox;
            
            float x0 = Math.Min(points.Min(p => p.X), bbox.x0);
            float y0 = Math.Min(points.Min(p => p.Y), bbox.y0);
            float x1 = Math.Max(points.Max(p => p.X), bbox.x1);
            float y1 = Math.Max(points.Max(p => p.Y), bbox.y1);
            
            return (x0, y0, x1, y1);
        }

        /// <summary>
        /// Analyze the page for OCR decision (delegates to <see cref="Ocr.AnalyzePage"/>).
        /// </summary>
        /// <param name="page">Page to analyze.</param>
        /// <param name="blocks">Optional pre-extracted text blocks; extracted when <c>null</c>.</param>
        /// <param name="replaceOcr">When <c>true</c>, replace an existing OCR text layer.</param>
        public static Dictionary<string, object> AnalyzePage(
            Page page,
            List<Block> blocks = null,
            bool replaceOcr = false) =>
            global::PDF4LLM.Ocr.AnalyzePage.Analyze(page, blocks, replaceOcr: replaceOcr);

        /// <summary>Compute intersection over union of two rectangles.</summary>
        public static float Iou(Rect r1, Rect r2) =>
            LayoutParseHelpers.IntersectionOverUnion(r1, r2);

        /// <summary>
        /// Split a table layout box into separate picture and table regions when tilted vector graphics dominate the area.
        /// </summary>
        /// <param name="blocks">Text and vector blocks from the page text layer.</param>
        /// <param name="tableEntry">Layout table box to validate or split.</param>
        public static (LayoutInfoEntry picture, LayoutInfoEntry table) TableCleaner(
            List<Dictionary<string, object>> blocks,
            LayoutInfoEntry tableEntry)
        {
            if (tableEntry?.Bbox == null || blocks == null)
                return (null, null);

            Rect bbox = new Rect(tableEntry.Bbox);
            var allVectors = new List<(IRect irect, bool isRect)>();
            foreach (Dictionary<string, object> b in blocks)
            {
                if (!b.TryGetValue("type", out object typeObj) || Convert.ToInt32(typeObj) != 3)
                    continue;
                if (!b.TryGetValue("bbox", out object bboxObj))
                    continue;
                Rect vb = DictBboxToRect(bboxObj);
                if (!vb.Contains(new Point(bbox.X0, bbox.Y0)))
                    continue;
                bool isRect = b.TryGetValue("isrect", out object ir) && Convert.ToBoolean(ir);
                allVectors.Add((new IRect(vb), isRect));
            }

            var tiltVectors = allVectors.Where(v => !v.isRect).ToList();
            if (tiltVectors.Count == 0)
                return (null, null);

            float y0 = tiltVectors.Min(v => v.irect.Y0);
            float y1 = tiltVectors.Max(v => v.irect.Y1);
            float x0 = tiltVectors.Min(v => v.irect.X0);
            float x1 = tiltVectors.Max(v => v.irect.X1);
            Rect tilted = new Rect(x0, y0, x1, y1);

            if (tilted.Width >= bbox.Width * 0.8f && tilted.Height >= bbox.Height * 0.8f)
            {
                return (new LayoutInfoEntry { Bbox = bbox, Class = "picture" }, null);
            }

            var spanRects = new List<Rect>();
            foreach (Dictionary<string, object> b in blocks)
            {
                if (!b.TryGetValue("type", out object typeObj) || Convert.ToInt32(typeObj) != 0)
                    continue;
                if (!b.TryGetValue("lines", out object linesObj)
                    || !(linesObj is List<Dictionary<string, object>> lines))
                    continue;
                foreach (Dictionary<string, object> line in lines)
                {
                    if (!line.TryGetValue("spans", out object spansObj)
                        || !(spansObj is List<Dictionary<string, object>> spans))
                        continue;
                    foreach (Dictionary<string, object> span in spans)
                    {
                        if (!span.TryGetValue("bbox", out object sb))
                            continue;
                        Rect sr = DictBboxToRect(sb);
                        if (bbox.Contains(sr))
                            spanRects.Add(sr);
                    }
                }
            }

            if (tilted.Y1 - bbox.Y0 <= bbox.Height * 0.3f && tilted.Width >= bbox.Width * 0.7f)
            {
                tilted = new Rect(tilted.X0, tilted.Y0, tilted.X1, tilted.Y1 + 2);
                foreach (Rect r in spanRects)
                {
                    if (tilted.Intersects(r))
                        tilted |= r;
                }

                var picture = new LayoutInfoEntry
                {
                    Bbox = new Rect(bbox.X0, bbox.Y0, bbox.X1, tilted.Y1),
                    Class = "picture"
                };
                var table = new LayoutInfoEntry
                {
                    Bbox = new Rect(bbox.X0, tilted.Y1 + 1, bbox.X1, bbox.Y1),
                    Class = "table"
                };
                return (picture, table);
            }

            return (null, null);
        }

        internal static Rect DictBboxToRect(object bboxObj)
        {
            if (bboxObj is Rect r)
                return r;
            if (bboxObj is float[] fa && fa.Length >= 4)
                return new Rect(fa[0], fa[1], fa[2], fa[3]);
            if (bboxObj is List<object> lo && lo.Count >= 4)
                return new Rect(
                    Convert.ToSingle(lo[0], CultureInfo.InvariantCulture),
                    Convert.ToSingle(lo[1], CultureInfo.InvariantCulture),
                    Convert.ToSingle(lo[2], CultureInfo.InvariantCulture),
                    Convert.ToSingle(lo[3], CultureInfo.InvariantCulture));
            if (bboxObj is IEnumerable<float> seq)
            {
                var list = seq.ToList();
                if (list.Count >= 4)
                    return new Rect(list[0], list[1], list[2], list[3]);
            }

            return MuPDF.NET.Utils.EMPTY_RECT();
        }
    }
}