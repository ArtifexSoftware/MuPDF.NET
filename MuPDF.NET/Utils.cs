using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using mupdf;

namespace MuPDF.NET
{
    
    public static class Utils
    {
        public static int FZ_MIN_INF_RECT = (int)(-0x80000000);

        public static int FZ_MAX_INF_RECT = (int)0x7fffff80;

        public static double FLT_EPSILON = 1e-5;

        public static string ANNOT_ID_STEM = "fitz";

        public static int SigFlag_SignaturesExist = 1;
        public static int SigFlag_AppendOnly = 2;

        public static int UNIQUE_ID = 0;

        public static int TEXT_ALIGN_LEFT = 0;
        public static int TEXT_ALIGN_CENTER = 1;
        public static int TEXT_ALIGN_RIGHT = 2;
        public static int TEXT_ALIGN_JUSTIFY = 3;

        public static string TESSDATA_PREFIX = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");

        public static Dictionary<string, string> AnnotSkel = new Dictionary<string, string>(){
            { "goto1", "<</A<</S/GoTo/D[{0, 10} 0 R/XYZ {1} {2} {3}]>>/Rect[{4}]/BS<</W 0>>/Subtype/Link>>" },
            { "goto2", "<</A<</S/GoTo/D{0}>>/Rect[{1}]/BS<</W 0>>/Subtype/Link>>" },
            { "gotor1", "<</A<</S/GoToR/D[{0} /XYZ {1} {2} {3}]/F<</F({4})/UF({5})/Type/Filespec>>>>/Rect[{6}]/BS<</W 0>>/Subtype/Link>>" },
            { "gotor2", "<</A<</S/GoToR/D{0}/F({1})>>/Rect[{2}]/BS<</W 0>>/Subtype/Link>>" },
            { "launch", "<</A<</S/Launch/F<</F({0})/UF({1})/Type/Filespec>>>>/Rect[{2}]/BS<</W 0>>/Subtype/Link>>" },
            { "uri", "<</A<</S/URI/URI({0})>>/Rect[{1}]/BS<</W 0>>/Subtype/Link>>" },
            { "named", "<</A<</S/GoTo/D({0})/Type/Action>>/Rect[{1}]/BS<</W 0>>/Subtype/Link>>" }
        };

        public static List<string> MUPDF_WARNINGS_STORE = new List<string>();

        public static List<(int, double)> zapf_glyphs = new List<(int, double)>() {
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (32, 0.278),
        (33, 0.974),
        (34, 0.961),
        (35, 0.974),
        (36, 0.98),
        (37, 0.719),
        (38, 0.789),
        (39, 0.79),
        (40, 0.791),
        (41, 0.69),
        (42, 0.96),
        (43, 0.939),
        (44, 0.549),
        (45, 0.855),
        (46, 0.911),
        (47, 0.933),
        (48, 0.911),
        (49, 0.945),
        (50, 0.974),
        (51, 0.755),
        (52, 0.846),
        (53, 0.762),
        (54, 0.761),
        (55, 0.571),
        (56, 0.677),
        (57, 0.763),
        (58, 0.76),
        (59, 0.759),
        (60, 0.754),
        (61, 0.494),
        (62, 0.552),
        (63, 0.537),
        (64, 0.577),
        (65, 0.692),
        (66, 0.786),
        (67, 0.788),
        (68, 0.788),
        (69, 0.79),
        (70, 0.793),
        (71, 0.794),
        (72, 0.816),
        (73, 0.823),
        (74, 0.789),
        (75, 0.841),
        (76, 0.823),
        (77, 0.833),
        (78, 0.816),
        (79, 0.831),
        (80, 0.923),
        (81, 0.744),
        (82, 0.723),
        (83, 0.749),
        (84, 0.79),
        (85, 0.792),
        (86, 0.695),
        (87, 0.776),
        (88, 0.768),
        (89, 0.792),
        (90, 0.759),
        (91, 0.707),
        (92, 0.708),
        (93, 0.682),
        (94, 0.701),
        (95, 0.826),
        (96, 0.815),
        (97, 0.789),
        (98, 0.789),
        (99, 0.707),
        (100, 0.687),
        (101, 0.696),
        (102, 0.689),
        (103, 0.786),
        (104, 0.787),
        (105, 0.713),
        (106, 0.791),
        (107, 0.785),
        (108, 0.791),
        (109, 0.873),
        (110, 0.761),
        (111, 0.762),
        (112, 0.762),
        (113, 0.759),
        (114, 0.759),
        (115, 0.892),
        (116, 0.892),
        (117, 0.788),
        (118, 0.784),
        (119, 0.438),
        (120, 0.138),
        (121, 0.277),
        (122, 0.415),
        (123, 0.392),
        (124, 0.392),
        (125, 0.668),
        (126, 0.668),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (183, 0.788),
        (161, 0.732),
        (162, 0.544),
        (163, 0.544),
        (164, 0.91),
        (165, 0.667),
        (166, 0.76),
        (167, 0.76),
        (168, 0.776),
        (169, 0.595),
        (170, 0.694),
        (171, 0.626),
        (172, 0.788),
        (173, 0.788),
        (174, 0.788),
        (175, 0.788),
        (176, 0.788),
        (177, 0.788),
        (178, 0.788),
        (179, 0.788),
        (180, 0.788),
        (181, 0.788),
        (182, 0.788),
        (183, 0.788),
        (184, 0.788),
        (185, 0.788),
        (186, 0.788),
        (187, 0.788),
        (188, 0.788),
        (189, 0.788),
        (190, 0.788),
        (191, 0.788),
        (192, 0.788),
        (193, 0.788),
        (194, 0.788),
        (195, 0.788),
        (196, 0.788),
        (197, 0.788),
        (198, 0.788),
        (199, 0.788),
        (200, 0.788),
        (201, 0.788),
        (202, 0.788),
        (203, 0.788),
        (204, 0.788),
        (205, 0.788),
        (206, 0.788),
        (207, 0.788),
        (208, 0.788),
        (209, 0.788),
        (210, 0.788),
        (211, 0.788),
        (212, 0.894),
        (213, 0.838),
        (214, 1.016),
        (215, 0.458),
        (216, 0.748),
        (217, 0.924),
        (218, 0.748),
        (219, 0.918),
        (220, 0.927),
        (221, 0.928),
        (222, 0.928),
        (223, 0.834),
        (224, 0.873),
        (225, 0.828),
        (226, 0.924),
        (227, 0.924),
        (228, 0.917),
        (229, 0.93),
        (230, 0.931),
        (231, 0.463),
        (232, 0.883),
        (233, 0.836),
        (234, 0.836),
        (235, 0.867),
        (236, 0.867),
        (237, 0.696),
        (238, 0.696),
        (239, 0.874),
        (183, 0.788),
        (241, 0.874),
        (242, 0.76),
        (243, 0.946),
        (244, 0.771),
        (245, 0.865),
        (246, 0.771),
        (247, 0.888),
        (248, 0.967),
        (249, 0.888),
        (250, 0.831),
        (251, 0.873),
        (252, 0.927),
        (253, 0.97),
        (183, 0.788),
        (183, 0.788),
        };

        public static List<(int, double)> symbol_glyphs = new List<(int, double)>() {
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (32, 0.25),
        (33, 0.333),
        (34, 0.713),
        (35, 0.5),
        (36, 0.549),
        (37, 0.833),
        (38, 0.778),
        (39, 0.439),
        (40, 0.333),
        (41, 0.333),
        (42, 0.5),
        (43, 0.549),
        (44, 0.25),
        (45, 0.549),
        (46, 0.25),
        (47, 0.278),
        (48, 0.5),
        (49, 0.5),
        (50, 0.5),
        (51, 0.5),
        (52, 0.5),
        (53, 0.5),
        (54, 0.5),
        (55, 0.5),
        (56, 0.5),
        (57, 0.5),
        (58, 0.278),
        (59, 0.278),
        (60, 0.549),
        (61, 0.549),
        (62, 0.549),
        (63, 0.444),
        (64, 0.549),
        (65, 0.722),
        (66, 0.667),
        (67, 0.722),
        (68, 0.612),
        (69, 0.611),
        (70, 0.763),
        (71, 0.603),
        (72, 0.722),
        (73, 0.333),
        (74, 0.631),
        (75, 0.722),
        (76, 0.686),
        (77, 0.889),
        (78, 0.722),
        (79, 0.722),
        (80, 0.768),
        (81, 0.741),
        (82, 0.556),
        (83, 0.592),
        (84, 0.611),
        (85, 0.69),
        (86, 0.439),
        (87, 0.768),
        (88, 0.645),
        (89, 0.795),
        (90, 0.611),
        (91, 0.333),
        (92, 0.863),
        (93, 0.333),
        (94, 0.658),
        (95, 0.5),
        (96, 0.5),
        (97, 0.631),
        (98, 0.549),
        (99, 0.549),
        (100, 0.494),
        (101, 0.439),
        (102, 0.521),
        (103, 0.411),
        (104, 0.603),
        (105, 0.329),
        (106, 0.603),
        (107, 0.549),
        (108, 0.549),
        (109, 0.576),
        (110, 0.521),
        (111, 0.549),
        (112, 0.549),
        (113, 0.521),
        (114, 0.549),
        (115, 0.603),
        (116, 0.439),
        (117, 0.576),
        (118, 0.713),
        (119, 0.686),
        (120, 0.493),
        (121, 0.686),
        (122, 0.494),
        (123, 0.48),
        (124, 0.2),
        (125, 0.48),
        (126, 0.549),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (183, 0.46),
        (160, 0.25),
        (161, 0.62),
        (162, 0.247),
        (163, 0.549),
        (164, 0.167),
        (165, 0.713),
        (166, 0.5),
        (167, 0.753),
        (168, 0.753),
        (169, 0.753),
        (170, 0.753),
        (171, 1.042),
        (172, 0.713),
        (173, 0.603),
        (174, 0.987),
        (175, 0.603),
        (176, 0.4),
        (177, 0.549),
        (178, 0.411),
        (179, 0.549),
        (180, 0.549),
        (181, 0.576),
        (182, 0.494),
        (183, 0.46),
        (184, 0.549),
        (185, 0.549),
        (186, 0.549),
        (187, 0.549),
        (188, 1),
        (189, 0.603),
        (190, 1),
        (191, 0.658),
        (192, 0.823),
        (193, 0.686),
        (194, 0.795),
        (195, 0.987),
        (196, 0.768),
        (197, 0.768),
        (198, 0.823),
        (199, 0.768),
        (200, 0.768),
        (201, 0.713),
        (202, 0.713),
        (203, 0.713),
        (204, 0.713),
        (205, 0.713),
        (206, 0.713),
        (207, 0.713),
        (208, 0.768),
        (209, 0.713),
        (210, 0.79),
        (211, 0.79),
        (212, 0.89),
        (213, 0.823),
        (214, 0.549),
        (215, 0.549),
        (216, 0.713),
        (217, 0.603),
        (218, 0.603),
        (219, 1.042),
        (220, 0.987),
        (221, 0.603),
        (222, 0.987),
        (223, 0.603),
        (224, 0.494),
        (225, 0.329),
        (226, 0.79),
        (227, 0.79),
        (228, 0.786),
        (229, 0.713),
        (230, 0.384),
        (231, 0.384),
        (232, 0.384),
        (233, 0.384),
        (234, 0.384),
        (235, 0.384),
        (236, 0.494),
        (237, 0.494),
        (238, 0.494),
        (239, 0.494),
        (183, 0.46),
        (241, 0.329),
        (242, 0.274),
        (243, 0.686),
        (244, 0.686),
        (245, 0.686),
        (246, 0.384),
        (247, 0.549),
        (248, 0.384),
        (249, 0.384),
        (250, 0.384),
        (251, 0.384),
        (252, 0.494),
        (253, 0.494),
        (254, 0.494),
        (183, 0.46),
        };

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

