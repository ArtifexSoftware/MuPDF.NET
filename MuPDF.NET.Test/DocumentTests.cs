using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for the Document class.
    /// Ported from tests/test_general.py, tests/test_metadata.py.
    /// </summary>
    public class DocumentTests
    {
        // ─── Construction ───────────────────────────────────────────────

        [Fact]
        public void Document_NewEmptyPdf()
        {
            using var doc = new Document();
            Assert.True(doc.IsPdf);
            Assert.Equal(0, doc.PageCount);
            Assert.False(doc.IsClosed);
        }

        [Fact]
        public void Document_init_doc_OpenPdf_NoThrow()
        {
            using var doc = new Document();
            doc.init_doc();
            Assert.False(doc.IsEncrypted);
        }

        [Fact]
        public void Document_OpenNonExistent_Throws()
        {
            Assert.Throws<FileNotFoundException>(() => new Document("nonexistent.pdf"));
        }

        [Fact]
        public void Document_OpenEmptyFile_Throws()
        {
            var path = Path.GetTempFileName();
            try
            {
                Assert.Throws<EmptyFileException>(() => new Document(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Document_OpenEmptyStream_Throws()
        {
            Assert.Throws<EmptyFileException>(() => new Document(Array.Empty<byte>()));
        }

        [Fact]
        public void Document_Close()
        {
            var doc = new Document();
            Assert.False(doc.IsClosed);
            doc.Close();
            Assert.True(doc.IsClosed);
        }

        [Fact]
        public void Document_CloseTwiceNoError()
        {
            var doc = new Document();
            doc.Close();
            doc.Close();
            Assert.True(doc.IsClosed);
        }

        [Fact]
        public void Document_AccessAfterClose_Throws()
        {
            var doc = new Document();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => _ = doc.PageCount);
            Assert.Equal("document closed", ex.Message);
        }

        [Fact]
        public void Document_DisposeClosesDocument()
        {
            var doc = new Document();
            doc.Dispose();
            Assert.True(doc.IsClosed);
        }

        // ─── New page creation ──────────────────────────────────────────

        [Fact]
        public void Document_NewPage()
        {
            using var doc = new Document();
            var page = doc.NewPage();
            Assert.Equal(1, doc.PageCount);
            Assert.Equal(595, page.Width);
            Assert.Equal(842, page.Height);
        }

        [Fact]
        public void Document_NewPageCustomSize()
        {
            using var doc = new Document();
            var page = doc.NewPage(width: 200, height: 300);
            Assert.True(TestHelper.IsClose(200, page.Width));
            Assert.True(TestHelper.IsClose(300, page.Height));
        }

        [Fact]
        public void Document_NewPage_WhenClosed_ThrowsLikePython()
        {
            var doc = new Document();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.NewPage());
            Assert.Equal("document closed or encrypted", ex.Message);
        }

        [Fact]
        public void Document_NewPage_InsertBeforeLessThanMinusOne_ThrowsLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            Assert.Throws<ValueErrorException>(() => doc.NewPage(-2));
        }

        [Fact]
        public void Document_MultiplePages()
        {
            using var doc = new Document();
            doc.NewPage();
            doc.NewPage();
            doc.NewPage();
            Assert.Equal(3, doc.PageCount);
        }

        // ─── Page loading ───────────────────────────────────────────────

        [Fact]
        public void Document_LoadPage()
        {
            using var doc = new Document();
            doc.NewPage();
            var page = doc.LoadPage(0);
            Assert.NotNull(page);
            Assert.Equal(0, page.Number);
        }

        [Fact]
        public void Document_LoadPageNegativeIndex()
        {
            using var doc = new Document();
            doc.NewPage();
            doc.NewPage();
            var page = doc.LoadPage(-1);
            Assert.Equal(1, page.Number);
        }

        [Fact]
        public void Document_LoadPageOutOfRange_Throws()
        {
            using var doc = new Document();
            doc.NewPage();
            var ex = Assert.Throws<ValueErrorException>(() => doc.LoadPage(5));
            Assert.Equal("page not in document", ex.Message);
        }

        [Fact]
        public void Document_LoadPage_WhenClosed_ThrowsLikePython()
        {
            var doc = new Document();
            doc.NewPage();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.LoadPage(0));
            Assert.Equal("document closed or encrypted", ex.Message);
        }

        [Fact]
        public void Document_Indexer()
        {
            using var doc = new Document();
            doc.NewPage();
            var page = doc[0];
            Assert.NotNull(page);
        }

        [Fact]
        public void Document_Indexer_OutOfRange_ThrowsIndexErrorLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            var ex = Assert.Throws<IndexOutOfRangeException>(() => _ = doc[5]);
            Assert.Contains("page 5", ex.Message, StringComparison.Ordinal);
            Assert.Contains("not in document", ex.Message, StringComparison.Ordinal);
        }

        // ─── Page enumeration ───────────────────────────────────────────

        [Fact]
        public void Document_Enumerate()
        {
            using var doc = new Document();
            doc.NewPage();
            doc.NewPage();
            int count = 0;
            foreach (var page in doc)
                count++;
            Assert.Equal(2, count);
        }

        [Fact]
        public void Document_PagesGenerator()
        {
            using var doc = new Document();
            doc.NewPage();
            doc.NewPage();
            doc.NewPage();
            int count = 0;
            foreach (var page in doc.Pages(start: 0, stop: 2))
                count++;
            Assert.Equal(2, count);
        }

        // ─── Properties ─────────────────────────────────────────────────

        [Fact]
        public void Document_IsPdf()
        {
            using var doc = new Document();
            Assert.True(doc.IsPdf);
        }

        /// <summary>Matches PyMuPDF <c>tests/test_general.py::test_isdirty</c> (opened file, not a fresh in-memory PDF).</summary>
        [Fact]
        public void Document_IsDirty_FalseOnOpenPdf()
        {
            var path = TestHelper.GetResource("test_4043.pdf");
            Assert.True(File.Exists(path), $"missing test PDF: {path}");
            using var doc = new Document(path);
            Assert.False(doc.IsDirty);
        }

        [Fact]
        public void Document_NeedsPass_FalseOnNew()
        {
            using var doc = new Document();
            Assert.False(doc.NeedsPass);
        }

        [Fact]
        public void Document_IsReflowable_FalseForPdf()
        {
            using var doc = new Document();
            Assert.False(doc.IsReflowable);
        }

        [Fact]
        public void Document_ChapterCount()
        {
            using var doc = new Document();
            Assert.True(doc.ChapterCount >= 1);
        }

        [Fact]
        public void Document_ContainsPage()
        {
            using var doc = new Document();
            doc.NewPage();
            Assert.True(doc.ContainsPage(0));
            Assert.False(doc.ContainsPage(5));
        }

        // ─── Metadata ───────────────────────────────────────────────────

        [Fact]
        public void Document_GetMetadata()
        {
            using var doc = new Document();
            var meta = doc.GetMetadata();
            Assert.NotNull(meta);
            Assert.True(meta.ContainsKey("format"));
        }

        [Fact]
        public void Document_SetMetadata()
        {
            using var doc = new Document();
            doc.SetMetadata(new Dictionary<string, string>
            {
                ["title"] = "Test Title",
                ["author"] = "Test Author"
            });
            var meta = doc.GetMetadata();
            Assert.Contains("Test Title", meta.GetValueOrDefault("title", ""));
        }

        [Fact]
        public void Document_SetMetadata_WhenClosed_ThrowsLikePython()
        {
            var doc = new Document();
            doc.NewPage();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() =>
                doc.SetMetadata(new Dictionary<string, string> { ["title"] = "x" }));
            Assert.Equal("document closed or encrypted", ex.Message);
        }

        // ─── Table of Contents ──────────────────────────────────────────

        [Fact]
        public void Document_GetToc_EmptyOnNew()
        {
            using var doc = new Document();
            doc.NewPage();
            var toc = doc.GetToc();
            Assert.Empty(toc);
        }

        // ─── Page Operations ────────────────────────────────────────────

        [Fact]
        public void Document_DeletePagesBySlice_WhenClosed_ThrowsLikePython()
        {
            var doc = new Document();
            doc.NewPage();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.DeletePagesBySlice(0, 1));
            Assert.Equal("document closed", ex.Message);
        }

        [Fact]
        public void Document_DeletePage()
        {
            using var doc = new Document();
            doc.NewPage();
            doc.NewPage();
            Assert.Equal(2, doc.PageCount);
            doc.DeletePage(0);
            Assert.Equal(1, doc.PageCount);
        }

        [Fact]
        public void Document_DeletePage_WhenClosed_ThrowsDocumentClosedLikePython()
        {
            var doc = new Document();
            doc.NewPage();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.DeletePage(0));
            Assert.Equal("document closed", ex.Message);
        }

        [Fact]
        public void Document_FullcopyPage_WhenClosed_ThrowsDocumentClosedLikePython()
        {
            var doc = new Document();
            doc.NewPage();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.FullcopyPage(0));
            Assert.Equal("document closed", ex.Message);
        }

        [Fact]
        public void Document_CopyPage()
        {
            using var doc = new Document();
            doc.NewPage();
            Assert.Equal(1, doc.PageCount);
            doc.CopyPage(0);
            Assert.Equal(2, doc.PageCount);
        }

        [Fact]
        public void Document_MovePage()
        {
            using var doc = new Document();
            doc.NewPage(width: 100, height: 100);
            doc.NewPage(width: 200, height: 200);
            doc.MovePage(0, 2);
            Assert.Equal(2, doc.PageCount);
        }

        [Fact]
        public void Document_CopyPage_WhenClosed_ThrowsDocumentClosedLikePython()
        {
            var doc = new Document();
            doc.NewPage();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.CopyPage(0));
            Assert.Equal("document closed", ex.Message);
        }

        [Fact]
        public void Document_MovePage_WhenClosed_ThrowsDocumentClosedLikePython()
        {
            var doc = new Document();
            doc.NewPage();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.MovePage(0, -1));
            Assert.Equal("document closed", ex.Message);
        }

        // ─── Write / ToBytes ────────────────────────────────────────────

        [Fact]
        public void Document_Write()
        {
            using var doc = new Document();
            doc.NewPage();
            var bytes = doc.Write();
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public void Document_ToBytes()
        {
            using var doc = new Document();
            doc.NewPage();
            var bytes = doc.ToBytes();
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public void Document_Save()
        {
            var tmpDir = Path.GetTempPath();
            var path = Path.Combine(tmpDir, $"mupdfnet_test_{Guid.NewGuid()}.pdf");
            try
            {
                using var doc = new Document();
                doc.NewPage();
                doc.Save(path);
                Assert.True(File.Exists(path));
                Assert.True(new FileInfo(path).Length > 0);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // ─── Xref ───────────────────────────────────────────────────────

        [Fact]
        public void Document_XrefLength()
        {
            using var doc = new Document();
            doc.NewPage();
            Assert.True(doc.XrefLength > 0);
        }

        [Fact]
        public void Document_PageXref()
        {
            using var doc = new Document();
            doc.NewPage();
            int xref = doc.PageXref(0);
            Assert.True(xref > 0);
        }

        /// <summary>PyMuPDF <c>xref_object</c> uses <c>pdf_print_obj</c>; binding <c>pdf_to_str_buf</c> was empty for many dicts.</summary>
        [Fact]
        public void Document_XrefObject_PageDictNonEmpty()
        {
            using var doc = new Document();
            doc.NewPage(595, 842);
            int xref = doc.PageXref(0);
            var text = doc.XrefObject(xref);
            Assert.False(string.IsNullOrWhiteSpace(text));
            Assert.Contains("/Type", text, StringComparison.Ordinal);
            Assert.Contains("/Page", text, StringComparison.Ordinal);
            Assert.Contains("MediaBox", text, StringComparison.Ordinal);
        }

        [Fact]
        public void Document_XrefObject_ContainsCropBoxAfterSet()
        {
            using var doc = new Document();
            doc.NewPage(595, 842);
            doc[0].SetCropBox(new Rect(100, 200, 400, 700));
            int xref = doc.PageXref(0);
            var text = doc.XrefObject(xref);
            Assert.Contains("/CropBox", text, StringComparison.Ordinal);
            // pdf_print_obj (compress=false) may add spaces, e.g. "[ 100 200 400 700 ]".
            var compact = Regex.Replace(text, @"\s+", "");
            Assert.Contains("100200400700", compact);
        }

        [Fact]
        public void Document_PdfTrailer_NonEmpty()
        {
            using var doc = new Document();
            doc.NewPage();
            var t = doc.PdfTrailer();
            Assert.False(string.IsNullOrWhiteSpace(t));
            Assert.Contains("Root", t, StringComparison.Ordinal);
        }

        [Fact]
        public void Document_XrefObject_AfterClose_Throws()
        {
            var doc = new Document();
            doc.NewPage();
            int xref = doc.PageXref(0);
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => _ = doc.XrefObject(xref));
            Assert.Equal("document closed", ex.Message);
        }

        [Fact]
        public void Document_XrefGetKey_XrefZero_Throws()
        {
            using var doc = new Document();
            doc.NewPage();
            Assert.Throws<ValueErrorException>(() => doc.XrefGetKey(0, "Type"));
        }

        [Fact]
        public void Document_XrefGetKey_OutOfRange_Throws()
        {
            using var doc = new Document();
            doc.NewPage();
            int bad = doc.XrefLength + 50;
            Assert.Throws<ValueErrorException>(() => doc.XrefGetKey(bad, "Type"));
        }

        [Fact]
        public void Document_XrefGetKeys_TrailerMinusOne()
        {
            using var doc = new Document();
            doc.NewPage();
            var keys = doc.XrefGetKeys(-1);
            Assert.NotNull(keys);
            Assert.Contains("Root", keys);
        }

        [Fact]
        public void Document_XrefStream_XrefZero_Throws()
        {
            using var doc = new Document();
            doc.NewPage();
            Assert.Throws<ValueErrorException>(() => doc.XrefStream(0));
        }

        [Fact]
        public void Document_XrefStream_TrailerMinusOne_NoThrow()
        {
            using var doc = new Document();
            doc.NewPage();
            // Trailer is usually not a stream; PyMuPDF returns None.
            _ = doc.XrefStream(-1);
        }

        [Fact]
        public void Document_XrefStreamRaw_XrefZero_Throws()
        {
            using var doc = new Document();
            doc.NewPage();
            Assert.Throws<ValueErrorException>(() => doc.XrefStreamRaw(0));
        }

        [Fact]
        public void Document_UpdateObject_XrefZero_Throws()
        {
            using var doc = new Document();
            doc.NewPage();
            Assert.Throws<ValueErrorException>(() => doc.UpdateObject(0, "<<>>"));
        }

        [Fact]
        public void Document_UpdateStream_XrefZero_Throws()
        {
            using var doc = new Document();
            doc.NewPage();
            Assert.Throws<ValueErrorException>(() => doc.UpdateStream(0, new byte[] { 1, 2, 3 }));
        }

        [Fact]
        public void Document_UpdateStream_NonDictXref_Throws()
        {
            using var doc = new Document();
            doc.NewPage();
            int xref = doc.GetNewXref();
            doc.UpdateObject(xref, "42");
            var ex = Assert.Throws<ValueErrorException>(() => doc.UpdateStream(xref, new byte[] { 1, 2, 3 }));
            Assert.Contains("dict", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Document_update_stream_FourArgPythonOverload_IgnoresNewUsesCompressInt()
        {
            using var doc = new Document();
            doc.NewPage();
            int xref = doc.GetNewXref();
            doc.UpdateObject(xref, "<<>>");
            doc.update_stream(xref, new byte[] { 0xAB, 0xCD, 0xEF }, new_: 999, compress: 0);
            var raw = doc.XrefStreamRaw(xref);
            Assert.NotNull(raw);
        }

        [Fact]
        public void Document_XrefSetKey_NullKeyword_RemovesDictEntry()
        {
            using var doc = new Document();
            doc.NewPage();
            int xref = doc.GetNewXref();
            doc.UpdateObject(xref, "<< /Y 1 >>");
            doc.XrefSetKey(xref, "Y", "null");
            Assert.DoesNotContain("Y", doc.XrefGetKeys(xref));
            var y = doc.XrefGetKey(xref, "Y");
            Assert.Equal("null", y.type);
            Assert.Equal("null", y.value);
        }

        [Fact]
        public void Document_XrefCopy_CopiesDictKeysAndRespectsKeep()
        {
            using var doc = new Document();
            doc.NewPage();
            int a = doc.GetNewXref();
            int b = doc.GetNewXref();
            doc.UpdateObject(a, "<< /K1 1 /K2 2 >>");
            doc.UpdateObject(b, "<< /K2 99 /K3 3 >>");
            doc.XrefCopy(a, b, new[] { "K3" });
            Assert.Equal("int", doc.XrefGetKey(b, "K1").type);
            Assert.Equal("1", doc.XrefGetKey(b, "K1").value);
            Assert.Equal("2", doc.XrefGetKey(b, "K2").value);
            Assert.Equal("3", doc.XrefGetKey(b, "K3").value);
        }

        [Fact]
        public void Document_XrefCopy_StaticOverload_MatchesInstance()
        {
            using var doc = new Document();
            doc.NewPage();
            int a = doc.GetNewXref();
            int b = doc.GetNewXref();
            doc.UpdateObject(a, "<< /X 42 >>");
            doc.UpdateObject(b, "<< /Y 1 >>");
            Document.XrefCopy(doc, a, b);
            Assert.Equal("42", doc.XrefGetKey(b, "X").value);
            Assert.Equal("null", doc.XrefGetKey(b, "Y").type);
        }

        [Fact]
        public void Document_xref_copy_PythonCompat()
        {
            using var doc = new Document();
            doc.NewPage();
            int a = doc.GetNewXref();
            int b = doc.GetNewXref();
            doc.UpdateObject(a, "<< /P 7 >>");
            doc.UpdateObject(b, "<< /Q 8 >>");
            Document.xref_copy(doc, a, b, new List<string> { "Q" });
            Assert.Equal("7", doc.XrefGetKey(b, "P").value);
            Assert.Equal("8", doc.XrefGetKey(b, "Q").value);
        }

        [Fact]
        public void Document_GetNewXref()
        {
            using var doc = new Document();
            int xref = doc.GetNewXref();
            Assert.True(xref > 0);
        }

        // ─── ConvertToPdf ───────────────────────────────────────────────

        [Fact]
        public void Document_ConvertToPdf()
        {
            using var doc = new Document();
            doc.NewPage();
            var pdfBytes = doc.ConvertToPdf();
            Assert.True(pdfBytes.Length > 0);
        }

        // ─── Journal ────────────────────────────────────────────────────

        [Fact]
        public void Document_JournalEnable()
        {
            using var doc = new Document();
            doc.JournalEnable();
            Assert.True(doc.JournalIsEnabled);
        }

        [Fact]
        public void Document_Journal_WhenClosed_ThrowsValueErrorLikePython()
        {
            var doc = new Document();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.JournalEnable());
            Assert.Equal("document closed or encrypted", ex.Message);
            var ex2 = Assert.Throws<ValueErrorException>(() => doc.JournalCanDo());
            Assert.Equal("document closed or encrypted", ex2.Message);
        }

        [Fact]
        public void Document_MakeBookmark_WhenClosed_ThrowsValueErrorLikePython()
        {
            var doc = new Document();
            doc.NewPage();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.MakeBookmark((0, 0)));
            Assert.Equal("document closed or encrypted", ex.Message);
        }

        [Fact]
        public void Document_FindBookmark_WhenClosed_ThrowsValueErrorLikePython()
        {
            var doc = new Document();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.FindBookmark(0UL));
            Assert.Equal("document closed or encrypted", ex.Message);
        }

        // ─── Select / scrub (Python ValueError messages) ────────────────

        [Fact]
        public void Document_Select_WhenClosed_ThrowsCombinedMessageLikePython()
        {
            var doc = new Document();
            doc.NewPage();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.Select(new[] { 0 }));
            Assert.Equal("document closed or encrypted", ex.Message);
        }

        [Fact]
        public void Document_Select_Null_ThrowsSequenceRequiredLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            var ex = Assert.Throws<ValueErrorException>(() => doc.Select(null!));
            Assert.Equal("sequence required", ex.Message);
        }

        [Fact]
        public void Document_Select_Empty_ThrowsBadPageNumbersLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            var ex = Assert.Throws<ValueErrorException>(() => doc.Select(Array.Empty<int>()));
            Assert.Equal("bad page number(s)", ex.Message);
        }

        [Fact]
        public void Document_Select_OutOfRange_ThrowsBadPageNumbersLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            var ex = Assert.Throws<ValueErrorException>(() => doc.Select(new[] { 5 }));
            Assert.Equal("bad page number(s)", ex.Message);
        }

        [Fact]
        public void Document_Select_ReordersPages()
        {
            using var doc = new Document();
            doc.NewPage();
            doc.NewPage();
            doc.NewPage();
            doc.Select(new[] { 2, 0, 1 });
            Assert.Equal(3, doc.PageCount);
        }

        [Fact]
        public void Document_Scrub_WhenClosed_ThrowsValueErrorLikePython()
        {
            var doc = new Document();
            doc.NewPage();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.Scrub());
            Assert.Equal("closed or encrypted doc", ex.Message);
        }

        [Fact]
        public void Document_Scrub_MinimalPdf_NoThrow()
        {
            using var doc = new Document();
            doc.NewPage();
            doc.Scrub();
            Assert.Equal(1, doc.PageCount);
        }

        // ─── TOC (set_toc / set_toc_item ValueError parity) ─────────────

        [Fact]
        public void Document_SetToc_FirstLevelNot1_ThrowsValueErrorLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            var toc = new List<object> { new List<object> { 2, "x", 1 } };
            var ex = Assert.Throws<ValueErrorException>(() => doc.SetToc(toc));
            Assert.Equal("hierarchy level of item 0 must be 1", ex.Message);
        }

        [Fact]
        public void Document_SetToc_PageOutOfRange_ThrowsValueErrorLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            var toc = new List<object>
            {
                new List<object> { 1, "a", 100 },
                new List<object> { 1, "b", 1 },
            };
            var ex = Assert.Throws<ValueErrorException>(() => doc.SetToc(toc));
            Assert.Equal("row 0: page number out of range", ex.Message);
        }

        [Fact]
        public void Document_SetTocItem_BadPno_ThrowsValueErrorLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            doc.SetToc(new List<object> { new List<object> { 1, "t", 1 } });
            var ex = Assert.Throws<ValueErrorException>(() =>
                doc.SetTocItem(0, destDict: default, Constants.LINK_GOTO, 5));
            Assert.Equal("bad page number", ex.Message);
        }

        // ─── Embedded files / page_annot_xrefs (ValueError parity) ─────

        [Fact]
        public void Document_EmbfileAdd_DuplicateName_ThrowsValueErrorLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            doc.EmbfileAdd("a", new byte[] { 1, 2, 3 });
            var ex = Assert.Throws<ValueErrorException>(() => doc.EmbfileAdd("a", new byte[] { 4 }));
            Assert.Equal("Name 'a' already exists.", ex.Message);
        }

        [Fact]
        public void Document_EmbfileGet_MissingName_ThrowsValueErrorLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            var ex = Assert.Throws<ValueErrorException>(() => doc.EmbfileGet("missing"));
            Assert.Equal("'missing' not in EmbeddedFiles array.", ex.Message);
        }

        [Fact]
        public void Document_PageAnnotXrefs_BadPage_ThrowsValueErrorLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            var ex = Assert.Throws<ValueErrorException>(() => doc.PageAnnotXrefs(5));
            Assert.Equal("bad page number(s)", ex.Message);
        }

        [Fact]
        public void Document_InsertPdf_SameInstance_ThrowsValueErrorLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            var ex = Assert.Throws<ValueErrorException>(() => doc.InsertPdf(doc));
            Assert.Equal("source and target cannot be same object", ex.Message);
        }

        [Fact]
        public void Document_InsertFile_BadInfile_ThrowsValueErrorLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            var ex = Assert.Throws<ValueErrorException>(() => doc.InsertFile(123));
            Assert.Equal("bad infile parameter", ex.Message);
        }

        [Fact]
        public void Document_SetPageLayout_Invalid_ThrowsValueErrorLikePython()
        {
            using var doc = new Document();
            doc.NewPage();
            var ex = Assert.Throws<ValueErrorException>(() => doc.SetPageLayout("Nope"));
            Assert.Equal("bad PageLayout value", ex.Message);
        }

        [Fact]
        public void Document_PageCropBox_WhenClosed_ThrowsValueErrorLikePython()
        {
            var doc = new Document();
            doc.NewPage();
            doc.Close();
            var ex = Assert.Throws<ValueErrorException>(() => doc.PageCropBox(0));
            Assert.Equal("document closed", ex.Message);
        }

        // ─── Bake ───────────────────────────────────────────────────────

        [Fact]
        public void Document_Bake_NoError()
        {
            using var doc = new Document();
            doc.NewPage();
            doc.Bake();
            Assert.Equal(1, doc.PageCount);
        }

        // ─── ToString ───────────────────────────────────────────────────

        [Fact]
        public void Document_ToString()
        {
            using var doc = new Document();
            var s = doc.ToString();
            Assert.Contains("Document", s);
        }
    }
}
