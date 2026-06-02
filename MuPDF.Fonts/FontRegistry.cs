using System;
using System.Collections.Generic;
using MuPDF.Fonts.MupdfFontsData;

namespace MuPDF.Fonts
{
    /// <summary>Font descriptor and buffer loaders generated from pymupdf-fonts sources.</summary>
    public static class FontRegistry
    {
        /// <summary>One pymupdf-fonts descriptor entry.</summary>
        public sealed class Entry
        {
            /// <summary>True when the face is bold.</summary>
            public bool Bold { get; set; }
            /// <summary>True when the face is italic.</summary>
            public bool Italic { get; set; }
            /// <summary>Returns decoded font bytes (cached per font entry).</summary>
            public Func<byte[]> Loader { get; set; } = () => Array.Empty<byte>();
        }

        private static readonly Dictionary<string, Entry> s_map = Build();

        /// <summary>
        /// Mapping from font code (for example <c>notos</c>) to descriptor and byte loader.
        /// </summary>
        public static IReadOnlyDictionary<string, Entry> Descriptors => s_map;

        private static Dictionary<string, Entry> Build()
        {
            var map = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var kv in Registry.Map)
            {
                map[kv.Key] = new Entry
                {
                    Bold = kv.Value.Bold,
                    Italic = kv.Value.Italic,
                    Loader = kv.Value.Loader,
                };
            }
            return map;
        }
    }
}
