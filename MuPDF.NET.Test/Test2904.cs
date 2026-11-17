using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// : <c>test_2904.pdf</c> page 6 (0-based index 5), image 3 has a broken JPX stream.
    /// </summary>
    /// <see cref="Page.GetImageRects"/> must throw <c>code=8: Failed to read JPX header</c> for that entry only.

    [Collection("MuPDF.NET native")]
    public class Test2904
    {
        private static readonly string testDoc = _Path.ForTestClass("test_2904.pdf", nameof(Test2904));

        [Fact]
        public void test_2904()
        {
            var mupdf_version_tuple = _Version.mupdf_version_tuple();
            Console.WriteLine($"test_2904(): mupdf_version_tuple={mupdf_version_tuple}.");
            using var pdf_docs = new Document(testDoc);
            int page_id = 0;
            foreach (Page page in pdf_docs)
            {
                var page_imgs = page.GetImages();
                int i = 0;
                foreach (var imgEntry in page_imgs)
                {
                    object img = imgEntry;
                    string imgText = FormatImage(imgEntry);
                    if (page_id == 5)
                        Console.Out.Flush();
                    Exception? e = null;
                    List<(Rect bbox, Matrix transform)> recs = null;
                    try
                    {
                        recs = page.GetImageRects(img, transform: true);
                    }
                    catch (Exception ee)
                    {
                        Console.WriteLine($"Exception: page_id={page_id} i={i} img={imgText}: {ee.Message}");
                        Console.Out.Flush();
                        e = ee;
                    }
                    if (page_id == 5)
                    {
                        string eText = e == null ? "null" : e.Message;
                        Console.WriteLine($"mupdf_version_tuple={mupdf_version_tuple}: page_id={page_id} i={i} e={eText} img={imgText}: recs={FormatRects(recs)}");
                        Console.Out.Flush();
                    }
                    if (page_id == 5 && i == 3)
                    {
                        Assert.NotNull(e);
                        Assert.Equal("code=8: Failed to read JPX header", e!.Message);
                    }
                    else
                        Assert.Null(e);
                    i++;
                }
                page_id++;
            }

            // Clear warnings, as we will have generated many.
            Tools.MupdfWarnings();
        }

        static string FormatImage(Entry img) =>
            $"({img.Xref}, {img.Smask}, {(int)img.Width}, {(int)img.Height}, {img.Bpc}, '{img.CsName}', '{img.AltCsName}', '{img.Name}', '{img.Filter}')";

        static string FormatRects(List<(Rect bbox, Matrix transform)> recs)
        {
            if (recs == null)
                return "null";
            return "[" + string.Join(", ", recs.Select(r =>
                $"({r.bbox}, {r.transform})")) + "]";
        }
    }
}