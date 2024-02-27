using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class MuPDFGraftMap : IDisposable
    {
        private PdfGraftMap _nativeGraftMap;

        public bool ThisOwn = false;

        public PdfGraftMap ToPdfGraftMap()
        {
            return _nativeGraftMap;
        }

        public MuPDFGraftMap(MuPDFDocument doc) 
        {
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(doc);
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
