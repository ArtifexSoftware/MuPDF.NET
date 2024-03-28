namespace MuPDF.NET
{
    public class ByteStream
    {
        private byte[] _data;

        private long _offset;

        private int _length;

        public static int Begin = 1;
        public static int Current = 2;
        public static int End = 3;

        public ByteStream(byte[] data, int offset, int length)
        {
            _data = data;
            _offset = offset;
            _length = length;
        }

        public ByteStream(byte[] data)
        {
            _data = data;
            _offset = 0;
            _length = data.Length;
        }

        public ByteStream()
        {
            _data = new byte[0];
            _offset = 0;
            _length = _data.Length;
        }

        public ByteStream(int capacity)
        {
            _data = new byte[capacity];
            _offset = 0;
            _length = capacity;
        }

        public byte[] Data
        {
            get { return _data; }
        }

        public long Offset
        {
            get { return _offset; }
        }

        public int Write(byte[] buffer, int length)
        {
            long t = _offset + length - _length;
            if (t > 0)
            {
                byte[] tmp = new byte[_offset + length];
                for (int i = 0; i < _length; i++)
                    tmp[i] = _data[i];
                _data = tmp;
            }

            for (int i = 0; i < buffer.Length; i++)
                _data[_offset + i] = buffer[i];

            _length = _data.Length;
            _offset += length;

            return _length;
        }

        public void Seek(long offset, int option)
        {
            switch (option)
            {
                case 1:
                    _offset = offset; break;
                case 2:
                    _offset = offset + _offset; break;
                case 3:
                    _offset = offset + _length; break;
            }
        }

        public void Resize(int size)
        {
            _length = size;
            _offset = 0;
            _data = new byte[size];
        }
    }
}
