using System;
using System.Collections.Generic;
using System.IO;

namespace MuPDF.NET
{
    /// <summary>
    /// Python-name compatibility surface for <c>Document</c>.
    /// These wrappers preserve naming traceability to <c>src/__init__.py:class Document</c>.
    /// </summary>
    public partial class Document
    {
        /// <summary>Python <c>fitz.open()</c> / <c>fitz.open(path)</c> / memory open.</summary>
        public static Document open() => Open();

        public static Document open(string filename, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            => Open(filename, filetype, rect, width, height, fontsize);

        public static Document open(byte[] data, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            => Open(data, filetype, rect, width, height, fontsize);

        public static Document open(Stream stream, string filetype = null, Rect rect = null, float width = 0, float height = 0, float fontsize = 11)
            => Open(stream, filetype, rect, width, height, fontsize);

        // Python-style wrappers.
        public int page_count() => PageCount;
        public int chapter_count() => ChapterCount;
        public bool needs_pass() => NeedsPass;
        public bool is_reflowable() => IsReflowable;
        public bool is_closed() => IsClosed;
        public bool is_encrypted() => IsEncrypted;
        /// <summary>Python <c>Document.init_doc()</c> — safe to call after <c>authenticate()</c>; throws if the document is still encrypted.</summary>
        public void init_doc()
        {
            if (IsEncrypted)
                throw new ValueErrorException("cannot initialize - document still encrypted");
            InitDoc();
        }
        public bool is_dirty() => IsDirty;
        public bool is_form_pdf() => IsFormPdf;
        public bool is_fast_webaccess() => IsFastWebaccess;
        public bool is_pdf() => IsPdf;
        public bool is_repaired() => IsRepaired;
        public (int, int) last_location() => LastLocation;
        public bool journal_is_enabled() => JournalIsEnabled;
        public string pagelayout() => PageLayout;
        public string pagemode() => PageMode;
        public int permissions() => Permissions;
        public string name() => Name;
        public string language() => Language;
        public int version_count() => VersionCount;
        public int xref_length() => XrefLength;
        public int pdf_catalog() => PdfCatalog;
        public Dictionary<string, bool> markinfo() => MarkInfo;
        public bool set_markinfo(Dictionary<string, object> markinfo) => SetMarkInfo(markinfo);
        public bool has_annots() => HasAnnots;
        public bool has_links() => HasLinks;

        // Python dunder compatibility helpers.
        public int __len__() => PageCount;
        public bool __contains__(int page_number) => ContainsPage(page_number);
        public bool __contains__((int chapter, int page) loc) => ContainsLocation(loc);
        public bool contains_chapter_page(int chapter, int page_in_chapter) => ContainsChapterPage(chapter, page_in_chapter);
        public bool contains_location((int chapter, int page) loc) => ContainsLocation(loc);
        public string __repr__() => ToString();
        public Page __getitem__(int page_number) => GetItemPageForIndexer(page_number);
        public Page __getitem__((int chapter, int page) loc) => GetItemPageForIndexer(loc.chapter, loc.page);

        /// <summary>Python <c>with fitz.open(...) as doc:</c> — returns self; exit closes the document.</summary>
        public Document __enter__() => this;

        /// <summary>Python <c>Document.__exit__</c> (exception args ignored).</summary>
        public void __exit__(object exc_type = null, object exc_value = null, object traceback = null) => Close();

        // Page loading/navigation.
        public Page load_page(int page_id) => LoadPage(page_id);
        public Page load_page(int chapter, int pno) => LoadPage(chapter, pno);
        public IEnumerable<Page> pages(int? start = null, int? stop = null, int? step = null) => Pages(start, stop, step);
        public (int chapter, int pageInChapter) next_location((int chapter, int page) loc) => NextLocation(loc);
        public (int chapter, int pageInChapter) prev_location((int chapter, int page) loc) => PrevLocation(loc);
        public (int chapter, int page) location_from_page_number(int pno) => LocationFromPageNumber(pno);
        public int page_number_from_location((int chapter, int page) loc) => PageNumberFromLocation(loc);
        public int chapter_page_count(int chapter) => ChapterPageCount(chapter);
        public ulong make_bookmark((int chapter, int page) loc) => MakeBookmark(loc);
        public (int chapter, int page) find_bookmark(ulong bm) => FindBookmark(bm);

        // Metadata and outlines.
        public Dictionary<string, string> metadata() => GetMetadata();
        public void set_metadata(Dictionary<string, string> metadata) => SetMetadata(metadata);
        public List<(int level, string title, int page, Dictionary<string, object> link)> get_toc(bool simple = true) => GetToc(simple);
        public int set_toc(IList<object> toc, int collapse = 1) => SetToc(toc, collapse);
        public void set_toc_item(int idx, Dictionary<string, object> dest_dict = null, int? kind = null, int? pno = null,
            string uri = null, string title = null, Point to = null, string filename = null, float zoom = 0)
            => SetTocItem(idx, dest_dict, kind, pno, uri, title, to, filename, zoom);
        public void del_toc_item(int idx) => DelTocItem(idx);
        public Outline get_outline() => GetOutline();
        public List<int> get_outline_xrefs() => GetOutlineXrefs();

        // Page editing.
        public Page new_page(int pno = -1, float width = 595, float height = 842) => NewPage(pno, width, height);
        public void delete_page(int pno) => DeletePage(pno);
        public void _delete_page(int pno) => DeletePage(pno);
        public void delete_pages(params int[] pages) => DeletePages(pages);
        public void delete_pages(int from_page, int to_page) => DeletePages(from_page, to_page);
        public void delete_pages_by_slice(int start, int stop, int step = 1) => DeletePagesBySlice(start, stop, step);
        public List<Page> load_pages_by_slice(int start, int stop, int step = 1) => LoadPagesBySlice(start, stop, step);
        public void __delitem__(int page_number)
        {
            EnsurePdf();
            DeletePage(page_number);
        }
        public void __delitem__(int[] pages)
        {
            EnsurePdf();
            DeletePages(pages);
        }
        public Page insert_page(int pno = -1, string text = null, float fontsize = 11, float width = 595, float height = 842, string fontname = "helv", float[] color = null)
            => InsertPage(pno, text, fontsize, width, height, fontname, color);
        public void copy_page(int pno, int to = -1) => CopyPage(pno, to);
        public void fullcopy_page(int pno, int to = -1) => FullcopyPage(pno, to);
        public void move_page(int pno, int to = -1) => MovePage(pno, to);
        public Page _newPage(int pno = -1, float width = 595, float height = 842) => NewPage(pno, width, height);
        public void select(int[] pages) => Select(pages);
        public Page reload_page(Page page) => ReloadPage(page);

        public byte[] tobytes(bool garbage = false, bool clean = false, bool deflate = false) => ToBytes(garbage, clean, deflate);
        public byte[] convert_to_pdf(int from_page = 0, int to_page = -1, int rotate = 0) => ConvertToPdf(from_page, to_page, rotate);
        public void save_incr() => SaveIncr();
        public void saveIncr() => SaveIncr();
        public bool can_save_incrementally() => CanSaveIncrementally();
        public bool authenticate(string password) => Authenticate(password);
        public void ez_save(string filename, int garbage = 1, int clean = 0, int deflate = 1,
            int deflate_images = 1, int deflate_fonts = 1, int incremental = 0)
            => EzSave(filename, garbage, clean, deflate, deflate_images, deflate_fonts, incremental);

        // Xref/object APIs.
        public int page_xref(int pno) => PageXref(pno);
        public string xref_object(int xref, bool compressed = false, bool ascii = false) => XrefObject(xref, compressed, ascii);
        public bool xref_is_stream(int xref = 0) => XrefIsStream(xref);
        public bool xref_is_font(int xref) => XrefIsFont(xref);
        public bool xref_is_image(int xref) => XrefIsImage(xref);
        public bool xref_is_xobject(int xref) => XrefIsXobject(xref);
        public byte[] xref_stream(int xref) => XrefStream(xref);
        public byte[] xref_stream_raw(int xref) => XrefStreamRaw(xref);
        public (string type, string value) xref_get_key(int xref, string key) => XrefGetKey(xref, key);
        public List<string> xref_get_keys(int xref) => XrefGetKeys(xref);
        public void xref_set_key(int xref, string key, string value) => XrefSetKey(xref, key, value);
        public void _deleteObject(int xref)
        {
            if (xref < 1 || xref >= XrefLength)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);
            mupdf.mupdf.pdf_delete_object(NativePdfDocument, xref);
        }
        public int xref() => GetNewXref();
        public int get_new_xref() => GetNewXref();

        // XML metadata.
        public string xref_xml_metadata() => GetXmlMetadata();
        public string get_xml_metadata() => GetXmlMetadata();
        public void set_xml_metadata(string metadata) => SetXmlMetadata(metadata);
        public void del_xml_metadata() => DelXmlMetadata();

        // Object / stream updates.
        public void update_object(int xref, string text) => UpdateObject(xref, text);
        public void update_object(int xref, string text, Page page) => UpdateObject(xref, text, page);
        public void update_stream(int xref, byte[] stream, bool compress = true) => UpdateStream(xref, stream, compress);

        /// <summary>Python <c>update_stream(xref, stream, new=1, compress=1)</c> when <c>new</c> and <c>compress</c> are passed as ints (e.g. <c>0</c>/<c>1</c>). <paramref name="new_"/> is ignored (unused in PyMuPDF).</summary>
        public void update_stream(int xref, byte[] stream, int new_, int compress)
            => UpdateStream(xref, stream, compress != 0);
        public static void xref_copy(Document doc, int source, int target, List<string> keep = null)
            => Document.XrefCopy(doc, source, target, keep);
        public string pdf_trailer(bool compressed = false, bool ascii = false) => PdfTrailer(compressed, ascii);

        // Embedded files.
        public List<string> embfile_names() => EmbfileNames();
        public byte[] embfile_get(string name) => EmbfileGet(name);
        public byte[] embfile_get(int idx) => EmbfileGetByIndex(idx);
        public byte[] embfile_get_by_index(int idx) => EmbfileGetByIndex(idx);
        public byte[] _embeddedFileGet(int idx) => EmbfileGetByIndex(idx);
        public int _embeddedFileIndex(object item)
        {
            if (item is int i)
                return i;
            if (item is string s)
                return EmbfileIndex(s);
            throw new ValueErrorException($"'{item}' not in EmbeddedFiles array.");
        }
        public int embfile_add(string name, byte[] buffer_, string filename = null, string ufilename = null, string desc = null)
            => EmbfileAdd(name, buffer_, filename, ufilename, desc);
        public int _embfile_add(string name, byte[] buffer_, string filename = null, string ufilename = null, string desc = null)
            => EmbfileAdd(name, buffer_, filename, ufilename, desc);
        public Dictionary<string, object> embfile_info(string item) => EmbfileInfo(item);
        public Dictionary<string, object> embfile_info(int item) => EmbfileInfo(item);
        public int _embfile_info(int idx, Dictionary<string, object> infodict)
        {
            var info = EmbfileInfo(idx);
            if (infodict != null)
            {
                foreach (var kv in info)
                    infodict[kv.Key] = kv.Value;
            }
            return info.TryGetValue("xref", out var xrefObj) ? Convert.ToInt32(xrefObj) : 0;
        }
        public void _embfile_names(List<string> namelist)
        {
            if (namelist == null) return;
            namelist.AddRange(EmbfileNames());
        }
        public int embfile_upd(string item, byte[] buffer_ = null, string filename = null, string ufilename = null, string desc = null)
            => EmbfileUpd(item, buffer_, filename, ufilename, desc);
        public int embfile_upd(int item, byte[] buffer_ = null, string filename = null, string ufilename = null, string desc = null)
            => EmbfileUpd(item, buffer_, filename, ufilename, desc);
        public int _embfile_upd(int idx, byte[] buffer_ = null, string filename = null, string ufilename = null, string desc = null)
            => EmbfileUpd(idx, buffer_, filename, ufilename, desc);
        public void _embfile_del(int idx) => EmbfileDel(idx);
        public void embfile_del(string name) => EmbfileDel(name);
        public void embfile_del(int idx) => EmbfileDel(idx);
        public int embfile_count() => EmbfileCount;

        // Page resource extraction helpers.
        public List<(int xref, string ext, string type, string baseName, string name, string encoding)> get_page_fonts(int pno, bool full = false) => GetPageFonts(pno, full);
        public List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)> get_page_images(int pno, bool full = false) => GetPageImages(pno, full);
        /// <summary>
        /// Python-shape compatibility helper for <c>get_page_fonts</c>.
        /// Returns list entries as object arrays so callers can compare tuple lengths:
        /// <c>full=False</c> => 6 fields, <c>full=True</c> => 7 fields (last entry is referencer placeholder).
        /// </summary>
        public List<object[]> get_page_fonts_py(int pno, bool full = false)
        {
            var src = GetPageFontsCore(pno, true);
            var ret = new List<object[]>(src.Count);
            foreach (var t in src)
            {
                if (full)
                    ret.Add(new object[] { t.xref, t.ext, t.type, t.baseName, t.name, t.encoding, 0 });
                else
                    ret.Add(new object[] { t.xref, t.ext, t.type, t.baseName, t.name, t.encoding });
            }
            return ret;
        }

