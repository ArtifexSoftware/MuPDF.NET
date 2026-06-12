using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MuPDF.NET
{
    /// <summary>
    /// MuPDF <see cref="mupdf.FzDevice2"/> implementations used when running a page for
    /// <see cref="Page.GetBboxlog"/> and <see cref="Page.GetTextTrace"/>.
    /// </summary>
    /// <remarks>
    /// <para>Ports PyMuPDF <c>JM_new_bbox_device_Device</c> and <c>JM_new_texttrace_device</c> in
    /// <c>src/__init__.py</c> (~23403 and ~23559). Types in this file are <c>internal</c>; they are not
    /// part of the public MuPDF.NET API.</para>
    /// <para>
    /// Override methods such as <c>fill_path</c> use MuPDF/SWIG names from <see cref="mupdf.FzDevice2"/>
    /// and cannot be renamed to PascalCase without breaking virtual dispatch from native code.
    /// </para>
    /// </remarks>
    internal static class PageRunDevices
    {
        /// <summary>Creates a bbox-logging MuPDF device.</summary>
        internal static PageBboxLogDevice NewBboxDevice(
            List<(string code, Rect bbox, string? layer)> result,
            bool includeLayers) =>
            new PageBboxLogDevice(result, includeLayers);

        /// <summary>Creates a bbox-logging MuPDF device.</summary>
        internal static PageBboxLogDevice JM_new_bbox_device(
            List<(string code, Rect bbox, string? layer)> rc,
            bool inc_layers) =>
            NewBboxDevice(rc, inc_layers);
    }

    /// <summary>
    /// Bounding-box log device for <see cref="Page.GetBboxlog"/> (PyMuPDF <c>JM_new_bbox_device_Device</c>).
    /// </summary>
    /// <remarks>
    /// Appends <c>(code, rect[, layer])</c> tuples to <paramref name="sink"/> for each drawing operation,
    /// matching <c>jm_bbox_add_rect</c>. When <c>layers</c> is truthy in Python (<see cref="Page.GetBboxlog"/>
    /// with <c>includeLayerNames</c>), the optional-content layer name is included.
    /// </remarks>
    internal sealed class PageBboxLogDevice : mupdf.FzDevice2
    {
        private readonly List<(string code, Rect bbox, string? layer)> _result;
        private readonly bool _includeLayers;
        private string _layerName = "";

        /// <summary>Accumulated bbox log (Python <c>dev.result</c>).</summary>
        internal IReadOnlyList<(string code, Rect bbox, string? layer)> Result => _result;

        internal PageBboxLogDevice(List<(string code, Rect bbox, string? layer)> sink, bool includeLayers)
        {
            _result = sink;
            _includeLayers = includeLayers;
            use_virtual_fill_path();
            use_virtual_stroke_path();
            use_virtual_fill_text();
            use_virtual_stroke_text();
            use_virtual_ignore_text();
            use_virtual_fill_shade();
            use_virtual_fill_image();
            use_virtual_fill_image_mask();
            use_virtual_begin_layer();
            use_virtual_end_layer();
        }

        /// <summary>Records a bbox entry on the bbox device.</summary>
        private void Add(string code, mupdf.fz_rect r)
        {
            var rect = new Rect(r);
            if (_includeLayers)
                _result.Add((code, rect, _layerName));
            else
                _result.Add((code, rect, null));
        }

        /// <summary>/ <c>JM_new_bbox_device_Device.begin_layer</c>.</summary>
        public override void begin_layer(mupdf.fz_context ctx, string name)
        {
            _layerName = string.IsNullOrEmpty(name) ? "" : name;
        }

        /// <summary>Ends a line-art layer on the trace device.</summary>
        public override void end_layer(mupdf.fz_context ctx)
        {
            _layerName = "";
        }

        /// <summary>BBox device callback for filled paths (<c>fill-path</c>).</summary>
        public override void fill_path(mupdf.fz_context ctx, mupdf.SWIGTYPE_p_fz_path path, int evenOdd, mupdf.fz_matrix ctm,
            mupdf.fz_colorspace colorspace, mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams)
        {
            try { Add("fill-path", mupdf.mupdf.ll_fz_bound_path(path, null, ctm)); }
            catch { }
        }

        /// <summary>BBox device callback for stroked paths (<c>stroke-path</c>).</summary>
        public override void stroke_path(mupdf.fz_context ctx, mupdf.SWIGTYPE_p_fz_path path, mupdf.fz_stroke_state stroke, mupdf.fz_matrix ctm,
            mupdf.fz_colorspace colorspace, mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams)
        {
            try { Add("stroke-path", mupdf.mupdf.ll_fz_bound_path(path, stroke, ctm)); }
            catch { }
        }

        /// <summary>BBox device callback for filled text (<c>fill-text</c>).</summary>
        public override void fill_text(mupdf.fz_context ctx, mupdf.fz_text text, mupdf.fz_matrix ctm, mupdf.fz_colorspace colorspace,
            mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams)
        {
            try { Add("fill-text", mupdf.mupdf.ll_fz_bound_text(text, null, ctm)); }
            catch { }
        }

        /// <summary>BBox device callback for stroked text (<c>stroke-text</c>).</summary>
        public override void stroke_text(mupdf.fz_context ctx, mupdf.fz_text text, mupdf.fz_stroke_state stroke, mupdf.fz_matrix ctm,
            mupdf.fz_colorspace colorspace, mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams)
        {
            try { Add("stroke-text", mupdf.mupdf.ll_fz_bound_text(text, stroke, ctm)); }
            catch { }
        }

        /// <summary>BBox device callback for ignored text (<c>ignore-text</c>).</summary>
        public override void ignore_text(mupdf.fz_context ctx, mupdf.fz_text text, mupdf.fz_matrix ctm)
        {
            try { Add("ignore-text", mupdf.mupdf.ll_fz_bound_text(text, null, ctm)); }
            catch { }
        }

        /// <summary>BBox device callback for shade fills (<c>fill-shade</c>).</summary>
        public override void fill_shade(mupdf.fz_context ctx, mupdf.fz_shade shade, mupdf.fz_matrix ctm, float alpha, mupdf.fz_color_params colorParams)
        {
            try { Add("fill-shade", mupdf.mupdf.ll_fz_bound_shade(shade, ctm)); }
            catch { }
        }

        /// <summary>BBox device callback for images (<c>fill-image</c>; unit rect transformed by <c>ctm</c>).</summary>
        public override void fill_image(mupdf.fz_context ctx, mupdf.fz_image image, mupdf.fz_matrix ctm, float alpha, mupdf.fz_color_params colorParams)
        {
            Add("fill-image", mupdf.mupdf.ll_fz_transform_rect(mupdf.mupdf.fz_unit_rect, ctm));
        }

        /// <summary>BBox device callback for image masks (<c>fill-imgmask</c>).</summary>
        public override void fill_image_mask(mupdf.fz_context ctx, mupdf.fz_image image, mupdf.fz_matrix ctm, mupdf.fz_colorspace colorspace,
            mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams)
        {
            try { Add("fill-imgmask", mupdf.mupdf.ll_fz_transform_rect(mupdf.mupdf.fz_unit_rect, ctm)); }
            catch { }
        }
    }

    /// <summary>
    /// Trace TEXT device for <see cref="Page.GetTextTrace"/> (PyMuPDF <c>JM_new_texttrace_device</c>).
    /// </summary>
    /// <remarks>
    /// <para>Docstring in Python: "Trace TEXT device for Python method Page.GetTextTrace()".</para>
    /// <para>
    /// Non-text operations bump <see cref="seqno"/> (<c>jm_increase_seqno</c> / <c>jm_dev_linewidth</c>).
    /// Text operations build span dictionaries via <c>jm_lineart_fill_text</c>, <c>jm_lineart_stroke_text</c>,
    /// and <c>jm_lineart_ignore_text</c> (type 0, 1, 3).
    /// </para>
    /// </remarks>
    internal sealed class JM_new_texttrace_device : mupdf.FzDevice2
    {
        /// <summary>Output span list (Python <c>dev.out</c>).</summary>
        internal readonly List<Dictionary<string, object>> Out;
        internal ulong seqno;
        internal float linewidth;
        /// <summary>Page transform set by <see cref="Page.GetTextTrace"/> before <c>fz_run_page</c>.</summary>
        internal mupdf.FzMatrix ptm = new mupdf.FzMatrix();
        internal string layer_name = "";

        internal JM_new_texttrace_device(List<Dictionary<string, object>> output)
        {
            Out = output;
            use_virtual_fill_path();
            use_virtual_stroke_path();
            use_virtual_fill_text();
            use_virtual_stroke_text();
            use_virtual_ignore_text();
            use_virtual_fill_shade();
            use_virtual_fill_image();
            use_virtual_fill_image_mask();
            use_virtual_begin_layer();
            use_virtual_end_layer();
        }

        /// <summary>Line-art trace device callback when a layer begins.</summary>
        public override void begin_layer(mupdf.fz_context ctx, string name) =>
            layer_name = string.IsNullOrEmpty(name) ? "" : name;

        /// <summary>Ends a line-art layer on the trace device.</summary>
        public override void end_layer(mupdf.fz_context ctx) => layer_name = "";

        /// <summary>Increments line-art sequence number for non-text fill paths.</summary>
        public override void fill_path(mupdf.fz_context ctx, mupdf.SWIGTYPE_p_fz_path path, int evenOdd, mupdf.fz_matrix ctm,
            mupdf.fz_colorspace colorspace, mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams) =>
            jm_increase_seqno();

        /// <summary>Records stroke width and increments the line-art sequence number.</summary>
        public override void stroke_path(mupdf.fz_context ctx, mupdf.SWIGTYPE_p_fz_path path, mupdf.fz_stroke_state stroke, mupdf.fz_matrix ctm,
            mupdf.fz_colorspace colorspace, mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams) =>
            jm_dev_linewidth(stroke);

        /// <summary>Increments the line-art trace sequence number.</summary>
        public override void fill_shade(mupdf.fz_context ctx, mupdf.fz_shade shade, mupdf.fz_matrix ctm, float alpha, mupdf.fz_color_params colorParams) =>
            jm_increase_seqno();

        /// <summary>Increments the line-art trace sequence number.</summary>
        public override void fill_image(mupdf.fz_context ctx, mupdf.fz_image image, mupdf.fz_matrix ctm, float alpha, mupdf.fz_color_params colorParams) =>
            jm_increase_seqno();

        /// <summary>Increments the line-art trace sequence number.</summary>
        public override void fill_image_mask(mupdf.fz_context ctx, mupdf.fz_image image, mupdf.fz_matrix ctm, mupdf.fz_colorspace colorspace,
            mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams) =>
            jm_increase_seqno();

        /// <summary>Line-art trace callback for filled text (type 0).</summary>
        public override void fill_text(mupdf.fz_context ctx, mupdf.fz_text text, mupdf.fz_matrix ctm, mupdf.fz_colorspace colorspace,
            mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams)
        {
            jm_lineart_fill_text(text, ctm, colorspace, color, alpha);
        }

        /// <summary>Line-art trace callback for stroked text (type 1).</summary>
        public override void stroke_text(mupdf.fz_context ctx, mupdf.fz_text text, mupdf.fz_stroke_state stroke, mupdf.fz_matrix ctm,
            mupdf.fz_colorspace colorspace, mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams)
        {
            jm_lineart_stroke_text(text, ctm, colorspace, color, alpha);
        }

        /// <summary>Line-art trace callback for ignored text (type 3).</summary>
        public override void ignore_text(mupdf.fz_context ctx, mupdf.fz_text text, mupdf.fz_matrix ctm) =>
            jm_lineart_ignore_text(text, ctm);

        private void jm_increase_seqno() => seqno++;

        private void jm_dev_linewidth(mupdf.fz_stroke_state stroke)
        {
            linewidth = stroke.linewidth;
            seqno++;
        }

        private void jm_lineart_fill_text(mupdf.fz_text text, mupdf.fz_matrix ctm, mupdf.fz_colorspace colorspace,
            mupdf.SWIGTYPE_p_float color, float alpha)
        {
            jm_trace_text(text, 0, ctm, colorspace, color, alpha, seqno);
            seqno++;
        }

        private void jm_lineart_stroke_text(mupdf.fz_text text, mupdf.fz_matrix ctm, mupdf.fz_colorspace colorspace,
            mupdf.SWIGTYPE_p_float color, float alpha)
        {
            jm_trace_text(text, 1, ctm, colorspace, color, alpha, seqno);
            seqno++;
        }

        private void jm_lineart_ignore_text(mupdf.fz_text text, mupdf.fz_matrix ctm)
        {
            jm_trace_text(text, 3, ctm, null, null, 1f, seqno);
            seqno++;
        }

        private void jm_trace_text(mupdf.fz_text text, int type_, mupdf.fz_matrix ctm, mupdf.fz_colorspace colorspace,
            mupdf.SWIGTYPE_p_float colorPtr, float alpha, ulong seqno_)
        {
            for (var raw = text.head; raw != null; raw = raw.next)
            {
                using var span = new mupdf.FzTextSpan(raw);
                jm_trace_text_span(span, type_, ctm, colorspace, colorPtr, alpha, seqno_);
            }
        }

        private void jm_trace_text_span(mupdf.FzTextSpan span, int type_, mupdf.fz_matrix ctm, mupdf.fz_colorspace colorspace,
            mupdf.SWIGTYPE_p_float colorPtr, float alpha, ulong seqno_)
        {
            const int TEXT_FONT_ITALIC = 2;
            const int TEXT_FONT_SERIFED = 4;
            const int TEXT_FONT_MONOSPACED = 8;
            const int TEXT_FONT_BOLD = 16;

            mupdf.fz_font font = span.m_internal.font;
            string fontname = Helpers.JM_font_name(font);
            var ctmFz = new mupdf.FzMatrix(ctm);
            using var trm = span.trm();
            using var mat = mupdf.mupdf.fz_concat(trm, ctmFz);
            float fsize;
            float dx, dy;
            using (var dir0 = mupdf.mupdf.fz_transform_vector(mupdf.mupdf.fz_make_point(1, 0), mat))
            {
                fsize = (float)Math.Sqrt(dir0.x * dir0.x + dir0.y * dir0.y);
                using (var dirN = dir0.fz_normalize_vector())
                {
                    dx = dirN.x;
                    dy = dirN.y;
                }
            }

            float asc = Helpers.JM_font_ascender(font);
            float dsc = Helpers.JM_font_descender(font);
            float asc_dsc = asc - dsc + 1.192092896e-07f;
            if (asc < 1e-3f)
            {
                dsc = -0.1f;
                asc = 0.9f;
                asc_dsc = 1.0f;
            }
            if (Helpers.SmallGlyphHeights || asc_dsc < 1)
            {
                dsc = dsc / asc_dsc;
                asc = asc / asc_dsc;
            }
            float ascsize = asc * fsize / (asc - dsc);
            float dscsize = dsc * fsize / (asc - dsc);

            int fflags = 0;
            if (mupdf.mupdf.ll_fz_font_is_monospaced(font) != 0) fflags |= TEXT_FONT_MONOSPACED;
            if (mupdf.mupdf.ll_fz_font_is_italic(font) != 0) fflags |= TEXT_FONT_ITALIC;
            if (mupdf.mupdf.ll_fz_font_is_serif(font) != 0) fflags |= TEXT_FONT_SERIFED;
            if (mupdf.mupdf.ll_fz_font_is_bold(font) != 0) fflags |= TEXT_FONT_BOLD;

            float lastAdv = 0f;
            float spaceAdv = 0f;
            var spanInternal = span.m_internal;
            int len = spanInternal.len;
            using var rot = mupdf.mupdf.fz_make_matrix(dx, dy, -dy, dx, 0, 0);
            if (Math.Abs(dx + 1f) < 1e-6f)
                rot.d = 1;

            var chars = new List<object>(len);
            Rect? spanAcc = null;
            for (int i = 0; i < len; i++)
            {
                using var item = span.items(i);
                float adv = 0f;
                if (item.gid >= 0)
                {
                    adv = mupdf.mupdf.ll_fz_advance_glyph(font, item.gid, (int)spanInternal.wmode) * fsize;
                    lastAdv = adv;
                }
                if (item.ucs == 32)
                    spaceAdv = adv;

                using var charOrig = mupdf.mupdf.fz_make_point(item.x, item.y);
                using var charOrigT = mupdf.mupdf.fz_transform_point(charOrig, ctmFz);
                using var m1 = mupdf.mupdf.fz_concat(
                    mupdf.mupdf.fz_make_matrix(1, 0, 0, 1, -charOrigT.x, -charOrigT.y),
                    mupdf.mupdf.fz_concat(rot, mupdf.mupdf.fz_make_matrix(1, 0, 0, 1, charOrigT.x, charOrigT.y)));

                float x0 = charOrigT.x;
                float x1 = x0 + adv;
                float y0, y1;
                if ((mat.d > 0 && (Math.Abs(dx - 1f) < 1e-6f || Math.Abs(dx + 1f) < 1e-6f))
                    || (Math.Abs(mat.b) > 1e-6f && Math.Abs(mat.b + mat.c) < 1e-6f))
                {
                    y0 = charOrigT.y + dscsize;
                    y1 = charOrigT.y + ascsize;
                }
                else
                {
                    y0 = charOrigT.y - ascsize;
                    y1 = charOrigT.y - dscsize;
                }

                using var charRect = mupdf.mupdf.fz_make_rect(x0, y0, x1, y1);
                using var charBbox = charRect.fz_transform_rect(m1);
                var cb = new Rect(charBbox);

                chars.Add(new object[]
                {
                    item.ucs,
                    item.gid,
                    new object[] { charOrigT.x, charOrigT.y },
                    new object[] { cb.X0, cb.Y0, cb.X1, cb.Y1 },
                });
                spanAcc = spanAcc == null ? cb : spanAcc.IncludeRect(cb);
            }

            if (spaceAdv <= 0f)
            {
                if ((fflags & TEXT_FONT_MONOSPACED) == 0)
                {
                    using var outparams = new mupdf.ll_fz_encode_character_with_fallback_outparams();
                    int c = mupdf.mupdf.ll_fz_encode_character_with_fallback_outparams_fn(
                        font, 32, 0, 0, outparams);
                    mupdf.fz_font advFont = outparams.out_font ?? font;
                    spaceAdv = mupdf.mupdf.ll_fz_advance_glyph(advFont, c, (int)spanInternal.wmode) * fsize;
                    if (spaceAdv <= 0f)
                        spaceAdv = lastAdv;
                }
                else
                {
                    spaceAdv = lastAdv;
                }
            }

            var (r, g, b) = jm_lineart_color(colorspace, colorPtr);
            float span_linewidth = linewidth > 0 ? linewidth : fsize * 0.05f;

            var span_dict = new Dictionary<string, object>
            {
                ["dir"] = new object[] { dx, dy },
                ["font"] = fontname,
                ["wmode"] = spanInternal.wmode,
                ["flags"] = fflags,
                ["bidi_lvl"] = (int)spanInternal.bidi_level,
                ["bidi_dir"] = (int)spanInternal.markup_dir,
                ["ascender"] = asc,
                ["descender"] = dsc,
                ["colorspace"] = 3,
                ["color"] = new[] { r, g, b },
                ["size"] = fsize,
                ["opacity"] = alpha,
                ["linewidth"] = span_linewidth,
                ["spacewidth"] = spaceAdv,
                ["type"] = type_,
                ["bbox"] = spanAcc ?? new Rect(),
                ["layer"] = layer_name,
                ["seqno"] = seqno_,
                ["chars"] = chars,
            };
            Out.Add(span_dict);
        }

        private static (float r, float g, float b) jm_lineart_color(mupdf.fz_colorspace colorspace, mupdf.SWIGTYPE_p_float colorPtr) =>
            ConvertColorToRgb(colorspace, colorPtr);

        private static (float r, float g, float b) ConvertColorToRgb(mupdf.fz_colorspace srcCs, mupdf.SWIGTYPE_p_float colorPtr)
        {
            if (srcCs == null || colorPtr == null)
                return (0f, 0f, 0f);
            using var cs = new mupdf.FzColorspace(mupdf.mupdf.ll_fz_keep_colorspace(srcCs));
            int n = mupdf.mupdf.fz_colorspace_n(cs);
            if (n <= 0 || n > 32)
                return (0f, 0f, 0f);
            var src = new float[n];
            Marshal.Copy(mupdf.SWIGTYPE_p_float.getCPtr(colorPtr).Handle, src, 0, n);
            var dst = new float[4];
            var h = GCHandle.Alloc(dst, GCHandleType.Pinned);
            var hSrc = GCHandle.Alloc(src, GCHandleType.Pinned);
            try
            {
                var pSrc = new mupdf.SWIGTYPE_p_float(hSrc.AddrOfPinnedObject(), false);
                var pDst = new mupdf.SWIGTYPE_p_float(h.AddrOfPinnedObject(), false);
                cs.fz_convert_color(pSrc, Helpers.DeviceColorspace(3), pDst, new mupdf.FzColorspace(), new mupdf.FzColorParams());
                return (dst[0], dst[1], dst[2]);
            }
            finally
            {
                h.Free();
                hSrc.Free();
            }
        }
    }
}
