using mupdf;

namespace MuPDF.NET
{
    public class Outline
    {
        private FzOutline _nativeOutline;

        /// <summary>
        /// The link destination details object.
        /// </summary>
        public MuPDFLinkDest Dest
        {
            get
            {
                return new MuPDFLinkDest(this, (null, 0, 0), null);
            }
        }

        /// <summary>
        /// The next outline item on the next level down.
        /// </summary>
        public Outline Down
        {
            get
            {
                FzOutline downOL = _nativeOutline.down();
                if (downOL == null)
                    return null;
                return new Outline(downOL);
            }
        }

        /// <summary>
        /// A bool specifying whether the target is outside of the current document.
        /// </summary>
        public bool IsExternal
        {
            get
            {
                if (_nativeOutline == null)
                    return false;

                string uri = _nativeOutline.m_internal.uri;
                if (uri is null)
                    return false;
                return mupdf.mupdf.fz_is_external_link(uri) != 0;
            }
        }

        /// <summary>
        /// Indicator showing whether any sub-outlines should be expanded (true) or be collapsed (false). This information is interpreted by PDF reader software.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return _nativeOutline.m_internal.is_open != 0;
            }
        }

        /// <summary>
        /// The next outline item at the same level as this item.
        /// </summary>
        public Outline Next
        {
            get
            {
                FzOutline nextOL = _nativeOutline.next();
                if (nextOL.m_internal == null)
                    return null;
                return new Outline(nextOL);
            }
        }

        /// <summary>
        /// The page number (0-based) this bookmark points to.
        /// </summary>
        public int Page
        {
            get
            {
                return _nativeOutline.page().page;
            }
        }

        /// <summary>
        /// The item's title as a string or *null*.
        /// </summary>
        public string Title
        {
            get
            {
                return _nativeOutline.title();
            }
        }

        /// <summary>
        /// A string specifying the link target.
        /// </summary>
        public string Uri
        {
            get
            {
                return _nativeOutline.m_internal.uri;
            }
        }

        public float X
        {
            get
            {
                return _nativeOutline.m_internal.x;
            }
        }

        public float Y
        {
            get
            {
                return _nativeOutline.m_internal.y;
            }
        }
        public Outline(FzOutline ol)
        {
            _nativeOutline = ol;
        }

        /// <summary>
        /// Like `dest` property but uses `document` to resolve destinations for kind=LINK_NAMED.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public MuPDFLinkDest Destination(PdfDocument doc)
        {
            return new MuPDFLinkDest(this, (null, 0, 0), new MuPDFDocument(doc));
        }

        public FzOutline ToFzOutline()
        {
            return _nativeOutline;
        }
    }
}