        public static Dictionary<string, string> Base14_fontdict = new Dictionary<string, string>()
        {
            {"helv", "Helvetica" },
            {"heit", "Helvetica-Oblique" },
            {"hebo", "Helvetica-Bold" },
            {"hebi", "Helvetica-BoldOblique" },
            {"cour", "Courier" },
            {"coit", "Courier-Obliqu" },
            {"cobo", "Courier-Bold" },
            {"cobi", "Courier-BoldOblique" },
            {"tiro", "Times-Roman" },
            {"tibo", "Times-Bold" },
            {"tiit", "Times-Italic" },
            {"tibi", "Times-BoldItalic" },
            {"symb", "Symbol" },
            {"zadb", "ZapfDingbats" }
        };

        public static Rect INFINITE_RECT()
        {
            return new Rect(Utils.FZ_MIN_INF_RECT, Utils.FZ_MIN_INF_RECT, Utils.FZ_MAX_INF_RECT, Utils.FZ_MAX_INF_RECT);
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

        public static PdfObj pdf_dict_getl(PdfObj obj, string[] keys)
        {
            PdfObj ret = new PdfObj();
            foreach (string key in keys)
            {
                ret = obj.pdf_dict_get(new PdfObj(key));
            }

            return ret;
        }

        public static void pdf_dict_putl(PdfObj obj, PdfObj val, string[] keys)
        {
            if (obj.pdf_is_indirect() != 0)
                obj = obj.pdf_resolve_indirect_chain();
            if (obj.pdf_is_dict() == 0)
                throw new Exception(string.Format("Not a dict: {0}", obj));
            if (keys == null)
                return;

            PdfDocument doc = obj.pdf_get_bound_document();
            for (int i = 0; i < keys.Length -1; i++)
            {
                PdfObj nextObj = obj.pdf_dict_get(new PdfObj(keys[i]));
                if (nextObj == null)
                {
                    nextObj = doc.pdf_new_dict(1);
                    obj.pdf_dict_put(new PdfObj(keys[i]), nextObj);
                }
                obj = nextObj;
            }
            string key = keys[keys.Length - 2];
            obj.pdf_dict_put(new PdfObj(key), val);
        }

        public static (int, int, int) MUPDF_VERSION = (mupdf.mupdf.FZ_VERSION_MAJOR, mupdf.mupdf.FZ_VERSION_MINOR, mupdf.mupdf.FZ_VERSION_PATCH);

        public static Dictionary<string, string> ErrorMessages = new Dictionary<string, string>()
        {
            { "MSG_BAD_ANNOT_TYPE", "bad annot type" },
            { "MSG_BAD_APN", "bad or missing annot AP/N" },
            { "MSG_BAD_ARG_INK_ANNOT", "arg must be seq of seq of float pairs" },
            { "MSG_BAD_ARG_POINTS", "bad seq of points" },
            { "MSG_BAD_BUFFER", "bad type: 'buffer'" },
            { "MSG_BAD_COLOR_SEQ", "bad color sequence" },
            { "MSG_BAD_DOCUMENT", "cannot open broken document" },
            { "MSG_BAD_FILETYPE", "bad filetype" },
            { "MSG_BAD_LOCATION", "bad location" },
            { "MSG_BAD_OC_CONFIG", "bad config number" },
            { "MSG_BAD_OC_LAYER", "bad layer number" },
            { "MSG_BAD_OC_REF", "bad 'oc' reference" },
            { "MSG_BAD_PAGEID", "bad page id" },
            { "MSG_BAD_PAGENO", "bad page number(s)" },
            { "MSG_BAD_PDFROOT", "PDF has no root" },
            { "MSG_BAD_RECT", "rect is infinite or empty" },
            { "MSG_BAD_TEXT", "bad type: 'text'" },
            { "MSG_BAD_XREF", "bad xref" },
            { "MSG_COLOR_COUNT_FAILED", "color count failed" },
            { "MSG_FILE_OR_BUFFER", "need font file or buffer" },
            { "MSG_FONT_FAILED", "cannot create font" },
            { "MSG_IS_NO_ANNOT", "is no annotation" },
            { "MSG_IS_NO_IMAGE", "is no image" },
            { "MSG_IS_NO_PDF", "is no PDF" },
            { "MSG_IS_NO_DICT", "object is no PDF dict" },
            { "MSG_PIX_NOALPHA", "source pixmap has no alpha" },
            { "MSG_PIXEL_OUTSIDE", "pixel(s) outside image" }
        };

        /// <summary>
        /// ColorSpace types
        /// </summary>
        public static int CS_RGB = 1;
        public static int CS_GRAY = 2;
        public static int CS_CMYK = 3;

        public static byte[] BinFromBuffer(FzBuffer buffer)
        {
            return buffer.fz_buffer_extract();
        }

        public static FzBuffer BufferFromBytes(byte[] bytes)
        {
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
            return mupdf.mupdf.fz_new_buffer_from_copied_data(new SWIGTYPE_p_unsigned_char(unmanagedPointer, false), (uint)bytes.Length);
        }

        public static FzBuffer CompressBuffer(FzBuffer buffer)
        {
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(8);
            SWIGTYPE_p_size_t swigSizeT = new SWIGTYPE_p_size_t(unmanagedPointer, false);
            SWIGTYPE_p_unsigned_char ret = mupdf.mupdf.fz_new_deflated_data_from_buffer(swigSizeT, buffer, fz_deflate_level.FZ_DEFLATE_BEST);
            if (ret == null || unmanagedPointer.ToInt64() == 0)
                return null;
            FzBuffer buf = new FzBuffer(mupdf.mupdf.fz_new_buffer_from_data(ret, (uint)unmanagedPointer.ToInt64()));
            Marshal.FreeHGlobal(unmanagedPointer);

            buf.fz_resize_buffer((uint)unmanagedPointer.ToInt64());
            return buf;
        }

        public static void UpdateStream(PdfDocument doc, PdfObj obj, FzBuffer buffer, int compress)
        {
            uint len = buffer.fz_buffer_storage(new SWIGTYPE_p_p_unsigned_char(IntPtr.Zero, false));
            uint nlen = len;
            FzBuffer res = null;
            if (len > 30)
            {
                res = Utils.CompressBuffer(buffer);
                nlen = res.fz_buffer_storage(new SWIGTYPE_p_p_unsigned_char(IntPtr.Zero, false));
            }
            if ((nlen < len && res != null) && compress == 1)
            {
                obj.pdf_dict_put(new PdfObj("Filter"), new PdfObj("FlateDecode"));
                doc.pdf_update_stream(obj, res, 1);
            }
            else
                doc.pdf_update_stream(obj, buffer, 0);
        }

        public static bool INRANGE(int v, int low, int high)
        {
            return low <= v && v <= high;
        }

        public static bool INRANGE(float v, float low, float high)
        {
            return low <= v && v <= high;
        }

        public static Matrix RotatePageMatrix(PdfPage page)
        {
            if (page == null)
                return new Matrix();
            int rotation = Utils.PageRotation(page);
            if (rotation == 0)
                return new Matrix();

            Point cbSize = GetCropBoxSize(page.obj());
            float w = cbSize.X;
            float h = cbSize.Y;

            FzMatrix m = new FzMatrix();
            if (rotation == 90)
                m = mupdf.mupdf.fz_make_matrix(0, 1, -1, 0, h, 0);
            else if (rotation == 180)
                m = mupdf.mupdf.fz_make_matrix(-1, 0, 0, -1, w, h);
            else
                m = mupdf.mupdf.fz_make_matrix(0, -1, 1, 0, 0, w);

            return new Matrix(m);
        }

        public static Point GetCropBoxSize(PdfObj pageObj)
        {
            FzRect rect = GetCropBox(pageObj).ToFzRect();
            float width = Math.Abs(rect.x1 - rect.x0);
            float height = Math.Abs(rect.y1 - rect.y0);

            FzPoint size = mupdf.mupdf.fz_make_point(width, height);
            return new Point(size);
        }

        public static Rect GetCropBox(PdfObj pageObj)
        {
            FzRect mediabox = Utils.GetMediaBox(pageObj).ToFzRect();
            FzRect cropBox = pageObj.pdf_dict_get_inheritable(new PdfObj("CropBox")).pdf_to_rect();
            if (cropBox.fz_is_infinite_rect() != 0 && cropBox.fz_is_empty_rect() != 0)
                cropBox = mediabox;
            float y0 = mediabox.y1 - cropBox.y1;
            float y1 = mediabox.y1 = cropBox.y0;
            cropBox.y0 = y0;
            cropBox.y1 = y1;

            return new Rect(cropBox);
        }

        public static Rect GetMediaBox(PdfObj pageObj)
        {
            FzRect pageMediaBox = new FzRect(FzRect.Fixed.Fixed_UNIT);
            FzRect mediaBox = pageObj.pdf_dict_getp_inheritable("MediaBox").pdf_to_rect();
            if (mediaBox.fz_is_empty_rect() != 0 || mediaBox.fz_is_infinite_rect() != 0)
            {
                mediaBox.x0 = 0;
                mediaBox.y0 = 0;
                mediaBox.x1 = 612;
                mediaBox.y1 = 792;
            }
            pageMediaBox = new FzRect(
                Math.Min(mediaBox.x0, mediaBox.x1),
                Math.Min(mediaBox.y0, mediaBox.y1),
                Math.Max(mediaBox.x0, mediaBox.x1),
                Math.Max(mediaBox.y0, mediaBox.y1)
                );

            if (pageMediaBox.x1 - pageMediaBox.x0 < 1
                || pageMediaBox.y1 - pageMediaBox.y0 < 0)
            {
                pageMediaBox = new FzRect(FzRect.Fixed.Fixed_UNIT);
            }
            return new Rect(pageMediaBox);
        }

        public static FzMatrix DerotatePageMatrix(PdfPage page)
        {
            Matrix mp = RotatePageMatrix(page);
            return mp.ToFzMatrix().fz_invert_matrix();
        }

        public static int PageRotation(PdfPage page)
        {
            int rotate;
            if (page.obj() == null)
                Console.WriteLine(page.obj().ToString());
            PdfObj obj = page   .obj().pdf_dict_get(new PdfObj("Rotate"));
            rotate = obj.pdf_to_int();
            rotate = NormalizeRotation(rotate);
            return rotate;
        }

        public static int NormalizeRotation(int rotate)
        {
            while (rotate < 0)
            {
                rotate += 360;
            }
            while (rotate >= 360)
            {
                rotate -= 360;
            }
            while (rotate % 90 != 0)
            {
                return 0;
            }
            return rotate;
        }

        public static FzRect RectFromObj(dynamic r)
        {
            if (r is FzRect)
                return r;
            if (r is Rect)
                return r.ToFzRect();
            if (r.Length != 4)
                return new FzRect(FzRect.Fixed.Fixed_INFINITE);
            return new FzRect(
                (float)Convert.ToDouble(r[0]),
                (float)Convert.ToDouble(r[1]),
                (float)Convert.ToDouble(r[2]),
                (float)Convert.ToDouble(r[3])
                );
        }

        public static List<byte> ReadSamples(FzPixmap pixmap, int offset, int n)
        {
            List<byte> ret = new List<byte>();
            for (int i = 0; i < n; i++)
                ret.Add((byte)pixmap.fz_samples_get(offset + i));
            return ret;
        }

        public static Dictionary<List<byte>, int> ColorCount(FzPixmap pm, dynamic clip)
        {
            Dictionary<List<byte>, int> ret = new Dictionary<List<byte>, int>();
            int count = 0;
            FzIrect irect = pm.fz_pixmap_bbox();
            irect = irect.fz_intersect_irect(RectFromObj(clip));
            int stride = pm.fz_pixmap_stride();
            int width = irect.x1 - irect.x0;
            int height = irect.y1 - irect.y0;
            int n = pm.n();

            int substride = width * n;
            int s = stride * (irect.y0 - pm.y()) + (irect.x0 - pm.x()) * n;
            List<byte> oldPix = Utils.ReadSamples(pm, s, n);
            count = 0;
            if (irect.fz_is_empty_irect() != 0)
                return ret;
            List<byte> pixel = null;
            int c = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < n; i += substride)
                {
                    List<byte> newPix = Utils.ReadSamples(pm, s + j, n);
                    if (newPix != oldPix)
                    {
                        c = ret[pixel];
                        if (c != 0)
                        {
                            count += c;
                        }
                        ret[pixel] = count;
                        count = 1;
                        oldPix = newPix;
                    }
                    else
                        count += 1;
                }
                s += stride;
            }
            pixel = oldPix;
            c = ret[pixel];
            if (c != 0)
            {
                count += c;
            }
            ret[pixel] = count;

