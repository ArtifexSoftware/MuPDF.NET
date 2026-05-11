using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a link on a document page.
    /// </summary>
    public class Link
    {
        private mupdf.FzLink _nativeLink;
        private bool _detached;
        internal Page? Parent { get; private set; }
        /// <summary>Python <c>Page.load_links</c> / <c>Link.next</c>: xref + <c>/NM</c> when matched to the PDF link-annot list; otherwise rect-based xref resolution.</summary>
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

        /// <summary>
        /// Link rectangle.
        /// </summary>
        public Rect Rect
        {
            get
            {
                var r = _nativeLink.m_internal.rect;
                return new Rect(r.x0, r.y0, r.x1, r.y1);
            }
        }

        /// <summary>
        /// Link URI string.
        /// </summary>
        public string Uri => _nativeLink.m_internal.uri;

        /// <summary>
        /// Check if link target is external.
        /// </summary>
        /// <remarks>PyMuPDF <c>Link.is_external</c> uses <c>mupdf.fz_is_external_link</c> on the native URI.</remarks>
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

        /// <summary>
        /// Link xref number (PDF only).
        /// </summary>
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

        /// <summary>Python <c>Link.dest</c> — destination details as <see cref="LinkDest"/>.</summary>
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

        /// <summary>
        /// Link flags.
        /// </summary>
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

        /// <summary>
        /// Next link.
        /// </summary>
        public Link Next
        {
            get
            {
                var nextNative = new mupdf.FzLink(_nativeLink.m_internal.next);
                if (nextNative.m_internal == null) return null;
                var val = new Link(nextNative, RequirePage());
                int prevXref = Xref;
                if (prevXref > 0)
                {
                    try
                    {
                        var page = RequirePage();
                        var items = Helpers.JM_get_annot_xref_list(page.NativePdfPage.obj());
                        var linkAnnots = new List<(int xref, string nm)>();
                        foreach (var t in items)
                        {
                            if (t.type_ == (int)mupdf.pdf_annot_type.PDF_ANNOT_LINK)
                                linkAnnots.Add((t.xref, t.nm ?? ""));
                        }
                        for (int i = 0; i < linkAnnots.Count - 1; i++)
                        {
                            if (linkAnnots[i].xref == prevXref)
                            {
                                val.SetLinkAnnotIdentity(linkAnnots[i + 1].xref, linkAnnots[i + 1].nm);
                                break;
                            }
                        }
                    }
                    catch { }
                }
                return val;
            }
        }

        /// <summary>
        /// Target page number for internal links.
        /// </summary>
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

        /// <summary>
        /// Return link as a dictionary.
        /// </summary>
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

        /// <summary>Python <c>Link._erase</c>: detach wrapper from the owning <see cref="Page"/>.</summary>
        public void _erase()
        {
            if (_detached) return;
            _detached = true;
            Parent = null;
        }

        /// <summary>
        /// Set link border properties.
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
        /// Set link colors.
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

        /// <summary>
        /// Set link flags.
        /// </summary>
        public void SetFlags(int flags)
        {
            int xref = Xref;
            if (xref == 0) return;
            var pdf = RequirePage().RequireParent().NativePdfDocument;
            var obj = mupdf.mupdf.pdf_resolve_indirect(mupdf.mupdf.pdf_new_indirect(pdf, xref, 0));
            mupdf.mupdf.pdf_dict_put_int(obj, mupdf.mupdf.pdf_new_name("F"), flags);
        }

        /// <summary>
        /// Returns a string representation of this link.
        /// </summary>
        public override string ToString() => $"Link('{Uri}' at {Rect})";

        // Python/legacy compatibility aliases (mirrors _alias(Link, ...) and common snake_case access).
        public bool is_external() => IsExternal;
        public bool isExternal() => is_external();
        public string id => Id;
        public int xref => Xref;
        public Link next => Next;
        public string uri => Uri;
        public int page => Page;
        public Rect rect => Rect;
        public LinkDest dest => Dest;
        public Dictionary<string, object> to_dict() => ToDictionary();
        /// <summary>Python <c>link.flags</c> getter; use this because a <c>flags</c> property would reserve <c>set_flags</c>.</summary>
        public int get_flags() => Flags;
        public Dictionary<string, object> get_border() => Border;
        public Dictionary<string, object> get_colors() => Colors;
        public void set_border(Dictionary<string, object> border) => SetBorder(border);
        public void setBorder(Dictionary<string, object> border) => set_border(border);
        public void set_colors(Dictionary<string, object> colors) => SetColors(colors);
        public void setColors(Dictionary<string, object> colors) => set_colors(colors);
        /// <summary>Python <c>Link.set_flags</c>. Use <see cref="Flags"/> for the getter (<c>flags</c> property name is not used here: it would reserve <c>set_flags</c> as a property accessor).</summary>
        public void set_flags(int flags) => SetFlags(flags);
        public void setFlags(int flags) => set_flags(flags);
    }
}
