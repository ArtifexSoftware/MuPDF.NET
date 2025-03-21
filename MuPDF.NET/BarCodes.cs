using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;
using ZXing.Common;
using System.Collections.Generic;
using System;
using System.IO;
using ZXing.Datamatrix.Encoder;

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

            Result[] results = reader.DecodeMultiple(source);

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

            var barcodeWriter = new ZXing.SkiaSharp.BarcodeWriter
            {
                Format = (ZXing.BarcodeFormat)barcodeFormat
            };
            barcodeWriter.Options.Hints[EncodeHintType.WIDTH] = width;
            barcodeWriter.Options.Hints[EncodeHintType.HEIGHT] = height;
            barcodeWriter.Options.Hints[EncodeHintType.MARGIN] = margin;
            barcodeWriter.Options.Hints[EncodeHintType.NO_PADDING] = true;
            barcodeWriter.Options.Hints[EncodeHintType.PURE_BARCODE] = pureBarcode;
            if (barcodeFormat == BarcodeFormat.DATA_MATRIX)
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
