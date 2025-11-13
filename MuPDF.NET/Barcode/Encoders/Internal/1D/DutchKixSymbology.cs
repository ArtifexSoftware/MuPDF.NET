using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws Dutch KIX barcodes.
    /// </summary>
    class DutchKixSymbology : RoyalMailSymbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DutchKixSymbology"/> class.
        /// </summary>
        public DutchKixSymbology()
            : base()
        {
            m_type = TrueSymbologyType.DutchKix;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DutchKixSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public DutchKixSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.DutchKix;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "Ductch KIX symbology allows only digits and characters from A to Z to be encoded.";
        }

        /// <summary>
        /// Gets the barcode value encoded using current symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            return Value.ToUpper();
        }
    }
}
