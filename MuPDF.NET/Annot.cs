using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Threading;
using mupdf;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a PDF annotation bound to a page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ports PyMuPDF <c>class Annot</c>. An annotation associates an object (note, sound, link, etc.)
    /// with a location on a page. It is always tied to its <see cref="Parent"/> page; if the page
    /// or document becomes invalid, wrappers may throw when accessed.
    /// </para>
    /// <para>
    /// Legacy MuPDF.NET names and DTOs (<see cref="AnnotInfo"/>, <see cref="Color"/> colors,
    /// <c>GetPixmap</c> with <c>dpi</c>, etc.) are on the <c>Annot.Legacy.cs</c> partial —
    /// see <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Annot.html"/>.
    /// </para>
    /// </remarks>
    public partial class Annot : IDisposable
    {
        private static int _nextAnnotRefId;
        private mupdf.PdfAnnot _nativeAnnot;
        private bool _disposed;
        public Page Parent { get; }

        /// <summary>
        /// Stable identity for <see cref="Page"/> annot-ref bookkeeping (Python uses <c>id(annot)</c> keys in <c>Page._annot_refs</c>).
        /// </summary>
        internal int AnnotRefId { get; }

        /// <summary>PyMuPDF <c>Annot._yielded</c> (set by <c>Page.annots()</c>).</summary>
        public bool Yielded { get; set; }

        internal mupdf.PdfAnnot NativeAnnot
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Annot));
                return _nativeAnnot;
            }
        }

        internal Annot(mupdf.PdfAnnot annot, Page page)
        {
            _nativeAnnot = annot;
            Parent = page;
            AnnotRefId = Interlocked.Increment(ref _nextAnnotRefId);
            page.RegisterAnnotRef(this);
        }

        private mupdf.PdfObj AnnotObj
        {
            get
            {
                EnsurePageBound();
                return mupdf.mupdf.pdf_annot_obj(NativeAnnot);
            }
        }

        /// <summary>PyMuPDF: annot use after its page wrapper was erased (e.g. after <see cref="Document.NewPage"/>).</summary>
        private void EnsurePageBound()
        {
            if (Parent == null || Parent.py_this_is_none())
                throw new InvalidOperationException("annotation not bound to any page");
        }
        private mupdf.PdfDocument ParentPdfDocument => Parent.Parent.NativePdfDocument;
        private Matrix RotatePageMatrix => Helpers.RotatePageMatrix(Parent);
        private Matrix DerotatePageMatrix => Helpers.DerotatePageMatrix(Parent);
        private Matrix VertexMatrix => Helpers.AnnotVertexMatrix(Parent);
        private bool IsWidgetAnnot => AnnotationType == AnnotationType.Widget;

        // ─── Properties ─────────────────────────────────────────────────

        /// <summary>
        /// Type of the annotation: numeric PDF type, name string, and optional intent.
        /// </summary>
        /// <remarks>
        /// Legacy code used <c>(PdfAnnotType, string, string)</c>; use <see cref="AnnotTypeInfo.Item1"/>,
        /// <see cref="AnnotTypeInfo.Item2"/>, and <see cref="AnnotTypeInfo.Item3"/> for the same layout.
        /// </remarks>
        public AnnotTypeInfo Type
        {
            get
            {
                var t = mupdf.mupdf.pdf_annot_type(NativeAnnot);
                string name = mupdf.mupdf.pdf_string_from_annot_type(t);
                string intent = null;
                var annotObj = AnnotObj;
                var itObj = mupdf.mupdf.pdf_dict_get(annotObj, mupdf.mupdf.pdf_new_name("IT"));
                if (itObj.m_internal != null && mupdf.mupdf.pdf_is_name(itObj) != 0)
                    intent = mupdf.mupdf.pdf_to_name(itObj);
                return new AnnotTypeInfo((PdfAnnotType)(int)t, name, intent);
            }
        }

        /// <summary>PyMuPDF-style annotation type enum.</summary>
        public AnnotationType AnnotationType => Type;

        /// <summary>Annotation type name string (PyMuPDF <c>Annot.type</c>).</summary>
        public string TypeString => Type.Name;

        /// <summary>
        /// Rectangle containing the annotation in page coordinates.
        /// </summary>
        /// <remarks>Transformed by the page derotation matrix (PyMuPDF <c>Annot.rect</c>).</remarks>
        public Rect Rect
        {
            get
            {
                var r = mupdf.mupdf.pdf_bound_annot(NativeAnnot);
                // Python: "val *= p.derotation_matrix"
                return Helpers.TransformRect(new Rect(r), DerotatePageMatrix);
            }
        }

        /// <summary>Annotation xref number (PyMuPDF <c>Annot.xref</c>).</summary>
        public int Xref => mupdf.mupdf.pdf_to_num(mupdf.mupdf.pdf_annot_obj(NativeAnnot));

        /// <summary>Flags field (PyMuPDF <c>Annot.flags</c> / <c>set_flags</c>).</summary>
        public int Flags
        {
            get => mupdf.mupdf.pdf_annot_flags(NativeAnnot);
            set => mupdf.mupdf.pdf_set_annot_flags(NativeAnnot, value);
        }

        /// <summary>Annotation contents (PyMuPDF <c>Annot.info</c> / PDF /Contents).</summary>
        public string Contents
        {
            get => mupdf.mupdf.pdf_annot_contents(NativeAnnot);
            set
            {
                mupdf.mupdf.pdf_set_annot_contents(NativeAnnot, value);
                mupdf.mupdf.pdf_update_annot(NativeAnnot);
            }
        }

        /// <summary>Whether the annotation has a Popup (PyMuPDF <c>Annot.has_popup</c>).</summary>
        public bool HasPopup
        {
            get
            {
                var popup = mupdf.mupdf.pdf_dict_gets(AnnotObj, "Popup");
                return popup.m_internal != null;
            }
        }

        /// <summary>Annotation Popup rectangle (PyMuPDF <c>Annot.popup_rect</c>).</summary>
        public Rect PopupRect
        {
            get
            {
                var rect = Rect.Infinite;
                var popup = mupdf.mupdf.pdf_dict_gets(AnnotObj, "Popup");
                if (popup.m_internal != null)
                    rect = new Rect(mupdf.mupdf.pdf_dict_get_rect(popup, mupdf.mupdf.pdf_new_name("Rect")));
                // Python: val = Rect(val) * transformation_matrix; val *= derotation_matrix
                rect = Helpers.TransformRect(rect, Parent.TransformationMatrix);
                rect = Helpers.TransformRect(rect, DerotatePageMatrix);
                return rect;
            }
        }

        /// <summary>Annotation Popup xref (PyMuPDF <c>Annot.popup_xref</c>).</summary>
        public int PopupXref
        {
            get
            {
                if (!HasPopup) return 0;
                var popup = mupdf.mupdf.pdf_dict_gets(AnnotObj, "Popup");
                return mupdf.mupdf.pdf_to_num(popup);
            }
        }

        /// <summary>Open status of annotation or its Popup (PyMuPDF <c>Annot.is_open</c> / <c>set_open</c>).</summary>
        public bool IsOpen
        {
            get
            {
                return mupdf.mupdf.pdf_annot_is_open(NativeAnnot) != 0;
            }
            set => mupdf.mupdf.pdf_set_annot_is_open(NativeAnnot, value ? 1 : 0);
        }

        /// <summary>Opacity (PyMuPDF <c>Annot.opacity</c> / <c>set_opacity</c>).</summary>
        public float Opacity
        {
            get
            {
                var ca = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("CA"));
                return mupdf.mupdf.pdf_is_number(ca) != 0 ? mupdf.mupdf.pdf_to_real(ca) : -1;
            }
            set
            {
                SetOpacity(value);
            }
        }

        /// <summary>Border width from <see cref="Border"/> (PyMuPDF <c>Annot.border</c> width).</summary>
        public float BorderWidth
        {
            get
            {
                var border = Border;
                return border != null && border.Width > 0 ? border.Width : -1;
            }
        }

        /// <summary>Annotation unique id /NM (PyMuPDF <c>Annot.info</c> id).</summary>
        public string Id
        {
            get
            {
                var nm = mupdf.mupdf.pdf_dict_gets(mupdf.mupdf.pdf_annot_obj(NativeAnnot), "NM");
                return nm.m_internal != null ? mupdf.mupdf.pdf_to_text_string(nm) : null;
            }
        }

        /// <summary>Next annotation on the page (PyMuPDF <c>Annot.next</c>).</summary>
        public Annot Next
        {
            get
            {
                var next = IsWidgetAnnot
                    ? mupdf.mupdf.pdf_next_widget(NativeAnnot)
                    : mupdf.mupdf.pdf_next_annot(NativeAnnot);
                return next.m_internal != null ? new Annot(next, Parent) : null;
            }
        }


        /// <summary>Stroke color components (PyMuPDF <c>Annot.colors</c> stroke). Legacy <c>Colors</c> DTO: <see cref="Annot.Legacy.cs"/>.</summary>
        public float[] StrokeColor
        {
            get => ColorInfo.TryGetValue("stroke", out var stroke) ? (float[])stroke : Array.Empty<float>();
        }

        /// <summary>Interior (fill) color components (PyMuPDF <c>Annot.colors</c> fill).</summary>
        public float[] InteriorColor
        {
            get => ColorInfo.TryGetValue("fill", out var fill) ? (float[])fill : Array.Empty<float>();
        }

        /// <summary>Color definitions (PyMuPDF <c>Annot.colors</c>).</summary>
        public Dictionary<string, object> ColorInfo
        {
            get => Helpers.JM_annot_colors(AnnotObj);
        }

        /// <summary>Border information (PyMuPDF <c>Annot.border</c>).</summary>
        public Border Border
        {
            get
            {
                var annotType = AnnotationType;
                if (annotType != AnnotationType.Circle
                    && annotType != AnnotationType.FreeText
                    && annotType != AnnotationType.Ink
                    && annotType != AnnotationType.Line
                    && annotType != AnnotationType.PolyLine
                    && annotType != AnnotationType.Polygon
                    && annotType != AnnotationType.Square)
                    return new Border();
                return GetBorderFromAnnot(AnnotObj, 0);
            }
        }

        /// <summary>Annotation appearance bbox (PyMuPDF <c>Annot.apn_bbox</c>).</summary>
        public Rect ApnBBox
        {
            get
            {
                var ap = GetAppearanceStreamObject("N");
                Rect value = ap.m_internal == null
                    ? Rect.Infinite
                    : new Rect(mupdf.mupdf.pdf_dict_get_rect(ap, mupdf.mupdf.pdf_new_name("BBox")));
                value = Helpers.TransformRect(value, Parent.TransformationMatrix);
                value = Helpers.TransformRect(value, DerotatePageMatrix);
                return value;
            }
        }

        /// <summary>Annotation appearance matrix (PyMuPDF <c>Annot.apn_matrix</c>).</summary>
        public Matrix ApnMatrix
        {
            get
            {
                var ap = GetAppearanceStreamObject("N");
                if (ap.m_internal == null) return new Matrix();
                return Helpers.MatrixFromFz(mupdf.mupdf.pdf_dict_get_matrix(ap, mupdf.mupdf.pdf_new_name("Matrix")));
            }
        }

        /// <summary>Annotation language (PyMuPDF <c>Annot.language</c>).</summary>
        public string Language
        {
            get
            {
                var lang = mupdf.mupdf.pdf_annot_language(NativeAnnot);
                return lang == mupdf.fz_text_language.FZ_LANG_UNSET
                    ? null
                    : mupdf.mupdf.fz_string_from_text_language2(lang);
            }
            set => SetLanguage(value);
        }

        /// <summary>Line end codes (PyMuPDF <c>Annot.line_ends</c>).</summary>
        public (int start, int end)? LineEnds
        {
            get
            {
                if (mupdf.mupdf.pdf_annot_has_line_ending_styles(NativeAnnot) == 0)
                    return null;
                return ((int)mupdf.mupdf.pdf_annot_line_start_style(NativeAnnot),
                    (int)mupdf.mupdf.pdf_annot_line_end_style(NativeAnnot));
            }
        }

        /// <summary>Annotation rotation (PyMuPDF <c>Annot.rotation</c>).</summary>
        public int Rotation
        {
            get
            {
                var rotation = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("Rotate"));
                return rotation.m_internal == null ? -1 : mupdf.mupdf.pdf_to_int(rotation);
            }
        }

        // ─── Methods ────────────────────────────────────────────────────

        /// <summary>
        /// Change the annotation rectangle (move or scale; no rotation of appearance).
        /// </summary>
        /// <param name="rect">New rectangle; must be finite and non-empty.</param>
        /// <returns>
        /// <see langword="null"/> on success, <see langword="false"/> if MuPDF rejected the change
        /// (unsupported type or internal error). Python returns <c>None</c>/<c>False</c> similarly.
        /// </returns>
        /// <exception cref="ValueErrorException">If <paramref name="rect"/> is empty or infinite.</exception>
        /// <remarks>
        /// Does not require <see cref="Update"/>. Only affects certain annotation types; others log
        /// a message and return <see langword="false"/>.
        /// </remarks>
        public bool? SetRect(Rect rect)
        {
            // CheckParent(self)
            var annot = NativeAnnot;
            // pdfpage = _pdf_annot_page(annot)
            var pdfpage = mupdf.mupdf.pdf_annot_page(annot);            
            // rot = JM_rotate_page_matrix(pdfpage)
            var rot = Helpers.JM_rotate_page_matrix(pdfpage);
            // r = mupdf.fz_transform_rect(JM_rect_from_py(rect), rot)
            var r = mupdf.mupdf.fz_transform_rect(new Rect(rect).ToFzRect(), rot.ToFzMatrix());
            if (mupdf.mupdf.fz_is_empty_rect(r) != 0 || mupdf.mupdf.fz_is_infinite_rect(r) != 0)
                throw new ValueErrorException(Constants.MSG_BAD_RECT);
            try
            {
                mupdf.mupdf.pdf_set_annot_rect(annot, r);
                return null;
            }
            catch (Exception e)
            {
                Trace.WriteLine($"cannot set rect: {e}");
                return false;
            }
        }

        /// <summary>
        /// Change border width, style, dashing, or cloud effect (PDF only).
        /// </summary>
        /// <param name="width">Border width; <c>-1</c> leaves unchanged. Must be non-negative when set.</param>
        /// <param name="style">Border style letter (<c>"S"</c>, <c>"D"</c>, etc.); <c>null</c> leaves unchanged.</param>
        /// <param name="dashes">Dash pattern as integers; empty array removes dashing and sets style <c>"D"</c>.</param>
        /// <remarks>
        /// Supported for Circle, FreeText, Ink, Line, PolyLine, Polygon, and Square annotations.
        /// Use the dictionary overload for cloud borders. See <see cref="Border"/>.
        /// </remarks>
        public void SetBorder(float width = -1, string style = null, int[] dashes = null)
        {
            SetBorder(null, width, style, dashes, -1);
        }

        /// <summary>
        /// Change border width and dash pattern (dash values as floats, converted to integers).
        /// </summary>
        /// <param name="width">Border width; <c>-1</c> leaves unchanged.</param>
        /// <param name="dashes">Dash lengths in user units.</param>
        public void SetBorder(float width, float[] dashes)
            => SetBorder(width, null, Array.ConvertAll(dashes, x => (int)x));

        /// <summary>
        /// Change border properties using a dictionary and/or individual fields.
        /// </summary>
        /// <param name="border">
        /// Dictionary with optional keys <c>width</c>, <c>style</c>, <c>dashes</c>, <c>clouds</c>
        /// (as returned by <see cref="Border"/>). <c>null</c> uses only the other parameters.
        /// </param>
        /// <param name="width">Border width when not present in <paramref name="border"/>.</param>
        /// <param name="style">Border style when not present in <paramref name="border"/>.</param>
        /// <param name="dashes">Dash array when not present in <paramref name="border"/>.</param>
        /// <param name="clouds">
        /// Cloud border intensity for Square, Circle, and Polygon; <c>0</c> removes clouds.
        /// </param>
        /// <remarks>
        /// Omitted keys leave the corresponding property unchanged. Non-integer dash entries are ignored.
        /// </remarks>
        public void SetBorder(Dictionary<string, object> border, float width = -1, string style = null, int[] dashes = null, float clouds = -1)
        {
            var annotType = AnnotationType;
            if (annotType != AnnotationType.Circle
                && annotType != AnnotationType.FreeText
                && annotType != AnnotationType.Ink
                && annotType != AnnotationType.Line
                && annotType != AnnotationType.PolyLine
                && annotType != AnnotationType.Polygon
                && annotType != AnnotationType.Square)
                return;

            if (annotType != AnnotationType.Circle
                && annotType != AnnotationType.FreeText
                && annotType != AnnotationType.Polygon
                && annotType != AnnotationType.Square
                && clouds > 0)
                clouds = -1;

            border ??= new Dictionary<string, object>();
            if (!border.ContainsKey("width")) border["width"] = width;
            if (!border.ContainsKey("style")) border["style"] = style;
            if (!border.ContainsKey("dashes")) border["dashes"] = dashes;
            if (!border.ContainsKey("clouds")) border["clouds"] = clouds;
            if (border["width"] == null) border["width"] = -1f;
            if (border["clouds"] == null) border["clouds"] = -1f;
            if (annotType != AnnotationType.Circle
                && annotType != AnnotationType.FreeText
                && annotType != AnnotationType.Polygon
                && annotType != AnnotationType.Square
                && border.TryGetValue("clouds", out var cloudObj)
                && cloudObj != null
                && Convert.ToSingle(cloudObj) > 0)
                border["clouds"] = -1f;

            if (border.TryGetValue("dashes", out var dashObj) && dashObj is System.Collections.IEnumerable seq && dashObj is not string)
            {
                var parsed = new List<int>();
                bool ok = true;
                foreach (var item in seq)
                {
                    if (item is int di) parsed.Add(di);
                    else { ok = false; break; }
                }
                border["dashes"] = ok ? parsed.ToArray() : null;
            }
            Helpers.JM_annot_set_border(border, ParentPdfDocument, AnnotObj);
        }

        /// <summary>
        /// Set stroke and fill colors (PyMuPDF <c>Annot.set_colors</c>).
        /// <para>Use either a dictionary or the direct <paramref name="stroke"/> / <paramref name="fill"/> arguments.</para>
        /// </summary>
        public void SetColors(float[] stroke = null, float[] fill = null)
        {
            SetColors((Dictionary<string, float[]>)null, stroke, fill);
        }

        /// <inheritdoc cref="SetColors(float[], float[])"/>
        public void SetColors(Dictionary<string, object> colors, object stroke = null, object fill = null)
        {
            colors ??= new Dictionary<string, object>
            {
                ["fill"] = fill,
                ["stroke"] = stroke,
            };
            var strokeObj = colors.TryGetValue("stroke", out var s) ? s : stroke;
            var fillObj = colors.TryGetValue("fill", out var f) ? f : fill;
            SetColors((Dictionary<string, float[]>)null, NormalizeColorSequence(strokeObj), NormalizeColorSequence(fillObj));
        }

        /// <inheritdoc cref="SetColors(float[], float[])"/>
        public void SetColors(Dictionary<string, float[]> colors, float[] stroke = null, float[] fill = null)
        {
            if (AnnotationType == AnnotationType.FreeText)
                throw new ArgumentException("cannot be used for FreeText annotations");

            colors ??= new Dictionary<string, float[]>
            {
                ["fill"] = fill,
                ["stroke"] = stroke,
            };

            fill = colors.TryGetValue("fill", out var fillColor) ? fillColor : null;
            stroke = colors.TryGetValue("stroke", out var strokeColor) ? strokeColor : null;

            if (stroke != null)
            {
                if (stroke.Length == 0)
                    Parent.Parent.XrefSetKey(Xref, "C", "[]");
                else
                {
                    Helpers.CheckColor(stroke);
                    Parent.Parent.XrefSetKey(Xref, "C", Helpers.EscapePdfArray(stroke));
                }
            }

            bool allowFill = AnnotationType == AnnotationType.Circle
                || AnnotationType == AnnotationType.Square
                || AnnotationType == AnnotationType.Line
                || AnnotationType == AnnotationType.PolyLine
                || AnnotationType == AnnotationType.Polygon
                || AnnotationType == AnnotationType.Redact;

            if (fill != null)
            {
                if (!allowFill)
                    return;
                if (fill.Length == 0)
                    Parent.Parent.XrefSetKey(Xref, "IC", "[]");
                else
                {
                    Helpers.CheckColor(fill);
                    Parent.Parent.XrefSetKey(Xref, "IC", Helpers.EscapePdfArray(fill));
                }
            }
        }

        /// <summary>Set opacity (PyMuPDF <c>Annot.set_opacity</c>).</summary>
        public void SetOpacity(float opacity)
        {
            if (!Helpers.InRange(opacity, 0.0f, 1.0f))
            {
                mupdf.mupdf.pdf_set_annot_opacity(NativeAnnot, 1);
                return;
            }
            mupdf.mupdf.pdf_set_annot_opacity(NativeAnnot, opacity);
            if (opacity < 1.0)
            {
                var page = mupdf.mupdf.pdf_annot_page(NativeAnnot);
                var pdfPage = page.m_internal;
                if (pdfPage != null)
                    pdfPage.transparency = 1;
            }
        }

        /// <summary>Set /Name (icon) of annotation (PyMuPDF <c>Annot.set_name</c>).</summary>
        public void SetName(string name)
        {
            mupdf.mupdf.pdf_dict_put_name(AnnotObj, mupdf.mupdf.pdf_new_name("Name"), name);
        }

        /// <summary>Get /Name (icon) of annotation (from <c>Annot.info</c> name field).</summary>
        public string GetName()
        {
            var obj = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("Name"));
            return obj.m_internal != null ? mupdf.mupdf.pdf_to_name(obj) : "";
        }

        /// <summary>Set annotation BlendMode (PyMuPDF <c>Annot.set_blendmode</c>).</summary>
        public void SetBlendMode(string mode)
        {
            mupdf.mupdf.pdf_dict_put_name(AnnotObj, mupdf.mupdf.pdf_new_name("BM"), mode);
        }

        /// <summary>Annotation BlendMode (PyMuPDF <c>Annot.blendmode</c>).</summary>
        public string BlendMode
        {
            get
            {
                var bm = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("BM"));
                if (bm.m_internal != null) return mupdf.mupdf.pdf_to_name(bm);

                var extState = GetNestedDict(AnnotObj, "AP", "N", "Resources", "ExtGState");
                if (mupdf.mupdf.pdf_is_dict(extState) != 0)
                {
                    int n = mupdf.mupdf.pdf_dict_len(extState);
                    for (int i = 0; i < n; i++)
                    {
                        var state = mupdf.mupdf.pdf_dict_get_val(extState, i);
                        if (mupdf.mupdf.pdf_is_dict(state) == 0) continue;
                        int m = mupdf.mupdf.pdf_dict_len(state);
                        for (int j = 0; j < m; j++)
                        {
                            var key = mupdf.mupdf.pdf_dict_get_key(state, j);
                            if (mupdf.mupdf.pdf_objcmp(key, mupdf.mupdf.pdf_new_name("BM")) == 0)
                                return mupdf.mupdf.pdf_to_name(mupdf.mupdf.pdf_dict_get_val(state, j));
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>Annotation author / title (PyMuPDF <c>Annot.info</c> title).</summary>
        public string Author
        {
            get
            {
                var title = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("T"));
                return title.m_internal != null ? mupdf.mupdf.pdf_to_text_string(title) : "";
            }
            set
            {
                mupdf.mupdf.pdf_set_annot_author(NativeAnnot, value);
            }
        }

        /// <summary>Creation date (PyMuPDF <c>Annot.info</c> creationDate).</summary>
        public string CreationDate
        {
            get
            {
                var cd = mupdf.mupdf.pdf_dict_gets(mupdf.mupdf.pdf_annot_obj(NativeAnnot), "CreationDate");
                return cd.m_internal != null ? mupdf.mupdf.pdf_to_text_string(cd) : "";
            }
        }

        /// <summary>Modification date (PyMuPDF <c>Annot.info</c> modDate).</summary>
        public string ModDate
        {
            get
            {
                var md = mupdf.mupdf.pdf_dict_gets(mupdf.mupdf.pdf_annot_obj(NativeAnnot), "M");
                return md.m_internal != null ? mupdf.mupdf.pdf_to_text_string(md) : "";
            }
        }

        /// <summary>Set various properties (PyMuPDF <c>Annot.set_info</c>).</summary>
        public void SetInfo(string content = null, string title = null, string creationDate = null, string modDate = null, string subject = null)
        {
            SetInfo((Dictionary<string, string>)null, content, title, creationDate, modDate, subject);
        }

        /// <summary>Set various properties from <see cref="AnnotInfo"/> (MuPDF.NET / <c>annot.SetInfo(info)</c>).</summary>
        public void SetInfo(
            AnnotInfo info = null,
            string content = null,
            string title = null,
            string creationDate = null,
            string modDate = null,
            string subject = null)
        {
            if (info != null)
            {
                content = info.Content;
                title = info.Title;
                creationDate = info.CreationDate;
                modDate = info.ModDate;
                subject = info.Subject;
            }
            SetInfo((Dictionary<string, string>)null, content, title, creationDate, modDate, subject);
        }

        /// <inheritdoc cref="SetInfo(string, string, string, string, string)"/>
        public void SetInfo(Dictionary<string, string> info = null, string content = null, string title = null, string creationDate = null, string modDate = null, string subject = null)
        {
            if (info != null)
            {
                info.TryGetValue("content", out content);
                info.TryGetValue("title", out title);
                info.TryGetValue("creationDate", out creationDate);
                info.TryGetValue("modDate", out modDate);
                info.TryGetValue("subject", out subject);
            }

            if (!string.IsNullOrEmpty(content))
                mupdf.mupdf.pdf_set_annot_contents(NativeAnnot, content);
            bool isMarkup = mupdf.mupdf.pdf_annot_has_author(NativeAnnot) != 0;
            if (isMarkup)
            {
                if (!string.IsNullOrEmpty(title)) Author = title;
                if (!string.IsNullOrEmpty(creationDate))
                    mupdf.mupdf.pdf_dict_put_text_string(AnnotObj,
                        mupdf.mupdf.pdf_new_name("CreationDate"), creationDate);
                if (!string.IsNullOrEmpty(modDate))
                    mupdf.mupdf.pdf_dict_put_text_string(AnnotObj,
                        mupdf.mupdf.pdf_new_name("M"), modDate);
                if (!string.IsNullOrEmpty(subject))
                    mupdf.mupdf.pdf_dict_puts(AnnotObj,
                        "Subj", mupdf.mupdf.pdf_new_text_string(subject));
            }
        }

        /// <summary>Various information details (PyMuPDF <c>Annot.info</c>).</summary>
        public Dictionary<string, string> GetInfo()
        {
            var res = new Dictionary<string, string>();

            res["content"] = Contents;

            var nameObj = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("Name"));
            res["name"] = nameObj.m_internal != null ? mupdf.mupdf.pdf_to_name(nameObj) : "";

            // Title (= author)
            res["title"] = Author;

            // CreationDate
            res["creationDate"] = CreationDate;

            // ModDate
            res["modDate"] = ModDate;

            // Subj
            var subjObj = mupdf.mupdf.pdf_dict_gets(AnnotObj, "Subj");
            res["subject"] = subjObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(subjObj) : "";

            // Identification (PDF key /NM)
            var nmObj = mupdf.mupdf.pdf_dict_gets(AnnotObj, "NM");
            res["id"] = nmObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(nmObj) : "";

            return res;
        }

        /// <summary>Annotation vertex points (PyMuPDF <c>Annot.vertices</c>).</summary>
        public List<Point> Vertices
        {
            get
            {
                var result = new List<Point>();
                var source = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("Vertices"));
                if (source.m_internal == null) source = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("L"));
                if (source.m_internal == null) source = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("QuadPoints"));
                if (source.m_internal == null) source = mupdf.mupdf.pdf_dict_gets(AnnotObj, "CL");

                if (source.m_internal != null)
                {
                    for (int i = 0; i < mupdf.mupdf.pdf_array_len(source); i += 2)
                    {
                        float x = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(source, i));
                        float y = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(source, i + 1));
                        result.Add(Helpers.TransformPoint(new Point(x, y), VertexMatrix));
                    }
                    return result;
                }

                var inkList = mupdf.mupdf.pdf_dict_gets(AnnotObj, "InkList");
                if (inkList.m_internal != null)
                {
                    for (int i = 0; i < mupdf.mupdf.pdf_array_len(inkList); i++)
                    {
                        var sub = mupdf.mupdf.pdf_array_get(inkList, i);
                        for (int j = 0; j < mupdf.mupdf.pdf_array_len(sub); j += 2)
                        {
                            float x = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(sub, j));
                            float y = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(sub, j + 1));
                            result.Add(Helpers.TransformPoint(new Point(x, y), VertexMatrix));
                        }
                    }
                }
                return result;
            }
        }


        /// <summary>Line annotation endpoints (MuPDF <c>pdf_annot_line</c>; no named PyMuPDF API).</summary>
        public (Point, Point) Line
        {
            get
            {
                var p1 = new mupdf.FzPoint();
                var p2 = new mupdf.FzPoint();
                NativeAnnot.pdf_annot_line(p1, p2);
                return (Helpers.TransformPoint(new Point(p1), VertexMatrix), Helpers.TransformPoint(new Point(p2), VertexMatrix));
            }
        }

        /// <summary>Set line endpoints (MuPDF <c>pdf_set_annot_line</c>; no named PyMuPDF API).</summary>
        public void SetLine(Point p1, Point p2)
        {
            var rp1 = Helpers.TransformPoint(new Point(p1), RotatePageMatrix);
            var rp2 = Helpers.TransformPoint(new Point(p2), RotatePageMatrix);
            mupdf.mupdf.pdf_set_annot_line(NativeAnnot, rp1.ToFzPoint(), rp2.ToFzPoint());
        }

        /// <summary>Set annotation vertices (no direct PyMuPDF name; uses MuPDF vertex APIs).</summary>
        public void SetVertices(Point[] points)
        {
            mupdf.mupdf.pdf_clear_annot_vertices(NativeAnnot);
            foreach (var p in points)
            {
                var rp = Helpers.TransformPoint(new Point(p), RotatePageMatrix);
                mupdf.mupdf.pdf_add_annot_vertex(NativeAnnot, rp.ToFzPoint());
            }
        }

        /// <summary>
        /// Synchronize the annotation’s visual appearance with its properties after changes.
        /// </summary>
        /// <param name="blendMode">
        /// PDF blend mode for all annotation types (e.g. <c>"Normal"</c>). Use
        /// <c>PDF_BM_Normal</c> to remove a blend mode.
        /// </param>
        /// <param name="opacity">
        /// Transparency in <c>[0, 1)</c> for all types. Values outside the range are treated as opaque.
        /// </param>
        /// <param name="fontSize">Font size for FreeText annotations only.</param>
        /// <param name="fontName">Font name for FreeText annotations only.</param>
        /// <param name="textColor">
        /// Text color for FreeText (1, 3, or 4 floats, 0–1 per channel: gray, RGB, or CMYK).
        /// </param>
        /// <param name="borderColor">
        /// Border color for FreeText with rich text (<c>/RC</c> present) only.
        /// </param>
        /// <param name="fillColor">
        /// Fill color; for Line, PolyLine, and Polygon can color line-end symbols.
        /// </param>
        /// <param name="crossOut">
        /// For Redact annotations: draw diagonal lines when <see langword="true"/> (default).
        /// </param>
        /// <param name="rotate">
        /// Rotation in degrees; <c>-1</c> means unchanged. FreeText accepts 0, 90, 180, 270 only.
        /// </param>
        /// <remarks>
        /// <para>
        /// You may omit this call after <see cref="SetRect"/>, <see cref="SetFlags"/>,
        /// <see cref="SetOC"/>, <see cref="UpdateFile"/>, and most <see cref="SetInfo"/> changes
        /// (except content changes). Required after color, border, rotation, and appearance edits.
        /// </para>
        /// <para>
        /// Avoid calling inside a <see cref="Page.Annots"/> iteration loop — the page may need
        /// reloading, which is not safe there.
        /// </para>
        /// </remarks>
        public void Update(
            string? blendMode = null,
            float? opacity = null,
            float fontSize = 0,
            string? fontName = null,
            float[]? textColor = null,
            float[]? borderColor = null,
            float[]? fillColor = null,
            bool crossOut = true,
            int rotate = -1)
        {
            var annotObj = AnnotObj;
            if (borderColor != null)
            {
                var isRichText = mupdf.mupdf.pdf_dict_get(annotObj, mupdf.mupdf.pdf_new_name("RC"));
                if (isRichText.m_internal == null)
                    throw new ArgumentException("cannot set border_color if rich_text is False");
            }
            UpdateTimingTest();

            var annotType = AnnotationType;
            var border = Border;
            var dashes = border.Dashes;
            float borderWidth = border.Width;
            float[] stroke = StrokeColor;
            float[] fill = fillColor ?? InteriorColor;
            Matrix apnMatrix = ApnMatrix;
            Rect? lineEndRect = null;
            int lineEndLe = 0;
            int lineEndRi = 0;
            var lineEndsVal = LineEnds;
            if (lineEndsVal != null)
            {
                lineEndLe = lineEndsVal.Value.start;
                lineEndRi = lineEndsVal.Value.end;
            }
            Matrix iMat = Parent.TransformationMatrix.Inverted() ?? Matrix.Identity;

            if (rotate != -1)
            {
                while (rotate < 0) rotate += 360;
                while (rotate >= 360) rotate -= 360;
                if (annotType == AnnotationType.FreeText && rotate % 90 != 0)
                    rotate = 0;
            }

            string effectiveBlendMode = blendMode ?? BlendMode;
            float effectiveOpacity = opacity ?? Opacity;
            string opaCode = (effectiveOpacity >= 0 && effectiveOpacity < 1) || !string.IsNullOrEmpty(effectiveBlendMode)
                ? "/H gs\n" // Python: "then we must reference this 'gs'"
                : "";

            if (annotType == AnnotationType.FreeText)
            {
                Helpers.CheckColor(textColor);
                Helpers.CheckColor(fillColor);
                var (tcol, fname, fsize) = Helpers.ParseAnnotDefaultAppearance(NativeAnnot);
                if (fsize <= 0) fsize = 12;
                if (textColor != null) tcol = textColor;
                if (!string.IsNullOrEmpty(fontName)) fname = fontName;
                if (fontSize > 0) fsize = fontSize;
                Helpers.JM_make_annot_DA(NativeAnnot, tcol?.Length ?? 0, tcol ?? Array.Empty<float>(), fname, fsize);
                effectiveBlendMode = null; // Python: not supported for FreeText.
            }

            bool val = _update_appearance(effectiveOpacity, effectiveBlendMode, fill, rotate, annotType);
            if (!val)
                throw new InvalidOperationException("Error updating annotation.");

            // Python: "read contents as created by MuPDF"
            byte[] ap = _getAP();
            if (ap == null || ap.Length == 0)
                return;

            if (annotType == AnnotationType.FreeText)
            {
                string apPrefixCheck = Encoding.UTF8.GetString(ap);
                if (effectiveOpacity >= 0 && effectiveOpacity < 1
                    && !apPrefixCheck.StartsWith("/H gs", StringComparison.Ordinal))
                    _setAP(Encoding.UTF8.GetBytes("/H gs\n").Concat(ap).ToArray());
                return;
            }

            string apText = Encoding.UTF8.GetString(ap);
            bool apUpdated = false;

            if (annotType == AnnotationType.Redact && crossOut)
            {
                // Python: create crossed-out rect for redact annotations.
                var lines = new List<string>(apText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
                if (lines.Count >= 6)
                {
                    int n = lines.Count;
                    string ll = lines[n - 4];
                    string lr = lines[n - 3];
                    string ur = lines[n - 2];
                    string ul = lines[n - 5];
                    lines.RemoveAt(lines.Count - 1);
                    lines.Add(lr);
                    lines.Add(ll);
                    lines.Add(ur);
                    lines.Add(ll);
                    lines.Add(ul);
                    lines.Add("S");
                    apText = string.Join("\n", lines);
                    apUpdated = true;
                }
            }

            string fillCode = Helpers.ColorCode(fill, "f");
            string strokeCode = Helpers.ColorCode(stroke, "c");

            if (annotType == AnnotationType.Polygon || annotType == AnnotationType.PolyLine)
            {
                var tab = apText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                if (tab.Length > 0)
                    apText = string.Join("\n", tab.Take(tab.Length - 1)) + "\n";
                if (!string.IsNullOrEmpty(fillCode))
                    apText += fillCode + (annotType == AnnotationType.Polygon ? "b" : "S");
                else
                    apText += annotType == AnnotationType.Polygon ? "s" : "S";
                apUpdated = true;
            }

            if (dashes != null && dashes.Length > 0)
            {
                apText = "[" + string.Join(" ", dashes) + "] 0 d\n" + apText;
                apText = apText.Replace("\nS\n", "\nS\n[] 0 d\n");
                apUpdated = true;
            }
            if (!string.IsNullOrEmpty(opaCode))
            {
                apText = opaCode + apText;
                apUpdated = true;
            }

            if (annotType == AnnotationType.Redact && (borderWidth > 0 || !string.IsNullOrEmpty(strokeCode)))
            {
                var rebuilt = new StringBuilder();
                if (borderWidth > 0)
                    rebuilt.AppendLine(borderWidth.ToString("G", System.Globalization.CultureInfo.InvariantCulture) + " w");
                foreach (var line in apText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    if (line.EndsWith("w", StringComparison.Ordinal)) continue;
                    rebuilt.AppendLine(line.EndsWith("RG", StringComparison.Ordinal) && !string.IsNullOrEmpty(strokeCode)
                        ? strokeCode.TrimEnd()
                        : line);
                }
                apText = rebuilt.ToString();
                apUpdated = true;
            }

            apText = "q\n" + apText.TrimEnd() + "\nQ\n";

            if ((lineEndLe + lineEndRi) > 0
                && (annotType == AnnotationType.Polygon || annotType == AnnotationType.PolyLine))
            {
                apText = AppendLineEndSymbols(this, apText, lineEndLe, lineEndRi, iMat, fill, ref lineEndRect);
                apUpdated = true;
            }

            if (apUpdated)
            {
                byte[] updated = Encoding.UTF8.GetBytes(apText);
                if (lineEndRect != null)
                {
                    SetRect(lineEndRect);
                    _setAP(updated, 1);
                }
                else
                    _setAP(updated, 0);
            }

            //-------------------------------
            // handle annotation rotations
            //-------------------------------
            if (annotType != AnnotationType.Caret
                && annotType != AnnotationType.Circle
                && annotType != AnnotationType.FileAttachment
                && annotType != AnnotationType.Ink
                && annotType != AnnotationType.Line
                && annotType != AnnotationType.PolyLine
                && annotType != AnnotationType.Polygon
                && annotType != AnnotationType.Square
                && annotType != AnnotationType.Stamp
                && annotType != AnnotationType.Text)
                return;

            int rot = Rotation; // get value from annot object
            if (rot == -1) // nothing to change
                return;

            var center = (Rect.TL + Rect.BR) / 2.0f; // center of annot rect
            if (rot == 0) // undo rotations
            {
                if (apnMatrix == Matrix.Identity)
                    return; // matrix already is a no-op
                var quad = Rect.Morph(center, apnMatrix.Inverted() ?? Matrix.Identity); // derotate rect
                SetRect(new Rect(quad.Rect));
                SetApnMatrix(Matrix.Identity); // appearance matrix = no-op
                return;
            }

            var mat = Matrix.Rotation(rot);
            var rotated = Rect.Morph(center, mat);
            SetRect(new Rect(rotated.Rect));
            SetApnMatrix(apnMatrix * mat);
        }

        /// <summary>PyMuPDF <c>Annot._update_appearance</c> — MuPDF appearance refresh and optional opacity/blend ExtGState.</summary>
        private bool _update_appearance(float opacity, string blendMode, float[] fillColor, int rotate, AnnotationType annotType)
        {
            var annotObj = AnnotObj;
            int nFill = fillColor?.Length ?? 0;

            bool supportsInterior = annotType == AnnotationType.Square
                || annotType == AnnotationType.Circle
                || annotType == AnnotationType.Line
                || annotType == AnnotationType.PolyLine
                || annotType == AnnotationType.Polygon;
            // Match PyMuPDF _update_appearance: IC handling for annots that use interior color.
            if (nFill == 0 || !supportsInterior)
                mupdf.mupdf.pdf_dict_del(annotObj, mupdf.mupdf.pdf_new_name("IC"));
            else if (nFill > 0)
            {
                var icPin = GCHandle.Alloc(fillColor, GCHandleType.Pinned);
                try
                {
                    var icPtr = new mupdf.SWIGTYPE_p_float(icPin.AddrOfPinnedObject(), false);
                    mupdf.mupdf.pdf_set_annot_interior_color(NativeAnnot, nFill, icPtr);
                }
                finally
                {
                    if (icPin.IsAllocated)
                        icPin.Free();
                }
            }

            bool insertRot = rotate >= 0;
            if (annotType != AnnotationType.Caret
                && annotType != AnnotationType.Circle
                && annotType != AnnotationType.FreeText
                && annotType != AnnotationType.FileAttachment
                && annotType != AnnotationType.Ink
                && annotType != AnnotationType.Line
                && annotType != AnnotationType.PolyLine
                && annotType != AnnotationType.Polygon
                && annotType != AnnotationType.Square
                && annotType != AnnotationType.Stamp
                && annotType != AnnotationType.Text)
                insertRot = false;
            if (insertRot)
                mupdf.mupdf.pdf_dict_put_int(annotObj, mupdf.mupdf.pdf_new_name("Rotate"), rotate);

            // FreeText fill uses border color via pdf_set_annot_color; others use /IC (see Python).
            if (annotType == AnnotationType.FreeText)
            {
                if (nFill > 0)
                {
                    var cPin = GCHandle.Alloc(fillColor, GCHandleType.Pinned);
                    try
                    {
                        var cPtr = new mupdf.SWIGTYPE_p_float(cPin.AddrOfPinnedObject(), false);
                        mupdf.mupdf.pdf_set_annot_color(NativeAnnot, nFill, cPtr);
                    }
                    finally
                    {
                        if (cPin.IsAllocated)
                            cPin.Free();
                    }
                }
            }
            else if (nFill > 0)
            {
                var col = mupdf.mupdf.pdf_new_array(ParentPdfDocument, nFill);
                for (int i = 0; i < nFill; i++)
                    mupdf.mupdf.pdf_array_push_real(col, fillColor[i]);
                mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("IC"), col);
            }

            mupdf.mupdf.pdf_dirty_annot(NativeAnnot);
            mupdf.mupdf.pdf_update_annot(NativeAnnot);

            if ((opacity < 0 || opacity >= 1) && string.IsNullOrEmpty(blendMode))
                return true;

            var ap = Helpers.PdfDictGetl(annotObj, mupdf.mupdf.pdf_new_name("AP"), mupdf.mupdf.pdf_new_name("N"));
            if (ap.m_internal == null) return true;
            var resources = mupdf.mupdf.pdf_dict_get(ap, mupdf.mupdf.pdf_new_name("Resources"));
            if (resources.m_internal == null)
                resources = mupdf.mupdf.pdf_dict_put_dict(ap, mupdf.mupdf.pdf_new_name("Resources"), 2);

            var alp0 = mupdf.mupdf.pdf_new_dict(ParentPdfDocument, 3);
            if (opacity >= 0 && opacity < 1)
            {
                mupdf.mupdf.pdf_dict_put_real(alp0, mupdf.mupdf.pdf_new_name("CA"), opacity);
                mupdf.mupdf.pdf_dict_put_real(alp0, mupdf.mupdf.pdf_new_name("ca"), opacity);
                mupdf.mupdf.pdf_dict_put_real(annotObj, mupdf.mupdf.pdf_new_name("CA"), opacity);
            }
            if (!string.IsNullOrEmpty(blendMode))
            {
                mupdf.mupdf.pdf_dict_put_name(alp0, mupdf.mupdf.pdf_new_name("BM"), blendMode);
                mupdf.mupdf.pdf_dict_put_name(annotObj, mupdf.mupdf.pdf_new_name("BM"), blendMode);
            }
            var extg = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("ExtGState"));
            if (extg.m_internal == null)
                extg = mupdf.mupdf.pdf_dict_put_dict(resources, mupdf.mupdf.pdf_new_name("ExtGState"), 2);
            mupdf.mupdf.pdf_dict_put(extg, mupdf.mupdf.pdf_new_name("H"), alp0);
            return true;
        }

        /// <summary>PyMuPDF <c>Annot.update_timing_test</c> — no-op timing stub called from <see cref="Update"/>.</summary>
        private static void UpdateTimingTest()
        {
            int total = 0;
            for (int i = 0; i < 30 * 1000; i++) total += i;
        }

        /// <summary>
        /// Renders the annotation into a <see cref="Pixmap"/> (PyMuPDF <c>Annot.get_pixmap</c>).
        /// </summary>
        /// <param name="matrix">Transform matrix; ignored when <paramref name="dpi"/> is non-zero.</param>
        /// <param name="cs">Target colorspace; default RGB.</param>
        /// <param name="alpha">Include an alpha channel when <see langword="true"/>.</param>
        /// <param name="dpi">When non-zero, builds a scale matrix from <c>dpi / 72</c> and sets pixmap DPI.</param>
        public Pixmap GetPixmap(Matrix matrix = null, Colorspace cs = null, bool alpha = false, int dpi = 0)
        {
            if (dpi != 0)
                matrix = new Matrix((float)(dpi / 72.0), (float)(dpi / 72.0));
            var ctm = (matrix ?? Matrix.Identity).ToFzMatrix();
            var colorspace = (cs ?? Colorspace.Rgb).ToFzColorspace();
            var pix = mupdf.mupdf.pdf_new_pixmap_from_annot(NativeAnnot, ctm, colorspace, new mupdf.FzSeparations(), alpha ? 1 : 0);
            var result = new Pixmap(pix);
            if (dpi != 0)
                result.SetDpi(dpi, dpi);
            return result;
        }

        /// <summary>
        /// Make annotation TextPage (PyMuPDF <c>Annot.get_textpage</c>).
        /// </summary>
        public TextPage GetTextPage(int flags = 0, IRect clip = null)
        {
            var opts = new mupdf.FzStextOptions(flags);
            if (clip != null)
            {
                opts.clip = new Rect(clip).ToFzRect().internal_();
                opts.flags |= mupdf.mupdf.FZ_STEXT_CLIP_RECT;
            }
            var stp = new mupdf.FzStextPage(NativeAnnot, opts);
            return new TextPage(stp) { Parent = Parent };
        }

        /// <summary>Extract annotation text (PyMuPDF <c>Annot.get_text()</c>).</summary>
        public string GetText()
            => (string)Utils.GetText(this, "text");

        /// <summary>Extract annotation text with stext flags (PyMuPDF <c>Annot.get_textpage</c> + extract).</summary>
        public string GetText(int flags)
        {
            int f = flags == 0 ? Constants.TextFlagsText : flags;
            using var tp = GetTextPage(f);
            return tp.ExtractText();
        }

        /// <summary>Extract annotation text (PyMuPDF <c>Annot.get_text</c> / <c>utils.get_text</c>).</summary>
        public dynamic GetText(
            string option,
            IRect clip = null,
            int? flags = null,
            TextPage textpage = null,
            bool sort = false,
            char[] delimiters = null,
            float tolerance = 3)
            => Utils.GetText(this, option, clip, flags, textpage, sort, delimiters, tolerance);

        /// <summary>
        /// Legacy overload: <paramref name="page"/> must be this annotation's parent; extraction uses the annot AP, not the page.
        /// </summary>
        public dynamic GetText(
            Page page,
            string option = "text",
            Rect clip = null,
            int flags = 0,
            TextPage stPage = null,
            bool sort = false,
            char[] delimiters = null,
            float tolerance = 3)
        {
            if (page != null && page != Parent)
                throw new ArgumentException("page must be the annotation's parent page");
            return GetText(
                option,
                clip != null ? new IRect(clip) : null,
                flags == 0 ? (int?)null : flags,
                stPage,
                sort,
                delimiters,
                tolerance);
        }

        /// <summary>Extract annotation text within a rectangle (PyMuPDF <c>Annot.get_textbox</c>).</summary>
        public string GetTextbox(Rect rect, int flags = 0)
        {
            using var tp = GetTextPage(flags);
            return tp.ExtractTextbox(rect);
        }

        /// <summary>Sound stream bytes from <see cref="GetSoundDictionary"/>.</summary>
        public byte[] GetSoundData()
        {
            var sound = GetSoundDictionary();
            return sound != null && sound.TryGetValue("stream", out var stream) ? (byte[])stream : null;
        }

        /// <summary>Retrieve sound stream (PyMuPDF <c>Annot.get_sound</c>).</summary>
        public Dictionary<string, object> GetSoundDictionary()
        {
            if (AnnotationType != AnnotationType.Sound)
                throw new ArgumentException(Constants.MSG_BAD_ANNOT_TYPE);
            var sound = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("Sound"));
            if (sound.m_internal == null)
                throw new ArgumentException(Constants.MSG_BAD_ANNOT_TYPE);
            if (mupdf.mupdf.pdf_dict_get(sound, mupdf.mupdf.pdf_new_name("F")).m_internal != null)
                throw new InvalidOperationException("unsupported sound stream");

            var result = new Dictionary<string, object>();
            var rate = mupdf.mupdf.pdf_dict_get(sound, mupdf.mupdf.pdf_new_name("R"));
            if (rate.m_internal != null) result["rate"] = mupdf.mupdf.pdf_to_real(rate);
            var channels = mupdf.mupdf.pdf_dict_get(sound, mupdf.mupdf.pdf_new_name("C"));
            if (channels.m_internal != null) result["channels"] = mupdf.mupdf.pdf_to_int(channels);
            var bps = mupdf.mupdf.pdf_dict_get(sound, mupdf.mupdf.pdf_new_name("B"));
            if (bps.m_internal != null) result["bps"] = mupdf.mupdf.pdf_to_int(bps);
            var encoding = mupdf.mupdf.pdf_dict_get(sound, mupdf.mupdf.pdf_new_name("E"));
            if (encoding.m_internal != null) result["encoding"] = mupdf.mupdf.pdf_to_name(encoding);
            var compression = mupdf.mupdf.pdf_dict_gets(sound, "CO");
            if (compression.m_internal != null) result["compression"] = mupdf.mupdf.pdf_to_name(compression);
            result["stream"] = Helpers.BufferToBytes(mupdf.mupdf.pdf_load_stream(sound));
            return result;
        }

        /// <summary>Set or remove annotation OC xref (PyMuPDF <c>Annot.set_oc</c>).</summary>
        public void SetOC(int oc)
        {
            var obj = mupdf.mupdf.pdf_annot_obj(NativeAnnot);
            if (oc <= 0)
                mupdf.mupdf.pdf_dict_dels(obj, "OC");
            else
            {
                var pdf = Parent.Parent.NativePdfDocument;
                Helpers.JM_add_oc_object(pdf, obj, oc);
            }
        }

        /// <summary>Delete Popup and responding annotations (PyMuPDF <c>Annot.delete_responses</c>).</summary>
        public void DeleteResponses()
        {
            var page = mupdf.mupdf.pdf_annot_page(NativeAnnot);
            while (true)
            {
                var irtAnnot = Helpers.JM_find_annot_irt(NativeAnnot);
                if (irtAnnot.m_internal == null) break;
                mupdf.mupdf.pdf_delete_annot(page, irtAnnot);
            }
            mupdf.mupdf.pdf_dict_del(AnnotObj, mupdf.mupdf.pdf_new_name("Popup"));

            // Python: also scan /Annots and remove entries whose /Parent is this annot.
            var annots = mupdf.mupdf.pdf_dict_get(page.obj(), mupdf.mupdf.pdf_new_name("Annots"));
            int n = mupdf.mupdf.pdf_array_len(annots);
            bool found = false;
            for (int i = n - 1; i >= 0; i--)
            {
                var o = mupdf.mupdf.pdf_array_get(annots, i);
                if (o.m_internal == null) continue;
                var p = mupdf.mupdf.pdf_dict_get(o, mupdf.mupdf.pdf_new_name("Parent"));
                if (mupdf.mupdf.pdf_objcmp(p, AnnotObj) == 0)
                {
                    mupdf.mupdf.pdf_array_delete(annots, i);
                    found = true;
                }
            }
            if (found)
                mupdf.mupdf.pdf_dict_put(page.obj(), mupdf.mupdf.pdf_new_name("Annots"), annots);
        }

        /// <summary>Get annotation optional content reference (PyMuPDF <c>Annot.get_oc</c>).</summary>
        public int GetOC()
        {
            var obj = mupdf.mupdf.pdf_annot_obj(NativeAnnot);
            var oc = mupdf.mupdf.pdf_dict_gets(obj, "OC");
            return oc.m_internal != null ? mupdf.mupdf.pdf_to_num(oc) : 0;
        }

        /// <summary>Normal (/AP/N) appearance stream text (.NET helper; Python uses <c>_getAP</c> bytes).</summary>
        public string GetAPNormal() => GetAP("N");
        /// <summary>Rollover (/AP/R) appearance stream text.</summary>
        public string GetAPRollover() => GetAP("R");
        /// <summary>Down (/AP/D) appearance stream text.</summary>
        public string GetAPDown() => GetAP("D");

        /// <summary>Decode annotation appearance stream as UTF-8 text (.NET helper).</summary>
        public string GetAP(string which = "N")
        {
            var ap = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("AP"));
            if (ap.m_internal == null) return "";

            mupdf.PdfObj apStream;
            switch (which.ToUpper())
            {
                case "N": apStream = mupdf.mupdf.pdf_dict_get(ap, mupdf.mupdf.pdf_new_name("N")); break;
                case "R": apStream = mupdf.mupdf.pdf_dict_get(ap, mupdf.mupdf.pdf_new_name("R")); break;
                case "D": apStream = mupdf.mupdf.pdf_dict_get(ap, mupdf.mupdf.pdf_new_name("D")); break;
                default: apStream = mupdf.mupdf.pdf_dict_get(ap, mupdf.mupdf.pdf_new_name("N")); break;
            }

            if (apStream.m_internal == null) return "";
            if (mupdf.mupdf.pdf_is_stream(apStream) == 0) return "";

            try
            {
                var buf = mupdf.mupdf.pdf_load_stream(apStream);
                return Helpers.BufferToUtf8(buf);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>Set annotation appearance stream (.NET helper; Python uses <c>_setAP</c> on /AP/N).</summary>
        public void SetAP(string which, byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return;
            var pdf = ParentPdfDocument;
            var ap = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("AP"));
            if (ap.m_internal == null)
            {
                ap = mupdf.mupdf.pdf_new_dict(pdf, 3);
                mupdf.mupdf.pdf_dict_put(AnnotObj, mupdf.mupdf.pdf_new_name("AP"), ap);
            }

            var fzBuf = Helpers.BufferFromBytes(buffer);
            var stream = mupdf.mupdf.pdf_add_stream(pdf, fzBuf, new mupdf.PdfObj(), 0);

            var r = mupdf.mupdf.pdf_annot_rect(NativeAnnot);
            var bbox = mupdf.mupdf.pdf_new_array(pdf, 4);
            mupdf.mupdf.pdf_array_push_real(bbox, r.x0);
            mupdf.mupdf.pdf_array_push_real(bbox, r.y0);
            mupdf.mupdf.pdf_array_push_real(bbox, r.x1);
            mupdf.mupdf.pdf_array_push_real(bbox, r.y1);
            mupdf.mupdf.pdf_dict_put(stream, mupdf.mupdf.pdf_new_name("BBox"), bbox);
            mupdf.mupdf.pdf_dict_put(stream, mupdf.mupdf.pdf_new_name("Type"), mupdf.mupdf.pdf_new_name("XObject"));
            mupdf.mupdf.pdf_dict_put(stream, mupdf.mupdf.pdf_new_name("Subtype"), mupdf.mupdf.pdf_new_name("Form"));

            switch (which.ToUpper())
            {
                case "N": mupdf.mupdf.pdf_dict_put(ap, mupdf.mupdf.pdf_new_name("N"), stream); break;
                case "R": mupdf.mupdf.pdf_dict_puts(ap, "R", stream); break;
                case "D": mupdf.mupdf.pdf_dict_puts(ap, "D", stream); break;
                default: mupdf.mupdf.pdf_dict_put(ap, mupdf.mupdf.pdf_new_name("N"), stream); break;
            }
        }

        /// <summary>Attached file information (PyMuPDF <c>Annot.file_info</c>).</summary>
        public Dictionary<string, object> GetFileInfo()
        {
            if (AnnotationType != AnnotationType.FileAttachment)
                throw new ArgumentException(Constants.MSG_BAD_ANNOT_TYPE);
            var fs = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("FS"));
            if (fs.m_internal == null) return null;

            var result = new Dictionary<string, object>();
            var ufnameObj = mupdf.mupdf.pdf_dict_get(fs, mupdf.mupdf.pdf_new_name("UF"));
            var fnameObj = mupdf.mupdf.pdf_dict_get(fs, mupdf.mupdf.pdf_new_name("F"));
            string filename = ufnameObj.m_internal != null
                ? mupdf.mupdf.pdf_to_text_string(ufnameObj)
                : fnameObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(fnameObj) : "";
            result["filename"] = filename;

            var descObj = mupdf.mupdf.pdf_dict_get(fs, mupdf.mupdf.pdf_new_name("Desc"));
            result["desc"] = descObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(descObj) : null;
            result["length"] = -1;
            result["size"] = -1;

            var ef = mupdf.mupdf.pdf_dict_get(fs, mupdf.mupdf.pdf_new_name("EF"));
            if (ef.m_internal != null)
            {
                var fStream = mupdf.mupdf.pdf_dict_get(ef, mupdf.mupdf.pdf_new_name("F"));
                if (fStream.m_internal != null)
                {
                    // Python: expose both stream /Length and /Params /Size.
                    var lengthObj = mupdf.mupdf.pdf_dict_get(fStream, mupdf.mupdf.pdf_new_name("Length"));
                    result["length"] = lengthObj.m_internal != null ? mupdf.mupdf.pdf_to_int(lengthObj) : -1;
                    var sizeObj = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_dict_get(fStream, mupdf.mupdf.pdf_new_name("Params")), mupdf.mupdf.pdf_new_name("Size"));
                    result["size"] = sizeObj.m_internal != null ? mupdf.mupdf.pdf_to_int(sizeObj) : -1;
                }
            }
            return result;
        }

        /// <summary>Retrieve attached file content (PyMuPDF <c>Annot.get_file</c>).</summary>
        public byte[] GetFile()
        {
            if (AnnotationType != AnnotationType.FileAttachment)
                throw new ArgumentException(Constants.MSG_BAD_ANNOT_TYPE);
            var stream = GetNestedDict(AnnotObj, "FS", "EF", "F");
            if (stream.m_internal == null)
                throw new InvalidOperationException("bad PDF: file entry not found");
            return Helpers.BufferToBytes(mupdf.mupdf.pdf_load_stream(stream));
        }

        /// <summary>Update attached file (PyMuPDF <c>Annot.update_file</c>).</summary>
        public void UpdateFile(byte[] buffer = null, string filename = null, string ufilename = null, string desc = null)
        {
            if (AnnotationType != AnnotationType.FileAttachment)
                throw new ArgumentException(Constants.MSG_BAD_ANNOT_TYPE);
            var stream = GetNestedDict(AnnotObj, "FS", "EF", "F");
            if (stream.m_internal == null)
                throw new InvalidOperationException("bad PDF: no /EF object");

            var fs = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("FS"));
            var res = Helpers.BufferFromBytes(buffer);
            if (buffer != null && res.m_internal == null)
                throw new ArgumentException(Constants.MSG_BAD_BUFFER);
            if (res.m_internal != null)
            {
                mupdf.mupdf.pdf_update_stream(ParentPdfDocument, stream, res, 1);
                // Python: adjust /DL and /Params /Size after stream replacement.
                int len = buffer.Length;
                var l = mupdf.mupdf.pdf_new_int(len);
                mupdf.mupdf.pdf_dict_put(stream, mupdf.mupdf.pdf_new_name("DL"), l);
                var parms = mupdf.mupdf.pdf_dict_get(stream, mupdf.mupdf.pdf_new_name("Params"));
                if (parms.m_internal == null)
                {
                    parms = mupdf.mupdf.pdf_new_dict(ParentPdfDocument, 1);
                    mupdf.mupdf.pdf_dict_put(stream, mupdf.mupdf.pdf_new_name("Params"), parms);
                }
                mupdf.mupdf.pdf_dict_put(parms, mupdf.mupdf.pdf_new_name("Size"), l);
            }

            if (!string.IsNullOrEmpty(filename))
            {
                mupdf.mupdf.pdf_dict_put_text_string(stream, mupdf.mupdf.pdf_new_name("F"), filename);
                mupdf.mupdf.pdf_dict_put_text_string(fs, mupdf.mupdf.pdf_new_name("F"), filename);
                mupdf.mupdf.pdf_dict_put_text_string(stream, mupdf.mupdf.pdf_new_name("UF"), filename);
                mupdf.mupdf.pdf_dict_put_text_string(fs, mupdf.mupdf.pdf_new_name("UF"), filename);
                mupdf.mupdf.pdf_dict_put_text_string(AnnotObj, mupdf.mupdf.pdf_new_name("Contents"), filename);
            }
            if (!string.IsNullOrEmpty(ufilename))
            {
                mupdf.mupdf.pdf_dict_put_text_string(stream, mupdf.mupdf.pdf_new_name("UF"), ufilename);
                mupdf.mupdf.pdf_dict_put_text_string(fs, mupdf.mupdf.pdf_new_name("UF"), ufilename);
            }
            if (!string.IsNullOrEmpty(desc))
            {
                mupdf.mupdf.pdf_dict_put_text_string(stream, mupdf.mupdf.pdf_new_name("Desc"), desc);
                mupdf.mupdf.pdf_dict_put_text_string(fs, mupdf.mupdf.pdf_new_name("Desc"), desc);
            }
        }

        /// <summary>Clean appearance contents stream (PyMuPDF <c>Annot.clean_contents</c>).</summary>
        public void CleanContents(int sanitize = 1)
        {
            // filter_ = _make_PdfFilterOptions(recurse=1, instance_forms=0, ascii=0, sanitize=sanitize)
            Helpers.PdfFilterOptionsRef filterPkg = Helpers.MakePdfFilterOptions(
                recurse: 1, instance_forms: 0, ascii: 0, sanitize: sanitize);
            mupdf.mupdf.pdf_filter_annot_contents(ParentPdfDocument, NativeAnnot, filterPkg.Filter);
        }

        /// <summary>Create annotation Popup or update its rectangle (PyMuPDF <c>Annot.set_popup</c>).</summary>
        public void SetPopup(Rect rect)
        {
            var transformed = Helpers.TransformRect(new Rect(rect), RotatePageMatrix);
            mupdf.mupdf.pdf_set_annot_popup(NativeAnnot, transformed.ToFzRect());
        }

        /// <summary>Set annotation appearance bbox (PyMuPDF <c>Annot.set_apn_bbox</c>).</summary>
        public void SetApnBBox(Rect bbox)
        {
            var transformed = Helpers.TransformRect(new Rect(bbox), RotatePageMatrix);
            var ap = GetAppearanceStreamObject("N");
            if (ap.m_internal == null)
                throw new InvalidOperationException(Constants.MSG_BAD_APN);
            mupdf.mupdf.pdf_dict_put_rect(ap, mupdf.mupdf.pdf_new_name("BBox"), transformed.ToFzRect());
        }

        private static PdfObj PdfDictGetl(PdfObj obj, string[] keys)
        {
            foreach (string key in keys)
            {
                if (obj.m_internal == null)
                    break;
                obj = obj.pdf_dict_get(new PdfObj(key));
            }
            return obj;
        }

        /// <summary>Set annotation appearance matrix (PyMuPDF <c>Annot.set_apn_matrix</c>).</summary>
        public void SetApnMatrix(Matrix matrix)
        {
            /*
            var annot = NativeAnnot;
            var annot_obj = AnnotObj;
            var ap = Helpers.PdfDictGetl(annot_obj, mupdf.mupdf.pdf_new_name("AP"), mupdf.mupdf.pdf_new_name("N"));
            if (ap.m_internal == null)
                throw new InvalidOperationException(Constants.MSG_BAD_APN);
            var mat = matrix.ToFzMatrix();
            mupdf.mupdf.pdf_dict_put_matrix(ap, mupdf.mupdf.pdf_new_name("Matrix"), mat);
            */
            var annot = NativeAnnot;
            var annotObj = AnnotObj;// annot.pdf_annot_obj();
            //var ap = Helpers.PdfDictGetl(annotObj, mupdf.mupdf.pdf_new_name("AP"), mupdf.mupdf.pdf_new_name("N"));
            PdfObj ap = PdfDictGetl(annotObj, new string[] { "AP", "N" });

            if (ap.m_internal == null)
                throw new InvalidOperationException(Constants.MSG_BAD_APN);

            ap.pdf_dict_put_matrix(new PdfObj("Matrix"), matrix.ToFzMatrix());
        }

        /// <summary>Set annotation language (PyMuPDF <c>Annot.set_language</c>).</summary>
        public void SetLanguage(string language = null)
        {
            var lang = string.IsNullOrEmpty(language)
                ? mupdf.fz_text_language.FZ_LANG_UNSET
                : mupdf.mupdf.fz_text_language_from_string(language);
            mupdf.mupdf.pdf_set_annot_language(NativeAnnot, lang);
        }

        /// <summary>Set line end codes (PyMuPDF <c>Annot.set_line_ends</c>).</summary>
        public void SetLineEnds(int start, int end)
        {
            if (mupdf.mupdf.pdf_annot_has_line_ending_styles(NativeAnnot) != 0)
                mupdf.mupdf.pdf_set_annot_line_ending_styles(NativeAnnot, (mupdf.pdf_line_ending)start, (mupdf.pdf_line_ending)end);
            else
                Trace.TraceWarning("bad annot type for line ends");
        }

        /// <summary>Legacy overload using <see cref="PdfLineEnding"/>.</summary>
        public void SetLineEnds(PdfLineEnding start, PdfLineEnding end) => SetLineEnds((int)start, (int)end);

        /// <summary>Set annotation rotation (PyMuPDF <c>Annot.set_rotation</c>).</summary>
        public void SetRotation(int rotate = 0)
        {
            switch (AnnotationType)
            {
                case AnnotationType.Caret:
                case AnnotationType.Circle:
                case AnnotationType.FreeText:
                case AnnotationType.FileAttachment:
                case AnnotationType.Ink:
                case AnnotationType.Line:
                case AnnotationType.PolyLine:
                case AnnotationType.Polygon:
                case AnnotationType.Square:
                case AnnotationType.Stamp:
                case AnnotationType.Text:
                    break;
                default:
                    return;
            }

            while (rotate < 0) rotate += 360;
            while (rotate >= 360) rotate -= 360;
            if (AnnotationType == AnnotationType.FreeText && rotate % 90 != 0)
                rotate = 0;
            mupdf.mupdf.pdf_dict_put_int(AnnotObj, mupdf.mupdf.pdf_new_name("Rotate"), rotate);
        }

        /// <summary>Set open status of annotation or its Popup (PyMuPDF <c>Annot.set_open</c>).</summary>
        public void SetOpen(bool isOpen)
        {
            mupdf.mupdf.pdf_set_annot_is_open(NativeAnnot, isOpen ? 1 : 0);
        }

        // ─── IDisposable ────────────────────────────────────────────────

        /// <summary>
        /// Releases resources used by this annotation wrapper.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Parent.ForgetAnnotRef(this);
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~Annot() { Dispose(); }

        /// <summary>String form (PyMuPDF <c>Annot.__repr__</c> / <c>__str__</c>).</summary>
        public override string ToString() => $"Annot('{TypeString}' on {Parent})";

        public static string EscapeStrFromStr(string c) => Helpers.JM_EscapeStrFromStr(c);
        public static string UnicodeFromStr(string s) => s ?? string.Empty;
        public static byte[] MergeByte(byte[] a, byte[] b) => a.Concat(b).ToArray();
        public static float[] ColorFromSequence(float[] seq) => NormalizeColorSequence(seq);
        public static string ColorCode(float[] cs, string code) => Helpers.ColorCode(cs, code);
        public static byte[] ColorString(float[] cs, string code) => Encoding.UTF8.GetBytes((Helpers.ColorCode(cs, code) ?? "") + "\n");
        public static mupdf.PdfObj GetBorderStyle(string s)
        {
            if (string.IsNullOrEmpty(s))
                return mupdf.mupdf.pdf_new_name("S");
            char c = char.ToUpperInvariant(s[0]);
            return c switch
            {
                'B' => mupdf.mupdf.pdf_new_name("B"),
                'D' => mupdf.mupdf.pdf_new_name("D"),
                'I' => mupdf.mupdf.pdf_new_name("I"),
                'U' => mupdf.mupdf.pdf_new_name("U"),
                _ => mupdf.mupdf.pdf_new_name("S"),
            };
        }
        public static Dictionary<string, object> GetBorderFromAnnot(mupdf.PdfObj annotObj) => Helpers.JM_annot_border(annotObj);
        public static Dictionary<string, object> GetColorFromAnnot(mupdf.PdfObj annotObj) => Helpers.JM_annot_colors(annotObj);
        public static void SetBorderAnnot(Dictionary<string, object> border, mupdf.PdfDocument doc, mupdf.PdfObj annotObj) => Helpers.JM_annot_set_border(border, doc, annotObj);
        public static mupdf.PdfAnnot FindAnnotIRT(mupdf.PdfAnnot annot) => Helpers.JM_find_annot_irt(annot);
        public static void AddOCObject(mupdf.PdfDocument doc, mupdf.PdfObj reference, int xref) => Helpers.JM_add_oc_object(doc, reference, xref);

        /// <summary>PyMuPDF <c>Annot._getAP</c> — AP/N stream bytes when present.</summary>
        private byte[] _getAP()
        {
            var ap = GetAppearanceStreamObject("N");
            if (mupdf.mupdf.pdf_is_stream(ap) == 0)
                return null;
            return Helpers.BufferToBytes(mupdf.mupdf.pdf_load_stream(ap));
        }

        /// <summary>PyMuPDF <c>Annot._setAP</c> — update AP/N stream; optional BBox sync when <paramref name="rect"/> is 1.</summary>
        private void _setAP(byte[] buffer, int rect = 0)
        {
            var apobj = GetAppearanceStreamObject("N");
            if (apobj.m_internal == null || mupdf.mupdf.pdf_is_stream(apobj) == 0)
                throw new InvalidOperationException(Constants.MSG_BAD_APN);
            var res = Helpers.BufferFromBytes(buffer);
            if (res.m_internal == null)
                throw new ArgumentException(Constants.MSG_BAD_BUFFER);
            mupdf.mupdf.pdf_update_stream(ParentPdfDocument, apobj, res, 1);
            if (rect != 0)
            {
                // Python _setAP(rect=1): sync AP /BBox to annot /Rect.
                var bbox = mupdf.mupdf.pdf_dict_get_rect(AnnotObj, mupdf.mupdf.pdf_new_name("Rect"));
                mupdf.mupdf.pdf_dict_put_rect(apobj, mupdf.mupdf.pdf_new_name("BBox"), bbox);
            }
        }

        private mupdf.PdfObj GetAppearanceStreamObject(string which)
        {
            var ap = mupdf.mupdf.pdf_dict_gets(AnnotObj, "AP");
            if (ap.m_internal == null) return ap;
            return mupdf.mupdf.pdf_dict_gets(ap, which);
        }

        private static mupdf.PdfObj GetNestedDict(mupdf.PdfObj root, params string[] keys)
        {
            var current = root;
            foreach (var key in keys)
            {
                if (current.m_internal == null) return current;
                current = mupdf.mupdf.pdf_dict_gets(current, key);
            }
            return current;
        }

        private static float[] NormalizeColorSequence(object value)
        {
            if (value == null) return null;
            if (value is float[] fa) return fa;
            if (value is float[] da) return Array.ConvertAll(da, x => (float)x);
            if (value is int[] ia) return Array.ConvertAll(ia, x => (float)x);
            if (value is float f) return new[] { f };
            if (value is float d) return new[] { (float)d };
            if (value is int i) return new[] { (float)i };
            if (value is System.Collections.IEnumerable seq && value is not string)
            {
                var list = new List<float>();
                foreach (var item in seq)
                {
                    if (item == null) return null;
                    list.Add(Convert.ToSingle(item));
                }
                return list.ToArray();
            }
            return null;
        }

    }
}
