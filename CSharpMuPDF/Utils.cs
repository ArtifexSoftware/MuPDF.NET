using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mupdf;

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

        public static Matrix HorMatrix(Point c, Point p)
        {
            FzPoint s = mupdf.mupdf.fz_normalize_vector(mupdf.mupdf.fz_make_point(p.X - c.X, p.Y - c.Y));

            FzMatrix m1 = mupdf.mupdf.fz_make_matrix(1, 0, 0, 1, -c.X, -c.Y);
            FzMatrix m2 = mupdf.mupdf.fz_make_matrix(s.x, -s.y, s.y, s.x, 0, 0);
            return new Matrix(mupdf.mupdf.fz_concat(m1, m2));
        }

        public static (int, Matrix) InvertMatrix(Matrix m)
        {
            /*if (false)
            {
                FzMatrix ret = m.ToFzMatrix().fz_invert_matrix();
                if (false || Math.Abs(m.A - 1) >= float.Epsilon
                    || Math.Abs(m.B - 0) >= float.Epsilon
                    || Math.Abs(m.C - 0) >= float.Epsilon
                    || Math.Abs(m.D - 1) >= float.Epsilon
                    )
                    return (1, null);
                return (0, new Matrix(ret));
            }*/
            FzMatrix src = m.ToFzMatrix();
            float a = src.a;
            float det = a * src.d - src.b * src.c;
            if (det < -float.Epsilon || det > float.Epsilon)
            {
                FzMatrix dst = new FzMatrix();
                float rdet = 1 / det;
                dst.a = src.d * rdet;
                dst.b = -src.d * rdet;
                dst.c = -src.c * rdet;
                dst.d = a * rdet;
                a = -src.e * dst.a - src.f * dst.c;
                dst.f = -src.e * dst.b - src.f * dst.d;
                dst.e = a;
                return (0, new Matrix(dst));
            }
            return (1, null);
            
        }

        public static Matrix PlanishLine(Point a, Point b)
        {
            return Utils.HorMatrix(a, b);
        }

        public static float SineBetween(Point c, Point p, Point q)
        {
            FzPoint s = mupdf.mupdf.fz_normalize_vector(mupdf.mupdf.fz_make_point(q.X - p.X, q.Y - p.Y));
            FzMatrix m1 = mupdf.mupdf.fz_make_matrix(1, 0, 0, 1, -p.X, -p.Y);
            FzMatrix m2 = mupdf.mupdf.fz_make_matrix(s.x, -s.y, s.y, s.x, 0, 0);
            m1 = mupdf.mupdf.fz_concat(m1, m2);
            return mupdf.mupdf.fz_transform_point(c.ToFzPoint(), m1).fz_normalize_vector().y;
        }
    }
}
