// import os
// import pymupdf
// from gentle_compare import gentle_compare
//
// scriptdir = os.path.dirname(__file__)
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>Port of <c>PyMuPDF-1.27.2.2/tests/test_remove-rotation.py</c>.</summary>
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

        [Fact]
        public void test_remove_rotation()
        {
            // """Remove rotation verifying identical appearance and text."""
            // filename = os.path.join(scriptdir, "resources", "test-2812.pdf")
            string filename = Resource("test-2812.pdf");
            // doc = pymupdf.open(filename)
            using var doc = new Document(filename);

            // We always create fresh pages to avoid false positives from cache content.
            // Text on these pages consists of pairwise different strings, sorting by
            // these strings must therefore yield identical bounding boxes.
            // for i in range(1, doc.page_count):
            for (int i = 1; i < doc.PageCount; i++)
            {
                var page = doc[i];
                // assert doc[i].rotation  # must be a rotated page
                Assert.NotEqual(0, page.Rotation);
                // pix0 = doc[i].GetPixmap()  # make image
                var pix0 = page.GetPixmap();
                // words0 = []
                var words0 = new List<(float x0, float y0, float x1, float y1, string word)>();
                // for w in doc[i].GetText("words"):
                foreach (var w in page.get_text_words())
                {
                    // words0.Append(list(pymupdf.Rect(w[:4]) * doc[i].rotation_matrix) + [w[4]])
                    var r = new Rect(w.x0, w.y0, w.x1, w.y1) * page.RotationMatrix;
                    words0.Add(((float)r.X0, (float)r.Y0, (float)r.X1, (float)r.Y1, w.word));
                }
                // words0.sort(key=lambda w: w[4])  # sort by word strings
                words0 = words0.OrderBy(w => w.word).ToList();
                // derotate page and confirm nothing else has changed
                // doc[i].RemoveRotation()
                page.RemoveRotation();
                // assert doc[i].rotation == 0
                Assert.Equal(0, page.Rotation);
                // pix1 = doc[i].GetPixmap()
                var pix1 = page.GetPixmap();
                // words1 = doc[i].GetText("words")
                var words1Raw = page.get_text_words();
                // words1.sort(key=lambda w: w[4])  # sort by word strings
                var words1 = words1Raw
                    .OrderBy(w => w.word)
                    .Select(w => (w.x0, w.y0, w.x1, w.y1, w.word))
                    .ToList();
                // assert pix1.digest == pix0.digest, f"{pix1.digest}/{pix0.digest}"
                Assert.True(
                    pix1.digest.SequenceEqual(pix0.digest),
                    $"{string.Join(",", pix1.digest)}/{string.Join(",", pix0.digest)}");
                // assert gentle_compare(words0, words1)
                Assert.True(_Compare.GentleCompareWordList(words0, words1));
            }
            doc.Save(Out("test_remove_rotation.pdf"));
        }
    }
}
