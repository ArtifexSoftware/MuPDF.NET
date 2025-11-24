using System;
using System.Collections.Generic;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace MuPDF.NET
{
    /// <summary>
    /// New "Projection" deskew method
    /// </summary>
    internal unsafe class Deskew
    {
        public static void Process(ref SKBitmap inputImage, double minAngle)
        {
            var angle = ProjectionCalculator.FindRotateAngle(inputImage);
            angle = (angle * 180 / Math.PI); // to degrees

            if (Math.Abs(angle) > minAngle)
            {
                var rotated = Rotate(inputImage, angle);

                inputImage.Dispose();
                inputImage = rotated;
            }
        }

        private static SKBitmap Rotate(SKBitmap img, double angle, SKFilterQuality quality = SKFilterQuality.Medium)
        {
            var rotated = new SKBitmap(img.Width, img.Height);
            using (var surface = SKSurface.Create(new SKImageInfo(img.Width, img.Height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);
                canvas.Translate(img.Width / 2f, img.Height / 2f);
                canvas.RotateDegrees(-(float)angle);
                canvas.Translate(-img.Width / 2f, -img.Height / 2f);

                using (var paint = new SKPaint { FilterQuality = quality })
                {
                    canvas.DrawBitmap(img, 0, 0, paint);
                }

                using (var image = surface.Snapshot())
                {
                    image.Encode(SKEncodedImageFormat.Png, 100).AsStream().Position = 0;
                    rotated = SKBitmap.Decode(image.Encode(SKEncodedImageFormat.Png, 100));
                }
            }

            return rotated;
        }

        private static unsafe class ProjectionCalculator
        {
            /// <summary>
            /// Calc angle of skewing
            /// </summary>
            /// <param name="bmp">Bitmap</param>
            /// <param name="precision">Precision - from 0 to 1 (recommended 0.2)</param>
            /// <param name="maxSkewAngle">Max skewing angle in degrees</param>
            /// <param name="isBinarized">True if image already was binarized</param>
            /// <returns>Skewing angle in radians</returns>
            public static double FindRotateAngle(SKBitmap bmp, float precision = 0.2f, int maxSkewAngle = 5, bool isBinarized = false)
            {
                using (var wr = new UnmanagedImage(bmp))
                {
                    //calc threshold level (to binarize)
                    var treshold = CalcThreshold(wr, isBinarized);

                    //calc projection histogram (HOG)
                    var dX = 3 + (int)(precision * bmp.Width / 4); //length of ray
                    var dY = (int)(Math.Tan(maxSkewAngle * Math.PI / 180) * dX); //max DY of ray

                    var hist = CalcHistogram(wr, treshold, dX, dY);

                    //find max of histogram
                    return FindMaxOnHistogram(hist, dY, dX);
                }
            }

            private static double FindMaxOnHistogram(int[] hist, int dY, int dX)
            {
                var maxVal = 0;
                int bestIndex = -1;
                for (int i = 0; i < hist.Length; i++)
                    if (hist[i] > maxVal)
                    {
                        maxVal = hist[i];
                        bestIndex = i;
                    }

                //
                float dy = bestIndex - dY;

                //calc subpixel precision
                if (bestIndex > 0 && bestIndex < hist.Length - 2)
                {
                    float x0 = bestIndex - 1;
                    float x1 = bestIndex + 0;
                    float x2 = bestIndex + 1;
                    float v0 = hist[(int)x0];
                    float v1 = hist[(int)x1];
                    float v2 = hist[(int)x2];

                    //Lagrange quadratic interpolation
                    var max = 0f;

                    for (int dI = -20; dI <= 20; dI++)
                    {
                        var x = x1 + dI / 20f;
                        var y = v0 * (x - x1) * (x - x2) / 2f - v1 * (x - x0) * (x - x2) + v2 * (x - x0) * (x - x1) / 2;
                        if (y > max)
                        {
                            max = y;
                            dy = x - dY;
                        }
                    }
                }

                //calc angle
                var a = Math.Atan2(dy, dX);
                return a;
            }

            private static int[] CalcHistogram(UnmanagedImage wr, int treshold, int dX, int dY)
            {
                var w = wr.Width;
                var h = wr.Height;
                var step = wr.Step;
                var stride = wr.Stride;

                var hist = new int[1 + dY * 2];

                Parallel.For(0, h, (y) =>
                //for (int y = 0; y < h; y++)
                {
                    var ptr = wr.StartGreen + y * wr.Stride;

                    for (int x = 0; x < w; x++)
                    {
                        var g = ptr[x * step];

                        if (g < treshold)
                        {
                            var X = x + dX;
                            if (X >= w)
                                continue;
                            var ptr2 = ptr + X * step;

                            var fromY = Math.Max(0, y - dY) - y;
                            var toY = Math.Min(h - 1, y + dY) - y;
                            for (int i = fromY; i <= toY; i++)
                            {
                                g = ptr2[i * stride];
                                if (g < treshold)
                                    hist[i + dY]++;
                            }
                        }
                    }
                });

                return hist;
            }

            private static int CalcThreshold(UnmanagedImage wr, bool isBinarized)
            {
                //min, max, avg
                long min = 255, max = 0, avg = 0;
                if (!isBinarized)
                {
                    var w = wr.Width;
                    var h = wr.Height;
                    var step = wr.Step;

                    for (int y = 0; y < h; y++)
                    {
                        var ptr = wr.StartGreen + y * wr.Stride;

                        for (int x = 0; x < w; x++)
                        {
                            var g = ptr[x * step];
                            if (g < min) min = g;
                            if (g > max) max = g;
                            avg += g;
                        }
                    }

                    avg /= w * h;
                }
                else
                {
                    min = 0;
                    max = 255;
                    avg = 127;
                }

                //calc threshold
                var thres = (int)((avg + min) * 0.9f);

                return thres;
            }
        }
    }


    public unsafe class LineRemover
    {
        private const float MAX_LINE_WIDTH = 180;//280
        private const float MIN_LINE = 45;
        private const float MIN_LINE_VERT = 50;//is not used
        private const float LINE_BRIGHTNESS = 0.1f;//0.15
        private const float NO_LINE_BRIGHTNESS = 0.6f;
        private const float SENSITIVITY = 0.7f;//0.7//1
        private const float CONST5 = 0.6f;
        private const float BROKENNESS_TOLERANCE = 0.02f;//0.04

        public static void Process(ref SKBitmap bitmap, bool vertical)
        {
            using (var unmanagedImage = new UnmanagedImage(bitmap))
            {
                var threshold = CalcThreshold(unmanagedImage, false);
                var lines = FindLines(unmanagedImage, threshold, vertical);
                RemoveLinesAdvanced(unmanagedImage, lines);
            }
        }

        public static void CollectLines(ref SKBitmap bitmap, ref List<Segment> horzLines, ref List<Segment> vertLines)
        {
            using (var unmanagedImage = new UnmanagedImage(bitmap))
            {
                var threshold = CalcThreshold(unmanagedImage, false);
                horzLines?.AddRange(FindLines(unmanagedImage, threshold, false));
                vertLines?.AddRange(FindLines(unmanagedImage, threshold, true));
            }
        }

        /// <summary>
        /// Calc threshold level.
        /// This value will be used to find line on the image
        /// </summary>
        /// <param name="isBinarized">True if image already binarized</param>
        /// <returns></returns>
        private static int CalcThreshold(UnmanagedImage wr, bool isBinarized, float sensitivity = SENSITIVITY)
        {
            //min, max, avg
            long min = 255, max = 0, avg = 0;
            if (!isBinarized)
            {
                var w = wr.Width;
                var h = wr.Height;
                var step = wr.Step;

                for (int y = 0; y < h; y++)
                {
                    var ptr = wr.StartGreen + y * wr.Stride;

                    for (int x = 0; x < w; x++)
                    {
                        var g = ptr[x * step];
                        if (g < min) min = g;
                        if (g > max) max = g;
                        avg += g;
                    }
                }

                avg /= w * h;
            }
            else
            {
                min = 0;
                max = 255;
                avg = 127;
            }

            //calc threshold
            var thres = (int)((avg + max * sensitivity) / (1 + sensitivity));

            return thres;
        }

        /// <summary>
        /// Find lines
        /// </summary>
        private static List<Segment> FindLines(UnmanagedImage wr, int threshold, bool byVert, int maxLineWidth = -1)
        {
            if (maxLineWidth < 0)
                maxLineWidth = (int)(1 + Math.Max(wr.Width, wr.Height) / MAX_LINE_WIDTH);

            var res = new List<Segment>();

            int w = wr.Width, h = wr.Height;

            if (byVert)
            {
                w = wr.Height;
                h = wr.Width;
            }

            //calc integral image by horiz/vert
            int[][] integral;
            if (byVert)
                integral = CalcIntegralByCols(wr, threshold);
            else
                integral = CalcIntegralByRows(wr, threshold);

            //find lines
            //var minLineLength = (int)(5 + w / (byVert ? MIN_LINE_VERT : MIN_LINE));
            var minLineLength = (int)(5 + Math.Max(w, h) / MIN_LINE);

            if (minLineLength < 25)
                minLineLength = 25;

            var th1 = LINE_BRIGHTNESS * minLineLength;
            var th2 = NO_LINE_BRIGHTNESS * minLineLength;

            Parallel.For(maxLineWidth, h - maxLineWidth, (y) =>
            {
                var row0 = integral[y - maxLineWidth];
                var row1 = integral[y];
                var row2 = integral[y + maxLineWidth];

                for (int x = 0; x < w - minLineLength; x++)
                {
                    var sum0 = row0[x + minLineLength] - row0[x];//row above
                    var sum1 = row1[x + minLineLength] - row1[x];//the row
                    var sum2 = row2[x + minLineLength] - row2[x];//row below

                    if (sum1 <= th1)
                        if (sum0 >= th2 || sum2 >= th2)
                        {
                            //start scan line
                            var fromX = FirstBlack(row1, x);
                            int toX = -1;
                            if (fromX == -1)
                                continue;
                            for (x = fromX; x < w - minLineLength; x++)
                            {
                                var sum = row1[x + minLineLength] - row1[x];
                                if (sum > th1)
                                    break;
                                if (row1[x + minLineLength + 1] - row1[x + minLineLength] == 0)//is black
                                    toX = x + minLineLength;
                            }

                            if (toX >= 0)
                            {
                                //we found start X and stop X of line
                                var segment = new Segment() { From = new SKPointI(fromX, y), To = new SKPointI(toX, y) };
                                //clac width
                                CalcLineWidth(integral, segment, maxLineWidth);

                                if (segment.Width <= 0)//it is not line
                                    continue;
                                if (segment.Width > maxLineWidth)
                                    continue;//too thick

                                if (segment.Length / segment.Width < 8)
                                    continue;//it is rectangle, not line

                                if (segment.Width > maxLineWidth / 3)
                                {
                                    var disp = CalcWhitePercent(integral, segment);
                                    if (disp > BROKENNESS_TOLERANCE + segment.Length / 15000)
                                        continue;//too dotted line (may be it is word with small font)
                                }

                                //add to segment list
                                if (byVert)
                                    segment.SwapXY();
                                res.Add(segment);
                            }
                        }
                }
            });

            return res;
        }

        private static double CalcWhitePercent(int[][] integral, Segment segment)
        {
            var sum = integral[segment.To.Y][segment.To.X + 1] - integral[segment.From.Y][segment.From.X];
            return sum / segment.Length;
        }

        /// <summary>
        /// Remove lines from image (simple algo)
        /// </summary>
        private static void RemoveLines(SKBitmap bmp, List<Segment> lines, SKColor? color = null)
        {
            var white = color == null ? SKColors.White : color.Value;
            using (var surface = SKSurface.Create(new SKImageInfo(bmp.Width, bmp.Height)))
            {
                var canvas = surface.Canvas;
                canvas.DrawBitmap(bmp, 0, 0);

                foreach (var line in lines)
                {
                    using (var paint = new SKPaint
                    {
                        Color = white,
                        StrokeWidth = line.Width + 2,
                        IsAntialias = true,
                        Style = SKPaintStyle.Stroke
                    })
                    {
                        canvas.DrawLine(line.CorrectedFrom.X, line.CorrectedFrom.Y, line.CorrectedTo.X, line.CorrectedTo.Y, paint);
                    }
                }

                using (var image = surface.Snapshot())
                {
                    var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    bmp = SKBitmap.Decode(data);
                }
            }
        }

        /// <summary>
        /// Remove lines from image (preserve letters)
        /// </summary>
        private static void RemoveLinesAdvanced(UnmanagedImage wr, List<Segment> lines, SKColor? color = null, bool removeHoriz = true, bool removeVert = true)
        {
            var maxLineWidth = (int)(1 + Math.Max(wr.Width, wr.Height) / 280);

            var white = color == null ? SKColors.White : color.Value;
            var w = wr.Width;
            var h = wr.Height;
            var D = maxLineWidth / 2f;

            var points = new List<SKPointI>();
            var rect = new SKRect(D + 2, D + 2, w - D - 3, h - D - 3);

            //remove horiz
            foreach (var line in lines)
                if (line != null)
                {
                    var d = line.Width / 2;
                    //if (!rect.Contains(line.CorrectedFrom.X, line.CorrectedFrom.Y)) continue;
                    //if (!rect.Contains(line.CorrectedTo.X, line.CorrectedTo.Y)) continue;

                    var dY = Math.Abs(line.CorrectedTo.Y - line.CorrectedFrom.Y);
                    var dX = Math.Abs(line.CorrectedTo.X - line.CorrectedFrom.X);
                    if (dX > dY)
                    {
                        if (removeHoriz)
                            RemoveLineAdvancedHoriz(wr, line, points, white, d);
                    }
                    else
                    {
                        if (removeVert)
                            RemoveLineAdvancedVert(wr, line, points, white, d);
                    }
                }
        }

        private static void RemovePoints(UnmanagedImage wr, List<SKPointI> pointsToDel, SKColor white)
        {
            //remove points
            var step = wr.Step;
            var stride = wr.Stride;
            foreach (var p in pointsToDel)
            {
                var ptr = wr.Start + p.X * step + p.Y * stride;
                switch (step)
                {
                    case 1: ptr[0] = white.Green; break;
                    case 3: ptr[0] = white.Blue; ptr[1] = white.Green; ptr[2] = white.Red; break;
                    case 4: ptr[0] = white.Blue; ptr[1] = white.Green; ptr[2] = white.Red; ptr[3] = white.Alpha; break;
                }
            }
        }

        private static void RemoveLineAdvancedHoriz(UnmanagedImage wr, Segment line, List<SKPointI> points, SKColor white, float d)
        {
            points.Clear();
            var fromX = line.CorrectedFrom.X;
            var toX = line.CorrectedTo.X;
            var length = toX - fromX;
            var toDel = 0;
            var step = wr.Step;
            var stride = wr.Stride;

            if (length < 1) return;

            for (float x = fromX; x <= toX; x++)
            {
                var Y = Lerp(line.CorrectedFrom.Y, line.CorrectedTo.Y, (x - fromX) / length);
                var sum1 = 0;
                var sum2 = 0;
                var sum3 = 0;
                var ptr = wr.StartGreen + (int)(x * step);

                // added fixes for issue #2598
                if ((Y - d) < 2.0f)
                {
                    Y = d + 2.0f;
                }

                // added fixes for issue #2600
                if ((ptr + ((int)(Y + d) + 2) * stride) >= wr.MaxPtr)
                {
                    continue;
                }

                var y = (int)(Y - d);
                sum1 = (ptr[(y - 1) * stride] + ptr[(y - 2) * stride]) / 2;
                y = (int)(Y + d);
                sum3 = (ptr[(y + 1) * stride] + ptr[(y + 2) * stride]) / 2;

                var count = 0;
                for (y = (int)(Y - d); y <= Y + d; y++)
                {
                    var g = ptr[(int)(y) * stride];
                    sum2 += g;
                    count++;
                }

                sum2 /= count;

                if (sum1 <= sum2 * 0.9f)
                    continue; //disbalance - no line
                if (sum3 <= sum2 * 0.9f)
                    continue; //disbalance - no line
                if (sum1 < 127 || sum3 < 127)
                    continue; //too dark sides - no line

                //it is line - remove
                var fromY = Y - d - 2;
                var toY = Y + d + 2;
                for (float yy = fromY; yy <= toY; yy++)
                    points.Add(new SKPointI((int)x, (int)yy));

                toDel++;
            }

            if (toDel < (toX - fromX) * 0.85f)
                points.Clear();

            RemovePoints(wr, points, white);
        }

        private static void RemoveLineAdvancedVert(UnmanagedImage wr, Segment line, List<SKPointI> points, SKColor white, float d)
        {
            points.Clear();
            var fromY = line.CorrectedFrom.Y;
            var toY = line.CorrectedTo.Y;
            var length = toY - fromY;
            var toDel = 0;
            var step = wr.Step;
            var stride = wr.Stride;

            if (length < 2) return;

            for (float y = fromY; y <= toY; y++)
            {
                var X = Lerp(line.CorrectedFrom.X, line.CorrectedTo.X, (y - fromY) / length);
                var sum1 = 0;
                var sum2 = 0;
                var sum3 = 0;
                var ptr = wr.StartGreen + (int)(y * stride);

                var x = (int)(X - d);
                sum1 = (ptr[(x - 1) * step] + ptr[(x - 2) * step]) / 2;
                x = (int)(X + d);
                sum3 = (ptr[(x + 1) * step] + ptr[(x + 2) * step]) / 2;

                var count = 0;
                for (x = (int)(X - d); x <= X + d; x++)
                {
                    var g = ptr[(int)(x) * step];
                    sum2 += g;
                    count++;
                }

                sum2 /= count;

                if (sum1 <= sum2 * 0.9f)
                    continue; //disbalance - no line
                if (sum3 <= sum2 * 0.9f)
                    continue; //disbalance - no line
                if (sum1 < 127 || sum3 < 127)
                    continue; //too dark sides - no line

                //it is line - remove
                var fromX = X - d - 2;
                var toX = X + d + 2;
                for (float xx = fromX; xx <= toX; xx++)
                    points.Add(new SKPointI((int)xx, (int)y));

                toDel++;
            }

            if (toDel < (toY - fromY) * 0.85f)
                points.Clear();

            RemovePoints(wr, points, white);
        }

        static float Lerp(float from, float to, float k)
        {
            return @from * (1 - k) + to * k;
        }

        private static void CalcLineWidth(int[][] intByRows, Segment segment, int MAX_LINE_WIDTH)
        {
            var y = segment.From.Y;
            var th = CONST5 * (segment.To.X - segment.From.X);
            var th2 = 0.70f;//0.85

            //build histogram
            var hist = new float[MAX_LINE_WIDTH * 2 + 1];
            for (int dy = -MAX_LINE_WIDTH; dy <= MAX_LINE_WIDTH; dy++)
            {
                hist[MAX_LINE_WIDTH + dy] = intByRows[y + dy][segment.To.X + 1] - intByRows[y + dy][segment.From.X];
            }

            //normalize histogram
            var min = float.MaxValue;
            var max = float.MinValue;
            for (int i = 0; i < hist.Length; i++)
            {
                if (hist[i] < min) min = hist[i];
                if (hist[i] > max) max = hist[i];
            }

            if ((max - min) < th)
            {
                segment.Width = -1;
                return;
            }

            for (int i = 0; i < hist.Length; i++)
                hist[i] = (hist[i] - min) / (max - min);

            //find top white line
            var center = hist[MAX_LINE_WIDTH];
            var top = 0;
            for (int dy = -1; dy >= -MAX_LINE_WIDTH; dy--)
            {
                if (hist[MAX_LINE_WIDTH + dy] - center > th2)
                {
                    top = dy;
                    break;
                }
            }

            //find bottom white line
            var bottom = 0;
            for (int dy = 1; dy <= MAX_LINE_WIDTH; dy++)
            {
                if (hist[MAX_LINE_WIDTH + dy] - center > th2)
                {
                    bottom = dy;
                    break;
                }
            }

            if (top == 0 || bottom == 0)
                segment.Width = -1; //no white line on on top or bottom
            else
            {
                segment.Width = (bottom - top) - 1;
                var correctedY = segment.From.Y + (bottom + top) / 2f;
                segment.CorrectedFrom = new SKPoint(segment.From.X, correctedY);
                segment.CorrectedTo = new SKPoint(segment.To.X, correctedY);
            }
        }

        private static int FirstBlack(int[] integralRow, int startIndex)
        {
            if (startIndex <= 0)
                startIndex = 1;

            for (int i = startIndex; i < integralRow.Length; i++)
            {
                if (integralRow[i] - integralRow[i - 1] == 0)
                    return i - 1;
            }

            return -1;
        }

        private static int[][] CalcIntegralByRows(UnmanagedImage wr, int treshold)
        {
            var w = wr.Width;
            var h = wr.Height;
            var step = wr.Step;

            var rows = new int[wr.Height][];

            Parallel.For(0, h, (y) =>
            {
                var row = rows[y] = new int[w + 1];
                var sum = 0;
                var ptr = wr.StartGreen + y * wr.Stride;

                for (int x = 0; x < w; x++)
                {
                    var g = ptr[x * step] < treshold ? 0 : 1;
                    sum += g;
                    row[x + 1] = sum;
                }
            }
            );

            return rows;
        }

        private static int[][] CalcIntegralByCols(UnmanagedImage wr, int treshold)
        {
            var w = wr.Width;
            var h = wr.Height;
            var step = wr.Step;
            var stride = wr.Stride;

            var cols = new int[wr.Width][];

            Parallel.For(0, w, (x) =>
            {
                var col = cols[x] = new int[h + 1];
                var sum = 0;
                var ptr = wr.StartGreen + x * step;

                for (int y = 0; y < h; y++)
                {
                    var g = ptr[y * stride] < treshold ? 0 : 1;
                    sum += g;
                    col[y + 1] = sum;
                }
            }
            );

            return cols;
        }

        /// <summary>
        /// Joins neighbor segments
        /// </summary>
        internal static List<Segment> JoinSegments(List<Segment> segments)
        {
            var temp = new List<Segment>();

            //horiz
            foreach (var s in segments)
                if (s != null && !s.IsVertical)
                    temp.Add(s);
            var res = JoinSegmentsHoriz(temp);

            //vert
            temp.Clear();
            foreach (var s in segments)
                if (s != null && s.IsVertical)
                {
                    s.SwapXY();
                    temp.Add(s);
                }

            temp = JoinSegmentsHoriz(temp);

            foreach (var s in temp)
                s.SwapXY();

            res.AddRange(temp);

            //
            return res;
        }

        private static List<Segment> JoinSegmentsHoriz(List<Segment> segments)
        {
            //build hash by Y
            var hashByY = new List<List<Segment>>();
            foreach (var s in segments)
            {
                var y = (int)Math.Round(s.CorrectedFrom.Y);
                while (hashByY.Count < y + 1)
                    hashByY.Add(null);

                if (hashByY[y] == null)
                    hashByY[y] = new List<Segment>();

                hashByY[y].Add(s);
            }

            //sort by X
            foreach (var list in hashByY)
                if (list != null)
                    list.Sort((s1, s2) => s1.CorrectedFrom.X.CompareTo(s2.CorrectedFrom.X));

            //join horizontally
            var maxD = 10;
            foreach (var list in hashByY)
                if (list != null && list.Count > 1)
                {
                    for (int i = 1; i < list.Count; i++)
                    {
                        var s1 = list[i - 1];
                        var s2 = list[i];
                        if (s2.From.X - s1.To.X <= maxD)
                        {
                            JoinSegments(s1, s2);
                            list.RemoveAt(i);
                            i--;
                        }
                    }
                }

            //join vertically (cluster analysis)
            var segToCluster = new Dictionary<Segment, int>();

            foreach (var list in hashByY)
                if (list != null)
                    foreach (var s in list)
                        segToCluster[s] = s.GetHashCode();

            for (int i = 1; i < hashByY.Count; i++)
            {
                var list1 = hashByY[i - 1];
                var list2 = hashByY[i];
                if (list1 != null && list2 != null)
                {
                    foreach (var s1 in list2)
                        foreach (var s2 in list1)
                        {
                            //intersect?
                            if (AreIntersectedHoriz(s1, s2))
                            {
                                var cl1 = segToCluster[s1];
                                var cl2 = segToCluster[s2];
                                if (cl1 < cl2)
                                    ChangeCluster(segToCluster, cl2, cl1);
                                else
                                    ChangeCluster(segToCluster, cl1, cl2);
                            }
                        }
                }
            }

            //get clusters
            var clusters = new Dictionary<int, Segment>();
            foreach (var pair in segToCluster)
            {
                Segment s = null;
                if (!clusters.TryGetValue(pair.Value, out s))
                    clusters[pair.Value] = pair.Key;
                else
                    JoinSegments(s, pair.Key);
            }

            return new List<Segment>(clusters.Values);
        }

        private static void ChangeCluster(Dictionary<Segment, int> segToCluster, int from, int to)
        {
            var temp = new List<KeyValuePair<Segment, int>>();
            foreach (var pair in segToCluster)
                if (pair.Value == from)
                    temp.Add(pair);

            foreach (var pair in temp)
                segToCluster[pair.Key] = to;
        }

        private static bool AreIntersectedHoriz(Segment s1, Segment s2)
        {
            if (s1.To.X < s2.From.X || s1.From.X > s2.To.X) return false;
            return true;
        }

        private static void JoinSegments(Segment s1, Segment s2)
        {
            if (s1.From.X > s2.From.X)
            {
                s1.From = s2.From;
                s1.CorrectedFrom = s2.CorrectedFrom;
            }
            if (s1.To.X < s2.To.X)
            {
                s1.To = s2.To;
                s1.CorrectedTo = s2.CorrectedTo;
            }

            s1.Width = (s1.Width * s1.Length + s2.Width * s2.Length) / (s1.Length + s2.Length);
        }

        public class Segment
        {
            public SKPointI From { get; internal set; }
            public SKPointI To { get; internal set; }
            public SKPoint CorrectedFrom { get; internal set; }
            public SKPoint CorrectedTo { get; internal set; }
            public float Width { get; internal set; }

            public bool IsVertical
            {
                get
                {
                    var dY = Math.Abs(CorrectedTo.Y - CorrectedFrom.Y);
                    var dX = Math.Abs(CorrectedTo.X - CorrectedFrom.X);
                    return dX < dY;
                }
            }

            public float Length
            {
                get
                {
                    var dx = From.X - To.X;
                    var dy = From.Y - To.Y;
                    return (float)Math.Sqrt(dx * dx + dy * dy);
                }
            }

            internal void SwapXY()
            {
                From = new SKPointI(From.Y, From.X);
                To = new SKPointI(To.Y, To.X);
                CorrectedFrom = new SKPoint(CorrectedFrom.Y, CorrectedFrom.X);
                CorrectedTo = new SKPoint(CorrectedTo.Y, CorrectedTo.X);
            }
#if DEBUG
            public override string ToString()
            {
                return $"CorrectedFrom: {CorrectedFrom}, CorrectedTo: {CorrectedTo}";
            }
#endif
        }
    }


    /// <summary>
	/// TiltModeler estimates tilt in input image and rotates it.
	/// OBSOLETE! Use `Deskew` class instead.
	/// </summary>
	[Obsolete]
    internal class TiltModeler
    {
        // models and corrects any tilt in input image
        public static void Process(ref SKBitmap inpImage, double minAngle)
        {
            List<SKPoint> points = new List<SKPoint>();

            var bytesPerPixel = inpImage.BytesPerPixel;
            var stride = inpImage.RowBytes;
            var width = inpImage.Width;
            var height = inpImage.Height;

            /* the size of the image in Bytes */
            int size = stride * height;

            /* Allocate buffer for image */
            byte[] inData = new byte[size];
            Marshal.Copy(inpImage.GetPixels(), inData, 0, size);

            for (int i = 0; i < height; i += 8)
            {
                int imgLocation = i * stride;
                for (int j = 0; j < width / 5; j++) //max left margin 1/5th
                {
                    if (inData[imgLocation] < 32)
                    {
                        points.Add(new SKPoint(j, i));
                        break;
                    }
                    imgLocation += bytesPerPixel;
                }
            }

            double m, c;
            double rSqr = Ransac(points, out m, out c);
            float slope = (float)Math.Round(Math.Atan(m) * 180 / Math.PI, 2);

            if (Math.Abs(slope) > minAngle)
            {
                SKBitmap rotImage = Transform.RotateImage(inpImage, slope);
                inpImage.Dispose();
                inpImage = rotImage;
            }
        }

        // Ransac based tilt estimation from input image
        private static double Ransac(List<SKPoint> points, out double optM, out double optC)
        {
            optM = 0;
            optC = 0;
            int n = points.Count;
            int sampleSize = n / 5;
            sampleSize = 5;
            if (n < 10) return (99999);
            Random rand = new Random();
            List<SKPoint> subset = new List<SKPoint>();
            double minErr = 9999999;

            for (int iter = 0; iter < 10000; iter++)
            {
                for (int j = 0; j < sampleSize; j++)
                {
                    int p = (int)(rand.NextDouble() * n);
                    subset.Add(points[p]);
                }
                double m, c;
                LeastSquare(subset, out m, out c);
                double rSqr = Evalute(points, m, c);
                subset.Clear();
                if (rSqr < minErr)
                {
                    minErr = rSqr;
                    optM = m;
                    optC = c;
                }
            }
            return minErr;
        }

        //evaluated linear model
        private static double Evalute(List<SKPoint> points, double m, double c)
        {
            double rSqr = 0;
            List<double> errors = new List<double>();
            for (int i = 0; i < points.Count; i++)
            {
                double x = points[i].Y;
                double y = points[i].X;

                double yp = m * x + c;
                double err = yp - y;

                err *= err;
                errors.Add(err);
            }
            errors.Sort();
            rSqr = Math.Sqrt(errors[errors.Count / 4]);
            return rSqr;
        }

        // Least square estimation of tilt parameters
        private static void LeastSquare(List<SKPoint> points, out double m, out double c)
        {
            double sx = 0;
            double sy = 0;
            double sxy = 0;
            double syy = 0;
            double sxx = 0;

            for (int i = 0; i < points.Count; i++)
            {
                double x = points[i].Y;
                double y = points[i].X;

                sx += x;
                sy += y;
                sxy += (x * y);
                syy += (y * y);
                sxx += (x * x);
            }

            m = (points.Count * sxy - sx * sy) / (points.Count * sxx - sx * sx);
            c = (sy - m * sx) / points.Count;
        }
    }

    /// <summary>
    /// Corrects broken letters.
    /// </summary>
    internal class Dilate
    {
        static int THR = 127;
        static int DILATE_FILTER_X_SIZE = 3;
        static int DILATE_FILTER_Y_SIZE = 3;

        public static void Process(ref SKBitmap bitmap)
        {
            int winX = DILATE_FILTER_X_SIZE;
            int winY = DILATE_FILTER_Y_SIZE;
            int halfX = winX / 2;
            int halfY = winY / 2;
            int halfXplus1 = halfX + 1;
            int halfYplus1 = halfY + 1;

            int i1 = bitmap.Height - halfY;
            int j1 = bitmap.Width - halfX;

            SKBitmap destImage = bitmap.Copy();

            int origBytesPerPixel = bitmap.BytesPerPixel;
            int nChannels = origBytesPerPixel;
            if (nChannels == 4) nChannels--;

            /* the size of the image in Bytes */
            int stride = bitmap.RowBytes;
            int size = stride * bitmap.Height;

            /* Allocate buffer for image */
            byte[] inData = new byte[size];
            byte[] destData = new byte[size];

            Marshal.Copy(bitmap.GetPixels(), inData, 0, size);
            Marshal.Copy(destImage.GetPixels(), destData, 0, size);

            // Get the address of the first line.
            for (int i = halfY; i < i1; i++)
            {
                int imgLocation = i * stride + halfX * origBytesPerPixel;

                for (int j = halfX; j < j1; j++)
                {
                    byte val = 0;

                    if (inData[imgLocation] < THR || inData[imgLocation - origBytesPerPixel] < THR || inData[imgLocation + origBytesPerPixel] < THR)
                    {
                        val = Math.Min(inData[imgLocation], inData[imgLocation - origBytesPerPixel]);
                        val = Math.Min(val, inData[imgLocation + origBytesPerPixel]);
                    }
                    else
                    {
                        val = Math.Max(inData[imgLocation], inData[imgLocation - origBytesPerPixel]);
                        val = Math.Max(val, inData[imgLocation + origBytesPerPixel]);
                    }
                    for (int b = 0; b < nChannels; b++)
                    {
                        destData[imgLocation + b] = val;
                    }
                    imgLocation += origBytesPerPixel;
                }
            }

            // Get the address of the first line.
            for (int i = halfY; i < i1; i++)
            {
                int imgLocation = i * stride + halfX * origBytesPerPixel;
                for (int j = halfX; j < j1; j++)
                {
                    byte val = 0;
                    if (inData[imgLocation] < THR && (inData[imgLocation - origBytesPerPixel] > THR || inData[imgLocation + origBytesPerPixel] > THR))
                    {
                        for (int b = 0; b < nChannels; b++)
                        {
                            destData[imgLocation + b] = val;
                        }
                    }
                    imgLocation += origBytesPerPixel;
                }
            }

            unsafe
            {
                fixed (byte* ptr = destData)
                {
                    destImage.SetPixels((IntPtr)ptr);
                }
            }

            bitmap.Dispose();
            bitmap = destImage;
        }
    }


    /// <summary>
    /// Obsolete. Use `LineRemover` instead.
    /// </summary>
    [Obsolete]
    internal class VerticalLineRemover
    {
        static int VERTICAL_LINE_FILTER_LENGTH = 101;

        public static void Process(ref SKBitmap bitmap)
        {
            int winX = 3;
            int winY = VERTICAL_LINE_FILTER_LENGTH;
            int halfX = winX / 2;
            int halfY = winY / 2;
            int halfXplus1 = halfX + 1;
            int halfYplus1 = halfY + 1;

            int i1 = bitmap.Height - halfY;
            int j1 = bitmap.Width - halfX;

            SKBitmap destImage = bitmap.Copy();

            int origBytesPerPixel = bitmap.BytesPerPixel;
            int destBytesPerPixel = destImage.BytesPerPixel;

            int oChannels = destBytesPerPixel;
            if (oChannels == 4) oChannels--;

            /* the size of the image in bytes */
            int inStride = bitmap.RowBytes;
            int destStride = destImage.RowBytes;
            int inSize = inStride * bitmap.Height;
            int destSize = destStride * destImage.Height;

            /* Allocate buffer for image */
            byte[] inData = new byte[inSize];
            byte[] destData = new byte[destSize];

            Marshal.Copy(bitmap.GetPixels(), inData, 0, inSize);
            Marshal.Copy(destImage.GetPixels(), destData, 0, destSize);

            // First line processing
            int[] filterData = new int[bitmap.Width];

            for (int j = halfX; j < j1; j++)
            {
                int v = 0;
                for (int k = -halfY; k < halfYplus1; k++)
                {
                    for (int l = -halfX; l < halfXplus1; l++)
                    {
                        if (inData[(halfY + k) * inStride + (j + l) * origBytesPerPixel] < 127)
                        {
                            v++;
                        }
                    }
                }
                filterData[j] = v;
            }

            // Get the address of the first line.
            for (int i = halfY + 1; i < i1; i++)
            {
                for (int j = halfX; j < j1; j++)
                {
                    int val1 = 0;

                    int k1 = i - halfY - 1;
                    for (int l = -halfX; l < halfXplus1; l++)
                    {
                        if (inData[(k1) * inStride + (j + l) * origBytesPerPixel] < 127)
                        {
                            val1++;
                        }
                    }

                    int val2 = 0;

                    int k2 = i - 1 + halfY;
                    for (int l = -halfX; l < halfXplus1; l++)
                    {
                        if (inData[(k2) * inStride + (j + l) * origBytesPerPixel] < 127)
                        {
                            val2++;
                        }
                    }
                    int val = filterData[j] - val1 + val2;
                    filterData[j] = val;
                    if (val > (2 * winX * winY) / 3)
                    {
                        for (int b = 0; b < oChannels; b++)
                        {
                            destData[i * destStride + j * destBytesPerPixel + b] = 255;
                        }
                    }
                }
            }

            unsafe
            {
                fixed (byte* ptr = destData)
                {
                    destImage.SetPixels((IntPtr)ptr);
                }
            }

            bitmap.Dispose();
            bitmap = destImage;
        }
    }


    /// <summary>
    /// Obsolete. Use `LineRemover` instead.
    /// </summary>
    [Obsolete]
    internal class HorizontalLineRemover
    {
        static int HORIZONTAL_LINE_FILTER_LENGTH = 101;

        public static void Process(ref SKBitmap bitmap)
        {
            int winX = HORIZONTAL_LINE_FILTER_LENGTH;
            int winY = 3;
            int halfX = winX / 2;
            int halfY = winY / 2;
            int halfXplus1 = halfX + 1;
            int halfYplus1 = halfY + 1;

            int i1 = bitmap.Height - halfY;
            int j1 = bitmap.Width - halfX;

            var destImage = bitmap.Copy();

            int origBytesPerPixel = bitmap.BytesPerPixel;
            int destBytesPerPixel = destImage.BytesPerPixel;

            int oChannels = destBytesPerPixel;
            if (oChannels == 4) oChannels--;

            /*the size of the image in bytes */
            int inStride = bitmap.RowBytes;
            int destStride = destImage.RowBytes;
            int inSize = inStride * bitmap.Height;
            int destSize = destStride * destImage.Height;

            /*Allocate buffer for image*/
            byte[] inData = new byte[inSize];
            byte[] destData = new byte[destSize];

            Marshal.Copy(bitmap.GetPixels(), inData, 0, inSize);
            Marshal.Copy(destImage.GetPixels(), destData, 0, destSize);

            // Get the address of the first line.
            for (int i = halfY; i < i1; i++)
            {
                for (int j = halfX; j < j1; j++)
                {
                    int val = 0;
                    for (int k = -halfY; k < halfYplus1; k++)
                    {
                        for (int l = -halfX; l < halfXplus1; l++)
                        {
                            if (inData[(i + k) * inStride + (j + l) * origBytesPerPixel] < 127)
                            {
                                val++;
                            }
                        }
                    }
                    if (val > (2 * winX * winY) / 3)
                    {
                        for (int b = 0; b < oChannels; b++)
                        {
                            destData[i * destStride + j * destBytesPerPixel + b] = 255;
                        }
                    }
                }
            }

            unsafe
            {
                fixed (byte* ptr = destData)
                {
                    destImage.SetPixels((IntPtr)ptr);
                }
            }

            bitmap.Dispose();
            bitmap = destImage;
        }
    }


    internal class Median
    {
        public static void Process(ref SKBitmap bitmap, int winX, int winY)
        {
            int halfX = winX / 2;
            int halfY = winY / 2;
            int halfXplus1 = halfX + 1;
            int halfYplus1 = halfY + 1;

            int i1 = bitmap.Height - halfY;
            int j1 = bitmap.Width - halfX;

            int origBytesPerPixel = bitmap.BytesPerPixel;
            int nChannels = origBytesPerPixel;
            if (nChannels == 4) nChannels--;
            byte[] buf = new byte[winX * winY];

            /* the size of the image in bytes */
            int stride = bitmap.RowBytes;
            int size = stride * bitmap.Height;

            /* Allocate buffer for image */
            byte[] inData = new byte[size];

            /* This overload copies data of /size/ into /data/ from location specified (/Scan0/) */
            Marshal.Copy(bitmap.GetPixels(), inData, 0, size);

            for (int i = halfY; i < i1; i++)
            {

                for (int j = halfX; j < j1; j++)
                {
                    int index = 0;
                    for (int k = -halfY; k < halfYplus1; k++)
                    {
                        for (int l = -halfX; l < halfXplus1; l++)
                        {
                            buf[index++] = inData[(i + k) * stride + (j + l) * origBytesPerPixel];
                        }
                    }

                    Array.Sort(buf);
                    for (int b = 0; b < nChannels; b++)
                    {
                        inData[i * stride + j * origBytesPerPixel + b] = buf[buf.Length / 2];
                    }
                }
            }

            unsafe
            {
                fixed (byte* ptr = inData)
                {
                    bitmap.SetPixels((IntPtr)ptr);
                }
            }
        }
    }


    internal class Gamma
    {
        public static void Process(ref SKBitmap bitmap, double gamma)
        {
            byte[] gammaLUT = GammaLUT(gamma);

            int origBytesPerPixel = bitmap.BytesPerPixel;
            int nChannels = origBytesPerPixel;
            if (nChannels == 4)
                nChannels--;

            /*the size of the image in bytes */
            int stride = bitmap.RowBytes;
            int size = stride * bitmap.Height;

            /*Allocate buffer for image*/
            byte[] inData = new byte[size];

            /*This overload copies data of /size/ into /data/ from location specified (/Scan0/)*/
            Marshal.Copy(bitmap.GetPixels(), inData, 0, size);

            for (int i = 0; i < bitmap.Height; i++)
            {
                for (int j = 0; j < bitmap.Width; j++)
                {
                    for (int b = 0; b < nChannels; b++)
                    {
                        byte val = inData[i * stride + j * origBytesPerPixel + b];
                        inData[i * stride + j * origBytesPerPixel + b] = gammaLUT[val];
                    }
                }
            }

            unsafe
            {
                fixed (byte* ptr = inData)
                {
                    bitmap.SetPixels((IntPtr)ptr);
                }
            }
        }

        // Create the gamma correction lookup table
        private static byte[] GammaLUT(double gamma_new)
        {
            byte[] gammaLUT = new byte[256];

            for (int i = 0; i < gammaLUT.Length; i++)
                gammaLUT[i] = (byte)(255 * Math.Pow((double)i / (double)255, gamma_new));

            return gammaLUT;
        }
    }


    /// <summary>
    /// Represents contrast image filter.
    /// </summary>
    internal class Contrast
    {
        /// <summary>
        /// Applies contrast image filter to bitmap.
        /// </summary>
        /// <param name="bmp">Input 24-bit bitmap.</param>
        /// <param name="contrastLevel">Contrast level from -100 to +100, default 0.</param>
        /// <returns></returns>
        public static bool Process(ref SKBitmap bmp, int contrastLevel)
        {
            if (bmp.ColorType != SKColorType.Rgb888x)
                return false;

            float value = (100.0f + contrastLevel) / 100.0f;
            value *= value;

            int w = bmp.Width;
            int h = bmp.Height;

            SKBitmap tmp = bmp.Copy();

            var srcPixels = tmp.Pixels;
            var dstPixels = bmp.Pixels;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var srcPixel = srcPixels[y * w + x];
                    byte r = srcPixel.Red;
                    byte g = srcPixel.Green;
                    byte b = srcPixel.Blue;

                    float red = r / 255.0f;
                    float green = g / 255.0f;
                    float blue = b / 255.0f;
                    red = (((red - 0.5f) * value) + 0.5f) * 255.0f;
                    green = (((green - 0.5f) * value) + 0.5f) * 255.0f;
                    blue = (((blue - 0.5f) * value) + 0.5f) * 255.0f;

                    int iR = (int)red;
                    iR = iR > 255 ? 255 : iR;
                    iR = iR < 0 ? 0 : iR;
                    int iG = (int)green;
                    iG = iG > 255 ? 255 : iG;
                    iG = iG < 0 ? 0 : iG;
                    int iB = (int)blue;
                    iB = iB > 255 ? 255 : iB;
                    iB = iB < 0 ? 0 : iB;

                    dstPixels[y * w + x] = new SKColor((byte)iR, (byte)iG, (byte)iB);
                }
            }

            // Update bitmap with modified pixels
            unsafe
            {
                fixed (SKColor* ptr = dstPixels)
                {
                    bmp.SetPixels((IntPtr)ptr);
                }
            }
            tmp.Dispose();

            return true;
        }
    }


    /// <summary>
    /// Represents blur image filter.
    /// </summary>
    internal class Blur
    {
        /// <summary>
        /// Applies blur image filter to bitmap.
        /// </summary>
        /// <param name="bitmap">Input 24-bit bitmap.</param>
        /// <param name="blurZoneSize">Blur zone size from 1 to 10.</param>
        /// <returns></returns>
        public static void Process(ref SKBitmap bitmap, int blurZoneSize)
        {
            if (blurZoneSize < 1 || blurZoneSize > 10)
                return;

            int len = blurZoneSize * 2 + 1;

            int[,] mask = new int[len, len];

            for (int row = 0; row < len; row++)
                for (int col = 0; col < len; col++)
                    mask[row, col] = 1;

            ConvolutionCore(ref bitmap, mask, len * len, 0);
        }

        private static bool ConvolutionCore(ref SKBitmap bmp, int[,] mask, double divfactor, double offset)
        {
            if (bmp.ColorType != SKColorType.Rgb888x)
                return false;

            int w = bmp.Width;
            int h = bmp.Height;

            int row = mask.GetLength(0);
            int col = mask.GetLength(1);

            int xzone = (row - 1) / 2;
            int yzone = (col - 1) / 2;

            int ix, iy, xx, yy, mx, my;

            SKBitmap tmp = bmp.Copy();

            var srcPixels = tmp.Pixels;
            var dstPixels = bmp.Pixels;
            {
                int r, g, b;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        r = g = b = 0;
                        for (iy = y - yzone; iy <= y + yzone; iy++)
                        {
                            for (ix = x - xzone; ix <= x + xzone; ix++)
                            {
                                mx = ix - x + xzone;
                                my = iy - y + yzone;

                                if (mask[mx, my] == 0) continue;

                                xx = ix;
                                yy = iy;
                                if ((iy < 0) || (iy > h - 1)) yy = y;
                                if ((ix < 0) || (ix > w - 1)) xx = x;

                                var srcPixel = srcPixels[yy * w + xx];
                                r += srcPixel.Red * mask[mx, my];
                                g += srcPixel.Green * mask[mx, my];
                                b += srcPixel.Blue * mask[mx, my];
                            }
                        }

                        dstPixels[y * w + x] = new SKColor(
                            AdjustByte((double)r / divfactor + offset),
                            AdjustByte((double)g / divfactor + offset),
                            AdjustByte((double)b / divfactor + offset));
                    }
                }
            }

            // Update bitmap with modified pixels
            unsafe
            {
                fixed (SKColor* ptr = dstPixels)
                {
                    bmp.SetPixels((IntPtr)ptr);
                }
            }
            tmp.Dispose();

            return true;
        }

        private static byte AdjustByte(double value)
        {
            if (value < 0) return 0; else if (value > 255) return 255;
            return (byte)value;
        }
    }


    /// <summary>
    /// Transform class corrects for image tilt
    /// and supports image resize. Although, resize filter is
    /// not used, it will be useful if image has very small font
    /// or scanned at a low DPI
    /// Author: KKMohanty (kkmohanty@gmail.com )
    /// </summary>
    internal class Transform
    {
        public static SKBitmap RotateImage(SKBitmap image, float angle)
        {
            int extraWidth = (int)(1.1 * (image.Width * Math.Cos(angle * Math.PI / 180)));
            var destImage = new SKBitmap(extraWidth, image.Height);

            using (var surface = SKSurface.Create(new SKImageInfo(extraWidth, image.Height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                using (var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.Medium,
                    IsAntialias = true
                })
                {
                    canvas.RotateDegrees(angle);
                    canvas.DrawBitmap(image, 0, 0, paint);
                }

                using (var snapshot = surface.Snapshot())
                {
                    destImage = SKBitmap.Decode(snapshot.Encode(SKEncodedImageFormat.Png, 100));
                }
            }

            return destImage;
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static void ResizeImage(ref SKBitmap image, int width, int height)
        {
            var destImage = new SKBitmap(width, height);

            using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                using (var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.High,
                    IsAntialias = true
                })
                {
                    var srcRect = new SKRect(0, 0, image.Width, image.Height);
                    var destRect = new SKRect(0, 0, width, height);
                    canvas.DrawBitmap(image, srcRect, destRect, paint);
                }

                using (var snapshot = surface.Snapshot())
                {
                    destImage = SKBitmap.Decode(snapshot.Encode(SKEncodedImageFormat.Png, 100));
                }
            }

            image.Dispose();
            image = destImage;
        }
    }


    internal enum eRGB { b, g, r, a };

    internal abstract class BitmapX : IDisposable
    {
        private bool _isDisposed = false;

        protected SKBitmap Bitmap;
        protected int Width, Height;
        protected IntPtr Scan0;
        protected int Stride;
        protected int Length;
        protected byte[] Bytes;

        public int DataLength
        {
            get { return Length; }
        }

        public BitmapX(SKBitmap bmp)
        {
            Bitmap = bmp;
            Width = bmp.Width;
            Height = bmp.Height;
        }

        private void Dispose(bool flag)
        {
            if (!_isDisposed)
            {
                if (flag && Bytes != null && Scan0 != IntPtr.Zero)
                {
                    IntPtr pixelsPtr = Bitmap.GetPixels();
                    Marshal.Copy(Bytes, 0, pixelsPtr, Length);
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BitmapX()
        {
            Dispose(false);
        }
    }

    internal class Bitmap32 : BitmapX
    {
        private int xyr, xyg, xyb, xya;

        public Bitmap32(SKBitmap bmp)
            : base(bmp)
        {
            Scan0 = bmp.GetPixels();
            Stride = bmp.RowBytes;
            Length = Stride * Height;
            Bytes = new byte[Length];
            Marshal.Copy(Scan0, Bytes, 0, Length);
        }

        public byte this[int x, int y, eRGB rgb]
        {
            get { return Bytes[Stride * y + x * 4 + (int)(rgb)]; }
            set { Bytes[Stride * y + x * 4 + (int)(rgb)] = value; }
        }

        public byte this[int index]
        {
            get { return Bytes[index]; }
            set { Bytes[index] = value; }
        }

        public int IndexR(int x, int y)
        {
            return Stride * y + x * 4 + 2; // a <- +1
        }

        public int IndexRA(int x, int y)
        {
            return Stride * y + x * 4 + 3;
        }

        public void SetXY(int x, int y)
        {
            xyb = Stride * y + x * 4;
            xyg = xyb + 1;
            xyr = xyg + 1;
            xya = xyr + 1;
        }

        public byte R
        {
            get { return Bytes[xyr]; }
            set { Bytes[xyr] = value; }
        }

        public byte G
        {
            get { return Bytes[xyg]; }
            set { Bytes[xyg] = value; }
        }

        public byte B
        {
            get { return Bytes[xyb]; }
            set { Bytes[xyb] = value; }
        }

        public byte A
        {
            get { return Bytes[xya]; }
            set { Bytes[xya] = value; }
        }
    }

    internal class Bitmap24 : BitmapX
    {
        private int xyr, xyg, xyb;

        public Bitmap24(SKBitmap bmp)
            : base(bmp)
        {
            Scan0 = bmp.GetPixels();
            Stride = bmp.RowBytes;
            Length = Stride * Height;
            Bytes = new byte[Length];
            Marshal.Copy(Scan0, Bytes, 0, Length);
        }

        public byte this[int x, int y, eRGB rgb]
        {
            get { return Bytes[Stride * y + x * 3 + (int)(rgb)]; }
            set { Bytes[Stride * y + x * 3 + (int)(rgb)] = value; }
        }

        public byte this[int index]
        {
            get { return Bytes[index]; }
            set { Bytes[index] = value; }
        }

        public int IndexR(int x, int y)
        {
            return Stride * y + x * 3 + 2;
        }

        public void SetXY(int x, int y)
        {
            xyb = Stride * y + x * 3;
            xyg = xyb + 1;
            xyr = xyg + 1;
        }

        public byte R
        {
            get { return Bytes[xyr]; }
            set { Bytes[xyr] = value; }
        }

        public byte G
        {
            get { return Bytes[xyg]; }
            set { Bytes[xyg] = value; }
        }

        public byte B
        {
            get { return Bytes[xyb]; }
            set { Bytes[xyb] = value; }
        }
    }

    internal class Bitmap8 : BitmapX
    {
        public Bitmap8(SKBitmap bmp)
            : base(bmp)
        {
            Scan0 = bmp.GetPixels();
            Stride = bmp.RowBytes;
            Length = Stride * Height;
            Bytes = new byte[Length];
            Marshal.Copy(Scan0, Bytes, 0, Length);
        }

        public byte this[int x, int y]
        {
            get { return Bytes[Stride * y + x]; }
            set { Bytes[Stride * y + x] = value; }
        }

        public byte this[int index]
        {
            get { return Bytes[index]; }
            set { Bytes[index] = value; }
        }

        public int Index(int x, int y)
        {
            return Stride * y + x;
        }
    }

    internal class Bitmap1 : BitmapX
    {
        public Bitmap1(SKBitmap bmp)
            : base(bmp)
        {
            Scan0 = bmp.GetPixels();
            Stride = bmp.RowBytes;
            Length = Stride * Height;
            Bytes = new byte[Length];
            Marshal.Copy(Scan0, Bytes, 0, Length);
        }

        private byte pd;

        public bool this[int x, int y]
        {
            get
            {
                pd = Bytes[Stride * y + x / 8];

                return ((pd >> (7 - (x % 8)) & 1) == 1);
            }

            set
            {
                pd = Bytes[Stride * y + x / 8];

                if (value)
                {
                    Bytes[Stride * y + x / 8] = (byte)(pd | (1 << (7 - (x % 8))));
                }
                else
                {
                    Bytes[Stride * y + x / 8] = (byte)(pd & (~(1 << (7 - (x % 8)))));
                }
            }
        }
    }

    /// <summary>
    /// Image in unmanaged memory.
    /// </summary>
    internal unsafe class UnmanagedImage : IDisposable
    {
        // pointer to image data in unmanaged memory
        private IntPtr imageData;

        // image size
        private int width, height;

        // image stride (line size)
        private int stride;

        // image pixel format
        private SKColorType colorType;

        // flag which indicates if the image should be disposed or not
        private bool mustBeDisposed = false;

        private SKBitmap bmp;
        private byte[] pixelData;

        public byte* Start { get; private set; }
        public byte* StartGreen { get; private set; }
        public byte* MaxPtr { get; private set; }
        public int Step { get; private set; }

        public int GreenComponentOffset
        {
            get
            {
                return colorType == SKColorType.Gray8 ? 0 : 1;
            }
        }

        /// <summary>
        /// Pointer to image data in unmanaged memory.
        /// </summary>
        public IntPtr ImageData
        {
            get { return imageData; }
        }

        /// <summary>
        /// Image width in pixels.
        /// </summary>
        public int Width
        {
            get { return width; }
        }

        /// <summary>
        /// Image height in pixels.
        /// </summary>
        public int Height
        {
            get { return height; }
        }

        /// <summary>
        /// Image stride (line size in bytes).
        /// </summary>
        public int Stride
        {
            get { return stride; }
        }

        /// <summary>
        /// Image pixel format.
        /// </summary>
        public SKColorType ColorType
        {
            get { return colorType; }
        }

        public SKColor DefaultColor
        {
            get; set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedImage"/> class.
        /// </summary>
        public UnmanagedImage(IntPtr imageData, int width, int height, int stride, SKColorType colorType)
        {
            Init(imageData, width, height, stride, colorType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedImage"/> class.
        /// </summary>
        private void Init(IntPtr imageData, int width, int height, int stride, SKColorType colorType)
        {
            this.imageData = imageData;
            this.width = width;
            this.height = height;
            this.stride = stride;
            this.colorType = colorType;

            Start = (byte*)imageData.ToPointer();
            StartGreen = (byte*)imageData.ToPointer() + GreenComponentOffset;
            MaxPtr = (byte*)imageData.ToPointer() + stride * height;
            Step = GetBytesPerPixel(colorType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedImage"/> class.
        /// </summary>
        public UnmanagedImage(SKBitmap bmp)
        {
            this.bmp = bmp;
            this.width = bmp.Width;
            this.height = bmp.Height;
            this.stride = bmp.RowBytes;
            this.colorType = bmp.ColorType;

            // Get pixel data
            IntPtr pixelsPtr = bmp.GetPixels();
            int dataSize = stride * height;
            pixelData = new byte[dataSize];
            Marshal.Copy(pixelsPtr, pixelData, 0, dataSize);

            fixed (byte* ptr = pixelData)
            {
                imageData = new IntPtr(ptr);
                Init(imageData, width, height, stride, colorType);
            }
        }

        private int GetBytesPerPixel(SKColorType colorType)
        {
            switch (colorType)
            {
                case SKColorType.Gray8:
                    return 1;
                case SKColorType.Rgb565:
                    return 2;
                case SKColorType.Argb4444:
                    return 2;
                case SKColorType.Rgba8888:
                    return 4;
                case SKColorType.Rgb888x:
                    return 4;
                case SKColorType.Bgra8888:
                    return 4;
                default:
                    return 4;
            }
        }

        /// <summary>
        /// Destroys the instance of the <see cref="UnmanagedImage"/> class.
        /// </summary>
        ~UnmanagedImage()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // remove me from the Finalization queue 
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Update bitmap if we have one
                if (bmp != null && pixelData != null)
                {
                    IntPtr pixelsPtr = bmp.GetPixels();
                    Marshal.Copy(pixelData, 0, pixelsPtr, pixelData.Length);
                    pixelData = null;
                    bmp = null;
                }
            }

            // free image memory if the image was allocated using this class
            if ((mustBeDisposed) && (imageData != IntPtr.Zero))
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(imageData);
                System.GC.RemoveMemoryPressure(stride * height);
                imageData = IntPtr.Zero;
            }
        }
    }

    internal class Grayscale
    {
        public static void Process(ref SKBitmap bitmap)
        {
            SKBitmap negative = new SKBitmap(bitmap.Width, bitmap.Height, SKColorType.Rgb888x, SKAlphaType.Opaque);

            using (var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                using (var paint = new SKPaint
                {
                    ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                    {
                        0.299f, 0.587f, 0.114f, 0, 0,
                        0.299f, 0.587f, 0.114f, 0, 0,
                        0.299f, 0.587f, 0.114f, 0, 0,
                        0, 0, 0, 1, 0
                    })
                })
                {
                    canvas.DrawBitmap(bitmap, 0, 0, paint);
                }

                using (var image = surface.Snapshot())
                {
                    negative = SKBitmap.Decode(image.Encode(SKEncodedImageFormat.Png, 100));
                }
            }

            bitmap.Dispose();
            bitmap = negative;
        }
    }

    /// <summary>
    /// Resize the image using the specified scale factor.
    /// </summary>
    internal class Scale
    {
        public static SKSizeI Process(ref SKBitmap bitmap, double scale, SKFilterQuality filterQuality)
        {
            int width = (int)(bitmap.Width * scale);
            int height = (int)(bitmap.Height * scale);

            SKBitmap resultBitmap = new SKBitmap(width, height);

            using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                using (var paint = new SKPaint
                {
                    FilterQuality = filterQuality,
                    IsAntialias = true
                })
                {
                    var srcRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);
                    var destRect = new SKRect(0, 0, width, height);
                    canvas.DrawBitmap(bitmap, srcRect, destRect, paint);
                }

                using (var image = surface.Snapshot())
                {
                    resultBitmap = SKBitmap.Decode(image.Encode(SKEncodedImageFormat.Png, 100));
                }
            }

            bitmap.Dispose();
            bitmap = resultBitmap;

            return new SKSizeI(width, height);
        }
    }

    /// <summary>
    /// Invert filter.
    /// </summary>
    internal class Invert
    {
        public static void Process(ref SKBitmap bitmap)
        {
            // Work on a copy if you want to keep original unchanged
            var bmp = bitmap.Copy();

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    SKColor c = bmp.GetPixel(x, y);
                    SKColor inv = new SKColor((byte)(255 - c.Red),
                                              (byte)(255 - c.Green),
                                              (byte)(255 - c.Blue),
                                              c.Alpha);
                    bmp.SetPixel(x, y, inv);
                }
            }

            bitmap.Dispose();
            bitmap = bmp;
        }
    }
}
