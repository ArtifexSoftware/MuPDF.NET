using mupdf;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

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

        public Dictionary<(int, int), int> ShownPages = new Dictionary<(int, int), int>();

        public string InsertedImages = "";

        private FzDocument _nativeDocument;

        public Dictionary<int, MuPDFPage> PageRefs;

        public string Name = null;

        public PdfDocument PdfDocument;

        public List<byte> Stream;

        private bool _isPDF;

        public bool NeedsPass
        {
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
                _isPDF = value;
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

        public string Language
        {
            get
            {
                PdfDocument pdf = AsPdfDocument(_nativeDocument);
                if (pdf == null)
                    return null;
                fz_text_language lang = mupdf.mupdf.pdf_document_language(pdf);
                if (lang == fz_text_language.FZ_LANG_UNSET)
                    return null;
                if (Utils.MUPDF_VERSION.CompareTo((1, 23, 7)) < 0)
                    throw new Exception("not implemented yet'");
                return mupdf.mupdf.fz_string_from_text_language2(lang);
            }
        }

        public (int, int) LastLocation
        {
            get
            {
                if (IsClosed)
                    throw new Exception("document closed");
                FzLocation lastLoc = _nativeDocument.fz_last_page();
                return (lastLoc.chapter, lastLoc.page);
            }
        }

        public string PageLayout
        {
            get
            {
                int xref = GetPdfCatelog();
                if (xref == 0)
                    return null;
                (string, string) rc = GetKeyXref(xref, "PageLayout");
                if (rc.Item1 == "null")
                    return "SinglePage";
                if (rc.Item1 == "name")
                    return rc.Item2.Substring(1);
                return "SinglePage";
            }
        }

        public string PageMode
        {
            get
            {
                int xref = GetPdfCatelog();
                if (xref == 0)
                    return null;
                (string, string) rc = GetKeyXref(xref, "PageMode");
                if (rc.Item1 == "null")
                    return "UseNone";
                if (rc.Item1 == "name")
                    return rc.Item2.Substring(1);
                return "UseNone";
            }
        }

        public Dictionary<string, bool> MarkInfo
        {
            get
            {
                int xref = GetPdfCatelog();
                string val;
                if (xref == 0)
                    return null;
                (string, string) rc = GetKeyXref(xref, "MarkInfo");
                if (rc.Item1 == "null")
                    return new Dictionary<string, bool>();
                if (rc.Item1 == "xref")
                {
                    xref = Convert.ToInt32(rc.Item2.Split(" ")[0]);
                    val = GetXrefObject(xref, compressed: 1);
                }
                else if (rc.Item1 == "dict")
                    val = rc.Item2;
                else
                    val = null;
                if (val == null || (val.Substring(0, 2) == "<<" && val.Substring(-2) == ">>"))
                    return new Dictionary<string, bool>();
                Dictionary<string, bool> valid = new Dictionary<string, bool>()
                {
                    { "Marked", false },
                    { "UserProperties", false },
                    { "Suspects", false}
                };

                string[] valArray = val.Substring(2, -2).Split("/").Skip(1).ToArray();
                foreach (string v in valArray)
                {
                    string[] kv = v.Split(" ");
                    if (kv.Length == 2 && kv[1] == "true")
                        valid.Add(kv[0], true);
                }
                return valid;
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
                }

                if (fromFile && Stream != null && (new FileInfo(filename).Length == 0 || Stream.Count == 0))
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
                                        if (Utils.MUPDF_VERSION.Item1 == 1 && Utils.MUPDF_VERSION.Item2 >= 24)
                                        {
                                            FzStream _stream = new FzStream(filename);
                                            FzStream accel = new FzStream();
                                            FzArchive archive = new FzArchive();
                                            doc = new FzDocument(mupdf.mupdf.ll_fz_document_open_fn_call(handler.open, _stream.m_internal,
                                                accel.m_internal, archive.m_internal));
                                        }
                                        /*else
                                        {
                                            doc = new FzDocument(mupdf.mupdf.ll_fz_document_open_fn_call(handler.open, filename));
                                        }*/
                                    }
                                    catch (Exception)
                                    {
                                        throw new Exception(Utils.ErrorMessages["MSG_BAD_DOCUMENT"]);
                                    }
                                }
                                else if (Utils.MUPDF_VERSION.Item1 >= 1 && Utils.MUPDF_VERSION.Item2 >= 24)
                                {
                                    Debug.Assert(false);
                                    ///////////////////////// in less than version 1.24
                                    /*data = mupdf.mupdf.fz_open_file(filename);
                                    doc.m_internal = mupdf.mupdf.ll_fz_document_open_with_stream_fn_call(handler.open_with_stream, data.m_internal);*/
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
                PdfObj resources = new PdfObj();
                FzBuffer contents = new FzBuffer();
                FzDevice dev = pdfout.pdf_page_write(mediabox, resources, contents);
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

            for (i = len0; i < len1; i++)
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
            PdfObj ret = doc.pdf_parse_stm_obj(stream, lexBuffer);

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

            if (rsrc.m_internal != null)
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
            List<List<dynamic>> val = GetPageInfo(pno, 2);
            if (full == false)
            {
                List<List<dynamic>> ret = new List<List<dynamic>>();
                foreach (List<dynamic> v in val)
                    ret.Add(v.Take(v.Count - 1).ToList());
                return ret;
            }

            return val;
        }

        private void _DeletePage(int pno)
        {
            PdfDocument pdf = AsPdfDocument(this);
            pdf.pdf_delete_page(pno);
            if (pdf.m_internal.rev_page_map != null)
                mupdf.mupdf.ll_pdf_drop_page_tree(pdf.m_internal);

        }

        /// <summary>
        /// Create a table of contents.
        /// </summary>
        /// <param name="simple">a bool to control output.</param>
        /// <returns>Returns a list, where each entry consists of outline level, title, page number and link destination (if simple = False). For details see PyMuPDF's documentation.</returns>
        /// <exception cref="Exception"></exception>
        public List<List<dynamic>> GetToc(bool simple = true)
        {
            List<List<dynamic>> Recurse(Outline olItem, List<List<dynamic>> list, int lvl)
            {
                while (olItem != null)
                {
                    string title = "";
                    int page = -1;
                    if (olItem.Title != null)
                        title = olItem.Title;

                    if (!olItem.IsExternal)
                    {
                        if (olItem.Uri != null)
                        {
                            if (olItem.Page == -1)
                            {
                                (List<int>, float, float) resolve = ResolveLink(olItem.Uri);
                                // issue
                            }
                            else
                                page = olItem.Page + 1;
                        }
                        else
                            page = -1;
                    }
                    else
                        page = -1;

                    if (!simple)
                    {
                        LinkStruct link = Utils.GetLinkStruct(olItem, this);
                        list.Add(new List<dynamic>() { lvl, title, page, link });
                    }
                    else
                        list.Add(new List<dynamic>() { lvl, title, page });

                    if (olItem.Down != null)
                        list = Recurse(olItem.Down, list, lvl + 1);
                    olItem = olItem.Next;
                }

                return list;
            }

            if (IsClosed)
                throw new Exception("document closed");
            InitDocument();
            Outline olItem = Outline;
            if (olItem == null)
                return null;

            int lvl = 1;
            List<List<dynamic>> liste = new List<List<dynamic>>();
            List<List<dynamic>> toc = Recurse(olItem, liste, lvl);

            if (IsPDF && simple == false)
                ExtendTocItems(toc);

            return toc;
        }

        /// <summary>
        /// Convert the PDF's destination names into a Python dict.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="chapters"></param>
        /// <returns></returns>
        public (List<int>, float, float) ResolveLink(string uri = null, int chapters = 0)
        {
            fz_location loc = null;
            float xp = 0.0f;
            float yp = 0.0f;

            if (uri == null)
            {
                if (chapters != 0)
                    return (new List<int>() { -1, -1 }, 0, 0);
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

        /// <summary>
        /// Return string version of a PDF object definition.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private string ObjString(PdfObj obj)
        {
            FzBuffer buffer = mupdf.mupdf.fz_new_buffer(512);
            FzOutput output = new FzOutput(buffer);
            output.pdf_print_obj(obj, 1, 0);
            return Utils.UnicodeFromBuffer(buffer);
        }

        /// <summary>
        /// Generate value of one item of the names dictionary.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="page_refs"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Insert a page range from another PDF.
        /// </summary>
        /// <param name="docSrc">PDF to copy from. Must be different object, but may be same file.</param>
        /// <param name="fromPage">first source page to copy, 0-based, default 0.</param>
        /// <param name="toPage">last source page to copy, 0-based, default last page.</param>
        /// <param name="startAt">from_page will become this page number in target.</param>
        /// <param name="rotate">rotate copied pages, default -1 is no change.</param>
        /// <param name="links">whether to also copy links.</param>
        /// <param name="annots">whether to also copy annotations.</param>
        /// <param name="showProgress">progress message interval, 0 is no messages.</param>
        /// <param name="final"></param>
        /// <param name="gmap">internal use only</param>
        /// <exception cref="Exception"></exception>
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

            ByteStream memoryStream = new ByteStream(journal);

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

        /// <summary>
        /// End a journalling operation.
        /// </summary>
        public void JournalStopOp()
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = AsPdfDocument(_nativeDocument);
            pdf.pdf_end_operation();
        }

        /// <summary>
        /// Move backwards in the journal.
        /// </summary>
        /// <returns>true</returns>
        /// <exception cref="Exception">document closed or encrypted</exception>
        public bool JournalUndo()
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = AsPdfDocument(_nativeDocument);
            pdf.pdf_undo();
            return true;
        }

        /// <summary>
        /// Show OC visibility status modifiable by user.
        /// </summary>
        /// <returns></returns>
        public List<LayerConfigUI> LayerUIConfigs()
        {
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
            PdfLayerConfigUi info = new PdfLayerConfigUi();
            int n = pdf.pdf_count_layer_config_ui();
            string type;

            List<LayerConfigUI> rc = new List<LayerConfigUI>();
            for (int i = 0; i < n; i++)
            {
                pdf.pdf_layer_config_ui_info(i, info);
                switch ((int)info.type)
                {
                    case 1:
                        type = "checkbox";
                        break;
                    case 2:
                        type = "radiobox";
                        break;
                    default:
                        type = "label";
                        break;
                }

                LayerConfigUI item = new LayerConfigUI()
                {
                    Number = i,
                    Text = info.text,
                    Depth = info.depth,
                    Type = type,
                    On = info.selected != 0,
                    IsLocked = info.locked != 0
                };
                rc.Add(item);
            }
            return rc;
        }

        /// <summary>
        /// Re-layout a reflowable document.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="fontSize"></param>
        /// <exception cref="Exception"></exception>
        public void SetLayout(Rect rect = null, float width = 0, float height = 0, int fontSize = 11)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            FzDocument doc = _nativeDocument;
            if (doc.fz_is_document_reflowable() == 0)
                return;
            float w = width;
            float h = height;
            FzRect r = rect.ToFzRect();
            if (r.fz_is_infinite_rect() == 0)
            {
                w = r.x1 - r.x0;
                h = r.y1 - r.y0;
            }
            if (w <= 0.0f || h <= 0.0f)
                throw new Exception("bad page size");
            doc.fz_layout_document(w, h, fontSize);

            ResetPageRefs();
            InitDocument();
        }

        /// <summary>
        /// Convert pno to (chapter, page)
        /// </summary>
        /// <param name="pno"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public (int, int) GetLocationFromPageNumber(int pno)
        {
            if (IsClosed)
            {
                throw new Exception("document is closed");
            }
            FzDocument doc = _nativeDocument;
            FzLocation loc = mupdf.mupdf.fz_make_location(-1, -1);
            int pageCount = doc.fz_count_pages();
            while (pno < 0)
                pno += pageCount;
            if (pno >= pageCount)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_PAGENO"]);
            loc = doc.fz_location_from_page_number(pno);
            return (loc.chapter, loc.page);
        }

        /// <summary>
        /// Make a page pointer before layouting document.
        /// </summary>
        /// <param name="locNumbers">Contains chapter and page numbers</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public ulong MakeBookmark((int, int) locNumbers)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            FzLocation loc = new FzLocation(locNumbers.Item1, locNumbers.Item2);
            ulong mark = mupdf.mupdf.ll_fz_make_bookmark2(_nativeDocument.m_internal, loc.internal_());
            return mark;
        }

        /// <summary>
        /// Get xref of PDF catalog.
        /// </summary>
        /// <returns></returns>
        public int GetPdfCatelog()
        {
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
            int xref = 0;
            if (pdf == null)
                return xref;
            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            xref = root.pdf_to_num();

            return xref;
        }

        /// <summary>
        /// Get PDF trailer as a string.
        /// </summary>
        /// <param name="compressed"></param>
        /// <param name="ascii"></param>
        /// <returns></returns>
        public string GetPdfTrailer(int compressed = 0, int ascii = 0)
        {
            return GetXrefObject(-1, compressed, ascii);
        }

        /// <summary>
        /// Move a page within a PDF document.
        /// </summary>
        /// <param name="pno">source page number.</param>
        /// <param name="to">put before this page, '-1' means after last page.</param>
        /// <exception cref="Exception"></exception>
        public void MovePage(int pno, int to = -1)
        {
            if (IsClosed)
                throw new Exception("document closed");
            int pageCount = GetPageCount();
            if (pno >= pageCount || (to < -1 && to >= pageCount))
                throw new Exception("bad page numbers(s)");
            bool before = true;
            bool copy = false;
            if (to == -1)
            {
                to = pageCount - 1;
                before = false;
            }
            MoveCopyPage(pno, to, before, copy);
        }

        private void MoveCopyPage(int pno, int nb, bool before, bool copy)
        {
            PdfDocument pdf = AsPdfDocument(_nativeDocument);
            bool same;
            (PdfObj page1, PdfObj parent1, int i1) = pdf.pdf_lookup_page_loc(pno);
            PdfObj kids1 = parent1.pdf_dict_get(new PdfObj("Kids"));

            (PdfObj page2, PdfObj parent2, int i2) = pdf.pdf_lookup_page_loc(nb);
            PdfObj kids2 = parent2.pdf_dict_get(new PdfObj("Kids"));

            PdfObj parent;
            int pos;
            if (before)
                pos = i2;
            else
                pos = i2 + 1;

            same = mupdf.mupdf.pdf_objcmp(kids1, kids2) == 0; // if same, true else false
            if (!copy && !same)
                page1.pdf_dict_put(new PdfObj("Parent"), parent2);
            kids2.pdf_array_insert(page1, pos);

            if (!same) // not same
            {
                parent = parent2;
                while (parent != null)
                {
                    int count = parent.pdf_dict_get_int(new PdfObj("Count"));
                    parent.pdf_dict_put_int(new PdfObj("Count"), count + 1);
                    parent = parent.pdf_dict_get(new PdfObj("Parent"));
                }
                if (!copy)
                {
                    kids1.pdf_array_delete(i1);
                    parent = parent1;
                    while (parent != null)
                    {
                        int count = parent.pdf_dict_get_int(new PdfObj("Count"));
                        parent.pdf_dict_put_int(new PdfObj("Count"), count - 1);
                        parent = parent.pdf_dict_get(new PdfObj("Parent"));
                    }
                }
            }
            else
            {

            }
        }

        /// <summary>
        /// Get/set the NeedAppearances value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int NeedAppearances(int value = 0)
        {
            if (IsFormPDF == 0)
                return 0;
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
            int oldVal = -1;
            string appkey = "NeedAppearances";

            PdfObj form = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root/AcroForm" });
            PdfObj app = form.pdf_dict_gets(appkey);
            if (app.pdf_is_bool() == 1)
                oldVal = app.pdf_to_bool();
            if (value != 0)
                form.pdf_dict_puts(appkey, new PdfObj(mupdf.mupdf.PDF_ENUM_TRUE));
            else
                form.pdf_dict_puts(appkey, new PdfObj(mupdf.mupdf.PDF_ENUM_FALSE));
            if (value == 0)
                return Convert.ToInt32(oldVal >= 0);
            return value;
        }

        /// <summary>
        /// Get (chapter, page) of next page.
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public (int, int) NextLocation(int pageId)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            (int, int) _pageId;
            _pageId = (0, pageId);
            // issue
            if (_pageId == LastLocation)
                return (-1, -1);
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(_nativeDocument);
            int val = _pageId.Item1;
            int chapter = val;
            val = _pageId.Item2;
            int pno = val;
            FzLocation loc = mupdf.mupdf.fz_make_location(chapter, pno);
            FzLocation nextLoc = mupdf.mupdf.fz_next_page(_nativeDocument, loc);
            return (nextLoc.chapter, nextLoc.page);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<(int, pdf_annot_type, string)> PageAnnotXrefs(int n)
        {
            PdfDocument pdf = AsPdfDocument(_nativeDocument);
            int pageCount = pdf.pdf_count_pages();
            while (n < 0)
            {
                n += pageCount;
            }

            if (n > pageCount)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_PAGENO"]);
            PdfObj pageObj = pdf.pdf_lookup_page_obj(n);
            return Utils.GetAnnotXrefList(pageObj);
        }

        /// <summary>
        /// PDF only: Return the unrotated page rectangle – without loading the page
        /// </summary>
        /// <param name="pno">0-based page number.</param>
        /// <returns>Rect of the page</returns>
        /// <exception cref="Exception"></exception>
        public Rect PageCropBox(int pno)
        {
            if (IsClosed)
                throw new Exception("document closed");
            FzDocument doc = _nativeDocument;
            int pageCount = doc.fz_count_pages();
            int n = pno;
            while (n < 0)
                n += pageCount;
            PdfDocument pdf = AsPdfDocument(doc);
            if (n >= pageCount)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_PAGENO"]);
            PdfObj pageRef = pdf.pdf_lookup_page_obj(n);
            Rect cropbox = Utils.GetCropBox(pageRef);

            return cropbox;
        }

        /// <summary>
        /// Convert (chapter, pno) to page number.
        /// </summary>
        /// <param name="pageId">page id</param>
        /// <returns>chapter and pno</returns>
        public int GetPageNumberFromLocation(int pageId)
        {
            int pageN = GetPageCount();
            while (pageId < 0)
                pageId += pageN;
            (int, int) _pageId = (0, pageId);
            // issue: Check whether pageId is in this
            (int chapter, int pno) = _pageId;
            FzLocation loc = mupdf.mupdf.fz_make_location(chapter, pno);
            pageN = _nativeDocument.fz_page_number_from_location(loc);
            return pageN;
        }

        /// <summary>
        /// PDF only: Return the xref of the page – without loading the page
        /// </summary>
        /// <param name="pno">0-based page number</param>
        /// <returns>xref of the page</returns>
        /// <exception cref="Exception"></exception>
        public int PageXref(int pno)
        {
            if (IsClosed)
                throw new Exception("document closed");
            int pageCount = _nativeDocument.fz_count_pages();
            int n = pno;
            while (n < 0)
                n += pageCount;
            PdfDocument pdf = AsPdfDocument(_nativeDocument);
            int xref = 0;
            if (n >= pageCount)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_PAGENO"]);
            xref = pdf.pdf_lookup_page_obj(n).pdf_to_num();
            return xref;
        }

        /// <summary>
        /// A generator for a range of pages.
        /// </summary>
        /// <param name="start">start iteration with this page number</param>
        /// <param name="stop">stop iteration at this page number.</param>
        /// <param name="step">stop iteration at this page number.</param>
        /// <returns>a generator iterator over the document’s pages.</returns>
        /// <exception cref="Exception"></exception>
        public List<MuPDFPage> GetPages(int start, int stop, int step)
        {
            while (start < 0)
                start += GetPageCount();
            if (!(GetPageCount() > start && start >= 0))
                throw new Exception("bad start page number");
            stop = (stop <= GetPageCount()) ? stop : GetPageCount();
            if (step == 0)
                throw new Exception("arg 3 must not be zero");

            if ((start > stop && step > 0) || (start < stop && step < 0))
                throw new Exception("bad step, pick right direction");

            List<MuPDFPage> ret = new List<MuPDFPage>();
            for (int i = start; i < stop; i += step)
            {
                ret.Add(LoadPage(i));
            }

            return ret;
        }

        /// <summary>
        /// Add new form font.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="font"></param>
        /// <exception cref="Exception"></exception>
        public void AddFromFont(string name, string font)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");

            PdfDocument pdf = AsPdfDocument(this);
            if (pdf == null)
                return;

            PdfObj fonts = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[]
            {
                "Root", "AcroFrom", "DR", "Font"
            });

            if (fonts.m_internal == null || fonts.pdf_is_dict() == 0)
                throw new Exception("PDF has no form fonts yet");
            PdfObj k = mupdf.mupdf.pdf_new_name(name);
            PdfObj v = Utils.PdfObjFromStr(pdf, font);
            fonts.pdf_dict_put(k, v);
        }

        /// <summary>
        /// Add color info to all items of an extended TOC list.
        /// </summary>
        /// <param name="items"></param>
        /// <exception cref="Exception"></exception>
        public void ExtendTocItems(List<List<dynamic>> items)
        {
            if (IsClosed)
                throw new Exception("document closed");
            PdfDocument pdf = AsPdfDocument(this);
            string zoom = "zoom";
            string bold = "bold";
            string italic = "italic";
            string collapse = "collapse";

            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            if (root.m_internal == null)
                return;

            PdfObj olRoot = root.pdf_dict_get(new PdfObj("Outlines"));
            if (olRoot.m_internal == null)
                return;

            PdfObj first = olRoot.pdf_dict_get(new PdfObj("First"));
            if (first.m_internal == null)
                return;

            List<int> xrefs = new List<int>();
            xrefs = Utils.GetOutlineXrefs(first, xrefs);
            int n = xrefs.Count;
            int m = items.Count;

            if (n == 0)
                return;
            if (n != m)
                throw new Exception("internal error finding outline xrefs");

            for (int i = 0; i < n; i++)
            {
                int xref = xrefs[i];
                List<dynamic> item = items[i];
                LinkStruct link;
                if (item.Count == 4)
                    link = item[3];
                else
                    throw new Exception("need non-simple TOC format");

                link.Xref = xrefs[i];
                PdfObj bm = pdf.pdf_load_object(xref);
                int flags = bm.pdf_dict_get(new PdfObj("F")).pdf_to_int();
                if (flags == 1)
                    link.Italic = true;
                else if (flags == 2)
                    link.Bold = true;
                else if (flags == 3)
                {
                    link.Italic = true;
                    link.Bold = true;
                }
                int count = bm.pdf_dict_get(new PdfObj("F")).pdf_to_int();
                if (count < 0)
                    link.Collapse = true;
                else if (count > 0)
                    link.Collapse = false;
                PdfObj col = bm.pdf_dict_get(new PdfObj("C"));
                float[] color = null;
                if (col.pdf_is_array() != 0 && col.pdf_array_len() == 3)
                {
                    color = new float[3]
                    {
                        col.pdf_array_get(0).pdf_to_real(),
                        col.pdf_array_get(1).pdf_to_real(),
                        col.pdf_array_get(2).pdf_to_real(),
                    };
                    link.Color = color;
                }

                float z = 0;
                PdfObj obj = bm.pdf_dict_get(new PdfObj("Dest"));
                if (obj.m_internal == null || obj.pdf_is_array() == 0)
                {
                    obj = Utils.pdf_dict_getl(bm, new string[] { "A", "D" });
                }

                if (obj.pdf_is_array() != 0 && obj.pdf_array_len() == 5)
                {
                    z = obj.pdf_array_get(4).pdf_to_real();
                }

                link.Zoom = z;
                item[3] = link;
                items[i] = item;
            }
        }

        /// <summary>
        /// Remove a page from document page dict.
        /// </summary>
        /// <param name="page"></param>
        public void ForgetPage(MuPDFPage page)
        {
            int pid = page.GetHashCode();
            if (PageRefs.ContainsKey(pid))
            {
                PageRefs.Remove(pid);
            }
        }

        public List<(int, string)> GetPageLabels()
        {
            PdfDocument pdf = AsPdfDocument(this);
            List<(int, string)> rc = new List<(int, string)>();

            PdfObj pageLabels = mupdf.mupdf.pdf_new_name("PageLabels");
            PdfObj obj = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "PageLabels" });
            if (obj.m_internal == null)
                return rc;

            PdfObj nums = obj.pdf_dict_get(new PdfObj("Nums")).pdf_resolve_indirect();
            if (nums.m_internal != null)
            {
                Utils.GetPageLabels(rc, nums);
                return rc;
            }

            nums = Utils.pdf_dict_getl(obj, new string[] { "Kids", "Nums" }).pdf_resolve_indirect();
            if (nums.m_internal != null)
            {
                Utils.GetPageLabels(rc, nums);
                return rc;
            }

            PdfObj kids = obj.pdf_dict_get(new PdfObj("Kids")).pdf_resolve_indirect();
            if (kids.m_internal == null || kids.pdf_is_array() == 0)
            {
                return rc;
            }

            int n = kids.pdf_array_len();
            for (int i = 0; i < n; i++)
            {
                nums = kids.pdf_array_get(i).pdf_dict_get(new PdfObj("Nums")).pdf_resolve_indirect();
                Utils.GetPageLabels(rc, nums);
            }
            return rc;
        }

        /// <summary>
        /// Get xref of Outline Root, create it if missing.
        /// </summary>
        public int GetOlRootNumber()
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = AsPdfDocument(this);

            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            PdfObj olRoot = root.pdf_dict_get(new PdfObj("Outlines"));

            if (olRoot.m_internal == null)
            {
                olRoot = pdf.pdf_new_dict(4);
                olRoot.pdf_dict_put(new PdfObj("Type"), new PdfObj("Outlines"));
                PdfObj indObj = pdf.pdf_add_object(olRoot);
                root.pdf_dict_put(new PdfObj("Outlines"), indObj);
                olRoot = root.pdf_dict_get(new PdfObj("Outlines"));
            }

            return olRoot.pdf_to_num();
        }

        /// <summary>
        /// Get PDF file id.
        /// </summary>
        /// <returns>string list or null</returns>
        public List<string> GetPdfFileID()
        {
            PdfDocument pdf = AsPdfDocument(this);
            if (pdf == null)
                return null;

            List<string> idList = new List<string>();
            PdfObj identity = pdf.pdf_trailer().pdf_dict_get(new PdfObj("ID"));
            if (identity.m_internal != null)
            {
                int n = identity.pdf_array_len();
                for (int i = 0; i < n; i++)
                {
                    PdfObj o = identity.pdf_array_get(i);
                    string text = o.pdf_to_text_string();
                    byte[] ba = Encoding.Default.GetBytes(text);
                    var hexString = BitConverter.ToString(ba);
                    string hex = hexString.Replace("-", "");

                    idList.Add(hex);
                }
            }
            return idList;
        }

        /// <summary>
        /// Make an array page number -> page object.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void MakePageMap()
        {
            if (IsClosed)
                throw new Exception("document closed");
        }

        public void RemoveLinksTo(List<int> numbers)
        {
            PdfDocument pdf = AsPdfDocument(this);
            Utils.RemoveDestRange(pdf, numbers);
        }

        /// <summary>
        /// "remove" bookmark by letting it point to nowhere
        /// </summary>
        /// <param name="xref"></param>
        public void RemoveTocItem(int xref)
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj item = pdf.pdf_new_indirect(xref, 0);
            item.pdf_dict_del(new PdfObj("Dest"));
            item.pdf_dict_del(new PdfObj("A"));
            PdfObj color = pdf.pdf_new_array(3);
            for (int i = 0; i < 3; i++)
                color.pdf_array_push_real(0.8f);
            item.pdf_dict_put(new PdfObj("C"), color);
        }

        public void SetPageLabels(string labels)
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));

            root.pdf_dict_del(new PdfObj("PageLabels"));
            Utils.pdf_dict_putl(root, mupdf.mupdf.pdf_new_array(pdf, 0), new string[] { "PageLabels", "Nums" });
            int xref = GetPdfCatelog();
            string text = GetXrefObject(xref, compressed: 1);
            text = text.Replace("/Nums[]", $"/Nums[{labels}]");
            UpdateObject(xref, text);
        }

        /// <summary>
        /// Replace xref stream part.
        /// </summary>
        /// <param name="xref"></param>
        /// <param name="stream"></param>
        /// <param name="_new"></param>
        /// <param name="compress"></param>
        /// <exception cref="Exception"></exception>
        public void UpdateStream(int xref, byte[] stream = null, int _new = 1, int compress = 1)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = AsPdfDocument(this);
            int xrefLen = pdf.pdf_xref_len();
            if (xref < 1 || xref > xrefLen)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);
            PdfObj obj = pdf.pdf_new_indirect(xref, 0);
            if (obj.pdf_is_dict() == 0)
                throw new Exception(Utils.ErrorMessages["MSG_IS_NO_DICT"]);
            FzBuffer res = Utils.BufferFromBytes(stream);
            if (res == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_BUFFER"]);
            Utils.UpdateStream(pdf, obj, res, compress);
            // pdfdocument does not have `dirty` property
        }

        /// <summary>
        /// Replace object definition source.
        /// </summary>
        /// <param name="xref"></param>
        /// <param name="text"></param>
        /// <param name="page"></param>
        /// <exception cref="Exception"></exception>
        public void UpdateObject(int xref, string text, PdfPage page = null)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = AsPdfDocument(this);
            int xrefLen = pdf.pdf_xref_len();
            if (!Utils.INRANGE(xref, 1, xrefLen - 1))
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);
            PdfObj newObj = Utils.PdfObjFromStr(pdf, text);
            pdf.pdf_update_object(xref, newObj);

            Utils.RefreshLinks(page);
        }

        public void CopyPage(int pno, int to = -1)
        {
            if (IsClosed)
                throw new Exception("document closed");
            int pageCount = Len;
            if (!(pno < pageCount) || !Utils.INRANGE(to, -1, pageCount - 1))
                throw new Exception("bad page number(s)");

            int before = 1;
            int copy = 1;
            if (to == -1)
            {
                to = pageCount - 1;
                before = 0;
            }

            MoveCopyPage(pno, to, before != 0, copy != 0);
        }

        /// <summary>
        /// Delete XML metadata.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void DeleteXmlMetadata()
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            if (root.m_internal != null)
                root.pdf_dict_del(new PdfObj("Metadata"));
        }

        /// <summary>
        /// PDF only: Delete a page given by its 0-based number in -∞ < pno < page_count - 1.
        /// </summary>
        /// <param name="pno">the page to be deleted. Negative number count backwards from the end of the document (like with indices). Default is the last page.</param>
        /// <exception cref="Exception"></exception>
        public void DeletePage(int pno = -1)
        {
            if (!IsPDF)
                throw new Exception("is no pdf");
            if (IsClosed)
                throw new Exception("document is closed");

            int pageCount = Len;
            while (pno < 0)
                pno += pageCount;

            if (pno >= pageCount)
                throw new Exception("bad page number(s)");

            List<List<dynamic>> toc = GetToc();
            List<int> olXrefs = GetOutlineXrefs();

            for (int i = 0; i < toc.Count; i++)
            {
                if (toc[i][2] == pno + 1)
                    RemoveTocItem(olXrefs[i]);
            }

            RemoveLinksTo(new List<int>() { pno });
            _DeletePage(pno);
            ResetPageRefs();
        }

        public void DeletePages(int from = -1, int to = -1)
        {
            if (!IsPDF)
                throw new Exception("is no PDF");
            if (IsClosed)
                throw new Exception("document is closed");
            int pageCount = Len;
            List<int> numbers = new List<int>();

            while (from < 0)
                from += pageCount;
            while (to < 0)
                to += pageCount;

            for (int i = from; i < to; i++)
                numbers.Add(i);
            if (numbers.Count == 0)
            {
                Console.WriteLine("noting to delete");
                return;
            }

            numbers.Sort();
            if (numbers[0] < 0 || numbers[numbers.Count - 1] >= pageCount)
                throw new ArgumentException("bad page number(s)");
            List<List<dynamic>> toc = GetToc();
            List<int> olXrefs = GetOutlineXrefs();
            for (int i = 0; i < olXrefs.Count; i++)
            {
                if (numbers.Contains(toc[i][2] - 1))
                    RemoveTocItem(olXrefs[i]);
            }
            RemoveLinksTo(numbers);
            numbers.Reverse();
            foreach (int j in numbers)
            {
                DeletePage(j);
            }

            ResetPageRefs();
        }

        public void DeletePages(List<int> numbers)
        {
            if (numbers.Count == 0)
            {
                Console.WriteLine("noting to delete");
                return;
            }

            numbers.Sort();
            if (numbers[0] < 0 || numbers[numbers.Count - 1] >= Len)
                throw new ArgumentException("bad page number(s)");
            List<List<dynamic>> toc = GetToc();
            List<int> olXrefs = GetOutlineXrefs();
            for (int i = 0; i < olXrefs.Count; i++)
            {
                if (numbers.Contains(toc[i][2] - 1))
                    RemoveTocItem(olXrefs[i]);
            }
            RemoveLinksTo(numbers);
            numbers.Reverse();
            foreach (int j in numbers)
            {
                DeletePage(j);
            }

            ResetPageRefs();
        }

        public void DeletePages(int[] nums)
        {
            List<int> numbers = new List<int>(nums);
            if (numbers.Count == 0)
            {
                Console.WriteLine("noting to delete");
                return;
            }

            numbers.Sort();
            if (numbers[0] < 0 || numbers[numbers.Count - 1] >= Len)
                throw new ArgumentException("bad page number(s)");
            List<List<dynamic>> toc = GetToc();
            List<int> olXrefs = GetOutlineXrefs();
            for (int i = 0; i < olXrefs.Count; i++)
            {
                if (numbers.Contains(toc[i][2] - 1))
                    RemoveTocItem(olXrefs[i]);
            }
            RemoveLinksTo(numbers);
            numbers.Reverse();
            foreach (int j in numbers)
            {
                DeletePage(j);
            }

            ResetPageRefs();
        }

        public List<int> GetOutlineXrefs()
        {
            List<int> xrefs = new List<int>();
            PdfDocument pdf = AsPdfDocument(this);
            if (!IsPDF)
                return xrefs;
            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            if (root.m_internal == null)
                return xrefs;

            PdfObj olRoot = root.pdf_dict_get(new PdfObj("Outlines"));
            if (olRoot.m_internal == null)
                return xrefs;

            PdfObj first = olRoot.pdf_dict_get(new PdfObj("First"));
            if (first.m_internal == null)
                return xrefs;

            xrefs = Utils.GetOutlineXrefs(first, xrefs);
            return xrefs;
        }

        public int AddEmbfile(string name, byte[] buffer, string filename = null, string ufilename = null, string desc = null)
        {
            List<string> filenames = GetEmbfileNames();
            string msg = $"Name {name} already exists.";
            if (filenames.Contains(name))
                throw new Exception(msg);

            if (filename == null)
                filename = name;
            if (ufilename == null)
                ufilename = filename;
            if (desc == null)
                desc = name;
            int xref = _AddEmbfile(name, buffer, filename, ufilename, desc);
            string date = Utils.GetPdfNow();
            SetKeyXRef(xref, "Type", "/EmbeddedFile");
            SetKeyXRef(xref, "Params/CreationDate", Utils.GetPdfStr(date));
            SetKeyXRef(xref, "Params/ModDate", Utils.GetPdfStr(date));

            return xref;
        }

        public int _AddEmbfile(string name, byte[] buffer, string filename = null, string ufilename = null, string desc = null)
        {
            PdfDocument pdf = AsPdfDocument(this);
            FzBuffer data = Utils.BufferFromBytes(buffer);
            if (data.m_internal == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_BUFFER"]);

            PdfObj names = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "Names", "EmbeddedFiles", "Names" });
            if (names.pdf_is_array() == 0)
            {
                PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
                names = pdf.pdf_new_array(6);
                Utils.pdf_dict_putl(root, names, new string[] { "Names", "EmbeddedFiles", "Names" });
            }

            PdfObj fileEntry = Utils.EmbedFile(pdf, data, filename, ufilename, desc, 1);
            int xref = Utils.pdf_dict_getl(fileEntry, new string[] { "EF", "F" }).pdf_to_num();
            names.pdf_array_push(mupdf.mupdf.pdf_new_text_string(name));
            names.pdf_array_push(fileEntry);

            return xref;
        }

        /// <summary>
        /// Get list of names of EmbeddedFiles.
        /// </summary>
        /// <returns></returns>
        public List<string> GetEmbfileNames()
        {
            List<string> names = new List<string>();
            _EmbfileNames(names);
            return names;
        }

        private void _EmbfileNames(List<string> filenames)
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj names = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "Names", "EmbeddedFiles", "Names" });
            if (names.pdf_is_array() != 0)
            {
                int n = names.pdf_array_len();
                for (int i = 0; i < n; i += 2)
                {
                    string val = Utils.EscapeStrFromStr(names.pdf_array_get(i).pdf_to_text_string());
                    filenames.Add(val);
                }
            }
        }

        public int GetEmbfileCount()
        {
            return GetEmbfileNames().Count;
        }

        /// <summary>
        /// Delete an entry from EmbeddedFiles.
        /// </summary>
        /// <param name="item">name or number of item.></param>
        public void DeleteEmbfile(int item)
        {
            int idx = EmbeddedfileIndex(item);
            _DeleteEmbfile(idx);
        }

        /// <summary>
        /// Delete an entry from EmbeddedFiles.
        /// </summary>
        /// <param name="item">name or number of item.></param>
        public void DeleteEmbfile(string item)
        {
            int idx = EmbeddedfileIndex(item);
            _DeleteEmbfile(idx);
        }

        public void _DeleteEmbfile(int idx)
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj names = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "Names", "EmbeddedFiles", "Names" });
            names.pdf_array_delete(idx + 1);
            names.pdf_array_delete(idx);
        }

        public int EmbeddedfileIndex(dynamic item)
        {
            List<string> filenames = GetEmbfileNames();
            string msg = $"{item} not in EmbeddedFiles array";
            int idx = 0;

            if (item is string && filenames.Contains(item))
                idx = filenames.IndexOf(item);
            else if (item is int && Utils.INRANGE(item, 0, filenames.Count - 1))
                idx = item;
            else throw new Exception(msg);
            return idx;
        }

        public byte[] _GetEmbeddedFile(int idx)
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj names = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "Names", "EmbeddedFiles", "Names" });
            PdfObj entry = names.pdf_array_get(2 * idx + 1);
            PdfObj fileSpec = Utils.pdf_dict_getl(entry, new string[] { "EF", "F" });
            FzBuffer buf = fileSpec.pdf_load_stream();
            byte[] cont = Utils.BinFromBuffer(buf);

            return cont;
        }

        /// <summary>
        /// Get the content of an item in the EmbeddedFiles array.
        /// </summary>
        /// <param name="item"></param>
        public byte[] GetEmbfile(int item)
        {
            int idx = EmbeddedfileIndex(item);
            return _GetEmbeddedFile(idx);
        }

        public EmbfileInfoStruct GetEmbfileInfo(dynamic item)
        {
            int index = EmbeddedfileIndex(item);
            EmbfileInfoStruct infoDict = new EmbfileInfoStruct() { Name = GetEmbfileNames()[index] };
            PdfDocument pdf = AsPdfDocument(this);
            int xref = 0;
            int ciXref = 0;

            PdfObj trailer = pdf.pdf_trailer();
            PdfObj names = Utils.pdf_dict_getl(trailer, new string[] { "Root", "Names", "EmbeddedFiles", "Names" });
            PdfObj o = names.pdf_array_get(2 * index + 1);
            PdfObj ci = o.pdf_dict_get(new PdfObj("CI"));
            if (ci.m_internal != null)
                ciXref = ci.pdf_to_num();

            infoDict.Collection = ciXref;
            string name = o.pdf_dict_get(new PdfObj("F")).pdf_to_text_string();
            infoDict.FileName = Utils.EscapeStrFromStr(name);

            name = o.pdf_dict_get(new PdfObj("UF")).pdf_to_text_string();
            infoDict.UFileName = Utils.EscapeStrFromStr(name);

            name = o.pdf_dict_get(new PdfObj("Desc")).pdf_to_text_string();
            infoDict.Desc = Utils.UnicodeFromStr(name);

            int len = -1;
            int DL = -1;
            PdfObj fileEntry = Utils.pdf_dict_getl(o, new string[] { "EF", "F" });
            xref = fileEntry.pdf_to_num();
            o = fileEntry.pdf_dict_get(new PdfObj("Length"));
            if (o.m_internal != null)
                len = o.pdf_to_int();

            o = fileEntry.pdf_dict_get(new PdfObj("DL"));
            if (o.m_internal != null)
                DL = o.pdf_to_int();
            else
            {
                o = Utils.pdf_dict_getl(fileEntry, new string[] { "Params", "Size" });
                if (o.m_internal != null)
                    DL = o.pdf_to_int();
            }
            infoDict.Size = DL;
            infoDict.Length = len;
            (string t, string date) = GetKeyXref(xref, "Params/CreationDate");
            if (t != "null")
                infoDict.ModDate = date;
            (t, string md5) = GetKeyXref(xref, "Params/ModDate");
            if (t != "null")
            {
                byte[] textBytes = Encoding.UTF8.GetBytes(md5);
                string hexString = BitConverter.ToString(textBytes).Replace("-", "").ToLower();
                infoDict.CheckSum = hexString;
            }
            return infoDict;
        }

        /// <summary>
        /// Change an item of the EmbeddedFiles array.
        /// </summary>
        /// <param name="item">number or name</param>
        /// <param name="buffer">new file content</param>
        /// <param name="filename">new file name</param>
        /// <param name="ufilename">unicode new file name</param>
        /// <param name="desc">the new description</param>
        /// <returns></returns>
        public int GetEmbfileUpd(dynamic item, byte[] buffer = null, string filename = null, string ufilename = null,
            string desc = null)
        {
            int idx = EmbeddedfileIndex(item);
            PdfDocument pdf = AsPdfDocument(this);
            int xref = 0;
            PdfObj names = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "Names", "EmbeddedFiles", "Names" });
            PdfObj entry = names.pdf_array_get(2 * idx + 1);
            PdfObj fileSpec = Utils.pdf_dict_getl(entry, new string[] { "EF", "F" });
            if (fileSpec.m_internal == null)
                throw new Exception("bad PDF: no /EF object");
            FzBuffer res = Utils.BufferFromBytes(buffer);
            if (buffer != null && res.m_internal == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_BUFFER"]);
            if (res.m_internal != null && buffer != null)
            {
                Utils.UpdateStream(pdf, fileSpec, res, 1);
                uint len = res.fz_buffer_storage(null);
                PdfObj l = mupdf.mupdf.pdf_new_int(len);
                fileSpec.pdf_dict_put(new PdfObj("DL"), l);
                Utils.pdf_dict_putl(fileSpec, l, new string[] { "Params", "Size" });
            }
            xref = fileSpec.pdf_to_num();
            if (!string.IsNullOrEmpty(filename))
                entry.pdf_dict_put_text_string(new PdfObj("F"), filename);
            if (!string.IsNullOrEmpty(ufilename))
                entry.pdf_dict_put_text_string(new PdfObj("UF"), ufilename);
            if (!string.IsNullOrEmpty(desc))
                entry.pdf_dict_put_text_string(new PdfObj("Desc"), desc);

            string date = Utils.GetPdfNow();
            SetKeyXRef(xref, "Params/ModDate", Utils.GetPdfStr(date));
            return xref;
        }

        /// <summary>
        /// Get image by xref. Returns a dictionary.
        /// </summary>
        /// <param name="xref"></param>
        /// <returns></returns>
        public ImageInfoStruct ExtractImage(int xref)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");

            PdfDocument pdf = AsPdfDocument(this);
            int imgType = 0;
            int smask = 0;
            string ext = null;
            FzBuffer res;
            FzImage img;
            if (!Utils.INRANGE(xref, 1, pdf.pdf_xref_len() - 1))
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);

            PdfObj obj = pdf.pdf_new_indirect(xref, 0);
            PdfObj subtype = obj.pdf_dict_get(new PdfObj("Subtype"));
            if (subtype.pdf_name_eq(new PdfObj("Image")) != 0) // mismatch
                throw new Exception("not an image");

            PdfObj o = obj.pdf_dict_geta(new PdfObj("SMask"), new PdfObj("Mask"));
            if (o.m_internal != null)
                smask = o.pdf_to_num();
            if (obj.pdf_is_jpx_image() != 0)
            {
                imgType = (int)ImageType.FZ_IMAGE_JPX;
                res = obj.pdf_load_stream();
                ext = "jpx";
            }
            if (Utils.IsJbig2Image(obj))
            {
                imgType = (int)ImageType.FZ_IMAGE_JBIG2;
                res = obj.pdf_load_stream();
                ext = "jb2";
            }
            res = obj.pdf_load_raw_stream();
            if (imgType == (int)ImageType.FZ_IMAGE_UNKNOWN)
            {
                res = obj.pdf_load_raw_stream();
                ll_fz_buffer_storage_outparams outparams = new ll_fz_buffer_storage_outparams();
                uint len = mupdf.mupdf.ll_fz_buffer_storage_outparams_fn(res.m_internal, outparams);
                imgType = mupdf.mupdf.fz_recognize_image_format(outparams.datap);
                ext = Utils.GetImageExtention(imgType);
            }
            if (imgType == (int)ImageType.FZ_IMAGE_UNKNOWN)
            {
                res = null;
                img = pdf.pdf_load_image(obj);
                fz_compressed_buffer llCbuf = mupdf.mupdf.ll_fz_compressed_image_buffer(img.m_internal);
                if (llCbuf != null && !(
                    llCbuf.params_.type == (int)ImageType.FZ_IMAGE_RAW ||
                    llCbuf.params_.type == (int)ImageType.FZ_IMAGE_FAX ||
                    llCbuf.params_.type == (int)ImageType.FZ_IMAGE_FLATE ||
                    llCbuf.params_.type == (int)ImageType.FZ_IMAGE_LZW ||
                    llCbuf.params_.type == (int)ImageType.FZ_IMAGE_RLD))
                {
                    imgType = llCbuf.params_.type;
                    ext = Utils.GetImageExtention(imgType);
                    res = new FzBuffer(mupdf.mupdf.ll_fz_keep_buffer(llCbuf.buffer));
                }
                else
                {
                    fz_color_params defaultColorParams = new fz_color_params();
                    defaultColorParams.ri = 1;
                    defaultColorParams.bp = 1;
                    defaultColorParams.op = 0;
                    defaultColorParams.opm = 0;
                    res = img.fz_new_buffer_from_image_as_png(new FzColorParams(defaultColorParams));
                    ext = "png";
                }
            }
            else
                img = res.fz_new_image_from_buffer();

            (float xres, float yres) = img.fz_image_resolution();
            float width = img.w();
            float height = img.h();
            int colorspace = img.n();
            int bpc = img.bpc();
            string csName = img.colorspace().fz_colorspace_name();

            ImageInfoStruct ret = new ImageInfoStruct()
            {
                Ext = ext,
                Smask = smask,
                Width = width,
                Height = height,
                ColorSpace = colorspace,
                Bpc = bpc,
                Xres = xres,
                Yres = yres,
                CsName = csName,
                Image = Utils.BinFromBuffer(res)
            };

            return ret;
        }

        /// <summary>
        /// Find new location after layouting a document.
        /// </summary>
        /// <returns></returns>
        public LocationStruct FindBookmark(int bm)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            FzLocation location = _nativeDocument.fz_lookup_bookmark(bm);
            return new LocationStruct() { Chapter = location.chapter, Page = location.page };
        }

        public void CopyFullPage(int pno, int to = -1)
        {
            PdfDocument pdf = AsPdfDocument(this);
            int pageCount = pdf.pdf_count_pages();
            int xref;
            try
            {
                if (!Utils.INRANGE(pno, 0, pageCount - 1) || !Utils.INRANGE(to, -1, pageCount - 1))
                    throw new Exception(Utils.ErrorMessages["MSG_BAD_PAGENO"]);
                PdfObj page1 = pdf.pdf_lookup_page_obj(pno).pdf_resolve_indirect();
                PdfObj page2 = page1.pdf_deep_copy_obj();
                PdfObj oldAnnots = page2.pdf_dict_get(new PdfObj("Annots"));

                if (oldAnnots.m_internal != null)
                {
                    int n = oldAnnots.pdf_array_len();
                    PdfObj newAnnots = pdf.pdf_new_array(n);
                    for (int i = 0; i < n; i++)
                    {
                        PdfObj o = oldAnnots.pdf_dict_get(i);
                        PdfObj subtype = o.pdf_dict_get(new PdfObj("Subtype"));
                        if (subtype.pdf_name_eq(new PdfObj("Popup")) != 0)
                            continue;
                        if (o.pdf_dict_gets("IRT").m_internal != null)
                            continue;
                        PdfObj copyObj = o.pdf_resolve_indirect().pdf_deep_copy_obj();
                        xref = pdf.pdf_create_object();
                        pdf.pdf_update_object(xref, copyObj);
                        copyObj = pdf.pdf_new_indirect(xref, 0);
                        copyObj.pdf_dict_del(new PdfObj("Popup"));
                        copyObj.pdf_dict_del(new PdfObj("P"));
                        newAnnots.pdf_dict_del(copyObj);
                    }
                    page2.pdf_dict_put(new PdfObj("Annots"), newAnnots);
                }
                FzBuffer res = Utils.ReadContents(page1);

                if (res.m_internal != null)
                {
                    IntPtr pBuf = Marshal.AllocHGlobal(1);
                    Marshal.Copy(Encoding.UTF8.GetBytes(" "), 0, pBuf, 1);
                    SWIGTYPE_p_unsigned_char swigBuf = new SWIGTYPE_p_unsigned_char(pBuf, true);
                    PdfObj contents = pdf.pdf_add_stream(mupdf.mupdf.fz_new_buffer_from_copied_data(swigBuf, 1), new PdfObj(), 0);
                    Utils.UpdateStream(pdf, contents, res, 1);
                    page2.pdf_dict_put(new PdfObj("Contents"), contents);
                }

                xref = pdf.pdf_create_object();
                pdf.pdf_update_object(xref, page2);

                page2 = pdf.pdf_new_indirect(xref, 0);
                pdf.pdf_insert_page(to, page2);
            }
            finally
            {
                mupdf.mupdf.ll_pdf_drop_page_tree(pdf.m_internal);
            }

        }

        /// <summary>
        /// Content of ON, OFF, RBGroups of an OC layer.
        /// </summary>
        /// <param name="config"></param>
        /// <returns>OCLayer object</returns>
        /// <exception cref="Exception"></exception>
        public OCLayer GetLayer(int config = -1)
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj ocp = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "OCProperties" });
            PdfObj obj, o;

            if (ocp.m_internal == null)
                return null;
            if (config == -1)
                obj = ocp.pdf_dict_get(new PdfObj("D"));
            else
            {
                obj = ocp.pdf_dict_get(new PdfObj("Configs")).pdf_array_get(config);
            }
            if (obj.m_internal == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_OC_CONFIG"]);

            OCLayer ret = new OCLayer();

            PdfObj arr = obj.pdf_dict_get(new PdfObj("ON"));
            List<int> list = Utils.GetOcgArraysImp(arr);
            if (list != null || list.Count != 0)
                ret.On = list.ToArray();

            arr = obj.pdf_dict_get(new PdfObj("OFF"));
            list = Utils.GetOcgArraysImp(arr);
            if (list != null || list.Count != 0)
                ret.Off = list.ToArray();

            arr = obj.pdf_dict_get(new PdfObj("Locked"));
            list = Utils.GetOcgArraysImp(arr);
            if (list != null || list.Count != 0)
                ret.Locked = list.ToArray();

            arr = obj.pdf_dict_get(new PdfObj("RBGroups"));
            List<int[]> rb = new List<int[]>();
            if (arr.pdf_is_array() != 0)
            {
                int n = arr.pdf_array_len();
                for (int i = 0; i < n; i++)
                {
                    o = arr.pdf_array_get(i);
                    int[] list1 = Utils.GetOcgArraysImp(o).ToArray();
                    rb.Add(list1);
                }
            }
            if (rb.Count != 0)
                ret.RBGroups = rb;
            o = obj.pdf_dict_get(new PdfObj("BaseState"));
            if (o.m_internal != null)
                ret.BaseState = o.pdf_to_name();
            return ret;
        }

        /// <summary>
        /// Show optional OC layers.
        /// </summary>
        /// <returns>OCLayer config list</returns>
        public List<OCLayerConfig> GetLayers()
        {
            PdfDocument pdf = AsPdfDocument(this);
            int n = pdf.pdf_count_layer_configs();
            if (n == 1)
            {
                PdfObj obj = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "OCProperties", "Configs" });
                if (obj.pdf_is_array() == 0)
                    n = 0;
            }
            List<OCLayerConfig> ret = new List<OCLayerConfig>();
            PdfLayerConfig info = new PdfLayerConfig();
            for (int i = 0; i < n; i++)
            {
                pdf.pdf_layer_config_info(i, info);
                ret.Add(new OCLayerConfig(i, info.name, info.creator));
            }
            return ret;
        }

        /// <summary>
        /// Make new xref
        /// </summary>
        /// <returns>number of xref</returns>
        /// <exception cref="Exception"></exception>
        public int GetNexXref()
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = AsPdfDocument(this);
            int xref = 0;
            Utils.EnsureOperations(pdf);
            xref = pdf.pdf_create_object();
            return xref;
        }

        /// <summary>
        /// Show existing optional content groups.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, OCGroup> GetOcgs()
        {
            PdfObj ci = mupdf.mupdf.pdf_new_name("CreatorInfo");
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj ocgs = Utils.pdf_dict_getl(pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root")), new string[] { "OCProperties", "OCGs" });

            Dictionary<int, OCGroup> ret = new Dictionary<int, OCGroup>();
            if (ocgs.pdf_is_array() == 0)
                return ret;
            int n = ocgs.pdf_array_len();
            for (int i = 0; i < n; i++)
            {
                PdfObj ocg = ocgs.pdf_array_get(i);
                int xref = ocg.pdf_to_num();
                string name = ocg.pdf_dict_get(new PdfObj("Name")).pdf_to_text_string();
                PdfObj obj = Utils.pdf_dict_getl(ocg, new string[] { "Usage", "CreatorInfo", " Subtype" });
                string usage = "";
                if (obj.m_internal != null)
                {
                    usage = obj.pdf_to_name();
                }
                List<string> intents = new List<string>();
                PdfObj intent = ocg.pdf_dict_get(new PdfObj("Intent"));
                if (intent.m_internal != null)
                {
                    if (intent.pdf_is_name() != 0)
                        intents.Add(intent.pdf_to_name());
                    else if (intent.pdf_is_array() != 0)
                    {
                        int m = intent.pdf_array_len();
                        for (int j = 0; j < m; j++)
                        {
                            PdfObj o = intent.pdf_array_get(j);
                            if (o.pdf_is_name() != 0)
                                intents.Add(o.pdf_to_name());
                        }
                    }
                }
                int hidden = pdf.pdf_is_ocg_hidden(new PdfObj(), usage, ocg);
                OCGroup item = new OCGroup()
                {
                    Name = name,
                    Intents = intents,
                    On = 1 - hidden,
                    Usage = usage
                };
                ret[xref] = item;
            }
            return ret;
        }

        /// <summary>
        /// Save a file snapshot suitable for journalling.
        /// </summary>
        /// <param name="filename"></param>
        public void SaveSnapshot(string filename)
        {
            if (IsClosed)
                throw new Exception("doc is closed");
            if (filename == Name)
                throw new Exception("cannot snapshot to original");
            PdfDocument pdf = AsPdfDocument(this);
            pdf.pdf_save_snapshot(filename);
        }

        /// <summary>
        /// Save PDF incrementally
        /// </summary>
        public void SaveIncremental()
        {
            Save(Name, incremental: 1, encryption: (int)PdfCrypt.PDF_ENCRYPT_KEEP);
        }

        public void Dispose()
        {
            _nativeDocument.Dispose();
        }

        /// <summary>
        /// Build sub-pdf with page numbers in the list.
        /// </summary>
        /// <param name="list">numbers of pages</param>
        /// <exception cref="Exception"></exception>
        public void Select(List<int> list)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (!IsPDF)
                throw new Exception("is no PDF");
            if (list.Count == 0 || list.Min() < 0 || list.Max() > Len)
                throw new Exception("bad page number(s)");

            PdfDocument pdf = AsPdfDocument(this);

            IntPtr pNumbers = Marshal.AllocHGlobal(list.Count * sizeof(int));
            Marshal.Copy(list.ToArray(), 0, pNumbers, list.Count);
            SWIGTYPE_p_int swigNumbers = new SWIGTYPE_p_int(pNumbers, true);
            //pdf.pdf_rearrange_pages(list.Count, swigNumbers);
            ResetPageRefs();
        }

        public bool SetLanguage(string language)
        {
            PdfDocument pdf = AsPdfDocument(this);
            fz_text_language lang;
            if (string.IsNullOrEmpty(language))
                lang = fz_text_language.FZ_LANG_UNSET;
            else
                lang = mupdf.mupdf.fz_text_language_from_string(language);
            pdf.pdf_set_document_language(lang);
            return true;
        }

        public void SetLayer(int config, string baseState = null, int[] on = null, int[] off = null,
            List<int[]> rbgroups = null, int[] locked = null)
        {
            if (IsClosed)
                throw new Exception("document is closed");
            HashSet<int> ocgs = new HashSet<int>(GetOcgs().Keys);
            HashSet<int> s;

            if (on != null)
            {
                s = new HashSet<int>(on);
                s.ExceptWith(ocgs);
            }
            if (off != null)
            {
                s = new HashSet<int>(off);
                s.ExceptWith(ocgs);
            }
            if (locked != null)
            {
                s = new HashSet<int>(locked);
                s.ExceptWith(ocgs);
            }
            if (rbgroups != null)
            {
                foreach (int[] x in rbgroups)
                {
                    s = new HashSet<int>(x);
                    s.ExceptWith(ocgs);
                }
            }
            if (!string.IsNullOrEmpty(baseState))
            {
                baseState = baseState.ToUpper();
                if (baseState == "UNCHANGED")
                    baseState = "Unchanged";
                if (!(new List<string>() { "ON", "OFF", "Unchanged" }).Contains(baseState))
                    throw new Exception("bad 'baseState'");
            }
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj obj;
            PdfObj ocp = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "OCProperties" });
            if (ocp.m_internal == null)
                return;
            if (config == -1)
                obj = ocp.pdf_dict_get(new PdfObj("D"));
            else
                obj = ocp.pdf_dict_get(new PdfObj("Configs")).pdf_array_get(config);
            if (obj.m_internal == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_OC_CONFIG"]);

            // set_ocg_arrays
            if (!string.IsNullOrEmpty(baseState))
                obj.pdf_dict_put_name(new PdfObj("BaseState"), baseState);
            if (on != null)
            {
                obj.pdf_dict_del(new PdfObj("ON"));
                PdfObj arr = obj.pdf_dict_put_array(new PdfObj("ON"), 1);
                Utils.SetOcgArraysImp(arr, new List<int>(on));
            }
            if (off != null)
            {
                obj.pdf_dict_del(new PdfObj("OFF"));
                PdfObj arr = obj.pdf_dict_put_array(new PdfObj("OFF"), 1);
                Utils.SetOcgArraysImp(arr, new List<int>(off));
            }
            if (locked != null)
            {
                obj.pdf_dict_del(new PdfObj("Locked"));
                PdfObj arr = obj.pdf_dict_put_array(new PdfObj("Locked"), 1);
                Utils.SetOcgArraysImp(arr, new List<int>(locked));
            }
            if (rbgroups != null)
            {
                obj.pdf_dict_del(new PdfObj("RBGroups"));
                PdfObj arr = obj.pdf_dict_put_array(new PdfObj("RBGroups"), 1);
                int n = rbgroups.Count;
                for (int i = 0; i < n; i++)
                {
                    List<int> item = new List<int>(rbgroups[i]);
                    PdfObj o = arr.pdf_array_push_array(1);
                    Utils.SetOcgArraysImp(o, item);
                }
            }
        }

        /// <summary>
        /// Set / unset OC intent configuration.
        /// </summary>
        /// <param name="number">string or int</param>
        /// <param name="action"></param>
        public void SetLayerUIConfig(dynamic number, int action = 0)
        {
            int num;
            if (number is string)
            {
                List<int> select = LayerUIConfigs().Where(ui => ui.Text == number).Select(ui => ui.Number).ToList();
                if (select.Count == 0)
                {
                    throw new Exception($"bad OCG '{number}'");
                }
                num = select[0];
            }
            else if (number is int) num = number;
            else num = -1;

            PdfDocument pdf = AsPdfDocument(this);
            if (action == 1)
                pdf.pdf_toggle_layer_config_ui(num);
            else if (action == 2)
                pdf.pdf_deselect_layer_config_ui(num);
            else
                pdf.pdf_select_layer_config_ui(num);
        }

        /// <summary>
        /// Set the PDF MarkInfo values.
        /// </summary>
        /// <param name="markInfo"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool SetMarkInfo(Dictionary<string, bool> markInfo)
        {
            int xref = GetPdfCatelog();
            if (xref == 0)
                throw new Exception("not a pdf");
            if (markInfo == null)
                return false;
            Dictionary<string, bool> valid = new Dictionary<string, bool>()
            {
                {"Marked", false }, {"UserProperties", false}, {"Suspects", false}
            };

            if (valid.Keys.Except(markInfo.Keys).Count() <= 0)
                throw new Exception("bad MarkInfo key(s)");
            string pdfDict = "<<";
            foreach ((string k, bool v) in valid)
            {
                string vStr = v.ToString().ToLower();
                if (vStr != "true" || vStr != "false")
                    throw new Exception($"bad key value {k} : {v}");
                pdfDict += $"/{k} {v}";
            }
            pdfDict += ">>";
            SetKeyXRef(xref, "MarkInfo", pdfDict);
            return true;
        }

        /// <summary>
        /// Set the PDF PageLayout value.
        /// </summary>
        /// <param name="pageLayout"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool SetPageLayout(string pageLayout)
        {
            string[] valid = { "SinglePage", "OneColumn", "TwoColumnLeft", "TwoColumnRight", "TwoPageLeft", "TwoPageRight" };
            int xref = GetPdfCatelog();
            if (xref == 0)
                throw new Exception("not a PDF");
            if (string.IsNullOrEmpty(pageLayout))
                throw new Exception("bad PageLayout value");
            if (pageLayout[0] == '/')
                pageLayout = pageLayout.Substring(1);
            foreach (string v in valid)
            {
                if (pageLayout.ToLower() == v.ToLower())
                {
                    SetKeyXRef(xref, "PageLayout", $"/{v}");
                    return true;
                }
            }
            throw new Exception("bad pagelayout value");
        }

        /// <summary>
        /// Set the PDF PageMode value.
        /// </summary>
        /// <param name="pageMode"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool SetPageMode(string pageMode)
        {
            string[] valid = { "UseNone", "UseOutlines", "UseThumbs", "FullScreen", "UseOC", "UseAttachments" };
            int xref = GetPdfCatelog();
            if (xref == 0)
                throw new Exception("not a PDF");
            if (string.IsNullOrEmpty(pageMode))
                throw new Exception("bad page mode value");
            if (pageMode[0] == '/')
                pageMode = pageMode.Substring(1);
            foreach (string v in valid)
            {
                if (pageMode.ToLower() == v.ToLower())
                {
                    SetKeyXRef(xref, "PageMode", $"/{v}");
                    return true;
                }
            }
            throw new Exception("bad page mode value");
        }

        /// <summary>
        /// Store XML document level metadata.
        /// </summary>
        public void SetXmlMetaData(string metadata)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document is closed or encrypted");
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            if (root.m_internal == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_PDFROOT"]);

            byte[] utf8 = Encoding.UTF8.GetBytes(metadata);
            IntPtr pBuf = Marshal.AllocHGlobal(utf8.Length);
            Marshal.Copy(utf8, 0, pBuf, utf8.Length);
            SWIGTYPE_p_unsigned_char swigBuf = new SWIGTYPE_p_unsigned_char(pBuf, true);

            FzBuffer res = mupdf.mupdf.fz_new_buffer_from_copied_data(swigBuf, (uint)utf8.Length);
            PdfObj xml = root.pdf_dict_get(new PdfObj("Metadata"));
            if (xml.m_internal != null)
                Utils.UpdateStream(pdf, xml, res, 0);
            else
            {
                xml = pdf.pdf_add_stream(res, new PdfObj(), 0);
                xml.pdf_dict_put(new PdfObj("Type"), new PdfObj("Metadata"));
                xml.pdf_dict_put(new PdfObj("Subtype"), new PdfObj("XML"));
                root.pdf_dict_put(new PdfObj("Metadata"), xml);
            }
        }

        /// <summary>
        /// Activate an OC layer.
        /// </summary>
        /// <param name="config">config number as returned by layerconfigs</param>
        /// <param name="asDefault">make this the default configuration.</param>
        /// <exception cref="Exception"></exception>
        public void SwitchLayer(int config, int asDefault = 0)
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj cfgs = Utils.pdf_dict_getl(pdf.pdf_trailer(), new string[] { "Root", "OCProperties", "Configs" });
            if (cfgs.pdf_is_array() == 0 || cfgs.pdf_array_len() == 0)
            {
                if (config < 1)
                    return;
                throw new Exception(Utils.ErrorMessages["MSG_BAD_OC_LAYER"]);
            }
            if (config < 0)
                return;
            pdf.pdf_select_layer_config(config);
            if (asDefault != 0)
            {
                pdf.pdf_set_layer_config_as_default();
                mupdf.mupdf.ll_pdf_read_ocg(pdf.m_internal);
            }
        }

        public byte[] Write(bool garbage = false, bool clean = false, bool deflate = false, bool deflateImages = false,
            bool deflateFonts = false, bool incremental = false, bool ascii = false, bool expand = false, bool linear = false,
            bool noNewId = false, bool appearance = false, bool pretty = false, int encryption = 1, int permissions = 4095,
            string ownerPW = null, string userPW = null)
        {
            ByteStream byteStream = new ByteStream();
            Save(
                filename: byteStream,
                garbage: garbage ? 1 : 0,
                clean: clean ? 1 : 0,
                noNewId: noNewId ? 1 : 0,
                appearance: appearance ? 1 : 0,
                deflate: deflate ? 1 : 0,
                deflateImages: deflateImages ? 1 : 0,
                deflateFonts: deflateFonts ? 1 : 0,
                incremental: incremental ? 1 : 0,
                ascii: ascii ? 1 : 0,
                expand: expand ? 1 : 0,
                linear: linear ? 1 : 0,
                pretty: pretty ? 1 : 0,
                encryption: encryption,
                permissions: permissions,
                ownerPW: ownerPW,
                userPW: userPW);
            return byteStream.Data;
        }

        public List<string> GetXrefKeys(int xref)
        {
            PdfDocument pdf = AsPdfDocument(this);
            int len = pdf.pdf_xref_len();
            PdfObj obj;

            if (!Utils.INRANGE(xref, 1, len - 1) && xref != -1)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);
            if (xref > 0)
                obj = pdf.pdf_load_object(xref);
            else
                obj = pdf.pdf_trailer();
            int n = obj.pdf_dict_len();
            List<string> ret = new List<string>();
            if (n == 0)
                return ret;
            for (int i = 0; i < n; i++)
            {
                string key = obj.pdf_dict_get_key(i).pdf_to_name();
                ret.Add(key);
            }
            return ret;
        }

        /// <summary>
        /// Check if xref is an image object.
        /// </summary>
        /// <param name="xref"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool XrefIsFont(int xref)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (GetKeyXref(xref, "Subtype").Item2 == "/Image")
                return true;
            return false;
        }

        /// <summary>
        /// Check if xref is a stream object.
        /// </summary>
        /// <param name="xref"></param>
        /// <returns></returns>
        public bool XrefIsStream(int xref = 0)
        {
            PdfDocument pdf = AsPdfDocument(this);
            if (pdf == null)
                return false;
            return pdf.pdf_obj_num_is_stream(xref) != 0;
        }

        /// <summary>
        /// Check if xref is a form xobject.
        /// </summary>
        /// <param name="xref"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool XrefIsXObject(int xref)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document is closed or encrypted");
            if (GetKeyXref(xref, "Subtype").Item2 == "/From")
                return true;
            return false;
        }

        /// <summary>
        /// Get length of xref table.
        /// </summary>
        /// <returns></returns>
        public int XrefLength()
        {
            int len = 0;
            PdfDocument pdf = AsPdfDocument(this);
            if (pdf != null)
                len = pdf.pdf_xref_len();
            return len;
        }

        public byte[] GetXrefStream(int xref)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = AsPdfDocument(this);
            int len = pdf.pdf_xref_len();
            PdfObj obj;

            if (!Utils.INRANGE(xref, 1, len - 1) && xref != -1)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);
            if (xref >= 0)
                obj = pdf.pdf_new_indirect(xref, 0);
            else
                obj = pdf.pdf_trailer();
            byte[] r = null;
            if (obj.pdf_is_stream() != 0)
            {
                FzBuffer res = pdf.pdf_load_stream_number(xref);
                r = Utils.BinFromBuffer(res);
            }
            return r;
        }

        /// <summary>
        /// Get xref stream without decompression.
        /// </summary>
        /// <param name="xref"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public byte[] GetXrefStreamRaw(int xref)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = AsPdfDocument(this);
            int len = pdf.pdf_xref_len();
            PdfObj obj;

            if (!Utils.INRANGE(xref, 1, len - 1) && xref != -1)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);
            if (xref >= 0)
                obj = pdf.pdf_new_indirect(xref, 0);
            else
                obj = pdf.pdf_trailer();
            byte[] r = null;
            if (obj.pdf_is_stream() != 0)
            {
                FzBuffer res = pdf.pdf_load_raw_stream_number(xref);
                r = Utils.BinFromBuffer(res);
            }
            return r;
        }

        /// <summary>
        /// Get xref of document XML metadata.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int XrefXmlMetaData()
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            if (root.m_internal == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_PDFROOT"]);
            PdfObj xml = root.pdf_dict_get(new PdfObj("Metadata"));
            int xref = 0;
            if (xml.m_internal != null)
                xref = xml.pdf_to_num();
            return xref;
        }
    }
}
