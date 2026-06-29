using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using mupdf;

namespace MuPDF.NET
{
    public static partial class Utils
    {
        /// <summary>PDF link annotation skeleton strings used by <see cref="DoLinks"/>.</summary>
        internal static readonly IReadOnlyDictionary<string, string> AnnotSkel =
            new Dictionary<string, string>
            {
                ["goto1"] =
                    "<</A<</S/GoTo/D[{0} 0 R/XYZ {1} {2} {3}]>>/Rect[{4}]/BS<</W 0>>/Subtype/Link>>",
                ["goto2"] = "<</A<</S/GoTo/D{0}>>/Rect[{1}]/BS<</W 0>>/Subtype/Link>>",
                ["gotor1"] =
                    "<</A<</S/GoToR/D[{0} /XYZ {1} {2} {3}]/F<</F({4})/UF({5})/Type/Filespec>>>>/Rect[{6}]/BS<</W 0>>/Subtype/Link>>",
                ["gotor2"] = "<</A<</S/GoToR/D{0}/F({1})>>/Rect[{2}]/BS<</W 0>>/Subtype/Link>>",
                ["launch"] =
                    "<</A<</S/Launch/F<</F({0})/UF({1})/Type/Filespec>>>>/Rect[{2}]/BS<</W 0>>/Subtype/Link>>",
                ["uri"] = "<</A<</S/URI/URI({0})>>/Rect[{1}]/BS<</W 0>>/Subtype/Link>>",
                ["named"] = "<</A<</S/GoTo/D({0})/Type/Action>>/Rect[{1}]/BS<</W 0>>/Subtype/Link>>",
            };

        // ─── Geometry sentinels ──────────────────────────────────────────

        /// <summary>Largest valid rectangle; all valid rects are contained in it (PyMuPDF <c>INFINITE_RECT</c>).</summary>
        public static Rect INFINITE_RECT() => Helpers.INFINITE_RECT();

        /// <summary><see cref="INFINITE_RECT"/> as <see cref="IRect"/> (legacy <c>INFINITE_IRECT</c>).</summary>
        public static IRect INFINITE_IRECT() => INFINITE_RECT().IRect;

        /// <summary><see cref="INFINITE_RECT"/> as <see cref="Quad"/> (legacy <c>INFINITE_QUAD</c>).</summary>
        public static Quad INFINITE_QUAD() => INFINITE_RECT().Quad;

        /// <summary>Empty / invalid <see cref="IRect"/> (legacy <c>EMPTY_IRECT</c>).</summary>
        public static IRect EMPTY_IRECT() => EMPTY_RECT().IRect;

        // ─── Color conversion ────────────────────────────────────────────

        /// <summary>Convert sRGB <c>RRGGBB</c> integer to RGB bytes 0–255 (legacy <c>sRGB2Rgb</c>).</summary>
        public static (int r, int g, int b) SRgb2Rgb(int srgb)
        {
            srgb &= 0xffffff;
            return ((srgb >> 16) & 0xff, (srgb >> 8) & 0xff, srgb & 0xff);
        }

        /// <inheritdoc cref="SRgb2Rgb"/>
        public static (int, int, int) sRGB2Rgb(int srgb) => SRgb2Rgb(srgb);

        /// <summary>Convert sRGB <c>RRGGBB</c> to PDF color components 0–1 (legacy <c>sRGB2Pdf</c>).</summary>
        public static (float r, float g, float b) SRgb2Pdf(int srgb)
        {
            var (r, g, b) = SRgb2Rgb(srgb);
            return (r / 255f, g / 255f, b / 255f);
        }

        /// <inheritdoc cref="SRgb2Pdf"/>
        public static (float, float, float) sRGB2Pdf(int srgb) => SRgb2Pdf(srgb);

        /// <summary>Named color as PDF RGB floats 0–1 (legacy <c>GetColors</c>); white if unknown.</summary>
        public static float[] GetColors(string name)
        {
            var c = GetColor(name);
            return new[] { c.R, c.G, c.B };
        }

        // ─── Text extraction wrappers ────────────────────────────────────

