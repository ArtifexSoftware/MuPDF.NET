using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class MuPDFDocumentWriter
    {
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

        public MuPDFDeviceWrapper BeginPage(Rect mediabox)
        {
            FzDevice device = _nativeDocumentWriter.fz_begin_page(mediabox.ToFzRect());
            MuPDFDeviceWrapper deviceWrapper = new MuPDFDeviceWrapper(device);
            return deviceWrapper;
        }

        public void Close()
        {
            _nativeDocumentWriter.fz_close_document_writer();
        }

        public void EndPage()
        {
            _nativeDocumentWriter.fz_end_page();
        }
    }
}
