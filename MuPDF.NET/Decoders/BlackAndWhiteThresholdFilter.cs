using System;
using System.Diagnostics;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core
{
    /// <summary>
    /// Converts given grayscale image into a black and white (1 bit) image.
    /// This filter make simple cutoff with predefined threshlod level.
    /// </summary>
    internal class BlackAndWhiteThresholdFilter : IBlackAndWhiteFilter, IParallelSupporting
    {
        //cutoff threshold
        protected readonly int ThresholdLevel = 127;

        protected IPreparedImage _image;
        protected XBitArray[] _rows;
        private XBitArray[] _columns;

        public BlackAndWhiteThresholdFilter(IPreparedImage image, int thresholdLevel = 127)
        {
            _image = image;
            ThresholdLevel = thresholdLevel;

            _rows = new XBitArray[_image.Height];
            _columns = new XBitArray[_image.Width];

            for (int i = 0; i < _rows.Length; i++)
                _rows[i] = new XBitArray(_image.Width);

            binarizeEntireImage();
        }

        public XBitArray GetRow(int y)
        {
            return _rows[y];
        }

        public void ResetColumns()
        {
            lock (_columns)
            {
                for (int x = 0; x < _image.Width; x++)
                    _columns[x] = null;
            }
        }

        public XBitArray GetColumn(int x)
        {
            lock (_columns)
            {
                if (_columns[x] == null)
                {
                    XBitArray column = new XBitArray(_image.Height);

                    //now fill the column
                    for (int j = 0; j < column.Size; j++)
                    {
                        column[j] = GetRow(j)[x];
                    }

                    _columns[x] = column;
                }

                return _columns[x];
            }
        }

        protected virtual void binarizeEntireImage()
        {
            //try to read pixels and fill rows
            Parallel.For(0, _image.Height, -1, 100, (pair) =>
            {
                for (var y = pair.From; y < pair.To; y++)
                {
                    var row = _image.GetRow(y);
                    var outRow = _rows[y];

                    for (var x = 0; x < row.Length; x++)
                    {
                        if(row[x] < ThresholdLevel)
                            outRow[x] = true;
                    }
                }
            });
        }
        
        public bool IsParallelSupported
        {
            get { return true; }
        }
    }
}