        /// <summary>Header for multi-page <see cref="Page.GetText"/> HTML/JSON/XML output (legacy <c>ConversionHeader</c>).</summary>
        public static string ConversionHeader(string format, string filename = "unknown")
        {
            string t = (format ?? "").ToLowerInvariant();
            string html =
                """
                <!DOCTYPE html>
                <html>
                <head>
                <style>
                body{background-color:gray}
                div{position:relative;background-color:white;margin:1em auto}
                p{position:absolute;margin:0}
                img{position:absolute}
                </style>
                </head>
                <body>
                """;
            string xml = $"<?xml version='1.0'?>\n<document name='{filename}'>\n";
            string xhtml =
                """
                <?xml version='1.0'?>
                <!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'>
                <html xmlns='http://www.w3.org/1999/xhtml'>
                <head>
                <style>
                body{background-color:gray}
                div{background-color:white;margin:1em;padding:1em}
                p{white-space:pre-wrap}
                </style>
                </head>
                <body>
                """;
            return t switch
            {
                "html" => html,
                "json" => $"{{\"document\": \"{filename}\", \"pages\": [\n",
                "xml" => xml,
                "xhtml" => xhtml,
                _ => "",
            };
        }

        /// <summary>Trailer for multi-page <see cref="Page.GetText"/> HTML/JSON/XML output (legacy <c>ConversionTrailer</c>).</summary>
        public static string ConversionTrailer(string format)
        {
            string t = (format ?? "").ToLowerInvariant();
            return t switch
            {
                "html" or "xhtml" => "</body>\n</html>\n",
                "json" => "]\n}",
                "xml" => "</document>\n",
                _ => "",
            };
        }

        /// <summary>Image metadata (legacy <c>GetImageProfile</c>); never throws — returns <c>null</c> on error.</summary>
        public static ImageInfo? GetImageProfile(byte[] image, int keepImage = 0)
        {
            try
            {
                var dict = ImageProperties(image, keepImage);
                return dict == null ? null : (ImageInfo)dict;
            }
            catch
            {
                return null;
            }
        }

        /// <inheritdoc cref="ImageProperties"/>
        public static Dictionary<string, object>? GetImageProfile(object img, int keepImage = 0) =>
            ImageProperties(img, keepImage);

        // ─── Buffers / compression ───────────────────────────────────────

        /// <summary>Copy <see cref="FzBuffer"/> bytes (PyMuPDF <c>JM_BinFromBuffer</c>).</summary>
        public static byte[] BinFromBuffer(FzBuffer buffer) => Helpers.BinFromBuffer(buffer);

        /// <summary>Create <see cref="FzBuffer"/> from bytes (PyMuPDF buffer helper).</summary>
        public static FzBuffer BufferFromBytes(byte[] data) => Helpers.BufferFromBytes(data);

        /// <summary>Compress a buffer with zlib (PyMuPDF <c>JM_compress_buffer</c>).</summary>
        public static FzBuffer? CompressBuffer(FzBuffer inbuffer) => Helpers.JmCompressBuffer(inbuffer);

        /// <summary>Decode Python-style raw-unicode-escape bytes in a string.</summary>
        public static string DecodeRawUnicodeEscape(string s) =>
            Helpers.PyUnicode_DecodeRawUnicodeEscape(s);

        /// <summary>Decode raw-unicode-escape content from a buffer.</summary>
        public static string DecodeRawUnicodeEscape(FzBuffer s) =>
            Helpers.JmEscapeStrFromBuffer(s);

        // ─── PDF / page utilities ────────────────────────────────────────

        /// <summary>Unique numeric id for this process (legacy <c>GetId</c> / PyMuPDF <c>TOOLS.gen_id</c>).</summary>
        public static int GetId() => Tools.GenId();

        /// <summary>Concatenate all <c>/Contents</c> streams of a page (legacy API; see <see cref="Tools.GetAllContents"/>).</summary>
        public static byte[] GetAllContents(Page page) => Tools.GetAllContents(page);

        /// <summary>Insert PDF content bytes as a new <c>/Contents</c> stream (legacy; see <see cref="Tools.InsertContents"/>).</summary>
        public static int InsertContents(Page page, byte[] newContent, bool overlay = true) =>
            Tools.InsertContents(page, newContent, overlay);

