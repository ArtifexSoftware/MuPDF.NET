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
    /// <summary>
    /// Internal decode options used by pixmap barcode helpers (not part of the public API).
    /// </summary>
    internal sealed class Config
    {
        /// <summary>Spend extra effort locating difficult symbols.</summary>
        public bool TryHarder { get; set; }

        /// <summary>Also search inverted (light-on-dark) barcodes.</summary>
        public bool TryInverted { get; set; }

        /// <summary>Assume the image contains only a barcode (no surrounding graphics).</summary>
        public bool PureBarcode { get; set; }

        /// <summary>When <see langword="true"/>, decode multiple symbols per image.</summary>
        public bool Multi { get; set; } = true;

        /// <summary>Optional crop rectangle as <c>[x0, y0, x1, y1]</c> in pixels.</summary>
        public int[] Crop { get; set; }

        /// <summary>Number of threads for parallel decode (default 1).</summary>
        public int Threads { get; set; }

        /// <summary>Try rotated orientations of the image.</summary>
        public bool AutoRotate { get; set; }

        /// <summary>Initializes default config (<see cref="Multi"/> = true, <see cref="Threads"/> = 1).</summary>
        public Config()
        {
            Multi = true;
            Threads = 1;
        }
    }

    /// <summary>
    /// Decodes barcodes from Skia bitmaps using the bundled BarcodeReader engine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Construct with a symbology name (case-insensitive) and an optional configuration file path.
    /// Call <see cref="Decode"/> on a <see cref="SKBitmap"/>, then read results via
    /// <see cref="GetFoundBarcodesAsStrings"/> and <see cref="GetFoundBarcodesAsRectangles"/>.
    /// </para>
    /// <para>
    /// Supported <c>barcodeType</c> values include <c>QR</c>, <c>DM</c>, <c>PDF417</c>, <c>CODE128</c>,
    /// <c>CODE39</c>, <c>EAN13</c>, <c>AZTEC</c>, <c>IM</c>, OMR variants (<c>OMRCIRCLE</c>, …),
    /// <c>MICR</c>, <c>RM</c>, GS1 DataBar types, and others (see constructor implementation).
    /// </para>
    /// </remarks>
    public class BarcodeReader
    {
        private readonly string _configFile = null;
        private readonly object _reader = null;

        /// <summary>
        /// Creates a reader for one symbology.
        /// </summary>
        /// <param name="barcodeType">
        /// Symbology identifier (e.g. <c>"QR"</c>, <c>"CODE128"</c>, <c>"EAN13"</c>). Compared case-insensitively.
        /// </param>
        /// <param name="configFile">
        /// Optional path to a text configuration file with <c>Property=value</c> or <c>Method()</c> lines
        /// applied to the underlying decoder via reflection. Pass <c>null</c> to skip.
        /// </param>
        /// <exception cref="Exception">Thrown when <paramref name="barcodeType"/> is not recognized.</exception>
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


        /// <summary>Decoded barcode text values from the last successful <see cref="Decode"/> call.</summary>
        private List<string> listFoundBarcodesAsStrings = new List<string>();

        /// <summary>Bounding rectangles for each decoded symbol from the last <see cref="Decode"/> call.</summary>
        private List<SKRect> listFoundBarcodesAsRectangles = new List<SKRect>();

        /// <summary>
        /// Returns decoded barcode values from the most recent <see cref="Decode"/> operation.
        /// </summary>
        /// <returns>
        /// Array of decoded strings, sorted top-to-bottom then left-to-right. Empty if nothing was found
        /// or the last decode failed.
        /// </returns>
        public string[] GetFoundBarcodesAsStrings()
        {
            return listFoundBarcodesAsStrings.ToArray();
        }

        /// <summary>
        /// Returns bounding rectangles for symbols found by the most recent <see cref="Decode"/> call.
        /// </summary>
        /// <returns>
        /// <see cref="SKRect"/> array parallel to <see cref="GetFoundBarcodesAsStrings"/> (same index order).
        /// </returns>
        public SKRect[] GetFoundBarcodesAsRectangles()
        {
            return listFoundBarcodesAsRectangles.ToArray();
        }

        /// <summary>
        /// Message from the last decode exception, or <c>null</c> if the last <see cref="Decode"/> succeeded.
        /// </summary>
        public string ExceptionMessage = null;

        /// <summary>
        /// Whether the last <see cref="Decode"/> call threw an exception.
        /// </summary>
        public bool GotException = false;

        /// <summary>
        /// Elapsed milliseconds for the last <see cref="Decode"/> call (native decoder time only).
        /// </summary>
        public long LastDecodingTime = 0;

        /// <summary>
        /// Decodes barcodes in the given bitmap.
        /// </summary>
        /// <param name="bitmap">Source image (converted internally to a black-and-white representation).</param>
        /// <returns>
        /// <see langword="true"/> if at least one symbol was found; <see langword="false"/> on failure or when
        /// no barcodes are detected. Check <see cref="GotException"/> and <see cref="ExceptionMessage"/> on failure.
        /// </returns>
        /// <remarks>
        /// Clears previous results, applies the optional config file from construction, runs the symbology-specific
        /// decoder (2D vs linear thresholding), sorts results by position, and fills
        /// <see cref="GetFoundBarcodesAsStrings"/> / <see cref="GetFoundBarcodesAsRectangles"/>.
        /// For <c>QR</c>, partial symbols may be merged before returning.
        /// </remarks>
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

        /// <summary>
        /// Applies decoder settings from a line-based configuration file.
        /// </summary>
        /// <param name="reader">Underlying symbology reader instance.</param>
        /// <param name="configFile">Path to a text file with <c>Name=value</c> or <c>Type.Method()</c> lines.</param>
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
    /// Encodes barcode symbols into Skia bitmaps or image files using the bundled BarcodeWriter engine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Construct with a symbology name (e.g. <c>"QR"</c>, <c>"CODE128"</c>, <c>"EAN13"</c>), then call
    /// <see cref="Encode(string, SKEncodedImageFormat, int, int, string, bool, bool, bool, int, int, int, int, int, int)"/>
    /// to obtain an <see cref="SKBitmap"/>, or the file overload to write PNG (or other format) directly.
    /// </para>
    /// </remarks>
    public class BarcodeWriter
    {
        private readonly SymbologyType _symbologyType = SymbologyType.Unknown;
        private readonly object _encoder = null;

        /// <summary>
        /// Creates a writer for one symbology.
        /// </summary>
        /// <param name="barcodeType">
        /// Symbology name: <c>CODE128</c>, <c>CODE39</c>, <c>QR</c>, <c>DM</c>, <c>PDF417</c>, <c>EAN13</c>,
        /// <c>UPC_A</c>, <c>AZTEC</c>, GS1 variants, and others supported by <see cref="SymbologyType"/>.
        /// Unknown names map to <see cref="SymbologyType.Unknown"/> and cause <see cref="Encode"/> to throw.
        /// </param>
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

        /// <summary>
        /// Renders a barcode into a new in-memory Skia bitmap.
        /// </summary>
        /// <param name="contents">Data to encode (symbology-specific format).</param>
        /// <param name="imageFormat">Reserved for file encoding; bitmap output is format-agnostic.</param>
        /// <param name="width">Target width when <paramref name="forceFitToRect"/> is <see langword="true"/>.</param>
        /// <param name="height">Target height when <paramref name="forceFitToRect"/> is <see langword="true"/>; also used as bar height if <paramref name="barHeight"/> is 0.</param>
        /// <param name="characterSet">Reserved (not applied by current encoder).</param>
        /// <param name="disableEci">Reserved (not applied by current encoder).</param>
        /// <param name="forceFitToRect">When <see langword="true"/> with positive <paramref name="width"/> and <paramref name="height"/>, scale to fit the rectangle.</param>
        /// <param name="pureBarcode">When <see langword="true"/>, omit human-readable caption under the symbol.</param>
        /// <param name="marginLeft">Quiet zone / margin on the left (pixels).</param>
        /// <param name="marginTop">Quiet zone / margin on the top (pixels).</param>
        /// <param name="marginRight">Quiet zone / margin on the right (pixels).</param>
        /// <param name="marginBottom">Quiet zone / margin on the bottom (pixels).</param>
        /// <param name="barHeight">Explicit bar height in pixels; overrides <paramref name="height"/> when &gt; 0.</param>
        /// <param name="narrowBarWidth">Module width in pixels; 0 selects a symbology-appropriate default (1 or 3).</param>
        /// <returns>Encoded barcode image, or <c>null</c> if <paramref name="contents"/> is <c>null</c>.</returns>
        /// <exception cref="Exception">Thrown when symbology was not recognized at construction.</exception>
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

        /// <summary>
        /// Renders a barcode and saves it to an image file.
        /// </summary>
        /// <param name="imageFile">Output file path.</param>
        /// <param name="contents">Data to encode.</param>
        /// <param name="imageFormat">File format (e.g. <see cref="SKEncodedImageFormat.Png"/>).</param>
        /// <param name="width">Target width when <paramref name="forceFitToRect"/> is <see langword="true"/>.</param>
        /// <param name="height">Target height when <paramref name="forceFitToRect"/> is <see langword="true"/>.</param>
        /// <param name="characterSet">Reserved (not applied by current encoder).</param>
        /// <param name="disableEci">Reserved (not applied by current encoder).</param>
        /// <param name="forceFitToRect">When <see langword="true"/>, save using explicit <see cref="SKSize"/> dimensions.</param>
        /// <param name="pureBarcode">When <see langword="true"/>, omit human-readable caption.</param>
        /// <param name="marginLeft">Left margin in pixels.</param>
        /// <param name="marginTop">Top margin in pixels.</param>
        /// <param name="marginRight">Right margin in pixels.</param>
        /// <param name="marginBottom">Bottom margin in pixels.</param>
        /// <param name="barHeight">Bar height in pixels when &gt; 0.</param>
        /// <param name="narrowBarWidth">Module width; 0 uses symbology default.</param>
        /// <remarks>No-op when <paramref name="contents"/> is <c>null</c>.</remarks>
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