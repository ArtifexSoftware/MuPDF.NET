using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;
using ZXing.Common;
using static ZXing.RGBLuminanceSource;

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
            var reader = new BarcodeReader();
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

            var reader = new BarcodeReader();
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
            bool disableEci = false
            )
        {
            if (contents == null)
            {
                return null;
            }

            var barcodeWriter = new BarcodeWriter
            {
                Format = (ZXing.BarcodeFormat)barcodeFormat,
                Options = new EncodingOptions
                {
                    Height = height,
                    Width = width
                }
            };
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
