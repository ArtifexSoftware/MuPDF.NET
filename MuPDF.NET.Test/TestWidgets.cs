// Test PDF field (widget) insertion.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using MuPDF.NET;
namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestWidgets/</c>; outputs: <c>TestDocuments/_Output/TestWidgets/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestWidgets
    {
        private const string TestClassName = nameof(TestWidgets);

        // scriptdir / resources
        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);


        private static string Filename => Doc("widgettest.pdf");
        private static string File2333 => Doc("test-2333.pdf");
        private static string File4055 => Doc("test-4055.pdf");

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
            var widget = new Widget(); // create a widget object
            widget.BorderColor = BlueList; // border color
            widget.BorderWidth = 0.3f; // border width
            widget.BorderStyle = "d";
            widget.BorderDashes = new List<int> { 2, 3 };
            widget.FieldName = "Textfield-1"; // field name
            widget.FieldLabel = "arbitrary text - e.g. to help filling the field";
            widget.FieldType = WidgetType.Text; // field type
            widget.FillColor = GoldList; // field background
            widget.Rect = FieldRect; // set field rectangle
            widget.TextColor = BlueList; // text color
            widget.TextFont = "TiRo"; // use font Times-Roman
            widget.TextFontsize = Fontsize; // set fontsize
            widget.TextMaxlen = 50; // restrict number of characters
            widget.FieldValue = "Times-Roman";
            page.AddWidget(widget); // create the field
            var field = page.FirstWidget;
            Assert.Equal("Text", field.FieldTypeString);
        }

        [Fact]
        public void test_checkbox()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var widget = new Widget();
            widget.BorderStyle = "b";
            widget.FieldName = "Button-1";
            widget.FieldLabel = "a simple check box button";
            widget.FieldType = WidgetType.CheckBox;
            widget.FillColor = GoldList;
            widget.Rect = FieldRect;
            widget.TextColor = BlueList;
            widget.TextFont = "ZaDb";
            widget.SetFieldValue(true);
            // Check #2350 - setting checkbox to readonly.
            widget.FieldFlags |= mupdf.mupdf.PDF_FIELD_IS_READ_ONLY;
            page.AddWidget(widget); // create the field
            var field = page.FirstWidget;
            Assert.Equal("CheckBox", field.FieldTypeString);

            string path = Out("test_checkbox.pdf");
            doc.Save(path);

            using var doc2 = new Document(path);
            page = doc2[0];
            widget = page.FirstWidget;
            Assert.NotNull(widget);
            Assert.Equal(mupdf.mupdf.PDF_FIELD_IS_READ_ONLY, widget.FieldFlags);
        }

        [Fact]
        public void test_listbox()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var widget = new Widget();
            widget.FieldName = "ListBox-1";
            widget.FieldLabel = "is not a drop down: scroll with cursor in field";
            widget.FieldType = WidgetType.ListBox;
            widget.FieldFlags = mupdf.mupdf.PDF_CH_FIELD_IS_COMMIT_ON_SEL_CHANGE;
            widget.FillColor = GoldList;
            widget.ChoiceValues = new List<string>
            {
                "Frankfurt", "Hamburg", "Stuttgart", "Hannover", "Berlin",
                "München", "Köln", "Potsdam",
            };
            widget.Rect = FieldRect;
            widget.TextColor = BlueList;
            widget.TextFontsize = Fontsize;
            widget.FieldValue = widget.ChoiceValues[^1];
            Console.WriteLine($"About to add '{widget.FieldName}'");
            page.AddWidget(widget); // create the field
            var field = page.FirstWidget;
            Assert.Equal("ListBox", field.FieldTypeString);
        }

        [Fact]
        public void test_combobox()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var widget = new Widget();
            widget.FieldName = "ComboBox-1";
            widget.FieldLabel = "an editable combo box ...";
            widget.FieldType = WidgetType.ComboBox;
            widget.FieldFlags = mupdf.mupdf.PDF_CH_FIELD_IS_COMMIT_ON_SEL_CHANGE
                | mupdf.mupdf.PDF_CH_FIELD_IS_EDIT;
            widget.FillColor = GoldList;
            widget.ChoiceValues = new List<string>
            {
                "Spanien", "Frankreich", "Holland", "Dänemark", "Schweden", "Norwegen",
                "England", "Polen", "Russland", "Italien", "Portugal", "Griechenland",
            };
            widget.Rect = FieldRect;
            widget.TextColor = BlueList;
            widget.TextFontsize = Fontsize;
            widget.FieldValue = widget.ChoiceValues[^1];
            page.AddWidget(widget); // create the field
            var field = page.FirstWidget;
            Assert.Equal("ComboBox", field.FieldTypeString);
        }

        [Fact]
        public void test_text2()
        {
            using var doc = new Document();
            doc.NewPage();

            var page = doc[0];
            var widget = new Widget();
            widget.FieldName = "textfield-2";
            widget.FieldLabel = "multi-line text with tabs is also possible!";
            widget.FieldFlags = mupdf.mupdf.PDF_TX_FIELD_IS_MULTILINE;
            widget.FieldType = WidgetType.Text;
            widget.FillColor = Gray;
            widget.Rect = FieldRect;
            widget.TextColor = BlueList;
            widget.TextFont = "TiRo";
            widget.TextFontsize = Fontsize;
            widget.FieldValue = "This\n\tis\n\t\ta\n\t\t\tmulti-\n\t\tline\n\ttext.";
            page.AddWidget(widget); // create the field
            var widgets = page.Widgets().ToList();
            var field = widgets[0];
            Assert.Equal("Text", field.FieldTypeString);
        }

        [Fact]
        public void test_2333()
        {
            using var doc = new Document(File2333);
            var page = doc[0];

            HashSet<string> Values() => new()
            {
                doc.XrefGetKey(635, "AS").value,
                doc.XrefGetKey(636, "AS").value,
                doc.XrefGetKey(637, "AS").value,
                doc.XrefGetKey(638, "AS").value,
                doc.XrefGetKey(127, "V").value,
            };

            Widget w = null;
            int[] xrefs = { 635, 636, 637, 638 };
            for (int i = 0; i < xrefs.Length; i++)
            {
                int xref = xrefs[i];
                w = page.LoadWidget(xref);
                w.SetFieldValue(true);
                w.Update();
                Assert.Equal(new HashSet<string> { "/Off", $"/{i}" }, Values());
            }
            w.SetFieldValue(false);
            w.Update();
            Assert.Equal(new HashSet<string> { "/Off" }, Values());
        }

        [Fact]
        public void test_2411()
        {
            // Add combobox values in different formats.
            using var doc = new Document();
            var page = doc.NewPage();
            var rect = new Rect(100, 100, 300, 200);
            var gold = Utils.GetColor("gold"); // pdfcolor["gold"]

            var widget = new Widget();
            widget.FieldFlags = mupdf.mupdf.PDF_CH_FIELD_IS_COMBO
                | mupdf.mupdf.PDF_CH_FIELD_IS_EDIT
                | mupdf.mupdf.PDF_CH_FIELD_IS_COMMIT_ON_SEL_CHANGE;
            widget.FieldName = "ComboBox-1";
            widget.FieldLabel = "an editable combo box ...";
            widget.FieldType = WidgetType.ComboBox;
            widget.FillColor = new List<float> { gold.r, gold.g, gold.b };
            widget.Rect = rect;
            widget.SetChoiceValues(new object[]
            {
                new List<string> { "Spain", "ES" }, // double value as list
                new object[] { "Italy", "I" }, // double value as tuple
                "Portugal", // single value
            });
            page.AddWidget(widget);
        }

        [Fact]
        public void test_2391()
        {
            var doc = new Document(Filename);
            try
            {
                var page = doc[0];
                // its work when we update first-time
                foreach (var field in page.Widgets(WidgetType.CheckBox))
                {
                    field.SetFieldValue(true);
                    field.Update();
                }

                for (int i = 0; i < 5; i++)
                {
                    byte[] pdfdata = doc.ToBytes();
                    doc.Close();
                    doc.Dispose();
                    doc = new Document(pdfdata, "pdf");
                    page = doc[0];
                    foreach (var field in page.Widgets(WidgetType.CheckBox))
                    {
                        Assert.Equal(field.OnState(), field.FieldValue);
                        string fieldFieldValue = field.OnState();
                        _ = fieldFieldValue;
                        field.Update();
                    }
                }
            }
            finally
            {
                doc?.Dispose();
            }
        }

        [Fact]
        public void test_3216()
        {
            using var document = new Document(Filename);
            foreach (var page in document)
            {
                while (true)
                {
                    var w = page.FirstWidget;
                    Console.WriteLine($"w={w}");
                    if (w == null)
                        break;
                    page.DeleteWidget(w);
                }
            }
        }

        [Fact]
        public void test_add_widget()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var w = new Widget();
            w.FieldType = WidgetType.Button;
            w.Rect = new Rect(5, 5, 20, 20);
            w.FieldFlags = mupdf.mupdf.PDF_BTN_FIELD_IS_PUSHBUTTON;
            w.FieldName = "button";
            w.FillColor = new List<float> { 0, 0, 1 };
            w.Script = "app.alert('Hello, PDF!');";
            page.AddWidget(w);
        }

        /// <summary>
        /// Confirm correct working of interfield calculations.
        /// We are going to create three pages with a computed result field each.
        /// Tests the fix for https://github.com///issues/3402.
        /// </summary>
        [Fact]
        public void test_interfield_calculation()
        {
            // Field bboxes (same on each page)
            var r1 = new Rect(100, 100, 300, 120);
            var r2 = new Rect(100, 130, 300, 150);
            var r3 = new Rect(100, 180, 300, 200);

            using var doc = new Document();
            var pdf = doc.NativePdfDocument; // we need underlying PDF document

            using var coName = mupdf.mupdf.pdf_new_name("CO"); // = PDF_NAME(CO)
            mupdf.PdfObj co = null;
            for (int i = 0; i < 3; i++)
            {
                var page = doc.NewPage();
                var w = new Widget();
                w.FieldName = $"NUM1{page.Number}";
                w.Rect = r1;
                w.FieldType = WidgetType.Text;
                w.FieldValue = $"{i * 100 + 1}";
                w.FieldFlags = 2;
                page.AddWidget(w);

                w = new Widget();
                w.FieldName = $"NUM2{page.Number}";
                w.Rect = r2;
                w.FieldType = WidgetType.Text;
                w.FieldValue = "200";
                w.FieldFlags = 2;
                page.AddWidget(w);

                w = new Widget();
                w.FieldName = $"RESULT{page.Number}";
                w.Rect = r3;
                w.FieldType = WidgetType.Text;
                w.FieldValue = "Result?";
                // Script that adds previous two fields.
                w.ScriptCalc = $"AFSimple_Calculate(\"SUM\",\n        new Array(\"NUM1{page.Number}\", \"NUM2{page.Number}\"));";
                page.AddWidget(w);

                // Access the inter-field calculation array. It contains a reference to
                // all fields which have a JavaScript stored in their "script_calc"
                // Every iteration adds another such field, so this array's length must
                // always equal the loop index.
                if (i == 0) // only need to execute this on first time through
                {
                    co = Helpers.PdfDictGetl(
                        mupdf.mupdf.pdf_trailer(pdf),
                        mupdf.mupdf.pdf_new_name("Root"),
                        mupdf.mupdf.pdf_new_name("AcroForm"),
                        coName);
                }

                // we confirm CO is an array of foreseeable length
                Assert.Equal(i + 1, mupdf.mupdf.pdf_array_len(co));

                // last item in the /CO array is that of the last widget's parent
                int lastXref = mupdf.mupdf.pdf_to_num(co.pdf_array_get(i));
                using var obj = mupdf.mupdf.pdf_load_object(pdf, w.Xref);
                using var parentKey = mupdf.mupdf.pdf_new_name("Parent");
                int parentXref = obj.pdf_dict_get(parentKey).pdf_to_num();
                Assert.Equal(parentXref, lastXref);
            }
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
                    {
                        items.Add(widget.FieldLabel);
                        Console.WriteLine($"test_3950(): widget.FieldLabel={widget.FieldLabel}.");
                    }
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
            Dictionary<string, List<(int pageNum, Widget widget)>> GetWidgetsByName(Document document)
            {
                // Extracts and returns a dictionary of widgets indexed by their names.
                var widgetsByName = new Dictionary<string, List<(int, Widget)>>();
                for (int pageNum = 0; pageNum < document.PageCount; pageNum++)
                {
                    var page = document.LoadPage(pageNum);
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

            // Open document and get widgets
            using var doc = new Document(Doc("test_4004.pdf"));
            var widgetsByName = GetWidgetsByName(doc);

            // Print widget information
            foreach (var kv in widgetsByName)
            {
                Console.WriteLine($"Widget Name: {kv.Key}");
                foreach (var entry in kv.Value)
                {
                    var widget = entry.widget;
                    Console.WriteLine($"  Page: {entry.pageNum + 1}, Type: {(int)widget.FieldType}, Value: {widget.FieldValue}, Rect: {widget.Rect}");
                }
            }

            // Attempt to update field value
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
        }

        [Fact]
        public void test_4055()
        {
            // Check correct setting of CheckBox "Yes" values.
            // this PDF has digits as "Yes" values.
            using var doc = new Document(File4055);
            var page = doc[0];

            // Round 1: confirm all check boxes are off
            foreach (var w in page.Widgets(WidgetType.CheckBox))
            {
                // check that this file doesn't use the "Yes" standard
                Assert.NotEqual("Yes", w.OnState());
                Assert.Equal("Off", w.FieldValue); // all check boxes are off
                w.FieldValue = w.OnState();
                w.Update();
            }

            page = doc.ReloadPage(page); // reload page to make sure we start fresh

            // Round 2: confirm that fields contain the PDF's own on values
            foreach (var w in page.Widgets(WidgetType.CheckBox))
            {
                // confirm each value coincides with the "Yes" value
                Assert.Equal(w.OnState(), w.FieldValue);
                w.SetFieldValue(false); // switch to "Off" using False
                w.Update();
            }

            page = doc.ReloadPage(page);

            foreach (var w in page.Widgets(WidgetType.CheckBox))
            {
                Assert.Equal("Off", w.FieldValue);
                w.SetFieldValue(true); // use True for the next round
                w.Update();
            }

            page = doc.ReloadPage(page);

            foreach (var w in page.Widgets(WidgetType.CheckBox))
            {
                Assert.Equal(w.OnState(), w.FieldValue);
                w.SetFieldValue("Off"); // set off again
                w.Update();
                w.FieldValue = "Yes";
                w.Update();
            }

            page = doc.ReloadPage(page);

            // Round 5: final check: setting to "Yes" also does work
            foreach (var w in page.Widgets(WidgetType.CheckBox))
                Assert.Equal(w.OnState(), w.FieldValue);
        }

        [Fact]
        public void test_4965()
        {
            using var document = new Document(Doc("test_4965.pdf"));
            foreach (var page in document)
            {
                Console.WriteLine($"test_4965(): page.Number={page.Number}");
                // Iterate over all form fields (widgets) on the page
                int widgetI = 0;
                foreach (var field in page.Widgets())
                {
                    // Access field properties
                    string name = field.FieldName; // The internal name of the field
                    string value = field.FieldValue; // The data currently in the field
                    int fType = field.FieldType; // Integer representing the field type
                    Console.WriteLine($"     widget_i={widgetI}");
                    Console.WriteLine($"        name={name}");
                    Console.WriteLine($"        value={value}");
                    Console.WriteLine($"        f_type={fType}");
                    widgetI++;
                }
            }
        }

        [Fact]
        public void test_4114()
        {
            Console.WriteLine();
            string path = Doc("test_4114.pdf");
            string pathOut = Out("test_4114_out.pdf");
            var expectedValues = new List<string>
            {
                " - Select One - ", "  ", "Cincinnati, OH 45999", "Memphis, TN 37501",
                "Ogden, UT 84201", "Philadelphia, PA 19255",
            };
            var expectedValues2 = new[] { expectedValues, expectedValues };
            var values = new List<List<string>>();
            using (var document = new Document(path))
            {
                int pageI = 0;
                foreach (var page in document)
                {
                    foreach (var widget in page.Widgets())
                    {
                        if (widget.FieldTypeString == "ComboBox")
                        {
                            Console.WriteLine($"test_4114(): page_i={pageI} widget.ChoiceValues={string.Join(",", widget.ChoiceValues)}");
                            values.Add(widget.ChoiceValues);
                        }
                        widget.Update();
                    }
                    pageI++;
                }
                document.Save(pathOut);
            }
            Assert.Equal(expectedValues2, values);
        }

        [Fact]
        public void test_4950()
        {
            using var document = new Document();
            var page = document.NewPage();
            page.SetRotation(90);

            // Simulate an existing invisible signature field with a zero-dimension
            // rectangle. Creating such a widget through add_widget is rejected by
            // object directly.
            var widget = new Widget();
            widget.FieldName = "Signature";
            widget.FieldType = WidgetType.Signature;
            widget.Rect = new Rect(0, 0, 10, 10);
            page.AddWidget(widget);
            document.XrefSetKey(page.FirstWidget.Xref, "Rect", "[0 0 0 0]");
            page = document.ReloadPage(page);

            page.RemoveRotation();
            Assert.Equal(0, page.Rotation);
        }

        [Fact]
        public void test_hierarchy()
        {
            // Create a new PDF document
            using var doc = new Document();
            var pdfdoc = doc.NativePdfDocument;

            void ChainUpwards(Widget w)
            {
                // Check the names climbing up the hierarchy.
                int xref = w.Xref;
                string[] parts = w.FieldName.Split('.');
                var obj = mupdf.mupdf.pdf_load_object(pdfdoc, xref); // the widget object
                using (var tKey = mupdf.mupdf.pdf_new_name("T"))
                    Assert.True(obj.pdf_dict_get(tKey).pdf_is_null() != 0);
                var items = new List<string>();
                while (true)
                {
                    using var parentKey = mupdf.mupdf.pdf_new_name("Parent");
                    var parent = obj.pdf_dict_get(parentKey);
                    obj.Dispose();
                    obj = parent;
                    if (obj == null || obj.m_internal == null || obj.pdf_is_null() != 0)
                        break;
                    using var tKey = mupdf.mupdf.pdf_new_name("T");
                    string pdfName = obj.pdf_dict_get(tKey).pdf_to_text_string();
                    items.Insert(0, pdfName);
                }
                obj?.Dispose();
                Assert.True(
                    items.SequenceEqual(parts),
                    $"\nitems={string.Join(",", items)}\nparts={string.Join(",", parts)}");
            }

            // Add a page to the document
            var page = doc.NewPage();
            var r = new Rect(100, 100, 200, 120);
            var w = new Widget();
            w.FieldName = "Person.Name.First";
            w.FieldType = WidgetType.Text;
            w.Rect = r;
            w.FieldValue = "John";
            w.BorderWidth = 1;
            page.AddWidget(w);
            r += new Rect(0, 30, 0, 30);

            w = new Widget();
            w.FieldName = "Person.Name.Last";
            w.FieldType = WidgetType.Text;
            w.Rect = r;
            w.FieldValue = "Doe";
            w.BorderWidth = 1;
            page.AddWidget(w);

            var fieldNames = new[] { "Person.Name.First", "Person.Name.Last" };
            using var rootName = mupdf.mupdf.pdf_new_name("Root");
            using var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdfdoc), rootName);
            using var acroName = mupdf.mupdf.pdf_new_name("AcroForm");
            using var acro = root.pdf_dict_get(acroName);
            using var fieldsName = mupdf.mupdf.pdf_new_name("Fields");
            using var fields = acro.pdf_dict_get(fieldsName);
            Assert.True(fields.pdf_array_len() == 1, "must have exactly 1 root field");
            int i = 0;
            foreach (var widget in page.Widgets())
            {
                string fieldName = widget.FieldName; // the field name is a concatenation
                Assert.Equal(fieldNames[i], fieldName);
                ChainUpwards(widget); // confirm the hierarchy is correct
                i++;
            }
        }

        [Fact]
        public void test_5101()
        {
            Console.WriteLine();
            string path = Doc("test_5101.pdf");
            Tools.ResetMupdfWarnings();
            using var document = new Document(path);
            Console.WriteLine($"len(document)={document.PageCount}");
            var version = Constants.MupdfVersion;
            bool repairedParents = version.Major > 1
                || (version.Major == 1 && (version.Minor > 28 || (version.Minor == 28 && version.Patch >= 5)));
            if (repairedParents)
            {
                var page = document[0];
                _ = page;
                string wt = Tools.MupdfWarnings();
                Console.WriteLine($"wt={wt}");
                Assert.Equal(
                    "cycle in parent chain\nfixed bad Parent in AcroForm tree\n... repeated 2 times...",
                    wt);
                using var document2 = new Document();
                document2.InsertPdf(document, annots: false, widgets: false, links: true);
            }
            else
            {
                Exception caught = null;
                try
                {
                    var page = document[0];
                    _ = page;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Received expected exception: {e}");
                    caught = e;
                    Assert.Contains("cycle in resources", e.ToString());
                }
                if (caught == null)
                    Assert.Fail("test_5101(): Expected exception from document[0].");

                using var document2 = new Document();
                caught = null;
                try
                {
                    document2.InsertPdf(document, annots: false, widgets: false, links: true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Received expected exception: {e}");
                    caught = e;
                    Assert.Contains("cycle in resources", e.ToString());
                }
                if (caught == null)
                    Assert.Fail("test_5101(): Expected exception from document2.insert_pdf.");
            }
        }

        /// <summary>Confirm: Deleting all widgets converts to a Non-Form-PDF.</summary>
        [Fact]
        public void test_delete_all()
        {
            string path = Doc("test_hierarchy.pdf");
            using var doc = new Document(path);
            Assert.True(doc.IsFormPdf);
            int widgets = 0;
            foreach (var page in doc)
            {
                var w = page.FirstWidget;
                while (w != null)
                {
                    widgets += 1;
                    w = page.DeleteWidget(w);
                }
            }
            Console.WriteLine($"Deleted {widgets} widgets");
            Assert.False(doc.IsFormPdf);
        }

        [Fact]
        public void test_3478()
        {
            Console.WriteLine();
            Console.WriteLine($"test_3478(): version={Constants.Version}");
            string path = Doc("test_3478.pdf");
            string pathOut = Out("test_3478_out.pdf");
            string pathOut2 = Out("test_3478_out2.pdf");

            using (var document = new Document(path))
            {
                Console.WriteLine("test_3478(): Root/AcroForm/Fields/[]/T:");
                var acroNames = GetAcroNames(document);
                foreach (string acroName in acroNames)
                {
                    if (acroName.StartsWith("sig") || acroName.StartsWith("init"))
                        Console.WriteLine($"test_3478():     {acroName}");
                }

                Console.WriteLine("test_3478(): Page widgets:");
                foreach (var page in document)
                {
                    // iterate the fields on this page.
                    var widget = page.FirstWidget;
                    while (widget != null)
                    {
                        string name = widget.FieldName;
                        // If it's a signature, remove it.
                        if (name.StartsWith("sig") || name.StartsWith("init"))
                        {
                            Console.WriteLine($"test_3478():     Calling page.delete_widget() for name={name}.");
                            widget = page.DeleteWidget(widget);
                        }
                        else
                        {
                            widget = widget.Next;
                        }
                    }
                }

                document.Save(pathOut2);
                document.EzSave(pathOut);
            }

            // DeleteWidget can leave a cyclic reference; collect so the file handle
            // is released before later assertions.
            GC.Collect();

            // In the ez_save'd document:

            // (This has always been ok.)
            // (This appears to have been fixed in 1.25.3.)

            Console.WriteLine("test_3478(): looking at ez_save document.");
            int numStillPresent = 0;
            int numStillPresentAcro = 0;

            using (var document = new Document(pathOut))
            {
                Console.WriteLine("test_3478(): Page widgets:");
                int pageI = 0;
                foreach (var page in document)
                {
                    int widgetI = 0;
                    foreach (var widget in page.Widgets())
                    {
                        string name = widget.FieldName;
                        if (name.StartsWith("sig") || name.StartsWith("init"))
                        {
                            Console.WriteLine($"test_3478():     page_i={pageI} widget_i={widgetI}: {name}");
                            numStillPresent += 1;
                        }
                        widgetI++;
                    }
                    pageI++;
                }
                Console.WriteLine($"test_3478(): num_still_present={numStillPresent}");

                var acroNames = GetAcroNames(document);
                int i = 0;
                foreach (string acroName in acroNames)
                {
                    if (acroName.StartsWith("sig") || acroName.StartsWith("init"))
                    {
                        Console.WriteLine($"test_3478():     {i}: {acroName}");
                        numStillPresentAcro += 1;
                    }
                    i++;
                }
                Console.WriteLine($"test_3478(): num_still_present_acro={numStillPresentAcro}");
            }

            Assert.Equal(0, numStillPresent);
            Assert.Equal(0, numStillPresentAcro);
        }

        /// <summary>Let 'insert_pdf' survive broken parent /Kids array.</summary>
        [Fact]
        public void test_5154()
        {
            using var doc = new Document();
            var pdoc = doc.NativePdfDocument;
            var page = doc.NewPage();
            var w = new Widget();
            w.FieldType = WidgetType.Text;
            w.FieldName = "child";
            w.FieldValue = "child";
            w.Rect = new Rect(50, 50, 200, 80);
            page.AddWidget(w);
            int wx = page.FirstWidget.Xref;

            // empty / invalidate the parent's /Kids array
            using var widgetObj = mupdf.mupdf.pdf_load_object(pdoc, wx);
            using var parentKey = mupdf.mupdf.pdf_new_name("Parent");
            using var parent = widgetObj.pdf_dict_get(parentKey);
            using var kidsKey = mupdf.mupdf.pdf_new_name("Kids");
            using var kids = parent.pdf_dict_get(kidsKey);
            kids.pdf_array_delete(0); // delete pointer to 'child'
            using var src = new Document(doc.ToBytes(), "pdf");
            using var output = new Document();
            output.InsertPdf(src, widgets: true); // ValueError: 9 is not in list
        }

        [Fact]
        public void test_5055()
        {
            string path = Doc("test_5055.pdf");
            using var src = new Document(path);
            using var tar = new Document();
            tar.InsertPdf(src);
            var ptar = tar.NativePdfDocument;
            using var fields = Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(ptar),
                "Root",
                "AcroForm",
                "Fields");
            Assert.Equal(1, fields.pdf_array_len());
            using var field = fields.pdf_array_get(0);
            using var kidsKey = mupdf.mupdf.pdf_new_name("Kids");
            using var kids = field.pdf_dict_get(kidsKey);
            var kidXrefs = new HashSet<int>();
            int kidCount = kids.pdf_array_len();
            for (int i = 0; i < kidCount; i++)
            {
                using var kid = kids.pdf_array_get(i);
                kidXrefs.Add(kid.pdf_to_num());
            }
            int fieldXref = field.pdf_to_num();
            var page = tar[0];
            var annotXrefs = new HashSet<int>(page.AnnotXrefs().Select(x => x.xref));
            Assert.True(
                kidXrefs.SetEquals(annotXrefs),
                $"kid_xrefs: {string.Join(",", kidXrefs)}, annot_xrefs: {string.Join(",", annotXrefs)}");
            foreach (var widget in page.Widgets())
            {
                using var widgetObj = mupdf.mupdf.pdf_load_object(ptar, widget.Xref);
                using var parentKey = mupdf.mupdf.pdf_new_name("Parent");
                using var parent = widgetObj.pdf_dict_get(parentKey);
                Assert.Equal(fieldXref, parent.pdf_to_num());
            }
        }

        [Fact]
        public void test_5165()
        {
            using var doc = new Document();
            var pdoc = doc.NativePdfDocument;
            var page = doc.NewPage();

            var xrefs = new List<int>();
            var rbParent = new HashSet<int>();

            for (int i = 0; i < 3; i++)
            {
                var w = new Widget();
                w.FieldType = WidgetType.RadioButton;
                w.FieldName = "group"; // same full name for both buttons
                w.Rect = new Rect(50, 50 + 30 * i, 70, 70 + 30 * i);
                w.FieldValue = i == 0 ? "Male" : "Female";
                page.AddWidget(w);
                xrefs.Add(w.Xref);
            }

            foreach (int xref in xrefs)
            {
                var w = page.LoadWidget(xref);
                rbParent.Add(w.RbParent);
            }

            // Assert all widgets have the same parent
            Assert.Single(rbParent);

            using var parent = mupdf.mupdf.pdf_load_object(pdoc, rbParent.Single());
            using var vKey = mupdf.mupdf.pdf_new_name("V");
            Assert.Equal("Male", parent.pdf_dict_get(vKey).pdf_to_name());

            var fieldValues = new List<string>();
            foreach (var w in page.Widgets())
                fieldValues.Add(w.FieldValue);
            Assert.Equal(new[] { "Male", "Off", "Off" }, fieldValues);

            // Set one of the widgets with duplicate values ("Female") to true
            var w3 = page.LoadWidget(xrefs[^1]);
            w3.FieldValue = "Female";
            w3.Update();

            // Assert that the parent shows that value
            Assert.Equal("Female", parent.pdf_dict_get(vKey).pdf_to_name());

            // Assert that all widgets with that value are updated accordingly
            fieldValues = new List<string>();
            foreach (var w in page.Widgets())
                fieldValues.Add(w.FieldValue);
            Assert.Equal(new[] { "Off", "Female", "Female" }, fieldValues);
        }

        private static List<string> GetAcroNames(Document document)
        {
            var ret = new List<string>();
            var obj = Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(document.NativePdfDocument),
                "Root",
                "AcroForm",
                "Fields");
            Assert.True(mupdf.mupdf.pdf_is_array(obj) != 0);
            int length = mupdf.mupdf.pdf_array_len(obj);
            for (int i = 0; i < length; i++)
            {
                using var field = mupdf.mupdf.pdf_array_get(obj, i);
                Assert.True(mupdf.mupdf.pdf_is_dict(field) != 0);
                using var t = field.pdf_dict_getp("T");
                ret.Add(t.pdf_to_text_string());
            }
            return ret;
        }
    }
}
