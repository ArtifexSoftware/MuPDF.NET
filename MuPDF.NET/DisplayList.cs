using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a display list (recorded drawing operations for a page).
    /// </summary>
    public class DisplayList : IDisposable
    {
        private mupdf.FzDisplayList _nativeDl;
        private bool _disposed;

        internal mupdf.FzDisplayList NativeDisplayList
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(DisplayList));
                return _nativeDl;
            }
        }

        internal DisplayList(mupdf.FzDisplayList dl)
        {
            _nativeDl = dl;
        }

        /// <summary>
        /// Initializes a new display list with the given media box.
        /// </summary>
        public DisplayList(Rect mediabox)
        {
            _nativeDl = mupdf.mupdf.fz_new_display_list(mediabox.ToFzRect());
        }

        /// <summary>
        /// MediaBox of the display list.
        /// </summary>
        public Rect Rect
        {
            get
            {
                var r = mupdf.mupdf.fz_bound_display_list(NativeDisplayList);
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
        }

        /// <summary>
        /// Generate a Pixmap from the display list.
        /// </summary>
        public Pixmap GetPixmap(Matrix matrix = null, Colorspace cs = null, bool alpha = false, IRect clip = null)
        {
            var ctm = (matrix ?? Matrix.Identity).ToFzMatrix();
            var colorspace = (cs ?? Colorspace.CsRGB).ToFzColorspace();
            var pix = mupdf.mupdf.fz_new_pixmap_from_display_list(NativeDisplayList, ctm, colorspace, alpha ? 1 : 0);
            return new Pixmap(pix);
        }

        /// <summary>
        /// Generate a TextPage from the display list.
        /// </summary>
        public TextPage GetTextPage(int flags = 3)
        {
            var opts = new mupdf.fz_stext_options();
            opts.flags = flags;
            var stp = new mupdf.FzStextPage(NativeDisplayList, new mupdf.FzStextOptions(opts));
            return new TextPage(stp);
        }

        /// <summary>
        /// Replay the display list through a device.
        /// </summary>
        public void Run(mupdf.FzDevice dev, Matrix ctm, Rect area = null)
        {
            var cookie = new mupdf.FzCookie();
            mupdf.mupdf.fz_run_display_list(NativeDisplayList, dev, ctm.ToFzMatrix(), new mupdf.FzRect(), cookie);
        }

        /// <summary>
        /// Search for a string. Returns list of Quad hit rectangles.
        /// </summary>
        public List<Quad> Search(string needle, int maxHits = 16)
        {
            using var tp = GetTextPage();
            return tp.Search(needle, maxHits);
        }

        /// <summary>
        /// Extract text from the display list.
        ///
        /// Options: 'text', 'html', 'xhtml', 'xml'.
        /// </summary>
        public string GetText(string option = "text", int flags = 0)
        {
            using var tp = GetTextPage(flags);
            switch (option.ToLower())
            {
                case "text": return tp.ExtractText();
                case "html": return tp.ExtractHtml();
                case "xhtml": return tp.ExtractXhtml();
                case "xml": return tp.ExtractXml();
                default: return tp.ExtractText();
            }
        }

        // ─── IDisposable ────────────────────────────────────────────────

        /// <summary>
        /// Releases all resources used by the <see cref="DisplayList"/>.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _nativeDl?.Dispose();
                _nativeDl = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~DisplayList() { Dispose(); }

        /// <summary>
        /// Returns a string that represents the current display list.
        /// </summary>
        public override string ToString() => $"DisplayList({Rect})";

        // Python/legacy compatibility aliases (mirrors _alias(DisplayList, ...)).
        public Pixmap get_pixmap(Matrix matrix = null, Colorspace cs = null, bool alpha = false, IRect clip = null)
            => GetPixmap(matrix, cs, alpha, clip);
        public TextPage get_textpage(int flags = 3) => GetTextPage(flags);
        public TextPage getTextPage(int flags = 3) => get_textpage(flags);
    }
}
