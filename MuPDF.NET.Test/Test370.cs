using System;
using System.IO;
using MuPDF.NET;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET.layout")]
    public class Test370
    {
        private const string TestClassName = nameof(Test370);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_370()
        {
            string path = Doc("test_370.pdf");
            string pathExpected = Doc("test_370_expected.md");
            string pathActual = Out("test_370_actual.md");

            string expected = File.ReadAllText(pathExpected);

            bool priorLayout = Utils.LayoutEnabled;
            try
            {
                Utils.UseLayout(true);

                using (var document = new Document(path))
                {
                    string actual = Utils.ToMarkdown(
                        document,
                        writeImages: false,
                        embedImages: false,
                        imageFormat: "jpg",
                        header: false,
                        footer: false,
                        showProgress: true,
                        forceText: true,
                        pageSeparators: true);

                    File.WriteAllText(pathActual, actual);

                    // Full golden compare only when layout is off (stext fallback).
                    if (!Utils.LayoutAvailable)
                        Assert.Equal(expected.Replace("\r\n", "\n"), actual.Replace("\r\n", "\n"));
                    else
                    {
                        Assert.Contains("Synthesis of Silyl Dienol Ethers", actual);
                        Assert.Contains("Masahiro Sai", actual);
                    }
                }
            }
            finally
            {
                Utils.UseLayout(priorLayout);
            }
        }
    }
}