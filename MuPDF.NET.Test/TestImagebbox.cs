using System;
using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Ensures equality of bboxes computed via
    /// <c>page.get_image_bbox()</c>,
    /// <c>page.GetImageInfo()</c>, and
    /// <c>page.GetBboxlog()</c>.
    /// Inputs: <c>TestDocuments/TestImagebbox/</c>; outputs: <c>TestDocuments/_Output/TestImagebbox/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestImagebbox
    {
        private const float Tol = 1e-4f;
        private const string TestClassName = nameof(TestImagebbox);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static Rect BboxFromInfo(Dictionary<string, object> im)
        {
            if (!im.TryGetValue("bbox", out var bboxObj))
                throw new InvalidOperationException("image info missing bbox");
            if (bboxObj is Rect r)
                return r;
            if (bboxObj is float[] f && f.Length >= 4)
                return new Rect(f[0], f[1], f[2], f[3]);
            if (bboxObj is float[] d && d.Length >= 4)
                return new Rect(d[0], d[1], d[2], d[3]);
            throw new InvalidOperationException($"unexpected bbox type: {bboxObj?.GetType().Name}");
        }

        private static bool BboxesNear(Rect a, Rect b) => (a - b).Norm() < Tol;

        /// <summary>Regression test: image bbox.</summary>
        [Fact]
        public void test_image_bbox()
        {
            using var doc = new Document(Doc("image-file1.pdf"));
            var page = doc[0];

            var bboxList = new List<Rect>();
            foreach (var item in doc.get_page_images_py(0, full: true))
                bboxList.Add(page.GetImageBbox(item));

            var infos = page.GetImageInfoDict(xrefs: true);
            bool match = false;
            foreach (var im in infos)
            {
                var bbox1 = BboxFromInfo(im);
                match = false;
                foreach (var bbox2 in bboxList)
                {
                    if (BboxesNear(bbox2, bbox1))
                    {
                        match = true;
                        break;
                    }
                }
            }
            Assert.True(match);
        }

        /// <summary>Regression test: bboxlog.</summary>
        [Fact]
        public void test_bboxlog()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            int xref = page.InsertImage(page.Rect, filename: Doc("img-transparent.png"));

            var imgInfo = page.GetImageInfoDict(xrefs: true);
            Assert.Single(imgInfo);
            var info = imgInfo[0];
            Assert.Equal(xref, Convert.ToInt32(info["xref"]));

            var bboxLog = page.GetBboxlogTuples();
            Assert.Single(bboxLog);
            var (boxType, bbox, _) = bboxLog[0];
            Assert.Equal("fill-image", boxType);
            Assert.Equal(BboxFromInfo(info), bbox);
            doc.Save(Out("test_bboxlog.pdf"));
        }
    }
}