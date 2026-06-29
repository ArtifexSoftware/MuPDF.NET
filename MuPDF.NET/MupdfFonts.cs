using System;
using System.Collections.Generic;
using MuPDF.Fonts;

namespace MuPDF.NET
{
    /// <summary>Bridge to <c>MuPDF.Fonts</c> package data (PyMuPDF <c>fitz_fontdescriptors</c>).</summary>
    /// <remarks>Font naming uses codes like <c>notos</c>, <c>notosit</c>, <c>notosbo</c>, <c>notosbi</c>.</remarks>
    internal static class MupdfFonts
    {
        /// <summary>
        /// One MuPDF built-in font entry: bold/italic flags and byte loader.
        /// </summary>
        internal sealed class FontDescriptor
        {
            /// <summary>True when face is bold.</summary>
            public bool Bold { get; set; }
            /// <summary>True when face is italic.</summary>
            public bool Italic { get; set; }
            /// <summary>Returns font bytes.</summary>
            public Func<byte[]> Loader { get; set; } = () => Array.Empty<byte>();

            internal bool bold => Bold;
            internal bool italic => Italic;
            internal Func<byte[]> loader => Loader;
        }

        private static readonly Dictionary<string, FontDescriptor> s_fitzFontDescriptors = LoadFitzFontDescriptors();

        /// <summary>Module-level <c>fitz_fontdescriptors</c> (read-only).</summary>
        internal static IReadOnlyDictionary<string, FontDescriptor> FitzFontDescriptors => s_fitzFontDescriptors;

        /// <summary>Load descriptors from <c>MuPDF.Fonts</c> package.</summary>
        private static Dictionary<string, FontDescriptor> LoadFitzFontDescriptors()
        {
            var result = new Dictionary<string, FontDescriptor>(StringComparer.Ordinal);
            foreach (var kv in FontRegistry.Descriptors)
            {
                result[kv.Key] = new FontDescriptor
                {
                    Bold = kv.Value.Bold,
                    Italic = kv.Value.Italic,
                    Loader = kv.Value.Loader,
                };
            }
            return result;
        }
    }
}
