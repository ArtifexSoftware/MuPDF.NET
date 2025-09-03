/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2012 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using GS1 DataBar Expanded symbology rules.
    /// </summary>
    class GS1DataBarExpandedSymbology : SymbologyDrawing
    {
        private const int m_elements = 4; // number of pairs of elements in a set

        // Characteristics of signs with structure (17,4)
        private static int[,] m_signProperties = new int[,]
        {
         // | value    | Gsum | Nodd | Neven | Widest | Widest | Todd | Teven |
         // | range to |      |      |       |  odd   |  even  |      |       |
            {  347,         0,   12,     5,      7,       2,      87,     4},
            { 1387,       348,   10,     7,      5,       4,      52,    20},
            { 2947,      1388,    8,     9,      4,       5,      30,    52},
            { 3987,      2948,    6,    11,      3,       6,      10,   104},
            { 4191,      3988,    4,    13,      1,       8,       1,   204},
        };

        // Table of weighted coefficients for calculating the check sum (Math.Pow(3,x)%211)
        private static int[][] m_weightCoefficient = new int[][]
        {
            new int[] {  1,   3,   9,  27,  81,  32,  96,  77}, // right of A1
            new int[] { 20,  60, 180, 118, 143,   7,  21,  63}, // left of A2
            new int[] {189, 145,  13,  39, 117, 140, 209, 205}, // right of A2
            new int[] {193, 157,  49, 147,  19,  57, 171,  91}, // left of B1
            new int[] { 62, 186, 136, 197, 169,  85,  44, 132}, // right of B1
            new int[] {185, 133, 188, 142,   4,  12,  36, 108}, // left of B2
            new int[] {113, 128, 173,  97,  80,  29,  87,  50}, // right of B2
            new int[] {150,  28,  84,  41, 123, 158,  52, 156}, // ...
            new int[] { 46, 138, 203, 187, 139, 206, 196, 166},
            new int[] { 76,  17,  51, 153,  37, 111, 122, 155},
            new int[] { 43, 129, 176, 106, 107, 110, 119, 146},
            new int[] { 16,  48, 144,  10,  30,  90,  59, 177},
            new int[] {109, 116, 137, 200, 178, 112, 125, 164},
            new int[] { 70, 210, 208, 202, 184, 130, 179, 115},
            new int[] {134, 191, 151,  31,  93,  68, 204, 190},
            new int[] {148,  22,  66, 198, 172,  94,  71,   2},
            new int[] {  6,  18,  54, 162,  64, 192, 154,  40},
            new int[] {120, 149,  25,  75,  14,  42, 126, 167},
            new int[] { 79,  26,  78,  23,  69, 207, 199, 175},
            new int[] {103,  98,  83,  38, 114, 131, 182, 124},
            new int[] {161,  61, 183, 127, 170,  88,  53, 159},
            new int[] { 55, 165,  73,   8,  24,  72,   5,  15},
            new int[] { 45, 135, 194, 160,  58, 174, 100,  89},
        };

        // Table with widths of finder pattern elements
        private static int[][] m_finderPaternValues = new int[][]
        {
            new int[] {1, 8, 4, 1, 1}, // A (А1=A, A2 - mirrored represenation with inverted colors)
            new int[] {3, 6, 4, 1, 1}, // B
            new int[] {3, 4, 6, 1, 1}, // C
            new int[] {3, 2, 8, 1, 1}, // D
            new int[] {2, 6, 5, 1, 1}, // E
            new int[] {2, 2, 9, 1, 1}, // F
        };

        // the order of finding templates
        private static int[][] m_finderPaternOrder = new int[][]
        {
         //         |   number   |  order of finding template   |
         //         | segments |    1=A1, -1=A2, 2=B1, -2=B2 и т.д.   |
            new int[] { 4,        1, -1}, 
            new int[] { 6,        1, -2, 2},
            new int[] { 8,        1, -3, 2, -4},
            new int[] {10,        1, -5, 2, -4, 3},
            new int[] {12,        1, -5, 2, -4, 4, -6},
            new int[] {14,        1, -5, 2, -4, 5, -6, 6},
            new int[] {16,        1, -1, 2, -2, 3, -3, 4, -4},
            new int[] {18,        1, -1, 2, -2, 3, -3, 4, -5, 5},
            new int[] {20,        1, -1, 2, -2, 3, -3, 4, -5, 6, -6},
            new int[] {22,        1, -1, 2, -2, 3, -3, 4, -5, 5, -6, 6},
        };

        private static int[] m_guardPattern = new int[] { 1, 1 };

        // Symbol FNC1 is corresponding to ASCII character 29 (<GS>)
        // Character FNC1 translates into barcode as ]C1
        private static char FNC1 = '\u001D';

        private static string m_alphanumericChars = GS1Utils.Numbers + GS1Utils.CapitalLetter + "*,-./";// + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

        // List of codes AI with variable length for substitution in regular expression
        private static string[] m_variableLengthAI = new string[]
            {
                "10","21","22","23[0-9]","240","241","242","250","251","253","254","30","37",
                "390[0-9]","391[0-9]","392[0-9]","393[0-9]","400","401","403","420","421","423",
                "7002","7004","703[0-9]","8002","8003","8004","8007","8008","8020","8110","9[0-9]"
            };

        private static Coding m_coding = Coding.AlphaNumeric;
        private static int m_fixnum = 0;

        public GS1DataBarExpandedSymbology(TrueSymbologyType type)
            : base(type)
        {
        }

        public GS1DataBarExpandedSymbology(SymbologyDrawing prototype, TrueSymbologyType type)
            : base(prototype, type)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarOmnidirectionalSymbology"/> class.
        /// </summary>
        public GS1DataBarExpandedSymbology()
            : base(TrueSymbologyType.GS1_DataBar_Expanded)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataBarOmnidirectionalSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public GS1DataBarExpandedSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.GS1_DataBar_Expanded)
        {
        }

        protected static int[][] FinderPaternOrder
        {
            get { return m_finderPaternOrder; }
        }

        protected static int[][] FinderPaternValues
        {
            get { return m_finderPaternValues; }
        }

        protected static int[] GuardPattern
        {
            get { return m_guardPattern; }
        }

        /// <summary>
        /// Validates the value using GS1 DataBar Expanded symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">This parameter is not applicable to this symbology (checksum is always mandatory for GS1 barcodes).</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            // Maybe we need to analyze all AI?
            int index = value.IndexOf("(01)");
            if (index > -1)
            {
                string gtin = value.Substring(index + 4, 14);
                return GS1Utils.IsGTIN(gtin);
            }
            else
                return true;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "GS1 DataBar Expanded symbology allows encoding up to 74 numeric or 41 alphabetic characters of AI Element String data.";
        }

        /// <summary>
        /// Gets the barcode value encoded using GS1 DataBar Expanded symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            if (forCaption)
                return Value;
            else
            {
                string result = Value;
                string replacement = "$&" + FNC1;
                for (int i = 0; i < m_variableLengthAI.Length; i++)
                {
                    string pattern = "\\(" + m_variableLengthAI[i] + "\\)[a-zA-Z0-9]+";
                    if (Regex.IsMatch(result, pattern + "\\("))
                        result = Regex.Replace(result, pattern, replacement);
                }
                return result;
            }
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
                if (value < 34 * NarrowBarWidth)
                    base.BarHeight = 34 * NarrowBarWidth;
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
                // saving proportions between BarHeight and NarrowBarWidth
                double ratio = BarHeight / base.NarrowBarWidth;
                base.NarrowBarWidth = value;
                BarHeight = (int)Math.Round(ratio * base.NarrowBarWidth);
            }
        }

        /// <summary>

        /// Check if the characters in the string value starting with the character index 
        /// and ending with the character index + count - 1 are digits.
        /// </summary>
        /// <param name="value">String for checking.</param>
        /// <param name="index">Index of first checked synbol.</param>
        /// <param name="count">Number of checked symbols.</param>
        /// <returns>
        /// 	<c>true</c> if synbols in the given range are digits; otherwise, <c>false</c>.
        /// </returns>
        static private bool IsDigit(string value, int index, int count)
        {
            if (index + count > value.Length)
                throw new BarcodeException("Error of coding of Databar Expanded symbology");

            for (int i = index; i < index + count; i++)
            {
                if (!char.IsDigit(value[i]) && value[i] != FNC1)
                    return false;
            }
            return true;
        }

        static private intList getAIList(string value)
        {
            intList aiList = new intList();
            bool bracket = false;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (value[i] == '(')
                {
                    bracket = true;
                }
                else if (value[i] == ')')
                {
                    if (bracket)
                    {
                        try
                        {
                            aiList.Add(int.Parse(sb.ToString()));
                        }
                        catch
                        {
                            // ignore
                        }
                        finally
                        {
                            sb = new StringBuilder();
                        }
                    }
                    bracket = false;
                }
                else if (bracket)
                {
                    sb.Append(value[i]);
                }
            }
            return aiList;
        }

        static private string getEncodingMethod(string value, intList aiList)
        {
            if (aiList.Count < 1 || aiList[0] != 01)
                return "00";
            if (aiList.Count == 2 && aiList[0] == 01 && value[4] == '9')
            {
                if (aiList[1] == 3103)
                    return "0100";
                if (aiList[1] == 3202 || aiList[1] == 3203)
                    return "0101";
                if (aiList[1] >= 3920 && aiList[1] <= 3929)
                    return "01100";
                if (aiList[1] >= 3930 && aiList[1] <= 3939)
                    return "01101";
            }
            else if (aiList.Count == 3 && aiList[0] == 01 && value[4] == '9')
            {
                if (aiList[1] >= 3100 && aiList[1] <= 3109 && aiList[2] == 11)
                    return "0111000";
                if (aiList[1] >= 3200 && aiList[1] <= 3209 && aiList[2] == 11)
                    return "0111001";
                if (aiList[1] >= 3100 && aiList[1] <= 3109 && aiList[2] == 13)
                    return "0111010";
                if (aiList[1] >= 3200 && aiList[1] <= 3209 && aiList[2] == 13)
                    return "0111011";
                if (aiList[1] >= 3100 && aiList[1] <= 3109 && aiList[2] == 15)
                    return "0111100";
                if (aiList[1] >= 3200 && aiList[1] <= 3209 && aiList[2] == 15)
                    return "0111101";
                if (aiList[1] >= 3100 && aiList[1] <= 3109 && aiList[2] == 17)
                    return "0111110";
                if (aiList[1] >= 3200 && aiList[1] <= 3209 && aiList[2] == 17)
                    return "0111111";
            }
            else if (aiList[0] == 01 && value.Length > 18 &&value[18] == '(')
                return "1";

            return "00";
        }

        /// <summary>
        /// Checks if the characters in the string 'value' starting from the character at 'index'
        /// and ending with the character at 'index + count - 1' can be encoded using the
        /// alphanumeric digital encoding scheme.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <param name="index">The index of the first character to check.</param>
        /// <param name="count">The number of characters to check.</param>
        /// <returns>
        /// 	<c>true</c> if the characters in the specified range can be encoded using the alphanumeric digital encoding scheme; otherwise, <c>false</c>.
        /// </returns>

        static private bool IsAlphaNumeric(string value, int index, int count)
        {
            if (index >= value.Length)
                throw new BarcodeException("Error of coding of Databar Expanded symbology");

            for (int i = index; (i < index + count) && (i < value.Length); i++)
            {
                if (m_alphanumericChars.IndexOf(value[i]) < 0)
                    return false;
            }
            return true;
        }

        static protected string numberCoding(char c1, char c2)
        {
            // Scheme of number coding, see page 31 of GOST ISO/IEC 24724-2011
            int d1;
            if (char.IsDigit(c1))
                d1 = (int)Char.GetNumericValue(c1); //int.Parse(c1.ToString())
            else if (c1 == FNC1)
                d1 = 10;
            else
                throw new BarcodeException("Incorrect character for number coding of Databar Expanded symbology");
            int d2;
            if (char.IsDigit(c2))
                d2 = (int)Char.GetNumericValue(c2);
            else if (c2 == FNC1)
                d2 = 10;
            else
                throw new BarcodeException("Incorrect character for number coding of Databar Expanded symbology");
            int value = 11 * d1 + d2 + 8;
            StringBuilder result = new StringBuilder(Convert.ToString(value, 2));
            while (result.Length < 7)
            {
                result.Insert(0, "0");
            }
            return result.ToString();
        }

        static protected string alphanumericCoding(char c)
        {
            // Scheme of alphanumeric coding, see page 32 of ISO/IEC 24724-2011
            string symbols = "*,-./";
            if (char.IsDigit(c))
            {
                int digit = (int)Char.GetNumericValue(c); //int.Parse(c1.ToString())
                string sDigit = Convert.ToString(digit + 5, 2);
                return sDigit.Length == 3 ? "00" + sDigit : "0" + sDigit;
            }
            else if (char.IsUpper(c))
            {
                return Convert.ToString((int)c - 33, 2);
            }
            else if (c == FNC1)
                return "01111";
            else
            {
                int index = symbols.IndexOf(c);
                if (index >= 0)
                    return Convert.ToString(index + 58, 2);
            }

            throw new BarcodeException("Incorrect character for alphanumeric coding of Databar Expanded symbology");
        }

        static protected string isoIec646Coding(char c)
        {
            // Scheme of ISO/IEC 646 coding, see page 34 of ISO/IEC 24724-2011
            string symbols = "!\"%&'()*+,-./:;<=>?_ ";
            if (char.IsDigit(c))
            {
                int digit = (int)Char.GetNumericValue(c); //int.Parse(c1.ToString())
                string sDigit = Convert.ToString(digit + 5, 2);
                return sDigit.Length == 3 ? "00" + sDigit : "0" + sDigit;
            }
            else if (char.IsUpper(c))
            {
                return Convert.ToString((int)c - 1, 2);
            }
            else if (char.IsLower(c))
            {
                return Convert.ToString((int)c - 7, 2);
            }

            else if (c == FNC1)
                return "01111";
            else
            {
                int index = symbols.IndexOf(c);
                if (index >= 0)
                    return Convert.ToString(index + 232, 2);
            }
            throw new BarcodeException("Incorrect character for alphanumeric coding of Databar Expanded symbology");
        }

        protected enum Coding
        {
            AlphaNumeric,
            IsoIec646,
            Number,
        }

        static protected Coding LastCoding
        {
            get { return m_coding; }
        }

        static protected int FixNum
        {
            get { return m_fixnum; }
        }

        /// <summary>
        /// Encoding binary value of symbol in the field of universal data compression.
        /// </summary>
        /// <param name="value">Data for encoding.</param>
        /// <param name="prefix"></param>
        /// <param name="segmentsNumber"></param>
        /// <returns></returns>
        static protected string universalDataCompressionField(string value, string prefix, int segmentsNumber)
        {

            const string number = "000";// the pointer of setting the scheme of number coding
            const string alphanumeric = "0000";// the pointer of setting the scheme of alphanumeric coding
            const string iso_iec646 = "00100"; // The pointer of setting the scheme according to ISO/IEC 646
            StringBuilder binaryString = new StringBuilder(prefix);

            // encoding data
            int i = 0;
            Coding coding = Coding.Number;
            while (i < value.Length)
            {
                switch (coding)
                {
                    case Coding.AlphaNumeric:
                        if ((value.Length - i > 5) && IsDigit(value, i, 6))
                        {
                            // adding the pointer to the number coding scheme
                            binaryString.Append(number);
                            coding = Coding.Number;
                        }
                        else if ((value.Length - i > 3) && IsDigit(value, i, value.Length - i))
                        {
                            // adding the pointer to the number coding scheme
                            binaryString.Append(number);
                            coding = Coding.Number;
                        }
                        else if (m_alphanumericChars.IndexOf(value[i]) >= 0)
                        {                           
                            // applying the alphanumeric coding scheme
                            binaryString.Append(alphanumericCoding(value[i]));
                            i++;
                        }
                        else
                        {
                            // adding pointer to the encoding scheme according to ISO/IEC 646
                            binaryString.Append(iso_iec646);
                            coding = Coding.IsoIec646;
                        }
                        break;
                    case Coding.IsoIec646:
                        if ((value.Length - i > 3) && IsDigit(value, i, 4) && IsAlphaNumeric(value, i + 4, 10))
                        {
                            // adding the pointer to the number coding scheme
                            binaryString.Append(number);
                            coding = Coding.Number;
                        }
                        else if ((value.Length - i > 4) && IsAlphaNumeric(value, i, 15))
                        {
                            // adding the pointer to the alphanumeric coding scheme
                            binaryString.Append(iso_iec646); // it is correct!!!
                            coding = Coding.AlphaNumeric;
                        }
                        else
                        {
                            // appying the encoding scheme ISO/IEC 646                            
                            binaryString.Append(isoIec646Coding(value[i]));
                            i++;
                        }
                        break;
                    case Coding.Number:
                    default:
                        if (i + 1 < value.Length)
                        {
                            if ((char.IsDigit(value[i]) && char.IsDigit(value[i + 1]))
                                || (char.IsDigit(value[i]) && value[i + 1] == FNC1)
                                || (value[i] == FNC1 && char.IsDigit(value[i + 1]))
                                )
                            {
                                // applying the number coding scheme
                                binaryString.Append(numberCoding(value[i], value[i + 1]));
                                i += 2;
                            }
                            else
                            {
                                // adding the pointer to the alphanumeric coding scheme
                                binaryString.Append(alphanumeric);
                                coding = Coding.AlphaNumeric;
                            }
                        }
                        else
                        {
                            if (char.IsDigit(value[i]))
                            {
                                int remainder = 12 - binaryString.Length % 12;
                                if (remainder >= 7 || remainder < 4)
                                {
                                    binaryString.Append(numberCoding(value[i], FNC1));
                                }
                                else if (remainder >= 4)
                                {
                                    if ((segmentsNumber < 22) && ((binaryString.Length / 12 + 2) % segmentsNumber == 1))
                                    {
                                        // Expanded Stacked Symbology && last row contain one sign (add second sign)
                                        binaryString.Append(numberCoding(value[i], FNC1));
                                    }
                                    else
                                    {
                                        int n = (int)Char.GetNumericValue(value[i]) + 1;
                                        StringBuilder s = new StringBuilder(Convert.ToString(n, 2));
                                        while (s.Length < 4)
                                        {
                                            s.Insert(0, "0");
                                        }
                                        binaryString.Append(s.ToString());
                                    }
                                }
                                i++;
                            }
                            else
                            {
                                // adding the pointer of setting alphanumeric codine scheme
                                binaryString.Append(alphanumeric);
                                coding = Coding.AlphaNumeric;
                            }
                        }
                        break;
                }
            }

            m_coding = coding;
            m_fixnum = 0;
            int lastRemainder = 12 - binaryString.Length % 12;
            if (lastRemainder != 12)
            {
                if (coding == Coding.Number)
                {
                    for (int j = 0; (j < lastRemainder) && (j < 4); j++)
                    {
                        binaryString.Append('0'); // adding the pointer of setting alphanumeric codine scheme (alphanumeric = "0000")
                        m_coding = Coding.AlphaNumeric;
                        m_fixnum = j + 1;// maaybe need to pass last j to GS1DataBarStackedExpandedSymbology
                    }
                }
                lastRemainder = 12 - binaryString.Length % 12;
                if (lastRemainder != 12)
                {
                    int length = iso_iec646.Length;
                    for (int j = 0; j < lastRemainder; j++)
                    {
                        int k = j < length ? j : j % length;
                        binaryString.Append(iso_iec646[k]);
                    }
                }
            }

            return binaryString.ToString();
        }

        static protected string dataCompressionField(string value, int[] groups, int[] bits)
        {
            int sum = 0;
            for (int i = 0; i < groups.Length; i++)
                sum += groups[i];
            if (value.Length != sum)
                throw new BarcodeException("Error of coding of Databar Expanded symbology");

            int startIndex = 0;
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < groups.Length; i++)
            {
                string s = value.Substring(startIndex, groups[i]);
                startIndex += groups[i];
                int gValue = int.Parse(s);
                StringBuilder gString = new StringBuilder(Convert.ToString(gValue, 2));
                while (gString.Length < bits[i])
                {
                    gString.Insert(0, "0");
                }
                result.Append(gString.ToString());
            }
            return result.ToString();
        }

        /// <summary>
        /// Gets the binary field of variable-length symbol.
        /// </summary>
        /// <param name="count">The count of signs.</param>
        /// <returns></returns>
        static protected string getVariableLengthSymbolField(int count)
        {
            // bits of fields of symbols of variable length
            // only for encoding methods "1","00","01100","01101"
            // the first bit = 0 if the number of signs of symbol is even and 1 if - odd;
            // second bit = 0 if the number of signs of symbol <=14 and 1 if >14;
            string a = ((count / 12) % 2 == 0 ? "1" : "0");
            string b = (count > 13 * 12 ? "1" : "0");
            return a + b;
        }

        static protected string removeBrackets(string value)
        {
            string s = value.Replace("(", "");
            return s.Replace(")", "");
        }

        protected string getBinaryValue(int segmentsNumber)
        {
            intList aiList = getAIList(Value);
            string code = getEncodingMethod(Value, aiList);
            string sValue = GetEncodedValue(false);

            StringBuilder binaryValue = new StringBuilder();
            // indexes in Substring are calculated for sValue like (01)90012345678908(3103)012233(15)991231
            switch (code)
            {
                case "00":
                    binaryValue.Append(universalDataCompressionField(removeBrackets(sValue), "0" + code + "00", segmentsNumber));
                    string s00 = getVariableLengthSymbolField(binaryValue.Length);
                    binaryValue[code.Length + 1] = s00[0];
                    binaryValue[code.Length + 2] = s00[1];
                    break;
                case "1":
                    string compressionField = dataCompressionField(sValue.Substring(4, 13),
                                                            new int[] { 1, 3, 3, 3, 3 },
                                                            new int[] { 4, 10, 10, 10, 10 });
                    string s = sValue.Substring(18);
                    binaryValue.Append(universalDataCompressionField(removeBrackets(s), "0" + code + "00" + compressionField, segmentsNumber));
                    string s1 = getVariableLengthSymbolField(binaryValue.Length);
                    binaryValue[code.Length + 1] = s1[0];
                    binaryValue[code.Length + 2] = s1[1];
                    break;
                case "0100":
                    binaryValue.Append(dataCompressionField(sValue.Substring(5, 12) + sValue.Substring(24),
                                                            new int[] { 3, 3, 3, 3, 6 },
                                                            new int[] { 10, 10, 10, 10, 15 }));
                    binaryValue.Insert(0, code); // value of encoding method 
                    binaryValue.Insert(0, "0"); // missing two-dimensional component
                    break;
                case "0101":
                    int mass = int.Parse(sValue.Substring(26));
                    if (aiList[1] == 3203)
                        mass += 10000;
                    binaryValue.Append(dataCompressionField(sValue.Substring(5, 12) + mass.ToString("000000"),
                                                            new int[] { 3, 3, 3, 3, 6 },
                                                            new int[] { 10, 10, 10, 10, 15 }));
                    binaryValue.Insert(0, code); // value of encoding method
                    binaryValue.Insert(0, "0");  // missing two-dimensional component
                    break;
                case "01100":
                    int digitCount0 = aiList[1] % 10;
                    if (digitCount0 > 3)
                        throw new BarcodeException("Error of coding of Databar Expanded symbology");
                    string st = dataCompressionField(sValue.Substring(5, 12) + digitCount0.ToString(),
                                                            new int[] { 3, 3, 3, 3, 1 },
                                                            new int[] { 10, 10, 10, 10, 2 });
                    binaryValue.Append(universalDataCompressionField(sValue.Substring(24), "0" + code + "00" + st, segmentsNumber));
                    string s01100 = getVariableLengthSymbolField(binaryValue.Length);
                    binaryValue[code.Length + 1] = s01100[0];
                    binaryValue[code.Length + 2] = s01100[1];
                    break;
                case "01101":
                    int digitCount1 = aiList[1] % 10;
                    if (digitCount1 > 3)
                        throw new BarcodeException("Error of coding of Databar Expanded symbology");
                    string ss = dataCompressionField(sValue.Substring(5, 12) + digitCount1.ToString() + sValue.Substring(24, 3),
                                                            new int[] { 3, 3, 3, 3, 1, 3 },
                                                            new int[] { 10, 10, 10, 10, 2, 10 });
                    binaryValue.Append(universalDataCompressionField(sValue.Substring(27), "0" + code + "00" + ss, segmentsNumber));
                    string s01101 = getVariableLengthSymbolField(binaryValue.Length);
                    binaryValue[code.Length + 1] = s01101[0];
                    binaryValue[code.Length + 2] = s01101[1];
                    break;
                case "0111000":
                case "0111001":
                case "0111010":
                case "0111011":
                case "0111100":
                case "0111101":
                case "0111110":
                case "0111111":
                    int digitCount2 = aiList[1] % 10;

                    binaryValue.Append(dataCompressionField(sValue.Substring(5, 12) + digitCount2.ToString() + sValue.Substring(25, 5),
                                                            new int[] { 3, 3, 3, 3, 6 },
                                                            new int[] { 10, 10, 10, 10, 20 }));
                    int YY = int.Parse(sValue.Substring(34, 2));
                    int MM = int.Parse(sValue.Substring(36, 2));
                    int DD = int.Parse(sValue.Substring(38, 2));
                    int date = YY * 384 + (MM - 1) * 32 + DD;
                    string sDate = date.ToString();
                    binaryValue.Append(dataCompressionField(sDate,
                                                            new int[] { sDate.Length },
                                                            new int[] { 16 }));
                    binaryValue.Insert(0, code); // value of encoding method
                    binaryValue.Insert(0, "0"); // missing the two-dimensional component
                    break;
                default:
                    throw new BarcodeException("Error of coding of Databar Expanded symbology");
            }
            if (segmentsNumber < 22)
            {
                // Expanded Stacked Symbology
                if ((binaryValue.Length / 12 + 1) % segmentsNumber == 1)
                {
                    // the last row must contain at least two symbol signs
                    if ((LastCoding == Coding.Number) || (FixNum > 0 && FixNum < 4))
                        switch (FixNum)
                        {
                            case 1:
                                binaryValue.Append("000001000010"); // dataSigns.Add(66); //000001000010 == 33
                                break;
                            case 2:
                                binaryValue.Append("000010000100"); // dataSigns.Add(132); //000010000100 == 132
                                break;
                            case 3:
                                binaryValue.Append("000100001000"); // dataSigns.Add(264); //000100001000 == 264
                                break;
                            default:
                                binaryValue.Append("000000100001"); // dataSigns.Add(33); //000000100001 == 33
                                break;
                        }
                    else
                        binaryValue.Append("001000010000"); // dataSigns.Add(528); // 001000010000 == 528
                    string sLength = getVariableLengthSymbolField(binaryValue.Length);
                    binaryValue[code.Length + 1] = sLength[0];
                    binaryValue[code.Length + 2] = sLength[1];
                }
            }
            return binaryValue.ToString();
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
                    int[] odd = GS1Utils.getRSSwidths(Vodd, m_signProperties[i, 2], m_elements, m_signProperties[i, 4], false);
                    int[] even = GS1Utils.getRSSwidths(Veven, m_signProperties[i, 3], m_elements, m_signProperties[i, 5], true);
                    if (odd.Length != m_elements || even.Length != m_elements)
                        throw new BarcodeException("Incorrect outer sign for GS1 DataBar Expanded symbology");
                    int[] result = new int[odd.Length + even.Length];
                    for (int j = 0; j < m_elements; j++)
                    {
                        result[j * 2] = odd[j];
                        result[j * 2 + 1] = even[j];
                    }
                    return result;
                }
            }
            throw new BarcodeException("Incorrect outer sign for GS1 DataBar Expanded symbology");
        }

        static protected int signCheckSum(int[] sign, int[] weights)
        {
            if (sign.Length != weights.Length)
                throw new BarcodeException("Incorrect sign for GS1 DataBar Expanded symbology");
            int sum = 0;
            for (int i = 0; i < sign.Length; i++)
            {
                sum += sign[i] * weights[i];
            }
            return sum;
        }

        /// <summary>
        /// Сhecksums.
        /// </summary>
        /// <param name="value">Width of elements of sign.</param>
        /// <returns></returns>
        static protected int checkSum(int[][] value)
        {
            if (value.Length > 21 || value.Length < 3)
                throw new BarcodeException("Incorrect sign for GS1 DataBar Expanded symbology");
            // find index for table of finder patterns sequences
            int index = value.Length % 2 == 0 ? (value.Length - 2) / 2 : (value.Length - 3) / 2;
            int sum = signCheckSum(value[0], m_weightCoefficient[0]); // the same for all variations
            for (int i = 1; i < value.Length; i++)
            {
                if (value[i].Length != m_elements * 2)
                    throw new BarcodeException("Incorrect sign for GS1 DataBar Expanded symbology");
                if (i % 2 != 0)
                {
                    // symbol is located to the left of the finder pattern
                    int k = m_finderPaternOrder[index][(i + 1) / 2 + 1]; // code of finder pattern
                    int l = k > 0 ? 4 * k - 5 : -4 * k - 3; // index for weighted coefficient table
                    sum += signCheckSum(value[i], m_weightCoefficient[l]);
                }
                else
                {
                    // symbols is located at the right of the finder pattern
                    int k = m_finderPaternOrder[index][i / 2 + 1]; // code of finder pattern
                    int l = k > 0 ? 4 * k - 4 : -4 * k - 2; // index for table of weighted coefficients
                    sum += signCheckSum(value[i], m_weightCoefficient[l]);
                }
            }
            return sum % 211;
        }

        protected override Size buildBars(SKCanvas canvas, SKFont font)
        {
            int maxSignNumber = 21;
            string sValue = GetEncodedValue(false);

            // Find binary value of symbol.
            string binaryValue = getBinaryValue(22);
            if ((binaryValue.Length > maxSignNumber * 12) || (binaryValue.Length % 12 != 0))
                throw new BarcodeException("Incorrect value for Databar Expanded symbology");

            // Split into signs of 12 bits and find their decimal value
            intList dataSigns = new intList();
            for (int i = 0; i < binaryValue.Length / 12; i++)
            {
                string binarySign = binaryValue.Substring(i * 12, 12);
                dataSigns.Add(Convert.ToInt16(binarySign, 2));
            }

            // find widths of elements of signs
            int[][] signs = new int[dataSigns.Count][];
            for (int i = 0; i < dataSigns.Count; i++)
            {
                signs[i] = signValue(dataSigns[i]);
            }

            // find check sign of symbol
            int checkSignValue = 211 * (dataSigns.Count - 3) + checkSum(signs);
            int[] checkSign = signValue(checkSignValue);

            intList symbol = new intList();
            GS1Utils.addArray(symbol, m_guardPattern, false);
            GS1Utils.addArray(symbol, checkSign, false);
            GS1Utils.addArray(symbol, m_finderPaternValues[0], false);
            GS1Utils.addArray(symbol, signs[0], true);

            // find index for table of finder patterns sequences
            int index = signs.Length % 2 == 0 ? (signs.Length - 2) / 2 : (signs.Length - 3) / 2;
            for (int i = 1; i < signs.Length; i++)
            {
                if (i % 2 != 0)
                {
                    // symbols is located at the left of finder pattern
                    GS1Utils.addArray(symbol, signs[i], false);
                    int k = m_finderPaternOrder[index][(i + 1) / 2 + 1]; // code of finder pattern
                    if (k > 0)
                        GS1Utils.addArray(symbol, m_finderPaternValues[k - 1], false);
                    else
                        GS1Utils.addArray(symbol, m_finderPaternValues[-k - 1], true);
                }
                else
                {
                    // symbol is located to the right of the finder pattern
                    GS1Utils.addArray(symbol, signs[i], true);
                }
            }
            GS1Utils.addArray(symbol, m_guardPattern, false);

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
