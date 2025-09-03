using System;

namespace BarcodeReader.Core
{
#if CORE_DEV
    public
#else
    internal
#endif
    interface IBlackAndWhiteFilter
	{
		XBitArray GetRow(int y);
		XBitArray GetColumn(int y);
	    void ResetColumns();
	}

    /// <summary>
    /// Converts given grayscale image into a black and white (1 bit) image.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
	class BlackAndWhiteFilter : IBlackAndWhiteFilter
    {
        private const int LuminanceBits = 5;
        private const int LuminanceShift = 8 - LuminanceBits;
        private const int LuminanceBucketCount = 1 << LuminanceBits;

		private IPreparedImage _image;
        private int[] _buckets = null;
        private XBitArray[] _rows;
        private XBitArray[] _columns;

		public BlackAndWhiteFilter(IPreparedImage image)
        {
            _image = image;

			_rows = new XBitArray[_image.Height];
			_columns = new XBitArray[_image.Width];
        }

        /// <summary>
        /// Gets specified row of grayscale image converted to black and white.
        /// </summary>
        /// <param name="y">The row index.</param>
        /// <returns>The array that contains black and white row.</returns>
        public XBitArray GetRow(int y)
	    {
			lock (_rows)
		    {
			    int width = _image.Width;

			    if (_rows[y] != null)
				    return _rows[y];

			    _rows[y] = new XBitArray(width);
			    XBitArray row = _rows[y];

			    if (_buckets == null)
				    _buckets = new int[LuminanceBucketCount];
			    else
				    Array.Clear(_buckets, 0, _buckets.Length);

			    byte[] grayScaleRow = _image.GetRow(y);
			    for (int x = 0; x < width; x++)
			    {
				    int pixel = grayScaleRow[x];
				    _buckets[pixel >> LuminanceShift]++;
			    }

			    bool isUnusable;
			    int blackPoint = estimateBlackPoint(_buckets, out isUnusable);
			    if (isUnusable)
				    return row;

			    int left = grayScaleRow[0];
			    int center = grayScaleRow[1];
			    for (int x = 1; x < width - 1; x++)
			    {
				    int right = grayScaleRow[x + 1];

				    // -1 4 -1 box filter with a weight of 2.
				    int luminance = ((center << 2) - left - right) >> 1;
				    if (luminance < blackPoint)
					    row[x] = true;

				    left = center;
				    center = right;
			    }

			    return row;
		    }
	    }

        public void ResetColumns()
        {
            lock (_columns)
            {
                for (int x = 0; x < _image.Width; x++)
                    _columns[x] = null;
            }
        }

        /// <summary>
        /// Gets specified column of grayscale image converted to black and white.
        /// </summary>
        /// <param name="x">The column index.</param>
        /// <returns>The array that contains black and white row.</returns>
        public XBitArray GetColumn(int x)
	    {
			lock (_columns)
			{
				if (_columns[x] == null)
					_columns[x] = new XBitArray(_image.Height);
				else
					return _columns[x];

				XBitArray column = _columns[x];

				// now fill the column
				for (int j = 0; j < column.Size; j++)
				{
					column[j] = GetRow(j)[x];
				}

				return column;
			}
	    }

	    private static int estimateBlackPoint(int[] buckets, out bool isUnusable)
        {
            // Find the tallest peak in the histogram.
            int firstPeakIndex = 0;
            int firstPeakValue = 0;
            for (int x = 0; x < buckets.Length; x++)
            {
                if (buckets[x] > firstPeakValue)
                {
                    firstPeakIndex = x;
                    firstPeakValue = buckets[x];
                }
            }

            int maxBucketValue = firstPeakValue;

            // Find an another tallest peak (shorter than first) which is
            // somewhat far from the tallest peak.
            int secondPeakIndex = 0;
            int secondPeakScore = 0;
            for (int x = 0; x < buckets.Length; x++)
            {
                int distanceToFirst = x - firstPeakIndex;
                // Encourage more distant second peaks by multiplying by square of distance.
                int score = buckets[x] * distanceToFirst * distanceToFirst;
                if (score > secondPeakScore)
                {
                    secondPeakIndex = x;
                    secondPeakScore = score;
                }
            }

            // Make sure firstPeakIndex corresponds to the black peak.
            if (firstPeakIndex > secondPeakIndex)
            {
                int temp = firstPeakIndex;
                firstPeakIndex = secondPeakIndex;
                secondPeakIndex = temp;
            }

            // If there is too little contrast in the image to pick a
            // meaningful black point, mark result as unusable.
            if (secondPeakIndex - firstPeakIndex <= buckets.Length >> 4)
            {
                isUnusable = true;
                return 0;
            }

            // Find a valley between them that is low and closer to the white peak.
            int bestValleyIndex = secondPeakIndex - 1;
            int bestValleyScore = -1;
            for (int x = secondPeakIndex - 1; x > firstPeakIndex; x--)
            {
                int distanceToFirst = x - firstPeakIndex;
                int distanceToSecond = secondPeakIndex - x;

                int score = distanceToFirst * distanceToFirst * distanceToSecond * (maxBucketValue - buckets[x]);
                if (score > bestValleyScore)
                {
                    bestValleyIndex = x;
                    bestValleyScore = score;
                }
            }

            isUnusable = false;
            return bestValleyIndex << LuminanceShift;
        }
    }
}
