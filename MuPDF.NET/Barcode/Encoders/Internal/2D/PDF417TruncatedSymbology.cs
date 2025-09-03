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
    class PDF417TruncatedSymbology : PDF417Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PDF417TruncatedSymbology"/> class.
        /// </summary>
        public PDF417TruncatedSymbology()
            : base()
        {
            m_type = TrueSymbologyType.PDF417Truncated;
            m_compact = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PDF417TruncatedSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public PDF417TruncatedSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.PDF417Truncated;
            m_compact = true;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "Truncated PDF417 symbology allows a maximum data size of 1850 text characters, or 2710 digits.\n";
        }
    }
}
