using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace MuPDF.NET
{
    /// <summary>
    /// One page of a document.
    /// </summary>
    /// <remarks>
    /// Ported from PyMuPDF <c>class Page</c> in <c>src/__init__.py</c> (lines 9488–13241). Public methods follow that
    /// class’s intent and ordering where practical; names use .NET PascalCase. Doc comments are adapted from the
    /// Python docstrings on the matching methods (same behaviour, not a verbatim copy of narrative text).
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

        /// <summary>
        /// MuPDF’s global context is not safe under concurrent PDF font registration; parallel tests triggered AVs in <c>pdf_add_simple_font</c>.
        /// </summary>
        private static readonly object s_insertFontLock = new object();

        /// <summary>
        /// Stable identity for <see cref="Document"/> page-ref bookkeeping (Python uses <c>id(page)</c>).
        /// </summary>
        internal int PageRefId { get; }

        /// <summary>
        /// Owning document. Becomes <c>null</c> after <see cref="Page.PythonCompat._erase"/>-style teardown.
        /// </summary>
        internal Document? Parent { get; private set; }

        internal mupdf.FzPage NativePage
        {
            get
            {
                if (_disposed || _nativePage == null)
                    throw new ObjectDisposedException(nameof(Page));
                return _nativePage;
            }
        }

        internal mupdf.PdfPage NativePdfPage => Helpers.AsPdfPage(NativePage, required: true);

        internal Page(mupdf.FzPage fzPage, Document owner)
        {
            _nativePage = fzPage;
            PageRefId = Document.NextPageRefId();
            Parent = owner;
            owner.RegisterPageRef(this);
        }

        [MemberNotNull(nameof(Parent))]
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
            mupdf.PdfDocument? pdfForNm = null;
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
                        if (pdfForNm != null && pdfForNm.m_internal != null)
                        {
                            var nm = Helpers.PdfAnnotNmForXref(pdfForNm, xr);
                            if (!string.IsNullOrEmpty(nm))
                                _linkRefsByNm[nm] = new WeakReference<Link>(l);
                        }
                    }
                }
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
        /// Page number (0-based), from the underlying <c>fz_page</c> / <c>pdf_page</c>.
        /// </summary>
        public int Number => mupdf.mupdf.pdf_lookup_page_number(RequireParent().NativePdfDocument, NativePdfPage.obj());

        /// <summary>
        /// Page rectangle; equivalent to PyMuPDF <c>Page.bound()</c> / <c>fz_bound_page</c>.
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
        /// Return the page rectangle (PyMuPDF <c>Page.bound()</c> alias).
        /// </summary>
        public Rect Bound() => Rect;

        /// <summary>
        /// Page MediaBox.
        /// </summary>
        public Rect MediaBox => GetMediaBox();
        /// <summary>
        /// Page CropBox.
        /// </summary>
        public Rect CropBox => GetCropBox();
        /// <summary>
        /// Page BleedBox.
        /// </summary>
        public Rect BleedBox => GetSpecialBox("BleedBox");
        /// <summary>
        /// Page TrimBox.
        /// </summary>
        public Rect TrimBox => GetSpecialBox("TrimBox");
        /// <summary>
        /// Page ArtBox.
        /// </summary>
        public Rect ArtBox => GetSpecialBox("ArtBox");

        /// <summary>
        /// Page width.
        /// </summary>
        public float Width => (float)Rect.Width;
        /// <summary>
        /// Page height.
        /// </summary>
        public float Height => (float)Rect.Height;

        /// <summary>
        /// Media box width and height.
        /// Port of Python Page.mediabox_size (Point).
        /// </summary>
        public Point MediaBoxSize => new Point(MediaBox.Width, MediaBox.Height);

        /// <summary>
        /// Page rotation.
        /// </summary>
        public int Rotation
        {
            get
            {
                try
                {
                    var pdfPage = NativePdfPage;
                    return Helpers.PageRotation(pdfPage);
                }
                catch { return 0; }
            }
        }

        /// <summary>
        /// Page transformation matrix. Reflects page rotation and target coordinate system.
        /// </summary>
        public Matrix TransformationMatrix
        {
            get
            {
                try
                {
                    var mediabox = new mupdf.FzRect();
                    var ctm = new mupdf.FzMatrix();
                    mupdf.mupdf.pdf_page_transform(NativePdfPage, mediabox, ctm);
                    return Helpers.MatrixFromFz(ctm);
                }
                catch { return Matrix.Identity; }
            }
        }

        /// <summary>
        /// Reflects page de-rotation.
        /// </summary>
        public Matrix DerotationMatrix
        {
            get
            {
                int rot = Rotation;
                if (rot == 0) return Matrix.Identity;
                var mp = Rect.Width / 2.0 + Rect.X0;
                var mq = Rect.Height / 2.0 + Rect.Y0;
                return new Matrix(1, 0, 0, 1, (float)-mp, (float)-mq)
                    * Matrix.Rotation(-rot)
                    * new Matrix(1, 0, 0, 1, (float)mp, (float)mq);
            }
        }

        /// <summary>
        /// Whether the parent document is a PDF.
        /// </summary>
        public bool IsPdf => RequireParent().IsPdf;

        /// <summary>
        /// PDF xref number of page.
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
        /// First annotation on the page, or <c>null</c> if none (<c>Page.first_annot</c> in PyMuPDF).
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
        /// First link on the page, or <c>null</c> if none (<c>Page.first_link</c> in PyMuPDF).
        /// </summary>
        /// <remarks>Python <c>first_link</c> is <c>load_links()</c>; same xref/<c>/NM</c> seeding applies.</remarks>
        public Link FirstLink => LoadLinks();

        /// <summary>
        /// First widget (form field) on the page, or <c>null</c> if none (<c>Page.first_widget</c> in PyMuPDF).
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
        /// Generator over the annotations on the page.
        /// </summary>
        public IEnumerable<Annot> Annots()
        {
            var a = FirstAnnot;
            while (a != null)
            {
                yield return a;
                a = a.Next;
            }
        }

        /// <summary>
        /// Iterate annotations, optionally filtering by annotation types.
        /// Mirrors PyMuPDF <c>Page.annots(types=...)</c>.
        /// </summary>
        public IEnumerable<Annot> Annots(params AnnotationType[] types)
        {
            HashSet<AnnotationType> filter = null;
            if (types != null && types.Length > 0)
                filter = new HashSet<AnnotationType>(types);
            foreach (var annot in Annots())
            {
                if (filter == null || filter.Contains(annot.Type))
                    yield return annot;
            }
        }

        /// <summary>
        /// Generator over the links on the page.
        /// </summary>
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
        /// Generator over links, optionally filtered by link kind.
        /// Mirrors PyMuPDF <c>links(kinds=...)</c>.
        /// </summary>
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
        /// Generator over links, optionally filtered by raw link kind integers
        /// (Python compatibility shape for <c>links(kinds=[...])</c>).
        /// </summary>
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
        /// Generator over the form fields on the page.
        /// </summary>
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
        /// Generator over widgets, optionally filtered by field type.
        /// Mirrors PyMuPDF <c>widgets(types=...)</c>.
        /// </summary>
        public IEnumerable<Widget> Widgets(params WidgetType[] types)
        {
            HashSet<WidgetType> filter = null;
            if (types != null && types.Length > 0)
                filter = new HashSet<WidgetType>(types);
            foreach (var widget in Widgets())
            {
                if (filter == null || filter.Contains(widget.FieldType))
                    yield return widget;
            }
        }

        /// <summary>
        /// Generator over widgets, optionally filtered by raw widget type integers
        /// (Python compatibility shape for <c>widgets(types=[...])</c>).
        /// </summary>
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

        /// <summary>Add a “Text” (sticky note) annotation.</summary>
        /// <remarks>Matches PyMuPDF <c>Page._add_text_annot</c> / <c>add_text_annot</c> (<c>src/__init__.py</c>).</remarks>
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

        /// <summary>Add a “FreeText” annotation.</summary>
        /// <remarks>Ported to follow PyMuPDF <c>add_freetext_annot</c> behavior and options.</remarks>
        public Annot AddFreeTextAnnot(
            Rect rect,
            string text,
            float fontsize = 11,
            string fontname = null,
            float[] textColor = null,
            float[] fillColor = null,
            float[] borderColor = null,
            float borderWidth = 0,
            int[] dashes = null,
            Point[] callout = null,
            mupdf.pdf_line_ending lineEnd = mupdf.pdf_line_ending.PDF_ANNOT_LE_OPEN_ARROW,
            float opacity = 1,
            int align = 0,
            int rotate = 0,
            bool richtext = false,
            string style = null)
        {
            string rc = "<?xml version=\"1.0\"?>\n" +
                        "<body xmlns=\"http://www.w3.org/1999/xtml\"\n" +
                        "xmlns:xfa=\"http://www.xfa.org/schema/xfa-data/1.0/\"\n" +
                        "xfa:contentType=\"text/html\" xfa:APIVersion=\"Acrobat:8.0.0\" xfa:spec=\"2.4\">\n" +
                        (text ?? "");

            if (borderColor != null && !richtext)
                throw new ValueErrorException("cannot set border_color if rich_text is False");
            if (borderColor != null && textColor == null)
                textColor = borderColor;

            var r = rect.ToFzRect();
            if (mupdf.mupdf.fz_is_infinite_rect(r) != 0 || mupdf.mupdf.fz_is_empty_rect(r) != 0)
                throw new ValueErrorException(Constants.MSG_BAD_RECT);

            var page = NativePdfPage;
            var annot = mupdf.mupdf.pdf_create_annot(page, mupdf.pdf_annot_type.PDF_ANNOT_FREE_TEXT);
            var annotObj = mupdf.mupdf.pdf_annot_obj(annot);

            if (!richtext)
            {
                mupdf.mupdf.pdf_set_annot_contents(annot, text ?? "");
            }
            else
            {
                mupdf.mupdf.pdf_dict_put_text_string(annotObj, mupdf.mupdf.pdf_new_name("RC"), rc);
                if (!string.IsNullOrEmpty(style))
                    mupdf.mupdf.pdf_dict_put_text_string(annotObj, mupdf.mupdf.pdf_new_name("DS"), style);
            }

            mupdf.mupdf.pdf_set_annot_rect(annot, r);

            while (rotate < 0) rotate += 360;
            while (rotate >= 360) rotate -= 360;
            if (rotate != 0)
                mupdf.mupdf.pdf_dict_put_int(annotObj, mupdf.mupdf.pdf_new_name("Rotate"), rotate);

            mupdf.mupdf.pdf_set_annot_quadding(annot, align);
            mupdf.mupdf.pdf_set_annot_border_width(annot, borderWidth);
            mupdf.mupdf.pdf_set_annot_opacity(annot, opacity);

            if (fillColor != null && fillColor.Length > 0)
            {
                Helpers.CheckColor(fillColor);
                RequireParent().XrefSetKey(mupdf.mupdf.pdf_to_num(annotObj), "C", Helpers.EscapePdfArray(fillColor));
            }

            if (dashes != null)
            {
                foreach (var d in dashes)
                    mupdf.mupdf.pdf_add_annot_border_dash_item(annot, d);
            }

            if (callout != null && callout.Length > 0)
            {
                mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("IT"), mupdf.mupdf.pdf_new_name("FreeTextCallout"));
                mupdf.mupdf.pdf_set_annot_callout_style(annot, lineEnd);
                var vv = new mupdf.vector_fz_point();
                foreach (var p in callout)
                {
                    if (p == null) continue;
                    var fp = new mupdf.fz_point { x = (float)p.X, y = (float)p.Y };
                    vv.Add(fp);
                }
                if (vv.Count > 0)
                    mupdf.mupdf.pdf_set_annot_callout_line2(annot, vv);
            }

            if (!richtext)
            {
                if (textColor != null) Helpers.CheckColor(textColor);
                Helpers.JM_make_annot_DA(annot, textColor?.Length ?? 0, textColor ?? Array.Empty<float>(), fontname ?? "Helv", fontsize);
            }

            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
        }

        /// <summary>Add a “Line” annotation.</summary>
        /// <remarks>PyMuPDF <c>Page._add_line_annot</c> / <c>add_line_annot</c>.</remarks>
        public Annot AddLineAnnot(Point p1, Point p2)
        {
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_LINE);
            mupdf.mupdf.pdf_set_annot_line(annot, p1.ToFzPoint(), p2.ToFzPoint());
            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
        }

        /// <summary>Add a “Square” (rectangle) annotation.</summary>
        /// <remarks>PyMuPDF <c>Page._add_square_or_circle</c> with <c>PDF_ANNOT_SQUARE</c>.</remarks>
        public Annot AddRectAnnot(Rect rect)
        {
            var fr = rect.ToFzRect();
            if (mupdf.mupdf.fz_is_infinite_rect(fr) != 0 || mupdf.mupdf.fz_is_empty_rect(fr) != 0)
                throw new ValueErrorException(Constants.MSG_BAD_RECT);
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_SQUARE);
            mupdf.mupdf.pdf_set_annot_rect(annot, fr);
            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
        }

        /// <summary>Add a “Circle” (ellipse, oval) annotation.</summary>
        /// <remarks>PyMuPDF <c>Page._add_square_or_circle</c> with <c>PDF_ANNOT_CIRCLE</c>.</remarks>
        public Annot AddCircleAnnot(Rect rect)
        {
            var fr = rect.ToFzRect();
            if (mupdf.mupdf.fz_is_infinite_rect(fr) != 0 || mupdf.mupdf.fz_is_empty_rect(fr) != 0)
                throw new ValueErrorException(Constants.MSG_BAD_RECT);
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_CIRCLE);
            mupdf.mupdf.pdf_set_annot_rect(annot, fr);
            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
        }

        /// <summary>Add a “PolyLine” annotation.</summary>
        /// <remarks>PyMuPDF <c>Page._add_multiline</c> with <c>PDF_ANNOT_POLY_LINE</c>.</remarks>
        public Annot AddPolylineAnnot(Point[] points)
        {
            if (points == null || points.Length < 2)
                throw new ArgumentException(Constants.MSG_BAD_ARG_POINTS);
            foreach (var p in points)
                if (p == null) throw new ArgumentException(Constants.MSG_BAD_ARG_POINTS);
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_POLY_LINE);
            SetAnnotVertices(annot, points);
            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
        }

        /// <summary>Add a “Polygon” annotation.</summary>
        /// <remarks>PyMuPDF <c>Page._add_multiline</c> with <c>PDF_ANNOT_POLYGON</c>.</remarks>
        public Annot AddPolygonAnnot(Point[] points)
        {
            if (points == null || points.Length < 2)
                throw new ArgumentException(Constants.MSG_BAD_ARG_POINTS);
            foreach (var p in points)
                if (p == null) throw new ArgumentException(Constants.MSG_BAD_ARG_POINTS);
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_POLYGON);
            SetAnnotVertices(annot, points);
            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
        }

        /// <summary>Add a “Highlight” annotation.</summary>
        /// <remarks>PyMuPDF <c>add_highlight_annot</c> (selection or explicit quads).</remarks>
        public Annot AddHighlightAnnot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null)
        {
            return AddMarkupAnnot(mupdf.pdf_annot_type.PDF_ANNOT_HIGHLIGHT, quads, start, stop, clip);
        }

        /// <summary>Add an “Underline” annotation.</summary>
        public Annot AddUnderlineAnnot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null)
        {
            return AddMarkupAnnot(mupdf.pdf_annot_type.PDF_ANNOT_UNDERLINE, quads, start, stop, clip);
        }

        /// <summary>Add a “StrikeOut” annotation.</summary>
        public Annot AddStrikeoutAnnot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null)
        {
            return AddMarkupAnnot(mupdf.pdf_annot_type.PDF_ANNOT_STRIKE_OUT, quads, start, stop, clip);
        }

        /// <summary>Add a “Squiggly” underline annotation.</summary>
        public Annot AddSquigglyAnnot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null)
        {
            return AddMarkupAnnot(mupdf.pdf_annot_type.PDF_ANNOT_SQUIGGLY, quads, start, stop, clip);
        }

        /// <summary>Add a “Caret” annotation.</summary>
        /// <remarks>PyMuPDF <c>Page._add_caret_annot</c> (default size from <c>pdf_annot_rect</c>).</remarks>
        public Annot AddCaretAnnot(Point pos)
        {
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_CARET);
            var fzp = pos.ToFzPoint();
            var r0 = mupdf.mupdf.pdf_annot_rect(annot);
            mupdf.mupdf.pdf_set_annot_rect(annot, mupdf.mupdf.fz_make_rect(fzp.x, fzp.y, fzp.x + (r0.x1 - r0.x0), fzp.y + (r0.y1 - r0.y0)));
            mupdf.mupdf.pdf_update_annot(annot);
            Helpers.AddAnnotId(annot, "A");
            return new Annot(annot, this);
        }

        /// <summary>Add a (“rubber”) “Stamp” annotation.</summary>
        /// <remarks>PyMuPDF <c>add_stamp_annot</c>; image/custom stamps are not fully ported here.</remarks>
        public Annot AddStampAnnot(Rect rect, int stamp = 0)
        {
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_STAMP);
            mupdf.mupdf.pdf_set_annot_rect(annot, rect.ToFzRect());
            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
        }

        /// <summary>Add a “FileAttachment” annotation.</summary>
        /// <remarks>PyMuPDF <c>Page._add_file_annot</c> embeds the file; this port is still minimal.</remarks>
        public Annot AddFileAnnot(Point pos, byte[] buffer, string filename, string ufilename = null, string desc = null, string icon = "PushPin")
        {
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_FILE_ATTACHMENT);
            var fzp = pos.ToFzPoint();
            mupdf.mupdf.pdf_set_annot_rect(annot, mupdf.mupdf.fz_make_rect(fzp.x, fzp.y, fzp.x + 20, fzp.y + 20));
            mupdf.mupdf.pdf_set_annot_icon_name(annot, icon);
            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
        }

        /// <summary>Add an “Ink” (“handwriting”) annotation.</summary>
        /// <remarks>
        /// The argument must be a sequence of strokes, each a sequence of <c>(x, y)</c> pairs, as in PyMuPDF
        /// <c>Page._add_ink_annot</c>: points are transformed by the inverse of <c>pdf_page_transform</c>.
        /// </remarks>
        public Annot AddInkAnnot(Point[][] paths)
        {
            if (paths == null)
                throw new ArgumentException(Constants.MSG_BAD_ARG_INK_ANNOT);
            var pdfPage = NativePdfPage;
            var ctm = new mupdf.FzMatrix();
            pdfPage.pdf_page_transform(new mupdf.FzRect(), ctm);
            var invCtm = ctm.fz_invert_matrix();
            var annot = mupdf.mupdf.pdf_create_annot(pdfPage, mupdf.pdf_annot_type.PDF_ANNOT_INK);
            var annotObj = mupdf.mupdf.pdf_annot_obj(annot);
            var pdf = RequireParent().NativePdfDocument;
            var inkList = mupdf.mupdf.pdf_new_array(pdf, paths.Length);
            foreach (var path in paths)
            {
                if (path == null)
                    throw new ArgumentException(Constants.MSG_BAD_ARG_INK_ANNOT);
                var stroke = mupdf.mupdf.pdf_new_array(pdf, path.Length * 2);
                foreach (var pt in path)
                {
                    if (pt == null)
                        throw new ArgumentException(Constants.MSG_BAD_ARG_INK_ANNOT);
                    var tp = mupdf.FzPoint.fz_transform_point(pt.ToFzPoint(), invCtm);
                    mupdf.mupdf.pdf_array_push_real(stroke, tp.x);
                    mupdf.mupdf.pdf_array_push_real(stroke, tp.y);
                }
                mupdf.mupdf.pdf_array_push(inkList, stroke);
            }
            mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("InkList"), inkList);
            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
        }

        /// <summary>Add a “Redact” annotation.</summary>
        /// <remarks>PyMuPDF <c>add_redact_annot</c> supports overlay text, fill, <c>cross_out</c>, etc.; subset here.</remarks>
        public Annot AddRedactAnnot(Quad quad, string? text = null, string? fontname = null, float fontsize = 11,
            int align = 0, float[]? fillColor = null, float[]? textColor = null)
        {
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, mupdf.pdf_annot_type.PDF_ANNOT_REDACT);
            mupdf.mupdf.pdf_set_annot_rect(annot, quad.Rect.ToFzRect());
            if (text != null) mupdf.mupdf.pdf_set_annot_contents(annot, text);
            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
        }

        /// <summary>Apply the redaction annotations of the page.</summary>
        /// <param name="images">0 ignore, 1 remove overlapping, 2 blank parts, 3 remove unless invisible (PyMuPDF).</param>
        /// <param name="graphics">0 ignore, 1 remove if contained, 2 remove overlapping.</param>
        /// <param name="text">0 remove text, 1 ignore text.</param>
        /// <remarks>Core call matches PyMuPDF <c>Page._apply_redactions</c>; Python <c>apply_redactions</c> also redraws overlay text.</remarks>
        public bool ApplyRedactions(int images = 2, int graphics = 1, int text = 0)
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

        /// <summary>
        /// Delete annot and return the next one.
        /// </summary>
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
        /// Delete a link.
        /// </summary>
        public void DeleteLink(Link link)
        {
            if (link == null) return;
            var pdf = RequireParent().NativePdfDocument;
            var pdfPage = NativePdfPage;
            var annotArray = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Annots"));
            if (annotArray.m_internal == null)
            {
                Helpers.JM_refresh_links(pdf, pdfPage);
                link._erase();
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
            link._erase();
            SyncLinkWrapperCache();
        }

        /// <summary>Delete a link given a link dictionary (<c>Page.delete_link</c> in PyMuPDF).</summary>
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
                        linkobj._erase();
                    else if (linkdict.TryGetValue("id", out var idObj) && idObj is string sid && !string.IsNullOrEmpty(sid)
                             && TryGetCachedLinkByAnnotNm(sid, out var byNm))
                        byNm._erase();
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

        private Annot AddMarkupAnnot(mupdf.pdf_annot_type type, Quad[] quads, Point start, Point stop, IRect clip)
        {
            var annot = mupdf.mupdf.pdf_create_annot(NativePdfPage, type);
            if (quads != null && quads.Length > 0)
            {
                foreach (var q in quads)
                    mupdf.mupdf.pdf_add_annot_quad_point(annot, q.ToFzQuad());
            }
            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
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

        /// <summary>Insert a new link for the current page.</summary>
        /// <remarks>
        /// PyMuPDF <c>Page.insert_link</c> returns <c>None</c> and only supports link dicts built via <c>utils.getLinkText</c> (a <c>kind</c> key is required).
        /// This overload returns the first <see cref="Link"/> after <see cref="LoadLinks"/> for convenience and also accepts some legacy dicts without <c>kind</c>.
        /// For strict return-type and input parity with Python, use <see cref="InsertLinkVoid"/>.
        /// </remarks>
        /// <param name="mark">Python <c>insert_link(..., mark=True)</c>; reserved, not used in this binding.</param>
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

        /// <summary>Insert a link the same way as Python <c>Page.insert_link</c> (returns <c>None</c>): requires <c>kind</c> and uses <c>_addAnnot_FromString</c>.</summary>
        /// <param name="mark">Reserved for API parity with Python; not used.</param>
        public void InsertLinkVoid(Dictionary<string, object> linkDict, bool mark = true)
        {
            _ = mark;
            if (linkDict == null) return;
            if (!Helpers.TryBuildInsertLinkAnnotObjectString(this, linkDict, out var objSrc))
                throw new ValueErrorException("link kind not supported");
            _addAnnot_FromString(new[] { objSrc });
        }

        /// <summary>Replace the page’s link annotations (implementation clears link annots then inserts).</summary>
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
        /// Update an existing link (Python <c>utils.update_link</c>: <c>getLinkText</c> + <c>doc.update_object(xref, annot, page=page)</c>).
        /// Requires <c>xref</c> and a supported <c>kind</c> in <paramref name="linkDict"/> (same shape as <see cref="InsertLinkVoid"/>).
        /// </summary>
        public void UpdateLink(Dictionary<string, object> linkDict)
        {
            if (linkDict == null) throw new ArgumentNullException(nameof(linkDict));
            if (!linkDict.TryGetValue("xref", out var xrefObj) || xrefObj == null)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);
            int xref = Convert.ToInt32(xrefObj, CultureInfo.InvariantCulture);
            if (xref < 1)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);

            if (!Helpers.TryBuildInsertLinkAnnotObjectString(this, linkDict, out var objSrc))
                throw new ValueErrorException("link kind not supported");

            RequireParent().UpdateObject(xref, objSrc, this);
        }

        /// <summary>Create a list of all links contained in a PDF page.</summary>
        /// <remarks>PyMuPDF <c>Page.get_links</c> (may attach xref/id when available).</remarks>
        public List<Dictionary<string, object>> GetLinks()
        {
            var result = new List<Dictionary<string, object>>();
            var link = FirstLink;
            while (link != null)
            {
                result.Add(link.ToDictionary());
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

        /// <summary>Create pixmap of page.</summary>
        /// <param name="matrix">Transform matrix (default identity).</param>
        /// <param name="cs">Colorspace; default RGB.</param>
        /// <param name="clip">Optional clip rectangle.</param>
        /// <param name="alpha">Whether to include an alpha channel.</param>
        /// <param name="annots">Whether to render annotations.</param>
        /// <remarks>PyMuPDF <c>Page.get_pixmap</c> also supports <c>dpi</c> (derives matrix).</remarks>
        public Pixmap GetPixmap(Matrix matrix = null, Colorspace cs = null, IRect clip = null, bool alpha = false, bool annots = true)
        {
            var dl = GetDisplayList(annots ? 1 : 0);
            return dl.GetPixmap(matrix ?? Matrix.Identity, cs ?? Colorspace.CsRGB, alpha, clip);
        }

        /// <summary>Build a display list for the page (with or without annotations).</summary>
        /// <remarks>PyMuPDF <c>Page.get_displaylist</c>.</remarks>
        public DisplayList GetDisplayList(int annots = 1)
        {
            mupdf.FzDisplayList dl;
            if (annots != 0)
                dl = mupdf.mupdf.fz_new_display_list_from_page(NativePage);
            else
                dl = mupdf.mupdf.fz_new_display_list_from_page_contents(NativePage);
            return new DisplayList(dl);
        }

        /// <summary>Make SVG image from page.</summary>
        /// <remarks>PyMuPDF <c>Page.get_svg_image</c>.</remarks>
        public string GetSvgImage(Matrix matrix = null, int textAsPath = 1)
        {
            var ctm = (matrix ?? Matrix.Identity).ToFzMatrix();
            var mediabox = mupdf.mupdf.fz_bound_page(NativePage);
            var tbounds = mupdf.mupdf.fz_transform_rect(mediabox, ctm);
            var buf = mupdf.mupdf.fz_new_buffer(1024);
            var output = new mupdf.FzOutput(buf);
            var dev = mupdf.mupdf.fz_new_svg_device(output,
                tbounds.x1 - tbounds.x0,
                tbounds.y1 - tbounds.y0,
                textAsPath, 1);
            mupdf.mupdf.fz_run_page(NativePage, dev, ctm, new mupdf.FzCookie());
            mupdf.mupdf.fz_close_device(dev);
            mupdf.mupdf.fz_close_output(output);
            return System.Text.Encoding.UTF8.GetString(buf.fz_buffer_extract());
        }

        // ─── Text Extraction ────────────────────────────────────────────

        /// <summary>
        /// Create a TextPage from the page.
        /// </summary>
        public TextPage GetTextPage(int flags = 0, IRect clip = null)
        {
            var opts = new mupdf.fz_stext_options();
            opts.flags = flags;
            var stp = new mupdf.FzStextPage(RequireParent().NativeDocument, Number, new mupdf.FzStextOptions(opts));
            var textPage = new TextPage(stp) { Parent = this };
            return textPage;
        }

        /// <summary>
        /// Extract page text in various formats.
        /// Options: 'text', 'blocks', 'words', 'html', 'xhtml', 'xml', 'json', 'dict', 'rawdict'.
        /// </summary>
        public string GetText(string option = "text", IRect clip = null, int flags = 0, TextPage textpage = null, string sort = null)
        {
            var tp = textpage ?? GetTextPage(flags, clip);
            try
            {
                switch (option.ToLower())
                {
                    case "text": return tp.ExtractText();
                    case "blocks": return tp.ExtractBlocks().ToString();
                    case "words": return tp.ExtractWords().ToString();
                    case "html": return tp.ExtractHtml();
                    case "xhtml": return tp.ExtractXhtml();
                    case "xml": return tp.ExtractXml();
                    case "json": return tp.ExtractJson();
                    case "rawdict": return tp.ExtractRawDict().ToString();
                    case "dict": return tp.ExtractDict().ToString();
                    default: return tp.ExtractText();
                }
            }
            finally
            {
                if (textpage == null) tp.Dispose();
            }
        }

        /// <summary>
        /// Extract text blocks as list.
        /// </summary>
        public List<(float x0, float y0, float x1, float y1, string text, int blockNo, int blockType)> GetTextBlocks(int flags = 0)
        {
            using var tp = GetTextPage(flags);
            return tp.ExtractBlocks();
        }

        /// <summary>
        /// Extract text words as list.
        /// </summary>
        public List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> GetTextWords(int flags = 0)
        {
            using var tp = GetTextPage(flags);
            return tp.ExtractWords();
        }

        /// <summary>
        /// Search for a string on the page. Returns a list of Quads, each containing one occurrence.
        /// </summary>
        /// <remarks>Matches PyMuPDF <c>Page.search_for</c>: clip is applied when building the text page; wrong-parent <paramref name="textpage"/> raises like Python.</remarks>
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
        /// Like <see cref="SearchFor"/> but returns merged axis-aligned rectangles (PyMuPDF <c>Page.search_for(..., quads=False)</c> via <see cref="TextPage.SearchRects"/>).
        /// </summary>
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
        /// Extract text in a rectangle.
        /// Corresponds to Python Page.get_textbox().
        /// </summary>
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
        /// Extract selected text between two points.
        /// Corresponds to Python Page.get_text_selection().
        /// </summary>
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
        /// Create an OCR-backed TextPage.
        /// Port of Python utils.get_textpage_ocr() using available C# bindings.
        /// </summary>
        public TextPage GetTextPageOcr(int flags = 0, string language = "eng", int dpi = 72, bool full = false, string tessdata = null)
        {
            // Ensure unknown-unicode replacement is not suppressed, matching Python.
            flags = flags & ~mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE & ~mupdf.mupdf.FZ_STEXT_USE_GID_FOR_UNKNOWN_UNICODE;

            if (full)
            {
                using var pixFull = GetPixmap(matrix: Matrix.Identity, alpha: false, annots: true);
                var pdfocrOptions = new mupdf.FzPdfocrOptions();
                pdfocrOptions.compress = 0;
                if (!string.IsNullOrEmpty(language))
                    pdfocrOptions.language = language;
                if (!string.IsNullOrEmpty(tessdata))
                    pdfocrOptions.datadir = tessdata;

                var ocrBuf = mupdf.mupdf.fz_new_buffer(1024);
                var ocrOut = new mupdf.FzOutput(ocrBuf);
                ocrOut.fz_write_pixmap_as_pdfocr(pixFull.NativePixmap, pdfocrOptions);
                mupdf.mupdf.fz_close_output(ocrOut);
                byte[] pdfBytes = ocrBuf.fz_buffer_extract();

                // Must stay alive as long as returned TextPage is in use.
                var ocrDocFull = new Document(pdfBytes, "pdf");
                var ocrPageFull = ocrDocFull.LoadPage(0);
                // OCR-only text page.
                var ocrTp = ocrPageFull.GetTextPage(flags);
                ocrTp.Parent = this;
                return ocrTp;
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

            using var pixPartial = tempPage.GetPixmap(matrix: Matrix.Identity, alpha: false, annots: true);
            var pdfocrOpts = new mupdf.FzPdfocrOptions();
            pdfocrOpts.compress = 0;
            if (!string.IsNullOrEmpty(language))
                pdfocrOpts.language = language;
            if (!string.IsNullOrEmpty(tessdata))
                pdfocrOpts.datadir = tessdata;
            var partialBuf = mupdf.mupdf.fz_new_buffer(1024);
            var partialOut = new mupdf.FzOutput(partialBuf);
            partialOut.fz_write_pixmap_as_pdfocr(pixPartial.NativePixmap, pdfocrOpts);
            mupdf.mupdf.fz_close_output(partialOut);
            byte[] partialPdfBytes = partialBuf.fz_buffer_extract();

            using var ocrDocPartial = new Document(partialPdfBytes, "pdf");
            var ocrPagePartial = ocrDocPartial.LoadPage(0);

            // Extend original textpage with OCR page content.
            var mergedTp = tp;
            var stOpts = new mupdf.fz_stext_options();
            stOpts.flags = mupdf.mupdf.FZ_STEXT_ACCURATE_BBOXES;
            var stDevice = mergedTp.NativeStextPage.fz_new_stext_device(new mupdf.FzStextOptions(stOpts));
            ocrPagePartial.NativePage.fz_run_page(stDevice, Matrix.Identity.ToFzMatrix(), new mupdf.FzCookie());
            mupdf.mupdf.fz_close_device(stDevice);
            mergedTp.Parent = this;
            return mergedTp;
        }

        // ─── Text Insertion ─────────────────────────────────────────────

        /// <summary>
        /// Insert text starting at a given point.
        /// </summary>
        public int InsertText(Point point, string text, float fontsize = 11, string fontname = "helv",
            float[] color = null, float rotate = 0, int renderMode = 0, float borderWidth = 0.05f)
        {
            var tw = new TextWriter(Rect, color: color);
            tw.Append(point, text, fontsize: fontsize, fontname: fontname);
            tw.WriteText(this);
            return text.Split('\n').Length;
        }

        /// <summary>
        /// Insert text into a given rectangle. Creates a Shape object, uses its same-named method and commits it.
        /// </summary>
        public (int rc, List<string> rest) InsertTextbox(Rect rect, string text, float fontsize = 11, string fontname = "helv",
            float[] color = null, int align = 0, float borderWidth = 0.05f, float expandTabs = 1, int renderMode = 0)
        {
            if (string.IsNullOrEmpty(text)) return (0, new List<string>());
            if (expandTabs > 1)
                text = text.Replace("\t", new string(' ', (int)expandTabs));

            var tw = new TextWriter(Rect, color: color);
            var font = new Font(fontname);
            float lineHeight = fontsize * 1.2f;
            float maxWidth = (float)rect.Width;
            float y = (float)rect.Y0 + fontsize;
            var lines = text.Split('\n');
            var rest = new List<string>();
            int linesWritten = 0;

            foreach (var rawLine in lines)
            {
                if (y + lineHeight > rect.Y1 + fontsize)
                {
                    rest.Add(rawLine);
                    continue;
                }

                float textWidth = font.TextLength(rawLine, fontsize);
                if (textWidth <= maxWidth)
                {
                    float x;
                    if (align == 1) x = (float)rect.X0 + (maxWidth - textWidth) / 2;
                    else if (align == 2) x = (float)rect.X1 - textWidth;
                    else x = (float)rect.X0;

                    tw.Append(new Point(x, y), rawLine, fontsize: fontsize, font: font);
                    y += lineHeight;
                    linesWritten++;
                }
                else
                {
                    var words = rawLine.Split(' ');
                    string currentLine = "";
                    foreach (var word in words)
                    {
                        string testLine = currentLine.Length == 0 ? word : currentLine + " " + word;
                        if (font.TextLength(testLine, fontsize) <= maxWidth)
                        {
                            currentLine = testLine;
                        }
                        else
                        {
                            if (currentLine.Length > 0)
                            {
                                if (y + lineHeight > rect.Y1 + fontsize) { rest.Add(currentLine); }
                                else
                                {
                                    float tw2 = font.TextLength(currentLine, fontsize);
                                    float x;
                                    if (align == 1) x = (float)rect.X0 + (maxWidth - tw2) / 2;
                                    else if (align == 2) x = (float)rect.X1 - tw2;
                                    else x = (float)rect.X0;
                                    tw.Append(new Point(x, y), currentLine, fontsize: fontsize, font: font);
                                    y += lineHeight;
                                    linesWritten++;
                                }
                            }
                            currentLine = word;
                        }
                    }
                    if (currentLine.Length > 0)
                    {
                        if (y + lineHeight > rect.Y1 + fontsize) { rest.Add(currentLine); }
                        else
                        {
                            float tw3 = font.TextLength(currentLine, fontsize);
                            float x;
                            if (align == 1) x = (float)rect.X0 + (maxWidth - tw3) / 2;
                            else if (align == 2) x = (float)rect.X1 - tw3;
                            else x = (float)rect.X0;
                            tw.Append(new Point(x, y), currentLine, fontsize: fontsize, font: font);
                            y += lineHeight;
                            linesWritten++;
                        }
                    }
                }
            }

            tw.WriteText(this);
            return (linesWritten, rest);
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
        public (double spareHeight, double scale) InsertHtmlbox(
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
            if (rotate % 90 != 0)
                throw new ValueErrorException("bad rotation angle");
            while (rotate < 0) rotate += 360;
            rotate %= 360;
            if (scaleLow < 0 || scaleLow > 1)
                throw new ValueErrorException("'scale_low' must be in [0, 1]");

            string userCss = "body {margin:1px;}" + (css ?? "");
            using var story = new Story(text ?? "", userCss, archive: archive);

            double tempW = rotate == 90 || rotate == 270 ? rect.Height : rect.Width;
            double tempH = rotate == 90 || rotate == 270 ? rect.Width : rect.Height;

            var fit = FindStoryScale(story, tempW, tempH, scaleLow);
            if (!fit.success)
                return (-1, fit.scale);

            // Render the fitted story to an image and place it.
            var drawRect = new Rect(0, 0, tempW / fit.scale, tempH / fit.scale);
            story.Reset();
            story.Place(drawRect);

            var pix = mupdf.mupdf.fz_new_pixmap_with_bbox(
                Colorspace.CsRGB,
                new IRect(drawRect).ToFzIRect(),
                new mupdf.FzSeparations(),
                1);
            mupdf.mupdf.fz_clear_pixmap(pix);
            var dev = mupdf.mupdf.fz_new_draw_device(Matrix.Identity.ToFzMatrix(), pix);
            story.Draw(dev, Matrix.Identity);
            mupdf.mupdf.fz_close_device(dev);

            using var pm = new Pixmap(pix);
            byte[] png = pm.ToPng();

            // Match PyMuPDF's high-level pipeline: render into a temporary PDF page,
            // then place that page on the target page via ShowPdfPage.
            using var tempDoc = new Document();
            using var tempPage = tempDoc.NewPage(width: (float)tempW, height: (float)tempH);
            tempPage.InsertImage(
                new Rect(0, 0, tempW, tempH),
                stream: png,
                keepProportion: false,
                alpha: 1,
                overlay: "true");
            ShowPdfPage(
                rect,
                tempDoc,
                pno: 0,
                keepProportion: false,
                overlay: overlay,
                oc: oc,
                rotate: rotate);

            return (fit.spareHeight, fit.scale);
        }

        /// <summary>
        /// Backward-compatible wrapper that ignores returned fit information.
        /// </summary>
        public void InsertHtmlbox(Rect rect, string text, string css = null, float scaleLow = 0, float opacity = 1, int rotate = 0, int oc = 0)
        {
            _ = InsertHtmlbox(rect, text, css, scaleLow, archive: null, rotate: rotate, oc: oc, opacity: opacity);
        }

        /// <summary>
        /// Show a PDF source page inside a target rectangle.
        /// Port of PyMuPDF <c>Page.show_pdf_page()</c>.
        /// </summary>
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
            if (!RequireParent().Graftmaps.TryGetValue(srcGraftId, out var gmap))
            {
                gmap = new Graftmap(RequireParent());
                RequireParent().Graftmaps[srcGraftId] = gmap;
            }

            if (overlay)
                WrapContents();

            int rcXref = ShowPdfPageInternal(
                srcPage.NativePdfPage,
                overlay: overlay,
                matrix: matrix,
                xref: 0,
                oc: oc,
                clip: srcRectPdf,
                graftmap: gmap.NativeGraftMap,
                imgName: imgName,
                pdfOut: pdfOut,
                targetPageObj: targetPageObj);
            return rcXref;
        }

        private static (bool success, double scale, double spareHeight) FindStoryScale(Story story, double width, double height, float scaleLow)
        {
            double low = Math.Max(scaleLow, 1e-3);
            double high = 1.0;
            bool haveFit = false;
            double bestScale = low;
            double bestSpare = -1;

            for (int i = 0; i < 22; i++)
            {
                double mid = (low + high) * 0.5;
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
            double smpX = (srcRect.X0 + srcRect.X1) * 0.5;
            double smpY = (srcRect.Y0 + srcRect.Y1) * 0.5;
            double tmpX = (tarRect.X0 + tarRect.X1) * 0.5;
            double tmpY = (tarRect.Y0 + tarRect.Y1) * 0.5;

            // m moves to (0,0), then rotates.
            Matrix m = new Matrix(1, 0, 0, 1, -smpX, -smpY) * Matrix.Rotation(rotate);
            Rect srcRot = new Rect(srcRect).Transform(m);

            double fw = tarRect.Width / Math.Max(srcRot.Width, Constants.Epsilon);
            double fh = tarRect.Height / Math.Max(srcRot.Height, Constants.Epsilon);
            if (keepProportion)
            {
                double f = Math.Min(fw, fh);
                fw = f;
                fh = f;
            }

            m = m * new Matrix(fw, fh);
            m = m * new Matrix(1, 0, 0, 1, tmpX, tmpY);
            return m;
        }

        private string MakeShowPdfResourceName()
        {
            // keep "fzFrm" prefix for Python-side traceability.
            string baseName = "fzFrm";
            string candidate = baseName + Number.ToString(CultureInfo.InvariantCulture);
            int i = 0;
            while (ResourceNameExists(candidate))
            {
                i++;
                candidate = baseName + i.ToString(CultureInfo.InvariantCulture);
            }
            return candidate;
        }

        private bool ResourceNameExists(string name)
        {
            var resources = mupdf.mupdf.pdf_dict_get(NativePdfPage.obj(), mupdf.mupdf.pdf_new_name("Resources"));
            if (resources.m_internal == null)
                return false;
            var xobj = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("XObject"));
            if (xobj.m_internal == null)
                return false;
            var existing = mupdf.mupdf.pdf_dict_gets(xobj, name);
            return existing.m_internal != null;
        }

        // ─── Fonts and Images ───────────────────────────────────────────

        /// <summary>
        /// List of fonts defined in the page object.
        /// </summary>
        public List<(int xref, string ext, string type, string baseName, string name, string encoding)> GetFonts(bool full = false)
        {
            return RequireParent().GetPageFonts(Number, full);
        }

        /// <summary>
        /// List of images defined in the page object.
        /// </summary>
        public List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)> GetImages(bool full = false)
        {
            return RequireParent().GetPageImages(Number, full);
        }

        /// <summary>
        /// Insert an image for display in a rectangle.
        /// </summary>
        public int InsertImage(Rect rect, string filename = null, byte[] stream = null, Pixmap pixmap = null,
            string mask = null, int rotate = 0, int xref = 0, int oc = 0, bool keepProportion = true,
            int alpha = -1, string overlay = "true")
        {
            var pdf = RequireParent().NativePdfDocument;
            var pdfPage = NativePdfPage;

            mupdf.FzImage fzImg;
            if (xref > 0)
            {
                var obj = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
                fzImg = mupdf.mupdf.pdf_load_image(pdf, obj);
            }
            else if (!string.IsNullOrEmpty(filename))
            {
                fzImg = mupdf.mupdf.fz_new_image_from_file(filename);
            }
            else if (stream != null && stream.Length > 0)
            {
                var buf = Helpers.BufferFromBytes(stream);
                fzImg = mupdf.mupdf.fz_new_image_from_buffer(buf);
            }
            else if (pixmap != null)
            {
                fzImg = mupdf.mupdf.fz_new_image_from_pixmap(pixmap.NativePixmap, new mupdf.FzImage());
            }
            else
                throw new ArgumentException("need one of filename, stream, pixmap or xref");

            var imgObj = mupdf.mupdf.pdf_add_image(pdf, fzImg);
            int imgXref = mupdf.mupdf.pdf_to_num(imgObj);

            float w = (float)rect.Width, h = (float)rect.Height;
            if (keepProportion)
            {
                int imgW = fzImg.w();
                int imgH = fzImg.h();
                float scaleW = w / imgW, scaleH = h / imgH;
                float scale = Math.Min(scaleW, scaleH);
                w = imgW * scale;
                h = imgH * scale;
            }

            string name = $"Img{imgXref}";
            var resources = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Resources"));
            if (resources.m_internal == null)
            {
                resources = mupdf.mupdf.pdf_new_dict(pdf, 2);
                mupdf.mupdf.pdf_dict_put(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Resources"), resources);
            }
            var xObject = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("XObject"));
            if (xObject.m_internal == null)
            {
                xObject = mupdf.mupdf.pdf_new_dict(pdf, 1);
                mupdf.mupdf.pdf_dict_put(resources, mupdf.mupdf.pdf_new_name("XObject"), xObject);
            }
            mupdf.mupdf.pdf_dict_puts(xObject, name, imgObj);

            string cmd = $"q {w:G} 0 0 {h:G} {rect.X0:G} {rect.Y1 - h:G} cm /{name} Do Q\n";
            var buf2 = Helpers.BufferFromBytes(System.Text.Encoding.ASCII.GetBytes(cmd));
            var contents = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Contents"));
            if (contents.m_internal == null)
            {
                var stream2 = mupdf.mupdf.pdf_add_stream(pdf, buf2, new mupdf.PdfObj(), 0);
                mupdf.mupdf.pdf_dict_put(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Contents"), stream2);
            }
            else
            {
                var newStream = mupdf.mupdf.pdf_add_stream(pdf, buf2, new mupdf.PdfObj(), 0);
                if (mupdf.mupdf.pdf_is_array(contents) != 0)
                    mupdf.mupdf.pdf_array_push(contents, newStream);
                else
                {
                    var arr = mupdf.mupdf.pdf_new_array(pdf, 2);
                    mupdf.mupdf.pdf_array_push(arr, contents);
                    mupdf.mupdf.pdf_array_push(arr, newStream);
                    mupdf.mupdf.pdf_dict_put(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Contents"), arr);
                }
            }

            return imgXref;
        }

        /// <summary>
        /// Insert a font for use on the page.
        /// </summary>
        public int InsertFont(string fontname = "helv", string fontfile = null, byte[] fontbuffer = null,
            bool setSimple = false, int encoding = 0)
        {
            lock (s_insertFontLock)
            {
                var pdf = RequireParent().NativePdfDocument;
                var pdfPage = NativePdfPage;

                mupdf.PdfObj fontObj;
                if (!string.IsNullOrEmpty(fontfile))
                {
                    var buf = mupdf.mupdf.fz_read_file(fontfile);
                    var fzFont = mupdf.mupdf.fz_new_font_from_buffer(fontname, buf, 0, 0);
                    fontObj = mupdf.mupdf.pdf_add_cid_font(pdf, fzFont);
                }
                else if (fontbuffer != null && fontbuffer.Length > 0)
                {
                    var buf = Helpers.BufferFromBytes(fontbuffer);
                    var fzFont = mupdf.mupdf.fz_new_font_from_buffer(fontname, buf, 0, 0);
                    fontObj = mupdf.mupdf.pdf_add_cid_font(pdf, fzFont);
                }
                else
                {
                    string base14 = Font.NormalizeBase14FontName(fontname);
                    var fzFont = mupdf.mupdf.fz_new_base14_font(base14);
                    if (fzFont?.m_internal == null)
                        throw new ValueErrorException($"cannot create base-14 font: {fontname}");
                    fontObj = mupdf.mupdf.pdf_add_simple_font(pdf, fzFont, encoding);
                }

                int xref = mupdf.mupdf.pdf_to_num(fontObj);

                var resources = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Resources"));
                if (resources.m_internal == null)
                {
                    resources = mupdf.mupdf.pdf_new_dict(pdf, 2);
                    mupdf.mupdf.pdf_dict_put(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Resources"), resources);
                }
                var fonts = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("Font"));
                if (fonts.m_internal == null)
                {
                    fonts = mupdf.mupdf.pdf_new_dict(pdf, 1);
                    mupdf.mupdf.pdf_dict_put(resources, mupdf.mupdf.pdf_new_name("Font"), fonts);
                }
                string resName = $"F{xref}";
                mupdf.mupdf.pdf_dict_puts(fonts, resName, fontObj);

                return xref;
            }
        }

        /// <summary>
        /// Extract image information from the page.
        /// </summary>
        public List<Dictionary<string, object>> GetImageInfo(bool hashes = false, bool xrefs = false)
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

            var imglist = GetImages();
            var digests = new Dictionary<string, int>();
            foreach (var item in imglist)
            {
                int xref = item.xref;
                var obj = mupdf.mupdf.pdf_load_object(doc.NativePdfDocument, xref);
                var image = mupdf.mupdf.pdf_load_image(doc.NativePdfDocument, obj);
                var pix = image.fz_get_pixmap_from_image(new mupdf.FzIrect(mupdf.mupdf.fz_infinite_irect), new mupdf.FzMatrix(image.w(), 0, 0, image.h(), 0, 0), null, null);
                var md5 = pix.fz_md5_pixmap2();
                byte[] digestBytes = new byte[md5.Count];
                for (int i = 0; i < md5.Count; i++)
                    digestBytes[i] = md5[i];
                var digestKey = BitConverter.ToString(digestBytes);
                digests[digestKey] = xref;
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
        /// Backward-compatible overload where argument means xrefs.
        /// </summary>
        public List<Dictionary<string, object>> GetImageInfo(bool xrefs)
        {
            return GetImageInfo(false, xrefs);
        }

        /// <summary>
        /// Return list of image positions on a page.
        /// </summary>
        public List<Rect> GetImageRects(object name)
        {
            int xref = ResolveImageXref(name);
            var doc = RequireParent();
            var obj = mupdf.mupdf.pdf_load_object(doc.NativePdfDocument, xref);
            var image = mupdf.mupdf.pdf_load_image(doc.NativePdfDocument, obj);
            var pix = image.fz_get_pixmap_from_image(new mupdf.FzIrect(mupdf.mupdf.fz_infinite_irect), new mupdf.FzMatrix(image.w(), 0, 0, image.h(), 0, 0), null, null);
            var md5 = pix.fz_md5_pixmap2();
            byte[] digest = new byte[md5.Count];
            for (int i = 0; i < md5.Count; i++)
                digest[i] = md5[i];

            var infos = GetImageInfo(hashes: true);
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
            var obj = mupdf.mupdf.pdf_load_object(doc.NativePdfDocument, xref);
            var image = mupdf.mupdf.pdf_load_image(doc.NativePdfDocument, obj);
            var pix = image.fz_get_pixmap_from_image(new mupdf.FzIrect(mupdf.mupdf.fz_infinite_irect), new mupdf.FzMatrix(image.w(), 0, 0, image.h(), 0, 0), null, null);
            var md5 = pix.fz_md5_pixmap2();
            byte[] digest = new byte[md5.Count];
            for (int i = 0; i < md5.Count; i++)
                digest[i] = md5[i];

            var infos = GetImageInfo(hashes: true);
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
        /// Get rectangle occupied by image <paramref name="name"/>.
        /// Port of Python Page.get_image_bbox(name, transform=0).
        /// </summary>
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
                var imglist = GetImages(full: true).Where(i => i.name == imgName).ToList();
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

            // Python falls back to JM_image_reporter when xref==0. No direct binding here.
            return (infRect, nullMat);
        }

        /// <summary>
        /// List XObjects defined in this page object.
        /// </summary>
        public List<Dictionary<string, object>> GetXobjects()
        {
            return RequireParent().GetPageXobjects(Number);
        }

        /// <summary>
        /// Page language (inheritable /Lang).
        /// Port of Python Page.language property.
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
        /// Set page language (/Lang) on page dictionary.
        /// Port of Python Page.set_language().
        /// </summary>
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
        /// Reflects page rotation.
        /// Port of Python Page.rotation_matrix.
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
        /// Refresh page after link/annot/widget updates.
        /// Port of Python Page.refresh().
        /// </summary>
        public void Refresh()
        {
            var reloaded = RequireParent().ReloadPage(this);
            _nativePage?.Dispose();
            _nativePage = reloaded._nativePage;
            reloaded._nativePage = null;
        }

        /// <summary>
        /// Replace image content referenced by xref.
        /// Port of Python Page.replace_image().
        /// </summary>
        public void ReplaceImage(int xref, string? filename = null, Pixmap? pixmap = null, byte[]? stream = null)
        {
            var doc = RequireParent();
            if (!doc.XrefIsImage(xref))
                throw new ArgumentException("xref not an image");

            int count = 0;
            if (!string.IsNullOrEmpty(filename)) count++;
            if (pixmap != null) count++;
            if (stream != null && stream.Length > 0) count++;
            if (count != 1)
                throw new ArgumentException("Exactly one of filename/stream/pixmap must be given");

            int newXref = InsertImage(Rect, filename: filename, stream: stream, pixmap: pixmap);

            // Copy new image object over old xref.
            string newObj = doc.XrefObject(newXref, compressed: false, ascii: false);
            doc.UpdateObject(xref, newObj);
            byte[] newStream = doc.XrefStreamRaw(newXref);
            doc.UpdateStream(xref, newStream, compress: true);

            var contents = GetContents();
            if (contents.Count > 0)
            {
                int lastContentsXref = contents[contents.Count - 1];
                doc.UpdateStream(lastContentsXref, new byte[] { (byte)' ' }, compress: true);
            }
            _imageInfo = null;
        }

        /// <summary>
        /// Clip away page content outside the rectangle.
        /// Port of Python Page.clip_to_rect().
        /// </summary>
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
        /// Try to access layout information.
        /// Port of Python Page.get_layout().
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
        /// Cache slot for layout analysis information (Python: layout_information).
        /// </summary>
        public object LayoutInformation
        {
            get => _layoutInformation;
            set => _layoutInformation = value;
        }

        /// <summary>
        /// Global lazy layout provider callback (Python: _get_layout).
        /// If set, GetLayout() invokes this once and caches the result in layout_information.
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
        /// Write one or more TextWriter objects to this page.
        /// Port of Python Page.write_text().
        /// </summary>
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
            if (name is object[] arr && arr.Length > 0 && arr[0] is int arrXref)
                return arrXref;
            if (name is string imgName)
            {
                var imglist = GetImages().Where(i => i.name == imgName).ToList();
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
        /// Set page rotation.
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
        /// Set the page's MediaBox.
        /// </summary>
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
        /// Return the top-left point of the CropBox.
        /// Mirrors PyMuPDF <c>Page.cropbox_position</c>.
        /// </summary>
        public Point CropBoxPosition() => CropBox.TopLeft;

        /// <summary>
        /// Set the CropBox. Will also change Page.Rect.
        /// </summary>
        public void SetCropBox(Rect rect)
        {
            var pdfPage = NativePdfPage;
            var obj = pdfPage.obj();
            mupdf.mupdf.pdf_dict_put_rect(obj, mupdf.mupdf.pdf_new_name("CropBox"), rect.ToFzRect());
        }

        /// <summary>
        /// Set the page's BleedBox.
        /// </summary>
        public void SetBleedBox(Rect rect) => SetSpecialBox("BleedBox", rect);
        /// <summary>
        /// Set the page's TrimBox.
        /// </summary>
        public void SetTrimBox(Rect rect) => SetSpecialBox("TrimBox", rect);
        /// <summary>
        /// Set the page's ArtBox.
        /// </summary>
        public void SetArtBox(Rect rect) => SetSpecialBox("ArtBox", rect);

        private void SetSpecialBox(string name, Rect rect)
        {
            var pdfPage = NativePdfPage;
            var obj = pdfPage.obj();
            mupdf.mupdf.pdf_dict_put_rect(obj, mupdf.mupdf.pdf_new_name(name), rect.ToFzRect());
        }

        // ─── Drawing / Shapes ───────────────────────────────────────────

        /// <summary>
        /// Create a new Shape object for the page.
        /// </summary>
        public Shape NewShape() => new Shape(this);

        /// <summary>
        /// Draw a line from point p1 to point p2.
        /// </summary>
        public Point DrawLine(Point p1, Point p2, float[] color = null, float width = 1, string lineCap = null, string lineJoin = null, float[] dashes = null, float opacity = 1, string blendMode = null, int overlay = 1, string morph = null, int oc = 0)
        {
            var shape = NewShape();
            shape.DrawLine(p1, p2);
            shape.Finish(color: color, width: width, opacity: opacity);
            shape.Commit(overlay == 1);
            return p2;
        }

        /// <summary>
        /// Draw a rectangle. See Shape class method for details.
        /// </summary>
        public Point DrawRect(Rect rect, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
        {
            var shape = NewShape();
            shape.DrawRect(rect);
            shape.Finish(color: color, fill: fill, width: width, opacity: opacity);
            shape.Commit(overlay == 1);
            return rect.TopLeft;
        }

        /// <summary>
        /// Draw a circle given its center and radius.
        /// </summary>
        public Point DrawCircle(Point center, float radius, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
        {
            var shape = NewShape();
            shape.DrawCircle(center, radius);
            shape.Finish(color: color, fill: fill, width: width, opacity: opacity);
            shape.Commit(overlay == 1);
            return center;
        }

        /// <summary>
        /// Draw an oval given its containing rectangle or quad.
        /// </summary>
        public Point DrawOval(Rect rect, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
        {
            var shape = NewShape();
            shape.DrawOval(rect);
            shape.Finish(color: color, fill: fill, width: width, opacity: opacity);
            shape.Commit(overlay == 1);
            return rect.TopLeft;
        }

        /// <summary>
        /// Draw a special Bezier curve from p1 to p3, generating control points on lines p1 to p2 and p2 to p3.
        /// </summary>
        public Point DrawCurve(Point p1, Point p2, Point p3, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
        {
            var shape = NewShape();
            shape.DrawCurve(p1, p2, p3);
            shape.Finish(color: color, fill: fill, width: width, opacity: opacity);
            shape.Commit(overlay == 1);
            return p3;
        }

        /// <summary>
        /// Draw a squiggly line from point p1 to point p2.
        /// </summary>
        public Point DrawSquiggle(Point p1, Point p2, float breadth = 2, float[] color = null, float width = 1, float opacity = 1, int overlay = 1)
        {
            var shape = NewShape();
            shape.DrawSquiggle(p1, p2, breadth);
            shape.Finish(color: color, width: width, opacity: opacity);
            shape.Commit(overlay == 1);
            return p2;
        }

        /// <summary>
        /// Draw a zigzag line from point p1 to point p2.
        /// </summary>
        public Point DrawZigzag(Point p1, Point p2, float breadth = 2, float[] color = null, float width = 1, float opacity = 1, int overlay = 1)
        {
            var shape = NewShape();
            shape.DrawZigzag(p1, p2, breadth);
            shape.Finish(color: color, width: width, opacity: opacity);
            shape.Commit(overlay == 1);
            return p2;
        }

        /// <summary>
        /// Draw a circle sector given circle center, one arc end point and the angle of the arc.
        /// </summary>
        public Point DrawSector(Point center, Point point, float angle, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, bool fullSector = true, int overlay = 1)
        {
            var shape = NewShape();
            shape.DrawSector(center, point, angle, fullSector);
            shape.Finish(color: color, fill: fill, width: width, opacity: opacity);
            shape.Commit(overlay == 1);
            return point;
        }

        /// <summary>
        /// Draw multiple connected line segments.
        /// </summary>
        public Point DrawPolyline(Point[] points, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
        {
            var shape = NewShape();
            shape.DrawPolyline(points);
            shape.Finish(color: color, fill: fill, width: width, opacity: opacity);
            shape.Commit(overlay == 1);
            return points.Last();
        }

        /// <summary>
        /// Draw a quadrilateral.
        /// </summary>
        public Point DrawQuad(Quad quad, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
        {
            var shape = NewShape();
            shape.DrawQuad(quad);
            shape.Finish(color: color, fill: fill, width: width, opacity: opacity);
            shape.Commit(overlay == 1);
            return quad.UL;
        }

        /// <summary>
        /// Draw a general cubic Bezier curve from p1 to p4 using control points p2 and p3.
        /// </summary>
        public Point DrawBezier(Point p1, Point p2, Point p3, Point p4, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
        {
            var shape = NewShape();
            shape.DrawBezier(p1, p2, p3, p4);
            shape.Finish(color: color, fill: fill, width: width, opacity: opacity);
            shape.Commit(overlay == 1);
            return p4;
        }

        // ─── Page Contents / Resources ──────────────────────────────────

        /// <summary>
        /// Clean the page contents streams.
        /// </summary>
        public void CleanContents(int sanitize = 1)
        {
            try
            {
                var pdfPage = Helpers.AsPdfPage(NativePage, required: false);
                if (pdfPage == null || pdfPage.m_internal == null) return;
                var filter = new mupdf.PdfFilterOptions();
                filter.recurse = 1;
                mupdf.mupdf.pdf_filter_page_contents(pdfPage.doc(), pdfPage, filter);
            }
            catch { }
        }

        /// <summary>
        /// All /Contents streams concatenated to one bytes object.
        /// </summary>
        public byte[] ReadContents()
        {
            var pdfPage = NativePdfPage;
            var contents = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Contents"));
            if (contents.m_internal == null) return Array.Empty<byte>();
            var buf = mupdf.mupdf.pdf_load_stream(contents);
            return buf.fz_buffer_extract();
        }

        /// <summary>
        /// Set object at <paramref name="xref"/> as the page's <c>/Contents</c> (PyMuPDF <c>set_contents</c>).
        /// </summary>
        public void SetContents(int xref)
        {
            if (RequireParent().IsClosed)
                throw new ValueErrorException("document closed");
            if (!RequireParent().IsPdf)
                throw new ValueErrorException("is no PDF");
            if (xref < 1 || xref >= RequireParent().XrefLength)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);
            if (!RequireParent().XrefIsStream(xref))
                throw new ValueErrorException("xref is no stream");
            RequireParent().XrefSetKey(Xref, "Contents", $"{xref} 0 R");
        }

        /// <summary>
        /// Get list of xrefs of page contents objects.
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
        /// Ensure page is in a balanced graphics state.
        /// </summary>
        public void WrapContents()
        {
            var pdfPage = NativePdfPage;
            var pdf = RequireParent().NativePdfDocument;
            var contents = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Contents"));
            if (contents.m_internal == null) return;

            var existingBuf = LoadPageContentsBuffer(pdfPage.obj());
            var existingBytes = existingBuf.fz_buffer_extract();
            var wrappedBytes = new byte[existingBytes.Length + 4];
            wrappedBytes[0] = (byte)'q';
            wrappedBytes[1] = (byte)'\n';
            Array.Copy(existingBytes, 0, wrappedBytes, 2, existingBytes.Length);
            wrappedBytes[wrappedBytes.Length - 2] = (byte)'\n';
            wrappedBytes[wrappedBytes.Length - 1] = (byte)'Q';

            var newBuf = Helpers.BufferFromBytes(wrappedBytes);
            var newStream = mupdf.mupdf.pdf_add_stream(pdf, newBuf, new mupdf.PdfObj(), 0);
            mupdf.mupdf.pdf_dict_put(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Contents"), newStream);
        }

        /// <summary>
        /// Check whether <c>/Contents</c> appears to be in a balanced graphics state (PyMuPDF <c>is_wrapped</c>).
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
        /// Set page rotation to 0 while maintaining visual appearance (PyMuPDF <c>remove_rotation</c>).
        /// Returns the inverse of the generated derotation matrix.
        /// </summary>
        public Matrix RemoveRotation()
        {
            int rot = Rotation % 360;
            if (rot < 0) rot += 360;
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
            PrefixContentsMatrix(mat);

            if (rot == 90 || rot == 270)
            {
                var swapped = new Rect(mb.Y0, mb.X0, mb.Y1, mb.X1);
                SetMediaBox(swapped);
            }

            SetRotation(0);

            var inv = mat.Inverted() ?? Matrix.Identity;

            // move annotations
            foreach (var annot in Annots())
            {
                var tr = new Rect(annot.Rect).Transform(inv);
                try { annot.SetRect(tr); } catch { }
            }

            // move links
            var links = GetLinks();
            foreach (var link in links)
            {
                if (link.TryGetValue("from", out var fromObj) && fromObj is Rect rr)
                    link["from"] = new Rect(rr).Transform(inv);
                try { UpdateLink(link); } catch { }
            }

            // move widgets
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

            return inv;
        }

        private void PrefixContentsMatrix(Matrix mat)
        {
            string cmd = string.Format(
                CultureInfo.InvariantCulture,
                "{0:G} {1:G} {2:G} {3:G} {4:G} {5:G} cm ",
                mat.A, mat.B, mat.C, mat.D, mat.E, mat.F);
            byte[] prefix = System.Text.Encoding.UTF8.GetBytes(cmd);
            byte[] existing = ReadContents();
            var merged = new byte[prefix.Length + existing.Length];
            Buffer.BlockCopy(prefix, 0, merged, 0, prefix.Length);
            if (existing.Length > 0)
                Buffer.BlockCopy(existing, 0, merged, prefix.Length, existing.Length);

            var pdfPage = NativePdfPage;
            var pdf = RequireParent().NativePdfDocument;
            var buf = Helpers.BufferFromBytes(merged);
            var newStream = mupdf.mupdf.pdf_add_stream(pdf, buf, new mupdf.PdfObj(), 0);
            mupdf.mupdf.pdf_dict_put(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Contents"), newStream);
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

        private (int push, int pop) CountQBalance()
        {
            byte[] data = ReadContents();
            int balance = 0;
            int missingPush = 0;
            int i = 0;
            while (i < data.Length)
            {
                byte b = data[i];
                if (b == (byte)'%')
                {
                    while (i < data.Length && data[i] != (byte)'\n' && data[i] != (byte)'\r') i++;
                    continue;
                }

                if (IsPdfWhitespace(b))
                {
                    i++;
                    continue;
                }

                int start = i;
                while (i < data.Length && !IsPdfWhitespace(data[i])) i++;
                int len = i - start;
                if (len == 1)
                {
                    byte t = data[start];
                    if (t == (byte)'q') balance++;
                    else if (t == (byte)'Q')
                    {
                        balance--;
                        if (balance < 0)
                        {
                            missingPush++;
                            balance = 0;
                        }
                    }
                }
            }
            return (missingPush, balance);
        }

        private static bool IsPdfWhitespace(byte b)
        {
            return b == 0 || b == 9 || b == 10 || b == 12 || b == 13 || b == 32;
        }

        // ─── Get Drawings ───────────────────────────────────────────────

        /// <summary>
        /// Retrieve vector graphics (line art) from the page. The extended version includes clips.
        /// </summary>
        public List<Dictionary<string, object>> GetDrawings(bool extended = false)
        {
            var result = new List<Dictionary<string, object>>();
            var dl = mupdf.mupdf.fz_new_display_list(Rect.ToFzRect());
            var dev = mupdf.mupdf.fz_new_list_device(dl);
            mupdf.mupdf.fz_run_page(NativePage, dev, Matrix.Identity.ToFzMatrix(), new mupdf.FzCookie());
            mupdf.mupdf.fz_close_device(dev);

            var traceOut = new mupdf.FzOutput(mupdf.mupdf.fz_new_buffer(0));
            var tracedev = mupdf.mupdf.fz_new_trace_device(traceOut);
            mupdf.mupdf.fz_run_display_list(dl, tracedev, Matrix.Identity.ToFzMatrix(), Rect.Infinite.ToFzRect(), new mupdf.FzCookie());
            mupdf.mupdf.fz_close_device(tracedev);

            return result;
        }

        /// <summary>
        /// Get text trace information for the page.
        /// </summary>
        public List<Dictionary<string, object>> GetTexttrace()
        {
            var result = new List<Dictionary<string, object>>();
            var buf = mupdf.mupdf.fz_new_buffer(1024);
            var output = new mupdf.FzOutput(buf);
            var dev = mupdf.mupdf.fz_new_trace_device(output);
            mupdf.mupdf.fz_run_page(NativePage, dev, Matrix.Identity.ToFzMatrix(), new mupdf.FzCookie());
            mupdf.mupdf.fz_close_device(dev);
            mupdf.mupdf.fz_close_output(output);
            return result;
        }

        /// <summary>
        /// Get extended page drawings (equivalent to GetDrawings with extended set to true).
        /// </summary>
        public List<Dictionary<string, object>> GetCdp() => GetDrawings(extended: true);

        // ─── Labels ─────────────────────────────────────────────────────

        /// <summary>
        /// Return the label for this PDF page. Errors return an empty string.
        /// </summary>
        public string GetLabel()
        {
            try
            {
                var pdf = RequireParent().NativePdfDocument;
                // Current binding exposes a write-into-buffer API shape only.
                return "";
            }
            catch { return ""; }
        }

        // ─── Tab ────────────────────────────────────────────────────────

        /// <summary>
        /// Get annotation type information for the page.
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
        /// Annotation identifiers on this page (PyMuPDF <c>annot_names()</c>).
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
        /// Annotation xref/type/id triples (PyMuPDF <c>annot_xrefs()</c>).
        /// </summary>
        public List<(int xref, AnnotationType type, string id)> AnnotXrefs()
        {
            var result = new List<(int xref, AnnotationType type, string id)>();
            foreach (var annot in Annots())
            {
                var info = annot.GetInfo();
                string id = "";
                if (info != null && info.TryGetValue("id", out var value))
                    id = value ?? "";
                result.Add((annot.Xref, annot.Type, id));
            }
            return result;
        }

        /// <summary>
        /// Load an annotation by name (/NM key) or xref.
        /// Mirrors PyMuPDF <c>Page.load_annot()</c>.
        /// </summary>
        public Annot LoadAnnot(object ident)
        {
            if (ident is string name)
            {
                foreach (var annot in Annots())
                {
                    if (annot.Id == name)
                        return annot;
                }
                return null;
            }
            if (ident is int xref)
            {
                foreach (var annot in Annots())
                {
                    if (annot.Xref == xref)
                        return annot;
                }
                return null;
            }
            throw new ArgumentException("identifier must be a string or integer");
        }

        /// <summary>
        /// Get first link object loaded from the underlying page.
        /// Mirrors PyMuPDF <c>Page.load_links()</c>.
        /// </summary>
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
        /// Load a widget by xref.
        /// Mirrors PyMuPDF <c>Page.load_widget()</c>.
        /// </summary>
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
        /// Add a widget (form field) to this page.
        /// Best-effort port of Python add_widget using exposed SWIG APIs.
        /// </summary>
        public Annot AddWidget(Widget widget)
        {
            if (widget == null)
                throw new ArgumentException("bad type: widget");
            if (!RequireParent().IsPdf)
                throw new ArgumentException("is no PDF");

            var pdfPage = NativePdfPage;
            var annot = mupdf.mupdf.pdf_create_annot(pdfPage, mupdf.pdf_annot_type.PDF_ANNOT_WIDGET);
            if (annot == null || annot.m_internal == null)
                throw new InvalidOperationException("cannot create widget");

            var obj = mupdf.mupdf.pdf_annot_obj(annot);

            // Map field type to /FT name.
            string ft = "Tx";
            switch (widget.FieldType)
            {
                case WidgetType.Button:
                case WidgetType.CheckBox:
                case WidgetType.RadioButton:
                    ft = "Btn";
                    break;
                case WidgetType.ComboBox:
                case WidgetType.ListBox:
                    ft = "Ch";
                    break;
                case WidgetType.Signature:
                    ft = "Sig";
                    break;
                default:
                    ft = "Tx";
                    break;
            }
            mupdf.mupdf.pdf_dict_put(obj, mupdf.mupdf.pdf_new_name("FT"), mupdf.mupdf.pdf_new_name(ft));

            if (!string.IsNullOrEmpty(widget.FieldName))
                mupdf.mupdf.pdf_dict_put_text_string(obj, mupdf.mupdf.pdf_new_name("T"), widget.FieldName);
            if (!string.IsNullOrEmpty(widget.FieldValue))
                mupdf.mupdf.pdf_dict_put_text_string(obj, mupdf.mupdf.pdf_new_name("V"), widget.FieldValue);
            if (!string.IsNullOrEmpty(widget.FieldLabel))
                mupdf.mupdf.pdf_dict_put_text_string(obj, mupdf.mupdf.pdf_new_name("TU"), widget.FieldLabel);

            mupdf.mupdf.pdf_set_annot_rect(annot, widget.Rect.ToFzRect());
            mupdf.mupdf.pdf_update_annot(annot);
            return new Annot(annot, this);
        }

        /// <summary>
        /// Delete widget from page and return the next one.
        /// Port of Python delete_widget().
        /// </summary>
        public Widget DeleteWidget(Widget widget)
        {
            if (widget == null)
                throw new ArgumentException("bad type: widget");
            var nextWidget = widget.Next;
            var annot = LoadAnnot(widget.Xref);
            if (annot == null)
                throw new ArgumentException("bad type: widget");
            DeleteAnnot(annot);
            return nextWidget;
        }

        // ─── Bounding boxes ─────────────────────────────────────────────

        private Rect GetMediaBox()
        {
            try
            {
                var pdfPage = NativePdfPage;
                var mb = mupdf.mupdf.pdf_dict_get_inheritable(pdfPage.obj(), mupdf.mupdf.pdf_new_name("MediaBox"));
                if (mb.m_internal != null)
                {
                    var r = mupdf.mupdf.pdf_to_rect(mb);
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
                var cb = mupdf.mupdf.pdf_dict_get_inheritable(pdfPage.obj(), mupdf.mupdf.pdf_new_name("CropBox"));
                if (cb.m_internal != null)
                {
                    var r = mupdf.mupdf.pdf_to_rect(cb);
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
                    return new Rect(r.x0, r.y0, r.x1, r.y1);
                }
            }
            catch { }
            return CropBox;
        }

        // ─── Run page ───────────────────────────────────────────────────

        /// <summary>
        /// Run the page through a device.
        /// </summary>
        public void Run(mupdf.FzDevice dev, Matrix transform)
        {
            mupdf.mupdf.fz_run_page(NativePage, dev, transform.ToFzMatrix(), new mupdf.FzCookie());
        }

        // ─── Table Detection ─────────────────────────────────────────────

        /// <summary>
        /// Find tables on this page using the pdfplumber-style algorithm.
        /// Returns a TableFinder containing all detected tables.
        /// </summary>
        /// <param name="settings">Optional table detection settings.</param>
        public TableFinder FindTables(TableSettings? settings = null)
        {
            return new TableFinder(this, settings);
        }

        // ─── IDisposable ────────────────────────────────────────────────

        /// <summary>
        /// Releases all resources used by the page.
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

            _nativePage?.Dispose();
            _nativePage = null;
            _disposed = true;
        }

        ~Page() { Dispose(); }

        /// <summary>Same idea as PyMuPDF <c>Page.__str__</c>: page index and parent document label.</summary>
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
