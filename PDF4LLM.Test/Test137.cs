using System;
using System.IO;
using System.Text.RegularExpressions;
using MuPDF.NET;
using PDF4LLM;
using Xunit;

namespace PDF4LLM.Test
{
    /// <summary>Port of <c>tests/test_137.py</c>.</summary>
    [Collection("PDF4LLM")]
    public class Test137
    {
        private const string TestClassName = nameof(Test137);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_137()
        {
            // def test_137():
            //     # This doesn't actually detect any exception, but does show the different
            //     # output with/without layout.
            //     if pymupdf.mupdf_version_tuple < (1, 28):
            //         print(f'test_137(): not running because {pymupdf.mupdf_version=} < 1.28.')
            //         return
            var mupdfVersion = Constants.MupdfVersion;
            if (mupdfVersion.Major < 1 || (mupdfVersion.Major == 1 && mupdfVersion.Minor < 28))
                return;

            //     path = os.path.normpath(f'{__file__}/../../tests/test_137.pdf')
            string path = Doc("test_137.pdf");

            bool prior = PdfExtractor.UseLayout;
            try
            {
                //     pymupdf4llm.use_layout(False)
                PdfExtractor.SetUseLayout(false);
                //     with pymupdf.open(path) as document:
                //         md = pymupdf4llm.to_markdown(document, embed_images=True)
                using (var document = new Document(path))
                {
                    string md = PdfExtractor.ToMarkdown(document, embedImages: true);
                    //     path_md = f'{path}.out_nolayout.md'
                    //     with open(path_md, 'w') as f:
                    //         f.write(md)
                    File.WriteAllText(Out("test_137.out_nolayout.md"), md);
                }

                //     pymupdf4llm.use_layout(True)
                PdfExtractor.SetUseLayout(true);
                //     with pymupdf.open(path) as document:
                //         md = pymupdf4llm.to_markdown(document, embed_images=True)
                using (var document = new Document(path))
                {
                    string md = PdfExtractor.ToMarkdown(document, embedImages: true);
                    //     path_md = f'{path}.out_layout.md'
                    //     with open(path_md, 'w', encoding='utf8') as f:
                    //         f.write(md)
                    File.WriteAllText(Out("test_137.out_layout.md"), md);
                }
            }
            finally
            {
                PdfExtractor.SetUseLayout(prior);
            }
        }

        [Fact]
        public void test_to_markdown_link_malicious()
        {
            //     '''
            //     Check that when running without layout, we don't propagate bogus links into
            //     markdown. See: https://bugs.ghostscript.com/show_bug.cgi?id=709173
            //     '''

            string path = Doc("test_to_markdown_link_malicious.pdf");
            string pathMdExpected = Doc("test_to_markdown_link_malicious.pdf.expected.md");
            string pathMdActual = Out("test_to_markdown_link_malicious.pdf.md");

            //     # Disable use of layout so we attempt to handle links.
            bool prior = PdfExtractor.UseLayout;
            try
            {
                PdfExtractor.SetUseLayout(false);
                using (var document = new Document(path))
                {
                    string md = PdfExtractor.ToMarkdown(document, embedImages: true);
                    File.WriteAllText(pathMdActual, md);
                    string mdExpected = File.ReadAllText(pathMdExpected);
                    Assert.Equal(NormalizeMarkdown(mdExpected), NormalizeMarkdown(md));
                }
            }
            finally
            {
                PdfExtractor.SetUseLayout(prior);
            }
        }

        private static string NormalizeMarkdown(string md) =>
            Regex.Replace(md.Replace("\r\n", "\n").TrimEnd(), "\n{3,}", "\n\n");
    }
}
