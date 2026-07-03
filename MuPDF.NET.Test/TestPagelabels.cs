// Define some page labels in a PDF.
// Check success in various aspects.
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestPagelabels/</c>; outputs: <c>TestDocuments/_Output/TestPagelabels/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestPagelabels
    {
        private const string TestClassName = nameof(TestPagelabels);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static Document make_doc()
        {
            var doc = new Document();
            for (int i = 0; i < 10; i++)
            {
                // page = doc.NewPage()
                doc.NewPage();
            }
            return doc;
        }

        //     - Rule 1: labels like "A-n", page 0 is first and has "A-1".
        //     - Rule 2: labels as capital Roman numbers, page 4 is first and has "I".
        private static List<Dictionary<string, object>> make_labels()
        {
            //     {"startpage": 0, "prefix": "A-", "style": "D", "firstpagenum": 1},
            //     {"startpage": 4, "prefix": "", "style": "R", "firstpagenum": 1},
            // ]
            return new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["startpage"] = 0,
                    ["prefix"] = "A-",
                    ["style"] = "D",
                    ["firstpagenum"] = 1,
                },
                new Dictionary<string, object>
                {
                    ["startpage"] = 4,
                    ["prefix"] = "",
                    ["style"] = "R",
                    ["firstpagenum"] = 1,
                },
            };
        }

        private static bool PageLabelsRulesEqual(
            List<Dictionary<string, object>> actual,
            List<Dictionary<string, object>> expected)
        {
            if (actual.Count != expected.Count)
                return false;
            for (int i = 0; i < expected.Count; i++)
            {
                var a = actual[i];
                var e = expected[i];
                if (Convert.ToInt32(a["startpage"]) != Convert.ToInt32(e["startpage"]))
                    return false;
                if ((a.ContainsKey("prefix") ? a["prefix"]?.ToString() ?? "" : "") !=
                    (e.ContainsKey("prefix") ? e["prefix"]?.ToString() ?? "" : ""))
                    return false;
                if ((a.ContainsKey("style") ? a["style"]?.ToString() ?? "" : "") !=
                    (e.ContainsKey("style") ? e["style"]?.ToString() ?? "" : ""))
                    return false;
                if (Convert.ToInt32(a.ContainsKey("firstpagenum") ? a["firstpagenum"] : 1) !=
                    Convert.ToInt32(e.ContainsKey("firstpagenum") ? e["firstpagenum"] : 1))
                    return false;
            }
            return true;
        }

        [Fact]
        public void test_setlabels()
        {
            // - Make a PDF with 10 pages
            // - Label pages
            // - Inquire labels of pages
            // - Get list of page numbers for a given label.
            // doc = make_doc()
            using var doc = make_doc();
            doc.SetPageLabels(make_labels());
            // page_labels = [p.get_label() for p in doc]
            var page_labels = doc.Select(p => p.get_label()).ToList();
            // answer = ["A-1", "A-2", "A-3", "A-4", "I", "II", "III", "IV", "V", "VI"]
            string[] answer = { "A-1", "A-2", "A-3", "A-4", "I", "II", "III", "IV", "V", "VI" };
            Assert.Equal(answer, page_labels);
            Assert.Equal(new[] { 8 }, doc.get_page_numbers("V"));
            Assert.True(PageLabelsRulesEqual(doc.get_page_labels(), make_labels()));
            doc.Save(Out("test_setlabels.pdf"));
        }

        [Fact]
        public void test_labels_styleA()
        {
            // doc = make_doc()
            using var doc = make_doc();
            // labels = [
            //     {"startpage": 0, "prefix": "", "style": "a", "firstpagenum": 1},
            //     {"startpage": 5, "prefix": "", "style": "A", "firstpagenum": 1},
            // ]
            var labels = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["startpage"] = 0,
                    ["prefix"] = "",
                    ["style"] = "a",
                    ["firstpagenum"] = 1,
                },
                new Dictionary<string, object>
                {
                    ["startpage"] = 5,
                    ["prefix"] = "",
                    ["style"] = "A",
                    ["firstpagenum"] = 1,
                },
            };
            doc.SetPageLabels(labels);
            // pdfdata = doc.ToBytes()
            byte[] pdfdata = doc.ToBytes();
            doc.Close();
            using var doc2 = new Document(pdfdata, "pdf");
            // answer = ["a", "b", "c", "d", "e", "A", "B", "C", "D", "E"]
            string[] answer = { "a", "b", "c", "d", "e", "A", "B", "C", "D", "E" };
            // page_labels = [page.get_label() for page in doc]
            var page_labels = doc2.Select(page => page.get_label()).ToList();
            Assert.Equal(answer, page_labels);
            Assert.True(PageLabelsRulesEqual(doc2.get_page_labels(), labels));

            doc2.Save(Out("test_labels_styleA.pdf"));
        }
    }
}