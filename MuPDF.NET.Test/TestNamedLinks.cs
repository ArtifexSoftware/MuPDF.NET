using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestNamedLinks/</c>; outputs: <c>TestDocuments/_Output/TestNamedLinks/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestNamedLinks
    {
        private const string TestClassName = nameof(TestNamedLinks);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_2886()
        {
            //     return

            string path = Doc("cython.pdf");
            using var doc = new Document(path);
            // name "Doc-Start" is a valid named destination in that file
            // link = {
            // }
            var link = new Dictionary<string, object>
            {
                ["kind"] = Constants.LinkNamed,
                ["from"] = new Rect(0, 0, 50, 50),
                ["name"] = "Doc-Start",
            };
            // page = doc[-1]
            var page = doc[doc.PageCount - 1];
            // page.insert_link(link)
            page.insert_link(link);
            // page = doc.ReloadPage(page)
            page = doc.ReloadPage(page);

            // links = page.GetLinks()
            var links = page.GetLinks();
            // l_dict = links[-1]
            var lDict = links[links.Count - 1];
            Assert.Equal(Constants.LinkNamed, lDict["kind"]);
            Assert.Equal(link["name"], lDict["nameddest"]);
            Assert.Equal((Rect)link["from"], (Rect)lDict["from"]);
            doc.Save(Out("test_2886.pdf"));
        }

        [Fact]
        public void test_2922()
        {
            // Re-insertion of a named link item in 'Page.GetLinks()' does not have
            // the required "name" key. We test the fallback here that uses key
            //     return

            string path = Doc("cython.pdf");
            using var doc = new Document(path);
            // page = doc[2]
            var page = doc[2];
            // links = page.GetLinks()
            var links = page.GetLinks();
            // link0 = links[0]
            var link0 = links[0];
            // page.insert_link(link0)
            page.insert_link(link0);
            // page = doc.ReloadPage(page)
            page = doc.ReloadPage(page);
            // links = page.GetLinks()
            links = page.GetLinks();
            // link1 = links[-1]
            var link1 = links[links.Count - 1];

            Assert.Equal(link0["nameddest"], link1["nameddest"]);
            Assert.Equal(link0["page"], link1["page"]);
            Assert.Equal(link0["to"], link1["to"]);
            Assert.Equal((Rect)link0["from"], (Rect)link1["from"]);
            doc.Save(Out("test_2922.pdf"));
        }

        [Fact]
        public void test_3301()
        {
            // Links encoded as /URI in PDF are converted to either LINK_URI or
            // LINK_LAUNCH in MuPDF.
            // This function ensures that the 'Link.uri' containing a ':' colon
            // is converted to a URI if not explicitly starting with "file://".
            //     return

            // text = { ... }
            var text = new Dictionary<string, int>
            {
                ["https://www.google.de"] = Constants.LinkUri,
                ["http://www.google.de"] = Constants.LinkUri,
                ["mailto:jorj.x.mckie@outlook.de"] = Constants.LinkUri,
                ["www.wikipedia.de"] = Constants.LinkLaunch,
                ["awkward:resource"] = Constants.LinkUri,
                ["ftp://www.google.de"] = Constants.LinkUri,
                ["some.program"] = Constants.LinkLaunch,
                ["file://some.program"] = Constants.LinkLaunch,
                ["another.exe"] = Constants.LinkLaunch,
            };

            var r = new Rect(0, 0, 50, 20);
            // rects = [r + (0, r.height * i, 0, r.height * i) for i in range(len(text.keys()))]
            var rects = Enumerable.Range(0, text.Count)
                .Select(i => r + new Rect(0, r.Height * i, 0, r.Height * i))
                .ToList();

            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();
            int i = 0;
            foreach (var k in text.Keys)
            {
                var link = new Dictionary<string, object>
                {
                    ["kind"] = Constants.LinkUri,
                    ["uri"] = k,
                    ["from"] = rects[i],
                };
                // page.insert_link(link)
                page.insert_link(link);
                i++;
            }

            // pdfdata = doc.write()
            var pdfdata = doc.Write();
            using var doc2 = new Document(pdfdata, "pdf");
            // page = doc[0]
            page = doc2[0];
            foreach (var link in page.GetLinks())
            {
                // t = link["uri"] if (_ := link.get("file")) is None else _
                string t = link.TryGetValue("file", out var f) && f != null
                    ? (string)f
                    : (string)link["uri"];
                Assert.Equal(text[t], link["kind"]);
            }
            doc2.Save(Out("test_3301.pdf"));
        }

        [Fact]
        public void test_resolve_names_fit_variants()
        {
            // Ensure named destinations parse all common /Fit* variants and /XYZ.
            using var doc = new Document();
            var page = doc.NewPage(width: 200, height: 300);
            int pageXref = doc.PageXref(0);
            int catalogXref = doc.PdfCatalog;

            string dests =
                "<< " +
                $"/XYZDest [{pageXref} 0 R /XYZ 10 250 1.5] " +
                "/NullPageDest [null /XYZ 10 20 0] " +
                $"/FitDest [{pageXref} 0 R /Fit] " +
                $"/FitBDest [{pageXref} 0 R /FitB] " +
                $"/FitHDest [{pageXref} 0 R /FitH 20] " +    // codespell:ignore
                $"/FitHNull [{pageXref} 0 R /FitH null] " +  // codespell:ignore
                $"/FitVDest [{pageXref} 0 R /FitV 15] " +
                $"/FitBHDest [{pageXref} 0 R /FitBH 25] " +
                $"/FitBVDest [{pageXref} 0 R /FitBV 30] " +
                $"/FitRDest [{pageXref} 0 R /FitR 5 10 100 200] " +
                ">>";
            doc.XrefSetKey(catalogXref, "Dests", dests);

            var names = doc.ResolveNames();
            Assert.Equal(0, Convert.ToInt32(names["XYZDest"]["page"]));
            Assert.Equal((10.0f, 250.0f), ((float, float))names["XYZDest"]["to"]);
            Assert.Equal(1.5f, Convert.ToSingle(names["XYZDest"]["zoom"]));

            Assert.Equal(-1, Convert.ToInt32(names["NullPageDest"]["page"]));
            Assert.StartsWith("/XYZ", (string)names["NullPageDest"]["dest"]);

            Assert.Equal((0.0f, 0.0f), ((float, float))names["FitDest"]["to"]);
            Assert.Equal((0.0f, 0.0f), ((float, float))names["FitBDest"]["to"]);

            Assert.Equal((0.0f, 20.0f), ((float, float))names["FitHDest"]["to"]);
            Assert.Equal((0.0f, 0.0f), ((float, float))names["FitHNull"]["to"]);
            Assert.Equal((15.0f, 0.0f), ((float, float))names["FitVDest"]["to"]);
            Assert.Equal((0.0f, 25.0f), ((float, float))names["FitBHDest"]["to"]);
            Assert.Equal((30.0f, 0.0f), ((float, float))names["FitBVDest"]["to"]);
            Assert.Equal((5.0f, 200.0f), ((float, float))names["FitRDest"]["to"]);
        }

        [Fact]
        public void test_linkdest_view_fith_uri()
        {
            // codespell:ignore FitH
            var d = new LinkDest("#page=1&view=FitH,-4.299011");
            Assert.Equal(Constants.LinkGoto, d.Kind);
            Assert.Equal(0, d.Page);
            Assert.Equal(0.0f, d.Lt.X);
            Assert.Equal(-4.299011f, d.Lt.Y);
        }
    }
}