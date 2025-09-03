/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;
using System.Drawing;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using EAN8 symbology rules. EAN-8 is a short version 
    /// of EAN-13 that is intended to be used on packaging which would 
    /// be otherwise too small to use one of the other versions. Used with 
    /// consumer products internationally. EAN-8 symbology allows only numeric 
    /// values to be encoded.
    /// </summary>
    class EAN8Symbology : EAN13Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EAN8Symbology"/> class.
        /// </summary>
        public EAN8Symbology()
            : base()
        {
            m_type = TrueSymbologyType.EAN8;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EAN8Symbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public EAN8Symbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.EAN8;
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "0000000";
        }

        /// <summary>
        /// Validates the value using EAN-8 symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Checksum is mandatory or not (if applicable).</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length != 7 && value.Length != 8)
                return false;

	        if (value.Length == 7 && checksumIsMandatory)
		        return false;

	        if (value.Length == 8)
            {
                // user wants to enter value with check digit
                // we need to verify that digit
                char c = getChecksum(value.Substring(0, 7));
                if (value[7] != c)
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
            return "EAN-8 symbology expects strings with 7 digits to be encoded. Optionally, user may enter 8th digit. In the latter case the last digit (check digit) will be verified.";
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
            string s = Value.Substring(0, 7);
            return s + getChecksum(s);
        }

        /// <summary>
        /// Gets the checksum char.
        /// </summary>
        /// <param name="value">The value to calculate checksum for.</param>
        /// <returns>The checksum char.</returns>
        protected override char getChecksum(string value)
        {
            int total = 0;
            for (int i = 0; i < value.Length; i++)
            {
                if (i % 2 == 0)
                    total += getCharPosition(value[i]) * 3;
                else
                    total += getCharPosition(value[i]);
            }

            int lastDigit = total % 10;
            if (lastDigit == 0)
                return '0';

            return m_alphabet[10 - lastDigit];
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
        protected override int drawLeftHandPart(int x, int y)
        {
            string encoded = GetEncodedValue(false);

            for (int i = 0; i < 4; i++)
            {
                string pattern = getLeftOddCharPattern(encoded[i]);
                x = drawPattern(pattern, x, y, NarrowBarWidth, BarHeight);
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
        protected override int drawRightHandPart(int x, int y)
        {
            string encoded = GetEncodedValue(false);

            for (int i = 0; i < 4; i++)
            {
                string pattern = getRightCharPattern(encoded[4 + i]);
                x = drawPattern(pattern, x, y, NarrowBarWidth, BarHeight);
            }

            return x;
        }

        protected override int occupiedByCaptionBefore(SKCanvas canvas, SKFont font)
        {
			if (DrawCaption)
			{
				if (CaptionPosition == CaptionPosition.Before)
				{
					return base.occupiedByCaptionBefore(canvas, font);
				}
			}

            return 0;
        }

        protected override string getCaptionBeforePart()
        {
            // EAN-8 does not draw first char
            return string.Empty;
        }

        protected override string getCaptionLeftPart()
        {
            return Caption.Substring(0, Math.Min(Caption.Length - 1, 4));
        }

        protected override string getCaptionRightPart()
        {
            if (Caption.Length >= 5)
                return Caption.Substring(4);

            return string.Empty;
        }
    }
}
