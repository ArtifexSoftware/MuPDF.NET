using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a hyperlink on a PDF page.
    /// <para>Ports PyMuPDF <c>class Link</c> (<c>src/__init__.py</c>).</para>
    /// </summary>
    public class Link
    {
        private mupdf.FzLink _nativeLink;
        private bool _detached;
        public Page? Parent { get; internal set; }
        public bool ThisOwn { get; set; } = true;
        /// <summary>Resolves link xref and <c>/NM</c> from the PDF link list, or by rectangle match.</summary>
        private int? _linkAnnotXrefHint;
        private string _linkAnnotIdHint;

        private Page RequirePage()
        {
            if (_detached || Parent == null)
                throw new ObjectDisposedException(nameof(Link));
            return Parent;
        }

        internal Link(mupdf.FzLink link, Page page)
        {
            _nativeLink = link;
            Parent = page;
        }

        /// <summary>Match Python <c>Page.load_links</c> (first annot) or <c>Link.next</c> (successor row when prior <c>xref</c> &gt; 0).</summary>
        internal void SetLinkAnnotIdentity(int xref, string nm)
        {
            _linkAnnotXrefHint = xref;
            _linkAnnotIdHint = nm ?? "";
        }

        /// <summary>Hot area rectangle (PyMuPDF <c>Link.rect</c>).</summary>
        public Rect Rect
        {
            get
            {
                var r = _nativeLink.m_internal.rect;
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
        }

        /// <summary>URI string (PyMuPDF <c>Link.uri</c>).</summary>
        public string Uri => _nativeLink.m_internal.uri;

        /// <summary>
        /// Whether the link is external (PyMuPDF <c>Link.is_external</c>).
        /// Uses <c>mupdf.fz_is_external_link</c> on the native URI when available.
        /// </summary>
        public bool IsExternal
        {
            get
            {
                var u = Uri;
                if (string.IsNullOrEmpty(u)) return false;
                try
                {
                    return mupdf.mupdf.fz_is_external_link(u) != 0;
                }
                catch
                {
                    return u.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        || u.StartsWith("mailto", StringComparison.OrdinalIgnoreCase)
                        || u.StartsWith("ftp", StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        /// <summary>PDF annotation xref (PyMuPDF <c>Link.xref</c>).</summary>
        public int Xref
        {
            get
            {
                if (_linkAnnotXrefHint.HasValue)
                    return _linkAnnotXrefHint.Value;
                try
                {
                    var pdfPage = Helpers.AsPdfPage(RequirePage().NativePage, required: false);
                    if (pdfPage == null || pdfPage.m_internal == null) return 0;
                    var annotArr = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Annots"));
                    if (annotArr.m_internal == null) return 0;
                    int n = mupdf.mupdf.pdf_array_len(annotArr);
                    for (int i = 0; i < n; i++)
                    {
                        var obj = mupdf.mupdf.pdf_array_get(annotArr, i);
                        var subtype = mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("Subtype"));
                        if (mupdf.mupdf.pdf_name_eq(subtype, mupdf.mupdf.pdf_new_name("Link")) != 0)
                        {
                            var r = mupdf.mupdf.pdf_dict_get_rect(obj, mupdf.mupdf.pdf_new_name("Rect"));
                            var linkRect = Rect;
                            if (Math.Abs(r.x0 - linkRect.X0) < 1 && Math.Abs(r.y0 - linkRect.Y0) < 1
                                && Math.Abs(r.x1 - linkRect.X1) < 1 && Math.Abs(r.y1 - linkRect.Y1) < 1)
                                return mupdf.mupdf.pdf_to_num(obj);
                        }
                    }
                }
                catch { }
                return 0;
            }
        }

        /// <summary>PDF <c>/NM</c> for this link when known (Python <c>Link.id</c>).</summary>
        public string Id
        {
            get
            {
                if (_linkAnnotXrefHint.HasValue)
                    return _linkAnnotIdHint ?? "";
                try
                {
                    int xr = Xref;
                    if (xr < 1) return "";
                    return Helpers.PdfAnnotNmForXref(RequirePage().RequireParent().NativePdfDocument, xr);
                }
                catch { return ""; }
            }
        }

        /// <summary>Returns link destination details as <see cref="LinkDest"/>.</summary>
        public LinkDest Dest
        {
            get
            {
                if (_detached || Parent == null)
                    throw new ValueErrorException("orphaned object: parent is None");
                var doc = RequirePage().RequireParent();
                if (doc.IsClosed || doc.IsEncrypted)
                    throw new ValueErrorException("document closed or encrypted");

                var u = Uri ?? "";
                (int page, float x, float y)? resolved = null;
                if (!IsExternal && !u.StartsWith("#", StringComparison.Ordinal))
                    resolved = doc.ResolveLink(u);
                return new LinkDest(this, resolved, doc);
            }
        }

        /// <summary>Link annotation flags (PyMuPDF <c>Link.flags</c>).</summary>
        public int Flags
        {
            get
            {
                try
                {
                    int xref = Xref;
                    if (xref == 0) return 0;
                    var pdf = RequirePage().RequireParent().NativePdfDocument;
                    var obj = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
                    var resolved = mupdf.mupdf.pdf_resolve_indirect(obj);
                    var fObj = mupdf.mupdf.pdf_dict_get(resolved, mupdf.mupdf.pdf_new_name("F"));
                    return fObj.m_internal != null ? mupdf.mupdf.pdf_to_int(fObj) : 0;
                }
                catch { return 0; }
            }
        }

        /// <summary>Border dictionary from the link annotation (Python <c>Link.border</c> / <c>_border</c>).</summary>
        public Dictionary<string, object> Border
        {
            get
            {
                try
                {
                    int xref = Xref;
                    if (xref < 1 || !RequirePage().RequireParent().IsPdf)
                        return new Dictionary<string, object>();
                    var pdf = RequirePage().RequireParent().NativePdfDocument;
                    var obj = mupdf.mupdf.pdf_resolve_indirect(mupdf.mupdf.pdf_new_indirect(pdf, xref, 0));
                    return Helpers.JM_annot_border(obj);
                }
                catch
                {
                    return new Dictionary<string, object>();
                }
            }
        }

        /// <summary>Color dictionary from the link annotation (Python <c>Link.colors</c> / <c>_colors</c>).</summary>
        public Dictionary<string, object> Colors
        {
            get
            {
                int xref = Xref;
                if (xref < 1)
                    throw new ValueErrorException(Constants.MSG_BAD_XREF);
                if (!RequirePage().RequireParent().IsPdf)
                    throw new ValueErrorException("is no PDF");
                var pdf = RequirePage().RequireParent().NativePdfDocument;
                var obj = mupdf.mupdf.pdf_resolve_indirect(mupdf.mupdf.pdf_new_indirect(pdf, xref, 0));
                if (obj.m_internal == null)
                    throw new ValueErrorException(Constants.MSG_BAD_XREF);
                return Helpers.JM_annot_colors(obj);
            }
        }

        /// <summary>Next link in the page chain (PyMuPDF <c>Link.next</c>).</summary>
        public Link Next
        {
            get
            {
                // Python Link.next: if not self.this.m_internal: return None
                if (_detached || Parent == null)
                    return null;
                if (_nativeLink?.m_internal == null)
                    return null;
                mupdf.FzLink val;
                try
                {
                    val = _nativeLink.next();
                }
                catch
                {
                    return null;
                }
                if (val?.m_internal == null)
                    return null;
                var nextLink = new Link(val, RequirePage());
                int prevXref = Xref;
                if (prevXref > 0)  // prev link has an xref
                {
                    try
                    {
                        var page = RequirePage();
                        var link_xrefs = new List<int>();
                        var link_ids = new List<string>();
                        foreach (var t in Helpers.JM_get_annot_xref_list(page.NativePdfPage.obj()))
                        {
                            if (t.type_ == (int)mupdf.pdf_annot_type.PDF_ANNOT_LINK)
                            {
                                link_xrefs.Add(t.xref);
                                link_ids.Add(t.nm ?? "");
                            }
                        }
                        int idx = link_xrefs.IndexOf(prevXref);
                        if (idx >= 0 && idx + 1 < link_xrefs.Count)
                        {
                            nextLink.SetLinkAnnotIdentity(link_xrefs[idx + 1], link_ids[idx + 1]);
                        }
                        else
                        {
                            nextLink.SetLinkAnnotIdentity(0, "");
                        }
                    }
                    catch
                    {
                        nextLink.SetLinkAnnotIdentity(0, "");
                    }
                }
                else
                {
                    nextLink.SetLinkAnnotIdentity(0, "");
                }
                return nextLink;
            }
        }

        /// <summary>Target page number for internal links (PyMuPDF <c>Link.page</c>, default -1).</summary>
        public int Page
        {
            get
            {
                if (Uri == null) return -1;
                if (Uri.StartsWith("#"))
                {
                    var loc = mupdf.mupdf.fz_resolve_link(RequirePage().RequireParent().NativeDocument, Uri, null, null);
                    return mupdf.mupdf.fz_page_number_from_location(RequirePage().RequireParent().NativeDocument, loc);
                }
                return -1;
            }
        }

        /// <summary>Link as a dictionary (PyMuPDF <c>utils.getLinkDict</c> / link dict conventions).</summary>
        public Dictionary<string, object> ToDictionary()
        {
            int xr = Xref;
            string nm = Id;

            var d = new Dictionary<string, object>
            {
                ["kind"] = IsExternal ? (int)LinkType.Uri : (int)LinkType.Goto,
                ["from"] = Rect,
                ["uri"] = Uri,
                ["page"] = Page,
                ["xref"] = xr,
                ["id"] = nm,
            };
            if (xr > 0)
            {
                try
                {
                    if (RequirePage().RequireParent().IsPdf)
                        Helpers.EnrichLinkDictFromPdfAnnot(RequirePage().RequireParent().NativePdfDocument, xr, d);
                }
                catch { }
            }
            return d;
        }

        /// <summary>
        /// Detach this wrapper from its owning <see cref="Page"/> (PyMuPDF <c>Link._erase</c>).
        /// </summary>
        public void Erase()
        {
            if (_detached) return;
            _detached = true;
            Parent = null;
        }

        /// <summary>
        /// Set link border (PyMuPDF <c>Link.set_border</c>).
        /// <para>Pass a border dictionary, or use <c>width</c>/<c>style</c>/<c>dashes</c> keys as in Python.</para>
        /// </summary>
        public void SetBorder(Dictionary<string, object> border)
        {
            int xref = Xref;
            if (xref == 0) return;
            var pdf = RequirePage().RequireParent().NativePdfDocument;
            var obj = mupdf.mupdf.pdf_resolve_indirect(mupdf.mupdf.pdf_new_indirect(pdf, xref, 0));

            if (border == null)
            {
                mupdf.mupdf.pdf_dict_dels(obj, "Border");
                return;
            }

            var borderArr = mupdf.mupdf.pdf_new_array(pdf, 3);
            float hCorner = border.ContainsKey("hCorner") ? Convert.ToSingle(border["hCorner"]) : 0;
            float vCorner = border.ContainsKey("vCorner") ? Convert.ToSingle(border["vCorner"]) : 0;
            float width = border.ContainsKey("width") ? Convert.ToSingle(border["width"]) : 0;
            mupdf.mupdf.pdf_array_push_real(borderArr, hCorner);
            mupdf.mupdf.pdf_array_push_real(borderArr, vCorner);
            mupdf.mupdf.pdf_array_push_real(borderArr, width);
            mupdf.mupdf.pdf_dict_puts(obj, "Border", borderArr);
        }

        /// <summary>
        /// Set link colors (PyMuPDF <c>Link.set_colors</c>).
        /// <para>Links have no fill color; only <c>stroke</c> is applied. Empty stroke removes <c>C</c>.</para>
        /// </summary>
        public void SetColors(Dictionary<string, object> colors)
        {
            int xref = Xref;
            if (xref == 0) return;
            var pdf = RequirePage().RequireParent().NativePdfDocument;
            var obj = mupdf.mupdf.pdf_resolve_indirect(mupdf.mupdf.pdf_new_indirect(pdf, xref, 0));

            if (colors == null || !colors.ContainsKey("stroke"))
            {
                mupdf.mupdf.pdf_dict_dels(obj, "C");
                return;
            }

            var strokeColor = colors["stroke"] as float[];
            if (strokeColor == null) return;

            var cArr = mupdf.mupdf.pdf_new_array(pdf, strokeColor.Length);
            foreach (float c in strokeColor)
                mupdf.mupdf.pdf_array_push_real(cArr, c);
            mupdf.mupdf.pdf_dict_puts(obj, "C", cArr);
        }

        /// <summary>Set link flags (PyMuPDF <c>Link.set_flags</c>). Requires a PDF document.</summary>
        public void SetFlags(int flags)
        {
            if (!RequirePage().RequireParent().IsPdf)
                throw new ValueErrorException("is no PDF");
            int xref = Xref;
            if (xref == 0) return;
            var pdf = RequirePage().RequireParent().NativePdfDocument;
            var obj = mupdf.mupdf.pdf_resolve_indirect(mupdf.mupdf.pdf_new_indirect(pdf, xref, 0));
            mupdf.mupdf.pdf_dict_put_int(obj, mupdf.mupdf.pdf_new_name("F"), flags);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!_detached && Parent != null)
                return "link on " + Parent;
            return $"Link('{Uri}' at {Rect})";
        }

        // ─── PyMuPDF API names (internal, same assembly) ─────────────────

        internal void _erase() => Erase();

        internal bool is_external() => IsExternal;

        internal string id => Id;

        internal int xref => Xref;

        internal Link next => Next;

        internal string uri => Uri;

        internal int page => Page;

        internal Rect rect => Rect;

        internal LinkDest dest => Dest;

        internal Dictionary<string, object> to_dict() => ToDictionary();

        internal int get_flags() => Flags;

        internal Dictionary<string, object> get_border() => Border;

        internal Dictionary<string, object> get_colors() => Colors;

        internal void set_border(Dictionary<string, object> border) => SetBorder(border);

        internal void set_colors(Dictionary<string, object> colors) => SetColors(colors);

        internal void set_flags(int flags) => SetFlags(flags);
    }
}
