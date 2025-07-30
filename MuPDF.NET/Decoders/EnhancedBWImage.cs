using System;
using System.Drawing;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace BarcodeReader.Core
{
    internal class EnhancedBWImage : IPreparedImage
	{
		private byte[][] _samples;

		public int nBilateralFilter = 1;// This can be increased to 2 for better edge, but will add to computation

		public int[][] sharpFilter = { new int[] { -1, -1, -1 }, new int[] { -1, 12, -1 }, new int[] { -1, -1, -1 } };
		public int filterWeight = 4;
		public int EDGE_FILTER_THRESOLD = 32;// default 32. higher value results in smoothing of image

		public SKBitmap SKBitmap { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }

		private EnhancedBWImage()
		{
		}

		public EnhancedBWImage(SKBitmap sourceBitmap)
		{
			_samples = Process(sourceBitmap);
		}

		public IPreparedImage Clone()
		{
			EnhancedBWImage copy = new EnhancedBWImage();

			if (_samples == null)
				copy._samples = null;
			else
			{
				copy._samples = new byte[_samples.Length][];
				for (var i = 0; i < _samples.Length; i++)
				{
					if (_samples[i] != null)
					{
						copy._samples[i] = new byte[_samples[i].Length];
						Array.Copy(_samples[i], copy._samples[i], _samples[i].Length);
					}
				}
			}

			copy.nBilateralFilter = nBilateralFilter;
			copy.filterWeight = filterWeight;
			copy.EDGE_FILTER_THRESOLD = EDGE_FILTER_THRESOLD;
			copy.SKBitmap = SKBitmap == null ? null : (SKBitmap) SKBitmap.Copy();
			copy.Width = Width;
			copy.Height = Height;

			return copy;
		}

		public void Dispose()
		{
		}

		public byte[] GetRow(int y)
		{
			if (y < 0 || y >= Height)
				throw new ArgumentException();

			return _samples[y];
		}

        public byte[][] Process(SKBitmap bitmap)
        {
            Width = bitmap.Width;
            Height = bitmap.Height;

            // Clone input
            SKBitmap tmpBitmap = bitmap.Copy();

            // Convert SKBitmap to byte[] in RGB format
            int bytesPerPixel = 3;
            int nChannels = bytesPerPixel;
            int stride = Width * bytesPerPixel;
            byte[] imageBytes = new byte[Height * stride];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    SKColor color = tmpBitmap.GetPixel(x, y);
                    int index = y * stride + x * bytesPerPixel;
                    imageBytes[index] = color.Red;
                    imageBytes[index + 1] = color.Green;
                    imageBytes[index + 2] = color.Blue;
                }
            }

            // Apply image processing steps
            imageBytes = Sharpen(imageBytes, Height, Width, nChannels, bytesPerPixel, stride);

            for (int f = 0; f < nBilateralFilter; f++)
                imageBytes = EdgePreservedSmoothing(imageBytes, Height, Width, nChannels, bytesPerPixel, stride);

            int thresholdValue = OtsuThreshold.GetOtsuThreshold(imageBytes, Height, Width, stride);
            imageBytes = threshold(imageBytes, Height, Width, nChannels, bytesPerPixel, stride, thresholdValue);

            // Write back to SKBitmap
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = y * stride + x * bytesPerPixel;
                    byte r = imageBytes[index];
                    byte g = imageBytes[index + 1];
                    byte b = imageBytes[index + 2];

                    tmpBitmap.SetPixel(x, y, new SKColor(r, g, b));
                }
            }

#if DEBUG_IMAGE
			using var image = SKImage.FromBitmap(tmpBitmap);
			using var data = image.Encode(SKEncodedImageFormat.Png, 100);
			using var stream = File.OpenWrite("EnhancedBWImage.png");
			data.SaveTo(stream);