            return ret;
        }

        public static void GetWidgetProperties(PdfAnnot annot, Widget widget)
        {
            PdfObj annotObj = mupdf.mupdf.pdf_annot_obj(annot);
            PdfPage page = mupdf.mupdf.pdf_annot_page(annot);
            PdfDocument pdf = page.doc();
            PdfAnnot tw = annot;


        }

        public static void AddAnnotId(PdfAnnot annot, string stem)
        {
            PdfPage page = annot.pdf_annot_page();
            PdfObj annotObj = annot.pdf_annot_obj();
            List<string> names = GetAnnotIDList(page);
            int i = 0;
            string stemId = "";
            while (true)
            {
                stemId = $"{ANNOT_ID_STEM}-{stem}{i}";
                if (!names.Contains(stemId))
                    break;
                i += 1;
            }
            PdfObj name = mupdf.mupdf.pdf_new_string(stemId, (uint)stemId.Length);
            annotObj.pdf_dict_puts("NM", name);
            page.doc().m_internal.resynth_required = 0;
        }

        public static List<string> GetAnnotIDList(PdfPage page)
        {
            List<string> ids = new List<string>();
            PdfObj annots = page.obj().pdf_dict_get(new PdfObj("Annots"));
            if (annots == null)
                return ids;
            for (int i = 0; i < annots.pdf_array_len(); i++)
            {
                PdfObj annotObj = annots.pdf_array_get(i);
                PdfObj name = annotObj.pdf_dict_gets("NM");
                if (name != null)
                    ids.Add(name.pdf_to_text_string());
            }
            return ids;
        }

