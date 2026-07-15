namespace PDF4LLM.Layout
{
    /// <summary>
    /// ONNX layout analysis via an external Python worker.
    /// Run <c>dotnet msbuild -t:PDF4LLMSetupLayoutPython</c> once to install layout bridge.
    /// </summary>
    public static class PyMuPdfLayout
    {
        /// <summary>Whether the layout package can be imported in Python.</summary>
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