using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
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

		public int GetOtsuThreshold(SKBitmap bmp)
		{
			byte t = 0;
			float[] vet = new float[256];
			int[] hist = new int[256];
			vet.Initialize();

            // Prepare pixel data
            int width = bmp.Width;
            int height = bmp.Height;
            int stride = bmp.RowBytes; // bytes per row in SKBitmap
            int size = stride * height;
            byte[] data = new byte[size];

            // Copy SKBitmap pixel data to byte array
            IntPtr ptr = bmp.GetPixels();
            Marshal.Copy(ptr, data, 0, size);

            // Call your histogram logic
            GetHistogram(data, width, height, stride, hist);

            // Otsu thresholding logic (maximizing between-class variance)
            for (int k = 1; k < 255; k++)
            {
                float p1 = Px(0, k, hist);
                float p2 = Px(k + 1, 255, hist);
                float p12 = p1 * p2;
                if (p12 == 0)
                    p12 = 1;

                float diff = (Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1);
                vet[k] = (float)(diff * diff / p12);
            }

            t = (byte) FindMax(vet, 256);

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

		public void Convert2GrayScaleFast(SKBitmap bmp)
        {
			int width = bmp.Width;
			int height = bmp.Height;

            int bytesPerPixel = 4; // Use 3 for Rgb888x if you used that format
            int stride = bmp.RowBytes;
            int size = stride * height;

            byte[] data = new byte[size];
            IntPtr ptr = bmp.GetPixels();
            Marshal.Copy(ptr, data, 0, size);

            // Convert to grayscale
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * stride + x * bytesPerPixel;

                    byte b = data[i];
                    byte g = data[i + 1];
                    byte r = data[i + 2];

                    byte gray = (byte)(0.299 * r + 0.587 * g + 0.114 * b);

                    data[i] = gray;       // B
                    data[i + 1] = gray;   // G
                    data[i + 2] = gray;   // R

                    // Optional: if using BGRA8888
                    // data[i + 3] = 255; // Leave alpha alone or set to 255
                }
            }

            // Copy back to SKBitmap
            Marshal.Copy(data, 0, ptr, size);
        }

		public void Threshold(SKBitmap bmp, int thresh)
		{
			int width = bmp.Width;
			int height = bmp.Height;

            // Access pixel data as IntPtr (unsafe) or copy to managed buffer
            // Using unsafe code for better performance:
            unsafe
            {
                IntPtr pixelsPtr = bmp.GetPixels();
                int bytesPerPixel = 4; // for BGRA8888 format

                byte* ptr = (byte*)pixelsPtr;

                int rowBytes = bmp.RowBytes;

                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + y * rowBytes;

                    for (int x = 0; x < width; x++)
                    {
                        int i = x * bytesPerPixel;

                        // BGRA order
                        byte b = row[i + 0];
                        byte g = row[i + 1];
                        byte r = row[i + 2];
                        // byte a = row[i + 3]; // alpha channel if needed

                        // Threshold each channel separately:
                        row[i + 0] = (byte)(b > thresh ? 255 : 0);
                        row[i + 1] = (byte)(g > thresh ? 255 : 0);
                        row[i + 2] = (byte)(r > thresh ? 255 : 0);
                    }
                }
            }
        }
	}
}

