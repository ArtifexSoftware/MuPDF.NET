
using System;
using System.Text;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Level of error correction in Aztec Code symbols
    /// </summary>
    public enum AztecErrorCorrectionLevel
    {
        /// <summary>
        /// (0) Library will choose best possible error correction level.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// (1) Level 1 error correction. 10% of data region will be filled by error correction data.
        /// </summary>
        Level1 = 1,

        /// <summary>
        /// (2) Level 2 error correction. 23% of data region will be filled by error correction data.
        /// </summary>
        Level2 = 2,

        /// <summary>
        /// (3) Level 3 error correction. 36% of data region will be filled by error correction data.
        /// </summary>
        Level3 = 3,

        /// <summary>
        /// (4) Level 1 error correction. 50% of data region will be filled by error correction data.
        /// </summary>
        Level4 = 4
    }
}
