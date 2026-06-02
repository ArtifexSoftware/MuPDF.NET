using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Port of <c>PyMuPDF-1.27.2.2/tests/test_named_links.py</c>.
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
            // """Confirm correct insertion of a 'named' link."""
            // if not hasattr(pymupdf, "mupdf"):
            //     print(f"test_2886(): not running on classic.")
            //     return

            // path = os.path.abspath(f"{__file__}/../../tests/resources/cython.pdf")
            string path = Doc("cython.pdf");
            // doc = pymupdf.open(path)
            using var doc = new Document(path);
            // name "Doc-Start" is a valid named destination in that file
            // link = {
            //     "kind": pymupdf.LINK_NAMED,
            //     "from": pymupdf.Rect(0, 0, 50, 50),
            //     "name": "Doc-Start",
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
            // assert l_dict["kind"] == pymupdf.LINK_NAMED
            Assert.Equal(Constants.LinkNamed, lDict["kind"]);
            // assert l_dict["nameddest"] == link["name"]
            Assert.Equal(link["name"], lDict["nameddest"]);
            // assert l_dict["from"] == link["from"]
            Assert.Equal((Rect)link["from"], (Rect)lDict["from"]);
            doc.Save(Out("test_2886.pdf"));
        }

        [Fact]
        public void test_2922()
        {
            // """Confirm correct recycling of a 'named' link.
            //
            // Re-insertion of a named link item in 'Page.GetLinks()' does not have
            // the required "name" key. We test the fallback here that uses key
            // "nameddest" instead.
            // """
            // if not hasattr(pymupdf, "mupdf"):
            //     print(f"test_2922(): not running on classic.")
            //     return

            // path = os.path.abspath(f"{__file__}/../../tests/resources/cython.pdf")
            string path = Doc("cython.pdf");
            // doc = pymupdf.open(path)
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

            // assert link0["nameddest"] == link1["nameddest"]
            Assert.Equal(link0["nameddest"], link1["nameddest"]);
            // assert link0["page"] == link1["page"]
            Assert.Equal(link0["page"], link1["page"]);
            // assert link0["to"] == link1["to"]
            Assert.Equal(link0["to"], link1["to"]);
            // assert link0["from"] == link1["from"]
            Assert.Equal((Rect)link0["from"], (Rect)link1["from"]);
            doc.Save(Out("test_2922.pdf"));
        }

        [Fact]
        public void test_3301()
        {
            // """Test correct differentiation between URI and LAUNCH links.
            //
            // Links encoded as /URI in PDF are converted to either LINK_URI or
            // LINK_LAUNCH in PyMuPDF.
            // This function ensures that the 'Link.uri' containing a ':' colon
            // is converted to a URI if not explicitly starting with "file://".
            // """
            // if not hasattr(pymupdf, "mupdf"):
            //     print(f"test_3301(): not running on classic.")
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

            // r = pymupdf.Rect(0, 0, 50, 20)
            var r = new Rect(0, 0, 50, 20);
            // rects = [r + (0, r.height * i, 0, r.height * i) for i in range(len(text.keys()))]
            var rects = Enumerable.Range(0, text.Count)
                .Select(i => r + new Rect(0, r.Height * i, 0, r.Height * i))
                .ToList();

            // doc = pymupdf.open()
            using var doc = new Document();
            // page = doc.NewPage()
            var page = doc.NewPage();
            int i = 0;
            foreach (var k in text.Keys)
            {
                // link = {"kind": pymupdf.LINK_URI, "uri": k, "from": rects[i]}
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
            // doc = pymupdf.open("pdf", pdfdata)
            using var doc2 = new Document(pdfdata, "pdf");
            // page = doc[0]
            page = doc2[0];
            // for link in page.GetLinks():
            foreach (var link in page.GetLinks())
            {
                // t = link["uri"] if (_ := link.get("file")) is None else _
                string t = link.TryGetValue("file", out var f) && f != null
                    ? (string)f
                    : (string)link["uri"];
                // assert text[t] == link["kind"]
                Assert.Equal(text[t], link["kind"]);
            }
            doc2.Save(Out("test_3301.pdf"));
        }
    }
}
