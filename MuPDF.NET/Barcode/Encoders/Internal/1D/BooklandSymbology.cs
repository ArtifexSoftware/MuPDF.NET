/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using Bookland symbology rules. Bookland symbology is 
    /// used exclusively with books.
    /// </summary>
    class BooklandSymbology : EAN13Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BooklandSymbology"/> class.
        /// </summary>
        public BooklandSymbology()
            : base()
        {
            m_type = TrueSymbologyType.Bookland;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BooklandSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public BooklandSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.Bookland;
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
        /// Validates the value using Bookland symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length != 9)
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
            return "Bookland symbology allows only strings with exactly nine numbers to be encoded. Any value is always prepended with '978'";
        }

        /// <summary>
        /// Gets the barcode value encoded using Bookland symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            // checksum char ALWAYS added to encoded value and caption
            return "978" + Value + getChecksum("978" + Value);
        }
    }
}
