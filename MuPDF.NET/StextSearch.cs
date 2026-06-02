using System;
using System.Collections.Generic;
using System.Text;

namespace MuPDF.NET
{
    /// <summary>
    /// Text search over an <c>fz_stext_page</c> (used by <see cref="TextPage.Search"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ports PyMuPDF <c>JM_search_stext_page</c> in <c>src/__init__.py</c> (~21384) and
    /// <c>src/extra.i</c> (~4194). This type is <c>internal</c>; there is no public surface here.
    /// User-facing search is <see cref="Page.SearchFor"/> / <see cref="TextPage.Search"/>.
    /// </para>
    /// <para>
    /// Builds a flattened haystack from the text page, finds matches, then walks characters in lockstep
    /// with the haystack index to emit merged <see cref="Quad"/> hits (<c>on_highlight_char</c> in C).
    /// </para>
    /// <para>
    /// <c>hfuzz</c> / <c>vfuzz</c> (0.2 / 0.1 × char size) merge kerning gaps on the same line without
    /// joining words across large horizontal gaps (Python: "merge kerns but not large gaps").
    /// </para>
    /// </remarks>
    internal static class StextSearch
    {
        /// <summary>Mutable hit list while scanning (C <c>struct highlight</c> / Python <c>Hits</c>).</summary>
        private sealed class SearchHits
        {
            public readonly List<Quad> Quads = new List<Quad>();
            public int Len;
            /// <summary>Horizontal merge tolerance as fraction of char size (Python <c>hfuzz = 0.2</c>).</summary>
            public float Hfuzz = 0.2f;
            /// <summary>Vertical merge tolerance as fraction of char size (Python <c>vfuzz = 0.1</c>).</summary>
            public float Vfuzz = 0.1f;
        }

