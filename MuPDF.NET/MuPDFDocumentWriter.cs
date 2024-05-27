using mupdf;

namespace MuPDF.NET
{
    public class MuPDFDocumentWriter
    {
        static MuPDFDocumentWriter()
        {
            if (!File.Exists("mupdfcsharp.dll"))
                Utils.LoadEmbeddedDll();
        }

        private FzDocumentWriter _nativeDocumentWriter;

        public MuPDFDocumentWriter(string path, string options = "")
        {
            _nativeDocumentWriter = new FzDocumentWriter(path, options, FzDocumentWriter.PathType.PathType_PDF);
        }

        public MuPDFDocumentWriter(MemoryStream memory, string options = "")
        {
            FilePtrOutput filePtr = new FilePtrOutput(memory);
            _nativeDocumentWriter = new FzDocumentWriter(filePtr, options, FzDocumentWriter.OutputType.OutputType_PDF);
        }

        /// <summary>
        /// Start a new output page of a given dimension.
        /// </summary>
        /// <param name="mediabox">a rectangle specifying the page size.</param>
        /// <returns></returns>
        public DeviceWrapper BeginPage(Rect mediabox)
        {
            FzDevice device = _nativeDocumentWriter.fz_begin_page(mediabox.ToFzRect());
            DeviceWrapper deviceWrapper = new DeviceWrapper(device);
            return deviceWrapper;
        }

        /// <summary>
        /// Close the output file. This method is required for writing any pending data.
        /// </summary>
        public void Close()
        {
            _nativeDocumentWriter.fz_close_document_writer();
        }

        /// <summary>
        /// Finish a page. This flushes any pending data and appends the page to the output document.
        /// </summary>
        public void EndPage()
        {
            _nativeDocumentWriter.fz_end_page();
        }
    }
}
