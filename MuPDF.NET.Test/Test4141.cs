// Port of PyMuPDF-1.27.2.2/tests/test_4141.py
using System;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test4141
    {
        private static readonly string testDocPath = _Path.ForTestClass("test_4141.pdf", nameof(Test4141));
        private static readonly string outDocPath = _Path.ForOutput("test_4141.pdf", nameof(Test4141));

        [Fact]
        public void test_4141()
        {
            // survive missing /Resources object in a number of cases
            var doc = new Document(testDocPath);
            try
            {
                Page page = doc[0];
                // make sure the right test file
                Assert.Equal(("null", "null"), doc.XrefGetKey(page.Xref, "Resources"));
                page.InsertHtmlbox(_Constants.rect, "Hallo", css: null, scaleLow: 0f, opacity: 1f, rotate: 0, oc: 0);  // will fail without the fix
                string docName = doc.Name;
                doc.Close();
                doc = new Document(docName);
                page = doc[0];
                var tw = new TextWriter(page.Rect);
                tw.Append(new Point(100, 100), "Hallo");
                tw.WriteText(page);  // will fail without the fix
                doc.Save(outDocPath);
            }
            finally
            {
                doc?.Dispose();
            }
        }
    }
}
