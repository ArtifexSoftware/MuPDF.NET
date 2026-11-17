using System;
using System.IO;
using Xunit;

namespace MuPDF.NET.Test
{
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
            string spikesYes = Doc("spikes-yes.png");
            string spikesNo = Doc("spikes-no.png");
            using var doc = new Document();
            string text = "NATO MEMBERS";
            var point = new Point(10, 35);

            // make text provoking spikes

            var page = doc.NewPage(width: 200, height: 50);
            // text,
            page.InsertText(
                point,
                text,
                fontSize: 20,
                renderMode: 1,
                borderWidth: 0.3f,
                miterLimit: null);
            // write same text in white over the previous for better demo purpose
            page.InsertText(point, text, fontSize: 20, color: _Constants.white);
            var pix1 = page.GetPixmap();
            Assert.Equal(File.ReadAllBytes(spikesYes), pix1.ToBytes());
            pix1.Save(Out("test_spikes_test_pix1.png"));

            // make text suppressing spikes

            page = doc.NewPage(width: 200, height: 50);
            // text,
            page.InsertText(
                point,
                text,
                fontSize: 20,
                renderMode: 1,
                borderWidth: 0.3f,
                miterLimit: 1);
            page.InsertText(point, text, fontSize: 20, color: _Constants.white);
            var pix2 = page.GetPixmap();
            pix2.Save(Out("test_spikes_test_pix2.png"));
            Assert.Equal(File.ReadAllBytes(spikesNo), pix2.ToBytes());
            doc.Save(Out("test_spikes.pdf"));
        }
    }
}