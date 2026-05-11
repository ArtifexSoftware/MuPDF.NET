using mupdf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text;

namespace MuPDF.NET
{
    /// <summary>
    /// Internal helper/utility methods for type conversions and common operations.
    /// </summary>
    internal static class Helpers
    {
        /// <summary>Python-style tuple / array length for <c>Page._addAnnot_FromString</c> (works on netstandard2.0 without referencing <c>ITuple</c>).</summary>
        internal static int PythonTupleLikeCount(object linklist)
        {
            if (linklist == null)
                throw new ValueErrorException("bad 'linklist' argument");
            if (linklist is object[] a)
                return a.Length;
            if (linklist is IList list && linklist is not string)
                return list.Count;
            var itf = linklist.GetType().GetInterface("System.Runtime.CompilerServices.ITuple");
            if (itf != null)
            {
                var lenProp = itf.GetProperty("Length", BindingFlags.Public | BindingFlags.Instance);
                if (lenProp != null)
                    return Convert.ToInt32(lenProp.GetValue(linklist, null), CultureInfo.InvariantCulture);
            }
            throw new ValueErrorException("bad 'linklist' argument");
        }

        /// <summary>Python-style tuple / array item for <c>Page._addAnnot_FromString</c>.</summary>
        internal static object PythonTupleLikeItem(object linklist, int index)
        {
            if (linklist is object[] a)
                return a[index];
            if (linklist is IList list)
                return list[index];
            var itf = linklist.GetType().GetInterface("System.Runtime.CompilerServices.ITuple");
            if (itf != null)
            {
                var getter = itf.GetMethod("get_Item", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
                if (getter != null)
                    return getter.Invoke(linklist, new object[] { index });
            }
            throw new ValueErrorException("bad 'linklist' argument");
        }

        /// <summary>Convert a Rect to MuPDF's native FzRect, using infinite rect for null.</summary>
        internal static mupdf.FzRect RectToFz(Rect? rect)
        {
            if (rect == null) return new mupdf.FzRect(mupdf.mupdf.fz_infinite_rect);
            return rect.ToFzRect();
        }

        /// <summary>
        /// Walk <c>dict[key0][key1]...</c> like MuPDF <c>pdf_dict_getl</c> (not exposed on this C# wrapper).
        /// </summary>
        internal static mupdf.PdfObj PdfDictGetl(mupdf.PdfObj dict, params mupdf.PdfObj[] keys)
        {
            if (dict?.m_internal == null || keys == null || keys.Length == 0)
                return new mupdf.PdfObj();
            mupdf.PdfObj current = dict;
            foreach (var key in keys)
            {
                if (current.m_internal == null) return new mupdf.PdfObj();
                current = mupdf.mupdf.pdf_dict_get(current, key);
            }
            return current;
        }

        internal static Rect RectFromFz(mupdf.FzRect fz) => new Rect(fz.x0, fz.y0, fz.x1, fz.y1);
        internal static Rect RectFromFz(mupdf.fz_rect fz) => new Rect(fz.x0, fz.y0, fz.x1, fz.y1);

        internal static mupdf.FzIrect IRectToFz(IRect? irect)
        {
            if (irect == null)
            {
                var ir = new mupdf.FzIrect(); ir.x0 = ir.y0 = Constants.FzMinInfRect; ir.x1 = ir.y1 = Constants.FzMaxInfRect; return ir;
            }
            return irect.ToFzIRect();
        }

        internal static IRect IRectFromFz(mupdf.FzIrect fz) => new IRect(fz.x0, fz.y0, fz.x1, fz.y1);

        internal static mupdf.FzPoint PointToFz(Point? p)
        {
            if (p == null) return mupdf.mupdf.fz_make_point(0, 0);
            return p.ToFzPoint();
        }

        internal static Point PointFromFz(mupdf.FzPoint fz) => new Point(fz.x, fz.y);
        internal static Point PointFromFz(mupdf.fz_point fz) => new Point(fz.x, fz.y);

        internal static mupdf.FzMatrix MatrixToFz(Matrix? m)
        {
            if (m == null) return new mupdf.FzMatrix(mupdf.mupdf.fz_identity);
            return m.ToFzMatrix();
        }

        internal static Matrix MatrixFromFz(mupdf.FzMatrix fz) => new Matrix(fz.a, fz.b, fz.c, fz.d, fz.e, fz.f);

        internal static mupdf.FzQuad QuadToFz(Quad? q)
        {
            if (q == null) return new mupdf.FzQuad();
            return q.ToFzQuad();
        }

        internal static Quad QuadFromFz(mupdf.FzQuad fz) => new Quad(fz);

        /// <summary>Validate a color component array (must be 1, 3, or 4 values in [0, 1]).</summary>
        internal static void CheckColor(float[]? c)
        {
            if (c == null || c.Length == 0) return;
            if (c.Length != 1 && c.Length != 3 && c.Length != 4)
                throw new ArgumentException("need 1, 3 or 4 color components in range 0 to 1");
            foreach (var v in c)
                if (v < 0 || v > 1) throw new ArgumentException("color components must be in range 0 to 1");
        }

        internal static mupdf.FzBuffer BufferFromBytes(byte[]? data)
        {
            if (data == null || data.Length == 0) return new mupdf.FzBuffer();
            var buffer = new mupdf.FzBuffer((uint)data.Length);
            foreach (byte b in data)
                buffer.fz_append_byte(b);
            return buffer;
        }

        internal static (int count, float[] color) ColorFromSequence(float[]? seq)
        {
            if (seq == null || seq.Length == 0) return (0, Array.Empty<float>());
            int n = seq.Length;
            if (n != 1 && n != 3 && n != 4) throw new ArgumentException(Constants.MSG_BAD_COLOR_SEQ);
            return (n, seq);
        }

        internal static bool InRange(int val, int low, int high) => val >= low && val <= high;
        internal static bool InRange(double val, double low, double high) => val >= low && val <= high;

        internal static int ResolvePageIndex(int pageCount, int index)
        {
            if (index < 0) index += pageCount;
            if (index < 0 || index >= pageCount) throw new IndexOutOfRangeException($"page {index} not in document");
            return index;
        }

        /// <summary>
        /// Return a PDF string depending on its coding.
        /// Notes:
        /// Returns a string bracketed with either "()" or "&lt;&gt;" for hex values.
        /// If only ascii then "(original)" is returned, else if only 8 bit chars
        /// then "(original)" with interspersed octal strings \nnn is returned,
        /// else a string "&lt;FEFF[hexstring]&gt;" is returned, where [hexstring] is the
        /// UTF-16BE encoding of the original.
        /// </summary>
        internal static string GetPdfStr(string s)
        {
            if (string.IsNullOrEmpty(s)) return "()";
            var sb = new StringBuilder();
            // The following either returns the original string with mixed-in
            // octal numbers \nnn for chars outside the ASCII range, or returns
            // the UTF-16BE BOM version of the string.
            foreach (char c in s)
            {
                int oc = c;
                if (oc > 255) // shortcut if beyond 8-bit code range
                {
                    var bom = new byte[] { 254, 255 };
                    var utf16 = Encoding.BigEndianUnicode.GetBytes(s);
                    var all = new byte[bom.Length + utf16.Length];
                    Array.Copy(bom, all, bom.Length);
                    Array.Copy(utf16, 0, all, bom.Length, utf16.Length);
                    return "<" + BitConverter.ToString(all).Replace("-", "").ToLower() + ">";
                }
                if (oc > 31 && oc < 127) // in ASCII range
                {
                    if (c == '(' || c == ')' || c == '\\') // these need to be escaped
                        sb.Append('\\');
                    sb.Append(c);
                }
                else if (oc > 127) // beyond ASCII
                {
                    sb.Append($"\\{oc:000}");
                }
                else switch (oc)
                {
                    // now the white spaces
                    case 8: sb.Append("\\b"); break;
                    case 9: sb.Append("\\t"); break;
                    case 10: sb.Append("\\n"); break;
                    case 12: sb.Append("\\f"); break;
                    case 13: sb.Append("\\r"); break;
                    default: sb.Append("\\267"); break; // unsupported: replace by 0xB7
                }
            }
            return "(" + sb + ")";
        }

        /// <summary>
        /// "Now" timestamp in PDF Format.
        /// </summary>
        internal static string GetPdfNow()
        {
            // a = str(abs(time.altzone // 3600)).rjust(2, "0")
            // b = str((abs(time.altzone // 60) % 60)).rjust(2, "0")
            // tz = f"{a}'{b}'"
            var now = DateTime.Now;
            var offset = TimeZoneInfo.Local.GetUtcOffset(now);
            char sign = offset < TimeSpan.Zero ? '-' : '+';
            offset = offset.Duration();
            // tstamp = time.strftime("D:%Y%m%d%H%M%S", time.localtime())
            // if time.altzone > 0: tstamp += "-" + tz
            // elif time.altzone < 0: tstamp += "+" + tz
            return $"D:{now:yyyyMMddHHmmss}{sign}{offset.Hours:00}'{offset.Minutes:00}'";
        }

        /// <summary>Set the /NM key of an annotation if it does not already have one.</summary>
        internal static void AddAnnotId(mupdf.PdfAnnot annot, string stem)
        {
            var annot_obj = mupdf.mupdf.pdf_annot_obj(annot);
            var name = mupdf.mupdf.pdf_dict_gets(annot_obj, "NM");
            if (name.m_internal != null) return;
            int xref = mupdf.mupdf.pdf_to_num(annot_obj);
            string id = $"{Constants.JM_ANNOT_ID_STEM}-{stem}{xref}";
            mupdf.mupdf.pdf_dict_puts(annot_obj, "NM", mupdf.mupdf.pdf_new_text_string(id));
        }