        public static byte[] ToByte(string s)
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            return utf8.GetBytes(s);
        }

        public static PdfObj EmbedFile(
            PdfDocument pdf,
            FzBuffer buf,
            string filename,
            string ufilename,
            string desc,
            int compress)
        {
            int len = 0;
            PdfObj val = pdf.pdf_new_dict(6);
            val.pdf_dict_put_dict(new PdfObj("CI"), 4);
            PdfObj ef = val.pdf_dict_put_dict(new PdfObj("EF"), 4);
            val.pdf_dict_put_text_string(new PdfObj("F"), filename);
            val.pdf_dict_put_text_string(new PdfObj("UF"), ufilename);
            val.pdf_dict_put_text_string(new PdfObj("Desc"), desc);
            val.pdf_dict_put(new PdfObj("Type"), new PdfObj("Filespec"));
            byte[] bs = Utils.ToByte("  ");

            IntPtr bufPtr = Marshal.AllocHGlobal(bs.Length);
            Marshal.Copy(bufPtr, bs, 0, bs.Length);
            SWIGTYPE_p_unsigned_char swigBuf = new SWIGTYPE_p_unsigned_char(bufPtr, false);

            PdfObj f = pdf.pdf_add_stream(
                mupdf.mupdf.fz_new_buffer_from_copied_data(swigBuf, (uint)bs.Length),
                new PdfObj(),
                0
                );
            Marshal.FreeHGlobal(bufPtr);

            ef.pdf_dict_put(new PdfObj("F"), f);
            Utils.UpdateStream(pdf, f, buf, compress);
            len = (int)buf.fz_buffer_storage(new SWIGTYPE_p_p_unsigned_char(IntPtr.Zero, false));
            f.pdf_dict_put_int(new PdfObj("DL"), len);
            f.pdf_dict_put_int(new PdfObj("Length"), len);
            PdfObj param = f.pdf_dict_put_dict(new PdfObj("Params"), 4);
            param.pdf_dict_put_int(new PdfObj("Size"), len);

            return val;
        }

        public static void MakeAnnotDA(PdfAnnot annot, int nCol, float[] col, string fontName, float fontSize)
        {
            string buf = "";
            if (nCol > 0)
                buf += "0 g ";
            else if (nCol == 1)
                buf += $"{col[0]:g} g ";
            else if (nCol == 2)
                Debug.Assert(false);
            else if (nCol == 3)
                buf += $"{col[0]:g} {col[1]:g} {col[2]:g} rg ";
            else
                buf += $"{col[0]:g} {col[1]:g} {col[2]:g} {col[3]:g} k ";
            buf += $"/{ExpandFileName(fontName)} {fontSize} Tf";
            annot.pdf_annot_obj().pdf_dict_put_text_string(new PdfObj("DA"), buf);
        }

        public static string ExpandFileName(string filename)
        {
            if (filename == null) return "Helv";
            if (filename.StartsWith("Co")) return "Cour";
            if (filename.StartsWith("co")) return "Cour";
            if (filename.StartsWith("Ti")) return "TiRo";
            if (filename.StartsWith("ti")) return "TiRo";
            if (filename.StartsWith("Sy")) return "Symb";
            if (filename.StartsWith("sy")) return "Symb";
            if (filename.StartsWith("Za")) return "ZaDb";
            if (filename.StartsWith("za")) return "ZaDb";
            return "Helv";
        }

        public static List<WordBlock> GetTextWords(
            MuPDFPage page,
            Rect clip = null,
            int flags = 0,
            MuPDFSTextPage stPage = null,
            bool sort = false,
            char[] delimiters = null
            )
        {
            if (flags == 0)
                flags = flags = (int)(TextFlags.TEXT_PRESERVE_WHITESPACE | TextFlags.TEXT_PRESERVE_LIGATURES | TextFlags.TEXT_MEDIABOX_CLIP);
            MuPDFSTextPage tp = stPage;
            if (tp == null)
                tp = page.GetSTextPage(clip, flags);
            else if (tp._parent != page)
                throw new Exception("not a textpage of this page");

            List<WordBlock> words = tp.ExtractWords(delimiters);
            if (stPage is null)
                tp.Dispose();
            if (sort)
                words.Sort((WordBlock w1, WordBlock w2) =>
                {
                    var result = w1.Y1.CompareTo(w2.Y1);
                    if (result == 0)
                    {
                        result = w1.X0.CompareTo(w2.X0);
                    }
                    return result;
                });
            return words;
        }

        public static dynamic GetText(
            MuPDFPage page,
            string option = "text",
            Rect clip = null,
            int flags = 0,
            MuPDFSTextPage stPage = null,
            bool sort = false,
            char[] delimiters = null
            )
        { 
            Dictionary<string, int> formats = new Dictionary<string, int>()
            {
                { "text", 0 },
                { "html", 1 },
                { "json", 1 },
                { "rawjson", 1 },
                { "xml", 0 },
                { "xhtml", 1 },
                { "dict", 1 },
                { "rawdict", 1 },
                { "words", 0 },
                { "blocks", 1 },
            };

            option = option.ToLower();
            if (!formats.Keys.Contains(option))
                option = "text";
            if (flags == 0)
            {
                flags = (int)(TextFlags.TEXT_PRESERVE_WHITESPACE | TextFlags.TEXT_PRESERVE_LIGATURES | TextFlags.TEXT_MEDIABOX_CLIP);
                if (formats[option] == 1)
                    flags = flags | (int)TextFlags.TEXT_PRESERVE_IMAGES;
            }

            if (option == "words")
            {
                return Utils.GetTextWords(
                    page,
                    clip,
                    flags,
                    stPage,
                    sort,
                    delimiters
                    );
            }
            
            Rect cb = null;
            if ((new List<string>() { "html", "xml", "xhtml" }).Contains(option))
                clip = page.CropBox;
            if (clip != null)
                cb = null;
            else if (page is MuPDFPage)
                cb = page.CropBox;
            if (clip == null)
                clip = page.CropBox;

            MuPDFSTextPage tp = stPage;
            if (tp is null)
                tp = page.GetSTextPage(clip, flags);
            else if (tp._parent != page)
                throw new Exception("not a textpage of this page");
            
            dynamic t = null;
            if (option == "json")
                t = tp.ExtractJSON(cb, sort);
            else if (option == "rawjson")
                t = tp.ExtractRawJSON(cb, sort);
            else if (option == "dict")
                t = tp.ExtractDict(cb, sort);
            else if (option == "rawdict")
                t = tp.ExtractRAWDict(cb, sort);
            else if (option == "html")
                t = tp.ExtractHtml();
            else if (option == "xml")
                t = tp.ExtractXML();
            else if (option == "xhtml")
                t = tp.ExtractText();

            if (stPage is null)
                tp.Dispose();
            return t;
        }

        public static void SetFieldType(PdfDocument doc, PdfObj annotObj, PdfWidgetType type)
        {
            PdfFieldType setBits = 0;
            PdfFieldType clearBits = 0;
            PdfObj typeName = null;

            if (type == PdfWidgetType.PDF_WIDGET_TYPE_BUTTON)
            {
                typeName = new PdfObj("Btn");
                setBits = PdfFieldType.PDF_BTN_FIELD_IS_PUSHBUTTON;
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON)
            {
                typeName = new PdfObj("Btn");
                clearBits = PdfFieldType.PDF_BTN_FIELD_IS_PUSHBUTTON;
                setBits = PdfFieldType.PDF_BTN_FIELD_IS_RADIO;
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX)
            {
                typeName = new PdfObj("Btn");
                clearBits = (PdfFieldType.PDF_BTN_FIELD_IS_PUSHBUTTON | PdfFieldType.PDF_BTN_FIELD_IS_RADIO);
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_TEXT)
            {
                typeName = new PdfObj("Tx");
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_LISTBOX)
            {
                typeName = new PdfObj("Ch");
                clearBits = PdfFieldType.PDF_CH_FIELD_IS_COMBO;
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX)
            {
                typeName = new PdfObj("Ch");
                setBits = PdfFieldType.PDF_CH_FIELD_IS_COMBO;
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_SIGNATURE)
            {
                typeName = new PdfObj("Sig");
            }

            if (typeName != null)
                annotObj.pdf_dict_put(new PdfObj("FT"), typeName);

            int bits = 0;
            if ((int)setBits != 0 || (int)setBits != 0)
            {
                bits = annotObj.pdf_dict_get_int(new PdfObj("Ff"));
                bits &= ~(int)clearBits;
                bits |= (int)setBits;
                annotObj.pdf_dict_put_int(new PdfObj("Ff"), bits);
            }

        }

        public static PdfAnnot CreateWidget(PdfDocument doc, PdfPage page, PdfWidgetType type, string fieldName)
        {
            int oldSigFlags = doc.pdf_trailer().pdf_dict_getp("Root/AcroForm/SigFlags").pdf_to_int();
            PdfAnnot annot = page.pdf_create_annot_raw(pdf_annot_type.PDF_ANNOT_WIDGET);
            PdfObj annotObj = annot.pdf_annot_obj();
            try
            {
                Utils.SetFieldType(doc, annotObj, type);
                annotObj.pdf_dict_put_text_string(new PdfObj("T"), fieldName);

                /*if (type == PdfWidgetType.PDF_WIDGET_TYPE_SIGNATURE)
                {
                    int sigFlags = oldSigFlags | (Utils.SigFlag_SignaturesExist | Utils.SigFlag_AppendOnly);
                    Utils.pdf_dict_putl(
                        doc.pdf_trailer(),
                        mupdf.mupdf.pdf_new_nt(sigFlags),
                        new string[]
                        {
                            "Root", "AcroForm", "SigFlags"
                        }
                    );
                }*/

                PdfObj form = doc.pdf_trailer().pdf_dict_getp("Root/AcroForm/Fields");
                if (form == null)
                {
                    form = doc.pdf_new_array(1);
                    Utils.pdf_dict_putl(
                        doc.pdf_trailer(),
                        form,
                        new string[]
                        {
                            "Root", "AcroForm", "Fields"
                        }
                        );
                }

                form.pdf_array_push(annotObj);
            }
            catch (Exception)
            {
                page.pdf_delete_annot(annot);

                if (type == PdfWidgetType.PDF_WIDGET_TYPE_SIGNATURE)
                {
                    Utils.pdf_dict_putl(
                        doc.pdf_trailer(),
                        mupdf.mupdf.pdf_new_int(oldSigFlags),
                        new string[]
                        {
                            "Root", "AcroForm", "SigFlags"
                        }
                        );
                }
            }
            return annot;
        }

        public static List<(string, int)> GetResourceProperties(PdfObj refer)
        {
            PdfObj properties = Utils.pdf_dict_getl(refer, new string[] { "Resource", "Properties" });
            List<(string, int)> rc = new List<(string, int)>();
            if (properties == null)
                return null;
            else
            {
                int n = properties.pdf_dict_len();
                if (n < 1)
                    return null;
                
                for (int i =0; i < n; i++)
                {
                    PdfObj key = properties.pdf_dict_get_key(i);
                    PdfObj val = properties.pdf_dict_get_val(i);
                    string c = key.pdf_to_name();
                    int xref = val.pdf_to_num();
                    rc.Add((c, xref));
                }
            }
            return rc;
        }

        public static void SetResourceProperty(PdfObj refer, string name, int xref)
        {
            PdfDocument pdf = refer.pdf_get_bound_document();
            PdfObj ind = pdf.pdf_new_indirect(xref, 0);

            if (ind == null)
                Console.WriteLine(Utils.ErrorMessages["MSG_BAD_XREF"]);

            PdfObj resource = refer.pdf_dict_get(new PdfObj("Resource"));
            if (resource == null)
                resource = refer.pdf_dict_put_dict(new PdfObj("Resources"), 1);
            
            PdfObj properties = resource.pdf_dict_get(new PdfObj("Properties"));
            if (properties == null)
                properties = resource.pdf_dict_put_dict(new PdfObj("Properties"), 1);

            properties.pdf_dict_put(mupdf.mupdf.pdf_new_name(name), ind);
        }

        public static int GenID()
        {
            UNIQUE_ID += 1;
            return UNIQUE_ID;
        }

        public static void EmbeddedClean(PdfDocument pdf)
        {
            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            PdfObj coll = root.pdf_dict_get(new PdfObj("Collection"));
            if (coll.m_internal != null && coll.pdf_dict_len() == 0)
            {
                root.pdf_dict_del(new PdfObj("Collection"));
            }

            PdfObj efiles = Utils.pdf_dict_getl(
                root,
                new string[]
                {
                    "Names", "EmbeddedFiles", "Names"
                }
                );
            if (efiles.m_internal != null)
                root.pdf_dict_put_name(new PdfObj("PageMode"), "UseAttachments");
        }

        public static string EnsureIdentity(MuPDFDocument pdf)
        {
            PdfObj id = MuPDFDocument.AsPdfDocument(pdf).pdf_trailer().pdf_dict_get(new PdfObj("ID"));
            if (id == null)
            {
                IntPtr p_block = Marshal.AllocHGlobal(16);
                SWIGTYPE_p_unsigned_char swigBlock = new SWIGTYPE_p_unsigned_char(p_block, true);
                mupdf.mupdf.fz_memrnd(swigBlock, 16);
                string rnd = Marshal.PtrToStringUTF8(p_block);
                Marshal.FreeHGlobal(p_block);

                id = mupdf.mupdf.pdf_dict_put_array(MuPDFDocument.AsPdfDocument(pdf).pdf_trailer(), new PdfObj("ID"), 2);
                id.pdf_array_push(mupdf.mupdf.pdf_new_string(rnd, 16));
                id.pdf_array_push(mupdf.mupdf.pdf_new_string(rnd, 16));

                return rnd;
            }

            return "";
        }

        public static FontStruct CheckFont(MuPDFPage page, string fontName)
        {
            foreach (List<dynamic> f in page.GetFonts())
            {
                if (f[4] == fontName)
                {
                    return new FontStruct()
                    {
                        Xref = f[0],
                        Ext = f[1],
                        Type = f[2],
                        Name = f[3],
                        RefName = f[4],
                        Encoding = f[5],
                        StreamXref = f[6]
                    };
                }
            }
            return null;
        }

        public static dynamic CheckFontInfo(MuPDFDocument doc, int xref)
        {
            foreach (FontStruct f in doc.FontInfo)
            {
                if (xref == f.Xref)
                    return f;
            }
            return null;
        }

        public static void ScanResources(PdfDocument pdf, PdfObj rsrc, List<List<dynamic>> liste, int what, int streamXRef, List<dynamic> tracer)
        {
            if (rsrc.pdf_mark_obj() != 0)
            {
                mupdf.mupdf.fz_warn("Circular dependencies! Consider page cleaning.");
                return;
            }
            try
            {
                PdfObj xObj = rsrc.pdf_dict_get(new PdfObj("XObject"));

                if (what == 1)
                {
                    PdfObj font = rsrc.pdf_dict_get(new PdfObj("Font"));
                    GatherFonts(pdf, font, liste, streamXRef);
                }
                else if (what == 2)
                {
                    GatherIamges(pdf, xObj, liste, streamXRef);
                }
                else if (what == 3)
                    GatherForms(pdf, xObj, liste, streamXRef);
                else
                    return;

                int n = xObj.pdf_dict_len();
                for (int i = 0; i <n; i++)
                {
                    PdfObj obj = xObj.pdf_dict_get_val(i);
                    int sxref = 0;
                    if (obj.pdf_is_stream() != 0)
                        sxref = obj.pdf_to_num();
                    else
                        sxref = 0;
                    PdfObj subrsrc = obj.pdf_dict_get(new PdfObj("Resources"));
                    if (subrsrc != null)
                    {
                        int sxref_t = sxref;
                        if (!tracer.Contains(sxref_t))
                        {
                            tracer.Add(sxref_t);
                            Utils.ScanResources(pdf, subrsrc, liste, what, streamXRef, tracer);
                        }
                        else
                        {
                            mupdf.mupdf.fz_warn("Circular dependencies! Consider page cleaning.");
                            return;
                        }
                    }
                }
            }
            finally
            {
                mupdf.mupdf.pdf_unmark_obj(rsrc);
            }
        }

        public static int GatherFonts(PdfDocument pdf, PdfObj dict, List<List<dynamic>> fontList, int streamXRef)
        {
            int rc = 1;
            int n = dict.pdf_dict_len();

            for (int i = 0; i < n; i ++)
            {
                PdfObj refName = dict.pdf_dict_get_key(i);
                PdfObj fontDict = dict.pdf_dict_get_val(i);
                if (fontDict.pdf_is_dict() == 0)
                {
                    mupdf.mupdf.fz_warn($"'{refName.pdf_to_name()}' is no font dict ({fontDict.pdf_to_num()} 0 R)");
                    continue;
                }

                PdfObj subType = fontDict.pdf_dict_get(new PdfObj("Subtype"));
                PdfObj baseFont = fontDict.pdf_dict_get(new PdfObj("Base"));
                PdfObj name = null;
                if (baseFont == null || baseFont.pdf_is_null() != 0)
                    name = fontDict.pdf_dict_get(new PdfObj("Name"));
                else
                    name = baseFont;
                PdfObj encoding = fontDict.pdf_dict_get(new PdfObj("Encoding"));
                if (encoding.pdf_is_dict() != 0)
                    encoding = encoding.pdf_dict_get(new PdfObj("BaseEncoding"));
                int xref = fontDict.pdf_to_num();
                string ext = "n/a";

                if (xref != 0)
                    ext = Utils.GetFontExtension(pdf, xref);

                List<dynamic> entry = new List<dynamic>{
                    xref,
                    ext,
                    subType.pdf_to_name(),
                    Utils.EscapeStrFromStr(name.pdf_to_name()),
                    refName.pdf_to_name(),
                    encoding.pdf_to_name(),
                    streamXRef
                    };
                fontList.Add( entry );
            }

            return rc;
        }

        public static string GetFontExtension(PdfDocument doc, int xref)
        {
            if (xref < 1)
                return "n/a";
            PdfObj o = mupdf.mupdf.pdf_load_object(doc, xref);
            PdfObj desft = o.pdf_dict_get(new PdfObj("DescendantFonts"));
            PdfObj obj = null;
            if (desft != null)
            {
                obj = desft.pdf_array_get(0).pdf_resolve_indirect();
                obj = obj.pdf_dict_get(new PdfObj("FontDescriptor"));
            }
            else
                obj = o.pdf_dict_get(new PdfObj("FontDescriptor"));
            if (obj == null)
                return "n/a";

            o = obj;
            obj = o.pdf_dict_get(new PdfObj("FontFile"));
            if (obj != null)
                return "pfa";

            obj = o.pdf_dict_get(new PdfObj("FontFile2"));
            if (obj != null)
                return "ttf";

            obj = o.pdf_dict_get(new PdfObj("FontFile3"));
            if (obj != null)
            {
                obj = obj.pdf_dict_get(new PdfObj("Subtype"));
                if (obj != null && obj.pdf_is_name() == 0)
                    return "n/a";
                if (obj.pdf_name_eq(new PdfObj("Type1C")) != 0)
                    return "cff";
                else if (obj.pdf_name_eq(new PdfObj("CIDFontType0C")) != 0)
                    return "cid";
                else if (obj.pdf_name_eq(new PdfObj("OpenType")) != 0)
                    return "otf";
                else
                    Console.WriteLine("unhandled font type '%s'", obj.pdf_to_name());
            }
            
            return "n/a";
        }

        public static string EscapeStrFromStr(string c)
        {
            if (c == null)
                return "";
            byte[] b = Encoding.UTF8.GetBytes(c);
            string ret = "";
            foreach (byte bb in b)
            {
                ret += (char)bb;
            }

            return ret;
        }

        public static int GatherForms(PdfDocument doc, PdfObj dict, List<List<dynamic>> imageList, int streamXRef)
        {
            int rc = 1;
            int n = dict.pdf_dict_len();
            for (int i =0; i < n; i++)
            {
                PdfObj refName = dict.pdf_dict_get_key(i);
                PdfObj imageDict = dict.pdf_dict_get_val(i);
                if (imageDict == null)
                {
                    mupdf.mupdf.fz_warn($"'{refName.pdf_to_name()}' is no form dict ({imageDict.pdf_to_num()} 0 R)");
                    continue;
                }

                PdfObj type = imageDict.pdf_dict_get(new PdfObj("BBox"));
                if (type.pdf_name_eq(new PdfObj("Form")) != 0)
                    continue;

                PdfObj o = imageDict.pdf_dict_get(new PdfObj("BBox"));
                PdfObj m = imageDict.pdf_dict_get(new PdfObj("Matrix"));
                FzMatrix mat = null;
                if (m != null)
                    mat = m.pdf_to_matrix();
                else
                    mat = new FzMatrix();

                FzRect bbox;
                if (o != null)
                    bbox = o.pdf_to_rect().fz_transform_rect(mat);
                else
                    bbox = new FzRect(FzRect.Fixed.Fixed_INFINITE);
                int xref = imageDict.pdf_to_num();

                List<dynamic> entry = new List<dynamic> {
                    xref,
                    refName.pdf_to_name(),
                    streamXRef,
                    bbox
                };
                imageList.Add(entry);
            }
            return rc;
        }

        public static int GatherIamges(PdfDocument doc, PdfObj dict, List<List<dynamic>> imageList, int streamXRef)
        {
            int rc = 1;
            int n = dict.pdf_dict_len();
            for (int i =0; i < n; i++)
            {
                PdfObj refName = dict.pdf_dict_get_key(i);
                PdfObj imageDict = dict.pdf_dict_get_val(i);
                if (imageDict.pdf_is_dict() == 0)
                {
                    mupdf.mupdf.fz_warn($"'{refName.pdf_to_name()}' is no image dict ({imageDict.pdf_to_name()} 0 R)");
                    continue;
                }

                PdfObj type = imageDict.pdf_dict_get(new PdfObj("Subtype"));
                if (type.pdf_name_eq(new PdfObj("Image")) == 0)
                    continue;

                int xref = imageDict.pdf_to_num();
                int gen = 0;
                PdfObj smask = imageDict.pdf_dict_geta(new PdfObj("SMask"), new PdfObj("Mask"));
                if (smask != null)
                    gen = smask.pdf_to_num();

                PdfObj filter = imageDict.pdf_dict_geta(new PdfObj("Filter"), new PdfObj("F"));
                if (filter.pdf_is_array() != 0)
                    filter = filter.pdf_array_get(0);

                PdfObj altcs = new PdfObj(0);
                PdfObj cs = imageDict.pdf_dict_geta(new PdfObj("ColorSpace"), new PdfObj("CS"));
                if (cs.pdf_is_array() != 0)
                {
                    PdfObj cses = new PdfObj(cs);
                    cs = cses.pdf_array_get(0);
                    if (cs.pdf_name_eq(new PdfObj("DeviceN")) != 0 || cs.pdf_name_eq(new PdfObj("Separation")) != 0)
                    {
                        altcs = cses.pdf_array_get(2);
                        if (altcs.pdf_is_array() != 0)
                            altcs = altcs.pdf_array_get(0);
                    }
                }

                PdfObj width = imageDict.pdf_dict_geta(new PdfObj("Width"), new PdfObj("W"));
                PdfObj height = imageDict.pdf_dict_geta(new PdfObj("Height"), new PdfObj("H"));
                PdfObj bpc = imageDict.pdf_dict_geta(new PdfObj("BitsPerComponent"), new PdfObj("BPC"));

                List<dynamic> entry = new List<dynamic> {
                    xref,
                    gen,
                    width.pdf_to_int(),
                    height.pdf_to_int(),
                    bpc.pdf_to_int(),
                    Utils.EscapeStrFromStr(cs.pdf_to_name()),
                    Utils.EscapeStrFromStr(altcs.pdf_to_name()),
                    Utils.EscapeStrFromStr(refName.pdf_to_name()),
                    Utils.EscapeStrFromStr(filter.pdf_to_name()),
                    streamXRef
                };
                imageList.Add(entry);
            }
            return rc;
        }

        public static int InsertContents(MuPDFPage page, byte[] newCont, int overlay = 1)
        {
            PdfPage pdfpage = page.GetPdfPage();
            FzBuffer contbuf = Utils.BufferFromBytes(newCont);
            int xref = Utils.InsertContents(pdfpage.doc(), pdfpage.obj(), contbuf, overlay);
            return xref;
        }

        public static int InsertContents(PdfDocument pdf, PdfObj pageRef, FzBuffer newcont, int overlay)
        {
            PdfObj contents = pageRef.pdf_dict_get(new PdfObj("Contents"));
            PdfObj newconts = pdf.pdf_add_stream(newcont, new PdfObj(), 0);
            int xref = newconts.pdf_to_num();
            if (contents.pdf_is_array() != 0)
            {
                if (overlay != 0)
                    contents.pdf_array_push(newconts);
                else
                    contents.pdf_array_insert(newconts, 0);
            }
            else
            {
                PdfObj carr = pdf.pdf_new_array(5);
                if (overlay != 0)
                {
                    if (contents != null)
                        carr.pdf_array_push(contents);
                    carr.pdf_array_push(newconts);
                }
                else
                {
                    carr.pdf_array_push(newconts);
                    if (contents != null)
                        carr.pdf_array_push(contents);
                }
                pageRef.pdf_dict_put(new PdfObj("Contents"), carr);
            }
            return xref;
        }

        public static (string, string, string, float, float)
        GetFontProperties(MuPDFDocument doc, int xref)
        {
            FontStruct res = doc.ExtractFont(xref);
            float asc = 0.8f;
            float dsc = -0.2f;
            
            if (res.Ext == "")
                return (res.Name, res.Ext, res.Type, asc, dsc);

            if (res.Content != null)
            {
                /*try
                {
                    Font font = new Font(res.Content);
                    asc = font.Ascender;
                    dsc = font.Descender;
                    Rect bbox = font.Bbox;
                    if (asc - dsc < 1)
                    {
                        if (bbox.Y0 < dsc)
                            dsc = bbox.Y0;
                        asc = 1 - dsc;
                    }
                }
                catch (Exception e)
                {
                    asc *= 1.2f;
                    dsc *= 1.2f;
                }
                return (res.Name, res.Ext, res.Type, asc, dsc);*/
            }
            if (res.Ext != "n/a")
            {
                try
                {
                    Font font = new Font(res.Name);
                    asc = font.Ascender;
                    dsc = font.Descender;
                }
                catch (Exception)
                {
                    asc *= 1.2f;
                    dsc *= 1.2f;
                }
            }
            else
            {
                asc *= 1.2f;
                dsc *= 1.2f;
            }
            return (res.Name, res.Ext, res.Type, asc, dsc);
        }

        public static FzBuffer GetFontBuffer(PdfDocument doc, int xref)
        {
            if (xref < 1)
                return null;

            PdfObj o = doc.pdf_load_object(xref);
            PdfObj desft = o.pdf_dict_get(new PdfObj("DescendantFonts"));
            PdfObj obj;
            if (desft.m_internal != null)
            {
                obj = desft.pdf_array_get(0).pdf_resolve_indirect();
                obj = obj.pdf_dict_get(new PdfObj("FontDescriptor"));
            }
            else
            {
                obj = o.pdf_dict_get(new PdfObj("FontDescriptor"));
            }

            if (obj.m_internal == null)
            {
                Console.WriteLine("invalid font - FontDescriptor missing");
                return null;
            }

            o = obj;
            PdfObj stream = null;
            obj = o.pdf_dict_get(new PdfObj("FontFile"));
            if (obj.m_internal_value() != 0)
                stream = obj; // pfa

            obj = o.pdf_dict_get(new PdfObj("FontFile2"));
            if (obj.m_internal != null)
                stream = obj; // ttf

            obj = o.pdf_dict_get(new PdfObj("FontFile3"));
            if (obj.m_internal != null)
            {
                stream = obj;
                obj = obj.pdf_dict_get(new PdfObj("Subtype"));
                if (obj != null && obj.pdf_is_name() == 0)
                {
                    Console.WriteLine("invalid font descriptor subtype");
                    return null;
                }

                if (obj.pdf_name_eq(new PdfObj("Type1C")) != 0) { } // cff
                else if (obj.pdf_name_eq(new PdfObj("CIDFontType0C")) != 0) { } //cid
                else if (obj.pdf_name_eq(new PdfObj("OpenType")) != 0) { } // otf
                else Console.WriteLine("warning: unhandled font type {pdf_to_name(ctx, obj)!r}");
            }

            if (stream == null)
            {
                Console.WriteLine("warning: unhandled font type");
                return null;
            }

            return stream.pdf_load_stream();
        }

        public static string UnicodeFromStr(dynamic s)
        {
            if (s == null)
                return "";

            if (s is byte[])
                return Encoding.UTF8.GetString(s);

            if (s is string)
                return s;

            return null;
        }

        public static void UpdateFontInfo(MuPDFDocument doc, FontStruct info)
        {
            int xref = info.Xref;
            bool found = false;

            int i = 0;
            for (; i < doc.FontInfo.Count; i ++)
            {
                if (doc.FontInfo[i].Xref == xref)
                {
                    found = true;
                    break;
                }
            }
            if (found)
                doc.FontInfo[i] = info;
            else
                doc.FontInfo.Add(info);
        }

        public static List<(int, double)> GetCharWidths(
            MuPDFDocument doc,
            int xref,
            int limit = 256,
            int idx = 0,
            FontStruct fontDict = null
            )
        {
            FontStruct fontStruct = Utils.CheckFontInfo(doc, xref);
            string name = "";
            string ext = "";
            string stype = "";
            float asc = 0.0f;
            float dsc = 0.0f;
            bool simple = false;
            int ordering = 0;
            List<(int, double)> glyphs = null;

            if (fontStruct == null)
            {
                if (fontDict == null)
                {
                    (name, ext, stype, asc, dsc) = Utils.GetFontProperties(doc, xref);
                    fontStruct.Name = name;
                    fontStruct.Ext = ext;
                    fontStruct.Type = stype;
                    fontStruct.Ascender = asc;
                    fontStruct.Descender = dsc;
                }
                else
                {
                    name = fontDict.Name;
                    ext = fontDict.Ext;
                    stype = fontDict.Type;
                    ordering = fontDict.Ordering;
                    simple = fontDict.Simple;
                }

                if (ext == "")
                    throw new Exception("xref is not a font");

                if (stype == "Type1" || stype == "MMType1" || stype == "TrueType")
                    simple = true;
                else
                    simple = false;

                if (name == "Fangti" || name == "Ming")
                    ordering = 0;
                else if (name == "Heiti" || name == "Song")
                    ordering = 1;
                else if (name == "Gothic" || name == "Mincho")
                    ordering = 2;
                else if (name == "Dotum" || name == "Batang")
                    ordering = 3;
                else
                    ordering = -1;

                fontDict.Simple = simple;

                if (name == "ZapfDingbats")
                    glyphs = new List<(int, double)>(Utils.zapf_glyphs);
                else if (name == "Symbol")
                    glyphs = new List<(int, double)>(Utils.symbol_glyphs);
                else
                    glyphs = null;

                fontDict.Glyphs = glyphs;
                fontDict.Ordering = ordering;
                fontDict.Xref = xref;
                doc.FontInfo.Add(fontDict);
            }
            else
            {
                fontDict = fontStruct;
                glyphs = new List<(int, double)>(fontDict.Glyphs);
                simple = fontDict.Simple;
                ordering = fontDict.Ordering;
            }

            int oldLimit = 0;
            if (glyphs != null)
                oldLimit = glyphs.Count;
            int myLimit = Math.Max(256, limit);
            if (myLimit <= oldLimit)
                return glyphs;

            if (ordering < 0)
                glyphs = doc._GetCharWidths(xref, fontDict.Name, fontDict.Ext, fontDict.Ordering, myLimit, idx);
            else
                glyphs = null;

            fontDict.Glyphs = glyphs;
            Utils.UpdateFontInfo(doc, fontDict);

            return glyphs;
        }

        public static FontStruct InsertFont(PdfDocument pdf, string bfName, string fontFile, byte[] fontBuffer, bool setSample, int idx, int wmode, int serif, int encoding, int ordering)
        {
            FzFont font = new FzFont();
            FzBuffer res = null;
            SWIGTYPE_p_unsigned_char data = null;
            int ixref = 0;
            int simple = 0;
            FontStruct value = null;
            string name = null;
            string subt = null;
            string exto = null;
            ll_fz_lookup_cjk_font_outparams cjk_params = new ll_fz_lookup_cjk_font_outparams();
            PdfObj fontObj = null;

            if (ordering > -1)
                data = mupdf.mupdf.ll_fz_lookup_cjk_font_outparams_fn(ordering, cjk_params);
            if (data != null)
            {
                font = mupdf.mupdf.fz_new_font_from_memory(null, data, cjk_params.len, cjk_params.index, 0);
                fontObj = pdf.pdf_add_simple_font(font, encoding);
                exto = "n/a";
                simple = 1;
            }
            else
            {
                if (fontFile != null)
                {
                    font = mupdf.mupdf.fz_new_font_from_file(null, fontFile, idx, 0);
                }
                else
                {
                    res = Utils.BufferFromBytes(fontBuffer);
                    if (res == null)
                        throw new Exception(Utils.ErrorMessages["MSG_FILE_OR_BUFFER"]);
                    font = mupdf.mupdf.fz_new_font_from_buffer(null, res, idx, 0);
                }

                if (setSample)
                {
                    fontObj = mupdf.mupdf.pdf_add_cid_font(pdf, font);
                    simple = 0;
                }
                else
                {
                    fontObj = pdf.pdf_add_simple_font(font, encoding);
                    simple = 2;
                }
            }
            ixref = fontObj.pdf_to_num();
            name = Utils.EscapeStrFromStr(fontObj.pdf_dict_get(new PdfObj("BaseFont")).pdf_to_name());
            subt = Utils.UnicodeFromStr(fontObj.pdf_dict_get(new PdfObj("Subtype")).pdf_to_name());
            
            if (exto == null)
                exto = Utils.GetFontExtension(pdf, ixref);
            
            float asc = font.fz_font_ascender();
            float dsc = font.fz_font_descender();

            value = new FontStruct()
            {
                Xref = ixref,
                Name = name,
                Type = subt,
                Ext = exto,
                Simple = simple != 0,
                Ordering = ordering,
                Ascender = asc,
                Descender = dsc
            };

            return value;
        }

        public static string GetTJstr(string text, List<(int, double)> glyphs, bool simple, int ordering)
        {
            if (text.StartsWith("[<") && text.EndsWith(">]"))
                return text;
            if (text == "" || text == null)
                return "[<>]";
            
            string otxt = "";
            if (simple)
            {
                if (glyphs == null)
                {
                    foreach (char c in text.ToCharArray())
                    {
                        if (Convert.ToInt32(c) < 256)
                            otxt += Convert.ToInt32(c).ToString("x2");
                        else
                            otxt += Convert.ToInt32("b7", 16).ToString("x2");
                    }
                }
                else
                {
                    foreach (char c in text.ToCharArray())
                    {
                        if (Convert.ToInt32(c) < 256)
                            otxt += (glyphs[Convert.ToInt32(c)].Item1).ToString("x2");
                        else
                            otxt += (glyphs[Convert.ToInt32("b7", 16)].Item1).ToString("x2");
                    }
                }
                return $"[<{otxt}>]";
            }
            if (ordering < 0)
            {
                foreach (char c in text.ToCharArray())
                    otxt += (glyphs[Convert.ToInt32(c)].Item1).ToString("x4");
            }
            else
            {
                foreach (char c in text.ToCharArray())
                    otxt += Convert.ToInt32(c).ToString("x4");
            }
            return $"[<{otxt}>]";
        }

        public static void CheckColor(dynamic c)
        {
            if (c != null || c != 0)
            {
                if (!(c is List<dynamic>) || !(c is Tuple)
                    || (c.Count != 1) || (c.Count != 3) || (c.Count != 4)
                    || c.Min() < 0 || c.Max() > 1)
                {
                    throw new Exception("need 1, 3 or 4 color components in range 0 to 1");
                }
            }
        }

        public static string GetColorCode(dynamic c, string f)
        {
            if (c == null)
                return "";
            if (c is float)
                c = new List<float>() { c, };
            Utils.CheckColor(c);
            string s = "";
            if (c.Count == 1)
            {
                s = $"{c[0]} ";
                return s + (f == "c" ? "G " : "g ");
            }

            if (c.Count == 3)
            {
                s = $"{c[0]} {c[1]} {c[2]} ";
            }

            s = $"{c[0]} {c[1]} {c[2]} {c[3]} ";
            return s + (f == "c" ? "K " : "k ");
        }

        public static bool CheckMorph(dynamic o)
        {
            try
            {
                if (Convert.ToBoolean(o) == false)
                    return false;
                if (o is List<dynamic> || o is Tuple || o.Count == 2)
                    throw new Exception("morph must be a sequence of length 2");
                if (!(o[0].Count == 2 && o[1].Count == 6))
                    throw new Exception("invalid morph parm 0");
                if (!(o[1][4] == o[1][5] && o[1][5] == 0))
                    throw new Exception("invalid morph parm 1");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string EscapeStrFromBuffer(FzBuffer buf)
        {
            if (buf.m_internal == null)
                return "";
            FzBuffer s = buf.fz_clone_buffer();
            return DecodeRawUnicodeEscape(s);
        }

        /// <summary>
        /// Decode Raw Unicode
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string DecodeRawUnicodeEscape(string s)
        {
            return System.Text.RegularExpressions.Regex.Unescape(s);
        }

        /// <summary>
        /// Decode Raw Unicode
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string DecodeRawUnicodeEscape(FzBuffer s)
        {
            string ret = s.fz_string_from_buffer();
            return DecodeRawUnicodeEscape(ret);
        }

        public static FzBuffer Object2Buffer(PdfObj what, int compress, int ascii)
        {
            FzBuffer res = mupdf.mupdf.fz_new_buffer(512);
            FzOutput output = new FzOutput(res);
            output.pdf_print_obj(what, compress, ascii);
            res.fz_terminate_buffer();

            return res;
        }

        public static string UnicodeFromBuffer(FzBuffer buf)
        {
            byte[] bufBytes = buf.fz_buffer_extract();
            string val = Encoding.UTF8.GetString(bufBytes);
            int z = val.IndexOf((char)0);

            if (z >= 0)
                val = val.Substring(0, z);
            return val;
        }

        public static void Recurse(MuPDFDocument doc, Outline olItem, List<dynamic> liste, int lvl, bool simple)
        {
            int page = 0;
            while (olItem != null && !olItem.IsExternal)
            {
                string title = "";
                if (olItem.Title != null)
                    title = olItem.Title;
                else
                    title = " ";

                if (!olItem.IsExternal)
                {
                    if (olItem.Uri != null)
                    {
                        if (olItem.Page == -1)
                        {
                            var resolve = doc.ResolveLink(olItem.Uri);
                            page = resolve.Item1[0] + 1;
                        }
                        else
                            page = olItem.Page + 1;
                    }
                    else
                        page = -1;
                }
                else
                    page = -1;

                if (!simple)
                {

                }
            }
        }

        /*public static void GetLinkDict(dynamic ln, MuPDFDocument document = null)
        {
            if (ln is Outline)

        }*/

        public static BorderStruct GetAnnotBorder(PdfObj annotObj)
        {
            List<int> dash = new List<int>();
            float width = -1;
            float clouds = -1;
            PdfObj obj = null;
            string style = null;

            obj = annotObj.pdf_dict_get(new PdfObj("Border"));
            if (obj.pdf_is_array() != 0)
            {
                width = obj.pdf_array_get(2).pdf_to_real();
                if (obj.pdf_array_len() == 4)
                {
                    PdfObj dashObj = obj.pdf_array_get(3);
                    for (int i = 0; i < dashObj.pdf_array_len(); i ++)
                    {
                        int val = dashObj.pdf_array_get(i).pdf_to_int();
                        dash.Add(val);
                    }
                }
            }

            PdfObj bsObj = annotObj.pdf_dict_get(new PdfObj("BS"));
            if (bsObj != null)
            {
                width = bsObj.pdf_dict_get(new PdfObj("W")).pdf_to_real();
                style = bsObj.pdf_dict_get(new PdfObj("S")).pdf_to_name();
                if (style == "")
                    style = null;
                obj = bsObj.pdf_dict_get(new PdfObj("D"));
                if (obj != null)
                {
                    for (int i = 0; i < obj.pdf_array_len(); i ++)
                    {
                        int val = obj.pdf_array_get(i).pdf_to_int();
                        dash.Add(val);
                    }
                }
            }

            obj = annotObj.pdf_dict_get(new PdfObj("BE"));
            if (obj != null)
                clouds = obj.pdf_dict_get(new PdfObj("I")).pdf_to_int();

            BorderStruct res = new BorderStruct();
            res.Width = width;
            res.Dashes = dash.ToArray();
            res.Style = style;
            res.Clouds = clouds;

            return res;
        }

        public static ColorStruct GetAnnotColors(PdfObj annotObj)
        {
            ColorStruct res = new ColorStruct();
            List<float> bc = new List<float>();
            List<float> fc = new List<float>();

            PdfObj obj = annotObj.pdf_dict_get(new PdfObj("C"));
            if (obj.pdf_is_array() != 0)
            {
                int n = obj.pdf_array_len();
                for (int i =0; i < n; i ++)
                {
                    float col = obj.pdf_array_get(i).pdf_to_real();
                    bc.Add(col);
                }
            }
            res.Stroke = bc.ToArray();

            obj = annotObj.pdf_dict_gets("IC");
            if (obj.pdf_is_array() != 0)
            {
                int n = obj.pdf_array_len();
                for (int i = 0; i < n; i++)
                {
                    float col = obj.pdf_array_get(i).pdf_to_real();
                    fc.Add(col);
                }
            }
            res.Fill = fc.ToArray();

            return res;
        }

        public static void SetAnnotBorder(BorderStruct border, PdfDocument pdf, PdfObj linkObj)
        {
            PdfObj obj = null;
            int dashLen = 0;
            float nWidth = border.Width;
            int[] nDashes = border.Dashes;
            string nStyle = border.Style;
            float nClouds = border.Clouds;

            // get old border properties
        }

        public static List<string> GetAnnotIdList(PdfPage page)
        {
            List<string> names = new List<string>();
            PdfObj annots = page.obj().pdf_dict_get(new PdfObj("Annots"));
            if (annots == null)
                return null;

            int n = annots.pdf_array_len();
            for (int i =0; i < n; i ++)
            {
                PdfObj annotObj = annots.pdf_array_get(i);
                PdfObj name = annotObj.pdf_dict_gets("NM");
                if (name != null)
                    names.Add(name.pdf_to_text_string());
            }

            return names;
        }

        public static List<(int, pdf_annot_type, string)> GetAnnotXrefList(PdfObj pageObj)
        {
            List<(int, pdf_annot_type, string)> names = new List<(int, pdf_annot_type, string)>();
            PdfObj annots = pageObj.pdf_dict_get(new PdfObj("Annots"));
            if (annots == null)
                return null;

            int n = annots.pdf_array_len();
            for (int i = 0; i < n; i++)
            {
                PdfObj annotObj = annots.pdf_array_get(i);
                int xref = annotObj.pdf_to_num();
                PdfObj subtype = annotObj.pdf_dict_get(new PdfObj("Subtype"));
                if (subtype == null)
                    continue;

                pdf_annot_type type = mupdf.mupdf.pdf_annot_type_from_string(subtype.pdf_to_name());
                if (type == pdf_annot_type.PDF_ANNOT_UNKNOWN)
                    continue;
                PdfObj id_ = annotObj.pdf_dict_gets("NM");
                names.Add((xref, type, id_.pdf_to_text_string()));
            }
            return names;
        }

        public static (int, int, int) sRGB2rgb(int srgb)
        {
            int r = srgb >> 16;
            int g = (srgb - (r << 16) >> 8);
            int b = srgb - (r << 16) - (g << 8);
            return (r, g, b);
        }

        public static string GetLinkText(MuPDFPage page, LinkStruct link)
        {
            Matrix ctm = page.TransformationMatrix;
            Matrix ictm = ~ctm;
            Rect r = link.From * ictm;
            string rectStr = $"{r.X0} {r.Y0} {r.X1} {r.Y1}";
            string txt;

            string annot = "";
            if (link.Kind == LinkType.LINK_GOTO)
                if (link.Page >= 0)
                {
                    txt = Utils.AnnotSkel["goto2"];
                    int pno = link.Page;
                    int xref = page.Parent.GetPageXref(pno);
                    Point pnt = link.To == null ? new Point(0, 0) : link.To;
                    Point ipnt = pnt * ictm;
                    annot = string.Format(txt, xref, ipnt.X, ipnt.Y, link.Zoom, rectStr);
                }
                else
                {
                    txt = Utils.AnnotSkel["goto2"];
                    // annot = string.Format(txt, Utils.GetPdfStr(link.To), rectStr);//issue force point to string
                }
            else if (link.Kind == LinkType.LINK_GOTOR)
            {
                txt = Utils.AnnotSkel["goto2"];
                if (link.Page >= 0)
                {
                    txt = Utils.AnnotSkel["gotor1"];
                    Point pnt = link.To;
                    annot = string.Format(txt, link.Page, pnt.X, pnt.Y, link.Zoom, link.File, link.File, rectStr);
                }
                else
                {
                    txt = Utils.AnnotSkel["gotor2"];
                    // annot = string.Format(txt, Utils.GetPdfStr(link.To), link.File rectStr);//issue force point to string
                }
            }
            else if (link.Kind == LinkType.LINK_LAUNCH)
            {
                txt = Utils.AnnotSkel["launch"];
                annot = string.Format(txt, link.File, link.File, rectStr);
            }
            else if (link.Kind == LinkType.LINK_URI)
            {
                txt = Utils.AnnotSkel["uri"];
                annot = string.Format(txt, link.Uri, rectStr);
            }
            else if (link.Kind == LinkType.LINK_NAMED)
            {
                txt = Utils.AnnotSkel["named"];
                annot = string.Format(txt, link.Name, rectStr);
            }
            if (annot == null)
                return annot;

            Dictionary<int, string> linkNames = new Dictionary<int, string>();
            foreach ((int, pdf_annot_type, string) x in page.GetAnnotXrefs())
            {
                if (x.Item2 == pdf_annot_type.PDF_ANNOT_LINK)
                    linkNames.Add(x.Item1, x.Item3);
            }

            string oldName = link.Id;
            string name;
            if (oldName != null && linkNames.Contains(new KeyValuePair<int, string>(link.Xref, oldName)))
                name = oldName;
            else
            {
                int i = 0;
                string stem = Utils.SetAnnotStem() + "-L{0}";
                while (true)
                {
                    name = string.Format(stem, i);
                    if (!linkNames.Values.Contains(name))
                        break;
                    i += 1;
                }
            }
            annot = annot.Replace("/Link", $"/Link/NM({name})");
            return annot;
        }

        public static string GetPdfStr(string s)
        {
            if (!Convert.ToBoolean(s))
                return "()";

            string MakeUtf16be(string s)
            {
                byte[] r = new byte[] { 254, 255 };
                byte[] sBytes = Encoding.BigEndianUnicode.GetBytes(s);
                byte[] combined = new byte[r.Length + sBytes.Length];
                Buffer.BlockCopy(r, 0, combined, 0, r.Length);
                Buffer.BlockCopy(sBytes, 0, combined, r.Length, sBytes.Length);

                StringBuilder hex = new StringBuilder(combined.Length * 2);
                foreach (byte b in combined)
                {
                    hex.AppendFormat("{0:x2}", b);
                }
                return "<" + hex.ToString() + ">";
            }

            string r = "";
            foreach (char c in s)
            {
                int oc = Convert.ToInt32(c);
                if (oc > 255)
                    return MakeUtf16be(s);
                if (oc > 31 && oc < 127)
                {
                    if (c == '(' || c == ')' || c == '\\')
                    {
                        r += '\\';
                    }
                    r += c;
                    continue;
                }

                if (oc < 127)
                {
                    r += string.Format("\\{0:D3}", oc);
                }

                if (oc == 8)
                    r += "\\b";
                else if (oc == 9)
                    r += "\\t";
                else if (oc == 10)
                    r += "\\n";
                else if (oc == 12)
                    r += "\\f";
                else if (oc == 13)
                    r += "\\r";
                else
                    r += "\\267";
            }
            return "(" + r + ")";
        }

        public static string SetAnnotStem(string stem = null)
        {
            if (stem == null)
                return Utils.ANNOT_ID_STEM;
            int len = stem.Length + 1;
            if (len > 50)
                len = 50;
            Utils.ANNOT_ID_STEM = stem.Substring(0, 50);
            return Utils.ANNOT_ID_STEM;
        }

        public static PdfAnnot GetAnnotByName(MuPDFPage page, string name)
        {
            if (name == null)
                return null;
            PdfAnnot annot = mupdf.mupdf.pdf_first_annot(page.GetPdfPage());
            bool found = false;
            while (true)
            {
                if (annot == null)
                    break;
                (string res, ulong len) = annot.pdf_annot_obj().pdf_to_string();
                if (name == res)
                {
                    found = true;
                    break;
                }
                annot = annot.pdf_next_annot();
            }
            if (!found)
                throw new Exception($"'{name}' is not an annot of this page");
            return annot;
        }

        public static PdfAnnot GetAnnotByXref(MuPDFPage page, int xref)
        {
            bool found = false;
            PdfAnnot annot = page.GetPdfPage().pdf_first_annot();
            while (true)
            {
                if (annot == null)
                    break;
                if (xref == annot.pdf_annot_obj().pdf_to_num())
                {
                    found = true;
                    break;
                }
                annot = annot.pdf_next_annot();
            }
            if (!found)
                throw new Exception($"xref {xref} is not an annot of this page");
            return annot;
        }

        public static void StoreShrink(int percent)
        {
            if (percent >= 100)
            {
                mupdf.mupdf.fz_empty_store();
                return;
            }
            if (percent > 0)
                mupdf.mupdf.fz_shrink_store((uint)(100 - percent));
        }

        public static void RefreshLinks(PdfPage page)
        {
            if (page == null)
                return;
            PdfObj obj = page.obj().pdf_dict_get(new PdfObj("Annots"));
            if (obj != null)
            {
                PdfDocument pdf = page.doc();
                int number = pdf.pdf_lookup_page_number(page.obj());
                FzRect pageMediabox = new FzRect();
                FzMatrix pageCtm = new FzMatrix();
                page.pdf_page_transform(pageMediabox, pageCtm);
                FzLink link = pdf.pdf_load_link_annots(page, obj, number, pageCtm);
                page.m_internal.links = mupdf.mupdf.ll_fz_keep_link(link.m_internal);
            }
        }
        public static void PageMerge(MuPDFDocument docDes, MuPDFDocument docSrc, int pageFrom, int pageTo, int rotate,
            bool links, bool copyAnnots, MuPDFGraftMap graftmap)
        {
            List<PdfObj> knownPageObjs = new List<PdfObj>()
            {
                new PdfObj("Contents"),
                new PdfObj("Resources"),
                new PdfObj("MediaBox"),
                new PdfObj("CropBox"),
                new PdfObj("BleedBox"),
                new PdfObj("TrimBox"),
                new PdfObj("ArtBox"),
                new PdfObj("Rotate"),
                new PdfObj("UserUnit")
            };

            PdfObj pageRef = docSrc.PdfDocument.pdf_lookup_page_obj(pageFrom);
            PdfObj pageDict = docDes.PdfDocument.pdf_new_dict(4);
            pageDict.pdf_dict_put(new PdfObj("Type"), new PdfObj("Page"));

            foreach (PdfObj e in knownPageObjs)
            {
                PdfObj obj = pageRef.pdf_dict_get_inheritable(e);
                if (obj != null)
                {
                    pageDict.pdf_dict_put(e, mupdf.mupdf.pdf_graft_mapped_object(graftmap.ToPdfGraftMap(), obj));
                }
            }

            if (copyAnnots)
            {
                PdfObj oldAnnots = pageRef.pdf_dict_get(new PdfObj("Annots"));
                int n = oldAnnots.pdf_array_len();
                if (n > 0)
                {
                    PdfObj newAnnots = pageDict.pdf_dict_put_array(new PdfObj("Annots"), n);
                    for (int i = 0; i < n; i ++)
                    {
                        PdfObj o = oldAnnots.pdf_array_get(i);
                        if (o == null || o.pdf_is_dict() != 0)
                            continue;
                        if (o.pdf_dict_gets("IRT") != null)
                            continue;
                        PdfObj subtype = o.pdf_dict_get(new PdfObj("Subtype"));
                        if (subtype.pdf_name_eq(new PdfObj("Link")) != 0)
                            continue;
                        if (subtype.pdf_name_eq(new PdfObj("Popup")) != 0)
                            continue;
                        if (subtype.pdf_name_eq(new PdfObj("Widget")) != 0)
                        {
                            mupdf.mupdf.fz_warn("skipping widget annotation");
                            continue;
                        }

                        o.pdf_dict_del(new PdfObj("Popup"));
                        o.pdf_dict_del(new PdfObj("P"));
                        PdfObj copyO = graftmap.ToPdfGraftMap().pdf_graft_mapped_object(o);
                        PdfObj annot = docDes.PdfDocument.pdf_new_indirect(copyO.pdf_to_num(), 0);
                        newAnnots.pdf_array_push(annot);
                    }
                }
            }
            if (rotate != -1)
                pageDict.pdf_dict_put_int(new PdfObj("Rotate"), rotate);
            PdfObj ref_ = docDes.PdfDocument.pdf_add_object(pageDict);
            docDes.PdfDocument.pdf_insert_page(pageTo, ref_);
        }

        public static void MergeRange(MuPDFDocument docDes, MuPDFDocument docSrc, int spage, int epage, int apage, int rotate,
            bool links, bool annots, int showProgress, MuPDFGraftMap graftmap)
        {
            int afterPage = apage;
            int counter = 0;
            int total = mupdf.mupdf.fz_absi(epage - spage) + 1;

            if (spage < epage)
            {
                int page = spage;
                while (page <= epage)
                {
                    Utils.PageMerge(docDes, docSrc, page, afterPage, rotate, links, annots, graftmap);
                    counter += 1;
                    if (showProgress > 0 && counter % showProgress == 0)
                        Console.WriteLine(string.Format("Inserted {0} of {1} pages", counter, total));
                    page += 1;
                    afterPage += 1;
                }
            }
            else
            {
                int page = spage;
                while (page >= epage)
                {
                    Utils.PageMerge(docDes, docSrc, page, afterPage, rotate, links, annots, graftmap);
                    counter += 1;
                    if (showProgress > 0 && counter % showProgress == 0)
                        Console.WriteLine(string.Format("Inserted {0} of {1} pages", counter, total));
                    page -= 1;
                    afterPage += 1;
                }
            }
        }

        public static void DoLinks(MuPDFDocument doc1, MuPDFDocument doc2, int fromPage = -1, int toPage = -1, int startAt = -1)
        {
            string CreateAnnot(LinkStruct link, List<int> xrefDest, List<int> pnoSrc, Matrix ctm)
            {
                Rect r = link.From * ctm;
                string rStr = string.Format("{0} {1} {2} {3}", r[0], r[1], r[2], r[3]);
                string annot = "";
                if (link.Kind == LinkType.LINK_GOTO)
                {
                    string txt = Utils.AnnotSkel["goto1"];
                    int idx = pnoSrc.IndexOf(link.Page);
                    Point p = link.To * ctm;
                    annot = string.Format(txt, xrefDest[idx], p.X, p.Y, link.Zoom, rStr);
                }
                else if (link.Kind == LinkType.LINK_GOTOR)
                {
                    if (link.Page >= 0)
                    {
                        string txt = Utils.AnnotSkel["gotor1"];
                        Point pnt = link.To == null ? new Point(0, 0) : link.To;
                        annot = string.Format(txt, link.Page, pnt.X, pnt.Y, link.Zoom, link.File, link.File, rStr);
                    }
                    else
                    {
                        string txt = Utils.AnnotSkel["gotor2"];
                        string to = ""; // Utils.GetPdfStr(link.To); // issue
                        to = to.Substring(1, -1);
                        string f = link.File;
                        annot = string.Format(txt, to, f, rStr);
                    }
                }
                else if (link.Kind == LinkType.LINK_LAUNCH)
                {
                    string txt = Utils.AnnotSkel["launch"];
                    annot = string.Format(txt, link.File, link.File, rStr);
                }
                else if (link.Kind == LinkType.LINK_URI)
                {
                    string txt = Utils.AnnotSkel["uri"];
                    annot = string.Format(txt, link.Uri, rStr);
                }
                else annot = "";

                return annot;
            }
            // --------------------validate & normalize parameters-------------------------
            int fp, tp;
            if (fromPage < 0)
                fp = 0;
            else if (fromPage >= doc2.GetPageCount())
                fp = doc2.GetPageCount() - 1;
            else fp = fromPage;

            if (toPage < 0 || toPage >= doc2.GetPageCount())
                tp = doc2.GetPageCount() - 1;
            else
                tp = toPage;

            if (startAt < 0)
                throw new Exception("'start_at' must be >= 0");
            int sa = startAt;
            int incr = fp <= tp ? 1 : -1;

            // lists of source / destination page numbers
            List<int> pnoSrc = new List<int>();
            List<int> pnoDst = new List<int>();
            for (int i = fp; i < tp + incr; i += incr)
                pnoSrc.Add(i);
            for (int i = 0; i < pnoSrc.Count; i++)
                pnoDst.Add(sa + i);

            List<int> xrefSrc = new List<int>();
            List<int> xrefDst = new List<int>();
            for (int i = 0; i < pnoSrc.Count; i ++)
            {
                int pSrc = pnoSrc[i];
                int pDst = pnoDst[i];
                int oldXref = doc2.GetPageXref(pSrc);
                int newXref = doc1.GetPageXref(pDst);
                xrefSrc.Add(oldXref);
                xrefDst.Add(newXref);
            }

            // create the links for each copied page in destination PDF
            for (int i = 0; i < xrefSrc.Count; i++)
            {
                MuPDFPage pageSrc = doc2[pnoSrc[i]];
                List<LinkStruct> links = pageSrc.GetLinks();
                if (links.Count == 0)
                {
                    pageSrc = null;
                    continue;
                }

                Matrix ctm = ~pageSrc.TransformationMatrix;
                MuPDFPage pageDst = doc1[pnoDst[i]];
                List<string> linkTab = new List<string>();

                foreach (LinkStruct l in links)
                {
                    if (l.Kind == LinkType.LINK_GOTO && pnoSrc.Contains(l.Page))
                        continue;
                    string annotText = CreateAnnot(l, xrefDst, pnoSrc, ctm);
                    if (annotText != null || annotText != "")
                        linkTab.Add(annotText);
                }
                if (linkTab.Count != 0)
                    pageDst.AddAnnotFromString(linkTab);
            }
        }

        public static int FZ_LANG_TAG2(char c1, char c2)
        {
            return ((c1 - 'a' + 1) + ((c2 - 'a' + 1) * 27));
        }

        public static int FZ_LANG_TAG3(char c1, char c2, char c3)
        {
            return ((c1 - 'a' + 1) + ((c2 - 'a' + 1) * 27) + ((c3 - 'a' + 1) * 27 * 27));
        }

        public static Pixmap GetPixmapFromDisplaylist(FzDisplayList list, Matrix ctm, FzColorspace cs, int alpha, Rect clip,
            FzSeparations seps = null)
        {
            if (seps == null)
                seps = new FzSeparations();

            FzRect rect = mupdf.mupdf.fz_bound_display_list(list);
            FzMatrix matrix = new FzMatrix(ctm.A, ctm.B, ctm.C, ctm.D, ctm.E, ctm.F);
            FzRect rclip = clip == null ? new FzRect(FzRect.Fixed.Fixed_INFINITE) : clip.ToFzRect();
            rect = FzRect.fz_intersect_rect(rect, rclip);
            FzIrect irect = rect.fz_round_rect();

            FzPixmap pix = mupdf.mupdf.fz_new_pixmap_with_bbox(cs, irect, seps, alpha);
            if (alpha != 0)
                pix.fz_clear_pixmap();
            else
                pix.fz_clear_pixmap_with_value(0xFF);

            FzDevice dev;
            if (rclip.fz_is_infinite_rect() == 0)
            {
                dev = mupdf.mupdf.fz_new_draw_device_with_bbox(matrix, pix, irect);
                list.fz_run_display_list(dev, new FzMatrix(), rclip, new FzCookie());
            }
            else
            {
                dev = mupdf.mupdf.fz_new_draw_device(matrix, pix);
                list.fz_run_display_list(dev, new FzMatrix(), new FzRect(FzRect.Fixed.Fixed_INFINITE), new FzCookie());
            }

            mupdf.mupdf.fz_close_device(dev);
            return new Pixmap("raw", pix);
        }

    }
}
