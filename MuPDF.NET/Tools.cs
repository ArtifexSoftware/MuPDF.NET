using System;
using System.Text;
using System.Threading;

namespace MuPDF.NET
{
    /// <summary>
    /// Global MuPDF runtime helpers (anti-aliasing, caches, warnings, page contents).
    /// </summary>
    /// <remarks>
    /// All members are static. Some of these used to be documented on <see cref="Utils"/>
    /// (<see cref="Utils.GetId"/>, <see cref="Utils.GetAllContents"/>, <see cref="Utils.InsertContents"/>);
    /// those forwards remain for compatibility. Prefer <see cref="Tools"/> for new code.
    /// </remarks>
    public static class Tools
    {
        private static int _uniqueId;

        /// <summary>Generates a unique annotation/object ID.</summary>
        public static int GenId()
        {
            return Interlocked.Increment(ref _uniqueId);
        }

        /// <summary>
        /// Adds bytes as a new page <c>/Contents</c> object and returns its xref.
        /// </summary>
        /// <param name="page">Target PDF page.</param>
        /// <param name="newContent">Raw PDF content bytes.</param>
        /// <param name="overlay">If <see langword="true"/>, append; otherwise prepend.</param>
        public static int InsertContents(Page page, ReadOnlySpan<byte> newContent, bool overlay = true)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            var contbuf = Helpers.BufferFromBytes(newContent.ToArray());
            var pdf = page.RequireParent().NativePdfDocument;
            var pdfPage = Helpers.AsPdfPageFresh(page);
            try
            {
                int xref = Helpers.JM_insert_contents(pdf, pdfPage.obj(), contbuf, overlay);
                page.DisposeCachedPdfPage();
                return xref;
            }
            finally
            {
                pdfPage.Dispose();
            }
        }

