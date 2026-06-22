namespace PDF4LLM
{
    /// <summary>PDF4LLM package version metadata.</summary>
    public static class VersionInfo
    {
        /// <summary>PDF4LLM NuGet package version.</summary>
        public const string Version = Artifex.Versions.PDF4LLM;

        /// <summary>PyMuPDF bind version required from MuPDF.NET (<see cref="MuPDF.NET.Utils.VersionBind"/>).</summary>
        public const string RequiredPyMuPDF = Artifex.Versions.PyMuPDF;

        /// <summary>PyMuPDF4LLM / pymupdf-layout version used by the layout Python bridge.</summary>
        public const string RequiredPyMuPDF4LLM = Artifex.Versions.PyMuPDF4LLM;

        /// <summary>Native MuPDF version bundled with MuPDF.NET.</summary>
        public const string RequiredMuPdf = Artifex.Versions.MuPdf;
    }
}
