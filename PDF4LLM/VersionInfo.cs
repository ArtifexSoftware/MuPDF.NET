namespace PDF4LLM
{
    /// <summary>
    /// Version information for PDF4LLM (aligned with pymupdf4llm versions_file).
    /// </summary>
    public static class VersionInfo
    {
        /// <summary>Must match <see cref="MuPDF.NET.Utils.VersionBind"/> major.minor.patch.</summary>
        public static readonly (int Major, int Minor, int Patch) MinimumMuPDFVersion = (1, 27, 2);
        // MuPDF.NET Utils.VersionBind must match this triple (patch level of PyMuPDF bind).

        public const string Version = "1.27.2.10";
    }
}
