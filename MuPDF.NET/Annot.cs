using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Threading;
using mupdf;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a PDF annotation.
    /// </summary>
    public class Annot : IDisposable
    {
        private static int _nextAnnotRefId;
        private mupdf.PdfAnnot _nativeAnnot;
        private bool _disposed;
        internal Page Parent { get; }

        /// <summary>
        /// Stable identity for <see cref="Page"/> annot-ref bookkeeping (Python uses <c>id(annot)</c> keys in <c>Page._annot_refs</c>).
        /// </summary>
        internal int AnnotRefId { get; }

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

        private mupdf.PdfObj AnnotObj => mupdf.mupdf.pdf_annot_obj(NativeAnnot);
        private mupdf.PdfDocument ParentPdfDocument => Parent.Parent.NativePdfDocument;
        private Matrix RotatePageMatrix => Helpers.RotatePageMatrix(Parent);
        private Matrix DerotatePageMatrix => Helpers.DerotatePageMatrix(Parent);
        private Matrix VertexMatrix => Helpers.AnnotVertexMatrix(Parent);
        private bool IsWidgetAnnot => Type == AnnotationType.Widget;

        // ─── Properties ─────────────────────────────────────────────────

        /// <summary>
        /// Annotation type as enum.
        /// </summary>
        public AnnotationType Type
        {
            get
            {
                var t = mupdf.mupdf.pdf_annot_type(NativeAnnot);
                return (AnnotationType)(int)t;
            }
        }

        /// <summary>
        /// Annotation type as string.
        /// </summary>
        public string TypeString => mupdf.mupdf.pdf_string_from_annot_type(
            (mupdf.pdf_annot_type)(int)Type);

        /// <summary>
        /// Annotation rectangle.
        /// </summary>
        public Rect Rect
        {
            get
            {
                var r = mupdf.mupdf.pdf_bound_annot(NativeAnnot);
                // Python: "val *= p.derotation_matrix"
                return Helpers.TransformRect(new Rect(r), DerotatePageMatrix);
            }
        }

        /// <summary>
        /// Annotation xref number.
        /// </summary>
        public int Xref => mupdf.mupdf.pdf_to_num(mupdf.mupdf.pdf_annot_obj(NativeAnnot));

        /// <summary>
        /// Flags field.
        /// </summary>
        public int Flags
        {
            get => mupdf.mupdf.pdf_annot_flags(NativeAnnot);
            set => mupdf.mupdf.pdf_set_annot_flags(NativeAnnot, value);
        }

        /// <summary>
        /// Annotation contents.
        /// </summary>
        public string Contents
        {
            get => mupdf.mupdf.pdf_annot_contents(NativeAnnot);
            set
            {
                mupdf.mupdf.pdf_set_annot_contents(NativeAnnot, value);
                mupdf.mupdf.pdf_update_annot(NativeAnnot);
            }
        }

        /// <summary>
        /// Check if annotation has a Popup.
        /// </summary>
        public bool HasPopup
        {
            get
            {
                var popup = mupdf.mupdf.pdf_dict_gets(AnnotObj, "Popup");
                return popup.m_internal != null;
            }
        }

        /// <summary>
        /// Annotation 'Popup' rectangle.
        /// </summary>
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

        /// <summary>
        /// Annotation 'Popup' xref.
        /// </summary>
        public int PopupXref
        {
            get
            {
                if (!HasPopup) return 0;
                var popup = mupdf.mupdf.pdf_dict_gets(AnnotObj, "Popup");
                return mupdf.mupdf.pdf_to_num(popup);
            }
        }

        /// <summary>
        /// Get 'open' status of annotation or its Popup.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return mupdf.mupdf.pdf_annot_is_open(NativeAnnot) != 0;
            }
            set => mupdf.mupdf.pdf_set_annot_is_open(NativeAnnot, value ? 1 : 0);
        }

        /// <summary>
        /// Opacity value.
        /// </summary>
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

        /// <summary>
        /// Border width.
        /// </summary>
        public float Border_Width
        {
            get => Border.TryGetValue("width", out var width) && width != null ? Convert.ToSingle(width) : -1;
        }

        /// <summary>
        /// Annotation unique id (NM key).
        /// </summary>
        public string Id
        {
            get
            {
                var nm = mupdf.mupdf.pdf_dict_gets(mupdf.mupdf.pdf_annot_obj(NativeAnnot), "NM");
                return nm.m_internal != null ? mupdf.mupdf.pdf_to_text_string(nm) : null;
            }
        }

        /// <summary>
        /// Next annotation.
        /// </summary>
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


        /// <summary>
        /// Stroke color components.
        /// </summary>
        public float[] Colors
        {
            get => ColorInfo.TryGetValue("stroke", out var stroke) ? (float[])stroke : Array.Empty<float>();
        }

        /// <summary>
        /// Interior (fill) color components.
        /// </summary>
        public float[] InteriorColor
        {
            get => ColorInfo.TryGetValue("fill", out var fill) ? (float[])fill : Array.Empty<float>();
        }

        public Dictionary<string, object> ColorInfo
        {
            get => Helpers.JM_annot_colors(AnnotObj);
        }

        public Dictionary<string, object> Border
        {
            get
            {
                var annotType = Type;
                if (annotType != AnnotationType.Circle
                    && annotType != AnnotationType.FreeText
                    && annotType != AnnotationType.Ink
                    && annotType != AnnotationType.Line
                    && annotType != AnnotationType.PolyLine
                    && annotType != AnnotationType.Polygon
                    && annotType != AnnotationType.Square)
                    return new Dictionary<string, object>();
                return Helpers.JM_annot_border(AnnotObj);
            }
        }

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

        public Matrix ApnMatrix
        {
            get
            {
                var ap = GetAppearanceStreamObject("N");
                if (ap.m_internal == null) return new Matrix();
                return Helpers.MatrixFromFz(mupdf.mupdf.pdf_dict_get_matrix(ap, mupdf.mupdf.pdf_new_name("Matrix")));
            }
        }

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
        /// Set annotation rectangle.
        /// </summary>
        /// <returns>
        /// Python returns <c>None</c> on success and <c>False</c> on failure; this returns <see langword="null"/> / <see langword="false"/>.
        /// </returns>
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
        /// Set border properties.
        /// </summary>
        public void SetBorder(float width = -1, float[] dashes = null, string style = null)
        {
            SetBorder(null, width, style, dashes != null ? Array.ConvertAll(dashes, x => (int)x) : null, -1);
        }

        public void SetBorder(Dictionary<string, object> border = null, float width = -1, string style = null, int[] dashes = null, float clouds = -1)
        {
            var annotType = Type;
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
        /// Set 'stroke' and 'fill' colors.
        /// </summary>
        public void SetColors(float[] stroke = null, float[] fill = null)
        {
            SetColors((Dictionary<string, float[]>)null, stroke, fill);
        }

        public void SetColors(Dictionary<string, object> colors = null, object stroke = null, object fill = null)
        {
            colors ??= new Dictionary<string, object>
            {
                ["fill"] = fill,
                ["stroke"] = stroke,
            };
            var strokeObj = colors.TryGetValue("stroke", out var s) ? s : stroke;
            var fillObj = colors.TryGetValue("fill", out var f) ? f : fill;
            SetColors(null, NormalizeColorSequence(strokeObj), NormalizeColorSequence(fillObj));
        }

        public void SetColors(Dictionary<string, float[]> colors = null, float[] stroke = null, float[] fill = null)
        {
            if (Type == AnnotationType.FreeText)
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

            bool allowFill = Type == AnnotationType.Circle
                || Type == AnnotationType.Square
                || Type == AnnotationType.Line
                || Type == AnnotationType.PolyLine
                || Type == AnnotationType.Polygon
                || Type == AnnotationType.Redact;

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

        /// <summary>
        /// Set opacity.
        /// </summary>
        public void SetOpacity(float opacity)
        {
            if (!Helpers.InRange(opacity, 0.0, 1.0))
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

        /// <summary>
        /// Set /Name (icon) of annotation.
        /// </summary>
        public void SetName(string name)
        {
            mupdf.mupdf.pdf_dict_put_name(AnnotObj, mupdf.mupdf.pdf_new_name("Name"), name);
        }

        /// <summary>
        /// Get /Name (icon) of annotation.
        /// </summary>
        public string GetName()
        {
            var obj = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("Name"));
            return obj.m_internal != null ? mupdf.mupdf.pdf_to_name(obj) : "";
        }

        /// <summary>
        /// Set annotation BlendMode.
        /// </summary>
        public void SetBlendMode(string mode)
        {
            mupdf.mupdf.pdf_dict_put_name(AnnotObj, mupdf.mupdf.pdf_new_name("BM"), mode);
        }

        /// <summary>
        /// Annotation BlendMode.
        /// </summary>
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

        /// <summary>
        /// Annotation author.
        /// </summary>
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

        /// <summary>
        /// Annotation creation date.
        /// </summary>
        public string CreationDate
        {
            get
            {
                var cd = mupdf.mupdf.pdf_dict_gets(mupdf.mupdf.pdf_annot_obj(NativeAnnot), "CreationDate");
                return cd.m_internal != null ? mupdf.mupdf.pdf_to_text_string(cd) : "";
            }
        }

        /// <summary>
        /// Annotation modification date.
        /// </summary>
        public string ModDate
        {
            get
            {
                var md = mupdf.mupdf.pdf_dict_gets(mupdf.mupdf.pdf_annot_obj(NativeAnnot), "M");
                return md.m_internal != null ? mupdf.mupdf.pdf_to_text_string(md) : "";
            }
        }

        /// <summary>
        /// Set various annotation properties.
        /// </summary>
        public void SetInfo(string content = null, string title = null, string creationDate = null, string modDate = null, string subject = null)
        {
            SetInfo(null, content, title, creationDate, modDate, subject);
        }

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

        /// <summary>
        /// Various information details.
        /// </summary>
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

        /// <summary>
        /// Annotation vertex points.
        /// </summary>
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
                        double x = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(source, i));
                        double y = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(source, i + 1));
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
                            double x = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(sub, j));
                            double y = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(sub, j + 1));
                            result.Add(Helpers.TransformPoint(new Point(x, y), VertexMatrix));
                        }
                    }
                }
                return result;
            }
        }


        /// <summary>
        /// Line annotation endpoints.
        /// </summary>
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

        /// <summary>
        /// Set line endpoints.
        /// </summary>
        public void SetLine(Point p1, Point p2)
        {
            var rp1 = Helpers.TransformPoint(new Point(p1), RotatePageMatrix);
            var rp2 = Helpers.TransformPoint(new Point(p2), RotatePageMatrix);
            mupdf.mupdf.pdf_set_annot_line(NativeAnnot, rp1.ToFzPoint(), rp2.ToFzPoint());
        }

        /// <summary>
        /// Set annotation vertices.
        /// </summary>
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
        /// Update annot appearance.
        ///
        /// Notes:
        /// Depending on the annot type, some parameters make no sense,
        /// while others are only available in this method to achieve the
        /// desired result. This is especially true for 'FreeText' annots.
        ///
        /// Args:
        /// blend_mode: set the blend mode, all annotations.
        /// opacity: set the opacity, all annotations.
        /// fontsize: set fontsize, 'FreeText' only.
        /// fontname: set the font, 'FreeText' only.
        /// border_color: set border color, 'FreeText' only.
        /// text_color: set text color, 'FreeText' only.
        /// fill_color: set fill color, all annotations.
        /// cross_out: draw diagonal lines, 'Redact' only.
        /// rotate: set rotation, 'FreeText' and some others.
        /// </summary>
        public void Update(
            string? blendMode = null,
            float? opacity = null,
            float fontsize = 0,
            string? fontname = null,
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

            var annotType = Type;
            var border = Border;
            var dashes = border.TryGetValue("dashes", out var dashValue) ? dashValue as int[] : null;
            float borderWidth = border.TryGetValue("width", out var widthValue) && widthValue != null ? Convert.ToSingle(widthValue) : -1;
            float[] stroke = Colors;
            float[] fill = fillColor ?? InteriorColor;
            Matrix apnMatrix = ApnMatrix;
            Rect rect = null;

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
                if (!string.IsNullOrEmpty(fontname)) fname = fontname;
                if (fontsize > 0) fsize = fontsize;
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
            if (dashes != null && dashes.Length > 0)
            {
                // Python: handle dashes and reset where appropriate.
                apText = "[" + string.Join(" ", dashes) + "] 0 d\n" + apText;
                apText = apText.Replace("\nS\n", "\nS\n[] 0 d\n");
                apUpdated = true;
            }
            if (!string.IsNullOrEmpty(opaCode))
            {
                apText = opaCode + apText;
                apUpdated = true;
            }

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

            if (apUpdated)
            {
                byte[] updated = Encoding.UTF8.GetBytes("q\n" + apText.TrimEnd() + "\nQ\n");
                _setAP(updated, rect != null ? 1 : 0);
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

            var center = (Rect.TL + Rect.BR) / 2.0; // center of annot rect
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

        private bool _update_appearance(float opacity, string blendMode, float[] fillColor, int rotate, AnnotationType annotType)
        {
            var annotObj = AnnotObj;
            int nFill = fillColor?.Length ?? 0;

            bool supportsInterior = annotType == AnnotationType.Square
                || annotType == AnnotationType.Circle
                || annotType == AnnotationType.Line
                || annotType == AnnotationType.PolyLine
                || annotType == AnnotationType.Polygon;
            if (nFill == 0 || !supportsInterior)
                mupdf.mupdf.pdf_dict_del(annotObj, mupdf.mupdf.pdf_new_name("IC"));
            else if (annotType == AnnotationType.FreeText)
            {
                var col = mupdf.mupdf.pdf_new_array(ParentPdfDocument, nFill);
                for (int i = 0; i < nFill; i++)
                    mupdf.mupdf.pdf_array_push_real(col, fillColor[i]);
                mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("C"), col);
            }
            else
            {
                var col = mupdf.mupdf.pdf_new_array(ParentPdfDocument, nFill);
                for (int i = 0; i < nFill; i++)
                    mupdf.mupdf.pdf_array_push_real(col, fillColor[i]);
                mupdf.mupdf.pdf_dict_put(annotObj, mupdf.mupdf.pdf_new_name("IC"), col);
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

        private static void UpdateTimingTest()
        {
            int total = 0;
            for (int i = 0; i < 30 * 1000; i++) total += i;
        }

        /// <summary>
        /// Annotation Pixmap.
        /// </summary>
        public Pixmap GetPixmap(Matrix matrix = null, Colorspace cs = null, bool alpha = false)
        {
            var ctm = (matrix ?? Matrix.Identity).ToFzMatrix();
            var colorspace = (cs ?? Colorspace.CsRGB).ToFzColorspace();
            var pix = mupdf.mupdf.pdf_new_pixmap_from_annot(NativeAnnot, ctm, colorspace, new mupdf.FzSeparations(), alpha ? 1 : 0);
            return new Pixmap(pix);
        }

        /// <summary>
        /// Make annotation TextPage.
        /// </summary>
        public TextPage GetTextPage(int flags = 0)
        {
            var opts = new mupdf.fz_stext_options();
            opts.flags = flags;
            var stp = new mupdf.FzStextPage(NativeAnnot, new mupdf.FzStextOptions(opts));
            return new TextPage(stp);
        }

        /// <summary>
        /// Sound attachment data for sound annotations.
        /// </summary>
        public byte[] GetSoundData()
        {
            var sound = GetSound();
            return sound != null && sound.TryGetValue("stream", out var stream) ? (byte[])stream : null;
        }

        public Dictionary<string, object> GetSound()
        {
            if (Type != AnnotationType.Sound)
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

        /// <summary>
        /// Set / remove annotation optional content reference.
        /// </summary>
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

        /// <summary>
        /// Delete popup and response annotations.
        /// </summary>
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

        /// <summary>
        /// Get annotation optional content reference.
        /// </summary>
        public int GetOC()
        {
            var obj = mupdf.mupdf.pdf_annot_obj(NativeAnnot);
            var oc = mupdf.mupdf.pdf_dict_gets(obj, "OC");
            return oc.m_internal != null ? mupdf.mupdf.pdf_to_num(oc) : 0;
        }

        /// <summary>
        /// Normal appearance stream text.
        /// </summary>
        public string GetAPNormal() => GetAP("N");
        /// <summary>
        /// Rollover appearance stream text.
        /// </summary>
        public string GetAPRollover() => GetAP("R");
        /// <summary>
        /// Down appearance stream text.
        /// </summary>
        public string GetAPDown() => GetAP("D");

        /// <summary>
        /// Get annotation appearance stream.
        /// </summary>
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

        /// <summary>
        /// Set annotation appearance stream.
        /// </summary>
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

        /// <summary>
        /// File attachment information for file attachment annotations.
        /// </summary>
        public Dictionary<string, object> GetFileInfo()
        {
            if (Type != AnnotationType.FileAttachment)
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

        public byte[] GetFile()
        {
            if (Type != AnnotationType.FileAttachment)
                throw new ArgumentException(Constants.MSG_BAD_ANNOT_TYPE);
            var stream = GetNestedDict(AnnotObj, "FS", "EF", "F");
            if (stream.m_internal == null)
                throw new InvalidOperationException("bad PDF: file entry not found");
            return Helpers.BufferToBytes(mupdf.mupdf.pdf_load_stream(stream));
        }

        public void UpdateFile(byte[] buffer = null, string filename = null, string ufilename = null, string desc = null)
        {
            if (Type != AnnotationType.FileAttachment)
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

        public void CleanContents(int sanitize = 1)
        {
            var filter = new mupdf.PdfFilterOptions();
            filter.recurse = 1;
            filter.instance_forms = 0;
            filter.ascii = 0;
            mupdf.mupdf.pdf_filter_annot_contents(ParentPdfDocument, NativeAnnot, filter);
        }

        public void SetPopup(Rect rect)
        {
            var transformed = Helpers.TransformRect(new Rect(rect), RotatePageMatrix);
            mupdf.mupdf.pdf_set_annot_popup(NativeAnnot, transformed.ToFzRect());
        }

        public void SetApnBBox(Rect bbox)
        {
            var transformed = Helpers.TransformRect(new Rect(bbox), RotatePageMatrix);
            var ap = GetAppearanceStreamObject("N");
            if (ap.m_internal == null)
                throw new InvalidOperationException(Constants.MSG_BAD_APN);
            mupdf.mupdf.pdf_dict_put_rect(ap, mupdf.mupdf.pdf_new_name("BBox"), transformed.ToFzRect());
        }

        public static PdfObj pdf_dict_getl(PdfObj obj, string[] keys)
        {
            foreach (string key in keys)
            {
                if (obj.m_internal == null)
                    break;
                obj = obj.pdf_dict_get(new PdfObj(key));
            }
            return obj;
        }

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
            PdfObj ap = pdf_dict_getl(annotObj, new string[] { "AP", "N" });

            if (ap.m_internal == null)
                throw new InvalidOperationException(Constants.MSG_BAD_APN);

            ap.pdf_dict_put_matrix(new PdfObj("Matrix"), matrix.ToFzMatrix());
        }

        public void SetLanguage(string language = null)
        {
            var lang = string.IsNullOrEmpty(language)
                ? mupdf.fz_text_language.FZ_LANG_UNSET
                : mupdf.mupdf.fz_text_language_from_string(language);
            mupdf.mupdf.pdf_set_annot_language(NativeAnnot, lang);
        }

        /// <summary>
        /// Set line end codes.
        /// </summary>
        public void SetLineEnds(int start, int end)
        {
            if (mupdf.mupdf.pdf_annot_has_line_ending_styles(NativeAnnot) != 0)
                mupdf.mupdf.pdf_set_annot_line_ending_styles(NativeAnnot, (mupdf.pdf_line_ending)start, (mupdf.pdf_line_ending)end);
            else
                Trace.TraceWarning("bad annot type for line ends");
        }

        public void SetRotation(int rotate = 0)
        {
            switch (Type)
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
            if (Type == AnnotationType.FreeText && rotate % 90 != 0)
                rotate = 0;
            mupdf.mupdf.pdf_dict_put_int(AnnotObj, mupdf.mupdf.pdf_new_name("Rotate"), rotate);
        }

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

        /// <summary>
        /// Returns a string representation of this annotation.
        /// </summary>
        public override string ToString() => $"Annot('{TypeString}' on {Parent})";

        private byte[] _getAP()
        {
            var ap = GetAppearanceStreamObject("N");
            if (mupdf.mupdf.pdf_is_stream(ap) == 0)
                return null;
            // Python _getAP: returns AP/N stream bytes when present.
            return Helpers.BufferToBytes(mupdf.mupdf.pdf_load_stream(ap));
        }

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
            if (value is double[] da) return Array.ConvertAll(da, x => (float)x);
            if (value is int[] ia) return Array.ConvertAll(ia, x => (float)x);
            if (value is float f) return new[] { f };
            if (value is double d) return new[] { (float)d };
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

        // Python/legacy compatibility aliases (mirrors _alias(Annot, ...)).
        public byte[] get_file() => GetFile();
        public byte[] fileGet() => get_file();
        public Pixmap get_pixmap(Matrix matrix = null, Colorspace cs = null, bool alpha = false) => GetPixmap(matrix, cs, alpha);
        public Dictionary<string, object> get_sound() => GetSound();
        public byte[] soundGet() => GetSoundData();
        public TextPage get_textpage(int flags = 0) => GetTextPage(flags);
        public TextPage getTextPage(int flags = 0) => get_textpage(flags);
        public string get_text(string option = "text", int flags = 0)
        {
            using (var tp = GetTextPage(flags))
            {
                return tp.ExtractText();
            }
        }
        public string get_textbox(Rect rect, int flags = 0)
        {
            using (var tp = GetTextPage(flags))
            {
                return tp.ExtractTextbox(rect);
            }
        }
        public (int start, int end)? line_ends() => LineEnds;
        public void set_blendmode(string mode) => SetBlendMode(mode);
        public void setBlendMode(string mode) => set_blendmode(mode);
        public void set_border(float width = -1, float[] dashes = null, string style = null) => SetBorder(width, dashes, style);
        public void set_colors(float[] stroke = null, float[] fill = null) => SetColors(stroke, fill);
        public void set_flags(int flags) => Flags = flags;
        public void set_info(string content = null, string title = null, string creationDate = null, string modDate = null, string subject = null)
            => SetInfo(content, title, creationDate, modDate, subject);
        public void set_line_ends(int start, int end) => SetLineEnds(start, end);
        public void set_name(string name) => SetName(name);
        public void set_oc(int oc) => SetOC(oc);
        public void setOC(int oc) => set_oc(oc);
        public void set_opacity(float opacity) => SetOpacity(opacity);
        public bool? set_rect(Rect rect) => SetRect(rect);
        public void update_file(byte[] buffer = null, string filename = null, string ufilename = null, string desc = null)
            => UpdateFile(buffer, filename, ufilename, desc);
        public void fileUpd(byte[] buffer = null, string filename = null, string ufilename = null, string desc = null)
            => update_file(buffer, filename, ufilename, desc);
    }
}
