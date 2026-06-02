using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Descriptor for one mounted sub-archive inside a <see cref="Archive"/>.
    /// </summary>
    /// <remarks>
    /// Each entry in <see cref="Archive.EntryList"/> records how content was added
    /// (<c>dir</c>, <c>zip</c>, <c>tar</c>, <c>tree</c>, or <c>multi</c>) and which paths it exposes.
    /// </remarks>
    public class SubArchive
    {
        /// <summary>
        /// Sub-archive format: <c>dir</c>, <c>zip</c>, <c>tar</c>, <c>tree</c>, or <c>multi</c>.
        /// </summary>
        public string Fmt { get; set; }

        /// <summary>
        /// Entry names (file paths) contributed by this sub-archive.
        /// </summary>
        public List<string> Entries { get; set; } = new List<string>();

        /// <summary>
        /// Virtual mount path prefix, or <c>null</c> for the archive root.
        /// </summary>
        public string Path { get; set; }
    }
}
