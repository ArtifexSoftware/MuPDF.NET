using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public enum PdfCrypt
    {
        PDF_ENCRYPT_KEEP,
        PDF_ENCRYPT_NONE,
        PDF_ENCRYPT_RC4_40,
        PDF_ENCRYPT_RC4_128,
        PDF_ENCRYPT_AES_128,
        PDF_ENCRYPT_AES_256,
        PDF_ENCRYPT_UNKNOWN
    }
}
