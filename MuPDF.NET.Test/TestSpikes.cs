// import pymupdf
// import pathlib
// import os
using System;
using System.IO;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>Port of <c>PyMuPDF-1.27.2.2/tests/test_spikes.py</c>.</summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestSpikes/</c>; outputs: <c>TestDocuments/_Output/TestSpikes/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestSpikes
    {
        private const string TestClassName = nameof(TestSpikes);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        [Fact]
        public void test_spikes()
        {
            // """Check suppression of text spikes caused by long miters."""
            string spikesYes = Doc("spikes-yes.png");
            string spikesNo = Doc("spikes-no.png");
            // doc = pymupdf.open()
            using var doc = new Document();
            // text = "NATO MEMBERS"  # some text provoking spikes ("N", "M")
            string text = "NATO MEMBERS";
            // point = (10, 35)  # insert point
            var point = new Point(10, 35);

            // make text provoking spikes
            // page = doc.NewPage(width=200, height=50)  # small page
            var page = doc.NewPage(width: 200, height: 50);
            // page.InsertText(
            //     point,
            //     text,
            //     fontsize=20,
            //     render_mode=1,  # stroke text only
            //     border_width=0.3,  # causes thick border lines
            //     miter_limit=None,  # do not care about miter spikes
            // )
            page.InsertText(
                point,
                text,
                fontSize: 20,
                renderMode: 1,
                borderWidth: 0.3f,
                miterLimit: null);
            // write same text in white over the previous for better demo purpose
            // page.InsertText(point, text, fontsize=20, color=(1, 1, 1))
            page.InsertText(point, text, fontSize: 20, color: _Constants.white);
            // pix1 = page.GetPixmap()
            var pix1 = page.GetPixmap();
            // assert pix1.ToBytes() == spikes_yes.read_bytes()
            Assert.Equal(File.ReadAllBytes(spikesYes), pix1.ToBytes());

            // make text suppressing spikes
            // page = doc.NewPage(width=200, height=50)
            page = doc.NewPage(width: 200, height: 50);
            // page.InsertText(
            //     point,
            //     text,
            //     fontsize=20,
            //     render_mode=1,
            //     border_width=0.3,
            //     miter_limit=1,  # suppress each and every miter spike
            // )
            page.InsertText(
                point,
                text,
                fontSize: 20,
                renderMode: 1,
                borderWidth: 0.3f,
                miterLimit: 1);
            // page.InsertText(point, text, fontsize=20, color=(1, 1, 1))
            page.InsertText(point, text, fontSize: 20, color: _Constants.white);
            // pix2 = page.GetPixmap()
            var pix2 = page.GetPixmap();
            // assert pix2.ToBytes() == spikes_no.read_bytes()
            Assert.Equal(File.ReadAllBytes(spikesNo), pix2.ToBytes());
            doc.Save(Out("test_spikes.pdf"));
        }
    }
}
