/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes all supported barcode symbologies (types).
    /// </summary>
#if QRCODESDK
    internal enum SymbologyType
#else
    public enum SymbologyType
#endif
    {
        Unknown = -1,

        /// <summary>
        /// (0) Code 128 barcode (Also known as Code-128). It is a very effective, 
        /// high-density symbology which permits the encoding of alphanumeric 
        /// (subject to alphabet selection) data. Code 128 is a very dense 
        /// code, used extensively worldwide.
        /// </summary>
        Code128 = 0,

        /// <summary>
        /// (1) Code 39 barcode (Also known as USD-3, Code 3 of 9, LOGMARS and 
        /// in extended mode also know as Code39Extended, Code 39 Full ASCII mode). 
        /// Code 39 symbology allows all ASCII symbols to be encoded in 
        /// extended mode or symbols from this string 
        /// "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%" in 
        /// standard mode. This symbology used for example by U.S. Government 
        /// and military, required for DoD applications.
        /// </summary>
        Code39 = 1,

        /// <summary>
        /// (2) Postnet barcode. This symbology usually gets printed by U.S. Post 
        /// Office on envelopes. Postnet symbology allows only numeric
        /// values to be encoded. The bar code itself can encode either a 
        /// standard 5-digit Zip Code, a Zip+4 code, or a full 11-point 
        /// delivery point code.
        /// </summary>
        Postnet = 2,

        /// <summary>
        /// (3) UPC-A barcode (Also known as UPCA). Used with consumer products 
        /// in U.S. UPC-A symbology allows only numeric values to be encoded.
        /// </summary>
        UPCA = 3,

        /// <summary>
        /// (4) EAN-8 barcode (GTIN-8). This symbology is a short version of EAN-13 that
        /// is intended to be used on packaging which would be otherwise 
        /// too small to use one of the other versions. Used with consumer 
        /// products internationally. EAN-8 symbology allows only numeric 
        /// values to be encoded.
        /// </summary>
        EAN8 = 4,
        
        /// <summary>
        /// (5) ISBN Number encoded as EAN-13 barcode. 
        /// </summary>
        ISBN = 5,

        /// <summary>
        /// (6) Codabar barcode (Also known as Ames Code, USD-4, NW-7, 
        /// Code 2 of 7). Codabar symbology allows only symbols from this 
        /// string '0123456789-$:/.+' to be encoded. This symbology used 
        /// for example in libraries and blood banks.
        /// </summary>
        Codabar = 6,

        /// <summary>
        /// (7) Interleaved 2 of 5 barcode (Also know as Code 2 of 5 Interleaved). 
        /// Interleaved 2 of 5 symbology allows only numeric values to be 
        /// encoded. This symbology is used primarily in the distribution 
        /// and warehouse industry.
        /// </summary>
        I2of5 = 7,

        /// <summary>
        /// (8) Code 93 barcode (Also known as USS-93, Code93 and in extended 
        /// mode also known as Code93Extended, Code 93 Full ASCII mode). 
        /// Code 93 symbology allows all ASCII symbols to be encoded in 
        /// extended mode or symbols from this string 
        /// "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%" in 
        /// standard mode. This symbology was designed to 
        /// complement and improve upon Code 39 symbology. Code 93
        /// produces denser code than that of Code 39.
        /// </summary>
        Code93 = 8,

        /// <summary>
        /// (9) EAN-13 barcode (GTIN-13). Used with consumer 
        /// products internationally. EAN-13 symbology allows only numeric 
        /// values to be encoded.
        /// </summary>
        EAN13 = 9,

        /// <summary>
        /// (10) JAN-13 barcode (Also known as JAN codes). This symbology is 
        /// mostly the same as EAN-13 symbology, but used in Japan. JAN 
        /// stands for Japanese Numbering Authority. First two digits of 
        /// JAN-13 symbology are always "49".
        /// </summary>
        JAN13 = 10,

        /// <summary>
        /// (11) Bookland barcode. This symbology is mostly the same 
        /// as EAN-13 symbology, but used exclusively with books. First three 
        /// digits of Bookland symbology are always "978" the rest of code
        /// is a ISBN number without embedded hyphens and ISBN check digit.
        /// </summary>
        Bookland = 11,

        /// <summary>
        /// (12) UPC-E barcode (Also known as UPCE). This symbology is 
        /// zero-suppression version of UPC-A. It is intended to be used on 
        /// packaging which would be otherwise too small to use one of the 
        /// other versions. The code is smaller because it drops out zeros 
        /// which would otherwise occur in a symbol. Used with consumer 
        /// products in U.S. UPC-E symbology allows only numeric values 
        /// to be encoded.
        /// </summary>
        UPCE = 12,

        /// <summary>
        /// (13) PDF417 symbology. This symbology is heavily used in the parcel 
        /// industry. The PDF417 symbology can encode a vast amount of data 
        /// into a small space. This symbology allows a maximum data size of 
        /// 1850 text characters, or 2710 digits. 
        /// </summary>
        PDF417 = 13,

        /// <summary>
        /// (14) PDF417 Truncated symbology. This symbology is a truncted (right 
        /// column is missing) or compact version of PDF417 symbology. 
        /// </summary>
        PDF417Truncated = 14,

        /// <summary>
        /// (15) Data Matrix symbology. The most popular application for Data 
        /// Matrix is marking small items. The Data Matrix can encode text
        /// and raw data. Usual data size is from a few bytes up to 2 kilobytes.
        /// </summary>
        DataMatrix = 15,

        /// <summary>
        /// (16) QR Code symbology. QR Code initially was used for tracking parts in vehicle 
        /// manufacturing, but now QR Codes used in a much broader context, 
        /// including both commercial tracking applications and convenience-oriented 
        /// applications aimed at mobile phone users (known as mobile tagging).
        /// </summary>
        QRCode = 16,

        /// <summary>
        /// (17) Aztec Code symbology. An Aztec code barcode is used by Deutsche Bahn, 
        /// Trenitalia and by Swiss Federal Railways for tickets sold online and 
        /// printed out by customers. The Aztec Code has been selected by the airline 
        /// industry (IATA's BCBP standard) for the electronic boarding passes.
        /// </summary>
        Aztec = 17,

        /// <summary>
        /// (18) PLANET (The Postal Alpha Numeric Encoding Technique) barcode is used by 
        /// the United States Postal Service to identify and track pieces of 
        /// mail during delivery - the Post Office's "CONFIRM" services.
        /// </summary>
        Planet = 18,

        /// <summary>
        /// (19) EAN128 symbology (Also known as EAN-128, EAN-14, Shipping 
        /// Container Code, UCC-14, DUN-14 (Distribution Unit Number), 
        /// SSC-14, GS1-128, UCC-128, UCC/EAN-128. This symbology was developed 
        /// to provide a worldwide format and standard for exchanging common 
        /// data between companies.
        /// </summary>
        EAN128 = 19,

        /// <summary>
        /// (20) GS1-128 symbology (new name for EAN128 symbology)
        /// </summary>
        GS1_128 = 20,

        /// <summary>
        /// (21) USPS Sack Label symbology (Also known as USPS 25 Sack Label). This
        /// is in fact Interleaved 2 of 5 symbology with exactly 8 digits 
        /// encoded: 5-digit Zip Code (the sack destination) and a 3-digit 
        /// content identifier number(CIN).
        /// </summary>
        USPSSackLabel = 21,

        /// <summary>
        /// (22) USPS Tray Label symbology (Also known as USPS 25 Tray Label). This
        /// is in fact Interleaved 2 of 5 symbology with exactly 10 digits 
        /// encoded: 5-digit Zip Code (the tray destination) and a 3-digit 
        /// content identifier number(CIN), and a 2-digit USPS processing code.
        /// </summary>
        USPSTrayLabel = 22,

        /// <summary>
        /// (23) Deutsche Post Identcode (Also known as Deutsche Post AG IdentCode, 
        /// German Postal 2 of 5 IdentCode, Deutsche Frachtpost IdentCode, 
        /// IdentCode, Deutsche Post AG (DHL). This symbology is used by 
        /// German Post (Deutsche Post AG) (Deutsche Frachtpost). The barcode
        /// contains a tracking number providing an identification of the 
        /// customer (sender) and the mail piece. The value to encode length 
        /// is fixed to 11 digits. The value to encode must have the following 
        /// structure: 2 digits for ID of primary distribution center, 3 digits 
        /// for Customer ID, and 6 digits for Mailing number.
        /// </summary>
        DeutschePostIdentcode = 23,

        /// <summary>
        /// (24) Deutsche Post Leitcode (Also known as German Postal 2 of 5 LeitCode, 
        /// LeitCode, CodeLeitcode, Deutsche Post AG (DHL)). This symbology is used by 
        /// German Post (Deutsche Post AG) (Deutsche Frachtpost). The barcode
        /// gives an indication of the destination. The value to encode length 
        /// is fixed to 13 digits. The value to encode must have the following 
        /// structure: 5 digits for Postal Code (Postleitzahl, PLZ), 3 digits 
        /// for Street ID/number, 3 digits for House number, and 2 digits for 
        /// Product code.
        /// </summary>
        DeutschePostLeitcode = 24,

        /// <summary>
        /// (25) Numly barcode (Also known as ESN, Electronic Serial Number). This
        /// barcode is a unique identifier that allows an author or publisher to assign 
        /// to content and track licensing of each id assignment. Numly Numbers 
        /// are useful if you wish to identify each electronic distributed copy 
        /// of any form of electronic media. Numly Numbers can also act a 
        /// third-party content submission time stamps to aid in copyright 
        /// proving instances and emails. The length of value to encode is 
        /// fixed to 19 digits.
        /// </summary>
        Numly = 25,

        /// <summary>
        /// (26) PZN barcode (Also known as Pharma-Zentral-Nummer, Pharmazentralnummer, 
        /// Code PZN, CodePZN, Pharma Zentral Nummer). This symbology is 
        /// used for distribution of pharmaceutical / health care products 
        /// in Germany. The specification of this application is maintained 
        /// at Informationsstelle für Arzneispezialitäten GmbH (IFA). The 
        /// length of value to encode is 6 digits.
        /// </summary>
        PZN = 26,

        /// <summary>
        /// (27) Optical Product Code (Also known as OPC, Vision Council of America 
        /// OPC, VCA BarCode, VCA OPC) symbology. This symbology is used for 
        /// marking retail optical products. The OPC is a 9-digit, numeric 
        /// code that identifies the product and the manufacturer. The 
        /// structure of the OPC code is the following: 5 digits for 
        /// Manufacturer Identification Number assigned by the Optical 
        /// Product Code Council, Inc., 4 digits for Item Identification Number 
        /// assigned and controlled by the optical manufacturer.
        /// </summary>
        OpticalProduct = 27,

        /// <summary>
        /// (28) Swiss Post Parcel (Also known as SwissPost Parcel Barcode, 
        /// Switzerland Post Parcel Barcode, Swiss PostParcel Barcode) symbology.
        /// This symbology is used by Swiss Post. It identifies each parcel 
        /// and serves as a means of verifying mailing and delivery and 
        /// checking the service offering. All parcels must have a unique 
        /// barcode. The barcode is the requirement for automated processing. 
        /// The barcode serves as a means of identifying the item. The 
        /// structure of the Swiss Post Parcel barcode is 18 numeric digits: 
        /// 2 digits for Swiss Post reference, 8 digits for Franking license 
        /// number, and 8 digits for Item number.
        /// </summary>
        SwissPostParcel = 28,

        /// <summary>
        /// (29) Royal Mail barcode (Also known as RMS4CC, RoyalMail4SCC, 
        /// Royal Mail 4-State, British Royal Mail 4-State Customer Code,
        /// 4-State). This symbology was created for automated mail sorting.
        /// It normally codes the postcode and the house or mailbox number 
        /// in a machine readable format. The contents of the code may vary 
        /// in different countries. This symbology encodes alpha-numeric 
        /// characters (0-9, A-Z).
        /// </summary>
        RoyalMail = 29,

        /// <summary>
        /// (30) Dutch KIX barcode (Also known as Royal TNT Post Kix, Dutch 
        /// KIX 4-State Barcode, Kix Barcode, TPG KIX, Klantenindex Barcode,
        /// TPGPOST KIX). This symbology is used by Royal Dutch TPG Post 
        /// (Netherlands) for Postal code and automatic mail sorting. It 
        /// provides information about the address of the receiver. This 
        /// symbology encodes alpha-numeric characters (0-9, A-Z).
        /// </summary>
        DutchKix = 30,

        /// <summary>
        /// (31) Singapore 4-State Postal Code barcode (Also known as Singapore 
        /// 4-State Postal, SingPost 4-State, SingPost Barcode, Singapore 
        /// 4-State Code). This Symbology is used by Singapore Post 
        /// (SingPost) for Postal code and automatic mail sorting. Such
        /// barcode provides information about the address of the receiver.
        /// This symbology encodes alpha-numeric characters (0-9, A-Z).
        /// </summary>
        SingaporePostalCode = 31,

        /// <summary>
        /// (32) The EAN-2 (Also known as EAN/2 and EAN 2) is a supplement to 
        /// the EAN-13 and UPC-A barcodes. It is often used on magazines 
        /// and periodicals to indicate an issue number.
        /// </summary>
        EAN2 = 32,

        /// <summary>
        /// (33) The EAN-5 (Also known as EAN/5 and EAN 5) is a supplement to
        /// EAN-13 and UPC-A barcodes. It is often used to give a 
        /// suggestion for the price of the book.
        /// </summary>
        EAN5 = 33,

        /// <summary>
        /// (34) The EAN14 symbology is used for traded goods.
        /// </summary>
        EAN14 = 34,

        /// <summary>
        /// (35) The Macro version of PDF417 Symbology.
        /// </summary>
        MacroPDF417 = 35,

        /// <summary>
        /// (36) The Micro version of PDF417 Symbology.
        /// </summary>
        MicroPDF417 = 36,

        /// <summary>
        /// (37) GS1 DataMatrix is a 2D (two-dimensional) barcode that holds
        /// large amounts of data in a relatively small space. These barcodes
        /// are used primarily in aerospace, pharmaceuticals, medical device
        /// manufacturing, and by the U.S. Department of Defense to add
        /// visibility to the value chain. GS1 DataMatrix can be used for parts
        /// that need to be tracked in the manufacturing process because the
        /// barcode allows users to encode a variety of information related
        /// to the product, such as date or lot number. They are not intended
        /// to be used on items that pass through retail point-of-sale (POS).
        /// </summary>
        GS1_DataMatrix = 37,

        /// <summary>
        /// (38) The Telepen symbology. This symbology is used in many countries and
        /// very widely in the UK. Most Universities and other academic libraries use
        /// Telepen, as do many public libraries. Other users include the motor
        /// industry, Ministry of Defence and innumerable well-known organisations
        /// for many different applications.
        /// </summary>
        Telepen = 38,

        /// <summary>
        /// (39) The Intelligent Mail Barcode symbology. 
        /// This symbology is used in the USPS mailstream. It is also known as
        /// the USPS OneCode Solution or USPS 4-State Customer Barcode (abbreviated
        /// 4CB, 4-CB, or USPS4CB)
        /// </summary>
        IntelligentMail = 39,

        /// <summary>
        /// (40) The GS1 DataBar Omnidirectional symbology. 
        /// </summary>
        GS1_DataBar_Omnidirectional = 40,

        /// <summary>
        /// (41) The GS1 DataBar Truncated symbology. 
        /// </summary>
        GS1_DataBar_Truncated = 41,

        /// <summary>
        /// (42) The GS1 DataBar Stacked symbology. 
        /// </summary>
        GS1_DataBar_Stacked = 42,

        /// <summary>
        /// (43) The GS1 DataBar Stacked Omnidirectional symbology. 
        /// </summary>
        GS1_DataBar_Stacked_Omnidirectional = 43,

        /// <summary>
        /// (44) The GS1 DataBar Limited symbology. 
        /// </summary>
        GS1_DataBar_Limited = 44,

        /// <summary>
        /// (45) The GS1 DataBar Expanded symbology. 
        /// </summary>
        GS1_DataBar_Expanded = 45,

        /// <summary>
        /// (46) The GS1 DataBar Expanded Stacked symbology. 
        /// </summary>
        GS1_DataBar_Expanded_Stacked = 46,

        /// <summary>
        /// (47) The MaxiCode symbology.
        /// </summary>
        MaxiCode = 47,

        /// <summary>
        /// (48) The Plessey Code symbology.
        /// This symbology is used primarily in libraries and for retail grocery shelf marking.
        /// </summary>
        Plessey = 48,

        /// <summary>
        /// (49) The MSI  (also known as Modified Plessey) symbology.
        /// This symbology is used primarily for inventory control,
        /// marking storage containers and shelves in warehouse environments.
        /// </summary>
        MSI = 49,

        /// <summary>
        /// (50) The ITF-14 (GTIN-14, UCC-14) symbology.
        /// ITF-14 is the GS1 implementation of an Interleaved 2 of 5 bar code to encode
        /// a Global Trade Item Number. ITF-14 symbols are generally used on a packaging
        /// step of products. The ITF-14 always encodes 14 digits.
        /// </summary>
        ITF14 = 50,

        /// <summary>
        /// (51) The GTIN-12 (12-digit UPC-A) symbology.
        /// GTIN-12 is a 12-digit number used primarily in North America.
        /// </summary>
        GTIN12 = 51,

        /// <summary>
        /// (52) The GTIN-8 (EAN-8, UCC-8): this is an 8-digit number used predominately outside of North America.
        /// </summary>
        GTIN8 = 52,
        
        /// <summary>
        /// (53) The GTIN-13 (EAN-13, UCC-13): this is an 13-digit number used predominately outside of North America.
        /// </summary>
        GTIN13 = 53,
        
        /// <summary>
        /// (54) The GTIN-14 (ITF-14, EAN-14, UCC-14) symbology.
        /// GTIN-14 is the GS1 implementation of an Interleaved 2 of 5 bar code to encode
        /// a Global Trade Item Number. GTIN-14 symbols are generally used on a packaging
        /// step of products. The GTIN-14 always encodes 14 digits.
        /// </summary>
        GTIN14 = 54,

        /// <summary>
        /// (55) GS1 QRCode 
        /// </summary>
        GS1_QRCode = 55,

        /// <summary>
        /// (56) Pharma Code
        /// </summary>
        PharmaCode = 56,
    }
}
