namespace PDF4LLM
{
    /// <summary>PDF4LLM package version metadata.</summary>
    public static class VersionInfo
    {
        /// <summary>PDF4LLM NuGet package version.</summary>
        public const string Version = Artifex.Versions.PDF4LLM;

        /// <summary>Required <c>pymupdf-layout</c> PyPI package version for the layout bridge.</summary>
        public const string RequiredPyMuPDFLayout = Artifex.Versions.PyMuPDFLayout;

        /// <summary>Native MuPDF version bundled with MuPDF.NET.</summary>
        public const string RequiredMuPdf = Artifex.Versions.MuPDF;
    }
}
