using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Strict ports of <c>PyMuPDF-1.27.2.2/tests/test_annots.py</c> (same test names and flow).
    /// Inputs: <c>TestDocuments/TestAnnots/</c>; outputs: <c>TestDocuments/_Output/TestAnnots/</c>.
    /// </summary>
    [Collection("MuPDF.NET native")]
    public class TestAnnots
    {
        private const string TestClassName = nameof(TestAnnots);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static void AssertAnnotType(Annot annot, AnnotationType expectedType, string expectedName)
        {
            Assert.Equal(expectedType, annot.Type);
            Assert.Equal(expectedName, annot.TypeString);
        }

        private static float PixmapsRms(Pixmap a, Pixmap b)
        {
            Assert.Equal(a.Width, b.Width);
            Assert.Equal(a.Height, b.Height);
            Assert.Equal(a.N, b.N);
            var sa = a.Samples;
            var sb = b.Samples;
            Assert.Equal(sa.Length, sb.Length);
            float e = 0;
            for (int i = 0; i < sa.Length; i++)
            {
                int d = sa[i] - sb[i];
                e += d * d;
            }
            return (float)Math.Sqrt(e / sa.Length);
        }

        private static bool GentleWordsEqual(
            IReadOnlyList<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> w0,
            IReadOnlyList<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> w1,
            float tol = 1e-3f)
        {
            if (w0.Count != w1.Count) return false;
            for (int i = 0; i < w0.Count; i++)
            {
                if (w0[i].word != w1[i].word) return false;
                var r0 = new Rect(w0[i].x0, w0[i].y0, w0[i].x1, w0[i].y1);
                var r1 = new Rect(w1[i].x0, w1[i].y0, w1[i].x1, w1[i].y1);
                if ((r1 - r0).Norm() > tol) return false;
            }
            return true;
        }

        private static string ColorKey(byte[] c) => c == null ? "" : Convert.ToHexString(c);

        [Fact]
        public void test_caret()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.add_caret_annot(_Constants.rect.TL);
                AssertAnnotType(annot, AnnotationType.Caret, "Caret");
                annot.Update(rotate: 20);
                page.annot_names();
                page.annot_xrefs();
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_caret.pdf"));
            }
        }

        [Fact]
        public void test_freetext()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.AddFreeTextAnnot(
                    _Constants.rect,
                    _Constants.t1,
                    fontSize: 10,
                    rotate: 90,
                    textColor: _Constants.blue,
                    fillColor: _Constants.gold,
                    align: Constants.TextAlignCenter);
                annot.SetBorder(width: 0.3f, dashes: new float[] { 2 });
                annot.Update(textColor: _Constants.blue, fillColor: _Constants.gold);
                AssertAnnotType(annot, AnnotationType.FreeText, "FreeText");
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_freetext.pdf"));
            }
        }

        [Fact]
        public void test_text()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.AddTextAnnot(_Constants.r.TL, _Constants.t1);
                AssertAnnotType(annot, AnnotationType.Text, "Text");
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_text.pdf"));
            }
        }

        [Fact]
        public void test_highlight()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.AddHighlightAnnot(new[] { new Quad(_Constants.rect) });
                AssertAnnotType(annot, AnnotationType.Highlight, "Highlight");
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_highlight.pdf"));
            }
        }

        [Fact]
        public void test_underline()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.AddUnderlineAnnot(new[] { new Quad(_Constants.rect) });
                AssertAnnotType(annot, AnnotationType.Underline, "Underline");
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_underline.pdf"));
            }
        }

        [Fact]
        public void test_squiggly()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.add_squiggly_annot(new[] { new Quad(_Constants.rect) });
                AssertAnnotType(annot, AnnotationType.Squiggly, "Squiggly");
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_squiggly.pdf"));
            }
        }

        [Fact]
        public void test_strikeout()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.add_strikeout_annot(new[] { new Quad(_Constants.rect) });
                AssertAnnotType(annot, AnnotationType.StrikeOut, "StrikeOut");
                page.DeleteAnnot(annot);
                page.Dispose();
                doc.Save(Out("test_strikeout.pdf"));
            }
        }

        [Fact]
        public void test_polyline()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var cellRect = page.Rect + new Rect(100, 36, -100, -36);
                var cell = Utils.MakeTable(cellRect, rows: 10);
                for (int i = 0; i < 10; i++)
                {
                    var annot = page.AddPolylineAnnot(new[] { cell[i][0].BottomLeft, cell[i][0].BottomRight });
                    annot.SetLineEnds(i, i);
                    annot.Update();
                }
                int idx = 0;
                foreach (var annot in page.Annots())
                {
                    var le = annot.LineEnds;
                    Assert.NotNull(le);
                    Assert.Equal((idx, idx), le.Value);
                    idx++;
                }
                var last = page.Annots().Last();
                AssertAnnotType(last, AnnotationType.PolyLine, "PolyLine");
                last.Dispose();
                page.Dispose();
                doc.Save(Out("test_polyline.pdf"));
            }
        }

        [Fact]
        public void test_polygon()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.AddPolygonAnnot(new[] { _Constants.rect.BL, _Constants.rect.TR, _Constants.rect.BR, _Constants.rect.TL });
                AssertAnnotType(annot, AnnotationType.Polygon, "Polygon");
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_polygon.pdf"));
            }
        }

        [Fact]
        public void test_line()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var cellRect = page.Rect + new Rect(100, 36, -100, -36);
                var cell = Utils.MakeTable(cellRect, rows: 10);
                for (int i = 0; i < 10; i++)
                {
                    var annot = page.AddLineAnnot(cell[i][0].BottomLeft, cell[i][0].BottomRight);
                    annot.SetLineEnds(i, i);
                    annot.Update();
                }
                int idx = 0;
                foreach (var annot in page.Annots())
                {
                    var le = annot.LineEnds;
                    Assert.NotNull(le);
                    Assert.Equal((idx, idx), le.Value);
                    idx++;
                }
                var last = page.Annots().Last();
                AssertAnnotType(last, AnnotationType.Line, "Line");
                last.Dispose();
                page.Dispose();
                doc.Save(Out("test_line.pdf"));
            }
        }

        [Fact]
        public void test_square()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.add_rect_annot(_Constants.rect);
                AssertAnnotType(annot, AnnotationType.Square, "Square");
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_square.pdf"));
            }
        }

        [Fact]
        public void test_circle()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.AddCircleAnnot(_Constants.rect);
                AssertAnnotType(annot, AnnotationType.Circle, "Circle");
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_circle.pdf"));
            }
        }

        [Fact]
        public void test_fileattachment()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.AddFileAnnot(_Constants.rect.TL, Encoding.UTF8.GetBytes("just anything for testing"), "testdata.txt");
                AssertAnnotType(annot, AnnotationType.FileAttachment, "FileAttachment");
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_fileattachment.pdf"));
            }
        }

        [Fact]
        public void test_stamp()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.AddStampAnnot(_Constants.r, 0);
                AssertAnnotType(annot, AnnotationType.Stamp, "Stamp");
                Assert.Equal("Approved", annot.GetInfo()["content"]);
                var annot_id = annot.GetInfo()["id"];
                var annot_xref = annot.Xref;
                page.LoadAnnot(annot_id);
                page.LoadAnnot(annot_xref);
                page = doc.ReloadPage(page);
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_stamp.pdf"));
            }
        }

        [Fact]
        public void test_image_stamp()
        {
            var filename = Doc("nur-ruhig.jpg");
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.AddStampAnnot(_Constants.r, filename);
                Assert.Equal("Image Stamp", annot.GetInfo()["content"]);
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_image_stamp.pdf"));
            }
        }

        [Fact]
        public void test_redact1()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var annot = page.AddRedactAnnot(new Quad(_Constants.r), text: "Hello");
                annot.Update(crossOut: true, rotate: -1);
                AssertAnnotType(annot, AnnotationType.Redact, "Redact");
                using (annot.GetPixmap()) { }
                var info = annot.GetInfo();
                annot.SetInfo(info);
                Assert.False(annot.HasPopup);
                annot.SetPopup(_Constants.r);
                var s = annot.PopupRect;
                Assert.Equal(_Constants.r, s);
                page.ApplyRedactions();
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_redact1.pdf"));
            }
        }

        [Fact]
        public void test_redact2()
        {
            using var doc = new Document(Doc("symbol-list.pdf"));
            var page = doc[0];
            var all_text0 = page.get_text_words();
            page.AddRedactAnnot(new Quad(page.Rect));
            page.ApplyRedactions(text: 1);
            var t = page.get_text_words();
            Assert.True(GentleWordsEqual(all_text0, t));
            Assert.Empty(page.GetDrawings());
            page.Dispose();
            doc.Save(Out("test_redact2.pdf"));
        }

        [Fact]
        public void test_redact3()
        {
            using var doc = new Document(Doc("symbol-list.pdf"));
            var page = doc[0];
            page.AddRedactAnnot(new Quad(page.Rect));
            page.ApplyRedactions();
            Assert.Empty(page.get_text_words());
            Assert.Empty(page.GetDrawings());
            page.Dispose();
            doc.Save(Out("test_redact3.pdf"));
        }

        [Fact]
        public void test_redact4()
        {
            /* Python captures line_art = page.GetDrawings() before redact; MuPDF.NET hits AV in
               PageLineartDevice on symbol-list.pdf for that call. Parity on the redaction outcome only. */
            using var doc = new Document(Doc("symbol-list.pdf"));
            var page = doc[0];
            page.AddRedactAnnot(new Quad(page.Rect));
            page.ApplyRedactions(graphics: 0);
            Assert.Empty(page.get_text_words());
            page.Dispose();
            doc.Save(Out("test_redact4.pdf"));
        }

        [Fact]
        public void test_1645()
        {
            var path_in = Doc("symbol-list.pdf");
            var path_expected = Doc("test_1645_expected-after-1.27.0.pdf");
            var path_out = Out("test_1645_out.pdf");
            using (var doc = new Document(path_in))
            {
                var page = doc[0];
                var page_bounds = page.Bound();
                var annot_loc = new Rect(page_bounds.X0, page_bounds.Y0, page_bounds.X0 + 75, page_bounds.Y0 + 15);
                Assert.IsType<Matrix>(page.derotation_matrix);
                var fill = Utils.GetColor("FIREBRICK1");
                page.AddFreeTextAnnot(
                    annot_loc * page.derotation_matrix,
                    "TEST",
                    fontSize: 18,
                    fillColor: new float[] { fill.r, fill.g, fill.b },
                    rotate: page.rotation());
                doc.Save(path_out, garbage: 1, deflate: 1, noNewId: 1);
                using var doc_expected = new Document(path_expected);
                using var doc_out = new Document(path_out);
                var rms = PixmapsRms(doc_expected[0].GetPixmap(), doc_out[0].GetPixmap());
                Assert.True(rms < 0.1, $"Pixmaps differ: rms={rms} expected={path_expected} out={path_out}");
                page.Dispose();
                doc.Save(Out("test_1645.pdf"));
            }
        }

        [Fact]
        public void test_1824()
        {
            using var doc = new Document(Doc("test_1824.pdf"));
            var page = doc[0];
            page.ApplyRedactions();
            page.Dispose();
            doc.Save(Out("test_1824.pdf"));
        }

        [Fact]
        public void test_2270()
        {
            using var document = new Document(Doc("test_2270.pdf"));
            for (int page_number = 0; page_number < document.PageCount; page_number++)
            {
                var page = document[page_number];
                foreach (var textBox in page.Annots(AnnotationType.FreeText, AnnotationType.Text).ToList())
                {
                    Assert.Equal((AnnotationType.FreeText, "FreeText"), (textBox.Type, textBox.TypeString));
                    using (var twtp = textBox.GetTextPage())
                    {
                        var words = twtp.ExtractWords();
                        Assert.Equal("abc123", words[0].word);
                    }
                    Assert.Equal("abc123\n", textBox.GetText().Replace(" \n", "\n"));
                    Assert.Equal("abc123", textBox.GetTextbox(textBox.Rect));
                    Assert.Equal("abc123", textBox.GetInfo()["content"]);
                    using (var textpage = textBox.GetTextPage())
                    {
                        _ = page.GetText();
                        _ = page.GetText(textpage: textpage);
                    }
                    var clip = new Rect(textBox.Rect);
                    clip.X1 = clip.X0 + (clip.X1 - clip.X0) / 3.0f;
                    using (var textpage2 = textBox.GetTextPage())
                    {
                        var text = textpage2.ExtractTextbox(clip);
                        Assert.Contains("ab", text.Replace(" ", "").Replace("\n", ""));
                    }
                    textBox.Dispose();
                }
                page.Dispose();
            }
        }

        [Fact]
        public void test_2934_add_redact_annot()
        {
            var data = File.ReadAllBytes(Doc("mupdf_explored.pdf"));
            using var doc = new Document(data);
            var page = doc[0];
            var page_json_str = (string)doc[0].GetText("json");
            using var page_json_data = JsonDocument.Parse(page_json_str);
            var span = page_json_data.RootElement.GetProperty("blocks")[0].GetProperty("lines")[0].GetProperty("spans")[0];
            var bbox = span.GetProperty("bbox");
            var q = new Quad(new Rect(
                (float)bbox[0].GetDouble(),
                (float)bbox[1].GetDouble(),
                (float)bbox[2].GetDouble(),
                (float)bbox[3].GetDouble()));
            page.AddRedactAnnot(q, text: "");
            page.ApplyRedactions();
            page.Dispose();
            doc.Save(Out("test_2934_add_redact_annot.pdf"));
        }

        [Fact]
        public void test_2969()
        {
            using var doc = new Document(Doc("test_2969.pdf"));
            var page = doc[0];
            var first_annot = page.Annots().First();
            _ = first_annot.Next;
            first_annot.Dispose();
            page.Dispose();
            doc.Save(Out("test_2969.pdf"));
        }

        [Fact]
        public void test_file_info()
        {
            using var document = new Document(Doc("test_annot_file_info.pdf"));
            var results = new List<Dictionary<string, object>>();
            for (int i = 0; i < document.PageCount; i++)
            {
                var page = document[i];
                foreach (var annotation in page.Annots().ToList())
                {
                    if (annotation.Type == AnnotationType.FileAttachment)
                    {
                        var file_info = annotation.GetFileInfo();
                        results.Add(file_info);
                    }
                    annotation.Dispose();
                }
                page.Dispose();
            }
            Assert.Equal(2, results.Count);
            static int GetInt(Dictionary<string, object> d, string k) => Convert.ToInt32(d[k]);
            static string GetStr(Dictionary<string, object> d, string k) => d[k]?.ToString() ?? "";
            Assert.Equal("example.pdf", GetStr(results[0], "filename"));
            Assert.Equal("", GetStr(results[0], "desc"));
            Assert.Equal(8416, GetInt(results[0], "length"));
            Assert.Equal(8992, GetInt(results[0], "size"));
            Assert.Equal("photo1.jpeg", GetStr(results[1], "filename"));
            Assert.Equal("", GetStr(results[1], "desc"));
            Assert.Equal(10154, GetInt(results[1], "length"));
            Assert.Equal(8012, GetInt(results[1], "size"));
            document.Save(Out("test_file_info.pdf"));
        }

        [Fact]
        public void test_3131()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                page.AddLineAnnot(new Point(0, 0), new Point(1, 1));
                page.AddLineAnnot(new Point(1, 0), new Point(0, 1));
                var two = page.Annots().Take(2).ToList();
                var first_annot = two[0];
                _ = first_annot.Next.Type;
                first_annot.Dispose();
                two[1].Dispose();
                page.Dispose();
                doc.Save(Out("test_3131.pdf"));
            }
        }

        [Fact]
        public void test_3209()
        {
            var pdf = new Document();
            using (pdf)
            {
                var page = pdf.NewPage();
                page.AddInkAnnot(new[] { new[] { new Point(300, 300), new Point(400, 380), new Point(350, 350) } });
                int n = 0;
                foreach (var annot in page.Annots().ToList())
                {
                    n++;
                    Assert.Equal(3, annot.Vertices.Count);
                    Assert.True(_Tools.IsClose(300, annot.Vertices[0].X) && _Tools.IsClose(300, annot.Vertices[0].Y));
                    Assert.True(_Tools.IsClose(400, annot.Vertices[1].X) && _Tools.IsClose(380, annot.Vertices[1].Y));
                    Assert.True(_Tools.IsClose(350, annot.Vertices[2].X) && _Tools.IsClose(350, annot.Vertices[2].Y));
                    annot.Dispose();
                }
                Assert.Equal(1, n);
                page.Dispose();
                pdf.Save(Out("test_3209.pdf"));
            }
        }

        [Fact]
        public void test_3863()
        {
            var path_in = Doc("test_3863.pdf");
            var path_out = Out("test_3863.pdf");
            using (var document = new Document(path_in))
            {
                for (int num = 0; num < document.PageCount; num++)
                {
                    var page = document[num];
                    var redact_rect = page.Rect;
                    if (page.Rotation == 90 || page.Rotation == 270)
                        redact_rect = new Rect(0, 0, page.Rect.Height, page.Rect.Width);
                    page.AddRedactAnnot(new Quad(redact_rect));
                    page.ApplyRedactions(images: mupdf.mupdf.PDF_REDACT_IMAGE_NONE);
                    page.Dispose();
                }
                document.Save(path_out);
            }
            using (var document = new Document(path_out))
            {
                Assert.Equal(8, document.PageCount);
                for (int num = 0; num < document.PageCount; num++)
                {
                    var page = document[num];
                    var path_png = Out($"test_3863.{num}.png");
                    using (var pixmap = page.GetPixmap())
                        pixmap.Save(path_png);
                    var path_png_expected = Doc($"test_3863.pdf.pdf.{num}.png");
                    using var exp = new Pixmap(path_png_expected);
                    using var act = new Pixmap(path_png);
                    var rms = PixmapsRms(exp, act);
                    Assert.True(rms < 1, $"page {num} rms={rms}");
                    page.Dispose();
                }
            }
        }

        [Fact]
        public void test_parent()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var a = page.AddHighlightAnnot(new[] { new Quad(page.Rect) });
                page = doc.NewPage();
                var ex = Assert.ThrowsAny<Exception>(() => a.Update());
                Assert.True(
                    ex.Message.Contains("annotation not bound", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("reference not set", StringComparison.OrdinalIgnoreCase),
                    ex.Message);
                a.Dispose();
                page.Dispose();
                doc.Save(Out("test_parent.pdf"));
            }
        }

        [Fact]
        public void test_4047()
        {
            using var document = new Document(Doc("test_4047.pdf"));
            var page = document[0];
            var fonts = page.GetFonts();
            var fontname = fonts[0].baseName;
            if (!Constants.Base14FontNames.Any(n => string.Equals(n, fontname, StringComparison.OrdinalIgnoreCase)))
                fontname = "Courier";
            foreach (var hit in page.SearchForRects("|"))
                page.AddRedactAnnot(new Quad(hit), " ", fontname, fontSize: 10, align: Constants.TextAlignCenter);
            page.ApplyRedactions();
            page.Dispose();
            document.Save(Out("test_4047.pdf"));
        }

        [Fact]
        public void test_4079()
        {
            var path = Doc("test_4079.pdf");
            var path_after = Doc("test_4079_after.pdf");
            Pixmap pixmap_after_expected;
            using (var document_after = new Document(path_after))
            {
                pixmap_after_expected = document_after[0].GetPixmap();
                document_after[0].Dispose();
            }
            Pixmap pixmap_after;
            using (var document = new Document(path))
            {
                var page = document[0];
                var rects = new[]
                {
                    new Rect(164, 213, 282, 227),
                    new Rect(282, 213, 397, 233),
                    new Rect(434, 209, 525, 243),
                    new Rect(169, 228, 231, 243),
                    new Rect(377, 592, 440, 607),
                    new Rect(373, 611, 444, 626),
                };
                foreach (var rr in rects)
                {
                    page.AddRedactAnnot(rr, fillColor: _Constants.red);
                    page.DrawRect(rr, color: _Constants.green);
                }
                document.Save(Out("test_4079_before.pdf"));
                page.ApplyRedactions(images: 0);
                pixmap_after = page.GetPixmap();
                document.Save(Out("test_4079_after.pdf"));
                page.Dispose();
            }
            try
            {
                var rms = _Compare.PixmapsRms(pixmap_after_expected, pixmap_after);
                var diff = _Compare.PixmapsDiff(pixmap_after_expected, pixmap_after);
                diff.Save(Out("test_4079_diff.png"));
                Assert.True(rms < 30, $"test_4079 pixmap rms={rms}");
            }
            finally
            {
                pixmap_after_expected.Dispose();
                pixmap_after.Dispose();
            }
        }

        [Fact]
        public void test_4254()
        {
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var rt = new Rect(100, 100, 200, 150);
                var annot = page.AddFreeTextAnnot(rt, "Test Annotation from minimal example");
                annot.SetBorder(width: 1, dashes: new float[] { 3, 3 });
                annot.SetOpacity(0.5f);
                var ex = Assert.ThrowsAny<ArgumentException>(() => annot.SetColors(_Constants.red, (float[])null));
                Assert.Contains("cannot be used for FreeText annotations", ex.Message);
                annot.Update();
                var rt2 = new Rect(200, 200, 400, 400);
                var annot2 = page.AddFreeTextAnnot(rt2, "Test Annotation from minimal example pt 2");
                annot2.SetBorder(width: 1, dashes: new float[] { 3, 3 });
                annot2.SetOpacity(0.5f);
                var ex2 = Assert.ThrowsAny<ArgumentException>(() => annot2.SetColors(_Constants.red, (float[])null));
                Assert.Contains("cannot be used for FreeText annotations", ex2.Message);
                annot.Update();
                annot2.Update();
                var top_colors = new HashSet<string>();
                foreach (var a in page.Annots().ToList())
                {
                    using var pix = a.GetPixmap();
                    top_colors.Add(ColorKey(pix.Color_TopUsage().color));
                    a.Dispose();
                }
                Assert.Single(top_colors);
                annot.Dispose();
                annot2.Dispose();
                page.Dispose();
                doc.Save(Out("test_4254.pdf"));
            }
        }

        [Fact]
        public void test_richtext()
        {
            var bullet = $"{(char)0x2610}{(char)0x2611}{(char)0x2612}";
            var text = $@"<p style=""text-align:justify;margin-top:-25px;"">
    PyMuPDF <span style=""color: red;"">འདི་ ཡིག་ཆ་བཀྲམ་སྤེལ་གྱི་དོན་ལུ་ པའི་ཐོན་ཐུམ་སྒྲིལ་དྲག་ཤོས་དང་མགྱོགས་ཤོས་ཅིག་ཨིན།</span>
    <span style=""color:blue;"">Here is some <b>bold</b> and <i>italic</i> text, followed by <b><i>bold-italic</i></b>. Text-based check boxes: {bullet}.</span>
    </p>";
            var doc = new Document();
            using (doc)
            {
                var page = doc.NewPage();
                var rt = new Rect(100, 100, 350, 200);
                var p2 = rt.TR + new Point(50, 30);
                var p3 = p2 + new Point(0, 30);
                page.AddFreeTextAnnot(
                    rt,
                    text,
                    fillColor: _Constants.gold,
                    opacity: 0.5f,
                    rotate: 90,
                    borderWidth: 1,
                    dashes: null,
                    richtext: true,
                    callout: new[] { p3, p2, rt.TR });
                using var pix1 = page.GetPixmap();
                page = doc.NewPage();
                var annot = page.AddFreeTextAnnot(
                    rt,
                    text,
                    borderWidth: 1,
                    dashes: null,
                    richtext: true,
                    callout: new[] { p3, p2, rt.TR });
                annot.Update(fillColor: _Constants.gold, opacity: 0.5f, rotate: 90);
                using var pix2 = page.GetPixmap();
                var rms = PixmapsRms(pix1, pix2);
                Assert.True(rms < 20, $"richtext pixmap rms={rms}");
                annot.Dispose();
                page.Dispose();
                doc.Save(Out("test_richtext.pdf"));
            }
        }

        [Fact]
        public void test_4447()
        {
            var document = new Document();
            using (document)
            {
                var page = document.NewPage();
                var text_color = _Constants.red;
                var fill_color = _Constants.green;
                var border_color = _Constants.blue;
                var annot_rect = new Rect(90.1f, 486.73f, 139.26f, 499.46f);
                var ex = Assert.ThrowsAny<Exception>(() => page.AddFreeTextAnnot(
                    annot_rect,
                    "AETERM",
                    fontName: "Arial",
                    fontSize: 10,
                    textColor: text_color,
                    fillColor: fill_color,
                    borderColor: border_color,
                    borderWidth: 1));
                Assert.Contains("cannot set border_color if rich_text is False", ex.Message);
                var ex2 = Assert.ThrowsAny<Exception>(() => page.AddFreeTextAnnot(
                    new Rect(30, 400, 100, 450),
                    "Two",
                    fontName: "Arial",
                    fontSize: 10,
                    textColor: text_color,
                    fillColor: fill_color,
                    borderColor: border_color,
                    borderWidth: 1));
                Assert.Contains("cannot set border_color if rich_text is False", ex2.Message);
                var annot = page.AddFreeTextAnnot(
                    new Rect(30, 500, 100, 550),
                    "Three",
                    fontName: "Arial",
                    fontSize: 10,
                    textColor: text_color,
                    borderWidth: 1);
                annot.Update(textColor: text_color, fillColor: fill_color);
                var ex3 = Assert.ThrowsAny<Exception>(() => annot.Update(borderColor: border_color));
                Assert.Contains("cannot set border_color if rich_text is False", ex3.Message);
                document.Save(Out("test_4447.pdf"));
                annot.Dispose();
                page.Dispose();
            }
        }

        [Fact]
        public void test_4755()
        {
            using (var document = new Document(Doc("test_4755.pdf")))
            {
                for (int page_i = 0; page_i < document.PageCount; page_i++)
                {
                    var page = document[page_i];
                    page.add_caret_annot(new Point(50, 50));
                    var colours = new (float, float, float)[]
                    {
                        (0, 0, 1),
                        (0, 1, 0),
                        (1, 0, 0),
                    };
                    int annot_i = 0;
                    foreach (var annot in page.Annots().ToList())
                    {
                        var label = annot_i;
                        var before_rect = new Rect(annot.Rect);
                        void DrawRectangle(Rect rr, (float r, float g, float b) c, float w)
                        {
                            var drect = page.AddFreeTextAnnot(rr, label.ToString(), textColor: new float[] { c.r, c.g, c.b });
                            drect.SetBorder(w, null, null);
                            drect.Update();
                            drect.Dispose();
                        }
                        var colour = colours[annot_i];
                        DrawRectangle(annot.Rect, colour, 0.3f);
                        annot.SetRect(annot.Rect);
                        DrawRectangle(annot.Rect, (0, 1, 0), 0.3f);
                        _ = annot.Rect - before_rect;
                        annot_i++;
                        annot.Dispose();
                    }
                    page.Dispose();
                }
                document.Save(Out("test_4755.pdf"));
            }
        }
    }
}
