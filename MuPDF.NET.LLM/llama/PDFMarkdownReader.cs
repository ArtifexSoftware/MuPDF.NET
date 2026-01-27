using System;
using System.Collections.Generic;
using System.IO;
using MuPDF.NET;

namespace MuPDF.NET.LLM.Llama
{
    /// <summary>
    /// LlamaIndex-compatible PDF reader using MuPDF.NET.LLM.
    /// Ported and adapted from the Python module llama/pdf_markdown_reader.py.
    /// Note: This is a C# implementation that provides similar functionality
    /// to the original Python `PDFMarkdownReader`.
    /// </summary>
    public class PDFMarkdownReader
    {
        public Func<Dictionary<string, object>, Dictionary<string, object>> MetaFilter { get; set; }

        public PDFMarkdownReader(Func<Dictionary<string, object>, Dictionary<string, object>> metaFilter = null)
        {
            MetaFilter = metaFilter;
        }

        /// <summary>
        /// Loads list of documents from PDF file and also accepts extra information in dict format.
        /// </summary>
        /// <param name="filePath">
        /// Path-like object (string or <c>Path</c>-like) pointing to the PDF file.
        /// </param>
        /// <param name="extraInfo">
        /// Optional base metadata dictionary that is copied and enriched per page
        /// (file path, page number, total pages, document metadata).
        /// </param>
        /// <param name="loadKwargs">
        /// Optional keyword arguments controlling rendering:
        /// <c>write_images</c>, <c>embed_images</c>, <c>image_path</c>,
        /// <c>image_format</c>, <c>force_text</c>, <c>show_progress</c> – these are
        /// forwarded to <see cref="Helpers.MuPdfRag.ToMarkdown(MuPDF.NET.Document,System.Collections.Generic.List{int},object,bool,bool,bool,bool,bool,string,string,float,string,bool,bool,bool,System.Collections.Generic.List{float},int,float,float?,string,int?,float,bool,bool,bool,bool,bool)"/>.
        /// </param>
        /// <returns>
        /// A list of <see cref="LlamaIndexDocument"/> instances, one per page, whose
        /// <see cref="LlamaIndexDocument.Text"/> contains Markdown for that page and whose
        /// <see cref="LlamaIndexDocument.ExtraInfo"/> holds page‑level metadata.
        /// </returns>
        public List<LlamaIndexDocument> LoadData(
            object filePath, // Can be Path or string
            Dictionary<string, object> extraInfo = null,
            Dictionary<string, object> loadKwargs = null)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            string filePathStr = filePath is string str ? str : filePath.ToString();
            if (!File.Exists(filePathStr))
                throw new FileNotFoundException($"File not found: {filePathStr}");

            if (extraInfo == null)
                extraInfo = new Dictionary<string, object>();

            if (loadKwargs == null)
                loadKwargs = new Dictionary<string, object>();

            // Extract text header information
            var hdrInfo = new Helpers.IdentifyHeaders(filePathStr);

            Document doc = new Document(filePathStr);
            List<LlamaIndexDocument> docs = new List<LlamaIndexDocument>();

            try
            {
                for (int i = 0; i < doc.PageCount; i++)
                {
                    docs.Add(ProcessDocPage(
                        doc, extraInfo, filePathStr, i, hdrInfo, loadKwargs));
                }
            }
            finally
            {
                doc.Close();
            }

            return docs;
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
                writeImages: loadKwargs.ContainsKey("write_images") && (bool)loadKwargs["write_images"],
                embedImages: loadKwargs.ContainsKey("embed_images") && (bool)loadKwargs["embed_images"],
                imagePath: loadKwargs.ContainsKey("image_path") ? (string)loadKwargs["image_path"] : "",
                imageFormat: loadKwargs.ContainsKey("image_format") ? (string)loadKwargs["image_format"] : "png",
                filename: filePath,
                forceText: loadKwargs.ContainsKey("force_text") ? (bool)loadKwargs["force_text"] : true,
                showProgress: loadKwargs.ContainsKey("show_progress") && (bool)loadKwargs["show_progress"]
            );

            return new LlamaIndexDocument
            {
                Text = text,
                ExtraInfo = extraInfo
            };
        }

        /// <summary>
        /// Process metas of a PDF document.
        /// </summary>
        private Dictionary<string, object> ProcessDocMeta(
            Document doc,
            string filePath,
            int pageNumber,
            Dictionary<string, object> extraInfo)
        {
            if (extraInfo == null)
                extraInfo = new Dictionary<string, object>();

            // Add document metadata
            var metadata = doc.MetaData;
            foreach (var kvp in metadata)
            {
                extraInfo[kvp.Key] = kvp.Value;
            }

            extraInfo["page"] = pageNumber + 1;
            extraInfo["total_pages"] = doc.PageCount;
            extraInfo["file_path"] = filePath;

            return extraInfo;
        }
    }

    /// <summary>
    /// Document structure for LlamaIndex compatibility
    /// </summary>
    public class LlamaIndexDocument
    {
        public string Text { get; set; }
        public Dictionary<string, object> ExtraInfo { get; set; }
    }
}
