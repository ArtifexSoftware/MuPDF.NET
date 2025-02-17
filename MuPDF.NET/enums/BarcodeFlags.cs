namespace MuPDF.NET
{
    public enum BarcodeFormat
    {
        AZTEC = 1,
        CODABAR = 2,
        CODE_39 = 4,
        CODE_93 = 8,
        CODE_128 = 0x10,
        DATA_MATRIX = 0x20,
        EAN_8 = 0x40,
        EAN_13 = 0x80,
        ITF = 0x100,
        MAXICODE = 0x200,
        PDF_417 = 0x400,
        QR_CODE = 0x800,
        RSS_14 = 0x1000,
        RSS_EXPANDED = 0x2000,
        UPC_A = 0x4000,
        UPC_E = 0x8000,
        UPC_EAN_EXTENSION = 0x10000,
        MSI = 0x20000,
        PLESSEY = 0x40000,
        IMB = 0x80000,
        PHARMA_CODE = 0x100000,
        All_1D = 0xF1DE
    }

    public enum BarcodeMetadataType
    {
        OTHER,
        ORIENTATION,
        BYTE_SEGMENTS,
        ERROR_CORRECTION_LEVEL,
        ISSUE_NUMBER,
        SUGGESTED_PRICE,
        POSSIBLE_COUNTRY,
        UPC_EAN_EXTENSION,
        STRUCTURED_APPEND_SEQUENCE,
        STRUCTURED_APPEND_PARITY,
        PDF417_EXTRA_METADATA,
        AZTEC_EXTRA_METADATA,
        SYMBOLOGY_IDENTIFIER,
        QR_MASK_PATTERN
    }
}
