
using System;
using System.Drawing;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using Codabar (aka Ames Code/USD-4/NW-7/2 of 7 Code) symbology rules.
    /// This symbology used for example in libraries and blood banks.
    /// </summary>
    class CodabarSymbology : SymbologyDrawing
    {
        private static string m_alphabet = "0123456789-$:/.+";

        /// <summary>
        /// Initializes a new instance of the <see cref="CodabarSymbology"/> class.
        /// </summary>
        public CodabarSymbology()
            : base(TrueSymbologyType.Codabar)
        {
            // Codabar symbology usually shows start and stop symbols within a caption
            // because them can be used for additional data encoding.
            Options.ShowStartStop = true;

            // Codabar symbology have no "official" checksum algorithm
            AddChecksum = false;
            AddChecksumToCaption = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodabarSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public CodabarSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.Codabar)
        {
        }

        /// <summary>
        /// Validates the value using Codabar symbology rules.
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
            return "Codabar symbology allows only symbols from this string '0123456789-$:/.+' to be encoded.\n" +
                "Start and stop symbols are set separately. Please use Options to set start and stop Codabar symbols.";
        }

        /// <summary>
        /// Gets the barcode value encoded using Codabar symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using Codabar symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            StringBuilder sb = new StringBuilder();

            if (Options.ShowStartStop || !forCaption)
                sb.Append(Options.CodabarStartSymbol.ToString());

            sb.Append(Value);

            if (AddChecksum)
            {
                if ((forCaption && AddChecksumToCaption) || (!forCaption))
                {
                    char checksumChar = getChecksumChar(Value);
                    sb.Append(checksumChar);
                }
            }

            if (Options.ShowStartStop || !forCaption)
                sb.Append(Options.CodabarStopSymbol.ToString());

            return sb.ToString();
        }

        /// <summary>
        /// Gets the encoding pattern for given character.
        /// </summary>
        /// <param name="c">The character to retrieve pattern for.</param>
        /// <returns>The encoding pattern for given character.</returns>
        protected override string getCharPattern(char c)
        {
            switch (c)
            {
                case '0':
                    return "nnnnnww";
                case '1':
                    return "nnnnwwn";
                case '2':
                    return "nnnwnnw";
                case '3':
                    return "wwnnnnn";
                case '4':
                    return "nnwnnwn";
                case '5':
                    return "wnnnnwn";
                case '6':
                    return "nwnnnnw";
                case '7':
                    return "nwnnwnn";
                case '8':
                    return "nwwnnnn";
                case '9':
                    return "wnnwnnn";
                case '-':
                    return "nnnwwnn";
                case '$':
                    return "nnwwnnn";
                case ':':
                    return "wnnnwnw";
                case '/':
                    return "wnwnnnw";
                case '.':
                    return "wnwnwnn";
                case '+':
                    return "nnwnwnw";
                case 'A':
                    return "nnwwnwn";
                case 'B':
                    return "nwnwnnw";
                case 'C':
                    return "nnnwnww";
                case 'D':
                    return "nnnwwwn";
            }

            return "wwwwwww";
        }

        /// <summary>
        /// Gets the checksum char.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The checksum char.</returns>
        private char getChecksumChar(string value)
        {
            // There is no checksum defined as part of the Codabar standard, but 
            // some industries (libraries, for example) have adopted their 
            // own checksum standards.

            if (Options.CodabarChecksumAlgorithm == CodabarChecksumAlgorithm.Modulo9)
                return getModulo9ChecksumChar(value);

            return getAIIMChecksumChar(value);
        }

        /// <summary>
        /// Gets the modulo9 checksum char.
        /// </summary>
        /// <param name="value">The value to calculate checksum char for.</param>
        /// <returns>The modulo9 checksum char.</returns>
        private static char getModulo9ChecksumChar(string value)
        {
            // Many libraries use the following system which includes 13 digits 
            // plus a checksum; 
            // Digit 1 indicates the type of barcode:  2 = patron, 3 = item (book)
            // Digits 2-5 identify the institution
            // The next 6 digits (00010 586) identify the individual patron or item
            // Digit 14 is the checksum

            // For implementation details please take a look at
            // https://www.barcodesinc.com/articles/codabar.htm

            // https://en.wikipedia.org/wiki/Codabar
            // https://en.wikipedia.org/wiki/Luhn_algorithm

            int evenSum = 0;
            int oddSum = 0;

            // The most significant (leftmost) digit position is considered 'odd'.
            for (int i = 0; i < value.Length; i++)
            {
                if (i % 2 == 0)
                {
                    int product = getCharPosition(value[i]) * 2;
                    if (product < 10)
                        oddSum += product;
                    else
                        oddSum += product - 9;
                }
                else
                    evenSum += getCharPosition(value[i]);
            }

            int total = oddSum + evenSum;

            int checkCharPos = 0;
            if (total != 0)
                checkCharPos = (total / 10) * 10 + 10 - total;

            // ... in case the sum of digits ends in 0 then 0 is the check digit.
            if (checkCharPos % 10 == 0)
                checkCharPos = 0;

            return m_alphabet[checkCharPos];
        }

        /// <summary>
        /// Gets the AIIM checksum char.
        /// </summary>
        /// <param name="value">The value to calculate checksum char for.</param>
        /// <returns>The AIIM checksum char.</returns>
        private char getAIIMChecksumChar(string value)
        {
            // AIM recommends the following checkdigit calculation
            // 1. The sum of all character values is taken, including the 
            //    Start and the Stop characters.
            // 2. The data character whose value that when added to the total 
            //    from step one equals a multiple of 16 is the check character.
            int total = getSpecialSymbolValue(Options.CodabarStartSymbol);

            for (int i = 0; i < value.Length; i++)
                total += getCharPosition(value[i]);

            total += getSpecialSymbolValue(Options.CodabarStopSymbol);

            int checkCharPos = (total / 16) * 16 + 16 - total;

            if ((checkCharPos % 16) == 0)
                checkCharPos = 0;

            return m_alphabet[checkCharPos];
        }

        /// <summary>
        /// Gets the char position within the alphabet.
        /// </summary>
        /// <param name="c">The char to find.</param>
        /// <returns></returns>
        private static int getCharPosition(char c)
        {
            for (int i = 0; i < m_alphabet.Length; i++)
            {
                if (m_alphabet[i] == c)
                    return i;
            }

            string message = String.Format("Incorrect character '%c' for Codabar symbology", c);
            throw new BarcodeException(message);
        }

        /// <summary>
        /// Gets the special symbol value.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns></returns>
        private static int getSpecialSymbolValue(CodabarSpecialSymbol symbol)
        {
            if (symbol == CodabarSpecialSymbol.A)
                return 16;
            else if (symbol == CodabarSpecialSymbol.B)
                return 17;
            else if (symbol == CodabarSpecialSymbol.C)
                return 18;

            return 19; // D
        }
    }
}
