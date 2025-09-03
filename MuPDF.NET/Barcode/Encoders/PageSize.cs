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
        internal static SizeF GetPageSize(PageSize pageSize)
        {
            switch (pageSize)
            {
                case PageSize.LETTER:
                    return new SizeF(612, 792);
                case PageSize.NOTE:
                    return new SizeF(540, 720);
                case PageSize.LEGAL:
                    return new SizeF(612, 1008);
                case PageSize.TABLOID:
                    return new SizeF(792, 1224);
                case PageSize.EXECUTIVE:
                    return new SizeF(522, 756);
                case PageSize.POSTCARD:
                    return new SizeF(283, 416);
                case PageSize.A0:
                    return new SizeF(2384, 3370);
                case PageSize.A1:
                    return new SizeF(1684, 2384);
                case PageSize.A2:
                    return new SizeF(1191, 1684);
                case PageSize.A3:
                    return new SizeF(842, 1191);
                case PageSize.A4:
                    return new SizeF(595, 842);
                case PageSize.A5:
                    return new SizeF(420, 595);
                case PageSize.A6:
                    return new SizeF(297, 420);
                case PageSize.A7:
                    return new SizeF(210, 297);
                case PageSize.A8:
                    return new SizeF(148, 210);
                case PageSize.A9:
                    return new SizeF(105, 148);
                case PageSize.A10:
                    return new SizeF(73, 105);
                case PageSize.B0:
                    return new SizeF(2834, 4008);
                case PageSize.B1:
                    return new SizeF(2004, 2834);
                case PageSize.B2:
                    return new SizeF(1417, 2004);
                case PageSize.B3:
                    return new SizeF(1000, 1417);
                case PageSize.B4:
                    return new SizeF(708, 1000);
                case PageSize.B5:
                    return new SizeF(498, 708);
                case PageSize.B6:
                    return new SizeF(354, 498);
                case PageSize.B7:
                    return new SizeF(249, 354);
                case PageSize.B8:
                    return new SizeF(175, 249);
                case PageSize.B9:
                    return new SizeF(124, 175);
                case PageSize.B10:
                    return new SizeF(87, 124);
                case PageSize.ARCH_E:
                    return new SizeF(2592, 3456);
                case PageSize.ARCH_D:
                    return new SizeF(1728, 2592);
                case PageSize.ARCH_C:
                    return new SizeF(1296, 1728);
                case PageSize.ARCH_B:
                    return new SizeF(864, 1296);
                case PageSize.ARCH_A:
                    return new SizeF(648, 864);
                case PageSize.FLSA:
                    return new SizeF(612, 936);
                case PageSize.FLSE:
                    return new SizeF(648, 936);
                case PageSize.HALFLETTER:
                    return new SizeF(396, 612);
                case PageSize._11X17:
                    return new SizeF(792, 1224);
                case PageSize.ID_1:
                    return new SizeF(242.65f, 153);
                case PageSize.ID_2:
                    return new SizeF(297, 210);
                case PageSize.ID_3:
                    return new SizeF(354, 249);
                case PageSize.LEDGER:
                    return new SizeF(1224, 792);
                case PageSize.CROWN_QUARTO:
                    return new SizeF(535, 697);
                case PageSize.LARGE_CROWN_QUARTO:
                    return new SizeF(569, 731);
                case PageSize.DEMY_QUARTO:
                    return new SizeF(620, 782);
                case PageSize.ROYAL_QUARTO:
                    return new SizeF(671, 884);
                case PageSize.CROWN_OCTAVO:
                    return new SizeF(348, 527);
                case PageSize.LARGE_CROWN_OCTAVO:
                    return new SizeF(365, 561);
                case PageSize.DEMY_OCTAVO:
                    return new SizeF(391, 612);
                case PageSize.ROYAL_OCTAVO:
                    return new SizeF(442, 663);
                case PageSize.SMALL_PAPERBACK:
                    return new SizeF(314, 504);
                case PageSize.PENGUIN_SMALL_PAPERBACK:
                    return new SizeF(314, 513);
                case PageSize.PENGUIN_LARGE_PAPERBACK:
                    return new SizeF(365, 561);
                default:
                    return SizeF.Empty;
            }
        }
    }
}
