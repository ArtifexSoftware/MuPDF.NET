/**************************************************
 *
 Copyright (c) 2008 - 2012 Bytescout
 *
 *
**************************************************/

using System;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using GS1 DataBar Limited symbology rules.
    /// This symbology used within the GS1 System for encode a GTIN.
    /// </summary>
    class GS1DataBarLimitedSymbology : SymbologyDrawing
    {
        private const int m_combinationsNumber = 2013571;
        private const int m_elements = 7; // number of pairs of elements in a set

        // Charactestics of the signs with the structure (26,7)
        private static int[,] m_signProperties = new int[,]
        {
         // | value    |  Gsum  | Nodd | Neven | Widest | Widest | Todd | Teven |
         // | range to |        |      |       |  odd   |  even  |      |       |
            {  183063,        0,   17,     9,      6,       3,     6538,    28},
            {  820063,   183064,   13,    13,      5,       4,      875,   728},
            { 1000775,   820064,    9,    17,      3,       6,       28,  6454},
            { 1491020,  1000776,   15,    11,      5,       4,     2415,   203},
            { 1979844,  1491021,   11,    15,      4,       5,      203,  2408},
            { 1996938,  1979845,   19,     7,      8,       1,    17094,     1},
            { 2013570,  1996939,    7,    19,      1,       8,        1, 16632},
        };

        // Table with weighted coefficients for calculating the check sum (Math.Pow(3,x)%89)
        private static int[,] m_weightCoefficient = new int[,]
        {
            { 1,  3,  9, 27, 81, 65, 17, 51, 64, 14, 42, 37, 22, 66},
            {20, 60,  2,  6, 18, 54, 73, 41, 34, 13, 39, 28, 84, 74}
        };

        // Table with the order numbers for calculating the width of the bars and spaces of the check sign
        private static int[] m_checkSignSeries = new int[]
            {0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,
             25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,45,52,57,
             63,64,65,66,73,74,75,76,77,78,79,82,126,127,128,129,130,132,141,
             142,143,144,145,146,210,211,212,213,214,215,216,217,220,316,317,
             318,319,320,322,323,326,337};

        private static int[] m_leftGuardPattern = new int[] { 1, 1 };
        private static int[] m_rightGuardPattern = new int[] { 1, 1, 5 };

        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarOmnidirectionalSymbology"/> class.
        /// </summary>
        public GS1DataBarLimitedSymbology()
            : base(TrueSymbologyType.GS1_DataBar_Limited)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarOmnidirectionalSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public GS1DataBarLimitedSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.GS1_DataBar_Limited)
        {
        }

        /// <summary>
        /// Validates the value using GS1 DataBar Limited symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">This parameter is not applicable to this symbology (checksum is always mandatory for GS1 barcodes).</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            bool result = GS1Utils.IsGTIN(value);
            if (result)
            {
                // aside from that the value of the indicator digit must be equal to 0 or 1
                int index = value.IndexOf("(01)") == 0 ? 4 : 0;
                result = value[index] == '0' || value[index] == '1';
            }
            return result;
        }

        /// <summary>
        /// Gets or sets the barcode value to encode.
        /// </summary>
        /// <value>The barcode value to encode.</value>
        public override string Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                if (ValueIsValid(value, false))
                {
                    int index = value.IndexOf("(01)");
                    if (index == 0)
                        base.Value = value.Substring(4);
                    else
                        base.Value = value;
                }
                else
                {
                    string generic = "Provided value can't be encoded by current symbology.\n";
                    throw new BarcodeException(generic + getValueRestrictions());
                }
            }
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            // GS1 Databar Limited allows encoding of up to 14 digits of data. Last digit must be checksum and will be verified.
            return "GS1 DataBar Limited symbology allows encoding of up to 14 digits of data. Last digit must be checksum and will be verified.";
        }

        /// <summary>
        /// Gets the barcode value encoded using GS1 DataBar Limited symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            return GS1Utils.GetEncodedGTINValue(Value, forCaption);
        }

        /// <summary>
        /// Gets the encoding pattern for given character.
        /// </summary>
        /// <param name="c">The character to retrieve pattern for.</param>
        /// <returns>
        /// The encoding pattern for given character.
        /// </returns>
        protected override string getCharPattern(char c)
        {
            return null;
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "00000000000000";
        }

        /// <summary>
        /// Gets or sets the height of the barcode bars in pixels.
        /// </summary>
        /// <value>The height of the barcode bars in pixels.</value>
        public override int BarHeight
        {
            get
            {
                return base.BarHeight;
            }
            set
            {
                if (value < 10 * NarrowBarWidth)
                    base.BarHeight = 10 * NarrowBarWidth;
                else
                    base.BarHeight = value;
            }
        }

        /// <summary>
        /// Gets or sets the width of the narrow bar in pixels.
        /// </summary>
        /// <value>The width of the narrow bar in pixels.</value>
        public override int NarrowBarWidth
        {
            get
            {
                return base.NarrowBarWidth;
            }
            set
            {
                // preserving proportions between BarHeight and NarrowBarWidth
                double ratio = BarHeight / base.NarrowBarWidth;
                base.NarrowBarWidth = value;
                BarHeight = (int)Math.Round(ratio * base.NarrowBarWidth);
            }
        }

        static protected int[] signValue(int value)
        {
            for (int i = 0; i < m_signProperties.GetLength(0); i++)
            {
                if (value <= m_signProperties[i, 0])
                {
                    int Gsum = m_signProperties[i, 1];
                    int Teven = m_signProperties[i, 7];
                    int Vodd = (value - Gsum) / Teven;
                    int Veven = (value - Gsum) % Teven;
                    int[] odd = GS1Utils.getRSSwidths(Vodd, m_signProperties[i, 2], m_elements, m_signProperties[i, 4], true);
                    int[] even = GS1Utils.getRSSwidths(Veven, m_signProperties[i, 3], m_elements, m_signProperties[i, 5], false); // false or true ???
                    if (odd.Length != m_elements || even.Length != m_elements)
                        throw new BarcodeException("Incorrect outer sign for GS1 DataBar Limited symbology");
                    int[] result = new int[odd.Length + even.Length];
                    for (int j = 0; j < m_elements; j++)
                    {
                        result[j * 2] = odd[j];
                        result[j * 2 + 1] = even[j];
                    }
                    return result;
                }
            }
            throw new BarcodeException("Incorrect outer sign for GS1 DataBar Limited symbology");
        }

        static protected int[] checkSignValue(int value)
        {
            // see addon C in ISO/IEC 24724-2011 standard
            const int combinationsNumber = 21;
            int serialNumber = m_checkSignSeries[value];
            int space = (int)(serialNumber / combinationsNumber);
            int bar = (int)(serialNumber % combinationsNumber);

            int[] odd = GS1Utils.getRSSwidths(space, 8, 6, 3, true);
            int[] even = GS1Utils.getRSSwidths(bar, 8, 6, 3, true);
            int[] result = new int[odd.Length + even.Length + 2];
            for (int j = 0; j < odd.Length; j++)
            {
                result[j * 2] = odd[j];
                result[j * 2 + 1] = even[j];
            }
            result[result.Length - 2] = 1;
            result[result.Length - 1] = 1;
            return result;
        }

        /// <summary>
        /// Сhecksums.
        /// </summary>
        /// <param name="n">Number of sign of symbol.</param>
        /// <param name="value">Widths of sign elements.</param>
        /// <returns></returns>
        static protected int checkSum(int n, int[] value)
        {
            if (value.Length > 14)
                throw new BarcodeException("Incorrect sign for GS1 DataBar Limited symbology");
            if (n > 2)
                throw new BarcodeException("GS1 DataBar Limited symbology error");
            int sum = 0;
            for (int i = 0; i < value.Length; i++)
                sum += value[i] * m_weightCoefficient[n, i];
            return sum;
        }


        protected override Size buildBars(SKCanvas canvas, SKFont font)
        {
            string sValue = GetEncodedValue(false);
            long value = long.Parse(sValue);
            int left = (int)(value / m_combinationsNumber);
            int right = (int)(value % m_combinationsNumber);
            int[] leftSign = signValue(left);
            int[] rightSign = signValue(right);

            int сhecksum = checkSum(0, leftSign);
            сhecksum += checkSum(1, rightSign);
            сhecksum = сhecksum % 89;
            int[] checkSign = checkSignValue(сhecksum);

            intList symbol = new intList();
            GS1Utils.addArray(symbol, m_leftGuardPattern, false);
            GS1Utils.addArray(symbol, leftSign, false);
            GS1Utils.addArray(symbol, checkSign, false);
            GS1Utils.addArray(symbol, rightSign, false);
            GS1Utils.addArray(symbol, m_rightGuardPattern, false);
            if (symbol.Count != 47)
                throw new BarcodeException("GS1 DataBar Limited symbology error");

            Size drawingSize = new Size();
            int x = 0;
            int y = 0;

            int height = BarHeight;
            int width = NarrowBarWidth;

            Size captionSize = calculateCaptionSize(canvas, font);
            int guardHeight = height + captionSize.Height / 2;

            for (int i = 0; i < symbol.Count; i++)
            {
                if (i % 2 != 0)
                    m_rects.Add(new Rectangle(x, y, width * symbol[i], guardHeight));
                x += width * symbol[i];
            }

            drawingSize.Width = x;
            drawingSize.Height = BarHeight + captionSize.Height / 2;
            return drawingSize;
        }
    }
}
