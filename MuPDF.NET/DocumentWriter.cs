using System;
using System.IO;

namespace MuPDF.NET
{
    /// <summary>
    /// Writes multi-page documents (primarily PDF) using MuPDF's document writer API.
    /// </summary>
    /// <remarks>
    /// <para>Typical use is with
    /// <see cref="Story"/> to layout HTML/CSS into PDF pages. Legacy API:
    /// <see href="https://mupdfnet.readthedocs.io/en/latest/classes/DocumentWriter.html"/>.</para>
    /// <para>Use <c>using</c> (<see cref="IDisposable"/>) like a context manager: call
    /// <see cref="Close"/> (or dispose) to flush output.</para>
    /// </remarks>
    public partial class DocumentWriter : IDisposable
    {
        private mupdf.FzDocumentWriter _nativeWriter;
        private mupdf.FzBuffer _outputBuffer;
        private FilePtrOutput _filePtr;
        private bool _disposed;

        /// <summary>
        /// Gets the underlying MuPDF writer; throws if disposed.
        /// </summary>
        internal mupdf.FzDocumentWriter NativeWriter
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(DocumentWriter));
                return _nativeWriter;
            }
        }

        /// <summary>
        /// Creates a writer that saves to a file path.
        /// </summary>
        /// <param name="path">Output file path.</param>
        /// <param name="options">
        /// Save options for the output PDF (e.g. <c>compress</c>, <c>clean</c>). See MuPDF
        /// <c>mutool convert</c> option strings.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        public DocumentWriter(string path, string options = "")
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            _nativeWriter = new mupdf.FzDocumentWriter(
                path, options ?? "", mupdf.FzDocumentWriter.PathType.PathType_PDF);
        }

        /// <summary>
        /// Creates a writer that saves to a file path from a <see cref="FileInfo"/> (path-like).
        /// </summary>
        /// <param name="path">Output file.</param>
        /// <param name="options">PDF writer options (see <see cref="DocumentWriter(string, string)"/>).</param>
        /// <inheritdoc cref="DocumentWriter(string, string)"/>
        public DocumentWriter(global::System.IO.FileInfo path, string options = "")
            : this(path?.FullName ?? throw new ArgumentNullException(nameof(path)), options)
        {
        }

        /// <summary>
        /// Creates a writer that writes PDF into a growable buffer.
        /// </summary>
        /// <param name="stream">Memory stream that receives PDF bytes when the writer is closed.</param>
        /// <param name="options">PDF writer options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        public DocumentWriter(MemoryStream stream, string options = "")
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            _filePtr = new FilePtrOutput(stream);
            _nativeWriter = new mupdf.FzDocumentWriter(
                _filePtr, options ?? "", mupdf.FzDocumentWriter.OutputType.OutputType_PDF);
        }

        /// <summary>
        /// Creates a writer backed by an <see cref="mupdf.FzBuffer"/> (same as ).
        /// </summary>
        /// <param name="buffer">Buffer; bytes are extracted in <see cref="Close"/> when this is the primary sink.</param>
        /// <param name="format">Document format (default <c>pdf</c>).</param>
        /// <param name="options">Writer options string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        public DocumentWriter(mupdf.FzBuffer buffer, string format = "pdf", string options = "")
        {
            _outputBuffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _nativeWriter = new mupdf.FzDocumentWriter(buffer, format, options ?? "");
        }

        internal DocumentWriter(mupdf.FzDocumentWriter writer)
        {
            _nativeWriter = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        /// <summary>
        /// Starts a new output page of the given size.
        /// </summary>
        /// <param name="mediabox">Page size in points (media box).</param>
        /// <returns>
        /// A <see cref="DeviceWrapper"/> for drawing; pass to <see cref="Story.Draw"/> or
        /// <see cref="DisplayList.Run"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Writer is closed.</exception>
        public DeviceWrapper BeginPage(Rect mediabox)
        {
            var device = mupdf.mupdf.fz_begin_page(NativeWriter, mediabox.ToFzRect());
            return new DeviceWrapper(device);
        }

        /// <summary>
        /// Finishes the current page and appends it to the output document.
        /// </summary>
        /// <remarks>Flushes pending page data. Call once per <see cref="BeginPage"/>.</remarks>
        /// <exception cref="ObjectDisposedException">Writer is closed.</exception>
        public void EndPage()
        {
            mupdf.mupdf.fz_end_page(NativeWriter);
        }

        /// <summary>
        /// Closes the writer and flushes all pending output.
        /// </summary>
        /// <returns>
        /// PDF bytes when the writer owns an <see cref="mupdf.FzBuffer"/>; otherwise <see langword="null"/>
        /// (file and <see cref="MemoryStream"/> sinks keep data in the target you provided).
        /// </returns>
        /// <remarks>Required to complete the document. Idempotent after the first successful call.</remarks>
        public byte[] Close()
        {
            if (_disposed)
                return null;

            _nativeWriter.fz_close_document_writer();
            _nativeWriter.Dispose();
            _nativeWriter = null;

            byte[] bytes = null;
            if (_outputBuffer != null)
            {
                bytes = _outputBuffer.fz_buffer_extract();
                _outputBuffer = null;
            }

            _filePtr?.Dispose();
            _filePtr = null;

            _disposed = true;
            GC.SuppressFinalize(this);
            return bytes;
        }

        /// <inheritdoc/>
        /// <remarks>Calls <see cref="Close"/> if not already closed.</remarks>
        public void Dispose()
        {
            if (!_disposed)
                Close();
        }

        /// <inheritdoc/>
        public override string ToString() => "DocumentWriter()";

        // ─── MuPDF API names (internal, same assembly) ─────────────────

        internal DeviceWrapper begin_page(Rect mediabox) => BeginPage(mediabox);

        internal void end_page() => EndPage();

        internal byte[] close() => Close();
    }
}