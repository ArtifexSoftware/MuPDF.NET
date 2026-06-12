using mupdf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MuPDF.NET
{
    /// <summary>
    /// Internal helper/utility methods for type conversions and common operations.
    /// </summary>
    internal static class Helpers
    {
        /// <summary>Thread-safe list storing MuPDF warning messages.</summary>
        internal static readonly List<string> JM_mupdf_warnings_store = new List<string>();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void MuPdfWarningCallback(IntPtr user, IntPtr message);

        private static readonly MuPdfWarningCallback _muPdfWarningCallback = JmMupdfWarning;
        private static readonly MuPdfWarningCallback _muPdfErrorCallback = JmMupdfError;
        private static bool _mupdfWarningsHooked;

        static Helpers()
        {
            EnsureMupdfWarningsHooked();
        }

        /// <summary>UTF-8 C string from MuPDF callbacks (net472/net48 lack <c>Marshal.PtrToStringUTF8</c>).</summary>
        private static string PtrToStringUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return string.Empty;
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0)
                len++;
            if (len == 0)
                return string.Empty;
            var bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>Redirects MuPDF library warnings into the warning store.</summary>
        private static void JmMupdfWarning(IntPtr user, IntPtr message)
        {
            string text = PtrToStringUtf8(message);
            if (!string.IsNullOrEmpty(text))
            {
                lock (JM_mupdf_warnings_store)
                    JM_mupdf_warnings_store.Add(text);
            }
        }

        /// <summary>MuPDF error callback handler.</summary>
        private static void JmMupdfError(IntPtr user, IntPtr message)
        {
            string text = PtrToStringUtf8(message);
            if (!string.IsNullOrEmpty(text))
            {
                lock (JM_mupdf_warnings_store)
                    JM_mupdf_warnings_store.Add(text);
            }
        }

        internal static void EnsureMupdfWarningsHooked()
        {
            if (_mupdfWarningsHooked)
                return;
            _mupdfWarningsHooked = true;
            IntPtr warnFn = Marshal.GetFunctionPointerForDelegate(_muPdfWarningCallback);
            mupdf.mupdf.fz_set_warning_callback(
                new SWIGTYPE_p_f_p_void_p_q_const__char__void(warnFn, false),
                new SWIGTYPE_p_void(IntPtr.Zero, false));
            IntPtr errFn = Marshal.GetFunctionPointerForDelegate(_muPdfErrorCallback);
            mupdf.mupdf.fz_set_error_callback(
                new SWIGTYPE_p_f_p_void_p_q_const__char__void(errFn, false),
                new SWIGTYPE_p_void(IntPtr.Zero, false));
        }

        /// <summary>Returns MuPDF's infinite rectangle constant.</summary>
        internal static Rect INFINITE_RECT()
            => Rect.Infinite;

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
                current = PdfDictGet(current, key);
            }
            return current;
        }

        /// <summary>Like <see cref="PdfDictGetl"/> but keys are PDF names; temporary key objects are dropped after the walk.</summary>
        internal static mupdf.PdfObj PdfDictGetl(mupdf.PdfObj dict, params string[] keys)
        {
            if (dict?.m_internal == null || keys == null || keys.Length == 0)
                return new mupdf.PdfObj();
            var keyObjs = new mupdf.PdfObj[keys.Length];
            try
            {
                for (int i = 0; i < keys.Length; i++)
                    keyObjs[i] = mupdf.mupdf.pdf_new_name(keys[i]);
                return PdfDictGetl(dict, keyObjs);
            }
            finally
            {
                for (int i = 0; i < keyObjs.Length; i++)
                    keyObjs[i]?.Dispose();
            }
        }

        /// <summary>MuPDF <c>pdf_dict_putl</c>: set a nested dict path, creating intermediate dicts.</summary>
        internal static void PdfDictPutl(mupdf.PdfDocument pdf, mupdf.PdfObj dict, mupdf.PdfObj leafVal, params mupdf.PdfObj[] keys)
        {
            if (dict?.m_internal == null || keys == null || keys.Length == 0)
                return;
            if (keys.Length == 1)
            {
                PdfDictPut(dict, keys[0], leafVal);
                return;
            }
            mupdf.PdfObj current = dict;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                var next = PdfDictGet(current, keys[i]);
                if (next.m_internal == null)
                {
                    next = mupdf.mupdf.pdf_new_dict(pdf, 2);
                    PdfDictPut(current, keys[i], next);
                }
                current = next;
            }
            PdfDictPut(current, keys[keys.Length - 1], leafVal);
        }

        internal static void PdfDictPutl(mupdf.PdfDocument pdf, mupdf.PdfObj dict, mupdf.PdfObj leafVal, params string[] keys)
        {
            if (keys == null || keys.Length == 0)
                return;
            var keyObjs = new mupdf.PdfObj[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                keyObjs[i] = mupdf.mupdf.pdf_new_name(keys[i]);
            PdfDictPutl(pdf, dict, leafVal, keyObjs);
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

        /// <summary>Validates and converts a rectangle argument.</summary>
        internal static bool CheckRect(object? r)
        {
            try
            {
                var rect = RectFromPy(r);
                return !rect.IsEmpty && !rect.IsInfinite;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Validates and converts a quad argument.</summary>
        internal static bool CheckQuad(object? q)
        {
            try
            {
                if (q is Quad quad)
                    return quad.IsConvex && !quad.IsEmpty && !quad.IsInfinite;
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Validates text-marker annotation arguments.</summary>
        internal static IReadOnlyList<object> CheckMarkerArg(object quads)
        {
            if (CheckRect(quads))
                return new object[] { RectFromPy(quads) };
            if (CheckQuad(quads))
                return new object[] { quads };
            if (quads is string)
                throw new ValueErrorException("bad quads entry");
            if (quads is IEnumerable enumerable)
            {
                var list = new List<object>();
                foreach (var item in enumerable)
                {
                    if (!CheckRect(item) && !CheckQuad(item))
                        throw new ValueErrorException("bad quads entry");
                    list.Add(CheckRect(item) ? RectFromPy(item) : item!);
                }
                return list;
            }
            throw new ValueErrorException("bad quads entry");
        }

        /// <summary>Converts rectangle, quad, or four-float sequences to <see cref="Quad"/>.</summary>
        internal static mupdf.FzQuad QuadFromPy(object item)
        {
            if (item is mupdf.FzQuad fz)
                return fz;
            if (item is Quad q)
                return q.ToFzQuad();
            if (item is Rect r)
                return mupdf.mupdf.fz_quad_from_rect(r.ToFzRect());
            if (item is IRect ir)
                return mupdf.mupdf.fz_quad_from_rect(new Rect(ir).ToFzRect());
            if (item is float[] fa && fa.Length == 4)
                return mupdf.mupdf.fz_quad_from_rect(new mupdf.FzRect(fa[0], fa[1], fa[2], fa[3]));
            if (item is float[] da && da.Length == 4)
                return mupdf.mupdf.fz_quad_from_rect(new mupdf.FzRect((float)da[0], (float)da[1], (float)da[2], (float)da[3]));
            return new mupdf.FzQuad();
        }

        internal static Rect RectFromPy(object? r)
        {
            if (r is Rect rect)
                return rect;
            if (r is IRect irect)
                return new Rect(irect);
            if (r is mupdf.FzRect fz)
                return new Rect(fz);
            if (r is float[] fa && fa.Length == 4)
                return new Rect(fa[0], fa[1], fa[2], fa[3]);
            if (r is float[] da && da.Length == 4)
                return new Rect(da[0], da[1], da[2], da[3]);
            if (r is IEnumerable seq && r is not string)
            {
                var vals = new List<float>();
                foreach (var v in seq)
                {
                    if (v is float f) vals.Add(f);
                    else if (v is float d) vals.Add(d);
                    else if (v is int i) vals.Add(i);
                    else throw new ArgumentException("invalid rect-like sequence");
                }
                if (vals.Count != 4)
                    throw new ArgumentException("invalid rect-like sequence");
                return new Rect(vals[0], vals[1], vals[2], vals[3]);
            }
            throw new ArgumentException("invalid rect-like");
        }

        /// <summary>Builds highlight quads for a text selection.</summary>
        internal static List<Rect> GetHighlightSelection(Page page, Point? start = null, Point? stop = null, IRect? clip = null)
        {
            Rect clipRect = clip == null ? page.Rect : new Rect(clip);
            Point startPt = start ?? clipRect.TopLeft;
            Point stopPt = stop ?? clipRect.BottomRight;
            clipRect = new Rect(clipRect.X0, startPt.Y, clipRect.X1, stopPt.Y);
            if (clipRect.IsEmpty || clipRect.IsInfinite)
                return new List<Rect>();

            var pageDict = Utils.GetText(page, "dict", clip: new IRect(clipRect), flags: 0) as Dictionary<string, object>;
            var lines = new List<Rect>();
            if (pageDict == null || !pageDict.TryGetValue("blocks", out var blocksObj)
                || blocksObj is not List<Dictionary<string, object>> blocks)
            {
                return lines;
            }

            foreach (var block in blocks)
            {
                if (!block.TryGetValue("lines", out var linesObj) || linesObj is not List<Dictionary<string, object>> blockLines)
                    continue;
                foreach (var line in blockLines)
                {
                    if (!line.TryGetValue("bbox", out var bboxObj) || bboxObj is not float[] bb || bb.Length != 4)
                        continue;
                    var bbox = new Rect(bb[0], bb[1], bb[2], bb[3]);
                    if (bbox.IsInfinite || bbox.IsEmpty)
                        continue;
                    lines.Add(bbox);
                }
            }

            if (lines.Count == 0)
                return lines;

            lines.Sort((a, b) => a.Y1.CompareTo(b.Y1));

            var bboxf = lines[0];
            lines.RemoveAt(0);
            if (bboxf.Y0 - startPt.Y <= 0.1 * bboxf.Height)
            {
                var r = new Rect(startPt.X, bboxf.Y0, bboxf.BottomRight);
                if (!r.IsEmpty && !r.IsInfinite)
                    lines.Insert(0, r);
            }
            else
            {
                lines.Insert(0, bboxf);
            }

            if (lines.Count == 0)
                return lines;

            var bboxl = lines[lines.Count - 1];
            lines.RemoveAt(lines.Count - 1);
            if (stopPt.Y - bboxl.Y1 <= 0.1 * bboxl.Height)
            {
                var r = new Rect(bboxl.TopLeft, stopPt.X, bboxl.Y1);
                if (!r.IsEmpty && !r.IsInfinite)
                    lines.Add(r);
            }
            else
            {
                lines.Add(bboxl);
            }

            return lines;
        }

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

        /// <summary>Emits user-facing warning messages.</summary>
        internal static void message(string text = "")
        {
            if (!string.IsNullOrEmpty(text))
                Console.WriteLine(text);
        }

        /// <summary>Start-info for <c>python -c …</c> (compatible with .NET Framework and .NET Standard 2.0).</summary>
        internal static ProcessStartInfo CreatePythonProcessStartInfo(string script, params string[] extraArgs)
        {
            string python = Environment.GetEnvironmentVariable("PYTHON") ?? "python";
            var psi = new ProcessStartInfo(python)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            var sb = new StringBuilder();
            sb.Append("-c ").Append(QuoteProcessArgument(script));
            if (extraArgs != null)
            {
                foreach (string arg in extraArgs)
                {
                    if (arg != null)
                        sb.Append(' ').Append(QuoteProcessArgument(arg));
                }
            }
            psi.Arguments = sb.ToString();
            return psi;
        }

        static string QuoteProcessArgument(string arg) =>
            "\"" + (arg ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        /// <summary>Resolves a MuPDF <c>fz_font</c> from font parameters.</summary>
        internal static mupdf.FzFont JM_get_font(
            string fontname,
            string fontfile,
            byte[] fontbuffer,
            int script,
            int lang,
            int ordering,
            int is_bold,
            int is_italic,
            int is_serif,
            int embed)
        {
            mupdf.FzFont fertig(mupdf.FzFont font)
            {
                if (font.m_internal == null)
                    throw new Exception(Constants.MSG_FONT_FAILED);
                // if font allows this, set embedding
                var flags = mupdf.mupdf.ll_fz_font_flags(font.m_internal);
                if (flags != null && flags.never_embed == 0)
                    font.fz_set_font_embedding(embed);
                return font;
            }

            int index = 0;
            mupdf.FzFont font = null;
            if (fontfile != null)
            {
                font = mupdf.mupdf.fz_new_font_from_file(null, fontfile, index, 0);
                return fertig(font);
            }

            if (fontbuffer != null && fontbuffer.Length > 0)
            {
                mupdf.FzBuffer res = BufferFromBytes(fontbuffer);
                font = mupdf.mupdf.fz_new_font_from_buffer(null, res, index, 0);
                return fertig(font);
            }

            if (ordering > -1)
            {
                font = mupdf.mupdf.fz_new_cjk_font(ordering);
                return fertig(font);
            }

            if (fontname != null)
            {
                // Base-14 or a MuPDF builtin font
                font = mupdf.mupdf.fz_new_base14_font(fontname);
                if (font.m_internal != null)
                    return fertig(font);
                font = mupdf.mupdf.fz_new_builtin_font(fontname, is_bold, is_italic);
                return fertig(font);
            }

            // Check for NOTO font
            using (var notoOut = new mupdf.ll_fz_lookup_noto_font_outparams())
            {
                var data = mupdf.mupdf.ll_fz_lookup_noto_font_outparams_fn(script, lang, notoOut);
                int size = notoOut.len;
                index = notoOut.subfont;
                if (data != null && size > 0)
                    font = mupdf.mupdf.fz_new_font_from_memory(null, data, size, index, 0);
            }
            if (font != null && font.m_internal != null)
                return fertig(font);
            font = mupdf.mupdf.fz_load_fallback_font(script, lang, is_serif, is_bold, is_italic);
            return fertig(font);
        }

        /// <summary>FontDescriptor for Type0/CID or simple fonts.</summary>
        internal static mupdf.PdfObj JM_get_font_descriptor(mupdf.PdfObj fontObj)
        {
            if (fontObj.m_internal == null)
                return new mupdf.PdfObj();
            var desft = PdfDictGet(fontObj, mupdf.mupdf.pdf_new_name("DescendantFonts"));
            if (desft.m_internal != null)
            {
                var first = mupdf.mupdf.pdf_resolve_indirect(mupdf.mupdf.pdf_array_get(desft, 0));
                return PdfDictGet(first, mupdf.mupdf.pdf_new_name("FontDescriptor"));
            }
            return PdfDictGet(fontObj, mupdf.mupdf.pdf_new_name("FontDescriptor"));
        }

        /// <summary>Returns embedded font file bytes for a font xref.</summary>
        internal static byte[] JM_get_fontbuffer(mupdf.PdfDocument pdf, int xref)
        {
            if (xref < 1)
                return null;
            var o = mupdf.mupdf.pdf_load_object(pdf, xref);
            var desc = JM_get_font_descriptor(o);
            if (desc.m_internal == null)
            {
                message("invalid font - FontDescriptor missing");
                return null;
            }

            mupdf.PdfObj stream = new mupdf.PdfObj();
            var ff = PdfDictGet(desc, mupdf.mupdf.pdf_new_name("FontFile"));
            if (ff.m_internal != null)
                stream = ff;
            ff = PdfDictGet(desc, mupdf.mupdf.pdf_new_name("FontFile2"));
            if (ff.m_internal != null)
                stream = ff;
            ff = PdfDictGet(desc, mupdf.mupdf.pdf_new_name("FontFile3"));
            if (ff.m_internal != null)
                stream = ff;

            if (stream.m_internal == null)
            {
                message("warning: unhandled font type");
                return null;
            }

            try
            {
                var buf = stream.pdf_load_stream();
                return buf?.fz_buffer_extract();
            }
            catch (ApplicationException)
            {
                stream = PdfResolveIndirect(stream);
                var buf = mupdf.mupdf.pdf_load_stream(stream);
                return buf?.fz_buffer_extract();
            }
        }

        /// <summary>
        /// Ensure an embedded font stream is populated (PyMuPDF <c>pdf_add_cid_font</c> result).
        /// Some SWIG paths leave <c>/FontFile2</c> with <c>/Length 0</c> while <c>/Length1</c> is set.
        /// </summary>
        internal static void JM_sync_fontfile_streams(mupdf.PdfDocument pdf, Document doc)
        {
            if (pdf?.m_internal == null || doc == null)
                return;
            foreach (var fi in doc.FontInfos)
            {
                if (fi == null || fi.Length < 2 || !(fi[0] is int xref))
                    continue;
                if (!(fi[1] is Dictionary<string, object> fontdict))
                    continue;
                if (!fontdict.TryGetValue("fontfile", out var ffObj))
                    continue;
                string path = ffObj?.ToString();
                if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                    continue;
                JmEnsureFontFileEmbedded(pdf, xref, System.IO.File.ReadAllBytes(path));
            }
        }

        internal static void JmEnsureFontFileEmbedded(mupdf.PdfDocument pdf, int fontXref, byte[] fontBytes)
        {
            if (pdf?.m_internal == null || fontXref < 1 || fontBytes == null || fontBytes.Length == 0)
                return;
            byte[] existing = JM_get_fontbuffer(pdf, fontXref);
            if (existing != null && existing.Length > 0)
                return;

            var fontObj = PdfLoadObject(pdf, fontXref);
            var desc = JM_get_font_descriptor(fontObj);
            if (desc.m_internal == null)
                return;

            var buf = BufferFromBytes(fontBytes);
            var ff2 = PdfDictGet(desc, mupdf.mupdf.pdf_new_name("FontFile2"));
            mupdf.PdfObj stream;
            if (ff2.m_internal != null)
            {
                int streamXref = mupdf.mupdf.pdf_to_num(ff2);
                stream = streamXref > 0
                    ? mupdf.mupdf.pdf_new_indirect(pdf, streamXref, 0)
                    : PdfResolveIndirect(ff2);
            }
            else
            {
                stream = mupdf.mupdf.pdf_add_stream(pdf, buf, new mupdf.PdfObj(), 0);
                PdfDictPutName(stream, "Subtype", "OpenType");
                PdfDictPut(desc, "FontFile2", stream);
                PdfDictPutInt(stream, "Length", fontBytes.Length);
                PdfDictPutInt(stream, "Length1", fontBytes.Length);
                return;
            }

            mupdf.mupdf.pdf_update_stream(pdf, stream, buf, 0);
            PdfDictPutInt(stream, "Length", fontBytes.Length);
            PdfDictPutInt(stream, "Length1", fontBytes.Length);
            mupdf.mupdf.pdf_dict_del(stream, mupdf.mupdf.pdf_new_name("Filter"));
            stream.pdf_dirty_obj();
            fontObj.pdf_dirty_obj();
        }

        internal static string JM_EscapeStrFromStr(string c)
        {
            if (c == null)
                return "";
            var b = Encoding.UTF8.GetBytes(c);
            var ret = new StringBuilder(b.Length);
            foreach (byte bb in b)
                ret.Append((char)bb);
            return ret.ToString();
        }

        /// <summary>Legacy MuPDF.NET <c>Utils.InsertFont</c> (proven font embedding path).</summary>
        internal static object[] JmInsertFontLegacy(
            mupdf.PdfDocument pdf,
            Document doc,
            string bfName,
            string fontFile,
            byte[] fontBuffer,
            bool setSimple,
            int idx,
            int wmode,
            int serif,
            int encoding,
            int ordering)
        {
            mupdf.FzFont font = new mupdf.FzFont();
            mupdf.FzBuffer res = null;
            int simple = 0;
            string exto = null;
            mupdf.PdfObj fontObj = null;

            if (ordering > -1)
            {
                using var cjk_params = new mupdf.ll_fz_lookup_cjk_font_outparams();
                var data = mupdf.mupdf.ll_fz_lookup_cjk_font_outparams_fn(ordering, cjk_params);
                if (data != null)
                {
                    font = mupdf.mupdf.fz_new_font_from_memory(null, data, cjk_params.len, cjk_params.index, 0);
                    fontObj = pdf.pdf_add_simple_font(font, encoding);
                    exto = "n/a";
                    simple = 1;
                }
            }
            else
            {
                using var outparams = new mupdf.ll_fz_lookup_base14_font_outparams();
                var data = !string.IsNullOrEmpty(bfName)
                    ? mupdf.mupdf.ll_fz_lookup_base14_font_outparams_fn(bfName, outparams)
                    : null;
                if (data != null)
                {
                    font = mupdf.mupdf.fz_new_font_from_memory(bfName, data, outparams.len, 0, 0);
                    fontObj = pdf.pdf_add_simple_font(font, encoding);
                    exto = "n/a";
                    simple = 1;
                }
                else
                {
                    if (!string.IsNullOrEmpty(fontFile))
                        font = mupdf.mupdf.fz_new_font_from_file(null, fontFile, idx, 0);
                    else
                    {
                        res = BufferFromBytes(fontBuffer);
                        if (res.m_internal == null)
                            throw new ValueErrorException(Constants.MSG_FILE_OR_BUFFER);
                        font = mupdf.mupdf.fz_new_font_from_buffer(null, res, idx, 0);
                    }

                    if (!setSimple)
                    {
                        fontObj = mupdf.mupdf.pdf_add_cid_font(pdf, font);
                        simple = 0;
                    }
                    else
                    {
                        fontObj = pdf.pdf_add_simple_font(font, encoding);
                        simple = 2;
                    }
                }
            }

            int ixref = fontObj.pdf_to_num();
            if (!setSimple && simple == 0)
            {
                byte[] embedBytes = null;
                if (!string.IsNullOrEmpty(fontFile) && System.IO.File.Exists(fontFile))
                    embedBytes = System.IO.File.ReadAllBytes(fontFile);
                else if (fontBuffer != null && fontBuffer.Length > 0)
                    embedBytes = fontBuffer;
                if (embedBytes != null)
                    JmEnsureFontFileEmbedded(pdf, ixref, embedBytes);
            }
            string name = JM_EscapeStrFromStr(fontObj.pdf_dict_get(new mupdf.PdfObj("BaseFont")).pdf_to_name());
            string subt = mupdf.mupdf.pdf_to_name(fontObj.pdf_dict_get(new mupdf.PdfObj("Subtype")));
            if (string.IsNullOrEmpty(exto))
                exto = doc.JM_get_fontextension(ixref);

            var fontdict = new Dictionary<string, object>
            {
                ["name"] = name,
                ["type"] = subt,
                ["ext"] = exto,
                ["simple"] = simple != 0,
                ["ordering"] = ordering,
                ["ascender"] = mupdf.mupdf.fz_font_ascender(font),
                ["descender"] = mupdf.mupdf.fz_font_descender(font),
            };
            if (!string.IsNullOrEmpty(fontFile))
                fontdict["fontfile"] = fontFile;
            return new object[] { ixref, fontdict };
        }

        /// <summary>Inserts a font dictionary into a PDF document.</summary>
        internal static object[] JM_insert_font(
            mupdf.PdfDocument pdf,
            Document doc,
            string bfname,
            string fontfile,
            byte[] fontbuffer,
            bool set_simple,
            int idx,
            int wmode,
            int serif,
            int encoding,
            int ordering)
        {
            mupdf.FzFont font = null;
            mupdf.PdfObj fontObj = null;
            string exto = null;
            int simple = 0;

            if (ordering > -1)
            {
                using (var cjkOut = new mupdf.ll_fz_lookup_cjk_font_outparams())
                {
                    var data = mupdf.mupdf.ll_fz_lookup_cjk_font_outparams_fn(ordering, cjkOut);
                    int size = cjkOut.len;
                    int index = cjkOut.index;
                    if (data != null && size > 0)
                    {
                        font = mupdf.mupdf.fz_new_font_from_memory(null, data, size, index, 0);
                        fontObj = PdfAddCjkFont(pdf, font, ordering, wmode, serif);
                        exto = "n/a";
                        simple = 0;
                    }
                }
            }

            if (fontObj == null)
            {
                if (!string.IsNullOrEmpty(bfname))
                {
                    using (var b14Out = new mupdf.ll_fz_lookup_base14_font_outparams())
                    {
                        var data = mupdf.mupdf.ll_fz_lookup_base14_font_outparams_fn(bfname, b14Out);
                        int size = b14Out.len;
                        if (data != null && size > 0)
                        {
                            font = mupdf.mupdf.fz_new_font_from_memory(bfname, data, size, 0, 0);
                            fontObj = PdfAddSimpleFont(pdf, font, encoding);
                            exto = "n/a";
                            simple = 1;
                        }
                    }
                }

                if (fontObj == null)
                {
                    // PyMuPDF JM_insert_font: fz_new_font_from_file when fontfile set, else buffer.
                    if (!string.IsNullOrEmpty(fontfile))
                        font = mupdf.mupdf.fz_new_font_from_file(null, fontfile, idx, 0);
                    else
                    {
                        var res = BufferFromBytes(fontbuffer);
                        if (res.m_internal == null)
                            throw new ValueErrorException(Constants.MSG_FILE_OR_BUFFER);
                        font = mupdf.mupdf.fz_new_font_from_buffer(null, res, idx, 0);
                    }

                    if (font?.m_internal != null)
                        mupdf.mupdf.ll_fz_keep_font(font.m_internal);

                    if (!set_simple)
                    {
                        fontObj = mupdf.mupdf.pdf_add_cid_font(pdf, font);
                        simple = 0;
                    }
                    else
                    {
                        fontObj = pdf.pdf_add_simple_font(font, encoding);
                        simple = 2;
                    }
                }
            }

            int ixref = mupdf.mupdf.pdf_to_num(fontObj);
            string name = JM_EscapeStrFromStr(
                mupdf.mupdf.pdf_to_name(PdfDictGets(fontObj, "BaseFont")) ?? "");
            string subt = mupdf.mupdf.pdf_to_name(
                PdfDictGets(fontObj, "Subtype")) ?? "";
            if (exto == null)
                exto = doc.JM_get_fontextension(ixref);

            float asc = mupdf.mupdf.fz_font_ascender(font);
            float dsc = mupdf.mupdf.fz_font_descender(font);
            var fontdict = new Dictionary<string, object>
            {
                ["name"] = name,
                ["type"] = subt,
                ["ext"] = exto,
                ["simple"] = simple != 0,
                ["ordering"] = ordering,
                ["ascender"] = asc,
                ["descender"] = dsc,
            };
            if (!string.IsNullOrEmpty(fontfile))
                fontdict["fontfile"] = fontfile;
            return new object[] { ixref, fontdict };
        }

        /// <summary>Deflates buffer contents, or returns null if compression is not worthwhile.</summary>
        internal static mupdf.FzBuffer? JmCompressBuffer(mupdf.FzBuffer inbuffer)
        {
            if (inbuffer?.m_internal == null)
                return null;
            using var outparams = new mupdf.ll_fz_new_deflated_data_from_buffer_outparams();
            var dataPtr = mupdf.mupdf.ll_fz_new_deflated_data_from_buffer_outparams_fn(
                inbuffer.m_internal,
                mupdf.fz_deflate_level.FZ_DEFLATE_BEST,
                outparams);
            uint compressedLength = outparams.compressed_length;
            if (dataPtr == null || compressedLength == 0)
                return null;
            var buf = new mupdf.FzBuffer(dataPtr, compressedLength);
            buf.fz_resize_buffer(compressedLength);
            return buf;
        }

        /// <summary>Updates a PDF stream, optionally Flate-compressing when beneficial.</summary>
        internal static void JmUpdateStream(mupdf.PdfDocument doc, mupdf.PdfObj obj, mupdf.FzBuffer buffer_, int compress)
        {
            if (compress != 0)
            {
                using var outLen = new mupdf.ll_fz_buffer_storage_outparams();
                uint length = mupdf.mupdf.ll_fz_buffer_storage_outparams_fn(buffer_.m_internal, outLen);
                if (length > 30)
                {
                    using var compressed = JmCompressBuffer(buffer_);
                    if (compressed != null && compressed.m_internal != null)
                    {
                        using var outLenC = new mupdf.ll_fz_buffer_storage_outparams();
                        uint lenC = mupdf.mupdf.ll_fz_buffer_storage_outparams_fn(compressed.m_internal, outLenC);
                        if (lenC < length)
                        {
                            PdfDictPut(
                                obj, "Filter", mupdf.mupdf.pdf_new_name("FlateDecode"));
                            mupdf.mupdf.pdf_update_stream(doc, obj, compressed, 1);
                            return;
                        }
                    }
                }
            }
            mupdf.mupdf.pdf_update_stream(doc, obj, buffer_, 0);
        }

        /// <summary>Builds a <c>/Filespec</c> dictionary with an embedded file stream.</summary>
        internal static mupdf.PdfObj JmEmbedFile(
            mupdf.PdfDocument pdf,
            mupdf.FzBuffer buf,
            string filename,
            string ufilename,
            string desc,
            int compress)
        {
            filename ??= string.Empty;
            ufilename ??= filename;
            desc ??= filename;

            var val = mupdf.mupdf.pdf_new_dict(pdf, 6);
            Helpers.PdfDictPutDict(val, "CI", 4);
            var ef = Helpers.PdfDictPutDict(val, "EF", 4);
            PdfDictPutTextString(val, "F", filename);
            PdfDictPutTextString(val, "UF", ufilename);
            PdfDictPutTextString(val, "Desc", desc);
            PdfDictPutName(val, "Type", "Filespec");

            var placeholder = BufferFromBytes(new byte[] { (byte)' ', (byte)' ' });
            var f = PdfAddStream(pdf, placeholder, new mupdf.PdfObj(), 0);
            PdfDictPut(ef, "F", f);
            JmUpdateStream(pdf, f, buf, compress);

            using var outSt = new mupdf.ll_fz_buffer_storage_outparams();
            uint len_ = mupdf.mupdf.ll_fz_buffer_storage_outparams_fn(buf.m_internal, outSt);
            PdfDictPutInt(f, "DL", len_);
            PdfDictPutInt(f, "Length", len_);
            var prm = Helpers.PdfDictPutDict(f, "Params", 4);
            PdfDictPutInt(prm, "Size", len_);
            return val;
        }

        internal static (int count, float[] color) ColorFromSequence(float[]? seq)
        {
            if (seq == null || seq.Length == 0) return (0, Array.Empty<float>());
            int n = seq.Length;
            if (n != 1 && n != 3 && n != 4) throw new ArgumentException(Constants.MSG_BAD_COLOR_SEQ);
            return (n, seq);
        }

        /// <summary>
        /// MuPDF page annotations are owned by the document; SWIG must not call <c>pdf_drop_annot</c> on them.
        /// </summary>
        internal static mupdf.PdfAnnot PdfAnnotBorrowed(mupdf.PdfAnnot wrapper)
        {
            if (wrapper == null || wrapper.m_internal == null)
                return new mupdf.PdfAnnot();
            IntPtr handle;
            try
            {
                handle = mupdf.PdfAnnot.swigRelease(wrapper).Handle;
            }
            catch (ApplicationException)
            {
                // Already a non-owning wrapper from a prior borrow.
                handle = mupdf.PdfAnnot.getCPtr(wrapper).Handle;
            }
            if (handle == IntPtr.Zero)
                return new mupdf.PdfAnnot();
            return new mupdf.PdfAnnot(handle, false);
        }

        /// <summary>
        /// Wrap a <see cref="mupdf.PdfPage"/> that is borrowed from the document (e.g. <c>pdf_annot_page</c>).
        /// Not for <c>pdf_page_from_fz_page</c> results — those are caller-owned and must use <c>cMemoryOwn=true</c>.
        /// </summary>
        internal static mupdf.PdfPage PdfPageBorrowed(mupdf.PdfPage wrapper)
        {
            if (wrapper == null || wrapper.m_internal == null)
                return new mupdf.PdfPage();
            IntPtr handle;
            try
            {
                handle = mupdf.PdfPage.swigRelease(wrapper).Handle;
            }
            catch (ApplicationException)
            {
                handle = mupdf.PdfPage.getCPtr(wrapper).Handle;
            }
            if (handle == IntPtr.Zero)
                return new mupdf.PdfPage();
            return new mupdf.PdfPage(handle, false);
        }

        /// <summary>
        /// Non-owning <see cref="mupdf.PdfDocument"/> view of an open <see cref="mupdf.FzDocument"/>.
        /// Uses <c>ll_pdf_specifics</c> — <c>new PdfDocument(fz)</c> duplicates stream-backed PDFs (~1 MB/iter leak).
        /// </summary>
        internal static mupdf.PdfDocument PdfDocumentBorrowedFromFz(mupdf.FzDocument fz)
        {
            var internalPdf = mupdf.mupdf.ll_pdf_specifics(fz.m_internal);
            if (internalPdf == null)
                return new mupdf.PdfDocument();
            return PdfDocumentBorrowed(new mupdf.PdfDocument(internalPdf));
        }

        /// <summary>
        /// Wrap a <see cref="mupdf.PdfDocument"/> borrowed from an existing <see cref="mupdf.FzDocument"/>.
        /// </summary>
        internal static mupdf.PdfDocument PdfDocumentBorrowed(mupdf.PdfDocument wrapper)
        {
            if (wrapper == null || wrapper.m_internal == null)
                return new mupdf.PdfDocument();
            IntPtr handle;
            try
            {
                handle = mupdf.PdfDocument.swigRelease(wrapper).Handle;
            }
            catch (ApplicationException)
            {
                handle = mupdf.PdfDocument.getCPtr(wrapper).Handle;
            }
            if (handle == IntPtr.Zero)
                return new mupdf.PdfDocument();
            return new mupdf.PdfDocument(handle, false);
        }

        internal static mupdf.PdfPage PdfAnnotPage(mupdf.PdfAnnot annot)
            => PdfPageBorrowed(mupdf.mupdf.pdf_annot_page(annot));

        /// <summary>
        /// Non-owning <see cref="mupdf.PdfDocument"/> for a page. Never use <c>page.doc()</c> — it duplicates
        /// the native <c>pdf_document</c> (~400 KB/iter leak). Use <see cref="PdfDocumentForPdfPage"/> instead.
        /// </summary>
        internal static mupdf.PdfDocument PdfDocumentForPdfPage(mupdf.PdfPage page)
        {
            if (page?.m_internal?.doc == null)
                return new mupdf.PdfDocument();
            return PdfDocumentBorrowed(new mupdf.PdfDocument(page.m_internal.doc));
        }

        /// <summary>Clear <c>pdf_document.resynth_required</c> without calling <c>page.doc()</c>.</summary>
        internal static void PdfClearResynthRequired(mupdf.PdfPage page)
        {
            if (page?.m_internal?.doc != null)
                page.m_internal.doc.resynth_required = 0;
        }

        /// <summary>Clear SWIG ownership on an existing wrapper without invalidating it (safe when caller still uses the handle).</summary>
        internal static void PdfObjReleaseOwnership(mupdf.PdfObj obj)
        {
            if (obj == null) return;
            var field = typeof(mupdf.PdfObj).GetField(
                "swigCMemOwn",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(obj, false);
        }

        /// <summary>MuPDF device colorspaces are process singletons; keep one owning wrapper alive for each.</summary>
        private static readonly mupdf.FzColorspace DeviceRgbColorspace = mupdf.mupdf.fz_device_rgb();
        private static readonly mupdf.FzColorspace DeviceGrayColorspace = mupdf.mupdf.fz_device_gray();
        private static readonly mupdf.FzColorspace DeviceCmykColorspace = mupdf.mupdf.fz_device_cmyk();

        /// <summary>Borrowed device colorspace for <paramref name="componentCount"/> (1, 3, or 4).</summary>
        internal static mupdf.FzColorspace DeviceColorspace(int componentCount)
        {
            if (componentCount == 3) return DeviceRgbColorspace;
            if (componentCount == 4) return DeviceCmykColorspace;
            return DeviceGrayColorspace;
        }

        /// <summary>Indirect PDF objects from MuPDF getters are borrowed; SWIG must not call <c>pdf_drop_obj</c>.</summary>
        internal static mupdf.PdfObj PdfObjBorrowed(mupdf.PdfObj wrapper)
        {
            if (wrapper == null || wrapper.m_internal == null)
                return new mupdf.PdfObj();
            IntPtr handle;
            try
            {
                handle = mupdf.PdfObj.swigRelease(wrapper).Handle;
            }
            catch (ApplicationException)
            {
                handle = mupdf.PdfObj.getCPtr(wrapper).Handle;
            }
            if (handle == IntPtr.Zero)
                return new mupdf.PdfObj();
            return new mupdf.PdfObj(handle, false);
        }

        internal static mupdf.PdfObj PdfAnnotObj(mupdf.PdfAnnot annot)
            => PdfObjBorrowed(mupdf.mupdf.pdf_annot_obj(annot));

        internal static mupdf.PdfObj PdfPageObj(mupdf.PdfPage page)
            => PdfObjBorrowed(page.obj());

        internal static mupdf.PdfObj PdfLoadObject(mupdf.PdfDocument doc, int xref)
            => PdfObjBorrowed(mupdf.mupdf.pdf_load_object(doc, xref));

        internal static mupdf.PdfObj PdfTrailer(mupdf.PdfDocument doc)
            => PdfObjBorrowed(mupdf.mupdf.pdf_trailer(doc));

        internal static mupdf.PdfObj PdfArrayGet(mupdf.PdfObj arr, int index)
            => PdfObjBorrowed(mupdf.mupdf.pdf_array_get(arr, index));

        internal static mupdf.PdfObj PdfResolveIndirect(mupdf.PdfObj obj)
            => PdfObjBorrowed(mupdf.mupdf.pdf_resolve_indirect(obj));

        internal static mupdf.PdfObj PdfLookupPageObj(mupdf.PdfDocument doc, int pageNo)
            => PdfObjBorrowed(mupdf.mupdf.pdf_lookup_page_obj(doc, pageNo));

        internal static mupdf.PdfObj PdfAddObject(mupdf.PdfDocument doc, mupdf.PdfObj obj)
        {
            var added = mupdf.mupdf.pdf_add_object(doc, obj);
            PdfObjReleaseOwnership(obj);
            return PdfObjBorrowed(added);
        }

        /// <summary>Register an image in the PDF; returned xref wrapper is borrowed.</summary>
        internal static mupdf.PdfObj PdfAddImage(mupdf.PdfDocument doc, mupdf.FzImage image)
            => PdfObjBorrowed(mupdf.mupdf.pdf_add_image(doc, image));

        internal static mupdf.PdfObj PdfAddCjkFont(mupdf.PdfDocument doc, mupdf.FzFont font, int script, int wmode, int serif)
            => PdfObjBorrowed(mupdf.mupdf.pdf_add_cjk_font(doc, font, script, wmode, serif));

        internal static mupdf.PdfObj PdfAddSimpleFont(mupdf.PdfDocument doc, mupdf.FzFont font, int encoding)
            => PdfObjBorrowed(mupdf.mupdf.pdf_add_simple_font(doc, font, encoding));

        internal static mupdf.PdfObj PdfAddCidFont(mupdf.PdfDocument doc, mupdf.FzFont font)
        {
            var added = mupdf.mupdf.pdf_add_cid_font(doc, font);
            PdfObjReleaseOwnership(added);
            return added;
        }

        /// <summary>Register a new dict in the PDF; returned wrapper is borrowed.</summary>
        internal static mupdf.PdfObj PdfAddNewDict(mupdf.PdfDocument doc, int initial)
            => PdfObjBorrowed(mupdf.mupdf.pdf_add_new_dict(doc, initial));

        /// <summary>Detached deep copy; caller must <see cref="PdfUpdateObject"/> or embed via <see cref="PdfDictPut"/>.</summary>
        internal static mupdf.PdfObj PdfDeepCopyObj(mupdf.PdfObj obj)
            => mupdf.mupdf.pdf_deep_copy_obj(obj);

        /// <summary>Create a page object and release SWIG ownership on embedded <paramref name="resources"/> / <paramref name="contents"/>.</summary>
        internal static mupdf.PdfObj PdfAddPage(
            mupdf.PdfDocument doc,
            mupdf.FzRect mediabox,
            int rotate,
            mupdf.PdfObj resources,
            mupdf.FzBuffer contents)
        {
            var page = mupdf.mupdf.pdf_add_page(doc, mediabox, rotate, resources, contents);
            PdfObjReleaseOwnership(resources);
            contents?.Dispose();
            return PdfObjBorrowed(page);
        }

        internal static void PdfInsertPage(mupdf.PdfDocument doc, int at, mupdf.PdfObj pageObj)
        {
            mupdf.mupdf.pdf_insert_page(doc, at, pageObj);
            PdfObjReleaseOwnership(pageObj);
        }

        /// <summary>Store <paramref name="obj"/> at <paramref name="xref"/>; release SWIG ownership on the wrapper.</summary>
        internal static void PdfUpdateObject(mupdf.PdfDocument doc, int xref, mupdf.PdfObj obj)
        {
            mupdf.mupdf.pdf_update_object(doc, xref, obj);
            PdfObjReleaseOwnership(obj);
        }

        internal static mupdf.PdfObj PdfGraftObject(mupdf.PdfDocument doc, mupdf.PdfObj obj)
            => PdfObjBorrowed(mupdf.mupdf.pdf_graft_object(doc, obj));

        internal static mupdf.PdfObj PdfGraftMappedObject(mupdf.PdfGraftMap map, mupdf.PdfObj obj)
            => PdfObjBorrowed(map.pdf_graft_mapped_object(obj));

        internal static mupdf.PdfObj PdfNewIndirect(mupdf.PdfDocument doc, int num, int gen)
            => PdfObjBorrowed(mupdf.mupdf.pdf_new_indirect(doc, num, gen));

        /// <summary>Put into a dict that now holds <paramref name="val"/>; release SWIG ownership on key and value wrappers.</summary>
        internal static void PdfDictPut(mupdf.PdfObj dict, mupdf.PdfObj key, mupdf.PdfObj val)
        {
            mupdf.mupdf.pdf_dict_put(dict, key, val);
            PdfObjReleaseOwnership(key);
            PdfObjReleaseOwnership(val);
        }

        internal static void PdfDictPut(mupdf.PdfObj dict, string key, mupdf.PdfObj val)
            => PdfDictPut(dict, mupdf.mupdf.pdf_new_name(key), val);

        internal static mupdf.PdfObj PdfDictPutDict(mupdf.PdfObj dict, mupdf.PdfObj key, int initial)
        {
            var val = mupdf.mupdf.pdf_dict_put_dict(dict, key, initial);
            PdfObjReleaseOwnership(key);
            return PdfObjBorrowed(val);
        }

        internal static mupdf.PdfObj PdfDictPutDict(mupdf.PdfObj dict, string key, int initial)
            => PdfDictPutDict(dict, mupdf.mupdf.pdf_new_name(key), initial);

        internal static mupdf.PdfObj PdfDictPutArray(mupdf.PdfObj dict, mupdf.PdfObj key, int initial)
        {
            var val = mupdf.mupdf.pdf_dict_put_array(dict, key, initial);
            PdfObjReleaseOwnership(key);
            return PdfObjBorrowed(val);
        }

        internal static mupdf.PdfObj PdfDictPutArray(mupdf.PdfObj dict, string key, int initial)
            => PdfDictPutArray(dict, mupdf.mupdf.pdf_new_name(key), initial);

        internal static mupdf.PdfObj PdfAddStream(mupdf.PdfDocument doc, mupdf.FzBuffer buf, mupdf.PdfObj obj, int compress)
        {
            var stream = mupdf.mupdf.pdf_add_stream(doc, buf, obj, compress);
            PdfObjReleaseOwnership(obj);
            return PdfObjBorrowed(stream);
        }

        internal static mupdf.PdfObj PdfNewXObject(
            mupdf.PdfDocument doc,
            mupdf.FzRect bbox,
            mupdf.FzMatrix matrix,
            mupdf.PdfObj res,
            mupdf.FzBuffer buffer)
        {
            var xobj = mupdf.mupdf.pdf_new_xobject(doc, bbox, matrix, res, buffer);
            PdfObjReleaseOwnership(res);
            return PdfObjBorrowed(xobj);
        }

        internal static void PdfArrayPush(mupdf.PdfObj arr, mupdf.PdfObj val)
        {
            mupdf.mupdf.pdf_array_push(arr, val);
            PdfObjReleaseOwnership(val);
        }

        internal static void PdfArrayPushName(mupdf.PdfObj arr, string name)
            => PdfArrayPush(arr, mupdf.mupdf.pdf_new_name(name));

        internal static void PdfArrayInsert(mupdf.PdfObj arr, mupdf.PdfObj val, int index)
        {
            mupdf.mupdf.pdf_array_insert(arr, val, index);
            PdfObjReleaseOwnership(val);
        }

        internal static mupdf.PdfObj PdfDictGet(mupdf.PdfObj dict, mupdf.PdfObj key)
            => PdfObjBorrowed(mupdf.mupdf.pdf_dict_get(dict, key));

        internal static mupdf.PdfObj PdfDictGets(mupdf.PdfObj dict, string key)
            => PdfObjBorrowed(mupdf.mupdf.pdf_dict_gets(dict, key));

        internal static void PdfDictPuts(mupdf.PdfObj dict, string key, mupdf.PdfObj val)
        {
            mupdf.mupdf.pdf_dict_puts(dict, key, val);
            PdfObjReleaseOwnership(val);
        }

        internal static void PdfDictPutTextString(mupdf.PdfObj dict, string key, string value)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            mupdf.mupdf.pdf_dict_put_text_string(dict, keyObj, value);
            PdfObjReleaseOwnership(keyObj);
        }

        internal static void PdfDictPutInt(mupdf.PdfObj dict, string key, long value)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            mupdf.mupdf.pdf_dict_put_int(dict, keyObj, value);
            PdfObjReleaseOwnership(keyObj);
        }

        internal static void PdfDictPutReal(mupdf.PdfObj dict, string key, float value)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            mupdf.mupdf.pdf_dict_put_real(dict, keyObj, value);
            PdfObjReleaseOwnership(keyObj);
        }

        internal static void PdfDictPutName(mupdf.PdfObj dict, string key, string value)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            mupdf.mupdf.pdf_dict_put_name(dict, keyObj, value);
            PdfObjReleaseOwnership(keyObj);
        }

        internal static void PdfDictPutBool(mupdf.PdfObj dict, string key, int value)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            mupdf.mupdf.pdf_dict_put_bool(dict, keyObj, value);
            PdfObjReleaseOwnership(keyObj);
        }

        internal static void PdfDictPutRect(mupdf.PdfObj dict, string key, mupdf.FzRect value)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            mupdf.mupdf.pdf_dict_put_rect(dict, keyObj, value);
            PdfObjReleaseOwnership(keyObj);
        }

        internal static void PdfDictPutMatrix(mupdf.PdfObj dict, string key, mupdf.FzMatrix value)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            mupdf.mupdf.pdf_dict_put_matrix(dict, keyObj, value);
            PdfObjReleaseOwnership(keyObj);
        }

        internal static void PdfDictDel(mupdf.PdfObj dict, string key)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            mupdf.mupdf.pdf_dict_del(dict, keyObj);
            keyObj.Dispose();
        }

        /// <summary>Compare a PDF object to a name; drops the temporary name object after the call.</summary>
        internal static bool PdfNameEq(mupdf.PdfObj obj, string name)
        {
            var nameObj = mupdf.mupdf.pdf_new_name(name);
            try
            {
                return mupdf.mupdf.pdf_name_eq(obj, nameObj) != 0;
            }
            finally
            {
                nameObj.Dispose();
            }
        }

        internal static bool PdfObjCmpName(mupdf.PdfObj obj, string name)
        {
            var nameObj = mupdf.mupdf.pdf_new_name(name);
            try
            {
                return mupdf.mupdf.pdf_objcmp(obj, nameObj) == 0;
            }
            finally
            {
                nameObj.Dispose();
            }
        }

        internal static int PdfDictGetInheritableInt(mupdf.PdfObj dict, string key)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            try
            {
                return mupdf.mupdf.pdf_dict_get_inheritable_int(dict, keyObj);
            }
            finally
            {
                keyObj.Dispose();
            }
        }

        internal static string PdfDictGetTextString(mupdf.PdfObj dict, string key)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            try
            {
                return mupdf.mupdf.pdf_dict_get_text_string(dict, keyObj) ?? "";
            }
            finally
            {
                keyObj.Dispose();
            }
        }

        internal static int PdfDictGetInt(mupdf.PdfObj dict, string key)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            try
            {
                return mupdf.mupdf.pdf_dict_get_int(dict, keyObj);
            }
            finally
            {
                keyObj.Dispose();
            }
        }

        internal static mupdf.FzDisplayList FzDisplayListBorrowed(mupdf.FzDisplayList wrapper)
        {
            if (wrapper == null || wrapper.m_internal == null)
                return new mupdf.FzDisplayList();
            IntPtr handle;
            try
            {
                handle = mupdf.FzDisplayList.swigRelease(wrapper).Handle;
            }
            catch (ApplicationException)
            {
                handle = mupdf.FzDisplayList.getCPtr(wrapper).Handle;
            }
            if (handle == IntPtr.Zero)
                return new mupdf.FzDisplayList();
            return new mupdf.FzDisplayList(handle, false);
        }

        internal static mupdf.FzPixmap FzPixmapBorrowed(mupdf.FzPixmap wrapper)
        {
            if (wrapper == null || wrapper.m_internal == null)
                return new mupdf.FzPixmap();
            IntPtr handle;
            try
            {
                handle = mupdf.FzPixmap.swigRelease(wrapper).Handle;
            }
            catch (ApplicationException)
            {
                handle = mupdf.FzPixmap.getCPtr(wrapper).Handle;
            }
            if (handle == IntPtr.Zero)
                return new mupdf.FzPixmap();
            return new mupdf.FzPixmap(handle, false);
        }

        /// <summary>
        /// Drop an owning <see cref="mupdf.FzPixmap"/> created by MuPDF (e.g. <c>fz_scale_pixmap</c>).
        /// <c>delete_FzPixmap</c> alone does not release sample storage; call <c>fz_drop_pixmap</c> first.
        /// </summary>
        internal static void DropFzPixmap(ref mupdf.FzPixmap? pm)
        {
            if (pm == null) return;
            lock (Utils.MuPDFLock)
            {
                if (mupdf.FzPixmap.getCPtr(pm).Handle == IntPtr.Zero)
                {
                    pm = null;
                    return;
                }
                try
                {
                    var internalPix = pm.m_internal;
                    if (internalPix != null)
                        mupdf.mupdf.ll_fz_drop_pixmap(internalPix);
                }
                catch
                {
                    // Best-effort; still run wrapper dispose.
                }
                try { pm.Dispose(); } catch { }
            }
            pm = null;
        }

        internal static mupdf.PdfObj PdfDictGetInheritable(mupdf.PdfObj dict, mupdf.PdfObj key)
            => PdfObjBorrowed(mupdf.mupdf.pdf_dict_get_inheritable(dict, key));

        internal static mupdf.PdfObj PdfDictGetInheritable(mupdf.PdfObj dict, string key)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            try
            {
                return PdfDictGetInheritable(dict, keyObj);
            }
            finally
            {
                keyObj.Dispose();
            }
        }

        internal static mupdf.PdfObj PdfObjDictGet(mupdf.PdfObj dict, string key)
            => PdfDictGets(dict, key);

        internal static mupdf.PdfObj PdfDictGetp(mupdf.PdfObj dict, string path)
            => PdfObjBorrowed(mupdf.mupdf.pdf_dict_getp(dict, path));

        internal static mupdf.PdfObj PdfDictGetpInheritable(mupdf.PdfObj dict, string path)
            => PdfObjBorrowed(mupdf.mupdf.pdf_dict_getp_inheritable(dict, path));

        internal static mupdf.PdfObj PdfDictGetsInheritable(mupdf.PdfObj dict, string key)
            => PdfObjBorrowed(mupdf.mupdf.pdf_dict_gets_inheritable(dict, key));

        internal static mupdf.PdfObj PdfDictGetVal(mupdf.PdfObj dict, int idx)
            => PdfObjBorrowed(mupdf.mupdf.pdf_dict_get_val(dict, idx));

        internal static mupdf.PdfObj PdfDictGetKey(mupdf.PdfObj dict, int idx)
            => PdfObjBorrowed(mupdf.mupdf.pdf_dict_get_key(dict, idx));

        internal static mupdf.PdfObj PdfDictGeta(mupdf.PdfObj dict, mupdf.PdfObj key, mupdf.PdfObj abbrev)
            => PdfObjBorrowed(mupdf.mupdf.pdf_dict_geta(dict, key, abbrev));

        internal static mupdf.PdfObj PdfDictGeta(mupdf.PdfObj dict, string key, string abbrev)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            var abbrevObj = mupdf.mupdf.pdf_new_name(abbrev);
            try
            {
                return PdfDictGeta(dict, keyObj, abbrevObj);
            }
            finally
            {
                keyObj.Dispose();
                abbrevObj.Dispose();
            }
        }

        internal static mupdf.FzLink PdfCreateLink(mupdf.PdfPage page, mupdf.FzRect bbox, string uri)
            => FzLinkBorrowed(mupdf.mupdf.pdf_create_link(page, bbox, uri));

        internal static mupdf.PdfObj PdfObjDictGet(mupdf.PdfObj dict, mupdf.PdfObj key)
            => PdfDictGet(dict, key);

        internal static mupdf.PdfObj PdfObjDictGet(mupdf.PdfObj dict, int key)
            => PdfObjBorrowed(dict.pdf_dict_get(key));

        internal static mupdf.PdfObj PdfObjDictGeta(mupdf.PdfObj dict, mupdf.PdfObj key, mupdf.PdfObj abbrev)
            => PdfDictGeta(dict, key, abbrev);

        internal static mupdf.PdfObj PdfObjDictGeta(mupdf.PdfObj dict, string key, string abbrev)
            => PdfDictGeta(dict, key, abbrev);

        internal static mupdf.PdfObj PdfObjDictGetp(mupdf.PdfObj dict, string path)
            => PdfDictGetp(dict, path);

        internal static mupdf.FzRect FzBoundPage(mupdf.FzPage page)
            => FzRectBorrowed(mupdf.mupdf.fz_bound_page(page));

        /// <summary><c>pdf_annot_rect</c> / <c>pdf_bound_annot</c> return rects owned by the annot; do not drop.</summary>
        internal static mupdf.FzRect FzRectBorrowed(mupdf.FzRect wrapper)
        {
            if (wrapper == null || mupdf.FzRect.getCPtr(wrapper).Handle == IntPtr.Zero)
                return new mupdf.FzRect();
            IntPtr handle;
            try
            {
                handle = mupdf.FzRect.swigRelease(wrapper).Handle;
            }
            catch (ApplicationException)
            {
                handle = mupdf.FzRect.getCPtr(wrapper).Handle;
            }
            if (handle == IntPtr.Zero)
                return new mupdf.FzRect();
            return new mupdf.FzRect(handle, false);
        }

        internal static mupdf.FzRect PdfAnnotRect(mupdf.PdfAnnot annot)
            => FzRectBorrowed(mupdf.mupdf.pdf_annot_rect(annot));

        internal static mupdf.FzRect PdfBoundAnnot(mupdf.PdfAnnot annot)
            => FzRectBorrowed(mupdf.mupdf.pdf_bound_annot(annot));

        internal static mupdf.FzMatrix FzMatrixBorrowed(mupdf.FzMatrix wrapper)
        {
            if (wrapper == null || mupdf.FzMatrix.getCPtr(wrapper).Handle == IntPtr.Zero)
                return new mupdf.FzMatrix();
            IntPtr handle;
            try
            {
                handle = mupdf.FzMatrix.swigRelease(wrapper).Handle;
            }
            catch (ApplicationException)
            {
                handle = mupdf.FzMatrix.getCPtr(wrapper).Handle;
            }
            if (handle == IntPtr.Zero)
                return new mupdf.FzMatrix();
            return new mupdf.FzMatrix(handle, false);
        }

        internal static mupdf.FzRect PdfDictGetRect(mupdf.PdfObj dict, mupdf.PdfObj key)
            => FzRectBorrowed(mupdf.mupdf.pdf_dict_get_rect(dict, key));

        internal static mupdf.FzRect PdfDictGetRect(mupdf.PdfObj dict, string key)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            try
            {
                return PdfDictGetRect(dict, keyObj);
            }
            finally
            {
                keyObj.Dispose();
            }
        }

        internal static mupdf.FzMatrix PdfDictGetMatrix(mupdf.PdfObj dict, mupdf.PdfObj key)
            => FzMatrixBorrowed(mupdf.mupdf.pdf_dict_get_matrix(dict, key));

        internal static mupdf.FzMatrix PdfDictGetMatrix(mupdf.PdfObj dict, string key)
        {
            var keyObj = mupdf.mupdf.pdf_new_name(key);
            try
            {
                return PdfDictGetMatrix(dict, keyObj);
            }
            finally
            {
                keyObj.Dispose();
            }
        }

        internal static mupdf.PdfAnnot PdfFirstAnnot(mupdf.PdfPage page)
            => PdfAnnotBorrowed(mupdf.mupdf.pdf_first_annot(page));

        internal static mupdf.PdfAnnot PdfNextAnnot(mupdf.PdfAnnot annot)
            => PdfAnnotBorrowed(mupdf.mupdf.pdf_next_annot(annot));

        internal static mupdf.PdfAnnot PdfNextWidget(mupdf.PdfAnnot widget)
            => PdfAnnotBorrowed(mupdf.mupdf.pdf_next_widget(widget));

        internal static mupdf.PdfAnnot PdfCreateAnnot(mupdf.PdfPage page, mupdf.pdf_annot_type type)
            => PdfAnnotBorrowed(mupdf.mupdf.pdf_create_annot(page, type));

        internal static mupdf.PdfAnnot PdfCreateAnnotRaw(mupdf.PdfPage page, mupdf.pdf_annot_type type)
            => PdfAnnotBorrowed(mupdf.mupdf.pdf_create_annot_raw(page, type));

        /// <summary>Rebuild annot appearance. Does not invalidate the page tree (existing <see cref="Annot"/> handles stay valid).</summary>
        internal static void PdfUpdateAnnot(mupdf.PdfAnnot annot, Page page = null)
        {
            mupdf.mupdf.pdf_update_annot(annot);
        }

        internal static void PdfDirtyAndUpdateAnnot(mupdf.PdfAnnot annot, Page page = null)
        {
            mupdf.mupdf.pdf_dirty_annot(annot);
            PdfUpdateAnnot(annot, page);
        }

        /// <summary>Remove annot from page; drop only the matching wrapper ref when provided.</summary>
        internal static void PdfDeleteAnnot(mupdf.PdfPage pdfPage, mupdf.PdfAnnot annot, Page page, Annot annotWrapper = null)
        {
            mupdf.mupdf.pdf_delete_annot(pdfPage, annot);
            if (annotWrapper != null)
                page?.ForgetAnnotRef(annotWrapper);
        }

        internal static mupdf.PdfAnnot PdfFirstWidget(mupdf.PdfPage page)
            => PdfAnnotBorrowed(mupdf.mupdf.pdf_first_widget(page));

        /// <summary>Page-owned link nodes must not be dropped by SWIG finalizers.</summary>
        internal static mupdf.FzLink FzLinkBorrowed(mupdf.FzLink wrapper)
        {
            if (wrapper == null || wrapper.m_internal == null)
                return new mupdf.FzLink();
            IntPtr handle;
            try
            {
                handle = mupdf.FzLink.swigRelease(wrapper).Handle;
            }
            catch (ApplicationException)
            {
                handle = mupdf.FzLink.getCPtr(wrapper).Handle;
            }
            if (handle == IntPtr.Zero)
                return new mupdf.FzLink();
            return new mupdf.FzLink(handle, false);
        }

        internal static mupdf.FzLink FzLoadLinks(mupdf.FzPage page)
            => FzLinkBorrowed(mupdf.mupdf.fz_load_links(page));

        internal static mupdf.FzLink FzLinkNext(mupdf.FzLink link)
            => FzLinkBorrowed(link.next());

        internal static bool InRange(int val, int low, int high) => val >= low && val <= high;
        internal static bool InRange(float val, float low, float high) => val >= low && val <= high;

        internal static int ResolvePageIndex(int pageCount, int index)
        {
            if (index < 0) index += pageCount;
            if (index < 0 || index >= pageCount) throw new IndexOutOfRangeException($"page {index} not in document");
            return index;
        }

        /// <summary>
        /// Return a PDF string depending on its coding (PyMuPDF <c>get_pdf_str</c>).
        /// </summary>
        /// <remarks>
        /// Returns a string bracketed with either "()" or "&lt;&gt;" for hex values.
        /// If only ascii then "(original)" is returned, else if only 8 bit chars
        /// then "(original)" with interspersed octal strings \nnn is returned,
        /// else a string "&lt;FEFF[hexstring]&gt;" is returned, where [hexstring] is the
        /// UTF-16BE encoding of the original.
        /// </remarks>
        internal static string GetPdfStr(string s)
        {
            if (string.IsNullOrEmpty(s))
                // return "()"
                return "()";

            static string MakeUtf16be(string text)
            {
                byte[] r = new byte[2 + Encoding.BigEndianUnicode.GetByteCount(text)];
                r[0] = 254;
                r[1] = 255;
                Encoding.BigEndianUnicode.GetBytes(text, 0, text.Length, r, 2);
                // return "<" + r.hex() + ">"  # brackets indicate hex
                return "<" + BytesToHex(r).ToLowerInvariant() + ">";
            }

            // octal numbers \nnn for chars outside the ASCII range, or returns
            var r = new StringBuilder();
            foreach (char c in s)
            {
                // oc = ord(c)
                int oc = c;
                if (oc > 255)
                    // return make_utf16be(s)
                    return MakeUtf16be(s);

                if (oc > 31 && oc < 127)
                {
                    if (c is '(' or ')' or '\\')
                        r.Append('\\');
                    r.Append(c);
                    continue;
                }

                if (oc > 127)
                {
                    r.Append('\\');
                    r.Append(Convert.ToString(oc, 8).PadLeft(3, '0'));
                    continue;
                }

                if (oc == 8)
                    r.Append("\\b");
                else if (oc == 9)
                    r.Append("\\t");
                else if (oc == 10)
                    r.Append("\\n");
                else if (oc == 12)
                    r.Append("\\f");
                else if (oc == 13)
                    r.Append("\\r");
                else
                    r.Append("\\267");
            }

            // return "(" + r + ")"
            return "(" + r + ")";
        }

        /// <summary>
        /// "Now" timestamp in PDF Format.
        /// </summary>
        internal static string GetPdfNow()
        {
            var now = DateTime.Now;
            var offset = TimeZoneInfo.Local.GetUtcOffset(now);
            char sign = offset < TimeSpan.Zero ? '-' : '+';
            offset = offset.Duration();
            return $"D:{now:yyyyMMddHHmmss}{sign}{offset.Hours:00}'{offset.Minutes:00}'";
        }

        /// <summary>PySequence to fz_point.</summary>
        internal static mupdf.FzPoint JM_point_from_py(object p)
        {
            if (p is mupdf.FzPoint fp)
                return fp;
            if (p is Point pt)
                return pt.ToFzPoint();
            if (p is Rect rect)
                return new Point(rect.X0, rect.Y0).ToFzPoint();
            if (p is IRect irect)
                return new Point(irect.X0, irect.Y0).ToFzPoint();
            if (p is IList seq)
            {
                if (seq.Count != 2)
                    throw new ValueErrorException(Constants.MSG_BAD_ARG_INK_ANNOT);
                return new mupdf.FzPoint(Convert.ToSingle(seq[0]), Convert.ToSingle(seq[1]));
            }
            return new mupdf.FzPoint(Constants.FzMinInfRect, Constants.FzMinInfRect);
        }

        /// <summary>Lists annotation NM identifiers on a page.</summary>
        internal static List<string> JM_get_annot_id_list(mupdf.PdfPage page)
        {
            var names = new List<string>();
            var annots = PdfDictGets(PdfPageObj(page), "Annots");
            if (annots.m_internal == null)
                return names;
            for (int i = 0; i < mupdf.mupdf.pdf_array_len(annots); i++)
            {
                var annot_obj = PdfArrayGet(annots, i);
                var name = PdfDictGets(annot_obj, "NM");
                if (name.m_internal != null)
                    names.Add(mupdf.mupdf.pdf_to_text_string(name));
            }
            return names;
        }

        /// <summary>
        /// Add a unique /NM key to an annotation or widget.
        /// Append a number to 'stem' such that the result is a unique name.
        /// PyMuPDF equivalent: <c>JM_add_annot_id</c>.
        /// </summary>
        internal static void JM_add_annot_id(mupdf.PdfAnnot annot, string stem, mupdf.PdfPage page)
        {
            var annot_obj = PdfAnnotObj(annot);
            var names = JM_get_annot_id_list(page);
            int i = 0;
            string stem_id;
            while (true)
            {
                stem_id = $"{Utils.ANNOT_ID_STEM}-{stem}{i}";
                if (!names.Contains(stem_id))
                    break;
                i++;
            }
            var name = mupdf.mupdf.pdf_new_string(stem_id, (uint)stem_id.Length);
            PdfDictPuts(annot_obj, "NM", name);
            PdfClearResynthRequired(page);
        }

        /// <summary>Return the inheritable /Rotate value of a PDF page.</summary>
        internal static int PageRotation(mupdf.PdfPage page)
        {
            int rotate = PdfDictGetInheritableInt(PdfPageObj(page), "Rotate");
            return JmNormRotation(rotate);
        }

        internal static Matrix RotatePageMatrix(Page page)
        {
            if (page == null) return Matrix.Identity;
            int rotation = page.Rotation;
            if (rotation == 0) return Matrix.Identity;

            float w = page.CropBox.Width;
            float h = page.CropBox.Height;
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

            var cb = Helpers.PdfDictGetsInheritable(PdfPageObj(page), "CropBox");
            Rect cbSize;
            if (cb.m_internal != null)
            {
                cbSize = new Rect(mupdf.mupdf.pdf_to_rect(cb));
            }
            else
            {
                var mb = Helpers.PdfDictGetsInheritable(PdfPageObj(page), "MediaBox");
                cbSize = new Rect(mupdf.mupdf.pdf_to_rect(mb));
            }
            float w = cbSize.Width;
            float h = cbSize.Height;
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

        /// <summary>Copies buffer bytes without consuming the <see cref="mupdf.FzBuffer"/>.</summary>
        internal static byte[] BinFromBuffer(mupdf.FzBuffer buffer) => BufferToBytes(buffer);

        internal static byte[] BufferToBytes(mupdf.FzBuffer buffer)
        {
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

            var obj = PdfDictGets(annot_obj, "Border");
            if (mupdf.mupdf.pdf_is_array(obj) != 0)
            {
                width = mupdf.mupdf.pdf_to_real(Helpers.PdfArrayGet(obj, 2));
                if (mupdf.mupdf.pdf_array_len(obj) == 4)
                {
                    var dash = Helpers.PdfArrayGet(obj, 3);
                    for (int i = 0; i < mupdf.mupdf.pdf_array_len(dash); i++)
                        dashes.Add(mupdf.mupdf.pdf_to_int(Helpers.PdfArrayGet(dash, i)));
                }
            }

            var bs = PdfDictGets(annot_obj, "BS");
            if (bs.m_internal != null)
            {
                width = mupdf.mupdf.pdf_to_real(PdfDictGets(bs, "W"));
                style = mupdf.mupdf.pdf_to_name(PdfDictGets(bs, "S"));
                if (style == "") style = null;
                obj = PdfDictGets(bs, "D");
                if (obj.m_internal != null)
                {
                    dashes.Clear();
                    for (int i = 0; i < mupdf.mupdf.pdf_array_len(obj); i++)
                        dashes.Add(mupdf.mupdf.pdf_to_int(Helpers.PdfArrayGet(obj, i)));
                }
            }

            obj = PdfDictGets(annot_obj, "BE");
            if (obj.m_internal != null)
                clouds = mupdf.mupdf.pdf_to_int(PdfDictGets(obj, "I"));

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

            var o = Helpers.PdfObjDictGet(annot_obj,mupdf.mupdf.PDF_ENUM_NAME_C);
            if (mupdf.mupdf.pdf_is_array(o) != 0)
            {
                int n = mupdf.mupdf.pdf_array_len(o);
                for (int i = 0; i < n; i++)
                    stroke.Add(mupdf.mupdf.pdf_to_real(Helpers.PdfArrayGet(o, i)));
            }

            o = Helpers.PdfDictGets(annot_obj, "IC");
            if (mupdf.mupdf.pdf_is_array(o) != 0)
            {
                int n = mupdf.mupdf.pdf_array_len(o);
                for (int i = 0; i < n; i++)
                    fill.Add(mupdf.mupdf.pdf_to_real(Helpers.PdfArrayGet(o, i)));
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

            var oborder = JM_annot_border(annot_obj);

            PdfDictDel(annot_obj, "BS");
            PdfDictDel(annot_obj, "BE");
            PdfDictDel(annot_obj, "Border");

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
                PdfDictPut(bs, "D", darr);
                PdfDictPut(annot_obj, "BS", bs);
            }

            var bsObj = PdfDictGets(annot_obj, "BS");
            if (bsObj.m_internal == null)
            {
                bsObj = mupdf.mupdf.pdf_new_dict(doc, 2);
                PdfDictPut(annot_obj, "BS", bsObj);
            }
            PdfDictPut(bsObj, "W", mupdf.mupdf.pdf_new_real(nwidth));
            PdfDictPutName(bsObj, "S", dashlen == 0 ? JM_get_border_style(nstyle) : "D");

            if (nclouds > 0)
            {
                var be = mupdf.mupdf.pdf_new_dict(doc, 2);
                PdfDictPutName(be, "S", "C");
                PdfDictPutInt(be, "I", nclouds);
                PdfDictPut(annot_obj, "BE", be);
            }
        }

        internal static void JM_add_oc_object(mupdf.PdfDocument pdf, mupdf.PdfObj reference, int xref)
        {
            var indobj = PdfNewIndirect(pdf, xref, 0);
            if (mupdf.mupdf.pdf_is_dict(indobj) == 0)
                throw new ArgumentException("bad optional content reference");
            var type = PdfDictGets(indobj, "Type");
            bool isOcg = PdfObjCmpName(type, "OCG");
            bool isOcmd = PdfObjCmpName(type, "OCMD");
            if (!isOcg && !isOcmd)
                throw new ArgumentException("bad optional content reference");
            PdfDictPut(reference, "OC", indobj);
        }

        /// <summary><c>src/__init__.py</c>) — read and concatenate a page's /Contents stream(s.</summary>
        internal static mupdf.FzBuffer JM_read_contents(mupdf.PdfObj pageref)
        {
            var contents = PdfDictGets(pageref, "Contents");
            if (mupdf.mupdf.pdf_is_array(contents) != 0)
            {
                var res = new mupdf.FzBuffer(1024);
                int n = mupdf.mupdf.pdf_array_len(contents);
                for (int i = 0; i < n; i++)
                {
                    if (i > 0)
                        mupdf.mupdf.fz_append_byte(res, 32);
                    var obj = Helpers.PdfArrayGet(contents, i);
                    if (mupdf.mupdf.pdf_is_stream(obj) != 0)
                    {
                        var nres = mupdf.mupdf.pdf_load_stream(obj);
                        mupdf.mupdf.fz_append_buffer(res, nres);
                    }
                }
                return res;
            }
            if (contents.m_internal != null)
                return mupdf.mupdf.pdf_load_stream(contents);
            return new mupdf.FzBuffer(0);
        }

        /// <summary><c>src/__init__.py</c>.</summary>
        /// <remarks>
        /// Python docstring (verbatim intent):
        /// Insert a buffer as a new separate /Contents object of a page.
        /// 1. Create a new stream object from buffer 'newcont'
        /// 2. If /Contents already is an array, then just prepend or append this object
        /// 3. Else, create new array and put old content obj and this object into it.
        /// If the page had no /Contents before, just create a 1-item array.
        /// </remarks>
        internal static int JM_insert_contents(mupdf.PdfDocument pdf, mupdf.PdfObj pageref, mupdf.FzBuffer newcont, bool overlay)
        {
            var contents = PdfDictGets(pageref, "Contents");
            var newconts = PdfAddStream(pdf, newcont, new mupdf.PdfObj(), 0);
            int xref = mupdf.mupdf.pdf_to_num(newconts);
            if (mupdf.mupdf.pdf_is_array(contents) != 0)
            {
                if (overlay) // append new object
                    PdfArrayPush(contents, newconts);
                else // prepend new object
                    PdfArrayInsert(contents, newconts, 0);
            }
            else
            {
                var carr = mupdf.mupdf.pdf_new_array(pdf, 5);
                if (overlay)
                {
                    if (contents.m_internal != null)
                        PdfArrayPush(carr, contents);
                    PdfArrayPush(carr, newconts);
                }
                else
                {
                    PdfArrayPush(carr, newconts);
                    if (contents.m_internal != null)
                        PdfArrayPush(carr, contents);
                }
                PdfDictPut(pageref, "Contents", carr);
            }
            return xref;
        }

        internal static void JM_set_resource_property(mupdf.PdfObj reference, string name, int xref)
        {
            var pdf = mupdf.mupdf.pdf_get_bound_document(reference);
            var ind = PdfNewIndirect(pdf, xref, 0);
            if (ind.m_internal == null)
                throw new ValueErrorException(Constants.MSG_BAD_XREF);
            var resources = PdfDictGets(reference, "Resources");
            if (resources.m_internal == null)
                resources = Helpers.PdfDictPutDict(reference, "Resources", 1);
            var properties = PdfDictGets(resources, "Properties");
            if (properties.m_internal == null)
                properties = Helpers.PdfDictPutDict(resources, "Properties", 1);
            PdfDictPut(properties, name, ind);
        }

        internal static mupdf.PdfObj JM_xobject_from_page(mupdf.PdfDocument pdfout, mupdf.PdfPage srcpage, int xref, mupdf.PdfGraftMap gmap)
        {
            if (xref > 0)
                return PdfNewIndirect(pdfout, xref, 0);

            var spageref = PdfPageObj(srcpage);
            var mediabox = mupdf.mupdf.pdf_to_rect(
                Helpers.PdfDictGetsInheritable(spageref, "MediaBox"));
            var resourcesSrc = Helpers.PdfDictGetsInheritable(spageref, "Resources");
            var resources = gmap?.m_internal != null
                ? PdfGraftMappedObject(gmap, resourcesSrc)
                : PdfGraftObject(pdfout, resourcesSrc);

            var res = JM_read_contents(spageref);
            var xobj1 = PdfNewXObject(pdfout, mediabox, new mupdf.FzMatrix(), new mupdf.PdfObj(), res);
            mupdf.mupdf.pdf_update_stream(pdfout, xobj1, res, 1);
            PdfDictPut(xobj1, "Resources", resources);
            return xobj1;
        }

        /// <summary>Serializes a PDF object to a buffer.</summary>
        internal static mupdf.FzBuffer JmObjectToBuffer(mupdf.PdfObj what, int compress, int ascii)
        {
            var res = mupdf.mupdf.fz_new_buffer(512);
            using (var output = new mupdf.FzOutput(res))
            {
                output.pdf_print_obj(what, compress, ascii);
                output.fz_close_output();
            }
            res.fz_terminate_buffer();
            return res;
        }

        /// <summary>/ <c>extra.ll_JM_color_count</c>.</summary>
        internal static Dictionary<byte[], int> JmColorCount(mupdf.FzPixmap pm, IRect clip = null)
        {
            var rc = new Dictionary<byte[], int>(ByteArrayComparer.Instance);
            var irect = pm.fz_pixmap_bbox();
            irect = mupdf.mupdf.fz_intersect_irect(irect, JmRectFromPy(clip).fz_round_rect());
            if (irect.fz_is_empty_irect() != 0)
                return rc;
            int stride = pm.fz_pixmap_stride();
            int width = irect.x1 - irect.x0;
            int height = irect.y1 - irect.y0;
            int n = mupdf.mupdf.fz_pixmap_components(pm);
            int substride = width * n;
            int s = stride * (irect.y0 - pm.fz_pixmap_y()) + n * (irect.x0 - pm.fz_pixmap_x());
            var oldpix = JmReadPixmapSamples(pm, s, n);
            long cnt = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < substride; j += n)
                {
                    var newpix = JmReadPixmapSamples(pm, s + j, n);
                    if (!ByteArrayComparer.BytesEqual(oldpix, newpix))
                    {
                        /* Pixel differs from previous pixel, so update results with
                        last run of pixels. We get a PyObject representation of pixel
                        so we can look up in Python dict <rc>. */
                        var pixel = (byte[])oldpix.Clone();
                        if (rc.TryGetValue(pixel, out int c))
                            cnt += c;
                        rc[pixel] = (int)cnt;
                        /* Start next run of identical pixels. */
                        cnt = 1;
                        oldpix = newpix;
                    }
                    else
                    {
                        cnt += 1;
                    }
                }
                s += stride;
            }
            /* Update results with last pixel. */
            {
                var pixel = (byte[])oldpix.Clone();
                if (rc.TryGetValue(pixel, out int c))
                    cnt += c;
                rc[pixel] = (int)cnt;
            }
            return rc;
        }

        private static byte[] JmReadPixmapSamples(mupdf.FzPixmap pm, int offset, int n)
        {
            var pix = new byte[n];
            for (int i = 0; i < n; i++)
                pix[i] = (byte)pm.fz_samples_get(offset + i);
            return pix;
        }

        private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public static readonly ByteArrayComparer Instance = new ByteArrayComparer();

            public static bool BytesEqual(byte[] a, byte[] b)
            {
                if (a.Length != b.Length)
                    return false;
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i])
                        return false;
                }
                return true;
            }

            public bool Equals(byte[] x, byte[] y) => BytesEqual(x, y);

            public int GetHashCode(byte[] obj)
            {
                int hash = 17;
                for (int i = 0; i < obj.Length; i++)
                    hash = hash * 31 + obj[i];
                return hash;
            }
        }

        /// <summary>Normalizes page rotation to 0, 90, 180, or 270 degrees.</summary>
        internal static int JmNormRotation(int rotate)
        {
            // return normalized /Rotate value:one of 0, 90, 180, 270
            while (rotate < 0)
                rotate += 360;
            while (rotate >= 360)
                rotate -= 360;
            if (rotate % 90 != 0)
                return 0;
            return rotate;
        }

        /// <summary>Converts a document to PDF in memory.</summary>
        internal static byte[] JmConvertToPdf(mupdf.FzDocument doc, int fp, int tp, int rotate)
        {
            /*
            Convert any MuPDF document to a PDF
            Returns bytes object containing the PDF, created via 'write' function.
            */
            var pdfout = new mupdf.PdfDocument();
            int incr = 1;
            int s = fp;
            int e = tp;
            if (fp > tp)
            {
                incr = -1;   // count backwards
                s = tp;      // adjust ...
                e = fp;      // ... range
            }
            int rot = JmNormRotation(rotate);
            int i = fp;
            while (true)
            {
                if (!InRange(i, s, e))
                    break;
                var page = mupdf.mupdf.fz_load_page(doc, i);
                try
                {
                    var mediabox = FzBoundPage(page);
                    var (dev, resources, contents) = pdfout.pdf_page_write(mediabox);
                    mupdf.mupdf.fz_run_page(page, dev, new mupdf.FzMatrix(), new mupdf.FzCookie());
                    mupdf.mupdf.fz_close_device(dev);
                    dev.Dispose();
                    var page_obj = PdfAddPage(pdfout, mediabox, rot, resources, contents);
                    PdfInsertPage(pdfout, -1, page_obj);
                }
                finally
                {
                    page?.Dispose();
                }
                i += incr;
            }
            // PDF created - now write it to Python bytearray
            // prepare write options structure
            var opts = new mupdf.PdfWriteOptions();
            opts.do_garbage = 4;
            opts.do_compress = 1;
            opts.do_compress_images = 1;
            opts.do_compress_fonts = 1;
            opts.do_sanitize = 1;
            opts.do_incremental = 0;
            opts.do_ascii = 0;
            opts.do_decompress = 0;
            opts.do_linear = 0;
            opts.do_clean = 1;
            opts.do_pretty = 0;

            var res = mupdf.mupdf.fz_new_buffer(8192);
            var out_ = new mupdf.FzOutput(res);
            mupdf.mupdf.pdf_write_document(pdfout, out_, opts);
            out_.fz_close_output();
            byte[] c = res.fz_buffer_extract();
            return c;
        }

        /// <summary>Reads PDF page label dictionaries.</summary>
        internal static void JmGetPageLabels(List<(int pno, string rule)> liste, mupdf.PdfObj nums)
        {
            int n = nums.pdf_array_len();
            for (int i = 0; i < n; i += 2)
            {
                var key = Helpers.PdfResolveIndirect(Helpers.PdfArrayGet(nums,i));
                int pno = key.pdf_to_int();
                var val = Helpers.PdfResolveIndirect(Helpers.PdfArrayGet(nums,i + 1));
                using (var res = JmObjectToBuffer(val, 1, 0))
                {
                    var c = res.fz_buffer_extract();
                    string rule = System.Text.Encoding.UTF8.GetString(c);
                    liste.Add((pno, rule));
                }
            }
        }

        /// <summary>+ <c>JM_EscapeStrFromBuffer</c>.</summary>
        internal static string PdfObjPrintToString(mupdf.PdfObj obj, int compress, int ascii)
        {
            try
            {
                if (obj?.m_internal == null) return "";
                using (var res = JmObjectToBuffer(obj, compress, ascii))
                    return JmEscapeStrFromBuffer(res);
            }
            catch
            {
                return "";
            }
        }

        /// <summary><c>Document.xref_set_key</c>.</summary>
        internal static mupdf.PdfObj JmSetObjectValue(mupdf.PdfDocument pdf, mupdf.PdfObj obj, string key, string value)
        {
            const string eyecatcher = "fitz: replace me!";
            var list = key.Split('/');
            int len = list.Length;
            string skey = list[len - 1];
            var pathParts = new List<string>(len > 1 ? list.Take(len - 1) : Array.Empty<string>());

            var testkey = Helpers.PdfDictGetp(obj, key);
            if (testkey.m_internal == null)
            {
                while (pathParts.Count > 0)
                {
                    string t = string.Join("/", pathParts);
                    var sub = Helpers.PdfDictGetp(obj, t);
                    if (mupdf.mupdf.pdf_is_indirect(sub) != 0)
                        throw new ValueErrorException($"path to '{skey}' has indirects");
                    pathParts.RemoveAt(pathParts.Count - 1);
                }
            }

            mupdf.mupdf.pdf_dict_putp(obj, key, mupdf.mupdf.pdf_new_text_string(eyecatcher));
            testkey = Helpers.PdfDictGetp(obj, key);
            if (mupdf.mupdf.pdf_is_string(testkey) == 0)
                throw new ValueErrorException($"cannot insert value for '{key}'");
            if (mupdf.mupdf.pdf_to_text_string(testkey) != eyecatcher)
                throw new ValueErrorException($"cannot insert value for '{key}'");

            string objstr = PdfObjPrintToString(obj, 1, 0);
            string nullval = $"/{skey}({eyecatcher})";
            string newval = $"/{skey} {value}";
            string newstr = objstr.Replace(nullval, newval);
            return JM_pdf_obj_from_str(pdf, newstr);
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

                var pageref = Helpers.PdfLookupPageObj(pdf, i);
                var annots = PdfDictGets(pageref, "Annots");
                if (annots.m_internal == null)
                    continue;
                int len = mupdf.mupdf.pdf_array_len(annots);
                for (int j = len - 1; j >= 0; j--)
                {
                    var o = Helpers.PdfArrayGet(annots, j);
                    var subtype = PdfDictGets(o, "Subtype");
                    if (!PdfObjCmpName(subtype, "Link"))
                        continue;
                    var action = PdfDictGets(o, "A");
                    var dest = PdfDictGets(o, "Dest");
                    if (action.m_internal != null)
                    {
                        var actionS = PdfDictGets(action, "S");
                        if (!PdfObjCmpName(actionS, "GoTo"))
                            continue;
                        dest = PdfDictGets(action, "D");
                    }
                    int pno = -1;
                    if (mupdf.mupdf.pdf_is_array(dest) != 0)
                    {
                        var target = Helpers.PdfArrayGet(dest, 0);
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
            var annotObj = Helpers.PdfAnnotObj(annot);
            var page = PdfAnnotPage(annot);
            var it = PdfFirstAnnot(page);
            while (it.m_internal != null)
            {
                var irtObj = Helpers.PdfDictGets(Helpers.PdfAnnotObj(it), "IRT");
                if (irtObj.m_internal != null && mupdf.mupdf.pdf_objcmp(irtObj, annotObj) == 0)
                    return it;
                it = PdfNextAnnot(it);
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

            PdfObj root = PdfDictGets(Helpers.PdfTrailer(pdf), "Root");

            // remove any empty /Collection entry
            PdfObj coll = PdfDictGets(root, "Collection");
            if (coll.m_internal != null && mupdf.mupdf.pdf_dict_len(coll) == 0)
                PdfDictDel(root, "Collection");

            PdfObj efiles = PdfDictGetl(root, "Names", "EmbeddedFiles", "Names");
            if (efiles.m_internal != null)
                PdfDictPutName(root, "PageMode", "UseAttachments");
        }

        internal static void JM_ensure_identity(PdfDocument pdf)
        {
            // Store ID in PDF trailer
            PdfObj id_ = PdfDictGets(Helpers.PdfTrailer(pdf), "ID");
            if (id_.m_internal == null)
            {
                byte[] rnd0 = new byte[16];
                // Need to convert raw bytes into a str to send to
                string rnd = "";
                for (int i = 0; i < rnd0.Length; i++)
                {
                    rnd += (char)rnd0[i];
                }
                id_ = Helpers.PdfDictPutArray(Helpers.PdfTrailer(pdf), "ID", 2);
                PdfArrayPush(id_, mupdf.mupdf.pdf_new_string(rnd, (uint)rnd.Length));
                PdfArrayPush(id_, mupdf.mupdf.pdf_new_string(rnd, (uint)rnd.Length));
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
            var annotObj = Helpers.PdfAnnotObj(annot);
            var pdf = mupdf.mupdf.pdf_get_bound_document(annotObj);
            var da = Helpers.PdfDictGetsInheritable(annotObj, "DA");
            if (da.m_internal == null)
            {
                var trailer = Helpers.PdfTrailer(pdf);
                da = PdfDictGetl(trailer, "Root", "AcroForm", "DA");
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

        /// <summary>Sets annotation color (FreeText fill or border).</summary>
        internal static void PdfSetAnnotColor(mupdf.PdfAnnot annot, int n, float[] color)
        {
            if (n <= 0 || color == null || color.Length == 0)
                return;
            var pin = GCHandle.Alloc(color, GCHandleType.Pinned);
            try
            {
                var ptr = new mupdf.SWIGTYPE_p_float(pin.AddrOfPinnedObject(), false);
                mupdf.mupdf.pdf_set_annot_color(annot, n, ptr);
            }
            finally
            {
                if (pin.IsAllocated)
                    pin.Free();
            }
        }

        internal static void JM_make_annot_DA(mupdf.PdfAnnot annot, int ncol, float[] col, string fontname, float fontsize)
        {
            var sb = new StringBuilder();
            if (ncol < 1) sb.Append("0 g ");
            else if (ncol == 1) sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:g} g ", col[0]);
            else if (ncol == 3) sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:g} {1:g} {2:g} rg ", col[0], col[1], col[2]);
            else sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:g} {1:g} {2:g} {3:g} k ", col[0], col[1], col[2], col[3]);
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "/{0} {1:g} Tf", JM_expand_fname(fontname), fontsize);
            PdfDictPutTextString(Helpers.PdfAnnotObj(annot), "DA", sb.ToString());
        }

        internal static bool JM_update_appearance(
            mupdf.PdfAnnot annot,
            AnnotationType annotType,
            float opacity,
            string blendMode,
            float[] fillColor,
            int rotate)
        {
            var annotObj = Helpers.PdfAnnotObj(annot);
            int nFill = fillColor?.Length ?? 0;

            bool supportsInterior = annotType == AnnotationType.Square
                || annotType == AnnotationType.Circle
                || annotType == AnnotationType.Line
                || annotType == AnnotationType.PolyLine
                || annotType == AnnotationType.Polygon;
            if (nFill == 0 || !supportsInterior)
            {
                PdfDictDel(annotObj, "IC");
            }
            else
            {
                var col = mupdf.mupdf.pdf_new_array(mupdf.mupdf.pdf_get_bound_document(annotObj), nFill);
                for (int i = 0; i < nFill; i++)
                    mupdf.mupdf.pdf_array_push_real(col, fillColor[i]);
                PdfDictPut(annotObj, "IC", col);
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
                PdfDictPutInt(annotObj, "Rotate", rotate);

            PdfDirtyAndUpdateAnnot(annot, null);

            if ((opacity < 0 || opacity >= 1) && string.IsNullOrEmpty(blendMode))
                return true;

            var ap = PdfDictGetl(annotObj, "AP", "N");
            if (ap.m_internal == null) return true;

            var resources = PdfDictGets(ap, "Resources");
            if (resources.m_internal == null)
                resources = PdfDictPutDict(ap, "Resources", 2);

            var alp0 = mupdf.mupdf.pdf_new_dict(mupdf.mupdf.pdf_get_bound_document(annotObj), 3);
            if (opacity >= 0 && opacity < 1)
            {
                PdfDictPutReal(alp0, "CA", opacity);
                PdfDictPutReal(alp0, "ca", opacity);
                PdfDictPutReal(annotObj, "CA", opacity);
            }
            if (!string.IsNullOrEmpty(blendMode))
            {
                PdfDictPutName(alp0, "BM", blendMode);
                PdfDictPutName(annotObj, "BM", blendMode);
            }

            var extg = PdfDictGets(resources, "ExtGState");
            if (extg.m_internal == null)
                extg = PdfDictPutDict(resources, "ExtGState", 2);
            PdfDictPut(extg, "H", alp0);
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
            if (doc is Document d)
            {
                if (d.IsClosed) throw new ValueErrorException("document closed");
                if (d.IsPdf)
                    // Fresh non-owning wrapper: callers (e.g. PDF4LLM) may Dispose without breaking the cache.
                    return PdfDocumentBorrowed(d.NativePdfDocument);
                if (required) throw new InvalidOperationException(Constants.MSG_IS_NO_PDF);
                return new mupdf.PdfDocument();
            }
            if (doc is mupdf.PdfDocument pd) return pd;
            if (doc is mupdf.FzDocument fd)
            {
                var ret = PdfDocumentBorrowedFromFz(fd);
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
            if (page is mupdf.PdfPage pp)
                return PdfPageBorrowed(pp);
            if (page is mupdf.FzPage fp)
            {
                // pdf_page_from_fz_page() returns a new pdf_page* owned by the caller (must pdf_drop_page).
                // Do not use PdfPageBorrowed here — that suppresses drop and leaks the page / file handle on Windows.
                var ret = mupdf.mupdf.pdf_page_from_fz_page(fp);
                if (required && ret.m_internal == null) throw new InvalidOperationException(Constants.MSG_IS_NO_PDF);
                return ret;
            }
            throw new ArgumentException($"Unrecognised page type: {page.GetType()}");
        }

        /// <summary>Returns a fresh <c>pdf_page</c> from <c>fz_page</c> (never a cached wrapper).</summary>
        internal static mupdf.PdfPage AsPdfPageFresh(Page page, bool required = true)
        {
            page.DisposeCachedPdfPage();
            return AsPdfPage(page.NativePage, required);
        }

        /// <summary>Drop MuPDF's loaded page tree after direct xref edits (PyMuPDF relies on fresh <c>pdf_page_from_fz_page</c>).</summary>
        internal static void InvalidatePdfPageCache(mupdf.PdfDocument pdf, Page page)
        {
            mupdf.mupdf.ll_pdf_drop_page_tree_internal(pdf.m_internal);
            page.DisposeCachedPdfPage();
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
            return WithKeptFont(font, f => f.fz_font_ascender());
        }

        internal static float JM_font_descender(mupdf.fz_font font)
        {
            if (SkipQuadCorrections) return -0.2f;
            return WithKeptFont(font, f => f.fz_font_descender());
        }

        internal static string JM_font_name(mupdf.fz_font font)
        {
            return WithKeptFont(font, wrapped =>
            {
                string name = wrapped.fz_font_name();
                int s = name.IndexOf('+');
                if (SubsetFontnames || s == -1 || s != 6)
                    return name;
                return name.Substring(s + 1);
            });
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
            return WithKeptFont(font, wrapped =>
            {
                int flags = DetectSuperScript(line, ch);
                flags += wrapped.fz_font_is_italic() * TEXT_FONT_ITALIC;
                flags += wrapped.fz_font_is_serif() * TEXT_FONT_SERIFED;
                flags += wrapped.fz_font_is_monospaced() * TEXT_FONT_MONOSPACED;
                flags += wrapped.fz_font_is_bold() * TEXT_FONT_BOLD;
                return flags;
            });
        }

        /// <summary>Run a callback on a temporary <see cref="mupdf.FzFont"/> wrapper (always disposed).</summary>
        private static T WithKeptFont<T>(mupdf.fz_font font, Func<mupdf.FzFont, T> fn)
        {
            var wrapped = new mupdf.FzFont(mupdf.mupdf.ll_fz_keep_font(font));
            try
            {
                return fn(wrapped);
            }
            finally
            {
                wrapped.Dispose();
            }
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
                WithKeptFont(font, wrappedFont =>
                {
                    int glyph = mupdf.mupdf.fz_encode_character(wrappedFont, ch.c);
                    if (glyph != 0)
                    {
                        float fwidth = mupdf.mupdf.fz_advance_glyph(wrappedFont, glyph, line.wmode != 0 ? 1 : 0);
                        quad.lr.x = quad.ll.x + fwidth * fsize;
                        quad.ur.x = quad.lr.x;
                    }
                    return 0;
                });
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

        internal static string make_escape(int ch)
        {
            if (ch == 92)
                return "\\u005c";
            else if (32 <= ch && ch <= 127 || ch == 10)
                return ((char)ch).ToString();
            else if (0xd800 <= ch && ch <= 0xdfff)  // orphaned surrogate
                return "\\ufffd";
            else if (ch <= 0xffff)
                return string.Format(CultureInfo.InvariantCulture, "\\u{0:x4}", ch);
            else
                return string.Format(CultureInfo.InvariantCulture, "\\U{0:x8}", ch);
        }

        internal static string JM_copy_rectangle(mupdf.FzStextPage page, mupdf.FzRect area)
        {
            int need_new_line = 0;
            var buffer = new StringBuilder();
            // Walk via first_block/next and first_char/next — not SWIG iterators (__increment__
            // returns owning wrappers that Release GC can dispose mid-loop → AccessViolation).
            for (mupdf.fz_stext_block block = page.m_internal.first_block;
                 block != null;
                 block = block.next)
            {
                if (block.type != mupdf.mupdf.FZ_STEXT_BLOCK_TEXT)
                    continue;

                mupdf.fz_stext_line line = FirstStextLinePtr(block);
                for (; line != null; line = line.next)
                {
                    int line_had_text = 0;
                    for (mupdf.fz_stext_char ch = line.first_char;
                         ch != null;
                         ch = ch.next)
                    {
                        var r = JM_char_bbox(line, ch);
                        if (JM_rects_overlap(area, r))
                        {
                            line_had_text = 1;
                            if (need_new_line != 0)
                            {
                                buffer.Append('\n');
                                need_new_line = 0;
                            }
                            buffer.Append(make_escape(ch.c));
                        }
                    }
                    if (line_had_text != 0)
                        need_new_line = 1;
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Non-owning <see cref="mupdf.FzStextBlock"/> view of a block inside a live stext page.
        /// Do not use <c>new FzStextBlock(internal_)</c> — that wrapper is owning and its finalizer
        /// can delete in-page data while other code still walks lines/chars.
        /// </summary>
        internal static mupdf.FzStextBlock BorrowStextBlock(mupdf.fz_stext_block block)
        {
            if (block == null)
                return null;
            global::System.IntPtr cPtr = mupdf.mupdfPINVOKE.new_FzStextBlock__SWIG_2(mupdf.fz_stext_block.getCPtr(block));
            if (mupdf.mupdfPINVOKE.SWIGPendingException.Pending)
                throw mupdf.mupdfPINVOKE.SWIGPendingException.Retrieve();
            return new mupdf.FzStextBlock(cPtr, false);
        }

        /// <summary>First line of a text block; then walk <c>line.next</c>.</summary>
        internal static mupdf.fz_stext_line FirstStextLinePtr(mupdf.fz_stext_block block)
        {
            var wrap = BorrowStextBlock(block);
            var iter = wrap.begin();
            try
            {
                return iter.__deref__()?.m_internal;
            }
            finally
            {
                iter.Dispose();
            }
        }

        /// <summary>First line via a cached non-owning block view (see <see cref="TextPage"/>).</summary>
        internal static mupdf.fz_stext_line FirstStextLine(mupdf.FzStextBlock block)
        {
            var iter = block.begin();
            try
            {
                return iter.__deref__()?.m_internal;
            }
            finally
            {
                iter.Dispose();
            }
        }

        /// <summary>Decodes PDF raw Unicode escape sequences in a buffer.</summary>
        internal static string PyUnicode_DecodeRawUnicodeEscape(string s, string errors = "strict")
        {
            // FIXED: handle raw unicode escape sequences
            if (string.IsNullOrEmpty(s))
                return "";
            byte[] rc = Encoding.UTF8.GetBytes(s);
            return DecodeRawUnicodeEscapeBytes(rc, errors);
        }

        private static string DecodeRawUnicodeEscapeBytes(byte[] rc, string errors)
        {
            var sb = new StringBuilder();
            int i = 0;
            while (i < rc.Length)
            {
                if (rc[i] == (byte)'\\' && i + 1 < rc.Length)
                {
                    if (rc[i + 1] == (byte)'u' && i + 6 <= rc.Length
                        && TryParseHexBytes(rc, i + 2, 4, out int cpU))
                    {
                        sb.Append(char.ConvertFromUtf32(cpU));
                        i += 6;
                        continue;
                    }
                    if (rc[i + 1] == (byte)'U' && i + 10 <= rc.Length
                        && TryParseHexBytes(rc, i + 2, 8, out int cpBig))
                    {
                        sb.Append(char.ConvertFromUtf32(cpBig));
                        i += 10;
                        continue;
                    }
                }
                sb.Append((char)rc[i]);
                i++;
            }
            return sb.ToString();
        }

        private static bool TryParseHexBytes(byte[] data, int offset, int count, out int value)
        {
            value = 0;
            if (offset + count > data.Length)
                return false;
            for (int j = 0; j < count; j++)
            {
                int digit = data[offset + j];
                int n;
                if (digit >= (byte)'0' && digit <= (byte)'9')
                    n = digit - (byte)'0';
                else if (digit >= (byte)'a' && digit <= (byte)'f')
                    n = digit - (byte)'a' + 10;
                else if (digit >= (byte)'A' && digit <= (byte)'F')
                    n = digit - (byte)'A' + 10;
                else
                    return false;
                value = (value << 4) + n;
            }
            return true;
        }

        internal static mupdf.FzRect JM_rect_from_py(object? r) => JmRectFromPy(r);

        /// <summary>Python <c>JM_get_annot_xref_list</c> (<c>src/__init__.py</c>) for a page dictionary.</summary>
        internal static List<(int xref, int type_, string nm)> JM_get_annot_xref_list(mupdf.PdfObj pageObj)
        {
            var names = new List<(int, int, string)>();
            if (pageObj.m_internal == null)
                return names;
            var annots = PdfDictGets(pageObj, "Annots");
            if (annots.m_internal == null || mupdf.mupdf.pdf_is_array(annots) == 0)
                return names;
            int n = mupdf.mupdf.pdf_array_len(annots);
            for (int i = 0; i < n; i++)
            {
                var annot_obj = Helpers.PdfArrayGet(annots, i);
                int xref = mupdf.mupdf.pdf_to_num(annot_obj);
                var subtype = PdfDictGets(annot_obj, "Subtype");
                if (subtype.m_internal == null)
                    continue;
                var typeEnum = mupdf.mupdf.pdf_annot_type_from_string(mupdf.mupdf.pdf_to_name(subtype));
                if (typeEnum == mupdf.pdf_annot_type.PDF_ANNOT_UNKNOWN)
                    continue;
                var idObj = Helpers.PdfDictGets(annot_obj, "NM");
                string nm = idObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(idObj) : "";
                names.Add((xref, (int)typeEnum, nm));
            }
            return names;
        }

        /// <summary>Finds an annotation by xref on a page.</summary>
        internal static mupdf.PdfAnnot JmGetAnnotByXref(mupdf.PdfPage page, int xref)
        {
            int found = 0;
            var annot = PdfFirstAnnot(page);
            while (annot.m_internal != null)
            {
                if (xref == mupdf.mupdf.pdf_to_num(Helpers.PdfAnnotObj(annot)))
                {
                    found = 1;
                    break;
                }
                annot = PdfNextAnnot(annot);
            }
            if (found == 0)
                throw new Exception($"xref {xref} is not an annot of this page");
            return annot;
        }

        /// <summary>Finds an annotation by NM name on a page.</summary>
        internal static mupdf.PdfAnnot JmGetAnnotByName(mupdf.PdfPage page, string name)
        {
            if (string.IsNullOrEmpty(name))
                return new mupdf.PdfAnnot();
            int found = 0;
            var annot = PdfFirstAnnot(page);
            while (annot.m_internal != null)
            {
                var nmObj = Helpers.PdfDictGets(Helpers.PdfAnnotObj(annot), "NM");
                string response = nmObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(nmObj) : "";
                if (name == response)
                {
                    found = 1;
                    break;
                }
                annot = PdfNextAnnot(annot);
            }
            if (found == 0)
                throw new Exception($"'{name}' is not an annot of this page");
            return annot;
        }

        /// <summary>Read <c>/NM</c> for an indirect object (used for link dict <c>id</c> like PyMuPDF <c>get_links</c>).</summary>
        internal static string PdfAnnotNmForXref(mupdf.PdfDocument pdf, int xref)
        {
            if (pdf == null || pdf.m_internal == null || xref < 1)
                return "";
            try
            {
                var resolved = Helpers.PdfResolveIndirect(PdfNewIndirect(pdf, xref, 0));
                var idObj = Helpers.PdfDictGets(resolved, "NM");
                if (idObj.m_internal == null)
                    return "";
                return mupdf.mupdf.pdf_to_text_string(idObj);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>Python <c>JM_have_operation</c> (<c>src/__init__.py</c>).</summary>
        internal static bool JM_have_operation(mupdf.PdfDocument pdf)
        {
            //     return 0
            // return 1
            if (pdf?.m_internal == null)
                return true;
            if (pdf.m_internal.journal != null && string.IsNullOrEmpty(mupdf.mupdf.pdf_undoredo_step(pdf, 0)))
                return false;
            return true;
        }

        /// <summary>Python <c>ENSURE_OPERATION</c> (<c>src/__init__.py</c>).</summary>
        internal static void ENSURE_OPERATION(mupdf.PdfDocument pdf)
        {
            if (!JM_have_operation(pdf))
                throw new Exception("No journalling operation started");
        }

        /// <summary>Python <c>JM_refresh_links</c> (<c>src/__init__.py</c>).</summary>
        internal static void JM_refresh_links(mupdf.PdfDocument pdf, mupdf.PdfPage pdfPage)
        {
            if (pdf == null || pdf.m_internal == null || pdfPage == null || pdfPage.m_internal == null)
                return;
            var obj = PdfDictGets(PdfPageObj(pdfPage), "Annots");
            if (obj.m_internal == null)
                return;

            var pageMediabox = new mupdf.FzRect();
            var pageCtm = new mupdf.FzMatrix();
            pdfPage.pdf_page_transform(pageMediabox, pageCtm);
            int number = mupdf.mupdf.pdf_lookup_page_number(pdf, PdfPageObj(pdfPage));
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

        /// <summary>Space-separated PDF real formatting (Python <c>_format_g</c> / <c>fz_format_double</c>).</summary>
        /// <remarks>
        /// Do not use <c>ToString("G9")</c>: values near zero become tokens like <c>-3.67E-17</c>
        /// that MuPDF's content parser rejects, so paths (e.g. <see cref="Shape.DrawCircle"/>) fail silently.
        /// </remarks>
        internal static string FormatPdfReals(params float[] values)
        {
            if (values == null || values.Length == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(FormatPdfReal(values[i]));
            }
            return sb.ToString();
        }

        /// <summary>Single PDF real (Python <c>_format_g(value)</c>).</summary>
        internal static string FormatPdfReal(float value)
        {
            try
            {
                return mupdf.mupdf.fz_format_double("%g", value);
            }
            catch
            {
                return value.ToString("0.#########", CultureInfo.InvariantCulture);
            }
        }

        /// <summary><c>src/__init__.py</c>.</summary>
        internal static Matrix UtilHorMatrix(Point c, Point p)
        {
            var diff = p - c;
            float len = diff.Norm;
            if (len < Constants.Epsilon)
                return new Matrix(Matrix.Identity);
            float ux = diff.X / len, uy = diff.Y / len;
            var m1 = new Matrix(1, 0, 0, 1, -c.X, -c.Y);
            var m2 = new Matrix(ux, -uy, uy, ux, 0, 0);
            return m1 * m2;
        }

        /// <summary><c>src/__init__.py</c>.</summary>
        internal static Matrix CalcImageMatrix(int width, int height, Rect tr, int rotate, bool keep)
        {
            var trect = tr.ToFzRect();
            float trw = trect.x1 - trect.x0;
            float trh = trect.y1 - trect.y0;
            float w = trw;
            float h = trh;
            float fw;
            float fh;
            if (keep)
            {
                int large = Math.Max(width, height);
                fw = width / (float)large;
                fh = height / (float)large;
            }
            else
            {
                fw = 1.0f;
                fh = 1.0f;
            }
            float small = Math.Min(fw, fh);
            if (rotate != 0 && rotate != 180)
            {
                float f = fw;
                fw = fh;
                fh = f;
            }
            if (fw < 1)
            {
                if (trw / fw > trh / fh)
                {
                    w = trh * small;
                    h = trh;
                }
                else
                {
                    w = trw;
                    h = trw / small;
                }
            }
            else if (Math.Abs(fw - fh) > Constants.Epsilon)
            {
                if (trw / fw > trh / fh)
                {
                    w = trh / small;
                    h = trh;
                }
                else
                {
                    w = trw;
                    h = trw * small;
                }
            }
            else
            {
                w = trw;
                h = trh;
            }

            float cx = (float)((trect.x0 + trect.x1) * 0.5);
            float cy = (float)((trect.y0 + trect.y1) * 0.5);
            mupdf.FzMatrix mat = mupdf.mupdf.fz_make_matrix(1, 0, 0, 1, -0.5f, -0.5f);
            mat = mupdf.mupdf.fz_concat(mat, mupdf.mupdf.fz_rotate(rotate));
            mat = mupdf.mupdf.fz_concat(mat, mupdf.mupdf.fz_scale((float)w, (float)h));
            mat = mupdf.mupdf.fz_concat(mat, mupdf.mupdf.fz_translate(cx, cy));
            return new Matrix(mat);
        }

        /// <summary>MD5 hex key for stream + optional mask (Python <c>_insert_image</c> digest path).</summary>
        internal static string Md5HexKey(byte[] data, byte[]? extra = null)
        {
            using var md5 = MD5.Create();
            if (extra != null && extra.Length > 0)
            {
                md5.TransformBlock(data, 0, data.Length, null, 0);
                md5.TransformFinalBlock(extra, 0, extra.Length);
            }
            else
                md5.TransformFinalBlock(data, 0, data.Length);
            return BytesToHex(md5.Hash!);
        }

        /// <summary>MD5 hex key from <see cref="mupdf.FzPixmap.fz_md5_pixmap2"/> (PyMuPDF pixmap digest).</summary>
        internal static string Md5HexKeyFromPixmap(mupdf.FzPixmap pm)
        {
            using var digest = pm.fz_md5_pixmap2();
            var bytes = new byte[digest.Count];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = digest[i];
            return BytesToHex(bytes);
        }

        internal static string BytesToHex(byte[] bytes)
        {
            var c = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                c[i * 2] = "0123456789ABCDEF"[b >> 4];
                c[i * 2 + 1] = "0123456789ABCDEF"[b & 0xF];
            }
            return new string(c);
        }

        /// <summary><c>src/__init__.py</c>.</summary>
        internal static bool CheckMorph(Point morphFix, Matrix morphMat)
        {
            if (morphFix == null || morphMat == null)
                return false;
            // if not o[1][4] == o[1][5] == 0:
            if (Math.Abs(morphMat[4]) > Constants.Epsilon || Math.Abs(morphMat[5]) > Constants.Epsilon)
                throw new ValueErrorException("invalid morph param 1");
            return true;
        }

        /// <summary>PDF hex string for the <c>TJ</c> operator.</summary>
        internal static string GetTJstr(string text, List<(int glyph, float width)> glyphs, bool simple, int ordering)
        {
            // if text.startswith("[<") and text.endswith(">]"):  # already done
            if (text.StartsWith("[<", StringComparison.Ordinal) && text.EndsWith(">]", StringComparison.Ordinal))
                return text;
            if (string.IsNullOrEmpty(text))
                return "[<>]";

            var sb = new StringBuilder();
            sb.Append("[<");
            if (simple) // each char or its glyph is coded as a 2-byte hex
            {
                if (glyphs == null) // not Symbol, not ZapfDingbats: use char code
                {
                    foreach (var c in text)
                    {
                        int oc = c;
                        sb.Append(oc < 256 ? oc.ToString("x2", CultureInfo.InvariantCulture) : "b7");
                    }
                }
                else // Symbol or ZapfDingbats: use glyphs
                {
                    foreach (var c in text)
                    {
                        int oc = c;
                        if (oc < 256 && oc < glyphs.Count)
                        {
                            int g = glyphs[oc].glyph;
                            sb.Append(g.ToString("x2", CultureInfo.InvariantCulture));
                        }
                        else sb.Append("b7");
                    }
                }
            }
            else // non-simple fonts: each char or its glyph is coded as 4-byte hex
            {
                foreach (var c in text)
                {
                    int oc = c;
                    if (ordering < 0) // not a CJK font: use the glyphs
                    {
                        if (glyphs == null || oc >= glyphs.Count)
                            throw new ValueErrorException("bad glyph lookup for non-simple font");
                        sb.Append(glyphs[oc].glyph.ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else // CJK: use the char codes
                        sb.Append(oc.ToString("x4", CultureInfo.InvariantCulture));
                }
            }

            sb.Append(">]");
            return sb.ToString();
        }

        internal static bool TryCoerceRect(object o, out Rect r)
        {
            r = default;
            if (o == null) return false;
            if (o is Rect rr) { r = rr; return true; }
            if (o is IRect ir) { r = ir.Rect; return true; }
            if (o is IList list && list.Count >= 4)
            {
                try
                {
                    r = new Rect((float)Convert.ToDouble(list[0], CultureInfo.InvariantCulture),
                        (float)Convert.ToDouble(list[1], CultureInfo.InvariantCulture),
                        (float)Convert.ToDouble(list[2], CultureInfo.InvariantCulture),
                        (float)Convert.ToDouble(list[3], CultureInfo.InvariantCulture));
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
                    p = new Point((float)Convert.ToDouble(list[0], CultureInfo.InvariantCulture),
                        (float)Convert.ToDouble(list[1], CultureInfo.InvariantCulture));
                    return true;
                }
                catch { return false; }
            }
            return false;
        }

        /// <summary>
        /// Return list of outline xref numbers. Recursive function. Arguments:
        /// obj first OL item
        /// xrefs empty list
        /// </summary>
        internal static List<int> JM_outline_xrefs(mupdf.PdfObj obj, List<int> xrefs)
        {
            if (obj == null || obj.m_internal == null)
                return xrefs;
            var thisobj = obj;
            while (thisobj != null && thisobj.m_internal != null)
            {
                int newxref = mupdf.mupdf.pdf_to_num(thisobj);
                var typeObj = PdfDictGets(thisobj, "Type");
                if (xrefs.Contains(newxref) || typeObj.m_internal != null)
                {
                    // circular ref or top of chain: terminate
                    break;
                }
                xrefs.Add(newxref);
                var first = PdfDictGets(thisobj, "First"); // try go down
                if (mupdf.mupdf.pdf_is_dict(first) != 0)
                    xrefs = JM_outline_xrefs(first, xrefs);
                thisobj = PdfDictGets(thisobj, "Next"); // try go next
                var parent = PdfDictGets(thisobj, "Parent"); // get parent
                if (mupdf.mupdf.pdf_is_dict(thisobj) == 0)
                    thisobj = parent;
            }
            return xrefs;
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

        /// <summary>Python <c>utils.getLinkText</c> — empty string if link kind is not supported.</summary>
        internal static string GetLinkText(Page page, Dictionary<string, object> lnk)
        {
            if (TryBuildInsertLinkAnnotObjectString(page, lnk, out var dictionarySource))
                return dictionarySource ?? "";
            return "";
        }

        /// <summary>Python <c>utils.getLinkText</c>: build a link annotation dictionary stream (with <c>/NM</c>) for <c>Page.insert_link</c>.</summary>
        internal static bool TryBuildInsertLinkAnnotObjectString(Page page, Dictionary<string, object> lnk, out string dictionarySource)
        {
            dictionarySource = null;
            if (page == null || lnk == null) return false;
            lnk = CopyLinkDictForBuild(lnk);
            if (!lnk.TryGetValue("kind", out var kindObj)) return false;
            int kind = Convert.ToInt32(kindObj, CultureInfo.InvariantCulture);
            if (kind == Constants.LinkNone) return false;
            if (!TryCoerceRect(lnk.TryGetValue("from", out var fromO) ? fromO : null, out var fromRect))
                return false;

            var inv = page.TransformationMatrix.Inverted() ?? Matrix.Identity;
            var rPdf = new Rect(fromRect).Transform(inv);
            string rectStr = FormatPdfReals(rPdf.X0, rPdf.Y0, rPdf.X1, rPdf.Y1);

            var doc = page.RequireParent();
            var pdfPage = page.NativePdfPage;
            var pdf = doc.NativePdfDocument;

            var linkXrefToNm = new Dictionary<int, string>();
            foreach (var t in JM_get_annot_xref_list(PdfPageObj(pdfPage)))
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
                    var candidate = $"{Utils.ANNOT_ID_STEM}-L{i}";
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
                case Constants.LinkGoto:
                    if (!lnk.TryGetValue("page", out var pageO)) return false;
                    int pno = Convert.ToInt32(pageO, CultureInfo.InvariantCulture);
                    if (pno >= 0)
                    {
                        int pageXref = doc.PageXref(pno);
                        var destPage = doc[pno];
                        var destInv = destPage.TransformationMatrix.Inverted() ?? Matrix.Identity;
                        var toPt = new Point(0, 0);
                        if (lnk.TryGetValue("to", out var toO) && toO != null
                            && TryCoercePoint(toO, out var coercedTo))
                            toPt = coercedTo;
                        toPt = new Point(toPt).Transform(destInv);
                        float zoom = 0;
                        if (lnk.TryGetValue("zoom", out var zO))
                            zoom = (float)Convert.ToDouble(zO, CultureInfo.InvariantCulture);
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

                case Constants.LinkUri:
                    if (!lnk.TryGetValue("uri", out var uriO) || uriO == null) return false;
                    var uriStr = uriO.ToString() ?? "";
                    if (string.IsNullOrEmpty(uriStr)) return false;
                    annot = "<</A<</S/URI/URI" + GetPdfStr(uriStr) + ">>/Rect[" + rectStr + "]/BS<</W 0>>/Subtype/Link>>";
                    break;

                case Constants.LinkLaunch:
                    if (!lnk.TryGetValue("file", out var launchFile) || launchFile == null) return false;
                    var lf = GetPdfStr(launchFile.ToString() ?? "");
                    annot = "<</A<</S/Launch/F<</F" + lf + "/UF" + lf + "/Type/Filespec>>>>/Rect[" + rectStr + "]/BS<</W 0>>/Subtype/Link>>";
                    break;

                case Constants.LinkGotor:
                    if (!lnk.TryGetValue("file", out var gf) || gf == null) return false;
                    var fspec = GetPdfStr(gf.ToString() ?? "");
                    if (!lnk.TryGetValue("page", out var gpO)) return false;
                    int gp = Convert.ToInt32(gpO, CultureInfo.InvariantCulture);
                    if (gp >= 0)
                    {
                        var toG = new Point(0, 0);
                        if (lnk.TryGetValue("to", out var toGo) && toGo != null
                            && TryCoercePoint(toGo, out var coercedG))
                            toG = coercedG;
                        float zg = 0;
                        if (lnk.TryGetValue("zoom", out var zgO))
                            zg = (float)Convert.ToDouble(zgO, CultureInfo.InvariantCulture);
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

                case Constants.LinkNamed:
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
            var fs = Helpers.PdfResolveIndirect(f);
            if (fs.m_internal == null) return "";
            var f1 = PdfDictGets(fs, "F");
            if (f1.m_internal != null)
                return mupdf.mupdf.pdf_to_text_string(f1);
            var uf = PdfDictGets(fs, "UF");
            return uf.m_internal != null ? mupdf.mupdf.pdf_to_text_string(uf) : "";
        }

        static bool ApplyGoToDestDict(mupdf.PdfDocument pdf, Dictionary<string, object> d, mupdf.PdfObj dx)
        {
            if (dx.m_internal == null) return false;
            var r = Helpers.PdfResolveIndirect(dx);
            if (mupdf.mupdf.pdf_is_array(r) == 0) return false;
            int len = mupdf.mupdf.pdf_array_len(r);
            if (len < 1) return false;
            var pageObj = Helpers.PdfArrayGet(r, 0);
            int pno = mupdf.mupdf.pdf_lookup_page_number(pdf, pageObj);
            d["page"] = pno;
            d["to"] = new Point(0, 0);
            d["zoom"] = 0.0;
            if (len >= 2)
            {
                var m0 = Helpers.PdfArrayGet(r, 1);
                if (mupdf.mupdf.pdf_is_name(m0) != 0)
                {
                    var mode = mupdf.mupdf.pdf_to_name(m0);
                    if (mode == "XYZ" && len >= 5)
                    {
                        d["to"] = new Point(
                            mupdf.mupdf.pdf_to_real(Helpers.PdfArrayGet(r, 2)),
                            mupdf.mupdf.pdf_to_real(Helpers.PdfArrayGet(r, 3)));
                        d["zoom"] = mupdf.mupdf.pdf_to_real(Helpers.PdfArrayGet(r, 4));
                    }
                    else if (mode == "Fit" || mode == "FitB")
                    {
                        // whole page; leave to (0,0), zoom 0
                    }
                    else if ((mode == "FitH" || mode == "FitBH") && len >= 3)
                    {
                        float top = mupdf.mupdf.pdf_to_real(Helpers.PdfArrayGet(r, 2));
                        d["to"] = new Point(0, top);
                    }
                    else if ((mode == "FitV" || mode == "FitBV") && len >= 3)
                    {
                        float left = mupdf.mupdf.pdf_to_real(Helpers.PdfArrayGet(r, 2));
                        d["to"] = new Point(left, 0);
                    }
                    else if (mode == "FitR" && len >= 6)
                    {
                        float left = mupdf.mupdf.pdf_to_real(Helpers.PdfArrayGet(r, 2));
                        float top = mupdf.mupdf.pdf_to_real(Helpers.PdfArrayGet(r, 5));
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
                var r = Helpers.PdfResolveIndirect(dx);
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
                var inner = PdfDictGets(r, "D");
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
                var rdx = Helpers.PdfResolveIndirect(dx);
                if (mupdf.mupdf.pdf_is_array(rdx) != 0)
                {
                    if (ApplyGoToDestDict(pdf, d, dx))
                        d["kind"] = Constants.LinkGoto;
                    return;
                }
                if (mupdf.mupdf.pdf_is_name(rdx) != 0 || mupdf.mupdf.pdf_is_string(rdx) != 0)
                {
                    d["kind"] = Constants.LinkNamed;
                    d["page"] = -1;
                    d["name"] = mupdf.mupdf.pdf_is_name(rdx) != 0
                        ? mupdf.mupdf.pdf_to_name(rdx)
                        : mupdf.mupdf.pdf_to_text_string(rdx);
                    return;
                }
                if (mupdf.mupdf.pdf_is_dict(rdx) == 0) return;
                var inner = PdfDictGets(rdx, "D");
                if (inner.m_internal == null) return;
                dx = inner;
            }
        }

        /// <summary>Port of <c>utils.getLinkDict</c> — used by <see cref="Page.GetLinks"/> like PyMuPDF.</summary>
        internal static Dictionary<string, object> GetLinkDict(Link ln, Document document)
            => GetLinkDictFromDest(ln.Dest, ln.Rect, document);

        /// <summary>Port of <c>utils.getLinkDict</c> for outline items (<c>Outline.destination</c>).</summary>
        internal static Dictionary<string, object> GetLinkDict(Outline ln, Document document)
            => GetLinkDictFromDest(ln.Destination(document), null, document);

        static Dictionary<string, object> GetLinkDictFromDest(LinkDest dest, Rect fromRect, Document document)
        {
            var nl = new Dictionary<string, object> { ["kind"] = dest.Kind, ["xref"] = 0 };
            if (fromRect != null)
            {
                try { nl["from"] = fromRect; }
                catch { /* Python: except on ln.rect */ }
            }

            var pnt = new Point(0, 0);
            if ((dest.Flags & Constants.LinkFlagLValid) != 0)
                pnt.X = dest.Lt.X;
            if ((dest.Flags & Constants.LinkFlagTValid) != 0)
                pnt.Y = dest.Lt.Y;

            if (dest.Kind == Constants.LinkUri)
                nl["uri"] = dest.Uri ?? "";
            else if (dest.Kind == Constants.LinkGoto)
            {
                nl["page"] = dest.Page;
                nl["to"] = pnt;
                nl["zoom"] = (dest.Flags & Constants.LinkFlagRIsZoom) != 0 ? dest.Rb.X : 0.0;
            }
            else if (dest.Kind == Constants.LinkGotor)
            {
                nl["file"] = (dest.FileSpec ?? "").Replace("\\", "/");
                nl["page"] = dest.Page;
                if (dest.Page < 0)
                    nl["to"] = dest.DestStr;
                else
                {
                    nl["to"] = pnt;
                    nl["zoom"] = (dest.Flags & Constants.LinkFlagRIsZoom) != 0 ? dest.Rb.X : 0.0;
                }
            }
            else if (dest.Kind == Constants.LinkLaunch)
                nl["file"] = (string.IsNullOrEmpty(dest.FileSpec) ? dest.Uri : dest.FileSpec).Replace("\\", "/");
            else if (dest.Kind == Constants.LinkNamed)
            {
                foreach (var kv in dest.Named)
                    nl[kv.Key] = kv.Value;
                if (nl.TryGetValue("to", out var toVal) && toVal != null && toVal is not Point
                    && toVal is IEnumerable<object> seq)
                {
                    var vals = seq.Select(o => (float)Convert.ToDouble(o, CultureInfo.InvariantCulture)).ToList();
                    if (vals.Count >= 2)
                        nl["to"] = new Point(vals[0], vals[1]);
                }
            }
            else
                nl["page"] = dest.Page;

            return nl;
        }

        /// <summary>Best-effort enrichment from the PDF link annotation (<c>/A</c>, <c>/Dest</c>); not used for <see cref="Page.GetLinks"/> listing.</summary>
        internal static void EnrichLinkDictFromPdfAnnot(mupdf.PdfDocument pdf, int xref, Dictionary<string, object> d)
        {
            if (pdf == null || pdf.m_internal == null || xref < 1 || d == null) return;
            try
            {
                var annot = Helpers.PdfResolveIndirect(PdfNewIndirect(pdf, xref, 0));
                if (annot.m_internal == null) return;
                var a = PdfDictGets(annot, "A");
                var destKey = PdfDictGets(annot, "Dest");

                if (a.m_internal != null)
                {
                    var sObj = PdfDictGets(a, "S");
                    if (sObj.m_internal == null) return;
                    var s = mupdf.mupdf.pdf_to_name(sObj);
                    if (s == "URI")
                    {
                        var uriObj = PdfDictGets(a, "URI");
                        d["kind"] = Constants.LinkUri;
                        d["uri"] = uriObj.m_internal != null ? mupdf.mupdf.pdf_to_text_string(uriObj) : "";
                        d["page"] = -1;
                        return;
                    }
                    if (s == "Launch")
                    {
                        var f = PdfDictGets(a, "F");
                        d["kind"] = Constants.LinkLaunch;
                        d["file"] = (ExtractFilespecPath(f) ?? "").Replace("\\", "/");
                        d["page"] = -1;
                        return;
                    }
                    if (s == "GoToR")
                    {
                        d["kind"] = Constants.LinkGotor;
                        var f = PdfDictGets(a, "F");
                        d["file"] = (ExtractFilespecPath(f) ?? "").Replace("\\", "/");
                        var dx = PdfDictGets(a, "D");
                        ApplyGoToRDestDict(pdf, d, dx);
                        return;
                    }
                    if (s == "GoTo")
                    {
                        var dx = PdfDictGets(a, "D");
                        ApplyGoToDestinationOperand(pdf, d, dx);
                        return;
                    }
                    if (s == "JavaScript")
                    {
                        d["kind"] = Constants.LinkNone;
                        d["page"] = -1;
                        return;
                    }
                    if (s == "GoToE")
                    {
                        d["kind"] = Constants.LinkNone;
                        d["page"] = -1;
                        var fE = PdfDictGets(a, "F");
                        if (fE.m_internal != null)
                            d["file"] = (ExtractFilespecPath(fE) ?? "").Replace("\\", "/");
                        return;
                    }
                    if (s == "Named")
                    {
                        var nObj = PdfDictGets(a, "N");
                        if (nObj.m_internal == null)
                        {
                            d["kind"] = Constants.LinkNone;
                            d["page"] = -1;
                            return;
                        }
                        string nStr = mupdf.mupdf.pdf_is_name(nObj) != 0
                            ? mupdf.mupdf.pdf_to_name(nObj)
                            : mupdf.mupdf.pdf_to_text_string(nObj);
                        d["kind"] = Constants.LinkNamed;
                        d["page"] = -1;
                        d["name"] = nStr;
                        d["named_action"] = nStr;
                        d["is_viewer_named_action"] = true;
                        return;
                    }
                    if (s == "ResetForm")
                    {
                        d["kind"] = Constants.LinkNone;
                        d["page"] = -1;
                        return;
                    }
                    if (s == "SubmitForm")
                    {
                        d["kind"] = Constants.LinkNone;
                        d["page"] = -1;
                        var fSf = PdfDictGets(a, "F");
                        if (fSf.m_internal != null)
                            d["file"] = (ExtractFilespecPath(fSf) ?? "").Replace("\\", "/");
                        return;
                    }
                    if (s == "Thread")
                    {
                        d["kind"] = Constants.LinkNone;
                        d["page"] = -1;
                        return;
                    }
                    return;
                }

                if (destKey.m_internal != null)
                {
                    if (ApplyGoToDestDict(pdf, d, destKey))
                        d["kind"] = Constants.LinkGoto;
                    else
                    {
                        var rd = Helpers.PdfResolveIndirect(destKey);
                        if (mupdf.mupdf.pdf_is_name(rd) != 0 || mupdf.mupdf.pdf_is_string(rd) != 0)
                        {
                            d["kind"] = Constants.LinkNamed;
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
            var annots = PdfDictGets(PdfPageObj(pdfPage), "Annots");
            if (annots.m_internal == null)
            {
                Helpers.PdfDictPutArray(PdfPageObj(pdfPage), "Annots", 1);
                annots = PdfDictGets(PdfPageObj(pdfPage), "Annots");
            }

            var parsed = JM_pdf_obj_from_str(pdf, text);
            var annot = PdfAddObject(pdf, parsed);
            var indObj = PdfNewIndirect(pdf, mupdf.mupdf.pdf_to_num(annot), 0);
            PdfArrayPush(annots, indObj);
        }

        /// <summary>Builds <c>PdfFilterOptions</c> and keeps the sanitize factory alive for the filter lifetime.</summary>
        internal readonly struct PdfFilterOptionsRef
        {
            public mupdf.PdfFilterOptions Filter { get; }
            readonly mupdf.PdfFilterFactory2 _factory;

            internal PdfFilterOptionsRef(mupdf.PdfFilterOptions filter, mupdf.PdfFilterFactory2 factory)
            {
                Filter = filter;
                _factory = factory;
            }
        }

        /// <summary>Reports image resource names and quads from page contents.</summary>
        internal static List<(string name, mupdf.FzQuad quad)> JmImageReporter(mupdf.PdfPage page)
        {
            var entries = new List<(string, mupdf.FzQuad)>();
            var pageMatrix = new mupdf.FzMatrix();
            var mediabox = new mupdf.FzRect();
            mupdf.mupdf.pdf_page_transform(page, mediabox, pageMatrix);

            var sopts = new ImageReporterSanitizeOptions(entries, pageMatrix);
            var filterPkg = MakePdfFilterOptions(
                instance_forms: 1, ascii: 1, no_update: 1, sanitize: 1, sopts: sopts);
            var pdf = PdfDocumentForPdfPage(page);
            mupdf.mupdf.pdf_filter_page_contents(pdf, page, filterPkg.Filter);
            return entries;
        }

        /// <summary>Returns a <see cref="mupdf.PdfFilterOptions"/> instance (PyMuPDF <c>_make_PdfFilterOptions</c>).</summary>
        internal static PdfFilterOptionsRef MakePdfFilterOptions(
            int recurse = 0,
            int instance_forms = 0,
            int ascii = 0,
            int no_update = 0,
            int sanitize = 0,
            mupdf.PdfSanitizeFilterOptions sopts = null)
        {
            var filter_ = new mupdf.PdfFilterOptions();
            filter_.recurse = recurse;
            filter_.instance_forms = instance_forms;
            filter_.ascii = ascii;
            filter_.no_update = no_update;
            mupdf.PdfFilterFactory2 factory = null;
            if (sanitize != 0)
            {
                if (sopts == null)
                    sopts = new mupdf.PdfSanitizeFilterOptions();
                factory = new SanitizePdfFilterFactory(sopts);
                filter_.add_factory(factory.internal_());
            }
            return new PdfFilterOptionsRef(filter_, factory);
        }

        /// <summary>Returns the page MediaBox as a <see cref="Rect"/>.</summary>
        internal static mupdf.FzRect JmMediabox(mupdf.PdfObj pageObj)
        {
            var mediabox = mupdf.mupdf.pdf_to_rect(
                Helpers.PdfDictGetsInheritable(pageObj, "MediaBox"));
            if (mupdf.mupdf.fz_is_empty_rect(mediabox) != 0 || mupdf.mupdf.fz_is_infinite_rect(mediabox) != 0)
            {
                mediabox.x0 = 0;
                mediabox.y0 = 0;
                mediabox.x1 = 612;
                mediabox.y1 = 792;
            }
            return mupdf.mupdf.fz_make_rect(
                Math.Min(mediabox.x0, mediabox.x1),
                Math.Min(mediabox.y0, mediabox.y1),
                Math.Max(mediabox.x0, mediabox.x1),
                Math.Max(mediabox.y0, mediabox.y1));
        }

        /// <summary>Returns the page CropBox as a <see cref="Rect"/>.</summary>
        internal static mupdf.FzRect JmCropbox(mupdf.PdfObj pageObj)
        {
            var mediabox = JmMediabox(pageObj);
            var cropbox = mupdf.mupdf.pdf_to_rect(
                Helpers.PdfDictGetsInheritable(pageObj, "CropBox"));
            if (mupdf.mupdf.fz_is_infinite_rect(cropbox) != 0 || mupdf.mupdf.fz_is_empty_rect(cropbox) != 0)
                cropbox = mediabox;
            float y0 = mediabox.y1 - cropbox.y1;
            float y1 = mediabox.y1 - cropbox.y0;
            cropbox.y0 = y0;
            cropbox.y1 = y1;
            return cropbox;
        }

        /// <summary>Converts rectangle-like input to <see cref="Rect"/>.</summary>
        internal static mupdf.FzRect JmRectFromPy(object? r)
        {
            if (r is mupdf.FzRect fz)
                return fz;
            if (r is mupdf.FzIrect irect)
                return mupdf.mupdf.fz_make_rect(irect.x0, irect.y0, irect.x1, irect.y1);
            if (r is Rect rect)
                return rect.ToFzRect();
            if (r is IRect ir)
                return new Rect(ir).ToFzRect();
            if (r == null || r is not System.Collections.IEnumerable || r is string)
                return new mupdf.FzRect(mupdf.mupdf.fz_infinite_rect);
            return RectFromPy(r).ToFzRect();
        }

        /// <summary>
        /// Version of fz_new_pixmap_from_display_list (util.c) to also support
        /// rendering of only the 'clip' part of the displaylist rectangle.
        /// </summary>
        /// <remarks>PyMuPDF equivalent: <c>JM_pixmap_from_display_list</c>.</remarks>
        internal static Pixmap JmPixmapFromDisplayList(
            mupdf.FzDisplayList list_,
            Matrix? ctm,
            mupdf.FzColorspace cs,
            int alpha,
            object? clip,
            mupdf.FzSeparations? seps)
        {
            if (seps == null)
                seps = new mupdf.FzSeparations();

            var rect = mupdf.mupdf.fz_bound_display_list(list_);
            var matrix = MatrixToFz(ctm);
            var rclip = JmRectFromPy(clip);
            rect = mupdf.mupdf.fz_intersect_rect(rect, rclip);

            rect = rect.fz_transform_rect(matrix);
            var irect = rect.fz_round_rect();

            var pix = cs.fz_new_pixmap_with_bbox(irect, seps, alpha);
            if (alpha != 0)
                pix.fz_clear_pixmap();
            else
                pix.fz_clear_pixmap_with_value(0xFF);

            mupdf.FzDevice dev;
            if (mupdf.mupdf.fz_is_infinite_rect(rclip) == 0)
            {
                dev = mupdf.mupdf.fz_new_draw_device_with_bbox(matrix, pix, irect);
                mupdf.mupdf.fz_run_display_list(
                    list_, dev, new mupdf.FzMatrix(), rclip, new mupdf.FzCookie());
            }
            else
            {
                dev = mupdf.mupdf.fz_new_draw_device(matrix, pix);
                mupdf.mupdf.fz_run_display_list(
                    list_, dev, new mupdf.FzMatrix(),
                    new mupdf.FzRect(mupdf.mupdf.fz_infinite_rect),
                    new mupdf.FzCookie());
            }

            dev.fz_close_device();
            dev.Dispose();
            // Use special raw Pixmap constructor so we don't set alpha to true.
            return new Pixmap(pix);
        }

        /// <summary><c>src/__init__.py</c>.</summary>
        internal static string JM_UnicodeFromBuffer(mupdf.FzBuffer buff)
        {
            if (buff?.m_internal == null)
                return "";
            byte[] buff_bytes = buff.fz_buffer_extract();
            string val = System.Text.Encoding.UTF8.GetString(buff_bytes);
            // z = val.find(chr(0))
            int z = val.IndexOf('\0');
            // if z >= 0:
            if (z >= 0)
                val = val.Substring(0, z);
            // return val
            return val;
        }

        /// <summary><c>JM_append_rune</c> — non-ASCII as <c>\uXXXX</c> in buffer.</summary>
        internal static void JmAppendRune(mupdf.FzBuffer buff, int ch)
        {
            if (ch == 92)
                buff.fz_append_string("\\u005c");
            else if ((ch >= 32 && ch <= 127) || ch == 10)
                mupdf.mupdf.fz_append_byte(buff, (byte)ch);
            else if (ch >= 0xd800 && ch <= 0xdfff)
                buff.fz_append_string("\\ufffd");
            else if (ch <= 0xffff)
                buff.fz_append_string(string.Format(System.Globalization.CultureInfo.InvariantCulture, "\\u{0:x4}", ch));
            else
                buff.fz_append_string(string.Format(System.Globalization.CultureInfo.InvariantCulture, "\\U{0:x8}", ch));
        }

        /// <summary><c>PyUnicode_DecodeRawUnicodeEscape</c>.</summary>
        internal static string JmEscapeStrFromBuffer(mupdf.FzBuffer buff)
        {
            if (buff?.m_internal == null)
                return "";
            return DecodeRawUnicodeEscapeBytes(buff.fz_buffer_extract(), "replace");
        }

        /// <summary>Merges PDF resource dictionaries.</summary>
        internal static (int maxAlp, int maxFonts) JmMergeResources(mupdf.PdfPage page, mupdf.PdfObj tempRes)
        {
            var resources = PdfDictGets(PdfPageObj(page), "Resources");
            if (resources.m_internal == null)
                resources = Helpers.PdfDictPutDict(PdfPageObj(page), "Resources", 5);

            var mainExtg = PdfDictGets(resources, "ExtGState");
            var mainFonts = PdfDictGets(resources, "Font");
            var tempExtg = PdfDictGets(tempRes, "ExtGState");
            var tempFonts = PdfDictGets(tempRes, "Font");

            int maxAlp = -1;
            int maxFonts = -1;

            if (mupdf.mupdf.pdf_is_dict(tempExtg) != 0)
            {
                int n = mupdf.mupdf.pdf_dict_len(tempExtg);
                if (mupdf.mupdf.pdf_is_dict(mainExtg) != 0)
                {
                    for (int i = 0; i < mupdf.mupdf.pdf_dict_len(mainExtg); i++)
                    {
                        string alp = mupdf.mupdf.pdf_to_name(Helpers.PdfDictGetKey(mainExtg, i)) ?? "";
                        if (!alp.StartsWith("Alp", StringComparison.Ordinal))
                            continue;
                        int j = mupdf.mupdf.fz_atoi(alp.Substring(3));
                        if (j > maxAlp)
                            maxAlp = j;
                    }
                }
                else
                    mainExtg = Helpers.PdfDictPutDict(resources, "ExtGState", n);

                maxAlp++;
                for (int i = 0; i < n; i++)
                {
                    string alp = mupdf.mupdf.pdf_to_name(Helpers.PdfDictGetKey(tempExtg, i)) ?? "";
                    int j = mupdf.mupdf.fz_atoi(alp.Length > 3 ? alp.Substring(3) : "0") + maxAlp;
                    var val = Helpers.PdfDictGetVal(tempExtg, i);
                    Helpers.PdfDictPuts(mainExtg, $"Alp{j}", val);
                }
            }

            if (mupdf.mupdf.pdf_is_dict(mainFonts) != 0)
            {
                for (int i = 0; i < mupdf.mupdf.pdf_dict_len(mainFonts); i++)
                {
                    string font = mupdf.mupdf.pdf_to_name(Helpers.PdfDictGetKey(mainFonts, i)) ?? "";
                    if (!font.StartsWith("F", StringComparison.Ordinal))
                        continue;
                    int j = mupdf.mupdf.fz_atoi(font.Length > 1 ? font.Substring(1) : "0");
                    if (j > maxFonts)
                        maxFonts = j;
                }
            }
            else
                mainFonts = Helpers.PdfDictPutDict(resources, "Font", 2);

            maxFonts++;
            for (int i = 0; i < mupdf.mupdf.pdf_dict_len(tempFonts); i++)
            {
                string font = mupdf.mupdf.pdf_to_name(Helpers.PdfDictGetKey(tempFonts, i)) ?? "";
                int j = mupdf.mupdf.fz_atoi(font.Length > 1 ? font.Substring(1) : "0") + maxFonts;
                var val = Helpers.PdfDictGetVal(tempFonts, i);
                Helpers.PdfDictPuts(mainFonts, $"F{j}", val);
            }
            return (maxAlp, maxFonts);
        }

        /// <summary>Python <c>JM_merge_range</c> (<c>src/__init__.py</c>).</summary>
        internal static void JmMergeRange(
            mupdf.PdfDocument docDes,
            mupdf.PdfDocument docSrc,
            int spage,
            int epage,
            int apage,
            int rotate,
            bool links,
            bool annots,
            int showProgress,
            mupdf.PdfGraftMap graftMap)
        {
            int afterpage = apage;
            int counter = 0;  // copied pages counter
            int total = mupdf.mupdf.fz_absi(epage - spage) + 1;   // total pages to copy

            if (spage < epage)
            {
                int page = spage;
                while (page <= epage)
                {
                    PageMerge(docDes, docSrc, page, afterpage, rotate, links, annots, graftMap);
                    counter += 1;
                    if (showProgress > 0 && counter % showProgress == 0)
                        message($"Inserted {counter} of {total} pages.");
                    page += 1;
                    afterpage += 1;
                }
            }
            else
            {
                int page = spage;
                while (page >= epage)
                {
                    PageMerge(docDes, docSrc, page, afterpage, rotate, links, annots, graftMap);
                    counter += 1;
                    if (showProgress > 0 && counter % showProgress == 0)
                        message($"Inserted {counter} of {total} pages.");
                    page -= 1;
                    afterpage += 1;
                }
            }
        }

        /// <summary>Python <c>page_merge</c> (<c>src/__init__.py</c>).</summary>
        internal static void PageMerge(
            mupdf.PdfDocument docDes,
            mupdf.PdfDocument docSrc,
            int pageFrom,
            int pageTo,
            int rotate,
            bool links,
            bool copyAnnots,
            mupdf.PdfGraftMap graftMap)
        {
            // list of object types (per page) we want to copy
            var knownPageObjKeys = new[]
            {
                "Contents", "Resources", "MediaBox", "CropBox", "BleedBox",
                "TrimBox", "ArtBox", "Rotate", "UserUnit",
            };
            var pageRef = Helpers.PdfLookupPageObj(docSrc, pageFrom);

            // make new page dict in dest doc
            var pageDict = mupdf.mupdf.pdf_new_dict(docDes, 4);
            PdfDictPutName(pageDict, "Type", "Page");

            // copy objects of source page into it
            for (int i = 0; i < knownPageObjKeys.Length; i++)
            {
                var obj = PdfDictGetsInheritable(pageRef, knownPageObjKeys[i]);
                if (obj.m_internal != null)
                    PdfDictPut(pageDict, knownPageObjKeys[i], PdfGraftMappedObject(graftMap, obj));
            }

            // Copy annotations, but skip Link, Popup, IRT, Widget types
            // If selected, remove dict keys P (parent) and Popup
            if (copyAnnots)
            {
                var oldAnnots = PdfDictGets(pageRef, "Annots");
                int n = mupdf.mupdf.pdf_array_len(oldAnnots);
                if (n > 0)
                {
                    var newAnnots = Helpers.PdfDictPutArray(pageDict, "Annots", n);
                    for (int i = 0; i < n; i++)
                    {
                        var o = Helpers.PdfArrayGet(oldAnnots, i);
                        if (o.m_internal == null || mupdf.mupdf.pdf_is_dict(o) == 0)
                            continue;    // skip non-dict items
                        if (Helpers.PdfDictGets(o, "IRT").m_internal != null)
                            continue;
                        var subtype = PdfDictGets(o, "Subtype");
                        if (PdfNameEq(subtype, "Link"))
                            continue;
                        if (PdfNameEq(subtype, "Popup"))
                            continue;
                        if (PdfNameEq(subtype, "Widget"))
                            continue;
                        PdfDictDel(o, "Popup");
                        PdfDictDel(o, "P");
                        var copyO = PdfGraftMappedObject(graftMap, o);
                        var annot = PdfNewIndirect(docDes, mupdf.mupdf.pdf_to_num(copyO), 0);
                        PdfArrayPush(newAnnots, annot);
                    }
                }
            }

            // rotate the page
            if (rotate != -1)
                PdfDictPutInt(pageDict, "Rotate", rotate);
            // Now add the page dictionary to dest PDF
            var pageRefOut = PdfAddObject(docDes, pageDict);

            // Insert new page at specified location
            mupdf.mupdf.pdf_insert_page(docDes, pageTo, pageRefOut);
        }

        /// <summary>Collects image placements via sanitize <c>image_filter</c> (PyMuPDF <c>JM_image_filter</c>).</summary>
        sealed class ImageReporterSanitizeOptions : mupdf.PdfSanitizeFilterOptions2
        {
            readonly List<(string name, mupdf.FzQuad quad)> _entries;
            readonly mupdf.FzMatrix _pageMatrix;

            internal ImageReporterSanitizeOptions(List<(string name, mupdf.FzQuad quad)> entries, mupdf.FzMatrix pageMatrix)
            {
                _entries = entries;
                _pageMatrix = pageMatrix;
                use_virtual_image_filter();
            }

            public override mupdf.fz_image image_filter(mupdf.fz_context ctx, mupdf.fz_matrix ctm, string name, mupdf.fz_image image, mupdf.fz_rect scissor)
            {
                var r = new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_UNIT);
                var q = r.fz_quad_from_rect();
                q = q.fz_transform_quad(new mupdf.FzMatrix(ctm));
                q = q.fz_transform_quad(_pageMatrix);
                _entries.Add((name, q));
                return image;
            }
        }

        // ─── Widget / form field (PyMuPDF JM_create_widget, JM_set_widget_properties) ───

        /// <summary>Port of <c>JM_create_widget</c>.</summary>
        internal static mupdf.PdfAnnot JmCreateWidget(
            mupdf.PdfDocument doc,
            mupdf.PdfPage page,
            WidgetType type,
            string fieldName)
        {
            int oldSigFlags = 0;
            var sigFlagsObj = PdfObjDictGetp(Helpers.PdfTrailer(doc), "Root/AcroForm/SigFlags");
            if (sigFlagsObj.m_internal != null)
                oldSigFlags = mupdf.mupdf.pdf_to_int(sigFlagsObj);

            var annot = PdfCreateAnnotRaw(page, mupdf.pdf_annot_type.PDF_ANNOT_WIDGET);
            if (annot?.m_internal == null)
                return null;

            var annotObj = Helpers.PdfAnnotObj(annot);
            try
            {
                JmSetFieldType(annotObj, type);
                if (!string.IsNullOrEmpty(fieldName))
                    PdfDictPutTextString(annotObj, "T", fieldName);

                if (type == WidgetType.Signature)
                {
                    int sigFlags = oldSigFlags | (Constants.SigFlagSignaturesExist | Constants.SigFlagAppendOnly);
                    var trailer = Helpers.PdfTrailer(doc);
                    PdfDictPutl(doc, trailer, mupdf.mupdf.pdf_new_int(sigFlags), "Root", "AcroForm", "SigFlags");
                }

                var form = PdfObjDictGetp(Helpers.PdfTrailer(doc), "Root/AcroForm/Fields");
                if (form.m_internal == null)
                {
                    form = mupdf.mupdf.pdf_new_array(doc, 1);
                    PdfDictPutl(doc, Helpers.PdfTrailer(doc), form, "Root", "AcroForm", "Fields");
                }
                PdfArrayPush(form, annotObj);
            }
            catch
            {
                mupdf.mupdf.pdf_delete_annot(page, annot);
                if (type == WidgetType.Signature)
                {
                    var trailer = Helpers.PdfTrailer(doc);
                    PdfDictPutl(doc, trailer, mupdf.mupdf.pdf_new_int(oldSigFlags), "Root", "AcroForm", "SigFlags");
                }
                throw;
            }
            return annot;
        }

        /// <summary>Port of <c>JM_set_field_type</c>.</summary>
        internal static void JmSetFieldType(mupdf.PdfObj obj, WidgetType type)
        {
            int setBits = 0, clearBits = 0;
            mupdf.PdfObj typename = null;
            switch (type)
            {
                case WidgetType.Button:
                    typename = mupdf.mupdf.pdf_new_name("Btn");
                    setBits = mupdf.mupdf.PDF_BTN_FIELD_IS_PUSHBUTTON;
                    break;
                case WidgetType.RadioButton:
                    typename = mupdf.mupdf.pdf_new_name("Btn");
                    clearBits = mupdf.mupdf.PDF_BTN_FIELD_IS_PUSHBUTTON;
                    setBits = mupdf.mupdf.PDF_BTN_FIELD_IS_RADIO;
                    break;
                case WidgetType.CheckBox:
                    typename = mupdf.mupdf.pdf_new_name("Btn");
                    clearBits = mupdf.mupdf.PDF_BTN_FIELD_IS_PUSHBUTTON | mupdf.mupdf.PDF_BTN_FIELD_IS_RADIO;
                    break;
                case WidgetType.Text:
                    typename = mupdf.mupdf.pdf_new_name("Tx");
                    break;
                case WidgetType.ListBox:
                    typename = mupdf.mupdf.pdf_new_name("Ch");
                    clearBits = mupdf.mupdf.PDF_CH_FIELD_IS_COMBO;
                    break;
                case WidgetType.ComboBox:
                    typename = mupdf.mupdf.pdf_new_name("Ch");
                    setBits = mupdf.mupdf.PDF_CH_FIELD_IS_COMBO;
                    break;
                case WidgetType.Signature:
                    typename = mupdf.mupdf.pdf_new_name("Sig");
                    break;
            }
            if (typename?.m_internal != null)
                PdfDictPut(obj, "FT", typename);
            if (setBits != 0 || clearBits != 0)
            {
                int bits = PdfDictGetInt(obj, "Ff");
                bits &= ~clearBits;
                bits |= setBits;
                PdfDictPutInt(obj, "Ff", bits);
            }
        }

        internal static mupdf.PdfObj JmGetBorderStyle(string style)
        {
            if (string.IsNullOrEmpty(style))
                return mupdf.mupdf.pdf_new_name("S");
            char c = char.ToLowerInvariant(style[0]);
            return c switch
            {
                'b' => mupdf.mupdf.pdf_new_name("B"),
                'd' => mupdf.mupdf.pdf_new_name("D"),
                'i' => mupdf.mupdf.pdf_new_name("I"),
                'u' => mupdf.mupdf.pdf_new_name("U"),
                _ => mupdf.mupdf.pdf_new_name("S"),
            };
        }

        /// <summary>Port of <c>JM_new_javascript</c>.</summary>
        internal static mupdf.PdfObj JmNewJavascript(mupdf.PdfDocument pdf, string value)
        {
            if (string.IsNullOrEmpty(value))
                return new mupdf.PdfObj();
            var res = BufferFromBytes(Encoding.UTF8.GetBytes(value));
            var source = PdfAddStream(pdf, res, new mupdf.PdfObj(), 0);
            var newaction = PdfAddNewDict(pdf, 4);
            PdfDictPutName(newaction, "S", "JavaScript");
            PdfDictPut(newaction, "JS", source);
            return newaction;
        }

        /// <summary>Port of <c>JM_put_script</c>.</summary>
        internal static void JmPutScript(mupdf.PdfObj annotObj, string key1, string key2, string value)
        {
            if (string.IsNullOrEmpty(key2))
            {
                var k1 = mupdf.mupdf.pdf_new_name(key1);
                try
                {
                    JmPutScript(annotObj, k1, new mupdf.PdfObj(), value);
                }
                finally
                {
                    k1.Dispose();
                }
                return;
            }
            var key1Name = mupdf.mupdf.pdf_new_name(key1);
            var key2Name = mupdf.mupdf.pdf_new_name(key2);
            try
            {
                JmPutScript(annotObj, key1Name, key2Name, value);
            }
            finally
            {
                key1Name.Dispose();
                key2Name.Dispose();
            }
        }

        internal static void JmPutScript(mupdf.PdfObj annotObj, mupdf.PdfObj key1, mupdf.PdfObj key2, string value)
        {
            var key1Obj = PdfDictGet(annotObj, key1);
            var pdf = mupdf.mupdf.pdf_get_bound_document(annotObj);
            if (string.IsNullOrEmpty(value))
            {
                if (key2?.m_internal == null)
                    mupdf.mupdf.pdf_dict_del(annotObj, key1);
                else if (key1Obj.m_internal != null)
                    mupdf.mupdf.pdf_dict_del(key1Obj, key2);
                return;
            }
            string existing = null;
            if (key2?.m_internal == null || key1Obj.m_internal == null)
                existing = JmGetScript(key1Obj);
            else
                existing = JmGetScript(PdfDictGet(key1Obj, key2));
            if (value != existing)
            {
                var newaction = JmNewJavascript(pdf, value);
                if (key2?.m_internal == null)
                    PdfDictPut(annotObj, key1, newaction);
                else
                    PdfDictPutl(pdf, annotObj, newaction, key1, key2);
            }
        }

        internal static string JmGetScript(mupdf.PdfObj action)
        {
            if (action?.m_internal == null) return null;
            var js = PdfDictGets(action, "JS");
            if (js.m_internal == null) return null;
            if (mupdf.mupdf.pdf_is_stream(js) != 0)
            {
                var buf = mupdf.mupdf.pdf_load_stream(js);
                return Encoding.UTF8.GetString(buf.fz_buffer_extract());
            }
            return mupdf.mupdf.pdf_to_text_string(js);
        }

        /// <summary>Port of <c>util_ensure_widget_calc</c>.</summary>
        internal static void EnsureWidgetCalc(mupdf.PdfAnnot annot)
        {
            var annotObj = Helpers.PdfAnnotObj(annot);
            var pdf = mupdf.mupdf.pdf_get_bound_document(annotObj);
            var acro = PdfDictGetl(Helpers.PdfTrailer(pdf), "Root", "AcroForm");
            var co = PdfDictGets(acro, "CO");
            if (mupdf.mupdf.pdf_is_array(co) == 0)
                co = Helpers.PdfDictPutArray(acro, "CO", 2);
            int n = mupdf.mupdf.pdf_array_len(co);
            int xref = mupdf.mupdf.pdf_to_num(annotObj);
            for (int i = 0; i < n; i++)
            {
                if (mupdf.mupdf.pdf_to_num(Helpers.PdfArrayGet(co, i)) == xref)
                    return;
            }
            PdfArrayPush(co, PdfNewIndirect(pdf, xref, 0));
        }

        internal static void JmSetChoiceOptions(mupdf.PdfAnnot annot, IList<string> liste)
        {
            if (liste == null || liste.Count == 0)
                return;
            JmSetChoiceOptions(annot, liste.Cast<object>().ToList());
        }

        /// <summary>Port of <c>JM_set_choice_options</c>.</summary>
        internal static void JmSetChoiceOptions(mupdf.PdfAnnot annot, IList<object> liste)
        {
            if (liste == null || liste.Count == 0)
                return;
            var annotObj = Helpers.PdfAnnotObj(annot);
            var pdf = mupdf.mupdf.pdf_get_bound_document(annotObj);
            var optarr = mupdf.mupdf.pdf_new_array(pdf, liste.Count);
            foreach (var val in liste)
            {
                if (val is string s)
                {
                    optarr.pdf_array_push_text_string(s);
                    continue;
                }
                if (TryGetChoicePair(val, out string opt1, out string opt2))
                {
                    var optarrsub = optarr.pdf_array_push_array(2);
                    optarrsub.pdf_array_push_text_string(opt1);
                    optarrsub.pdf_array_push_text_string(opt2);
                    continue;
                }
                throw new ValueErrorException("bad choice field list");
            }
            PdfDictPut(annotObj, "Opt", optarr);
        }

        private static bool TryGetChoicePair(object val, out string opt1, out string opt2)
        {
            opt1 = opt2 = null;
            switch (val)
            {
                case IList list when list.Count == 2:
                    opt1 = list[0]?.ToString();
                    opt2 = list[1]?.ToString();
                    break;
                case Array arr when arr.Length == 2:
                    opt1 = arr.GetValue(0)?.ToString();
                    opt2 = arr.GetValue(1)?.ToString();
                    break;
                default:
                    return false;
            }
            return !string.IsNullOrEmpty(opt1) && !string.IsNullOrEmpty(opt2);
        }

        /// <summary>Port of <c>JM_set_widget_properties</c>.</summary>
        internal static void JmSetWidgetProperties(mupdf.PdfAnnot annot, Widget widget)
        {
            var page = PdfAnnotPage(annot);
            if (page?.m_internal == null)
                throw new InvalidOperationException("Annot is not bound to a page");
            var annotObj = Helpers.PdfAnnotObj(annot);
            var pdf = PdfDocumentForPdfPage(page);
            var fieldType = widget.InsertFieldType;

            // rectangle
            var rect = widget.InsertRect;
            var rotMat = JM_rotate_page_matrix(page);
            rect = new Rect(rect).Transform(rotMat);
            mupdf.mupdf.pdf_set_annot_rect(annot, rect.ToFzRect());

            // fill color
            if (widget.InsertFillColor != null && widget.InsertFillColor.Count > 0)
            {
                var fillCol = mupdf.mupdf.pdf_new_array(pdf, widget.InsertFillColor.Count);
                foreach (var col in widget.InsertFillColor)
                    mupdf.mupdf.pdf_array_push_real(fillCol, col);
                annotObj.pdf_field_set_fill_color(fillCol);
            }

            // border dashes
            if (widget.InsertBorderDashes != null && widget.InsertBorderDashes.Count > 0)
            {
                var dashes = mupdf.mupdf.pdf_new_array(pdf, widget.InsertBorderDashes.Count);
                foreach (var d in widget.InsertBorderDashes)
                    mupdf.mupdf.pdf_array_push_int(dashes, d);
                PdfDictPutl(pdf, annotObj, dashes, "BS", "D");
            }

            // border color
            if (widget.InsertBorderColor != null && widget.InsertBorderColor.Count > 0)
            {
                var borderCol = mupdf.mupdf.pdf_new_array(pdf, widget.InsertBorderColor.Count);
                foreach (var col in widget.InsertBorderColor)
                    mupdf.mupdf.pdf_array_push_real(borderCol, col);
                PdfDictPutl(pdf, annotObj, borderCol, "MK", "BC");
            }

            // field label
            if (widget.InsertFieldLabel != null)
                PdfDictPutTextString(annotObj, "TU", widget.InsertFieldLabel);

            // field name
            if (!string.IsNullOrEmpty(widget.InsertFieldName))
            {
                var oldName = mupdf.mupdf.pdf_load_field_name(annotObj);
                if (widget.InsertFieldName != oldName)
                    PdfDictPutTextString(annotObj, "T", widget.InsertFieldName);
            }

            // max text len
            if (fieldType == WidgetType.Text && widget.InsertTextMaxLen > 0)
                PdfDictPutInt(annotObj, "MaxLen", widget.InsertTextMaxLen);

            annotObj.pdf_field_set_display(widget.InsertFieldDisplay);

            // choice values
            if (fieldType == WidgetType.ListBox || fieldType == WidgetType.ComboBox)
                if (widget.InsertChoiceValuesMixed != null && widget.InsertChoiceValuesMixed.Count > 0)
                    JmSetChoiceOptions(annot, widget.InsertChoiceValuesMixed);
                else if (widget.InsertChoiceValues != null && widget.InsertChoiceValues.Count > 0)
                    JmSetChoiceOptions(annot, widget.InsertChoiceValues);

            // border style / width
            PdfDictPutl(pdf, annotObj, JmGetBorderStyle(widget.InsertBorderStyle), "BS", "S");
            PdfDictPutl(pdf, annotObj, mupdf.mupdf.pdf_new_real(widget.InsertBorderWidth), "BS", "W");

            // /DA string
            if (!string.IsNullOrEmpty(widget.InsertTextDa))
            {
                PdfDictPutTextString(annotObj, "DA", widget.InsertTextDa);
                PdfDictDel(annotObj, "DS");
                PdfDictDel(annotObj, "RC");
            }

            // field flags
            if (widget.InsertFieldFlags.HasValue)
            {
                int fieldFlags = widget.InsertFieldFlags.Value;
                if (fieldType == WidgetType.ComboBox)
                    fieldFlags |= mupdf.mupdf.PDF_CH_FIELD_IS_COMBO;
                else if (fieldType == WidgetType.RadioButton)
                    fieldFlags |= mupdf.mupdf.PDF_BTN_FIELD_IS_RADIO;
                else if (fieldType == WidgetType.Button)
                    fieldFlags |= mupdf.mupdf.PDF_BTN_FIELD_IS_PUSHBUTTON;
                PdfDictPutInt(annotObj, "Ff", fieldFlags);
            }

            // button caption
            if (!string.IsNullOrEmpty(widget.InsertButtonCaption))
                annotObj.pdf_field_set_button_caption(widget.InsertButtonCaption);

            // scripts
            JmPutScript(annotObj, "A", null, widget.InsertScript);
            JmPutScript(annotObj, "AA", "K", widget.InsertScriptStroke);
            JmPutScript(annotObj, "AA", "F", widget.InsertScriptFormat);
            JmPutScript(annotObj, "AA", "V", widget.InsertScriptChange);
            JmPutScript(annotObj, "AA", "C", widget.InsertScriptCalc);
            JmPutScript(annotObj, "AA", "Bl", widget.InsertScriptBlur);
            JmPutScript(annotObj, "AA", "Fo", widget.InsertScriptFocus);

            // field value
            string text = widget.InsertFieldValue;
            if (fieldType == WidgetType.RadioButton)
            {
                if (widget.InsertFieldValueBool == false || string.IsNullOrEmpty(text) || text == "Off")
                {
                    mupdf.mupdf.pdf_set_field_value(pdf, annotObj, "Off", 1);
                    PdfDictPutName(annotObj, "AS", "Off");
                }
                else
                {
                    var onstate = mupdf.mupdf.pdf_button_field_on_state(annotObj);
                    if (onstate.m_internal != null)
                    {
                        string on = mupdf.mupdf.pdf_to_name(onstate);
                        mupdf.mupdf.pdf_set_field_value(pdf, annotObj, on, 1);
                        PdfDictPutName(annotObj, "AS", on);
                    }
                    else if (!string.IsNullOrEmpty(text))
                        PdfDictPutName(annotObj, "AS", text);
                }
            }
            else if (fieldType == WidgetType.CheckBox)
            {
                var onstate = mupdf.mupdf.pdf_button_field_on_state(annotObj);
                string on = onstate.m_internal != null ? mupdf.mupdf.pdf_to_name(onstate) : "Yes";
                if (widget.InsertFieldValueBool == true || text == on || text == "Yes" || text == "true" || text == "True")
                {
                    mupdf.mupdf.pdf_set_field_value(pdf, annotObj, on, 1);
                    PdfDictPutName(annotObj, "AS", on);
                    PdfDictPutName(annotObj, "V", on);
                }
                else
                {
                    PdfDictPutName(annotObj, "AS", "Off");
                    PdfDictPutName(annotObj, "V", "Off");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(text))
                {
                    mupdf.mupdf.pdf_set_field_value(pdf, annotObj, text, 1);
                    if (fieldType == WidgetType.ComboBox || fieldType == WidgetType.ListBox)
                        PdfDictDel(annotObj, "I");
                }
            }

            annot.pdf_set_annot_hot(1);
            annot.pdf_set_annot_active(1);
            PdfDirtyAndUpdateAnnot(annot);
        }

        sealed class SanitizePdfFilterFactory : mupdf.PdfFilterFactory2
        {
            readonly mupdf.PdfSanitizeFilterOptions _sopts;

            internal SanitizePdfFilterFactory(mupdf.PdfSanitizeFilterOptions sopts)
            {
                _sopts = sopts;
                use_virtual_filter();
            }

            public override pdf_processor filter(
                fz_context ctx,
                pdf_document doc,
                pdf_processor chain,
                int struct_parents,
                fz_matrix transform,
                pdf_filter_options options)
            {
                return mupdf.mupdf.ll_pdf_new_sanitize_filter(
                    doc,
                    chain,
                    struct_parents,
                    transform,
                    options,
                    new SWIGTYPE_p_void(pdf_sanitize_filter_options.getCPtr(_sopts.internal_()).Handle, false));
            }
        }

        /// <summary><c>src/__init__.py</c>.</summary>
        internal static float UtilMeasureString(string text, string fontname, float fontsize, int encoding)
        {
            FzFont font = mupdf.mupdf.fz_new_base14_font(fontname);
            try
            {
                // w = 0
                float w = 0;
                // pos = 0
                int pos = 0;
                // while pos < len(text):
                while (pos < text.Length)
                {
                    using var chartoruneOut = new ll_fz_chartorune_outparams();
                    int t = mupdf.mupdf.ll_fz_chartorune_outparams_fn(text.Substring(pos), chartoruneOut);
                    int c = chartoruneOut.rune;
                    // pos += t
                    pos += t;
                    if (encoding == mupdf.mupdf.PDF_SIMPLE_ENCODING_GREEK)
                        c = mupdf.mupdf.fz_iso8859_7_from_unicode(c);
                    else if (encoding == mupdf.mupdf.PDF_SIMPLE_ENCODING_CYRILLIC)
                        c = mupdf.mupdf.fz_windows_1251_from_unicode(c);
                    else
                        c = mupdf.mupdf.fz_windows_1252_from_unicode(c);
                    // if c < 0:
                    if (c < 0)
                        // c = 0xB7
                        c = 0xB7;
                    int g = font.fz_encode_character(c);
                    float dw = font.fz_advance_glyph(g, 0);
                    // w += dw
                    w += dw;
                }
                // ret = w * fontsize
                float ret = w * fontsize;
                // return ret
                return ret;
            }
            finally
            {
                font?.Dispose();
            }
        }

        /// <summary>Resolves the Tesseract language-data directory.</summary>
        internal static string GetTessdata(string tessdata = null)
        {
            if (!string.IsNullOrWhiteSpace(tessdata))
                return tessdata;

            string env = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
            if (!string.IsNullOrWhiteSpace(env))
                return env;

            string fromTesseract = TryFindTessdataViaTesseractListLangs();
            if (!string.IsNullOrWhiteSpace(fromTesseract))
                return fromTesseract;

            if (IsWindowsPlatform())
            {
                string fromWhere = TryFindTessdataOnWindows();
                if (!string.IsNullOrWhiteSpace(fromWhere))
                    return fromWhere;
            }

            throw new ValueErrorException(
                "No tessdata specified and Tesseract is not installed");
        }

        static bool IsWindowsPlatform()
        {
#if NET5_0_OR_GREATER
            return OperatingSystem.IsWindows();
#else
            PlatformID platform = Environment.OSVersion.Platform;
            return platform == PlatformID.Win32NT || platform == PlatformID.Win32Windows;
#endif
        }

        static string TryFindTessdataViaTesseractListLangs()
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "tesseract",
                    Arguments = "--list-langs",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                if (process == null)
                    return null;
                string stdout = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    return null;
                Match m = Regex.Match(
                    stdout,
                    @"List of available languages in ""(.+)""");
                return m.Success ? m.Groups[1].Value : null;
            }
            catch
            {
                return null;
            }
        }

        static string TryFindTessdataOnWindows()
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "tesseract",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                if (process == null)
                    return null;
                string stdout = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
                    return null;
                string exePath = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                string tessdata = Path.Combine(Path.GetDirectoryName(exePath)!, "tessdata");
                return Directory.Exists(tessdata) ? tessdata : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// LINEART device for <see cref="Page.get_cdrawings"/> (PyMuPDF <c>extra.JM_new_lineart_device</c> / <c>trace_path_walker</c>).
    /// </summary>
    internal sealed class JM_new_lineart_device_Device : mupdf.FzDevice2
    {
        private const int FillPath = 1;
        private const int StrokePath = 2;
        private const int ClipPath = 3;

        private static readonly fz_path_walker s_tracePathWalker;
        private static readonly PathMovetoDelegate s_moveto;
        private static readonly PathMovetoDelegate s_lineto;
        private static readonly PathCurvetoDelegate s_curveto;
        private static readonly PathClosepathDelegate s_closepath;

        /// <summary>Output list or callback target (PyMuPDF <c>dev.out</c>).</summary>
        internal object Out { get; }

        internal ulong seqno;
        internal int depth;
        internal bool clips;
        internal object? method;

        internal List<mupdf.FzRect>? scissors;
        internal string layer_name = "";
        internal bool pathBoundsEmpty = true;
        internal float pathX0, pathY0, pathX1, pathY1;
        internal float pathfactor;
        internal mupdf.FzMatrix ptm = new mupdf.FzMatrix();
        internal float ctm_a = 1f, ctm_b, ctm_c, ctm_d = 1f, ctm_e, ctm_f;
        internal Point lastpoint;
        internal Point firstpoint;
        internal int havemove;
        internal int linecount;
        internal int path_type;
        internal Dictionary<string, object>? pathdict;

        static JM_new_lineart_device_Device()
        {
            s_moveto = TraceMoveto;
            s_lineto = TraceLineto;
            s_curveto = TraceCurveto;
            s_closepath = TraceClosepath;
            s_tracePathWalker = new fz_path_walker
            {
                moveto = ToMovetoFn(s_moveto),
                lineto = ToMovetoFn(s_lineto),
                curveto = ToCurvetoFn(s_curveto),
                closepath = ToClosepathFn(s_closepath),
            };
        }

        public IReadOnlyList<Dictionary<string, object>> RawDrawings =>
            Out is List<Dictionary<string, object>> list ? list : Array.Empty<Dictionary<string, object>>();

        internal JM_new_lineart_device_Device(object output, bool clips, object? method)
        {
            Out = output;
            this.clips = clips;
            this.method = method;

            use_virtual_fill_path();
            use_virtual_stroke_path();
            use_virtual_clip_path();
            use_virtual_clip_image_mask();
            use_virtual_clip_stroke_path();
            use_virtual_clip_stroke_text();
            use_virtual_clip_text();

            use_virtual_fill_text();
            use_virtual_stroke_text();
            use_virtual_ignore_text();

            use_virtual_fill_shade();
            use_virtual_fill_image();
            use_virtual_fill_image_mask();

            use_virtual_pop_clip();

            use_virtual_begin_group();
            use_virtual_end_group();

            use_virtual_begin_layer();
            use_virtual_end_layer();
        }

        public override void begin_layer(mupdf.fz_context ctx, string name) =>
            layer_name = string.IsNullOrEmpty(name) ? "" : name;

        public override void end_layer(mupdf.fz_context ctx) => layer_name = "";

        public override void fill_path(mupdf.fz_context ctx, mupdf.SWIGTYPE_p_fz_path path, int evenOdd, mupdf.fz_matrix ctm,
            mupdf.fz_colorspace colorspace, mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams)
        {
            SetCtm(ctm);
            path_type = FillPath;
            jm_lineart_path(path);
            if (pathdict == null) return;
            pathdict["type"] = "f";
            pathdict["even_odd"] = evenOdd != 0;
            pathdict["fill_opacity"] = alpha;
            pathdict["fill"] = jm_lineart_color(colorspace, color);
            pathdict["rect"] = PathBoundsFzRect();
            pathdict["seqno"] = seqno;
            pathdict["layer"] = layer_name;
            if (clips)
                pathdict["level"] = depth;
            jm_append_merge();
            seqno++;
        }

        public override void stroke_path(mupdf.fz_context ctx, mupdf.SWIGTYPE_p_fz_path path, mupdf.fz_stroke_state stroke, mupdf.fz_matrix ctm,
            mupdf.fz_colorspace colorspace, mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams)
        {
            pathfactor = (float)Math.Sqrt(Math.Abs(ctm.a * ctm.d - ctm.b * ctm.c));

            SetCtm(ctm);
            path_type = StrokePath;
            jm_lineart_path(path);
            if (pathdict == null) return;
            pathdict["type"] = "s";
            pathdict["stroke_opacity"] = alpha;
            pathdict["color"] = jm_lineart_color(colorspace, color);
            pathdict["width"] = pathfactor * stroke.linewidth;
            pathdict["lineCap"] = new object[] { (int)stroke.start_cap, (int)stroke.dash_cap, (int)stroke.end_cap };
            pathdict["lineJoin"] = (float)(int)stroke.linejoin;
            if (!pathdict.ContainsKey("closePath"))
                pathdict["closePath"] = false;

            int dashLen = stroke.dash_len;
            if (dashLen > 0)
            {
                var sb = new StringBuilder();
                sb.Append("[ ");
                var dashPtr = stroke.dash_list;
                var dashArr = new float[dashLen];
                if (dashPtr != null)
                    Marshal.Copy(mupdf.SWIGTYPE_p_float.getCPtr(dashPtr).Handle, dashArr, 0, dashLen);
                for (int i = 0; i < dashLen; i++)
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0:g} ", pathfactor * dashArr[i]);
                sb.AppendFormat(CultureInfo.InvariantCulture, "] {0:g}", pathfactor * stroke.dash_phase);
                pathdict["dashes"] = sb.ToString();
            }
            else
                pathdict["dashes"] = "[] 0";

            pathdict["rect"] = PathBoundsFzRect();
            pathdict["layer"] = layer_name;
            pathdict["seqno"] = seqno;
            if (clips)
                pathdict["level"] = depth;
            jm_append_merge();
            seqno++;
        }

        public override void clip_path(mupdf.fz_context ctx, mupdf.SWIGTYPE_p_fz_path path, int evenOdd, mupdf.fz_matrix ctm, mupdf.fz_rect scissor)
        {
            if (!clips) return;
            SetCtm(ctm);
            path_type = ClipPath;
            jm_lineart_path(path);
            if (pathdict == null) return;
            pathdict["type"] = "clip";
            pathdict["even_odd"] = evenOdd != 0;
            if (!pathdict.ContainsKey("closePath"))
                pathdict["closePath"] = false;
            pathdict["scissor"] = compute_scissor();
            pathdict["level"] = depth;
            pathdict["layer"] = layer_name;
            jm_append_merge();
            depth++;
        }

        public override void clip_stroke_path(mupdf.fz_context ctx, mupdf.SWIGTYPE_p_fz_path path, mupdf.fz_stroke_state stroke, mupdf.fz_matrix ctm, mupdf.fz_rect scissor)
        {
            if (!clips) return;
            SetCtm(ctm);
            path_type = StrokePath;
            jm_lineart_path(path);
            if (pathdict == null) return;
            pathdict["type"] = "clip";
            pathdict["even_odd"] = null!;
            if (!pathdict.ContainsKey("closePath"))
                pathdict["closePath"] = false;
            pathdict["scissor"] = compute_scissor();
            pathdict["level"] = depth;
            pathdict["layer"] = layer_name;
            jm_append_merge();
            depth++;
        }

        public override void clip_text(mupdf.fz_context ctx, mupdf.fz_text text, mupdf.fz_matrix ctm, mupdf.fz_rect scissor)
        {
            if (!clips) return;
            compute_scissor();
            depth++;
        }

        public override void clip_stroke_text(mupdf.fz_context ctx, mupdf.fz_text text, mupdf.fz_stroke_state stroke, mupdf.fz_matrix ctm, mupdf.fz_rect scissor)
        {
            if (!clips) return;
            compute_scissor();
            depth++;
        }

        public override void clip_image_mask(mupdf.fz_context ctx, mupdf.fz_image image, mupdf.fz_matrix ctm, mupdf.fz_rect scissor)
        {
            if (!clips) return;
            compute_scissor();
            depth++;
        }

        public override void pop_clip(mupdf.fz_context ctx)
        {
            if (!clips || scissors == null || scissors.Count < 1) return;
            scissors.RemoveAt(scissors.Count - 1);
            depth--;
        }

        public override void begin_group(mupdf.fz_context ctx, mupdf.fz_rect bbox, mupdf.fz_colorspace cs, int isolated, int knockout, int blendmode, float alpha)
        {
            if (!clips) return;
            pathdict = new Dictionary<string, object>
            {
                ["type"] = "group",
                ["rect"] = new mupdf.FzRect(bbox),
                ["isolated"] = isolated != 0,
                ["knockout"] = knockout != 0,
                ["blendmode"] = mupdf.mupdf.fz_blendmode_name(blendmode) ?? "",
                ["opacity"] = alpha,
                ["level"] = depth,
                ["layer"] = layer_name,
            };
            jm_append_merge();
            depth++;
        }

        public override void end_group(mupdf.fz_context ctx)
        {
            if (!clips) return;
            depth--;
        }

        public override void fill_text(mupdf.fz_context ctx, mupdf.fz_text text, mupdf.fz_matrix ctm, mupdf.fz_colorspace colorspace,
            mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams) => seqno++;

        public override void stroke_text(mupdf.fz_context ctx, mupdf.fz_text text, mupdf.fz_stroke_state stroke, mupdf.fz_matrix ctm,
            mupdf.fz_colorspace colorspace, mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams) => seqno++;

        public override void ignore_text(mupdf.fz_context ctx, mupdf.fz_text text, mupdf.fz_matrix ctm) => seqno++;

        public override void fill_shade(mupdf.fz_context ctx, mupdf.fz_shade shade, mupdf.fz_matrix ctm, float alpha, mupdf.fz_color_params colorParams) => seqno++;

        public override void fill_image(mupdf.fz_context ctx, mupdf.fz_image image, mupdf.fz_matrix ctm, float alpha, mupdf.fz_color_params colorParams) => seqno++;

        public override void fill_image_mask(mupdf.fz_context ctx, mupdf.fz_image image, mupdf.fz_matrix ctm, mupdf.fz_colorspace colorspace,
            mupdf.SWIGTYPE_p_float color, float alpha, mupdf.fz_color_params colorParams) => seqno++;

        private void SetCtm(mupdf.fz_matrix m)
        {
            ctm_a = m.a;
            ctm_b = m.b;
            ctm_c = m.c;
            ctm_d = m.d;
            ctm_e = m.e;
            ctm_f = m.f;
        }

        private (float x, float y) TransformPoint(float x, float y)
        {
            float tx = ctm_a * x + ctm_c * y + ctm_e;
            float ty = ctm_b * x + ctm_d * y + ctm_f;
            return (tx, ty);
        }

        private mupdf.FzRect PathBoundsFzRect()
        {
            if (pathBoundsEmpty)
                return new mupdf.FzRect(mupdf.mupdf.fz_infinite_rect);
            return mupdf.mupdf.fz_make_rect(pathX0, pathY0, pathX1, pathY1);
        }

        private void ResetPathBounds() => pathBoundsEmpty = true;

        private void IncludePathPoint(float x, float y)
        {
            if (pathBoundsEmpty)
            {
                pathX0 = pathX1 = x;
                pathY0 = pathY1 = y;
                pathBoundsEmpty = false;
            }
            else
            {
                if (x < pathX0) pathX0 = x;
                if (x > pathX1) pathX1 = x;
                if (y < pathY0) pathY0 = y;
                if (y > pathY1) pathY1 = y;
            }
        }

        internal void jm_lineart_path(mupdf.SWIGTYPE_p_fz_path path)
        {
            ResetPathBounds();
            linecount = 0;
            lastpoint = new Point(0, 0);
            firstpoint = new Point(0, 0);
            pathdict = new Dictionary<string, object> { ["items"] = new List<object>() };

            GCHandle handle = GCHandle.Alloc(this);
            try
            {
                // extra.i: ll_fz_walk_path(path, &trace_path_walker, dev);
                mupdf.mupdf.ll_fz_walk_path(path, s_tracePathWalker,
                    new mupdf.SWIGTYPE_p_void(GCHandle.ToIntPtr(handle), false));
            }
            finally
            {
                handle.Free();
            }

            if (((List<object>)pathdict["items"]).Count == 0)
                pathdict = null;
        }

        internal mupdf.FzRect compute_scissor()
        {
            scissors ??= new List<mupdf.FzRect>();
            mupdf.FzRect scissor;
            if (scissors.Count > 0)
            {
                using var last = new mupdf.FzRect(scissors[scissors.Count - 1]);
                using var pathR = PathBoundsFzRect();
                scissor = mupdf.mupdf.fz_intersect_rect(last, pathR);
            }
            else
                scissor = PathBoundsFzRect();
            scissors.Add(new mupdf.FzRect(scissor));
            return new mupdf.FzRect(scissor);
        }

        internal void jm_append_merge()
        {
            if (pathdict == null) return;

            if (IsCallbackMode())
            {
                InvokeCallback(pathdict);
                pathdict = null;
                return;
            }

            var outList = (List<Dictionary<string, object>>)Out;
            int len = outList.Count;
            if (len == 0)
            {
                AppendPath(outList);
                return;
            }
            if (pathdict["type"] is not string thistype || thistype != "s")
            {
                AppendPath(outList);
                return;
            }
            var prev = outList[len - 1];
            if (prev["type"] is not string prevtype || prevtype != "f")
            {
                AppendPath(outList);
                return;
            }
            if (!DrawingItemsEqual(prev["items"], pathdict["items"]))
            {
                AppendPath(outList);
                return;
            }
            MergeDictShallow(prev, pathdict);
            prev["type"] = "fs";
            pathdict = null;
        }

        private void AppendPath(List<Dictionary<string, object>> outList)
        {
            outList.Add(new Dictionary<string, object>(pathdict!));
            pathdict = null;
        }

        private bool IsCallbackMode()
        {
            if (Out is Delegate) return true;
            if (method != null && method is not string) return true;
            if (method is string ms && ms.Length > 0) return true;
            return false;
        }

        private void InvokeCallback(Dictionary<string, object> dict)
        {
            try
            {
                if (method is string methodName && methodName.Length > 0)
                {
                    var mi = Out.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    mi?.Invoke(Out, new object[] { dict });
                }
                else if (Out is Action<Dictionary<string, object>> action)
                    action(dict);
                else if (Out is Delegate del)
                    del.DynamicInvoke(dict);
            }
            catch
            {
            }
        }

        private static object jm_lineart_color(mupdf.fz_colorspace? colorspace, mupdf.SWIGTYPE_p_float? colorPtr)
        {
            if (colorspace == null || colorPtr == null)
                return Array.Empty<float>();
            using var cs = new mupdf.FzColorspace(mupdf.mupdf.ll_fz_keep_colorspace(colorspace));
            int n = mupdf.mupdf.fz_colorspace_n(cs);
            if (n <= 0 || n > 32)
                return Array.Empty<float>();
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
                return new[] { dst[0], dst[1], dst[2] };
            }
            finally
            {
                h.Free();
                hSrc.Free();
            }
        }

        internal static bool jm_checkquad(JM_new_lineart_device_Device dev)
        {
            if (dev.pathdict == null) return false;
            var items = (List<object>)dev.pathdict["items"];
            int len = items.Count;
            if (len < 4) return false;
            float[] f = new float[8];
            Point lp = default;
            for (int i = 0; i < 4; i++)
            {
                var line = (object[])items[len - 4 + i]!;
                var temp = PointFromItem(line[1]);
                f[i * 2] = temp.X;
                f[i * 2 + 1] = temp.Y;
                lp = PointFromItem(line[2]);
            }
            if (lp.X != f[0] || lp.Y != f[1])
                return false;

            dev.linecount = 0;
            var q = mupdf.mupdf.fz_make_quad(
                (float)f[0], (float)f[1], (float)f[6], (float)f[7], (float)f[2], (float)f[3], (float)f[4], (float)f[5]);
            items[len - 4] = new object[] { "qu", q };
            items.RemoveRange(len - 3, 3);
            return true;
        }

        internal static bool jm_checkrect(JM_new_lineart_device_Device dev)
        {
            dev.linecount = 0;
            if (dev.pathdict == null) return false;
            var items = (List<object>)dev.pathdict["items"];
            int len = items.Count;
            if (len < 3) return false;

            var line0 = (object[])items[len - 3]!;
            var line2 = (object[])items[len - 1]!;
            var ll = PointFromItem(line0[1]);
            var lr = PointFromItem(line0[2]);
            var ur = PointFromItem(line2[1]);
            var ul = PointFromItem(line2[2]);

            if (ll.Y != lr.Y || ll.X != ul.X || ur.Y != ul.Y || ur.X != lr.X)
                return false;

            long orientation;
            mupdf.FzRect r;
            if (ul.Y < lr.Y)
            {
                r = mupdf.mupdf.fz_make_rect((float)ul.X, (float)ul.Y, (float)lr.X, (float)lr.Y);
                orientation = 1;
            }
            else
            {
                r = mupdf.mupdf.fz_make_rect((float)ll.X, (float)ll.Y, (float)ur.X, (float)ur.Y);
                orientation = -1;
            }

            items[len - 3] = new object[] { "re", new mupdf.FzRect(r), orientation };
            items.RemoveRange(len - 2, 2);
            return true;
        }

        private static Point PointFromItem(object item)
        {
            if (item is Point p) return p;
            if (item is mupdf.FzPoint fp) return Helpers.PointFromFz(fp);
            throw new InvalidOperationException("expected point in path item");
        }

        private static bool DrawingItemsEqual(object? a, object? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            if (a is Point pa && b is Point pb) return pa == pb;
            if (a is mupdf.FzRect ra && b is mupdf.FzRect rb)
                return ra.x0 == rb.x0 && ra.y0 == rb.y0 && ra.x1 == rb.x1 && ra.y1 == rb.y1;
            if (a is mupdf.FzQuad qa && b is mupdf.FzQuad qb)
            {
                static bool Eq(mupdf.fz_point? u, mupdf.fz_point? v) =>
                    u != null && v != null && u.x == v.x && u.y == v.y;
                return Eq(qa.ul, qb.ul) && Eq(qa.ur, qb.ur) && Eq(qa.ll, qb.ll) && Eq(qa.lr, qb.lr);
            }
            if (a is object[] aa && b is object[] bb)
            {
                if (aa.Length != bb.Length) return false;
                for (int i = 0; i < aa.Length; i++)
                {
                    if (i == 0 && aa[0] is string sa && bb[0] is string sb)
                    {
                        if (sa != sb) return false;
                        continue;
                    }
                    if (!DrawingItemsEqual(aa[i], bb[i])) return false;
                }
                return true;
            }
            if (a is List<object> la && b is List<object> lb)
            {
                if (la.Count != lb.Count) return false;
                for (int i = 0; i < la.Count; i++)
                {
                    if (!DrawingItemsEqual(la[i], lb[i])) return false;
                }
                return true;
            }
            return a.Equals(b);
        }

        private static void MergeDictShallow(Dictionary<string, object> into, Dictionary<string, object> from)
        {
            foreach (var kv in from)
            {
                if (!into.ContainsKey(kv.Key))
                    into[kv.Key] = kv.Value;
            }
        }

        private static JM_new_lineart_device_Device DevFromArg(IntPtr arg) =>
            (JM_new_lineart_device_Device)GCHandle.FromIntPtr(arg).Target!;

        private static void TraceMoveto(IntPtr ctx, IntPtr arg, float x, float y)
        {
            var dev = DevFromArg(arg);
            var (lx, ly) = dev.TransformPoint(x, y);
            dev.lastpoint = new Point(lx, ly);
            if (dev.pathBoundsEmpty)
                dev.IncludePathPoint(lx, ly);
            dev.firstpoint = new Point(lx, ly);
            dev.havemove = 1;
            dev.linecount = 0;
        }

        private static void TraceLineto(IntPtr ctx, IntPtr arg, float x, float y)
        {
            var dev = DevFromArg(arg);
            var (lx, ly) = dev.TransformPoint(x, y);
            var p1 = new Point(lx, ly);
            dev.IncludePathPoint(lx, ly);
            var list = new object[] { "l", new Point(dev.lastpoint.X, dev.lastpoint.Y), p1 };
            dev.lastpoint = p1;
            ((List<object>)dev.pathdict!["items"]).Add(list);
            dev.linecount++;
            if (dev.linecount == 4 && dev.path_type != FillPath)
                jm_checkquad(dev);
        }

        private static void TraceCurveto(IntPtr ctx, IntPtr arg, float x1, float y1, float x2, float y2, float x3, float y3)
        {
            var dev = DevFromArg(arg);
            dev.linecount = 0;
            var (x1t, y1t) = dev.TransformPoint(x1, y1);
            var (x2t, y2t) = dev.TransformPoint(x2, y2);
            var (x3t, y3t) = dev.TransformPoint(x3, y3);
            var p1 = new Point(x1t, y1t);
            var p2 = new Point(x2t, y2t);
            var p3 = new Point(x3t, y3t);
            dev.IncludePathPoint(x1t, y1t);
            dev.IncludePathPoint(x2t, y2t);
            dev.IncludePathPoint(x3t, y3t);
            var list = new object[] { "c", new Point(dev.lastpoint.X, dev.lastpoint.Y), p1, p2, p3 };
            dev.lastpoint = p3;
            ((List<object>)dev.pathdict!["items"]).Add(list);
        }

        private static void TraceClosepath(IntPtr ctx, IntPtr arg)
        {
            var dev = DevFromArg(arg);
            if (dev.linecount == 3)
            {
                if (jm_checkrect(dev))
                    return;
            }
            dev.linecount = 0;
            if (dev.havemove != 0)
            {
                if (dev.lastpoint.X != dev.firstpoint.X || dev.lastpoint.Y != dev.firstpoint.Y)
                {
                    var list = new object[] { "l", new Point(dev.lastpoint.X, dev.lastpoint.Y), new Point(dev.firstpoint.X, dev.firstpoint.Y) };
                    dev.lastpoint = new Point(dev.firstpoint.X, dev.firstpoint.Y);
                    ((List<object>)dev.pathdict!["items"]).Add(list);
                }
                dev.havemove = 0;
                dev.pathdict!["closePath"] = false;
            }
            else
                dev.pathdict!["closePath"] = true;
        }

        private static SWIGTYPE_p_f_p_fz_context_p_void_float_float__void ToMovetoFn(PathMovetoDelegate d) =>
            new SWIGTYPE_p_f_p_fz_context_p_void_float_float__void(Marshal.GetFunctionPointerForDelegate(d), false);

        private static SWIGTYPE_p_f_p_fz_context_p_void_float_float_float_float_float_float__void ToCurvetoFn(PathCurvetoDelegate d) =>
            new SWIGTYPE_p_f_p_fz_context_p_void_float_float_float_float_float_float__void(Marshal.GetFunctionPointerForDelegate(d), false);

        private static SWIGTYPE_p_f_p_fz_context_p_void__void ToClosepathFn(PathClosepathDelegate d) =>
            new SWIGTYPE_p_f_p_fz_context_p_void__void(Marshal.GetFunctionPointerForDelegate(d), false);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PathMovetoDelegate(IntPtr ctx, IntPtr arg, float x, float y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PathCurvetoDelegate(IntPtr ctx, IntPtr arg, float x1, float y1, float x2, float y2, float x3, float y3);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PathClosepathDelegate(IntPtr ctx, IntPtr arg);
    }
}
