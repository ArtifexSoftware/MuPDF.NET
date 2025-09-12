/**************************************************
 *
 *
 *
 *
**************************************************/

using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using JAN13 (aka JAN Codes) symbology rules. JAN-13
    /// symbology is used mostly in Japan.
    /// </summary>
    class JAN13Symbology : EAN13Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JAN13Symbology"/> class.
        /// </summary>
        public JAN13Symbology()
            : base()
        {
            m_type = TrueSymbologyType.JAN13;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JAN13Symbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public JAN13Symbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.JAN13;
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
        /// Validates the value using JAN-13 symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length != 10)
                return false;

            foreach (char c in value)
            {
                if (m_alphabet.IndexOf(c) == -1)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "JAN-13 symbology allows only strings with exactly ten numbers to be encoded. Any value is always prepended with '49'";
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
            // checksum char ALWAYS added to encoded value and caption
            return "49" + Value + getChecksum("49" + Value);
        }
    }
}
