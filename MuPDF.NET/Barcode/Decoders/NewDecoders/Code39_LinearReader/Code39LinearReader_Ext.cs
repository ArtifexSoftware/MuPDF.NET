using System;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
using System.Text;
using BarcodeReader.Core.Common;


namespace BarcodeReader.Core.Code39
{
    /// <summary>
    /// Code39 reader with extended character set.
    /// </summary>
    [Obsolete("This class does not pass all tests.")]
#if CORE_DEV
    public
#else
    internal
#endif
    class Code39ExtendedLinearReader : Code39LinearReader
    {
        public Code39ExtendedLinearReader()
        {
            DoDecodeExtended = true;
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.Code39Ext;
        }
    }

    /// <summary>
    /// Code39 reader with CheckMod43Checksum.
    /// </summary>
    [Obsolete("This class does not pass all tests.")]
#if CORE_DEV
    public
#else
    internal
#endif
    class Code39Mod43LinearReader : Code39LinearReader
    {
        public Code39Mod43LinearReader()
        {
            CheckSumMode = Mode.CheckMod43Checksum;
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.Code39Mod43;
        }
    }

    /// <summary>
    /// Code39 reader with CheckMod43Checksuma and extended charset.
    /// </summary>
    [Obsolete("This class does not pass all tests.")]
#if CORE_DEV
    public
#else
    internal
#endif
    class Code39Mod43ExtLinearReader : Code39LinearReader
    {
        public Code39Mod43ExtLinearReader()
        {
            DoDecodeExtended = true;
            CheckSumMode = Mode.CheckMod43Checksum;
        }

        public override SymbologyType GetBarCodeType()
        {
            return SymbologyType.Code39Mod43Ext;
        }
    }
}