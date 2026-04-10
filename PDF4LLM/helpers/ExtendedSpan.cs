using System.Collections.Generic;
using MuPDF.NET;
using Newtonsoft.Json;

namespace PDF4LLM.Helpers
{
    /// <summary>
    /// Extended span information for text line extraction.
    /// Mirrors the span dictionaries produced by LLM helpers.
    /// JSON names follow <c>pdf4llm.helpers.document_layout</c> / rawdict-style keys.
    /// </summary>
    public class ExtendedSpan
    {
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("bbox")]
        public Rect Bbox { get; set; }
        [JsonProperty("size")]
        public float Size { get; set; }
        [JsonProperty("font")]
        public string Font { get; set; }
        [JsonProperty("flags")]
        public int Flags { get; set; }
        [JsonProperty("char_flags")]
        public int CharFlags { get; set; }
        [JsonProperty("alpha")]
        public float Alpha { get; set; }
        [JsonProperty("line")]
        public int Line { get; set; }
        [JsonProperty("block")]
        public int Block { get; set; }
        [JsonProperty("dir")]
        public Point Dir { get; set; }
        [JsonProperty("chars")]
        public List<Char> Chars { get; set; }
    }
}
