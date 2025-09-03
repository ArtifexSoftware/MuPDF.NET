namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using GTIN-12 symbology rules. GTIN-12 is UPC-A with 12 digits.
    /// </summary>
    class GTIN12Symbology : UPCASymbology
    {
        public GTIN12Symbology()
        {
            m_type = TrueSymbologyType.GTIN12;
        }

        public GTIN12Symbology(SymbologyDrawing prototype) : base(prototype)
        {
            m_type = TrueSymbologyType.GTIN12;
        }

        public override string getValueRestrictions()
        {
            return "GTIN-12 (UPC-A) symbology expects strings with 11 digits to be encoded. Optionally, user may enter 12th digit. In the latter case the last digit (check digit) will be verified.";
        }
    }
}