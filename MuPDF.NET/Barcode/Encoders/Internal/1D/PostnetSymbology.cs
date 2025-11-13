/**************************************************
 *
 *
 *
 *
**************************************************/

using System;
using System.Text;
using System.Drawing;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using Postnet symbology. PostNet was developed by the 
    /// United States Postal Service (USPS) to allow faster sorting and 
    /// routing of mail. Postnet is often printed on envelopes and business 
    /// return mail.
    /// </summary>
    class PostnetSymbology : SymbologyDrawing
    {
        protected static string m_alphabet = "0123456789";

        /// <summary>
        /// Initializes a new instance of the <see cref="PostnetSymbology"/> class.
        /// </summary>
        public PostnetSymbology()
            : base(TrueSymbologyType.Postnet)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostnetSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public PostnetSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.Postnet)
        {
        }

        /// <summary>
        /// Gets a value indicating whether this symbology can not have
        /// a caption drawn.
        /// </summary>
        public override bool PreventCaptionDrawing
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "00000";
        }

        /// <summary>
        /// Validates the value using Postnet symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length != 5 && value.Length != 9 && value.Length != 11)
                return false;

            foreach (char c in value)
            {
                if (m_alphabet.IndexOf(c) == -1)
                    return false;
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
            return "Postnet symbology allows only strings with five, nine or eleven digits to be encoded.";
        }

        /// <summary>
        /// Gets the barcode value encoded using Postnet symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            // caption is never drawn
            // checksum char ALWAYS added to encoded value
            return Value + getChecksum(Value);
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
                case 's': // start|stop
                    return "1";

                case '0':
                    return "11000";

                case '1':
                    return "00011";

                case '2':
                    return "00101";

                case '3':
                    return "00110";

                case '4':
                    return "01001";

                case '5':
                    return "01010";

                case '6':
                    return "01100";

                case '7':
                    return "10001";

                case '8':
                    return "10010";

                case '9':
                    return "10100";
            }

            throw new BarcodeException("Incorrect symbol for Postnet symbology");
        }

        /// <summary>
        /// Gets the checksum char.
        /// </summary>
        /// <param name="value">The value to calculate checksum for.</param>
        /// <returns>The checksum char.</returns>
        private static char getChecksum(string value)
        {
            int total = 0;
            for (int i = 0; i < value.Length; i++)
                total += getCharPosition(value[i]);

            int lastDigit = total % 10;
            if (lastDigit == 0)
                return '0';

            return m_alphabet[10 - lastDigit];
        }

        /// <summary>
        /// Gets the char position within alphabet.
        /// </summary>
        /// <param name="c">The char.</param>
        /// <returns>The char position within alphabet.</returns>
        private static int getCharPosition(char c)
        {
            int charPos = m_alphabet.IndexOf(c);
            if (charPos == -1)
                throw new BarcodeException("Incorrect char for Postnet symbology");

            return charPos;
        }

        protected override SKSize buildBars(SKCanvas canvas, SKFont font)
        {
            SKSize drawingSize = new SKSize();
            int x = 0;
            int y = 0;

            int tallHeight = BarHeight;
            int shortHeight = BarHeight / WideToNarrowRatio;
            int gapWidth = Math.Max(NarrowBarWidth / 2, 1);
            int width = NarrowBarWidth;

            string value = "s" + GetEncodedValue(false) + "s";
            foreach (char c in value)
            {
                string pattern = getCharPattern(c);
                foreach (char patternChar in pattern)
                {
                    if (patternChar == '0')
                        m_rects.Add(new SKRect(x, y + tallHeight - shortHeight, x+width, y + tallHeight));
                    else
                        m_rects.Add(new SKRect(x, y, width+x, tallHeight+y));

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
