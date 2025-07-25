﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualBasic;
using mupdf;
using MuPDF.NET;
using Newtonsoft.Json;
using SkiaSharp;
using ZXing;
using static System.Net.Mime.MediaTypeNames;
using static MuPDF.NET.Global;

namespace MuPDF.NET
{
    public static class Utils
    {
        public static (string, string) VERSION = ("1.26.1", "3.2.8-rc.6");

        public static int FZ_MIN_INF_RECT = (int)(-0x80000000);

        public static int FZ_MAX_INF_RECT = (int)0x7fffff80;

        public static double FLT_EPSILON = 1e-5;

        public static bool IsInitialized = false;

        public static string ANNOT_ID_STEM = "fitz";

        public static int SigFlag_SignaturesExist = 1;
        public static int SigFlag_AppendOnly = 2;

        public static bool SmallGlyphHeights = false;

        public static int UNIQUE_ID = 0;

        public static Dictionary<string, int> AdobeUnicodes = new Dictionary<string, int>();

        public static Dictionary<int, string> AdobeGlyphs = new Dictionary<int, string>();

        public static string TESSDATA_PREFIX = Environment.GetEnvironmentVariable(
            "TESSDATA_PREFIX"
        );

        public static int trace_device_FILL_PATH = 1;
        public static int trace_device_STROKE_PATH = 2;
        public static int trace_device_CLIP_PATH = 3;
        public static int trace_device_CLIP_STROKE_PATH = 4;

        public static Dictionary<string, string> AnnotSkel = new Dictionary<string, string>()
        {
            {
                "goto1",
                "<</A<</S/GoTo/D[{0, 10} 0 R/XYZ {1} {2} {3}]>>/Rect[{4}]/BS<</W 0>>/Subtype/Link>>"
            },
            { "goto2", "<</A<</S/GoTo/D{0}>>/Rect[{1}]/BS<</W 0>>/Subtype/Link>>" },
            {
                "gotor1",
                "<</A<</S/GoToR/D[{0} /XYZ {1} {2} {3}]/F<</F({4})/UF({5})/Type/Filespec>>>>/Rect[{6}]/BS<</W 0>>/Subtype/Link>>"
            },
            { "gotor2", "<</A<</S/GoToR/D{0}/F({1})>>/Rect[{2}]/BS<</W 0>>/Subtype/Link>>" },
            {
                "launch",
                "<</A<</S/Launch/F<</F({0})/UF({1})/Type/Filespec>>>>/Rect[{2}]/BS<</W 0>>/Subtype/Link>>"
            },
            { "uri", "<</A<</S/URI/URI({0})>>/Rect[{1}]/BS<</W 0>>/Subtype/Link>>" },
            { "named", "<</A<</S/GoTo/D({0})/Type/Action>>/Rect[{1}]/BS<</W 0>>/Subtype/Link>>" }
        };

        public static List<string> MUPDF_WARNINGS_STORE = new List<string>();

        private static Dictionary<string, (int, int)> PaperSizes = new Dictionary<
            string,
            (int, int)
        >()
        {
            { "a0", (2384, 3370) },
            { "a1", (1684, 2384) },
            { "a10", (74, 105) },
            { "a2", (1191, 1684) },
            { "a3", (842, 1191) },
            { "a4", (595, 842) },
            { "a5", (420, 595) },
            { "a6", (298, 420) },
            { "a7", (210, 298) },
            { "a8", (147, 210) },
            { "a9", (105, 147) },
            { "b0", (2835, 4008) },
            { "b1", (2004, 2835) },
            { "b10", (88, 125) },
            { "b2", (1417, 2004) },
            { "b3", (1001, 1417) },
            { "b4", (709, 1001) },
            { "b5", (499, 709) },
            { "b6", (354, 499) },
            { "b7", (249, 354) },
            { "b8", (176, 249) },
            { "b9", (125, 176) },
            { "c0", (2599, 3677) },
            { "c1", (1837, 2599) },
            { "c10", (79, 113) },
            { "c2", (1298, 1837) },
            { "c3", (918, 1298) },
            { "c4", (649, 918) },
            { "c5", (459, 649) },
            { "c6", (323, 459) },
            { "c7", (230, 323) },
            { "c8", (162, 230) },
            { "c9", (113, 162) },
            { "card-4x6", (288, 432) },
            { "card-5x7", (360, 504) },
            { "commercial", (297, 684) },
            { "executive", (522, 756) },
            { "invoice", (396, 612) },
            { "ledger", (792, 1224) },
            { "legal", (612, 1008) },
            { "legal-13", (612, 936) },
            { "letter", (612, 792) },
            { "monarch", (279, 540) },
            { "tabloid-extra", (864, 1296) }
        };

