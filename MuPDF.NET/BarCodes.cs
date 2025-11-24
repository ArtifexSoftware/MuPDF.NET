using BarcodeReader.Core;
using BarcodeReader.Core.AustraliaPostCode;
using BarcodeReader.Core.Aztec;
using BarcodeReader.Core.CodaBlockF;
using BarcodeReader.Core.Code128;
using BarcodeReader.Core.Code16K;
using BarcodeReader.Core.Code39;
using BarcodeReader.Core.Code93;
using BarcodeReader.Core.Common;
using BarcodeReader.Core.Datamatrix;
using BarcodeReader.Core.EAN;
using BarcodeReader.Core.FormLines;
using BarcodeReader.Core.FormOMR;
using BarcodeReader.Core.FormTables;
using BarcodeReader.Core.GS1DataBar;
using BarcodeReader.Core.IntelligentMail;
using BarcodeReader.Core.LegacyDecoders;
using BarcodeReader.Core.MaxiCode;
using BarcodeReader.Core.MICR;
using BarcodeReader.Core.MicroPDF;
using BarcodeReader.Core.MSI;
using BarcodeReader.Core.PatchCode;
using BarcodeReader.Core.PDF417;
using BarcodeReader.Core.Pharmacode;
using BarcodeReader.Core.PostNet;
using BarcodeReader.Core.QR;
using BarcodeReader.Core.RoyalMail;
using BarcodeReader.Core.RoyalMailKIX;
using BarcodeReader.Core.TriOptic;
using BarcodeWriter.Core;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using SymbologyType = BarcodeWriter.Core.SymbologyType;

namespace MuPDF.NET
{
    internal sealed class Config
    {
        public bool TryHarder { get; set; }
        public bool TryInverted { get; set; }
        public bool PureBarcode { get; set; }
        public bool Multi { get; set; } = true;
        public int[] Crop { get; set; }
        public int Threads { get; set; }
        public bool AutoRotate { get; set; }

        public Config()
        {
            Multi = true;
            Threads = 1;
        }
    }

    /// <summary>
    /// Barcode Reader Class
    /// </summary>
    public class BarcodeReader
    {
        private readonly string _configFile = null;
        private readonly object _reader = null;

        static BarcodeReader()
        {
            Utils.InitApp();
        }

