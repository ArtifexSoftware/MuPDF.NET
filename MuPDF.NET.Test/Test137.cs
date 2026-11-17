using System;
using System.IO;
using System.Text.RegularExpressions;
using MuPDF.NET;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET.layout")]
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

            bool prior = Utils.LayoutEnabled;
            try
            {
                // layout package.use_layout(False)
                Utils.UseLayout(false);
                using (var document = new Document(path))
                {
                    string md = Utils.ToMarkdown(document, embedImages: true);
                    //     path_md = f'{path}.out_nolayout.md'
                    //         f.write(md)
                    File.WriteAllText(Out("test_137.out_nolayout.md"), md);
                }

                // layout package.use_layout(True)
                Utils.UseLayout(true);
                using (var document = new Document(path))
                {
                    string md = Utils.ToMarkdown(document, embedImages: true);
                    //     path_md = f'{path}.out_layout.md'
                    //         f.write(md)
                    File.WriteAllText(Out("test_137.out_layout.md"), md);
                }
            }
            finally
            {
                Utils.UseLayout(prior);
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

            bool prior = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(false);
                using (var document = new Document(path))
                {
                    string md = Utils.ToMarkdown(document, embedImages: true);
                    File.WriteAllText(pathMdActual, md);
                    string mdExpected = File.ReadAllText(pathMdExpected);
                    Assert.Equal(NormalizeMarkdown(mdExpected), NormalizeMarkdown(md));
                }
            }
            finally
            {
                Utils.UseLayout(prior);
            }
        }

        private static string NormalizeMarkdown(string md) =>
            Regex.Replace(md.Replace("\r\n", "\n").TrimEnd(), "\n{3,}", "\n\n");
    }
}