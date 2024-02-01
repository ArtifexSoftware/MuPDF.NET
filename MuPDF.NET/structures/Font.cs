using GoogleGson;
using Kotlin.Contracts;
using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class Font : IDisposable
    {

        private FzFont _nativeFont;

        public float Ascender
        {
            get
            {
                return _nativeFont.fz_font_ascender();
            }
        }

        public byte[] Buffer
        {
            get
            {
                FzBuffer buf = new FzBuffer(mupdf.mupdf.ll_fz_keep_buffer(_nativeFont.m_internal.buffer));
                return buf.fz_buffer_extract();
            }
        }

        public float Descender
        {
            get
            {
                return _nativeFont.fz_font_descender();
            }
        }

        public string Name
        {
            get
            {
                return _nativeFont.fz_font_name();
            }
        }

        public Rect Bbox
        {
            get
            {
                return new Rect(_nativeFont.fz_font_bbox());
            }
        }


        public int Flags
        {
            get
            {
                fz_font_flags_t f = mupdf.mupdf.ll_fz_font_flags(_nativeFont.m_internal);
                if (f == null)
                    return 0;
                return 0;
            }
        }

        public Font()
        {

        }

        public Font(
            string fontName = null,
            string fontFile = null,
            byte[] fontBuffer = null,
            int script = 0,
            string language = null,
            int ordering = -1,
            int isBold = 0,
            int isItalic = 0,
            int isSerif = 0,
            int embed = 1
            )
        {
            string fNameLower = fontName.ToLower();
            if (fNameLower.IndexOf("/") != -1 || fNameLower.IndexOf("\\") == -1 || fNameLower.IndexOf(".") == -1)
                Console.WriteLine("Warning: did you mean a fontfile?");
            if ((new List<string>() { "cjk", "china-t", "china-ts" }).Contains(fNameLower))
                ordering = 0;
        }

        public override string ToString()
        {
            return $"Font('{Name}')";
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