        /// <summary>
        /// Python-shape compatibility helper for <c>get_page_images</c>.
        /// Returns list entries as object arrays so callers can compare tuple lengths:
        /// <c>full=False</c> => 9 fields, <c>full=True</c> => 10 fields (last entry is referencer placeholder).
        /// </summary>
        public List<object[]> get_page_images_py(int pno, bool full = false)
        {
            var src = GetPageImagesCore(pno, true);
            var ret = new List<object[]>(src.Count);
            foreach (var t in src)
            {
                if (full)
                    ret.Add(new object[] { t.xref, t.smask, t.width, t.height, t.bpc, t.colorspace, t.altCs, t.name, t.filter, 0 });
                else
                    ret.Add(new object[] { t.xref, t.smask, t.width, t.height, t.bpc, t.colorspace, t.altCs, t.name, t.filter });
            }
            return ret;
        }
        public List<(int glyph, double width)> get_char_widths(int xref, int limit = 256, int idx = 0, Dictionary<string, object> fontdict = null)
            => GetCharWidths(xref, limit, idx, fontdict);
        public List<(int xref, AnnotationType type, string id)> page_annot_xrefs(int n) => PageAnnotXrefs(n);
        public List<Dictionary<string, object>> get_page_xobjects(int pno) => GetPageXobjects(pno);
        public (string name, string ext, string type, byte[] content) extract_font(int xref) => ExtractFont(xref);
        public Dictionary<string, object> extract_image(int xref) => ExtractImage(xref);
        public List<Dictionary<string, object>> get_page_labels() => GetPageLabels();
        public List<Dictionary<string, object>> _get_page_labels() => GetPageLabels();
        public void set_page_labels(List<Dictionary<string, object>> labels) => SetPageLabels(labels);
        public void _set_page_labels(List<Dictionary<string, object>> labels) => SetPageLabels(labels);
        public List<int> get_page_numbers(string label, bool only_one = false) => GetPageNumbers(label, only_one);
        public List<Quad> search_page_for(int pno, string needle, int max_hits = 16, Quad clip = null, int flags = 0, TextPage textpage = null)
            => SearchPageFor(pno, needle, max_hits, clip, flags, textpage);

