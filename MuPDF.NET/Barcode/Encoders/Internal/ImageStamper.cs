#if !PocketPC && !WindowsCE && !TARGETTING_FX_1_1

using BitMiracle.LibTiff.Classic;
using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace BarcodeWriter.Core.Internal
{
	internal class ImageStamper : IDisposable
	{
		public void Dispose()
		{
		}

        public void DrawToImage(Stream inputStream, SKBitmap barcodeImage, int pageIndex, int x, int y, Stream outputStream)
        {
            using (var managedStream = new SKManagedStream(inputStream))
            using (var codec = SKCodec.Create(managedStream))
            {
                if (codec == null)
                    throw new Exception("Cannot read input image.");

                SKImageInfo info = codec.Info;

                // Create a bitmap for drawing
                using (var bitmap = new SKBitmap(info.Width, info.Height, info.ColorType, info.AlphaType))
                {
                    SKCodecResult result = codec.GetPixels(bitmap.Info, bitmap.GetPixels());

                    if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
                        throw new Exception("Failed to decode input image.");

                    using (var canvas = new SKCanvas(bitmap))
                    {
                        // Draw the barcode bitmap
                        canvas.DrawBitmap(barcodeImage, x, y);
                    }

                    // Detect format by file extension (fallback to PNG)
                    SKEncodedImageFormat skFormat = SKEncodedImageFormat.Png;
                    if (outputStream is FileStream fs)
                    {
                        string ext = Path.GetExtension(fs.Name).ToLowerInvariant();
                        switch (ext)
                        {
                            case ".bmp":
                                skFormat = SKEncodedImageFormat.Bmp;
                                break;

                            case ".gif":
                                skFormat = SKEncodedImageFormat.Gif;
                                break;

                            case ".jpg":
                            case ".jpeg":
                                skFormat = SKEncodedImageFormat.Jpeg;
                                break;

                            case ".png":
                                skFormat = SKEncodedImageFormat.Png;
                                break;

                            case ".webp":
                                skFormat = SKEncodedImageFormat.Webp;
                                break;

                            default:
                                skFormat = SKEncodedImageFormat.Png;
                                break;
                        }
                    }

                    using (var image = SKImage.FromBitmap(bitmap))
                    using (var data = image.Encode(skFormat, 100))
                        data.SaveTo(outputStream);
                }
            }
        }
        private void ConvertToBitonal(ref SKBitmap srcBitmap)
        {
            int width = srcBitmap.Width;
            int height = srcBitmap.Height;

            // Create destination bitmap in 1-bit format (simulate with ALPHA_8 + manual threshold)
            var dstBitmap = new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Opaque);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SKColor pixel = srcBitmap.GetPixel(x, y);

                    // Compute brightness (simple luminance)
                    int brightness = pixel.Red + pixel.Green + pixel.Blue;

                    // Threshold for 1-bit conversion
                    byte value = brightness < 500 ? (byte)0 : (byte)255;

                    dstBitmap.SetPixel(x, y, new SKColor(value, value, value));
                }
            }

            srcBitmap.Dispose();
            srcBitmap = dstBitmap;
        }

        private static byte[] ExtractBytes(SKBitmap bitmap, out int rowBytes)
        {
            // Ensure bitmap is in 32-bit color (RGBA)
            if (bitmap.ColorType != SKColorType.Rgba8888)
                throw new ArgumentException("Bitmap must be in RGBA8888 format.");

            using (var pixmap = bitmap.PeekPixels())
            {
                rowBytes = pixmap.RowBytes;
                int totalBytes = rowBytes * pixmap.Height;
                byte[] buffer = new byte[totalBytes];

                unsafe
                {
                    fixed (byte* dest = buffer)
                    {
                        System.Buffer.MemoryCopy(
                            (void*)pixmap.GetPixels(),   // source pointer (IntPtr)
                            dest,                 // destination pointer
                            totalBytes,           // destination size in bytes
                            totalBytes            // copy size
                        );
                    }
                }

                return buffer;
            }
        }

        internal static SKEncodedImageFormat GetSkiaFormat(string mimeType)
        {
            switch (mimeType.ToLowerInvariant())
            {
                case "image/bmp":
                    return SKEncodedImageFormat.Bmp;
                case "image/gif":
                    return SKEncodedImageFormat.Gif;
                case "image/jpeg":
                case "image/jpg":
                    return SKEncodedImageFormat.Jpeg;
                case "image/png":
                    return SKEncodedImageFormat.Png;
                case "image/webp":
                    return SKEncodedImageFormat.Webp;
                default:
                    throw new NotSupportedException($"MIME type '{mimeType}' is not supported in SkiaSharp.");
            }
        }

        public SKBitmap GetFrame(Stream imageStream, int index)
        {
            using (var codec = SKCodec.Create(imageStream))
            {
                if (codec == null)
                    throw new Exception("Cannot create codec for image");

                if (index < 0 || index >= codec.FrameCount)
                    throw new ArgumentOutOfRangeException(nameof(index));

                SKBitmap bitmap = new SKBitmap(codec.Info.Width, codec.Info.Height);
                SKCodecOptions options = new SKCodecOptions(index);
                codec.GetPixels(bitmap.Info, bitmap.GetPixels(), options);

                return bitmap;
            }
        }

        private static byte[] GetImageRasterBytes(SKBitmap bitmap)
        {
            if (bitmap == null)
                return null;

            // Create a buffer to hold pixel data
            int rowBytes = bitmap.RowBytes;
            int height = bitmap.Height;
            byte[] buffer = new byte[rowBytes * height];

            // Copy pixels into the buffer
            using (var pixmap = bitmap.PeekPixels())
            {
                if (pixmap == null)
                    return null;

                IntPtr pixelsPtr = pixmap.GetPixels();
                if (pixelsPtr == IntPtr.Zero)
                    return null;

                System.Runtime.InteropServices.Marshal.Copy(pixelsPtr, buffer, 0, buffer.Length);
            }

            return buffer;
        }

        // Converts BGRA samples into RGBA samples
        private static void ConvertSamples(byte[] data, int width, int height)
		{
			int stride = data.Length / height;
			const int samplesPerPixel = 3;

			for (int y = 0; y < height; y++)
			{
				int offset = stride * y;
				int strideEnd = offset + width * samplesPerPixel;

				for (int i = offset; i < strideEnd; i += samplesPerPixel)
				{
					byte temp = data[i + 2];
					data[i + 2] = data[i];
					data[i] = temp;
				}
			}
		}

		private static void RemovePadding(byte[] raster, int width, int height)
		{
			int stride = raster.Length / height;
			int rowLength = width * 3;

			for (int i = 1, readOffset = stride, writeOffset = rowLength; i < height; i++)
			{
				Buffer.BlockCopy(raster, readOffset, raster, writeOffset, rowLength);
				readOffset += stride;
				writeOffset += rowLength;
			}
		}
	}
}

#endif // !PocketPC && !WindowsCE && !TARGETTING_FX_1_1
