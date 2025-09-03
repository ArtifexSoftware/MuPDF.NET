using System;
using System.Collections.Generic;
using System.Diagnostics;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core
{
    /// <summary>
    /// Applies special "grid" filter to binarized image.
    /// Use it to remove thin single lines and single pixels from image.
    /// </summary>
    class BlackAndWhiteBlockGridFilter : BlackAndWhiteBlockFilter
    {
        public int SmoothRadius { get; set; } = 6;     //5
        public float Sensitivity { get; set; } = 0.3f; //0.4

        public BlackAndWhiteBlockGridFilter(IPreparedImage image, int thresholdLevelAdjustment) : base(image, thresholdLevelAdjustment)
        {
        }

        protected override void binarizeEntireImage()
        {
            base.binarizeEntireImage();
            Smooth();
        }

        private void Smooth()
        {
            var w = _image.Width;
            var h = _image.Height;

            //build integral image
            var integral = IntegralBuilder.Build(this, w, h);

            //parameters
            var r = SmoothRadius;
            var size = 2 * r + 1;
            var lim1 = Sensitivity * size * size;
            var step = 1;

            //scan pixels
            Parallel.For(r, h - r, (y) =>
            {
                var row = GetRow(y);
                for (int x = r; x < w - r; x += step)
                {
                    if (!row[x]) continue;//white pixel

                    //get sum of pixels around
                    var sum = IntegralBuilder.GetSum(integral, x - r, y - r, size, size);
                    if (sum <= lim1)
                    {
                        //thin line or single pixel -> erase pixel
                        row[x] = false;
                    };
                }
            }
            );
        }
    }
}