        public BarcodeReader(string barcodeType, string configFile)
        {
            _configFile = configFile;

            barcodeType = barcodeType.ToUpper();

            if (barcodeType == "AZTEC") _reader = new AztecReader();
            else if (barcodeType == "BOXES") _reader = new FieldsToFillDetector();
            else if (barcodeType == "CODABAR") _reader = new CodabarReader();
            else if (barcodeType == "CODABLOCKF") _reader = new CodaBlockFReader();
            else if (barcodeType == "CODE128") _reader = new Code128Reader();
            else if (barcodeType == "CODE16K") _reader = new Code16KReader();
            else if (barcodeType == "CODE39") _reader = new Code39Reader();
            else if (barcodeType == "CODE39_LINEARREADER") _reader = new Code39LinearReader() { };
            else if (barcodeType == "CODE39_EX") _reader = new Code39Reader() { UsePatternFinderNoiseRowEx = true };
            else if (barcodeType == "CODE39_NOISE1") _reader = new Code39Reader() { NoiseLevel = 1 };
            else if (barcodeType == "CODE93") _reader = new Code93Reader();
            else if (barcodeType == "DM") _reader = new DMReader();
            else if (barcodeType == "DM_DPM") _reader = new DM_DPM_Reader();
            else if (barcodeType == "EAN13") _reader = new EAN13LinearReader() { };
            else if (barcodeType == "EAN2") _reader = new EAN2LinearReader() { };
            else if (barcodeType == "EAN5") _reader = new EAN5LinearReader() { };
            else if (barcodeType == "EAN8") _reader = new EAN8LinearReader() { };
            else if (barcodeType == "EAN_UPC_OLD") _reader = new EANReader_old() { FindUpce = true, FindEan13 = true, FindEan2 = true, FindEan5 = true, FindEan8 = true, FindUpca = true };
            else if (barcodeType == "GS1DATABAREXP") _reader = new GS1DataBarExpanded();
            else if (barcodeType == "GS1DATABAREXPSTACKED") _reader = new GS1DataBarExpandedStacked();
            else if (barcodeType == "GS1DATABAROMNI") _reader = new GS1DataBarOmnidirectional();
            else if (barcodeType == "GS1DATABARSTACKED") _reader = new GS1DataBarStacked();
            else if (barcodeType == "GS1DATABARSTACKEDOMNI") _reader = new GS1DataBarStacked();
            else if (barcodeType == "GS1DATABARLIMITED") _reader = new GS1DataBarLimited();
            else if (barcodeType == "HORIZONTALLINES") _reader = new HorizontalLinesDecoder();
            else if (barcodeType == "I2OF5") _reader = new I2of5Reader();
            else if (barcodeType == "IM") _reader = new IMReader();
            else if (barcodeType == "KIX") _reader = new KIXReader();
            else if (barcodeType == "LINETABLES") _reader = new LineTablesDetector();
            else if (barcodeType == "MAXICODE") _reader = new MaxiCodeReader();
            else if (barcodeType == "MICR")
            {
                _reader = new MICRReader(); //throw new Exception("MICR is not enabled! Pleae fix!"); //_reader = new MICR();
                //(reader as MICRReader).RestrictHorizontal = true;
                //(reader as MICRReader).MinDigitCount = 5; // change from 15 to 10 min digit count (will search for blocks with 7 numbers or more)
                //(reader as MICRReader).MinRad = 5; // set min radius to 5 so it will read images like CanadianChequeSamplePAR.png

            }
            else if (barcodeType == "MICROPDF") _reader = new MicroPDFReader();
            else if (barcodeType == "MSI") _reader = new MSIReader();
            else if (barcodeType == "OMRCIRCLE") _reader = new FormOMRCircle();
            else if (barcodeType == "OMRCIRCLE_EXT") _reader = new FormOMRCircle() { ExtendedMode = true };
            else if (barcodeType == "OMROVAL") _reader = new FormOMROval();
            else if (barcodeType == "OMROVAL_EXT") _reader = new FormOMROval() { ExtendedMode = true };
            else if (barcodeType == "OMRSQUARE") _reader = new FormOMRSquare();
            else if (barcodeType == "OMRSQUARE_EXT") _reader = new FormOMRSquare() { ExtendedMode = true };
            else if (barcodeType == "OMRSQUARELPATTERN") _reader = new FormOMRSquareLPattern();
            else if (barcodeType == "OMRRECTANGLE") _reader = new FormOMRRectangle();
            else if (barcodeType == "OMRRECTANGLE_EXT") _reader = new FormOMRRectangle() { ExtendedMode = true };
            else if (barcodeType == "OMRRECTANGLELPATTERNVERT") _reader = new FormOMRRectangleLPatternVert();
            else if (barcodeType == "OMRRECTANGLELPATTERNHORIZ") _reader = new FormOMRRectangleLPatternHoriz();
            else if (barcodeType == "PATCH") _reader = new PatchCodeReader();
            else if (barcodeType == "PHARMA") _reader = new PharmaReader();
            else if (barcodeType == "PDF417") _reader = new PDF417Reader();
            else if (barcodeType == "POSTCODE") _reader = new PostCodeReader();
            else if (barcodeType == "POSTNET") _reader = new PostNetReader();
            else if (barcodeType == "QR") _reader = new QRReader();
            else if (barcodeType == "RAWOMR") _reader = new RawSlicer();
            else if (barcodeType == "RM") _reader = new RMReader();
            else if (barcodeType == "VERTICALLINES") _reader = new VerticalLinesDecoder();
            else if (barcodeType == "UPC_A") _reader = new UPCALinearReader() { };
            else if (barcodeType == "UPC_E") _reader = new UPCELinearReader() { };
            else if (barcodeType == "TRIOPTIC") _reader = new TriOpticLinearReader();
            else throw new Exception("unknown decoder type");
        }


        /// <summary>
        /// Output results in string[] form
        /// </summary>
        private List<string> listFoundBarcodesAsStrings = new List<string>();
        private List<SKRect> listFoundBarcodesAsRectangles = new List<SKRect>();

        /// <summary>
        /// returns found barcodes as an array of strings
        /// </summary>
        /// <returns></returns>
        public string[] GetFoundBarcodesAsStrings()
        {
            return listFoundBarcodesAsStrings.ToArray();
        }

