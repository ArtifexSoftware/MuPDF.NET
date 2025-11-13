using SkiaSharp;
using System;
using System.Drawing;

namespace BarcodeWriter.Core
{
    interface ISymbologyOptions
    {
        AztecErrorCorrectionLevel AztecErrorCorrectionLevel { get; set; }
        int AztecSymbolSize { get; set; }
        CodabarChecksumAlgorithm CodabarChecksumAlgorithm { get; set; }
        CodabarSpecialSymbol CodabarStartSymbol { get; set; }
        CodabarSpecialSymbol CodabarStopSymbol { get; set; }
        Code128Alphabet Code128Alphabet { get; set; }
        DataMatrixCompactionMode DataMatrixCompactionMode { get; set; }
        AztecCompactionMode AztecCompactionMode { get; set; }
        DataMatrixSize DataMatrixSize { get; set; }
        bool DrawIntercharacterGap { get; set; }
        bool ISBNAutoCaption { get; set; }
        string ISBNCaptionTemplate { get; set; }
        int PDF417ColumnCount { get; set; }
        PDF417CompactionMode PDF417CompactionMode { get; set; }
        bool PDF417CreateMacro { get; set; }
        PDF417ErrorCorrectionLevel PDF417ErrorCorrectionLevel { get; set; }
        int PDF417FileID { get; set; }
        bool PDF417LastSegment { get; set; }
        int PDF417MinimumColumnCount { get; set; }
        int PDF417RowCount { get; set; }
        int PDF417SegmentIndex { get; set; }
        bool PDF417UseManualSize { get; set; }
        QREncodeHint QREncodeHint { get; set; }
        QRErrorCorrectionLevel QRErrorCorrectionLevel { get; set; }
        int QRVersion { get; set; }
        bool ShowStartStop { get; set; }
        int SupplementSpace { get; set; }
        int GS1ExpandedStackedSegmentsNumber { get; set; }
        byte MaxiCodeMode { get; set; }
        bool MaxiCodeEnableCustomWidth { get; set; }
        MSIChecksumAlgorithm MSIChecksumAlgorithm { get; set; }
        bool OnlyHorizontalBearerBar { get; set; }
        PZNType PZNType { get; set; }
		int TextEncodingCodePage { get; set; }
		bool TextEncodingUseUTF8 { get; set; }

        bool PharmaCodeTwoTrack { get; set; }
        bool PharmaCodeSupplementaryCode { get;set; }
        SKColor PharmaCodeSupplementaryBarColor { get; set; }
        bool PharmaCodeMiniature { get; set; }
    }
}
