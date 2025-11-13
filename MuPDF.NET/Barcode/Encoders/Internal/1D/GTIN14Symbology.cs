namespace BarcodeWriter.Core.Internal
{
    class GTIN14Symbology : ITF14Symbology
    {
        public GTIN14Symbology()
        {
            m_type = TrueSymbologyType.GTIN14;
        }

        public GTIN14Symbology(SymbologyDrawing prototype) : base(prototype)
        {
            m_type = TrueSymbologyType.GTIN14;
        }

        public override string getValueRestrictions()
        {
            return "GTIN-14 (ITF-14) symbology expects strings with 13 digits to be encoded. Optionally, user may enter 14th digit. In the latter case the last digit (check digit) will be verified.";
        }
    }
}