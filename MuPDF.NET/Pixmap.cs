using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace MuPDF.NET
{
    /// <summary>
    /// Represents a pixmap (pixel map / raster image). Mirrors PyMuPDF's Pixmap class.
    /// </summary>
    public class Pixmap : IDisposable
    {
        private mupdf.FzPixmap _nativePixmap;
        private bool _disposed;

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

        /// <summary>
        /// Create a pixmap with explicit dimensions and optional initial samples.
        /// </summary>
        public Pixmap(Colorspace cs, int width, int height, byte[] samples, bool alpha = false)
        {
            _nativePixmap = mupdf.mupdf.fz_new_pixmap(cs, width, height, new mupdf.FzSeparations(), alpha ? 1 : 0);
            if (samples != null)
                SetSamples(samples);
        }

        /// <summary>
        /// Convert an existing pixmap to another colorspace.
        /// </summary>
        public Pixmap(Colorspace cs, Pixmap src)
        {
            _nativePixmap = mupdf.mupdf.fz_convert_pixmap(src.NativePixmap, cs, new mupdf.FzColorspace(), new mupdf.FzDefaultColorspaces(), new mupdf.FzColorParams(), 1);
        }

        /// <summary>
        /// Scale a pixmap to new dimensions, optionally clipping to a rectangle.
        /// </summary>
        public Pixmap(Pixmap src, int width, int height, IRect clip = null)
        {
            if (clip != null)
                _nativePixmap = mupdf.mupdf.fz_scale_pixmap(src.NativePixmap, src.X, src.Y, width, height, clip.ToFzIRect());
            else
                _nativePixmap = mupdf.mupdf.fz_scale_pixmap(src.NativePixmap, src.X, src.Y, width, height, new mupdf.FzIrect());
        }

        /// <summary>
        /// Clone a pixmap and optionally apply gamma correction.
        /// </summary>
        public Pixmap(Pixmap src, float alpha)
        {
            _nativePixmap = mupdf.mupdf.fz_clone_pixmap(src.NativePixmap);
            if (alpha >= 0) GammaWith(alpha);
        }

        /// <summary>
        /// Load a pixmap from an image file.
        /// </summary>
        public Pixmap(string filename)
        {
            var img = mupdf.mupdf.fz_new_image_from_file(filename);
            _nativePixmap = img.fz_get_pixmap_from_image(new mupdf.FzIrect(), new mupdf.FzMatrix(), null, null);
        }

        /// <summary>
        /// Load a pixmap from image bytes in memory.
        /// </summary>
        public Pixmap(byte[] data)
        {
            var buf = Helpers.BufferFromBytes(data);
            var img = mupdf.mupdf.fz_new_image_from_buffer(buf);
            _nativePixmap = img.fz_get_pixmap_from_image(new mupdf.FzIrect(), new mupdf.FzMatrix(), null, null);
        }

        internal Pixmap(mupdf.FzPixmap pix)
        {
            _nativePixmap = pix;
        }

        // ─── Properties ─────────────────────────────────────────────────

        /// <summary>
        /// Width of the region in pixels.
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
        /// Height of the region in pixels.
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
        /// x component of Pixmap origin.
        /// </summary>
        public int X => mupdf.mupdf.fz_pixmap_x(_nativePixmap);
        /// <summary>
        /// y component of Pixmap origin.
        /// </summary>
        public int Y => mupdf.mupdf.fz_pixmap_y(_nativePixmap);
        /// <summary>
        /// The size of one pixel (number of components).
        /// </summary>
        public int N => mupdf.mupdf.fz_pixmap_components(_nativePixmap);
        /// <summary>
        /// Indicates presence of alpha channel.
        /// </summary>
        public int Alpha => mupdf.mupdf.fz_pixmap_alpha(_nativePixmap);
        /// <summary>
        /// Length of one image line (width * n).
        /// </summary>
        public int Stride => mupdf.mupdf.fz_pixmap_stride(_nativePixmap);
        /// <summary>
        /// Resolution in x direction.
        /// </summary>
        public float XRes => _nativePixmap.m_internal.xres;
        /// <summary>
        /// Resolution in y direction.
        /// </summary>
        public float YRes => _nativePixmap.m_internal.yres;
        /// <summary>
        /// Pixmap size in bytes.
        /// </summary>
        public int Size => (int)mupdf.mupdf.fz_pixmap_size(_nativePixmap);

        /// <summary>
        /// Check if pixmap is monochrome.
        /// </summary>
        public bool IsMonochrome
        {
            get
            {
                if (N != 1 + Alpha) return false;
                return mupdf.mupdf.fz_is_pixmap_monochrome(_nativePixmap) != 0;
            }
        }

        /// <summary>
        /// Check if pixmap has only one color.
        /// </summary>
        public bool IsUnicolor
        {
            get
            {
                int n = N, w = Width, h = Height;
                int count = w * h * n;
                byte[] samples = Samples;
                byte[] sample0 = new byte[n];
                Array.Copy(samples, 0, sample0, 0, n);
                for (int offset = n; offset < count; offset += n)
                {
                    for (int i = 0; i < n; i++)
                    {
                        if (samples[offset + i] != sample0[i])
                            return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Pixmap bbox - an IRect object.
        /// </summary>
        public IRect IRect => new IRect(X, Y, X + Width, Y + Height);
        /// <summary>
        /// Pixmap Colorspace.
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
        /// The width.
        /// </summary>
        public int W => Width;
        /// <summary>
        /// The height.
        /// </summary>
        public int H => Height;

        /// <summary>
        /// Copy of the pixel area as bytes.
        /// </summary>
        public byte[] Samples
        {
            get
            {
                int len = Width * Height * N;
                var ptr = mupdf.mupdf.fz_pixmap_samples(_nativePixmap);
                var basePtr = mupdf.SWIGTYPE_p_unsigned_char.getCPtr(ptr).Handle;
                byte[] result = new byte[len];
                Marshal.Copy(basePtr, result, 0, len);
                return result;
            }
        }

        /// <summary>
        /// Pixmap samples pointer (same data as Samples).
        /// </summary>
        public byte[] SamplesPtr => Samples;

        /// <summary>
        /// Pixmap samples memoryview length (<c>Width * Height * N</c>).
        /// </summary>
        public int SamplesMv => Width * Height * N;

        /// <summary>
        /// MD5 digest of pixmap.
        /// </summary>
        public string Digest
        {
            get
            {
                var md5 = mupdf.mupdf.fz_md5_pixmap2(_nativePixmap);
                var bytes = new byte[md5.Count];
                for (int i = 0; i < md5.Count; i++)
                    bytes[i] = md5[i];
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        // ─── Methods ────────────────────────────────────────────────────

        /// <summary>
        /// Fill all color components with same value.
        /// </summary>
        public void ClearWith(int value = 0)
        {
            mupdf.mupdf.fz_clear_pixmap_with_value(_nativePixmap, value);
        }

        /// <summary>
        /// Fill all color components within a bbox with same value.
        /// </summary>
        public void ClearWith(int value, IRect irect)
        {
            mupdf.mupdf.fz_clear_pixmap_rect_with_value(_nativePixmap, value, irect.ToFzIRect());
        }

        /// <summary>
        /// Tint colors with modifiers for black and white.
        /// </summary>
        public void TintWith(int black, int white)
        {
            if (Colorspace == null || Colorspace.N > 3)
                throw new InvalidOperationException("colorspace invalid for function");
            mupdf.mupdf.fz_tint_pixmap(NativePixmap, black, white);
        }

        /// <summary>
        /// Apply gamma correction. gamma=1 is a no-op.
        /// </summary>
        public void GammaWith(float gamma)
        {
            if (Colorspace == null)
                throw new InvalidOperationException("colorspace invalid for function");
            mupdf.mupdf.fz_gamma_pixmap(NativePixmap, gamma);
        }

        /// <summary>
        /// Apply sharpening to the pixmap.
        /// </summary>
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
        /// Invert the colors inside a bbox.
        /// </summary>
        public bool InvertIRect(IRect irect = null)
        {
            if (Colorspace == null)
                return false;
            if (irect == null)
                mupdf.mupdf.fz_invert_pixmap(NativePixmap);
            else
                mupdf.mupdf.fz_invert_pixmap_rect(NativePixmap, irect.ToFzIRect());
            return true;
        }

        /// <summary>
        /// Set alpha channel to values contained in a byte array.
        /// If omitted, set all alpha values to 255.
        /// </summary>
        public void SetAlpha(byte[] alphaValues = null, int premultiply = 1, float[] opaque = null)
        {
            if (Alpha == 0) throw new InvalidOperationException("pixmap has no alpha channel");
            int w = Width, h = Height, n = N, stride = Stride;
            int alphaPos = n - 1;
            var ptr = mupdf.mupdf.fz_pixmap_samples(NativePixmap);
            var basePtr = mupdf.SWIGTYPE_p_unsigned_char.getCPtr(ptr).Handle;
            if (alphaValues != null)
            {
                if (alphaValues.Length != w * h)
                    throw new ArgumentException($"alpha values length must be {w * h}, got {alphaValues.Length}");
                int ai = 0;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int offset = y * stride + x * n + alphaPos;
                        Marshal.WriteByte(IntPtr.Add(basePtr, offset), alphaValues[ai++]);
                    }
                }
            }
            else
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int offset = y * stride + x * n + alphaPos;
                        Marshal.WriteByte(IntPtr.Add(basePtr, offset), 255);
                    }
                }
            }
            // MuPDF C# bindings currently do not expose fz_premultiply_pixmap.
        }

        /// <summary>
        /// Set top-left coordinates.
        /// </summary>
        public void SetOrigin(int x, int y)
        {
            NativePixmap.m_internal.x = x;
            NativePixmap.m_internal.y = y;
        }

        /// <summary>
        /// Set resolution in both dimensions.
        /// </summary>
        public void SetDpi(int xres, int yres)
        {
            NativePixmap.m_internal.xres = xres;
            NativePixmap.m_internal.yres = yres;
        }

        /// <summary>
        /// Set color of all pixels in bbox.
        /// </summary>
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
        /// Set color of a pixel.
        /// </summary>
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
        /// Get color tuple of pixel (x, y).
        /// Last item is the alpha if Pixmap.Alpha is true.
        /// </summary>
        public float[] GetPixel(int x, int y)
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
        /// Copy bbox from another Pixmap.
        /// </summary>
        public void CopyPixmap(Pixmap src, IRect irect)
        {
            mupdf.mupdf.fz_copy_pixmap_rect(NativePixmap, src.NativePixmap, irect.ToFzIRect(), new mupdf.FzDefaultColorspaces());
        }

        // ─── Color conversions ──────────────────────────────────────────

        /// <summary>
        /// Convert pixmap to another colorspace.
        /// </summary>
        public Pixmap ToColorspace(Colorspace cs, bool alpha = true)
        {
            var newPix = mupdf.mupdf.fz_convert_pixmap(NativePixmap, cs, new mupdf.FzColorspace(), new mupdf.FzDefaultColorspaces(), new mupdf.FzColorParams(), 1);
            return new Pixmap(newPix);
        }

        // ─── Save ───────────────────────────────────────────────────────

        /// <summary>
        /// Output as image in format determined by filename extension.
        /// </summary>
        public void Save(string filename, string output = null, int quality = 95)
        {
            string ext = output ?? System.IO.Path.GetExtension(filename).TrimStart('.');
            ext = ext.ToLower();

            if (Alpha != 0 && (ext == "pnm" || ext == "pgm" || ext == "ppm" || ext == "pbm" || ext == "ps" || ext == "jpg" || ext == "jpeg"))
                throw new ArgumentException($"'{ext}' cannot have alpha");
            if (Colorspace != null && Colorspace.N > 3 && (ext == "png" || ext == "pnm" || ext == "pgm" || ext == "ppm" || ext == "pbm"))
                throw new ArgumentException($"unsupported colorspace for '{ext}'");

            switch (ext)
            {
                case "png": mupdf.mupdf.fz_save_pixmap_as_png(NativePixmap, filename); break;
                case "pnm":
                case "pgm":
                case "ppm":
                case "pbm": mupdf.mupdf.fz_save_pixmap_as_pnm(NativePixmap, filename); break;
                case "pam": mupdf.mupdf.fz_save_pixmap_as_pam(NativePixmap, filename); break;
                case "psd": mupdf.mupdf.fz_save_pixmap_as_psd(NativePixmap, filename); break;
                case "ps":  mupdf.mupdf.fz_save_pixmap_as_ps(NativePixmap, filename, 0); break;
                case "jpg":
                case "jpeg":
                    SetDpi((int)XRes, (int)YRes);
                    mupdf.mupdf.fz_save_pixmap_as_jpeg(NativePixmap, filename, quality);
                    break;
                default:
                    mupdf.mupdf.fz_save_pixmap_as_png(NativePixmap, filename);
                    break;
            }
        }

        /// <summary>
        /// Convert to binary image stream of desired type.
        /// </summary>
        public byte[] ToBytes(string output = "png", int quality = 96)
        {
            mupdf.FzBuffer buf;
            switch (output?.ToLower())
            {
                case "pnm":
                case "pgm":
                case "ppm":
                case "pbm":
                    buf = mupdf.mupdf.fz_new_buffer_from_pixmap_as_pnm(NativePixmap, new mupdf.FzColorParams());
                    break;
                case "pam":
                    buf = mupdf.mupdf.fz_new_buffer_from_pixmap_as_pam(NativePixmap, new mupdf.FzColorParams());
                    break;
                case "psd":
                    buf = mupdf.mupdf.fz_new_buffer_from_pixmap_as_psd(NativePixmap, new mupdf.FzColorParams());
                    break;
                case "jpg":
                case "jpeg":
                    buf = mupdf.mupdf.fz_new_buffer_from_pixmap_as_jpeg(NativePixmap, new mupdf.FzColorParams(), quality, 0);
                    break;
                case "png":
                default:
                    buf = mupdf.mupdf.fz_new_buffer_from_pixmap_as_png(NativePixmap, new mupdf.FzColorParams());
                    break;
            }
            return buf.fz_buffer_extract();
        }

        /// <summary>
        /// Save pixmap as a PDF page.
        /// </summary>
        public byte[] ToPdfBytes(bool compressImages = true) =>
            PdfOCRSave(compress: compressImages);

        /// <summary>
        /// Save pixmap as an OCR-ed PDF page.
        /// </summary>
        public byte[] PdfOCRSave(bool compress = true)
        {
            var buf = mupdf.mupdf.fz_new_buffer(1024);
            var output = new mupdf.FzOutput(buf);
            output.fz_write_pixmap_as_pdfocr(NativePixmap, new mupdf.FzPdfocrOptions());
            mupdf.mupdf.fz_close_output(output);
            return buf.fz_buffer_extract();
        }

        // ─── SkiaSharp interop ───────────────────────────────────────────

        /// <summary>
        /// Convert pixmap to an SkiaSharp SKBitmap.
        /// Works cross-platform (Windows, Linux, macOS).
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
        /// Create a Pixmap from an SkiaSharp SKBitmap.
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
        /// Convert pixmap to PNG bytes.
        /// </summary>
        public byte[] ToPng() => ToBytes("png");

        /// <summary>
        /// Set the pixel area from bytes.
        /// </summary>
        public void SetSamples(byte[] data)
        {
            int expected = Width * Height * N;
            if (data.Length != expected)
                throw new ArgumentException($"samples length {data.Length} must be {expected}");
            var ptr = mupdf.mupdf.fz_pixmap_samples(_nativePixmap);
            var basePtr = mupdf.SWIGTYPE_p_unsigned_char.getCPtr(ptr).Handle;
            Marshal.Copy(data, 0, basePtr, data.Length);
        }

        /// <summary>
        /// Apply an affine transform to the pixmap.
        /// </summary>
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
        /// Return pixmap from a warped quad.
        /// </summary>
        public Pixmap Warp(Quad quad, int width, int height)
        {
            if (!quad.IsConvex) throw new ArgumentException("quad must be convex");
            var dst = NativePixmap.fz_warp_pixmap(quad.ToFzQuad(), width, height);
            return new Pixmap(dst);
        }

        /// <summary>
        /// Divide width and height by 2**factor.
        /// E.g. factor=1 shrinks to 25% of original size (in place).
        /// </summary>
        public Pixmap Shrink(int factor)
        {
            mupdf.mupdf.fz_subsample_pixmap(NativePixmap, factor);
            return this;
        }

        /// <summary>
        /// Return count of each color, or just the number of distinct colors.
        /// </summary>
        public object Color_Count(bool colors = false, IRect clip = null)
        {
            var counts = new Dictionary<string, (byte[] color, int count)>();
            int n = N, w = Width, h = Height, stride = Stride;
            byte[] samples = Samples;

            int x0 = clip != null ? Math.Max(clip.X0 - X, 0) : 0;
            int y0 = clip != null ? Math.Max(clip.Y0 - Y, 0) : 0;
            int x1 = clip != null ? Math.Min(clip.X1 - X, w) : w;
            int y1 = clip != null ? Math.Min(clip.Y1 - Y, h) : h;

            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    int offset = y * stride + x * n;
                    byte[] pixel = new byte[n];
                    Array.Copy(samples, offset, pixel, 0, n);
                    string key = BitConverter.ToString(pixel);
                    if (counts.ContainsKey(key))
                        counts[key] = (counts[key].color, counts[key].count + 1);
                    else
                        counts[key] = (pixel, 1);
                }
            }
            if (!colors)
                return counts.Count;
            var result = new Dictionary<byte[], int>();
            foreach (var kv in counts)
                result[kv.Value.color] = kv.Value.count;
            return result;
        }

        /// <summary>
        /// Return most frequent color and its usage ratio.
        /// </summary>
        public (float ratio, byte[] color) Color_TopUsage(IRect clip = null)
        {
            int n = N;
            var colorCounts = Color_Count(colors: true, clip: clip) as Dictionary<byte[], int>;
            if (colorCounts == null || colorCounts.Count == 0)
                return (1f, new byte[n]);

            int totalPixels = 0;
            byte[] topColor = null;
            int topCount = 0;
            foreach (var kv in colorCounts)
            {
                totalPixels += kv.Value;
                if (kv.Value > topCount)
                {
                    topCount = kv.Value;
                    topColor = kv.Key;
                }
            }
            float ratio = totalPixels > 0 ? (float)topCount / totalPixels : 0f;
            return (ratio, topColor ?? new byte[n]);
        }

        // Python/legacy compatibility aliases (mirrors _alias(Pixmap, ...)).
        public void clear_with(int value = 0) => ClearWith(value);
        public Pixmap copy(float alpha = -1) => new Pixmap(this, alpha);
        public Pixmap copyPixmap(float alpha = -1) => copy(alpha);
        public void gamma_with(float gamma) => GammaWith(gamma);
        public bool invert_irect(IRect irect = null) => InvertIRect(irect);
        public bool invertIRect(IRect irect = null) => invert_irect(irect);
        public void pil_save(string filename, string output = null, int quality = 95) => Save(filename, output, quality);
        public void pillowWrite(string filename, string output = null, int quality = 95) => pil_save(filename, output, quality);
        public byte[] pil_tobytes(string output = "png", int quality = 96) => ToBytes(output, quality);
        public byte[] pillowData(string output = "png", int quality = 96) => pil_tobytes(output, quality);
        public void save(string filename, string output = null, int quality = 95) => Save(filename, output, quality);
        public void writeImage(string filename, string output = null, int quality = 95) => save(filename, output, quality);
        public void writePNG(string filename, int quality = 95) => save(filename, "png", quality);
        public void set_alpha(byte[] alphaValues = null, int premultiply = 1, float[] opaque = null) => SetAlpha(alphaValues, premultiply, opaque);
        public void set_dpi(int xres, int yres) => SetDpi(xres, yres);
        public void setResolution(int xres, int yres) => set_dpi(xres, yres);
        public void set_origin(int x, int y) => SetOrigin(x, y);
        public void set_pixel(int x, int y, float[] color) => SetPixel(x, y, color);
        public void set_rect(IRect irect, float[] color) => SetRect(irect, color);
        public void tint_with(int black, int white) => TintWith(black, white);
        public byte[] tobytes(string output = "png", int quality = 96) => ToBytes(output, quality);
        public byte[] getImageData(string output = "png", int quality = 96) => tobytes(output, quality);
        public byte[] getPNGData() => tobytes("png");
        public byte[] getPNGdata() => getPNGData();

        // ─── IDisposable ────────────────────────────────────────────────

        /// <summary>
        /// Releases the native pixmap resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _nativePixmap?.Dispose();
                _nativePixmap = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~Pixmap() { Dispose(); }

        /// <summary>
        /// Returns a string describing colorspace, bounds, and alpha.
        /// </summary>
        public override string ToString() => $"Pixmap({Colorspace?.Name}, {IRect}, {(Alpha > 0 ? "alpha" : "no alpha")})";
    }
}
