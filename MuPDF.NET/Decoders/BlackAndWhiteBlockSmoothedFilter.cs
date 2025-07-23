namespace BarcodeReader.Core
{
    /// <summary>
    /// Removes thin lines and single pixels.
    /// Use it for dotted DPM datamatrix.
    /// </summary>
    class BlackAndWhiteBlockSmoothedFilter : BlackAndWhiteBlockFilter
    {
        private int w;
        private int h;

        public int SmoothRadius { get; set; } = 6;     //6
        public float Sensitivity { get; set; } = 1.1f; //1.1

        public BlackAndWhiteBlockSmoothedFilter(IPreparedImage image, int thresholdLevelAdjustment) : base(image, thresholdLevelAdjustment)
        {
        }

        protected override void binarizeEntireImage()
        {
            base.binarizeEntireImage();
            Smooth();
        }

        private void Smooth()
        {
            w = _image.Width;
            h = _image.Height;

            var integral = IntegralBuilder.Build(this, w, h);
            var size = SmoothRadius;//5/6
            var lim0 = size * size / 120f;
            var lim1 = Sensitivity * size * size;//1.1//1.1
            var step = 1;

            //find corners
            for (int y = size; y < h - size; y += step)
                for (int x = size; x < w - size; x += step)
                {
                    var pixel = IntegralBuilder.GetSum(integral, x, y, 1, 1);
                    if (pixel > 0)
                        continue;

                    //  s0 s1
                    //  s2 s3
                    var s0 = IntegralBuilder.GetSum(integral, x - size, y - size, size, size);
                    var s1 = IntegralBuilder.GetSum(integral, x, y - size, size, size);
                    var s2 = IntegralBuilder.GetSum(integral, x - size, y, size, size);
                    var s3 = IntegralBuilder.GetSum(integral, x, y, size, size);

                    if (s0 + s1 + s2 + s3 < lim1) continue;

                    //if (s0 > lim0 && s1 > lim0 && s2 > lim0 && s3 > lim0) continue;
                    //if (s0 < lim0 || s1 < lim0 || s2 < lim0 || s3 < lim0) continue;

                    FillCircle(x, y, size / 4);

                    //DebugHelper.FillInNoScaled(Color.Red, 2/* + size / 2*/, new MyPoint(x, y));

                    //DebugHelper.DrawSquare(x, y, Color.Red);
                }
        }

        void FillCircle(int x, int y, int radius)
        {
            var r2 = radius * radius;

            for(int i = -radius; i <= radius; i++)
            for (int j = -radius; j <= radius; j++)
            {
                if (i * i + j * j > r2) continue;
                FillSafe(x + i, y + j);
            }
        }

        void FillSafe(int x, int y)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            _rows[y][x] = true;
        }
    }
}