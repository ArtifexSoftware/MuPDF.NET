// Port of PyMuPDF-1.27.2.2/tests/test_4505.py
using System.Collections.Generic;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test4505
    {
        private static readonly string testDocPath = _Path.ForTestClass("test_4505.pdf", nameof(Test4505));

        [Fact]
        public void test_4505()
        {
            // Copy field flags to Parent widget and all of its kids.
            // path = os.path.abspath(f"{__file__}/../../tests/resources/test_4505.pdf")
            using var doc = new Document(testDocPath);
            Page page = doc[0];
            var text1_flags_before = new Dictionary<int, int>();
            var text1_flags_after = new Dictionary<int, int>();
            // extract all widgets having the same field name
            foreach (Widget w in page.Widgets())
            {
                if (w.field_name != "text_1")
                    continue;
                text1_flags_before[w.xref] = w.field_flags;
            }
            // expected exiting field flags
            Assert.Equal(new Dictionary<int, int> { { 8, 1 }, { 10, 0 }, { 33, 0 } }, text1_flags_before);
            Widget w0 = page.LoadWidget(8);  // first of these widgets
            // give all connected widgets that field flags value
            w0.update(sync_flags: true);
            // confirm that all connected widgets have the same field flags
            foreach (Widget w in page.Widgets())
            {
                if (w.field_name != "text_1")
                    continue;
                text1_flags_after[w.xref] = w.field_flags;
            }
            Assert.Equal(new Dictionary<int, int> { { 8, 1 }, { 10, 1 }, { 33, 1 } }, text1_flags_after);
        }
    }
}
