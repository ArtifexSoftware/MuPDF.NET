using System.IO;
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

        private static bool HasObjStm(Document doc)
        {
            for (int xref = 1; xref < doc.XrefLength; xref++)
            {
                string objstring = doc.XrefObject(xref, compressed: true);
                if (objstring.Contains("/Type/ObjStm"))
                    return true;
            }
            return false;
        }

        private static void MakePageWithContent(out Document doc, out Page page)
        {
            // make some arbitrary page with content
            string text = "Hello, World! Hallo, Welt!";
            doc = new Document();
            page = doc.NewPage();
            var rect = new Rect(50, 50, 200, 500);
            // page.insert_htmlbox(rect, text)  # place into the rectangle
            page.InsertHtmlbox(rect, text);
        }

        [Fact]
        public void test_objectstream1()
        {
            // Test save option "use_objstms".
            // This option compresses PDF object definitions into a special object type
            // "ObjStm". We test its presence by searching for that /Type.
            MakePageWithContent(out var doc, out var page);
            using (doc)
            {
                _ = doc.Write(useObjstms: true);
                Assert.True(HasObjStm(doc), "No object stream found");
                doc.Save(Out("test_objectstream1.pdf"));
            }
        }

        [Fact]
        public void test_objectstream2()
        {
            // Test save option "use_objstms".
            // This option compresses PDF object definitions into a special object type
            // "ObjStm". We test its presence by searching for that /Type.
            MakePageWithContent(out var doc, out var page);
            using (doc)
            {
                _ = doc.Write(useObjstms: false);
                Assert.False(HasObjStm(doc), "Unexpected: Object stream found!");
                doc.Save(Out("test_objectstream2.pdf"));
            }
        }

        [Fact]
        public void test_objectstream3()
        {
            // Test ez_save().
            // Should automatically use object streams
            MakePageWithContent(out var doc, out var page);
            using (doc)
            {
                using var fp = new MemoryStream();
                doc.Save(
                    fp,
                    garbage: 1,
                    clean: 0,
                    deflate: 1,
                    deflate_images: 1,
                    deflate_fonts: 1,
                    pretty: 0,
                    linear: 0,
                    ascii: 0,
                    encryption: 1,
                    noNewId: 1,
                    use_objstms: 1);
                Assert.True(HasObjStm(doc), "No object stream found");
                doc.EzSave(Out("test_objectstream3.pdf"));
            }
        }
    }
}