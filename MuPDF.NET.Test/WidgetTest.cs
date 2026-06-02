using mupdf;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class WidgetTest
    {
        private const string TestClassName = nameof(WidgetTest);
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);
        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void Text()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Widget w = new Widget(page);

            w.BorderColor = new float[] { 0, 0, 1 };
            w.BorderWidth = 0.3f;
            w.BorderStyle = "d";
            w.BorderDashes = new int[] { 2, 3 };
            w.FieldName = "Textfield-1";
            w.FieldLabel = "arbitray text - e.g. to help filling the field";
            w.FieldType = (int)PdfWidgetType.PDF_WIDGET_TYPE_TEXT;
            w.FillColor = new float[] { 1, 1, 0 };
            w.Rect = new Rect(50, 72, 400, 200);
            w.TextColor = new float[] { 0, 0, 1 };
            w.TextFont = "TiRo";
            w.TextFontSize = 11.0f;
            w.TextMaxLen = 50;
            w.FieldValue = "Times-Roman";
            page.AddWidget(w);
            Widget first = page.FirstWidget;

            Assert.Equal("Text", first.FieldTypeString);
            doc.Save(Out("Text.pdf"));
        }

        [Fact]
        public void Test4505()
        {
            // Copy field flags to Parent widget and all of its kids.
            Document doc = new Document(Doc("test_4505.pdf"));
            Page page = doc[0];

            Dictionary<int, int> text1_flags_before = new Dictionary<int, int>();
            Dictionary<int, int> text1_flags_after = new Dictionary<int, int>();

            // extract all widgets having the same field name
            foreach (Widget _w in page.GetWidgets())
            {
                if (_w.FieldName != "text_1")
                    continue;

                text1_flags_before[_w.Xref] = _w.FieldFlags;
            }
            Assert.Equal(new Dictionary<int, int>{{ 8, 1 },{ 10, 0 },{ 33, 0 }}, text1_flags_before);

            Widget w = page.LoadWidget(8);  // first of these widgets
            // give all connected widgets that field flags value
            w.Update(syncFlags: true);
            // confirm that all connected widgets have the same field flags
            foreach (Widget _w in page.GetWidgets())
            {
                if (_w.FieldName != "text_1")
                    continue;

                text1_flags_after[_w.Xref] = _w.FieldFlags;
            }
            Assert.Equal(new Dictionary<int, int> { { 8, 1 }, { 10, 1 }, { 33, 1 } }, text1_flags_after);
        }

        [Fact]
        public void ShouldNotThrowOnGetWidgets()
        {
            Document doc = new Document(Doc("test_widget_parse.pdf"));

            var widgets = doc[0].GetWidgets().ToList();
            Assert.Equal(85, widgets.Count);

            widgets = doc[1].GetWidgets().ToList();
            Assert.Equal(20, widgets.Count);
        }

        [Fact]
        public void Checkbox()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Widget w = new Widget(page);

            w.BorderStyle = "b";
            w.FieldName = "Button-1";
            w.FieldLabel = "a simple check box button";
            w.FieldType = (int)PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX;
            w.FillColor = new float[] { 0, 1, 1 };
            w.Rect = new Rect(50, 72, 100, 77);
            w.TextColor = new float[] { 0, 0, 1 };
            w.TextFont = "ZaDb";
            w.FieldValue = "true";
            page.AddWidget(w);

            Widget field = page.FirstWidget;
            Assert.Equal("CheckBox", field.FieldTypeString);

            w.FieldFlags |= (int)FormFlags.PDF_FIELD_IS_READ_ONLY;
            w.Update();
            doc.Save(Out("Checkbox.pdf"));
            //Assert.Pass();
        }

        [Fact]
        public void TestWidget()
        {
            string testFilePath = Path.GetFullPath(Doc("Widget.pdf"));
            Document doc = new Document(testFilePath);

            Page page = doc[0];
            Widget fWidget = page.FirstWidget;

            Assert.True(fWidget.FieldName == "partlyDetail");
            Assert.True(fWidget.FieldType == 7);
            Assert.True(fWidget.FieldValue == "");
            Assert.True(fWidget.FieldFlags == 8392704);
            Assert.True(fWidget.FieldLabel == "undefined");
            Assert.True(fWidget.TextFont == "SimSun");
            Assert.True(fWidget.TextFontSize == 12);
            
            page.Dispose();
            doc.Close();
        }
    }
}
