using System;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Port of <c>PyMuPDF-1.27.2.2/tests/test_object_manipulation.py</c>.
    /// </summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestObjectManipulation/</c>; outputs: <c>TestDocuments/_Output/TestObjectManipulation/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestObjectManipulation
    {
        private const string TestClassName = nameof(TestObjectManipulation);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_rotation1()
        {
            // doc = pymupdf.open()
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();
            // page.SetRotation(270)
            page.SetRotation(270);
            // assert doc.XrefGetKey(page.Xref, "Rotate") == ("int", "270")
            Assert.Equal(("int", "270"), doc.XrefGetKey(page.Xref, "Rotate"));
            doc.Save(Out("test_rotation1.pdf"));
        }

        [Fact]
        public void test_rotation2()
        {
            // doc = pymupdf.open()
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();
            // doc.XrefSetKey(page.Xref, "Rotate", "270")
            doc.XrefSetKey(page.Xref, "Rotate", "270");
            // assert page.rotation == 270
            Assert.Equal(270, page.Rotation);
            doc.Save(Out("test_rotation2.pdf"));
        }

        [Fact]
        public void test_trailer()
        {
            // """Access PDF trailer information."""
            // doc = pymupdf.open(filename)
            using var doc = new Document(Doc("001003ED.pdf"));
            // xreflen = doc.XrefLength
            int xreflen = doc.XrefLength;
            // _, xreflen_str = doc.XrefGetKey(-1, "Size")
            var (_, xreflenStr) = doc.XrefGetKey(-1, "Size");
            // assert xreflen == int(xreflen_str)
            Assert.Equal(xreflen, int.Parse(xreflenStr));
            // trailer_keys = doc.xref_get_keys(-1)
            var trailerKeys = doc.xref_get_keys(-1);
            // assert "ID" in trailer_keys
            Assert.Contains("ID", trailerKeys);
            // assert "Root" in trailer_keys
            Assert.Contains("Root", trailerKeys);
        }

        [Fact]
        public void test_valid_name()
        {
            // """Verify correct PDF names in method xref_set_key."""
            // doc = pymupdf.open()
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();

            // testing name in "key": confirm correct spec is accepted
            // doc.XrefSetKey(page.Xref, "Rotate", "90")
            doc.XrefSetKey(page.Xref, "Rotate", "90");
            // assert page.rotation == 90
            Assert.Equal(90, page.Rotation);

            // check wrong spec is detected
            // error_generated = False
            bool errorGenerated = false;
            try
            {
                // illegal char in name (white space)
                // doc.XrefSetKey(page.Xref, "my rotate", "90")
                doc.XrefSetKey(page.Xref, "my rotate", "90");
            }
            catch (ValueErrorException e)
            {
                // assert str(e) == "bad 'key'"
                Assert.Equal("bad 'key'", e.Message);
                errorGenerated = true;
            }
            // assert error_generated
            Assert.True(errorGenerated);

            // test name in "value": confirm correct spec is accepted
            // doc.XrefSetKey(page.Xref, "my_rotate/something", "90")
            doc.XrefSetKey(page.Xref, "my_rotate/something", "90");
            // assert doc.XrefGetKey(page.Xref, "my_rotate/something") == ("int", "90")
            Assert.Equal(("int", "90"), doc.XrefGetKey(page.Xref, "my_rotate/something"));
            // doc.XrefSetKey(page.Xref, "my_rotate", "/90")
            doc.XrefSetKey(page.Xref, "my_rotate", "/90");
            // assert doc.XrefGetKey(page.Xref, "my_rotate") == ("name", "/90")
            Assert.Equal(("name", "/90"), doc.XrefGetKey(page.Xref, "my_rotate"));

            // check wrong spec is detected
            // error_generated = False
            errorGenerated = false;
            try
            {
                // no slash inside name allowed
                // doc.XrefSetKey(page.Xref, "my_rotate", "/9/0")
                doc.XrefSetKey(page.Xref, "my_rotate", "/9/0");
            }
            catch (ValueErrorException e)
            {
                // assert str(e) == "bad 'value'"
                Assert.Equal("bad 'value'", e.Message);
                errorGenerated = true;
            }
            // assert error_generated
            Assert.True(errorGenerated);

            doc.Save(Out("test_valid_name.pdf"));
        }
    }
}
