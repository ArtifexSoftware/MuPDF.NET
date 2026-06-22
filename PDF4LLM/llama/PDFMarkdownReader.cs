using System;
using System.Collections.Generic;
using System.IO;
using MuPDF.NET;

namespace PDF4LLM.Llama
{
    /// <summary>Read PDF files and emit LlamaIndex documents.</summary>
    public class PDFMarkdownReader
    {
        public Func<Dictionary<string, object>, Dictionary<string, object>> MetaFilter { get; set; }

        /// <summary>Creates a reader with optional metadata filtering.</summary>
        /// <param name="metaFilter">Optional callback to transform per-page metadata before documents are returned.</param>
        public PDFMarkdownReader(Func<Dictionary<string, object>, Dictionary<string, object>> metaFilter = null)
        {
            MetaFilter = metaFilter;
        }

        /// <summary>
        /// Loads list of documents from PDF file and also accepts extra information in dict format.
        /// </summary>
        /// <param name="filePath">Path to the PDF file (string or path-like object).</param>
        /// <param name="extraInfo">Additional metadata merged into each page document's <see cref="LlamaIndexDocument.ExtraInfo"/>.</param>
        /// <param name="loadKwargs">Optional keyword arguments forwarded to the Markdown extractor.</param>
        public List<LlamaIndexDocument> LoadData(
            object filePath,
            object extraInfo = null,
            Dictionary<string, object> loadKwargs = null)
        {
            if (!IsValidFilePath(filePath))
                throw new ArgumentException("file_path must be a string or Path.");

            Dictionary<string, object> extraDict;
            if (extraInfo == null)
                extraDict = new Dictionary<string, object>();
            else if (extraInfo is Dictionary<string, object> dict)
                extraDict = dict;
            else
                throw new ArgumentException("extra_info must be a dictionary.");

            if (loadKwargs == null)
                loadKwargs = new Dictionary<string, object>();

            string filePathStr = filePath is string s ? s : filePath.ToString();

            var hdrInfo = new Helpers.IdentifyHeaders(filePathStr);

            Document doc = new Document(filePathStr);
            var docs = new List<LlamaIndexDocument>();

            try
            {
                for (int i = 0; i < doc.PageCount; i++)
                {
                    var pageExtraInfo = new Dictionary<string, object>(extraDict);
                    docs.Add(ProcessDocPage(
                        doc, pageExtraInfo, filePathStr, i, hdrInfo, loadKwargs));
                }
            }
            finally
            {
                doc.Close();
            }

            return docs;
        }

        private static bool IsValidFilePath(object filePath)
        {
            if (filePath is string)
                return true;
            if (filePath is System.IO.FileInfo)
                return true;
            return filePath != null && filePath.GetType().Name == "Path";
        }

        private LlamaIndexDocument ProcessDocPage(
            Document doc,
            Dictionary<string, object> extraInfo,
            string filePath,
            int pageNumber,
            object hdrInfo,
            Dictionary<string, object> loadKwargs)
        {
            extraInfo = ProcessDocMeta(doc, filePath, pageNumber, extraInfo);

            if (MetaFilter != null)
                extraInfo = MetaFilter(extraInfo);

            string text = Helpers.MuPdfRag.ToMarkdown(
                doc,
                pages: new List<int> { pageNumber },
                hdrInfo: hdrInfo,
                loadKwargs);

            return new LlamaIndexDocument
            {
                Text = text,
                ExtraInfo = extraInfo
            };
        }

        /// <summary>Processes metas of a PDF document.</summary>
        private Dictionary<string, object> ProcessDocMeta(
            Document doc,
            string filePath,
            int pageNumber,
            Dictionary<string, object> extraInfo)
        {
            if (extraInfo == null)
                extraInfo = new Dictionary<string, object>();

            foreach (var kvp in doc.MetaData)
                extraInfo[kvp.Key] = kvp.Value;

            extraInfo["page"] = pageNumber + 1;
            extraInfo["total_pages"] = doc.PageCount;
            extraInfo["file_path"] = filePath;

            return extraInfo;
        }
    }

    /// <summary>LlamaIndex-compatible document payload.</summary>
    public class LlamaIndexDocument
    {
        public string Text { get; set; }
        public Dictionary<string, object> ExtraInfo { get; set; }
    }
}
