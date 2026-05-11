using System;

namespace MuPDF.NET
{
    /// <summary>
    /// Writes pages to an output file.
    /// </summary>
    public class DocumentWriter : IDisposable
    {
        private mupdf.FzDocumentWriter _nativeWriter;
        private mupdf.FzBuffer _outputBuffer;
        private bool _disposed;

        internal mupdf.FzDocumentWriter NativeWriter
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(DocumentWriter));
                return _nativeWriter;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWriter"/> class writing to a file path.
        /// </summary>
        public DocumentWriter(string path, string options = "")
        {
            _nativeWriter = new mupdf.FzDocumentWriter(path, options ?? "", mupdf.FzDocumentWriter.PathType.PathType_PDF);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentWriter"/> class writing to a buffer.
        /// </summary>
        public DocumentWriter(mupdf.FzBuffer buffer, string format = "pdf", string options = "")
        {
            _outputBuffer = buffer;
            _nativeWriter = new mupdf.FzDocumentWriter(buffer, format, options ?? "");
        }

        /// <summary>
        /// Initializes a new instance writing PDF to an internal buffer.
        /// Used by Story for in-memory PDF generation.
        /// </summary>
        public DocumentWriter(Rect mediabox)
        {
            _outputBuffer = mupdf.mupdf.fz_new_buffer(1024);
            _nativeWriter = new mupdf.FzDocumentWriter(_outputBuffer, "pdf", "");
        }

        internal DocumentWriter(mupdf.FzDocumentWriter writer)
        {
            _nativeWriter = writer;
        }

        /// <summary>
        /// Start a new output page with given MediaBox.
        ///
        /// Returns a Device to receive drawing commands.
        /// </summary>
        public mupdf.FzDevice BeginPage(Rect mediabox)
        {
            return mupdf.mupdf.fz_begin_page(NativeWriter, mediabox.ToFzRect());
        }

        /// <summary>
        /// Finish the current output page.
        /// </summary>
        public void EndPage()
        {
            mupdf.mupdf.fz_end_page(NativeWriter);
        }

        /// <summary>
        /// Flush pending output and close the writer. Returns the PDF bytes if using a buffer-based writer.
        /// </summary>
        public byte[] Close()
        {
            if (!_disposed)
            {
                NativeWriter.fz_close_document_writer();
            }
            if (_outputBuffer != null)
                return _outputBuffer.fz_buffer_extract();
            return null;
        }

        // ─── IDisposable ────────────────────────────────────────────────

        /// <summary>
        /// Releases all resources used by the <see cref="DocumentWriter"/>.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                try { NativeWriter.fz_close_document_writer(); } catch { }
                _nativeWriter?.Dispose();
                _nativeWriter = null;
                _outputBuffer = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~DocumentWriter() { Dispose(); }

        /// <summary>
        /// Returns a string that represents the current document writer.
        /// </summary>
        public override string ToString() => "DocumentWriter()";
    }
}
