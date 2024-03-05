using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class ByteStream
    {
        private byte[] _data;

        private int _offset;

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
            _data= data;
            _offset = 0;
            _length= data.Length;
        }

        public ByteStream(int capacity)
        {
            _data= new byte[capacity];
            _offset = 0;
            _length = capacity;
        }

        public byte[] Data
        {
            get { return _data; }
        }

        public int Offset
        {
            get { return _offset; }
        }

        public int Write(byte[] buffer, int length)
        {
            int t = _offset + length - _length;
            List<byte> list = new List<byte>(_data);
            if (t > 0)
                for (int i = 0; i < t; i++)
                    list.Add(0);// resize

            for (int i = 0; i < buffer.Length; i++)
                list[_offset + i] = buffer[i];

            _data = list.ToArray();
            _length = _data.Length;
            _offset += length;

            return _length;
        }

        public void Seek(int offset, int option)
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
