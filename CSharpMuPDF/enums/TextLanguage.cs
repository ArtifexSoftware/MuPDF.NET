using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMuPDF
{
    public enum TextLanguage
    {
        FZ_LANG_UNSET = 0,
        FZ_LANG_ur = (('u' - 'a' + 1) + (('r' - 'a' + 1) * 27)),
        FZ_LANG_urd = (('u' - 'a' + 1) + (('r' - 'a' + 1) * 27) + (('d' - 'a' + 1) * 27 * 27)),
        FZ_LANG_ko = (('k' - 'a' + 1) + (('o' - 'a' + 1) * 27)),
        FZ_LANG_ja = (('j' - 'a' + 1) + (('a' - 'a' + 1) * 27)),
        FZ_LANG_zh = (('z' - 'a' + 1) + (('h' - 'a' + 1) * 27)),
        FZ_LANG_zh_Hans = (('z' - 'a' + 1) + (('h' - 'a' + 1) * 27) + (('s' - 'a' + 1) * 27 * 27)),
        FZ_LANG_zh_Hant = (('z' - 'a' + 1) + (('h' - 'a' + 1) * 27) + (('t' - 'a' + 1) * 27 * 27))
    }
}
