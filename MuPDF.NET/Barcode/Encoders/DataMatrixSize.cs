/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes all possible matrix sizes for DataMatrix symbology.
    /// </summary>
    public enum DataMatrixSize
    {
        /// <summary>
        /// (0) Library will choose smallest possible square matrix size.
        /// </summary>
        AutoSquareSize = 0, 

        /// <summary>
        /// (1) Library will choose smallest possible matrix size.
        /// </summary>
        AutoSize,

        /// <summary>
        /// (2) 8 rows x 18 columns. 1 data region.
        /// </summary>
        Size8x18,

        /// <summary>
        /// (3) 8 rows x 32 columns. 2 data regions.
        /// </summary>
        Size8x32,

        /// <summary>
        /// (4) 10 rows x 10 columns. 1 data region.
        /// </summary>
        Size10x10,

        /// <summary>
        /// (5) 12 rows x 12 columns. 1 data region.
        /// </summary>
        Size12x12,

        /// <summary>
        /// (6) 12 rows x 26 columns. 1 data region.
        /// </summary>
        Size12x26,

        /// <summary>
        /// (7) 12 rows x 36 columns. 2 vertical data regions.
        /// </summary>
        Size12x36,

        /// <summary>
        /// (8) 14 rows x 14 columns. 1 data region.
        /// </summary>
        Size14x14,

        /// <summary>
        /// (9) 16 rows x 16 columns. 1 data region.
        /// </summary>
        Size16x16,

        /// <summary>
        /// (10) 16 rows x 36 columns. 2 vertical data regions.
        /// </summary>
        Size16x36,

        /// <summary>
        /// (11) 16 rows x 48 columns. 2 vertical data regions.
        /// </summary>
        Size16x48,

        /// <summary>
        /// (12) 18 rows x 18 columns. 1 data region.
        /// </summary>
        Size18x18,

        /// <summary>
        /// (13) 20 rows x 20 columns. 1 data region.
        /// </summary>
        Size20x20,

        /// <summary>
        /// (14) 22 rows x 22 columns. 1 data region.
        /// </summary>
        Size22x22,

        /// <summary>
        /// (15) 24 rows x 24 columns. 1 data region.
        /// </summary>
        Size24x24,

        /// <summary>
        /// (16) 26 rows x 26 columns. 1 data region.
        /// </summary>
        Size26x26,

        /// <summary>
        /// (17) 32 rows x 32 columns. 2 x 2 data regions.
        /// </summary>
        Size32x32,

        /// <summary>
        /// (18) 36 rows x 36 columns. 2 x 2 data regions.
        /// </summary>
        Size36x36,

        /// <summary>
        /// (19) 40 rows x 40 columns. 2 x 2 data regions.
        /// </summary>
        Size40x40,

        /// <summary>
        /// (20) 44 rows x 44 columns. 2 x 2 data regions.
        /// </summary>
        Size44x44,

        /// <summary>
        /// (21) 48 rows x 48 columns. 2 x 2 data regions.
        /// </summary>
        Size48x48,

        /// <summary>
        /// (22) 52 rows x 52 columns. 2 x 2 data regions.
        /// </summary>
        Size52x52,

        /// <summary>
        /// (23) 64 rows x 64 columns. 4 x 4 data regions.
        /// </summary>
        Size64x64,

        /// <summary>
        /// (24) 72 rows x 72 columns. 4 x 4 data regions.
        /// </summary>
        Size72x72,

        /// <summary>
        /// (25) 80 rows x 80 columns. 4 x 4 data regions.
        /// </summary>
        Size80x80,

        /// <summary>
        /// (26) 88 rows x 88 columns. 4 x 4 data regions.
        /// </summary>
        Size88x88,

        /// <summary>
        /// (27) 96 rows x 96 columns. 4 x 4 data regions.
        /// </summary>
        Size96x96,

        /// <summary>
        /// (28) 104 rows x 104 columns. 4 x 4 data regions.
        /// </summary>
        Size104x104,

        /// <summary>
        /// (29) 120 rows x 120 columns. 6 x 6 data regions.
        /// </summary>
        Size120x120,

        /// <summary>
        /// (30) 132 rows x 132 columns. 6 x 6 data regions.
        /// </summary>
        Size132x132,

        /// <summary>
        /// (31) 144 rows x 144 columns. 6 x 6 data regions.
        /// </summary>
        Size144x144,
    }
}
