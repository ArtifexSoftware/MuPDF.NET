using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws Numly barcodes.
    /// </summary>
    class NumlySymbology : Code39Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumlySymbology"/> class.
        /// </summary>
        public NumlySymbology()
            : base()
        {
            m_type = TrueSymbologyType.Numly;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumlySymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public NumlySymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.Numly;
        }

        /// <summary>
        /// Validates the value using Numly symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            string cleanValue = getCleanValue(value);
            if (cleanValue.Length != 19)
                return false;

			return base.ValueIsValid(cleanValue, checksumIsMandatory);
        }

        private static string getCleanValue(string value)
        {
            return value.Replace("-", "");
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "Numly symbology allows only numeric values with exactly 19 digits to be encoded. Additionally, dashes can be added as separators\n";
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "00000-000000-000000-00";
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
            string encoded = getCleanValue(Value);

            if (forCaption)
            {
                encoded = encoded.Insert(5, "-");
                encoded = encoded.Insert(12, "-");
                encoded = encoded.Insert(19, "-");

                encoded = "ESN " + encoded;
            }
            else
                encoded = "*" + encoded + "*";

            return encoded;
        }
    }
}
