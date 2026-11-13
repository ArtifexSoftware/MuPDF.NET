using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>LlamaIndex-compatible PDF markdown reader.</summary>
    public class PDFMarkdownReader : global::MuPDF.NET.PDF4LLM.Llama.PDFMarkdownReader
    {
        /// <summary>Creates a reader with optional metadata filtering.</summary>
        public PDFMarkdownReader(
            Func<Dictionary<string, object>, Dictionary<string, object>> metaFilter = null)
            : base(metaFilter)
        {
        }
    }

    /// <summary>LlamaIndex-compatible document payload.</summary>
    public class LlamaIndexDocument : global::MuPDF.NET.PDF4LLM.Llama.LlamaIndexDocument
    {
    }
}
