using mupdf;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace MuPDF.NET
{
    public class DisplayList : IDisposable
    {
        private FzDisplayList _nativeDisplayList;

        private FzColorspace _colorSpace;

        public bool ThisOwn;

        public DisplayList(Rect rect)
        {
            _nativeDisplayList = new FzDisplayList(rect.ToFzRect());
            ThisOwn = true;
        }

        public DisplayList(DisplayList displayList)
        {
            _nativeDisplayList = displayList.ToFzDisplayList();
        }

        public DisplayList(FzDisplayList displayList)
        {
            _nativeDisplayList = displayList;
        }

        public FzDisplayList ToFzDisplayList()
        {
            return _nativeDisplayList;
        }

        public Pixmap GetPixmap(Matrix matrix = null, ColorSpace colorSpace = null, int alpha = 0, Rect clip = null)
        {
            if (colorSpace != null)
                _colorSpace = colorSpace.ToFzColorspace();
            else
                _colorSpace = new FzColorspace(mupdf.FzColorspace.Fixed.Fixed_RGB);
            Pixmap val = Utils.GetPixmapFromDisplaylist(_nativeDisplayList, matrix, _colorSpace, alpha, clip, null);
            ThisOwn = true;
            return val;
        }

        public void Dispose()
        {
            _nativeDisplayList.Dispose();
        }

        public MuPDFTextPage GetTextPage(int flags = 3)
        {
            FzStextOptions opts = new FzStextOptions();
            opts.flags = flags;

            /*IntPtr pList = Marshal.AllocHGlobal(Marshal.SizeOf(_nativeDisplayList.m_internal));
            Marshal.StructureToPtr(_nativeDisplayList.m_internal, pList, false);

            fz_stext_page val = mupdf.mupdf.ll_fz_new_stext_page_from_display_list(new SWIGTYPE_p_fz_display_list(pList, true), opts.internal_());
            MuPDFTextPage ret = new MuPDFTextPage(new FzStextPage(val));
            ret.ThisOwn = true;*/
            Console.WriteLine(Marshal.SizeOf(_nativeDisplayList.m_internal));
            return null;
        }
    }
}
