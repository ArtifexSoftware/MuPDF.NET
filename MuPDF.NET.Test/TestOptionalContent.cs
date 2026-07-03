using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// </summary>
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
            // ocg1 = doc.AddOcg("ocg1")
            int ocg1 = doc.AddOcg("ocg1");
            // ocg2 = doc.AddOcg("ocg2")
            int ocg2 = doc.AddOcg("ocg2");
            // ocg3 = doc.AddOcg("ocg3")
            int ocg3 = doc.AddOcg("ocg3");
            // ocmd1 = doc.set_ocmd(xref=0, ocgs=(ocg1, ocg2))
            int ocmd1 = doc.set_ocmd(xref: 0, ocgs: new List<int> { ocg1, ocg2 });
            doc.set_layer(-1);
            doc.AddLayer("layer1");
            // test = doc.get_layer()
            object? test = doc.get_layer();
            // test = doc.GetLayers()
            test = doc.GetLayers();
            // test = doc.GetOcgs()
            test = doc.GetOcgs();
            // test = doc.layer_ui_configs()
            test = doc.layer_ui_configs();
            doc.switch_layer(0);
        }

        [Fact]
        public void test_oc2()
        {
            using var src = new Document(Doc("joined.pdf"));

            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();

            // r0 = page.Rect / 2
            var r0 = page.Rect / 2;
            // r1 = r0 + (r0.width, 0, r0.width, 0)
            var r1 = r0 + new Rect(r0.Width, 0, r0.Width, 0);
            // r2 = r0 + (0, r0.height, 0, r0.height)
            var r2 = r0 + new Rect(0, r0.Height, 0, r0.Height);
            // r3 = r2 + (r2.width, 0, r2.width, 0)
            var r3 = r2 + new Rect(r2.Width, 0, r2.Width, 0);

            // ocg0 = doc.AddOcg("ocg0", on=True)
            int ocg0 = doc.AddOcg("ocg0", on: true);
            // ocg1 = doc.AddOcg("ocg1", on=False)
            int ocg1 = doc.AddOcg("ocg1", on: false);
            // ocg2 = doc.AddOcg("ocg2", on=False)
            int ocg2 = doc.AddOcg("ocg2", on: false);
            // ocg3 = doc.AddOcg("ocg3", on=False)
            int ocg3 = doc.AddOcg("ocg3", on: false);

            // ocmd0 = doc.set_ocmd(ve=["and", ocg0, ["not", ["or", ocg1, ocg2, ocg3]]])
            int ocmd0 = doc.set_ocmd(ve: new List<object>
            {
                "and", ocg0, new List<object> { "not", new List<object> { "or", ocg1, ocg2, ocg3 } },
            });
            // ocmd1 = doc.set_ocmd(ve=["and", ocg1, ["not", ["or", ocg0, ocg2, ocg3]]])
            int ocmd1 = doc.set_ocmd(ve: new List<object>
            {
                "and", ocg1, new List<object> { "not", new List<object> { "or", ocg0, ocg2, ocg3 } },
            });
            // ocmd2 = doc.set_ocmd(ve=["and", ocg2, ["not", ["or", ocg1, ocg0, ocg3]]])
            int ocmd2 = doc.set_ocmd(ve: new List<object>
            {
                "and", ocg2, new List<object> { "not", new List<object> { "or", ocg1, ocg0, ocg3 } },
            });
            // ocmd3 = doc.set_ocmd(ve=["and", ocg3, ["not", ["or", ocg1, ocg2, ocg0]]])
            int ocmd3 = doc.set_ocmd(ve: new List<object>
            {
                "and", ocg3, new List<object> { "not", new List<object> { "or", ocg1, ocg2, ocg0 } },
            });
            // ocmds = (ocmd0, ocmd1, ocmd2, ocmd3)
            var ocmds = new[] { ocmd0, ocmd1, ocmd2, ocmd3 };
            // page.ShowPdfPage(r0, src, 0, oc=ocmd0)
            page.ShowPdfPage(r0, src, 0, oc: ocmd0);
            // page.ShowPdfPage(r1, src, 1, oc=ocmd1)
            page.ShowPdfPage(r1, src, 1, oc: ocmd1);
            // page.ShowPdfPage(r2, src, 2, oc=ocmd2)
            page.ShowPdfPage(r2, src, 2, oc: ocmd2);
            // page.ShowPdfPage(r3, src, 3, oc=ocmd3)
            page.ShowPdfPage(r3, src, 3, oc: ocmd3);
            // xobj_ocmds = [doc.GetOc(item[0]) for item in page.GetXobjects() if item[1] != 0]
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
            // page.get_oc_items()
            page.get_oc_items();
            doc.Save(Out("test_oc2.pdf"));
        }

        [Fact]
        public void test_3143()
        {
            using var doc = new Document(Doc("test-3143.pdf"));
            // page = doc[0]
            var page = doc[0];
            // set0 = set([l["text"] for l in doc.layer_ui_configs()])
            var set0 = new HashSet<string>(doc.layer_ui_configs().Select(l => l["text"].ToString()!));
            // set1 = set([p["layer"] for p in page.GetDrawings()])
            var set1 = new HashSet<string>(page.GetDrawingsDict().Select(p => p["layer"].ToString()!));
            // set2 = set([b[2] for b in page.GetBboxlog(layers=True)])
            var set2 = new HashSet<string>(page.GetBboxlogTuples(includeLayerNames: true).Select(b => b.Item3!));
            Assert.Equal(set0, set1);
            Assert.Equal(set0, set2);
        }

        [Fact]
        public void test_3180()
        {
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();

            // combo_items = ['first', 'second', 'third']
            string[] comboItems = { "first", "second", "third" };

            // combo_box.field_name = "myComboBox"
            // combo_box.field_value = combo_items[0]
            // combo_box.choice_values = combo_items
            // combo_box.script_change = """
            // var value = event.value;
            // app.alert('You selected: ' + value);
            // //var group_id = optional_content_group_ids[value];
            var comboBox = new Widget();
            comboBox.FieldType = WidgetType.ComboBox;
            comboBox.FieldName = "myComboBox";
            comboBox.FieldValue = comboItems[0];
            comboBox.ChoiceValues = comboItems.ToList();
            comboBox.Rect = new Rect(50, 50, 200, 75);
            comboBox.ScriptChange = """
                var value = event.value;
                app.alert('You selected: ' + value);

                //var group_id = optional_content_group_ids[value];

                """;
            page.add_widget(comboBox);
            /*

            // optional_content_group_ids = {}
            var optionalContentGroupIds = new Dictionary<string, int>();
            for (int i = 0; i < comboItems.Length; i++)
            {
                string item = comboItems[i];
                // optional_content_group_id = doc.AddOcg(item, on=False)
                int optionalContentGroupId = doc.AddOcg(item, on: 0);
                // optional_content_group_ids[item] = optional_content_group_id
                optionalContentGroupIds[item] = optionalContentGroupId;
                var rect = new Rect(50, 100, 250, 300);
                // image_file_name = f'{item}.png'
                string imageFileName = $"{item}.png";
            }

            // first_id = optional_content_group_ids['first']
            int firstId = optionalContentGroupIds["first"];
            // second_id = optional_content_group_ids['second']
            int secondId = optionalContentGroupIds["second"];
            // third_id = optional_content_group_ids['third']
            int thirdId = optionalContentGroupIds["third"];
            doc.set_layer(-1, basestate: "OFF");
            // layers = doc.get_layer()
            var layers = doc.get_layer();
            doc.set_layer(config: -1, on: new List<int> { firstId });

            */
            doc.Save(Out("test_3180.pdf"));
        }
    }
}