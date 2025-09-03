using System;
using System.Collections.Generic;

namespace BarcodeReader.Core
{
    /// <summary>
    /// Applies median filter to grayscale or binarized image.
    /// Use it to remove thin lines and single pixels from image.
    /// </summary>
    class BlackAndWhiteBlockMedianFilter : BlackAndWhiteBlockFilter
    {
        public bool Prefiltering = false;
        public bool Use8Connectivity = false;

        public BlackAndWhiteBlockMedianFilter(IPreparedImage image, int thresholdLevelAdjustment) : base(image, thresholdLevelAdjustment)
        {
        }

        protected override void binarizeEntireImage()
        {
            //prefiltering
            if (Prefiltering)
            {
                if(Use8Connectivity)
                    SmoothGrayscale8();
                else
                    SmoothGrayscale4();
            }
            
            //binarization
            base.binarizeEntireImage();

            //postfiltering
            if (!Prefiltering)
            {
                if (Use8Connectivity)
                    SmoothBinarized8();
                else
                    SmoothBinarized3();
                    //SmoothBinarized5();
            }
            
        }

        struct Pixel
        {
            public int X;
            public int Y;
            public byte Val;
        }

        private void SmoothGrayscale4()
        {
            var w = _image.Width;
            var h = _image.Height;

            var toChange = new LinkedList<Pixel>();

            var pixels = new byte[5];

            for (int y = 1; y < h - 1; y++)
            {
                var row0 = _image.GetRow(y - 1);
                var row1 = _image.GetRow(y);
                var row2 = _image.GetRow(y + 1);

                for (int x = 1; x < w - 1; x++)
                {
                    pixels[0] = row0[x];
                    pixels[1] = row1[x - 1];
                    pixels[2] = row1[x + 0];
                    pixels[3] = row1[x + 1];
                    pixels[4] = row2[x];

                    Array.Sort(pixels);

                    var need = pixels[2];

                    if (need != row1[x])
                        toChange.AddLast(new Pixel() {X = x, Y = y, Val = need});
                }
            }

            foreach (var p in toChange)
                _image.GetRow(p.Y)[p.X] = p.Val;
        }

        private void SmoothGrayscale8()
        {
            var w = _image.Width;
            var h = _image.Height;

            var toChange = new LinkedList<Pixel>();

            var pixels = new byte[9];

            for (int y = 1; y < h - 1; y++)
            {
                var row0 = _image.GetRow(y - 1);
                var row1 = _image.GetRow(y);
                var row2 = _image.GetRow(y + 1);

                for (int x = 1; x < w - 1; x++)
                {
                    pixels[0] = row0[x - 1];
                    pixels[1] = row0[x + 0];
                    pixels[2] = row0[x + 1];
                    pixels[3] = row1[x - 1];
                    pixels[4] = row1[x + 0];
                    pixels[5] = row1[x + 1];
                    pixels[6] = row2[x - 1];
                    pixels[7] = row2[x + 0];
                    pixels[8] = row2[x + 1];

                    Array.Sort(pixels);

                    var need = pixels[4];

                    if (need != row1[x])
                        toChange.AddLast(new Pixel() { X = x, Y = y, Val = need });
                }
            }

            foreach (var p in toChange)
                _image.GetRow(p.Y)[p.X] = p.Val;
        }

        private void SmoothBinarized3()
        {
            var w = _image.Width;
            var h = _image.Height;

            var toFalse = new LinkedList<MyPoint>();
            var toTrue = new LinkedList<MyPoint>();

            for (int y = 1; y < h - 2; y++)
            {
                var row0 = _rows[y - 1];
                var row1 = _rows[y + 0];
                var row2 = _rows[y + 1];
                var row3 = _rows[y + 2];

                for (int x = 1; x < w - 1; x++)
                {
                    var sum = 0;
                    if (row0[x]) sum++;
                    if (row1[x]) sum++;
                    if (row2[x]) sum++;
                    if (row3[x]) sum++;

                    var need = sum > 1;

                    if (row1[x + 0])
                    {
                        if (!need)
                            toFalse.AddLast(new MyPoint(x, y));
                    }
                    else
                    {
                        if (need)
                            toTrue.AddLast(new MyPoint(x, y));
                    }
                }
            }

            foreach (var p in toTrue)
                _rows[p.Y][p.X] = true;

            foreach (var p in toFalse)
                _rows[p.Y][p.X] = false;
        }

