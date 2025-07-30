using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ZXing;
using ZXing.Common.Detector;

namespace MuPDF.NET
{
    public class BarcodePoint
    {
        private readonly float x;

        private readonly float y;

        private readonly byte[] bytesX;

        private readonly byte[] bytesY;

        private string toString;

        public virtual float X => x;

        public virtual float Y => y;

        public BarcodePoint()
        {
        }

        public BarcodePoint(float x, float y)
        {
            this.x = x;
            this.y = y;
            bytesX = BitConverter.GetBytes(x);
            bytesY = BitConverter.GetBytes(y);
        }

        public override bool Equals(object other)
        {
            if (!(other is BarcodePoint resultPoint))
            {
                return false;
            }

            if (x == resultPoint.x)
            {
                return y == resultPoint.y;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return 31 * ((bytesX[0] << 24) + (bytesX[1] << 16) + (bytesX[2] << 8) + bytesX[3]) + (bytesY[0] << 24) + (bytesY[1] << 16) + (bytesY[2] << 8) + bytesY[3];
        }

        public override string ToString()
        {
            if (toString == null)
            {
                StringBuilder stringBuilder = new StringBuilder(25);
                stringBuilder.AppendFormat(CultureInfo.CurrentUICulture, "({0}, {1})", x, y);
                toString = stringBuilder.ToString();
            }

            return toString;
        }

        public static void orderBestPatterns(BarcodePoint[] patterns)
        {
            float num = distance(patterns[0], patterns[1]);
            float num2 = distance(patterns[1], patterns[2]);
            float num3 = distance(patterns[0], patterns[2]);
            BarcodePoint resultPoint;
            BarcodePoint resultPoint2;
            BarcodePoint resultPoint3;
            if (num2 >= num && num2 >= num3)
            {
                resultPoint = patterns[0];
                resultPoint2 = patterns[1];
                resultPoint3 = patterns[2];
            }
            else if (num3 >= num2 && num3 >= num)
            {
                resultPoint = patterns[1];
                resultPoint2 = patterns[0];
                resultPoint3 = patterns[2];
            }
            else
            {
                resultPoint = patterns[2];
                resultPoint2 = patterns[0];
                resultPoint3 = patterns[1];
            }

            if (crossProductZ(resultPoint2, resultPoint, resultPoint3) < 0f)
            {
                BarcodePoint resultPoint4 = resultPoint2;
                resultPoint2 = resultPoint3;
                resultPoint3 = resultPoint4;
            }

            patterns[0] = resultPoint2;
            patterns[1] = resultPoint;
            patterns[2] = resultPoint3;
        }

        public static float distance(BarcodePoint pattern1, BarcodePoint pattern2)
        {
            return MathUtils.distance(pattern1.x, pattern1.y, pattern2.x, pattern2.y);
        }

        private static float crossProductZ(BarcodePoint pointA, BarcodePoint pointB, BarcodePoint pointC)
        {
            float num = pointB.x;
            float num2 = pointB.y;
            return (pointC.x - num) * (pointA.y - num2) - (pointC.y - num2) * (pointA.x - num);
        }
    }    

    public class Barcode
    {
        public string Text { get; private set; }
        public byte[] RawBytes { get; private set; }
        public BarcodePoint[] ResultPoints { get; private set; }
        public BarcodeFormat BarcodeFormat { get; private set; }
        public long Timestamp { get; private set; }
        public int NumBits { get; private set; }
        public Barcode(string text, byte[] rawBytes, BarcodePoint[] resultPoints, BarcodeFormat type)
        : this(text, rawBytes, (rawBytes != null) ? (8 * rawBytes.Length) : 0, resultPoints, type, DateTime.Now.Ticks)
        {
        }

        public Barcode(string text, byte[] rawBytes, int numBits, BarcodePoint[] resultPoints, BarcodeFormat type)
            : this(text, rawBytes, numBits, resultPoints, type, DateTime.Now.Ticks)
        {
        }

        public Barcode(string text, byte[] rawBytes, BarcodePoint[] resultPoints, BarcodeFormat type, long timestamp)
            : this(text, rawBytes, (rawBytes != null) ? (8 * rawBytes.Length) : 0, resultPoints, type, timestamp)
        {
        }

        public Barcode(string text, byte[] rawBytes, int numBits, BarcodePoint[] resultPoints, BarcodeFormat type, long timestamp)
        {
            if (text == null && rawBytes == null)
            {
                throw new ArgumentException("Text and bytes are null");
            }

            Text = text;
            RawBytes = rawBytes;
            NumBits = numBits;
            ResultPoints = resultPoints;
            BarcodeFormat = type;
            Timestamp = timestamp;
        }

        public void addResultPoints(BarcodePoint[] newPoints)
        {
            BarcodePoint[] resultPoints = ResultPoints;
            if (resultPoints == null)
            {
                ResultPoints = newPoints;
            }
            else if (newPoints != null && newPoints.Length != 0)
            {
                BarcodePoint[] array = new BarcodePoint[resultPoints.Length + newPoints.Length];
                Array.Copy(resultPoints, 0, array, 0, resultPoints.Length);
                Array.Copy(newPoints, 0, array, resultPoints.Length, newPoints.Length);
                ResultPoints = array;
            }
        }

        public override string ToString()
        {
            if (Text == null)
            {
                return "[" + RawBytes.Length + " bytes]";
            }

            return Text;
        }
    }
}
