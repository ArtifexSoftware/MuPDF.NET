using System;
using System.Text;
using System.Drawing;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws Royal Mail 4-State barcodes.
    /// See also: 
    /// http://en.wikipedia.org/wiki/RM4SCC
    /// http://www.morovia.com/education/symbology/royalmail.asp
    /// </summary>
    class RoyalMailSymbology : SymbologyDrawing
    {
        protected const string m_alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        protected static string[] m_royalValues = {
            "11", "12", "13", "14", "15", "10", "21", "22", "23", "24", 
            "25", "20", "31", "32", "33", "34", "35", "30", "41", "42", 
            "43", "44", "45", "40", "51", "52", "53", "54", "55", "50", 
            "01", "02", "03", "04", "05", "00"
        };

        /* 0 = Full, 1 = Ascender, 2 = Descender, 3 = Tracker */
        protected static string[] m_royalTable = {
            "3300", "3210", "3201", "2310", "2301", "2211", "3120", "3030", 
            "3021", "2130", "2121", "2031", "3102", "3012", "3003", "2112", 
            "2103", "2013", "1320", "1230", "1221", "0330", "0321", "0231", 
            "1302", "1212", "1203", "0312", "0303", "0213", "1122", "1032", 
            "1023", "0132", "0123", "0033"};

        /// <summary>
        /// Initializes a new instance of the <see cref="RoyalMailSymbology"/> class.
        /// </summary>
        public RoyalMailSymbology()
            : base(TrueSymbologyType.RoyalMail)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoyalMailSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public RoyalMailSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.RoyalMail)
        {
        }

        /// <summary>
        /// Validates the value using current symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            string upper = value.ToUpper();

            foreach (char c in upper)
            {
                if (getCharPosition(c) == -1)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the char position within the alphabet.
        /// </summary>
        /// <param name="c">The char to find.</param>
        /// <returns></returns>
        protected static int getCharPosition(char c)
        {
            return m_alphabet.IndexOf(c);
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "Royal Mail 4-State symbology allows only digits and characters from A to Z to be encoded.";
        }

        /// <summary>
        /// Gets the barcode value encoded using current symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            StringBuilder sb = new StringBuilder();

            if (!forCaption)
                sb.Append("(");

            string upper = Value.ToUpper();
            sb.Append(upper);

            if (AddChecksum)
            {
                if ((forCaption && AddChecksumToCaption) || (!forCaption))
                {
                    int checksumCharPosition = calculateChecksum(upper);
                    char checksumChar = m_alphabet[checksumCharPosition];
                    sb.Append(checksumChar);
                }
            }

            if (!forCaption)
                sb.Append(")");

            return sb.ToString();
        }

        /// <summary>
        /// Gets the encoding pattern for given character.
        /// </summary>
        /// <param name="c">The character to retrieve pattern for.</param>
        /// <returns>The encoding pattern for given character.</returns>
        protected override string getCharPattern(char c)
        {
            if (c == '(')
            {
                // start symbol
                return "1";
            }
            else if (c == ')')
            {
                // stop symbol
                return "0";
            }

            int pos = getCharPosition(c);
            return m_royalTable[pos];
        }

        /// <summary>
        /// Calculates the checksum of the given value.
        /// </summary>
        /// <param name="value">The value to calculate checksum for.</param>
        /// <returns></returns>
        private static int calculateChecksum(string value)
        {
            int top = 0;
            int bottom = 0;
            foreach (char c in value)
            {
                int pos = getCharPosition(c);
                string royalValue = m_royalValues[pos];
                int checksumValue = int.Parse(royalValue);

                top += checksumValue / 10;
                bottom += checksumValue % 10;
            }

            // Calculate the check digit
            int row = (top % 6) - 1;
            int column = (bottom % 6) - 1;

            if (row == -1)
                row = 5;

            if (column == -1)
                column = 5;

            return (6 * row) + column;
        }

        protected override SKSize buildBars(SKCanvas canvas, SKFont font)
        {
            SKSize drawingSize = new SKSize();
            int x = 0;
            int y = 0;

            int height = BarHeight;
            int width = NarrowBarWidth;

            string encoded = GetEncodedValue(false);

            int fullHeight = height;
            int ascenderHeight = Math.Max(6 * height / 10, 1);
            int descenderHeight = ascenderHeight;
            int trackerHeight = Math.Max(ascenderHeight / 3, 1);
            int gapWidth = Math.Max(NarrowBarWidth / 2, 1);

            foreach (char c in encoded)
            {
                string pattern = getCharPattern(c);
                foreach (char patternChar in pattern)
                {
                    // 0 = Full, 1 = Ascender, 2 = Descender, 3 = Tracker
                    if (patternChar == '0')
                        m_rects.Add(new SKRect(x, y, width+x, fullHeight+y));
                    else if (patternChar == '1')
                        m_rects.Add(new SKRect(x, y, x+width, y+ascenderHeight));
                    else if (patternChar == '2')
                        m_rects.Add(new SKRect(x, y + ascenderHeight - trackerHeight, x+width, y + ascenderHeight - trackerHeight+descenderHeight));
                    else
                        m_rects.Add(new SKRect(x, y + ascenderHeight - trackerHeight, x + width, y + ascenderHeight));

                    x += width;
                    x += gapWidth;
                }
            }

            drawingSize.Width = x;
            drawingSize.Height = BarHeight;
            return drawingSize;
        }
    }
}
