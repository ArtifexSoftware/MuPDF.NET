using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class TableFlags
    {
        public const float TABLE_UNSET = 0.0f;
        public const float TABLE_DEFAULT_SNAP_TOLERANCE = 3.0f;
        public const float TABLE_DEFAULT_JOIN_TOLERANCE = 3.0f;
        public const float TABLE_DEFAULT_MIN_WORDS_VERTICAL = 3.0f;
        public const float TABLE_DEFAULT_MIN_WORDS_HORIZONTAL = 1.0f;
        public const float TABLE_DEFAULT_X_TOLERANCE = 3.0f;
        public const float TABLE_DEFAULT_Y_TOLERANCE = 3.0f;
        public const float TABLE_DEFAULT_X_DENSITY = 7.25f;
        public const float TABLE_DEFAULT_Y_DENSITY = 13.0f;

        public static readonly string[] TABLE_STRATEGIES = { "lines", "lines_strict", "text", "explicit" };
        public static readonly Dictionary<string, string> TABLE_LIGATURES = new Dictionary<string, string>
        {
            { "ﬀ", "ff" },
            { "ﬃ", "ffi" },
            { "ﬄ", "ffl" },
            { "ﬁ", "fi" },
            { "ﬂ", "fl" },
            { "ﬆ", "st" },
            { "ﬅ", "st" }
        };
    }
}
