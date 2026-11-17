using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{


    /// <remarks>
    /// Inputs: <c>TestDocuments/TestOptionalContent/</c>; outputs: <c>TestDocuments/_Output/TestOptionalContent/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestOptionalContent
    {
        private const string TestClassName = nameof(TestOptionalContent);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_oc1()
        {
            using var doc = new Document();
            int ocg1 = doc.AddOcg("ocg1");
            Console.WriteLine($"ocg1: {ocg1}");
            int ocg2 = doc.AddOcg("ocg2");
            Console.WriteLine($"ocg2: {ocg2}");
            int ocg3 = doc.AddOcg("ocg3");
            Console.WriteLine($"ocg3: {ocg3}");
            int ocmd1 = doc.set_ocmd(xref: 0, ocgs: new List<int> { ocg1, ocg2 });
            Console.WriteLine($"ocmd1: {ocmd1}");
            doc.set_layer(-1);
            doc.AddLayer("layer1");
            object? test = doc.GetLayer();
            Console.WriteLine($"test: {FormatPy(test)}");
            test = doc.GetLayers();
            Console.WriteLine($"test: {FormatPy(test)}");
            test = doc.GetOcgs();
            Console.WriteLine($"test: {FormatPy(test)}");
            test = doc.layer_ui_configs();
            Console.WriteLine($"test: {FormatPy(test)}");
            doc.switch_layer(0);
        }

        /// <summary>repr for the OC dumps in <see cref="test_oc1"/>.</summary>
        private static string FormatPy(object? value)
        {
            if (value == null)
                return "None";
            if (value is bool b)
                return b ? "True" : "False";
            if (value is string s)
                return $"'{s}'";
            if (value is System.Collections.IDictionary dict)
            {
                var parts = new List<string>();
                foreach (System.Collections.DictionaryEntry kv in dict)
                    parts.Add($"{FormatPy(kv.Key)}: {FormatPy(kv.Value)}");
                return "{" + string.Join(", ", parts) + "}";
            }
            if (value is System.Collections.IEnumerable seq && value is not string)
            {
                var parts = new List<string>();
                foreach (var item in seq)
                    parts.Add(FormatPy(item));
                bool isList = value is System.Collections.IList && value is not Array;
                if (isList)
                    return "[" + string.Join(", ", parts) + "]";
                return "(" + string.Join(", ", parts) + ")";
            }
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
        }

        [Fact]
        public void test_oc2()
        {
            using var src = new Document(Doc("joined.pdf"));

            using var doc = new Document();

            var page = doc.NewPage();

            var r0 = page.Rect / 2;
            var r1 = r0 + new Rect(r0.Width, 0, r0.Width, 0);
            var r2 = r0 + new Rect(0, r0.Height, 0, r0.Height);
            var r3 = r2 + new Rect(r2.Width, 0, r2.Width, 0);

            int ocg0 = doc.AddOcg("ocg0", on: true);
            int ocg1 = doc.AddOcg("ocg1", on: false);
            int ocg2 = doc.AddOcg("ocg2", on: false);
            int ocg3 = doc.AddOcg("ocg3", on: false);

            int ocmd0 = doc.set_ocmd(ve: new List<object>
            {
                "and", ocg0, new List<object> { "not", new List<object> { "or", ocg1, ocg2, ocg3 } },
            });
            int ocmd1 = doc.set_ocmd(ve: new List<object>
            {
                "and", ocg1, new List<object> { "not", new List<object> { "or", ocg0, ocg2, ocg3 } },
            });
            int ocmd2 = doc.set_ocmd(ve: new List<object>
            {
                "and", ocg2, new List<object> { "not", new List<object> { "or", ocg1, ocg0, ocg3 } },
            });
            int ocmd3 = doc.set_ocmd(ve: new List<object>
            {
                "and", ocg3, new List<object> { "not", new List<object> { "or", ocg1, ocg2, ocg0 } },
            });
            var ocmds = new[] { ocmd0, ocmd1, ocmd2, ocmd3 };
            page.ShowPdfPage(r0, src, 0, oc: ocmd0);
            page.ShowPdfPage(r1, src, 1, oc: ocmd1);
            page.ShowPdfPage(r2, src, 2, oc: ocmd2);
            page.ShowPdfPage(r3, src, 3, oc: ocmd3);
            var xobjOcmds = new List<int>();
            foreach (var item in page.GetXobjects())
            {
                if (item["xref"] is int xref && item["name"] is string name && name != "0")
                    xobjOcmds.Add(doc.GetOc(xref));
            }
            Assert.True(new HashSet<int>(ocmds).IsSubsetOf(xobjOcmds));
            Assert.Equal(
                new HashSet<int> { ocg0, ocg1, ocg2, ocg3 },
                new HashSet<int>(doc.GetOcgs().Keys));
            doc.get_ocmd(ocmd0);
            page.get_oc_items();
            doc.Save(Out("test_oc2.pdf"));
        }

        [Fact]
        public void test_3143()
        {
            using var doc = new Document(Doc("test-3143.pdf"));

            var page = doc[0];
            var set0 = new HashSet<string>(doc.LayerUiConfigs().Select(l => l["text"].ToString()!));
            var set1 = new HashSet<string>(page.GetDrawingsDict().Select(p => p["layer"].ToString()!));
            var set2 = new HashSet<string>(page.GetBboxlogTuples(includeLayerNames: true).Select(b => b.Item3!));
            Assert.Equal(set0, set1);
            Assert.Equal(set0, set2);
        }

        [Fact]
        public void test_3180()
        {
            using var doc = new Document();
            var page = doc.NewPage();

            // Define the items for the combo box
            string[] comboItems = { "first", "second", "third" };

            // Create a combo box field
            var comboBox = new Widget();  // create a new widget
            comboBox.FieldType = Constants.PDF_WIDGET_TYPE_COMBOBOX;
            comboBox.FieldName = "myComboBox";
            comboBox.FieldValue = comboItems[0];
            comboBox.ChoiceValues = comboItems.ToList();
            comboBox.Rect = new Rect(50, 50, 200, 75);  // position of the combo box
            comboBox.ScriptChange = """
                var value = event.value;
                app.alert('You selected: ' + value);

                // var group_id = optional_content_group_ids[value];

                """;

            // Insert the combo box into the page
            page.AddWidget(comboBox);

            // Load images and create OCGs for each
            var optionalContentGroupIds = new Dictionary<string, int>();
            for (int i = 0; i < comboItems.Length; i++)
            {
                string item = comboItems[i];
                int optionalContentGroupId = doc.AddOcg(item, on: false);
                optionalContentGroupIds[item] = optionalContentGroupId;
                var rect = new Rect(50, 100, 250, 300);
                string imageFileName = $"{item}.png";
                // rect,
            }

            int firstId = optionalContentGroupIds["first"];
            int secondId = optionalContentGroupIds["second"];
            int thirdId = optionalContentGroupIds["third"];

            doc.SetLayer(-1, basestate: "OFF");
            var layers = doc.GetLayer();
            doc.SetLayer(config: -1, on: new List<int> { firstId });


            doc.Save(Out("test_3180.pdf"));
        }
    }
}