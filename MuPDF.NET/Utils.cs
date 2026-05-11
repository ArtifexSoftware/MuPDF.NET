using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MuPDF.NET
{
    /// <summary>
    /// Static utility functions ported from PyMuPDF's utils.py module.
    /// Provides color lookup, page label handling, quad recovery, and font helpers.
    /// </summary>
    public static class Utils
    {
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
        public static (float r, float g, float b) GetColor(string name)
        {
            if (WxColors.PdfColorDict.TryGetValue(name.ToLower(), out var c))
                return c;
            return (1f, 1f, 1f);
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
        /// Convert a positive integer to an alphabetic sequence string
        /// (0→A, 1→B, … 25→Z, 26→AA, …).
        /// </summary>
        public static string IntegerToLetter(int i)
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

        /// <summary>
        /// Convert a positive integer to its Roman numeral representation.
        /// </summary>
        public static string IntegerToRoman(int num)
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
            (double cos, double sin) lineDir,
            Dictionary<string, object> span,
            Rect bbox,
            bool smallGlyphHeights = false)
        {
            double cos = lineDir.cos;
            double sin = lineDir.sin;

            double d;
            if (smallGlyphHeights)
                d = 1.0;
            else
                d = Convert.ToDouble(span["ascender"]) - Convert.ToDouble(span["descender"]);

            double height = d * Convert.ToDouble(span["size"]);

            double hs = height * sin;
            double hc = height * cos;

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
            (double cos, double sin) lineDir,
            Dictionary<string, object> span,
            bool smallGlyphHeights = false)
        {
            Rect bbox = DictToRect(span["bbox"]);
            return RecoverBboxQuad(lineDir, span, bbox, smallGlyphHeights);
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

            double h = 0;
            foreach (var s in spans)
            {
                double size = Convert.ToDouble(s["size"]);
                double spanH;
                if (smallGlyphHeights)
                    spanH = size;
                else
                    spanH = size * (Convert.ToDouble(s["ascender"]) - Convert.ToDouble(s["descender"]));
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
            (double cos, double sin) lineDir,
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

            double size = Convert.ToDouble(span["size"]);
            double h;
            if (smallGlyphHeights)
                h = size;
            else
                h = size * (Convert.ToDouble(span["ascender"]) - Convert.ToDouble(span["descender"]));

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
            (double cos, double sin) lineDir,
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
                    using var font = new Font(fontbuffer: buffer);
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
                    using var font = new Font(fontname: fontname);
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

        /// <summary>
        /// "Now" timestamp in PDF Format.
        /// </summary>
        public static string get_pdf_now() => Helpers.GetPdfNow();
        public static string getPDFnow() => get_pdf_now();
        /// <summary>
        /// Return a PDF string depending on its coding.
        /// Notes:
        /// Returns a string bracketed with either "()" or "&lt;&gt;" for hex values.
        /// If only ascii then "(original)" is returned, else if only 8 bit chars
        /// then "(original)" with interspersed octal strings \nnn is returned,
        /// else a string "&lt;FEFF[hexstring]&gt;" is returned, where [hexstring] is the
        /// UTF-16BE encoding of the original.
        /// </summary>
        public static string get_pdf_str(string s) => Helpers.GetPdfStr(s);
        public static string getPDFstr(string s) => get_pdf_str(s);
        /// <summary>
        /// Calculate length of a string for a built-in font.
        /// Args:
        /// fontname: name of the font.
        /// fontsize: font size points.
        /// encoding: encoding to use, 0=Latin (default), 1=Greek, 2=Cyrillic.
        /// Returns:
        /// (float) length of text.
        /// </summary>
        public static float get_text_length(string text, string fontname = "helv", float fontsize = 11, int encoding = 0)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;

            // fontname = fontname.lower()
            string lower = (fontname ?? "helv").ToLowerInvariant();
            // basename = Base14_fontdict.get(fontname, None)
            string resolved;
            if (Constants.Base14FontDict.TryGetValue(lower, out resolved))
            {
                // if fontname in Base14_fontdict.keys():
                //     return util_measure_string(text, Base14_fontdict[fontname], fontsize, encoding)
                using (var font = new Font(lower))
                {
                    return font.TextLength(text, fontsize, script: encoding);
                }
            }

            // if fontname in ("china-t","china-s","china-ts","china-ss","japan","japan-s","korea","korea-s"):
            //     return len(text) * fontsize
            if (lower == "china-t" || lower == "china-s" || lower == "china-ts" || lower == "china-ss" ||
                lower == "japan" || lower == "japan-s" || lower == "korea" || lower == "korea-s")
                return text.Length * fontsize;

            // raise ValueError(f"Font '{fontname}' is unsupported")
            throw new ArgumentException($"Font '{fontname}' is unsupported");
        }
        public static float getTextlength(string text, string fontname = "helv", float fontsize = 11, int encoding = 0)
            => get_text_length(text, fontname, fontsize, encoding);
        /// <summary>
        /// Return basic properties of an image.
        /// Args:
        /// img: bytes, bytearray, io.BytesIO object or an opened image file.
        /// Returns:
        /// A dictionary with keys width, height, colorspace.n, bpc, type, ext and size,
        /// where 'type' is the MuPDF image type (0 to 14) and 'ext' the suitable
        /// file extension.
        /// </summary>
        public static Dictionary<string, object> image_profile(object img, int keep_image = 0)
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
        public static Dictionary<string, object> ImageProperties(object img, int keep_image = 0) => image_profile(img, keep_image);
        /// <summary>
        /// Return a Rect for the paper size indicated in string 's'.
        /// Must conform to the argument of method 'PaperSize', which will be invoked.
        /// </summary>
        public static Rect paper_rect(string s) => Helpers.PaperRect(s);
        public static Rect PaperRect(string s) => paper_rect(s);
        /// <summary>
        /// Return a tuple (width, height) for a given paper format string.
        /// Notes:
        /// 'A4-L' will return (842, 595), the values for A4 landscape.
        /// Suffix '-P' and no suffix return the portrait tuple.
        /// </summary>
        public static (float w, float h) paper_size(string s) => Helpers.PaperSize(s);
        public static (float w, float h) PaperSize(string s) => paper_size(s);
        /// <summary>
        /// Known paper formats @ 72 dpi as a dictionary. Key is the format string
        /// like "a4" for ISO-A4. Value is the tuple (width, height).
        /// Information taken from:
        /// www.din-formate.de
        /// www.din-formate.info/amerikanische-formate.html
        /// www.directtools.de/wissen/normen/iso.htm
        /// </summary>
        public static Dictionary<string, (float w, float h)> paper_sizes() => Helpers.GetPaperSizes();
        /// <summary>
        /// Compute matrix which maps line from p1 to p2 to the x-axis, such that it
        /// maintains its length and p1 * matrix = Point(0, 0).
        /// Returns:
        /// Matrix which maps p1 to Point(0, 0) and p2 to a point on the x axis at
        /// the same distance to Point(0,0). Will always combine a rotation and a
        /// transformation.
        /// </summary>
        public static Matrix planish_line(Point p1, Point p2) => PlanishLineCore(p1, p2);

        /// <summary>
        /// Compute matrix which maps line from p1 to p2 to the x-axis, such that it
        /// maintains its length and p1 * matrix = Point(0, 0).
        ///
        /// Args:
        /// p1, p2: point_like
        /// Returns:
        /// Matrix which maps p1 to Point(0, 0) and p2 to a point on the x axis at
        /// the same distance to Point(0,0). Will always combine a rotation and a
        /// transformation.
        /// </summary>
        private static Matrix PlanishLineCore(Point p1, Point p2)
        {
            // p1 = Point(p1)
            // p2 = Point(p2)
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length < Constants.Epsilon)
                return new Matrix(1, 0, 0, 1, -p1.X, -p1.Y);

            double cos = dx / length;
            double sin = dy / length;

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

        private static string ImageExtensionFromType(int type)
        {
            // result[dictkey_ext] = JM_image_extension(type_)
            switch (type)
            {
                // if type_ == mupdf.FZ_IMAGE_LZW:     return "lzw"
                case int _ when type == mupdf.mupdf.FZ_IMAGE_LZW: return "lzw";
                // if type_ == mupdf.FZ_IMAGE_RLD:     return "rld"
                case int _ when type == mupdf.mupdf.FZ_IMAGE_RLD: return "rld";
                // if type_ == mupdf.FZ_IMAGE_JBIG2:   return "jb2"
                case int _ when type == mupdf.mupdf.FZ_IMAGE_JBIG2: return "jb2";
                // if type_ == mupdf.FZ_IMAGE_PNG:     return "png"
                case int _ when type == mupdf.mupdf.FZ_IMAGE_PNG: return "png";
                // if type_ == mupdf.FZ_IMAGE_JPEG:    return "jpeg"
                case int _ when type == mupdf.mupdf.FZ_IMAGE_JPEG: return "jpeg";
                // if type_ == mupdf.FZ_IMAGE_JXR:     return "jxr"
                case int _ when type == mupdf.mupdf.FZ_IMAGE_JXR: return "jxr";
                // if type_ == mupdf.FZ_IMAGE_JPX:     return "jpx"
                case int _ when type == mupdf.mupdf.FZ_IMAGE_JPX: return "jpx";
                // if type_ == mupdf.FZ_IMAGE_BMP:     return "bmp"
                case int _ when type == mupdf.mupdf.FZ_IMAGE_BMP: return "bmp";
                // if type_ == mupdf.FZ_IMAGE_GIF:     return "gif"
                case int _ when type == mupdf.mupdf.FZ_IMAGE_GIF: return "gif";
                // if type_ == mupdf.FZ_IMAGE_TIFF:    return "tiff"
                case int _ when type == mupdf.mupdf.FZ_IMAGE_TIFF: return "tiff";
                // if type_ == mupdf.FZ_IMAGE_PNM:     return "pnm"
                case int _ when type == mupdf.mupdf.FZ_IMAGE_PNM: return "pnm";
                // if type_ == mupdf.FZ_IMAGE_PSD: return "psd"
                default: return "n/a";
            }
        }

        /// <summary>
        /// Convert a direction value (tuple or array stored in a dictionary)
        /// to a (cos, sin) tuple.
        /// </summary>
        private static (double cos, double sin) DictToDir(object dirObj)
        {
            if (dirObj is ValueTuple<double, double> tuple)
                return (tuple.Item1, tuple.Item2);
            if (dirObj is IList<double> list && list.Count >= 2)
                return (list[0], list[1]);
            if (dirObj is double[] arr && arr.Length >= 2)
                return (arr[0], arr[1]);
            if (dirObj is float[] farr && farr.Length >= 2)
                return (farr[0], farr[1]);
            if (dirObj is IList<object> olist && olist.Count >= 2)
                return (Convert.ToDouble(olist[0]), Convert.ToDouble(olist[1]));
            throw new ArgumentException("cannot extract direction from object");
        }

        /// <summary>
        /// Convert a bbox value stored in a dictionary to a <see cref="Rect"/>.
        /// Accepts Rect, double[], float[], or IList&lt;double&gt;.
        /// </summary>
        private static Rect DictToRect(object bboxObj)
        {
            if (bboxObj is Rect r)
                return r;
            if (bboxObj is double[] darr && darr.Length >= 4)
                return new Rect(darr[0], darr[1], darr[2], darr[3]);
            if (bboxObj is float[] farr && farr.Length >= 4)
                return new Rect(farr[0], farr[1], farr[2], farr[3]);
            if (bboxObj is IList<double> dlist && dlist.Count >= 4)
                return new Rect(dlist[0], dlist[1], dlist[2], dlist[3]);
            if (bboxObj is IList<object> olist && olist.Count >= 4)
                return new Rect(
                    Convert.ToDouble(olist[0]),
                    Convert.ToDouble(olist[1]),
                    Convert.ToDouble(olist[2]),
                    Convert.ToDouble(olist[3]));
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

            double height = rect.Height / rows; // height of one table cell
            double width = rect.Width / cols;   // width of one table cell

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
    }
}
