using System;
using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
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

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        /// <summary>Confirm correct identification of known examples.</summary>
        [Fact]
        public void test_cluster1()
        {
            string filename = Doc("symbol-list.pdf");
            using (var doc = new Document(filename))
            {
                var page = doc[0];
                Assert.Equal(10, page.ClusterDrawings(drawings: (List<Dictionary<string, object>>?)null).Count);
            }

            filename = Doc("chinese-tables.pdf");
            using (var doc = new Document(filename))
            {
                var page = doc[0];
                Assert.Equal(2, page.ClusterDrawings(drawings: (List<Dictionary<string, object>>?)null).Count);
            }
        }

        /// <summary>Regression test: 4599.</summary>
        [Fact]
        public void test_4599()
        {
            Console.WriteLine();
            string path = Doc("test_4599.pdf");
            int n = 0;
            using (var document = new Document(path))
            {
                foreach (var page in document)
                {
                    foreach (var clip in page.ClusterDrawings(drawings: (List<Dictionary<string, object>>?)null))
                    {
                        Console.WriteLine(clip);
                        n++;
                    }
                }
            }
            Assert.Equal(3, n);
        }

        /// <summary>
        /// Join disjoint but neighbored drawings.
        /// <summary>Regression test: cluster2.</summary>
        /// </summary>
        [Fact]
        public void test_cluster2()
        {
            //     return

            using var doc = new Document();
            var page = doc.NewPage();
            var r1 = _Constants.rect;
            var r2 = new Rect(203, 203, 400, 400);
            page.DrawRect(r1);
            page.DrawRect(r2);
            // MuPDF: page.cluster_drawings() calls get_drawings() internally. Supply equivalent "rect" dicts so we test
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
        /// <summary>Regression test: cluster3.</summary>
        /// </summary>
        [Fact]
        public void test_cluster3()
        {
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