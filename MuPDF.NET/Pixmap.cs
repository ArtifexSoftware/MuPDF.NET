using mupdf;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace MuPDF.NET
{
    public class Pixmap : IDisposable
    {
        static Pixmap()
        {
            Utils.InitApp();
        }

        private FzPixmap _nativePixmap;
        private bool _disposed;

        public Pixmap(ColorSpace cs, IRect irect, int alpha = 0)
        {
            _nativePixmap = mupdf.mupdf.fz_new_pixmap_with_bbox(cs.ToFzColorspace(), irect.ToFzIrect(), new FzSeparations(0), alpha);
        }

        public Pixmap(Document doc, int xref)
        {
            PdfDocument pdf = Document.AsPdfDocument(doc);
            int xrefLen = pdf.pdf_xref_len();
            if (!Utils.INRANGE(xref, 1, xrefLen - 1))
            {
                pdf.Dispose();
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);
            }

            PdfObj r = pdf.pdf_new_indirect(xref, 0);
            PdfObj type = r.pdf_dict_get(new PdfObj("Subtype"));
            if (type.pdf_name_eq(new PdfObj("Image")) == 0 &&
                type.pdf_name_eq(new PdfObj("Alpha")) == 0 &&
                type.pdf_name_eq(new PdfObj("Luminosity")) == 0)
                throw new Exception(Utils.ErrorMessages["MSG_IS_NO_IMAGE"]);
            FzImage img = pdf.pdf_load_image(r);
            FzPixmap pix = img.fz_get_pixmap_from_image(new FzIrect(Utils.FZ_MIN_INF_RECT, Utils.FZ_MIN_INF_RECT, Utils.FZ_MAX_INF_RECT, Utils.FZ_MAX_INF_RECT),
                new FzMatrix(img.w(), 0, 0, img.h(), 0, 0),
                null,
                null
                );
            _nativePixmap = pix;

            pdf.Dispose();
        }

        public Pixmap(Pixmap spix, float w, float h)
        {
            FzIrect bbox = new FzIrect(mupdf.mupdf.fz_infinite_irect);
            if (spix == null)
                throw new Exception("bad pixmap");
            FzPixmap srcPix = spix.ToFzPixmap();
            FzPixmap pm = null;
            if (bbox.fz_is_infinite_irect() == 0)
                pm = srcPix.fz_scale_pixmap(srcPix.x(), srcPix.y(), w, h, bbox);
            else
                pm = srcPix.fz_scale_pixmap(srcPix.x(), srcPix.y(), w, h, new FzIrect(mupdf.mupdf.fz_infinite_irect));
            _nativePixmap = pm;
        }

        /// <summary>
        /// Contains the IRect of the pixmap
        /// </summary>
        private IRect _irect = null;
        public IRect IRect
        {
            get
            {
                if (_irect != null)
                {
                    return _irect;
                }
                FzIrect val = _nativePixmap.fz_pixmap_bbox();
                _irect = new IRect(val);

                return _irect;
            }
        }

        /// <summary>
        /// transparency indicator
        /// </summary>
        public int Alpha
        {
            get
            {
                return _nativePixmap.alpha();
            }
        }

        /// <summary>
        /// pixmap's Colorspace
        /// </summary>
        private ColorSpace _colorSpace = null;
        public ColorSpace ColorSpace
        {
            get
            {
                if (_colorSpace == null)
                {
                    _colorSpace = new ColorSpace(_nativePixmap.fz_pixmap_colorspace());
                }
                return _colorSpace;
            }
        }

        /// <summary>
        /// MD5 hashcode of the pixmap
        /// </summary>
        public byte[] Digest
        {
            get
            {
                vectoruc res = _nativePixmap.fz_md5_pixmap2();
                byte[] ret = new byte[res.Count];
                res.CopyTo(ret, 0);
                return ret;
            }
        }

        /// <summary>
        /// pixmap height
        /// </summary>
        public int H
        {
            get
            {
                return _nativePixmap.fz_pixmap_height();
            }
        }

        /// <summary>
        /// True for a gray pixmap which only has the colors black and white
        /// </summary>
        public bool IsMonoChrome
        {
            get
            {
                return Convert.ToBoolean(_nativePixmap.fz_is_pixmap_monochrome());
            }
        }

        /// <summary>
        /// True if all pixels are identical (any colorspace)
        /// </summary>
        public bool IsUniColor
        {
            get
            {
                FzPixmap pm = _nativePixmap;
                byte n = pm.n();
                int count = pm.w() * pm.h();
                List<byte> sample0 = PixmapReadSamples(0, n);
                List<byte> sample = null;
                for (int i = n; i < count; i += n)
                {
                    sample = PixmapReadSamples(i, n);
                    if (!sample.SequenceEqual(sample0))
                        return false;
                }
            
                return true;
            }
        }

        /// <summary>
        /// bytes per pixel
        /// </summary>
        private int _n = -1;
        public int N
        {
            get
            {
                if (_n < 0)
                    _n =  _nativePixmap.fz_pixmap_components();

                return _n;
            }
        }

        /// <summary>
        /// Memory of pixel area
        /// </summary>
        public Memory<byte> SAMPLES_MV
        {
            get
            {
                if (_nativePixmap.m_internal == null)
                    return null;

                Memory<byte> ret = new Memory<byte>(SAMPLES);
                
                return ret;
            }
        }

        /// <summary>
        /// bytes copy of pixel area
        /// </summary>
        private byte[] _samples = null;
        public byte[] SAMPLES
        {
            get
            {
                if (_samples != null)
                    return _samples;

                //if (_nativePixmap.m_internal == null)
                //    return null;

                int size = (ColorSpace.N + Alpha) * _nativePixmap.w() * _nativePixmap.h();
                _samples = new byte[size];
                SWIGTYPE_p_unsigned_char pData = _nativePixmap.samples();
                Marshal.Copy(SWIGTYPE_p_unsigned_char.getCPtr(pData).Handle, _samples, 0, size);

                return _samples;
            }
        }

        /// <summary>
        /// pointer to pixel area
        /// </summary>
        public long SamplesPtr
        {
            get
            {
                return _nativePixmap.fz_pixmap_samples_int();
            }
        }

        /// <summary>
        /// pixmap's total length
        /// </summary>
        public int Size
        {
            get
            {
                return _nativePixmap.n() * _nativePixmap.w() * _nativePixmap.h();
            }
        }

        /// <summary>
        /// size of one image row
        /// </summary>
        private int _stride = -1;
        public int Stride
        {
            get
            {
                if (_stride < 0)
                    _stride = _nativePixmap.fz_pixmap_stride();
                return _stride;
            }
        }

        /// <summary>
        /// pixmap width
        /// </summary>
        public int W
        {
            get
            {
                return _nativePixmap.fz_pixmap_width();
            }
        }

        /// <summary>
        /// X-coordinate of top-left corner
        /// </summary>
        private int _x = -1;
        public int X
        {
            get
            {
                if (_x < 0)
                {
                    _x = _nativePixmap.fz_pixmap_x();
                }
                return _x;
            }
        }

        /// <summary>
        /// resolution in X-direction
        /// </summary>
        public int Xres
        {
            get
            {
                return _nativePixmap.xres();
            }
        }

        /// <summary>
        /// Y-coordinate of top-left corner
        /// </summary>
        private int _y = -1;
        public int Y
        {
            get
            {
                if (_y < 0)
                    _y = _nativePixmap.fz_pixmap_y();
                return _y;
            }
        }

        /// <summary>
        /// resolution in Y-direction
        /// </summary>
        public int Yres
        {
            get
            {
                return _nativePixmap.yres();
            }
        }

        private List<byte> PixmapReadSamples(int offset, int n)
        {
            List<byte> ret = new List<byte>();
            for (int i = 0; i < n; i++)
            {
                ret.Add(Convert.ToByte(_nativePixmap.fz_samples_get(offset + i)));
            }
            
            return ret;
        }

        public Pixmap(ColorSpace cs, Pixmap src)
        {
            ColorSpace cs_ = cs;
            FzPixmap pix = src.ToFzPixmap();
            if (pix.fz_pixmap_colorspace().m_internal == null)
                throw new Exception("source colorspace must not be None");

            if (cs_ != null)
            {
                _nativePixmap = pix.fz_convert_pixmap(cs_.ToFzColorspace(), new FzColorspace(0), new FzDefaultColorspaces(), new FzColorParams(), 1);
            }
            else
            {
                _nativePixmap = pix.fz_new_pixmap_from_alpha_channel();
                if (_nativePixmap == null)
                    throw new Exception(Utils.ErrorMessages["MSG_PIX_NOALPHA"]);
            }
        }

        public Pixmap(Pixmap src, float width, float height, Rect clip = null)
        {
            FzIrect bBox = new FzIrect(mupdf.mupdf.fz_infinite_irect);
            if (clip != null)
                bBox = new FzIrect(clip.ToFzRect());

            FzPixmap srcPix = src.ToFzPixmap();
            FzPixmap pm;
            if (bBox.fz_is_infinite_irect() == 0)
                pm = srcPix.fz_scale_pixmap(srcPix.x(), srcPix.y(), width, height, bBox);
            else
                pm = srcPix.fz_scale_pixmap(srcPix.x(), srcPix.y(), width, height, new FzIrect(mupdf.mupdf.fz_infinite_irect));
            _nativePixmap = pm;
        }

        public Pixmap(FzPixmap fzPix)
        {
            _nativePixmap = fzPix;
        }

        public Pixmap(string filename)
        {
            FzImage img = mupdf.mupdf.fz_new_image_from_file(filename);
            FzPixmap pix = img.fz_get_pixmap_from_image(
                new FzIrect(Utils.FZ_MIN_INF_RECT, Utils.FZ_MIN_INF_RECT, Utils.FZ_MAX_INF_RECT, Utils.FZ_MAX_INF_RECT),
                new FzMatrix(img.w(), 0, 0, img.h(), 0, 0),
                new SWIGTYPE_p_int(IntPtr.Zero, false),
                new SWIGTYPE_p_int(IntPtr.Zero, false)
                );
            (int xres, int yres) = img.fz_image_resolution();
            pix.m_internal.xres = xres;
            pix.m_internal.yres = yres;
            _nativePixmap = pix;
            img.Dispose();
        }

        public Pixmap(byte[] image)
        {
            FzBuffer buffer = Utils.BufferFromBytes(image);
            FzImage img = mupdf.mupdf.fz_new_image_from_buffer(buffer);
            FzPixmap pix = img.fz_get_pixmap_from_image(
                new FzIrect(Utils.FZ_MIN_INF_RECT, Utils.FZ_MIN_INF_RECT, Utils.FZ_MAX_INF_RECT, Utils.FZ_MAX_INF_RECT),
                new FzMatrix(img.w(), 0, 0, img.h(), 0, 0),
                new SWIGTYPE_p_int(IntPtr.Zero, false),
                new SWIGTYPE_p_int(IntPtr.Zero, false)
                );
            (int xres, int yres) = img.fz_image_resolution();
            pix.m_internal.xres = xres;
            pix.m_internal.yres = yres;
            _nativePixmap = pix;
        }

        public Pixmap(ColorSpace cs, int w, int h, byte[] samples, int alpha)
        {
            int n = cs.N;
            int stride = (n + alpha) * w;
            FzSeparations seps = new FzSeparations();
            FzPixmap pixmap = mupdf.mupdf.fz_new_pixmap(cs.ToFzColorspace(), w, h, seps, alpha);
            int size = 0;

            size = samples.Length;

            if (stride * h != size)
            {
                throw new Exception($"bad samples length {w} {h} {alpha} {n} {stride} {size}");
            }
            Marshal.Copy(samples, 0, SWIGTYPE_p_unsigned_char.getCPtr(pixmap.samples()).Handle, size);
            _nativePixmap = pixmap;
        }

        public Pixmap(string arg0, Pixmap arg1)
        {
            if (arg0 == "raw" && arg1 != null)
            {
                _nativePixmap = arg1.ToFzPixmap();
                int n = N;
                byte[] samples = SAMPLES;
                ColorSpace colorSpace = ColorSpace;
                IRect irect = IRect;
                int stride = Stride;
                int x = X;
                int y = Y;
            }
            else
                throw new Exception("arg0 must be `raw` or arg1 must be not null.");
        }

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
        /// Extract text from this Pixmap using OCR (Optical Character Recognition) with optional image preprocessing filters.
        /// </summary>
        /// <param name="imageFilters">Optional image preprocessing filters to apply before OCR. If null, no filters are applied.</param>
        /// <param name="language">OCR language code (e.g., "eng" for English). Default is "eng".</param>
        /// <param name="tessdata">Path to Tesseract tessdata directory. If null, uses TESSDATA_PREFIX environment variable.</param>
        /// <param name="compress">Whether to compress the intermediate OCR PDF. Default is true.</param>
        /// <param name="flags">Text extraction flags. Default is 0 (no flags).</param>
        /// <returns>The extracted text as a string.</returns>
        /// <exception cref="Exception">Thrown if OCR support is not available or if OCR processing fails.</exception>
        /// <example>
        /// <code>
        /// // Extract text with image filters
        /// var pipeline = new ImageFilterPipeline();
        /// pipeline.AddGrayscale();
        /// pipeline.AddDeskew();
        /// string text = pixmap.GetTextFromOcr(pipeline, language: "eng");
        /// 
        /// // Extract text without filters
        /// string text = pixmap.GetTextFromOcr();
        /// </code>
        /// </example>
        public string GetTextFromOcr(
            ImageFilterPipeline imageFilters = null,
            string language = "eng",
            string tessdata = null,
            bool compress = true,
            int flags = 0
        )
        {
            if (string.IsNullOrEmpty(Utils.TESSDATA_PREFIX) && string.IsNullOrEmpty(tessdata))
                throw new Exception("No OCR support: TESSDATA_PREFIX not set");

            // Create a working copy to avoid modifying/disposing the original pixmap
            Pixmap processedPixmap = new Pixmap(this.ToBytes("png", 95));
            bool disposeProcessedPixmap = true;

            try
            {
                // Apply image filters if provided
                if (imageFilters != null)
                {
                    var filtered = ApplyImageFilters(processedPixmap, imageFilters);
                    // ApplyImageFilters already disposes the input, so processedPixmap is already disposed
                    processedPixmap = filtered;
                }

                // Ensure pixmap is in RGB format for OCR
                if (processedPixmap.N - processedPixmap.Alpha != 3)
                {
                    var tempPixmap = processedPixmap;
                    processedPixmap = new Pixmap(new ColorSpace(Utils.CS_RGB), processedPixmap);
                    tempPixmap.Dispose();
                }

                // Remove alpha channel if present
                if (processedPixmap.Alpha != 0)
                {
                    var tempPixmap = processedPixmap;
                    processedPixmap = new Pixmap(processedPixmap, 0);
                    tempPixmap.Dispose();
                }

                // Perform OCR
                byte[] ocrPdfBytes = processedPixmap.PdfOCR2Bytes(compress, language, tessdata);
                if (ocrPdfBytes == null || ocrPdfBytes.Length == 0)
                    throw new Exception("Failed to generate OCR PDF");

                // Create document from OCR PDF bytes
                Document ocrDoc = null;
                Page ocrPage = null;
                TextPage textPage = null;

                try
                {
                    ocrDoc = new Document("pdf", ocrPdfBytes);
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
                // Dispose processed pixmap if we created it
                if (disposeProcessedPixmap && processedPixmap != null)
                {
                    processedPixmap.Dispose();
                }
            }
        }

        public Pixmap(PdfDocument doc, int xref)
        {
            int xrefLen = doc.pdf_xref_len();
            if (!Utils.INRANGE(xref, 1, xrefLen - 1))
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);
            PdfObj refObj = doc.pdf_new_indirect(xref, 0);
            PdfObj type = refObj.pdf_dict_get(new PdfObj("Subtype"));

            if ((type.pdf_name_eq(new PdfObj("Image")) == 0 && type.pdf_name_eq(new PdfObj("Alpha")) == 0)
                && type.pdf_name_eq(new PdfObj("Luminosity")) == 0)
            {
                throw new Exception(Utils.ErrorMessages["MSG_IS_NO_IMAGE"]);
            }
            FzImage img = doc.pdf_load_image(refObj);

            FzPixmap pix = img.fz_get_pixmap_from_image(
                new FzIrect(Utils.FZ_MIN_INF_RECT, Utils.FZ_MIN_INF_RECT, Utils.FZ_MAX_INF_RECT, Utils.FZ_MAX_INF_RECT),
                new FzMatrix(img.w(), 0, 0, img.h(), 0, 0),
                new SWIGTYPE_p_int(IntPtr.Zero, false),
                new SWIGTYPE_p_int(IntPtr.Zero, false)
                );
            _nativePixmap = pix;
        }

        public Pixmap(Pixmap pix, int alpha)
        {
            FzPixmap srcPix = pix.ToFzPixmap();
            if (!Utils.INRANGE(alpha, 0, 1))
                throw new Exception("bad alpha value");
            FzColorspace cs = mupdf.mupdf.fz_pixmap_colorspace(srcPix);
            if (cs.m_internal == null && alpha == 0)
                throw new Exception("cannot drop alpha for 'Null' colorspace");
            FzSeparations seps = new FzSeparations();
            int n = srcPix.fz_pixmap_colorants();
            int w = srcPix.fz_pixmap_width();
            int h = srcPix.fz_pixmap_height();
            FzPixmap pm = mupdf.mupdf.fz_new_pixmap(cs, w, h, seps, alpha);
            pm.m_internal.x = srcPix.m_internal.x;
            pm.m_internal.y = srcPix.m_internal.y;
            pm.m_internal.xres = srcPix.m_internal.xres;
            pm.m_internal.yres = srcPix.m_internal.yres;
            
        }

        public FzPixmap ToFzPixmap() { return _nativePixmap; }

        /// <summary>
        /// Convert to binary image stream of desired type.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="jpgQuality"></param>
        /// <returns></returns>
        internal byte[] ToBytes(int format, int jpgQuality)
        {
            FzPixmap pixmap = _nativePixmap;
            int size = pixmap.fz_pixmap_stride() * pixmap.h();
            FzBuffer res = new FzBuffer((uint)size);
            FzOutput output = new FzOutput(res);

            if (format == 1) mupdf.mupdf.fz_write_pixmap_as_png(output, pixmap);
            else if (format == 2) mupdf.mupdf.fz_write_pixmap_as_pnm(output, pixmap);
            else if (format == 3) mupdf.mupdf.fz_write_pixmap_as_pam(output, pixmap);
            else if (format == 5) mupdf.mupdf.fz_write_pixmap_as_psd(output, pixmap);
            else if (format == 6) mupdf.mupdf.fz_write_pixmap_as_ps(output, pixmap);
            else if (format == 7) output.fz_write_pixmap_as_jpeg(pixmap, jpgQuality, 0); // v1.24 later
            else mupdf.mupdf.fz_write_pixmap_as_png(output, pixmap);

            byte[] barray = Utils.BinFromBuffer(res);
            output.fz_close_output();
            output.Dispose();
            res.Dispose();
            return barray;
        }

        private void WriteImage(string filename, int format, int jpgQuality)
        {
            FzPixmap pixmap = _nativePixmap;

            if (format == 1) mupdf.mupdf.fz_save_pixmap_as_png(pixmap, filename);
            else if (format == 2) mupdf.mupdf.fz_save_pixmap_as_pnm(pixmap, filename);
            else if (format == 3) mupdf.mupdf.fz_save_pixmap_as_pam(pixmap, filename);
            else if (format == 5) mupdf.mupdf.fz_save_pixmap_as_psd(pixmap, filename);
            else if (format == 6) mupdf.mupdf.fz_save_pixmap_as_ps(pixmap, filename, 0);
            else if (format == 7) mupdf.mupdf.fz_save_pixmap_as_jpeg(pixmap, filename, jpgQuality);
            else mupdf.mupdf.fz_save_pixmap_as_png(pixmap, filename);
        }

        public static int ClearPixmap_RectWithValue(Pixmap pixmap, int v = 0, FzIrect bbox = null)
        {
            FzPixmap dest = pixmap.ToFzPixmap();
            FzIrect b = bbox.fz_intersect_irect(dest.fz_pixmap_bbox());
            float w = b.x1 - b.x0;
            float y = b.y1 - b.y0;
            if (w <= 0 || y <= 0)
                return 0;
            int destSpan = dest.fz_pixmap_stride();
            float destP = destSpan * (b.y0 - dest.y()) + dest.n() * (b.x0 - dest.x());
            int v_ = 0;

            if (dest.colorspace().fz_colorspace_n() == 4)
            {
                v_ = 255 - v;
                while (true)
                {
                    int s = (int)destP;
                    for (int i = 0; i < w; i++)
                    {
                        dest.fz_samples_set(s, 0);
                        s += 1;
                        dest.fz_samples_set(s, 0);
                        s += 1;
                        dest.fz_samples_set(s, 0);
                        s += 1;
                        dest.fz_samples_set(s, v_);
                        s += 1;
                        if (dest.alpha() != 0)
                        {
                            dest.fz_samples_set(s, 255);
                            s += 1;
                        }
                    }
                    destP += destSpan;
                    if (y == 0)
                        break;
                    y -= 1;
                }
                return 1;
            }
            while (true)
            {
                int s = (int)destP;
                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < dest.n() - 1; j++)
                    {
                        dest.fz_samples_set(s, v);
                        s += 1;
                    }
                    if (dest.alpha() != 0)
                    {
                        dest.fz_samples_set(s, 255);
                        s += 1;
                    }
                    else
                    {
                        dest.fz_samples_set(s, v);
                        s += 1;
                    }
                }
                destP += destSpan;
                if (y == 0)
                    break;
                y -= 1;
            }
            
            return 1;
        }

        /// <summary>
        /// Fill all color components with same value.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="bbox"></param>
        public void ClearWith(int v = 0, IRect bbox = null)
        {
            if (v == 0)
                _nativePixmap.fz_clear_pixmap();
            else if (bbox is null)
                _nativePixmap.fz_clear_pixmap_with_value(v);
            else Pixmap.ClearPixmap_RectWithValue(this, v, bbox.ToFzIrect());
        }

        /// <summary>
        /// Return count of each color.
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="clip"></param>
        /// <returns>Dcitionary<List<byte>, int> or int</returns>
        /// <exception cref="Exception"></exception>
        public dynamic ColorCount(bool colors = false, dynamic clip = null)
        {
            if (_disposed || _nativePixmap == null)
                throw new ObjectDisposedException("Pixmap has been disposed.");
            
            try
            {
                FzPixmap pm = _nativePixmap;
                IRect irect = IRect;
                int stride = Stride;
                int n = N;
                int x = X;
                int y = Y;
                byte[] samples = SAMPLES;
                
                Dictionary<string, int> rc = Utils.ColorCount(pm, irect, stride, n, x, y, samples, clip);
                if (rc == null)
                    throw new Exception(Utils.ErrorMessages["MSG_COLOR_COUNT_FAILED"]);
                if (!colors)
                    return rc.Count;
                
                return rc;
            }
            catch (AccessViolationException)
            {
                _disposed = true;
                throw new ObjectDisposedException("Pixmap has been disposed.");
            }
        }

        /// <summary>
        /// Return most frequent color and its usage ratio.
        /// </summary>
        /// <param name="clip">Return most frequent color and its usage ratio.</param>
        /// <returns></returns>
        public (float, byte[]) ColorTopUsage(dynamic clip = null)
        {
            int allPixels = 0;
            int count = 0;
            string maxPixel = null;
            if (clip != null)
                clip = this.IRect;

            Dictionary<string, int> colorCount = (Dictionary<string, int>)ColorCount(true, clip);
            foreach (string pixel in colorCount.Keys)
            {
                int c = colorCount[pixel];
                allPixels += c;
                if (c > count)
                {
                    count = c;
                    maxPixel = pixel;
                }
            }

            if (allPixels == 0)
                return (1, Enumerable.Repeat((byte)255, N).ToArray());
            
            return (count / (float)allPixels, maxPixel.Split(',').Select(b => byte.Parse(b)).ToArray());
        }

        /// <summary>
        /// Copy bbox from another Pixmap.
        /// </summary>
        /// <param name="src">source pixmap</param>
        /// <param name="bbox">The area to be copied</param>
        /// <exception cref="Exception"></exception>
        public void Copy(Pixmap src, IRect bbox)
        {
            FzPixmap pm = _nativePixmap;
            FzPixmap srcPm = src.ToFzPixmap();
            if (srcPm.fz_pixmap_colorspace() == null)
                throw new Exception("cannot copy pixmap with NULL colorspace");

            if (pm.alpha() != srcPm.alpha())
                throw new Exception("source and target alpha must be equal");

            pm.fz_copy_pixmap_rect(srcPm, bbox.ToFzIrect(), new FzDefaultColorspaces());
        }

        /// <summary>
        /// Apply correction with some float
        /// </summary>
        /// <param name="gamma"></param>
        /// <exception cref="Exception"></exception>
        public void GammaWith(float gamma)
        {
            if (_nativePixmap.fz_pixmap_colorspace() == null)
            {
                throw new Exception("colorspace invalid for function");
            }
            _nativePixmap.fz_gamma_pixmap(gamma);
        }

        private int InvertPixmapRect(FzPixmap dest, FzIrect bb)
        {
            FzIrect b = bb.fz_intersect_irect(dest.fz_pixmap_bbox());  
            int w = b.x1 - b.x0;
            int y = b.y1 - b.y0;
            if (w <= 0 || y <= 0)
                return 0;
            int destSpan = dest.fz_pixmap_stride();
            int destp = destSpan * (b.y0 - dest.y()) + dest.n() * (b.x0 - dest.x());
            int n0 = dest.n() - dest.alpha();
            int alpha = dest.alpha();

            while (true)
            {
                int s = destp;
                for (int x = 0; x < w; x++)
                {
                    for (int i = 0; i < n0; i++)
                    {
                        int ss = dest.fz_samples_get(s);
                        ss = 255 - ss;
                        dest.fz_samples_set(s, ss);
                        s += 1;
                    }
                    if (alpha != 0)
                    {
                        int ss = dest.fz_samples_get(s);
                        ss += 1;
                        dest.fz_samples_set(s, ss);
                    }
                }
                destp += destSpan;
                y -= 1;
                if (y == 0)
                    break;
            }
            
            return 1;
        }

        /// <summary>
        /// Invert the colors inside a bbox.
        /// </summary>
        /// <param name="bbox">The area to be inverted</param>
        /// <returns></returns>
        public bool InvertIrect(IRect bbox = null)
        {
            FzPixmap pm = _nativePixmap;
            if (_nativePixmap.fz_pixmap_colorspace() == null)
            {
                Console.WriteLine("ignored for stencil pixmap");
                return false;
            }
            FzIrect r = bbox.ToFzIrect();
            if (r.fz_is_infinite_irect() != 0)
                r = pm.fz_pixmap_bbox();
            
            return Convert.ToBoolean(InvertPixmapRect(pm, r));
        }

        /// <summary>
        /// Save pixmap as an OCR-ed PDF page
        /// </summary>
        /// <param name="filename">File name </param>
        /// <param name="compress">(bool) compress, default 1 (True)</param>
        /// <param name="language">language(s) occurring on page, default "eng"</param>
        /// <param name="tessdata">folder name of Tesseract's language support. Must be given if environment variable TESSDATA_PREFIX is not set</param>
        /// <exception cref="Exception"></exception>
        public void SavePdfOCR(string filename, int compress = 1, string language = null, string tessdata = null)
        {
            if (Utils.TESSDATA_PREFIX == null && tessdata == null)
                throw new Exception("No OCR support: TESSDATA_PREFIX not set");
            FzPdfocrOptions opts = new FzPdfocrOptions();
            opts.compress = compress;
            FzPixmap pix = _nativePixmap;

            if (language != null)
            {
                opts.language_set2(language);
            }
            if (tessdata != null)
            {
                opts.datadir_set2(tessdata);
            }

            pix.fz_save_pixmap_as_pdfocr(filename, 0, opts);
        }

        /// <summary>
        /// Save pixmap as an OCR-ed PDF page
        /// </summary>
        /// <param name="filename">Buffer to store page data</param>
        /// <param name="compress">(bool) compress, default 1 (True)</param>
        /// <param name="language">language(s) occurring on page, default "eng"</param>
        /// <param name="tessdata">folder name of Tesseract's language support. Must be given if environment variable TESSDATA_PREFIX is not set</param>
        /// <exception cref="Exception"></exception>
        public void SavePdfOCR(MemoryStream filename, int compress = 1, string language = null, string tessdata = null)
        {
            if (Utils.TESSDATA_PREFIX == null && tessdata == null)
                throw new Exception("No OCR support: TESSDATA_PREFIX not set");
            FzPdfocrOptions opts = new FzPdfocrOptions();
            opts.compress = compress;
            FzPixmap pix = _nativePixmap;

            if (language != null)
            {
                opts.language_set2(language);
            }
            if (tessdata != null)
            {
                opts.datadir_set2(tessdata);
            }

            FilePtrOutput output = null;
            try
            {
                output = new FilePtrOutput(filename);
                output.fz_write_pixmap_as_pdfocr(pix, opts);
                output.fz_close_output();
            }
            finally
            {
                output?.Dispose();
            }
        }

        /// <summary>
        /// Return the value of the pixel at location (x, y) (column, line)
        /// </summary>
        /// <param name="x">the column number of the pixel. Must be in range(pix.width)</param>
        /// <param name="y">the line number of the pixel, Must be in range(pix.height)</param>
        /// <exception cref="Exception"></exception>
        public byte[] GetPixel(int x, int y)
        {
            if (false || x < 0 || x >= _nativePixmap.m_internal.w
                || y < 0 || y >= _nativePixmap.m_internal.h
                )
            {
                throw new Exception(Utils.ErrorMessages["MSG_PIXEL_OUTSIDE"]);
            }

            int n = _nativePixmap.m_internal.n;
            int stride = _nativePixmap.fz_pixmap_stride();
            int i = stride * y + n * x;

            byte[] pixel = SAMPLES.Skip(i).Take(n).ToArray();
            
            return pixel;
        }

        /// <summary>
        /// Output as image in format determined by filename extension
        /// </summary>
        /// <param name="filename">The file to save to. May be provided as a string</param>
        /// <param name="output">only use to overrule filename extension. Default is PNG. Others are JPEG, JPG, PNM, PGM, PPM, PBM, PAM, PSD, PS.</param>
        /// <param name="jpgQuality">The desired image quality, default 95. Only applies to JPEG images, else ignored</param>
        /// <exception cref="Exception"></exception>
        public void Save(string filename, string output = null, int jpgQuality = 95)
        {
            Dictionary<string, int> validFormats = new Dictionary<string, int>()
            {
                {"png", 1 },
                {"pnm", 2 },
                {"pgm", 2 },
                {"ppm", 2 },
                {"pbm", 2 },
                {"pam", 3 },
                {"psd", 5 },
                {"ps", 6 },
                {"jpg", 7 },
                {"jpeg", 7 }
            };

            string filename_ = filename;
            string output_ = output;

            if (string.IsNullOrEmpty(filename))
                throw new Exception("filename must be set");

            if (string.IsNullOrEmpty(output_))
                output_ = Path.GetExtension(filename_).Substring(1);
            
            int idx = validFormats[output_.ToLower()];
            if (idx is -1)
                throw new Exception($"Image format {output_} not in {validFormats.Keys}");
            if (Alpha != 0 && (new List<int>() { 2, 6, 7 }).Contains(idx))
            {
                throw new Exception(string.Format("'{0}' cannot have alpha", output_));
            }
            if (ColorSpace != null && ColorSpace.N > 3 && (new List<int>() { 1, 2, 4 }).Contains(idx))
            {
                throw new Exception(string.Format("unsupported colorspace for '{0}'", output_));
            }
            if (idx == 7)
                SetDpi(Xres, Yres);
            WriteImage(filename_, idx, jpgQuality);
        }

        private int fz_mul255(int a, int b)
        {
            int x = a * b + 128;
            x += x / 256;
            
            return x / 256;
        }

        /// <summary>
        /// Set alpha channel to values contained in a byte array
        /// </summary>
        /// <param name="alphaValues">with length (width * height) or 'None'</param>
        /// <param name="premultiply">premultiply colors with alpha values</param>
        /// <param name="opaque">this color receives opacity 0</param>
        /// <param name="matte">preblending background color</param>
        /// <exception cref="Exception"></exception>
        public void SetAlpha(dynamic alphaValues = null, int premultiply = 1, dynamic opaque = null, dynamic matte = null)
        {
            FzPixmap pixmap = _nativePixmap;
            int alpha = 0;
            int m = 0;
            if (pixmap.alpha() == 0)
                throw new Exception(Utils.ErrorMessages["MSG_PIX_NOALPHA"]);

            int n = pixmap.fz_pixmap_colorants();
            int w = pixmap.fz_pixmap_width();
            int h = pixmap.fz_pixmap_height();
            int balen = w * h * (n + 1);
            int[] colors = new int[4] { 0, 0, 0, 0 };
            int[] bgColor = new int[4] { 0, 0, 0, 0 };
            int zeroOut = 0;
            int bground = 0;
            if (opaque != null && (opaque is List<int> || opaque is Tuple) && opaque.Count == n)
            {
                foreach (int i in opaque)
                {
                    colors[i] = opaque[i];
                }
                zeroOut = 1;
            }
            if (matte != null && (matte is Tuple || matte is List<int>) && matte.Count == n)
            {
                foreach (int i in matte)
                    bgColor[i] = matte[i];
                bground = 1;
            }
            List<byte> data = null;
            int dataLen = 0;
            if (alphaValues != null)
            {
                if (alphaValues is List<byte> || alphaValues is byte[])
                {
                    data = new List<byte>(alphaValues);
                    dataLen = alphaValues.Count;
                }
                else
                    throw new Exception($"unexpected type for alphavalues: {alphaValues.GetType()}");
                if (dataLen < w * h)
                    throw new Exception("bad alpha values");
            }

            if (true)
            {
                IntPtr dataPtr = Marshal.AllocHGlobal(dataLen * sizeof(byte));
                Marshal.Copy(data.ToArray(), 0, dataPtr, dataLen);
                SWIGTYPE_p_unsigned_char swigData = new SWIGTYPE_p_unsigned_char(dataPtr, false);

                IEnumerable<int> iColors = colors;
                IEnumerable<int> iBgColor = bgColor;

                mupdf.mupdf.Pixmap_set_alpha_helper(
                    balen,
                    n,
                    dataLen,
                    zeroOut,
                    swigData,
                    pixmap.m_internal,
                    premultiply,
                    bground,
                    new vectori(iColors),
                    new vectori(iBgColor)
                    );
            }
            else
            {
                int i = 0; int j = 0; int k = 0;
                int dataFix = 255;
                while (i < balen)
                {
                    alpha = data[k];
                    if (zeroOut != 0)
                    {
                        for (j = i; j < i + n; j++)
                        {
                            if (pixmap.fz_samples_get(j) != colors[j - 1])
                            {
                                dataFix = 255;
                                break;
                            }
                            else
                                dataFix = 0;
                        }
                    }
                    if (dataLen != 0)
                    {
                        if (dataFix == 0)
                            pixmap.fz_samples_set(i + n, 0);
                        else
                            pixmap.fz_samples_set(i + n, alpha);
                        if (premultiply != 0 && bground == 0)
                        {
                            for (j = i; j < i + n; j++)
                                pixmap.fz_samples_set(j, fz_mul255(pixmap.fz_samples_get(j), alpha));
                        }
                        else if (bground != 0)
                            for (j = i; j < i + n; j++)
                            {
                                m = bgColor[j - 1];
                                pixmap.fz_samples_set(j, fz_mul255(pixmap.fz_samples_get(j) - m, alpha));
                            }
                    }
                    else
                        pixmap.fz_samples_set(i + n, dataFix);
                    i += n + 1;
                    k += 1;
                }
            }
        }

        /// <summary>
        /// Convert to binary image stream of desired type
        /// </summary>
        /// <param name="output">The desired image format. The default is "png"</param>
        /// <param name="jpgQuality">The desired image quality, default 95. Only applies to JPEG images, else ignored. This parameter trades quality against file size. A value of 98 is close to lossless. Higher values should not lead to better quality</param>
        /// <returns>Returns bytes</returns>
        /// <exception cref="Exception"></exception>
        public byte[] ToBytes(string output = "png", int jpgQuality = 95)
        {
            Dictionary<string, int> validFormats = new Dictionary<string, int>()
            {
                {"png", 1 },
                {"pnm", 2 },
                {"pgm", 2 },
                {"ppm", 2 },
                {"pbm", 2 },
                {"pam", 3 },
                {"psd", 5 },
                {"ps", 6 },
                {"jpg", 7 },
                {"jpeg", 7 }
            };

            //int idx = validFormats.GetValueOrDefault(output.ToLower(), 0);
            int idx;
            if (!validFormats.TryGetValue(output.ToLower(), out idx))
            {
                idx = 0;  // Default value if key does not exist
            }
            if (idx == 0)
            {
                throw new Exception($"Image format {output} not in {string.Join(", ", validFormats.Keys)}");
            }
            if (Alpha != 0 && (new List<int>() { 2, 6, 7 }).Contains(idx))
                throw new Exception($"'{output}' cannot have alpha");
            if (ColorSpace != null && ColorSpace.N > 3 && (new List<int>() { 1, 2, 4 }).Contains(idx))
                throw new Exception($"unsupported colorspace for '{output}'");

            if (idx == 7)
                SetDpi(Xres, Yres);
            
            return ToBytes(idx, jpgQuality);
        }

        public void SetDpi(int xres, int yres)
        {
            _nativePixmap.m_internal.xres = xres;
            _nativePixmap.m_internal.yres = yres;
        }

        /// <summary>
        /// Set the x and y values of the pixmap’s top-left point
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        public void SetOrigin(int x, int y)
        {
            _nativePixmap.m_internal.x = x;
            _nativePixmap.m_internal.y = y;
        }

        /// <summary>
        /// Set color of pixel (x, y)
        /// </summary>
        /// <param name="x">the column number of the pixel</param>
        /// <param name="y">the line number of the pixel</param>
        /// <param name="color"></param>
        /// <exception cref="Exception"></exception>
        public void SetPixel(int x, int y, byte[] color)
        {
            FzPixmap pm = _nativePixmap;
            if (!Utils.INRANGE(x, 0, pm.w() - 1) || !Utils.INRANGE(y, 0, pm.h() - 1))
            {
                throw new Exception(Utils.ErrorMessages["MSG_PIXEL_OUTSIDE"]);
            }
            int n = pm.n();
            List<int> c = new List<int>();

            for (int j = 0; j < n; j++)
            {
                byte t = color[j];
                if (!Utils.INRANGE(t, 0, 255))
                    throw new Exception(Utils.ErrorMessages["MSG_BAD_COLOR_SEQ"]);
                c.Add(Convert.ToInt32(t));
            }
            int stride = pm.fz_pixmap_stride();
            int i = stride * y + n * x;
            for (int j = 0; j < n; j++)
                pm.fz_samples_set(i + j, c[j]);
        }

        private int FillPixmap_RectWithColor(FzPixmap dest, byte[] col, FzIrect b)
        {
            FzIrect b_ = b.fz_intersect_irect(dest.fz_pixmap_bbox());
            int w = b.x1 - b.x0;
            int y = b.y1 - b.y0;
            if (w <= 0 && y <= 0)
                return 0;

            int destSpan = dest.fz_pixmap_stride();
            int destP = destSpan * (b.y0 - dest.y()) + dest.n() * (b.x0 - dest.x());
            while (true)
            {
                int s = destP;
                for (int x = 0; x < w; x++)
                    for (int i = 0; i < dest.n(); i++)
                    {
                        dest.fz_samples_set(s, col[i]);
                        s += 1;
                    }
                destP += destSpan;
                y -= 1;

                if (y == 0) break;
            }
            
            return 1;
        }

        /// <summary>
        /// Set color of all pixels in bbox.
        /// </summary>
        /// <param name="bbox">the rectangle to be filled with the value</param>
        /// <param name="color">the desired value, given as a sequence of integers in range(256)</param>
        /// <returns>False if the rectangle was invalid or had an empty intersection with Pixmap.irect, else True</returns>
        /// <exception cref="Exception"></exception>
        public bool SetRect(IRect bbox, byte[] color)
        {
            FzPixmap pm = _nativePixmap;
            int n = pm.n();
            List<byte> c = new List<byte>();
            int i = 0;
            for (int j = 0; j < n; j++)
            {
                i = color[j];
                if (!Utils.INRANGE(i, 0, 255))
                    throw new Exception(Utils.ErrorMessages["MSG_BAD_COLOR_SEQ"]);
                c.Add((byte)i);
            }
            i = FillPixmap_RectWithColor(pm, c.ToArray(), bbox.ToFzIrect());

            return Convert.ToBoolean(i);
        }

        /// <summary>
        /// Divide width and height by 2**factor
        /// </summary>
        /// <param name="factor">determines the new pixmap (samples) size. For example, a value of 2 divides width and height by 4 and thus results in a size of one 16th of the original. Values less than 1 are ignored with a warning.</param>
        public void Shrink(int factor)
        {
            if (factor < 1)
            {
                Console.WriteLine("ignoring shrink factor < 1");
                return;
            }
            _nativePixmap.fz_subsample_pixmap(factor);
        }

        /// <summary>
        /// Tint colors with modifiers for black and white.
        /// </summary>
        /// <param name="black">replace black with this value. Specifying 0x000000 makes no changes</param>
        /// <param name="white">replace white with this value. Specifying 0xFFFFFF makes no changes</param>
        public void TintWith(int black, int white)
        {
            if (ColorSpace == null || ColorSpace.N > 3)
            {
                Console.WriteLine("warning: colorspace invalid for function");
                return;
            }

            _nativePixmap.fz_tint_pixmap(black, white);
        }

        /// <summary>
        /// Return a new pixmap by “warping” the quad such that the quad corners become the new pixmap’s corners. The target pixmap’s IRect will be (0, 0, width, height)
        /// </summary>
        /// <param name="quad">a convex quad with coordinates inside Pixmap.irect (including the border points)</param>
        /// <param name="width">desired resulting width</param>
        /// <param name="height">desired resulting height</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Pixmap Warp(Quad quad, float width, float height)
        {
            if (!quad.IsConvex)
                throw new Exception("quad must be convex");

            FzPoint[] points = new FzPoint[4] {
                quad.UpperLeft.ToFzPoint(),
                quad.UpperRight.ToFzPoint(),
                quad.LowerRight.ToFzPoint(),
                quad.UpperLeft.ToFzPoint()
            };

            FzPixmap fpxmp = mupdf.mupdf.fz_warp_pixmap(_nativePixmap, quad.ToFzQuad(), (int)width, (int)height);
            
            return new Pixmap(fpxmp);
        }

        public byte[] PdfOCR2Bytes(bool compress = true, string language = "eng", string tessdata = null)
        {
            if (Utils.TESSDATA_PREFIX == null && tessdata == null)
                throw new Exception("No OCR support: TESSDATA_PREFIX not set");
            MemoryStream byteStream = new MemoryStream();
            SavePdfOCR(byteStream, compress ? 1 : 0, language, tessdata);

            return byteStream.ToArray();
        }

        ~Pixmap()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (_nativePixmap != null)
            {
                _nativePixmap.Dispose();
                _nativePixmap = null;
            }

            _disposed = true;
        }
    }

    public class FilePtrOutput : FzOutput2
    {
        public MemoryStream data { get; set; }

        public FilePtrOutput(MemoryStream src) : base()
        {
            this.data = src;
            this.use_virtual_write();
            this.use_virtual_seek();
            this.use_virtual_tell();
            this.use_virtual_truncate();
        }

        public override void seek(fz_context arg_0, long arg_2, int arg_3)
        {
            data.Seek(arg_2, (SeekOrigin)arg_3);
        }

        public override long tell(fz_context arg_0)
        {
            return data.Position;
        }

        public override void truncate(fz_context arg_0)
        {
            data.SetLength(0);
        }

        public override void write(fz_context arg_0, SWIGTYPE_p_void arg_2, ulong arg_3)
        {
            byte[] data = new byte[(int)arg_3];
            Marshal.Copy(SWIGTYPE_p_void.getCPtr(arg_2).Handle, data, 0, data.Length);

            this.data.Write(data, 0, data.Length);
        }
    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  