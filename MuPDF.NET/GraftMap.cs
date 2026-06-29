using System;

namespace MuPDF.NET
{
    // Legacy type-name compatibility (MuPDF.NET exposed GraftMap).
    public class GraftMap : Graftmap
    {
        public GraftMap(Document doc) : base(doc)
        {
        }
    }

    /// <summary>
    /// Maps PDF objects when grafting pages or merging documents so shared resources are not duplicated.
    /// <para>Ports PyMuPDF <c>class Graftmap</c> (<c>src/__init__.py</c>).</para>
    /// <para>Created and cached on <see cref="Document.Graftmaps"/> during <c>insert_pdf</c> /
    /// <see cref="Document.InsertPdf"/> and <see cref="Page.ShowPdfPage"/> to avoid duplicate object copies.</para>
    /// </summary>
    public class Graftmap : IDisposable
    {
        private mupdf.PdfGraftMap _nativeGraftmap;
        private bool _disposed;
        public bool ThisOwn { get; set; } = true;

        /// <summary>Native MuPDF graft map handle.</summary>
        internal mupdf.PdfGraftMap NativeGraftMap
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Graftmap));
                return _nativeGraftmap;
            }
        }

        /// <summary>
        /// Create a graft map for the target PDF document (PyMuPDF <c>Graftmap.__init__(doc)</c>).
        /// </summary>
        /// <param name="doc">Destination PDF document (<c>_as_pdf_document(doc)</c> in Python).</param>
        public Graftmap(Document doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));
            // dst = _as_pdf_document(doc)
            // self.this = map_
            // self.thisown = True
            _nativeGraftmap = mupdf.mupdf.pdf_new_graft_map(doc.NativePdfDocument);
        }

        internal Graftmap(mupdf.PdfGraftMap gm)
        {
            _nativeGraftmap = gm ?? throw new ArgumentNullException(nameof(gm));
        }

        public mupdf.PdfGraftMap ToPdfGraftMap() => NativeGraftMap;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _nativeGraftmap?.Dispose();
                _nativeGraftmap = null;
                ThisOwn = false;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public override string ToString() => "Graftmap()";
    }
}
