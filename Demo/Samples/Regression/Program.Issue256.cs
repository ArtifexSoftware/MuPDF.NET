using System.Diagnostics;

namespace Demo
{
    internal partial class Program
    {
        /// <summary>
        /// Issue #256 — widget enumeration, TextPage.Search, GetKeyXref(AP/N),
        /// and Page.MediaBox (Search then MediaBox per widget).
        /// </summary>
        internal static void TestIssue256(string[] args)
        {
            Console.WriteLine("\n=== issue-256: widgets / Search / GetKeyXref / MediaBox memory ===");
            Console.WriteLine("https://github.com/ArtifexSoftware/MuPDF.NET/issues/256");

            string[] rest = args ?? Array.Empty<string>();
            if (rest.Length > 0 && string.Equals(rest[0], "issue-256", StringComparison.OrdinalIgnoreCase))
                rest = rest.Skip(1).ToArray();

            int iterations = 80;
            string mode = "all";
            string needle = "the";
            if (rest.Length > 0 && int.TryParse(rest[0], out int n))
                iterations = n;
            if (rest.Length > 1)
                mode = rest[1];
            if (rest.Length > 2)
                needle = rest[2];

            if (string.Equals(mode, "all", StringComparison.OrdinalIgnoreCase))
            {
                RunIssue256Mode("widgets", iterations, needle);
                RunIssue256Mode("search", iterations, needle);
                RunIssue256Mode("getkey10", iterations, needle);
                RunIssue256Mode("mediabox", iterations, needle);
                RunIssue256Mode("mediabox-once", iterations, needle);
                RunIssue256Mode("mediabox-widget", iterations, needle);
                return;
            }

            RunIssue256Mode(mode, iterations, needle);
        }

        private static bool Issue256IsMediaBoxMode(string mode) =>
            mode == "mediabox" || mode == "mediabox-once" || mode == "mediabox-widget";

        private static void RunIssue256Mode(string mode, int iterations, string needle)
        {
            byte[] data = mode == "search"
                ? Issue256BuildTextPdf()
                : Issue256IsMediaBoxMode(mode)
                    ? Issue256BuildFormAndTextPdf()
                    : Issue256BuildWidgetPdf();
            Console.WriteLine($"--- {mode}  iterations={iterations}  pdfBytes={data.Length} ---");

            using (var probe = new Document(stream: data))
            {
                Page page = probe[0];
                if (mode == "search")
                    Issue256SearchOnce(page, needle);
                else if (Issue256IsMediaBoxMode(mode))
                    Issue256MediaBoxOnce(page, needle, mode);
                else
                    Issue256WidgetsOnce(probe, page, mode);
                page.Dispose();
            }

            Issue256Stabilize();
            long startPrivate = Process.GetCurrentProcess().PrivateMemorySize64;

            for (int i = 1; i <= iterations; i++)
            {
                if (Issue256IsMediaBoxMode(mode))
                {
                    Issue256MediaBoxRepro(data, needle, mode);
                }
                else
                {
                    using var doc = new Document(stream: data);
                    Page page = doc[0];
                    if (mode == "search")
                        Issue256SearchPage(page, needle);
                    else
                        Issue256WidgetsPage(doc, page, mode);
                    page.Dispose();
                    doc.Close();
                }
            }

            Issue256Stabilize();
            long endPrivate = Process.GetCurrentProcess().PrivateMemorySize64;
            Console.WriteLine(
                $"  deltaPrivateKB={(endPrivate - startPrivate) / 1024}  (widgets/Search/GetKeyXref/MediaBox after dispose)");
        }

        private static void Issue256WidgetsOnce(Document doc, Page page, string mode)
        {
            int n = 0;
            int repeats = mode == "getkey10" ? 10 : (mode == "getkey" ? 1 : 0);
            List<Widget> widgets = page.GetWidgets().ToList();
            try
            {
                foreach (Widget w in widgets)
                {
                    n++;
                    (string kind, string raw) = (null, null);
                    for (int r = 0; r < Math.Max(repeats, 1); r++)
                    {
                        if (repeats > 0)
                            (kind, raw) = doc.GetKeyXref(w.Xref, "AP/N");
                        else
                        {
                            _ = w.FieldName;
                            _ = w.Rect;
                        }
                    }
                    if (n == 1)
                    {
                        if (repeats > 0)
                            Console.WriteLine($"  first xref={w.Xref}  AP/N kind={kind}  repeats={repeats}");
                        else
                            Console.WriteLine($"  first FieldName={w.FieldName}  Rect={w.Rect}");
                    }
                }
            }
            finally
            {
                foreach (Widget w in widgets)
                    w.Dispose();
            }
            Console.WriteLine($"  widgetsOnPage0={n}");
        }

