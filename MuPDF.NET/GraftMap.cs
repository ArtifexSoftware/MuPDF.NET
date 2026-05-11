using System;

namespace MuPDF.NET
{
    /// <summary>
    /// Optimizes object copying between PDF documents, avoiding duplicate copies of shared resources.
    /// </summary>
    public class Graftmap : IDisposable
    {
        private mupdf.PdfGraftMap _nativeGraftmap;
        private bool _disposed;

        internal mupdf.PdfGraftMap NativeGraftMap
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Graftmap));
                return _nativeGraftmap;
            }
        }

        /// <summary>
        /// Initializes a new graft map for the specified document.
        /// </summary>
        public Graftmap(Document doc)
        {
            _nativeGraftmap = mupdf.mupdf.pdf_new_graft_map(doc.NativePdfDocument);
        }

        internal Graftmap(mupdf.PdfGraftMap gm)
        {
            _nativeGraftmap = gm;
        }

        // ─── IDisposable ────────────────────────────────────────────────

        /// <summary>
        /// Releases all resources used by the <see cref="Graftmap"/>.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _nativeGraftmap?.Dispose();
                _nativeGraftmap = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~Graftmap() { Dispose(); }

        /// <summary>
        /// Returns a string that represents the current graft map.
        /// </summary>
        public override string ToString() => "Graftmap()";
    }
}
