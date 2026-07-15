using System;
using MuPDF.NET;
using PDF4LLM.Helpers;

namespace PDF4LLM
{
    public static partial class PdfExtractor
    {
        /// <summary>Whether a layout provider is registered.</summary>
        public static bool LayoutAvailable => Page.GetLayoutProvider != null;

        /// <summary>Register the callback that supplies page layout boxes.</summary>
        /// <param name="provider">Callback that returns layout boxes for a page, or <see langword="null"/> to clear the provider.</param>
        public static void SetLayoutProvider(Func<Page, object> provider)
        {
            Page.GetLayoutProvider = provider;
        }

        /// <summary>Enable or disable the layout analysis pipeline.</summary>
        /// <param name="useLayout">When true, activate layout; when false, restore legacy header helpers.</param>
        public static void SetUseLayout(bool useLayout)
        {
            UseLayout = useLayout;

            if (useLayout)
            {
                // IdentifyHeaders and TocHeaders are not available.
                LayoutActivation.Activate();
            }
            else
            {
                LayoutActivation.Deactivate();
            }
        }

        /// <summary>Legacy header detection type (only when <see cref="UseLayout"/> is false).</summary>
        public static Type IdentifyHeadersType =>
            UseLayout ? null : typeof(IdentifyHeaders);

        /// <summary>TOC-based header type (only when <see cref="UseLayout"/> is false).</summary>
        public static Type TocHeadersType =>
            UseLayout ? null : typeof(TocHeaders);
    }
}