        /// <inheritdoc cref="InsertContents(Page, ReadOnlySpan{byte}, bool)"/>
        public static int InsertContents(Page page, byte[] newContent, bool overlay = true)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            if (newContent == null) throw new ArgumentNullException(nameof(newContent));
            var pdf = page.RequireParent().NativePdfDocument;
            var contbuf = Helpers.BufferFromBytes(newContent);
            var pdfPage = Helpers.AsPdfPageFresh(page);
            try
            {
                int xref = Helpers.JM_insert_contents(pdf, pdfPage.obj(), contbuf, overlay);
                page.DisposeCachedPdfPage();
                return xref;
            }
            finally
            {
                pdfPage.Dispose();
            }
        }

        /// <summary>UTF-8 variant of <see cref="InsertContents(Page, ReadOnlySpan{byte}, bool)"/> for ASCII PDF operators.</summary>
        public static int InsertContents(Page page, string utf8Content, bool overlay = true)
        {
            if (utf8Content == null) throw new ArgumentNullException(nameof(utf8Content));
            return InsertContents(page, Encoding.UTF8.GetBytes(utf8Content), overlay);
        }

        /// <summary>Reads and concatenates all page <c>/Contents</c> stream bytes.</summary>
        public static byte[] GetAllContents(Page page)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            var pdfPage = Helpers.AsPdfPage(page, required: true);
            var res = Helpers.JM_read_contents(pdfPage.obj());
            try
            {
                return Helpers.BinFromBuffer(res);
            }
            finally
            {
                res?.Dispose();
            }
        }

        /// <summary>Empties the MuPDF glyph cache.</summary>
        public static void GlyphCacheEmpty() => mupdf.mupdf.fz_purge_glyph_cache();

        /// <summary>Returns the version of the linked MuPDF native library.</summary>
        public static string MupdfVersion() => mupdf.mupdf.FZ_VERSION;

        /// <summary>
        /// Returns accumulated MuPDF warnings and errors.
        /// </summary>
        /// <param name="reset">If <see langword="true"/>, clear the stored list after reading.</param>
        public static string MupdfWarnings(bool reset = true)
        {
            Helpers.EnsureMupdfWarningsHooked();
            mupdf.mupdf.fz_flush_warnings();
            string ret;
            lock (Helpers.JM_mupdf_warnings_store)
                ret = string.Join("\n", Helpers.JM_mupdf_warnings_store);
            if (reset)
                ResetMupdfWarnings();
            return ret;
        }

        /// <summary>Clear the stored MuPDF warning list.</summary>
        public static void ResetMupdfWarnings()
        {
            lock (Helpers.JM_mupdf_warnings_store)
                Helpers.JM_mupdf_warnings_store.Clear();
        }

        /// <summary>
        /// Sets the number of anti-aliasing bits used when rendering graphics and text (0–8).
        /// The value stays in effect until changed again. Used by <see cref="Page.GetPixmap"/>.
        /// </summary>
        /// <param name="level">Anti-aliasing bits. Values outside 0–8 are clamped by MuPDF.</param>
        public static void SetAaLevel(int level) => mupdf.mupdf.fz_set_aa_level(level);

        /// <summary>
        /// Sets the minimum stroked line width in pixels when rendering graphics.
        /// Hairlines thinner than this are drawn at least this wide. Used by <see cref="Page.GetPixmap"/>.
        /// </summary>
        /// <param name="minLineWidth">Minimum stroke width in pixels (0 = no minimum).</param>
        public static void SetGraphicsMinLineWidth(float minLineWidth) =>
            mupdf.mupdf.fz_set_graphics_min_line_width(minLineWidth);

        /// <summary>
        /// Returns the current anti-aliasing levels and graphics minimum line width.
        /// </summary>
        /// <returns>
        /// Graphics AA bits, text AA bits, and minimum stroke width in pixels.
        /// Typical defaults are graphics=8, text=8, graphicsMinLineWidth=0.
        /// </returns>
        public static (int graphics, int text, float graphicsMinLineWidth) ShowAaLevel() => (
            mupdf.mupdf.fz_graphics_aa_level(),
            mupdf.mupdf.fz_text_aa_level(),
            mupdf.mupdf.fz_graphics_min_line_width());

        /// <summary>
        /// Frees a percentage of the current MuPDF resource-store size.
        /// </summary>
        /// <param name="percent">
        /// 0 does nothing. 1–99 shrinks the store. 100 or more empties it.
        /// Least-recently-used items are removed first.
        /// </param>
        public static void StoreShrink(int percent)
        {
            if (percent >= 100)
            {
                mupdf.mupdf.fz_empty_store();
            }
            else if (percent > 0)
            {
                mupdf.mupdf.fz_shrink_store((uint)(100 - percent));
            }
        }

        /// <summary>
        /// Sets or queries whether text search/extract uses smaller glyph bbox heights.
        /// </summary>
        /// <param name="on">New value, or <see langword="null"/> to only query.</param>
        /// <returns>The current setting.</returns>
        public static bool SetSmallGlyphHeights(bool? on = null)
        {
            if (on != null)
                Helpers.SmallGlyphHeights = on.Value;
            return Helpers.SmallGlyphHeights;
        }

        /// <summary>Set fixed font width in a descendant font.</summary>
        public static bool SetFontWidth(Document doc, int xref, int width)
        {
            if (doc?.NativePdfDocument == null) return false;
            var pdf = doc.NativePdfDocument;
            using var font = mupdf.mupdf.pdf_load_object(pdf, xref);
            if (font.m_internal == null) return false;
            var dfonts = Helpers.PdfDictGet(font, mupdf.mupdf.pdf_new_name("DescendantFonts"));
            if (mupdf.mupdf.pdf_is_array(dfonts) != 0)
            {
                int n = mupdf.mupdf.pdf_array_len(dfonts);
                for (int i = 0; i < n; i++)
                {
                    var dfont = mupdf.mupdf.pdf_array_get(dfonts, i);
                    var warray = mupdf.mupdf.pdf_new_array(pdf, 3);
                    mupdf.mupdf.pdf_array_push(warray, mupdf.mupdf.pdf_new_int(0));
                    mupdf.mupdf.pdf_array_push(warray, mupdf.mupdf.pdf_new_int(65535));
                    mupdf.mupdf.pdf_array_push(warray, mupdf.mupdf.pdf_new_int(width));
                    mupdf.mupdf.pdf_dict_put(dfont, mupdf.mupdf.pdf_new_name("W"), warray);
                }
            }
            return true;
        }

        // ─── MuPDF API names (internal, same assembly) ─────────────────

        internal static int gen_id() => GenId();
        internal static void glyph_cache_empty() => GlyphCacheEmpty();
        internal static string mupdf_version() => MupdfVersion();
        internal static string mupdf_warnings(bool reset = true) => MupdfWarnings(reset);
        internal static void reset_mupdf_warnings() => ResetMupdfWarnings();
        internal static void set_aa_level(int level) => SetAaLevel(level);
        internal static void set_graphics_min_line_width(float min_line_width) => SetGraphicsMinLineWidth(min_line_width);
        internal static (int graphics, int text, float graphicsMinLineWidth) show_aa_level() => ShowAaLevel();
        internal static void store_shrink(int percent) => StoreShrink(percent);
        internal static bool set_small_glyph_heights(bool? on = null) => SetSmallGlyphHeights(on);
        internal static bool set_font_width(Document doc, int xref, int width) => SetFontWidth(doc, xref, width);
        internal static int _insert_contents(Page page, byte[] newcont, bool overlay = true) => InsertContents(page, newcont, overlay);
        internal static byte[] _get_all_contents(Page page) => GetAllContents(page);
    }
}