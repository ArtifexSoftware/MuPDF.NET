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
    /// Code 128 is a very effective, high-density symbology which permits 
    /// the encoding of alphanumeric data. Code 128 is a very dense code, 
    /// used extensively worldwide.
    /// </summary>
    class Code128Symbology : SymbologyDrawing
    {
        protected const char m_fnc1 = 'à';
        protected static char m_fnc2 = 'á';
        protected static char m_fnc3 = 'â';
        protected static char m_fnc4 = 'ã';

        protected const char m_shift = 'ä';

        protected const char m_codeA = 'å';
        protected const char m_codeB = '¸';
        protected const char m_codeC = 'æ';

        protected const char m_startA = 'è';
        protected const char m_startB = 'ê';
        protected const char m_startC = 'ë';

        protected string m_numbers = "0123456789";
        protected string m_lowerCase = "abcdefghijklmnopqrstuvwxyz";

        protected char[] m_aSpecials = { m_fnc3, m_fnc2, m_shift, m_codeC, m_codeB, m_fnc4, m_fnc1, m_startA, m_startB, m_startC };
        protected char[] m_bSpecials = { m_fnc3, m_fnc2, m_shift, m_codeC, m_fnc4, m_codeA, m_fnc1, m_startA, m_startB, m_startC };
        protected char[] m_cSpecials = { m_codeB, m_codeA, m_fnc1, m_startA, m_startB, m_startC };

        /// <summary>
        /// Initializes a new instance of the <see cref="Code128Symbology"/> class.
        /// </summary>
        public Code128Symbology()
            : base(TrueSymbologyType.Code128)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Code128Symbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public Code128Symbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.Code128)
        {
        }

        /// <summary>
        /// Validates the value using Code 128 symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            switch (getUsedAlphabet())
            {
                case Code128Alphabet.A:
                    return aValueIsValid(value);

                case Code128Alphabet.B:
                    return bValueIsValid(value);

                case Code128Alphabet.C:
                    return cValueIsValid(value);

                case Code128Alphabet.Auto:
                default:
                    return autoValueIsValid(value);
            }
        }

        protected virtual Code128Alphabet getUsedAlphabet()
        {
            return Options.Code128Alphabet;
        }

        /// <summary>
        /// Validates the value using Code 128 Alphabet A symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
        private static bool aValueIsValid(string value)
        {
            // Alphabet A allows only character from NUL (ASCII 0) to 
            // '_' (ASCII 95) to be encoded.

            foreach (int c in value)
            {
                if (c > 95)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the value using Code 128 Alphabet B symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
        private static bool bValueIsValid(string value)
        {
            // Alphabet B allows only character from SPACE (ASCII 32) to 
            // DEL (ASCII 127) to be encoded.

            foreach (int c in value)
            {
                if (c > 127 || c < 32)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the value using Code 128 Alphabet C symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
        private bool cValueIsValid(string value)
        {
            // Alphabet C allows only numeric values with even length

            if (value.Length % 2 != 0)
                return false;

            foreach (char c in value)
            {
                if (m_numbers.IndexOf(c) == -1)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the value using Code 128 with auto alphabet selection symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
        protected bool autoValueIsValid(string value)
        {
            return aValueIsValid(value) || bValueIsValid(value) || cValueIsValid(value);
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>The value restrictions description string.</returns>
        public override string getValueRestrictions()
        {
            switch (getUsedAlphabet())
            {
                case Code128Alphabet.A:
                    return "Code 128 Alphabet A allows only character from NUL (ASCII 0) to '_' (ASCII 95) to be encoded.";

                case Code128Alphabet.C:
                    return "Code 128 Alphabet C allows only numeric values with even length.";

                case Code128Alphabet.B:
                    return "Code 128 Alphabet B allows only character from SPACE (ASCII 32) to DEL (ASCII 127) to be encoded.";

                case Code128Alphabet.Auto:
                default:
                    return "Code 128 with auto alphabet selection allows at most first 128 ASCII symbols to be encoded.";
            }
        }

        /// <summary>
        /// Gets the barcode value encoded using Code 128 symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using Code 128 symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            StringBuilder sb = new StringBuilder();

            if (!forCaption && getUsedAlphabet() != Code128Alphabet.Auto)
                sb.Append(getStartChar());

            if (forCaption || getUsedAlphabet() != Code128Alphabet.Auto)
                sb.Append(Value);
            else
            {
                // we need to encode value using auto alphabet selection
                sb.Append(autoEncodeValue(Value));
            }

            // checksum chars are always added to encoded value 
            // (and NEVER to caption)

            return sb.ToString();
        }

        /// <summary>
        /// Gets the encoding pattern for given character.
        /// </summary>
        /// <param name="c">The character to retrieve pattern for.</param>
        /// <returns>The encoding pattern for given character.</returns>
        protected override string getCharPattern(char c)
        {
            return null;
        }

        /// <summary>
        /// Gets the start char for the given alphabet.
        /// </summary>
        /// <returns></returns>
        protected char getStartChar()
        {
            switch (getUsedAlphabet())
            {
                case Code128Alphabet.A:
                    return m_startA;

                case Code128Alphabet.B:
                    return m_startB;

                case Code128Alphabet.C:
                    return m_startC;

                default:
                    throw new BarcodeException("Incorrect alphabet");
            }
        }

        /// <summary>
        /// Encodes current value using auto alphabet selection.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <returns>Current value encoded using auto alphabet selection.</returns>
        protected string autoEncodeValue(string value)
        {
            StringBuilder sb = new StringBuilder();

            Code128Alphabet alphabet = selectAlphabet(value, 0);
            if (alphabet == Code128Alphabet.C)
                sb.Append(m_startC);
            else if (alphabet == Code128Alphabet.A)
                sb.Append(m_startA);
            else
                sb.Append(m_startB);

            int pos = 0;
            while (pos < value.Length)
            {
                if (alphabet == Code128Alphabet.C)
                {
                    int numberCount = getNumberCount(value, pos);
                    int skipCharCount = numberCount - numberCount % 2;
                    for (int i = 0; i < skipCharCount; i++)
                    {
                        sb.Append(value[pos]);
                        pos++;
                    }

                    if (pos != value.Length)
                    {
                        alphabet = selectAlphabet(value, pos);
                        if (alphabet == Code128Alphabet.A)
                            sb.Append(m_codeA);
                        else
                            sb.Append(m_codeB);
                    }
                }
                else if ((alphabet == Code128Alphabet.A || alphabet == Code128Alphabet.B) && getNumberCount(value, pos) >= 4)
                {
                    int numberCount = getNumberCount(value, pos);
                    if (numberCount % 2 == 1)
                    {
                        sb.Append(value[pos]);
                        pos++;
                    }

                    sb.Append(m_codeC);
                    alphabet = Code128Alphabet.C;
                }
                else if ((alphabet == Code128Alphabet.B) && isControlCharacter(value[pos]))
                {
                    if ((pos < value.Length - 2) && isLowerCaseCharacter(value[pos + 1]) && isControlCharacter(value[pos + 2]))
                    {
                        sb.Append(m_shift);
                        sb.Append(value[pos]);
                        pos++;
                    }
                    else
                    {
                        sb.Append(m_codeA);
                        alphabet = Code128Alphabet.A;
                    }
                }
                else if (alphabet == Code128Alphabet.A && isLowerCaseCharacter(value[pos]))
                {
                    if ((pos < value.Length - 2) && isControlCharacter(value[pos + 1]) && isLowerCaseCharacter(value[pos + 2]))
                    {
                        sb.Append(m_shift);
                        sb.Append(value[pos]);
                        pos++;
                    }
                    else
                    {
                        sb.Append(m_codeB);
                        alphabet = Code128Alphabet.B;
                    }
                }
                else
                {
                    sb.Append(value[pos]);
                    pos++;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Selects the alphabet to encode value from the given position.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="pos">The start position.</param>
        /// <returns>The alphabet to encode value from the given position.</returns>
        protected virtual Code128Alphabet selectAlphabet(string value, int pos)
        {
            if (getNumberCount(value, pos) >= 4)
                return Code128Alphabet.C;
            else if (getFirstControlCharPos(value, pos) < getFirstLowerCasePos(value, pos))
                return Code128Alphabet.A;
            else
                return Code128Alphabet.B;
        }

        /// <summary>
        /// Gets the length of consecutively going numbers.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startPos">The start position.</param>
        /// <returns>The length of consecutively going numbers.</returns>
        protected int getNumberCount(string value, int startPos)
        {
            int numberCount = 0;

            for (int i = startPos; i < value.Length; i++)
            {
                if (m_numbers.IndexOf(value[i]) == -1)
                    break;
                else
                    numberCount++;
            }

            return numberCount;
        }

        /// <summary>
        /// Gets the first lower case character posistion.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startPos">The start position.</param>
        /// <returns>The first lower case character posistion.</returns>
        protected int getFirstLowerCasePos(string value, int startPos)
        {
            for (int i = startPos; i < value.Length; i++)
            {
                if (isLowerCaseCharacter(value[i]))
                    return i;
            }

            return value.Length;
        }

        /// <summary>
        /// Gets the first control character position.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startPos">The start position.</param>
        /// <returns>The first control character position.</returns>
        protected static int getFirstControlCharPos(string value, int startPos)
        {
            for (int i = startPos; i < value.Length; i++)
            {
                if (isControlCharacter(value[i]))
                    return i;
            }

            return value.Length;
        }

        /// <summary>
        /// Determines whether given character is a control character.
        /// </summary>
        /// <param name="c">The character to test.</param>
        /// <returns>
        /// 	<c>true</c> if given character is a control character; otherwise, <c>false</c>.
        /// </returns>
        protected static bool isControlCharacter(char c)
        {
            if (c >= '\0' && c < ' ')
                return true;

            return false;
        }

        /// <summary>
        /// Determines whether given character is a lower case character.
        /// </summary>
        /// <param name="c">The character to test.</param>
        /// <returns>
        /// 	<c>true</c> if given character is a lower case character; otherwise, <c>false</c>.
        /// </returns>
        protected bool isLowerCaseCharacter(char c)
        {
            if (m_lowerCase.IndexOf(c) != -1)
                return true;

            return false;
        }

        /// <summary>
        /// Calculates the modulo 103 checksum.
        /// </summary>
        /// <param name="values">The value to calculate checksum for.</param>
        /// <returns>The checksum.</returns>
        private static int calculateChecksum(int[] values)
        {
            int total = values[0];
            for (int i = 1; i < values.Length; i++)
                total += values[i] * i;

            return total % 103;
        }

        /// <summary>
        /// Gets the character values array for the given string.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>The character values array for the given string.</returns>
        protected virtual int[] getValues(string value)
        {
            // number of values is less or equal to number of characters in source string
            int[] intialValues = new int[value.Length];

            Code128Alphabet alphabet = Code128Alphabet.Auto;
            switch (value[0])
            {
                case m_startA:
                    alphabet = Code128Alphabet.A;
                    break;

                case m_startB:
                default:
                    alphabet = Code128Alphabet.B;
                    break;

                case m_startC:
                    alphabet = Code128Alphabet.C;
                    break;
            }

            intialValues[0] = getCharValue(value[0], alphabet);
            bool shiftOccured = false;

            int valuesAdded = 1;
            for (int i = valuesAdded; i < value.Length; i++)
            {
                bool special = false;
                switch (value[i])
                {
                    case m_codeA:
                        special = true;
                        intialValues[valuesAdded] = getCharValue(value[i], alphabet);
                        alphabet = Code128Alphabet.A;
                        break;

                    case m_codeB:
                        special = true;
                        intialValues[valuesAdded] = getCharValue(value[i], alphabet);
                        alphabet = Code128Alphabet.B;
                        break;

                    case m_codeC:
                        special = true;
                        intialValues[valuesAdded] = getCharValue(value[i], alphabet);
                        alphabet = Code128Alphabet.C;
                        break;

                    case m_shift:
                        special = true;
                        intialValues[valuesAdded] = getCharValue(value[i], alphabet);
                        if (alphabet == Code128Alphabet.A)
                        {
                            alphabet = Code128Alphabet.B;
                            shiftOccured = true;
                        }
                        else
                        {
                            alphabet = Code128Alphabet.A;
                            shiftOccured = true;
                        }
                        break;
                }

                if (!special)
                {
                    if (alphabet == Code128Alphabet.C)
                    {
                        string s = value.Substring(i, 2);
                        try
                        {
                            intialValues[valuesAdded] = Convert.ToInt16(s);
                            i++;
                        }
                        catch (FormatException)
                        {
                            char c = Char.IsDigit(s[0]) ? s[1] : s[0];
                            string message = String.Format("Incorrect character '{0}' for C alphabet of Code 128 symbology", c);
                            throw new BarcodeException(message);
                        }
                    }
                    else
                        intialValues[valuesAdded] = getCharValue(value[i], alphabet);

                    if (shiftOccured)
                    {
                        if (alphabet == Code128Alphabet.A)
                            alphabet = Code128Alphabet.B;
                        else
                            alphabet = Code128Alphabet.A;

                        shiftOccured = false;
                    }
                }

                valuesAdded++;
            }

            if (valuesAdded != value.Length)
            {
                int[] values = new int[valuesAdded];
                for (int i = 0; i < valuesAdded; i++)
                    values[i] = intialValues[i];

                return values;
            }

            return intialValues;
        }

        /// <summary>
        /// Gets the character value for the given alphabet.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <param name="alphabet">The alphabet.</param>
        /// <returns>The character value for the given alphabet.</returns>
        protected int getCharValue(char c, Code128Alphabet alphabet)
        {
            int value = 0;

            if (alphabet == Code128Alphabet.A)
            {
                if (c >= ' ' && c <= '_')
                    value = (int)c - 32;
                else if (c >= '\0' && c < ' ')
                    value = (int)c + 64;
                else
                {
                    for (int i = 0; i < m_aSpecials.Length; i++)
                    {
                        if (m_aSpecials[i] == c)
                        {
                            value += 96 + i;
                            break;
                        }
                    }
                }
            }
            else if (alphabet == Code128Alphabet.B)
            {
                if (c >= ' ' && (int)c <= 127)
                    value = (int)c - 32;
                else
                {
                    for (int i = 0; i < m_bSpecials.Length; i++)
                    {
                        if (m_bSpecials[i] == c)
                        {
                            value += 96 + i;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_cSpecials.Length; i++)
                {
                    if (m_cSpecials[i] == c)
                    {
                        value += 100 + i;
                        break;
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// Gets the encoding pattern for given value.
        /// </summary>
        /// <param name="value">The value to get encoding pattern for.</param>
        /// <returns>
        /// The encoding pattern for given value.
        /// </returns>
        private string getValuePattern(int value)
        {
            return m_patterns[value];
        }

        protected override SKSize buildBars(SKCanvas canvas, SKFont font)
        {
            SKSize drawingSize = new SKSize();
            int x = 0;
            int y = 0;

            string encoded = GetEncodedValue(false);
            int[] values = getValues(encoded);
            int checksumCharValue = calculateChecksum(values);

            foreach (int i in values)
            {
                string pattern = getValuePattern(i);
                x = drawPattern(pattern, x, y, NarrowBarWidth, BarHeight);
            }

            string checksum = getValuePattern(checksumCharValue);
            x = drawPattern(checksum, x, y, NarrowBarWidth, BarHeight);

            x = drawPattern(m_stopPattern, x, y, NarrowBarWidth, BarHeight);

            // draw termination bar
            m_rects.Add(new SKRect(x, y, NarrowBarWidth * 2+x, BarHeight+y));

            x += NarrowBarWidth * 2;

            drawingSize.Width = x;
            drawingSize.Height = BarHeight;
            return drawingSize;
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
        /// The new X position (start X position + width of the
        /// rectangle occupied by pattern.
        /// </returns>
        private int drawPattern(string pattern, int x, int y, int width, int height)
        {
            foreach (char patternChar in pattern)
            {
                bool drawBar = (patternChar == '1');
                if (drawBar)
                    m_rects.Add(new SKRect(x, y, width+x, height+y));

                x += width;
            }

            return x;
        }


        //////////////////////////////////////////////////////////////////////////
        //
        // Patterns part
        //
        //////////////////////////////////////////////////////////////////////////

        private string[] m_patterns = { 
            "11011001100", "11001101100", "11001100110", "10010011000",
            "10010001100", "10001001100", "10011001000", "10011000100",
            "10001100100", "11001001000", "11001000100", "11000100100",
            "10110011100", "10011011100", "10011001110", "10111001100",
            "10011101100", "10011100110", "11001110010", "11001011100",
            "11001001110", "11011100100", "11001110100", "11101101110",
            "11101001100", "11100101100", "11100100110", "11101100100",
            "11100110100", "11100110010", "11011011000", "11011000110",
            "11000110110", "10100011000", "10001011000", "10001000110",
            "10110001000", "10001101000", "10001100010", "11010001000",
            "11000101000", "11000100010", "10110111000", "10110001110",
            "10001101110", "10111011000", "10111000110", "10001110110",
            "11101110110", "11010001110", "11000101110", "11011101000",
            "11011100010", "11011101110", "11101011000", "11101000110",
            "11100010110", "11101101000", "11101100010", "11100011010",
            "11101111010", "11001000010", "11110001010", "10100110000",
            "10100001100", "10010110000", "10010000110", "10000101100",
            "10000100110", "10110010000", "10110000100", "10011010000",
            "10011000010", "10000110100", "10000110010", "11000010010",
            "11001010000", "11110111010", "11000010100", "10001111010",
            "10100111100", "10010111100", "10010011110", "10111100100",
            "10011110100", "10011110010", "11110100100", "11110010100",
            "11110010010", "11011011110", "11011110110", "11110110110",
            "10101111000", "10100011110", "10001011110", "10111101000",
            "10111100010", "11110101000", "11110100010", "10111011110",
            "10111101110", "11101011110", "11110101110", "11010000100",
            "11010010000", "11010011100"
        };

        private string m_stopPattern = "11000111010";
    }
}
