using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a document opened from a file path, byte buffer, or created as a new PDF.
    /// </summary>
    /// <remarks>
    /// <para>Ports PyMuPDF <c>class Document</c> (<c>src/__init__.py</c>). Modern members use C# naming
    /// (<see cref="LoadPage"/>, <see cref="Metadata"/>); legacy readthedocs names live in
    /// <c>Document.Legacy.cs</c> (<see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/>).</para>
    /// <para>After structural PDF changes (<see cref="InsertPdf"/>, <see cref="Select"/>, <see cref="DeletePage"/>, …),
    /// refresh cached <see cref="Page"/> references and counts — see PyMuPDF referential integrity notes.</para>
    /// </remarks>
    public partial class Document : IDisposable, IEnumerable<Page>
    {
        private mupdf.FzDocument? _nativeDoc;
        private mupdf.PdfDocument _cachedPdfDocument;
        private bool _disposed;
        /// <summary>
        /// Gets or sets Gets or sets whether this wrapper owns the native document handle.
        /// </summary>
        /// <value>Gets or sets whether this wrapper owns the native document handle.</value>
        /// <remarks>PyMuPDF <c>Document.this_own</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool ThisOwn { get; set; } = true;
        private int _graftId;
        private static int _nextGraftId;
        private static int _nextPageRefId;
        private Dictionary<string, Dictionary<string, object>> _resolvedNames;
        /// <summary>PyMuPDF <c>Document._outline</c> — first outline, set by <see cref="InitDoc"/>.</summary>
        private Outline _outline;

        /// <summary>
        /// Loaded <see cref="Page"/> wrappers keyed by <see cref="Page.PageRefId"/> (Python <c>_page_refs</c>).
        /// Strong references so <see cref="ResetPageRefsInternal"/> can invalidate the cache (legacy: clear only; optional erase).
        /// </summary>
        private readonly Dictionary<int, Page> _pageRefs = new Dictionary<int, Page>();
        private int _suppressPageRefReset;
        /// <summary>
        /// Gets or sets has document been closed?.
        /// </summary>
        /// <value>has document been closed?</value>
        /// <remarks>PyMuPDF <c>Document.is_closed</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool IsClosed { get; private set; }
        /// <summary>
        /// Gets or sets document (still) encrypted?.
        /// </summary>
        /// <value>document (still) encrypted?</value>
        /// <remarks>PyMuPDF <c>Document.is_encrypted</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool IsEncrypted { get; private set; }
        /// <summary>
        /// Gets or sets Gets the file path or "&lt;memory&gt;" for stream-backed documents.
        /// </summary>
        /// <value>Gets the file path or "&lt;memory&gt;" for stream-backed documents.</value>
        /// <remarks>PyMuPDF <c>Document.name</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public string Name { get; private set; } = "";
        /// <summary>
        /// Gets or sets Gets the in-memory file bytes when the document was opened from memory.
        /// </summary>
        /// <value>Gets the in-memory file bytes when the document was opened from memory.</value>
        /// <remarks>PyMuPDF <c>Document.stream_data</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public byte[] StreamData { get; private set; }

        internal List<object[]> FontInfos { get; } = new List<object[]>();
        internal Dictionary<int, Graftmap?> Graftmaps { get; } = new Dictionary<int, Graftmap?>();
        /// <summary>PyMuPDF <c>Document.ShownPages</c> — (source graft id, source page number) → reused Form XObject xref.</summary>
        internal Dictionary<(int srcGraftId, int pno), int> ShownPages { get; } = new Dictionary<(int, int), int>();
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
                if (_cachedPdfDocument != null)
                    return _cachedPdfDocument;
                var owned = NativeDocument.pdf_document_from_fz_document();
                if (owned.m_internal == null)
                    throw new InvalidOperationException(Constants.MSG_IS_NO_PDF);
                // Borrowed from FzDocument — must not pdf_drop_document on dispose.
                _cachedPdfDocument = Helpers.PdfDocumentBorrowed(owned);
                return _cachedPdfDocument;
            }
        }

        internal void DisposeCachedPdfDocument()
        {
            _cachedPdfDocument = null;
        }

        internal int GraftId => _graftId;

        // ─── Constructors ───────────────────────────────────────────────

        /// <summary>
        /// Creates a new empty PDF document (PyMuPDF <c>fitz.open()</c> / <see cref="Open()"/>).
        /// </summary>
        /// <remarks>Alias: <see cref="Open()"/>.</remarks>
        public Document()
        {
            Helpers.EnsureMupdfWarningsHooked();
            var pdf = new mupdf.PdfDocument();
            _nativeDoc = new mupdf.FzDocument(pdf);
            _graftId = _nextGraftId++;
            Name = "";
            InitDoc();
        }

        /// <summary>
        /// Opens a document from a file path (content type detected from file bytes or extension).
        /// </summary>
        /// <param name="filename">Path to the file; must exist and be non-empty.</param>
        /// <param name="filetype">Optional type hint when detection fails (e.g. <c>txt</c>, <c>html</c>).</param>
        /// <param name="rect">Layout rectangle for reflowable documents (origin at top-left).</param>
        /// <param name="width">Page width if <paramref name="rect"/> is omitted.</param>
        /// <param name="height">Page height if <paramref name="rect"/> is omitted.</param>
        /// <param name="fontsize">Default font size for reflowable layout.</param>
        /// <exception cref="FileNotFoundException">File not found.</exception>
        /// <exception cref="EmptyFileException">File is empty.</exception>
        /// <exception cref="FileDataException">File cannot be opened as a document.</exception>
        public Document(string filename, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
        {
            Helpers.EnsureMupdfWarningsHooked();
            _graftId = _nextGraftId++;
            if (!File.Exists(filename))
                throw new FileNotFoundException($"no such file: '{filename}'");
            if (Directory.Exists(filename))
                throw new FileDataException($"'{filename}' is no file");
            if (new global::System.IO.FileInfo(filename).Length == 0)
                throw new EmptyFileException($"Cannot open empty file: {filename}");

            Name = Path.GetFullPath(filename);
            float w = width, h = height;
            if (rect != null)
            {
                var r = rect.ToFzRect();
                if (mupdf.mupdf.fz_is_infinite_rect(r) == 0) { w = r.x1 - r.x0; h = r.y1 - r.y0; }
            }

            mupdf.FzDocument doc;
            try
            {
                // fz_open_document(path) keeps the file open on Windows; load bytes so
                // callers can overwrite or move the path after Close() (Demo TestMoveFile).
                if (string.IsNullOrEmpty(filetype))
                {
                    StreamData = File.ReadAllBytes(filename);
                    doc = OpenNativeFromBytes(StreamData, null);
                }
                else
                    doc = OpenNativeFromFilename(filename, filetype);
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
        /// Opens a document from a byte buffer (<see cref="Name"/> becomes <c>&lt;memory&gt;</c>).
        /// </summary>
        /// <param name="data">Non-empty file content.</param>
        /// <param name="filetype">Optional type hint when detection fails.</param>
        /// <param name="rect">Layout rectangle for reflowable documents.</param>
        /// <param name="width">Page width if <paramref name="rect"/> is omitted.</param>
        /// <param name="height">Page height if <paramref name="rect"/> is omitted.</param>
        /// <param name="fontsize">Default font size for reflowable layout.</param>
        /// <exception cref="EmptyFileException"><paramref name="data"/> is null or empty.</exception>
        /// <exception cref="FileDataException">Buffer cannot be opened as a document.</exception>
        public Document(byte[] data, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
        {
            InitFromByteArray(data, filetype, rect, width, height, fontsize);
        }

        private void InitFromByteArray(byte[] data, string filetype, Rect rect, float width, float height, float fontsize)
        {
            Helpers.EnsureMupdfWarningsHooked();
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
                doc = OpenNativeFromBytes(data, filetype);
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

        /// <summary>PyMuPDF opens paths with <c>fz_open_document</c> when <paramref name="filetype"/> is unset.</summary>
        private static mupdf.FzDocument OpenNativeFromFilename(string filename, string filetype)
        {
            lock (Utils.MuPDFLock)
            {
                if (string.IsNullOrEmpty(filetype))
                    return mupdf.mupdf.fz_open_document(filename);
                using var fzStream = mupdf.mupdf.fz_open_file(filename);
                return mupdf.mupdf.fz_open_document_with_stream(filetype, fzStream);
            }
        }

        /// <summary>Opens memory bytes; disposes the temporary buffer/stream after MuPDF takes the document (MuPDF.NET pattern).</summary>
        private static mupdf.FzDocument OpenNativeFromBytes(byte[] data, string filetype)
        {
            using var mem = Helpers.BufferFromBytes(data);
            using var bufStream = mupdf.mupdf.fz_open_buffer(mem);
            lock (Utils.MuPDFLock)
                return mupdf.mupdf.fz_open_document_with_stream(filetype ?? "", bufStream);
        }

        /// <summary>
        /// Opens a document from a readable <see cref="Stream"/> (PyMuPDF <c>fitz.open(stream=…)</c>).
        /// </summary>
        /// <param name="stream">Readable stream; read to EOF (seekable streams are rewound to position 0 first).</param>
        /// <param name="filetype">Optional type hint when content detection fails.</param>
        /// <param name="rect">Layout rectangle for reflowable documents.</param>
        /// <param name="width">Page width if <paramref name="rect"/> is omitted.</param>
        /// <param name="height">Page height if <paramref name="rect"/> is omitted.</param>
        /// <param name="fontsize">Default font size for reflowable layout.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        /// <exception cref="EmptyFileException">Stream has no data.</exception>
        /// <exception cref="FileDataException">Stream cannot be opened as a document.</exception>
        public Document(Stream stream, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            : this(ReadStreamFully(stream), filetype, rect, width, height, fontsize)
        {
        }

        // ─── Static factory (Python fitz.open) ───────────────────────────

        /// <summary>Creates a new empty PDF (<see cref="Document()"/>).</summary>
        /// <remarks>PyMuPDF alias: <c>fitz.open()</c>.</remarks>
        public static Document Open() => new Document();

        /// <summary>Opens a document from a file path.</summary>
        /// <inheritdoc cref="Document(string, string, Rect, float, float, float)"/>
        public static Document Open(string filename, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            => new Document(filename, filetype, rect, width, height, fontsize);

        /// <summary>Opens a document from a byte buffer.</summary>
        /// <inheritdoc cref="Document(byte[], string, Rect, float, float, float)"/>
        public static Document Open(byte[] data, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            => new Document(data, filetype, rect, width, height, fontsize);

        /// <summary>Opens a document from a readable stream.</summary>
        /// <inheritdoc cref="Document(Stream, string, Rect, float, float, float)"/>
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
        /// Gets is this a PDF?.
        /// </summary>
        /// <value>is this a PDF?</value>
        /// <remarks>PyMuPDF <c>Document.is_pdf</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool IsPdf
        {
            get
            {
                if (IsClosed || _nativeDoc == null)
                    return false;
                if (_nativeDoc is mupdf.PdfDocument)
                    return true;
                try
                {
                    return mupdf.mupdf.ll_pdf_specifics(_nativeDoc.m_internal) != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>PyMuPDF <c>g_use_extra</c>.</summary>
        private static bool GUseExtra => true;
        /// <summary>
        /// Gets number of pages.
        /// </summary>
        /// <value>number of pages</value>
        /// <remarks>PyMuPDF <c>Document.page_count</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int PageCount
        {
            get
            {
                if (IsClosed)
                    throw new ValueErrorException("document closed");
                if (GUseExtra)
                    return IsPdf ? QueryPageCountPdf(this) : QueryPageCountFz(this);
                if (_nativeDoc is mupdf.FzDocument)
                    return mupdf.mupdf.fz_count_pages((mupdf.FzDocument)_nativeDoc);
                return mupdf.mupdf.pdf_count_pages(NativePdfDocument);
            }
        }
        /// <summary>
        /// Gets whether the native handle was released after Close.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.is_native_released</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool IsNativeReleased => _nativeDoc == null;

        private static int QueryPageCountFz(Document self) =>
            mupdf.mupdf.fz_count_pages(self.NativeDocument);

        /// <summary>PyMuPDF <c>extra.page_count_pdf</c> (<c>src/extra.i</c>).</summary>
        private static int QueryPageCountPdf(Document self) => QueryPageCountFz(self);
        /// <summary>
        /// number of chapters
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.chapter_count</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int ChapterCount => mupdf.mupdf.fz_count_chapters(NativeDocument);
        /// <summary>
        /// require password to access data?
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.needs_pass</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool NeedsPass => mupdf.mupdf.fz_needs_password(NativeDocument) != 0;
        /// <summary>
        /// is this a reflowable document?
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.is_reflowable</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool IsReflowable => mupdf.mupdf.fz_is_document_reflowable(NativeDocument) != 0;
        /// <summary>
        /// Gets PDF only: has document been changed yet?.
        /// </summary>
        /// <value>PDF only: has document been changed yet?</value>
        /// <remarks>PyMuPDF <c>Document.is_dirty</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool IsDirty
        {
            get
            {
                if (!IsPdf) return false;
                return mupdf.mupdf.pdf_has_unsaved_changes(NativePdfDocument) != 0;
            }
        }
        /// <summary>
        /// Gets is this a Form PDF?.
        /// </summary>
        /// <value>is this a Form PDF?</value>
        /// <remarks>PyMuPDF <c>Document.is_form_pdf</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool IsFormPdf
        {
            get
            {
                mupdf.PdfDocument pdf = Helpers.AsPdfDocument(this, required: false);
                if (pdf.m_internal == null)
                    return false;
                int count = -1;
                try
                {
                    mupdf.PdfObj fields = PdfDictGetl(
                            mupdf.mupdf.pdf_trailer(pdf),
                            mupdf.mupdf.PDF_ENUM_NAME_Root,
                            mupdf.mupdf.PDF_ENUM_NAME_AcroForm,
                            mupdf.mupdf.PDF_ENUM_NAME_Fields
                            );
                    if (mupdf.mupdf.pdf_is_array(fields) != 0)
                        count = mupdf.mupdf.pdf_array_len(fields);
                }
                catch (Exception)
                {
                    // if g_exceptions_verbose:    exception_info()
                    return false;
                }
                if (count >= 0)
                    return count != 0;
                return false;
            }
        }
        /// <summary>
        /// Gets See PyMuPDF Document.form_fonts.
        /// </summary>
        /// <value>See PyMuPDF Document.form_fonts.</value>
        /// <remarks>PyMuPDF <c>Document.form_fonts</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public List<string> FormFonts
        {
            get
            {
                mupdf.PdfDocument pdf = Helpers.AsPdfDocument(this, required: false);
                if (pdf.m_internal == null)
                    return null;

                mupdf.PdfObj fonts = Helpers.PdfDictGetl(
                    mupdf.mupdf.pdf_trailer(pdf),
                    "Root",
                    "AcroForm",
                    "DR",
                    "Font");
                var names = new List<string>();
                if (fonts.m_internal != null && mupdf.mupdf.pdf_is_dict(fonts) != 0)
                {
                    int n = mupdf.mupdf.pdf_dict_len(fonts);
                    for (int i = 0; i < n; i++)
                    {
                        mupdf.PdfObj key = fonts.pdf_dict_get_key(i);
                        names.Add(Utils.UnicodeFromStr(key.pdf_to_name()));
                    }
                }

                return names;
            }
        }

        private static mupdf.PdfObj PdfDictGetl(mupdf.PdfObj dict, int key0, int key1, int key2)
        {
            if (dict.m_internal == null)
                return new mupdf.PdfObj();
            mupdf.PdfObj current = Helpers.PdfObjDictGet(dict,key0);
            if (current.m_internal == null)
                return new mupdf.PdfObj();
            current = Helpers.PdfObjDictGet(current,key1);
            if (current.m_internal == null)
                return new mupdf.PdfObj();
            return Helpers.PdfObjDictGet(current,key2);
        }
        /// <summary>
        /// Gets PDF only: has this PDF been repaired during open?.
        /// </summary>
        /// <value>PDF only: has this PDF been repaired during open?</value>
        /// <remarks>PyMuPDF <c>Document.is_repaired</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool IsRepaired
        {
            get
            {
                if (!IsPdf) return false;
                return mupdf.mupdf.pdf_was_repaired(NativePdfDocument) != 0;
            }
        }
        /// <summary>
        /// Gets is PDF linearized?.
        /// </summary>
        /// <value>is PDF linearized?</value>
        /// <remarks>PyMuPDF <c>Document.is_fast_webaccess</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool IsFastWebaccess
        {
            get
            {
                try { return mupdf.mupdf.pdf_doc_was_linearized(NativePdfDocument) != 0; }
                catch { return false; }
            }
        }
        /// <summary>
        /// Gets PDF count of versions.
        /// </summary>
        /// <value>PDF count of versions</value>
        /// <remarks>PyMuPDF <c>Document.version_count</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int VersionCount
        {
            get
            {
                try { return mupdf.mupdf.pdf_count_versions(NativePdfDocument); }
                catch { return 0; }
            }
        }
        /// <summary>
        /// Gets See PyMuPDF Document.xref_length.
        /// </summary>
        /// <value>See PyMuPDF Document.xref_length.</value>
        /// <remarks>PyMuPDF <c>Document.xref_length</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int XrefLength
        {
            get
            {
                try { return mupdf.mupdf.pdf_xref_len(NativePdfDocument); }
                catch { return 0; }
            }
        }
        /// <summary>
        /// Gets permissions to access the document.
        /// </summary>
        /// <value>permissions to access the document</value>
        /// <remarks>PyMuPDF <c>Document.permissions</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int Permissions
        {
            get
            {
                try { return mupdf.mupdf.pdf_document_permissions(NativePdfDocument); }
                catch { return 0; }
            }
        }
        /// <summary>
        /// Gets the document language tag from PDF /Root/Lang.
        /// </summary>
        /// <value>the document language tag from PDF /Root/Lang.</value>
        /// <remarks>PyMuPDF <c>Document.language</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
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
        /// Decrypts the document with an owner or user password.
        /// </summary>
        /// <remarks>
        /// PyMuPDF <c>Document.authenticate</c>. On success, <see cref="IsEncrypted"/> becomes
        /// <see langword="false"/> and <see cref="InitDoc"/> runs. MuPDF may ignore permission flags
        /// for owner-authenticated opens (see PyMuPDF docs).
        /// </remarks>
        /// <param name="password">Owner or user password (max 40 characters).</param>
        /// <returns>
        /// MuPDF status: 0 = failed; 1 = no passwords required; 2 = user password; 4 = owner password;
        /// 6 = owner and user passwords are equal.
        /// </returns>
        /// <exception cref="ValueErrorException">Document is closed.</exception>
        public int Authenticate(string password)
        {
            if (IsClosed)
                throw new ValueErrorException("document closed");
            int val = mupdf.mupdf.fz_authenticate_password(NativeDocument, password);
            if (val != 0)
            {
                IsEncrypted = false;
                InitDoc();
            }
            return val;
        }
        /// <summary>
        /// Loads a page by 0-based index for rendering, text extraction, or annotation work.
        /// </summary>
        /// <remarks>
        /// PyMuPDF <c>Document.load_page</c>. Negative <paramref name="pageNo"/> values wrap from the end
        /// (e.g. <c>-1</c> is the last page). Equivalent to <c>doc[pageNo]</c> in Python.
        /// </remarks>
        /// <param name="pageNo">0-based page number. Negative values wrap from the end of the document.</param>
        /// <returns>A new <see cref="Page"/> instance.</returns>
        /// <exception cref="ValueErrorException">Document is closed, encrypted, or page out of range.</exception>
        public Page LoadPage(int pageNo)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            lock (Utils.MuPDFLock)
                return LoadPageCore(pageNo);
        }

        private Page LoadPageCore(int pageNo)
        {
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
            return new Page(fzPage, this, pageNumber: idx);
        }
        /// <summary>
        /// Loads a page by chapter and page index (EPUB and other multi-chapter formats).
        /// </summary>
        /// <remarks>
        /// PyMuPDF <c>Document.load_page((chapter, pno))</c>. Faster than a global page index for large EPUBs.
        /// </remarks>
        /// <param name="chapter">0-based chapter number; must be less than <see cref="ChapterCount"/>.</param>
        /// <param name="pageInChapter">0-based page within the chapter.</param>
        /// <returns>A new <see cref="Page"/> instance.</returns>
        /// <exception cref="ValueErrorException">Document is closed, encrypted, or location out of range.</exception>
        public Page LoadPage(int chapter, int pageInChapter)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            lock (Utils.MuPDFLock)
            {
                if (!ContainsChapterPage(chapter, pageInChapter))
                    throw new ValueErrorException("page not in document");
                var fzPage = mupdf.mupdf.fz_load_chapter_page(NativeDocument, chapter, pageInChapter);
                return new Page(fzPage, this, chapter: chapter, chapterPage: pageInChapter);
            }
        }

        /// <summary>Python <c>Document.__getitem__(i)</c> for an <c>int</c>: <c>if i not in self: raise IndexError</c> then <c>load_page(i)</c>.</summary>
        internal Page GetItemPageForIndexer(int pageNo)
        {
            lock (Utils.MuPDFLock)
            {
                int pc = PageCount;
                if (!(pageNo < pc))
                    throw new IndexOutOfRangeException($"page {pageNo} not in document");
                return LoadPageCore(pageNo);
            }
        }

        /// <summary>Python <c>Document.__getitem__((chapter, pno))</c> membership then <c>load_page</c>.</summary>
        internal Page GetItemPageForIndexer(int chapter, int pageInChapter)
        {
            lock (Utils.MuPDFLock)
            {
                _ = PageCount;
                if (!ContainsChapterPage(chapter, pageInChapter))
                    throw new IndexOutOfRangeException($"page ({chapter}, {pageInChapter}) not in document");
                var fzPage = mupdf.mupdf.fz_load_chapter_page(NativeDocument, chapter, pageInChapter);
                return new Page(fzPage, this, chapter: chapter, chapterPage: pageInChapter);
            }
        }

        /// <summary>
        /// Load a page (Python <c>doc[i]</c> / <c>__getitem__</c>).
        /// </summary>
        public Page this[int pageNo] => GetItemPageForIndexer(pageNo);
        /// <summary>
        /// number of pages in chapter
        /// </summary>
        /// <remarks>Return the number of pages of a chapter. PyMuPDF <c>Document.chapter_page_count</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int ChapterPageCount(int chapter) => mupdf.mupdf.fz_count_chapter_pages(NativeDocument, chapter);
        /// <summary>
        /// See PyMuPDF Document.contains_page.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.contains_page</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool ContainsPage(int pageNo) => pageNo >= 0 && pageNo < PageCount;
        /// <summary>
        /// See PyMuPDF Document.contains_chapter_page.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.contains_chapter_page</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="chapter">0-based chapter number (EPUB and similar formats).</param>
        /// <param name="pageInChapter">0-based page index within the chapter.</param>
        /// <returns><see langword="true"/> if the operation succeeded.</returns>
        public bool ContainsChapterPage(int chapter, int pageInChapter)
        {
            if (chapter < 0 || chapter >= ChapterCount) return false;
            if (pageInChapter < 0 || pageInChapter >= ChapterPageCount(chapter)) return false;
            return true;
        }
        /// <summary>
        /// Gets whether the (chapter, page) location exists in this document.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.contains_location</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool ContainsLocation((int chapter, int page) loc) => ContainsChapterPage(loc.chapter, loc.page);
        /// <summary>
        /// Iterates pages with optional start, stop, and step (like Python slice).
        /// </summary>
        /// <remarks>A generator for a range of pages. Parameters have the same meaning as in the built-in function *range()*. Intended for expressions of the form *"for page in doc.pages(start, stop, step): ..."*. PyMuPDF <c>Document.pages</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="start">start iteration with this page number. Default is zero, allowed values are <c>-∞ &lt; start &lt; page_count</c>. While this is negative, <see cref="PageCount"/> is added before starting the iteration.</param>
        /// <param name="stop">stop iteration at this page number. Default is <see cref="PageCount"/>, possible are <c>-∞ &lt; stop &lt;= page_count</c>. Larger values are silently replaced by the default. Negative values will cyclically emit the pages in reversed order. As with the built-in *range()*, this is the first page not returned.</param>
        /// <param name="step">stepping value. Defaults are 1 if start &lt; stop and -1 if start &gt; stop. Zero is not allowed.</param>
        /// <returns>a generator iterator over the document's pages. Some examples:</returns>
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
        /// See PyMuPDF Document.page_number_from_location.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.page_number_from_location</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="(int chapter, int page) loc">See PyMuPDF parameter &lt;c&gt;(int chapter, int page) loc&lt;/c&gt;.</param>
        public int PageNumberFromLocation((int chapter, int page) loc)
        {
            var fzLoc = new mupdf.fz_location();
            fzLoc.chapter = loc.chapter;
            fzLoc.page = loc.page;
            return mupdf.mupdf.fz_page_number_from_location(NativeDocument, new mupdf.FzLocation(fzLoc));
        }
        /// <summary>
        /// Creates a bookmark pointer for reflowable documents.
        /// </summary>
        /// <remarks>Return a page pointer in a reflowable document. After re-layouting the document, the result of this method can be used to find the new location of the page. PyMuPDF <c>Document.make_bookmark</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="(int chapter, int page) loc">See PyMuPDF parameter &lt;c&gt;(int chapter, int page) loc&lt;/c&gt;.</param>
        /// <returns>a long integer in pointer format. To be used for finding the new location of the page after re-layouting the document. Do not touch or re-assign.</returns>
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

        private Dictionary<string, string> _metadata;
        /// <summary>
        /// Gets Gets or sets the document metadata dictionary (PDF Info keys).
        /// </summary>
        /// <value>Gets or sets the document metadata dictionary (PDF Info keys).</value>
        /// <remarks>PyMuPDF <c>Document.metadata</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public Dictionary<string, string> Metadata
        {
            get => _metadata ?? GetMetadata();
            set => _metadata = value;
        }
        /// <summary>
        /// Returns a copy of the metadata dictionary.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.get_metadata</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns>A dictionary of entries.</returns>
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
                var infoRef = Helpers.PdfDictGet(trailer, mupdf.mupdf.pdf_new_name("Info"));
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
                var val = Helpers.PdfObjDictGet(infoDict,mupdf.mupdf.pdf_new_name(pdfName));
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
        /// Replaces document metadata from a dictionary.
        /// </summary>
        /// <remarks>PDF only: Sets or updates the metadata of the document as specified in *m*, a Python dictionary. PyMuPDF <c>Document.set_metadata</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="Dictionary<string">See PyMuPDF parameter &lt;c&gt;Dictionary&lt;string&lt;/c&gt;.</param>
        /// <param name="m">A dictionary with the same keys as *metadata* (see below). All keys are optional. A PDF's format and encryption method cannot be set or changed and will be ignored. If any value should not contain data, do not specify its key or set the value to <c>None</c>. If you use *{}* all metadata information will be cleared to the string *"none"*. If you want to selectively change only some values, modify a copy of *doc.metadata* and use it as the argument. Arbitrary unicode values are possible if specified as UTF-8-encoded.</param>
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

            var (infoType, infoTemp) = XrefGetKey(-1, "Info");
            int infoXref = 0;
            if (infoType == "xref")
                infoXref = int.Parse(infoTemp.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);

            if (m.Count == 0 && infoXref == 0)
                return;

            if (m.Count == 0)
            {
                // PyMuPDF: doc.XrefSetKey(-1, "Info", "null") — keep /Info with null value
                XrefSetKey(-1, "Info", "null");
                InitDoc();
                return;
            }

            var pdf = NativePdfDocument;
            var trailer = mupdf.mupdf.pdf_trailer(pdf);
            var infoKey = mupdf.mupdf.pdf_new_name("Info");
            var info = Helpers.PdfDictGet(trailer, infoKey);

            mupdf.PdfObj infoObj;
            if (infoXref == 0)
            {
                // PyMuPDF: info_xref = doc.get_new_xref(); doc.UpdateObject(info_xref, "<<>>");
                infoXref = GetNewXref();
                UpdateObject(infoXref, "<<>>");
                XrefSetKey(-1, "Info", $"{infoXref} 0 R");
                info = Helpers.PdfDictGet(trailer, infoKey);
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
        /// extract the table of contents
        /// </summary>
        /// <remarks>Creates a table of contents (TOC) out of the document's outline chain. PyMuPDF <c>Document.get_toc</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="simple">Indicates whether a simple or a detailed TOC is required. If <see langword="false"/>, each item of the list also contains a dictionary with link destination details for each outline entry.</param>
        /// <returns>a list of lists. Each entry has the form *[lvl, title, page, dest]*. Its entries have the following meanings:</returns>
        public TocResult GetToc(bool simple = true)
        {
            List<(int level, string title, int page, Dictionary<string, object> link)> Recurse(
                Outline olItem,
                List<(int level, string title, int page, Dictionary<string, object> link)> liste,
                int lvl)
            {
                // Recursively follow the outline item chain and record item information in a list.
                while (olItem != null && olItem.IsValid)
                {
                    string title;
                    if (!string.IsNullOrEmpty(olItem.Title))
                        title = olItem.Title;
                    else
                        title = " ";

                    int page;
                    if (!olItem.IsExternal)
                    {
                        if (!string.IsNullOrEmpty(olItem.Uri))
                        {
                            if (olItem.Page == -1)
                            {
                                var resolve = ResolveLink(olItem.Uri);
                                page = resolve.page + 1;
                            }
                            else
                                page = olItem.Page + 1;
                        }
                        else
                            page = -1;
                    }
                    else
                        page = -1;

                    if (!simple)
                    {
                        var link = Helpers.GetLinkDict(olItem, this);
                        liste.Add((lvl, title, page, link));
                    }
                    else
                        liste.Add((lvl, title, page, null));

                    if (olItem.Down != null)
                        liste = Recurse(olItem.Down, liste, lvl + 1);
                    olItem = olItem.Next;
                }
                return liste;
            }

            // ensure document is open
            if (IsClosed)
                throw new ValueErrorException("document closed");
            InitDoc();
            Outline olItem = _outline;
            if (olItem == null)
                return new TocResult(new List<Toc>());
            int lvl = 1;
            var liste = new List<(int level, string title, int page, Dictionary<string, object> link)>();
            var toc = Recurse(olItem, liste, lvl);
            if (IsPdf && !simple)
                _extend_toc_items(toc);
            return new TocResult(toc.Select(t => (Toc)t).ToList());
        }

        /// <summary>Add color info to all items of an extended TOC list.</summary>
        private void _extend_toc_items(List<(int level, string title, int page, Dictionary<string, object> link)> items)
        {
            if (IsClosed)
                throw new ValueErrorException("document closed");
            var pdf = NativePdfDocument;
            string zoom = "zoom";
            string bold = "bold";
            string italic = "italic";
            string collapse = "collapse";

            var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            if (root.m_internal == null)
                return;
            var olroot = Helpers.PdfDictGet(root, mupdf.mupdf.pdf_new_name("Outlines"));
            if (olroot.m_internal == null)
                return;
            var first = Helpers.PdfDictGet(olroot, mupdf.mupdf.pdf_new_name("First"));
            if (first.m_internal == null)
                return;
            var xrefs = new List<int>();
            xrefs = Helpers.JM_outline_xrefs(first, xrefs);
            int n = xrefs.Count;
            int m = items.Count;
            if (n == 0)
                return;
            if (n != m)
                throw new IndexOutOfRangeException("internal error finding outline xrefs");

            // update all TOC item dictionaries
            for (int i = 0; i < n; i++)
            {
                int xref = xrefs[i];
                var item = items[i];
                var itemdict = item.link;
                if (itemdict == null)
                    throw new ValueErrorException("need non-simple TOC format");
                itemdict["xref"] = xrefs[i];
                var bm = mupdf.mupdf.pdf_load_object(pdf, xref);
                int flags = mupdf.mupdf.pdf_to_int(Helpers.PdfDictGet(bm, mupdf.mupdf.pdf_new_name("F")));
                if (flags == 1)
                    itemdict[italic] = true;
                else if (flags == 2)
                    itemdict[bold] = true;
                else if (flags == 3)
                {
                    itemdict[italic] = true;
                    itemdict[bold] = true;
                }
                int count = mupdf.mupdf.pdf_to_int(Helpers.PdfDictGet(bm, mupdf.mupdf.pdf_new_name("Count")));
                if (count < 0)
                    itemdict[collapse] = true;
                else if (count > 0)
                    itemdict[collapse] = false;
                var col = Helpers.PdfDictGet(bm, mupdf.mupdf.pdf_new_name("C"));
                if (mupdf.mupdf.pdf_is_array(col) != 0 && mupdf.mupdf.pdf_array_len(col) == 3)
                {
                    var color = (
                        mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(col, 0)),
                        mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(col, 1)),
                        mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(col, 2)));
                    itemdict["color"] = color;
                }
                float z = 0;
                var obj = Helpers.PdfDictGet(bm, mupdf.mupdf.pdf_new_name("Dest"));
                if (obj.m_internal == null || mupdf.mupdf.pdf_is_array(obj) == 0)
                    obj = Helpers.PdfDictGet(
                        Helpers.PdfDictGet(bm, mupdf.mupdf.pdf_new_name("A")),
                        mupdf.mupdf.pdf_new_name("D"));
                if (obj.m_internal != null && mupdf.mupdf.pdf_is_array(obj) != 0
                    && mupdf.mupdf.pdf_array_len(obj) == 5)
                {
                    z = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(obj, 4));
                }
                itemdict[zoom] = (float)z;
                items[i] = (item.level, item.title, item.page, itemdict);
            }
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

            var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            var olroot = Helpers.PdfDictGet(root, mupdf.mupdf.pdf_new_name("Outlines"));
            if (olroot.m_internal == null)
                return xrefs;

            var first = Helpers.PdfDictGet(olroot, mupdf.mupdf.pdf_new_name("First"));
            void Collect(mupdf.PdfObj item)
            {
                if (item == null || item.m_internal == null)
                    return;
                xrefs.Add(mupdf.mupdf.pdf_to_num(item));
                var down = Helpers.PdfDictGet(item, mupdf.mupdf.pdf_new_name("First"));
                if (down.m_internal != null)
                    Collect(down);
                var next = Helpers.PdfDictGet(item, mupdf.mupdf.pdf_new_name("Next"));
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
            var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            var olroot = Helpers.PdfDictGet(root, mupdf.mupdf.pdf_new_name("Outlines"));
            if (olroot.m_internal == null)
            {
                olroot = mupdf.mupdf.pdf_new_dict(pdf, 4);
                mupdf.mupdf.pdf_dict_put(olroot, mupdf.mupdf.pdf_new_name("Type"), mupdf.mupdf.pdf_new_name("Outlines"));
                var indObj = mupdf.mupdf.pdf_add_object(pdf, olroot);
                mupdf.mupdf.pdf_dict_put(root, mupdf.mupdf.pdf_new_name("Outlines"), indObj);
                olroot = Helpers.PdfDictGet(root, mupdf.mupdf.pdf_new_name("Outlines"));
            }
            return mupdf.mupdf.pdf_to_num(olroot);
        }

        private static string FormatNum(float v) => v.ToString("g", System.Globalization.CultureInfo.InvariantCulture);

        private static string BuildDestAction(int xref, Dictionary<string, object> ddict)
        {
            if (ddict == null)
                return "";
            int kind = ddict.ContainsKey("kind") && ddict["kind"] is int ? (int)ddict["kind"] : Constants.LinkNone;
            if (kind == Constants.LinkNone)
                return "";

            if (kind == Constants.LinkGoto)
            {
                float zoom = ddict.ContainsKey("zoom") ? (float)Convert.ToDouble(ddict["zoom"], System.Globalization.CultureInfo.InvariantCulture) : 0.0f;
                Point to = ddict.ContainsKey("to") && ddict["to"] is Point ? new Point((Point)ddict["to"]) : new Point(0, 0);
                return "/A<</S/GoTo/D[" + xref.ToString(System.Globalization.CultureInfo.InvariantCulture) + " 0 R/XYZ "
                    + FormatNum(to.X) + " " + FormatNum(to.Y) + " " + FormatNum(zoom) + "]>>";
            }
            if (kind == Constants.LinkUri)
            {
                string uri = ddict.ContainsKey("uri") ? ddict["uri"]?.ToString() ?? "" : "";
                return "/A<</S/URI/URI" + Helpers.GetPdfStr(uri) + ">>";
            }
            if (kind == Constants.LinkLaunch)
            {
                string file = ddict.ContainsKey("file") ? ddict["file"]?.ToString() ?? "" : "";
                string fspec = Helpers.GetPdfStr(file);
                return "/A<</S/Launch/F<</F" + fspec + "/UF" + fspec + "/Type/Filespec>>>>";
            }
            if (kind == Constants.LinkGotor)
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
                float z = ddict.ContainsKey("zoom") ? (float)Convert.ToDouble(ddict["zoom"], System.Globalization.CultureInfo.InvariantCulture) : 0.0f;
                return "/A<</S/GoToR/D[" + page.ToString(System.Globalization.CultureInfo.InvariantCulture) + " /XYZ "
                    + FormatNum(p.X) + " " + FormatNum(p.Y) + " " + FormatNum(z) + "]/F<</F" + fspec + "/UF" + fspec + "/Type/Filespec>>>>";
            }
            if (kind == Constants.LinkNamed)
            {
                string lname = null;
                if (ddict.ContainsKey("name") && ddict["name"] != null)
                    lname = ddict["name"].ToString();
                if (string.IsNullOrEmpty(lname) && ddict.ContainsKey("nameddest") && ddict["nameddest"] != null)
                    lname = ddict["nameddest"].ToString();
                if (string.IsNullOrEmpty(lname))
                    return "";
                return "/A<</S/GoTo/D" + Helpers.GetPdfStr(lname) + "/Type/Action>>";
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
        /// PDF only: set the table of contents (TOC)
        /// </summary>
        /// <remarks>PDF only: Replaces the complete current outline tree (table of contents) with the one provided as the argument. After successful execution, the new outline tree can be accessed as usual via <see cref="GetToc"/> or via <see cref="GetOutline"/>. Like with other output-oriented methods, changes become permanent only via <see cref="Save"/> (incremental save supported). Internally, this method consists of the following two steps. For a demonstration see example below. PyMuPDF <c>Document.set_toc</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="toc">See PyMuPDF parameter &lt;c&gt;toc&lt;/c&gt;.</param>
        /// <param name="collapse">*(new in v1.16.9)* controls the hierarchy level beyond which outline entries should initially show up collapsed. The default 1 will hence only display level 1, higher levels must be unfolded using the PDF viewer. To unfold everything, specify either a large integer, 0 or None.</param>
        /// <returns>the number of inserted, resp. deleted items.</returns>
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
                float pageHeight = PageCropBox(pno).Height;
                Point top = new Point(72, pageHeight - 36);
                var dest = new Dictionary<string, object> { ["to"] = top, ["kind"] = Constants.LinkGoto };
                if (Convert.ToInt32(o[2], System.Globalization.CultureInfo.InvariantCulture) < 0)
                    dest["kind"] = Constants.LinkNone;

                if (o.Count > 3)
                {
                    object o3 = o[3];
                    if (o3 is int || o3 is float || o3 is float || o3 is decimal)
                    {
                        float t = (float)Convert.ToDouble(o3, System.Globalization.CultureInfo.InvariantCulture);
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
                var countObj = Helpers.PdfDictGet(item, mupdf.mupdf.pdf_new_name("Count"));
                if (countObj.m_internal != null)
                {
                    int i = mupdf.mupdf.pdf_dict_get_int(item, mupdf.mupdf.pdf_new_name("Count"));
                    if ((i < 0 && collapse.Value == false) || (i > 0 && collapse.Value))
                        mupdf.mupdf.pdf_dict_put_int(item, mupdf.mupdf.pdf_new_name("Count"), -i);
                }
            }
        }
        /// <summary>
        /// PDF only: remove a single TOC item
        /// </summary>
        /// <remarks>PDF only: Remove this TOC item. This is a high-speed method, which disables the respective item, but leaves the overall TOC structure intact. Physically, the item still exists in the TOC tree, but is shown grayed-out and will no longer point to any destination. PyMuPDF <c>Document.del_toc_item</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="idx">the index of the item in list <see cref="GetToc"/>.</param>
        public void DeleteTocItem(int idx)
        {
            int xref = GetOutlineXrefs()[idx];
            RemoveTocItemByXref(xref);
        }
        /// <summary>
        /// PDF only: change a single TOC item
        /// </summary>
        /// <remarks>PDF only: Changes the TOC item identified by its index. Change the item title, destination, appearance (color, bold, italic) or collapsing sub-items -- or to remove the item altogether. PyMuPDF <c>Document.set_toc_item</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="idx">the index of the entry in the list created by <see cref="GetToc"/>.</param>
        /// <param name="Dictionary<string">See PyMuPDF parameter &lt;c&gt;Dictionary&lt;string&lt;/c&gt;.</param>
        /// <param name="destDict">See PyMuPDF parameter &lt;c&gt;destDict&lt;/c&gt;.</param>
        /// <param name="kind">the link kind, see linkDest Kinds. If LINK_NONE, then all remaining parameter will be ignored, and the TOC item will be removed -- same as <see cref="DeleteTocItem"/>. If None, then only the title is modified and the remaining parameters are ignored. All other values will lead to making a new destination dictionary using the subsequent arguments.</param>
        /// <param name="pno">the 1-based page number, i.e. a value 1 &lt;= pno &lt;= doc.page_count. Required for LINK_GOTO.</param>
        /// <param name="uri">the URL text. Required for LINK_URI.</param>
        /// <param name="title">the desired new title. None if no change.</param>
        /// <param name="to">(optional) points to a coordinate on the target page. Relevant for LINK_GOTO. If omitted, a point near the page's top is chosen.</param>
        /// <param name="filename">required for LINK_GOTOR and LINK_LAUNCH.</param>
        /// <param name="zoom">use this zoom factor when showing the target page.</param>
        public void SetTocItem(int idx, Dictionary<string, object> destDict = null, int? kind = null, int? pno = null,
            string uri = null, string title = null, Point to = null, string filename = null, float zoom = 0)
        {
            int xref = GetOutlineXrefs()[idx];
            int pageXref = 0;

            if (destDict != null)
            {
                if (destDict.ContainsKey("kind") && Convert.ToInt32(destDict["kind"], System.Globalization.CultureInfo.InvariantCulture) == Constants.LinkGoto)
                {
                    int dpno = Convert.ToInt32(destDict["page"], System.Globalization.CultureInfo.InvariantCulture);
                    pageXref = PageXref(dpno);
                    float pageHeight = PageCropBox(dpno).Height;
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
                    else if (destDict["color"] is float[] dc)
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
            if (kind.HasValue && kind.Value == Constants.LinkNone)
            {
                DeleteTocItem(idx);
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
            if (k == Constants.LinkGoto)
            {
                if (!pno.HasValue || pno.Value < 1 || pno.Value > PageCount)
                    throw new ValueErrorException("bad page number");
                pageXref = PageXref(pno.Value - 1);
                float pageHeight = PageCropBox(pno.Value - 1).Height;
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
        /// <summary>
        /// first `Outline` item
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.outline</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public Outline GetOutline() => _outline;

        internal Outline _loadOutline()
        {
            try
            {
                var ol = mupdf.mupdf.fz_load_outline(NativeDocument);
                if (ol.m_internal == null)
                    return null;
                return new Outline(ol);
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// PDF only: xref a TOC item
        /// </summary>
        /// <remarks>PDF only: Return the xref of the outline item. This is mainly used for internal purposes. PyMuPDF <c>Document.outline_xref</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns>xref.</returns>
        public List<int> GetOutlineXrefs()
        {
            var xrefs = new List<int>();
            var pdf = Helpers.AsPdfDocument(NativeDocument, required: false);
            if (pdf == null || pdf.m_internal == null)
                return xrefs;
            var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            if (root.m_internal == null)
                return xrefs;
            var olroot = Helpers.PdfDictGet(root, mupdf.mupdf.pdf_new_name("Outlines"));
            if (olroot.m_internal == null)
                return xrefs;
            var first = Helpers.PdfDictGet(olroot, mupdf.mupdf.pdf_new_name("First"));
            if (first.m_internal == null)
                return xrefs;

            void Walk(mupdf.PdfObj item)
            {
                if (item == null || item.m_internal == null)
                    return;
                xrefs.Add(mupdf.mupdf.pdf_to_num(item));
                var down = Helpers.PdfDictGet(item, mupdf.mupdf.pdf_new_name("First"));
                if (down.m_internal != null)
                    Walk(down);
                var next = Helpers.PdfDictGet(item, mupdf.mupdf.pdf_new_name("Next"));
                if (next.m_internal != null)
                    Walk(next);
            }

            Walk(first);
            return xrefs;
        }

        // ─── Page Operations ────────────────────────────────────────────
        /// <summary>
        /// PDF only: insert a new empty page
        /// </summary>
        /// <remarks>PDF only: Insert an empty page. PyMuPDF <c>Document.new_page</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">page number index (zero-indexed) at which to insert page. Special values -1 and *<see cref="PageCount"/>* insert after the last page.</param>
        /// <param name="width">page width.</param>
        /// <param name="height">page height.</param>
        /// <returns>the created page object. Be aware that the page numbers of pages after the inserted one will have changed after method execution. For the same reason, all existing page objects will be invalidated. Using them will lead to exceptions.</returns>
        public Page NewPage(int pno = -1, float width = 595, float height = 842)
        {
            _newPage(pno, width, height);
            return LoadPage(pno);
        }

        /// <summary>PyMuPDF <c>Document._newPage</c>.</summary>
        internal Page _newPage(int pno = -1, float width = 595, float height = 842)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");

            var pdf = Helpers.AsPdfDocument(this, required: true);
            var mediabox = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_UNIT);
            mediabox.x1 = width;
            mediabox.y1 = height;
            var contents = new mupdf.FzBuffer();
            if (pno < -1)
                throw new ValueErrorException(Constants.MSG_BAD_PAGENO);

            var resources = pdf.pdf_add_new_dict(1);
            var pageObj = pdf.pdf_add_page(mediabox, 0, resources, contents);
            mupdf.mupdf.pdf_insert_page(pdf, pno, pageObj);

            // PyMuPDF Document._newPage() -> _reset_page_refs() invalidates all cached Page wrappers.
            ResetPageRefsInternal(erasePages: true);
            return LoadPage(pno);
        }

        /// <summary>PyMuPDF <c>Document._delete_page</c>.</summary>
        private void _delete_page(int pno)
        {
            // pdf = _as_pdf_document(self)
            var pdf = Helpers.AsPdfDocument(this, required: true);
            // mupdf.pdf_delete_page( pdf, pno)
            mupdf.mupdf.pdf_delete_page(pdf, pno);
            // if pdf.m_internal.rev_page_map:
            if (pdf.m_internal.rev_page_map != null)
                // mupdf.ll_pdf_drop_page_tree( pdf.m_internal)
                mupdf.mupdf.ll_pdf_drop_page_tree(pdf.m_internal);
        }
        /// <summary>
        /// Deletes a single page.
        /// </summary>
        /// <remarks>PDF only: Delete a page given by its 0-based number in `-∞ &lt; pno &lt; page_count`. PyMuPDF <c>Document.delete_page</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">the page to be deleted. Negative number count backwards from the end of the document (like with indices). Default is the last page.</param>
        public void DeletePage(int pno = -1)
        {
            // """ Delete one page from a PDF.
            // """
            // return self.delete_pages(pno)
            delete_pages(pno);
        }

        /// <summary>
        /// Delete pages from a PDF (Python <c>Document.delete_pages</c>).
        /// </summary>
        private void DeletePagesCore(IEnumerable<int> pages)
        {
            EnsurePdfOpenForDeletePages();
            int pageCount = PageCount;
            var numbers = new List<int>();
            foreach (int p in pages)
            {
                int pno = p;
                while (pno < 0)
                    pno += pageCount;
                numbers.Add(pno);
            }
            numbers = numbers.Distinct().OrderBy(x => x).ToList();
            if (numbers.Count == 0)
                return;
            if (numbers[0] < 0 || numbers[numbers.Count - 1] >= pageCount)
                throw new ValueErrorException(Constants.MSG_BAD_PAGENO);

            var frozen = new HashSet<int>(numbers);
            var toc = GetToc();
            var outlineXrefs = GetOutlineXrefs();
            int n = Math.Min(toc.Count, outlineXrefs.Count);
            for (int i = 0; i < n; i++)
            {
                if (frozen.Contains(toc[i].page - 1))
                    RemoveTocItemByXref(outlineXrefs[i]);
            }

            _remove_links_to(frozen);

            _suppressPageRefReset++;
            try
            {
                foreach (int p in numbers.OrderByDescending(x => x))
                    _delete_page(p);
            }
            finally
            {
                _suppressPageRefReset--;
            }
            ResetPageRefsInternal();
        }
        /// <summary>
        /// Deletes one or more pages by number.
        /// </summary>
        /// <remarks>PDF only: Delete multiple pages given as 0-based numbers. PyMuPDF <c>Document.delete_pages</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void DeletePages(params int[] pages) => DeletePagesCore(pages);
        /// <summary>
        /// Deletes one or more pages by number.
        /// </summary>
        /// <remarks>PDF only: Delete multiple pages given as 0-based numbers. PyMuPDF <c>Document.delete_pages</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="fromPage">First source page number (0-based, inclusive).</param>
        /// <param name="toPage">Last source page number (0-based, inclusive).</param>
        public void DeletePages(int fromPage, int toPage)
        {
            if (fromPage > toPage)
            {
                int t = fromPage;
                fromPage = toPage;
                toPage = t;
            }
            DeletePagesCore(Enumerable.Range(fromPage, toPage - fromPage + 1));
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
        /// PDF only: delete multiple pages
        /// </summary>
        /// <remarks>PDF only: Delete multiple pages given as 0-based numbers. PyMuPDF <c>Document.delete_pages</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="start">See PyMuPDF parameter &lt;c&gt;start&lt;/c&gt;.</param>
        /// <param name="stop">See PyMuPDF parameter &lt;c&gt;stop&lt;/c&gt;.</param>
        /// <param name="step">See PyMuPDF parameter &lt;c&gt;step&lt;/c&gt;.</param>
        public void DeletePagesBySlice(int start, int stop, int step = 1)
        {
            EnsurePdfOpenForDeletePages();
            var indices = GetSlicePageIndices(start, stop, step, PageCount);
            if (indices.Count == 0) return;
            DeletePages(indices.ToArray());
        }
        /// <summary>
        /// iterator over a page range
        /// </summary>
        /// <remarks>A generator for a range of pages. Parameters have the same meaning as in the built-in function *range()*. Intended for expressions of the form *"for page in doc.pages(start, stop, step): ..."*. PyMuPDF <c>Document.pages</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="start">start iteration with this page number. Default is zero, allowed values are <c>-∞ &lt; start &lt; page_count</c>. While this is negative, <see cref="PageCount"/> is added before starting the iteration.</param>
        /// <param name="stop">stop iteration at this page number. Default is <see cref="PageCount"/>, possible are <c>-∞ &lt; stop &lt;= page_count</c>. Larger values are silently replaced by the default. Negative values will cyclically emit the pages in reversed order. As with the built-in *range()*, this is the first page not returned.</param>
        /// <param name="step">stepping value. Defaults are 1 if start &lt; stop and -1 if start &gt; stop. Zero is not allowed.</param>
        /// <returns>a generator iterator over the document's pages. Some examples:</returns>
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
        /// PDF only: insert a new page
        /// </summary>
        /// <remarks>PDF only: Insert a new page and insert some text. Convenience function which combines <see cref="NewPage"/> and (parts of) <see cref="Page.InsertText"/>. PyMuPDF <c>Document.insert_page</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">page number index (zero-indexed) at which to insert page. Special values -1 and <c><see cref="PageCount"/></c> insert after the last page.</param>
        /// <param name="text">Object definition or page text source.</param>
        /// <param name="fontsize">Default font size for reflowable layout.</param>
        /// <param name="width">Page width for reflowable layout (used with height if rect is omitted).</param>
        /// <param name="height">Page height for reflowable layout (used with width if rect is omitted).</param>
        /// <param name="fontname">See PyMuPDF parameter &lt;c&gt;fontname&lt;/c&gt;.</param>
        /// <param name="color">Whether color images may be processed.</param>
        /// <returns>the result of <see cref="Page.InsertText"/> (number of successfully inserted lines).</returns>
        public Page InsertPage(int pno = -1, string text = null, float fontsize = 11, float width = 595, float height = 842, string fontname = "helv", float[] color = null)
        {
            var page = NewPage(pno, width, height);
            if (!string.IsNullOrEmpty(text))
            {
                page.InsertText(new Point(72, 72), text, fontSize: fontsize, fontName: fontname, color: color);
            }
            return page;
        }

        /// <summary>PyMuPDF <c>Document._move_copy_page</c>.</summary>
        private void _move_copy_page(int pno, int nb, int before, int copy)
        {
            // """Move or copy a PDF page reference."""
            // pdf = _as_pdf_document(self)
            var pdf = Helpers.AsPdfDocument(this, required: true);
            // same = 0
            int same = 0;
            // get the two page objects -----------------------------------
            // locate the /Kids arrays and indices in each
            //
            // page1, parent1, i1 = pdf_lookup_page_loc( pdf, pno)
            var (page1, parent1, i1) = pdf.pdf_lookup_page_loc(pno);
            //
            // kids1 = Helpers.PdfObjDictGet(mupdf, parent1, PDF_NAME('Kids'))
            var kids1 = Helpers.PdfDictGet(parent1, mupdf.mupdf.pdf_new_name("Kids"));
            //
            // page2, parent2, i2 = pdf_lookup_page_loc( pdf, nb)
            var (page2, parent2, i2) = pdf.pdf_lookup_page_loc(nb);
            // kids2 = Helpers.PdfObjDictGet(mupdf, parent2, PDF_NAME('Kids'))
            var kids2 = Helpers.PdfDictGet(parent2, mupdf.mupdf.pdf_new_name("Kids"));
            int pos;
            // if before:  # calc index of source page in target /Kids
            if (before != 0)
                // pos = i2
                pos = i2;
            else
                // pos = i2 + 1
                pos = i2 + 1;
            //
            // same /Kids array? ------------------------------------------
            // same = mupdf.pdf_objcmp( kids1, kids2)
            same = mupdf.mupdf.pdf_objcmp(kids1, kids2);
            //
            // put source page in target /Kids array ----------------------
            // if not copy and same != 0:  # update parent in page object
            if (copy == 0 && same != 0)
                // mupdf.pdf_dict_put( page1, PDF_NAME('Parent'), parent2)
                mupdf.mupdf.pdf_dict_put(page1, mupdf.mupdf.pdf_new_name("Parent"), parent2);
            // mupdf.pdf_array_insert( kids2, page1, pos)
            mupdf.mupdf.pdf_array_insert(kids2, page1, pos);
            //
            // if same != 0:   # different /Kids arrays ----------------------
            if (same != 0)
            {
                // parent = parent2
                var parent = parent2;
                // while parent.m_internal:    # increase /Count objects in parents
                while (parent.m_internal != null)
                {
                    // count = mupdf.pdf_dict_get_int( parent, PDF_NAME('Count'))
                    int count = mupdf.mupdf.pdf_dict_get_int(parent, mupdf.mupdf.pdf_new_name("Count"));
                    // mupdf.pdf_dict_put_int( parent, PDF_NAME('Count'), count + 1)
                    mupdf.mupdf.pdf_dict_put_int(parent, mupdf.mupdf.pdf_new_name("Count"), count + 1);
                    // parent = Helpers.PdfObjDictGet(mupdf, parent, PDF_NAME('Parent'))
                    parent = Helpers.PdfDictGet(parent, mupdf.mupdf.pdf_new_name("Parent"));
                }
                // if not copy:    # delete original item
                if (copy == 0)
                {
                    // mupdf.pdf_array_delete( kids1, i1)
                    mupdf.mupdf.pdf_array_delete(kids1, i1);
                    // parent = parent1
                    parent = parent1;
                    // while parent.m_internal:    # decrease /Count objects in parents
                    while (parent.m_internal != null)
                    {
                        // count = mupdf.pdf_dict_get_int( parent, PDF_NAME('Count'))
                        int count = mupdf.mupdf.pdf_dict_get_int(parent, mupdf.mupdf.pdf_new_name("Count"));
                        // mupdf.pdf_dict_put_int( parent, PDF_NAME('Count'), count - 1)
                        mupdf.mupdf.pdf_dict_put_int(parent, mupdf.mupdf.pdf_new_name("Count"), count - 1);
                        // parent = Helpers.PdfObjDictGet(mupdf, parent, PDF_NAME('Parent'))
                        parent = Helpers.PdfDictGet(parent, mupdf.mupdf.pdf_new_name("Parent"));
                    }
                }
            }
            else
            {
                // else:   # same /Kids array
                // if copy:    # source page is copied
                if (copy != 0)
                {
                    // parent = parent2
                    var parent = parent2;
                    // while parent.m_internal:    # increase /Count object in parents
                    while (parent.m_internal != null)
                    {
                        // count = mupdf.pdf_dict_get_int( parent, PDF_NAME('Count'))
                        int count = mupdf.mupdf.pdf_dict_get_int(parent, mupdf.mupdf.pdf_new_name("Count"));
                        // mupdf.pdf_dict_put_int( parent, PDF_NAME('Count'), count + 1)
                        mupdf.mupdf.pdf_dict_put_int(parent, mupdf.mupdf.pdf_new_name("Count"), count + 1);
                        // parent = Helpers.PdfDictGet( parent, PDF_NAME('Parent'))
                        parent = Helpers.PdfDictGet(parent, mupdf.mupdf.pdf_new_name("Parent"));
                    }
                }
                else
                {
                    // if i1 < pos:
                    if (i1 < pos)
                        // mupdf.pdf_array_delete( kids1, i1)
                        mupdf.mupdf.pdf_array_delete(kids1, i1);
                    else
                        // mupdf.pdf_array_delete( kids1, i1 + 1)
                        mupdf.mupdf.pdf_array_delete(kids1, i1 + 1);
                }
            }
            // if pdf.m_internal.rev_page_map: # page map no longer valid: drop it
            if (pdf.m_internal.rev_page_map != null)
                // mupdf.ll_pdf_drop_page_tree( pdf.m_internal)
                mupdf.mupdf.ll_pdf_drop_page_tree(pdf.m_internal);
            //
            // self._reset_page_refs()
            ResetPageRefsInternal();
        }
        /// <summary>
        /// Copies a page reference within the same PDF.
        /// </summary>
        /// <remarks>PDF only: Copy a page reference within the document. PyMuPDF <c>Document.copy_page</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">the page to be copied. Must be in range `0 &lt;= pno &lt; page_count`.</param>
        /// <param name="to">the page number in front of which to copy. The default inserts after the last page.</param>
        public void CopyPage(int pno, int to = -1)
        {
            // """Copy a page within a PDF document.
            //
            // This will only create another reference of the same page object.
            // Args:
            //     pno: source page number
            //     to: put before this page, '-1' means after last page.
            // """
            // if self.is_closed:
            if (IsClosed)
                // raise ValueError("document closed")
                throw new ValueErrorException("document closed");
            //
            // page_count = len(self)
            int page_count = PageCount;
            // if (
            //         pno not in range(page_count)
            //         or to not in range(-1, page_count)
            //         ):
            if (pno < 0 || pno >= page_count || to < -1 || to >= page_count)
                // raise ValueError("bad page number(s)")
                throw new ValueErrorException("bad page number(s)");
            // before = 1
            int before = 1;
            // copy = 1
            int copy = 1;
            // if to == -1:
            if (to == -1)
            {
                // to = page_count - 1
                to = page_count - 1;
                // before = 0
                before = 0;
            }
            //
            // return self._move_copy_page(pno, to, before, copy)
            _move_copy_page(pno, to, before, copy);
        }
        /// <summary>
        /// PDF only: duplicate a page
        /// </summary>
        /// <remarks>PDF only: Make a full copy (duplicate) of a page. PyMuPDF <c>Document.fullcopy_page</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">the page to be duplicated. Must be in range `0 &lt;= pno &lt; page_count`.</param>
        /// <param name="to">the page number in front of which to copy. The default inserts after the last page.</param>
        public void FullCopyPage(int pno, int to = -1)
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

                var oldAnnots = Helpers.PdfDictGet(page2, mupdf.mupdf.pdf_new_name("Annots"));
                if (oldAnnots.m_internal != null)
                {
                    int n = mupdf.mupdf.pdf_array_len(oldAnnots);
                    var newAnnots = mupdf.mupdf.pdf_new_array(pdf, n);
                    for (int i = 0; i < n; i++)
                    {
                        var o = mupdf.mupdf.pdf_array_get(oldAnnots, i);
                        var subtype = Helpers.PdfDictGet(o, mupdf.mupdf.pdf_new_name("Subtype"));
                        if (mupdf.mupdf.pdf_name_eq(subtype, mupdf.mupdf.pdf_new_name("Popup")) != 0)
                            continue;
                        if (Helpers.PdfDictGets(o, "IRT").m_internal != null)
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

                var contentsObj = Helpers.PdfDictGet(page1, mupdf.mupdf.pdf_new_name("Contents"));
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
        /// Moves a page to another position in the document.
        /// </summary>
        /// <remarks>PDF only: Move (copy and then delete original) a page within the document. PyMuPDF <c>Document.move_page</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">the page to be moved. Must be in range `0 &lt;= pno &lt; page_count`.</param>
        /// <param name="to">the page number in front of which to insert the moved page. The default moves after the last page.</param>
        public void MovePage(int pno, int to = -1)
        {
            // """Move a page within a PDF document.
            //
            // Args:
            //     pno: source page number.
            //     to: put before this page, '-1' means after last page.
            // """
            // if self.is_closed:
            if (IsClosed)
                // raise ValueError("document closed")
                throw new ValueErrorException("document closed");
            // page_count = len(self)
            int page_count = PageCount;
            // if (pno not in range(page_count) or to not in range(-1, page_count)):
            if (pno < 0 || pno >= page_count || to < -1 || to >= page_count)
                // raise ValueError("bad page number(s)")
                throw new ValueErrorException("bad page number(s)");
            // before = 1
            int before = 1;
            // copy = 0
            int copy = 0;
            // if to == -1:
            if (to == -1)
            {
                // to = page_count - 1
                to = page_count - 1;
                // before = 0
                before = 0;
            }
            //
            // return self._move_copy_page(pno, to, before, copy)
            _move_copy_page(pno, to, before, copy);
        }
        /// <summary>
        /// Replaces the PDF with only the selected pages.
        /// </summary>
        /// <remarks>PDF only: Keeps only those pages of the document whose numbers occur in the list. Empty sequences or elements outside <c>range(<see cref="PageCount"/>)</c> will cause a *ValueError*. For more details see remarks at the bottom or this chapter. PyMuPDF <c>Document.select</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pages">Page numbers to select or delete.</param>
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
        /// PDF only: provide a new copy of a page
        /// </summary>
        /// <remarks>PDF only: Provide a new copy of a page after finishing and updating all pending changes. PyMuPDF <c>Document.reload_page</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="page">Page object for context-sensitive updates.</param>
        /// <returns>a new copy of the same page. All pending updates (e.g. to annotations or widgets) will be finalized and a fresh copy of the page will be loaded.</returns>
        public Page ReloadPage(Page page)
        {
            // old_annots = {}  # copy annot references to here
            var old_annots = new Dictionary<int, Annot>();
            // pno = page.number  # save the page number
            int pno = page.Number;
            int chapter = page.ReloadChapter;
            int chapterPage = page.ReloadChapterPage;
            // for k, v in page._annot_refs.items():  # save the annot dictionary
            foreach (var kvp in page.SnapshotAnnotRefsForReload())
                old_annots[kvp.Key] = kvp.Value;

            // When we call `self.load_page()` below, it will end up in
            // fz_load_chapter_page(), which will return any matching page in the
            // document's list of non-ref-counted loaded pages, instead of actually
            // reloading the page.
            //
            // We want to assert that we have actually reloaded the fz_page, and not
            // simply returned the same `fz_page*` pointer from the document's list
            // of non-ref-counted loaded pages.
            //
            // So we first remove our reference to the `fz_page*`. This will
            // decrement .refs, and if .refs was 1, this is guaranteed to free the
            // `fz_page*` and remove it from the document's list if it was there. So
            // we are guaranteed that our returned `fz_page*` is from a genuine
            // reload, even if it happens to reuse the original block of memory.
            //
            // However if the original .refs is greater than one, there must be
            // other references to the `fz_page` somewhere, and we require that
            // these other references are not keeping the page in the document's
            // list.  We check that we are returning a newly loaded page by
            // asserting that our returned `fz_page*` is different from the original
            // `fz_page*` - the original was not freed, so a new `fz_page` cannot
            // reuse the same block of memory.
            //

            // refs_old = page.this.m_internal.refs
            var fz_page_old = page.NativePage;
            int refs_old = fz_page_old.m_internal.refs;
            // m_internal_old = page.this.m_internal_value()
            long m_internal_old = fz_page_old.m_internal_value();

            // page.this = None
            page.ReleaseNativeForReload();
            // page._erase()  # remove the page
            page.EraseForReload();
            // page = None

            // TOOLS.store_shrink(100)
            Tools.StoreShrink(100);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // page = self.load_page(pno)  # reload the page
            page = chapter >= 0 ? LoadPage(chapter, chapterPage) : LoadPage(pno);

            // copy annot refs over to the new dictionary
            // page_proxy = weakref.proxy(page)
            page.RestoreAnnotRefsFromReload(old_annots);
            if (refs_old == 1)
            {
                // We know that `page.this = None` will have decremented the ref
                // count to zero so we are guaranteed that the new `fz_page` is a
                // new page even if it happens to have reused the same block of
                // memory.
            }
            else if (IsPdf)
            {
                // Evict MuPDF's non-refcounted page cache when other wrappers may still
                // hold the old fz_page (common in .NET after Story/ShowPdfPage work).
                long m_internal_new = page.NativePage.m_internal_value();
                if (m_internal_new == m_internal_old)
                {
                    mupdf.mupdf.ll_pdf_drop_page_tree_internal(NativePdfDocument.m_internal);
                    page = chapter >= 0 ? LoadPage(chapter, chapterPage) : LoadPage(pno);
                    page.RestoreAnnotRefsFromReload(old_annots);
                }
            }
            return page;
        }

        // ─── Save / Write ───────────────────────────────────────────────

        /// <summary>
        /// Save document to file.
        /// </summary>
        internal void Save3(string filename, bool garbage = false, bool clean = false, bool deflate = false,
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

        internal void Save1(
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

            var pdf = NativePdfDocument;
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
        }

        /*
        // Legacy bool Save(Stream) — use Save(Stream, int garbage, ...) or Write() instead.
        */
        /// <summary>
        /// PDF only: save the document
        /// </summary>
        /// <remarks>PDF only: Saves the document in its current state. PyMuPDF <c>Document.save</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="filename">File path, or a Stream to receive output bytes.</param>
        /// <param name="garbage">Garbage collection level: 0 none; 1 unused objects; 2 compact xref; 3 merge duplicates; 4 stream dedup.</param>
        /// <param name="clean">Clean and sanitize content streams (mutool clean -sc).</param>
        /// <param name="deflate">Deflate (compress) uncompressed streams.</param>
        /// <param name="deflate_images">Deflate uncompressed image streams.</param>
        /// <param name="deflate_fonts">Deflate uncompressed fontfile streams.</param>
        /// <param name="incremental">Incremental save to the original file only; excludes garbage and linear.</param>
        /// <param name="ascii">Convert binary stream data to ASCII.</param>
        /// <param name="expand">Decompress objects: 0 none, 1 images, 2 fonts, 255 all.</param>
        /// <param name="linear">Write linearized PDF; excludes incremental and object streams.</param>
        /// <param name="noNewId">If true, do not update the document /ID.</param>
        /// <param name="appearance">Regenerate widget appearance streams.</param>
        /// <param name="pretty">Prettify PDF object syntax.</param>
        /// <param name="encryption">Encryption method when saving.</param>
        /// <param name="permissions">Permission flags for encrypted output.</param>
        /// <param name="owner_pw">Owner password (max 40 characters).</param>
        /// <param name="user_pw">User password (max 40 characters).</param>
        /// <param name="preserve_metadata">Preserve existing document metadata.</param>
        /// <param name="use_objstms">Store eligible objects in object streams (size reduction).</param>
        /// <param name="compression_effort">Compression effort 0 (default) to 100 (maximum).</param>
        /// <param name="raise_on_repair">Throw if save repairs the PDF structure.</param>
        /// <exception cref="ValueErrorException">Document is closed, encrypted, or arguments are invalid.</exception>
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
            int? noNewId = null,
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
            SaveCore(filename, garbage, clean, deflate, deflate_images, deflate_fonts, incremental, ascii, expand, linear, noNewId, appearance, pretty, encryption, permissions, owner_pw, user_pw, preserve_metadata, use_objstms, compression_effort, raise_on_repair);
        }
        /// <summary>
        /// PDF only: save the document
        /// </summary>
        /// <remarks>PDF only: Saves the document in its current state. PyMuPDF <c>Document.save</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="output">Output stream to receive PDF bytes.</param>
        /// <param name="garbage">Garbage collection level: 0 none; 1 unused objects; 2 compact xref; 3 merge duplicates; 4 stream dedup.</param>
        /// <param name="clean">Clean and sanitize content streams (mutool clean -sc).</param>
        /// <param name="deflate">Deflate (compress) uncompressed streams.</param>
        /// <param name="deflateImages">Deflate uncompressed image streams.</param>
        /// <param name="deflateFonts">Deflate uncompressed fontfile streams.</param>
        /// <param name="incremental">Incremental save to the original file only; excludes garbage and linear.</param>
        /// <param name="ascii">Convert binary stream data to ASCII.</param>
        /// <param name="expand">Decompress objects: 0 none, 1 images, 2 fonts, 255 all.</param>
        /// <param name="linear">Write linearized PDF; excludes incremental and object streams.</param>
        /// <param name="noNewId">If true, do not update the document /ID.</param>
        /// <param name="appearance">Regenerate widget appearance streams.</param>
        /// <param name="pretty">Prettify PDF object syntax.</param>
        /// <param name="encryption">Encryption method when saving.</param>
        /// <param name="permissions">Permission flags for encrypted output.</param>
        /// <param name="owner_pw">Owner password (max 40 characters).</param>
        /// <param name="user_pw">User password (max 40 characters).</param>
        /// <param name="preserve_metadata">Preserve existing document metadata.</param>
        /// <param name="use_objstms">Store eligible objects in object streams (size reduction).</param>
        /// <param name="compression_effort">Compression effort 0 (default) to 100 (maximum).</param>
        /// <param name="raise_on_repair">Throw if save repairs the PDF structure.</param>
        /// <exception cref="ValueErrorException">Document is closed, encrypted, or arguments are invalid.</exception>
        public void Save(
            Stream output,
            int garbage = 0,
            int clean = 0,
            int deflate = 0,
            int deflateImages = 0,
            int deflateFonts = 0,
            int incremental = 0,
            int ascii = 0,
            int expand = 0,
            int linear = 0,
            int? noNewId = null,
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
            SaveCore(output, garbage, clean, deflate, deflateImages, deflateFonts, incremental, ascii, expand, linear, noNewId, appearance, pretty, encryption, permissions, owner_pw, user_pw, preserve_metadata, use_objstms, compression_effort, raise_on_repair);
        }

        private void SaveCore(
            object filename,
            int garbage,
            int clean,
            int deflate,
            int deflate_images,
            int deflate_fonts,
            int incremental,
            int ascii,
            int expand,
            int linear,
            int? noNewId,
            int appearance,
            int pretty,
            int encryption,
            int permissions,
            string owner_pw,
            string user_pw,
            int preserve_metadata,
            int use_objstms,
            int compression_effort,
            bool raise_on_repair)
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
            // PyMuPDF: disallow overwriting the on-disk path unless incremental. Memory opens (StreamData) are ok.
            if (fname == Name && incremental == 0 && (StreamData == null || StreamData.Length == 0))
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
            
            // Use cached borrowed PdfDocument — do not dispose (owned by FzDocument).
            var pdf = NativePdfDocument;
            if (pdf.m_internal == null)
                throw new InvalidOperationException(Constants.MSG_IS_NO_PDF);
            try
            {
                using (var opts = new mupdf.PdfWriteOptions())
                {
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
                    opts.dont_regenerate_id = noNewId ?? 0;
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

                    pdf.m_internal.resynth_required = 0;
                    Helpers.JM_embedded_clean(pdf);
                    Helpers.JM_sync_fontfile_streams(pdf, this);
                    if ((noNewId ?? 0) == 0)
                        Helpers.JM_ensure_identity(pdf);

                    if (fname != null)
                    {
                        if (incremental != 0)
                        {
                            pdf.pdf_save_document(fname, opts);
                        }
                        else
                        {
                            // pdf_save_document removes the target first; on Windows that
                            // fails if another handle still has the file open. Write to memory
                            // then create/truncate via .NET (PyMuPDF uses pdf_save_document
                            // with fz_open_document(path) which does not share this pattern).
                            using var memStream = new MemoryStream();
                            var output = new FilePtrOutput(memStream);
                            try
                            {
                                pdf.pdf_write_document(output, opts);
                                output.fz_close_output();
                            }
                            finally
                            {
                                output.Dispose();
                            }
                            WriteBytesToFile(fname, memStream.ToArray());
                        }
                    }
                    else if (stream != null)
                    {
                        var memStream = stream as MemoryStream;
                        if (memStream == null)
                        {
                            using var copy = new MemoryStream();
                            if (stream.CanSeek)
                                stream.Position = 0;
                            stream.CopyTo(copy);
                            memStream = copy;
                        }
                        var output = new FilePtrOutput(memStream);
                        try
                        {
                            pdf.pdf_write_document(output, opts);
                            output.fz_close_output();
                        }
                        finally
                        {
                            output.Dispose();
                        }
                        if (!ReferenceEquals(stream, memStream) && stream.CanWrite)
                        {
                            memStream.Position = 0;
                            memStream.CopyTo(stream);
                        }
                    }
                }
            }
            finally
            {
                DropPdfPageTreeIfPdf();
            }
            if (raise_on_repair)
            {
                if (IsRepaired && !is_repaired_pre)
                    throw new Exception("Document save did a repair");
            }
        }
        /// <summary>
        /// PDF only: writes document to memory
        /// </summary>
        /// <remarks>PDF only: Writes the current content of the document to a bytes object instead of to a file. Obviously, you should be wary about memory requirements. The meanings of the parameters exactly equal those in <see cref="Save"/>. Chapter the PyMuPDF FAQ contains an example for using this method as a pre-processor to <c>pdfrw &lt;https://pypi.python.org/pypi/pdfrw/0.3&gt;. PyMuPDF <c>Document.tobytes</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="garbage">Garbage-collection level (0–4). Positive values exclude incremental save.</param>
        /// <param name="clean">If true, clean and sanitize content streams.</param>
        /// <param name="deflate">If true, deflate (compress) uncompressed streams.</param>
        /// <param name="deflateImages">If true, deflate uncompressed image streams.</param>
        /// <param name="deflateFonts">If true, deflate uncompressed font streams.</param>
        /// <param name="incremental">If true, save only changes (requires saving to the original path).</param>
        /// <param name="ascii">If true, restrict xref_object output to ASCII.</param>
        /// <param name="expand">Decompression level for objects (0, 1, 2, or 255 for all).</param>
        /// <param name="linear">If true, write a linearized PDF for fast web access.</param>
        /// <param name="noNewId">If true, do not regenerate the document /ID entry.</param>
        /// <param name="appearance">If true, regenerate widget appearance streams when saving.</param>
        /// <param name="pretty">If true, prettify PDF object syntax for readability.</param>
        /// <param name="encryption">Encryption method (see PyMuPDF encryption constants).</param>
        /// <param name="permissions">Permission flags bitmask (see PyMuPDF permission codes).</param>
        /// <param name="ownerPw">Owner password (max 40 characters).</param>
        /// <param name="userPw">User password (max 40 characters).</param>
        /// <param name="ownerPW">See PyMuPDF parameter &lt;c&gt;ownerPW&lt;/c&gt;.</param>
        /// <param name="userPW">See PyMuPDF parameter &lt;c&gt;userPW&lt;/c&gt;.</param>
        /// <param name="preserveMetadata">See PyMuPDF parameter &lt;c&gt;preserveMetadata&lt;/c&gt;.</param>
        /// <param name="useObjstms">See PyMuPDF parameter &lt;c&gt;useObjstms&lt;/c&gt;.</param>
        /// <param name="compressionEffort">See PyMuPDF parameter &lt;c&gt;compressionEffort&lt;/c&gt;.</param>
        /// <returns>a bytes object containing the complete document.</returns>
        public byte[] Write(bool garbage = false, bool clean = false, bool deflate = false,
            bool deflateImages = false, bool deflateFonts = false, bool incremental = false,
            bool ascii = false, bool expand = false, bool linear = false, bool noNewId = false,
            bool appearance = false, bool pretty = false, int encryption = 1, int permissions = 4095,
            string ownerPw = null, string userPw = null, string ownerPW = null, string userPW = null,
            bool preserveMetadata = true, bool useObjstms = false, bool compressionEffort = false)
        {
            string ownerPassword = ownerPW ?? ownerPw;
            string userPassword = userPW ?? userPw;
            using var ms = new MemoryStream();
            Save(
                ms,
                garbage: garbage ? 1 : 0,
                clean: clean ? 1 : 0,
                deflate: deflate ? 1 : 0,
                deflateImages: deflateImages ? 1 : 0,
                deflateFonts: deflateFonts ? 1 : 0,
                incremental: incremental ? 1 : 0,
                ascii: ascii ? 1 : 0,
                expand: expand ? 1 : 0,
                linear: linear ? 1 : 0,
                noNewId: noNewId ? 1 : 0,
                appearance: appearance ? 1 : 0,
                pretty: pretty ? 1 : 0,
                encryption: encryption,
                permissions: permissions,
                owner_pw: ownerPassword,
                user_pw: userPassword,
                preserve_metadata: preserveMetadata ? 1 : 0,
                use_objstms: useObjstms ? 1 : 0,
                compression_effort: compressionEffort ? 1 : 0);
            return ms.ToArray();
        }
        /// <summary>
        /// PDF only: writes the current document to a byte array (PyMuPDF <c>Document.tobytes</c>).
        /// </summary>
        /// <remarks>Parameter meanings match <see cref="Save(object, int, int, int, int, int, int, int, int, int, int?, int, int, int, int, string, string, int, int, int, bool)"/>.</remarks>
        /// <param name="garbage">Garbage-collection level (0–4).</param>
        /// <param name="clean">If true, clean and sanitize content streams.</param>
        /// <param name="deflate">If true, deflate uncompressed streams.</param>
        /// <returns>PDF file bytes.</returns>
        public byte[] ToBytes(bool garbage = false, bool clean = false, bool deflate = false) =>
            Write(garbage: garbage, clean: clean, deflate: deflate);

        /// <summary>
        /// Convert document to a PDF, selecting page range and optional rotation. Output bytes object.
        /// </summary>
        public byte[] ConvertToPdf(int fromPage = 0, int toPage = -1, int rotate = 0)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            var fz_doc = NativeDocument;
            int fp = fromPage;
            int tp = toPage;
            int srcCount = mupdf.mupdf.fz_count_pages(fz_doc);
            if (fp < 0)
                fp = 0;
            if (fp > srcCount - 1)
                fp = srcCount - 1;
            if (tp < 0)
                tp = srcCount - 1;
            if (tp > srcCount - 1)
                tp = srcCount - 1;
            int len0 = Helpers.JM_mupdf_warnings_store.Count;
            byte[] doc = Helpers.JmConvertToPdf(fz_doc, fp, tp, rotate);
            int len1 = Helpers.JM_mupdf_warnings_store.Count;
            for (int i = len0; i < len1; i++)
                Helpers.message($"{Helpers.JM_mupdf_warnings_store[i]}");
            return doc;
        }
        /// <summary>
        /// PDF only: save the document incrementally
        /// </summary>
        /// <remarks>PDF only: saves the document incrementally. This is a convenience abbreviation for <c>doc.save(doc.name, incremental=True, encryption=PDF_ENCRYPT_KEEP)</c>. PyMuPDF <c>Document.saveIncr</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void SaveIncr() => Save(Name, incremental: 1);
        /// <summary>
        /// check if incremental save is possible
        /// </summary>
        /// <remarks>Check whether the document can be saved incrementally. Use it to choose the right option without encountering exceptions. PyMuPDF <c>Document.can_save_incrementally</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns><see langword="true"/> if the operation succeeded.</returns>
        public bool CanSaveIncrementally()
        {
            try { return mupdf.mupdf.pdf_can_be_saved_incrementally(NativePdfDocument) != 0; }
            catch { return false; }
        }
        /// <summary>
        /// PDF only: <see cref="Save"/> with different defaults
        /// </summary>
        /// <remarks>PDF only: The same as <see cref="Save"/> but with changed defaults <c>deflate=True, garbage=3, use_objstms=1</c>. PyMuPDF <c>Document.ez_save</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="filename">File path to open or save.</param>
        /// <param name="garbage">Garbage-collection level (0–4). Positive values exclude incremental save.</param>
        /// <param name="clean">If true, clean and sanitize content streams.</param>
        /// <param name="deflate">If true, deflate (compress) uncompressed streams.</param>
        /// <param name="deflateImages">If true, deflate uncompressed image streams.</param>
        /// <param name="deflateFonts">If true, deflate uncompressed font streams.</param>
        /// <param name="pretty">If true, prettify PDF object syntax for readability.</param>
        /// <param name="linear">If true, write a linearized PDF for fast web access.</param>
        /// <param name="ascii">If true, restrict xref_object output to ASCII.</param>
        /// <param name="encryption">Encryption method (see PyMuPDF encryption constants).</param>
        /// <param name="noNewId">If true, do not regenerate the document /ID entry.</param>
        /// <param name="useObjstms">See PyMuPDF parameter &lt;c&gt;useObjstms&lt;/c&gt;.</param>
        public void EzSave(string filename, int garbage = 1, int clean = 0, int deflate = 1,
            int deflateImages = 1, int deflateFonts = 1, int pretty = 0, int linear = 0,
            int ascii = 0, int encryption = 1, int noNewId = 1, int useObjstms = 1)
        {
            Save(filename, garbage: garbage, clean: clean, deflate: deflate, deflate_images: deflateImages,
                deflate_fonts: deflateFonts, pretty: pretty, linear: linear, ascii: ascii, encryption: encryption,
                noNewId: noNewId, use_objstms: useObjstms);
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
        /// PDF only: xref of a page number
        /// </summary>
        /// <remarks>PDF only: Return the xref of the page -- without loading the page (via <see cref="LoadPage"/>). This is meant for internal purpose requiring best possible performance. PyMuPDF <c>Document.page_xref</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">0-based page number.</param>
        /// <returns>xref of the page like <see cref="Page.Xref"/>.</returns>
        public int PageXref(int pno)
        {
            pno = Helpers.ResolvePageIndex(PageCount, pno);
            return mupdf.mupdf.pdf_to_num(mupdf.mupdf.pdf_lookup_page_obj(NativePdfDocument, pno));
        }
        /// <summary>
        /// PDF only: get the definition source of xref
        /// </summary>
        /// <remarks>PDF only: Return the definition source of a PDF object. PyMuPDF <c>Document.xref_object</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">the object's xref. *Changed in v1.18.10:* A value of <c>-1</c> returns the PDF trailer source.</param>
        /// <param name="compressed">whether to generate a compact output with no line breaks or spaces.</param>
        /// <param name="ascii">whether to ASCII-encode binary data.</param>
        /// <returns>The object definition source.</returns>
        public string XrefObject(int xref, bool compressed = false, bool ascii = false)
        {
            if (IsClosed)
                throw new ValueErrorException("document closed");
            // if g_use_extra:
            //     ret = extra.xref_object( self.this, xref, compressed, ascii)
            //     return ret
            var pdf = Helpers.AsPdfDocument(this, required: true);
            int xreflen = mupdf.mupdf.pdf_xref_len(pdf);
            if (!Helpers.InRange(xref, 1, xreflen - 1) && xref != -1)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);
            mupdf.PdfObj obj;
            if (xref > 0)
                obj = mupdf.mupdf.pdf_load_object(pdf, xref);
            else
                obj = mupdf.mupdf.pdf_trailer(pdf);
            int compress = compressed ? 1 : 0;
            int asciiVal = ascii ? 1 : 0;
            using (var res = Helpers.JmObjectToBuffer(mupdf.mupdf.pdf_resolve_indirect(obj), compress, asciiVal))
            {
                string text = Helpers.JmEscapeStrFromBuffer(res);
                return text;
            }
        }
        /// <summary>
        /// Gets whether the xref identifies a stream object.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.xref_is_stream</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">PDF cross-reference number of the object.</param>
        /// <returns><see langword="true"/> if the operation succeeded.</returns>
        public bool XrefIsStream(int xref = 0)
        {
            try { return mupdf.mupdf.pdf_obj_num_is_stream(NativePdfDocument, xref) != 0; }
            catch { return false; }
        }
        /// <summary>
        /// Gets whether the xref identifies a font object.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.xref_is_font</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">PDF cross-reference number of the object.</param>
        /// <returns><see langword="true"/> if the operation succeeded.</returns>
        public bool XrefIsFont(int xref)
        {
            if (xref <= 0)
                return false;
            if (XrefGetKey(xref, "Type").value == "/Font")
                return true;
            var st = XrefGetKey(xref, "Subtype").value;
            return st == "/Type1" || st == "/TrueType" || st == "/MMType1" || st == "/Type3"
                || st == "/Type0" || st == "/CIDFontType0" || st == "/CIDFontType2" || st == "/CIDFontType0C";
        }
        /// <summary>
        /// Gets whether the xref identifies an image object.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.xref_is_image</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool XrefIsImage(int xref) => XrefGetKey(xref, "Subtype").value == "/Image";
        /// <summary>
        /// Gets whether the xref identifies a Form XObject.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.xref_is_xobject</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool XrefIsXobject(int xref) => XrefGetKey(xref, "Subtype").value == "/Form";
        /// <summary>
        /// Gets the decompressed stream bytes at xref.
        /// </summary>
        /// <remarks>PDF only: Return the decompressed contents of the xref stream object. PyMuPDF <c>Document.xref_stream</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">xref number.</param>
        /// <returns>the (decompressed) stream of the object.</returns>
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
        /// PDF only: raw stream source at xref
        /// </summary>
        /// <remarks>PDF only: Return the unmodified (esp. not decompressed) contents of the xref stream object. Otherwise equal to <see cref="XrefStream"/>. PyMuPDF <c>Document.xref_stream_raw</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">PDF cross-reference number of the object.</param>
        /// <returns>the (original, unmodified) stream of the object.</returns>
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
            => Helpers.PdfObjPrintToString(obj, compress, ascii);

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
            var sub = Helpers.PdfDictGetp(obj, key);
            if (sub.m_internal == null && !string.IsNullOrEmpty(key) && key[0] != '/')
                sub = Helpers.PdfDictGet(obj, mupdf.mupdf.pdf_new_name(key));
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
        /// PDF only: list the keys of object at xref
        /// </summary>
        /// <remarks>PDF only: Return the PDF dictionary keys of the dictionary object provided by its xref number. PyMuPDF <c>Document.xref_get_keys</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">the xref. *(Changed in v1.18.10)* Use <c>-1</c> to access the special dictionary "PDF trailer".</param>
        /// <returns>a tuple of dictionary keys present in object xref. Examples:</returns>
        public List<string> XrefGetKeys(int xref)
        {
            EnsureNotClosed();
            EnsureValidXrefDict(xref);
            var pdf = NativePdfDocument;
            var obj = xref > 0 ? mupdf.mupdf.pdf_load_object(pdf, xref) : mupdf.mupdf.pdf_trailer(pdf);
            int n = mupdf.mupdf.pdf_dict_len(obj);
            var rc = new List<string>(n);
            for (int i = 0; i < n; i++)
                rc.Add(mupdf.mupdf.pdf_to_name(Helpers.PdfDictGetKey(obj, i)));
            return rc;
        }

        // Python: INVALID_NAME_CHARS = set(string.whitespace + "()<>[]{}/%" + chr(0))
        private static readonly HashSet<char> InvalidNameChars = new HashSet<char>(
            " \t\n\r\f\v()<>[]{}%/\0");

        private static bool IsValidXrefSetKey(string key)
        {
            // INVALID_NAME_CHARS.intersection(key) not in (set(), {"/"})
            if (string.IsNullOrEmpty(key))
                return false;
            var bad = new HashSet<char>(key.Where(c => InvalidNameChars.Contains(c)));
            return bad.Count == 0 || (bad.Count == 1 && bad.Contains('/'));
        }

        private static bool IsValidXrefSetValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            if (value[0] == '/')
                return !value.Substring(1).Any(c => InvalidNameChars.Contains(c));
            return true;
        }
        /// <summary>
        /// PDF only: set the value of a dictionary key
        /// </summary>
        /// <remarks>PDF only: Set (add, update, delete) the value of a PDF key for the dictionary object given by its xref. PyMuPDF <c>Document.xref_set_key</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">the xref. *Changed in v1.18.13:* To update the PDF trailer, specify -1.</param>
        /// <param name="key">the desired PDF key (without leading "/"). Must not be empty. Any valid PDF key -- whether already present in the object (which will be overwritten) -- or new. It is possible to use PDF path notation like `"Resources/ExtGState"` -- which sets the value for key `"/ExtGState"` as a sub-object of `"/Resources"`.</param>
        public void XrefSetKey(int xref, string key, string value)
        {
            EnsureNotClosed();
            // if not key or not isinstance(key, str) or INVALID_NAME_CHARS.intersection(key) not in (set(), {"/"}):
            if (!IsValidXrefSetKey(key))
                throw new ValueErrorException("bad 'key'");
            // if not isinstance(value, str) or not value or value[0] == "/" and INVALID_NAME_CHARS.intersection(value[1:]) != set():
            if (!IsValidXrefSetValue(value))
                throw new ValueErrorException("bad 'value'");
            EnsureValidXrefDict(xref);
            var pdf = NativePdfDocument;
            var obj = xref > 0 ? mupdf.mupdf.pdf_load_object(pdf, xref) : mupdf.mupdf.pdf_trailer(pdf);
            // PyMuPDF JM_set_object_value: "null" writes a PDF null object (key remains in the dict).
            var newObj = Helpers.JmSetObjectValue(pdf, obj, key, value);
            if (newObj?.m_internal == null)
                return;
            if (xref != -1)
            {
                mupdf.mupdf.pdf_update_object(pdf, xref, newObj);
                return;
            }
            int n = mupdf.mupdf.pdf_dict_len(newObj);
            for (int i = 0; i < n; i++)
            {
                mupdf.mupdf.pdf_dict_put(
                    obj,
                    Helpers.PdfDictGetKey(newObj, i),
                    Helpers.PdfDictGetVal(newObj, i));
            }
        }
        /// <summary>
        /// Gets PDF only: xref of XML metadata.
        /// </summary>
        /// <value>PDF only: xref of XML metadata</value>
        /// <remarks>PDF only: Return the xref of the document's XML metadata. PyMuPDF <c>Document.xref_xml_metadata</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int XrefXmlMetadata
        {
            get
            {
                try
                {
                    var pdf = NativePdfDocument;
                    var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                    var xml = Helpers.PdfDictGets(root, "Metadata");
                    return xml.m_internal != null ? mupdf.mupdf.pdf_to_num(xml) : 0;
                }
                catch { return 0; }
            }
        }
        /// <summary>
        /// PDF only: read the XML metadata
        /// </summary>
        /// <remarks>PDF only: Get the document XML metadata. PyMuPDF <c>Document.get_xml_metadata</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns>XML metadata of the document. Empty string if not present or not a PDF.</returns>
        public string GetXmlMetadata()
        {
            // xml = None
            mupdf.PdfObj xml = new mupdf.PdfObj();
            // pdf = _as_pdf_document(self, required=0)
            var pdf = Helpers.AsPdfDocument(this, required: false);
            // if pdf.m_internal:
            if (pdf.m_internal != null)
            {
                // xml = mupdf.pdf_dict_getl(
                //         mupdf.pdf_trailer(pdf),
                //         PDF_NAME('Root'),
                //         PDF_NAME('Metadata'),
                //         )
                xml = Helpers.PdfDictGetl(
                    mupdf.mupdf.pdf_trailer(pdf),
                    mupdf.mupdf.pdf_new_name("Root"),
                    mupdf.mupdf.pdf_new_name("Metadata"));
            }
            // if xml is not None and xml.m_internal:
            if (xml.m_internal != null)
            {
                // buff = mupdf.pdf_load_stream(xml)
                using var buff = mupdf.mupdf.pdf_load_stream(xml);
                // rc = JM_UnicodeFromBuffer(buff)
                return Helpers.JM_UnicodeFromBuffer(buff);
            }
            // else:
            //     rc = ''
            return "";
        }
        /// <summary>
        /// PDF only: create or update document XML metadata
        /// </summary>
        /// <remarks>PDF only: Sets or updates XML metadata of the document. PyMuPDF <c>Document.set_xml_metadata</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="metadata">Metadata or XML string.</param>
        public void SetXmlMetadata(string metadata)
        {
            // if self.is_closed or self.is_encrypted:
            if (IsClosed || IsEncrypted)
                // raise ValueError("document closed or encrypted")
                throw new ValueErrorException("document closed or encrypted");
            // pdf = _as_pdf_document(self)
            var pdf = Helpers.AsPdfDocument(this, required: true);
            // root = Helpers.PdfObjDictGet(mupdf, mupdf.pdf_trailer( pdf), PDF_NAME('Root'))
            var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            // if not root.m_internal:
            if (root.m_internal == null)
                // RAISEPY( MSG_BAD_PDFROOT, JM_Exc_FileDataError)
                throw new FileDataException(Constants.MSG_BAD_PDFROOT);
            // res = mupdf.fz_new_buffer_from_copied_data( metadata.encode('utf-8'))
            var res = Helpers.BufferFromBytes(System.Text.Encoding.UTF8.GetBytes(metadata ?? ""));
            // xml = Helpers.PdfObjDictGet(mupdf, root, PDF_NAME('Metadata'))
            var xml = Helpers.PdfDictGet(root, mupdf.mupdf.pdf_new_name("Metadata"));
            // if xml.m_internal:
            if (xml.m_internal != null)
            {
                // JM_update_stream( pdf, xml, res, 0)
                Helpers.JmUpdateStream(pdf, xml, res, 0);
            }
            else
            {
                // xml = mupdf.pdf_add_stream( pdf, res, mupdf.PdfObj(), 0)
                xml = mupdf.mupdf.pdf_add_stream(pdf, res, new mupdf.PdfObj(), 0);
                // mupdf.pdf_dict_put( xml, PDF_NAME('Type'), PDF_NAME('Metadata'))
                mupdf.mupdf.pdf_dict_put(xml, mupdf.mupdf.pdf_new_name("Type"), mupdf.mupdf.pdf_new_name("Metadata"));
                // mupdf.pdf_dict_put( xml, PDF_NAME('Subtype'), PDF_NAME('XML'))
                mupdf.mupdf.pdf_dict_put(xml, mupdf.mupdf.pdf_new_name("Subtype"), mupdf.mupdf.pdf_new_name("XML"));
                // mupdf.pdf_dict_put( root, PDF_NAME('Metadata'), xml)
                mupdf.mupdf.pdf_dict_put(root, mupdf.mupdf.pdf_new_name("Metadata"), xml);
            }
        }
        /// <summary>
        /// See PyMuPDF Document.delete_xml_metadata.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.delete_xml_metadata</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void DeleteXmlMetadata()
        {
            EnsurePdf();
            var pdf = NativePdfDocument;
            var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            mupdf.mupdf.pdf_dict_dels(root, "Metadata");
        }
        /// <summary>
        /// PDF only: Replace object definition of xref with the provided string. The xref may also be new, in which case this instruction completes the object definition. If a page object is also given, its links and annotations will be reloaded afterwards.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.update_object</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">xref number.</param>
        /// <param name="text">Object definition or page text source.</param>
        /// <param name="page">Page object for context-sensitive updates.</param>
        public void UpdateObject(int xref, string text, Page page = null)
        {
            // if self.is_closed or self.is_encrypted:
            if (IsClosed || IsEncrypted)
                // raise ValueError("document closed or encrypted")
                throw new ValueErrorException("document closed or encrypted");
            // pdf = _as_pdf_document(self)
            var pdf = Helpers.AsPdfDocument(this, required: true);
            // xreflen = mupdf.pdf_xref_len(pdf)
            int xreflen = mupdf.mupdf.pdf_xref_len(pdf);
            // if not _INRANGE(xref, 1, xreflen-1):
            if (!Helpers.InRange(xref, 1, xreflen - 1))
                // RAISEPY("bad xref", MSG_BAD_XREF)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);
            // ENSURE_OPERATION(pdf)
            Helpers.ENSURE_OPERATION(pdf);
            // create new object with passed-in string
            // new_obj = JM_pdf_obj_from_str(pdf, text)
            var new_obj = Helpers.JM_pdf_obj_from_str(pdf, text);
            // mupdf.pdf_update_object(pdf, xref, new_obj)
            mupdf.mupdf.pdf_update_object(pdf, xref, new_obj);
            // if page:
            if (page != null)
                // JM_refresh_links( _as_pdf_page(page))
                Helpers.JM_refresh_links(pdf, Helpers.AsPdfPage(page, required: true));
        }
        /// <summary>
        /// Replace the stream of an object identified by xref, which must be a PDF dictionary. If the object is no stream, it will be turned into one. The function automatically performs a compress operation ("deflate") where beneficial.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.update_stream</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">PDF object number of the font to extract.</param>
        /// <param name="stream">the new content of the stream.</param>
        /// <param name="compress">whether to compress the inserted stream. If `True` (default), the stream will be inserted using `/FlateDecode` compression (if beneficial), otherwise the stream will inserted as is.</param>
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
            if (buf?.m_internal == null)
                throw new ArgumentException(Constants.MSG_BAD_BUFFER);
            Helpers.JmUpdateStream(pdf, obj, buf, compress ? 1 : 0);
        }
        /// <summary>
        /// PDF only: copy a PDF dictionary to another xref
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.xref_copy</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="document">Document instance.</param>
        /// <param name="source">Source xref for dictionary copy.</param>
        /// <param name="target">Target xref for dictionary copy.</param>
        /// <param name="keepKeys">Dictionary keys to preserve when copying xref dictionaries.</param>
        public static void XrefCopy(Document document, int source, int target, IReadOnlyCollection<string> keepKeys = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            document.XrefCopyImpl(source, target, keepKeys);
        }
        /// <summary>
        /// PDF only: copy a PDF dictionary to another xref
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.xref_copy</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
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
        /// See PyMuPDF Document.get_new_xref.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.get_new_xref</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns>A non-negative result code or xref number.</returns>
        public int GetNewXref()
        {
            EnsureNotClosed();
            if (IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            EnsurePdf();
            return mupdf.mupdf.pdf_create_object(NativePdfDocument);
        }
        /// <summary>
        /// PDF only: get OCG /OCMD xref of image / form xobject
        /// </summary>
        /// <remarks>Return the cross reference number of an OCG or OCMD attached to an image or form xobject. PyMuPDF <c>Document.get_oc</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">the xref of an image or form xobject. Valid such cross reference numbers are returned by <see cref="GetPageImages"/>, resp. <see cref="GetPageXobjects"/>. For invalid numbers, an exception is raised.</param>
        /// <returns>the cross reference number of an optional contents object or zero if there is none.</returns>
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
        /// PDF only: attach OCG/OCMD to image / form xobject
        /// </summary>
        /// <remarks>If xref represents an image or form xobject, set or remove the cross reference number *ocxref* of an optional contents object. PyMuPDF <c>Document.set_oc</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">the xref of an image or form xobject . Valid such cross reference numbers are returned by <see cref="GetPageImages"/>, resp. <see cref="GetPageXobjects"/>. For invalid numbers, an exception is raised.</param>
        /// <param name="oc">OCG or OCMD xref to attach.</param>
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
        /// PDF only: retrieve definition of an OCMD
        /// </summary>
        /// <remarks>Retrieve the definition of an OCMD. PyMuPDF <c>Document.get_ocmd</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">the xref of the OCMD.</param>
        /// <returns>a dictionary with the keys xref, *ocgs*, *policy* and *ve*.</returns>
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
        /// PDF only: create or update an OCMD
        /// </summary>
        /// <remarks>Create or update an OCMD, Optional Content Membership Dictionary. PyMuPDF <c>Document.set_ocmd</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">xref of the OCMD to be updated, or 0 for a new OCMD.</param>
        /// <param name="ocgs">a sequence of xref numbers of existing OCG PDF objects.</param>
        /// <param name="policy">one of "AnyOn" (default), "AnyOff", "AllOn", "AllOff" (mixed or lower case).</param>
        /// <param name="ve">a "visibility expression". This is a list of arbitrarily nested other lists -- see explanation below. Use as an alternative to the combination *ocgs* / *policy* if you need to formulate more complex conditions.</param>
        /// <returns>xref of the OCMD. Use as <c>oc=xref</c> parameter in supporting objects, and respectively in <see cref="SetOc"/> or <see cref="Annot.SetOc"/>.</returns>
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
        /// PDF only: Convert destination names into a Python dict
        /// </summary>
        /// <remarks>PDF only: Convert destination names into a Python dict. PyMuPDF <c>Document.resolve_names</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns>A dictionary of entries.</returns>
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
                    array = Helpers.PdfObjPrintToString(val, 1, 0);
                else if (mupdf.mupdf.pdf_is_dict(val) != 0)
                    array = Helpers.PdfObjPrintToString(Helpers.PdfDictGets(val, "D"), 1, 0);
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
                    var split = array.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    var arrayList = new List<string>();
                    for (int si = 1; si < split.Length && si < 4; si++)
                        arrayList.Add(split[si]);
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
                    var key = Helpers.PdfDictGetKey(pdfDict, i);
                    var val = Helpers.PdfDictGetVal(pdfDict, i);
                    string dictKey = null;
                    if (mupdf.mupdf.pdf_is_name(key) != 0)
                        dictKey = mupdf.mupdf.pdf_to_name(key);
                    if (!string.IsNullOrEmpty(dictKey))
                        destDict[dictKey] = GetArray(val);
                }
            }

            var pdf = NativePdfDocument;
            var catalog = Helpers.PdfDictGets(mupdf.mupdf.pdf_trailer(pdf), "Root");

            var destDictResult = new Dictionary<string, Dictionary<string, object>>();
            var dests = mupdf.mupdf.pdf_new_name("Dests");

            var oldDests = Helpers.PdfDictGet(catalog, dests);
            if (mupdf.mupdf.pdf_is_dict(oldDests) != 0)
                FillDict(destDictResult, oldDests);

            var tree = mupdf.mupdf.pdf_load_name_tree(pdf, dests);
            if (mupdf.mupdf.pdf_is_dict(tree) != 0)
                FillDict(destDictResult, tree);

            _resolvedNames = destDictResult;
            return destDictResult;
        }
        /// <summary>
        /// Gets Gets the xref of the PDF catalog (root) dictionary.
        /// </summary>
        /// <value>Gets the xref of the PDF catalog (root) dictionary.</value>
        /// <remarks>PDF only: Return the xref number of the PDF catalog (or root) object. Use that number with <see cref="XrefObject"/> to see its source. PyMuPDF <c>Document.pdf_catalog</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int PdfCatalog
        {
            get
            {
                try
                {
                    var pdf = NativePdfDocument;
                    var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                    return mupdf.mupdf.pdf_to_num(root);
                }
                catch { return 0; }
            }
        }
        /// <summary>
        /// Gets the trailer dictionary as a formatted string.
        /// </summary>
        /// <remarks>PDF only: Return the trailer source of the PDF, which is usually located at the PDF file's end. This is <see cref="XrefObject"/> with an xref argument of -1. PyMuPDF <c>Document.pdf_trailer</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="compressed">If true, compress object syntax in xref_object output.</param>
        /// <param name="ascii">If true, restrict xref_object output to ASCII.</param>
        public string PdfTrailer(bool compressed = false, bool ascii = false)
        {
            return XrefObject(-1, compressed, ascii);
        }

        // ─── Embedded Files ─────────────────────────────────────────────
        /// <summary>
        /// Gets Gets the number of embedded files.
        /// </summary>
        /// <value>Gets the number of embedded files.</value>
        /// <remarks>PDF only: Return the number of embedded files. PyMuPDF <c>Document.embfile_count</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int EmbeddedFileCount
        {
            get
            {
                if (!IsPdf) return 0;
                return GetEmbeddedFileNames().Count;
            }
        }
        /// <summary>
        /// PDF only: list of embedded files
        /// </summary>
        /// <remarks>PDF only: Return a list of embedded file names. The sequence of the names equals the physical sequence in the document. PyMuPDF <c>Document.embfile_names</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns>A list of results.</returns>
        public List<string> GetEmbeddedFileNames()
        {
            var filenames = new List<string>();
            if (IsPdf)
                _embfile_names(filenames);
            return filenames;
        }
        /// <summary>
        /// PDF only: extract an embedded file buffer
        /// </summary>
        /// <remarks>PDF only: Retrieve the content of embedded file by its entry number or name. If the document is not a PDF, or entry cannot be found, an exception is raised. PyMuPDF <c>Document.embfile_get</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public byte[] GetEmbeddedFile(string name) => _embeddedFileGet(_embeddedFileIndex(name));
        /// <summary>
        /// PDF only: extract an embedded file buffer
        /// </summary>
        /// <remarks>PDF only: Retrieve the content of embedded file by its entry number or name. If the document is not a PDF, or entry cannot be found, an exception is raised. PyMuPDF <c>Document.embfile_get</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public byte[] GetEmbeddedFile(int idx) => _embeddedFileGet(_embeddedFileIndex(idx));
        /// <summary>
        /// PDF only: delete an embedded file entry
        /// </summary>
        /// <remarks>PDF only: Remove an entry from `/EmbeddedFiles`. As always, physical deletion of the embedded file content (and file space regain) will occur only when the document is saved to a new file with a suitable garbage option. PyMuPDF <c>Document.embfile_del</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void DeleteEmbeddedFile(string name) => _embfile_del(_embeddedFileIndex(name));
        /// <summary>
        /// PDF only: delete an embedded file entry
        /// </summary>
        /// <remarks>PDF only: Remove an entry from `/EmbeddedFiles`. As always, physical deletion of the embedded file content (and file space regain) will occur only when the document is saved to a new file with a suitable garbage option. PyMuPDF <c>Document.embfile_del</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void DeleteEmbeddedFile(int idx) => _embfile_del(_embeddedFileIndex(idx));
        /// <summary>
        /// Adds an embedded file from a byte buffer.
        /// </summary>
        /// <remarks>PDF only: Embed a new file. All string parameters except the name may be unicode (in previous versions, only ASCII worked correctly). File contents will be compressed (where beneficial). PyMuPDF <c>Document.embfile_add</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="name">entry identifier, must not already exist.</param>
        /// <param name="buffer">file contents.</param>
        /// <param name="filename">optional filename. Documentation only, will be set to *name* if <c>None</c>.</param>
        /// <param name="uFileName">See PyMuPDF parameter &lt;c&gt;uFileName&lt;/c&gt;.</param>
        /// <param name="desc">optional description. Documentation only, will be set to *name* if <c>None</c>.</param>
        /// <returns>*(Changed in v1.18.13)* The method now returns the xref of the inserted file. In addition, the file object now will be automatically given the PDF keys <c>/CreationDate</c> and <c>/ModDate</c> based on the current date-time.</returns>
        public int AddEmbeddedFile(string name, byte[] buffer, string filename = null, string uFileName = null, string desc = null)
        {
            var filenames = GetEmbeddedFileNames();
            if (filenames.Contains(name))
                throw new ValueErrorException($"Name '{name}' already exists.");
            if (filename == null) filename = name;
            if (uFileName == null) uFileName = filename;
            if (desc == null) desc = name;
            int xref = _embfile_add(name, buffer, filename, uFileName, desc);
            string date = Helpers.GetPdfNow();
            XrefSetKey(xref, "Type", "/EmbeddedFile");
            XrefSetKey(xref, "Params/CreationDate", Helpers.GetPdfStr(date));
            XrefSetKey(xref, "Params/ModDate", Helpers.GetPdfStr(date));
            return xref;
        }
        /// <summary>
        /// PDF only: metadata of an embedded file
        /// </summary>
        /// <remarks>PDF only: Retrieve information of an embedded file given by its number or by its name. PyMuPDF <c>Document.embfile_info</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public Dictionary<string, object> GetEmbeddedFileInfo(string name) => GetEmbeddedFileInfo(_embeddedFileIndex(name));
        /// <summary>
        /// PDF only: metadata of an embedded file
        /// </summary>
        /// <remarks>PDF only: Retrieve information of an embedded file given by its number or by its name. PyMuPDF <c>Document.embfile_info</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="idx">0-based index in the table of contents.</param>
        /// <returns>a dictionary with the following keys:</returns>
        public Dictionary<string, object> GetEmbeddedFileInfo(int idx)
        {
            idx = _embeddedFileIndex(idx);
            var infodict = new Dictionary<string, object> { ["name"] = GetEmbeddedFileNames()[idx] };
            int xref = _embfile_info(idx, infodict);
            var (t, date) = XrefGetKey(xref, "Params/CreationDate");
            if (t != "null")
                infodict["creationDate"] = date;
            (t, date) = XrefGetKey(xref, "Params/ModDate");
            if (t != "null")
                infodict["modDate"] = date;
            (t, var md5) = XrefGetKey(xref, "Params/CheckSum");
            if (t != "null")
            {
                var md5Bytes = Encoding.UTF8.GetBytes(md5);
#if NET5_0_OR_GREATER
                infodict["checksum"] = Helpers.BytesToHex(md5Bytes).ToLowerInvariant();
#else
                var sb = new StringBuilder(md5Bytes.Length * 2);
                foreach (byte b in md5Bytes)
                    sb.Append(b.ToString("x2"));
                infodict["checksum"] = sb.ToString();
#endif
            }
            return infodict;
        }
        /// <summary>
        /// Updates an existing embedded file entry.
        /// </summary>
        /// <remarks>PDF only: Change an embedded file given its entry number or name. All parameters are optional. Letting them default leads to a no-operation. PyMuPDF <c>Document.embfile_upd</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int UpdateEmbeddedFile(string name, byte[] buffer = null, string filename = null, string uFileName = null, string desc = null)
            => UpdateEmbeddedFile(_embeddedFileIndex(name), buffer, filename, uFileName, desc);
        /// <summary>
        /// Updates an existing embedded file entry.
        /// </summary>
        /// <remarks>PDF only: Change an embedded file given its entry number or name. All parameters are optional. Letting them default leads to a no-operation. PyMuPDF <c>Document.embfile_upd</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="idx">0-based index in the table of contents.</param>
        /// <param name="buffer">the new file content.</param>
        /// <param name="filename">the new filename.</param>
        /// <param name="uFileName">See PyMuPDF parameter &lt;c&gt;uFileName&lt;/c&gt;.</param>
        /// <param name="desc">the new description.</param>
        /// <returns>xref of the file object. Automatically, its `/ModDate` PDF key will be updated with the current date-time.</returns>
        public int UpdateEmbeddedFile(int idx, byte[] buffer = null, string filename = null, string uFileName = null, string desc = null)
        {
            idx = _embeddedFileIndex(idx);
            int xref = _embfile_upd(idx, buffer, filename, uFileName, desc);
            XrefSetKey(xref, "Params/ModDate", Helpers.GetPdfStr(Helpers.GetPdfNow()));
            return xref;
        }

        /// <summary>PyMuPDF <c>Document._embeddedFileGet</c>.</summary>
        private byte[] _embeddedFileGet(int idx)
        {
            var pdf = NativePdfDocument;
            var names = Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"),
                mupdf.mupdf.pdf_new_name("Names"),
                mupdf.mupdf.pdf_new_name("EmbeddedFiles"),
                mupdf.mupdf.pdf_new_name("Names"));
            var entry = mupdf.mupdf.pdf_array_get(names, 2 * idx + 1);
            var filespec = Helpers.PdfDictGetl(entry, mupdf.mupdf.pdf_new_name("EF"), mupdf.mupdf.pdf_new_name("F"));
            var buf = mupdf.mupdf.pdf_load_stream(filespec);
            return buf.fz_buffer_extract();
        }

        /// <summary>PyMuPDF <c>Document._embeddedFileIndex</c>.</summary>
        private int _embeddedFileIndex(object item)
        {
            var filenames = GetEmbeddedFileNames();
            if (item is string s)
            {
                int i = filenames.IndexOf(s);
                if (i >= 0) return i;
            }
            else if (item is int idx && idx >= 0 && idx < filenames.Count)
                return idx;
            throw new ValueErrorException($"'{item}' not in EmbeddedFiles array.");
        }

        /// <summary>PyMuPDF <c>Document._embfile_add</c>.</summary>
        private int _embfile_add(string name, byte[] buffer_, string filename, string ufilename, string desc)
        {
            var pdf = NativePdfDocument;
            var data = Helpers.BufferFromBytes(buffer_);
            if (data.m_internal == null)
                throw new ArgumentException(Constants.MSG_BAD_BUFFER);

            var names = Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"),
                mupdf.mupdf.pdf_new_name("Names"),
                mupdf.mupdf.pdf_new_name("EmbeddedFiles"),
                mupdf.mupdf.pdf_new_name("Names"));
            if (mupdf.mupdf.pdf_is_array(names) == 0)
            {
                var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                names = mupdf.mupdf.pdf_new_array(pdf, 6);
                Helpers.PdfDictPutl(
                    pdf,
                    root,
                    names,
                    mupdf.mupdf.pdf_new_name("Names"),
                    mupdf.mupdf.pdf_new_name("EmbeddedFiles"),
                    mupdf.mupdf.pdf_new_name("Names"));
            }
            var fileentry = Helpers.JmEmbedFile(pdf, data, filename, ufilename, desc, 1);
            int xref = mupdf.mupdf.pdf_to_num(
                Helpers.PdfDictGetl(fileentry, mupdf.mupdf.pdf_new_name("EF"), mupdf.mupdf.pdf_new_name("F")));
            mupdf.mupdf.pdf_array_push(names, mupdf.mupdf.pdf_new_text_string(name));
            mupdf.mupdf.pdf_array_push(names, fileentry);
            return xref;
        }

        /// <summary>PyMuPDF <c>Document._embfile_del</c>.</summary>
        private void _embfile_del(int idx)
        {
            var pdf = NativePdfDocument;
            var names = Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"),
                mupdf.mupdf.pdf_new_name("Names"),
                mupdf.mupdf.pdf_new_name("EmbeddedFiles"),
                mupdf.mupdf.pdf_new_name("Names"));
            mupdf.mupdf.pdf_array_delete(names, idx + 1);
            mupdf.mupdf.pdf_array_delete(names, idx);
        }

        /// <summary>PyMuPDF <c>Document._embfile_info</c>.</summary>
        private int _embfile_info(int idx, Dictionary<string, object> infodict)
        {
            var pdf = NativePdfDocument;
            var names = Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"),
                mupdf.mupdf.pdf_new_name("Names"),
                mupdf.mupdf.pdf_new_name("EmbeddedFiles"),
                mupdf.mupdf.pdf_new_name("Names"));
            var o = mupdf.mupdf.pdf_array_get(names, 2 * idx + 1);
            var ci = Helpers.PdfDictGet(o, mupdf.mupdf.pdf_new_name("CI"));
            infodict["collection"] = ci.m_internal != null ? mupdf.mupdf.pdf_to_num(ci) : 0;
            infodict["filename"] = JM_EscapeStrFromStr(
                mupdf.mupdf.pdf_to_text_string(Helpers.PdfDictGet(o, mupdf.mupdf.pdf_new_name("F"))) ?? "");
            infodict["ufilename"] = JM_EscapeStrFromStr(
                mupdf.mupdf.pdf_to_text_string(Helpers.PdfDictGet(o, mupdf.mupdf.pdf_new_name("UF"))) ?? "");
            infodict["description"] = mupdf.mupdf.pdf_to_text_string(
                Helpers.PdfDictGet(o, mupdf.mupdf.pdf_new_name("Desc")));

            int len_ = -1;
            int dl = -1;
            var fileentry = Helpers.PdfDictGetl(o, mupdf.mupdf.pdf_new_name("EF"), mupdf.mupdf.pdf_new_name("F"));
            int xref = mupdf.mupdf.pdf_to_num(fileentry);
            var lengthObj = Helpers.PdfDictGet(fileentry, mupdf.mupdf.pdf_new_name("Length"));
            if (lengthObj.m_internal != null)
                len_ = mupdf.mupdf.pdf_to_int(lengthObj);
            var dlObj = Helpers.PdfDictGet(fileentry, mupdf.mupdf.pdf_new_name("DL"));
            if (dlObj.m_internal != null)
                dl = mupdf.mupdf.pdf_to_int(dlObj);
            else
            {
                var sizeObj = Helpers.PdfDictGetl(fileentry, mupdf.mupdf.pdf_new_name("Params"), mupdf.mupdf.pdf_new_name("Size"));
                if (sizeObj.m_internal != null)
                    dl = mupdf.mupdf.pdf_to_int(sizeObj);
            }
            infodict["size"] = dl;
            infodict["length"] = len_;
            return xref;
        }

        /// <summary>PyMuPDF <c>Document._embfile_names</c>.</summary>
        private void _embfile_names(List<string> namelist)
        {
            var pdf = NativePdfDocument;
            var names = Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"),
                mupdf.mupdf.pdf_new_name("Names"),
                mupdf.mupdf.pdf_new_name("EmbeddedFiles"),
                mupdf.mupdf.pdf_new_name("Names"));
            if (mupdf.mupdf.pdf_is_array(names) != 0)
            {
                int n = mupdf.mupdf.pdf_array_len(names);
                for (int i = 0; i < n; i += 2)
                {
                    namelist.Add(JM_EscapeStrFromStr(
                        mupdf.mupdf.pdf_to_text_string(mupdf.mupdf.pdf_array_get(names, i)) ?? ""));
                }
            }
        }

        /// <summary>PyMuPDF <c>Document._embfile_upd</c>.</summary>
        private int _embfile_upd(int idx, byte[] buffer_, string filename, string ufilename, string desc)
        {
            var pdf = NativePdfDocument;
            var names = Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"),
                mupdf.mupdf.pdf_new_name("Names"),
                mupdf.mupdf.pdf_new_name("EmbeddedFiles"),
                mupdf.mupdf.pdf_new_name("Names"));
            var entry = mupdf.mupdf.pdf_array_get(names, 2 * idx + 1);
            var filespec = Helpers.PdfDictGetl(entry, mupdf.mupdf.pdf_new_name("EF"), mupdf.mupdf.pdf_new_name("F"));
            if (filespec.m_internal == null)
                throw new FileDataException("bad PDF: no /EF object");

            var res = Helpers.BufferFromBytes(buffer_);
            if (buffer_ != null && buffer_.Length > 0 && res.m_internal == null)
                throw new ArgumentException(Constants.MSG_BAD_BUFFER);
            if (res.m_internal != null && buffer_ != null && buffer_.Length > 0)
            {
                Helpers.JmUpdateStream(pdf, filespec, res, 1);
                using var outSt = new mupdf.ll_fz_buffer_storage_outparams();
                uint len = mupdf.mupdf.ll_fz_buffer_storage_outparams_fn(res.m_internal, outSt);
                var l = mupdf.mupdf.pdf_new_int((int)len);
                mupdf.mupdf.pdf_dict_put(filespec, mupdf.mupdf.pdf_new_name("DL"), l);
                Helpers.PdfDictPutl(
                    pdf,
                    filespec,
                    l,
                    mupdf.mupdf.pdf_new_name("Params"),
                    mupdf.mupdf.pdf_new_name("Size"));
            }
            int xref = mupdf.mupdf.pdf_to_num(filespec);
            if (filename != null)
                mupdf.mupdf.pdf_dict_put_text_string(entry, mupdf.mupdf.pdf_new_name("F"), filename);
            if (ufilename != null)
                mupdf.mupdf.pdf_dict_put_text_string(entry, mupdf.mupdf.pdf_new_name("UF"), ufilename);
            if (desc != null)
                mupdf.mupdf.pdf_dict_put_text_string(entry, mupdf.mupdf.pdf_new_name("Desc"), desc);
            return xref;
        }

        // ─── Font / Image extraction ────────────────────────────────────

        /// <summary>
        /// List fonts used on a page (PyMuPDF <c>Document.get_page_fonts</c>).
        /// When <paramref name="full"/> is false, the last tuple item is null (Python omits the referencer xref).
        /// When true, it is the stream xref of the Form XObject whose <c>/Resources</c> contained the font (0 on the page itself).
        /// </summary>
        public List<(int xref, string ext, string type, string baseName, string name, string encoding, int? referencer)> GetPageFonts(int pno, bool full = false)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            if (!IsPdf)
                return new List<(int xref, string ext, string type, string baseName, string name, string encoding, int?)>();
            var val = UnboxFontRowsFromPageInfo(_getPageInfo(pno, 1));
            if (!full)
                return val.ConvertAll(t => (t.xref, t.ext, t.type, t.baseName, t.name, t.encoding, (int?)null));
            return val.ConvertAll(t => (t.xref, t.ext, t.type, t.baseName, t.name, t.encoding, (int?)t.streamXref));
        }

        /// <summary>PyMuPDF <c>get_page_fonts(doc, page)</c> overload using <see cref="Page.Number"/>.</summary>
        public List<(int xref, string ext, string type, string baseName, string name, string encoding, int? referencer)> GetPageFonts(Page page, bool full = false)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            return GetPageFonts(page.Number, full);
        }

        private static List<(int xref, string ext, string type, string baseName, string name, string encoding, int streamXref)> UnboxFontRowsFromPageInfo(List<object> liste)
        {
            var r = new List<(int xref, string ext, string type, string baseName, string name, string encoding, int streamXref)>(liste.Count);
            foreach (var o in liste)
                r.Add(((int xref, string ext, string type, string baseName, string name, string encoding, int streamXref))o);
            return r;
        }

        private int _normalize_pno_for_get_page_info(int pno)
        {
            int pageCount = PageCount;
            if (pageCount <= 0)
                throw new ValueErrorException(Constants.MSG_BAD_PAGENO);
            int n = pno;
            while (n < 0)
                n += pageCount;
            if (n >= pageCount)
                throw new ValueErrorException(Constants.MSG_BAD_PAGENO);
            return n;
        }

        private void JM_scan_resources(mupdf.PdfDocument pdf, mupdf.PdfObj rsrc, List<object> liste, int what, int stream_xref, List<int> tracer)
        {
            if (what < 1 || what > 3)
                return;
            if (mupdf.mupdf.pdf_mark_obj(rsrc) != 0)
            {
                mupdf.mupdf.fz_warn("Circular dependencies! Consider page cleaning.");
                return;
            }
            try
            {
                var xobj = Helpers.PdfDictGet(rsrc, mupdf.mupdf.pdf_new_name("XObject"));
                if (what == 1)
                {
                    var font = Helpers.PdfDictGet(rsrc, mupdf.mupdf.pdf_new_name("Font"));
                    JM_gather_fonts(pdf, font, liste, stream_xref);
                }
                else if (what == 2)
                    JM_gather_images(pdf, xobj, liste, stream_xref);
                else
                    JM_gather_forms(pdf, xobj, liste, stream_xref);

                if (xobj.m_internal == null || mupdf.mupdf.pdf_is_dict(xobj) == 0)
                    return;
                int n = mupdf.mupdf.pdf_dict_len(xobj);
                for (int i = 0; i < n; i++)
                {
                    var obj = Helpers.PdfDictGetVal(xobj, i);
                    int sxref = mupdf.mupdf.pdf_is_stream(obj) != 0 ? mupdf.mupdf.pdf_to_num(obj) : 0;
                    var subrsrc = Helpers.PdfDictGet(obj, mupdf.mupdf.pdf_new_name("Resources"));
                    if (subrsrc.m_internal == null)
                        continue;
                    if (!tracer.Contains(sxref))
                    {
                        tracer.Add(sxref);
                        JM_scan_resources(pdf, subrsrc, liste, what, sxref, tracer);
                    }
                    else
                    {
                        mupdf.mupdf.fz_warn("Circular dependencies! Consider page cleaning.");
                        return;
                    }
                }
            }
            finally
            {
                mupdf.mupdf.pdf_unmark_obj(rsrc);
            }
        }

        private void JM_gather_fonts(mupdf.PdfDocument pdf, mupdf.PdfObj dict_, List<object> fontlist, int stream_xref)
        {
            if (dict_.m_internal == null || mupdf.mupdf.pdf_is_dict(dict_) == 0)
                return;
            int n = mupdf.mupdf.pdf_dict_len(dict_);
            for (int i = 0; i < n; i++)
            {
                var refname = Helpers.PdfDictGetKey(dict_, i);
                var fontdict = Helpers.PdfDictGetVal(dict_, i);
                if (mupdf.mupdf.pdf_is_dict(fontdict) == 0)
                {
                    mupdf.mupdf.fz_warn($"'{mupdf.mupdf.pdf_to_name(refname)}' is no font dict ({mupdf.mupdf.pdf_to_num(fontdict)} 0 R)");
                    continue;
                }

                var subtype = Helpers.PdfDictGet(fontdict, mupdf.mupdf.pdf_new_name("Subtype"));
                var basefont = Helpers.PdfDictGet(fontdict, mupdf.mupdf.pdf_new_name("BaseFont"));
                mupdf.PdfObj nameObj = (basefont.m_internal != null && mupdf.mupdf.pdf_is_null(basefont) == 0)
                    ? basefont
                    : Helpers.PdfDictGet(fontdict, mupdf.mupdf.pdf_new_name("Name"));
                var encoding = Helpers.PdfDictGet(fontdict, mupdf.mupdf.pdf_new_name("Encoding"));
                if (mupdf.mupdf.pdf_is_dict(encoding) != 0)
                    encoding = Helpers.PdfDictGet(encoding, mupdf.mupdf.pdf_new_name("BaseEncoding"));
                int xref = mupdf.mupdf.pdf_to_num(fontdict);
                string ext = "n/a";
                if (xref != 0)
                    ext = JM_get_fontextension(xref);
                string st = mupdf.mupdf.pdf_to_name(subtype) ?? "";
                string nm = JM_EscapeStrFromStr(mupdf.mupdf.pdf_to_name(nameObj) ?? "");
                string enc = mupdf.mupdf.pdf_to_name(encoding) ?? "";
                string rn = mupdf.mupdf.pdf_to_name(refname) ?? "";
                fontlist.Add((xref, ext, st, nm, rn, enc, stream_xref));
            }
        }

        private void JM_gather_images(mupdf.PdfDocument doc, mupdf.PdfObj dict_, List<object> imagelist, int stream_xref)
        {
            if (dict_.m_internal == null || mupdf.mupdf.pdf_is_dict(dict_) == 0)
                return;
            int n = mupdf.mupdf.pdf_dict_len(dict_);
            for (int i = 0; i < n; i++)
            {
                var refname = Helpers.PdfDictGetKey(dict_, i);
                var imagedict = Helpers.PdfDictGetVal(dict_, i);
                if (mupdf.mupdf.pdf_is_dict(imagedict) == 0)
                {
                    mupdf.mupdf.fz_warn($"'{mupdf.mupdf.pdf_to_name(refname)}' is no image dict ({mupdf.mupdf.pdf_to_num(imagedict)} 0 R)");
                    continue;
                }
                var type_ = Helpers.PdfDictGet(imagedict, mupdf.mupdf.pdf_new_name("Subtype"));
                if (mupdf.mupdf.pdf_name_eq(type_, mupdf.mupdf.pdf_new_name("Image")) == 0)
                    continue;
                int xref = mupdf.mupdf.pdf_to_num(imagedict);
                int gen = 0;
                var smask = Helpers.PdfDictGeta(imagedict, mupdf.mupdf.pdf_new_name("SMask"), mupdf.mupdf.pdf_new_name("Mask"));
                if (smask.m_internal != null)
                    gen = mupdf.mupdf.pdf_to_num(smask);
                var filter_ = Helpers.PdfDictGeta(imagedict, mupdf.mupdf.pdf_new_name("Filter"), mupdf.mupdf.pdf_new_name("F"));
                if (mupdf.mupdf.pdf_is_array(filter_) != 0 && mupdf.mupdf.pdf_array_len(filter_) > 0)
                    filter_ = mupdf.mupdf.pdf_array_get(filter_, 0);
                var altcs = new mupdf.PdfObj();
                var cs = Helpers.PdfDictGeta(imagedict, mupdf.mupdf.pdf_new_name("ColorSpace"), mupdf.mupdf.pdf_new_name("CS"));
                if (mupdf.mupdf.pdf_is_array(cs) != 0 && mupdf.mupdf.pdf_array_len(cs) > 0)
                {
                    var cses = cs;
                    cs = mupdf.mupdf.pdf_array_get(cses, 0);
                    if (mupdf.mupdf.pdf_name_eq(cs, mupdf.mupdf.pdf_new_name("DeviceN")) != 0
                        || mupdf.mupdf.pdf_name_eq(cs, mupdf.mupdf.pdf_new_name("Separation")) != 0)
                    {
                        var altcsCandidate = mupdf.mupdf.pdf_array_get(cses, 2);
                        if (mupdf.mupdf.pdf_is_array(altcsCandidate) != 0 && mupdf.mupdf.pdf_array_len(altcsCandidate) > 0)
                            altcs = mupdf.mupdf.pdf_array_get(altcsCandidate, 0);
                        else
                            altcs = altcsCandidate;
                    }
                }
                var width = Helpers.PdfDictGeta(imagedict, mupdf.mupdf.pdf_new_name("Width"), mupdf.mupdf.pdf_new_name("W"));
                var height = Helpers.PdfDictGeta(imagedict, mupdf.mupdf.pdf_new_name("Height"), mupdf.mupdf.pdf_new_name("H"));
                var bpc = Helpers.PdfDictGeta(imagedict, mupdf.mupdf.pdf_new_name("BitsPerComponent"), mupdf.mupdf.pdf_new_name("BPC"));
                int wi = width.m_internal != null ? mupdf.mupdf.pdf_to_int(width) : 0;
                int hi = height.m_internal != null ? mupdf.mupdf.pdf_to_int(height) : 0;
                int bpci = bpc.m_internal != null ? mupdf.mupdf.pdf_to_int(bpc) : 0;
                string csName = JM_EscapeStrFromStr(mupdf.mupdf.pdf_to_name(cs) ?? "");
                string altcsName = JM_EscapeStrFromStr(mupdf.mupdf.pdf_to_name(altcs) ?? "");
                string nmName = JM_EscapeStrFromStr(mupdf.mupdf.pdf_to_name(refname) ?? "");
                string fltName = JM_EscapeStrFromStr(mupdf.mupdf.pdf_to_name(filter_) ?? "");
                imagelist.Add((xref, gen, wi, hi, bpci, csName, altcsName, nmName, fltName, stream_xref));
            }
        }

        private void JM_gather_forms(mupdf.PdfDocument doc, mupdf.PdfObj dict_, List<object> formlist, int stream_xref)
        {
            if (dict_.m_internal == null || mupdf.mupdf.pdf_is_dict(dict_) == 0)
                return;
            int n = mupdf.mupdf.pdf_dict_len(dict_);
            for (int i = 0; i < n; i++)
            {
                var refname = Helpers.PdfDictGetKey(dict_, i);
                var formdict = Helpers.PdfDictGetVal(dict_, i);
                if (mupdf.mupdf.pdf_is_dict(formdict) == 0)
                {
                    mupdf.mupdf.fz_warn($"'{mupdf.mupdf.pdf_to_name(refname)}' is no form dict ({mupdf.mupdf.pdf_to_num(formdict)} 0 R)");
                    continue;
                }
                var type_ = Helpers.PdfDictGet(formdict, mupdf.mupdf.pdf_new_name("Subtype"));
                if (mupdf.mupdf.pdf_name_eq(type_, mupdf.mupdf.pdf_new_name("Form")) == 0)
                    continue;
                var o = Helpers.PdfDictGet(formdict, mupdf.mupdf.pdf_new_name("BBox"));
                var m = Helpers.PdfDictGet(formdict, mupdf.mupdf.pdf_new_name("Matrix"));
                mupdf.FzMatrix mat;
                if (m.m_internal != null)
                    mat = mupdf.mupdf.pdf_to_matrix(m);
                else
                    mat = new mupdf.FzMatrix();
                mupdf.FzRect bbox;
                if (o.m_internal != null)
                    bbox = mupdf.mupdf.fz_transform_rect(mupdf.mupdf.pdf_to_rect(o), mat);
                else
                    bbox = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_INFINITE);
                int xref = mupdf.mupdf.pdf_to_num(formdict);
                var bboxRect = new Rect(bbox);
                formlist.Add((xref, mupdf.mupdf.pdf_to_name(refname) ?? "", stream_xref, bboxRect));
            }
        }

        private static string JM_EscapeStrFromStr(string c)
        {
            if (c == null)
                return "";
            var b = Encoding.UTF8.GetBytes(c);
            var ret = new StringBuilder(b.Length);
            foreach (var bb in b)
                ret.Append((char)bb);
            return ret.ToString();
        }

        internal string JM_get_fontextension(int xref)
        {
            var doc = NativePdfDocument;
            if (xref < 1)
                return "n/a";
            var o = doc.pdf_load_object(xref);
            var desft = Helpers.PdfDictGet(o, mupdf.mupdf.pdf_new_name("DescendantFonts"));
            mupdf.PdfObj fd;
            if (desft.m_internal != null)
            {
                var first = mupdf.mupdf.pdf_resolve_indirect(mupdf.mupdf.pdf_array_get(desft, 0));
                fd = Helpers.PdfDictGet(first, mupdf.mupdf.pdf_new_name("FontDescriptor"));
            }
            else
                fd = Helpers.PdfDictGet(o, mupdf.mupdf.pdf_new_name("FontDescriptor"));
            if (fd.m_internal == null)
                return "n/a";

            var ff = Helpers.PdfDictGet(fd, mupdf.mupdf.pdf_new_name("FontFile"));
            if (ff.m_internal != null)
                return "pfa";
            ff = Helpers.PdfDictGet(fd, mupdf.mupdf.pdf_new_name("FontFile2"));
            if (ff.m_internal != null)
                return "ttf";
            ff = Helpers.PdfDictGet(fd, mupdf.mupdf.pdf_new_name("FontFile3"));
            if (ff.m_internal != null)
            {
                var subt = Helpers.PdfDictGet(ff, mupdf.mupdf.pdf_new_name("Subtype"));
                if (subt.m_internal != null && mupdf.mupdf.pdf_is_name(subt) == 0)
                {
                    Helpers.message("invalid font descriptor subtype");
                    return "n/a";
                }
                if (mupdf.mupdf.pdf_name_eq(subt, mupdf.mupdf.pdf_new_name("Type1C")) != 0)
                    return "cff";
                if (mupdf.mupdf.pdf_name_eq(subt, mupdf.mupdf.pdf_new_name("CIDFontType0C")) != 0)
                    return "cid";
                if (mupdf.mupdf.pdf_name_eq(subt, mupdf.mupdf.pdf_new_name("OpenType")) != 0)
                    return "otf";
                if (subt.m_internal != null && mupdf.mupdf.pdf_is_name(subt) != 0)
                    Helpers.message($"unhandled font type '{mupdf.mupdf.pdf_to_name(subt)}'");
            }
            return "n/a";
        }
        /// <summary>
        /// Lists images referenced on a page.
        /// </summary>
        /// <remarks>PDF only: Return a list of all images (directly or indirectly) referenced by the page. PyMuPDF <c>Document.get_page_images</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">page number, 0-based, `-∞ &lt; pno &lt; page_count`.</param>
        /// <param name="full">whether to also include the referencer's xref (which is zero if this is the page).</param>
        /// <returns>a list of images referenced by this page. Each item looks like:</returns>
        public List<Entry> GetPageImages(int pno, bool full = false)
        {
            var rows = GetPageImageRows(pno, full);
            var result = new List<Entry>(rows.Count);
            foreach (var row in rows)
            {
                var entry = EntryFromImageRow(row);
                if (!full)
                    entry.StreamXref = 0;
                result.Add(entry);
            }
            return result;
        }

        internal List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)> GetPageImageRows(int pno, bool full = false)
        {
            var pageInfo = _getPageInfo(pno, 2);
            var result = new List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)>(pageInfo.Count);
            foreach (var item in pageInfo)
            {
                var t = ((int xref, int gen, int width, int height, int bpc, string colorspace, string altCs, string name, string filter, int streamXref))item;
                string sm = t.gen.ToString(System.Globalization.CultureInfo.InvariantCulture);
                result.Add((t.xref, sm, t.width, t.height, t.bpc, t.colorspace, t.altCs, t.name, t.filter));
            }
            return result;
        }

        internal static Entry EntryFromImageRow((int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter) row)
        {
            int smask = 0;
            if (!string.IsNullOrEmpty(row.smask))
                int.TryParse(row.smask, out smask);
            return new Entry
            {
                Xref = row.xref,
                Smask = smask,
                Width = row.width,
                Height = row.height,
                Bpc = row.bpc,
                CsName = row.colorspace,
                AltCsName = row.altCs,
                Name = row.name,
                Filter = row.filter,
            };
        }

        internal List<(int xref, string ext, string type, string baseName, string name, string encoding)> GetPageFontsCore(int pno, bool full = false)
            => UnboxFontRowsFromPageInfo(_getPageInfo(pno, 1)).ConvertAll(t => (t.xref, t.ext, t.type, t.baseName, t.name, t.encoding));

        internal List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)> GetPageImagesCore(int pno, bool full = false)
            => GetPageImageRows(pno, full);

        /// <summary>PyMuPDF <c>Document.page_annot_xrefs</c> → <c>JM_get_annot_xref_list</c> on the page object.</summary>
        /// <summary>Annotation xrefs on a page (PyMuPDF <c>Document.page_annot_xrefs</c>).</summary>
        public List<(int xref, AnnotationType type, string id)> GetPageAnnotXrefs(int n)
        {
            if (!IsPdf)
                throw new ValueErrorException("is no PDF");
            int pageCount = PageCount;
            while (n < 0)
                n += pageCount;
            if (n >= pageCount)
                throw new ValueErrorException(Constants.MSG_BAD_PAGENO);
            var pageObj = mupdf.mupdf.pdf_lookup_page_obj(NativePdfDocument, n);
            var result = new List<(int xref, AnnotationType type, string id)>();
            foreach (var item in Helpers.JM_get_annot_xref_list(pageObj))
                result.Add((item.xref, (AnnotationType)item.type_, item.nm));
            return result;
        }
        /// <summary>
        /// See PyMuPDF Document.page_annot_xrefs.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.page_annot_xrefs</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="n">See PyMuPDF parameter &lt;c&gt;n&lt;/c&gt;.</param>
        /// <returns>A list of results.</returns>
        public List<AnnotXref> PageAnnotXrefs(int n)
        {
            var items = GetPageAnnotXrefs(n);
            var result = new List<AnnotXref>(items.Count);
            foreach (var (xref, type, id) in items)
            {
                result.Add(new AnnotXref
                {
                    Xref = xref,
                    AnnotType = (PdfAnnotType)(int)type,
                    Id = id,
                });
            }
            return result;
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

            var basefont = Helpers.PdfDictGets(obj, "BaseFont");
            if (basefont.m_internal != null) name = mupdf.mupdf.pdf_to_name(basefont);

            var subtype = Helpers.PdfDictGets(obj, "Subtype");
            if (subtype.m_internal != null) type = mupdf.mupdf.pdf_to_name(subtype);

            ext = JM_get_fontextension(xref);
            content = Helpers.JM_get_fontbuffer(pdf, xref) ?? Array.Empty<byte>();
            return (name, ext, type, content);
        }

        internal object[] CheckFontInfo(int xref)
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

        /// <summary>PyMuPDF <c>CheckFontInfo(doc, xref)</c> fontdict for <see cref="Shape"/> text insertion.</summary>
        internal Dictionary<string, object> GetFontDictForXref(int xref)
        {
            var fi = CheckFontInfo(xref);
            return fi?[1] as Dictionary<string, object>;
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

        private static List<(int glyph, float width)> BuildCharWidths(mupdf.FzFont font, int limit)
        {
            int mylimit = limit < 256 ? 256 : limit;
            var wlist = new List<(int glyph, float width)>(mylimit);
            for (int i = 0; i < mylimit; i++)
            {
                int glyph = font.fz_encode_character(i);
                float adv = font.fz_advance_glyph(glyph, 0);
                if (glyph > 0)
                    wlist.Add((glyph, adv));
                else
                    wlist.Add((glyph, 0.0f));
            }
            return wlist;
        }

        /// <summary>
        /// Load an <see cref="mupdf.FzFont"/> for width/glyph tables (PyMuPDF
        /// <c>Document._get_char_widths</c>: Base-14 lookup, then <c>JM_get_fontbuffer</c>).
        /// </summary>
        internal static mupdf.FzFont LoadFzFontForCharWidths(
            Document doc, int xref, string pdfBaseFontName, byte[] content, int idx = 0,
            string fontfile = null)
        {
            if (content != null && content.Length > 0)
            {
                var buf = Helpers.BufferFromBytes(content);
                return new mupdf.FzFont(null, buf, idx, 0);
            }

            if (!string.IsNullOrEmpty(fontfile))
                return mupdf.mupdf.fz_new_font_from_file(null, fontfile, idx, 0);

            string loadName = Font.NormalizeBase14FontName(
                string.IsNullOrEmpty(pdfBaseFontName) ? "helv" : pdfBaseFontName);
            if (Constants.Base14FontDict.TryGetValue(loadName.ToLowerInvariant(), out string mapped))
            {
                using (var b14Out = new mupdf.ll_fz_lookup_base14_font_outparams())
                {
                    var data = mupdf.mupdf.ll_fz_lookup_base14_font_outparams_fn(mapped, b14Out);
                    int size = b14Out.len;
                    if (data != null && size > 0)
                        return mupdf.mupdf.fz_new_font_from_memory(mapped, data, size, 0, 0);
                }
            }

            if (doc != null && xref > 0)
            {
                byte[] fb = Helpers.JM_get_fontbuffer(doc.NativePdfDocument, xref);
                if (fb != null && fb.Length > 0)
                {
                    var buf = Helpers.BufferFromBytes(fb);
                    return new mupdf.FzFont(null, buf, idx, 0);
                }
            }

            if (Constants.Base14FontDict.TryGetValue(loadName.ToLowerInvariant(), out mapped))
                loadName = mapped;
            return Helpers.JM_get_font(loadName, null, null, 0, 0, -1, 0, 0, 0, 0);
        }

        /// <summary>
        /// Get list of glyph / width data for a font xref.
        /// Port of Python Document.get_char_widths().
        /// </summary>
        public List<(int glyph, float width)> GetCharWidths(int xref, int limit = 256, int idx = 0, Dictionary<string, object> fontdict = null)
        {
            EnsurePdf();
            if (!XrefIsFont(xref))
                throw new ArgumentException("xref is not a font");

            var fontinfo = CheckFontInfo(xref);
            if (fontinfo == null)
            {
                var ef = ExtractFont(xref);
                bool hasStream = ef.content != null && ef.content.Length > 0;
                string name;
                string ext;
                string stype;
                if (fontdict == null)
                {
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

                // No embedded font stream: widths are built via FzFont(BaseFont name). Base-14 / simple Type1
                // objects typically have empty ext but a valid BaseFont (PyMuPDF allows this path).
                string loadName = string.IsNullOrEmpty(name) ? ef.name : name;
                if (!hasStream && string.IsNullOrEmpty(loadName))
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

            List<(int glyph, float width)> glyphs = null;
            if (fontdict.ContainsKey("glyphs") && fontdict["glyphs"] is List<(int glyph, float width)>)
                glyphs = (List<(int glyph, float width)>)fontdict["glyphs"];

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
            string pdfName = fontdict.ContainsKey("name") ? (fontdict["name"]?.ToString() ?? "") : "";
            if (string.IsNullOrEmpty(pdfName))
                pdfName = ef2.name;
            byte[] streamContent = ef2.content;
            if ((streamContent == null || streamContent.Length == 0)
                && fontdict.TryGetValue("content", out var cachedContent)
                && cachedContent is byte[] cachedBytes
                && cachedBytes.Length > 0)
            {
                streamContent = cachedBytes;
            }
            string fontfile = fontdict.TryGetValue("fontfile", out var ffObj) ? ffObj?.ToString() : null;
            var font = LoadFzFontForCharWidths(this, xref, pdfName, streamContent, idx, fontfile);
            // Only dispose fonts loaded from an embedded stream; Base-14 fonts from
            // JM_get_font may be shared and must not be destroyed here.
            bool disposeFont = (streamContent != null && streamContent.Length > 0)
                || !string.IsNullOrEmpty(fontfile);
            try
            {
                glyphs = BuildCharWidths(font, mylimit);
            }
            finally
            {
                if (disposeFont)
                    font?.Dispose();
            }
            fontdict["glyphs"] = glyphs;
            fontinfo[1] = fontdict;
            UpdateFontInfo(fontinfo);
            return glyphs;
        }
        /// <summary>
        /// Extracts image information by xref.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.extract_image</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public ImageInfo ExtractImage(int xref) => ExtractImageDict(xref);

        /// <summary>PyMuPDF-shaped dictionary result for <c>extract_image</c>.</summary>
        internal Dictionary<string, object> ExtractImageDict(int xref)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");

            var pdf = NativePdfDocument;

            if (xref < 1 || xref > mupdf.mupdf.pdf_xref_len(pdf) - 1)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);

            var obj = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
            var subtype = Helpers.PdfDictGet(obj, mupdf.mupdf.pdf_new_name("Subtype"));
            if (subtype.m_internal == null || !string.Equals(mupdf.mupdf.pdf_to_name(subtype), "Image", StringComparison.Ordinal))
                throw new ValueErrorException("not an image");

            var o = Helpers.PdfDictGeta(obj, mupdf.mupdf.pdf_new_name("SMask"), mupdf.mupdf.pdf_new_name("Mask"));
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

        /// <summary>PyMuPDF <c>JM_image_extension</c>.</summary>
        private static string JMImageExtension(int type)
        {
            if (type == mupdf.mupdf.FZ_IMAGE_FAX) return "fax";
            if (type == mupdf.mupdf.FZ_IMAGE_RAW) return "raw";
            if (type == mupdf.mupdf.FZ_IMAGE_FLATE) return "flate";
            if (type == mupdf.mupdf.FZ_IMAGE_LZW) return "lzw";
            if (type == mupdf.mupdf.FZ_IMAGE_RLD) return "rld";
            if (type == mupdf.mupdf.FZ_IMAGE_BMP) return "bmp";
            if (type == mupdf.mupdf.FZ_IMAGE_GIF) return "gif";
            if (type == mupdf.mupdf.FZ_IMAGE_JBIG2) return "jb2";
            if (type == mupdf.mupdf.FZ_IMAGE_JPEG) return "jpeg";
            if (type == mupdf.mupdf.FZ_IMAGE_JPX) return "jpx";
            if (type == mupdf.mupdf.FZ_IMAGE_JXR) return "jxr";
            if (type == mupdf.mupdf.FZ_IMAGE_PNG) return "png";
            if (type == mupdf.mupdf.FZ_IMAGE_PNM) return "pnm";
            if (type == mupdf.mupdf.FZ_IMAGE_TIFF) return "tiff";
            return "n/a";
        }

        // ─── Search ─────────────────────────────────────────────────────
        /// <summary>
        /// search for a string on a page
        /// </summary>
        /// <remarks>Search for "text" on page number "pno". Works exactly like the corresponding <see cref="Page.SearchFor"/>. Any integer <c>-∞ &lt; pno &lt; page_count</c> is acceptable. PyMuPDF <c>Document.search_page_for</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">0-based page number. Negative values wrap from the end of the document.</param>
        /// <param name="needle">Text string to search for.</param>
        /// <param name="maxHits">Maximum number of search hits to return.</param>
        /// <param name="clip">Clip rectangle in page coordinates.</param>
        /// <param name="flags">Text search or extraction flags (see PyMuPDF text flags).</param>
        /// <param name="textpage">Optional reused TextPage for faster repeated searches.</param>
        /// <returns>A list of results.</returns>
        public List<Quad> SearchPageFor(int pno, string needle, int maxHits = 16, Quad clip = null, int flags = 0, TextPage textpage = null)
        {
            using var page = LoadPage(pno);
            return page.SearchFor(needle, clip, maxHits, flags, textpage);
        }
        /// <summary>
        /// search for a string on a page
        /// </summary>
        /// <remarks>Search for "text" on page number "pno". Works exactly like the corresponding <see cref="Page.SearchFor"/>. Any integer <c>-∞ &lt; pno &lt; page_count</c> is acceptable. PyMuPDF <c>Document.search_page_for</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">0-based page number. Negative values wrap from the end of the document.</param>
        /// <param name="needle">Text string to search for.</param>
        /// <param name="maxHits">Maximum number of search hits to return.</param>
        /// <param name="clip">Clip rectangle in page coordinates.</param>
        /// <param name="flags">Text search or extraction flags (see PyMuPDF text flags).</param>
        /// <param name="textpage">Optional reused TextPage for faster repeated searches.</param>
        /// <returns>A list of results.</returns>
        public List<Rect> SearchPageForRects(int pno, string needle, int maxHits = 16, Quad clip = null, int flags = 0, TextPage textpage = null)
        {
            using var page = LoadPage(pno);
            return page.SearchForRects(needle, clip, maxHits, flags, textpage);
        }

        // ─── Page Pixmap / Text convenience ─────────────────────────────
        /// <summary>
        /// create a pixmap of a page by page number
        /// </summary>
        /// <remarks>Creates a pixmap from page *pno* (zero-based). Invokes <see cref="Page.GetPixmap"/>. PyMuPDF <c>Document.get_page_pixmap</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">page number, 0-based in `-∞ &lt; pno &lt; page_count`.</param>
        /// <param name="matrix">Transformation matrix applied when rendering.</param>
        /// <param name="cs">Target colorspace for rendering.</param>
        /// <param name="alpha">Whether to include an alpha channel in the pixmap.</param>
        /// <param name="clip">Clip rectangle in page coordinates.</param>
        public Pixmap GetPagePixmap(int pno, Matrix matrix = null, Colorspace cs = null, bool alpha = false, IRect clip = null)
        {
            using var page = LoadPage(pno);
            return page.GetPixmap(matrix, cs, clip, alpha);
        }
        /// <summary>
        /// extract the text of a page by page number
        /// </summary>
        /// <remarks>Extracts the text of a page given its page number *pno* (zero-based). Invokes <see cref="Page.GetText"/>. PyMuPDF <c>Document.get_page_text</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">page number, 0-based, any value `-∞ &lt; pno &lt; page_count`.</param>
        /// <param name="option">Text extraction option (text, blocks, html, etc.).</param>
        /// <param name="flags">Text search or extraction flags (see PyMuPDF text flags).</param>
        public object GetPageText(int pno, string option = "text", int? flags = null)
        {
            using var page = LoadPage(pno);
            return page.GetText(option, flags: flags);
        }
        /// <summary>
        /// Lists Form XObjects referenced on a page.
        /// </summary>
        /// <remarks>PDF only: Return a list of all XObjects referenced by a page. PyMuPDF <c>Document.get_page_xobjects</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">page number, 0-based, `-∞ &lt; pno &lt; page_count`.</param>
        /// <returns>a list of (non-image) XObjects. These objects typically represent pages *embedded* (not copied) from other PDFs. For example, <see cref="Page.ShowPdfPage"/> will create this type of object. An item of this list has the following layout: <c>(xref, name, invoker, bbox)</c>, where</returns>
        public List<Dictionary<string, object>> GetPageXobjects(int pno)
        {
            var result = new List<Dictionary<string, object>>();
            if (!IsPdf)
                return result;

            using var page = LoadPage(pno);
            var pageObj = page.NativePdfPage.obj();
            var resources = Helpers.PdfDictGet(pageObj, mupdf.mupdf.pdf_new_name("Resources"));
            if (resources.m_internal == null)
                return result;

            var xobjects = Helpers.PdfDictGet(resources, mupdf.mupdf.pdf_new_name("XObject"));
            if (xobjects.m_internal == null || mupdf.mupdf.pdf_is_dict(xobjects) == 0)
                return result;

            int n = mupdf.mupdf.pdf_dict_len(xobjects);
            for (int i = 0; i < n; i++)
            {
                var key = Helpers.PdfDictGetKey(xobjects, i);
                var val = Helpers.PdfDictGetVal(xobjects, i);
                var resolved = mupdf.mupdf.pdf_resolve_indirect(val);

                string name = mupdf.mupdf.pdf_to_name(key);
                int xref = mupdf.mupdf.pdf_to_num(val);
                string subtype = "";
                var subtypeObj = Helpers.PdfDictGet(resolved, mupdf.mupdf.pdf_new_name("Subtype"));
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
        /// Gets page numbers that use a given label.
        /// </summary>
        /// <remarks>PDF only: Return a list of page numbers that have the specified label -- note that labels may not be unique in a PDF. This implies a sequential search through all page numbers to compare their labels. PyMuPDF <c>Document.get_page_numbers</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="label">the label to look for, e.g. "vii" (Roman number 7).</param>
        /// <param name="onlyOne">See PyMuPDF parameter &lt;c&gt;onlyOne&lt;/c&gt;.</param>
        /// <returns>list of page numbers that have this label. Empty if none found, no labels defined, etc.</returns>
        public List<int> GetPageNumbers(string label, bool onlyOne = false)
        {
            // Jorj McKie, 2021-01-06
            var numbers = new List<int>();
            // if not label: return numbers
            if (string.IsNullOrEmpty(label))
                return numbers;

            // labels = doc._get_page_labels()
            var labels = _get_page_labels();
            // if labels == []: return numbers
            if (labels.Count == 0)
                return numbers;
            // for i in range(doc.page_count):
            for (int i = 0; i < PageCount; i++)
            {
                // plabel = utils.get_label_pno(i, labels)
                string plabel = Utils.GetLabelPno(i, labels);
                // if plabel == label:
                if (plabel == label)
                {
                    // numbers.append(i)
                    numbers.Add(i);
                    // if only_one: break
                    if (onlyOne)
                        break;
                }
            }

            return numbers;
        }

        /// <summary>PyMuPDF <c>Document._get_page_labels</c>.</summary>
        internal List<(int pno, string rule)> _get_page_labels()
        {
            // pdf = _as_pdf_document(self)
            var pdf = Helpers.AsPdfDocument(this, required: true);
            var rc = new List<(int pno, string rule)>();
            // pagelabels = mupdf.pdf_new_name("PageLabels")
            var pagelabels = mupdf.mupdf.pdf_new_name("PageLabels");
            // obj = mupdf.pdf_dict_getl( mupdf.pdf_trailer(pdf), PDF_NAME('Root'), pagelabels)
            var obj = Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"),
                pagelabels);
            // if not obj.m_internal:
            if (obj.m_internal == null)
                // return rc
                return rc;
            // simple case: direct /Nums object
            // nums = mupdf.pdf_resolve_indirect( Helpers.PdfObjDictGet(mupdf, obj, PDF_NAME('Nums')))
            var nums = mupdf.mupdf.pdf_resolve_indirect(
                Helpers.PdfDictGet(obj, mupdf.mupdf.pdf_new_name("Nums")));
            // if nums.m_internal:
            if (nums.m_internal != null)
            {
                // JM_get_page_labels(rc, nums)
                Helpers.JmGetPageLabels(rc, nums);
                // return rc
                return rc;
            }
            // case: /Kids/Nums
            // nums = mupdf.pdf_resolve_indirect( mupdf.pdf_dict_getl(obj, PDF_NAME('Kids'), PDF_NAME('Nums')))
            nums = mupdf.mupdf.pdf_resolve_indirect(
                Helpers.PdfDictGetl(obj, mupdf.mupdf.pdf_new_name("Kids"), mupdf.mupdf.pdf_new_name("Nums")));
            // if nums.m_internal:
            if (nums.m_internal != null)
            {
                // JM_get_page_labels(rc, nums)
                Helpers.JmGetPageLabels(rc, nums);
                // return rc
                return rc;
            }
            // case: /Kids is an array of multiple /Nums
            // kids = mupdf.pdf_resolve_indirect( Helpers.PdfObjDictGet(mupdf, obj, PDF_NAME('Kids')))
            var kids = mupdf.mupdf.pdf_resolve_indirect(
                Helpers.PdfDictGet(obj, mupdf.mupdf.pdf_new_name("Kids")));
            // if not kids.m_internal or not mupdf.pdf_is_array(kids):
            if (kids.m_internal == null || mupdf.mupdf.pdf_is_array(kids) == 0)
                // return rc
                return rc;
            // n = mupdf.pdf_array_len(kids)
            int n = kids.pdf_array_len();
            // for i in range(n):
            for (int i = 0; i < n; i++)
            {
                // nums = mupdf.pdf_resolve_indirect(
                //         Helpers.PdfObjDictGet(mupdf,
                //             mupdf.pdf_array_get(kids, i),
                //             PDF_NAME('Nums'),
                //             )
                //         )
                nums = mupdf.mupdf.pdf_resolve_indirect(
                    Helpers.PdfDictGet(
                        kids.pdf_array_get(i),
                        mupdf.mupdf.pdf_new_name("Nums")));
                // JM_get_page_labels(rc, nums)
                Helpers.JmGetPageLabels(rc, nums);
            }
            // return rc
            return rc;
        }
        /// <summary>
        /// Gets PDF page label definitions.
        /// </summary>
        /// <remarks>PDF only: Extract the list of page label definitions. Typically used for modifications before feeding it into <see cref="SetPageLabels"/>. PyMuPDF <c>Document.get_page_labels</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns>a list of dictionaries as defined in <see cref="SetPageLabels"/>.</returns>
        public List<Dictionary<string, object>> GetPageLabels()
        {
            // Jorj McKie, 2021-01-10
            // return [utils.rule_dict(item) for item in self._get_page_labels()]
            var result = new List<Dictionary<string, object>>();
            if (!IsPdf)
                return result;
            foreach (var item in _get_page_labels())
                result.Add(Utils.RuleDict(item));
            return result;
        }

        /// <summary>PyMuPDF <c>Document._set_page_labels</c>.</summary>
        internal void _set_page_labels(string labels)
        {
            // pdf = _as_pdf_document(self)
            var pdf = Helpers.AsPdfDocument(this, required: true);
            // pagelabels = mupdf.pdf_new_name("PageLabels")
            var pagelabels = mupdf.mupdf.pdf_new_name("PageLabels");
            // root = Helpers.PdfObjDictGet(mupdf,mupdf.pdf_trailer(pdf), PDF_NAME('Root'))
            var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            // mupdf.pdf_dict_del(root, pagelabels)
            mupdf.mupdf.pdf_dict_del(root, pagelabels);
            // mupdf.pdf_dict_putl(root, mupdf.pdf_new_array(pdf, 0), pagelabels, PDF_NAME('Nums'))
            Helpers.PdfDictPutl(
                pdf,
                root,
                mupdf.mupdf.pdf_new_array(pdf, 0),
                pagelabels,
                mupdf.mupdf.pdf_new_name("Nums"));

            // xref = self.pdf_catalog()
            int xref = pdf_catalog();
            // text = self.xref_object(xref, compressed=True)
            string text = xref_object(xref, compressed: true);
            // text = text.replace("/Nums[]", f"/Nums[{labels}]")
            text = text.Replace("/Nums[]", $"/Nums[{labels}]");
            // self.UpdateObject(xref, text)
            UpdateObject(xref, text);
        }
        /// <summary>
        /// PDF only: add/update page label definitions
        /// </summary>
        /// <remarks>PDF only: Add or update the page label definitions of the PDF. PyMuPDF <c>Document.set_page_labels</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="List<Dictionary<string">See PyMuPDF parameter &lt;c&gt;List&lt;Dictionary&lt;string&lt;/c&gt;.</param>
        /// <param name="labels">a list of dictionaries. Each dictionary defines a label building rule and a 0-based "start" page number. That start page is the first for which the label definition is valid. Each dictionary has up to 4 items and looks like `{'startpage': int, 'prefix': str, 'style': str, 'firstpagenum': int}` and has the following items.</param>
        public void SetPageLabels(List<Dictionary<string, object>> labels)
        {
            // """Add / replace page label definitions in PDF document.
            //
            // Args:
            //     doc: PDF document (resp. 'self').
            //     labels: list of label dictionaries like:
            //     {'startpage': int, 'prefix': str, 'style': str, 'firstpagenum': int},
            //     as returned by get_page_labels().
            // """
            // William Chapman, 2021-01-06

            // def create_label_str(label):
            string create_label_str(Dictionary<string, object> label)
            {
                // """Convert Python label dict to corresponding PDF rule string.
                //
                // Args:
                //     label: (dict) build rule for the label.
                // Returns:
                //     PDF label rule string wrapped in "<<", ">>".
                // """
                // s = f"{label['startpage']}<<"
                string s = Convert.ToInt32(label["startpage"]) + "<<";
                // if label.get("prefix", "") != "":
                if (((label.ContainsKey("prefix") ? label["prefix"]?.ToString() : null) ?? "") != "")
                    // s += f"/P({label['prefix']})"
                    s += "/P(" + label["prefix"] + ")";
                // if label.get("style", "") != "":
                if (((label.ContainsKey("style") ? label["style"]?.ToString() : null) ?? "") != "")
                    // s += f"/S/{label['style']}"
                    s += "/S/" + label["style"];
                // if label.get("firstpagenum", 1) > 1:
                if ((label.ContainsKey("firstpagenum") ? Convert.ToInt32(label["firstpagenum"]) : 1) > 1)
                    // s += f"/St {label['firstpagenum']}"
                    s += "/St " + label["firstpagenum"];
                // s += ">>"
                s += ">>";
                // return s
                return s;
            }

            // def create_nums(labels):
            string create_nums(List<Dictionary<string, object>> labelList)
            {
                // """Return concatenated string of all labels rules.
                //
                // Args:
                //     labels: (list) dictionaries as created by function 'rule_dict'.
                // Returns:
                //     PDF compatible string for page label definitions, ready to be
                //     enclosed in PDF array 'Nums[...]'.
                // """
                // labels.sort(key=lambda x: x["startpage"])
                labelList.Sort((a, b) => Convert.ToInt32(a["startpage"]).CompareTo(Convert.ToInt32(b["startpage"])));
                // s = "".join([create_label_str(label) for label in labels])
                string s = string.Concat(labelList.Select(create_label_str));
                // return s
                return s;
            }

            // doc._set_page_labels(create_nums(labels))
            _set_page_labels(create_nums(labels));
        }

        // ─── Layout ─────────────────────────────────────────────────────
        /// <summary>
        /// Re-layouts a reflowable document to new dimensions.
        /// </summary>
        /// <remarks>Re-paginate ("reflow") the document based on the given page dimension and fontsize. This only affects some document types like e-books and HTML. Ignored if not supported. Supported documents have <see langword="true"/> in property <see cref="IsReflowable"/>. PyMuPDF <c>Document.layout</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="width">use it together with <c>height</c> as alternative to <c>rect</c>.</param>
        /// <param name="height">use it together with <c>width</c> as alternative to <c>rect</c>.</param>
        /// <param name="fontsize">the desired default fontsize.</param>
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
        /// Re-layouts a reflowable document to new dimensions.
        /// </summary>
        /// <remarks>Re-paginate ("reflow") the document based on the given page dimension and fontsize. This only affects some document types like e-books and HTML. Ignored if not supported. Supported documents have <see langword="true"/> in property <see cref="IsReflowable"/>. PyMuPDF <c>Document.layout</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="rect">desired page size. Must be finite, not empty and start at point (0, 0).</param>
        /// <param name="fontsize">the desired default fontsize.</param>
        public void Layout(Rect rect, float fontsize = 11)
        {
            Layout((float)rect.Width, (float)rect.Height, fontsize);
        }

        // ─── Journal ────────────────────────────────────────────────────
        /// <summary>
        /// PDF only: enables journalling for the document
        /// </summary>
        /// <remarks>PDF only: Enable journalling. Use this before you start logging operations. PyMuPDF <c>Document.journal_enable</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void JournalEnable()
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            var pdf = NativePdfDocument;
            mupdf.mupdf.pdf_enable_journal(pdf);
        }
        /// <summary>
        /// Gets Gets whether PDF journalling is enabled.
        /// </summary>
        /// <value>Gets whether PDF journalling is enabled.</value>
        /// <remarks>PyMuPDF <c>Document.journal_is_enabled</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
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
        /// PDF only: undo current operation
        /// </summary>
        /// <remarks>PDF only: Revert (undo) the current step in the journal. This moves towards the journal's top. PyMuPDF <c>Document.journal_undo</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void JournalUndo()
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            mupdf.mupdf.pdf_undo(NativePdfDocument);
        }
        /// <summary>
        /// PDF only: redo current operation
        /// </summary>
        /// <remarks>PDF only: Re-apply (redo) the current step in the journal. This moves towards the journal's bottom. PyMuPDF <c>Document.journal_redo</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void JournalRedo()
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            mupdf.mupdf.pdf_redo(NativePdfDocument);
        }
        /// <summary>
        /// Starts a named journalling operation (PDF undo stack).
        /// </summary>
        /// <remarks>PDF only: Start journalling an *"operation"* identified by a string "name". Updates will fail for a journal-enabled PDF, if no operation has been started. PyMuPDF <c>Document.journal_start_op</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="name">Name string (layer, OCG, embedded file, etc.).</param>
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
        /// PDF only: end current operation
        /// </summary>
        /// <remarks>PDF only: Stop the current operation. The updates between start and stop of an operation belong to the same unit of work and will be undone / redone together. PyMuPDF <c>Document.journal_stop_op</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void JournalStopOp()
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            mupdf.mupdf.pdf_end_operation(NativePdfDocument);
        }
        /// <summary>
        /// PDF only: save journal to a file
        /// </summary>
        /// <remarks>PDF only: Save the journal to a file. PyMuPDF <c>Document.journal_save</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="filename">either a filename as string or a file object opened as "wb" (or an `io.BytesIO()` object).</param>
        public void JournalSave(string filename)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            mupdf.mupdf.pdf_save_journal(NativePdfDocument, filename);
        }
        /// <summary>
        /// PDF only: load journal from a file
        /// </summary>
        /// <remarks>PDF only: Load journal from a file. Enables journalling for the document. If journalling is already enabled, an exception is raised. PyMuPDF <c>Document.journal_load</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="filename">the filename (str) of the journal or a file object opened as "rb" (or an `io.BytesIO()` object).</param>
        public void JournalLoad(string filename)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            mupdf.mupdf.pdf_load_journal(NativePdfDocument, filename);
            if (!JournalIsEnabled)
                throw new FileDataException("Journal and document do not match");
        }
        /// <summary>
        /// PDF only: load journal from a file
        /// </summary>
        /// <remarks>PDF only: Load journal from a file. Enables journalling for the document. If journalling is already enabled, an exception is raised. PyMuPDF <c>Document.journal_load</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="data">See PyMuPDF parameter &lt;c&gt;data&lt;/c&gt;.</param>
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
        /// PDF only: return name of a journalling step
        /// </summary>
        /// <remarks>PDF only: Return the name of operation number *step.* PyMuPDF <c>Document.journal_op_name</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="step">See PyMuPDF parameter &lt;c&gt;step&lt;/c&gt;.</param>
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
        /// Repairs PDF structure issues.
        /// </summary>
        /// <remarks>Repair document. PyMuPDF <c>Document.repair</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void Repair()
        {
            var pdf = Helpers.AsPdfDocument(NativeDocument, required: false);
            if (pdf != null && pdf.m_internal != null)
                mupdf.mupdf.pdf_check_document(pdf);
        }

        // ─── Insert PDF ─────────────────────────────────────────────────
        /// <summary>
        /// Copies a page range from another PDF into this document.
        /// </summary>
        /// <remarks>PDF only: Copy the page range [from_page, to_page] (including both) of PDF document *docsrc* into the current one. Inserts will start with page number *start_at*. Value -1 indicates default values. All pages thus copied will be rotated as specified. Links, annotations and widgets can be excluded in the target, see below. All page numbers are 0-based. PyMuPDF <c>Document.insert_pdf</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="docsrc">Source PDF document to copy pages from.</param>
        /// <param name="fromPage">First source page number (0-based, inclusive).</param>
        /// <param name="toPage">Last source page number (0-based, inclusive).</param>
        /// <param name="startAt">See PyMuPDF parameter &lt;c&gt;startAt&lt;/c&gt;.</param>
        /// <param name="rotate">All copied pages will be rotated by the provided value (degrees, integer multiple of 90).</param>
        /// <param name="links">Choose whether (internal and external) links should be included in the copy. Default is <c>True</c>. *Named* links (LINK_NAMED) and internal links to outside the copied page range are always excluded.</param>
        /// <param name="annots">choose whether annotations should be included in the copy.</param>
        /// <param name="widgets">choose whether annotations should be included in the copy. If `True` and at least one of the source pages contains form fields, the target PDF will be turned into a Form PDF (if not already being one).</param>
        /// <param name="joinDuplicates">See PyMuPDF parameter &lt;c&gt;joinDuplicates&lt;/c&gt;.</param>
        /// <param name="showProgress">See PyMuPDF parameter &lt;c&gt;showProgress&lt;/c&gt;.</param>
        /// <param name="final">*(new in v1.18.0)* controls whether the list of already copied objects should be dropped after this method, default <see langword="true"/>. Set it to 0 except for the last one of multiple insertions from the same source PDF. This saves target file size and speeds up execution considerably.</param>
        /// <param name="gmap">See PyMuPDF parameter &lt;c&gt;gmap&lt;/c&gt;.</param>
        /// <exception cref="ValueErrorException">Document is closed, encrypted, or arguments are invalid.</exception>
        public void InsertPdf(
            Document docsrc,
            int fromPage = -1,
            int toPage = -1,
            int startAt = -1,
            int rotate = -1,
            bool links = true,
            bool annots = true,
            bool widgets = true,
            bool joinDuplicates = false,
            int showProgress = 0,
            int final = 1,
            Graftmap gmap = null)
        {
            // Insert pages from a source PDF into this PDF.
            // For reconstructing the links (_do_links method), we must save the
            // insertion point (start_at) if it was specified as -1.
            //log( 'insert_pdf(): start')
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            if (docsrc == null)
                throw new ArgumentNullException(nameof(docsrc));
            if (_graftId == docsrc._graftId)
                throw new ValueErrorException("source and target cannot be same object");
            int sa = startAt;
            if (sa < 0)
                sa = PageCount;
            int outCount = PageCount;
            int srcCount = docsrc.PageCount;

            // local copies of page numbers
            int fp = fromPage;
            int tp = toPage;
            sa = startAt;

            // normalize page numbers
            fp = Math.Max(fp, 0); // -1 = first page
            fp = Math.Min(fp, srcCount - 1);  // but do not exceed last page

            if (tp < 0)
                tp = srcCount - 1;   // -1 = last page
            tp = Math.Min(tp, srcCount - 1);  // but do not exceed last page

            if (sa < 0)
                sa = outCount;   // -1 = behind last page
            sa = Math.Min(sa, outCount);  // but that is also the limit

            if (docsrc.PageCount > showProgress && showProgress > 0)
            {
                string inname = Path.GetFileName(docsrc.Name);
                if (string.IsNullOrEmpty(inname))
                    inname = "memory PDF";
                string outname = Path.GetFileName(Name);
                if (string.IsNullOrEmpty(outname))
                    outname = "memory PDF";
                Helpers.message($"Inserting '{inname}' at '{outname}'");
            }

            // retrieve / make a Graftmap to avoid duplicate objects
            //log( 'insert_pdf(): Graftmaps')
            int isrt = docsrc._graftId;
            Graftmap _gmap = null;
            if (!Graftmaps.TryGetValue(isrt, out _gmap) || _gmap == null)
            {
                //log( 'insert_pdf(): Graftmaps2')
                _gmap = new Graftmap(this);
                Graftmaps[isrt] = _gmap;
            }

            if (GUseExtra)
            {
                //log( 'insert_pdf(): calling extra_FzDocument_insert_pdf()')
                var pdfout = Helpers.AsPdfDocument(this);
                var pdfsrc = Helpers.AsPdfDocument(docsrc);
                if (pdfout.m_internal == null || pdfsrc.m_internal == null)
                    throw new ArgumentException("source or target not a PDF");
                Helpers.ENSURE_OPERATION(pdfout);
                Helpers.JmMergeRange(pdfout, pdfsrc, fp, tp, sa, rotate, links, annots, showProgress, _gmap.NativeGraftMap);
                //log( 'insert_pdf(): extra_FzDocument_insert_pdf() returned.')
            }
            else
            {
                var pdfout = Helpers.AsPdfDocument(this);
                var pdfsrc = Helpers.AsPdfDocument(docsrc);

                if (pdfout.m_internal == null || pdfsrc.m_internal == null)
                    throw new ArgumentException("source or target not a PDF");
                Helpers.ENSURE_OPERATION(pdfout);
                Helpers.JmMergeRange(pdfout, pdfsrc, fp, tp, sa, rotate, links, annots, showProgress, _gmap.NativeGraftMap);
            }

            //log( 'insert_pdf(): calling self._reset_page_refs()')
            ResetPageRefsInternal();
            if (links)
            {
                //log( 'insert_pdf(): calling self._do_links()')
                _do_links(docsrc, fromPage: fp, toPage: tp, startAt: sa);
            }
            if (widgets)
                _do_widgets(docsrc, _gmap, fromPage: fp, toPage: tp, startAt: sa, joinDuplicates: joinDuplicates);
            if (final == 1)
                Graftmaps[isrt] = null;
            //log( 'insert_pdf(): returning')
        }

        /// <summary>
        /// Insert links contained in copied page range into destination PDF.
        ///
        /// Parameter values must equal those of method insert_pdf(), which must
        /// have been previously executed.
        /// </summary>
        internal void _do_links(
            Document doc2,
            int fromPage = -1,
            int toPage = -1,
            int startAt = -1)
        {
            // doc1 is this (target PDF); doc2 is the source PDF (docsrc in insert_pdf).
            Document doc1 = this;
            //pymupdf.log( 'utils.do_links()')
            // --------------------------------------------------------------------------
            // internal function to create the actual "/Annots" object string
            // --------------------------------------------------------------------------
            // annot_skel — Python module-level dict in __init__.py
            string annot_goto1(int a, float b, float c, float d, string e) =>
                "<</A<</S/GoTo/D[" + a.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " 0 R/XYZ " + Helpers.FormatPdfReals(b, c, d) + "]>>/Rect[" + e + "]/BS<</W 0>>/Subtype/Link>>";
            string annot_gotor1(int a, float b, float c, float d, string e, string f, string g) =>
                "<</A<</S/GoToR/D[" + a.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " /XYZ " + Helpers.FormatPdfReals(b, c, d) + "]/F<</F(" + e + ")/UF(" + f + ")/Type/Filespec>>>>/Rect["
                + g + "]/BS<</W 0>>/Subtype/Link>>";
            string annot_gotor2(string a, string b, string c) =>
                "<</A<</S/GoToR/D" + a + "/F(" + b + ")>>/Rect[" + c + "]/BS<</W 0>>/Subtype/Link>>";
            string annot_launch(string a, string b, string c) =>
                "<</A<</S/Launch/F<</F(" + a + ")/UF(" + b + ")/Type/Filespec>>>>/Rect[" + c + "]/BS<</W 0>>/Subtype/Link>>";
            string annot_uri(string a, string b) =>
                "<</A<</S/URI/URI(" + a + ")>>/Rect[" + b + "]/BS<</W 0>>/Subtype/Link>>";

            string cre_annot(Dictionary<string, object> lnk, IList<int> xref_dst, IList<int> pno_src, Matrix ctm)
            {
                // Create annotation object string for a passed-in link.
                if (!lnk.TryGetValue("from", out var fromO) || !Helpers.TryCoerceRect(fromO, out var fromRect))
                    return "";
                var r = fromRect.Transform(ctm);  // rect in PDF coordinates
                string rect = Helpers.FormatPdfReals(r.X0, r.Y0, r.X1, r.Y1);
                int kind = Convert.ToInt32(lnk["kind"], System.Globalization.CultureInfo.InvariantCulture);
                string annot;
                if (kind == Constants.LinkGoto)
                {
                    // txt = annot_skel["goto1"]  # annot_goto
                    int idx = pno_src.IndexOf(Convert.ToInt32(lnk["page"], System.Globalization.CultureInfo.InvariantCulture));
                    if (idx < 0)
                        return "";
                    var p = new Point(0, 0);
                    if (lnk.TryGetValue("to", out var toO))
                    {
                        if (toO is Point pt)
                            p = pt;
                        else
                            Helpers.TryCoercePoint(toO, out p);
                    }
                    p = p.Transform(ctm);  // target point in PDF coordinates
                    float zoom = lnk.TryGetValue("zoom", out var zoomO)
                        ? (float)Convert.ToDouble(zoomO, System.Globalization.CultureInfo.InvariantCulture)
                        : 0.0f;
                    annot = annot_goto1(xref_dst[idx], p.X, p.Y, zoom, rect);
                }
                else if (kind == Constants.LinkGotor)
                {
                    int gpage = Convert.ToInt32(lnk["page"], System.Globalization.CultureInfo.InvariantCulture);
                    if (gpage >= 0)
                    {
                        // txt = annot_skel["gotor1"]  # annot_gotor
                        object pnt = lnk.TryGetValue("to", out var toO) ? toO : new Point(0, 0);  // destination point
                        if (!(pnt is Point))
                            pnt = new Point(0, 0);
                        var pntPt = (Point)pnt;
                        float zoom = lnk.TryGetValue("zoom", out var zoomO)
                            ? (float)Convert.ToDouble(zoomO, System.Globalization.CultureInfo.InvariantCulture)
                            : 0.0f;
                        string file = lnk["file"]?.ToString() ?? "";
                        annot = annot_gotor1(
                            gpage,
                            pntPt.X,
                            pntPt.Y,
                            zoom,
                            file,
                            file,
                            rect);
                    }
                    else
                    {
                        // txt = annot_skel["gotor2"]  # annot_gotor_n
                        string to = Helpers.GetPdfStr(DoLinksToPdfString(lnk["to"]));
                        to = to.Length >= 2 ? to.Substring(1, to.Length - 2) : to;
                        string f = lnk["file"]?.ToString() ?? "";
                        annot = annot_gotor2(to, f, rect);
                    }
                }
                else if (kind == Constants.LinkLaunch)
                {
                    // txt = annot_skel["launch"]  # annot_launch
                    string file = lnk["file"]?.ToString() ?? "";
                    annot = annot_launch(file, file, rect);
                }
                else if (kind == Constants.LinkUri)
                {
                    // txt = annot_skel["uri"]  # annot_uri
                    annot = annot_uri(lnk["uri"]?.ToString() ?? "", rect);
                }
                else
                    annot = "";

                return annot;
            }

            // --------------------------------------------------------------------------

            // validate & normalize parameters
            int fp;
            if (fromPage < 0)
                fp = 0;
            else if (fromPage >= doc2.PageCount)
                fp = doc2.PageCount - 1;
            else
                fp = fromPage;

            int tp;
            if (toPage < 0 || toPage >= doc2.PageCount)
                tp = doc2.PageCount - 1;
            else
                tp = toPage;

            if (startAt < 0)
                throw new ValueErrorException("'start_at' must be >= 0");
            int sa = startAt;

            int incr = fp <= tp ? 1 : -1;  // page range could be reversed

            // lists of source / destination page numbers
            var pno_src = new List<int>();
            for (int p = fp; ; p += incr)
            {
                if (incr > 0 && p > tp) break;
                if (incr < 0 && p < tp) break;
                pno_src.Add(p);
            }
            var pno_dst = new List<int>();
            for (int i = 0; i < pno_src.Count; i++)
                pno_dst.Add(sa + i);

            // lists of source / destination page xrefs
            var xref_src = new List<int>();
            var xref_dst = new List<int>();
            for (int i = 0; i < pno_src.Count; i++)
            {
                int p_src = pno_src[i];
                int p_dst = pno_dst[i];
                int old_xref = doc2.PageXref(p_src);
                int new_xref = doc1.PageXref(p_dst);
                xref_src.Add(old_xref);
                xref_dst.Add(new_xref);
            }

            // create the links for each copied page in destination PDF
            for (int i = 0; i < xref_src.Count; i++)
            {
                Page page_src = doc2[pno_src[i]];  // load source page
                var links = page_src.GetLinksDict();  // get all its links
                //log( '{pno_src=}')
                //log( '{type(page_src)=}')
                //log( '{page_src=}')
                //log( '{=i len(links)}')
                if (links.Count == 0)  // no links there
                {
                    page_src = null;
                    continue;
                }
                // Multiply link rects by inverse of pdf_page_transform() page CTM (#4958).
                var pageCtm = new mupdf.FzMatrix();
                Helpers.AsPdfPage(page_src, required: true).pdf_page_transform(new mupdf.FzRect(0, 0, 0, 0), pageCtm);
                Matrix ctm = new Matrix(pageCtm.fz_invert_matrix());
                Page page_dst = doc1[pno_dst[i]];  // load destination page
                var link_tab = new List<string>();  // store all link definitions here
                foreach (var l in links)
                {
                    if (l.TryGetValue("kind", out var kindO)
                        && Convert.ToInt32(kindO, System.Globalization.CultureInfo.InvariantCulture) == Constants.LinkGoto
                        && l.TryGetValue("page", out var pageO)
                        && !pno_src.Contains(Convert.ToInt32(pageO, System.Globalization.CultureInfo.InvariantCulture)))
                        continue;  // GOTO link target not in copied pages
                    string annot_text = cre_annot(l, xref_dst, pno_src, ctm);
                    if (!string.IsNullOrEmpty(annot_text))
                        link_tab.Add(annot_text);
                }
                if (link_tab.Count > 0)
                    page_dst._addAnnot_FromString(link_tab.ToArray());
            }
            //log( 'utils.do_links() returning.')
        }

        /// <summary>String operand for <c>get_pdf_str</c> in <c>_do_links</c> / GoToR named destinations.</summary>
        static string DoLinksToPdfString(object to)
        {
            if (to == null) return "";
            if (to is string s) return s;
            if (to is Point) return "";
            return to.ToString() ?? "";
        }

        /// <summary>
        /// Insert widgets of copied page range into target PDF.
        ///
        /// Parameter values must equal those of method insert_pdf() which
        /// must have been previously executed.
        /// </summary>
        internal void _do_widgets(
            Document src,
            Graftmap graftmap,
            int fromPage = -1,
            int toPage = -1,
            int startAt = -1,
            bool joinDuplicates = false)
        {
            if (!src.IsFormPdf)  // nothing to do: source PDF has no fields
                return;

            var tarpdf = NativePdfDocument;
            var srcpdf = src.NativePdfDocument;
            var gm = graftmap.NativeGraftMap;

            void clean_kid_parents(mupdf.PdfObj acro_fields)
            {
                // Make sure all kids have correct "Parent" pointers.
                int n = acro_fields.pdf_array_len();
                for (int i = 0; i < n; i++)
                {
                    var parent = acro_fields.pdf_array_get(i);
                    var kids = Helpers.PdfObjDictGet(parent,mupdf.mupdf.pdf_new_name("Kids"));
                    int kn = kids.pdf_array_len();
                    for (int j = 0; j < kn; j++)
                    {
                        var kid = kids.pdf_array_get(j);
                        kid.pdf_dict_put(mupdf.mupdf.pdf_new_name("Parent"), parent);
                    }
                }
            }

            void join_widgets(mupdf.PdfDocument pdf, mupdf.PdfObj acro_fields, int xref1, int xref2, string name)
            {
                void re_target(mupdf.PdfDocument pdfDoc, mupdf.PdfObj acroFlds, int x1, mupdf.PdfObj kids1, int x2, mupdf.PdfObj kids2)
                {
                    var w1_ind = mupdf.mupdf.pdf_new_indirect(pdfDoc, x1, 0);
                    var w2_ind = mupdf.mupdf.pdf_new_indirect(pdfDoc, x2, 0);
                    int idx = acroFlds.pdf_array_find(w2_ind);
                    acroFlds.pdf_array_delete(idx);

                    if (kids2.pdf_is_array() == 0)
                    {
                        var widget = mupdf.mupdf.pdf_load_object(pdfDoc, x2);
                        widget.pdf_dict_del(mupdf.mupdf.pdf_new_name("T"));
                        widget.pdf_dict_put(mupdf.mupdf.pdf_new_name("Parent"), w1_ind);
                        kids1.pdf_array_push(w2_ind);
                    }
                    else
                    {
                        int kn = kids2.pdf_array_len();
                        for (int i = 0; i < kn; i++)
                        {
                            var kid = kids2.pdf_array_get(i);
                            kid.pdf_dict_put(mupdf.mupdf.pdf_new_name("Parent"), w1_ind);
                            var kid_ind = mupdf.mupdf.pdf_new_indirect(pdfDoc, kid.pdf_to_num(), 0);
                            kids1.pdf_array_push(kid_ind);
                        }
                    }
                }

                void new_target(mupdf.PdfDocument pdfDoc, mupdf.PdfObj acroFlds, int x1, mupdf.PdfObj w1, int x2, mupdf.PdfObj w2, string fieldName)
                {
                    var newDict = mupdf.mupdf.pdf_new_dict(pdfDoc, 5);
                    newDict.pdf_dict_put_text_string(mupdf.mupdf.pdf_new_name("T"), fieldName);
                    var kids = newDict.pdf_dict_put_array(mupdf.mupdf.pdf_new_name("Kids"), 2);
                    var new_obj = mupdf.mupdf.pdf_add_object(pdfDoc, newDict);
                    int new_obj_xref = new_obj.pdf_to_num();
                    var new_ind = mupdf.mupdf.pdf_new_indirect(pdfDoc, new_obj_xref, 0);

                    var ft = Helpers.PdfObjDictGet(w1,mupdf.mupdf.pdf_new_name("FT"));
                    w1.pdf_dict_del(mupdf.mupdf.pdf_new_name("FT"));
                    new_obj.pdf_dict_put(mupdf.mupdf.pdf_new_name("FT"), ft);

                    var aa = Helpers.PdfObjDictGet(w1,mupdf.mupdf.pdf_new_name("AA"));
                    w1.pdf_dict_del(mupdf.mupdf.pdf_new_name("AA"));
                    new_obj.pdf_dict_put(mupdf.mupdf.pdf_new_name("AA"), aa);

                    w1.pdf_dict_del(mupdf.mupdf.pdf_new_name("T"));
                    w1.pdf_dict_put(mupdf.mupdf.pdf_new_name("Parent"), new_ind);
                    w2.pdf_dict_del(mupdf.mupdf.pdf_new_name("T"));
                    w2.pdf_dict_put(mupdf.mupdf.pdf_new_name("Parent"), new_ind);

                    var ind1 = mupdf.mupdf.pdf_new_indirect(pdfDoc, x1, 0);
                    var ind2 = mupdf.mupdf.pdf_new_indirect(pdfDoc, x2, 0);
                    kids.pdf_array_push(ind1);
                    kids.pdf_array_push(ind2);

                    int idx = acroFlds.pdf_array_find(ind1);
                    acroFlds.pdf_array_delete(idx);
                    idx = acroFlds.pdf_array_find(ind2);
                    acroFlds.pdf_array_delete(idx);

                    acroFlds.pdf_array_push(new_ind);
                }

                var w1 = mupdf.mupdf.pdf_load_object(pdf, xref1);
                var w2 = mupdf.mupdf.pdf_load_object(pdf, xref2);
                var kids1 = Helpers.PdfObjDictGet(w1,mupdf.mupdf.pdf_new_name("Kids"));
                var kids2 = Helpers.PdfObjDictGet(w2,mupdf.mupdf.pdf_new_name("Kids"));

                if (kids1.pdf_is_array() != 0)
                    re_target(pdf, acro_fields, xref1, kids1, xref2, kids2);
                else if (kids2.pdf_is_array() != 0)
                    re_target(pdf, acro_fields, xref2, kids2, xref1, kids1);
                else
                    new_target(pdf, acro_fields, xref1, w1, xref2, w2, name);
            }

            List<int> get_kids(mupdf.PdfObj parent, List<int> kids_list)
            {
                var kids = Helpers.PdfDictGet(parent, mupdf.mupdf.pdf_new_name("Kids"));
                if (kids.pdf_is_array() == 0)
                    return kids_list;
                int n = kids.pdf_array_len();
                for (int i = 0; i < n; i++)
                {
                    var kid = kids.pdf_array_get(i);
                    if (mupdf.mupdf.pdf_is_dict(Helpers.PdfDictGet(kid, mupdf.mupdf.pdf_new_name("Kids"))) != 0)
                        kids_list = get_kids(kid, kids_list);
                    else
                        kids_list.Add(kid.pdf_to_num());
                }
                return kids_list;
            }

            (int parent_xref, List<int> kids_list) kids_xrefs(mupdf.PdfObj widget)
            {
                var kids_list = new List<int>();
                var parent = Helpers.PdfDictGet(widget, mupdf.mupdf.pdf_new_name("Parent"));
                int parent_xref = parent.pdf_to_num();
                if (parent_xref == 0)
                    return (parent_xref, kids_list);
                kids_list = get_kids(parent, kids_list);
                return (parent_xref, kids_list);
            }

            void deduplicate_names(mupdf.PdfDocument pdf, mupdf.PdfObj acro_fields, bool join_duplicates)
            {
                var names = new Dictionary<string, List<int>>();

                int n = mupdf.mupdf.pdf_array_len(acro_fields);
                for (int i = 0; i < n; i++)
                {
                    var wobject = mupdf.mupdf.pdf_array_get(acro_fields, i);
                    int xref = wobject.pdf_to_num();
                    string T = wobject.pdf_dict_get_text_string(mupdf.mupdf.pdf_new_name("T"));
                    if (!names.TryGetValue(T, out var xrefs))
                        xrefs = new List<int>();
                    xrefs.Add(xref);
                    names[T] = xrefs;
                }

                foreach (var kv in names)
                {
                    string name = kv.Key;
                    var xrefs = kv.Value;
                    if (xrefs.Count < 2)
                        continue;
                    int xref0 = xrefs[0];
                    int xref1 = xrefs[1];
                    if (join_duplicates)
                        join_widgets(pdf, acro_fields, xref0, xref1, name);
                    else
                    {
                        string newname = name + $" [{xref1}]";  // append this to the name
                        var wobject = mupdf.mupdf.pdf_load_object(pdf, xref1);
                        wobject.pdf_dict_put_text_string(mupdf.mupdf.pdf_new_name("T"), newname);
                    }
                }

                clean_kid_parents(acro_fields);
            }

            mupdf.PdfObj get_acroform(Document doc)
            {
                var pdf = doc.NativePdfDocument;
                return Helpers.PdfDictGetp(mupdf.mupdf.pdf_trailer(pdf), "Root/AcroForm");
            }

            mupdf.PdfObj acro;
            mupdf.PdfObj acro_fields;
            mupdf.PdfObj tar_co;

            if (IsFormPdf)
            {
                acro = get_acroform(this);
                acro_fields = Helpers.PdfObjDictGet(acro,mupdf.mupdf.pdf_new_name("Fields"));
                tar_co = Helpers.PdfObjDictGet(acro,mupdf.mupdf.pdf_new_name("CO"));
                if (tar_co.pdf_is_array() == 0)
                    tar_co = acro.pdf_dict_put_array(mupdf.mupdf.pdf_new_name("CO"), 5);
            }
            else
            {
                acro = mupdf.mupdf.pdf_deep_copy_obj(get_acroform(src));
                acro.pdf_dict_del(mupdf.mupdf.pdf_new_name("Fields"));
                acro.pdf_dict_put_array(mupdf.mupdf.pdf_new_name("Fields"), 5);
                acro.pdf_dict_del(mupdf.mupdf.pdf_new_name("CO"));
                acro.pdf_dict_put_array(mupdf.mupdf.pdf_new_name("CO"), 5);

                var acro_graft = gm.pdf_graft_mapped_object(acro);
                var acro_tar = mupdf.mupdf.pdf_add_object(tarpdf, acro_graft);
                acro_fields = Helpers.PdfObjDictGet(acro_tar,mupdf.mupdf.pdf_new_name("Fields"));
                tar_co = Helpers.PdfObjDictGet(acro_tar,mupdf.mupdf.pdf_new_name("CO"));

                int tar_xref = acro_tar.pdf_to_num();
                var acro_tar_ind = mupdf.mupdf.pdf_new_indirect(tarpdf, tar_xref, 0);
                var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(tarpdf), mupdf.mupdf.pdf_new_name("Root"));
                root.pdf_dict_put(mupdf.mupdf.pdf_new_name("AcroForm"), acro_tar_ind);
            }

            List<int> src_range;
            if (fromPage <= toPage)
            {
                src_range = new List<int>();
                for (int p = fromPage; p <= toPage; p++)
                    src_range.Add(p);
            }
            else
            {
                src_range = new List<int>();
                for (int p = fromPage; p >= toPage; p--)
                    src_range.Add(p);
            }

            var parents = new Dictionary<int, Dictionary<string, object>>();

            foreach (int i in src_range)
            {
                Page src_page = src[i];
                foreach (var (xref, wtype, _) in src_page.AnnotXrefs())
                {
                    if (wtype != AnnotationType.Widget)
                        continue;
                    var w_obj = mupdf.mupdf.pdf_load_object(srcpdf, xref);
                    w_obj.pdf_dict_del(mupdf.mupdf.pdf_new_name("P"));

                    var (parent_xref, old_kids) = kids_xrefs(w_obj);
                    if (parent_xref != 0)
                    {
                        parents[parent_xref] = new Dictionary<string, object>
                        {
                            ["new_xref"] = 0,
                            ["old_kids"] = old_kids,
                            ["new_kids"] = new List<int>(),
                        };
                    }
                }
            }

            foreach (int xref in parents.Keys)
            {
                var parent = mupdf.mupdf.pdf_load_object(srcpdf, xref);
                var parent_graft = gm.pdf_graft_mapped_object(parent);
                var parent_tar = mupdf.mupdf.pdf_add_object(tarpdf, parent_graft);
                var kids_xrefs_new = get_kids(parent_tar, new List<int>());
                int parent_xref_new = parent_tar.pdf_to_num();
                var parent_ind = mupdf.mupdf.pdf_new_indirect(tarpdf, parent_xref_new, 0);
                acro_fields.pdf_array_push(parent_ind);
                parents[xref]["new_xref"] = parent_xref_new;
                parents[xref]["new_kids"] = kids_xrefs_new;
            }

            for (int i = 0; i < src_range.Count; i++)
            {
                Page tar_page = this[startAt + i];
                Page src_page = src[src_range[i]];

                var w_xrefs = new List<int>();
                foreach (var (xref, wtype, _) in src_page.AnnotXrefs())
                {
                    if (wtype == AnnotationType.Widget)
                        w_xrefs.Add(xref);
                }
                if (w_xrefs.Count == 0)
                    continue;

                var tar_page_pdf = tar_page.NativePdfPage;
                var tar_annots = Helpers.PdfDictGet(tar_page_pdf.obj(), mupdf.mupdf.pdf_new_name("Annots"));
                if (mupdf.mupdf.pdf_is_array(tar_annots) == 0)
                    tar_annots = tar_page_pdf.obj().pdf_dict_put_array(mupdf.mupdf.pdf_new_name("Annots"), 5);

                foreach (int xref in w_xrefs)
                {
                    var w_obj = mupdf.mupdf.pdf_load_object(srcpdf, xref);
                    var is_aac = mupdf.mupdf.pdf_is_dict(w_obj.pdf_dict_getp("AA/C"));
                    int parent_xref = Helpers.PdfObjDictGet(w_obj,mupdf.mupdf.pdf_new_name("Parent")).pdf_to_num();
                    mupdf.PdfObj w_obj_tar_ind;
                    if (parent_xref == 0)
                    {
                        try
                        {
                            var w_obj_graft = gm.pdf_graft_mapped_object(w_obj);
                            var w_obj_tar = mupdf.mupdf.pdf_add_object(tarpdf, w_obj_graft);
                            int tar_xref = w_obj_tar.pdf_to_num();
                            w_obj_tar_ind = mupdf.mupdf.pdf_new_indirect(tarpdf, tar_xref, 0);
                            tar_annots.pdf_array_push(w_obj_tar_ind);
                            acro_fields.pdf_array_push(w_obj_tar_ind);
                        }
                        catch (Exception e)
                        {
                            Helpers.message($"cannot copy widget at xref={xref}: {e}");
                            continue;
                        }
                    }
                    else
                    {
                        var parent = parents[parent_xref];
                        var old_kids = (List<int>)parent["old_kids"];
                        int idx = old_kids.IndexOf(xref);
                        var new_kids = (List<int>)parent["new_kids"];
                        int tar_xref = new_kids[idx];
                        w_obj_tar_ind = mupdf.mupdf.pdf_new_indirect(tarpdf, tar_xref, 0);
                        tar_annots.pdf_array_push(w_obj_tar_ind);
                    }

                    if (is_aac != 0)
                        tar_co.pdf_array_push(w_obj_tar_ind);
                }
            }

            deduplicate_names(tarpdf, acro_fields, joinDuplicates);
        }
        /// <summary>
        /// Inserts pages from any supported document type.
        /// </summary>
        /// <remarks>PDF only: Add an arbitrary supported document to the current PDF. Opens "infile" as a document, converts it to a PDF and then invokes <see cref="InsertPdf"/>. Parameters are the same as for that method. Among other things, this features an easy way to append images as full pages to an output PDF. PyMuPDF <c>Document.insert_file</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="infile">the input document to insert. May be a filename specification as is valid for creating a Document or a Pixmap.</param>
        /// <param name="fromPage">First source page number (0-based, inclusive).</param>
        /// <param name="toPage">Last source page number (0-based, inclusive).</param>
        /// <param name="startAt">See PyMuPDF parameter &lt;c&gt;startAt&lt;/c&gt;.</param>
        /// <param name="rotate">Rotation in degrees (multiple of 90).</param>
        /// <param name="links">Whether to copy link annotations.</param>
        /// <param name="annots">Whether to copy non-widget annotations.</param>
        /// <param name="showProgress">See PyMuPDF parameter &lt;c&gt;showProgress&lt;/c&gt;.</param>
        /// <param name="final">If true, drop copied-object cache after insert (use false for batch inserts).</param>
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

        /// <summary>Port of PyMuPDF <c>JM_ensure_ocproperties</c>.</summary>
        private static mupdf.PdfObj EnsureOcProperties(mupdf.PdfDocument pdf)
        {
            var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            var ocp = Helpers.PdfDictGet(root, mupdf.mupdf.pdf_new_name("OCProperties"));
            if (ocp.m_internal != null)
                return ocp;
            ocp = mupdf.mupdf.pdf_dict_put_dict(root, mupdf.mupdf.pdf_new_name("OCProperties"), 2);
            mupdf.mupdf.pdf_dict_put_array(ocp, mupdf.mupdf.pdf_new_name("OCGs"), 0);
            var d = mupdf.mupdf.pdf_dict_put_dict(ocp, mupdf.mupdf.pdf_new_name("D"), 5);
            mupdf.mupdf.pdf_dict_put_array(d, mupdf.mupdf.pdf_new_name("ON"), 0);
            mupdf.mupdf.pdf_dict_put_array(d, mupdf.mupdf.pdf_new_name("OFF"), 0);
            mupdf.mupdf.pdf_dict_put_array(d, mupdf.mupdf.pdf_new_name("Order"), 0);
            mupdf.mupdf.pdf_dict_put_array(d, mupdf.mupdf.pdf_new_name("RBGroups"), 0);
            return ocp;
        }
        /// <summary>
        /// Gets details for all OCGs.
        /// </summary>
        /// <remarks>Details of all optional content groups. This is a dictionary of dictionaries like this (key is the OCG's xref): PyMuPDF <c>Document.get_ocgs</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns>A non-negative result code or xref number.</returns>
        public Dictionary<int, Dictionary<string, object>> GetOcgs()
        {
            // Return the definitions of existing optional content groups.
            var result = new Dictionary<int, Dictionary<string, object>>();
            var pdf = NativePdfDocument;
            var ci = mupdf.mupdf.pdf_new_name("CreatorInfo");
            var ocProps = Helpers.PdfDictGet(Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root")), mupdf.mupdf.pdf_new_name("OCProperties"));
            if (ocProps.m_internal == null) return result;

            var ocgs = Helpers.PdfDictGet(ocProps, mupdf.mupdf.pdf_new_name("OCGs"));
            if (ocgs.m_internal == null || mupdf.mupdf.pdf_is_array(ocgs) == 0) return result;

            int n = mupdf.mupdf.pdf_array_len(ocgs);
            for (int i = 0; i < n; i++)
            {
                var ocg = mupdf.mupdf.pdf_array_get(ocgs, i);
                int xref = mupdf.mupdf.pdf_to_num(ocg);
                var ocgObj = mupdf.mupdf.pdf_resolve_indirect(ocg);
                var nameObj = Helpers.PdfDictGet(ocgObj, mupdf.mupdf.pdf_new_name("Name"));
                string name = nameObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(nameObj) : "";

                string usage = null;
                var usageObj = Helpers.PdfDictGet(
                    Helpers.PdfDictGet(
                        Helpers.PdfDictGet(ocgObj, mupdf.mupdf.pdf_new_name("Usage")),
                        ci),
                    mupdf.mupdf.pdf_new_name("Subtype"));
                if (usageObj.m_internal != null)
                    usage = mupdf.mupdf.pdf_to_name(usageObj);

                var intents = new List<string>();
                var intent = Helpers.PdfDictGet(ocgObj, mupdf.mupdf.pdf_new_name("Intent"));
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
        /// Adds an optional content configuration.
        /// </summary>
        /// <remarks>Add an optional content configuration. Layers serve as a collection of ON / OFF states for optional content groups and allow fast visibility switches between different views on the same document. PyMuPDF <c>Document.add_layer</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="name">arbitrary name.</param>
        /// <param name="creator">(optional) creating software.</param>
        /// <param name="on">a sequence of OCG xref numbers which should be set to ON when this layer gets activated. All OCGs not listed here will be set to OFF.</param>
        public void AddLayer(string name, string creator = null, bool on = true)
        {
            // Add a new OC layer.
            var pdf = NativePdfDocument;
            var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            var ocProps = Helpers.PdfDictGet(root, mupdf.mupdf.pdf_new_name("OCProperties"));
            if (ocProps.m_internal == null)
            {
                ocProps = mupdf.mupdf.pdf_new_dict(pdf, 2);
                mupdf.mupdf.pdf_dict_puts(root, "OCProperties", ocProps);
            }

            var ocgs = Helpers.PdfDictGet(ocProps, mupdf.mupdf.pdf_new_name("OCGs"));
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
                var d = Helpers.PdfDictGet(ocProps, mupdf.mupdf.pdf_new_name("D"));
                if (d.m_internal == null)
                {
                    d = mupdf.mupdf.pdf_new_dict(pdf, 1);
                    mupdf.mupdf.pdf_dict_puts(ocProps, "D", d);
                }
                var onArr = Helpers.PdfDictGet(d, mupdf.mupdf.pdf_new_name("ON"));
                if (onArr.m_internal == null)
                {
                    onArr = mupdf.mupdf.pdf_new_array(pdf, 1);
                    mupdf.mupdf.pdf_dict_puts(d, "ON", onArr);
                }
                mupdf.mupdf.pdf_array_push(onArr, indRef);
            }
        }
        /// <summary>
        /// Adds an optional content group (layer).
        /// </summary>
        /// <remarks>Add an optional content group. An OCG is the most important unit of information to determine object visibility. For a PDF, in order to be regarded as having optional content, at least one OCG must exist. PyMuPDF <c>Document.add_ocg</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="name">arbitrary name. Will show up in supporting PDF viewers.</param>
        /// <param name="config">layer configuration number. Default -1 is the standard configuration.</param>
        /// <param name="on">standard visibility status for objects pointing to this OCG.</param>
        /// <param name="intent">a string or list of strings declaring the visibility intents. There are two PDF standard values to choose from: "View" and "Design". Default is "View". Correct spelling is important.</param>
        /// <param name="usage">another influencer for OCG visibility. This will become part of the OCG's `/Usage` key. There are two PDF standard values to choose from: "Artwork" and "Technical". Default is "Artwork". Please only change when required.</param>
        /// <returns>xref of the created OCG. Use as entry for <c>oc</c> parameter in supporting objects.</returns>
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

            var ocp = EnsureOcProperties(pdf);
            var ocgs = Helpers.PdfDictGet(ocp, mupdf.mupdf.pdf_new_name("OCGs"));
            mupdf.mupdf.pdf_array_push(ocgs, indocg);

            mupdf.PdfObj cfg;
            // if config > -1: use Configs[config], else use default config D
            if (config > -1)
            {
                var cfgs = Helpers.PdfDictGet(ocp, mupdf.mupdf.pdf_new_name("Configs"));
                if (mupdf.mupdf.pdf_is_array(cfgs) == 0)
                    throw new ValueErrorException(Constants.MSG_BAD_OC_CONFIG);
                cfg = mupdf.mupdf.pdf_array_get(cfgs, config);
                if (cfg.m_internal == null)
                    throw new ValueErrorException(Constants.MSG_BAD_OC_CONFIG);
            }
            else
            {
                cfg = Helpers.PdfDictGet(ocp, mupdf.mupdf.pdf_new_name("D"));
            }

            var order = Helpers.PdfDictGet(cfg, mupdf.mupdf.pdf_new_name("Order"));
            if (order.m_internal == null)
                order = mupdf.mupdf.pdf_dict_put_array(cfg, mupdf.mupdf.pdf_new_name("Order"), 1);
            mupdf.mupdf.pdf_array_push(order, indocg);

            var stateArr = Helpers.PdfDictGet(cfg, mupdf.mupdf.pdf_new_name(on ? "ON" : "OFF"));
            if (stateArr.m_internal == null)
                stateArr = mupdf.mupdf.pdf_dict_put_array(cfg, mupdf.mupdf.pdf_new_name(on ? "ON" : "OFF"), 1);
            mupdf.mupdf.pdf_array_push(stateArr, indocg);

            mupdf.mupdf.ll_pdf_read_ocg(pdf.m_internal);
            return mupdf.mupdf.pdf_to_num(indocg);
        }
        /// <summary>
        /// Gets OCG on/off/radio-button groups for a configuration.
        /// </summary>
        /// <remarks>List of optional content groups by status in the specified configuration. This is a dictionary with lists of cross reference numbers for OCGs that occur in the arrays `/ON`, `/OFF` or in some radio button group (`/RBGroups`). PyMuPDF <c>Document.get_layer</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="config">the configuration layer (default is the standard config layer).</param>
        /// <returns>A dictionary of entries.</returns>
        public Dictionary<string, object> GetLayer(int config = -1)
        {
            var pdf = NativePdfDocument;
            var ocp = Helpers.PdfDictGet(
                Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root")),
                mupdf.mupdf.pdf_new_name("OCProperties"));
            if (ocp.m_internal == null)
                return null;

            mupdf.PdfObj obj;
            if (config == -1)
            {
                obj = Helpers.PdfDictGet(ocp, mupdf.mupdf.pdf_new_name("D"));
            }
            else
            {
                obj = mupdf.mupdf.pdf_array_get(
                    Helpers.PdfDictGet(ocp, mupdf.mupdf.pdf_new_name("Configs")),
                    config
                );
            }
            if (obj.m_internal == null)
                throw new ValueErrorException(Constants.MSG_BAD_OC_CONFIG);

            List<int> ReadXrefArray(string key)
            {
                var arr = Helpers.PdfDictGet(obj, mupdf.mupdf.pdf_new_name(key));
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
            var rbObj = Helpers.PdfDictGet(obj, mupdf.mupdf.pdf_new_name("RBGroups"));
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
            var baseObj = Helpers.PdfDictGet(obj, mupdf.mupdf.pdf_new_name("BaseState"));
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
        /// Lists optional content configurations.
        /// </summary>
        /// <remarks>Show optional layer configurations. There always is a standard one, which is not included in the response. PyMuPDF <c>Document.get_layers</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns>A list of results.</returns>
        public List<Dictionary<string, object>> GetLayers()
        {
            var pdf = NativePdfDocument;
            int n = mupdf.mupdf.pdf_count_layer_configs(pdf);
            if (n == 1)
            {
                var obj = Helpers.PdfDictGet(
                    Helpers.PdfDictGet(
                        Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root")),
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
        /// Lists user-toggleable optional content items.
        /// </summary>
        /// <remarks>Show the visibility status of optional content that is modifiable by the user interface of supporting PDF viewers. PyMuPDF <c>Document.layer_ui_configs</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns>A list of results.</returns>
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
        /// Changes OC visibility via UI-style actions.
        /// </summary>
        /// <remarks>Modify OC visibility status of content groups. This is analog to what supporting PDF viewers would offer. PyMuPDF <c>Document.set_layer_ui_config</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="number">either the sequence number of the item in list <see cref="LayerConfigs"/> or the "text" of one of these items.</param>
        /// <param name="action">`PDF_OC_ON` = set on (default), `PDF_OC_TOGGLE` = toggle on/off, `PDF_OC_OFF` = set off.</param>
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
        /// Permanently sets OCG states for a configuration.
        /// </summary>
        /// <remarks>Mass status changes of optional content groups. Permanently sets the status of OCGs. PyMuPDF <c>Document.set_layer</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="config">desired configuration layer, choose -1 for the default one.</param>
        /// <param name="basestate">state of OCGs that are not mentioned in *on* or *off*. Possible values are "ON", "OFF" or "Unchanged". Upper / lower case possible.</param>
        /// <param name="on">list of xref of OCGs to set ON. Replaces previous values. An empty list will cause no OCG being set to ON anymore. Should be specified if <c>basestate="ON"</c> is used.</param>
        /// <param name="off">list of xref of OCGs to set OFF. Replaces previous values. An empty list will cause no OCG being set to OFF anymore. Should be specified if <c>basestate="OFF"</c> is used.</param>
        /// <param name="rbgroups">a list of lists. Replaces previous values. Each sublist should contain two or more OCG xrefs. OCGs in the same sublist are handled like buttons in a radio button group: setting one to ON automatically sets all other group members to OFF.</param>
        /// <param name="locked">a list of OCG xref number that cannot be changed by the user interface.</param>
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
            var ocp = Helpers.PdfDictGet(
                Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root")),
                mupdf.mupdf.pdf_new_name("OCProperties"));
            if (ocp.m_internal == null)
                return;

            mupdf.PdfObj obj;
            // if config == -1: obj = D else obj = Configs[config]
            if (config == -1)
            {
                obj = Helpers.PdfDictGet(ocp, mupdf.mupdf.pdf_new_name("D"));
            }
            else
            {
                obj = mupdf.mupdf.pdf_array_get(
                    Helpers.PdfDictGet(ocp, mupdf.mupdf.pdf_new_name("Configs")),
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
        /// Temporarily switches the active OC configuration.
        /// </summary>
        /// <remarks>Switch to a document view as defined by the optional layer's configuration number. This is temporary, except if established as default. PyMuPDF <c>Document.switch_layer</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="config">Optional content configuration number (-1 for default).</param>
        /// <param name="asDefault">See PyMuPDF parameter &lt;c&gt;asDefault&lt;/c&gt;.</param>
        public void SwitchLayer(int config, bool asDefault = false)
        {
            var pdf = NativePdfDocument;
            var cfgs = Helpers.PdfDictGet(
                Helpers.PdfDictGet(
                    Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root")),
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
        /// PDF only: check if PDF contains any annots
        /// </summary>
        /// <remarks>PDF only: Check whether there are links, resp. annotations anywhere in the document. PyMuPDF <c>Document.has_annots</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns><see langword="true"/> / <see langword="false"/>. As opposed to fields, which are also stored in a central place of a PDF document, the existence of links / annotations can only be detected by parsing each page. These methods are tuned to do this efficiently and will immediately return, if the answer is <see langword="true"/> for a page. For PDFs with many thousand pages however, an answer may take some time if no link, resp. no annotation is found.</returns>
        public bool HasAnnots()
        {
            if (IsClosed)
                throw new ValueErrorException("document closed");
            if (!IsPdf)
                throw new ValueErrorException("is no PDF");
            for (int i = 0; i < PageCount; i++)
            {
                foreach (var item in GetPageAnnotXrefs(i))
                {
                    // pylint: disable=no-member
                    if (!((int)item.type == (int)mupdf.pdf_annot_type.PDF_ANNOT_LINK || (int)item.type == (int)mupdf.pdf_annot_type.PDF_ANNOT_WIDGET))  // pylint: disable=no-member
                        return true;
                }
            }
            return false;
        }
        /// <summary>
        /// PDF only: check if PDF contains any links
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.has_links</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns><see langword="true"/> if the operation succeeded.</returns>
        public bool HasLinks()
        {
            if (IsClosed)
                throw new ValueErrorException("document closed");
            if (!IsPdf)
                throw new ValueErrorException("is no PDF");
            for (int i = 0; i < PageCount; i++)
            {
                foreach (var item in GetPageAnnotXrefs(i))
                {
                    if ((int)item.type == (int)mupdf.pdf_annot_type.PDF_ANNOT_LINK)  // pylint: disable=no-member
                        return true;
                }
            }
            return false;
        }

        // ─── Bake ───────────────────────────────────────────────────────
        /// <summary>
        /// Bakes annotations and widgets into page content.
        /// </summary>
        /// <remarks>PDF only: Convert annotations and / or widgets to become permanent parts of the pages. The PDF will be changed by this method. If <c>widgets</c> is <c>True</c>, the document will also no longer be a "Form PDF". PyMuPDF <c>Document.bake</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="annots">convert annotations.</param>
        /// <param name="widgets">convert fields / widgets. After execution, the document will no longer be a "Form PDF".</param>
        public void Bake(bool annots = true, bool widgets = true)
        {
            var pdf = NativePdfDocument;
            mupdf.mupdf.pdf_bake_document(pdf, annots ? 1 : 0, widgets ? 1 : 0);
        }

        // ─── Scrub ──────────────────────────────────────────────────────
        /// <summary>
        /// Removes sensitive data from the PDF (metadata, scripts, etc.).
        /// </summary>
        /// <remarks>PDF only: Remove potentially sensitive data from the PDF. This function is inspired by the similar "Sanitize" function in Adobe Acrobat products. The process is configurable by a number of options. PyMuPDF <c>Document.scrub</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="attachedFiles">See PyMuPDF parameter &lt;c&gt;attachedFiles&lt;/c&gt;.</param>
        /// <param name="cleanPages">See PyMuPDF parameter &lt;c&gt;cleanPages&lt;/c&gt;.</param>
        /// <param name="embeddedFiles">See PyMuPDF parameter &lt;c&gt;embeddedFiles&lt;/c&gt;.</param>
        /// <param name="hiddenText">See PyMuPDF parameter &lt;c&gt;hiddenText&lt;/c&gt;.</param>
        /// <param name="javascript">Remove JavaScript sources.</param>
        /// <param name="metadata">Remove PDF standard metadata.</param>
        /// <param name="redactions">Apply redaction annotations.</param>
        /// <param name="redactImages">See PyMuPDF parameter &lt;c&gt;redactImages&lt;/c&gt;.</param>
        /// <param name="removeLinks">See PyMuPDF parameter &lt;c&gt;removeLinks&lt;/c&gt;.</param>
        /// <param name="resetFields">See PyMuPDF parameter &lt;c&gt;resetFields&lt;/c&gt;.</param>
        /// <param name="resetResponses">See PyMuPDF parameter &lt;c&gt;resetResponses&lt;/c&gt;.</param>
        /// <param name="thumbnails">Remove thumbnail images from pages.</param>
        /// <param name="xmlMetadata">See PyMuPDF parameter &lt;c&gt;xmlMetadata&lt;/c&gt;.</param>
        public void Scrub(
            bool attachedFiles = true,
            bool cleanPages = true,
            bool embeddedFiles = true,
            bool hiddenText = true,
            bool javascript = true,
            bool metadata = true,
            bool redactions = true,
            int redactImages = 0,
            bool removeLinks = true,
            bool resetFields = true,
            bool resetResponses = true,
            bool thumbnails = true,
            bool xmlMetadata = true)
        {
            byte[][] RemoveHidden(byte[][] contLines)
            {
                // """Remove hidden text from a PDF page.
                //
                // Args:
                //     cont_lines: list of lines with /Contents content. Should have status
                //         from after page.cleanContents().
                //
                // Returns:
                //     List of /Contents lines from which hidden text has been removed.
                //
                // Notes:
                //     The input must have been created after the page's /Contents object(s)
                //     have been cleaned with page.cleanContents(). This ensures a standard
                //     formatting: one command per line, single spaces between operators.
                //     This allows for drastic simplification of this code.
                // """
                var outLines = new List<byte[]>();  // will return this
                bool inText = false;  // indicate if within BT/ET object
                bool suppress = false;  // indicate text suppression active
                bool makeReturn = false;
                foreach (byte[] line in contLines)
                {
                    if (ScrubBytesEqual(line, "BT"))  // start of text object
                    {
                        inText = true;  // switch on
                        outLines.Add(line);  // output it
                        continue;
                    }
                    if (ScrubBytesEqual(line, "ET"))  // end of text object
                    {
                        inText = false;  // switch off
                        outLines.Add(line);  // output it
                        continue;
                    }
                    if (ScrubBytesEqual(line, "3 Tr"))  // text suppression operator
                    {
                        suppress = true;  // switch on
                        makeReturn = true;
                        continue;
                    }
                    // if line[-2:] == b"Tr" and line[0] != b"3":
                    if (line.Length >= 2 && line[line.Length - 2] == (byte)'T' && line[line.Length - 1] == (byte)'r' && line[0] != (byte)'3')
                    {
                        suppress = false;  // text rendering changed
                        outLines.Add(line);
                        continue;
                    }
                    if (ScrubBytesEqual(line, "Q"))  // unstack command also switches off
                    {
                        suppress = false;
                        outLines.Add(line);
                        continue;
                    }
                    if (suppress && inText)  // suppress hidden lines
                        continue;
                    outLines.Add(line);
                }
                if (makeReturn)
                    return outLines.ToArray();
                // else:
                return null;
            }

            // if not doc.is_pdf:  # only works for PDF
            if (!IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);
            // if doc.is_encrypted or doc.is_closed:
            if (IsEncrypted || IsClosed)
                throw new ValueErrorException("closed or encrypted doc");

            // if not clean_pages:
            if (!cleanPages)
            {
                // hidden_text = False
                hiddenText = false;
                // redactions = False
                redactions = false;
            }

            // if metadata:
            if (metadata)
                SetMetadata(new Dictionary<string, string>());  // remove standard metadata

            // MuPDF's in-memory page cache can be stale after xref edits (e.g. TextWriter).
            if (cleanPages || hiddenText)
                SyncNativePdfFromMemory();

            // for page in doc:
            int pageCount = PageCount;
            for (int pno = 0; pno < pageCount; pno++)
            {
                Page page = LoadPage(pno);
                // if reset_fields:
                if (resetFields)
                {
                    // reset form fields (widgets)
                    // for widget in page.Widgets():
                    foreach (var widget in page.Widgets())
                        widget.reset();
                }

                // if remove_links:
                if (removeLinks)
                {
                    // links = page.GetLinks()  # list of all links on page
                    var links = page.GetLinksDict();
                    // for link in links:  # remove all links
                    foreach (var link in links)
                        page.DeleteLink(link);
                }

                // found_redacts = False
                bool foundRedacts = false;
                // for annot in page.Annots():
                foreach (var annot in page.Annots())
                {
                    // if annot.type[0] == mupdf.PDF_ANNOT_FILE_ATTACHMENT and attached_files:
                    if (annot.AnnotationType == AnnotationType.FileAttachment && attachedFiles)
                        annot.UpdateFile(buffer: new byte[] { (byte)' ' });  // set file content to empty
                    // if reset_responses:
                    if (resetResponses)
                        annot.DeleteResponses();
                    // if annot.type[0] == mupdf.PDF_ANNOT_REDACT:  # pylint: disable=no-member
                    if (annot.AnnotationType == AnnotationType.Redact)
                        foundRedacts = true;
                }

                // if redactions and found_redacts:
                if (redactions && foundRedacts)
                    page.ApplyRedactions(images: redactImages);

                // if not (clean_pages or hidden_text):
                if (!(cleanPages || hiddenText))
                    continue;  // done with the page

                // page.CleanContents()
                page.CleanContents();
                // if not page.GetContents():
                if (page.GetContents().Count == 0)
                    continue;
                // if hidden_text:
                if (hiddenText)
                {
                    // xrefs = page.GetContents()
                    var xrefs = page.GetContents();
                    // assert len(xrefs) == 1  # only one because of cleaning.
                    System.Diagnostics.Debug.Assert(xrefs.Count == 1);
                    // xref = xrefs[0]
                    int xref = xrefs[0];
                    // cont = doc.xref_stream(xref)
                    byte[] cont = xref_stream(xref);
                    // cont_lines = remove_hidden(cont.splitlines())  # remove hidden text
                    byte[][] contLines = ScrubSplitBytesLines(cont);
                    byte[][] cleaned = RemoveHidden(contLines);
                    // if cont_lines:  # something was actually removed
                    if (cleaned != null && cleaned.Length > 0)
                    {
                        // cont = b"\n".join(cont_lines)
                        cont = ScrubJoinBytesLines(cleaned);
                        // doc.UpdateStream(xref, cont)  # rewrite the page /Contents
                        UpdateStream(xref, cont);  // rewrite the page /Contents
                    }
                }

                // if thumbnails:  # remove page thumbnails?
                if (thumbnails)
                {
                    // if doc.xref_get_key(page.Xref, "Thumb")[0] != "null":
                    if (xref_get_key(page.Xref, "Thumb").type != "null")
                        XrefSetKey(page.Xref, "Thumb", "null");
                }
            }

            // pages are scrubbed, now perform document-wide scrubbing
            // remove embedded files
            // if embedded_files:
            if (embeddedFiles)
            {
                // for name in doc.embfile_names():
                foreach (var name in embfile_names())
                    embfile_del(name);
            }

            // if xml_metadata:
            if (xmlMetadata)
                DeleteXmlMetadata();
            // if not (xml_metadata or javascript):
            int xrefLimit;
            if (!(xmlMetadata || javascript))
                xrefLimit = 0;
            else
                xrefLimit = xref_length();
            // for xref in range(1, xref_limit):
            for (int xref = 1; xref < xrefLimit; xref++)
            {
                // if not doc.xref_object(xref):
                if (string.IsNullOrEmpty(xref_object(xref)))
                {
                    // msg = f"bad xref {xref} - clean PDF before scrubbing"
                    string msg = $"bad xref {xref} - clean PDF before scrubbing";
                    throw new ValueErrorException(msg);
                }
                // if javascript and doc.xref_get_key(xref, "S")[1] == "/JavaScript":
                if (javascript && xref_get_key(xref, "S").value == "/JavaScript")
                {
                    // obj = "<</S/JavaScript/JS()>>"  # replace with a null JavaScript
                    string obj = "<</S/JavaScript/JS()>>";
                    // doc.UpdateObject(xref, obj)  # update this object
                    UpdateObject(xref, obj);
                    continue;  // no further handling
                }

                // if not xml_metadata:
                if (!xmlMetadata)
                    continue;

                // if doc.xref_get_key(xref, "Type")[1] == "/Metadata":
                if (xref_get_key(xref, "Type").value == "/Metadata")
                {
                    // delete any metadata object directly
                    // doc.UpdateObject(xref, "<<>>")
                    UpdateObject(xref, "<<>>");
                    // doc.UpdateStream(xref, b"deleted", new=True)
                    UpdateStream(xref, System.Text.Encoding.ASCII.GetBytes("deleted"));
                    continue;
                }

                // if doc.xref_get_key(xref, "Metadata")[0] != "null":
                if (xref_get_key(xref, "Metadata").type != "null")
                    XrefSetKey(xref, "Metadata", "null");
            }
        }

        private static bool ScrubBytesEqual(byte[] line, string text)
        {
            byte[] b = System.Text.Encoding.ASCII.GetBytes(text);
            if (line.Length != b.Length) return false;
            for (int i = 0; i < b.Length; i++)
                if (line[i] != b[i]) return false;
            return true;
        }

        private static byte[][] ScrubSplitBytesLines(byte[] cont)
        {
            var lines = new List<byte[]>();
            int start = 0;
            for (int i = 0; i <= cont.Length; i++)
            {
                if (i == cont.Length || cont[i] == (byte)'\n')
                {
                    int end = i;
                    if (end > start && cont[end - 1] == (byte)'\r')
                        end--;
                    if (end > start)
                    {
                        var slice = new byte[end - start];
                        Buffer.BlockCopy(cont, start, slice, 0, end - start);
                        lines.Add(slice);
                    }
                    start = i + 1;
                }
            }
            return lines.ToArray();
        }

        private static byte[] ScrubJoinBytesLines(byte[][] contLines)
        {
            using var ms = new System.IO.MemoryStream();
            for (int i = 0; i < contLines.Length; i++)
            {
                if (i > 0)
                    ms.WriteByte((byte)'\n');
                ms.Write(contLines[i], 0, contLines[i].Length);
            }
            return ms.ToArray();
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
        /// Creates font subsets to reduce file size.
        /// </summary>
        /// <remarks>PDF only: Investigate eligible fonts for their use by text in the document. If a font is supported and a size reduction is possible, that font is replaced by a version with a subset of its characters. PyMuPDF <c>Document.subset_fonts</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="verbose">write various progress information to sysout. This currently only has an effect if `fallback` is `True`.</param>
        /// <param name="fallback">if <c>True</c> use the deprecated algorithm that makes use of package <c>fontTools &lt;https://pypi.org/project/fonttools/&gt;</c>_ (which hence must be installed). If using the recommended value <c>False</c> (default), MuPDF's native function is used -- which is very much faster and can subset a broader range of font types. Package fontTools is not required then.</param>
        public int? SubsetFonts(bool verbose = false, bool fallback = false)
        {
            // Font binaries: -  "buffer" -> (names, xrefs, (unicodes, glyphs))
            // An embedded font is uniquely defined by its fontbuffer only. It may have
            // multiple names and xrefs.
            // Once the sets of used unicodes and glyphs are known, we compute a
            // smaller version of the buffer user package fontTools.

            if (!fallback)  // by default use MuPDF function
            {
                var pdf = NativePdfDocument;
                var pages = new mupdf.vectori();
                for (int i = 0; i < PageCount; i++)
                    pages.Add(i);
                mupdf.mupdf.pdf_subset_fonts2(pdf, pages);
                return null;
            }

            var font_buffers = new Dictionary<FontBufferKey, (HashSet<string> name_set, HashSet<int> xref_set, (HashSet<int> set_ucs, HashSet<int> set_gid) subsets)>();

            (string widths, string dwidths) get_old_widths(int xref)
            {
                /// Retrieve old font '/W' and '/DW' values.
                var df = XrefGetKey(xref, "DescendantFonts");
                if (df.type != "array")  // only handle xref specifications
                    return (null, null);
                int df_xref = int.Parse(df.value.Substring(1, df.value.Length - 1).Replace("0 R", ""));
                var widths = XrefGetKey(df_xref, "W");
                string widths_val = null;
                if (widths.type != "array")  // no widths key found
                    widths_val = null;
                else
                    widths_val = widths.value;
                var dwidths = XrefGetKey(df_xref, "DW");
                string dwidths_val = null;
                if (dwidths.type != "int")
                    dwidths_val = null;
                else
                    dwidths_val = dwidths.value;
                return (widths_val, dwidths_val);
            }

            void set_old_widths(int xref, string widths, string dwidths)
            {
                /// Restore the old '/W' and '/DW' in subsetted font.
                ///
                /// If either parameter is None or evaluates to False, the corresponding
                /// dictionary key will be set to null.
                var df = XrefGetKey(xref, "DescendantFonts");
                if (df.type != "array")  // only handle xref specs
                    return;
                int df_xref = int.Parse(df.value.Substring(1, df.value.Length - 1).Replace("0 R", ""));
                if ((widths == null || widths.Length == 0) && XrefGetKey(df_xref, "W").type != "null")
                    XrefSetKey(df_xref, "W", "null");
                else
                    XrefSetKey(df_xref, "W", widths);
                if ((dwidths == null || dwidths.Length == 0) && XrefGetKey(df_xref, "DW").type != "null")
                    XrefSetKey(df_xref, "DW", "null");
                else
                    XrefSetKey(df_xref, "DW", dwidths);
            }

            void set_subset_fontname(int new_xref)
            {
                /// Generate a name prefix to tag a font as subset.
                ///
                /// We use a random generator to select 6 upper case ASCII characters.
                /// The prefixed name must be put in the font xref as the "/BaseFont" value
                /// and in the FontDescriptor object as the '/FontName' value.
                // The following generates a prefix like 'ABCDEF+'
                const string ascii_uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var rng = new Random();
                var prefixChars = new char[6];
                for (int i = 0; i < 6; i++)
                    prefixChars[i] = ascii_uppercase[rng.Next(ascii_uppercase.Length)];
                string prefix = new string(prefixChars) + "+";
                string font_str = XrefObject(new_xref, compressed: true);
                font_str = font_str.Replace("/BaseFont/", "/BaseFont/" + prefix);
                var df = XrefGetKey(new_xref, "DescendantFonts");
                if (df.type == "array")
                {
                    int df_xref = int.Parse(df.value.Substring(1, df.value.Length - 1).Replace("0 R", ""));
                    var fd = XrefGetKey(df_xref, "FontDescriptor");
                    if (fd.type == "xref")
                    {
                        int fd_xref = int.Parse(fd.value.Replace("0 R", ""));
                        string fd_str = XrefObject(fd_xref, compressed: true);
                        fd_str = fd_str.Replace("/FontName/", "/FontName/" + prefix);
                        UpdateObject(fd_xref, fd_str);
                    }
                }
                UpdateObject(new_xref, font_str);
            }

            byte[] build_subset(byte[] buffer, HashSet<int> unc_set, HashSet<int> gid_set)
            {
                /// Build font subset using fontTools.
                ///
                /// Args:
                ///     buffer: (bytes) the font given as a binary buffer.
                ///     unc_set: (set) required glyph ids.
                /// Returns:
                ///     Either None if subsetting is unsuccessful or the subset font buffer.
                try
                {
                    // import fontTools.subset as fts
                    RunFontToolsSubsetCheckImport();
                }
                catch (Exception)
                {
                    // if g_exceptions_verbose:    exception_info()
                    Helpers.message("This method requires fontTools to be installed.");
                    throw;
                }

                string tmp_dir = Path.Combine(Path.GetTempPath(), "mupdf_subset_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmp_dir);
                try
                {
                    string oldfont_path = Path.Combine(tmp_dir, "oldfont.ttf");
                    string newfont_path = Path.Combine(tmp_dir, "newfont.ttf");
                    string uncfile_path = Path.Combine(tmp_dir, "uncfile.txt");
                    var args = new List<string>
                    {
                        oldfont_path,
                        "--retain-gids",
                        $"--output-file={newfont_path}",
                        "--layout-features=*",
                        "--passthrough-tables",
                        "--ignore-missing-glyphs",
                        "--ignore-missing-unicodes",
                        "--symbol-cmap",
                    };

                    // store glyph ids or unicodes as file
                    if (unc_set.Contains(0xFFFD))  // error unicode exists -> use glyphs
                    {
                        args.Add($"--gids-file={uncfile_path}");
                        gid_set.Add(189);
                        var unc_list = gid_set.ToList();
                        using (var unc_file = new StreamWriter(uncfile_path, false, Encoding.UTF8))
                        {
                            foreach (int unc in unc_list)
                                unc_file.WriteLine($"{unc}");
                        }
                    }
                    else
                    {
                        args.Add($"--unicodes-file={uncfile_path}");
                        unc_set.Add(255);
                        var unc_list = unc_set.ToList();
                        using (var unc_file = new StreamWriter(uncfile_path, false, Encoding.UTF8))
                        {
                            foreach (int unc in unc_list)
                                unc_file.WriteLine($"{unc:x4}");
                        }
                    }

                    // store fontbuffer as a file
                    File.WriteAllBytes(oldfont_path, buffer);
                    try
                    {
                        File.Delete(newfont_path);  // remove old file
                    }
                    catch (Exception)
                    {
                    }
                    byte[] new_buffer = null;
                    try
                    {  // invoke fontTools subsetter
                        RunFontToolsSubsetMain(args);
                        using var font = new Font(fontFile: newfont_path);
                        new_buffer = font.Buffer;  // subset font binary
                        if (font.GlyphCount == 0)  // intercept empty font
                            new_buffer = null;
                    }
                    catch (Exception)
                    {
                        // exception_info()
                        new_buffer = null;
                    }
                    return new_buffer;
                }
                finally
                {
                    try { Directory.Delete(tmp_dir, recursive: true); } catch (Exception) { }
                }
            }

            void repl_fontnames(Document doc)
            {
                /// Populate 'font_buffers'.
                ///
                /// For each font candidate, store its xref and the list of names
                /// by which PDF text may refer to it (there may be multiple).

                string norm_name(string name)
                {
                    /// Recreate font name that contains PDF hex codes.
                    ///
                    /// E.g. #20 -> space, chr(32)
                    while (name.Contains("#"))
                    {
                        int p = name.IndexOf('#');
                        int c = Convert.ToInt32(name.Substring(p + 1, 2), 16);
                        name = name.Replace(name.Substring(p, 3), ((char)c).ToString());
                    }
                    return name;
                }

                List<string> get_fontnames(Document doc_, object[] item)
                {
                    /// Return a list of fontnames for an item of page.get_fonts().
                    ///
                    /// There may be multiple names e.g. for Type0 fonts.
                    string fontname = (string)item[3];
                    var names = new List<string> { fontname };
                    string baseFont = XrefGetKey((int)item[0], "BaseFont").value;
                    if (baseFont.Length > 0 && baseFont[0] == '/')
                        baseFont = baseFont.Substring(1);
                    fontname = norm_name(baseFont);
                    if (!names.Contains(fontname))
                        names.Add(fontname);
                    var descendents = XrefGetKey((int)item[0], "DescendantFonts");
                    if (descendents.type != "array")
                        return names;
                    string descendents_str = descendents.value.Substring(1, descendents.value.Length - 1);
                    if (descendents_str.EndsWith(" 0 R"))
                    {
                        int xref = int.Parse(descendents_str.Substring(0, descendents_str.Length - 4));
                        descendents_str = XrefObject(xref, compressed: true);
                    }
                    int p1 = descendents_str.IndexOf("/BaseFont");
                    if (p1 >= 0)
                    {
                        int p2 = descendents_str.IndexOf('/', p1 + 1);
                        int p_end = Math.Min(
                            descendents_str.IndexOf('/', p2 + 1),
                            descendents_str.IndexOf(">>", p2 + 1));
                        fontname = descendents_str.Substring(p2 + 1, p_end - (p2 + 1));
                        fontname = norm_name(fontname);
                        if (!names.Contains(fontname))
                            names.Add(fontname);
                    }
                    return names;
                }

                for (int i = 0; i < doc.PageCount; i++)
                {
                    foreach (var f in doc.get_page_fonts_py(i, full: true))
                    {
                        int font_xref = (int)f[0];  // font xref
                        string font_ext = (string)f[1];  // font file extension
                        string basename = (string)f[3];  // font basename

                        if (font_ext != "otf" && font_ext != "ttf" && font_ext != "woff" && font_ext != "woff2")
                        {  // skip if not supported by fontTools
                            continue;
                        }
                        // skip fonts which already are subsets
                        if (basename.Length > 6 && basename[6] == '+')
                            continue;

                        var extr = doc.extract_font(font_xref);
                        byte[] fontbuffer = extr.content;
                        var names = get_fontnames(doc, f);
                        var key = new FontBufferKey(fontbuffer);
                        if (!font_buffers.TryGetValue(key, out var entry))
                            entry = (new HashSet<string>(), new HashSet<int>(), (new HashSet<int>(), new HashSet<int>()));
                        var (name_set, xref_set, subsets) = entry;
                        xref_set.Add(font_xref);
                        foreach (string name in names)
                            name_set.Add(name);
                        using (var font = new Font(fontBuffer: fontbuffer))
                            name_set.Add(font.Name);
                        font_buffers[key] = (name_set, xref_set, subsets);
                    }
                }
            }

            byte[] find_buffer_by_name(string name)
            {
                foreach (var kv in font_buffers)
                {
                    if (kv.Value.name_set.Contains(name))
                        return kv.Key.Buffer;
                }
                return null;
            }

            // -----------------
            // main function
            // -----------------
            repl_fontnames(this);  // populate font information
            if (font_buffers.Count == 0)  // nothing found to do
            {
                if (verbose)
                    Helpers.message("No fonts to subset.");
                return 0;
            }

            int old_fontsize = 0;
            int new_fontsize = 0;
            foreach (var kv in font_buffers)
                old_fontsize += kv.Key.Buffer.Length;

            // Scan page text for usage of subsettable fonts
            foreach (var page in this)
            {
                // go through the text and extend set of used glyphs by font
                // we use a modified MuPDF trace device, which delivers us glyph ids.
                foreach (var span in page.get_texttrace())
                {
                    if (span == null)  // skip useless information
                        continue;
                    string fontname = ((string)span["font"]).Substring(0, Math.Min(33, ((string)span["font"]).Length));  // fontname for the span
                    byte[] buffer = find_buffer_by_name(fontname);
                    if (buffer == null)
                        continue;
                    var key = new FontBufferKey(buffer);
                    var (name_set, xref_set, subsets) = font_buffers[key];
                    var (set_ucs, set_gid) = subsets;
                    foreach (var c in (IEnumerable)span["chars"])
                    {
                        var ch = (object[])c;
                        set_ucs.Add((int)ch[0]);  // unicode
                        set_gid.Add((int)ch[1]);  // glyph id
                    }
                    font_buffers[key] = (name_set, xref_set, (set_ucs, set_gid));
                }
            }

            // build the font subsets
            foreach (var kv in font_buffers)
            {
                byte[] old_buffer = kv.Key.Buffer;
                var (name_set, xref_set, subsets) = kv.Value;
                byte[] new_buffer = build_subset(old_buffer, subsets.set_ucs, subsets.set_gid);
                string fontname = name_set.First();
                if (new_buffer == null || new_buffer.Length >= old_buffer.Length)
                {
                    // subset was not created or did not get smaller
                    if (verbose)
                        Helpers.message($"Cannot subset '{fontname}'.");
                    continue;
                }
                if (verbose)
                    Helpers.message($"Built subset of font '{fontname}'.");
                object[] val = _insert_font(fontbuffer: new_buffer);  // store subset font in PDF
                int new_xref = (int)val[0];  // get its xref
                set_subset_fontname(new_xref);  // tag fontname as subset font
                string font_str = XrefObject(  // get its object definition
                    new_xref,
                    compressed: true);
                // walk through the original font xrefs and replace each by the subset def
                foreach (int font_xref in xref_set)
                {
                    // we need the original '/W' and '/DW' width values
                    var (width_table, def_width) = get_old_widths(font_xref);
                    // ... and replace original font definition at xref with it
                    UpdateObject(font_xref, font_str);
                    // now copy over old '/W' and '/DW' values
                    if (!string.IsNullOrEmpty(width_table) || !string.IsNullOrEmpty(def_width))
                        set_old_widths(font_xref, width_table, def_width);
                }
                // 'new_xref' remains unused in the PDF and must be removed
                // by garbage collection.
                new_fontsize += new_buffer.Length;
            }

            return old_fontsize - new_fontsize;
        }

        static void RunFontToolsSubsetCheckImport()
        {
            var psi = Helpers.CreatePythonProcessStartInfo("import fontTools.subset");
            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start python");
            proc.WaitForExit();
            if (proc.ExitCode != 0)
                throw new InvalidOperationException(proc.StandardError.ReadToEnd().Trim());
        }

        static void RunFontToolsSubsetMain(List<string> args)
        {
            var psi = Helpers.CreatePythonProcessStartInfo(
                "import fontTools.subset as fts, sys; fts.main(sys.argv[1:])",
                args.ToArray());
            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start python");
            proc.WaitForExit();
            if (proc.ExitCode != 0)
                throw new InvalidOperationException(proc.StandardError.ReadToEnd().Trim());
        }

        private readonly struct FontBufferKey : IEquatable<FontBufferKey>
        {
            public byte[] Buffer { get; }
            public FontBufferKey(byte[] buffer) => Buffer = buffer;
            public bool Equals(FontBufferKey other) =>
                StructuralComparisons.StructuralEqualityComparer.Equals(Buffer, other.Buffer);
            public override bool Equals(object obj) => obj is FontBufferKey other && Equals(other);
            public override int GetHashCode() =>
                StructuralComparisons.StructuralEqualityComparer.GetHashCode(Buffer);
        }
        /// <summary>
        /// Rewrites or recompresses images across the PDF.
        /// </summary>
        /// <remarks>PDF only: Walk through all images and rewrite them according to the specified parameters. This is useful for reducing file size, changing image formats, or converting color spaces. PyMuPDF <c>Document.rewrite_images</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="quality">desired target JPEG quality, a value between 0 and 100. 0 means no quality change, 100 means best quality.</param>
        /// <param name="dpiThreshold">See PyMuPDF parameter &lt;c&gt;dpiThreshold&lt;/c&gt;.</param>
        /// <param name="dpiTarget">See PyMuPDF parameter &lt;c&gt;dpiTarget&lt;/c&gt;.</param>
        /// <param name="lossy">include lossy image types (e.g. JPEG).</param>
        /// <param name="lossless">include lossless image types (e.g. PNG).</param>
        /// <param name="bitonal">include black-and-white images (e.g. FAX).</param>
        /// <param name="color">include colored images.</param>
        /// <param name="gray">include grayscale images.</param>
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
        /// Recolors all pages (PDF only).
        /// </summary>
        /// <remarks>PDF only: Change the color component counts for all object types text, images and vector graphics for all pages. PyMuPDF <c>Document.recolor</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="components">desired color space indicated by the number of color components: 1 = DeviceGRAY, 3 = DeviceRGB, 4 = DeviceCMYK.</param>
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
        /// Sets the document language (/Root/Lang).
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.set_language</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="language">Document language tag (e.g. en-US).</param>
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
        /// Gets PDF only: get/set `/NeedAppearances` property.
        /// </summary>
        /// <value>PDF only: get/set `/NeedAppearances` property</value>
        /// <remarks>PDF only: Get or set the */NeedAppearances* property of Form PDFs. Quote: *"(Optional) A flag specifying whether to construct appearance streams and appearance dictionaries for all widget annotations in the document ... Default value: false."* This may help controlling the behavior of some readers / viewers. PyMuPDF <c>Document.need_appearances</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
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
                    var na = Helpers.PdfDictGets(acro, "NeedAppearances");
                    return na.m_internal != null && mupdf.mupdf.pdf_to_bool(na) != 0;
                }
                catch { return false; }
            }
        }
        /// <summary>
        /// See PyMuPDF Document.set_need_appearances.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.set_need_appearances</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
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
        /// Gets PDF MarkInfo value.
        /// </summary>
        /// <value>PDF MarkInfo value</value>
        /// <remarks>PyMuPDF <c>Document.markinfo</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public Dictionary<string, bool> MarkInfo
        {
            get
            {
                var result = new Dictionary<string, bool>();
                try
                {
                    var pdf = NativePdfDocument;
                    var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                    var mi = Helpers.PdfDictGets(root, "MarkInfo");
                    if (mi.m_internal == null) return result;
                    foreach (var key in new[] { "Marked", "UserProperties", "Suspects" })
                    {
                        var val = Helpers.PdfDictGets(mi, key);
                        result[key] = val.m_internal != null && mupdf.mupdf.pdf_to_bool(val) != 0;
                    }
                }
                catch { }
                return result;
            }
        }
        /// <summary>
        /// Sets PDF MarkInfo dictionary values.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.set_mark_info</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="Dictionary<string">See PyMuPDF parameter &lt;c&gt;Dictionary&lt;string&lt;/c&gt;.</param>
        /// <param name="markinfo">See PyMuPDF parameter &lt;c&gt;markinfo&lt;/c&gt;.</param>
        /// <returns><see langword="true"/> if the operation succeeded.</returns>
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
        /// Gets PDF PageLayout value.
        /// </summary>
        /// <value>PDF PageLayout value</value>
        /// <remarks>PyMuPDF <c>Document.pagelayout</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public string PageLayout
        {
            get
            {
                try
                {
                    var pdf = NativePdfDocument;
                    var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                    var val = Helpers.PdfDictGets(root, "PageLayout");
                    return val.m_internal != null ? mupdf.mupdf.pdf_to_name(val) : "SinglePage";
                }
                catch { return "SinglePage"; }
            }
        }
        /// <summary>
        /// PDF only: set the PageLayout
        /// </summary>
        /// <remarks>PDF only: Set the `/PageLayout`. PyMuPDF <c>Document.set_pagelayout</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="layout">Page layout string for PDF PageLayout.</param>
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
                    // self.XrefSetKey(xref, "PageLayout", f"/{v}")
                    XrefSetKey(xref, "PageLayout", "/" + v);
                    return;
                }
            }
            throw new ValueErrorException("bad PageLayout value");
        }
        /// <summary>
        /// Gets PDF PageMode value.
        /// </summary>
        /// <value>PDF PageMode value</value>
        /// <remarks>PyMuPDF <c>Document.pagemode</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public string PageMode
        {
            get
            {
                try
                {
                    var pdf = NativePdfDocument;
                    var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
                    var val = Helpers.PdfDictGets(root, "PageMode");
                    return val.m_internal != null ? mupdf.mupdf.pdf_to_name(val) : "UseNone";
                }
                catch { return "UseNone"; }
            }
        }
        /// <summary>
        /// PDF only: set the PageMode
        /// </summary>
        /// <remarks>PDF only: Set the `/PageMode`. PyMuPDF <c>Document.set_pagemode</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="mode">See PyMuPDF parameter &lt;c&gt;mode&lt;/c&gt;.</param>
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
                    // self.XrefSetKey(xref, "PageMode", f"/{v}")
                    XrefSetKey(xref, "PageMode", "/" + v);
                    return;
                }
            }
            throw new ValueErrorException("bad PageMode value");
        }

        // ─── Signature flags ────────────────────────────────────────────
        /// <summary>
        /// PDF only: determine signature state
        /// </summary>
        /// <remarks>PDF only: Return whether the document contains signature fields. This is an optional PDF property: if not present (return value -1), no conclusions can be drawn -- the PDF creator may just not have bothered using it. PyMuPDF <c>Document.get_sigflags</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns>A non-negative result code or xref number.</returns>
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
        /// PDF only: the unrotated page rectangle
        /// </summary>
        /// <remarks>PDF only: Return the unrotated page rectangle -- without loading the page (via <see cref="LoadPage"/>). This is meant for internal purpose requiring best possible performance. PyMuPDF <c>Document.page_cropbox</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">0-based page number.</param>
        /// <returns>Rect of the page like <see cref="Page.Rect"/>, but ignoring any rotation.</returns>
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
            var cropbox = Helpers.PdfDictGetInheritable(page_obj, mupdf.mupdf.pdf_new_name("CropBox"));
            if (cropbox.m_internal != null)
            {
                var r = mupdf.mupdf.pdf_to_rect(cropbox);
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
            var mb = Helpers.PdfDictGetInheritable(page_obj, mupdf.mupdf.pdf_new_name("MediaBox"));
            if (mb.m_internal != null)
            {
                var r = mupdf.mupdf.pdf_to_rect(mb);
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
            return new Rect(0, 0, 595, 842);
        }
        /// <summary>
        /// PDF only: Saves a "snapshot" of the document. This is a PDF document with a special, incremental-save format compatible with journalling -- therefore no save options are available. Saving a snapshot is not possible for new documents.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.save_snapshot</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="filename">File path to open or save.</param>
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
        /// PDF only: Saves a "snapshot" of the document. This is a PDF document with a special, incremental-save format compatible with journalling -- therefore no save options are available. Saving a snapshot is not possible for new documents.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.save_snapshot</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="filename">File path to open or save.</param>
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
            else if (filename is global::System.IO.FileInfo fi)
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
        /// Closes the document and releases native resources.
        /// </summary>
        /// <remarks>Release objects and space allocations associated with the document. If created from a file, also closes *filename* (releasing control to the OS). Explicitly closing a document is equivalent to deleting it, <c>del doc</c>, or assigning it to something else like <c>doc = None</c>. PyMuPDF <c>Document.close</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <exception cref="ValueErrorException">Document is closed, encrypted, or arguments are invalid.</exception>
        public void Close()
        {
            if (!IsClosed)
            {
                if (_outline != null)
                    _outline = null;
                // PyMuPDF Document.close() -> _reset_page_refs() invalidates all Page wrappers.
                ResetPageRefsInternal(erasePages: true);
                Graftmaps.Clear();
                IsClosed = true;
                DisposeCachedPdfDocument();
                lock (Utils.MuPDFLock)
                {
                    _nativeDoc?.Dispose();
                }
                _nativeDoc = null;
                StreamData = null;
                if (!_disposed)
                {
                    _disposed = true;
                    ThisOwn = false;
                    GC.SuppressFinalize(this);
                }
            }
        }

        /// <summary>Write PDF bytes to <paramref name="path"/> with an explicit close (PyMuPDF stream-then-flush pattern).</summary>
        private static void WriteBytesToFile(string path, byte[] data)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                fs.Write(data, 0, data.Length);
                fs.Flush(true);
            }
        }

        /// <summary>Drop MuPDF's cached page tree after page content edits (widgets, TextWriter, etc.).</summary>
        internal void InvalidatePageTree()
        {
            DropPdfPageTreeIfPdf();
        }

        private void DropPdfPageTreeIfPdf()
        {
            if (IsClosed || _nativeDoc == null)
                return;
            try
            {
                if (!IsPdf)
                    return;
                var pdf = NativePdfDocument;
                if (pdf.m_internal != null)
                    mupdf.mupdf.ll_pdf_drop_page_tree_internal(pdf.m_internal);
            }
            catch
            {
                // Best-effort; native handle may already be invalid.
            }
        }

        internal static int NextPageRefId() => Interlocked.Increment(ref _nextPageRefId);

        internal void RegisterPageRef(Page page)
        {
            if (page == null) return;
            lock (_pageRefs)
                _pageRefs[page.PageRefId] = page;
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
        /// <summary>
        /// Re-open the native PDF from current <see cref="Write()"/> bytes so MuPDF sees latest xref edits.
        /// </summary>
        internal void SyncNativePdfFromMemory()
        {
            if (IsClosed || IsEncrypted || !IsPdf)
                return;
            byte[] data = Write();
            DisposeCachedPdfDocument();
            _nativeDoc?.Dispose();
            _nativeDoc = null;
            _nativeDoc = OpenNativeFromBytes(data, "pdf");
            StreamData = data;
            ResetPageRefsInternal();
        }

        internal void ResetPageRefsInternal(bool erasePages = false)
        {
            if (IsClosed || IsEncrypted)
                return;

            List<Page> pages;
            lock (_pageRefs)
            {
                pages = erasePages ? new List<Page>(_pageRefs.Values) : null;
                _pageRefs.Clear();
            }

            if (erasePages)
            {
                for (int i = 0; i < pages.Count; i++)
                    pages[i]?._erase();
            }
        }

        // ─── Internal ───────────────────────────────────────────────────

        private void InitDoc()
        {
            if (IsEncrypted)
                return;
            _outline = _loadOutline();
            _metadata = GetMetadata();
            if (_metadata.TryGetValue("encryption", out string enc) && enc == "None")
                _metadata["encryption"] = "";
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

        // ─── IDisposable ────────────────────────────────────────────────
        /// <summary>
        /// Releases the document; prefer Close() for PDF semantics.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.dispose</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void Dispose()
        {
            if (!_disposed) { Close(); _disposed = true; ThisOwn = false; }
            GC.SuppressFinalize(this);
        }

        ~Document() { Dispose(); }

        // ─── IEnumerable<Page> ──────────────────────────────────────────
        /// <summary>
        /// Returns an enumerator over all pages.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.get_enumerator</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <returns>A <see cref="Page"/> instance.</returns>
        public IEnumerator<Page> GetEnumerator()
        {
            for (int i = 0; i < PageCount; i++)
                yield return LoadPage(i);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();



        // ─── PyMuPDF API names (internal, same assembly) ─────────────────
        // Ported from Document.PythonCompat.cs: snake_case / dunder / legacy aliases.
        // Public callers should use the PascalCase members on this class.

        /// <summary>Python <c>fitz.open()</c> / <c>fitz.open(path)</c> / memory open.</summary>
        internal static Document open() => Open();

        internal static Document open(string filename, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            => Open(filename, filetype, rect, width, height, fontsize);

        internal static Document open(byte[] data, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            => Open(data, filetype, rect, width, height, fontsize);

        internal static Document open(Stream stream, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            => Open(stream, filetype, rect, width, height, fontsize);

        /// <summary>PyMuPDF <c>g_use_extra</c>.</summary>
        private static bool g_use_extra => true;

        // Python-style wrappers.
        /// <summary>PyMuPDF <c>Document.page_count</c> property.</summary>
        internal int page_count()
        {
            // """Number of pages."""
            // if self.is_closed:
            if (IsClosed)
                // raise ValueError('document closed')
                throw new ValueErrorException("document closed");
            // if g_use_extra:
            if (GUseExtra)
                // return self.page_count2(self)
                return page_count2(this);
            // if isinstance( self.this, mupdf.FzDocument):
            if (_nativeDoc is mupdf.FzDocument)
                // return mupdf.fz_count_pages( self.this)
                return mupdf.mupdf.fz_count_pages((mupdf.FzDocument)_nativeDoc);
            // else:
            // return mupdf.pdf_count_pages( self.this)
            return mupdf.mupdf.pdf_count_pages(NativePdfDocument);
        }

        /// <summary>PyMuPDF <c>Document.page_count2</c> — <c>extra.page_count_pdf</c> or <c>extra.page_count_fz</c>.</summary>
        private static int page_count2(Document self) =>
            self.IsPdf ? page_count_pdf(self) : page_count_fz(self);

        /// <summary>PyMuPDF <c>extra.page_count_fz</c> (<c>src/extra.i</c>).</summary>
        private static int page_count_fz(Document self) =>
            mupdf.mupdf.fz_count_pages(self.NativeDocument);

        /// <summary>PyMuPDF <c>extra.page_count_pdf</c> (<c>src/extra.i</c>).</summary>
        private static int page_count_pdf(Document self)
        {
            // mupdf::FzDocument document = pdf.super();
            // return page_count_fz(document);
            return page_count_fz(self);
        }

        /// <summary>Python <c>Document.this is None</c> after <see cref="Close"/>.</summary>
        internal bool py_this_is_none() => _nativeDoc == null;
        internal bool is_pdf() => IsPdf;
        internal int chapter_count() => ChapterCount;
        internal bool needs_pass() => NeedsPass;
        internal bool is_reflowable() => IsReflowable;
        internal bool is_closed() => IsClosed;
        internal bool is_encrypted() => IsEncrypted;
        /// <summary>Python <c>Document.init_doc()</c> — safe to call after <c>authenticate()</c>; throws if the document is still encrypted.</summary>
        internal void init_doc()
        {
            if (IsEncrypted)
                throw new ValueErrorException("cannot initialize - document still encrypted");
            InitDoc();
        }
        internal bool is_dirty() => IsDirty;
        internal bool is_form_pdf() => IsFormPdf;
        internal bool is_fast_webaccess() => IsFastWebaccess;
        internal bool is_repaired() => IsRepaired;
        internal (int, int) last_location() => LastLocation;
        internal bool journal_is_enabled() => JournalIsEnabled;
        internal string pagelayout() => PdfCatalog == 0 ? null : PageLayout;
        internal string pagemode() => PdfCatalog == 0 ? null : PageMode;
        internal int permissions() => Permissions;
        internal string name() => Name;
        internal string language() => Language;
        internal int version_count() => VersionCount;
        internal int xref_length() => XrefLength;
        internal int pdf_catalog() => PdfCatalog;
        internal Dictionary<string, bool> markinfo() => MarkInfo;
        internal bool set_markinfo(Dictionary<string, object> markinfo) => SetMarkInfo(markinfo);
        internal bool has_annots() => HasAnnots();
        internal bool has_links() => HasLinks();

        // Python dunder compatibility helpers.
        internal int __len__() => PageCount;
        internal bool __contains__(int page_number) => ContainsPage(page_number);
        internal bool __contains__((int chapter, int page) loc) => ContainsLocation(loc);
        internal bool contains_chapter_page(int chapter, int page_in_chapter) => ContainsChapterPage(chapter, page_in_chapter);
        internal bool contains_location((int chapter, int page) loc) => ContainsLocation(loc);
        internal string __repr__() => ToString();
        internal Page __getitem__(int page_number) => GetItemPageForIndexer(page_number);
        internal Page __getitem__((int chapter, int page) loc) => GetItemPageForIndexer(loc.chapter, loc.page);

        /// <summary>Python <c>with fitz.open(...) as doc:</c> — returns self; exit closes the document.</summary>
        internal Document __enter__() => this;

        /// <summary>Python <c>Document.__exit__</c> (exception args ignored).</summary>
        internal void __exit__(object exc_type = null, object exc_value = null, object traceback = null) => Close();

        // Page loading/navigation.
        internal Page load_page(int page_id) => LoadPage(page_id);
        internal Page load_page(int chapter, int pno) => LoadPage(chapter, pno);
        internal IEnumerable<Page> pages(int? start = null, int? stop = null, int? step = null) => Pages(start, stop, step);
        internal (int chapter, int pageInChapter) next_location((int chapter, int page) loc) => NextLocation(loc);
        internal (int chapter, int pageInChapter) prev_location((int chapter, int page) loc) => PrevLocation(loc);
        internal (int chapter, int page) location_from_page_number(int pno) => LocationFromPageNumber(pno);
        internal int page_number_from_location((int chapter, int page) loc) => PageNumberFromLocation(loc);
        internal int chapter_page_count(int chapter) => ChapterPageCount(chapter);
        internal ulong make_bookmark((int chapter, int page) loc) => MakeBookmark(loc);
        internal (int chapter, int page) find_bookmark(ulong bm) => FindBookmark(bm);

        // Metadata and outlines.
        internal List<(int level, string title, int page, Dictionary<string, object> link)> get_toc(bool simple = true) => GetToc(simple);
        internal int set_toc(IList<object> toc, int collapse = 1) => SetToc(toc, collapse);
        internal void set_toc_item(int idx, Dictionary<string, object> dest_dict = null, int? kind = null, int? pno = null,
            string uri = null, string title = null, Point to = null, string filename = null, float zoom = 0)
            => SetTocItem(idx, dest_dict, kind, pno, uri, title, to, filename, zoom);
        internal void del_toc_item(int idx) => DeleteTocItem(idx);
        internal Outline get_outline() => _outline;
        internal List<int> get_outline_xrefs() => GetOutlineXrefs();

        // Page editing.
        internal Page new_page(int pno = -1, float width = 595, float height = 842) => NewPage(pno, width, height);
        internal void delete_page(int pno = -1) => DeletePage(pno);
        internal void delete_pages(params int[] pages) => DeletePages(pages);
        internal void delete_pages(int from_page, int to_page) => DeletePages(from_page, to_page);
        internal void delete_pages_by_slice(int start, int stop, int step = 1) => DeletePagesBySlice(start, stop, step);
        internal List<Page> load_pages_by_slice(int start, int stop, int step = 1) => LoadPagesBySlice(start, stop, step);
        internal void __delitem__(int page_number)
        {
            EnsurePdf();
            DeletePage(page_number);
        }
        internal void __delitem__(int[] pages)
        {
            EnsurePdf();
            DeletePages(pages);
        }
        internal Page insert_page(int pno = -1, string text = null, float fontsize = 11, float width = 595, float height = 842, string fontname = "helv", float[] color = null)
            => InsertPage(pno, text, fontsize, width, height, fontname, color);
        internal void copy_page(int pno, int to = -1) => CopyPage(pno, to);
        internal void fullcopy_page(int pno, int to = -1) => FullCopyPage(pno, to);
        internal void move_page(int pno, int to = -1) => MovePage(pno, to);
        internal void select(int[] pages) => Select(pages);
        internal Page reload_page(Page page) => ReloadPage(page);

        internal byte[] tobytes(bool garbage = false, bool clean = false, bool deflate = false) => ToBytes(garbage, clean, deflate);
        internal byte[] tobytes(int garbage = 0, int clean = 0, int deflate = 0)
        {
            using var ms = new MemoryStream();
            Save(ms, garbage: garbage, clean: clean, deflate: deflate);
            return ms.ToArray();
        }
        internal byte[] convert_to_pdf(int from_page = 0, int to_page = -1, int rotate = 0) => ConvertToPdf(from_page, to_page, rotate);
        internal void save_incr() => SaveIncr();
        internal void saveIncr() => SaveIncr();
        internal bool can_save_incrementally() => CanSaveIncrementally();
        internal bool authenticate(string password) => Authenticate(password) != 0;
        internal void ez_save(string filename, int garbage = 1, int clean = 0, int deflate = 1,
            int deflate_images = 1, int deflate_fonts = 1, int incremental = 0)
            => EzSave(filename, garbage, clean, deflate, deflate_images, deflate_fonts, incremental);

        internal void save(object filename, bool expand = false, bool pretty = false)
            => Save(filename, expand: expand ? 1 : 0, pretty: pretty ? 1 : 0);

        // Xref/object APIs.
        internal int page_xref(int pno) => PageXref(pno);
        internal string xref_object(int xref, bool compressed = false, bool ascii = false) => XrefObject(xref, compressed, ascii);
        internal bool xref_is_stream(int xref = 0) => XrefIsStream(xref);
        internal bool xref_is_font(int xref) => XrefIsFont(xref);
        internal bool xref_is_image(int xref) => XrefIsImage(xref);
        internal bool xref_is_xobject(int xref) => XrefIsXobject(xref);
        internal byte[] xref_stream(int xref) => XrefStream(xref);
        internal byte[] xref_stream_raw(int xref) => XrefStreamRaw(xref);
        internal (string type, string value) xref_get_key(int xref, string key) => XrefGetKey(xref, key);
        internal List<string> xref_get_keys(int xref) => XrefGetKeys(xref);
        internal void xref_set_key(int xref, string key, string value) => XrefSetKey(xref, key, value);
        internal void _deleteObject(int xref)
        {
            if (xref < 1 || xref >= XrefLength)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);
            mupdf.mupdf.pdf_delete_object(NativePdfDocument, xref);
        }
        internal int xref() => GetNewXref();
        internal int get_new_xref() => GetNewXref();

        // XML metadata.
        internal string xref_xml_metadata() => GetXmlMetadata();
        internal string get_xml_metadata() => GetXmlMetadata();
        internal void set_xml_metadata(string metadata) => SetXmlMetadata(metadata);
        internal void del_xml_metadata() => DeleteXmlMetadata();

        // Object / stream updates.
        internal void update_object(int xref, string text, Page page = null) => UpdateObject(xref, text, page);
        internal void update_stream(int xref, byte[] stream, bool compress = true) => UpdateStream(xref, stream, compress);

        /// <summary>Python <c>UpdateStream(xref, stream, new=1, compress=1)</c> when <c>new</c> and <c>compress</c> are passed as ints (e.g. <c>0</c>/<c>1</c>). <paramref name="new_"/> is ignored (unused in PyMuPDF).</summary>
        internal void UpdateStream(int xref, byte[] stream, int new_, int compress)
            => UpdateStream(xref, stream, compress != 0);
        internal static void xref_copy(Document doc, int source, int target, List<string> keep = null)
            => Document.XrefCopy(doc, source, target, keep);
        internal string pdf_trailer(bool compressed = false, bool ascii = false) => PdfTrailer(compressed, ascii);

        // Embedded files (Python-name aliases).
        internal List<string> embfile_names() => GetEmbeddedFileNames();
        internal byte[] embfile_get(string name) => GetEmbeddedFile(name);
        internal byte[] embfile_get(int idx) => GetEmbeddedFile(idx);
        internal byte[] embfile_get_by_index(int idx) => GetEmbeddedFile(idx);
        internal int embfile_add(string name, byte[] buffer_, string filename = null, string ufilename = null, string desc = null)
            => AddEmbeddedFile(name, buffer_, filename, ufilename, desc);
        internal Dictionary<string, object> embfile_info(string item) => GetEmbeddedFileInfo(item);
        internal Dictionary<string, object> embfile_info(int item) => GetEmbeddedFileInfo(item);
        internal int embfile_upd(string item, byte[] buffer_ = null, string filename = null, string ufilename = null, string desc = null)
            => UpdateEmbeddedFile(item, buffer_, filename, ufilename, desc);
        internal int embfile_upd(int item, byte[] buffer_ = null, string filename = null, string ufilename = null, string desc = null)
            => UpdateEmbeddedFile(item, buffer_, filename, ufilename, desc);
        internal void embfile_del(string name) => DeleteEmbeddedFile(name);
        internal void embfile_del(int idx) => DeleteEmbeddedFile(idx);
        internal int embfile_count() => EmbeddedFileCount;

        // Page resource extraction helpers.
        internal List<(int xref, string ext, string type, string baseName, string name, string encoding, int? referencer)> get_page_fonts(int pno, bool full = false) => GetPageFonts(pno, full);
        internal List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)> get_page_images(int pno, bool full = false) => GetPageImageRows(pno, full);
        /// <summary>
        /// Python-shape compatibility helper for <c>get_page_fonts</c>.
        /// Returns list entries as object arrays so callers can compare tuple lengths:
        /// <c>full=False</c> => 6 fields, <c>full=True</c> => 7 fields (last entry is referencer / stream xref).
        /// </summary>
        internal List<object[]> get_page_fonts_py(int pno, bool full = false)
        {
            var val = _getPageInfo(pno, 1);
            var ret = new List<object[]>(val.Count);
            foreach (var o in val)
            {
                var t = ((int xref, string ext, string type, string baseName, string name, string encoding, int streamXref))o;
                if (full)
                    ret.Add(new object[] { t.xref, t.ext, t.type, t.baseName, t.name, t.encoding, t.streamXref });
                else
                    ret.Add(new object[] { t.xref, t.ext, t.type, t.baseName, t.name, t.encoding });
            }
            return ret;
        }

        /// <summary>
        /// Python-shape compatibility helper for <c>get_page_images</c>.
        /// Returns list entries as object arrays so callers can compare tuple lengths:
        /// <c>full=False</c> => 9 fields, <c>full=True</c> => 10 fields (last entry is referencer / stream xref).
        /// </summary>
        internal List<object[]> get_page_images_py(int pno, bool full = false)
        {
            var val = _getPageInfo(pno, 2);
            var ret = new List<object[]>(val.Count);
            foreach (var o in val)
            {
                var t = ((int xref, int gen, int width, int height, int bpc, string colorspace, string altCs, string name, string filter, int streamXref))o;
                string sm = t.gen.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (full)
                    ret.Add(new object[] { t.xref, sm, t.width, t.height, t.bpc, t.colorspace, t.altCs, t.name, t.filter, t.streamXref });
                else
                    ret.Add(new object[] { t.xref, sm, t.width, t.height, t.bpc, t.colorspace, t.altCs, t.name, t.filter });
            }
            return ret;
        }
        internal List<(int glyph, float width)> get_char_widths(int xref, int limit = 256, int idx = 0, Dictionary<string, object> fontdict = null)
            => GetCharWidths(xref, limit, idx, fontdict);
        internal List<(int xref, AnnotationType type, string id)> page_annot_xrefs(int n) => GetPageAnnotXrefs(n);
        internal List<Dictionary<string, object>> get_page_xobjects(int pno)
        {
            var list = _getPageInfo(pno, 3);
            var r = new List<Dictionary<string, object>>(list.Count);
            foreach (var o in list)
            {
                var t = ((int xref, string name, int streamXref, Rect bbox))o;
                r.Add(new Dictionary<string, object>
                {
                    ["xref"] = t.xref,
                    ["name"] = t.name,
                    ["subtype"] = "Form",
                    ["stream_xref"] = t.streamXref,
                    ["bbox"] = t.bbox,
                });
            }
            return r;
        }
        internal (string name, string ext, string type, byte[] content) extract_font(int xref) => ExtractFont(xref);
        internal Dictionary<string, object> extract_image(int xref) => ExtractImageDict(xref);
        internal List<Dictionary<string, object>> get_page_labels() => GetPageLabels();
        internal void set_page_labels(List<Dictionary<string, object>> labels) => SetPageLabels(labels);
        internal List<int> get_page_numbers(string label, bool only_one = false) => GetPageNumbers(label, only_one);
        internal List<Quad> search_page_for(int pno, string needle, int max_hits = 16, Quad clip = null, int flags = 0, TextPage textpage = null)
            => SearchPageFor(pno, needle, max_hits, clip, flags, textpage);

        internal List<Rect> search_page_for_rects(int pno, string needle, int max_hits = 16, Quad clip = null, int flags = 0, TextPage textpage = null)
            => SearchPageForRects(pno, needle, max_hits, clip, flags, textpage);
        internal Pixmap get_page_pixmap(int pno, Matrix matrix = null, Colorspace cs = null, bool alpha = false, IRect clip = null)
            => GetPagePixmap(pno, matrix, cs, alpha, clip);
        internal string get_page_text(int pno, string option = "text", int flags = 0)
            => (string)GetPageText(pno, option, flags);

        // Layout and journalling.
        internal void layout(float width = 400, float height = 600, float fontsize = 11) => Layout(width, height, fontsize);
        internal void layout(Rect rect, float fontsize = 11) => Layout(rect, fontsize);
        internal void journal_enable() => JournalEnable();
        internal Dictionary<string, bool> journal_can_do()
        {
            var state = JournalCanDo();
            return new Dictionary<string, bool>
            {
                ["undo"] = state.canUndo,
                ["redo"] = state.canRedo
            };
        }
        internal bool journal_undo()
        {
            JournalUndo();
            return true;
        }
        internal bool journal_redo()
        {
            JournalRedo();
            return true;
        }
        internal void journal_start_op(string name = null) => JournalStartOp(name);
        internal void journal_stop_op() => JournalStopOp();
        internal void journal_save(string filename) => JournalSave(filename);
        internal void journal_load(string filename) => JournalLoad(filename);
        internal void journal_load(byte[] data) => JournalLoad(data);
        internal string journal_op_name(int step) => JournalOpName(step);
        internal (int rc, int steps) journal_position() => JournalPosition();
        internal void repair() => Repair();

        // Merge and layers.
        internal void insert_pdf(Document src, int from_page = -1, int to_page = -1, int start_at = -1, int rotate = -1, bool links = true, bool annots = true, bool widgets = true, bool join_duplicates = false, int show_progress = 0, int final = 1)
            => InsertPdf(src, from_page, to_page, start_at, rotate, links, annots, widgets, join_duplicates, show_progress, final);
        internal void insert_file(object infile, int from_page = -1, int to_page = -1, int start_at = -1, int rotate = -1, bool links = true, bool annots = true, int show_progress = 0, int final = 1)
            => InsertFile(infile, from_page, to_page, start_at, rotate, links, annots, show_progress, final);
        internal Dictionary<int, Dictionary<string, object>> get_ocgs() => GetOcgs();
        internal Dictionary<string, object> get_layer(int config = -1) => GetLayer(config);
        internal List<Dictionary<string, object>> get_layers() => GetLayers();
        internal int get_oc(int xref) => GetOc(xref);
        internal Dictionary<string, object> get_ocmd(int xref) => GetOcmd(xref);
        internal void set_oc(int xref, int oc) => SetOc(xref, oc);
        internal int set_ocmd(int xref = 0, List<int> ocgs = null, string policy = null, object ve = null) => SetOcmd(xref, ocgs, policy, ve);
        internal Dictionary<string, Dictionary<string, object>> resolve_names() => ResolveNames();
        internal void add_layer(string name, string creator = null, bool on = true) => AddLayer(name, creator, on);
        internal int add_ocg(string name, int config = -1, int on = 1, string intent = null, string usage = null) => AddOcg(name, config, on != 0, intent, usage);
        internal void set_layer(int config, string basestate = null, object on = null, object off = null, object rbgroups = null, object locked = null)
            => SetLayer(config, basestate, on, off, rbgroups, locked);
        internal void switch_layer(int config, bool as_default = false) => SwitchLayer(config, as_default);
        internal List<Dictionary<string, object>> layer_ui_configs() => LayerUiConfigs();
        internal void set_layer_ui_config(object number, int action = 0) => SetLayerUiConfig(number, action);

        // Cleanup and misc.
        internal void bake(bool annots = true, bool widgets = true) => Bake(annots, widgets);
        internal void scrub(bool attached_files = true, bool clean_pages = true, bool embedded_files = true,
            bool hidden_text = true, bool javascript = true, bool metadata = true, bool redactions = true, int redact_images = 0, bool remove_links = true, bool reset_fields = true, bool reset_responses = true, bool thumbnails = true, bool xml_metadata = true)
            => Scrub(attached_files, clean_pages, embedded_files, hidden_text, javascript, metadata, redactions, redact_images, remove_links, reset_fields, reset_responses, thumbnails, xml_metadata);
        internal (object page_id, float x, float y) resolve_link(string uri = null, bool chapters = false) => ResolveLink(uri, chapters);
        internal int? subset_fonts(bool verbose = false, bool fallback = false) => SubsetFonts(verbose, fallback);
        internal void recolor(int components = 1) => Recolor(components);
        internal void rewrite_images(int quality = 0, int dpi_threshold = 0, int dpi_target = 0, bool lossy = true, bool lossless = true, bool bitonal = true, bool color = true, bool gray = true)
            => RewriteImages(quality, dpi_threshold, dpi_target, lossy, lossless, bitonal, color, gray);
        internal bool set_language(string language)
        {
            SetLanguage(language);
            return true;
        }
        internal void set_need_appearances(bool value) => SetNeedAppearances(value);
        internal bool? need_appearances(bool? value = null)
        {
            if (!IsFormPdf)
                return null;
            var pdf = NativePdfDocument;
            var form = Helpers.PdfDictGetp(mupdf.mupdf.pdf_trailer(pdf), "Root/AcroForm");
            var app = form.m_internal != null ? Helpers.PdfDictGets(form, "NeedAppearances") : new mupdf.PdfObj();
            bool old = app.m_internal != null && mupdf.mupdf.pdf_is_bool(app) != 0;
            if (value.HasValue)
                SetNeedAppearances(value.Value);
            return value ?? old;
        }
        internal bool set_pagelayout(string layout)
        {
            SetPageLayout(layout);
            return true;
        }
        internal bool set_pagemode(string mode)
        {
            SetPageMode(mode);
            return true;
        }
        internal int get_sigflags() => GetSigFlags();
        internal Rect page_cropbox(int pno) => PageCropBox(pno);
        internal void save_snapshot(string filename) => SaveSnapshot(filename);
        internal void save_snapshot(object filename) => SaveSnapshot(filename);
        internal void close() => Close();

        // Private Python helper-name parity.
        internal List<int> _delToC()
        {
            var xrefs = GetOutlineXrefs();
            foreach (var xref in xrefs)
                RemoveTocItemByXref(xref);
            return xrefs;
        }
        internal void _remove_toc_item(int xref) => RemoveTocItemByXref(xref);
        internal void _update_toc_item(int xref, string action = null, string title = null, int flags = 0, bool? collapse = null, float[] color = null)
            => UpdateTocItemByXref(xref, action, title, flags, collapse, color);
        internal string _getMetadata(string key)
        {
            try { return mupdf.mupdf.fz_lookup_metadata2(NativeDocument, key); }
            catch { return ""; }
        }
        internal int _getOLRootNumber()
        {
            var pdf = NativePdfDocument;
            var root = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));
            var olroot = Helpers.PdfDictGet(root, mupdf.mupdf.pdf_new_name("Outlines"));
            if (olroot.m_internal == null)
            {
                olroot = mupdf.mupdf.pdf_new_dict(pdf, 4);
                mupdf.mupdf.pdf_dict_put(olroot, mupdf.mupdf.pdf_new_name("Type"), mupdf.mupdf.pdf_new_name("Outlines"));
                var indObj = mupdf.mupdf.pdf_add_object(pdf, olroot);
                mupdf.mupdf.pdf_dict_put(root, mupdf.mupdf.pdf_new_name("Outlines"), indObj);
                olroot = Helpers.PdfDictGet(root, mupdf.mupdf.pdf_new_name("Outlines"));
            }
            return mupdf.mupdf.pdf_to_num(olroot);
        }
        internal List<string> _getPDFfileid()
        {
            var ret = new List<string>();
            var pdf = NativePdfDocument;
            var identity = Helpers.PdfDictGet(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("ID"));
            if (identity.m_internal == null) return ret;
            int n = mupdf.mupdf.pdf_array_len(identity);
            for (int i = 0; i < n; i++)
            {
                var o = mupdf.mupdf.pdf_array_get(identity, i);
                string text = mupdf.mupdf.pdf_to_text_string(o) ?? "";
                ret.Add(BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(text)).Replace("-", "").ToLowerInvariant());
            }
            return ret;
        }
        /// <summary>PyMuPDF <c>Document._getPageInfo(self, pno, what)</c>: single path via <see cref="JM_scan_resources"/>; <paramref name="what"/> is 1 fonts / 2 images / 3 form XObjects (or string containing font/image).</summary>
        internal List<object> _getPageInfo(int pno, object what)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            int wi;
            if (what is string ws)
            {
                ws = ws.ToLowerInvariant();
                if (ws.Contains("font"))
                    wi = 1;
                else if (ws.Contains("image"))
                    wi = 2;
                else
                    wi = 3;
            }
            else
                wi = Convert.ToInt32(what);
            if (!IsPdf)
                return new List<object>();
            int n = _normalize_pno_for_get_page_info(pno);
            var pdf = NativePdfDocument;
            var pageref = mupdf.mupdf.pdf_lookup_page_obj(pdf, n);
            var rsrc = Helpers.PdfDictGetInheritable(pageref, mupdf.mupdf.pdf_new_name("Resources"));
            var liste = new List<object>();
            var tracer = new List<int>();
            if (rsrc.m_internal != null)
                JM_scan_resources(pdf, rsrc, liste, wi, 0, tracer);
            return liste;
        }
        /// <summary>
        /// Python-shape compatibility overload for <c>_getPageInfo</c>.
        /// </summary>
        internal List<object[]> _getPageInfo_py(int pno, int what, bool full = true)
        {
            if (what == 1) return get_page_fonts_py(pno, full);
            if (what == 2) return get_page_images_py(pno, full);
            var list = _getPageInfo(pno, 3);
            var ret = new List<object[]>(list.Count);
            foreach (var o in list)
            {
                var t = ((int xref, string name, int streamXref, Rect bbox))o;
                ret.Add(new object[] { t.xref, t.name, t.streamXref, t.bbox });
            }
            return ret;
        }
        internal object[] _insert_font(string fontfile = null, byte[] fontbuffer = null)
        {
            // Utility: insert font from file or binary (PyMuPDF Document path uses JM_insert_font).
            return Helpers.JM_insert_font(NativePdfDocument, this, null, fontfile, fontbuffer,
                set_simple: false, idx: 0, wmode: 0, serif: 0, encoding: 0, ordering: -1);
        }
        internal void _forget_page(Page page) => ForgetPageRef(page);
        internal void _reset_page_refs() => ResetPageRefsInternal();
        internal void _remove_links_to(object numbers)
        {
            var refs = new HashSet<int>();
            if (numbers is IEnumerable<int> ints)
            {
                foreach (var n in ints) refs.Add(n);
            }
            else if (numbers is IEnumerable<object> objs)
            {
                foreach (var o in objs) refs.Add(Convert.ToInt32(o));
            }
            else if (numbers is int n)
            {
                refs.Add(n);
            }
            else
            {
                throw new ArgumentException("bad page number(s)");
            }
            Helpers.JM_remove_dest_range(NativePdfDocument, refs);
        }
        internal void _addFormFont(string name, string font)
        {
            if (IsClosed || IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            var pdf = NativePdfDocument;
            var fonts = Helpers.PdfDictGetl(
                mupdf.mupdf.pdf_trailer(pdf),
                mupdf.mupdf.pdf_new_name("Root"),
                mupdf.mupdf.pdf_new_name("AcroForm"),
                mupdf.mupdf.pdf_new_name("DR"),
                mupdf.mupdf.pdf_new_name("Font"));
            if (fonts.m_internal == null || mupdf.mupdf.pdf_is_dict(fonts) == 0)
                throw new InvalidOperationException("PDF has no form fonts yet");
            var k = mupdf.mupdf.pdf_new_name(name);
            var v = Helpers.JM_pdf_obj_from_str(pdf, font);
            mupdf.mupdf.pdf_dict_put(fonts, k, v);
        }
        /// <summary>
        /// Returns a short diagnostic string for this document.
        /// </summary>
        /// <remarks>PyMuPDF <c>Document.to_string</c>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public override string ToString()
        {
            string p = IsClosed ? "closed " : "";
            if (StreamData != null) return $"{p}Document('{Name}', <memory, doc# {_graftId}>)";
            if (string.IsNullOrEmpty(Name)) return $"{p}Document(<new PDF, doc# {_graftId}>)";
            return $"{p}Document('{Name}')";
        }
    }
}
