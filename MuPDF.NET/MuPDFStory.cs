using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class MuPDFStory
    {
        private FzStory _nativeStory;

        public MuPDFStory(string html = "", string userCss = null, int em = 12, MuPDFArchive archive = null)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(html);
            
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
            // Call unmanaged code
            SWIGTYPE_p_unsigned_char s = new SWIGTYPE_p_unsigned_char(unmanagedPointer, false);
            FzBuffer buf = mupdf.mupdf.fz_new_buffer_from_copied_data(s, (uint)bytes.Length);
            Marshal.FreeHGlobal(unmanagedPointer);

            FzArchive arch = archive != null ? archive.ToFzArchive() : new FzArchive();
            _nativeStory = new FzStory(buf, userCss, em, arch);
        }

        public void AddHeaderIds()
        {

        }

        public string GetDocument()
        {
            FzXml dom = _nativeStory.fz_story_document();
            return new Xml(dom);
        }
    }
}
