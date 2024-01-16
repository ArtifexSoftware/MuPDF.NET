using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMuPDF
{
    public enum TextFlags
    {
        TEXT_PRESERVE_LIGATURES = 1,

        TEXT_PRESERVE_WHITESPACE = 2,

        TEXT_PRESERVE_IMAGES = 4,

        TEXT_INHIBIT_SPACES = 8,

        TEXT_DEHYPHENATE = 16,

        TEXT_PRESERVE_SPANS = 32,

        TEXT_MEDIABOX_CLIP = 64
    }
}
