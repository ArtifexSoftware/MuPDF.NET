using mupdf;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace MuPDF.NET
{
    public class DisplayList
    {
        private FzDisplayList _nativeDisplayList;

        private FzColorspace _colorSpace;

        public bool ThisOwn { get; set; }

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

        public MuPDFTextPage GetTextPage(int flags = 3)
        {
            FzStextOptions opts = new FzStextOptions();
            opts.flags = flags;

            FzStextPage textPage = new FzStextPage(_nativeDisplayList, opts);
            MuPDFTextPage ret = new MuPDFTextPage(textPage);
            ret.ThisOwn = true;
           
            return ret;
        }
    }
}
