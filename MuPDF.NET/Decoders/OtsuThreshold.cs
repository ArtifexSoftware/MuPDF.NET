using SkiaSharp;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace BarcodeReader.Core
{
    internal class OtsuThreshold
    {
		private static float Px(int init, int end, int[] hist)
		{
			int sum = 0;
			int i;
			for (i = init; i <= end; i++)
				sum += hist[i];

			return (float) sum;
		}

		private static float Mx(int init, int end, int[] hist)
		{
			int sum = 0;
			int i;
			for (i = init; i <= end; i++)
				sum += i * hist[i];

			return (float) sum;
		}

		private static int FindMax(float[] vec, int n)
        {
            float maxVec = 0;
            int idx=0;
            int i;

            for (i = 1; i < n - 1; i++)
            {
                if (vec[i] > maxVec)
                {
                    maxVec = vec[i];
                    idx = i;
                }
            }
            return idx;
        }

		private static void GetHistogram(byte[] data, int width, int height, int stride, int[] hist)
		{
			hist.Initialize();

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width * 3; x += 3)
				{
					int i = y * stride + x;
					hist[data[i]]++;
				}
			}
		}
		/*
		public int GetOtsuThreshold(SKBitmap bmp)
		{
			byte t = 0;
			float[] vet = new float[256];
			int[] hist = new int[256];
			vet.Initialize();

			float p1, p2, p12;
			int k;

			BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
			int size = bitmapData.Stride * bitmapData.Height;
			byte[] data = new byte[size];
			Marshal.Copy(bitmapData.Scan0, data, 0, size);

			GetHistogram(data, bitmapData.Width, bitmapData.Height, bitmapData.Stride, hist);

			// loop through all possible t values and maximize between class variance
			for (k = 1; k != 255; k++)
			{
				p1 = Px(0, k, hist);
				p2 = Px(k + 1, 255, hist);
				p12 = p1 * p2;
				if (p12 == 0)
					p12 = 1;
				float diff = (Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1);
				vet[k] = (float) diff * diff / p12;
			}

			bmp.UnlockBits(bitmapData);

			t = (byte) FindMax(vet, 256);

			return t;
		}
		*/
        public int GetOtsuThreshold(SKBitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            int[] hist = new int[256];
            float[] vet = new float[256];

            // Step 1: Calculate grayscale histogram
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SKColor color = bmp.GetPixel(x, y);

                    // Convert to grayscale using luminance formula (ITU-R BT.601)
                    int gray = (int)(color.Red * 0.299 + color.Green * 0.587 + color.Blue * 0.114);
                    gray = Common.Utils.Clamp(gray, 0, 255);
                    hist[gray]++;
                }
            }

            // Step 2: Otsu's method
            byte t = 0;
            float p1, p2, p12;
            for (int k = 1; k < 255; k++)
            {
                p1 = Px(0, k, hist);
                p2 = Px(k + 1, 255, hist);
                p12 = p1 * p2;
                if (p12 == 0)
                    p12 = 1;
                float diff = (Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1);
                vet[k] = diff * diff / p12;
            }

            t = (byte)FindMax(vet, 256);
            return t;
        }

        // Threshold the image to binary using given threshold value
        public static int GetOtsuThreshold(byte[] data, int height, int width, int stride)
		{
			byte t = 0;
			float[] vet = new float[256];
			int[] hist = new int[256];
			vet.Initialize();

			float p1, p2, p12;
			int k;

			GetHistogram(data, width, height, stride, hist);
			
			// loop through all possible t values and maximize between class variance
			for (k = 1; k != 255; k++)
			{
				p1 = Px(0, k, hist);
				p2 = Px(k + 1, 255, hist);
				p12 = p1 * p2;
				if (p12 == 0)
					p12 = 1;
				float diff = (Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1);
				vet[k] = (float) diff * diff / p12;
			}

			t = (byte) FindMax(vet, 256);

			return t;

		}
        /*
		public void Convert2GrayScaleFast(SKBitmap bmp)
        {
			int width = bmp.Width;
			int height = bmp.Height;

			BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
			int size = bitmapData.Stride * bitmapData.Height;
			byte[] data = new byte[size];
	        
			Marshal.Copy(bitmapData.Scan0, data, 0, size);

			for (int y = 0; y < height; y++)
	        {
		        for (int x = 0; x < width; x++)
		        {
					int i = bitmapData.Stride * y + x * 3;
					data[i] = (byte) (.299 * data[i + 2] + .587 * data[i + 1] + .114 * data[i]);
			        data[i + 1] = data[i];
			        data[i + 2] = data[i];
		        }
	        }

			Marshal.Copy(data, 0, bitmapData.Scan0, size);
			bmp.UnlockBits(bitmapData);
        }
		*/
        public void Convert2GrayScaleFast(SKBitmap bmp)
        {
            using (var pixmap = bmp.PeekPixels())
            {
                int width = bmp.Width;
                int height = bmp.Height;
                int rowBytes = pixmap.RowBytes;

                IntPtr pixels = pixmap.GetPixels();

                unsafe
                {
                    byte* ptr = (byte*)pixels.ToPointer();

                    for (int y = 0; y < height; y++)
                    {
                        byte* row = ptr + y * rowBytes;

                        for (int x = 0; x < width; x++)
                        {
                            byte* px = row + x * 4; // 4 bytes per pixel: B, G, R, A

                            byte b = px[0];
                            byte g = px[1];
                            byte r = px[2];

                            byte gray = (byte)(0.299 * r + 0.587 * g + 0.114 * b);

                            px[0] = gray; // Blue
                            px[1] = gray; // Green
                            px[2] = gray; // Red
                                          // Leave alpha (px[3]) unchanged
                        }
                    }
                }
            }
        }
        /*
        public void Threshold(SKBitmap bmp, int thresh)
		{
			int width = bmp.Width;
			int height = bmp.Height;

			BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
			int size = bitmapData.Stride * bitmapData.Height;
			byte[] data = new byte[size];
			
			Marshal.Copy(bitmapData.Scan0, data, 0, size);

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int i = bitmapData.Stride * y + x * 3;
					data[i] = (byte) (data[i] > thresh ? 255 : 0);
					data[i + 1] = (byte) (data[i + 1] > thresh ? 255 : 0);
					data[i + 2] = (byte) (data[i + 2] > thresh ? 255 : 0);
				}
			}

			Marshal.Copy(data, 0, bitmapData.Scan0, size);
			bmp.UnlockBits(bitmapData);
		}
		*/
        public void Threshold(SKBitmap bmp, int thresh)
        {
            int width = bmp.Width;
            int height = bmp.Height;

            using (var pixmap = bmp.PeekPixels())
            {
                int rowBytes = pixmap.RowBytes;
                IntPtr pixels = pixmap.GetPixels();

                unsafe
                {
                    byte* ptr = (byte*)pixels.ToPointer();

                    for (int y = 0; y < height; y++)
                    {
                        byte* row = ptr + y * rowBytes;

                        for (int x = 0; x < width; x++)
                        {
                            byte* px = row + x * 4; // BGRA

                            // Apply threshold on each channel
                            px[0] = (byte)(px[0] > thresh ? 255 : 0); // Blue
                            px[1] = (byte)(px[1] > thresh ? 255 : 0); // Green
                            px[2] = (byte)(px[2] > thresh ? 255 : 0); // Red
                                                                      // Alpha (px[3]) stays unchanged
                        }
                    }
                }
            }
        }
    }
}