        private void SmoothBinarized5()
        {
            var w = _image.Width;
            var h = _image.Height;

            var toFalse = new LinkedList<MyPoint>();
            var toTrue = new LinkedList<MyPoint>();

            for (int y = 2; y < h - 2; y++)
            {
                var row0 = _rows[y - 2];
                var row1 = _rows[y - 1];
                var row2 = _rows[y + 0];
                var row3 = _rows[y + 1];
                var row4 = _rows[y + 2];

                for (int x = 1; x < w - 1; x++)
                {
                    var sum = 0;
                    if (row0[x]) sum++;
                    if (row1[x]) sum++;
                    if (row2[x]) sum++;
                    if (row3[x]) sum++;
                    if (row4[x]) sum++;

                    var need = sum > 2;

                    if (row2[x + 0])
                    {
                        if (!need)
                            toFalse.AddLast(new MyPoint(x, y));
                    }
                    else
                    {
                        if (need)
                            toTrue.AddLast(new MyPoint(x, y));
                    }
                }
            }

            foreach (var p in toTrue)
                _rows[p.Y][p.X] = true;

            foreach (var p in toFalse)
                _rows[p.Y][p.X] = false;
        }

        private void SmoothBinarized4()
        {
            var w = _image.Width;
            var h = _image.Height;

            var toFalse = new LinkedList<MyPoint>();
            var toTrue = new LinkedList<MyPoint>();

            for (int y = 1; y < h - 1; y++)
            {
                var row0 = _rows[y-1];
                var row1 = _rows[y];
                var row2 = _rows[y + 1];

                for (int x = 1; x < w - 1; x++)
                {
                    var sum = 0;
                    if (row0[x]) sum++;
                    if (row1[x - 1]) sum++;
                    if (row1[x + 0]) sum++;
                    if (row1[x + 1]) sum++;
                    if (row2[x]) sum++;

                    var need = sum > 2;

                    if (row1[x + 0])
                    {
                        if (!need)
                            toFalse.AddLast(new MyPoint(x, y));
                    }else
                    {
                        if (need)
                            toTrue.AddLast(new MyPoint(x, y));
                    }
                }
            }

            foreach (var p in toTrue)
                _rows[p.Y][p.X] = true;

            foreach (var p in toFalse)
                _rows[p.Y][p.X] = false;
        }

        private void SmoothBinarized8()
        {
            var w = _image.Width;
            var h = _image.Height;

            var toFalse = new LinkedList<MyPoint>();
            var toTrue = new LinkedList<MyPoint>();

            for (int y = 1; y < h - 1; y++)
            {
                var row0 = _rows[y - 1];
                var row1 = _rows[y];
                var row2 = _rows[y + 1];

                for (int x = 1; x < w - 1; x++)
                {
                    var sum = 0;
                    if (row0[x - 1]) sum++;
                    if (row0[x + 0]) sum++;
                    if (row0[x + 1]) sum++;
                    if (row1[x - 1]) sum++;
                    if (row1[x + 0]) sum++;
                    if (row1[x + 1]) sum++;
                    if (row2[x - 1]) sum++;
                    if (row2[x + 0]) sum++;
                    if (row2[x + 1]) sum++;

                    var need = sum > 4;

                    if (row1[x + 0])
                    {
                        if (!need)
                            toFalse.AddLast(new MyPoint(x, y));
                    }
                    else
                    {
                        if (need)
                            toTrue.AddLast(new MyPoint(x, y));
                    }
                }
            }

            foreach (var p in toTrue)
                _rows[p.Y][p.X] = true;

            foreach (var p in toFalse)
                _rows[p.Y][p.X] = false;
        }
    }
}