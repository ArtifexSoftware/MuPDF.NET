using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws USPS Sack Label barcodes.
    /// </summary>
    class USPSSackLabelSymbology : I2of5Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="USPSSackLabelSymbology"/> class.
        /// </summary>
        public USPSSackLabelSymbology() : base()
        {
            m_type = TrueSymbologyType.USPSSackLabel;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="USPSSackLabelSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public USPSSackLabelSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.USPSSackLabel;
        }

        /// <summary>
        /// Validates the value using USPS Sack Label symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Checksum is mandatory or not (if applicable).</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length != 8)
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
            return "USPS Sack Label symbology allows only numeric values with exactly 8 digits: 5-digit Zip Code (the sack destination) and a 3-digit content identifier number(CIN) to be encoded.\n";
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "00000000";
        }

        /// <summary>
        /// Gets the barcode value encoded using USPS Sack Label symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            return Value;
        }
    }
}
