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

        /// <summary>Regression test: insert.</summary>
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

        /// <summary>Regression test: compress.</summary>
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
                deflateImages: 1,
                deflateFonts: 1,
                pretty: 1);
        }

        /// <summary>
        /// keepProportion must preserve a square image's aspect ratio inside a non-square target.
        /// </summary>
        [Fact]
        public void test_keep_proportion_square()
        {
            using var doc = new Document();
            using var page = doc.NewPage(width: 400, height: 300);

            using var pix = new Pixmap(Colorspace.Rgb, new IRect(0, 0, 100, 100), false);
            pix.SetRect(pix.IRect, new float[] { 1f, 0f, 0f });

            var target = new Rect(50, 50, 250, 100); // 200 x 50
            page.DrawRect(target, color: new[] { 0f, 0f, 1f }, width: 1);
            page.InsertImage(target, pixmap: pix, keepProportion: true);

            var info = page.GetImageInfoDict();
            Assert.Single(info);
            var bbox = BboxFromInfo(info[0]);

            float expected = Math.Min(target.Width, target.Height); // 50
            Assert.True(Math.Abs(bbox.Width - expected) < 0.5f, $"bbox width {bbox.Width}, expected ~{expected}");
            Assert.True(Math.Abs(bbox.Height - expected) < 0.5f, $"bbox height {bbox.Height}, expected ~{expected}");
            Assert.True(Math.Abs(bbox.Width / bbox.Height - 1f) < 1e-3f);

            doc.Save(Out("keep_proportion_square.pdf"));
        }

        /// <summary>Regression test: 3087.</summary>
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