        public List<Rect> search_page_for_rects(int pno, string needle, int max_hits = 16, Quad clip = null, int flags = 0, TextPage textpage = null)
            => SearchPageForRects(pno, needle, max_hits, clip, flags, textpage);
        public Pixmap get_page_pixmap(int pno, Matrix matrix = null, Colorspace cs = null, bool alpha = false, IRect clip = null)
            => GetPagePixmap(pno, matrix, cs, alpha, clip);
        public string get_page_text(int pno, string option = "text", int flags = 0) => GetPageText(pno, option, flags);

        // Layout and journalling.
        public void layout(float width = 400, float height = 600, float fontsize = 11) => Layout(width, height, fontsize);
        public void layout(Rect rect, float fontsize = 11) => Layout(rect, fontsize);
        public void journal_enable() => JournalEnable();
        public Dictionary<string, bool> journal_can_do()
        {
            var state = JournalCanDo();
            return new Dictionary<string, bool>
            {
                ["undo"] = state.canUndo,
                ["redo"] = state.canRedo
            };
        }
        public bool journal_undo()
        {
            JournalUndo();
            return true;
        }
        public bool journal_redo()
        {
            JournalRedo();
            return true;
        }
        public void journal_start_op(string name = null) => JournalStartOp(name);
        public void journal_stop_op() => JournalStopOp();
        public void journal_save(string filename) => JournalSave(filename);
        public void journal_load(string filename) => JournalLoad(filename);
        public void journal_load(byte[] data) => JournalLoad(data);
        public string journal_op_name(int step) => JournalOpName(step);
        public (int rc, int steps) journal_position() => JournalPosition();
        public void repair() => Repair();

