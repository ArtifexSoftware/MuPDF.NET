using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// PyMuPDF <c>get_text("dict")</c> / <c>get_text("rawdict")</c> result: dictionary-shaped
    /// with typed <see cref="Blocks"/> for legacy MuPDF.NET callers.
    /// </summary>
    public class TextPageDict : Dictionary<string, object>
    {
        public TextPageDict()
        {
        }

        public TextPageDict(Dictionary<string, object> source)
        {
            if (source == null)
                return;
            foreach (var kv in source)
                this[kv.Key] = kv.Value;
        }

        /// <summary>Typed blocks view (same data as <c>["blocks"]</c>).</summary>
        public virtual List<Block> Blocks
        {
            get
            {
                if (this is PageInfo pageInfo && pageInfo.Blocks != null)
                    return pageInfo.Blocks;
                return TextPage.ToPageInfo(this).Blocks;
            }
        }
    }
}