        public static List<(int, double)> zapf_glyphs = new List<(int, double)>()
        {
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

        public static List<(int, double)> symbol_glyphs = new List<(int, double)>()
        {
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

        public static string GetImageExtension(int type)
        {
            if (type == (int)ImageType.FZ_IMAGE_FAX)
                return "fax";
            if (type == (int)ImageType.FZ_IMAGE_RAW)
                return "raw";
            if (type == (int)ImageType.FZ_IMAGE_FLATE)
                return "flate";
            if (type == (int)ImageType.FZ_IMAGE_RLD)
                return "rld";
            if (type == (int)ImageType.FZ_IMAGE_BMP)
                return "bmp";
            if (type == (int)ImageType.FZ_IMAGE_GIF)
                return "gif";
            if (type == (int)ImageType.FZ_IMAGE_LZW)
                return "lzw";
            if (type == (int)ImageType.FZ_IMAGE_JBIG2)
                return "jb2";
            if (type == (int)ImageType.FZ_IMAGE_JPEG)
                return "jpeg";
            if (type == (int)ImageType.FZ_IMAGE_JPX)
                return "jpx";
            if (type == (int)ImageType.FZ_IMAGE_JXR)
                return "jxr";
            if (type == (int)ImageType.FZ_IMAGE_PNG)
                return "png";
            if (type == (int)ImageType.FZ_IMAGE_PNM)
                return "pnm";
            if (type == (int)ImageType.FZ_IMAGE_TIFF)
                return "tiff";
            return "n/a";
        }

        public static Dictionary<string, string> Base14_fontdict = new Dictionary<string, string>()
        {
            { "helv", "Helvetica" },
            { "heit", "Helvetica-Oblique" },
            { "hebo", "Helvetica-Bold" },
            { "hebi", "Helvetica-BoldOblique" },
            { "cour", "Courier" },
            { "coit", "Courier-Obliqu" },
            { "cobo", "Courier-Bold" },
            { "cobi", "Courier-BoldOblique" },
            { "tiro", "Times-Roman" },
            { "tibo", "Times-Bold" },
            { "tiit", "Times-Italic" },
            { "tibi", "Times-BoldItalic" },
            { "symb", "Symbol" },
            { "zadb", "ZapfDingbats" }
        };

        public static Rect INFINITE_RECT()
        {
            return new Rect(
                Utils.FZ_MIN_INF_RECT,
                Utils.FZ_MIN_INF_RECT,
                Utils.FZ_MAX_INF_RECT,
                Utils.FZ_MAX_INF_RECT
            );
        }

        public static Matrix HorMatrix(Point c, Point p)
        {
            FzPoint s = mupdf.mupdf.fz_normalize_vector(
                mupdf.mupdf.fz_make_point(p.X - c.X, p.Y - c.Y)
            );

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
                dst.b = -src.b * rdet;
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
            FzPoint s = mupdf.mupdf.fz_normalize_vector(
                mupdf.mupdf.fz_make_point(q.X - p.X, q.Y - p.Y)
            );
            FzMatrix m1 = mupdf.mupdf.fz_make_matrix(1, 0, 0, 1, -p.X, -p.Y);
            FzMatrix m2 = mupdf.mupdf.fz_make_matrix(s.x, -s.y, s.y, s.x, 0, 0);
            m1 = mupdf.mupdf.fz_concat(m1, m2);
            return mupdf.mupdf.fz_transform_point(c.ToFzPoint(), m1).fz_normalize_vector().y;
        }

        internal static PdfObj pdf_dict_getl(PdfObj obj, string[] keys)
        {
            foreach (string key in keys)
            {
                if (obj.m_internal == null)
                    break;
                obj = obj.pdf_dict_get(new PdfObj(key));
            }
            return obj;
        }

        internal static void pdf_dict_putl(PdfObj obj, PdfObj val, string[] keys)
        {
            if (obj.pdf_is_indirect() != 0)
                obj = obj.pdf_resolve_indirect_chain();
            if (obj.pdf_is_dict() == 0)
                throw new Exception(string.Format("Not a dict: {0}", obj));
            if (keys.Length == 0)
                return;

            PdfDocument doc = obj.pdf_get_bound_document();
            for (int i = 0; i < keys.Length - 1; i++)
            {
                PdfObj nextObj = obj.pdf_dict_get(new PdfObj(keys[i]));
                if (nextObj.m_internal == null)
                {
                    nextObj = doc.pdf_new_dict(1);
                    obj.pdf_dict_put(new PdfObj(keys[i]), nextObj);
                }
                obj = nextObj;
            }
            string key = keys[keys.Length - 1];
            obj.pdf_dict_put(new PdfObj(key), val);
        }

        public static (int, int, int) MUPDF_VERSION = (1, 26, 1);

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

        //public static ColorSpace csRGB = new ColorSpace(Utils.CS_RGB);
        //public static ColorSpace csGRAY = new ColorSpace(Utils.CS_GRAY);
        //public static ColorSpace csCMYK = new ColorSpace(Utils.CS_CMYK);

        public static byte[] BinFromBuffer(FzBuffer buffer)
        {
            //return buffer.fz_buffer_extract();
            var outparams = new mupdf.ll_fz_buffer_storage_outparams();
            uint n = mupdf.mupdf.ll_fz_buffer_storage_outparams_fn(buffer.m_internal, outparams);
            var raw1 = mupdf.SWIGTYPE_p_unsigned_char.getCPtr(outparams.datap);
            System.IntPtr raw2 = System.Runtime.InteropServices.HandleRef.ToIntPtr(raw1);
            byte[] ret = new byte[n];
            // Marshal.Copy() raises exception if <raw2> is null even if <n> is zero.
            if (n > 0)
            {
                System.Runtime.InteropServices.Marshal.Copy(raw2, ret, 0, (int)n);
                outparams.Dispose();
            }
            
            return ret;
        }

        internal static FzBuffer BufferFromBytes(byte[] bytes)
        {
            if (bytes == null)
                return new FzBuffer();
            return Utils.fz_new_buffer_from_data(bytes);
        }

        internal static FzBuffer CompressBuffer(FzBuffer buffer)
        {
            ll_fz_new_deflated_data_from_buffer_outparams outparams =
                new ll_fz_new_deflated_data_from_buffer_outparams();
            SWIGTYPE_p_unsigned_char data =
                mupdf.mupdf.ll_fz_new_deflated_data_from_buffer_outparams_fn(
                    buffer.m_internal,
                    fz_deflate_level.FZ_DEFLATE_BEST,
                    outparams
                );

            if (data == null || outparams.compressed_length == 0)
                return null;
            FzBuffer buf = new FzBuffer(
                mupdf.mupdf.fz_new_buffer_from_data(data, outparams.compressed_length)
            );

            buf.fz_resize_buffer(outparams.compressed_length);
            return buf;
        }

        internal static void UpdateStream(
            PdfDocument doc,
            PdfObj obj,
            FzBuffer buffer,
            int compress
        )
        {
            if (compress != 0)
            {
                uint len = buffer.fz_buffer_storage(new SWIGTYPE_p_p_unsigned_char(IntPtr.Zero, false));
                if (len > 30)
                {
                    FzBuffer res = Utils.CompressBuffer(buffer);
                    if (res != null)
                    {
                        uint nlen = res.fz_buffer_storage(new SWIGTYPE_p_p_unsigned_char(IntPtr.Zero, false));
                        if (nlen < len)
                        {
                            obj.pdf_dict_put(new PdfObj("Filter"), new PdfObj("FlateDecode"));
                            doc.pdf_update_stream(obj, res, 1);
                            return;
                        }
                    }
                }
            }

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

        internal static Matrix RotatePageMatrix(PdfPage page)
        {
            if (page.m_internal == null)
                return new Matrix(new FzMatrix());
            int rotation = Utils.PageRotation(page);
            if (rotation == 0)
                return new Matrix(new FzMatrix());

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

        internal static Point GetCropBoxSize(PdfObj pageObj)
        {
            FzRect rect = GetCropBox(pageObj).ToFzRect();
            float width = Math.Abs(rect.x1 - rect.x0);
            float height = Math.Abs(rect.y1 - rect.y0);

            FzPoint size = mupdf.mupdf.fz_make_point(width, height);
            return new Point(size);
        }

        internal static Rect GetCropBox(PdfObj pageObj)
        {
            FzRect mediabox = Utils.GetMediaBox(pageObj).ToFzRect();
            FzRect cropBox = pageObj.pdf_dict_get_inheritable(new PdfObj("CropBox")).pdf_to_rect();
            if (cropBox.fz_is_infinite_rect() != 0 || cropBox.fz_is_empty_rect() != 0)
                cropBox = mediabox;
            float y0 = mediabox.y1 - cropBox.y1;
            float y1 = mediabox.y1 - cropBox.y0;
            cropBox.y0 = y0;
            cropBox.y1 = y1;

            return new Rect(cropBox);
        }

        internal static Rect GetMediaBox(PdfObj pageObj)
        {
            FzRect pageMediaBox = new FzRect(FzRect.Fixed.Fixed_UNIT);
            FzRect mediaBox = pageObj
                .pdf_dict_get_inheritable(new PdfObj("MediaBox"))
                .pdf_to_rect();
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

            if (pageMediaBox.x1 - pageMediaBox.x0 < 1 || pageMediaBox.y1 - pageMediaBox.y0 < 1)
            {
                pageMediaBox = new FzRect(FzRect.Fixed.Fixed_UNIT);
            }
            return new Rect(pageMediaBox);
        }

        internal static FzMatrix DerotatePageMatrix(PdfPage page)
        {
            Matrix mp = RotatePageMatrix(page);
            return mp.ToFzMatrix().fz_invert_matrix();
        }

        internal static int PageRotation(PdfPage page)
        {
            int rotate;

            PdfObj obj = page.obj().pdf_dict_get_inheritable(new PdfObj("Rotate"));
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
            if (rotate % 90 != 0)
            {
                return 0;
            }
            return rotate;
        }

        internal static FzRect RectFromObj(dynamic r)
        {
            if (r is FzRect)
                return r;
            if (r is FzIrect)
                return new FzRect(r);
            if (r is Rect || r is IRect)
                return mupdf.mupdf.fz_make_rect(r.X0, r.Y0, r.X1, r.Y1);
            if (r == null || r.Length != 4)
                return new FzRect(FzRect.Fixed.Fixed_INFINITE);
            if (r is float[] || r is Tuple<float> || r is List<float>)
            {
                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        if (r[i] < Utils.FZ_MIN_INF_RECT)
                            r[i] = Utils.FZ_MIN_INF_RECT;
                        if (r[i] > Utils.FZ_MAX_INF_RECT)
                            r[i] = Utils.FZ_MAX_INF_RECT;
                    }
                    catch (Exception)
                    {
                        return new FzRect(FzRect.Fixed.Fixed_INFINITE);
                    }
                }
            }
            return mupdf.mupdf.fz_make_rect(r[0], r[1], r[2], r[3]);
        }

        internal static string ReadSamples(FzPixmap pixmap, int offset, int n)
        {
            List<byte> ret = new List<byte>();
            for (int i = 0; i < n; i++)
                ret.Add((byte)pixmap.fz_samples_get(offset + i));
            return Utils.Bytes2Str(ret);
        }

        public static string Bytes2Str(List<byte> bytes)
        {
            //return string.Join(',', bytes.Select(b => $"{b}"));
            return string.Join(",", bytes.Select(b => $"{b}"));
        }

        internal static Dictionary<string, int> ColorCount(FzPixmap pm, dynamic clip)
        {
            Dictionary<string, int> ret = new Dictionary<string, int>();
            int count = 0;
            FzIrect irect = pm.fz_pixmap_bbox();
            irect = irect.fz_intersect_irect(new FzIrect(RectFromObj(clip)));
            int stride = pm.fz_pixmap_stride();
            int width = irect.x1 - irect.x0;
            int height = irect.y1 - irect.y0;
            int n = pm.n();

            int substride = width * n;
            int s = stride * (irect.y0 - pm.y()) + (irect.x0 - pm.x()) * n;
            string oldPix = Utils.ReadSamples(pm, s, n);

            count = 0;
            if (irect.fz_is_empty_irect() != 0)
                return ret;
            string pixel = null;
            int c = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < substride; j += n)
                {
                    string newPix = Utils.ReadSamples(pm, s + j, n);
                    if (!newPix.SequenceEqual(oldPix))
                    {
                        pixel = oldPix;
                        //c = ret.GetValueOrDefault(pixel, 0);
                        c = 0;
                        ret.TryGetValue(pixel, out c);
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
            //c = ret.GetValueOrDefault(pixel, 0);
            c = 0;
            ret.TryGetValue(pixel, out c);
            if (c != 0)
            {
                count += c;
            }
            ret[pixel] = count;
            return ret;
        }

        public static void GetWidgetProperties(Annot annot, Widget widget)
        {
            PdfObj annotObj = mupdf.mupdf.pdf_annot_obj(annot.ToPdfAnnot());
            PdfPage page = mupdf.mupdf.pdf_annot_page(annot.ToPdfAnnot());
            PdfDocument pdf = page.doc();
            PdfAnnot tw = annot.ToPdfAnnot();

            pdf_widget_type fieldType = tw.pdf_widget_type();
            widget.FieldType = (int)fieldType;
            if (fieldType == (pdf_widget_type)PdfWidgetType.PDF_WIDGET_TYPE_SIGNATURE)
            {
                if (pdf.pdf_signature_is_signed(annotObj) != 0)
                    widget.IsSigned = true;
                else
                    widget.IsSigned = false;
            }
            else
                widget.IsSigned = false;
            widget.BorderStyle = Utils.UnicodeFromStr(annotObj.pdf_field_border_style());
            widget.FieldTypeString = Utils.UnicodeFromStr(Utils.GetFieldTypeText((int)fieldType));

            string fieldName = annotObj.pdf_load_field_name();
            widget.FieldName = fieldName;

            PdfObj obj = annotObj.pdf_dict_get(new PdfObj("TU"));
            if (obj.m_internal != null)
            {
                string label = obj.pdf_to_text_string();
                widget.FieldLabel = label;
            }

            string fVal = "";
            if (fieldType == (pdf_widget_type)PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON)
            {
                obj = annotObj.pdf_dict_get(new PdfObj("Parent"));
                if (obj.m_internal != null)
                    widget.RbParent = obj.pdf_to_num();
                obj = annotObj.pdf_dict_get(new PdfObj("AS"));
                if (obj.m_internal != null)
                    fVal = obj.pdf_to_name();
            }
            if (string.IsNullOrEmpty(fVal))
            {
                fVal = annotObj.pdf_field_value();
            }
            widget.FieldValue = Utils.UnicodeFromStr(fVal);
            widget.FieldDisplay = annotObj.pdf_field_display();
            float borderWidth = Utils
                .pdf_dict_getl(annotObj, new string[] { "BS", "W" })
                .pdf_to_real();
            if (borderWidth == 0)
                borderWidth = 1;
            widget.BorderWidth = borderWidth;

            obj = Utils.pdf_dict_getl(annotObj, new string[] { "BS", "D" });
            if (obj.pdf_is_array() != 0)
            {
                int n = obj.pdf_array_len();
                int[] d = new int[n];
                for (int i = 0; i < n; i++)
                    d[i] = obj.pdf_array_get(i).pdf_to_int();
                widget.BorderDashes = d;
            }

            widget.TextMaxLen = tw.pdf_text_widget_max_len();
            widget.TextFormat = tw.pdf_text_widget_format();

            obj = Utils.pdf_dict_getl(annotObj, new string[] { "MK", "BG" });
            if (obj.pdf_is_array() != 0)
            {
                int n = obj.pdf_array_len();
                float[] col = new float[n];
                for (int i = 0; i < n; i++)
                    col[i] = obj.pdf_array_get(i).pdf_to_real();
                widget.FillColor = col;
            }

            obj = Utils.pdf_dict_getl(annotObj, new string[] { "MK", "BC" });
            if (obj.pdf_is_array() != 0)
            {
                int n = obj.pdf_array_len();
                float[] col = new float[n];
                for (int i = 0; i < n; i++)
                    col[i] = obj.pdf_array_get(i).pdf_to_real();
                widget.BorderColor = col;
            }

            widget.ChoiceValues = Utils.GetChoiceOptions(annot.ToPdfAnnot());

            string da = annotObj.pdf_dict_get_inheritable(new PdfObj("DA")).pdf_to_text_string();
            widget.TextDa = Utils.UnicodeFromStr(da);

            obj = Utils.pdf_dict_getl(annotObj, new string[] { "MK", "CA" });
            if (obj.m_internal != null)
                widget.ButtonCaption = Utils.UnicodeFromStr(obj.pdf_to_text_string());
            widget.FieldFlags = annotObj.pdf_field_flags();

            widget.ParseDa();

            PdfObj s = annotObj.pdf_dict_get(new PdfObj("A"));
            string ss = Utils.GetScript(s);
            widget.Script = ss;
            widget.ScriptStroke = Utils.GetScript(
                Utils.pdf_dict_getl(annotObj, new string[] { "AA", "K" })
            );
            widget.ScriptFormat = Utils.GetScript(
                Utils.pdf_dict_getl(annotObj, new string[] { "AA", "F" })
            );
            widget.ScriptChange = Utils.GetScript(
                Utils.pdf_dict_getl(annotObj, new string[] { "AA", "V" })
            );
            widget.ScriptCalc = Utils.GetScript(
                Utils.pdf_dict_getl(annotObj, new string[] { "AA", "C" })
            );
            widget.ScriptBlur = Utils.GetScript(
                Utils.pdf_dict_getl(annotObj, new string[] { "AA", "Bl" })
            );
            widget.ScriptFocus = Utils.GetScript(
                Utils.pdf_dict_getl(annotObj, new string[] { "AA", "Fo" })
            );
        }

        internal static List<dynamic> GetChoiceOptions(PdfAnnot annot)
        {
            PdfObj annotObj = annot.pdf_annot_obj();
            vectors opts = mupdf.mupdf.pdf_choice_widget_options2(annot, 0);
            int n = opts.Capacity;

            if (n == 0)
                return null;
            PdfObj optArr = annotObj.pdf_dict_get(new PdfObj("Opt"));
            List<dynamic> ret = new List<dynamic>();

            for (int i = 0; i < n; i++)
            {
                int m = optArr.pdf_array_get(i).pdf_array_len();
                if (m == 2)
                {
                    ret.Add(
                        new List<string>()
                        {
                            optArr.pdf_array_get(i).pdf_array_get(0).pdf_to_text_string(),
                            optArr.pdf_array_get(i).pdf_array_get(1).pdf_to_text_string()
                        }
                    );
                }
                else
                {
                    ret.Add(new List<string>() { optArr.pdf_array_get(i).pdf_to_text_string() });
                }
            }
            return ret;
        }

        internal static void AddAnnotId(PdfAnnot annot, string stem)
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

        internal static List<string> GetAnnotIDList(PdfPage page)
        {
            List<string> ids = new List<string>();
            PdfObj annots = page.obj().pdf_dict_get(new PdfObj("Annots"));
            if (annots.m_internal == null)
                return ids;
            for (int i = 0; i < annots.pdf_array_len(); i++)
            {
                PdfObj annotObj = annots.pdf_array_get(i);
                PdfObj name = annotObj.pdf_dict_gets("NM");
                if (name.m_internal != null)
                    ids.Add(name.pdf_to_text_string());
            }
            return ids;
        }

        public static byte[] ToByte(string s)
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            return utf8.GetBytes(s);
        }

        public static byte[] ReplaceBytes(byte[] src, byte[] search, byte[] replace, int limit=1)
        {
            if (limit <= 0)
                return src; // no replacement
            using (var ms = new MemoryStream())
            {
                int matchCount = 0;
                for (int i = 0; i < src.Length;)
                {
                    bool match = i + search.Length <= src.Length &&
                                 src.Skip(i).Take(search.Length).SequenceEqual(search);
                    if (match && matchCount < limit)
                    {
                        ms.Write(replace, 0, replace.Length);
                        i += search.Length;
                        matchCount++;
                    }
                    else
                    {
                        ms.WriteByte(src[i]);
                        i++;
                    }
                }
                return ms.ToArray();
            }
        }

        internal static PdfObj EmbedFile(
            PdfDocument pdf,
            FzBuffer buf,
            string fileName,
            string uFilename,
            string desc,
            int compress
        )
        {
            int len = 0;
            PdfObj val = pdf.pdf_new_dict(6);
            val.pdf_dict_put_dict(new PdfObj("CI"), 4);
            PdfObj ef = val.pdf_dict_put_dict(new PdfObj("EF"), 4);
            val.pdf_dict_put_text_string(new PdfObj("F"), fileName);
            val.pdf_dict_put_text_string(new PdfObj("UF"), uFilename);
            val.pdf_dict_put_text_string(new PdfObj("Desc"), desc);
            val.pdf_dict_put(new PdfObj("Type"), new PdfObj("Filespec"));
            byte[] bs = Utils.ToByte("  ");

            PdfObj f = pdf.pdf_add_stream(Utils.fz_new_buffer_from_data(bs), new PdfObj(), 0);

            ef.pdf_dict_put(new PdfObj("F"), f);
            Utils.UpdateStream(pdf, f, buf, compress);
            len = (int)buf.fz_buffer_storage(new SWIGTYPE_p_p_unsigned_char(IntPtr.Zero, false));
            f.pdf_dict_put_int(new PdfObj("DL"), len);
            f.pdf_dict_put_int(new PdfObj("Length"), len);
            PdfObj param = f.pdf_dict_put_dict(new PdfObj("Params"), 4);
            param.pdf_dict_put_int(new PdfObj("Size"), len);

            return val;
        }

        internal static void MakeAnnotDA(
            PdfAnnot annot,
            int nCol,
            float[] col,
            string fontName,
            float fontSize
        )
        {
            string buf = "";
            if (nCol < 1)
                buf += "0 g ";
            else if (nCol == 1)
                buf += $"{col[0]:g} g ";
            else if (nCol == 2)
                Debug.Assert(false);
            else if (nCol == 3)
                buf += $"{col[0]:g} {col[1]:g} {col[2]:g} rg ";
            else
                buf += $"{col[0]:g} {col[1]:g} {col[2]:g} {col[3]:g} k ";
            buf += $"/{ExpandFontName(fontName)} {fontSize} Tf";
            annot.pdf_annot_obj().pdf_dict_put_text_string(new PdfObj("DA"), buf);
        }

        public static string ExpandFontName(string fontname)
        {
            if (fontname == null)
                return "Helv";
            if (fontname.StartsWith("Co"))
                return "Cour";
            if (fontname.StartsWith("co"))
                return "Cour";
            if (fontname.StartsWith("Ti"))
                return "TiRo";
            if (fontname.StartsWith("ti"))
                return "TiRo";
            if (fontname.StartsWith("Sy"))
                return "Symb";
            if (fontname.StartsWith("sy"))
                return "Symb";
            if (fontname.StartsWith("Za"))
                return "ZaDb";
            if (fontname.StartsWith("za"))
                return "ZaDb";
            return "Helv";
        }

        /// <summary>
        /// Return the text words as a list with the bbox for each word
        /// </summary>
        /// <param name="page"></param>
        /// <param name="clip">area on page to consider</param>
        /// <param name="flags">control the amount of data parsed into the textpage</param>
        /// <param name="stPage">either passed-in or null</param>
        /// <param name="sort">sort the words in reading sequence</param>
        /// <param name="delimiters">characters to use as word delimiters</param>
        /// <param name="tolerance">consider words to be part of the same line</param>
        /// <returns>Word Tuples (x0, y0, x1, y1, "word", bno, lno, wno)</returns>
        /// <exception cref="Exception"></exception>
        public static List<WordBlock> GetTextWords(
            Page page,
            Rect clip = null,
            int flags = 0,
            TextPage stPage = null,
            bool sort = false,
            char[] delimiters = null,
            int tolerance = 3
        )
        {
            List<WordBlock> SortWords(List<WordBlock> _words)
            {
                _words.Sort((w1, w2) =>
                {
                    if (w1.Y1 == w2.Y1)
                    {
                        return w1.X0.CompareTo(w2.X0);
                    }
                    else
                    {
                        return w1.Y1.CompareTo(w2.Y1);
                    }
                });

                List<WordBlock> nWords = new List<WordBlock>();
                List<WordBlock> line = new List<WordBlock>() { _words[0] };
                Rect lrect = new Rect(_words[0].X0, _words[0].Y0, _words[0].X1, _words[0].Y1);

                foreach (WordBlock w in _words.Skip(1))
                {
                    Rect wrect = new Rect(w.X0, w.Y0, w.X1, w.Y1);
                    if (Math.Abs(wrect.Y0 - lrect.Y0) <= tolerance || Math.Abs(wrect.Y1 - lrect.Y1) <= tolerance)
                    {
                        line.Add(w);
                        lrect |= wrect;
                    }
                    else
                    {
                        line.Sort((w1, w2) => { return w1.X0.CompareTo(w2.X0); });
                        nWords.AddRange(line);
                        line = new List<WordBlock> { w };
                        lrect = wrect;
                    }
                }
                line.Sort((w1, w2) => { return w1.X0.CompareTo(w2.X0); });
                nWords.AddRange(line);

                return nWords;
            }
            if (flags == 0)
                flags = (int)(0
                    | TextFlags.TEXT_PRESERVE_WHITESPACE
                    | TextFlags.TEXT_PRESERVE_LIGATURES
                    | TextFlags.TEXT_MEDIABOX_CLIP
                    | TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE
                );

            TextPage tp = stPage;

            if (tp == null)
                tp = page.GetTextPage(clip, flags);
            else if (tp.Parent != page)
                throw new Exception("not a textpage of this page");

            List<WordBlock> words = tp.ExtractWords(delimiters);
            if (stPage != null)
            {
                words = words.Where(w =>
                {
                    Rect wRect = new Rect(w.X0, w.Y0, w.X1, w.Y1);
                    return (clip & wRect).Abs() >= (0.5f * wRect.Abs());
                }).ToList();
            }

            if (stPage is null)
                tp = null;

            if (words.Count > 0 && sort)
                words = SortWords(words);

            return words;
        }

        public static List<TextBlock> GetTextBlocks(
            Page page,
            Rect clip = null,
            int flags = 0,
            TextPage textPage = null,
            bool sort = false
        )
        {
            if (flags == 0)
            {
                flags = (int)(
                    TextFlags.TEXT_PRESERVE_WHITESPACE
                    | TextFlags.TEXT_PRESERVE_IMAGES
                    | TextFlags.TEXT_PRESERVE_LIGATURES
                    | TextFlags.TEXT_MEDIABOX_CLIP
                );
            }
            TextPage tp = textPage;
            if (tp == null)
                tp = page.GetTextPage(clip, flags);
            else if (tp.Parent != page)
                throw new Exception("not a textpage of this page");

            List<TextBlock> blocks = tp.ExtractBlocks();
            if (textPage == null)
                tp = null;
            if (sort == true)
                blocks.Sort(
                    (TextBlock t1, TextBlock t2) =>
                    {
                        var result = t1.Y1.CompareTo(t2.Y1);
                        if (result == 0)
                        {
                            result = t1.X0.CompareTo(t2.X0);
                        }
                        return result;
                    }
                );
            return blocks;
        }

        public static List<Table> GetTables(
            Page page,
            Rect clip = null,
            string vertical_strategy = "lines",
            string horizontal_strategy = "lines",
            List<Edge> vertical_lines = null,
            List<Edge> horizontal_lines = null,
            float snap_tolerance = TableFlags.TABLE_DEFAULT_SNAP_TOLERANCE,
            float snap_x_tolerance = 0.0f,
            float snap_y_tolerance = 0.0f,
            float join_tolerance = TableFlags.TABLE_DEFAULT_JOIN_TOLERANCE,
            float join_x_tolerance = 0.0f,
            float join_y_tolerance = 0.0f,
            float edge_min_length = 3.0f,
            float min_words_vertical = TableFlags.TABLE_DEFAULT_MIN_WORDS_VERTICAL,
            float min_words_horizontal = TableFlags.TABLE_DEFAULT_MIN_WORDS_HORIZONTAL,
            float intersection_tolerance = 3.0f,
            float intersection_x_tolerance = 0.0f,
            float intersection_y_tolerance = 0.0f,
            float text_tolerance = 3.0f,
            float text_x_tolerance = 3.0f,
            float text_y_tolerance = 3.0f,
            string strategy = null,  // offer abbreviation
            List<Line> add_lines = null
        )
        {
            if (strategy != null)
            {
                vertical_strategy = strategy;
                horizontal_strategy = strategy;
            }

            Dictionary<string, object> settings = new Dictionary<string, object>
            {
                { "vertical_strategy", vertical_strategy },
                { "horizontal_strategy", horizontal_strategy },
                { "explicit_vertical_lines", vertical_lines },
                { "explicit_horizontal_lines", horizontal_lines },
                { "snap_tolerance", snap_tolerance },
                { "snap_x_tolerance", snap_x_tolerance },
                { "snap_y_tolerance", snap_y_tolerance },
                { "join_tolerance", join_tolerance },
                { "join_x_tolerance", join_x_tolerance },
                { "join_y_tolerance", join_y_tolerance },
                { "edge_min_length", edge_min_length },
                { "min_words_vertical", min_words_vertical },
                { "min_words_horizontal", min_words_horizontal },
                { "intersection_tolerance", intersection_tolerance },
                { "intersection_x_tolerance", intersection_x_tolerance },
                { "intersection_y_tolerance", intersection_y_tolerance },
                { "text_tolerance", text_tolerance },
                { "text_x_tolerance", text_x_tolerance },
                { "text_y_tolerance", text_y_tolerance }
            };

            // Resolve settings
            TableSettings tset = TableSettings.resolve(settings);

            List<Table> tables = TableFinder.FindTables(page, clip, tset);
            return tables;
        }

        // Manually turn on all formats, even those not yet considered production quality.
        private static IDictionary<DecodeHintType, object> buildHints(Config config)
        {
            var hints = new Dictionary<DecodeHintType, Object>();
            var vector = new List<ZXing.BarcodeFormat>
            {
                ZXing.BarcodeFormat.AZTEC,
                ZXing.BarcodeFormat.CODABAR,
                ZXing.BarcodeFormat.CODE_39,
                ZXing.BarcodeFormat.CODE_93,
                ZXing.BarcodeFormat.CODE_128,
                ZXing.BarcodeFormat.DATA_MATRIX,
                ZXing.BarcodeFormat.EAN_8,
                ZXing.BarcodeFormat.EAN_13,
                ZXing.BarcodeFormat.ITF,
                ZXing.BarcodeFormat.MAXICODE,
                ZXing.BarcodeFormat.PDF_417,
                ZXing.BarcodeFormat.QR_CODE,
                ZXing.BarcodeFormat.RSS_14,
                ZXing.BarcodeFormat.RSS_EXPANDED,
                ZXing.BarcodeFormat.UPC_A,
                ZXing.BarcodeFormat.UPC_E,
                ZXing.BarcodeFormat.UPC_EAN_EXTENSION,
                ZXing.BarcodeFormat.MSI,
                ZXing.BarcodeFormat.PLESSEY,
                ZXing.BarcodeFormat.IMB,
                //ZXing.BarcodeFormat.PHARMA_CODE
            };
            hints[DecodeHintType.POSSIBLE_FORMATS] = vector;
            if (config.TryHarder)
            {
                hints[DecodeHintType.TRY_HARDER] = true;
            }
            if (config.TryInverted)
            {
                hints[DecodeHintType.ALSO_INVERTED] = true;
            }
            if (config.PureBarcode)
            {
                hints[DecodeHintType.PURE_BARCODE] = true;
            }
            return hints;
        }

        /// <summary>
        /// Read barcodes from page.
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="decodeEmbeddedOnly">Decode barcodes only from embedded images in the PDF resources.</param>
        /// <param name="tryHarder">Spend more time to try to find a barcode; optimize for accuracy, not speed.</param>
        /// <param name="tryInverted">Try to decode as inverted image.</param>
        /// <param name="pureBarcode">Image is a pure monochrome image of a barcode.</param>
        /// <param name="multi">Try to read multi barcodes on page.</param>
        /// <param name="autoRotate">Indicate whether the image should be automatically rotated.
        ///                          Rotation is supported for 90, 180 and 270 degrees.</param>
        public static List<Barcode> ReadBarcodes(
            Page page,
            Rect clip = null,
            bool decodeEmbeddedOnly = false,
            bool tryHarder = true,
            bool tryInverted = false,
            bool pureBarcode = false,
            bool multi = true,
            bool autoRotate = true
            )
        {
            // if clip is null, use the whole page rect
            if (clip == null)
            {
                clip = new Rect(0, 0, page.Rect.Width, page.Rect.Height);
            }

            List<Barcode> barcodes = new List<Barcode>();

            List<Rect> blockRects = new List<Rect>();

            if (!decodeEmbeddedOnly)
            {
                blockRects.Add(clip); // add the clip rect to the list
            }
            else
            {
                foreach (Block block in page.GetImageInfo())
                {
                    Rect blockRectItem = block.Bbox;
                    if (clip.Contains(blockRectItem))
                    {
                        blockRects.Add(blockRectItem);
                    }
                }
            }

            foreach (Rect blockRect in blockRects)
            {
                Rect newRect = blockRect;
                // make space around of clip
                if (blockRect.X0 > 5 && blockRect.Y0 > 5 &&
                    blockRect.X1 < page.Rect.Width - 5 && blockRect.Y1 < page.Rect.Height - 5)
                {
                    newRect = new Rect(blockRect.X0 - 5, blockRect.Y0 - 5, blockRect.X1 + 5, blockRect.Y1 + 5);
                }

                // save the start x and y of the clip
                float startX = newRect.X0;
                float startY = newRect.Y0;

                Config config = new Config();

                Pixmap pxmp = page.GetPixmap(dpi: 100, clip: newRect);
                byte[] pmBuf = pxmp.ToBytes();

                // Calculate Rect ratio between PDF page and image.
                float imageWidth = pxmp.IRect.Width;
                float imageHeight = pxmp.IRect.Height + 0.01f;
                float pageWidth = newRect.Width;
                float pageHeight = newRect.Height + 0.01f;
                float widthRatio = imageWidth / pageWidth;
                float heightRatio = imageHeight / pageHeight;

                pxmp.Dispose(); // free pixmap memory

                // set config options
                config.TryHarder = tryHarder;
                config.TryInverted = tryInverted;
                config.PureBarcode = pureBarcode;
                config.Multi = multi;
                config.AutoRotate = autoRotate;
                config.Hints = buildHints(config);

                var decodeObject = new Decode();
                Result[] results = decodeObject.decodeMulti(pmBuf, config);
                if (results == null || results.Length == 0)
                {
                    continue; // no barcodes found
                }
                foreach (var result in results)
                {
                    BarcodePoint[] points = new BarcodePoint[result.ResultPoints.Length];
                    for (int i = 0; i < result.ResultPoints.Length; i++)
                    {
                        // revert the original pdf page ratio
                        points[i] = new BarcodePoint(startX + result.ResultPoints[i].X / widthRatio, startY + result.ResultPoints[i].Y / heightRatio);
                    }

                    Barcode barcode = new Barcode(
                        result.Text,
                        result.RawBytes,
                        result.NumBits,
                        points,
                        (BarcodeFormat)result.BarcodeFormat,
                        result.Timestamp
                    );
                    foreach (var metadata in result.ResultMetadata)
                    {
                        barcode.putMetadata((BarcodeMetadataType)metadata.Key, metadata.Value);
                    }
                    barcodes.Add(barcode);
                }
            }

            return barcodes;
        }

        /// <summary>
        /// Read barcodes from image file.
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="tryHarder">Spend more time to try to find a barcode; optimize for accuracy, not speed.</param>
        /// <param name="tryInverted">Try to decode as inverted image.</param>
        /// <param name="pureBarcode">Image is a pure monochrome image of a barcode.</param>
        /// <param name="multi">Try to read multi barcodes on page.</param>
        /// <param name="autoRotate">Indicate whether the image should be automatically rotated.
        ///                          Rotation is supported for 90, 180 and 270 degrees.</param>
        public static List<Barcode> ReadBarcodes(
            string imageFile,
            Rect clip = null,
            bool tryHarder = true,
            bool tryInverted = false,
            bool pureBarcode = false,
            bool multi = true,
            bool autoRotate = true
            )
        {
            Config config = new Config();

            // set crop
            if (clip != null)
            {
                int[] crop = new int[4];
                // copy crop from clip
                crop[0] = (int)clip.X0;
                crop[1] = (int)clip.Y0;
                crop[2] = (int)clip.Width;
                crop[3] = (int)clip.Height;

                config.Crop = crop;
            }

            config.TryHarder = tryHarder;
            config.TryInverted = tryInverted;
            config.PureBarcode = pureBarcode;
            config.Multi = multi;
            config.AutoRotate = autoRotate;

            config.Hints = buildHints(config);

            List<Barcode> barcodes = new List<Barcode>();

            var decodeObject = new Decode();
            Result[] results = decodeObject.decodeMulti(imageFile, config);
            if (results == null || results.Length == 0)
            {
                return barcodes; // no barcodes found
            }

            foreach (var result in results)
            {
                BarcodePoint[] points = new BarcodePoint[result.ResultPoints.Length];
                for (int i = 0; i < result.ResultPoints.Length; i++)
                {
                    points[i] = new BarcodePoint(result.ResultPoints[i].X, result.ResultPoints[i].Y);
                }

                Barcode barcode = new Barcode(
                    result.Text,
                    result.RawBytes,
                    result.NumBits,
                    points,
                    (BarcodeFormat)result.BarcodeFormat,
                    result.Timestamp
                );
                foreach (var metadata in result.ResultMetadata)
                {
                    barcode.putMetadata((BarcodeMetadataType)metadata.Key, metadata.Value);
                }
                barcodes.Add(barcode);
            }

            return barcodes;
        }

        /// <summary>
        /// Write barcode to pdf page.
        /// </summary>
        /// <param name="clip">Rect area on page to write</param>
        /// <param name="text">Contents to write</param>
        /// <param name="barcodeFormat">Format to encode; Supported formats: QR_CODE, EAN_8, EAN_13, UPC_A, CODE_39, CODE_128, ITF, PDF_417, CODABAR</param>
        /// <param name="characterSet">Use a specific character set for binary encoding (if supported by the selected barcode format)</param>
        /// <param name="disableEci">Don't generate ECI segment if non-default character set is used</param>
        /// <param name="forceFitToRect">Resize output barcode image width/height into clip region</param>
        /// <param name="pureBarcode">Don't put the content string into the output image</param>
        /// <param name="margin">Specifies margin, in pixels, to use when generating the barcode</param>
        public static void WriteBarcode(
            Page page,
            Rect clip,
            string text,
            BarcodeFormat barcodeFormat,
            string characterSet = null,
            bool disableEci = false,
            bool forceFitToRect = false,
            bool pureBarcode = false,
            int margin = 1
            )
        {
            if (clip == null)
            {
                throw new Exception("Rect is required");
            }
            if (text == null)
            {
                throw new Exception("Text is required");
            }

            int width = (int)clip.Width;
            int height = (int)clip.Height;

            // get image format from file extension
            SKEncodedImageFormat imageFormat = SKEncodedImageFormat.Png;

            var encodeObject = new Encode();

            // create barcode image
            SKBitmap encodedImage = encodeObject.encode(text,
                barcodeFormat,
                imageFormat,
                width,
                height,
                characterSet,
                disableEci,
                pureBarcode,
                margin);

            if (encodedImage == null)
            {
                throw new Exception("Failed to create barcode image");
            }

            // resize image to fit into clip region
            if (forceFitToRect)
            {
                SKBitmap resizedBitmap = new SKBitmap(width, height);
                using (SKCanvas canvas = new SKCanvas(resizedBitmap))
                {
                    canvas.DrawBitmap(encodedImage, new SKRect(0, 0, width, height));
                }
                encodedImage = resizedBitmap;
            }
            
            Rect rect = new Rect(clip.X0, clip.Y0, clip.X0 + encodedImage.Width + 1, clip.Y0 + encodedImage.Height+1);

            MemoryStream ms = new MemoryStream();
            using (SKData data = encodedImage.Encode(SKEncodedImageFormat.Png, 300))
            {
                data.SaveTo(ms);
                ms.Position = 0; // Reset stream position
                page.InsertImage(rect, stream: ms.ToArray());
            }
        }

        /// <summary>
        /// Write barcode to image file.
        /// </summary>
        /// <param name="imageFile">Full path of being created barcode image file</param>
        /// <param name="text">Contents to write</param>
        /// <param name="barcodeFormat">Format to encode; Supported formats: QR_CODE, EAN_8, EAN_13, UPC_A, CODE_39, CODE_128, ITF, PDF_417, CODABAR</param>
        /// <param name="width">width of image</param>
        /// <param name="height">height of image</param>
        /// <param name="characterSet">Use a specific character set for binary encoding (if supported by the selected barcode format)</param>
        /// <param name="disableEci">don't generate ECI segment if non-default character set is used</param>
        /// <param name="forceFitToRect">Resize output barcode image width/height with params</param>
        /// <param name="pureBarcode">Don't put the content string into the output image</param>
        /// <param name="margin">Specifies margin, in pixels, to use when generating the barcode</param>
        public static void WriteBarcode(
            string imageFile,
            string text,
            BarcodeFormat barcodeFormat,
            int width = 300,
            int height = 300,
            string characterSet = null,
            bool disableEci = false,
            bool forceFitToRect = false,
            bool pureBarcode = false,
            int margin = 1
            )
        {
            // get image format from file extension
            SKEncodedImageFormat imageFormat = SKEncodedImageFormat.Png;
            string extension = System.IO.Path.GetExtension(imageFile).ToLower();
            switch (extension)
            {
                case ".bmp":
                    imageFormat = SKEncodedImageFormat.Bmp;
                    break;
                case ".gif":
                    imageFormat = SKEncodedImageFormat.Gif;
                    break;
                case ".ico":
                case ".icon":
                    imageFormat = SKEncodedImageFormat.Ico;
                    break;
                case ".jpeg":
                case ".jpg":
                    imageFormat = SKEncodedImageFormat.Jpeg;
                    break;
                case ".png":
                    imageFormat = SKEncodedImageFormat.Png;
                    break;
                case ".webp":
                    imageFormat = SKEncodedImageFormat.Webp;
                    break;
                default:
                    throw new Exception("Unsupported image format");
            }

            var encodeObject = new Encode();

            // create barcode image
            SKBitmap encodedImage = encodeObject.encode(text,
                barcodeFormat,
                imageFormat,
                width,
                height,
                characterSet,
                disableEci,
                pureBarcode,
                margin
                );

            if (encodedImage == null)
            {
                throw new Exception("Failed to create barcode image");
            }

            // resize image to fit into clip region
            if (forceFitToRect)
            {
                SKBitmap resizedBitmap = new SKBitmap(width, height);
                using (SKCanvas canvas = new SKCanvas(resizedBitmap))
                {
                    canvas.DrawBitmap(encodedImage, new SKRect(0, 0, width, height));
                }
                encodedImage = resizedBitmap;
            }

            // save image to file
            using (var image = SKImage.FromBitmap(encodedImage))
            {
                using (var data = image.Encode(imageFormat, 300))
                {
                    using (var stream = File.OpenWrite(imageFile))
                    {
                        data.SaveTo(stream);
                    }
                }
            }
        }

        public static dynamic GetText(
            Page page,
            string option = "text",
            Rect clip = null,
            int flags = 0,
            TextPage stPage = null,
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
                flags = (int)(
                    TextFlags.TEXT_PRESERVE_WHITESPACE
                    | TextFlags.TEXT_PRESERVE_LIGATURES
                    | TextFlags.TEXT_MEDIABOX_CLIP
                    | TextFlags.TEXT_PRESERVE_IMAGES
                    | TextFlags.TEXT_INHIBIT_SPACES
                    | TextFlags.TEXT_DEHYPHENATE
                    | TextFlags.TEXT_PRESERVE_SPANS
                    | TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE
                    | TextFlags.TEXT_COLLECT_STRUCTURE
                    | TextFlags.TEXT_ACCURATE_BBOXES
                );
                if (formats[option] == 1)
                    flags = flags | (int)TextFlags.TEXT_PRESERVE_IMAGES;
            }

            if (option == "words")
            {
                return Utils.GetTextWords(page, clip, flags, stPage, sort, delimiters);
            }

            if (option == "blocks")
            {
                return Utils.GetTextBlocks(page, clip, flags, stPage, sort);
            }

            Rect cb = null;
            if ((new List<string>() { "html", "xml", "xhtml" }).Contains(option))
                clip = page.CropBox;
            if (clip != null)
                cb = null;
            else if (page is Page)
                cb = page.CropBox;
            if (clip == null)
                clip = page.CropBox;

            TextPage tp = stPage;
            if (tp == null)
                tp = page.GetTextPage(clip, flags);
            else if (tp.Parent != page)
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
                t = tp.ExtractText(sort);
            else
                t = tp.ExtractText();

            if (stPage == null)
                tp = null;
            return t;
        }

        internal static void SetFieldType(PdfDocument doc, PdfObj annotObj, PdfWidgetType type)
        {
            PdfFieldFlags setBits = 0;
            PdfFieldFlags clearBits = 0;
            PdfObj typeName = null;

            if (type == PdfWidgetType.PDF_WIDGET_TYPE_BUTTON)
            {
                typeName = new PdfObj("Btn");
                setBits = PdfFieldFlags.PDF_BTN_FIELD_IS_PUSHBUTTON;
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON)
            {
                typeName = new PdfObj("Btn");
                clearBits = PdfFieldFlags.PDF_BTN_FIELD_IS_PUSHBUTTON;
                setBits = PdfFieldFlags.PDF_BTN_FIELD_IS_RADIO;
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX)
            {
                typeName = new PdfObj("Btn");
                clearBits = (
                    PdfFieldFlags.PDF_BTN_FIELD_IS_PUSHBUTTON | PdfFieldFlags.PDF_BTN_FIELD_IS_RADIO
                );
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_TEXT)
            {
                typeName = new PdfObj("Tx");
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_LISTBOX)
            {
                typeName = new PdfObj("Ch");
                clearBits = PdfFieldFlags.PDF_CH_FIELD_IS_COMBO;
            }
            else if (type == PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX)
            {
                typeName = new PdfObj("Ch");
                setBits = PdfFieldFlags.PDF_CH_FIELD_IS_COMBO;
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

        internal static PdfAnnot CreateWidget(
            PdfDocument doc,
            PdfPage page,
            PdfWidgetType type,
            string fieldName
        )
        {
            int oldSigFlags = doc.pdf_trailer()
                .pdf_dict_getp("Root/AcroForm/SigFlags")
                .pdf_to_int();
            PdfAnnot annot = page.pdf_create_annot_raw(pdf_annot_type.PDF_ANNOT_WIDGET);
            PdfObj annotObj = annot.pdf_annot_obj();
            try
            {
                Utils.SetFieldType(doc, annotObj, type);
                annotObj.pdf_dict_put_text_string(new PdfObj("T"), fieldName);

                if (type == PdfWidgetType.PDF_WIDGET_TYPE_SIGNATURE)
                {
                    int sigFlags =
                        oldSigFlags | (Utils.SigFlag_SignaturesExist | Utils.SigFlag_AppendOnly);
                    Utils.pdf_dict_putl(
                        doc.pdf_trailer(),
                        mupdf.mupdf.pdf_new_int(sigFlags),
                        new string[] { "Root", "AcroForm", "SigFlags" }
                    );
                }

                PdfObj form = doc.pdf_trailer().pdf_dict_getp("Root/AcroForm/Fields");
                if (form.m_internal == null)
                {
                    form = doc.pdf_new_array(1);
                    Utils.pdf_dict_putl(
                        doc.pdf_trailer(),
                        form,
                        new string[] { "Root", "AcroForm", "Fields" }
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
                        new string[] { "Root", "AcroForm", "SigFlags" }
                    );
                }
            }
            return annot;
        }

        internal static List<(string, int)> GetResourceProperties(PdfObj refer)
        {
            PdfObj properties = Utils.pdf_dict_getl(
                refer,
                new string[] { "Resource", "Properties" }
            );
            List<(string, int)> rc = new List<(string, int)>();
            if (properties.m_internal == null)
                return new List<(string, int)>();
            else
            {
                int n = properties.pdf_dict_len();
                if (n < 1)
                    return new List<(string, int)>();

                for (int i = 0; i < n; i++)
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

        internal static void SetResourceProperty(PdfObj refer, string name, int xref)
        {
            PdfDocument pdf = refer.pdf_get_bound_document();
            PdfObj ind = pdf.pdf_new_indirect(xref, 0);

            if (ind.m_internal == null)
                Console.WriteLine(Utils.ErrorMessages["MSG_BAD_XREF"]);

            PdfObj resource = refer.pdf_dict_get(new PdfObj("Resource"));
            if (resource.m_internal == null)
                resource = refer.pdf_dict_put_dict(new PdfObj("Resources"), 1);

            PdfObj properties = resource.pdf_dict_get(new PdfObj("Properties"));
            if (properties.m_internal == null)
                properties = resource.pdf_dict_put_dict(new PdfObj("Properties"), 1);

            properties.pdf_dict_put(mupdf.mupdf.pdf_new_name(name), ind);
        }

        public static int GenID()
        {
            UNIQUE_ID += 1;
            return UNIQUE_ID;
        }

        internal static void EmbeddedClean(PdfDocument pdf)
        {
            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            PdfObj coll = root.pdf_dict_get(new PdfObj("Collection"));
            if (coll.m_internal != null && coll.pdf_dict_len() == 0)
            {
                root.pdf_dict_del(new PdfObj("Collection"));
            }

            PdfObj efiles = Utils.pdf_dict_getl(
                root,
                new string[] { "Names", "EmbeddedFiles", "Names" }
            );
            if (efiles.m_internal != null)
                root.pdf_dict_put_name(new PdfObj("PageMode"), "UseAttachments");
        }

        public static void EnsureIdentity(Document pdf)
        {
            PdfObj id = Document.AsPdfDocument(pdf).pdf_trailer().pdf_dict_get(new PdfObj("ID"));
            if (id.m_internal == null)
            {
                IntPtr p_block = Marshal.AllocHGlobal(16);
                SWIGTYPE_p_unsigned_char swigBlock = new SWIGTYPE_p_unsigned_char(p_block, true);
                mupdf.mupdf.fz_memrnd(swigBlock, 16);

                //string rnd0 = Marshal.PtrToStringUTF8(p_block);
                byte[] byteArray = new byte[16];
                Marshal.Copy(p_block, byteArray, 0, byteArray.Length);
                string rnd0 = System.Text.Encoding.UTF8.GetString(byteArray);

                Marshal.FreeHGlobal(p_block);

                id = Document.AsPdfDocument(pdf).pdf_trailer().pdf_dict_put_array(new PdfObj("ID"), 2);
                id.pdf_array_push(mupdf.mupdf.pdf_new_string(rnd0, (uint)rnd0.Length));
                id.pdf_array_push(mupdf.mupdf.pdf_new_string(rnd0, (uint)rnd0.Length));
            }
            id.Dispose();
        }

        public static FontInfo CheckFont(Page page, string fontName)
        {
            foreach (Entry f in page.GetFonts())
            {
                if (f.RefName == fontName)
                {
                    return new FontInfo()
                    {
                        Xref = f.Xref,
                        Ext = f.Ext,
                        Type = f.Type,
                        Name = f.Name,
                        RefName = f.RefName,
                        Encoding = f.Encoding,
                        StreamXref = f.StreamXref
                    };
                }
            }
            return null;
        }

        public static FontInfo CheckFontInfo(Document doc, int xref)
        {
            foreach (FontInfo f in doc.FontInfos)
            {
                if (xref == f.Xref)
                    return f;
            }
            return null;
        }

        internal static void ScanResources(
            PdfDocument pdf,
            PdfObj rsrc,
            List<Entry> liste,
            int what,
            int streamXRef,
            List<dynamic> tracer
        )
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
                for (int i = 0; i < n; i++)
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

        internal static int GatherFonts(
            PdfDocument pdf,
            PdfObj dict,
            List<Entry> fontList,
            int streamXRef
        )
        {
            int rc = 1;
            int n = dict.pdf_dict_len();

            for (int i = 0; i < n; i++)
            {
                PdfObj refName = dict.pdf_dict_get_key(i);
                PdfObj fontDict = dict.pdf_dict_get_val(i);
                if (fontDict.pdf_is_dict() == 0)
                {
                    mupdf.mupdf.fz_warn(
                        $"'{refName.pdf_to_name()}' is no font dict ({fontDict.pdf_to_num()} 0 R)"
                    );
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

                Entry entry = new Entry
                {
                    Xref = xref,
                    Ext = ext,
                    Type = subType.pdf_to_name(),
                    Name = Utils.EscapeStrFromStr(name.pdf_to_name()),
                    RefName = refName.pdf_to_name(),
                    Encoding = encoding.pdf_to_name(),
                    StreamXref = streamXRef
                };
                fontList.Add(entry);
            }

            return rc;
        }

        internal static string GetFontExtension(PdfDocument doc, int xref)
        {
            if (xref < 1)
                return "n/a";
            PdfObj o = mupdf.mupdf.pdf_load_object(doc, xref);
            PdfObj desft = o.pdf_dict_get(new PdfObj("DescendantFonts"));
            PdfObj obj = null;
            if (desft.m_internal != null)
            {
                obj = desft.pdf_array_get(0).pdf_resolve_indirect();
                obj = obj.pdf_dict_get(new PdfObj("FontDescriptor"));
            }
            else
                obj = o.pdf_dict_get(new PdfObj("FontDescriptor"));
            if (obj.m_internal == null)
                return "n/a";

            o = obj;
            obj = o.pdf_dict_get(new PdfObj("FontFile"));
            if (obj.m_internal != null)
                return "pfa";

            obj = o.pdf_dict_get(new PdfObj("FontFile2"));
            if (obj.m_internal != null)
                return "ttf";

            obj = o.pdf_dict_get(new PdfObj("FontFile3"));
            if (obj.m_internal != null)
            {
                obj = obj.pdf_dict_get(new PdfObj("Subtype"));
                if (obj.m_internal != null && obj.pdf_is_name() == 0)
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

        internal static string EscapeStrFromStr(string c)
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

        internal static int GatherForms(
            PdfDocument doc,
            PdfObj dict,
            List<Entry> imageList,
            int streamXRef
        )
        {
            int rc = 1;
            int n = dict.pdf_dict_len();
            for (int i = 0; i < n; i++)
            {
                PdfObj refName = dict.pdf_dict_get_key(i);
                PdfObj imageDict = dict.pdf_dict_get_val(i);
                if (imageDict == null)
                {
                    mupdf.mupdf.fz_warn(
                        $"'{refName.pdf_to_name()}' is no form dict ({imageDict.pdf_to_num()} 0 R)"
                    );
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

                Entry entry = new Entry()
                {
                    Xref = xref,
                    RefName = refName.pdf_to_name(),
                    StreamXref = streamXRef,
                    Bbox = new Rect(bbox)
                };
                imageList.Add(entry);
            }
            return rc;
        }

        /// <summary>
        /// Store info of an image in Python list
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="dict"></param>
        /// <param name="imageList"></param>
        /// <param name="streamXRef"></param>
        /// <returns></returns>
        internal static int GatherIamges(
            PdfDocument doc,
            PdfObj dict,
            List<Entry> imageList,
            int streamXRef
        )
        {
            int rc = 1;
            int n = dict.pdf_dict_len();
            for (int i = 0; i < n; i++)
            {
                PdfObj refName = dict.pdf_dict_get_key(i);
                PdfObj imageDict = dict.pdf_dict_get_val(i);
                if (imageDict.pdf_is_dict() == 0)
                {
                    mupdf.mupdf.fz_warn(
                        $"'{refName.pdf_to_name()}' is no image dict ({imageDict.pdf_to_name()} 0 R)"
                    );
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
                    if (
                        cs.pdf_name_eq(new PdfObj("DeviceN")) != 0
                        || cs.pdf_name_eq(new PdfObj("Separation")) != 0
                    )
                    {
                        altcs = cses.pdf_array_get(2);
                        if (altcs.pdf_is_array() != 0)
                            altcs = altcs.pdf_array_get(0);
                    }
                }

                PdfObj width = imageDict.pdf_dict_geta(new PdfObj("Width"), new PdfObj("W"));
                PdfObj height = imageDict.pdf_dict_geta(new PdfObj("Height"), new PdfObj("H"));
                PdfObj bpc = imageDict.pdf_dict_geta(
                    new PdfObj("BitsPerComponent"),
                    new PdfObj("BPC")
                );

                Entry entry = new Entry()
                {
                    Xref = xref,
                    Smask = gen,
                    Width = width.pdf_to_int(),
                    Height = height.pdf_to_int(),
                    Bpc = bpc.pdf_to_int(),
                    CsName = Utils.EscapeStrFromStr(cs.pdf_to_name()),
                    AltCsName = Utils.EscapeStrFromStr(altcs.pdf_to_name()),
                    RefName = Utils.EscapeStrFromStr(refName.pdf_to_name()),
                    Filter = Utils.EscapeStrFromStr(filter.pdf_to_name()),
                    StreamXref = streamXRef
                };
                imageList.Add(entry);
            }
            return rc;
        }

        public static int InsertContents(Page page, byte[] newCont, int overlay = 1)
        {
            PdfPage pdfpage = page.GetPdfPage();
            if (overlay != 0)
            {
                if (!page.IsWrapped)
                {
                    PdfObj qConts = pdfpage.doc().pdf_add_stream(Utils.BufferFromBytes(Encoding.UTF8.GetBytes("q\n")), new PdfObj(), 0);
                    PdfObj QConts = pdfpage.doc().pdf_add_stream(Utils.BufferFromBytes(Encoding.UTF8.GetBytes("\nQ")), new PdfObj(), 0);
                    PdfObj contents = pdfpage.obj().pdf_dict_get(new PdfObj("Contents"));
                    if (contents.pdf_is_array() != 0)
                    {
                        contents.pdf_array_insert(qConts, 0);
                        contents.pdf_array_push(QConts);
                    }
                    else
                    {
                        PdfObj carr = pdfpage.doc().pdf_new_array(5);
                        if (contents.m_internal != null)
                            carr.pdf_array_push(contents);
                        carr.pdf_array_insert(qConts, 0);
                        carr.pdf_array_push(QConts);
                        pdfpage.obj().pdf_dict_put(new PdfObj("Contents"), carr);
                    }
                    page.WasWrapped = true;
                }
            }
            FzBuffer contbuf = Utils.BufferFromBytes(newCont);
            int xref = Utils.InsertContents(pdfpage.doc(), pdfpage.obj(), contbuf, overlay);
            return xref;
        }

        internal static int InsertContents(
            PdfDocument pdf,
            PdfObj pageRef,
            FzBuffer newcont,
            int overlay
        )
        {
            PdfObj contents = pageRef.pdf_dict_get(new PdfObj("Contents"));
            PdfObj newconts = pdf.pdf_add_stream(newcont, new PdfObj(), 0);
            int xref = newconts.pdf_to_num();
            if (contents.pdf_is_array() != 0)
            {
                if (overlay != 0)
                {
                    contents.pdf_array_push(newconts);
                }
                else
                {
                    contents.pdf_array_insert(newconts, 0);
                }
            }
            else
            {
                PdfObj carr = pdf.pdf_new_array(5);
                if (overlay != 0)
                {
                    if (contents.m_internal != null)
                        carr.pdf_array_push(contents);
                    carr.pdf_array_push(newconts);
                }
                else
                {
                    carr.pdf_array_push(newconts);
                    if (contents.m_internal != null)
                        carr.pdf_array_push(contents);
                }
                pageRef.pdf_dict_put(new PdfObj("Contents"), carr);
            }
            return xref;
        }

        public static (string, string, string, float, float) GetFontProperties(
            Document doc,
            int xref
        )
        {
            FontInfo res = doc.ExtractFont(xref);
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

        internal static FzBuffer GetFontBuffer(PdfDocument doc, int xref)
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
                else
                    Console.WriteLine("warning: unhandled font type {pdf_to_name(ctx, obj)!r}");
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

        public static void UpdateFontInfo(Document doc, FontInfo info)
        {
            int xref = info.Xref;
            bool found = false;

            int i = 0;
            for (; i < doc.FontInfos.Count; i++)
            {
                if (doc.FontInfos[i].Xref == xref)
                {
                    found = true;
                    break;
                }
            }
            if (found)
                doc.FontInfos[i] = info;
            else
                doc.FontInfos.Add(info);
        }

        internal static FontInfo InsertFont(
            PdfDocument pdf,
            string bfName,
            string fontFile,
            byte[] fontBuffer,
            bool setSimple,
            int idx,
            int wmode,
            int serif,
            int encoding,
            int ordering
        )
        {
            FzFont font = new FzFont();
            FzBuffer res = null;
            SWIGTYPE_p_unsigned_char data = null;
            int ixref = 0;
            int simple = 0;
            FontInfo value = null;
            string name = null;
            string subt = null;
            string exto = null;
            ll_fz_lookup_cjk_font_outparams cjk_params = new ll_fz_lookup_cjk_font_outparams();
            PdfObj fontObj = null;

            if (ordering > -1)
                data = mupdf.mupdf.ll_fz_lookup_cjk_font_outparams_fn(ordering, cjk_params);
            if (data != null)
            {
                font = mupdf.mupdf.fz_new_font_from_memory(
                    null,
                    data,
                    cjk_params.len,
                    cjk_params.index,
                    0
                );
                fontObj = pdf.pdf_add_simple_font(font, encoding);
                exto = "n/a";
                simple = 1;
            }
            else
            {
                ll_fz_lookup_base14_font_outparams outparams =
                    new ll_fz_lookup_base14_font_outparams();
                if (!string.IsNullOrEmpty(bfName))
                {
                    data = mupdf.mupdf.ll_fz_lookup_base14_font_outparams_fn(bfName, outparams);
                }
                if (data != null)
                {
                    font = mupdf.mupdf.fz_new_font_from_memory(bfName, data, outparams.len, 0, 0);
                    fontObj = pdf.pdf_add_simple_font(font, encoding);
                    exto = "n/a";
                    simple = 1;
                }
                else
                {
                    if (!string.IsNullOrEmpty(fontFile))
                    {
                        font = mupdf.mupdf.fz_new_font_from_file(null, fontFile, idx, 0);
                    }
                    else
                    {
                        res = Utils.BufferFromBytes(fontBuffer);
                        if (res.m_internal == null)
                            throw new Exception(Utils.ErrorMessages["MSG_FILE_OR_BUFFER"]);
                        font = mupdf.mupdf.fz_new_font_from_buffer(null, res, idx, 0);
                    }

                    if (!setSimple)
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
            }
            ixref = fontObj.pdf_to_num();
            name = Utils.EscapeStrFromStr(
                fontObj.pdf_dict_get(new PdfObj("BaseFont")).pdf_to_name()
            );
            subt = Utils.UnicodeFromStr(fontObj.pdf_dict_get(new PdfObj("Subtype")).pdf_to_name());

            if (string.IsNullOrEmpty(exto))
                exto = Utils.GetFontExtension(pdf, ixref);

            float asc = font.fz_font_ascender();
            float dsc = font.fz_font_descender();

            value = new FontInfo()
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

        public static string GetTJstr(
            string text,
            List<(int, double)> glyphs,
            bool simple,
            int ordering
        )
        {
            if (text.StartsWith("[<") && text.EndsWith(">]"))
                return text;
            if (string.IsNullOrEmpty(text))
                return "[<>]";

            string otxt = "";
            if (simple)
            {
                if (glyphs == null)
                {
                    foreach (char c in text)
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

        public static void CheckColor(float[] c)
        {
            if (c != null)
            {
                if (c.Length != 1 && c.Length != 3 && c.Length != 4 || c.Min() < 0 || c.Max() > 1)
                {
                    throw new Exception("need 1, 3 or 4 color components in range 0 to 1");
                }
            }
        }

        public static string GetColorCode(float[] c, string f)
        {
            if (c == null || c.Length == 0)
                return "";

            Utils.CheckColor(c);
            string s = "";
            if (c.Length == 1)
            {
                s = $"{c[0]} ";
                return s + (f == "c" ? "G " : "g ");
            }

            if (c.Length == 3)
            {
                s = $"{c[0]} {c[1]} {c[2]} ";
                return s + (f == "c" ? "RG " : "rg ");
            }

            s = $"{c[0]} {c[1]} {c[2]} {c[3]} ";
            return s + (f == "c" ? "K " : "k ");
        }

        internal static string GetColorCode(float c, string f)
        {
            float[] color = { c, };
            Utils.CheckColor(color);
            string s = "";
            s = $"{color[0]} ";
            return s + (f == "c" ? "G " : "g ");
        }

        internal static string EscapeStrFromBuffer(FzBuffer buf)
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
            try
            {
                return System.Text.RegularExpressions.Regex.Unescape(s);
            }
            catch (Exception)
            {
                return s;
            }
        }

        /// <summary>
        /// Decode Raw Unicode
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static string DecodeRawUnicodeEscape(FzBuffer s)
        {
            string ret = s.fz_string_from_buffer();
            return DecodeRawUnicodeEscape(ret);
        }

        internal static FzBuffer Object2Buffer(PdfObj what, int compress, int ascii)
        {
            FzBuffer res = mupdf.mupdf.fz_new_buffer(512);
            FzOutput output = new FzOutput(res);
            output.pdf_print_obj(what, compress, ascii);
            output.fz_close_output();
            output.Dispose();
            res.fz_terminate_buffer();

            return res;
        }

        internal static string UnicodeFromBuffer(FzBuffer buf)
        {
            //byte[] bufBytes = buf.fz_buffer_extract();
            byte[] bufBytes = Utils.BinFromBuffer(buf);
            string val = Encoding.UTF8.GetString(bufBytes);
            int z = val.IndexOf((char)0);

            if (z >= 0)
                val = val.Substring(0, z);
            return val;
        }

        internal static void Recurse(
            Document doc,
            Outline olItem,
            List<dynamic> liste,
            int lvl,
            bool simple
        )
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

                if (!simple) { }
            }
        }

        internal static LinkInfo GetLinkDict(Link ln, Document doc = null)
        {
            return Utils._GetLinkDict(ln.Dest, ln.Rect, doc);
        }

        internal static LinkInfo GetLinkDict(Outline ol, Document doc = null)
        {
            return Utils._GetLinkDict(ol.Dest, null, doc);
        }

        internal static LinkInfo _GetLinkDict(LinkDest dest, Rect r, Document document)
        {
            LinkInfo nl = new LinkInfo();
            nl.Kind = dest.Kind;
            nl.Xref = 0;
            nl.From = (r == null) ? null : new Rect(r);
            Point pnt = new Point(0, 0);

            if ((dest.Flags & (int)LinkFlags.LINK_FLAG_L_VALID) != 0)
                pnt.X = dest.TopLeft.X;
            if ((dest.Flags & (int)LinkFlags.LINK_FLAG_T_VALID) != 0)
                pnt.Y = dest.TopLeft.Y;

            if (dest.Kind == LinkType.LINK_URI)
            {
                nl.Uri = dest.Uri;
            }
            else if (dest.Kind == LinkType.LINK_GOTO)
            {
                nl.Page = dest.Page;
                nl.To = new Point(pnt);
                if ((dest.Flags & (int)LinkFlags.LINK_FLAG_R_IS_ZOOM) != 0)
                    nl.Zoom = dest.BottomRight.X;
                else
                    nl.Zoom = 0.0f;
            }
            else if (dest.Kind == LinkType.LINK_GOTOR)
            {
                nl.File = dest.FileSpec.Replace("\\", "/");
                nl.Page = dest.Page;
                if (dest.Page < 0)
                    nl.ToStr = dest.Dest;
                else
                {
                    nl.To = pnt;
                    if ((dest.Flags & (int)LinkFlags.LINK_FLAG_R_IS_ZOOM) != 0)
                        nl.Zoom = dest.BottomRight.X;
                    else
                        nl.Zoom = 0.0f;
                }
            }
            else if (dest.Kind == LinkType.LINK_LAUNCH)
            {
                nl.File = (string.IsNullOrEmpty(dest.FileSpec) ? dest.Uri : dest.FileSpec).Replace(
                    "\\",
                    "/"
                );
            }
            else if (dest.Kind == LinkType.LINK_NAMED)
            {
                bool andKeys = dest
                    .Named.Keys.Intersect(nl.GetType().GetFields().Select(e => e.Name))
                    .Any();
                if (!andKeys)
                    throw new Exception("not same keys");
            }
            else
                nl.Page = dest.Page;
            return nl;
        }

        internal static Border GetAnnotBorder(PdfObj annotObj)
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
                    for (int i = 0; i < dashObj.pdf_array_len(); i++)
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
                    for (int i = 0; i < obj.pdf_array_len(); i++)
                    {
                        int val = obj.pdf_array_get(i).pdf_to_int();
                        dash.Add(val);
                    }
                }
            }

            obj = annotObj.pdf_dict_get(new PdfObj("BE"));
            if (obj != null)
                clouds = obj.pdf_dict_get(new PdfObj("I")).pdf_to_int();

            Border res = new Border();
            res.Width = width;
            res.Dashes = dash.ToArray();
            res.Style = style;
            res.Clouds = clouds;

            return res;
        }

        internal static Color GetAnnotColors(PdfObj annotObj)
        {
            Color res = new Color();
            List<float> bc = new List<float>();
            List<float> fc = new List<float>();

            PdfObj obj = annotObj.pdf_dict_get(new PdfObj("C"));
            if (obj.pdf_is_array() != 0)
            {
                int n = obj.pdf_array_len();
                for (int i = 0; i < n; i++)
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

        internal static void SetAnnotBorder(Border border, PdfDocument pdf, PdfObj linkObj)
        {
            /*
            PdfObj obj = null;
            int dashLen = 0;
            float nWidth = border.Width;
            int[] nDashes = border.Dashes;
            string nStyle = border.Style;
            float nClouds = border.Clouds;
            */
            // get old border properties
        }

        internal static List<string> GetAnnotIdList(PdfPage page)
        {
            List<string> names = new List<string>();
            PdfObj annots = page.obj().pdf_dict_get(new PdfObj("Annots"));
            if (annots == null)
                return null;

            int n = annots.pdf_array_len();
            for (int i = 0; i < n; i++)
            {
                PdfObj annotObj = annots.pdf_array_get(i);
                PdfObj name = annotObj.pdf_dict_gets("NM");
                if (name != null)
                    names.Add(name.pdf_to_text_string());
            }

            return names;
        }

        internal static List<AnnotXref> GetAnnotXrefList(PdfObj pageObj)
        {
            List<AnnotXref> names = new List<AnnotXref>();
            PdfObj annots = pageObj.pdf_dict_get(new PdfObj("Annots"));
            if (annots == null)
                return null;

            int n = annots.pdf_array_len();
            for (int i = 0; i < n; i++)
            {
                PdfObj annotObj = annots.pdf_array_get(i);
                int xref = annotObj.pdf_to_num();
                PdfObj subtype = annotObj.pdf_dict_get(new PdfObj("Subtype"));
                if (subtype.m_internal == null)
                    continue;

                pdf_annot_type type = mupdf.mupdf.pdf_annot_type_from_string(subtype.pdf_to_name());
                if (type == pdf_annot_type.PDF_ANNOT_UNKNOWN)
                    continue;
                PdfObj id_ = annotObj.pdf_dict_gets("NM");
                names.Add(
                    new AnnotXref()
                    {
                        Xref = xref,
                        AnnotType = (PdfAnnotType)type,
                        Id = id_.pdf_to_text_string()
                    }
                );
            }
            return names;
        }

        /// <summary>
        /// Convert sRGB color code to an RGB color triple.
        /// </summary>
        /// <param name="srgb">srgb: (int) RRGGBB (red, green, blue), each color in range(255).</param>
        /// <returns>Tuple (red, green, blue) each item in interval 0 <= item <= 255.</returns>
        public static (int, int, int) sRGB2Rgb(int srgb)
        {
            int r = srgb >> 16;
            int g = (srgb - (r << 16) >> 8);
            int b = srgb - (r << 16) - (g << 8);
            return (r, g, b);
        }

        /// <summary>
        /// Convert sRGB color code to a PDF color triple.
        /// </summary>
        /// <param name="srgb">(int) RRGGBB (red, green, blue), each color in range(255).</param>
        /// <returns>Tuple (red, green, blue) each item in interval 0 <= item <= 1.</returns>
        public static (float, float, float) sRGB2Pdf(int srgb)
        {
            (int, int, int) t = sRGB2Rgb(srgb);
            return (t.Item1 / 255.0f, t.Item2 / 255.0f, t.Item3 / 255.0f);
        }

        public static string GetLinkText(Page page, LinkInfo link)
        {
            Matrix ctm = page.TransformationMatrix;
            Matrix ictm = ~ctm;
            if (link.From == null)
                throw new Exception("should contain 'From' in Link");

            Rect r = link.From * ictm;
            string rectStr = $"{r.X0} {r.Y0} {r.X1} {r.Y1}";
            string txt;

            string annot = "";
            if (link.Kind == LinkType.LINK_GOTO)
                if (link.Page >= 0)
                {
                    txt = Utils.AnnotSkel["goto1"];
                    int pno = link.Page;
                    int xref = page.Parent.GetPageXref(pno);
                    Point pnt = link.To == null ? new Point(0, 0) : link.To;
                    Page destPage = page.Parent[pno];
                    Matrix destCtm = destPage.TransformationMatrix;
                    Matrix destIctm = ~destCtm;
                    Point ipnt = pnt * destIctm;
                    annot = string.Format(txt, xref, ipnt.X, ipnt.Y, link.Zoom, rectStr);
                }
                else
                {
                    txt = Utils.AnnotSkel["goto2"];
                    annot = string.Format(txt, Utils.GetPdfString(link.ToStr), rectStr);
                }
            else if (link.Kind == LinkType.LINK_GOTOR)
            {
                if (link.Page >= 0)
                {
                    txt = Utils.AnnotSkel["gotor1"];
                    Point pnt = link.To;
                    annot = string.Format(
                        txt,
                        link.Page,
                        pnt.X,
                        pnt.Y,
                        link.Zoom,
                        link.File,
                        link.File,
                        rectStr
                    );
                }
                else
                {
                    txt = Utils.AnnotSkel["gotor2"];
                    annot = string.Format(txt, Utils.GetPdfString(link.ToStr), link.File, rectStr);
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
            foreach (AnnotXref x in page.GetAnnotXrefs())
            {
                if (x.AnnotType == PdfAnnotType.PDF_ANNOT_LINK)
                    linkNames.Add(x.Xref, x.Id);
            }

            string oldName = link.Id;
            string name;
            if (
                oldName != null
                && linkNames.Contains(new KeyValuePair<int, string>(link.Xref, oldName))
            )
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

        public static string SetAnnotStem(string stem = null)
        {
            if (stem == null)
                return Utils.ANNOT_ID_STEM;
            int len = stem.Length;
            if (len > 50)
                len = 50;
            Utils.ANNOT_ID_STEM = stem.Substring(0, len);
            return Utils.ANNOT_ID_STEM;
        }

        public static PdfAnnot GetAnnotByName(Page page, string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            PdfAnnot annot = mupdf.mupdf.pdf_first_annot(page.GetPdfPage());
            bool found = false;
            while (true)
            {
                if (annot.m_internal == null)
                    break;
                (string res, ulong len) = annot.pdf_annot_obj().pdf_dict_gets("NM").pdf_to_string();
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

        internal static PdfAnnot GetAnnotByXref(Page page, int xref)
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

        public static void GlyphCacheEmpty()
        {
            mupdf.mupdf.fz_purge_glyph_cache();
        }

        internal static void RefreshLinks(PdfPage page)
        {
            if (page == null)
                return;
            PdfObj obj = page.obj().pdf_dict_get(new PdfObj("Annots"));
            if (obj.m_internal != null)
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

        internal static void PageMerge(
            Document docDes,
            Document docSrc,
            int pageFrom,
            int pageTo,
            int rotate,
            bool links,
            bool copyAnnots,
            GraftMap graftmap
        )
        {
            PdfDocument pdfDes = Document.AsPdfDocument(docDes);
            PdfDocument pdfSrc = Document.AsPdfDocument(docSrc);
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

            PdfObj pageRef = pdfSrc.pdf_lookup_page_obj(pageFrom);
            PdfObj pageDict = pdfDes.pdf_new_dict(4);
            pageDict.pdf_dict_put(new PdfObj("Type"), new PdfObj("Page"));

            foreach (PdfObj e in knownPageObjs)
            {
                PdfObj obj = pageRef.pdf_dict_get_inheritable(e);
                if (obj.m_internal != null)
                {
                    pageDict.pdf_dict_put(
                        e,
                        mupdf.mupdf.pdf_graft_mapped_object(graftmap.ToPdfGraftMap(), obj)
                    );
                }
            }

            if (copyAnnots)
            {
                PdfObj oldAnnots = pageRef.pdf_dict_get(new PdfObj("Annots"));
                int n = oldAnnots.pdf_array_len();
                if (n > 0)
                {
                    PdfObj newAnnots = pageDict.pdf_dict_put_array(new PdfObj("Annots"), n);
                    for (int i = 0; i < n; i++)
                    {
                        PdfObj o = oldAnnots.pdf_array_get(i);
                        if (o.m_internal == null || o.pdf_is_dict() == 0)
                            continue;
                        if (o.pdf_dict_gets("IRT").m_internal != null)
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
                        PdfObj annot = pdfDes.pdf_new_indirect(copyO.pdf_to_num(), 0);
                        newAnnots.pdf_array_push(annot);
                    }
                }
            }
            if (rotate != -1)
                pageDict.pdf_dict_put_int(new PdfObj("Rotate"), rotate);
            PdfObj ref_ = pdfDes.pdf_add_object(pageDict);
            pdfDes.pdf_insert_page(pageTo, ref_);

            pdfSrc.Dispose();
            pdfDes.Dispose();
        }

        public static void MergeRange(
            Document docDes,
            Document docSrc,
            int spage,
            int epage,
            int apage,
            int rotate,
            bool links,
            bool annots,
            int showProgress,
            GraftMap graftmap
        )
        {
            int afterPage = apage;
            int counter = 0;
            int total = mupdf.mupdf.fz_absi(epage - spage) + 1;

            if (spage < epage)
            {
                int page = spage;
                while (page <= epage)
                {
                    Utils.PageMerge(
                        docDes,
                        docSrc,
                        page,
                        afterPage,
                        rotate,
                        links,
                        annots,
                        graftmap
                    );
                    counter += 1;
                    if (showProgress > 0 && counter % showProgress == 0)
                        Console.WriteLine(
                            string.Format("Inserted {0} of {1} pages", counter, total)
                        );
                    page += 1;
                    afterPage += 1;
                }
            }
            else
            {
                int page = spage;
                while (page >= epage)
                {
                    Utils.PageMerge(
                        docDes,
                        docSrc,
                        page,
                        afterPage,
                        rotate,
                        links,
                        annots,
                        graftmap
                    );
                    counter += 1;
                    if (showProgress > 0 && counter % showProgress == 0)
                        Console.WriteLine(
                            string.Format("Inserted {0} of {1} pages", counter, total)
                        );
                    page -= 1;
                    afterPage += 1;
                }
            }
        }

        /// <summary>
        /// Insert links contained in copied page range into destination PDF.
        /// </summary>
        /// <param name="doc1"></param>
        /// <param name="doc2"></param>
        /// <param name="fromPage"></param>
        /// <param name="toPage"></param>
        /// <param name="startAt"></param>
        /// <exception cref="Exception"></exception>
        public static void DoLinks(
            Document doc1,
            Document doc2,
            int fromPage = -1,
            int toPage = -1,
            int startAt = -1
        )
        {
            string CreateAnnot(LinkInfo link, List<int> xrefDest, List<int> _pnoSrc, Matrix ctm)
            {
                Rect r = link.From * ctm;
                string rStr = string.Format("{0} {1} {2} {3}", r[0], r[1], r[2], r[3]);
                string annot = "";
                if (link.Kind == LinkType.LINK_GOTO)
                {
                    string txt = Utils.AnnotSkel["goto1"];
                    int idx = _pnoSrc.IndexOf(link.Page);
                    Point p = link.To * ctm;
                    annot = string.Format(txt, xrefDest[idx], p.X, p.Y, link.Zoom, rStr);
                }
                else if (link.Kind == LinkType.LINK_GOTOR)
                {
                    if (link.Page >= 0)
                    {
                        string txt = Utils.AnnotSkel["gotor1"];
                        Point pnt = link.To == null ? new Point(0, 0) : link.To;
                        annot = string.Format(
                            txt,
                            link.Page,
                            pnt.X,
                            pnt.Y,
                            link.Zoom,
                            link.File,
                            link.File,
                            rStr
                        );
                    }
                    else
                    {
                        string txt = Utils.AnnotSkel["gotor2"];
                        string to = Utils.GetPdfString(link.ToStr);
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
                else
                    annot = "";

                return annot;
            }
            // --------------------validate & normalize parameters-------------------------
            int fp,
                tp;
            if (fromPage < 0)
                fp = 0;
            else if (fromPage >= doc2.PageCount)
                fp = doc2.PageCount - 1;
            else
                fp = fromPage;

            if (toPage < 0 || toPage >= doc2.PageCount)
                tp = doc2.PageCount - 1;
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
            for (int i = 0; i < pnoSrc.Count; i++)
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
                Page pageSrc = doc2[pnoSrc[i]];
                List<LinkInfo> links = pageSrc.GetLinks();
                if (links.Count == 0)
                {
                    pageSrc = null;
                    continue;
                }

                Matrix ctm = ~pageSrc.TransformationMatrix;
                Page pageDst = doc1[pnoDst[i]];
                List<string> linkTab = new List<string>();
                foreach (LinkInfo l in links)
                {
                    if (l.Kind == LinkType.LINK_GOTO && !pnoSrc.Contains(l.Page))
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

        internal static Pixmap GetPixmapFromDisplaylist(
            FzDisplayList list,
            Matrix ctm,
            FzColorspace cs,
            int alpha,
            Rect clip,
            FzSeparations seps = null
        )
        {
            var disposables = new List<IDisposable>();

            try
            {
                if (seps == null)
                {
                    seps = new FzSeparations();
                    disposables.Add(seps);
                }

                FzRect rect = mupdf.mupdf.fz_bound_display_list(list);
                //disposables.Add(rect);

                FzMatrix matrix = new FzMatrix(ctm.A, ctm.B, ctm.C, ctm.D, ctm.E, ctm.F);
                //disposables.Add(matrix);

                FzRect rclip = clip == null ? new FzRect(FzRect.Fixed.Fixed_INFINITE) : clip.ToFzRect();
                //disposables.Add(rclip);
                rect = FzRect.fz_intersect_rect(rect, rclip);

                rect = rect.fz_transform_rect(matrix);
                FzIrect irect = rect.fz_round_rect();
                //disposables.Add(irect);

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
                    list.fz_run_display_list(
                        dev,
                        new FzMatrix(),
                        new FzRect(FzRect.Fixed.Fixed_INFINITE),
                        new FzCookie()
                    );
                }
                disposables.Add(dev);

                dev.fz_close_device();

                return new Pixmap("raw", new Pixmap(pix));
            }
            finally
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }            
        }

        internal static FzFont GetFont(
            string fontName,
            string fontFile,
            byte[] fontBuffer,
            int script,
            int lang,
            int ordering,
            int isBold,
            int isItalic,
            int isSerif,
            int embed
        )
        {
            FzFont Fertig(FzFont _font)
            {
                if (_font.m_internal == null)
                    throw new Exception(Utils.ErrorMessages["MSG_FONT_FAILED"]);
                if (_font.m_internal.flags.never_embed == 0)
                    _font.fz_set_font_embedding(embed);
                return _font;
            }

            int index = 0;
            FzFont font = null;
            if (fontFile != null)
            {
                font = mupdf.mupdf.fz_new_font_from_file(null, fontFile, index, 0);
                return Fertig(font);
            }

            if (ordering > -1)
            {
                font = mupdf.mupdf.fz_new_cjk_font(ordering);
                return Fertig(font);
            }

            if (fontName != null)
            {
                font = mupdf.mupdf.fz_new_base14_font(fontName);
                if (font.m_internal != null)
                    return Fertig(font);
                font = mupdf.mupdf.fz_new_builtin_font(fontName, isBold, isItalic);
                return Fertig(font);
            }

            ll_fz_lookup_noto_font_outparams outparams = new ll_fz_lookup_noto_font_outparams();
            SWIGTYPE_p_unsigned_char data = mupdf.mupdf.ll_fz_lookup_noto_font_outparams_fn(
                script,
                lang,
                outparams
            );
            font = null;
            if (data != null)
                font = mupdf.mupdf.fz_new_font_from_memory(
                    null,
                    data,
                    outparams.len,
                    outparams.subfont,
                    0
                );
            if (font.m_internal != null)
                return Fertig(font);
            font = mupdf.mupdf.fz_load_fallback_font(script, lang, isSerif, isBold, isItalic);
            return Fertig(font);
        }

        /// <summary>
        /// Adobe Glyph List function
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GlyphName2Unicode(string name)
        {
            if (Utils.AdobeUnicodes.Count == 0)
            {
                string[] lines = Utils.GetGlyphText();
                foreach (string line in lines)
                {
                    if (line.StartsWith("#"))
                        continue;
                    string[] items = line.Split(';');
                    if (items.Length != 2)
                        continue;
                    int c = Convert.ToInt32(items[1].Substring(0, 4), 16);
                    AdobeUnicodes[items[0]] = c;
                }
            }
            //return AdobeUnicodes.GetValueOrDefault(name, 65533);
            int value;
            if (!AdobeUnicodes.TryGetValue(name, out value))
            {
                value = 65533; // Default value if key doesn't exist
            }
            return value;
        }

        public static string[] GetGlyphText()
        {
            string base64String =
                "H4sIABmRaF8C/7W9SZfjRpI1useviPP15utzqroJgBjYWhEkKGWVl" +
                "KnOoapVO0YQEYSCJEIcMhT569+9Ppibg8xevHdeSpmEXfPBfDZ3N3" +
                "f/t7u//r//k/zb3WJ4eTv2T9vzXTaZZH/NJunsbr4Z7ru7/7s9n1" +
                "/+6z//8/X19T/WRP7jYdj/57//R/Jv8Pax2/Sn87G/v5z74XC3Pm" +
                "zuLqfurj/cnYbL8aEzyH1/WB/f7h6H4/70l7vX/ry9G47wzK/hcr" +
                "7bD5v+sX9YM4i/3K2P3d1Ld9z353O3uXs5Dl/7DT7O2/UZ/3Tw9z" +
                "jsdsNrf3i6exgOm57eTsbbvjv/1w2xTnfDo5fnYdjA3eV0vjt25z" +
                "XkRJB36/vhKwN+kEw4DOf+ofsLuP3pboewGISO7bAxPkUU+EaUD7" +
                "t1v++O/3FTCESmcsILgQRuLhDs/w857lz6NsPDZd8dzmtfSP85HO" +
                "8GcI53+/W5O/br3QkeJa9NERmPKgE2Ue+73vgj97Ded5TH1pPDEF" +
                "CT4/35RFFtAMORMezXb3dwiioCsYe77rABjjCOjHs/nLs7mx3wuY" +
                "FYX+HsEQyTfHg/DY/nVxa0rzmnl+6BVQfeegTyemSlOdjqczqJ0J" +
                "9/evfp7tOH1ed/zj+2d/j+9eOHf7xbtsu75jcw27vFh19/+/jux5" +
                "8+3/304edl+/HT3fz9kq3iw/vPH981Xz5/APR/5p/g9/+Qhb+/3b" +
                "X/8+vH9tOnuw8f79798uvP7xAcwv84f//5XfvpL/D97v3i5y/Ld+" +
                "9//Msdgrh7/+Hz3c/vfnn3GQ4/f/iLifja492HFbz+0n5c/ARg3r" +
                "z7+d3n30ycq3ef3zO+FSKc3/06//j53eLLz/OPd79++fjrh0/tHR" +
                "IHr8t3nxY/z9/90i7/AxIg1rv2H+37z3effpr//PPN1CIF47Q2LU" +
                "SdNz+3NjakdvnuY7v4/BcEGb4WyEPI+DMT++nXdvEOn8iWFomaf/z" +
                "tL8wZhPqp/e8vcAbm3XL+y/xHpPH/xlnDejXKHJTQ4svH9hdK/mF1" +
                "9+lL8+nzu89fPrd3P374sDSZ/qn9+I93i/bTD/D+8wcWxOruy6f2L" +
                "4jl89xEjkCQaZ9+4Hfz5dM7k33v3n9uP3788uvndx/e/zu8/vThn8" +
                "ggSDqH56XJ6Q/vTZKRVx8+/sZgmRemIP5y98+fWuAo8vc+z+bMjE/" +
                "Iu8Vn7RBxIis/q7TevW9//Pndj+37RWuz/AND+ue7T+2/o+zefaKT" +
                "dzbqf84R7xeTdJYYJLOf7z4xq11N/osp2bt3q7v58h/vKLxzjtrw6" +
                "Z2rOSbzFj+5rEd7+P84ULxH8/6vO/lj2/6Pu7eX7d3P6C3Y2tb3u+" +
                "7ua3dkA/yvu+w/JqyV6GeUt0/dy7nb36MjySZ/MUMO3Hz5+LNycsd" +
                "x54SB5wmN/XJvRh0z/vz1/PaCf4Zhd/rP9dPur/j7eDDtfIV+dX3+r" +
                "7vz63B36vb9w7AbDn/ddLseown7kr7bbU4YIhD6/03//e7JiM0O669" +
                "/vbyg1/hPdKLd8WGNPmnXoSs52h5200OGk/WW/fvdl0NvhpHTw3q3P" +
                "t59Xe8uCOARA8ydCcX433Z/rjfonfbrnfhP5j9MJtM0mbf4XZT4XT9" +
                "czt0Pk3S1ALFfPxyHA6g2A3WCz90Pq6qFO+dsskjdtzAB3B+7rwwDe" +
                "Wi/reu0nbcOeMBostv1Dz9MpsuJwzbD+b5DcuGuKR32dFx/pcfGO9o" +
                "Ow7MZlAj64M/9bmOAaTJ/WFuJF0t898eHXfdDNmV4JC77x133J8XON" +
                "CDiTTWq5JkvNMMLNY9C1ZLNa82RrIki9ULP50AZ/6pczOyn92DSE3I" +
                "qRSZs7nc2+gmqKMi+O3an/sQkTQOpszcLsBTnsg2gSEf/KskTQ4YaA" +
                "NrFPFn4b/ELIEo/Iu2jQkbg/QEtEJXe1Y6MtWP3sl3/MMlnqf08D4c" +
                "Baclr5KzEzHTuyXhZPyCXVhkcD0/DoXsmEwEfoWVQqsJ+Sg2eW9qniO" +
                "GQFqHh3n+XCNMWCMLJ3bc4BPB2vz5CYenXkKjI06Rhu8mSJlSxKmmQX" +
                "+uHB6g1jC0ztEQ+TRqdISmC6A46TLiH/sfMwBczE0mo4WrXHzoJpUyaK" +
                "CvglLnpJC1XiEWSBN55eIHcDChLFpQ4TxZrHWkL2mUXwl6YtoN6OLefE" +
                "myRLHy7mizwDT1yt1szryqhfCOa1AJJBtKVZFRtCd8WU3pATvFrbr5cH" +
                "lo6DometzoF0xmAbn3/vF2fgKgcbhbkKCCrCKBYETp0uZt+2siJ5pSGc" +
                "92+kOVgbLVIOREE/rw+jcJfNGSxGWBysYMmOzxrCU3qelSBOUV1VQCf4" +
                "56kXEGaqB4gykGJUKTJQupBnixZ9NNk+S+2ihS/0kkCjOoD6ccjhCO3n" +
                "iVLKfYW367Y0xY90TIU6MwSVkRfVdMM6HFYsxzpPGobc0NLrV4ky6htQI" +
                "oOA9rLmWTeIupuh6aRZaij5vPp2LH15zO49PmEMH1niBrcCCWd60KgH00" +
                "/BmgpkM8t9NzL/mm930scS/j7XYuHlr2MGiXkiwoDQvnESoFVyfKEarx1" +
                "uSGFA7ehkULobywiRPBNiqgAcbOCo9MFRwtGp1GVn6wSDuzTImllwJ65b" +
                "2mcAPyAjZxvfcTpHN+2xC0bZboApKt6joBDPZhbIgyyEeD7B7Sx9kZ1qT" +
                "WqKgeUkvZ66MUI1N4eejGytzeG3kgUP/QumFyVWyD1+EpSja9NICVYYqb" +
                "rSkvzJV2Xo0WhQfIedV+EsGU0rd23hAogyuUKtNZ7kBjOxTEPBT9LS/Cv" +
                "BlfE32OqDgVzo+JFfWt3uqkhATv4OEhYCFtGXrRhR/jCY7Is4kuCVWavQ" +
                "0QdiVoDqoiutekS9K0eFjpDy3E8nc75EdVjKGbtgVmg+1KkWtQAVp/hpa" +
                "PQM1SNl1O/YwryWeEJUS3gUkebwTnzDLP+DdtgG0jtClLrXh86SHu6mQo" +
                "Ib1r5HM1KWjmksEN7xQ9VsjVpEQ1ezvA7gUqMD+97RcpruAv3Le0G8V2O" +
                "ww/ZBDpq+40xQxPBh2/G6D1BqRSiKq7YJ5TJKjTdJlnpDjptk1U0phVwr" +
                "bvkabJy/S5Ut1UPnyELqgwIovM1Cm6jCoGgMDERdp6sJJ/K5EeKViU/Nq" +
                "c/Lutj90OeYwD8UVS6Kb7RNzMrc/sZhqsZmYenfh3EnCc/StfWJj9KniA" +
                "e0WFSKFE/hpxYWEK0k5TAwIh806Z72+hRd37UjZ50NJBBxu16o3UD+N1i" +
                "HrjZ7LpRfab42+5KJ5gZH5eX8+WomxFq+Y++BBALJnWqVgGIRywArlFjJ" +
                "gefUXkgf/142NpPKQ84le/KfdtYs1kD2gjLDJ0mP7Hg6uSntEb8P2TFYm" +
                "W+p/xGo+B3kfK7SX7CQF4ZPE1++lUKGh3sT+tbAx3G5J/WN5WyDIzj5tQ" +
                "/aecZYrMDKqraT6b8fWshK2gxGcINBb+0hBQ8uuifpPuHY4SlmwhqwU+q" +
                "g6frKFcRttbIphPQR9WCwJesxfcF85bjZb9bX84siFWEiBYBh98kv1AF3" +
                "jHTZ8k7PUvMVsm7v0F+TCjefdF4m7wTJWDpvmXIAeBbSrZI3on2gcBCFr" +
                "WWCAN8BEhYRFXlK5N3elStQapRdRVIP8hQ0huaNirZu6sBmN5NW8wn5kv" +
                "aoqNFjZgn77qrpQeIFrXXInn3eFw/o62hZ8IU7Z2M0Qv3LREDiNQOJKvX" +
                "QZEej8mQoT9th+NZO0TxyYCL+ukInW4UZFS14AO1SrX3Jnk36ByH4DIyM" +
                "jMHO/jMzJfqMEsDhNLI0VCJyIAEUiopfEt7xzj2zk2XU9T0d9GQxPrzbd" +
                "ufT9GgMPWgrwuaWSZ/Y02eJ3+L5nZp8rdQ+VaWkPaJucrfok6uTv42mog" +
                "1yd+ijEP4kpx58ndG2SR/V0NNkfz976E/WiZ/X99DZ3/uoxF+AtjV1Nx8" +
                "q8JEqDd7qhkZYwUmB/byYoqG7OuuvwX63cnibJH8XQa0Gt8yoOUlKJ9v0" +
                "JT/Ho9fZKuWgX7i7/FYPwUQLU2skr9vdTKh0/19q9UBhOgHI0gSjz0QU8" +
                "+WUGx/jwoFJTAgF5SXemIhmYEhH066cZUEfEE2yc8syEXyM3s9aIU//4y" +
                "uEtXlZ6815DN87+83Jqfh3OdavsR3yDVyJNdSS8STlByRjPISnlz/szJf" +
                "gWNp8VoGUoZiqH8/969RViOG35kMcOJsRBqibJwnP0fZCI9+gol2Y79l3" +
                "IBnya9F8gvza5n8oip+mfxihVqVUD7tt0yJVwRchW+TX0ImZckvekjEGP" +
                "eLSjJ0nV+iejSdJr9EMkMGEQvfVHGMioqq/cuFhbVI3lPWNnlvynaevPd" +
                "lOs2T974coS++D+WIye77IGJuibgc0dG8j8uRnqKkTA0tHsrkPSv4rnuk" +
                "69kyeY+yEBW2Tt6bQmvwGxUa4tGFBv3ofZQBSNjwqnMI8UiOgOmXJJep+" +
                "5Y5AQCTQ8vkA3NolXzARD8tMvxKqc+TD37AX+buWwIAACXpGM1y0I048N" +
                "bwi+C8ioAS+eBzH7J9YK7Bw8aPCTPIE8pgaglRG5YR4KsW6t2HmysAy1o" +
                "z/LxzmWlUD8Vx8JLgCPXzKWgAH3T/jXRhfPKVrJgYUlSXBcigutDvrXxS" +
                "sEROTCkjCMiMz1JUDQCnajBhkaqxAhD1zwXoPeodVNIPkQ7Skj6yUDBIm" +
                "U/J3LmllRBtZiHJ0IWlo6x0IfrsahmsVlVtHvWMEcFdKTzwLroNeugP8W" +
                "ICa2u8mMDA9t3T2iWOn7rbd1w/LmCKbejjcDnoalzNLX7uzzutF1ULh3v" +
                "1BrV031vx8pkQwqZz3VrhQjV6CCNKFtuGJcJ+CXy7FQn0rh9c3zxhZTbf" +
                "MqVtHSDFTRe+D0CUduDXzrX6WJH2vUThvn0GM8sNoOYxU+9B4iuSX+EZW" +
                "f+rFMw0+TU0X/B111iUya+R0rwCHaldcwA3p7hzeLXr2/ywCsMccRkI8f" +
                "evR13P8+RXnf9Qtn49Gac1P3QmkOOSg+//ZnLS5L9DEsrkv6OQwBT3afK" +
                "R7rPkY6R7LkD7bmCafPS9XVHjW8Ya5MXHEEsFIhpVyFb9RzoBqXOyNrRv" +
                "kMU8kKIiFJAj1s4QiJqjgL0dmCdIRtjbKlcLknFrTJFEPRoVbfIxyhXwJ" +
                "Vf8tw8E/ut0hJ0uLx2tXMBryuQTczFPPq24YzeZYHqP/hJU5qh0Sir31I" +
                "TU1FM1qcJRufFXOiozVOV5JpTa+zO8mXdJnoncxM4YUpElI+VdlimozLs" +
                "sycu8SxQaKC81OltQXuqS6cu81IUJxUtdVKS81MWSlJe6oJyZl7poQOXi" +
                "siUlLlekxOWclJe6YPqmIvWMlJe6pNRTL3XJtE+91IWhvNQlZZl6qUtKP" +
                "fWylCyHqZelNPF5WUrmxFRkYeyFl6Wgv0JykPlZSA4yzwrJQaa9EFmQPm" +
                "ll/ls3EYqw3r/0vsvHAPTJN8XSf0ceSgdKS0BBqAaLzH7YvvITvb/51Os" +
                "BtYVubaNDutDSa0vIXJTlGzX9jDU6kmtiaN/2WOU8GTmDt7gzhfjR+jzS" +
                "F2+AVgT05AxBbB9iCIUVzdcQ+zZy0SB5236vlk6Rov7JrLTOUYD9nyIAq" +
                "kHUa4A7PJ7Ha3DwLn0JXJwZlszn5slndhbT5POaSiyGgM92wQ6p+yzFCz" +
                "QUHDLsc8j/mSVirR49/+e4/6WnKHfnhpZCWCSfow1iOL+5+Tunw1AEiL0" +
                "7n6KNW8i6dbv3NT7d0LbgJ/WxCRQp8ymDLmlkh4SJqNWgXJIfzwyh4n/W" +
                "vTemB5+jcoAIesERk97PUEgee6OwNwtDnXrW1npqiPPrQCGr5POxg47h1" +
                "WhiCDtKH5Sxz6d4Z7EB4gsY4b12O7XkD+brIFSafGFxF8kXmY7M3bfkBw" +
                "A/uUCxfJHJRY5vKfa5JcJEotGA1INSoxID3aoUIWCl6aPufNEj9RSk0vQ" +
                "XgfQ+llXAJOYsYJKCmcKU2cAkwC7WlMm5NtUpAihpoTxKk4e0MnuYuW9x" +
                "C0Cr9JiefPGThJX99Gofpn9fRpMEiqknCVB0v4wnCegqvkSThBZ0PElg9" +
                "mpIZwTy7EpTgYxab6wgmGQIGvGX6zXS1oNK1a3oUjcRZKWo7Cwr2SacF5" +
                "5I2T8Jy+QM03p6298PO+nAcnEgi6lN6jG9ntqMwRuBTb2bwIuEkPkI0mh" +
                "NnVI0/i/jheQJMd8ikR7MG9bcJdb9WBvga+MTlJGfv2MY+hLNJCoPSFWf" +
                "Jv9goy6Tf4T22ST/UHUHU5N/RBOFDHS02gEHrsdpwIuKCuFG2yd18g9JH" +
                "Hi+rmFK90+KUSX/9KLWWfLPINLCEjJSQ+5/qipSk1QjBKZq/1RJqOvkn7" +
                "7q15Pkn5GIiFNEqpL/oRh18j8h6mXyPzqmBUgd0zz5n2ikz+Ges5tZm/x" +
                "PFA8ClXjq5DfGM0t+k6506b6lwRPQpY6x5bcgVWuJkCFl8luosSljuOpu" +
                "VsC06K2hpY+YJr9hHqA714bI5Va3h+B9hqLl/+aLP7efvktZQSi9wzEtQ" +
                "Ou6XoGOhkfonL9FuYYsklzDt68wFOByuu+fdAbNHXbLYGJB3q4/n3e6Lk" +
                "NREfiWrzr5F8tpnvwrMq8qQfsRZ5aIGVa1dN8y/K8ASJE5whVZ2s4myb/" +
                "sonPVmC9ReBztS2aWJf+KWmAF+ub2RE3GDa23BW7VGoi+7XRa5gTGO2qL" +
                "lKiO0vi7Gafl3Ih0kfxLazqzafKvqGgRsxQtv/2uVFMktEmEvrFe33cYb" +
                "XZoTzM06bVvLC1Zm+4rnM0mxJ8uv6+P6zPczWtLH/eXZ65RzA1/v0Z3qc" +
                "C8BXi8yML5JAf9dYD2QwU4RNq0Gncx5hGooqbre2Zlb87D7NfHZ121VxF" +
                "XBYhhVScUyb8fXob98Dj8kNN+ay2G2Ln7FkvnlQN0vqcO03ZLlcPEENs7" +
                "igySfPBipgJRZAsZiZO6vJxYQlQ4TEXWNwyxC41qq+SlZoghdqXRyBB5p" +
                "jlict0kvkZAczefJoKH/T2qelpZyFKT1FFDRLoSKJx3LtkMXCRBYzUABm" +
                "0XwJQ+Qi7nyAG9pgzuZrN+VnWsIuTqKPJB6aFQ9G7OTfMAB70RguiMSw0" +
                "ZlidBmxaBWh4WF5G73fNw7FDvcq7srrvgAZE89v2EO/g/QOzCkvVsmtL4" +
                "aGrIdII+yFqqe7K2xs6enFlFwJHZxFrJeDK11p+ezOyevCdzu7ftyantX" +
                "jxZ2A7Ok6XdhPdkZbfaPVnbzVpPzqwpnCPzibVj82RqzdY8mdmNAk/mdg" +
                "3Uk1NrU+bJwhqLebK000xPVnYm4snaWgZ6cma3Wh05ndiJmCdTa9Lsycx" +
                "O/T2Z22m/J6fWLsaThR2kPVnaGbsnK2vw5snaGo94cmZtTBxZTKwxkidT" +
                "ayDrycxaH3kyt1aWnpxao1VPFtZaxJOlHeg9Wdk9fk/WdlPUkzO73ebIc" +
                "mKnqJ5M7Ua0JzOrLnsyp8WNSFVOSYpUZeEarSMpVS4FWlKqXNJbUqpc0l" +
                "tSqlxCrihVLiFXlKqQoCpKlUvyK+ZVLsmvmFe5JL8yUknyKyOVJL8yUkn" +
                "yKyOVJL8yUkn51kYqyY2aUuVSvjWlmkrya0o1FZlrSjWV5NeUairJrynV" +
                "VJJfU6qpJL+mVFNJb02pppLeGaWaSnpnlGoq6Z0ZqSS9MyOVpHdmpJL0z" +
                "oxUkt6ZkUrSOzNSSXpnlGomCZxRqsInEADJXEhTglMhKVVRCEmpilJISl" +
                "VUQlKqohaSUhUzISlVMReSUhWNkEYqn8A0NVL5FKWmdU9WQpZ2DuDJypp" +
                "oerK2xjmORMai8ovMJmMLCcpkbCnJNxlbBZIRVT75NbpNBFUJaUL26a2N" +
                "VEub3gy5nE1cg8y5MDxx4mO4JWHLrqhyVs6ynAsJ4UvXrkGyVpTlRMicZ" +
                "CrklGQmZEEyF7IkORWyIlkIyYjKUsgZycqRU9aKsqyFNELOhKQYbnAhyZ" +
                "DdeEGSQWVeyCmLsswyIRlUlgvJBGZTIRlyVgjJBGalkExgJkKmTGAmQnK" +
                "YLjMRksN0mc2FNFKJzJmRaiGkkWoppJGqFdJIJQnkMF3mEyEpVS7p5TBd" +
                "5pJeDtNlLunlMF3mkl4O02Uu6eUwXeaSXg7TZS7p5TBd5pJeDtNlLunNj" +
                "VSSXo6t5VSE5NhaTkVIjq3lVITk2FpORUiOreVUhGTrK6ciJOt5ORUh2d" +
                "zKqUjFwbScilSFEUOkKowYUgqFEUNKoTBiSCkURgwphcKIIaXAwbQsJIE" +
                "cTMtCEsjBtCwkgZURw+dkwZ6qnE+FZFBVKySDqkshGdSsFpIJnHsxClOf" +
                "q5mQTFEtjk19nqVCMkXNXEgGtfRCFqYElz6fUQ+ohXrHJUuhaLyQJRNYL" +
                "HyRoZ2DXE6EpONlKmRJMhOyIhn8MqjlVMgZSRGDWVcsSyFTkpWQGclayJ" +
                "zkTEgjlSShMlI1QhqpFkIaqZZCGqkkvZWRymd7ySG+aCW97EWLVtLLIb5" +
                "oJb0c4otW0sshvmglvRzii1bSyyG+aCW9HOKLVtLL/rloJb0c4otW0jsz" +
                "Ukl60T+vmiyQBUmf/Ap97KqZBpJc6UUrdm7FaiIkxVilQlKMlU9ghQ5q1" +
                "Ug3UnGYKJqpkExvE7imIpVCMqJGxOAwUTS1kIyoqYRkehsvVc1homgyIV" +
                "kKTSokS6HJhaRUi+CYUi2CYyPGTEgjhq8bdW7i9XWjnpqIVkIyooWXasZ" +
                "ONXN+yzRDB5WlTicHiSLLUjdBK9McXVCWujlXmRY04p9kCyGnJJdCFiRb" +
                "R7LRYSh3jvO0NCOsczydcSqUUWa/kcHqqldniiRanAG57Y/rp/Vh/UPOk" +
                "7jraNoPifuwMsL5Sa+XRiBU76bYnKrGR5URdK9iNp5V1MbDeF2IXTpvUl" +
                "nfMwwz0PSHRyA7h61ogQ4M/517jTZE990mAhcER7ZUTNKNlSaqVP14pWk" +
                "agSoxdP28PuOvybd5Fsjtevf42m/O2x9WKy5ByDoAR5Fd9+i6THxJMqld" +
                "gN6sn7rT1iwGvrJpWVdx6uvWgNv1/tvalFIIJB9xRh6ngW0WM4LHYsQZe" +
                "awt24olwu/WyGyR1aVtzzWYkVjZiDMK3bOfT5fjWnxxLA9w7GU10bxxRV" +
                "jlmjuqECubCS8oqpDPmc3SP7hIeQqoSdHLFg2Vfdxu1/1xWe9+yDJqDu6" +
                "4PXsdfdx+DlY4bg+mXm6lHrR/6Y6n9WHzAxdWAqmdTRTuV2eN22BPjyw7" +
                "qFbIHD48aWBK4Hm7PjxvL+ftGhWWRlHAuHaYcVWFn/fH9cNzdza2uJgt1" +
                "FeoN5lHxnEiq7jmCiN6ml3DytfUxWSiyPLMuba+QRuZuOxsrDDRgg/DGY" +
                "575m2NNnG4bNbns1/Eo2J1uJy+sjTDYm0A/VpfQHS/BzRcdoACfVmj2ML" +
                "684TIsTv8kPFAwPploFgv0Uo9s1Bwu0rJ/v7lBbm6qlcrfh6H9cO2OyGX" +
                "qSSS/lPqTa2B4Yi+74nFwWQZnJ1ht3sT9xDyuO7UQiLbPpEAoJ8/PiAn" +
                "uRJocpWdj9nbTNvZnJi50YF6RnSjQ2NpOXmNqnk8Dq/3w5n1fTa15GZ9" +
                "2m6GV9oeUI/xkC1NXmQhkCtRXm8i2OWFgAt5c79zgS+ngriwl7kgLujlR" +
                "BAf8jITyAS89AHbMGZ5IF0gs1mAfChUqD32uu2RGRDRuUNZb4i79ecioA" +
                "zQoVlATZgOzgN8eXGYS+cWJf2t+xM1hPocES/fJJBIlUq2Q9x+TMYrWAR" +
                "HB3r0qeH6gsclNQ6TFGeKjgJdKQYE//r2Q1bNWgUyKierT4zBJSqXmWfe" +
                "CmSrxFQQqREuH02hzVJPbEyhFYG8PzHIeS0ISuJ+PQJ9zpUaGB5dHVhIc" +
                "JL4yiMis0OMTmAKBWGdHvrebm5wr7HVQLRf5jjeTLjStHZogzj2LzRg4+" +
                "zQEv5Yhmnx9gio0rxSh2mtYoxp1YLLJife8HZ65mgyF2q9456JjKRUDT3" +
                "nBoY+B60yS0No0WAUgnVjUcuFIAuh0zYKo5ivrkq2pdPb/uU8mCFAdWZo" +
                "IWcesEAV9/nHPuUcGYaTKfGgjwo5Bs5F6aFTkmrAI9vroeRptdPSQe0kv" +
                "UNQ5y33B0OgnF5ervRRdPCXW9pihHttMQK1tgjGV2rkWz9Icdk4ugqH2f" +
                "rWH9wM8o0KD4sxqCMTg4oWBlf33KPFjxoNoYDcYyT2RvKFIqOaTNxJkvF" +
                "byTq3tOSA4auKWk1In51aAb3gXivCS3KPbBz0doxaBRBVZhiD78N2Zprc" +
                "Rxeb5IaW8QluO+pyp/7PcwcnWyoKGGXLEoF2D+sLO4ospzO9RYhQaRriN" +
                "dGaZKxLohMGNtYhZ8ajSvOM9EiXRM9qwG4/8r6YrYRzGnYY1DfCmhgZDs" +
                "MQT2oWaJH3nc5HxqjtMljQ3dmur9xbU4LGQOuRFRQTdLYzCc4h0kCGiYU" +
                "Bg0JvSGjZobahJt9vdb1akvY1xhC6yjgg1BkC9nh7gZLsdVaS1gklvUMu" +
                "rHcPKDVzIh551B82eq4Ine6+V+YCTMEONdtXIJ6SNwBKCHVuQ6R0CAaHl" +
                "6E/nKHvQEF1SjBn+YbNEcSzzW93pOfpNVd5xqzfscF5uKAYY106/d/4Wq" +
                "tuvuPO69dp+r850CH55PCWO8aipEU/G3jGo2ZmlnnsHs4em7vAjNvrzGn" +
                "mN9g6a13Om57cFZm5u8Ch/Q7uH9kpZKXPgeDMZd3pjG4kK9nySZrb98bp" +
                "mireVbqCRyehEUeLOR270EyTLYdn9E0Zs09fU1SBHlBTswJT4/toigdfw" +
                "z1XNXrXP6ZI9aCrP7J20NUftMw70Gr+CLM8RIuy7oyWgnmrIey5yUnVBP" +
                "L+TH4egH2/IZIpRPfCyqsfajV2fqHnNAC6klUWtrUTYiwVbeVoFeIE0Y4" +
                "iSTRDRFko0MqiES1MnehGh8Gu0YAVZ6Ihq++tNBQNipF/E3fbJlGDRCTL" +
                "CLGxNBFmC2weYVE8cRA2keju3frUsk7CVRvW8iVrLeQMaUpLycKWcriKW" +
                "c4OJ43RzXCBwm55JXn95imKbu6wGzHk5GECcbCj/ByyiNlYjdzWuiCchi" +
                "u5UEEvuh3A40W3A9KY/p251Jm5bxM/R3au9VtoQPCYtx+pss4MdureTJf" +
                "cJg/Uh/LkQVsKloDVOIY58YPc01fh2yuNxLXSaOmgNJLehWPeNcjDhoP3" +
                "YaP00jrVuMv9icb8GkXkUC9TkPFysv0Lj0M+IMbh0a4lO0uwbFHZT11mC" +
                "wu5KmIo9GZP3bGjEg3/DfzrpVskQe6kW+JbriLEFOlhfBXhDJDoapklwr" +
                "2D5F6OO472iMRdQdiYr3AFIenQucGdRNjUnnBpgQDGE5dV+dU/cXGHeZB" +
                "b+vDoK9lyZRDdvtqJgYbd5nR+49JM5YLRdRNuotM/0PAetMIza0j72mEI" +
                "XT0cEOoHAZ27U9C3b1NckvPwzLkHJtxpbsjAn1YE/vfLFVeRE82xnm+YC" +
                "xdkaCvpykR8+3LFBVnfv1yRWUUDa1bDbd9deEbKVA6/LpVVgWMGN2Gkwh" +
                "j5KGeeEZbL5x6Kw2B12w4ImlM4M8hO5h7xQG2BPjhxnobOA0yku/EQrhn" +
                "PVSpKh4/S4OBxClwoQX4HjKR36GUUKMQRXbZx3/vL7ty/7N7Q2c0qh6Fx" +
                "gZo56mV34VrjrPD0AL1pZ+pWjs7dobxTnWMalw+MysMedaKYsnQo3DTRT" +
                "TxblMnofJBrqkuFu74HjW3XUXkzDZk6/Xr3tcM8iOPAIrPQhnfW7whMLM" +
                "Bp0tEiqUXkMBUx1Nbd5Z4TPvt1uvRnJ6yG3DIPbUoe9g/omUOXM0eTjHQ" +
                "1+HJr6soRpNHHJdgdD+ZoywQjn/nc88TX+vjGbfJUIAk2dc64AqCciH5T" +
                "WNqqmlTome12xXCZjnkOp1DmsjbuEdqTedxIceNLriBTkA4vEn2Ib1Uuv" +
                "EM/H574wNQS99JCqodtUwtFy0LOp78NT4szjVlundyFK9ngkqS75MxCds" +
                "1HhxgxXHgNsRd0XZxDUJrD0/HCdJp1c75NMFyOnLA8Hc36E1Qo82DBAIL" +
                "G5o6YL3h5ETQqRzct78ChZuBoHsZmk7XkYs5rVNJA88Q7R09LLhcp2Wmg" +
                "M9JZoHPSeaCnpKdCm9irldA/89JRKhCWbnnhDNQeT77nAf1JIfQHngadS" +
                "HDtJ15VzKHJ0Z952XJaBZpnbUJmrHidoSlaSzLtqZA/GlLS+pOJS2T52f" +
                "ide/L9nPmaimgfjWcpg0+8b20i6fzEq1cmgWvTIdn2ycop2frpi0mHRPb" +
                "pN1MqUohfTGQS+j9MaMwF9/QGFYtZIE/rw4m6voZQKR+pXRBDrRtN700e" +
                "jeBoaTa75utdsTRmy2ba8gYehZvfcKADNvG+DEd7vsF3aqZCBdWL5Q9Pz" +
                "08BQtbJJBTFcLx863p7FyZChALQnalWcGkGnqHpvXELM6ONvqGMOk4F/H" +
                "JEIA9vzGDUwrejuVOb+ZiSWrEvX9H0CMS9ZxmHj45VJNwaLafJJlLiSav" +
                "FqBLkJtgIGNItTZnveImvaYmNl/igRAEd2wtMErdyZsxAomUzjzxxDWSS" +
                "Tdy32bmZZClJtSJWGjosiJFW05+S3tX0x0S8CyuVFG5nl/ty+xlW9CIgr" +
                "Ok5eItA7f628XxnLGVGnLDyd8U/dU88Nek46Zgz8un5AXVAf+z/EFdTBY" +
                "4C8CxoB3sBZwocuXesOH2VAkfuHctu7Qtaa3Tkw/Mu9xflo9HoyIfjxTl" +
                "XKnDk3rO2pso6cKLAkXvHYqfUCVgocOTesOImMJ8D00P/dGUBbQbisfP6" +
                "MNpCmi4CJ8IOvApuZprn8SnIPa8sYPrFCMRM4+XQcZdFjvKYQX5aQ+r7n" +
                "b8/lfWIy2/XRgrzWwy9KrQcO5DetbnJ0X5b4+LIecP10or1rvZv0XN5RG" +
                "1Sc1vb54tJ05NPUymUU5RXBLSOsiCAGLnayKNBlaLd8ovJGLMxGzATzsu" +
                "x33ujBJNJPmFcf8k4OiqMnpWGNWHC1c4MWtl9GBzQImShAFGpy+vR/MOq" +
                "QG6J0W3kRP3l9XAedeOG9h23IXQP6oDQhRog9JGYtW3GFb2pIfpmIxP3A" +
                "jm6ifYxskSxM0vpWD0SoiWid6YaQ8tiMOqbfQrm1L2szdJU2GVtrni06z" +
                "FjmmOqvSrUpo6bOFwQQZPvtn1oOktDh9EDFUPfQoJS0XtHC7LROYjZTeN" +
                "osbspCdg9pKn9lCsDa8Z1GPbIVsiLn8sJXcHhsrfrbiErV8j/jvdkZxjr" +
                "40yuEpXHhtBZ7ICQwwTcZhE+MR6/nblD5E/rFyPMnQacJrLXwxMFjogmg" +
                "Si6cOZvXifx1RNoklUS3TzhWvpUUNc8gk9pzAGK5NSFxNh1qZA+nwc3OY" +
                "faven5JhtEW1Xum3P5zDL4wpLdxs0y6NGb6D7EAmE9n7ZmUayYwUO0P4H" +
                "qEJYqobFtwj30aEPRHBhJPchmBgguomzWfokE3cKAmuW3MsjXCURb01sZ" +
                "C9I7M82fMA/Nt55I5g6LZpLeoVquE89iCuBD1tNFOjo8UUdF9R7U3iBrd" +
                "1h4zJazQLryrBLfgl2J5wEYFKISt2IkGGxOvDgtzVNP/c4rUluh7GKZq8" +
                "0mQ8/OwGJRkOCavCzzoHMyK/Fvw8YqNMYSO8ZEvzOc1wMS8qyP2LaCurU" +
                "CRCOqPLzoHEMSzuveLNMii8LSPOTQS/MctvTSPCU3r2kgT75ZzYCNnpQc" +
                "TS5J2CXgOZ3ffmcjJUdXYzqNVj+LVcIGARE6OWo+w/eReciTJJ1abIdbv" +
                "eS6SDq5ox7+7fq6X29fekCvtQt4ZchRXHG0NYfhuhbV4Hv0uAeD1UutTM" +
                "3D9i2+Z6GuAMrgObVEOM0914C8+LHSqIyxM43q2zErzZAXP1KNRtde5po" +
                "jb3tQelVCEFUfuwbX5zGk02eskTPuSY8q6aInPSwtR+Mhf6f3+hFOd2WH" +
                "Az/63Q/0XJ1YuNf4VsUK/1H2w2u0No/y0YZX8B2dwYfckY07gnOrBnltP" +
                "8MI74BQKdvWIlK0jD0AbkeLSw52jSGrZql14HKxdAF0mEj7MKpUMN+2Md" +
                "oIxAa+YXufWUzlhRdH5aSPYIs+4yohXFT/th0uyJfMQzS1sdY3HFMbi2K" +
                "wGpD/L9verRzkWeZSKl1+NqldGNECqcNUh+/z1SeucpFIyuqVAE59Wjkv" +
                "/m6sykUu/V02qZwTbwBNcnwWgL5u3DqCzNVmeHUgI+N+1MHn4YBc1JcOG" +
                "NCf/AehX4nJkbBdt7frlFArOvNkTKgrc4dIRrQekDLOHCIJp59d/8JGl9" +
                "Go3FMyscky1oKgA+SekLdoKo/IWzTIAP0WTY6+db8xygiXK+23njmhgkZ" +
                "6Bf2/cAA4je/gaMg5v506kwVwF1myQzY9YmA21x18vLn71vFmxG5dNEfH" +
                "5g2chh86CkY5ehSH0PhOeRTOwSbHPGHZhRdy0MqGUMKIyN5OmzFp/HzYD" +
                "Se7WDa3QHgzBoN+DInboo0ZXiFGBvjKMJ/g21+0hVl+F99qhUmCNbZEP+" +
                "U+o2bnMNGpSkerBrMg1H/FvP3AdGclivWo8w5+dC5PIZFOXB1I7Qox671" +
                "IjuK3n/xBBnLpLatzfjh9oi5JDEffQUIrtfTVoG0cegF2w/DCq9nmBKkb" +
                "npWk7D2vDHArh+mWP8ai1VgGfTZG+xseX6BcSttCZtoZVsUPNRzVpKXU4" +
                "Ms8VbRCXsqtL0v3LUM8cuaM2M/rxwH9jEwMOXYoPFpvCbwb0LVLP/9bIu" +
                "6LVG/WAHkVqbtlB1sp2BeExrTeBPzPB7PSxwVT+637hoXD7JpqLiTNuyf" +
                "cSgu03KnvwWhS4UE5P0MAUzXaDpgeEbMvO3dlf6reeFoZyla8mXGjH3ya" +
                "EbAqdNrMk0dqqmXyKKsNLb7VUGBoBHDYdj1XhyYz0OetWoVrLRCtwjksW" +
                "mtrkke9PlMnj0F1LJLH6MWpVfKobF7R2B4jbQjN6XFsBLvMiI1XyJc50d" +
                "EKOTTVR730gNgxdlASHvt+fMRMZcLfnh8I4HHHD3gyAITpHyPVBtqIg0S" +
                "zyQSRQQ8y0xq080MBnex2GMeHP63JoCVpw2jNF036nteP9iCwp8Ia+hgL" +
                "y+iBE5ZVAxYWkud2sThmKC8xWxZ753ZFN8JHvhx33+3tyWRPBWcOO1wO9" +
                "nSyp4ILh7109giyI4LxuIP4ikxvzyEHOrgiejydzRVMqB7diToTpvmPPe" +
                "S2Vlck4kfLGLRRy/PCfAUd09JKV24MEOrCVNE3NOW6NXyvKFvfVkeF7pM" +
                "WSwNo7bdxSFB+LRLrvoXDguprkVs6rhVRq7jWbTTUWkgruBYRta62pKi3" +
                "C0977da6Fx3PxqqHauvAq7agTDtDu+DBMvMmEb4jlQxtKBwhxFThcXgUe" +
                "xl2GsOjX/eBqvAIXXAv7CnZR3alvM474XPYLN+p+Qr5aGlVvnMDhPLNFX" +
                "2rfJeG78vX+tbF6ZFQnBaJi3PqsFCcFrlVnFYiXZzWbVScFrq1BFoZji5" +
                "o61YK2joIBd142he0dS8FbeXRBW0dxH3mUjDpNNMASa9ZWMzVERfQdtSa" +
                "IZEomAjkuH7g3jFP9kxJHR449ucJTxFiKvukTeRI+gOFBb69tRzxcLZ5v" +
                "iIZL9NjaH3iod5owGlmU6LxgNPMGLI2vasMHSzvSGs1bgFaq3Ck7UuHTW" +
                "4/dwjJKRCYMDlQ3cHfTgDF7x82iZ5DTJYg/VITkifqA2RRzyEi5DBMl5Y" +
                "IzyEijNFziHDvnkNMzVfggI72CuBSL2EUGWiV5ob0sOcOV3QIq2A4x45v" +
                "ZjDkoAAuHC7IKnfI/vLHRu3CzpbEUVl5kpCXpq5II8A33nkeB9oGVggXR" +
                "Qzt162BY0r3FBld1qT1M49VZhBXsQxb1wUHhMpgAH1/wNwCoxsEWote3S" +
                "GwsvhY50F9+N5bkwVZ10+KMWE33ppE/m/D5tTcUFphJGInfiXjVE8UIkC" +
                "9uQAt8UlvLsxJa12a1brfdzt7A4v5DNpPBATVx8FBiwAQbzsg0N1wxvRB" +
                "Xq6QK0NbzzqdOfHK2JgDoF6/gDKnGO6s7ERjaqLG/L1mOE/pLZ5ux5EIX" +
                "tRsnl7DKso5Uh3e+ITbaBRFC9d7IOhVn/QeSANautOM38G0EI3syOsl7e" +
                "JPlfjlSxY1P/WyfpnojWLnwN+c6UhfjXJLhpszWwtEcjs/6jZNIh2NLjm" +
                "Ut57wXQWUIo0MR25vAF82Ho+GSPE/HGUJgcms8sBwIVSVQF9VfILKAgUk" +
                "kEO0mIc+hUdSwdEbFgWScuEEYD/4syDzJkDe5qux2Kk/PLlz5pN8FiC3O" +
                "Uo7zye9/dEw9ON6HzaY2Mu8hf3xWcL5O6b129uPrs7IiA0qUHV1v9fQyU" +
                "177jwJJ0bpSN91a+lwoy5pddhxSXJkBpIRG/d689ygYf9nRXrUB86nAPu" +
                "z2mWbJ9vIgmmlaL1MUtPhDrqkXs2ncLymRKRNLRBbqWTpnTFLCSw9K7bc" +
                "heXGE2vLahXr2mNjudFFKKlgz+vTcRQeqlnEvQ7Spep0eb6MWAVznja9Z" +
                "qJ65MoKM/Tqyd0pM+v4MgzmEoP79fHenJtvFh62p448vqBIoSbSs7L+aj" +
                "JFm5udIiTLr5DHMRJs3zR6cJcd3OJRGLTi20zUie6KI3NqU9sFSO+voKy" +
                "+gvLpFRQiiOCx0BHzSuqIG4vtWN7eq0kVbS7MipBsOkbyyRgJYWt0LLDm" +
                "XcmrmbG44LhHnKtEb4NN0K7iN53RItSbzuhOgvZaWSK86VwkW/2mM/jRm" +
                "865oSVkuO7sbW+8UOXMfaTCfkZ2/AoTGw6I3wXNZSpUUFuIbW90sHoVrC" +
                "Ipeo3xYbtG7W3VzCvNOb8O0v9h7rkdL5tZ7Dv3LTXzIuaOj4I3cyOG741" +
                "HgtSaJxE2Bg2H6Iwr11OPApgplvhHNwI5OhRc6DUqBqpP4tWKjjryJRmX" +
                "c3Rve14CPIjWyvw7XtQwwVHJ2rGSpSxFQXpPpf3Ur6Ch+Prucn2uqHH46" +
                "PCMg8cncpYWDidyWguMTuTQmc5V9EvRCXVNRxnCaK2hK/Q+85lOFZGlmt" +
                "goIrROB4zbuoOvmrnD4xYOMLrmH/kZ6X4oUH2mpcKgAR32xS0MsNlHJ5R" +
                "J6+RrOko+ctPZ7VIX4Wc6U0RWKiLPFBFEd8A4+Q6+Sr7D4+QTPAzP24s3" +
                "VMoomNvQ9zrzzEAPmnjhQgAUsG+xnWdqmHL4SLMysoJd/ZS0fop+ZuhvA" +
                "482ObPLgpA7lclqOpxPL7x5ydxdwYIxN1fw0NRW5g3oPHVbQHHJPSjsIq" +
                "NjtKT7Xl1klcN3dLC2UHRUfOgMoseFsuUyQlxmQeivXE9EOG8vW+508mp" +
                "C+62tuzw/2ojxDkWpzz2gdspKh/EdrYzHXXrq07OkFxOgJb+VlrRK1KWE" +
                "dZVoe42MpFucgaC9vB+FcMOAVid9bHDTJvpdlKJMem3lAmH86qExRnIB5" +
                "Vm9CpzH/tgFRpOoBUea3GJW0PmFx3yluWQLZx5xkCsqUIwpmsnNY5oSlh" +
                "FqjorlPC8zRs2sZ7WC6hlxuO1/vuzMoRERo4rdHLm3EuTINdfkiCypRik" +
                "zzxmjwp9CypcR/8+Hbse5ogQ9i/iP3GHFbNL7xqxVczHgHh54c4j4Lm/y" +
                "JfIR+yhiZVFxbddfg8BZxIH+HbIhysieBxj9syMsgKiwduiOjkHO+oon8" +
                "cUsFFmILyoU9kvCiRLGYf+B9uHCnsXsc8gSdJaaNYQqkEU18bDehyyJ0u" +
                "0WnHOaSWiYx+9CgqNoMPI+SI2Z5jHrBVolaoRENovZJ24hBFHicJXpFVI" +
                "d5eSpe+A5JhFoFjN3jyJPlIzT8NB35zeJLxLW9nN8kjNGu6jSRfXgdB4e" +
                "noWVxqzLJkQUVcjTJbTMOC72o191+1po9itXVKRAY9YwbIQTNbpv3XFgo" +
                "lRtM1Um9G0q01ljAkNVGVaYkNuqxiAtAVeJMbKGoJSwFDUwjKzWFIQSKo" +
                "vDVSC9bVOmMG2KyjJRlpLI7KsnmKCiRvfZshw7jo9jpdTjI6XUwWOltLJ" +
                "wUEodMFJKgYp9I7JC2zeSpcwlQeqVYeR0ZNSJeq4HS7QJPdCxt5Hs5LeO" +
                "yNIhJtJXhpkowSuzOmRnP35Wj+345r27E417E5II1DYkYPxOC2y0Q73+P" +
                "U1uqujQ5ftgzAI/5ua5bIkc3V3ewgEL0GIgx6Hg+l3EPDH3dQ7Hm3d1Fo" +
                "Y9euIKVS/Sw5EBB/RB3vwPXfbB7IHxfH+KJnXQL7WVkEIdDQrU/cBDBDz" +
                "FkQbsHNP2CppCaC7Jw8EkAIo+ome0e35ZRhHPfbgVlUF89Rez8BYWkGLA" +
                "vqTrr7zPqQu3OfX6ofgCIonhHJviYE2iZuZLve+4mEeIt45i9wDYbNhR+" +
                "7X+xHYKAYrSjApw1JWVJX9l4pU7TNecMRaZeCHBp9N2rfd8IalsJRi+0m" +
                "TRNXklQEU7U7A+UkDYvRPJjI8svtgjRzccwsFFq8CoL7eeS1slV20p15h" +
                "eQAb+bdufT5H5RuFBOaymmFXyO1XzefJ7dHdKClrt4i1A+i07fusdO0uH" +
                "DTvQ2tZ6kvzu9fUVv0Vfn1lCFqDQGf+OJno6df5MA3L5d3cMQ8qnWCXxB" +
                "lYNutuHtdmFoUdXArYGvLoTcGXg8bo4pFQLTTNGsB2dSWuS36NdziVpn0" +
                "GG0DnkgJBFBOKrWxAgWk3Oo/6/Rz0MCkYaBDJIzyKzhNeEolfByLA+bZ/" +
                "7yPIyJRwkLEC6ATQnS3fjc9A3nyFsDMOmigE82mcXnpUtABpgZIbVJDcs" +
                "sAw4MlBjpMogyzi5slcz6HjvdkEwvttwCUjneGHokOGkda/BcMfmwVNgu" +
                "hdpFB0NQCUYLy+m15vbz/i+RlRzoG/dcDnsoQfsZbSqUmG8cNXqJaxj1d" +
                "PAIif4qYVxOq2hU8TcGbjH4dirDp55cdr2mzUm/EMop4mGUcF69kz2Cun" +
                "Yzag3XTHvwjVZlFPvoxST5GrrxBTH9Q76KmGwLAYMtztjjnR8jnKWYX33" +
                "kiI0o2e92N0mz9EFXjPSzmqD32K1gYnvc+h2UGSxkQbZSnGEGvIcm1dOC" +
                "ai9SZRiZJqh6Sg5kCK+8BM5cGWQvEJ1Ys057NaHDROaQoF7jnqXkrQeKQ" +
                "oCvmEarq78Dgi13wBqH7E19Ggj0Tq62kmsDDzuIimhthmlq2AFMTOUtoI" +
                "ggor7fL38WwtnpGsLY6xtzz0j6NuNh0YaN50Oz1u5uhHTWQMMcqtUYYHL" +
                "2p8pmeQWeQ2epkT2Fzl1wtjsNVMzpgv647O+uYoZqcw8UDsiZR61OFJzN" +
                "R3VHuRpfxzGG9WFQfddd9YHJFnEgAMNmXt0Gs/j/C5bzxhllcfH7icOl8" +
                "zm6GGQUQDe4akfTsExcjMertF565VtDPrP6mQrCn18xxNSFg2IyP3rO55" +
                "QrpENR05aPa8A4ZBkKdHUkKEF54qOygAVaECXE/IV2TSgw1cpqhkYk3s6" +
                "85KA48Y9U466vSJnOPhDxxwqZSwv+R0SgIhOehLHruIc5CflF4yhzDzrB" +
                "eMpmHp5eK7pKDXI3a8SZgPqNVBtwmMm5SLZaSuGDKSzB4SWsBPDBeJa77" +
                "R0mCeRfjat4m09eJPTIuHhgKvnT1YLj3/vnZNVfe1ivPfWrqrI0Y1XT1b" +
                "zaxfXwcy8o2tW41nfe/kEffmVi+tgbD7IYDkleb8x+kTjvsUwZmYQljsf" +
                "uDKfQdeKgKBtOTjoVh7wV7Is7L0rAZQbchzrztyMM+arAG+6GvPJGil9L" +
                "bHrYWaxMEVzpf6tiN7Q3BcLE/jzrZBMhhlptuOsX65YL8f6fjuxYHdDsG" +
                "Vde+ZVRAvPuTW1WK7uEPL0zkwnnLtb46tyx5iOT2I7X7RIvd3mnyF3UFu" +
                "N1RRi1UoQSK/05MhcpfSQI0pPY4n4lHG+BBqrQvBk7VWhCu60vaqjxWsV" +
                "SLGsy1Eo3aO9clpf9jY38PiYO5JL67EJDwXxS8zGpoEcjt6gLcuWc4NHN" +
                "mrW59hALXNo8AuV3UDaOs1CsovFWM3xIYyQvDTRXaCAGKK9QzpAtqH3tS" +
                "877+Ij4CwermWxfsbjHgC+Xo+RaBe60ZyE7kcJ6NER5aacI7rd1wFKb/+" +
                "gTPLTgHo7ewXdWFFo8xts7xU8axbr1jEyzC+jU4dTJDGMrEukZ3jYcqvJ" +
                "7dSCPTxRgbcXimWVpw+DMeNbKFpsNDPeqetwc/VYhuox7MJlnxk6zYF7r" +
                "JMUw6q/QMfsRZmrdVbttE3ie3UyT/OIEeKAE5Tc8A35YM65oD7JaAwh3Q" +
                "ML6RT+/NXlPFm706tBiOMsl3Qgl/1TTBlq01XJsPLEBTMJyK1yyZLvFgt" +
                "Yf4ZMzxMeuENF3Os7WtrEL3hSB7Df+p7n1GFuF3jqyGBlunRIdPVuTtAt" +
                "HDBUfwkMY9N3wFg6XAFDmkq9Ots4nwoW3yNlcLUFTr/cskOn8UrjPNN/M" +
                "KdXNab2Me8oB8LBnGqm1zsaDYZb550Xpq/vnuNYUHQe1eHXjYV9yLUlx2" +
                "HWc+LQfrh+oPGpwv1rGyyV/rzuMQnRTmcB9rFVBsJQG4u6CnAka+tw733" +
                "m6Ctpl4aBrirO6CzAUR6nDvfhzh19lbMTMt7W+0HyqwSiDRlaRUeGDEyT" +
                "PYFIKQ6nN22jwXz4Q60dNQzmePKu0fO7WU+oYAwvrBSgyPUYivDC3VhLl" +
                "FEYN1ENRtMRVD9tFjdNDe07bKj4e70aCZ13f7UaiXZ+Q6FoW+t3rJ1MHX" +
                "qtgSzTwBo/SsKqOZojovfb63WMmt77b7HlGLJSr220qaJ1CbF22NOM9LE" +
                "POqkig0ZqwKAektSjZsU0cikoFFjhkOfuEWNLwMsIj3sRz4tRhOSs0iok" +
                "Rs/MkQQz0qlrgaKdgsLwzajVoI5wKe9q+SJz+GjxwsHjyfQ0iRcEWXsIv" +
                "KCK62lzNfF4NMV23uMlQOgrBo0CwPRxHxnAkdYtT9NRuTLmg7mB2iQCn9" +
                "pcynF9A6FxhgHcTUWVpdwV1hg8SdLoE17xfezvI0tDdh0AA40uiqP8rnu" +
                "S2S6zQi0QIL5xi0QskX6Can61QDBDevUCQZ2RVgsEKAi9IsAmenNFgMPF" +
                "EORZQp5hL7oPQ6FGE4SrIkRJjfYp2of5DiwMMiEEqIR7rYEgIcF0DMSFt" +
                "RM19ZL6D9XRIRWXh23Qg6HLEXDHNkpk/+UxuEZnd/Fr2I0hAg+Zqtccap" +
                "SKXnNoNR3lF7LkosqPArob0CcT1peLOsFK6Q7KQp1FSyBu0ARPToE09sR" +
                "zDZiLBkqTUGCP6BXttd18IM1A3Pt78RgzUOU180utkKBwL2qJBFnydd89" +
                "hfzFFHevnCM1rzEfwSv/y4SqGdrrQWttNUlM2cwBooNfbZlO8e1VLTrRq" +
                "palg6pFWp/2mCeH6ByHpqNhtgBDnr9krDMAodDTRN/kMmlA2lYGBXOSHP" +
                "zEE2PNIUw8MciHc63LpSXiiSc0skM88aSnaFgtDC0ekDPRbYkINroeUdN" +
                "RCiFa9wr1/w+rTtuH0A+q0kOU6ATsjLRfWjeEXlp3QFhaJ4Aey+toLEK9" +
                "TZwn5hYae4SJo8VhPJus4ITGIlcLtSuHj8YAB8fvEuSFR+MwUgvHJtN5a" +
                "dEATC0wHoXK2uORBC7Q2GllwXP/3F3OAWZUutyQ29EFipqOyo0ezXqJ1p" +
                "+Z/Q71GiUKntO/Cc998SucGbe0ml2tDBCOXNeKvnWJV2b4fgJmfeuj6x4" +
                "JR9ctEh9dnzksHF23yK2j61YifXTduo3WPCykD6hbRA6oLywpZ8YnnvYH" +
                "1K17OaBuY9UH1K2D+L6yTDA5oF4GSCKbW8ztlCAgsxoCkeLVEDjTW2B5I" +
                "KPBA6ULXcDMPqgXcCkMvadeIWGPFY3+4KsRBfFEnW1O2nerhtD9qgNCx0" +
                "oguEdU0WWZiCq6LFPTUWWmxwOGr/UzzcRVD8prWP0NDTlJ34+wlIdB7ai" +
                "WydUDg21rwaftBUKK02au0NEZ/ZVh3TqGUt2ZsyRkX/MMfGsZdpkF1tUM" +
                "pDG88XSmduiNwIrAugqsNbzrRxahmGDU57MA6/5ApWbCRJzVlWwzRfPVJ" +
                "Y/4dUAWw1mpSCtFHwZZL8TkIcL90VcTWL8xj/nZAJknZ69itZ7QQZkoeX" +
                "3wbtcZU7DSAEdeO2kujK2Ni9Pl3t6pVk8tidERKiSB1AJs1NYF8+5VT6k" +
                "QpOiXkFEpOfCrGzvS619vXYF1ofKHTI2uD0WeRteHajqq6RUZZ72DtLCI" +
                "X8J0pF7zFChsHxHa37PHejKHE3JFR4cRNEMeIlkl9mIPax3lFFrMMRVq3" +
                "k0UVmFZAxf8kG/mDh5otPiQee1UkcHsxIDhch2QSh1EqEr5Q2t403pGS9" +
                "rrGYbQeoYDgp7RJgN1x1Uy+BMU6DSHsOucLZPhfn082jlT4Qlt7jjz4C3" +
                "j2QbMIByC1iZcZLrjF1NIEF3DmqYe0PILeGUFOrviaFNQw3WHOzJ8ix7Z" +
                "WkIOd6ymGvALlMtUo0qBXM40w9+JuMw1qk1s0RcN1/emYr6iTSFzCMXr4" +
                "p3KXqSGlAMmKBGfR4hHGTWvykDqMkDo2oAZ/k2w8Kyun5wn3vqSB/ftt5" +
                "uc18ng7YtXyDxdHggjMmlB8vQOMgKNDIxXpI8shXlqPyWHG0srQdvcQpK" +
                "rS0tH+elC9DnZMtjoqJLJPl7EjFF4uLI+hne9wz1Pbm/XI1khp5CdegkQ" +
                "gos9MNTGIb4wk7kcX5hJefbeomWCb8zsaNY6s58pH+Yt7bfet08tZOxb5" +
                "SrIqrLocUAfoq0vG4ufoebqmlUtHe7MYqFaDHtVnkvK09vEcJbpCHG+AK" +
                "KVIriwSnKaRO+IG1KpyBXpoCFPAnnrbqc52V4/Nl5RKzpobOgbzIMqU2L" +
                "2Ni9e5tWQfOx5YzbvW1+Q1Ap1ZYGgTxsgVqdTC+14UR+GqSFWrQ33lmZt" +
                "UqIVa+My0qsNcutGKJMKrW8bl6JuG3a4Dqp2pFe2jWN36pEym1SL7m3kC" +
                "jadk2ZGwKvPqSX6Iy+jZA0Vw2v215aQOt0uCakhg+6vTPvpz91tCsFFQ0" +
                "BRAhWrcGiWNO2iAXmeoVEdN49GXzOViI6Pm/369HDZWaQhct5SIKPgpKh" +
                "v+n7PNHP01WgAj/5h81XtvuUCKoYyNveeOUz3BmMsWsRFgq0xRRRsWFBb" +
                "oQj0mQboQ4PoQ4X79r0E+w0DqIPybFyRWTdKzT3mwXXPVqh4t3KexE9+T" +
                "AoBwn7lLGD3u9f11zeCCwE90hjk9DAcO7v3N9w6lNEo2Oe/xvQ43CQvfL" +
                "Zskrys1/uXoDzWBuFZrmATlcGxnmPNQfpetcC3nz4Rf+rMzZ9ZigGBlLn" +
                "yAoP7SzQPMy7VNIy0XsxOQfdva0wH/CZUxuD0+jaduLPAxkh/9DTNlOzh" +
                "YRvZQS+YuNFCPMNFxOxOWNHLRKvtTN2xO7gLajD+Chkf3V/mbWCZ94XRW" +
                "AWwbxgvAqD7KeUuUnxVXKL3zhSmFHwVhH0BuQmAvnjZpcbfrZPNFD1Oz0" +
                "rx7IPJtULsWZVKITpJrcKjNOkIJVFzDapU6VDse8ulQnS6DM6Z5qZ/NPO" +
                "/DMCpCyf2Tbmfolt1KUpYkCfl7l+p7GeaamKjiGytiLBF6YDxqXgHX52K" +
                "d3h8Kp7gN+UKutmLXp9FQoPCjBLSC6rQhuzNoaj50Qk4uAuXcUynQoVJD" +
                "rHuW9ilyVF/rN3b2GUORjAzZhHFhxzmib6wlOGOzlUYKceLE01RGzS0fx" +
                "PO6FJB1v7ozgs6unnB25yRxMcHKOnRPVDMVm2JoHXMPRTVV3EoRkTGHRU" +
                "BBNO6b612zxxmhwKqhtxZtFg0aqUO1KfxvcNIBh+LtJfMA2rPqDbYCTUF" +
                "kphZrzNINY4x8G/6B75NisYxN4milcDJ2O9gYAJw4r3XGe/OflFL50ht9" +
                "EZQQ9r39obQnboDQq9OwLw5XPLD6NNF4s5FXO2zzoUz2mkVxnjte5GMz1" +
                "hg9HbQaEXbOPUn0qqa1OEsdhe5iSI+4mEktTbgc/P5El4qxlzdABeZnKe" +
                "MYDiteX++N8eASvpiUs9fyHSV4tzho/Q6OF7/r0qPxnlQWHhkwV1lSbyF" +
                "PHXAKFucbzMgjkKYKpaEosDRPkDlgjoz+8+hRDAvsvjIOROpGzxD1m2b9" +
                "KhAmAOvR93YEAj3odEUG/OljQ9XBgnb2IWh7c73hCc6DGk3tUtHqFZnA5" +
                "Rmn1lSjU6oMtoD5o8vymYONSy6ngX1cuAhzcNTD83sT6pI/rIkSqp5HLS" +
                "Ft4h5ZuQTZhszLy/CYXQ6N0m/iAFfisTpJ6ehvAf60R6OZ+WVuQPch5VL" +
                "phyasbnkz8wfUgqiHrKbWSpY/vFS6ZfjsLk8mOXaFYnfeXz1q7lFxTC5+" +
                "N9t/G7BgtBLtzOWgjQkNeQxLJdmgoQF0txgmIPYY7F5pWg7aUE2nEyLrP" +
                "mhpwQpgV3/nWcOUT/U6ipyJrrNBfFEd7eAVmuEqMhqjXCe/EGtO03+kKM" +
                "0Nb/3ygCGgDp9l5EcGVmXxK4MjSui46N0DM1f1ea/00lErSPqQVNZFVEz" +
                "TeW5pjidClRQaTwy1os8/gfPlX0H/l/9XGlUETfWq4T1PT/Xzo+Hjtc6K" +
                "I1xlfyhl0xRhqKLtZPkD2eCNMdn1DHA3cBTlRjd8REUMUUGNcWA0X2AbW" +
                "Vfe43woGKNuP5+O4unMT7yZbkBM6S7Gsu6mAo08moZ7rCBhWYCjdwaRpy" +
                "aSqCRW8OQ+mqxOmAj15bj33y1WBOwkWvDifOnFGjk1jLc9f8Wmgg0cmsY" +
                "/p1XCxUCjdyCIZ3qInG10Ru5IKN8Wiis+U5rTWWFpvJUU6H2emTcejx+1" +
                "Qg8I24ERHmRj7E2xiTCU9IzpRoL74G0gronQJpVhPjnPRQs2zTBb7RwF1" +
                "x6z0YeZwuE4T8T6n59Mq+wtoK4W2PThSDRQB+8mlGLw2EbQzKQ5XxJ3bP" +
                "8zbMe8tHUgVQjYNpY+BbkA5op+mBNdQxgLrr16ZorjEtBWaWBKGVVwvVG" +
                "qILH6Nz/ArTavZuA9NsbRSKbPjnxjdvwRKyOsCsZxt3IDK4dYcoQbkVWI" +
                "JcJp2asYqtETdIcrfcNJ0l8NwdpbaI2A61N1DQdWRkgK9ZmQxBjo1nCVI" +
                "u/KXjOSvSayRj3J7tTQuNOcx8ElYsy0W8spSD9rhamqcdgK4X5bnhLoUV" +
                "csVUU2WpHCYPKMZrTzwzt92GKJpByJqdAfnaYQ/L5J6PQQd9qCKGwgsJU" +
                "ChIUJsTdPfGBHTtPZRE6mpsALOg6IGZLYFVi0n1UKwB5asmgk08IjA4eM" +
                "2BdbgvSb52x49UH5fL0btWucvxTt3fm3NwxMlVeKDoqXwplTrcZiU/b8b" +
                "Bq0Xhcre3IGTNCfz1my8hR27EzZoz8OXYALe0H19qOoYKNfDuOH15rO4o" +
                "KNnJtOXGyqoCNXFtOGGJrO5AGcOTesWSQre1QGsCRe8uKM6sM2Mi14/iB" +
                "trbjqWAj15YjQ21tR1TBRq7JsZ2tXezPeIsdoF6pdJUFaBS7VuVlcXWoy" +
                "RxeOvIFHW9o3gZSXUNfoQfTCyaYeB3DoXkSA6cfKT9sOEv7GYyhGw3ou0" +
                "AKMkbXUJiAzv0Dfbi5LATDfHt3tdiQOny02ODg8bJCbuHRTawTi46Pi88" +
                "1HBsNzhxL3DogNpJnf0X0yjxx4fFo1cIJN178gU5g8WjlI18oNA7dxRof" +
                "Z19acLyOkbt8HZs/urQj5cd+ZIVZMiiurJuh2uyZ2bXs0THJmYOPvXfJg" +
                "VCvjtSMRXeEmo46QjTXnlZ0PEvJL23ZXxjE7UVZNv06y1UTZ0C0RjeLOF" +
                "r0RcQJa57ZMheO223ImjaG9Lm1WczSAWVkxbYCKQM/RydfMMs6aqPBAql" +
                "x5wzYqBZChYaGHIjmaYgoOj+A0ovOC2g6ynNUI4giJwQgnOj48KOVreWC" +
                "tNewUhL6Cg1y9bVEqaFH9xIxyOsTopOA+u16BekteAXf2kKc3mD7rcRbP" +
                "L2lCL7edoX4Z3/KdoZoQ9bPPKH7N/iOzh8gW6PzB5qO8h+hIRij+yjNLb" +
                "NonLxVTrTnq90l+2Y53InIrw93NskoTycB0TfuBfRWjubJdzP0BkvnZ55" +
                "wqbLCj1bY6+QkCnvjvrXOWBYAN0GnMqSrcvS7iZWzZk5svJbUMOTNaC2p" +
                "WQDU+nlt6KCfk9Z3dDBqfQmHpiOrHsYGfRn/b4cLYnzbdq9rA+3DyX4Ku" +
                "u+ejZaTuu+wnBIjQfXzeNAOiGBK5Btsnlna22RMHb/f8/+dXCmC6h/wS3" +
                "hmLbfw3gfnaE9ODCmBW7Lv9enM0mHeS2Fp7cRB3oUVRc592hRcuk57qT3" +
                "oPVUO0I485t1YUWRfxIUh9Cw56VkPSD/rKVP3HVVFBK+mQitQ29c1LVNm" +
                "9lNf3OmgG2Zzy8ay/PO6qAhhSpVZQu6Yg5Z1iuZYGcWMpEoN7YcK6DpCR" +
                "s7grUP13u30SIUm0D0Mdt8sd9+jx9nmib+bccL9tFPXqaetckOPmmBmwK" +
                "s2aN2OGyHK3j9iUdrPNNfEoyKyB0WEebYDxgtEDr5aH3K43j3PkhuPVtB" +
                "dtBu8JKD6A5RjdK2WpqP+oAVj3z8MO7v41AQyrD4pMFosUrhsmU4N9nXo" +
                "URs5TjgBZosbeDS2oMp2+m7NLEtGpjEspK/mgnU2MH6GTWUHqHF6aZFgg" +
                "Fdq4NYZlYl14Ed1F4B6QLO1iB7jlx4KhnYOik3tKg8G+zoH3bKwc6JqQw" +
                "/nOsp/h2lzOgeJQd3c0WJS1wrgjeqcFzGjc5HrHTjnJD7EMgmgnGKZKky" +
                "OsdQOdIZ4COzxLHflQ3E7baNVs4qAGoVL0vrCtpoAbwSSa/NSh+jnkVaL" +
                "MoLDnXqrBUvScPSzSPAw0bC+hK9wTyJZtr60D74yDUfRrBK538I64ikMo" +
                "6TlltzZFUlef2Fo9kCXvXJvlQmTBVodcEDQBwyww1R+px4RMbHoUQRj2/" +
                "Yhzkx0vduo25xaYNRvlha96jgri497ThaRvtKOgvDYoD0yaL+dmB4x6xL" +
                "NxH5CVE1pIss00SkidI8OGPe6Dr7qdR0ed7EEo6xiH7rlzceSKlbd3pxv" +
                "mJmvoCJpOihIGjVfwxlwtriGxU/MFC/LKzT4cLwh1INFaqCgl1lBlAhzD" +
                "YSgHCzOGkUHV0StvlCj1vZP5jFRqtT8pCnKwsGmTil6dzmsz91ooYU8PZ" +
                "KhhukJeaPpaCRDTvW7i3o7ZmmB6MCzAfe9tc+hijHKKcY+nK6WdKYWHq3" +
                "oWHRkPdI6MF7lKZNblh/zJDb6KAwdHyilxt6zz48WZmx4o/tLl8ktcxEmk" +
                "qc82Ef0f4YhyZBqwDTuwnBZBPKWvfqKbD9UGq96WHRAGBQNEA+JpYXCgG" +
                "iAW8OhEUUPhsZlNBQaRA+EBpBhcGYoGQSXjvRDoHEsA6CJTg9/hh0/Mbw" +
                "S6HLkfsDbBuPwHvU7NnefeWcyQuaCyPhYGciNjojL2XBnK/sZ7TQRs4c3" +
                "K/epFekZ6oq+bhz1K1p4QeTcDT6pVrIwWDwec0d19O4eyi+6E5KudKvUd" +
                "NQqIeWw6zcXI6uxtV6/OQW/9ixjzh7zkCdcdBKTZGQk2l+4GIt+T35WNm" +
                "lIhXUhJNudC80m9lPXPAduzE6w+4yeWVOYPLM2TU6y1IQWbnRSPVlpHPb" +
                "wwAswpp7a89zs0lF+08vcyw394mHL1w4x2M9nzkV4HslzfEjPTzQSXHnK" +
                "hNsK9bB+6eGJUXtwd6BxVOqpgf6XmSP3JjTvFDWGzMKTJvCFp5zs3E70o" +
                "YXzCddJKZ2bcIHRYLYDzWqjd1RpR3ZJ1rqiB++odo68+bHHvZymbF5RQ8" +
                "zcw5Ueb7Q4HYN1GMolWtKpSHu1yhBarTIAn6TQPTqHbaLxkjPXCYjGj1X" +
                "UE4uO1+0zC8c9e+mCGNkP5haNR4bSgqO+nU1IrwMiGnsqgs+RMyccFd1B" +
                "hlI0ZziuG2TpODfaI0RVFmH2Wx38recOCwdz2UmHQ7YcxS4PW6rVNEwjp" +
                "bsTZHH0pqymo+5kmcSvhxYUhtq9tURLkbgLLyPh0B4ZrHlKC90IqsRGHQ" +
                "g2ZUsE8zZcXtfRvU6LhLbNUAr04dw5yYdneyQjc5Q1VeB7UHJqNyNH2/J" +
                "aOpjyklbbvhXJ0fvcGbGr17nz5BytCa5IjzTzBUPvmaYoRcvkHC0frhQd" +
                "nUmegHF+7bqdvuf8vOZBZxP0V6qXc34Y5ZRab6C2IzJoxgYM+ilIe1kn5" +
                "s1nbZUPhiyDFfjG6Mu3DdBXnMPqV4mMeNDPW6IqGiBe30eVNOjYQp7F+3" +
                "D1OGTDPLLw1Wl7eDEXjybnsFiWWyK+q6VKgUZWCZRVnX+CLnCOVsYaQ8s" +
                "CGmTQBw6mqAjdrccG5nSoLimfkxw941ASu3Hp6zzzjPHFAZMFOVcPP1QG" +
                "DQfcTcC3bjjAAOI5V0E3ZO35cO9ZvSs8U+hI/KlhxbV7VlvwRtRT4VxF3" +
                "ZJ1fRtChaKJ7sUpFR01CjrcdS9bngvNeGZNSK9TmDh2PSft3WbQd7BNPO" +
                "OPjksHgcGkK4XTkLeUY8MQRXdpKFEtKUpY2aFTqpZ8KO1sXx1lhp3DhXO" +
                "KDBfOGTBcOGfIk66GDZpi97UPM+pZY4Fo6kUwOuJQkPa9oiF0t+iA0C8a" +
                "IPQ7+cTQI/uXBUEuNT1jpBndwViPeNFFjJVm+tX+KLSrKxlRH3QvkzWGH" +
                "lXTuQGv2ox1O66+jA99Qfdnfzqb+zdyCzzyMGLGd+VA2ieCavtpTnqk9n" +
                "tkxE/U7KxfzWZnwhlNaIUxnr42yXiX3uSNgUYzU+P0GM+WFoLJPGgSIKm" +
                "tTB60SqOvhLs2UybEHQ9Z8vPFnCYRdkaMVmOTVZtYb+r8SOUgASYWGMKB" +
                "ktoi6ogJS9Ye2tF302eCnsx7cpzrhens4gY3TDENGyXDeXhuP4NXB6i5+" +
                "MwiIQczDdyaj7vw/YzcBaAWr50DPUufeSjM0x0Uz9RzD4a5uoNudUhOVD" +
                "1fd66jGbvDbh0SLy1LT+eda+nnnJMwpZ8L4Cf1zotb7TNHUdoY4t2aJ7N" +
                "B7RjSU7o06MPkLjg/Tyeprr9E1Y3u5kKdje7m0nQ0dhgGmtFVI514xqiN" +
                "enzcRLNkPDmoHDJqoHQoz7yFR7Wcoj+xkLNdyR01RORmuNzvnJPSeeARE" +
                "RajXVazUDSDmFrQz+Yciozv9506PEShedIxDBulQ+LBxKAv0YtmlERd/e" +
                "BOlFDm6FrxCsqtNmApQUerJJBUvwfNNhFdVYX+IrqqStNR2TIgxIPs//N" +
                "Mc9qnrbUca4uIIXdGs0FaXLktPRac1R7a9xsHVQZ67M29Ms3SUGbZjxNV" +
                "Enw8GB2o8WrutbDShd01hkAzRn+/8ATZwmlgj45m22GCfUSf0Jkb5GieP" +
                "f0uV7YCl991ok8Uz266sqZMOR+I/i5bImq/70bHhC4CqrWMGwjZHWv3o0" +
                "uTnGWRB6mn/ZA1803ZqXnSW+zOFeRNdhGC3Efo18SR5cd+/bRBsHziwRC" +
                "7R16aPrXEkTtAzdwSPMRPa1jagPLZWr4013NO5D7DRCoCwlTKwWEyRSCa" +
                "NBjAGHZSceNnmmlCc7J7RYRVdAeMN1gcfLXB4vB4g4XgNrrIDrmnVzPQc" +
                "vUEe7Yi7W/BMIS+lccB4coOAvoE9czQ8RyQ88vrKU3DJn41u2jYEcQa7M" +
                "QAXoW1lNZhPRKUWCLeOKtG5NHNYKgP0c1gmo46FlSPy/g2D47Sl/F1Hos" +
                "rMDoZjSx67XZflZ7ROEQGWu8kaGm5Q2SwNH4O57ewNZw7RDSGIp9OHSYa" +
                "YOUBCZkB8WauPONH0D8MqbSjmnSQOQ3kLc3IhOr1IuN1dLNO4bDvIboPm" +
                "ZCjdajaAkGDMkCsP2UWCtqTAW7pTiYpWnMyLiO9ySC3tCYjtNaZjEspSM" +
                "MO+tLMkV5bMo6lSI0c8m5OY7JQK0PGtVeFHNEfN0bRnCa8RhnxXeR2tXl" +
                "yMes5GaK9KLM/UuqylxqkuxqtXCYXubwMIYaFFUeEy8saDchKS5VEz4Hm" +
                "yWWzDt1HkYIOt41VlpSzIZDd2yFCRH3b2CKQ3jMmxIJJ9HnAJBlzhQXRV" +
                "mmAnQDpUkUjdxItS4DqpjAIKTeUQUptJmnI8C4xSH3tD8LR14lBd7i4C8" +
                "qaif30V860M0uraCmuvqCsbSwdhbi0mFxQtgIdX1DGHNeQzhDk3ZUdMmT" +
                "UtxSVye3lYXjVt1Ogz7+EO8yQqZKZ6Ogu148YrzyoluQq43J08xOkj1RG" +
                "lAVX4PytQcVK0eYS7QlTIJD2m2u3uqvJFe4vJ6Jb9xTxnJ/s7cyy9QQlJ" +
                "xdaMRt8u2eRvsgLPCTQiqMtbzQonsg2158tCk/ox4ebMeh1SBO44fgLHz" +
                "APc4jcn4bK8DI2xPeYO0kBEaL8ZQKsdT0v37+Mn8qGwnc1/E2L5Gr0m4+" +
                "xaPBD3UAPtzZW8GrldBXgq1czG5S7f5KY/qP7rCoPSCeA6HVvh6yRboXf" +
                "usVaOjRZ0le1LgN4y+45wr3FcwRqW2cwbgWSJtdhaEwHkSZf2cWXyVfZS" +
                "yvwrbfSLB0MlEjrW4or0NwsWJIRtgdyRZbFCAhLkgYMS5KWNKe4oAE3Qg" +
                "Wt2GDaz2pC5G0IL7uhZ/sahhkEqXo9qEHRS88YW78q3XI+JTlSLRtiV5r" +
                "lguhYsVwC1JkzA23ejeDuiu8TzAg6qRYCcBKrngabLCOOPo8yizjhjaI4" +
                "LAfWAKPbb9vkq5/LIE16WWMFt2iC+uEkNHcL+TrkaV1/iJ3WR31XPObpD" +
                "vNNRADdTgBGHS+qoJ6rVxDImJjefGe8HTN1UjxTG602yf9isEoPOoB58l" +
                "U6XVQlP/hVSGxQ+ZHjeiyeoeLogW01TV5ZyFXy6rsVJPl1re4snYHUhzd" +
                "WoPXhDU1H8i7IkGBqUOM+tG49qAMkeFZ2uAWF+2ou1uMEncF+fbs9hCE1" +
                "69ewU8g4R89ImtBfw0uUYTV9GjNib3WZvKpnhpbJa2i5pSXETB3d8Ksaz" +
                "2uSaosN85BX1dKhO73q3axZChq+OSbwFuo0RSqixkoHIV+Rnk7dmwrJvK" +
                "ZUwyFNFvTFkAaQRwox0CrAzWWAL2cOh07VHeOFmEn7HZ4qB2i/1278Cst" +
                "k9T2mDmFqHaHb2huT/GJRRYi7NJzn4LjlZSqRclw7x8PrwV+kY5yEk3g8" +
                "kn7lRrOXls2kfS+IRX7tRrNTz+b94ryja7SmVX6HL4tRLs2G/m46Zjcca" +
                "b4LxPjzb+PxRl2H9jTYCAZcFhVnLgmnMw0Yy4mTWG0/lr48/7fFu/r7Ti" +
                "StLhnQF7+X0GLsQjNRFHpBfDYBrVuNoaWZQOaoW0ce6SXXWQZa+9Z0pNQ" +
                "hQwbzMMmMH5HdC1noSf1GUIY4pL9GeEbfTLmF/KrPysFV6L1RB98OZqK0" +
                "Sjj3xHDzpxqB82Xypza3zpJgT4lZ1p+6F4LTqBdqkj+jEx3QCf7kBUpNm" +
                "0SWjui4xawRmfynkrXNEz4EBD30bb3ehA572ib6tnRouG8yM18mcnF6Rl" +
                "z1ZFkSXaNuvOmlLNJ68JiC1uOGpqOByDAkmhTUfs3h1e+6UtyroSn3oI7" +
                "iCozqwgJcrdqXcB7Ko7ZEGCaq5E3P9JG8qIAsLdPgInlTCuB0TtLcCB+G" +
                "sGUWwFg3ZF6Od4pXxvWtkbCMGaORcB5zxzvNqFgRf7TlDIXk7Xp7GlPwt" +
                "6vdaegmb7eNKzD+vn3HuALV9e2WccXMBGa3LIezXTcJGYc6oSoi029MU5" +
                "nncZsmokZbQ16dDq8ZwHG9RRN4Q9sMJhbzCI8fxjI8fXHZlBl5vLmCgwY" +
                "HKDYETAUbH7VnVXasGGcFOPdhijKDDF55YIm4bYpmaj/9agumUm+91oGR" +
                "C1rwgvxgdIhY+sMb+mmMFWzD8eYYhYi6G6RtMA9mm48wT1NkmJYZMEzLDB" +
                "lNsTKH6PsyVk0KMaID4ag0QxC5Zji62deKjnqWkgypDSiwqzuvoe29XV16" +
                "3V6BUT+C/sg8VmLPJ6AgBt1PGmFVh2ZieJNttIxJfgtv72KWJkvgLMmX4a" +
                "lDIe9ZAryXaR5D+oJRlCtt4uZIpR+skDN6sIIoftrBShkGLiQhOvGNIC4q" +
                "g9EJRAfAS0VHGVyQIVVpAup03z/pPrZxWD+c+8c+ejQDQxp4u/4MPUTDVY" +
                "Bv+ZqRPS7GwoNa7CswKkbGrroVdowX3XuwJ9Xj5HJF2i8Yr5JvHFvnyTd9" +
                "WA36xjdZRCbPO2/wrS8cIK2MOmuSI6NOBnVt1FkZNBh1Gldjo04G16szXJ" +
                "mhR0e4JgC1jSdD+qN7xIRbHVhFCRs0visQvfW39fEPtSnPGN/M2adlaT9D" +
                "1xABoXNwcOgeAGhtCSn1S+VVi28ZqWeWcCM1an0KwBp+8tO+sV4tzJcYVj" +
                "raj9ezPPkWLeAgtpuWk2hS37pbJ6NRAaITtgg/OmFL+mh2rybmK2z/WFrt" +
                "X5UG8FtSltJ7Sh4Jm0oWiXeVbLB6s8gi0W6RhfSukEXUzo8F9HkXi/jtHU" +
                "uZZvT7wLfOqAusAngYDg7PJpNFwK0MwFD3ndEakhGdR0ShbDvdnOYEzKK/" +
                "vko+I6oLj+HcLr3KcG4U3zL5Fh0rQwWOjpWRPgzqPnBUQW0lwoYRDYwQNT" +
                "oRA/fRiRjQ0s/D79gsABOib2GDDQmK7OEReGQPP0/+7a59v0z+H+SUGTTsMAEA";

            byte[] compressedBytes = Convert.FromBase64String(base64String);
            byte[] decompressedBytes = DecompressGzip(compressedBytes);
            string decompressedString = Encoding.UTF8.GetString(decompressedBytes);
            //string[] lines = decompressedString.Split(Environment.NewLine);
            string[] lines = decompressedString.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            return lines;
        }

        private static byte[] DecompressGzip(byte[] compressedBytes)
        {
            using (MemoryStream compressedStream = new MemoryStream(compressedBytes))
            {
                using (MemoryStream decompressedStream = new MemoryStream())
                {
                    using (
                        GZipStream gzipStream = new GZipStream(
                            compressedStream,
                            CompressionMode.Decompress
                        )
                    )
                    {
                        gzipStream.CopyTo(decompressedStream);
                    }
                    return decompressedStream.ToArray();
                }
            }
        }

        public static string Unicode2GlyphName(int ch)
        {
            if (Utils.AdobeGlyphs.Count == 0)
            {
                string[] lines = Utils.GetGlyphText();
                foreach (string line in lines)
                {
                    if (line.StartsWith("#"))
                        continue;
                    string[] items = line.Split(';');
                    if (items.Length != 2)
                        continue;

                    int c = Convert.ToInt32(items[1].Substring(0, 4), 16);
                    AdobeGlyphs.Add(c, items[0]);
                }
            }
            //return AdobeGlyphs.GetValueOrDefault(ch, ".notdef");
            return AdobeGlyphs.ContainsKey(ch) ? AdobeGlyphs[ch] : ".notdef";
        }

        internal static FzMatrix ShowStringCS(
            FzText text,
            Font userFont,
            FzMatrix trm,
            string s,
            int wmode,
            int bidi_level,
            fz_bidi_direction markupDir,
            fz_text_language langauge
        )
        {
            int i = 0;
            while (i < s.Length)
            {
                ll_fz_chartorune_outparams outparams = new ll_fz_chartorune_outparams();
                int l = mupdf.mupdf.ll_fz_chartorune_outparams_fn(s.Substring(i), outparams);
                i += l;
                float adv;
                FzFont font;
                int gid = mupdf.mupdf.fz_encode_character_sc(userFont.ToFzFont(), outparams.rune);
                if (gid == 0)
                {
                    using (FzFont _font = new FzFont())
                    {
                        int _gid = userFont.ToFzFont().fz_encode_character_with_fallback(outparams.rune, 0, (int)langauge, _font);
                        mupdf.mupdf.fz_show_glyph(
                            text,
                            _font,
                            trm,
                            _gid,
                            outparams.rune,
                            wmode,
                            bidi_level,
                            markupDir,
                            langauge
                        );
                        adv = mupdf.mupdf.fz_advance_glyph(_font, _gid, wmode);
                    }
                    /*
                    (gid, font) = userFont
                        .ToFzFont()
                        .fz_encode_character_with_fallback(outparams.rune, 0, (int)langauge);
                    mupdf.mupdf.fz_show_glyph(
                        text,
                        font,
                        trm,
                        gid,
                        outparams.rune,
                        wmode,
                        bidi_level,
                        markupDir,
                        langauge
                    );
                    adv = mupdf.mupdf.fz_advance_glyph(font, gid, wmode);
                    */
                }
                else
                {
                    font = userFont.ToFzFont();
                    mupdf.mupdf.fz_show_glyph(
                        text,
                        font,
                        trm,
                        gid,
                        outparams.rune,
                        wmode,
                        bidi_level,
                        markupDir,
                        langauge
                    );
                    adv = mupdf.mupdf.fz_advance_glyph(font, gid, wmode);
                }

                if (wmode == 0)
                    trm = trm.fz_pre_translate(adv, 0);
                else
                    trm = trm.fz_pre_translate(0, -adv);
            }

            return trm;
        }

        internal static int CheckQuad(LineartDevice dev)
        {
            List<Item> items = dev.PathDict.Items;
            int len = items.Count;
            float[] f = new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
            Point lp = new Point();

            for (int i = 0; i < 4; i++)
            {
                Item line = items[len - 4 + i];
                Point tmp = line.P1;
                f[i * 2] = tmp.X;
                f[i * 2 + 1] = tmp.Y;
                lp = line.LastPoint;
            }

            if (lp.X != f[0] || lp.Y != f[1])
                return 0;

            dev.LineCount = 0;
            FzQuad q = mupdf.mupdf.fz_make_quad(f[0], f[1], f[6], f[7], f[2], f[3], f[4], f[5]);
            Item rect = new Item() { Type = "qu", Quad = new Quad(q) };

            items[len - 4] = rect;
            for (int i = len - 3; i < len; i++)
                items.RemoveAt(len - 3);
            return 1;
        }

        internal static int CheckRect(LineartDevice dev)
        {
            dev.LineCount = 0;
            int orientation = 0;
            List<Item> items = dev.PathDict.Items;
            int len = items.Count;

            Item line0 = items[len - 3];
            Point ll = line0.P1;
            Point lr = line0.LastPoint;

            Item line2 = items[len - 1];
            Point ur = line2.P1;
            Point ul = line2.LastPoint;

            if (ll.Y != lr.Y || ll.X != ul.X || ur.Y != ul.Y || ur.X != lr.X)
                return 0;

            FzRect r;
            if (ul.Y < lr.Y)
            {
                r = mupdf.mupdf.fz_make_rect(ul.X, ul.Y, lr.X, lr.Y);
                orientation = 1;
            }
            else
            {
                r = mupdf.mupdf.fz_make_rect(ll.X, ll.Y, ur.X, ur.Y);
                orientation = -1;
            }

            Item rect = new Item()
            {
                Type = "re",
                Rect = new Rect(r),
                Orientation = orientation
            };

            items[len - 3] = rect;
            for (int i = len - 2; i < len; i++)
            {
                items.RemoveAt(len - 2);
            }

            return 1;
        }

        internal static FzRect ComputerScissor(LineartDevice dev)
        {
            if (dev.Scissors == null)
                dev.Scissors = new List<FzRect>();
            int numScissors = dev.Scissors.Count;
            FzRect scissor;
            if (numScissors > 0)
            {
                FzRect lastScissor = dev.Scissors[numScissors - 1];
                scissor = lastScissor;
                scissor = FzRect.fz_intersect_rect(scissor, dev.PathRect);
            }
            else
            {
                scissor = dev.PathRect;
            }
            dev.Scissors.Add(scissor);

            return scissor;
        }

        internal static List<int> GetOutlineXrefs(PdfObj obj, List<int> xrefs)
        {
            if (obj.m_internal == null)
                return xrefs;
            PdfObj thisobj = obj;
            while (thisobj.m_internal != null)
            {
                int newXref = thisobj.pdf_to_num();
                if (
                    xrefs.Contains(newXref)
                    || thisobj.pdf_dict_get(new PdfObj("Type")).m_internal != null
                )
                    break;
                xrefs.Add(newXref);
                PdfObj first = thisobj.pdf_dict_get(new PdfObj("First"));
                if (first.pdf_is_dict() != 0)
                {
                    xrefs = Utils.GetOutlineXrefs(first, xrefs);
                }

                thisobj = thisobj.pdf_dict_get(new PdfObj("Next"));
                PdfObj parent = thisobj.pdf_dict_get(new PdfObj("Parent"));
                if (thisobj.pdf_is_dict() == 0)
                    thisobj = parent;
            }
            return xrefs;
        }

        internal static void GetPageLabels(List<(int, string)> list, PdfObj nums)
        {
            int n = nums.pdf_array_len();
            for (int i = 0; i < n; i += 2)
            {
                PdfObj key = nums.pdf_array_get(i).pdf_resolve_indirect();
                int pno = key.pdf_to_int();
                PdfObj val = nums.pdf_array_get(i + 1).pdf_resolve_indirect();
                FzBuffer res = Utils.Object2Buffer(val, 1, 0);
                //byte[] c = res.fz_buffer_extract();
                byte[] c = Utils.BinFromBuffer(res);

                string cStr = Encoding.UTF8.GetString(c);
                list.Add((pno, cStr));
            }
        }

        internal static void RemoveDestRange(PdfDocument pdf, List<int> numbers)
        {
            int pageCount = pdf.pdf_count_pages();
            for (int i = 0; i < pageCount; i++)
            {
                int n1 = i;
                if (numbers.Contains(n1))
                    continue;

                PdfObj pageRef = pdf.pdf_lookup_page_obj(i);
                PdfObj annots = pageRef.pdf_dict_get(new PdfObj("Annots"));
                if (annots.m_internal == null)
                    continue;

                int len = annots.pdf_array_len();
                for (int j = len - 1; j > -1; j -= 1)
                {
                    PdfObj o = annots.pdf_array_get(j);
                    if (
                        mupdf.mupdf.pdf_name_eq(
                            o.pdf_dict_get(new PdfObj("Subtype")),
                            new PdfObj("Link")
                        ) == 0
                    )
                        continue;
                    PdfObj action = o.pdf_dict_get(new PdfObj("A"));
                    PdfObj dest = o.pdf_dict_get(new PdfObj("Dest"));
                    if (action.m_internal != null)
                    {
                        if (
                            mupdf.mupdf.pdf_name_eq(
                                action.pdf_dict_get(new PdfObj("S")),
                                new PdfObj("GoTo")
                            ) == 0
                        )
                            continue;
                        dest = action.pdf_dict_get(new PdfObj("D"));
                    }

                    int pno = -1;
                    if (dest.pdf_is_array() != 0)
                    {
                        PdfObj target = dest.pdf_array_get(0);
                        pno = pdf.pdf_lookup_page_number(target);
                    }
                    else if (dest.pdf_is_string() != 0)
                    {
                        FzLocation location = pdf.super()
                            .fz_resolve_link(dest.pdf_to_text_string(), null, null);
                        pno = location.page;
                    }
                    if (pno < 0)
                        continue;
                    n1 = pno;
                    if (numbers.Contains(n1))
                        annots.pdf_array_delete(j);
                }
            }
        }

        public static PdfPage AsPdfPage(dynamic page)
        {
            if (page is Page)
                return (page as Page).GetPdfPage();
            if (page is PdfPage)
                return page;
            else if (page is FzPage)
                return (page as FzPage).pdf_page_from_fz_page();
            else if (page == null)
                throw new Exception("Page is none");
            return null;
        }

        internal static PdfObj PdfObjFromStr(PdfDocument doc, string src)
        {
            byte[] bSrc = Encoding.UTF8.GetBytes(src);

            FzBuffer buffer_ = Utils.fz_new_buffer_from_data(bSrc);
            FzStream stream = buffer_.fz_open_buffer();
            PdfLexbuf lexBuf = new PdfLexbuf(256);
            PdfObj ret = doc.pdf_parse_stm_obj(stream, lexBuf); // issue

            return ret;
        }

        public static string GetPdfNow()
        {
            DateTimeOffset dto = DateTimeOffset.Now;
            int offsetHours = dto.Offset.Hours;
            int offsetMinutes = dto.Offset.Minutes;

            string offset = string.Format(
                "{0:00}'{1:00}'",
                Math.Abs(offsetHours),
                Math.Abs(offsetMinutes)
            );
            string timestamp = dto.ToString("D:yyyyMMddHHmmss");

            if (dto.Offset > TimeSpan.Zero)
            {
                timestamp += "-" + offset;
            }
            else if (dto.Offset < TimeSpan.Zero)
            {
                timestamp += "+" + offset;
            }

            return timestamp;
        }

        private static string MakeUtf16Be(string s)
        {
            byte[] bytes = Encoding.BigEndianUnicode.GetBytes(s);
            byte[] resBytes = new byte[bytes.Length + 2];
            resBytes[0] = 254;
            resBytes[1] = 255;
            Array.Copy(bytes, 0, resBytes, 2, bytes.Length);
            return "<" + BitConverter.ToString(resBytes).Replace("-", string.Empty) + ">";
        }

        internal static int Find(byte[] haystack, byte[] needle)
        {
            for (var i = 0; i < haystack.Length - needle.Length + 1; i++)
            {
                if (haystack[i] == needle[0])
                {
                    var fail = false;
                    for (var j = 1; j < needle.Length; j++)
                    {
                        if (haystack[i + j] != needle[j])
                        {
                            fail = true;
                            break;
                        }
                    }
                    if (!fail)
                        return i;
                }
            }
            return -1; // if not found
        }

        internal static void EnsureOperations(PdfDocument pdf)
        {
            if (!HaveOperations(pdf))
                throw new Exception("No journalling operation started");
        }

        /// <summary>
        /// Ensure valid journalling state
        /// </summary>
        /// <param name="pdf"></param>
        /// <returns></returns>
        internal static bool HaveOperations(PdfDocument pdf)
        {
            if (pdf.m_internal.journal != null && string.IsNullOrEmpty(pdf.pdf_undoredo_step(0)))
                return false;
            return true;
        }

        internal static PdfObj GetXObjectFromPage(
            PdfDocument pdfOut,
            PdfPage pdfPage,
            int xref,
            GraftMap gmap
        )
        {
            PdfObj xobj,
                resources;
            if (xref > 0)
                xobj = pdfOut.pdf_new_indirect(xref, 0);
            else
            {
                PdfPage srcPage = pdfPage;
                PdfObj srcPageRef = srcPage.obj();
                FzRect mediaBox = srcPageRef
                    .pdf_dict_get_inheritable(new PdfObj("MediaBox"))
                    .pdf_to_rect();
                PdfObj o = srcPageRef.pdf_dict_get_inheritable(new PdfObj("Resources"));
                if (gmap.ToPdfGraftMap().m_internal != null)
                {
                    resources = gmap.ToPdfGraftMap().pdf_graft_mapped_object(o);
                }
                else
                {
                    resources = pdfOut.pdf_graft_object(o);
                }
                FzBuffer res = Utils.ReadContents(srcPageRef);

                xobj = pdfOut.pdf_new_xobject(mediaBox, new FzMatrix(), new PdfObj(0), res);
                Utils.UpdateStream(pdfOut, xobj, res, 1);

                xobj.pdf_dict_put(new PdfObj("Resources"), resources);
            }
            return xobj;
        }

        /// <summary>
        /// Read and concatenate a PDF page's /Conents object(s) in a buffer
        /// </summary>
        /// <param name="pageRef"></param>
        /// <returns></returns>
        internal static FzBuffer ReadContents(PdfObj pageRef)
        {
            PdfObj contents = pageRef.pdf_dict_get(new PdfObj("Contents"));
            FzBuffer res = null;
            if (contents.pdf_is_array() != 0)
            {
                res = new FzBuffer(1024);
                for (int i = 0; i < contents.pdf_array_len(); i++)
                {
                    if (i > 0)
                        res.fz_append_byte(32);
                    PdfObj obj = contents.pdf_array_get(i);
                    if (obj.pdf_is_stream() != 0)
                    {
                        FzBuffer nres = obj.pdf_load_stream();
                        res.fz_append_buffer(nres);
                    }
                }
            }
            else if (contents.m_internal != null)
                res = contents.pdf_load_stream();
            else
                res = null;

            return res;
        }

        /// <summary>
        /// Add OC object reference to a dictionary
        /// </summary>
        /// <param name="pdf"></param>
        /// <param name="_ref"></param>
        /// <param name="xref"></param>
        /// <exception cref="Exception"></exception>
        internal static void AddOcObject(PdfDocument pdf, PdfObj _ref, int xref)
        {
            PdfObj indObj = pdf.pdf_new_indirect(xref, 0);
            if (indObj.pdf_is_dict() == 0)
                throw new Exception(ErrorMessages["MSG_BAD_OC_REF"]);
            PdfObj type = indObj.pdf_dict_get(new PdfObj("Type"));
            if (type.pdf_objcmp(new PdfObj("OCG")) == 0 || type.pdf_objcmp(new PdfObj("OCMD")) == 0)
            {
                _ref.pdf_dict_put(new PdfObj("OC"), indObj);
            }
            else
                throw new Exception(ErrorMessages["MSG_BAD_OC_REF"]);
        }

        internal static bool IsJbig2Image(PdfObj obj)
        {
            return false;
        }

        internal static List<int> GetOcgArraysImp(PdfObj arr)
        {
            List<int> list = new List<int>();
            if (arr.pdf_is_array() != 0)
            {
                int n = arr.pdf_array_len();
                for (int i = 0; i < n; i++)
                {
                    PdfObj obj = arr.pdf_array_get(i);
                    int item = obj.pdf_to_num();
                    if (!list.Contains(item))
                        list.Add(item);
                }
            }
            return list;
        }

        internal static void SetOcgArraysImp(PdfObj arr, List<int> list)
        {
            PdfDocument pdf = mupdf.mupdf.pdf_get_bound_document(arr);
            foreach (int xref in list)
            {
                PdfObj obj = pdf.pdf_new_indirect(xref, 0);
                arr.pdf_array_push(obj);
            }
        }

        public static FzMatrix CalcImageMatrix(
            int width,
            int height,
            Rect tr,
            float rotate,
            bool keep
        )
        {
            FzMatrix rot = mupdf.mupdf.fz_rotate(rotate);
            float trw = tr.X1 - tr.X0;
            float trh = tr.Y1 - tr.Y0;
            float w = trw;
            float h = trh;
            float fw;
            float fh;
            if (keep)
            {
                float large = Math.Max(width, height);
                fw = width / large;
                fh = height / large;
            }
            else
                fw = fh = 1;
            float small = Math.Min(fw, fh);
            if (rotate != 0 && rotate != 180)
            {
                float f = fw;
                fw = fh;
                fh = f;
            }
            if (fw < 1)
            {
                if (trw / fw > trh / fh)
                {
                    w = trh * small;
                    h = trh;
                }
                else
                {
                    w = trw;
                    h = trw / small;
                }
            }
            else if (fw != fh)
            {
                if (trw / fw > trh / fh)
                {
                    w = trh / small;
                    h = trh;
                }
                else
                {
                    w = trw;
                    h = trw * small;
                }
            }
            else
            {
                w = trw;
                h = trh;
            }
            FzPoint tmp = mupdf.mupdf.fz_make_point((tr.X0 + tr.X1) / 2, (tr.Y0 + tr.Y1) / 2);
            FzMatrix mat = mupdf.mupdf.fz_make_matrix(1, 0, 0, 1, -0.5f, -0.5f);
            mat = FzMatrix.fz_concat(mat, rot);
            mat = FzMatrix.fz_concat(mat, mupdf.mupdf.fz_scale(w, h));
            mat = FzMatrix.fz_concat(mat, mupdf.mupdf.fz_translate(tmp.x, tmp.y));
            return mat;
        }

        public static string GetPageLabel(int pno, List<(int, string)> labels)
        {
            List<(int, string)> items = new List<(int, string)>();
            foreach ((int, string) label in labels)
            {
                if (label.Item1 <= pno)
                    items.Add(label);
            }

            Label rule = Utils.RuleDict(items.Last());
            string prefix = rule.Prefix;
            string style = rule.Style;
            int delta = (style == "a" || style == "A") ? -1 : 0;
            int pageNumber = pno - rule.StartPage + rule.FirstPageNum + delta;
            return Utils.ConstructLabel(style, prefix, pageNumber);
        }

        public static Label RuleDict((int, string) item)
        {
            string rule = item.Item2;
            string[] rules = rule.Substring(2, rule.Length - 2 - 2).Split('/').Skip(1).ToArray();
            Label ret = new Label()
            {
                StartPage = item.Item1,
                Prefix = "",
                FirstPageNum = 1
            };
            bool skip = false;
            int i = -1;

            foreach (string s in rules)
            {
                i++;
                if (skip)
                {
                    skip = false;
                    continue;
                }
                if (s == "S")
                {
                    ret.Style = rules[i + 1];
                    skip = true;
                    continue;
                }
                if (s.StartsWith("P"))
                {
                    string x = s.Substring(1).Replace("(", "").Replace(")", "");
                    ret.Prefix = x;
                    continue;
                }
                if (s.StartsWith("St"))
                {
                    int x = Convert.ToInt32(s.Substring(2));
                    ret.FirstPageNum = x;
                }
            }

            return ret;
        }

        public static string ConstructLabel(string style, string prefix, int pno)
        {
            string nStr = "";
            if (style == "D")
                nStr = Convert.ToString(pno);
            else if (style == "r")
                nStr = Utils.Integer2Roman(pno).ToLower();
            else if (style == "R")
                nStr = Utils.Integer2Roman(pno).ToUpper();
            else if (style == "a")
                nStr = Utils.Integer2Letter(pno).ToLower();
            else if (style == "A")
                nStr = Utils.Integer2Letter(pno).ToUpper();
            string ret = prefix + nStr;
            return ret;
        }

        public static string Integer2Letter(int i)
        {
            string asciiUppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int n = 1;
            int a = i;
            while (Math.Pow(26, n) <= a)
            {
                a -= Convert.ToInt32(Math.Pow(26, n));
                n += 1;
            }
            string ret = "";
            for (int j = n - 1; j >= 0; j--)
            {
                int g = a % Convert.ToInt32(Math.Pow(26, j));
                int f = a / Convert.ToInt32(Math.Pow(26, j));
                ret += asciiUppercase[f];
            }
            return ret;
        }

        public static string Integer2Roman(int num)
        {
            Dictionary<int, string> roman = new Dictionary<int, string>()
            {
                { 1000, "M" },
                { 900, "CM" },
                { 500, "D" },
                { 400, "CD" },
                { 100, "C" },
                { 90, "XC" },
                { 50, "L" },
                { 40, "XL" },
                { 10, "X" },
                { 9, "IX" },
                { 5, "V" },
                { 4, "IV" },
                { 1, "I" },
            };

            IEnumerable<string> RomanNum(int _num)
            {
                //foreach ((int r, string ltr) in roman)
                foreach (var pair in roman)
                {
                    int r = pair.Key;
                    string ltr = pair.Value;
                    int x = _num / r;
                    yield return string.Concat(Enumerable.Repeat(ltr, x));
                    _num -= r * x;
                    if (_num <= 0)
                        break;
                }
            }

            return string.Concat(RomanNum(num).ToArray());
        }

        public static string GetTextbox(Page page, Rect rect, TextPage textPage = null)
        {
            TextPage tp = textPage;
            if (tp == null)
                tp = page.GetTextPage();
            else if (tp.Parent != page)
                throw new Exception("not a textpage of this page");
            string ret = tp.ExtractTextBox(rect.ToFzRect());
            if (textPage == null)
                tp = null;
            return ret;
        }

        public static string GetTextSelection(
            Page page,
            Point p1,
            Point p2,
            Rect clip = null,
            TextPage textPage = null
        )
        {
            TextPage tp = textPage;
            if (tp == null)
                tp = page.GetTextPage(clip, flags: (int)TextFlags.TEXT_DEHYPHENATE);
            else if (tp.Parent != page)
                throw new Exception("not a textpage of this page");
            string ret = tp.ExtractSelection(p1, p2);
            if (textPage == null)
                tp = null;
            return ret;
        }

        public static void UpdateLink(Page page, LinkInfo link)
        {
            string annot = GetLinkText(page, link);
            if (annot == "")
                throw new Exception("link kind not supported");
            page.Parent.UpdateObject(link.Xref, annot, page.GetPdfPage());
            return;
        }

        public static void WriteText(
            Page page,
            Rect rect = null,
            TextWriter[] writers = null,
            bool overlay = true,
            float[] color = null,
            float opacity = 0,
            bool keepProportion = true,
            int rotate = 0,
            int oc = 0
        )
        {
            if (writers == null)
                throw new Exception("need at least one TextWriter");
            if (writers.Length == 1 && rotate == 0 && rect == null)
            {
                writers[0]
                    .WriteText(page, opacity: opacity, color: color, overlay: overlay ? 1 : 0);
                return;
            }
            Rect clip = writers[0].TextRect;
            Document textDoc = new Document();
            Page tpage = textDoc.NewPage(width: page.Rect.Width, height: page.Rect.Height);
            foreach (TextWriter writer in writers)
            {
                clip = clip | writer.TextRect;
                writer.WriteText(tpage, opacity: opacity, color: color);
            }

            if (rect == null)
                rect = clip;
            page.ShowPdfPage(
                rect,
                textDoc,
                0,
                overlay: overlay,
                keepProportion: keepProportion,
                rotate: rotate,
                clip: clip,
                oc: oc
            );
            textDoc = null;
            tpage = null;
        }

        /// <summary>
        /// Merge the /Resources object created by a text pdf device into the page.
        /// The device may have created multiple /ExtGState/Alp? and /Font/F? objects.
        /// These need to be renamed (renumbered) to not overwrite existing page
        /// objects from previous executions.
        /// </summary>
        /// <param name="pageRef"></param>
        /// <returns>the next available numbers n, m for objects /Alp<n>, /F<m></returns>
        internal static (int, int) MergeResources(PdfPage page, PdfObj res)
        {
            // page objects /Resources, /Resources/ExtGState, /Resources/Font
            PdfObj resources = page.obj().pdf_dict_get(new PdfObj("Resources"));
            if (resources.m_internal == null)
            {
                resources = mupdf.mupdf.pdf_dict_put_dict(page.obj(), new PdfObj("Resources"), 5);
            }
            PdfObj mainExtg = resources.pdf_dict_get(new PdfObj("ExtGState"));
            PdfObj mainFonts = resources.pdf_dict_get(new PdfObj("Font"));

            // text pdf device objects /ExtGState, /Font
            PdfObj tmpExtg = res.pdf_dict_get(new PdfObj("ExtGState"));
            PdfObj tmpFonts = res.pdf_dict_get(new PdfObj("Font"));

            int maxAlp = -1;
            int maxFonts = -1;
            int n = 0;

            // Handle /Alp objects
            if (tmpExtg.pdf_is_dict() != 0) // any created at all?
            {
                n = tmpExtg.pdf_dict_len(); // does page have /ExtGState yet?
                if (mainExtg.pdf_is_dict() != 0)
                {
                    for (int i = 0; i < mainExtg.pdf_dict_len(); i++)
                    {
                        // get highest number of objects named /Alpxxx
                        string alp = mainExtg.pdf_dict_get_key(i).pdf_to_name();
                        if (!alp.StartsWith("Alp"))
                            continue;
                        int j = mupdf.mupdf.fz_atoi(alp.Substring(3));
                        if (j > maxAlp)
                            maxAlp = j;
                    }
                }
                else // create a /ExtGState for the page
                    mainExtg = resources.pdf_dict_put_dict(new PdfObj("ExtGState"), n);

                maxAlp += 1;
                for (int i = 0; i < n; i++) // copy over renumbered /Alp objects
                {
                    string alp = tmpExtg.pdf_dict_get_key(i).pdf_to_name();
                    int j = mupdf.mupdf.fz_atoi(alp.Substring(3)) + maxAlp;
                    string text = $"Alp{j}";
                    PdfObj val = tmpExtg.pdf_dict_get_val(i);
                    mainExtg.pdf_dict_puts(text, val);
                }
            }
            if (mainFonts.pdf_is_dict() != 0) // has page any fonts yet?
            {
                for (int i = 0; i < mainFonts.pdf_dict_len(); i++) // get max font number
                {
                    string font = mainFonts.pdf_dict_get_key(i).pdf_to_name();
                    if (!font.StartsWith("F"))
                        continue;
                    int j = mupdf.mupdf.fz_atoi(font.Substring(1));
                    if (j > maxFonts)
                        maxFonts = j;
                }
            }
            else // create a Resources/Font for the page
                mainFonts = resources.pdf_dict_put_dict(new PdfObj("Font"), 2);

            maxFonts += 1;
            for (int i = 0; i < tmpFonts.pdf_dict_len(); i++) // copy renumbered fonts
            {
                string font = tmpFonts.pdf_dict_get_key(i).pdf_to_name();
                int j = mupdf.mupdf.fz_atoi(font.Substring(1)) + maxFonts;
                string text = $"F{j}";
                PdfObj val = tmpFonts.pdf_dict_get_val(i);
                mainFonts.pdf_dict_puts(text, val);
            }
            return (maxAlp, maxFonts);
        }

        public static void RepairMonoFont(Page page, Font font)
        {
            if (font.Flags["mono"] == 0)
                return;
            Document doc = page.Parent;
            List<Entry> fonts = page.GetFonts();
            List<int> xrefs = new List<int>();
            foreach (Entry f in fonts)
            {
                if (
                    f.Name == font.Name
                    && f.RefName.StartsWith("F")
                    && f.Encoding.StartsWith("Identity")
                )
                    xrefs.Add(f.Xref);
            }

            if (xrefs.Count == 0)
                return;
            int width = Convert.ToInt32(font.GlyphAdvance(32) * 1000);
            foreach (int xref in xrefs)
            {
                if (Utils.SetFontWidth(doc, xref, width))
                    Console.WriteLine($"Cannot set width for {font.Name} in xref {xref}");
            }
        }

        public static bool SetFontWidth(Document doc, int xref, int width)
        {
            PdfDocument pdf = Document.AsPdfDocument(doc);
            if (pdf.m_internal == null)
                return false;

            PdfObj font = pdf.pdf_load_object(xref);
            PdfObj dFonts = font.pdf_dict_get(new PdfObj("DescendantFonts"));
            if (dFonts.pdf_is_array() != 0)
            {
                int n = dFonts.pdf_array_len();
                for (int i = 0; i < n; i++)
                {
                    PdfObj dFont = dFonts.pdf_array_get(i);
                    PdfObj wArray = pdf.pdf_new_array(3);
                    wArray.pdf_array_push(mupdf.mupdf.pdf_new_int(0));
                    wArray.pdf_array_push(mupdf.mupdf.pdf_new_int(65535));
                    wArray.pdf_array_push(mupdf.mupdf.pdf_new_int(width));
                    dFont.pdf_dict_put(new PdfObj("W"), wArray);
                }
            }
            font.Dispose();
            pdf.Dispose();
            return true;
        }

        public static int GetOC(Document doc, int xref)
        {
            if (doc.IsClosed || doc.IsEncrypted)
                throw new Exception("document close or encrypted");
            (string t, string name) = doc.GetKeyXref(xref, "Subtype");
            if (t != "name" || !(name == "/Image" || name == "/Form"))
                throw new Exception($"bad object type at xref {xref}");
            (string _t, string oc) = doc.GetKeyXref(xref, "OC");
            t = _t;
            if (t != "xref")
                return 0;
            return Convert.ToInt32(oc.Replace("0 R", ""));
        }

        public static OCMD GetOCMD(Document doc, int xref)
        {
            if (!INRANGE(xref, 0, doc.GetXrefLength() - 1))
                throw new Exception("bad xref");
            string text = doc.GetXrefObject(xref, compressed: 1);
            if (!text.Contains("/Type/OCMD"))
                throw new Exception("bad object type");
            int textLen = text.Length;

            int p0 = text.IndexOf("/OCGs[");
            int p1 = text.IndexOf("]", p0);
            int[] ocgs = null;

            if (p0 < 0 || p1 < 0)
                ocgs = null;
            else
            {
                ocgs = text.Substring(p0 + 6, p1 - p0 - 6)
                    .Replace("0 R", " ")
                    .Split(' ')
                    .Select(x => int.Parse(x))
                    .ToArray();
            }

            p0 = text.IndexOf("/P/");
            string policy;
            string ve;
            List<dynamic> obj = null;

            if (p0 < 0)
                policy = null;
            else
            {
                p1 = text.IndexOf("ff", p0);
                if (p1 < 0)
                    p1 = text.IndexOf("on", p0);
                if (p1 < 0)
                    throw new Exception("bad object at xref");
                else
                    policy = text.Substring(p0 + 3, p1 + 2);
            }

            p0 = text.IndexOf("/VE[");
            if (p0 < 0)
                ve = null;
            else
            {
                int lp = 0;
                int rp = 0;
                p1 = p0;
                while (lp < 1 || lp != rp)
                {
                    p1 += 1;
                    if (!(p1 < textLen))
                        throw new Exception("bad object ast xref");
                    if (text[p1] == '[')
                        lp += 1;
                    if (text[p1] == ']')
                        rp += 1;
                }
                ve = text.Substring(p0 + 3, p1 + 1);
                ve = ve.Replace("/And", "\"and\",")
                    .Replace("/Not", "\"not\",")
                    .Replace("/Or", "\"or\",");
                ve = ve.Replace(" 0 R]", "]").Replace(" 0 R", ",").Replace("][", "],[");

                obj = JsonConvert.DeserializeObject<List<dynamic>>(
                    ve,
                    new JsonSerializerSettings() { Converters = { new VEConverter() } }
                );

                if (obj == null)
                    throw new Exception($"bad /VE key: {ve}");
            }
            return new OCMD()
            {
                Xref = xref,
                Ocgs = ocgs,
                Policy = policy,
                Ve = obj.ToArray()
            };
        }

        internal class VEConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(List<dynamic>));
            }

            public override void WriteJson(
                JsonWriter writer,
                dynamic value,
                JsonSerializer serializer
            )
            {
                throw new NotImplementedException();
            }

            public override dynamic ReadJson(
                JsonReader reader,
                Type objectType,
                object existingValue,
                JsonSerializer serializer
            )
            {
                List<dynamic> result = new List<dynamic>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartArray)
                    {
                        result.Add(ReadJson(reader, objectType, existingValue, serializer));
                    }
                    else if (reader.TokenType == JsonToken.EndArray)
                    {
                        return result;
                    }
                    else
                    {
                        dynamic value = (
                            reader.ValueType == typeof(long)
                                ? Convert.ToInt32(reader.Value)
                                : reader.Value
                        );
                        result.Add(value);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Return a list of page numbers with the given label
        /// </summary>
        /// <param name="label">label</param>
        /// <param name="onlyOne">(bool) stop searching after first hit</param>
        /// <returns></returns>
        public static List<int> GetPageNumbers(Document doc, string label, bool onlyOne = false)
        {
            List<int> numbers = new List<int>();
            if (string.IsNullOrEmpty(label))
                return numbers;
            List<(int, string)> labels = doc._getPageLabels();
            if (labels.Count == 0)
                return numbers;
            for (int i = 0; i < doc.PageCount; i++)
            {
                string pageLabel = GetPageLabel(i, labels);
                if (pageLabel == label)
                {
                    numbers.Add(i);
                    if (onlyOne)
                        break;
                }
            }
            return numbers;
        }

        /// <summary>
        /// Create pixmap of document page by page number.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="pno">page number</param>
        /// <param name="matrix">Matrix for transformation </param>
        /// <param name="dpi"></param>
        /// <param name="colorSpace">rgb, rgb, gray - case ignored, default csRGB</param>
        /// <param name="clip">restrict rendering to this area</param>
        /// <param name="alpha">include alpha channel</param>
        /// <param name="annots">also render annotations</param>
        /// <returns></returns>
        internal static Pixmap GetPagePixmap(
            Document doc,
            int pno,
            IdentityMatrix matrix = null,
            int dpi = 0,
            string colorSpace = null,
            Rect clip = null,
            bool alpha = false,
            bool annots = true
        )
        {
            return doc[pno]
                .GetPixmap(
                    matrix: matrix,
                    dpi: dpi,
                    colorSpace: colorSpace,
                    clip: clip,
                    alpha: alpha,
                    annots: annots
                );
        }

        /// <summary>
        /// Return a PDF string depending on its coding
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string GetPdfString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "()";
            string MakeUtf16be(string _s)
            {
                byte[] _r = Annot.MergeByte(
                    new byte[] { 254, 255 },
                    Encoding.BigEndianUnicode.GetBytes(_s)
                );
                return "<" + BitConverter.ToString(_r).Replace("-", string.Empty) + ">";
            }

            string r = "";
            foreach (char c in s)
            {
                int oc = Convert.ToInt32(c);
                if (oc > 255)
                    return MakeUtf16Be(s);
                if (oc > 31 && oc < 127)
                {
                    if (c == '(' || c == ')' || c == '\\')
                        r += '\\';
                    r += c;
                    continue;
                }
                if (oc > 127)
                {
                    r += string.Format("\\{0}", Convert.ToString(oc, 8).PadLeft(3, '0'));
                    continue;
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

        public static byte[] GetAllContents(Page page)
        {
            FzBuffer res = Utils.ReadContents(page.GetPdfPage().obj());
            if (res == null)
                return null;
            return Utils.BinFromBuffer(res);
        }

        internal static PdfFilterOptions MakePdfFilterOptions(
            int recurse = 0,
            int instanceForms = 0,
            int ascii = 0,
            int noUpdate = 0,
            int sanitize = 0,
            PdfSanitizeFilterOptions sopts = null
        )
        {
            PdfFilterOptions filter = new PdfFilterOptions();
            filter.recurse = recurse;
            filter.instance_forms = instanceForms;
            filter.ascii = ascii;
            filter.no_update = noUpdate;
            if (sanitize != 0)
            {
                if (sopts == null)
                    sopts = new PdfSanitizeFilterOptions();
                Factory factory = new Factory(sopts);
                filter.add_factory(factory.internal_());
            }

            return filter;
        }

        /// <summary>
        /// Calculate the PDF action string.
        /// </summary>
        /// <param name="xref"></param>
        /// <param name="dDict"></param>
        /// <returns></returns>
        public static string GetDestString(int xref, int dDict)
        {
            string goto_ = "/A<</S/GoTo/D[{0} 0 R/XYZ {1} {2} {3}]>>";

            return string.Format(goto_, xref, 0, dDict, 0);
        }

        /// <summary>
        /// Calculate the PDF action string.
        /// </summary>
        /// <param name="xref"></param>
        /// <param name="dDict"></param>
        /// <returns></returns>
        public static string GetDestString(int xref, float dDict)
        {
            string goto_ = "/A<</S/GoTo/D[{0} 0 R/XYZ {1} {2} {3}]>>";

            return string.Format(goto_, xref, 0, dDict, 0);
        }

        /// <summary>
        /// Calculate the PDF action string.
        /// </summary>
        /// <param name="xref"></param>
        /// <param name="dDict"></param>
        /// <returns></returns>
        public static string GetDestString(int xref, LinkInfo dDict)
        {
            if (dDict == null)
                return "";
            string goto_ = "/A<</S/GoTo/D[{0} 0 R/XYZ {1} {2} {3}]>>";
            string gotor1 = "/A<</S/GoToR/D[{0} /XYZ {1} {2} {3}]/F<</F{4}/UF{5}/Type/Filespec>>>>";
            string gotor2 = "/A<</S/GoToR/D{0}/F<</F{1}/UF{2}/Type/Filespec>>>>";
            string launch = "/A<</S/Launch/F<</F{0}/UF{1}/Type/Filespec>>>>";
            string uri = "/A<</S/URI/URI{0}>>";

            if (dDict.Kind == LinkType.LINK_GOTO)
            {
                float zoom = dDict.Zoom;
                Point to = dDict.To;
                float left = to.X;
                float top = to.Y;
                return string.Format(goto_, xref, left, top, zoom);
            }

            if (dDict.Kind == LinkType.LINK_URI)
            {
                return string.Format(uri, Utils.GetPdfString(dDict.Uri));
            }

            if (dDict.Kind == LinkType.LINK_LAUNCH)
            {
                string fSpec = Utils.GetPdfString(dDict.File);
                return string.Format(launch, fSpec, fSpec);
            }

            if (dDict.Kind == LinkType.LINK_GOTOR && dDict.Page < 0)
            {
                string fSpec = Utils.GetPdfString(dDict.File);
                return string.Format(gotor2, Utils.GetPdfString(dDict.ToStr), fSpec, fSpec);
            }

            if (dDict.Kind == LinkType.LINK_GOTOR && dDict.Page >= 0)
            {
                string fSpec = Utils.GetPdfString(dDict.File);
                return string.Format(
                    gotor1,
                    dDict.Page,
                    dDict.To.X,
                    dDict.To.Y,
                    dDict.Zoom,
                    fSpec,
                    fSpec
                );
            }

            return "";
        }

        internal static void ResetWidget(PdfAnnot annot)
        {
            PdfAnnot thisAnnot = annot;
            PdfObj thisAnnotObj = mupdf.mupdf.pdf_annot_obj(thisAnnot);
            PdfDocument pdf = thisAnnotObj.pdf_get_bound_document();
            pdf.pdf_field_reset(thisAnnotObj);
        }

        /// <summary>
        /// Ensure that widgets with /AA/C JavaScript are in array AcroForm/CO
        /// </summary>
        /// <param name="annot"></param>
        internal static void EnsureWidgetCalc(PdfAnnot annot)
        {
            PdfObj annotObj = annot.pdf_annot_obj();
            PdfDocument pdf = annotObj.pdf_get_bound_document();
            PdfObj coName = new PdfObj("CO");   // = PDF_NAME(CO)
            PdfObj acro = Utils.pdf_dict_getl(
                pdf.pdf_trailer(),
                new string[] { "Root", "AcroForm" }
            );
            PdfObj co = acro.pdf_dict_get(coName);  // = AcroForm/CO
            if (co.m_internal == null)
                co = acro.pdf_dict_put_array(coName, 2);
            int n = co.pdf_array_len();
            int found = 0;
            int xref = annotObj.pdf_to_num();

            for (int i = 0; i < n; i++)
            {
                int nxref = co.pdf_array_get(i).pdf_to_num();
                if (xref == nxref)
                {
                    found = 1;
                    break;
                }
            }
            if (found == 0)
                co.pdf_array_push(pdf.pdf_new_indirect(xref, 0));
        }

        internal static void SaveWidget(PdfAnnot annot, Widget widget)
        {
            PdfPage page = annot.pdf_annot_page();
            PdfObj annotObj = annot.pdf_annot_obj();
            PdfDocument pdf = page.doc();

            int value = widget.FieldType;
            int fieldType = value;

            Rect rect = widget.Rect;
            Matrix rotMat = Utils.RotatePageMatrix(page);
            FzRect rect_ = mupdf.mupdf.fz_transform_rect(rect.ToFzRect(), rotMat.ToFzMatrix());
            annot.pdf_set_annot_rect(rect_);

            float[] color = widget.FillColor;
            if (color != null)
            {
                int n = color.Length;
                PdfObj fillCol = pdf.pdf_new_array(n);
                float col = 0;
                for (int i = 0; i < n; i++)
                {
                    col = color[i];
                    fillCol.pdf_array_push_real(col);
                }
                annotObj.pdf_field_set_fill_color(fillCol);
            }

            int[] borderDashes = widget.BorderDashes;
            if (borderDashes != null)
            {
                int n = borderDashes.Length;
                PdfObj dashes = pdf.pdf_new_array(n);
                for (int i = 0; i < n; i++)
                {
                    dashes.pdf_array_push_int(borderDashes[i]);
                }
                Utils.pdf_dict_putl(annotObj, dashes, new string[] { "BS", "D" });
            }

            float[] borderColor = widget.BorderColor;
            if (borderColor != null)
            {
                int n = borderColor.Length;
                PdfObj borderCol = pdf.pdf_new_array(n);
                float col = 0;
                for (int i = 0; i < n; i++)
                {
                    col = borderColor[i];
                    borderCol.pdf_array_push_real(col);
                }
                Utils.pdf_dict_putl(annotObj, borderCol, new string[] { "MK", "BC" });
            }

            string fieldLabel = widget.FieldLabel;
            if (!string.IsNullOrEmpty(fieldLabel))
            {
                annotObj.pdf_dict_put_text_string(new PdfObj("TU"), fieldLabel);
            }

            string fieldName = widget.FieldName;
            if (!string.IsNullOrEmpty(fieldName))
            {
                string oldName = annotObj.pdf_load_field_name();
                if (fieldName != oldName)
                    annotObj.pdf_dict_put_text_string(new PdfObj("T"), fieldName);
            }

            if (fieldType == (int)PdfWidgetType.PDF_WIDGET_TYPE_TEXT)
            {
                int maxlen = widget.TextMaxLen;
                if (maxlen != 0)
                    annotObj.pdf_dict_put_int(new PdfObj("MaxLen"), maxlen);
            }

            int fieldDisplay = widget.FieldDisplay;
            annotObj.pdf_field_set_display(fieldDisplay);

            if (
                fieldType == (int)PdfWidgetType.PDF_WIDGET_TYPE_LISTBOX
                || fieldType == (int)PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX
            )
            {
                List<dynamic> choiceValues = widget.ChoiceValues;
                SetChoiceOptions(annot, choiceValues);
            }

            string borderStyle = widget.BorderStyle;
            int val = Utils.GetBorderStyle(borderStyle);
            Utils.pdf_dict_putl(annotObj, new PdfObj(val), new string[] { "BS", "S" });

            float borderWidth = widget.BorderWidth;
            Utils.pdf_dict_putl(
                annotObj,
                mupdf.mupdf.pdf_new_real(borderWidth),
                new string[] { "BS", "W" }
            );

            string da = widget.TextDa;
            annotObj.pdf_dict_put_text_string(new PdfObj("DA"), da);
            annotObj.pdf_dict_del(new PdfObj("DS"));
            annotObj.pdf_dict_del(new PdfObj("RC"));

            int fieldFlags = widget.FieldFlags;
            if (fieldFlags != 0)
            {
                if (fieldType == (int)PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX)
                    fieldFlags |= (int)PdfFieldFlags.PDF_CH_FIELD_IS_COMBO;
                else if (fieldType == (int)PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON)
                    fieldType |= (int)PdfFieldFlags.PDF_BTN_FIELD_IS_RADIO;
                else if (fieldType == (int)PdfWidgetType.PDF_WIDGET_TYPE_BUTTON)
                    fieldType |= (int)PdfFieldFlags.PDF_BTN_FIELD_IS_PUSHBUTTON;
                annotObj.pdf_dict_put_int(new PdfObj("Ff"), fieldFlags);
            }

            string buttonCap = widget.ButtonCaption;
            if (!string.IsNullOrEmpty(buttonCap))
                annotObj.pdf_field_set_button_caption(buttonCap);

            string script = widget.Script;
            Utils.PutScript(annotObj, new PdfObj("A"), new PdfObj(), script);

            string scriptStroke = widget.ScriptStroke;
            PutScript(annotObj, new PdfObj("AA"), new PdfObj("K"), scriptStroke);

            string scriptFormat = widget.ScriptFormat;
            PutScript(annotObj, new PdfObj("AA"), new PdfObj("F"), scriptFormat);

            string scriptChange = widget.ScriptChange;
            PutScript(annotObj, new PdfObj("AA"), new PdfObj("V"), scriptChange);

            string scriptCalc = widget.ScriptCalc;
            PutScript(annotObj, new PdfObj("AA"), new PdfObj("C"), scriptCalc);

            string scriptBlur = widget.ScriptBlur;
            PutScript(annotObj, new PdfObj("AA"), new PdfObj("B1"), scriptBlur);

            string scriptFocus = widget.ScriptFocus;
            PutScript(annotObj, new PdfObj("AA"), new PdfObj("Fo"), scriptFocus);

            string fieldVal = widget.FieldValue;
            if (fieldType == (int)PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON)
            {
                if (string.IsNullOrEmpty(fieldVal))
                {
                    pdf.pdf_set_field_value(annotObj, "Off", 1);
                    annotObj.pdf_dict_put_name(new PdfObj("AS"), "Off");
                }
                else
                {
                    PdfObj onstate = annotObj.pdf_button_field_on_state();
                    if (onstate.m_internal != null)
                    {
                        string on = onstate.pdf_to_name();
                        pdf.pdf_set_field_value(annotObj, on, 1);
                        annotObj.pdf_dict_put_name(new PdfObj("AS"), on);
                    }
                    else if (!string.IsNullOrEmpty(fieldVal))
                    {
                        annotObj.pdf_dict_put_name(new PdfObj("AS"), fieldVal);
                    }
                }
            }
            else if (fieldType == (int)PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX)
            {
                if (fieldVal == "Yes")
                {
                    PdfObj onstate = annotObj.pdf_button_field_on_state();
                    string on = onstate.pdf_to_name();
                    pdf.pdf_set_field_value(annotObj, on, 1);
                    annotObj.pdf_dict_put_name(new PdfObj("AS"), "Yes");
                    annotObj.pdf_dict_put_name(new PdfObj("V"), "Yes");
                }
                else
                {
                    annotObj.pdf_dict_put_name(new PdfObj("AS"), "Off");
                    annotObj.pdf_dict_put_name(new PdfObj("V"), "Off");
                }
            }
            else if (!string.IsNullOrEmpty(fieldVal))
            {
                pdf.pdf_set_field_value(annotObj, fieldVal, 1);
                if (
                    fieldType == (int)PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX
                    || fieldType == (int)PdfWidgetType.PDF_WIDGET_TYPE_LISTBOX
                )
                    annotObj.pdf_dict_del(new PdfObj("I"));
            }
            annot.pdf_dirty_annot();
            annot.pdf_set_annot_hot(1);
            annot.pdf_set_annot_active(1);
            annot.pdf_update_annot();
        }

        internal static void PutScript(PdfObj annotObj, PdfObj key1, PdfObj key2, string value)
        {
            PdfObj key1Obj = annotObj.pdf_dict_get(key1);
            PdfDocument pdf = annotObj.pdf_get_bound_document();

            if (string.IsNullOrEmpty(value))
            {
                if (key2 == null || key2.m_internal == null)
                    annotObj.pdf_dict_del(key1);
                else if (key1Obj.m_internal != null)
                    key1Obj.pdf_dict_del(key2);
                return;
            }

            string script;
            if (key2.m_internal == null || key1Obj.m_internal == null)
                script = Utils.GetScript(key1Obj);
            else
                script = Utils.GetScript(key1Obj.pdf_dict_get(key2));

            if (value != script)
            {
                PdfObj newAction = NewJavaScript(pdf, value);
                if (key2.m_internal == null)
                    annotObj.pdf_dict_put(key1, newAction);
                else
                    Utils.pdf_dict_putl(
                        annotObj,
                        newAction,
                        new string[] { key1.pdf_to_name(), key2.pdf_to_name() }
                    );
            }
        }

        internal static PdfObj NewJavaScript(PdfDocument pdf, string value)
        {
            if (value == null)
                return null;
            FzBuffer res = Utils.fz_new_buffer_from_data(Encoding.UTF8.GetBytes(value));
            PdfObj source = pdf.pdf_add_stream(res, new PdfObj(), 0);
            PdfObj newAction = pdf.pdf_add_new_dict(4);
            newAction.pdf_dict_put(new PdfObj("S"), mupdf.mupdf.pdf_new_name("JavaScript"));
            newAction.pdf_dict_put(new PdfObj("JS"), source);

            return newAction;
        }

        internal static string GetScript(PdfObj key)
        {
            if (key.m_internal == null)
                return null;
            PdfObj j = key.pdf_dict_get(new PdfObj("S"));
            string jj = mupdf.mupdf.pdf_to_name(j);
            PdfObj js;
            if (jj == "JavaScript")
            {
                js = key.pdf_dict_get(new PdfObj("JS"));
                if (js.m_internal == null)
                    return null;
            }
            else
                return null;

            string script;
            if (js.pdf_is_string() != 0)
                script = Utils.UnicodeFromStr(js.pdf_to_text_string());
            else if (js.pdf_is_stream() != 0)
            {
                FzBuffer res = js.pdf_load_stream();
                script = Utils.UnicodeFromBuffer(res);
            }
            else
                return null;

            if (!string.IsNullOrEmpty(script))
                return script;
            return null;
        }

        internal static void SetChoiceOptions(PdfAnnot annot, List<dynamic> list)
        {
            if (list == null)
                return;
            int n = list.Count;
            if (n == 0)
                return;
            PdfObj annotObj = annot.pdf_annot_obj();
            PdfDocument pdf = annotObj.pdf_get_bound_document();
            PdfObj optArr = pdf.pdf_new_array(n);
            for (int i = 0; i < n; i++)
            {
                dynamic val = list[i];
                if (val is string)
                    optArr.pdf_array_push_text_string(val);
                else
                {
                    PdfObj optArrSub = optArr.pdf_array_push_array(2);
                    optArrSub.pdf_array_push_text_string(val[0]);
                    optArrSub.pdf_array_push_text_string(val[1]);
                }
            }
            annotObj.pdf_dict_put(new PdfObj("Opt"), optArr);
        }

        public static int GetBorderStyle(string style)
        {
            int val = new PdfObj("S").pdf_to_num();
            if (string.IsNullOrEmpty(style))
                return val;

            string s = style;
            if (s.StartsWith("b") || s.StartsWith("B"))
                val = new PdfObj("B").pdf_to_num();
            else if (s.StartsWith("d") || s.StartsWith("D"))
                val = new PdfObj("D").pdf_to_num();
            else if (s.StartsWith("i") || s.StartsWith("I"))
                val = new PdfObj("I").pdf_to_num();
            else if (s.StartsWith("u") || s.StartsWith("U"))
                val = new PdfObj("U").pdf_to_num();
            else if (s.StartsWith("s") || s.StartsWith("S"))
                val = new PdfObj("S").pdf_to_num();

            return val;
        }

        internal static FzBuffer fz_new_buffer_from_data(byte[] data)
        {
            IntPtr pData = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, pData, data.Length);

            FzBuffer ret = mupdf.mupdf.fz_new_buffer_from_copied_data(
                new SWIGTYPE_p_unsigned_char(pData, true),
                (uint)data.Length
            );

            Marshal.FreeHGlobal(pData);
            return ret;
        }

        internal static void FillWidget(Annot annot, Widget widget)
        {
            Utils.GetWidgetProperties(annot, widget);

            widget.Rect = annot.Rect;
            widget.Xref = annot.Xref;
            widget.Parent = annot.Parent;
            widget._annot = annot._nativeAnnotion;

            if (string.IsNullOrEmpty(widget.Script))
                widget.Script = null;
            if (string.IsNullOrEmpty(widget.ScriptStroke))
                widget.ScriptStroke = null;
            if (string.IsNullOrEmpty(widget.ScriptFormat))
                widget.ScriptFormat = null;
            if (string.IsNullOrEmpty(widget.ScriptChange))
                widget.ScriptChange = null;
            if (string.IsNullOrEmpty(widget.ScriptCalc))
                widget.ScriptCalc = null;
            if (string.IsNullOrEmpty(widget.ScriptBlur))
                widget.ScriptBlur = null;
            if (string.IsNullOrEmpty(widget.ScriptFocus))
                widget.ScriptFocus = null;
        }

        public static string GetFieldTypeText(int wtype)
        {
            if (wtype == (int)PdfWidgetType.PDF_WIDGET_TYPE_BUTTON)
                return "Button";
            if (wtype == (int)PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX)
                return "CheckBox";
            if (wtype == (int)PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON)
                return "RadioButton";
            if (wtype == (int)PdfWidgetType.PDF_WIDGET_TYPE_TEXT)
                return "Text";
            if (wtype == (int)PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX)
                return "ComboBox";
            if (wtype == (int)PdfWidgetType.PDF_WIDGET_TYPE_SIGNATURE)
                return "Signature";
            return "unknown";
        }

        internal static PdfAnnot GetWidgetByXref(PdfPage page, int xref)
        {
            bool found = false;
            PdfAnnot annot = page.pdf_first_widget();
            while (annot.m_internal != null)
            {
                PdfObj annotObj = annot.pdf_annot_obj();
                if (xref == annotObj.pdf_to_num())
                {
                    found = true;
                    break;
                }
                annot = annot.pdf_next_annot();
            }
            if (!found)
                throw new Exception($"xref {xref} is not a widget of this page");
            return annot;
        }

        internal static Matrix GetRotateMatrix(Page page)
        {
            PdfPage pdfpage = page.GetPdfPage();
            if (pdfpage.m_internal == null)
                return new Matrix();
            return Utils.RotatePageMatrix(pdfpage);
        }

        public static Rect PaperRect(string size)
        {
            (int width, int height) = Utils.PaperSize(size);
            return new Rect(0, 0, width, height);
        }

        public static (int, int) PaperSize(string size)
        {
            string s = size.ToLower();
            string f = "p";
            if (s.EndsWith("-l"))
            {
                f = "l";
                s = s.Substring(0, s.Length - 2);
            }
            if (s.EndsWith("-p"))
            {
                s = s.Substring(0, s.Length - 2);
            }

            //(int, int) ret = Utils.PaperSizes.GetValueOrDefault(s, (-1, -1));
            (int, int) ret;
            if (!Utils.PaperSizes.TryGetValue(s, out ret))
            {
                ret = (-1, -1);  // Default value if key not found
            }
            if (f == "p")
                return ret;
            return (ret.Item2, ret.Item1);
        }

        /// <summary>
        /// Calculate length of a string for a built-in font.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fontname">name of the font.</param>
        /// <param name="fontsize">font size points.</param>
        /// <param name="encoding">encoding to use, 0=Latin (default), 1=Greek, 2=Cyrillic.</param>
        /// <returns>length of text.</returns>
        /// <exception cref="Exception"></exception>
        public static float GetTextLength(
            string text,
            string fontFile,
            string fontName = "helv",
            float fontSize = 11,
            int encoding = 0
        )
        {
            fontName = fontName.ToLower();
            //string basename = Utils.Base14_fontdict.GetValueOrDefault(fontName, null);
            string basename;
            if (!Utils.Base14_fontdict.TryGetValue(fontName, out basename))
            {
                basename = null;  // Default value if key not found
            }

            List<(int, double)> glyphs = new List<(int, double)>();
            if (basename == "Symbol")
                glyphs = Utils.symbol_glyphs;
            if (basename == "ZapfDingbats")
                glyphs = Utils.zapf_glyphs;
            if (glyphs.Count != 0)
            {
                float w = 0f;
                foreach (char c in text)
                {
                    int cInt = Convert.ToInt32(c);
                    w += (float)(
                        (Convert.ToInt32(c)) < 256 ? glyphs[cInt].Item2 : glyphs[183].Item2
                    );
                }
                return w * fontSize;
            }

            if (Utils.Base14_fontdict.Keys.Contains(fontName))
            //if (true)
                return Utils.MeasureString(text, fontFile, fontName, fontSize, encoding);
            if (
                (
                    new string[]
                    {
                        "china-t",
                        "china-s",
                        "china-ts",
                        "china-ss",
                        "japan",
                        "japan-s",
                        "korea",
                        "korea-s"
                    }
                ).Contains(fontName)
            )
                return text.Length * fontSize;
            throw new Exception($"Font {fontName} is unsupported");
        }

        public static float MeasureString(
            string text,
            string fontFile,
            string fontName,
            float fontSize = 11.0f,
            int encoding = 0
        )
        {
            if (string.IsNullOrEmpty(fontFile))
            {
                //throw new Exception("should specify font file.");
                return -1f;
            }

            FzFont font = new FzFont(fontName, fontFile, 0, 0);
            float w = 0;
            int pos = 0;
            while (pos < text.Length)
            {
                ll_fz_chartorune_outparams o = new ll_fz_chartorune_outparams();
                int t = mupdf.mupdf.ll_fz_chartorune_outparams_fn(
                    text.Substring(pos, text.Length - pos),
                    o
                );
                int c = o.rune;
                pos += t;
                if (encoding == (int)SimpleEncoding.PDF_SIMPLE_ENCODING_GREEK)
                    c = mupdf.mupdf.fz_iso8859_7_from_unicode(c);
                else if (encoding == (int)SimpleEncoding.PDF_SIMPLE_ENCODING_CYRILLIC)
                    c = mupdf.mupdf.fz_windows_1251_from_unicode(c);
                else
                    c = mupdf.mupdf.fz_windows_1252_from_unicode(c);
                if (c < 0)
                    c = 0xB7;
                int g = font.fz_encode_character(c);
                float dw = font.fz_advance_glyph(g, 0);
                w += dw;
            }
            float ret = w * fontSize;
            return ret;
        }

        /// <summary>
        /// Compute the quad located inside the bbox.
        /// </summary>
        /// <param name="lineDir">'line["dir"]' of the owning line or None.</param>
        /// <param name="span">the span. May be from get_texttrace() method.</param>
        /// <param name="bbox">the bbox of the span or any of its characters.</param>
        /// <returns>The quad which is wrapped by the bbox.</returns>
        public static Quad RecoverBboxQuad((float, float) lineDir, Span span, Rect bbox)
        {
            (float cos, float sin) = lineDir;

            float d = span.Asc - span.Desc;
            float height = d * span.Size;

            float hs = height * sin;
            float hc = height * cos;
            Point ul,
                ur,
                ll,
                lr;
            if (hc >= 0 && hs <= 0)
            {
                ul = bbox.BottomLeft - new Point(0, hc);
                ur = bbox.TopRight + new Point(hs, 0);
                ll = bbox.BottomLeft - new Point(hs, 0);
                lr = bbox.TopRight + new Point(0, hc);
            }
            else if (hc <= 0 && hs <= 0)
            {
                ul = bbox.BottomRight + new Point(hs, 0);
                ur = bbox.TopLeft - new Point(0, hc);
                ll = bbox.BottomRight + new Point(0, hc);
                lr = bbox.TopLeft - new Point(hs, 0);
            }
            else if (hc <= 0 && hs >= 0)
            {
                ul = bbox.TopRight - new Point(0, hc);
                ur = bbox.BottomLeft + new Point(hs, 0);
                ll = bbox.TopRight - new Point(hs, 0);
                lr = bbox.BottomLeft + new Point(0, hc);
            }
            else
            {
                ul = bbox.TopLeft + new Point(hs, 0);
                ur = bbox.BottomRight - new Point(0, hc);
                ll = bbox.TopLeft + new Point(0, hc);
                lr = bbox.BottomRight - new Point(hs, 0);
            }

            return new Quad(ul, ur, ll, lr);
        }

        public static bool SetSmallGlyphHeights(bool on = false)
        {
            if (on)
                SmallGlyphHeights = true;
            return SmallGlyphHeights;
        }

        /// <summary>
        /// Recover the quadrilateral of a text character.
        /// </summary>
        /// <param name="lineDir">Line Dir</param>
        /// <param name="span"></param>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static Quad RecoverCharQuad((float, float) lineDir, Span span, Char ch)
        {
            Rect bbox = new Rect(ch.Bbox);
            return RecoverBboxQuad(lineDir, span, bbox);
        }

        /// <summary>
        /// Calculate the span quad for 'dict' / 'rawdict' text extractions.
        /// </summary>
        /// <param name="lineDir"></param>
        /// <param name="span"></param>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static Quad RecoverSpanQuad((float, float) lineDir, Span span, Char[] chars)
        {
            if (chars == null)
                return RecoverQuad(lineDir, span);

            Quad q0 = RecoverCharQuad(lineDir, span, chars[0]);
            Quad q1;
            if (chars.Length > 1)
                q1 = RecoverCharQuad(lineDir, span, chars[chars.Length - 1]);
            else
                q1 = q0;

            Point spanll = q0.LowerLeft;
            Point spanlr = q1.LowerRight;
            Matrix mat0 = PlanishLine(spanll, spanlr);
            Point xlr = spanlr * mat0;

            bool small = SetSmallGlyphHeights();
            float h = 0;

            h = span.Size * (small ? 1 : (span.Asc - span.Desc));
            Rect spanRect = new Rect(0, -h, xlr.X, 0);
            Quad spanQuad = spanRect.Quad;
            spanQuad = spanQuad * ~mat0;

            return spanQuad;
        }

        /// <summary>
        /// Recover the quadrilateral of a text span.
        /// </summary>
        /// <param name="lineDir">'line["dir"]' of the owning line.</param>
        /// <param name="span">the span.</param>
        /// <returns>The quadrilateral enveloping the span's text.</returns>
        public static Quad RecoverQuad((float, float) lineDir, Span span)
        {
            if (span == null)
                throw new Exception("bad span argument");
            return Utils.RecoverBboxQuad(lineDir, span, span.Bbox);
        }

        /// <summary>
        /// Calculate the line quad for 'dict' / 'rawdict' text extractions.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="spans"></param>
        /// <returns></returns>
        public static Quad RecoverLineQuad(Line line, List<Span> spans = null)
        {
            if (spans == null)
                spans = line.Spans;
            if (spans.Count == 0)
                throw new Exception("bad span list");
            float cos = line.Dir.X;
            float sin = line.Dir.Y;
            Quad q0 = Utils.RecoverQuad((cos, sin), spans[0]);
            Quad q1;
            if (spans.Count > 1)
                q1 = RecoverQuad((cos, sin), spans[spans.Count - 1]);
            else
                q1 = q0;
            Point linell = q0.LowerLeft;
            Point linelr = q0.LowerRight;

            Matrix mat0 = Utils.PlanishLine(linell, linelr);
            Point xlr = linelr * mat0;

            float h = 0;
            foreach (Span s in spans)
            {
                if (h < s.Size * (s.Asc - s.Desc))
                    h = s.Size * (s.Asc - s.Desc);
            }
            Rect lineRect = new Rect(0, -h, xlr.X, 0);
            Quad lineQuad = lineRect.Quad;
            lineQuad *= ~mat0;

            return lineQuad;
        }

        public static List<string> GetColorList()
        {
            return Utils.GetColorInfoList().Select(x => x.Item1).ToList();
        }

        public static List<(string, int, int, int)> GetColorInfoList()
        {
            return new List<(string, int, int, int)>
            {
                ("ALICEBLUE", 240, 248, 255),
                ("ANTIQUEWHITE", 240, 248, 255),
                ("ANTIQUEWHITE2", 238, 223, 204),
                ("ANTIQUEWHITE3", 205, 192, 176),
                ("ANTIQUEWHITE4", 139, 131, 120),
                ("AQUAMARINE", 127, 255, 212),
                ("AQUAMARINE1", 127, 255, 212),
                ("AQUAMARINE2", 118, 238, 198),
                ("AQUAMARINE3", 102, 205, 170),
                ("AQUAMARINE4", 69, 139, 116),
                ("AZURE", 240, 255, 255),
                ("AZURE1", 240, 255, 255),
                ("AZURE2", 224, 238, 238),
                ("AZURE3", 193, 205, 205),
                ("AZURE4", 131, 139, 139),
                ("BEIGE", 245, 245, 220),
                ("BISQUE", 255, 228, 196),
                ("BISQUE1", 255, 228, 196),
                ("BISQUE2", 238, 213, 183),
                ("BISQUE3", 205, 183, 158),
                ("BISQUE4", 139, 125, 107),
                ("BLACK", 0, 0, 0),
                ("BLANCHEDALMOND", 255, 235, 205),
                ("BLUE", 0, 0, 255),
                ("BLUE1", 0, 0, 255),
                ("BLUE2", 0, 0, 238),
                ("BLUE3", 0, 0, 205),
                ("BLUE4", 0, 0, 139),
                ("BLUEVIOLET", 138, 43, 226),
                ("BROWN", 165, 42, 42),
                ("BROWN1", 255, 64, 64),
                ("BROWN2", 238, 59, 59),
                ("BROWN3", 205, 51, 51),
                ("BROWN4", 139, 35, 35),
                ("BURLYWOOD", 222, 184, 135),
                ("BURLYWOOD1", 255, 211, 155),
                ("BURLYWOOD2", 238, 197, 145),
                ("BURLYWOOD3", 205, 170, 125),
                ("BURLYWOOD4", 139, 115, 85),
                ("CADETBLUE", 95, 158, 160),
                ("CADETBLUE1", 152, 245, 255),
                ("CADETBLUE2", 142, 229, 238),
                ("CADETBLUE3", 122, 197, 205),
                ("CADETBLUE4", 83, 134, 139),
                ("CHARTREUSE", 127, 255, 0),
                ("CHARTREUSE1", 127, 255, 0),
                ("CHARTREUSE2", 118, 238, 0),
                ("CHARTREUSE3", 102, 205, 0),
                ("CHARTREUSE4", 69, 139, 0),
                ("CHOCOLATE", 210, 105, 30),
                ("CHOCOLATE1", 255, 127, 36),
                ("CHOCOLATE2", 238, 118, 33),
                ("CHOCOLATE3", 205, 102, 29),
                ("CHOCOLATE4", 139, 69, 19),
                ("COFFEE", 156, 79, 0),
                ("CORAL", 255, 127, 80),
                ("CORAL1", 255, 114, 86),
                ("CORAL2", 238, 106, 80),
                ("CORAL3", 205, 91, 69),
                ("CORAL4", 139, 62, 47),
                ("CORNFLOWERBLUE", 100, 149, 237),
                ("CORNSILK", 255, 248, 220),
                ("CORNSILK1", 255, 248, 220),
                ("CORNSILK2", 238, 232, 205),
                ("CORNSILK3", 205, 200, 177),
                ("CORNSILK4", 139, 136, 120),
                ("CYAN", 0, 255, 255),
                ("CYAN1", 0, 255, 255),
                ("CYAN2", 0, 238, 238),
                ("CYAN3", 0, 205, 205),
                ("CYAN4", 0, 139, 139),
                ("DARKBLUE", 0, 0, 139),
                ("DARKCYAN", 0, 139, 139),
                ("DARKGOLDENROD", 184, 134, 11),
                ("DARKGOLDENROD1", 255, 185, 15),
                ("DARKGOLDENROD2", 238, 173, 14),
                ("DARKGOLDENROD3", 205, 149, 12),
                ("DARKGOLDENROD4", 139, 101, 8),
                ("DARKGREEN", 0, 100, 0),
                ("DARKGRAY", 169, 169, 169),
                ("DARKKHAKI", 189, 183, 107),
                ("DARKMAGENTA", 139, 0, 139),
                ("DARKOLIVEGREEN", 85, 107, 47),
                ("DARKOLIVEGREEN1", 202, 255, 112),
                ("DARKOLIVEGREEN2", 188, 238, 104),
                ("DARKOLIVEGREEN3", 162, 205, 90),
                ("DARKOLIVEGREEN4", 110, 139, 61),
                ("DARKORANGE", 255, 140, 0),
                ("DARKORANGE1", 255, 127, 0),
                ("DARKORANGE2", 238, 118, 0),
                ("DARKORANGE3", 205, 102, 0),
                ("DARKORANGE4", 139, 69, 0),
                ("DARKORCHID", 153, 50, 204),
                ("DARKORCHID1", 191, 62, 255),
                ("DARKORCHID2", 178, 58, 238),
                ("DARKORCHID3", 154, 50, 205),
                ("DARKORCHID4", 104, 34, 139),
                ("DARKRED", 139, 0, 0),
                ("DARKSALMON", 233, 150, 122),
                ("DARKSEAGREEN", 143, 188, 143),
                ("DARKSEAGREEN1", 193, 255, 193),
                ("DARKSEAGREEN2", 180, 238, 180),
                ("DARKSEAGREEN3", 155, 205, 155),
                ("DARKSEAGREEN4", 105, 139, 105),
                ("DARKSLATEBLUE", 72, 61, 139),
                ("DARKSLATEGRAY", 47, 79, 79),
                ("DARKTURQUOISE", 0, 206, 209),
                ("DARKVIOLET", 148, 0, 211),
                ("DEEPPINK", 255, 20, 147),
                ("DEEPPINK1", 255, 20, 147),
                ("DEEPPINK2", 238, 18, 137),
                ("DEEPPINK3", 205, 16, 118),
                ("DEEPPINK4", 139, 10, 80),
                ("DEEPSKYBLUE", 0, 191, 255),
                ("DEEPSKYBLUE1", 0, 191, 255),
                ("DEEPSKYBLUE2", 0, 178, 238),
                ("DEEPSKYBLUE3", 0, 154, 205),
                ("DEEPSKYBLUE4", 0, 104, 139),
                ("DIMGRAY", 105, 105, 105),
                ("DODGERBLUE", 30, 144, 255),
                ("DODGERBLUE1", 30, 144, 255),
                ("DODGERBLUE2", 28, 134, 238),
                ("DODGERBLUE3", 24, 116, 205),
                ("DODGERBLUE4", 16, 78, 139),
                ("FIREBRICK", 178, 34, 34),
                ("FIREBRICK1", 255, 48, 48),
                ("FIREBRICK2", 238, 44, 44),
                ("FIREBRICK3", 205, 38, 38),
                ("FIREBRICK4", 139, 26, 26),
                ("FLORALWHITE", 255, 250, 240),
                ("FORESTGREEN", 34, 139, 34),
                ("GAINSBORO", 220, 220, 220),
                ("GHOSTWHITE", 248, 248, 255),
                ("GOLD", 255, 215, 0),
                ("GOLD1", 255, 215, 0),
                ("GOLD2", 238, 201, 0),
                ("GOLD3", 205, 173, 0),
                ("GOLD4", 139, 117, 0),
                ("GOLDENROD", 218, 165, 32),
                ("GOLDENROD1", 255, 193, 37),
                ("GOLDENROD2", 238, 180, 34),
                ("GOLDENROD3", 205, 155, 29),
                ("GOLDENROD4", 139, 105, 20),
                ("GREEN YELLOW", 173, 255, 47),
                ("GREEN", 0, 255, 0),
                ("GREEN1", 0, 255, 0),
                ("GREEN2", 0, 238, 0),
                ("GREEN3", 0, 205, 0),
                ("GREEN4", 0, 139, 0),
                ("GREENYELLOW", 173, 255, 47),
                ("GRAY", 190, 190, 190),
                ("GRAY0", 0, 0, 0),
                ("GRAY1", 3, 3, 3),
                ("GRAY10", 26, 26, 26),
                ("GRAY100", 255, 255, 255),
                ("GRAY11", 28, 28, 28),
                ("GRAY12", 31, 31, 31),
                ("GRAY13", 33, 33, 33),
                ("GRAY14", 36, 36, 36),
                ("GRAY15", 38, 38, 38),
                ("GRAY16", 41, 41, 41),
                ("GRAY17", 43, 43, 43),
                ("GRAY18", 46, 46, 46),
                ("GRAY19", 48, 48, 48),
                ("GRAY2", 5, 5, 5),
                ("GRAY20", 51, 51, 51),
                ("GRAY21", 54, 54, 54),
                ("GRAY22", 56, 56, 56),
                ("GRAY23", 59, 59, 59),
                ("GRAY24", 61, 61, 61),
                ("GRAY25", 64, 64, 64),
                ("GRAY26", 66, 66, 66),
                ("GRAY27", 69, 69, 69),
                ("GRAY28", 71, 71, 71),
                ("GRAY29", 74, 74, 74),
                ("GRAY3", 8, 8, 8),
                ("GRAY30", 77, 77, 77),
                ("GRAY31", 79, 79, 79),
                ("GRAY32", 82, 82, 82),
                ("GRAY33", 84, 84, 84),
                ("GRAY34", 87, 87, 87),
                ("GRAY35", 89, 89, 89),
                ("GRAY36", 92, 92, 92),
                ("GRAY37", 94, 94, 94),
                ("GRAY38", 97, 97, 97),
                ("GRAY39", 99, 99, 99),
                ("GRAY4", 10, 10, 10),
                ("GRAY40", 102, 102, 102),
                ("GRAY41", 105, 105, 105),
                ("GRAY42", 107, 107, 107),
                ("GRAY43", 110, 110, 110),
                ("GRAY44", 112, 112, 112),
                ("GRAY45", 115, 115, 115),
                ("GRAY46", 117, 117, 117),
                ("GRAY47", 120, 120, 120),
                ("GRAY48", 122, 122, 122),
                ("GRAY49", 125, 125, 125),
                ("GRAY5", 13, 13, 13),
                ("GRAY50", 127, 127, 127),
                ("GRAY51", 130, 130, 130),
                ("GRAY52", 133, 133, 133),
                ("GRAY53", 135, 135, 135),
                ("GRAY54", 138, 138, 138),
                ("GRAY55", 140, 140, 140),
                ("GRAY56", 143, 143, 143),
                ("GRAY57", 145, 145, 145),
                ("GRAY58", 148, 148, 148),
                ("GRAY59", 150, 150, 150),
                ("GRAY6", 15, 15, 15),
                ("GRAY60", 153, 153, 153),
                ("GRAY61", 156, 156, 156),
                ("GRAY62", 158, 158, 158),
                ("GRAY63", 161, 161, 161),
                ("GRAY64", 163, 163, 163),
                ("GRAY65", 166, 166, 166),
                ("GRAY66", 168, 168, 168),
                ("GRAY67", 171, 171, 171),
                ("GRAY68", 173, 173, 173),
                ("GRAY69", 176, 176, 176),
                ("GRAY7", 18, 18, 18),
                ("GRAY70", 179, 179, 179),
                ("GRAY71", 181, 181, 181),
                ("GRAY72", 184, 184, 184),
                ("GRAY73", 186, 186, 186),
                ("GRAY74", 189, 189, 189),
                ("GRAY75", 191, 191, 191),
                ("GRAY76", 194, 194, 194),
                ("GRAY77", 196, 196, 196),
                ("GRAY78", 199, 199, 199),
                ("GRAY79", 201, 201, 201),
                ("GRAY8", 20, 20, 20),
                ("GRAY80", 204, 204, 204),
                ("GRAY81", 207, 207, 207),
                ("GRAY82", 209, 209, 209),
                ("GRAY83", 212, 212, 212),
                ("GRAY84", 214, 214, 214),
                ("GRAY85", 217, 217, 217),
                ("GRAY86", 219, 219, 219),
                ("GRAY87", 222, 222, 222),
                ("GRAY88", 224, 224, 224),
                ("GRAY89", 227, 227, 227),
                ("GRAY9", 23, 23, 23),
                ("GRAY90", 229, 229, 229),
                ("GRAY91", 232, 232, 232),
                ("GRAY92", 235, 235, 235),
                ("GRAY93", 237, 237, 237),
                ("GRAY94", 240, 240, 240),
                ("GRAY95", 242, 242, 242),
                ("GRAY96", 245, 245, 245),
                ("GRAY97", 247, 247, 247),
                ("GRAY98", 250, 250, 250),
                ("GRAY99", 252, 252, 252),
                ("HONEYDEW", 240, 255, 240),
                ("HONEYDEW1", 240, 255, 240),
                ("HONEYDEW2", 224, 238, 224),
                ("HONEYDEW3", 193, 205, 193),
                ("HONEYDEW4", 131, 139, 131),
                ("HOTPINK", 255, 105, 180),
                ("HOTPINK1", 255, 110, 180),
                ("HOTPINK2", 238, 106, 167),
                ("HOTPINK3", 205, 96, 144),
                ("HOTPINK4", 139, 58, 98),
                ("INDIANRED", 205, 92, 92),
                ("INDIANRED1", 255, 106, 106),
                ("INDIANRED2", 238, 99, 99),
                ("INDIANRED3", 205, 85, 85),
                ("INDIANRED4", 139, 58, 58),
                ("IVORY", 255, 255, 240),
                ("IVORY1", 255, 255, 240),
                ("IVORY2", 238, 238, 224),
                ("IVORY3", 205, 205, 193),
                ("IVORY4", 139, 139, 131),
                ("KHAKI", 240, 230, 140),
                ("KHAKI1", 255, 246, 143),
                ("KHAKI2", 238, 230, 133),
                ("KHAKI3", 205, 198, 115),
                ("KHAKI4", 139, 134, 78),
                ("LAVENDER", 230, 230, 250),
                ("LAVENDERBLUSH", 255, 240, 245),
                ("LAVENDERBLUSH1", 255, 240, 245),
                ("LAVENDERBLUSH2", 238, 224, 229),
                ("LAVENDERBLUSH3", 205, 193, 197),
                ("LAVENDERBLUSH4", 139, 131, 134),
                ("LAWNGREEN", 124, 252, 0),
                ("LEMONCHIFFON", 255, 250, 205),
                ("LEMONCHIFFON1", 255, 250, 205),
                ("LEMONCHIFFON2", 238, 233, 191),
                ("LEMONCHIFFON3", 205, 201, 165),
                ("LEMONCHIFFON4", 139, 137, 112),
                ("LIGHTBLUE", 173, 216, 230),
                ("LIGHTBLUE1", 191, 239, 255),
                ("LIGHTBLUE2", 178, 223, 238),
                ("LIGHTBLUE3", 154, 192, 205),
                ("LIGHTBLUE4", 104, 131, 139),
                ("LIGHTCORAL", 240, 128, 128),
                ("LIGHTCYAN", 224, 255, 255),
                ("LIGHTCYAN1", 224, 255, 255),
                ("LIGHTCYAN2", 209, 238, 238),
                ("LIGHTCYAN3", 180, 205, 205),
                ("LIGHTCYAN4", 122, 139, 139),
                ("LIGHTGOLDENROD", 238, 221, 130),
                ("LIGHTGOLDENROD1", 255, 236, 139),
                ("LIGHTGOLDENROD2", 238, 220, 130),
                ("LIGHTGOLDENROD3", 205, 190, 112),
                ("LIGHTGOLDENROD4", 139, 129, 76),
                ("LIGHTGOLDENRODYELLOW", 250, 250, 210),
                ("LIGHTGREEN", 144, 238, 144),
                ("LIGHTGRAY", 211, 211, 211),
                ("LIGHTPINK", 255, 182, 193),
                ("LIGHTPINK1", 255, 174, 185),
                ("LIGHTPINK2", 238, 162, 173),
                ("LIGHTPINK3", 205, 140, 149),
                ("LIGHTPINK4", 139, 95, 101),
                ("LIGHTSALMON", 255, 160, 122),
                ("LIGHTSALMON1", 255, 160, 122),
                ("LIGHTSALMON2", 238, 149, 114),
                ("LIGHTSALMON3", 205, 129, 98),
                ("LIGHTSALMON4", 139, 87, 66),
                ("LIGHTSEAGREEN", 32, 178, 170),
                ("LIGHTSKYBLUE", 135, 206, 250),
                ("LIGHTSKYBLUE1", 176, 226, 255),
                ("LIGHTSKYBLUE2", 164, 211, 238),
                ("LIGHTSKYBLUE3", 141, 182, 205),
                ("LIGHTSKYBLUE4", 96, 123, 139),
                ("LIGHTSLATEBLUE", 132, 112, 255),
                ("LIGHTSLATEGRAY", 119, 136, 153),
                ("LIGHTSTEELBLUE", 176, 196, 222),
                ("LIGHTSTEELBLUE1", 202, 225, 255),
                ("LIGHTSTEELBLUE2", 188, 210, 238),
                ("LIGHTSTEELBLUE3", 162, 181, 205),
                ("LIGHTSTEELBLUE4", 110, 123, 139),
                ("LIGHTYELLOW", 255, 255, 224),
                ("LIGHTYELLOW1", 255, 255, 224),
                ("LIGHTYELLOW2", 238, 238, 209),
                ("LIGHTYELLOW3", 205, 205, 180),
                ("LIGHTYELLOW4", 139, 139, 122),
                ("LIMEGREEN", 50, 205, 50),
                ("LINEN", 250, 240, 230),
                ("MAGENTA", 255, 0, 255),
                ("MAGENTA1", 255, 0, 255),
                ("MAGENTA2", 238, 0, 238),
                ("MAGENTA3", 205, 0, 205),
                ("MAGENTA4", 139, 0, 139),
                ("MAROON", 176, 48, 96),
                ("MAROON1", 255, 52, 179),
                ("MAROON2", 238, 48, 167),
                ("MAROON3", 205, 41, 144),
                ("MAROON4", 139, 28, 98),
                ("MEDIUMAQUAMARINE", 102, 205, 170),
                ("MEDIUMBLUE", 0, 0, 205),
                ("MEDIUMORCHID", 186, 85, 211),
                ("MEDIUMORCHID1", 224, 102, 255),
                ("MEDIUMORCHID2", 209, 95, 238),
                ("MEDIUMORCHID3", 180, 82, 205),
                ("MEDIUMORCHID4", 122, 55, 139),
                ("MEDIUMPURPLE", 147, 112, 219),
                ("MEDIUMPURPLE1", 171, 130, 255),
                ("MEDIUMPURPLE2", 159, 121, 238),
                ("MEDIUMPURPLE3", 137, 104, 205),
                ("MEDIUMPURPLE4", 93, 71, 139),
                ("MEDIUMSEAGREEN", 60, 179, 113),
                ("MEDIUMSLATEBLUE", 123, 104, 238),
                ("MEDIUMSPRINGGREEN", 0, 250, 154),
                ("MEDIUMTURQUOISE", 72, 209, 204),
                ("MEDIUMVIOLETRED", 199, 21, 133),
                ("MIDNIGHTBLUE", 25, 25, 112),
                ("MINTCREAM", 245, 255, 250),
                ("MISTYROSE", 255, 228, 225),
                ("MISTYROSE1", 255, 228, 225),
                ("MISTYROSE2", 238, 213, 210),
                ("MISTYROSE3", 205, 183, 181),
                ("MISTYROSE4", 139, 125, 123),
                ("MOCCASIN", 255, 228, 181),
                ("MUPDFBLUE", 37, 114, 172),
                ("NAVAJOWHITE", 255, 222, 173),
                ("NAVAJOWHITE1", 255, 222, 173),
                ("NAVAJOWHITE2", 238, 207, 161),
                ("NAVAJOWHITE3", 205, 179, 139),
                ("NAVAJOWHITE4", 139, 121, 94),
                ("NAVY", 0, 0, 128),
                ("NAVYBLUE", 0, 0, 128),
                ("OLDLACE", 253, 245, 230),
                ("OLIVEDRAB", 107, 142, 35),
                ("OLIVEDRAB1", 192, 255, 62),
                ("OLIVEDRAB2", 179, 238, 58),
                ("OLIVEDRAB3", 154, 205, 50),
                ("OLIVEDRAB4", 105, 139, 34),
                ("ORANGE", 255, 165, 0),
                ("ORANGE1", 255, 165, 0),
                ("ORANGE2", 238, 154, 0),
                ("ORANGE3", 205, 133, 0),
                ("ORANGE4", 139, 90, 0),
                ("ORANGERED", 255, 69, 0),
                ("ORANGERED1", 255, 69, 0),
                ("ORANGERED2", 238, 64, 0),
                ("ORANGERED3", 205, 55, 0),
                ("ORANGERED4", 139, 37, 0),
                ("ORCHID", 218, 112, 214),
                ("ORCHID1", 255, 131, 250),
                ("ORCHID2", 238, 122, 233),
                ("ORCHID3", 205, 105, 201),
                ("ORCHID4", 139, 71, 137),
                ("PALEGOLDENROD", 238, 232, 170),
                ("PALEGREEN", 152, 251, 152),
                ("PALEGREEN1", 154, 255, 154),
                ("PALEGREEN2", 144, 238, 144),
                ("PALEGREEN3", 124, 205, 124),
                ("PALEGREEN4", 84, 139, 84),
                ("PALETURQUOISE", 175, 238, 238),
                ("PALETURQUOISE1", 187, 255, 255),
                ("PALETURQUOISE2", 174, 238, 238),
                ("PALETURQUOISE3", 150, 205, 205),
                ("PALETURQUOISE4", 102, 139, 139),
                ("PALEVIOLETRED", 219, 112, 147),
                ("PALEVIOLETRED1", 255, 130, 171),
                ("PALEVIOLETRED2", 238, 121, 159),
                ("PALEVIOLETRED3", 205, 104, 137),
                ("PALEVIOLETRED4", 139, 71, 93),
                ("PAPAYAWHIP", 255, 239, 213),
                ("PEACHPUFF", 255, 218, 185),
                ("PEACHPUFF1", 255, 218, 185),
                ("PEACHPUFF2", 238, 203, 173),
                ("PEACHPUFF3", 205, 175, 149),
                ("PEACHPUFF4", 139, 119, 101),
                ("PERU", 205, 133, 63),
                ("PINK", 255, 192, 203),
                ("PINK1", 255, 181, 197),
                ("PINK2", 238, 169, 184),
                ("PINK3", 205, 145, 158),
                ("PINK4", 139, 99, 108),
                ("PLUM", 221, 160, 221),
                ("PLUM1", 255, 187, 255),
                ("PLUM2", 238, 174, 238),
                ("PLUM3", 205, 150, 205),
                ("PLUM4", 139, 102, 139),
                ("POWDERBLUE", 176, 224, 230),
                ("PURPLE", 160, 32, 240),
                ("PURPLE1", 155, 48, 255),
                ("PURPLE2", 145, 44, 238),
                ("PURPLE3", 125, 38, 205),
                ("PURPLE4", 85, 26, 139),
                ("PY_COLOR", 240, 255, 210),
                ("RED", 255, 0, 0),
                ("RED1", 255, 0, 0),
                ("RED2", 238, 0, 0),
                ("RED3", 205, 0, 0),
                ("RED4", 139, 0, 0),
                ("ROSYBROWN", 188, 143, 143),
                ("ROSYBROWN1", 255, 193, 193),
                ("ROSYBROWN2", 238, 180, 180),
                ("ROSYBROWN3", 205, 155, 155),
                ("ROSYBROWN4", 139, 105, 105),
                ("ROYALBLUE", 65, 105, 225),
                ("ROYALBLUE1", 72, 118, 255),
                ("ROYALBLUE2", 67, 110, 238),
                ("ROYALBLUE3", 58, 95, 205),
                ("ROYALBLUE4", 39, 64, 139),
                ("SADDLEBROWN", 139, 69, 19),
                ("SALMON", 250, 128, 114),
                ("SALMON1", 255, 140, 105),
                ("SALMON2", 238, 130, 98),
                ("SALMON3", 205, 112, 84),
                ("SALMON4", 139, 76, 57),
                ("SANDYBROWN", 244, 164, 96),
                ("SEAGREEN", 46, 139, 87),
                ("SEAGREEN1", 84, 255, 159),
                ("SEAGREEN2", 78, 238, 148),
                ("SEAGREEN3", 67, 205, 128),
                ("SEAGREEN4", 46, 139, 87),
                ("SEASHELL", 255, 245, 238),
                ("SEASHELL1", 255, 245, 238),
                ("SEASHELL2", 238, 229, 222),
                ("SEASHELL3", 205, 197, 191),
                ("SEASHELL4", 139, 134, 130),
                ("SIENNA", 160, 82, 45),
                ("SIENNA1", 255, 130, 71),
                ("SIENNA2", 238, 121, 66),
                ("SIENNA3", 205, 104, 57),
                ("SIENNA4", 139, 71, 38),
                ("SKYBLUE", 135, 206, 235),
                ("SKYBLUE1", 135, 206, 255),
                ("SKYBLUE2", 126, 192, 238),
                ("SKYBLUE3", 108, 166, 205),
                ("SKYBLUE4", 74, 112, 139),
                ("SLATEBLUE", 106, 90, 205),
                ("SLATEBLUE1", 131, 111, 255),
                ("SLATEBLUE2", 122, 103, 238),
                ("SLATEBLUE3", 105, 89, 205),
                ("SLATEBLUE4", 71, 60, 139),
                ("SLATEGRAY", 112, 128, 144),
                ("SNOW", 255, 250, 250),
                ("SNOW1", 255, 250, 250),
                ("SNOW2", 238, 233, 233),
                ("SNOW3", 205, 201, 201),
                ("SNOW4", 139, 137, 137),
                ("SPRINGGREEN", 0, 255, 127),
                ("SPRINGGREEN1", 0, 255, 127),
                ("SPRINGGREEN2", 0, 238, 118),
                ("SPRINGGREEN3", 0, 205, 102),
                ("SPRINGGREEN4", 0, 139, 69),
                ("STEELBLUE", 70, 130, 180),
                ("STEELBLUE1", 99, 184, 255),
                ("STEELBLUE2", 92, 172, 238),
                ("STEELBLUE3", 79, 148, 205),
                ("STEELBLUE4", 54, 100, 139),
                ("TAN", 210, 180, 140),
                ("TAN1", 255, 165, 79),
                ("TAN2", 238, 154, 73),
                ("TAN3", 205, 133, 63),
                ("TAN4", 139, 90, 43),
                ("THISTLE", 216, 191, 216),
                ("THISTLE1", 255, 225, 255),
                ("THISTLE2", 238, 210, 238),
                ("THISTLE3", 205, 181, 205),
                ("THISTLE4", 139, 123, 139),
                ("TOMATO", 255, 99, 71),
                ("TOMATO1", 255, 99, 71),
                ("TOMATO2", 238, 92, 66),
                ("TOMATO3", 205, 79, 57),
                ("TOMATO4", 139, 54, 38),
                ("TURQUOISE", 64, 224, 208),
                ("TURQUOISE1", 0, 245, 255),
                ("TURQUOISE2", 0, 229, 238),
                ("TURQUOISE3", 0, 197, 205),
                ("TURQUOISE4", 0, 134, 139),
                ("VIOLET", 238, 130, 238),
                ("VIOLETRED", 208, 32, 144),
                ("VIOLETRED1", 255, 62, 150),
                ("VIOLETRED2", 238, 58, 140),
                ("VIOLETRED3", 205, 50, 120),
                ("VIOLETRED4", 139, 34, 82),
                ("WHEAT", 245, 222, 179),
                ("WHEAT1", 255, 231, 186),
                ("WHEAT2", 238, 216, 174),
                ("WHEAT3", 205, 186, 150),
                ("WHEAT4", 139, 126, 102),
                ("WHITE", 255, 255, 255),
                ("WHITESMOKE", 245, 245, 245),
                ("YELLOW", 255, 255, 0),
                ("YELLOW1", 255, 255, 0),
                ("YELLOW2", 238, 238, 0),
                ("YELLOW3", 205, 205, 0),
                ("YELLOW4", 139, 139, 0),
                ("YELLOWGREEN", 154, 205, 50),
            };
        }

        public static float[] GetColor(string name)
        {
            try
            {
                (string, float, float, float) c = Utils.GetColorInfoList()[
                    Utils.GetColorList().IndexOf(name.ToUpper())
                ];
                return new float[] { c.Item2 / 255.0f, c.Item3 / 255.0f, c.Item4 / 255.0f };
            }
            catch //(Exception e)
            {
                return new float[] { 1, 1, 1 };
            }
        }

        public static float[] GetColorHSV(string name)
        {
            (string, float, float, float) x;
            try
            {
                x = GetColorInfoList()[GetColorList().IndexOf(name.ToUpper())];
            }
            catch// (Exception e)
            {
                return new float[] { -1, -1, -1 };
            }
            float r = x.Item2 / 255.0f;
            float g = x.Item3 / 255.0f;
            float b = x.Item4 / 255.0f;
            float cmax = Math.Max(Math.Max(r, g), b);
            float V = (float)Math.Round(cmax * 100, 1);

            float cmin = Math.Min(Math.Min(r, g), b);
            float delta = cmax - cmin;
            float hue = 0;

            if (delta == 0)
                hue = 0;
            else if (cmax == r)
                hue = 60.0f * (((g - b) / delta) % 6);
            else if (cmax == g)
                hue = 60.0f * (((b - r) / delta) + 2);
            else
                hue = 60.0f * (((r - g) / delta) + 4);
            float H = (int)(Math.Round(hue));
            float sat = 0;
            if (cmax == 0)
                sat = 0;
            else
                sat = delta / cmax;
            float S = (int)Math.Round(sat * 100);

            return new float[] { H, S, V };
        }

        internal static PdfObj EnsureOCProperties(PdfDocument pdf)
        {
            PdfObj ocp = pdf.pdf_trailer()
                .pdf_dict_get(new PdfObj("Root"))
                .pdf_dict_get(new PdfObj("OCProperties"));
            if (ocp.m_internal != null)
                return ocp;

            PdfObj root = pdf.pdf_trailer().pdf_dict_get(new PdfObj("Root"));
            ocp = root.pdf_dict_put_dict(new PdfObj("OCProperties"), 2);
            ocp.pdf_dict_put_array(new PdfObj("OCGs"), 0);
            PdfObj D = ocp.pdf_dict_put_dict(new PdfObj("D"), 5);
            D.pdf_dict_put_array(new PdfObj("ON"), 0);
            D.pdf_dict_put_array(new PdfObj("OFF"), 0);
            D.pdf_dict_put_array(new PdfObj("Order"), 0);
            D.pdf_dict_put_array(new PdfObj("RBGroups"), 0);

            return ocp;
        }

        internal static string GetFontName(fz_font font)
        {
            string name = mupdf.mupdf.fz_font_name(new FzFont(font));
            int s = name.IndexOf("+");
            return name.Substring(s + 1, name.Length - s - 1);
        }

        internal static void LoadEmbeddedDllForWindows()
        {
            string binaryDir = AppContext.BaseDirectory;
            if (!File.Exists(binaryDir + "mupdfcpp64.dll"))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceStream = assembly.GetManifestResourceStream("mupdfcpp64.dll");
                var tempFile = File.Create(binaryDir + "mupdfcpp64.dll");

                resourceStream?.CopyTo(tempFile);
                resourceStream?.Dispose();
                tempFile.Dispose();
            }

            if (!File.Exists(binaryDir + "mupdfcsharp.dll"))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceStream = assembly.GetManifestResourceStream("mupdfcsharp.dll");
                var tempFile = File.Create(binaryDir + "mupdfcsharp.dll");

                resourceStream?.CopyTo(tempFile);
                resourceStream?.Dispose();
                tempFile.Dispose();
            }
        }

        internal static void LoadEmbeddedDllForLinux()
        {
            string binaryDir = AppContext.BaseDirectory;
            if (!File.Exists(binaryDir + "libmupdf.so"))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceStream = assembly.GetManifestResourceStream("libmupdf.so");
                if (resourceStream != null)
                {
                    var tempFile = File.Create(binaryDir + "libmupdf.so");
                    resourceStream.CopyTo(tempFile);
                    resourceStream.Dispose();
                    tempFile.Dispose();
                }
            }

            if (!File.Exists(binaryDir + "libmupdf.so.26.0"))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceStream = assembly.GetManifestResourceStream("libmupdf.so.26.0");
                if (resourceStream != null)
                {
                    var tempFile = File.Create(binaryDir + "libmupdf.so.26.0");
                    resourceStream.CopyTo(tempFile);
                    resourceStream.Dispose();
                    tempFile.Dispose();
                }
            }

            if (!File.Exists(binaryDir + "libmupdfcpp.so"))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceStream = assembly.GetManifestResourceStream("libmupdfcpp.so");
                if (resourceStream != null)
                {
                    var tempFile = File.Create(binaryDir + "libmupdfcpp.so");
                    resourceStream.CopyTo(tempFile);
                    resourceStream.Dispose();
                    tempFile.Dispose();
                }
            }

            if (!File.Exists(binaryDir + "libmupdfcpp.so.26.0"))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceStream = assembly.GetManifestResourceStream("libmupdfcpp.so.26.0");
                if (resourceStream != null)
                {
                    var tempFile = File.Create(binaryDir + "libmupdfcpp.so.26.0");
                    resourceStream.CopyTo(tempFile);
                    resourceStream.Dispose();
                    tempFile.Dispose();
                }
            }

            if (!File.Exists(binaryDir + "mupdfcsharp.dll"))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceStream = assembly.GetManifestResourceStream("mupdfcsharp.so");
                if (resourceStream != null)
                {
                    var tempFile = File.Create(binaryDir + "mupdfcsharp.dll");
                    resourceStream.CopyTo(tempFile);
                    resourceStream.Dispose();
                    tempFile.Dispose();
                }
            }
        }

        internal static void AddLayerConfig(
            PdfDocument pdf,
            string name,
            string creator,
            OCLayerConfig on
        )
        {
            try
            {
                PdfObj ocp = Utils.EnsureOCProperties(pdf);
                PdfObj configs = ocp.pdf_dict_get(new PdfObj("Configs"));
                if (configs.pdf_is_array() == 0)
                    configs = ocp.pdf_dict_put_array(new PdfObj("Configs"), 1);
                PdfObj d = pdf.pdf_new_dict(5);
                d.pdf_dict_put_text_string(new PdfObj("Name"), name);
                if (!string.IsNullOrEmpty(creator))
                    d.pdf_dict_put_text_string(new PdfObj("Creator"), creator);
                d.pdf_dict_put(new PdfObj("BaseState"), new PdfObj("OFF"));
                PdfObj onarray = d.pdf_dict_put_array(new PdfObj("ON"), 5);
                if (on == null) { }
                else
                {
                    PdfObj ocgs = ocp.pdf_dict_get(new PdfObj("OCGs"));
                    int xref = on.Number;
                    PdfObj ind = pdf.pdf_new_indirect(xref, 0);
                    if (ocgs.pdf_array_contains(ind) != 0)
                        onarray.pdf_array_push(ind);
                }
                configs.pdf_array_push(d);
            }
            catch// (Exception e)
            {
                throw;
            }
        }

        internal static IntPtr Utf16_Utf8Ptr(string s)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(s);
            byte[] nullTerminator = { 0 };
            byte[] bytes = utf8Bytes.Concat(nullTerminator).ToArray();
            IntPtr utf8Ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, utf8Ptr, bytes.Length);

            return utf8Ptr;
        }

        internal static void SetDotCultureForNumber()
        {
            CultureInfo culture = new CultureInfo("en-US"); // or any specific culture you want
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.NumberFormat.CurrencyDecimalSeparator = ".";

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public static void InitApp()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (Utils.IsInitialized)
                    return;

                Utils.SetDotCultureForNumber();
                //Utils.LoadEmbeddedDllForWindows();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (Utils.IsInitialized)
                    return;

                Utils.SetDotCultureForNumber();
                //Utils.LoadEmbeddedDllForLinux();
            }
            else
            {
                return;
            }
            Utils.IsInitialized = true;
        }

        /// <summary>
        /// Calculate area of rectangle. parameter is one of 'px' (default), 'in', 'cm', or 'mm'.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static float GetArea(Rect rect, string unit = "px")
        {
            Dictionary<string, (float, float)> u = new Dictionary<string, (float, float)>()
            {
                { "px", (1f, 1f) },
                { "in", (1f, 72.0f) },
                { "cm", (2.54f, 72.0f) },
                { "mm", (25.4f, 72f) }
            };
            float f = (float)Math.Pow(u[unit].Item1 / u[unit].Item2, 2);

            return f * rect.Width * rect.Height;
        }

        /// <summary>
        /// Return basic properties of an image.
        /// </summary>
        /// <param name="image">bytes array opened</param>
        /// <param name="keepImage"></param>
        /// <returns></returns>
        public static ImageInfo GetImageProfile(byte[] image, int keepImage = 0)
        {
            if (image == null)
                return null;

            int len = image.Length;
            if (len < 8)
            {
                Console.WriteLine("bad image data");
                return null;
            }

            //nint swigImage = Marshal.AllocHGlobal(len);
            IntPtr swigImage = Marshal.AllocHGlobal(len);
            Marshal.Copy(image, 0, swigImage, len);
            SWIGTYPE_p_unsigned_char c = new SWIGTYPE_p_unsigned_char(swigImage, true);
            int type = mupdf.mupdf.fz_recognize_image_format(c);
            if (type == (int)ImageType.FZ_IMAGE_UNKNOWN)
                return null;

            // get properties for imageinfo
            FzBuffer res = null;
            if (keepImage != 0)
                res = mupdf.mupdf.fz_new_buffer_from_copied_data(c, (uint)len);
            else
                res = mupdf.mupdf.fz_new_buffer_from_shared_data(c, (uint)len);
            FzImage img = mupdf.mupdf.fz_new_image_from_buffer(res);
            FzMatrix ctm = mupdf.mupdf.fz_image_orientation_matrix(img);
            (int xres, int yres) = img.fz_image_resolution();
            byte orientation = img.fz_image_orientation();
            string csName = img.colorspace().fz_colorspace_name();

            // create imageinfo to return
            ImageInfo ret = new ImageInfo()
            {
                Width = img.w(),
                Height = img.h(),
                Orientation = orientation,
                Matrix = new Matrix(ctm),
                Xres = xres,
                Yres = yres,
                ColorSpace = img.n(),
                Bpc = img.bpc(),
                Ext = GetImageExtension(type),
                CsName = csName
            };

            if (keepImage != 0)
                ret.Image = BinFromBuffer(res);
            return ret;
        }

        public static string ConversionHeader(string i, string filename = "unknown")
        {
            string t = i.ToLower();
            string html =
                @"
                <!DOCTYPE html>
                <html>
                <head>
                <style>
                body{background-color:gray}
                div{position:relative;background-color:white;margin:1em auto}
                p{position:absolute;margin:0}
                img{position:absolute}
                </style>
                </head>
                <body>
                ";

            string xml =
                $@"
                <?xml version='1.0'?>
                <document name='{filename}'>
                ";

            string xhtml =
                @"
                <?xml version='1.0'?>
                <!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'>
                <html xmlns='http://www.w3.org/1999/xhtml'>
                <head>
                <style>
                body{background-color:gray}
                div{background-color:white;margin:1em;padding:1em}
                p{white-space:pre-wrap}
                </style>
                </head>
                <body>
                ";

            string r = "";
            string json = $"{{\"document\": \"{filename}\", \"pages\": [\n";
            if (t == "html")
                r = html;
            else if (t == "json")
                r = json;
            else if (t == "xml")
                r = xml;
            else if (t == "xhtml")
                r = xhtml;
            else
                r = "";

            return r;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static string ConversionTrailer(string i)
        {
            string t = i.ToLower();
            string text = "";
            string json = "]\n}";
            string html = "</body>\n</html>\n";
            string xml = "</document>\n";
            string xhtml = html;
            string r = "";

            if (t == "html")
                r = html;
            else if (t == "json")
                r = json;
            else if (t == "xml")
                r = xml;
            else if (t == "xhtml")
                r = xhtml;
            else
                r = text;

            return r;
        }

        public static Rect EMPTY_RECT()
        {
            return new Rect(Utils.FZ_MAX_INF_RECT, Utils.FZ_MAX_INF_RECT, Utils.FZ_MIN_INF_RECT, Utils.FZ_MIN_INF_RECT);
        }

        public static Quad EMPTY_QUAD()
        {
            return Utils.EMPTY_RECT().Quad;
        }

        public static string GetSortedText(Page page, Rect clip = null, int flags = 0, TextPage textpage = null, int tolerance = 3)
        {
            string LineText(Rect _clip, List<(Rect, string)> _line)
            {
                _line.Sort((l1, l2) =>
                {
                    return (int)((l1.Item1.X0 - l2.Item1.X0) * 10);
                });
                string _ltext = "";
                float x1 = _clip.X0;
                Rect _lrect = Utils.EMPTY_RECT();

                foreach ((Rect r, string t) in _line)
                {
                    _lrect = _lrect | r; // update _line bbox
                    int dist = Math.Max((int)(Math.Round((r.X0 - x1) / r.Width * t.Length)), x1 == _clip.X0 ? 0 : 1); // number of space chars
                    _ltext += new string(' ', dist) + t;
                    x1 = r.X1;
                }

                return _ltext;
            }

            List<WordBlock> wordblocks = Utils.GetTextWords(page, clip, flags, textpage, sort: true, tolerance: tolerance);

            List<(Rect, string)> words = new List<(Rect, string)>();
            foreach (WordBlock block in wordblocks)
                words.Add((new Rect(block.X0, block.Y0, block.X1, block.Y1), block.Text));

            if (words.Count == 0 || words == null)
                return "";
            Rect totalBox = Utils.EMPTY_RECT();
            foreach ((Rect wr, string text) in words)
                totalBox = totalBox | wr;

            List<(Rect, string)> lines = new List<(Rect, string)>();
            List<(Rect, string)> line = new List<(Rect, string)>() { words[0] };
            Rect lrect = words[0].Item1;
            string ltext = "";

            foreach ((Rect wr, string text) in words.Skip(1))
            {
                (Rect w0r, string _) = line[line.Count-1];

                if (Math.Abs(lrect.Y0 - wr.Y0) < tolerance || Math.Abs(lrect.Y1 - wr.Y1) <= tolerance)
                {
                    line.Add((lrect, text));
                    lrect |= wr;
                }
                else
                {
                    ltext = LineText(totalBox, line);
                    lines.Add((lrect, text));
                    line = new List<(Rect, string)>() { (wr, text) };
                    lrect = wr;
                }
            }

            ltext = LineText(totalBox, line);
            lines.Add((lrect, ltext));

            lines.Sort((l1, l2) => { return (int)((l1.Item1.Y1 - l2.Item1.Y1) * 10); });

            string ret = lines[0].Item2;
            float y1 = lines[0].Item1.Y1;

            foreach ((Rect lr, string lt) in lines.Skip(1))
            {
                int distance = Math.Min((int)(Math.Round((lr.Y0 - y1) / lr.Height)), 5);
                string breaks = new String('\n', distance + 1);
                ret += breaks + lt;
                y1 = lr.Y1;
            }

            return ret;
        }

        static bool IsRtl(string text)
        {
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
                    continue;

                var direction = CharUnicodeInfo.GetUnicodeCategory(c);
                if (direction == UnicodeCategory.OtherLetter || 
                    direction == UnicodeCategory.LetterNumber || 
                    direction == UnicodeCategory.LowercaseLetter || 
                    direction == UnicodeCategory.UppercaseLetter)
                {
                    // Check if it's Hebrew, Arabic, or other RTL scripts
                    if ((c >= '\u0590' && c <= '\u08FF') || // Hebrew, Arabic ranges
                        (c >= '\uFB1D' && c <= '\uFEFC'))   // Hebrew presentation forms, Arabic presentation forms
                    {
                        return true; // RTL
                    }
                    else
                    {
                        return false; // LTR
                    }
                }
            }

            // Default: assume LTR if no strong character found
            return false;
        }

        public static string GetTextWithLayout(Page page, Rect clip = null, int flags = 0, int tolerance = 5)
        {
            string LineText(Rect _clip, List<(Rect, string)> _line, int _tolerance)
            {
                _line.Sort((l1, l2) =>
                {
                    return (int)((l1.Item1.X0 - l2.Item1.X0) * 10);
                });
                string _ltext = "";
                Rect _prevRect = Utils.EMPTY_RECT();

                string LRM = "\u200E"; // Left-to-Right Mark
                string RLM = "\u200F"; // Right-to-Left Mark
                bool prevIsRLM = false;

                foreach ((Rect r, string t) in _line)
                {
                    int spaceCount = 0;
                    if (_prevRect.IsEmpty == true)
                        spaceCount = (int)Math.Max(0, (r.X0) / _tolerance);
                    else
                    {
                        float dist = r.X0 - _prevRect.X1;
                        spaceCount = (int)Math.Max(0, dist / _tolerance);
                        if (spaceCount > 1)
                            spaceCount = (int)(r.X0) / _tolerance - _ltext.Length;
                        else if (dist > 1)
                            spaceCount = 1;
                    }
                    if (spaceCount < 0)
                    {
                        spaceCount = 0;
                    }
                    _prevRect = r;
                    _ltext += new string(' ', spaceCount);
                    if (Utils.IsRtl(t))
                    {
                        if (!prevIsRLM)
                            _ltext += RLM;
                        else
                            _ltext += LRM;
                        prevIsRLM = true;
                    }
                    else
                    {
                        if (prevIsRLM)
                            _ltext += LRM;
                        prevIsRLM = false;
                    }


                    // Replace all groups of 1 or more spaces with a thin space
                    string t1 = t.Replace("     ", " ");
                    _ltext += t1;
                }

                return _ltext;
            }

            // check parameters
            if (flags == 0)
            {
                flags = (int)(
                    TextFlags.TEXT_PRESERVE_WHITESPACE
                    | TextFlags.TEXT_PRESERVE_LIGATURES
                    | TextFlags.TEXT_MEDIABOX_CLIP
                    //| TextFlags.TEXT_PRESERVE_IMAGES
                    | TextFlags.TEXT_INHIBIT_SPACES
                    //| TextFlags.TEXT_DEHYPHENATE
                    | TextFlags.TEXT_PRESERVE_SPANS
                    | TextFlags.TEXT_CID_FOR_UNKNOWN_UNICODE
                    | TextFlags.TEXT_ACCURATE_BBOXES
                );
            }
            if (clip == null)
                clip = page.Rect;
            if (tolerance <= 0)
                tolerance = 5;

            List<(Rect, string)>[] words = new List<(Rect, string)>[2];
            words[0] = new List<(Rect, string)>();
            words[1] = new List<(Rect, string)>();

            foreach (Block block in page.GetText("dict", clip:clip, flags: flags, sort:true).Blocks)
            {
                if (block.Lines != null)
                {
                    foreach (Line _line in block.Lines)
                    {
                        if (_line == null)
                            continue;
                        foreach (Span span in _line.Spans)
                        {
                            // horizontal
                            Rect spanRect = new Rect(span.Bbox.X0, span.Bbox.Y0, span.Bbox.X1, span.Bbox.Y1);
                            if (page.Rotation != 0)
                                spanRect = spanRect.Transform(page.RotationMatrix);
                            if (_line.Dir.Y == 0)
                                words[0].Add((spanRect, span.Text));
                            else
                                words[1].Add((spanRect, span.Text));
                        }
                    }
                }
            }

            // sort again.
            words[0].Sort((w1, w2) =>
            {
                if (Utils.IsSameLine(w1.Item1, w2.Item1))
                {
                    return w1.Item1.X0.CompareTo(w2.Item1.X0);
                }
                else
                {
                    float center1 = (w1.Item1.Y0 + w1.Item1.Y1) / 2;
                    float center2 = (w2.Item1.Y0 + w2.Item1.Y1) / 2;

                    return center1.CompareTo(center2);
                }
            });
            words[1].Sort((w1, w2) =>
            {
                if (Utils.IsSameLine(w1.Item1, w2.Item1))
                {
                    return w1.Item1.X0.CompareTo(w2.Item1.X0);
                }
                else
                {
                    float center1 = (w1.Item1.Y0 + w1.Item1.Y1) / 2;
                    float center2 = (w2.Item1.Y0 + w2.Item1.Y1) / 2;

                    return center1.CompareTo(center2);
                }
            });

            string ret = "";
            for (int i = 0; i < 2; i++)
            {
                ret += "\n";
                if (words[i].Count == 0)
                {
                    continue;
                }
                Rect totalBox = Utils.EMPTY_RECT();
                foreach ((Rect wr, string text) in words[i])
                    totalBox = totalBox | wr;

                List<(Rect, string)> lines = new List<(Rect, string)>();
                List<(Rect, string)> line = new List<(Rect, string)>() { words[i][0] };
                Rect lrect = new Rect(words[i][0].Item1);
                string ltext = "";

                foreach ((Rect wr, string text) in words[i].Skip(1))
                {
                    (Rect w0r, string _) = line[line.Count - 1];

                    if (w0r.EqualTo(wr))
                    {
                        continue;
                    }

                    if (w0r.X0 < wr.X0 && Utils.IsSameLine(w0r, wr))
                    {
                        line.Add((wr, text));
                        lrect |= wr;
                    }
                    else
                    {
                        ltext = LineText(totalBox, line, tolerance);
                        lines.Add((lrect, ltext));
                        line = new List<(Rect, string)>() { (wr, text) };
                        lrect = new Rect(wr);
                    }
                }

                ltext = LineText(totalBox, line, tolerance);
                lines.Add((lrect, ltext));

                lines.Sort((l1, l2) => { return (int)((l1.Item1.Y1 - l2.Item1.Y1) * 10); });

                ret += lines[0].Item2;
                float x0 = lines[0].Item1.X0;
                float y1 = lines[0].Item1.Y1;

                foreach ((Rect lr, string lt) in lines.Skip(1))
                {
                    int distance = Math.Min((int)(Math.Round((lr.Y0 - y1) / lr.Height)), tolerance);
                    if (distance < 0)
                    {
                        distance = 0;
                    }
                    string breaks = new String('\n', distance + 1);
                    ret += breaks + lt;
                    x0 = lr.X0;
                    y1 = lr.Y1;
                }
            }

            return ret;
        }

        public static bool IsSameLine(Rect rect0, Rect rect1, float tolerance = 5.0f)
        {
            if (rect0.IsEmpty || rect1.IsEmpty)
                return false;

            if (rect0.Y0 <= rect1.Y0)
            {
                if (rect0.Y1 <= rect1.Y0)
                {
                    return false;
                }
                else
                {
                    if (rect0.Y1 >= rect1.Y1)
                        return true;
                    else
                    {
                        float overlappedHeight = rect0.Y1 - rect1.Y0;
                        if (overlappedHeight < rect1.Height / 2)
                            return false;
                        else
                            return true;
                    }
                }
            }
            else
            {
                if (rect1.Y1 <= rect0.Y0)
                {
                    return false;
                }
                else
                {
                    if (rect1.Y1 >= rect0.Y1)
                        return true;
                    else
                    {
                        float overlappedHeight = rect1.Y1 - rect0.Y0;
                        if (overlappedHeight < rect1.Height / 2)
                            return false;
                        else
                            return true;
                    }
                }
            }
            return false;
        }

        public static int GetBlankLines(Rect rect0, Rect rect1)
        {
            float distanceBetweenRect = 0f;
            float minLineHeight = Math.Min(rect0.Height, rect1.Height);

            if (rect0.Y1 < rect1.Y0)
                distanceBetweenRect = rect1.Y0 - rect0.Y1;
            else if (rect1.Y1 < rect0.Y0)
                distanceBetweenRect = rect0.Y0 - rect1.Y1;

            if (minLineHeight > 0f)
                return (int)(distanceBetweenRect/minLineHeight);

            return 0;
        }

        public static bool IsHorizontalNeighbors(Rect rect0, Rect rect1, float yTolerance = 5.0f, float xGapTolerance = 10.0f)
        {
            // Check if they're on the same line (vertically aligned)
            bool sameLine = IsSameLine(rect0, rect1, yTolerance);

            // Check if they are next to each other (b is to the right of a, or vice versa)
            bool aBeforeB = rect0.X1 <= rect1.X0 && (rect1.X0 - rect0.X1) < xGapTolerance;
            bool bBeforeA = rect1.X1 <= rect0.X0 && (rect0.X0 - rect1.X1) < xGapTolerance;

            return sameLine && (aBeforeB || bBeforeA);
        }
    }

    
}
