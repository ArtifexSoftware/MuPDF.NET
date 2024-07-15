using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using mupdf;

namespace MuPDF.NET
{
    public class Document
    {
        static Document()
        {
            Utils.InitApp();
        }

        /// <summary>
        /// False if document is still open. If closed, most other attributes and methods will have been deleted / disabled.
        /// </summary>
        public bool IsClosed { get; set; }

        /// <summary>
        /// True if this is a PDF document and contains unsaved changes, else False.
        /// </summary>
        public bool IsEncrypted { get; set; }

        internal int GraftID { get; set; }

        public Dictionary<string, string> MetaData { get; set; }

        public List<FontInfo> FontInfos { get; set; }

        public Dictionary<int, GraftMap> GraftMaps { get; set; } =
            new Dictionary<int, GraftMap>();

        public Dictionary<(int, int), int> ShownPages { get; set; } =
            new Dictionary<(int, int), int>();

        public Dictionary<string, int> InsertedImages { get; set; } = new Dictionary<string, int>();

        public Dictionary<int, Page> PageRefs { get; set; }

        /// <summary>
        /// Contains the filename or filetype value with which Document was created.
        /// </summary>
        public string Name { get; set; }

        public List<byte> Stream { get; set; }

        private bool _isPDF;

        private FzDocument _nativeDocument;

        /// <summary>
        /// Indicates whether the document is password-protected against access.
        /// <br/>
        /// This indicator remains unchanged – even after the document has been authenticated. Precludes incremental saves if true.
        /// </summary>
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

        /// <summary>
        /// True if this is a PDF document, else False.
        /// </summary>
        public Outline Outline { get; set; }

        /// <summary>
        /// True if this is a PDF document, else False.
        /// </summary>
        public bool IsPDF
        {
            get
            {
                if (mupdf.mupdf.ll_pdf_specifics(_nativeDocument.m_internal) != null)
                    return true;
                else
                    return false;
            }
            set { _isPDF = value; }
        }

        public bool ThisOwn { get; set; }

        /// <summary>
        /// An integer counting the number of versions present in the document. Zero if not a PDF, otherwise the number of incremental saves plus one.
        /// </summary>
        public int VersionCount
        {
            get
            {
                PdfDocument pdf = Document.AsPdfDocument(this);
                if (pdf.m_internal != null)
                    pdf.pdf_count_versions();
                return 0;
            }
        }

        /// <summary>
        /// Number of pages.
        /// </summary>
        public int PageCount
        {
            get { return GetPageCount(); }
        }

        public bool IsDirty
        {
            get
            {
                PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
                if (pdf.m_internal == null)
                    return false;
                int r = pdf.pdf_has_unsaved_changes();
                return r != 0;
            }
        }

        /// <summary>
        /// Contains the number of chapters in the document. Always at least 1.
        /// <br/>
        /// Relevant only for document types with chapter support (EPUB currently). Other documents will return 1.
        /// </summary>
        public int ChapterCount
        {
            get
            {
                if (IsClosed)
                    throw new Exception("document closed");
                return _nativeDocument.fz_count_chapters();
            }
        }

        /// <summary>
        /// True if PDF is in linearized format. False for non-PDF documents.
        /// </summary>
        public bool IsFastWebaccess
        {
            get
            {
                PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
                if (pdf != null)
                    return pdf.pdf_doc_was_linearized() != 0;
                return false;
            }
        }

        /// <summary>
        /// False if this is not a PDF or has no form fields, otherwise the number of root form fields (fields with no ancestors).
        /// </summary>
        public int IsFormPDF // return -1 or fields count
        {
            get
            {
                PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
                if (pdf.m_internal == null)
                    return -1;
                int count = -1;
                try
                {
                    PdfObj fields = Utils.pdf_dict_getl(
                        pdf.pdf_trailer(),
                        new string[] { "Root", "AcroForm", "Fields" }
                    );
                    if (fields.pdf_is_array() != 0)
                        count = fields.pdf_array_len();
                }
                catch (Exception)
                {
                    return -1;
                }

                if (count >= 0)
                    return count;
                return -1;
            }
        }

        /// <summary>
        /// True if document has a variable page layout (like e-books or HTML).
        /// </summary>
        public bool IsReflowable
        {
            get
            {
                if (IsClosed)
                    throw new Exception("document is closed");
                return _nativeDocument.fz_is_document_reflowable() != 0;
            }
        }

        /// <summary>
        /// True if PDF has been repaired during open (because of major structure issues). Always False for non-PDF documents.
        /// </summary>
        public bool IsRepaired
        {
            get
            {
                PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
                if (pdf.m_internal == null)
                    return false;
                return pdf.pdf_was_repaired() != 0;
            }
        }

        public string Language
        {
            get
            {
                PdfDocument pdf = AsPdfDocument(_nativeDocument);
                if (pdf.m_internal == null)
                    return null;
                fz_text_language lang = mupdf.mupdf.pdf_document_language(pdf);
                if (lang == fz_text_language.FZ_LANG_UNSET)
                    return null;

                return mupdf.mupdf.fz_string_from_text_language2(lang);
            }
        }

        /// <summary>
        /// Contains (chapter, pno) of the document’s last page.
        /// <br/>
        /// Relevant only for document types with chapter support (EPUB currently). Other documents will return (0, page_count - 1) and (0, -1) if it has no pages.
        /// </summary>
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

