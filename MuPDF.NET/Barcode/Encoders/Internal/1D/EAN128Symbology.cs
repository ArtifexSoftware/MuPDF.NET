/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using EAN128 symbology. This symbology was developed 
    /// to provide a worldwide format and standard for exchanging common 
    /// data between companies.
    /// </summary>
    /// <remarks>See also:
    /// http://www.gs1-128.info/
    /// http://www.barcodeisland.com/uccean128.phtml
    /// </remarks>
    class EAN128Symbology : Code128Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EAN128Symbology"/> class.
        /// </summary>
        public EAN128Symbology()
            : base()
        {
            m_type = TrueSymbologyType.EAN128;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EAN128Symbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public EAN128Symbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.EAN128;
        }

        /// <summary>
        /// Validates the value using EAN 128 symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Checksum is mandatory or not (if applicable).</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length == 0)
                return false;

            intList leftBracketPos = new intList();
            intList rightBracketPos = new intList();
            bool validBrackets = GS1ValueChecker.FindBracketPositions(value, leftBracketPos, rightBracketPos);
			bool useCheckDigits = (AddChecksum || checksumIsMandatory) && validBrackets && (leftBracketPos.Count == 1);
        	
        	return base.autoValueIsValid(getCleanValue(value, useCheckDigits));
        }

        /// <summary>
        /// Gets or sets the barcode value to encode.
        /// </summary>
        /// <value>The barcode value to encode.</value>
        public override string Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                //if (this is EAN14Symbology)
                //{
                //    base.Value = value;
                //}
                //else
                {
                    bool isBracket = false;
                    for (int i = 0; i < value.Length; i++)
                        if (value[i] == '(')
                        {
                            isBracket = true;
                            break;
                        }
                    if (isBracket)
                        base.Value = value;
                    else
                        base.Value = ApplicationIdentifiers.SelectAIs(value);
                }
            }
        }


        // there is also GS1ValueChecker.GetStripped() to get the value cleaned from ( and )
        private string getCleanValue(string value, bool useCheckDigits)
        {
            string[] parts = value.Split(new char[] { '(', ')' });
            StringBuilder sb = new StringBuilder();
            foreach (string s in parts)
            {
                if (s.Length != 0)
                {
                    sb.Append(s);

                	bool containsAlpha = false;

					for (int pos = 0; pos < s.Length; pos++)
					{
						if (!char.IsDigit(s[pos]))
						{
							containsAlpha = true;
							break;
						}
					}

                    if (useCheckDigits)
                    {
						if (s.Length == 7 && !containsAlpha)
                            sb.Append(calculateMod10Checksum(s));
						else if (s.Length == 11 && !containsAlpha)
                            sb.Append(calculateMod10Checksum(s));
						else if (s.Length == 13 && !containsAlpha)
                            sb.Append(calculateMod10Checksum(s));
						else if (s.Length == 17 && !containsAlpha)
                            sb.Append(calculateMod10Checksum(s));
                    }
                }
            }

            return sb.ToString();
        }

        private char calculateMod10Checksum(string s)
        {
            // http://www.gs1.org/barcodes/support/check_digit_calculator
            // 1) Multiply value of each position by
            // x3 x1 x3 x1 x3 x1 x3 x1 x3 x1 x3 x1 x3 x1 x3 x1 x3
            // 2) Add results together to create sum
            // 3) Subtract the sum from nearest equal or higher multiple of ten = Check Digit

            int total = 0;
            for (int i = 0; i < s.Length; i++)
            {
                int value = m_numbers.IndexOf(s[i]);
                if (i % 2 != 0)
                    total += value;
                else
                    total += value * 3;
            }

            if (total % 10 == 0)
                return '0';

            int baseValue = total / 10;
            baseValue++;

            return m_numbers[(baseValue * 10) - total];
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>The value restrictions description string.</returns>
        public override string getValueRestrictions()
        {
            string restriction = "GS1-128 (UCC/EAN128) allows at most first 128 ASCII symbols to be encoded.";
            restriction += "The value can be set in 2 forms: with parenthesis for AI and without.";
            restriction += "If a value comes in form like xxxxxxxxxxx then the SDK automatically sets brackets according to GS1 AI (Application Identifiers).";
            restriction += "If a value comes in form like (xx)yyyyyy then the SDK do NOT verifies the value against AI. However you may verify if the value (with parenthesis) is valid or not by using Barcode.ValueIsValidGS1() bool function";
            return restriction;
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "1112345617ABCDEF";
        }

        /// <summary>
        /// Gets the barcode value encoded using EAN 128 symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using EAN 128 symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            if (forCaption)
                return Value;

            return getValueForEncoding(Value);
        }

        protected string getValueForEncoding(string value)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_startC);
            sb.Append(m_fnc1);

            // always start with Alphabet C
            Code128Alphabet alphabet = Code128Alphabet.C;

            intList leftBracketPos = new intList();
            intList rightBracketPos = new intList();
            if (GS1ValueChecker.FindBracketPositions(value, leftBracketPos, rightBracketPos))
            {
                bool sawNonFixedLength = false;
                bool useCheckDigits = AddChecksum;

                for (int i = 0; i < leftBracketPos.Count; i++)
                {
                    // we need to encode value chunk using auto alphabet selection
                    int start = leftBracketPos[i];
                    int nextStart = value.Length;
                    if (i != (leftBracketPos.Count - 1))
                        nextStart = leftBracketPos[i + 1];

                    bool fixedLength = false;
                    string aiStart = value.Substring(start + 1, 2);
                    for (int j = 0; j < GS1ValueChecker.FixedLengthAIs.Length; j++)
                    {
                        string ai = GS1ValueChecker.FixedLengthAIs[j];
                        if (aiStart == ai)
                            fixedLength = true;
                    }

                    string chunk = value.Substring(start, nextStart - start);

                    if (sawNonFixedLength)
                    {
                        // should separate variable length chunks with FNC1
                        sb.Append(m_fnc1);
                        sawNonFixedLength = false;
                    }

                    sb.Append(encodeValue(getCleanValue(chunk, useCheckDigits), ref alphabet));

                    if (!fixedLength)
                        sawNonFixedLength = true;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Encodes current value using auto alphabet selection.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <param name="alphabet">The alphabet to start encoding with.</param>
        /// <returns>
        /// Current value encoded using auto alphabet selection.
        /// </returns>
        private string encodeValue(string value, ref Code128Alphabet alphabet)
        {
            StringBuilder sb = new StringBuilder();

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
        protected override Code128Alphabet selectAlphabet(string value, int pos)
        {
            int numCount = getNumberCount(value, pos);
            if (numCount != 0 && numCount % 2 == 0)
                return Code128Alphabet.C;
            else if (getFirstControlCharPos(value, pos) > getFirstLowerCasePos(value, pos))
                return Code128Alphabet.A;
            else
                return Code128Alphabet.B;
        }

        /// <summary>
        /// Gets the character values array for the given string.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>The character values array for the given string.</returns>
        protected override int[] getValues(string value)
        {
            // number of values is less or equal to number of characters in source string
            int[] intialValues = new int[value.Length];

            Code128Alphabet alphabet = Code128Alphabet.C;
            intialValues[0] = getCharValue(m_startC, alphabet);
            intialValues[1] = getCharValue(m_fnc1, alphabet);
            int valuesAdded = 1;

            bool shiftOccured = false;
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

                    case m_fnc1:
                        special = true;
                        intialValues[valuesAdded] = getCharValue(m_fnc1, alphabet);
                        break;
                }

                if (!special)
                {
                    if (alphabet == Code128Alphabet.C)
                    {
                        string s = value.Substring(i, 2);
                        intialValues[valuesAdded] = Convert.ToInt16(s);
                        i++;
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
    }
}
