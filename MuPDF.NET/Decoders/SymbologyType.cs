namespace BarcodeReader.Core
{
    /// <summary>
    /// Describes all supported barcode symbologies (types).
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    enum SymbologyType
	{
        /// <summary>
        /// (0) Indicates that barcode symbology is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// (1) Codabar barcode (Also known as Ames Code, USD-4, NW-7, 
        /// Code 2 of 7). Codabar symbology allows only symbols from this 
        /// string '0123456789-$:/.+' to be encoded. This symbology used 
        /// for example in libraries and blood banks.
        /// </summary>
        Codabar = 1,

        /// <summary>
        /// (2) Code 128 barcode. It is a very effective, high-density
        /// symbology which permits the encoding of alphanumeric (subject to
        /// alphabet selection) data. Code 128 is a very dense code, used
        /// extensively worldwide.
        /// </summary>
        Code128 = 2,

        /// <summary>
        /// (3) GS1-128 symbology (new name for EAN128 symbology).
        /// </summary>
        GS1 = 3,

        /// <summary>
        /// (4) Code 39 barcode (aka USD-3, 3 of 9). Code 39 symbology allows 
        /// all ASCII symbols to be encoded in extended mode or symbols from
        /// this string "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%" in 
        /// standard mode. This symbology used for example by U.S. Government 
        /// and military, required for DoD applications.
        /// </summary>
        Code39 = 4,

        /// <summary>
        /// (5) Interleaved 2 of 5 barcode (also known as Code 2 of 5 Interleaved). 
        /// Interleaved 2 of 5 symbology allows only numeric values to be 
        /// encoded. This symbology is used primarily in the distribution 
        /// and warehouse industry.
        /// </summary>
        I2of5 = 5,
        
        /// <summary>
        /// (6) EAN-13 barcode. Used with consumer products internationally.
        /// EAN-13 symbology allows only numeric values to be encoded.
        /// </summary>
        EAN13 = 6,
        
        /// <summary>
        /// (7) EAN-8 barcode. This symbology is a short version of EAN-13 that
        /// is intended to be used on packaging which would be otherwise 
        /// too small to use one of the other versions. Used with consumer 
        /// products internationally. EAN-8 symbology allows only numeric 
        /// values to be encoded.
        /// </summary>
        EAN8 = 7,
        
        /// <summary>
        /// (8) UPC-A barcode. Used with consumer products in U.S. UPC-A
        /// symbology allows only numeric values to be encoded.
        /// </summary>
        UPCA = 8,
        
        /// <summary>
        /// (9) UPC-E barcode. This symbology is zero-suppression version 
        /// of UPC-A. It is intended to be used on packaging which would 
        /// be otherwise too small to use one of the other versions. The 
        /// code is smaller because it drops out zeros which would otherwise 
        /// occur in a symbol. Used with consumer products in U.S. 
        /// UPC-E symbology allows only numeric values to be encoded.
        /// </summary>
        UPCE = 9,

        /// <summary>
        /// (10) The EAN-2 (Also known as EAN/2 and EAN 2) is a supplement to 
        /// the EAN-13 and UPC-A barcodes. It is often used on magazines 
        /// and periodicals to indicate an issue number.
        /// </summary>
        EAN2 = 10,

        /// <summary>
        /// (11) The EAN-5 (Also known as EAN/5 and EAN 5) is a supplement to
        /// EAN-13 and UPC-A barcodes. It is often used to give a 
        /// suggestion for the price of the book.
        /// </summary>
        EAN5 = 11,

        /// <summary>
        /// (12) PDF417 symbology. This symbology is heavily used in the parcel 
        /// industry. The PDF417 symbology can encode a vast amount of data 
        /// into a small space. This symbology allows a maximum data size of 
        /// 1850 text characters, or 2710 digits. 
        /// </summary>
        PDF417 = 12,

        /// <summary>
        /// (13) DataMatrix symbology. The most popular application for DataMatrix 
        /// is marking small items. The Data Matrix can encode text
        /// and raw data. Usual data size is from a few bytes up to 2 kilobytes.
        /// </summary>
        DataMatrix = 13,

        /// <summary>
        /// (14) QR Code symbology. QR Code initially was used for tracking
        /// parts in vehicle manufacturing, but now QR Codes used in a much
        /// broader context, including both commercial tracking applications
        /// and convenience-oriented applications aimed at mobile phone
        /// users (known as mobile tagging).
        /// </summary>
        QRCode = 14,
		
		/// <summary>
		/// (15) Aztec symbology.
        /// </summary>
		Aztec = 15,

        /// <summary>
        /// (16) Trioptic Code 39 symbology.
        /// </summary>
        TriopticCode39 = 16,

        /// <summary>
        /// (17) Patch Code symbology.
        /// </summary>
        PatchCode = 17,

        /// <summary>
        /// (18) GS1 DataBar Omnidirectional symbology.
        /// </summary>
        GS1DataBarOmnidirectional = 18,

        /// <summary>
        /// (19) GS1 DataBar Expanded symbology.
        /// </summary>
        GS1DataBarExpanded = 19,

        /// <summary>
        /// (20) GS1 DataBar Limited symbology.
        /// </summary>
        GS1DataBarLimited = 20,

        /// <summary>
        /// (21) GS1 DataBar Stacked symbology.
        /// </summary>
        GS1DataBarStacked = 21,

		/// <summary>
		/// (22) GS1 DataBar Expanded Stacked symbology.
		/// </summary>
		GS1DataBarExpandedStacked = 22,

		/// <summary>
		/// (23) MaxiCode symbology.
		/// </summary>
		MaxiCode = 23,
		
		/// <summary>
		/// (24) MICR symbology.
		/// </summary>
		MICR = 24,
		
		/// <summary>
		/// (25) USPS Intelligent Mail symbology.
		/// </summary>
		IntelligentMail = 25,
		
		/// <summary>
		/// (26) Royal Mail symbology.
		/// </summary>
		RoyalMail = 26,
		
		/// <summary>
		/// (27) Royal Mail KIX symbology.
		/// </summary>
		RoyalMailKIX = 27,
		
		/// <summary>
		/// (28) Australian Post 4 State Customer Code symbology.
		/// </summary>
		AustralianPostCode = 28,

		/// <summary>
		/// (29) Codablock F symbology.
		/// </summary>
		CodablockF = 29,
		
		/// <summary>
		/// (30) Code 16K symbology.
		/// </summary>
		Code16K = 30,
        
        /// <summary>
		/// (31) PostNet symbology.
		/// </summary>
        PostNet = 31,
        
        /// <summary>
        /// (32) MicroPDF symbology.
		/// </summary>
        MicroPDF = 32,
		
		/// <summary>
        /// (33) Code 93 symbology.
		/// </summary>
        Code93 = 33,
		
		/// <summary>
        /// (34) MSI symbology.
		/// </summary>
        MSI = 34,

        /// <summary>
        /// (35) ITF-14 symbology (Interleaved 2 of 5 restricted to 14 symbols).
        /// </summary>
        ITF14 = 35,

        /// <summary>
        /// (36) GTIN-14 barcode (also known as Code 2 of 5 Interleaved with 14 digits). 
        /// GTIN-14 symbology allows only 14 numeric values to be 
        /// encoded. This symbology is used primarily in the distribution 
        /// and warehouse industry.
        /// </summary>
        GTIN14 = 36,

        /// <summary>
        /// (37) GTIN-13 barcode. Used with consumer products internationally.
        /// GTIN-13 symbology allows only 13 numeric values to be encoded.
        /// </summary>
        GTIN13 = 37,

        /// <summary>
        /// (38) GTIN-8 barcode. This symbology is a short version of GTIN-13 that
        /// is intended to be used on packaging which would be otherwise 
        /// too small to use one of the other versions. Used with consumer 
        /// products internationally. GTIN-8 symbology allows only 8 numeric 
        /// values to be encoded.
        /// </summary>
        GTIN8 = 38,

        /// <summary>
        /// (39) GTIN-12 barcode. Used with consumer products in U.S. GTIN-12
        /// symbology allows only 12 numeric values to be encoded.
        /// </summary>
        GTIN12 = 39,

        /// <summary>
        /// (40) Circular variation of Interleaved 2 of 5 barcode.
        /// </summary>
        CircularI2of5 = 40,

		/// <summary>
		/// (41) PZN barcode. German Pharmacy Barcode.
		/// </summary>
		PZN = 41,
		
		/// <summary>
		/// (42) Pharmacode, also known as Pharmaceutical Binary Code, is a barcode standard, used in the pharmaceutical industry as a packing control system. 
		/// It is designed to be readable despite printing errors.
		/// </summary>
		Pharmacode = 42,

		/// <summary>
		/// (43) Extended Code 39 barcode symbology.
		/// </summary>
		Code39Ext = 43,
		
		/// <summary>
		/// (44) Code 39 mod 43 barcode symbology. This is Code 39 with modulo 43 check digit.
		/// </summary>
		Code39Mod43 = 44,
		
		/// <summary>
		/// (45) Code 39 mod 43 Extended barcode symbology. This is Extended Code 39 with modulo 43 check digit.
		/// </summary>
		Code39Mod43Ext = 45,
		
		/// <summary>
		/// (46) UPU (Universal Postal Union) barcode symbology. This is the Code 39 with modulo 11 check digit.
		/// </summary>
		UPU = 46,
//#else
		/// <summary>
		/// (47) Segment element of Optical Mark Recognition (OMR).
		/// </summary>
		Segment = 47,
		/// <summary>
		/// (48) Circle element of Optical Mark Recognition (OMR).
		/// </summary>
		Circle = 48,
		/// <summary>
		/// (49) Oval element of Optical Mark Recognition (OMR).
		/// </summary>
		Oval = 49,
		/// <summary>
		/// (50) Checkbox element of Optical Mark Recognition (OMR).
		/// </summary>
		Checkbox = 50,
		/// <summary>
		/// (51) Horizontal line element of Optical Mark Recognition (OMR).
		/// </summary>
		HorizontalLine = 51,
		/// <summary>
		/// (52) Vertical line element of Optical Mark Recognition (OMR).
		/// </summary>
		VerticalLine = 52,
		/// <summary>
		/// (53) Underlined field element of Optical Mark Recognition (OMR).
		/// </summary>
		UnderlinedField = 53,
		/// <summary>
		/// (54) Bordered table element of Optical Mark Recognition (OMR).
		/// </summary>
		Table = 54,
		/// <summary>
		/// (55) DPM (Direct Part Marking) DataMatrix symbology.
		/// </summary>
		DPMDataMatrix = 55,
	}
}
