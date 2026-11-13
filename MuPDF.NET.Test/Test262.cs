// https://github.com/ArtifexSoftware/MuPDF.NET/issues/262
// DeleteAnnot looks up /IRT through pdf_annot_page. That wrapper owns a page
// reference. The file must be free as soon as the document is disposed.
// A finalizer cannot be part of the check: the old borrow path suppressed it.
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class Test262
    {
        private const int RepeatCount = 8;

        [Fact]
        public void test_delete_annot_drops_page_ref()
        {
            var dir = Path.Combine(Path.GetTempPath(), "mupdf-net-262-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var replied = Path.Combine(dir, "reply.pdf");
                WriteNoteAndReply(replied);
                DeleteParentAndReplies(replied);
                AssertFileIsFree(replied);

                for (int i = 0; i < RepeatCount; i++)
                {
                    var path = Path.Combine(dir, $"note-{i}.pdf");
                    WriteNote(path, $"note {i}");
                    DeleteEveryAnnot(path);
                    AssertFileIsFree(path);
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (IOException)
                {
                }
            }
        }

        private static void WriteNote(string path, string text)
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.AddTextAnnot(new Point(72, 72), text, "Note");
            doc.Save(path);
        }

        private static void WriteNoteAndReply(string path)
        {
            using var doc = new Document();
            var page = doc.NewPage();
            var note = page.AddTextAnnot(new Point(72, 72), "Parent note", "Note");
            var reply = page.AddTextAnnot(new Point(72, 140), "Reply", "Comment");
            reply.SetIrtXref(note.Xref);
            doc.Save(path);
        }

        /// <summary>
        /// Deletes the note that other annotations point at. <see cref="Page.DeleteAnnot"/>
        /// removes those replies first, which is the <c>pdf_annot_page</c> walk from issue 262.
        /// </summary>
        private static void DeleteParentAndReplies(string path)
        {
            using var doc = new Document(path);
            using var page = doc.LoadPage(0);
            var annots = page.GetAnnots().ToList();
            Assert.Equal(2, annots.Count);
            var parent = Assert.Single(annots, a => a.IrtXref == 0);
            Assert.Contains(annots, a => a.IrtXref == parent.Xref);

            page.DeleteAnnot(parent);

            Assert.Empty(page.GetAnnots().ToList());
        }

        private static void DeleteEveryAnnot(string path)
        {
            using var doc = new Document(path);
            using var page = doc.LoadPage(0);
            foreach (var annot in page.GetAnnots().ToList())
                page.DeleteAnnot(annot);
            Assert.Empty(page.GetAnnots().ToList());
        }

        private static void AssertFileIsFree(string path)
        {
            try
            {
                using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException e)
            {
                Assert.Fail($"{Path.GetFileName(path)} stayed open after Dispose: {e.Message}");
            }
        }
    }
}