        /// <summary>
        /// A string containing the /PageLayout value. If not specified, the default “SinglePage” is returned. If not a PDF, None is returned.
        /// </summary>
        public string PageLayout
        {
            get
            {
                int xref = GetPdfCatalog();
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

        /// <summary>
        /// A string containing the /PageMode value. If not specified, the default “UseNone” is returned. If not a PDF, None is returned.
        /// </summary>
        public string PageMode
        {
            get
            {
                int xref = GetPdfCatalog();
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

        /// <summary>
        /// A dictionary indicating the /MarkInfo value. If not specified, the empty dictionary is returned. If not a PDF, None is returned.
        /// </summary>
        public Dictionary<string, bool> MarkInfo
        {
            get
            {
                int xref = GetPdfCatalog();
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
                    { "Suspects", false }
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

        /// <summary>
        /// Get list of field font resource names.
        /// </summary>
        public List<string> FormFonts
        {
            get
            {
                PdfDocument pdf = Document.AsPdfDocument(this);
                if (pdf.m_internal == null)
                    return null;
                PdfObj fonts = Utils.pdf_dict_getl(
                    pdf.pdf_trailer(),
                    new string[] { "Root", "AcroForm", "DR", "Font" });
                List<string> ret = new List<string>();
                if (fonts.m_internal != null && fonts.pdf_is_dict() != 0)
                {
                    int n = fonts.pdf_dict_len();
                    for (int i = 0; i < n; i ++)
                    {
                        PdfObj f = fonts.pdf_dict_get_key(i);
                        ret.Add(Utils.UnicodeFromStr(f.pdf_to_name()));
                    }
                }
                return ret;
            }
        }

        /// <summary>
        /// Document permissions.
        /// </summary>
        public uint Permissions
        {
            get
            {
                if (IsEncrypted)
                    return 0;
                FzDocument doc = _nativeDocument;
                PdfDocument pdf = doc.pdf_document_from_fz_document();

                if (pdf.m_internal != null)
                    return (uint)pdf.pdf_document_permissions();

                uint perm = 0xFFFFFFFC;
                if (doc.fz_has_permission(fz_permission.FZ_PERMISSION_PRINT) == 0)
                    perm = perm ^ (uint)mupdf.mupdf.PDF_PERM_PRINT;
                if (doc.fz_has_permission(fz_permission.FZ_PERMISSION_EDIT) == 0)
                    perm = perm ^ (uint)mupdf.mupdf.PDF_PERM_MODIFY;
                if (doc.fz_has_permission(fz_permission.FZ_PERMISSION_COPY) == 0)
                    perm = perm ^ (uint)mupdf.mupdf.PDF_PERM_COPY;
                if (doc.fz_has_permission(fz_permission.FZ_PERMISSION_ANNOTATE) == 0)
                    perm = perm ^ (uint)mupdf.mupdf.PDF_PERM_ANNOTATE;
                return perm;
            }
        }

        public Document(PdfDocument doc)
        {
            _nativeDocument = doc.super();
            IsPDF = true;
        }

        public Document(
            string fileName = null,
            byte[] stream = null,
            string fileType = null,
            Rect rect = null,
            float width = 0,
            float height = 0,
            int fontSize = 11
        )
        {
            try
            {
                IsClosed = false;
                IsEncrypted = false;
                MetaData = null;
                FontInfos = new List<FontInfo>();
                PageRefs = new Dictionary<int, Page>();

                if (stream != null)
                    Stream = new List<byte>(stream);
                else
                    Stream = null;

                bool fromFile;
                if (!string.IsNullOrEmpty(fileName) && stream == null)
                {
                    fromFile = true;
                    Name = fileName;
                }
                else
                {
                    fromFile = false;
                    Name = "";
                }

                string msg;
                if (fromFile)
                {
                    if (!File.Exists(fileName))
                    {
                        msg = $"No such file: {fileName}";
                        throw new FileNotFoundException(msg);
                    }
                    /*_nativeDocument = mupdf.mupdf.fz_open_document(filename);*/
                }

                if (
                    fromFile
                    && Stream != null
                    && (new System.IO.FileInfo(fileName).Length == 0 || Stream.Count == 0)
                )
                {
                    msg = $"cannot open empty document";
                    throw new Exception(msg);
                }

                float w = width;
                float h = height;
                FzRect r = (rect == null) ? new FzRect(FzRect.Fixed.Fixed_INFINITE) : rect.ToFzRect();
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
                    if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileType))
                        fileName = "pdf";
                    
                    string magic = fileName;
                    if (magic == null)
                        magic = fileType;

                    doc = mupdf.mupdf.fz_open_document_with_stream(magic, data);
                }
                else
                {
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        if (string.IsNullOrEmpty(fileType))
                        {

                            try
                            {
                                doc = mupdf.mupdf.fz_open_document(fileName);
                            }
                            catch(Exception)
                            {
                                throw new Exception("Failed to open document");
                            }
                        }
                        else
                        {
                            fz_document_handler handler = mupdf.mupdf.ll_fz_recognize_document(
                                fileType
                            );
                            if (handler != null)
                            {
                                if (handler.open != null)
                                {
                                    try
                                    {
/*                                        if (
                                            Utils.MUPDF_VERSION.Item1 == 1
                                            && Utils.MUPDF_VERSION.Item2 >= 24
                                        )*/
                                        {
                                            FzStream _stream = new FzStream(fileName);
                                            FzStream accel = new FzStream();
                                            FzArchive archive = new FzArchive();
                                            doc = new FzDocument(
                                                mupdf.mupdf.ll_fz_document_handler_open(handler, _stream.m_internal, accel.m_internal, archive.m_internal)       
                                            );
                                        }
                                        /*else
                                        {
                                            doc = new FzDocument(mupdf.mupdf.ll_fz_document_open_fn_call(handler.open, filename));
                                        }*/
                                    }
                                    catch (Exception)
                                    {
                                        throw new Exception(
                                            Utils.ErrorMessages["MSG_BAD_DOCUMENT"]
                                        );
                                    }
                                }
                                else if (
                                    mupdf.mupdf.FZ_VERSION_MAJOR >= 1
                                    && mupdf.mupdf.FZ_VERSION_MINOR >= 24
                                )
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

                ThisOwn = true;

                if (ThisOwn)
                {
                    GraftID = Utils.GenID();
                    if (NeedsPass)
                    {
                        IsEncrypted = true;
                    }
                    else
                        InitDocument();

                    string filename_ = fileName;
                    if (
                        (fileName != null && filename_.ToLower().EndsWith("svg"))
                        || (fileType != null && fileType.ToLower().Contains("svg"))
                    )
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
            finally { }
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
                fp = 0;
            if (fp > srcCount - 1)
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
            opts.do_incremental = 0;
            opts.do_ascii = 0;
            opts.do_decompress = 0;
            opts.do_linear = 0;
            opts.do_clean = 1;
            opts.do_pretty = 0;

            FzBuffer res = mupdf.mupdf.fz_new_buffer(8192);
            FzOutput output = new FzOutput(res);
            pdfout.pdf_write_document(output, opts);
            output.fz_close_output();

            byte[] ret = Utils.BinFromBuffer(res);
            int len1 = Utils.MUPDF_WARNINGS_STORE.Count;

            for (i = len0; i < len1; i++)
            {
                Console.WriteLine($"{Utils.MUPDF_WARNINGS_STORE[i]}");
            }
            return ret;
        }

        /// <summary>
        /// Number of pages.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int GetPageCount()
        {
            if (IsClosed)
                throw new Exception("document closed");
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

        public static PdfDocument AsPdfDocument(FzDocument document, bool required = true)
        {
            PdfDocument ret = new PdfDocument(document);
            if (required)
                if (ret.m_internal == null)
                    throw new Exception("document is Null");
            return ret;
        }

        public static PdfDocument AsPdfDocument(Document document)
        {
            if (document.IsClosed)
                throw new Exception("document closed");
            if (document == null)
                throw new Exception("document is Null");
            return document._nativeDocument.pdf_document_from_fz_document();
        }

        public void InitDocument()
        {
            if (IsEncrypted)
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
            int pageCount = PageCount;
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
                if (ol.m_internal == null)
                    return null;
            }
            catch (Exception) { }
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

            if (testkey.m_internal == null)
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
            if (testkey.pdf_is_string() == 0)
                throw new Exception(string.Format("cannot insert value for '{0}'", key));

            string temp = mupdf.mupdf.pdf_to_text_string(testkey);
            if (temp != eyecatcher)
                throw new Exception(string.Format("cannot insert value for '{0}'", key));

            FzBuffer res = Object2Buffer(obj, 1, 0);
            string objStr = Utils.EscapeStrFromBuffer(res);

            string nullVal = string.Format("{0}({1})", skey, eyecatcher);
            string newVal = string.Format("{0} {1}", skey, value);
            string newStr = objStr.Replace(nullVal, newVal);

            PdfObj newObj = Utils.PdfObjFromStr(pdf, newStr);
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

        public void SetKeyXRef(int xref, string key, string value)
        {
            if (IsClosed)
                throw new Exception("Document closed");

            HashSet<char> INVALID_NAME_CHARS = new HashSet<char>(
                new char[] { ' ', '(', ')', '<', '>', '[', ']', '{', '}', '/', '%', '\0' }
            );
            var invalidChars = new HashSet<char>(INVALID_NAME_CHARS);
            var intersection = invalidChars.Intersect(key.ToArray());

            if (string.IsNullOrEmpty(key) || intersection.Count() != 0)
            {
                if (intersection.Count() == 1 && intersection.First() != '/')
                    throw new Exception("Bad Key");
            }
            if (
                !(value is string)
                || string.IsNullOrEmpty(value)
                || (value[0] == '/' && INVALID_NAME_CHARS.Intersect(value.Substring(1)).Any())
            )
            {
                throw new Exception("Bad Value");
            }

            PdfDocument pdf = AsPdfDocument(this);
            int xrefLen = pdf.pdf_xref_len();
            PdfObj obj = null;
            if (!Utils.INRANGE(xref, 1, xrefLen - 1) && xref != -1)
            {
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);
            }

            if (xref != -1)
                obj = pdf.pdf_load_object(xref);
            else
                obj = pdf.pdf_trailer();
            PdfObj nObj = SetObjectValue(obj, key, value);
            if (nObj.m_internal == null)
                return;

            if (xref != -1)
                pdf.pdf_update_object(xref, nObj);
            else
            {
                int n = nObj.pdf_dict_len();
                for (int i = 0; i < n; i++)
                    obj.pdf_dict_put(nObj.pdf_dict_get_key(i), nObj.pdf_dict_get_val(i));
            }
        }

        public (string, string) GetKeyXref(int xref, string key)
        {
            if (IsClosed)
                throw new Exception("document closed");

            PdfDocument pdf = AsPdfDocument(this);
            int xrefLen = pdf.pdf_xref_len();
            if (!Utils.INRANGE(xref, 1, xrefLen - 1) && xref != -1)
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
                text = $"{subObj.pdf_to_int()}";
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
            string userPW = null,
            int preserveMetadata = 1,
            int useObjstms = 0,
            int compressionEffort = 0
        )
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document is closed or encrypted");

            if (PageCount < 1)
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
            opts.do_preserve_metadata = preserveMetadata;
            opts.do_use_objstms = useObjstms;
            opts.compression_effort = compressionEffort;

            FzOutput output = null;
            pdf.m_internal.resynth_required = 0;
            Utils.EmbeddedClean(pdf);

            if (filename is string)
            {
                pdf.pdf_save_document(filename, opts);
            }
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
            Page page = NewPage(pno, width, height);
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

        private void ResetPageRefs()
        {
            if (IsClosed)
                return;
            if (PageRefs != null)
                PageRefs.Clear();
        }

        public Page this[int i]
        {
            get
            {
                return LoadPage(i);
            }
        }

        /// <summary>
        /// PDF only: Insert an empty page.
        /// </summary>
        /// <param name="pno">page number in front of which the new page should be inserted. Must be in 1 < pno <= page_count. Special values -1 and doc.page_count insert after the last page.</param>
        /// <param name="width">page width.</param>
        /// <param name="height">page height.</param>
        /// <returns>the created page object.</returns>
        /// <exception cref="Exception"></exception>
        public Page NewPage(int pno = -1, float width = 595, float height = 842)
        {
            if (IsClosed || IsEncrypted)
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

        public List<Entry> GetPageFonts(int pno, bool full = false)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (!IsPDF)
                return null;
            List<Entry> val = GetPageInfo(pno, 1);
            List<Entry> ret = new List<Entry>();
            if (full == false)
            {
                foreach (Entry v in val)
                {
                    v.StreamXref = 0;
                    ret.Add(v);
                }
            }
            return ret;
        }

        /// <summary>
        /// List fonts, images, XObjects used on a page.
        /// </summary>
        /// <param name="pno"></param>
        /// <param name="what"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private List<Entry> GetPageInfo(int pno, int what)
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

            List<Entry> liste = new List<Entry>();
            List<dynamic> tracer = new List<dynamic>();

            if (rsrc.m_internal != null)
                Utils.ScanResources(pdf, rsrc, liste, what, 0, tracer);
            return liste;
        }

        /// <summary>
        /// Create a Page object for further processing (like rendering, text searching, etc.).
        /// </summary>
        /// <param name="pageId">Either a 0-based page number, or a tuple (chapter, pno). For an integer, any -∞ < page_id < page_count is acceptable.</param>
        /// <returns>page object</returns>
        /// <exception cref="Exception"></exception>
        public Page LoadPage(int pageId)
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

            if (Utils.INRANGE(pageId, 0, PageCount - 1) == false)
                throw new Exception("document page count is not enough");

            FzPage page = _nativeDocument.fz_load_page(pageId);
            Page val = new Page(page, this);

            val.ThisOwn = true;
            val.Parent = this;
            PageRefs[val.GetHashCode()] = val;
            val.AnnotRefs = new Dictionary<int, dynamic>();
            val.Number = pageId;

            return val;
        }

        /// <summary>
        /// Create a Page object for further processing (like rendering, text searching, etc.).
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="pagenum"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Page LoadPage(int chapter, int pagenum)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");

            FzPage page = _nativeDocument.fz_load_chapter_page(chapter, pagenum);
            Page val = new Page(page, this);

            val.ThisOwn = true;
            val.Parent = this;
            PageRefs[val.GetHashCode()] = val;
            val.AnnotRefs = new Dictionary<int, dynamic>();
            val.Number = 0;

            return val;
        }

        /// <summary>
        /// PDF only: Provide a new copy of a page after finishing and updating all pending changes.
        /// </summary>
        /// <param name="page">page object.</param>
        /// <returns>a new copy of the same page. All pending updates (e.g. to annotations or widgets) will be finalized and a fresh copy of the page will be loaded.</returns>
        public Page ReloadPage(Page page)
        {
            Dictionary<int, dynamic> oldAnnots = new Dictionary<int, dynamic>();
            int pno = page.Number;
            foreach ((int k, dynamic v) in page.AnnotRefs)
                oldAnnots.Add(k, v);

            int old_ref = page.GetPdfPage().super().m_internal.refs;
            long m_internal_old = page.GetPdfPage().super().m_internal_value();

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

        /// <summary>
        /// PDF Only: Return an embedded font file’s data and appropriate file extension.
        /// <br/>
        /// This can be used to store the font as an external file. The method does not throw exceptions (other than via checking for PDF and valid xref).
        /// </summary>
        /// <param name="xref">PDF object number of the font to extract.</param>
        /// <param name="infoOnly">only return font information, not the buffer. To be used for information-only purposes, avoids allocation of large buffer areas.</param>
        /// <param name="named"> If true, a dictionary with the following keys is returned: ‘name’ (font base name), ‘ext’ (font file extension), ‘type’ (font type), ‘content’ (font file content).</param>
        /// <returns>Font object, where ext is a 3-byte suggested file extension (str), basename is the font’s name (str), type is the font’s type (e.g. “Type1”) and content is a bytes object containing the font file’s content (or b””).</returns>
        public FontInfo ExtractFont(int xref = 0, int infoOnly = 0, string named = null)
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj obj = pdf.pdf_load_object(xref);
            PdfObj type = obj.pdf_dict_get(new PdfObj("Type"));
            PdfObj subType = obj.pdf_dict_get(new PdfObj("Subtype"));

            if (
                type.pdf_name_eq(new PdfObj("Font")) != 0
                && !subType.pdf_to_name().StartsWith("CIDFontType")
            ) // matched
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

                return new FontInfo()
                {
                    Name = Utils.EscapeStrFromStr(bName.pdf_to_name()),
                    Ext = Utils.UnicodeFromStr(ext),
                    Type = Utils.UnicodeFromStr(subType.pdf_to_name()),
                    Content = bytes
                };
            }
            else
            {
                return new FontInfo()
                {
                    Name = "",
                    Ext = "",
                    Type = "",
                    Content = Encoding.UTF8.GetBytes("")
                };
            }
        }

        public List<(int, double)> _GetCharWidths(
            int xref,
            string bfName,
            string ext,
            int ordering,
            int limit,
            int idx = 0
        )
        {
            PdfDocument pdf = AsPdfDocument(this);
            int myLimit = limit;
            FzFont font = null;

            if (myLimit < 256)
                myLimit = 256;
            if (ordering >= 0)
            {
                ll_fz_lookup_cjk_font_outparams cjk = new ll_fz_lookup_cjk_font_outparams();
                SWIGTYPE_p_unsigned_char data = mupdf.mupdf.ll_fz_lookup_cjk_font_outparams_fn(
                    ordering,
                    cjk
                );

                font = mupdf.mupdf.fz_new_font_from_memory(null, data, cjk.len, cjk.index, 0);
            }
            else
            {
                ll_fz_lookup_base14_font_outparams base14 =
                    new ll_fz_lookup_base14_font_outparams();
                SWIGTYPE_p_unsigned_char data = mupdf.mupdf.ll_fz_lookup_base14_font_outparams_fn(
                    bfName,
                    base14
                );
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

        /// <summary>
        /// PDF only: Return the definition source of a PDF object.
        /// </summary>
        /// <param name="xref">the object’s xref.</param>
        /// <param name="compressed">whether to generate a compact output with no line breaks or spaces.</param>
        /// <param name="ascii">whether to ASCII-encode binary data.</param>
        /// <returns>whether to ASCII-encode binary data.</returns>
        public string GetXrefObject(int xref, int compressed = 0, int ascii = 0)
        {
            if (IsClosed)
                throw new Exception("document closed");
            PdfDocument pdf = AsPdfDocument(this);
            int xrefLen = pdf.pdf_xref_len();
            PdfObj obj = null;

            if (!Utils.INRANGE(xref, 1, xrefLen - 1) && xref != -1)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);
            if (xref > 0)
                obj = pdf.pdf_load_object(xref);
            else
                obj = pdf.pdf_trailer();

            FzBuffer res = Utils.Object2Buffer(obj.pdf_resolve_indirect(), compressed, ascii);
            string text = Utils.EscapeStrFromBuffer(res);

            return text;
        }

        /// <summary>
        /// PDF only: Return a list of all XObjects referenced by a page.
        /// </summary>
        /// <param name="pno">page number, 0-based, -∞ < pno < page_count.</param>
        /// <returns>a list of (non-image) XObjects. These objects typically represent pages embedded (not copied) from other PDFs.</returns>
        /// <exception cref="Exception"></exception>
        public List<Entry> GetPageXObjects(int pno)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (!IsPDF)
                return new List<Entry>();
            List<Entry> val = GetPageInfo(pno, 3);
            return val;
        }

        /// <summary>
        /// Retrieve a list of images used on a page.
        /// </summary>
        /// <param name="pno"> page number, 0-based, -∞ < pno < page_count.</param>
        /// <param name="full"> whether to also include the referencer’s xref (which is zero if this is the page).</param>
        /// <returns>a list of images referenced by this page.</returns>
        /// <exception cref="Exception"></exception>
        public List<Entry> GetPageImages(int pno, bool full = false)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (!IsPDF)
                return new List<Entry>();
            List<Entry> val = GetPageInfo(pno, 2);
            if (full == false)
            {
                List<Entry> ret = new List<Entry>();
                foreach (Entry v in val)
                {
                    v.StreamXref = 0;
                    ret.Add(v);
                }
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
        public List<Toc> GetToc(bool simple = true)
        {
            List<Toc> Recurse(Outline olItem, List<Toc> list, int lvl)
            {
                while (olItem != null && olItem.ToFzOutline().m_internal != null)
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
                                page = resolve.Item1[0] + 1;
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
                        LinkInfo link = Utils.GetLinkDict(olItem, this);
                        list.Add(
                            new Toc()
                            {
                                Level = lvl,
                                Title = title,
                                Page = page,
                                Link = link
                            }
                        );
                    }
                    else
                        list.Add(
                            new Toc()
                            {
                                Level = lvl,
                                Title = title,
                                Page = page
                            }
                        );

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
                return new List<Toc>();

            int lvl = 1;
            List<Toc> liste = new List<Toc>();
            List<Toc> toc = Recurse(olItem, liste, lvl);

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
        internal (List<int>, float, float) ResolveLink(string uri = null, int chapters = 0)
        {
            fz_location loc = null;
            float xp = 0.0f;
            float yp = 0.0f;

            if (string.IsNullOrEmpty(uri))
            {
                if (chapters != 0)
                    return (new List<int>() { -1, -1 }, 0, 0);
                return (new List<int>() { -1 }, 0, 0);
            }
            try
            {
                ll_fz_resolve_link_outparams outparams = new ll_fz_resolve_link_outparams();
                loc = mupdf.mupdf.ll_fz_resolve_link_outparams_fn(
                    _nativeDocument.m_internal,
                    uri,
                    outparams
                );
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
        /// <param name="obj">PdfObj</param>
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
        private DestName GetArray(PdfObj val, Dictionary<int, int> page_refs)
        {
            DestName template = new DestName() { Page = -1, Dest = "" };

            string array = "";
            if (val.pdf_is_indirect() != 0)
                val = val.pdf_resolve_indirect();
            if (val.pdf_is_array() != 0)
                array = ObjString(val);
            else if (val.pdf_is_dict() != 0)
                array = ObjString(val.pdf_dict_gets("D"));
            else
                return template;

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

        private void FillDict(
            Dictionary<string, DestName> destDict,
            PdfObj pdfDict,
            Dictionary<int, int> page_refs
        )
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
        /// PDF only: Convert destination names into a Python dict.
        /// </summary>
        /// <returns>PDF only: Convert destination names into a Python dict.</returns>
        public Dictionary<string, dynamic> ResolveNames()
        {
            Dictionary<int, int> page_refs = new Dictionary<int, int>();
            for (int i = 0; i < PageCount; i++)
                page_refs.Add(GetPageXref(i), i);

            PdfDocument pdf = Document.AsPdfDocument(this);

            // access PDF catalog
            PdfObj catalog = pdf.pdf_trailer().pdf_dict_gets("Root");
            Dictionary<string, DestName> destDict = new Dictionary<string, DestName>();
            PdfObj dests = mupdf.mupdf.pdf_new_name("Dests");

            PdfObj oldDests = catalog.pdf_dict_get(dests);
            if (oldDests.pdf_is_dict() != 0)
                FillDict(destDict, oldDests, page_refs);

            PdfObj tree = pdf.pdf_load_name_tree(dests);
            if (tree.pdf_is_dict() != 0)
                FillDict(destDict, tree, page_refs);

            Dictionary<string, dynamic> ret = new Dictionary<string, dynamic>();
            foreach ((string k, DestName v) in destDict)
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
            if (pdf.m_internal == null)
                return -1;
            PdfObj sigflags = Utils.pdf_dict_getl(
                pdf.pdf_trailer(),
                new string[] { "Root", "AcroForm", "SigFlags" }
            );
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
            if (xml.m_internal != null)
            {
                FzBuffer buff = xml.pdf_load_stream();
                rc = Utils.UnicodeFromBuffer(buff);
            }

            return rc;
        }

        /// <summary>
        /// PDF only: Add an arbitrary supported document to the current PDF. Opens “infile” as a document, converts it to a PDF and then invokes Document.insert_pdf(). Parameters are the same as for that method. Among other things, this features an easy way to append images as full pages to an output PDF.
        /// </summary>
        /// <param name="infile"> the input document to insert. May be a filename specification as is valid for creating a Document or a Pixmap.</param>
        /// <param name="fromPage"></param>
        /// <param name="toPage"></param>
        /// <param name="startAt"></param>
        /// <param name="rotate"></param>
        /// <param name="links"></param>
        /// <param name="annots"></param>
        /// <param name="showProgress"></param>
        /// <param name="final"></param>
        /// <exception cref="Exception"></exception>
        public void InsertFile(
            Document infile,
            int fromPage = -1,
            int toPage = -1,
            int startAt = -1,
            int rotate = -1,
            bool links = true,
            bool annots = true,
            int showProgress = 0,
            int final = 1
        )
        {
            Document src = infile;
            if (src == null)
                throw new Exception("bad infile parameter");
            if (!src.IsPDF)
            {
                byte[] pdfBytes = src.Convert2Pdf();
                src = new Document("pdf", pdfBytes);
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
        public void InsertPdf(
            Document docSrc,
            int fromPage = -1,
            int toPage = -1,
            int startAt = -1,
            int rotate = -1,
            bool links = true,
            bool annots = true,
            int showProgress = 0,
            int final = 1,
            GraftMap gmap = null
        )
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (GraftID == docSrc.GraftID)
                throw new Exception("source and target cannot be same object");
            int sa = startAt;
            if (sa < 0)
                sa = GetPageCount();
            if (docSrc.PageCount > showProgress && showProgress > 0)
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
            if (gmap == null)
            {
                gmap = new GraftMap(this);
                GraftMaps[isrt] = gmap;
            }

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
            Utils.MergeRange(
                new Document(pdfout),
                new Document(pdfsrc),
                fp,
                tp,
                sa,
                rotate,
                links,
                annots,
                showProgress,
                gmap
            );

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
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
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
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
            pdf.pdf_enable_journal();
        }

        /// <summary>
        /// Move forward in the journal.
        /// </summary>
        /// <returns>true if success</returns>
        /// <exception cref="Exception"></exception>
        public bool JournalRedo()
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            PdfDocument pdf = Document.AsPdfDocument(this);
            pdf.pdf_redo();
            return true;
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
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
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
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);

            IntPtr utf8Ptr = Utils.Utf16_Utf8Ptr(filename);
            try
            {
                pdf.pdf_load_journal(filename);
            }
            catch (Exception)
            {
                Marshal.FreeHGlobal(utf8Ptr);
            }
            
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
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
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
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
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
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
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
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
            IntPtr utf8Ptr = Utils.Utf16_Utf8Ptr(filename);
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
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);

            MemoryStream memoryStream = new MemoryStream(journal);

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
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
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
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
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
        public void SetLayout(
            Rect rect = null,
            float width = 0,
            float height = 0,
            int fontSize = 11
        )
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
            ulong mark = mupdf.mupdf.ll_fz_make_bookmark2(
                _nativeDocument.m_internal,
                loc.internal_()
            );
            return mark;
        }

        /// <summary>
        /// Get xref of PDF catalog.
        /// </summary>
        /// <returns></returns>
        public int GetPdfCatalog()
        {
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument, false);
            int xref = 0;
            if (pdf.m_internal == null)
                return xref;
            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            xref = root.pdf_to_num();

            return xref;
        }

        /// <summary>
        /// PDF only: Return the trailer source of the PDF, which is usually located at the PDF file’s end.
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
                while (parent.m_internal != null)
                {
                    int count = parent.pdf_dict_get_int(new PdfObj("Count"));
                    parent.pdf_dict_put_int(new PdfObj("Count"), count + 1);
                    parent = parent.pdf_dict_get(new PdfObj("Parent"));
                }
                if (!copy)
                {
                    kids1.pdf_array_delete(i1);
                    parent = parent1;
                    while (parent.m_internal != null)
                    {
                        int count = parent.pdf_dict_get_int(new PdfObj("Count"));
                        parent.pdf_dict_put_int(new PdfObj("Count"), count - 1);
                        parent = parent.pdf_dict_get(new PdfObj("Parent"));
                    }
                }
            }
            else
            {
                if (copy)
                {
                    parent = parent2;
                    while (parent.m_internal != null)
                    {
                        int count = parent.pdf_dict_get_int(new PdfObj("Count"));
                        parent.pdf_dict_put_int(new PdfObj("Count"), count + 1);
                        parent = parent.pdf_dict_get(new PdfObj("Parent"));
                    }
                }
                else
                {
                    if (i1 < pos)
                        kids1.pdf_array_delete(i1);
                    else
                        kids1.pdf_array_delete(i1 + 1);
                }
            }
            if (pdf.m_internal.rev_page_map != null)
                mupdf.mupdf.ll_pdf_drop_page_tree(pdf.m_internal);
            ResetPageRefs();
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
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
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
        /// <param name="pageId">the current page id. This must be a tuple (chapter, pno) identifying an existing page.</param>
        /// <returns>The tuple of the following page, i.e. either (chapter, pno + 1) or (chapter + 1, 0), or the empty tuple () if the argument was the last page. Relevant only for document types with chapter support (EPUB currently).</returns>
        /// <exception cref="Exception"></exception>
        public (int, int) NextLocation(int pageId)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            (int, int) _pageId;
            _pageId = (0, pageId);

            if (!Contains(_pageId))
                throw new Exception("page id not in document");

            if (_pageId.Item1 == LastLocation.Item1 && _pageId.Item2 == LastLocation.Item2)
                return (-1, -1);
            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
            int val = _pageId.Item1;
            int chapter = val;
            val = _pageId.Item2;
            int pno = val;
            FzLocation loc = mupdf.mupdf.fz_make_location(chapter, pno);
            FzLocation nextLoc = mupdf.mupdf.fz_next_page(_nativeDocument, loc);
            return (nextLoc.chapter, nextLoc.page);
        }

        /// <summary>
        /// Get (chapter, page) of next page.
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        public (int, int) NextLocation((int, int) pageId)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (pageId.Item1 == LastLocation.Item1 && pageId.Item2 == LastLocation.Item2)
                return (-1, -1);
            if (!Contains(pageId))
                throw new Exception("page id not in document");

            PdfDocument pdf = Document.AsPdfDocument(_nativeDocument);
            int val = pageId.Item1;
            int chapter = val;
            val = pageId.Item2;
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
        public List<AnnotXref> PageAnnotXrefs(int n)
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
            if (!Contains(_pageId))
                throw new Exception("page id not in document");

            (int chapter, int pno) = _pageId;
            FzLocation loc = mupdf.mupdf.fz_make_location(chapter, pno);
            pageN = _nativeDocument.fz_page_number_from_location(loc);
            return pageN;
        }

        /// <summary>
        /// Convert (chapter, pno) to page number.
        /// </summary>
        /// <param name="pageId">page id</param>
        /// <returns>chapter and pno</returns>
        public int GetPageNumberFromLocation(int chapter, int pno)
        {
            int pageN = GetPageCount();
            while (pno < 0)
                pno += pageN;
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
        public List<Page> GetPages(int start, int stop, int step)
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

            List<Page> ret = new List<Page>();
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
        private void AddFormFont(string name, string font)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");

            PdfDocument pdf = AsPdfDocument(this);
            if (pdf.m_internal == null)
                return;

            PdfObj fonts = Utils.pdf_dict_getl(
                pdf.pdf_trailer(),
                new string[] { "Root", "AcroFrom", "DR", "Font" }
            );

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
        public void ExtendTocItems(List<Toc> items)
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
                Toc item = items[i];
                LinkInfo link;
                if (item.Link != null)
                    link = item.Link;
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
                item.Link = link;
                items[i] = item;
            }
        }

        /// <summary>
        /// Remove a page from document page dict.
        /// </summary>
        /// <param name="page"></param>
        public void ForgetPage(Page page)
        {
            int pid = page.GetHashCode();
            if (PageRefs.ContainsKey(pid))
            {
                PageRefs.Remove(pid);
            }
        }

        internal List<(int, string)> _getPageLabels()
        {
            PdfDocument pdf = AsPdfDocument(this);
            List<(int, string)> rc = new List<(int, string)>();

            PdfObj obj = Utils.pdf_dict_getl(
                pdf.pdf_trailer(),
                new string[] { "Root", "PageLabels" }
            );
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
                nums = kids.pdf_array_get(i)
                    .pdf_dict_get(new PdfObj("Nums"))
                    .pdf_resolve_indirect();
                Utils.GetPageLabels(rc, nums);
            }
            return rc;
        }

        /// <summary>
        /// Return page label definitions in PDF document.
        /// </summary>
        /// <returns>A list of dictionaries with the following format</returns>
        public List<Label> GetPageLabels()
        {
            List<Label> ret = new List<Label>();
            foreach ((int, string) item in _getPageLabels())
            {
                Label d = Utils.RuleDict(item);
                ret.Add(d);
            }
            return ret;
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

        /// <summary>
        /// PDF only: Add or update the page label definitions of the PDF.
        /// </summary>
        /// <param name="labels">a list of dictionaries. Each dictionary defines a label building rule and a 0-based “start” page number.</param>
        public void SetPageLabels(List<Label> labels)
        {
            string CreateLabelStr(Label label)
            {
                string s = $"{label.StartPage}<<";
                if (!string.IsNullOrEmpty(label.Prefix))
                    s += $"/P({label.Prefix})";
                if (!string.IsNullOrEmpty(label.Style))
                    s += $"/S/{label.Style}";
                if (label.FirstPageNum > 1)
                    s += $"/St {label.FirstPageNum}";
                s += ">>";
                return s;
            }

            string CreateNums(List<Label> labels)
            {
                labels.Sort((a, b) =>
                {
                    return a.StartPage - b.StartPage;
                });
                string s = string.Join("", labels.Select(label => CreateLabelStr(label)).ToArray());
                return s;
            }

            PdfDocument pdf = AsPdfDocument(this);
            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));

            root.pdf_dict_del(mupdf.mupdf.pdf_new_name("PageLabels"));
            Utils.pdf_dict_putl(
                root,
                mupdf.mupdf.pdf_new_array(pdf, 0),
                new string[] { "PageLabels", "Nums" }
            );
            int xref = GetPdfCatalog();
            string text = GetXrefObject(xref, compressed: 1);
            text = text.Replace("/Nums[]", $"/Nums[{CreateNums(labels)}]");
            UpdateObject(xref, text);
        }

