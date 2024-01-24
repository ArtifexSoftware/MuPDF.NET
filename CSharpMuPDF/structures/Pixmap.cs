using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using mupdf;
using System.Net;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Policy;

namespace MuPDF.NET
{
    public class Pixmap
    {
        private FzPixmap _nativePixmap;

        private string TESSDATA_PREFIX = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");

        public Pixmap(ColorSpace cs, IRect irect, int alpha)
        {
            _nativePixmap = mupdf.mupdf.fz_new_pixmap_with_bbox(cs.ToFzColorspace(), irect.ToFzIrect(), new FzSeparations(0), alpha);
        }

        public IRect IRECT
        {
            get
            {
                FzIrect val = _nativePixmap.fz_pixmap_bbox();
                return new IRect(val);
            }
        }

        public int ALPHA
        {
            get
            {
                return _nativePixmap.fz_pixmap_alpha();
            }
        }

        public ColorSpace COLORSPACE
        {
            get
            {
                return new ColorSpace(_nativePixmap.fz_pixmap_colorspace());
            }
        }

        public byte[] DIGEST
        {
            get
            {
                vectoruc res = _nativePixmap.fz_md5_pixmap2();
                byte[] ret = new byte[res.Count];
                res.CopyTo(ret, 0);
                return ret;
            }
        }

        public int H
        {
            get
            {
                return _nativePixmap.fz_pixmap_height();
            }
        }

