using mupdf;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class WidgetTest
    {
        [Test]
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

            Assert.That(first.FieldTypeString, Is.EqualTo("Text"));
        }

        [Test]
        public void Text4505()
        {
            // Copy field flags to Parent widget and all of its kids.
            Document doc = new Document("../../../resources/test_4505.pdf");
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
            Assert.That(text1_flags_before, Is.EqualTo(new Dictionary<int, int>{{ 8, 1 },{ 10, 0 },{ 33, 0 }}));

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
            Assert.That(text1_flags_after, Is.EqualTo(new Dictionary<int, int> { { 8, 1 }, { 10, 1 }, { 33, 1 } }));
        }
        /*
        [Test]
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
            Assert.That(field.FieldTypeString, Is.EqualTo("CheckBox"));

            w.FieldFlags |= (int)FormFlags.PDF_FIELD_IS_READ_ONLY;
            w.Update();
            doc.Save("output.pdf");
            //Assert.Pass();
        }
        */
    }
}
