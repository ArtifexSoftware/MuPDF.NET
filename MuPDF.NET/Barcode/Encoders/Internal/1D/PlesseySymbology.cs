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
    /// Draws barcodes using Plessey Code symbology rules.
    /// This symbology is used primarily in libraries and for retail grocery shelf marking.
    /// </summary>
    /// <remarks>See also:
    /// http://en.wikipedia.org/wiki/Plessey_Code
    /// http://www.adams1.com/plessy.html
    /// </remarks>
    class PlesseySymbology : SymbologyDrawing
    {
        protected static string m_alphabet = "0123456789ABCDEF";
        protected static int m_crc8poly = 0xE9; // generator polinomial g(x) = x^8 + x^7 + x^6 + x^5 + x^3 + 1
        protected static string m_forwardStartCode = "1101";
        protected static string m_reverseStartCode = "0011";

        private CRC8 crc = new CRC8(m_crc8poly);

        /// <summary>
        /// Initializes a new instance of the <see cref="PlesseySymbology"/> class.
        /// </summary>
        public PlesseySymbology()
            : base(TrueSymbologyType.Plessey)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlesseySymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public PlesseySymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.Plessey)
        {
        }

        /// <summary>
        /// Validates the value using Plessey Code symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
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
            return "Plessey Code symbology allows only hexadecimal number values to be encoded.";
        }

        /// <summary>
        /// Gets the barcode value encoded using Plessey Code symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            StringBuilder sb = new StringBuilder();

            if (forCaption)
            {
                sb.Append(Value);
            }
            else
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    sb.Append(getCharPattern(Value[i]));
                }
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
                    return "0000";
                case '1':
                    return "1000";
                case '2':
                    return "0100";
                case '3':
                    return "1100";
                case '4':
                    return "0010";
                case '5':
                    return "1010";
                case '6':
                    return "0110";
                case '7':
                    return "1110";
                case '8':
                    return "0001";
                case '9':
                    return "1001";
                case 'A':
                    return "0101";
                case 'B':
                    return "1101";
                case 'C':
                    return "0011";
                case 'D':
                    return "1011";
                case 'E':
                    return "0111";
                case 'F':
                    return "1111";
            }

            return "0000";
        }

        /// <summary>
        /// Gets the checksum.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The checksum.</returns>
        protected string getChecksum(string value)
        {
            // CRC 8, Poly E9 (0x1E9)

            if (value.Length % 8 != 0)
                value = "0000" + value;
            int length = value.Length / 8;

            byte[] msg = new byte[length];
            for (int i = 0; i < length; i++)
            {
                msg[i] = Convert.ToByte(value.Substring(i * 8, 8), 2);
            }
            byte checksum = crc.Checksum(msg);
            string checksum_bin = Convert.ToString(checksum, 2);
            while (checksum_bin.Length < 8)
                checksum_bin = checksum_bin.Insert(0, "0");

            return checksum_bin;
        }

        protected override Size buildBars(SKCanvas canvas, SKFont font)
        {
            Size drawingSize = new Size();
            int x = 0;
            int y = 0;

            string label = GetEncodedValue(false);
            if (label.Length % 4 != 0)
                throw new BarcodeException("Incorrect value length.");
            string value = m_forwardStartCode + label + getChecksum(label);

            int width0 = NarrowBarWidth;
            int pitchWidth = width0 * 5;
            int width1 = (int)Math.Round(pitchWidth * 0.54);
            int height = BarHeight;

            // left margin
            x += width0;
            // Forward start code + Label + Check code
            for (int i = 0; i < value.Length; i++)
            {
                int barWidth = width0;
                if (value[i] == '1')
                    barWidth = width1;
                m_rects.Add(new Rectangle(x, y, barWidth, height));
                x += pitchWidth;
            }
            // Termination bar
            m_rects.Add(new Rectangle(x, y, pitchWidth, height));
            x += pitchWidth;
            // Reverse start code
            for (int i = 0; i < m_reverseStartCode.Length; i++)
            {
                int barWidth = width0;
                if (m_reverseStartCode[i] == '1')
                    barWidth = width1;
                x += pitchWidth;
                m_rects.Add(new Rectangle(x - barWidth, y, barWidth, height));
            }
            // right margin
            x += width0;

            drawingSize.Width = x;
            drawingSize.Height = height;
            return drawingSize;
        }
    }
}