#endif

            return ConstructSamples(imageBytes, stride);
        }

		private byte[][] ConstructSamples(byte[] bytes, int stride)
		{
			byte[][] samples = new byte[Height][];

			for (int y = 0; y < Height; y++)
			{
				samples[y] = new byte[Width];

				for (int x = 0, srcX = y * stride; x < Width; x++)
				{
					samples[y][x] = bytes[srcX];
					srcX += 3;
				}
			}

			return samples;
		}

		// Sharpens the input image using sharp filter
		public byte[] Sharpen(byte[] inData, int height, int width, int nChannels, int bytesPerPixel, int stride)
		{
			int heightUpper = height - 1;
			int widthUpper = width - 1;

			for (int i = 1; i < heightUpper; i++)
			{
				for (int j = 1; j < widthUpper; j++)
				{
					int sum = 0;
					for (int k = -1; k < 2; k++)
					{
						for (int l = -1; l < 2; l++)
						{
							//compute weighted sum
							sum += inData[(i + k) * stride + (j + l) * bytesPerPixel] * sharpFilter[k + 1][l + 1];
						}
					}
					if (sum < 0) sum = 0;

					for (int b = 0; b < nChannels; b++)
					{
						inData[i * stride + j * bytesPerPixel + b] = (byte) (sum / filterWeight);
					}
				}
			}

			return inData;
		}

		// This smoothes / blurs image while preserving edges
		// a faster alternative to expensive Bilateral filter of OpenCV
		public byte[] EdgePreservedSmoothing(byte[] inData, int height, int width, int nChannels, int bytesPerPixel_InputImage, int stride)
		{
			int[] diff = new int[4];

			for (int i = 1; i < height - 1; i++)
			{
				for (int j = 1; j < width - 1; j++)
				{
					int x = inData[i * stride + j * bytesPerPixel_InputImage];

					int l0 = inData[i * stride + (j - 1) * bytesPerPixel_InputImage];
					byte l1 = inData[i * stride + (j + 1) * bytesPerPixel_InputImage];
					diff[0] = Math.Abs(l1 - x) + Math.Abs(l0 - x);

					byte t0 = inData[(i - 1) * stride + j * bytesPerPixel_InputImage];
					byte t1 = inData[(i + 1) * stride + j * bytesPerPixel_InputImage];
					diff[1] = Math.Abs(t1 - x) + Math.Abs(t0 - x);

					byte d0 = inData[(i - 1) * stride + (j - 1) * bytesPerPixel_InputImage];
					byte d1 = inData[(i + 1) * stride + (j + 1) * bytesPerPixel_InputImage];
					diff[2] = Math.Abs(t1 - x) + Math.Abs(t0 - x);

					byte e0 = inData[(i - 1) * stride + (j + 1) * bytesPerPixel_InputImage];
					byte e1 = inData[(i + 1) * stride + (j - 1) * bytesPerPixel_InputImage];
					diff[3] = Math.Abs(t1 - x) + Math.Abs(t0 - x);

					int min = diff[0];
					int minIndex = 0;

					for (int k = 1; k < 4; ++k)
					{
						if (diff[k] < min)
						{
							min = diff[k];
							minIndex = k;
						}
					}

					if (diff[minIndex] > EDGE_FILTER_THRESOLD)
						continue;

					switch (minIndex)
					{
						case 0:
							x = (l1 + l0 + x) / 3;
							break;

						case 1:
							x = (t1 + t0 + x) / 3;
							break;

						case 2:
							x = (d1 + d0 + x) / 3;
							break;

						case 3:
							x = (e1 + e0 + x) / 3;
							break;
					}

					for (int b = 0; b < nChannels; b++)
					{
						inData[i * stride + j * bytesPerPixel_InputImage + b] = (byte) (x);
					}
				}
			}

			return inData;
		}

		// Threshold the image to binary using given threshold value
		public byte[] threshold(byte[] inData, int height, int width, int nChannels, int bytesPerPixel, int stride, int thr)
		{
			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					int val = 0;

					if (inData[i * stride + j * bytesPerPixel] > thr)
					{
						val = 255;
					}

					for (int b = 0; b < nChannels; b++)
					{
						inData[i * stride + j * bytesPerPixel + b] = (byte) (val);
					}
				}
			}

			return inData;
		}
	}
}