        /// <summary>
        /// Replace xref stream part.
        /// </summary>
        /// <param name="xref">xref number</param>
        /// <param name="stream">the new content of the stream.</param>
        /// <param name="_new">deprecated</param>
        /// <param name="compress">whether to compress the inserted stream.</param>
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
        /// <param name="xref">xref number.</param>
        /// <param name="text">a string containing a valid PDF object definition.</param>
        /// <param name="page"> a page object. If provided, indicates, that annotations of this page should be refreshed (reloaded) to reflect changes incurred with links and / or annotations.</param>
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

        /// <summary>
        /// PDF only: Copy a page reference within the document.
        /// </summary>
        /// <param name="pno">the page to be copied. Must be in range 0 <= pno < page_count.</param>
        /// <param name="to">the page number in front of which to copy. The default inserts after the last page.</param>
        /// <exception cref="Exception"></exception>
        public void CopyPage(int pno, int to = -1)
        {
            if (IsClosed)
                throw new Exception("document closed");
            int pageCount = PageCount;
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

            int pageCount = PageCount;
            while (pno < 0)
                pno += pageCount;

            if (pno >= pageCount)
                throw new Exception("bad page number(s)");

            List<Toc> toc = GetToc();
            List<int> olXrefs = GetOutlineXrefs();

            for (int i = 0; i < (toc != null ? toc.Count : 0); i++)
            {
                if (toc[i].Page == pno + 1)
                    RemoveTocItem(olXrefs[i]);
            }

            RemoveLinksTo(new List<int>() { pno });
            _DeletePage(pno);
            ResetPageRefs();
        }

