using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// A single PDF outline item (bookmark / table-of-contents entry).
    /// </summary>
    /// <remarks>
    /// <para>Outlines form a tree: follow <see cref="Down"/> for the first child and <see cref="Next"/> for
    /// siblings at the same level. The document’s root entry is <see cref="Document.GetOutline"/> (PyMuPDF
    /// <c>Document.outline</c>).</para>
    /// <para>Legacy API:
    /// <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Outline.html"/>.</para>
    /// <para>Ports PyMuPDF <c>class Outline</c> (<c>src/__init__.py</c>).</para>
    /// </remarks>
    public class Outline
    {
        private readonly mupdf.FzOutline _nativeOutline;

        internal Outline(mupdf.FzOutline outline)
        {
            _nativeOutline = outline ?? throw new ArgumentNullException(nameof(outline));
        }

        /// <summary>
        /// The link destination details object (legacy <c>Dest</c>; PyMuPDF <c>dest</c>).
        /// </summary>
        /// <remarks>
        /// Does not resolve named destinations; use <see cref="Destination(Document)"/> when
        /// <see cref="LinkDest.Kind"/> may be <c>LINK_NAMED</c>.
        /// </remarks>
        public LinkDest Dest => new LinkDest(this, null);

        /// <summary>
        /// Like <see cref="Dest"/> but resolves named destinations using <paramref name="document"/>.
        /// </summary>
        /// <param name="document">Document used to resolve <c>LINK_NAMED</c> targets.</param>
        /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
        public LinkDest Destination(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            return new LinkDest(this, document);
        }

        /// <summary>
        /// The next outline item on the next level down; <see langword="null"/> if this item has no children.
        /// </summary>
        public Outline? Down
        {
            get
            {
                var child = _nativeOutline.down();
                if (child?.m_internal == null)
                {
                    child?.Dispose();
                    return null;
                }
                return new Outline(child);
            }
        }

        /// <summary>
        /// The next outline item at the same level; <see langword="null"/> if this is the last sibling.
        /// </summary>
        public Outline? Next
        {
            get
            {
                var sibling = _nativeOutline.next();
                if (sibling?.m_internal == null)
                {
                    sibling?.Dispose();
                    return null;
                }
                return new Outline(sibling);
            }
        }

        /// <summary>
        /// The page number (0-based) this bookmark points to (PyMuPDF <c>page</c>).
        /// </summary>
        public int Page =>
            _nativeOutline.m_internal != null ? _nativeOutline.m_internal.page.page : -1;

        /// <summary>
        /// The item’s title, or <see langword="null"/> if missing (PyMuPDF <c>title</c>).
        /// </summary>
        public string? Title =>
            _nativeOutline.m_internal != null ? _nativeOutline.m_internal.title : null;

        /// <summary>
        /// Whether sub-outlines should be expanded (<see langword="true"/>) or collapsed
        /// (<see langword="false"/>); interpreted by PDF viewers (PyMuPDF <c>is_open</c>).
        /// </summary>
        public bool IsOpen =>
            _nativeOutline.m_internal != null && _nativeOutline.m_internal.is_open != 0;

        /// <summary>
        /// Whether the target is outside the current document (PyMuPDF <c>is_external</c>).
        /// </summary>
        /// <remarks>Uses <c>mupdf.fz_is_external_link</c> on <see cref="Uri"/> when present.</remarks>
        public bool IsExternal
        {
            get
            {
                string? u = Uri;
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
        /// Link target URI (PyMuPDF <c>uri</c>).
        /// </summary>
        /// <remarks>
        /// Interpret together with <see cref="IsExternal"/>:
        /// <list type="bullet">
        /// <item><description><see cref="IsExternal"/> is <see langword="true"/>: external resource
        /// (<c>http://</c>, <c>file://</c>, <c>mailto:</c>, etc.).</description></item>
        /// <item><description><see cref="IsExternal"/> is <see langword="false"/>: internal location—often
        /// <c>#page=nnnn</c> (1-based page in PDF) or a named destination.</description></item>
        /// </list>
        /// </remarks>
        public string? Uri =>
            _nativeOutline.m_internal != null ? _nativeOutline.m_internal.uri : null;

        /// <summary>Horizontal destination coordinate (PyMuPDF <c>x</c>).</summary>
        public float X =>
            _nativeOutline.m_internal != null ? _nativeOutline.m_internal.x : 0f;

        /// <summary>Vertical destination coordinate (PyMuPDF <c>y</c>).</summary>
        public float Y =>
            _nativeOutline.m_internal != null ? _nativeOutline.m_internal.y : 0f;

        /// <summary>Whether the native outline node is present.</summary>
        public bool IsValid => _nativeOutline.m_internal != null;

        /// <summary>
        /// Flattens the outline tree to <c>(level, title, page)</c> tuples (MuPDF.NET helper; not in legacy docs).
        /// </summary>
        public List<(int level, string title, int page)> Flatten()
        {
            var result = new List<(int, string, int)>();
            FlattenRecurse(this, result, 1);
            return result;
        }

        /// <inheritdoc/>
        public override string ToString() => $"Outline('{Title ?? ""}', page={Page})";

        private static void FlattenRecurse(Outline ol, List<(int, string, int)> result, int level)
        {
            var current = ol;
            while (current != null && current.IsValid)
            {
                result.Add((level, current.Title ?? "", current.Page));
                var d = current.Down;
                if (d != null) FlattenRecurse(d, result, level + 1);
                current = current.Next;
            }
        }

        // PyMuPDF snake_case aliases (same assembly / dynamic interop)
        internal bool is_external() => IsExternal;
        internal bool is_open() => IsOpen;
        internal LinkDest dest => Dest;
        internal LinkDest destination(Document document) => Destination(document);
    }
}