        public SKRect[] GetFoundBarcodesAsRectangles()
        {
            return listFoundBarcodesAsRectangles.ToArray();
        }


        /// <summary>
        /// Contains exception message
        /// </summary>
        public string ExceptionMessage = null;

        /// <summary>
        /// Indicates if got exception last time decoding bitmap
        /// </summary>
        public bool GotException = false;

        /// <summary>
        /// last decoding time
        /// </summary>
        public long LastDecodingTime = 0;

        /// <summary>
        /// Runs the decoding
        /// </summary>
        /// <param name="bitmap">input image</param>
        /// <returns>true if decoded successfully (see FoundBarcodes property) or false (see ExceptionMessage for more info)</returns>
        public bool Decode(SKBitmap bitmap)
        {
            try
            {
                if (_configFile != null)
                    ApplyConfig(_reader, _configFile);

                if (_reader is QRReader)
                {
                    (_reader as QRReader).MergePartialBarcodes = true;
                }

                // reset
                LastDecodingTime = 0;

                FoundBarcode[] foundBarcodes = null;
                BlackAndWhiteImage bwImage;

                if (_reader is SymbologyReader2D)
                {
                    bwImage = new BlackAndWhiteImage(bitmap, 1, (_reader as SymbologyReader2D).ThresholdFilterMethodToUse, 24);
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    foundBarcodes = (_reader as SymbologyReader2D).Decode(bwImage);
                    LastDecodingTime = sw.ElapsedMilliseconds;
                }
                else // if (reader is SymbologyReader)
                {
                    bwImage = new BlackAndWhiteImage(bitmap, 1, ThresholdFilterMethod.Block, 24);
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    foundBarcodes = (_reader as SymbologyReader).Decode(bwImage);
                    LastDecodingTime = sw.ElapsedMilliseconds;
                }

                listFoundBarcodesAsStrings.Clear();
                listFoundBarcodesAsRectangles.Clear();

                // Sort results by Y, then X
                Array.Sort(foundBarcodes, (x, y) =>
                {
                    float result = x.Rect.Top - y.Rect.Top;

                    if (result == 0f)
                        result = x.Rect.Left - y.Rect.Left;

                    return (int)result;
                });

                if (foundBarcodes != null)
                {
                    foreach (FoundBarcode barcode in foundBarcodes)
                    {
                        StringBuilder tag = new StringBuilder();

                        // Add cells for detected tables
                        if (_reader is LineTablesDetector)
                        {
                            foreach (ArrayList row in (ArrayList)barcode.Tag)
                                tag.AppendFormat(" {0}", row.Count);
                        }

                        listFoundBarcodesAsStrings.Add(barcode.Value + tag);

                        if (barcode.Rect.IsEmpty && barcode.Polygon != null)
                        {
                            // Get bounding rectangle from barcode polygon
                            /*
							byte[] pointTypes = new byte[5] { (byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line };
							GraphicsPath path = new GraphicsPath(barcode.Polygon, pointTypes);
							barcode.Rect = Rectangle.Round(path.GetBounds());
                            */
                            // Assuming barcode.Polygon is a PointF[]
                            SKPointI[] polygon = barcode.Polygon;

                            var path = new SKPath();

                            // Move to first point
                            path.MoveTo(polygon[0].X, polygon[0].Y);

                            // Draw lines to remaining points
                            for (int i = 1; i < polygon.Length; i++)
                            {
                                path.LineTo(polygon[i].X, polygon[i].Y);
                            }

                            // Optional: close the shape
                            path.Close();

                            // Get bounds
                            SKRect bounds = path.Bounds;

                            // Set barcode.Rect as a System.Drawing.Rectangle
                            barcode.Rect = new SKRect(bounds.Left, bounds.Top, bounds.Left+bounds.Width, bounds.Top+bounds.Height);
                        }

                        listFoundBarcodesAsRectangles.Add(barcode.Rect);
                    }
                }

                GotException = false;

                return listFoundBarcodesAsStrings.Count > 0;

            }
            catch (Exception e)
            {
                GotException = true;
                listFoundBarcodesAsStrings.Clear();
                ExceptionMessage = $"{e.GetType().ToString()}, {e.Message}\r\nException:\r\n{e.InnerException.ToString()}";
                return false;
            }
        }