        /// <summary>Find annotation by <c>/NM</c> name (legacy <c>GetAnnotByName</c>).</summary>
        public static Annot GetAnnotByName(Page page, string name)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            var pdfPage = Helpers.AsPdfPage(page, required: true);
            var annot = Helpers.JmGetAnnotByName(pdfPage, name);
            return new Annot(annot, page);
        }

        /// <summary>Rectangle area in px, in, cm, or mm (legacy <c>GetArea</c>).</summary>
        public static float GetArea(Rect rect, string unit = "px") => rect.GetArea(unit);

        /// <summary>PDF border-style code as integer from a letter (legacy <c>GetBorderStyle</c>).</summary>
        public static int GetBorderStyle(string style)
        {
            using var obj = Annot.GetBorderStyle(style);
            return obj.pdf_to_num();
        }

        /// <summary>Normalize <c>/Rotate</c> to 0, 90, 180, or 270.</summary>
        public static int NormalizeRotation(int rotate)
        {
            while (rotate < 0)
                rotate += 360;
            while (rotate >= 360)
                rotate -= 360;
            if (rotate % 90 != 0)
                return 0;
            return rotate;
        }

        /// <summary>Built-in font name shorthand for annotation <c>/DA</c> strings.</summary>
        public static string ExpandFontName(string fontName)
        {
            if (fontName == null)
                return "Helv";
            if (fontName.StartsWith("Co", StringComparison.OrdinalIgnoreCase))
                return "Cour";
            if (fontName.StartsWith("Ti", StringComparison.OrdinalIgnoreCase))
                return "TiRo";
            if (fontName.StartsWith("Sy", StringComparison.OrdinalIgnoreCase))
                return "Symb";
            if (fontName.StartsWith("Za", StringComparison.OrdinalIgnoreCase))
                return "Zadb";
            if (fontName.StartsWith("He", StringComparison.OrdinalIgnoreCase))
                return "Helv";
            return fontName;
        }

        /// <summary>Build annotation default appearance (<c>/DA</c>) for a widget.</summary>
        public static void MakeAnnotDA(mupdf.PdfAnnot annot, int nCol, float[] col, string fontName, float fontSize)
        {
            string buf = "";
            if (nCol < 1)
                buf += "0 g ";
            else if (nCol == 1)
                buf += $"{FloatToString(col[0])} g ";
            else if (nCol == 3)
                buf += $"{FloatToString(col[0])} {FloatToString(col[1])} {FloatToString(col[2])} rg ";
            else if (nCol >= 4)
                buf +=
                    $"{FloatToString(col[0])} {FloatToString(col[1])} {FloatToString(col[2])} {FloatToString(col[3])} k ";
            buf += $"/{ExpandFontName(fontName)} {FloatToString(fontSize)} Tf";
            mupdf.mupdf.pdf_dict_put_text_string(
                mupdf.mupdf.pdf_annot_obj(annot),
                mupdf.mupdf.pdf_new_name("DA"),
                buf);
        }

        /// <summary>Assign unique <c>/NM</c> to an annotation (legacy <c>AddAnnotId</c>).</summary>
        public static void AddAnnotId(mupdf.PdfAnnot annot, string stem)
        {
            var page = annot.pdf_annot_page();
            var annotObj = annot.pdf_annot_obj();
            var names = GetAnnotIdList(page);
            int i = 0;
            string stemId;
            while (true)
            {
                stemId = $"{ANNOT_ID_STEM}-{stem}{i}";
                if (!names.Contains(stemId))
                    break;
                i++;
            }
            annotObj.pdf_dict_puts("NM", mupdf.mupdf.pdf_new_string(stemId, (uint)stemId.Length));
            Helpers.PdfClearResynthRequired(page);
        }

        /// <summary>Add optional-content reference to a dictionary (legacy <c>AddOcObject</c>).</summary>
        public static void AddOcObject(mupdf.PdfDocument pdf, mupdf.PdfObj reference, int xref) =>
            Annot.AddOCObject(pdf, reference, xref);