        private static void Issue256WidgetsPage(Document doc, Page page, string mode)
        {
            int repeats = mode == "getkey10" ? 10 : (mode == "getkey" ? 1 : 0);
            List<Widget> widgets = page.GetWidgets().ToList();
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

        private static void Issue256SearchOnce(Page page, string needle)
        {
            using TextPage tp = page.GetTextPage();
            string text = tp.ExtractText() ?? "";
            var quads = TextPage.Search(tp, needle, hitMax: 1);
            Console.WriteLine($"  extractChars={text.Length}  searchHits={quads.Count}");
        }

        private static void Issue256SearchPage(Page page, string needle)
        {
            using TextPage tp = page.GetTextPage();
            _ = tp.ExtractText();
            for (int k = 0; k < 20; k++)
            {
                var quads = TextPage.Search(tp, needle, hitMax: 1);
                if (quads.Count > 0)
                    _ = quads[0].Rect;
            }
        }

        private static void Issue256MediaBoxOnce(Page page, string needle, string mode)
        {
            using TextPage tp = page.GetTextPage();
            var hits = TextPage.Search(tp, needle, hitMax: 1);
            Console.WriteLine($"  searchHits={hits.Count}  MediaBox={page.MediaBox}");
            var widgets = page.GetWidgets().ToList();
            try
            {
                Console.WriteLine($"  widgetsOnPage0={widgets.Count}  mode={mode}");
                if (widgets.Count > 0)
                    Console.WriteLine($"  first widget Rect={widgets[0].Rect}  intersects={page.MediaBox.Intersects(widgets[0].Rect)}");
            }
            finally
            {
                foreach (Widget w in widgets)
                    w.Dispose();
            }
        }

        /// <summary>
        /// https://github.com/ArtifexSoftware/MuPDF.NET/issues/256#issuecomment-5728502983
        /// </summary>
        private static void Issue256MediaBoxRepro(byte[] data, string needle, string mode)
        {
            bool searchFirst = mode != "mediabox";
            bool mediaBoxPerWidget = mode != "mediabox-once";

            if (searchFirst)
            {
                using (var doc = new Document(stream: data))
                {
                    foreach (Page page in doc)
                    {
                        using TextPage tp = page.GetTextPage();
                        var hits = TextPage.Search(tp, needle, hitMax: 1);
                        if (hits.Count > 0)
                            _ = page.Rect.Intersects(hits[0].Rect.Transform(page.RotationMatrix));
                        page.Dispose();
                    }
                    doc.Close();
                }
            }

            using (var doc = new Document(stream: data))
            {
                foreach (Page page in doc)
                {
                    if (mediaBoxPerWidget)
                    {
                        foreach (Widget w in page.GetWidgets())
                            _ = page.MediaBox.Intersects(w.Rect);
                    }
                    else
                    {
                        Rect mb = page.MediaBox;
                        foreach (Widget w in page.GetWidgets())
                            _ = mb.Intersects(w.Rect);
                    }
                    page.Dispose();
                }
                doc.Close();
            }
        }

        private static void Issue256Stabilize()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static byte[] Issue256BuildWidgetPdf()
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

        private static byte[] Issue256BuildTextPdf()
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

        private static byte[] Issue256BuildFormAndTextPdf()
        {
            using var doc = new Document();
            Page page = doc.NewPage();
            var html = new StringBuilder();
            html.Append("<html><body>");
            for (int i = 0; i < 40; i++)
                html.Append("<p>the quick brown fox jumps over the lazy dog ").Append(i).Append("</p>");
            html.Append("</body></html>");
            page.InsertHtmlbox(page.Rect, html.ToString());
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
    }
}
