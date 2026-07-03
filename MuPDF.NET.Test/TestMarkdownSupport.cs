using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestMarkdownSupport/</c>; outputs: <c>TestDocuments/_Output/TestMarkdownSupport/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestMarkdownSupport
    {
        private const string TestClassName = nameof(TestMarkdownSupport);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static string Dedent(string text)
        {
            // textwrap.dedent
            var lines = text.Replace("\r\n", "\n").Trim('\n').Split('\n');
            if (lines.Length == 0)
                return "";
            int minIndent = int.MaxValue;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                int indent = line.Length - line.TrimStart(' ').Length;
                if (indent < minIndent)
                    minIndent = indent;
            }
            if (minIndent == int.MaxValue)
                minIndent = 0;
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                    sb.Append('\n');
                var line = lines[i];
                if (line.Length >= minIndent)
                    sb.Append(line.Substring(minIndent));
                else
                    sb.Append(line);
            }
            return sb.ToString();
        }

        [Fact]
        public void test_archive_markdown()
        {
            // Test Archive support.
            if (!_Version.mupdf_version_tuple_at_least(1, 28, 0))
            {
                Console.WriteLine("no testing on MuPDF < 1.28.0");
                return;
            }

            string path = Path.GetFullPath(Path.GetDirectoryName(Doc("nur-ruhig.jpg"))!);
            byte[] md = Encoding.UTF8.GetBytes("![](nur-ruhig.jpg)\n\n**A referenced image.**");
            using var md_doc = new Document(md, "md", archive: new Archive(path));
            // pdfdata = md_doc.convert_to_pdf()
            byte[] pdfdata = md_doc.ConvertToPdf();
            using var doc = new Document(pdfdata, "pdf");
            // page = doc[0]
            var page = doc[0];
            // images = page.get_image_info()
            var images = page.GetImageInfo();
            Assert.Single(images);
        }

        [Fact]
        public void test_archive_links()
        {
            // Create an internal and an external link and confirm
            // that they are correctly converted to PDF links.
            if (!_Version.mupdf_version_tuple_at_least(1, 28, 0))
            {
                Console.WriteLine("no testing on MuPDF < 1.28.0");
                return;
            }
            string md = @"Some text containing an external [link](http://www.google.com) to Google.
    Now an internal link to a header in this document: [Some Header](#some-header). The header is here:

    <h2 id=""some-header"">Some Header</h2>

    Some text following the header.
    ";
            using var md_doc = new Document(Encoding.UTF8.GetBytes(md), "md");
            // pdfdata = md_doc.convert_to_pdf()
            byte[] pdfdata = md_doc.ConvertToPdf();
            using var doc = new Document(pdfdata, "pdf");
            // page = doc[0]
            var page = doc[0];
            // links=page.get_links()
            var links = page.GetLinks();
            Assert.Equal(2, links.Count);
            Assert.Equal("http://www.google.com", links[0].Uri);
            Assert.Equal(Constants.LinkUri, (int)links[0].Kind);
            Assert.Equal(Constants.LinkGoto, (int)links[1].Kind);
        }

        [Fact]
        public void test_markdown_style()
        {
            Console.WriteLine();
            if (!_Version.mupdf_version_tuple_at_least(1, 28, 0))
            {
                Console.WriteLine("test_markdown_style(): not running because mupdf<1.28.");
                return;
            }

            var font = new Font("tiro");
            using var arch = new Archive(font.Buffer, "tiro");

            // css = """@font-face {font-family: sans-serif; src: url(tiro);}"""
            string css = "@font-face {font-family: sans-serif; src: url(tiro);}";
            string md = "Overriding sans-serif with Times-Roman.";
            foreach (int use_css in new[] { 0, 1 })
            {
                using var md_doc = new Document(Encoding.UTF8.GetBytes(md), "md", archive: arch);
                if (use_css != 0)
                    // md_doc.apply_css(css)  # apply the CSS to the document
                    md_doc.ApplyCss(css);

                // md_pdf_stream = md_doc.convert_to_pdf()
                byte[] md_pdf_stream = md_doc.ConvertToPdf();
                using (var pdf_doc = new Document(md_pdf_stream, "pdf"))
                {
                    // page = pdf_doc[0]
                    var page = pdf_doc[0];
                    // spans = [
                    //     s for b in page.get_text("dict")["blocks"] for l in b["lines"] for s in l["spans"]
                    // ]
                    var dict = (Dictionary<string, object>)page.GetText("dict");
                    var spans = new List<Dictionary<string, object>>();
                    foreach (var b in (List<Dictionary<string, object>>)dict["blocks"])
                    {
                        foreach (var l in (List<Dictionary<string, object>>)b["lines"])
                        {
                            foreach (var s in (List<Dictionary<string, object>>)l["spans"])
                                spans.Add(s);
                        }
                    }

                    Assert.Single(spans);
                    Console.WriteLine($"test_markdown_style(): use_css={use_css} font={spans[0]["font"]}.");
                    if (use_css != 0)
                        Assert.Contains("Roman", spans[0]["font"]?.ToString());
                    else
                        Assert.DoesNotContain("Roman", spans[0]["font"]?.ToString());
                }
            }
        }

        [Fact]
        public void test_markdown_save()
        {
            string md = Dedent(@"
            # title
            
            ## section
            
            text
            ");
            using (var document_md = new Document(Encoding.UTF8.GetBytes(md), "md"))
            {
                // out_pdf = os.path.normpath(f'{__file__}/../../tests/test_markdown_save.pdf')
                string out_pdf = Out("test_markdown_save.pdf");
                // document_md.save(out_pdf)
                document_md.Save(out_pdf);
            }
        }
    }
}