        /// <summary>Ensure PDF trailer has an <c>/ID</c> array.</summary>
        public static void EnsureIdentity(Document pdf)
        {
            if (pdf?.NativePdfDocument == null)
                throw new ArgumentNullException(nameof(pdf));
            var trailer = pdf.NativePdfDocument.pdf_trailer();
            var id = trailer.pdf_dict_get(mupdf.mupdf.pdf_new_name("ID"));
            if (id.m_internal != null)
                return;
            var rnd = new byte[16];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                rng.GetBytes(rnd);
            string rnd0 = Encoding.UTF8.GetString(rnd);
            var arr = trailer.pdf_dict_put_array(mupdf.mupdf.pdf_new_name("ID"), 2);
            arr.pdf_array_push(mupdf.mupdf.pdf_new_string(rnd0, (uint)rnd0.Length));
            arr.pdf_array_push(mupdf.mupdf.pdf_new_string(rnd0, (uint)rnd0.Length));
        }

        /// <summary>PDF destination/action string for a GoTo link (legacy overload).</summary>
        public static string GetDestString(int xref, int pageDict) =>
            string.Format(CultureInfo.InvariantCulture, "/A<</S/GoTo/D[{0} 0 R/XYZ {1} {2} {3}]>>", xref, 0, pageDict, 0);

        /// <summary>PDF destination/action string (legacy overload).</summary>
        public static string GetDestString(int xref, float pageDict) =>
            string.Format(CultureInfo.InvariantCulture, "/A<</S/GoTo/D[{0} 0 R/XYZ {1} {2} {3}]>>", xref, 0, pageDict, 0);

