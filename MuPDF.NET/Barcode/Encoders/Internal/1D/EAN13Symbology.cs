/**************************************************
 *
 *
 *
 *
**************************************************/

using System;
using System.Text;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using EAN13 symbology rules. EAN-13, based upon the 
    /// UPC-A standard, was implemented by the International Article 
    /// Numbering Association (EAN) in Europe. EAN-13 used with consumer 
    /// products internationally.
    /// </summary>
    class EAN13Symbology : SymbologyDrawing
    {
        protected static string m_alphabet = "0123456789";

        protected int m_leftLeft;
        protected int m_leftRight;
        protected int m_rightLeft;
        protected int m_rightRight;

        protected string m_leftGuardPattern = "101";
        protected string m_centerGuardPattern = "01010";
        protected string m_rightGuardPattern = "101";

        /// <summary>
        /// Initializes a new instance of the <see cref="EAN13Symbology"/> class.
        /// </summary>
        public EAN13Symbology()
            : base(TrueSymbologyType.EAN13)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EAN13Symbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public EAN13Symbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.EAN13)
        {
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "000000000000";
        }

        /// <summary>
        /// Validates the value using EAN-13 symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Checksum is mandatory or not (if applicable).</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length != 12 && value.Length != 13)
                return false;

	        if (value.Length == 12 && checksumIsMandatory)
		        return false;

            if (value.Length == 13)
            {
                // user wants to enter value with check digit
                // we need to verify that digit
                char c = getChecksum(value.Substring(0, 12));
                if (value[12] != c)
                    return false;
            }

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
            return "EAN-13 symbology expects strings with 12 digits to be encoded. Optionally, user may enter 13th digit. In the latter case the last digit (check digit) will be verified.";
        }

        /// <summary>
        /// Gets the barcode value encoded using EAN-13 symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            // checksum char ALWAYS added to encoded value and caption
            string s = Value.Substring(0, 12);
            return s + getChecksum(s);
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
        /// Gets the left-hand odd parity char pattern.
        /// </summary>
        /// <param name="c">The char to retrieve pattern for.</param>
        /// <returns>The left-hand odd parity char pattern.</returns>
        protected static string getLeftOddCharPattern(char c)
        {
            switch (c)
            {
                case '0':
                    return "0001101";

                case '1':
                    return "0011001";

                case '2':
                    return "0010011";

                case '3':
                    return "0111101";

                case '4':
                    return "0100011";

                case '5':
                    return "0110001";

                case '6':
                    return "0101111";

                case '7':
                    return "0111011";

                case '8':
                    return "0110111";

                case '9':
                    return "0001011";
            }

            return "0000000";
        }

        /// <summary>
        /// Gets the left-hand even parity char pattern.
        /// </summary>
        /// <param name="c">The char to retrieve pattern for.</param>
        /// <returns>The left-hand even parity char pattern.</returns>
        protected static string getLeftEvenCharPattern(char c)
        {
            // The "left-hand even" encoding pattern is based on the "left-hand odd" 
            // encoding pattern. To arrive at the even encoding, work from 
            // the left encoding and do the following: 
            // 1) Change all the 1's to 0's and 0's to 1. 
            // 2) Read the resulting encoding in reverse order (from right 
            //    to left). The result is the "left-hand even" encoding pattern. 

            string leftOdd = getLeftOddCharPattern(c);
            StringBuilder sb = new StringBuilder();
            for (int i = leftOdd.Length - 1; i >= 0; i--)
            {
                if (leftOdd[i] == '0')
                    sb.Append('1');
                else
                    sb.Append('0');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the right-hand char pattern.
        /// </summary>
        /// <param name="c">The char to retrieve pattern for.</param>
        /// <returns>The right-hand char pattern.</returns>
        protected static string getRightCharPattern(char c)
        {
            // The "right-hand" encoding pattern is exactly the same as the 
            // "left-hand odd" encoding pattern, but with 1's changed to 0's, 
            // and 0's changed to 1's.

            string leftOdd = getLeftOddCharPattern(c);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < leftOdd.Length; i++)
            {
                if (leftOdd[i] == '0')
                    sb.Append('1');
                else
                    sb.Append('0');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the parity string.
        /// </summary>
        /// <param name="firstNumber">The first number.</param>
        /// <returns>The parity string.</returns>
        protected virtual string getParityString(char firstNumber)
        {
            switch (firstNumber)
            {
                default:
                case '0':
                    return "oooooo";

                case '1':
                    return "ooeoee";

                case '2':
                    return "ooeeoe";

                case '3':
                    return "ooeeeo";

                case '4':
                    return "oeooee";

                case '5':
                    return "oeeooe";

                case '6':
                    return "oeeeoo";

                case '7':
                    return "oeoeoe";

                case '8':
                    return "oeoeeo";

                case '9':
                    return "oeeoeo";
            }
        }

        /// <summary>
        /// Gets the checksum char.
        /// </summary>
        /// <param name="value">The value to calculate checksum for.</param>
        /// <returns>The checksum char.</returns>
        protected virtual char getChecksum(string value)
        {
            int total = 0;
            for (int i = 0; i < value.Length; i++)
            {
                if (i % 2 == 0)
                    total += getCharPosition(value[i]);
                else
                    total += getCharPosition(value[i]) * 3;
            }

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
        protected static int getCharPosition(char c)
        {
            int charPos = m_alphabet.IndexOf(c);
            if (charPos == -1)
                throw new BarcodeException("Incorrect char for EAN-13 symbology");

            return charPos;
        }

        protected override SKSize buildBars(SKCanvas canvas, SKFont font)
        {
            SKSize drawingSize = new SKSize();
            int x = 0;
            int y = 0;

            int height = BarHeight;
            int width = NarrowBarWidth;

            SKSize captionSize = calculateCaptionSize(canvas, font);
            float guardHeight = height + captionSize.Height / 2;

            // left guard bars
            x = (int)drawPattern(m_leftGuardPattern, x, y, width, guardHeight);

            m_leftLeft = x;
            x = drawLeftHandPart(x, y);
            m_leftRight = x;

            // center guard bars
            x = (int)drawPattern(m_centerGuardPattern, x, y, width, guardHeight);

            m_rightLeft = x;
            x = drawRightHandPart(x, y);
            m_rightRight = x;

            // right guard bars
            x = (int)drawPattern(m_rightGuardPattern, x, y, width, guardHeight);

            drawingSize.Width = x;
            drawingSize.Height = BarHeight + captionSize.Height / 2;
            return drawingSize;
        }

        /// <summary>
        /// Draws left-hand part of the bars.
        /// </summary>
        /// <param name="x">The start X position.</param>
        /// <param name="y">The start Y position.</param>
        /// <returns>
        /// The new X position (start X position + the width of the rectangle
        /// occupied by right-hand part of barcode bars and gaps).
        /// </returns>
        protected virtual int drawLeftHandPart(int x, int y)
        {
            string encoded = GetEncodedValue(false);

            // draw second char and 5 left-hand chars (Manufacturer digits) taking parity pattern into account
            string parityPattern = getParityString(encoded[0]);
            string pattern = null;
            for (int i = 0; i < 6; i++)
            {
                if (parityPattern[i] == 'o')
                    pattern = getLeftOddCharPattern(encoded[1 + i]);
                else
                    pattern = getLeftEvenCharPattern(encoded[1 + i]);

                x = (int)drawPattern(pattern, x, y, NarrowBarWidth, BarHeight);
            }

            return x;
        }

        /// <summary>
        /// Draws right-hand part of the bars.
        /// </summary>
        /// <param name="x">The start X position.</param>
        /// <param name="y">The start Y position.</param>
        /// <returns>
        /// The new X position (start X position + the width of the rectangle
        /// occupied by right-hand part of barcode bars and gaps).
        /// </returns>
        protected virtual int drawRightHandPart(int x, int y)
        {
            string encoded = GetEncodedValue(false);

            // draw 5 right-hand (Product Code) digits and checksum char
            for (int i = 0; i < 6; i++)
            {
                string pattern = getRightCharPattern(encoded[7 + i]);
                x = (int)drawPattern(pattern, x, y, NarrowBarWidth, BarHeight);
            }

            return x;
        }

        /// <summary>
        /// Draws the pattern.
        /// </summary>
        /// <param name="pattern">The pattern to draw.</param>
        /// <param name="x">The start X position.</param>
        /// <param name="y">The start Y position.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns>
        /// The next horizontal position to draw barcode part at.
        /// </returns>
        protected float drawPattern(string pattern, float x, float y, float width, float height)
        {
            foreach (char patternChar in pattern)
            {
	            bool drawBar = patternChar == '1';

	            if (drawBar)
                    m_rects.Add(new SKRect(x, y, width+x, height+y));

                x += width;
            }

            return x;
        }

        protected override int occupiedByCaptionBefore(SKCanvas canvas, SKFont font)
        {
	        if (DrawCaption)
	        {
		        if (CaptionPosition == CaptionPosition.Below)
		        {
                    SKRect bounds = new SKRect();
                    string firstChar = Caption.Substring(0, 1);
                    font.MeasureText(firstChar, out bounds);
                    return (int)bounds.Width;
		        }
		
				if (CaptionPosition == CaptionPosition.Before)
		        {
			        return base.occupiedByCaptionBefore(canvas, font);
		        }
	        }

	        return 0;
        }

        protected override SKSize occupiedByCaptionBelow(SKCanvas canvas, SKFont font)
        {
            SKSize captionSize = new SKSize();

            if (CaptionMayBeDrawn && CaptionPosition == CaptionPosition.Below)
            {
                // if caption is drawn below bars then part of the caption
                // is drawn inside the bars. so, we should return the size
                // of part that is drawn outside the bars only
                captionSize = calculateCaptionSize(canvas, font);
                int captionGap = Utils.CalculateCaptionGap(font);
                captionSize.Height += captionGap;
                captionSize.Height /= 2;
            }

            return captionSize;
        }

        protected virtual string getCaptionBeforePart()
        {
            if (Caption.Length >= 0)
                return Caption.Substring(0, 1);

            return string.Empty;
        }

        protected virtual string getCaptionLeftPart()
        {
            if (Caption.Length >= 1)
                return Caption.Substring(1, Math.Min(Caption.Length - 1, 6));

            return string.Empty;
        }

        protected virtual string getCaptionRightPart()
        {
            if (Caption.Length >= 7)
                return Caption.Substring(7);

            return string.Empty;
        }

        protected virtual string getCaptionAfterPart()
        {
            return string.Empty;
        }

        protected override void drawCaptionBeforePart(SKCanvas canvas, SKPaint paint, SKFont font, SKPoint position)
        {
            // When caption is drawn above the bars then it's not split in parts
            if (CaptionPosition == CaptionPosition.Below)
            {
                string firstChar = getCaptionBeforePart();
                if (string.IsNullOrEmpty(firstChar))
                    return;

                // Measure width and height
                float textWidth = font.MeasureText(firstChar);
                var metrics = font.Metrics;
                float textHeight = metrics.Descent - metrics.Ascent + metrics.Leading;

                int captionGap = CustomCaptionGap == -1 ? Utils.CalculateCaptionGap(font) : CustomCaptionGap;
                int captionTop = System.Math.Max((int)(position.Y + BarHeight + captionGap), 0);

                // Compute rectangle (for reference; X/Y used directly)
                SKRect captionRect = new SKRect(
                    position.X,
                    captionTop,
                    position.X + textWidth + 1,
                    captionTop + textHeight + 1
                );

                // Compute centered X
                float x = position.X + (textWidth / 2); // centered relative to position.X (adjust as needed)

                // Compute baseline Y
                float y = captionTop - metrics.Ascent; // top alignment

                canvas.DrawText(firstChar, x, y, font, paint);
            }
        }

        protected override void drawCaption(SKCanvas canvas, SKPaint paint, SKFont font, SKPoint position)
        {
            if (CaptionPosition == CaptionPosition.Below)
            {
                int captionGap = CustomCaptionGap == -1 ? Utils.CalculateCaptionGap(font) : CustomCaptionGap;
                int captionTop = (int)(position.Y + BarHeight + captionGap);

                // Compute caption height using font metrics
                var metrics = font.Metrics;
                float captionHeight = metrics.Descent - metrics.Ascent + metrics.Leading;

                // Draw left group
                string leftGroup = getCaptionLeftPart();
                if (!string.IsNullOrEmpty(leftGroup))
                {
                    float textWidth = font.MeasureText(leftGroup);
                    float x = position.X + m_leftLeft + ((m_leftRight - m_leftLeft) - textWidth) / 2f;
                    float y = captionTop - metrics.Ascent; // top alignment

                    canvas.DrawText(leftGroup, x, y, font, paint);
                }

                // Draw right group
                string rightGroup = getCaptionRightPart();
                if (!string.IsNullOrEmpty(rightGroup))
                {
                    float textWidth = font.MeasureText(rightGroup);
                    float x = position.X + m_rightLeft + ((m_rightRight - m_rightLeft) - textWidth) / 2f;
                    float y = captionTop - metrics.Ascent; // top alignment

                    canvas.DrawText(rightGroup, x, y, font, paint);
                }
            }
            else
            {
                // Draw above or default using base method
                base.drawCaption(canvas, paint, font, position);
            }
        }

        protected override void drawCaptionAfterPart(SKCanvas canvas, SKPaint paint, SKFont font, SKPoint position)
        {
            if (CaptionPosition == CaptionPosition.Below)
            {
                string lastChar = getCaptionAfterPart();
                if (string.IsNullOrEmpty(lastChar))
                    return;

                // Measure width and height
                float textWidth = font.MeasureText(lastChar);
                var metrics = font.Metrics;
                float textHeight = metrics.Descent - metrics.Ascent + metrics.Leading;

                int captionGap = Utils.CalculateCaptionGap(font);
                int captionTop = Math.Max((int)(position.Y + BarHeight + captionGap), 0);

                // Compute rectangle (for reference)
                SKRect captionRect = new SKRect(
                    position.X + m_drawingSize.Width,
                    captionTop,
                    position.X + m_drawingSize.Width + textWidth + 1,
                    captionTop + textHeight + 1
                );

                // Centered X inside rectangle
                float x = captionRect.Left + ((captionRect.Width - textWidth) / 2f);

                // Baseline Y (top-aligned)
                float y = captionTop - metrics.Ascent;

                canvas.DrawText(lastChar, x, y, font, paint);
            }
        }

        protected override SKFont getFontForCaption(SKCanvas canvas, CaptionPosition position)
        {
            if (position == CaptionPosition.Below)
            {
                // Widths of left and right sections between guard bars
                float leftWidth = m_leftRight - m_leftLeft;
                float rightWidth = m_rightRight - m_rightLeft;

                float minWidth = rightWidth;
                if (leftWidth < rightWidth || rightWidth == 0)
                    minWidth = leftWidth;

                // Calculate font sizes for left and right groups
                string leftGroup = getCaptionLeftPart();
                SKFont leftFont = calculateFont(leftGroup, CaptionFont.Typeface, CaptionFont.Size, minWidth);

                string rightGroup = getCaptionRightPart();
                SKFont rightFont = calculateFont(rightGroup, CaptionFont.Typeface, CaptionFont.Size, minWidth);

                // Return the smaller font size to fit both
                if (rightFont.Size < leftFont.Size)
                {
                    leftFont.Dispose();
                    return rightFont;
                }

                rightFont.Dispose();
                return leftFont;
            }
            else
            {
                // For captions above, fallback to base behavior
                return base.getFontForCaption(canvas, position);
            }
        }
        
        private static SKFont calculateFont(string text, SKTypeface typeface, float startingSize, float maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return new SKFont(typeface, startingSize);

            float fontSize = startingSize;
            SKFont result = new SKFont(typeface, fontSize);

            float textWidth = result.MeasureText(text);

            // Decrease font size until text fits within maxWidth
            while (textWidth > maxWidth && fontSize > 1)
            {
                result.Dispose(); // dispose previous font
                fontSize -= 0.5f;
                result = new SKFont(typeface, fontSize);
                textWidth = result.MeasureText(text);
            }

            return result;
        }
    }
}
