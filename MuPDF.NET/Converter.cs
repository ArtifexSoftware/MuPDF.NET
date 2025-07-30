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
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace MuPDF.NET
{

    /// <summary>
    /// Summary description for Class1
    /// </summary>
    public class BarCodeConverterClass
    {
        private readonly string _configFile = null;
        private readonly object _reader = null;


        public BarCodeConverterClass(string barcodeType, string configFile)
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
        private List<Rectangle> listFoundBarcodesAsRectangles = new List<Rectangle>();

        /// <summary>
        /// returns found barcodes as an array of strings
        /// </summary>
        /// <returns></returns>
        public string[] GetFoundBarcodesAsStrings()
        {
            return listFoundBarcodesAsStrings.ToArray();
        }

        public Rectangle[] GetFoundBarcodesAsRectangles()
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
                    int result = x.Rect.Top - y.Rect.Top;

                    if (result == 0)
                        result = x.Rect.Left - y.Rect.Left;

                    return result;
                });

                if (foundBarcodes != null)
                {
                    foreach (FoundBarcode barcode in foundBarcodes)
                    {
	                    StringBuilder tag = new StringBuilder();

						// Add cells for detected tables
	                    if (_reader is LineTablesDetector)
	                    {
		                    foreach (ArrayList row in (ArrayList) barcode.Tag)
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
                            barcode.Rect = Rectangle.Round(new RectangleF(bounds.Left, bounds.Top, bounds.Width, bounds.Height));
                        }

						listFoundBarcodesAsRectangles.Add(barcode.Rect);
                    }
                }

                GotException = false;

                return listFoundBarcodesAsStrings.Count>0;

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
}