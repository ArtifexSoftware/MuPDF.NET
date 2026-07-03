using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestWidgets/</c>; outputs: <c>TestDocuments/_Output/TestWidgets/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestWidgets
    {
        private const string TestClassName = nameof(TestWidgets);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static List<float> BlueList => new(_Constants.blue);
        private static List<float> GoldList => new(_Constants.gold);
        private static readonly List<float> Gray = new() { 0.9f, 0.9f, 0.9f };
        private const float Fontsize = 11f;
        private static readonly Rect FieldRect = new(50, 72, 400, 200);

        [Fact]
        public void test_text()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var widget = new Widget
            {
                BorderColor = BlueList,
                BorderWidth = 0.3f,
                BorderStyle = "d",
                BorderDashes = new List<int> { 2, 3 },
                FieldName = "Textfield-1",
                FieldLabel = "arbitrary text - e.g. to help filling the field",
                FieldType = WidgetType.Text,
                FillColor = GoldList,
                Rect = FieldRect,
                TextColor = BlueList,
                TextFont = "TiRo",
                TextFontsize = Fontsize,
                TextMaxlen = 50,
                FieldValue = "Times-Roman",
            };
            page.AddWidget(widget);
            Assert.Equal("Text", page.FirstWidget.FieldTypeString);
            doc.Save(Out("test_text.pdf"));
        }

        [Fact]
        public void test_checkbox()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var widget = new Widget
            {
                BorderStyle = "b",
                FieldName = "Button-1",
                FieldLabel = "a simple check box button",
                FieldType = WidgetType.CheckBox,
                FillColor = GoldList,
                Rect = FieldRect,
                TextColor = BlueList,
                TextFont = "ZaDb",
            };
            widget.SetFieldValue(true);
            page.AddWidget(widget);
            Assert.Equal("CheckBox", page.FirstWidget.FieldTypeString);

            widget.FieldFlags |= mupdf.mupdf.PDF_FIELD_IS_READ_ONLY;
            widget.Update();

            string path = Out("test_checkbox.pdf");
            doc.Save(path);

            using var doc2 = new Document(path);
            var w = doc2[0].FirstWidget;
            Assert.NotNull(w);
            Assert.Equal(mupdf.mupdf.PDF_FIELD_IS_READ_ONLY, w.FieldFlags);
        }

        [Fact]
        public void test_listbox()
        {
            var choices = new List<string>
            {
                "Frankfurt", "Hamburg", "Stuttgart", "Hannover", "Berlin",
                "München", "Köln", "Potsdam",
            };
            using var doc = new Document();
            var page = doc.NewPage();
            var widget = new Widget
            {
                FieldName = "ListBox-1",
                FieldLabel = "is not a drop down: scroll with cursor in field",
                FieldType = WidgetType.ListBox,
                FieldFlags = mupdf.mupdf.PDF_CH_FIELD_IS_COMMIT_ON_SEL_CHANGE,
                FillColor = GoldList,
                ChoiceValues = choices,
                Rect = FieldRect,
                TextColor = BlueList,
                TextFontsize = Fontsize,
                FieldValue = choices[^1],
            };
            page.AddWidget(widget);
            Assert.Equal("ListBox", page.FirstWidget.FieldTypeString);
            doc.save(Out("test_listbox.pdf"));
        }

        [Fact]
        public void test_combobox()
        {
            var choices = new List<string>
            {
                "Spanien", "Frankreich", "Holland", "Dänemark", "Schweden", "Norwegen",
                "England", "Polen", "Russland", "Italien", "Portugal", "Griechenland",
            };
            using var doc = new Document();
            var page = doc.NewPage();
            var widget = new Widget
            {
                FieldName = "ComboBox-1",
                FieldLabel = "an editable combo box ...",
                FieldType = WidgetType.ComboBox,
                FieldFlags = mupdf.mupdf.PDF_CH_FIELD_IS_COMMIT_ON_SEL_CHANGE
                    | mupdf.mupdf.PDF_CH_FIELD_IS_EDIT,
                FillColor = GoldList,
                ChoiceValues = choices,
                Rect = FieldRect,
                TextColor = BlueList,
                TextFontsize = Fontsize,
                FieldValue = choices[^1],
            };
            page.AddWidget(widget);
            Assert.Equal("ComboBox", page.FirstWidget.FieldTypeString);
            doc.save(Out("test_combobox.pdf"));
        }

        [Fact]
        public void test_text2()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var widget = new Widget
            {
                FieldName = "textfield-2",
                FieldLabel = "multi-line text with tabs is also possible!",
                FieldFlags = mupdf.mupdf.PDF_TX_FIELD_IS_MULTILINE,
                FieldType = WidgetType.Text,
                FillColor = Gray,
                Rect = FieldRect,
                TextColor = BlueList,
                TextFont = "TiRo",
                TextFontsize = Fontsize,
                FieldValue = "This\n\tis\n\t\ta\n\t\t\tmulti-\n\t\tline\n\ttext.",
            };
            page.AddWidget(widget);
            Assert.Equal("Text", page.Widgets().First().FieldTypeString);
            doc.save(Out("test_text2.pdf"));
        }

        [Fact]
        public void test_2333()
        {
            using var doc = new Document(Doc("test-2333.pdf"));
            var page = doc[0];

            HashSet<string> Values()
            {
                return new HashSet<string>
                {
                    doc.XrefGetKey(635, "AS").value,
                    doc.XrefGetKey(636, "AS").value,
                    doc.XrefGetKey(637, "AS").value,
                    doc.XrefGetKey(638, "AS").value,
                    doc.XrefGetKey(127, "V").value,
                };
            }

            Widget w = null;
            for (int i = 0; i < 4; i++)
            {
                int xref = 635 + i;
                w = page.LoadWidget(xref);
                w.SetFieldValue(true);
                w.Update();
                Assert.Equal(new HashSet<string> { "/Off", $"{i}", $"/{i}" }, Values());
            }
            w!.SetFieldValue(false);
            w.Update();
            Assert.Equal(new HashSet<string> { "Off", "/Off" }, Values());
            doc.save(Out("test_2333.pdf"));
        }

        [Fact]
        public void test_2411()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var rect = new Rect(100, 100, 300, 200);
            var gold = Utils.GetColor("gold");

            var widget = new Widget
            {
                FieldFlags = mupdf.mupdf.PDF_CH_FIELD_IS_COMBO
                    | mupdf.mupdf.PDF_CH_FIELD_IS_EDIT
                    | mupdf.mupdf.PDF_CH_FIELD_IS_COMMIT_ON_SEL_CHANGE,
                FieldName = "ComboBox-1",
                FieldLabel = "an editable combo box ...",
                FieldType = WidgetType.ComboBox,
                FillColor = new List<float> { gold.r, gold.g, gold.b },
                Rect = rect,
            };
            widget.SetChoiceValues(new object[]
            {
                new[] { "Spain", "ES" },
                new[] { "Italy", "I" },
                "Portugal",
            });
            page.AddWidget(widget);

            doc.save(Out("test_2411.pdf"));
        }

        [Fact]
        public void test_2391()
        {
            string path = Doc("widgettest.pdf");

            using (var doc = new Document(path))
            {
                var page = doc[0];
                foreach (var field in page.Widgets(WidgetType.CheckBox))
                {
                    field.SetFieldValue(true);
                    field.Update();
                }
            }

            for (int i = 0; i < 5; i++)
            {
                byte[] pdfdata;
                using (var doc = new Document(path))
                {
                    pdfdata = doc.ToBytes();
                }
                using var doc2 = new Document(pdfdata);
                var page = doc2[0];
                foreach (var field in page.Widgets(WidgetType.CheckBox))
                {
                    Assert.Equal(field.OnState(), field.FieldValue);
                    field.Update();
                }
                doc2.save(Out($"test_2391_{i}.pdf"));
            }
        }

        [Fact]
        public void test_3216()
        {
            using var document = new Document(Doc("widgettest.pdf"));
            foreach (var page in document)
            {
                while (true)
                {
                    var w = page.FirstWidget;
                    if (w == null)
                        break;
                    page.DeleteWidget(w);
                }
            }
            document.save(Out("test_3216.pdf"));
        }

        [Fact]
        public void test_add_widget()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var w = new Widget
            {
                FieldType = WidgetType.Button,
                Rect = new Rect(5, 5, 20, 20),
                FieldFlags = mupdf.mupdf.PDF_BTN_FIELD_IS_PUSHBUTTON,
                FieldName = "button",
                FillColor = BlueList,
                Script = "app.alert('Hello, PDF!');",
            };
            page.AddWidget(w);
            doc.save(Out("test_add_widget.pdf"));
        }

        [Fact]
        public void test_interfield_calculation()
        {
            var r1 = new Rect(100, 100, 300, 120);
            var r2 = new Rect(100, 130, 300, 150);
            var r3 = new Rect(100, 180, 300, 200);

            using var doc = new Document();
            var pdf = doc.NativePdfDocument;
            var coName = mupdf.mupdf.pdf_new_name("CO");

            for (int i = 0; i < 3; i++)
            {
                var page = doc.NewPage();
                int pno = page.Number;

                var w1 = new Widget
                {
                    FieldName = $"NUM1{pno}",
                    Rect = r1,
                    FieldType = WidgetType.Text,
                    FieldValue = $"{i * 100 + 1}",
                    FieldFlags = 2,
                };
                page.AddWidget(w1);

                var w2 = new Widget
                {
                    FieldName = $"NUM2{pno}",
                    Rect = r2,
                    FieldType = WidgetType.Text,
                    FieldValue = "200",
                    FieldFlags = 2,
                };
                page.AddWidget(w2);

                var w3 = new Widget
                {
                    FieldName = $"RESULT{pno}",
                    Rect = r3,
                    FieldType = WidgetType.Text,
                    FieldValue = "Result?",
                    ScriptCalc = $"""AFSimple_Calculate("SUM", new Array("NUM1{pno}", "NUM2{pno}"));""",
                };
                page.AddWidget(w3);

                var acro = Helpers.PdfDictGetl(
                    mupdf.mupdf.pdf_trailer(pdf),
                    mupdf.mupdf.pdf_new_name("Root"),
                    mupdf.mupdf.pdf_new_name("AcroForm"));
                var co = mupdf.mupdf.pdf_dict_get(acro, coName);
                Assert.Equal(i + 1, mupdf.mupdf.pdf_array_len(co));

                int lastXref = page.Widgets().Last().Xref;
                int coXref = mupdf.mupdf.pdf_to_num(mupdf.mupdf.pdf_array_get(co, i));
                Assert.Equal(lastXref, coXref);
            }
            doc.save(Out("test_interfield_calculation.pdf"));
        }

        [Fact]
        public void test_3950()
        {
            var items = new List<string>();
            using (var document = new Document(Doc("test_3950.pdf")))
            {
                foreach (var page in document)
                {
                    foreach (var widget in page.Widgets())
                        items.Add(widget.FieldLabel);
                }
            }

            Assert.Equal(
                new[]
                {
                    "{{ named_insured }}",
                    "{{ policy_period_start_date }}",
                    "{{ policy_period_end_date }}",
                    "{{ insurance_line }}",
                },
                items);
        }

        [Fact]
        public void test_4004()
        {
            using var doc = new Document(Doc("test_4004.pdf"));
            var widgetsByName = GetWidgetsByName(doc);

            foreach (var kv in widgetsByName)
            {
                Console.WriteLine($"Widget Name: {kv.Key}");
                foreach (var entry in kv.Value)
                {
                    var widget = entry.widget;
                    Console.WriteLine($"  Page: {entry.pageNum + 1}, Type: {(int)widget.FieldType}, " + $"Value: {widget.FieldValue}, Rect: {widget.Rect}");
                }
            }

            var w = widgetsByName["Text1"][0];
            var field = w.widget;
            field.Value = "1234567890";
            try
            {
                field.Update();
            }
            catch (Exception e)
            {
                Assert.Equal("Annot is not bound to a page", e.Message);
            }

            doc.save(Out("test_4004.pdf"));
        }

        [Fact]
        public void test_4055()
        {
            using var doc = new Document(Doc("test-4055.pdf"));
            var page = doc[0];

            foreach (var w in page.Widgets(WidgetType.CheckBox))
            {
                Assert.NotEqual("Yes", w.OnState());
                Assert.Equal("Off", w.FieldValue);
                w.SetFieldValue(w.OnState());
                w.Update();
            }

            page = doc.ReloadPage(page);

            foreach (var w in page.Widgets(WidgetType.CheckBox))
            {
                Assert.Equal(w.OnState(), w.FieldValue);
                w.SetFieldValue(false);
                w.Update();
            }

            page = doc.ReloadPage(page);

            foreach (var w in page.Widgets(WidgetType.CheckBox))
            {
                Assert.Equal("Off", w.FieldValue);
                w.SetFieldValue(true);
                w.Update();
            }

            page = doc.ReloadPage(page);

            foreach (var w in page.Widgets(WidgetType.CheckBox))
            {
                Assert.Equal(w.OnState(), w.FieldValue);
                w.SetFieldValue("Off");
                w.Update();
                w.FieldValue = "Yes";
                w.Update();
            }

            page = doc.ReloadPage(page);

            foreach (var w in page.Widgets(WidgetType.CheckBox))
                Assert.Equal(w.OnState(), w.FieldValue);
            doc.save(Out("test_4055.pdf"));
        }

        private static Dictionary<string, List<(int pageNum, Widget widget)>> GetWidgetsByName(Document doc)
        {
            var widgetsByName = new Dictionary<string, List<(int, Widget)>>();
            for (int pageNum = 0; pageNum < doc.PageCount; pageNum++)
            {
                var page = doc.LoadPage(pageNum);
                foreach (var field in page.Widgets())
                {
                    if (!widgetsByName.TryGetValue(field.FieldName, out var list))
                    {
                        list = new List<(int, Widget)>();
                        widgetsByName[field.FieldName] = list;
                    }
                    list.Add((pageNum, field));
                }
            }
            return widgetsByName;
        }

        [Fact]
        public void test_4114()
        {
            var expectedValues = new List<string>
            {
                " - Select One - ", "  ", "Cincinnati, OH 45999", "Memphis, TN 37501",
                "Ogden, UT 84201", "Philadelphia, PA 19255",
            };
            var values = new List<List<string>>();
            string path = Doc("test_4114.pdf");
            using var document = new Document(path);
            for (int page_i = 0; page_i < document.PageCount; page_i++)
            {
                var page = document[page_i];
                foreach (var widget in page.Widgets())
                {
                    if (widget.FieldTypeString == "ComboBox")
                        values.Add(widget.ChoiceValues);
                    widget.Update();
                }
            }
            document.Save(Out("test_4114_out.pdf"));
            Assert.Equal(new[] { expectedValues, expectedValues }, values);
        }

        [Fact]
        public void test_4950()
        {
            using var document = new Document();
            var page = document.NewPage();
            page.SetRotation(90);

            var widget = new Widget
            {
                FieldName = "Signature",
                FieldType = WidgetType.Signature,
                Rect = new Rect(0, 0, 10, 10),
            };
            page.AddWidget(widget);
            document.XrefSetKey(page.FirstWidget.Xref, "Rect", "[0 0 0 0]");
            page = document.ReloadPage(page);

            page.RemoveRotation();
            document.Save(Out("test_4950_out.pdf"));
            Assert.Equal(0, page.Rotation);
        }

        [Fact]
        public void test_4965()
        {
            string path = Doc("test_4965.pdf");
            using (var document = new Document(path))
            {
                foreach (var page in document)
                {
                    Console.WriteLine($"test_4965(): page.Number={page.Number}");
                    // Iterate over all form fields (widgets) on the page
                    int widgetI = 0;
                    foreach (var field in page.Widgets())
                    {
                        // Access field properties
                        string name = field.FieldName;       // The internal name of the field
                        string value = field.FieldValue;     // The data currently in the field
                        var fType = field.FieldType;      // Integer representing the field type
                        Console.WriteLine($"     widgetI={widgetI}");
                        Console.WriteLine($"        name={name}");
                        Console.WriteLine($"        value={value}");
                        Console.WriteLine($"        fType={fType}");
                        widgetI++;
                    }
                }
            }
        }
    }
}
