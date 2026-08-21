using System;
using System.Runtime.InteropServices;
using MuPDF.NET.Office;
using MuPDF.NET.PDF4LLM;
using MuPDF.NET.PDF4LLM.Layout;
using MuPDF.NET.PDF4LLM.Llama;
using Xunit;

namespace MuPDF.NET.PDF4LLM.Test
{
    /// <summary>
    /// Integration coverage for the optional MuPDF.NET.Office document handler.
    /// Mirrors the PyMuPDFPro test_4496 direct pymupdf4llm path scenario.
    /// </summary>
    [Collection("MuPDF.NET.PDF4LLM")]
    public class TestOffice
    {
        private static readonly object UnlockLock = new object();
        private static bool _unlocked;

        [Theory]
        [InlineData("test_4496.hwpx")]
        [InlineData("test_4159.doc")]
        [InlineData("pages.docx")]
        [InlineData("pages.odt")]
        public void ToMarkdown_accepts_Office_paths(string fileName)
        {
            if (!TryUnlockOffice())
                return;

            bool priorLayout = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(false);
                string markdown = MuPDF4LLM.ToMarkdown(
                    _Path.Office(fileName),
                    showProgress: false);

                Assert.NotNull(markdown);
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(priorLayout);
            }
        }

        [Fact]
        public void Layout_pipeline_accepts_HWPX_path()
        {
            if (!TryUnlockOffice() || !PyMuPdfLayout.IsAvailable)
                return;

            string path = _Path.Office("test_4496.hwpx");
            bool priorLayout = MuPDF4LLM.UseLayout;
            try
            {
                MuPDF4LLM.SetUseLayout(true);

                string markdown = MuPDF4LLM.ToMarkdown(path, showProgress: false);
                string json = MuPDF4LLM.ToJson(path, useOcr: false);
                string text = MuPDF4LLM.ToText(path, useOcr: false);

                Assert.NotNull(markdown);
                Assert.False(string.IsNullOrWhiteSpace(json));
                Assert.NotNull(text);
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(priorLayout);
            }
        }

        [Fact]
        public void Llama_reader_accepts_DOCX_path()
        {
            if (!TryUnlockOffice())
                return;

            var reader = new PDFMarkdownReader();
            var documents = reader.LoadData(_Path.Office("pages.docx"));

            Assert.NotEmpty(documents);
            Assert.All(documents, document => Assert.NotNull(document.Text));
        }

        [Fact]
        public void GetKeyValues_is_safe_for_non_PDF_Office_document()
        {
            if (!TryUnlockOffice())
                return;

            var fields = MuPDF4LLM.GetKeyValues(_Path.Office("pages.odt"));

            Assert.Empty(fields);
        }

        private static bool TryUnlockOffice()
        {
            // NativeAssets currently ships the SmartOffice bridge for Windows x64.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                return false;
            }

            lock (UnlockLock)
            {
                if (!_unlocked)
                {
                    // No key is stored in MuPDF.NET.PDF4LLM.Test. Restricted mode is
                    // sufficient because all integration fixtures have <= 3 pages.
                    MuPDFOffice.Unlock(fontPathAuto: true);
                    _unlocked = true;
                }
            }

            return true;
        }
    }
}
