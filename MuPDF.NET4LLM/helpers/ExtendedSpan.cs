using System.Collections.Generic;
using MuPDF.NET;

namespace MuPDF.NET4LLM.Helpers
{
    /// <summary>
    /// Extended span information for text line extraction.
    /// Mirrors the span dictionaries produced by pymupdf4llm in the Python helpers.
    /// </summary>
    public class ExtendedSpan
    {
        public string Text { get; set; }
        public Rect Bbox { get; set; }
        public float Size { get; set; }
        public string Font { get; set; }
        public int Flags { get; set; }
        public int CharFlags { get; set; }
        public float Alpha { get; set; }
        public int Line { get; set; }
        public int Block { get; set; }
        public Point Dir { get; set; }
        public List<Char> Chars { get; set; }
    }
}
