using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;
using ZXing.Common;
using System.Collections.Generic;
using System;
using System.IO;
using ZXing.Datamatrix.Encoder;
using System.Linq;

namespace MuPDF.NET
{
    internal sealed class Config
    {
        public IDictionary<DecodeHintType, object> Hints { get; set; }
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
    /// One of a pool of threads which pulls images off the Inputs queue and decodes them in parallel.
    /// @see CommandLineRunner
    /// </summary>
    internal sealed class Decode
    {
        public Result decode(string filePath, Config config)
        {
            SKBitmap image;
            try
            {
                image = SKBitmap.Decode(filePath);
            }
            catch (Exception e)
            {
                throw new FileNotFoundException("Resource not found: " + filePath + "(" + e.Message + ")");
            }

            LuminanceSource source;
            if (config.Crop == null)
            {
                source = new SKBitmapLuminanceSource(image);
            }
            else
            {
                int[] crop = config.Crop;
                source = new SKBitmapLuminanceSource(image).crop(crop[0], crop[1], crop[2], crop[3]);
            }
            var reader = new BarcodeReaderGeneric();
            reader.AutoRotate = config.AutoRotate;
            foreach (var entry in config.Hints)
                reader.Options.Hints.Add(entry.Key, entry.Value);
            Result result = reader.Decode(source);

            return result;
        }

        public Result decode(byte[] imageBuf, Config config)
        {
            SKBitmap image;
            try
            {
                image = SKBitmap.Decode(imageBuf);
            }
            catch (Exception e)
            {
                throw new FileNotFoundException("Resource not valid: " + "(" + e.Message + ")");
            }

            LuminanceSource source;
            if (config.Crop == null)
            {
                source = new SKBitmapLuminanceSource(image);
            }
            else
            {
                int[] crop = config.Crop;
                source = new SKBitmapLuminanceSource(image).crop(crop[0], crop[1], crop[2], crop[3]);
            }
            var reader = new BarcodeReaderGeneric();
            reader.AutoRotate = config.AutoRotate;
            foreach (var entry in config.Hints)
                reader.Options.Hints.Add(entry.Key, entry.Value);
            Result result = reader.Decode(source);

            return result;
        }

        public Result[] decodeMulti(string filePath, Config config)
        {
            SKBitmap image;
            try
            {
                image = SKBitmap.Decode(filePath);
            }
            catch (Exception e)
            {
                throw new FileNotFoundException("Resource not found: " + filePath + "(" + e.Message + ")");
            }

            LuminanceSource source;
            if (config.Crop == null)
            {
                source = new SKBitmapLuminanceSource(image);
            }
            else
            {
                int[] crop = config.Crop;
                source = new SKBitmapLuminanceSource(image).crop(crop[0], crop[1], crop[2], crop[3]);
            }

            var reader = new BarcodeReaderGeneric();
            reader.AutoRotate = config.AutoRotate;
            foreach (var entry in config.Hints)
                reader.Options.Hints.Add(entry.Key, entry.Value);

            Result[] results = reader.DecodeMultiple(source);

            return results;
        }

        public Result[] decodeMulti(byte[] imageBuf, Config config)
        {
            SKBitmap image;
            try
            {
                image = SKBitmap.Decode(imageBuf);
            }
            catch (Exception e)
            {
                throw new FileNotFoundException("Resource invalid: " + "(" + e.Message + ")");
            }

            LuminanceSource source;
            if (config.Crop == null)
            {
                source = new SKBitmapLuminanceSource(image);
            }
            else
            {
                int[] crop = config.Crop;
                source = new SKBitmapLuminanceSource(image).crop(crop[0], crop[1], crop[2], crop[3]);
            }

            var reader = new BarcodeReaderGeneric();
            reader.AutoRotate = config.AutoRotate;
            foreach (var entry in config.Hints)
                reader.Options.Hints.Add(entry.Key, entry.Value);

            Result[] results = { };

            try
            {
                results = reader.DecodeMultiple(source);
            }
            catch (Exception)
            {
                return new Result[] { };
            }

            return results;
        }
    }

