using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws PZN (Pharma Zentral Nummer) barcodes.
    /// </summary>
    class PZNSymbology : Code39Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PZNSymbology"/> class.
        /// </summary>
        public PZNSymbology()
            : base()
        {
            m_type = TrueSymbologyType.PZN;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PZNSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public PZNSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.PZN;
        }

        /// <summary>
        /// Validates the value using PZN symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (Options.PZNType == PZNType.PZN7 && value.Length != 6)
                return false;
            if (Options.PZNType == PZNType.PZN8 && value.Length != 6 && value.Length != 7)
                return false;

			return base.ValueIsValid(value, checksumIsMandatory);
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            if (Options.PZNType == PZNType.PZN7)
                return "PZN symbology allows only numeric values with exactly 6 digits to be encoded\n";
            else
                return "PZN symbology allows only numeric values with exactly 6 or 7 digits to be encoded\n";
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "000000";
        }

        /// <summary>
        /// Gets the barcode value encoded using Numly symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            StringBuilder sb = new StringBuilder();

            if (forCaption)
                sb.Append("PZN - ");

            if (!forCaption)
                sb.Append("*-");

            if (Options.PZNType == PZNType.PZN8 && Value.Length == 6)
                sb.Append("0");

            sb.Append(Value);
            sb.Append(getChecksumChar(Value));

            if (!forCaption)
                sb.Append("*");

            return sb.ToString();
        }

        /// <summary>
        /// Gets the checksum char.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The checksum char.</returns>
        protected static char getChecksumChar(string value)
        {
            int sum = 0;
            int shift = 8 - value.Length;
            for (int i = 0; i < value.Length; i++)
                sum += (i + shift) * getCharPosition(value[i]);

            int checkDigitPos = sum % 11;
            if (checkDigitPos == 11)
                checkDigitPos = 0;

            return m_alphabet[checkDigitPos];
        }
    }
}