        private void ApplyConfig(object reader, string configFile)
        {
            StreamReader textReader = File.OpenText(configFile);

            while (true)
            {
                var line = textReader.ReadLine();
                if (line == null)
                    break;

                line = line.Trim();
                if (line.Length == 0)
                    continue;

                string[] argument = line.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (argument.Length >= 1 && argument.Length <= 2)
                {
                    // Split by dot assuming we can have a chain,
                    // e.g. `MidProperty1.MidProperty2.Property` or `MidProperty1.MidProperty2.Method()`
                    string[] propertyChain = argument[0].Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    object @object = reader;
                    Type type = reader.GetType();

                    for (int i = 0; i < propertyChain.Length; i++)
                    {
                        string memberName = propertyChain[i];

                        // recurse mid properties
                        if (i < propertyChain.Length - 1)
                        {
                            PropertyInfo propertyInfo = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                            if (propertyInfo == null)
                                throw new Exception($"Property \"{memberName}\" does not exist in \"{type}\"");

                            @object = propertyInfo.GetValue(@object, null);
                            type = @object.GetType();
                        }
                        else // process the last part of the chain: a property or method
                        {
                            if (argument.Length == 2) // .Property=Value
                            {
                                PropertyInfo propertyInfo = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                                if (propertyInfo == null)
                                    throw new Exception($"Property \"{memberName}\" does not exist in \"{type}\"");

                                try
                                {
                                    TypeConverter typeConverter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
                                    object value = typeConverter.ConvertFromInvariantString(argument[1]);
                                    propertyInfo.SetValue(@object, value, BindingFlags.FlattenHierarchy, null, null, CultureInfo.InvariantCulture);
                                }
                                catch (Exception exception)
                                {
                                    throw new Exception($"Could not set value \"{argument[1]}\" to property \"{argument[0]}\"", exception);
                                }
                            }
                            else if (memberName.EndsWith("()", StringComparison.Ordinal)) // .Method()
                            {
                                string methodName = memberName.Substring(0, memberName.Length - 2);
                                MethodInfo methodInfo = type.GetMethod(methodName, new Type[0]);
                                if (methodInfo == null)
                                    throw new Exception($"Method \"{methodName}\" does not exist in \"{type}\"");

                                try
                                {
                                    methodInfo.Invoke(@object, null);
                                }
                                catch (Exception exception)
                                {
                                    throw new Exception($"Could not invoke method \"{argument[0]}\"", exception);
                                }
                            }
                            else throw new Exception($"Invalid config argument \"{line}\"");
                        }
                    }

                    Console.WriteLine($"Configuration: {line}");
                }
                else throw new Exception($"Invalid config argument \"{line}\"");
            }
        }
    }

    /// <summary>
    /// Barcode Writer Class
    /// </summary>
    public class BarcodeWriter
    {
        private readonly SymbologyType _symbologyType = SymbologyType.Unknown;
        private readonly object _encoder = null;

