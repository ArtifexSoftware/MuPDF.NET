namespace MuPDF.NET.PDF4LLM
{
    /// <summary>MuPDF.NET.PDF4LLM package version metadata.</summary>
    public static class VersionInfo
    {
        /// <summary>MuPDF.NET.PDF4LLM NuGet package version.</summary>
        public const string Version = BuildVersions.Package;

        /// <summary>Required <c>pymupdf-layout</c> PyPI package version for the layout bridge.</summary>
        public const string RequiredPyMuPDFLayout = BuildVersions.RequiredPyMuPDFLayout;

        /// <summary>Native MuPDF version expected from MuPDF.NET.</summary>
        public const string RequiredMuPdf = BuildVersions.RequiredMuPdf;
    }
}
