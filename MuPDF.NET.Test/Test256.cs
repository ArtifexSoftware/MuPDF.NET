using System;
using System.Diagnostics;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Regression for <see href="https://github.com/ArtifexSoftware/MuPDF.NET/issues/256"/>.
    /// Widget enumeration, <c>TextPage.Search</c>, and <c>GetKeyXref(AP/N)</c> must
    /// remain correct and must not grow private memory unboundedly across open/close loops.
    /// </summary>
    [Collection("MuPDF.NET native")]
    public class Test256
    {
        private const int MemoryIterations = 80;

        [Fact]
        public void test_256_widgets_and_getkey_xref()
        {
            byte[] data = BuildWidgetPdf();
            using var doc = new Document(stream: data);
            Page page = doc[0];
            var widgets = page.GetWidgets().ToList();
            int n = 0;
            string firstName = null;
            string firstKind = null;
            string tenthKind = null;
            try
            {
                foreach (Widget w in widgets)
                {
                    n++;
                    if (n == 1)
                    {
                        firstName = w.FieldName;
                        (firstKind, _) = doc.GetKeyXref(w.Xref, "AP/N");
                        for (int r = 0; r < 9; r++)
                            (tenthKind, _) = doc.GetKeyXref(w.Xref, "AP/N");
                    }
                }
            }
            finally
            {
                foreach (Widget w in widgets)
                    w.Dispose();
            }
            page.Dispose();

            Assert.Equal(8, n);
            Assert.Equal("text_0", firstName);
            Assert.False(string.IsNullOrEmpty(firstKind));
            Assert.Equal(firstKind, tenthKind);
        }

        [Fact]
        public void test_256_search()
        {
            byte[] data = BuildTextPdf();
            using var doc = new Document(stream: data);
            Page page = doc[0];
            using TextPage tp = page.GetTextPage();
            string text = tp.ExtractText() ?? "";
            var quads = TextPage.Search(tp, "the", hitMax: 1);
            page.Dispose();

            Assert.True(text.Length > 100, $"extractChars={text.Length}");
            Assert.True(quads.Count >= 1, $"searchHits={quads.Count}");
        }

        [Fact]
        public void test_256_widgets_memory()
        {
            AssertMemoryStable("widgets", BuildWidgetPdf());
        }

        [Fact]
        public void test_256_getkey10_memory()
        {
            AssertMemoryStable("getkey10", BuildWidgetPdf());
        }

        [Fact]
        public void test_256_search_memory()
        {
            AssertMemoryStable("search", BuildTextPdf());
        }

        private static void AssertMemoryStable(string mode, byte[] data)
        {
            Stabilize();
            var stats = new long[MemoryIterations];
            for (int i = 0; i < MemoryIterations; i++)
            {
                using (var doc = new Document(stream: data))
                {
                    Page page = doc[0];
                    if (mode == "search")
                        SearchPage(page);
                    else
                        WidgetsPage(doc, page, mode);
                    page.Dispose();
                    doc.Close();
                }
                Stabilize();
                stats[i] = PrivateBytes();
            }

            long baseline = stats[9];
            long last = stats[^1];
            float ratio = baseline == 0 ? 0 : (float)last / baseline;
            Console.WriteLine($"{nameof(Test256)} {mode}: baseline={baseline} last={last} ratio={ratio}");
            Assert.True(ratio < 1.25f, $"{mode} private-memory ratio={ratio} (baseline={baseline} last={last})");
        }

        private static void WidgetsPage(Document doc, Page page, string mode)
        {
            int repeats = mode == "getkey10" ? 10 : (mode == "getkey" ? 1 : 0);
            var widgets = page.GetWidgets().ToList();
            try
            {
                foreach (Widget w in widgets)
                {
                    if (repeats > 0)
                    {
                        for (int r = 0; r < repeats; r++)
                            _ = doc.GetKeyXref(w.Xref, "AP/N");
                    }
                    else
                    {
                        _ = w.FieldName;
                        _ = w.Rect;
                    }
                }
            }
            finally
            {
                foreach (Widget w in widgets)
                    w.Dispose();
            }
        }

        private static void SearchPage(Page page)
        {
            using TextPage tp = page.GetTextPage();
            _ = tp.ExtractText();
            for (int k = 0; k < 20; k++)
            {
                var quads = TextPage.Search(tp, "the", hitMax: 1);
                if (quads.Count > 0)
                    _ = quads[0].Rect;
            }
        }

        private static void Stabilize()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static long PrivateBytes()
        {
            using var process = Process.GetCurrentProcess();
            process.Refresh();
            return process.PrivateMemorySize64;
        }

        private static byte[] BuildWidgetPdf()
        {
            using var doc = new Document();
            Page page = doc.NewPage();
            for (int i = 0; i < 8; i++)
            {
                var w = new Widget(page)
                {
                    FieldName = "text_" + i,
                    FieldType = (int)WidgetType.Text,
                    Rect = new Rect(50, 50 + i * 28, 400, 72 + i * 28),
                    FieldValue = "value " + i,
                };
                page.AddWidget(w);
            }
            return doc.Write();
        }

        private static byte[] BuildTextPdf()
        {
            using var doc = new Document();
            Page page = doc.NewPage();
            var html = new StringBuilder();
            html.Append("<html><body>");
            for (int i = 0; i < 40; i++)
                html.Append("<p>the quick brown fox jumps over the lazy dog ").Append(i).Append("</p>");
            html.Append("</body></html>");
            page.InsertHtmlbox(page.Rect, html.ToString());
            return doc.Write();
        }
    }
}
