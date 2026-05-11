using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a document outline (bookmark / table of contents entry).
    /// </summary>
    public class Outline
    {
        private mupdf.FzOutline _nativeOutline;

        internal Outline(mupdf.FzOutline outline)
        {
            _nativeOutline = outline;
        }

        /// <summary>
        /// Outline entry title.
        /// </summary>
        public string Title => _nativeOutline.m_internal?.title ?? "";

        /// <summary>
        /// Outline target URI.
        /// </summary>
        public string Uri => _nativeOutline.m_internal?.uri ?? "";

        /// <summary>
        /// Target page number.
        /// </summary>
        public int Page => _nativeOutline.m_internal?.page.page ?? -1;

        /// <summary>
        /// Check if target is external.
        /// </summary>
        /// <remarks>Python <c>Outline.is_external</c> uses <c>mupdf.fz_is_external_link</c>.</remarks>
        public bool IsExternal
        {
            get
            {
                string u = Uri;
                if (string.IsNullOrEmpty(u)) return false;
                try
                {
                    return mupdf.mupdf.fz_is_external_link(u) != 0;
                }
                catch
                {
                    return u.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        || u.StartsWith("mailto", StringComparison.OrdinalIgnoreCase)
                        || u.StartsWith("ftp", StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        /// <summary>
        /// Outline open state.
        /// </summary>
        public bool IsOpen
        {
            get => _nativeOutline.m_internal != null && _nativeOutline.m_internal.is_open != 0;
        }

        /// <summary>
        /// First child outline.
        /// </summary>
        public Outline Down
        {
            get
            {
                if (_nativeOutline.m_internal?.down == null) return null;
                return new Outline(new mupdf.FzOutline(_nativeOutline.m_internal.down));
            }
        }

        /// <summary>
        /// Next sibling outline.
        /// </summary>
        public Outline Next
        {
            get
            {
                if (_nativeOutline.m_internal?.next == null) return null;
                return new Outline(new mupdf.FzOutline(_nativeOutline.m_internal.next));
            }
        }

        /// <summary>Python <c>Outline.dest</c> — <see cref="LinkDest"/> (not the native x/y rect).</summary>
        public LinkDest LinkDest => new LinkDest(this, null);

        /// <summary>Python <c>Outline.destination(document)</c> for named-destination resolution.</summary>
        public LinkDest Destination(Document document) => new LinkDest(this, document);

        /// <summary>Python <c>Outline.destination</c> (snake_case).</summary>
        public LinkDest destination(Document document) => Destination(document);

        /// <summary>
        /// Outline destination point as a degenerate rectangle.
        /// </summary>
        public Rect Dest
        {
            get
            {
                if (_nativeOutline.m_internal == null) return null;
                float x = _nativeOutline.m_internal.x;
                float y = _nativeOutline.m_internal.y;
                return new Rect(x, y, x, y);
            }
        }

        /// <summary>
        /// Check if outline is valid.
        /// </summary>
        public bool IsValid => _nativeOutline.m_internal != null;

        /// <summary>
        /// Flatten the outline tree to a list of (level, title, page) tuples.
        /// </summary>
        public List<(int level, string title, int page)> Flatten()
        {
            var result = new List<(int, string, int)>();
            FlattenRecurse(this, result, 1);
            return result;
        }

        private static void FlattenRecurse(Outline ol, List<(int, string, int)> result, int level)
        {
            var current = ol;
            while (current != null && current.IsValid)
            {
                result.Add((level, current.Title, current.Page));
                var d = current.Down;
                if (d != null) FlattenRecurse(d, result, level + 1);
                current = current.Next;
            }
        }

        /// <summary>
        /// Returns a string representation of this outline entry.
        /// </summary>
        public override string ToString() => $"Outline('{Title}', page={Page})";

        // Python/legacy compatibility aliases (mirrors _alias(Outline, ...)).
        public bool is_external() => IsExternal;
        public bool is_open() => IsOpen;
        public LinkDest dest => LinkDest;
    }
}
