using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using mupdf;
using System.Reflection;

namespace MuPDF.NET
{
    public class MuPDFDocument : IDisposable
    {
        public bool IsClosed = false;

        public bool IsEncrypted = false;

        public bool Is_Encrypted;

        public int GraftID;

        public Dictionary<string, string> MetaData;

        public List<FontStruct> FontInfo = new List<FontStruct>();

        public Dictionary<int, MuPDFGraftMap> GraftMaps = new Dictionary<int, MuPDFGraftMap>();

        public string ShownPages = "";

        public string InsertedImages = "";

        private FzDocument _nativeDocument;

        public Dictionary<int, MuPDFPage> PageRefs;

        public string Name = null;

        public PdfDocument PdfDocument;

        public List<byte> Stream;

        public bool NeedsPass {
            get
            {
                if (IsClosed)
                    throw new Exception("Document closed");
                FzDocument doc = _nativeDocument;
                int ret = doc.fz_needs_password();
                return ret != 0 ? true : false;
            }
        }

        public Outline Outline;
        public bool IsPDF
        {
            get
            {
                return true;
            }
            set
            {
                IsPDF = value;
            }
        }

        public bool IsOwn { get; set; }

        public int Len
        {
            get
            {
                return GetPageCount();
            }
        }

        public bool IsDirty
        {
            get
            {
                PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
                if (pdf == null)
                    return false;
                int r = pdf.pdf_has_unsaved_changes();
                return r != 0;
            }
        }

        public bool IsFastWebaccess
        {
            get
            {
                PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
                if (pdf != null)
                    return pdf.pdf_doc_was_linearized() != 0;
                return false;
            }
        }

        public int IsFormPDF // return -1 or fields count
        {
            get
            {
                PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
                if (pdf == null)
                    return -1;
                int count = -1;
                try
                {
                    PdfObj fields = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "AcroForm", "Fields" });
                    if (fields.pdf_is_array() != 0)
                        count = fields.pdf_array_len();
                }
                catch (Exception) { return -1; }

