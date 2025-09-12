
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BarcodeWriter.Core.Internal;
using Encoding = System.Text.Encoding;
using Rectangle = System.Drawing.Rectangle;
using Newtonsoft.Json;
using SkiaSharp;
using BarcodeWriter.Core;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Represents barcode generator.
    /// </summary>
#if QRCODESDK
    internal class BarcodeEncoder : IDisposable
#else
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class BarcodeEncoder : IBarcodeEncoder, IDisposable
#endif
    {
        internal static bool Forbid1D = false;
        internal static bool Forbid2D = false;

        private SymbologyDrawing _drawing = null;
        private float _zoomLevel = 1.0f;
        private Size _outputSize = Size.Empty;
        
        private MemoryStream _valueStream = new MemoryStream();

        // This graphics and bitmap are used for size measures
        private SKCanvas _graphicsForMeasures = null;
        private SKBitmap _bitmapForMeasures = null;

        private SKImage _decorationImage = null;
        private int _decorationImageScale = -1;
        
#if !BARCODESDK_EMBEDDED_SOURCES
        private ProfileManager _profileManager = null;
#endif
        private string _profiles = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="BarcodeEncoder"/> class.
        /// </summary>
        public BarcodeEncoder()
        {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
            if (SingleInstanceDetector.IsAlreadyRunning(GetType().FullName))
                throw new BarcodeException("Community Edition does not allow multiple concurrent instances.");
#endif
            
#if !BARCODESDK_EMBEDDED_SOURCES
            _profileManager = new ProfileManager(this);
#endif

#if NETCOREAPP2_1
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif

#if QRCODESDK
            _drawing = new QRSymbology();
#else
            _drawing = new Code39Symbology();
#endif
            _drawing.Options.Changed += OptionsChanged;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BarcodeEncoder"/> class.
        /// </summary>
        /// <param name="type">The barcode type (symbology).</param>
        public BarcodeEncoder(SymbologyType type)
            : this()
        {
            switch (type)
            {
                case SymbologyType.QRCode:
                    _drawing = new QRSymbology();
                    break;
                case SymbologyType.GS1_QRCode:
                    _drawing = new GS1QRSymbology();
                    break;
#if !QRCODESDK
                case SymbologyType.Code39:
                    _drawing = new Code39Symbology();
                    break;
                case SymbologyType.Code128:
                    _drawing = new Code128Symbology();
                    break;
                case SymbologyType.Postnet:
                    _drawing = new PostnetSymbology();
                    break;
                case SymbologyType.UPCA:
                    _drawing = new UPCASymbology();
                    break;
                case SymbologyType.EAN8:
                    _drawing = new EAN8Symbology();
                    break;
                case SymbologyType.ISBN:
                    _drawing = new ISBNSymbology();
                    break;
                case SymbologyType.Codabar:
                    _drawing = new CodabarSymbology();
                    break;
                case SymbologyType.I2of5:
                    _drawing = new I2of5Symbology();
                    break;
                case SymbologyType.Code93:
                    _drawing = new Code93Symbology();
                    break;
                case SymbologyType.EAN13:
                    _drawing = new EAN13Symbology();
                    break;
                case SymbologyType.JAN13:
                    _drawing = new JAN13Symbology();
                    break;
                case SymbologyType.Bookland:
                    _drawing = new BooklandSymbology();
                    break;
                case SymbologyType.UPCE:
                    _drawing = new UPCESymbology();
                    break;
                case SymbologyType.PDF417:
                case SymbologyType.MacroPDF417:
                    _drawing = new PDF417Symbology();
                    if (type == SymbologyType.MacroPDF417)
                        _drawing.Options.PDF417CreateMacro = true;
                    else
                        _drawing.Options.PDF417CreateMacro = false;
                    break;
                case SymbologyType.MicroPDF417:
                    _drawing = new PDF417MicroSymbology();
                    break;
                case SymbologyType.PDF417Truncated:
                    _drawing = new PDF417TruncatedSymbology();
                    break;
                case SymbologyType.DataMatrix:
                    _drawing = new DataMatrixSymbology();
                    break;
                case SymbologyType.Aztec:
                    _drawing = new AztecSymbology();
                    break;
                case SymbologyType.Planet:
                    _drawing = new PlanetSymbology();
                    break;
                case SymbologyType.EAN128:
                case SymbologyType.GS1_128:
                    _drawing = new EAN128Symbology();
                    break;
                case SymbologyType.USPSSackLabel:
                    _drawing = new USPSSackLabelSymbology();
                    break;
                case SymbologyType.USPSTrayLabel:
                    _drawing = new USPSTrayLabelSymbology();
                    break;
                case SymbologyType.DeutschePostIdentcode:
                    _drawing = new DeutschePostIdentcodeSymbology();
                    break;
                case SymbologyType.DeutschePostLeitcode:
                    _drawing = new DeutschePostLeitcodeSymbology();
                    break;
                case SymbologyType.Numly:
                    _drawing = new NumlySymbology();
                    break;
                case SymbologyType.PZN:
                    _drawing = new PZNSymbology();
                    break;
                case SymbologyType.OpticalProduct:
                    _drawing = new OPCSymbology();
                    break;
                case SymbologyType.SwissPostParcel:
                    _drawing = new SwissPostParcelsymbology();
                    break;
                case SymbologyType.RoyalMail:
                    _drawing = new RoyalMailSymbology();
                    break;
                case SymbologyType.DutchKix:
                    _drawing = new DutchKixSymbology();
                    break;
                case SymbologyType.SingaporePostalCode:
                    _drawing = new SingaporePostSymbology();
                    break;
                case SymbologyType.EAN2:
                    _drawing = new EAN2Symbology();
                    break;
                case SymbologyType.EAN5:
                    _drawing = new EAN5Symbology();
                    break;
                case SymbologyType.EAN14:
                    _drawing = new EAN14Symbology();
                    break;
                case SymbologyType.GS1_DataMatrix:
                    _drawing = new GS1DataMatrixSymbology();
                    break;
                case SymbologyType.Telepen:
                    _drawing = new TelepenSymbology();
                    break;
                case SymbologyType.IntelligentMail:
                    _drawing = new IntelligentMailSymbology();
                    break;
                case SymbologyType.GS1_DataBar_Omnidirectional:
                    _drawing = new GS1DataBarOmnidirectionalSymbology();
                    break;
                case SymbologyType.GS1_DataBar_Truncated:
                    _drawing = new GS1DataBarTruncatedSymbology();
                    break;
                case SymbologyType.GS1_DataBar_Stacked:
                    _drawing = new GS1DataBarStackedSymbology();
                    break;
                case SymbologyType.GS1_DataBar_Stacked_Omnidirectional:
                    _drawing = new GS1DataBarStackedOmnidirectionalSymbology();
                    break;
                case SymbologyType.GS1_DataBar_Limited:
                    _drawing = new GS1DataBarLimitedSymbology();
                    break;
                case SymbologyType.GS1_DataBar_Expanded:
                    _drawing = new GS1DataBarExpandedSymbology();
                    break;
                case SymbologyType.GS1_DataBar_Expanded_Stacked:
                    _drawing = new GS1DataBarStackedExpandedSymbology();
                    break;
                case SymbologyType.MaxiCode:
                    _drawing = new MaxiCodeSymbology();
                    break;
                case SymbologyType.Plessey:
                    _drawing = new PlesseySymbology();
                    break;
                case SymbologyType.MSI:
                    _drawing = new MSISymbology();
                    break;
                case SymbologyType.ITF14:
                    _drawing = new ITF14Symbology();
                    break;
                case SymbologyType.GTIN12:
                    _drawing = new GTIN12Symbology();
                    break;
                case SymbologyType.GTIN8:
                    _drawing = new GTIN8Symbology();
                    break;
                case SymbologyType.GTIN13:
                    _drawing = new GTIN13Symbology();
                    break;
                case SymbologyType.GTIN14:
                    _drawing = new GTIN14Symbology();
                    break;
                case SymbologyType.PharmaCode:
                    _drawing = new PharmaCodeSymbology();
                    break;
#endif
                default:
                    throw new BarcodeException("Unknown symbology type.");
            }

            setOptimizedOptions(false);

            _drawing.Options.Changed += OptionsChanged;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BarcodeEncoder"/> class.
        /// </summary>
        /// <param name="prototype">The existing barcode from which to create new barcode with different symbology type.</param>
        /// <param name="type">The type (symbology) of new barcode.</param>
        public BarcodeEncoder(BarcodeEncoder prototype, SymbologyType type)
            : this(type)
        {
            AddChecksum = prototype.AddChecksum;
            AddChecksumToCaption = prototype.AddChecksumToCaption;
            AdditionalCaption = prototype.AdditionalCaption;
            AdditionalCaptionFont = new SKFont(
                prototype.AdditionalCaptionFont.Typeface,
                prototype.AdditionalCaptionFont.Size,
                prototype.AdditionalCaptionFont.ScaleX,
                prototype.AdditionalCaptionFont.SkewX
            );
            AdditionalCaptionPosition = prototype.AdditionalCaptionPosition;
            AdditionalCaptionAlignment = prototype.AdditionalCaptionAlignment;
            BackColor = prototype.BackColor;
            BarHeight = prototype.BarHeight;
            CaptionFont = new SKFont(prototype.CaptionFont.Typeface, prototype.CaptionFont.Size);
            CaptionPosition = prototype.CaptionPosition;
            CaptionAlignment = prototype.CaptionAlignment;
            DrawCaption = prototype.DrawCaption;
            ForeColor = prototype.ForeColor;
            Margins = (Margins) prototype.Margins.Clone();
            NarrowBarWidth = prototype.NarrowBarWidth;
            Options = (SymbologyOptions) prototype.Options.Clone();
            Angle = prototype.Angle;
            Value = (string) prototype.Value.Clone();
            SupplementValue = (string) prototype.SupplementValue.Clone();
            WideToNarrowRatio = prototype.WideToNarrowRatio;

            _drawing.Options.Changed += OptionsChanged;
        }

        /// <inheritdoc />
        public void Dispose()
        {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
            SingleInstanceDetector.Close();
#endif

#if !BARCODESDK_EMBEDDED_SOURCES
            _profileManager?.Dispose();
            _profileManager = null;
#endif
            _valueStream?.Dispose();
            _valueStream = null;

            _decorationImage?.Dispose();
            _decorationImage = null;

            if (_drawing != null)
            {
                _drawing.Options.Changed -= OptionsChanged;
                _drawing.Dispose();
                _drawing = null;
            }

            _additionalCaptionFont?.Dispose();
            _additionalCaptionFont = null;
            
            disposeGraphicsForMeasures();
        }

        /// <summary>
        /// Resets barcode options and properties to non optimized defaults.
        /// </summary>
        public void ResetToNonOptimizedDefaults()
        {
            setDefaultOptions(false);
            removeFitSize();
        }

        /// <summary>
		/// Gets or sets a value indicating whether TIFF images should be saved as monochrome (1-bit, black and white) images.
		/// This property is obsolete since has been superseded with ProduceMonochromeImages property.
        /// </summary>
		/// <value><c>true</c> if Tiff images should be saved in monochrome; otherwise, <c>false</c>.</value>
        [Obsolete]
        public bool TIFFUse1BitFormat
        {
            get => ProduceMonochromeImages;
            set => ProduceMonochromeImages = value;
        }

        /// <inheritdoc />
        public bool ProduceMonochromeImages
        {
            get => _produceMonochromeImages;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _produceMonochromeImages = value;
            }
        }
        private bool _produceMonochromeImages = false;

        /// <inheritdoc />
        public string AdditionalCaption
        {
            get => _additionalCaption;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _additionalCaption = value;
                removeFitSize();
            }
        }
        private string _additionalCaption = "";

        /// <inheritdoc />
        public bool AddChecksum
        {
            get => _drawing.AddChecksum;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif                
                _drawing.AddChecksum = value;
                removeFitSize();
            }
        }

        /// <inheritdoc />
        public bool AddChecksumToCaption
        {
            get => _drawing.AddChecksumToCaption;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.AddChecksumToCaption = value;
                removeFitSize();
            }
        }

        /// <inheritdoc />
        public string Caption
        {
            get => _drawing.CustomCaption;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.CustomCaption = value;
                removeFitSize();
            }
        }

        /// <inheritdoc />
        public bool DrawCaption
        {
            get => _drawing.DrawCaption;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.DrawCaption = value;
                removeFitSize();
            }
        }

        /// <inheritdoc />
        public bool DrawCaptionFor2DBarcodes
        {
            get => _drawing.DrawCaption2D;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.DrawCaption2D = value;
                removeFitSize();
            }
        }

        /// <inheritdoc />
        public bool DrawQuietZones
        {
            get => _drawQuietZones;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawQuietZones = value;
	            _drawing.DrawQuietZones = value;
            }
        }
        private bool _drawQuietZones = true;

        /// <inheritdoc />
        public SymbologyType Symbology
        {
            get => _drawing.Symbology;
            set
            {
#if !QRCODESDK
                if (value == _drawing.Symbology)
                    return;

                SymbologyDrawing previous = _drawing;

                switch (value)
                {
                    default:
                        throw new ArgumentOutOfRangeException("value");
                    case SymbologyType.Code39:
                        _drawing = new Code39Symbology(_drawing);
                        break;
                    case SymbologyType.Code128:
                        _drawing = new Code128Symbology(_drawing);
                        break;
                    case SymbologyType.Postnet:
                        _drawing = new PostnetSymbology(_drawing);
                        break;
                    case SymbologyType.UPCA:
                        _drawing = new UPCASymbology(_drawing);
                        break;
                    case SymbologyType.EAN8:
                        _drawing = new EAN8Symbology(_drawing);
                        break;
                    case SymbologyType.ISBN:
                        _drawing = new ISBNSymbology(_drawing);
                        break;
                    case SymbologyType.Codabar:
                        _drawing = new CodabarSymbology(_drawing);
                        break;
                    case SymbologyType.I2of5:
                        _drawing = new I2of5Symbology(_drawing);
                        break;
                    case SymbologyType.Code93:
                        _drawing = new Code93Symbology(_drawing);
                        break;
                    case SymbologyType.EAN13:
                        _drawing = new EAN13Symbology(_drawing);
                        break;
                    case SymbologyType.JAN13:
                        _drawing = new JAN13Symbology(_drawing);
                        break;
                    case SymbologyType.Bookland:
                        _drawing = new BooklandSymbology(_drawing);
                        break;
                    case SymbologyType.UPCE:
                        _drawing = new UPCESymbology(_drawing);
                        break;
                    case SymbologyType.PDF417:
                    case SymbologyType.MacroPDF417:
                        _drawing = new PDF417Symbology(_drawing);
                        _drawing.Options.PDF417CreateMacro = value == SymbologyType.MacroPDF417;
                        break;
                    case SymbologyType.PDF417Truncated:
                        _drawing = new PDF417TruncatedSymbology(_drawing);
                        break;
                    case SymbologyType.MicroPDF417:
                        _drawing = new PDF417MicroSymbology(_drawing);
                        break;
                    case SymbologyType.DataMatrix:
                        _drawing = new DataMatrixSymbology(_drawing);
                        break;
                    case SymbologyType.GS1_DataMatrix:
                        _drawing = new GS1DataMatrixSymbology(_drawing);
                        break;
                    case SymbologyType.QRCode:
                        _drawing = new QRSymbology(_drawing);
                        break;
                    case SymbologyType.Aztec:
                        _drawing = new AztecSymbology(_drawing);
                        break;
                    case SymbologyType.Planet:
                        _drawing = new PlanetSymbology(_drawing);
                        break;
                    case SymbologyType.EAN128:
                    case SymbologyType.GS1_128:
                        _drawing = new EAN128Symbology(_drawing);
                        break;
                    case SymbologyType.USPSSackLabel:
                        _drawing = new USPSSackLabelSymbology(_drawing);
                        break;
                    case SymbologyType.USPSTrayLabel:
                        _drawing = new USPSTrayLabelSymbology(_drawing);
                        break;
                    case SymbologyType.DeutschePostIdentcode:
                        _drawing = new DeutschePostIdentcodeSymbology(_drawing);
                        break;
                    case SymbologyType.DeutschePostLeitcode:
                        _drawing = new DeutschePostLeitcodeSymbology(_drawing);
                        break;
                    case SymbologyType.Numly:
                        _drawing = new NumlySymbology(_drawing);
                        break;
                    case SymbologyType.PZN:
                        _drawing = new PZNSymbology(_drawing);
                        break;
                    case SymbologyType.OpticalProduct:
                        _drawing = new OPCSymbology(_drawing);
                        break;
                    case SymbologyType.SwissPostParcel:
                        _drawing = new SwissPostParcelsymbology(_drawing);
                        break;
                    case SymbologyType.RoyalMail:
                        _drawing = new RoyalMailSymbology(_drawing);
                        break;
                    case SymbologyType.DutchKix:
                        _drawing = new DutchKixSymbology(_drawing);
                        break;
                    case SymbologyType.SingaporePostalCode:
                        _drawing = new SingaporePostSymbology(_drawing);
                        break;
                    case SymbologyType.EAN2:
                        _drawing = new EAN2Symbology(_drawing);
                        break;
                    case SymbologyType.EAN5:
                        _drawing = new EAN5Symbology(_drawing);
                        break;
                    case SymbologyType.EAN14:
                        _drawing = new EAN14Symbology(_drawing);
                        break;
                    case SymbologyType.Telepen:
                        _drawing = new TelepenSymbology(_drawing);
                        break;
                    case SymbologyType.IntelligentMail:
                        _drawing = new IntelligentMailSymbology(_drawing);
                        break;
                    case SymbologyType.GS1_DataBar_Omnidirectional:
                        _drawing = new GS1DataBarOmnidirectionalSymbology(_drawing);
                        break;
                    case SymbologyType.GS1_DataBar_Truncated:
                        _drawing = new GS1DataBarTruncatedSymbology(_drawing);
                        break;
                    case SymbologyType.GS1_DataBar_Stacked:
                        _drawing = new GS1DataBarStackedSymbology(_drawing);
                        break;
                    case SymbologyType.GS1_DataBar_Stacked_Omnidirectional:
                        _drawing = new GS1DataBarStackedOmnidirectionalSymbology(_drawing);
                        break;
                    case SymbologyType.GS1_DataBar_Limited:
                        _drawing = new GS1DataBarLimitedSymbology(_drawing);
                        break;
                    case SymbologyType.GS1_DataBar_Expanded:
                        _drawing = new GS1DataBarExpandedSymbology(_drawing);
                        break;
                    case SymbologyType.GS1_DataBar_Expanded_Stacked:
                        _drawing = new GS1DataBarStackedExpandedSymbology(_drawing);
                        break;
                    case SymbologyType.MaxiCode:
                        _drawing = new MaxiCodeSymbology(_drawing);
                        break;
                    case SymbologyType.Plessey:
                        _drawing = new PlesseySymbology(_drawing);
                        break;
                    case SymbologyType.MSI:
                        _drawing = new MSISymbology(_drawing);
                        break;
                    case SymbologyType.ITF14:
                        _drawing = new ITF14Symbology(_drawing);
                        break;
                    case SymbologyType.GTIN12:
                        _drawing = new GTIN12Symbology(_drawing);
                        break;
                    case SymbologyType.GTIN8:
                        _drawing = new GTIN8Symbology(_drawing);
                        break;
                    case SymbologyType.GTIN13:
                        _drawing = new GTIN13Symbology(_drawing);
                        break;
                    case SymbologyType.GTIN14:
                        _drawing = new GTIN14Symbology(_drawing);
                        break;
                    case SymbologyType.GS1_QRCode:
                        _drawing = new GS1QRSymbology(_drawing);
                        break;
                    case SymbologyType.PharmaCode:
                        _drawing = new PharmaCodeSymbology(_drawing);
                        break;
                }

                // dispose previous drawing
                previous.Options.Changed -= OptionsChanged;
                previous.Dispose();

	            _drawing.DrawQuietZones = DrawQuietZones;
                
                setOptimizedOptions(true);
                removeFitSize();

                _drawing.Options.Changed += OptionsChanged;

#endif // !QRCODESDK
            }
        }

        /// <inheritdoc />
        public SymbologyOptions Options
        {
            get => _drawing.Options;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.Options = value;
                removeFitSize();
            }
        }

        /// <inheritdoc />
        public string Value
        {
            get => _drawing.Value;
            set
            {
                _drawing.Value = value;
                
                _valueStream.SetLength(0);
                _valueStream.Position = 0;
                byte[] bytes = Encoding.Unicode.GetBytes(_drawing.Value);
                _valueStream.Write(bytes, 0, bytes.Length);

                removeFitSize();
            }
        }

        /// <summary>
        /// Gets the copy of barcode value as stream.
        /// </summary>
        /// <value>The copy of barcode value as stream.</value>
        /// <remarks>
        /// Please use <see cref="O:BarcodeWriter.Core.BarcodeEncoder.LoadValueFromStream"/>
        /// if you want to set up value using a stream.
        /// </remarks>
        [Obsolete]
        public Stream ValueAsStream => _valueStream;

        /// <summary>
        /// Loads the value from the given stream. Read begins from current 
        /// position within a stream. 
        /// </summary>
        /// <overloads>Loads the value from the given stream.</overloads>
        /// <param name="stream">The stream to read value from.</param>
        /// <param name="length">The value's length.</param>
        [ComVisible(false)]
        public void LoadValueFromStream(Stream stream, int length)
        {
            byte[] bytes = new byte[length];
            stream.Read(bytes, 0, length);
            Value = Encoding.Default.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Loads the value from the given stream. Read begins from current
        /// position within a stream. All bytes till the end of the stream
        /// a considered to be a value's bytes.
        /// </summary>
        /// <param name="stream">The stream to read value from.</param>
        [ComVisible(false)]
        public void LoadValueFromStream(Stream stream)
        {
            int length = (int)(stream.Length - stream.Position);
            LoadValueFromStream(stream, length);
        }

        /// <inheritdoc />
        public string SupplementValue
        {
            get => _supplementValue;
            set
            {
                bool containsNonDigits = false;
                foreach (char c in value)
                {
                    if (!Char.IsDigit(c))
                    {
                        containsNonDigits = true;
                        break;
                    }
                }

                bool lengthIsOk = (value.Length == 0 || value.Length == 2 || value.Length == 5);
                if (!lengthIsOk || containsNonDigits)
                    throw new BarcodeException(GetSupplementaryValueRestrictions());

                _supplementValue = value;
                removeFitSize();
            }
        }
        private string _supplementValue = "";

        /// <inheritdoc />
        public SKFont AdditionalCaptionFont
        {
            get => _additionalCaptionFont;
            set
            {
                _additionalCaptionFont = value;
                removeFitSize();
            }
        }
        // Create font with size 12
        private SKFont _additionalCaptionFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal), 12);

        /// <inheritdoc />
        public int BarHeight
        {
            get => _drawing.BarHeight;
            set
            {
                if (value < 1)
                    throw new BarcodeException("Bar can not be less than 1 pixel in height.");

                _drawing.BarHeight = value;
                removeFitSize();
            }
        }

        /// <inheritdoc />
        public SKFont CaptionFont
        {
            get => _drawing.CaptionFont;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.CaptionFont = value;
                removeFitSize();
            }
        }

        /// <inheritdoc />
        public int NarrowBarWidth
        {
            get => _drawing.NarrowBarWidth;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.NarrowBarWidth = value;
                removeFitSize();
            }
        }

        /// <inheritdoc />
        public int WideToNarrowRatio
        {
            get => _drawing.WideToNarrowRatio;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.WideToNarrowRatio = value;
                removeFitSize();
            }
        }

        /// <inheritdoc />
        public CaptionPosition AdditionalCaptionPosition
        {
            get => _additionalCaptionPosition;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _additionalCaptionPosition = value;
                removeFitSize();
            }
        }
        private CaptionPosition _additionalCaptionPosition = CaptionPosition.Above;
        
        /// <inheritdoc />
        public CaptionAlignment AdditionalCaptionAlignment
        {
            get => _additionalCaptionAlignment;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _additionalCaptionAlignment = value;
                removeFitSize();
            }
        }
        private CaptionAlignment _additionalCaptionAlignment = CaptionAlignment.Auto;

        /// <inheritdoc />
        public RotationAngle Angle
        {
            get => _angle;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _angle = value;
                removeFitSize();
            }
        }
        private RotationAngle _angle = RotationAngle.Degrees0;

        /// <inheritdoc />
        public SKColor BackColor
        {
            get => _backColor;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _backColor = value;
            }
        }
        private SKColor _backColor = SKColors.White;

        /// <inheritdoc />
        public CaptionPosition CaptionPosition
        {
            get => _drawing.CaptionPosition;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.CaptionPosition = value;
                removeFitSize();
            }
        }
        
        /// <inheritdoc />
        public CaptionAlignment CaptionAlignment
        {
            get => _drawing.CaptionAlignment;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.CaptionAlignment = value;
                removeFitSize();
            }
        }

        /// <inheritdoc />
        public SKColor ForeColor
        {
            get => _drawing.Color;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.Color = value;
            }
        }

        /// <inheritdoc />
        public Margins Margins
        {
            get => _margins;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _margins = value;
                RevertToNormalSize();
            }
        }
        private Margins _margins = new Margins();

        /// <inheritdoc />
        public float ResolutionX
        {
            get => _resolutionX;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                if (value <= 0)
                    throw new BarcodeException("Resolution should be greater then 0.");

                _resolutionX = value;
                RevertToNormalSize();
                disposeGraphicsForMeasures();
            }
        }
        private float _resolutionX = 96;

        /// <inheritdoc />
        public float ResolutionY
        {
            get => _resolutionY;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                if (value <= 0)
                    throw new BarcodeException("Resolution should be greater then 0");

                _resolutionY = value;
                RevertToNormalSize();
                disposeGraphicsForMeasures();
            }
        }
        private float _resolutionY = 96;
        
        /// <summary>
        /// Gets or sets a value indicating whether unused space should be cut when 
        /// drawing or saving barcode images. Unused space is usually a result
        /// of calling one of FitInto methods with size greater then needed
        /// to draw barcode.
        /// </summary>
        /// <value><c>true</c> if unused space should be cut; otherwise, <c>false</c>.</value>
        public bool CutUnusedSpace
        {
            get => _cutUnusedSpace;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _cutUnusedSpace = value;
                removeFitSize();
            }
        }
        private bool _cutUnusedSpace = false;

        /// <inheritdoc />
        public bool PreserveMinReadableSize
        {
            get => _preserveMinReadableSize;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _preserveMinReadableSize = value;
            }
        }
        private bool _preserveMinReadableSize = true;
        
        /// <inheritdoc />
        public bool RoundDots
        {
            get => _drawing.RoundDots;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.RoundDots = value;
            }
        }

        /// <inheritdoc />
        public int RoundDotsScale
        {
            get => _drawing.RoundDotsScale;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _drawing.RoundDotsScale = value;
            }
        }

        /// <inheritdoc />
        public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        
        /// <inheritdoc />
        public string Profiles
        {
            get => _profiles;
            set
            {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
                RegInfo.CommunityEditionDisclaimer();
                return;
#endif
                _profiles = value;
#if !BARCODESDK_EMBEDDED_SOURCES
                _profileManager.ApplyProfiles(_profiles);
#endif
            }
        }

        /// <inheritdoc />
        public bool ValueIsValid(string value)
        {
            return _drawing.ValueIsValid(value, false);
        }

        /// <inheritdoc />
		public bool ValueIsValid(string value, bool checksumIsMandatory)
        {
			return _drawing.ValueIsValid(value, checksumIsMandatory);
        }

        /// <inheritdoc />
        public bool ValueIsValidGS1(string value)
        {
            return GS1ValueChecker.Check(value);
        }

        /// <inheritdoc />
        public Size GetMinimalSize()
        {
            if (OutputSize != Size.Empty)
                return OutputSize;

            return getDrawingSize();
        }

        /// <inheritdoc />
        public void LoadProfiles(string fileName)
        {
            if (!File.Exists(fileName))
                throw new ArgumentException("File does not exist.", "fileName");

#if !BARCODEREADERSDK_EMBEDDED_SOURCES
            _profileManager.LoadFromFile(fileName);
#endif
        }
        
        /// <inheritdoc />
        public void LoadProfilesFromString(string jsonString)
        {
#if !BARCODEREADERSDK_EMBEDDED_SOURCES
            _profileManager.LoadFromString(jsonString);
#endif
        }

        /// <inheritdoc />
        public void LoadAndApplyProfiles(string jsonString)
        {
#if !BARCODEREADERSDK_EMBEDDED_SOURCES
            _profileManager.LoadAndApplyProfiles(jsonString);

            List<string> loadedProfileNames = new List<string>();
            foreach (Profile profile in _profileManager.Profiles)
                loadedProfileNames.Add(profile.Name);
            _profiles = string.Join(",", loadedProfileNames.ToArray());
#endif
        }

        /// <inheritdoc />
        public void CreateProfile(string profileName, string outputFileName)
        {
            File.WriteAllText(outputFileName, CreateProfile(profileName));
        }
        
        /// <inheritdoc />
        public string CreateProfile(string profileName)
        {
            return $"\"profiles\": [ {{ \"{profileName}\": {JsonConvert.SerializeObject(this)} }} ]";
        }

        /// <inheritdoc />
        public void FitInto(Size size)
        {
            FitInto(new SizeF(size.Width, size.Height), UnitOfMeasure.Pixel);
        }

        /// <inheritdoc />
        public void FitInto(int width, int height)
        {
            FitInto(new SizeF(width, height), UnitOfMeasure.Pixel);
        }

        /// <inheritdoc />
        [ComVisible(false)]
        public void FitInto(SizeF size, UnitOfMeasure unit)
        {
#if COMMUNITY_EDITION && !BARCODESDK_EMBEDDED_SOURCES
            RegInfo.CommunityEditionDisclaimer();
            return;
#endif    
            Size sizePixels = Utils.GetSizeInPixels(_resolutionX, _resolutionY, size, unit);
            fitInto(sizePixels);
        }

        /// <inheritdoc />
        public void FitInto(float width, float height, UnitOfMeasure unit)
        {
            FitInto(new SizeF(width, height), unit);
        }

        /// <inheritdoc />
        public void RevertToNormalSize()
        {
            _zoomLevel = 1.0f;
            _outputSize = Size.Empty;
        }

        /// <inheritdoc />
        public void Draw(SKCanvas canvas, Point position)
        {
            Size size = getDrawingSize(canvas);
            Size barcodeSize = size;
            
            if (OutputSize != Size.Empty)
            {
                int left = 0;
                int top = 0;
                Utils.CalculateDrawPosition(size, OutputSize.Width, OutputSize.Height, out left, out top,
                    BarcodeHorizontalAlignment.Center, BarcodeVerticalAlignment.Middle);

                position.X += left;
                position.Y += top;

                size = OutputSize;
            }

            using (var paint = new SKPaint())
            {
                paint.Style = SKPaintStyle.Fill;
                paint.Color = BackColor; // assuming BackColor is System.Drawing.Color

                canvas.DrawRect(new SKRect(
                    position.X,
                    position.Y,
                    position.X + size.Width,
                    position.Y + size.Height),
                    paint);
            }

            transformGraphicsAndDraw(canvas, barcodeSize, position.X, position.Y);
        }

        /// <inheritdoc />
        public SKBitmap GetImage()
        {
            int left = 0;
            int top = 0;
            Size size = getDrawingSize();
            Size barcodeSize = size;

            if (OutputSize != Size.Empty)
            {
                Utils.CalculateDrawPosition(size, OutputSize.Width, OutputSize.Height, out left, out top,
                    BarcodeHorizontalAlignment.Center, BarcodeVerticalAlignment.Middle);

                size = OutputSize;
            }

			if (!ProduceMonochromeImages)
			{
                // Create SkiaSharp bitmap
                var image = new SKBitmap(size.Width, size.Height);

                using (var canvas = new SKCanvas(image))
                {
                    // Clear with background color
                    canvas.Clear();

                    // Call your custom drawing logic (need to adapt transformGraphicsAndDraw to SkiaSharp)
                    transformGraphicsAndDraw(canvas, barcodeSize, left, top);
                }

                return image;
            }
			else
            {
                // Load TIFF image from bytes into a SkiaSharp bitmap
                byte[] imageBytes = GetImageBytesTIFF();
                var monochromeImage = SKBitmap.Decode(imageBytes);
                // (!) Do not dispose the stream as it should be live for the image lifetime.

                return monochromeImage;
            }
        }

        /// <inheritdoc />
        public void SaveImage(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
                SaveImage(fs, Utils.FormatFromName(fileName));
        }

        /// <inheritdoc />
        [ComVisible(false)]
	    public void SaveImage(string fileName, SKEncodedImageFormat imageFormat)
	    {
			using (FileStream fs = new FileStream(fileName, FileMode.Create))
			{
				SaveImage(fs, imageFormat);
			}
	    }

        /// <inheritdoc />
        [ComVisible(false)]
        public void SaveImage(string fileName, SKEncodedImageFormat format, Size areaSize, int imageLeft, int imageTop)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
                SaveImage(fs, format, areaSize, imageLeft, imageTop);
        }

        /// <inheritdoc />
        [ComVisible(false)]
        public void SaveImage(Stream stream)
        {
            SaveImage(stream, SKEncodedImageFormat.Bmp);
        }

        /// <inheritdoc />
        [ComVisible(false)]
        public void SaveImage(Stream stream, SKEncodedImageFormat format)
        {
            int left = 0;
            int top = 0;
            if (OutputSize != Size.Empty)
            {
                Size size = getDrawingSize();

                Utils.CalculateDrawPosition(size, OutputSize.Width, OutputSize.Height, out left, out top,
                    BarcodeHorizontalAlignment.Center, BarcodeVerticalAlignment.Middle);
            }
            
            saveImage(stream, format, OutputSize, left, top);
        }

        /// <inheritdoc />
        [ComVisible(false)]
        public void SaveImage(Stream stream, SKEncodedImageFormat format, Size areaSize, int imageLeft, int imageTop)
        {
            saveImage(stream, format, areaSize, imageLeft, imageTop);
        }

        /// <inheritdoc />
        public byte[] GetImageBytes()
        {
            return getImageBytes(SKEncodedImageFormat.Bmp);
        }

        /// <inheritdoc />
        public byte[] GetImageBytesPNG()
        {
            return getImageBytes(SKEncodedImageFormat.Png);
        }

        /// <inheritdoc />
        public byte[] GetImageBytesGIF()
        {
            return getImageBytes(SKEncodedImageFormat.Gif);
        }

        /// <inheritdoc />
        public byte[] GetImageBytesTIFF()
        {
            return getImageBytes(SKEncodedImageFormat.Png);
        }

        /// <inheritdoc />
        public byte[] GetImageBytesJPG()
        {
            return getImageBytes(SKEncodedImageFormat.Jpeg);
        }

        /// <inheritdoc />
        public string GetValueRestrictions(SymbologyType symbology)
        {
            using (BarcodeEncoder temp = new BarcodeEncoder(symbology))
                return temp._drawing.getValueRestrictions();
        }

        /// <inheritdoc />
        public string GetSupplementaryValueRestrictions()
        {
            return "Supplementary barcode value should be 0, 2 or 5 digits long.\n";
        }

        /// <inheritdoc />
        public void SetCaptionFont(string familyName, int size)
        {
            var typeface = SKTypeface.FromFamilyName(familyName);
            CaptionFont = new SKFont(typeface, size);
        }

        /// <inheritdoc />
        public void SetCaptionFont(string familyName, int size, bool bold, bool italic, bool underline, bool strikeout, byte gdiCharSet)
        {
            // Map bold/italic to SKFontStyle
            SKFontStyleWeight weight = bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
            SKFontStyleSlant slant = italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
            SKFontStyleWidth width = SKFontStyleWidth.Normal;

            var fontStyle = new SKFontStyle(weight, width, slant);
            SKTypeface typeface = SKTypeface.FromFamilyName(familyName, fontStyle);

            CaptionFont = new SKFont(typeface, size);

            // Note: underline and strikeout are applied when drawing text with SKPaint
            // e.g., paint.IsUnderline = underline; paint.IsStrikeThru = strikeout;
        }

        /// <inheritdoc />
        public void SetAdditionalCaptionFont(string familyName, int size)
        {
            // Create the typeface
            SKTypeface typeface = SKTypeface.FromFamilyName(familyName);

            // Create the SKFont with the desired size
            AdditionalCaptionFont = new SKFont(typeface, size);
        }

        /// <inheritdoc />
        public void SetAdditionalCaptionFont(string familyName, int size, bool bold, bool italic, bool underline, bool strikeout, byte gdiCharSet)
        {
            // Map bold/italic to SKFontStyle
            SKFontStyleWeight weight = bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
            SKFontStyleSlant slant = italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
            SKFontStyleWidth width = SKFontStyleWidth.Normal;

            // Create the typeface
            SKTypeface typeface = SKTypeface.FromFamilyName(familyName, new SKFontStyle(weight, width, slant));

            // Create the SKFont
            AdditionalCaptionFont = new SKFont(typeface, size);

            // Underline and strikeout are applied when drawing text via SKPaint
            // e.g.:
            // paint.IsUnderline = underline;
            // paint.IsStrikeThru = strikeout;
        }

        /// <inheritdoc />
        public void SetMargins(float left, float top, float right, float bottom, UnitOfMeasure unit)
        {
            Size sz1 = Utils.GetSizeInPixels(_resolutionX, _resolutionY, new SizeF(left, top), unit);
            Size sz2 = Utils.GetSizeInPixels(_resolutionX, _resolutionY, new SizeF(right, bottom), unit);
            Margins = new Margins(sz1.Width, sz1.Height, sz2.Width, sz2.Height);
        }

        /// <inheritdoc />
        public void GetMargins(out float left, out float top, out float right, out float bottom, UnitOfMeasure unit)
        {
            SizeF sz1 = Utils.GetSizeInUnits(_resolutionX, _resolutionY, new Size(Margins.Left, Margins.Top), unit);
            SizeF sz2 = Utils.GetSizeInUnits(_resolutionX, _resolutionY, new Size(Margins.Left, Margins.Top), unit);

            left = sz1.Width;
            top = sz1.Height;
            right = sz2.Width;
            bottom = sz2.Height;
        }

        /// <inheritdoc />
        public float GetMarginLeft(UnitOfMeasure unit)
        {
            float left;
            float top;
            float right;
            float bottom;
            GetMargins(out left, out top, out right, out bottom, unit);
            return left;
        }

        /// <inheritdoc />
        public float GetMarginTop(UnitOfMeasure unit)
        {
            float left;
            float top;
            float right;
            float bottom;
            GetMargins(out left, out top, out right, out bottom, unit);
            return top;
        }

        /// <inheritdoc />
        public float GetMarginRight(UnitOfMeasure unit)
        {
            float left;
            float top;
            float right;
            float bottom;
            GetMargins(out left, out top, out right, out bottom, unit);
            return right;
        }

        /// <inheritdoc />
        public float GetMarginBottom(UnitOfMeasure unit)
        {
            float left;
            float top;
            float right;
            float bottom;
            GetMargins(out left, out top, out right, out bottom, unit);
            return bottom;
        }

        /// <inheritdoc />
        public void SetBarHeight(float height, UnitOfMeasure unit)
        {
            Size size = Utils.GetSizeInPixels(_resolutionX, _resolutionY, new SizeF(0, height), unit);
            BarHeight = Math.Max(size.Height, 1);
        }

        /// <inheritdoc />
        public float GetBarHeight(UnitOfMeasure unit)
        {
            SizeF size = Utils.GetSizeInUnits(_resolutionX, _resolutionY, new Size(0, BarHeight), unit);
            return size.Height;
        }

        /// <inheritdoc />
        public void SetNarrowBarWidth(float width, UnitOfMeasure unit)
        {
            Size size = Utils.GetSizeInPixels(_resolutionX, _resolutionY, new SizeF(width, 0), unit);
            NarrowBarWidth = Math.Max(size.Width, 1);
        }

        /// <inheritdoc />
        public float GetNarrowBarWidth(UnitOfMeasure unit)
        {
            SizeF size = Utils.GetSizeInUnits(_resolutionX, _resolutionY, new Size(NarrowBarWidth, 0), unit);
            return size.Width;
        }

        /// <inheritdoc />
        public SizeF GetMinimalSize(UnitOfMeasure unit)
        {
            Size sz = GetMinimalSize();
            SizeF res = Utils.GetSizeInUnits(_resolutionX, _resolutionY, sz, unit);
            return res;
        }

        /// <inheritdoc />
        public float GetMinimalWidth(UnitOfMeasure unit)
        {
            SizeF size = GetMinimalSize(unit);
            return size.Width;
        }

        /// <inheritdoc />
        public float GetMinimalHeight(UnitOfMeasure unit)
        {
            SizeF size = GetMinimalSize(unit);
            return size.Height;
        }

        /// <inheritdoc />
        public void Draw(SKCanvas canvas, float left, float top, UnitOfMeasure unit)
        {
            Size size = Utils.GetSizeInPixels(canvas, new SizeF(left, top), unit);
            Draw(canvas, new Point(size.Width, size.Height));
        }

        // Handles changes in barcode options.
        protected void OptionsChanged(Object sender, EventArgs e)
        {
            if (!_drawing.ValueIsValid(_drawing.Value, false))
                _drawing.Value = "";
        }

        private void disposeGraphicsForMeasures()
        {
            _graphicsForMeasures?.Dispose();
            _graphicsForMeasures = null;

            _bitmapForMeasures?.Dispose();
            _bitmapForMeasures = null;
        }

        private void createGraphicsForMeasures()
        {
            disposeGraphicsForMeasures();
            _bitmapForMeasures = new SKBitmap(10, 10);
            _graphicsForMeasures = new SKCanvas(_bitmapForMeasures);
        }

        // Sets the default (the same for all symbologies) options and
        private void setDefaultOptions(bool copyMode)
        {
            if (!copyMode)
            {
                _additionalCaption = "";
                _additionalCaptionPosition = CaptionPosition.Above;
                _additionalCaptionAlignment = CaptionAlignment.Auto;
                _margins = new Margins();

                // Create a typeface for Arial, regular
                SKTypeface typeface = SKTypeface.FromFamilyName("Arial");

                // Create the SKFont with size 12 pixels
                AdditionalCaptionFont = new SKFont(typeface, 12);
            }

            _zoomLevel = 1.0f;
            _outputSize = Size.Empty;
            _cutUnusedSpace = false;

            if (!copyMode)
            {
                _drawing.Options = new SymbologyOptions();

                _drawing.AddChecksum = true;
                _drawing.AddChecksumToCaption = true;

                _drawing.CustomCaption = "";
                _drawing.DrawCaption = true;
                SKTypeface typeface = SKTypeface.FromFamilyName("Arial");
                _drawing.CaptionFont = new SKFont(typeface, 12);
                _drawing.CaptionPosition = CaptionPosition.Below;
                _drawing.CaptionAlignment = CaptionAlignment.Auto;
            }

            _drawing.BarHeight = 50;
            _drawing.NarrowBarWidth = 3;
            _drawing.WideToNarrowRatio = 3;

            if (!copyMode)
                _drawing.Color = SKColors.Black;
        }

        // Sets the symbology-specific optimized options and properties.
        private void setOptimizedOptions(bool copyMode)
        {
            setDefaultOptions(copyMode);

#if !QRCODESDK
            
            if (Symbology == SymbologyType.PDF417 || Symbology == SymbologyType.PDF417Truncated ||
                Symbology == SymbologyType.MacroPDF417 || Symbology == SymbologyType.DataMatrix ||
                Symbology == SymbologyType.GS1_DataMatrix)
            {
                _drawing.BarHeight = 6;
            }

            if (Symbology == SymbologyType.MicroPDF417)
                _drawing.BarHeight = 3;

            if (Symbology == SymbologyType.UPCA || Symbology == SymbologyType.UPCE ||
                Symbology == SymbologyType.EAN13 || Symbology == SymbologyType.EAN8 ||
                Symbology == SymbologyType.Plessey || Symbology == SymbologyType.MSI ||
                Symbology == SymbologyType.GTIN12 || Symbology == SymbologyType.GTIN8 ||
                Symbology == SymbologyType.GTIN13)
            {
                SetBarHeight(1.02f, UnitOfMeasure.Inch);
				SetNarrowBarWidth(13f * 0.001f, UnitOfMeasure.Inch); // 1 mils == 0.001 inch
            }

            if (Symbology == SymbologyType.GS1_DataBar_Omnidirectional)
                _drawing.BarHeight = _drawing.NarrowBarWidth * 33; // Min height of symbols according to ISO/IEC 24724-2011
                                                                     // Max height is not limited

            if (Symbology == SymbologyType.PharmaCode)
            {
                SetNarrowBarWidth(0.6f, UnitOfMeasure.Millimeter);
                _drawing.WideToNarrowRatio = 3;
            }

#endif
        }

        private void removeFitSize()
        {
            RevertToNormalSize();
        }

        // IMPORTANT: Call this method AFTER setting the barcode value.
        // Fits the barcode into specified size.
        // Calling this method will change the barcode properties.
        // Properties will be recalculated so that resulting barcode
        // fit into specified size.
        private void fitInto(Size size)
        {
            RevertToNormalSize();

            Size drawingSize = getDrawingSize();
            float widthRatio = (float)drawingSize.Width / (float)size.Width;
            float heightRatio = (float)drawingSize.Height / (float)size.Height;

            float ratio = 1.0f;
            if (PreserveMinReadableSize)
            {
                ratio = 1.0f / widthRatio;
                if (ratio < 1.0f)
                {
                    size.Width = drawingSize.Width;
                    ratio = 1.0f;
                }

                ratio = 1.0f / heightRatio;
                if (ratio < 1.0f)
                {
                    size.Height = drawingSize.Height;
                    ratio = 1.0f;
                }
            }

            widthRatio = (float)drawingSize.Width / (float)size.Width;
            heightRatio = (float)drawingSize.Height / (float)size.Height;

            _zoomLevel = 1.0f / System.Math.Max(widthRatio, heightRatio);

            _outputSize = size;
        }

        private void transformGraphicsAndDraw(SKCanvas canvas, Size occupiedByBarcodeOnly, int imageLeft, int imageTop)
        {
            // Move origin
            canvas.Translate(imageLeft, imageTop);

            // Rotate (same logic you had for GDI+)
            Utils.RotateGraphics(canvas, _angle, (int)occupiedByBarcodeOnly.Width, (int)occupiedByBarcodeOnly.Height);

            // Scale (zoom)
            canvas.Scale(_zoomLevel, _zoomLevel);

            // --- Rendering quality settings ---
            // In SkiaSharp, antialiasing is per SKPaint, not per canvas.
            // So you’ll need to set IsAntialias = true on the SKPaints you use in drawOnCanvas.
            // There is no direct TextRenderingHint equivalent, but you can tune
            // SubpixelText and LcdRenderText on SKPaint for text quality.

            // Draw content
            drawOnGraphics(canvas, false);

            // Reset transform
            canvas.ResetMatrix();
        }

        private Size getDrawingSize()
        {
            if (_graphicsForMeasures == null)
                createGraphicsForMeasures();

            return getDrawingSize(_graphicsForMeasures);
        }

        private Size getDrawingSize(SKCanvas canvas)
        {
            return drawOnGraphics(canvas, true);
        }

        [Obsolete]
        private Size drawOnGraphics(SKCanvas canvas, bool onlyCalculate)
        {
	        // Final barcode image is as follows:
	        // 
	        // -------------------------------------------------------------------------------------
	        // |                        margin top                                                 |
	        // |                   additional caption                                              |
	        // |                            QZ                                                     |
	        // |                 --------------------------                                        |
	        // |                 |                        |             QZ                         |
	        // |                 |                        |       ----------------                 |
	        // | margin left  QZ |        barcode         | QZ SS | supplement   | QZ margin right |
	        // |                 |                        |       ----------------                 |
	        // |                 |                        |             QZ                         |
	        // |                 --------------------------                                        |
	        // |                            QZ                                                     |
	        // |              demo warning                                                         |
	        // |                     margin bottom                                                 |
	        // -------------------------------------------------------------------------------------
	        //
	        // (QZ == Quiet Zone, SS == Supplement Space)
	        //
	        // Note:
	        //  1. Additional caption maybe drawn below barcode (just above demo warning)
	        //  2. There is always a blank space above additional caption.
	        //  3. Additional caption is centered between left and right margins and
	        //     NOT between start and end of barcode.
	        //  4. Warning string starts right after margin (not where barcode starts)

            _drawing.BarcodeResolution = new SizeF(_resolutionX, _resolutionY);

	        Size barcodeSize = _drawing.Draw(canvas, new SKPoint(0, 0), true);

	        Size supplementBarcodeSize = new Size();
	        BarcodeEncoder supplement = getSupplementaryBarcode();
	        if (supplement != null)
	        {
		        supplementBarcodeSize = supplement._drawing.Draw(canvas, new SKPoint(0, 0), true);
		        supplementBarcodeSize.Width += Options.SupplementSpace + QuietZoneWidth;
		        supplementBarcodeSize.Height += QuietZoneHeight*2;
	        }

	        string savedAdditionalCaption = setISBNCaption();

	        Size additionalCaptionSize = new Size();
	        int additionalCaptionGap = 0;
	        if (AdditionalCaption.Length != 0)
	        {
                SKRect sKRect = new SKRect();
                float width = AdditionalCaptionFont.MeasureText(AdditionalCaption, out sKRect);
                SizeF sizeF = new SizeF(sKRect.Width, sKRect.Height);
		        additionalCaptionSize = new Size((int) (sizeF.Width + 1), (int) (sizeF.Height + 1));
		        additionalCaptionGap = Utils.CalculateCaptionGap(AdditionalCaptionFont);
	        }

	        // Main caption for left and right postions (SymbologyDrawing draws only standard top and bottom captions)
	        Size sideCaptionSize = new Size();
	        int sideCaptionGap = 0;
	        if (DrawCaption && (CaptionPosition == CaptionPosition.Before || CaptionPosition == CaptionPosition.After))
	        {
                SKRect sKRect = new SKRect();
                float width = CaptionFont.MeasureText(_drawing.Caption, out sKRect);
                SizeF sizeF = new SizeF(sKRect.Width, sKRect.Height);
                sideCaptionSize = new Size((int) (sizeF.Width + 1), (int) (sizeF.Height + 1));
		        sideCaptionGap = Utils.CalculateCaptionGap(CaptionFont);
	        }

	        int widthOccupiedByBarcodes = QuietZoneWidth*2 + barcodeSize.Width + supplementBarcodeSize.Width;

	        int additionalCaptionHeightAbove = 0;
	        int additionalCaptionWidthAbove = 0;
	        int additionalCaptionHeightBelow = 0;
	        int additionalCaptionWidthBelow = 0;
	        int additionalCaptionWidthBefore = 0;
	        int additionalCaptionHeightBefore = 0;
	        int additionalCaptionWidthAfter = 0;
	        int additionalCaptionHeightAfter = 0;

	        if (!additionalCaptionSize.IsEmpty)
	        {
		        if (AdditionalCaptionPosition == CaptionPosition.Above)
		        {
			        additionalCaptionWidthAbove = additionalCaptionSize.Width;
			        additionalCaptionHeightAbove = additionalCaptionSize.Height + additionalCaptionGap;
		        }
		        else if (AdditionalCaptionPosition == CaptionPosition.Below)
		        {
			        additionalCaptionWidthBelow = additionalCaptionSize.Width;
			        additionalCaptionHeightBelow = additionalCaptionGap + additionalCaptionSize.Height;
		        }
		        else if (AdditionalCaptionPosition == CaptionPosition.Before)
		        {
			        additionalCaptionWidthBefore = additionalCaptionSize.Width + additionalCaptionGap;
			        additionalCaptionHeightBefore = additionalCaptionSize.Height;
		        }
		        else if (AdditionalCaptionPosition == CaptionPosition.After)
		        {
			        additionalCaptionWidthAfter = additionalCaptionGap + additionalCaptionSize.Width;
			        additionalCaptionHeightAfter = additionalCaptionSize.Height;
		        }
	        }

	        int sideCaptionWidthBefore = 0;
	        int sideCaptionHeightBefore = 0;
	        int sideCaptionWidthAfter = 0;
	        int sideCaptionHeightAfter = 0;

	        if (!sideCaptionSize.IsEmpty)
	        {
		        if (CaptionPosition == CaptionPosition.Before)
		        {
			        sideCaptionWidthBefore = sideCaptionSize.Width + sideCaptionGap;
			        sideCaptionHeightBefore = sideCaptionSize.Height;
		        }
		        else if (CaptionPosition == CaptionPosition.After)
		        {
			        sideCaptionWidthAfter = sideCaptionGap + sideCaptionSize.Width;
			        sideCaptionHeightAfter = sideCaptionSize.Height;
		        }
	        }

	        if (!onlyCalculate)
	        {
                // Determine horizontal alignment for SkiaSharp
                SKTextAlign hAlign;
                switch (AdditionalCaptionAlignment)
                {
                    case CaptionAlignment.Auto:
                        hAlign = additionalCaptionWidthAbove > widthOccupiedByBarcodes
                            ? SKTextAlign.Left
                            : SKTextAlign.Center;
                        break;
                    case CaptionAlignment.Left:
                        hAlign = SKTextAlign.Left;
                        break;
                    case CaptionAlignment.Center:
                        hAlign = SKTextAlign.Center;
                        break;
                    case CaptionAlignment.Right:
                        hAlign = SKTextAlign.Right;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Only draw if there’s text
                if (!additionalCaptionSize.IsEmpty)
                {
                    // Compute the rectangle where the caption will go
                    SKRect captionRect;
                    float verticalCenter;

                    if (AdditionalCaptionPosition == CaptionPosition.Above)
                    {
                        captionRect = new SKRect(
                            _margins.Left + sideCaptionWidthBefore + QuietZoneWidth,
                            _margins.Top,
                            _margins.Left + sideCaptionWidthBefore + QuietZoneWidth + Math.Max(additionalCaptionWidthAbove, barcodeSize.Width),
                            _margins.Top + additionalCaptionHeightAbove);

                        verticalCenter = captionRect.Top + additionalCaptionHeightAbove / 2f;
                    }
                    else if (AdditionalCaptionPosition == CaptionPosition.Below)
                    {
                        captionRect = new SKRect(
                            _margins.Left + sideCaptionWidthBefore + QuietZoneWidth,
                            _margins.Top + barcodeSize.Height + additionalCaptionGap,
                            _margins.Left + sideCaptionWidthBefore + QuietZoneWidth + Math.Max(additionalCaptionWidthBelow, barcodeSize.Width),
                            _margins.Top + barcodeSize.Height + additionalCaptionGap + additionalCaptionHeightBelow);

                        verticalCenter = captionRect.Top + additionalCaptionHeightBelow / 2f;
                    }
                    else if (AdditionalCaptionPosition == CaptionPosition.Before)
                    {
                        captionRect = new SKRect(
                            _margins.Left,
                            _margins.Top,
                            _margins.Left + additionalCaptionWidthBefore,
                            _margins.Top + Math.Max(additionalCaptionHeightBefore, barcodeSize.Height));

                        verticalCenter = captionRect.Top + (Math.Max(additionalCaptionHeightBefore, barcodeSize.Height) / 2f);
                        if (AdditionalCaptionAlignment == CaptionAlignment.Auto)
                            hAlign = SKTextAlign.Right;
                    }
                    else // After
                    {
                        captionRect = new SKRect(
                            _margins.Left + sideCaptionWidthBefore + widthOccupiedByBarcodes + sideCaptionWidthAfter + additionalCaptionGap,
                            _margins.Top,
                            _margins.Left + sideCaptionWidthBefore + widthOccupiedByBarcodes + sideCaptionWidthAfter + additionalCaptionGap + additionalCaptionWidthAfter,
                            _margins.Top + Math.Max(additionalCaptionHeightAfter, barcodeSize.Height));

                        verticalCenter = captionRect.Top + (Math.Max(additionalCaptionHeightAfter, barcodeSize.Height) / 2f);
                        if (AdditionalCaptionAlignment == CaptionAlignment.Auto)
                            hAlign = SKTextAlign.Left;
                    }

                    // Setup paint
                    using (var paint = new SKPaint
                    {
                        IsAntialias = true,
                        Color = _drawing.Color,
                        Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal),
                        TextSize = 12,
                        TextAlign = hAlign
                    })
                    {
                        // Get font metrics
                        SKFontMetrics metrics;
                        paint.GetFontMetrics(out metrics);

                        // Compute y to center text vertically
                        float y = verticalCenter - (metrics.Ascent + metrics.Descent) / 2;

                        // Compute x based on SKTextAlign
                        float x;
                        switch (hAlign)
                        {
                            case SKTextAlign.Left:
                                x = captionRect.Left;
                                break;
                            case SKTextAlign.Center:
                                x = (captionRect.Left + captionRect.Right) / 2f; // MidX
                                break;
                            case SKTextAlign.Right:
                                x = captionRect.Right;
                                break;
                            default:
                                x = captionRect.Left;
                                break;
                        }

                        // Draw the text
                        canvas.DrawText(AdditionalCaption, x, y, paint);
                    }
                }

                if (!sideCaptionSize.IsEmpty)
		        {
                    SKRect sideCaptionRect;

                    if (CaptionPosition == CaptionPosition.Before)
                    {
                        sideCaptionRect = new SKRect(
                            _margins.Left + additionalCaptionWidthBefore,
                            _margins.Top + additionalCaptionHeightAbove,
                            _margins.Left + additionalCaptionWidthBefore + sideCaptionWidthBefore,
                            _margins.Top + additionalCaptionHeightAbove + Math.Max(sideCaptionHeightBefore, barcodeSize.Height));
                    }
                    else // CaptionPosition.After
                    {
                        sideCaptionRect = new SKRect(
                            _margins.Left + additionalCaptionWidthBefore + sideCaptionGap + widthOccupiedByBarcodes + sideCaptionGap,
                            _margins.Top + additionalCaptionHeightAbove,
                            _margins.Left + additionalCaptionWidthBefore + sideCaptionGap + widthOccupiedByBarcodes + sideCaptionGap + sideCaptionWidthAfter,
                            _margins.Top + additionalCaptionHeightAbove + Math.Max(sideCaptionHeightAfter, barcodeSize.Height));
                    }

                    // Create SKPaint with font and color
                    using (var paint = new SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.Color = _drawing.Color; // convert System.Drawing.Color to SKColor
                        paint.Typeface = CaptionFont.Typeface;   // expose SKTypeface from your font wrapper
                        paint.TextSize = CaptionFont.Size;

                        // Map horizontal alignment
                        switch (CaptionAlignment)
                        {
                            case CaptionAlignment.Left:
                                paint.TextAlign = SKTextAlign.Left;
                                break;
                            case CaptionAlignment.Center:
                                paint.TextAlign = SKTextAlign.Center;
                                break;
                            case CaptionAlignment.Right:
                                paint.TextAlign = SKTextAlign.Right;
                                break;
                            case CaptionAlignment.Auto:
                                paint.TextAlign = SKTextAlign.Center; // or Left/Right logic if needed
                                break;
                        }

                        // Get font metrics for vertical alignment
                        SKFontMetrics metrics;
                        paint.GetFontMetrics(out metrics);

                        // Compute X/Y
                        float x;
                        if (paint.TextAlign == SKTextAlign.Left)
                            x = sideCaptionRect.Left;
                        else if (paint.TextAlign == SKTextAlign.Center)
                            x = sideCaptionRect.MidX;
                        else // Right
                            x = sideCaptionRect.Right;

                        float y = sideCaptionRect.Top + (sideCaptionRect.Height / 2) - (metrics.Ascent + metrics.Descent) / 2;

                        // Draw the text
                        canvas.DrawText(_drawing.Caption, x, y, paint);
                    }
                }

		        // draw main barcode 
                SKPoint barcodePosition = new SKPoint(_margins.Left + additionalCaptionWidthBefore + sideCaptionWidthBefore + QuietZoneWidth,
                    _margins.Top + additionalCaptionHeightAbove);
                _drawing.Draw(canvas, barcodePosition, false);
                
                if (Symbology == SymbologyType.QRCode && _decorationImage != null)
                {
                    if (_decorationImage.Width > barcodeSize.Width && _decorationImageScale == -1)
                        throw new BarcodeException("The decoration image is large than the generated barcode.");

                    // HighQualityBicubic equivalent
                    var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);

                    // Compute scaled decoration image size
                    int decorationImageWidth = _decorationImageScale != -1
                        ? (int)Math.Round(Math.Sqrt(barcodeSize.Width * barcodeSize.Width * _decorationImageScale / 100d))
                        : _decorationImage.Width;

                    int decorationImageHeight = _decorationImageScale != -1
                        ? (int)Math.Round(Math.Sqrt(barcodeSize.Height * barcodeSize.Height * _decorationImageScale / 100d))
                        : _decorationImage.Height;

                    if (_decorationImageScale != -1 && (barcodeSize.Width - decorationImageWidth) % 2 > 0)
                        decorationImageWidth++;

                    if (_decorationImageScale != -1 && (barcodeSize.Height - decorationImageHeight) % 2 > 0)
                        decorationImageHeight++;

                    // Center position
                    int x = (int)barcodePosition.X + barcodeSize.Width / 2 - decorationImageWidth / 2;
                    int y = (int)barcodePosition.Y + barcodeSize.Height / 2 - decorationImageHeight / 2;

                    // Destination rectangle
                    var destRect = new SKRect(x, y, x + decorationImageWidth, y + decorationImageHeight);

                    // Source rectangle
                    var srcRect = new SKRect(0, 0, _decorationImage.Width, _decorationImage.Height);

                    // Draw image with cubic interpolation
                    canvas.DrawImage(_decorationImage, srcRect, destRect, sampling);
                }

		        // draw supplement barcode, if needed
		        if (supplement != null)
		        {
			        supplement._drawing.Draw(canvas,
				        new SKPoint(_margins.Left + additionalCaptionWidthBefore + sideCaptionWidthBefore + QuietZoneWidth + barcodeSize.Width +
					        QuietZoneWidth + Options.SupplementSpace,
					        _margins.Top + additionalCaptionHeightAbove + QuietZoneHeight * 2), false);
		        }
	        }

	        if (savedAdditionalCaption != null)
		        AdditionalCaption = savedAdditionalCaption;

	        if (supplement != null)
		        supplement.Dispose();

	        float imageWidth = Margins.Left + additionalCaptionWidthBefore + sideCaptionWidthBefore +
	                           Math.Max(widthOccupiedByBarcodes, Math.Max(additionalCaptionWidthAbove, additionalCaptionWidthBelow)) +
	                           sideCaptionWidthAfter + additionalCaptionWidthAfter + Margins.Right;
	        float imageHeight = Margins.Top + additionalCaptionHeightAbove +
	                            Math.Max(barcodeSize.Height, Math.Max(additionalCaptionHeightBefore, additionalCaptionHeightAfter)) +
	                            additionalCaptionHeightBelow + /*warningSize.Height +*/ Margins.Bottom;

	        imageWidth *= _zoomLevel;
	        imageHeight *= _zoomLevel;

	        Size imageSize = rotateSize(new Size((int) imageWidth, (int) imageHeight));
	        imageSize.Width = Math.Max(imageSize.Width, 1);
	        imageSize.Height = Math.Max(imageSize.Height, 1);

	        // draw license warning, if needed
			if (true)
	        {
		        bool is2DSymbology = Utils.Is2DSymbology(_drawing.Symbology);

		        if (is2DSymbology && Forbid2D || !is2DSymbology && Forbid1D)
		        {
                    /*using (SolidBrush brush = new SolidBrush(Color.Red))
			        {
				        string message = RegInfo.GetLicenseWarningString();
				        Font font = new Font(CaptionFont.Name, CaptionFont.Size, FontStyle.Bold);
						Size size = g.MeasureString(message, font).ToSize();
				        size.Width += 4;
				        size.Height += 4;
						imageSize.Width = Math.Max(size.Width, imageSize.Width);
						imageSize.Height = Math.Max(size.Height, imageSize.Height);

				        if (!onlyCalculate)
				        {
					        using (Pen pen = new Pen(Color.White, 7))
					        {
								g.DrawLine(pen, 0, imageSize.Height / 2, imageSize.Width, imageSize.Height / 2);
								g.DrawLine(pen, imageSize.Width / 2, 0, imageSize.Width / 2, imageSize.Height);
					        }

							StringFormat format = new StringFormat(StringFormatFlags.NoClip | StringFormatFlags.NoWrap);
							format.Alignment = StringAlignment.Center;
							format.LineAlignment = StringAlignment.Center;
							g.DrawString(message, font, brush, new Rectangle(0, 0, imageSize.Width, imageSize.Height), format);
				        }
			        }*/

                    throw new BarcodeException("The barcode cannot be generated because the current license forbids it.\n");

                }
	        }
	        return imageSize;
        }

	    private int QuietZoneWidth
        {
            get
            {
                if (!DrawQuietZones)
                    return 0;

                if (_drawing is SymbologyDrawing2D)
                    return (2 * NarrowBarWidth);

                return (10 * NarrowBarWidth);
            }
        }

        private int QuietZoneHeight
        {
            get
            {
                if (!DrawQuietZones)
                    return 0;

                if (_drawing is SymbologyDrawing2D)
                    return (2 * NarrowBarWidth);

                return 0;
            }
        }

        private Size OutputSize
        {
            get
            {
                if (_cutUnusedSpace)
                    return Size.Empty;

                return _outputSize;
            }
        }

        private byte[] getImageBytes(SKEncodedImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                SaveImage(ms, format);
                return ms.ToArray();
            }
        }

        private BarcodeEncoder getSupplementaryBarcode()
        {
            BarcodeEncoder supplement = null;

            if (Value.Length != 0 && SupplementValue.Length != 0)
            {
#if !QRCODESDK
                if (Symbology == SymbologyType.EAN13 || Symbology == SymbologyType.ISBN || Symbology == SymbologyType.UPCA)
                {
                    supplement = new BarcodeEncoder(this, this.Symbology);
                    supplement.SupplementValue = "";

                    if (SupplementValue.Length == 2)
                        supplement.Symbology = SymbologyType.EAN2;
                    else
                        supplement.Symbology = SymbologyType.EAN5;

                    supplement.Value = SupplementValue;
                    supplement.BarHeight = BarHeight * 3 / 4;
                    supplement.NarrowBarWidth = NarrowBarWidth;

                    supplement.CaptionFont = new SKFont(CaptionFont.Typeface, CaptionFont.Size);
                    supplement.Angle = Angle;
                    supplement.ResolutionX = ResolutionX;
                    supplement.ResolutionY = ResolutionY;

                    supplement.WideToNarrowRatio = WideToNarrowRatio;                    
                }
#endif
            }

            return supplement;
        }

        private string setISBNCaption()
        {
            string savedAdditionalCaption = null;
#if !QRCODESDK
            if (_drawing.Symbology == SymbologyType.ISBN && Options.ISBNAutoCaption)
            {
                ISBNSymbology isbn = _drawing as ISBNSymbology;
                savedAdditionalCaption = AdditionalCaption;
                AdditionalCaption = isbn.GetAutoCaption();
            }
#endif
            return savedAdditionalCaption;
        }

        // Rotates the given size object if needed.
        private Size rotateSize(Size size)
        {
            if (_angle == RotationAngle.Degrees270 || _angle == RotationAngle.Degrees90)
            {
                int temp = size.Width;
                size.Width = size.Height;
                size.Height = temp;
            }

            return size;
        }

        // Saves the image with the barcode.
        private void saveImage(Stream stream, SKEncodedImageFormat format, Size areaSize, int imageLeft, int imageTop)
        {
            if (stream == null)
                throw new BarcodeException("Stream should not be null.");

            Stream metafileStream = null;

            Size size = getDrawingSize();
            Size barcodeSize = size;

            if (areaSize != Size.Empty)
            {
                if (PreserveMinReadableSize)
                {
                    if (size.Width > areaSize.Width || size.Height > areaSize.Height)
                    {
                        string ex = string.Format(
                            "The barcode size you have specified is too small and this may cause the unreadable barcode.\n" +
                            "The minimal size of the barcode for the specified barcode value and symbology is\n" +
                            " {0}x{1} (in pixels) with DPI(X)={2} and DPI(Y)={3}.\n\n" +
                            " TIP: You can turn off such exceptions by setting PreserveMinReadableSize to false (NOT recommended)",
                            size.Width, size.Height, _resolutionX, _resolutionY);

                        throw new BarcodeException(ex);
                    }
                }
            
                size = areaSize;
            }

            SKImage image = null;

            if (metafileStream != null)
            {
                // SkiaSharp cannot create a metafile. 
                // Instead, use SKPictureRecorder if you want vector output.
                var recorder = new SKPictureRecorder();
                var rect = new SKRect(0, 0, size.Width, size.Height);
                var canvas = recorder.BeginRecording(rect);

                // Fill background
                canvas.Clear(BackColor);

                // Do your drawing
                transformGraphicsAndDraw(canvas, barcodeSize, imageLeft, imageTop);

                // End recording → produces SKPicture
                var picture = recorder.EndRecording();
                picture.Serialize(metafileStream); // vector serialization (Skia format, not EMF/WMF)
                picture.Dispose();

                return; // no further bitmap steps
            }
            else
            {
                // Create a raster image
                var info = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using (var surface = SKSurface.Create(info))
                {
                    var canvas = surface.Canvas;

                    // Fill background
                    canvas.Clear(BackColor);

                    // Do your drawing
                    transformGraphicsAndDraw(canvas, barcodeSize, imageLeft, imageTop);

                    // Snapshot to SKImage
                    image = surface.Snapshot();

                    // If you need raw bitmap resolution handling (DPI), Skia doesn't have SetResolution.
                    // You embed DPI when encoding (e.g., PNG with pHYs chunk).
                }
            }

            // Create a new SKBitmap with the same size
            SKBitmap copy = new SKBitmap(image.Width, image.Height);
            // Copy pixels from SKImage into SKBitmap
            bool success = image.ReadPixels(copy.Info, copy.GetPixels(), copy.RowBytes, 0, 0);
            if (!success)
                throw new Exception("Failed to convert SKImage to SKBitmap.");

            // Only do the clone if saving as GIF (optional in SkiaSharp)
            if (format == SKEncodedImageFormat.Gif)
            {
                using (SKSurface surface = SKSurface.Create(new SKImageInfo(copy.Width, copy.Height)))
                {
                    SKCanvas canvas = surface.Canvas;

                    // Clear the surface (optional)
                    canvas.Clear(SKColors.Transparent);

                    // Draw the original bitmap onto the new surface
                    canvas.DrawBitmap(copy, 0, 0);

                    // Take a snapshot as a new SKImage
                    using (SKImage clonedImage = surface.Snapshot())
                    {
                        // Convert back to SKBitmap if you need a mutable bitmap
                        SKBitmap clonedBitmap = new SKBitmap(clonedImage.Width, clonedImage.Height);
                        clonedImage.ReadPixels(clonedBitmap.Info, clonedBitmap.GetPixels(), clonedBitmap.RowBytes, 0, 0);

                        copy = clonedBitmap;
                    }
                }
            }

            using (MemoryStream ms = new MemoryStream())
            {
                if (ProduceMonochromeImages)
	            {
                    if (format == SKEncodedImageFormat.Png)
                        Utils.SaveAsBitonalTiff(copy, ms);
                    else
                    {
                        // Assume 'copy' is SKBitmap
                        using (SKBitmap bitonal = Utils.ConvertToBitonal(copy))
                        using (SKData data = bitonal.Encode(format, 100))
                        {
                            data.SaveTo(ms);
                        }
                    }
                }
	            else
                {
                    try
                    {
                        using (SKData data = copy.Encode(format, 100))
                        {
                            data.SaveTo(ms);
                        }
                    }
                    catch (ExternalException)
                    {
                        // refs #526 (crashing on batch generation of barcodes)
                        // some delay as advised on https://social.msdn.microsoft.com/Forums/vstudio/en-US/b15357f1-ad9d-4c80-9ec1-92c786cca4e6/bitmapsave-a-generic-error-occurred-in-gdi?forum=netfxbcl
                        Thread.Sleep(30);
                        // try to save again
                        ms.Position = 0; // reset the position to zero
                        using (SKData data = copy.Encode(format, 100))
                        {
                            data.SaveTo(ms);
                        }
                    }
                }

                // finally write to the output stream
	            ms.WriteTo(stream);
            }

            copy.Dispose();

            image.Dispose();
        }

        /// <inheritdoc />
        public void DrawToImage(string inputFile, int pageIndex, int x, int y, string outputFile)
	    {
		    if (String.IsNullOrEmpty(inputFile))
                throw new ArgumentException("inputFile should not by null or empty.", "inputFile");

            if (!File.Exists(inputFile))
                throw new ArgumentException("inputFile should specify existing PDF file.", "inputFile");

            if (String.Compare(inputFile, outputFile, StringComparison.OrdinalIgnoreCase) == 0)
                throw new ArgumentException("Cannot not save to the same file.", "outputFile");

            using (Stream inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (Stream outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite))
                DrawToImage(inputStream, pageIndex, x, y, outputStream);
        }

        /// <inheritdoc />
	    public void DrawToImage(Stream inputStream, int pageIndex, int x, int y, Stream outputStream)
	    {
		    if (inputStream == null)
                throw new ArgumentNullException("inputStream");

            if (outputStream == null)
                throw new ArgumentNullException("outputStream");

            if (!inputStream.CanRead)
                throw new ArgumentException("inputStream must be readable.", "inputStream");

            if (!outputStream.CanWrite)
                throw new ArgumentException("outputStream must be writable.", "outputStream");

            using (ImageStamper imageStamper = new ImageStamper())
                imageStamper.DrawToImage(inputStream, GetImage(), pageIndex, x, y, outputStream);
        }

        /// <inheritdoc />
	    public void SetForeColorRGB(byte r, byte g, byte b)
	    {
		    ForeColor = new SKColor((byte)r, (byte)g, (byte)b);
	    }

        /// <inheritdoc />
	    public void SetBackColorRGB(byte r, byte g, byte b)
	    {
		    BackColor = new SKColor((byte)r, (byte)g, (byte)b);
        }

        /// <inheritdoc />
	    public void SetCustomCaptionGap(float gap, UnitOfMeasure unit)
	    {
		    _drawing.CustomCaptionGap = (float.IsNaN(gap)) ? -1 : Utils.UnitsToPixels(_resolutionY, gap, unit);
	    }

        /// <summary>
        /// Determines whether the symbology is of 2D type.
        /// </summary>
        /// <param name="symbology">BarcodeEncoder type to check whether it's of 2D type.</param>
        /// <returns><c>True</c> if the specified symbology is of 2D type.</returns>
        public static bool Is2DSymblogy(SymbologyType symbology)
	    {
            return symbology == SymbologyType.Aztec || symbology == SymbologyType.DataMatrix || symbology == SymbologyType.MaxiCode ||
                   symbology == SymbologyType.PDF417 || symbology == SymbologyType.PDF417Truncated ||
                   symbology == SymbologyType.MacroPDF417 || symbology == SymbologyType.MicroPDF417 || symbology == SymbologyType.QRCode;
	    }

	    /// <inheritdoc />
	    public string ProcessMacros(string value)
	    {
		    StringBuilder builder = new StringBuilder(value);

		    builder.Replace("{NUL}", ((char) 0).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{SOH}", ((char) 1).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{STX}", ((char) 2).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{ETX}", ((char) 3).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{EOT}", ((char) 4).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{ENQ}", ((char) 5).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{ACK}", ((char) 6).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{BEL}", ((char) 7).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{BS}", ((char) 8).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{TAB}", ((char) 9).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{LF}", ((char) 10).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{VT}", ((char) 11).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{FF}", ((char) 12).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{CR}", ((char) 13).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{SO}", ((char) 14).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{SI}", ((char) 15).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{DLE}", ((char) 16).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{DC1}", ((char) 17).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{DC2}", ((char) 18).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{DC3}", ((char) 19).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{DC4}", ((char) 20).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{NAK}", ((char) 21).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{SYN}", ((char) 22).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{ETB}", ((char) 23).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{CAN}", ((char) 24).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{EM}", ((char) 25).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{SUB}", ((char) 26).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{ESC}", ((char) 27).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{FS}", ((char) 28).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{GS}", ((char) 29).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{RS}", ((char) 30).ToString(CultureInfo.InvariantCulture));
		    builder.Replace("{US}", ((char) 31).ToString(CultureInfo.InvariantCulture));

		    return builder.ToString();
	    }

        /// <inheritdoc />
        public void AddDecorationImage(string imageFileName, int scale)
        {
            byte[] bytes = File.ReadAllBytes(imageFileName);
            MemoryStream memoryStream = new MemoryStream(bytes);
            _decorationImage = SKImage.FromBitmap(SKBitmap.Decode(memoryStream));
            _decorationImageScale = scale;
        }

        /// <inheritdoc />
        [ComVisible(false)]
        public void AddDecorationImage(SKImage image, int scale)
        {
            _decorationImage = image;
            _decorationImageScale = scale;
        }
    }
}