        /// <summary>PDF link action string from <see cref="LinkInfo"/> (legacy <c>GetDestString</c>).</summary>
        public static string GetDestString(int xref, LinkInfo link)
        {
            if (link == null)
                return "";
            if (link.Kind == LinkType.LINK_GOTO)
            {
                var to = link.To;
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "/A<</S/GoTo/D[{0} 0 R/XYZ {1} {2} {3}]>>",
                    xref,
                    to.X,
                    to.Y,
                    link.Zoom);
            }
            if (link.Kind == LinkType.LINK_URI)
                return $"/A<</S/URI/URI{GetPdfString(link.Uri)}>>";
            if (link.Kind == LinkType.LINK_LAUNCH)
            {
                string f = GetPdfString(link.File);
                return $"/A<</S/Launch/F<</F{f}/UF{f}/Type/Filespec>>>>";
            }
            if (link.Kind == LinkType.LINK_GOTOR && link.Page < 0)
            {
                string f = GetPdfString(link.File);
                return $"/A<</S/GoToR/D{GetPdfString(link.ToStr)}/F<</F{f}/UF{f}/Type/Filespec>>>>";
            }
            if (link.Kind == LinkType.LINK_GOTOR && link.Page >= 0)
            {
                string f = GetPdfString(link.File);
                var to = link.To;
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "/A<</S/GoToR/D[{0} /XYZ {1} {2} {3}]/F<</F{f}/UF{f}/Type/Filespec>>>>",
                    link.Page,
                    to.X,
                    to.Y,
                    link.Zoom,
                    f,
                    f);
            }
            return "";
        }

        /// <summary>Widget type name from PDF field type integer (legacy <c>GetFieldTypeText</c>).</summary>
        public static string GetFieldTypeText(int wtype) =>
            wtype switch
            {
                (int)PdfWidgetType.PDF_WIDGET_TYPE_BUTTON => "Button",
                (int)PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX => "CheckBox",
                (int)PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON => "RadioButton",
                (int)PdfWidgetType.PDF_WIDGET_TYPE_TEXT => "Text",
                (int)PdfWidgetType.PDF_WIDGET_TYPE_LISTBOX => "ListBox",
                (int)PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX => "ComboBox",
                (int)PdfWidgetType.PDF_WIDGET_TYPE_SIGNATURE => "Signature",
                _ => "unknown",
            };

        /// <summary>Bind widget properties from an existing annotation (legacy <c>GetWidgetProperties</c>).</summary>
        public static void GetWidgetProperties(Annot annot, Widget widget)
        {
            if (annot == null)
                throw new ArgumentNullException(nameof(annot));
            if (widget == null)
                throw new ArgumentNullException(nameof(widget));
            widget.BindAnnot(annot.NativeAnnot, annot.Parent, annot);
            widget.SyncFromNative();
        }

        /// <summary>Adobe Glyph List lines (<c>name;CODE</c>) for custom glyph tables (legacy <c>GetGlyphText</c>).</summary>
        public static string[] GetGlyphText() => GlyphTextLazy.Value;

        /// <summary>Built-in font string width in points (legacy <c>MeasureString</c>).</summary>
        public static float MeasureString(
            string text,
            string fontName = "helv",
            float fontSize = 11,
            int encoding = 0) =>
            GetTextLength(text, fontName, fontSize, encoding);

        /// <summary>Image placement matrix for <see cref="Page.InsertImage"/> (legacy <c>CalcImageMatrix</c>).</summary>
        public static Matrix CalcImageMatrix(int width, int height, Rect tr, float rotate, bool keep) =>
            Helpers.CalcImageMatrix(width, height, tr, (int)rotate, keep);

        /// <summary>Color histogram for a pixmap (legacy <c>ColorCount</c>).</summary>
        public static Dictionary<byte[], int> ColorCount(Pixmap pm, Rect? clip = null) =>
            Helpers.JmColorCount(pm.NativePixmap, clip);

        /// <summary>Replace byte runs in a buffer (legacy <c>ReplaceBytes</c>).</summary>
        public static byte[] ReplaceBytes(byte[] src, byte[] search, byte[] replace, int limit = 1)
        {
            if (limit <= 0 || search == null || search.Length == 0)
                return src;
            using var ms = new MemoryStream();
            int matchCount = 0;
            for (int i = 0; i < src.Length;)
            {
                bool match = i + search.Length <= src.Length && MatchAt(src, i, search);
                if (match && matchCount < limit)
                {
                    ms.Write(replace, 0, replace.Length);
                    i += search.Length;
                    matchCount++;
                }
                else
                {
                    ms.WriteByte(src[i]);
                    i++;
                }
            }
            return ms.ToArray();
        }

        /// <summary>
        /// Copy pages from <paramref name="docSrc"/> into <paramref name="docDes"/> (legacy <c>MergeRange</c>).
        /// </summary>
        public static void MergeRange(
            Document docDes,
            Document docSrc,
            int spage,
            int epage,
            int apage,
            int rotate = 0,
            bool links = true,
            bool annots = true,
            int showProgress = 0,
            Graftmap? graftmap = null)
        {
            if (docDes?.NativePdfDocument == null || docSrc?.NativePdfDocument == null)
                throw new ArgumentException("documents must be PDF");
            Helpers.JmMergeRange(
                docDes.NativePdfDocument,
                docSrc.NativePdfDocument,
                spage,
                epage,
                apage,
                rotate,
                links,
                annots,
                showProgress,
                graftmap?.NativeGraftMap);
        }

        /// <summary>Re-create link annotations after copying pages between PDFs (legacy <c>DoLinks</c>).</summary>
        public static void DoLinks(
            Document doc1,
            Document doc2,
            int fromPage = -1,
            int toPage = -1,
            int startAt = -1)
        {
            if (doc1 == null || doc2 == null)
                throw new ArgumentNullException(doc1 == null ? nameof(doc1) : nameof(doc2));

            int fp = fromPage < 0 ? 0 : Math.Min(fromPage, doc2.PageCount - 1);
            int tp = toPage < 0 || toPage >= doc2.PageCount ? doc2.PageCount - 1 : toPage;
            if (startAt < 0)
                throw new ValueErrorException("'startAt' must be >= 0");

            int incr = fp <= tp ? 1 : -1;
            var pnoSrc = new List<int>();
            var pnoDst = new List<int>();
            for (int i = fp; i != tp + incr; i += incr)
                pnoSrc.Add(i);
            for (int i = 0; i < pnoSrc.Count; i++)
                pnoDst.Add(startAt + i);

            var xrefDst = new List<int>();
            for (int i = 0; i < pnoSrc.Count; i++)
                xrefDst.Add(doc1.GetPageXref(pnoDst[i]));

            for (int i = 0; i < pnoSrc.Count; i++)
            {
                Page pageSrc = doc2[pnoSrc[i]];
                var links = pageSrc.GetLinks();
                if (links.Count == 0)
                    continue;

                Matrix ctm = ~pageSrc.TransformationMatrix;
                Page pageDst = doc1[pnoDst[i]];
                var linkTab = new List<string>();
                foreach (var lnk in links)
                {
                    if (lnk.Kind == LinkType.LINK_GOTO && !pnoSrc.Contains(lnk.Page))
                        continue;
                    string? annotText = CreateLinkAnnotString(lnk, xrefDst, pnoSrc, ctm);
                    if (!string.IsNullOrEmpty(annotText))
                        linkTab.Add(annotText);
                }
                if (linkTab.Count > 0)
                    pageDst.AddAnnotFromString(linkTab);
            }
        }

        // ─── Legacy snake_case aliases ───────────────────────────────────

        internal static Rect infinite_rect() => INFINITE_RECT();
        internal static IRect infinite_irect() => INFINITE_IRECT();
        internal static Quad infinite_quad() => INFINITE_QUAD();
        internal static IRect empty_irect() => EMPTY_IRECT();
        internal static (int, int, int) sRGB2rgb(int srgb) => sRGB2Rgb(srgb);
        internal static (float, float, float) sRGB2pdf(int srgb) => sRGB2Pdf(srgb);
        internal static float[] get_colors(string name) => GetColors(name);
        internal static string conversion_header(string format, string filename = "unknown") =>
            ConversionHeader(format, filename);
        internal static string conversion_trailer(string format) => ConversionTrailer(format);
        internal static ImageInfo? get_image_profile(byte[] image, int keep_image = 0) =>
            GetImageProfile(image, keep_image);
        internal static byte[] bin_from_buffer(FzBuffer buffer) => BinFromBuffer(buffer);
        internal static FzBuffer buffer_from_bytes(byte[] data) => BufferFromBytes(data);
        internal static FzBuffer? compress_buffer(FzBuffer buf) => CompressBuffer(buf);
        internal static string decode_raw_unicode_escape(string s) => DecodeRawUnicodeEscape(s);
        internal static int get_id() => GetId();
        internal static byte[] get_all_contents(Page page) => GetAllContents(page);
        internal static int insert_contents(Page page, byte[] content, bool overlay = true) =>
            InsertContents(page, content, overlay);
        internal static Annot get_annot_by_name(Page page, string name) => GetAnnotByName(page, name);
        internal static float get_area(Rect rect, string unit = "px") => GetArea(rect, unit);
        internal static int get_border_style(string style) => GetBorderStyle(style);
        internal static int normalize_rotation(int rotate) => NormalizeRotation(rotate);
        internal static string expand_font_name(string fontname) => ExpandFontName(fontname);
        internal static void make_annot_da(mupdf.PdfAnnot annot, int ncol, float[] col, string font, float fontsize) =>
            MakeAnnotDA(annot, ncol, col, font, fontsize);
        internal static void add_annot_id(mupdf.PdfAnnot annot, string stem) => AddAnnotId(annot, stem);
        internal static void add_oc_object(mupdf.PdfDocument pdf, mupdf.PdfObj reference, int xref) =>
            AddOcObject(pdf, reference, xref);
        internal static void ensure_identity(Document pdf) => EnsureIdentity(pdf);
        internal static string get_dest_string(int xref, int ddict) => GetDestString(xref, ddict);
        internal static string get_dest_string(int xref, LinkInfo ddict) => GetDestString(xref, ddict);
        internal static string get_field_type_text(int wtype) => GetFieldTypeText(wtype);
        internal static void get_widget_properties(Annot annot, Widget widget) => GetWidgetProperties(annot, widget);
        internal static string[] get_glyph_text() => GetGlyphText();
        internal static float measure_string(string text, string fontname = "helv", float fontsize = 11, int encoding = 0) =>
            MeasureString(text, fontname, fontsize, encoding);
        internal static Matrix calc_image_matrix(int width, int height, Rect tr, float rotate, bool keep) =>
            CalcImageMatrix(width, height, tr, rotate, keep);
        internal static Dictionary<byte[], int> color_count(Pixmap pm, Rect? clip = null) => ColorCount(pm, clip);
        internal static byte[] replace_bytes(byte[] src, byte[] search, byte[] replace, int limit = 1) =>
            ReplaceBytes(src, search, replace, limit);
        internal static void merge_range(
            Document docDes,
            Document docSrc,
            int spage,
            int epage,
            int apage,
            int rotate = 0,
            bool links = true,
            bool annots = true,
            int showProgress = 0,
            Graftmap? graftmap = null) =>
            MergeRange(docDes, docSrc, spage, epage, apage, rotate, links, annots, showProgress, graftmap);
        internal static void do_links(Document doc1, Document doc2, int from_page = -1, int to_page = -1, int start_at = -1) =>
            DoLinks(doc1, doc2, from_page, to_page, start_at);
        internal static string get_image_extension(int type) => GetImageExtension(type);
        internal static int glyph_name2unicode(string name) => GlyphName2Unicode(name);
        internal static string unicode2glyph_name(int ch) => Unicode2GlyphName(ch);
        internal static string integer2letter(int i) => Integer2Letter(i);
        internal static string integer2roman(int i) => Integer2Roman(i);

        private static readonly Lazy<string[]> GlyphTextLazy = new(LoadGlyphTextLines);

        private static string[] LoadGlyphTextLines()
        {
            var lines = new List<string>();
            foreach (string file in new[] { "Glyphnames.txt.gz", "NameAliases.txt.gz" })
            {
                using var stream = OpenEmbeddedGzip(file);
                using var reader = new StreamReader(stream);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0 || line[0] == '#')
                        continue;
                    string[] parts = line.Split(';');
                    if (parts.Length < 2)
                        continue;
                    if (!int.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int cp))
                        continue;
                    string name = parts[1];
                    if (name.Length == 0 || name[0] == '<')
                        continue;
                    lines.Add($"{name};{cp:X4}");
                }
            }
            return lines.ToArray();
        }

        private static Stream OpenEmbeddedGzip(string fileName)
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

        private static List<string> GetAnnotIdList(mupdf.PdfPage page)
        {
            var ids = new List<string>();
            var annots = page.obj().pdf_dict_get(mupdf.mupdf.pdf_new_name("Annots"));
            if (annots.m_internal == null)
                return ids;
            int n = mupdf.mupdf.pdf_array_len(annots);
            for (int i = 0; i < n; i++)
            {
                var annotObj = mupdf.mupdf.pdf_array_get(annots, i);
                var name = annotObj.pdf_dict_gets("NM");
                if (name.m_internal != null)
                    ids.Add(name.pdf_to_text_string());
            }
            return ids;
        }

        private static bool MatchAt(byte[] src, int offset, byte[] pattern)
        {
            for (int j = 0; j < pattern.Length; j++)
            {
                if (src[offset + j] != pattern[j])
                    return false;
            }
            return true;
        }

        private static string? CreateLinkAnnotString(LinkInfo link, List<int> xrefDest, List<int> pnoSrc, Matrix ctm)
        {
            Rect r = link.From * ctm;
            string rStr = string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1} {2} {3}",
                FloatToString(r.X0),
                FloatToString(r.Y0),
                FloatToString(r.X1),
                FloatToString(r.Y1));

            if (link.Kind == LinkType.LINK_GOTO)
            {
                int idx = pnoSrc.IndexOf(link.Page);
                if (idx < 0)
                    return null;
                Point p = link.To * ctm;
                return string.Format(
                    CultureInfo.InvariantCulture,
                    AnnotSkel["goto1"],
                    xrefDest[idx],
                    FloatToString(p.X),
                    FloatToString(p.Y),
                    FloatToString(link.Zoom),
                    rStr);
            }
            if (link.Kind == LinkType.LINK_GOTOR)
            {
                if (link.Page >= 0)
                {
                    Point pnt = link.To;
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        AnnotSkel["gotor1"],
                        link.Page,
                        FloatToString(pnt.X),
                        FloatToString(pnt.Y),
                        FloatToString(link.Zoom),
                        link.File,
                        link.File,
                        rStr);
                }
                string to = GetPdfString(link.ToStr);
                if (to.Length >= 2)
                    to = to.Substring(1, to.Length - 2);
                return string.Format(CultureInfo.InvariantCulture, AnnotSkel["gotor2"], to, link.File, rStr);
            }
            if (link.Kind == LinkType.LINK_LAUNCH)
                return string.Format(CultureInfo.InvariantCulture, AnnotSkel["launch"], link.File, link.File, rStr);
            if (link.Kind == LinkType.LINK_URI)
                return string.Format(CultureInfo.InvariantCulture, AnnotSkel["uri"], link.Uri, rStr);
            return null;
        }
    }
}