        static BarcodeWriter()
        {
            Utils.InitApp();
        }
        public BarcodeWriter(string barcodeType)
        {
            switch (barcodeType)
            {
                case "CODE128": _symbologyType = SymbologyType.Code128; break;
                case "CODE39": _symbologyType = SymbologyType.Code39; break;
                case "POSTNET": _symbologyType = SymbologyType.Postnet; break;
                case "UPC_A": _symbologyType = SymbologyType.UPCA; break;  // UPC-A
                case "EAN8": _symbologyType = SymbologyType.EAN8; break;
                case "CODABAR": _symbologyType = SymbologyType.Codabar; break;
                case "I2OF5": _symbologyType = SymbologyType.I2of5; break;
                case "CODE93": _symbologyType = SymbologyType.Code93; break;
                case "EAN13": _symbologyType = SymbologyType.EAN13; break;
                case "JAN13": _symbologyType = SymbologyType.JAN13; break;
                case "BOOKLAND": _symbologyType = SymbologyType.Bookland; break;
                case "UPC_E": _symbologyType = SymbologyType.UPCE; break;  // UPC-E
                case "PDF417": _symbologyType = SymbologyType.PDF417; break;
                case "PDF417_TRUNCATED": _symbologyType = SymbologyType.PDF417Truncated; break;
                case "DM": _symbologyType = SymbologyType.DataMatrix; break;
                case "QR": _symbologyType = SymbologyType.QRCode; break;
                case "AZTEC": _symbologyType = SymbologyType.Aztec; break;
                case "PLANET": _symbologyType = SymbologyType.Planet; break;
                case "EAN128": _symbologyType = SymbologyType.EAN128; break;
                case "GS1_128": _symbologyType = SymbologyType.GS1_128; break;
                case "USPSSACKLABEL": _symbologyType = SymbologyType.USPSSackLabel; break;
                case "USPSTRAYLABEL": _symbologyType = SymbologyType.USPSTrayLabel; break;
                case "DEUTSCHEPOSTIDENTCODE": _symbologyType = SymbologyType.DeutschePostIdentcode; break;
                case "DEUTSCHEPOSTLEITCODE": _symbologyType = SymbologyType.DeutschePostLeitcode; break;
                case "NUMLY": _symbologyType = SymbologyType.Numly; break;
                case "PZN": _symbologyType = SymbologyType.PZN; break;
                case "OPTICALPRODUCT": _symbologyType = SymbologyType.OpticalProduct; break;
                case "SWISSPOSTPARCEL": _symbologyType = SymbologyType.SwissPostParcel; break;
                case "RM": _symbologyType = SymbologyType.RoyalMail; break;     // Royal Mail
                case "DUTCHKIX": _symbologyType = SymbologyType.DutchKix; break;
                case "SINGAPORE": _symbologyType = SymbologyType.SingaporePostalCode; break;
                case "EAN2": _symbologyType = SymbologyType.EAN2; break;
                case "EAN5": _symbologyType = SymbologyType.EAN5; break;
                case "EAN14": _symbologyType = SymbologyType.EAN14; break;
                case "MACROPDF417": _symbologyType = SymbologyType.MacroPDF417; break;
                case "MICROPDF417": _symbologyType = SymbologyType.MicroPDF417; break;
                case "GS1DATAMATRIX": _symbologyType = SymbologyType.GS1_DataMatrix; break;
                case "TELEPEN": _symbologyType = SymbologyType.Telepen; break;
                case "IM": _symbologyType = SymbologyType.IntelligentMail; break;
                case "GS1DATABAROMNI": _symbologyType = SymbologyType.GS1_DataBar_Omnidirectional; break;
                case "GS1DATABARTRUNCATED": _symbologyType = SymbologyType.GS1_DataBar_Truncated; break;
                case "GS1DATABARSTACKED": _symbologyType = SymbologyType.GS1_DataBar_Stacked; break;
                case "GS1DATABARSTACKEDOMNI": _symbologyType = SymbologyType.GS1_DataBar_Stacked_Omnidirectional; break;
                case "GS1DATABARLIMITED": _symbologyType = SymbologyType.GS1_DataBar_Limited; break;
                case "GS1DATABAREXP": _symbologyType = SymbologyType.GS1_DataBar_Expanded; break;
                case "MAXICODE": _symbologyType = SymbologyType.MaxiCode; break;
                case "PLESSEY": _symbologyType = SymbologyType.Plessey; break;
                case "MSI": _symbologyType = SymbologyType.MSI; break;
                case "ITF14": _symbologyType = SymbologyType.ITF14; break;
                case "GTIN12": _symbologyType = SymbologyType.GTIN12; break;
                case "GTIN8": _symbologyType = SymbologyType.GTIN8; break;
                case "GTIN13": _symbologyType = SymbologyType.GTIN13; break;
                case "GTIN14": _symbologyType = SymbologyType.GTIN14; break;
                case "GS1_QRCODE": _symbologyType = SymbologyType.GS1_QRCode; break; // Tri-Optic
                case "PHARMA": _symbologyType = SymbologyType.PharmaCode; break;
                default: _symbologyType = SymbologyType.Unknown; break;
            }
        }

