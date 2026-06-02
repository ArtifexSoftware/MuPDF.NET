// import pymupdf
// import os
//
// scriptdir = os.path.dirname(__file__)
using System;
using System.IO;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>Port of <c>PyMuPDF-1.27.2.2/tests/test_rewrite_images.py</c>.</summary>
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
            // """Example for decreasing file size by more than 30%."""
            string filename = Doc("test-rewrite-images.pdf");
            // doc = pymupdf.open(filename)
            using var doc = new Document(filename);
            // size0 = os.path.getsize(doc.name)
            long size0 = new System.IO.FileInfo(doc.Name).Length;
            // doc.rewrite_images(dpi_threshold=100, dpi_target=72, quality=33)
            doc.RewriteImages(dpiThreshold: 100, dpiTarget: 72, quality: 33);
            // data = doc.tobytes(garbage=3, deflate=True)
            byte[] data;
            using (var ms = new MemoryStream())
            {
                doc.Save(ms, garbage: 3, deflate: 1);
                data = ms.ToArray();
            }
            // size1 = len(data)
            int size1 = data.Length;
            // assert (1 - (size1 / size0)) > 0.3
            Assert.True((1 - (size1 / (float)size0)) > 0.3);
            doc.Save(Out("test_rewrite_images.pdf"));
        }
    }
}
