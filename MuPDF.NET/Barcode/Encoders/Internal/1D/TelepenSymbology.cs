using System;
using System.Text;
using System.Collections;
using System.Drawing;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using Telepen symbology.
    /// This symbology is used in many countries and
    /// very widely in the UK. Most Universities and other academic libraries use
    /// Telepen, as do many public libraries. Other users include the motor
    /// industry, Ministry of Defence and innumerable well-known organisations
    /// for many different applications.
    /// </summary>
    /// http://www.codeproject.com/KB/graphics/BarcodeLibrary.aspx
    class TelepenSymbology : SymbologyDrawing
    {
        private const int START1 = 128;
        private const int STOP1 = 129;
        
        private const int START2 = 130;
        private const int STOP2 = 131;
        
        private const int START3 = 132;
        private const int STOP3 = 133;

        private const int MAX_CONTROL_CHAR = 31; // max control char so we allow to encode as [charid] control characters from 0 to 31 only

        private static string[] m_charPatterns = new string[]
        {
            "1110111011101110", "1011101110111010", "1110001110111010", 
            "1010111011101110", "1110101110111010", "1011100011101110", 
            "1000100011101110", "1010101110111010", "1110111000111010", 
            "1011101011101110", "1110001011101110", "1010111000111010", 
            "1110101011101110", "1010001000111010", "1000101000111010", 
            "1010101011101110", "1110111010111010", "1011101110001110", 
            "1110001110001110", "1010111010111010", "1110101110001110", 
            "1011100010111010", "1000100010111010", "1010101110001110", 
            "1110100010001110", "1011101010111010", "1110001010111010", 
            "1010100010001110", "1110101010111010", "1010001010001110", 
            "1000101010001110", "1010101010111010", "1110111011100010", 
            "1011101110101110", "1110001110101110", "1010111011100010", 
            "1110101110101110", "1011100011100010", "1000100011100010", 
            "1010101110101110", "1110111000101110", "1011101011100010", 
            "1110001011100010", "1010111000101110", "1110101011100010", 
            "1010001000101110", "1000101000101110", "1010101011100010", 
            "1110111010101110", "1011101000100010", "1110001000100010", 
            "1010111010101110", "1110101000100010", "1011100010101110", 
            "1000100010101110", "1010101000100010", "1110100010100010", 
            "1011101010101110", "1110001010101110", "1010100010100010", 
            "1110101010101110", "1010001010100010", "1000101010100010", 
            "1010101010101110", "1110111011101010", "1011101110111000", 
            "1110001110111000", "1010111011101010", "1110101110111000", 
            "1011100011101010", "1000100011101010", "1010101110111000", 
            "1110111000111000", "1011101011101010", "1110001011101010", 
            "1010111000111000", "1110101011101010", "1010001000111000", 
            "1000101000111000", "1010101011101010", "1110111010111000", 
            "1011101110001010", "1110001110001010", "1010111010111000", 
            "1110101110001010", "1011100010111000", "1000100010111000", 
            "1010101110001010", "1110100010001010", "1011101010111000", 
            "1110001010111000", "1010100010001010", "1110101010111000", 
            "1010001010001010", "1000101010001010", "1010101010111000", 
            "1110111010001000", "1011101110101010", "1110001110101010", 
            "1010111010001000", "1110101110101010", "1011100010001000", 
            "1000100010001000", "1010101110101010", "1110111000101010", 
            "1011101010001000", "1110001010001000", "1010111000101010", 
            "1110101010001000", "1010001000101010", "1000101000101010", 
            "1010101010001000", "1110111010101010", "1011101000101000", 
            "1110001000101000", "1010111010101010", "1110101000101000", 
            "1011100010101010", "1000100010101010", "1010101000101000", 
            "1110100010101000", "1011101010101010", "1110001010101010", 
            "1010100010101000", "1110101010101010", "1010001010101000", 
            "1000101010101000", "1010101010101010",  // 127
            "1010101010111000",  // START1
            "1110001010101010",  // STOP1
            "1010101011101000",  // START2
            "1110100010101010",  // STOP2
            "1010101110101000",  // START3
            "1110101000101010"   // STOP3
        };

        private int m_startCode;
        private int m_stopCode;
        private int m_switchModeCharPos;
        private bool m_isOnlyNumeric = false;
        private string m_trailingControlChars = ""; // holds string with trailing control characters if any

        /// <summary>
        /// Initializes a new instance of the <see cref="TelepenSymbology"/> class.
        /// </summary>
        public TelepenSymbology()
            : base(TrueSymbologyType.Telepen)
        {
        }

        protected virtual TelepenAlphabet getUsedAlphabet()
        {
            return Options.TelepenAlphabet;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="TelepenSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public TelepenSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.Telepen)
        {
        }

        /// <summary>
        /// Validates the value using Telepen symbology rules.
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
                byte b = (byte)c;
                if (b > 127)
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
            return "Telepen symbology allows only ASCII chars to be encoded.";
        }

        /// <summary>
        /// Gets the barcode value encoded using Telepen symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            if (forCaption)
                return Value;

            return encodeValue();
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
            if (c >= 0 && c <= 127)
                return m_charPatterns[(int)c];

            throw new BarcodeException("Incorrect symbol for Telepen symbology");
        }

        protected override Size buildBars(SKCanvas canvas, SKFont font)
        {
            Size drawingSize = new Size();
            int x = 0;
            int y = 0;

            string value = GetEncodedValue(false);
            foreach (char c in value)
            {
                if (c == '1')
                    m_rects.Add(new Rectangle(x, y, NarrowBarWidth, BarHeight));

                x += NarrowBarWidth;
            }

            drawingSize.Width = x;
            drawingSize.Height = BarHeight;
            return drawingSize;
        }

        private string encodeValue()
        {
            string bcValue = "";
            
            bcValue = Value;
            // replace all [9] and similar to control character codes
            //bcValue = preProcessControlCharacters(Value);
            // support for codes in square brackets has been disabled as we can pass control characters inside string if needed

            // cut out leading control character codes if any
            bcValue = cutTrailingControlCharacters(bcValue);

            // analyze the value and determine how we should encode the value and which modes should use
            planEncodingSequence(bcValue);

            StringBuilder sb = new StringBuilder();
            //sb.Append(m_charPatterns[m_startCode]);
            sb.Append(m_charPatterns[START1]);

            int checkSum = 0;

            // encode trailing control characters if any
            if (m_trailingControlChars.Length > 0)
            {
                encodeASCII(m_trailingControlChars, sb, ref checkSum);
            }


            switch (m_startCode)
            {
                case START2:
                    // numeric --> ascii

                    // adding DLE character to indicate we start numeric mode
                    if (!m_isOnlyNumeric)
                    {
                        // we insert switch only if we have numbers and ascii mixed
                    //    encodeSwitchMode(sb, ref checkSum);
                    }

                    encodeNumeric(bcValue.Substring(0, m_switchModeCharPos), sb, ref checkSum);

                    // if we have ASCII remaining then add switch character and add and encode ascii data 
                    if (m_switchModeCharPos < bcValue.Length)
                    {
                        encodeSwitchMode(sb, ref checkSum);
                        encodeASCII(bcValue.Substring(m_switchModeCharPos), sb, ref checkSum);
                    }
                    break;

                case START3:
                    //ascii --> numeric

                    encodeASCII(bcValue.Substring(0, m_switchModeCharPos), sb, ref checkSum);
                    // insert DLE to indicate we switch to numeric value
                    encodeSwitchMode(sb, ref checkSum);
                    encodeNumeric(bcValue.Substring(m_switchModeCharPos), sb, ref checkSum);
                    break;
                
                default:
                    //full ascii
                    encodeASCII(bcValue, sb, ref checkSum);
                    break;
            }

            checkSum = 127 - (checkSum % 127);
            sb.Append(m_charPatterns[checkSum]);

            m_stopCode = STOP1;
            sb.Append(m_charPatterns[m_stopCode]);

            return sb.ToString();
        }

        private void planEncodingSequence(string Value)
        {
            // use full ASCII by default
            m_startCode = START1;
            m_stopCode = STOP1;

            m_switchModeCharPos = Value.Length;

            // we assume we have auto mode, we will check to know if we have numeric only mode or ascii or mixed (auto)
            m_isOnlyNumeric = false;

            if (Options.TelepenAlphabet == TelepenAlphabet.ASCII) { 
                // do nothing as full ASCII is set by default           
            }
            else if (Options.TelepenAlphabet == TelepenAlphabet.Numeric){
                // Numeric only mode due to only numbers being present
                m_startCode = START2;
                m_stopCode = STOP2;

                // If the data consists of an odd number of digits 
                // and it is not acceptable to add a leading zero, insert a DLE character before the last digit and 
                // encode that as a normal ASCII character. 
                if ((Value.Length % 2) > 0)
                    m_switchModeCharPos = Value.Length - 1;

                m_isOnlyNumeric = CheckIfNumericValuesOnly(Value);

                if (Value.Length> 0 && !m_isOnlyNumeric)
                {
                    throw new BarcodeException("You can use numeric values (0..9) while using TelepenAlphabet.Numeric alphabet for Telepen symbology");
                }


            }
            else if (Options.TelepenAlphabet == TelepenAlphabet.Auto)
            {

                int leadingNumerics = 0;
                foreach (char c in Value)
                {
                    if (!Char.IsNumber(c))
                        break;

                    leadingNumerics++;
                }                

                if (leadingNumerics == Value.Length)
                {
                    m_isOnlyNumeric = true; // we have numbers only

                    // Numeric only mode due to only numbers being present
                    m_startCode = START2;
                    m_stopCode = STOP2;

                    // If the data consists of an odd number of digits 
                    // and it is not acceptable to add a leading zero, insert a DLE character before the last digit and 
                    // encode that as a normal ASCII character. 
                    if ((Value.Length % 2) > 0)
                        m_switchModeCharPos = Value.Length - 1;
                }
                else
                {
                    // there not only numbers in value

                    int trailingNumerics = 0;
                    for (int i = Value.Length - 1; i >= 0; i--)
                    {
                        if (!Char.IsNumber(Value[i]))
                            break;

                        trailingNumerics++;
                    }

                    if (
                        (leadingNumerics >= 4 || trailingNumerics >= 4)
                       )
                    {
                        // hybrid mode will be used
                        if (leadingNumerics > trailingNumerics)
                        {
                            // start in numeric switching to ascii
                            m_startCode = START2;
                            m_stopCode = STOP2;

                            if ((leadingNumerics % 2) == 1)
                                m_switchModeCharPos = leadingNumerics - 1;
                            else
                                m_switchModeCharPos = leadingNumerics;
                        }
                        else
                        {
                            //start in ascii switching to numeric
                            m_startCode = START3;
                            m_stopCode = STOP3;

                            if ((trailingNumerics % 2) == 1)
                                m_switchModeCharPos = Value.Length - trailingNumerics + 1;
                            else
                                m_switchModeCharPos = Value.Length - trailingNumerics;
                        }
                    }
                }

            }
        }
        /// <summary>
        /// Checks the whole value if it contains only numeric values (0 to 9)
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        private bool CheckIfNumericValuesOnly(string Value)
        {
            int count = 0;
            foreach (char c in Value)
            {
                if (!Char.IsNumber(c))
                    return false;

                count++;
            }

            return count > 0;
        }

        /// <summary>
        /// Cuts leading control characters into separate string m_trailingControlChars
        /// </summary>
        /// <param name="input">input string</param>
        /// <returns>returns number of leading control characters (leading only!!)</returns>
        private string cutTrailingControlCharacters(string input) {
            int count = 0;
            m_trailingControlChars = "";
            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
            {
                if (c > 31)
                    break;

                sb.Append(c);
                count++;
            }
            m_trailingControlChars = sb.ToString();
            if (count > 0)
                return input.Substring(count, input.Length - count);
            else
                return input;
        }

        private void encodeNumeric(string input, StringBuilder output, ref int checkSum)
        {           
            if (input.Length % 2 > 0)
                throw new InvalidOperationException("Numeric encoding attempted on odd number of characters");

            // now encode numeric values by pairs (and shifted with +27
            // so we get double density as we encode "10" number as 37 ASCII character
            for (int j = 0; j < input.Length; j += 2)
            {
                string cS = input.Substring(j, 2);
                int c = Int32.Parse(cS) + 27;
                output.Append(m_charPatterns[c]);
                checkSum += c;
            }
        }


        /// <summary>
        /// Replaces control characters given inside square brackets
        /// i.e. to insert control character 10 we can use [10]
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string preProcessControlCharacters(string input) {
            if (input.IndexOf('[') == -1 || input.IndexOf(']') == -1)
                return input; // we don't have both [ and [ so we exit

            for (int i = 0; i <= 31; i++) {
                string pattern = String.Format("[{0}]", i);
                input = input.Replace(pattern, String.Format("{0}", (char)i));
            }



            return input;
        }

        private void encodeSwitchMode(StringBuilder output, ref int checkSum)
        {
            // ASCII code DLE (16) is used to switch modes
            checkSum += 16;
            output.Append(m_charPatterns[16]);
        }

        private void encodeASCII(string input, StringBuilder output, ref int checkSum)
        {
            foreach (char c in input)
            {
                int i = (int)c;
                output.Append(m_charPatterns[i]);
                checkSum += i;
            }
        }
    }
}
