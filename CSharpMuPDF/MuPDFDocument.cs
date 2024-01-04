using System;
using mupdf;


namespace CSharpMuPDF
{
    public class MuPDFDocument
    {
        private bool _isClosed = false;

        private bool _isEncrypted = false;

        public string METADATA = "";

        public string FONTINFO = "";

        public string GRAFTMAPS = "";

        public string SHOWNPAGES = "";

        public string INSERTEDIMAGES = "";

        private FzDocument _nativeDocument;

        public MuPDFDocument(string filename)
        {
            _nativeDocument = new FzDocument(filename);
        }

        public int GetPageCount()
        {
            return _nativeDocument.fz_count_pages();
        }

        public MuPDFSTextPage GetStextPage(int i)
        {
            return new MuPDFSTextPage(_nativeDocument.fz_load_page(i));
        }
    }
}
