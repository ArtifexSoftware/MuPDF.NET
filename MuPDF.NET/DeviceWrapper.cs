using mupdf;

namespace MuPDF.NET
{
    public class DeviceWrapper
    {
        static DeviceWrapper()
        {
            Utils.InitApp();
        }

        internal FzDevice _nativeDevice;

        public DeviceWrapper(FzDevice device)
        {
            _nativeDevice = device;
        }

        public DeviceWrapper(Pixmap pixmap, Rect clip)
        {
            FzIrect bbox = new IRect(clip).ToFzIrect();
            if (bbox.fz_is_infinite_irect() != 0)
                _nativeDevice = mupdf.mupdf.fz_new_draw_device(new FzMatrix(), pixmap.ToFzPixmap());
            else
                _nativeDevice = mupdf.mupdf.fz_new_draw_device_with_bbox(
                    new FzMatrix(),
                    pixmap.ToFzPixmap(),
                    bbox
                );
        }

        public DeviceWrapper(FzDisplayList dl)
        {
            _nativeDevice = dl.fz_new_list_device();
        }

        public DeviceWrapper(TextPage stpage, int flags)
        {
            FzStextOptions opts = new FzStextOptions(flags);
            _nativeDevice = stpage._nativeTextPage.fz_new_stext_device(opts);
        }

        public FzDevice ToFzDevice()
        {
            return _nativeDevice;
        }
    }
}