        public bool IsMonoChrome
        {
            get
            {
                return Convert.ToBoolean(_nativePixmap.fz_is_pixmap_monochrome());
            }
        }

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
                    if (!sample.Equals(sample0))
                        return false;
                }
                return true;
            }
        }

        public int N
        {
            get
            {
                return _nativePixmap.fz_pixmap_components();
            }
        }

        /*public void SAMPLES_MV
        {
            get
            {
                return _nativePixmap.fz_pixmap_samples_memoryview
            }
        }*/

        /*public byte[] SAMPLES
        {
            get
            {

            }
        }*/

        public long SAMPLES_PTR
        {
            get
            {
                return _nativePixmap.fz_pixmap_samples_int();
            }
        }

        public int SIZE
        {
            get
            {
                return _nativePixmap.n() * _nativePixmap.w() * _nativePixmap.h();
            }
        }

        public int STRIDE
        {
            get
            {
                return _nativePixmap.fz_pixmap_stride();
            }
        }

        public int W
        {
            get
            {
                return _nativePixmap.fz_pixmap_width();
            }
        }

        public int X
        {
            get
            {
                return _nativePixmap.fz_pixmap_x();
            }
        }

        public int XRES
        {
            get
            {
                return _nativePixmap.xres();
            }
        }

        public int Y
        {
            get
            {
                return _nativePixmap.fz_pixmap_y();
            }
        }

        public int YRES
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

        public Pixmap(Pixmap src, float width, float height, dynamic clip)
        {
            FzPixmap srcPix = src.ToFzPixmap();
            FzIrect bBox = new FzIrect(clip);
            FzPixmap pm = null;

            /*if (bBox.fz_is_infinite_irect() == 0)
                _nativePixmap = srcPix.fz_scale_pixmap_cached(src_pix.x, src_pix.y, w, h, bbox);
            else
                _nativePixmap = srcPix.fz_scale_pixmap(src_pix.x, src_pix.y, w, h, None);*///issue
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

        public Pixmap(ColorSpace cs, int w, int h, dynamic samples, int alpha)
        {
            int n = cs.N;
            int stride = (n + alpha) * w;
            FzSeparations seps = new FzSeparations();
            FzPixmap pixmap = mupdf.mupdf.fz_new_pixmap(cs.ToFzColorspace(), w, h, seps, alpha);
            int size = 0;

            if (samples is List<byte> || samples is byte[])
            {
                FzBuffer samples2 = Utils.BufferFromBytes(samples is List<byte> ? samples.ToArray() : samples);
                size = samples is List<byte> ? samples.Count : samples.Length;
            }
            else
            {

            }

            //issue
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

        public FzPixmap ToFzPixmap() { return  _nativePixmap; }

        public byte[] ToBytes_(int format, int jpgQuality)
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
            //else if (format == 7) mupdf.mupdf.fz_write_pixmap_as_jpeg(output, pixmap, jpgQuality); //issue
            else mupdf.mupdf.fz_write_pixmap_as_png(output, pixmap);

            byte[] barray = Utils.BinFromBuffer(res);
            return barray;
        }

        private void writeImg(string filename, int format, int jpgQuality)
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
            int destSpan= dest.fz_pixmap_stride();
            float destP = destSpan * (b.y0 - dest.y()) + dest.n() * (b.x0 - dest.x());
            int v_ = 0;

            if(dest.colorspace().fz_colorspace_n() == 4)
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
                for (int i = 0; i < w; i ++)
                {
                    for (int j = 0; j < dest.n() -1; j ++)
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

        public void ClearWith(int v = 0, IRect bbox = null)
        {
            if (v == 0)
                _nativePixmap.fz_clear_pixmap();
            else if (bbox is null)
                _nativePixmap.fz_clear_pixmap_with_value(v);
            else Pixmap.ClearPixmap_RectWithValue(this, v, bbox.ToFzIrect());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="clip"></param>
        /// <returns>Dcitionary<List<byte>, int> or int</returns>
        /// <exception cref="Exception"></exception>
        public dynamic ColorCount(bool colors = false, dynamic clip = null)
        {
            FzPixmap pm = _nativePixmap;
            Dictionary<List<byte>, int> rc = Utils.ColorCount(pm, clip);
            if (rc == null)
                throw new Exception(Utils.ErrorMessages["MSG_COLOR_COUNT_FAILED"]);
            if (!colors)
                return rc.Count;
            return rc;
        }

        public (int, List<byte>) ColorTopUsage(dynamic clip = null)
        {
            int allPixels = 0;
            int count = 0;
            List<byte> maxPixel = null;
            if (clip != null)
                clip = IRECT;

            Dictionary < List<byte>, int> colorCount = (Dictionary < List<byte>, int>)ColorCount(true, clip);
            
            for (int i = 0; i < colorCount.Count; i++)
            {
                int c = colorCount.Values.ElementAt(i);
                allPixels += c;
                if (c > count)
                {
                    count = c;
                    maxPixel = colorCount.Keys.ElementAt(i);
                }
            }
            if (allPixels == 0)
                return (1, Enumerable.Repeat((byte)255, N).ToList());
            return (count / allPixels, maxPixel);
        }

        public void Copy(Pixmap src, IRect bbox)
        {
            FzPixmap pm = _nativePixmap;
            FzPixmap srcPm = src.ToFzPixmap();
            if (srcPm.fz_pixmap_colorspace() == null)
                throw new Exception("cannot copy pixmap with NULL colorspace");

            if (pm.alpha() != srcPm.alpha())
                throw new Exception("source and target alpha must be equal");

            //pm.fz_copy
        }

        public void GammaWith(float gamma)
        {
            if (_nativePixmap.fz_pixmap_colorspace() == null)
            {
                throw new Exception("colorspace invalid for function");
            }
            _nativePixmap.fz_gamma_pixmap(gamma);
        }
        
        private int InvertPixmapRect(FzPixmap dest, FzIrect b)
        {
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

        public void SavePdfOCR(dynamic filename, int compress = 1, string language = null, string tessdata = null)
        {
            if (TESSDATA_PREFIX == null && tessdata == null)
                throw new Exception("No OCR support: TESSDATA_PREFIX not set");
            FzPdfocrOptions opts = new FzPdfocrOptions();
            opts.compress = compress;
            if (language != null)
            {
                opts.language_set2(language);
            }
            if (tessdata != null)
            {
                opts.datadir_set2(tessdata);
            }
            FzPixmap pix = _nativePixmap;
            if (filename is string)
                pix.fz_save_pixmap_as_pdfocr(filename, 0, opts);
            else
            {
                FilePtrOutput output = new FilePtrOutput(filename);
                output.fz_write_pixmap_as_pdfocr(pix, opts);
            }    
        }

        public byte[] Pdfocr2Bytes(int compress = 1, string language = "eng", string tessdata = null)
        {
            if (TESSDATA_PREFIX == null && tessdata == null)
                throw new Exception("No OCR support: TESSDATA_PREFIX not set");
            MemoryStream fstream = new MemoryStream();
            SavePdfOCR(fstream, compress, language, tessdata);
            return fstream.GetBuffer();
        }

        public void SaveDrawing()
        {
            ColorSpace cspace = COLORSPACE;
            string mode = null;
            if (cspace is null)
                mode = "L";
            else if (cspace.N == 1)
                mode = (this.ALPHA == 0) ? "L" : "LA";
            else if (cspace.N == 3)
                mode = (ALPHA == 0) ? "RGB" : "RGBA";
            else
                mode = "CMYK";
            
            
        }

        public void Drawing2Bytes()
        {

        }

        public void GetPixel(int x, int y)
        {
            if (false || x < 0 || x >= _nativePixmap.m_internal.w
                || y < 0 || y >= _nativePixmap.m_internal.h
                )
            {
                throw new Exception(Utils.ErrorMessages["MSG_PIXEL_OUTSIDE"]);
            }

            int n = _nativePixmap.m_internal.w;
            int stride = _nativePixmap.fz_pixmap_stride();
            int i = stride * y + n * x;
            
            //int ret = new Tuple()
        }

        public void Save(dynamic filename, string output, int jpgQuality = 95)
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

            string filename_ = "";
            string output_ = "";

            if (filename is string)
                filename_ = filename;
            else if (filename is Dictionary<string, string> && filename.ContainsKey("absolute"))
                filename_ = filename["absolute"];
            else if (output is null)
                output_ = Path.GetExtension(filename).Substring(1);

            int idx = validFormats[output_.ToLower()];
            if (idx is -1)
                throw new Exception($"Image format {output_} not in {validFormats.Keys}");
            if (ALPHA != 0 && (new List<int>() { 2, 6, 7}).Contains(idx))
            {
                throw new Exception(string.Format("'{0}' cannot have alpha", output_));
            }
            if (COLORSPACE != null && COLORSPACE.N > 3 && (new List<int>() { 1, 2, 4 }).Contains(idx))
            {
                throw new Exception(string.Format("unsupported colorspace for '{0}'", output_));
            }
            if (idx == 7)
                SetDpi(XRES, YRES);
            writeImg(filename_, idx, jpgQuality);
        }

        private int fz_mul255(int a, int b)
        {
            int x = a * b + 128;
            x += x / 256;
            return x / 256;
        }

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
                        for (j = i; j < i + n; j ++)
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

            int idx = 0;
            if (validFormats.TryGetValue(output, out idx))
            {
                throw new Exception($"Image format {output} not in {string.Join(", ", validFormats.Keys)}");
            }
            if (ALPHA != 0 && (new List<int>() { 2, 6, 7 }).Contains(idx))
                throw new Exception($"'{output}' cannot have alpha");
            if (COLORSPACE != null && COLORSPACE.N > 3 && (new List<int>() { 1, 2, 4 }).Contains(idx))
                throw new Exception($"unsupported colorspace for '{output}'");

            if (idx == 7)
                SetDpi(XRES, YRES);
            return ToBytes_(idx, jpgQuality);
        }

        public void SetDpi(int xres, int yres)
        {
            _nativePixmap.m_internal.xres = xres;
            _nativePixmap.m_internal.yres = yres;
        }

        public void SetOrigin(int x, int y)
        {
            _nativePixmap.m_internal.x = x;
            _nativePixmap.m_internal.y = y;
        }

        public void SetPixel(int x, int y, byte[] color)
        {
            FzPixmap pm = _nativePixmap;
            if (!Utils.INRANGE(x, 0, pm.w() - 1) || !Utils.INRANGE(y, 0, pm.h() - 1))
            {
                throw new Exception(Utils.ErrorMessages["MSG_PIXEL_OUTSIDE"]);
            }
            int n = pm.n();
            List<int> c = new List<int>();

            for (int j = 0; j < n; j ++)
            {
                byte t = color[j];
                if (Utils.INRANGE(t, 0, 255))
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
                for (int x = 0; x < w ; x++)
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

        public bool SetRect(IRect bbox, byte[] color)
        {
            FzPixmap pm = _nativePixmap;
            int n = pm.n();
            List<byte> c = new List<byte>();
            int i = 0;
            for (int j = 0; j < n; j ++)
            {
                i = color[j];
                if (!Utils.INRANGE(i, 0, 255))
                    throw new Exception(Utils.ErrorMessages["MSG_BAD_COLOR_SEQ"]);
                c.Add((byte)i);
            }
            i = FillPixmap_RectWithColor(pm, c.ToArray(), bbox.ToFzIrect());

            return Convert.ToBoolean(i);
        }

        public void Shrink(int factor)
        {
            if (factor < 1)
            {
                Console.WriteLine("ignoring shrink factor < 1");
                return;
            }
            //mupdf.fz_subsample_pixmap(self.this, factor)
        }

        public void TintWith(int black, int white)
        {
            if (COLORSPACE == null || COLORSPACE.N > 3)
            {
                Console.WriteLine("warning: colorspace invalid for function");
                return;
            }

            _nativePixmap.fz_tint_pixmap(black, white);
        }

        public Pixmap Warp(Quad quad, int width, int height)
        {
            if (!quad.IsConvex)
                throw new Exception("quad must be convex");

            FzPoint[] points = new FzPoint[4] { 
                quad.UpperLeft.ToFzPoint(),
                quad.UpperRight.ToFzPoint(),
                quad.LowerRight.ToFzPoint(),
                quad.UpperLeft.ToFzPoint()
            };
            //FzPixmap dst = mupdf.mupdf.fz_warp_pixmap(_nativePixmap, points, width, height);//issue
            return null;//issue
        }
    }

    internal class FilePtrOutput : FzOutput2
    {
        public MemoryStream fstream { get; set; }

        public FilePtrOutput(MemoryStream s) : base()
        {
            this.fstream = s;
            this.use_virtual_write();
            this.use_virtual_seek();
            this.use_virtual_tell();
            this.use_virtual_truncate();
        }

        public long Seek(FzContext ctx, int offset, int whence)
        {
            return fstream.Seek(offset, (SeekOrigin)whence);
        }

        public long Tell(FzContext ctx)
        {
            return fstream.Position;
        }

        public void Truncate(FzContext ctx)
        {
            fstream.SetLength(0);
        }

        public void Write(FzContext ctx, byte[] rawData, int dataLength)
        {
            fstream.Write(rawData, 0, dataLength);
        }
    }
}
