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

        public float ASCENDER
        {
            get
            {
                return _nativeFont.fz_font_ascender();
            }
        }

        public byte[] BUFFER
        {
            get
            {
                FzBuffer buf = new FzBuffer(mupdf.mupdf.ll_fz_keep_buffer(_nativeFont.m_internal.buffer));
                return buf.fz_buffer_extract();
            }
        }

        public float DESCENDER
        {
            get
            {
                return _nativeFont.fz_font_descender();
            }
        }

        public string NAME
        {
            get
            {
                return _nativeFont.fz_font_name();
            }
        }


        public int FLAGS
        {
            get
            {
                fz_font_flags_t f = mupdf.mupdf.ll_fz_font_flags(_nativeFont.m_internal);
                if (f == null)
                    return 0;

            }
        }

        public Font()
        {

        }

        public override string ToString()
        {
            return $"Font('{NAME}')";
        }
        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
