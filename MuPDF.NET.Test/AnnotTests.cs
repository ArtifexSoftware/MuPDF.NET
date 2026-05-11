using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for the Annot class.
    /// Ported from tests/test_annots.py.
    /// </summary>
    public class AnnotTests
    {
        private float[] red = { 1, 0, 0 };
        private float[] blue = { 0, 0, 1 };
        private float[] gold = { 1, 1, 0 };
        private float[] green = { 0, 1, 0 };

        private Rect displ = new Rect(0, 50, 0, 50);
        private Rect r = new Rect(72, 72, 220, 100);
        private string t1 = "têxt üsès Lätiñ charß,\nEUR: €, mu: µ, super scripts: ²³!";
        private Rect rect = new Rect(100, 100, 200, 200);

        private (Document doc, Page page) CreateDocWithPage()
        {
            var doc = new Document();
            var page = doc.NewPage();
            return (doc, page);
        }

        // ─── Add annotations ────────────────────────────────────────────

        [Fact]
        public void Annot_AddTextAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddTextAnnot(r.TL, t1);
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddTextAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("Text", annot.TypeString);
                Assert.True(annot.Xref > 0);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        [Fact]
        public void Annot_AddFreeTextAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                // `borderWidth` exists only on the full AddFreeTextAnnot overload (disambiguates from compact).
                var annot = page.AddFreeTextAnnot(
                    rect,
                    t1,
                    fontsize: 10,
                    rotate: 90,
                    textColor: blue,
                    fillColor: gold, 
                    align: Constants.TEXT_ALIGN_CENTER
                );
                annot.SetBorder(width: 0.3f, dashes: new float[] { 2 });
                annot.Update(textColor: blue, fillColor: gold);
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddFreeTextAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("FreeText", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        [Fact]
        public void Annot_AddHighlightAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddHighlightAnnot(new[] { new Quad(rect) });
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddHighlightAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("Highlight", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        [Fact]
        public void Annot_AddUnderlineAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddUnderlineAnnot(new[] { new Quad(rect) });
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddUnderlineAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("Underline", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        [Fact]
        public void Annot_AddStrikeoutAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddStrikeoutAnnot(new[] { new Quad(rect) });
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddStrikeoutAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("StrikeOut", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        [Fact]
        public void Annot_AddSquigglyAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddSquigglyAnnot(new[] { new Quad(rect) });
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddSquigglyAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("Squiggly", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        [Fact]
        public void Annot_AddLineAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddLineAnnot(new Point(50, 50), new Point(200, 200));
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddLineAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("Line", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        [Fact]
        public void Annot_AddRectAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddRectAnnot(new Rect(50, 50, 200, 200));
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddRectAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("Square", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        [Fact]
        public void Annot_AddCircleAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddCircleAnnot(new Rect(50, 50, 200, 200));
                annot.Update(fillColor: new float[] { 1, 0, 0 }, rotate: 20, opacity: 0.1f);
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddCircleAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("Circle", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        [Fact]
        public void Annot_AddPolylineAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                Rect rect = page.Rect + new Rect(100, 36, -100, -36);

                var cell = Utils.MakeTable(rect, rows: 10);

                for (int i = 0; i < 10; i++)
                {
                    // (cell[i][0].bl, cell[i][0].br)
                    var points = new[]
                    {
                        cell[i][0].BottomLeft,
                        cell[i][0].BottomRight
                    };

                    var annot_ = page.AddPolylineAnnot(points);

                    annot_.SetLineEnds(i, i);
                    annot_.Update();
                }

                int index = 0;

                foreach (var annot__ in page.annots())
                {
                    var lineEnds = annot__.line_ends;

                    //Debug.Assert(lineEnds.Item1 == index);
                    //Debug.Assert(lineEnds.Item2 == index);

                    index++;
                }

                // last annot from enumeration
                var lastAnnot = page.annots().Last();

                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddPolylineAnnot.pdf");
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        [Fact]
        public void Annot_AddPolygonAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var points = new List<Point>
                {
                    rect.BL, rect.TR, rect.BR, rect.TL
                };
                var annot = page.AddPolygonAnnot(points.ToArray());
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddPolygonAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("Polygon", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        [Fact]
        public void Annot_AddInkAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var strokes = new List<List<Point>>
                {
                    new List<Point>
                    {
                        new Point(50, 50), new Point(100, 100), new Point(150, 50)
                    }
                };
                var annot = page.AddInkAnnot(strokes.Select(s => s.ToArray()).ToArray());
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddInkAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("Ink", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        [Fact]
        public void Annot_AddCaretAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddCaretAnnot(new Point(100, 100));
                annot.Update(rotate: 20);
                Assert.NotNull(annot);
                Assert.Equal("Caret", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                List<string> names = page.AnnotNames();
                var xrefs = page.AnnotXrefs();
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on save.
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddCaretAnnot.pdf");
            }
        }

        [Fact]
        public void Annot_AddStampAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddStampAnnot(new Rect(100, 100, 300, 200));
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddStampAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("Stamp", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on save.
            }
        }

        [Fact]
        public void Annot_AddRedactAnnot()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddRedactAnnot(new Quad(new Rect(50, 50, 200, 100)));
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_AddRedactAnnot.pdf");
                Assert.NotNull(annot);
                Assert.Equal("Redact", annot.TypeString);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on
            }
        }

        // ─── Annotation properties ──────────────────────────────────────

        [Fact]
        public void Annot_RectProperty()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddRectAnnot(new Rect(50, 50, 200, 200));
                var rect = annot.Rect;
                Assert.True(rect.Width > 0);
                Assert.True(rect.Height > 0);
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_RectProperty.pdf");
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on save.
            }
        }

        [Fact]
        public void Annot_Contents()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddTextAnnot(new Point(100, 100), "My Contents");
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_Contents.pdf");
                Assert.Equal("My Contents", annot.Contents);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on save.
            }
        }

        [Fact]
        public void Annot_Flags()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddTextAnnot(new Point(100, 100), "Flags");
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_Flags.pdf");
                Assert.True(annot.Flags >= 0);
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on save.
            }
        }

        [Fact]
        public void Annot_Opacity()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot1 = page.AddTextAnnot(new Point(100, 100), "Flags");
                //var annot = page.AddRectAnnot(new Rect(50, 50, 200, 200));
                var annot = page.AddRedactAnnot(new Quad(new Rect(50, 50, 200, 200)));
                //annot.SetOpacity(0.5f);
                Assert.False(TestHelper.IsClose(0.5, annot.Opacity, 0.01));
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_Opacity.pdf");
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on save.
            }
        }

        // ─── Iteration ──────────────────────────────────────────────────

        [Fact]
        public void Page_AnnotsEnumeration()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                page.AddTextAnnot(new Point(50, 50), "A1");
                page.AddRectAnnot(new Rect(100, 100, 200, 200));
                page.AddCircleAnnot(new Rect(200, 200, 300, 300));
                int count = page.Annots().Count();
                Assert.Equal(3, count);
                doc.Save(@"E:\Pdf\Tmp\Test\Page_AnnotsEnumeration.pdf");
                page.Dispose();
            }
        }

        [Fact]
        public void Page_NoAnnots()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                int count = page.Annots().Count();
                Assert.Equal(0, count);
            }
        }

        // ─── Delete ─────────────────────────────────────────────────────

        [Fact]
        public void Annot_Delete()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddTextAnnot(new Point(50, 50), "Delete Me");
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_Delete_Before.pdf");
                Assert.NotNull(page.FirstAnnot);
                page.DeleteAnnot(annot);
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_Delete.pdf");
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on save.
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on save.
            }
        }

        // ─── Update ─────────────────────────────────────────────────────

        [Fact]
        public void Annot_Update()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddRectAnnot(new Rect(50, 50, 200, 200));
                annot.SetColors(new float[] { 0, 1, 1 });
                //annot.Update(default(float[]), opacity: 0);
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_Update.pdf");
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on save.
            }
        }

        // ─── SetInfo / GetInfo ──────────────────────────────────────────

        [Fact]
        public void Annot_SetAndGetInfo()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var annot = page.AddTextAnnot(new Point(50, 50), "Info Test");
                annot.SetInfo(new Dictionary<string, string> { ["title"] = "Test Author" });
                var result = annot.GetInfo();
                Assert.Contains("title", result.Keys);
                Assert.Equal("Test Author", result["title"]);
                doc.Save(@"E:\Pdf\Tmp\Test\Annot_SetAndGetInfo.pdf");
                annot.Dispose(); // Dispose annot before page to avoid "Page still in use" error on
                page.Dispose(); // Dispose page before doc to avoid "Page still in use" error on save.
            }
        }
    }
}