        /// <summary>
        /// Search a structured text page for <paramref name="needle"/> and return highlight quads.
        /// PyMuPDF <c>JM_search_stext_page(page, needle)</c> → <c>extra.JM_search_stext_page</c>.
        /// </summary>
        /// <returns>Empty list when <paramref name="needle"/> is empty or no match is found.</returns>
        internal static List<Quad> JM_search_stext_page(mupdf.FzStextPage page, string needle)
        {
            if (string.IsNullOrEmpty(needle))
                return new List<Quad>();

            var hits = new SearchHits();
            // Python: buffer_ = JM_new_buffer_from_stext_page(page); haystack_string = fz_string_from_buffer(buffer_)
            string haystackString = JM_new_string_from_stext_page(page);
            int haystack = 0;
            var (begin, end) = FindString(haystackString, haystack, needle);
            if (begin == null)
                return hits.Quads;

            int beginPos = begin.Value;
            int endPos = end.Value;
            bool inside = false;
            var rect = page.m_internal.mediabox;
            using var pageRect = new mupdf.FzRect(rect);

            // Walk blocks/lines/chars; haystack index advances per char and per '\n' after each line/block.
            for (var blockIter = page.begin();
                 blockIter.m_internal != page.end().m_internal;
                 blockIter = blockIter.__increment__())
            {
                var block = blockIter.__deref__();
                if (block.m_internal.type != mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                    continue;

                for (var lineIter = block.begin();
                     lineIter.m_internal != block.end().m_internal;
                     lineIter = lineIter.__increment__())
                {
                    var line = lineIter.__deref__();
                    for (var chIter = line.begin();
                         chIter.m_internal != line.end().m_internal;
                         chIter = chIter.__increment__())
                    {
                        var ch = chIter.__deref__();
                        // Skip chars outside page mediabox when clip is finite (JM_rects_overlap).
                        if (mupdf.mupdf.fz_is_infinite_rect(pageRect) == 0)
                        {
                            var r = Helpers.JM_char_bbox(line.m_internal, ch.m_internal);
                            if (!Helpers.JM_rects_overlap(pageRect, r))
                                goto next_char;
                        }

                        // try_new_match: track whether haystack index is inside [begin, end).
                        while (true)
                        {
                            if (!inside)
                            {
                                if (haystack >= beginPos)
                                    inside = true;
                            }
                            if (inside)
                            {
                                if (haystack < endPos)
                                {
                                    OnHighlightChar(hits, line.m_internal, ch.m_internal);
                                    break;
                                }
                                inside = false;
                                (begin, end) = FindString(haystackString, haystack, needle);
                                if (begin == null)
                                    return hits.Quads;
                                beginPos = begin.Value;
                                endPos = end.Value;
                                continue;
                            }
                            break;
                        }
                        haystack++;
                    next_char:;
                    }
                    // Python: assert haystack_string[haystack] == '\n'
                    haystack++;
                }
                haystack++;
            }
            return hits.Quads;
        }

        /// <summary>
        /// Build the flat search string from a text page (Python <c>JM_new_buffer_from_stext_page</c> +
        /// <c>fz_string_from_buffer</c>). Inserts <c>'\n'</c> after each line and each block.
        /// </summary>
        private static string JM_new_string_from_stext_page(mupdf.FzStextPage page)
        {
            var rect = page.m_internal.mediabox;
            using var pageRect = new mupdf.FzRect(rect);
            var sb = new StringBuilder();
            for (var blockIter = page.begin();
                 blockIter.m_internal != page.end().m_internal;
                 blockIter = blockIter.__increment__())
            {
                var block = blockIter.__deref__();
                if (block.m_internal.type != mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                    continue;
                for (var lineIter = block.begin();
                     lineIter.m_internal != block.end().m_internal;
                     lineIter = lineIter.__increment__())
                {
                    var line = lineIter.__deref__();
                    for (var chIter = line.begin();
                         chIter.m_internal != line.end().m_internal;
                         chIter = chIter.__increment__())
                    {
                        var ch = chIter.__deref__();
                        if (mupdf.mupdf.fz_is_infinite_rect(pageRect) == 0)
                        {
                            var r = Helpers.JM_char_bbox(line.m_internal, ch.m_internal);
                            if (!Helpers.JM_rects_overlap(pageRect, r))
                                continue;
                        }
                        sb.Append(char.ConvertFromUtf32(ch.m_internal.c));
                    }
                    sb.Append('\n');
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Find next occurrence of <paramref name="needle"/> in <paramref name="haystack"/> from
        /// <paramref name="offset"/> (C <c>find_string</c> / Python <c>find_string</c> + <c>match_string</c>).
        /// </summary>
        /// <remarks>
        /// Uses ordinal case-insensitive matching as a .NET approximation of PyMuPDF
        /// <c>chartocanon</c> / <c>match_string</c> canonical comparison in <c>extra.i</c>.
        /// </remarks>
        private static (int? begin, int? end) FindString(string haystack, int offset, string needle)
        {
            if (offset < 0 || offset >= haystack.Length)
                return (null, null);
            int idx = haystack.IndexOf(needle, offset, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return (null, null);
            return (idx, idx + needle.Length);
        }

        /// <summary>
        /// Append or extend a highlight quad for character <paramref name="ch"/> (C <c>on_highlight_char</c>).
        /// </summary>
        private static void OnHighlightChar(SearchHits hits, mupdf.fz_stext_line line, mupdf.fz_stext_char ch)
        {
            float vfuzz = ch.size * hits.Vfuzz;
            float hfuzz = ch.size * hits.Hfuzz;
            using var chQuad = Helpers.JM_char_quad(line, ch);
            var quad = new Quad(chQuad);
            if (hits.Len > 0)
            {
                var end = hits.Quads[hits.Len - 1];
                if (Hdist(line.dir, end.LR, quad.LL) < hfuzz
                    && Vdist(line.dir, end.LR, quad.LL) < vfuzz
                    && Hdist(line.dir, end.UR, quad.UL) < hfuzz
                    && Vdist(line.dir, end.UR, quad.UL) < vfuzz)
                {
                    end.UR = quad.UR;
                    end.LR = quad.LR;
                    return;
                }
            }
            hits.Quads.Add(quad);
            hits.Len++;
        }

        /// <summary>Horizontal distance along line direction (C <c>hdist</c>).</summary>
        private static float Hdist(mupdf.fz_point dir, Point a, Point b)
        {
            float dx = (float)(b.X - a.X);
            float dy = (float)(b.Y - a.Y);
            return Math.Abs(dx * dir.x + dy * dir.y);
        }

        /// <summary>Vertical distance perpendicular to line direction (C <c>vdist</c>).</summary>
        private static float Vdist(mupdf.fz_point dir, Point a, Point b)
        {
            float dx = (float)(b.X - a.X);
            float dy = (float)(b.Y - a.Y);
            return Math.Abs(dx * dir.y + dy * dir.x);
        }
    }
}
