using mupdf;

namespace MuPDF.NET
{
    public class DisplayList
    {

        static DisplayList()
        {
            Utils.InitApp();
        }

        private FzDisplayList _nativeDisplayList;

        /// <summary>
        /// Contains the display list's mediabox.
        /// </summary>
        public Rect Rect
        {
            get
            {
                return new Rect(_nativeDisplayList.fz_bound_display_list());
            }
        }

        public bool ThisOwn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rect">The page's rectangle.</param>
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

        public void Dispose()
        {
            if (_nativeDisplayList != null)
            {
                _nativeDisplayList.Dispose();
                _nativeDisplayList = null;
                ThisOwn = false;
            }
        }

        /// <summary>
        /// Returns native object
        /// </summary>
        /// <returns>FzDisplayList</returns>
        public FzDisplayList ToFzDisplayList()
        {
            return _nativeDisplayList;
        }

        /// <summary>
        /// Run the display list through a draw device and return a pixmap.
        /// </summary>
        /// <param name="matrix">matrix to use. Default is the identity matrix.</param>
        /// <param name="colorSpace">the desired colorspace. Default is RGB.</param>
        /// <param name="alpha">determine whether or not (0, default) to include a transparency channel.</param>
        /// <param name="clip">restrict rendering to the intersection of this area with Rect.</param>
        /// <returns>pixmap of the display list.</returns>
        public Pixmap GetPixmap(Matrix matrix = null, ColorSpace colorSpace = null, int alpha = 0, Rect clip = null)
        {
            FzColorspace _colorSpace;
            if (colorSpace != null)
                _colorSpace = colorSpace.ToFzColorspace();
            else
                _colorSpace = new FzColorspace(mupdf.FzColorspace.Fixed.Fixed_RGB);

            if (matrix == null)
                matrix = new Matrix(1.0f, 1.0f);

            Pixmap val = Utils.GetPixmapFromDisplaylist(_nativeDisplayList, matrix, _colorSpace, alpha, clip, null);
            ThisOwn = true;

            return val;
        }

        /// <summary>
        /// Run the display list through a text device and return a text page.
        /// </summary>
        /// <param name="flags"control which information is parsed into a text page.</param>
        /// <returns>text page of the display list.</returns>
        public TextPage GetTextPage(int flags = 3)
        {
            FzStextOptions opts = new FzStextOptions();
            opts.flags = flags;

            FzStextPage textPage = new FzStextPage(_nativeDisplayList, opts);
            TextPage ret = new TextPage(textPage);
            ret.ThisOwn = true;
           
            return ret;
        }

        /// <summary>
        /// Run the display list through a device. The device will populate the display list with its "commands" (i.e. text extraction or image creation). The display list can later be used to "read" a page many times without having to re-interpret it from the document file.
        /// </summary>
        /// <param name="dw">Device</param>
        /// <param name="matrix">Transformation matrix to apply to the display list contents.</param>
        /// <param name="area">Only the part visible within this area will be considered when the list is run through the device.</param>
        public void Run(DeviceWrapper dw, Matrix matrix, Rect area)
        {
            _nativeDisplayList.fz_run_display_list(
                dw.ToFzDevice(),
                matrix.ToFzMatrix(),
                area.ToFzRect(),
                new FzCookie());
        }
    }
}
