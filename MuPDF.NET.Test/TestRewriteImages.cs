using System;
using System.IO;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestRewriteImages/</c>; outputs: <c>TestDocuments/_Output/TestRewriteImages/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestRewriteImages
    {
        private const string TestClassName = nameof(TestRewriteImages);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_rewrite_images()
        {
            string filename = Doc("test-rewrite-images.pdf");
            using var doc = new Document(filename);
            long size0 = new System.IO.FileInfo(doc.Name).Length;
            doc.RewriteImages(dpiThreshold: 100, dpiTarget: 72, quality: 33);
            byte[] data;
            using (var ms = new MemoryStream())
            {
                doc.Save(ms, garbage: 3, deflate: 1);
                data = ms.ToArray();
            }
            // size1 = len(data)
            int size1 = data.Length;
            Assert.True((1 - (size1 / (float)size0)) > 0.3);
            doc.Save(Out("test_rewrite_images.pdf"));
        }

        [Fact]
        public void test_4918()
        {
            string path = Doc("test_4918.pdf");
            Console.WriteLine($"path={path}");
            using var document = new Document(path);
            document.RewriteImages(dpiThreshold: 150, dpiTarget: 100, quality: 50);
            document.Save(Out("test_4918.pdf"));
        }
    }
}