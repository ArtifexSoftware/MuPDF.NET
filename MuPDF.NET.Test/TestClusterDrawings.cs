using System;
using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Inputs: <c>TestDocuments/MuPDF.NET.Test/TestClusterDrawings/</c>.
    /// </summary>
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
                Assert.Equal(10, page.ClusterDrawings().Count);
            }

            filename = Doc("chinese-tables.pdf");
            using (var doc = new Document(filename))
            {
                var page = doc[0];
                Assert.Equal(2, page.ClusterDrawings().Count);
            }
        }

        /// <summary>Join disjoint but neighbored drawings.</summary>
        [Fact]
        public void test_cluster2()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var r1 = new Rect(100, 100, 200, 200);
            var r2 = new Rect(203, 203, 400, 400);
            page.DrawRect(r1);
            page.DrawRect(r2);
            AssertRectListsEqual(new List<Rect> { r1 | r2 }, page.ClusterDrawings());
            doc.Save(Out("test_cluster2.pdf"));
        }

        /// <summary>Confirm as separate if neighborhood threshold exceeded.</summary>
        [Fact]
        public void test_cluster3()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var r1 = new Rect(100, 100, 200, 200);
            var r2 = new Rect(204, 200, 400, 400);
            page.DrawRect(r1);
            page.DrawRect(r2);
            AssertRectListsEqual(new List<Rect> { r1, r2 }, page.ClusterDrawings());
            doc.Save(Out("test_cluster3.pdf"));
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
                    foreach (var clip in page.ClusterDrawings())
                    {
                        Console.WriteLine(clip);
                        n++;
                    }
                }
            }
            Assert.Equal(3, n);
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
