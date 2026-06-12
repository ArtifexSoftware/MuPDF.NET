using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace MuPDF.NET
{
    /// <summary>
    /// Read-only view over a <see cref="Pixmap"/> sample buffer (PyMuPDF <c>samples_mv</c>).
    /// </summary>
    /// <remarks>Call <see cref="Release"/> when finished; the owning <see cref="Pixmap"/> also releases this view on dispose.</remarks>
    public sealed class PixmapSamplesMemoryView
    {
        private readonly Pixmap _pixmap;
        private IntPtr _base;
        private int _length;
        private bool _released;

        internal PixmapSamplesMemoryView(Pixmap pixmap)
        {
            _pixmap = pixmap;
            var pm = pixmap.NativePixmap;
            var ptr = mupdf.mupdf.fz_pixmap_samples(pm);
            _base = mupdf.SWIGTYPE_p_unsigned_char.getCPtr(ptr).Handle;
            _length = pm.fz_pixmap_stride() * pm.h();
        }

        public void Release()
        {
            _released = true;
        }

        private void CheckReleased()
        {
            if (_released)
                throw new ObjectDisposedException(nameof(PixmapSamplesMemoryView));
        }
        /// <summary>Number of bytes in the sample buffer.</summary>
        public int Length
        {
            get
            {
                CheckReleased();
                return _length;
            }
        }
        /// <summary>Bytes per element (always 1).</summary>
        public int ItemSize => 1;
        /// <summary>Total byte length (same as <see cref="Length"/>).</summary>
        public int NBytes => Length;
        /// <summary>Number of dimensions (always 1).</summary>
        public int NDim => 1;
        /// <summary>Shape tuple for the buffer.</summary>
        public int[] Shape => new[] { Length };
        /// <summary>Stride tuple for indexed access.</summary>
        public int[] Strides => new[] { 1 };

        internal int itemsize => ItemSize;
        internal int nbytes => NBytes;
        internal int ndim => NDim;
        internal int[] shape => Shape;
        internal int[] strides => Strides;

        public byte this[int index]
        {
            get
            {
                CheckReleased();
                if ((uint)index >= (uint)_length)
                    throw new IndexOutOfRangeException();
                return Marshal.ReadByte(IntPtr.Add(_base, index));
            }
        }
        /// <summary>Returns a copy of all sample bytes.</summary>
        public byte[] ToArray()
        {
            CheckReleased();
            byte[] buf = new byte[_length];
            Marshal.Copy(_base, buf, 0, _length);
            return buf;
        }
    }

    /// <summary>
    /// Raster image (pixel map) backed by MuPDF <c>fz_pixmap</c>.
    /// </summary>
    /// <remarks>
    /// <para>Pixmaps hold rectangular pixel data: color components per <see cref="Colorspace"/> plus an
    /// optional alpha channel. Create from <see cref="Page.GetPixmap"/>, files, byte buffers, PDF image
    /// xrefs, or other pixmaps (scale, convert colorspace, add mask).</para>
    /// <para>Ports PyMuPDF <c>Pixmap</c> (<c>src/__init__.py</c>).</para>
    /// </remarks>
    public class Pixmap : IDisposable
    {
        private mupdf.FzPixmap _nativePixmap;
        private bool _disposed;
        /// <summary><c>fz_scale_pixmap</c> results need <c>fz_drop_pixmap</c> before <c>delete_FzPixmap</c>.</summary>
        private bool _dropNativePixmapSamples;

        // Cached memory view over native samples (PyMuPDF samples_mv); kept for dispose safety.
        private PixmapSamplesMemoryView? _samplesMv;

        private object? _memoryView;

        internal mupdf.FzPixmap NativePixmap
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Pixmap));
                return _nativePixmap;
            }
        }

        // ─── Constructors ───────────────────────────────────────────────

        /// <summary>
        /// Create a pixmap with the given colorspace and bounding rectangle.
        /// </summary>
        public Pixmap(Colorspace cs, IRect irect, bool alpha = false)
        {
            _nativePixmap = mupdf.mupdf.fz_new_pixmap_with_bbox(cs, irect.ToFzIRect(), new mupdf.FzSeparations(), alpha ? 1 : 0);
            mupdf.mupdf.fz_clear_pixmap(_nativePixmap);
        }

        /// <summary>Creates an empty pixmap without clearing samples (integer alpha overload).</summary>
        public Pixmap(Colorspace cs, IRect irect, int alpha)
        {
            _nativePixmap = mupdf.mupdf.fz_new_pixmap_with_bbox(cs, irect.ToFzIRect(), new mupdf.FzSeparations(), alpha);
        }

        /// <summary>Legacy MuPDF.NET <see cref="ColorSpace"/> overload.</summary>
        public Pixmap(ColorSpace cs, IRect irect, int alpha = 0)
        {
            _nativePixmap = mupdf.mupdf.fz_new_pixmap_with_bbox(
                cs.ToFzColorspace(), irect.ToFzIRect(), new mupdf.FzSeparations(), alpha);
        }

        /// <summary>Legacy MuPDF.NET <see cref="ColorSpace"/> + samples constructor.</summary>
        public Pixmap(ColorSpace cs, int width, int height, byte[] samples, int alpha = 0)
            : this(new Colorspace(cs.ToFzColorspace()), width, height, samples, alpha != 0)
        {
        }

        /// <summary>
        /// Create a pixmap with explicit dimensions and optional initial samples.
        /// </summary>
        public Pixmap(Colorspace cs, int width, int height, byte[] samples, bool alpha = false)
        {
            int a = alpha ? 1 : 0;
            int n = mupdf.mupdf.fz_colorspace_n(cs);
            int stride = (n + a) * width;
            int expected = stride * height;
            if (samples != null && samples.Length != expected)
                throw new ValueErrorException(
                    $"bad samples length w={width} h={height} alpha={a} n={n} stride={stride} size={samples.Length}");
            _nativePixmap = mupdf.mupdf.fz_new_pixmap(cs, width, height, new mupdf.FzSeparations(), a);
            if (samples != null)
                SetSamples(samples);
        }

        /// <summary>Copies a pixmap, optionally converting colorspace or extracting alpha.</summary>
        public Pixmap(Colorspace cs, Pixmap src)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            var spix = src.NativePixmap;
            if (mupdf.mupdf.fz_pixmap_colorspace(spix).m_internal == null)
                throw new ValueErrorException("source colorspace must not be None");
            if (cs == null)
            {
                _nativePixmap = spix.fz_new_pixmap_from_alpha_channel();
                if (_nativePixmap.m_internal == null)
                    throw new InvalidOperationException(Constants.MSG_PIX_NOALPHA);
            }
            else
            {
                _nativePixmap = mupdf.mupdf.fz_convert_pixmap(
                    spix, cs, new mupdf.FzColorspace(), new mupdf.FzDefaultColorspaces(),
                    new mupdf.FzColorParams(), 1);
            }
        }

        /// <summary>Combines color and mask pixmaps into one image.</summary>
        public Pixmap(Pixmap colorPixmap, Pixmap maskPixmap)
        {
            if (maskPixmap == null) throw new ArgumentNullException(nameof(maskPixmap));
            var mpm = maskPixmap.NativePixmap;
            if (colorPixmap == null)
            {
                _nativePixmap = mpm.fz_new_pixmap_from_alpha_channel();
                if (_nativePixmap.m_internal == null)
                    throw new InvalidOperationException(Constants.MSG_PIX_NOALPHA);
            }
            else
            {
                _nativePixmap = colorPixmap.NativePixmap.fz_new_pixmap_from_color_and_mask(mpm);
            }
        }

        /// <summary>Creates a scaled copy of a pixmap, optionally clipped.</summary>
        public Pixmap(Pixmap src, int width, int height, IRect clip = null)
            : this(src, (float)width, (float)height, clip) { }

        public Pixmap(Pixmap src, float width, float height, IRect clip = null)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            int w = (int)width, h = (int)height;
            var spix = src.NativePixmap;
            mupdf.FzIrect bbox = clip != null ? clip.ToFzIRect() : InfiniteIrect();
            try
            {
                _nativePixmap = mupdf.mupdf.fz_scale_pixmap(
                    spix, spix.fz_pixmap_x(), spix.fz_pixmap_y(), w, h, bbox);
                _dropNativePixmapSamples = true;
            }
            finally
            {
                bbox?.Dispose();
            }
        }

        /// <summary>Copies a pixmap and adds or removes the alpha channel (0 or 1).</summary>
        public Pixmap(Pixmap src, int alpha)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (alpha != 0 && alpha != 1)
                throw new ValueErrorException("bad alpha value");
            var srcPix = src.NativePixmap;
            var cs = mupdf.mupdf.fz_pixmap_colorspace(srcPix);
            if (cs.m_internal == null && alpha == 0)
                throw new ValueErrorException("cannot drop alpha for 'NULL' colorspace");
            int n = srcPix.fz_pixmap_colorants();
            int w = mupdf.mupdf.fz_pixmap_width(srcPix);
            int h = mupdf.mupdf.fz_pixmap_height(srcPix);
            _nativePixmap = mupdf.mupdf.fz_new_pixmap(cs, w, h, new mupdf.FzSeparations(), alpha);
            _nativePixmap.m_internal.x = srcPix.m_internal.x;
            _nativePixmap.m_internal.y = srcPix.m_internal.y;
            _nativePixmap.m_internal.xres = srcPix.m_internal.xres;
            _nativePixmap.m_internal.yres = srcPix.m_internal.yres;

            // copy samples data ------------------------------------------
            // We use our pixmap_copy() to get best performance.
            CopyPixmapSamples(_nativePixmap, srcPix, alpha);
        }

        /// <summary>Clone; apply gamma when <paramref name="gamma"/> &gt;= 0.</summary>
        public Pixmap(Pixmap src, float gamma)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            _nativePixmap = mupdf.mupdf.fz_clone_pixmap(src.NativePixmap);
            if (gamma >= 0)
                GammaWith(gamma);
        }

        /// <summary>Wraps an existing pixmap without copying (tag <c>raw</c>).</summary>
        public Pixmap(string tag, Pixmap pm)
        {
            if (!string.Equals(tag, "raw", StringComparison.Ordinal))
                throw new ArgumentException($"Unrecognised Pixmap tag: {tag}");
            if (pm == null) throw new ArgumentNullException(nameof(pm));
            _nativePixmap = pm.NativePixmap;
        }

        /// <summary>Loads a pixmap from an image file.</summary>
        public Pixmap(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ValueErrorException("bad image data");
            using var img = mupdf.mupdf.fz_new_image_from_file(filename);
            _nativePixmap = PixmapFromImage(img);
        }

        /// <summary>Loads a pixmap from encoded image bytes.</summary>
        public Pixmap(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ValueErrorException("bad image data");
            using var buf = Helpers.BufferFromBytes(data);
            if (buf.m_internal == null || buf.m_internal.len == 0)
                throw new ValueErrorException("bad image data");
            using var img = mupdf.mupdf.fz_new_image_from_buffer(buf);
            _nativePixmap = PixmapFromImage(img);
        }

        /// <summary>
        /// Load pixmap pixels for a PDF image stream identified by xref.
        /// Port of PyMuPDF <c>Pixmap(Document, xref)</c> (<c>src/__init__.py</c>, PDF image branch).
        /// </summary>
        public Pixmap(Document document, int xref)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (document.IsClosed || document.IsEncrypted)
                throw new ValueErrorException("document closed or encrypted");
            if (!document.IsPdf)
                throw new ValueErrorException(Constants.MSG_IS_NO_PDF);

            var pdf = document.NativePdfDocument;
            if (xref < 1 || xref > mupdf.mupdf.pdf_xref_len(pdf) - 1)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);

            using var obj = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
            var subtype = Helpers.PdfDictGet(obj, mupdf.mupdf.pdf_new_name("Subtype"));
            if (subtype.m_internal == null)
                throw new ValueErrorException(Constants.MSG_IS_NO_IMAGE);
            string st = mupdf.mupdf.pdf_to_name(subtype);
            if (!string.Equals(st, "Image", StringComparison.Ordinal)
                && !string.Equals(st, "Alpha", StringComparison.Ordinal)
                && !string.Equals(st, "Luminosity", StringComparison.Ordinal))
                throw new ValueErrorException(Constants.MSG_IS_NO_IMAGE);

            using var img = pdf.pdf_load_image(obj);
            int iw = img.w();
            int ih = img.h();
            _nativePixmap = img.fz_get_pixmap_from_image(
                InfiniteIrect(), new mupdf.FzMatrix(iw, 0, 0, ih, 0, 0), null, null);
        }

        internal Pixmap(mupdf.FzPixmap pix) => _nativePixmap = pix;

        private static mupdf.FzIrect InfiniteIrect()
        {
            var ir = new mupdf.FzIrect();
            ir.x0 = Constants.FzMinInfRect;
            ir.y0 = Constants.FzMinInfRect;
            ir.x1 = Constants.FzMaxInfRect;
            ir.y1 = Constants.FzMaxInfRect;
            return ir;
        }

        private static mupdf.FzPixmap PixmapFromImage(mupdf.FzImage img)
        {
            int iw = img.w(), ih = img.h();
            using var subarea = InfiniteIrect();
            var pm = img.fz_get_pixmap_from_image(
                subarea, new mupdf.FzMatrix(iw, 0, 0, ih, 0, 0), null, null);
            using var res = new mupdf.ll_fz_image_resolution_outparams();
            mupdf.mupdf.ll_fz_image_resolution_outparams_fn(img.m_internal, res);
            pm.fz_set_pixmap_resolution(res.xres, res.yres);
            return pm;
        }

        // copy samples data — extra.pixmap_copy() in extra.i
        private static void CopyPixmapSamples(mupdf.FzPixmap dst, mupdf.FzPixmap src, int dstAlpha)
        {
            int w = mupdf.mupdf.fz_pixmap_width(src);
            int h = mupdf.mupdf.fz_pixmap_height(src);
            int dstN = mupdf.mupdf.fz_pixmap_components(dst);
            int srcN = mupdf.mupdf.fz_pixmap_components(src);
            if (dstN == srcN)
            {
                // identical samples
                int size = w * h * dstN;
                for (int i = 0; i < size; i++)
                    dst.fz_samples_set(i, mupdf.mupdf.fz_samples_get(src, i));
            }
            else
            {
                int nn;
                int doAlpha;
                int dstStride = dst.fz_pixmap_stride();
                int srcStride = src.fz_pixmap_stride();
                if (dstN > srcN)
                {
                    nn = srcN;
                    doAlpha = 1;
                }
                else
                {
                    nn = dstN;
                    doAlpha = 0;
                }
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int dstI = dstStride * y + dstN * x;
                        int srcI = srcStride * y + srcN * x;
                        for (int j = 0; j < nn; j++)
                            dst.fz_samples_set(dstI + j, mupdf.mupdf.fz_samples_get(src, srcI + j));
                        if (doAlpha != 0)
                            dst.fz_samples_set(dstI + dstN - 1, 255);
                    }
                }
            }
        }

        private void InvalidateSamplesCache()
        {
            _memoryView = null;
            _samplesMvRelease();
        }

        private void _samplesMvRelease()
        {
            if (_samplesMv != null)
            {
                _samplesMv.Release();
                _samplesMv = null;
            }
        }

        // ─── Properties (PyMuPDF) ───────────────────────────────────────
        /// <summary>
        /// pixmap width.
        /// </summary>
        public int Width
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Pixmap));
                return mupdf.mupdf.fz_pixmap_width(_nativePixmap);
            }
        }
        /// <summary>
        /// pixmap height.
        /// </summary>
        public int Height
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Pixmap));
                return mupdf.mupdf.fz_pixmap_height(_nativePixmap);
            }
        }
        /// <summary>
        /// X coordinate of the pixmap origin within its bounding rectangle.
        /// </summary>
        public int X => NativePixmap.fz_pixmap_x();
        /// <summary>
        /// Y coordinate of the pixmap origin within its bounding rectangle.
        /// </summary>
        public int Y => NativePixmap.fz_pixmap_y();
        /// <summary>
        /// Total bytes per pixel (color components plus alpha if present).
        /// </summary>
        public int N => mupdf.mupdf.fz_pixmap_components(_nativePixmap);
        /// <summary>
        /// 1 if the pixmap has an alpha channel, else 0.
        /// </summary>
        public int Alpha => mupdf.mupdf.fz_pixmap_alpha(_nativePixmap);
        /// <summary>
        /// Byte length of one image row (may include padding).
        /// </summary>
        public int Stride => NativePixmap.fz_pixmap_stride();
        /// <summary>
        /// Horizontal resolution in dots per inch.
        /// </summary>
        public int XRes => _nativePixmap.m_internal.xres;
        /// <summary>
        /// Vertical resolution in dots per inch.
        /// </summary>
        public int YRes => _nativePixmap.m_internal.yres;
        /// <summary>
        /// Total byte size of the sample buffer in memory.
        /// </summary>
        public int Size => (int)mupdf.mupdf.fz_pixmap_size(_nativePixmap);
        /// <summary>
        /// True if every pixel is black or white only.
        /// </summary>
        public bool IsMonochrome => mupdf.mupdf.fz_is_pixmap_monochrome(_nativePixmap) != 0;

        /// <summary>Legacy spelling of <see cref="IsMonochrome"/>.</summary>
        public bool IsMonoChrome => IsMonochrome;

        /// <summary>
        /// True if all pixels share the same color value.
        /// </summary>
        public bool IsUnicolor
        {
            get
            {
                var pm = NativePixmap;
                int n = pm.n();
                int count = pm.w() * pm.h() * n;
                byte[]? sample0 = null;
                for (int offset = 0; offset < count; offset += n)
                {
                    if (sample0 == null)
                    {
                        sample0 = new byte[n];
                        for (int i = 0; i < n; i++)
                            sample0[i] = (byte)pm.fz_samples_get(offset + i);
                    }
                    else
                    {
                        for (int i = 0; i < n; i++)
                        {
                            if (pm.fz_samples_get(offset + i) != sample0[i])
                                return false;
                        }
                    }
                }
                return true;
            }
        }
        /// <summary>
        /// Legacy spelling of IsUnicolor.
        /// </summary>
        public bool IsUniColor => IsUnicolor;
        /// <summary>
        /// IRect of the pixmap.
        /// </summary>
        public IRect IRect
        {
            get
            {
                using var b = NativePixmap.fz_pixmap_bbox();
                return new IRect(b.x0, b.y0, b.x1, b.y1);
            }
        }
        /// <summary>
        /// Colorspace of the pixmap, or null for alpha-only masks.
        /// </summary>
        public Colorspace Colorspace
        {
            get
            {
                var cs = mupdf.mupdf.fz_pixmap_colorspace(_nativePixmap);
                return cs.m_internal != null ? new Colorspace(cs) : null;
            }
        }
        /// <summary>
        /// Legacy Colorspace wrapper.
        /// </summary>
        public ColorSpace ColorSpace
        {
            get
            {
                var cs = mupdf.mupdf.fz_pixmap_colorspace(_nativePixmap);
                return cs.m_internal != null ? new ColorSpace(cs) : null;
            }
        }
        /// <summary>
        /// Width in pixels (alias).
        /// </summary>
        public int W => Width;
        /// <summary>
        /// Height in pixels (alias).
        /// </summary>
        public int H => Height;
        /// <summary>
        /// Copy of all pixel bytes (including alpha when present).
        /// </summary>
        public byte[] Samples
        {
            get
            {
                var mv = SamplesMv;
                return mv.ToArray();
            }
        }
        /// <summary>
        /// Legacy alias for Samples.
        /// </summary>
        public byte[] SAMPLES => Samples;
        /// <summary>
        /// Legacy alias for SamplesMv.
        /// </summary>
        public PixmapSamplesMemoryView SAMPLES_MV => SamplesMv;
        /// <summary>
        /// Unmanaged pointer to the first sample byte.
        /// </summary>
        public IntPtr SamplesPtr
        {
            get
            {
                return new IntPtr(mupdf.mupdf.fz_pixmap_samples_int(NativePixmap));
            }
        }
        /// <summary>
        /// Read-only memory view over the native sample buffer.
        /// </summary>
        public PixmapSamplesMemoryView SamplesMv
        {
            get
            {
                // We remember the returned memoryview so that our `__del__()` can
                // release it; otherwise accessing it after we have been destructed will
                // fail, possibly crashing Python; this is #4155.
                if (_samplesMv == null)
                    _samplesMv = new PixmapSamplesMemoryView(this);
                return _samplesMv;
            }
        }
        /// <summary>
        /// MD5 digest of the sample bytes.
        /// </summary>
        public byte[] Digest
        {
            get
            {
                var md5 = mupdf.mupdf.fz_md5_pixmap2(_nativePixmap);
                var bytes = new byte[md5.Count];
                for (int i = 0; i < md5.Count; i++)
                    bytes[i] = md5[i];
                return bytes;
            }
        }
        // ─── Methods ────────────────────────────────────────────────────
        /// <summary>
        /// Initialize the samples area.
        /// </summary>
        /// <param name="value">Fill byte 0–255 for each color component; alpha set to 255 when present. Omit to clear all bytes to 0.</param>
        /// <param name="bbox">Sub-rectangle to affect; only valid when <paramref name="value"/> is specified.</param>
        public void ClearWith(int? value = null, IRect bbox = null)
        {
            if (value == null)
                mupdf.mupdf.fz_clear_pixmap(_nativePixmap);
            else if (bbox == null)
                mupdf.mupdf.fz_clear_pixmap_with_value(_nativePixmap, value.Value);
            else
                NativePixmap.fz_clear_pixmap_rect_with_value(value.Value, bbox.ToFzIRect());
        }
        /// <summary>
        /// Colorize a pixmap by replacing black and / or white with colors given as sRGB integer values. Only colorspaces CS_GRAY and CS_RGB are supported, others are ignored with a warning.
        /// </summary>
        /// <param name="black">replace black with this value. Specifying 0x000000 makes no changes.</param>
        /// <param name="white">replace white with this value. Specifying 0xFFFFFF makes no changes.</param>
        public void TintWith(int black, int white)
        {
            if (Colorspace == null || Colorspace.N > 3)
                return;
            mupdf.mupdf.fz_tint_pixmap(NativePixmap, black, white);
        }
        /// <summary>
        /// Apply a gamma factor to a pixmap, i.e. lighten or darken it. Pixmaps with colorspace None are ignored with a warning.
        /// </summary>
        /// <param name="gamma">*gamma = 1.0* does nothing, *gamma &lt; 1.0* lightens, *gamma &gt; 1.0* darkens the image.</param>
        public void GammaWith(float gamma)
        {
            if (mupdf.mupdf.fz_pixmap_colorspace(_nativePixmap).m_internal == null)
                return;
            mupdf.mupdf.fz_gamma_pixmap(NativePixmap, gamma);
        }
        /// <summary>
        /// Apply a gamma factor to a pixmap, i.e. lighten or darken it. Pixmaps with colorspace None are ignored with a warning.
        /// </summary>
        /// <param name="radius">Sharpen filter radius.</param>
        public void SharpenWith(int radius = 2)
        {
            if (radius < 1) return;
            int w = Width, h = Height, n = N, stride = Stride;
            byte[] src = Samples;
            byte[] dst = new byte[src.Length];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    for (int c = 0; c < n; c++)
                    {
                        int idx = y * stride + x * n + c;
                        int sum = 0, count = 0;
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            for (int dx = -radius; dx <= radius; dx++)
                            {
                                int ny = y + dy, nx = x + dx;
                                if (ny >= 0 && ny < h && nx >= 0 && nx < w)
                                {
                                    sum += src[ny * stride + nx * n + c];
                                    count++;
                                }
                            }
                        }
                        int blur = sum / count;
                        int sharp = src[idx] + (src[idx] - blur);
                        dst[idx] = (byte)(sharp < 0 ? 0 : (sharp > 255 ? 255 : sharp));
                    }
                }
            }
            SetSamples(dst);
        }
        /// <summary>
        /// Invert the color of all pixels in IRect *irect*. Will have no effect if colorspace is None.
        /// </summary>
        /// <param name="bbox">Sub-rectangle to affect (IRect).</param>
        public bool InvertIRect(IRect bbox = null)
        {
            if (mupdf.mupdf.fz_pixmap_colorspace(_nativePixmap).m_internal == null)
                return false;
            if (bbox == null)
            {
                NativePixmap.fz_invert_pixmap();
                return true;
            }
            var r = bbox.ToFzIRect();
            if (r.fz_is_infinite_irect() != 0)
                NativePixmap.fz_invert_pixmap();
            else
                NativePixmap.fz_invert_pixmap_rect(r);
            return true;
        }
        /// <summary>
        /// Invert the color of all pixels in IRect *irect*. Will have no effect if colorspace is None.
        /// </summary>
        public bool InvertIrect(IRect bbox = null) => InvertIRect(bbox);
        /// <summary>
        /// Change the alpha values. The pixmap must have an alpha channel.
        /// </summary>
        /// <param name="alphaValues">Per-pixel alpha bytes.</param>
        /// <param name="premultiply">*New in v1.18.13:* whether to premultiply color components with the alpha value.</param>
        /// <param name="opaque">ignore the alpha value and set this color to fully transparent. A sequence of integers in `range(256)` with a length of n. Default is None. For example, a typical choice for RGB would be `opaque=(255, 255, 255)` (white).</param>
        /// <param name="matte">Matte color for SetAlpha.</param>
        public void SetAlpha(byte[] alphaValues = null, int premultiply = 1, int[] opaque = null, int[] matte = null)
        {
            var pix = NativePixmap;
            if (pix.alpha() == 0)
                throw new ValueErrorException(Constants.MSG_PIX_NOALPHA);
            int n = pix.fz_pixmap_colorants();
            int w = mupdf.mupdf.fz_pixmap_width(pix);
            int h = mupdf.mupdf.fz_pixmap_height(pix);
            int balen = w * h * (n + 1);
            var colors = new mupdf.vectori();   // make this color opaque
            var bgcolor = new mupdf.vectori();  // preblending background color
            for (int i = 0; i < 4; i++)
            {
                colors.Add(0);
                bgcolor.Add(0);
            }
            int zeroOut = 0;
            int bground = 0;
            if (opaque != null && opaque.Length == n)
            {
                for (int i = 0; i < n; i++)
                    colors[i] = opaque[i];
                zeroOut = 1;
            }
            if (matte != null && matte.Length == n)
            {
                for (int i = 0; i < n; i++)
                    bgcolor[i] = matte[i];
                bground = 1;
            }
            int dataLen = 0;
            GCHandle? pin = null;
            mupdf.SWIGTYPE_p_unsigned_char dataPtr = null;
            try
            {
                if (alphaValues != null)
                {
                    if (alphaValues.Length < w * h)
                        throw new ValueErrorException("bad alpha values");
                    dataLen = alphaValues.Length;
                    pin = GCHandle.Alloc(alphaValues, GCHandleType.Pinned);
                    dataPtr = new mupdf.SWIGTYPE_p_unsigned_char(pin.Value.AddrOfPinnedObject(), false);
                }
                // Use C implementation for speed.
                mupdf.mupdf.Pixmap_set_alpha_helper(
                    balen, n, dataLen, zeroOut, dataPtr, pix.m_internal,
                    premultiply, bground, colors, bgcolor);
            }
            finally
            {
                if (pin != null)
                    pin.Value.Free();
            }
            InvalidateSamplesCache();
        }
        /// <summary>
        /// Set the x and y values of the pixmap's top-left point.
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        public void SetOrigin(int x, int y)
        {
            NativePixmap.m_internal.x = x;
            NativePixmap.m_internal.y = y;
        }
        /// <summary>
        /// Set the resolution (dpi) in x and y direction.
        /// </summary>
        /// <param name="xres">resolution in x direction.</param>
        /// <param name="yres">resolution in y direction.</param>
        public void SetDpi(int xres, int yres)
        {
            NativePixmap.m_internal.xres = xres;
            NativePixmap.m_internal.yres = yres;
        }
        /// <summary>
        /// .. note::.
        /// </summary>
        /// <param name="irect">the rectangle to be filled with the value. The actual area is the intersection of this parameter and irect. For an empty intersection (or an invalid parameter), no change will happen.</param>
        /// <param name="color">the desired value, given as a sequence of integers in `range(256)`. The length of the sequence must equal n, which includes any alpha byte.</param>
        /// <returns>False if the rectangle was invalid or had an empty intersection with irect, else True.</returns>
        public void SetRect(IRect irect, float[] color)
        {
            int n = N;
            if (color.Length != n)
                throw new ArgumentException($"color length {color.Length} must match {n}");
            byte[] colorBytes = new byte[n];
            for (int i = 0; i < n; i++)
            {
                int component = (int)(color[i] * 255 + 0.5f);
                colorBytes[i] = (byte)(component < 0 ? 0 : (component > 255 ? 255 : component));
            }

            int stride = Stride;
            var ptr = mupdf.mupdf.fz_pixmap_samples(NativePixmap);
            var basePtr = mupdf.SWIGTYPE_p_unsigned_char.getCPtr(ptr).Handle;
            int x0 = Math.Max(irect.X0, X), y0 = Math.Max(irect.Y0, Y);
            int x1 = Math.Min(irect.X1, X + Width), y1 = Math.Min(irect.Y1, Y + Height);

            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    int offset = (y - Y) * stride + (x - X) * n;
                    for (int c = 0; c < n; c++)
                        Marshal.WriteByte(IntPtr.Add(basePtr, offset + c), colorBytes[c]);
                }
            }
        }
        /// <summary>
        /// set color and alpha of a pixel.
        /// </summary>
        /// <param name="x">the column number of the pixel. Must be in `range(pix.width)`.</param>
        /// <param name="y">the line number of the pixel. Must be in `range(pix.height)`.</param>
        /// <param name="color">the desired pixel value given as a sequence of integers in `range(256)`. The length of the sequence must equal n, which includes any alpha byte.</param>
        public void SetPixel(int x, int y, float[] color)
        {
            int n = N;
            if (color.Length != n) throw new ArgumentException($"color length {color.Length} must match {n}");
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException($"pixel ({x},{y}) out of range");
            int stride = Stride;
            var ptr = mupdf.mupdf.fz_pixmap_samples(NativePixmap);
            var basePtr = mupdf.SWIGTYPE_p_unsigned_char.getCPtr(ptr).Handle;
            int offset = y * stride + x * n;
            for (int c = 0; c < n; c++)
            {
                int component = (int)(color[c] * 255 + 0.5f);
                Marshal.WriteByte(IntPtr.Add(basePtr, offset + c), (byte)(component < 0 ? 0 : (component > 255 ? 255 : component)));
            }
        }
        /// <summary>
        /// return the value of a pixel.
        /// </summary>
        /// <param name="x">the column number of the pixel. Must be in `range(pix.width)`.</param>
        /// <param name="y">the line number of the pixel, Must be in `range(pix.height)`.</param>
        /// <returns>a list of color values and, potentially the alpha value. Its length and content depend on the pixmap's colorspace and the presence of an alpha. For RGBA pixmaps the result would e.g. be *[r, g, b, a]*. All items are integers in `range(256)`.</returns>
        public byte[] GetPixel(int x, int y)
        {
            var pm = NativePixmap;
            if (x < 0 || x >= pm.w() || y < 0 || y >= pm.h())
                throw new ValueErrorException(Constants.MSG_PIXEL_OUTSIDE);
            int n = pm.n();
            int stride = pm.fz_pixmap_stride();
            int i = stride * y + n * x;
            var mv = SamplesMv;
            byte[] ret = new byte[n];
            for (int j = 0; j < n; j++)
                ret[j] = mv[i + j];
            return ret;
        }
        /// <summary>
        /// return the value of a pixel.
        /// </summary>
        public byte[] GetPixelBytes(int x, int y) => GetPixel(x, y);
        /// <summary>
        /// return the value of a pixel.
        /// </summary>
        /// <param name="x">the column number of the pixel. Must be in `range(pix.width)`.</param>
        /// <param name="y">the line number of the pixel, Must be in `range(pix.height)`.</param>
        /// <returns>a list of color values and, potentially the alpha value. Its length and content depend on the pixmap's colorspace and the presence of an alpha. For RGBA pixmaps the result would e.g. be *[r, g, b, a]*. All items are integers in `range(256)`.</returns>
        public float[] GetPixelFloat(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException($"pixel ({x},{y}) out of range");
            int n = N, stride = Stride;
            var ptr = mupdf.mupdf.fz_pixmap_samples(NativePixmap);
            var basePtr = mupdf.SWIGTYPE_p_unsigned_char.getCPtr(ptr).Handle;
            int offset = y * stride + x * n;
            float[] result = new float[n];
            for (int c = 0; c < n; c++)
                result[c] = Marshal.ReadByte(IntPtr.Add(basePtr, offset + c)) / 255.0f;
            return result;
        }
        /// <summary>
        /// Copy the *irect* part of the *source* pixmap into the corresponding area of this one. The two pixmaps may have different dimensions and can each have CS_GRAY or CS_RGB colorspaces, but they currently must have the same alpha property . The copy mechanism automatically adjusts discrepancies between source and target like so:.
        /// </summary>
        /// <param name="src">Source pixmap.</param>
        /// <param name="bbox">Sub-rectangle to affect (IRect).</param>
        public void Copy(Pixmap src, IRect bbox)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            var srcPix = src.NativePixmap;
            if (mupdf.mupdf.fz_pixmap_colorspace(srcPix).m_internal == null)
                throw new ValueErrorException("cannot copy pixmap with NULL colorspace");
            if (NativePixmap.alpha() != srcPix.alpha())
                throw new ValueErrorException("source and target alpha must be equal");
            mupdf.mupdf.fz_copy_pixmap_rect(
                NativePixmap, srcPix, bbox.ToFzIRect(), new mupdf.FzDefaultColorspaces());
        }

        // ─── Color conversions ──────────────────────────────────────────
        /// <summary>
        /// return a memory area in a variety of formats.
        /// </summary>
        /// <param name="cs">Target or source colorspace.</param>
        /// <param name="alpha">Whether to include an alpha channel (0/1 or bool).</param>
        public Pixmap ToColorspace(Colorspace cs, bool alpha = true)
        {
            var newPix = mupdf.mupdf.fz_convert_pixmap(NativePixmap, cs, new mupdf.FzColorspace(), new mupdf.FzDefaultColorspaces(), new mupdf.FzColorParams(), 1);
            return new Pixmap(newPix);
        }

        // ─── Save ───────────────────────────────────────────────────────
        /// <summary>
        /// Save pixmap as an image file. Depending on the output chosen, only some or all colorspaces are supported and different file extensions can be chosen. Please see the table below.
        /// </summary>
        /// <param name="filename">The file to save to. May be provided as a string, as a pathlib.Path or as a Python file object. In the latter two cases, the filename is taken from the resp. object. The filename's extension determines the image format, which can be overruled by the output parameter.</param>
        /// <param name="output">The desired image format. The default is the filename's extension. If both, this value and the file extension are unsupported, an exception is raised. For possible values see PixmapOutput.</param>
        /// <param name="jpg_quality">The desired image quality, default 95. Only applies to JPEG images, else ignored. This parameter trades quality against file size. A value of 98 is close to lossless. Higher values should not lead to better quality.</param>
        public void Save(string filename, string output = null, int jpg_quality = 95)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            string path = filename;
            if (Path.GetExtension(path) == "" && output != null)
                path = filename;
            string ext = output ?? Path.GetExtension(path).TrimStart('.');
            ext = ext.ToLowerInvariant();
            int? idx = FormatIndex(ext);
            if (idx == null)
                throw new ValueErrorException($"Image format {ext} not in supported set");
            if (Alpha != 0 && (idx == 2 || idx == 6 || idx == 7))
                throw new ValueErrorException($"'{ext}' cannot have alpha");
            if (Colorspace != null && Colorspace.N > 3 && (idx == 1 || idx == 2 || idx == 4))
                throw new ValueErrorException($"unsupported colorspace for '{ext}'");
            if (idx == 7)
                SetDpi(XRes, YRes);
            WriteImage(path, idx.Value, jpg_quality);
        }

        private static int? FormatIndex(string ext) => ext switch
        {
            "png" => 1,
            "pnm" or "pgm" or "ppm" or "pbm" => 2,
            "pam" => 3,
            "psd" => 5,
            "ps" => 6,
            "jpg" or "jpeg" => 7,
            _ => null,
        };

        private void WriteImage(string filename, int format, int jpg_quality)
        {
            var pm = NativePixmap;
            switch (format)
            {
                case 1: mupdf.mupdf.fz_save_pixmap_as_png(pm, filename); break;
                case 2: mupdf.mupdf.fz_save_pixmap_as_pnm(pm, filename); break;
                case 3: mupdf.mupdf.fz_save_pixmap_as_pam(pm, filename); break;
                case 5: mupdf.mupdf.fz_save_pixmap_as_psd(pm, filename); break;
                case 6: mupdf.mupdf.fz_save_pixmap_as_ps(pm, filename, 0); break;
                case 7: mupdf.mupdf.fz_save_pixmap_as_jpeg(pm, filename, jpg_quality); break;
                default: mupdf.mupdf.fz_save_pixmap_as_png(pm, filename); break;
            }
        }

        private byte[] ToBytesInternal(int format, int jpg_quality)
        {
            var pm = NativePixmap;
            int size = pm.fz_pixmap_stride() * pm.h();
            using var res = mupdf.mupdf.fz_new_buffer((uint)size);
            using var out_ = new mupdf.FzOutput(res);
            switch (format)
            {
                case 1: out_.fz_write_pixmap_as_png(pm); break;
                case 2: out_.fz_write_pixmap_as_pnm(pm); break;
                case 3: out_.fz_write_pixmap_as_pam(pm); break;
                case 5: out_.fz_write_pixmap_as_psd(pm); break;
                case 6: out_.fz_write_pixmap_as_ps(pm); break;
                case 7: out_.fz_write_pixmap_as_jpeg(pm, jpg_quality, 0); break;
                default: out_.fz_write_pixmap_as_png(pm); break;
            }
            out_.fz_close_output();
            return Helpers.BinFromBuffer(res);
        }
        /// <summary>
        /// return a memory area in a variety of formats.
        /// </summary>
        /// <param name="output">The requested image format. The default is "png". For other possible values see PixmapOutput.</param>
        /// <param name="jpg_quality">The desired image quality, default 95. Only applies to JPEG images, else ignored. This parameter trades quality against file size. A value of 98 is close to lossless. Higher values should not lead to better quality.</param>
        public byte[] ToBytes(string output = "png", int jpg_quality = 95)
        {
            string ext = (output ?? "png").ToLowerInvariant();
            int? idx = FormatIndex(ext);
            if (idx == null)
                throw new ValueErrorException($"Image format {output} not in supported set");
            if (Alpha != 0 && (idx == 2 || idx == 6 || idx == 7))
                throw new ValueErrorException($"'{output}' cannot have alpha");
            if (Colorspace != null && Colorspace.N > 3 && (idx == 1 || idx == 2 || idx == 4))
                throw new ValueErrorException($"unsupported colorspace for '{output}'");
            if (idx == 7)
                SetDpi(XRes, YRes);
            return ToBytesInternal(idx.Value, jpg_quality);
        }
        /// <summary>
        /// Returns a one-page searchable PDF (OCR text layer) as bytes; alias for <see cref="PdfOCRSave"/>.
        /// </summary>
        public byte[] ToPdfBytes(bool compressImages = true) =>
            PdfOCRSave(compress: compressImages);

        /// <summary>
        /// Runs Tesseract OCR and writes a one-page searchable PDF to <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">Output PDF path.</param>
        /// <param name="compress">Whether to compress the PDF (default true).</param>
        /// <param name="language">Tesseract language code(s), e.g. <c>eng</c> or <c>eng+deu</c>.</param>
        /// <param name="tessdata">Tesseract <c>tessdata</c> folder; uses <c>TESSDATA_PREFIX</c> when null.</param>
        public void SavePdfOCR(string filename, bool compress = true, string language = "eng", string tessdata = null)
        {
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));
            var opts = BuildPdfOcrOptions(compress, language, tessdata);
            NativePixmap.fz_save_pixmap_as_pdfocr(filename, 0, opts);
        }

        /// <summary>
        /// Runs Tesseract OCR and returns a one-page searchable PDF in memory (PyMuPDF <c>pdfocr_tobytes</c>).
        /// </summary>
        /// <param name="compress">Whether to compress the PDF (default true).</param>
        /// <param name="language">Tesseract language code(s).</param>
        /// <param name="tessdata">Tesseract data directory.</param>
        /// <returns>PDF file bytes suitable for <c>new Document("pdf", bytes)</c>.</returns>
        public byte[] PdfOCRSave(bool compress = true, string language = "eng", string tessdata = null)
        {
            var opts = BuildPdfOcrOptions(compress, language, tessdata);
            var buf = mupdf.mupdf.fz_new_buffer(1024);
            var output = new mupdf.FzOutput(buf);
            output.fz_write_pixmap_as_pdfocr(NativePixmap, opts);
            mupdf.mupdf.fz_close_output(output);
            return buf.fz_buffer_extract();
        }

        /// <summary>
        /// Legacy MuPDF.NET name for <see cref="PdfOCRSave"/> (OCR PDF as bytes).
        /// </summary>
        public byte[] PdfOCR2Bytes(bool compress = true, string language = "eng", string tessdata = null) =>
            PdfOCRSave(compress, language, tessdata);
        /// <summary>
        /// Applies an ImageFilterPipeline and returns a new pixmap.
        /// </summary>
        /// <param name="pixmap">Input pixmap for static helpers.</param>
        /// <param name="pipeline">Image filter pipeline to apply.</param>
        public static Pixmap ApplyImageFilters(Pixmap pixmap, ImageFilterPipeline pipeline)
        {
            if (pipeline == null || pixmap == null)
                return pixmap;

            var filters = pipeline.Filters;
            if (filters == null || filters.Count == 0)
                return pixmap;

            byte[] sourceBytes = pixmap.ToBytes("png", 95);
            if (sourceBytes == null || sourceBytes.Length == 0)
                return pixmap;

            SKBitmap workingBitmap = SKBitmap.Decode(sourceBytes);
            if (workingBitmap == null)
                return pixmap;

            try
            {
                pipeline.Apply(ref workingBitmap);
                ImageFilter.EnsureColorType(ref workingBitmap, SKColorType.Rgb888x, SKAlphaType.Opaque);

                using (var image = SKImage.FromBitmap(workingBitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    var bytes = data.ToArray();
                    if (bytes == null || bytes.Length == 0)
                        return pixmap;

                    pixmap.Dispose();
                    return new Pixmap(bytes);
                }
            }
            finally
            {
                workingBitmap.Dispose();
            }
        }
        /// <summary>
        /// Runs Tesseract OCR and returns extracted text.
        /// </summary>
        /// <param name="language">Tesseract language code(s), e.g. eng or eng+deu.</param>
        /// <param name="tessdata">Path to tessdata directory.</param>
        /// <param name="compress">Whether to compress the OCR PDF.</param>
        public string GetTextFromOcr(
            ImageFilterPipeline imageFilters = null,
            string language = "eng",
            string tessdata = null,
            bool compress = true,
            int flags = 0)
        {
            if (string.IsNullOrEmpty(Utils.TESSDATA_PREFIX) && string.IsNullOrEmpty(tessdata))
                throw new Exception("No OCR support: TESSDATA_PREFIX not set");

            Pixmap processedPixmap = new Pixmap(ToBytes("png", 95));
            bool disposeProcessedPixmap = true;

            try
            {
                if (imageFilters != null)
                {
                    var filtered = ApplyImageFilters(processedPixmap, imageFilters);
                    processedPixmap = filtered;
                }

                if (N - Alpha != 3)
                {
                    var tempPixmap = processedPixmap;
                    processedPixmap = new Pixmap(Colorspace.Rgb, tempPixmap);
                    tempPixmap.Dispose();
                }

                if (Alpha != 0)
                {
                    var tempPixmap = processedPixmap;
                    processedPixmap = new Pixmap(tempPixmap, 0);
                    tempPixmap.Dispose();
                }

                byte[] ocrPdfBytes = processedPixmap.PdfOCR2Bytes(compress, language, tessdata);
                if (ocrPdfBytes == null || ocrPdfBytes.Length == 0)
                    throw new Exception("Failed to generate OCR PDF");

                Document ocrDoc = null;
                Page ocrPage = null;
                TextPage textPage = null;

                try
                {
                    ocrDoc = new Document(ocrPdfBytes, "pdf");
                    ocrPage = ocrDoc.LoadPage(0);
                    textPage = ocrPage.GetTextPage(flags: flags);
                    return textPage.ExtractText();
                }
                finally
                {
                    textPage?.Dispose();
                    ocrPage?.Dispose();
                    ocrDoc?.Close();
                }
            }
            finally
            {
                if (disposeProcessedPixmap && processedPixmap != null)
                    processedPixmap.Dispose();
            }
        }

        static mupdf.FzPdfocrOptions BuildPdfOcrOptions(bool compress, string language, string tessdata)
        {
            string tessdataDir = Helpers.GetTessdata(tessdata);
            var opts = new mupdf.FzPdfocrOptions();
            opts.compress = compress ? 1 : 0;
            if (!string.IsNullOrEmpty(language))
                opts.language = language;
            if (!string.IsNullOrEmpty(tessdataDir))
                opts.datadir = tessdataDir;
            return opts;
        }

        // ─── SkiaSharp interop ───────────────────────────────────────────
        /// <summary>
        /// Exports pixels to an SkiaSharp SKBitmap.
        /// </summary>
        public SKBitmap ToSKBitmap()
        {
            int w = Width, h = Height, n = N;
            bool hasAlpha = Alpha != 0;
            var cs = Colorspace;

            SKColorType colorType;
            if (cs != null && cs.N == 3 && hasAlpha)
                colorType = SKColorType.Rgba8888;
            else if (cs != null && cs.N == 3 && !hasAlpha)
                colorType = SKColorType.Rgb888x;
            else if (cs != null && cs.N == 1 && !hasAlpha)
                colorType = SKColorType.Gray8;
            else
            {
                var png = ToPng();
                return SKBitmap.Decode(png);
            }

            var info = new SKImageInfo(w, h, colorType, hasAlpha ? SKAlphaType.Unpremul : SKAlphaType.Opaque);
            var bmp = new SKBitmap(info);
            var samples = Samples;

            if (colorType == SKColorType.Rgba8888 || colorType == SKColorType.Rgb888x)
            {
                if (n == 4)
                {
                    var ptr = bmp.GetPixels();
                    Marshal.Copy(samples, 0, ptr, samples.Length);
                }
                else
                {
                    var ptr = bmp.GetPixels();
                    unsafe
                    {
                        byte* dst = (byte*)ptr.ToPointer();
                        int si = 0;
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < w; x++)
                            {
                                dst[0] = samples[si];
                                dst[1] = samples[si + 1];
                                dst[2] = samples[si + 2];
                                dst[3] = 255;
                                dst += 4;
                                si += 3;
                            }
                        }
                    }
                }
            }
            else
            {
                var ptr = bmp.GetPixels();
                Marshal.Copy(samples, 0, ptr, samples.Length);
            }

            return bmp;
        }
        /// <summary>
        /// Creates a pixmap from an SkiaSharp bitmap.
        /// </summary>
        public static Pixmap FromSKBitmap(SKBitmap bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var pngBytes = data.ToArray();

            var buf = Helpers.BufferFromBytes(pngBytes);
            var img = mupdf.mupdf.fz_new_image_from_buffer(buf);
            var nativePix = img.fz_get_pixmap_from_image(new mupdf.FzIrect(), new mupdf.FzMatrix(), null, null);
            return new Pixmap(nativePix);
        }
        /// <summary>
        /// Returns PNG-encoded image bytes.
        /// </summary>
        public byte[] ToPng() => ToBytes("png");
        /// <summary>
        /// Replaces sample bytes from a caller-provided buffer.
        /// </summary>
        /// <param name="data">Image file bytes.</param>
        public void SetSamples(byte[] data)
        {
            int expected = Stride * Height;
            if (data.Length != expected)
                throw new ArgumentException($"samples length {data.Length} must be {expected}");
            var ptr = mupdf.mupdf.fz_pixmap_samples(_nativePixmap);
            var basePtr = mupdf.SWIGTYPE_p_unsigned_char.getCPtr(ptr).Handle;
            Marshal.Copy(data, 0, basePtr, data.Length);
            InvalidateSamplesCache();
        }
        /// <summary>
        /// return a pixmap made from a quad inside.
        /// </summary>
        /// <param name="matrix">Affine matrix for WarpAffine.</param>
        /// <param name="irect">Bounding rectangle (position and size).</param>
        public Pixmap WarpAffine(Matrix matrix, IRect irect = null)
        {
            int w = Width, h = Height, n = N;
            bool hasAlpha = Alpha != 0;
            var src = Samples;
            int srcStride = Stride;

            int dstW = irect?.Width ?? w;
            int dstH = irect?.Height ?? h;

            var result = new Pixmap(Colorspace, new IRect(0, 0, dstW, dstH), hasAlpha);
            var dst = new byte[dstW * dstH * n];
            int dstStride = dstW * n;

            float det = (float)(matrix.A * matrix.D - matrix.B * matrix.C);
            if (Math.Abs(det) < 1e-10f)
                return result;

            float invA = (float)(matrix.D / det), invB = (float)(-matrix.B / det);
            float invC = (float)(-matrix.C / det), invD = (float)(matrix.A / det);
            float invE = (float)((matrix.C * matrix.F - matrix.D * matrix.E) / det);
            float invF = (float)((matrix.B * matrix.E - matrix.A * matrix.F) / det);

            for (int dy = 0; dy < dstH; dy++)
            {
                for (int dx = 0; dx < dstW; dx++)
                {
                    float sx = invA * dx + invC * dy + invE;
                    float sy = invB * dx + invD * dy + invF;
                    int isx = (int)sx, isy = (int)sy;
                    if (isx >= 0 && isx < w && isy >= 0 && isy < h)
                    {
                        int srcOff = isy * srcStride + isx * n;
                        int dstOff = dy * dstStride + dx * n;
                        for (int c = 0; c < n; c++)
                            dst[dstOff + c] = src[srcOff + c];
                    }
                }
            }

            result.SetSamples(dst);
            return result;
        }
        /// <summary>
        /// return a pixmap made from a quad inside.
        /// </summary>
        /// <param name="quad">Source quad inside the pixmap for Warp.</param>
        /// <param name="width">Target width in pixels.</param>
        /// <param name="height">Target height in pixels.</param>
        public Pixmap Warp(Quad quad, int width, int height)
        {
            if (!quad.IsConvex) throw new ArgumentException("quad must be convex");
            var dst = NativePixmap.fz_warp_pixmap(quad.ToFzQuad(), width, height);
            return new Pixmap(dst);
        }
        /// <summary>
        /// Shrink the pixmap by dividing both, its width and height by 2\ :sup:n.
        /// </summary>
        /// <param name="factor">Subsample factor for Shrink (≥1).</param>
        public Pixmap Shrink(int factor)
        {
            if (factor < 1)
                return this;
            mupdf.mupdf.fz_subsample_pixmap(NativePixmap, factor);
            InvalidateSamplesCache();
            return this;
        }
        /// <summary>
        /// determine used colors.
        /// </summary>
        /// <param name="colors">If true, ColorCount returns a histogram dictionary.</param>
        /// <param name="clip">Clip rectangle inside the pixmap.</param>
        public object ColorCount(bool colors = false, IRect clip = null)
        {
            var rc = Helpers.JmColorCount(NativePixmap, clip);
            if (!colors)
                return rc.Count;
            return rc;
        }

        /// <summary>Return most frequent color and its usage ratio (PyMuPDF <c>Pixmap.color_topusage</c>).</summary>
        public (float ratio, byte[] color) ColorTopUsage(IRect clip = null)
        {
            int allpixels = 0;
            int cnt = 0;
            byte[]? maxpixel = null;
            if (clip != null && new Rect(IRect).Contains(new Rect(clip)))
                clip = IRect;
            foreach (var kv in (Dictionary<byte[], int>)ColorCount(colors: true, clip: clip))
            {
                allpixels += kv.Value;
                if (kv.Value > cnt)
                {
                    cnt = kv.Value;
                    maxpixel = kv.Key;
                }
            }
            if (allpixels == 0)
                return (1f, Enumerable.Repeat((byte)255, N).ToArray());
            return ((float)cnt / allpixels, maxpixel!);
        }

        private static int[]? OpaqueToInt(float[]? v)
        {
            if (v == null) return null;
            var a = new int[v.Length];
            for (int i = 0; i < v.Length; i++)
                a[i] = (int)v[i];
            return a;
        }

        private static int[]? MatteToInt(float[]? v) => OpaqueToInt(v);

        // ─── PyMuPDF API names (internal, same assembly) ─────────────────

        internal int x => X;
        internal int y => Y;
        internal int n => N;
        internal int alpha => Alpha;
        internal int stride => Stride;
        internal int xres => XRes;
        internal int yres => YRes;
        internal int size => Size;
        internal int w => W;
        internal int h => H;
        internal int width => Width;
        internal int height => Height;
        internal bool is_monochrome => IsMonochrome;
        internal bool is_unicolor => IsUnicolor;
        internal IRect irect => IRect;
        internal byte[] samples => Samples;
        internal IntPtr samples_ptr => SamplesPtr;
        internal PixmapSamplesMemoryView samples_mv => SamplesMv;
        internal byte[] digest => Digest;

        internal byte[] pixel(int x, int y) => GetPixel(x, y);
        internal object color_count(bool colors = false, IRect clip = null) => ColorCount(colors, clip);
        internal (float ratio, byte[] color) color_topusage(IRect clip = null) => ColorTopUsage(clip);
        internal (float ratio, byte[] color) Color_TopUsage(IRect clip = null) => ColorTopUsage(clip);
        internal object Color_Count(bool colors = false, IRect clip = null) => ColorCount(colors, clip);

        internal byte[] pdfocr_tobytes(bool compress = true, string language = "eng", string tessdata = null) =>
            PdfOCRSave(compress, language, tessdata);
        internal void pdfocr_save(string filename, bool compress = true, string language = "eng", string tessdata = null) =>
            SavePdfOCR(filename, compress, language, tessdata);
        internal Pixmap copy(int alpha = 1) => new Pixmap(this, alpha);
        internal Pixmap copy() => new Pixmap(this, -1f);
        internal void copy_pixmap(Pixmap src, IRect irect) => Copy(src, irect);
        internal void copyPixmap(Pixmap src, IRect irect) => Copy(src, irect);
        internal void gamma_with(float gamma) => GammaWith(gamma);
        internal bool invert_irect(IRect irect = null) => InvertIRect(irect);
        internal bool invertIRect(IRect irect = null) => InvertIRect(irect);
        internal void pil_save(string filename, string output = null, int quality = 95) => Save(filename, output, quality);
        internal void pillowWrite(string filename, string output = null, int quality = 95) => Save(filename, output, quality);
        internal byte[] pil_tobytes(string output = "png", int quality = 96) => ToBytes(output, quality);
        internal byte[] pillowData(string output = "png", int quality = 96) => ToBytes(output, quality);
        internal void save(string filename, string output = null, int quality = 95) => Save(filename, output, quality);
        internal void writeImage(string filename, string output = null, int quality = 95) => Save(filename, output, quality);
        internal void writePNG(string filename, int quality = 95) => Save(filename, "png", quality);
        internal void set_alpha(byte[] alphaValues = null, int premultiply = 1, float[] opaque = null, float[] matte = null) =>
            SetAlpha(alphaValues, premultiply, OpaqueToInt(opaque), MatteToInt(matte));
        internal void set_dpi(int xres, int yres) => SetDpi(xres, yres);
        internal void setResolution(int xres, int yres) => SetDpi(xres, yres);
        internal void set_origin(int x, int y) => SetOrigin(x, y);
        internal void set_pixel(int x, int y, float[] color) => SetPixel(x, y, color);
        internal void set_rect(IRect irect, float[] color) => SetRect(irect, color);
        internal void tint_with(int black, int white) => TintWith(black, white);
        internal byte[] tobytes(string output = "png", int quality = 96) => ToBytes(output, quality);
        internal byte[] getImageData(string output = "png", int quality = 96) => ToBytes(output, quality);
        internal byte[] getPNGData() => ToBytes("png");
        internal byte[] getPNGdata() => ToBytes("png");

        private void ReleaseNativePixmap()
        {
            if (_nativePixmap == null)
                return;
            if (_dropNativePixmapSamples)
                Helpers.DropFzPixmap(ref _nativePixmap);
            else
            {
                _nativePixmap.Dispose();
                _nativePixmap = null;
            }
        }

        // ─── IDisposable ────────────────────────────────────────────────
        /// <summary>
        /// Releases the native fz_pixmap and cached sample view.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _samplesMvRelease();
                ReleaseNativePixmap();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~Pixmap()
        {
            if (_disposed)
                return;
            _samplesMvRelease();
            ReleaseNativePixmap();
            _disposed = true;
        }
        /// <summary>
        /// Diagnostic string with colorspace, IRect, and alpha flag.
        /// </summary>
        public override string ToString()
        {
            string cs = Colorspace?.Name ?? "none";
            if (string.Equals(cs, "None", StringComparison.Ordinal))
                cs = "none";
            return $"Pixmap({cs}, {IRect}, {Alpha})";
        }
    }
}
