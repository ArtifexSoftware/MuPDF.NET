using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MuPDF.NET
{
    /// <summary>
    /// One page of a document.
    /// </summary>
    /// <remarks>
    /// <para>Created only by <see cref="Document.LoadPage(int)"/> or document indexing
    /// (<c>doc[n]</c>). Closing or disposing the parent <see cref="Document"/> orphans existing
    /// page instances; further use raises an exception.</para>
    /// <para>Coordinates passed to insert, draw, and annotation APIs are in unrotated page space.
    /// <see cref="Rect"/> and <see cref="Bound"/> include rotation; most other geometry and text
    /// APIs return unrotated values. Use <see cref="RotationMatrix"/> and
    /// <see cref="DerotationMatrix"/> to convert.</para>
    /// <para>After adding or updating annotations, links, or widgets, reload the page with
    /// <see cref="Document.ReloadPage(int)"/> before iterating those objects or rendering, when
    /// changes must be fully visible in the PDF structures.</para>
    /// <para>Ports PyMuPDF <c>Page</c> (<c>src/__init__.py</c>).</para>
    /// </remarks>
    public partial class Page : IDisposable
    {
        private mupdf.FzPage? _nativePage;
        private bool _disposed;
        private List<Dictionary<string, object>> _imageInfo;
        private object _layoutInformation;
        private static Func<Page, object> _getLayout;
        /// <summary>
        /// Weak cache of <see cref="Annot"/> wrappers for this page, analogous to Python <c>Page._annot_refs</c>
        /// (<c>weakref.WeakValueDictionary</c> after <c>load_page</c>).
        /// </summary>
        private readonly object _wrapperCacheLock = new object();
        private readonly Dictionary<int, WeakReference<Annot>> _annotRefs = new Dictionary<int, WeakReference<Annot>>();
        /// <summary>Best-effort cache of <see cref="Link"/> wrappers keyed by link-annot xref (Python <c>delete_link</c> / <c>get_links</c> flow).</summary>
        private readonly Dictionary<int, WeakReference<Link>> _linkRefsByXref = new Dictionary<int, WeakReference<Link>>();
        /// <summary>PDF <c>/NM</c> string -> link wrapper (Python <c>delete_link</c> <c>finished()</c> uses <c>linkdict["id"]</c> with <c>_annot_refs</c>).</summary>
        private readonly Dictionary<string, WeakReference<Link>> _linkRefsByNm = new Dictionary<string, WeakReference<Link>>(StringComparer.Ordinal);
        private int _syncingLinkWrapperCache;

        /// <summary>Built-in PDF stamp names (PyMuPDF <c>Page._add_stamp_annot</c> <c>stamp_id</c> list).</summary>
        private static readonly string[] s_stampBuiltinNames =
        {
            "Approved",
            "AsIs",
            "Confidential",
            "Departmental",
            "Experimental",
            "Expired",
            "Final",
            "ForComment",
            "ForPublicRelease",
            "NotApproved",
            "NotForPublicRelease",
            "Sold",
            "TopSecret",
            "Draft",
        };

        private static string StampBuiltinNameFromIndex(int stamp)
        {
            if (stamp >= 0 && stamp < s_stampBuiltinNames.Length)
                return s_stampBuiltinNames[stamp];
            return s_stampBuiltinNames[0];
        }

        /// <summary>
        /// Stable identity for <see cref="Document"/> page-ref bookkeeping (Python uses <c>id(page)</c>).
        /// </summary>
        internal int PageRefId { get; }
        /// <summary>
        /// Gets or sets Owning document. Null after the page is detached from a closed document.
        /// </summary>
        public Document? Parent { get; internal set; }
        /// <summary>
        /// Gets or sets Whether this wrapper disposes the native page handle.
        /// </summary>
        public bool ThisOwn { get; set; } = true;

        /// <summary>0-based page index from <c>load_page</c> (Python <c>Page.number</c>).</summary>
        private readonly int _pageNumber;
        /// <summary>Chapter index when loaded via <c>load_page(chapter, pno)</c>; otherwise -1.</summary>
        private readonly int _chapter;
        /// <summary>Page within chapter when <see cref="_chapter"/> &gt;= 0; otherwise -1.</summary>
        private readonly int _chapterPage;
        internal int ReloadChapter => _chapter;
        internal int ReloadChapterPage => _chapterPage;
        private mupdf.PdfPage? _cachedPdfPage;

        internal mupdf.FzPage NativePage
        {
            get
            {
                if (_disposed || _nativePage == null)
                    throw new ObjectDisposedException(nameof(Page));
                return _nativePage;
            }
        }

        internal mupdf.PdfPage NativePdfPage
        {
            get
            {
                if (_cachedPdfPage == null || _cachedPdfPage.m_internal == null)
                {
                    if (_cachedPdfPage != null)
                    {
                        _cachedPdfPage.Dispose();
                        _cachedPdfPage = null;
                    }
                    _cachedPdfPage = NativePage.pdf_page_from_fz_page();
                    if (_cachedPdfPage.m_internal == null)
                    {
                        _cachedPdfPage.Dispose();
                        _cachedPdfPage = null;
                        throw new InvalidOperationException(Constants.MSG_IS_NO_PDF);
                    }
                }
                return _cachedPdfPage;
            }
        }

        internal Page(mupdf.FzPage fzPage, Document owner, int pageNumber = -1, int chapter = -1, int chapterPage = -1)
        {
            _nativePage = fzPage;
            var pdfPage = fzPage.pdf_page_from_fz_page();
            if (pdfPage.m_internal != null)
                _cachedPdfPage = pdfPage;
            else
                pdfPage.Dispose();
            _pageNumber = pageNumber;
            _chapter = chapter;
            _chapterPage = chapterPage;
            PageRefId = Document.NextPageRefId();
            Parent = owner;
            owner.RegisterPageRef(this);
        }

        internal void DisposeCachedPdfPage()
        {
            _cachedPdfPage?.Dispose();
            _cachedPdfPage = null;
        }

        internal Document RequireParent()
        {
            if (Parent == null)
                throw new ObjectDisposedException(nameof(Page), "page is detached from its document");
            return Parent;
        }

        internal void RegisterAnnotRef(Annot annot)
        {
            if (annot == null) return;
            lock (_wrapperCacheLock)
                _annotRefs[annot.AnnotRefId] = new WeakReference<Annot>(annot);
        }

        internal void ForgetAnnotRef(Annot annot)
        {
            if (annot == null) return;
            lock (_wrapperCacheLock)
                _annotRefs.Remove(annot.AnnotRefId);
        }

        /// <summary>Python <c>reload_page</c>: copy <c>page._annot_refs</c> before teardown.</summary>
        internal Dictionary<int, Annot> SnapshotAnnotRefsForReload()
        {
            var old_annots = new Dictionary<int, Annot>();
            lock (_wrapperCacheLock)
            {
                foreach (var kvp in _annotRefs)
                {
                    if (kvp.Value.TryGetTarget(out var v) && v != null)
                        old_annots[kvp.Key] = v;
                }
            }
            return old_annots;
        }

        /// <summary>Python <c>reload_page</c>: restore saved annot wrappers onto the reloaded page.</summary>
        internal void RestoreAnnotRefsFromReload(Dictionary<int, Annot> old_annots)
        {
            lock (_wrapperCacheLock)
            {
                foreach (var k in old_annots.Keys)
                {
                    Annot annot = old_annots[k];
                    // annot.parent = page_proxy  # refresh parent to new page
                    _annotRefs[k] = new WeakReference<Annot>(annot);
                }
            }
        }

        /// <summary>Python <c>reload_page</c>: <c>page.this = None</c> — drop the <c>fz_page</c> wrapper.</summary>
        internal void ReleaseNativeForReload()
        {
            DisposeCachedPdfPage();
            if (_nativePage == null)
                return;
            // Python: page.this = None — one ref drop via the SWIG wrapper only.
            _nativePage.Dispose();
            _nativePage = null;
        }

        /// <summary>Python <c>Page._erase</c> after <c>page.this = None</c> in <c>reload_page</c>.</summary>
        internal void EraseForReload()
        {
            if (_disposed)
                return;

            _reset_annot_refs();
            try
            {
                Parent?.ForgetPageRef(this);
            }
            catch
            {
                // exception_info()
            }
            Parent = null;
            // thisown = False
            // number = None
            // this = None
            _disposed = true;
        }

        /// <summary>
        /// Port of Python <c>Page._reset_annot_refs</c>: drop cached annotation wrapper references.
        /// </summary>
        internal void ResetAnnotRefsInternal()
        {
            lock (_wrapperCacheLock)
            {
                _annotRefs.Clear();
                _linkRefsByXref.Clear();
                _linkRefsByNm.Clear();
            }
        }

        /// <summary>
        /// Rebuild <see cref="_linkRefsByXref"/> from the current MuPDF link list (call after link mutations).
        /// </summary>
        internal void SyncLinkWrapperCache()
        {
            if (Interlocked.CompareExchange(ref _syncingLinkWrapperCache, 1, 0) != 0)
                return;

            try
            {
                mupdf.PdfDocument pdfForNm = null;
                try
                {
                    if (Parent != null)
                        pdfForNm = RequireParent().NativePdfDocument;
                }
                catch
                {
                    pdfForNm = null;
                }

                lock (_wrapperCacheLock)
                {
                    _linkRefsByXref.Clear();
                    _linkRefsByNm.Clear();
                    foreach (var l in Links())
                    {
                        int xr = l.Xref;
                        if (xr > 0)
                        {
                            _linkRefsByXref[xr] = new WeakReference<Link>(l);
                            if (pdfForNm != null)
                            {
                                try
                                {
                                    var nm = Helpers.PdfAnnotNmForXref(pdfForNm, xr);
                                    if (!string.IsNullOrEmpty(nm))
                                        _linkRefsByNm[nm] = new WeakReference<Link>(l);
                                }
                                catch
                                {
                                    // Ignore stale xref/NM during cache rebuild.
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _syncingLinkWrapperCache, 0);
            }
        }

        internal bool TryGetCachedLinkByXref(int xref, out Link link)
        {
            link = null;
            if (xref < 1) return false;
            lock (_wrapperCacheLock)
            {
                return _linkRefsByXref.TryGetValue(xref, out var wr) && wr != null && wr.TryGetTarget(out link) && link != null;
            }
        }

        internal bool TryGetCachedLinkByAnnotNm(string nm, out Link link)
        {
            link = null;
            if (string.IsNullOrEmpty(nm)) return false;
            lock (_wrapperCacheLock)
            {
                return _linkRefsByNm.TryGetValue(nm, out var wr) && wr != null && wr.TryGetTarget(out link) && link != null;
            }
        }

        // ─── Properties ─────────────────────────────────────────────────
        /// <summary>
        /// True when the native fz_page pointer is null.
        /// </summary>
        public bool IsNativeReleased => _nativePage == null;
        /// <summary>
        /// Zero-based page index assigned when the page was loaded.
        /// </summary>
        public int Number
        {
            get
            {
                if (_pageNumber >= 0)
                    return _pageNumber;
                return mupdf.mupdf.pdf_lookup_page_number(
                    RequireParent().NativePdfDocument, NativePdfPage.obj());
            }
        }
        /// <summary>
        /// Visible page rectangle from fz_bound_page; reflects rotation (unlike CropBox).
        /// </summary>
        public Rect Rect
        {
            get
            {
                var r = mupdf.mupdf.fz_bound_page(NativePage);
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
        }
        /// <summary>
        /// Same as Rect: page bounds including rotation effects.
        /// </summary>
        /// <remarks>Determine the rectangle of the page. Same as property rect. For PDF documents this usually also coincides with mediabox and cropbox, but not always. For example, if the page is rotated, then this is reflected by this method; the cropbox however will not change.</remarks>
        public Rect Bound()
        {
            // CheckParent(self)  # no-op in shipped PyMuPDF
            if (_nativePage == null)
                throw new ValueErrorException("page is None");
            return Rect;
        }
        /// <summary>
        /// PDF /MediaBox rectangle in user space.
        /// </summary>
        public Rect MediaBox => GetMediaBox();
        /// <summary>
        /// PDF /CropBox (visible region). Unchanged when rotation is applied.
        /// </summary>
        public Rect CropBox => GetCropBox();
        /// <summary>
        /// PDF /BleedBox, or CropBox if BleedBox is not set.
        /// </summary>
        public Rect BleedBox => GetSpecialBox("BleedBox");
        /// <summary>
        /// PDF /TrimBox, or CropBox if TrimBox is not set.
        /// </summary>
        public Rect TrimBox => GetSpecialBox("TrimBox");
        /// <summary>
        /// PDF /ArtBox, or CropBox if ArtBox is not set.
        /// </summary>
        public Rect ArtBox => GetSpecialBox("ArtBox");
        /// <summary>
        /// Width of Rect.
        /// </summary>
        public float Width => (float)Rect.Width;
        /// <summary>
        /// Height of Rect.
        /// </summary>
        public float Height => (float)Rect.Height;
        /// <summary>
        /// Bottom-right corner of MediaBox as a Point (PyMuPDF mediabox_size).
        /// </summary>
        public Point MediaBoxSize => new Point(MediaBox.X1, MediaBox.Y1);
        /// <summary>
        /// Clockwise page rotation in degrees (0, 90, 180, 270) for PDF pages; 0 otherwise.
        /// </summary>
        public int Rotation
        {
            get
            {
                // PyMuPDF: _as_pdf_page(self.this, required=0); return 0 if not a PDF page.
                if (!IsPdf)
                    return 0;
                try
                {
                    var pdfPage = NativePdfPage;
                    return Helpers.PageRotation(pdfPage);
                }
                catch { return 0; }
            }
        }
        /// <summary>
        /// Maps between PDF user space and MuPDF device space for this page.
        /// </summary>
        public Matrix TransformationMatrix
        {
            get
            {
                try
                {
                    var pdfPage = NativePdfPage;
                    if (pdfPage?.m_internal == null)
                        return Matrix.Identity;
                    var mediabox = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_UNIT);
                    var ctm = new mupdf.FzMatrix();
                    mupdf.mupdf.pdf_page_transform(pdfPage, mediabox, ctm);
                    if (Rotation % 360 == 0)
                        return Helpers.MatrixFromFz(ctm);
                    return new Matrix(1, 0, 0, -1, 0, CropBox.Height);
                }
                catch { return Matrix.Identity; }
            }
        }
        /// <summary>
        /// Maps rotated coordinates back to unrotated page space.
        /// </summary>
        public Matrix DerotationMatrix => Helpers.DerotatePageMatrix(this);
        /// <summary>
        /// True when the parent document is a PDF.
        /// </summary>
        public bool IsPdf => RequireParent().IsPdf;
        /// <summary>
        /// PDF object number of the page dictionary.
        /// </summary>
        public int Xref
        {
            get
            {
                try { return mupdf.mupdf.pdf_to_num(NativePdfPage.obj()); }
                catch { return 0; }
            }
        }

        // ─── Annotations ────────────────────────────────────────────────
        /// <summary>
        /// First PDF annotation on the page, or null.
        /// </summary>
        public Annot FirstAnnot
        {
            get
            {
                try
                {
                    var a = mupdf.mupdf.pdf_first_annot(NativePdfPage);
                    return a.m_internal != null ? new Annot(a, this) : null;
                }
                catch { return null; }
            }
        }
        /// <summary>
        /// First link on the page (same as LoadLinks), or null.
        /// </summary>
        public Link FirstLink => LoadLinks();
        /// <summary>
        /// First form field (widget) on the page, or null.
        /// </summary>
        public Widget FirstWidget
        {
            get
            {
                try
                {
                    var w = mupdf.mupdf.pdf_first_widget(NativePdfPage);
                    return w.m_internal != null ? new Widget(w, this) : null;
                }
                catch { return null; }
            }
        }
        /// <summary>
        /// Return a generator over the page's annotations.
        /// </summary>
        public IEnumerable<Annot> Annots() => AnnotationsGenerator(typeFilter: null);
        /// <summary>
        /// Return a generator over the page's annotations.
        /// </summary>
        public IEnumerable<Annot> Annots(params AnnotationType[] types)
            => AnnotationsGenerator(typeFilter: types ?? Array.Empty<AnnotationType>());

        private IEnumerable<Annot> AnnotationsGenerator(AnnotationType[] typeFilter)
        {
            // CheckParent(self)
            RequireParent();
            var pdfPage = NativePdfPage;
            if (pdfPage?.m_internal == null)
                yield break;

            // skip_types = (PDF_ANNOT_LINK, PDF_ANNOT_POPUP, PDF_ANNOT_WIDGET)
            var skipTypes = new HashSet<int>
            {
                (int)mupdf.pdf_annot_type.PDF_ANNOT_LINK,
                (int)mupdf.pdf_annot_type.PDF_ANNOT_POPUP,
                (int)mupdf.pdf_annot_type.PDF_ANNOT_WIDGET,
            };

            // annot_xrefs = [a[0] for a in self.annot_xrefs() if ...]
            List<int> annotXrefs;
            var xrefEntries = AnnotXrefs();
            if (typeFilter == null)
            {
                annotXrefs = xrefEntries
                    .Where(a => !skipTypes.Contains((int)a.type))
                    .Select(a => a.xref)
                    .ToList();
            }
            else
            {
                var types = new HashSet<int>(typeFilter.Select(t => (int)t));
                annotXrefs = xrefEntries
                    .Where(a => types.Contains((int)a.type) && !skipTypes.Contains((int)a.type))
                    .Select(a => a.xref)
                    .ToList();
            }

            foreach (int xref in annotXrefs)
            {
                // annot = self.load_annot(xref)
                Annot annot = LoadAnnot(xref);
                // annot._yielded=True
                if (annot != null)
                    annot.Yielded = true;
                yield return annot;
            }
        }
        /// <summary>
        /// Return a generator over the page's links. The results equal the entries of Page.get_links.
        /// </summary>
        /// <returns>an entry of Page.get_links() for each iteration.</returns>
        public IEnumerable<Link> Links()
        {
            var link = FirstLink;
            while (link != null)
            {
                yield return link;
                link = link.Next;
            }
        }
        /// <summary>
        /// Return a generator over the page's links. The results equal the entries of Page.get_links.
        /// </summary>
        /// <param name="kinds">a sequence of integers to down-select to one or more link kinds. Default is all links. Example: *kinds=(pymupdf.LINK_GOTO,)* will only return internal links.</param>
        /// <returns>an entry of Page.get_links() for each iteration.</returns>
        public IEnumerable<Link> Links(params LinkType[] kinds)
        {
            HashSet<LinkType> filter = null;
            if (kinds != null && kinds.Length > 0)
                filter = new HashSet<LinkType>(kinds);
            foreach (var link in Links())
            {
                if (filter == null)
                {
                    yield return link;
                    continue;
                }
                var kind = link.IsExternal ? LinkType.Uri : LinkType.Goto;
                if (filter.Contains(kind))
                    yield return link;
            }
        }
        /// <summary>
        /// Return a generator over the page's links. The results equal the entries of Page.get_links.
        /// </summary>
        /// <param name="kinds">a sequence of integers to down-select to one or more link kinds. Default is all links. Example: *kinds=(pymupdf.LINK_GOTO,)* will only return internal links.</param>
        /// <returns>an entry of Page.get_links() for each iteration.</returns>
        public IEnumerable<Link> Links(params int[] kinds)
        {
            if (kinds == null || kinds.Length == 0)
            {
                foreach (var link in Links())
                    yield return link;
                yield break;
            }

            var mapped = new List<LinkType>(kinds.Length);
            foreach (var kind in kinds)
            {
                if (Enum.IsDefined(typeof(LinkType), kind))
                    mapped.Add((LinkType)kind);
            }

            foreach (var link in Links(mapped.ToArray()))
                yield return link;
        }
        /// <summary>
        /// Return a generator over the page's form fields.
        /// </summary>
        /// <returns>a Widget for each iteration.</returns>
        public IEnumerable<Widget> Widgets()
        {
            var w = FirstWidget;
            while (w != null)
            {
                yield return w;
                w = w.Next;
            }
        }
        /// <summary>
        /// Return a generator over the page's form fields.
        /// </summary>
        /// <param name="types">a sequence of integers to down-select to one or more widget types. Default is all form fields. Example: `types=(pymupdf.PDF_WIDGET_TYPE_TEXT,)` will only return 'Text' fields.</param>
        /// <returns>a Widget for each iteration.</returns>
        public IEnumerable<Widget> Widgets(params WidgetType[] types)
        {
            HashSet<WidgetType> filter = null;
            if (types != null && types.Length > 0)
                filter = new HashSet<WidgetType>(types);
            foreach (var widget in Widgets())
            {
                if (filter == null || filter.Contains((WidgetType)widget.FieldType))
                    yield return widget;
            }
        }
        /// <summary>
        /// Return a generator over the page's form fields.
        /// </summary>
        /// <param name="types">a sequence of integers to down-select to one or more widget types. Default is all form fields. Example: `types=(pymupdf.PDF_WIDGET_TYPE_TEXT,)` will only return 'Text' fields.</param>
        /// <returns>a Widget for each iteration.</returns>
        public IEnumerable<Widget> Widgets(params int[] types)
        {
            if (types == null || types.Length == 0)
            {
                foreach (var widget in Widgets())
                    yield return widget;
                yield break;
            }

            var mapped = new List<WidgetType>(types.Length);
            foreach (var type in types)
            {
                if (Enum.IsDefined(typeof(WidgetType), type))
                    mapped.Add((WidgetType)type);
            }

            foreach (var widget in Widgets(mapped.ToArray()))
                yield return widget;
        }

        // ─── Add Annotations ────────────────────────────────────────────
        /// <summary>
        /// PDF only: Add a comment icon ("sticky note") with accompanying text. Only the icon is visible, the accompanying text is hidden and can be visualized by many PDF viewers by hovering the mouse over the symbol.
        /// </summary>
        /// <param name="pos">Top-left point for a 20×20 icon rectangle (annotation APIs).</param>
        /// <param name="text">the commentary text. This will be shown on double clicking or hovering over the icon. May contain any Latin characters.</param>
        /// <param name="icon">choose one of "Note" (default), "Comment", "Help", "Insert", "Key", "NewParagraph", "Paragraph" as the visual symbol for the embodied text .</param>
        /// <returns>the created annotation. Stroke color yellow = (1, 1, 0), no fill color support.</returns>
        public Annot AddTextAnnot(Point pos, string text, string icon = "Note")
        {
            var pdfPage = NativePdfPage;
            var fzPoint = pos.ToFzPoint();
            var annot = mupdf.mupdf.pdf_create_annot(pdfPage, mupdf.pdf_annot_type.PDF_ANNOT_TEXT);
            var r0 = mupdf.mupdf.pdf_annot_rect(annot);
            var r = mupdf.mupdf.fz_make_rect(fzPoint.x, fzPoint.y, fzPoint.x + (r0.x1 - r0.x0), fzPoint.y + (r0.y1 - r0.y0));
            mupdf.mupdf.pdf_set_annot_rect(annot, r);
            mupdf.mupdf.pdf_set_annot_contents(annot, text);
            if (!string.IsNullOrEmpty(icon))
                mupdf.mupdf.pdf_set_annot_icon_name(annot, icon);
            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
        }
        /// <summary>
        /// PDF only: Add text in a given rectangle. Optionally, the appearance of a "callout" shape can be requested by specifying two or three point-like objects; see below.
        /// </summary>
        /// <param name="rect">the rectangle into which the text should be inserted. Text is automatically wrapped to a new line at box width. Text portions not fitting into the rectangle will be invisible without warning.</param>
        /// <param name="text">the text. May contain any mixture of Latin, Greek, Cyrillic, Chinese, Japanese and Korean characters. If `richtext=True` (see below), the string is interpreted as HTML syntax. This adds a plethora of ways for attractive effects.</param>
        /// <param name="fontSize">the fontsize. Default is 11. Ignored if `richtext=True`.</param>
        /// <param name="fontName">The font name. Default is "Helv". Ignored if `richtext=True`, otherwise the following restritions apply:</param>
        /// <param name="dashes">a list of floats specifying how border and callout lines should be dashed. Default is None.</param>
        /// <param name="callout">a list / tuple of two or three point_like objects, which will be interpreted as end point [, knee point] and start point (in this sequence) of up to two line segments, converting this annotation into a call-out shape.</param>
        /// <param name="opacity">a float `0 &lt;= opacity &lt; 1` turning the annotation transparent. Default is no transparency.</param>
        /// <param name="align">text alignment, one of TEXT_ALIGN_LEFT, TEXT_ALIGN_CENTER, TEXT_ALIGN_RIGHT - justify is not supported. Ignored if `richtext=True`.</param>
        /// <param name="rotate">the text orientation. Accepted values are integer multiples of 90°. Invalid entries receive a rotation of 0.</param>
        /// <param name="richtext">treat text as HTML syntax. This allows to achieve bold, *italic*, arbitrary text colors, font sizes, text alignment including justify and more - as far as the PDF subset of HTML and styling instructions supports this. This is similar to what happens in Page.insert_htmlbox. The base library will for example pull in required fonts if it encounters characters not contained in the standard ones. Some parameters are ignored if this option is set, as mentioned above. Default is False.</param>
        /// <param name="style">supply optional HTML styling information in CSS syntax. Ignored if `richtext=False`.</param>
        /// <returns>the created annotation.</returns>
        public Annot AddFreeTextAnnot(
            Rect rect,
            string text,
            float fontSize = 11,
            string fontName = null,
            float[] textColor = null,
            float[] fillColor = null,
            float[] borderColor = null,
            float borderWidth = 0,
            int[] dashes = null,
            Point[] callout = null,
            PdfLineEnding lineEnd = PdfLineEnding.PDF_ANNOT_LE_OPEN_ARROW,
            float opacity = 1,
            int align = 0,
            int rotate = 0,
            bool richtext = false,
            string style = null)
        {
            int oldRotation = annot_preprocess();
            Annot annot;
            try
            {
                string rc = $@"<?xml version=""1.0""?>
                    <body xmlns=""http://www.w3.org/1999/xtml""
                    xmlns:xfa=""http://www.xfa.org/schema/xfa-data/1.0/""
                    xfa:contentType=""text/html"" xfa:APIVersion=""Acrobat:8.0.0"" xfa:spec=""2.4"">
                    {text ?? ""}";

                if (borderColor != null && borderColor.Length > 0 && !richtext)
                    throw new ValueErrorException("cannot set border_color if rich_text is False");
                if (borderColor != null && borderColor.Length > 0 && textColor == null)
                    textColor = borderColor;

                var fColor = Annot.ColorFromSequence(fillColor);
                var tColor = Annot.ColorFromSequence(textColor);

                var r = rect.ToFzRect();
                if (mupdf.mupdf.fz_is_infinite_rect(r) != 0 || mupdf.mupdf.fz_is_empty_rect(r) != 0)
                    throw new ValueErrorException(Constants.MSG_BAD_RECT);

                var page = NativePdfPage;
                var nativeAnnot = mupdf.mupdf.pdf_create_annot(page, mupdf.pdf_annot_type.PDF_ANNOT_FREE_TEXT);
                var annotObj = mupdf.mupdf.pdf_annot_obj(nativeAnnot);

                if (!richtext)
                    mupdf.mupdf.pdf_set_annot_contents(nativeAnnot, text ?? "");
                else
                {
                    mupdf.mupdf.pdf_dict_put_text_string(annotObj, mupdf.mupdf.pdf_new_name("RC"), rc);
                    if (style != null)
                        mupdf.mupdf.pdf_dict_put_text_string(annotObj, mupdf.mupdf.pdf_new_name("DS"), style);
                }

                mupdf.mupdf.pdf_set_annot_rect(nativeAnnot, r);

                while (rotate < 0) rotate += 360;
                while (rotate >= 360) rotate -= 360;
                if (rotate != 0)
                    mupdf.mupdf.pdf_dict_put_int(annotObj, mupdf.mupdf.pdf_new_name("Rotate"), rotate);

                mupdf.mupdf.pdf_set_annot_quadding(nativeAnnot, align);

                if (fColor != null && fColor.Length > 0)
                {
                    Helpers.CheckColor(fColor);
                    Helpers.PdfSetAnnotColor(nativeAnnot, fColor.Length, fColor);
                }

                mupdf.mupdf.pdf_set_annot_border_width(nativeAnnot, borderWidth);
                mupdf.mupdf.pdf_set_annot_opacity(nativeAnnot, opacity);

                if (dashes != null)
                {
                    foreach (var d in dashes)
                        mupdf.mupdf.pdf_add_annot_border_dash_item(nativeAnnot, d);
                }

                if (callout != null && callout.Length > 0)
                {
                    mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("IT"), mupdf.mupdf.pdf_new_name("FreeTextCallout"));
                    mupdf.mupdf.pdf_set_annot_callout_style(nativeAnnot, (mupdf.pdf_line_ending)lineEnd);
                    var vv = new mupdf.vector_fz_point();
                    foreach (var p in callout)
                    {
                        if (p == null) continue;
                        vv.Add(new mupdf.fz_point { x = (float)p.X, y = (float)p.Y });
                    }
                    if (vv.Count > 0)
                        mupdf.mupdf.pdf_set_annot_callout_line2(nativeAnnot, vv);
                }

                if (!richtext)
                {
                    if (tColor != null) Helpers.CheckColor(tColor);
                    Helpers.JM_make_annot_DA(nativeAnnot, tColor?.Length ?? 0, tColor ?? Array.Empty<float>(), fontName ?? "Helv", fontSize);
                }

                mupdf.mupdf.pdf_update_annot(nativeAnnot);
                Helpers.JM_add_annot_id(nativeAnnot, "A");
                annot = new Annot(nativeAnnot, this);
            }
            finally
            {
                if (oldRotation != 0)
                    SetRotation(oldRotation);
            }
            annot_postprocess(annot);
            return annot;
        }
        /// <summary>
        /// PDF only: Add a line annotation.
        /// </summary>
        /// <param name="p1">the starting point of the line.</param>
        /// <param name="p2">the end point of the line.</param>
        /// <returns>the created annotation. It is drawn with line (stroke) color red = (1, 0, 0) and line width 1. No fill color support. The annot rectangle is automatically created to contain both points, each one surrounded by a circle of radius 3 * line width to make room for any line end symbols.</returns>
        public Annot AddLineAnnot(Point p1, Point p2)
        {
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_LINE);
            mupdf.mupdf.pdf_set_annot_line(annot, p1.ToFzPoint(), p2.ToFzPoint());
            mupdf.mupdf.pdf_update_annot(annot);
            Helpers.JM_add_annot_id(annot, "A");
            return new Annot(annot, this);
        }
        /// <summary>
        /// PDF only: add a rectangle annotation.
        /// </summary>
        /// <param name="rect">Target rectangle in unrotated page coordinates.</param>
        public Annot AddRectAnnot(Rect rect)
        {
            var fr = rect.ToFzRect();
            if (mupdf.mupdf.fz_is_infinite_rect(fr) != 0 || mupdf.mupdf.fz_is_empty_rect(fr) != 0)
                throw new ValueErrorException(Constants.MSG_BAD_RECT);
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_SQUARE);
            mupdf.mupdf.pdf_set_annot_rect(annot, fr);
            mupdf.mupdf.pdf_update_annot(annot);
            Helpers.JM_add_annot_id(annot, "A");
            return new Annot(annot, this);
        }
        /// <summary>
        /// PDF only: Add a rectangle, resp. circle annotation.
        /// </summary>
        /// <param name="rect">the rectangle in which the circle or rectangle is drawn, must be finite and not empty. If the rectangle is not equal-sided, an ellipse is drawn.</param>
        /// <returns>the created annotation. It is drawn with line (stroke) color red = (1, 0, 0), line width 1, fill color is supported.</returns>
        public Annot AddCircleAnnot(Rect rect)
        {
            var fr = rect.ToFzRect();
            if (mupdf.mupdf.fz_is_infinite_rect(fr) != 0 || mupdf.mupdf.fz_is_empty_rect(fr) != 0)
                throw new ValueErrorException(Constants.MSG_BAD_RECT);
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_CIRCLE);
            mupdf.mupdf.pdf_set_annot_rect(annot, fr);
            mupdf.mupdf.pdf_update_annot(annot);
            Helpers.JM_add_annot_id(annot, "A");
            return new Annot(annot, this);
        }
        /// <summary>
        /// PDF only: add a multi-line annotation.
        /// </summary>
        /// <param name="points">Vertices for polyline, polygon, or ink annotations.</param>
        public Annot AddPolylineAnnot(Point[] points)
        {
            if (points == null || points.Length < 2)
                throw new ArgumentException(Constants.MSG_BAD_ARG_POINTS);
            foreach (var p in points)
                if (p == null) throw new ArgumentException(Constants.MSG_BAD_ARG_POINTS);
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_POLY_LINE);
            SetAnnotVertices(annot, points);
            mupdf.mupdf.pdf_update_annot(annot);
            Helpers.JM_add_annot_id(annot, "A");
            return new Annot(annot, this);
        }
        /// <summary>
        /// PDF only: Add an annotation consisting of lines which connect the given points. A Polygon's first and last points are automatically connected, which does not happen for a PolyLine. The rectangle is automatically created as the smallest rectangle containing the points, each one surrounded by a circle of radius 3 (= 3 * line width). The following shows a 'PolyLine' that has been modified with colors and line ends.
        /// </summary>
        /// <param name="points">a list of point_like objects.</param>
        /// <returns>the created annotation. It is drawn with line color black, line width 1 no fill color but fill color support. Use methods of Annot to make any changes to achieve something like this:</returns>
        public Annot AddPolygonAnnot(Point[] points)
        {
            if (points == null || points.Length < 2)
                throw new ArgumentException(Constants.MSG_BAD_ARG_POINTS);
            foreach (var p in points)
                if (p == null) throw new ArgumentException(Constants.MSG_BAD_ARG_POINTS);
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_POLYGON);
            SetAnnotVertices(annot, points);
            mupdf.mupdf.pdf_update_annot(annot);
            Helpers.JM_add_annot_id(annot, "A");
            return new Annot(annot, this);
        }
        /// <summary>
        /// PDF only: These annotations are normally used for marking text which has previously been somehow located (for example via Page.search_for). But this is not required: you are free to "mark" just anything.
        /// </summary>
        public Annot AddHighlightAnnot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_HIGHLIGHT, quads, start, stop, clip);
        /// <summary>
        /// PDF only: These annotations are normally used for marking text which has previously been somehow located (for example via Page.search_for). But this is not required: you are free to "mark" just anything.
        /// </summary>
        public Annot AddHighlightAnnot(Rect rect)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_HIGHLIGHT, rect, null, null, null);
        /// <summary>
        /// PDF only: These annotations are normally used for marking text which has previously been somehow located (for example via Page.search_for). But this is not required: you are free to "mark" just anything.
        /// </summary>
        public Annot AddHighlightAnnot(object quads, Point start = null, Point stop = null, IRect clip = null)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_HIGHLIGHT, quads, start, stop, clip);
        /// <summary>
        /// PDF only: add an "underline" annotation.
        /// </summary>
        public Annot AddUnderlineAnnot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_UNDERLINE, quads, start, stop, clip);
        /// <summary>
        /// PDF only: add an "underline" annotation.
        /// </summary>
        public Annot AddUnderlineAnnot(Rect rect)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_UNDERLINE, rect, null, null, null);
        /// <summary>
        /// PDF only: add an "underline" annotation.
        /// </summary>
        public Annot AddUnderlineAnnot(object quads, Point start = null, Point stop = null, IRect clip = null)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_UNDERLINE, quads, start, stop, clip);
        /// <summary>
        /// PDF only: add a "strike-out" annotation.
        /// </summary>
        public Annot AddStrikeoutAnnot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_STRIKE_OUT, quads, start, stop, clip);
        /// <summary>
        /// PDF only: add a "strike-out" annotation.
        /// </summary>
        public Annot AddStrikeoutAnnot(Rect rect, Point start = null, Point stop = null, IRect clip = null)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_STRIKE_OUT, rect, start, stop, clip);
        /// <summary>
        /// PDF only: add a "strike-out" annotation.
        /// </summary>
        public Annot AddStrikeoutAnnot(object quads, Point start = null, Point stop = null, IRect clip = null)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_STRIKE_OUT, quads, start, stop, clip);
        /// <summary>
        /// PDF only: add a "squiggly" annotation.
        /// </summary>
        public Annot AddSquigglyAnnot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_SQUIGGLY, quads, start, stop, clip);
        /// <summary>
        /// PDF only: add a "squiggly" annotation.
        /// </summary>
        public Annot AddSquigglyAnnot(Rect rect)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_SQUIGGLY, rect, null, null, null);
        /// <summary>
        /// PDF only: add a "squiggly" annotation.
        /// </summary>
        public Annot AddSquigglyAnnot(object quads, Point start = null, Point stop = null, IRect clip = null)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_SQUIGGLY, quads, start, stop, clip);

        /// <summary>Add a 'Caret' annotation. PyMuPDF <c>Page.add_caret_annot</c>.</summary>
        internal Annot add_caret_annot(object point)
        {
            // """Add a 'Caret' annotation."""
            int old_rotation = annot_preprocess();
            mupdf.PdfAnnot pdf_annot;
            try
            {
                // annot = self._add_caret_annot(point)
                pdf_annot = _add_caret_annot(point);
            }
            finally
            {
                // if old_rotation != 0:
                if (old_rotation != 0)
                    // self.set_rotation(old_rotation)
                    SetRotation(old_rotation);
            }
            // annot = Annot( annot)
            Annot annot = new Annot(pdf_annot, this);
            annot_postprocess(annot);
            // assert hasattr( annot, 'parent')
            System.Diagnostics.Debug.Assert(annot.Parent != null);
            return annot;
        }

        /// <summary>PyMuPDF <c>Page._add_caret_annot</c>.</summary>
        internal mupdf.PdfAnnot _add_caret_annot(object point)
        {
            // if g_use_extra:
            //     annot = extra._add_caret_annot( self.this, JM_point_from_py(point))
            // else:
            // page = self._pdf_page()
            mupdf.PdfPage page = _pdf_page();
            // annot = mupdf.pdf_create_annot(page, mupdf.PDF_ANNOT_CARET)
            mupdf.PdfAnnot annot = mupdf.mupdf.pdf_create_annot(page, mupdf.pdf_annot_type.PDF_ANNOT_CARET);
            // if point:
            if (point != null)
            {
                // p = JM_point_from_py(point)
                mupdf.FzPoint p = Helpers.JM_point_from_py(point);
                // r = mupdf.pdf_annot_rect(annot)
                mupdf.FzRect r = mupdf.mupdf.pdf_annot_rect(annot);
                // r = mupdf.FzRect(p.x, p.y, p.x + r.x1 - r.x0, p.y + r.y1 - r.y0)
                r = new mupdf.FzRect(p.x, p.y, p.x + r.x1 - r.x0, p.y + r.y1 - r.y0);
                // mupdf.pdf_set_annot_rect(annot, r)
                mupdf.mupdf.pdf_set_annot_rect(annot, r);
            }
            // mupdf.pdf_update_annot(annot)
            mupdf.mupdf.pdf_update_annot(annot);
            // JM_add_annot_id(annot, "A")
            Helpers.JM_add_annot_id(annot, "A");
            // return annot
            return annot;
        }
        /// <summary>
        /// PDF only: Add a caret icon. A caret annotation is a visual symbol normally used to indicate the presence of text edits on the page.
        /// </summary>
        public Annot AddCaretAnnot(Point point) => add_caret_annot(point);
        /// <summary>
        /// PDF only: Add a "rubber stamp" annotation to e.g. indicate the document's intended use ("DRAFT", "CONFIDENTIAL", etc.). The parameter may be either an integer to select text from a predefined array of standard texts or an image.
        /// </summary>
        public Annot AddStampAnnot(Rect rect, int stamp = 0) =>
            AddStampAnnotFromPayload(rect, StampBuiltinNameFromIndex(stamp), imageBytes: null, usePixmap: null);
        /// <summary>
        /// PDF only: Add a "rubber stamp" annotation to e.g. indicate the document's intended use ("DRAFT", "CONFIDENTIAL", etc.). The parameter may be either an integer to select text from a predefined array of standard texts or an image.
        /// </summary>
        /// <param name="rect">rectangle where to place the annotation.</param>
        public Annot AddStampAnnot(Rect rect, byte[] imageBytes)
        {
            if (imageBytes == null)
                throw new ArgumentNullException(nameof(imageBytes));
            if (imageBytes.Length == 0)
                throw new ArgumentException("image bytes are empty", nameof(imageBytes));
            return AddStampAnnotFromPayload(rect, builtinName: null, imageBytes, usePixmap: null);
        }
        /// <summary>
        /// PDF only: Add a "rubber stamp" annotation to e.g. indicate the document's intended use ("DRAFT", "CONFIDENTIAL", etc.). The parameter may be either an integer to select text from a predefined array of standard texts or an image.
        /// </summary>
        /// <param name="rect">rectangle where to place the annotation.</param>
        public Annot AddStampAnnot(Rect rect, string imageFilePath)
        {
            if (imageFilePath == null)
                throw new ArgumentNullException(nameof(imageFilePath));
            var bytes = File.ReadAllBytes(imageFilePath);
            return AddStampAnnot(rect, bytes);
        }
        /// <summary>
        /// PDF only: Add a "rubber stamp" annotation to e.g. indicate the document's intended use ("DRAFT", "CONFIDENTIAL", etc.). The parameter may be either an integer to select text from a predefined array of standard texts or an image.
        /// </summary>
        /// <param name="rect">rectangle where to place the annotation.</param>
        /// <param name="pixmap">Source pixmap for insert or replace image.</param>
        public Annot AddStampAnnot(Rect rect, Pixmap pixmap)
        {
            if (pixmap == null)
                throw new ArgumentNullException(nameof(pixmap));
            return AddStampAnnotFromPayload(rect, builtinName: null, imageBytes: null, usePixmap: pixmap);
        }

        /// <summary>Core implementation of <see cref="AddStampAnnot(Rect, int)"/> and image overloads (PyMuPDF <c>Page._add_stamp_annot</c>).</summary>
        private Annot AddStampAnnotFromPayload(Rect rect, string? builtinName, byte[]? imageBytes, Pixmap? usePixmap)
        {
            if (!RequireParent().IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);

            var r = rect.ToFzRect();
            if (mupdf.mupdf.fz_is_infinite_rect(r) != 0 || mupdf.mupdf.fz_is_empty_rect(r) != 0)
                throw new ValueErrorException(Constants.MSG_BAD_RECT);

            int oldRotation = Rotation;
            bool clearedRotation = oldRotation != 0;
            if (clearedRotation)
                SetRotation(0);

            try
            {
                var page = NativePdfPage;
                var annot = mupdf.mupdf.pdf_create_annot(page, mupdf.pdf_annot_type.PDF_ANNOT_STAMP);
                var annotObj = mupdf.mupdf.pdf_annot_obj(annot);

                bool imageStamp = (imageBytes != null && imageBytes.Length > 0) || usePixmap != null;
                if (imageStamp)
                {
                    mupdf.FzImage fzImg;
                    if (usePixmap != null)
                    {
                        fzImg = mupdf.mupdf.fz_new_image_from_pixmap(usePixmap.NativePixmap, new mupdf.FzImage());
                    }
                    else
                    {
                        using var buf = Helpers.BufferFromBytes(imageBytes!);
                        fzImg = mupdf.mupdf.fz_new_image_from_buffer(buf);
                    }

                    using (fzImg)
                    {
                        int w = fzImg.w();
                        int h = fzImg.h();
                        float scale = Math.Min(rect.Width / w, rect.Height / h);
                        float width = w * scale;
                        float height = h * scale;
                        var center = (rect.TL + rect.BR) / 2.0f;
                        float x0 = (float)(center.X - width / 2.0f);
                        float y0 = (float)(center.Y - height / 2.0f);
                        float x1 = (float)(x0 + width);
                        float y1 = (float)(y0 + height);
                        var rImg = mupdf.mupdf.fz_make_rect(x0, y0, x1, y1);
                        mupdf.mupdf.pdf_set_annot_rect(annot, rImg);
                        annot.pdf_set_annot_stamp_image(fzImg);
                        mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("Name"), mupdf.mupdf.pdf_new_name("ImageStamp"));
                        mupdf.mupdf.pdf_set_annot_contents(annot, "Image Stamp");
                    }
                }
                else
                {
                    string name = builtinName ?? StampBuiltinNameFromIndex(0);
                    mupdf.mupdf.pdf_set_annot_rect(annot, r);
                    mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("Name"), mupdf.mupdf.pdf_new_name(name));
                    mupdf.mupdf.pdf_set_annot_contents(annot, name);
                }

                mupdf.mupdf.pdf_update_annot(annot);
                Helpers.JM_add_annot_id(annot, "A");
                return new Annot(annot, this);
            }
            finally
            {
                if (clearedRotation)
                    SetRotation(oldRotation);
            }
        }
        /// <summary>
        /// PDF only: Add a file attachment annotation with a "PushPin" icon at the specified location.
        /// </summary>
        public Annot add_file_annot(object point, byte[] buffer_, string filename, string ufilename = null, string desc = null, string icon = null)
            => add_file_annot_impl(point, buffer_, filename, uFileName: ufilename, desc: desc, icon: icon);

        internal Annot add_file_annot_impl(object point, byte[] buffer_, string filename, string uFileName = null, string desc = null, string icon = null)
        {
            // """Add a 'FileAttachment' annotation."""
            int old_rotation = annot_preprocess();
            Annot annot;
            try
            {
                annot = _add_file_annot(point, buffer_, filename, uFileName, desc, icon);
            }
            finally
            {
                // if old_rotation != 0:
                if (old_rotation != 0)
                    // self.set_rotation(old_rotation)
                    SetRotation(old_rotation);
            }
            annot_postprocess(annot);
            return annot;
        }

        /// <summary>PyMuPDF <c>Page._add_file_annot</c>.</summary>
        internal Annot _add_file_annot(object point, byte[] buffer_, string filename, string uFileName = null, string desc = null, string icon = null)
        {
            // page = self._pdf_page()
            mupdf.PdfPage page = _pdf_page();
            // uf = ufilename if ufilename else filename
            string uf = uFileName != null && uFileName.Length > 0 ? uFileName : filename;
            // d = desc if desc else filename
            string d = desc != null && desc.Length > 0 ? desc : filename;
            // p = JM_point_from_py(point)
            mupdf.FzPoint p = Helpers.JM_point_from_py(point);
            // filebuf = JM_BufferFromBytes(buffer_)
            mupdf.FzBuffer filebuf = Helpers.BufferFromBytes(buffer_);
            // if not filebuf.m_internal:
            if (filebuf?.m_internal == null)
                // raise TypeError( MSG_BAD_BUFFER)
                throw new ArgumentException(Constants.MSG_BAD_BUFFER);
            // annot = mupdf.pdf_create_annot(page, mupdf.PDF_ANNOT_FILE_ATTACHMENT)
            mupdf.PdfAnnot annot = mupdf.mupdf.pdf_create_annot(page, mupdf.pdf_annot_type.PDF_ANNOT_FILE_ATTACHMENT);
            // r = mupdf.pdf_annot_rect(annot)
            mupdf.FzRect r = mupdf.mupdf.pdf_annot_rect(annot);
            // r = mupdf.fz_make_rect(p.x, p.y, p.x + r.x1 - r.x0, p.y + r.y1 - r.y0)
            r = mupdf.mupdf.fz_make_rect(p.x, p.y, p.x + r.x1 - r.x0, p.y + r.y1 - r.y0);
            // mupdf.pdf_set_annot_rect(annot, r)
            mupdf.mupdf.pdf_set_annot_rect(annot, r);
            // flags = mupdf.PDF_ANNOT_IS_PRINT
            int flags = mupdf.mupdf.PDF_ANNOT_IS_PRINT;
            // mupdf.pdf_set_annot_flags(annot, flags)
            mupdf.mupdf.pdf_set_annot_flags(annot, flags);
            // if icon:
            if (!string.IsNullOrEmpty(icon))
                // mupdf.pdf_set_annot_icon_name(annot, icon)
                mupdf.mupdf.pdf_set_annot_icon_name(annot, icon);
            // val = JM_embed_file(page.doc(), filebuf, filename, uf, d, 1)
            mupdf.PdfObj val = Helpers.JmEmbedFile(page.doc(), filebuf, filename, uf, d, 1);
            // mupdf.pdf_dict_put(mupdf.pdf_annot_obj(annot), PDF_NAME('FS'), val)
            mupdf.mupdf.pdf_dict_put(mupdf.mupdf.pdf_annot_obj(annot), mupdf.mupdf.pdf_new_name("FS"), val);
            // mupdf.pdf_dict_put_text_string(mupdf.pdf_annot_obj(annot), PDF_NAME('Contents'), filename)
            mupdf.mupdf.pdf_dict_put_text_string(mupdf.mupdf.pdf_annot_obj(annot), mupdf.mupdf.pdf_new_name("Contents"), filename);
            // mupdf.pdf_update_annot(annot)
            mupdf.mupdf.pdf_update_annot(annot);
            // mupdf.pdf_set_annot_rect(annot, r)
            mupdf.mupdf.pdf_set_annot_rect(annot, r);
            // mupdf.pdf_set_annot_flags(annot, flags)
            mupdf.mupdf.pdf_set_annot_flags(annot, flags);
            // JM_add_annot_id(annot, "A")
            Helpers.JM_add_annot_id(annot, "A");
            // return Annot(annot)
            return new Annot(annot, this);
        }
        /// <summary>
        /// PDF only: Add a file attachment annotation with a "PushPin" icon at the specified location.
        /// </summary>
        public Annot AddFileAnnot(object point, byte[] buffer_, string filename, string uFileName = null, string desc = null, string icon = null)
            => add_file_annot_impl(point, buffer_, filename, uFileName, desc, icon);
        /// <summary>
        /// PDF only: Add a "freehand" scribble annotation.
        /// </summary>
        /// <param name="handwriting">Ink strokes as arrays of points.</param>
        /// <returns>the created annotation in default appearance black =(0, 0, 0),line width 1. No fill color support.</returns>
        public Annot AddInkAnnot(Point[][] handwriting)
        {
            int old_rotation = annot_preprocess();
            Annot annot;
            try
            {
                annot = _add_ink_annot(handwriting);
            }
            finally
            {
                if (old_rotation != 0)
                    SetRotation(old_rotation);
            }
            annot_postprocess(annot);
            return annot;
        }

        internal Annot add_ink_annot(object handwriting)
        {
            int old_rotation = annot_preprocess();
            Annot annot;
            try
            {
                annot = _add_ink_annot(handwriting);
            }
            finally
            {
                if (old_rotation != 0)
                    SetRotation(old_rotation);
            }
            annot_postprocess(annot);
            return annot;
        }

        internal int annot_preprocess()
        {
            RequireParent();
            if (!RequireParent().IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);
            int old_rotation = Rotation;
            if (old_rotation != 0)
                SetRotation(0);
            return old_rotation;
        }

        internal void annot_postprocess(Annot annot)
        {
            // annot.parent = page
            // page._annot_refs[id(annot)] = annot
            RegisterAnnotRef(annot);
            // annot.thisown = True
        }

        internal Annot _add_ink_annot(object list)
        {
            var page = NativePdfPage;
            if (list is not System.Collections.IList strokes)
                throw new ValueErrorException(Constants.MSG_BAD_ARG_INK_ANNOT);
            var ctm = new mupdf.FzMatrix();
            page.pdf_page_transform(new mupdf.FzRect(0, 0, 0, 0), ctm);
            var inv_ctm = ctm.fz_invert_matrix();
            var annot = mupdf.mupdf.pdf_create_annot(page, mupdf.pdf_annot_type.PDF_ANNOT_INK);
            var annot_obj = mupdf.mupdf.pdf_annot_obj(annot);
            int n0 = strokes.Count;
            var inklist = mupdf.mupdf.pdf_new_array(page.doc(), n0);

            for (int j = 0; j < n0; j++)
            {
                if (strokes[j] is not System.Collections.IList sublist)
                    throw new ValueErrorException(Constants.MSG_BAD_ARG_INK_ANNOT);
                int n1 = sublist.Count;
                var stroke = mupdf.mupdf.pdf_new_array(page.doc(), 2 * n1);

                for (int i = 0; i < n1; i++)
                {
                    object p = sublist[i];
                    if (p is not System.Collections.IList && p is not Point)
                        throw new ValueErrorException(Constants.MSG_BAD_ARG_INK_ANNOT);
                    if (p is System.Collections.IList seq && seq.Count != 2)
                        throw new ValueErrorException(Constants.MSG_BAD_ARG_INK_ANNOT);
                    var point = mupdf.FzPoint.fz_transform_point(Helpers.JM_point_from_py(p), inv_ctm);
                    mupdf.mupdf.pdf_array_push_real(stroke, point.x);
                    mupdf.mupdf.pdf_array_push_real(stroke, point.y);
                }

                mupdf.mupdf.pdf_array_push(inklist, stroke);
            }

            mupdf.mupdf.pdf_dict_put(annot_obj, mupdf.mupdf.pdf_new_name("InkList"), inklist);
            mupdf.mupdf.pdf_update_annot(annot);
            Helpers.JM_add_annot_id(annot, "A");
            return new Annot(annot, this);
        }
        /// <summary>
        /// , text_color=(0, 0, 0), cross_out=True).
        /// </summary>
        /// <param name="quad">specifies the (rectangular) area to be removed which is always equal to the annotation rectangle. This may be a rect_like or quad_like object. If a quad is specified, then the enveloping rectangle is taken.</param>
        /// <param name="text">text to be placed in the rectangle after applying the redaction (and thus removing old content).</param>
        /// <param name="fontName">the font to use when text is given, otherwise ignored. Only CJK and the Base-14-Fonts are supported. Apart from this, the same rules apply as for Page.insert_textbox; which is what the method Page.apply_redactions internally invokes.</param>
        /// <param name="fontSize">the fontsize to use for the replacing text. If the text is too large to fit, several insertion attempts will be made, gradually reducing the fontsize to no less than 4. If then the text will still not fit, no text insertion will take place at all.</param>
        /// <param name="align">the horizontal alignment for the replacing text. See insert_textbox for available values. The vertical alignment is (approximately) centered.</param>
        /// <returns>the created annotation. Its standard appearance looks like a red rectangle (no fill color), optionally showing two diagonal lines. Colors, line width, dashing, opacity and blend mode can now be set and applied via Annot.update like with other annotations. (Changed in v1.17.2)</returns>
        public Annot AddRedactAnnot(Quad quad, string? text = null, string? fontName = null, float fontSize = 11,
            int align = 0, float[]? fillColor = null, float[]? textColor = null, bool crossOut = true)
        {
            if (!RequireParent().IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);

            string? overlayText = text;
            string? daStr = null;
            float[]? fillResolved = fillColor;

            if (!string.IsNullOrEmpty(text) && !IsWhitespaceOnlyString(text))
            {
                Helpers.CheckColor(fillColor);
                Helpers.CheckColor(textColor);
                if (string.IsNullOrEmpty(fontName))
                    fontName = "Helv";
                if (fontSize == 0)
                    fontSize = 11;

                float[] tc = textColor;
                if (tc == null || tc.Length == 0)
                    tc = new float[] { 0, 0, 0 };
                else if (tc.Length == 1)
                    tc = new float[] { tc[0], tc[0], tc[0] };
                else if (tc.Length > 3)
                    tc = new float[] { tc[0], tc[1], tc[2] };

                daStr = $"{tc[0]:g} {tc[1]:g} {tc[2]:g} rg /{fontName} {fontSize:g} Tf";

                if (fillResolved == null)
                    fillResolved = new float[] { 1, 1, 1 };
                else if (fillResolved.Length == 1)
                    fillResolved = new float[] { fillResolved[0], fillResolved[0], fillResolved[0] };
                else if (fillResolved.Length > 3)
                    fillResolved = new float[] { fillResolved[0], fillResolved[1], fillResolved[2] };
            }
            else
            {
                overlayText = null;
            }

            int oldRotation = Rotation;
            bool clearedRotation = oldRotation != 0;
            if (clearedRotation)
                SetRotation(0);

            try
            {
                var annot = AddRedactAnnotCore(quad, overlayText, daStr, align, fillResolved);
                if (crossOut)
                    TryAppendRedactCrossOutAppearance(annot);
                return annot;
            }
            finally
            {
                if (clearedRotation)
                    SetRotation(oldRotation);
            }
        }
        /// <summary>
        /// , text_color=(0, 0, 0), cross_out=True).
        /// </summary>
        public Annot AddRedactAnnot(Rect rect, string? text = null, string? fontName = null, float fontSize = 11,
            int align = 0, float[]? fillColor = null, float[]? textColor = null, bool crossOut = true)
            => AddRedactAnnot(new Quad(rect), text, fontName, fontSize, align, fillColor, textColor, crossOut);

        /// <summary>PyMuPDF <c>Page._add_redact_annot</c> (no rotation preprocess; used by compat and internally after <see cref="AddRedactAnnot"/> prepares overlay).</summary>
        internal Annot AddRedactAnnotCore(Quad quad, string? text, string? daStr, int align, float[]? fill)
        {
            var pdfPage = NativePdfPage;
            var pdf = RequireParent().NativePdfDocument;
            var fzq = Helpers.QuadToFz(quad);
            var r = mupdf.mupdf.fz_rect_from_quad(fzq);
            var annot = mupdf.mupdf.pdf_create_annot(pdfPage, mupdf.pdf_annot_type.PDF_ANNOT_REDACT);
            mupdf.mupdf.pdf_set_annot_rect(annot, r);

            var annotObj = mupdf.mupdf.pdf_annot_obj(annot);
            var (nf, fcol) = ColorComponentsForRedactInterior(fill);
            if (nf > 0)
            {
                var arr = mupdf.mupdf.pdf_new_array(pdf, nf);
                for (int i = 0; i < nf; i++)
                    mupdf.mupdf.pdf_array_push_real(arr, fcol[i]);
                mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("IC"), arr);
            }

            if (!string.IsNullOrEmpty(text))
            {
                if (string.IsNullOrEmpty(daStr))
                    throw new InvalidOperationException("DA string is required when redact overlay text is set.");
                mupdf.mupdf.pdf_dict_puts(annotObj, "OverlayText", mupdf.mupdf.pdf_new_text_string(text));
                mupdf.mupdf.pdf_dict_put_text_string(annotObj, mupdf.mupdf.pdf_new_name("DA"), daStr);
                mupdf.mupdf.pdf_dict_put_int(annotObj, mupdf.mupdf.pdf_new_name("Q"), align);
            }

            mupdf.mupdf.pdf_update_annot(annot);
            Helpers.JM_add_annot_id(annot, "A");
            return new Annot(annot, this);
        }

        private static bool IsWhitespaceOnlyString(string s)
        {
            foreach (char c in s)
            {
                if (!char.IsWhiteSpace(c))
                    return false;
            }
            return true;
        }

        /// <summary>PyMuPDF <c>JM_color_FromSequence</c> subset for <c>/IC</c> (1, 3, or 4 components; clamp to [0,1]).</summary>
        private static (int nf, float[] comps) ColorComponentsForRedactInterior(float[]? color)
        {
            if (color == null || color.Length == 0)
                return (0, Array.Empty<float>());
            float[] ret;
            if (color.Length == 1)
                ret = new[] { color[0] };
            else if (color.Length == 3)
                ret = new[] { color[0], color[1], color[2] };
            else if (color.Length == 4)
                ret = new[] { color[0], color[1], color[2], color[3] };
            else
                return (0, Array.Empty<float>());

            for (int i = 0; i < ret.Length; i++)
            {
                if (ret[i] < 0 || ret[i] > 1)
                    ret[i] = 1f;
            }
            return (ret.Length, ret);
        }

        /// <summary>PyMuPDF <c>add_redact_annot</c> cross-out: extend the <c>/AP</c> <c>/N</c> stream with diagonals after <c>pdf_update_annot</c>.</summary>
        private static void TryAppendRedactCrossOutAppearance(Annot annot)
        {
            string ap = annot.GetAP("N");
            if (string.IsNullOrEmpty(ap))
                return;
            var lines = ap.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').ToList();
            if (lines.Count < 2)
                return;
            lines.RemoveAt(lines.Count - 1);
            if (lines.Count < 5)
                return;
            string ll = lines[1];
            string lr = lines[2];
            string ur = lines[3];
            string ul = lines[4];
            lines.Add(lr);
            lines.Add(ll);
            lines.Add(ur);
            lines.Add(ll);
            lines.Add(ul);
            lines.Add("S");
            string newAp = string.Join("\n", lines);
            annot.SetAP("N", Encoding.UTF8.GetBytes(newAp));
        }
        /// <summary>
        /// .. note::.
        /// </summary>
        /// <param name="images">How to redact overlapping images. The default `PDF_REDACT_IMAGE_PIXELS | 2` blanks out overlapping pixels. `PDF_REDACT_IMAGE_NONE | 0` ignores, and `PDF_REDACT_IMAGE_REMOVE | 1` completely removes images overlapping any redaction annotation. Option `PDF_REDACT_IMAGE_REMOVE_UNLESS_INVISIBLE | 3` only removes images that are actually visible.</param>
        /// <param name="graphics">How to redact overlapping vector graphics (also called "line-art" or "drawings"). The default `PDF_REDACT_LINE_ART_REMOVE_IF_COVERED | 1` removes any overlapping vector graphics. `PDF_REDACT_LINE_ART_NONE | 0` ignores, and `PDF_REDACT_LINE_ART_REMOVE_IF_TOUCHED | 2` removes graphics fully contained in a redaction annotation. When removing line-art, please be aware that stroked vector graphics (i.e. type "s" or "sf") have a larger wrapping rectangle than one might expect: first of all, at least 50% of the path's line width have to be added in each direction to truly include all of the drawing. If a so-called "miter limit" is provided (see page 121 of the PDF specification), the enlarging value is `miter * width / 2`. So, when letting everything default (width = 1, miter = 10), the redaction rectangle should be at least 5 points larger in every direction.</param>
        /// <param name="text">Whether to redact overlapping text. The default `PDF_REDACT_TEXT_REMOVE | 0` removes all characters whose boundary box overlaps any redaction rectangle. This complies with the original legal / data protection intentions of redaction annotations. Other use cases however may require to keep text while redacting vector graphics or images. This can be achieved by setting `text=True|PDF_REDACT_TEXT_NONE | 1`. This does not comply with the data protection intentions of redaction annotations. Do so at your own risk.</param>
        /// <returns>`True` if at least one redaction annotation has been processed, `False` otherwise.</returns>
        public bool ApplyRedactions(int images = 2, int graphics = 1, int text = 0)
        {
            var doc = RequireParent();
            if (doc.IsClosed || doc.IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            if (!doc.IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);

            var snapshots = new List<Dictionary<string, object>>();
            foreach (var annot in Annots(AnnotationType.Redact))
            {
                var snap = TryBuildRedactApplySnapshot(annot);
                if (snap != null)
                    snapshots.Add(snap);
            }

            if (snapshots.Count == 0)
                return false;

            if (!ApplyRedactionsPdfOnly(text, images, graphics))
                throw new ValueErrorException("Error applying redactions.");

            using var shape = NewShape();
            foreach (var redact in snapshots)
            {
                var annotRect = (Rect)redact["rect"];
                if (redact.TryGetValue("fill", out var fillObj) && fillObj is float[] fillArr && fillArr.Length > 0)
                {
                    shape.DrawRect(annotRect);
                    shape.Finish(fill: fillArr, color: fillArr, strokeOpacity: 1, fillOpacity: 1);
                }

                if (!redact.ContainsKey("text") || redact["text"] is not string newText)
                    continue;

                int align = redact.TryGetValue("align", out var ao) ? Convert.ToInt32(ao) : 0;
                string fname = redact.TryGetValue("fontname", out var fo) && fo != null ? Convert.ToString(fo) : "helv";
                if (string.IsNullOrEmpty(fname))
                    fname = "helv";
                float fsize = redact.TryGetValue("fontsize", out var fso) ? Convert.ToSingle(fso) : 11f;
                if (fsize <= 0)
                    fsize = 11f;
                float[] color = NormalizeRgbTextColor(redact.TryGetValue("text_color", out var co) ? co as float[] : null);

                var trect = RedactOverlayCenterRect(annotRect, newText, fname, fsize);
                float rc = -1;
                while (rc < 0 && fsize >= 4)
                {
                    rc = shape.InsertTextbox(
                        trect,
                        newText,
                        align: align,
                        color: color,
                        fontname: fname,
                        fontsize: fsize);
                    fsize -= 0.5f;
                }
            }
            shape.Commit();

            return true;
        }

        /// <summary>PyMuPDF <c>Page._apply_redactions</c>: MuPDF <c>pdf_redact_page</c> only (no overlay redraw).</summary>
        internal bool ApplyRedactionsPdfOnly(int text, int images, int graphics)
        {
            var pdfPage = NativePdfPage;
            var opts = new mupdf.PdfRedactOptions();
            opts.black_boxes = 0;
            opts.text = text;
            opts.image_method = images;
            opts.line_art = graphics;
            int result = mupdf.mupdf.pdf_redact_page(RequireParent().NativePdfDocument, pdfPage, opts);
            return result != 0;
        }

        /// <summary>PyMuPDF <c>Annot._get_redact_values</c> subset for <see cref="ApplyRedactions"/>.</summary>
        private static Dictionary<string, object>? TryBuildRedactApplySnapshot(Annot annot)
        {
            try
            {
                if (annot.AnnotationType != AnnotationType.Redact)
                    return null;

                var native = annot.NativeAnnot;
                var annotObj = mupdf.mupdf.pdf_annot_obj(native);

                string overlayText;
                var ot = mupdf.mupdf.pdf_dict_gets(annotObj, "OverlayText");
                if (ot.m_internal != null)
                    overlayText = mupdf.mupdf.pdf_to_text_string(ot) ?? "";
                else
                    overlayText = "";

                var qObj = mupdf.mupdf.pdf_dict_get(annotObj, mupdf.mupdf.pdf_new_name("Q"));
                int align = qObj.m_internal != null ? mupdf.mupdf.pdf_to_int(qObj) : 0;

                var dict = new Dictionary<string, object>
                {
                    ["text"] = overlayText,
                    ["align"] = align,
                    ["rect"] = annot.Rect,
                };

                var (tcol, font, fsize) = Helpers.ParseAnnotDefaultAppearance(native);
                dict["text_color"] = tcol;
                dict["fontname"] = string.IsNullOrEmpty(font) ? "Helv" : font;
                dict["fontsize"] = fsize <= 0 ? 11f : fsize;

                var colors = Helpers.JM_annot_colors(annotObj);
                if (colors.TryGetValue("fill", out var fillObj) && fillObj is float[] fa && fa.Length > 0)
                    dict["fill"] = fa;

                return dict;
            }
            catch
            {
                return null;
            }
        }

        private static float[] NormalizeRgbTextColor(float[]? c)
        {
            if (c == null || c.Length == 0)
                return new float[] { 0, 0, 0 };
            if (c.Length == 1)
                return new float[] { c[0], c[0], c[0] };
            if (c.Length >= 3)
                return new float[] { c[0], c[1], c[2] };
            return new float[] { 0, 0, 0 };
        }

        private static Rect RedactOverlayCenterRect(Rect annotRect, string newText, string font, float fsize)
        {
            const double Epsilon = 1e-5;
            if (string.IsNullOrEmpty(newText) || annotRect.Width <= Epsilon)
                return annotRect;
            try
            {
                float textWidth = Utils.GetTextLength(newText, font, fsize);
                float lineHeight = fsize * 1.2f;
                float limit = (float)annotRect.Width;
                float h = (float)(Math.Ceiling(textWidth / limit) * lineHeight);
                if (h >= annotRect.Height)
                    return annotRect;
                var r = new Rect(annotRect);
                float y = (annotRect.TL.Y + annotRect.BL.Y - h) * 0.5f;
                r.Y0 = y;
                return r;
            }
            catch (ArgumentException)
            {
                return annotRect;
            }
        }
        /// <summary>
        /// PDF only: Delete annotation from the page and return the next one.
        /// </summary>
        /// <param name="annot">Annotation to delete or reference.</param>
        /// <returns>the annotation following the deleted one. Please remember that physical removal requires saving to a new file with garbage &gt; 0.</returns>
        public Annot DeleteAnnot(Annot annot)
        {
            var pdfPage = NativePdfPage;
            var nextAnnot = mupdf.mupdf.pdf_next_annot(annot.NativeAnnot);
            mupdf.mupdf.pdf_delete_annot(pdfPage, annot.NativeAnnot);
            if (nextAnnot != null && nextAnnot.m_internal != null)
                return new Annot(nextAnnot, this);
            return null;
        }
        /// <summary>
        /// PDF only: Delete the specified link from the page. The parameter must be an original item of get_links(), see link_dict_description. The reason for this is the dictionary's *"xref"* key, which identifies the PDF object to be deleted.
        /// </summary>
        /// <param name="link">Link wrapper to delete.</param>
        public void DeleteLink(Link link)
        {
            if (link == null) return;
            var pdf = RequireParent().NativePdfDocument;
            var pdfPage = NativePdfPage;
            var annotArray = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Annots"));
            if (annotArray.m_internal == null)
            {
                Helpers.JM_refresh_links(pdf, pdfPage);
                link.Erase();
                SyncLinkWrapperCache();
                return;
            }
            int n = mupdf.mupdf.pdf_array_len(annotArray);
            var linkRect = link.Rect;
            for (int i = n - 1; i >= 0; i--)
            {
                var obj = mupdf.mupdf.pdf_array_get(annotArray, i);
                var subtype = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("Subtype"));
                if (mupdf.mupdf.pdf_name_eq(subtype, mupdf.mupdf.pdf_new_name("Link")) != 0)
                {
                    var r = mupdf.mupdf.pdf_dict_get_rect(obj, mupdf.mupdf.pdf_new_name("Rect"));
                    var cr = new Rect(r.x0, r.y0, r.x1, r.y1);
                    if (Math.Abs(cr.X0 - linkRect.X0) < 1 && Math.Abs(cr.Y0 - linkRect.Y0) < 1
                        && Math.Abs(cr.X1 - linkRect.X1) < 1 && Math.Abs(cr.Y1 - linkRect.Y1) < 1)
                    {
                        mupdf.mupdf.pdf_array_delete(annotArray, i);
                        break;
                    }
                }
            }
            Helpers.JM_refresh_links(pdf, pdfPage);
            link.Erase();
            SyncLinkWrapperCache();
        }
        /// <summary>
        /// PDF only: Delete the specified link from the page. The parameter must be an original item of get_links(), see link_dict_description. The reason for this is the dictionary's *"xref"* key, which identifies the PDF object to be deleted.
        /// </summary>
        /// <param name="linkdict">the link to be deleted.</param>
        public void DeleteLink(Dictionary<string, object> linkdict)
        {
            if (linkdict == null) return;

            void Finished()
            {
                try
                {
                    if (!linkdict.TryGetValue("xref", out var xo)) return;
                    int xref0 = Convert.ToInt32(xo);
                    if (xref0 == 0) return;
                    if (TryGetCachedLinkByXref(xref0, out var linkobj))
                        linkobj.Erase();
                    else if (linkdict.TryGetValue("id", out var idObj) && idObj is string sid && !string.IsNullOrEmpty(sid)
                             && TryGetCachedLinkByAnnotNm(sid, out var byNm))
                        byNm.Erase();
                }
                catch { }
            }

            mupdf.PdfPage page;
            try { page = NativePdfPage; }
            catch { Finished(); return; }

            if (page == null || page.m_internal == null)
            {
                Finished();
                return;
            }

            if (!linkdict.TryGetValue("xref", out var xrefObj))
            {
                Finished();
                return;
            }

            int xref = Convert.ToInt32(xrefObj);
            if (xref < 1)
            {
                Finished();
                return;
            }

            var annots = mupdf.mupdf.pdf_dict_get(page.obj(), mupdf.mupdf.pdf_new_name("Annots"));
            if (annots.m_internal == null)
            {
                Finished();
                return;
            }

            int len_ = mupdf.mupdf.pdf_array_len(annots);
            if (len_ == 0)
            {
                Finished();
                return;
            }

            int oxref = 0;
            int idx = 0;
            for (; idx < len_; idx++)
            {
                oxref = mupdf.mupdf.pdf_to_num(mupdf.mupdf.pdf_array_get(annots, idx));
                if (xref == oxref) break;
            }
            if (xref != oxref)
            {
                Finished();
                return;
            }

            mupdf.mupdf.pdf_array_delete(annots, idx);
            var pdf = RequireParent().NativePdfDocument;
            pdf.pdf_delete_object(xref);
            mupdf.mupdf.pdf_dict_put(page.obj(), mupdf.mupdf.pdf_new_name("Annots"), annots);
            Helpers.JM_refresh_links(pdf, page);
            Finished();
            SyncLinkWrapperCache();
        }

        /// <summary>PyMuPDF <c>Page.add_*_annot</c> shared path for text marker annotations.</summary>
        private Annot AddTextMarkerAnnot(mupdf.pdf_annot_type type, object quads, Point start, Point stop, IRect clip)
        {
            IReadOnlyList<object> items;
            if (quads == null)
            {
                var rects = Helpers.GetHighlightSelection(this, start, stop, clip);
                items = rects.Cast<object>().ToList();
            }
            else
            {
                items = Helpers.CheckMarkerArg(quads);
            }
            return AddTextMarker(type, items);
        }

        /// <summary>PyMuPDF <c>Page._add_text_marker</c>.</summary>
        private Annot AddTextMarker(mupdf.pdf_annot_type type, IReadOnlyList<object> items)
        {
            RequireParent();
            if (!RequireParent().IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);
            var annot = PageAddTextMarker(type, items);
            if (annot == null)
                return null;
            RegisterAnnotRef(annot);
            return annot;
        }

        /// <summary>PyMuPDF <c>Page__add_text_marker</c>.</summary>
        private Annot PageAddTextMarker(mupdf.pdf_annot_type type, IReadOnlyList<object> items)
        {
            var pdfPage = NativePdfPage;
            int rotation = Helpers.PageRotation(pdfPage);
            void RestoreRotation()
            {
                if (rotation != 0)
                    mupdf.mupdf.pdf_dict_put_int(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Rotate"), rotation);
            }
            try
            {
                if (rotation != 0)
                    mupdf.mupdf.pdf_dict_put_int(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Rotate"), 0);
                var annot = mupdf.mupdf.pdf_create_annot(pdfPage, type);
                foreach (var item in items)
                {
                    var q = Helpers.QuadFromPy(item);
                    mupdf.mupdf.pdf_add_annot_quad_point(annot, q);
                }
                mupdf.mupdf.pdf_update_annot(annot);
                Helpers.JM_add_annot_id(annot, "A");
                RestoreRotation();
                return new Annot(annot, this);
            }
            catch
            {
                RestoreRotation();
                return null;
            }
        }

        private void SetAnnotVertices(mupdf.PdfAnnot annot, Point[] points)
        {
            mupdf.mupdf.pdf_clear_annot_vertices(annot);
            foreach (var p in points)
                mupdf.mupdf.pdf_add_annot_vertex(annot, p.ToFzPoint());
        }

        // ─── Links ──────────────────────────────────────────────────────

        /// <summary>Append a link annotation dict to the page PDF without refreshing the native link list (for batch <see cref="SetLinks"/>).</summary>
        private void InsertLinkPdfOnly(Dictionary<string, object> linkDict)
        {
            if (linkDict == null) return;
            if (linkDict.TryGetValue("kind", out _))
            {
                if (!Helpers.TryBuildInsertLinkAnnotObjectString(this, linkDict, out var objSrc))
                    throw new ValueErrorException("link kind not supported");
                Helpers.AppendPdfAnnotFromObjectString(this, objSrc);
                return;
            }

            var pdfPage = NativePdfPage;
            var pdf = RequireParent().NativePdfDocument;
            var annotObj = mupdf.mupdf.pdf_new_dict(pdf, 4);
            mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("Type"), mupdf.mupdf.pdf_new_name("Annot"));
            mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("Subtype"), mupdf.mupdf.pdf_new_name("Link"));

            if (linkDict.TryGetValue("from", out var fromObj) && fromObj is Rect fromRect)
                mupdf.mupdf.pdf_dict_put_rect(annotObj, mupdf.mupdf.pdf_new_name("Rect"), fromRect.ToFzRect());

            if (linkDict.TryGetValue("uri", out var uriObj) && uriObj is string uri && !string.IsNullOrEmpty(uri))
            {
                var action = mupdf.mupdf.pdf_new_dict(pdf, 2);
                mupdf.mupdf.pdf_dict_put(action, mupdf.mupdf.pdf_new_name("S"), mupdf.mupdf.pdf_new_name("URI"));
                mupdf.mupdf.pdf_dict_puts(action, "URI", mupdf.mupdf.pdf_new_text_string(uri));
                mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("A"), action);
            }
            else if (linkDict.TryGetValue("page", out var pageObj))
            {
                int targetPage = Convert.ToInt32(pageObj);
                if (targetPage >= 0)
                {
                    var dest = mupdf.mupdf.pdf_new_array(pdf, 3);
                    var pageRef = mupdf.mupdf.pdf_lookup_page_obj(pdf, targetPage);
                    mupdf.mupdf.pdf_array_push(dest, pageRef);
                    mupdf.mupdf.pdf_array_push(dest, mupdf.mupdf.pdf_new_name("XYZ"));
                    mupdf.mupdf.pdf_array_push_int(dest, 0);
                    mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("Dest"), dest);
                }
            }

            var annots = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Annots"));
            if (annots.m_internal == null)
            {
                annots = mupdf.mupdf.pdf_new_array(pdf, 1);
                mupdf.mupdf.pdf_dict_put(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Annots"), annots);
            }
            var indObj = mupdf.mupdf.pdf_add_object(pdf, annotObj);
            mupdf.mupdf.pdf_array_push(annots, indObj);
        }
        /// <summary>
        /// PDF only: Insert a new link on this page. The parameter must be a dictionary of format as provided by get_links(), see link_dict_description.
        /// </summary>
        /// <param name="linkDict">Link dictionary in PyMuPDF get_links format.</param>
        /// <param name="mark">If true, mark the page dirty after link changes.</param>
        public Link InsertLink(Dictionary<string, object> linkDict, bool mark = true)
        {
            _ = mark;
            if (linkDict == null) return null;
            InsertLinkPdfOnly(linkDict);
            var pdfPage = NativePdfPage;
            var pdf = RequireParent().NativePdfDocument;
            Helpers.JM_refresh_links(pdf, pdfPage);
            SyncLinkWrapperCache();

            return LoadLinks();
        }
        /// <summary>
        /// PDF only: Insert a new link on this page. The parameter must be a dictionary of format as provided by get_links(), see link_dict_description.
        /// </summary>
        /// <param name="linkDict">Link dictionary in PyMuPDF get_links format.</param>
        /// <param name="mark">If true, mark the page dirty after link changes.</param>
        public void InsertLinkVoid(Dictionary<string, object> linkDict, bool mark = true)
        {
            _ = mark;
            if (linkDict == null) return;
            if (!Helpers.TryBuildInsertLinkAnnotObjectString(this, linkDict, out var objSrc))
                throw new ValueErrorException("link kind not supported");
            _addAnnot_FromString(new[] { objSrc });
        }
        /// <summary>
        /// PDF only: Insert a new link on this page. The parameter must be a dictionary of format as provided by get_links(), see link_dict_description.
        /// </summary>
        /// <param name="links">List of link dictionaries for bulk insert.</param>
        public void SetLinks(List<Dictionary<string, object>> links)
        {
            var pdfPage = NativePdfPage;
            var pdf = RequireParent().NativePdfDocument;
            var annotArray = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Annots"));

            if (annotArray.m_internal != null)
            {
                int n = mupdf.mupdf.pdf_array_len(annotArray);
                for (int i = n - 1; i >= 0; i--)
                {
                    var obj = mupdf.mupdf.pdf_array_get(annotArray, i);
                    var subtype = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("Subtype"));
                    if (mupdf.mupdf.pdf_name_eq(subtype, mupdf.mupdf.pdf_new_name("Link")) != 0)
                        mupdf.mupdf.pdf_array_delete(annotArray, i);
                }
            }

            if (links != null)
            {
                foreach (var linkDict in links)
                    InsertLinkPdfOnly(linkDict);
            }
            Helpers.JM_refresh_links(pdf, pdfPage);
            SyncLinkWrapperCache();
        }
        /// <summary>
        /// PDF only: Modify the specified link. The parameter must be a (modified) original item of get_links(), see link_dict_description. The reason for this is the dictionary's *"xref"* key, which identifies the PDF object to be changed.
        /// </summary>
        public void UpdateLink(Dictionary<string, object> lnk)
        {
            // CheckParent(page)
            RequireParent();
            // annot = utils.getLinkText(page, lnk)
            string annot = Utils.GetLinkText(this, lnk);
            // if annot == "":
            if (annot == "")
                // raise ValueError("link kind not supported")
                throw new ValueErrorException("link kind not supported");

            // page.parent.UpdateObject(lnk["xref"], annot, page=page)
            RequireParent().UpdateObject(Convert.ToInt32(lnk["xref"], CultureInfo.InvariantCulture), annot, this);
        }
        /// <summary>
        /// Retrieves all links of a page.
        /// </summary>
        public List<LinkInfo> GetLinks() =>
            GetLinksDict().Select(d => (LinkInfo)d).ToList();

        /// <summary>PyMuPDF <c>Page.get_links</c> as dictionaries.</summary>
        internal List<Dictionary<string, object>> GetLinksDict()
        {
            // Python Page.get_links: ln = page.first_link; while ln: ... ln = ln.next
            var result = new List<Dictionary<string, object>>();
            var link = FirstLink;
            while (link != null)
            {
                try
                {
                    result.Add(Helpers.GetLinkDict(link, RequireParent()));
                }
                catch
                {
                    // utils.getLinkDict catches rect/dest errors; stop this chain
                    break;
                }
                link = link.Next;
            }
            try
            {
                if (result.Count > 0 && RequireParent().IsPdf)
                {
                    var linkxrefs = Helpers.JM_get_annot_xref_list(NativePdfPage.obj())
                        .FindAll(t => t.type_ == (int)mupdf.pdf_annot_type.PDF_ANNOT_LINK);
                    if (linkxrefs.Count == result.Count)
                    {
                        for (int i = 0; i < linkxrefs.Count; i++)
                        {
                            result[i]["xref"] = linkxrefs[i].xref;
                            result[i]["id"] = linkxrefs[i].nm ?? "";
                        }
                    }
                }
            }
            catch { }
            SyncLinkWrapperCache();
            return result;
        }

        // ─── Rendering ──────────────────────────────────────────────────
        /// <summary>
        /// Create a pixmap from the page. This is probably the most often used method to create a Pixmap.
        /// </summary>
        /// <param name="matrix">default is Identity.</param>
        /// <param name="clip">restrict rendering to the intersection of this area with the page's rectangle.</param>
        /// <param name="alpha">whether to add an alpha channel. Always accept the default False if you do not really need transparency. This will save a lot of memory (25% in case of RGB ... and pixmaps are typically large!), and also processing time. Also note an important difference in how the image will be rendered: with True the pixmap's samples area will be pre-cleared with *0x00*. This results in transparent areas where the page is empty. With False the pixmap's samples will be pre-cleared with *0xff*. This results in white where the page has nothing to show.</param>
        /// <param name="annots">whether to also render annotations or to suppress them. You can create pixmaps for annotations separately.</param>
        /// <param name="dpi">desired resolution in x and y direction. If not `None`, the `"matrix"` parameter is ignored.</param>
        /// <param name="colorSpace">Colorspace name string (RGB, GRAY, CMYK).</param>
        /// <param name="clipRect">Clip rectangle (alias for clip).</param>
        /// <returns>Pixmap of the page. For fine-controlling the generated image, the by far most important parameter is matrix. E.g. you can increase or decrease the image resolution by using Matrix(xzoom, yzoom). If zoom &gt; 1, you will get a higher resolution: zoom=2 will double the number of pixels in that direction and thus generate a 2 times larger image. Non-positive values will flip horizontally, resp. vertically. Similarly, matrices also let you rotate or shear, and you can combine effects via e.g. matrix multiplication. See the Matrix section to learn more.</returns>
        /// <exception cref="ValueErrorException">Document or page is closed, or operation requires a PDF.</exception>
        public Pixmap GetPixmap(
            Matrix matrix = null,
            Colorspace cs = null,
            IRect clip = null,
            bool alpha = false,
            bool annots = true,
            float? dpi = null,
            string colorSpace = null,
            Rect clipRect = null)
        {
            if (clip == null && clipRect != null)
                clip = new IRect(clipRect);
            if (cs == null && !string.IsNullOrEmpty(colorSpace))
            {
                cs = colorSpace.ToUpperInvariant() switch
                {
                    "GRAY" or "GREY" => Colorspace.Gray,
                    "CMYK" => Colorspace.Cmyk,
                    _ => Colorspace.Rgb,
                };
            }
            // if dpi:
            if (dpi != null)
            {
                // zoom = dpi / 72
                float zoom = dpi.Value / 72.0f;
                // matrix = Matrix(zoom, zoom)
                matrix = new Matrix(zoom, zoom);
            }

            // dl = page.get_displaylist(annots=annots)
            using var dl = GetDisplayList(annots ? 1 : 0);
            // pix = dl.get_pixmap(matrix=matrix, colorspace=colorspace, alpha=alpha, clip=clip)
            var pix = dl.GetPixmap(matrix ?? Matrix.Identity, cs ?? Colorspace.Rgb, alpha, clip);
            // dl = None
            // if dpi:
            if (dpi != null)
                // pix.set_dpi(dpi, dpi)
                pix.SetDpi((int)dpi.Value, (int)dpi.Value);
            return pix;
        }
        /// <summary>
        /// See PyMuPDF Page.get_displaylist.
        /// </summary>
        /// <param name="annots">Whether to include annotations when building display list or pixmap.</param>
        public DisplayList GetDisplayList(int annots = 1)
        {
            lock (Utils.MuPDFLock)
            {
                mupdf.FzDisplayList dl;
                if (annots != 0)
                    dl = mupdf.mupdf.fz_new_display_list_from_page(NativePage);
                else
                    dl = mupdf.mupdf.fz_new_display_list_from_page_contents(NativePage);
                return new DisplayList(dl);
            }
        }
        /// <summary>
        /// Create an SVG image from the page. Only full page images are currently supported.
        /// </summary>
        /// <param name="matrix">a matrix, default is Identity.</param>
        /// <returns>a UTF-8 encoded string that contains the image. Because SVG has XML syntax it can be saved in a text file, the standard extension is `.svg`.</returns>
        public string GetSvgImage(Matrix matrix = null, int textAsPath = 1)
        {
            // CheckParent(self)
            RequireParent();
            // mediabox = mupdf.fz_bound_page(self.this)
            var mediabox = mupdf.mupdf.fz_bound_page(NativePage);
            // ctm = JM_matrix_from_py(matrix)
            var ctm = Helpers.MatrixToFz(matrix);
            // tbounds = mediabox
            var tbounds = mediabox;
            // text_option = mupdf.FZ_SVG_TEXT_AS_PATH if text_as_path == 1 else mupdf.FZ_SVG_TEXT_AS_TEXT
            int text_option = textAsPath == 1 ? mupdf.mupdf.FZ_SVG_TEXT_AS_PATH : mupdf.mupdf.FZ_SVG_TEXT_AS_TEXT;
            // tbounds = mupdf.fz_transform_rect(tbounds, ctm)
            tbounds = mupdf.mupdf.fz_transform_rect(tbounds, ctm);

            // res = mupdf.fz_new_buffer(1024)
            var res = mupdf.mupdf.fz_new_buffer(1024);
            // out = mupdf.FzOutput(res)
            var output = new mupdf.FzOutput(res);
            // dev = mupdf.fz_new_svg_device(
            var dev = mupdf.mupdf.fz_new_svg_device(
                output,
                tbounds.x1 - tbounds.x0,  // width
                tbounds.y1 - tbounds.y0,  // height
                text_option,
                1);
            // mupdf.fz_run_page(self.this, dev, ctm, mupdf.FzCookie())
            mupdf.mupdf.fz_run_page(NativePage, dev, ctm, new mupdf.FzCookie());
            // mupdf.fz_close_device(dev)
            mupdf.mupdf.fz_close_device(dev);
            // out.fz_close_output()
            output.fz_close_output();
            // text = JM_EscapeStrFromBuffer(res)
            string text = Helpers.JmEscapeStrFromBuffer(res);
            return text;
        }

        // ─── Text Extraction ────────────────────────────────────────────

        /// <summary>PyMuPDF <c>Page.get_textpage</c> — build textpage with rotation reset like Python.</summary>
        internal TextPage NewTextPageForGetText(IRect clip, int flags, Matrix matrix = null)
        {
            RequireParent();
            if (matrix == null)
                matrix = Matrix.Identity;
            int oldRotation = Rotation;
            if (oldRotation != 0)
                SetRotation(0);
            try
            {
                Rect clipRect = clip != null ? new Rect(clip) : null;
                return BuildTextPage(clipRect, flags, matrix);
            }
            finally
            {
                if (oldRotation != 0)
                    SetRotation(oldRotation);
            }
        }

        /// <summary>MuPDF.NET <c>Page._GetTextPage</c> / PyMuPDF <c>Page._get_textpage</c>.</summary>
        private mupdf.FzStextPage CreateStextPage(Rect clip, int flags, Matrix matrix)
        {
            // PyMuPDF extra.page_get_textpage() runs fz_run_page on the FzPage, not PdfPage.
            var page = NativePage;
            using var options = new mupdf.FzStextOptions(flags);
            mupdf.FzRect rect;
            if (clip == null || clip.IsInfinite || clip.IsEmpty)
                rect = mupdf.mupdf.fz_bound_page(page);
            else
                rect = clip.ToFzRect();
            using var ctm = matrix.ToFzMatrix();
            using var cookie = new mupdf.FzCookie();
            var stPage = new mupdf.FzStextPage(rect);
            var dev = stPage.fz_new_stext_device(options);
            try
            {
                mupdf.mupdf.fz_run_page(page, dev, ctm, cookie);
                mupdf.mupdf.fz_close_device(dev);
            }
            finally
            {
                dev?.Dispose();
            }
            return stPage;
        }

        private TextPage BuildTextPage(Rect clip, int flags, Matrix matrix)
            => new TextPage(CreateStextPage(clip, flags, matrix)) { Parent = this };
        /// <summary>
        /// Create a TextPage for the page.
        /// </summary>
        public TextPage GetTextPage(int flags = 0, IRect clip = null) =>
            NewTextPageForGetText(clip, flags);
        /// <summary>
        /// See PyMuPDF Page.extend_textpage.
        /// </summary>
        /// <param name="flags">Text extraction or search flags (see PyMuPDF text flags).</param>
        /// <param name="matrix">Transformation matrix for rendering or text extraction.</param>
        public void ExtendTextPage(TextPage tpage, int flags = 0, Matrix matrix = null)
        {
            if (tpage == null)
                throw new ArgumentNullException(nameof(tpage));
            int oldRot = Rotation;
            try
            {
                if (oldRot != 0)
                    SetRotation(0);
                using var opts = new mupdf.FzStextOptions(flags);
                var ctm = matrix ?? Matrix.Identity;
                var dev = new mupdf.FzDevice(tpage.NativeStextPage, opts);
                try
                {
                    mupdf.mupdf.fz_run_page(NativePage, dev, ctm.ToFzMatrix(), new mupdf.FzCookie());
                    mupdf.mupdf.fz_close_device(dev);
                }
                finally
                {
                    dev.Dispose();
                }
            }
            finally
            {
                if (oldRot != 0)
                    SetRotation(oldRot);
            }
        }
        /// <summary>
        /// Retrieves the content of a page in a variety of formats. Depending on the flags value, this may include text, images and several other object types. The method is a wrapper for multiple TextPage methods by choosing the output option `opt` as follows:.
        /// </summary>
        /// <param name="clip">Clip rectangle in unrotated page coordinates.</param>
        /// <param name="flags">indicator bits to control whether to include images or how text should be handled with respect to white spaces and ligatures. See TextPreserve for available indicators and text_extraction_flags for default settings.</param>
        /// <param name="textpage">Optional reused TextPage for repeated operations.</param>
        /// <param name="sort">sort the output by vertical, then horizontal coordinates. In many cases, this should suffice to generate a "natural" reading order. Has no effect on (X)HTML and XML. For options "blocks", "dict", "json", "rawdict", "rawjson", sorting happens by coordinates `(y1, x0)` of the respective block bbox. For options "words" and "text", the text lines are completely re-synthesized to follow the reading sequence and appearance in the document; which even establishes the original layout to some extent.</param>
        /// <param name="delimiters">use these characters as *additional* word separators with the "words" output option (ignored otherwise). By default, all white spaces (including non-breaking space `0xA0`) indicate start and end of a word. Now you can specify more characters causing this. For instance, the default will return `"john.doe@outlook.com"` as one word. If you specify `delimiters="@."` then the four words `"john"`, `"doe"`, `"outlook"`, `"com"` will be returned. Other possible uses include ignoring punctuation characters `delimiters=string.punctuation`. The "word" strings will not contain any delimiting character.</param>
        /// <param name="tolerance">Layout tolerance for text-with-layout extraction.</param>
        /// <returns>The page's content as a string, a list or a dictionary. Refer to the corresponding TextPage method for details.</returns>
        public dynamic GetText(
            string option = "text",
            IRect clip = null,
            int? flags = null,
            TextPage textpage = null,
            bool sort = false,
            object delimiters = null,
            float tolerance = 3)
        {
            return Utils.GetText(this, option, clip, flags, textpage, sort, delimiters, tolerance);
        }

        /// <summary>Return the text blocks on a page.</summary>
        /// <remarks>
        /// Port of PyMuPDF <c>Page.get_text_blocks</c> → <c>utils.get_text_blocks</c> (<c>src/utils.py</c>).
        /// Notes: Lines in a block are concatenated with line breaks.
        /// </remarks>
        /// <param name="flags">Control the amount of data parsed into the textpage.</param>
        /// <returns>
        /// A list of the blocks. Each item contains the containing rectangle coordinates,
        /// text lines, running block number and block type.
        /// </returns>
        public List<(float x0, float y0, float x1, float y1, string text, int blockNo, int blockType)> GetTextBlocks(
            IRect clip = null,
            int? flags = null,
            TextPage textpage = null,
            bool sort = false)
        {
            RequireParent(); // pymupdf.CheckParent(page)
            if (flags == null)
                flags = Constants.TextFlagsBlocks;
            TextPage tp = textpage;
            if (tp == null)
            {
                // tp = page.get_textpage(clip=clip, flags=flags)
                int oldRotation = Rotation;
                if (oldRotation != 0)
                    SetRotation(0);
                try
                {
                    Rect clipRect = clip != null ? new Rect(clip) : null;
                    var stp = _get_textpage(clipRect, flags.Value);
                    tp = new TextPage(stp) { Parent = this };
                }
                finally
                {
                    if (oldRotation != 0)
                        SetRotation(oldRotation);
                }
            }
            else if (tp.Parent != this)
            {
                throw new ValueErrorException("not a textpage of this page");
            }

            var blocks = tp.ExtractBlockTuples(); // blocks = tp.extractBLOCKS()
            if (textpage == null)
                tp.Dispose(); // if textpage is None: del tp
            if (sort)
                blocks.Sort((a, b) =>
                {
                    int c = a.y1.CompareTo(b.y1); // blocks.sort(key=lambda b: (b[3], b[0]))
                    return c != 0 ? c : a.x0.CompareTo(b.x0);
                });
            return blocks;
        }

        /// <summary>
        /// Return the text words as a list with the bbox for each word (PyMuPDF <c>Page.get_text_words</c> → <c>utils.get_text_words</c>).
        /// </summary>
        /// <param name="clip">Area on page to consider.</param>
        /// <param name="flags">Control the amount of data parsed into the textpage.</param>
        /// <param name="textpage">Either passed-in or null.</param>
        /// <param name="sort">Sort the words in reading sequence.</param>
        /// <param name="delimiters">Characters to use as word delimiters.</param>
        /// <param name="tolerance">Consider words to be part of the same line if top or bottom coordinate are not larger than this (only if <paramref name="sort"/> is true).</param>
        /// <returns>Word tuples (x0, y0, x1, y1, "word", bno, lno, wno).</returns>
        public List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> GetTextWords(
            IRect clip = null,
            int? flags = null,
            TextPage textpage = null,
            bool sort = false,
            string delimiters = null,
            float tolerance = 3)
        {
            List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> SortWords(
                List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> words)
            {
                // Sort words line-wise, forgiving small deviations.
                words.Sort((a, b) =>
                {
                    int c = a.y1.CompareTo(b.y1);  // words.sort(key=lambda w: (w[3], w[0]))
                    return c != 0 ? c : a.x0.CompareTo(b.x0);
                });
                var nwords = new List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)>();  // final word list
                var line = new List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> { words[0] };  // collects words roughly in same line
                var lrect = new Rect(words[0].x0, words[0].y0, words[0].x1, words[0].y1);  // start the line rectangle
                for (int i = 1; i < words.Count; i++)
                {
                    var w = words[i];
                    var wrect = new Rect(w.x0, w.y0, w.x1, w.y1);
                    if (
                        Math.Abs(wrect.Y0 - lrect.Y0) <= tolerance
                        || Math.Abs(wrect.Y1 - lrect.Y1) <= tolerance
                    )
                    {
                        line.Add(w);
                        lrect |= wrect;
                    }
                    else
                    {
                        line.Sort((a, b) => a.x0.CompareTo(b.x0));  // sort words in line l-t-r
                        nwords.AddRange(line);  // append to final words list
                        line = new List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> { w };  // start next line
                        lrect = wrect;  // start next line rect
                    }
                }

                line.Sort((a, b) => a.x0.CompareTo(b.x0));  // sort words in line l-t-r
                nwords.AddRange(line);  // append to final words list

                return nwords;
            }

            RequireParent();  // pymupdf.CheckParent(page)
            if (flags == null)
                flags = Constants.TextFlagsWords;  // flags = pymupdf.TEXTFLAGS_WORDS
            TextPage tp = textpage;
            if (tp == null)
            {
                // tp = page.get_textpage(clip=clip, flags=flags)
                int oldRotation = Rotation;
                if (oldRotation != 0)
                    SetRotation(0);
                try
                {
                    Rect clipRect = clip != null ? new Rect(clip) : null;
                    var stp = _get_textpage(clipRect, flags.Value);
                    tp = new TextPage(stp) { Parent = this };
                }
                finally
                {
                    if (oldRotation != 0)
                        SetRotation(oldRotation);
                }
            }
            else if (tp.Parent != this)
            {
                throw new ValueErrorException("not a textpage of this page");
            }

            var words = tp.ExtractWordTuples(delimiters);  // words = tp.extractWORDS(delimiters)

            // if textpage was given, we subselect the words in clip
            if (textpage != null && clip != null)
            {
                // sub-select words contained in clip
                var clipRect = new Rect(clip);
                var filtered = new List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)>();
                foreach (var w in words)
                {
                    var wrect = new Rect(w.x0, w.y0, w.x1, w.y1);
                    var inter = clipRect & wrect;  // clip & w[:4]
                    if (Math.Abs(inter.GetArea()) >= 0.5 * Math.Abs(wrect.GetArea()))
                        filtered.Add(w);
                }
                words = filtered;
            }

            if (textpage == null)
                tp.Dispose();  // if textpage is None: del tp
            if (words.Count > 0 && sort)
            {
                // advanced sort if any words found
                words = SortWords(words);
            }

            return words;
        }
        /// <summary>
        /// Search for *needle* on a page. Wrapper for TextPage.search.
        /// </summary>
        /// <param name="needle">Text to search for. May contain spaces. Upper / lower case is ignored, but only works for ASCII characters: For example, "COMPÉTENCES" will not be found if needle is "compétences"; "compÉtences" however will. Similar is true for German umlauts and the like.</param>
        /// <param name="clip">only search within this area.</param>
        /// <param name="maxHits">Maximum number of search hits to return.</param>
        /// <param name="flags">Control the data extracted by the underlying TextPage. By default, ligatures and white spaces are kept, and hyphenation is detected.</param>
        /// <param name="textpage">Optional reused TextPage for repeated operations.</param>
        public List<Quad> SearchFor(string needle, Quad clip = null, int maxHits = 16, int flags = 0, TextPage textpage = null)
        {
            if (textpage != null && textpage.Parent != this)
                throw new ValueErrorException("not a textpage of this page");
            IRect clipIr = clip != null ? clip.Rect.IRect : null;
            var tp = textpage ?? GetTextPage(flags, clipIr);
            try
            {
                return tp.Search(needle, maxHits);
            }
            finally
            {
                if (textpage == null) tp.Dispose();
            }
        }
        /// <summary>
        /// Search for *needle* on a page. Wrapper for TextPage.search.
        /// </summary>
        /// <param name="needle">Text to search for. May contain spaces. Upper / lower case is ignored, but only works for ASCII characters: For example, "COMPÉTENCES" will not be found if needle is "compétences"; "compÉtences" however will. Similar is true for German umlauts and the like.</param>
        /// <param name="clip">only search within this area.</param>
        /// <param name="maxHits">Maximum number of search hits to return.</param>
        /// <param name="flags">Control the data extracted by the underlying TextPage. By default, ligatures and white spaces are kept, and hyphenation is detected.</param>
        /// <param name="textpage">Optional reused TextPage for repeated operations.</param>
        public List<Rect> SearchForRects(string needle, Quad clip = null, int maxHits = 16, int flags = 0, TextPage textpage = null)
        {
            if (textpage != null && textpage.Parent != this)
                throw new ValueErrorException("not a textpage of this page");
            IRect clipIr = clip != null ? clip.Rect.IRect : null;
            var tp = textpage ?? GetTextPage(flags, clipIr);
            try
            {
                return tp.SearchRects(needle, maxHits);
            }
            finally
            {
                if (textpage == null) tp.Dispose();
            }
        }
        /// <summary>
        /// Retrieve the text contained in a rectangle.
        /// </summary>
        /// <param name="rect">Target rectangle in unrotated page coordinates.</param>
        /// <param name="textpage">Optional reused TextPage for repeated operations.</param>
        /// <returns>a string with interspersed linebreaks where necessary. It is based on dedicated code (changed in v1.19.0). A typical use is checking the result of Page.search_for:</returns>
        public string GetTextbox(Rect rect, TextPage textpage = null)
        {
            var tp = textpage ?? GetTextPage();
            try
            {
                if (textpage != null && tp.Parent != this)
                    throw new ArgumentException("not a textpage of this page");
                return tp.ExtractTextbox(rect);
            }
            finally
            {
                if (textpage == null) tp.Dispose();
            }
        }
        /// <summary>
        /// Retrieves the content of a page in a variety of formats. Depending on the flags value, this may include text, images and several other object types. The method is a wrapper for multiple TextPage methods by choosing the output option `opt` as follows:.
        /// </summary>
        /// <param name="p1">First point.</param>
        /// <param name="p2">Second point.</param>
        /// <param name="clip">Clip rectangle in unrotated page coordinates.</param>
        /// <param name="textpage">Optional reused TextPage for repeated operations.</param>
        /// <returns>The page's content as a string, a list or a dictionary. Refer to the corresponding TextPage method for details.</returns>
        public string GetTextSelection(Point p1, Point p2, IRect clip = null, TextPage textpage = null)
        {
            var tp = textpage ?? GetTextPage(flags: mupdf.mupdf.FZ_STEXT_DEHYPHENATE, clip: clip);
            try
            {
                if (textpage != null && tp.Parent != this)
                    throw new ArgumentException("not a textpage of this page");
                return tp.ExtractSelection(p1, p2);
            }
            finally
            {
                if (textpage == null) tp.Dispose();
            }
        }
        /// <summary>
        /// This method returns a TextPage for the page that includes OCRed text. MuPDF will invoke Tesseract-OCR if this method is used.
        /// </summary>
        /// <param name="flags">indicator bits controlling the content available for subsequent test extractions and searches; see the parameter of Page.get_text.</param>
        /// <param name="language">the expected language(s). Use "+"-separated values if multiple languages are expected, "eng+spa" for English and Spanish.</param>
        /// <param name="dpi">the desired resolution in dots per inch. Influences recognition quality (and execution time).</param>
        /// <param name="full">whether to OCR the full page, or only page areas that contain no legible text.</param>
        /// <param name="tessdata">The name of Tesseract's language support folder `tessdata`. If omitted, the name is determined using function get_tessdata.</param>
        public TextPage GetTextPageOcr(
            int flags = 0,
            string language = "eng",
            int dpi = 72,
            bool full = false,
            string tessdata = null,
            ImageFilterPipeline imageFilters = null)
        {
            // Ensure unknown-unicode replacement is not suppressed, matching Python.
            flags = flags & ~mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE & ~mupdf.mupdf.FZ_STEXT_USE_GID_FOR_UNKNOWN_UNICODE;
            string tessdataDir = Helpers.GetTessdata(tessdata);

            if (full)
            {
                Pixmap pixFull = GetPixmap(dpi: dpi, alpha: false, annots: true);
                Pixmap ocrPixmap = pixFull;
                Pixmap filteredPixmap = null;
                try
                {
                    if (imageFilters != null)
                    {
                        filteredPixmap = Pixmap.ApplyImageFilters(pixFull, imageFilters);
                        if (filteredPixmap != null)
                            ocrPixmap = filteredPixmap;
                    }

                var pdfocrOptions = new mupdf.FzPdfocrOptions();
                ConfigurePdfOcrOptions(pdfocrOptions, language, tessdataDir);

                var ocrBuf = mupdf.mupdf.fz_new_buffer(1024);
                var ocrOut = new mupdf.FzOutput(ocrBuf);
                ocrOut.fz_write_pixmap_as_pdfocr(ocrPixmap.NativePixmap, pdfocrOptions);
                mupdf.mupdf.fz_close_output(ocrOut);
                byte[] pdfBytes = ocrBuf.fz_buffer_extract();

                // Must stay alive as long as returned TextPage is in use.
                var ocrDocFull = new Document(pdfBytes, "pdf");
                var ocrPageFull = ocrDocFull.LoadPage(0);
                float unzoom = Rect.Width / ocrPageFull.Rect.Width;
                Matrix ctm = new Matrix(unzoom, unzoom) * DerotationMatrix;
                var ocrTp = ocrPageFull.NewTextPageForGetText(null, flags, ctm);
                ocrTp.Parent = this;
                return ocrTp;
                }
                finally
                {
                    if (filteredPixmap != null && !ReferenceEquals(filteredPixmap, pixFull))
                        filteredPixmap.Dispose();
                    else
                        pixFull?.Dispose();
                }
            }

            // Partial mode follows Python get_textpage_ocr() flow:
            // 1) make temporary one-page PDF
            // 2) detect/redact spans, then OCR remainder
            var tempPdf = new Document();
            tempPdf.InsertPdf(RequireParent(), fromPage: Number, toPage: Number);
            var tempPage = tempPdf.LoadPage(0);
            tempPage.RemoveRotation();

            var tp = tempPage.GetTextPage(flags);
            var blocksObj = tp.ExtractDict()["blocks"] as List<Dictionary<string, object>>;
            var blocks = blocksObj ?? new List<Dictionary<string, object>>();

            List<Rect> CollectSpanBboxes(List<Dictionary<string, object>> blockList, bool unreadable)
            {
                var list = new List<Rect>();
                foreach (var b in blockList)
                {
                    if (!b.ContainsKey("type") || Convert.ToInt32(b["type"]) != 0 || !b.ContainsKey("lines"))
                        continue;
                    var lines = b["lines"] as List<Dictionary<string, object>>;
                    if (lines == null) continue;
                    foreach (var l in lines)
                    {
                        if (!l.ContainsKey("spans")) continue;
                        var spans = l["spans"] as List<Dictionary<string, object>>;
                        if (spans == null) continue;
                        foreach (var s in spans)
                        {
                            string text = s.ContainsKey("text") ? Convert.ToString(s["text"]) : string.Empty;
                            bool hasFffd = !string.IsNullOrEmpty(text) && text.IndexOf('\uFFFD') >= 0;
                            if (unreadable ? !hasFffd : hasFffd)
                                continue;
                            if (!s.ContainsKey("bbox")) continue;
                            var bb = s["bbox"] as float[];
                            if (bb == null || bb.Length < 4) continue;
                            list.Add(new Rect(bb[0], bb[1], bb[2], bb[3]));
                        }
                    }
                }
                return list;
            }

            var fffdSpans = CollectSpanBboxes(blocks, unreadable: true);
            if (fffdSpans.Count > 0)
            {
                foreach (var bbox in fffdSpans)
                {
                    var q = new Quad(bbox.TopLeft, bbox.TopRight, bbox.BottomLeft, bbox.BottomRight);
                    tempPage.AddRedactAnnot(q);
                }
                tempPage.ApplyRedactions(
                    images: mupdf.mupdf.PDF_REDACT_IMAGE_NONE,
                    graphics: mupdf.mupdf.PDF_REDACT_LINE_ART_NONE,
                    text: mupdf.mupdf.PDF_REDACT_TEXT_REMOVE);

                tp = tempPage.GetTextPage(flags);
                blocksObj = tp.ExtractDict()["blocks"] as List<Dictionary<string, object>>;
                blocks = blocksObj ?? new List<Dictionary<string, object>>();

                tempPdf.InsertPdf(RequireParent(), fromPage: Number, toPage: Number);
                tempPage = tempPdf.LoadPage(tempPdf.PageCount - 1);
                tempPage.RemoveRotation();
            }

            var spanBboxes = CollectSpanBboxes(blocks, unreadable: false);
            foreach (var bbox in spanBboxes)
            {
                var q = new Quad(bbox.TopLeft, bbox.TopRight, bbox.BottomLeft, bbox.BottomRight);
                tempPage.AddRedactAnnot(q);
            }
            tempPage.ApplyRedactions(
                images: mupdf.mupdf.PDF_REDACT_IMAGE_NONE,
                graphics: mupdf.mupdf.PDF_REDACT_LINE_ART_NONE,
                text: mupdf.mupdf.PDF_REDACT_TEXT_REMOVE);

            Pixmap pixPartial = tempPage.GetPixmap(dpi: dpi, alpha: false, annots: true);
            Pixmap ocrPartialPixmap = pixPartial;
            Pixmap filteredPartialPixmap = null;
            try
            {
                if (imageFilters != null)
                {
                    filteredPartialPixmap = Pixmap.ApplyImageFilters(pixPartial, imageFilters);
                    if (filteredPartialPixmap != null)
                        ocrPartialPixmap = filteredPartialPixmap;
                }

            var pdfocrOpts = new mupdf.FzPdfocrOptions();
            ConfigurePdfOcrOptions(pdfocrOpts, language, tessdataDir);
            var partialBuf = mupdf.mupdf.fz_new_buffer(1024);
            var partialOut = new mupdf.FzOutput(partialBuf);
            partialOut.fz_write_pixmap_as_pdfocr(ocrPartialPixmap.NativePixmap, pdfocrOpts);
            mupdf.mupdf.fz_close_output(partialOut);
            byte[] partialPdfBytes = partialBuf.fz_buffer_extract();

            using var ocrDocPartial = new Document(partialPdfBytes, "pdf");
            var ocrPagePartial = ocrDocPartial.LoadPage(0);

            // Extend original textpage with OCR page content.
            var mergedTp = tp;
            using var stOpts = new mupdf.FzStextOptions(mupdf.mupdf.FZ_STEXT_ACCURATE_BBOXES);
            var stDevice = mergedTp.NativeStextPage.fz_new_stext_device(stOpts);
            try
            {
                ocrPagePartial.NativePage.fz_run_page(stDevice, Matrix.Identity.ToFzMatrix(), new mupdf.FzCookie());
                mupdf.mupdf.fz_close_device(stDevice);
            }
            finally
            {
                stDevice?.Dispose();
            }
            mergedTp.Parent = this;
            return mergedTp;
            }
            finally
            {
                if (filteredPartialPixmap != null && !ReferenceEquals(filteredPartialPixmap, pixPartial))
                    filteredPartialPixmap.Dispose();
                else
                    pixPartial?.Dispose();
            }
        }

        static void ConfigurePdfOcrOptions(mupdf.FzPdfocrOptions options, string language, string tessdataDir)
        {
            options.compress = 0;
            if (!string.IsNullOrEmpty(language))
                options.language = language;
            if (!string.IsNullOrEmpty(tessdataDir))
                options.datadir = tessdataDir;
        }

        // ─── Text Insertion ─────────────────────────────────────────────
        /// <summary>
        /// PDF only: Insert text lines starting at point_like point. See Shape.insert_text.
        /// </summary>
        /// <param name="point">Location for point-based annotations.</param>
        /// <param name="text">Redaction mode for text, or annotation text, or HTML source.</param>
        /// <param name="fontSize">Font size in points.</param>
        /// <param name="fontName">Font name.</param>
        /// <param name="rotate">Image or page rotation in degrees.</param>
        /// <param name="fontFile">Path to a font file to embed.</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        public int InsertText(
            Point point,
            string text,
            float fontSize = 11,
            string fontName = "helv",
            float[] color = null,
            float rotate = 0,
            int renderMode = 0,
            float borderWidth = 0.05f,
            float? lineHeight = null,
            string fontFile = null,
            int setSimple = 0,
            int encoding = 0,
            float[] fill = null,
            float? miterLimit = 1,
            Point morphFix = null,
            Matrix morphMat = null,
            Morph morph = null,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            if (morph != null)
            {
                morphFix = morph.P;
                morphMat = morph.M;
            }
            // img = page.new_shape()
            using var img = NewShape();
            // rc = img.insert_text(...)
            int rc = img.InsertText(
                point,
                text,
                fontsize: fontSize,
                lineheight: lineHeight,
                fontname: fontName,
                fontfile: fontFile,
                setSimple: setSimple,
                encoding: encoding,
                color: color,
                fill: fill,
                renderMode: renderMode,
                borderWidth: borderWidth,
                miterLimit: miterLimit,
                rotate: (int)rotate,
                morphFix: morphFix,
                morphMat: morphMat,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc);
            // if rc >= 0:
            //     img.commit(overlay)
            if (rc >= 0)
                img.Commit(overlay);
            // return rc
            return rc;
        }
        /// <summary>
        /// PDF only: Insert text into the specified rect_like *rect*.
        /// </summary>
        /// <param name="rect">Target rectangle in unrotated page coordinates.</param>
        /// <param name="fontFile">Path to a font file to embed.</param>
        /// <param name="fontName">Font name.</param>
        /// <param name="fontSize">Font size in points.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="rotate">Image or page rotation in degrees.</param>
        public InsertTextboxResult InsertTextbox(
            Rect rect,
            string buffer,
            int align = 0,
            float borderWidth = 0.05f,
            float[] color = null,
            int encoding = 0,
            float expandTabs = 1,
            float fillOpacity = 1,
            float[] fill = null,
            string fontFile = null,
            string fontName = "helv",
            float fontSize = 11,
            float? lineHeight = null,
            float? miterLimit = 1,
            Point morphFix = null,
            Matrix morphMat = null,
            int oc = 0,
            bool overlay = true,
            int renderMode = 0,
            int rotate = 0,
            int setSimple = 0,
            float strokeOpacity = 1)
        {
            // """Insert text into a given rectangle.
            //
            // Notes:
            //     Creates a Shape object, uses its same-named method and commits it.
            // Parameters:
            //     rect: (rect-like) area to use for text.
            //     buffer: text to be inserted
            //     fontname: a Base-14 font, font name or '/name'
            //     fontfile: name of a font file
            //     fontsize: font size
            //     lineheight: overwrite the font property
            //     color: RGB color triple
            //     expandtabs: handles tabulators with string function
            //     align: left, center, right, justified
            //     rotate: 0, 90, 180, or 270 degrees
            //     morph: morph box with a matrix and a fixpoint
            //     overlay: put text in foreground or background
            // Returns:
            //     unused or deficit rectangle area (float)
            // """
            // img = page.new_shape()
            using var img = NewShape();
            // rc = img.insert_textbox(
            //     rect,
            //     buffer,
            //     fontsize=fontsize,
            //     lineheight=lineheight,
            //     fontname=fontname,
            //     fontfile=fontfile,
            //     set_simple=set_simple,
            //     encoding=encoding,
            //     color=color,
            //     fill=fill,
            //     expandtabs=expandtabs,
            //     render_mode=render_mode,
            //     miter_limit=miter_limit,
            //     border_width=border_width,
            //     align=align,
            //     rotate=rotate,
            //     morph=morph,
            //     stroke_opacity=stroke_opacity,
            //     fill_opacity=fill_opacity,
            //     oc=oc,
            // )
            float rc = img.InsertTextbox(
                rect,
                buffer,
                align: align,
                borderWidth: borderWidth,
                color: color,
                encoding: encoding,
                expandTabs: expandTabs,
                fillOpacity: fillOpacity,
                fill: fill,
                fontfile: fontFile,
                fontname: fontName,
                fontsize: fontSize,
                lineheight: lineHeight,
                miterLimit: miterLimit,
                morphFix: morphFix,
                morphMat: morphMat,
                oc: oc,
                renderMode: renderMode,
                rotate: rotate,
                setSimple: setSimple,
                strokeOpacity: strokeOpacity);
            // if rc >= 0:
            //     img.commit(overlay)
            if (rc >= 0)
                img.Commit(overlay);
            // return rc  (Python returns the float from Shape.insert_textbox)
            return new InsertTextboxResult(rc, new List<string>());
        }

        /// <summary>
        /// Insert text with optional HTML tags and stylings into a rectangle.
        /// </summary>
        /// <param name="rect">Rectangle into which the text should be placed.</param>
        /// <param name="text">Text with optional HTML tags and styling.</param>
        /// <param name="css">CSS styling commands.</param>
        /// <param name="scaleLow">
        /// Force-fit lower bound in [0, 1].
        /// 1 means no scaling is allowed. 0 means arbitrary downscaling is allowed.
        /// </param>
        /// <param name="archive">Archive for fonts/images referenced by HTML/CSS.</param>
        /// <param name="rotate">Rotation angle; must be a multiple of 90.</param>
        /// <param name="oc">Optional content group/xobject id.</param>
        /// <param name="opacity">Requested opacity in [0, 1].</param>
        /// <param name="overlay">Put content in foreground if true.</param>
        /// <param name="scaleWordWidth">Internal compatibility flag; currently accepted but not specialized.</param>
        /// <param name="verbose">Internal compatibility flag; currently accepted but not specialized.</param>
        /// <returns>
        /// A tuple (spareHeight, scale). spareHeight is remaining height below inserted content, or -1 on fit failure.
        /// scale is the chosen scale factor (0 &lt;= scale &lt;= 1).
        /// </returns>
        public (float spareHeight, float scale) InsertHtmlbox(
            Rect rect,
            string text,
            string css = null,
            float scaleLow = 0,
            Archive archive = null,
            int rotate = 0,
            int oc = 0,
            float opacity = 1,
            bool overlay = true,
            bool scaleWordWidth = true,
            bool verbose = false)
        {
            string mycss = "body {margin:1px;}" + (css ?? "");
            using var story = new Story(text ?? "", mycss, archive: archive);
            return InsertHtmlbox(
                rect,
                story,
                scaleLow: scaleLow,
                rotate: rotate,
                oc: oc,
                opacity: opacity,
                overlay: overlay,
                scaleWordWidth: scaleWordWidth,
                verbose: verbose);
        }

        /// <summary>
        /// Insert a pre-built <see cref="Story"/> into a rectangle (PyMuPDF <c>insert_htmlbox</c> with Story).
        /// </summary>
        public (float spareHeight, float scale) InsertHtmlbox(
            Rect rect,
            Story story,
            float scaleLow = 0,
            int rotate = 0,
            int oc = 0,
            float opacity = 1,
            bool overlay = true,
            bool scaleWordWidth = true,
            bool verbose = false)
        {
            if (story == null)
                throw new ValueErrorException("'text' must be a string or a Story");
            if (rotate % 90 != 0)
                throw new ValueErrorException("bad rotation angle");
            while (rotate < 0) rotate += 360;
            rotate %= 360;
            if (scaleLow < 0 || scaleLow > 1)
                throw new ValueErrorException("'scale_low' must be in [0, 1]");

            rect = new Rect(rect);
            Rect tempRect;
            if (rotate == 90 || rotate == 270)
                tempRect = new Rect(0, 0, rect.Height, rect.Width);
            else
                tempRect = new Rect(0, 0, rect.Width, rect.Height);

            float? rectScaleMax = scaleLow == 0 ? null : (float?)(1.0 / scaleLow);
            int flags = scaleWordWidth ? mupdf.mupdf.FZ_PLACE_STORY_FLAG_NO_OVERFLOW : 0;
            var fit = story.fit_scale(
                tempRect,
                scale_min: 1,
                scale_max: rectScaleMax,
                flags: flags,
                verbose: verbose);

            if (fit.big_enough != true)
            {
                float failScale = 1.0f / fit.parameter!.Value;
                return (-1, failScale);
            }

            fit.filled = new Rect(fit.filled);
            float scale = 1.0f / fit.parameter!.Value;
            float spareHeight = Math.Max((fit.rect.Y1 - fit.filled.Y1) * scale, 0);

            Story.StoryRectFn rectFunction = (_, __) => (fit.rect, fit.rect, null);

            using var doc = story.write_with_links(rectFunction);

            if (opacity >= 0 && opacity < 1)
            {
                Page tpage = doc[0];
                string alp0 = tpage._set_opacity(CA: opacity, ca: opacity);
                string s = $"/{alp0} gs\n";
                Tools.InsertContents(tpage, Encoding.UTF8.GetBytes(s), overlay: false);
            }

            ShowPdfPage(rect, doc, pno: 0, keepProportion: true, overlay: overlay, oc: oc, rotate: rotate);

            Point mp1 = (fit.rect.TopLeft + fit.rect.BottomRight) * 0.5f * scale;
            Point mp2 = (rect.TopLeft + rect.BottomRight) * 0.5f;
            var mat =
                new Matrix(scale, 0, 0, scale, -mp1.X, -mp1.Y)
                * new Matrix(-rotate)
                * new Matrix(1, 0, 0, 1, mp2.X, mp2.Y);

            foreach (var link in doc[0].GetLinksDict())
            {
                if (link.TryGetValue("from", out var fromObj) && fromObj is Rect fromRect)
                {
                    link["from"] = fromRect * mat;
                    this.insert_link(link);
                }
            }

            return (spareHeight, scale);
        }
        /// <summary>
        /// PDF only: Display a page of another PDF. This is similar to Page.insert_image but the source page will appear like a copy of itself and will not be rasterized. This is a multi-purpose method. For example, you can use it to:.
        /// </summary>
        /// <param name="rect">where to place the image on current page. Must be finite and its intersection with the page must not be empty.</param>
        /// <param name="pno">page number (0-based, in `-∞ &lt; pno &lt; docsrc.page_count`) to be shown.</param>
        /// <param name="overlay">put image in foreground (default) or background.</param>
        /// <param name="oc">(xref) make visibility dependent on this OCG / OCMD (which must be defined in the target PDF) .</param>
        /// <param name="rotate">show the source rectangle rotated by some angle. Any angle is supported (changed in v1.14.11).</param>
        /// <param name="clip">choose which part of the source page to show. Default is the full page, else must be finite and its intersection with the source page must not be empty.</param>
        /// <exception cref="ValueErrorException">Document or page is closed, or operation requires a PDF.</exception>
        public int ShowPdfPage(
            Rect rect,
            Document docsrc,
            int pno = 0,
            bool keepProportion = true,
            bool overlay = true,
            int oc = 0,
            int rotate = 0,
            Rect clip = null)
        {
            if (docsrc == null)
                throw new ArgumentNullException(nameof(docsrc));
            if (!RequireParent().IsPdf || !docsrc.IsPdf)
                throw new ValueErrorException("is no PDF");
            if (rect.IsEmpty || rect.IsInfinite)
                throw new ValueErrorException("rect must be finite and not empty");
            if (ReferenceEquals(Parent, docsrc))
                throw new ValueErrorException("source document must not equal target");

            int srcPageCount = docsrc.PageCount;
            while (pno < 0) pno += srcPageCount;
            if (pno < 0 || pno >= srcPageCount)
                throw new ValueErrorException("bad page number");

            using var srcPage = docsrc.LoadPage(pno);
            var srcRect = clip == null ? new Rect(srcPage.Rect) : (new Rect(srcPage.Rect) & clip);
            if (srcRect.IsEmpty || srcRect.IsInfinite)
                throw new ValueErrorException("clip must be finite and not empty");

            // Python parity note: map target/source rectangles into PDF coordinates.
            var targetInv = TransformationMatrix.Inverted() ?? Matrix.Identity;
            var sourceInv = srcPage.TransformationMatrix.Inverted() ?? Matrix.Identity;
            var tarRectPdf = new Rect(rect).Transform(targetInv);
            var srcRectPdf = new Rect(srcRect).Transform(sourceInv);

            var matrix = CalcShowPdfPageMatrix(srcRectPdf, tarRectPdf, keepProportion, rotate);
            string imgName = MakeShowPdfResourceName();

            var pdfOut = RequireParent().NativePdfDocument;
            var targetPageObj = NativePdfPage.obj();

            int srcGraftId = docsrc.GraftId;
            if (!RequireParent().Graftmaps.TryGetValue(srcGraftId, out var gmap) || gmap == null)
            {
                gmap = new Graftmap(RequireParent());
                RequireParent().Graftmaps[srcGraftId] = gmap;
            }

            // take note of generated xref for automatic reuse
            // pno_id = (isrc, pno)  # id of docsrc[pno]
            var pnoId = (srcGraftId, pno);
            var doc = RequireParent();
            // xref = doc.ShownPages.get(pno_id, 0)
            doc.ShownPages.TryGetValue(pnoId, out int xref);

            if (overlay)
                WrapContents();
            // xref = page._show_pdf_page(..., xref=xref, ...)
            int rcXref = ShowPdfPageInternal(
                srcPage.NativePdfPage,
                overlay: overlay,
                matrix: matrix,
                xref: xref,
                oc: oc,
                clip: srcRectPdf,
                graftmap: gmap.NativeGraftMap,
                imgName: imgName,
                pdfOut: pdfOut,
                targetPageObj: targetPageObj);
            // doc.ShownPages[pno_id] = xref
            doc.ShownPages[pnoId] = rcXref;
            return rcXref;
        }

        private static (bool success, float scale, float spareHeight) FindStoryScale(Story story, float width, float height, float scaleLow)
        {
            float low = (float)Math.Max(scaleLow, 1e-3f);
            float high = 1.0f;
            bool haveFit = false;
            float bestScale = low;
            float bestSpare = -1;

            for (int i = 0; i < 22; i++)
            {
                float mid = (low + high) * 0.5f;
                var testRect = new Rect(0, 0, width / mid, height / mid);
                story.Reset();
                var (more, filled) = story.Place(testRect);
                if (!more)
                {
                    haveFit = true;
                    bestScale = mid;
                    bestSpare = Math.Max((testRect.Height - filled.Height) * mid, 0);
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            if (haveFit) return (true, bestScale, bestSpare);
            return (false, low, -1);
        }

        /// <summary>
        /// Internal port of PyMuPDF <c>Page._show_pdf_page()</c>.
        /// </summary>
        private int ShowPdfPageInternal(
            mupdf.PdfPage srcPdfPage,
            bool overlay,
            Matrix matrix,
            int xref,
            int oc,
            Rect clip,
            mupdf.PdfGraftMap graftmap,
            string imgName,
            mupdf.PdfDocument pdfOut,
            mupdf.PdfObj targetPageObj)
        {
            int rcXref = xref;

            // -------------------------------------------------------------
            // convert the source page to a Form XObject
            // -------------------------------------------------------------
            var xobj1 = Helpers.JM_xobject_from_page(pdfOut, srcPdfPage, xref, graftmap);
            if (rcXref == 0)
                rcXref = mupdf.mupdf.pdf_to_num(xobj1);

            // -------------------------------------------------------------
            // create referencing XObject (controls display on target page)
            // -------------------------------------------------------------
            var subres1 = mupdf.mupdf.pdf_new_dict(pdfOut, 5);
            mupdf.mupdf.pdf_dict_puts(subres1, "fullpage", xobj1);
            var subres = mupdf.mupdf.pdf_new_dict(pdfOut, 5);
            mupdf.mupdf.pdf_dict_put(subres, mupdf.mupdf.pdf_new_name("XObject"), subres1);

            var res = mupdf.mupdf.fz_new_buffer(20);
            res.fz_append_string("/fullpage Do");

            var xobj2 = mupdf.mupdf.pdf_new_xobject(pdfOut, clip.ToFzRect(), matrix.ToFzMatrix(), subres, res);
            if (oc > 0)
                Helpers.JM_add_oc_object(pdfOut, xobj2.pdf_resolve_indirect(), oc);

            // -------------------------------------------------------------
            // update target page with xobj2
            // -------------------------------------------------------------
            var resources = mupdf.mupdf.pdf_dict_get_inheritable(targetPageObj, mupdf.mupdf.pdf_new_name("Resources"));
            if (resources.m_internal == null)
            {
                resources = mupdf.mupdf.pdf_new_dict(pdfOut, 5);
                mupdf.mupdf.pdf_dict_put(targetPageObj, mupdf.mupdf.pdf_new_name("Resources"), resources);
            }

            var xobjects = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("XObject"));
            if (xobjects.m_internal == null)
            {
                xobjects = mupdf.mupdf.pdf_new_dict(pdfOut, 5);
                mupdf.mupdf.pdf_dict_put(resources, mupdf.mupdf.pdf_new_name("XObject"), xobjects);
            }
            mupdf.mupdf.pdf_dict_puts(xobjects, imgName, xobj2);

            var doBuf = mupdf.mupdf.fz_new_buffer(64);
            doBuf.fz_append_string(" q /");
            doBuf.fz_append_string(imgName);
            doBuf.fz_append_string(" Do Q ");
            Helpers.JM_insert_contents(pdfOut, targetPageObj, doBuf, overlay);

            return rcXref;
        }

        private static void InsertContentsBuffer(mupdf.PdfDocument pdf, mupdf.PdfObj pageObj, mupdf.FzBuffer buf, bool overlay)
        {
            var contents = mupdf.mupdf.pdf_dict_get(pageObj, mupdf.mupdf.pdf_new_name("Contents"));
            var newStream = mupdf.mupdf.pdf_add_stream(pdf, buf, new mupdf.PdfObj(), 0);
            if (contents.m_internal == null)
            {
                mupdf.mupdf.pdf_dict_put(pageObj, mupdf.mupdf.pdf_new_name("Contents"), newStream);
                return;
            }

            if (mupdf.mupdf.pdf_is_array(contents) != 0)
            {
                if (overlay)
                    mupdf.mupdf.pdf_array_push(contents, newStream);
                else
                    mupdf.mupdf.pdf_array_insert(contents, newStream, 0);
                return;
            }

            var arr = mupdf.mupdf.pdf_new_array(pdf, 2);
            if (overlay)
            {
                mupdf.mupdf.pdf_array_push(arr, contents);
                mupdf.mupdf.pdf_array_push(arr, newStream);
            }
            else
            {
                mupdf.mupdf.pdf_array_push(arr, newStream);
                mupdf.mupdf.pdf_array_push(arr, contents);
            }
            mupdf.mupdf.pdf_dict_put(pageObj, mupdf.mupdf.pdf_new_name("Contents"), arr);
        }

        private static Matrix CalcShowPdfPageMatrix(Rect srcRect, Rect tarRect, bool keepProportion, int rotate)
        {
            float smpX = (srcRect.X0 + srcRect.X1) * 0.5f;
            float smpY = (srcRect.Y0 + srcRect.Y1) * 0.5f;
            float tmpX = (tarRect.X0 + tarRect.X1) * 0.5f;
            float tmpY = (tarRect.Y0 + tarRect.Y1) * 0.5f;

            // m moves to (0,0), then rotates.
            Matrix m = new Matrix(1, 0, 0, 1, -smpX, -smpY) * Matrix.Rotation(rotate);
            Rect srcRot = new Rect(srcRect).Transform(m);

            float fw = tarRect.Width / Math.Max(srcRot.Width, Constants.Epsilon);
            float fh = tarRect.Height / Math.Max(srcRot.Height, Constants.Epsilon);
            if (keepProportion)
            {
                float f = Math.Min(fw, fh);
                fw = f;
                fh = f;
            }

            m = m * new Matrix(fw, fh);
            m = m * new Matrix(1, 0, 0, 1, tmpX, tmpY);
            return m;
        }

        private string MakeShowPdfResourceName()
        {
            // PyMuPDF show_pdf_page: list of existing /Form /XObjects, images, fonts
            var doc = RequireParent();
            var ilst = new HashSet<string>();
            foreach (var item in doc._getPageInfo_py(Number, 3))
                ilst.Add((string)item[1]);
            foreach (var item in doc.get_page_images_py(Number, full: false))
                ilst.Add((string)item[7]);
            foreach (var item in doc.get_page_fonts_py(Number, full: false))
                ilst.Add((string)item[4]);

            string n = "fzFrm";
            int i = 0;
            string imgname = n + "0";
            while (ilst.Contains(imgname))
            {
                i++;
                imgname = n + i.ToString(CultureInfo.InvariantCulture);
            }
            return imgname;
        }

        // ─── Fonts and Images ───────────────────────────────────────────

        /// <summary>
        /// List of fonts defined in the page object.
        /// </summary>
        public List<(int xref, string ext, string type, string baseName, string name, string encoding, int? referencer)> GetFonts(bool full = false)
        {
            return RequireParent().GetPageFonts(Number, full);
        }

        /// <summary>PyMuPDF-style image rows (tuples) for internal use.</summary>
        internal List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)> GetImageRows(bool full = false)
            => RequireParent().GetPageImageRows(Number, full);

        private static Entry ImageRowToEntry((int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter) i) =>
            new Entry
            {
                Xref = i.xref,
                Smask = int.TryParse(i.smask, out var sm) ? sm : 0,
                Width = i.width,
                Height = i.height,
                Bpc = i.bpc,
                CsName = i.colorspace,
                AltCsName = i.altCs,
                Name = i.name,
                Filter = i.filter,
            };
        /// <summary>
        /// PDF only: Return a list of images referenced by the page. Wrapper for Document.get_page_images.
        /// </summary>
        public List<Entry> GetImages(bool full = false) =>
            GetImageRows(full).Select(ImageRowToEntry).ToList();
        /// <summary>
        /// PDF only: Put an image inside the given rectangle. The image may already.
        /// </summary>
        /// <param name="rect">where to put the image. Must be finite and not empty.</param>
        /// <param name="filename">Path to an image or font file.</param>
        /// <param name="stream">Image or font bytes.</param>
        /// <param name="pixmap">Source pixmap for insert or replace image.</param>
        /// <param name="rotate">rotate the image.</param>
        /// <param name="xref">PDF xref of image or contents stream.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        /// <param name="alpha">deprecated and ignored.</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <exception cref="ValueErrorException">Document or page is closed, or operation requires a PDF.</exception>
        public int InsertImage(Rect rect, string filename = null, byte[] stream = null, Pixmap pixmap = null,
            byte[] mask = null, int rotate = 0, int xref = 0, int oc = 0, bool keepProportion = true,
            int alpha = -1, string overlay = "true")
        {
            var doc = RequireParent();
            if (!doc.IsPdf)
                throw new ValueErrorException("is no PDF");

            int srcCount = (xref > 0 ? 1 : 0)
                + (string.IsNullOrEmpty(filename) ? 0 : 1)
                + ((stream != null && stream.Length > 0) ? 1 : 0)
                + (pixmap != null ? 1 : 0);
            if (xref == 0 && srcCount != 1)
                throw new ValueErrorException("xref=0 needs exactly one of filename, pixmap, stream");

            if (!string.IsNullOrEmpty(filename) && !File.Exists(filename))
                throw new FileNotFoundException($"No such file: '{filename}'");
            if (mask != null && mask.Length > 0 && string.IsNullOrEmpty(filename) && (stream == null || stream.Length == 0))
                throw new ValueErrorException("mask requires stream or filename");

            while (rotate < 0) rotate += 360;
            while (rotate >= 360) rotate -= 360;
            if (rotate is not (0 or 90 or 180 or 270))
                throw new ValueErrorException("bad rotate value");

            var r = new Rect(rect);
            if (r.IsEmpty || r.IsInfinite)
                throw new ValueErrorException("rect must be finite and not empty");

            var inv = TransformationMatrix.Inverted() ?? Matrix.Identity;
            Rect clip = r * inv;

            string imgName = AllocateFzImgName();
            bool overlayB = string.IsNullOrEmpty(overlay)
                || overlay.Equals("true", StringComparison.OrdinalIgnoreCase)
                || overlay == "1";
            if (overlayB)
                WrapContents();

            var digests = doc.InsertedImages;
            var (imageXref, digestsOut) = _insert_image(
                filename: filename,
                pixmap: pixmap,
                stream: stream,
                imask: mask,
                clip: clip,
                overlay: overlayB ? 1 : 0,
                rotate: rotate,
                keep_proportion: keepProportion ? 1 : 0,
                oc: oc,
                width: 0,
                height: 0,
                xref: xref,
                alpha: alpha,
                _imgname: imgName,
                digests: digests);
            _ = digestsOut;
            return imageXref;
        }

        /// <summary>PyMuPDF <c>insert_image</c> resource name allocation (<c>fzImg*</c>).</summary>
        private string AllocateFzImgName()
        {
            var doc = RequireParent();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var im in GetImageRows(false))
            {
                if (!string.IsNullOrEmpty(im.name))
                    names.Add(im.name);
            }
            foreach (var xo in doc.GetPageXobjects(Number))
            {
                if (xo.TryGetValue("name", out var o) && o is string xs && !string.IsNullOrEmpty(xs))
                    names.Add(xs);
            }
            foreach (var fn in GetFonts(false))
            {
                if (!string.IsNullOrEmpty(fn.name))
                    names.Add(fn.name);
            }
            const string prefix = "fzImg";
            for (int i = 0; ; i++)
            {
                var cand = prefix + i.ToString(CultureInfo.InvariantCulture);
                if (!names.Contains(cand))
                    return cand;
            }
        }
        /// <summary>
        /// PDF only: Add a new font to be used by text output methods and return its xref. If not already present in the file, the font definition will be added. Supported are the built-in Base14_Fonts and the CJK fonts via "reserved" fontnames. Fonts can also be provided as a file path or a memory area containing the image of a font file.
        /// </summary>
        public int InsertFont(string fontName = "helv", string fontFile = null, byte[] fontbuffer = null,
            bool setSimple = false, int wmode = 0, int encoding = 0)
            => insert_font(fontName, fontFile, fontbuffer, setSimple, wmode, encoding);

        /// <summary>PyMuPDF <c>Page.insert_font</c> (<c>src/__init__.py</c>).</summary>
        internal int insert_font(string fontname = "helv", string fontfile = null, byte[] fontbuffer = null,
            bool set_simple = false, int wmode = 0, int encoding = 0)
        {
            Document doc = Parent;
            if (doc == null)
                throw new ValueErrorException("orphaned object: parent is None");
            int idx = 0;

            if (fontname.StartsWith("/"))
                fontname = fontname.Substring(1);
            var invChars = new HashSet<char>(fontname);
            invChars.IntersectWith(Constants.InvalidNameChars);
            if (invChars.Count > 0)
                throw new ValueErrorException($"bad fontname chars {invChars}");

            var font = CheckFont(fontname);
            if (font != null)                    // font already in font list of page
            {
                int xref = font.Value.xref;      // this is the xref
                if (doc.CheckFontInfo(xref) != null)        // also in our document font list?
                    return xref;                 // yes: we are done
                // need to build the doc FontInfo entry - done via get_char_widths
                doc.GetCharWidths(xref);
                return xref;
            }

            //--------------------------------------------------------------------------
            // the font is not present for this page
            //--------------------------------------------------------------------------

            Constants.Base14FontDict.TryGetValue(fontname.ToLowerInvariant(), out string bfname); // BaseFont if Base-14 font

            int serif = 0;
            int CJK_number = -1;
            string[] CJK_list_n = { "china-t", "china-s", "japan", "korea" };
            string[] CJK_list_s = { "china-ts", "china-ss", "japan-s", "korea-s" };

            try
            {
                CJK_number = Array.IndexOf(CJK_list_n, fontname);
                if (CJK_number < 0)
                    throw new ArgumentException();
                serif = 0;
            }
            catch
            {
                // Verbose in PyMuPDF/tests.
                // if (g_exceptions_verbose > 1)    exception_info()
            }

            if (CJK_number < 0)
            {
                try
                {
                    CJK_number = Array.IndexOf(CJK_list_s, fontname);
                    if (CJK_number < 0)
                        throw new ArgumentException();
                    serif = 1;
                }
                catch
                {
                    // Verbose in PyMuPDF/tests.
                    // if (g_exceptions_verbose > 1)    exception_info()
                }
            }

            // if (fontname.ToLowerInvariant() in fitz_fontdescriptors.Keys)
            // {
            //     import pymupdf_fonts
            //     fontbuffer = pymupdf_fonts.myfont(fontname);  // make a copy
            //     del pymupdf_fonts
            // }

            // install the font for the page
            string fontfile_str;
            if (fontfile != null)
            {
                fontfile_str = fontfile;
            }
            else
                fontfile_str = null;

            object[] val = _insertFont(fontname, bfname, fontfile_str, fontbuffer, set_simple, idx,
                wmode, serif, encoding, CJK_number);

            if (val == null || val.Length < 1)                   // did not work, error return
                return 0;                                      // (Python: return val)

            int xref_out = Convert.ToInt32(val[0]);                 // xref of installed font
            var fontdict = val[1] as Dictionary<string, object>;

            if (doc.CheckFontInfo(xref_out) != null)  // check again: document already has this font
                return xref_out;               // we are done

            // need to create document font info
            doc.GetCharWidths(xref_out, fontdict: fontdict);
            return xref_out;
        }

        /// <summary>PyMuPDF <c>CheckFont(page, fontname)</c> — page font list entry by resource name.</summary>
        private (int xref, string ext, string type, string baseName, string name, string encoding, int? referencer)? CheckFont(string fontname)
        {
            foreach (var f in GetFonts(false))
            {
                if (f.name == fontname)
                    return f;
            }
            return null;
        }

        /// <summary>PyMuPDF <c>Page._insertFont</c> (<c>src/__init__.py</c>).</summary>
        internal object[] _insertFont(string fontname, string bfname, string fontfile, byte[] fontbuffer,
            bool set_simple, int idx, int wmode, int serif, int encoding, int ordering)
        {
            lock (Utils.MuPDFLock)
            {
                mupdf.PdfPage page = NativePdfPage;
                mupdf.PdfDocument pdf = page.doc();

                object[] value = Helpers.JmInsertFontLegacy(pdf, RequireParent(), bfname, fontfile, fontbuffer,
                    set_simple, idx, wmode, serif, encoding, ordering);
                mupdf.PdfObj resources = mupdf.mupdf.pdf_dict_get_inheritable(page.obj(), mupdf.mupdf.pdf_new_name("Resources"));
                if (mupdf.mupdf.pdf_is_dict(resources) == 0)
                    resources = mupdf.mupdf.pdf_dict_put_dict(page.obj(), mupdf.mupdf.pdf_new_name("Resources"), 5);
                mupdf.PdfObj fonts = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("Font"));
                if (fonts.m_internal == null)
                {
                    fonts = mupdf.mupdf.pdf_new_dict(pdf, 5);
                    Helpers.PdfDictPutl(pdf, page.obj(), fonts,
                        mupdf.mupdf.pdf_new_name("Resources"), mupdf.mupdf.pdf_new_name("Font"));
                }
                int xref = Convert.ToInt32(value[0]);
                if (xref == 0)
                    throw new InvalidOperationException("cannot insert font");
                mupdf.PdfObj font_obj = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
                mupdf.mupdf.pdf_dict_puts(fonts, fontname, font_obj);
                pdf.Dispose();
                return value;
            }
        }
        /// <summary>
        /// Return a list of meta information dictionaries for all images displayed by the page. This works for all document types.
        /// </summary>
        public List<Block> GetImageInfo(bool hashes = false, bool xrefs = false)
            => ImageInfoDictToBlocks(GetImageInfoDict(hashes, xrefs));
        /// <summary>
        /// Return a list of meta information dictionaries for all images displayed by the page. This works for all document types.
        /// </summary>
        public List<Block> GetImageInfo(bool xrefs)
            => GetImageInfo(false, xrefs);
        /// <summary>
        /// Return a list of meta information dictionaries for all images displayed by the page. This works for all document types.
        /// </summary>
        /// <param name="hashes">Compute the MD5 hashcode for each encountered image, which allows identifying image duplicates. This adds the key `"digest"` to the output, whose value is a 16 byte `bytes` object.</param>
        /// <param name="xrefs">PDF only. Try to find the xref for each image. Implies `hashes=True`. Adds the `"xref"` key to the dictionary. If not found, the value is 0, which means, the image is either "inline" or its xref is undetectable for some reason. Please note that this option has an extended response time, because the MD5 hashcode will be computed at least two times for each image with an xref.</param>
        /// <returns>A list of dictionaries. This includes information for exactly those images, that are shown on the page; including *"inline images"*. The dictionary layout is similar to that of image blocks in `page.get_text("dict")`.</returns>
        public List<Dictionary<string, object>> GetImageInfoDict(bool hashes = false, bool xrefs = false)
        {
            var doc = RequireParent();
            if (xrefs && doc.IsPdf)
                hashes = true;
            if (!doc.IsPdf)
                xrefs = false;

            var imginfo = _imageInfo;
            if (imginfo != null && !xrefs)
                return imginfo;

            if (imginfo == null)
            {
                using (var tp = GetTextPage(flags: mupdf.mupdf.FZ_STEXT_PRESERVE_IMAGES))
                {
                    imginfo = tp.ExtractImgInfo(hashes);
                }
                if (hashes)
                    _imageInfo = imginfo;
            }

            if (!xrefs || !doc.IsPdf)
                return imginfo;

            var imglist = GetImageRows();
            var digests = new Dictionary<string, int>();
            foreach (var item in imglist)
            {
                int xref = item.xref;
                try
                {
                    using var pix = new Pixmap(doc, xref);
                    digests[BitConverter.ToString(pix.Digest)] = xref;
                }
                catch (ValueErrorException)
                {
                    // Not a loadable image xref (PyMuPDF Pixmap(doc, xref) raises similarly).
                }
            }
            for (int i = 0; i < imginfo.Count; i++)
            {
                var item = imginfo[i];
                int xref = 0;
                if (item.TryGetValue("digest", out var digestObj) && digestObj is byte[] digest)
                {
                    var key = BitConverter.ToString(digest);
                    if (digests.TryGetValue(key, out var found))
                        xref = found;
                }
                item["xref"] = xref;
                imginfo[i] = item;
            }
            return imginfo;
        }
        /// <summary>
        /// Return a list of meta information dictionaries for all images displayed by the page. This works for all document types.
        /// </summary>
        public List<Dictionary<string, object>> GetImageInfoDict(bool xrefs)
            => GetImageInfoDict(false, xrefs);

        private static Rect ImageInfoDictBbox(object bboxObj)
        {
            if (bboxObj is Rect r)
                return r;
            if (bboxObj is float[] f && f.Length >= 4)
                return new Rect(f[0], f[1], f[2], f[3]);
            return null;
        }

        private static Matrix ImageInfoDictTransform(object trObj)
        {
            if (trObj is Matrix m)
                return m;
            if (trObj is float[] t && t.Length >= 6)
                return new Matrix(t[0], t[1], t[2], t[3], t[4], t[5]);
            return null;
        }

        private static List<Block> ImageInfoDictToBlocks(List<Dictionary<string, object>> rows)
        {
            var result = new List<Block>(rows.Count);
            foreach (var d in rows)
            {
                var b = new Block
                {
                    Number = d.TryGetValue("number", out var n) ? Convert.ToInt32(n) : 0,
                    Bbox = d.TryGetValue("bbox", out var bb) ? ImageInfoDictBbox(bb) : null,
                    Width = d.TryGetValue("width", out var w) ? Convert.ToInt32(w) : 0,
                    Height = d.TryGetValue("height", out var h) ? Convert.ToInt32(h) : 0,
                    Ext = d.TryGetValue("ext", out var ex) ? ex?.ToString() : null,
                    Xres = d.TryGetValue("xres", out var xr) ? Convert.ToInt32(xr) : 0,
                    Yres = d.TryGetValue("yres", out var yr) ? Convert.ToInt32(yr) : 0,
                    Bpc = d.TryGetValue("bpc", out var bpc) ? Convert.ToByte(bpc) : (byte)0,
                    Transform = d.TryGetValue("transform", out var tr) ? ImageInfoDictTransform(tr) : null,
                    Size = d.TryGetValue("size", out var sz) ? Convert.ToUInt32(sz) : 0u,
                    Image = d.TryGetValue("image", out var img) ? img as byte[] : null,
                    Mask = d.TryGetValue("mask", out var m) ? m as byte[] : null,
                    Xref = d.TryGetValue("xref", out var xref) ? Convert.ToInt32(xref) : 0,
                };
                if (d.TryGetValue("cs-name", out var csName))
                    b.CsName = csName?.ToString();
                if (d.TryGetValue("colorspace", out var cs))
                    b.ColorSpace = Convert.ToInt32(cs);
                if (d.TryGetValue("colorspace.n", out var csn))
                    b.ColorSpace = Convert.ToInt32(csn);
                result.Add(b);
            }
            return result;
        }
        /// <summary>
        /// PDF only: Return boundary boxes and transformation matrices of an embedded image. This is an improved version of Page.get_image_bbox with the following differences:.
        /// </summary>
        /// <param name="name">Image or XObject name.</param>
        /// <returns>Boundary boxes and respective transformation matrices for each image occurrence on the page. If the item is not on the page, an empty list `[]` is returned.</returns>
        public List<Rect> GetImageRects(object name)
        {
            int xref = ResolveImageXref(name);
            var doc = RequireParent();
            byte[] digest;
            using (var pix = new Pixmap(doc, xref))
                digest = pix.Digest;

            var infos = GetImageInfoDict(hashes: true);
            var bboxes = new List<Rect>();
            foreach (var im in infos)
            {
                if (!im.TryGetValue("digest", out var digestObj) || digestObj is not byte[] d || !d.SequenceEqual(digest))
                    continue;
                if (im.TryGetValue("bbox", out var bboxObj) && bboxObj is float[] b && b.Length == 4)
                    bboxes.Add(new Rect(b[0], b[1], b[2], b[3]));
            }
            return bboxes;
        }

        /// <summary>
        /// Return image positions and transformation matrices.
        /// </summary>
        public List<(Rect bbox, Matrix transform)> GetImageRects(object name, bool transform)
        {
            if (!transform)
                return GetImageRects(name).Select(r => (r, Matrix.Identity)).ToList();

            int xref = ResolveImageXref(name);
            var doc = RequireParent();
            byte[] digest;
            using (var pix = new Pixmap(doc, xref))
                digest = pix.Digest;

            var infos = GetImageInfoDict(hashes: true);
            var bboxes = new List<(Rect bbox, Matrix matrix)>();
            foreach (var im in infos)
            {
                if (!im.TryGetValue("digest", out var digestObj) || digestObj is not byte[] d || !d.SequenceEqual(digest))
                    continue;
                if (!im.TryGetValue("bbox", out var bboxObj) || bboxObj is not float[] b || b.Length != 4)
                    continue;
                if (!im.TryGetValue("transform", out var trObj) || trObj is not float[] t || t.Length != 6)
                    continue;
                bboxes.Add((new Rect(b[0], b[1], b[2], b[3]), new Matrix(t[0], t[1], t[2], t[3], t[4], t[5])));
            }
            return bboxes;
        }
        /// <summary>
        /// PDF only: Return boundary box and transformation matrix of an embedded image.
        /// </summary>
        /// <param name="name">Image or XObject name.</param>
        /// <returns>the boundary box of the image; optionally also its transformation matrix.</returns>
        public Rect GetImageBbox(object name)
        {
            return GetImageBboxImpl(name, transform: false).bbox;
        }

        /// <summary>
        /// Get rectangle and transform occupied by image <paramref name="name"/>.
        /// Port of Python Page.get_image_bbox(name, transform=1).
        /// </summary>
        public (Rect bbox, Matrix transform) GetImageBbox(object name, bool transform)
        {
            return GetImageBboxImpl(name, transform);
        }

        private (Rect bbox, Matrix transform) GetImageBboxImpl(object name, bool transform)
        {
            var doc = RequireParent();
            if (doc.IsClosed || doc.IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");

            var infRect = new Rect(1, 1, -1, -1);
            var nullMat = Matrix.Identity;

            object itemObj;
            int xref;
            if (name is object[] arr || name is System.Collections.IList)
            {
                object[] vals = name as object[] ?? (name as System.Collections.IList).Cast<object>().ToArray();
                if (vals.Length == 0 || vals[vals.Length - 1] is not int lastInt)
                    throw new ValueErrorException("need item of full page image list");
                itemObj = vals;
                xref = lastInt;
            }
            else
            {
                string imgName = Convert.ToString(name);
                var imglist = GetImageRows(full: true).Where(i => i.name == imgName).ToList();
                if (imglist.Count == 0)
                    throw new ValueErrorException("bad image name");
                if (imglist.Count > 1)
                    throw new ValueErrorException($"found multiple images named '{imgName}'.");
                var i = imglist[0];
                itemObj = new object[] { i.xref, i.smask, i.width, i.height, i.bpc, i.colorspace, i.altCs, i.name, i.filter };
                xref = i.xref;
            }

            if (xref != 0 || transform)
            {
                try
                {
                    if (transform)
                    {
                        var list = GetImageRects(itemObj, transform: true);
                        if (list.Count > 0)
                            return list[0];
                        return (infRect, nullMat);
                    }
                    var rects = GetImageRects(itemObj);
                    if (rects.Count > 0)
                        return (rects[0], nullMat);
                    return (infRect, nullMat);
                }
                catch
                {
                    return (infRect, nullMat);
                }
            }

            // PyMuPDF JM_image_reporter when stream_xref (item[-1]) == 0.
            object[] itemVals = itemObj as object[]
                ?? (itemObj as System.Collections.IList)?.Cast<object>().ToArray();
            if (itemVals == null || itemVals.Length < 8)
                return (infRect, nullMat);

            string targetName = Convert.ToString(itemVals[7], CultureInfo.InvariantCulture);
            var pdfPage = NativePdfPage;
            if (pdfPage == null)
                return (infRect, nullMat);

            foreach (var (rname, q) in Helpers.JmImageReporter(pdfPage))
            {
                if (!string.Equals(rname, targetName, StringComparison.Ordinal))
                    continue;
                var bbox = new Quad(q).Rect;
                if (!transform)
                    return (bbox, nullMat);
                // transform==1: util_hor_matrix path (PyMuPDF get_image_bbox).
                var hm = Helpers.UtilHorMatrix(new Point(q.ll.x, q.ll.y), new Point(q.lr.x, q.lr.y));
                float h = Math.Abs(q.ll.y - q.ul.y);
                float w = Math.Abs(q.ur.x - q.ul.x);
                if (h < 1e-12 || w < 1e-12)
                    return (bbox, nullMat);
                var m0 = new Matrix(1 / w, 0, 0, 1 / h, 0, 0);
                var m = ~(hm * m0);
                return (bbox, m);
            }
            return (infRect, nullMat);
        }
        /// <summary>
        /// PDF only: Return a list of Form XObjects referenced by the page. Wrapper for Document.get_page_xobjects.
        /// </summary>
        public List<Dictionary<string, object>> GetXobjects()
        {
            return RequireParent().GetPageXobjects(Number);
        }
        /// <summary>
        /// See PyMuPDF Page.language.
        /// </summary>
        public string Language
        {
            get
            {
                var pdfPage = Helpers.AsPdfPage(NativePage, required: false);
                if (pdfPage == null || pdfPage.m_internal == null)
                    return null;
                var lang = mupdf.mupdf.pdf_dict_get_inheritable(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Lang"));
                if (lang == null || lang.m_internal == null)
                    return null;
                return mupdf.mupdf.pdf_to_str_buf(lang);
            }
        }
        /// <summary>
        /// See PyMuPDF Page.set_language.
        /// </summary>
        /// <param name="language">OCR language tag (e.g. eng).</param>
        public void SetLanguage(string language = null)
        {
            var pdfPage = NativePdfPage;
            var obj = pdfPage.obj();
            var key = mupdf.mupdf.pdf_new_name("Lang");
            if (string.IsNullOrEmpty(language))
            {
                mupdf.mupdf.pdf_dict_del(obj, key);
                return;
            }
            var val = mupdf.mupdf.pdf_new_text_string(language);
            mupdf.mupdf.pdf_dict_put(obj, key, val);
        }
        /// <summary>
        /// Maps unrotated page coordinates into rotated space.
        /// </summary>
        public Matrix RotationMatrix
        {
            get
            {
                var inv = DerotationMatrix.Inverted();
                return inv ?? Matrix.Identity;
            }
        }
        /// <summary>
        /// See PyMuPDF Page.refresh.
        /// </summary>
        public void Refresh()
        {
            var reloaded = RequireParent().ReloadPage(this);
            _nativePage?.Dispose();
            _nativePage = reloaded._nativePage;
            reloaded._nativePage = null;
        }
        /// <summary>
        /// Replace the image at xref with another one.
        /// </summary>
        /// <param name="xref">the xref of the image.</param>
        /// <param name="filename">Path to an image or font file.</param>
        /// <param name="pixmap">Source pixmap for insert or replace image.</param>
        /// <param name="stream">Image or font bytes.</param>
        public void ReplaceImage(int xref, string? filename = null, Pixmap? pixmap = null, byte[]? stream = null)
        {
            var doc = RequireParent();  // the owning document
            if (!doc.XrefIsImage(xref))
                throw new ValueErrorException("xref not an image");  // insert new image anywhere in page

            int count = (string.IsNullOrEmpty(filename) ? 0 : 1)
                + ((stream != null && stream.Length > 0) ? 1 : 0)
                + (pixmap != null ? 1 : 0);
            if (count != 1)
                throw new ValueErrorException("Exactly one of filename/stream/pixmap must be given");

            int newXref = InsertImage(Rect, filename: filename, stream: stream, pixmap: pixmap);
            doc.XrefCopy(newXref, xref);  // copy over new to old
            List<int> contentsList = GetContents();
            int lastContentsXref = contentsList[contentsList.Count - 1];  // last item of page.GetContents()
            // new image insertion has created a new /Contents source,
            // which we will set to spaces now
            doc.UpdateStream(lastContentsXref, new byte[] { (byte)' ' });
            _imageInfo = null;  // clear cache of extracted image information
        }
        /// <summary>
        /// Delete the image at xref. This is slightly misleading: actually the image is being replaced with a small transparent Pixmap using above Page.replace_image. The visible effect however is equivalent.
        /// </summary>
        /// <param name="xref">the xref of the image.</param>
        public void DeleteImage(int xref)
        {
            // make a small 100% transparent pixmap (of just any dimension)
            using var pix = new Pixmap(Colorspace.Gray, new IRect(0, 0, 1, 1), 1);
            pix.ClearWith();  // clear all samples bytes to 0x00
            ReplaceImage(xref, pixmap: pix);
        }
        /// <summary>
        /// PDF only: Permanently remove page content outside the given rectangle. This is similar to Page.set_cropbox, but the page's rectangle will not be changed, only the content outside the rectangle will be removed.
        /// </summary>
        /// <param name="rect">The rectangle to clip to. Must be finite and its intersection with the page must not be empty.</param>
        public void ClipToRect(Rect rect)
        {
            var clip = new Rect(rect);
            if (clip.IsInfinite || (clip & Rect).IsEmpty)
                throw new ArgumentException("rect must not be infinite or empty");
            clip = clip.Transform(TransformationMatrix);
            var pdfPage = NativePdfPage;
            pdfPage.pdf_clip_page(clip.ToFzRect());
        }
        /// <summary>
        /// See PyMuPDF Page.get_layout.
        /// </summary>
        public object GetLayout()
        {
            if (_layoutInformation != null)
                return _layoutInformation;
            if (_getLayout == null)
                return null;
            _layoutInformation = _getLayout(this);
            return _layoutInformation;
        }
        /// <summary>
        /// See PyMuPDF Page.layout_information.
        /// </summary>
        public object LayoutInformation
        {
            get => _layoutInformation;
            set => _layoutInformation = value;
        }
        /// <summary>
        /// See PyMuPDF Page.get_layout_provider.
        /// </summary>
        public static Func<Page, object> GetLayoutProvider
        {
            get => _getLayout;
            set => _getLayout = value;
        }

        /// <summary>
        /// Get OCGs and OCMDs used in page resources.
        /// Port of Python Page.get_oc_items().
        /// </summary>
        public List<(string name, int xref, string type)> GetOcItems()
        {
            var rc = new List<(string name, int xref, string type)>();
            var props = GetResourceProperties();
            foreach (var p in props)
            {
                string text = RequireParent().XrefObject(p.xref, compressed: true);
                string octype;
                if (text.Contains("/Type/OCG"))
                    octype = "ocg";
                else if (text.Contains("/Type/OCMD"))
                    octype = "ocmd";
                else
                    continue;
                rc.Add((p.name, p.xref, octype));
            }
            return rc;
        }

        private List<(string name, int xref)> GetResourceProperties()
        {
            var list = new List<(string name, int xref)>();
            var pdfPage = NativePdfPage;
            var resources = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Resources"));
            if (resources == null || resources.m_internal == null)
                return list;
            var props = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("Properties"));
            if (props == null || props.m_internal == null || mupdf.mupdf.pdf_is_dict(props) == 0)
                return list;
            int n = mupdf.mupdf.pdf_dict_len(props);
            for (int i = 0; i < n; i++)
            {
                var k = mupdf.mupdf.pdf_dict_get_key(props, i);
                var v = mupdf.mupdf.pdf_dict_get_val(props, i);
                if (k == null || k.m_internal == null || v == null || v.m_internal == null)
                    continue;
                string name = mupdf.mupdf.pdf_to_name(k);
                int xref = mupdf.mupdf.pdf_to_num(v);
                if (!string.IsNullOrEmpty(name) && xref > 0)
                    list.Add((name, xref));
            }
            return list;
        }
        /// <summary>
        /// PDF only: Write the text of one or more Textwriter objects to the page.
        /// </summary>
        /// <param name="rect">where to place the text. If omitted, the rectangle union of the text writers is used.</param>
        /// <param name="writers">a non-empty tuple / list of TextWriter objects or a single TextWriter.</param>
        /// <param name="overlay">put the text in foreground or background.</param>
        /// <param name="color">set the text color, overwrites resp. value in the text writers.</param>
        /// <param name="opacity">set transparency, overwrites resp. value in the text writers.</param>
        /// <param name="rotate">rotate the text by an arbitrary angle.</param>
        /// <param name="oc">the xref of an OCG or OCMD.</param>
        public void WriteText(Rect rect = null, IEnumerable<TextWriter> writers = null, bool overlay = true,
            float[] color = null, float? opacity = null, bool keepProportion = true, int rotate = 0, int oc = 0)
        {
            if (writers == null)
                throw new ArgumentException("need at least one pymupdf.TextWriter");
            var writerList = writers.ToList();
            if (writerList.Count == 0)
                throw new ArgumentException("need at least one pymupdf.TextWriter");

            if (writerList.Count == 1 && rotate == 0 && rect == null)
            {
                writerList[0].WriteText(this, opacity: opacity ?? -1, color: color, overlay: overlay ? 1 : 0, oc: oc);
                return;
            }

            var clip = new Rect(writerList[0].TextRect);
            var textDoc = new Document();
            var tpage = textDoc.NewPage(width: Width, height: Height);
            foreach (var writer in writerList)
            {
                clip = clip | writer.TextRect;
                writer.WriteText(tpage, opacity: opacity ?? -1, color: color, overlay: 1, oc: 0);
            }
            var target = rect ?? clip;
            ShowPdfPage(target, textDoc, 0, keepProportion: keepProportion, overlay: overlay, oc: oc, rotate: rotate, clip: clip);
        }

        private int ResolveImageXref(object name)
        {
            if (name is int ixref)
                return ixref;
            if (name is Entry entry)
                return entry.Xref;
            if (name is object[] arr && arr.Length > 0 && arr[0] is int arrXref)
                return arrXref;
            if (name is string imgName)
            {
                var imglist = GetImageRows().Where(i => i.name == imgName).ToList();
                if (imglist.Count == 0)
                    throw new ArgumentException("bad image name");
                if (imglist.Count != 1)
                    throw new ArgumentException("multiple image names found");
                return imglist[0].xref;
            }
            throw new ArgumentException("bad image name");
        }

        // ─── Page Modifications ─────────────────────────────────────────
        /// <summary>
        /// PDF only: Set the rotation of the page.
        /// </summary>
        public void SetRotation(int rotation)
        {
            var pdfPage = NativePdfPage;
            int rot = rotation;
            while (rot < 0) rot += 360;
            rot = rot % 360;
            mupdf.mupdf.pdf_dict_put_int(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Rotate"), rot);
        }
        /// <summary>
        /// PDF only: Change the physical page dimension by setting mediabox in the page's object definition.
        /// </summary>
        /// <param name="rect">Target rectangle in unrotated page coordinates.</param>
        public void SetMediaBox(Rect rect)
        {
            var pdfPage = NativePdfPage;
            var obj = pdfPage.obj();
            var fzr = rect.ToFzRect();
            if (mupdf.mupdf.fz_is_empty_rect(fzr) != 0 || mupdf.mupdf.fz_is_infinite_rect(fzr) != 0)
                throw new ArgumentException("rect must be finite and not empty");
            mupdf.mupdf.pdf_dict_put_rect(obj, mupdf.mupdf.pdf_new_name("MediaBox"), fzr);
            mupdf.mupdf.pdf_dict_del(obj, mupdf.mupdf.pdf_new_name("CropBox"));
            mupdf.mupdf.pdf_dict_del(obj, mupdf.mupdf.pdf_new_name("ArtBox"));
            mupdf.mupdf.pdf_dict_del(obj, mupdf.mupdf.pdf_new_name("BleedBox"));
            mupdf.mupdf.pdf_dict_del(obj, mupdf.mupdf.pdf_new_name("TrimBox"));
        }
        /// <summary>
        /// Top-left point of CropBox (displacement of the visible area).
        /// </summary>
        public Point CropBoxPosition => CropBox.TopLeft;
        /// <summary>
        /// PDF only: change the visible part of the page.
        /// </summary>
        public void SetCropBox(Rect rect) => _set_pagebox("CropBox", rect);
        /// <summary>
        /// PDF only: modify `/BleedBox`.
        /// </summary>
        public void SetBleedBox(Rect rect) => _set_pagebox("BleedBox", rect);
        /// <summary>
        /// PDF only: Set the resp. rectangle in the page object. For the meaning of these objects see AdobeManual, page 77. Parameter and restrictions are the same as for Page.set_cropbox.
        /// </summary>
        public void SetTrimBox(Rect rect) => _set_pagebox("TrimBox", rect);
        /// <summary>
        /// PDF only: modify `/ArtBox`.
        /// </summary>
        public void SetArtBox(Rect rect) => _set_pagebox("ArtBox", rect);

        private void SetSpecialBox(string name, Rect rect)
        {
            var pdfPage = NativePdfPage;
            var obj = pdfPage.obj();
            mupdf.mupdf.pdf_dict_put_rect(obj, mupdf.mupdf.pdf_new_name(name), rect.ToFzRect());
        }

        // ─── Drawing / Shapes ───────────────────────────────────────────
        /// <summary>
        /// PDF only: Create a new Shape object for the page.
        /// </summary>
        public Shape NewShape() => new Shape(this);

        private Point FinishPageDraw(
            Func<Shape, Point> draw,
            float[] color = null,
            float[] fill = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            bool closePath = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0,
            Point morphOrigin = null,
            Matrix morphMatrix = null,
            bool evenOdd = false,
            string blendMode = null)
        {
            float[] strokeColor = color ?? new float[] { 0f };
            var shape = NewShape();
            Point q = draw(shape);
            shape.Finish(
                color: strokeColor,
                fill: fill,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                dashes: dashes,
                closePath: closePath,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc,
                morphFix: morphOrigin,
                morphMat: morphMatrix,
                evenOdd: evenOdd,
                blendMode: blendMode);
            shape.Commit(overlay);
            return q;
        }
        /// <summary>
        /// , width=1, dashes=None, lineCap=0, lineJoin=0, overlay=True, morph=None, stroke_opacity=1, fill_opacity=1, oc=0).
        /// </summary>
        /// <param name="p1">First point.</param>
        /// <param name="p2">Second point.</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        public Point DrawLine(
            Point p1,
            Point p2,
            float[] color = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            Point morphOrigin = null,
            Matrix morphMatrix = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            return FinishPageDraw(
                s => s.DrawLine(p1, p2),
                color: color,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                overlay: overlay,
                closePath: false,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc,
                morphOrigin: morphOrigin,
                morphMatrix: morphMatrix);
        }
        /// <summary>
        /// , fill=None, width=1, dashes=None, lineCap=0, lineJoin=0, overlay=True, morph=None, stroke_opacity=1, fill_opacity=1, radius=None, oc=0).
        /// </summary>
        /// <param name="rect">Target rectangle in unrotated page coordinates.</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        /// <param name="radius">Radius in points.</param>
        public Point DrawRect(
            Rect rect,
            float[] color = null,
            float[] fill = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            Point morphOrigin = null,
            Matrix morphMatrix = null,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0,
            object radius = null)
        {
            return FinishPageDraw(
                s => s.DrawRect(rect, radius),
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                overlay: overlay,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc,
                morphOrigin: morphOrigin,
                morphMatrix: morphMatrix);
        }
        /// <summary>
        /// , fill=None, width=1, dashes=None, lineCap=0, lineJoin=0, overlay=True, morph=None, stroke_opacity=1, fill_opacity=1, oc=0).
        /// </summary>
        /// <param name="center">Center point for circles or sectors.</param>
        /// <param name="radius">Radius in points.</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        public Point DrawCircle(
            Point center,
            float radius,
            float[] color = null,
            float[] fill = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            Point morphOrigin = null,
            Matrix morphMatrix = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            return FinishPageDraw(
                s => s.DrawCircle(center, radius),
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                overlay: overlay,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc,
                morphOrigin: morphOrigin,
                morphMatrix: morphMatrix);
        }
        /// <summary>
        /// , fill=None, width=1, dashes=None, lineCap=0, lineJoin=0, overlay=True, morph=None, stroke_opacity=1, fill_opacity=1, oc=0).
        /// </summary>
        /// <param name="rect">Target rectangle in unrotated page coordinates.</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        public Point DrawOval(
            Rect rect,
            float[] color = null,
            float[] fill = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            Point morphOrigin = null,
            Matrix morphMatrix = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            return FinishPageDraw(
                s => s.DrawOval(rect),
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                overlay: overlay,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc,
                morphOrigin: morphOrigin,
                morphMatrix: morphMatrix);
        }
        /// <summary>
        /// , fill=None, width=1, dashes=None, lineCap=0, lineJoin=0, overlay=True, closePath=False, morph=None, stroke_opacity=1, fill_opacity=1, oc=0).
        /// </summary>
        /// <param name="p1">First point.</param>
        /// <param name="p2">Second point.</param>
        /// <param name="p3">Third point.</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        public Point DrawCurve(
            Point p1,
            Point p2,
            Point p3,
            float[] color = null,
            float[] fill = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            Point morphOrigin = null,
            Matrix morphMatrix = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            return FinishPageDraw(
                s => s.DrawCurve(p1, p2, p3),
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                overlay: overlay,
                closePath: false,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc,
                morphOrigin: morphOrigin,
                morphMatrix: morphMatrix);
        }
        /// <summary>
        /// , width=1, dashes=None, lineCap=0, lineJoin=0, overlay=True, morph=None, stroke_opacity=1, fill_opacity=1, oc=0).
        /// </summary>
        /// <param name="p1">First point.</param>
        /// <param name="p2">Second point.</param>
        /// <param name="breadth">Amplitude for squiggle or zigzag lines.</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        public Point DrawSquiggle(
            Point p1,
            Point p2,
            float breadth = 2,
            float[] color = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            Point morphOrigin = null,
            Matrix morphMatrix = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            return FinishPageDraw(
                s => s.DrawSquiggle(p1, p2, breadth),
                color: color,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                overlay: overlay,
                closePath: false,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc,
                morphOrigin: morphOrigin,
                morphMatrix: morphMatrix);
        }
        /// <summary>
        /// , width=1, dashes=None, lineCap=0, lineJoin=0, overlay=True, morph=None, stroke_opacity=1, fill_opacity=1, oc=0).
        /// </summary>
        /// <param name="p1">First point.</param>
        /// <param name="p2">Second point.</param>
        /// <param name="breadth">Amplitude for squiggle or zigzag lines.</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        public Point DrawZigzag(
            Point p1,
            Point p2,
            float breadth = 2,
            float[] color = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            Point morphOrigin = null,
            Matrix morphMatrix = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            return FinishPageDraw(
                s => s.DrawZigzag(p1, p2, breadth),
                color: color,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                overlay: overlay,
                closePath: false,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc,
                morphOrigin: morphOrigin,
                morphMatrix: morphMatrix);
        }
        /// <summary>
        /// , fill=None, width=1, dashes=None, lineCap=0, lineJoin=0, fullSector=True, overlay=True, closePath=False, morph=None, stroke_opacity=1, fill_opacity=1, oc=0).
        /// </summary>
        /// <param name="center">Center point for circles or sectors.</param>
        /// <param name="point">Location for point-based annotations.</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        public Point DrawSector(
            Point center,
            Point point,
            float beta,
            float[] color = null,
            float[] fill = null,
            string dashes = null,
            bool fullSector = true,
            Point morphOrigin = null,
            Matrix morphMatrix = null,
            float width = 1,
            bool closePath = false,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            return FinishPageDraw(
                s => s.DrawSector(center, point, beta, fullSector),
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                overlay: overlay,
                closePath: closePath,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc,
                morphOrigin: morphOrigin,
                morphMatrix: morphMatrix);
        }
        /// <summary>
        /// , fill=None, width=1, dashes=None, lineCap=0, lineJoin=0, overlay=True, closePath=False, morph=None, stroke_opacity=1, fill_opacity=1, oc=0).
        /// </summary>
        /// <param name="points">Vertices for polyline, polygon, or ink annotations.</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        public Point DrawPolyline(
            Point[] points,
            float[] color = null,
            float[] fill = null,
            string dashes = null,
            float width = 1,
            Point morphOrigin = null,
            Matrix morphMatrix = null,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            bool closePath = false,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            return FinishPageDraw(
                s => s.DrawPolyline(points),
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                overlay: overlay,
                closePath: closePath,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc,
                morphOrigin: morphOrigin,
                morphMatrix: morphMatrix);
        }
        /// <summary>
        /// , fill=None, width=1, dashes=None, lineCap=0, lineJoin=0, overlay=True, morph=None, stroke_opacity=1, fill_opacity=1, oc=0).
        /// </summary>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        public Point DrawQuad(
            Quad quad,
            float[] color = null,
            float[] fill = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            Point morphOrigin = null,
            Matrix morphMatrix = null,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            return FinishPageDraw(
                s => s.DrawQuad(quad),
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                overlay: overlay,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc,
                morphOrigin: morphOrigin,
                morphMatrix: morphMatrix);
        }
        /// <summary>
        /// , fill=None, width=1, dashes=None, lineCap=0, lineJoin=0, overlay=True, closePath=False, morph=None, stroke_opacity=1, fill_opacity=1, oc=0).
        /// </summary>
        /// <param name="p1">First point.</param>
        /// <param name="p2">Second point.</param>
        /// <param name="p3">Third point.</param>
        /// <param name="p4">Fourth point (Bezier).</param>
        /// <param name="overlay">Draw in overlay (foreground) when non-zero.</param>
        /// <param name="oc">Optional content group xref for layer visibility.</param>
        public Point DrawBezier(
            Point p1,
            Point p2,
            Point p3,
            Point p4,
            float[] color = null,
            float[] fill = null,
            string dashes = null,
            float width = 1,
            Point morphOrigin = null,
            Matrix morphMatrix = null,
            bool closePath = false,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            return FinishPageDraw(
                s => s.DrawBezier(p1, p2, p3, p4),
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                overlay: overlay,
                closePath: closePath,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc,
                morphOrigin: morphOrigin,
                morphMatrix: morphMatrix);
        }

        // ─── Page Contents / Resources ──────────────────────────────────
        /// <summary>
        /// See PyMuPDF Page.clean_contents.
        /// </summary>
        /// <param name="sanitize">Clean-contents sanitize level passed to MuPDF.</param>
        public void CleanContents(int sanitize = 1)
        {
            if (sanitize == 0 && !IsWrapped)
                WrapContents();
            var pdf = RequireParent().NativePdfDocument;
            Helpers.InvalidatePdfPageCache(pdf, this);
            mupdf.PdfPage pdfPage = Helpers.AsPdfPageFresh(this);
            if (pdfPage.m_internal == null)
                return;
            try
            {
                Helpers.PdfFilterOptionsRef filterPkg = Helpers.MakePdfFilterOptions(recurse: 1, sanitize: sanitize);
                // Python: mupdf.pdf_filter_page_contents(page.doc(), page, filter_)
                pdfPage.doc().pdf_filter_page_contents(pdfPage, filterPkg.Filter);
            }
            finally
            {
                pdfPage.Dispose();
            }
        }

        /// <summary>
        /// After direct PDF xref edits, MuPDF's cached <c>fz_page</c> can be stale.
        /// Reload it from the document so subsequent operations see current /Contents.
        /// </summary>
        internal void SyncPdfPageAfterEdit()
        {
            var doc = RequireParent();
            var pdf = doc.NativePdfDocument;
            mupdf.mupdf.ll_pdf_drop_page_tree_internal(pdf.m_internal);
            DisposeCachedPdfPage();
            int pno = Number;
            int chapter = ReloadChapter;
            int chapterPage = ReloadChapterPage;
            var fresh = chapter >= 0 ? doc.LoadPage(chapter, chapterPage) : doc.LoadPage(pno);
            _nativePage?.Dispose();
            _nativePage = fresh.NativePage;
            fresh._nativePage = null;
        }
        /// <summary>
        /// See PyMuPDF Page.read_contents.
        /// </summary>
        public byte[] ReadContents() => Tools.GetAllContents(this);
        /// <summary>
        /// See PyMuPDF Page.set_contents.
        /// </summary>
        /// <param name="xref">PDF xref of image or contents stream.</param>
        public void SetContents(int xref)
        {
            if (RequireParent().IsClosed)
                throw new ValueErrorException("document closed");
            if (!RequireParent().IsPdf)
                throw new ValueErrorException("is no PDF");
            int xrefLen = RequireParent().XrefLength;
            if (xref < 1 || xref >= xrefLen)
                throw new ValueErrorException("bad xref");
            if (!RequireParent().XrefIsStream(xref))
                throw new ValueErrorException("xref is no stream");
            RequireParent().XrefSetKey(Xref, "Contents", $"{xref} 0 R");
        }
        /// <summary>
        /// See PyMuPDF Page.get_contents.
        /// </summary>
        public List<int> GetContents()
        {
            var result = new List<int>();
            var pdfPage = NativePdfPage;
            var contents = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Contents"));
            if (contents.m_internal == null) return result;
            if (mupdf.mupdf.pdf_is_array(contents) != 0)
            {
                int n = mupdf.mupdf.pdf_array_len(contents);
                for (int i = 0; i < n; i++)
                    result.Add(mupdf.mupdf.pdf_to_num(mupdf.mupdf.pdf_array_get(contents, i)));
            }
            else
                result.Add(mupdf.mupdf.pdf_to_num(contents));
            return result;
        }
        /// <summary>
        /// See PyMuPDF Page.wrap_contents.
        /// </summary>
        public void WrapContents()
        {
            // Ensure page is in a balanced graphics state. (PyMuPDF Page.wrap_contents)
            var (push, pop) = CountQBalance(); // count missing "q"/"Q" commands
            if (push > 0) // prepend required push commands
            {
                var prepend = string.Concat(Enumerable.Repeat("q\n", push)); // Py: prepend = b"q\n" * push
                Tools.InsertContents(this, System.Text.Encoding.UTF8.GetBytes(prepend), overlay: false); // TOOLS._insert_contents(self, prepend, False)
            }
            if (pop > 0) // append required pop commands
            {
                var append = string.Concat(Enumerable.Repeat("\nQ", pop)) + "\n"; // Py: append = b"\nQ" * pop + b"\n"
                Tools.InsertContents(this, System.Text.Encoding.UTF8.GetBytes(append), overlay: true); // TOOLS._insert_contents(self, append, True)
            }
        }
        /// <summary>
        /// True when page contents are wrapped in q/Q graphics state operators.
        /// </summary>
        public bool IsWrapped
        {
            get
            {
                var (push, pop) = CountQBalance();
                return push == 0 && pop == 0;
            }
        }
        /// <summary>
        /// PDF only: Set page rotation to 0 while maintaining appearance and page content.
        /// </summary>
        /// <returns>The inverted matrix used to achieve this change. If the page was not rotated (rotation 0), Identity is returned. The method automatically recomputes the rectangles of any annotations, links and widgets present on the page.</returns>
        public Matrix RemoveRotation()
        {
            int rot = Rotation;
            if (rot == 0) return Matrix.Identity;

            var mb = MediaBox;
            Matrix mat0;
            if (rot == 90)
                mat0 = new Matrix(1, 0, 0, 1, mb.Y1 - mb.X1 - mb.X0 - mb.Y0, 0);
            else if (rot == 270)
                mat0 = new Matrix(1, 0, 0, 1, 0, mb.X1 - mb.Y1 - mb.Y0 - mb.X0);
            else
                mat0 = new Matrix(1, 0, 0, 1, -2 * mb.X0, -2 * mb.Y0);

            var mat = mat0 * DerotationMatrix;
            string cmd = Helpers.FormatPdfReals(mat.A, mat.B, mat.C, mat.D, mat.E, mat.F) + " cm ";
            var pdf = RequireParent().NativePdfDocument;
            var buf = Helpers.BufferFromBytes(System.Text.Encoding.UTF8.GetBytes(cmd));
            Helpers.JM_insert_contents(pdf, NativePdfPage.obj(), buf, overlay: false);

            if (rot == 90 || rot == 270)
            {
                var swapped = new Rect(mb.Y0, mb.X0, mb.Y1, mb.X1);
                SetMediaBox(swapped);
            }

            SetRotation(0);

            var inv = mat.Inverted() ?? Matrix.Identity;

            foreach (var annot in Annots())
            {
                var tr = new Rect(annot.Rect).Transform(inv);
                try { annot.SetRect(tr); } catch { }
            }

            foreach (var link in GetLinksDict())
            {
                if (link.TryGetValue("from", out var fromObj) && fromObj is Rect rr)
                    link["from"] = new Rect(rr).Transform(inv);
                DeleteLink(link);
                try { InsertLinkVoid(link); } catch { }
            }

            foreach (var widget in Widgets())
            {
                try
                {
                    var wr = new Rect(widget.Rect).Transform(inv);
                    widget.SetRect(wr);
                    widget.Update();
                }
                catch { }
            }

            // /Contents and MediaBox were edited; MuPDF's cached fz_page must be reloaded
            // before fz_run_page (e.g. GetBboxlog) or native code can AV.
            SyncPdfPageAfterEdit();

            return inv;
        }

        private static mupdf.FzBuffer LoadPageContentsBuffer(mupdf.PdfObj pageObj)
        {
            var contents = mupdf.mupdf.pdf_dict_get(pageObj, mupdf.mupdf.pdf_new_name("Contents"));
            if (contents.m_internal == null)
                return mupdf.mupdf.fz_new_buffer(16);

            if (mupdf.mupdf.pdf_is_array(contents) != 0)
            {
                var res = mupdf.mupdf.fz_new_buffer(1024);
                int n = mupdf.mupdf.pdf_array_len(contents);
                for (int i = 0; i < n; i++)
                {
                    var item = mupdf.mupdf.pdf_array_get(contents, i);
                    if (mupdf.mupdf.pdf_is_stream(item) != 0)
                    {
                        var b = mupdf.mupdf.pdf_load_stream(item);
                        mupdf.mupdf.fz_append_buffer(res, b);
                    }
                }
                return res;
            }

            if (mupdf.mupdf.pdf_is_stream(contents) != 0)
                return mupdf.mupdf.pdf_load_stream(contents);

            return mupdf.mupdf.fz_new_buffer(16);
        }

        /// <summary>PyMuPDF <c>Page._count_q_balance</c> — count missing graphic state pushs and pops.</summary>
        /// <remarks>
        /// Returns a pair (push, pop). Push is the number of missing PDF "q" commands, pop is the number of "Q" commands.
        /// A balanced graphics state for the page will be reached if its /Contents is prepended with 'push' copies of string "q\n"
        /// and appended with 'pop' copies of "\nQ". (PyMuPDF docstring.)
        /// </remarks>
        private (int push, int pop) CountQBalance()
        {
            // page = _as_pdf_page(self)  # need the underlying PDF page  (implicit: NativePdfPage)
            var pdfPage = NativePdfPage;
            var res = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Resources")); // access /Resources
            var cont = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Contents")); // access /Contents
            var pdf = RequireParent().NativePdfDocument; // need underlying PDF document

            // return value of MuPDF function
            return pdf.pdf_count_q_balance(res, cont);
        }

        // ─── Get Drawings ───────────────────────────────────────────────
        /// <summary>
        /// Return the vector graphics of the page. These are instructions which draw lines, rectangles, quadruples or curves, including properties like colors, transparency, line width and dashing, etc. Alternative terms are "line art" and "drawings".
        /// </summary>
        /// <param name="extended">Include extended drawing metadata.</param>
        /// <returns>a list of dictionaries. Each dictionary item contains one or more single draw commands belonging together: they have the same properties (colors, dashing, etc.). This is called a "path" in PDF, so we adopted that name here, but the method works for all document types.</returns>
        public List<Dictionary<string, object>> GetDrawingsDict(bool extended = false)
        {
            var val = get_cdrawings(extended) ?? new List<Dictionary<string, object>>();
            return NormalizeGetDrawingsOutput(val);
        }

        /// <summary>Mutates <paramref name="val"/> in place (Python <c>get_drawings</c> post-loop on the list from <c>get_cdrawings</c>).</summary>
        private static List<Dictionary<string, object>> NormalizeGetDrawingsOutput(List<Dictionary<string, object>> val)
        {
            string[] allkeys =
            {
                "closePath",
                "fill",
                "color",
                "width",
                "lineCap",
                "lineJoin",
                "dashes",
                "stroke_opacity",
                "fill_opacity",
                "even_odd",
            };

            for (int i = 0; i < val.Count; i++)
            {
                var npath = val[i];
                if (!(npath.TryGetValue("type", out var typeObj) && typeObj is string typeStr))
                    continue;

                if (!typeStr.StartsWith("clip", StringComparison.Ordinal))
                {
                    if (npath.TryGetValue("rect", out var rv))
                    {
                        if (rv is Rect r0)
                            npath["rect"] = r0;
                        else if (rv is mupdf.FzRect fr)
                            npath["rect"] = new Rect(fr);
                        else if (Helpers.TryCoerceRect(rv, out var r1))
                            npath["rect"] = r1;
                    }
                }
                else
                {
                    if (npath.TryGetValue("scissor", out var sv))
                    {
                        if (sv is Rect s0)
                            npath["scissor"] = s0;
                        else if (sv is mupdf.FzRect sc)
                            npath["scissor"] = new Rect(sc);
                        else if (Helpers.TryCoerceRect(sv, out var s1))
                            npath["scissor"] = s1;
                    }
                }

                if (typeStr != "group" && npath.TryGetValue("items", out var itemsObj) && itemsObj is List<object> items)
                {
                    var newitems = new List<object>(items.Count);
                    foreach (var item in items)
                    {
                        if (item is not object[] oa || oa.Length == 0)
                        {
                            newitems.Add(item);
                            continue;
                        }
                        string cmd = (string)oa[0]!;
                        if (cmd == "re" && oa.Length >= 3)
                        {
                            Rect rNorm;
                            if (oa[1] is mupdf.FzRect rz)
                                rNorm = new Rect(rz).Normalize();
                            else if (oa[1] is Rect rr)
                                rNorm = rr.Normalize();
                            else if (Helpers.TryCoerceRect(oa[1], out var rc))
                                rNorm = rc.Normalize();
                            else
                            {
                                newitems.Add(item);
                                continue;
                            }
                            long orient = oa[2] switch
                            {
                                long l => l,
                                int ii => ii,
                                _ => Convert.ToInt64(oa[2], CultureInfo.InvariantCulture),
                            };
                            newitems.Add(new object[] { "re", rNorm, orient });
                        }
                        else if (cmd == "qu" && oa.Length >= 2)
                        {
                            if (oa[1] is mupdf.FzQuad q)
                                newitems.Add(new object[] { "qu", new Quad(q) });
                            else if (oa[1] is Quad qq)
                                newitems.Add(new object[] { "qu", qq });
                            else
                            {
                                newitems.Add(item);
                                continue;
                            }
                        }
                        else
                        {
                            var rest = new object[oa.Length];
                            rest[0] = cmd;
                            for (int j = 1; j < oa.Length; j++)
                            {
                                object oj = oa[j]!;
                                if (oj is Point p)
                                    rest[j] = new Point(p);
                                else if (oj is mupdf.FzPoint fp)
                                    rest[j] = Helpers.PointFromFz(fp);
                                else if (Helpers.TryCoercePoint(oj, out var pt))
                                    rest[j] = pt;
                                else
                                    rest[j] = oj;
                            }
                            newitems.Add(rest);
                        }
                    }
                    npath["items"] = newitems;
                }

                // Python: if npath['type'] in ('f', 's'):
                if (typeStr is "f" or "s")
                {
                    foreach (var k in allkeys)
                        npath[k] = npath.TryGetValue(k, out var v) ? v : null;
                }

                val[i] = npath;
            }

            return val;
        }
        /// <summary>
        /// See PyMuPDF Page.get_text_trace.
        /// </summary>
        public List<SpanInfo> GetTextTrace() => TextTraceDictToSpanInfo(GetTextTraceDict());

        /// <summary>PyMuPDF <c>Page.get_texttrace</c> as dictionaries.</summary>
        internal List<Dictionary<string, object>> GetTextTraceDict()
        {
            RequireParent();
            int old_rotation = Rotation;
            if (old_rotation != 0)
                SetRotation(0);
            var page = NativePage;
            var rc = new List<Dictionary<string, object>>();
            var dev = new JM_new_texttrace_device(rc);
            var prect = mupdf.mupdf.fz_bound_page(page);
            dev.ptm = new mupdf.FzMatrix(1, 0, 0, -1, 0, prect.y1);
            mupdf.mupdf.fz_run_page(page, dev, new mupdf.FzMatrix(), new mupdf.FzCookie());
            mupdf.mupdf.fz_close_device(dev);
            if (old_rotation != 0)
                SetRotation(old_rotation);
            return rc;
        }

        private static List<SpanInfo> TextTraceDictToSpanInfo(List<Dictionary<string, object>> rows)
        {
            var result = new List<SpanInfo>(rows.Count);
            foreach (var d in rows)
            {
                result.Add(new SpanInfo
                {
                    Dir = TextTraceDirToPoint(d),
                    Font = d.TryGetValue("font", out var f) ? f?.ToString() : null,
                    WMode = d.TryGetValue("wmode", out var wm) ? Convert.ToUInt32(wm) : 0u,
                    Flags = d.TryGetValue("flags", out var fl) ? Convert.ToSingle(fl) : 0f,
                    BidiLevel = d.TryGetValue("bidi_lvl", out var bl) ? Convert.ToUInt32(bl) : 0u,
                    BidiDir = d.TryGetValue("bidi_dir", out var bd) ? Convert.ToUInt32(bd) : 0u,
                    Ascender = d.TryGetValue("ascender", out var asc) ? Convert.ToSingle(asc) : 0f,
                    Descender = d.TryGetValue("descender", out var desc) ? Convert.ToSingle(desc) : 0f,
                    ColorSpace = d.TryGetValue("colorspace", out var csp) ? Convert.ToInt32(csp) : 0,
                    Color = d.TryGetValue("color", out var col) ? col as float[] : null,
                    Size = d.TryGetValue("size", out var sz) ? Convert.ToSingle(sz) : 0f,
                    Opacity = d.TryGetValue("opacity", out var op) ? Convert.ToSingle(op) : 0f,
                    LineWidth = d.TryGetValue("linewidth", out var lw) ? Convert.ToSingle(lw) : 0f,
                    SpaceWidth = d.TryGetValue("spacewidth", out var sw) ? Convert.ToSingle(sw) : 0f,
                    Type = d.TryGetValue("type", out var tp) ? Convert.ToInt32(tp) : 0,
                    Bbox = d.TryGetValue("bbox", out var bbox) ? bbox as Rect : null,
                    Layer = d.TryGetValue("layer", out var ly) ? ly?.ToString() : null,
                    SeqNo = d.TryGetValue("seqno", out var sn) ? Convert.ToInt32(sn) : 0,
                    CharsPy = TextTraceCharsToPyList(d.TryGetValue("chars", out var chObj) ? chObj : null),
                    Chars = chObj != null ? TextTraceCharsToList(chObj) : null,
                });
            }
            return result;
        }

        /// <summary>PyMuPDF <c>span["chars"]</c> list (tuple rows as <c>object[]</c>).</summary>
        private static List<object> TextTraceCharsToPyList(object charsObj)
        {
            if (charsObj == null)
                return null;
            if (charsObj is List<object> list)
                return list;
            if (charsObj is not System.Collections.IEnumerable rows)
                return null;
            var result = new List<object>();
            foreach (var entry in rows)
                result.Add(entry);
            return result;
        }

        private static Point TextTraceDirToPoint(Dictionary<string, object> span)
        {
            if (!span.TryGetValue("dir", out var dir))
                return null;
            if (dir is Point p)
                return p;
            if (dir is object[] a && a.Length >= 2)
                return new Point(Convert.ToSingle(a[0]), Convert.ToSingle(a[1]));
            return null;
        }

        /// <summary>
        /// Convert trace-device char rows to <see cref="Char"/> list.
        /// Device stores PyMuPDF tuples: (ucs, gid, origin, bbox).
        /// </summary>
        private static List<Char> TextTraceCharsToList(object charsObj)
        {
            if (charsObj == null)
                return null;
            if (charsObj is List<Char> typed)
                return typed;
            if (charsObj is not System.Collections.IEnumerable rows)
                return null;

            var result = new List<Char>();
            foreach (var entry in rows)
            {
                if (entry is Char ch)
                {
                    result.Add(ch);
                    continue;
                }
                if (entry is not object[] row || row.Length < 4)
                    continue;
                var origin = row[2] as object[];
                var bbox = row[3] as object[];
                result.Add(new Char
                {
                    UCS = Convert.ToInt32(row[0]),
                    GID = Convert.ToInt32(row[1]),
                    Origin = origin != null && origin.Length >= 2
                        ? new mupdf.FzPoint(Convert.ToSingle(origin[0]), Convert.ToSingle(origin[1]))
                        : default,
                    Bbox = bbox != null && bbox.Length >= 4
                        ? new mupdf.FzRect(
                            Convert.ToSingle(bbox[0]), Convert.ToSingle(bbox[1]),
                            Convert.ToSingle(bbox[2]), Convert.ToSingle(bbox[3]))
                        : default,
                });
            }
            return result;
        }
        /// <summary>
        /// Extract the vector graphics on the page. Apart from following technical differences, functionally equivalent to Page.get_drawings, but much faster:.
        /// </summary>
        public List<Dictionary<string, object>> GetCdrawings(bool extended = false) =>
            get_cdrawings(extended) ?? new List<Dictionary<string, object>>();

        /// <summary>
        /// Bounding-box log tuples (PyMuPDF <c>Page.get_bboxlog</c> raw rows).
        /// When <paramref name="includeLayerNames"/> is true, each tuple includes the optional content-group layer name (Python: truthy <c>layers</c> argument).
        /// </summary>
        public List<(string code, Rect bbox, string? layer)> GetBboxlogTuples(bool includeLayerNames = false)
        {
            int oldRot = Rotation;
            try
            {
                if (oldRot != 0)
                    SetRotation(0);
                var rc = new List<(string, Rect, string?)>();
                var dev = new PageBboxLogDevice(rc, includeLayerNames);
                try
                {
                    NativePage.fz_run_page(dev, new mupdf.FzMatrix(), new mupdf.FzCookie());
                    dev.fz_close_device();
                }
                finally
                {
                    dev.Dispose();
                }
                return rc;
            }
            finally
            {
                if (oldRot != 0)
                    SetRotation(oldRot);
            }
        }
        /// <summary>
        /// See PyMuPDF Page.get_bboxlog.
        /// </summary>
        public List<BoxLog> GetBboxlog(bool includeLayerNames = false) =>
            GetBboxlogTuples(includeLayerNames)
                .Select(t => new BoxLog(t.code, t.bbox, t.layer))
                .ToList();
        /// <summary>
        /// Cluster vector graphics (synonyms are line-art or drawings) based on their geometrical vicinity. The method walks through the output of Page.get_drawings and joins paths whose `path["rect"]` are closer to each other than some tolerance values (given in the arguments). The result is a list of rectangles that each wrap things like tables (with gridlines), pie charts, bar charts, etc.
        /// </summary>
        /// <param name="clip">only consider paths inside this area. The default is the full page.</param>
        /// <param name="drawings">(optional) provide a previously generated output of Page.get_drawings. If `None` the method will execute the method.</param>
        public List<Rect> ClusterDrawings(Rect? clip = null, List<Dictionary<string, object>> drawings = null,
            float xTolerance = 3, float yTolerance = 3, bool finalFilter = true)
        {
            // CheckParent(self)  # no-op in shipped PyMuPDF (early return in CheckParent).
            // parea = self.rect  # the default clipping area
            var parea = Rect;
            if (clip is not null)
                parea = clip;
            float deltaX = xTolerance; // shorter local name
            float deltaY = yTolerance;
            if (drawings == null)
                drawings = GetDrawingsDict();

            static bool AreNeighbors(Rect r1, Rect r2, float deltaX, float deltaY)
            {
                // Detect whether r1, r2 are "neighbors".
                // Items r1, r2 are called neighbors if the minimum distance between their points is less-equal delta.
                // Both parameters must be (potentially invalid) rectangles.
                // normalize rectangles as needed
                float rr1X0 = r1.X0, rr1X1 = r1.X1;
                if (rr1X1 < rr1X0) { rr1X0 = r1.X1; rr1X1 = r1.X0; }
                float rr1Y0 = r1.Y0, rr1Y1 = r1.Y1;
                if (rr1Y1 < rr1Y0) { rr1Y0 = r1.Y1; rr1Y1 = r1.Y0; }
                float rr2X0 = r2.X0, rr2X1 = r2.X1;
                if (rr2X1 < rr2X0) { rr2X0 = r2.X1; rr2X1 = r2.X0; }
                float rr2Y0 = r2.Y0, rr2Y1 = r2.Y1;
                if (rr2Y1 < rr2Y0) { rr2Y0 = r2.Y1; rr2Y1 = r2.Y0; }
                if (
                    rr1X1 < rr2X0 - deltaX
                    || rr1X0 > rr2X1 + deltaX
                    || rr1Y1 < rr2Y0 - deltaY
                    || rr1Y0 > rr2Y1 + deltaY)
                {
                    // Rects do not overlap.
                    return false;
                }

                // Rects overlap.
                return true;
            }

            // exclude graphics not contained in the clip
            var paths = new List<Rect>();
            foreach (var p in drawings)
            {
                if (!p.TryGetValue("rect", out var rv) || !Helpers.TryCoerceRect(rv, out var r))
                    continue;
                if (r.X0 >= parea.X0 && r.X1 <= parea.X1 && r.Y0 >= parea.Y0 && r.Y1 <= parea.Y1)
                    paths.Add(r);
            }

            // list of all vector graphic rectangles
            var prects = paths.OrderBy(r => r.Y1).ThenBy(r => r.X0).ToList();
            var newRects = new List<Rect>();

            // -------------------------------------------------------------------------
            // The strategy is to identify and join all rects that are neighbors
            // -------------------------------------------------------------------------
            while (prects.Count > 0) // the algorithm will empty this list
            {
                var r = new Rect(prects[0]); // r = +prects[0]  # copy of first rectangle
                bool repeat;
                do
                {
                    repeat = false;
                    for (int i = prects.Count - 1; i >= 1; i--) // from back to front
                    {
                        if (AreNeighbors(prects[i], r, deltaX, deltaY))
                        {
                            r.IncludePoint(prects[i].TL); // r |= prects[i].tl
                            r.IncludePoint(prects[i].BR); // r |= prects[i].br
                            prects.RemoveAt(i); // del prects[i]
                            repeat = true;
                        }
                    }
                } while (repeat);

                newRects.Add(r);
                prects.RemoveAt(0); // del prects[0]
                prects = prects.Distinct().OrderBy(x => x.Y1).ThenBy(x => x.X0).ToList(); // sorted(set(prects), key=lambda r: (r.y1, r.x0))
            }

            newRects = newRects.Distinct().OrderBy(r => r.Y1).ThenBy(r => r.X0).ToList(); // sorted(set(new_rects), key=lambda r: (r.y1, r.x0))
            if (!finalFilter)
                return newRects;
            return newRects.Where(r => r.Width > deltaX && r.Height > deltaY).ToList(); // [r for r in new_rects if r.width > delta_x and r.height > delta_y]
        }

        // ─── Labels ─────────────────────────────────────────────────────
        /// <summary>
        /// PDF only: Return the label for the page.
        /// </summary>
        /// <returns>the label string like "vii" for Roman numbering or "" if not defined.</returns>
        public string GetLabel()
        {
            // """Return the label for this PDF page.
            //
            // Args:
            //     page: page object.
            // Returns:
            //     The label (str) of the page. Errors return an empty string.
            // """
            // Jorj McKie, 2021-01-06

            // labels = page.parent._get_page_labels()
            var labels = RequireParent()._get_page_labels();
            // if not labels:
            if (labels == null || labels.Count == 0)
                // return ""
                return "";
            // labels.sort()
            labels.Sort();
            // return utils.get_label_pno(page.number, labels)
            return Utils.GetLabelPno(Number, labels);
        }
        /// <summary>
        /// PDF only: Change the colorspace components of all objects on page.
        /// </summary>
        /// <param name="components">The desired count of color components. Must be one of 1, 3 or 4, which results in color spaces DeviceGray, DeviceRGB or DeviceCMYK respectively. The method affects text, images and vector graphics. For instance, with the default value 1, a page will be converted to grayscale. If a page is already grayscale, the method will not cause visible changes; independent of the value of components.</param>
        public void Recolor(int components = 1)
        {
            if (components != 1 && components != 3 && components != 4)
                throw new ValueErrorException("components must be one of 1, 3, 4");
            var doc = RequireParent();
            if (!doc.IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);
            using var ropts = new mupdf.PdfRecolorOptions();
            ropts.num_comp = components;
            mupdf.mupdf.pdf_recolor_page(doc.NativePdfDocument, Number, ropts);
        }

        // ─── Tab ────────────────────────────────────────────────────────
        /// <summary>
        /// See PyMuPDF Page.get_page_annot_types.
        /// </summary>
        public Dictionary<string, object>[] GetPageAnnotTypes()
        {
            var result = new List<Dictionary<string, object>>();
            foreach (var annot in Annots())
            {
                var d = new Dictionary<string, object>
                {
                    ["type"] = annot.Type.ToString(),
                    ["xref"] = annot.Xref,
                    ["id"] = annot.GetInfo().TryGetValue("id", out var id) ? id : "",
                };
                result.Add(d);
            }
            return result.ToArray();
        }
        /// <summary>
        /// PDF only: return a list of the names of annotations, widgets and links. Technically, these are the */NM* values of every PDF object found in the page's */Annots* array.
        /// </summary>
        public List<string> AnnotNames()
        {
            var result = new List<string>();
            foreach (var annot in Annots())
            {
                var info = annot.GetInfo();
                if (info != null && info.TryGetValue("id", out var id) && !string.IsNullOrEmpty(id))
                    result.Add(id);
            }
            return result;
        }
        /// <summary>
        /// PDF only: return a list of the names of annotations, widgets and links. Technically, these are the */NM* values of every PDF object found in the page's */Annots* array.
        /// </summary>
        public List<string> GetAnnotNames() => AnnotNames();

        /// <summary>
        /// List of xref numbers of annotations, fields and links.
        /// </summary>
        /// <remarks>PyMuPDF <c>Page.annot_xrefs()</c> → <c>JM_get_annot_xref_list2</c>.</remarks>
        public List<(int xref, AnnotationType type, string id)> AnnotXrefs()
        {
            // return JM_get_annot_xref_list2(self)
            var pdfPage = NativePdfPage;
            if (pdfPage?.m_internal == null)
                return new List<(int xref, AnnotationType type, string id)>();
            var names = Helpers.JM_get_annot_xref_list(pdfPage.obj());
            var result = new List<(int xref, AnnotationType type, string id)>(names.Count);
            foreach (var (xref, type_, nm) in names)
                result.Add((xref, (AnnotationType)type_, nm));
            return result;
        }
        /// <summary>
        /// PDF only: return a list of the xref numbers of annotations, widgets and links; technically of all entries found in the page's */Annots* array.
        /// </summary>
        /// <returns>a list of items *(xref, type)* where type is the annotation type. Use the type to tell apart links, fields and annotations, see AnnotationTypes.</returns>
        public List<AnnotXref> GetAnnotXrefs()
        {
            var items = AnnotXrefs();
            var result = new List<AnnotXref>(items.Count);
            foreach (var (xref, type, id) in items)
                result.Add(new AnnotXref
                {
                    Xref = xref,
                    AnnotType = (PdfAnnotType)(int)type,
                    Id = id,
                });
            return result;
        }
        /// <summary>
        /// PDF only: return the annotation identified by *ident*. This may be its unique name (PDF `/NM` key), or its xref.
        /// </summary>
        /// <param name="ident">the annotation name or xref.</param>
        /// <returns>the annotation or None.</returns>
        /// <exception cref="ValueErrorException">Document or page is closed, or operation requires a PDF.</exception>
        public Annot LoadAnnot(object ident)
        {
            // CheckParent(self)
            RequireParent();
            int xref = 0;
            string name = null;
            if (ident is string s)
                name = s;
            else if (ident is int i)
                xref = i;
            else
                throw new ArgumentException("identifier must be a string or integer");

            // val = self._load_annot(name, xref)
            Annot val = _load_annot(name, xref);
            if (val == null)
                return null;
            // val.thisown = True
            // val.parent = weakref.proxy(self)
            // self._annot_refs[id(val)] = val
            RegisterAnnotRef(val);
            return val;
        }
        /// <summary>
        /// Return the first link on a page. Synonym of property first_link.
        /// </summary>
        /// <returns>first link on the page (or None).</returns>
        public Link LoadLinks()
        {
            var nativeLink = NativePage.fz_load_links();
            if (nativeLink.m_internal == null)
                return null;
            var val = new Link(nativeLink, this);
            if (RequireParent().IsPdf)
            {
                var linkAnnots = Helpers.JM_get_annot_xref_list(NativePdfPage.obj())
                    .FindAll(t => t.type_ == (int)mupdf.pdf_annot_type.PDF_ANNOT_LINK);
                if (linkAnnots.Count > 0)
                    val.SetLinkAnnotIdentity(linkAnnots[0].xref, linkAnnots[0].nm ?? "");
            }
            return val;
        }
        /// <summary>
        /// PDF only: return the field identified by xref.
        /// </summary>
        /// <param name="xref">the field's xref.</param>
        /// <returns>the field or None.</returns>
        public Widget LoadWidget(int xref)
        {
            foreach (var widget in Widgets())
            {
                if (widget.Xref == xref)
                    return widget;
            }
            return null;
        }
        /// <summary>
        /// PDF only: Add a PDF Form field ("widget") to a page. This also turns the PDF into a Form PDF. Because of the large amount of different options available for widgets, we have developed a new class Widget, which contains the possible PDF field attributes. It must be used for both, form field creation and updates.
        /// </summary>
        /// <param name="widget">Widget (form field) instance.</param>
        /// <returns>a widget annotation.</returns>
        /// <exception cref="ValueErrorException">Document or page is closed, or operation requires a PDF.</exception>
        public Annot AddWidget(Widget widget)
        {
            if (widget == null)
                throw new ArgumentException("bad type: widget");
            if (!RequireParent().IsPdf)
                throw new ValueErrorException("is no PDF");

            widget.Validate();
            var annot = _addWidget(widget.InsertFieldType, widget.InsertFieldName);
            if (annot == null)
                return null;
            widget.BindAnnot(annot.NativeAnnot, this, annot);
            widget.Update();
            annot_postprocess(annot);
            return annot;
        }
        /// <summary>
        /// PDF only: Delete field from the page and return the next one.
        /// </summary>
        /// <param name="widget">Widget (form field) instance.</param>
        /// <returns>the widget following the deleted one. Please remember that physical removal requires saving to a new file with garbage &gt; 0.</returns>
        /// <exception cref="ValueErrorException">Document or page is closed, or operation requires a PDF.</exception>
        public Widget DeleteWidget(Widget widget)
        {
            if (widget == null)
                throw new ArgumentException("bad type: widget");
            var nextWidget = widget.Next;
            if (widget.BoundAnnot != null)
                DeleteAnnot(widget.BoundAnnot);
            else if (widget.NativeWidget?.m_internal != null)
                mupdf.mupdf.pdf_delete_annot(NativePdfPage, widget.NativeWidget);
            else
            {
                var annot = LoadAnnot(widget.Xref);
                if (annot == null)
                    throw new ArgumentException("bad type: widget");
                DeleteAnnot(annot);
            }
            return nextWidget;
        }

        // ─── Bounding boxes ─────────────────────────────────────────────

        private Rect GetMediaBox()
        {
            try
            {
                var pdfPage = NativePdfPage;
                if (pdfPage?.m_internal != null)
                {
                    var r = Helpers.JmMediabox(pdfPage.obj());
                    return new Rect(r.x0, r.y0, r.x1, r.y1);
                }
            }
            catch { }
            return Rect;
        }

        private Rect GetCropBox()
        {
            try
            {
                var pdfPage = NativePdfPage;
                if (pdfPage?.m_internal != null)
                {
                    var r = Helpers.JmCropbox(pdfPage.obj());
                    return new Rect(r.x0, r.y0, r.x1, r.y1);
                }
            }
            catch { }
            return MediaBox;
        }

        private Rect GetSpecialBox(string name)
        {
            try
            {
                var pdfPage = NativePdfPage;
                var box = mupdf.mupdf.pdf_dict_gets(pdfPage.obj(), name);
                if (box.m_internal != null)
                {
                    var r = mupdf.mupdf.pdf_to_rect(box);
                    var mb = MediaBox;
                    return new Rect(r.x0, mb.Y1 - r.y1, r.x1, mb.Y1 - r.y0);
                }
            }
            catch { }
            return CropBox;
        }

        // ─── Run page ───────────────────────────────────────────────────
        /// <summary>
        /// See PyMuPDF Page.run.
        /// </summary>
        /// <param name="dev">MuPDF device receiving rendered page content.</param>
        /// <param name="transform">If true, return transformation matrices with image rectangles.</param>
        public void Run(mupdf.FzDevice dev, Matrix transform)
        {
            mupdf.mupdf.fz_run_page(NativePage, dev, transform.ToFzMatrix(), new mupdf.FzCookie());
        }

        // ─── Table Detection ─────────────────────────────────────────────
        /// <summary>
        /// Gets or sets See PyMuPDF Page.table_settings.
        /// </summary>
        public TableSettings TableSettings { get; set; }

        // ==============================================================================
        // Page.find_tables
        // ==============================================================================
        /// <summary>
        /// Find tables on the page and return an object with related information. Typically, the default values of the many parameters will be sufficient. Adjustments should ever only be needed in corner case situations.
        /// </summary>
        /// <param name="settings">Table finder settings object.</param>
        /// <param name="paths">list of vector graphics in the format as returned be Page.get_drawings. Using this parameter will prevent the method to extract vector graphics itself. This is useful if the vector graphics are already available. This can save execution time significantly.</param>
        /// <param name="addLines">Add vector lines when detecting tables.</param>
        /// <param name="addBoxes">Add vector boxes when detecting tables.</param>
        /// <returns>a `TableFinder` object that has the following significant attributes:</returns>
        public TableFinder FindTables(
            TableSettings settings = null,
            IList<Dictionary<string, object>> paths = null,
            IList<(Point p1, Point p2)> addLines = null,
            IList<Rect> addBoxes = null)
        {
            return TableHelpers.FindTables(
                this,
                settings,
                paths: paths,
                addLines: addLines,
                addBoxes: addBoxes);
        }

        // ─── IDisposable ────────────────────────────────────────────────
        /// <summary>
        /// Releases the native page and cached PDF page handles.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                TearDownFromParent();
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Port of the teardown portion of Python <c>Page._erase</c>: detach from the owning
        /// <see cref="Document"/> and drop the native <c>fz_page</c> handle.
        /// </summary>
        internal void TearDownFromParent()
        {
            if (_disposed)
                return;

            _reset_annot_refs();
            Parent?.ForgetPageRef(this);
            Parent = null;

            DisposeCachedPdfPage();
            lock (Utils.MuPDFLock)
            {
                _nativePage?.Dispose();
                _nativePage = null;
            }
            ThisOwn = false;
            _disposed = true;
        }

        ~Page() { Dispose(); }



        // ─── PyMuPDF API names (internal, same assembly) ─────────────────
        // Ported from Page.PythonCompat.cs: snake_case / dunder / legacy aliases.
        // Public callers should use the PascalCase members on this class.

        // Properties mirrored from Python naming.
        internal int number => Number;
        internal Rect rect => Rect;
        internal Rect bound() => Bound();

        /// <summary>Python <c>Page.this is None</c> after document close / <c>_erase</c>.</summary>
        internal bool py_this_is_none() => _nativePage == null;
        internal Rect mediabox() => MediaBox;
        internal Point mediabox_size() => MediaBoxSize;
        internal string language() => Language;
        internal object get_layout() => GetLayout();
        internal object layout_information
        {
            get => LayoutInformation;
            set => LayoutInformation = value;
        }
        internal Rect cropbox() => CropBox;
        internal Rect bleedbox() => BleedBox;
        internal Rect trimbox() => TrimBox;
        internal Rect artbox() => ArtBox;
        internal int rotation() => Rotation;
        internal Matrix transformation_matrix => TransformationMatrix;
        internal Matrix derotation_matrix => DerotationMatrix;
        internal int xref => Xref;
        internal Annot first_annot => FirstAnnot;
        internal Link first_link => FirstLink;
        internal Widget first_widget => FirstWidget;
        internal bool is_wrapped => IsWrapped;

        // Annotation and link generators.
        internal IEnumerable<Annot> annots() => Annots();
        internal IEnumerable<Annot> annots(params AnnotationType[] types) => Annots(types);
        /// <summary>PyMuPDF <c>Page.links()</c> — generator over link dicts from <see cref="GetLinks"/>.</summary>
        internal IEnumerable<Dictionary<string, object>> links(IEnumerable<int> kinds = null)
        {
            HashSet<int> filter = null;
            if (kinds != null)
            {
                foreach (var k in kinds)
                {
                    if (filter == null)
                        filter = new HashSet<int>();
                    filter.Add(k);
                }
            }
            foreach (var link in GetLinksDict())
            {
                if (filter == null
                    || (link.TryGetValue("kind", out var kindObj) && filter.Contains(Convert.ToInt32(kindObj))))
                {
                    yield return link;
                }
            }
        }
        internal IEnumerable<Widget> widgets(params WidgetType[] types) =>
            types == null || types.Length == 0 ? Widgets() : Widgets(types);

        // Annotation creation.
        internal Annot _add_freetext_annot(
            Rect rect,
            string text,
            float fontsize = 11,
            string fontname = null,
            float[] text_color = null,
            float[] fill_color = null,
            float[] border_color = null,
            float border_width = 0,
            int[] dashes = null,
            Point[] callout = null,
            int line_end = (int)mupdf.pdf_line_ending.PDF_ANNOT_LE_OPEN_ARROW,
            float opacity = 1,
            int align = 0,
            int rotate = 0,
            bool richtext = false,
            string style = null)
            => AddFreeTextAnnot(rect, text, fontSize: fontsize, fontName: fontname, textColor: text_color, fillColor: fill_color, borderColor: border_color, borderWidth: border_width, dashes: dashes, callout: callout, lineEnd: (PdfLineEnding)line_end, opacity: opacity, align: align, rotate: rotate, richtext: richtext, style: style);
        internal Annot _add_line_annot(Point p1, Point p2) => AddLineAnnot(p1, p2);
        internal Annot _add_multiline(Point[] points, object annot_type)
        {
            int type = annot_type is int i ? i : (int)(annot_type is AnnotationType at ? at : AnnotationType.PolyLine);
            if (type == (int)AnnotationType.Polygon) return AddPolygonAnnot(points);
            return AddPolylineAnnot(points);
        }
        internal Annot _add_redact_annot(Quad quad, string? text = null, string? da_str = null, int align = 0, float[]? fill = null, float[]? text_color = null)
            => AddRedactAnnotCore(quad, text, da_str, align, fill);
        internal Annot _add_redact_annot(Rect rect, string? text = null, string? da_str = null, int align = 0, float[]? fill = null, float[]? text_color = null)
            => AddRedactAnnotCore(new Quad(rect), text, da_str, align, fill);
        internal Annot _add_square_or_circle(Rect rect, object annot_type)
        {
            int type = annot_type is int i ? i : (int)(annot_type is AnnotationType at ? at : AnnotationType.Square);
            if (type == (int)AnnotationType.Circle) return AddCircleAnnot(rect);
            return AddRectAnnot(rect);
        }
        internal Annot _add_stamp_annot(Rect rect, object stamp = null)
        {
            if (stamp == null)
                return AddStampAnnot(rect, 0);
            if (stamp is int i)
                return AddStampAnnot(rect, i);
            if (stamp is long l)
                return AddStampAnnot(rect, (int)l);
            if (stamp is Pixmap pm)
                return AddStampAnnot(rect, pm);
            if (stamp is byte[] b)
                return AddStampAnnot(rect, b);
            if (stamp is string s)
                return AddStampAnnot(rect, s);
            throw new ArgumentException($"unsupported stamp type: {stamp.GetType().Name}");
        }
        internal Annot _add_text_annot(Point point, string text, string icon = null) => AddTextAnnot(point, text, icon ?? "Note");
        internal Annot _add_text_marker(object quads, object annot_type)
        {
            var type = annot_type switch
            {
                int i => (mupdf.pdf_annot_type)i,
                mupdf.pdf_annot_type t => t,
                AnnotationType at => (mupdf.pdf_annot_type)(int)at,
                _ => mupdf.pdf_annot_type.PDF_ANNOT_HIGHLIGHT,
            };
            return AddTextMarkerAnnot(type, quads, start: null, stop: null, clip: null);
        }
        internal void _addAnnot_FromString(object linklist)
        {
            int lcount = Helpers.PythonTupleLikeCount(linklist);
            if (lcount < 1)
                return;

            _ = _pdf_page();

            for (int i = 0; i < lcount; i++)
            {
                object txtpy = Helpers.PythonTupleLikeItem(linklist, i);
                string text = txtpy as string ?? txtpy?.ToString();
                if (string.IsNullOrEmpty(text))
                {
                    Console.WriteLine($"skipping bad link / annot item {i:d}.");
                    continue;
                }
                try
                {
                    Helpers.AppendPdfAnnotFromObjectString(this, text);
                }
                catch
                {
                    Console.WriteLine($"skipping bad link / annot item {i:d}.");
                }
            }

            var pdf = RequireParent().NativePdfDocument;
            Helpers.JM_refresh_links(pdf, NativePdfPage);
            SyncLinkWrapperCache();
        }
        internal Annot add_text_annot(Point point, string text, string icon = "Note") => AddTextAnnot(point, text, icon);
        /// <summary>
        /// PDF only: Add text in a given rectangle. Optionally, the appearance of a "callout" shape can be requested by specifying two or three point-like objects; see below.
        /// </summary>
        public Annot add_freetext_annot(Rect rect, string text, float fontsize = 12, string fontname = "helv",
            float[] text_color = null, float[] fill_color = null, float[] border_color = null, float border_width = 0,
            int[] dashes = null, Point[] callout = null, int line_end = (int)mupdf.pdf_line_ending.PDF_ANNOT_LE_OPEN_ARROW,
            float opacity = 1, int align = 0, int rotate = 0, bool richtext = false, string style = null)
            => AddFreeTextAnnot(rect, text, fontSize: fontsize, fontName: fontname, textColor: text_color,
                fillColor: fill_color, borderColor: border_color, borderWidth: border_width, dashes: dashes,
                callout: callout, lineEnd: (PdfLineEnding)line_end, opacity: opacity, align: align, rotate: rotate,
                richtext: richtext, style: style);
        internal Annot add_line_annot(Point p1, Point p2) => AddLineAnnot(p1, p2);
        internal Annot add_rect_annot(Rect rect) => AddRectAnnot(rect);
        internal Annot add_circle_annot(Rect rect) => AddCircleAnnot(rect);
        internal Annot add_polyline_annot(Point[] points) => AddPolylineAnnot(points);
        internal Annot add_polygon_annot(Point[] points) => AddPolygonAnnot(points);
        internal Annot add_highlight_annot(object quads = null, Point start = null, Point stop = null, IRect clip = null)
            => AddTextMarkerAnnot(mupdf.pdf_annot_type.PDF_ANNOT_HIGHLIGHT, quads, start, stop, clip);
        internal Annot add_underline_annot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null) => AddUnderlineAnnot(quads, start, stop, clip);
        internal Annot add_strikeout_annot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null) => AddStrikeoutAnnot(quads, start, stop, clip);
        internal Annot add_squiggly_annot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null) => AddSquigglyAnnot(quads, start, stop, clip);
        internal Annot add_stamp_annot(Rect rect, object stamp = null) => _add_stamp_annot(rect, stamp);
        /// <summary>
        /// , text_color=(0, 0, 0), cross_out=True).
        /// </summary>
        public Annot add_redact_annot(Quad quad, string text = null, string fontname = null, float fontsize = 11,
            int align = 0, float[] fill = null, float[] text_color = null, bool cross_out = true)
            => AddRedactAnnot(quad, text, fontName: fontname, fontSize: fontsize, align: align, fillColor: fill, textColor: text_color, crossOut: cross_out);
        /// <summary>
        /// , text_color=(0, 0, 0), cross_out=True).
        /// </summary>
        public Annot add_redact_annot(Rect rect, string text = null, string fontname = null, float fontsize = 11,
            int align = 0, float[] fill = null, float[] text_color = null, bool cross_out = true)
            => AddRedactAnnot(rect, text, fontName: fontname, fontSize: fontsize, align: align, fillColor: fill, textColor: text_color, crossOut: cross_out);

        // Annotation/link operations.
        internal bool apply_redactions(int images = 2, int graphics = 1, int text = 0) => ApplyRedactions(images, graphics, text);
        internal Annot delete_annot(Annot annot) => DeleteAnnot(annot);
        internal void delete_link(Link link) => DeleteLink(link);
        internal void delete_link(Dictionary<string, object> linkdict) => DeleteLink(linkdict);
        internal Annot add_widget(Widget widget) => AddWidget(widget);
        internal Widget delete_widget(Widget widget) => DeleteWidget(widget);
        internal Annot load_annot(string ident) => LoadAnnot(ident);
        internal Annot load_annot(int ident) => LoadAnnot(ident);
        internal Link load_links() => LoadLinks();
        internal Widget load_widget(int xref) => LoadWidget(xref);
        /// <summary>Python <c>insert_link</c> (returns <c>None</c>); prefer this for strict parity.</summary>
        internal void insert_link(Dictionary<string, object> lnk, bool mark = true) => InsertLinkVoid(lnk, mark);
        /// <summary>Convenience: same as <see cref="InsertLink"/> (returns first link after refresh).</summary>
        internal Link insert_link_returning_link(Dictionary<string, object> lnk, bool mark = true) => InsertLink(lnk, mark);
        internal void set_links(List<Dictionary<string, object>> links) => SetLinks(links);
        internal void update_link(Dictionary<string, object> lnk) => UpdateLink(lnk);
        internal List<Dictionary<string, object>> get_links() => GetLinksDict();

        // Rendering and text extraction.
        internal Pixmap get_pixmap(Matrix matrix = null, Colorspace cs = null, IRect clip = null, bool alpha = false, bool annots = true, float? dpi = null)
            => GetPixmap(matrix, cs, clip, alpha, annots, dpi);
        internal DisplayList get_displaylist(int annots = 1) => GetDisplayList(annots);
        internal string get_svg_image(Matrix matrix = null, int text_as_path = 1) => GetSvgImage(matrix, text_as_path);
        internal TextPage get_textpage(int flags = 0, IRect clip = null) => GetTextPage(flags, clip);
        internal object get_text(
            string option = "text",
            IRect clip = null,
            int? flags = null,
            TextPage textpage = null,
            bool sort = false,
            object delimiters = null,
            float tolerance = 3)
            => GetText(option, clip, flags, textpage, sort, delimiters, tolerance);
        internal string get_textbox(Rect rect, TextPage textpage = null) => GetTextbox(rect, textpage);
        internal string get_text_selection(Point p1, Point p2, IRect clip = null, TextPage textpage = null)
            => GetTextSelection(p1, p2, clip, textpage);
        internal TextPage get_textpage_ocr(int flags = 0, string language = "eng", int dpi = 72, bool full = false, string tessdata = null, ImageFilterPipeline imageFilters = null)
            => GetTextPageOcr(flags, language, dpi, full, tessdata, imageFilters);
        internal List<(float x0, float y0, float x1, float y1, string text, int blockNo, int blockType)> get_text_blocks(
            IRect clip = null, int? flags = null, TextPage textpage = null, bool sort = false)
            => GetTextBlocks(clip, flags, textpage, sort);
        internal List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> get_text_words(
            IRect clip = null, int? flags = null, TextPage textpage = null, bool sort = false, string delimiters = null, float tolerance = 3)
            => GetTextWords(clip, flags, textpage, sort, delimiters, tolerance);
        /// <summary>Python <c>Page.search_for</c> (default <c>quads=False</c>).</summary>
        internal List<Rect> search_for(string needle, Quad clip = null, int max_hits = 16, int flags = 0, TextPage textpage = null)
            => SearchForRects(needle, clip, max_hits, flags, textpage);

        internal List<Rect> search_for_rects(string needle, Quad clip = null, int max_hits = 16, int flags = 0, TextPage textpage = null)
            => search_for(needle, clip, max_hits, flags, textpage);

        // Insertion helpers.
        /// <summary>
        /// PDF only: Insert text lines starting at point_like point. See Shape.insert_text.
        /// </summary>
        public int insert_text(Point point, string text, float fontsize = 11, string fontname = "helv",
            float[] color = null, float rotate = 0, int render_mode = 0, float border_width = 0.05f,
            float? miter_limit = null, Point morphFix = null, Matrix morphMat = null)
            => InsertText(point, text, fontSize: fontsize, fontName: fontname, color: color, rotate: rotate, renderMode: render_mode, borderWidth: border_width,
                miterLimit: miter_limit.HasValue ? (float?)miter_limit.Value : null,
                morphFix: morphFix, morphMat: morphMat);
        /// <summary>PyMuPDF <c>Page.insert_textbox</c> (snake_case parameters).</summary>
        public InsertTextboxResult insert_textbox(
            Rect rect,
            string text,
            float fontsize = 11,
            string fontname = "helv",
            float[] color = null,
            int align = 0,
            float border_width = 0.05f,
            float expandtabs = 1,
            int render_mode = 0,
            int rotate = 0,
            int encoding = 0,
            float fill_opacity = 1,
            float[] fill = null,
            string fontfile = null,
            float? lineheight = null,
            int set_simple = 0,
            Point morph_fix = null,
            Matrix morph_mat = null,
            bool overlay = true,
            float stroke_opacity = 1,
            int oc = 0)
            => InsertTextbox(
                rect,
                text,
                align: align,
                borderWidth: border_width,
                color: color,
                encoding: encoding,
                expandTabs: expandtabs,
                fillOpacity: fill_opacity,
                fill: fill,
                fontFile: fontfile,
                fontName: fontname,
                fontSize: fontsize,
                lineHeight: lineheight,
                morphFix: morph_fix,
                morphMat: morph_mat,
                oc: oc,
                overlay: overlay,
                renderMode: render_mode,
                rotate: rotate,
                setSimple: set_simple,
                strokeOpacity: stroke_opacity);
        internal (float spare_height, float scale) insert_htmlbox(Rect rect, string text, string css = null, float scale_low = 0,
            Archive archive = null, int rotate = 0, int oc = 0, float opacity = 1, bool overlay = true,
            bool scale_word_width = true, bool verbose = false)
            => InsertHtmlbox(rect, text, css, scale_low, archive, rotate, oc, opacity, overlay, scale_word_width, verbose);
        internal int show_pdf_page(Rect rect, Document docsrc, int pno = 0, bool keep_proportion = true, bool overlay = true, int oc = 0, int rotate = 0, Rect clip = null)
            => ShowPdfPage(rect, docsrc, pno, keep_proportion, overlay, oc, rotate, clip);
        internal void _set_resource_property(string name, int xref)
            => Helpers.JM_set_resource_property(NativePdfPage.obj(), name, xref);
        internal int _show_pdf_page(object fz_srcpage, int overlay = 1, Matrix matrix = null, int xref = 0, int oc = 0, Rect clip = null, Graftmap graftmap = null, string _imgname = null)
        {
            var srcPdfPage = Helpers.AsPdfPage(fz_srcpage);
            if (clip == null)
            {
                var srcBBox = new mupdf.FzRect();
                var srcCtm = new mupdf.FzMatrix();
                srcPdfPage.obj().pdf_page_obj_transform(srcBBox, srcCtm);
                clip = Helpers.RectFromFz(srcBBox);
            }
            return ShowPdfPageInternal(
                srcPdfPage,
                overlay: overlay != 0,
                matrix: matrix ?? Matrix.Identity,
                xref: xref,
                oc: oc,
                clip: clip,
                graftmap: graftmap?.NativeGraftMap,
                imgName: _imgname ?? MakeShowPdfResourceName(),
                pdfOut: Parent.NativePdfDocument,
                targetPageObj: NativePdfPage.obj());
        }
        internal mupdf.PdfPage _pdf_page(bool required = true)
        {
            var pdfPage = NativePdfPage;
            if (required && pdfPage.m_internal == null)
                throw new InvalidOperationException(Constants.MSG_IS_NO_PDF);
            return pdfPage;
        }
        internal (int push, int pop) _count_q_balance()
            => CountQBalance();
        internal List<(string name, int xref)> _get_resource_properties()
            => GetResourceProperties();
        internal string _get_optional_content(int? oc)
        {
            if (!oc.HasValue || oc.Value == 0)
                return null;
            int xref = oc.Value;
            string check = Parent.XrefObject(xref, compressed: true);
            if (check.IndexOf("/Type/OCG", StringComparison.Ordinal) < 0
                && check.IndexOf("/Type/OCMD", StringComparison.Ordinal) < 0)
                throw new ValueErrorException("bad optional content: 'oc'");

            var propsByXref = new Dictionary<int, string>();
            foreach (var p in _get_resource_properties())
                propsByXref[p.xref] = p.name;
            if (propsByXref.TryGetValue(xref, out string existing))
                return existing;

            int i = 0;
            string mc = $"MC{i}";
            var usedNames = new HashSet<string>(propsByXref.Values);
            while (usedNames.Contains(mc))
            {
                i++;
                mc = $"MC{i}";
            }
            _set_resource_property(mc, xref);
            return mc;
        }
        /// <summary>PyMuPDF <c>Page._set_pagebox</c>.</summary>
        internal void _set_pagebox(string boxtype, Rect rect)
        {
            var doc = Parent;
            if (doc == null)
                throw new ValueErrorException("orphaned object: parent is None");

            if (!doc.IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);

            if (boxtype != "CropBox" && boxtype != "BleedBox" && boxtype != "TrimBox" && boxtype != "ArtBox")
                throw new ValueErrorException("bad boxtype");

            rect = new Rect(rect);
            Rect mb = MediaBox;
            rect = new Rect(rect.X0, mb.Y1 - rect.Y1, rect.X1, mb.Y1 - rect.Y0);
            if (!(mb.X0 <= rect.X0 && rect.X0 < rect.X1 && rect.X1 <= mb.X1
                && mb.Y0 <= rect.Y0 && rect.Y0 < rect.Y1 && rect.Y1 <= mb.Y1))
                throw new ValueErrorException($"{boxtype} not in MediaBox");

            doc.XrefSetKey(xref, boxtype, $"[{Helpers.FormatPdfReals(rect.X0, rect.Y0, rect.X1, rect.Y1)}]");
        }
        internal Rect _other_box(string boxtype)
        {
            switch (boxtype)
            {
                case "BleedBox":
                    return BleedBox;
                case "TrimBox":
                    return TrimBox;
                case "ArtBox":
                    return ArtBox;
                default:
                    return null;
            }
        }
        internal void _reset_annot_refs() => ResetAnnotRefsInternal();
        internal void _erase()
            => TearDownFromParent();
        internal mupdf.FzStextPage _get_textpage(Rect clip = null, int flags = 0, Matrix matrix = null)
            => CreateStextPage(clip, flags, matrix ?? Matrix.Identity);
        internal string _set_opacity(string gstate = null, float CA = 1, float ca = 1, string blendmode = null)
        {
            if (CA >= 1 && ca >= 1 && blendmode == null)
                return null;
            int tCA = (int)Math.Round(Math.Max(CA, 0) * 100);
            if (tCA >= 100) tCA = 99;
            int tca = (int)Math.Round(Math.Max(ca, 0) * 100);
            if (tca >= 100) tca = 99;
            gstate = $"fitzca{tCA:00}{tca:00}";

            var page = _pdf_page();
            var resources = mupdf.mupdf.pdf_dict_get(page.obj(), mupdf.mupdf.pdf_new_name("Resources"));
            if (resources.m_internal == null)
                resources = mupdf.mupdf.pdf_dict_put_dict(page.obj(), mupdf.mupdf.pdf_new_name("Resources"), 2);
            var extg = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("ExtGState"));
            if (extg.m_internal == null)
                extg = mupdf.mupdf.pdf_dict_put_dict(resources, mupdf.mupdf.pdf_new_name("ExtGState"), 2);
            int n = mupdf.mupdf.pdf_dict_len(extg);
            for (int i = 0; i < n; i++)
            {
                var key = mupdf.mupdf.pdf_dict_get_key(extg, i);
                string name = mupdf.mupdf.pdf_to_name(key);
                if (name == gstate)
                    return gstate;
            }
            var opa = mupdf.mupdf.pdf_new_dict(page.doc(), 3);
            mupdf.mupdf.pdf_dict_put_real(opa, mupdf.mupdf.pdf_new_name("CA"), (float)CA);
            mupdf.mupdf.pdf_dict_put_real(opa, mupdf.mupdf.pdf_new_name("ca"), (float)ca);
            if (!string.IsNullOrEmpty(blendmode))
                mupdf.mupdf.pdf_dict_put_name(opa, mupdf.mupdf.pdf_new_name("BM"), blendmode);
            mupdf.mupdf.pdf_dict_puts(extg, gstate, opa);
            return gstate;
        }
        internal bool _apply_redactions(int text, int images, int graphics)
            => ApplyRedactionsPdfOnly(text, images, graphics);
        internal Annot _load_annot(string name, int xref = 0)
        {
            // page = self._pdf_page()
            var page = NativePdfPage;
            mupdf.PdfAnnot annot;
            if (xref == 0)
            {
                // annot = JM_get_annot_by_name(page, name)
                annot = Helpers.JmGetAnnotByName(page, name);
            }
            else
            {
                // annot = JM_get_annot_by_xref(page, xref)
                annot = Helpers.JmGetAnnotByXref(page, xref);
            }
            if (annot.m_internal == null)
                return null;
            // return Annot(annot)
            return new Annot(annot, this);
        }
        internal Pixmap _makePixmap(object doc, Matrix ctm, Colorspace cs, int alpha = 0, int annots = 1, IRect clip = null)
            => GetPixmap(ctm ?? Matrix.Identity, cs ?? Colorspace.Rgb, clip, alpha != 0, annots != 0);
        /// <summary>
        /// Low-level image insertion used by <see cref="InsertImage(Rect, string, byte[], Pixmap, byte[], int, int, int, bool, int, string)"/> (PyMuPDF <c>Page._insert_image</c>).
        /// </summary>
        /// <param name="filename">Image file path, or null if using <paramref name="stream"/> or <paramref name="pixmap"/>.</param>
        /// <param name="pixmap">Source pixmap, or null.</param>
        /// <param name="stream">Encoded image bytes, or null.</param>
        /// <param name="imask">Optional soft mask bytes (stream/filename path); combined into digest and may require a compressed image buffer.</param>
        /// <param name="clip">Target rectangle in the coordinate space expected by <see cref="Helpers.CalcImageMatrix"/> (typically <c>Rect(rect) * ~TransformationMatrix</c> from <c>insert_image</c>).</param>
        /// <param name="overlay">Non-zero to append the drawing commands (foreground); zero to prepend (background). Matches PyMuPDF <c>overlay</c> as 1/0.</param>
        /// <param name="rotate">Rotation in degrees (0, 90, 180, 270).</param>
        /// <param name="keep_proportion">1 to preserve aspect ratio inside <paramref name="clip"/>; 0 otherwise.</param>
        /// <param name="oc">Optional-content xref (<c>OCG</c>/<c>OCMD</c>), or 0.</param>
        /// <param name="width">Initial width hint (usually 0; overridden when loading from pixmap/stream).</param>
        /// <param name="height">Initial height hint (usually 0; overridden when loading from pixmap/stream).</param>
        /// <param name="xref">If non-zero, reference this existing image and skip pixmap/stream processing.</param>
        /// <param name="alpha">Reserved for PyMuPDF parity (stream/filename transparency hint).</param>
        /// <param name="_imgname">PDF resource name for the form XObject; if null or empty, a unique <c>fzImg*</c> name is allocated.</param>
        /// <param name="digests">Map of MD5 hex key to image xref for reuse; defaults to <see cref="Document.InsertedImages"/>.</param>
        /// <returns>
        /// The image xref and, when a new digest entry was added, the same <paramref name="digests"/> dictionary reference; otherwise <c>null</c> for the second value (PyMuPDF returns <c>None</c> when no new digest).
        /// </returns>
        /// <remarks>
        /// Implements pixmap/stream digest lookup, pixmap alpha split, compressed-buffer + mask path, <c>pdf_add_image</c>, <c>JM_add_oc_object</c>, inheritable <c>/Resources</c>/<c>/XObject</c>,
        /// and <c>JM_insert_contents</c> with the matrix from <see cref="Helpers.CalcImageMatrix"/>. Does not call <see cref="WrapContents"/>; callers that need a balanced graphics state (overlay foreground) must do so first, as <c>insert_image</c> does.
        /// </remarks>
        internal (int xref, Dictionary<string, object>? digests) _insert_image(
            string filename = null,
            Pixmap pixmap = null,
            byte[] stream = null,
            byte[] imask = null,
            Rect clip = null,
            int overlay = 1,
            int rotate = 0,
            int keep_proportion = 1,
            int oc = 0,
            int width = 0,
            int height = 0,
            int xref = 0,
            int alpha = -1,
            string _imgname = null,
            Dictionary<string, object> digests = null)
        {
            _ = alpha;
            var doc = RequireParent();
            var d = digests ?? doc.InsertedImages;
            string name = string.IsNullOrEmpty(_imgname) ? AllocateFzImgName() : _imgname;
            Rect clipRect = clip ?? Rect;
            bool overlayB = overlay != 0;
            bool keepProportion = keep_proportion != 0;

            var pdf = doc.NativePdfDocument;
            var pdfPage = NativePdfPage;
            var pageObj = pdfPage.obj();

            int w = width;
            int h = height;
            int imgXref = xref;
            bool rcDigest = false;
            string? md5Key = null;

            bool doProcessPixmap = xref == 0 && pixmap != null;
            bool doProcessStream = xref == 0 && ((stream != null && stream.Length > 0) || !string.IsNullOrEmpty(filename));
            bool doHaveImask = true;
            bool doHaveImage = true;
            bool doHaveXref = true;

            mupdf.FzImage? image = null;
            mupdf.PdfObj refObj = new mupdf.PdfObj();
            mupdf.FzBuffer? imgbuf = null;

            if (xref > 0)
            {
                refObj = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
                var wobj = refObj.pdf_dict_geta(mupdf.mupdf.pdf_new_name("Width"), mupdf.mupdf.pdf_new_name("W"));
                var hobj = refObj.pdf_dict_geta(mupdf.mupdf.pdf_new_name("Height"), mupdf.mupdf.pdf_new_name("H"));
                w = wobj.m_internal != null ? mupdf.mupdf.pdf_to_int(wobj) : 0;
                h = hobj.m_internal != null ? mupdf.mupdf.pdf_to_int(hobj) : 0;
                if (w + h == 0)
                    throw new ValueErrorException("is no image");
                doProcessPixmap = false;
                doProcessStream = false;
                doHaveImask = false;
                doHaveImage = false;
            }
            else
            {
                if (stream != null && stream.Length > 0)
                    imgbuf = Helpers.BufferFromBytes(stream);
                else if (!string.IsNullOrEmpty(filename))
                    imgbuf = mupdf.mupdf.fz_read_file(filename);
                if (imgbuf != null)
                    doProcessPixmap = false;
            }

            if (doProcessPixmap && pixmap != null)
            {
                var argPix = pixmap.NativePixmap;
                w = argPix.fz_pixmap_width();
                h = argPix.fz_pixmap_height();
                md5Key = Helpers.Md5HexKeyFromPixmap(argPix);
                if (d != null && md5Key != null && d.TryGetValue(md5Key, out var existing) && existing is int ex)
                {
                    imgXref = ex;
                    refObj = mupdf.mupdf.pdf_new_indirect(pdf, imgXref, 0);
                    var wobj = refObj.pdf_dict_geta(mupdf.mupdf.pdf_new_name("Width"), mupdf.mupdf.pdf_new_name("W"));
                    var hobj = refObj.pdf_dict_geta(mupdf.mupdf.pdf_new_name("Height"), mupdf.mupdf.pdf_new_name("H"));
                    w = wobj.m_internal != null ? mupdf.mupdf.pdf_to_int(wobj) : w;
                    h = hobj.m_internal != null ? mupdf.mupdf.pdf_to_int(hobj) : h;
                    doProcessStream = false;
                    doHaveImask = false;
                    doHaveImage = false;
                }
                else
                {
                    if (argPix.fz_pixmap_alpha() == 0)
                        image = mupdf.mupdf.fz_new_image_from_pixmap(argPix, new mupdf.FzImage());
                    else
                    {
                        using var pm = argPix.fz_convert_pixmap(
                            new mupdf.FzColorspace(),
                            new mupdf.FzColorspace(),
                            new mupdf.FzDefaultColorspaces(),
                            new mupdf.FzColorParams(),
                            1);
                        pm.m_internal.alpha = 0;
                        pm.m_internal.colorspace = null;
                        var maskIm = mupdf.mupdf.fz_new_image_from_pixmap(pm, new mupdf.FzImage());
                        image = mupdf.mupdf.fz_new_image_from_pixmap(argPix, maskIm);
                    }
                    doProcessStream = false;
                    doHaveImask = false;
                }
            }

            if (doProcessStream && imgbuf != null)
            {
                byte[] mainBytes = Helpers.BufferToBytes(imgbuf);
                byte[]? maskBytes = (imask != null && imask.Length > 0) ? imask : null;
                md5Key = Helpers.Md5HexKey(mainBytes, maskBytes);
                if (d != null && d.TryGetValue(md5Key, out var exo) && exo is int ex2)
                {
                    imgXref = ex2;
                    refObj = mupdf.mupdf.pdf_new_indirect(pdf, imgXref, 0);
                    var wobj = refObj.pdf_dict_geta(mupdf.mupdf.pdf_new_name("Width"), mupdf.mupdf.pdf_new_name("W"));
                    var hobj = refObj.pdf_dict_geta(mupdf.mupdf.pdf_new_name("Height"), mupdf.mupdf.pdf_new_name("H"));
                    w = wobj.m_internal != null ? mupdf.mupdf.pdf_to_int(wobj) : w;
                    h = hobj.m_internal != null ? mupdf.mupdf.pdf_to_int(hobj) : h;
                    doHaveImask = false;
                    doHaveImage = false;
                }
                else
                {
                    image = mupdf.mupdf.fz_new_image_from_buffer(imgbuf);
                    w = image.w();
                    h = image.h();
                    if (imask == null || imask.Length == 0)
                        doHaveImask = false;
                }
            }

            if (doHaveImask && image != null)
            {
                var cbuf1 = image.fz_compressed_image_buffer();
                if (cbuf1 == null || cbuf1.m_internal == null)
                    throw new ValueErrorException("uncompressed image cannot have mask");
                int bpc = (int)image.bpc();
                var colorspace = image.colorspace();
                if (colorspace == null || colorspace.m_internal == null)
                    colorspace = new mupdf.FzColorspace();
                var (xres, yres) = image.fz_image_resolution();
                var maskBuf = Helpers.BufferFromBytes(imask!);
                var maskIm = mupdf.mupdf.fz_new_image_from_buffer(maskBuf);
                var decode = new mupdf.vectorf();
                var colorkey = new mupdf.vectori();
                image = mupdf.mupdf.fz_new_image_from_compressed_buffer2(
                    w,
                    h,
                    bpc,
                    colorspace,
                    xres,
                    yres,
                    1,
                    0,
                    decode,
                    colorkey,
                    cbuf1,
                    maskIm);
            }

            if (doHaveImage && image != null)
            {
                refObj = mupdf.mupdf.pdf_add_image(pdf, image);
                if (oc != 0)
                    Helpers.JM_add_oc_object(pdf, refObj, oc);
                imgXref = mupdf.mupdf.pdf_to_num(refObj);
                if (d != null && md5Key != null)
                    d[md5Key] = imgXref;
                rcDigest = true;
            }

            if (doHaveXref)
            {
                var resources = mupdf.mupdf.pdf_dict_get_inheritable(pageObj, mupdf.mupdf.pdf_new_name("Resources"));
                if (resources.m_internal == null)
                    resources = mupdf.mupdf.pdf_dict_put_dict(pageObj, mupdf.mupdf.pdf_new_name("Resources"), 2);
                var xobject = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("XObject"));
                if (xobject.m_internal == null)
                    xobject = mupdf.mupdf.pdf_dict_put_dict(resources, mupdf.mupdf.pdf_new_name("XObject"), 2);
                refObj = mupdf.mupdf.pdf_new_indirect(pdf, imgXref, 0);
                mupdf.mupdf.pdf_dict_puts(xobject, name, refObj);
                Matrix mat = Helpers.CalcImageMatrix(w, h, clipRect, rotate, keepProportion);
                string g = Helpers.FormatPdfReals(mat.A, mat.B, mat.C, mat.D, mat.E, mat.F);
                var nres = mupdf.mupdf.fz_new_buffer(64);
                nres.fz_append_string($"\nq\n{g} cm\n/{name} Do\nQ\n");
                Helpers.JM_insert_contents(pdf, pageObj, nres, overlayB);
            }

            return (imgXref, rcDigest ? d : null);
        }
        /// <summary>Port of PyMuPDF <c>Page._addWidget</c> (<c>JM_create_widget</c>).</summary>
        internal Annot _addWidget(object field_type, string field_name)
        {
            WidgetType wt;
            if (field_type is WidgetType wte)
                wt = wte;
            else if (field_type is int wi && Enum.IsDefined(typeof(WidgetType), wi))
                wt = (WidgetType)wi;
            else
                throw new ArgumentException("bad field_type");

            var pdfPage = _pdf_page();
            var pdf = RequireParent().NativePdfDocument;
            var nativeAnnot = Helpers.JmCreateWidget(pdf, pdfPage, wt, field_name);
            if (nativeAnnot?.m_internal == null)
                throw new InvalidOperationException("cannot create widget");
            Helpers.JM_add_annot_id(nativeAnnot, "W");
            return new Annot(nativeAnnot, this);
        }

        // Resources.
        internal List<(int xref, string ext, string type, string baseName, string name, string encoding, int? referencer)> get_fonts(bool full = false) => GetFonts(full);
        internal List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)> get_images(bool full = false) => GetImageRows(full);
        internal Rect get_image_bbox(object name) => GetImageBbox(name);
        internal (Rect bbox, Matrix transform) get_image_bbox(object name, bool transform) => GetImageBbox(name, transform);
        internal List<Dictionary<string, object>> get_image_info(bool hashes = false, bool xrefs = false) => GetImageInfoDict(hashes, xrefs);
        internal List<Rect> get_image_rects(object name) => GetImageRects(name);
        internal List<(Rect bbox, Matrix transform)> get_image_rects(object name, bool transform) => GetImageRects(name, transform);
        internal List<Dictionary<string, object>> get_xobjects() => GetXobjects();
        /// <summary>Python-style alias for <see cref="InsertImage(Rect, string, byte[], Pixmap, byte[], int, int, int, bool, int, string)"/>.</summary>
        internal int insert_image(Rect rect, string filename = null, byte[] stream = null, Pixmap pixmap = null,
            byte[] mask = null, int rotate = 0, int xref = 0, int oc = 0, bool keep_proportion = true,
            int alpha = -1, string overlay = "true")
            => InsertImage(rect, filename, stream, pixmap, mask, rotate, xref, oc, keep_proportion, alpha, overlay);
        internal Matrix rotation_matrix() => RotationMatrix;
        internal void refresh() => Refresh();
        internal void replace_image(int xref, string filename = null, Pixmap pixmap = null, byte[] stream = null)
            => ReplaceImage(xref, filename, pixmap, stream);

        // Geometry / boxes.
        internal void set_rotation(int rotation) => SetRotation(rotation);
        internal void set_language(string language = null) => SetLanguage(language);
        internal void set_mediabox(Rect rect) => SetMediaBox(rect);
        internal void set_cropbox(Rect rect) => SetCropBox(rect);
        internal void set_bleedbox(Rect rect) => SetBleedBox(rect);
        internal void set_trimbox(Rect rect) => SetTrimBox(rect);
        internal void set_artbox(Rect rect) => SetArtBox(rect);
        internal Point cropbox_position() => CropBoxPosition;

        // Drawing and content streams.
        internal Shape new_shape() => NewShape();
        internal void clean_contents(int sanitize = 1) => CleanContents(sanitize);
        internal byte[] read_contents() => ReadContents();
        internal void set_contents(int xref) => SetContents(xref);
        internal List<int> get_contents() => GetContents();
        internal void wrap_contents() => WrapContents();
        internal Matrix remove_rotation() => RemoveRotation();
        internal List<Dictionary<string, object>> get_drawings(bool extended = false) => GetDrawingsDict(extended);
        /// <summary>Extract vector graphics ("line art") from the page.</summary>
        internal List<Dictionary<string, object>>? get_cdrawings(bool extended = false, object? callback = null, object? method = null)
        {
            int oldRotation = Rotation;
            if (oldRotation != 0)
                SetRotation(0);
            var page = NativePage;
            bool clips = extended;
            JM_new_lineart_device_Device dev;
            List<Dictionary<string, object>>? rc = null;
            if (callback is Delegate || (method != null && method is not string))
            {
                dev = new JM_new_lineart_device_Device(callback!, clips, method);
            }
            else if (method is string methodName && methodName.Length > 0)
            {
                dev = new JM_new_lineart_device_Device(callback ?? this, clips, method);
            }
            else
            {
                rc = new List<Dictionary<string, object>>();
                dev = new JM_new_lineart_device_Device(rc, clips, method);
            }
            var prect = mupdf.mupdf.fz_bound_page(page);
            dev.ptm = new mupdf.FzMatrix(1, 0, 0, -1, 0, prect.y1);
            using (dev)
            {
                mupdf.mupdf.fz_run_page(page, dev, new mupdf.FzMatrix(), new mupdf.FzCookie());
                mupdf.mupdf.fz_close_device(dev);
            }
            if (oldRotation != 0)
                SetRotation(oldRotation);
            if (callback is Delegate || (method != null && (method is not string || ((string)method).Length > 0)))
                return null;
            return rc;
        }
        internal List<Dictionary<string, object>> get_texttrace() => GetTextTraceDict();
        internal List<(string code, Rect bbox, string? layer)> get_bboxlog(object layers = null) =>
            GetBboxlogTuples(PythonLayersTruthy(layers));
        internal List<Rect> cluster_drawings(Rect? clip = null, List<Dictionary<string, object>> drawings = null,
            float x_tolerance = 3, float y_tolerance = 3, bool final_filter = true) =>
            ClusterDrawings(clip, drawings, x_tolerance, y_tolerance, final_filter);
        internal void extend_textpage(TextPage tpage, int flags = 0, Matrix matrix = null) => ExtendTextPage(tpage, flags, matrix);
        internal void delete_image(int xref) => DeleteImage(xref);
        internal void recolor(int components = 1) => Recolor(components);
        internal string get_label() => GetLabel();

        private static bool PythonLayersTruthy(object layers)
        {
            if (layers == null)
                return false;
            if (layers is bool b)
                return b;
            if (layers is string s)
                return !string.IsNullOrEmpty(s);
            if (layers is System.Collections.ICollection c)
                return c.Count > 0;
            return true;
        }

        // Annotation metadata helpers.
        internal List<string> annot_names() => AnnotNames();
        internal List<(int xref, AnnotationType type, string id)> annot_xrefs() => AnnotXrefs();

        // Tables / execution.
        internal TableFinder find_tables(
            TableSettings settings = null,
            IList<Dictionary<string, object>> paths = null,
            IList<(Point p1, Point p2)> addLines = null,
            IList<Rect> addBoxes = null)
            => FindTables(settings, paths, addLines, addBoxes); // return table.find_tables(self, **kwargs)
        internal void run(mupdf.FzDevice dev, Matrix transform) => Run(dev, transform);
        internal void clip_to_rect(Rect rect) => ClipToRect(rect);
        internal List<(string name, int xref, string type)> get_oc_items() => GetOcItems();
        internal void write_text(Rect rect = null, IEnumerable<TextWriter> writers = null, bool overlay = true,
            float[] color = null, float? opacity = null, bool keep_proportion = true, int rotate = 0, int oc = 0)
            => WriteText(rect, writers, overlay, color, opacity, keep_proportion, rotate, oc);

        internal Point draw_line(Point p1, Point p2, float[] color = null, float width = 1, string line_cap = null, string line_join = null, float[] dashes = null, float opacity = 1, string blend_mode = null, int overlay = 1, string morph = null, int oc = 0)
            => DrawLine(p1, p2, color, Shape.DashesArrayToPdfString(dashes), width,
                Shape.ParseLineCapForPdf(line_cap), Shape.ParseLineJoinForPdf(line_join),
                overlay != 0, strokeOpacity: opacity, fillOpacity: opacity, oc: oc);
        internal Point draw_rect(
            Rect rect,
            float[] color = null,
            float[] fill = null,
            string dashes = null,
            float width = 1,
            int line_cap = 0,
            int line_join = 0,
            Point morph = null,
            Matrix morph_mat = null,
            int overlay = 1,
            float stroke_opacity = 1,
            float fill_opacity = 1,
            int oc = 0,
            object radius = null)
            => DrawRect(rect, color, fill, dashes, width, line_cap, line_join, morph, morph_mat, overlay != 0, stroke_opacity, fill_opacity, oc, radius);
        internal Point draw_circle(Point center, float radius, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawCircle(center, radius, color, fill, width: width, overlay: overlay != 0, strokeOpacity: opacity, fillOpacity: opacity);
        internal Point draw_oval(Rect rect, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawOval(rect, color, fill, width: width, overlay: overlay != 0, strokeOpacity: opacity, fillOpacity: opacity);
        internal Point draw_curve(Point p1, Point p2, Point p3, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawCurve(p1, p2, p3, color, fill, width: width, overlay: overlay != 0, strokeOpacity: opacity, fillOpacity: opacity);
        internal Point draw_squiggle(Point p1, Point p2, float breadth = 2, float[] color = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawSquiggle(p1, p2, breadth, color, width: width, overlay: overlay != 0, strokeOpacity: opacity, fillOpacity: opacity);
        internal Point draw_zigzag(Point p1, Point p2, float breadth = 2, float[] color = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawZigzag(p1, p2, breadth, color, width: width, overlay: overlay != 0, strokeOpacity: opacity, fillOpacity: opacity);
        internal Point draw_sector(Point center, Point point, float angle, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, bool full_sector = true, int overlay = 1)
            => DrawSector(center, point, angle, color, fill, width: width, fullSector: full_sector, overlay: overlay != 0, strokeOpacity: opacity, fillOpacity: opacity);
        internal Point draw_polyline(Point[] points, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawPolyline(points, color, fill, width: width, overlay: overlay != 0, strokeOpacity: opacity, fillOpacity: opacity);
        internal Point draw_quad(Quad quad, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawQuad(quad, color, fill, width: width, overlay: overlay != 0, strokeOpacity: opacity, fillOpacity: opacity);
        internal Point draw_bezier(Point p1, Point p2, Point p3, Point p4, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawBezier(p1, p2, p3, p4, color, fill, width: width, overlay: overlay != 0, strokeOpacity: opacity, fillOpacity: opacity);
        /// <summary>
        /// Short diagnostic string with page index and document identity.
        /// </summary>
        public override string ToString()
        {
            var doc = Parent;
            if (doc == null)
                return "page <detached>";

            int number;
            try { number = Number; }
            catch { number = -1; }

            string x = doc.Name;
            if (doc.StreamData != null)
                x = "memory";
            else if (string.IsNullOrEmpty(x))
                x = "new PDF";
            return $"page {number} of <{x}, doc# {doc.GraftId}>";
        }
    }
}
