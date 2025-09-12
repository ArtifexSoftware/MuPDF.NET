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
    /// Draws barcodes using MSI symbology rules.
    /// This symbology is used primarily for inventory control,
    /// marking storage containers and shelves in warehouse environments.
    /// </summary>
    /// <remarks>See also:
    /// http://en.wikipedia.org/wiki/MSI_Barcode
    /// http://www.crifan.com/files/doc/docbook/symbology_plessey/release/html/symbology_plessey.html
    /// </remarks>
    class MSISymbology : SymbologyDrawing
    {
        protected static string m_alphabet = "0123456789";
        protected static string m_startCode = "110";
        protected static string m_stopCode = "1001";

        /// <summary>
        /// Initializes a new instance of the <see cref="MSISymbology"/> class.
        /// </summary>
        public MSISymbology()
            : base(TrueSymbologyType.Plessey)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MSISymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public MSISymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.Plessey)
        {
        }

        /// <summary>
        /// Validates the value using MSI symbology rules.
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
            return "MSI symbology allows only numeric values to be encoded.";
        }

        /// <summary>
        /// Gets the barcode value encoded using MSI symbology rules.
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
                sb.Append(Value);
                sb.Append(getChecksum(Value));
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
                    return "100100100100";
                case '1':
                    return "100100100110";
                case '2':
                    return "100100110100";
                case '3':
                    return "100100110110";
                case '4':
                    return "100110100100";
                case '5':
                    return "100110100110";
                case '6':
                    return "100110110100";
                case '7':
                    return "100110110110";
                case '8':
                    return "110100100100";
                case '9':
                    return "110100100110";
            }
            return "";
        }

        protected string getModulo10CheckDigit(string value)
        {
            // Luhn algorithm (http://en.wikipedia.org/wiki/Luhn_algorithm)
            int sum = 0;
            for (int i = 0; i < value.Length; i++)
            {
                byte d = byte.Parse(value[i].ToString());
                if ((value.Length - i) % 2 != 0) // Digits are numbered from right to left
                {
                    if (d > 4)
                        sum += d * 2 - 9;
                    else
                        sum += d * 2;
                }
                else
                    sum += d;
            }
            int checkDigit = 10 - sum % 10;
            return checkDigit.ToString();
        }

        protected string getModulo11CheckDigit(string value)
        {
            int sum = 0;
            int weight = 2;
            for (int i = value.Length-1; i > -1; i--)
            {
                sum += byte.Parse(value[i].ToString()) * weight;
                weight++;
                // The IBM algorithm is uses 2..7, the NCR algorithm is uses 2..9
                if (weight > 7)
                    weight = 2;
            }

            int checkDigit = 11 - sum % 11;
            return checkDigit.ToString();
        }

        /// <summary>
        /// Gets the checksum.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The checksum.</returns>
        protected string getChecksum(string value)
        {
            switch (Options.MSIChecksumAlgorithm)
            {
                case MSIChecksumAlgorithm.Modulo10:
                    return getModulo10CheckDigit(value);
                case MSIChecksumAlgorithm.Modulo11:
                    return getModulo11CheckDigit(value);
                case MSIChecksumAlgorithm.Modulo1010:
                    string checkDigit10 = getModulo10CheckDigit(value);
                    return checkDigit10 + getModulo10CheckDigit(value + checkDigit10);
                case MSIChecksumAlgorithm.Modulo1110:
                    string checkDigit11 = getModulo11CheckDigit(value);
                    return checkDigit11 + getModulo10CheckDigit(value + checkDigit11);
                case MSIChecksumAlgorithm.NoCheckDigit:
                default:
                    return string.Empty;
            }
        }

        protected override Size buildBars(SKCanvas canvas, SKFont font)
        {
            Size drawingSize = new Size();
            int x = 0;
            int y = 0;

            string label = GetEncodedValue(false);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < label.Length; i++)
            {
                sb.Append(getCharPattern(label[i]));
            }
            string value = m_startCode + sb.ToString() + m_stopCode;

            int width = NarrowBarWidth;
            int height = BarHeight;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '1')
                    m_rects.Add(new Rectangle(x, y, width, height));
                x += width;
            }

            drawingSize.Width = x;
            drawingSize.Height = height;
            return drawingSize;
        }
    }
}
