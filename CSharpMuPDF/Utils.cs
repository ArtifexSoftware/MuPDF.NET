using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMuPDF
{
    public static class Utils
    {
        public static int FZ_MIN_INF_RECT = (int)(-0x80000000);

        public static int FZ_MAX_INF_RECT = (int)0x7fffff80;

        public static double FLT_EPSILON = 1e-5;
        public static string GetImageExtention(int type)
        {
            if (type == (int)ImageType.FZ_IMAGE_FAX) return "fax";
            if (type == (int)ImageType.FZ_IMAGE_RAW) return "raw";
            if (type == (int)ImageType.FZ_IMAGE_FLATE) return "flate";
            if (type == (int)ImageType.FZ_IMAGE_RLD) return "rld";
            if (type == (int)ImageType.FZ_IMAGE_BMP) return "bmp";
            if (type == (int)ImageType.FZ_IMAGE_GIF) return "gif";
            if (type == (int)ImageType.FZ_IMAGE_LZW) return "lzw";
            if (type == (int)ImageType.FZ_IMAGE_JBIG2) return "jb2";
            if (type == (int)ImageType.FZ_IMAGE_JPEG) return "jpeg";
            if (type == (int)ImageType.FZ_IMAGE_JPX) return "jpx";
            if (type == (int)ImageType.FZ_IMAGE_JXR) return "jxr";
            if (type == (int)ImageType.FZ_IMAGE_PNG) return "png";
            if (type == (int)ImageType.FZ_IMAGE_PNM) return "pnm";
            if (type == (int)ImageType.FZ_IMAGE_TIFF) return "tiff";
            return "n/a";
        }
    }
}
