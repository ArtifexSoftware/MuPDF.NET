using mupdf;
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
            MuPDFWidget w = new MuPDFWidget(page);

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
            MuPDFWidget first = page.FirstWidget;

            Assert.That(first.FieldTypeString, Is.EqualTo("Text"));
        }

        [Test]
        public void Checkbox()
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            MuPDFWidget w = new MuPDFWidget(page);

            w.BorderStyle = "b";
            w.FieldName = "Button-1";
            w.FieldLabel = "a simple check box button";
            w.FieldType = (int)PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX;
            w.FillColor = new float[] { 0, 1, 1 };
            w.Rect = new Rect(50, 72, 60, 77);
            w.TextColor = new float[] { 0, 0, 1 };
            w.TextFont = "ZaDb";
            w.FieldValue = "true";
            page.AddWidget(w);

            MuPDFWidget field = page.FirstWidget;
            Assert.That(field.FieldTypeString, Is.EqualTo("CheckBox"));

            w.FieldFlags |= (int)FormFlags.PDF_FIELD_IS_READ_ONLY;
            w.Update();
            Assert.Pass();
        }
    }
}
