using System;
using System.Text;
using System.Drawing;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using EAN-5 symbology rules.
    /// For more, see: http://en.wikipedia.org/wiki/EAN_5
    /// </summary>
    class EAN5Symbology : SymbologyDrawing
    {
        private static string m_start = "01011";
        private static string m_gap = "01";
        private static string m_alphabet = "0123456789";

        private static string[] m_lPatterns = { 
            "0001101", "0011001", "0010011", "0111101", "0100011",
            "0110001", "0101111", "0111011", "0110111", "0001011"
        };

        private static string[] m_gPatterns = { 
            "0100111", "0110011", "0011011", "0100001", "0011101",
            "0111001", "0000101", "0010001", "0001001", "0010111"
        };

        private static string[] m_structures = {
            "GGLLL", "GLGLL", "GLLGL", "GLLLG", "LGGLL", 
            "LLGGL", "LLLGG", "LGLGL", "LGLLG", "LLGLG"
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="EAN5Symbology"/> class.
        /// </summary>
        public EAN5Symbology()
            : base(TrueSymbologyType.EAN2)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EAN5Symbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public EAN5Symbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.EAN5)
        {
        }

        /// <summary>
        /// Gets or sets the barcode caption position.
        /// </summary>
        /// <value>The barcode caption position.</value>
        public override CaptionPosition CaptionPosition
        {
            get
            {
                // EAN5 always draw caption above the barcode
                return CaptionPosition.Above;
            }
            set
            {
                base.CaptionPosition = value;
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
        /// Validates the value using EAN-2 symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length != 5)
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
            return "EAN-5 symbology allows only strings with exactly five numbers to be encoded.";
        }

        /// <summary>
        /// Gets the barcode value encoded using EAN-5 symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            if (forCaption && Options.EANDrawQuietZoneIndicator)
                return Value + ">";

            return Value;
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

        protected override Size buildBars(SKCanvas canvas, SKFont font)
        {
            Size drawingSize = new Size();
            int x = 0;
            int y = 0;

            string value = GetEncodedValue(false);
            string pattern = valueToPattern(value);

            foreach (char patternChar in pattern)
            {
                bool drawBar = false;
                if (patternChar == '1')
                    drawBar = true;

                if (drawBar)
                    m_rects.Add(new Rectangle(x, y, NarrowBarWidth, BarHeight));

                x += NarrowBarWidth;
            }

            drawingSize.Width = x;
            drawingSize.Height = BarHeight;
            return drawingSize;
        }

        private static string valueToPattern(string value)
        {
            string structure = getStructure(value);

            StringBuilder sb = new StringBuilder();
            sb.Append(m_start);

            for (int i = 0; i < structure.Length; i++)
            {
                int digit = (int)(value[i] - '0');
                if (structure[i] == 'L')
                    sb.Append(getLPattern(digit));
                else
                    sb.Append(getGPattern(digit));

                if (i != (structure.Length - 1))
                    sb.Append(m_gap);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the structure to use for barcode generation.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <returns>The structure to use for barcode generation.</returns>
        private static string getStructure(string value)
        {
            int checksum = getChecksum(value);
            return m_structures[checksum];
        }

        /// <summary>
        /// Calculates the EAN-5 checksum for a given value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The EAN-5 checksum for a given value.</returns>
        private static int getChecksum(string value)
        {
            int sum = 0;
            for (int i = 0; i < value.Length; i++)
            {
                int digit = (int)(value[i] - '0');
                if (i % 2 == 0)
                    sum += digit * 3;
                else
                    sum += digit * 9;
            }

            return sum % 10;
        }

        /// <summary>
        /// Gets the encoding pattern for given number using L table.
        /// </summary>
        /// <param name="n">The number to retrieve pattern for.</param>
        /// <returns>
        /// The encoding pattern for given number.
        /// </returns>
        private static string getLPattern(int n)
        {
            return m_lPatterns[n];
        }

        /// <summary>
        /// Gets the encoding pattern for given number using L table.
        /// </summary>
        /// <param name="n">The number to retrieve pattern for.</param>
        /// <returns>
        /// The encoding pattern for given number.
        /// </returns>
        private static string getGPattern(int n)
        {
            return m_gPatterns[n];
        }        
    }
}
