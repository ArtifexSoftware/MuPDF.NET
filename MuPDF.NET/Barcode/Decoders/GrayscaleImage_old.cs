using System;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace BarcodeReader.Core
{
#if OLD_GRAYSCALEIMAGE

    /// <summary>
    /// Allows requesting grayscale (luminance) values for any given
    /// source image row.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class GrayscaleImage : IPreparedImage
    {
        public bool UseMarshalCopy = true; // use Marshal.Copy or GetPixel instead (safe in ASP.NET Medium Trust)

        private bool _alreadyDisposed;

        /// <summary>
        /// Index of red component.
        /// </summary>
        public const short R = 2;

        /// <summary>
        /// Index of green component.
        /// </summary>
        public const short G = 1;

        /// <summary>
        /// Index of blue component.
        /// </summary>
        public const short B = 0;

        /// <summary>
        /// Index of alpha component.
        /// </summary>
        public const short A = 3;

        private SKBitmap _bitmap;
        private int _scanStep;
	    private bool _shouldDisposeBitmap;

        private int _originalWidth;  //original size of bitmap
        private int _originalHeight;

        const float PI = (float) Math.PI;
        private MyPointF p0;
        private MyVectorF vdX, vdY;


        /// <summary>
        /// already computed grayscale samples of an image.
        /// this array gets allocated and filled when calls to GetRow are made
        /// </summary>
        private byte[][] _samples;

        /// <summary>
        /// bytes of a one source image row 
        /// </summary>
        private byte[] _imgBytes;

	    private GrayscaleImage()
	    {
	    }

		public GrayscaleImage(SKBitmap bitmap, int scanStep)
		{
			_shouldDisposeBitmap = false;
            UseMarshalCopy = true;
            _bitmap = bitmap;
            _scanStep = scanStep;
            _originalWidth = _bitmap.Width;
            _originalHeight = _bitmap.Height;
            Width = _originalWidth;
            Height = _originalHeight / _scanStep;
            normalizeBitmapFormat();
            vdX = new MyVectorF(1f, 0f);
            vdY = new MyVectorF(0f, 1f);
        }

		// Slow resampling of the bitmap with the given angle (slow because marshal copy can not be used).
		public GrayscaleImage(SKBitmap bitmap, int scanStep, float degAngle)
        {
            //normalize [0..360]
            while (degAngle >= 360F) degAngle -= 360F;
            while (degAngle < 0F) degAngle += 360F;

            _shouldDisposeBitmap = true;
            UseMarshalCopy = true;
            _scanStep = scanStep;
            BlackAndWhiteImage.CalculateRotatedBorders(bitmap.Width, bitmap.Height, degAngle, out _originalWidth, out _originalHeight, out p0, out vdX, out vdY);

            SKBitmap newBitmap = new SKBitmap(_originalWidth, _originalHeight, Graphics.FromImage(bitmap));
            using (Graphics graphics = Graphics.FromImage(newBitmap))
            {
	            graphics.TranslateTransform((float)_originalWidth / 2, (float)_originalHeight / 2);
	            graphics.RotateTransform(-degAngle);
	            graphics.TranslateTransform(-(float)bitmap.Width / 2, -(float)bitmap.Height / 2);
	            graphics.DrawImage(bitmap, new SKPointI(0, 0));
            }

            _bitmap = newBitmap;

            Width = _originalWidth;
            Height = _originalHeight / _scanStep;
        }
		
	    public IPreparedImage Clone()
	    {
			GrayscaleImage copy = new GrayscaleImage();
			copy.UseMarshalCopy = UseMarshalCopy;
			copy._alreadyDisposed = _alreadyDisposed;
			copy._bitmap = _bitmap == null ? null : (SKBitmap) _bitmap.Clone();
			copy._scanStep = _scanStep;
			copy._shouldDisposeBitmap = _shouldDisposeBitmap;
			copy._originalWidth = _originalWidth;
			copy._originalHeight = _originalHeight;
			copy.Width = Width;
			copy.Height = Height;
			copy.p0 = new MyPointF(p0.X, p0.Y);
			copy.vdX = new MyVectorF(vdX.X, vdX.Y);
			copy.vdY = new MyVectorF(vdY.X, vdY.Y);
			
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

		    if (_imgBytes == null)
			    copy._imgBytes = null;
		    else
		    {
			    copy._imgBytes = new byte[_imgBytes.Length];
				Array.Copy(_imgBytes, copy._imgBytes, _imgBytes.Length);
		    }

		    return copy;
	    }

	    public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_alreadyDisposed)
            {
                if (disposing)
                {
                    if (_shouldDisposeBitmap && _bitmap != null)
                        _bitmap.Dispose();
                }

                _bitmap = null;
                _alreadyDisposed = true;
            }
        }

        public SKBitmap SKBitmap
        {
            get { return _bitmap; }
        }

        public bool ShouldDisposeBitmap
        {
            get { return _shouldDisposeBitmap; }
            set { _shouldDisposeBitmap = value; }
        }

        public void Save(string fileName)
        {
#if DEBUG_IMAGE
            SKBitmap output = new SKBitmap(Width, Height, PixelFormat.Format24bppRgb);
            byte[] scanBytes = null;
            Rectangle imgRect = new Rectangle(0, 0, Width, 1);

            for (int y = 0; y < Height; y++)
            {
                imgRect.Y = y;
                BitmapData imgData = output.LockBits(imgRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                if (scanBytes == null)
                    scanBytes = new byte[imgData.Stride];

                Array.Clear(scanBytes, 0, scanBytes.Length);
                byte[] row = GetRow(y);

                for (int x = 0, scanPos = 0; x < Width; x++)
                {
                    byte luminance = row[x];
                    scanBytes[scanPos++] = luminance;
                    scanBytes[scanPos++] = luminance;
                    scanBytes[scanPos++] = luminance;
                }

                Marshal.Copy(scanBytes, 0, imgData.Scan0, scanBytes.Length);
                output.UnlockBits(imgData);
            }

            output.Save(fileName);
            output.Dispose();
#endif
        }

        /// <summary>
        /// Gets the width of the image.
        /// </summary>
        /// <value>The width of the image.</value>
        public int Width { get; private set; }

        /// <summary>
        /// Gets the height of the image.
        /// </summary>
        /// <value>The height of the image.</value>
        public int Height { get; private set; }

        /// <summary>
        /// Gets specified row of image converted to grayscale.
        /// </summary>
        /// <param name="y">The row index.</param>
        /// <returns>The array that contains grayscale row.</returns>
        public byte[] GetRow(int y)
        {
            if (y < 0 || y >= Height)
                throw new ArgumentException();

            if (_samples == null)
            {
                _samples = new byte[Height][];

                //constructAllSampleRows();
            }

            if (_samples[y] == null)
            {
                _samples[y] = new byte[Width];

                if (UseMarshalCopy)
                    constructSampleRow_ViaMarshalCopy(y); // use Marshal.Copy (fast but not compatible with ASP.NET medium trust
                else
                    constructSampleRow_Safe(y); // safe in ASP.NET Medium Trust (uses GetPixel) but very slow (x10 times slower)
            }

            return _samples[y];
        }

        /// <summary>
        /// Constructs the sample row using default grayscale transformation.
        /// Y = 0.299R + 0.587G + 0.114B
        /// </summary>
        /// <param name="y">The row index.</param>
        private void constructSampleRow_Safe(int y)
        {
            // retrieve data using GetPixel (to avoid problems in ASP.NET Medium Trust)
            // for faster variant (x10 speed faster) with Marshal.Copy see constructSampleRow_viaMarshalCopy()            
            MyPointF a = p0 + vdY * (float)(y * _scanStep);
            int luminance;
            for (int x = 0; x < Width; x++)
            {
                int xx = (int)a.X, yy = (int)a.Y;
                if (xx >= 0 && xx < _bitmap.Width && yy >= 0 && yy < _bitmap.Height)
                {
                    Color tmp = _bitmap.GetPixel((int)a.X, (int)a.Y);
                    luminance = (306 * tmp.R + 601 * tmp.G + 117 * tmp.B) >> 10;
                }
                else luminance = 255;
                _samples[y][x] = (byte)luminance;
                a += vdX;
            }
        }

        /// <summary>
        /// Constructs the sample row using default grayscale transformation.
        /// Y = 0.299R + 0.587G + 0.114B
        /// This variant uses Marshal.Copy which can not be used in the ASP.NET Medium Trust mode
        /// </summary>
        /// <param name="y">The row index.</param>
        private void constructSampleRow_ViaMarshalCopy(int y)
        {
			lock (_bitmap)
			{
				Rectangle imgRect = new Rectangle(0, y * _scanStep, Width, 1);
				BitmapData imgData = _bitmap.LockBits(imgRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

				if (_imgBytes == null || _imgBytes.Length < imgData.Stride)
					_imgBytes = new byte[imgData.Stride];

				Marshal.Copy(imgData.Scan0, _imgBytes, 0, imgData.Stride);
				_bitmap.UnlockBits(imgData);

				for (int x = 0, srcX = 0; x < Width; x++, srcX += 3)
				{
					int luminance = (306 * _imgBytes[srcX + R] +
					                 601 * _imgBytes[srcX + G] + 117 * _imgBytes[srcX + B]) >> 10;

					_samples[y][x] = (byte) luminance;
				}
			}
        }

        /*private void constructAllSampleRows()
        {
            _samples = new byte[Height][];

            Rectangle imgRect = new Rectangle(0, 0, Width, Height);
            BitmapData imgData = _bitmap.LockBits(imgRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            byte[] allBytes = new byte[imgData.Stride * Height];
            int stride = imgData.Stride;
            Marshal.Copy(imgData.Scan0, allBytes, 0, stride * Height);
            _bitmap.UnlockBits(imgData);

            for (int row = 0; row < Height; row++)
            {
                _samples[row] = new byte[Width];

                for (int x = 0, srcX = row * stride; x < Width; x++)
	            {
		            int luminance = 117 * allBytes[srcX++];
		            luminance += 601 * allBytes[srcX++];
		            luminance += 306 * allBytes[srcX++];

		            _samples[row][x] = (byte) (luminance >> 10);
	            }
            }
        }*/

        private void normalizeBitmapFormat()
        {
            if (_bitmap.PixelFormat != PixelFormat.Format24bppRgb)
            {
                SKBitmap temp = new SKBitmap(_bitmap.Width, _bitmap.Height, PixelFormat.Format24bppRgb);
                using (Graphics g = Graphics.FromImage(temp))
                {
	                g.Clear(Color.White);
	                g.DrawImage(_bitmap, new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), 0, 0, _bitmap.Width, _bitmap.Height, GraphicsUnit.Pixel);
                }

                if (_shouldDisposeBitmap)
                    _bitmap.Dispose();

                _bitmap = temp;
                _shouldDisposeBitmap = true;
            }
        }
    }

#endif // OLD_GRAYSCALEIMAGE
}
