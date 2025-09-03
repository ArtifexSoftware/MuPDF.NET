/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Describes symbology specific options.
    /// </summary>
#if QRCODESDK
    internal class SymbologyOptions : ICloneable
#else
    [TypeConverter(typeof(ExpandableObjectConverter)), ClassInterface(ClassInterfaceType.AutoDual)]
    public class SymbologyOptions : ICloneable, ISymbologyOptions
#endif
    {
        /// <summary>
        /// Occurs when options get changed.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Raises the <see cref="E:Changed"/> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        protected virtual void FireChanged(object sender)
        {
            Changed?.Invoke(sender, null);
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show start and stop symbology symbols in caption text.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if to show start and stop symbology symbols in caption text; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Note that not all symbologies support this option. Some of the symbologies simply do not
        /// have printable start or stop symbol.
        /// </remarks>
        [Description("A value indicating whether to show start and stop symbology symbols in caption text.")]
        public bool ShowStartStop
        {
            get => _showStartStop;
            set
            {
                _showStartStop = value;
                FireChanged(this);
            }
        }
        private bool _showStartStop;

        /// <summary>
        /// Gets or sets a value indicating whether to draw intercharacter gaps.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if to draw intercharacter gaps; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Note that not all symbologies support this option.
        /// </remarks>
        [Description("A value indicating whether to draw intercharacter gaps.")]
        public bool DrawIntercharacterGap
        {
            get => _drawIntercharacterGap;
            set
            {
                _drawIntercharacterGap = value;
                FireChanged(this);
            }
        }
        private bool _drawIntercharacterGap = true;

        /// <summary>
        /// Gets or sets the space (in pixels) between main and supplemental barcode.
        /// </summary>
        /// <value>The space (in pixels) between main and supplemental barcode.</value>
        [Description("The space (in pixels) between main and supplemental barcode.")]
        public int SupplementSpace
        {
            get => _supplementSpace;
            set 
            {
                _supplementSpace = value;
                FireChanged(this);
            }
        }
        private int _supplementSpace = 5;

        /// <summary>
        /// Gets or sets the character encoding of barcode value for 2D barcode types (QR Code, PDF417, DataMatrix, Aztec, MaxiCode).
        /// Default is <see cref="System.Text.Encoding.Default"/> (ANSI).
        /// </summary>
        /// <value>The character encoding.</value>
        public Encoding Encoding
        {
            get => _encoding;
            set
            {
                _encoding = value;
                FireChanged(this);
            }
        }
        private Encoding _encoding = System.Text.Encoding.Default;

        /// <summary>
        /// Gets or sets the character encoding of barcode value.
        /// Default is <see cref="System.Text.Encoding.Default"/> - the default encoding on your computer.
        /// You may override this value like:
        /// <code>
        /// barcode.Options.TextEncodingCodePage = 1250; // to use German locale to decode text
        /// </code>
        /// .NET interface: please use <see cref="Encoding"/> property instead
        /// Some of available code pages are listed below for your reference:
        /// <code>
        /// Name               CodePage  EncodingName
        /// shift_jis          932       Japanese (Shift-JIS)
        /// windows-1250       1250      Central European (Windows)
        /// windows-1251       1251      Cyrillic (Windows)
        /// Windows-1252       1252      Western European (Windows)
        /// windows-1253       1253      Greek (Windows)
        /// windows-1254       1254      Turkish (Windows)
        /// csISO2022JP        50221     Japanese (JIS-Allow 1 byte Kana)
        /// iso-2022-kr        50225     Korean (ISO)
        /// </code>
        /// </summary>
        public int TextEncodingCodePage
        {
            get => _encoding.CodePage;
            set
            {
                _encoding = System.Text.Encoding.GetEncoding(value);
                FireChanged(this);
            }
        }

        /// <summary>
        /// ActiveX interface: Enables UTF8 text encoding for use for barcode value decoding 
        /// .NET interface: Use .TextEncoding property and set it to System.Text.Encoding.UTF8 if you need to
        /// </summary>
        public bool TextEncodingUseUTF8
        {
            get => Equals(_encoding, System.Text.Encoding.UTF8);
            set
            {
                _encoding = value ? System.Text.Encoding.UTF8 : System.Text.Encoding.Default;
                FireChanged(this);
            }
        }

        /// <summary>
        /// Gets or sets the hint to use when encoding non-alphanumeric data while 
        /// creating QR Code barcodes.
        /// </summary>
        /// <value>The hint to use when encoding non-alphanumeric data while 
        /// creating QR Code barcodes.</value>
        [Description("A hint to use when encoding non-alphanumeric data while creating QR Code barcodes.")]
        public QREncodeHint QREncodeHint
        {
            get => _qrHint;
            set
            {
                _qrHint = value;
                FireChanged(this);
            }
        }
        private QREncodeHint _qrHint = QREncodeHint.Mode8;

        /// <summary>
        /// Gets or sets the error correction level for QR Code barcodes.
        /// </summary>
        /// <value>The error correction level for QR Code barcodes.</value>
        [Description("An error correction level for QR Code barcodes.")]
        public QRErrorCorrectionLevel QRErrorCorrectionLevel
        {
            get => _qrErrorCorrectionLevel;
            set
            {
                _qrErrorCorrectionLevel = value;
                FireChanged(this);
            }
        }
        private QRErrorCorrectionLevel _qrErrorCorrectionLevel = QRErrorCorrectionLevel.Low;

        /// <summary>
        /// Gets or sets the minimum version (size) for QR Code barcodes.
        /// </summary>
        /// <value>The minimum version (size) for QR Code barcodes. Should be in [0..40] range.</value>
        [Description("A minimum version (size) for QR Code barcodes. Should be in [0..40] range.")]
        public int QRVersion
        {
            get => _qrVersion;
            set
            {
                if (value < 0 || value > 40)
                    throw new BarcodeException("Minimum version should be in [0..40] range");

                _qrVersion = value;
                FireChanged(this);
            }
        }
        private int _qrVersion;

#if !QRCODESDK
        /// <summary>
        /// Gets or sets the algorithm to use for Codabar symbology checksum calculation.
        /// </summary>
        /// <value>The algorithm to use for Codabar symbology checksum calculation.</value>
        [Description("The algorithm to use for Codabar symbology checksum calculation.")]
        public CodabarChecksumAlgorithm CodabarChecksumAlgorithm
        {
            get => _codabarChecksumAlgorithm;
            set
            {
                _codabarChecksumAlgorithm = value;
                FireChanged(this);
            }
        }
        private CodabarChecksumAlgorithm _codabarChecksumAlgorithm = CodabarChecksumAlgorithm.Modulo9;

        /// <summary>
        /// Gets or sets the symbol to use as stop symbol in Codabar symbology.
        /// </summary>
        /// <value>The symbol to use as stop symbol in Codabar symbology.</value>
        [Description("The symbol to use as stop symbol in Codabar symbology.")]
        public CodabarSpecialSymbol CodabarStopSymbol
        {
            get => _codabarStopSymbol;
            set
            {
                _codabarStopSymbol = value;
                FireChanged(this);
            }
        }
        private CodabarSpecialSymbol _codabarStopSymbol = CodabarSpecialSymbol.A;

        /// <summary>
        /// Gets or sets the symbol to use as start symbol in Codabar symbology.
        /// </summary>
        /// <value>The symbol to use as start symbol in Codabar symbology.</value>
        [Description("The symbol to use as start symbol in Codabar symbology.")]
        public CodabarSpecialSymbol CodabarStartSymbol
        {
            get => _codabarStartSymbol;
            set
            {
                _codabarStartSymbol = value;
                FireChanged(this);
            }
        }
        private CodabarSpecialSymbol _codabarStartSymbol = CodabarSpecialSymbol.A;

        /// <summary>
        /// Gets or sets the alphabet to use for Code 128 symbology.
        /// </summary>
        /// <value>The alphabet to use for Code 128 symbology.</value>
        [Description("The alphabet to use for Code 128 symbology.")]
        public Code128Alphabet Code128Alphabet
        {
            get => _code128Alphabet;
            set
            {
                _code128Alphabet = value;
                FireChanged(this);
            }
        }
        private Code128Alphabet _code128Alphabet = Code128Alphabet.Auto;

        /// <summary>
        /// Gets or sets the alphabet to use for Telepen symbology.
        /// </summary>
        /// <value>The alphabet to use for Telepen symbology.</value>
        [Description("The alphabet to use for Telepen symbology.")]
        public TelepenAlphabet TelepenAlphabet
        {
            get => _telepenAlphabet;
            set
            {
                _telepenAlphabet = value;
                FireChanged(this);
            }
        }
        private TelepenAlphabet _telepenAlphabet = TelepenAlphabet.Auto;

        /// <summary>
        /// Gets or sets the minimum data column count for PDF417 barcodes.
        /// </summary>
        /// <value>The minimum data column count for PDF417 barcodes.</value>
        [Description("A minimum data column count for PDF417 barcodes.")]
        public int PDF417MinimumColumnCount
        {
            get => _pdf417MinColumnCount;
            set
            {
                _pdf417MinColumnCount = value;
                FireChanged(this);
            }
        }
        private int _pdf417MinColumnCount;

        /// <summary>
        /// This property is used only when PDF417UseManualSize == true.
        /// Gets or sets the exact data column count for PDF417 barcodes.
        /// </summary>
        /// <value>The exact data column count for PDF417 barcodes.</value>
        [Description("The exact data column count for PDF417 barcodes.")]
        public int PDF417ColumnCount
        {
            get => _pdf417ColumnCount;
            set
            {
                _pdf417ColumnCount = value;
                FireChanged(this);
            }
        }
        private int _pdf417ColumnCount;

        /// <summary>
        /// This property is used only when PDF417UseManualSize == true.
        /// Gets or sets the exact data row count for PDF417 barcodes.
        /// Set this property to zero to automatically calculate it according to the given column count.
        /// </summary>
        /// <value>The exact data row count for PDF417 barcodes.</value>
        [Description("The exact data row count for PDF417 barcodes.")]
        public int PDF417RowCount
        {
            get => _pdf417RowCount;
            set
            {
                _pdf417RowCount = value;
                FireChanged(this);
            }
        }
        private int _pdf417RowCount;

        /// <summary>
        /// Gets a value indicating whether PDF417 barcodes should use exact row and column
        /// count (<see cref="PDF417ColumnCount"/> and <see cref="PDF417RowCount"/>)
        /// instead of minimal column count <see cref="PDF417MinimumColumnCount"/>.
        /// </summary>
        /// <value>A value indicating whether PDF417 barcodes should use exact row and column
        /// count instead of minimal column count.</value>
        [Description("A value indicating whether PDF417 barcodes should use exact row and column count set by RowCount and ColumnCount or automatically calculated size (with limitation for a minimal column count).")]
        public bool PDF417UseManualSize
        {
            get => _pdf417SizeIsManual;
            set
            {
                _pdf417SizeIsManual = value;
                FireChanged(this);
            }
        }
        private bool _pdf417SizeIsManual;

        /// <summary>
        /// Gets or sets the compaction mode to use while creating barcodes of PDF417 family.
        /// </summary>
        /// <value>The compaction mode to use while creating barcodes of PDF417 family.</value>
        [Description("A compaction mode to use while creating barcodes of PDF417 family.")]
        public PDF417CompactionMode PDF417CompactionMode
        {
            get => _pdf417CompactionMode;
            set
            {
                _pdf417CompactionMode = value;
                FireChanged(this);
            }
        }
        private PDF417CompactionMode _pdf417CompactionMode = PDF417CompactionMode.Auto;

        /// <summary>
        /// Gets or sets the error correction level for PDF417 barcodes.
        /// </summary>
        /// <value>The error correction level for PDF417 barcodes.</value>
        [Description("An error correction level for PDF417 barcodes.")]
        public PDF417ErrorCorrectionLevel PDF417ErrorCorrectionLevel
        {
            get => _pdf417ErrorCorrectionLevel;
            set
            {
                _pdf417ErrorCorrectionLevel = value;
                FireChanged(this);
            }
        }
        private PDF417ErrorCorrectionLevel _pdf417ErrorCorrectionLevel = PDF417ErrorCorrectionLevel.Auto;

        /// <summary>
        /// Gets or sets a value indicating whether to create PDF417 barcode as 
        /// part of Macro PDF417 sequence.
        /// </summary>
        /// <value><c>true</c> if PDF417 barcode should be created as part of 
        /// Macro PDF417 sequence; otherwise, <c>false</c>.</value>
        [Description("Whether to create PDF417 barcode as part of Macro PDF417 sequence.")]
        public bool PDF417CreateMacro
        {
            get => _pdf417Macro;
            set
            {
                _pdf417Macro = value;
                FireChanged(this);
            }
        }
        private bool _pdf417Macro;

        /// <summary>
        /// Gets or sets File ID value for Macro PDF417 barcodes.
        /// </summary>
        /// <value>The File ID value for Macro PDF417 barcodes.</value>
        [Description("File ID value for Macro PDF417 barcodes. Should be in [0..899] range.")]
        public int PDF417FileID
        {
            get => _pdf417FileID;
            set
            {
                var hi = value / 1000;
                var lo = value % 1000;

                if (hi < 0 || hi > 899 || lo < 0 || lo > 899)
                    throw new BarcodeException("Improper PDF417 File ID value.");

                _pdf417FileID = value;
                FireChanged(this);
            }
        }
        private int _pdf417FileID;
        
        /*/// <summary>
        /// Custom File ID for Macro PDF417 barcode in the form `\001\002\003`.
        /// </summary>
        /// <value>Extended File ID value for Macro PDF417 barcodes.</value>
        public string PDF417ExtendedFileID
        {
            get => _pdf417ExtendedFileID;
            set
            {
                _pdf417ExtendedFileID = value;
                FireChanged(this);
            }
        }
        private string _pdf417ExtendedFileID;*/

        /// <summary>
        /// Gets or sets Segment Index value for current Macro PDF417 barcode.
        /// </summary>
        /// <value>The Segment Index value for current Macro PDF417 barcode.</value>
        [Description("Segment Index value for current Macro PDF417 barcode. Should be in [0..99998] range.")]
        public int PDF417SegmentIndex
        {
            get => _pdf417SegmentIndex;
            set
            {
                if (value < 0 || value > 99998)
                    throw new BarcodeException("PDF417 Segment Index should be in [0..99998] range.");

                _pdf417SegmentIndex = value;
                FireChanged(this);
            }
        }
        private int _pdf417SegmentIndex;

        /// <summary>
        /// Gets or sets a value indicating if current Macro PDF 417 barcode 
        /// should be marked as last segment of sequence.
        /// </summary>
        /// <value><c>true</c> if current Macro PDF 417 barcode should be marked 
        /// as last segment of sequence.; otherwise, <c>false</c>.</value>
        [Description("Whether current Macro PDF 417 barcode should be marked as last segment of sequence.")]
        public bool PDF417LastSegment
        {
            get => _pdf417MacroLastSegment;
            set
            {
                _pdf417MacroLastSegment = value;
                FireChanged(this);
            }
        }
        private bool _pdf417MacroLastSegment = true;

        /// <summary>
        /// Gets or sets the symbol size for Data Matrix barcodes.
        /// </summary>
        /// <value>The symbol size for Data Matrix barcodes.</value>
        [Description("A the symbol size for Data Matrix barcodes.")]
        public DataMatrixSize DataMatrixSize
        {
            get => _dataMatrixSize;
            set
            {
                _dataMatrixSize = value;
                FireChanged(this);
            }
        }
        private DataMatrixSize _dataMatrixSize = DataMatrixSize.AutoSquareSize;

        /// <summary>
        /// Gets or sets the compaction mode to use while creating Data Matrix barcodes.
        /// </summary>
        /// <value>The compaction mode to use while creating Data Matrix barcodes.</value>
        [Description("A compaction mode to use while creating Data Matrix barcodes.")]
        public DataMatrixCompactionMode DataMatrixCompactionMode
        {
            get => _dataMatrixCompactionMode;
            set
            {
                _dataMatrixCompactionMode = value;
                FireChanged(this);
            }
        }
        private DataMatrixCompactionMode _dataMatrixCompactionMode = DataMatrixCompactionMode.Auto;

        /// <summary>
        /// Alternative Reed-Solomon error correction for DataMatrix barcodes of 144x144 size, might be required for compatibility with some hardware barcode scanners.
        /// </summary>
        [Description("Alternative Reed-Solomon error correction for DataMatrix barcodes of 144x144 size, for compatibility with some hardware barcode scanners.")]
		public bool DataMatrixAlternativeReedSolomonCorrectionFor144x144Size
		{
			get => _dataMatrixAlternativeReedSolomonCorrectionFor144x144Size;
            set
			{
				_dataMatrixAlternativeReedSolomonCorrectionFor144x144Size = value;
				FireChanged(this);
			}
		}
        private bool _dataMatrixAlternativeReedSolomonCorrectionFor144x144Size = false;

        /// <summary>
        /// Gets or sets the compaction mode to use while creating Aztec barcodes.
        /// </summary>
        /// <value>The compaction mode to use while creating Aztec barcodes.</value>
        [Description("A compaction mode to use while creating Aztec barcodes.")]
        public AztecCompactionMode AztecCompactionMode
        {
            get => _aztecCompactionMode;
            set
            {
                _aztecCompactionMode = value;
                FireChanged(this);
            }
        }
        private AztecCompactionMode _aztecCompactionMode = AztecCompactionMode.Auto;

        /// <summary>
        /// Gets or sets a value indicating whether to draw auto created additional caption when encoding barcodes using ISBN symbology..
        /// </summary>
        /// <value><c>true</c> if auto created additional caption for ISBN barcodes should be drawn; otherwise, <c>false</c>.</value>
        [Description("A value indicating whether to draw auto created additional caption when encoding barcodes using ISBN symbology.")]
        public bool ISBNAutoCaption
        {
            get => _ISBNAutoCaption;
            set
            {
                _ISBNAutoCaption = value;
                FireChanged(this);
            }
        }
        private bool _ISBNAutoCaption = true;

        /// <summary>
        /// Gets or sets the ISBN caption template (e.g.  #-#######-#-#).
        /// </summary>
        /// <value>The ISBN caption template.</value>
        [Description("The ISBN caption template.")]
        public string ISBNCaptionTemplate
        {
            get => _ISBNCaptionTemplate;
            set 
            { 
                _ISBNCaptionTemplate = value;
                FireChanged(this);
            }
        }
        private string _ISBNCaptionTemplate = string.Empty;

        /// <summary>
        /// Gets or sets the error correction level for Aztec Code barcodes.
        /// </summary>
        /// <value>The error correction level for Aztec Code barcodes.</value>
        [Description("An error correction level for Aztec Code barcodes.")]
        public AztecErrorCorrectionLevel AztecErrorCorrectionLevel
        {
            get => _aztecErrorCorrectionLevel;
            set
            {
                _aztecErrorCorrectionLevel = value;
                FireChanged(this);
            }
        }
        private AztecErrorCorrectionLevel _aztecErrorCorrectionLevel = AztecErrorCorrectionLevel.Auto;

        /// <summary>
        /// Gets or sets the minimum size for Aztec Code barcodes.
        /// </summary>
        /// <value>The version minimum size for Aztec Code barcodes.</value>
        [Description("A minimum size for Aztec Code barcodes. Should be in [0..36] range.")]
        public int AztecSymbolSize
        {
            get => _aztecMinimumSymbolSize;
            set
            {
                if (value < 0 || value > 36)
                    throw new BarcodeException("Minimum version should be in [0..36] range");

                _aztecMinimumSymbolSize = value;
                FireChanged(this);
            }
        }
        private int _aztecMinimumSymbolSize;

        /// <summary>
        /// Gets or sets the number of segments in line for GS1 DataBar Expanded Stacked symbology.
        /// </summary>
        /// <value>The number of segments in line.</value>
        [Description("The number of segments in line for GS1 DataBar Expanded Stacked symbology.")]
        public int GS1ExpandedStackedSegmentsNumber
        {
            get => _segmentsNumber;
            set
            {
                if (value % 2 != 0)
                    value--;
                if (value > 20)
                    value = 20;
                else if (value < 2)
                    value = 2;
                _segmentsNumber = value;
                FireChanged(this);
            }
        }
        private int _segmentsNumber = 4;

        /// <summary>
        /// Gets or sets the MaxiCode mode.
        /// </summary>
        /// <value>The MaxiCode mode.</value>
        [Description("The MaxiCode mode.")]
        public byte MaxiCodeMode 
        {
            // mode 2, 3, 4, 5 or 6
            get => _maxiCodeMode;
            set 
            {
                _maxiCodeMode = value;
                FireChanged(this);
            }
        }
        internal byte _maxiCodeMode = 4;

        /// <summary>
        /// Gets or sets a value indicating whether MaxiCode enables the use of a custom width.
        /// By default value is true and MaxiCode symbology size is determined as 30*NarrowBarWidth.
        /// Set value to false if you want to use standard MaxiCode symbology size (calculated
        /// as approximately one inch (2.54 cm) for the current resolution)
        /// </summary>
        /// <value>
        /// 	<c>true</c> if MaxiCode enable custom width; otherwise, <c>false</c>.
        /// </value>
        public bool MaxiCodeEnableCustomWidth
        {
            get => _maxiCodeUsingBarWidth;
            set 
            {
                _maxiCodeUsingBarWidth = value;
                FireChanged(this);
            }
        }
        private bool _maxiCodeUsingBarWidth = true;

        /// <summary>
        /// Gets or sets the algorithm to use for MSI symbology checksum calculation.
        /// </summary>
        /// <value>The algorithm to use for MSI symbology checksum calculation.</value>
        [Description("The algorithm to use for MSI symbology checksum calculation.")]
        public MSIChecksumAlgorithm MSIChecksumAlgorithm
        {
            get => _msiChecksumAlgorithm;
            set 
            {
                _msiChecksumAlgorithm = value;
                FireChanged(this);
            }
        }
        private MSIChecksumAlgorithm _msiChecksumAlgorithm = MSIChecksumAlgorithm.Modulo10;

        /// <summary>
        /// Gets or sets a value indicating whether ITF-14 symbology has only horizontal bearer bar or surrounding bars.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if only horizontal bearer bar; otherwise, <c>false</c>.
        /// </value>
        [Description("A value indicating whether ITF-14 symbology has only horizontal bearer bar or surrounding bars.")]
        public bool OnlyHorizontalBearerBar
        {
            get => _onlyHorizontalBearerBar;
            set 
            {
                _onlyHorizontalBearerBar = value;
                FireChanged(this);
            }
        }
        private bool _onlyHorizontalBearerBar = false;

        /// <summary>
        /// Gets or sets the type of PZN symbology.
        /// </summary>
        /// <value>The type of PZN symbology.</value>
        [Description("The type of PZN symbology.")]
        public PZNType PZNType
        {
            get => _pznType;
            set
            {
                _pznType = value;
                FireChanged(this);
            }
        }
        private PZNType _pznType = PZNType.PZN8;

        /// <summary>
        /// Two-track variant of PharmaCode
        /// </summary>
        public bool PharmaCodeTwoTrack
        {
            get => _pharmaCodeTwoTrack;
            set
            {
                _pharmaCodeTwoTrack = value;
                FireChanged(this);
            }
        }
        private bool _pharmaCodeTwoTrack;

        /// <summary>
        /// Add colored Supplementary bar in PharmaCode
        /// </summary>
        public bool PharmaCodeSupplementaryCode
        {
            get => _pharmaCodeSupplementaryCode;
            set
            {
                _pharmaCodeSupplementaryCode = value;
                FireChanged(this);
            }
        }
        private bool _pharmaCodeSupplementaryCode;

        /// <summary>
        /// Color of Supplementary bar in PharmaCode
        /// </summary>
        public SKColor PharmaCodeSupplementaryBarColor
        {
            get => _pharmaСodeSupplementaryBarColor;
            set
            {
                _pharmaСodeSupplementaryBarColor = value;
                FireChanged(this);
            }
        }
        private SKColor _pharmaСodeSupplementaryBarColor = SKColors.Magenta;

        /// <summary>
        /// Miniature variant of PharmaCode
        /// </summary>
        public bool PharmaCodeMiniature
        {
            get => _pharmaCodeMiniature;
            set
            {
                _pharmaCodeMiniature = value;
                FireChanged(this);
            }
        }
        private bool _pharmaCodeMiniature;
        
        /// <summary>
        /// Gets or sets whether to draw the quite zone indicator on EAN-2 and EAN-5 supplementary barcodes. Default is <c>false</c>. 
        /// </summary>
        public bool EANDrawQuietZoneIndicator
        {
            get => _eanDrawQuietZoneIndicator;
            set
            {
                _eanDrawQuietZoneIndicator = value;
                FireChanged(this);
            }
        }
        private bool _eanDrawQuietZoneIndicator;

#endif
    }
}
