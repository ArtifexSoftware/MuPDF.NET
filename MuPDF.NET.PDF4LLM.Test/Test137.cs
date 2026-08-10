using System;
using System.IO;
using System.Text.RegularExpressions;
using MuPDF.NET;
using MuPDF.NET.PDF4LLM;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
    public class Test137
    {
        private const string TestClassName = nameof(Test137);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_137()
        {
            //         return
            var mupdfVersion = Constants.MupdfVersion;
            if (mupdfVersion.Major < 1 || (mupdfVersion.Major == 1 && mupdfVersion.Minor < 28))
                return;

            string path = Doc("test_137.pdf");

            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                // layout package.use_layout(False)
                MuPDF4LLM.SetUseLayout(false);
                using (var document = new Document(path))
                {
                    string md = MuPDF4LLM.ToMarkdown(document, embedImages: true);
                    //     path_md = f'{path}.out_nolayout.md'
                    //         f.write(md)
                    File.WriteAllText(Out("test_137.out_nolayout.md"), md);
                }

                // layout package.use_layout(True)
                MuPDF4LLM.SetUseLayout(true);
                using (var document = new Document(path))
                {
                    string md = MuPDF4LLM.ToMarkdown(document, embedImages: true);
                    //     path_md = f'{path}.out_layout.md'
                    //         f.write(md)
                    File.WriteAllText(Out("test_137.out_layout.md"), md);
                }
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(prior);
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

            bool prior = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(false);
                using (var document = new Document(path))
                {
                    string md = MuPDF4LLM.ToMarkdown(document, embedImages: true);
                    File.WriteAllText(pathMdActual, md);
                    string mdExpected = File.ReadAllText(pathMdExpected);
                    Assert.Equal(NormalizeMarkdown(mdExpected), NormalizeMarkdown(md));
                }
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(prior);
            }
        }

        private static string NormalizeMarkdown(string md) =>
            Regex.Replace(md.Replace("\r\n", "\n").TrimEnd(), "\n{3,}", "\n\n");
    }
}