        /// <summary>
        /// PDF only: Delete multiple pages given as 0-based numbers.
        /// </summary>
        /// <param name="from">start page number</param>
        /// <param name="to">end page number</param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void DeletePages(int from = -1, int to = -1)
        {
            if (!IsPDF)
                throw new Exception("is no PDF");
            if (IsClosed)
                throw new Exception("document is closed");
            int pageCount = PageCount;
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
            List<Toc> toc = GetToc();
            List<int> olXrefs = GetOutlineXrefs();
            for (int i = 0; i < olXrefs.Count; i++)
            {
                if (numbers.Contains(toc[i].Page - 1))
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

        /// <summary>
        /// PDF only: Delete multiple pages given as 0-based numbers.
        /// </summary>
        /// <param name="numbers">page list</param>
        /// <exception cref="ArgumentException"></exception>
        public void DeletePages(List<int> numbers)
        {
            if (numbers.Count == 0)
            {
                Console.WriteLine("noting to delete");
                return;
            }

            numbers.Sort();
            if (numbers[0] < 0 || numbers[numbers.Count - 1] >= PageCount)
                throw new ArgumentException("bad page number(s)");
            List<Toc> toc = GetToc();
            List<int> olXrefs = GetOutlineXrefs();
            for (int i = 0; i < olXrefs.Count; i++)
            {
                if (numbers.Contains(toc[i].Page - 1))
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
            if (numbers[0] < 0 || numbers[numbers.Count - 1] >= PageCount)
                throw new ArgumentException("bad page number(s)");
            List<Toc> toc = GetToc();
            List<int> olXrefs = GetOutlineXrefs();
            for (int i = 0; i < olXrefs.Count; i++)
            {
                if (numbers.Contains(toc[i].Page - 1))
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

        /// <summary>
        /// PDF only: Return the xref of the outline item. This is mainly used for internal purposes.
        /// </summary>
        /// <returns>xref numbers</returns>
        public List<int> GetOutlineXrefs()
        {
            List<int> xrefs = new List<int>();
            PdfDocument pdf = AsPdfDocument(this);
            if (pdf.m_internal == null)
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

        /// <summary>
        /// PDF only: Embed a new file. All string parameters except the name may be unicode (in previous versions, only ASCII worked correctly). File contents will be compressed (where beneficial).
        /// </summary>
        /// <param name="name">entry identifier, must not already exist.</param>
        /// <param name="buffer">file contents.</param>
        /// <param name="filename">optional filename. Documentation only, will be set to name if None.</param>
        /// <param name="ufilename">optional unicode filename. Documentation only, will be set to filename if None.</param>
        /// <param name="desc">optional description. Documentation only, will be set to name if None.</param>
        /// <returns>The method now returns the xref of the inserted file.</returns>
        /// <exception cref="Exception"></exception>
        public int AddEmbfile(
            string name,
            byte[] buffer,
            string filename = null,
            string ufilename = null,
            string desc = null
        )
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
            SetKeyXRef(xref, "Params/CreationDate", Utils.GetPdfString(date));
            SetKeyXRef(xref, "Params/ModDate", Utils.GetPdfString(date));

            return xref;
        }

        private int _AddEmbfile(
            string name,
            byte[] buffer,
            string filename = null,
            string ufilename = null,
            string desc = null
        )
        {
            PdfDocument pdf = AsPdfDocument(this);
            FzBuffer data = Utils.BufferFromBytes(buffer);
            if (data.m_internal == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_BUFFER"]);

            PdfObj names = Utils.pdf_dict_getl(
                pdf.pdf_trailer(),
                new string[] { "Root", "Names", "EmbeddedFiles", "Names" }
            );
            if (names.pdf_is_array() == 0)
            {
                PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
                names = pdf.pdf_new_array(6);
                Utils.pdf_dict_putl(
                    root,
                    names,
                    new string[] { "Names", "EmbeddedFiles", "Names" }
                );
            }

            PdfObj fileEntry = Utils.EmbedFile(pdf, data, filename, ufilename, desc, 1);
            int xref = Utils.pdf_dict_getl(fileEntry, new string[] { "EF", "F" }).pdf_to_num();
            names.pdf_array_push(mupdf.mupdf.pdf_new_text_string(name));
            names.pdf_array_push(fileEntry);

            return xref;
        }

        /// <summary>
        /// PDF only: Retrieve the content of embedded file by its entry number or name. If the document is not a PDF, or entry cannot be found, an exception is raised.
        /// </summary>
        /// <returns>index or name of entry.</returns>
        public List<string> GetEmbfileNames()
        {
            List<string> names = new List<string>();
            _EmbfileNames(names);
            return names;
        }

        private void _EmbfileNames(List<string> filenames)
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj names = Utils.pdf_dict_getl(
                pdf.pdf_trailer(),
                new string[] { "Root", "Names", "EmbeddedFiles", "Names" }
            );
            if (names.pdf_is_array() != 0)
            {
                int n = names.pdf_array_len();
                for (int i = 0; i < n; i += 2)
                {
                    string val = Utils.EscapeStrFromStr(
                        names.pdf_array_get(i).pdf_to_text_string()
                    );
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

        private void _DeleteEmbfile(int idx)
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj names = Utils.pdf_dict_getl(
                pdf.pdf_trailer(),
                new string[] { "Root", "Names", "EmbeddedFiles", "Names" }
            );
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
            else
                throw new Exception(msg);
            return idx;
        }

        private byte[] _GetEmbeddedFile(int idx)
        {
            PdfDocument pdf = AsPdfDocument(this);
            PdfObj names = Utils.pdf_dict_getl(
                pdf.pdf_trailer(),
                new string[] { "Root", "Names", "EmbeddedFiles", "Names" }
            );
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

        public EmbfileInfo GetEmbfileInfo(dynamic item)
        {
            int index = EmbeddedfileIndex(item);
            EmbfileInfo infoDict = new EmbfileInfo() { Name = GetEmbfileNames()[index] };
            PdfDocument pdf = AsPdfDocument(this);
            int xref = 0;
            int ciXref = 0;

            PdfObj trailer = pdf.pdf_trailer();
            PdfObj names = Utils.pdf_dict_getl(
                trailer,
                new string[] { "Root", "Names", "EmbeddedFiles", "Names" }
            );
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
        public int GetEmbfileUpd(
            dynamic item,
            byte[] buffer = null,
            string filename = null,
            string ufilename = null,
            string desc = null
        )
        {
            int idx = EmbeddedfileIndex(item);
            PdfDocument pdf = AsPdfDocument(this);
            int xref = 0;
            PdfObj names = Utils.pdf_dict_getl(
                pdf.pdf_trailer(),
                new string[] { "Root", "Names", "EmbeddedFiles", "Names" }
            );
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
            SetKeyXRef(xref, "Params/ModDate", Utils.GetPdfString(date));
            return xref;
        }

        /// <summary>
        /// Get image by xref. Returns a dictionary.
        /// </summary>
        /// <param name="xref"></param>
        /// <returns></returns>
        public ImageInfo ExtractImage(int xref)
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
            if (subtype.pdf_name_eq(new PdfObj("Image")) == 0) // mismatch
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
                fz_compressed_buffer llCbuf = mupdf.mupdf.ll_fz_compressed_image_buffer(
                    img.m_internal
                );
                if (
                    llCbuf != null
                    && !(
                        llCbuf.params_.type == (int)ImageType.FZ_IMAGE_RAW
                        || llCbuf.params_.type == (int)ImageType.FZ_IMAGE_FAX
                        || llCbuf.params_.type == (int)ImageType.FZ_IMAGE_FLATE
                        || llCbuf.params_.type == (int)ImageType.FZ_IMAGE_LZW
                        || llCbuf.params_.type == (int)ImageType.FZ_IMAGE_RLD
                    )
                )
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
                    res = img.fz_new_buffer_from_image_as_png(
                        new FzColorParams(defaultColorParams)
                    );
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

            ImageInfo ret = new ImageInfo()
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
        public Location FindBookmark(int bm)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            FzLocation location = _nativeDocument.fz_lookup_bookmark(bm);
            return new Location() { Chapter = location.chapter, Page = location.page };
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
                        PdfObj o = oldAnnots.pdf_array_get(i);
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
                        newAnnots.pdf_array_push(copyObj);
                    }
                    page2.pdf_dict_put(new PdfObj("Annots"), newAnnots);
                }
                FzBuffer res = Utils.ReadContents(page1);

                if (res.m_internal != null)
                {
                    FzBuffer buf = Utils.fz_new_buffer_from_data(Encoding.UTF8.GetBytes(" "));
                    PdfObj contents = pdf.pdf_add_stream(buf, new PdfObj(), 0);
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
            PdfObj ocp = Utils.pdf_dict_getl(
                pdf.pdf_trailer(),
                new string[] { "Root", "OCProperties" }
            );
            PdfObj obj,
                o;

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
                PdfObj obj = Utils.pdf_dict_getl(
                    pdf.pdf_trailer(),
                    new string[] { "Root", "OCProperties", "Configs" }
                );
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
        public int GetNewXref()
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
            PdfObj ocgs = Utils.pdf_dict_getl(
                pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root")),
                new string[] { "OCProperties", "OCGs" }
            );

            Dictionary<int, OCGroup> ret = new Dictionary<int, OCGroup>();
            if (ocgs.pdf_is_array() == 0)
                return ret;
            int n = ocgs.pdf_array_len();
            for (int i = 0; i < n; i++)
            {
                PdfObj ocg = ocgs.pdf_array_get(i);
                int xref = ocg.pdf_to_num();
                string name = ocg.pdf_dict_get(new PdfObj("Name")).pdf_to_text_string();
                PdfObj obj = Utils.pdf_dict_getl(
                    ocg,
                    new string[] { "Usage", "CreatorInfo", " Subtype" }
                );
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
            if (list.Count == 0 || list.Min() < 0 || list.Max() > PageCount)
                throw new Exception("bad page number(s)");

            PdfDocument pdf = AsPdfDocument(this);

            IntPtr pNumbers = Marshal.AllocHGlobal(list.Count * sizeof(int));
            Marshal.Copy(list.ToArray(), 0, pNumbers, list.Count);
            SWIGTYPE_p_int swigNumbers = new SWIGTYPE_p_int(pNumbers, true);

            pdf.pdf_rearrange_pages(list.Count, swigNumbers);
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

        public void SetLayer(
            int config,
            string baseState = null,
            int[] on = null,
            int[] off = null,
            List<int[]> rbgroups = null,
            int[] locked = null
        )
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
            PdfObj ocp = Utils.pdf_dict_getl(
                pdf.pdf_trailer(),
                new string[] { "Root", "OCProperties" }
            );
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
                List<int> select = LayerUIConfigs()
                    .Where(ui => ui.Text == number)
                    .Select(ui => ui.Number)
                    .ToList();
                if (select.Count == 0)
                {
                    throw new Exception($"bad OCG '{number}'");
                }
                num = select[0];
            }
            else if (number is int)
                num = number;
            else
                num = -1;

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
            int xref = GetPdfCatalog();
            if (xref == 0)
                throw new Exception("not a pdf");
            if (markInfo == null)
                return false;
            Dictionary<string, bool> valid = new Dictionary<string, bool>()
            {
                { "Marked", false },
                { "UserProperties", false },
                { "Suspects", false }
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
            string[] valid =
            {
                "SinglePage",
                "OneColumn",
                "TwoColumnLeft",
                "TwoColumnRight",
                "TwoPageLeft",
                "TwoPageRight"
            };
            int xref = GetPdfCatalog();
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
            string[] valid =
            {
                "UseNone",
                "UseOutlines",
                "UseThumbs",
                "FullScreen",
                "UseOC",
                "UseAttachments"
            };
            int xref = GetPdfCatalog();
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
            FzBuffer res = Utils.fz_new_buffer_from_data(utf8);
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
            PdfObj cfgs = Utils.pdf_dict_getl(
                pdf.pdf_trailer(),
                new string[] { "Root", "OCProperties", "Configs" }
            );
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

        public byte[] Write(
            bool garbage = false,
            bool clean = false,
            bool deflate = false,
            bool deflateImages = false,
            bool deflateFonts = false,
            bool incremental = false,
            bool ascii = false,
            bool expand = false,
            bool linear = false,
            bool noNewId = false,
            bool appearance = false,
            bool pretty = false,
            int encryption = 1,
            int permissions = 4095,
            string ownerPW = null,
            string userPW = null,
            bool preserveMetadata = true,
            bool useObjstms = false,
            bool compressionEffort = false
        )
        {
            MemoryStream byteStream = new MemoryStream();
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
                userPW: userPW,
                preserveMetadata: preserveMetadata ? 1 : 0,
                useObjstms: useObjstms ? 1 : 0,
                compressionEffort: compressionEffort ? 1 : 0
            );
            return byteStream.ToArray();
        }

        public List<string> GetKeysXref(int xref)
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
            if (pdf.m_internal == null)
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
        public int GetXrefLength()
        {
            PdfDocument pdf = AsPdfDocument(this);
            if (pdf != null)
                return pdf.pdf_xref_len();
            return 0;
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

        /// <summary>
        /// Check if xref is an image object
        /// </summary>
        /// <param name="xref"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool XrefIsImage(int xref)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (GetKeyXref(xref, "Subtype").Item2 == "/Image")
                return true;
            return false;
        }

        /// <summary>
        /// Copy a PDF dictionary object to another one given their xref numbers.
        /// </summary>
        /// <param name="newXref"></param>
        /// <param name="xref"></param>
        public void CopyXref(int newXref, int xref, List<string> keep = null)
        {
            if (XrefIsStream(newXref))
            {
                byte[] stream = GetXrefStreamRaw(newXref);
                UpdateStream(xref, stream, 0);
            }

            if (keep == null)
                keep = new List<string>();
            foreach (string key in GetKeysXref(xref))
            {
                if (keep.Contains(key))
                    continue;
                SetKeyXRef(xref, key, "null");
            }
            foreach (string key in GetKeysXref(newXref))
            {
                (string, string) item = GetKeyXref(newXref, key);
                SetKeyXRef(xref, key, item.Item2);
            }
        }

        /// <summary>
        /// Search for a string on a page.
        /// </summary>
        /// <param name="pno">page number</param>
        /// <param name="text">string to be searched for</param>
        /// <param name="quads">return quads instead of rectangles</param>
        /// <param name="clip">restrict search to this rectangle</param>
        /// <param name="flags">bit switches, default: join hyphened words</param>
        /// <param name="textpage">reuse a prepared textpage</param>
        /// <returns>a list of rectangles or quads, each containing an occurrence.</returns>
        public List<Quad> SearchPageFor(
            int pno,
            string text,
            bool quads = false,
            Rect clip = null,
            int flags =
                (int)(
                    TextFlags.TEXT_DEHYPHENATE
                    | TextFlags.TEXT_PRESERVE_WHITESPACE
                    | TextFlags.TEXT_PRESERVE_LIGATURES
                    | TextFlags.TEXT_MEDIABOX_CLIP
                ), // 83
            TextPage textpage = null
        )
        {
            return this[pno].SearchFor(text, clip, quads, flags, textpage);
        }

        private void DoLinks(
            Document doc,
            int fromPage = -1,
            int toPage = -1,
            int startAt = -1
        )
        {
            Utils.DoLinks(this, doc, fromPage, toPage, startAt);
        }

        /// <summary>
        /// Delete TOC / bookmark item by index.
        /// </summary>
        /// <param name="idx"></param>
        public void DeleteTocItem(int idx)
        {
            int xref = GetOutlineXrefs()[idx];
            RemoveTocItem(xref);
        }

        /// <summary>
        /// Return the cross reference number of an OCG or OCMD attached to an image or form xobject.
        /// </summary>
        /// <param name="xref">the xref of an image or form xobject. Valid such cross reference numbers are returned by Document.get_page_images()</param>
        /// <returns></returns>
        public int GetOC(int xref)
        {
            return Utils.GetOC(this, xref);
        }

        /// <summary>
        /// Retrieve the definition of an OCMD.
        /// </summary>
        /// <param name="xref">the xref of the OCMD.</param>
        /// <returns></returns>
        public OCMD GetOCMD(int xref)
        {
            return Utils.GetOCMD(this, xref);
        }

        /// <summary>
        /// Return a list of page numbers with the given label
        /// </summary>
        /// <param name="label">label</param>
        /// <param name="onlyOne">(bool) stop searching after first hit</param>
        /// <returns></returns>
        public List<int> GetPageNumbers(string label, bool onlyOne = false)
        {
            return Utils.GetPageNumbers(this, label, onlyOne);
        }

        /// <summary>
        /// Create pixmap of document page by page number.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="pno">page number</param>
        /// <param name="matrix">Matrix for transformation </param>
        /// <param name="dpi"></param>
        /// <param name="colorSpace">rgb, rgb, gray - case ignored, default csRGB</param>
        /// <param name="clip">restrict rendering to this area</param>
        /// <param name="alpha">include alpha channel</param>
        /// <param name="annots">also render annotations</param>
        /// <returns></returns>
        public Pixmap GetPagePixmap(
            int pno,
            IdentityMatrix matrix,
            int dpi = 0,
            string colorSpace = null,
            Rect clip = null,
            bool alpha = false,
            bool annots = true
        )
        {
            return Utils.GetPagePixmap(this, pno, matrix, dpi, colorSpace, clip, alpha, annots);
        }

        /// <summary>
        /// Extract a document page's text by page number
        /// </summary>
        /// <param name="pno">page number</param>
        /// <param name="option">text, words, blocks, html, dict, json, rawdict, xhtml or xml.</param>
        /// <param name="clip"></param>
        /// <param name="flags"></param>
        /// <param name="textPage"></param>
        /// <param name="sort"></param>
        /// <returns>output from TextPage</returns>
        public dynamic GetPageText(
            int pno,
            string option = "text",
            Rect clip = null,
            int flags = 0,
            TextPage textPage = null,
            bool sort = false
        )
        {
            return this[pno].GetText(option, clip, flags, textPage, sort);
        }

        /// <summary>
        /// Check whether there are annotations on any page.
        /// </summary>
        /// <returns>True / False. As opposed to fields, which are also stored in a central place of a PDF document, the existence of links / annotations can only be detected by parsing each page.</returns>
        /// <exception cref="Exception"></exception>
        public bool HasAnnots()
        {
            if (IsClosed)
                throw new Exception("document closed");
            if (!IsPDF)
                throw new Exception("is no pdf");
            for (int i = 0; i < PageCount; i++)
            {
                foreach (AnnotXref item in PageAnnotXrefs(i))
                {
                    if (
                        item.AnnotType == PdfAnnotType.PDF_ANNOT_LINK
                        || item.AnnotType == PdfAnnotType.PDF_ANNOT_WIDGET
                    )
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// PDF only: Check whether there are links, resp. annotations anywhere in the document.
        /// </summary>
        /// <returns>True / False. As opposed to fields, which are also stored in a central place of a PDF document, the existence of links / annotations can only be detected by parsing each page.</returns>
        /// <exception cref="Exception"></exception>
        public bool HasLinks()
        {
            if (IsClosed)
                throw new Exception("document closed");
            if (!IsPDF)
                throw new Exception("is no pdf");
            for (int i = 0; i < PageCount; i++)
            {
                foreach (AnnotXref item in PageAnnotXrefs(i))
                {
                    if (item.AnnotType == PdfAnnotType.PDF_ANNOT_LINK)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// PDF only: Remove potentially sensitive data from the PDF. This function is inspired by the similar “Sanitize” function in Adobe Acrobat products. The process is configurable by a number of options.
        /// </summary>
        /// <param name="attachedFiles">Search for ‘FileAttachment’ annotations and remove the file content.</param>
        /// <param name="cleanPages">Remove any comments from page painting sources. If this option is set to False, then this is also done for hidden_text and redactions.</param>
        /// <param name="embeddedFiles">Remove embedded files.</param>
        /// <param name="hiddenText">Remove OCRed text and invisible text.</param>
        /// <param name="javascript">Remove JavaScript sources.</param>
        /// <param name="metadata">Remove PDF standard metadata.</param>
        /// <param name="redactions">Apply redaction annotations.</param>
        /// <param name="redactImages">how to handle images if applying redactions. One of 0 (ignore), 1 (blank out overlaps) or 2 (remove).</param>
        /// <param name="removeLinks">how to handle images if applying redactions. One of 0 (ignore), 1 (blank out overlaps) or 2 (remove).</param>
        /// <param name="resetFields">Reset all form fields to their defaults.</param>
        /// <param name="resetResponses">Remove all responses from all annotations.</param>
        /// <param name="thumbnails">Remove all responses from all annotations.</param>
        /// <param name="xmlMetadata">Remove all responses from all annotations.</param>
        /// <exception cref="Exception"></exception>
        public void Scrub(
            bool attachedFiles = true,
            bool cleanPages = true,
            bool embeddedFiles = true,
            bool hiddenText = true,
            bool javascript = true,
            bool metadata = true,
            bool redactions = true,
            int redactImages = 0,
            bool removeLinks = true,
            bool resetFields = true,
            bool resetResponses = true,
            bool thumbnails = true,
            bool xmlMetadata = true
        )
        {
            List<string> RemoveHidden(string[] contLines)
            {
                List<string> outlines = new List<string>();
                bool inText = false;
                bool suppress = false;
                bool makeReturn = false;
                foreach (string line in contLines)
                {
                    if (line == "BT")
                    {
                        inText = true;
                        outlines.Add(line);
                        continue;
                    }
                    if (line == "ET")
                    {
                        inText = false;
                        outlines.Add(line);
                        continue;
                    }
                    if (line == "3 Tr")
                    {
                        suppress = true;
                        makeReturn = true;
                        continue;
                    }
                    if (line.Substring(line.Length - 2, 2) == "Tr" && line[0] == '3')
                    {
                        suppress = false;
                        outlines.Add(line);
                        continue;
                    }
                    if (suppress && inText)
                        continue;
                    outlines.Add(line);
                }
                if (makeReturn)
                    return outlines;
                else
                    return null;
            }

            if (!IsPDF)
                throw new Exception("is no PDF");
            if (IsEncrypted || IsClosed)
                throw new Exception("closed or encrypted doc");
            if (cleanPages == false)
            {
                hiddenText = false;
                redactions = false;
            }
            if (metadata)
                SetMetadata(new Dictionary<string, string>()); // empty metadata

            for (int i = 0; i < PageCount; i++)
            {
                Page page = this[i];
                if (resetFields)
                {
                    foreach (Widget widget in page.GetWidgets())
                        widget.Reset();
                }
                if (removeLinks)
                {
                    List<LinkInfo> links = page.GetLinks();
                    foreach (LinkInfo link in links)
                        page.DeleteLink(link);
                }
                bool foundRedacts = false;
                foreach (Annot annot in page.GetAnnots())
                {
                    if (annot.Type.Item1 == PdfAnnotType.PDF_ANNOT_FILE_ATTACHMENT && attachedFiles)
                        annot.UpdateFile(buffer: new byte[] { 32 });
                    if (resetResponses)
                        annot.DeleteResponses();
                    if (annot.Type.Item1 == PdfAnnotType.PDF_ANNOT_REDACT)
                        foundRedacts = true;
                }

                if (redactions && foundRedacts)
                    page.ApplyRedactions(redactImages);
                if (!(cleanPages || hiddenText))
                    continue;
                page.CleanContetns();
                if (page.GetContents().Count == 0)
                    continue;
                if (hiddenText)
                {
                    int xref = page.GetContents()[0];
                    byte[] cont = GetXrefStream(xref);
                    List<string> contLines = RemoveHidden(
                        Encoding.UTF8.GetString(cont).Split("\n")
                    );
                    if (contLines.Count != 0)
                    {
                        cont = Encoding.UTF8.GetBytes(string.Join("\n", contLines.ToArray()));
                        UpdateStream(xref, cont);
                    }
                }
                if (thumbnails)
                {
                    if (GetKeyXref(page.Xref, "Thumb").Item1 != "null")
                        SetKeyXRef(page.Xref, "Thumb", "null");
                }
            }
            if (embeddedFiles)
            {
                foreach (string name in GetEmbfileNames())
                    DeleteEmbfile(name);
            }

            if (xmlMetadata)
                DeleteXmlMetadata();

            int xrefLimit = 0;
            if (xmlMetadata || javascript)
                xrefLimit = GetXrefLength();

            for (int xref = 1; xref <= xrefLimit; xref++)
            {
                if (string.IsNullOrEmpty(GetXrefObject(xref)))
                    throw new Exception($"bad xref {xref} - clean PDF before scrubbing");
                if (javascript && GetKeyXref(xref, "S").Item2 == "/JavaScript")
                {
                    string obj = "<</S/JavaScript/JS()>>";
                    UpdateObject(xref, obj);
                    continue;
                }
                if (!xmlMetadata)
                    continue;

                if (GetKeyXref(xref, "Type").Item2 == "/Metadata")
                {
                    UpdateObject(xref, "<<>>");
                    UpdateStream(xref, Encoding.UTF8.GetBytes("deleted")); // new is 1 as default
                    continue;
                }

                if (GetKeyXref(xref, "Metadata").Item1 != "null")
                    SetKeyXRef(xref, "Metadata", null);
            }
        }

        /// <summary>
        /// PDF only: Sets or updates the metadata of the document as specified in m, a Python dictionary.
        /// </summary>
        /// <param name="metadata">A dictionary with the same keys as metadata (see below). All keys are optional. A PDF’s format and encryption method cannot be set or changed and will be ignored. If any value should not contain data, do not specify its key or set the value to None. If you use {} all metadata information will be cleared to the string “none”. If you want to selectively change only some values, modify a copy of doc.metadata and use it as the argument. Arbitrary unicode values are possible if specified as UTF-8-encoded.</param>
        /// <exception cref="Exception"></exception>
        public void SetMetadata(Dictionary<string, string> metadata = null)
        {
            if (!IsPDF)
                throw new Exception("is no PDF");
            if (IsEncrypted || IsClosed)
                throw new Exception("closed or encrypted doc");
            if (metadata == null)
                metadata = new Dictionary<string, string>();

            Dictionary<string, string> keymap = new Dictionary<string, string>()
            {
                { "author", "Author" },
                { "producer", "Producer" },
                { "creator", "Creator" },
                { "title", "Title" },
                { "format", null },
                { "encryption", null },
                { "creationDate", "CreationDate" },
                { "modDate", "ModDate" },
                { "subject", "Subject" },
                { "keywords", "Keywords" },
                { "trapped", "Trapped" }
            };
            HashSet<string> keys = new HashSet<string>(keymap.Keys);
            List<string> diffKeys = (new HashSet<string>(metadata.Keys)).Except(keys).ToList();
            if (diffKeys.Count != 0)
                throw new Exception($"bad dict key(s) - {string.Join(", ", diffKeys.ToArray())}");

            (string t, string temp) = GetKeyXref(-1, "Info");
            int infoXref = 0;
            if (t != "xref")
                infoXref = 0;
            else
                infoXref = Convert.ToInt32(temp.Replace("0 R", ""));

            if (metadata.Count == 0 && infoXref == 0)
                return;
            if (infoXref == 0)
            {
                infoXref = GetNewXref();
                UpdateObject(infoXref, "<<>>");
                SetKeyXRef(-1, "Info", $"{infoXref} 0 R");
            }

            else if (metadata.Count == 0)
            {
                SetKeyXRef(-1, "Info", "null");
                return;
            }

            foreach (string k in metadata.Keys)
            {
                if (keymap.GetValueOrDefault(k, null) != null)
                {
                    string pdfKey = keymap[k];
                    string val = metadata[k];
                    if (string.IsNullOrEmpty(val) || (val == "none" || val == "null"))
                        val = "null";
                    else
                        val = Utils.GetPdfString(val);                                                                                                                                                    
                    SetKeyXRef(infoXref, pdfKey, val);
                }
            }
            InitDocument();
        }

        /// <summary>
        /// Attach optional content object to image or form xobject.
        /// </summary>
        /// <param name="xref"xref number of an image or form xobject></param>
        /// <param name="oc">xref number of an OCG or OCMD</param>
        /// <exception cref="Exception"></exception>
        public void SetOC(int xref, int oc)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document close or encrypted");
            (string t, string name) = GetKeyXref(xref, "Subtype");
            if (t != "name" || !(name == "/Image" || name == " /Form"))
                throw new Exception($"bad object type at xref {xref}");
            if (oc > 0)
                (t, name) = GetKeyXref(oc, "Type");
            if (t != "name" || !(name == "/OCG" || name == "/OCMD"))
                throw new Exception($"bad object type at xref {oc}");
            if (oc == 0)
            {
                SetKeyXRef(xref, "OC", "null");
                return;
            }
            SetKeyXRef(xref, "OC", $"{oc} 0 R");
        }

        /// <summary>
        /// Create or update an OCMD object in a PDF document.
        /// </summary>
        /// <param name="xref">0 for creating a new object, otherwise update existing one.</param>
        /// <param name="ocgs">OCG xref numbers, which shall be subject to 'policy'.</param>
        /// <param name="policy">one of 'AllOn', 'AllOff', 'AnyOn', 'AnyOff' (any casing).</param>
        /// <param name="ve">visibility expression. Use instead of 'ocgs' with 'policy'.</param>
        /// <returns>Xref of the created or updated OCMD.</returns>
        public int SetOCMD(
            OCMD ocmd = null,
            int xref = 0,
            int[] ocgs = null,
            string policy = null,
            dynamic[] ve = null
        )
        {
            List<int> allOcgs = GetOcgs().Keys.ToList();

            if (ocmd != null)
            {
                if (xref == 0)
                    xref = ocmd.Xref;
                if (ocgs == null)
                    ocgs = ocmd.Ocgs;
                if (string.IsNullOrEmpty(policy))
                    policy = ocmd.Policy;
                if (ve == null)
                    ve = ocmd.Ve;
            }

            string VeMaker(dynamic[] v)
            {
                if (v.Length < 2)
                    throw new Exception($"bad ve length: {v.Length}");
                if (
                    !(v[0] is string)
                    || !(new List<string>() { "and", "or", "not" }).Contains(v[0].ToLower())
                )
                    throw new Exception($"bad operand: {v[0]}");
                if (v[0].ToLower() == "not" && v.Length != 2)
                    throw new Exception($"operand is not, but ve length: {v.Length}");

                string item = $"[/{v[0]}";
                item = char.ToUpper(item[0]) + item.Substring(1).ToLower();
                foreach (var x in v.Skip(1).ToArray())
                {
                    if (x is int)
                    {
                        if (!allOcgs.Contains(x))
                            throw new Exception($"bad OCG {x}");
                        item += $" {x} 0 R";
                    }
                    else
                    {
                        item += $" {VeMaker(x)}";
                    }
                }
                item += "]";
                return item;
            }

            string text = "<</Type/OCMD";

            if (ocgs != null)
            {
                List<int> s = ocgs.Except(allOcgs).ToList();
                if (s.Count != 0)
                    throw new Exception($"bad OCGs count: {s.Count}");
                text += "/OCGs[" + string.Join(" ", ocgs.Select(x => $"{x} 0 R")) + "]";
            }

            if (!string.IsNullOrEmpty(policy))
            {
                policy = policy.ToLower();
                Dictionary<string, string> pols = new Dictionary<string, string>()
                {
                    { "anyon", "AnyOn" },
                    { "allon", "AllOn" },
                    { "anyoff", "AnyOff" },
                    { "alloff", "AnyOff" },
                };

                if (!pols.Keys.Contains(policy))
                    throw new Exception($"bad policy: {policy}");
                text += $"/P/{pols[policy]}";
            }

            if (ve != null)
            {
                text += $"/VE{VeMaker(ve)}";
            }

            text += ">>";
            if (xref == 0)
                xref = GetNewXref();
            else if (!GetXrefObject(xref, 1).Contains("/Type/OCMD"))
                throw new Exception("bad xref or not an OCMD");
            UpdateObject(xref, text);

            return xref;
        }

        /// <summary>
        /// Create new outline tree (table of contents, TOC)
        /// </summary>
        /// <param name="tocs">each entry must contain level, title, page and optionally top margin on the page.None or '()' remove the TOC</param>
        /// <param name="collapse">collapses entries beyond this level. Zero or Null shows all entries unfolded.</param>
        /// <returns>the number of inserted items, or the number of removed items respectively.</returns>
        public int SetToc(List<Toc> tocs, int collapse = 1)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (!IsPDF)
                throw new Exception("is no pdf");
            if (tocs == null || tocs.Count == 0)
                return DeleteToc().Count;

            int n = tocs.Count;
            int pageCount = PageCount;
            Toc t0 = tocs[0];
            if (t0.Level != 1)
                throw new Exception("hierarchy level of item 0 must be 1");

            foreach (int i in Enumerable.Range(0, n - 1))
            {
                Toc t1 = tocs[i];
                Toc t2 = tocs[i + 1];
                if (!(-1 <= t1.Page && t1.Page <= pageCount))
                    throw new Exception($"row {i}: page number out of range");
                if (t2.Level < 1)
                    throw new Exception($"bad hierarchy level in row {i + 1}");
                if (t2.Level > t1.Level + 1)
                    throw new Exception($"bad hierarchy level in row {i + 1}");
            }
            List<int> oldXrefs = DeleteToc();
            oldXrefs = new List<int>();

            List<int> xref = new List<int>() { 0 }
                .Concat(oldXrefs)
                .ToList();
            xref[0] = GetOlRootNumber();
            if (n > oldXrefs.Count)
            {
                for (int i = 0; i < (n - oldXrefs.Count); i++)
                {
                    xref.Add(GetNewXref());
                }
            }

            List<Dictionary<string, dynamic>> olItems = new List<Dictionary<string, dynamic>>()
            {
                new Dictionary<string, dynamic>()
                {
                    { "count", 0 },
                    { "first", -1 },
                    { "last", -1 },
                    { "xref", xref[0] }
                }
            };

            Dictionary<int, int> lvlTab = new Dictionary<int, int>();
            lvlTab.Add(0, 0);

            for (int i = 0; i < n; i++)
            {
                Toc o = tocs[i];
                int lvl = o.Level;
                string title = Utils.GetPdfString(o.Title);
                int pno = Math.Min(PageCount - 1, Math.Max(0, o.Page - 1));
                int pageXref = GetPageXref(pno);
                float pageHeight = PageCropBox(pno).Height;
                Point top = new Point(72, pageHeight - 36);

                LinkInfo dest = new LinkInfo() { To = top, Kind = LinkType.LINK_GOTO };
                if (o.Page < 0)
                    dest.Kind = LinkType.LINK_NONE;
                if (o.Link != null)
                {
                    if (o.Link is LinkInfo)
                    {
                        dest = o.Link;
                        if (dest.To == null)
                            dest.To = top;
                        else
                        {
                            Page page = this[pno];
                            Point point = new Point(dest.To);
                            point.Y = page.CropBox.Height - point.Y;
                            point = point * page.RotationMatrix;
                            dest.To = new Point(point);
                        }
                    }
                    else if (o.Link is float)
                    {
                        dest.To = new Point(72, pageHeight - o.Link);
                    }
                }

                Dictionary<string, dynamic> d = new Dictionary<string, dynamic>();
                d.Add("first", -1);
                d.Add("count", 0);
                d.Add("last", -1);
                d.Add("prev", -1);
                d.Add("next", -1);
                d.Add("dest", Utils.GetDestString(pageXref, dest));
                d.Add("top", dest.To);
                d.Add("title", title);
                d.Add("parent", lvlTab[lvl - 1]);
                d.Add("xref", xref[i + 1]);
                d.Add("color", dest.Color);
                d.Add("flags", (dest.Italic ? 1 : 0) + 2 * (dest.Bold ? 1 : 0));
                lvlTab[lvl] = i + 1;
                Dictionary<string, dynamic> parent = olItems[lvlTab[lvl - 1]];

                if (dest.Collapse || (collapse != 0 && lvl > collapse))
                    parent["count"] -= 1;
                else
                    parent["count"] += 1;

                if (parent["first"] == -1)
                {
                    parent["first"] = i + 1;
                    parent["last"] = i + 1;
                }
                else
                {
                    d["prev"] = parent["last"];
                    Dictionary<string, dynamic> prev = olItems[parent["last"]];
                    prev["next"] = i + 1;
                    parent["last"] = i + 1;
                }
                olItems.Add(d);
            }

            int index = 0;
            foreach (Dictionary<string, dynamic> ol in olItems)
            {
                string txt = "<<";
                if (ol["count"] != 0)
                    txt += $"/Count {ol["count"]}";
                try
                {
                    txt += ol["dest"];
                }
                catch (Exception) { }

                try
                {
                    if (ol["first"] > -1)
                        txt += $"/First {xref[ol["first"]]} 0 R";
                }
                catch (Exception) {  }

                try
                {
                    if (ol["last"] > -1)
                        txt += $"/Last {xref[ol["last"]]} 0 R";
                }
                catch (Exception) { }

                try
                {
                    if (ol["next"] > -1)
                        txt += $"/Next {xref[ol["next"]]} 0 R";
                }
                catch (Exception) { }

                try
                {
                    if (ol["parent"] > -1)
                        txt += $"/Parent {xref[ol["parent"]]} 0 R";
                }
                catch (Exception) { }

                try
                {
                    if (ol["prev"] > -1)
                        txt += $"/Prev {xref[ol["prev"]]} 0 R";
                }
                catch (Exception) { }

                try
                {
                    txt += "/Title" + ol["title"];
                }
                catch (Exception) { }
                if (ol.GetValueOrDefault("count", 0) != 0 && ol.GetValueOrDefault("color", null) != null)
                    if (ol["color"].Length == 3)
                        txt += $"/C[ {ol["color"][0]} {ol["color"][1]} {ol["color"][2]}]";
                if (ol.GetValueOrDefault("flags", 0) > 0)
                    txt += $"/F {ol["flags"]}";
                if (index == 0)
                    txt += "/Type/Outlines";
                txt += ">>";
                UpdateObject(xref[index], txt);
                index++;
            }
            InitDocument();
            return n;
        }

        /// <summary>
        /// Delete the Toc
        /// </summary>
        /// <returns></returns>
        private List<int> DeleteToc()
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            List<int> xrefs = new List<int>();
            PdfDocument pdf = AsPdfDocument(this);
            if (pdf.m_internal == null)
                return xrefs;

            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            PdfObj olRoot = root.pdf_dict_get(new PdfObj("Outlines"));
            if (olRoot.m_internal == null)
                return xrefs;

            PdfObj first = olRoot.pdf_dict_get(new PdfObj("First"));
            xrefs = Utils.GetOutlineXrefs(first, xrefs);
            int xrefCount = xrefs.Count;

            int olRootXref = olRoot.pdf_to_num();
            pdf.pdf_delete_object(olRootXref);
            root.pdf_dict_del(new PdfObj("Outlines"));

            for (int i = 0; i < xrefCount; i++)
            {
                int xref = xrefs[i];
                pdf.pdf_delete_object(xref);
            }
            xrefs.Add(olRootXref);
            InitDocument();

            return xrefs;
        }

        /// <summary>
        /// Update TOC item by index.
        /// </summary>
        /// <param name="idx">desired index of the TOC list, as created by get_toc.</param>
        /// <param name="dest">destination dictionary as created by get_toc(False). Outrules all other parameters.If None, the remaining parameters are used to make a dest dictionary.</param>
        /// <param name="kind"></param>
        /// <param name="pno"></param>
        /// <param name="uri"></param>
        /// <param name="title"></param>
        /// <param name="to"></param>
        /// <param name="filename"></param>
        /// <param name="zoom"></param>
        public void SetTocItem(
            int idx,
            LinkInfo dest,
            int kind = 0,
            int pno = 0,
            string uri = null,
            string title = null,
            Point to = null,
            string filename = null,
            float zoom = 0
        )
        {
            int xref = GetOutlineXrefs()[idx];
            int pageXref = 0;
            if (dest.Kind == LinkType.LINK_GOTO)
            {
                pno = dest.Page;
                pageXref = GetPageXref(pno);
                float pageHight = PageCropBox(pno).Height;
                to = dest.To == null ? new Point(72, 36) : dest.To;
                to.Y = pageHight - to.Y;
                dest.To = to;
            }
            string action = Utils.GetDestString(pageXref, dest);
            if (!action.StartsWith("/A"))
                throw new Exception("bad bookmark dest");
            float[] color = dest.Color;
            if (color != null)
            {
                if (color.Length != 3 || color.Min() < 0 || color.Max() > 1)
                    throw new Exception("bad color value");
            }
            bool bold = dest.Bold;
            bool italic = dest.Italic;
            int flags = italic ? 1 : 0 + 2 * (bold ? 1 : 0);
            bool collapse = dest.Collapse;
            UpdateTocItem(xref, action, title, flags, collapse, color);
        }

        /// <summary>
        /// "update" bookmark by letting it point to nowhere
        /// </summary>
        /// <param name="xref"></param>
        /// <param name="action"></param>
        /// <param name="title"></param>
        /// <param name="flags"></param>
        /// <param name="collapse"></param>
        /// <param name="color"></param>
        public void UpdateTocItem(
            int xref,
            string action = null,
            string title = null,
            int flags = 0,
            bool collapse = false,
            float[] color = null
        )
        {
            PdfDocument pdf = Document.AsPdfDocument(this);
            PdfObj item = pdf.pdf_new_indirect(xref, 0);
            if (!string.IsNullOrEmpty(title))
                item.pdf_dict_put_text_string(new PdfObj("Title"), title);
            if (!string.IsNullOrEmpty(action))
            {
                item.pdf_dict_del(new PdfObj("Dest"));
                PdfObj obj = Utils.PdfObjFromStr(pdf, action);
                item.pdf_dict_put(new PdfObj("A"), obj);
            }
            item.pdf_dict_put_int(new PdfObj("F"), flags);
            int i;
            if (color != null && color.Length == 3)
            {
                PdfObj c = pdf.pdf_new_array(3);
                for (i = 0; i < 3; i++)
                {
                    c.pdf_array_push_real((long)color[i]);
                }
                item.pdf_dict_put(new PdfObj("C"), c);
            }
            else if (color != null)
                item.pdf_dict_del(new PdfObj("C"));
            if (item.pdf_dict_get(new PdfObj("Count")).m_internal != null)
            {
                i = item.pdf_dict_get_int(new PdfObj("Count"));
                if ((i < 0 && collapse == false) || (i > 0 && collapse == true))
                {
                    i = i * -1;
                    item.pdf_dict_put_int(new PdfObj("Count"), i);
                }
            }
        }

        /// <summary>
        /// Build font subsets of a PDF.
        /// </summary>
        /// <param name="verbose">write various progress information to sysout. This currently only has an effect if fallback is True.</param>
        public void SubsetFonts(bool verbose = false)
        {
            mupdf.mupdf.pdf_subset_fonts2(
                AsPdfDocument(this),
                new vectori(Enumerable.Range(0, PageCount))
            );
            return;
        }

        public bool Contains(int page)
        {
            if (page < PageCount)
                return true;
            return false;
        }

        public bool Contains((int, int) loc)
        {
            (int chapter, int pno) = loc;
            if (chapter < 0 || chapter >= ChapterCount)
                return false;
            if (pno < 0 || pno >= GetChapterPageCount(chapter))
                return false;
            return true;
        }

        public int GetChapterPageCount(int chapter)
        {
            if (IsClosed)
                throw new Exception("document closed");
            int chapters = _nativeDocument.fz_count_chapters();
            if (chapters < 0 || chapter >= chapters)
                throw new Exception("bad chapter number");
            return _nativeDocument.fz_count_chapter_pages(chapter);
        }

        /// <summary>
        /// Convert annotations or fields to permanent content.
        /// </summary>
        /// <param name="annots">convert annotations</param>
        /// <param name="widgets">convert form fields</param>
        /// <exception cref="Exception"></exception>
        public void Bake(bool annots = true, bool widgets = true)
        {
            PdfDocument pdf = AsPdfDocument(this);
            if (pdf == null)
                throw new Exception("not a PDF");
            pdf.pdf_bake_document(annots ? 1 : 0, widgets ? 1 : 0);
        }

        public void Close()
        {
            if (IsClosed)
                throw new Exception("document closed");
            if (Outline != null)
                Outline = null;
            ResetPageRefs();
            IsClosed = true;
            GraftMaps = new Dictionary<int, GraftMap>();
            _nativeDocument.Dispose();
            _nativeDocument = null;
        }

        /// <summary>
        /// Add an optional content group. An OCG is the most important unit of information to determine object visibility. For a PDF, in order to be regarded as having optional content, at least one OCG must exist.
        /// </summary>
        /// <param name="name">arbitrary name. Will show up in supporting PDF viewers.</param>
        /// <param name="config">layer configuration number. Default -1 is the standard configuration.</param>
        /// <param name="on">standard visibility status for objects pointing to this OCG.</param>
        /// <param name="intent">a string or list of strings declaring the visibility intents. There are two PDF standard values to choose from: “View” and “Design”. Default is “View”. Correct spelling is important.</param>
        /// <param name="usage">another influencer for OCG visibility. This will become part of the OCG’s /Usage key. There are two PDF standard values to choose from: “Artwork” and “Technical”. Default is “Artwork”. Please only change when required.</param>
        /// <returns>xref of the created OCG. Use as entry for oc parameter in supporting objects.</returns>
        /// <exception cref="Exception"></exception>
        public int AddOcg(
            string name,
            int config = -1,
            bool on = true,
            string intent = null,
            string usage = null
        )
        {
            int xref = 0;
            PdfDocument pdf = Document.AsPdfDocument(this);

            PdfObj ocg = pdf.pdf_add_new_dict(3);
            ocg.pdf_dict_put(new PdfObj("Type"), new PdfObj("OCG"));
            ocg.pdf_dict_put_text_string(new PdfObj("Name"), name);
            PdfObj intents = ocg.pdf_dict_put_array(new PdfObj("Intent"), 2);

            if (string.IsNullOrEmpty(intent))
                intents.pdf_array_push(new PdfObj("View"));
            else
                intents.pdf_array_push(mupdf.mupdf.pdf_new_name(intent));
            PdfObj useFor = ocg.pdf_dict_put_dict(new PdfObj("Usage"), 3);
            PdfObj ciName = mupdf.mupdf.pdf_new_name("CreatorInfo");
            PdfObj creInfo = useFor.pdf_dict_put_dict(ciName, 2);
            creInfo.pdf_dict_put_text_string(new PdfObj("Creator"), "PyMuPDF");

            if (!string.IsNullOrEmpty(usage))
                creInfo.pdf_dict_put_name(new PdfObj("Subtype"), usage);
            else
                creInfo.pdf_dict_put_name(new PdfObj("Subtype"), "Artwork");
            PdfObj indOcg = pdf.pdf_add_object(ocg);

            PdfObj ocp = Utils.EnsureOCProperties(pdf);
            PdfObj obj = ocp.pdf_dict_get(new PdfObj("OCGs"));
            obj.pdf_array_push(indOcg);
            PdfObj cfg;
            if (config > -1)
            {
                obj = ocp.pdf_dict_get(new PdfObj("Configs"));
                if (obj.pdf_is_array() == 0)
                    throw new Exception(Utils.ErrorMessages["MSG_BAD_OC_CONFIG"]);
                cfg = obj.pdf_array_get(config);
                if (cfg.m_internal == null)
                    throw new Exception(Utils.ErrorMessages["MSG_BAD_OC_CONFIG"]);
            }
            else
            {
                cfg = ocp.pdf_dict_get(new PdfObj("D"));
            }

            obj = cfg.pdf_dict_get(new PdfObj("Order"));
            if (obj.m_internal == null)
                cfg.pdf_dict_put_array(new PdfObj("Order"), 1);
            obj.pdf_array_push(indOcg);

            if (on)
            {
                obj = cfg.pdf_dict_get(new PdfObj("ON"));
                if (obj.m_internal == null)
                    obj = cfg.pdf_dict_put_array(new PdfObj("ON"), 1);
            }
            else
            {
                obj = cfg.pdf_dict_get(new PdfObj("OFF"));
                if (obj.m_internal == null)
                    obj = cfg.pdf_dict_put_array(new PdfObj("OFF"), 1);
            }
            obj.pdf_array_push(indOcg);
            mupdf.mupdf.ll_pdf_read_ocg(pdf.m_internal);

            xref = indOcg.pdf_to_num();
            return xref;
        }

        /// <summary>
        /// Check whether incremental saves are possible.
        /// </summary>
        /// <returns></returns>
        public bool CanSaveIncrementally()
        {
            PdfDocument pdf = Document.AsPdfDocument(this);
            if (pdf.m_internal != null)
                return false;
            return pdf.pdf_can_be_saved_incrementally() != 0;
        }

        /// <summary>
        /// Add a new OC layer.
        /// </summary>
        /// <param name="name">arbitrary name.</param>
        /// <param name="creator">(optional) creating software.</param>
        /// <param name="on">a sequence of OCG</param>
        public void AddLayer(string name, string creator = null, OCLayerConfig on = null)
        {
            PdfDocument pdf = Document.AsPdfDocument(this);
            Utils.AddLayerConfig(pdf, name, creator, on);
            mupdf.mupdf.ll_pdf_read_ocg(pdf.m_internal);
        }

        /// <summary>
        /// Decrypt document.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int Authenticate(string password)
        {
            if (IsClosed)
                throw new Exception("document closed");
            int val = _nativeDocument.fz_authenticate_password(password);
            if (val != 0)
            {
                IsEncrypted = false;
                InitDocument();
                ThisOwn = true;
            }
            return val;
        }

        /// <summary>
        /// Get (chapter, page) of previous page.
        /// </summary>
        /// <param name="pno">current page number</param>
        /// <param name="chapter">chapter number</param>
        /// <returns>The tuple of the preceding page.</returns>
        public (int, int) PrevLocation(int pno, int chapter = 0)
        {
            if (IsClosed || IsEncrypted)
                throw new Exception("document closed or encrypted");
            if (pno == 0 && chapter == 0)
                return (-1, -1);
            FzLocation loc = mupdf.mupdf.fz_make_location(chapter, pno);
            FzLocation prevLoc = _nativeDocument.fz_previous_page(loc);
            return (prevLoc.chapter, prevLoc.page);
        }
    }
}
