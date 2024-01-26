using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace MuPDF.NET
{
    public static class Utils
    {
        public static int FZ_MIN_INF_RECT = (int)(-0x80000000);

        public static int FZ_MAX_INF_RECT = (int)0x7fffff80;

        public static double FLT_EPSILON = 1e-5;

        public static string ANNOT_ID_STEM = "fitz";

        public static int SigFlag_SignaturesExist = 1;
        public static int SigFlag_AppendOnly = 2;

        public static int UNIQUE_ID = 0;

        public static List<string> MUPDF_WARNINGS_STORE = new List<string>();
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

        /// <summary>
        /// ColorSpace types
        /// </summary>
        public static int CS_RGB = 1;
        public static int CS_GRAY = 2;
        public static int CS_CMYK = 3;

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
            int rotate;
            if (page.obj() == null)
                Console.WriteLine(page.obj().ToString());
            PdfObj obj = page   .obj().pdf_dict_get(new PdfObj("Rotate"));
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

        public static FzRect RectFromObj(dynamic r)
        {
            if (r is FzRect)
                return r;
            if (r is Rect)
                return r.ToFzRect();
            if (r.Length != 4)
                return new FzRect(FzRect.Fixed.Fixed_INFINITE);
            return new FzRect(
                (float)Convert.ToDouble(r[0]),
                (float)Convert.ToDouble(r[1]),
                (float)Convert.ToDouble(r[2]),
                (float)Convert.ToDouble(r[3])
                );
        }

        public static List<byte> ReadSamples(FzPixmap pixmap, int offset, int n)
        {
            List<byte> ret = new List<byte>();
            for (int i = 0; i < n; i++)
                ret.Add((byte)pixmap.fz_samples_get(offset + i));
            return ret;
        }

        public static Dictionary<List<byte>, int> ColorCount(FzPixmap pm, dynamic clip)
        {
            Dictionary<List<byte>, int> ret = new Dictionary<List<byte>, int>();
            int count = 0;
            FzIrect irect = pm.fz_pixmap_bbox();
            irect = irect.fz_intersect_irect(RectFromObj(clip));
            int stride = pm.fz_pixmap_stride();
            int width = irect.x1 - irect.x0;
            int height = irect.y1 - irect.y0;
            int n = pm.n();

            int substride = width * n;
            int s = stride * (irect.y0 - pm.y()) + (irect.x0 - pm.x()) * n;
            List<byte> oldPix = Utils.ReadSamples(pm, s, n);
            count = 0;
            if (irect.fz_is_empty_irect() != 0)
                return ret;
            List<byte> pixel = null;
            int c = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < n; i += substride)
                {
                    List<byte> newPix = Utils.ReadSamples(pm, s + j, n);
                    if (newPix != oldPix)
                    {
                        c = ret[pixel];
                        if (c != 0)
                        {
                            count += c;
                        }
                        ret[pixel] = count;
                        count = 1;
                        oldPix = newPix;
                    }
                    else
                        count += 1;
                }
                s += stride;
            }
            pixel = oldPix;
            c = ret[pixel];
            if (c != 0)
            {
                count += c;
            }
            ret[pixel] = count;

            return ret;
        }

        public static void GetWidgetProperties(PdfAnnot annot, Widget widget)
        {
            PdfObj annotObj = mupdf.mupdf.pdf_annot_obj(annot);
            PdfPage page = mupdf.mupdf.pdf_annot_page(annot);
            PdfDocument pdf = page.doc();
            PdfAnnot tw = annot;


        }

        public static void AddAnnotId(PdfAnnot annot, string stem)
        {
            PdfPage page = annot.pdf_annot_page();
            PdfObj annotObj = annot.pdf_annot_obj();
            List<string> names = GetAnnotIDList(page);
            int i = 0;
            string stemId = "";
            while (true)
            {
                stemId = $"{ANNOT_ID_STEM}-{stem}{i}";
                if (!names.Contains(stemId))
                    break;
                i += 1;
            }
            PdfObj name = mupdf.mupdf.pdf_new_string(stemId, (uint)stemId.Length);
            annotObj.pdf_dict_puts("NM", name);
            page.doc().m_internal.resynth_required = 0;
        }

        public static List<string> GetAnnotIDList(PdfPage page)
        {
            List<string> ids = new List<string>();
            PdfObj annots = page.obj().pdf_dict_get(new PdfObj("Annots"));
            if (annots == null)
                return ids;
            for (int i = 0; i < annots.pdf_array_len(); i++)
            {
                PdfObj annotObj = annots.pdf_array_get(i);
                PdfObj name = annotObj.pdf_dict_gets("NM");
                if (name != null)
                    ids.Add(name.pdf_to_text_string());
            }
            return ids;
        }

        public static byte[] ToByte(string s)
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            return utf8.GetBytes(s);
        }

        public static PdfObj EmbedFile(
            PdfDocument pdf,
            FzBuffer buf,
            string filename,
            string ufilename,
            string desc,
            int compress)
        {
            int len = 0;
            PdfObj val = pdf.pdf_new_dict(6);
            val.pdf_dict_put_dict(new PdfObj("CI"), 4);
            PdfObj ef = val.pdf_dict_put_dict(new PdfObj("EF"), 4);
            val.pdf_dict_put_text_string(new PdfObj("F"), filename);
            val.pdf_dict_put_text_string(new PdfObj("UF"), ufilename);
            val.pdf_dict_put_text_string(new PdfObj("Desc"), desc);
            val.pdf_dict_put(new PdfObj("Type"), new PdfObj("Filespec"));
            byte[] bs = Utils.ToByte("  ");

            IntPtr bufPtr = new IntPtr(bs.Length);
            Marshal.Copy(bufPtr, bs, 0, bs.Length);
            SWIGTYPE_p_unsigned_char swigBuf = new SWIGTYPE_p_unsigned_char(bufPtr, false);

            PdfObj f = pdf.pdf_add_stream(
                mupdf.mupdf.fz_new_buffer_from_copied_data(swigBuf, (uint)bs.Length),
                new PdfObj(),
                0
                );
            ef.pdf_dict_put(new PdfObj("F"), f);
            Utils.UpdateStream(pdf, f, buf, compress);
            len = (int)buf.fz_buffer_storage(new SWIGTYPE_p_p_unsigned_char(IntPtr.Zero, false));
            f.pdf_dict_put_int(new PdfObj("DL"), len);
            f.pdf_dict_put_int(new PdfObj("Length"), len);
            PdfObj param = f.pdf_dict_put_dict(new PdfObj("Params"), 4);
            param.pdf_dict_put_int(new PdfObj("Size"), len);

            return val;
        }

        public static void MakeAnnotDA(PdfAnnot annot, int nCol, float[] col, string fontName, float fontSize)
        {
            string buf = "";
            if (nCol > 0)
                buf += "0 g ";
            else if (nCol == 1)
                buf += $"{col[0]:g} g ";
            else if (nCol == 2)
                Debug.Assert(false);
            else if (nCol == 3)
                buf += $"{col[0]:g} {col[1]:g} {col[2]:g} rg ";
            else
                buf += $"{col[0]:g} {col[1]:g} {col[2]:g} {col[3]:g} k ";
            buf += $"/{ExpandFileName(fontName)} {fontSize} Tf";
            annot.pdf_annot_obj().pdf_dict_put_text_string(new PdfObj("DA"), buf);
        }

        public static string ExpandFileName(string filename)
        {
            if (filename == null) return "Helv";
            if (filename.StartsWith("Co")) return "Cour";
            if (filename.StartsWith("co")) return "Cour";
            if (filename.StartsWith("Ti")) return "TiRo";
            if (filename.StartsWith("ti")) return "TiRo";
            if (filename.StartsWith("Sy")) return "Symb";
            if (filename.StartsWith("sy")) return "Symb";
            if (filename.StartsWith("Za")) return "ZaDb";
            if (filename.StartsWith("za")) return "ZaDb";
            return "Helv";
        }

        public static List<WordBlock> GetTextWords(
            MuPDFPage page,
            Rect clip = null,
            int flags = 0,
            MuPDFSTextPage stPage = null,
            bool sort = false,
            char[] delimiters = null
            )
        {
            if (flags == 0)
                flags = flags = (int)(TextFlags.TEXT_PRESERVE_WHITESPACE | TextFlags.TEXT_PRESERVE_LIGATURES | TextFlags.TEXT_MEDIABOX_CLIP);
            MuPDFSTextPage tp = stPage;
            if (tp == null)
                tp = page.GetSTextPage(clip, flags);
            else if (tp._parent != page)
                throw new Exception("not a textpage of this page");

            List<WordBlock> words = tp.ExtractWords(delimiters);
            if (stPage is null)
                tp.Dispose();
            if (sort)
                words.Sort((WordBlock w1, WordBlock w2) =>
                {
                    var result = w1.Y1.CompareTo(w2.Y1);
                    if (result == 0)
                    {
                        result = w1.X0.CompareTo(w2.X0);
                    }
                    return result;
                });
            return words;
        }

        public static dynamic GetText(
            MuPDFPage page,
            string option = "text",
            Rect clip = null,
            int flags = 0,
            MuPDFSTextPage stPage = null,
            bool sort = false,
            char[] delimiters = null
            )
        { 
            Dictionary<string, int> formats = new Dictionary<string, int>()
            {
                { "text", 0 },
                { "html", 1 },
                { "json", 1 },
                { "rawjson", 1 },
                { "xml", 0 },
                { "xhtml", 1 },
                { "dict", 1 },
                { "rawdict", 1 },
                { "words", 0 },
                { "blocks", 1 },
            };

            option = option.ToLower();
            if (!formats.Keys.Contains(option))
                option = "text";
            if (flags == 0)
            {
                flags = (int)(TextFlags.TEXT_PRESERVE_WHITESPACE | TextFlags.TEXT_PRESERVE_LIGATURES | TextFlags.TEXT_MEDIABOX_CLIP);
                if (formats[option] == 1)
                    flags = flags | (int)TextFlags.TEXT_PRESERVE_IMAGES;
            }

            if (option == "words")
            {
                return Utils.GetTextWords(
                    page,
                    clip,
                    flags,
                    stPage,
                    sort,
                    delimiters
                    );
            }
            
            Rect cb = null;
            if ((new List<string>() { "html", "xml", "xhtml" }).Contains(option))
                clip = page.CROPBOX;
            if (clip != null)
                cb = null;
            else if (page is MuPDFPage)
                cb = page.CROPBOX;
            if (clip == null)
                clip = page.CROPBOX;

            MuPDFSTextPage tp = stPage;
            if (tp is null)
                tp = page.GetSTextPage(clip, flags);
            else if (tp._parent != page)
                throw new Exception("not a textpage of this page");
            
            dynamic t = null;
            if (option == "json")
                t = tp.ExtractJSON(cb, sort);
            else if (option == "rawjson")
                t = tp.ExtractRawJSON(cb, sort);
            else if (option == "dict")
                t = tp.ExtractDict(cb, sort);
            else if (option == "rawdict")
                t = tp.ExtractRAWDict(cb, sort);
            else if (option == "html")
                t = tp.ExtractHtml();
            else if (option == "xml")
                t = tp.ExtractXML();
            else if (option == "xhtml")
                t = tp.ExtractText();

            if (stPage is null)
                tp.Dispose();
            return t;
        }

        public static void SetFieldType(PdfDocument doc, PdfObj annotObj, PdfWidgetType type)
        {
            PdfFieldType setBits = 0;
            PdfFieldType clearBits = 0;
            PdfObj typeName = null;

            if (type == PdfWidgetType.PDF_WIDGET_TYPE_BUTTON)
            {
                typeName = new PdfObj("Btn");
                setBits = PdfFieldType.PDF_BTN_FIELD_IS_PUSHBUTTON;
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON)
            {
                typeName = new PdfObj("Btn");
                clearBits = PdfFieldType.PDF_BTN_FIELD_IS_PUSHBUTTON;
                setBits = PdfFieldType.PDF_BTN_FIELD_IS_RADIO;
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX)
            {
                typeName = new PdfObj("Btn");
                clearBits = (PdfFieldType.PDF_BTN_FIELD_IS_PUSHBUTTON | PdfFieldType.PDF_BTN_FIELD_IS_RADIO);
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_TEXT)
            {
                typeName = new PdfObj("Tx");
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_LISTBOX)
            {
                typeName = new PdfObj("Ch");
                clearBits = PdfFieldType.PDF_CH_FIELD_IS_COMBO;
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX)
            {
                typeName = new PdfObj("Ch");
                setBits = PdfFieldType.PDF_CH_FIELD_IS_COMBO;
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_SIGNATURE)
            {
                typeName = new PdfObj("Sig");
            }

            if (typeName != null)
                annotObj.pdf_dict_put(new PdfObj("FT"), typeName);

            int bits = 0;
            if ((int)setBits != 0 || (int)setBits != 0)
            {
                bits = annotObj.pdf_dict_get_int(new PdfObj("Ff"));
                bits &= ~(int)clearBits;
                bits |= (int)setBits;
                annotObj.pdf_dict_put_int(new PdfObj("Ff"), bits);
            }

        }

        public static PdfAnnot CreateWidget(PdfDocument doc, PdfPage page, PdfWidgetType type, string fieldName)
        {
            int oldSigFlags = doc.pdf_trailer().pdf_dict_getp("Root/AcroForm/SigFlags").pdf_to_int();
            PdfAnnot annot = page.pdf_create_annot_raw(pdf_annot_type.PDF_ANNOT_WIDGET);
            PdfObj annotObj = annot.pdf_annot_obj();
            try
            {
                Utils.SetFieldType(doc, annotObj, type);
                annotObj.pdf_dict_put_text_string(new PdfObj("T"), fieldName);

                /*if (type == PdfWidgetType.PDF_WIDGET_TYPE_SIGNATURE)
                {
                    int sigFlags = oldSigFlags | (Utils.SigFlag_SignaturesExist | Utils.SigFlag_AppendOnly);
                    Utils.pdf_dict_putl(
                        doc.pdf_trailer(),
                        mupdf.mupdf.pdf_new_nt(sigFlags),
                        new string[]
                        {
                            "Root", "AcroForm", "SigFlags"
                        }
                    );
                }*/

                PdfObj form = doc.pdf_trailer().pdf_dict_getp("Root/AcroForm/Fields");
                if (form == null)
                {
                    form = doc.pdf_new_array(1);
                    Utils.pdf_dict_putl(
                        doc.pdf_trailer(),
                        form,
                        new string[]
                        {
                            "Root", "AcroForm", "Fields"
                        }
                        );
                }

                form.pdf_array_push(annotObj);
            }
            catch (Exception e)
            {
                page.pdf_delete_annot(annot);

                if (type == PdfWidgetType.PDF_WIDGET_TYPE_SIGNATURE)
                {
                    Utils.pdf_dict_putl(
                        doc.pdf_trailer(),
                        mupdf.mupdf.pdf_new_int(oldSigFlags),
                        new string[]
                        {
                            "Root", "AcroForm", "SigFlags"
                        }
                        );
                }
                throw e;
            }
            return annot;
        }

        public static List<(string, int)> GetResourceProperties(PdfObj refer)
        {
            PdfObj properties = Utils.pdf_dict_getl(refer, new string[] { "Resource", "Properties" });
            List<(string, int)> rc = new List<(string, int)>();
            if (properties == null)
                return null;
            else
            {
                int n = properties.pdf_dict_len();
                if (n < 1)
                    return null;
                
                for (int i =0; i < n; i++)
                {
                    PdfObj key = properties.pdf_dict_get_key(i);
                    PdfObj val = properties.pdf_dict_get_val(i);
                    string c = key.pdf_to_name();
                    int xref = val.pdf_to_num();
                    rc.Add((c, xref));
                }
            }
            return rc;
        }

        public static void SetResourceProperty(PdfObj refer, string name, int xref)
        {
            PdfDocument pdf = refer.pdf_get_bound_document();
            PdfObj ind = pdf.pdf_new_indirect(xref, 0);

            if (ind == null)
                Console.WriteLine(Utils.ErrorMessages["MSG_BAD_XREF"]);

            PdfObj resource = refer.pdf_dict_get(new PdfObj("Resource"));
            if (resource == null)
                resource = refer.pdf_dict_put_dict(new PdfObj("Resources"), 1);
            
            PdfObj properties = resource.pdf_dict_get(new PdfObj("Properties"));
            if (properties == null)
                properties = resource.pdf_dict_put_dict(new PdfObj("Properties"), 1);

            properties.pdf_dict_put(mupdf.mupdf.pdf_new_name(name), ind);
        }

        public static int GenID()
        {
            UNIQUE_ID += 1;
            return UNIQUE_ID;
        }

        public static void EmbeddedClean(PdfDocument pdf)
        {
            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            PdfObj coll = root.pdf_dict_get(new PdfObj("Collection"));
            if (coll != null && coll.pdf_dict_len() > 0)
            {
                root.pdf_dict_del(new PdfObj("Collection"));
            }

            PdfObj efiles = Utils.pdf_dict_getl(
                root,
                new string[]
                {
                    "Names", "EmbeddedFiles", "Names"
                }
                );
            if (efiles != null)
                root.pdf_dict_put_name(new PdfObj("PageMode"), "UseAttachments");
        }

        public static void EnsureIdentity()
        {
            IntPtr rndPtr = Marshal.AllocHGlobal(10);
            Console.WriteLine(rndPtr);
            SWIGTYPE_p_unsigned_char swigRnd = new SWIGTYPE_p_unsigned_char(rndPtr, false);
            mupdf.mupdf.fz_memrnd(swigRnd, 16);
            Console.WriteLine(rndPtr);
            /*PdfObj id_ = pdf.pdf_trailer().pdf_dict_get(new PdfObj("ID"));
            if (id_ == null)
            {
                

            }*/
        }
    }
}
