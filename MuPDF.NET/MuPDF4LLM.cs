using System;
using Engine = MuPDF.NET.PDF4LLM.MuPDF4LLM;

namespace MuPDF.NET
{
    /// <summary>
    /// Layout analysis switch for Markdown / JSON / text extract.
    /// Same namespace as <see cref="Document"/>: <c>using MuPDF.NET;</c> is enough.
    /// </summary>
    public static class MuPDF4LLM
    {
        /// <summary>Enable or disable the layout analysis pipeline.</summary>
        /// <param name="useLayout">
        /// <see langword="true"/> turns the pipeline on and registers a layout provider when one is available.
        /// <see langword="false"/> turns it off (classic extract).
        /// </param>
        public static void SetLayout(bool useLayout) => Engine.SetUseLayout(useLayout);

        /// <summary>Same as <see cref="SetLayout"/>.</summary>
        public static void SetUseLayout(bool useLayout) => Engine.SetUseLayout(useLayout);

        /// <summary>
        /// Whether the layout analysis pipeline is currently enabled.
        /// Same namespace as <see cref="Document"/>: <c>using MuPDF.NET;</c> is enough.
        /// Same value as <see cref="Utils.LayoutEnabled"/>.
        /// </summary>
        public static bool LayoutEnabled => Engine.UseLayout;

        /// <summary>
        /// Whether a layout provider is currently registered.
        /// Same namespace as <see cref="Document"/>: <c>using MuPDF.NET;</c> is enough.
        /// Same value as <see cref="Utils.LayoutAvailable"/>.
        /// </summary>
        public static bool LayoutAvailable => Engine.LayoutAvailable;

        /// <summary>
        /// Register the callback that supplies page layout boxes, or <see langword="null"/> to clear it.
        /// Same namespace as <see cref="Document"/>: <c>using MuPDF.NET;</c> is enough.
        /// </summary>
        public static void SetLayoutProvider(Func<Page, object> provider)
            => Engine.SetLayoutProvider(provider);
    }
}
