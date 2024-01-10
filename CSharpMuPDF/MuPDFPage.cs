using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace CSharpMuPDF
{
    public class MuPDFPage
    {
        private PdfPage _nativePage;

        private MuPDFDocument _parent;

        public int NUMBER { get; set; }

        public override string ToString()
        {
            
            return base.ToString(); 
        }

        public MuPDFDocument PARENT
        {
            get
            {
                return _parent;
            }
        }

        public MuPDFPage(PdfPage nativePage, MuPDFDocument parent)
        {
            _nativePage = nativePage;
            _parent = parent;

            if (_nativePage == null)
                NUMBER = 0;
            else
                NUMBER = _nativePage.m_internal.super.number;
        }

        public MuPDFPage(FzPage fzPage, MuPDFDocument parent)
        {
            _nativePage = fzPage.pdf_page_from_fz_page();
            _parent = parent;

            if (_nativePage == null)
                NUMBER = 0;
            else
                NUMBER = _nativePage.m_internal.super.number;
        }


    }
}
