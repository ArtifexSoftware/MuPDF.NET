using System;
using BarcodeReader.Core.Common;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;

namespace BarcodeReader.Core.FormOMR
{
#if CORE_DEV
    public
#else
    internal
#endif
    abstract partial class FormOMR
    {
        /// <summary>
        /// This method finds template in recognized segements.
        /// And then finds unrecognized segments that are like template.
        /// This method can find partially filled, strike outed sqaures and circles.
        /// 
        /// This method is enough long!
        /// </summary>
        private void ExtendedSearch()
        {
            if (recognizedSegments.Count == 0) return;
            if (unrecognizedSegments.Count == 0) return;

            var locker = new Object();

            // find template in recognized segments
            // (template with minimum black points)
            Segment template = null;
            var minPoints = int.MaxValue;

            foreach(var seg in recognizedSegments)
            if (seg.BlackPixelCounter < minPoints)
            {
                template = seg;
                minPoints = seg.BlackPixelCounter;
            }

            //build template mask
            var blackMask = new Dictionary<MyPoint, byte>();
            var whiteMask = new Dictionary<MyPoint, byte>();
            foreach (var p in template.Points)
            {
                var pp = new MyPoint(p.X - template.Center.X, p.Y - template.Center.Y);
                blackMask[pp] = 1;
                blackMask[new MyPoint(pp.X + 1, pp.Y)] = 1;
                blackMask[new MyPoint(pp.X - 1, pp.Y)] = 1;
                blackMask[new MyPoint(pp.X, pp.Y + 1)] = 1;
                blackMask[new MyPoint(pp.X, pp.Y - 1)] = 1;
            }

            const float MAX_CENTER_OFFSET_X = 1 / 5f;
            const float MAX_CENTER_OFFSET_Y = 1 / 4f;
            const float BLACK_COEFF = 0.9f;
            const float WHITE_COEFF = 0.8f;
            var whiteD = (int)Math.Round(1 + template.Width / 20f);

            foreach (var p in template.Points)
            {
                var dx = p.X < template.Center.X ? -whiteD : whiteD;
                var dy = p.Y < template.Center.Y ? -whiteD : whiteD;
                var pp = new MyPoint(p.X - template.Center.X + dx, p.Y - template.Center.Y + dy);
                whiteMask[pp] = 1;
            }

            //find in unrecognized segments
            Parallel.For(0, unrecognizedSegments.Count, (iSeg) =>
            {
                var seg = unrecognizedSegments[iSeg];

                //filter unrecognized segments
                if (seg.BlackPixelCounter < template.BlackPixelCounter * 0.9f) goto next;
                if (seg.Width < template.Width * 0.9f) goto next;
                if (seg.Height < template.Height * 0.9f) goto next;
                if (seg.Width > template.Width * 2.5f) goto next;
                if (seg.Height > template.Height * 2.5f) goto next;

                //check different center offsets
                var dx = (int) (template.Width * MAX_CENTER_OFFSET_X);
                var dy = (int) (template.Height * MAX_CENTER_OFFSET_Y);
                var center = seg.Center;
                var bestCount = 0;
                MyPoint bestOffset = new MyPoint();

                for (var dX = -dx; dX <= dx; dX++)
                for (var dY = -dy; dY <= dy; dY++)
                {
                    //check black pixels
                    var blackCount = 0;
                    var whiteCount = template.BlackPixelCounter;
                    var offset = new MyPoint(dX - center.X, dY - center.Y);
                    foreach (var p in seg.Points)
                    {
                        var pp = new MyPoint(p.X + offset.X, p.Y + offset.Y);
                        if (blackMask.ContainsKey(pp))
                            blackCount++;
                        if (whiteMask.ContainsKey(pp))
                            whiteCount--;
                    }

                    if (blackCount < template.BlackPixelCounter * BLACK_COEFF) continue; //no black pixels
                    if (whiteCount < template.BlackPixelCounter * WHITE_COEFF) continue; //no white pixels

                    if (whiteCount > bestCount)
                    {
                        bestCount = whiteCount;
                        bestOffset = offset;
#if !DEBUG
                        goto recognized;
#endif
                    }
                }

                recognized:

                if (bestCount == 0) goto next;

#if DEBUG
                DebugHelper.DrawSquare(SKColors.Lime,
                    new MyPoint(center.X - (center.X + bestOffset.X), center.Y - (center.Y + bestOffset.Y)));
#endif
                lock (locker)
                    AddFoundBarcode(seg);

                next:;
            });
        }
    }
}