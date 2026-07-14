namespace PDF4LLM
{
    /// <summary>PDF4LLM package version metadata.</summary>
    public static class VersionInfo
    {
        /// <summary>PDF4LLM NuGet package version.</summary>
        public const string Version = Artifex.Versions.PDF4LLM;

        /// <summary>MuPDF bind version required from MuPDF.NET (<see cref="MuPDF.NET.Utils.VersionBind"/>).</summary>
        public const string RequiredPyMuPDF = Artifex.Versions.PyMuPDF;

        /// <summary>PyMuPDF4LLM Python package version (pymupdf4llm on PyPI).</summary>
        public const string RequiredPyMuPDF4LLM = Artifex.Versions.PyMuPDF4LLM;

        /// <summary>Required <c>pymupdf-layout</c> PyPI package version for the layout bridge.</summary>
        public const string RequiredPyMuPDFLayout = Artifex.Versions.PyMuPDF4LLM;

        /// <summary>Native MuPDF version bundled with MuPDF.NET.</summary>
        public const string RequiredMuPdf = Artifex.Versions.MuPDF;
    }
}