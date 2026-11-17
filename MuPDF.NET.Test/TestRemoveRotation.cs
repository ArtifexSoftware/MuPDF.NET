using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestRemoveRotation/</c>; outputs: <c>TestDocuments/_Output/TestRemoveRotation/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestRemoveRotation
    {
        private const string TestClassName = nameof(TestRemoveRotation);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static string Resource(string name) => Doc(name);

        private static bool HasFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Required test document not found: {path}");
            return true;
        }

        private static readonly int[] ROTATIONS = { 90, 180, 270 };

        [Fact]
        public void test_remove_rotation()
        {

            string filename = Resource("test-2812.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);

            // We always create fresh pages to avoid false positives from cache content.
            // Text on these pages consists of pairwise different strings, sorting by
            // these strings must therefore yield identical bounding boxes.
            for (int i = 1; i < doc.PageCount; i++)
            {
                var page = doc[i];
                Assert.NotEqual(0, page.Rotation);
                var pix0 = page.GetPixmap();
                pix0.Save(Out($"test_remove-rotation_test_pix0_{i}.png"));
                var words0 = new List<(float x0, float y0, float x1, float y1, string word)>();
                foreach (var w in page.GetTextWords())
                {
                    var r = new Rect(w.x0, w.y0, w.x1, w.y1) * page.RotationMatrix;
                    words0.Add(((float)r.X0, (float)r.Y0, (float)r.X1, (float)r.Y1, w.word));
                }
                words0 = words0.OrderBy(w => w.word).ToList();
                // doc[i].RemoveRotation
                page.RemoveRotation();
                Assert.Equal(0, page.Rotation);
                var pix1 = page.GetPixmap();
                pix1.Save(Out($"test_remove-rotation_test_pix1_{i}.png"));
                var words1 = page.GetTextWords()
                    .OrderBy(w => w.word)
                    .Select(w => (w.x0, w.y0, w.x1, w.y1, w.word))
                    .ToList();
                Assert.True(
                    pix1.Digest.SequenceEqual(pix0.Digest),
                    $"{string.Join(",", pix1.Digest)}/{string.Join(",", pix0.Digest)}");
                Assert.True(_Compare.GentleCompareWordList(words0, words1));
            }
        }

        [Fact]
        public void test_5077()
        {
            string filename = Resource("test-4000-links.pdf");
            if (!HasFile(filename)) return;
            using var doc = new Document(filename);
            var page = doc[0];
            Console.WriteLine($"page.Rotation={page.Rotation}, link count={page.GetLinks().Count}");
            var t0 = Stopwatch.GetTimestamp();
            page.RemoveRotation();
            doc.Save(Out("test_5077.pdf"));
            double elapsed = Stopwatch.GetElapsedTime(t0).TotalSeconds;
            Assert.True(elapsed < 10, $"de-rotation time {elapsed:F3} seconds is too long.");
        }

        [Fact]
        public void test_5059()
        {
            foreach (int rot in ROTATIONS)
            {
                using var src = MakeInputPdf(rot);
                var page = src[0];
                using var pixOrig = page.GetPixmap(cs: Colorspace.Gray);
                pixOrig.Save(Out($"test_5059_{rot}_orig.png"));
                page.RemoveRotation();
                using var pixRot0 = page.GetPixmap(cs: Colorspace.Gray);
                pixRot0.Save(Out($"test_5059_{rot}_rot0.png"));
                float rmsVal = _Compare.PixmapsRms(pixOrig, pixRot0);
                Console.WriteLine($"rotation={rot} irect orig={pixOrig.IRect} rot0={pixRot0.IRect} rms={rmsVal}");
                Assert.True(rmsVal < 2, $"RMS diff for rotation {rot}: {rmsVal}");
            }
        }

        private static void AddTestContent(Page page, int rotation)
        {
            page.DrawPolyline(new[]
            {
                new Point(20, 20), new Point(400, 20), new Point(400, 300),
                new Point(20, 300), new Point(20, 20),
            });
            page.DrawLine(new Point(20, 160), new Point(400, 160));
            page.InsertText(new Point(30, 150), $"PAGE ROT {rotation}", fontSize: 10);

            var rectA = new Rect(30, 30, 90, 80);
            var square = page.AddRectAnnot(rectA);
            square.SetColors(stroke: new float[] { 1, 0, 0 });
            square.SetBorder(width: 1);
            square.Update();

            var rectB = new Rect(110, 30, 170, 80);
            var circle = page.AddCircleAnnot(rectB);
            circle.SetColors(stroke: new float[] { 0, 0, 1 });
            circle.SetBorder(width: 1);
            circle.Update();

            var line = page.AddLineAnnot(new Point(190, 30), new Point(260, 80));
            line.SetColors(stroke: new float[] { 0, 0.5f, 0 });
            line.SetBorder(width: 1);
            line.Update();

            var polyline = page.AddPolylineAnnot(new[]
            {
                new Point(280, 30), new Point(320, 40), new Point(290, 80), new Point(350, 70),
            });
            polyline.SetColors(stroke: new float[] { 1, 0.4f, 0 });
            polyline.SetBorder(width: 1);
            polyline.Update();

            var polygon = page.AddPolygonAnnot(new[]
            {
                new Point(30, 100), new Point(80, 120), new Point(60, 150), new Point(20, 140),
            });
            polygon.SetColors(stroke: new float[] { 0.6f, 0, 0.8f });
            polygon.SetBorder(width: 1);
            polygon.Update();

            var ink = page.AddInkAnnot(new[]
            {
                new[] { new Point(110, 100), new Point(120, 120), new Point(130, 110), new Point(140, 130) },
                new[] { new Point(150, 100), new Point(160, 120), new Point(170, 110) },
            });
            ink.SetColors(stroke: new float[] { 0, 0.7f, 0.7f });
            ink.SetBorder(width: 1);
            ink.Update();

            var ftRect = new Rect(340, 250, 400, 300);
            var ft = page.AddFreeTextAnnot(ftRect, $"FT {rotation}", textColor: new float[] { 1, 0, 0 }, rotate: rotation);
            ft.Update();

            var textAnnot = page.AddTextAnnot(new Point(200, 120), $"Note {rotation}");
            textAnnot.Update();

            var stampRect = new Rect(280, 100, 380, 145);
            var stamp = page.AddStampAnnot(stampRect, stamp: 13);
            stamp.Update();

            page.InsertText(new Point(30, 220), "MARK_A MARK_B MARK_C MARK_D", fontSize: 12);

            var q1 = page.SearchFor("MARK_A");
            if (q1.Count > 0)
                page.AddHighlightAnnot(q1);

            var q2 = page.SearchFor("MARK_B");
            if (q2.Count > 0)
                page.AddUnderlineAnnot(q2);

            var q3 = page.SearchFor("MARK_C");
            if (q3.Count > 0)
                page.AddStrikeoutAnnot(q3);

            var q4 = page.SearchFor("MARK_D");
            if (q4.Count > 0)
                page.AddSquigglyAnnot(q4);
        }

        private static Document MakeInputPdf(int rotation)
        {
            var doc = new Document();
            var page = doc.NewPage(width: 420, height: 320);
            page.SetRotation(rotation);
            AddTestContent(page, rotation);
            return doc;
        }
    }
}
