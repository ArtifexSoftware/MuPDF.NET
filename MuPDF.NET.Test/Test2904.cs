using System;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// : <c>test_2904.pdf</c> page 6 (0-based index 5), image 3 has a broken JPX stream.
    /// <see cref="Page.GetImageRects"/> must throw <c>code=8: Failed to read JPX header</c> for that entry only.
    /// </summary>
    [Collection("MuPDF.NET native")]
    public class Test2904
    {
        private static readonly string testDoc = _Path.ForTestClass("test_2904.pdf", nameof(Test2904));

        [Fact]
        public void test_2904()
        {
            Console.WriteLine($"test_2904(): mupdf_version_tuple={_Version.mupdf_version_tuple()}.");
            using var pdf_docs = new Document(testDoc);
            int page_id = 0;
            foreach (Page page in pdf_docs)
            {
                var page_imgs = page.GetImages();
                int i = 0;
                foreach (var imgEntry in page_imgs)
                {
                    object img = imgEntry;
                    if (page_id == 5)
                    {
                        Console.Out.Flush();
                    }
                    Exception? e = null;
                    try
                    {
                        var recs = page.GetImageRects(img, transform: true);
                    }
                    catch (Exception ee)
                    {
                        // MuPDF: print(f'Exception: {page_id=} {i=} {img=}: {ee}')
                        e = ee;
                    }
                    if (page_id == 5 && i == 3)
                    {
                        Assert.NotNull(e);
                        // MuPDF: assert str(e) == 'code=8: Failed to read JPX header'
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
    }
}