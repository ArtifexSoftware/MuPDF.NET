using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMuPDF
{
    public enum PdfFieldType
    {
        PDF_FIELD_IS_READ_ONLY = 1,
        PDF_FIELD_IS_REQUIRED = 1 << 1,
        PDF_FIELD_IS_NO_EXPORT = 1 << 2,

        /* Text fields */
        PDF_TX_FIELD_IS_MULTILINE = 1 << 12,
        PDF_TX_FIELD_IS_PASSWORD = 1 << 13,
        PDF_TX_FIELD_IS_FILE_SELECT = 1 << 20,
        PDF_TX_FIELD_IS_DO_NOT_SPELL_CHECK = 1 << 22,
        PDF_TX_FIELD_IS_DO_NOT_SCROLL = 1 << 23,
        PDF_TX_FIELD_IS_COMB = 1 << 24,
        PDF_TX_FIELD_IS_RICH_TEXT = 1 << 25,

        /* Button fields */
        PDF_BTN_FIELD_IS_NO_TOGGLE_TO_OFF = 1 << 14,
        PDF_BTN_FIELD_IS_RADIO = 1 << 15,
        PDF_BTN_FIELD_IS_PUSHBUTTON = 1 << 16,
        PDF_BTN_FIELD_IS_RADIOS_IN_UNISON = 1 << 25,

        /* Choice fields */
        PDF_CH_FIELD_IS_COMBO = 1 << 17,
        PDF_CH_FIELD_IS_EDIT = 1 << 18,
        PDF_CH_FIELD_IS_SORT = 1 << 19,
        PDF_CH_FIELD_IS_MULTI_SELECT = 1 << 21,
        PDF_CH_FIELD_IS_DO_NOT_SPELL_CHECK = 1 << 22,
        PDF_CH_FIELD_IS_COMMIT_ON_SEL_CHANGE = 1 << 25,
    }
}
