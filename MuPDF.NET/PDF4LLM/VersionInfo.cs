namespace MuPDF.NET.PDF4LLM
{
    /// <summary>Version metadata for the compiled-in LLM extract (historical MuPDF.NET.PDF4LLM package id).</summary>
    public static class VersionInfo
    {
        /// <summary>Historical MuPDF.NET.PDF4LLM package version (type-forward identity).</summary>
        public const string Version = BuildVersions.Package;

        /// <summary>Required <c>layout</c> PyPI package version for the layout bridge.</summary>
        public const string RequiredPyMuPDFLayout = BuildVersions.RequiredPyMuPDFLayout;

        /// <summary>Native MuPDF version expected from MuPDF.NET.</summary>
        public const string RequiredMuPdf = BuildVersions.RequiredMuPdf;
    }
}