        /// <summary>Return the inheritable /Rotate value of a PDF page.</summary>
        internal static int PageRotation(mupdf.PdfPage page)
        {
            return mupdf.mupdf.pdf_dict_get_inheritable_int(page.obj(), mupdf.mupdf.pdf_new_name("Rotate"));
        }

        internal static Matrix RotatePageMatrix(Page page)
        {
            if (page == null) return Matrix.Identity;
            int rotation = page.Rotation;
            if (rotation == 0) return Matrix.Identity;

            double w = page.CropBox.Width;
            double h = page.CropBox.Height;
            return rotation switch
            {
                90 => new Matrix(0, 1, -1, 0, h, 0),
                180 => new Matrix(-1, 0, 0, -1, w, h),
                270 => new Matrix(0, -1, 1, 0, 0, w),
                _ => Matrix.Identity,
            };
        }

        internal static Matrix JM_rotate_page_matrix(mupdf.PdfPage page)
        {
            // calculate page rotation matrices
            if (page == null)
                return Matrix.Identity;  // no valid pdf page given

            int rotation = PageRotation(page);
            //log( '{rotation=}')
            if (rotation == 0)
                return Matrix.Identity;  // no rotation

            var cb = mupdf.mupdf.pdf_dict_get_inheritable(page.obj(), mupdf.mupdf.pdf_new_name("CropBox"));
            Rect cbSize;
            if (cb.m_internal != null)
            {
                cbSize = new Rect(mupdf.mupdf.pdf_to_rect(cb));
            }
            else
            {
                var mb = mupdf.mupdf.pdf_dict_get_inheritable(page.obj(), mupdf.mupdf.pdf_new_name("MediaBox"));
                cbSize = new Rect(mupdf.mupdf.pdf_to_rect(mb));
            }
            double w = cbSize.Width;
            double h = cbSize.Height;
            //log( '{=h w}')
            if (rotation == 90)
                return new Matrix(0, 1, -1, 0, h, 0);
            else if (rotation == 180)
                return new Matrix(-1, 0, 0, -1, w, h);
            else
                return new Matrix(0, -1, 1, 0, 0, w);
            //log( 'returning {m=}')
        }

        internal static Matrix DerotatePageMatrix(Page page)
        {
            var rotate = RotatePageMatrix(page);
            return rotate.Inverted() ?? Matrix.Identity;
        }

        internal static Matrix AnnotVertexMatrix(Page page)
        {
            if (page == null) return Matrix.Identity;
            return new Matrix(page.TransformationMatrix).Concat(page.TransformationMatrix, DerotatePageMatrix(page));
        }

        internal static Rect TransformRect(Rect rect, Matrix matrix)
        {
            return new Rect(rect).Transform(matrix);
        }

        internal static Point TransformPoint(Point point, Matrix matrix)
        {
            return new Point(point).Transform(matrix);
        }

        internal static byte[] BufferToBytes(mupdf.FzBuffer buffer)
        {
            /*
            return buffer?.m_internal != null
                ? buffer.fz_buffer_extract()
                : Array.Empty<byte>();
            */
            //return buffer.fz_buffer_extract();
            var outparams = new mupdf.ll_fz_buffer_storage_outparams();
            uint n = mupdf.mupdf.ll_fz_buffer_storage_outparams_fn(buffer.m_internal, outparams);
            var raw1 = mupdf.SWIGTYPE_p_unsigned_char.getCPtr(outparams.datap);
            System.IntPtr raw2 = System.Runtime.InteropServices.HandleRef.ToIntPtr(raw1);
            byte[] ret = new byte[n];
            // Marshal.Copy() raises exception if <raw2> is null even if <n> is zero.
            if (n > 0)
            {
                System.Runtime.InteropServices.Marshal.Copy(raw2, ret, 0, (int)n);
                outparams.Dispose();
            }

            return ret;
        }

        internal static string BufferToUtf8(mupdf.FzBuffer buffer)
        {
            return Encoding.UTF8.GetString(BufferToBytes(buffer));
        }

        internal static string EscapePdfArray(float[] values)
        {
            if (values == null || values.Length == 0) return "[]";
            var sb = new StringBuilder("[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(values[i].ToString("G", System.Globalization.CultureInfo.InvariantCulture));
            }
            sb.Append(']');
            return sb.ToString();
        }

        // ─── Annot helper ports (from __init__.py) ──────────────────────

        internal static string JM_get_border_style(string style)
        {
            // Python JM_get_border_style(): default to "S".
            if (string.IsNullOrEmpty(style)) return "S";
            char s = style[0];
            if (s == 'b' || s == 'B') return "B";
            if (s == 'd' || s == 'D') return "D";
            if (s == 'i' || s == 'I') return "I";
            if (s == 'u' || s == 'U') return "U";
            return "S";
        }

        internal static Dictionary<string, object> JM_annot_border(mupdf.PdfObj annot_obj)
        {
            var dashes = new List<int>();
            string style = null;
            float width = -1;
            int clouds = -1;

            var obj = mupdf.mupdf.pdf_dict_get(annot_obj, mupdf.mupdf.pdf_new_name("Border"));
            if (mupdf.mupdf.pdf_is_array(obj) != 0)
            {
                width = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(obj, 2));
                if (mupdf.mupdf.pdf_array_len(obj) == 4)
                {
                    var dash = mupdf.mupdf.pdf_array_get(obj, 3);
                    for (int i = 0; i < mupdf.mupdf.pdf_array_len(dash); i++)
                        dashes.Add(mupdf.mupdf.pdf_to_int(mupdf.mupdf.pdf_array_get(dash, i)));
                }
            }

            var bs = mupdf.mupdf.pdf_dict_get(annot_obj, mupdf.mupdf.pdf_new_name("BS"));
            if (bs.m_internal != null)
            {
                width = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_dict_get(bs, mupdf.mupdf.pdf_new_name("W")));
                style = mupdf.mupdf.pdf_to_name(mupdf.mupdf.pdf_dict_get(bs, mupdf.mupdf.pdf_new_name("S")));
                if (style == "") style = null;
                obj = mupdf.mupdf.pdf_dict_get(bs, mupdf.mupdf.pdf_new_name("D"));
                if (obj.m_internal != null)
                {
                    dashes.Clear();
                    for (int i = 0; i < mupdf.mupdf.pdf_array_len(obj); i++)
                        dashes.Add(mupdf.mupdf.pdf_to_int(mupdf.mupdf.pdf_array_get(obj, i)));
                }
            }

            obj = mupdf.mupdf.pdf_dict_get(annot_obj, mupdf.mupdf.pdf_new_name("BE"));
            if (obj.m_internal != null)
                clouds = mupdf.mupdf.pdf_to_int(mupdf.mupdf.pdf_dict_get(obj, mupdf.mupdf.pdf_new_name("I")));

            return new Dictionary<string, object>
            {
                ["width"] = width,
                ["dashes"] = dashes.ToArray(),
                ["style"] = style,
                ["clouds"] = clouds,
            };
        }

        internal static Dictionary<string, object> JM_annot_colors(mupdf.PdfObj annot_obj)
        {
            var stroke = new List<float>();
            var fill = new List<float>();

            var o = annot_obj.pdf_dict_get(mupdf.mupdf.PDF_ENUM_NAME_C);
            if (mupdf.mupdf.pdf_is_array(o) != 0)
            {
                int n = mupdf.mupdf.pdf_array_len(o);
                for (int i = 0; i < n; i++)
                    stroke.Add(mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(o, i)));
            }

