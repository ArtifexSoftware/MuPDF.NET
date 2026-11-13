// Synthetic form-field reproducers for Julian's September 22 test wheels.
// Assertions describe the desired result, so affected builds should FAIL.
// The supplied two-page PDF predates each test and has one shared text field.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestWidgetReproducers/</c>; outputs: <c>TestDocuments/_Output/TestWidgetReproducers/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestWidgetReproducers
    {
        private const string TestClassName = nameof(TestWidgetReproducers);

        private static readonly Regex XrefRef = new Regex(@"(\d+)\s+\d+\s+R", RegexOptions.Compiled);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);


        private static string SharedPdf => Doc("shared-text-field.pdf");

        private static string Sanitize(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            var chars = value.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray();
            return new string(chars);
        }


        private static Page Add(
            Page page,
            string name,
            object value,
            WidgetType kind = WidgetType.Text,
            float y = 50)
        {
            var widget = new Widget
            {
                FieldName = name,
                FieldType = kind,
                Rect = new Rect(50, y, 270, y + 30),
                TextFontsize = 12,
            };
            widget.SetFieldValue(value);
            page.AddWidget(widget);
            // return page.parent.reload_page(page)
            return page.Parent.ReloadPage(page);
        }


        private static (string type, string value) InheritedKey(Document doc, int xref, string key)
        {
            var seen = new HashSet<int>();
            while (!seen.Contains(xref))
            {
                seen.Add(xref);
                var value = doc.XrefGetKey(xref, key);
                if (value.type != "null")
                    return value;
                var parent = doc.XrefGetKey(xref, "Parent");
                if (parent.type != "xref")
                    return value;
                xref = int.Parse(parent.value.Split(' ')[0]);
            }
            throw new InvalidOperationException("Cycle in field parent chain");
        }


        private static Dictionary<string, (string type, string value)> FieldValues(Document doc)
        {
            var result = new Dictionary<string, (string type, string value)>();
            var visited = new HashSet<int>();

            void Walk(string array, string prefix = "")
            {
                foreach (Match match in XrefRef.Matches(array))
                {
                    int xref = int.Parse(match.Groups[1].Value);
                    Assert.True(!visited.Contains(xref), "Repeated or cyclic field-tree reference");
                    visited.Add(xref);
                    var (kind, partialName) = doc.XrefGetKey(xref, "T");
                    string name = prefix;
                    if (kind == "string")
                    {
                        name = prefix.Length > 0 ? prefix + "." + partialName : partialName;
                        // Deletion must remove the field, not just clear its value.
                        result[name] = doc.XrefGetKey(xref, "V");
                    }
                    var (kidsKind, kids) = doc.XrefGetKey(xref, "Kids");
                    if (kidsKind == "array")
                        Walk(kids, name);
                }
            }

            var (fieldsKind, fields) = doc.XrefGetKey(doc.PdfCatalog, "AcroForm/Fields");
            if (fieldsKind != "null")
            {
                Assert.Equal("array", fieldsKind);
                Walk(fields);
            }
            return result;
        }

        // DeleteWidget requires Widget.BoundAnnot. AddWidget sets it; FirstWidget does not.
        private static Widget BindForDelete(Page page, Widget widget)
        {
            if (widget != null && widget.BoundAnnot == null && widget.NativeWidget?.m_internal != null)
                widget.BindAnnot(widget.NativeWidget, page, new Annot(widget.NativeWidget, page));
            return widget;
        }


        private static void CreateRadioGroup(string path, int garbage)
        {
            using var doc = new Document();
            var page = Add(doc.NewPage(), "shipping", "Standard", WidgetType.RadioButton);
            Add(page, "shipping", "Express", WidgetType.RadioButton, y: 100);
            doc.Save(path, garbage: garbage);
        }

        /// <summary>Non-ASCII initial values must survive save/close/reopen unchanged.</summary>
        [Theory]
        [InlineData(0, "Alice")]
        [InlineData(0, "café")]
        [InlineData(0, "김민수 café")]
        [InlineData(4, "Alice")]
        [InlineData(4, "café")]
        [InlineData(4, "김민수 café")]
        public void test_text_creation_preserves_value(int garbage, string value)
        {
            string path = Out($"text-created-g{garbage}-{Sanitize(value)}.pdf");
            using (var doc = new Document())
            {
                Add(doc.NewPage(), "customer", value);
                doc.Save(path, garbage: garbage);
            }
            using (var doc = new Document(path))
            {
                var page = doc[0];
                Assert.Equal(value, page.FirstWidget.FieldValue);
                Assert.Equal(("string", value), FieldValues(doc)["customer"]);
            }
        }

        /// <summary>Control: updating an ASCII field to Unicode works on the tested wheels.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void test_text_unicode_update_control(int garbage)
        {
            string path = Out($"text-updated-g{garbage}.pdf");
            using (var doc = new Document())
            {
                var page = Add(doc.NewPage(), "customer", "Alice");
                var widget = page.FirstWidget;
                widget.FieldValue = "café";
                widget.Update();
                doc.ReloadPage(page);
                doc.Save(path, garbage: garbage);
            }
            using (var doc = new Document(path))
            {
                var page = doc[0];
                Assert.Equal("café", page.FirstWidget.FieldValue);
                Assert.Equal(("string", "café"), FieldValues(doc)["customer"]);
            }
        }

        /// <summary>PDF button /V is a name, not a text string (ISO 32000-1 12.7.4.2.3).</summary>
        [Theory]
        [InlineData(0, true)]
        [InlineData(0, false)]
        [InlineData(4, true)]
        [InlineData(4, false)]
        public void test_checkbox_creation_writes_pdf_name(int garbage, bool selected)
        {
            string path = Out($"checkbox-created-g{garbage}-{(selected ? "on" : "off")}.pdf");
            using (var doc = new Document())
            {
                Add(doc.NewPage(), "accepted", selected, WidgetType.CheckBox);
                doc.Save(path, garbage: garbage);
            }
            using (var doc = new Document(path))
            {
                var page = doc[0];
                var widget = page.FirstWidget;
                string expected = selected ? "/" + widget.OnState() : "/Off";
                using var widgetObj = mupdf.mupdf.pdf_load_object(doc.NativePdfDocument, widget.Xref);
                using var asKey = mupdf.mupdf.pdf_new_name("AS");
                using var vKey = mupdf.mupdf.pdf_new_name("V");
                using var asObj = widgetObj.pdf_dict_get_inheritable(asKey);
                using var vObj = widgetObj.pdf_dict_get_inheritable(vKey);
                Assert.Equal(expected.Substring(1), asObj.pdf_to_name());
                Assert.Equal(expected.Substring(1), vObj.pdf_to_name());
                Assert.Equal(("name", expected), FieldValues(doc)["accepted"]);
            }
        }

        /// <summary>The documented on_state update must also leave a name-valued /V.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void test_checkbox_update_writes_pdf_name(int garbage)
        {
            string path = Out($"checkbox-updated-g{garbage}.pdf");
            string wanted;
            using (var doc = new Document())
            {
                var page = Add(doc.NewPage(), "accepted", false, WidgetType.CheckBox);
                var widget = page.FirstWidget;
                wanted = widget.OnState();
                widget.FieldValue = wanted;
                widget.Update();
                doc.ReloadPage(page);
                doc.Save(path, garbage: garbage);
            }
            using (var doc = new Document(path))
            {
                var page = doc[0];
                var widget = page.FirstWidget;
                string expected = "/" + wanted;
                using var widgetObj = mupdf.mupdf.pdf_load_object(doc.NativePdfDocument, widget.Xref);
                using var asKey = mupdf.mupdf.pdf_new_name("AS");
                using var vKey = mupdf.mupdf.pdf_new_name("V");
                using var asObj = widgetObj.pdf_dict_get(asKey);
                using var vObj = widgetObj.pdf_dict_get_inheritable(vKey);
                Assert.Equal(expected.Substring(1), asObj.pdf_to_name());
                Assert.Equal(expected.Substring(1), vObj.pdf_to_name());
                Assert.Equal(expected.Substring(1), FieldValues(doc)["accepted"].value);
            }
        }

        /// <summary>Deleting a field's only widget must not leave its value in AcroForm.</summary>
        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(4, false)]
        [InlineData(4, true)]
        public void test_delete_last_widget_removes_field(int garbage, bool hierarchy)
        {
            string name = hierarchy ? "Customer.Address.City" : "customer";
            string path = Out($"deleted-g{garbage}-{(hierarchy ? "dotted" : "plain")}.pdf");
            using (var doc = new Document())
            {
                var page = Add(doc.NewPage(), name, "Seoul");
                if (hierarchy)
                    page = Add(page, "Customer.Address.Country", "Korea", y: 100);
                page.DeleteWidget(BindForDelete(page, page.FirstWidget));
                doc.Save(path, garbage: garbage);
            }
            using (var doc = new Document(path))
            {
                var page = doc[0];
                Assert.DoesNotContain(name, page.Widgets().Select(w => w.FieldName));
                var fields = FieldValues(doc);
                Assert.False(fields.ContainsKey(name), string.Join(", ", fields));
                if (hierarchy)
                    Assert.Equal(("string", "Korea"), fields["Customer.Address.Country"]);
            }
        }

        /// <summary>
        /// Independent fixture: retain a field while any widget remains.
        /// The delete-both case also fails on 1.28.2. Unlike the newly created plain
        /// field above, this shared-field cleanup problem is not a 2.0 regression.
        /// </summary>

        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(4, false)]
        [InlineData(4, true)]
        public void test_delete_shared_widgets(int garbage, bool deleteBoth)
        {
            string path = Out($"shared-deleted-g{garbage}-{(deleteBoth ? "both" : "one")}.pdf");
            using (var doc = new Document(SharedPdf))
            {
                var page = doc[0];
                page.DeleteWidget(BindForDelete(page, page.FirstWidget));
                if (deleteBoth)
                {
                    page = doc[1];
                    page.DeleteWidget(BindForDelete(page, page.FirstWidget));
                }
                doc.Save(path, garbage: garbage);
            }
            using (var doc = new Document(path))
            {
                int count = doc.Sum(page => page.Widgets().Count());
                Assert.Equal(deleteBoth ? 0 : 1, count);
                var fields = FieldValues(doc);
                if (deleteBoth)
                    Assert.False(fields.ContainsKey("customer"), string.Join(", ", fields));
                else
                    Assert.Equal(("string", "Alice"), fields["customer"]);
            }
        }

        /// <summary>
        /// Existing issue, also seen on 1.28.2. Do not call this a 2.0 regression.
        /// Compare page 2 with an explicit-update control using the same renderer.
        /// No hard-coded pixel hashes or appearance-stream text encoding assumptions.
        /// </summary>

        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(4, false)]
        [InlineData(4, true)]
        public void test_shared_value_update_refreshes_other_widget(int garbage, bool syncFlags)
        {
            // garbage and sync_flags are pytest parameters; the body does not read them.
            _ = garbage;
            _ = syncFlags;

            using (var doc = new Document(SharedPdf)) // step 1: change on page 0
            {
                var page0 = doc[0];
                using var pix0 = page0.GetPixmap();
                byte[] img0 = pix0.Samples;
                var page1 = doc[1];
                using var pix1 = page1.GetPixmap();
                byte[] img1 = pix1.Samples;
                var w = page0.FirstWidget;
                w.FieldValue = "Bob";
                w.Update();
                page1 = doc[1]; // assert page 1 reflects the update from page 0
                w = page1.FirstWidget;
                Assert.Equal("Bob", w.FieldValue);
                using var pix0b = page0.GetPixmap();
                using var pix1b = page1.GetPixmap();
                Assert.False(img0.AsSpan().SequenceEqual(pix0b.Samples));
                Assert.False(img1.AsSpan().SequenceEqual(pix1b.Samples));
            }

            using (var doc = new Document(SharedPdf)) // step 1: change on page 1
            {
                var page0 = doc[0];
                using var pix0 = page0.GetPixmap();
                byte[] img0 = pix0.Samples;
                var page1 = doc[1];
                using var pix1 = page1.GetPixmap();
                byte[] img1 = pix1.Samples;
                var w = page1.FirstWidget;
                w.FieldValue = "Bob";
                w.Update();
                w = page0.FirstWidget;
                Assert.Equal("Bob", w.FieldValue);
                using var pix0b = page0.GetPixmap();
                using var pix1b = page1.GetPixmap();
                Assert.False(img0.AsSpan().SequenceEqual(pix0b.Samples));
                Assert.False(img1.AsSpan().SequenceEqual(pix1b.Samples));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void test_shared_value_manual_update_control(int garbage)
        {
            string path = Out($"shared-updated-both-g{garbage}.pdf");
            byte[] before;
            using (var doc = new Document(SharedPdf))
            {
                using (var pix = doc[1].GetPixmap())
                    before = pix.Digest;
                foreach (var page in doc)
                {
                    var widget = page.FirstWidget;
                    widget.FieldValue = "Bob";
                    widget.Update();
                    doc.ReloadPage(page);
                }
                doc.Save(path, garbage: garbage);
            }
            using (var doc = new Document(path))
            {
                Assert.Equal(new[] { "Bob", "Bob" }, doc.Select(page => page.FirstWidget.FieldValue).ToArray());
                byte[] first, second;
                using (var pix = doc[0].GetPixmap())
                    first = pix.Digest;
                using (var pix = doc[1].GetPixmap())
                    second = pix.Digest;
                Assert.True(first.AsSpan().SequenceEqual(second));
                Assert.False(second.AsSpan().SequenceEqual(before));
            }
        }

        /// <summary>
        /// Construction-path finding, not a test of Harald's forthcoming work.
        /// 1.28.2 rejects this construction. Exclude radio tests for that comparison.
        /// </summary>

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void test_radio_creation_has_at_most_one_selected_widget(int garbage)
        {
            string path = Out($"radio-created-g{garbage}.pdf");
            CreateRadioGroup(path, garbage);
            using var doc = new Document(path);
            var page = doc[0];
            var widgets = page.Widgets().ToList();
            Assert.Equal(2, widgets.Count);
            Assert.True(
                widgets.Select(w => w.OnState()).ToHashSet()
                    .SetEquals(new[] { "Standard", "Express" }));
            int flags = int.Parse(InheritedKey(doc, widgets[0].Xref, "Ff").value);
            Assert.NotEqual(0, flags & mupdf.mupdf.PDF_BTN_FIELD_IS_RADIO);
            Assert.Equal(0, flags & mupdf.mupdf.PDF_BTN_FIELD_IS_RADIOS_IN_UNISON);
            var states = widgets.Select(w => doc.XrefGetKey(w.Xref, "AS").value).ToList();
            Assert.True(states.Count(s => s != "/Off") <= 1, string.Join(", ", states));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void test_radio_selection_writes_pdf_name(int garbage)
        {
            string initial = Out($"radio-selected-initial-g{garbage}.pdf");
            string path = Out($"radio-selected-g{garbage}.pdf");
            CreateRadioGroup(initial, garbage);
            using (var doc = new Document(initial))
            {
                var page = doc[0];
                var widget = page.FirstWidget;
                widget.FieldValue = widget.OnState();
                widget.Update();
                doc.ReloadPage(page);
                doc.Save(path, garbage: garbage);
            }
            using (var doc = new Document(path))
            {
                var pdf = doc.NativePdfDocument;
                var page = doc[0];
                var widgets = page.Widgets().ToList();
                var states = widgets.Select(w => doc.XrefGetKey(w.Xref, "AS").value).ToList();
                Assert.Equal(new[] { "/Standard", "/Off" }, states);
                using var widgetObj = mupdf.mupdf.pdf_load_object(pdf, widgets[0].Xref);
                Console.WriteLine(doc.XrefObject(6));
                using var vKey = mupdf.mupdf.pdf_new_name("V");
                using var vObj = widgetObj.pdf_dict_get_inheritable(vKey);
                Assert.Equal("Standard", vObj.pdf_to_name());
            }
        }
    }
}
