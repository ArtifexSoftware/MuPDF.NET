using mupdf;

namespace MuPDF.NET
{
    public class Outline : IDisposable
    {
        private FzOutline _nativeOutline;

        public LinkDest Dest
        {
            get
            {
                return new LinkDest(this, null, null);
            }
        }

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

        public bool IsOpen
        {
            get
            {
                return _nativeOutline.m_internal.is_open != 0;
            }
        }

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

        public int Page
        {
            get
            {
                return _nativeOutline.page().page;
            }
        }

        public string Title
        {
            get
            {
                return _nativeOutline.m_internal.title;
            }
        }

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

        public LinkDest Destination(PdfDocument doc)
        {
            return new LinkDest(this, null, new MuPDFDocument(doc));
        }

        public void Dispose()
        {
            _nativeOutline.Dispose();
        }

        public FzOutline ToFzOutline()
        {
            return _nativeOutline;
        }
    }
}
