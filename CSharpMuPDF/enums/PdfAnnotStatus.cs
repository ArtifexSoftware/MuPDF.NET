using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMuPDF
{
    public enum PdfAnnotStatus
    {
        PDF_ANNOT_IS_INVISIBLE = 1 << (1 - 1),
        PDF_ANNOT_IS_HIDDEN = 1 << (2 - 1),
        PDF_ANNOT_IS_PRINT = 1 << (3 - 1),
        PDF_ANNOT_IS_NO_ZOOM = 1 << (4 - 1),
        PDF_ANNOT_IS_NO_ROTATE = 1 << (5 - 1),
        PDF_ANNOT_IS_NO_VIEW = 1 << (6 - 1),
        PDF_ANNOT_IS_READ_ONLY = 1 << (7 - 1),
        PDF_ANNOT_IS_LOCKED = 1 << (8 - 1),
        PDF_ANNOT_IS_TOGGLE_NO_VIEW = 1 << (9 - 1),
        PDF_ANNOT_IS_LOCKED_CONTENTS = 1 << (10 - 1)
    }
}
