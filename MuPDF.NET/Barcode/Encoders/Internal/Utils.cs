/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using BitMiracle.LibTiff.Classic;
using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace BarcodeWriter.Core.Internal
{
    class Utils
    {
        private Utils()
        {
        }

        public static SKCanvas GetScreenCompatibleGraphics(out SKBitmap dummy)
        {
            // Create a small dummy bitmap (10x10) with 32-bit color
            dummy = new SKBitmap(10, 10, SKColorType.Bgra8888, SKAlphaType.Premul);

            // Create a canvas for drawing on the bitmap
            var canvas = new SKCanvas(dummy);

            // Clear with transparent or white if needed
            canvas.Clear(SKColors.Transparent);

            return canvas;
        }

        /// <summary>
        /// Calculates the draw position.
        /// </summary>
        /// <param name="barcodeSize">Size of the barcode.</param>
        /// <param name="totalWidth">The total width.</param>
        /// <param name="totalHeight">The total height.</param>
        /// <param name="left">The leftmost draw position.</param>
        /// <param name="top">The topmost draw position.</param>
        /// <param name="hAlign">The horizontal alignment.</param>
        /// <param name="vAlign">The vertical alignment.</param>
        public static void CalculateDrawPosition(Size barcodeSize,
            int totalWidth, int totalHeight, out int left, out int top,
            BarcodeHorizontalAlignment hAlign, BarcodeVerticalAlignment vAlign)
        {
            left = 0;
            top = 0;

            if (barcodeSize.Width < totalWidth)
            {
                switch (hAlign)
                {
                    case BarcodeHorizontalAlignment.Right:
                        left = totalWidth - barcodeSize.Width;
                        break;

                    case BarcodeHorizontalAlignment.Center:
                        left = (totalWidth - barcodeSize.Width) / 2;
                        break;
                }
            }

            if (barcodeSize.Height < totalHeight)
            {
                switch (vAlign)
                {
                    case BarcodeVerticalAlignment.Bottom:
                        top = totalHeight - barcodeSize.Height;
                        break;

                    case BarcodeVerticalAlignment.Middle:
                        top = (totalHeight - barcodeSize.Height) / 2;
                        break;
                }
            }
        }

        public static int CalculateCaptionGap(SKFont captionFont)
        {
            SKFontMetrics metrics = captionFont.Metrics;
            float height = metrics.Descent - metrics.Ascent + metrics.Leading;
            return (int)System.Math.Ceiling(height);
        }

        public static SKBitmap ConvertToBitonal(SKBitmap original)
        {
            int width = original.Width;
            int height = original.Height;

            // Destination bitmap: 8-bit grayscale
            var destination = new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Opaque);

            // Manual thresholding (same as original GDI+ approach)
            int threshold = 128; // ~500/3, like your original code

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SKColor color = original.GetPixel(x, y);
                    int brightness = color.Red + color.Green + color.Blue;

                    byte bw = (brightness > 384) // (? 128 * 3 = 384, matches old threshold=500 logic)
                        ? (byte)255
                        : (byte)0;

                    destination.SetPixel(x, y, new SKColor(bw, bw, bw));
                }
            }

            return destination;
        }

        public static SKBitmap ConvertToBitonal_(SKBitmap original)
        {
            int width = original.Width;
            int height = original.Height;

            // Calculate bytes per row (1-bit per pixel, padded to 8)
            int stride = (width + 7) / 8;
            byte[] bitonalData = new byte[stride * height];

            int threshold = 500; // same as your GDI+ code

            for (int y = 0; y < height; y++)
            {
                int destIndex = y * stride;
                byte destByte = 0;
                int bitMask = 0x80; // start with MSB

                for (int x = 0; x < width; x++)
                {
                    SKColor pixel = original.GetPixel(x, y);
                    int pixelTotal = pixel.Red + pixel.Green + pixel.Blue;

                    if (pixelTotal > threshold)
                        destByte |= (byte)bitMask;

                    if (bitMask == 1)
                    {
                        bitonalData[destIndex++] = destByte;
                        destByte = 0;
                        bitMask = 0x80;
                    }
                    else
                    {
                        bitMask >>= 1;
                    }
                }

                if (bitMask != 0x80) // write last byte if row not multiple of 8
                    bitonalData[destIndex] = destByte;
            }

            // Create a 1-bit SKBitmap for output
            SKImageInfo info = new SKImageInfo(width, height, SKColorType.Alpha8, SKAlphaType.Opaque);
            SKBitmap bitonal = new SKBitmap(info);
            IntPtr dstPixels = bitonal.GetPixels();

            // Copy the bitonalData to SKBitmap memory
            unsafe
            {
                fixed (byte* src = bitonalData)
                {
                    Buffer.MemoryCopy(src, (void*)dstPixels, bitonalData.Length, bitonalData.Length);
                }
            }

            return bitonal;
        }

        public static void SaveAsBitonalTiff(SKBitmap copy, Stream outputStream)
        {
            try
            {
                // Convert to 1bpp (threshold method)
                using (var bitonal = new SKBitmap(copy.Width, copy.Height, SKColorType.Gray8, SKAlphaType.Opaque))
                {
                    for (int y = 0; y < copy.Height; y++)
                    {
                        for (int x = 0; x < copy.Width; x++)
                        {
                            var color = copy.GetPixel(x, y);
                            byte gray = (byte)(0.3 * color.Red + 0.59 * color.Green + 0.11 * color.Blue);
                            byte bw = gray > 127 ? (byte)255 : (byte)0; // Threshold
                            bitonal.SetPixel(x, y, new SKColor(bw, bw, bw));
                        }
                    }

                    using (var image = SKImage.FromBitmap(bitonal))
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                        data.SaveTo(outputStream);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Can't save to 1-bit image.", ex);
            }
        }
        /*
        public static void SaveAsBitonalTiff(SKBitmap bitmap, MemoryStream ms)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            // Convert SKBitmap to 1-bit bitonal byte array (scanline by scanline)
            byte[] bitonalData = new byte[(width + 7) / 8 * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SKColor pixel = bitmap.GetPixel(x, y);
                    // simple threshold: black if intensity < 128
                    bool isBlack = (pixel.Red + pixel.Green + pixel.Blue) / 3 < 128;
                    int byteIndex = y * ((width + 7) / 8) + (x / 8);
                    int bitIndex = 7 - (x % 8);
                    if (isBlack)
                        bitonalData[byteIndex] |= (byte)(1 << bitIndex);
                }
            }

            // Save using LibTiff.NET
            using (Tiff tiff = Tiff.ClientOpen("in-memory", "w", ms, new TiffStream()))
            {
                if (tiff == null)
                    throw new Exception("Could not create TIFF");

                tiff.SetField(TiffTag.IMAGEWIDTH, width);
                tiff.SetField(TiffTag.IMAGELENGTH, height);
                tiff.SetField(TiffTag.BITSPERSAMPLE, 1);
                tiff.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                tiff.SetField(TiffTag.ROWSPERSTRIP, height);
                tiff.SetField(TiffTag.COMPRESSION, Compression.CCITTFAX4);
                tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISWHITE);

                tiff.WriteEncodedStrip(0, bitonalData, bitonalData.Length);
                tiff.Close();
            }
        }
        */
	    /// <summary>
        /// Detects which format to use for image saved into a file with given name.
        /// </summary>
        /// <param name="fileName">Name of the file to save image to.</param>
        /// <returns>The format to use for image.</returns>
        public static SKEncodedImageFormat FormatFromName(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            switch (extension)
            {
                case ".bmp":
                    return SKEncodedImageFormat.Bmp;

                case ".gif":   // single-frame only
                    return SKEncodedImageFormat.Gif;

                case ".jpg":
                case ".jpeg":
                    return SKEncodedImageFormat.Jpeg;

                case ".png":
                    return SKEncodedImageFormat.Png;

                case ".tif":
                case ".tiff":
                    return SKEncodedImageFormat.Png;

                case ".wmf":
                case ".emf":
                    throw new NotSupportedException("WMF/EMF is not supported in SkiaSharp.");

                default:
                    return SKEncodedImageFormat.Bmp;
            }
        }

        public static SizeF GetSizeInUnits(float dpiX, float dpiY, Size size, UnitOfMeasure unit)
        {
            float width = (float)size.Width;
            float height = (float)size.Height;

            switch (unit)
            {
                case UnitOfMeasure.Document:
                    width = 300.0f * (float)size.Width / dpiX;
                    height = 300.0f * (float)size.Height / dpiY;
                    break;

                case UnitOfMeasure.Inch:
                    width = (float)size.Width / dpiX;
                    height = (float)size.Height / dpiY;
                    break;

                case UnitOfMeasure.Point:
                    width = 72.0f * (float)size.Width / dpiX;
                    height = 72.0f * (float)size.Height / dpiY;
                    break;

                case UnitOfMeasure.Millimeter:
                    width = 25.4f * (float)size.Width / dpiX;
                    height = 25.4f * (float)size.Height / dpiY;
                    break;

                case UnitOfMeasure.Centimeter:
                    width = 2.54f * (float)size.Width / dpiX;
                    height = 2.54f * (float)size.Height / dpiY;
                    break;

                case UnitOfMeasure.Twip:
                    width = 1440.0f * (float)size.Width / dpiX;
                    height = 1440.0f * (float)size.Height / dpiY;
                    break;
            }

            return new SizeF(width, height);
        }

        /// <summary>
        /// Gets the size in pixels.
        /// </summary>
        /// <param name="g">The Graphics object (used to retrieve device properties).</param>
        /// <param name="size">The size in units.</param>
        /// <param name="unit">The unit of the size.</param>
        /// <returns></returns>
        public static Size GetSizeInPixels(SKCanvas canvas, SizeF size, UnitOfMeasure unit)
        {
            return GetSizeInPixels(96.0f, 96.0f, size, unit);
        }

        public static Size GetSizeInPixels(float dpiX, float dpiY, SizeF size, UnitOfMeasure unit)
        {
            int width = (int)size.Width;
            int height = (int)size.Height;

            switch (unit)
            {
                case UnitOfMeasure.Document:
                    width = (int)(size.Width * dpiX / 300);
                    height = (int)(size.Height * dpiY / 300);
                    break;

                case UnitOfMeasure.Inch:
                    width = (int)(size.Width * dpiX);
                    height = (int)(size.Height * dpiY);
                    break;

                case UnitOfMeasure.Point:
                    width = (int)(size.Width * dpiX / 72);
                    height = (int)(size.Height * dpiY / 72);
                    break;

                case UnitOfMeasure.Millimeter:
                    width = (int)(size.Width * dpiX / 25.4);
                    height = (int)(size.Height * dpiY / 25.4);
                    break;

                case UnitOfMeasure.Centimeter:
                    width = (int)(size.Width * dpiX / 2.54);
                    height = (int)(size.Height * dpiY / 2.54);
                    break;

                case UnitOfMeasure.Twip:
                    width = (int)(size.Width * dpiX / 1440);
                    height = (int)(size.Height * dpiY / 1440);
                    break;
            }

            return new Size(width, height);
        }
		
		public static int UnitsToPixels(float resolution, float value, UnitOfMeasure unit)
        {
            int result = 0;

            switch (unit)
            {
                case UnitOfMeasure.Document:
                    result = (int) (value * resolution / 300);
                    break;

                case UnitOfMeasure.Inch:
					result = (int) (value * resolution);
                    break;

                case UnitOfMeasure.Point:
					result = (int) (value * resolution / 72);
                    break;

                case UnitOfMeasure.Millimeter:
					result = (int) (value * resolution / 25.4);
                    break;

                case UnitOfMeasure.Centimeter:
					result = (int) (value * resolution / 2.54);
                    break;

                case UnitOfMeasure.Twip:
					result = (int) (value * resolution / 1440);
                    break;

                case UnitOfMeasure.Pixel:
                    result = (int) value;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Rotates the graphics using specified drawing angle.
        /// </summary>
        /// <param name="g">The Graphics object.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="width">The width of drawing.</param>
        /// <param name="height">The height of drawing.</param>
        public static void RotateGraphics(SKCanvas canvas, RotationAngle angle, int width, int height)
        {
            switch (angle)
            {
                case RotationAngle.Degrees90:
                    canvas.Translate(width, 0);
                    canvas.RotateDegrees(90);
                    break;

                case RotationAngle.Degrees180:
                    canvas.Translate(width, height);
                    canvas.RotateDegrees(180);
                    break;

                case RotationAngle.Degrees270:
                    canvas.Translate(0, height);
                    canvas.RotateDegrees(270);
                    break;
            }
        }

        public static SKBitmap CreateImage(SKSizeI imageSize, float dpiX, float dpiY)
        {
            // Create a 32-bit RGBA bitmap
            var info = new SKImageInfo(imageSize.Width, imageSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            var bitmap = new SKBitmap(info);

            // Note: SKBitmap does not store DPI directly; you may need to keep dpiX/dpiY separately
            // if needed for export or rendering calculations.

            return bitmap;
        }

        public static bool Is2DSymbology(SymbologyType symbology)
        {
            switch (symbology)
            {
                case SymbologyType.PDF417:
                case SymbologyType.PDF417Truncated:
                case SymbologyType.DataMatrix:
                case SymbologyType.QRCode:
                case SymbologyType.Aztec:
                case SymbologyType.MacroPDF417:
                case SymbologyType.MicroPDF417:
                case SymbologyType.GS1_DataMatrix:
                case SymbologyType.MaxiCode:
                    return true;
                default:
                   return false;
            }
        }
    }
}
