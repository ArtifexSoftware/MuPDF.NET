using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for the Shape class.
    /// Ported from tests/test_general.py drawing tests.
    /// </summary>
    public class ShapeTests
    {
        private (Document doc, Page page) CreateDocWithPage()
        {
            var doc = new Document();
            var page = doc.NewPage();
            return (doc, page);
        }

        // ─── Construction ───────────────────────────────────────────────

        [Fact]
        public void Shape_Create()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                Assert.NotNull(shape);
                Assert.Equal(0, shape.DrawCount);
            }
        }

        // ─── Drawing primitives ─────────────────────────────────────────

        [Fact]
        public void Shape_DrawLine()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                var last = shape.DrawLine(new Point(10, 10), new Point(200, 200));
                Assert.Equal(1, shape.DrawCount);
                Assert.True(last.X > 0);
            }
        }

        [Fact]
        public void Shape_DrawRect()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                shape.DrawRect(new Rect(50, 50, 200, 200));
                Assert.Equal(1, shape.DrawCount);
            }
        }

        [Fact]
        public void Shape_DrawCircle()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                shape.DrawCircle(new Point(200, 200), 50);
                Assert.Equal(1, shape.DrawCount);
            }
        }

        [Fact]
        public void Shape_DrawOval()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                shape.DrawOval(new Rect(100, 100, 300, 200));
                Assert.Equal(1, shape.DrawCount);
            }
        }

        [Fact]
        public void Shape_DrawCurve()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                shape.DrawCurve(
                    new Point(50, 50),
                    new Point(100, 25),
                    new Point(150, 50)
                );
                Assert.Equal(1, shape.DrawCount);
            }
        }

        [Fact]
        public void Shape_DrawBezier()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                shape.DrawBezier(
                    new Point(50, 50),
                    new Point(75, 25),
                    new Point(125, 75),
                    new Point(150, 50)
                );
                Assert.Equal(1, shape.DrawCount);
            }
        }

        [Fact]
        public void Shape_DrawPolyline()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                var points = new[]
                {
                    new Point(50, 50), new Point(100, 100),
                    new Point(150, 50), new Point(200, 100)
                };
                shape.DrawPolyline(points);
                Assert.Equal(1, shape.DrawCount);
            }
        }

        [Fact]
        public void Shape_DrawQuad()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                var q = new Quad(
                    new Point(50, 50), new Point(200, 50),
                    new Point(50, 200), new Point(200, 200)
                );
                shape.DrawQuad(q);
                Assert.Equal(1, shape.DrawCount);
            }
        }

        [Fact]
        public void Shape_DrawSector()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                shape.DrawSector(new Point(200, 200), new Point(250, 200), 90);
                Assert.Equal(1, shape.DrawCount);
            }
        }

        [Fact]
        public void Shape_DrawSquiggle()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                shape.DrawSquiggle(new Point(50, 100), new Point(400, 100));
                Assert.Equal(1, shape.DrawCount);
            }
        }

        [Fact]
        public void Shape_DrawZigzag()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                shape.DrawZigzag(new Point(50, 100), new Point(400, 100));
                Assert.Equal(1, shape.DrawCount);
            }
        }

        // ─── Finish & Commit ────────────────────────────────────────────

        [Fact]
        public void Shape_FinishAndCommit()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                shape.DrawRect(new Rect(50, 50, 200, 200));
                shape.Finish(color: new float[] { 1, 0, 0 }, width: 2);
                shape.Commit();
                Assert.Equal(0, shape.DrawCount);
            }
        }

        [Fact]
        public void Shape_MultipleDrawsFinishCommit()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                shape.DrawRect(new Rect(10, 10, 100, 100));
                shape.DrawCircle(new Point(200, 200), 50);
                Assert.Equal(2, shape.DrawCount);
                shape.Finish(color: new float[] { 0, 0, 1 });
                shape.Commit();
            }
        }

        // ─── InsertText ─────────────────────────────────────────────────

        [Fact]
        public void Shape_InsertText()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                int lines = shape.InsertText(new Point(72, 72), "Hello from Shape");
                Assert.True(lines > 0);
                shape.Commit();
            }
        }

        [Fact]
        public void Shape_InsertTextbox()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                var (rc, rest) = shape.InsertTextbox(
                    new Rect(50, 50, 300, 200),
                    "This is a textbox test with some longer text."
                );
                Assert.True(rc >= 0);
                shape.Commit();
            }
        }

        // ─── Rect property ──────────────────────────────────────────────

        [Fact]
        public void Shape_RectAfterDraw()
        {
            var (doc, page) = CreateDocWithPage();
            using (doc)
            {
                var shape = page.NewShape();
                shape.DrawRect(new Rect(50, 50, 200, 200));
                shape.Finish();
                var r = shape.Rect;
                Assert.True(r.Width > 0);
                Assert.True(r.Height > 0);
            }
        }
    }
}
