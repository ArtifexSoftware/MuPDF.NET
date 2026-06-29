using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MuPDF.NET
{
    public class FilePtrOutput : mupdf.FzOutput2
    {
        public MemoryStream data { get; set; }

        public FilePtrOutput(MemoryStream src) : base()
        {
            data = src;
            use_virtual_write();
            use_virtual_seek();
            use_virtual_tell();
            use_virtual_truncate();
        }

        public override void seek(mupdf.fz_context arg_0, long arg_2, int arg_3)
        {
            data.Seek(arg_2, (SeekOrigin)arg_3);
        }

        public override long tell(mupdf.fz_context arg_0)
        {
            return data.Position;
        }

        public override void truncate(mupdf.fz_context arg_0)
        {
            data.SetLength(0);
        }

        public override void write(mupdf.fz_context arg_0, mupdf.SWIGTYPE_p_void arg_2, ulong arg_3)
        {
            if (arg_3 == 0)
                return;
            var ptr = mupdf.SWIGTYPE_p_void.getCPtr(arg_2);
            if (ptr.Handle == IntPtr.Zero)
                return;
            byte[] bytes = new byte[(int)arg_3];
            Marshal.Copy(ptr.Handle, bytes, 0, bytes.Length);
            data.Write(bytes, 0, bytes.Length);
        }
    }
}
