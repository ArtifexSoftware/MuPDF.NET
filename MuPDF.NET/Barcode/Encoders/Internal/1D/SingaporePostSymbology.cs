using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws Singapore 4-State Postal Code barcodes.
    /// </summary>
    class SingaporePostSymbology : RoyalMailSymbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SingaporePostSymbology"/> class.
        /// </summary>
        public SingaporePostSymbology() : base()
        {
            m_type = TrueSymbologyType.SingaporePostalCode;
        }

        public SingaporePostSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.SingaporePostalCode;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "Singapore 4-State Postal Code symbology allows only digits and characters from A to Z to be encoded.";
        }
    }
}