        // Merge and layers.
        public void insert_pdf(Document src, int from_page = 0, int to_page = -1, int start_at = -1, int rotate = -1, bool links = true, bool annots = true)
            => InsertPdf(src, from_page, to_page, start_at, rotate, links, annots);
        public void insert_file(object infile, int from_page = -1, int to_page = -1, int start_at = -1, int rotate = -1, bool links = true, bool annots = true, int show_progress = 0, int final = 1)
            => InsertFile(infile, from_page, to_page, start_at, rotate, links, annots, show_progress, final);
        public Dictionary<int, Dictionary<string, object>> get_ocgs() => GetOcgs();
        public Dictionary<string, object> get_layer(int config = -1) => GetLayer(config);
        public List<Dictionary<string, object>> get_layers() => GetLayers();
        public int get_oc(int xref) => GetOc(xref);
        public Dictionary<string, object> get_ocmd(int xref) => GetOcmd(xref);
        public void set_oc(int xref, int oc) => SetOc(xref, oc);
        public int set_ocmd(int xref = 0, List<int> ocgs = null, string policy = null, object ve = null) => SetOcmd(xref, ocgs, policy, ve);
        public Dictionary<string, Dictionary<string, object>> resolve_names() => ResolveNames();
        public void add_layer(string name, string creator = null, bool on = true) => AddLayer(name, creator, on);
        public int add_ocg(string name, int config = -1, int on = 1, string intent = null, string usage = null) => AddOcg(name, config, on != 0, intent, usage);
        public void set_layer(int config, string basestate = null, object on = null, object off = null, object rbgroups = null, object locked = null)
            => SetLayer(config, basestate, on, off, rbgroups, locked);
        public void switch_layer(int config, bool as_default = false) => SwitchLayer(config, as_default);
        public List<Dictionary<string, object>> layer_ui_configs() => LayerUiConfigs();
        public void set_layer_ui_config(object number, int action = 0) => SetLayerUiConfig(number, action);

