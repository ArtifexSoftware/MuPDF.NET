using System;
using System.Text;
using System.Threading;

namespace MuPDF.NET
{
    /// <summary>
    /// Static utilities mirroring PyMuPDF <c>pymupdf.TOOLS</c> (<c>src/__init__.py</c>, class <c>TOOLS</c> ~24849).
    /// </summary>
    /// <remarks>
    /// <para>PyMuPDF uses <c>@staticmethod</c> so no instance is required. Public members use PascalCase;
    /// <c>internal</c> snake_case aliases match Python for same-assembly tests. <c>JM_*</c> helpers live on <see cref="Helpers"/>.</para>
    /// <para>Legacy MuPDF.NET readthedocs listed some of these under <see cref="Utils"/>:
    /// <see cref="Utils.GetId"/> → <see cref="GenId"/>,
    /// <see cref="Utils.GetAllContents"/> → <see cref="GetAllContents"/>,
    /// <see cref="Utils.InsertContents"/> → <see cref="InsertContents"/>.
    /// The <see cref="Utils"/> forwards are kept for backward compatibility.</para>
    /// </remarks>
    public static class Tools
    {
        private static int _uniqueId;

        /// <summary>PyMuPDF <c>TOOLS.gen_id</c>.</summary>
        public static int GenId()
        {
            // global TOOLS_JM_UNIQUE_ID
            // TOOLS_JM_UNIQUE_ID += 1
            return Interlocked.Increment(ref _uniqueId);
        }

        /// <summary>PyMuPDF <c>TOOLS._insert_contents</c> — add bytes as a new <c>/Contents</c> stream; returns xref of the new stream.</summary>
        /// <remarks>Python docstring: Add bytes as a new /Contents object for a page, and return its xref.</remarks>
        /// <param name="page">Target PDF page.</param>
        /// <param name="newContent">Raw PDF content bytes.</param>
        /// <param name="overlay">If <see langword="true"/>, append; otherwise prepend (same as PyMuPDF <c>overlay</c> argument).</param>
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

        /// <summary>PyMuPDF <c>TOOLS._get_all_contents</c>.</summary>
        public static byte[] GetAllContents(Page page)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            // page = _as_pdf_page(page.this)
            var pdfPage = Helpers.AsPdfPage(page, required: true);
            // res = JM_read_contents(page.obj())
            var res = Helpers.JM_read_contents(pdfPage.obj());
            try
            {
                // result = JM_BinFromBuffer(res)
                return Helpers.BinFromBuffer(res);
            }
            finally
            {
                res?.Dispose();
            }
        }

        /// <summary>PyMuPDF <c>TOOLS.glyph_cache_empty</c>.</summary>
        /// <remarks>Python docstring: Empty the glyph cache.</remarks>
        public static void GlyphCacheEmpty() => mupdf.mupdf.fz_purge_glyph_cache();

        /// <summary>PyMuPDF <c>TOOLS.mupdf_version</c>.</summary>
        /// <remarks>Python docstring: Get version of MuPDF binary build.</remarks>
        public static string MupdfVersion() => mupdf.mupdf.FZ_VERSION;

        /// <summary>
        /// Get MuPDF warnings/errors with optional reset (PyMuPDF <c>TOOLS.mupdf_warnings</c>).
        /// </summary>
        public static string MupdfWarnings(bool reset = true)
        {
            Helpers.EnsureMupdfWarningsHooked();
            // mupdf.fz_flush_warnings()
            mupdf.mupdf.fz_flush_warnings();
            string ret;
            lock (Helpers.JM_mupdf_warnings_store)
                ret = string.Join("\n", Helpers.JM_mupdf_warnings_store);
            if (reset)
                ResetMupdfWarnings();
            return ret;
        }

        /// <summary>Clear the stored MuPDF warning list (PyMuPDF <c>TOOLS.reset_mupdf_warnings</c>).</summary>
        public static void ResetMupdfWarnings()
        {
            // global JM_mupdf_warnings_store
            lock (Helpers.JM_mupdf_warnings_store)
                Helpers.JM_mupdf_warnings_store.Clear();
        }

        /// <summary>PyMuPDF <c>TOOLS.set_aa_level</c>.</summary>
        /// <remarks>Python docstring: Set anti-aliasing level.</remarks>
        public static void SetAaLevel(int level) => mupdf.mupdf.fz_set_aa_level(level);

        /// <summary>PyMuPDF <c>TOOLS.set_graphics_min_line_width</c>.</summary>
        /// <remarks>Python docstring: Set the graphics minimum line width.</remarks>
        public static void SetGraphicsMinLineWidth(float minLineWidth) =>
            mupdf.mupdf.fz_set_graphics_min_line_width(minLineWidth);

        /// <summary>PyMuPDF <c>TOOLS.show_aa_level</c>.</summary>
        /// <remarks>Python docstring: Show anti-aliasing values.</remarks>
        public static (int graphics, int text, float graphicsMinLineWidth) ShowAaLevel() => (
            mupdf.mupdf.fz_graphics_aa_level(),
            mupdf.mupdf.fz_text_aa_level(),
            mupdf.mupdf.fz_graphics_min_line_width());

        /// <summary>PyMuPDF <c>TOOLS.store_shrink</c>.</summary>
        /// <remarks>Python docstring: Free 'percent' of current store size.</remarks>
        public static void StoreShrink(int percent)
        {
            if (percent >= 100)
            {
                mupdf.mupdf.fz_empty_store();
                // return 0  (Python returned 0 here)
            }
            else if (percent > 0)
            {
                mupdf.mupdf.fz_shrink_store((uint)(100 - percent));
            }
            // fixme: return gctx->store->size.
        }

        /// <summary>Set or query small glyph heights mode (PyMuPDF <c>TOOLS.set_small_glyph_heights</c>).</summary>
        public static bool SetSmallGlyphHeights(bool? on = null)
        {
            if (on != null)
                Helpers.SmallGlyphHeights = on.Value;
            return Helpers.SmallGlyphHeights;
        }

        /// <summary>Set fixed font width in a descendant font (PyMuPDF <c>TOOLS.set_font_width</c>).</summary>
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

        // ─── PyMuPDF API names (internal, same assembly) ─────────────────

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
