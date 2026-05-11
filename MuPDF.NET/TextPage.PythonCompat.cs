using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Python-name compatibility for <see cref="TextPage"/> (<c>src/__init__.py</c> <c>TextPage</c>).
    /// </summary>
    public partial class TextPage
    {
        /// <summary>Python <c>TextPage.search(needle, hit_max=0, quads=1)</c> with <c>quads=True</c> (quad hits).</summary>
        public List<Quad> search(string needle, int hit_max = 16)
            => Search(needle, hit_max);

        /// <summary>Python <c>TextPage.search(..., quads=False)</c> (merged axis-aligned rectangles).</summary>
        public List<Rect> search_rects(string needle, int hit_max = 16)
            => SearchRects(needle, hit_max);
    }
}
