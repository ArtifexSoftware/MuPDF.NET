/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;
using System.Drawing;

namespace BarcodeWriter.Core.Internal
{
    class UPCESymbology : UPCASymbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UPCESymbology"/> class.
        /// </summary>
        public UPCESymbology()
            : base()
        {
            m_type = TrueSymbologyType.UPCE;
            m_rightGuardPattern = "1";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UPCESymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public UPCESymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.UPCE;
            m_rightGuardPattern = "1";
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
        /// Validates the value using UPC-E symbology rules.
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

            if (value[0] != '0' && value[0] != '1')
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
            return "UPC-E symbology expects strings with 7 digits to be encoded. First digit must be 0 or 1. Optionally, user may enter 8th digit. In the latter case the last digit (check digit) will be verified.";
        }

        /// <summary>
        /// Gets the barcode value encoded using UPC-E symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            // checksum char ALWAYS added to caption and NOT added to encoded value
            string s = Value.Substring(0, 7);

            if (forCaption)
            {
                char checksumChar = getChecksum(s);
                return s + checksumChar;
            }

            return s;
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
            // UPC-E symbology does not contain right-hand part.
            return x;
        }

        /// <summary>
        /// Gets the checksum char.
        /// </summary>
        /// <param name="value">The value to calculate checksum for.</param>
        /// <returns>The checksum char.</returns>
        protected override char getChecksum(string value)
        {
            string upcaValue = toUPCA(value.Substring(1));
            return base.getChecksum("0" + value[0] + upcaValue);
        }

        /// <summary>
        /// Converts UPC-E value to the UPC-A equivalent.
        /// </summary>
        /// <param name="value">The UPC-E value.</param>
        /// <returns>The UPC-A equivalent.</returns>
        private static string toUPCA(string value)
        {
            char lastChar = value[value.Length - 1];
            switch (lastChar)
            {
                case '0':
                case '1':
                case '2':
                    // if UPC-E code ends in 0, 1, or 2: The UPC-A code is 
                    // determined by taking the first two digits of the 
                    // UPC-E code, taking the last digit of the UPC-E code, 
                    // adding four 0 digits, and then adding characters 3 
                    // through 5 from the UPC-E code.
                    return value.Substring(0, 2) + lastChar + "0000" + value.Substring(2, 3);

                case '3':
                    // if UPC-E code ends in 3: The UPC-A code is determined 
                    // by taking the first three digits of the UPC-E code, 
                    // adding five 0 digits, then adding characters 4 and 5 
                    // from the UPC-E code.
                    return value.Substring(0, 3) + "00000" + value.Substring(3, 2);

                case '4':
                    // if UPC-E code ends in 4: The UPC-A code is determined 
                    // by taking the first four digits of the UPC-E code, 
                    // adding five 0 digits, then adding the fifth character 
                    // from the UPC-E code.
                    return value.Substring(0, 4) + "00000" + value.Substring(4, 1);

                default:
                    // if UPC-E code ends in 5, 6, 7, 8, or 9: The UPC-A 
                    // code is determined by taking the first five digits of 
                    // the UPC-E code, adding four 0 digits, then adding the 
                    // last character from the UPC-E code.
                    return value.Substring(0, 5) + "0000" + lastChar;
            }
        }

        /// <summary>
        /// Gets the parity string.
        /// </summary>
        /// <param name="firstNumber">The first number.</param>
        /// <returns>The parity string.</returns>
        protected override string getParityString(char firstNumber)
        {
            string encoded = GetEncodedValue(true);
            char checkSumChar = encoded[encoded.Length - 1];
            string pattern = null;
            switch (checkSumChar)
            {
                default:
                case '0':
                    pattern = "eeeooo";
                    break;

                case '1':
                    pattern = "eeoeoo";
                    break;

                case '2':
                    pattern = "eeooeo";
                    break;

                case '3':
                    pattern = "eeoooe";
                    break;

                case '4':
                    pattern = "eoeeoo";
                    break;

                case '5':
                    pattern = "eooeeo";
                    break;

                case '6':
                    pattern = "eoooee";
                    break;

                case '7':
                    pattern = "eoeoeo";
                    break;

                case '8':
                    pattern = "eoeooe";
                    break;

                case '9':
                    pattern = "eooeoe";
                    break;
            }

            if (firstNumber == '1')
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < pattern.Length; i++)
                {
                    if (pattern[i] == 'e')
                        sb.Append('o');
                    else
                        sb.Append('e');
                }

                pattern = sb.ToString();
            }

            return pattern;
        }

        protected override string getCaptionLeftPart()
        {
            if (Caption.Length >= 1)
                return Caption.Substring(1, Math.Min(Caption.Length - 1, 6));

            return string.Empty;
        }

        protected override string getCaptionRightPart()
        {
            return string.Empty;
        }
    }
}
