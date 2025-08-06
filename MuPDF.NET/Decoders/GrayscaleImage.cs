using System;
using System.Drawing;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace BarcodeReader.Core
{
#if !OLD_GRAYSCALEIMAGE

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
        public readonly bool UseMarshalCopy = true; // use Marshal.Copy or GetPixel instead (safe in ASP.NET Medium Trust)

        private bool _alreadyDisposed;

        private SKBitmap _bitmap;
        private int _scanStep;
	    private bool _shouldDisposeBitmap;

        private int _originalWidth;  //original size of bitmap
        private int _originalHeight;

        const float PI = (float) Math.PI;
        private MyPointF p0;
        private MyVectorF vdX, vdY;
        private bool _simpleMode;

        /// <summary>
        /// already computed grayscale samples of an image.
        /// this array gets allocated and filled when calls to GetRow are made
        /// </summary>
        private byte[][] _samples;
        

	    private GrayscaleImage()
	    {
	    }

        // Slow resampling of the bitmap with the given angle (slow because marshal copy can not be used).
        /*
        public GrayscaleImage(SKBitmap bitmap, int scanStep, float degAngle)
        {
            //normalize [0..360]
            while (degAngle >= 360F) degAngle -= 360F;
            while (degAngle < 0F) degAngle += 360F;

            _shouldDisposeBitmap = true;
            UseMarshalCopy = IsFullyTrusted; //in ASP Medium trust mode => false
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

            //
            if (UseMarshalCopy)
                GetPixelsViaMarshalCopy();
            else
                GetPixelsViaGetPixels();
        }
        */

        public GrayscaleImage(SKBitmap bitmap, int scanStep, float degAngle)
        {
            // Normalize angle to [0, 360)
            while (degAngle >= 360f) degAngle -= 360f;
            while (degAngle < 0f) degAngle += 360f;

            _shouldDisposeBitmap = true;
            UseMarshalCopy = IsFullyTrusted; // your custom trust check
            _scanStep = scanStep;

            // Calculate rotated borders
            BlackAndWhiteImage.CalculateRotatedBorders(bitmap.Width, bitmap.Height, degAngle, out _originalWidth, out _originalHeight, out p0, out vdX, out vdY);

            // Create target bitmap for the rotated image
            SKBitmap newBitmap = new SKBitmap(_originalWidth, _originalHeight);
            using (var canvas = new SKCanvas(newBitmap))
            {
                // Clear canvas (optional)
                canvas.Clear(SKColors.White);

                // Center transform
                canvas.Translate(_originalWidth / 2f, _originalHeight / 2f);
                canvas.RotateDegrees(-degAngle); // negative because Skia rotates clockwise
                canvas.Translate(-bitmap.Width / 2f, -bitmap.Height / 2f);

                // Draw the original bitmap into the rotated context
                canvas.DrawBitmap(bitmap, 0, 0);
            }

            _bitmap = newBitmap;
            Width = _originalWidth;
            Height = _originalHeight / _scanStep;

            // Get grayscale pixels
            if (UseMarshalCopy)
                GetPixelsViaMarshalCopy(); // you’ll need to update this to use `SKPixmap`
            else
                GetPixelsViaGetPixels();  // or modify it to use `SKBitmap.GetPixel`
        }

        /// <param name="simpleMode">Take to attention only Blue channel</param>
        public GrayscaleImage(SKBitmap bitmap, int scanStep, bool simpleMode)
        {
            UseMarshalCopy = IsFullyTrusted; //in ASP Medium trust mode => false
            _simpleMode = simpleMode;
            _bitmap = bitmap;
            _shouldDisposeBitmap = false;
            _scanStep = scanStep;
            _originalWidth = _bitmap.Width;
            _originalHeight = _bitmap.Height;
            Width = _originalWidth;
            Height = _originalHeight / _scanStep;

            if (UseMarshalCopy)
                GetPixelsViaMarshalCopy();
            else
                GetPixelsViaGetPixels();
        }


        public GrayscaleImage(SKBitmap bitmap, int scanStep) : this(bitmap, scanStep, false)
		{
        }


        bool IsFullyTrusted
        {
            get
            {
                try
                {
                    //new System.Security.Permissions.SecurityPermission(System.Security.Permissions.PermissionState.Unrestricted).Demand();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public IPreparedImage Clone()
	    {
			GrayscaleImage copy = new GrayscaleImage();
			copy._alreadyDisposed = _alreadyDisposed;
			copy._bitmap = _bitmap == null ? null : (SKBitmap) _bitmap.Copy();
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
            return _samples[y];
        }

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

        private void GetPixelsViaMarshalCopy()
        {
            normalizeBitmapFormat();

            // Get pixel info and pointer
            using (SKPixmap pixmap = _bitmap.PeekPixels())
            {
                int stride = pixmap.RowBytes;
                int totalBytes = stride * _originalHeight;
                IntPtr pixelsPtr = pixmap.GetPixels();

                // Allocate managed array
                byte[] pixels = new byte[totalBytes];

                // Copy unmanaged pixels into managed array
                Marshal.Copy(pixelsPtr, pixels, 0, totalBytes);

                // Allocate _samples rows
                _samples = new byte[Height][];

                int start = 0;
                int step = stride * _scanStep;

                if (_simpleMode)
                    ToGrayScaleSimple(start, step, pixels);
                else
                    ToGrayScale(start, step, pixels);
            }                
        }
        /*
        private void GetPixelsViaMarshalCopy()
        {
            normalizeBitmapFormat();

            //get pixels
            BitmapData imgData = _bitmap.LockBits(new Rectangle(0, 0, _originalWidth, _originalHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var pixels = new byte[imgData.Stride * _originalHeight];
            Marshal.Copy(imgData.Scan0, pixels, 0, imgData.Stride * _originalHeight);

            //copy pixels to byte array
            _samples = new byte[Height][];

            var start = 0;
            var step = imgData.Stride * _scanStep;

            if (_simpleMode)
                ToGrayScaleSimple(start, step, pixels);
            else
                ToGrayScale(start, step, pixels);
            
            _bitmap.UnlockBits(imgData);
        }
        */

        private void ToGrayScale(int start, int step, byte[] pixels)
        {
            for (int y = 0; y < Height; y++, start += step)
            {
                var row = _samples[y] = new byte[Width];
                var srcX = start;
                for (int x = 0; x < row.Length; x++, srcX += 4)
                {
                    row[x] = (byte) ((306 * pixels[srcX + R] + 601 * pixels[srcX + G] + 117 * pixels[srcX + B]) >> 10);
                }
            }
        }

        private void ToGrayScaleSimple(int start, int step, byte[] pixels)
        {
            for (int y = 0; y < Height; y++, start += step)
            {
                var row = _samples[y] = new byte[Width];
                var srcX = start;
                for (int x = 0; x < row.Length; x++, srcX += 4)
                {
                    row[x] = pixels[srcX];
                }
            }
        }

        private void GetPixelsViaGetPixels()
        {
            //copy pixels to byte array
            _samples = new byte[Height][];

            for (int y = 0; y < Height; y++)
            {
                var row = _samples[y] = new byte[Width];
                for (int x = 0; x < Width; x++)
                {
                    var pixel = _bitmap.GetPixel(x, y * _scanStep);
                    if(_simpleMode)
                        row[x] = pixel.Blue;
                    else
                        row[x] = (byte) ((306 * pixel.Red + 601 * pixel.Green + 117 * pixel.Blue) >> 10);
                }
            }
        }

        private void normalizeBitmapFormat()
        {
            // Ensure bitmap is in 24bpp equivalent format (RGB)
            if (_bitmap.ColorType != SKColorType.Rgb888x || _bitmap.AlphaType != SKAlphaType.Opaque)
            {
                // Create a new bitmap with desired format
                var temp = new SKBitmap(_bitmap.Width, _bitmap.Height, SKColorType.Rgb888x, SKAlphaType.Opaque);

                using (var canvas = new SKCanvas(temp))
                {
                    canvas.Clear(SKColors.White);
                    canvas.DrawBitmap(_bitmap, new SKPoint(0, 0));
                }

                if (_shouldDisposeBitmap)
                    _bitmap.Dispose();

                _bitmap = temp;
                _shouldDisposeBitmap = true;
            }
        }
        /*
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
        */
    }

#endif // !OLD_GRAYSCALEIMAGE
}
