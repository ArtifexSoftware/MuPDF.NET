using System;
using System.IO;
using MuPDF.NET;
using MuPDF.NET.PDF4LLM;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    [Collection("MuPDF.NET.PDF4LLM")]
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

            bool priorLayout = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(true);

                using (var document = new Document(path))
                {
                    string actual = MuPDF4LLM.ToMarkdown(
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
                    if (!MuPDF4LLM.LayoutAvailable)
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
                MuPDF4LLM.SetUseLayout(priorLayout);
            }
        }
    }
}