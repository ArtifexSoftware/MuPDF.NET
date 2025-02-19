using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;
using ZXing.Common;

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
    /// Represents the collection of all images files/URLs to decode.
    /// </summary>
    internal sealed class Inputs
    {
        private readonly List<String> inputs = new List<String>(10);
        private int position;

        public void addInput(String pathOrUrl)
        {
            lock (inputs)
            {
                inputs.Add(pathOrUrl);
            }
        }

        public String getNextInput()
        {
            lock (inputs)
            {
                if (position < inputs.Count)
                {
                    String result = inputs[position];
                    position++;
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

        public int getInputCount()
        {
            return inputs.Count;
        }
    }

    /// <summary>
    /// One of a pool of threads which pulls images off the Inputs queue and decodes them in parallel.
    /// @see CommandLineRunner
    /// </summary>
    internal sealed class DecodeThread
    {
        private int successful;
        private readonly Config config;
        private readonly Inputs inputs;
        public List<Result> Results { get; private set; }

        public DecodeThread(Config config, Inputs inputs)
        {
            this.config = config;
            this.inputs = inputs;
        }

        public void run()
        {
            Results = new List<Result>();
            while (true)
            {
                String input = inputs.getNextInput();
                if (input == null)
                {
                    break;
                }

                if (File.Exists(input))
                {
                    try
                    {
                        if (config.Multi)
                        {
                            Result[] results = decodeMulti(new Uri(Path.GetFullPath(input)), input, config.Hints);
                            if (results != null)
                            {
                                successful++;
                                foreach (Result result in results)
                                {
                                    Results.Add(result);
                                }
                            }
                        }
                        else
                        {
                            Result result = decode(new Uri(Path.GetFullPath(input)), input, config.Hints);
                            if (result != null)
                            {
                                successful++;
                                Results.Add(result);
                            }
                        }
                    }
                    catch (IOException exc)
                    {
                        Console.WriteLine(exc.ToString());
                    }
                }
                else
                {
                    try
                    {
                        var tempFile = Path.GetTempFileName();
                        var uri = new Uri(input);
                        WebClient client = new WebClient();
                        client.DownloadFile(uri, tempFile);
                        try
                        {
                            Result result = decode(new Uri(tempFile), input, config.Hints);
                            if (result != null)
                            {
                                successful++;
                                Results.Add(result);
                            }
                        }
                        finally
                        {
                            File.Delete(tempFile);
                        }
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine(exc.ToString());
                    }
                }
            }
        }

        public int getSuccessful()
        {
            return successful;
        }

        public List<Result> getResults()
        {
            return Results;
        }

        private Result decode(Uri uri, string originalInput, IDictionary<DecodeHintType, object> hints)
        {
            SKBitmap image = null;
            try
            {
                image = SKBitmap.Decode(uri.LocalPath);
            }
            catch (Exception e)
            {
                throw new FileNotFoundException("Resource not found: " + uri + "(" + e.Message + ")");
            }

            if (image == null)
                return null;

            using (image)
            {
                return decode(uri, image, originalInput, hints);
            }
        }

        private Result decode(Uri uri, SKBitmap image, string originalInput, IDictionary<DecodeHintType, object> hints)
        {
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
            foreach (var entry in hints)
                reader.Options.Hints.Add(entry.Key, entry.Value);
            Result result = reader.Decode(source);

            return result;
        }

        private Result[] decodeMulti(Uri uri, string originalInput, IDictionary<DecodeHintType, object> hints)
        {
            SKBitmap image;
            try
            {
                image = SKBitmap.Decode(uri.LocalPath);
            }
            catch (Exception e)
            {
                throw new FileNotFoundException("Resource not found: " + uri + "(" + e.Message + ")");
            }

            using (image)
            {
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
                foreach (var entry in hints)
                    reader.Options.Hints.Add(entry.Key, entry.Value);
                Result[] results = reader.DecodeMultiple(source);

                return results;
            }
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
