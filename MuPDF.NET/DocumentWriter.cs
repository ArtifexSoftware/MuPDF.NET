using mupdf;
using System;
using System.IO;

namespace MuPDF.NET
{
    public class DocumentWriter : IDisposable
    {
        static DocumentWriter()
        {
            Utils.InitApp();
        }

        private FilePtrOutput _filePtr;
        private FzDocumentWriter _nativeDocumentWriter;
        private bool _disposed;

        public DocumentWriter(string path, string options = "")
        {
            _nativeDocumentWriter = new FzDocumentWriter(path, options, FzDocumentWriter.PathType.PathType_PDF);
        }

        public DocumentWriter(MemoryStream memory, string options = "")
        {
            _filePtr = new FilePtrOutput(memory);
            _nativeDocumentWriter = new FzDocumentWriter(_filePtr, options, FzDocumentWriter.OutputType.OutputType_PDF);
        }

        /// <summary>
        /// Start a new output page of a given dimension.
        /// </summary>
        /// <param name="mediabox">a rectangle specifying the page size.</param>
        /// <returns></returns>
        public DeviceWrapper BeginPage(Rect mediabox)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
            FzDevice device = _nativeDocumentWriter.fz_begin_page(mediabox.ToFzRect());
            DeviceWrapper deviceWrapper = new DeviceWrapper(device);
            return deviceWrapper;
        }

        /// <summary>
        /// Close the output file. This method is required for writing any pending data.
        /// </summary>
        public void Close()
        {
            if (_disposed)
                return;
            _nativeDocumentWriter.fz_close_document_writer();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _nativeDocumentWriter?.Dispose();
            _nativeDocumentWriter = null;
            _filePtr?.Dispose();
            _filePtr = null;
            _disposed = true;
        }

        /// <summary>
        /// Finish a page. This flushes any pending data and appends the page to the output document.
        /// </summary>
        public void EndPage()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
            _nativeDocumentWriter.fz_end_page();
        }
    }
}
