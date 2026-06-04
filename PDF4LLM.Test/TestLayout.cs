using PDF4LLM;

namespace PDF4LLM.Test
{
    /// <summary>Port of pymupdf4llm/tests/pymupdf4llm/llama_index/test_layout.py</summary>
    [TestFixture]
    public class TestLayout
    {
        [Test]
        public void test_layout_switch()
        {
            bool prior = PdfExtractor.UseLayout;
            try
            {
                PdfExtractor.SetUseLayout(true);
                Assert.That(PdfExtractor.UseLayout, Is.True);

                PdfExtractor.SetUseLayout(false);
                Assert.That(PdfExtractor.UseLayout, Is.False);

                PdfExtractor.SetUseLayout(true);
                Assert.That(PdfExtractor.UseLayout, Is.True);
            }
            finally
            {
                PdfExtractor.SetUseLayout(prior);
            }
        }

        [Test]
        public void test_layout_default()
        {
            PdfExtractor.SetUseLayout(false);
            Assert.That(PdfExtractor.UseLayout, Is.False);
            Assert.That(PdfExtractor.LayoutAvailable, Is.False);
        }
    }
}
