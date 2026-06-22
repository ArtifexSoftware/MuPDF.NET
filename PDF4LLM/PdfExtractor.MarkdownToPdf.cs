using System;
using System.IO;
using Markdig;
using MuPDF.NET;

namespace PDF4LLM
{
    public static partial class PdfExtractor
    {
        /// <summary>
        /// Return a PDF document for a Markdown source string.
        /// The Markdown is converted to HTML, then rendered with MuPDF Story.
        /// </summary>
        /// <param name="mdPath">Path to the Markdown source file.</param>
        /// <param name="userCss">Optional CSS stylesheet; uses a built-in default when <see langword="null"/>.</param>
        /// <param name="pageRect">Media box for each page; uses A4 when empty.</param>
        /// <param name="margins">Four margin values <c>[left, top, right, bottom]</c> in points.</param>
        /// <param name="archive">Optional resource archive for embedded assets; defaults to the Markdown file directory.</param>
        /// <param name="outputPath">When set, save the PDF to this path and return <see langword="null"/>.</param>
        public static Document MarkdownToPdf(
            string mdPath,
            string userCss = null,
            Rect pageRect = default,
            float[] margins = null,
            Archive archive = null,
            string outputPath = null)
        {
            Rect mediabox = pageRect == null || pageRect.IsEmpty
                ? MuPDF.NET.Utils.PaperRect("A4")
                : pageRect;
            float[] borders;
            try
            {
                borders = margins;
                if (borders != null && borders.Length != 4)
                    throw new ArgumentException("margins must have 4 elements");
            }
            catch (Exception)
            {
                if (margins != null)
                    Console.WriteLine($"Warning: Invalid margins specified: {string.Join(", ", margins)}.");
                borders = new[] { 50f, 50f, 50f, 50f };
            }

            if (borders == null)
                borders = new[] { 50f, 50f, 50f, 50f };

            // available area for writing content.
            Rect where = new Rect(
                mediabox.X0 + borders[0],
                mediabox.Y0 + borders[1],
                mediabox.X1 - borders[2],
                mediabox.Y1 - borders[3]);

            const string userCssDefault = @"
    /* --- basic layout --- */
    body {
        font-family: sans-serif;
        line-height: 1.3;
        font-size: 12pt;
        color: #000;
        margin: 0 auto;
    }

    /* --- Headers --- */
    h1, h2, h3, h4, h5, h6 {
        font-weight: bold;
        margin-top: 0.6em;
        margin-bottom: 0;
    }

    h1 { font-size: 17pt; }
    h2 { font-size: 15pt; }
    h3 { font-size: 13pt; }

    /* --- Paragraphs --- */
    p {
        margin-top: 0.6em;
        margin-bottom: 0.6em;
    }

    /* --- Lists --- */
    ul, ol {
        margin-top: 0.6em;
        margin-bottom: 0.6em;
    }

    /* --- block quotes --- */
    blockquote {
        border-left: 4px solid #ccc;
        padding-left: 12px;
        color: #555;
        margin: 1em 0;
    }

    /* --- Tables --- */
    table {
        border-collapse: collapse;
        width: auto;
        margin-top: 2em;
        margin-bottom: 2em;
        font-size: 10pt;
    }

    table th, table td {
        border: 1px solid #aaa;
        padding: 3px 3px;
    }

    table th {
        font-weight: bold;
    }

    /* --- Images --- */
    img {
        max-width: 100%;
        height: auto;
        display: block;
        margin: 1em auto;
    }

    /* --- Code blocks --- */
    pre code {
        font-family: monospace;
        font-size: 9pt;
        color: #00f;
    }
    code {
        font-family: monospace;
        font-size: 12pt;
    }

    pre {
        /* light blue transparent background for code blocks */
        background: #EFF8;
        padding: 3px;
        overflow-x: visible;/* do not cut text overflowing to the right */
    }

    /* --- Page Breaks (PDF) --- */
    table, pre {
        page-break-inside: avoid;
    }
    ";

            if (string.IsNullOrWhiteSpace(mdPath))
                throw new ArgumentException("md_path must be a file path.", nameof(mdPath));

            string fullPath = Path.GetFullPath(mdPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Markdown file not found: {fullPath}");

            string mdText = System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(fullPath));
            string parentDir = Path.GetDirectoryName(fullPath) ?? ".";

            Archive arch = archive;
            if (arch == null)
                arch = new Archive(parentDir);
            else
                arch.Add(parentDir);

            mdText = mdText
                .Replace("\r\n", "\n")
                .Replace("image/jpg;", "image/jpeg;")
                .Replace("**==== Text in picture: ====**", "**==== Text in picture: ====**\n");

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
            string htmlText = Markdown.ToHtml(mdText, pipeline);

            string cssText = userCss ?? userCssDefault;

            using (var story = new Story(htmlText, userCss: cssText, archive: arch))
            {
                Story.StoryRectFn rectfn = (rectNum, filled) => (mediabox, where, null);
                Document doc = story.WriteWithLinks(rectfn);

                if (!string.IsNullOrEmpty(outputPath))
                {
                    doc.SubsetFonts();
                    doc.EzSave(outputPath);
                    doc.Close();
                    return null;
                }

                return doc;
            }
        }
    }

}
