using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Outputs: <c>TestDocuments/_Output/TestObjectstreams/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestObjectstreams
    {
        private const string TestClassName = nameof(TestObjectstreams);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_objectstream1()
        {
            // Test save option "use_objstms".
            // This option compresses PDF object definitions into a special object type
            // "ObjStm". We test its presence by searching for that /Type.
            string text = "Hello, World! Hallo, Welt!";
            using var doc = new Document();
            var page = doc.NewPage();
            var rect = new Rect(50, 50, 200, 500);

            page.InsertHtmlbox(rect, text);  // place into the rectangle
            _ = doc.Write(useObjstms: true);
            bool found = false;
            for (int xref = 1; xref < doc.XrefLength; xref++)
            {
                string objstring = doc.XrefObject(xref, compressed: true);
                if (objstring.Contains("/Type/ObjStm"))
                {
                    found = true;
                    break;
                }
            }
            Assert.True(found, "No object stream found");
            doc.Save(Out("test_objectstream1.pdf"));
        }

        [Fact]
        public void test_objectstream2()
        {
            // Test save option "use_objstms".
            // This option compresses PDF object definitions into a special object type
            // "ObjStm". We test its presence by searching for that /Type.
            string text = "Hello, World! Hallo, Welt!";
            using var doc = new Document();
            var page = doc.NewPage();
            var rect = new Rect(50, 50, 200, 500);

            page.InsertHtmlbox(rect, text);  // place into the rectangle
            _ = doc.Write(useObjstms: false);

            bool found = false;
            for (int xref = 1; xref < doc.XrefLength; xref++)
            {
                string objstring = doc.XrefObject(xref, compressed: true);
                if (objstring.Contains("/Type/ObjStm"))
                {
                    found = true;
                    break;
                }
            }
            Assert.False(found, "Unexpected: Object stream found!");
            doc.Save(Out("test_objectstream2.pdf"));
        }

        [Fact]
        public void test_objectstream3()
        {
            // Test ez_save.
            // Should automatically use object streams
            string text = "Hello, World! Hallo, Welt!";
            using var doc = new Document();
            var page = doc.NewPage();
            var rect = new Rect(50, 50, 200, 500);

            page.InsertHtmlbox(rect, text);  // place into the rectangle

            // ez_save(BytesIO); C# EzSave is filename-only (no extra overload).
            doc.EzSave(Out("test_objectstream3.pdf"));
            bool found = false;
            for (int xref = 1; xref < doc.XrefLength; xref++)
            {
                string objstring = doc.XrefObject(xref, compressed: true);
                if (objstring.Contains("/Type/ObjStm"))
                {
                    found = true;
                    break;
                }
            }
            Assert.True(found, "No object stream found!");
        }
    }
}
