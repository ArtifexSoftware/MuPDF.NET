namespace BarcodeWriter.Core.Internal
{
    class GTIN13Symbology : EAN13Symbology
    {
        public GTIN13Symbology()
        {
            m_type = TrueSymbologyType.GTIN13;
        }

        public GTIN13Symbology(SymbologyDrawing prototype) : base(prototype)
        {
            m_type = TrueSymbologyType.GTIN13;
        }

        public override string getValueRestrictions()
        {
            return "GTIN-13 (EAN-13) symbology expects strings with 12 digits to be encoded. Optionally, user may enter 13th digit. In the latter case the last digit (check digit) will be verified.";
        }
    }
}