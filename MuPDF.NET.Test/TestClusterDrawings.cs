// Port of PyMuPDF-1.27.2.2/tests/test_cluster_drawings.py
using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Port of <c>PyMuPDF-1.27.2.2/tests/test_cluster_drawings.py</c>.
    /// Outputs: <c>TestDocuments/_Output/TestClusterDrawings/</c>.
    /// </summary>
    /// <remarks>
    /// <c>test_cluster2</c> and <c>test_cluster3</c> assert <see cref="Page.ClusterDrawings"/> merge/split logic using
    /// supplied drawing dicts (no <see cref="Page.GetDrawings"/>).
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestClusterDrawings
    {
        private const string TestClassName = nameof(TestClusterDrawings);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        /// <summary>
        /// Join disjoint but neighbored drawings.
        /// PyMuPDF: <c>tests/test_cluster_drawings.py::test_cluster2</c>.
        /// </summary>
        [Fact]
        public void test_cluster2()
        {
            // if not hasattr(pymupdf, "mupdf"):
            //     print("Not executing 'test_cluster2' in classic")
            //     return

            using var doc = new Document();
            var page = doc.NewPage();
            var r1 = _Constants.rect;
            var r2 = new Rect(203, 203, 400, 400);
            page.DrawRect(r1);
            page.DrawRect(r2);
            // PyMuPDF: page.cluster_drawings() calls get_drawings() internally. Supply equivalent "rect" dicts so we test
            // ClusterDrawings without invoking GetDrawings (same skip reason as test_cluster1).
            var drawings = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["rect"] = r1 },
                new Dictionary<string, object> { ["rect"] = r2 },
            };
            AssertRectListsEqual(new List<Rect> { r1 | r2 }, page.ClusterDrawings(drawings: drawings));
            doc.Save(Out("test_cluster2.pdf"));
        }

        /// <summary>
        /// Confirm as separate if neighborhood threshold exceeded.
        /// PyMuPDF: <c>tests/test_cluster_drawings.py::test_cluster3</c>.
        /// </summary>
        [Fact]
        public void test_cluster3()
        {
            // if not hasattr(pymupdf, "mupdf"):
            //     print("Not executing 'test_cluster3' in classic")
            //     return

            using var doc = new Document();
            var page = doc.NewPage();
            var r1 = _Constants.rect;
            var r2 = new Rect(204, 200, 400, 400);
            page.DrawRect(r1);
            page.DrawRect(r2);
            var drawings = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["rect"] = r1 },
                new Dictionary<string, object> { ["rect"] = r2 },
            };
            AssertRectListsEqual(new List<Rect> { r1, r2 }, page.ClusterDrawings(drawings: drawings));
            doc.Save(Out("test_cluster3.pdf"));
        }

        private static void AssertRectListsEqual(IReadOnlyList<Rect> expected, IReadOnlyList<Rect> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.True(
                    expected[i] == actual[i],
                    $"expected[{i}]={expected[i]} actual[{i}]={actual[i]}");
            }
        }
    }
}
