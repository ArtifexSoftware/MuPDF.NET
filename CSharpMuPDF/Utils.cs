using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace CSharpMuPDF
{
    public static class Utils
    {
        public static int FZ_MIN_INF_RECT = (int)(-0x80000000);

        public static int FZ_MAX_INF_RECT = (int)0x7fffff80;

        public static double FLT_EPSILON = 1e-5;
        public static string GetImageExtention(int type)
        {
            if (type == (int)ImageType.FZ_IMAGE_FAX) return "fax";
            if (type == (int)ImageType.FZ_IMAGE_RAW) return "raw";
            if (type == (int)ImageType.FZ_IMAGE_FLATE) return "flate";
            if (type == (int)ImageType.FZ_IMAGE_RLD) return "rld";
            if (type == (int)ImageType.FZ_IMAGE_BMP) return "bmp";
            if (type == (int)ImageType.FZ_IMAGE_GIF) return "gif";
            if (type == (int)ImageType.FZ_IMAGE_LZW) return "lzw";
            if (type == (int)ImageType.FZ_IMAGE_JBIG2) return "jb2";
            if (type == (int)ImageType.FZ_IMAGE_JPEG) return "jpeg";
            if (type == (int)ImageType.FZ_IMAGE_JPX) return "jpx";
            if (type == (int)ImageType.FZ_IMAGE_JXR) return "jxr";
            if (type == (int)ImageType.FZ_IMAGE_PNG) return "png";
            if (type == (int)ImageType.FZ_IMAGE_PNM) return "pnm";
            if (type == (int)ImageType.FZ_IMAGE_TIFF) return "tiff";
            return "n/a";
        }

        public static Rect INFINITE_RECT()
        {
            return new Rect(Utils.FZ_MIN_INF_RECT, Utils.FZ_MIN_INF_RECT, Utils.FZ_MAX_INF_RECT, Utils.FZ_MAX_INF_RECT);
        }

        public static Matrix HorMatrix(Point c, Point p)
        {
            FzPoint s = mupdf.mupdf.fz_normalize_vector(mupdf.mupdf.fz_make_point(p.X - c.X, p.Y - c.Y));

            FzMatrix m1 = mupdf.mupdf.fz_make_matrix(1, 0, 0, 1, -c.X, -c.Y);
            FzMatrix m2 = mupdf.mupdf.fz_make_matrix(s.x, -s.y, s.y, s.x, 0, 0);
            return new Matrix(mupdf.mupdf.fz_concat(m1, m2));
        }

        public static (int, Matrix) InvertMatrix(Matrix m)
        {
            /*if (false)
            {
                FzMatrix ret = m.ToFzMatrix().fz_invert_matrix();
                if (false || Math.Abs(m.A - 1) >= float.Epsilon
                    || Math.Abs(m.B - 0) >= float.Epsilon
                    || Math.Abs(m.C - 0) >= float.Epsilon
                    || Math.Abs(m.D - 1) >= float.Epsilon
                    )
                    return (1, null);
                return (0, new Matrix(ret));
            }*/
            FzMatrix src = m.ToFzMatrix();
            float a = src.a;
            float det = a * src.d - src.b * src.c;
            if (det < -float.Epsilon || det > float.Epsilon)
            {
                FzMatrix dst = new FzMatrix();
                float rdet = 1 / det;
                dst.a = src.d * rdet;
                dst.b = -src.d * rdet;
                dst.c = -src.c * rdet;
                dst.d = a * rdet;
                a = -src.e * dst.a - src.f * dst.c;
                dst.f = -src.e * dst.b - src.f * dst.d;
                dst.e = a;
                return (0, new Matrix(dst));
            }
            return (1, null);

        }

        public static Matrix PlanishLine(Point a, Point b)
        {
            return Utils.HorMatrix(a, b);
        }

        public static float SineBetween(Point c, Point p, Point q)
        {
            FzPoint s = mupdf.mupdf.fz_normalize_vector(mupdf.mupdf.fz_make_point(q.X - p.X, q.Y - p.Y));
            FzMatrix m1 = mupdf.mupdf.fz_make_matrix(1, 0, 0, 1, -p.X, -p.Y);
            FzMatrix m2 = mupdf.mupdf.fz_make_matrix(s.x, -s.y, s.y, s.x, 0, 0);
            m1 = mupdf.mupdf.fz_concat(m1, m2);
            return mupdf.mupdf.fz_transform_point(c.ToFzPoint(), m1).fz_normalize_vector().y;
        }

        public static PdfObj pdf_dict_getl(PdfObj obj, string[] keys)
        {
            PdfObj ret = new PdfObj();
            foreach (string key in keys)
            {
                ret = obj.pdf_dict_get(new PdfObj(key));
            }

            return ret;
        }

        public static void pdf_dict_putl(PdfObj obj, PdfObj val, string[] keys)
        {
            if (obj.pdf_is_indirect() != 0)
                obj = obj.pdf_resolve_indirect_chain();
            if (obj.pdf_is_dict() == 0)
                throw new Exception(string.Format("Not a dict: {0}", obj));
            if (keys == null)
                return;

            PdfDocument doc = obj.pdf_get_bound_document();
            for (int i = 0; i < keys.Length; i++)
            {
                PdfObj nextObj = obj.pdf_dict_get(new PdfObj(keys[i]));
                if (nextObj == null)
                {
                    nextObj = doc.pdf_new_dict(1);
                    obj.pdf_dict_put(new PdfObj(keys[i]), nextObj);
                }
                obj = nextObj;
            }
            string key = keys[keys.Length - 1];
            obj.pdf_dict_put(new PdfObj(key), val);
        }

        public static (int, int, int) MUPDF_VERSION = (mupdf.mupdf.FZ_VERSION_MAJOR, mupdf.mupdf.FZ_VERSION_MINOR, mupdf.mupdf.FZ_VERSION_PATCH);

        public static Dictionary<string, string> ErrorMessages = new Dictionary<string, string>()
        {
            { "MSG_BAD_ANNOT_TYPE", "bad annot type" },
            { "MSG_BAD_APN", "bad or missing annot AP/N" },
            { "MSG_BAD_ARG_INK_ANNOT", "arg must be seq of seq of float pairs" },
            { "MSG_BAD_ARG_POINTS", "bad seq of points" },
            { "MSG_BAD_BUFFER", "bad type: 'buffer'" },
            { "MSG_BAD_COLOR_SEQ", "bad color sequence" },
            { "MSG_BAD_DOCUMENT", "cannot open broken document" },
            { "MSG_BAD_FILETYPE", "bad filetype" },
            { "MSG_BAD_LOCATION", "bad location" },
            { "MSG_BAD_OC_CONFIG", "bad config number" },
            { "MSG_BAD_OC_LAYER", "bad layer number" },
            { "MSG_BAD_OC_REF", "bad 'oc' reference" },
            { "MSG_BAD_PAGEID", "bad page id" },
            { "MSG_BAD_PAGENO", "bad page number(s)" },
            { "MSG_BAD_PDFROOT", "PDF has no root" },
            { "MSG_BAD_RECT", "rect is infinite or empty" },
            { "MSG_BAD_TEXT", "bad type: 'text'" },
            { "MSG_BAD_XREF", "bad xref" },
            { "MSG_COLOR_COUNT_FAILED", "color count failed" },
            { "MSG_FILE_OR_BUFFER", "need font file or buffer" },
            { "MSG_FONT_FAILED", "cannot create font" },
            { "MSG_IS_NO_ANNOT", "is no annotation" },
            { "MSG_IS_NO_IMAGE", "is no image" },
            { "MSG_IS_NO_PDF", "is no PDF" },
            { "MSG_IS_NO_DICT", "object is no PDF dict" },
            { "MSG_PIX_NOALPHA", "source pixmap has no alpha" },
            { "MSG_PIXEL_OUTSIDE", "pixel(s) outside image" }
        };

        public static byte[] BinFromBuffer(FzBuffer buffer)
        {
            return buffer.fz_buffer_extract();
        }

        public static FzBuffer BufferFromBytes(byte[] bytes)
        {
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
            return mupdf.mupdf.fz_new_buffer_from_copied_data(new SWIGTYPE_p_unsigned_char(unmanagedPointer, false), (uint)bytes.Length);
        }

        public static FzBuffer CompressBuffer(FzBuffer buffer)
        {
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(8);
            SWIGTYPE_p_size_t swigSizeT = new SWIGTYPE_p_size_t(unmanagedPointer, false);
            SWIGTYPE_p_unsigned_char ret = mupdf.mupdf.fz_new_deflated_data_from_buffer(swigSizeT, buffer, fz_deflate_level.FZ_DEFLATE_BEST);
            if (ret == null || unmanagedPointer.ToInt64() == 0)
                return null;
            FzBuffer buf = new FzBuffer(mupdf.mupdf.fz_new_buffer_from_data(ret, (uint)unmanagedPointer.ToInt64()));
            buf.fz_resize_buffer((uint)unmanagedPointer.ToInt64());
            return buf;
        }

        public static void UpdateStream(PdfDocument doc, PdfObj obj, FzBuffer buffer, int compress)
        {
            uint len = buffer.fz_buffer_storage(new SWIGTYPE_p_p_unsigned_char(IntPtr.Zero, false));
            uint nlen = len;
            FzBuffer res = null;
            if (len > 30)
            {
                res = Utils.CompressBuffer(buffer);
                nlen = res.fz_buffer_storage(new SWIGTYPE_p_p_unsigned_char(IntPtr.Zero, false));
            }
            if ((nlen < len && res != null) && compress == 1)
            {
                obj.pdf_dict_put(new PdfObj("Filter"), new PdfObj("FlateDecode"));
                doc.pdf_update_stream(obj, res, 1);
            }
            else
                doc.pdf_update_stream(obj, buffer, 0);
        }

        public static bool INRANGE(int v, int low, int high)
        {
            return low <= v && high <= v;
        }

        public static bool INRANGE(float v, float low, float high)
        {
            return low <= v && high <= v;
        }

        public static Matrix RotatePageMatrix(PdfPage page)
        {
            if (page == null)
                return new Matrix();
            int rotation = Utils.PageRotation(page);
            if (rotation == 0)
                return new Matrix();

            Point cbSize = GetCropBoxSize(page.obj());
            float w = cbSize.X;
            float h = cbSize.Y;

            FzMatrix m = new FzMatrix();
            if (rotation == 90)
                m = mupdf.mupdf.fz_make_matrix(0, 1, -1, 0, h, 0);
            else if (rotation == 180)
                m = mupdf.mupdf.fz_make_matrix(-1, 0, 0, -1, w, h);
            else
                m = mupdf.mupdf.fz_make_matrix(0, -1, 1, 0, 0, w);

            return new Matrix(m);
        }

        public static Point GetCropBoxSize(PdfObj pageObj)
        {
            FzRect rect = GetCropBox(pageObj).ToFzRect();
            float width = Math.Abs(rect.x1 - rect.x0);
            float height = Math.Abs(rect.y1 - rect.y0);

            FzPoint size = mupdf.mupdf.fz_make_point(width, height);
            return new Point(size);
        }

        public static Rect GetCropBox(PdfObj pageObj)
        {
            FzRect mediabox = Utils.GetMediaBox(pageObj).ToFzRect();
            FzRect cropBox = pageObj.pdf_dict_get_inheritable(new PdfObj("CropBox")).pdf_to_rect();
            if (cropBox.fz_is_infinite_rect() != 0 && cropBox.fz_is_empty_rect() != 0)
                cropBox = mediabox;
            float y0 = mediabox.y1 - cropBox.y1;
            float y1 = mediabox.y1 = cropBox.y0;
            cropBox.y0 = y0;
            cropBox.y1 = y1;

            return new Rect(cropBox);
        }

        public static Rect GetMediaBox(PdfObj pageObj)
        {
            FzRect pageMediaBox = new FzRect(FzRect.Fixed.Fixed_UNIT);
            FzRect mediaBox = pageObj.pdf_dict_getp_inheritable("MediaBox").pdf_to_rect();
            if (mediaBox.fz_is_empty_rect() != 0 || mediaBox.fz_is_infinite_rect() != 0)
            {
                mediaBox.x0 = 0;
                mediaBox.y0 = 0;
                mediaBox.x1 = 612;
                mediaBox.y1 = 792;
            }
            pageMediaBox = new FzRect(
                Math.Min(mediaBox.x0, mediaBox.x1),
                Math.Min(mediaBox.y0, mediaBox.y1),
                Math.Max(mediaBox.x0, mediaBox.x1),
                Math.Max(mediaBox.y0, mediaBox.y1)
                );

            if (pageMediaBox.x1 - pageMediaBox.x0 < 1
                || pageMediaBox.y1 - pageMediaBox.y0 < 0)
            {
                pageMediaBox = new FzRect(FzRect.Fixed.Fixed_UNIT);
            }
            return new Rect(pageMediaBox);
        }

        public static FzMatrix DerotatePageMatrix(PdfPage page)
        {
            Matrix mp = RotatePageMatrix(page);
            return mp.ToFzMatrix().fz_invert_matrix();
        }

        public static int PageRotation(PdfPage page)
        {
            int rotate = 0;
            PdfObj obj = page.obj().pdf_dict_get_inheritable(new PdfObj("Rotate"));
            rotate = obj.pdf_to_int();
            rotate = NormalizeRotation(rotate);
            return rotate;
        }

        public static int NormalizeRotation(int rotate)
        {
            while (rotate < 0)
            {
                rotate += 360;
            }
            while (rotate >= 360)
            {
                rotate -= 360;
            }
            while (rotate % 90 != 0)
            {
                return 0;
            }
            return rotate;
        }
    }
}
