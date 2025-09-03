/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Drawing;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using Code 39 (aka USD-3, 3 of 9) symbology rules.
    /// This symbology used for example by U.S. Government and military, required for DoD applications.
    /// </summary>
    class Code39Symbology : SymbologyDrawing
    {
        protected static char[] m_alphabet = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 
            'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '-', '.', ' ', '$', 
            '/', '+', '%' };

        private static string m_extendedAlphabet = "%U$A$B$C$D$E$F$G$H$I$J$K$L$M$N$O$P$Q$R$S$T$U$V$W$X$Y$Z%A%B%C%D%E_ /A/B/C/D/E/F/G/H/I/J/K/L_-_./O_0_1_2_3_4_5_6_7_8_9/Z%F%G%H%I%J%V_A_B_C_D_E_F_G_H_I_J_K_L_M_N_O_P_Q_R_S_T_U_V_W_X_Y_Z%K%L%M%N%O%W+A+B+C+D+E+F+G+H+I+J+K+L+M+N+O+P+Q+R+S+T+U+V+W+X+Y+Z%P%Q%R%S%Z";
        private bool m_useExtendedAlphabet;

        /// <summary>
        /// Initializes a new instance of the <see cref="Code39Symbology"/> class.
        /// </summary>
        public Code39Symbology()
            : base(TrueSymbologyType.Code39)
        {
            init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Code39Symbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public Code39Symbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.Code39)
        {
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        private void init()
        {
            // Since Code 39 is self-checking, a check digit normally isn't necessary.
            AddChecksum = false;
            AddChecksumToCaption = false;
        }

        /// <summary>
        /// Validates the value using Code 39 symbology rules.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns><c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.</returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            foreach (int c in value)
            {
                if (c > 127)
                    return false;
            }

            // value is valid, but do we need to use extended alphabet?
            m_useExtendedAlphabet = false;

            foreach (char c in value)
            {
                // if character not found within alphabet
                // then we need to use extended alphabet
                if (getCharPosition(c) == -1)
                    m_useExtendedAlphabet = true;
            }

            return true;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>The value restrictions description string.</returns>
        public override string getValueRestrictions()
        {
            return "Code 39 symbology allows at most first 128 ASCII symbols to be encoded.";
        }

        /// <summary>
        /// Gets the barcode value encoded using Code 39 symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using Code 39 symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            StringBuilder sb = new StringBuilder();

            if (Options.ShowStartStop || !forCaption)
                sb.Append("*");

            string valueAsSimpleChars = getValueAsSimpleChars();

            if (forCaption)
                sb.Append(Value);
            else
                sb.Append(valueAsSimpleChars);

            if (AddChecksum)
            {
                if ((forCaption && AddChecksumToCaption) || (!forCaption))
                {
                    int checksumCharPosition = calculateChecksum(valueAsSimpleChars);
                    char checksumChar = m_alphabet[checksumCharPosition];
                    sb.Append(checksumChar);
                }
            }

            if (Options.ShowStartStop || !forCaption)
                sb.Append("*");

            return sb.ToString();
        }

        /// <summary>
        /// Gets the value as simple chars (replaces extended chars with simple chars).
        /// </summary>
        /// <returns>The value as simple chars.</returns>
        public string getValueAsSimpleChars()
        {
            string value = Value;
            if (!m_useExtendedAlphabet)
                return value;

            // simple string is at most twice as long as extended
            // because each extended character encoded as two simple characters
            StringBuilder sb = new StringBuilder(value.Length * 2);

            foreach (char c in Value)
            {
                int shiftCharPosition = ((int)c) * 2;
                char shiftChar = m_extendedAlphabet[shiftCharPosition];
                if (shiftChar != '_')
                {
                    // underscore is a padding character. just skip it.
                    sb.Append(shiftChar);
                }

                sb.Append(m_extendedAlphabet[shiftCharPosition + 1]);
            }

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
                    return "nnnwwnwnnn";
                case '1':
                    return "wnnwnnnnwn";
                case '2':
                    return "nnwwnnnnwn";
                case '3':
                    return "wnwwnnnnnn";
                case '4':
                    return "nnnwwnnnwn";
                case '5':
                    return "wnnwwnnnnn";
                case '6':
                    return "nnwwwnnnnn";
                case '7':
                    return "nnnwnnwnwn";
                case '8':
                    return "wnnwnnwnnn";
                case '9':
                    return "nnwwnnwnnn";
                case 'A':
                    return "wnnnnwnnwn";
                case 'B':
                    return "nnwnnwnnwn";
                case 'C':
                    return "wnwnnwnnnn";
                case 'D':
                    return "nnnnwwnnwn";
                case 'E':
                    return "wnnnwwnnnn";
                case 'F':
                    return "nnwnwwnnnn";
                case 'G':
                    return "nnnnnwwnwn";
                case 'H':
                    return "wnnnnwwnnn";
                case 'I':
                    return "nnwnnwwnnn";
                case 'J':
                    return "nnnnwwwnnn";
                case 'K':
                    return "wnnnnnnwwn";
                case 'L':
                    return "nnwnnnnwwn";
                case 'M':
                    return "wnwnnnnwnn";
                case 'N':
                    return "nnnnwnnwwn";
                case 'O':
                    return "wnnnwnnwnn";
                case 'P':
                    return "nnwnwnnwnn";
                case 'Q':
                    return "nnnnnnwwwn";
                case 'R':
                    return "wnnnnnwwnn";
                case 'S':
                    return "nnwnnnwwnn";
                case 'T':
                    return "nnnnwnwwnn";
                case 'U':
                    return "wwnnnnnnwn";
                case 'V':
                    return "nwwnnnnnwn";
                case 'W':
                    return "wwwnnnnnnn";
                case 'X':
                    return "nwnnwnnnwn";
                case 'Y':
                    return "wwnnwnnnnn";
                case 'Z':
                    return "nwwnwnnnnn";
                case '-':
                    return "nwnnnnwnwn";
                case '.':
                    return "wwnnnnwnnn";
                case ' ':
                    return "nwwnnnwnnn";
                case '$':
                    return "nwnwnwnnnn";
                case '/':
                    return "nwnwnnnwnn";
                case '+':
                    return "nwnnnwnwnn";
                case '%':
                    return "nnnwnwnwnn";
                case '*':
                    return "nwnnwnwnnn";
            }

            return "wwwwwwwwww";
        }

        /// <summary>
        /// Gets the char position within the alphabet.
        /// </summary>
        /// <param name="c">The char to find.</param>
        /// <returns></returns>
        protected static int getCharPosition(char c)
        {
            for (int i = 0; i < m_alphabet.Length; i++)
            {
                if (m_alphabet[i] == c)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Calculates the modulo 43 checksum of the given value.
        /// </summary>
        /// <param name="value">The value to calculate checksum for.</param>
        /// <returns></returns>
        private static int calculateChecksum(string value)
        {
            int checksum = 0;

            foreach (char c in value)
            {
                int checkValue = getCharPosition(c);
                if (checkValue == -1)
                {
                    string message = String.Format("Incorrect character '%c' for Code 39 symbology", c);
                    throw new BarcodeException(message);
                }

                checksum += checkValue;
            }

            return checksum % 43;
        }
    }
}