        // Cleanup and misc.
        public void bake(bool annots = true, bool widgets = true) => Bake(annots, widgets);
        public void scrub(bool attached_files = true, bool clean_pages = true, bool embedded_files = true,
            bool hidden_text = true, bool javascript = true, bool metadata = true, bool redactions = true, int redact_images = 0, bool remove_links = true, bool reset_fields = true, bool reset_responses = true, bool thumbnails = true, bool xml_metadata = true)
            => Scrub(attached_files, clean_pages, embedded_files, hidden_text, javascript, metadata, redactions, redact_images != 0, remove_links, reset_fields, reset_responses, thumbnails, xml_metadata);
        public (object page_id, float x, float y) resolve_link(string uri = null, bool chapters = false) => ResolveLink(uri, chapters);
        public void subset_fonts() => SubsetFonts();
        public void recolor(int components = 1) => Recolor(components);
        public void rewrite_images(int quality = 0, int dpi_threshold = 0, int dpi_target = 0, bool lossy = true, bool lossless = true, bool bitonal = true, bool color = true, bool gray = true)
            => RewriteImages(quality, dpi_threshold, dpi_target, lossy, lossless, bitonal, color, gray);
        public bool set_language(string language)
        {
            SetLanguage(language);
            return true;
        }
        public void set_need_appearances(bool value) => SetNeedAppearances(value);
        public bool? need_appearances(bool? value = null)
        {
            if (!IsFormPdf)
                return null;
            var pdf = NativePdfDocument;
            var form = mupdf.mupdf.pdf_dict_getp(mupdf.mupdf.pdf_trailer(pdf), "Root/AcroForm");
            var app = form.m_internal != null ? mupdf.mupdf.pdf_dict_gets(form, "NeedAppearances") : new mupdf.PdfObj();
            bool old = app.m_internal != null && mupdf.mupdf.pdf_is_bool(app) != 0;
            if (value.HasValue)
                SetNeedAppearances(value.Value);
            return value ?? old;
        }
        public bool set_pagelayout(string layout)
        {
            SetPageLayout(layout);
            return true;
        }
        public bool set_pagemode(string mode)
        {
            SetPageMode(mode);
            return true;
        }
        public int get_sigflags() => GetSigFlags();
        public Rect page_cropbox(int pno) => PageCropBox(pno);
        public void save_snapshot(string filename) => SaveSnapshot(filename);
        public void save_snapshot(object filename) => SaveSnapshot(filename);
        public void close() => Close();

