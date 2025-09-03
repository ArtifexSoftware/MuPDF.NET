/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using SkiaSharp;
using System;
using System.Drawing;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using UPC-A symbology rules. UPC stands for Universal Product 
    /// Code. This code is typically used to record point of sale transactions 
    /// for consumer goods throughout the grocery industry.
    /// </summary>
    class UPCASymbology : EAN13Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UPCASymbology"/> class.
        /// </summary>
        public UPCASymbology()
            : base()
        {
            m_type = TrueSymbologyType.UPCA;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UPCASymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public UPCASymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.UPCA;
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "00000000000";
        }

        /// <summary>
        /// Validates the value using UPC-A symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Checksum is mandatory or not (if applicable).</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length != 11 && value.Length != 12)
                return false;

	        if (value.Length == 11 && checksumIsMandatory)
		        return false;

            if (value.Length == 12)
            {
                // user wants to enter value with check digit
                // we need to verify that digit
                char c = getChecksum("0" + value.Substring(0, 11));
                if (value[11] != c)
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
            return "UPC-A symbology expects strings with 11 digits to be encoded. Optionally, user may enter 12th digit. In the latter case the last digit (check digit) will be verified.";
        }

        /// <summary>
        /// Gets the barcode value encoded using UPC-A symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            // checksum char ALWAYS added to encoded value and caption
            string s = Value.Substring(0, 11);
            char checksumChar = getChecksum("0" + s);

            if (forCaption)
                return s + checksumChar;

            // encoded value differs from printed by leading zero
            return "0" + s + checksumChar;
        }

        protected override int occupiedByCaptionAfter(SKCanvas canvas, SKFont font)
        {
            if (DrawCaption)
            {
                if (CaptionPosition == CaptionPosition.Below)
                {
                    string lastChar = Caption.Substring(Caption.Length - 1, 1);
                    float width = font.MeasureText(lastChar);
                    return (int)Math.Ceiling(width);
                }

                if (CaptionPosition == CaptionPosition.After)
                {
                    return base.occupiedByCaptionAfter(canvas, font);
                }
            }

            return 0;
        }

        protected override string getCaptionLeftPart()
        {
            if (Caption.Length >= 1)
                return Caption.Substring(1, Math.Min(Caption.Length - 1, 5));

            return string.Empty;
        }

        protected override string getCaptionRightPart()
        {
            if (Caption.Length >= 6)
                return Caption.Substring(6, Math.Min(Caption.Length - 7, 5));

            return string.Empty;
        }

        protected override string getCaptionAfterPart()
        {
            if (Caption.Length >= 0)
                return Caption.Substring(Caption.Length - 1, 1);

            return string.Empty;
        }
    }
}
