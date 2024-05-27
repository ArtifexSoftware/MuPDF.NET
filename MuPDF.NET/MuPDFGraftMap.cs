using mupdf;

namespace MuPDF.NET
{
    public class MuPDFGraftMap
    {
        static MuPDFGraftMap()
        {
            if (!File.Exists("mupdfcsharp.dll"))
                Utils.LoadEmbeddedDll();
        }

        private PdfGraftMap _nativeGraftMap;

        public bool ThisOwn { get; set; }

        public PdfGraftMap ToPdfGraftMap()
        {
            return _nativeGraftMap;
        }

        public MuPDFGraftMap(Document doc)
        {
            PdfDocument pdf = Document.AsPdfDocument(doc);
            PdfGraftMap map = pdf.pdf_new_graft_map();
            _nativeGraftMap = map;
            ThisOwn = true;
        }

        public void Dispose()
        {
            ThisOwn = false;
            _nativeGraftMap.Dispose();
        }
    }
}
