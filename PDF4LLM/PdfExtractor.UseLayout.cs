using System;
using MuPDF.NET;
using PDF4LLM.Helpers;

namespace PDF4LLM
{
    public static partial class PdfExtractor
    {
        /// <summary>Whether a layout provider has been registered via <see cref="SetLayoutProvider"/>.</summary>
        public static bool LayoutAvailable => Page.GetLayoutProvider != null;

        /// <summary>
        /// Register a layout analyzer for <see cref="Page.GetLayout"/> / <c>layout_information</c>.
        /// Call before <see cref="SetUseLayout"/>(true) when you have a layout binding.
        /// </summary>
        public static void SetLayoutProvider(Func<Page, object> provider)
        {
            Page.GetLayoutProvider = provider;
        }

        /// <summary>
        /// Switch between layout and legacy RAG pipelines (<c>use_layout</c> in pymupdf4llm).
        /// </summary>
        public static void SetUseLayout(bool yes)
        {
            UseLayout = yes;

            if (!yes)
            {
                LayoutActivation.Deactivate();
            }
        }

        /// <summary>Legacy header detection (only when <see cref="UseLayout"/> is false).</summary>
        public static Type IdentifyHeadersType =>
            UseLayout ? null : typeof(IdentifyHeaders);

        /// <summary>TOC-based headers (only when <see cref="UseLayout"/> is false).</summary>
        public static Type TocHeadersType =>
            UseLayout ? null : typeof(TocHeaders);
    }
}
