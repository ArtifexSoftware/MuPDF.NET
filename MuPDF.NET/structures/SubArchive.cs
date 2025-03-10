using System.Collections.Generic;

namespace MuPDF.NET
{
    public class SubArchive
    {
        /// <summary>
        /// Format of Archive
        /// </summary>
        public string Fmt { get; set; }

        /// <summary>
        /// entities in Archive
        /// </summary>
        public List<string> Entries { get; set; }

        /// <summary>
        /// File path
        /// </summary>
        public string Path { get; set; }
    }
}