        // Private Python helper-name parity.
        public List<int> _delToC()
        {
            var xrefs = GetOutlineXrefs();
            foreach (var xref in xrefs)
                RemoveTocItemByXref(xref);
            return xrefs;
        }
        public void _remove_toc_item(int xref) => RemoveTocItemByXref(xref);
        public void _update_toc_item(int xref, string action = null, string title = null, int flags = 0, bool? collapse = null, float[] color = null)
            => UpdateTocItemByXref(xref, action, title, flags, collapse, color);
        public string _getMetadata(string key)
        {
            try { return mupdf.mupdf.fz_lookup_metadata2(NativeDocument, key); }
            catch { return ""; }
        }
        public int _getOLRootNumber()
        {
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
        public List<string> _getPDFfileid()
        {
            var ret = new List<string>();
            var pdf = NativePdfDocument;
            var identity = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("ID"));
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
        public List<object> _getPageInfo(int pno, object what)
        {
            if (what is string ws)
            {
                ws = ws.ToLowerInvariant();
                if (ws.Contains("font")) return new List<object>(GetPageFontsCore(pno, true).ConvertAll(x => (object)x));
                if (ws.Contains("image")) return new List<object>(GetPageImagesCore(pno, true).ConvertAll(x => (object)x));
                return new List<object>(GetPageXobjects(pno).ConvertAll(x => (object)x));
            }
            int wi = Convert.ToInt32(what);
            if (wi == 1) return new List<object>(GetPageFontsCore(pno, true).ConvertAll(x => (object)x));
            if (wi == 2) return new List<object>(GetPageImagesCore(pno, true).ConvertAll(x => (object)x));
            return new List<object>(GetPageXobjects(pno).ConvertAll(x => (object)x));
        }
        /// <summary>
        /// Python-shape compatibility overload for <c>_getPageInfo</c>.
        /// </summary>
        public List<object[]> _getPageInfo_py(int pno, int what, bool full = true)
        {
            if (what == 1) return get_page_fonts_py(pno, full);
            if (what == 2) return get_page_images_py(pno, full);
            var xobjs = GetPageXobjects(pno);
            var ret = new List<object[]>(xobjs.Count);
            foreach (var xo in xobjs)
                ret.Add(new object[] { xo });
            return ret;
        }
        public object[] _insert_font(string fontfile = null, byte[] fontbuffer = null)
        {
            /*
             * Utility: insert font from file or binary.
             */
            var pdf = NativePdfDocument;
            if (string.IsNullOrEmpty(fontfile) && (fontbuffer == null || fontbuffer.Length == 0))
                throw new ValueErrorException(Constants.MSG_FILE_OR_BUFFER);

            mupdf.FzFont font;
            mupdf.PdfObj fontObj;
            if (!string.IsNullOrEmpty(fontfile))
            {
                font = mupdf.mupdf.fz_new_font_from_file(null, fontfile, 0, 0);
                fontObj = mupdf.mupdf.pdf_add_cid_font(pdf, font);
            }
            else
            {
                var res = Helpers.BufferFromBytes(fontbuffer);
                if (res.m_internal == null)
                    throw new ValueErrorException(Constants.MSG_FILE_OR_BUFFER);
                font = mupdf.mupdf.fz_new_font_from_buffer(null, res, 0, 0);
                fontObj = mupdf.mupdf.pdf_add_cid_font(pdf, font);
            }

            int ixref = mupdf.mupdf.pdf_to_num(fontObj);
            string name = mupdf.mupdf.pdf_to_name(mupdf.mupdf.pdf_dict_get(fontObj, mupdf.mupdf.pdf_new_name("BaseFont")));
            string subt = mupdf.mupdf.pdf_to_name(mupdf.mupdf.pdf_dict_get(fontObj, mupdf.mupdf.pdf_new_name("Subtype")));
            string exto = "";
            try { exto = ExtractFont(ixref).ext; } catch { exto = ""; }
            float asc = font.fz_font_ascender();
            float dsc = font.fz_font_descender();
            var info = new Dictionary<string, object>
            {
                ["name"] = name,
                ["type"] = subt,
                ["ext"] = exto,
                ["simple"] = false,
                ["ordering"] = -1,
                ["ascender"] = asc,
                ["descender"] = dsc,
            };
            return new object[] { ixref, info };
        }
        public Outline _loadOutline() => GetOutline();
        public void _forget_page(Page page) => ForgetPageRef(page);
        public void _reset_page_refs() => ResetPageRefsInternal();
        public void _remove_links_to(object numbers)
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
        public void _addFormFont(string name, string font)
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

        // Legacy alias names from Python _alias(Document, ...).
        public byte[] convertToPDF(int from_page = 0, int to_page = -1, int rotate = 0) => convert_to_pdf(from_page, to_page, rotate);
        public void deletePageRange(int from_page, int to_page) => delete_pages(from_page, to_page);
        public int embeddedFileAdd(string name, byte[] buffer_, string filename = null, string ufilename = null, string desc = null) => embfile_add(name, buffer_, filename, ufilename, desc);
        public int embeddedFileCount() => embfile_count();
        public void embeddedFileDel(string name) => embfile_del(name);
        public void embeddedFileDel(int idx) => embfile_del(idx);
        public byte[] embeddedFileGet(string name) => embfile_get(name);
        public byte[] embeddedFileGet(int idx) => embfile_get(idx);
        public Dictionary<string, object> embeddedFileInfo(string item) => embfile_info(item);
        public Dictionary<string, object> embeddedFileInfo(int item) => embfile_info(item);
        public List<string> embeddedFileNames() => embfile_names();
        public int embeddedFileUpd(string item, byte[] buffer_ = null, string filename = null, string ufilename = null, string desc = null) => embfile_upd(item, buffer_, filename, ufilename, desc);
        public int embeddedFileUpd(int item, byte[] buffer_ = null, string filename = null, string ufilename = null, string desc = null) => embfile_upd(item, buffer_, filename, ufilename, desc);
        public Dictionary<int, Dictionary<string, object>> getOCGs() => get_ocgs();
        public List<(int xref, string ext, string type, string baseName, string name, string encoding)> getPageFontList(int pno, bool full = false) => get_page_fonts(pno, full);
        public List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)> getPageImageList(int pno, bool full = false) => get_page_images(pno, full);
        public List<Dictionary<string, object>> getPageXObjectList(int pno) => get_page_xobjects(pno);
        public int getSigFlags() => get_sigflags();
        public List<(int level, string title, int page, Dictionary<string, object> link)> getToC(bool simple = true) => get_toc(simple);
        public void insertPDF(Document src, int from_page = 0, int to_page = -1, int start_at = -1, int rotate = -1, bool links = true, bool annots = true) => insert_pdf(src, from_page, to_page, start_at, rotate, links, annots);
        public bool isFormPDF() => is_form_pdf();
        public bool isPDF() => is_pdf();
        public Rect pageCropBox(int pno) => page_cropbox(pno);
        public int PDFCatalog() => pdf_catalog();
        public string PDFTrailer(bool compressed = false, bool ascii = false) => pdf_trailer(compressed, ascii);
        public (int chapter, int pageInChapter) previousLocation((int chapter, int page) loc) => prev_location(loc);
        public int setToC(IList<object> toc, int collapse = 1) => set_toc(toc, collapse);
        public bool isStream(int xref = 0) => xref_is_stream(xref);
        public string metadataXML() => xref_xml_metadata();
        public void setToCItem(int idx, Dictionary<string, object> dest_dict = null, int? kind = null, int? pno = null,
            string uri = null, string title = null, Point to = null, string filename = null, float zoom = 0)
            => set_toc_item(idx, dest_dict, kind, pno, uri, title, to, filename, zoom);
        public void delToCItem(int idx) => del_toc_item(idx);
        public List<(int glyph, double width)> getCharWidths(int xref, int limit = 256, int idx = 0, Dictionary<string, object> fontdict = null)
            => get_char_widths(xref, limit, idx, fontdict);
    }
}
