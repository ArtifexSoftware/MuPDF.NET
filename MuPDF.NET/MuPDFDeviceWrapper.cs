using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class MuPDFDeviceWrapper
    {
        internal FzDevice _nativeDevice;

        public MuPDFDeviceWrapper(FzDevice device)
        {
            _nativeDevice = device;
        }

        public MuPDFDeviceWrapper(Pixmap pixmap, Rect clip)
        {
            FzIrect bbox = new IRect(clip).ToFzIrect();
            if (bbox.fz_is_infinite_irect() != 0)
                _nativeDevice = mupdf.mupdf.fz_new_draw_device(new FzMatrix(), pixmap.ToFzPixmap());
            else
                _nativeDevice = mupdf.mupdf.fz_new_draw_device_with_bbox(new FzMatrix(), pixmap.ToFzPixmap(), bbox);
        }

        public MuPDFDeviceWrapper(FzDisplayList dl)
        {
            _nativeDevice = dl.fz_new_list_device();
        }

        public MuPDFDeviceWrapper(MuPDFSTextPage stpage, int flags)
        {
            FzStextOptions opts = new FzStextOptions(flags);
            _nativeDevice = stpage._nativeSTextPage.fz_new_stext_device(opts);
        }

        public FzDevice ToFzDevice()
        {
            return _nativeDevice;
        }
    }
}
