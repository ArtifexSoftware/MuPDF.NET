using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a document. Mirrors PyMuPDF's Document class.
    /// </summary>
    public partial class Document : IDisposable, IEnumerable<Page>
    {
        private mupdf.FzDocument? _nativeDoc;
        private bool _disposed;
        private int _graftId;
        private static int _nextGraftId;
        private static int _nextPageRefId;
        private Dictionary<string, Dictionary<string, object>> _resolvedNames;

        /// <summary>
        /// Tracks live <see cref="Page"/> wrappers for this document, analogous to Python
        /// <c>Document._page_refs</c> (<c>weakref.WeakValueDictionary</c> keyed by <c>id(page)</c>).
        /// </summary>
        private readonly Dictionary<int, WeakReference<Page>> _pageRefs = new Dictionary<int, WeakReference<Page>>();
        private int _suppressPageRefReset;

        /// <summary>
        /// Indicate whether document has been closed.
        /// </summary>
        public bool IsClosed { get; private set; }

        /// <summary>
        /// Indicate whether document is still encrypted.
        /// </summary>
        public bool IsEncrypted { get; private set; }

        /// <summary>
        /// The filename or "&lt;memory&gt;" if created from data.
        /// </summary>
        public string Name { get; private set; } = "";

        /// <summary>
        /// The binary document data if created from a byte array.
        /// </summary>
        public byte[] StreamData { get; private set; }

        internal List<object[]> FontInfos { get; } = new List<object[]>();
        internal Dictionary<int, Graftmap> Graftmaps { get; } = new Dictionary<int, Graftmap>();
        internal Dictionary<int, Page> ShownPages { get; } = new Dictionary<int, Page>();
        internal Dictionary<string, object> InsertedImages { get; } = new Dictionary<string, object>();

        internal mupdf.FzDocument NativeDocument
        {
            get
            {
                if (IsClosed || _nativeDoc == null)
                    throw new ValueErrorException("document closed");
                return _nativeDoc;
            }
        }

        internal mupdf.PdfDocument NativePdfDocument
        {
            get
            {
                var pdf = Helpers.AsPdfDocument(NativeDocument, required: true);
                return pdf;
            }
        }

        internal int GraftId => _graftId;

        // ─── Constructors ───────────────────────────────────────────────

        /// <summary>
        /// Creates a document. Use 'open' as a synonym.
        ///
        /// Basic usages:
        /// Document() - new PDF document
        /// Document(filename) - open from file path.
        /// Document(data, filetype) - open from byte array.
        /// rect, width, height, fontsize: layout reflowable document on open (e.g. EPUB).
        /// </summary>
        public Document()
        {
            var pdf = new mupdf.PdfDocument();
            _nativeDoc = new mupdf.FzDocument(pdf);
            _graftId = _nextGraftId++;
            Name = "";
            InitDoc();
        }

        /// <summary>
        /// Creates a document. Use 'open' as a synonym.
        ///
        /// Basic usages:
        /// Document() - new PDF document
        /// Document(filename) - open from file path.
        /// Document(data, filetype) - open from byte array.
        /// rect, width, height, fontsize: layout reflowable document on open (e.g. EPUB).
        /// </summary>
        public Document(string filename, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
        {
            _graftId = _nextGraftId++;
            if (!File.Exists(filename))
                throw new FileNotFoundException($"no such file: '{filename}'");
            if (Directory.Exists(filename))
                throw new FileDataException($"'{filename}' is no file");
            if (new FileInfo(filename).Length == 0)
                throw new EmptyFileException($"Cannot open empty file: {filename}");

            Name = filename;
            float w = width, h = height;
            if (rect != null)
            {
                var r = rect.ToFzRect();
                if (mupdf.mupdf.fz_is_infinite_rect(r) == 0) { w = r.x1 - r.x0; h = r.y1 - r.y0; }
            }

            mupdf.FzDocument doc;
            try
            {
                if (filetype != null)
                {
                    var stream = mupdf.mupdf.fz_open_file(filename);
                    doc = mupdf.mupdf.fz_open_document_with_stream(filetype, stream);
                }
                else
                    doc = mupdf.mupdf.fz_open_document(filename);
            }
            catch (Exception e)
            {
                throw new FileDataException($"Failed to open file '{filename}'.", e);
            }

            bool laidOut = LayoutDoc(doc, w, h, fontsize);
            _nativeDoc = doc;
            if (laidOut)
                ResetPageRefsInternal();
            FinishOpen(filename, filetype);
        }

        /// <summary>
        /// Creates a document. Use 'open' as a synonym.
        ///
        /// Basic usages:
        /// Document() - new PDF document
        /// Document(filename) - open from file path.
        /// Document(data, filetype) - open from byte array.
        /// rect, width, height, fontsize: layout reflowable document on open (e.g. EPUB).
        /// </summary>
        public Document(byte[] data, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
        {
            _graftId = _nextGraftId++;
            if (data == null || data.Length == 0)
                throw new EmptyFileException("Cannot open empty stream.");

            StreamData = data;
            Name = "<memory>";
            float w = width, h = height;
            if (rect != null)
            {
                var r = rect.ToFzRect();
                if (mupdf.mupdf.fz_is_infinite_rect(r) == 0) { w = r.x1 - r.x0; h = r.y1 - r.y0; }
            }

            mupdf.FzDocument doc;
            try
            {
                var mem = Helpers.BufferFromBytes(data);
                var stream = mupdf.mupdf.fz_open_buffer(mem);
                doc = mupdf.mupdf.fz_open_document_with_stream(filetype ?? "", stream);
            }
            catch (Exception e)
            {
                throw new FileDataException("Failed to open stream", e);
            }

            bool laidOut = LayoutDoc(doc, w, h, fontsize);
            _nativeDoc = doc;
            if (laidOut)
                ResetPageRefsInternal();
            FinishOpen(null, filetype);
        }

        /// <summary>
        /// Open from a readable stream (Python <c>fitz.open(stream=buffer, filetype=type)</c>). Reads the stream to completion; when <see cref="Stream.CanSeek"/> is true, the position is reset to 0 first.
        /// </summary>
        public Document(Stream stream, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            : this(ReadStreamFully(stream), filetype, rect, width, height, fontsize)
        {
        }

        // ─── Static factory (Python fitz.open) ───────────────────────────

        /// <summary>Python <c>fitz.open()</c> — new empty PDF.</summary>
        public static Document Open() => new Document();

        /// <summary>Python <c>fitz.open(filename, ...)</c>.</summary>
        public static Document Open(string filename, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            => new Document(filename, filetype, rect, width, height, fontsize);

        /// <summary>Python <c>fitz.open(stream=buffer, filetype=type, ...)</c>.</summary>
        public static Document Open(byte[] data, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            => new Document(data, filetype, rect, width, height, fontsize);

        /// <summary>Python <c>fitz.open(stream=stream, filetype=type, ...)</c> for any readable <see cref="Stream"/>.</summary>
        public static Document Open(Stream stream, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            => new Document(stream, filetype, rect, width, height, fontsize);

        private static byte[] ReadStreamFully(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (stream.CanSeek && stream.Position != 0)
                stream.Position = 0;
            if (stream is MemoryStream mem)
                return mem.ToArray();
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
        }

        private bool LayoutDoc(mupdf.FzDocument doc, float w, float h, float fontsize)
        {
            if (w > 0 && h > 0)
            {
                mupdf.mupdf.fz_layout_document(doc, w, h, fontsize);
                return true;
            }
            if (mupdf.mupdf.fz_is_document_reflowable(doc) != 0)
            {
                mupdf.mupdf.fz_layout_document(doc, 400, 600, 11);
                return true;
            }
            return false;
        }

        private void FinishOpen(string filename, string filetype)
        {
            if (NeedsPass)
                IsEncrypted = true;
            else
                InitDoc();

            if (filename != null && filename.ToLower().EndsWith("svg") || filetype != null && filetype.ToLower().Contains("svg"))
            {
                try { ConvertToPdf(); }
                catch (Exception e) { throw new FileDataException("cannot open broken document", e); }
            }
        }

        // ─── Properties ─────────────────────────────────────────────────

        /// <summary>
        /// Check for PDF.
        /// </summary>
        public bool IsPdf
        {
            get
            {
                try { var p = new mupdf.PdfDocument(NativeDocument); return p.m_internal != null; }
                catch { return false; }
            }
        }

        /// <summary>
        /// Number of pages.
        /// </summary>
        public int PageCount => mupdf.mupdf.fz_count_pages(NativeDocument);
        /// <summary>
        /// Number of chapters.
        /// </summary>
        public int ChapterCount => mupdf.mupdf.fz_count_chapters(NativeDocument);
        /// <summary>
        /// Indicate password required.
        /// </summary>
        public bool NeedsPass => mupdf.mupdf.fz_needs_password(NativeDocument) != 0;
        /// <summary>
        /// Check if document is layoutable.
        /// </summary>
        public bool IsReflowable => mupdf.mupdf.fz_is_document_reflowable(NativeDocument) != 0;

        /// <summary>
        /// Check if document has unsaved changes.
        /// </summary>
        public bool IsDirty
        {
            get
            {
                if (!IsPdf) return false;
                return mupdf.mupdf.pdf_has_unsaved_changes(NativePdfDocument) != 0;
            }
        }

        /// <summary>
        /// Either False or PDF field count.
        /// </summary>
        public bool IsFormPdf
        {
            get
            {
                if (!IsPdf) return false;
                try
                {
                    var pdf = NativePdfDocument;
                    var fields = Helpers.PdfDictGetl(
                        mupdf.mupdf.pdf_trailer(pdf),
                        mupdf.mupdf.pdf_new_name("Root"),
                        mupdf.mupdf.pdf_new_name("AcroForm"),
                        mupdf.mupdf.pdf_new_name("Fields"));
                    return fields.m_internal != null && mupdf.mupdf.pdf_array_len(fields) > 0;
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// Check whether PDF was repaired.
        /// </summary>
        public bool IsRepaired
        {
            get
            {
                if (!IsPdf) return false;
                return mupdf.mupdf.pdf_was_repaired(NativePdfDocument) != 0;
            }
        }

        /// <summary>
        /// Check if PDF is linearized (fast web access).
        /// </summary>
        public bool IsFastWebaccess
        {
            get
            {
                try { return mupdf.mupdf.pdf_doc_was_linearized(NativePdfDocument) != 0; }
                catch { return false; }
            }
        }

        /// <summary>
        /// Count PDF document versions.
        /// </summary>
        public int VersionCount
        {
            get
            {
                try { return mupdf.mupdf.pdf_count_versions(NativePdfDocument); }
                catch { return 0; }
            }
        }

        /// <summary>
        /// Number of xref table entries.
        /// </summary>
        public int XrefLength
        {
            get
            {
                try { return mupdf.mupdf.pdf_xref_len(NativePdfDocument); }
                catch { return 0; }
            }
        }

        /// <summary>
        /// Document permissions.
        /// </summary>
        public int Permissions
        {
            get
            {
                try { return mupdf.mupdf.pdf_document_permissions(NativePdfDocument); }
                catch { return 0; }
            }
        }

        /// <summary>
        /// Document language.
        /// </summary>
        public string Language
        {
            get
            {
                try
                {
                    var pdf = NativePdfDocument;
                    var lang = Helpers.PdfDictGetl(
                        mupdf.mupdf.pdf_trailer(pdf),
                        mupdf.mupdf.pdf_new_name("Root"),
                        mupdf.mupdf.pdf_new_name("Lang"));
                    if (lang.m_internal != null) return mupdf.mupdf.pdf_to_text_string(lang);
                }
                catch { }
                return null;
            }
        }

        /// <summary>
        /// Id (chapter, page) of last page.
        /// </summary>
        public (int, int) LastLocation => (ChapterCount - 1, ChapterPageCount(ChapterCount - 1) - 1);

        // ─── Core Methods ───────────────────────────────────────────────

        /// <summary>
        /// Decrypt document.
        /// </summary>
        public bool Authenticate(string password)
        {
            bool ok = mupdf.mupdf.fz_authenticate_password(NativeDocument, password) != 0;
            if (ok) { IsEncrypted = false; InitDoc(); }
            return ok;
        }

        /// <summary>
        /// Load a page.
        /// </summary>
        public Page LoadPage(int pageNo)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            int pc = PageCount;
            // Python Document.__contains__(int): loc < page_count; load_page then wraps negatives.
            if (!(pageNo < pc))
                throw new ValueErrorException("page not in document");
            int idx = pageNo;
            if (idx < 0)
            {
                while (idx < 0)
                    idx += pc;
            }
            if (idx < 0 || idx >= pc)
                throw new ValueErrorException("page not in document");
            var fzPage = mupdf.mupdf.fz_load_page(NativeDocument, idx);
            return new Page(fzPage, this);
        }

        /// <summary>
        /// Load a page.
        /// </summary>
        public Page LoadPage(int chapter, int pageInChapter)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            if (!ContainsChapterPage(chapter, pageInChapter))
                throw new ValueErrorException("page not in document");
            var fzPage = mupdf.mupdf.fz_load_chapter_page(NativeDocument, chapter, pageInChapter);
            return new Page(fzPage, this);
        }

        /// <summary>Python <c>Document.__getitem__(i)</c> for an <c>int</c>: <c>if i not in self: raise IndexError</c> then <c>load_page(i)</c>.</summary>
        internal Page GetItemPageForIndexer(int pageNo)
        {
            int pc = PageCount;
            if (!(pageNo < pc))
                throw new IndexOutOfRangeException($"page {pageNo} not in document");
            return LoadPage(pageNo);
        }

        /// <summary>Python <c>Document.__getitem__((chapter, pno))</c> membership then <c>load_page</c>.</summary>
        internal Page GetItemPageForIndexer(int chapter, int pageInChapter)
        {
            _ = PageCount;
            if (!ContainsChapterPage(chapter, pageInChapter))
                throw new IndexOutOfRangeException($"page ({chapter}, {pageInChapter}) not in document");
            return LoadPage(chapter, pageInChapter);
        }

        /// <summary>
        /// Load a page (Python <c>doc[i]</c> / <c>__getitem__</c>).
        /// </summary>
        public Page this[int pageNo] => GetItemPageForIndexer(pageNo);

        /// <summary>
        /// Page count of chapter.
        /// </summary>
        public int ChapterPageCount(int chapter) => mupdf.mupdf.fz_count_chapter_pages(NativeDocument, chapter);

        /// <summary>
        /// Check if a page number is valid for this document.
        /// </summary>
        public bool ContainsPage(int pageNo) => pageNo >= 0 && pageNo < PageCount;

        /// <summary>
        /// Check if a chapter index and page-within-chapter index are valid (Python <c>(chapter, pno) in doc</c>).
        /// </summary>
        public bool ContainsChapterPage(int chapter, int pageInChapter)
        {
            if (chapter < 0 || chapter >= ChapterCount) return false;
            if (pageInChapter < 0 || pageInChapter >= ChapterPageCount(chapter)) return false;
            return true;
        }

        /// <summary>Same as <see cref="ContainsChapterPage(int, int)"/> using a <c>(chapter, page)</c> pair.</summary>
        public bool ContainsLocation((int chapter, int page) loc) => ContainsChapterPage(loc.chapter, loc.page);

        /// <summary>
        /// Return a generator iterator over a page range.
        /// </summary>
        public IEnumerable<Page> Pages(int? start = null, int? stop = null, int? step = null)
        {
            int s = start ?? 0, e = stop ?? PageCount, st = step ?? 1;
            for (int i = s; i < e; i += st)
                yield return LoadPage(i);
        }

        /// <summary>
        /// Get (chapter, page) of next page.
        /// </summary>
        public (int chapter, int pageInChapter) NextLocation((int chapter, int page) loc)
        {
            var fzLoc = new mupdf.fz_location();
            fzLoc.chapter = loc.chapter;
            fzLoc.page = loc.page;
            var next = mupdf.mupdf.fz_next_page(NativeDocument, new mupdf.FzLocation(fzLoc));
            return (next.chapter, next.page);
        }

        /// <summary>
        /// Get (chapter, page) of previous page.
        /// </summary>
        public (int chapter, int pageInChapter) PrevLocation((int chapter, int page) loc)
        {
            var fzLoc = new mupdf.fz_location();
            fzLoc.chapter = loc.chapter;
            fzLoc.page = loc.page;
            var prev = mupdf.mupdf.fz_previous_page(NativeDocument, new mupdf.FzLocation(fzLoc));
            return (prev.chapter, prev.page);
        }

        /// <summary>
        /// Convert page number to (chapter, page).
        /// </summary>
        public (int chapter, int page) LocationFromPageNumber(int pno)
        {
            var loc = mupdf.mupdf.fz_location_from_page_number(NativeDocument, pno);
            return (loc.chapter, loc.page);
        }

        /// <summary>
        /// Convert (chapter, pno) to page number.
        /// </summary>
        public int PageNumberFromLocation((int chapter, int page) loc)
        {
            var fzLoc = new mupdf.fz_location();
            fzLoc.chapter = loc.chapter;
            fzLoc.page = loc.page;
            return mupdf.mupdf.fz_page_number_from_location(NativeDocument, new mupdf.FzLocation(fzLoc));
        }

        /// <summary>
        /// Make a page pointer before layouting document.
        /// </summary>
        public ulong MakeBookmark((int chapter, int page) loc)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            var fzLoc = new mupdf.FzLocation(loc.chapter, loc.page);
            return mupdf.mupdf.ll_fz_make_bookmark2(NativeDocument.m_internal, fzLoc.internal_());
        }

        /// <summary>
        /// Find new location after layouting a document.
        /// </summary>
        public (int chapter, int page) FindBookmark(ulong bm)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            var location = mupdf.mupdf.fz_lookup_bookmark2(NativeDocument, bm);
            return (location.chapter, location.page);
        }

        // ─── Metadata ───────────────────────────────────────────────────

        /// <summary>
        /// Get document metadata.
        /// </summary>
        public Dictionary<string, string> GetMetadata()
        {
            var result = new Dictionary<string, string>();
            string[] keys = { "format", "encryption", "title", "author", "subject", "keywords", "creator", "producer", "creationDate", "modDate", "trapped" };
            foreach (var key in keys)
            {
                try
                {
                    if (IsPdf && key != "format" && key != "encryption")
                    {
                        var direct = TryGetMetadataFromPdfInfo(key);
                        if (!string.IsNullOrEmpty(direct))
                        {
                            result[key] = direct;
                            continue;
                        }
                    }

                    string mkey = key == "format" || key == "encryption" ? key : $"info:{key}";
                    result[key] = mupdf.mupdf.fz_lookup_metadata2(NativeDocument, mkey) ?? "";
                }
                catch { result[key] = ""; }
            }
            return result;
        }

        private static string MetadataKeyToInfoPdfName(string key) =>
            key switch
            {
                "title" => "Title",
                "author" => "Author",
                "subject" => "Subject",
                "keywords" => "Keywords",
                "creator" => "Creator",
                "producer" => "Producer",
                "creationDate" => "CreationDate",
                "modDate" => "ModDate",
                "trapped" => "Trapped",
                _ => null,
            };

        /// <summary>
        /// Read standard document info from the PDF <c>/Info</c> dictionary when
        /// <see cref="mupdf.mupdf.fz_lookup_metadata2"/> has not yet observed in-memory updates.
        /// </summary>
        private string TryGetMetadataFromPdfInfo(string key)
        {
            var pdfName = MetadataKeyToInfoPdfName(key);
            if (pdfName == null)
                return null;
            try
            {
                var pdf = NativePdfDocument;
                var trailer = mupdf.mupdf.pdf_trailer(pdf);
                var infoRef = mupdf.mupdf.pdf_dict_get(trailer, mupdf.mupdf.pdf_new_name("Info"));
                if (infoRef.m_internal == null)
                    return null;
                mupdf.PdfObj infoDict;
                if (mupdf.mupdf.pdf_is_indirect(infoRef) != 0)
                {
                    int xn = mupdf.mupdf.pdf_to_num(infoRef);
                    infoDict = mupdf.mupdf.pdf_load_object(pdf, xn);
                }
                else
                    infoDict = infoRef;
                if (infoDict.m_internal == null)
                    return null;
                var val = infoDict.pdf_dict_get(mupdf.mupdf.pdf_new_name(pdfName));
                if (val.m_internal == null)
                    return null;
                if (mupdf.mupdf.pdf_is_string(val) != 0)
                    return mupdf.mupdf.pdf_to_text_string(val);
                if (mupdf.mupdf.pdf_is_name(val) != 0)
                    return mupdf.mupdf.pdf_to_name(val);
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Set document metadata.
        /// </summary>
        public void SetMetadata(Dictionary<string, string> m)
        {
            EnsurePdf();

            m ??= new Dictionary<string, string>();

            // Keys accepted by PyMuPDF Document.set_metadata (src/__init__.py).
            var keymap = new Dictionary<string, string>
            {
                ["author"] = "Author",
                ["producer"] = "Producer",
                ["creator"] = "Creator",
                ["title"] = "Title",
                ["format"] = null,
                ["encryption"] = null,
                ["creationDate"] = "CreationDate",
                ["modDate"] = "ModDate",
                ["subject"] = "Subject",
                ["keywords"] = "Keywords",
                ["trapped"] = "Trapped",
            };

            var invalidKeys = m.Keys.Where(k => !keymap.ContainsKey(k)).ToList();
            if (invalidKeys.Count > 0)
                throw new ValueErrorException($"bad dict key(s): {{{string.Join(", ", invalidKeys)}}}");

            var pdf = NativePdfDocument;
            var trailer = mupdf.mupdf.pdf_trailer(pdf);
            var infoKey = mupdf.mupdf.pdf_new_name("Info");
            var info = mupdf.mupdf.pdf_dict_get(trailer, infoKey);

            if (m.Count == 0 && info.m_internal == null)
                return;

            if (m.Count == 0)
            {
                trailer.pdf_dict_del(infoKey);
                InitDoc();
                return;
            }

            mupdf.PdfObj infoObj;
            int infoXref = 0;
            if (info.m_internal == null)
            {
                // fz_lookup_metadata reads /Info as an indirect object; mirror PyMuPDF xref + trailer ref.
                infoXref = GetNewXref();
                var emptyDict = mupdf.mupdf.pdf_new_dict(pdf, 4);
                pdf.pdf_update_object(infoXref, emptyDict);
                var ind = pdf.pdf_new_indirect(infoXref, 0);
                trailer.pdf_dict_put(infoKey, ind);
                info = mupdf.mupdf.pdf_dict_get(trailer, infoKey);
            }

            if (info.m_internal != null && mupdf.mupdf.pdf_is_indirect(info) != 0)
            {
                infoXref = mupdf.mupdf.pdf_to_num(info);
                infoObj = mupdf.mupdf.pdf_load_object(pdf, infoXref);
            }
            else
                infoObj = info;

            foreach (var kv in m)
            {
                if (!keymap.TryGetValue(kv.Key, out var pdfKey) || pdfKey == null)
                    continue;

                var nameObj = mupdf.mupdf.pdf_new_name(pdfKey);
                if (string.IsNullOrEmpty(kv.Value) || string.Equals(kv.Value, "none", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(kv.Value, "null", StringComparison.OrdinalIgnoreCase))
                    infoObj.pdf_dict_del(nameObj);
                else
                    infoObj.pdf_dict_put_text_string(nameObj, kv.Value);
            }

            if (infoXref > 0)
                pdf.pdf_update_object(infoXref, infoObj);

            InitDoc();
        }

        // ─── TOC ────────────────────────────────────────────────────────

        /// <summary>
        /// Get table of contents.
        /// </summary>
        public List<(int level, string title, int page, Dictionary<string, object> link)> GetToc(bool simple = true)
        {
            var result = new List<(int, string, int, Dictionary<string, object>)>();
            var ol = mupdf.mupdf.fz_load_outline(NativeDocument);
            if (ol.m_internal == null) return result;
            CollectToc(ol, result, 1, simple);
            return result;
        }

        private List<int> DelTocInternal()
        {
            EnsureNotClosed();
            if (IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            var xrefs = new List<int>();
            var pdf = Helpers.AsPdfDocument(NativeDocument, required: false);
            if (pdf == null || pdf.m_internal == null)
                return xrefs;

            var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            var olroot = mupdf.mupdf.pdf_dict_get(root, mupdf.mupdf.pdf_new_name("Outlines"));
            if (olroot.m_internal == null)
                return xrefs;

            var first = mupdf.mupdf.pdf_dict_get(olroot, mupdf.mupdf.pdf_new_name("First"));
            void Collect(mupdf.PdfObj item)
            {
                if (item == null || item.m_internal == null)
                    return;
                xrefs.Add(mupdf.mupdf.pdf_to_num(item));
                var down = mupdf.mupdf.pdf_dict_get(item, mupdf.mupdf.pdf_new_name("First"));
                if (down.m_internal != null)
                    Collect(down);
                var next = mupdf.mupdf.pdf_dict_get(item, mupdf.mupdf.pdf_new_name("Next"));
                if (next.m_internal != null)
                    Collect(next);
            }
            Collect(first);

            int olrootXref = mupdf.mupdf.pdf_to_num(olroot);
            mupdf.mupdf.pdf_delete_object(pdf, olrootXref);
            mupdf.mupdf.pdf_dict_del(root, mupdf.mupdf.pdf_new_name("Outlines"));
            for (int i = 0; i < xrefs.Count; i++)
                mupdf.mupdf.pdf_delete_object(pdf, xrefs[i]);
            xrefs.Add(olrootXref);
            InitDoc();
            return xrefs;
        }

        private int GetOLRootNumber()
        {
            EnsureNotClosed();
            if (IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            var pdf = NativePdfDocument;
            var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            var olroot = mupdf.mupdf.pdf_dict_get(root, mupdf.mupdf.pdf_new_name("Outlines"));
            if (olroot.m_internal == null)
            {
                olroot = mupdf.mupdf.pdf_new_dict(pdf, 4);
                mupdf.mupdf.pdf_dict_put(olroot, mupdf.mupdf.pdf_new_name("Type"), mupdf.mupdf.pdf_new_name("Outlines"));
                var indObj = mupdf.mupdf.pdf_add_object(pdf, olroot);
                mupdf.mupdf.pdf_dict_put(root, mupdf.mupdf.pdf_new_name("Outlines"), indObj);
                olroot = mupdf.mupdf.pdf_dict_get(root, mupdf.mupdf.pdf_new_name("Outlines"));
            }
            return mupdf.mupdf.pdf_to_num(olroot);
        }

        private static string FormatNum(double v) => v.ToString("g", System.Globalization.CultureInfo.InvariantCulture);

        private static string BuildDestAction(int xref, Dictionary<string, object> ddict)
        {
            if (ddict == null)
                return "";
            int kind = ddict.ContainsKey("kind") && ddict["kind"] is int ? (int)ddict["kind"] : Constants.LINK_NONE;
            if (kind == Constants.LINK_NONE)
                return "";

            if (kind == Constants.LINK_GOTO)
            {
                double zoom = ddict.ContainsKey("zoom") ? Convert.ToDouble(ddict["zoom"], System.Globalization.CultureInfo.InvariantCulture) : 0.0;
                Point to = ddict.ContainsKey("to") && ddict["to"] is Point ? new Point((Point)ddict["to"]) : new Point(0, 0);
                return "/A<</S/GoTo/D[" + xref.ToString(System.Globalization.CultureInfo.InvariantCulture) + " 0 R/XYZ "
                    + FormatNum(to.X) + " " + FormatNum(to.Y) + " " + FormatNum(zoom) + "]>>";
            }
            if (kind == Constants.LINK_URI)
            {
                string uri = ddict.ContainsKey("uri") ? ddict["uri"]?.ToString() ?? "" : "";
                return "/A<</S/URI/URI" + Helpers.GetPdfStr(uri) + ">>";
            }
            if (kind == Constants.LINK_LAUNCH)
            {
                string file = ddict.ContainsKey("file") ? ddict["file"]?.ToString() ?? "" : "";
                string fspec = Helpers.GetPdfStr(file);
                return "/A<</S/Launch/F<</F" + fspec + "/UF" + fspec + "/Type/Filespec>>>>";
            }
            if (kind == Constants.LINK_GOTOR)
            {
                string file = ddict.ContainsKey("file") ? ddict["file"]?.ToString() ?? "" : "";
                string fspec = Helpers.GetPdfStr(file);
                int page = ddict.ContainsKey("page") ? Convert.ToInt32(ddict["page"], System.Globalization.CultureInfo.InvariantCulture) : -1;
                if (page < 0)
                {
                    string to = ddict.ContainsKey("to") ? Helpers.GetPdfStr(ddict["to"]?.ToString() ?? "") : Helpers.GetPdfStr("");
                    return "/A<</S/GoToR/D" + to + "/F<</F" + fspec + "/UF" + fspec + "/Type/Filespec>>>>";
                }
                Point p = ddict.ContainsKey("to") && ddict["to"] is Point ? new Point((Point)ddict["to"]) : new Point(0, 0);
                double z = ddict.ContainsKey("zoom") ? Convert.ToDouble(ddict["zoom"], System.Globalization.CultureInfo.InvariantCulture) : 0.0;
                return "/A<</S/GoToR/D[" + page.ToString(System.Globalization.CultureInfo.InvariantCulture) + " /XYZ "
                    + FormatNum(p.X) + " " + FormatNum(p.Y) + " " + FormatNum(z) + "]/F<</F" + fspec + "/UF" + fspec + "/Type/Filespec>>>>";
            }
            return "";
        }

        private static List<object> TocRowToList(object row)
        {
            if (row is IList<object> lo) return new List<object>(lo);
            if (row is object[] oa) return new List<object>(oa);
            if (row is System.Collections.IList il)
            {
                var rc = new List<object>(il.Count);
                for (int i = 0; i < il.Count; i++) rc.Add(il[i]);
                return rc;
            }
            return null;
        }

        /// <summary>
        /// Create new outline tree (table of contents, TOC).
        ///
        /// Args:
        /// toc: (list, tuple) each entry must contain level, title, page and
        /// optionally top margin on the page. None or '()' remove the TOC.
        /// collapse: (int) collapses entries beyond this level. Zero or None
        /// shows all entries unfolded.
        /// Returns:
        /// the number of inserted items, or the number of removed items respectively.
        /// </summary>
        public int SetToc(IList<object> toc, int collapse = 1)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            if (!IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);
            // if not toc:  # remove all entries
            if (toc == null || toc.Count == 0)
                return DelTocInternal().Count;

            int toclen = toc.Count;
            int pageCount = PageCount;
            var t0 = TocRowToList(toc[0]);
            if (t0 == null || (t0.Count != 3 && t0.Count != 4))
                throw new ValueErrorException("items must be sequences of 3 or 4 items");
            if (Convert.ToInt32(t0[0], System.Globalization.CultureInfo.InvariantCulture) != 1)
                throw new ValueErrorException("hierarchy level of item 0 must be 1");

            for (int i = 0; i < toclen - 1; i++)
            {
                var t1 = TocRowToList(toc[i]);
                var t2 = TocRowToList(toc[i + 1]);
                int page = Convert.ToInt32(t1[2], System.Globalization.CultureInfo.InvariantCulture);
                if (page < -1 || page > pageCount)
                    throw new ValueErrorException("row " + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + ": page number out of range");
                if (t2 == null || (t2.Count != 3 && t2.Count != 4))
                    throw new ValueErrorException("bad row " + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                int level2 = Convert.ToInt32(t2[0], System.Globalization.CultureInfo.InvariantCulture);
                if (level2 < 1)
                    throw new ValueErrorException("bad hierarchy level in row " + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                int level1 = Convert.ToInt32(t1[0], System.Globalization.CultureInfo.InvariantCulture);
                if (level2 > level1 + 1)
                    throw new ValueErrorException("bad hierarchy level in row " + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            DelTocInternal();
            var xref = new List<int>();
            xref.Add(GetOLRootNumber());
            for (int i = 0; i < toclen; i++)
                xref.Add(GetNewXref());

            var lvltab = new Dictionary<int, int> { [0] = 0 };
            var olitems = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["count"] = 0, ["first"] = -1, ["last"] = -1, ["xref"] = xref[0]
                }
            };

            for (int i = 0; i < toclen; i++)
            {
                var o = TocRowToList(toc[i]);
                int lvl = Convert.ToInt32(o[0], System.Globalization.CultureInfo.InvariantCulture);
                string title = Helpers.GetPdfStr(o[1]?.ToString() ?? "");
                int pno = Math.Min(PageCount - 1, Math.Max(0, Convert.ToInt32(o[2], System.Globalization.CultureInfo.InvariantCulture) - 1));
                int pageXref = PageXref(pno);
                double pageHeight = PageCropBox(pno).Height;
                Point top = new Point(72, pageHeight - 36);
                var dest = new Dictionary<string, object> { ["to"] = top, ["kind"] = Constants.LINK_GOTO };
                if (Convert.ToInt32(o[2], System.Globalization.CultureInfo.InvariantCulture) < 0)
                    dest["kind"] = Constants.LINK_NONE;

                if (o.Count > 3)
                {
                    object o3 = o[3];
                    if (o3 is int || o3 is float || o3 is double || o3 is decimal)
                    {
                        double t = Convert.ToDouble(o3, System.Globalization.CultureInfo.InvariantCulture);
                        dest["to"] = new Point(72, pageHeight - t);
                    }
                    else if (o3 is Dictionary<string, object> d)
                    {
                        // We make a copy of o[3] to avoid modifying our caller's data.
                        dest = new Dictionary<string, object>(d);
                        if (!dest.ContainsKey("to"))
                            dest["to"] = top;
                        else if (dest["to"] is Point)
                        {
                            var page = this[pno];
                            var point = new Point((Point)dest["to"]);
                            point.Y = page.CropBox.Height - point.Y;
                            point.Transform(page.RotationMatrix);
                            dest["to"] = point;
                        }
                    }
                }

                var dct = new Dictionary<string, object>
                {
                    ["first"] = -1,
                    ["count"] = 0,
                    ["last"] = -1,
                    ["prev"] = -1,
                    ["next"] = -1,
                    ["dest"] = BuildDestAction(pageXref, dest),
                    ["top"] = dest["to"],
                    ["title"] = title,
                    ["parent"] = lvltab[lvl - 1],
                    ["xref"] = xref[i + 1],
                    ["color"] = dest.ContainsKey("color") ? dest["color"] : null,
                    ["flags"] = (dest.ContainsKey("italic") ? Convert.ToInt32(dest["italic"], System.Globalization.CultureInfo.InvariantCulture) : 0)
                             + 2 * (dest.ContainsKey("bold") ? Convert.ToInt32(dest["bold"], System.Globalization.CultureInfo.InvariantCulture) : 0)
                };
                lvltab[lvl] = i + 1;
                var parent = olitems[lvltab[lvl - 1]];
                bool suppress = (dest.ContainsKey("collapse") && Convert.ToBoolean(dest["collapse"], System.Globalization.CultureInfo.InvariantCulture))
                    || (collapse != 0 && lvl > collapse);
                parent["count"] = Convert.ToInt32(parent["count"], System.Globalization.CultureInfo.InvariantCulture) + (suppress ? -1 : 1);
                if (Convert.ToInt32(parent["first"], System.Globalization.CultureInfo.InvariantCulture) == -1)
                {
                    parent["first"] = i + 1;
                    parent["last"] = i + 1;
                }
                else
                {
                    dct["prev"] = parent["last"];
                    var prev = olitems[Convert.ToInt32(parent["last"], System.Globalization.CultureInfo.InvariantCulture)];
                    prev["next"] = i + 1;
                    parent["last"] = i + 1;
                }
                olitems.Add(dct);
            }

            int OlInt(Dictionary<string, object> d, string k, int def = -1) =>
                d.TryGetValue(k, out var o) ? Convert.ToInt32(o, System.Globalization.CultureInfo.InvariantCulture) : def;

            for (int i = 0; i < olitems.Count; i++)
            {
                var ol = olitems[i];
                var txt = "<<";
                if (OlInt(ol, "count", 0) != 0)
                    txt += "/Count " + OlInt(ol, "count", 0).ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (ol.TryGetValue("dest", out var destVal) && destVal != null)
                    txt += destVal.ToString() ?? "";
                if (OlInt(ol, "first", -1) > -1)
                    txt += "/First " + xref[OlInt(ol, "first", -1)].ToString(System.Globalization.CultureInfo.InvariantCulture) + " 0 R";
                if (OlInt(ol, "last", -1) > -1)
                    txt += "/Last " + xref[OlInt(ol, "last", -1)].ToString(System.Globalization.CultureInfo.InvariantCulture) + " 0 R";
                if (OlInt(ol, "next", -1) > -1)
                    txt += "/Next " + xref[OlInt(ol, "next", -1)].ToString(System.Globalization.CultureInfo.InvariantCulture) + " 0 R";
                if (OlInt(ol, "parent", -1) > -1)
                    txt += "/Parent " + xref[OlInt(ol, "parent", -1)].ToString(System.Globalization.CultureInfo.InvariantCulture) + " 0 R";
                if (OlInt(ol, "prev", -1) > -1)
                    txt += "/Prev " + xref[OlInt(ol, "prev", -1)].ToString(System.Globalization.CultureInfo.InvariantCulture) + " 0 R";
                if (ol.TryGetValue("title", out var titleVal) && titleVal != null)
                    txt += "/Title" + titleVal.ToString();

                if (ol.TryGetValue("color", out var colorVal) && colorVal is float[] f3 && f3.Length == 3)
                    txt += "/C[ " + Helpers.EscapePdfArray(f3).Trim('[', ']') + "]";
                if (OlInt(ol, "flags", 0) > 0)
                    txt += "/F " + OlInt(ol, "flags", 0).ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (i == 0)
                    txt += "/Type/Outlines";
                txt += ">>";
                UpdateObject(xref[i], txt);
            }

            InitDoc();
            return toclen;
        }

        private void RemoveTocItemByXref(int xref)
        {
            // "remove" bookmark by letting it point to nowhere
            var pdf = NativePdfDocument;
            var item = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
            mupdf.mupdf.pdf_dict_del(item, mupdf.mupdf.pdf_new_name("Dest"));
            mupdf.mupdf.pdf_dict_del(item, mupdf.mupdf.pdf_new_name("A"));
            var color = mupdf.mupdf.pdf_new_array(pdf, 3);
            for (int i = 0; i < 3; i++)
                mupdf.mupdf.pdf_array_push_real(color, 0.8f);
            mupdf.mupdf.pdf_dict_put(item, mupdf.mupdf.pdf_new_name("C"), color);
        }

        private void UpdateTocItemByXref(int xref, string action = null, string title = null, int flags = 0, bool? collapse = null, float[] color = null)
        {
            // "update" bookmark by letting it point to nowhere
            var pdf = NativePdfDocument;
            var item = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
            if (!string.IsNullOrEmpty(title))
                mupdf.mupdf.pdf_dict_put_text_string(item, mupdf.mupdf.pdf_new_name("Title"), title);
            if (!string.IsNullOrEmpty(action))
            {
                mupdf.mupdf.pdf_dict_del(item, mupdf.mupdf.pdf_new_name("Dest"));
                mupdf.mupdf.pdf_dict_put(item, mupdf.mupdf.pdf_new_name("A"), Helpers.JM_pdf_obj_from_str(pdf, action));
            }
            mupdf.mupdf.pdf_dict_put_int(item, mupdf.mupdf.pdf_new_name("F"), flags);
            if (color != null && color.Length == 3)
            {
                var c = mupdf.mupdf.pdf_new_array(pdf, 3);
                for (int i = 0; i < 3; i++)
                    mupdf.mupdf.pdf_array_push_real(c, color[i]);
                mupdf.mupdf.pdf_dict_put(item, mupdf.mupdf.pdf_new_name("C"), c);
            }
            else if (color != null)
            {
                mupdf.mupdf.pdf_dict_del(item, mupdf.mupdf.pdf_new_name("C"));
            }

            if (collapse.HasValue)
            {
                var countObj = mupdf.mupdf.pdf_dict_get(item, mupdf.mupdf.pdf_new_name("Count"));
                if (countObj.m_internal != null)
                {
                    int i = mupdf.mupdf.pdf_dict_get_int(item, mupdf.mupdf.pdf_new_name("Count"));
                    if ((i < 0 && collapse.Value == false) || (i > 0 && collapse.Value))
                        mupdf.mupdf.pdf_dict_put_int(item, mupdf.mupdf.pdf_new_name("Count"), -i);
                }
            }
        }

        /// <summary>
        /// Delete TOC / bookmark item by index.
        /// </summary>
        public void DelTocItem(int idx)
        {
            int xref = GetOutlineXrefs()[idx];
            RemoveTocItemByXref(xref);
        }

        /// <summary>
        /// Update TOC item by index.
        ///
        /// It allows changing the item's title and link destination.
        ///
        /// Args:
        /// idx:
        /// (int) desired index of the TOC list, as created by get_toc.
        /// dest_dict:
        /// (dict) destination dictionary as created by get_toc(False).
        /// Outrules all other parameters. If None, the remaining parameters
        /// are used to make a dest dictionary.
        /// kind:
        /// (int) kind of link (pymupdf.LINK_GOTO, etc.). If None, then only
        /// the title will be updated. If pymupdf.LINK_NONE, the TOC item will
        /// be deleted.
        /// pno:
        /// (int) page number (1-based like in get_toc). Required if
        /// pymupdf.LINK_GOTO.
        /// uri:
        /// (str) the URL, required if pymupdf.LINK_URI.
        /// title:
        /// (str) the new title. No change if None.
        /// to:
        /// (point-like) destination on the target page. If omitted, (72, 36)
        /// will be used as target coordinates.
        /// filename:
        /// (str) destination filename, required for pymupdf.LINK_GOTOR and
        /// pymupdf.LINK_LAUNCH.
        /// name:
        /// (str) a destination name for pymupdf.LINK_NAMED.
        /// zoom:
        /// (float) a zoom factor for the target location (pymupdf.LINK_GOTO).
        /// </summary>
        public void SetTocItem(int idx, Dictionary<string, object> destDict = null, int? kind = null, int? pno = null,
            string uri = null, string title = null, Point to = null, string filename = null, float zoom = 0)
        {
            int xref = GetOutlineXrefs()[idx];
            int pageXref = 0;

            if (destDict != null)
            {
                if (destDict.ContainsKey("kind") && Convert.ToInt32(destDict["kind"], System.Globalization.CultureInfo.InvariantCulture) == Constants.LINK_GOTO)
                {
                    int dpno = Convert.ToInt32(destDict["page"], System.Globalization.CultureInfo.InvariantCulture);
                    pageXref = PageXref(dpno);
                    double pageHeight = PageCropBox(dpno).Height;
                    Point p = destDict.ContainsKey("to") && destDict["to"] is Point ? new Point((Point)destDict["to"]) : new Point(72, 36);
                    p.Y = pageHeight - p.Y;
                    destDict["to"] = p;
                }
                string action = BuildDestAction(pageXref, destDict);
                if (!action.StartsWith("/A", StringComparison.Ordinal))
                    throw new ValueErrorException("bad bookmark dest");

                float[] color = null;
                if (destDict.ContainsKey("color") && destDict["color"] != null)
                {
                    if (destDict["color"] is float[] fc)
                        color = fc;
                    else if (destDict["color"] is double[] dc)
                        color = new float[] { (float)dc[0], (float)dc[1], (float)dc[2] };
                    else if (destDict["color"] is IList list && list.Count == 3)
                        color = new float[] { Convert.ToSingle(list[0]), Convert.ToSingle(list[1]), Convert.ToSingle(list[2]) };
                    if (color == null || color.Length != 3 || color[0] < 0 || color[1] < 0 || color[2] < 0 || color[0] > 1 || color[1] > 1 || color[2] > 1)
                        throw new ValueErrorException("bad color value");
                }
                bool bold = destDict.ContainsKey("bold") && Convert.ToBoolean(destDict["bold"], System.Globalization.CultureInfo.InvariantCulture);
                bool italic = destDict.ContainsKey("italic") && Convert.ToBoolean(destDict["italic"], System.Globalization.CultureInfo.InvariantCulture);
                int flags = (italic ? 1 : 0) + (bold ? 2 : 0);
                bool? collapseState = destDict.ContainsKey("collapse") ? (bool?)Convert.ToBoolean(destDict["collapse"], System.Globalization.CultureInfo.InvariantCulture) : null;

                UpdateTocItemByXref(xref, action: action.Substring(2), title: title, color: color, flags: flags, collapse: collapseState);
                return;
            }

            // if kind == LINK_NONE:  # delete bookmark item
            if (kind.HasValue && kind.Value == Constants.LINK_NONE)
            {
                DelTocItem(idx);
                return;
            }
            // if kind is None and title is None:  # treat as no-op
            if (!kind.HasValue && title == null)
                return;
            // if kind is None:  # only update title text
            if (!kind.HasValue)
            {
                UpdateTocItemByXref(xref, action: null, title: title);
                return;
            }

            int k = kind.Value;
            if (k == Constants.LINK_GOTO)
            {
                if (!pno.HasValue || pno.Value < 1 || pno.Value > PageCount)
                    throw new ValueErrorException("bad page number");
                pageXref = PageXref(pno.Value - 1);
                double pageHeight = PageCropBox(pno.Value - 1).Height;
                if (to == null)
                    to = new Point(72, pageHeight - 36);
                else
                {
                    to = new Point(to);
                    to.Y = pageHeight - to.Y;
                }
            }

            var ddict = new Dictionary<string, object>
            {
                ["kind"] = k,
                ["to"] = to,
                ["uri"] = uri,
                ["page"] = pno.HasValue ? pno.Value : -1,
                ["file"] = filename,
                ["zoom"] = zoom
            };
            string action2 = BuildDestAction(pageXref, ddict);
            if (action2 == "" || !action2.StartsWith("/A", StringComparison.Ordinal))
                throw new ValueErrorException("bad bookmark dest");

            UpdateTocItemByXref(xref, action: action2.Substring(2), title: title);
        }

        private void CollectToc(mupdf.FzOutline ol, List<(int, string, int, Dictionary<string, object>)> result, int level, bool simple)
        {
            while (ol.m_internal != null)
            {
                string title = ol.m_internal.title ?? "";
                int page = ol.m_internal.page.page;
                Dictionary<string, object> link = null;
                if (!simple)
                {
                    link = new Dictionary<string, object> { ["kind"] = (int)LinkType.Goto, ["page"] = page, ["title"] = title };
                }
                result.Add((level, title, page, link));
                var down = new mupdf.FzOutline(ol.m_internal.down);
                if (down.m_internal != null) CollectToc(down, result, level + 1, simple);
                ol = new mupdf.FzOutline(ol.m_internal.next);
            }
        }

        /// <summary>
        /// Load first outline.
        /// </summary>
        public Outline GetOutline()
        {
            var ol = mupdf.mupdf.fz_load_outline(NativeDocument);
            if (ol.m_internal == null) return null;
            return new Outline(ol);
        }

        /// <summary>
        /// Get list of outline xref numbers.
        /// </summary>
        public List<int> GetOutlineXrefs()
        {
            var xrefs = new List<int>();
            var pdf = Helpers.AsPdfDocument(NativeDocument, required: false);
            if (pdf == null || pdf.m_internal == null)
                return xrefs;
            var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            if (root.m_internal == null)
                return xrefs;
            var olroot = mupdf.mupdf.pdf_dict_get(root, mupdf.mupdf.pdf_new_name("Outlines"));
            if (olroot.m_internal == null)
                return xrefs;
            var first = mupdf.mupdf.pdf_dict_get(olroot, mupdf.mupdf.pdf_new_name("First"));
            if (first.m_internal == null)
                return xrefs;

            void Walk(mupdf.PdfObj item)
            {
                if (item == null || item.m_internal == null)
                    return;
                xrefs.Add(mupdf.mupdf.pdf_to_num(item));
                var down = mupdf.mupdf.pdf_dict_get(item, mupdf.mupdf.pdf_new_name("First"));
                if (down.m_internal != null)
                    Walk(down);
                var next = mupdf.mupdf.pdf_dict_get(item, mupdf.mupdf.pdf_new_name("Next"));
                if (next.m_internal != null)
                    Walk(next);
            }

            Walk(first);
            return xrefs;
        }

        // ─── Page Operations ────────────────────────────────────────────

        /// <summary>
        /// Create and return a new page object.
        /// </summary>
        public Page NewPage(int pageNo = -1, float width = 595, float height = 842)
        {
            EnsurePdf();
            var pdf = NativePdfDocument;
            if (pageNo < -1)
                throw new ValueErrorException(Constants.MSG_BAD_PAGENO);
            if (pageNo < 0 || pageNo > PageCount) pageNo = PageCount;
            var mediabox = mupdf.mupdf.fz_make_rect(0, 0, width, height);
            var newPage = mupdf.mupdf.pdf_add_page(pdf, mediabox, 0, new mupdf.PdfObj(), new mupdf.FzBuffer());
            mupdf.mupdf.pdf_insert_page(pdf, pageNo, newPage);
            ResetPageRefsInternal();
            return LoadPage(pageNo);
        }

        /// <summary>
        /// Delete one page from a PDF.
        /// </summary>
        public void DeletePage(int pno)
        {
            EnsurePdfOpenForDeletePages();
            pno = Helpers.ResolvePageIndex(PageCount, pno);
            var pdf = NativePdfDocument;
            mupdf.mupdf.pdf_delete_page(pdf, pno);
            if (pdf.m_internal.rev_page_map != null)
                mupdf.mupdf.ll_pdf_drop_page_tree(pdf.m_internal);
            if (_suppressPageRefReset == 0)
                ResetPageRefsInternal();
        }

        /// <summary>
        /// Delete pages from a PDF.
        /// </summary>
        public void DeletePages(params int[] pages)
        {
            var sorted = pages.OrderByDescending(x => x).ToArray();
            _suppressPageRefReset++;
            try
            {
                foreach (var p in sorted)
                    DeletePage(p);
            }
            finally
            {
                _suppressPageRefReset--;
            }
            ResetPageRefsInternal();
        }

        /// <summary>
        /// Delete pages from a PDF.
        /// </summary>
        public void DeletePages(int fromPage, int toPage)
        {
            _suppressPageRefReset++;
            try
            {
                for (int i = toPage; i >= fromPage; i--)
                    DeletePage(i);
            }
            finally
            {
                _suppressPageRefReset--;
            }
            ResetPageRefsInternal();
        }

        /// <summary>
        /// Page indices for a half-open <c>[start, stop)</c> slice with <paramref name="step"/> (Python <c>range(start, stop, step)</c> semantics).
        /// </summary>
        private static List<int> GetSlicePageIndices(int start, int stop, int step, int pageCount)
        {
            if (step == 0)
                throw new ValueErrorException(Constants.MSG_BAD_PAGENO);
            int s = start;
            int e = stop;
            int pc = pageCount;
            while (s < 0) s += pc;
            if (s >= pc)
                throw new ValueErrorException(Constants.MSG_BAD_PAGENO);
            while (e < 0) e += pc;
            if (e > pc)
                throw new ValueErrorException(Constants.MSG_BAD_PAGENO);
            var indices = new List<int>();
            if (step > 0)
            {
                for (int i = s; i < e; i += step)
                    indices.Add(i);
            }
            else
            {
                for (int i = s; i > e; i += step)
                    indices.Add(i);
            }
            return indices;
        }

        /// <summary>
        /// Delete pages in a half-open index interval <c>[start, stop)</c> with optional <paramref name="step"/> (Python <c>del doc[start:stop:step]</c>, PDF only).
        /// </summary>
        public void DeletePagesBySlice(int start, int stop, int step = 1)
        {
            EnsurePdfOpenForDeletePages();
            var indices = GetSlicePageIndices(start, stop, step, PageCount);
            if (indices.Count == 0) return;
            DeletePages(indices.ToArray());
        }

        /// <summary>
        /// Load each page in a slice (Python <c>doc[start:stop:step]</c> when the subscript is a <c>slice</c>).
        /// </summary>
        public List<Page> LoadPagesBySlice(int start, int stop, int step = 1)
        {
            EnsureNotClosed();
            var indices = GetSlicePageIndices(start, stop, step, PageCount);
            var list = new List<Page>(indices.Count);
            foreach (int i in indices)
                list.Add(LoadPage(i));
            return list;
        }

        /// <summary>
        /// Insert a new page with optional text.
        /// </summary>
        public Page InsertPage(int pno = -1, string text = null, float fontsize = 11, float width = 595, float height = 842, string fontname = "helv", float[] color = null)
        {
            var page = NewPage(pno, width, height);
            if (!string.IsNullOrEmpty(text))
            {
                page.InsertText(new Point(72, 72), text, fontsize: fontsize, fontname: fontname, color: color);
            }
            return page;
        }

        /// <summary>
        /// Copy a page within a PDF document.
        ///
        /// This will only create another reference of the same page object.
        /// </summary>
        public void CopyPage(int pno, int to = -1)
        {
            EnsurePdfOpenForDeletePages();
            var pdf = NativePdfDocument;
            int pc = PageCount;
            pno = Helpers.ResolvePageIndex(pc, pno);
            if (to < 0) to = pc;
            var page = mupdf.mupdf.pdf_lookup_page_obj(pdf, pno);
            var newPage = mupdf.mupdf.pdf_deep_copy_obj(page);
            mupdf.mupdf.pdf_insert_page(pdf, to, newPage);
            if (_suppressPageRefReset == 0)
                ResetPageRefsInternal();
        }

        /// <summary>
        /// Make a full page duplicate including annotations and content streams.
        /// </summary>
        public void FullcopyPage(int pno, int to = -1)
        {
            EnsurePdfOpenForDeletePages();
            var pdf = NativePdfDocument;
            int pc = mupdf.mupdf.pdf_count_pages(pdf);
            pno = Helpers.ResolvePageIndex(pc, pno);
            if (to < -1 || to >= pc) to = pc;

            try
            {
                var page1 = mupdf.mupdf.pdf_resolve_indirect(mupdf.mupdf.pdf_lookup_page_obj(pdf, pno));
                var page2 = mupdf.mupdf.pdf_deep_copy_obj(page1);

                var oldAnnots = mupdf.mupdf.pdf_dict_get(page2, mupdf.mupdf.pdf_new_name("Annots"));
                if (oldAnnots.m_internal != null)
                {
                    int n = mupdf.mupdf.pdf_array_len(oldAnnots);
                    var newAnnots = mupdf.mupdf.pdf_new_array(pdf, n);
                    for (int i = 0; i < n; i++)
                    {
                        var o = mupdf.mupdf.pdf_array_get(oldAnnots, i);
                        var subtype = mupdf.mupdf.pdf_dict_get(o, mupdf.mupdf.pdf_new_name("Subtype"));
                        if (mupdf.mupdf.pdf_name_eq(subtype, mupdf.mupdf.pdf_new_name("Popup")) != 0)
                            continue;
                        if (mupdf.mupdf.pdf_dict_gets(o, "IRT").m_internal != null)
                            continue;
                        var copyO = mupdf.mupdf.pdf_deep_copy_obj(mupdf.mupdf.pdf_resolve_indirect(o));
                        int xref = mupdf.mupdf.pdf_create_object(pdf);
                        mupdf.mupdf.pdf_update_object(pdf, xref, copyO);
                        copyO = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
                        mupdf.mupdf.pdf_dict_del(copyO, mupdf.mupdf.pdf_new_name("Popup"));
                        mupdf.mupdf.pdf_dict_del(copyO, mupdf.mupdf.pdf_new_name("P"));
                        mupdf.mupdf.pdf_array_push(newAnnots, copyO);
                    }
                    mupdf.mupdf.pdf_dict_put(page2, mupdf.mupdf.pdf_new_name("Annots"), newAnnots);
                }

                var contentsObj = mupdf.mupdf.pdf_dict_get(page1, mupdf.mupdf.pdf_new_name("Contents"));
                if (contentsObj.m_internal != null)
                {
                    mupdf.FzBuffer res;
                    if (mupdf.mupdf.pdf_is_array(contentsObj) != 0)
                    {
                        res = mupdf.mupdf.fz_new_buffer(1024);
                        int arrLen = mupdf.mupdf.pdf_array_len(contentsObj);
                        for (int i = 0; i < arrLen; i++)
                        {
                            var item = mupdf.mupdf.pdf_array_get(contentsObj, i);
                            if (mupdf.mupdf.pdf_is_stream(item) != 0)
                            {
                                var buf = mupdf.mupdf.pdf_load_stream(item);
                                mupdf.mupdf.fz_append_buffer(res, buf);
                            }
                        }
                    }
                    else if (mupdf.mupdf.pdf_is_stream(contentsObj) != 0)
                    {
                        res = mupdf.mupdf.pdf_load_stream(contentsObj);
                    }
                    else
                    {
                        res = null;
                    }

                    if (res != null && res.m_internal != null)
                    {
                        var placeholder = Helpers.BufferFromBytes(System.Text.Encoding.UTF8.GetBytes(" "));
                        var newContents = mupdf.mupdf.pdf_add_stream(pdf, placeholder, new mupdf.PdfObj(), 0);
                        mupdf.mupdf.pdf_update_stream(pdf, newContents, res, 1);
                        mupdf.mupdf.pdf_dict_put(page2, mupdf.mupdf.pdf_new_name("Contents"), newContents);
                    }
                }

                int newXref = mupdf.mupdf.pdf_create_object(pdf);
                mupdf.mupdf.pdf_update_object(pdf, newXref, page2);
                page2 = mupdf.mupdf.pdf_new_indirect(pdf, newXref, 0);
                mupdf.mupdf.pdf_insert_page(pdf, to, page2);
            }
            finally
            {
                mupdf.mupdf.ll_pdf_drop_page_tree(pdf.m_internal);
            }
            if (_suppressPageRefReset == 0)
                ResetPageRefsInternal();
        }

        /// <summary>
        /// Move a page within a PDF document.
        /// </summary>
        public void MovePage(int pno, int to = -1)
        {
            EnsurePdfOpenForDeletePages();
            int pc = PageCount;
            pno = Helpers.ResolvePageIndex(pc, pno);
            if (to < 0) to = pc;
            if (pno == to) return;
            _suppressPageRefReset++;
            try
            {
                CopyPage(pno, to);
                if (pno < to) DeletePage(pno);
                else DeletePage(pno + 1);
            }
            finally
            {
                _suppressPageRefReset--;
            }
            ResetPageRefsInternal();
        }

        /// <summary>
        /// Build sub-pdf with page numbers in the list.
        /// </summary>
        public void Select(int[] pages)
        {
            if (pages == null)
                throw new ValueErrorException("sequence required");
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            if (!IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);
            var pdf = NativePdfDocument;
            int n = pages.Length;
            int pc = PageCount;
            if (n == 0 || pages.Min() < 0 || pages.Max() >= pc)
                throw new ValueErrorException(Constants.MSG_BAD_PAGENO);

            var pageList = new mupdf.vectori();
            foreach (var p in pages) pageList.Add(p);
            mupdf.mupdf.pdf_rearrange_pages2(pdf, pageList,
                mupdf.pdf_clean_options_structure.PDF_CLEAN_STRUCTURE_KEEP);
            ResetPageRefsInternal();
        }

        /// <summary>
        /// Make a fresh copy of a page.
        /// </summary>
        public Page ReloadPage(Page page) => LoadPage(page.Number);

        // ─── Save / Write ───────────────────────────────────────────────

        /// <summary>
        /// Save document to file.
        /// </summary>
        public void Save3(string filename, bool garbage = false, bool clean = false, bool deflate = false,
            bool deflateImages = false, bool deflateFonts = false, bool incremental = false,
            bool ascii = false, bool expand = false, bool linear = false, bool noNewId = false,
            bool pretty = false, int encryption = 1, int permissions = 4095,
            string ownerPw = null, string userPw = null, bool preserveMetadata = true,
            int useObjstms = 0, int compressionEffort = 0)
        {
            var pdf = NativePdfDocument;
            var opts = new mupdf.PdfWriteOptions();
            if (garbage) opts.do_garbage = 1;
            if (clean) opts.do_clean = 1;
            if (deflate) opts.do_compress = 1;
            if (deflateImages) opts.do_compress_images = 1;
            if (deflateFonts) opts.do_compress_fonts = 1;
            if (ascii) opts.do_ascii = 1;
            if (linear) opts.do_linear = 1;
            if (incremental) opts.do_incremental = 1;
            if (pretty) opts.do_pretty = 1;
            if (useObjstms > 0) opts.do_use_objstms = 1;
            mupdf.mupdf.pdf_save_document(pdf, filename, opts);
        }

        public void Save1(
            string filename,
            Annot annot,
            int garbage = 0,
            int clean = 0,
            int deflate = 0,
            int deflateImages = 0,
            int deflateFonts = 0,
            int incremental = 0,
            int ascii = 0,
            int expand = 0,
            int linear = 0,
            int noNewId = 0,
            int appearance = 0,
            int pretty = 0,
            int encryption = 1,
            int permissions = 4095,
            string ownerPW = null,
            string userPW = null,
            int preserveMetadata = 1,
            int useObjstms = 0,
            int compressionEffort = 0
        )
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document is closed or encrypted");

            if (PageCount < 1)
                throw new Exception("cannot save with zero pages");

            if ((userPW != null && userPW.Length > 40) || (ownerPW != null && ownerPW.Length > 40))
                throw new Exception("password length must not exceed 40");

            var pdf = NativeDocument.pdf_document_from_fz_document();
            var opts = new mupdf.PdfWriteOptions();
            //opts.do_incremental = incremental;
            opts.do_ascii = ascii;
            opts.do_compress = deflate;
            opts.do_compress_images = deflateImages;
            opts.do_compress_fonts = deflateFonts;
            opts.do_decompress = expand;
            opts.do_garbage = garbage;
            opts.do_pretty = pretty;
            opts.do_linear = linear;
            opts.do_clean = clean;
            opts.do_sanitize = clean;
            opts.dont_regenerate_id = noNewId;
            opts.do_appearance = appearance;
            opts.do_encrypt = encryption;
            opts.permissions = permissions;
            if (ownerPW != null)
                opts.opwd_utf8_set_value(ownerPW);
            else if (userPW != null)
                opts.opwd_utf8_set_value(userPW);
            
            if (userPW != null)
                opts.upwd_utf8_set_value(userPW);
            opts.do_preserve_metadata = preserveMetadata;
            opts.do_use_objstms = useObjstms;
            opts.compression_effort = compressionEffort;

            pdf.m_internal.resynth_required = 0;
            //Utils.EmbeddedClean(pdf);

            pdf.pdf_save_document(filename, opts);
            opts.Dispose();
            pdf.Dispose();
        }

        /// <summary>
        /// Save document to file.
        /// </summary>
        /*
        public void Save(Stream output, bool garbage = false, bool clean = false, bool deflate = false,
            bool deflateImages = false, bool deflateFonts = false, bool incremental = false,
            bool ascii = false, bool expand = false, bool linear = false, bool pretty = false,
            int encryption = 1, int permissions = 4095, string ownerPw = null, string userPw = null,
            bool preserveMetadata = true, int useObjstms = 0, int compressionEffort = 0)
        {
            var data = Write(garbage: garbage, clean: clean, deflate: deflate, deflateImages: deflateImages,
                deflateFonts: deflateFonts, incremental: incremental, ascii: ascii, expand: expand,
                linear: linear, pretty: pretty, encryption: encryption, permissions: permissions,
                ownerPw: ownerPw, userPw: userPw, preserveMetadata: preserveMetadata,
                useObjstms: useObjstms, compressionEffort: compressionEffort);
            output.Write(data, 0, data.Length);
        }
        */

        /// <summary>
        /// Save document to file or stream.
        /// </summary>
        public void Save(
            object filename, 
            int garbage = 0, 
            int clean = 0, 
            int deflate = 0, 
            int deflate_images = 0, 
            int deflate_fonts = 0, 
            int incremental = 0, 
            int ascii = 0, 
            int expand = 0, 
            int linear = 0, 
            int no_new_id = 0, 
            int appearance = 0, 
            int pretty = 0, 
            int encryption = 1, 
            int permissions = 4095, 
            string owner_pw = null, 
            string user_pw = null, 
            int preserve_metadata = 1, 
            int use_objstms = 0, 
            int compression_effort = 0, 
            bool raise_on_repair = false)
        {
            bool is_repaired_pre = IsRepaired;
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            string fname = null;
            Stream stream = null;
            if (filename is string s)
                fname = s;
            else if (filename is Stream st)
                stream = st;
            else
                throw new ValueErrorException("filename must be str or Stream");
            if (fname == Name && incremental == 0)
                throw new ValueErrorException("save to original must be incremental");
            if (linear != 0 && use_objstms != 0)
                throw new ValueErrorException("'linear' and 'use_objstms' cannot both be requested");
            if (PageCount < 1)
                throw new ValueErrorException("cannot save with zero pages");
            if (incremental != 0)
            {
                if (Name != fname || StreamData != null)
                    throw new ValueErrorException("incremental needs original file");
            }
            if ((user_pw != null && user_pw.Length > 40) || (owner_pw != null && owner_pw.Length > 40))
                throw new ValueErrorException("password length must not exceed 40");
            
            var pdf = NativePdfDocument;
            var opts = new mupdf.PdfWriteOptions();
            opts.do_incremental = incremental;
            opts.do_ascii = ascii;
            opts.do_compress = deflate;
            opts.do_compress_images = deflate_images;
            opts.do_compress_fonts = deflate_fonts;
            opts.do_decompress = expand;
            opts.do_garbage = garbage;
            opts.do_pretty = pretty;
            opts.do_linear = linear;
            opts.do_clean = clean;
            opts.do_sanitize = clean;
            opts.dont_regenerate_id = no_new_id;
            opts.do_appearance = appearance;
            opts.do_encrypt = encryption;
            opts.permissions = permissions;
            if (owner_pw != null)
                opts.opwd_utf8_set_value(owner_pw);
            else if (user_pw != null)
                opts.opwd_utf8_set_value(user_pw);
            if (user_pw != null)
                opts.upwd_utf8_set_value(user_pw);
            opts.do_preserve_metadata = preserve_metadata;
            opts.do_use_objstms = use_objstms;
            opts.compression_effort = compression_effort;

            mupdf.FzOutput fzOut = null;
            pdf.m_internal.resynth_required = 0;
            Helpers.JM_embedded_clean(pdf);
            if (no_new_id == 0)
            {
                Helpers.JM_ensure_identity(pdf); // not implemented
            }
            if (fname != null)
            {
                mupdf.mupdf.pdf_save_document(pdf, fname, opts);
            }
            else if (stream != null)
            {
                var buf = mupdf.mupdf.fz_new_buffer(8192);
                fzOut = new mupdf.FzOutput(buf);
                mupdf.mupdf.pdf_write_document(pdf, fzOut, opts);
                fzOut.fz_close_output();
                var data = buf.fz_buffer_extract();
                stream.Write(data, 0, data.Length);
            }
            if (raise_on_repair)
            {
                if (IsRepaired && !is_repaired_pre)
                    throw new Exception("Document save did a repair");
            }
        }

        /// <summary>
        /// Write document to bytes.
        /// </summary>
        public byte[] Write(bool garbage = false, bool clean = false, bool deflate = false,
            bool deflateImages = false, bool deflateFonts = false, bool incremental = false,
            bool ascii = false, bool expand = false, bool linear = false, bool noNewId = false,
            bool pretty = false, int encryption = 1, int permissions = 4095,
            string ownerPw = null, string userPw = null, bool preserveMetadata = true,
            int useObjstms = 0, int compressionEffort = 0)
        {
            var pdf = NativePdfDocument;
            var opts = new mupdf.PdfWriteOptions();
            if (garbage) opts.do_garbage = 1;
            if (clean) opts.do_clean = 1;
            if (deflate) opts.do_compress = 1;
            if (deflateImages) opts.do_compress_images = 1;
            if (deflateFonts) opts.do_compress_fonts = 1;
            if (ascii) opts.do_ascii = 1;
            if (linear) opts.do_linear = 1;
            if (incremental) opts.do_incremental = 1;
            if (pretty) opts.do_pretty = 1;
            if (useObjstms > 0) opts.do_use_objstms = 1;
            var buf = mupdf.mupdf.fz_new_buffer(8192);
            var out_ = new mupdf.FzOutput(buf);
            mupdf.mupdf.pdf_write_document(pdf, out_, opts);
            out_.fz_close_output();
            return buf.fz_buffer_extract();
        }

        /// <summary>
        /// Convert document to bytes.
        /// </summary>
        public byte[] ToBytes(bool garbage = false, bool clean = false, bool deflate = false) =>
            Write(garbage: garbage, clean: clean, deflate: deflate);

        /// <summary>
        /// Convert document to a PDF, selecting page range and optional rotation. Output bytes object.
        /// </summary>
        public byte[] ConvertToPdf(int fromPage = 0, int toPage = -1, int rotate = 0)
        {
            if (toPage < 0) toPage = PageCount - 1;
            var buf = mupdf.mupdf.fz_new_buffer(8192);
            var output = new mupdf.FzOutput(buf);
            var writer = new mupdf.FzDocumentWriter(output, "pdf", "");
            writer.fz_write_document(NativeDocument);
            writer.fz_close_document_writer();
            output.fz_close_output();
            return buf.fz_buffer_extract();
        }

        /// <summary>
        /// Save document incrementally.
        /// </summary>
        public void SaveIncr() => Save(Name, incremental: 1);

        /// <summary>
        /// Check whether incremental saves are possible.
        /// </summary>
        public bool CanSaveIncrementally()
        {
            try { return mupdf.mupdf.pdf_can_be_saved_incrementally(NativePdfDocument) != 0; }
            catch { return false; }
        }

        /// <summary>
        /// Save PDF using some different defaults.
        /// </summary>
        public void EzSave(string filename, int garbage = 1, int clean = 0, int deflate = 1,
            int deflateImages = 1, int deflateFonts = 1, int pretty = 0, int linear = 0,
            int ascii = 0, int encryption = 1, int noNewId = 1, int useObjstms = 1)
        {
            Save(filename, garbage: garbage, clean: clean, deflate: deflate, deflate_images: deflateImages,
                deflate_fonts: deflateFonts, pretty: pretty, linear: linear, ascii: ascii, encryption: encryption,
                no_new_id: noNewId, use_objstms: useObjstms);
        }

        // ─── Xref Operations ────────────────────────────────────────────

        /// <summary>PyMuPDF <c>_INRANGE(xref, 1, pdf_xref_len - 1)</c> for indirect objects (not the trailer).</summary>
        private void EnsureValidXrefPositiveIndirect(int xref)
        {
            var pdf = NativePdfDocument;
            int len = mupdf.mupdf.pdf_xref_len(pdf);
            if (xref < 1 || xref > len - 1)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);
        }

        /// <summary>
        /// PyMuPDF allows <paramref name="xref"/> in <c>[1, pdf_xref_len - 1]</c> or <c>-1</c> (trailer).
        /// Same rule as <c>xref_get_key</c>, <c>xref_object</c>, <c>xref_stream</c>, etc.
        /// </summary>
        private void EnsureValidXrefDict(int xref)
        {
            if (xref == -1) return;
            EnsureValidXrefPositiveIndirect(xref);
        }

        /// <summary>PyMuPDF <c>update_stream</c>: <c>xref &lt; 1 or xref &gt; pdf_xref_len</c> is invalid.</summary>
        private void EnsureValidXrefForUpdateStream(int xref)
        {
            var pdf = NativePdfDocument;
            int len = mupdf.mupdf.pdf_xref_len(pdf);
            if (xref < 1 || xref > len)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);
        }

        /// <summary>
        /// Get xref of page number.
        /// </summary>
        public int PageXref(int pno)
        {
            pno = Helpers.ResolvePageIndex(PageCount, pno);
            return mupdf.mupdf.pdf_to_num(mupdf.mupdf.pdf_lookup_page_obj(NativePdfDocument, pno));
        }

        /// <summary>
        /// Get string representation of a PDF object (PyMuPDF <c>xref_object</c>: <c>pdf_print_obj</c> into a buffer).
        /// </summary>
        public string XrefObject(int xref, bool compressed = false, bool ascii = false)
        {
            EnsureNotClosed();
            EnsureValidXrefDict(xref);
            var pdf = NativePdfDocument;
            var obj = xref > 0 ? mupdf.mupdf.pdf_load_object(pdf, xref) : mupdf.mupdf.pdf_trailer(pdf);
            var resolved = mupdf.mupdf.pdf_resolve_indirect(obj);
            return PdfObjPrintToString(resolved, compressed ? 1 : 0, ascii ? 1 : 0);
        }

        /// <summary>
        /// Check if xref is a stream object.
        /// </summary>
        public bool XrefIsStream(int xref = 0)
        {
            try { return mupdf.mupdf.pdf_obj_num_is_stream(NativePdfDocument, xref) != 0; }
            catch { return false; }
        }

        /// <summary>
        /// Check if xref is a font.
        /// </summary>
        public bool XrefIsFont(int xref) => XrefGetKey(xref, "Type").value == "/Font";
        /// <summary>
        /// Check if xref is an image.
        /// </summary>
        public bool XrefIsImage(int xref) => XrefGetKey(xref, "Subtype").value == "/Image";
        /// <summary>
        /// Check if xref is a Form XObject.
        /// </summary>
        public bool XrefIsXobject(int xref) => XrefGetKey(xref, "Subtype").value == "/Form";

        /// <summary>
        /// Get decompressed stream of a PDF object.
        /// </summary>
        public byte[] XrefStream(int xref)
        {
            EnsureNotClosed();
            if (IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            EnsureValidXrefDict(xref);
            var pdf = NativePdfDocument;
            var obj = xref >= 0 ? mupdf.mupdf.pdf_new_indirect(pdf, xref, 0) : mupdf.mupdf.pdf_trailer(pdf);
            if (mupdf.mupdf.pdf_is_stream(obj) != 0)
            {
                var res = mupdf.mupdf.pdf_load_stream_number(pdf, xref);
                return res.fz_buffer_extract();
            }
            return null;
        }

        /// <summary>
        /// Get raw (compressed) stream of a PDF object.
        /// </summary>
        public byte[] XrefStreamRaw(int xref)
        {
            EnsureNotClosed();
            if (IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            EnsureValidXrefDict(xref);
            var pdf = NativePdfDocument;
            var obj = xref >= 0 ? mupdf.mupdf.pdf_new_indirect(pdf, xref, 0) : mupdf.mupdf.pdf_trailer(pdf);
            if (mupdf.mupdf.pdf_is_stream(obj) != 0)
            {
                var res = mupdf.mupdf.pdf_load_raw_stream_number(pdf, xref);
                return res.fz_buffer_extract();
            }
            return null;
        }

        /// <summary>
        /// Same as PyMuPDF <c>JM_object_to_buffer</c> + UTF-8 decode (Python uses raw-unicode-escape; PDF syntax is ASCII-safe here).
        /// </summary>
        private static string PdfObjPrintToString(mupdf.PdfObj obj, int compress, int ascii)
        {
            try
            {
                if (obj?.m_internal == null) return "";
                using (var buf = mupdf.mupdf.fz_new_buffer(512))
                using (var output = new mupdf.FzOutput(buf))
                {
                    output.pdf_print_obj(obj, compress, ascii);
                    output.fz_close_output();
                    buf.fz_terminate_buffer();
                    return System.Text.Encoding.UTF8.GetString(Helpers.BufferToBytes(buf));
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Serialize a <see cref="mupdf.PdfObj"/> for <see cref="XrefGetKey"/> when <c>pdf_to_str_buf</c> is empty
        /// (observed for some arrays/dicts in the C# binding). Fallback matches PyMuPDF <c>JM_object_to_buffer(sub, 1, 0)</c>.
        /// </summary>
        private static string PdfObjToKeyValueString(mupdf.PdfObj sub)
        {
            try
            {
                if (sub?.m_internal == null) return "";
                var s = sub.pdf_to_str_buf();
                if (!string.IsNullOrEmpty(s))
                    return s;
                return PdfObjPrintToString(sub, 1, 0);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Get type and value of a PDF dictionary key.
        /// </summary>
        public (string type, string value) XrefGetKey(int xref, string key)
        {
            EnsureNotClosed();
            EnsureValidXrefDict(xref);
            var pdf = NativePdfDocument;
            var obj = xref > 0 ? mupdf.mupdf.pdf_load_object(pdf, xref) : mupdf.mupdf.pdf_trailer(pdf);
            if (obj.m_internal == null) return ("null", "null");
            // Prefer path lookup; fall back to direct name (PyMuPDF often uses plain keys like "CropBox").
            var sub = mupdf.mupdf.pdf_dict_getp(obj, key);
            if (sub.m_internal == null && !string.IsNullOrEmpty(key) && key[0] != '/')
                sub = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name(key));
            if (sub.m_internal == null) return ("null", "null");

            if (mupdf.mupdf.pdf_is_indirect(sub) != 0) return ("xref", $"{mupdf.mupdf.pdf_to_num(sub)} 0 R");
            if (mupdf.mupdf.pdf_is_int(sub) != 0) return ("int", $"{mupdf.mupdf.pdf_to_int(sub)}");
            if (mupdf.mupdf.pdf_is_real(sub) != 0) return ("float", PdfObjToKeyValueString(sub));
            if (mupdf.mupdf.pdf_is_null(sub) != 0) return ("null", "null");
            if (mupdf.mupdf.pdf_is_bool(sub) != 0) return ("bool", mupdf.mupdf.pdf_to_bool(sub) != 0 ? "true" : "false");
            if (mupdf.mupdf.pdf_is_name(sub) != 0) return ("name", $"/{mupdf.mupdf.pdf_to_name(sub)}");
            if (mupdf.mupdf.pdf_is_string(sub) != 0) return ("string", mupdf.mupdf.pdf_to_text_string(sub));
            if (mupdf.mupdf.pdf_is_array(sub) != 0) return ("array", PdfObjToKeyValueString(sub));
            if (mupdf.mupdf.pdf_is_dict(sub) != 0) return ("dict", PdfObjToKeyValueString(sub));
            return ("unknown", PdfObjToKeyValueString(sub));
        }

        /// <summary>
        /// Get list of PDF dictionary keys.
        /// </summary>
        public List<string> XrefGetKeys(int xref)
        {
            EnsureNotClosed();
            EnsureValidXrefDict(xref);
            var pdf = NativePdfDocument;
            var obj = xref > 0 ? mupdf.mupdf.pdf_load_object(pdf, xref) : mupdf.mupdf.pdf_trailer(pdf);
            int n = mupdf.mupdf.pdf_dict_len(obj);
            var rc = new List<string>(n);
            for (int i = 0; i < n; i++)
                rc.Add(mupdf.mupdf.pdf_to_name(mupdf.mupdf.pdf_dict_get_key(obj, i)));
            return rc;
        }

        /// <summary>
        /// Set a PDF dictionary key.
        /// </summary>
        public void XrefSetKey(int xref, string key, string value)
        {
            EnsureNotClosed();
            EnsureValidXrefDict(xref);
            var pdf = NativePdfDocument;
            var obj = xref > 0 ? mupdf.mupdf.pdf_load_object(pdf, xref) : mupdf.mupdf.pdf_trailer(pdf);
            // PyMuPDF xref_set_key(..., "null"): JM_pdf_obj_from_str("null") yields a real PDF null there; this binding's
            // pdf_parse_stm_obj path can fall back to a text string. Remove the entry instead (xref_copy / scrubber intent).
            if (value != null && string.Equals(value.Trim(), "null", StringComparison.Ordinal))
            {
                mupdf.mupdf.pdf_dict_dels(obj, key);
                if (xref != -1) mupdf.mupdf.pdf_update_object(pdf, xref, obj);
                return;
            }
            var newObj = Helpers.JM_pdf_obj_from_str(pdf, value);
            mupdf.mupdf.pdf_dict_puts(obj, key, newObj);
            if (xref != -1) mupdf.mupdf.pdf_update_object(pdf, xref, obj);
        }

        /// <summary>
        /// Get xref of XML metadata.
        /// </summary>
        public int XrefXmlMetadata
        {
            get
            {
                try
                {
                    var pdf = NativePdfDocument;
                    var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                    var xml = mupdf.mupdf.pdf_dict_gets(root, "Metadata");
                    return xml.m_internal != null ? mupdf.mupdf.pdf_to_num(xml) : 0;
                }
                catch { return 0; }
            }
        }

        /// <summary>
        /// Get document XML metadata.
        /// </summary>
        public string GetXmlMetadata()
        {
            int xref = XrefXmlMetadata;
            if (xref == 0) return "";
            var data = XrefStream(xref);
            return data != null ? System.Text.Encoding.UTF8.GetString(data) : "";
        }

        /// <summary>
        /// Set document XML metadata.
        /// </summary>
        public void SetXmlMetadata(string metadata)
        {
            EnsurePdf();
            var pdf = NativePdfDocument;
            var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            var buf = Helpers.BufferFromBytes(System.Text.Encoding.UTF8.GetBytes(metadata));
            var xml = mupdf.mupdf.pdf_dict_gets(root, "Metadata");
            if (xml.m_internal != null)
            {
                mupdf.mupdf.pdf_update_stream(pdf, xml, buf, 0);
            }
            else
            {
                xml = mupdf.mupdf.pdf_add_stream(pdf, buf, new mupdf.PdfObj(), 0);
                mupdf.mupdf.pdf_dict_puts(xml, "Type", mupdf.mupdf.pdf_new_name("Metadata"));
                mupdf.mupdf.pdf_dict_puts(xml, "Subtype", mupdf.mupdf.pdf_new_name("XML"));
                mupdf.mupdf.pdf_dict_puts(root, "Metadata", xml);
            }
        }

        /// <summary>
        /// Delete XML metadata.
        /// </summary>
        public void DelXmlMetadata()
        {
            EnsurePdf();
            var pdf = NativePdfDocument;
            var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            mupdf.mupdf.pdf_dict_dels(root, "Metadata");
        }

        /// <summary>
        /// Update a PDF object.
        /// </summary>
        public void UpdateObject(int xref, string text)
        {
            EnsureNotClosed();
            if (IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            EnsureValidXrefPositiveIndirect(xref);
            var pdf = NativePdfDocument;
            var newObj = Helpers.JM_pdf_obj_from_str(pdf, text);
            mupdf.mupdf.pdf_update_object(pdf, xref, newObj);
        }

        /// <summary>
        /// Replace object definition source; when <paramref name="page"/> is not <c>null</c>, refresh that page's link list (Python <c>Document.update_object(xref, text, page=page)</c>).
        /// </summary>
        public void UpdateObject(int xref, string text, Page page)
        {
            UpdateObject(xref, text);
            if (page == null) return;
            var pdf = NativePdfDocument;
            Helpers.JM_refresh_links(pdf, page.NativePdfPage);
            page.SyncLinkWrapperCache();
        }

        /// <summary>
        /// Update the stream of a PDF object.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.update_stream</c> rejects non-dictionary xrefs with <c>MSG_IS_NO_DICT</c>; the <c>new</c> parameter there is unused.</remarks>
        public void UpdateStream(int xref, byte[] stream, bool compress = true)
        {
            EnsureNotClosed();
            if (IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            EnsureValidXrefForUpdateStream(xref);
            var pdf = NativePdfDocument;
            var obj = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
            if (mupdf.mupdf.pdf_is_dict(obj) == 0)
                throw new ValueErrorException(Constants.MSG_IS_NO_DICT);
            var buf = Helpers.BufferFromBytes(stream);
            mupdf.mupdf.pdf_update_stream(pdf, obj, buf, compress ? 1 : 0);
        }

        /// <summary>
        /// PyMuPDF <c>Document.xref_copy(doc, source, target, keep=...)</c> (staticmethod in Python).
        /// </summary>
        public static void XrefCopy(Document document, int source, int target, IReadOnlyCollection<string> keepKeys = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            document.XrefCopyImpl(source, target, keepKeys);
        }

        /// <summary>
        /// Copy one PDF dictionary (and optionally raw stream bytes) from <paramref name="source"/> xref to <paramref name="target"/> xref.
        /// </summary>
        public void XrefCopy(int source, int target, IReadOnlyCollection<string> keepKeys = null)
            => XrefCopyImpl(source, target, keepKeys);

        private void XrefCopyImpl(int source, int target, IReadOnlyCollection<string> keepKeys)
        {
            EnsurePdf();
            EnsureValidXrefPositiveIndirect(source);
            EnsureValidXrefPositiveIndirect(target);

            if (XrefIsStream(source))
            {
                var raw = XrefStreamRaw(source);
                if (raw != null)
                    UpdateStream(target, raw, compress: false);
            }

            var keep = keepKeys != null && keepKeys.Count > 0
                ? new HashSet<string>(keepKeys, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            foreach (var key in new List<string>(XrefGetKeys(target)))
            {
                if (keep.Contains(key)) continue;
                XrefSetKey(target, key, "null");
            }

            foreach (var key in XrefGetKeys(source))
            {
                var (_, value) = XrefGetKey(source, key);
                XrefSetKey(target, key, value);
            }
        }

        /// <summary>
        /// Make new xref.
        /// </summary>
        public int GetNewXref()
        {
            var pdf = NativePdfDocument;
            return mupdf.mupdf.pdf_create_object(pdf);
        }

        /// <summary>
        /// Return optional content object xref for an image or form xobject.
        /// </summary>
        public int GetOc(int xref)
        {
            var subtype = XrefGetKey(xref, "Subtype");
            if (subtype.type != "name" || (subtype.value != "/Image" && subtype.value != "/Form"))
                throw new ValueErrorException($"bad object type at xref {xref}");

            var oc = XrefGetKey(xref, "OC");
            if (oc.type != "xref")
                return 0;

            return int.Parse(oc.value.Replace("0 R", "").Trim());
        }

        /// <summary>
        /// Attach optional content object to image or form xobject.
        /// </summary>
        public void SetOc(int xref, int oc)
        {
            var subtype = XrefGetKey(xref, "Subtype");
            if (subtype.type != "name" || (subtype.value != "/Image" && subtype.value != "/Form"))
                throw new ValueErrorException($"bad object type at xref {xref}");

            if (oc > 0)
            {
                var ocType = XrefGetKey(oc, "Type");
                if (ocType.type != "name" || (ocType.value != "/OCG" && ocType.value != "/OCMD"))
                    throw new ValueErrorException($"bad object type at xref {oc}");
            }

            if (oc == 0 && XrefGetKeys(xref).Contains("OC"))
            {
                XrefSetKey(xref, "OC", "null");
                return;
            }

            XrefSetKey(xref, "OC", $"{oc} 0 R");
        }

        /// <summary>
        /// Return the definition of an OCMD (optional content membership dictionary).
        /// </summary>
        public Dictionary<string, object> GetOcmd(int xref)
        {
            if (xref < 0 || xref >= XrefLength)
                throw new ValueErrorException("bad xref");

            string text = XrefObject(xref, compressed: true);
            if (!text.Contains("/Type/OCMD"))
                throw new ValueErrorException("bad object type");

            int textlen = text.Length;

            int p0 = text.IndexOf("/OCGs[", StringComparison.Ordinal);
            int p1 = p0 >= 0 ? text.IndexOf("]", p0, StringComparison.Ordinal) : -1;
            List<int> ocgs = null;
            if (p0 >= 0 && p1 >= 0)
            {
                ocgs = new List<int>();
                string[] parts = text.Substring(p0 + 6, p1 - (p0 + 6))
                    .Replace("0 R", " ")
                    .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                    ocgs.Add(int.Parse(part));
            }

            p0 = text.IndexOf("/P/", StringComparison.Ordinal);
            string policy = null;
            if (p0 >= 0)
            {
                p1 = text.IndexOf("ff", p0, StringComparison.Ordinal);
                if (p1 < 0) p1 = text.IndexOf("on", p0, StringComparison.Ordinal);
                if (p1 < 0) throw new ValueErrorException("bad object at xref");
                policy = text.Substring(p0 + 3, p1 + 2 - (p0 + 3));
            }

            p0 = text.IndexOf("/VE[", StringComparison.Ordinal);
            object ve = null;
            if (p0 >= 0)
            {
                int lp = 0, rp = 0;
                p1 = p0;
                while (lp < 1 || lp != rp)
                {
                    p1++;
                    if (!(p1 < textlen))
                        throw new ValueErrorException("bad object at xref");
                    if (text[p1] == '[') lp++;
                    if (text[p1] == ']') rp++;
                }
                string veText = text.Substring(p0 + 3, p1 + 1 - (p0 + 3));
                veText = veText
                    .Replace("/And", "\"and\",")
                    .Replace("/Not", "\"not\",")
                    .Replace("/Or", "\"or\",");
                veText = veText.Replace(" 0 R]", "]").Replace(" 0 R", ",").Replace("][", "],[");
                ve = System.Text.Json.JsonSerializer.Deserialize<object>(veText);
            }

            return new Dictionary<string, object>
            {
                ["xref"] = xref,
                ["ocgs"] = ocgs,
                ["policy"] = policy,
                ["ve"] = ve
            };
        }

        /// <summary>
        /// Create or update an OCMD object in a PDF document.
        /// </summary>
        public int SetOcmd(int xref = 0, List<int> ocgs = null, string policy = null, object ve = null)
        {
            var allOcgs = new HashSet<int>(GetOcgs().Keys);

            string VeMaker(object veObject)
            {
                if (!(veObject is IList list) || list.Count < 2)
                    throw new ValueErrorException($"bad 've' format: {veObject}");

                string op = list[0]?.ToString() ?? "";
                string opLower = op.ToLowerInvariant();
                if (opLower != "and" && opLower != "or" && opLower != "not")
                    throw new ValueErrorException($"bad operand: {op}");
                if (opLower == "not" && list.Count != 2)
                    throw new ValueErrorException($"bad 've' format: {veObject}");

                string item = $"[/{char.ToUpperInvariant(opLower[0])}{opLower.Substring(1)}";
                for (int i = 1; i < list.Count; i++)
                {
                    object x = list[i];
                    if (x is int xi)
                    {
                        if (!allOcgs.Contains(xi))
                            throw new ValueErrorException($"bad OCG {xi}");
                        item += $" {xi} 0 R";
                    }
                    else if (x is long xl)
                    {
                        int xli = (int)xl;
                        if (!allOcgs.Contains(xli))
                            throw new ValueErrorException($"bad OCG {xli}");
                        item += $" {xli} 0 R";
                    }
                    else
                    {
                        item += $" {VeMaker(x)}";
                    }
                }
                item += "]";
                return item;
            }

            string text = "<</Type/OCMD";

            if (ocgs != null && ocgs.Count > 0)
            {
                var bad = new HashSet<int>(ocgs);
                bad.ExceptWith(allOcgs);
                if (bad.Count != 0)
                {
                    string inner = string.Join(", ", bad.OrderBy(x => x).Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    throw new ValueErrorException($"bad OCGs: {{{inner}}}");
                }
                text += "/OCGs[" + string.Join(" ", ocgs.ConvertAll(x => $"{x} 0 R")) + "]";
            }

            if (!string.IsNullOrEmpty(policy))
            {
                string p = policy.ToLowerInvariant();
                var pols = new Dictionary<string, string>
                {
                    ["anyon"] = "AnyOn",
                    ["allon"] = "AllOn",
                    ["anyoff"] = "AnyOff",
                    ["alloff"] = "AllOff",
                };
                if (!pols.ContainsKey(p))
                    throw new ValueErrorException($"bad policy: {policy}");
                text += $"/P/{pols[p]}";
            }

            if (ve != null)
                text += $"/VE{VeMaker(ve)}";

            text += ">>";

            if (xref == 0)
                xref = GetNewXref();
            else if (!XrefObject(xref, compressed: true).Contains("/Type/OCMD"))
                throw new ValueErrorException("bad xref or not an OCMD");

            UpdateObject(xref, text);
            return xref;
        }

        /// <summary>
        /// Convert the PDF's destination names into a dictionary.
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> ResolveNames()
        {
            if (_resolvedNames != null)
                return _resolvedNames;

            var pageXrefs = new Dictionary<int, int>();
            for (int i = 0; i < PageCount; i++)
                pageXrefs[PageXref(i)] = i;

            Dictionary<string, object> GetArray(mupdf.PdfObj val)
            {
                var templ = new Dictionary<string, object>
                {
                    ["page"] = -1,
                    ["dest"] = ""
                };

                if (mupdf.mupdf.pdf_is_indirect(val) != 0)
                    val = mupdf.mupdf.pdf_resolve_indirect(val);

                string array;
                if (mupdf.mupdf.pdf_is_array(val) != 0)
                    array = mupdf.mupdf.pdf_to_str_buf(val);
                else if (mupdf.mupdf.pdf_is_dict(val) != 0)
                    array = mupdf.mupdf.pdf_to_str_buf(mupdf.mupdf.pdf_dict_gets(val, "D"));
                else
                    return templ;

                array = array.Replace("null", "0");
                if (array.Length >= 2 && array[0] == '[' && array[array.Length - 1] == ']')
                    array = array.Substring(1, array.Length - 2);

                int idx = array.IndexOf("/", StringComparison.Ordinal);
                if (idx < 1)
                {
                    templ["dest"] = array;
                    return templ;
                }

                string subval = array.Substring(0, idx).Trim();
                array = array.Substring(idx);
                templ["dest"] = array;

                if (array.StartsWith("/XYZ", StringComparison.Ordinal))
                {
                    templ.Remove("dest");
                    var arrayList = new List<string>(array.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    if (arrayList.Count > 0)
                        arrayList.RemoveAt(0); // omit /XYZ
                    while (arrayList.Count < 3)
                        arrayList.Add("0");
                    float x = float.Parse(arrayList[0], System.Globalization.CultureInfo.InvariantCulture);
                    float y = float.Parse(arrayList[1], System.Globalization.CultureInfo.InvariantCulture);
                    float z = float.Parse(arrayList[2], System.Globalization.CultureInfo.InvariantCulture);
                    templ["to"] = (x, y);
                    templ["zoom"] = z;
                }

                if (subval.EndsWith("0 R", StringComparison.Ordinal))
                {
                    int px = int.Parse(subval.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
                    templ["page"] = pageXrefs.ContainsKey(px) ? pageXrefs[px] : -1;
                }
                else
                {
                    templ["page"] = int.Parse(subval, System.Globalization.CultureInfo.InvariantCulture);
                }
                return templ;
            }

            void FillDict(Dictionary<string, Dictionary<string, object>> destDict, mupdf.PdfObj pdfDict)
            {
                int nameCount = mupdf.mupdf.pdf_dict_len(pdfDict);
                for (int i = 0; i < nameCount; i++)
                {
                    var key = mupdf.mupdf.pdf_dict_get_key(pdfDict, i);
                    var val = mupdf.mupdf.pdf_dict_get_val(pdfDict, i);
                    string dictKey = null;
                    if (mupdf.mupdf.pdf_is_name(key) != 0)
                        dictKey = mupdf.mupdf.pdf_to_name(key);
                    if (!string.IsNullOrEmpty(dictKey))
                        destDict[dictKey] = GetArray(val);
                }
            }

            var pdf = NativePdfDocument;
            var catalog = mupdf.mupdf.pdf_dict_gets(mupdf.mupdf.pdf_trailer(pdf), "Root");

            var destDictResult = new Dictionary<string, Dictionary<string, object>>();
            var dests = mupdf.mupdf.pdf_new_name("Dests");

            var oldDests = mupdf.mupdf.pdf_dict_get(catalog, dests);
            if (mupdf.mupdf.pdf_is_dict(oldDests) != 0)
                FillDict(destDictResult, oldDests);

            var tree = mupdf.mupdf.pdf_load_name_tree(pdf, dests);
            if (mupdf.mupdf.pdf_is_dict(tree) != 0)
                FillDict(destDictResult, tree);

            _resolvedNames = destDictResult;
            return destDictResult;
        }

        /// <summary>
        /// Get xref of PDF catalog.
        /// </summary>
        public int PdfCatalog
        {
            get
            {
                try
                {
                    var pdf = NativePdfDocument;
                    var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                    return mupdf.mupdf.pdf_to_num(root);
                }
                catch { return 0; }
            }
        }

        /// <summary>
        /// Get PDF trailer as a string.
        /// </summary>
        public string PdfTrailer(bool compressed = false, bool ascii = false)
        {
            return XrefObject(-1, compressed, ascii);
        }

        // ─── Embedded Files ─────────────────────────────────────────────

        /// <summary>
        /// Get number of EmbeddedFiles.
        /// </summary>
        public int EmbfileCount
        {
            get
            {
                if (!IsPdf) return 0;
                var names = GetEmbeddedFilesNamesArray();
                return names.m_internal != null ? mupdf.mupdf.pdf_array_len(names) / 2 : 0;
            }
        }

        /// <summary>
        /// Get list of names of EmbeddedFiles.
        /// </summary>
        public List<string> EmbfileNames()
        {
            var result = new List<string>();
            if (!IsPdf) return result;
            var names = GetEmbeddedFilesNamesArray();
            if (names.m_internal == null) return result;
            int count = mupdf.mupdf.pdf_array_len(names) / 2;
            for (int i = 0; i < count; i++)
            {
                var key = mupdf.mupdf.pdf_array_get(names, i * 2);
                result.Add(key.m_internal != null ? mupdf.mupdf.pdf_to_text_string(key) ?? "" : "");
            }
            return result;
        }

        /// <summary>
        /// Get the content of an item in the EmbeddedFiles array by name.
        /// </summary>
        public byte[] EmbfileGet(string name)
        {
            int idx = EmbfileIndex(name);
            return EmbfileGetByIndex(idx);
        }

        /// <summary>
        /// Get the content of an item in the EmbeddedFiles array by index.
        /// </summary>
        public byte[] EmbfileGetByIndex(int idx)
        {
            var names = GetEmbeddedFilesNamesArray();
            if (names.m_internal == null) throw new ValueErrorException($"'{idx}' not in EmbeddedFiles array.");
            int count = mupdf.mupdf.pdf_array_len(names) / 2;
            if (idx < 0 || idx >= count) throw new ValueErrorException($"'{idx}' not in EmbeddedFiles array.");
            var fs = mupdf.mupdf.pdf_array_get(names, idx * 2 + 1);
            var buf = mupdf.mupdf.pdf_load_embedded_file_contents(fs);
            return buf.fz_buffer_extract();
        }

        /// <summary>
        /// Delete an entry from EmbeddedFiles.
        ///
        /// Physical deletion of data will happen on save to a new file with appropriate garbage option.
        /// </summary>
        public void EmbfileDel(string name)
        {
            int idx = EmbfileIndex(name);
            var pdf = NativePdfDocument;
            var names = Helpers.PdfDictGetl(mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"), mupdf.mupdf.pdf_new_name("Names"),
                mupdf.mupdf.pdf_new_name("EmbeddedFiles"), mupdf.mupdf.pdf_new_name("Names"));
            int keyIndex = idx * 2;
            mupdf.mupdf.pdf_array_delete(names, keyIndex + 1);
            mupdf.mupdf.pdf_array_delete(names, keyIndex);
        }

        /// <summary>
        /// Delete an entry from EmbeddedFiles by index.
        /// </summary>
        public void EmbfileDel(int idx)
        {
            var names = EmbfileNames();
            if (idx < 0 || idx >= names.Count)
                throw new ValueErrorException($"'{idx}' not in EmbeddedFiles array.");
            EmbfileDel(names[idx]);
        }

        /// <summary>
        /// Add an item to the EmbeddedFiles array.
        /// </summary>
        public int EmbfileAdd(string name, byte[] buffer, string filename = null, string ufilename = null, string desc = null)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("name must not be empty");
            if (buffer == null) throw new ArgumentException("buffer must not be null");
            var filenames = EmbfileNames();
            if (filenames.Contains(name))
                throw new ValueErrorException($"Name '{name}' already exists.");

            filename ??= name;
            ufilename ??= filename;
            desc ??= name;

            var pdf = NativePdfDocument;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var fs = mupdf.mupdf.pdf_add_embedded_file(pdf, filename, null, Helpers.BufferFromBytes(buffer), now, now, 1);
            if (fs.m_internal == null)
                throw new InvalidOperationException("failed to add embedded file");

            mupdf.mupdf.pdf_dict_put_text_string(fs, mupdf.mupdf.pdf_new_name("UF"), ufilename);
            mupdf.mupdf.pdf_dict_put_text_string(fs, mupdf.mupdf.pdf_new_name("Desc"), desc);

            var names = EnsureEmbeddedFilesNamesArray();
            mupdf.mupdf.pdf_array_push(names, mupdf.mupdf.pdf_new_text_string(name));
            mupdf.mupdf.pdf_array_push(names, fs);

            var ef = mupdf.mupdf.pdf_dict_get(fs, mupdf.mupdf.pdf_new_name("EF"));
            var stream = ef.m_internal != null ? mupdf.mupdf.pdf_dict_get(ef, mupdf.mupdf.pdf_new_name("F")) : new mupdf.PdfObj();
            if (stream.m_internal != null)
            {
                mupdf.mupdf.pdf_dict_put(stream, mupdf.mupdf.pdf_new_name("Type"), mupdf.mupdf.pdf_new_name("EmbeddedFile"));
                var date = Helpers.GetPdfNow();
                var parms = mupdf.mupdf.pdf_dict_get(stream, mupdf.mupdf.pdf_new_name("Params"));
                if (parms.m_internal == null)
                {
                    parms = mupdf.mupdf.pdf_new_dict(pdf, 2);
                    mupdf.mupdf.pdf_dict_put(stream, mupdf.mupdf.pdf_new_name("Params"), parms);
                }
                mupdf.mupdf.pdf_dict_put_text_string(parms, mupdf.mupdf.pdf_new_name("CreationDate"), date);
                mupdf.mupdf.pdf_dict_put_text_string(parms, mupdf.mupdf.pdf_new_name("ModDate"), date);
                return mupdf.mupdf.pdf_to_num(stream);
            }

            return mupdf.mupdf.pdf_to_num(fs);
        }

        /// <summary>
        /// Get information of an item in the EmbeddedFiles array by index.
        /// </summary>
        public Dictionary<string, object> EmbfileInfo(int idx)
        {
            var names = GetEmbeddedFilesNamesArray();
            if (names.m_internal == null) throw new ValueErrorException($"'{idx}' not in EmbeddedFiles array.");
            int count = mupdf.mupdf.pdf_array_len(names) / 2;
            if (idx < 0 || idx >= count) throw new ValueErrorException($"'{idx}' not in EmbeddedFiles array.");

            var keyObj = mupdf.mupdf.pdf_array_get(names, idx * 2);
            var fs = mupdf.mupdf.pdf_array_get(names, idx * 2 + 1);

            var info = new Dictionary<string, object>();
            info["name"] = keyObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(keyObj) ?? "" : "";

            var fObj = mupdf.mupdf.pdf_dict_get(fs, mupdf.mupdf.pdf_new_name("F"));
            var ufObj = mupdf.mupdf.pdf_dict_get(fs, mupdf.mupdf.pdf_new_name("UF"));
            var descObj = mupdf.mupdf.pdf_dict_get(fs, mupdf.mupdf.pdf_new_name("Desc"));
            info["filename"] = fObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(fObj) ?? "" : "";
            info["ufilename"] = ufObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(ufObj) ?? "" : "";
            info["desc"] = descObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(descObj) : null;

            var ef = mupdf.mupdf.pdf_dict_get(fs, mupdf.mupdf.pdf_new_name("EF"));
            var stream = ef.m_internal != null ? mupdf.mupdf.pdf_dict_get(ef, mupdf.mupdf.pdf_new_name("F")) : new mupdf.PdfObj();
            if (stream.m_internal != null)
            {
                var lengthObj = mupdf.mupdf.pdf_dict_get(stream, mupdf.mupdf.pdf_new_name("Length"));
                info["length"] = lengthObj.m_internal != null ? mupdf.mupdf.pdf_to_int(lengthObj) : -1;

                var paramsObj = mupdf.mupdf.pdf_dict_get(stream, mupdf.mupdf.pdf_new_name("Params"));
                if (paramsObj.m_internal != null)
                {
                    var sizeObj = mupdf.mupdf.pdf_dict_get(paramsObj, mupdf.mupdf.pdf_new_name("Size"));
                    if (sizeObj.m_internal != null) info["size"] = mupdf.mupdf.pdf_to_int(sizeObj);

                    var cObj = mupdf.mupdf.pdf_dict_get(paramsObj, mupdf.mupdf.pdf_new_name("CreationDate"));
                    if (cObj.m_internal != null) info["creationDate"] = mupdf.mupdf.pdf_to_text_string(cObj);

                    var mObj = mupdf.mupdf.pdf_dict_get(paramsObj, mupdf.mupdf.pdf_new_name("ModDate"));
                    if (mObj.m_internal != null) info["modDate"] = mupdf.mupdf.pdf_to_text_string(mObj);

                    var csObj = mupdf.mupdf.pdf_dict_get(paramsObj, mupdf.mupdf.pdf_new_name("CheckSum"));
                    if (csObj.m_internal != null)
                    {
                        string checksum = mupdf.mupdf.pdf_to_text_string(csObj) ?? "";
                        info["checksum"] = BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(checksum)).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            else
            {
                info["length"] = -1;
            }

            return info;
        }

        /// <summary>
        /// Get information of an item in the EmbeddedFiles array by name.
        /// </summary>
        public Dictionary<string, object> EmbfileInfo(string name) => EmbfileInfo(EmbfileIndex(name));

        /// <summary>
        /// Change an item of the EmbeddedFiles array by index.
        /// </summary>
        public int EmbfileUpd(int idx, byte[] buffer = null, string filename = null, string ufilename = null, string desc = null)
        {
            var names = GetEmbeddedFilesNamesArray();
            if (names.m_internal == null) throw new ValueErrorException($"'{idx}' not in EmbeddedFiles array.");
            int count = mupdf.mupdf.pdf_array_len(names) / 2;
            if (idx < 0 || idx >= count) throw new ValueErrorException($"'{idx}' not in EmbeddedFiles array.");

            var pdf = NativePdfDocument;
            var fs = mupdf.mupdf.pdf_array_get(names, idx * 2 + 1);
            var ef = mupdf.mupdf.pdf_dict_get(fs, mupdf.mupdf.pdf_new_name("EF"));
            var stream = ef.m_internal != null ? mupdf.mupdf.pdf_dict_get(ef, mupdf.mupdf.pdf_new_name("F")) : new mupdf.PdfObj();
            if (stream.m_internal == null)
                throw new InvalidOperationException("bad PDF: no embedded file stream");

            if (buffer != null)
            {
                mupdf.mupdf.pdf_update_stream(pdf, stream, Helpers.BufferFromBytes(buffer), 1);
                var parms = mupdf.mupdf.pdf_dict_get(stream, mupdf.mupdf.pdf_new_name("Params"));
                if (parms.m_internal == null)
                {
                    parms = mupdf.mupdf.pdf_new_dict(pdf, 1);
                    mupdf.mupdf.pdf_dict_put(stream, mupdf.mupdf.pdf_new_name("Params"), parms);
                }
                mupdf.mupdf.pdf_dict_put(parms, mupdf.mupdf.pdf_new_name("Size"), mupdf.mupdf.pdf_new_int(buffer.Length));
                mupdf.mupdf.pdf_dict_put(stream, mupdf.mupdf.pdf_new_name("DL"), mupdf.mupdf.pdf_new_int(buffer.Length));
            }

            if (!string.IsNullOrEmpty(filename))
            {
                mupdf.mupdf.pdf_dict_put_text_string(fs, mupdf.mupdf.pdf_new_name("F"), filename);
                mupdf.mupdf.pdf_dict_put_text_string(fs, mupdf.mupdf.pdf_new_name("UF"), filename);
                mupdf.mupdf.pdf_dict_put_text_string(stream, mupdf.mupdf.pdf_new_name("F"), filename);
                mupdf.mupdf.pdf_dict_put_text_string(stream, mupdf.mupdf.pdf_new_name("UF"), filename);
            }
            if (!string.IsNullOrEmpty(ufilename))
            {
                mupdf.mupdf.pdf_dict_put_text_string(fs, mupdf.mupdf.pdf_new_name("UF"), ufilename);
                mupdf.mupdf.pdf_dict_put_text_string(stream, mupdf.mupdf.pdf_new_name("UF"), ufilename);
            }
            if (!string.IsNullOrEmpty(desc))
            {
                mupdf.mupdf.pdf_dict_put_text_string(fs, mupdf.mupdf.pdf_new_name("Desc"), desc);
                mupdf.mupdf.pdf_dict_put_text_string(stream, mupdf.mupdf.pdf_new_name("Desc"), desc);
            }

            var dateNow = Helpers.GetPdfNow();
            var paramsObj = mupdf.mupdf.pdf_dict_get(stream, mupdf.mupdf.pdf_new_name("Params"));
            if (paramsObj.m_internal == null)
            {
                paramsObj = mupdf.mupdf.pdf_new_dict(pdf, 1);
                mupdf.mupdf.pdf_dict_put(stream, mupdf.mupdf.pdf_new_name("Params"), paramsObj);
            }
            mupdf.mupdf.pdf_dict_put_text_string(paramsObj, mupdf.mupdf.pdf_new_name("ModDate"), dateNow);

            return mupdf.mupdf.pdf_to_num(stream);
        }

        /// <summary>
        /// Change an item of the EmbeddedFiles array by name.
        /// </summary>
        public int EmbfileUpd(string name, byte[] buffer = null, string filename = null, string ufilename = null, string desc = null)
            => EmbfileUpd(EmbfileIndex(name), buffer, filename, ufilename, desc);

        private int EmbfileIndex(string name)
        {
            var names = EmbfileNames();
            int idx = names.IndexOf(name);
            if (idx < 0) throw new ValueErrorException($"'{name}' not in EmbeddedFiles array.");
            return idx;
        }

        // ─── Font / Image extraction ────────────────────────────────────

        /// <summary>
        /// List fonts used on a page.
        /// </summary>
        public List<(int xref, string ext, string type, string baseName, string name, string encoding)> GetPageFonts(int pno, bool full = false)
        {
            // Keep Python structure: get_page_fonts() -> _getPageInfo(..., 1)
            var pageInfo = _getPageInfo(pno, 1);
            var result = new List<(int xref, string ext, string type, string baseName, string name, string encoding)>(pageInfo.Count);
            foreach (var item in pageInfo)
                result.Add(((int xref, string ext, string type, string baseName, string name, string encoding))item);
            return result;
        }

        /// <summary>
        /// List images used on a page.
        /// </summary>
        public List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)> GetPageImages(int pno, bool full = false)
        {
            // Keep Python structure: get_page_images() -> _getPageInfo(..., 2)
            var pageInfo = _getPageInfo(pno, 2);
            var result = new List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)>(pageInfo.Count);
            foreach (var item in pageInfo)
                result.Add(((int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter))item);
            return result;
        }

        internal List<(int xref, string ext, string type, string baseName, string name, string encoding)> GetPageFontsCore(int pno, bool full = false)
        {
            var result = new List<(int xref, string ext, string type, string baseName, string name, string encoding)>();
            if (!IsPdf)
                return result;

            using var page = LoadPage(pno);
            var pageObj = page.NativePdfPage.obj();
            var resources = mupdf.mupdf.pdf_dict_get_inheritable(pageObj, mupdf.mupdf.pdf_new_name("Resources"));
            if (resources.m_internal == null)
                return result;

            var fonts = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("Font"));
            if (fonts.m_internal == null || mupdf.mupdf.pdf_is_dict(fonts) == 0)
                return result;

            int n = mupdf.mupdf.pdf_dict_len(fonts);
            for (int i = 0; i < n; i++)
            {
                var key = mupdf.mupdf.pdf_dict_get_key(fonts, i);
                var val = mupdf.mupdf.pdf_dict_get_val(fonts, i);
                var resolved = mupdf.mupdf.pdf_resolve_indirect(val);

                int xref = mupdf.mupdf.pdf_to_num(val);
                string ext = "";
                string type = "";
                string baseName = "";
                string name = mupdf.mupdf.pdf_to_name(key) ?? "";
                string encoding = "";

                var subtypeObj = mupdf.mupdf.pdf_dict_get(resolved, mupdf.mupdf.pdf_new_name("Subtype"));
                if (subtypeObj.m_internal != null && mupdf.mupdf.pdf_is_name(subtypeObj) != 0)
                    type = mupdf.mupdf.pdf_to_name(subtypeObj);

                var basefontObj = mupdf.mupdf.pdf_dict_get(resolved, mupdf.mupdf.pdf_new_name("BaseFont"));
                if (basefontObj.m_internal != null && mupdf.mupdf.pdf_is_name(basefontObj) != 0)
                    baseName = mupdf.mupdf.pdf_to_name(basefontObj);

                var encodingObj = mupdf.mupdf.pdf_dict_get(resolved, mupdf.mupdf.pdf_new_name("Encoding"));
                if (encodingObj.m_internal != null)
                {
                    if (mupdf.mupdf.pdf_is_name(encodingObj) != 0)
                        encoding = mupdf.mupdf.pdf_to_name(encodingObj);
                    else if (mupdf.mupdf.pdf_is_dict(encodingObj) != 0)
                    {
                        var baseEncObj = mupdf.mupdf.pdf_dict_get(encodingObj, mupdf.mupdf.pdf_new_name("BaseEncoding"));
                        if (baseEncObj.m_internal != null && mupdf.mupdf.pdf_is_name(baseEncObj) != 0)
                            encoding = mupdf.mupdf.pdf_to_name(baseEncObj);
                    }
                }

                if (xref > 0)
                {
                    try { ext = ExtractFont(xref).ext ?? ""; }
                    catch { ext = ""; }
                }

                result.Add((xref, ext, type ?? "", baseName ?? "", name ?? "", encoding ?? ""));
            }

            return result;
        }

        internal List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)> GetPageImagesCore(int pno, bool full = false)
        {
            var result = new List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)>();
            if (!IsPdf)
                return result;

            using var page = LoadPage(pno);
            var pageObj = page.NativePdfPage.obj();
            var resources = mupdf.mupdf.pdf_dict_get_inheritable(pageObj, mupdf.mupdf.pdf_new_name("Resources"));
            if (resources.m_internal == null)
                return result;

            var xobjects = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("XObject"));
            if (xobjects.m_internal == null || mupdf.mupdf.pdf_is_dict(xobjects) == 0)
                return result;

            int n = mupdf.mupdf.pdf_dict_len(xobjects);
            for (int i = 0; i < n; i++)
            {
                var key = mupdf.mupdf.pdf_dict_get_key(xobjects, i);
                var val = mupdf.mupdf.pdf_dict_get_val(xobjects, i);
                var resolved = mupdf.mupdf.pdf_resolve_indirect(val);

                var subtypeObj = mupdf.mupdf.pdf_dict_get(resolved, mupdf.mupdf.pdf_new_name("Subtype"));
                if (subtypeObj.m_internal == null || mupdf.mupdf.pdf_is_name(subtypeObj) == 0)
                    continue;
                if (!string.Equals(mupdf.mupdf.pdf_to_name(subtypeObj), "Image", StringComparison.Ordinal))
                    continue;

                int xref = mupdf.mupdf.pdf_to_num(val);
                if (xref <= 0 || !XrefIsStream(xref))
                    continue;
                string name = mupdf.mupdf.pdf_to_name(key) ?? "";

                int width = 0;
                int height = 0;
                int bpc = 0;
                string colorspace = "";
                string altCs = "";
                string filter = "";
                string smask = "0";

                var wObj = mupdf.mupdf.pdf_dict_get(resolved, mupdf.mupdf.pdf_new_name("Width"));
                if (wObj.m_internal != null) width = mupdf.mupdf.pdf_to_int(wObj);
                var hObj = mupdf.mupdf.pdf_dict_get(resolved, mupdf.mupdf.pdf_new_name("Height"));
                if (hObj.m_internal != null) height = mupdf.mupdf.pdf_to_int(hObj);
                var bpcObj = mupdf.mupdf.pdf_dict_get(resolved, mupdf.mupdf.pdf_new_name("BitsPerComponent"));
                if (bpcObj.m_internal != null) bpc = mupdf.mupdf.pdf_to_int(bpcObj);

                var csObj = mupdf.mupdf.pdf_dict_get(resolved, mupdf.mupdf.pdf_new_name("ColorSpace"));
                if (csObj.m_internal != null)
                {
                    if (mupdf.mupdf.pdf_is_name(csObj) != 0)
                    {
                        colorspace = mupdf.mupdf.pdf_to_name(csObj);
                    }
                    else if (mupdf.mupdf.pdf_is_array(csObj) != 0 && mupdf.mupdf.pdf_array_len(csObj) > 0)
                    {
                        var cs0 = mupdf.mupdf.pdf_array_get(csObj, 0);
                        if (cs0.m_internal != null && mupdf.mupdf.pdf_is_name(cs0) != 0)
                            colorspace = mupdf.mupdf.pdf_to_name(cs0);
                        if (mupdf.mupdf.pdf_array_len(csObj) > 1)
                        {
                            var cs1 = mupdf.mupdf.pdf_array_get(csObj, 1);
                            if (cs1.m_internal != null && mupdf.mupdf.pdf_is_name(cs1) != 0)
                                altCs = mupdf.mupdf.pdf_to_name(cs1);
                        }
                    }
                }

                var filterObj = mupdf.mupdf.pdf_dict_get(resolved, mupdf.mupdf.pdf_new_name("Filter"));
                if (filterObj.m_internal != null)
                {
                    if (mupdf.mupdf.pdf_is_name(filterObj) != 0)
                        filter = mupdf.mupdf.pdf_to_name(filterObj);
                    else if (mupdf.mupdf.pdf_is_array(filterObj) != 0 && mupdf.mupdf.pdf_array_len(filterObj) > 0)
                    {
                        var f0 = mupdf.mupdf.pdf_array_get(filterObj, 0);
                        if (f0.m_internal != null && mupdf.mupdf.pdf_is_name(f0) != 0)
                            filter = mupdf.mupdf.pdf_to_name(f0);
                    }
                }

                var smaskObj = mupdf.mupdf.pdf_dict_get(resolved, mupdf.mupdf.pdf_new_name("SMask"));
                if (smaskObj.m_internal != null)
                    smask = mupdf.mupdf.pdf_to_num(smaskObj).ToString(System.Globalization.CultureInfo.InvariantCulture);

                result.Add((xref, smask, width, height, bpc, colorspace ?? "", altCs ?? "", name, filter ?? ""));
            }

            return result;
        }

        /// <summary>
        /// Retrieve annotation xref/type/id triples for a page.
        /// </summary>
        public List<(int xref, AnnotationType type, string id)> PageAnnotXrefs(int n)
        {
            int pageCount = PageCount;
            while (n < 0)
                n += pageCount;
            if (n < 0 || n >= pageCount)
                throw new ValueErrorException(Constants.MSG_BAD_PAGENO);
            using var page = LoadPage(n);
            return page.AnnotXrefs();
        }

        /// <summary>
        /// Extract a font by xref. Returns (name, ext, type, content).
        /// </summary>
        public (string name, string ext, string type, byte[] content) ExtractFont(int xref)
        {
            var pdf = NativePdfDocument;
            var obj = mupdf.mupdf.pdf_load_object(pdf, xref);
            string name = "", ext = "", type = "";
            byte[] content = Array.Empty<byte>();

            var basefont = mupdf.mupdf.pdf_dict_gets(obj, "BaseFont");
            if (basefont.m_internal != null) name = mupdf.mupdf.pdf_to_name(basefont);

            var subtype = mupdf.mupdf.pdf_dict_gets(obj, "Subtype");
            if (subtype.m_internal != null) type = mupdf.mupdf.pdf_to_name(subtype);

            var desc = mupdf.mupdf.pdf_dict_gets(obj, "FontDescriptor");
            if (desc.m_internal != null)
            {
                var ff = mupdf.mupdf.pdf_dict_gets(desc, "FontFile");
                if (ff.m_internal == null) ff = mupdf.mupdf.pdf_dict_gets(desc, "FontFile2");
                if (ff.m_internal == null) ff = mupdf.mupdf.pdf_dict_gets(desc, "FontFile3");
                if (ff.m_internal != null)
                {
                    var buf = mupdf.mupdf.pdf_load_stream(ff);
                    content = buf.fz_buffer_extract();
                    ext = "n/a";
                    var sub2 = mupdf.mupdf.pdf_dict_gets(ff, "Subtype");
                    if (sub2.m_internal != null)
                    {
                        string sn = mupdf.mupdf.pdf_to_name(sub2);
                        if (sn.Contains("Type1C")) ext = "cff";
                        else if (sn.Contains("CIDFontType0C")) ext = "cff";
                        else if (sn.Contains("OpenType")) ext = "otf";
                        else ext = "ttf";
                    }
                    else ext = type == "Type1" ? "pfa" : "ttf";
                }
            }
            return (name, ext, type, content);
        }

        private object[] CheckFontInfo(int xref)
        {
            foreach (var fi in FontInfos)
            {
                if (fi == null || fi.Length < 2)
                    continue;
                if (!(fi[0] is int))
                    continue;
                if ((int)fi[0] == xref)
                    return fi;
            }
            return null;
        }

        private void UpdateFontInfo(object[] fontInfo)
        {
            if (fontInfo == null || fontInfo.Length < 2 || !(fontInfo[0] is int))
                return;
            int xref = (int)fontInfo[0];
            for (int i = 0; i < FontInfos.Count; i++)
            {
                var fi = FontInfos[i];
                if (fi != null && fi.Length > 0 && fi[0] is int && (int)fi[0] == xref)
                {
                    FontInfos[i] = fontInfo;
                    return;
                }
            }
            FontInfos.Add(fontInfo);
        }

        private static int GetCjkOrdering(string name)
        {
            if (name == "Fangti" || name == "Ming") return 0;
            if (name == "Heiti" || name == "Song") return 1;
            if (name == "Gothic" || name == "Mincho") return 2;
            if (name == "Dotum" || name == "Batang") return 3;
            return -1;
        }

        private static bool IsSimpleFontType(string subtype)
        {
            return subtype == "Type1" || subtype == "MMType1" || subtype == "TrueType";
        }

        private static List<(int glyph, double width)> BuildCharWidths(mupdf.FzFont font, int limit)
        {
            int mylimit = limit < 256 ? 256 : limit;
            var wlist = new List<(int glyph, double width)>(mylimit);
            for (int i = 0; i < mylimit; i++)
            {
                int glyph = font.fz_encode_character(i);
                double adv = font.fz_advance_glyph(glyph, 0);
                if (glyph > 0)
                    wlist.Add((glyph, adv));
                else
                    wlist.Add((glyph, 0.0));
            }
            return wlist;
        }

        /// <summary>
        /// Get list of glyph / width data for a font xref.
        /// Port of Python Document.get_char_widths().
        /// </summary>
        public List<(int glyph, double width)> GetCharWidths(int xref, int limit = 256, int idx = 0, Dictionary<string, object> fontdict = null)
        {
            EnsurePdf();
            if (!XrefIsFont(xref))
                throw new ArgumentException("xref is not a font");

            var fontinfo = CheckFontInfo(xref);
            if (fontinfo == null)
            {
                string name;
                string ext;
                string stype;
                if (fontdict == null)
                {
                    var ef = ExtractFont(xref);
                    name = ef.name;
                    ext = ef.ext ?? "";
                    stype = ef.type ?? "";
                    fontdict = new Dictionary<string, object>
                    {
                        ["name"] = name,
                        ["ext"] = ext,
                        ["type"] = stype
                    };
                }
                else
                {
                    name = fontdict.ContainsKey("name") ? (fontdict["name"]?.ToString() ?? "") : "";
                    ext = fontdict.ContainsKey("ext") ? (fontdict["ext"]?.ToString() ?? "") : "";
                    stype = fontdict.ContainsKey("type") ? (fontdict["type"]?.ToString() ?? "") : "";
                }

                if (ext == "")
                    throw new ArgumentException("xref is not a font");

                bool simple = IsSimpleFontType(stype);
                int ordering = GetCjkOrdering(name);
                fontdict["simple"] = simple;
                fontdict["ordering"] = ordering;
                fontdict["glyphs"] = null;
                fontinfo = new object[] { xref, fontdict };
                UpdateFontInfo(fontinfo);
            }
            else
            {
                fontdict = (Dictionary<string, object>)fontinfo[1];
            }

            List<(int glyph, double width)> glyphs = null;
            if (fontdict.ContainsKey("glyphs") && fontdict["glyphs"] is List<(int glyph, double width)>)
                glyphs = (List<(int glyph, double width)>)fontdict["glyphs"];

            int oldlimit = glyphs != null ? glyphs.Count : 0;
            int mylimit = limit < 256 ? 256 : limit;
            if (glyphs != null && mylimit <= oldlimit)
                return glyphs;

            int cjkOrdering = fontdict.ContainsKey("ordering") && fontdict["ordering"] is int ? (int)fontdict["ordering"] : -1;
            if (cjkOrdering >= 0)
            {
                // Python returns None for CJK fonts here; keep null-equivalent in cache and return null.
                fontdict["glyphs"] = null;
                fontinfo[1] = fontdict;
                UpdateFontInfo(fontinfo);
                return null;
            }

            var ef2 = ExtractFont(xref);
            mupdf.FzFont font;
            if (ef2.content != null && ef2.content.Length > 0)
            {
                var buf = Helpers.BufferFromBytes(ef2.content);
                font = new mupdf.FzFont(null, buf, idx, 0);
            }
            else
            {
                string name = fontdict.ContainsKey("name") ? (fontdict["name"]?.ToString() ?? null) : null;
                font = new mupdf.FzFont(name);
            }

            glyphs = BuildCharWidths(font, mylimit);
            fontdict["glyphs"] = glyphs;
            fontinfo[1] = fontdict;
            UpdateFontInfo(fontinfo);
            return glyphs;
        }

        /// <summary>
        /// Get image by xref. Returns a dictionary.
        /// </summary>
        public Dictionary<string, object> ExtractImage(int xref)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");

            var pdf = NativePdfDocument;

            if (xref < 1 || xref > mupdf.mupdf.pdf_xref_len(pdf) - 1)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);

            var obj = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
            var subtype = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("Subtype"));
            if (subtype.m_internal == null || !string.Equals(mupdf.mupdf.pdf_to_name(subtype), "Image", StringComparison.Ordinal))
                throw new ValueErrorException("not an image");

            var o = mupdf.mupdf.pdf_dict_geta(obj, mupdf.mupdf.pdf_new_name("SMask"), mupdf.mupdf.pdf_new_name("Mask"));
            int smask = o.m_internal != null ? mupdf.mupdf.pdf_to_num(o) : 0;

            // load the image
            var img = mupdf.mupdf.pdf_load_image(pdf, obj);
            var rc = new Dictionary<string, object>();
            MakeImageDict(img, rc);
            rc["smask"] = smask;
            rc["cs-name"] = mupdf.mupdf.fz_colorspace_name(img.colorspace());
            return rc;
        }

        private static void MakeImageDict(mupdf.FzImage img, Dictionary<string, object> imgDict)
        {
            int imgType = img.fz_compressed_image_type();
            string ext = JMImageExtension(imgType);
            byte[] bytes_;

            var llCbuf = mupdf.mupdf.ll_fz_compressed_image_buffer(img.m_internal);

            if (llCbuf == null
                || imgType == mupdf.mupdf.FZ_IMAGE_JBIG2
                || imgType == mupdf.mupdf.FZ_IMAGE_UNKNOWN
                || imgType < mupdf.mupdf.FZ_IMAGE_BMP)
            {
                var res = mupdf.mupdf.fz_new_buffer_from_image_as_png(
                    img,
                    new mupdf.FzColorParams(mupdf.mupdf.fz_default_color_params));
                ext = "png";
                bytes_ = res.fz_buffer_extract();
            }
            else if (ext == "jpeg" && img.n() == 4)
            {
                var res = mupdf.mupdf.fz_new_buffer_from_image_as_jpeg(
                    img,
                    new mupdf.FzColorParams(mupdf.mupdf.fz_default_color_params),
                    95,
                    1);
                bytes_ = res.fz_buffer_extract();
            }
            else
            {
                var res = new mupdf.FzBuffer(mupdf.mupdf.ll_fz_keep_buffer(llCbuf.buffer));
                bytes_ = res.fz_buffer_extract();
            }

            imgDict["width"] = img.w();
            imgDict["height"] = img.h();
            imgDict["ext"] = ext;
            imgDict["colorspace"] = img.n();
            imgDict["xres"] = img.xres();
            imgDict["yres"] = img.yres();
            imgDict["bpc"] = img.bpc();
            imgDict["size"] = bytes_.Length;
            imgDict["image"] = bytes_;
        }

        private static string JMImageExtension(int type)
        {
            switch (type)
            {
                case 1: return "png";
                case 2: return "jpeg";
                case 3: return "jxr";
                case 4: return "jpx";
                case 5: return "bmp";
                case 6: return "gif";
                case 7: return "tiff";
                case 8: return "pnm";
                default: return "unknown";
            }
        }

        // ─── Search ─────────────────────────────────────────────────────

        /// <summary>
        /// Search for text on a page.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.search_page_for</c> delegates to <c>Page.search_for</c>; optional <paramref name="clip"/>, <paramref name="flags"/>, and <paramref name="textpage"/> are forwarded.</remarks>
        public List<Quad> SearchPageFor(int pno, string needle, int maxHits = 16, Quad clip = null, int flags = 0, TextPage textpage = null)
        {
            using var page = LoadPage(pno);
            return page.SearchFor(needle, clip, maxHits, flags, textpage);
        }

        /// <summary>
        /// Search for text on a page; returns merged rectangles (PyMuPDF <c>quads=False</c> path).
        /// </summary>
        public List<Rect> SearchPageForRects(int pno, string needle, int maxHits = 16, Quad clip = null, int flags = 0, TextPage textpage = null)
        {
            using var page = LoadPage(pno);
            return page.SearchForRects(needle, clip, maxHits, flags, textpage);
        }

        // ─── Page Pixmap / Text convenience ─────────────────────────────

        /// <summary>
        /// Create pixmap of document page by page number.
        /// </summary>
        public Pixmap GetPagePixmap(int pno, Matrix matrix = null, Colorspace cs = null, bool alpha = false, IRect clip = null)
        {
            using var page = LoadPage(pno);
            return page.GetPixmap(matrix, cs, clip, alpha);
        }

        /// <summary>
        /// Extract text from page by page number.
        /// </summary>
        public string GetPageText(int pno, string option = "text", int flags = 0)
        {
            using var page = LoadPage(pno);
            return page.GetText(option, flags: flags);
        }

        /// <summary>
        /// Retrieve a list of XObjects used on a page.
        /// </summary>
        public List<Dictionary<string, object>> GetPageXobjects(int pno)
        {
            var result = new List<Dictionary<string, object>>();
            if (!IsPdf)
                return result;

            using var page = LoadPage(pno);
            var pageObj = page.NativePdfPage.obj();
            var resources = mupdf.mupdf.pdf_dict_get(pageObj, mupdf.mupdf.pdf_new_name("Resources"));
            if (resources.m_internal == null)
                return result;

            var xobjects = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("XObject"));
            if (xobjects.m_internal == null || mupdf.mupdf.pdf_is_dict(xobjects) == 0)
                return result;

            int n = mupdf.mupdf.pdf_dict_len(xobjects);
            for (int i = 0; i < n; i++)
            {
                var key = mupdf.mupdf.pdf_dict_get_key(xobjects, i);
                var val = mupdf.mupdf.pdf_dict_get_val(xobjects, i);
                var resolved = mupdf.mupdf.pdf_resolve_indirect(val);

                string name = mupdf.mupdf.pdf_to_name(key);
                int xref = mupdf.mupdf.pdf_to_num(val);
                string subtype = "";
                var subtypeObj = mupdf.mupdf.pdf_dict_get(resolved, mupdf.mupdf.pdf_new_name("Subtype"));
                if (subtypeObj.m_internal != null && mupdf.mupdf.pdf_is_name(subtypeObj) != 0)
                    subtype = mupdf.mupdf.pdf_to_name(subtypeObj);

                result.Add(new Dictionary<string, object>
                {
                    ["xref"] = xref,
                    ["name"] = name,
                    ["subtype"] = subtype
                });
            }

            return result;
        }

        /// <summary>
        /// Return a list of page numbers with the given label.
        ///
        /// Args:
        /// doc: PDF document object (resp. 'self').
        /// label: (str) label.
        /// only_one: (bool) stop searching after first hit.
        /// Returns:
        /// List of page numbers having this label.
        /// </summary>
        public List<int> GetPageNumbers(string label, bool onlyOne = false)
        {
            // Jorj McKie, 2021-01-06
            var numbers = new List<int>();
            // if not label: return numbers
            if (string.IsNullOrEmpty(label))
                return numbers;

            // labels = doc._get_page_labels()
            // if labels == []: return numbers
            for (int i = 0; i < PageCount; i++)
            {
                using var page = LoadPage(i);
                string pageLabel = page.NativePage.fz_page_label("", 128);
                if (pageLabel == label)
                {
                    numbers.Add(i);
                    // if only_one: break
                    if (onlyOne)
                        break;
                }
            }

            return numbers;
        }

        /// <summary>
        /// Return page label definitions in PDF document.
        ///
        /// Returns:
        /// A list of dictionaries with the following format:
        /// {'startpage': int, 'prefix': str, 'style': str, 'firstpagenum': int}.
        /// </summary>
        public List<Dictionary<string, object>> GetPageLabels()
        {
            // Jorj McKie, 2021-01-10
            // return [utils.rule_dict(item) for item in self._get_page_labels()]
            var result = new List<Dictionary<string, object>>();
            if (!IsPdf)
                return result;

            var pdf = NativePdfDocument;
            var trailer = mupdf.mupdf.pdf_trailer(pdf);
            var root = mupdf.mupdf.pdf_dict_get(trailer, mupdf.mupdf.pdf_new_name("Root"));
            if (root.m_internal == null)
                return result;

            var pageLabels = mupdf.mupdf.pdf_dict_get(root, mupdf.mupdf.pdf_new_name("PageLabels"));
            if (pageLabels.m_internal == null)
                return result;

            var nums = mupdf.mupdf.pdf_dict_get(pageLabels, mupdf.mupdf.pdf_new_name("Nums"));
            if (nums.m_internal == null)
                return result;

            int n = nums.pdf_array_len();
            for (int i = 0; i + 1 < n; i += 2)
            {
                var startObj = nums.pdf_array_get(i);
                var ruleObj = nums.pdf_array_get(i + 1);
                if (startObj.m_internal == null || ruleObj.m_internal == null)
                    continue;

                var item = new Dictionary<string, object>
                {
                    ["startpage"] = startObj.pdf_to_int(),
                    ["prefix"] = "",
                    ["style"] = "",
                    ["firstpagenum"] = 1
                };

                var p = ruleObj.pdf_dict_get(mupdf.mupdf.pdf_new_name("P"));
                if (p.m_internal != null)
                    item["prefix"] = p.pdf_to_text_string();

                var s = ruleObj.pdf_dict_get(mupdf.mupdf.pdf_new_name("S"));
                if (s.m_internal != null)
                    item["style"] = s.pdf_to_name();

                var st = ruleObj.pdf_dict_get(mupdf.mupdf.pdf_new_name("St"));
                if (st.m_internal != null)
                    item["firstpagenum"] = st.pdf_to_int();

                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Add / replace page label definitions in PDF document.
        ///
        /// Args:
        /// doc: PDF document (resp. 'self').
        /// labels: list of label dictionaries like:
        /// {'startpage': int, 'prefix': str, 'style': str, 'firstpagenum': int},
        /// as returned by get_page_labels().
        /// </summary>
        public void SetPageLabels(List<Dictionary<string, object>> labels)
        {
            // William Chapman, 2021-01-06
            if (!IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));

            var pdf = NativePdfDocument;

            for (int i = 0; i < PageCount; i++)
                mupdf.mupdf.pdf_delete_page_labels(pdf, i);

            // def create_nums(labels):
            //     """Return concatenated string of all labels rules.
            //     Returns PDF compatible string for page label definitions,
            //     ready to be enclosed in PDF array 'Nums[...]'.
            //     """
            //     labels.sort(key=lambda x: x["startpage"])
            //     s = "".join([create_label_str(label) for label in labels])
            labels.Sort((a, b) => Convert.ToInt32(a["startpage"]).CompareTo(Convert.ToInt32(b["startpage"])));
            foreach (var label in labels)
            {
                // def create_label_str(label):
                //     """Convert Python label dict to corresponding PDF rule string.
                //     Returns PDF label rule string wrapped in "<<", ">>".
                //     """
                //     s = f"{label['startpage']}<<"
                if (!label.ContainsKey("startpage"))
                    throw new ValueErrorException("label is missing required key 'startpage'");
                int startpage = Convert.ToInt32(label["startpage"]);
                if (startpage < 0)
                    throw new ValueErrorException("'startpage' must be >= 0");
                string prefix = label.ContainsKey("prefix") ? (label["prefix"]?.ToString() ?? "") : "";
                string style = label.ContainsKey("style") ? (label["style"]?.ToString() ?? "") : "";
                int first = label.ContainsKey("firstpagenum") ? Convert.ToInt32(label["firstpagenum"]) : 1;
                if (first < 1)
                    throw new ValueErrorException("'firstpagenum' must be >= 1");
                // if label.get("prefix", "") != "": s += f"/P({label['prefix']})"
                // if label.get("style", "") != "": s += f"/S/{label['style']}"
                // if label.get("firstpagenum", 1) > 1: s += f"/St {label['firstpagenum']}"
                // s += ">>"
                // return s
                // doc._set_page_labels(create_nums(labels))
                mupdf.mupdf.pdf_set_page_labels(pdf, startpage, ParsePageLabelStyle(style), prefix, first);
            }
        }

        private static mupdf.pdf_page_label_style ParsePageLabelStyle(string style)
        {
            switch (style ?? "")
            {
                case "D": return mupdf.pdf_page_label_style.PDF_PAGE_LABEL_DECIMAL;
                case "R": return mupdf.pdf_page_label_style.PDF_PAGE_LABEL_ROMAN_UC;
                case "r": return mupdf.pdf_page_label_style.PDF_PAGE_LABEL_ROMAN_LC;
                case "A": return mupdf.pdf_page_label_style.PDF_PAGE_LABEL_ALPHA_UC;
                case "a": return mupdf.pdf_page_label_style.PDF_PAGE_LABEL_ALPHA_LC;
                case "": return mupdf.pdf_page_label_style.PDF_PAGE_LABEL_NONE;
                default: throw new ValueErrorException("bad page label style");
            }
        }

        // ─── Layout ─────────────────────────────────────────────────────

        /// <summary>
        /// Re-layout a reflowable document.
        /// </summary>
        public void Layout(float width = 400, float height = 600, float fontsize = 11)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            // if not mupdf.fz_is_document_reflowable(doc): return
            if (!IsReflowable)
                return;
            float w = width;
            float h = height;
            // if w <= 0.0 or h <= 0.0: raise ValueError("bad page size")
            if (w <= 0.0f || h <= 0.0f)
                throw new ValueErrorException("bad page size");
            mupdf.mupdf.fz_layout_document(NativeDocument, w, h, fontsize);
            ResetPageRefsInternal();
            InitDoc();
        }

        /// <summary>
        /// Re-layout a reflowable document.
        /// </summary>
        public void Layout(Rect rect, float fontsize = 11)
        {
            Layout((float)rect.Width, (float)rect.Height, fontsize);
        }

        // ─── Journal ────────────────────────────────────────────────────

        /// <summary>
        /// Activate document journalling.
        /// </summary>
        public void JournalEnable()
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            var pdf = NativePdfDocument;
            mupdf.mupdf.pdf_enable_journal(pdf);
        }
        /// <summary>
        /// Check if journalling is enabled.
        /// </summary>
        public bool JournalIsEnabled
        {
            get
            {
                if (IsClosed || IsEncrypted)
                    throw new ValueErrorException("document closed or encrypted");
                var pdf = NativePdfDocument;
                var pdoc = pdf.m_internal;
                return pdoc != null && pdoc.journal != null;
            }
        }

        /// <summary>
        /// Show if undo and / or redo are possible.
        /// </summary>
        public (bool canUndo, bool canRedo) JournalCanDo()
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            var pdf = NativePdfDocument;
            return (mupdf.mupdf.pdf_can_undo(pdf) != 0, mupdf.mupdf.pdf_can_redo(pdf) != 0);
        }

        /// <summary>
        /// Move backwards in the journal.
        /// </summary>
        public void JournalUndo()
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            mupdf.mupdf.pdf_undo(NativePdfDocument);
        }
        /// <summary>
        /// Move forward in the journal.
        /// </summary>
        public void JournalRedo()
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            mupdf.mupdf.pdf_redo(NativePdfDocument);
        }

        /// <summary>
        /// Begin a journalling operation.
        /// </summary>
        public void JournalStartOp(string name = null)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            var pdf = NativePdfDocument;
            if (!JournalIsEnabled)
                throw new InvalidOperationException("Journalling not enabled");
            if (!string.IsNullOrEmpty(name))
                mupdf.mupdf.pdf_begin_operation(pdf, name);
            else
                mupdf.mupdf.pdf_begin_implicit_operation(pdf);
        }

        /// <summary>
        /// End a journalling operation.
        /// </summary>
        public void JournalStopOp()
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            mupdf.mupdf.pdf_end_operation(NativePdfDocument);
        }

        /// <summary>
        /// Save journal to a file.
        /// </summary>
        public void JournalSave(string filename)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            mupdf.mupdf.pdf_save_journal(NativePdfDocument, filename);
        }
        /// <summary>
        /// Load a journal from a file.
        /// </summary>
        public void JournalLoad(string filename)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            mupdf.mupdf.pdf_load_journal(NativePdfDocument, filename);
            if (!JournalIsEnabled)
                throw new FileDataException("Journal and document do not match");
        }

        /// <summary>
        /// Load a journal from serialized bytes.
        /// </summary>
        public void JournalLoad(byte[] data)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            if (data == null || data.Length == 0)
                throw new EmptyFileException("Cannot open empty stream.");
            var buffer = Helpers.BufferFromBytes(data);
            var stream = mupdf.mupdf.fz_open_buffer(buffer);
            mupdf.mupdf.pdf_deserialise_journal(NativePdfDocument, stream);
            if (!JournalIsEnabled)
                throw new FileDataException("Journal and document do not match");
        }

        /// <summary>
        /// Show operation name for given step.
        /// </summary>
        public string JournalOpName(int step)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            return mupdf.mupdf.pdf_undoredo_step(NativePdfDocument, step);
        }

        /// <summary>
        /// Show journalling state.
        /// </summary>
        public (int rc, int steps) JournalPosition()
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            var (rc, steps) = NativePdfDocument.pdf_undoredo_state();
            return (rc, steps);
        }

        // ─── Repair ─────────────────────────────────────────────────────

        /// <summary>
        /// If this is a PDF, perform document repair/check.
        /// </summary>
        public void Repair()
        {
            var pdf = Helpers.AsPdfDocument(NativeDocument, required: false);
            if (pdf != null && pdf.m_internal != null)
                mupdf.mupdf.pdf_check_document(pdf);
        }

        // ─── Insert PDF ─────────────────────────────────────────────────

        /// <summary>
        /// Insert pages from another PDF.
        ///
        /// Args:
        /// docsrc: PDF to copy from. Must be different object, but may be same file.
        /// from_page: (int) first source page to copy, 0-based, default 0.
        /// to_page: (int) last source page to copy, 0-based, default last page.
        /// start_at: (int) from_page will become this page number in target.
        /// rotate: (int) rotate copied pages, default -1 is no change.
        /// links: (int/bool) whether to also copy links.
        /// annots: (int/bool) whether to also copy annotations.
        ///
        /// Copy sequence reversed if from_page > to_page.
        /// </summary>
        public void InsertPdf(Document src, int fromPage = 0, int toPage = -1, int startAt = -1,
            int rotate = -1, bool links = true, bool annots = true)
        {
            // if self.is_closed or self.is_encrypted: raise ValueError("document closed or encrypted")
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            if (src == null)
                throw new ArgumentNullException(nameof(src));
            // if self._graft_id == docsrc._graft_id: raise ValueError("source and target cannot be same object")
            if (ReferenceEquals(this, src))
                throw new ValueErrorException("source and target cannot be same object");

            var pdfDst = NativePdfDocument;
            var pdfSrc = src.NativePdfDocument;
            int outCount = PageCount;
            int srcCount = src.PageCount;

            // normalize page numbers
            int fp = Math.Max(fromPage, 0);
            fp = Math.Min(fp, srcCount - 1);

            int tp = toPage;
            if (tp < 0)
                tp = srcCount - 1;
            tp = Math.Min(tp, srcCount - 1);

            int sa = startAt;
            if (sa < 0)
                sa = outCount;
            sa = Math.Min(sa, outCount);

            var graftmap = mupdf.mupdf.pdf_new_graft_map(pdfDst);

            int step = fp <= tp ? 1 : -1;
            for (int i = fp; step > 0 ? i <= tp : i >= tp; i += step)
            {
                int dstIndex = sa + (step > 0 ? (i - fp) : (fp - i));
                mupdf.mupdf.pdf_graft_mapped_page(graftmap, dstIndex, pdfSrc, i);
            }
            ResetPageRefsInternal();
        }

        /// <summary>
        /// Insert an arbitrary supported document into an existing PDF.
        ///
        /// The infile may be given as a filename, a Document or a Pixmap. Other
        /// parameters - where applicable - equal those of insert_pdf().
        /// </summary>
        public void InsertFile(object infile, int fromPage = -1, int toPage = -1, int startAt = -1,
            int rotate = -1, bool links = true, bool annots = true, int showProgress = 0, int final = 1)
        {
            Document src = null;
            bool disposeSrc = false;

            if (infile is Pixmap pixmap)
            {
                // if infile.colorspace.n > 3: infile = Pixmap(csRGB, infile)
                src = new Document(pixmap.ToBytes("png"), "png");
                disposeSrc = true;
            }
            else if (infile is Document doc)
            {
                src = doc;
            }
            else if (infile is string path)
            {
                src = new Document(path);
                disposeSrc = true;
            }
            else
            {
                throw new ValueErrorException("bad infile parameter");
            }

            if (src == null)
                throw new ValueErrorException("bad infile parameter");

            Document pdfSrc = src;
            bool disposePdfSrc = false;
            // if not src.is_pdf: pdfbytes = src.convert_to_pdf(); src = Document("pdf", pdfbytes)
            if (!src.IsPdf)
            {
                var pdfbytes = src.ConvertToPdf();
                pdfSrc = new Document(pdfbytes, "pdf");
                disposePdfSrc = true;
            }

            InsertPdf(pdfSrc, fromPage, toPage, startAt, rotate, links, annots);

            if (disposePdfSrc)
                pdfSrc.Dispose();
            if (disposeSrc)
                src.Dispose();
        }

        // ─── OC Layers ──────────────────────────────────────────────────

        /// <summary>
        /// Show existing optional content groups.
        /// </summary>
        public Dictionary<int, Dictionary<string, object>> GetOcgs()
        {
            // Return the definitions of existing optional content groups.
            var result = new Dictionary<int, Dictionary<string, object>>();
            var pdf = NativePdfDocument;
            var ci = mupdf.mupdf.pdf_new_name("CreatorInfo");
            var ocProps = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root")), mupdf.mupdf.pdf_new_name("OCProperties"));
            if (ocProps.m_internal == null) return result;

            var ocgs = mupdf.mupdf.pdf_dict_get(ocProps, mupdf.mupdf.pdf_new_name("OCGs"));
            if (ocgs.m_internal == null || mupdf.mupdf.pdf_is_array(ocgs) == 0) return result;

            int n = mupdf.mupdf.pdf_array_len(ocgs);
            for (int i = 0; i < n; i++)
            {
                var ocg = mupdf.mupdf.pdf_array_get(ocgs, i);
                int xref = mupdf.mupdf.pdf_to_num(ocg);
                var ocgObj = mupdf.mupdf.pdf_resolve_indirect(ocg);
                var nameObj = mupdf.mupdf.pdf_dict_get(ocgObj, mupdf.mupdf.pdf_new_name("Name"));
                string name = nameObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(nameObj) : "";

                string usage = null;
                var usageObj = mupdf.mupdf.pdf_dict_get(
                    mupdf.mupdf.pdf_dict_get(
                        mupdf.mupdf.pdf_dict_get(ocgObj, mupdf.mupdf.pdf_new_name("Usage")),
                        ci),
                    mupdf.mupdf.pdf_new_name("Subtype"));
                if (usageObj.m_internal != null)
                    usage = mupdf.mupdf.pdf_to_name(usageObj);

                var intents = new List<string>();
                var intent = mupdf.mupdf.pdf_dict_get(ocgObj, mupdf.mupdf.pdf_new_name("Intent"));
                if (intent.m_internal != null)
                {
                    if (mupdf.mupdf.pdf_is_name(intent) != 0)
                    {
                        intents.Add(mupdf.mupdf.pdf_to_name(intent));
                    }
                    else if (mupdf.mupdf.pdf_is_array(intent) != 0)
                    {
                        int m = mupdf.mupdf.pdf_array_len(intent);
                        for (int j = 0; j < m; j++)
                        {
                            var o = mupdf.mupdf.pdf_array_get(intent, j);
                            if (mupdf.mupdf.pdf_is_name(o) != 0)
                                intents.Add(mupdf.mupdf.pdf_to_name(o));
                        }
                    }
                }

                var resourceStack = new mupdf.PdfResourceStack();
                int hidden = mupdf.mupdf.pdf_is_ocg_hidden(pdf, resourceStack, usage, ocgObj);

                var info = new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["intent"] = intents,
                    ["on"] = hidden == 0,
                    ["usage"] = usage,
                };
                result[xref] = info;
            }
            return result;
        }

        /// <summary>
        /// Add a new OC layer.
        /// </summary>
        public void AddLayer(string name, string creator = null, bool on = true)
        {
            // Add a new OC layer.
            var pdf = NativePdfDocument;
            var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            var ocProps = mupdf.mupdf.pdf_dict_get(root, mupdf.mupdf.pdf_new_name("OCProperties"));
            if (ocProps.m_internal == null)
            {
                ocProps = mupdf.mupdf.pdf_new_dict(pdf, 2);
                mupdf.mupdf.pdf_dict_puts(root, "OCProperties", ocProps);
            }

            var ocgs = mupdf.mupdf.pdf_dict_get(ocProps, mupdf.mupdf.pdf_new_name("OCGs"));
            if (ocgs.m_internal == null)
            {
                ocgs = mupdf.mupdf.pdf_new_array(pdf, 1);
                mupdf.mupdf.pdf_dict_puts(ocProps, "OCGs", ocgs);
            }

            var ocg = mupdf.mupdf.pdf_new_dict(pdf, 3);
            mupdf.mupdf.pdf_dict_put(ocg, mupdf.mupdf.pdf_new_name("Type"), mupdf.mupdf.pdf_new_name("OCG"));
            mupdf.mupdf.pdf_dict_put(ocg, mupdf.mupdf.pdf_new_name("Name"), mupdf.mupdf.pdf_new_text_string(name));
            var indRef = mupdf.mupdf.pdf_add_object(pdf, ocg);
            mupdf.mupdf.pdf_array_push(ocgs, indRef);

            if (on)
            {
                var d = mupdf.mupdf.pdf_dict_get(ocProps, mupdf.mupdf.pdf_new_name("D"));
                if (d.m_internal == null)
                {
                    d = mupdf.mupdf.pdf_new_dict(pdf, 1);
                    mupdf.mupdf.pdf_dict_puts(ocProps, "D", d);
                }
                var onArr = mupdf.mupdf.pdf_dict_get(d, mupdf.mupdf.pdf_new_name("ON"));
                if (onArr.m_internal == null)
                {
                    onArr = mupdf.mupdf.pdf_new_array(pdf, 1);
                    mupdf.mupdf.pdf_dict_puts(d, "ON", onArr);
                }
                mupdf.mupdf.pdf_array_push(onArr, indRef);
            }
        }

        /// <summary>
        /// Add a new optional content group (OCG) and insert it into a configuration.
        ///
        /// Args:
        /// name: (str) OCG name.
        /// config: (int) put OCG in this configuration number.
        /// on: (bool) set OCG state to ON (or OFF).
        /// intent: (str) a `/Intent` name, default "View".
        /// usage: (str) a `/Usage/CreatorInfo/Subtype` name, default "Artwork".
        /// Returns:
        /// Xref of the inserted OCG object.
        /// </summary>
        public int AddOcg(string name, int config = -1, bool on = true, string intent = null, string usage = null)
        {
            // Add new optional content group.
            var pdf = NativePdfDocument;

            var ocg = mupdf.mupdf.pdf_add_new_dict(pdf, 3);
            mupdf.mupdf.pdf_dict_put(ocg, mupdf.mupdf.pdf_new_name("Type"), mupdf.mupdf.pdf_new_name("OCG"));
            mupdf.mupdf.pdf_dict_put_text_string(ocg, mupdf.mupdf.pdf_new_name("Name"), name);

            var intents = mupdf.mupdf.pdf_dict_put_array(ocg, mupdf.mupdf.pdf_new_name("Intent"), 2);
            if (string.IsNullOrEmpty(intent))
                mupdf.mupdf.pdf_array_push(intents, mupdf.mupdf.pdf_new_name("View"));
            else
                mupdf.mupdf.pdf_array_push(intents, mupdf.mupdf.pdf_new_name(intent));

            var useFor = mupdf.mupdf.pdf_dict_put_dict(ocg, mupdf.mupdf.pdf_new_name("Usage"), 3);
            var creInfo = mupdf.mupdf.pdf_dict_put_dict(useFor, mupdf.mupdf.pdf_new_name("CreatorInfo"), 2);
            mupdf.mupdf.pdf_dict_put_text_string(creInfo, mupdf.mupdf.pdf_new_name("Creator"), "PyMuPDF");
            mupdf.mupdf.pdf_dict_put_name(creInfo, mupdf.mupdf.pdf_new_name("Subtype"), string.IsNullOrEmpty(usage) ? "Artwork" : usage);

            var indocg = mupdf.mupdf.pdf_add_object(pdf, ocg);

            var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            var ocp = mupdf.mupdf.pdf_dict_get(root, mupdf.mupdf.pdf_new_name("OCProperties"));
            if (ocp.m_internal == null)
            {
                ocp = mupdf.mupdf.pdf_new_dict(pdf, 2);
                mupdf.mupdf.pdf_dict_put(root, mupdf.mupdf.pdf_new_name("OCProperties"), ocp);
            }

            var ocgs = mupdf.mupdf.pdf_dict_get(ocp, mupdf.mupdf.pdf_new_name("OCGs"));
            if (ocgs.m_internal == null)
                ocgs = mupdf.mupdf.pdf_dict_put_array(ocp, mupdf.mupdf.pdf_new_name("OCGs"), 1);
            mupdf.mupdf.pdf_array_push(ocgs, indocg);

            mupdf.PdfObj cfg;
            // if config > -1: use Configs[config], else use default config D
            if (config > -1)
            {
                var cfgs = mupdf.mupdf.pdf_dict_get(ocp, mupdf.mupdf.pdf_new_name("Configs"));
                if (mupdf.mupdf.pdf_is_array(cfgs) == 0)
                    throw new ValueErrorException(Constants.MSG_BAD_OC_CONFIG);
                cfg = mupdf.mupdf.pdf_array_get(cfgs, config);
                if (cfg.m_internal == null)
                    throw new ValueErrorException(Constants.MSG_BAD_OC_CONFIG);
            }
            else
            {
                cfg = mupdf.mupdf.pdf_dict_get(ocp, mupdf.mupdf.pdf_new_name("D"));
            }

            var order = mupdf.mupdf.pdf_dict_get(cfg, mupdf.mupdf.pdf_new_name("Order"));
            if (order.m_internal == null)
                order = mupdf.mupdf.pdf_dict_put_array(cfg, mupdf.mupdf.pdf_new_name("Order"), 1);
            mupdf.mupdf.pdf_array_push(order, indocg);

            var stateArr = mupdf.mupdf.pdf_dict_get(cfg, mupdf.mupdf.pdf_new_name(on ? "ON" : "OFF"));
            if (stateArr.m_internal == null)
                stateArr = mupdf.mupdf.pdf_dict_put_array(cfg, mupdf.mupdf.pdf_new_name(on ? "ON" : "OFF"), 1);
            mupdf.mupdf.pdf_array_push(stateArr, indocg);

            mupdf.mupdf.ll_pdf_read_ocg(pdf.m_internal);
            return mupdf.mupdf.pdf_to_num(indocg);
        }

        /// <summary>
        /// Content of ON, OFF, RBGroups of an OC layer.
        /// </summary>
        public Dictionary<string, object> GetLayer(int config = -1)
        {
            var pdf = NativePdfDocument;
            var ocp = mupdf.mupdf.pdf_dict_get(
                mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root")),
                mupdf.mupdf.pdf_new_name("OCProperties"));
            if (ocp.m_internal == null)
                return null;

            mupdf.PdfObj obj;
            if (config == -1)
            {
                obj = mupdf.mupdf.pdf_dict_get(ocp, mupdf.mupdf.pdf_new_name("D"));
            }
            else
            {
                obj = mupdf.mupdf.pdf_array_get(
                    mupdf.mupdf.pdf_dict_get(ocp, mupdf.mupdf.pdf_new_name("Configs")),
                    config
                );
            }
            if (obj.m_internal == null)
                throw new ValueErrorException(Constants.MSG_BAD_OC_CONFIG);

            List<int> ReadXrefArray(string key)
            {
                var arr = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name(key));
                var rc = new List<int>();
                if (arr.m_internal == null || mupdf.mupdf.pdf_is_array(arr) == 0)
                    return rc;
                int n = mupdf.mupdf.pdf_array_len(arr);
                for (int i = 0; i < n; i++)
                {
                    var item = mupdf.mupdf.pdf_array_get(arr, i);
                    rc.Add(mupdf.mupdf.pdf_to_num(item));
                }
                return rc;
            }

            var rb = new List<List<int>>();
            var rbObj = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("RBGroups"));
            if (rbObj.m_internal != null && mupdf.mupdf.pdf_is_array(rbObj) != 0)
            {
                int n = mupdf.mupdf.pdf_array_len(rbObj);
                for (int i = 0; i < n; i++)
                {
                    var groupObj = mupdf.mupdf.pdf_array_get(rbObj, i);
                    var group = new List<int>();
                    if (groupObj.m_internal != null && mupdf.mupdf.pdf_is_array(groupObj) != 0)
                    {
                        int m = mupdf.mupdf.pdf_array_len(groupObj);
                        for (int j = 0; j < m; j++)
                            group.Add(mupdf.mupdf.pdf_to_num(mupdf.mupdf.pdf_array_get(groupObj, j)));
                    }
                    rb.Add(group);
                }
            }

            string basestate = "";
            var baseObj = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("BaseState"));
            if (baseObj.m_internal != null)
                basestate = mupdf.mupdf.pdf_to_name(baseObj);

            return new Dictionary<string, object>
            {
                ["basestate"] = basestate,
                ["on"] = ReadXrefArray("ON"),
                ["off"] = ReadXrefArray("OFF"),
                ["rbgroups"] = rb,
                ["locked"] = ReadXrefArray("Locked"),
            };
        }

        /// <summary>
        /// Show optional OC layers.
        /// </summary>
        public List<Dictionary<string, object>> GetLayers()
        {
            var pdf = NativePdfDocument;
            int n = mupdf.mupdf.pdf_count_layer_configs(pdf);
            if (n == 1)
            {
                var obj = mupdf.mupdf.pdf_dict_get(
                    mupdf.mupdf.pdf_dict_get(
                        mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root")),
                        mupdf.mupdf.pdf_new_name("OCProperties")),
                    mupdf.mupdf.pdf_new_name("Configs"));
                if (mupdf.mupdf.pdf_is_array(obj) == 0)
                    n = 0;
            }

            var rc = new List<Dictionary<string, object>>();
            var info = new mupdf.PdfLayerConfig();
            for (int i = 0; i < n; i++)
            {
                mupdf.mupdf.pdf_layer_config_info(pdf, i, info);
                rc.Add(new Dictionary<string, object>
                {
                    ["number"] = i,
                    ["name"] = info.name,
                    ["creator"] = info.creator,
                });
            }
            return rc;
        }

        /// <summary>
        /// Show OC visibility status modifiable by user.
        /// </summary>
        public List<Dictionary<string, object>> LayerUiConfigs()
        {
            var pdf = NativePdfDocument;
            var info = new mupdf.PdfLayerConfigUi();
            int n = mupdf.mupdf.pdf_count_layer_config_ui(pdf);
            var rc = new List<Dictionary<string, object>>();
            for (int i = 0; i < n; i++)
            {
                mupdf.mupdf.pdf_layer_config_ui_info(pdf, i, info);
                string type = "label";
                if ((int)info.type == 1) type = "checkbox";
                else if ((int)info.type == 2) type = "radiobox";
                rc.Add(new Dictionary<string, object>
                {
                    ["number"] = i,
                    ["text"] = info.text,
                    ["depth"] = info.depth,
                    ["type"] = type,
                    ["on"] = info.selected != 0,
                    ["locked"] = info.locked != 0,
                });
            }
            return rc;
        }

        /// <summary>
        /// Set / unset OC intent configuration.
        /// </summary>
        public void SetLayerUiConfig(object number, int action = 0)
        {
            // The user might have given the name instead of sequence number,
            // so select by that name and continue with corresp. number
            int uiNumber;
            if (number is string name)
            {
                uiNumber = -1;
                foreach (var ui in LayerUiConfigs())
                {
                    if ((ui.TryGetValue("text", out var t) ? t?.ToString() : "") == name)
                    {
                        uiNumber = Convert.ToInt32(ui["number"]);
                        break;
                    }
                }
                if (uiNumber < 0)
                    throw new ValueErrorException($"bad OCG '{name}'.");
            }
            else
            {
                uiNumber = Convert.ToInt32(number);
            }

            var pdf = NativePdfDocument;
            if (action == 1)
                mupdf.mupdf.pdf_toggle_layer_config_ui(pdf, uiNumber);
            else if (action == 2)
                mupdf.mupdf.pdf_deselect_layer_config_ui(pdf, uiNumber);
            else
                mupdf.mupdf.pdf_select_layer_config_ui(pdf, uiNumber);
        }

        /// <summary>
        /// Set the PDF keys /ON, /OFF, /RBGroups of an OC layer.
        /// </summary>
        public void SetLayer(int config, string basestate = null, object on = null, object off = null, object rbgroups = null, object locked = null)
        {
            if (IsClosed)
                throw new ValueErrorException("document closed");

            var ocgs = new HashSet<int>(GetOcgs().Keys);
            if (ocgs.Count == 0)
                throw new ValueErrorException("document has no optional content");

            List<int> ParseOcgsList(object value, string name)
            {
                if (value == null) return null;
                if (!(value is IList list))
                    throw new ValueErrorException($"bad type: '{name}'");
                var rc = new List<int>();
                foreach (var item in list)
                    rc.Add(Convert.ToInt32(item));
                return rc;
            }

            void ValidateOcgsList(List<int> list, string name)
            {
                if (list == null) return;
                var bad = new HashSet<int>(list);
                bad.ExceptWith(ocgs);
                if (bad.Count != 0)
                {
                    string badSetStr = string.Join(", ", bad.OrderBy(x => x).Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    throw new ValueErrorException($"bad OCGs in '{name}': {{{badSetStr}}}");
                }
            }

            var onList = ParseOcgsList(on, "on");
            var offList = ParseOcgsList(off, "off");
            var lockedList = ParseOcgsList(locked, "locked");
            ValidateOcgsList(onList, "on");
            ValidateOcgsList(offList, "off");
            ValidateOcgsList(lockedList, "locked");

            List<List<int>> rbGroupsList = null;
            if (rbgroups != null)
            {
                if (!(rbgroups is IList outer))
                    throw new ValueErrorException("bad type: 'rbgroups'");
                rbGroupsList = new List<List<int>>();
                foreach (var x in outer)
                {
                    if (!(x is IList innerList))
                        throw new ValueErrorException($"bad RBGroup '{x}'");
                    var grp = new List<int>();
                    foreach (var item in innerList)
                        grp.Add(Convert.ToInt32(item));
                    rbGroupsList.Add(grp);
                    var bad = new HashSet<int>(grp);
                    bad.ExceptWith(ocgs);
                    if (bad.Count != 0)
                    {
                        string rbBadSetStr = string.Join(", ", bad.OrderBy(y => y).Select(y => y.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                        throw new ValueErrorException($"bad OCGs in RBGroup: {{{rbBadSetStr}}}");
                    }
                }
            }

            string baseStateNorm = null;
            if (!string.IsNullOrEmpty(basestate))
            {
                baseStateNorm = basestate.ToUpperInvariant();
                if (baseStateNorm == "UNCHANGED")
                    baseStateNorm = "Unchanged";
                if (baseStateNorm != "ON" && baseStateNorm != "OFF" && baseStateNorm != "Unchanged")
                    throw new ValueErrorException("bad 'basestate'");
            }

            var pdf = NativePdfDocument;
            var ocp = mupdf.mupdf.pdf_dict_get(
                mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root")),
                mupdf.mupdf.pdf_new_name("OCProperties"));
            if (ocp.m_internal == null)
                return;

            mupdf.PdfObj obj;
            // if config == -1: obj = D else obj = Configs[config]
            if (config == -1)
            {
                obj = mupdf.mupdf.pdf_dict_get(ocp, mupdf.mupdf.pdf_new_name("D"));
            }
            else
            {
                obj = mupdf.mupdf.pdf_array_get(
                    mupdf.mupdf.pdf_dict_get(ocp, mupdf.mupdf.pdf_new_name("Configs")),
                    config
                );
            }
            if (obj.m_internal == null)
                throw new ValueErrorException(Constants.MSG_BAD_OC_CONFIG);

            mupdf.PdfObj BuildArray(List<int> refs)
            {
                var arr = mupdf.mupdf.pdf_new_array(pdf, refs?.Count ?? 0);
                if (refs != null)
                {
                    foreach (var r in refs)
                    {
                        var o = mupdf.mupdf.pdf_new_indirect(pdf, r, 0);
                        mupdf.mupdf.pdf_array_push(arr, o);
                    }
                }
                return arr;
            }

            if (baseStateNorm != null)
                mupdf.mupdf.pdf_dict_put(obj, mupdf.mupdf.pdf_new_name("BaseState"), mupdf.mupdf.pdf_new_name(baseStateNorm));

            if (onList != null) mupdf.mupdf.pdf_dict_put(obj, mupdf.mupdf.pdf_new_name("ON"), BuildArray(onList));
            if (offList != null) mupdf.mupdf.pdf_dict_put(obj, mupdf.mupdf.pdf_new_name("OFF"), BuildArray(offList));
            if (lockedList != null) mupdf.mupdf.pdf_dict_put(obj, mupdf.mupdf.pdf_new_name("Locked"), BuildArray(lockedList));

            if (rbGroupsList != null)
            {
                var rbTop = mupdf.mupdf.pdf_new_array(pdf, rbGroupsList.Count);
                foreach (var grp in rbGroupsList)
                    mupdf.mupdf.pdf_array_push(rbTop, BuildArray(grp));
                mupdf.mupdf.pdf_dict_put(obj, mupdf.mupdf.pdf_new_name("RBGroups"), rbTop);
            }

            mupdf.mupdf.ll_pdf_read_ocg(pdf.m_internal);
        }

        /// <summary>
        /// Activate an OC layer.
        /// </summary>
        public void SwitchLayer(int config, bool asDefault = false)
        {
            var pdf = NativePdfDocument;
            var cfgs = mupdf.mupdf.pdf_dict_get(
                mupdf.mupdf.pdf_dict_get(
                    mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root")),
                    mupdf.mupdf.pdf_new_name("OCProperties")),
                mupdf.mupdf.pdf_new_name("Configs"));

            if (mupdf.mupdf.pdf_is_array(cfgs) == 0 || mupdf.mupdf.pdf_array_len(cfgs) == 0)
            {
                if (config < 1)
                    return;
                throw new ValueErrorException(Constants.MSG_BAD_OC_LAYER);
            }

            if (config < 0)
                return;

            mupdf.mupdf.pdf_select_layer_config(pdf, config);
            if (asDefault)
            {
                mupdf.mupdf.pdf_set_layer_config_as_default(pdf);
                mupdf.mupdf.ll_pdf_read_ocg(pdf.m_internal);
            }
        }

        // ─── Annotations query ──────────────────────────────────────────

        /// <summary>
        /// Check whether there are annotations on any page.
        /// </summary>
        public bool HasAnnots
        {
            get
            {
                for (int i = 0; i < PageCount; i++)
                {
                    using var page = LoadPage(i);
                    if (page.FirstAnnot != null) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Check whether there are links on any page.
        /// </summary>
        public bool HasLinks
        {
            get
            {
                for (int i = 0; i < PageCount; i++)
                {
                    using var page = LoadPage(i);
                    if (page.FirstLink != null) return true;
                }
                return false;
            }
        }

        // ─── Bake ───────────────────────────────────────────────────────

        /// <summary>
        /// Convert annotations or fields to permanent page content.
        ///
        /// After execution, pages will still look the same, but no longer have annotations or fields.
        /// </summary>
        public void Bake(bool annots = true, bool widgets = true)
        {
            var pdf = NativePdfDocument;
            mupdf.mupdf.pdf_bake_document(pdf, annots ? 1 : 0, widgets ? 1 : 0);
        }

        // ─── Scrub ──────────────────────────────────────────────────────

        /// <summary>
        /// Remove potentially sensitive data from a PDF.
        /// Similar to the Adobe Acrobat 'sanitize' function.
        /// </summary>
        public void Scrub(bool attachedFiles = true, bool cleanPages = true, bool embeddedFiles = true,
            bool hiddenText = true, bool javascript = true, bool metadata = true,
            bool redactions = true, bool redactImages = false, bool removeLinks = true,
            bool resetFields = true, bool resetResponses = true, bool thumbnails = true, bool xmlMetadata = true)
        {
            // if not doc.is_pdf: raise ValueError("is no PDF")
            // After Close(), _nativeDoc is cleared so IsPdf is false; match Python by rejecting closed/encrypted first.
            if (IsEncrypted || IsClosed)
                throw new ValueErrorException("closed or encrypted doc");
            if (!IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);
            var pdf = NativePdfDocument;

            // if not clean_pages: hidden_text = False; redactions = False
            if (!cleanPages)
            {
                hiddenText = false;
                redactions = false;
            }

            if (metadata)
            {
                SetMetadata(new Dictionary<string, string>
                {
                    ["title"] = "", ["author"] = "", ["subject"] = "", ["keywords"] = "",
                    ["creator"] = "", ["producer"] = "", ["creationDate"] = "", ["modDate"] = ""
                });
            }

            if (xmlMetadata)
            {
                var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                mupdf.mupdf.pdf_dict_dels(root, "Metadata");
            }

            if (embeddedFiles)
            {
                var names = EmbfileNames();
                foreach (var name in names)
                {
                    try { EmbfileDel(name); } catch { }
                }
            }

            if (javascript)
            {
                var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                mupdf.mupdf.pdf_dict_dels(root, "OpenAction");
                mupdf.mupdf.pdf_dict_dels(root, "AA");
                var names = mupdf.mupdf.pdf_dict_get(root, mupdf.mupdf.pdf_new_name("Names"));
                if (names.m_internal != null)
                    mupdf.mupdf.pdf_dict_dels(names, "JavaScript");
            }

            for (int i = 0; i < PageCount; i++)
            {
                using var page = LoadPage(i);
                var pdfPage = Helpers.AsPdfPage(page.NativePage, required: false);
                if (pdfPage == null || pdfPage.m_internal == null) continue;

                if (resetFields)
                {
                    // reset form fields (widgets)
                    foreach (var widget in page.Widgets())
                        widget.Reset();
                }

                if (removeLinks)
                {
                    var annotArr = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Annots"));
                    if (annotArr.m_internal != null)
                    {
                        int n = mupdf.mupdf.pdf_array_len(annotArr);
                        for (int j = n - 1; j >= 0; j--)
                        {
                            var obj = mupdf.mupdf.pdf_array_get(annotArr, j);
                            var subtype = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("Subtype"));
                            if (mupdf.mupdf.pdf_name_eq(subtype, mupdf.mupdf.pdf_new_name("Link")) != 0)
                                mupdf.mupdf.pdf_array_delete(annotArr, j);
                        }
                    }
                }

                bool foundRedacts = false;
                foreach (var annot in page.Annots())
                {
                    if (annot.Type == AnnotationType.FileAttachment && attachedFiles)
                        annot.UpdateFile(buffer: new byte[] { (byte)' ' });
                    if (resetResponses)
                        annot.DeleteResponses();
                    if (annot.Type == AnnotationType.Redact)
                        foundRedacts = true;
                }

                if (redactions && foundRedacts)
                    page.ApplyRedactions(images: redactImages ? 1 : 0);

                if (thumbnails)
                    mupdf.mupdf.pdf_dict_dels(pdfPage.obj(), "Thumb");

                if (cleanPages)
                {
                    try { page.CleanContents(); } catch { }
                }
            }

            // pages are scrubbed, now perform document-wide scrubbing
            if (!(xmlMetadata || javascript))
                return;
            int xrefLimit = XrefLength;
            for (int xref = 1; xref < xrefLimit; xref++)
            {
                if (string.IsNullOrEmpty(XrefObject(xref)))
                    throw new ValueErrorException($"bad xref {xref} - clean PDF before scrubbing");

                if (javascript && XrefGetKey(xref, "S").value == "/JavaScript")
                {
                    // a /JavaScript action object
                    UpdateObject(xref, "<</S/JavaScript/JS()>>"); // replace with a null JavaScript
                    continue;
                }

                if (!xmlMetadata)
                    continue;

                if (XrefGetKey(xref, "Type").value == "/Metadata")
                {
                    // delete any metadata object directly
                    UpdateObject(xref, "<<>>");
                    UpdateStream(xref, System.Text.Encoding.ASCII.GetBytes("deleted"), compress: false);
                    continue;
                }

                if (XrefGetKey(xref, "Metadata").type != "null")
                    XrefSetKey(xref, "Metadata", "null");
            }
        }

        // ─── Resolve Link ───────────────────────────────────────────────

        /// <summary>
        /// Calculate internal link destination.
        ///
        /// Args:
        /// uri: (str) some Link.uri
        /// chapters: (bool) whether to use (chapter, page) format
        /// Returns:
        /// (page_id, x, y) where x, y are point coordinates on the page.
        /// page_id is either page number (if chapters=false), or (chapter, pno).
        /// </summary>
        public (int page, float x, float y) ResolveLink(string uri)
        {
            var result = ResolveLink(uri, chapters: false);
            int page = result.pageId is int p ? p : -1;
            return (page, result.x, result.y);
        }

        /// <summary>
        /// Calculate internal link destination with optional chapter-based page id.
        ///
        /// If chapters is false, pageId is an int page number.
        /// If chapters is true, pageId is a (chapter, page) tuple.
        /// </summary>
        public (object pageId, float x, float y) ResolveLink(string uri, bool chapters)
        {
            if (string.IsNullOrEmpty(uri))
            {
                if (chapters)
                    return ((-1, -1), 0, 0);
                return (-1, 0, 0);
            }

            try
            {
                var outparams = new mupdf.ll_fz_resolve_link_outparams();
                var loc = mupdf.mupdf.ll_fz_resolve_link_outparams_fn(NativeDocument.m_internal, uri, outparams);
                float xp = outparams.xp;
                float yp = outparams.yp;

                if (chapters)
                    return ((loc.chapter, loc.page), xp, yp);

                int pno = mupdf.mupdf.fz_page_number_from_location(NativeDocument, new mupdf.FzLocation(loc));
                return (pno, xp, yp);
            }
            catch
            {
                if (chapters)
                    return ((-1, -1), 0, 0);
                return (-1, 0, 0);
            }
        }

        // ─── Subset / Rewrite ───────────────────────────────────────────

        /// <summary>
        /// Subset embedded fonts to reduce file size.
        /// </summary>
        public void SubsetFonts()
        {
            var pdf = NativePdfDocument;
            var pages = new mupdf.vectori();
            for (int i = 0; i < PageCount; i++) pages.Add(i);
            mupdf.mupdf.pdf_subset_fonts2(pdf, pages);
        }

        /// <summary>
        /// Rewrite images in a PDF document.
        ///
        /// The typical use case is to reduce the size of the PDF by recompressing
        /// images. Default parameters will convert all images to JPEG where
        /// possible, using the specified quality.
        /// </summary>
        public void RewriteImages(int quality = 0, int dpiThreshold = 0, int dpiTarget = 0,
            bool lossy = true, bool lossless = true, bool bitonal = true,
            bool color = true, bool gray = true)
        {
            EnsurePdf();
            var pdf = NativePdfDocument;
            string qualityStr = quality.ToString();

            var opts = new mupdf.PdfImageRewriterOptions();
            if (bitonal)
            {
                opts.bitonal_image_recompress_method = mupdf.mupdf.FZ_RECOMPRESS_FAX;
                opts.bitonal_image_subsample_method = mupdf.mupdf.FZ_SUBSAMPLE_AVERAGE;
                opts.bitonal_image_subsample_to = dpiTarget;
                opts.bitonal_image_recompress_quality = qualityStr;
                opts.bitonal_image_subsample_threshold = dpiThreshold;
            }
            if (color)
            {
                if (lossless)
                {
                    opts.color_lossless_image_recompress_method = mupdf.mupdf.FZ_RECOMPRESS_JPEG;
                    opts.color_lossless_image_subsample_method = mupdf.mupdf.FZ_SUBSAMPLE_AVERAGE;
                    opts.color_lossless_image_subsample_to = dpiTarget;
                    opts.color_lossless_image_subsample_threshold = dpiThreshold;
                    opts.color_lossless_image_recompress_quality = qualityStr;
                }
                if (lossy)
                {
                    opts.color_lossy_image_recompress_method = mupdf.mupdf.FZ_RECOMPRESS_JPEG;
                    opts.color_lossy_image_subsample_method = mupdf.mupdf.FZ_SUBSAMPLE_AVERAGE;
                    opts.color_lossy_image_subsample_threshold = dpiThreshold;
                    opts.color_lossy_image_subsample_to = dpiTarget;
                    opts.color_lossy_image_recompress_quality = qualityStr;
                }
            }
            if (gray)
            {
                if (lossless)
                {
                    opts.gray_lossless_image_recompress_method = mupdf.mupdf.FZ_RECOMPRESS_JPEG;
                    opts.gray_lossless_image_subsample_method = mupdf.mupdf.FZ_SUBSAMPLE_AVERAGE;
                    opts.gray_lossless_image_subsample_to = dpiTarget;
                    opts.gray_lossless_image_subsample_threshold = dpiThreshold;
                    opts.gray_lossless_image_recompress_quality = qualityStr;
                }
                if (lossy)
                {
                    opts.gray_lossy_image_recompress_method = mupdf.mupdf.FZ_RECOMPRESS_JPEG;
                    opts.gray_lossy_image_subsample_method = mupdf.mupdf.FZ_SUBSAMPLE_AVERAGE;
                    opts.gray_lossy_image_subsample_threshold = dpiThreshold;
                    opts.gray_lossy_image_subsample_to = dpiTarget;
                    opts.gray_lossy_image_recompress_quality = qualityStr;
                }
            }

            mupdf.mupdf.pdf_rewrite_images(pdf, opts);
        }

        /// <summary>
        /// Change the color component count on all pages.
        /// </summary>
        public void Recolor(int components = 1)
        {
            if (!IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);
            var pdf = NativePdfDocument;
            var opts = new mupdf.PdfRecolorOptions();
            opts.num_comp = components;
            for (int i = 0; i < PageCount; i++)
                mupdf.mupdf.pdf_recolor_page(pdf, i, opts);
        }

        // ─── SetLanguage ────────────────────────────────────────────────

        /// <summary>
        /// Set document language.
        /// </summary>
        public void SetLanguage(string language)
        {
            var pdf = NativePdfDocument;
            // if not language: lang = mupdf.FZ_LANG_UNSET
            // else: lang = mupdf.fz_text_language_from_string(language)
            var lang = string.IsNullOrEmpty(language)
                ? mupdf.fz_text_language.FZ_LANG_UNSET
                : mupdf.mupdf.fz_text_language_from_string(language);
            // mupdf.pdf_set_document_language(pdf, lang)
            mupdf.mupdf.pdf_set_document_language(pdf, lang);
        }

        // ─── Need Appearances ───────────────────────────────────────────

        /// <summary>
        /// Get/set the NeedAppearances value.
        /// </summary>
        public bool NeedAppearances
        {
            get
            {
                try
                {
                    var pdf = NativePdfDocument;
                    var acro = Helpers.PdfDictGetl(mupdf.mupdf.pdf_trailer(pdf),
                        mupdf.mupdf.pdf_new_name("Root"), mupdf.mupdf.pdf_new_name("AcroForm"));
                    if (acro.m_internal == null) return false;
                    var na = mupdf.mupdf.pdf_dict_gets(acro, "NeedAppearances");
                    return na.m_internal != null && mupdf.mupdf.pdf_to_bool(na) != 0;
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// Get/set the NeedAppearances value.
        /// </summary>
        public void SetNeedAppearances(bool value)
        {
            var pdf = NativePdfDocument;
            var acro = Helpers.PdfDictGetl(mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"), mupdf.mupdf.pdf_new_name("AcroForm"));
            if (acro.m_internal == null) return;
            mupdf.mupdf.pdf_dict_put_bool(acro, mupdf.mupdf.pdf_new_name("NeedAppearances"), value ? 1 : 0);
        }

        // ─── MarkInfo ───────────────────────────────────────────────────

        /// <summary>
        /// Return the PDF MarkInfo value.
        /// </summary>
        public Dictionary<string, bool> MarkInfo
        {
            get
            {
                var result = new Dictionary<string, bool>();
                try
                {
                    var pdf = NativePdfDocument;
                    var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                    var mi = mupdf.mupdf.pdf_dict_gets(root, "MarkInfo");
                    if (mi.m_internal == null) return result;
                    foreach (var key in new[] { "Marked", "UserProperties", "Suspects" })
                    {
                        var val = mupdf.mupdf.pdf_dict_gets(mi, key);
                        result[key] = val.m_internal != null && mupdf.mupdf.pdf_to_bool(val) != 0;
                    }
                }
                catch { }
                return result;
            }
        }

        /// <summary>
        /// Set the PDF MarkInfo value.
        /// </summary>
        public bool SetMarkInfo(Dictionary<string, object> markinfo)
        {
            int xref = PdfCatalog;
            if (xref == 0)
                throw new ValueErrorException("not a PDF");
            if (markinfo == null || markinfo.Count == 0)
                return false;

            var valid = new Dictionary<string, object>
            {
                ["Marked"] = false,
                ["UserProperties"] = false,
                ["Suspects"] = false
            };

            var extra = new List<string>();
            foreach (var key in markinfo.Keys)
            {
                if (!valid.ContainsKey(key))
                    extra.Add(key);
            }
            if (extra.Count > 0)
            {
                extra.Sort(StringComparer.Ordinal);
                var quoted = new List<string>(extra.Count);
                foreach (var k in extra)
                    quoted.Add("'" + k + "'");
                string inner = string.Join(", ", quoted);
                throw new ValueErrorException($"bad MarkInfo key(s): {{{inner}}}");
            }

            foreach (var kv in markinfo)
                valid[kv.Key] = kv.Value;

            string pdfdict = "<<";
            foreach (var kv in valid)
            {
                string value = Convert.ToString(kv.Value, System.Globalization.CultureInfo.InvariantCulture);
                value = (value ?? "").ToLowerInvariant();
                if (value != "true" && value != "false")
                    throw new ValueErrorException($"bad key value '{kv.Key}': '{value}'");
                pdfdict += $"/{kv.Key} {value}";
            }
            pdfdict += ">>";
            XrefSetKey(xref, "MarkInfo", pdfdict);
            return true;
        }

        // ─── PageLayout / PageMode ──────────────────────────────────────

        /// <summary>
        /// Return the PDF PageLayout value.
        /// </summary>
        public string PageLayout
        {
            get
            {
                try
                {
                    var pdf = NativePdfDocument;
                    var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                    var val = mupdf.mupdf.pdf_dict_gets(root, "PageLayout");
                    return val.m_internal != null ? mupdf.mupdf.pdf_to_name(val) : "SinglePage";
                }
                catch { return "SinglePage"; }
            }
        }

        /// <summary>
        /// Set the PDF PageLayout value.
        /// </summary>
        public void SetPageLayout(string layout)
        {
            string[] valid = { "SinglePage", "OneColumn", "TwoColumnLeft", "TwoColumnRight", "TwoPageLeft", "TwoPageRight" };
            int xref = PdfCatalog;
            if (xref == 0)
                throw new ValueErrorException("not a PDF");
            // if not pagelayout: raise ValueError("bad PageLayout value")
            if (string.IsNullOrEmpty(layout))
                throw new ValueErrorException("bad PageLayout value");
            // if pagelayout[0] == "/": pagelayout = pagelayout[1:]
            if (layout[0] == '/')
                layout = layout.Substring(1);
            // for v in valid: if pagelayout.lower() == v.lower(): ...
            foreach (string v in valid)
            {
                if (string.Equals(layout, v, StringComparison.OrdinalIgnoreCase))
                {
                    // self.xref_set_key(xref, "PageLayout", f"/{v}")
                    XrefSetKey(xref, "PageLayout", "/" + v);
                    return;
                }
            }
            throw new ValueErrorException("bad PageLayout value");
        }

        /// <summary>
        /// Return the PDF PageMode value.
        /// </summary>
        public string PageMode
        {
            get
            {
                try
                {
                    var pdf = NativePdfDocument;
                    var root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                    var val = mupdf.mupdf.pdf_dict_gets(root, "PageMode");
                    return val.m_internal != null ? mupdf.mupdf.pdf_to_name(val) : "UseNone";
                }
                catch { return "UseNone"; }
            }
        }

        /// <summary>
        /// Set the PDF PageMode value.
        /// </summary>
        public void SetPageMode(string mode)
        {
            string[] valid = { "UseNone", "UseOutlines", "UseThumbs", "FullScreen", "UseOC", "UseAttachments" };
            int xref = PdfCatalog;
            if (xref == 0)
                throw new ValueErrorException("not a PDF");
            // if not pagemode: raise ValueError("bad PageMode value")
            if (string.IsNullOrEmpty(mode))
                throw new ValueErrorException("bad PageMode value");
            // if pagemode[0] == "/": pagemode = pagemode[1:]
            if (mode[0] == '/')
                mode = mode.Substring(1);
            // for v in valid: if pagemode.lower() == v.lower(): ...
            foreach (string v in valid)
            {
                if (string.Equals(mode, v, StringComparison.OrdinalIgnoreCase))
                {
                    // self.xref_set_key(xref, "PageMode", f"/{v}")
                    XrefSetKey(xref, "PageMode", "/" + v);
                    return;
                }
            }
            throw new ValueErrorException("bad PageMode value");
        }

        // ─── Signature flags ────────────────────────────────────────────

        /// <summary>
        /// Get the /SigFlags value.
        /// </summary>
        public int GetSigFlags()
        {
            if (!IsPdf)
                return -1;   // not a PDF
            var pdf = NativePdfDocument;
            var sigflags = Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"),
                mupdf.mupdf.pdf_new_name("AcroForm"),
                mupdf.mupdf.pdf_new_name("SigFlags"));
            int sigflag = -1;
            if (sigflags.m_internal != null)
                sigflag = mupdf.mupdf.pdf_to_int(sigflags);
            return sigflag;
        }

        // ─── Page CropBox ───────────────────────────────────────────────

        /// <summary>
        /// Get CropBox of page number (without loading page).
        /// </summary>
        public Rect PageCropBox(int pno)
        {
            if (IsClosed)
                throw new ValueErrorException("document closed");
            int pageCount = PageCount;
            int n = pno;
            while (n < 0)
                n += pageCount;
            var pdf = NativePdfDocument;
            if (n >= pageCount)
                throw new ValueErrorException(Constants.MSG_BAD_PAGENO);
            var page_obj = mupdf.mupdf.pdf_lookup_page_obj(pdf, n);
            var cropbox = mupdf.mupdf.pdf_dict_get_inheritable(page_obj, mupdf.mupdf.pdf_new_name("CropBox"));
            if (cropbox.m_internal != null)
            {
                var r = mupdf.mupdf.pdf_to_rect(cropbox);
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
            var mb = mupdf.mupdf.pdf_dict_get_inheritable(page_obj, mupdf.mupdf.pdf_new_name("MediaBox"));
            if (mb.m_internal != null)
            {
                var r = mupdf.mupdf.pdf_to_rect(mb);
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
            return new Rect(0, 0, 595, 842);
        }

        /// <summary>
        /// Save a file snapshot suitable for journalling.
        /// </summary>
        public void SaveSnapshot(string filename)
        {
            if (IsClosed) throw new ValueErrorException("doc is closed");
            if (string.IsNullOrEmpty(filename))
                throw new ValueErrorException("filename must be str, Path or file object");
            if (!string.IsNullOrEmpty(Name) && filename == Name)
                throw new ValueErrorException("cannot snapshot to original");
            var pdf = NativePdfDocument;
            mupdf.mupdf.pdf_save_snapshot(pdf, filename);
        }

        /// <summary>
        /// Save a file snapshot suitable for journalling.
        /// </summary>
        public void SaveSnapshot(object filename)
        {
            // if type(filename) is str: pass
            // elif hasattr(filename, "open"): filename = str(filename)
            // elif hasattr(filename, "name"): filename = filename.name
            string target = null;
            if (filename is string s)
            {
                target = s;
            }
            else if (filename is FileInfo fi)
            {
                target = fi.FullName;
            }
            else if (filename != null)
            {
                var nameProp = filename.GetType().GetProperty("Name");
                if (nameProp != null && nameProp.CanRead)
                    target = nameProp.GetValue(filename)?.ToString();
            }
            else
            {
                throw new ValueErrorException("filename must be str, Path or file object");
            }
            if (string.IsNullOrEmpty(target))
                throw new ValueErrorException("filename must be str, Path or file object");
            SaveSnapshot(target);
        }

        // ─── Close ──────────────────────────────────────────────────────

        /// <summary>
        /// Close document.
        /// </summary>
        public void Close()
        {
            if (!IsClosed)
            {
                ResetPageRefsInternal();
                Graftmaps.Clear();
                IsClosed = true;
                _nativeDoc?.Dispose();
                _nativeDoc = null;
            }
        }

        internal static int NextPageRefId() => Interlocked.Increment(ref _nextPageRefId);

        internal void RegisterPageRef(Page page)
        {
            if (page == null) return;
            lock (_pageRefs)
                _pageRefs[page.PageRefId] = new WeakReference<Page>(page);
        }

        internal void ForgetPageRef(Page page)
        {
            if (page == null) return;
            lock (_pageRefs)
                _pageRefs.Remove(page.PageRefId);
        }

        /// <summary>
        /// Port of Python <c>Document._reset_page_refs</c>: invalidate all tracked <see cref="Page"/> wrappers.
        /// </summary>
        internal void ResetPageRefsInternal()
        {
            if (IsClosed || IsEncrypted)
                return;

            List<Page> pages;
            lock (_pageRefs)
            {
                pages = new List<Page>(_pageRefs.Count);
                foreach (var kv in _pageRefs)
                {
                    if (kv.Value != null && kv.Value.TryGetTarget(out var p) && p != null)
                        pages.Add(p);
                }
                _pageRefs.Clear();
            }

            for (int i = 0; i < pages.Count; i++)
                pages[i]?._erase();
        }

        // ─── Internal ───────────────────────────────────────────────────

        private void InitDoc()
        {
            // Called after authentication or on open
        }

        /// <summary>PDF-only mutator guard (cf. Python <c>set_metadata</c>, <c>delete_pages</c>): reject closed/encrypted before <c>is no PDF</c> because <see cref="Close"/> clears the native handle.</summary>
        private void EnsurePdf()
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            if (!IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);
        }

        /// <summary>Python <c>delete_pages</c> / <c>copy_page</c> / <c>move_page</c> opening: <c>document closed</c> then <c>is no PDF</c> (closed first so a handle-cleared doc is not misreported as not-PDF).</summary>
        private void EnsurePdfOpenForDeletePages()
        {
            if (IsClosed)
                throw new ValueErrorException("document closed");
            if (!IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);
        }

        private void EnsureNotClosed()
        {
            if (IsClosed)
                throw new ValueErrorException("document closed");
            if (IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
        }

        private mupdf.PdfObj GetEmbeddedFilesNamesArray()
        {
            var pdf = NativePdfDocument;
            return Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"),
                mupdf.mupdf.pdf_new_name("Names"),
                mupdf.mupdf.pdf_new_name("EmbeddedFiles"),
                mupdf.mupdf.pdf_new_name("Names"));
        }

        private mupdf.PdfObj EnsureEmbeddedFilesNamesArray()
        {
            var pdf = NativePdfDocument;
            var trailer = mupdf.mupdf.pdf_trailer(pdf);
            var root = mupdf.mupdf.pdf_dict_get(trailer, mupdf.mupdf.pdf_new_name("Root"));
            var names = mupdf.mupdf.pdf_dict_get(root, mupdf.mupdf.pdf_new_name("Names"));
            if (names.m_internal == null)
            {
                names = mupdf.mupdf.pdf_new_dict(pdf, 1);
                mupdf.mupdf.pdf_dict_put(root, mupdf.mupdf.pdf_new_name("Names"), names);
            }

            var embedded = mupdf.mupdf.pdf_dict_get(names, mupdf.mupdf.pdf_new_name("EmbeddedFiles"));
            if (embedded.m_internal == null)
            {
                embedded = mupdf.mupdf.pdf_new_dict(pdf, 1);
                mupdf.mupdf.pdf_dict_put(names, mupdf.mupdf.pdf_new_name("EmbeddedFiles"), embedded);
            }

            var arr = mupdf.mupdf.pdf_dict_get(embedded, mupdf.mupdf.pdf_new_name("Names"));
            if (arr.m_internal == null || mupdf.mupdf.pdf_is_array(arr) == 0)
            {
                arr = mupdf.mupdf.pdf_new_array(pdf, 8);
                mupdf.mupdf.pdf_dict_put(embedded, mupdf.mupdf.pdf_new_name("Names"), arr);
            }

            return arr;
        }

        // ─── IDisposable ────────────────────────────────────────────────

        public void Dispose()
        {
            if (!_disposed) { Close(); _disposed = true; }
            GC.SuppressFinalize(this);
        }

        ~Document() { Dispose(); }

        // ─── IEnumerable<Page> ──────────────────────────────────────────

        public IEnumerator<Page> GetEnumerator()
        {
            for (int i = 0; i < PageCount; i++)
                yield return LoadPage(i);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            string p = IsClosed ? "closed " : "";
            if (StreamData != null) return $"{p}Document('{Name}', <memory, doc# {_graftId}>)";
            if (string.IsNullOrEmpty(Name)) return $"{p}Document(<new PDF, doc# {_graftId}>)";
            return $"{p}Document('{Name}')";
        }
    }
}
