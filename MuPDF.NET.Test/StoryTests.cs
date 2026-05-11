using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for Story, Archive, DocumentWriter, DisplayList, Graftmap classes.
    /// Ported from tests/test_story.py and related.
    /// </summary>
    public class StoryTests
    {
        // ─── Archive ────────────────────────────────────────────────────

        [Fact]
        public void Archive_CreateEmpty()
        {
            using var arch = new Archive();
            Assert.Equal(0, arch.EntryCount);
        }

        [Fact]
        public void Archive_AddBytes()
        {
            using var arch = new Archive();
            arch.Add(System.Text.Encoding.UTF8.GetBytes("hello"), "test.txt");
            Assert.True(arch.EntryCount > 0);
            Assert.True(arch.Has("test.txt"));
        }

        [Fact]
        public void Archive_Read()
        {
            using var arch = new Archive();
            var data = System.Text.Encoding.UTF8.GetBytes("content");
            arch.Add(data, "file.txt");
            var read = arch.Read("file.txt");
            Assert.NotNull(read);
        }

        [Fact]
        public void Archive_HasMissing()
        {
            using var arch = new Archive();
            Assert.False(arch.Has("nonexistent"));
        }

        // ─── Story ──────────────────────────────────────────────────────

        [Fact]
        public void Story_CreateBasic()
        {
            using var story = new Story("<p>Hello World</p>");
            Assert.NotNull(story);
            Assert.NotNull(story.Body);
        }

        [Fact]
        public void Story_Place()
        {
            using var story = new Story("<p>Test</p>");
            var rect = new Rect(0, 0, 200, 200);
            var (more, filled) = story.Place(rect);
            Assert.True(filled.Width > 0 || filled.Height > 0 || !more);
        }

        // ─── DisplayList ────────────────────────────────────────────────

        [Fact]
        public void DisplayList_FromPage()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "Display List Test");
            using var dl = page.GetDisplayList();
            Assert.NotNull(dl);
            Assert.True(dl.Rect.Width > 0);
        }

        [Fact]
        public void DisplayList_GetTextPage()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "DL TextPage");
            using var dl = page.GetDisplayList();
            using var tp = dl.GetTextPage();
            string text = tp.ExtractText();
            Assert.Contains("DL", text);
        }

        [Fact]
        public void DisplayList_GetPixmap()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            using var dl = page.GetDisplayList();
            using var pix = dl.GetPixmap();
            Assert.True(pix.Width > 0);
        }

        [Fact]
        public void DisplayList_Search()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            page.InsertText(new Point(72, 72), "Searchable");
            using var dl = page.GetDisplayList();
            var results = dl.Search("Searchable");
            Assert.NotEmpty(results);
        }

        // ─── DocumentWriter ─────────────────────────────────────────────

        [Fact]
        public void DocumentWriter_CreateAndClose()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"mupdf_dw_{System.Guid.NewGuid()}.pdf"
            );
            try
            {
                using var writer = new DocumentWriter(path);
                var dev = writer.BeginPage(new Rect(0, 0, 595, 842));
                Assert.NotNull(dev);
                writer.EndPage();
                writer.Close();
                Assert.True(System.IO.File.Exists(path));
            }
            finally
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        }

        // ─── Graftmap ───────────────────────────────────────────────────

        [Fact]
        public void Graftmap_Create()
        {
            using var doc = new Document();
            var gm = new Graftmap(doc);
            Assert.NotNull(gm);
        }
    }
}
