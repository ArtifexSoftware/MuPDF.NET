using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using mupdf;


namespace CSharpMuPDF
{
    public class MuPDFDocument
    {
        private bool _isClosed = false;

        private bool _isEncrypted = false;

        public string METADATA = "";

        public string FONTINFO = "";

        public string GRAFTMAPS = "";

        public string SHOWNPAGES = "";

        public string INSERTEDIMAGES = "";

        private FzDocument _nativeDocument;

        public bool IsPDF
        {
            get
            {
                return true;
            }
        }

        public MuPDFDocument(string filename)
        {
            _nativeDocument = new FzDocument(filename);
        }

        public MuPDFDocument(PdfDocument doc)
        {
            _nativeDocument = new FzDocument(doc);
        }

        public int GetPageCount()
        {
            return _nativeDocument.fz_count_pages();
        }

        public MuPDFSTextPage GetStextPage(int i)
        {
            return new MuPDFSTextPage(_nativeDocument.fz_load_page(i));
        }

        public FzDocument ToFzDocument()
        {
            return _nativeDocument;
        }

        private static PdfDocument AsPdfDocument(dynamic document)
        {
            if (document is MuPDFDocument)
                return document._nativeDocument;
            if (document is PdfDocument)
                return document;
            if (document is FzDocument)
                return new PdfDocument(document);
            return null;
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
            string objStr = MuPDFSTextPage.EscapeStrFromBuffer(res);

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
            PdfLexbuf lexBuffer = new PdfLexbuf(256);
            PdfObj ret = doc.pdf_parse_stm_obj(stream,lexBuffer);

            return ret;
        }

        public void SetKeyXRef(int xref, string key, string value)
        {
            if (_isClosed)
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
    }
}
