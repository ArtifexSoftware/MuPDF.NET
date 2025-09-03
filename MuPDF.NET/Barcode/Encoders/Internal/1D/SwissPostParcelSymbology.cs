using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws Swiss Post Parcel barcodes.
    /// </summary>
    class SwissPostParcelsymbology : Code128Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwissPostParcelsymbology"/> class.
        /// </summary>
        public SwissPostParcelsymbology()
            : base()
        {
            m_type = TrueSymbologyType.SwissPostParcel;
        }

        public SwissPostParcelsymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.SwissPostParcel;
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
            if (getCleanValue(value).Length < 18)
                return false;

			return base.ValueIsValid(getCleanValue(value), checksumIsMandatory);
        }

        private static string getCleanValue(string value)
        {
            return value.Replace(".", "");
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>The value restrictions description string.</returns>
        public override string getValueRestrictions()
        {
            return "Swiss Post Parcel symbology allows only numeric values with exactly 18 digits: 2 digits for Swiss Post reference, 8 digits for Franking license number, and 8 digits for Item number to be encoded. Additionally, dots could be added as separators.\n";
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "00.00.000000.00000000";
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

            if (!forCaption)
                sb.Append(getStartChar());

            sb.Append(getCleanValue(Value));

            string encoded = sb.ToString();

            if (forCaption)
            {
                encoded = encoded.Insert(2, ".");
                encoded = encoded.Insert(5, ".");
                encoded = encoded.Insert(12, ".");
            }

            return encoded;
        }

        protected override Code128Alphabet getUsedAlphabet()
        {
            return Code128Alphabet.C;
        }
    }
}
