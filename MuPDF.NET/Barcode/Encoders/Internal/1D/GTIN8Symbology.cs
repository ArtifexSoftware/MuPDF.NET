namespace BarcodeWriter.Core.Internal
{
    class GTIN8Symbology : EAN8Symbology
    {
        public GTIN8Symbology()
        {
            m_type = TrueSymbologyType.GTIN8;
        }

        public GTIN8Symbology(SymbologyDrawing prototype) : base(prototype)
        {
            m_type = TrueSymbologyType.GTIN8;
        }

        public override string getValueRestrictions()
        {
            return "GTIN-8 (EAN-8) symbology expects strings with 7 digits to be encoded. Optionally, user may enter 8th digit. In the latter case the last digit (check digit) will be verified.";
        }
    }
}