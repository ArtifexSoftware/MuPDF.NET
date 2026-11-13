namespace MuPDF.NET.PDF4LLM.Layout
{
    /// <summary>
    /// Layout analysis via the installed layout worker.
    /// Run the layout-worker setup MSBuild target once to install it.
    /// Enabled by default when the worker is available.
    /// </summary>
    public static class PyMuPdfLayout
    {
        /// <summary>Whether the layout package can be imported.</summary>
        public static bool IsAvailable => PyMuPdfLayoutBridge.IsAvailable;

        /// <summary>Layout package version string.</summary>
        public static string Version => PyMuPdfLayoutBridge.Version;

        /// <summary>Whether a layout provider is currently registered.</summary>
        public static bool IsActivated => PyMuPdfLayoutBridge.IsActivated;

        /// <summary>Start layout analysis and register <see cref="MuPDF.NET.Page.GetLayoutProvider"/>.</summary>
        public static bool Activate() => PyMuPdfLayoutBridge.TryActivate();

        /// <summary>Stop layout analysis and clear the layout provider.</summary>
        public static void Deactivate() => PyMuPdfLayoutBridge.Deactivate();
    }
}