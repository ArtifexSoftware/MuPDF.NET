using SkiaSharp;
using System;
using System.Drawing;
using System.Text;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes standart page sizes.
    /// </summary>
    public enum PageSize
    {
        /// <summary>
        /// 
        /// </summary>
        LETTER,
        /// <summary>
        /// 
        /// </summary>
        NOTE,
        /// <summary>
        /// 
        /// </summary>
        LEGAL,
        /// <summary>
        /// 
        /// </summary>
        TABLOID,
        /// <summary>
        /// 
        /// </summary>
        EXECUTIVE,
        /// <summary>
        /// 
        /// </summary>
        POSTCARD,
        /// <summary>
        /// 
        /// </summary>
        A0,
        /// <summary>
        /// 
        /// </summary>
        A1,
        /// <summary>
        /// 
        /// </summary>
        A2,
        /// <summary>
        /// 
        /// </summary>
        A3,
        /// <summary>
        /// 
        /// </summary>
        A4,
        /// <summary>
        /// 
        /// </summary>
        A5,
        /// <summary>
        /// 
        /// </summary>
        A6,
        /// <summary>
        /// 
        /// </summary>
        A7,
        /// <summary>
        /// 
        /// </summary>
        A8,
        /// <summary>
        /// 
        /// </summary>
        A9,
        /// <summary>
        /// 
        /// </summary>
        A10,
        /// <summary>
        /// 
        /// </summary>
        B0,
        /// <summary>
        /// 
        /// </summary>
        B1,
        /// <summary>
        /// 
        /// </summary>
        B2,
        /// <summary>
        /// 
        /// </summary>
        B3,
        /// <summary>
        /// 
        /// </summary>
        B4,
        /// <summary>
        /// 
        /// </summary>
        B5,
        /// <summary>
        /// 
        /// </summary>
        B6,
        /// <summary>
        /// 
        /// </summary>
        B7,
        /// <summary>
        /// 
        /// </summary>
        B8,
        /// <summary>
        /// 
        /// </summary>
        B9,
        /// <summary>
        /// 
        /// </summary>
        B10,
        /// <summary>
        /// 
        /// </summary>
        ARCH_E,
        /// <summary>
        /// 
        /// </summary>
        ARCH_D,
        /// <summary>
        /// 
        /// </summary>
        ARCH_C,
        /// <summary>
        /// 
        /// </summary>
        ARCH_B,
        /// <summary>
        /// 
        /// </summary>
        ARCH_A,
        /// <summary>
        /// 
        /// </summary>
        FLSA,
        /// <summary>
        /// 
        /// </summary>
        FLSE,
        /// <summary>
        /// 
        /// </summary>
        HALFLETTER,
        /// <summary>
        /// 
        /// </summary>
        _11X17,
        /// <summary>
        /// 
        /// </summary>
        ID_1,
        /// <summary>
        /// 
        /// </summary>
        ID_2,
        /// <summary>
        /// 
        /// </summary>
        ID_3,
        /// <summary>
        /// 
        /// </summary>
        LEDGER,
        /// <summary>
        /// 
        /// </summary>
        CROWN_QUARTO,
        /// <summary>
        /// 
        /// </summary>
        LARGE_CROWN_QUARTO,
        /// <summary>
        /// 
        /// </summary>
        DEMY_QUARTO,
        /// <summary>
        /// 
        /// </summary>
        ROYAL_QUARTO,
        /// <summary>
        /// 
        /// </summary>
        CROWN_OCTAVO,
        /// <summary>
        /// 
        /// </summary>
        LARGE_CROWN_OCTAVO,
        /// <summary>
        /// 
        /// </summary>
        DEMY_OCTAVO,
        /// <summary>
        /// 
        /// </summary>
        ROYAL_OCTAVO,
        /// <summary>
        /// 
        /// </summary>
        SMALL_PAPERBACK,
        /// <summary>
        /// 
        /// </summary>
        PENGUIN_SMALL_PAPERBACK,
        /// <summary>
        /// 
        /// </summary>
        PENGUIN_LARGE_PAPERBACK,
    }

    internal static class PageSizeUtils
    {
        internal static SKSize GetPageSize(PageSize pageSize)
        {
            switch (pageSize)
            {
                case PageSize.LETTER:
                    return new SKSize(612, 792);
                case PageSize.NOTE:
                    return new SKSize(540, 720);
                case PageSize.LEGAL:
                    return new SKSize(612, 1008);
                case PageSize.TABLOID:
                    return new SKSize(792, 1224);
                case PageSize.EXECUTIVE:
                    return new SKSize(522, 756);
                case PageSize.POSTCARD:
                    return new SKSize(283, 416);
                case PageSize.A0:
                    return new SKSize(2384, 3370);
                case PageSize.A1:
                    return new SKSize(1684, 2384);
                case PageSize.A2:
                    return new SKSize(1191, 1684);
                case PageSize.A3:
                    return new SKSize(842, 1191);
                case PageSize.A4:
                    return new SKSize(595, 842);
                case PageSize.A5:
                    return new SKSize(420, 595);
                case PageSize.A6:
                    return new SKSize(297, 420);
                case PageSize.A7:
                    return new SKSize(210, 297);
                case PageSize.A8:
                    return new SKSize(148, 210);
                case PageSize.A9:
                    return new SKSize(105, 148);
                case PageSize.A10:
                    return new SKSize(73, 105);
                case PageSize.B0:
                    return new SKSize(2834, 4008);
                case PageSize.B1:
                    return new SKSize(2004, 2834);
                case PageSize.B2:
                    return new SKSize(1417, 2004);
                case PageSize.B3:
                    return new SKSize(1000, 1417);
                case PageSize.B4:
                    return new SKSize(708, 1000);
                case PageSize.B5:
                    return new SKSize(498, 708);
                case PageSize.B6:
                    return new SKSize(354, 498);
                case PageSize.B7:
                    return new SKSize(249, 354);
                case PageSize.B8:
                    return new SKSize(175, 249);
                case PageSize.B9:
                    return new SKSize(124, 175);
                case PageSize.B10:
                    return new SKSize(87, 124);
                case PageSize.ARCH_E:
                    return new SKSize(2592, 3456);
                case PageSize.ARCH_D:
                    return new SKSize(1728, 2592);
                case PageSize.ARCH_C:
                    return new SKSize(1296, 1728);
                case PageSize.ARCH_B:
                    return new SKSize(864, 1296);
                case PageSize.ARCH_A:
                    return new SKSize(648, 864);
                case PageSize.FLSA:
                    return new SKSize(612, 936);
                case PageSize.FLSE:
                    return new SKSize(648, 936);
                case PageSize.HALFLETTER:
                    return new SKSize(396, 612);
                case PageSize._11X17:
                    return new SKSize(792, 1224);
                case PageSize.ID_1:
                    return new SKSize(242.65f, 153);
                case PageSize.ID_2:
                    return new SKSize(297, 210);
                case PageSize.ID_3:
                    return new SKSize(354, 249);
                case PageSize.LEDGER:
                    return new SKSize(1224, 792);
                case PageSize.CROWN_QUARTO:
                    return new SKSize(535, 697);
                case PageSize.LARGE_CROWN_QUARTO:
                    return new SKSize(569, 731);
                case PageSize.DEMY_QUARTO:
                    return new SKSize(620, 782);
                case PageSize.ROYAL_QUARTO:
                    return new SKSize(671, 884);
                case PageSize.CROWN_OCTAVO:
                    return new SKSize(348, 527);
                case PageSize.LARGE_CROWN_OCTAVO:
                    return new SKSize(365, 561);
                case PageSize.DEMY_OCTAVO:
                    return new SKSize(391, 612);
                case PageSize.ROYAL_OCTAVO:
                    return new SKSize(442, 663);
                case PageSize.SMALL_PAPERBACK:
                    return new SKSize(314, 504);
                case PageSize.PENGUIN_SMALL_PAPERBACK:
                    return new SKSize(314, 513);
                case PageSize.PENGUIN_LARGE_PAPERBACK:
                    return new SKSize(365, 561);
                default:
                    return SKSize.Empty;
            }
        }
    }
}
