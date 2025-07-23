using SkiaSharp;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace BarcodeReader.Core
{
#if CORE_DEV
    public
#else
    internal
#endif
    class BlackAndWhiteImage : IDisposable
    {
        private int _width, _height;
        private int _scanStep;

		private IPreparedImage _image;
        private BlackAndWhiteImage _unrotated;
        private XBitArray[] _rows, _columns; //cache of scanned rows and cols

        private bool _alreadyDisposed;

	    private ThresholdFilterMethod _method;
	    private IBlackAndWhiteFilter _filter;
	    private int _thresholdLevelAdjustment;

        const float PI=(float)Math.PI;
        private MyPointF p0;
        private MyVectorF vdX, vdY;
        private bool _rotated90;

	    private BlackAndWhiteImage()
	    {
	    }

        public BlackAndWhiteImage(SKBitmap bmp, int scanStep, ThresholdFilterMethod method, int thresholdLevelAdjustment) : this(bmp, scanStep, method, thresholdLevelAdjustment, false)
        {
            
        }


        public BlackAndWhiteImage(SKBitmap bmp, int scanStep, ThresholdFilterMethod method, int thresholdLevelAdjustment, bool sourceImageIsBinarized)
        {
            _scanStep = scanStep;
            _method = method;
            _thresholdLevelAdjustment = thresholdLevelAdjustment;

            switch (_method)
			{
				case ThresholdFilterMethod.Block:
    	            _image = new GrayscaleImage(bmp, scanStep, sourceImageIsBinarized);
                    _filter = new BlackAndWhiteBlockFilter(_image, thresholdLevelAdjustment);
					break;
                case ThresholdFilterMethod.Threshold:
                    _image = new GrayscaleImage(bmp, scanStep, sourceImageIsBinarized);
                    _filter = new BlackAndWhiteThresholdFilter(_image, 127);
                    break;
                case ThresholdFilterMethod.ThresholdEx:
                    _image = new GrayscaleImage(bmp, scanStep, sourceImageIsBinarized);
                    _filter = new BlackAndWhiteThresholdFilter(_image, thresholdLevelAdjustment);
                    break;
                case ThresholdFilterMethod.BlockSmoothed:
                    _image = new GrayscaleImage(bmp, scanStep);
                    _filter = new BlackAndWhiteBlockSmoothedFilter(_image, thresholdLevelAdjustment);
                    break;
                case ThresholdFilterMethod.BlockMedian:
                    _image = new GrayscaleImage(bmp, scanStep);
                    _filter = new BlackAndWhiteBlockMedianFilter(_image, thresholdLevelAdjustment);
                    break;
                case ThresholdFilterMethod.BlockGrid:
                    _image = new GrayscaleImage(bmp, scanStep);
                    _filter = new BlackAndWhiteBlockGridFilter(_image, thresholdLevelAdjustment);
                    break;
                case ThresholdFilterMethod.BlockOld:
					_image = new GrayscaleImage(bmp, scanStep);
					_filter = new BlackAndWhiteBlockFilterLegacy(_image, thresholdLevelAdjustment);
					break;
				case ThresholdFilterMethod.Enhancing:
					_image = new EnhancedBWImage(bmp /*, scanStep*/);
					_filter = new BlackAndWhiteBypassFilter(_image);
					break;
				default:
					_image = new GrayscaleImage(bmp, scanStep);
					_filter = new BlackAndWhiteFilter(_image);
					break;
			}

			_width = _image.Width;
			_height = _image.Height;
            
            vdX = new MyVectorF(1f, 0f);
            vdY = new MyVectorF(0f, 1f);
        }


		//angle [0..360°] but actually only 0..89° are enough (combined with 90°)
		//90° rotations are created just swaping rows by columns, thus much faster.
        public BlackAndWhiteImage(BlackAndWhiteImage image, float degAngle)
        {
            //normalize [0..360]
            while (degAngle >= 360F) degAngle -= 360F;
            while (degAngle < 0F) degAngle += 360F;

            _thresholdLevelAdjustment = image._thresholdLevelAdjustment;
            _scanStep = image._scanStep; //keep same scanStep as the original image
            if (_scanStep == 1)
            {
                _unrotated = image;
                if (degAngle == 90F)
                {
                    _rotated90 = true;
                }
                else
                {
                    _rotated90 = false;
                    CalculateRotatedBorders(image.Width, image.Height, degAngle, out _width, out _height, out p0, out vdX, out vdY);
                }
                _method = ThresholdFilterMethod.Rotated;
                _rows = new XBitArray[_height];
                _columns = new XBitArray[_width];
            }
            else
            {
				_image = new GrayscaleImage(image._image.SKBitmap, image._scanStep, degAngle);
                _width = _image.Width;
                _height = _image.Height;

                _method = image._method;

				// since the rotation made the prepared BW pixels gray again, set Block filter for it.
				if (_method == ThresholdFilterMethod.Enhancing)
					_method = ThresholdFilterMethod.Block;

                switch (_method)
                {
	                case ThresholdFilterMethod.Block:
		                _filter = new BlackAndWhiteBlockFilter(_image, _thresholdLevelAdjustment);
		                break;
                    case ThresholdFilterMethod.Threshold:
                        _filter = new BlackAndWhiteThresholdFilter(_image, 127);
                        break;
                    case ThresholdFilterMethod.BlockSmoothed:
                        _filter = new BlackAndWhiteBlockSmoothedFilter(_image, _thresholdLevelAdjustment);
                        break;
                    case ThresholdFilterMethod.BlockMedian:
                        _filter = new BlackAndWhiteBlockMedianFilter(_image, _thresholdLevelAdjustment);
                        break;
                    case ThresholdFilterMethod.BlockGrid:
                        _filter = new BlackAndWhiteBlockGridFilter(_image, _thresholdLevelAdjustment);
                        break;
                    case ThresholdFilterMethod.BlockOld:
		                _filter = new BlackAndWhiteBlockFilterLegacy(_image, _thresholdLevelAdjustment);
		                break;
	                default:
		                _filter = new BlackAndWhiteFilter(_image);
		                break;
                }

                vdX = new MyVectorF(1f, 0f);
                vdY = new MyVectorF(0f, 1f);
            }
        }

	    public BlackAndWhiteImage Clone()
	    {
			BlackAndWhiteImage copy = new BlackAndWhiteImage();
		    copy._width = _width;
			copy._height = _height;
			copy._scanStep = _scanStep;
			copy._image = _image == null ? null : _image.Clone();
			copy._unrotated = _unrotated;
			copy._alreadyDisposed = _alreadyDisposed;
			copy._method = _method;
			copy._filter = _filter;
			copy._thresholdLevelAdjustment = _thresholdLevelAdjustment;
			copy.p0 = new MyPointF(p0.X, p0.Y);
			copy.vdX = new MyVectorF(vdY.X, vdY.Y);
			copy._rotated90 = _rotated90;

			if (_rows == null) 
				copy._rows = null;
			else
			{
				copy._rows = new XBitArray[_rows.Length];
				for (var i = 0; i < _rows.Length; i++)
				{
					if (_rows[i] != null)
						copy._rows[i] = _rows[i].Clone();
				}
			}
			
			if (_columns == null)
				copy._columns = null;
			else
			{
				copy._columns = new XBitArray[_columns.Length];
				for (var i = 0; i < _columns.Length; i++)
				{
					if (_columns[i] != null)
						copy._columns[i] = _columns[i].Clone();
				}
			}

		    return copy;
	    }

	    public static void CalculateRotatedBorders(int width, int height, float degAngle, out int rotatedWidth, out int rotatedHeight, out MyPointF p0, out MyVectorF vdX, out MyVectorF vdY)
        {
            p0 = new MyPointF();
            float angle = PI * degAngle / 180F; //convert degree to radians

            float w = (float)width;
            float h = (float)height;
            float cosA = (float)Math.Cos(angle);
            float sinA = (float)Math.Sin(angle);

            //origin p0 and main directions vdX, vdY
            vdX.X = cosA;
            vdX.Y = sinA;
            vdY.X = -vdX.Y;  //rotated 90º
            vdY.Y = vdX.X;
            float l;
            if (angle <= PI / 2F)
            {
                l = w * sinA;
                p0.X = l * sinA;
                p0.Y = -l * cosA;
                rotatedWidth = (int)(w * cosA + h * sinA);
                rotatedHeight = (int)(h * cosA + w * sinA);
            }
            else if (angle < PI)
            {
                l = -h * cosA;
                p0.X = w + l * sinA;
                p0.Y = -l * cosA;
                rotatedWidth = (int)(h * sinA - w * cosA);
                rotatedHeight = (int)(w * sinA - h * cosA);
            }
            else if (angle < 3F * PI / 2F)
            {
                l = -w * sinA;
                p0.X = w + l * sinA;
                p0.Y = h - l * cosA;
                rotatedWidth = -(int)(w * cosA + h * sinA);
                rotatedHeight = -(int)(h * cosA + w * sinA);
            }
            else
            {
                l = h * cosA;
                p0.X = l * sinA;
                p0.Y = h - l * cosA;
                rotatedWidth = -(int)(h * sinA - w * cosA);
                rotatedHeight = -(int)(w * sinA - h * cosA);
            }
        }

        public void PostprocessResults(FoundBarcode[] barcodes)
        {
            if (_scanStep > 1)
            {
                foreach (FoundBarcode b in barcodes)
                {
					SKPoint[] poly = b.Polygon;
                    for (int i = 0; i < poly.Length; i++) poly[i].Y *= _scanStep;
				}
            }
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
                    if (_image != null)
                        _image.Dispose();
                }

                _image = null;
                _alreadyDisposed = true;
            }
        }

        public void Save(string fileName)
        {
#if DEBUG_IMAGE
            SKBitmap output = GetAsBitmap();
            output.Save(fileName);
            output.Dispose();
#endif
        }

        public SKBitmap GetAsBitmap()
        {
            var output = new SKBitmap(Width, Height, SKColorType.Gray8, SKAlphaType.Opaque);

            for (int y = 0; y < Height; y++)
            {
                XBitArray row = GetRow(y); // your method to get boolean row data

                unsafe
                {
                    byte* ptr = (byte*)output.GetAddress(0, y); // pointer to start of row
                    for (int x = 0; x < Width; x++)
                    {
                        // Set pixel: white for true, black for false (invert if needed)
                        ptr[x] = row[x] ? (byte)255 : (byte)0;
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Gets the width of the image.
        /// </summary>
        /// <value>The width of the image.</value>
        public int Width
        {
            get
            {
                return _rotated90?_unrotated.Height:_width;
            }
        }

        /// <summary>
        /// Gets the height of the image.
        /// </summary>
        /// <value>The height of the image.</value>
        public int Height
        {
            get
            {
                return _rotated90?_unrotated.Width: _height;
            }
        }

        public bool In(MyPoint p, int tolerance)
        {
            return p.X >= -tolerance && p.X < Width + tolerance &&
                   p.Y >= -tolerance && p.Y < Height + tolerance;
        }
        public bool In(MyPoint p) { return In(p.X, p.Y); }
        public bool In(int x, int y) { return InX(x) && InY(y); }
        public bool InX(int x) { return x >= 0 && x < Width; }
        public bool InY(int y) { return y >= 0 && y < Height; }

        /// <summary>
        /// Gets specified row of the image.
        /// </summary>
        /// <param name="y">The row index.</param>
        /// <returns>The array that contains black and white row.</returns>
		public XBitArray GetRow(int y)
	    {
		    switch (_method)
		    {
			    case ThresholdFilterMethod.Rotated:
			    {
				    if (_rotated90)
					    return _unrotated.GetColumn(Height - 1 - y);
				    if (_rows[y] == null)
					    return _rows[y] = ScanRotatedRow(y);
				    return _rows[y];
			    }
			    case ThresholdFilterMethod.None:
				    return _rows[y];
                default:
                    return _filter.GetRow(y);
            }

		    return null;
	    }

        /// <summary>
        /// Interpolated value
        /// </summary>
        public float GetPixelInterpolated(float x, float y)
        {
            var i = (int)x;
            var j = (int)y;
            var kx = x - i;
            var ky = y - j;

            if (i + 1 >= Width || j + 1 >= Height) return 127;

            var row0 = GetRow(j);
            var row1 = GetRow(j + 1);

            var v00 = row0[i] ? -1 : 1;
            var v10 = (row0[i + 1] ? -1 : 1) - v00;//right
            var v01 = (row1[i] ? -1 : 1) - v00;//bottom
            var v11 = (row1[i + 1] ? -1 : 1) - v00;//bottom-right

            return v00 + v10 * kx * (1 - ky) + v01 * (1 - kx) * ky + v11 * kx * ky;
        }

        XBitArray ScanRotatedRow(int y)
        {
            XBitArray row = new XBitArray(_width);
            MyPointF a = p0 + vdY * (float)y;
            for (int i = 0; i < _width; i++)
            {
                row[i] = _unrotated.IsBlack(a);
                a += vdX;
            }
            return row;
        }

        XBitArray ScanRotatedColumn(int x)
        {
            XBitArray column = new XBitArray(_height);
            for (int i = 0 ; i < _height; i++)
            {
                if (_rows[i] == null) _rows[i] = ScanRotatedRow(i);
                column[i] = _rows[i][x];
            }
            return column;
        }

        public SKPoint Unrotate(MyPointF p)
        {
            SKPoint Q = new SKPoint();
            if (_rotated90)
                Q = _unrotated.Unrotate(new MyPointF(Height -1 -p.Y, p.X));
            else  
                Q=p0 + vdX * p.X + vdY * p.Y;
            return Q;
        }

        public SKPoint[] Unrotate(SKPoint[] ps)
        {
            SKPoint[] r = new SKPoint[ps.Length];
            for (int i = 0; i < ps.Length; i++)
                r[i] = Unrotate(ps[i]);
            return r;
        }

        public SKPoint[] Unrotate(Rectangle rect)
        {
            SKPoint[] polygon = new SKPoint[5];
            polygon[0] = Unrotate(new MyPoint(rect.X, rect.Y));
            polygon[1] = Unrotate(new MyPoint(rect.X+rect.Width, rect.Y));
            polygon[2] = Unrotate(new MyPoint(rect.X+rect.Width, rect.Y+rect.Height));
            polygon[3] = Unrotate(new MyPoint(rect.X, rect.Y+rect.Height));
            polygon[4] = polygon[0];
            return polygon;
        }

        public bool IsBlack(MyPoint p)
        {
            if (p.Y >= 0 && p.Y < Height && p.X >= 0 && p.X < Width) return GetRow(p.Y)[p.X];
            return false; //by default white 
        }

        public void ResetColumns()
        {
            _filter.ResetColumns();
        }

		public XBitArray GetColumn(int x)
        {
            switch (_method)
            {
                default:
				    return _filter.GetColumn(x);
                case ThresholdFilterMethod.Rotated:
                    if (_rotated90) 
						return _unrotated.GetRow(x).Reverse();
		            if (_columns[x] == null) 
						return _columns[x] = ScanRotatedColumn(x);
		            return _columns[x];
            }
            return null;
        }

        public int ThresholdLevelAdjustment
        {
            get { return _thresholdLevelAdjustment; }
            set { _thresholdLevelAdjustment = value; }
        }

        public bool IsParallelSupported
        {
            get { return (_filter is IParallelSupporting) ? (_filter as IParallelSupporting).IsParallelSupported : false; }
        }
    }
}
