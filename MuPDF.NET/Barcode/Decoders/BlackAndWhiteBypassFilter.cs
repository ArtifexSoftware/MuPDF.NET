namespace BarcodeReader.Core
{
    internal class BlackAndWhiteBypassFilter : IBlackAndWhiteFilter
	{
		private readonly IPreparedImage _image;
		private XBitArray[] _rows;
		private XBitArray[] _columns;

		public BlackAndWhiteBypassFilter(IPreparedImage image)
        {
            _image = image;

			_rows = new XBitArray[_image.Height];
			_columns = new XBitArray[_image.Width];
        }

		public XBitArray GetRow(int y)
		{
			lock (_rows)
			{
				if (_rows[y] != null)
					return _rows[y];

				_rows[y] = new XBitArray(_image.Width);
				XBitArray row = _rows[y];

				byte[] imageRow = _image.GetRow(y);

				for (int x = 0; x < _image.Width; x++)
					row[x] = imageRow[x] == 0;

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
				for (int y = 0; y < column.Size; y++)
				{
					column[y] = GetRow(y)[x];
				}

				return column;
			}
		}
	}
}