            o = mupdf.mupdf.pdf_dict_gets(annot_obj, "IC");
            if (mupdf.mupdf.pdf_is_array(o) != 0)
            {
                int n = mupdf.mupdf.pdf_array_len(o);
                for (int i = 0; i < n; i++)
                    fill.Add(mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(o, i)));
            }

            return new Dictionary<string, object>
            {
                ["stroke"] = stroke.ToArray(),
                ["fill"] = fill.ToArray(),
            };
        }

        internal static void JM_annot_set_border(Dictionary<string, object> border, mupdf.PdfDocument doc, mupdf.PdfObj annot_obj)
        {
            if (border == null) throw new ArgumentNullException(nameof(border));

            int dashlen = 0;
            float nwidth = border.TryGetValue("width", out var widthObj) && widthObj != null ? Convert.ToSingle(widthObj) : -1;
            int[] ndashes = border.TryGetValue("dashes", out var dashesObj) ? dashesObj as int[] : null;
            string nstyle = border.TryGetValue("style", out var styleObj) ? styleObj as string : null;
            int nclouds = border.TryGetValue("clouds", out var cloudsObj) && cloudsObj != null ? Convert.ToInt32(cloudsObj) : -1;

            // Python: get old border properties before replacing dictionaries.
            var oborder = JM_annot_border(annot_obj);

            mupdf.mupdf.pdf_dict_del(annot_obj, mupdf.mupdf.pdf_new_name("BS"));
            mupdf.mupdf.pdf_dict_del(annot_obj, mupdf.mupdf.pdf_new_name("BE"));
            mupdf.mupdf.pdf_dict_del(annot_obj, mupdf.mupdf.pdf_new_name("Border"));

            if (nwidth < 0) nwidth = oborder.TryGetValue("width", out var ow) && ow != null ? Convert.ToSingle(ow) : -1;
            if (ndashes == null) ndashes = oborder.TryGetValue("dashes", out var od) ? od as int[] : null;
            if (nstyle == null) nstyle = oborder.TryGetValue("style", out var os) ? os as string : null;
            if (nclouds < 0) nclouds = oborder.TryGetValue("clouds", out var oc) && oc != null ? Convert.ToInt32(oc) : -1;

            if (ndashes != null && ndashes.Length > 0)
            {
                dashlen = ndashes.Length;
                var darr = mupdf.mupdf.pdf_new_array(doc, dashlen);
                foreach (int d in ndashes)
                    mupdf.mupdf.pdf_array_push_int(darr, d);
                var bs = mupdf.mupdf.pdf_new_dict(doc, 2);
                mupdf.mupdf.pdf_dict_put(bs, mupdf.mupdf.pdf_new_name("D"), darr);
                mupdf.mupdf.pdf_dict_put(annot_obj, mupdf.mupdf.pdf_new_name("BS"), bs);
            }

            var bsObj = mupdf.mupdf.pdf_dict_get(annot_obj, mupdf.mupdf.pdf_new_name("BS"));
            if (bsObj.m_internal == null)
            {
                bsObj = mupdf.mupdf.pdf_new_dict(doc, 2);
                mupdf.mupdf.pdf_dict_put(annot_obj, mupdf.mupdf.pdf_new_name("BS"), bsObj);
            }
            mupdf.mupdf.pdf_dict_put(bsObj, mupdf.mupdf.pdf_new_name("W"), mupdf.mupdf.pdf_new_real(nwidth));
            mupdf.mupdf.pdf_dict_put_name(bsObj, mupdf.mupdf.pdf_new_name("S"), dashlen == 0 ? JM_get_border_style(nstyle) : "D");

            if (nclouds > 0)
            {
                var be = mupdf.mupdf.pdf_new_dict(doc, 2);
                mupdf.mupdf.pdf_dict_put_name(be, mupdf.mupdf.pdf_new_name("S"), "C");
                mupdf.mupdf.pdf_dict_put_int(be, mupdf.mupdf.pdf_new_name("I"), nclouds);
                mupdf.mupdf.pdf_dict_put(annot_obj, mupdf.mupdf.pdf_new_name("BE"), be);
            }
        }

        internal static void JM_add_oc_object(mupdf.PdfDocument pdf, mupdf.PdfObj reference, int xref)
        {
            var indobj = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
            if (mupdf.mupdf.pdf_is_dict(indobj) == 0)
                throw new ArgumentException("bad optional content reference");
            var type = mupdf.mupdf.pdf_dict_get(indobj, mupdf.mupdf.pdf_new_name("Type"));
            bool isOcg = mupdf.mupdf.pdf_objcmp(type, mupdf.mupdf.pdf_new_name("OCG")) == 0;
            bool isOcmd = mupdf.mupdf.pdf_objcmp(type, mupdf.mupdf.pdf_new_name("OCMD")) == 0;
            if (!isOcg && !isOcmd)
                throw new ArgumentException("bad optional content reference");
            mupdf.mupdf.pdf_dict_put(reference, mupdf.mupdf.pdf_new_name("OC"), indobj);
        }

        internal static mupdf.FzBuffer JM_read_contents(mupdf.PdfObj pageref)
        {
            var contents = mupdf.mupdf.pdf_dict_get(pageref, mupdf.mupdf.pdf_new_name("Contents"));
            if (contents.m_internal == null)
                return mupdf.mupdf.fz_new_buffer(16);

            if (mupdf.mupdf.pdf_is_array(contents) != 0)
            {
                var res = mupdf.mupdf.fz_new_buffer(1024);
                int n = mupdf.mupdf.pdf_array_len(contents);
                for (int i = 0; i < n; i++)
                {
                    var item = mupdf.mupdf.pdf_array_get(contents, i);
                    if (mupdf.mupdf.pdf_is_stream(item) != 0)
                    {
                        var b = mupdf.mupdf.pdf_load_stream(item);
                        mupdf.mupdf.fz_append_buffer(res, b);
                    }
                }
                return res;
            }

            if (mupdf.mupdf.pdf_is_stream(contents) != 0)
                return mupdf.mupdf.pdf_load_stream(contents);

            return mupdf.mupdf.fz_new_buffer(16);
        }

        internal static int JM_insert_contents(mupdf.PdfDocument pdf, mupdf.PdfObj pageref, mupdf.FzBuffer newcont, bool overlay)
        {
            var contents = mupdf.mupdf.pdf_dict_get(pageref, mupdf.mupdf.pdf_new_name("Contents"));
            var newconts = mupdf.mupdf.pdf_add_stream(pdf, newcont, new mupdf.PdfObj(), 0);
            int xref = mupdf.mupdf.pdf_to_num(newconts);
            if (mupdf.mupdf.pdf_is_array(contents) != 0)
            {
                if (overlay)
                    mupdf.mupdf.pdf_array_push(contents, newconts);
                else
                    mupdf.mupdf.pdf_array_insert(contents, newconts, 0);
            }
            else
            {
                var carr = mupdf.mupdf.pdf_new_array(pdf, 5);
                if (overlay)
                {
                    if (contents.m_internal != null)
                        mupdf.mupdf.pdf_array_push(carr, contents);
                    mupdf.mupdf.pdf_array_push(carr, newconts);
                }
                else
                {
                    mupdf.mupdf.pdf_array_push(carr, newconts);
                    if (contents.m_internal != null)
                        mupdf.mupdf.pdf_array_push(carr, contents);
                }
                mupdf.mupdf.pdf_dict_put(pageref, mupdf.mupdf.pdf_new_name("Contents"), carr);
            }
            return xref;
        }

        internal static void JM_set_resource_property(mupdf.PdfObj reference, string name, int xref)
        {
            var pdf = mupdf.mupdf.pdf_get_bound_document(reference);
            var ind = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
            if (ind.m_internal == null)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);
            var resources = mupdf.mupdf.pdf_dict_get(reference, mupdf.mupdf.pdf_new_name("Resources"));
            if (resources.m_internal == null)
                resources = mupdf.mupdf.pdf_dict_put_dict(reference, mupdf.mupdf.pdf_new_name("Resources"), 1);
            var properties = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("Properties"));
            if (properties.m_internal == null)
                properties = mupdf.mupdf.pdf_dict_put_dict(resources, mupdf.mupdf.pdf_new_name("Properties"), 1);
            mupdf.mupdf.pdf_dict_put(properties, mupdf.mupdf.pdf_new_name(name), ind);
        }

        internal static mupdf.PdfObj JM_xobject_from_page(mupdf.PdfDocument pdfout, mupdf.PdfPage srcpage, int xref, mupdf.PdfGraftMap gmap)
        {
            if (xref > 0)
                return mupdf.mupdf.pdf_new_indirect(pdfout, xref, 0);

            var spageref = srcpage.obj();
            var mediabox = mupdf.mupdf.pdf_to_rect(
                mupdf.mupdf.pdf_dict_get_inheritable(spageref, mupdf.mupdf.pdf_new_name("MediaBox")));
            var resourcesSrc = mupdf.mupdf.pdf_dict_get_inheritable(spageref, mupdf.mupdf.pdf_new_name("Resources"));
            var resources = gmap?.m_internal != null
                ? mupdf.mupdf.pdf_graft_mapped_object(gmap, resourcesSrc)
                : mupdf.mupdf.pdf_graft_object(pdfout, resourcesSrc);

            var res = JM_read_contents(spageref);
            var xobj1 = mupdf.mupdf.pdf_new_xobject(pdfout, mediabox, new mupdf.FzMatrix(), new mupdf.PdfObj(), res);
            mupdf.mupdf.pdf_update_stream(pdfout, xobj1, res, 1);
            mupdf.mupdf.pdf_dict_put(xobj1, mupdf.mupdf.pdf_new_name("Resources"), resources);
            return xobj1;
        }

        internal static mupdf.PdfObj JM_pdf_obj_from_str(mupdf.PdfDocument pdf, string text)
        {
            if (string.IsNullOrEmpty(text))
                return mupdf.mupdf.pdf_new_text_string("");
            try
            {
                var srcBuf = BufferFromBytes(Encoding.UTF8.GetBytes(text));
                var srcStm = mupdf.mupdf.fz_open_buffer(srcBuf);
                // Default PdfLexbuf() does not run pdf_lexbuf_init; parsing then reads garbage and can AV.
                using var lex = new mupdf.PdfLexbuf(mupdf.mupdf.PDF_LEXBUF_SMALL);
                var parsed = pdf.pdf_parse_stm_obj(srcStm, lex);
                if (parsed?.m_internal != null)
                    return parsed;
            }
            catch
            {
                // Fallback to text-string object if parser path fails on current binding/input.
            }
            return mupdf.mupdf.pdf_new_text_string(text);
        }

        internal static void JM_remove_dest_range(mupdf.PdfDocument pdf, HashSet<int> numbers)
        {
            int pagecount = mupdf.mupdf.pdf_count_pages(pdf);
            for (int i = 0; i < pagecount; i++)
            {
                int n1 = i;
                if (numbers.Contains(n1))
                    continue;

                var pageref = mupdf.mupdf.pdf_lookup_page_obj(pdf, i);
                var annots = mupdf.mupdf.pdf_dict_get(pageref, mupdf.mupdf.pdf_new_name("Annots"));
                if (annots.m_internal == null)
                    continue;
                int len = mupdf.mupdf.pdf_array_len(annots);
                for (int j = len - 1; j >= 0; j--)
                {
                    var o = mupdf.mupdf.pdf_array_get(annots, j);
                    var subtype = mupdf.mupdf.pdf_dict_get(o, mupdf.mupdf.pdf_new_name("Subtype"));
                    if (mupdf.mupdf.pdf_objcmp(subtype, mupdf.mupdf.pdf_new_name("Link")) != 0)
                        continue;
                    var action = mupdf.mupdf.pdf_dict_get(o, mupdf.mupdf.pdf_new_name("A"));
                    var dest = mupdf.mupdf.pdf_dict_get(o, mupdf.mupdf.pdf_new_name("Dest"));
                    if (action.m_internal != null)
                    {
                        var actionS = mupdf.mupdf.pdf_dict_get(action, mupdf.mupdf.pdf_new_name("S"));
                        if (mupdf.mupdf.pdf_objcmp(actionS, mupdf.mupdf.pdf_new_name("GoTo")) != 0)
                            continue;
                        dest = mupdf.mupdf.pdf_dict_get(action, mupdf.mupdf.pdf_new_name("D"));
                    }
                    int pno = -1;
                    if (mupdf.mupdf.pdf_is_array(dest) != 0)
                    {
                        var target = mupdf.mupdf.pdf_array_get(dest, 0);
                        pno = mupdf.mupdf.pdf_lookup_page_number(pdf, target);
                    }
                    else if (mupdf.mupdf.pdf_is_string(dest) != 0)
                    {
                        var location = mupdf.mupdf.fz_resolve_link(pdf.super(), mupdf.mupdf.pdf_to_text_string(dest), null, null);
                        pno = mupdf.mupdf.fz_page_number_from_location(pdf.super(), location);
                    }
                    if (pno < 0) // page number lookup did not work
                        continue;
                    n1 = pno;
                    if (numbers.Contains(n1))
                        mupdf.mupdf.pdf_array_delete(annots, j);
                }
            }
        }

        internal static mupdf.PdfAnnot JM_find_annot_irt(mupdf.PdfAnnot annot)
        {
            // Python: return first annot with /IRT pointing to this annot.
            var annotObj = mupdf.mupdf.pdf_annot_obj(annot);
            var page = mupdf.mupdf.pdf_annot_page(annot);
            var it = mupdf.mupdf.pdf_first_annot(page);
            while (it.m_internal != null)
            {
                var irtObj = mupdf.mupdf.pdf_dict_gets(mupdf.mupdf.pdf_annot_obj(it), "IRT");
                if (irtObj.m_internal != null && mupdf.mupdf.pdf_objcmp(irtObj, annotObj) == 0)
                    return it;
                it = mupdf.mupdf.pdf_next_annot(it);
            }
            return new mupdf.PdfAnnot();
        }

        internal static void JM_embedded_clean(PdfDocument pdf)
        {
            /*
            perform some cleaning if we have / EmbeddedFiles:
            (1) remove any / Limits if / Names exists
            (2) remove any empty / Collection
            (3) set / PageMode / UseAttachments
            */

            PdfObj root = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("Root"));

            // remove any empty /Collection entry
            PdfObj coll = mupdf.mupdf.pdf_dict_get(root, mupdf.mupdf.pdf_new_name("Collection"));
            if (coll.m_internal != null && mupdf.mupdf.pdf_dict_len(coll) == 0)
                mupdf.mupdf.pdf_dict_del(root, mupdf.mupdf.pdf_new_name("Collection"));

            PdfObj efiles = PdfDictGetl(
                root,
                new PdfObj[]
                {
                    mupdf.mupdf.pdf_new_name("Names"),
                    mupdf.mupdf.pdf_new_name("EmbeddedFiles"),
                    mupdf.mupdf.pdf_new_name("Names")
                }
            );
            if (efiles.m_internal != null)
                mupdf.mupdf.pdf_dict_put_name(root, mupdf.mupdf.pdf_new_name("PageMode"), "UseAttachments");
        }

        internal static void JM_ensure_identity(PdfDocument pdf)
        {
            // Store ID in PDF trailer
            PdfObj id_ = mupdf.mupdf.pdf_dict_get(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("ID"));
            if (id_.m_internal == null)
            {
                byte[] rnd0 = new byte[16];
                // Need to convert raw bytes into a str to send to
                // mupdf.pdf_new_string(). chr() seems to work for this.
                string rnd = "";
                for (int i = 0; i < rnd0.Length; i++)
                {
                    rnd += (char)rnd0[i];
                }
                id_ = mupdf.mupdf.pdf_dict_put_array(mupdf.mupdf.pdf_trailer(pdf), mupdf.mupdf.pdf_new_name("ID"), 2);
                mupdf.mupdf.pdf_array_push(id_, mupdf.mupdf.pdf_new_string(rnd, (uint)rnd.Length));
                mupdf.mupdf.pdf_array_push(id_, mupdf.mupdf.pdf_new_string(rnd, (uint)rnd.Length));
            }        
        }
    
        internal static string JM_expand_fname(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Helv";
            if (name.StartsWith("Co", StringComparison.OrdinalIgnoreCase)) return "Cour";
            if (name.StartsWith("Ti", StringComparison.OrdinalIgnoreCase)) return "TiRo";
            if (name.StartsWith("Sy", StringComparison.OrdinalIgnoreCase)) return "Symb";
            if (name.StartsWith("Za", StringComparison.OrdinalIgnoreCase)) return "ZaDb";
            return "Helv";
        }

        internal static (float[] color, string font, float size) ParseAnnotDefaultAppearance(mupdf.PdfAnnot annot)
        {
            var annotObj = mupdf.mupdf.pdf_annot_obj(annot);
            var pdf = mupdf.mupdf.pdf_get_bound_document(annotObj);
            var da = mupdf.mupdf.pdf_dict_get_inheritable(annotObj, mupdf.mupdf.pdf_new_name("DA"));
            if (da.m_internal == null)
            {
                var trailer = mupdf.mupdf.pdf_trailer(pdf);
                da = PdfDictGetl(
                    trailer,
                    mupdf.mupdf.pdf_new_name("Root"),
                    mupdf.mupdf.pdf_new_name("AcroForm"),
                    mupdf.mupdf.pdf_new_name("DA"));
            }
            if (da.m_internal == null)
                return (new[] { 0f }, "", 0f);

            string daStr = mupdf.mupdf.pdf_to_text_string(da) ?? "";
            if (daStr.Length == 0)
                return (new[] { 0f }, "", 0f);

            string font = "Helv";
            float size = 12f;
            float[] col = new[] { 0f, 0f, 0f };
            string[] dat = daStr.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < dat.Length; i++)
            {
                if (dat[i] == "Tf" && i >= 2)
                {
                    font = dat[i - 2].TrimStart('/');
                    if (!float.TryParse(dat[i - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out size))
                        size = 12f;
                    continue;
                }
                if (dat[i] == "g" && i >= 1)
                {
                    if (float.TryParse(dat[i - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float c0))
                        col = new[] { c0 };
                    continue;
                }
                if (dat[i] == "rg" && i >= 3)
                {
                    if (float.TryParse(dat[i - 3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float c0)
                        && float.TryParse(dat[i - 2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float c1)
                        && float.TryParse(dat[i - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float c2))
                        col = new[] { c0, c1, c2 };
                    continue;
                }
                if (dat[i] == "k" && i >= 4)
                {
                    if (float.TryParse(dat[i - 4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float c0)
                        && float.TryParse(dat[i - 3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float c1)
                        && float.TryParse(dat[i - 2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float c2)
                        && float.TryParse(dat[i - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float c3))
                        col = new[] { c0, c1, c2, c3 };
                }
            }
            return (col, font, size);
        }

        internal static void JM_make_annot_DA(mupdf.PdfAnnot annot, int ncol, float[] col, string fontname, float fontsize)
        {
            var sb = new StringBuilder();
            if (ncol < 1) sb.Append("0 g ");
            else if (ncol == 1) sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:g} g ", col[0]);
            else if (ncol == 3) sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:g} {1:g} {2:g} rg ", col[0], col[1], col[2]);
            else sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:g} {1:g} {2:g} {3:g} k ", col[0], col[1], col[2], col[3]);
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "/{0} {1:g} Tf", JM_expand_fname(fontname), fontsize);
            mupdf.mupdf.pdf_dict_put_text_string(mupdf.mupdf.pdf_annot_obj(annot), mupdf.mupdf.pdf_new_name("DA"), sb.ToString());
        }

        internal static bool JM_update_appearance(
            mupdf.PdfAnnot annot,
            AnnotationType annotType,
            float opacity,
            string blendMode,
            float[] fillColor,
            int rotate)
        {
            var annotObj = mupdf.mupdf.pdf_annot_obj(annot);
            int nFill = fillColor?.Length ?? 0;

            // Python: remove fill color from unsupported annots or if requested.
            bool supportsInterior = annotType == AnnotationType.Square
                || annotType == AnnotationType.Circle
                || annotType == AnnotationType.Line
                || annotType == AnnotationType.PolyLine
                || annotType == AnnotationType.Polygon;
            if (nFill == 0 || !supportsInterior)
            {
                mupdf.mupdf.pdf_dict_del(annotObj, mupdf.mupdf.pdf_new_name("IC"));
            }
            else
            {
                var col = mupdf.mupdf.pdf_new_array(mupdf.mupdf.pdf_get_bound_document(annotObj), nFill);
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

            mupdf.mupdf.pdf_dirty_annot(annot);
            mupdf.mupdf.pdf_update_annot(annot);

            if ((opacity < 0 || opacity >= 1) && string.IsNullOrEmpty(blendMode))
                return true;

            // Python: create or update /ExtGState for opacity/blend mode.
            var ap = PdfDictGetl(
                annotObj,
                mupdf.mupdf.pdf_new_name("AP"),
                mupdf.mupdf.pdf_new_name("N"));
            if (ap.m_internal == null) return true;

            var resources = mupdf.mupdf.pdf_dict_get(ap, mupdf.mupdf.pdf_new_name("Resources"));
            if (resources.m_internal == null)
                resources = mupdf.mupdf.pdf_dict_put_dict(ap, mupdf.mupdf.pdf_new_name("Resources"), 2);

            var alp0 = mupdf.mupdf.pdf_new_dict(mupdf.mupdf.pdf_get_bound_document(annotObj), 3);
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

        internal static string JM_apply_redact_cross_out(string apText)
        {
            var lines = new List<string>(apText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None));
            if (lines.Count < 6) return apText;
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
            return string.Join("\n", lines);
        }

        internal static string JM_apply_dashes(string apText, int[] dashes)
        {
            if (dashes == null || dashes.Length == 0) return apText;
            string ret = "[" + string.Join(" ", dashes) + "] 0 d\n" + apText;
            // Python: reset dashing after first stroke.
            return ret.Replace("\nS\n", "\nS\n[] 0 d\n");
        }

        internal static string JM_apply_poly_fill(string apText, AnnotationType annotType, string fillCode)
        {
            if (annotType != AnnotationType.Polygon && annotType != AnnotationType.PolyLine)
                return apText;
            var tab = apText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            string ret = tab.Length > 0 ? string.Join("\n", tab, 0, tab.Length - 1) + "\n" : apText;
            if (!string.IsNullOrEmpty(fillCode))
                ret += fillCode + (annotType == AnnotationType.Polygon ? "b" : "S");
            else
                ret += annotType == AnnotationType.Polygon ? "s" : "S";
            return ret;
        }

        internal static string JM_apply_redact_border(string apText, float borderWidth, string strokeCode)
        {
            if (borderWidth <= 0 && string.IsNullOrEmpty(strokeCode)) return apText;
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
            return rebuilt.ToString();
        }

        internal static byte[] JM_apply_freetext_opacity_prefix(byte[] ap, float effectiveOpacity)
        {
            if (ap == null || ap.Length == 0) return ap;
            if (!(effectiveOpacity >= 0 && effectiveOpacity < 1)) return ap;
            string apText = Encoding.UTF8.GetString(ap);
            if (apText.StartsWith("/H gs", StringComparison.Ordinal)) return ap;
            byte[] prefix = Encoding.UTF8.GetBytes("/H gs\n");
            var combined = new byte[prefix.Length + ap.Length];
            Buffer.BlockCopy(prefix, 0, combined, 0, prefix.Length);
            Buffer.BlockCopy(ap, 0, combined, prefix.Length, ap.Length);
            return combined;
        }

        /// <summary>Convert a Document, FzDocument, or PdfDocument to a native FzDocument.</summary>
        internal static mupdf.FzDocument AsFzDocument(object doc)
        {
            if (doc is Document d) { if (d.IsClosed) throw new ValueErrorException("document closed"); return d.NativeDocument; }
            if (doc is mupdf.FzDocument fd) return fd;
            if (doc is mupdf.PdfDocument pd) return pd.super();
            throw new ArgumentException($"Unrecognised document type: {doc.GetType()}");
        }

        /// <summary>Convert a Document or FzDocument to a native PdfDocument.</summary>
        internal static mupdf.PdfDocument AsPdfDocument(object doc, bool required = true)
        {
            if (doc is Document d) { if (d.IsClosed) throw new ValueErrorException("document closed"); doc = d.NativeDocument; }
            if (doc is mupdf.PdfDocument pd) return pd;
            if (doc is mupdf.FzDocument fd)
            {
                var ret = new mupdf.PdfDocument(fd);
                if (required && ret.m_internal == null) throw new InvalidOperationException(Constants.MSG_IS_NO_PDF);
                return ret;
            }
            throw new ArgumentException($"Unrecognised document type: {doc.GetType()}");
        }

        /// <summary>Convert a Page, PdfPage, or FzPage to a native FzPage.</summary>
        internal static mupdf.FzPage AsFzPage(object page)
        {
            if (page is Page p) return p.NativePage;
            if (page is mupdf.PdfPage pp) return pp.super();
            if (page is mupdf.FzPage fp) return fp;
            throw new ArgumentException($"Unrecognised page type: {page.GetType()}");
        }

        /// <summary>Convert a Page or FzPage to a native PdfPage.</summary>
        internal static mupdf.PdfPage AsPdfPage(object page, bool required = true)
        {
            if (page is Page p) page = p.NativePage;
            if (page is mupdf.PdfPage pp) return pp;
            if (page is mupdf.FzPage fp)
            {
                var ret = mupdf.mupdf.pdf_page_from_fz_page(fp);
                if (required && ret.m_internal == null) throw new InvalidOperationException(Constants.MSG_IS_NO_PDF);
                return ret;
            }
            throw new ArgumentException($"Unrecognised page type: {page.GetType()}");
        }

        /// <summary>
        /// Build a PDF content-stream colour operator string from a colour array
        /// and a fill/stroke selector ("c" for stroke, "f" for fill).
        /// </summary>
        internal static string ColorCode(float[]? c, string f)
        {
            if (c == null || c.Length == 0) return "";
            CheckColor(c);
            var sb = new StringBuilder();
            foreach (var v in c) sb.Append($"{v:G} ");
            if (c.Length == 1) return sb + (f == "c" ? "G " : "g ");
            if (c.Length == 3) return sb + (f == "c" ? "RG " : "rg ");
            return sb + (f == "c" ? "K " : "k ");
        }

        /// <summary>
        /// Return a Rect for the paper size indicated in string <paramref name="size"/>.
        /// Must conform to the argument of method 'PaperSize', which will be invoked.
        /// </summary>
        internal static Rect PaperRect(string size)
        {
            var s = size.ToLower().Trim().Replace(" ", "-");
            if (PaperSizes.TryGetValue(s, out var wh))
                return new Rect(0, 0, wh.w, wh.h);
            if (s.EndsWith("-l") || s.EndsWith("-landscape"))
            {
                var baseSize = s.Substring(0, s.IndexOf('-'));
                if (PaperSizes.TryGetValue(baseSize, out wh))
                    return new Rect(0, 0, wh.h, wh.w);
            }
            return new Rect(0, 0, -1, -1);
        }

        /// <summary>
        /// Return a tuple (width, height) for a given paper format string.
        /// Notes:
        /// 'A4-L' will return (842, 595), the values for A4 landscape.
        /// Suffix '-P' and no suffix return the portrait tuple.
        /// </summary>
        internal static (float w, float h) PaperSize(string size)
        {
            var r = PaperRect(size);
            return ((float)r.Width, (float)r.Height);
        }

        /// <summary>
        /// Known paper formats @ 72 dpi as a dictionary. Key is the format string
        /// like "a4" for ISO-A4. Value is the tuple (width, height).
        /// Information taken from the following web sites:
        /// www.din-formate.de
        /// www.din-formate.info/amerikanische-formate.html
        /// www.directtools.de/wissen/normen/iso.htm
        /// </summary>
        internal static Dictionary<string, (float w, float h)> GetPaperSizes()
        {
            return new Dictionary<string, (float w, float h)>(PaperSizes);
        }

        private static readonly Dictionary<string, (float w, float h)> PaperSizes = new Dictionary<string, (float, float)>
        {
            ["a0"] = (2384, 3370), ["a1"] = (1684, 2384), ["a2"] = (1191, 1684), ["a3"] = (842, 1191),
            ["a4"] = (595, 842), ["a5"] = (420, 595), ["a6"] = (298, 420), ["a7"] = (210, 298),
            ["a8"] = (147, 210), ["a9"] = (105, 147), ["a10"] = (74, 105),
            ["b0"] = (2835, 4008), ["b1"] = (2004, 2835), ["b2"] = (1417, 2004), ["b3"] = (1001, 1417),
            ["b4"] = (709, 1001), ["b5"] = (499, 709), ["b6"] = (354, 499), ["b7"] = (249, 354),
            ["b8"] = (176, 249), ["b9"] = (125, 176), ["b10"] = (88, 125),
            ["c0"] = (2599, 3677), ["c1"] = (1837, 2599), ["c2"] = (1298, 1837), ["c3"] = (918, 1298),
            ["c4"] = (649, 918), ["c5"] = (459, 649), ["c6"] = (323, 459), ["c7"] = (230, 323),
            ["c8"] = (162, 230), ["c9"] = (113, 162), ["c10"] = (79, 113),
            ["letter"] = (612, 792), ["legal"] = (612, 1008), ["tabloid"] = (792, 1224), ["ledger"] = (1224, 792),
            ["executive"] = (522, 756), ["card-4x6"] = (288, 432), ["card-5x7"] = (360, 504),
        };

        // ─── TextPage helper ports (from extra.i / __init__.py) ──────────

        internal static bool SkipQuadCorrections = false;
        internal static bool SubsetFontnames = false;
        internal static bool SmallGlyphHeights = false;

        private const float FLT_EPSILON = 1.192092896e-07f;

        private const int TEXT_FONT_SUPERSCRIPT = 1;
        private const int TEXT_FONT_ITALIC = 2;
        private const int TEXT_FONT_SERIFED = 4;
        private const int TEXT_FONT_MONOSPACED = 8;
        private const int TEXT_FONT_BOLD = 16;

        internal static float JM_font_ascender(mupdf.fz_font font)
        {
            if (SkipQuadCorrections) return 0.8f;
            var wrapped = new mupdf.FzFont(mupdf.mupdf.ll_fz_keep_font(font));
            return wrapped.fz_font_ascender();
        }

        internal static float JM_font_descender(mupdf.fz_font font)
        {
            if (SkipQuadCorrections) return -0.2f;
            var wrapped = new mupdf.FzFont(mupdf.mupdf.ll_fz_keep_font(font));
            return wrapped.fz_font_descender();
        }

        internal static string JM_font_name(mupdf.fz_font font)
        {
            var wrapped = new mupdf.FzFont(mupdf.mupdf.ll_fz_keep_font(font));
            string name = wrapped.fz_font_name();
            int s = name.IndexOf('+');
            if (SubsetFontnames || s == -1 || s != 6)
                return name;
            return name.Substring(s + 1);
        }

        internal static int DetectSuperScript(mupdf.fz_stext_line line, mupdf.fz_stext_char ch)
        {
            if (line.wmode == 0 && line.dir.x == 1 && line.dir.y == 0)
            {
                return (ch.origin.y < line.first_char.origin.y - ch.size * 0.1f) ? 1 : 0;
            }
            return 0;
        }

        internal static int JM_char_font_flags(mupdf.fz_font font, mupdf.fz_stext_line line, mupdf.fz_stext_char ch)
        {
            var wrapped = new mupdf.FzFont(mupdf.mupdf.ll_fz_keep_font(font));
            int flags = DetectSuperScript(line, ch);
            flags += wrapped.fz_font_is_italic() * TEXT_FONT_ITALIC;
            flags += wrapped.fz_font_is_serif() * TEXT_FONT_SERIFED;
            flags += wrapped.fz_font_is_monospaced() * TEXT_FONT_MONOSPACED;
            flags += wrapped.fz_font_is_bold() * TEXT_FONT_BOLD;
            return flags;
        }

        internal static bool JM_rects_overlap(mupdf.FzRect a, mupdf.FzRect b)
        {
            if (a.x0 >= b.x1 || a.y0 >= b.y1 || a.x1 <= b.x0 || a.y1 <= b.y0)
                return false;
            return true;
        }

        internal static bool JM_rects_overlap(mupdf.fz_rect a, mupdf.fz_rect b)
        {
            if (a.x0 >= b.x1 || a.y0 >= b.y1 || a.x1 <= b.x0 || a.y1 <= b.y0)
                return false;
            return true;
        }

        internal static mupdf.FzQuad JM_char_quad(mupdf.fz_stext_line line, mupdf.fz_stext_char ch)
        {
            if (SkipQuadCorrections)
                return new mupdf.FzQuad(ch.quad);
            if (line.wmode != 0)
                return new mupdf.FzQuad(ch.quad);

            mupdf.fz_font font = ch.font;
            float asc = JM_font_ascender(font);
            float dsc = JM_font_descender(font);
            float fsize = ch.size;
            float asc_dsc = asc - dsc + FLT_EPSILON;

            if (asc_dsc >= 1 && !SmallGlyphHeights)
                return new mupdf.FzQuad(ch.quad);

            if (asc < 1e-3f)
            {
                dsc = -0.1f;
                asc = 0.9f;
                asc_dsc = 1.0f;
            }

            if (SmallGlyphHeights || asc_dsc < 1)
            {
                dsc = dsc / asc_dsc;
                asc = asc / asc_dsc;
            }
            asc_dsc = asc - dsc;
            asc = asc * fsize / asc_dsc;
            dsc = dsc * fsize / asc_dsc;

            float c = line.dir.x;
            float s = line.dir.y;
            var trm1 = mupdf.mupdf.fz_make_matrix(c, -s, s, c, 0, 0);
            var trm2 = mupdf.mupdf.fz_make_matrix(c, s, -s, c, 0, 0);
            if (c == -1)
            {
                trm1.d = 1;
                trm2.d = 1;
            }
            var xlate1 = mupdf.mupdf.fz_make_matrix(1, 0, 0, 1, -ch.origin.x, -ch.origin.y);
            var xlate2 = mupdf.mupdf.fz_make_matrix(1, 0, 0, 1, ch.origin.x, ch.origin.y);

            var quad = mupdf.mupdf.fz_transform_quad(new mupdf.FzQuad(ch.quad), xlate1);
            quad = mupdf.mupdf.fz_transform_quad(quad, trm1);

            if (c == 1 && quad.ul.y > 0)
            {
                quad.ul.y = asc; quad.ur.y = asc;
                quad.ll.y = dsc; quad.lr.y = dsc;
            }
            else
            {
                quad.ul.y = -asc; quad.ur.y = -asc;
                quad.ll.y = -dsc; quad.lr.y = -dsc;
            }

            if (quad.ll.x < 0)
            {
                quad.ll.x = 0;
                quad.ul.x = 0;
            }
            float cwidth = quad.lr.x - quad.ll.x;
            if (cwidth < FLT_EPSILON)
            {
                var wrappedFont = new mupdf.FzFont(mupdf.mupdf.ll_fz_keep_font(font));
                int glyph = mupdf.mupdf.fz_encode_character(wrappedFont, ch.c);
                if (glyph != 0)
                {
                    float fwidth = mupdf.mupdf.fz_advance_glyph(wrappedFont, glyph, line.wmode != 0 ? 1 : 0);
                    quad.lr.x = quad.ll.x + fwidth * fsize;
                    quad.ur.x = quad.lr.x;
                }
            }

            quad = mupdf.mupdf.fz_transform_quad(quad, trm2);
            quad = mupdf.mupdf.fz_transform_quad(quad, xlate2);
            return quad;
        }

        internal static mupdf.FzRect JM_char_bbox(mupdf.fz_stext_line line, mupdf.fz_stext_char ch)
        {
            var q = JM_char_quad(line, ch);
            var r = mupdf.mupdf.fz_rect_from_quad(q);
            if (line.wmode == 0)
                return r;
            if (r.y1 < r.y0 + ch.size)
                r.y0 = r.y1 - ch.size;
            return r;
        }

        /// <summary>Python <c>JM_get_annot_xref_list</c> (<c>src/__init__.py</c>) for a page dictionary.</summary>
        internal static List<(int xref, int type_, string nm)> JM_get_annot_xref_list(mupdf.PdfObj pageObj)
        {
            var names = new List<(int, int, string)>();
            if (pageObj.m_internal == null)
                return names;
            var annots = mupdf.mupdf.pdf_dict_get(pageObj, mupdf.mupdf.pdf_new_name("Annots"));
            if (annots.m_internal == null || mupdf.mupdf.pdf_is_array(annots) == 0)
                return names;
            int n = mupdf.mupdf.pdf_array_len(annots);
            for (int i = 0; i < n; i++)
            {
                var annot_obj = mupdf.mupdf.pdf_array_get(annots, i);
                int xref = mupdf.mupdf.pdf_to_num(annot_obj);
                var subtype = mupdf.mupdf.pdf_dict_get(annot_obj, mupdf.mupdf.pdf_new_name("Subtype"));
                if (subtype.m_internal == null)
                    continue;
                var typeEnum = mupdf.mupdf.pdf_annot_type_from_string(mupdf.mupdf.pdf_to_name(subtype));
                if (typeEnum == mupdf.pdf_annot_type.PDF_ANNOT_UNKNOWN)
                    continue;
                var idObj = mupdf.mupdf.pdf_dict_gets(annot_obj, "NM");
                string nm = idObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(idObj) : "";
                names.Add((xref, (int)typeEnum, nm));
            }
            return names;
        }

        /// <summary>Read <c>/NM</c> for an indirect object (used for link dict <c>id</c> like PyMuPDF <c>get_links</c>).</summary>
        internal static string PdfAnnotNmForXref(mupdf.PdfDocument pdf, int xref)
        {
            if (pdf == null || pdf.m_internal == null || xref < 1)
                return "";
            try
            {
                var obj = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
                var resolved = mupdf.mupdf.pdf_resolve_indirect(obj);
                var idObj = mupdf.mupdf.pdf_dict_gets(resolved, "NM");
                if (idObj.m_internal == null)
                    return "";
                return mupdf.mupdf.pdf_to_text_string(idObj);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>Python <c>JM_refresh_links</c> (<c>src/__init__.py</c>).</summary>
        internal static void JM_refresh_links(mupdf.PdfDocument pdf, mupdf.PdfPage pdfPage)
        {
            if (pdf == null || pdf.m_internal == null || pdfPage == null || pdfPage.m_internal == null)
                return;
            var obj = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Annots"));
            if (obj.m_internal == null)
                return;

            var pageMediabox = new mupdf.FzRect();
            var pageCtm = new mupdf.FzMatrix();
            pdfPage.pdf_page_transform(pageMediabox, pageCtm);
            int number = mupdf.mupdf.pdf_lookup_page_number(pdf, pdfPage.obj());
            var loaded = mupdf.mupdf.pdf_load_link_annots(pdf, pdfPage, obj, number, pageCtm);
            var pageStruct = pdfPage.m_internal;
            if (loaded == null || loaded.m_internal == null)
            {
                pageStruct.links = null;
                return;
            }
            var kept = mupdf.mupdf.ll_fz_keep_link(loaded.m_internal);
            pageStruct.links = kept;
        }

        /// <summary>Space-separated PDF real formatting (Python <c>_format_g</c> tuple behavior, <c>%g</c>-style).</summary>
        internal static string FormatPdfReals(params double[] values)
        {
            if (values == null || values.Length == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(values[i].ToString("G9", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        internal static bool TryCoerceRect(object o, out Rect r)
        {
            r = default;
            if (o == null) return false;
            if (o is Rect rr) { r = rr; return true; }
            if (o is IRect ir) { r = ir.ToRect(); return true; }
            if (o is IList list && list.Count >= 4)
            {
                try
                {
                    r = new Rect(Convert.ToDouble(list[0], CultureInfo.InvariantCulture),
                        Convert.ToDouble(list[1], CultureInfo.InvariantCulture),
                        Convert.ToDouble(list[2], CultureInfo.InvariantCulture),
                        Convert.ToDouble(list[3], CultureInfo.InvariantCulture));
                    return true;
                }
                catch { return false; }
            }
            return false;
        }

        internal static bool TryCoercePoint(object o, out Point p)
        {
            p = default;
            if (o == null) return false;
            if (o is Point pp) { p = pp; return true; }
            if (o is IList list && list.Count >= 2)
            {
                try
                {
                    p = new Point(Convert.ToDouble(list[0], CultureInfo.InvariantCulture),
                        Convert.ToDouble(list[1], CultureInfo.InvariantCulture));
                    return true;
                }
                catch { return false; }
            }
            return false;
        }

        /// <summary>Shallow copy of <paramref name="lnk"/> without keys only used by <see cref="EnrichLinkDictFromPdfAnnot"/> for inspection.</summary>
        internal static Dictionary<string, object> CopyLinkDictForBuild(Dictionary<string, object> lnk)
        {
            if (lnk == null) return null;
            if (!lnk.ContainsKey("named_action") && !lnk.ContainsKey("is_viewer_named_action"))
                return lnk;
            var l = new Dictionary<string, object>(lnk);
            l.Remove("named_action");
            l.Remove("is_viewer_named_action");
            return l;
        }

        /// <summary>Python <c>utils.getLinkText</c>: build a link annotation dictionary stream (with <c>/NM</c>) for <c>Page.insert_link</c>.</summary>
        internal static bool TryBuildInsertLinkAnnotObjectString(Page page, Dictionary<string, object> lnk, out string dictionarySource)
        {
            dictionarySource = null;
            if (page == null || lnk == null) return false;
            lnk = CopyLinkDictForBuild(lnk);
            if (!lnk.TryGetValue("kind", out var kindObj)) return false;
            int kind = Convert.ToInt32(kindObj, CultureInfo.InvariantCulture);
            if (kind == Constants.LINK_NONE) return false;
            if (!TryCoerceRect(lnk.TryGetValue("from", out var fromO) ? fromO : null, out var fromRect))
                return false;

            var inv = page.TransformationMatrix.Inverted() ?? Matrix.Identity;
            var rPdf = fromRect.Transform(inv);
            string rectStr = FormatPdfReals(rPdf.X0, rPdf.Y0, rPdf.X1, rPdf.Y1);

            var doc = page.RequireParent();
            var pdfPage = page.NativePdfPage;
            var pdf = doc.NativePdfDocument;

            var linkXrefToNm = new Dictionary<int, string>();
            foreach (var t in JM_get_annot_xref_list(pdfPage.obj()))
            {
                if (t.type_ == (int)mupdf.pdf_annot_type.PDF_ANNOT_LINK)
                    linkXrefToNm[t.xref] = t.nm ?? "";
            }

            var usedNm = new HashSet<string>(StringComparer.Ordinal);
            foreach (var existingNm in linkXrefToNm.Values)
            {
                if (!string.IsNullOrEmpty(existingNm))
                    usedNm.Add(existingNm);
            }

            string oldId = "";
            if (lnk.TryGetValue("id", out var idObj))
                oldId = idObj as string ?? idObj?.ToString() ?? "";
            int lnkXref = 0;
            if (lnk.TryGetValue("xref", out var xrefObj))
                lnkXref = Convert.ToInt32(xrefObj, CultureInfo.InvariantCulture);
            string annotNm;
            if (!string.IsNullOrEmpty(oldId) && lnkXref > 0
                && linkXrefToNm.TryGetValue(lnkXref, out var curNm) && curNm == oldId)
                annotNm = oldId;
            else
            {
                annotNm = null;
                for (int i = 0; i < 1_000_000; i++)
                {
                    var candidate = $"{Constants.JM_ANNOT_ID_STEM}-L{i}";
                    if (!usedNm.Contains(candidate))
                    {
                        annotNm = candidate;
                        break;
                    }
                }
                if (annotNm == null) return false;
            }

            string annot;
            switch (kind)
            {
                case Constants.LINK_GOTO:
                    if (!lnk.TryGetValue("page", out var pageO)) return false;
                    int pno = Convert.ToInt32(pageO, CultureInfo.InvariantCulture);
                    if (pno >= 0)
                    {
                        int pageXref = doc.PageXref(pno);
                        var destPage = doc[pno];
                        var destInv = destPage.TransformationMatrix.Inverted() ?? Matrix.Identity;
                        var toPt = new Point(0, 0);
                        if (lnk.TryGetValue("to", out var toO))
                            TryCoercePoint(toO, out toPt);
                        toPt = new Point(toPt).Transform(destInv);
                        double zoom = 0;
                        if (lnk.TryGetValue("zoom", out var zO))
                            zoom = Convert.ToDouble(zO, CultureInfo.InvariantCulture);
                        annot = "<</A<</S/GoTo/D[" + pageXref.ToString(CultureInfo.InvariantCulture) + " 0 R/XYZ "
                            + FormatPdfReals(toPt.X, toPt.Y, zoom) + "]>>/Rect[" + rectStr + "]/BS<</W 0>>/Subtype/Link>>";
                    }
                    else
                    {
                        if (!lnk.TryGetValue("to", out var toNamed))
                            return false;
                        var toStr = toNamed is string s ? GetPdfStr(s) : GetPdfStr(toNamed?.ToString() ?? "");
                        annot = "<</A<</S/GoTo/D" + toStr + ">>/Rect[" + rectStr + "]/BS<</W 0>>/Subtype/Link>>";
                    }
                    break;

                case Constants.LINK_URI:
                    if (!lnk.TryGetValue("uri", out var uriO) || uriO == null) return false;
                    var uriStr = uriO.ToString() ?? "";
                    if (string.IsNullOrEmpty(uriStr)) return false;
                    annot = "<</A<</S/URI/URI" + GetPdfStr(uriStr) + ">>/Rect[" + rectStr + "]/BS<</W 0>>/Subtype/Link>>";
                    break;

                case Constants.LINK_LAUNCH:
                    if (!lnk.TryGetValue("file", out var launchFile) || launchFile == null) return false;
                    var lf = GetPdfStr(launchFile.ToString() ?? "");
                    annot = "<</A<</S/Launch/F<</F" + lf + "/UF" + lf + "/Type/Filespec>>>>/Rect[" + rectStr + "]/BS<</W 0>>/Subtype/Link>>";
                    break;

                case Constants.LINK_GOTOR:
                    if (!lnk.TryGetValue("file", out var gf) || gf == null) return false;
                    var fspec = GetPdfStr(gf.ToString() ?? "");
                    if (!lnk.TryGetValue("page", out var gpO)) return false;
                    int gp = Convert.ToInt32(gpO, CultureInfo.InvariantCulture);
                    if (gp >= 0)
                    {
                        var toG = new Point(0, 0);
                        if (lnk.TryGetValue("to", out var toGo))
                            TryCoercePoint(toGo, out toG);
                        double zg = 0;
                        if (lnk.TryGetValue("zoom", out var zgO))
                            zg = Convert.ToDouble(zgO, CultureInfo.InvariantCulture);
                        annot = "<</A<</S/GoToR/D[" + gp.ToString(CultureInfo.InvariantCulture) + " /XYZ "
                            + FormatPdfReals(toG.X, toG.Y, zg) + "]/F<</F" + fspec + "/UF" + fspec + "/Type/Filespec>>>>/Rect["
                            + rectStr + "]/BS<</W 0>>/Subtype/Link>>";
                    }
                    else
                    {
                        if (!lnk.TryGetValue("to", out var toR)) return false;
                        var dPart = toR is string rs ? GetPdfStr(rs) : GetPdfStr(toR?.ToString() ?? "");
                        annot = "<</A<</S/GoToR/D" + dPart + "/F" + fspec + ">>/Rect[" + rectStr + "]/BS<</W 0>>/Subtype/Link>>";
                    }
                    break;

                case Constants.LINK_NAMED:
                    string lname = null;
                    if (lnk.TryGetValue("name", out var nameO) && nameO != null)
                        lname = nameO.ToString();
                    if (string.IsNullOrEmpty(lname) && lnk.TryGetValue("nameddest", out var ndO) && ndO != null)
                        lname = ndO.ToString();
                    if (string.IsNullOrEmpty(lname)) return false;
                    annot = "<</A<</S/GoTo/D" + GetPdfStr(lname) + "/Type/Action>>/Rect[" + rectStr + "]/BS<</W 0>>/Subtype/Link>>";
                    break;

                default:
                    return false;
            }

            dictionarySource = annot.Replace("/Link", "/Link/NM(" + annotNm + ")");
            return true;
        }

        static string ExtractFilespecPath(mupdf.PdfObj f)
        {
            if (f.m_internal == null) return "";
            var fs = mupdf.mupdf.pdf_resolve_indirect(f);
            if (fs.m_internal == null) return "";
            var f1 = mupdf.mupdf.pdf_dict_get(fs, mupdf.mupdf.pdf_new_name("F"));
            if (f1.m_internal != null)
                return mupdf.mupdf.pdf_to_text_string(f1);
            var uf = mupdf.mupdf.pdf_dict_get(fs, mupdf.mupdf.pdf_new_name("UF"));
            return uf.m_internal != null ? mupdf.mupdf.pdf_to_text_string(uf) : "";
        }

        static bool ApplyGoToDestDict(mupdf.PdfDocument pdf, Dictionary<string, object> d, mupdf.PdfObj dx)
        {
            if (dx.m_internal == null) return false;
            var r = mupdf.mupdf.pdf_resolve_indirect(dx);
            if (mupdf.mupdf.pdf_is_array(r) == 0) return false;
            int len = mupdf.mupdf.pdf_array_len(r);
            if (len < 1) return false;
            var pageObj = mupdf.mupdf.pdf_array_get(r, 0);
            int pno = mupdf.mupdf.pdf_lookup_page_number(pdf, pageObj);
            d["page"] = pno;
            d["to"] = new Point(0, 0);
            d["zoom"] = 0.0;
            if (len >= 2)
            {
                var m0 = mupdf.mupdf.pdf_array_get(r, 1);
                if (mupdf.mupdf.pdf_is_name(m0) != 0)
                {
                    var mode = mupdf.mupdf.pdf_to_name(m0);
                    if (mode == "XYZ" && len >= 5)
                    {
                        d["to"] = new Point(
                            mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(r, 2)),
                            mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(r, 3)));
                        d["zoom"] = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(r, 4));
                    }
                    else if (mode == "Fit" || mode == "FitB")
                    {
                        // whole page; leave to (0,0), zoom 0
                    }
                    else if ((mode == "FitH" || mode == "FitBH") && len >= 3)
                    {
                        double top = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(r, 2));
                        d["to"] = new Point(0, top);
                    }
                    else if ((mode == "FitV" || mode == "FitBV") && len >= 3)
                    {
                        double left = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(r, 2));
                        d["to"] = new Point(left, 0);
                    }
                    else if (mode == "FitR" && len >= 6)
                    {
                        double left = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(r, 2));
                        double top = mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(r, 5));
                        d["to"] = new Point(left, top);
                    }
                }
            }
            return true;
        }

        static void ApplyGoToRDestDict(mupdf.PdfDocument pdf, Dictionary<string, object> d, mupdf.PdfObj dx)
        {
            if (dx.m_internal == null) return;
            for (int depth = 0; depth < 8; depth++)
            {
                var r = mupdf.mupdf.pdf_resolve_indirect(dx);
                if (mupdf.mupdf.pdf_is_array(r) != 0)
                {
                    ApplyGoToDestDict(pdf, d, dx);
                    return;
                }
                if (mupdf.mupdf.pdf_is_name(r) != 0 || mupdf.mupdf.pdf_is_string(r) != 0)
                {
                    d["page"] = -1;
                    d["to"] = mupdf.mupdf.pdf_is_name(r) != 0 ? mupdf.mupdf.pdf_to_name(r) : mupdf.mupdf.pdf_to_text_string(r);
                    return;
                }
                if (mupdf.mupdf.pdf_is_dict(r) == 0) return;
                var inner = mupdf.mupdf.pdf_dict_get(r, mupdf.mupdf.pdf_new_name("D"));
                if (inner.m_internal == null) return;
                dx = inner;
            }
        }

        /// <summary>Resolve <c>/GoTo</c> <c>/D</c>: explicit array, named dest, or destination dict with nested <c>/D</c>.</summary>
        static void ApplyGoToDestinationOperand(mupdf.PdfDocument pdf, Dictionary<string, object> d, mupdf.PdfObj dx)
        {
            if (dx.m_internal == null) return;
            for (int depth = 0; depth < 8; depth++)
            {
                var rdx = mupdf.mupdf.pdf_resolve_indirect(dx);
                if (mupdf.mupdf.pdf_is_array(rdx) != 0)
                {
                    if (ApplyGoToDestDict(pdf, d, dx))
                        d["kind"] = Constants.LINK_GOTO;
                    return;
                }
                if (mupdf.mupdf.pdf_is_name(rdx) != 0 || mupdf.mupdf.pdf_is_string(rdx) != 0)
                {
                    d["kind"] = Constants.LINK_NAMED;
                    d["page"] = -1;
                    d["name"] = mupdf.mupdf.pdf_is_name(rdx) != 0
                        ? mupdf.mupdf.pdf_to_name(rdx)
                        : mupdf.mupdf.pdf_to_text_string(rdx);
                    return;
                }
                if (mupdf.mupdf.pdf_is_dict(rdx) == 0) return;
                var inner = mupdf.mupdf.pdf_dict_get(rdx, mupdf.mupdf.pdf_new_name("D"));
                if (inner.m_internal == null) return;
                dx = inner;
            }
        }

        /// <summary>Best-effort port of <c>utils.getLinkDict</c> fields from the PDF link annotation (<c>/A</c>, <c>/Dest</c>).</summary>
        internal static void EnrichLinkDictFromPdfAnnot(mupdf.PdfDocument pdf, int xref, Dictionary<string, object> d)
        {
            if (pdf == null || pdf.m_internal == null || xref < 1 || d == null) return;
            try
            {
                var ind = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
                var annot = mupdf.mupdf.pdf_resolve_indirect(ind);
                if (annot.m_internal == null) return;
                var a = mupdf.mupdf.pdf_dict_get(annot, mupdf.mupdf.pdf_new_name("A"));
                var destKey = mupdf.mupdf.pdf_dict_get(annot, mupdf.mupdf.pdf_new_name("Dest"));

                if (a.m_internal != null)
                {
                    var sObj = mupdf.mupdf.pdf_dict_get(a, mupdf.mupdf.pdf_new_name("S"));
                    if (sObj.m_internal == null) return;
                    var s = mupdf.mupdf.pdf_to_name(sObj);
                    if (s == "URI")
                    {
                        var uriObj = mupdf.mupdf.pdf_dict_get(a, mupdf.mupdf.pdf_new_name("URI"));
                        d["kind"] = Constants.LINK_URI;
                        d["uri"] = uriObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(uriObj) : "";
                        d["page"] = -1;
                        return;
                    }
                    if (s == "Launch")
                    {
                        var f = mupdf.mupdf.pdf_dict_get(a, mupdf.mupdf.pdf_new_name("F"));
                        d["kind"] = Constants.LINK_LAUNCH;
                        d["file"] = (ExtractFilespecPath(f) ?? "").Replace("\\", "/");
                        d["page"] = -1;
                        return;
                    }
                    if (s == "GoToR")
                    {
                        d["kind"] = Constants.LINK_GOTOR;
                        var f = mupdf.mupdf.pdf_dict_get(a, mupdf.mupdf.pdf_new_name("F"));
                        d["file"] = (ExtractFilespecPath(f) ?? "").Replace("\\", "/");
                        var dx = mupdf.mupdf.pdf_dict_get(a, mupdf.mupdf.pdf_new_name("D"));
                        ApplyGoToRDestDict(pdf, d, dx);
                        return;
                    }
                    if (s == "GoTo")
                    {
                        var dx = mupdf.mupdf.pdf_dict_get(a, mupdf.mupdf.pdf_new_name("D"));
                        ApplyGoToDestinationOperand(pdf, d, dx);
                        return;
                    }
                    if (s == "JavaScript")
                    {
                        d["kind"] = Constants.LINK_NONE;
                        d["page"] = -1;
                        return;
                    }
                    if (s == "GoToE")
                    {
                        d["kind"] = Constants.LINK_NONE;
                        d["page"] = -1;
                        var fE = mupdf.mupdf.pdf_dict_get(a, mupdf.mupdf.pdf_new_name("F"));
                        if (fE.m_internal != null)
                            d["file"] = (ExtractFilespecPath(fE) ?? "").Replace("\\", "/");
                        return;
                    }
                    if (s == "Named")
                    {
                        var nObj = mupdf.mupdf.pdf_dict_get(a, mupdf.mupdf.pdf_new_name("N"));
                        if (nObj.m_internal == null)
                        {
                            d["kind"] = Constants.LINK_NONE;
                            d["page"] = -1;
                            return;
                        }
                        string nStr = mupdf.mupdf.pdf_is_name(nObj) != 0
                            ? mupdf.mupdf.pdf_to_name(nObj)
                            : mupdf.mupdf.pdf_to_text_string(nObj);
                        d["kind"] = Constants.LINK_NAMED;
                        d["page"] = -1;
                        d["name"] = nStr;
                        d["named_action"] = nStr;
                        d["is_viewer_named_action"] = true;
                        return;
                    }
                    if (s == "ResetForm")
                    {
                        d["kind"] = Constants.LINK_NONE;
                        d["page"] = -1;
                        return;
                    }
                    if (s == "SubmitForm")
                    {
                        d["kind"] = Constants.LINK_NONE;
                        d["page"] = -1;
                        var fSf = mupdf.mupdf.pdf_dict_get(a, mupdf.mupdf.pdf_new_name("F"));
                        if (fSf.m_internal != null)
                            d["file"] = (ExtractFilespecPath(fSf) ?? "").Replace("\\", "/");
                        return;
                    }
                    if (s == "Thread")
                    {
                        d["kind"] = Constants.LINK_NONE;
                        d["page"] = -1;
                        return;
                    }
                    return;
                }

                if (destKey.m_internal != null)
                {
                    if (ApplyGoToDestDict(pdf, d, destKey))
                        d["kind"] = Constants.LINK_GOTO;
                    else
                    {
                        var rd = mupdf.mupdf.pdf_resolve_indirect(destKey);
                        if (mupdf.mupdf.pdf_is_name(rd) != 0 || mupdf.mupdf.pdf_is_string(rd) != 0)
                        {
                            d["kind"] = Constants.LINK_NAMED;
                            d["page"] = -1;
                            d["name"] = mupdf.mupdf.pdf_is_name(rd) != 0
                                ? mupdf.mupdf.pdf_to_name(rd)
                                : mupdf.mupdf.pdf_to_text_string(rd);
                        }
                    }
                }
            }
            catch
            {
                // keep fz-derived keys in <paramref name="d"/>
            }
        }

        /// <summary>Parse a PDF dictionary source string and append it to the page <c>/Annots</c> array (Python <c>Page._addAnnot_FromString</c> single-item path).</summary>
        internal static void AppendPdfAnnotFromObjectString(Page page, string text)
        {
            if (page == null || string.IsNullOrWhiteSpace(text)) return;
            var pdfPage = page.NativePdfPage;
            var pdf = page.RequireParent().NativePdfDocument;
            var annots = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Annots"));
            if (annots.m_internal == null)
            {
                mupdf.mupdf.pdf_dict_put_array(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Annots"), 1);
                annots = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Annots"));
            }

            var parsed = JM_pdf_obj_from_str(pdf, text);
            var annot = mupdf.mupdf.pdf_add_object(pdf, parsed);
            var indObj = mupdf.mupdf.pdf_new_indirect(pdf, mupdf.mupdf.pdf_to_num(annot), 0);
            mupdf.mupdf.pdf_array_push(annots, indObj);
        }
    }
}
