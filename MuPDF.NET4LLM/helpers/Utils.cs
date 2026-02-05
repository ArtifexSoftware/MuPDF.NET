using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MuPDF.NET;
using mupdf;

namespace MuPDF.NET4LLM.Helpers
{
    /// <summary>
    /// Utility functions for PDF processing and layout analysis.
    /// Ported and adapted from LLM helpers.
    /// </summary>
    public static class Utils
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
        public const string TYPE3_FONT_NAME = "Unnamed-T3";

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
            (int)TextFlags.TEXT_MEDIABOX_CLIP
        );

        /// <summary>
        /// Traverse /AcroForm/Fields hierarchy and return a dict:
        /// fully qualified field name -> {"value": ..., "pages": [...]}
        /// Optionally, the xref of the field is included.
        /// </summary>
        public static Dictionary<string, Dictionary<string, object>> ExtractFormFieldsWithPages(Document doc, bool xrefs = false)
        {
            // Access the AcroForm dictionary.
            // Fast exit if not present or empty.
            // Placeholder - would need to access PDF internals
            return new Dictionary<string, Dictionary<string, object>>();
        }

        /// <summary>
        /// Normalize a folder path ("" = script folder), ensure it exists,
        /// and return a Markdown-safe file reference using forward slashes.
        /// Prefers relative paths to avoid Windows drive-letter issues.
        /// </summary>
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
                         .Replace("[", "-").Replace("]", "-");

            return (mdRef, fullPath);
        }

        /// <summary>
        /// Check if text starts with a bullet character
        /// </summary>
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
        public static bool BboxIsEmpty(Rect bbox)
        {
            if (bbox == null)
                return true;
            return bbox.X0 >= bbox.X1 || bbox.Y0 >= bbox.Y1;
        }

        /// <summary>
        /// Intersect two rectangles
        /// </summary>
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
        public static bool BboxInAnyBbox(Rect rect, IEnumerable<Rect> rectList)
        {
            if (rect == null || rectList == null)
                return false;
            
            return rectList.Any(r => BboxInBbox(rect, r));
        }

        /// <summary>
        /// Check if rect is outside all rects in the list
        /// </summary>
        public static bool OutsideAllBboxes(Rect rect, IEnumerable<Rect> rectList)
        {
            if (rect == null || rectList == null)
                return true;
            
            return rectList.All(r => OutsideBbox(rect, r));
        }

        /// <summary>
        /// Check if middle of rect is contained in any rect of the list
        /// </summary>
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
        /// Analyze the page for OCR decision
        /// </summary>
        public static Dictionary<string, object> AnalyzePage(Page page, List<Block> blocks = null)
        {
            int charsTotal = 0;
            int charsBad = 0;
            
            if (blocks == null)
            {
                TextPage textPage = page.GetTextPage(
                    clip: new Rect(float.NegativeInfinity, float.NegativeInfinity, 
                                   float.PositiveInfinity, float.PositiveInfinity),
                    flags: FLAGS);
                PageInfo pageInfo = textPage.ExtractDict(null, false);
                blocks = pageInfo.Blocks;
                textPage.Dispose();
            }
            
            Rect imgRect = EmptyRect();
            Rect txtRect = EmptyRect();
            Rect vecRect = EmptyRect();
            float imgArea = 0;
            float txtArea = 0;
            float vecArea = 0;
            int ocrSpans = 0;
            
            foreach (var b in blocks)
            {
                // Intersect each block bbox with the page rectangle.
                // Note that this has no effect on text because of the clipping flags,
                // which causes that we will not see ANY clipped text.
                Rect bbox = IntersectRects(page.Rect, b.Bbox);
                float area = bbox.Width * bbox.Height;
                if (area == 0.0f) // Skip any empty block
                    continue;
                
                if (b.Type == 1) // Image block
                {
                    imgRect = JoinRects(new List<Rect> { imgRect, bbox });
                    imgArea += area;
                }
                else if (b.Type == 0) // Text block
                {
                    if (BboxIsEmpty(b.Bbox))
                        continue;
                    
                    if (b.Lines != null)
                    {
                        foreach (var line in b.Lines)
                        {
                            if (BboxIsEmpty(line.Bbox))
                                continue;
                            
                            if (line.Spans != null)
                            {
                                foreach (var span in line.Spans)
                                {
                                    string text = span.Text ?? "";
                                    if (IsWhite(text))
                                        continue;
                                    
                                    Rect sr = IntersectRects(page.Rect, span.Bbox);
                                    if (BboxIsEmpty(sr))
                                        continue;
                                    
                                    // Check for OCR spans: font is "GlyphLessFont" or
                                    // (char_flags & 8 == 0 and char_flags & 16 == 0)
                                    // Note: CharFlags and Alpha may need to be accessed differently
                                    // For now, check font name for OCR detection
                                    if (span.Font == "GlyphLessFont")
                                    {
                                        ocrSpans++;
                                    }
                                    // Alpha check would need to be implemented based on available API
                                    // Skip invisible text (alpha == 0)
                                    
                                    charsTotal += text.Trim().Length;
                                    charsBad += text.Count(c => c == REPLACEMENT_CHARACTER);
                                    txtRect = JoinRects(new List<Rect> { txtRect, sr });
                                    txtArea += sr.Width * sr.Height;
                                }
                            }
                        }
                    }
                }
                else if (
                    true
                    && b.Type == 3 // Vector block
                    // && b.Stroked  // Note: Stroked and IsRect may not be available
                    && 2 < bbox.Width && bbox.Width <= 20 // Width limit for typical characters
                    && 2 < bbox.Height && bbox.Height <= 20 // Height limit for typical characters
                    // && !b.IsRect  // Contains curves
                )
                {
                    // Potential character-like vector block
                    vecRect = JoinRects(new List<Rect> { vecRect, bbox });
                    vecArea += area;
                }
            }
            
            // The rectangle on page covered by some content
            Rect covered = JoinRects(new List<Rect> { imgRect, txtRect, vecRect });
            float coverArea = Math.Abs(covered.Width * covered.Height);
            
            // The area-related float values are computed as fractions of the total covered area.
            return new Dictionary<string, object>
            {
                ["covered"] = covered, // Page area covered by content
                ["img_joins"] = coverArea > 0 ? Math.Abs(imgRect.Width * imgRect.Height) / coverArea : 0, // Fraction of area of the joined images
                ["img_area"] = coverArea > 0 ? imgArea / coverArea : 0, // Fraction of sum of image area sizes
                ["txt_joins"] = coverArea > 0 ? Math.Abs(txtRect.Width * txtRect.Height) / coverArea : 0, // Fraction of area of the joined text spans
                ["txt_area"] = coverArea > 0 ? txtArea / coverArea : 0, // Fraction of sum of text span bbox area sizes
                ["vec_area"] = coverArea > 0 ? vecArea / coverArea : 0, // Fraction of sum of vector character area sizes
                ["vec_joins"] = coverArea > 0 ? Math.Abs(vecRect.Width * vecRect.Height) / coverArea : 0, // Fraction of area of the joined vector characters
                ["chars_total"] = charsTotal, // Count of visible characters
                ["chars_bad"] = charsBad, // Count of Replacement Unicode characters
                ["ocr_spans"] = ocrSpans, // Count: text spans with ignored text (render mode 3)
            };
        }
    }
}