    internal sealed class Encode
    {
        public SKBitmap encode(
            string contents,
            BarcodeFormat barcodeFormat,
            SKEncodedImageFormat imageFormat = SKEncodedImageFormat.Png,
            int width = 300,
            int height = 300,
            string characterSet = null,
            bool disableEci = false,
            bool pureBarcode = false,
            int margin = 1
            )
        {
            if (contents == null)
            {
                return null;
            }

            ZXing.BarcodeFormat encodeFormat = 0;
            switch (barcodeFormat)
            {
                case BarcodeFormat.AZTEC: encodeFormat = ZXing.BarcodeFormat.AZTEC; break;
                case BarcodeFormat.CODABAR: encodeFormat = ZXing.BarcodeFormat.CODABAR; break;
                case BarcodeFormat.CODE39:
                case BarcodeFormat.CODE39_LINEARREADER:
                case BarcodeFormat.CODE39_EX:
                case BarcodeFormat.CODE39_NOISE1: encodeFormat = ZXing.BarcodeFormat.CODE_39; break;
                case BarcodeFormat.CODE93: encodeFormat = ZXing.BarcodeFormat.CODE_93; break;
                case BarcodeFormat.CODE128: encodeFormat = ZXing.BarcodeFormat.CODE_128; break;
                case BarcodeFormat.DM: encodeFormat = ZXing.BarcodeFormat.DATA_MATRIX; break;
                case BarcodeFormat.EAN8: encodeFormat = ZXing.BarcodeFormat.EAN_8; break;
                case BarcodeFormat.EAN13: encodeFormat = ZXing.BarcodeFormat.EAN_13; break;
                case BarcodeFormat.I2OF5: encodeFormat = ZXing.BarcodeFormat.ITF; break;
                case BarcodeFormat.MAXICODE: encodeFormat = ZXing.BarcodeFormat.MAXICODE; break;
                case BarcodeFormat.PDF417: encodeFormat = ZXing.BarcodeFormat.PDF_417; break;
                case BarcodeFormat.QR: encodeFormat = ZXing.BarcodeFormat.QR_CODE; break;
                case BarcodeFormat.UPC_A: encodeFormat = ZXing.BarcodeFormat.UPC_A; break;
                case BarcodeFormat.UPC_E: encodeFormat = ZXing.BarcodeFormat.UPC_E; break;
                case BarcodeFormat.EAN2:
                case BarcodeFormat.EAN5: encodeFormat = ZXing.BarcodeFormat.UPC_EAN_EXTENSION; break;
                case BarcodeFormat.MSI: encodeFormat = ZXing.BarcodeFormat.MSI; break;
                default:
                    throw new ArgumentException("Unsupported barcode format for encoding: " + barcodeFormat);
            }

            var barcodeWriter = new ZXing.SkiaSharp.BarcodeWriter
            {
                Format = encodeFormat
            };
            barcodeWriter.Options.Hints[EncodeHintType.WIDTH] = width;
            barcodeWriter.Options.Hints[EncodeHintType.HEIGHT] = height;
            barcodeWriter.Options.Hints[EncodeHintType.MARGIN] = margin;
            barcodeWriter.Options.Hints[EncodeHintType.NO_PADDING] = true;
            barcodeWriter.Options.Hints[EncodeHintType.PURE_BARCODE] = pureBarcode;
            if (encodeFormat == ZXing.BarcodeFormat.DATA_MATRIX)
            {
                if (width == height)
                {
                    barcodeWriter.Options.Hints[EncodeHintType.DATA_MATRIX_SHAPE] = SymbolShapeHint.FORCE_SQUARE;
                }
            }
            
            if (characterSet != null)
            {
                barcodeWriter.Options.Hints[EncodeHintType.CHARACTER_SET] = characterSet;
            }
            if (disableEci)
            {
                barcodeWriter.Options.Hints[EncodeHintType.DISABLE_ECI] = true;
            }

            return barcodeWriter.Write(contents);
        }
    }
}