        public SKBitmap Encode(
            string contents,
            SKEncodedImageFormat imageFormat = SKEncodedImageFormat.Png,
            int width = 0,
            int height = 0,
            string characterSet = null,
            bool disableEci = false,
            bool forceFitToRect = false,
            bool pureBarcode = false,
            int marginLeft = 0,
            int marginTop = 0,
            int marginRight = 0,
            int marginBottom = 0,
            int barHeight = 0, 
            int narrowBarWidth = 0
            )
        {
            if (contents == null)
            {
                return null;
            }
            if (_symbologyType == SymbologyType.Unknown)
            {
                throw new Exception("Unknown symbology type");
            }

            using (BarcodeEncoder encoder = new BarcodeEncoder())
            {
                encoder.Value = contents;
                
                encoder.Symbology = _symbologyType;
                encoder.DrawCaption = !pureBarcode;
                encoder.Margins = new Margins(marginLeft, marginTop, marginRight, marginBottom);
                encoder.DrawQuietZones = false;

                if (barHeight > 0)
                    encoder.BarHeight = barHeight;
                else if (height > 0)
                    encoder.BarHeight = height;

                if (forceFitToRect == true && width > 0 && height > 0)
                {
                    encoder.PreserveMinReadableSize = false;
                    encoder.FitInto(width, height);
                }

                if (narrowBarWidth > 0)
                {
                    encoder.NarrowBarWidth = narrowBarWidth;
                }
                else
                {
                    switch (_symbologyType)
                    {
                        case SymbologyType.PDF417:
                        case SymbologyType.PDF417Truncated:
                        case SymbologyType.DataMatrix:
                        case SymbologyType.QRCode:
                        case SymbologyType.Aztec:
                        case SymbologyType.MicroPDF417:
                        case SymbologyType.MacroPDF417:
                        case SymbologyType.GS1_DataMatrix:
                        case SymbologyType.GS1_QRCode:
                        case SymbologyType.MaxiCode:
                            encoder.NarrowBarWidth = 3;
                            break;
                        default:
                            encoder.NarrowBarWidth = 1;
                            break;
                    }
                }

                SKBitmap image = encoder.GetImage();

                return image;
            }
        }

        public void Encode(
            string imageFile,
            string contents,
            SKEncodedImageFormat imageFormat = SKEncodedImageFormat.Png,
            int width = 0,
            int height = 0,
            string characterSet = null,
            bool disableEci = false,
            bool forceFitToRect = false,
            bool pureBarcode = false,
            int marginLeft = 0,
            int marginTop = 0,
            int marginRight = 0,
            int marginBottom = 0,
            int barHeight = 0,
            int narrowBarWidth = 0
            )
        {
            if (contents == null)
            {
                return;
            }
            if (_symbologyType == SymbologyType.Unknown)
            {
                throw new Exception("Unknown symbology type");
            }

            using (BarcodeEncoder encoder = new BarcodeEncoder())
            {
                encoder.Value = contents;
                encoder.Symbology = _symbologyType;
                encoder.DrawCaption = !pureBarcode;
                encoder.Margins = new Margins(marginLeft, marginTop, marginRight, marginBottom);
                encoder.DrawQuietZones = false;

                if (barHeight > 0)
                    encoder.BarHeight = barHeight;
                else if (height > 0)
                    encoder.BarHeight = height;

                if (forceFitToRect == true && width > 0 && height > 0)
                {
                    encoder.PreserveMinReadableSize = false;
                    encoder.FitInto(width, height);
                }

                if (narrowBarWidth > 0)
                {
                    encoder.NarrowBarWidth = narrowBarWidth;
                }
                else
                {
                    switch (_symbologyType)
                    {
                        case SymbologyType.PDF417:
                        case SymbologyType.PDF417Truncated:
                        case SymbologyType.DataMatrix:
                        case SymbologyType.QRCode:
                        case SymbologyType.Aztec:
                        case SymbologyType.MicroPDF417:
                        case SymbologyType.MacroPDF417:
                        case SymbologyType.GS1_DataMatrix:
                        case SymbologyType.GS1_QRCode:
                        case SymbologyType.MaxiCode:
                            encoder.NarrowBarWidth = 3;
                            break;
                        default:
                            encoder.NarrowBarWidth = 1;
                            break;
                    }
                }

                if (forceFitToRect == false)
                    encoder.SaveImage(imageFile, imageFormat);
                else
                    encoder.SaveImage(imageFile, imageFormat, new SKSize(width, height), 0, 0);
            }
        }
    }
}