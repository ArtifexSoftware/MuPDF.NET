using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace CSharpMuPDF
{
    public class Outline : IDisposable
    {
        private FzOutline _nativeOutline;
        public Outline(FzOutline ol)
        {
            _nativeOutline = ol;
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