                if (count >= 0)
                    return count;
                return -1;
            }
        }

        public bool IsReflowable
        {
            get
            {
                if (IsClosed)
                    throw new Exception("document is closed");
                return _nativeDocument.fz_is_document_reflowable() != 0;
            }
        }

        public bool IsRepaired
        {
            get
            {
                PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
                if (pdf == null)
                    return false;
                return pdf.pdf_was_repaired() != 0;
            }
        }

        public MuPDFDocument(PdfDocument doc)
        {
            PdfDocument = doc;
            IsPDF = true;
        }

        public MuPDFDocument(string filename = null, byte[] stream = null, string filetype = null,
            Rect rect = null, float width = 0, float height = 0, int fontSize = 11)
        {
            try
            {
                IsClosed = false;
                IsEncrypted = false;
                MetaData = null;
                FontInfo = new List<FontStruct>();
                PageRefs = new Dictionary<int, MuPDFPage>();

                if (stream != null)
                    Stream = new List<byte>(stream);
                else
                    Stream = null;

                bool fromFile;
                if (filename != null && stream == null)
                {
                    fromFile = true;
                    Name = filename;
                }
                else
                {
                    fromFile = false;
                    Name = "";
                }

                string msg;
                if (fromFile)
                {
                    if (!File.Exists(filename))
                    {
                        msg = $"No such file: {filename}";
                        throw new FileNotFoundException(msg);
                    }
                    _nativeDocument = new FzDocument(filename);
                    return;
                }

                if (fromFile && (new FileInfo(filename).Length == 0 || Stream.Count == 0))
                {
                    msg = $"cannot open empty document";
                    throw new Exception(msg);
                }

                float w = width;
                float h = height;
                FzRect r = rect == null ? new FzRect(FzRect.Fixed.Fixed_EMPTY) : rect.ToFzRect();
                if (r.fz_is_infinite_rect() != 0)
                {
                    w = r.x1 - r.x0;
                    h = r.y1 - r.y0;
                }

                FzStream data;
                FzDocument doc = null;
                if (stream != null)
                {
                    IntPtr dataPtr = Marshal.AllocHGlobal(stream.Length);
                    Marshal.Copy(stream, 0, dataPtr, stream.Length);
                    SWIGTYPE_p_unsigned_char swigData = new SWIGTYPE_p_unsigned_char(dataPtr, true);
                    data = mupdf.mupdf.fz_open_memory(swigData, (uint)stream.Length);
                
                    //Marshal.FreeHGlobal(dataPtr);
                    string magic = filename;
                    if (magic == null)
                        magic = filetype;
                    doc = mupdf.mupdf.fz_open_document_with_stream(magic, data);
                }
                else
                {
                    if (filename != null)
                    {
                        if (filetype == null)
                        {
                            try
                            {
                                doc = mupdf.mupdf.fz_open_document(filename);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                        }
                        else
                        {
                            fz_document_handler handler = mupdf.mupdf.ll_fz_recognize_document(filetype);
                            if (handler != null)
                            {
                                if (handler.open != null)
                                {
                                    try
                                    {
                                        doc.m_internal = mupdf.mupdf.ll_fz_document_open_fn_call(handler.open, filename);
                                    }
                                    catch (Exception)
                                    {
                                        throw new Exception(Utils.ErrorMessages["MSG_BAD_DOCUMENT"]);
                                    }
                                }
                                else if (handler.open_with_stream != null)
                                {
                                    data = mupdf.mupdf.fz_open_file(filename);
                                    doc.m_internal = mupdf.mupdf.ll_fz_document_open_with_stream_fn_call(handler.open_with_stream, data.m_internal);
                                }
                            }
                            else
                            {
                                throw new Exception(Utils.ErrorMessages["MSG_BAD_FILETYPE"]);
                            }
                        }
                    }
                    else
                    {
                        PdfDocument pdf = new PdfDocument();
                        doc = new FzDocument(pdf);
                    }

                }
                if (w > 0 && h > 0)
                    doc.fz_layout_document(w, h, fontSize);
                else if (doc.fz_is_document_reflowable() != 0)
                    doc.fz_layout_document(400, 600, 11);
                _nativeDocument = doc;

                IsOwn = true;

                if (IsOwn)
                {
                    GraftID = Utils.GenID();
                    if (NeedsPass)
                    {
                        IsEncrypted = true;
                        Is_Encrypted = true;
                    }
                    else
                        InitDocument();

                    string filename_ = filename;
                    if ((filename != null && filename_.ToLower().EndsWith("svg")) || (filetype != null && filetype.ToLower().Contains("svg")))
                    {
                        try
                        {
                            byte[] _ = Convert2Pdf();
                        }
                        catch (Exception)
                        {
                            throw new Exception("cannot open broken document");
                        }
                    }
                }
            }
            finally
            {
                //issue
            }
        }

        public byte[] Convert2Pdf(int from = 0, int to = -1, int rotate = 0)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            FzDocument doc = _nativeDocument;
            int fp = from;
            int tp = to;
            int srcCount = doc.fz_count_pages();

            if (fp < 0)
                fp = srcCount - 1;
            if (tp < 0)
                tp = srcCount - 1;
            if (tp > srcCount - 1)
                tp = srcCount - 1;
            int len0 = Utils.MUPDF_WARNINGS_STORE.Count;
            PdfDocument pdfout = new PdfDocument();
            int incr = 1;
            if (fp > tp)
            {
                incr = -1;
                int t = tp;
                tp = fp;
                fp = t;
            }
            int rot = Utils.NormalizeRotation(rotate);
            int i = fp;

            while (true)
            {
                if (!Utils.INRANGE(i, fp, tp))
                    break;
                FzPage page = doc.fz_load_page(i);
                FzRect mediabox = page.fz_bound_page();
                (FzDevice dev, PdfObj resources, FzBuffer contents) = pdfout.pdf_page_write(mediabox);
                page.fz_run_page(dev, new FzMatrix(), new FzCookie());
                dev.fz_close_device();
                dev = null;

                PdfObj pageObj = pdfout.pdf_add_page(mediabox, rot, resources, contents);
                pdfout.pdf_insert_page(-1, pageObj);
                i += incr;
            }

            PdfWriteOptions opts = new PdfWriteOptions();
            opts.do_garbage = 4;
            opts.do_compress = 1;
            opts.do_compress_images = 1;
            opts.do_compress_fonts = 1;
            opts.do_sanitize = 1;
            opts.do_incremental = 1;
            opts.do_ascii = 1;
            opts.do_decompress = 1;
            opts.do_linear = 0;
            opts.do_clean = 1;
            opts.do_pretty = 0;

            FzBuffer res = mupdf.mupdf.fz_new_buffer(8192);
            FzOutput output = new FzOutput(res);
            pdfout.pdf_write_document(output, opts);

            byte[] docBytes = Utils.BinFromBuffer(res);
            int len1 = Utils.MUPDF_WARNINGS_STORE.Count;
            
            for (i = len0; i < len1; i ++)
            {
                Console.WriteLine($"{Utils.MUPDF_WARNINGS_STORE[i]}");
            }
            return docBytes;
        }

        public int GetPageCount()
        {
            return _nativeDocument.fz_count_pages();
        }

        public FzPage GetPage(int index)
        {
            return _nativeDocument.fz_load_page(index);
        }

        public FzDocument ToFzDocument()
        {
            return _nativeDocument;
        }

        public static PdfDocument AsPdfDocument(FzDocument document)
        {
            return new PdfDocument(document);
        }

        public static PdfDocument AsPdfDocument(MuPDFDocument document)
        {
            return document._nativeDocument.pdf_document_from_fz_document();
        }

        public void InitDocument()
        {
            if (Is_Encrypted)
                throw new Exception("cannot initialize - document still encrypted");
            Outline = LoadOutline();
            MetaData = new Dictionary<string, string>();

            Dictionary<string, string> values = new Dictionary<string, string>()
            {
                { "format", "format" },
                { "title", "info:Title" },
                { "author", "info:Author" },
                { "subject", "info:Subject" },
                { "keywords", "info:Keywords" },
                { "creator", "info:Creator" },
                { "producer", "info:Producer" },
                { "creationDate", "info:CreationDate" },
                { "modDate", "info:ModDate" },
                { "trapped", "info:Trapped" }
            };

            foreach ((string key, string value) in values)
            {
                MetaData.Add(key, GetMetadata(value));
            }
            string enc = GetMetadata("encryption");
            MetaData.Add("encryption", enc == "None" ? null : enc);
        }

        private string GetMetadata(string key)
        {
            try
            {
                return _nativeDocument.fz_lookup_metadata2(key);
            }
            catch (Exception)
            {
                return "None";
            }
            
        }

        public int GetPageXref(int pno)
        {
            if (IsClosed)
                throw new Exception("document closed");
            int pageCount = Len;
            int n = pno;

            while (n < 0)
                n += pageCount;

            PdfDocument pdf = AsPdfDocument(this);
            int xref = 0;
            if (n >= pageCount)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_PAGENO"]);
            xref = pdf.pdf_lookup_page_obj(n).pdf_to_num();

            return xref;
        }

        private Outline LoadOutline()
        {
            FzDocument doc = _nativeDocument;
            FzOutline ol = null;
            try
            {
                ol = doc.fz_load_outline();
            }
            catch (Exception)
            {
                
            }
            return new Outline(ol);
        }
        public static PdfObj SetObjectValue(PdfObj obj, string key, string value)
        {
            string eyecatcher = "fiz: replace me!";
            PdfDocument pdf = obj.pdf_get_bound_document();

            string[] list = key.Split('/');
            int len = list.Length;
            int i = len - 1;
            string skey = list[i];

            list = list.Take(len - 1).ToArray();
            len = list.Length;
            PdfObj testkey = obj.pdf_dict_getp(key);

            if (testkey is null)
            {
                while (len > 0)
                {
                    string t = string.Join("/", list);
                    if (obj.pdf_dict_getp(t).pdf_is_indirect() != 0)
                        throw new Exception(string.Format("path to '{0}' has indirects", skey));
                    list = list.Take(len - 1).ToArray();
                    len = list.Length;
                }
            }

            obj.pdf_dict_putp(key, mupdf.mupdf.pdf_new_text_string(eyecatcher));
            testkey = obj.pdf_dict_getp(key);
            if (testkey.pdf_is_string() != 0)
                throw new Exception(string.Format("cannot insert value for '{0}'", key));

            string temp = mupdf.mupdf.pdf_to_text_string(testkey);
            if (temp != eyecatcher)
                throw new Exception(string.Format("cannot insert value for '{0}'", key));

            FzBuffer res = Object2Buffer(obj, 1, 0);
            string objStr = Utils.EscapeStrFromBuffer(res);

            string nullVal = string.Format("{0}({1})", skey, eyecatcher);
            string newVal = string.Format("%s %s", skey, value);
            string newStr = objStr.Replace(nullVal, newVal);

            PdfObj newObj = ObjectFromStr(pdf, newStr);
            return newObj;
        }

        public static FzBuffer Object2Buffer(PdfObj what, int compress, int ascii)
        {
            FzBuffer ret = new FzBuffer(512);
            FzOutput output = new FzOutput(ret);
            output.pdf_print_obj(what, compress, ascii);
            ret.fz_terminate_buffer();

            return ret;
        }

        public static PdfObj ObjectFromStr(PdfDocument doc, string src)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(src);
            IntPtr unmanagedBytes = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, unmanagedBytes, bytes.Length);
            SWIGTYPE_p_unsigned_char swigData = new SWIGTYPE_p_unsigned_char(unmanagedBytes, false);
            FzBuffer buffer = mupdf.mupdf.fz_new_buffer_from_copied_data(swigData, (uint)bytes.Length);
            FzStream stream = buffer.fz_open_buffer();
            Marshal.FreeHGlobal(unmanagedBytes);

            PdfLexbuf lexBuffer = new PdfLexbuf(256);
            PdfObj ret = doc.pdf_parse_stm_obj(stream,lexBuffer);

            return ret;
        }

        public void SetKeyXRef(int xref, string key, string value)
        {
            if (IsClosed)
                throw new Exception("Document closed");

            HashSet<char> INVALID_NAME_CHARS = new HashSet<char>(new char[] { ' ', '(', ')', '<', '>', '[', ']', '{', '}', '/', '%', '\0' });
            var invalidChars = new HashSet<char>(INVALID_NAME_CHARS);
            var intersection = invalidChars.Intersect(key);

            if (key != null || (intersection.Any() && !intersection.Equals(new HashSet<char> { '/' })))
            {
                throw new Exception("Bad Key");
            }
            if (!(value is string) || string.IsNullOrEmpty(value) || (value[0] == '/' && INVALID_NAME_CHARS.Intersect(value.Substring(1)).Any()))
            {
                throw new Exception("Bad Value");
            }

            PdfDocument pdf = AsPdfDocument(this);
            int xrefLen = pdf.pdf_xref_len();
            PdfObj obj = null;
            if (Utils.INRANGE(xref, 1, xrefLen - 1) && xref != -1)
            {
                throw new Exception(Utils.ErrorMessages["MS_BAD_XREF"]);
            }

            if (xref != -1)
                obj = pdf.pdf_load_object(xref);
            else
                obj = pdf.pdf_trailer();
            PdfObj nObj = SetObjectValue(obj, key, value);
            if (nObj == null)
                return;

            if (xref != -1)
                pdf.pdf_update_object(xref, nObj);
            else
            {
                int n = nObj.pdf_dict_len();
                for (int i = 0; i < n; i++)
                    obj.pdf_dict_put(
                        nObj.pdf_dict_get_key(i),
                        nObj.pdf_dict_get_val(i)
                        );
            }

        }

        public (string, string) GetKeyXref(int xref, string key)
        {
            if (IsClosed)
                throw new Exception("document closed");

            PdfDocument pdf = AsPdfDocument(this);
            int xrefLen = pdf.pdf_xref_len();
            if (Utils.INRANGE(xref, 1, xrefLen - 1) && xref != -1)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);

            PdfObj obj = null;
            if (xref > 0)
                obj = pdf.pdf_load_object(xref);
            else
                obj = pdf.pdf_trailer();
            if (obj == null)
                return ("null", "null");

            PdfObj subObj = obj.pdf_dict_getp(key);
            if (subObj == null)
                return ("null", "null");

            string type = null;
            string text = null;
            if (subObj.pdf_is_indirect() != 0)
            {
                type = "xref";
                text = $"{subObj.pdf_to_num()} 0 R";
            }
            else if (subObj.pdf_is_array() != 0)
                type = "array";
            else if (subObj.pdf_is_dict() != 0)
                type = "dict";
            else if (subObj.pdf_is_int() != 0)
            {
                type = "int";
                text = $"{subObj.pdf_to_int()} ";
            }
            else if (subObj.pdf_is_real() != 0)
                type = "float";
            else if (subObj.pdf_is_null() != 0)
            {
                type = "null";
                text = "null";
            }
            else if (subObj.pdf_is_bool() != 0)
            {
                type = "bool";
                if (subObj.pdf_to_bool() != 0)
                    text = "true";
                else
                    text = "false";
            }
            else if (subObj.pdf_is_name() != 0)
            {
                type = "name";
                text = $"/{subObj.pdf_to_name()}";
            }
            else if (subObj.pdf_is_string() != 0)
            {
                type = "string";
                text = Utils.UnicodeFromStr(subObj.pdf_to_text_string());
            }
            else
                type = "unknown";
            if (text is null)
            {
                FzBuffer res = Utils.Object2Buffer(subObj, 1, 0);
                text = Utils.UnicodeFromBuffer(res);
            }

            return (type, text);

        }

        public void Save(
            dynamic filename,
            int garbage = 0,
            int clean = 0,
            int deflate = 0,
            int deflateImages = 0,
            int deflateFonts = 0,
            int incremental = 0,
            int ascii = 0,
            int expand = 0,
            int linear = 0,
            int noNewId = 0,
            int appearance = 0,
            int pretty = 0,
            int encryption = 1,
            int permissions = 4095,
            string ownerPW = null,
            string userPW = null
            )
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document is closed or encrypted");
            if (filename is string)
            {
                //do something
            }
            if (Len < 1)
                throw new Exception("cannot save with zero pages");
            if ((userPW != null && userPW.Length > 40) || (ownerPW != null && ownerPW.Length > 40))
                throw new Exception("password length must not exceed 40");

            PdfDocument pdf = AsPdfDocument(this);
            PdfWriteOptions opts = new PdfWriteOptions();
            opts.do_incremental = incremental;
            opts.do_ascii = ascii;
            opts.do_compress = deflate;
            opts.do_compress_images = deflateImages;
            opts.do_compress_fonts = deflateFonts;
            opts.do_decompress = expand;
            opts.do_garbage = garbage;
            opts.do_pretty = pretty;
            opts.do_linear = linear;
            opts.do_clean = clean;
            opts.do_sanitize = clean;
            opts.dont_regenerate_id = noNewId;
            opts.do_appearance = appearance;
            opts.do_encrypt = encryption;
            opts.permissions = permissions;
            if (ownerPW != null)
                opts.opwd_utf8_set_value(ownerPW);
            else if (userPW != null)
                opts.opwd_utf8_set_value(userPW);
            if (userPW != null)
                opts.upwd_utf8_set_value(userPW);

            FzOutput output = null;
            pdf.m_internal.resynth_required = 0;
            Utils.EmbeddedClean(pdf);

            /*if (noNewId == 0)
                Utils.EnsureIdentity();*/

            if (filename is string)
                pdf.pdf_save_document(filename, opts);
            else
            {
                output = new FilePtrOutput(filename);
                pdf.pdf_write_document(output, opts);
            }
        }

        public int InsertPage(
            int pno,
            dynamic text = null,
            float fontSize = 11.0f,
            float width = 595,
            float height = 842,
            string fontName = "helv",
            string fontFile = null,
            float[] color = null
        )
        {
            MuPDFPage page = NewPage(pno, width, height);
            if (text == null)
                return 0;
            int rc = page.InsertText(
                new Point(50, 72),
                text,
                fontSize: fontSize,
                fontName: fontName,
                fontFile: fontFile,
                color: color
                );
            return rc;
        }

        public void ResetPageRefs()
        {
            if (IsClosed)
                return;
            if (PageRefs != null)
                PageRefs.Clear();
        }

        public MuPDFPage this[int i]
        {
            get
            {
                if (i == -1)
                    i = Len - 1;
                if (i < 0 || i > Len)
                {
                    throw new Exception($"Page {i} not in document");
                }
                return new MuPDFPage(GetPage(i), this);
            }
        }

        public MuPDFPage NewPage(
            int pno = -1,
            float width = 595,
            float height = 842
            )
        {
            if (IsClosed || Is_Encrypted)
                throw new Exception("document closed or encrypted");
            else
            {
                PdfDocument pdf = AsPdfDocument(this);
                FzRect mediaBox = new FzRect(FzRect.Fixed.Fixed_UNIT);
                mediaBox.x1 = width;
                mediaBox.y1 = height;
                FzBuffer contents = new FzBuffer();

                if (pno < -1)
                    throw new Exception(Utils.ErrorMessages["MSG_BAD_PAGENO"]);
                PdfObj resources = pdf.pdf_add_new_dict(1);
                PdfObj pageObj = pdf.pdf_add_page(mediaBox, 0, resources, contents);
                pdf.pdf_insert_page(pno, pageObj);
            }
            
            ResetPageRefs();
            return this[pno];
        }

        public List<List<dynamic>> GetPageFonts(int pno, bool full = false)
        {
            if (IsClosed || Is_Encrypted)
                throw new Exception("document closed or encrypted");
            if (!IsPDF)
                return null;
            List<List<dynamic>> val = GetPageInfo(pno, 1);
            List<List<dynamic>> ret = new List<List<dynamic>>();
            if (full == false)
            {
                foreach (List<dynamic> v in val)
                {
                    v.RemoveAt(v.Count - 1);
                    ret.Add(v);
                }
            }
            return ret;
        }

        private List<List<dynamic>> GetPageInfo(int pno, int what)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = AsPdfDocument(this);
            int pageCount = mupdf.mupdf.fz_count_pages(_nativeDocument);
            int n = pno;

            while (n < 0)
                n += pageCount;
            if (n >= pageCount)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_PAGENO"]);

            PdfObj pageRef = pdf.pdf_lookup_page_obj(n);
            PdfObj rsrc = pageRef.pdf_dict_get_inheritable(new PdfObj("Resources"));
            
            List<List<dynamic>> liste = new List<List<dynamic>>();
            List<dynamic> tracer = new List<dynamic>();

            if (rsrc != null)
                Utils.ScanResources(pdf, rsrc, liste, what, 0, tracer);
            return liste;
        }

        public MuPDFPage LoadPage(int pageId)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            int np;
            if (pageId < 0)
            {
                np = GetPageCount();
                while (pageId < 0)
                    pageId += np;
            }

            FzPage page = _nativeDocument.fz_load_page(pageId);
            MuPDFPage val = new MuPDFPage(page, this);

            val.ThisOwn = true;
            val.Parent = this;
            PageRefs[val.GetHashCode()] = val;
            val.AnnotRefs = new Dictionary<int, dynamic>();
            val.Number = pageId;

            return val;
        }

        public MuPDFPage LoadPage(int chapter, int pagenum)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");

            FzPage page = _nativeDocument.fz_load_chapter_page(chapter, pagenum);
            MuPDFPage val = new MuPDFPage(page, this);

            val.ThisOwn = true;
            val.Parent = this;
            PageRefs[val.GetHashCode()] = val;
            val.AnnotRefs = new Dictionary<int, dynamic>();
            val.Number = 0;

            return val;
        }

        public MuPDFPage ReloadPage(MuPDFPage page)
        {
            Dictionary<int, dynamic> oldAnnots = new Dictionary<int, dynamic>();
            int pno = page.Number;
            foreach ((int k, dynamic v) in page.AnnotRefs)
                oldAnnots.Add(k, v);

            int old_ref = page.GetPdfPage().super().m_internal.refs;
            long m_internal_old = page.GetPdfPage().super().m_internal_value();

            page.Dispose();
            page.Erase();
            page = null;
            Utils.StoreShrink(100);

            page = LoadPage(pno);

            foreach ((int k, dynamic v) in oldAnnots)
            {
                dynamic annot = oldAnnots[k];
                page.AnnotRefs[k] = annot;
            }

            if (old_ref == 1)
            {
                // pass
            }
            else
            {
                long m_internal_new = page.GetPdfPage().super().m_internal_value();
                Debug.Assert(m_internal_old != m_internal_new);
            }

            return page;
        }

        public FontStruct ExtractFont(int xref = 0, int infoOnly = 0, string named = null)
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj obj = pdf.pdf_load_object(xref);
            PdfObj type = obj.pdf_dict_get(new PdfObj("Type"));
            PdfObj subType = obj.pdf_dict_get(new PdfObj("Subtype"));

            if (type.pdf_name_eq(new PdfObj("Font")) != 0 && !subType.pdf_to_name().StartsWith("CIDFontType"))
            {
                PdfObj bName = null;
                PdfObj baseFont = obj.pdf_dict_get(new PdfObj("BaseFont"));
                if (baseFont == null || baseFont.pdf_is_null() != 0)
                {
                    bName = obj.pdf_dict_get(new PdfObj("Name"));
                }
                else
                {
                    bName = baseFont;
                }
                string ext = Utils.GetFontExtension(pdf, xref);
                byte[] bytes = null;
                if (ext != "n/a" && infoOnly == 0)
                {
                    FzBuffer buf = Utils.GetFontBuffer(pdf, xref);
                    bytes = Utils.BinFromBuffer(buf);
                }
                else
                    bytes = Encoding.UTF8.GetBytes("");

                return new FontStruct()
                {
                    Name = Utils.EscapeStrFromStr(bName.pdf_to_name()),
                    Ext = Utils.UnicodeFromStr(ext),
                    Type = Utils.UnicodeFromStr(subType.pdf_to_name()),
                    Content = bytes
                };
            }
            else
            { 
                return new FontStruct()
                {
                    Name = "",
                    Ext = "",
                    Type = "",
                    Content = Encoding.UTF8.GetBytes("")
                };
                
            }
        }

        public List<(int, double)> _GetCharWidths(int xref, string bfName, string ext, int ordering, int limit, int idx = 0)
        {
            PdfDocument pdf = AsPdfDocument(this);
            int myLimit = limit;
            FzFont font = null;

            if (myLimit < 256)
                myLimit = 256;
            if (ordering >= 0)
            {
                ll_fz_lookup_cjk_font_outparams cjk = new ll_fz_lookup_cjk_font_outparams();
                SWIGTYPE_p_unsigned_char data = mupdf.mupdf.ll_fz_lookup_cjk_font_outparams_fn(ordering, cjk);

                font = mupdf.mupdf.fz_new_font_from_memory(null, data, cjk.len, cjk.index, 0);
            }
            else
            {
                ll_fz_lookup_base14_font_outparams base14 = new ll_fz_lookup_base14_font_outparams();
                SWIGTYPE_p_unsigned_char data = mupdf.mupdf.ll_fz_lookup_base14_font_outparams_fn(bfName, base14);
                if (data != null)
                    font = mupdf.mupdf.fz_new_font_from_memory(bfName, data, base14.len, 0, 0);
                else
                {
                    FzBuffer buf = Utils.GetFontBuffer(pdf, xref);
                    if (buf == null)
                        throw new Exception($"font at xref {xref} is not supported");
                    font = mupdf.mupdf.fz_new_font_from_buffer(null, buf, idx, 0);
                }
            }
            List<(int, double)> wList = new List<(int, double)>();
            for (int i = 0; i < myLimit; i++)
            {
                int glyph = font.fz_encode_character(i);
                float adv = font.fz_advance_glyph(glyph, 0);
                if (ordering >= 0)
                    glyph = i;
                if (glyph > 0)
                    wList.Add((glyph, adv));
                else
                    wList.Add((glyph, 0.0f));
            }

            return wList;
        }

        public string GetXrefObject(int xref, int compressed = 0, int ascii = 0)
        {
            if (IsClosed)
                throw new Exception("document closed");
            PdfDocument pdf = AsPdfDocument(this);
            int xrefLen = pdf.pdf_xref_len();
            PdfObj obj = null;

            if (Utils.INRANGE(xref, 1, xrefLen - 1) && xref != -1)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);
            if (xref > 0)
                obj = pdf.pdf_load_object(xref);
            else
                obj = pdf.pdf_trailer();

            FzBuffer res = Utils.Object2Buffer(obj.pdf_resolve_indirect(), compressed, ascii);
            string text = Utils.EscapeStrFromBuffer(res);

            return text;
        }

        public List<List<dynamic>> GetPageXObjects(int pno)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (!IsPDF)
                return new List<List<dynamic>>();
            List<List<dynamic>> val = GetPageInfo(pno, 3);
            return val;
        }

        public List<List<dynamic>> GetPageImages(int pno, bool full = false)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (!IsPDF)
                return new List<List<dynamic>>();
            List<List<dynamic>>  val = GetPageInfo(pno, 2);
            if (full == false)
            {
                List<List<dynamic>> ret = new List<List<dynamic>>();
                foreach (List<dynamic> v in val)
                    ret.Add(v.Take(v.Count - 1).ToList());
                return ret;
            }

            return val;
        }

        /// <summary>
        /// PDF only: Delete a page given by its 0-based number in -∞ < pno < page_count - 1.
        /// </summary>
        /// <param name="pno">the page to be deleted. Negative number count backwards from the end of the document (like with indices). Default is the last page.</param>
        /// <exception cref="Exception"></exception>
        public void DeletePage(int pno = -1)
        {
            if (!IsPDF)
                throw new Exception("is no PDF");
            if (IsClosed)
                throw new Exception("document closed");

            int pageCount = this.Len;
            while (pno < 0)
                pno += pageCount;

            if (pno >= pageCount)
                throw new Exception("bad page number(s)");

            // remove TOC bookmarks pointing to deleted page
            
        }

        /*public List<dynamic> GetToc(bool simple)
        {
            if (IsClosed)
                throw new Exception("document closed");
            InitDocument();
            Outline olItem = Outline;
            if (olItem == null)
                return null;

            int lvl = 1;
            List<dynamic> liste = new List<dynamic>();

            olItem.v
        }*/

        public (List<int>, float, float) ResolveLink(string uri = null, int chapters = 0)
        {
            fz_location loc = null;
            float xp = 0.0f;
            float yp = 0.0f;

            if (uri == null)
            {
                if (chapters != 0)
                    return (new List<int>() { -1, -1}, 0, 0);
                return (new List<int>() { -1 }, 0, 0);
            }
            try
            {
                ll_fz_resolve_link_outparams outparams = new ll_fz_resolve_link_outparams();
                loc = mupdf.mupdf.ll_fz_resolve_link_outparams_fn(_nativeDocument.m_internal, uri, outparams);
                xp = outparams.xp;
                yp = outparams.yp;
            }
            catch (Exception)
            {
                if (chapters != 0)
                    return (new List<int>() { -1, -1 }, 0, 0);
                return (new List<int>() { -1 }, 0, 0);
            }

            if (chapters != 0)
                return (new List<int>() { loc.chapter, loc.page }, xp, yp);
            int pno = _nativeDocument.fz_page_number_from_location(new FzLocation(loc));
            return (new List<int>() { pno }, xp, yp);
        }

        private string ObjString(PdfObj obj)
        {
            FzBuffer buffer = mupdf.mupdf.fz_new_buffer(512);
            FzOutput output = new FzOutput(buffer);
            output.pdf_print_obj(obj, 1, 0);
            return Utils.UnicodeFromBuffer(buffer);
        }

        private DestNameStruct GetArray(PdfObj val, Dictionary<int, int> page_refs)
        {
            DestNameStruct template = new DestNameStruct() { Page = -1, Dest = "" };

            string array = "";
            if (val.pdf_is_indirect() != 0)
                val = val.pdf_resolve_indirect();
            if (val.pdf_is_array() != 0)
                array = ObjString(val);
            else if (val.pdf_is_dict() != 0)
                array = ObjString(val.pdf_dict_gets("D"));
            else return template;

            // replace PDF "null" by zero, omit the square brackets
            array = array.Replace("null", "0").Substring(1, -1);

            // find stuff before first /
            int idx = array.IndexOf("/");
            if (idx < 1)
            {
                template.Dest = array;
                return template;
            }

            string subval = array.Substring(0, idx);
            array = array.Substring(idx);
            template.Dest = array;

            if (array.StartsWith("/XYZ"))
            {
                template.Dest = "";
                string[] arr_t = array.Split();
                string[] arr = new string[5];
                Array.Copy(arr_t, arr, arr_t.Length - 1);
                float x = float.Parse(arr_t[0]);
                float y = float.Parse(arr_t[1]);
                float z = float.Parse(arr_t[2]);
                template.To = new Point(x, y);
                template.Zoom = z;
            }

            // extract page number
            if (subval.Contains("0 R"))
                template.Page = page_refs[int.Parse(subval.Split()[0])];
            else
                template.Page = int.Parse(subval);

            return template;
        }

        private void FillDict(Dictionary<string, DestNameStruct> destDict, PdfObj pdfDict, Dictionary<int, int> page_refs)
        {
            int nameCount = pdfDict.pdf_dict_len();

            for (int i = 0; i < nameCount; i++)
            {
                PdfObj key = pdfDict.pdf_dict_get_key(i);
                PdfObj val = pdfDict.pdf_dict_get_val(i);
                string dictKey = "";
                if (key.pdf_is_name() != 0)
                    dictKey = key.pdf_to_name();
                else
                {
                    Console.WriteLine($"key {i} is no /Name");
                    dictKey = null;
                }

                if (dictKey != null)
                {
                    destDict.Add(dictKey, GetArray(val, page_refs));
                }
            }
        }

        /// <summary>
        /// PDF only: Convert destination names into a  dict.
        /// </summary>
        /// <returns>PDF only: Convert destination names into a  dict.</returns>
        public Dictionary<string, dynamic> ResolveNames()
        {
            Dictionary<int, int> page_refs = new Dictionary<int, int>();
            for (int i = 0; i < Len; i++)
                page_refs.Add(GetPageXref(i), i);

            PdfDocument pdf = this.PdfDocument;

            // access PDF catalog
            PdfObj catalog = pdf.pdf_trailer().pdf_dict_gets("Root");
            Dictionary<string, DestNameStruct> destDict = new Dictionary<string, DestNameStruct>();
            PdfObj dests = mupdf.mupdf.pdf_new_name("Dests");

            PdfObj oldDests = catalog.pdf_dict_get(dests);
            if (oldDests.pdf_is_dict() != 0)
                FillDict(destDict, oldDests, page_refs);

            PdfObj tree = pdf.pdf_load_name_tree(dests);
            if (tree.pdf_is_dict() != 0)
                FillDict(destDict, tree, page_refs);

            Dictionary<string, dynamic> ret = new Dictionary<string, dynamic>();
            foreach ((string k, DestNameStruct v) in destDict)
                ret.Add(k, v);

            return ret;
        }

        
        /// <summary>
        /// PDF only: Return whether the document contains signature fields. This is an optional PDF property: if not present (return value -1), no conclusions can be drawn – the PDF creator may just not have bothered using it.
        /// </summary>
        /// <returns>int</returns>
        public int GetSigFlags()
        {
            PdfDocument pdf = AsPdfDocument(this);
            if (pdf == null)
                return -1;
            PdfObj sigflags = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "AcroForm", "SigFlags" });
            int sigflag = -1;
            if (sigflags != null)
                sigflag = sigflags.pdf_to_int();
            return sigflag;
        }

        /// <summary>
        /// PDF only: Get the document XML metadata.
        /// </summary>
        /// <returns>XML metadata of the document. Empty string if not present or not a PDF.</returns>
        public string GetXmlMetadata()
        {
            PdfObj xml = null;
            PdfDocument pdf = AsPdfDocument(this);
            if (pdf != null)
            {
                xml = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "Metadata" });
            }

            string rc = "";
            if (xml != null)
            {
                FzBuffer buff = xml.pdf_load_stream();
                rc = Utils.UnicodeFromBuffer(buff);
            }

            return rc;
        }

        /// <summary>
        /// PDF only: Add an arbitrary supported document to the current PDF. Opens “infile” as a document, converts it to a PDF and then invokes Document.insert_pdf(). Parameters are the same as for that method. Among other things, this features an easy way to append images as full pages to an output PDF.
        /// </summary>
        /// <param name="infile"></param>
        /// <param name="fromPage"></param>
        /// <param name="toPage"></param>
        /// <param name="startAt"></param>
        /// <param name="rotate"></param>
        /// <param name="links"></param>
        /// <param name="annots"></param>
        /// <param name="showProgress"></param>
        /// <param name="final"></param>
        /// <exception cref="Exception"></exception>
        public void InsertFile(MuPDFDocument infile, int fromPage = -1, int toPage = -1, int startAt = -1, int rotate = -1,
            bool links = true, bool annots = true, int showProgress = 0, int final = 1)
        {
            MuPDFDocument src = infile;
            if (src == null)
                throw new Exception("bad infile parameter");
            if (!src.IsPDF)
            {
                byte[] pdfBytes = src.Convert2Pdf();
                src = new MuPDFDocument("pdf", pdfBytes);
            }

            InsertPdf(src, fromPage, toPage, startAt, rotate, links, annots, showProgress, final);
        }

        public void InsertPdf(MuPDFDocument docSrc, int fromPage = -1, int toPage = -1, int startAt = -1, int rotate = -1,
            bool links = true, bool annots = true, int showProgress = 0, int final = 1, MuPDFGraftMap gmap = null)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (GraftID == docSrc.GraftID)
                throw new Exception("source and target cannot be same object");
            int sa = startAt;
            if (sa < 0)
                sa = GetPageCount();
            if (docSrc.Len > showProgress && showProgress > 0)
            {
                string inname = Path.GetFileName(docSrc.Name);
                if (inname == null)
                    inname = "memory PDF";
                string outname = Path.GetFileName(Name);
                if (outname == null)
                    outname = "memory PDF";
                Console.WriteLine(string.Format("Inserting {0} at {1}", inname, outname));
            }

            int isrt = docSrc.GraftID;
            Dictionary<string, string> t = new Dictionary<string, string>();

            gmap = GraftMaps.GetValueOrDefault(isrt, null);

            PdfDocument pdfout = AsPdfDocument(this);
            PdfDocument pdfsrc = AsPdfDocument(docSrc);
            int outCount = _nativeDocument.fz_count_pages();
            int srcCount = docSrc.ToFzDocument().fz_count_pages();

            int fp = fromPage;
            int tp = toPage;
            sa = startAt;

            fp = Math.Max(fp, 0);
            fp = Math.Min(fp, srcCount - 1);

            if (tp < 0)
                tp = srcCount - 1;
            tp = Math.Min(tp, srcCount - 1);

            if (sa < 0)
                sa = outCount;
            sa = Math.Min(sa, outCount);

            if (pdfout == null || pdfsrc == null)
                throw new Exception("source or target not a PDF");
            Utils.MergeRange(new MuPDFDocument(pdfout), new MuPDFDocument(pdfsrc), fp, tp, sa, rotate, links, annots, showProgress, gmap);

            ResetPageRefs();
            if (links)
                Utils.DoLinks(this, docSrc, fromPage, toPage, sa);
            if (final == 1)
                GraftMaps[isrt] = null;
        }

        /// <summary>
        /// Show if undo and / or redo are possible.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public (bool, bool) JournalCanDo()
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            int undo = 0;
            int redo = 0;
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
            undo = pdf.pdf_can_undo();
            redo = pdf.pdf_can_redo();
            return (undo != 0, redo != 0);
        }

        /// <summary>
        /// Activate document journalling.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void JournalEnable()
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
            pdf.pdf_enable_journal();
        }

        /// <summary>
        /// Check if journalling is enabled.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool IsEnabledJournal()
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
            bool enabled = (pdf != null) && (pdf.m_internal.journal != null);
            return enabled;
        }

        /// <summary>
        /// Load a journal from a file.
        /// </summary>
        /// <param name="filename">File name for loading journal</param>
        /// <exception cref="Exception"></exception>
        public void JournalLoad(string filename)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);

            pdf.pdf_load_journal(filename);
            if (pdf.m_internal.journal == null)
                throw new Exception("Journal and document do not match");
        }

        /// <summary>
        /// Load a journal from a file.
        /// </summary>
        /// <param name="journal">Journal bytes</param>
        /// <exception cref="Exception"></exception>
        public void JournalLoad(byte[] journal)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
            FzBuffer res = Utils.BufferFromBytes(journal);
            FzStream stream = res.fz_open_buffer();
            pdf.pdf_deserialise_journal(stream);

            if (pdf.m_internal.journal == null)
                throw new Exception("Journal and document do not match");
        }

        /// <summary>
        /// Show operation name for given step.
        /// </summary>
        /// <param name="step">Steps to redo or undo</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string JournalOpName(int step)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
            string name = pdf.pdf_undoredo_step(step);

            return name;
        }

        /// <summary>
        /// Show journalling state.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public (int, int) JournalPosition()
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
            (int, int) rc = pdf.pdf_undoredo_state();

            return rc;
        }

        /// <summary>
        /// Save journal to a file
        /// </summary>
        /// <param name="filename"></param>
        /// <exception cref="Exception"></exception>
        public void JournalSave(string filename)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
            pdf.pdf_save_journal(filename);
        }

        /// <summary>
        /// Save journal to a file
        /// </summary>
        /// <param name="journal"></param>
        /// <exception cref="Exception"></exception>
        public void JournalSave(byte[] journal)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);

            MemoryStream memoryStream = new MemoryStream(journal.Length);
            memoryStream.Write(journal, 0, journal.Length);

            FilePtrOutput output = new FilePtrOutput(memoryStream);
            pdf.pdf_write_journal(output);
        }

        /// <summary>
        /// Begin a journaling operation.
        /// </summary>
        /// <param name="name"></param>
        /// <exception cref="Exception"></exception>
        public void JournalStartOp(string name)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
            if (pdf.m_internal.journal == null)
                throw new Exception("Journalling not enabled");
            if (name != null && name != "")
                pdf.pdf_begin_operation(name);
            else
                pdf.pdf_begin_implicit_operation();
        }

        public void Dispose()
        {
            _nativeDocument.Dispose();
        }

        public void Select()
        {
            
        }
    }
}
