using System;
using System.Text;
using System.Drawing;

namespace BarcodeWriter.Core.Internal
{
    class EAN14Symbology : EAN128Symbology
    {
        /// <summary>
        /// EAN-14 (also known as DUN-14, SCC-14 or UPC Shipping Container Code)
        /// is a 14-digit code used to identify shipping containers.
        /// There are two types of Shipping Container Code representation:
        /// 1) Using UCC/EAN-128 system (which is based on CODE 128 encoding),
        ///    with AI set to 01;
        /// 2) Using ITF-14 encoding (which is based on Interleaved 2 of 5 symbology).
        ///    This type is not supported in this class.
        /// </summary>
        /// <remarks>See also:
        /// http://strokescribe.com/en/EAN14.html
        /// </remarks>
        public EAN14Symbology()
            : base()
        {
            m_type = TrueSymbologyType.EAN14;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EAN14Symbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public EAN14Symbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.EAN14;
        }

        /// <summary>
        /// Validates the value using EAN 14 symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">This parameter is not applicable to this symbology (checksum is always mandatory for this barcodes).</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length == 0)
                return false;

            string checkstr;
            if (value.Substring(0, 4) == "(01)")
                checkstr = value.Substring(4);
            else if (value.Substring(0, 2) == "01")
                checkstr = value.Substring(2);
            else
                checkstr = value;

            if (checkstr.Length != 14)
                return false;

            string alphabet = "0123456789";

            foreach (char c in checkstr)
            {
                if (alphabet.IndexOf(c) == -1)
                    return false;
            }

            char checksum = checkstr[13];
            if (checksum != GS1Utils.getGTINChecksum(checkstr.Substring(0, 13)))
                return false;

            return true;
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
                string tempstr;
                
                if (value.StartsWith("(01)"))
                    tempstr = value.Substring(4);
                else if (value.StartsWith("01"))
                    tempstr = value.Substring(2);
                else
                    tempstr = value;

                if (tempstr.Length == 13)
                    base.Value = "(01)" + tempstr + GS1Utils.getGTINChecksum(tempstr);
                else
                    base.Value = "(01)" + tempstr;
            }
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>The value restrictions description string.</returns>
        public override string getValueRestrictions()
        {
            return "The EAN-14 barcode value must have exactly 14 digits and must not start with a zero.";
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "00000000000000";
        }
    }
}
