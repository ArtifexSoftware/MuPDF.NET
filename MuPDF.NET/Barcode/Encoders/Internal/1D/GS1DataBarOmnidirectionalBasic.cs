using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    class GS1DataBarOmnidirectionalBasic : SymbologyDrawing
    {
        private const int m_CombinationsNumber = 4537077;
        private const int m_Vinside = 1597;
        private const int m_elements = 4; // number of pairs of elements in a set

        // Characteristics of outer signs with structure (16,4)
        private static int[,] m_outer = new int[,]
        {
         // | value    | Gsum | Nodd | Neven | Widest | Widest | Todd | Teven |
         // | range to |      |      |       |  odd   |  even  |      |       |
            {    160,       0,   12,     4,      8,       1,     161,      1},
            {    960,     161,   10,     6,      6,       3,      80,     10},
            {   2014,     961,    8,     8,      4,       5,      31,     34},
            {   2714,    2015,    6,    10,      3,       6,      10,     70},
            {   2840,    2715,    4,    12,      1,       8,       1,    126}
        };

        // Characteristics of internal signs with structure (15,4)
        private static int[,] m_inner = new int[,]
        {
         // | value    | Gsum | Nodd | Neven | Widest | Widest | Todd | Teven |
         // | range to |      |      |       |  odd   |  even  |      |       |
            {    335,       0,    5,     10,     2,       7,       4,     84},
            {   1035,     336,    7,      8,     4,       5,      20,     35},
            {   1515,    1036,    9,      6,     6,       3,      48,     10},
            {   1596,    1516,   11,      4,     8,       1,      81,      1}
        };

        // Table with weighted coefficients for calculating checksum (Math.Pow(3,x)%79)
        private static int[,] m_weightCoefficient = new int[,]
        {
            { 1,  3,  9, 27,  2,  6, 18, 54},
            { 4, 12, 36, 29,  8, 24, 72, 58},
            {16, 48, 65, 37, 32, 17, 51, 74},
            {64, 34, 23, 69, 49, 68, 46, 59}
        };

        // Table with width of finder pattern elements
        private static int[][] m_finderPaternValues = new int[][]
        {
            new int[] {3, 8, 2, 1, 1}, // 0
            new int[] {3, 5, 5, 1, 1}, // 1
            new int[] {3, 3, 7, 1, 1}, // 2
            new int[] {3, 1, 9, 1, 1}, // 3
            new int[] {2, 7, 4, 1, 1}, // 4
            new int[] {2, 5, 6, 1, 1}, // 5
            new int[] {2, 3, 8, 1, 1}, // 6
            new int[] {1, 5, 7, 1, 1}, // 7
            new int[] {1, 3, 9, 1, 1}, // 8
        };

        protected static int[] m_guardPattern = new int[] { 1, 1 };
        protected static int[] m_sign1;
        protected static int[] m_sign2;
        protected static int[] m_sign3;
        protected static int[] m_sign4;
        protected static int[] m_leftPatternSign;
        protected static int[] m_rightPatternSign;

        public GS1DataBarOmnidirectionalBasic(TrueSymbologyType type)
            : base(type)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbologyDrawing"/> class.
        /// </summary>
        /// <param name="prototype">The existing SymbologyDrawing object to use as parameter prototype.</param>
        /// <param name="type">The new symbology drawing type.</param>
        public GS1DataBarOmnidirectionalBasic(SymbologyDrawing prototype, TrueSymbologyType type)
            : base(prototype, type)
        {
        }

        /// <summary>
        /// Validates the value using GS1 DataBar Omnidirectional symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">This parameter is not applicable to this symbology (checksum is always mandatory for GS1 barcodes).</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            return GS1Utils.IsGTIN(value);
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
            // GS1 DataBar Omnidirectional symbology allows encoding of up to 14 digits of data. Last digit must be checksum and will be verified.
            return "GS1 DataBar Omnidirectional symbology allows encoding of up to 14 digits of data. Last digit must be checksum and will be verified.";
        }

        /// <summary>
        /// Gets the barcode value encoded using GS1 DataBar Omnidirectional symbology rules.
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
        /// Gets the checksum char.
        /// </summary>
        /// <param name="value">The value to calculate checksum for.</param>
        /// <returns>The checksum char.</returns>
        protected virtual char getChecksum(string value)
        {
            return GS1Utils.getGTINChecksum(value);
        }

        static protected int[] outerSign(int value)
        {
            for (int i = 0; i < m_outer.GetLength(0); i++)
            {
                if (value <= m_outer[i, 0])
                {
                    int Gsum = m_outer[i, 1];
                    int Teven = m_outer[i, 7];
                    int Vodd = (value - Gsum) / Teven;
                    int Veven = (value - Gsum) % Teven;
                    int[] odd = GS1Utils.getRSSwidths(Vodd, m_outer[i, 2], m_elements, m_outer[i, 4], true);
                    int[] even = GS1Utils.getRSSwidths(Veven, m_outer[i, 3], m_elements, m_outer[i, 5], false);
                    if (odd.Length != m_elements || even.Length != m_elements)
                        throw new BarcodeException("Incorrect outer sign for GS1 DataBar Omnidirectional symbology");
                    int[] result = new int[odd.Length + even.Length];
                    for (int j = 0; j < m_elements; j++)
                    {
                        result[j * 2] = odd[j];
                        result[j * 2 + 1] = even[j];
                    }
                    return result;
                }
            }
            throw new BarcodeException("Incorrect outer sign for GS1 DataBar Omnidirectional symbology");
        }

        static protected int[] innerSign(int value)
        {
            for (int i = 0; i < m_inner.GetLength(0); i++)
            {
                if (value <= m_inner[i, 0])
                {
                    int Gsum = m_inner[i, 1];
                    int Todd = m_inner[i, 6];
                    int Veven = (value - Gsum) / Todd;
                    int Vodd = (value - Gsum) % Todd;
                    int[] odd = GS1Utils.getRSSwidths(Vodd, m_inner[i, 2], 4, m_inner[i, 4], false);
                    int[] even = GS1Utils.getRSSwidths(Veven, m_inner[i, 3], 4, m_inner[i, 5], true);
                    if (odd.Length != 4 || even.Length != 4)
                        throw new BarcodeException("Incorrect inner sign for GS1 DataBar Omnidirectional symbology");
                    int[] result = new int[odd.Length + even.Length];
                    for (int j = 0; j < 4; j++)
                    {
                        result[j * 2] = odd[j];
                        result[j * 2 + 1] = even[j];
                    }
                    return result;
                }
            }
            throw new BarcodeException("Incorrect inner sign for GS1 DataBar Omnidirectional symbology");
        }
        
        /// <summary>
        /// Сhecksums.
        /// </summary>
        /// <param name="n">Number of sign symbol.</param>
        /// <param name="value">Widths of sign elements.</param>
        /// <returns></returns>
        static protected int checkSum(int n, int[] value)
        {
            if (value.Length > 8)
                throw new BarcodeException("Incorrect sign for GS1 DataBar Omnidirectional symbology");
            if (n > 3)
                throw new BarcodeException("GS1 DataBar Omnidirectional symbology error");
            int sum = 0;
            for (int i = 0; i < value.Length; i++)
                sum += value[i] * m_weightCoefficient[n, i];
            return sum;
        }

        static protected void createSegments(string sValue)
        {
            long value = long.Parse(sValue);
            int left = (int)(value / m_CombinationsNumber);
            int right = (int)(value % m_CombinationsNumber);
            int data1 = left / m_Vinside;
            int data2 = left % m_Vinside;
            int data3 = right / m_Vinside;
            int data4 = right % m_Vinside;

            m_sign1 = outerSign(data1);
            m_sign2 = innerSign(data2);
            m_sign3 = outerSign(data3);
            m_sign4 = innerSign(data4);

            int сhecksum = checkSum(0, m_sign1);
            сhecksum += checkSum(1, m_sign2);
            сhecksum += checkSum(2, m_sign3);
            сhecksum += checkSum(3, m_sign4);
            сhecksum = сhecksum % 79;
            if (сhecksum >= 8)
                сhecksum += 1;
            if (сhecksum >= 72)
                сhecksum += 1;

            int Cleft = сhecksum / 9;
            int Cright = сhecksum % 9;
            m_leftPatternSign = m_finderPaternValues[Cleft];
            m_rightPatternSign = m_finderPaternValues[Cright];

        }
    }
}
