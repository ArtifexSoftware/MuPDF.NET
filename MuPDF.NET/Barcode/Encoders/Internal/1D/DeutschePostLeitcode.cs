using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws Deutsche Post Leitcode
    /// </summary>
    class DeutschePostLeitcodeSymbology : I2of5Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeutschePostLeitcodeSymbology"/> class.
        /// </summary>
        public DeutschePostLeitcodeSymbology()
            : base()
        {
            m_type = TrueSymbologyType.DeutschePostLeitcode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeutschePostLeitcodeSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public DeutschePostLeitcodeSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.DeutschePostLeitcode;
        }

        /// <summary>
        /// Validates the value using Deutsche Post Leitcode symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Checksum is obligatory or not.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            string cleanValue = getCleanValue(value);
            if (cleanValue.Length != 13)
                return false;

			return base.ValueIsValid(cleanValue, checksumIsMandatory);
        }

        private static string getCleanValue(string value)
        {
            string cleanValue = value.Replace(" ", "");
            cleanValue = cleanValue.Replace(".", "");
            return cleanValue;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "Deutsche Post Leitcode symbology allows only numeric values with exactly 13 digits: 5 digits for Postal Code (Postleitzahl, PLZ), 3 digits for Street ID/number, 3 digits for House number, and 2 digits for Product code to be encoded. Additionally, spaces and dots can be added as separators\n";
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "00000.000.000.00";
        }

        /// <summary>
        /// Gets the barcode value encoded using Deutsche Post Leitcode symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            StringBuilder sb = new StringBuilder();

            string cleanValue = getCleanValue(Value);
            sb.Append(cleanValue);

            if ((forCaption && AddChecksumToCaption) || (!forCaption))
            {
                char checksumChar = getChecksumChar(cleanValue);
                sb.Append(checksumChar);
            }

            string encoded = sb.ToString();

            if (forCaption)
            {
                encoded = encoded.Insert(5, ".");
                encoded = encoded.Insert(9, ".");
                encoded = encoded.Insert(13, ".");

                if (AddChecksumToCaption)
                    encoded = encoded.Insert(16, " ");
            }

            return encoded;
        }

        /// <summary>
        /// Gets the checksum char.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The checksum char.</returns>
        protected override char getChecksumChar(string value)
        {
            int sum = 0;
            for (int i = value.Length - 1; i >= 0; i--)
            {
                sum += 4 * getCharPosition(value[i]);

                if (!((i % 2) == 0))
                    sum += 5 * getCharPosition(value[i]);
            }

            int checkDigitPos = 10 - (sum % 10);
            if (checkDigitPos == 10)
                checkDigitPos = 0;

            return m_alphabet[checkDigitPos];
        }
    }
}
