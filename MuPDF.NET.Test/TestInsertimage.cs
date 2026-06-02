/*
* Insert same image with different rotations in two places of a page.
* Extract bboxes and transformation matrices
* Assert image locations are inside given rectangles
*/
using System;
using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Port of <c>PyMuPDF-1.27.2.2/tests/test_insertimage.py</c>.
    /// </summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestInsertimage/</c>; outputs: <c>TestDocuments/_Output/TestInsertimage/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestInsertimage
    {
        private const string TestClassName = nameof(TestInsertimage);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static Rect BboxFromInfo(Dictionary<string, object> info)
        {
            if (info["bbox"] is Rect r)
                return r;
            if (info["bbox"] is float[] f && f.Length >= 4)
                return new Rect(f[0], f[1], f[2], f[3]);
            throw new InvalidOperationException("unexpected bbox in image info");
        }

        /// <summary>PyMuPDF <c>tests/test_insertimage.py::test_insert</c>.</summary>
        [Fact]
        public void test_insert()
        {
            var doc = new Document();
            var page = doc.NewPage();
            var r1 = new Rect(50, 50, 100, 100);
            var r2 = new Rect(50, 150, 200, 400);
            page.InsertImage(r1, filename: Doc("nur-ruhig.jpg"));
            page.InsertImage(r2, filename: Doc("nur-ruhig.jpg"), rotate: 270);
            var info_list = page.GetImageInfoDict();
            Assert.Equal(2, info_list.Count);
            var bbox1 = BboxFromInfo(info_list[0]);
            var bbox2 = BboxFromInfo(info_list[1]);
            Assert.True(r1.Contains(bbox1));
            Assert.True(r2.Contains(bbox2));
            doc.Save(Out("test_insert.pdf"));
        }

        /// <summary>PyMuPDF <c>tests/test_insertimage.py::test_compress</c>.</summary>
        [Fact]
        public void test_compress()
        {
            using var document = new Document(Doc("2.pdf"));
            using var document_new = new Document();
            foreach (var page in document)
            {
                using var pixmap = page.GetPixmap(
                    cs: Colorspace.Rgb,
                    dpi: 72,
                    annots: false);
                var page_new = document_new.NewPage(-1);
                page_new.InsertImage(rect: page_new.Bound(), pixmap: pixmap);
            }
            document_new.Save(
                Out("test_compress.pdf"),
                garbage: 3,
                deflate: 1,
                deflate_images: 1,
                deflate_fonts: 1,
                pretty: 1);
        }

        /// <summary>PyMuPDF <c>tests/test_insertimage.py::test_3087</c>.</summary>
        [Fact]
        public void test_3087()
        {
            using var doc = new Document(Doc("test_3087.pdf"));
            var page = doc[0];
            Console.WriteLine(page.GetImages());
            var base_ = (byte[])doc.extract_image(5)["image"];
            var mask = (byte[])doc.extract_image(5)["image"];
            page = doc.NewPage();
            page.InsertImage(page.Rect, stream: base_, mask: mask);

            using var doc2 = new Document(Doc("test_3087.pdf"));
            page = doc2[0];
            Console.WriteLine(page.GetImages());
            base_ = (byte[])doc2.extract_image(5)["image"];
            mask = (byte[])doc2.extract_image(6)["image"];
            page = doc2.NewPage();
            page.InsertImage(page.Rect, stream: base_, mask: mask);
            doc2.Save(Out("test_3087.pdf"));
        }
    }
}
