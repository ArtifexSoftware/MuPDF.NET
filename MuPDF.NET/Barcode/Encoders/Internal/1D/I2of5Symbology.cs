/**************************************************
 *
 *
 *
 *
**************************************************/

using SkiaSharp;
using System;
using System.Drawing;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using Interleaved 2 of 5 symbology rules.
    /// This symbology is used primarily in the distribution and warehouse industry.
    /// </summary>
    class I2of5Symbology : SymbologyDrawing
    {
        protected static string m_alphabet = "0123456789";

        /// <summary>
        /// Initializes a new instance of the <see cref="I2of5Symbology"/> class.
        /// </summary>
        public I2of5Symbology()
            : base(TrueSymbologyType.I2of5)
        {
            // there is no characters corresponding to Interleaved 2 of 5 
            // start and stop symbols.
            Options.ShowStartStop = false;
            AddChecksum = false;
            AddChecksumToCaption = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="I2of5Symbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public I2of5Symbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.I2of5)
        {
        }



        /// <summary>
        /// Validates the value using Interleaved 2 of 5 symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Checksum is obligatory or not.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length == 0)
                return true;

            foreach (char c in value)
            {
                if (m_alphabet.IndexOf(c) == -1)
                    return false;
            }

            // #1: if AddChecksum = true
            // EVEN number: checking if last digit is valid checksum
            // ODD number: calc and add checksum at the end (to make it EVEN number of digits)

            // #2: if AddChecksum = false
            // EVEN number: doing nothing and encoding as is
            // ODD number: add padding zero (at the beginning) to make it EVEN number of digits

            if (AddChecksum)
            {
                // if even number of characters
                if (value.Length % 2 == 0)
                {
                    // check if the last character is valid checksum
                    char curChecksumChar = value[value.Length - 1];
                    // get the checksum for the value (except the very last char
                    char ethalonChecksumChar = getChecksumChar(value.Substring(0, value.Length - 1));
                    if (curChecksumChar != ethalonChecksumChar)
                        return false;
                }
				else if (checksumIsMandatory)
				{
					return false;
				}
            }
			else if (checksumIsMandatory)
			{
				return (value.Length % 2 == 0);
			}

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
            return "Interleaved 2 of 5 symbology allows only numeric values to be encoded.\r\n" +
                "Total value length must be:\r\n\r\n" +
            "EVEN if checksum is not to be added (but the checksum will be checked if it is correct)\r\n" +
            "ODD if checksum is to be added.\r\n\r\n" +
            "when AddChecksum = false: even number of digits are encoded as is, odd number of characters is padded with zero at the beginning";
        }

        /// <summary>
        /// Gets the barcode value encoded using Interleaved 2 of 5 symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            StringBuilder sb = new StringBuilder();

            // #1: if AddChecksum = true
            // EVEN number: checking if last digit is valid checksum
            // ODD number: calc and add checksum at the end (to make it EVEN number of digits)

            // #2: if AddChecksum = false
            // EVEN number: doing nothing and encoding as is
            // ODD number: add padding zero (at the beginning) to make it EVEN number of digits

            if (AddChecksum)
            {
                sb.Append(Value);

                // append zero if value + checksum char produce string
                // with odd length
                if (Value.Length % 2 == 0)
                {
                    ; // the valid checksum is valided in ValueIsValid() already
                }
                else // we have ODD number of digits
                {
                    // so we should calculate and add the checksum
                    if ((forCaption && AddChecksumToCaption) || (!forCaption))
                    {
                        char checksumChar = getChecksumChar(Value);
                        sb.Append(checksumChar);
                    }
                }                            
            }
            else // if AddChecksum == false
            {
                // if we are not adding the checksum then we are padding 
                // with zeros to have EVEN number of characters!

                // append zero if value has odd length
                if (Value.Length % 2 != 0)
                    sb.Append("0");

                sb.Append(Value);
            }

            return sb.ToString();
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
            switch (c)
            {
                case '0':
                    return "nnwwn";
                case '1':
                    return "wnnnw";
                case '2':
                    return "nwnnw";
                case '3':
                    return "wwnnn";
                case '4':
                    return "nnwnw";
                case '5':
                    return "wnwnn";
                case '6':
                    return "nwwnn";
                case '7':
                    return "nnnww";
                case '8':
                    return "wnnwn";
                case '9':
                    return "nwnwn";
            }

            return "wwwww";
        }

        /// <summary>
        /// Gets the checksum char.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The checksum char.</returns>
        protected virtual char getChecksumChar(string value)
        {
            int evenSum = 0;
            int oddSum = 0;

            for (int i = 0; i < value.Length; i++)
            {
                if (i % 2 == 0)
                    evenSum += getCharPosition(value[i]);
                else
                    oddSum += getCharPosition(value[i]);
            }

            int total = 0;
            if (value.Length % 2 == 1)
                total = evenSum * 3 + oddSum;
            else
                total = oddSum * 3 + evenSum;

            int checkDigitPos = (total / 10) * 10 + 10 - total;
            if (checkDigitPos % 10 == 0)
                checkDigitPos = 0;

            return m_alphabet[checkDigitPos];
        }

        /// <summary>
        /// Gets the char position within the alphabet.
        /// </summary>
        /// <param name="c">The char to find.</param>
        /// <returns></returns>
        protected static int getCharPosition(char c)
        {
            for (int i = 0; i < m_alphabet.Length; i++)
            {
                if (m_alphabet[i] == c)
                    return i;
            }

            string message = String.Format("Incorrect character '%c' for Interleaved 2 of 5 symbology", c);
            throw new BarcodeException(message);
        }

        protected override Size buildBars(SKCanvas canvase, SKFont font)
        {
            Size drawingSize = new Size();
            int x = 0;
            int y = 0;

            string value = GetEncodedValue(false);
            if (value.Length % 2 != 0)
                throw new BarcodeException("Incorrect value length.");

            x += drawStartCode(x, y);

            int widthNarrow = NarrowBarWidth;
            int widthWide = NarrowBarWidth * WideToNarrowRatio;
            int height = BarHeight;

            for (int i = 0; i < (value.Length - 1); i += 2)
            {
                string barsPattern = getCharPattern(value[i]);
                string gapsPattern = getCharPattern(value[i + 1]);

                for (int patternPos = 0; patternPos < barsPattern.Length; patternPos++)
                {
                    int barWidth = widthNarrow;
                    if (barsPattern[patternPos] == 'w')
                        barWidth = widthWide;

                    int gapWidth = widthNarrow;
                    if (gapsPattern[patternPos] == 'w')
                        gapWidth = widthWide;

                    m_rects.Add(new Rectangle(x, y, barWidth, height));

                    x += barWidth;
                    x += gapWidth;
                }
            }

            x += drawStopCode(x, y);

            drawingSize.Width = x;
            drawingSize.Height = BarHeight;
            return drawingSize;
        }

        /// <summary>
        /// Draws the start code.
        /// </summary>
        /// <param name="x">The start X position.</param>
        /// <param name="y">The start Y position.</param>
        /// <returns>
        /// The width of the rectangle occupied by barcode start code.
        /// </returns>
        protected int drawStartCode(int x, int y)
        {
            // The start code consists of two narrow bars and two narrow spaces

            int widthNarrow = NarrowBarWidth;
            int height = BarHeight;
            int widthOccupied = 0;

            for (int i = 0; i < 2; i++)
            {
                m_rects.Add(new Rectangle(x + widthOccupied, y, widthNarrow, height));
                widthOccupied += widthNarrow;
                widthOccupied += widthNarrow;
            }

            return widthOccupied;
        }

        /// <summary>
        /// Draws the start code.
        /// </summary>
        /// <param name="x">The start X position.</param>
        /// <param name="y">The start Y position.</param>
        /// <returns>
        /// The width of the rectangle occupied by barcode start code.
        /// </returns>
        protected int drawStopCode(int x, int y)
        {
            // the stop code consists of a wide bar, narrow space, and a narrow bar.

            int widthNarrow = NarrowBarWidth;
            int widthWide = NarrowBarWidth * WideToNarrowRatio;
            int height = BarHeight;
            int widthOccupied = 0;

            m_rects.Add(new Rectangle(x + widthOccupied, y, widthWide, height));
            widthOccupied += widthWide;
            widthOccupied += widthNarrow;

            m_rects.Add(new Rectangle(x + widthOccupied, y, widthNarrow, height));
            widthOccupied += widthNarrow;

            return widthOccupied;
        }
    }
}
