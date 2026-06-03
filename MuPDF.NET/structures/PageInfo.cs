using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Typed page text dict from <see cref="TextPage.TextPage2Dict"/>; also dictionary-shaped
    /// for PyMuPDF <c>get_text("dict")</c> / <c>get_text("rawdict")</c> callers.
    /// </summary>
    public class PageInfo : TextPageDict
    {
        /// <summary>width of the clip rectangle</summary>
        public float Width { get; set; }

        /// <summary>height of the clip rectangle</summary>
        public float Height { get; set; }

        /// <summary>list of Block</summary>
        public new List<Block> Blocks { get; set; }

        /// <summary>Populate dictionary keys from typed fields (PyMuPDF dict shape).</summary>
        internal void SyncDict(bool raw) => TextPage.SyncPageInfoDict(this, raw);
    }
}
