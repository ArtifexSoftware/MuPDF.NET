using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws USPS Tray Label barcodes.
    /// </summary>
    class USPSTrayLabelSymbology : I2of5Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="USPSTrayLabelSymbology"/> class.
        /// </summary>
        public USPSTrayLabelSymbology() : base()
        {
            m_type = TrueSymbologyType.USPSTrayLabel;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="USPSTrayLabelSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public USPSTrayLabelSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.USPSTrayLabel;
        }

        /// <summary>
        /// Validates the value using USPS Tray Label symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Checksum is mandatory or not (if applicable).</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length != 10)
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
            return "USPS Tray Label symbology allows only numeric values with exactly 10 digits: 5-digit Zip Code (the sack destination), a 3-digit content identifier number(CIN), and a 2-digit USPS processing code to be encoded.\n";
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "0000000000";
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
