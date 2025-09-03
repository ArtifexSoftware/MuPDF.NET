using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws OPC (Optical Product Code) barcodes
    /// </summary>
    class OPCSymbology : I2of5Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OPCSymbology"/> class.
        /// </summary>
        public OPCSymbology()
            : base()
        {
            m_type = TrueSymbologyType.OpticalProduct;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OPCSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public OPCSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.OpticalProduct;
        }

        /// <summary>
        /// Validates the value using Optical Code symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Checksum is mandatory or not (if applicable).</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length != 9)
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
            return "Optical Product Code symbology allows only numeric values with exactly 9 digits: 5 digits for Manufacturer Identification Number assigned by the Optical Product Code Council, Inc., 4 digits for Item Identification Number assigned and controlled by the optical manufacturer to be encoded.\n";
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "000000000";
        }

        /// <summary>
        /// Gets the barcode value encoded using Interleaved 2 of 5 symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Value);

            char checksumChar = getChecksumChar(Value);
            sb.Append(checksumChar);

            return sb.ToString();
        }

        /// <summary>
        /// Gets the checksum char.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The checksum char.</returns>
        protected override char getChecksumChar(string value)
        {
            int sum = 0;

            for (int i = 0; i < value.Length; i++)
            {
                int weight = 0;
                if (i % 2 == 0)
                    weight = 2 * getCharPosition(value[i]);
                else
                    weight = getCharPosition(value[i]);

                if (weight < 10)
                    sum += weight;
                else
                    sum += weight / 10 + weight % 10;
            }

            int checkDigitPos = 10 - (sum % 10);
            if (checkDigitPos == 10)
                checkDigitPos = 0;

            return m_alphabet[checkDigitPos];
        }
    }
}
