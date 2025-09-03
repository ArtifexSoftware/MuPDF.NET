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
    /// <summary>
    /// Draws EAN-13 barcodes for ISBN numbers.
    /// </summary>
    class ISBNSymbology : EAN13Symbology
    {
        private static string m_template10 = "#-#######-#-#";
        private static string m_template13 = "###-#-####-####-#";
        private string m_currentTemplate = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ISBNSymbology"/> class.
        /// </summary>
        public ISBNSymbology()
            : base()
        {
            m_type = TrueSymbologyType.ISBN;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ISBNSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public ISBNSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.ISBN;
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "978000000000";
        }

        /// <summary>
        /// Validates the value using ISBN symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            string realValue = getRealValue(value, ref m_currentTemplate);
            if (realValue.Length != 9 && realValue.Length != 10 && realValue.Length != 12 && realValue.Length != 13)
                return false;

            if (realValue.Length == 12 || realValue.Length == 13)
            {
                string prefix = realValue.Substring(0, 3);
                if (prefix != "978" && prefix != "979")
                    return false;
            }

            foreach (char c in realValue)
            {
                if (m_alphabet.IndexOf(c) == -1 && c != 'x' && c != 'X')
                    return false;
            }

            return true;
        }

        public string GetAutoCaption()
        {
            string realValue = getRealValue(Value).ToUpper();

            if (realValue.Length == 9)
                realValue += getISBNCheckDigit(realValue);

            string template;
            if (Options.ISBNCaptionTemplate != string.Empty)
                template = Options.ISBNCaptionTemplate;
            else if (m_currentTemplate != string.Empty)
                template = m_currentTemplate;
            else if (realValue.Length == 10)
                template = m_template10;
            else
                template = m_template13;

            StringBuilder sb = new StringBuilder();
            int index = 0;
            for (int i = 0; i < template.Length; i++)
            {
                if (template[i] == '-')
                    sb.Append('-');
                else if (index<realValue.Length)
                {
                    sb.Append(realValue[index]);
                    index++;
                }
            }
            return "ISBN " + sb.ToString();

            //if (realValue.Length == 10)
            //{
            //    realValue = realValue.Insert(1, "-");
            //    realValue = realValue.Insert(9, "-");
            //    realValue = realValue.Insert(11, "-");
            //    return "ISBN " + realValue;
            //}

            //realValue = GetEncodedValue(true);
            //realValue = realValue.Insert(3, "-");
            //realValue = realValue.Insert(5, "-");
            //realValue = realValue.Insert(13, "-");
            //realValue = realValue.Insert(15, "-");
            //return "ISBN " + realValue;
        }

        private static string getISBNCheckDigit(string realValue)
        {
            int checksum = 0;
            for (int i = 0; i < 9; i++)
            {
                int digit = m_alphabet.IndexOf(realValue[i]);
                checksum += digit * (i + 1);
            }

            checksum = checksum % 11;
            if (checksum == 10)
                return "X";

            return m_alphabet[checksum].ToString();
        }

        private static string getRealValue(string value, ref string ISBNTemplate)
        {
            string[] chunks = value.Split(new char[] { ' '});

            StringBuilder sb = new StringBuilder();
            foreach (string chunk in chunks)
                sb.Append(chunk);

            chunks = sb.ToString().Split(new char[] { '-'});
            if (chunks.Length == 1)
            {
                ISBNTemplate = string.Empty;
                return chunks[0];
            }

            sb = new StringBuilder();
            StringBuilder template = new StringBuilder();
            for (int i = 0; i < chunks.Length; i++)
			{
                sb.Append(chunks[i]);
                for (int j = 0; j < chunks[i].Length; j++)
                    template.Append('#');
                if (i<chunks.Length-1)
                    template.Append('-');
            }
            ISBNTemplate = template.ToString();

            return sb.ToString();
        }

        private static string getRealValue(string value)
        {
            string[] chunks = value.Split(new char[] { ' ', '-' });

            StringBuilder sb = new StringBuilder();
            foreach (string chunk in chunks)
                sb.Append(chunk);

            return sb.ToString();
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "ISBN symbology allows only strings with valid ISBN (9, 10, 12 or 13 numbers and possibly dashes or spaces between them) to be encoded.";
        }

        /// <summary>
        /// Gets the barcode value encoded using JAN-13 symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            string realValue = getRealValue(Value);
            if (realValue.Length == 10 || realValue.Length == 13)
                realValue = realValue.Remove(realValue.Length - 1, 1);

            if (realValue.Length == 9)
                realValue = "978" + realValue;

            // checksum char ALWAYS added to encoded value and caption
            return realValue + getChecksum(realValue);
        }

        ///// <summary>
        ///// Gets or sets the ISBN caption template (e.g.  #-#######-#-#).
        ///// </summary>
        ///// <value>The ISBN caption template.</value>
        //public string ISBNCaptionTemplate
        //{
        //    get { return m_ISBNCaptionTemplate; }
        //    set { m_ISBNCaptionTemplate = value; }
        //}
    }
}
