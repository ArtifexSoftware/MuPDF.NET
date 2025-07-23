using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace BarcodeReader.Core.Common
{
#if CORE_DEV
    public
#else
    internal
#endif
    interface IImageScaner
    {
        int Width { get; }
        int Height { get; }
        bool In(MyPoint p);
        bool isBlack(MyPoint p);
        float[] GetPixels(MyPoint a, MyPoint b);
    }

#if CORE_DEV
    public
#else
    internal
#endif
    class ImageScaner : IImageScaner
    {
        public static readonly int LEFT = -1;
        public static readonly int RIGHT = 1;
        public static readonly int UP = -1;
        public static readonly int DOWN = 1;
        public static readonly MyPoint LU = new MyPoint(LEFT, UP);
        public static readonly MyPoint LD = new MyPoint(LEFT, DOWN);
        public static readonly MyPoint RU = new MyPoint(RIGHT, UP);
        public static readonly MyPoint RD = new MyPoint(RIGHT, DOWN);
        public static readonly MyPoint[] DIRS = new MyPoint[] { LU, LD, RU, RD };
#if DEBUG_IMAGE
        SKBitmap bb = null;
        Graphics g = null;
#endif

        BlackAndWhiteImage image;
        float bwThreshold;

        public ImageScaner(BlackAndWhiteImage image)
        {
            this.image = image;
#if DEBUG_IMAGE
            image.GetAsBitmap().Save(@"outRaw.png");
            Reset();
#endif
        }
        public float BWThreshold { get { return bwThreshold; } set { bwThreshold = value; } }
        public int Width { get { return image.Width; } }
        public int Height { get { return image.Height; } }
        public bool InX(int x) { return x >= 0 && x < image.Width; }
        public bool InY(int y) { return y >= 0 && y < image.Height; }
        public bool InX(float x) { return x >= 0F && x < (float)image.Width; }
        public bool InY(float y) { return y >= 0F && y < (float)image.Height; }
        public bool In(MyPoint p) { return InX(p.X) && InY(p.Y); }
        public bool In(MyPointF p) { return InX(p.X) && InY(p.Y); }

        public bool InBorderX(int x) { return x >= -1 && x <= image.Width; }
        public bool InBorderY(int y) { return y >= -1 && y <= image.Height; }
        public bool InBorder(MyPoint p) { return InBorderX(p.X) && InBorderY(p.Y); }

        public XBitArray GetColumn(int x) { return image.GetColumn(x); }
        public XBitArray GetRow(int y) { return image.GetRow(y); }

        public bool isBlack(MyPoint p) { return isBlack(p.X, p.Y); }
        public bool isBlack(int x, int y)
        {
            if (y >= 0 && y < image.Height && x >= 0 && x < image.Width)
                return image.GetRow(y)[x];
            return false; //by default returns white
        }

        public float getGray(MyPoint p)
        {
            return isBlack(p) ? 0F : 1F;
            //Comment the previous lines and uncomment lines below to sample the original RGB image
            //if (In(p)) return colorImage.GetPixel(p.X, p.Y).GetBrightness();
            //return 1.0F;
        }

        public bool getGrayIsBlack(MyPoint p)
        {
            return isBlack(p);
            //Comment the previous lines and uncomment lines below to sample the original RGB image
            //return getGray(p) < bwThreshold;
        }

        public bool getSampleSimple(MyPoint p)
        {
#if DEBUG_IMAGE
            if (In(p))
            {
                setPixel(p, Color.Orange); //paint pixel center
                setPixel(p+new MyPoint(1,0), isBlack(p) ? Color.Black : Color.White); //paint pixel center
            }
#endif
            return !isBlack(p);
        }

        public bool isBlackSample(MyPointF point, float moduleLength)
        {
            return getSample(point, moduleLength) < this.bwThreshold;
        }

        //Main sample function: uses 4 neighbours and ponderate them according to its distance to point
        public float getSample(MyPointF point, float moduleLength)
        {

            MyPoint p00 = new MyPoint((int)Math.Truncate(point.X), (int)Math.Truncate(point.Y));
            float c = 0f;
            float incX = point.X - p00.X;
            float incY = point.Y - p00.Y;            
            if (moduleLength < 5f)
            {
                MyPoint[] samples = new MyPoint[4];
                if (incX < 0.5F)
                    if (incY < 0.5F)
                    {
                        samples[0] = new MyPoint(p00.X, p00.Y);
                        samples[1] = new MyPoint(p00.X - 1, p00.Y);
                        samples[2] = new MyPoint(p00.X, p00.Y - 1);
                        samples[3] = new MyPoint(p00.X - 1, p00.Y - 1);
                    }
                    else
                    {
                        samples[0] = new MyPoint(p00.X, p00.Y);
                        samples[1] = new MyPoint(p00.X - 1, p00.Y);
                        samples[2] = new MyPoint(p00.X, p00.Y + 1);
                        samples[3] = new MyPoint(p00.X - 1, p00.Y + 1);
                        incY = 1F - incY;
                    }
                else
                    if (incY < 0.5F)
                    {
                        samples[0] = new MyPoint(p00.X, p00.Y);
                        samples[1] = new MyPoint(p00.X + 1, p00.Y);
                        samples[2] = new MyPoint(p00.X, p00.Y - 1);
                        samples[3] = new MyPoint(p00.X + 1, p00.Y - 1);
                        incX = 1F - incX;
                    }
                    else
                    {
                        samples[0] = new MyPoint(p00.X, p00.Y);
                        samples[1] = new MyPoint(p00.X + 1, p00.Y);
                        samples[2] = new MyPoint(p00.X, p00.Y + 1);
                        samples[3] = new MyPoint(p00.X + 1, p00.Y + 1);
                        incX = 1F - incX;
                        incY = 1F - incY;
                    }
                float c00 = getGray(samples[0]);
                float c10 = getGray(samples[1]);
                float c01 = getGray(samples[2]);
                float c11 = getGray(samples[3]);
                c = c00 * (0.5F + incX) * (0.5F + incY) +
                    c10 * (0.5F - incX) * (0.5F + incY) +
                    c01 * (0.5F + incX) * (0.5F - incY) +
                    c11 * (0.5F - incX) * (0.5F - incY);
            }
            else
            {
                MyPoint[] samples = new MyPoint[5];
                incX = incY = 0f;
                int inc = (int)Math.Round(moduleLength / 5f);
                samples[0] = new MyPoint(p00.X+inc, p00.Y+inc);
                samples[1] = new MyPoint(p00.X + inc, p00.Y-inc);
                samples[2] = new MyPoint(p00.X-inc, p00.Y - inc);
                samples[3] = new MyPoint(p00.X - inc, p00.Y + inc);
                samples[4] = new MyPoint(p00.X , p00.Y );

                c = 0f;
                for (int i = 0; i < samples.Length; i++) c += getGray(samples[i]);
                c /= (float)samples.Length;
            } 

           
#if DEBUG
            /*if (In(p00) && false)
            {
                //foreach (MyPoint p in samples)
                //    if (In(p)) bb.SetPixel(5 * p.X - 2, 5 * p.Y - 2, Color.Orange);
                MyPoint pm = point * 5F;
                if (In(point))
                {
                    setPixel(point, Color.Yellow);
                    setPixel(point,new MyPoint(1,0), c > bwThreshold ? Color.White : Color.Black);
                }
            }*/
#endif
#if DEBUG_IMAGE
            if (In(p00))
            {
                setPixel(point, Color.Orange); //paint pixel center
                setPixel(point + new MyPointF(1f, 0f), c<0.5F ? Color.Black : Color.White); //paint pixel center
            }
#endif
            return c;
        }


        static readonly float c0 = 2F / 3F;
        static readonly float c45 = (float)Math.Sqrt(2) / 3F;
        static readonly MyPointF[] moduleSamples = new MyPointF[]{new MyPointF(0F,0F), 
            new MyPointF(c0,0F), new MyPointF(-c0,0F), 
            new MyPointF(0F,c0), new MyPointF(0F,-c0), 
            new MyPointF(c45,c45), new MyPointF(-c45,c45), 
            new MyPointF(c45,-c45), new MyPointF(-c45,-c45) 
        };
        public float getModuleGrayLevel(MyPointF p, float W)
        {
            int count = 0;
            float grayLevel = 0F;
            float w2 = W / 3F;
            foreach(MyPointF offset in moduleSamples) {
                MyPointF pi = p +  offset * w2;
                grayLevel += 1F-getSample(pi,0f);
                count++;
#if DEBUG
                //setPixel(pi, System.Drawing.Color.Yellow);
#endif
            }
            float k = grayLevel / (float)count;
            return k;
        }

#if DEBUG_IMAGE
        const int SCALE = 5;
        public void Reset()
        {
            //save image with zoom *5 and sample and 4 points (orange), and the decimal sample point (yellow)
            bb = new SKBitmap(image.Width * SCALE, image.Height * SCALE);
            g = Graphics.FromImage(bb);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.DrawImage(image.GetAsBitmap(), + SCALE / 2, + SCALE / 2, bb.Width, bb.Height);
            //g.DrawImage(colorImage, 0, 0, bb.Width, bb.Height);
        }

        public void Save(string path)
        {
            try
            {
                bb.Save(path);
            }
            catch { }
        }

        public void setPixel(MyPointF p, Color color)
        {
            setPixel(p, new MyPoint(0, 0), color);
        }
        public void setPixel(MyPointF p, MyPoint inc, Color color)
        {
            MyPoint ip=new MyPoint((int)Math.Round(p.X * SCALE/*+2.5F*/), (int)Math.Round(p.Y * SCALE/*+2.5F*/));
            ip = ip + inc;
            if (ip.X>=0 && ip.X<bb.Width && ip.Y>=0 && ip.Y<bb.Height)
                bb.SetPixel(ip.X, ip.Y, color);
        }
#endif


        //starting from p point, follows a "horizontal" edge (-45º..45º) until the end.
        //p: start point
        //pIn, pOut: found segment
        //yDir: y direction
        public void FindHLine(MyPoint p, out MyPoint pIn, out MyPoint pEnd, bool current, int Ydir)
        {
            //to left
            bool end = false;
            int acum = 0, n = 1, d;
            pIn = new MyPoint(p.X - 1, p.Y);
            while (pIn.X >= 0 && !end)
            {
                XBitArray nextCol = image.GetColumn(pIn.X);
                int next = FindTransition(nextCol, pIn.Y, current, Ydir);
                if (next == -1) { end = true; pIn.X++; }
                else
                {
                    int expected = p.Y + acum * (n + 1) / n;
                    d = expected - next;
                    if (d < MAX_INC_SLOPE && d > -MAX_INC_SLOPE)
                    {
                        acum += (next - pIn.Y);
                        n++;
                        pIn.X--; pIn.Y = next;
                    }
                    else { end = true; pIn.X++; }
                }
            }

            //to right
            end = false;
            acum = 0; n = 1;
            pEnd = new MyPoint(p.X + 1, p.Y);
            while (pEnd.X < image.Width && !end)
            {
                XBitArray nextCol = image.GetColumn(pEnd.X);
                int next = FindTransition(nextCol, pEnd.Y, current, Ydir);
                if (next == -1) { end = true; pEnd.X--; }
                else
                {
                    int expected = p.Y + acum * (n + 1) / n;
                    d = expected - next;
                    if (d < MAX_INC_SLOPE && d > -MAX_INC_SLOPE)
                    {
                        acum += (next - pEnd.Y);
                        n++;
                        pEnd.X++; pEnd.Y = next;
                    }
                    else { end = true; pEnd.X--; }
                }
            }
        }

        //starting from p point, follows a "vertical" edge (135º..45º) until the end.
        //p: start point
        //pIn, pOut: found segment
        //yDir: y direction
        const int GOOD_INC_SLOPE = 1;
        const int MAX_INC_SLOPE = 6;
        public void FindVLine(MyPoint p, out MyPoint q, bool current, MyPoint dir)
        {
            bool end = false;
            MyPoint lastGood = q = new MyPoint(p.X, p.Y + dir.Y);
            int acum = 0, n = 1, d;
            while (q.Y >= 0 && q.Y < image.Height && !end)
            {
                XBitArray nextCol = (XBitArray)image.GetRow(q.Y);
                int next = FindTransition(nextCol, q.X, current, dir.X);
                if (next == -1) { end = true; q.Y -= dir.Y; }
                else
                {
                    float expected = (float)p.X + ((float)(2F * acum) / (float)(n));
                    d = (int)Math.Round(expected) - next;
                    if (d < MAX_INC_SLOPE && d > -MAX_INC_SLOPE)
                    {
                        if (d <= GOOD_INC_SLOPE && d >= -GOOD_INC_SLOPE) lastGood = new MyPoint(next, q.Y);
                        acum += (next - p.X);
                        n++;
                        q.Y += dir.Y; q.X = next; 
                    }
                    else { end = true; q.Y -= dir.Y; }
                }
            }
            q = lastGood;
            if (q.Y == image.Height) q.Y--;
            else if (q.Y == -1) q.Y++;
        }

        //starting from p point, follows a "horizontal" edge (-45º..45º) until the end.
        //p: start point
        //current: true is p is black
        public void FindHLine(MyPoint p, out MyPoint q, bool current, MyPoint dir)
        {
            bool end = false;
            MyPoint lastGood = q = new MyPoint(p.X + dir.X, p.Y);
            int acum = 0, n = 1, d;
            while (q.X >= 0 && q.X < image.Width && !end)
            {
                XBitArray nextCol = (XBitArray)image.GetColumn(q.X);
                int next = FindTransition(nextCol, q.Y, current, dir.Y);
                if (next == -1) { end = true; q.X -= dir.X; }
                else
                {
                    float expected = (float)p.Y + ((float)(2F * acum) / (float)(n));
                    d = (int)Math.Round(expected) - next;
                    if (d < MAX_INC_SLOPE && d > -MAX_INC_SLOPE)
                    {
                        if (d <= GOOD_INC_SLOPE && d >= -GOOD_INC_SLOPE) lastGood = new MyPoint(q.X, next);
                        acum += (next - p.Y);
                        n++;
                        q.X += dir.X; q.Y = next;
                    }
                    else { end = true; q.X -= dir.X; }
                }
            }
            q = lastGood;
            if (q.X == image.Width) q.X--;
            else if (q.X == -1) q.X++;
        }


        //current: current pixel is white or black
        //previous bits[start]==current
        //previous bits[start+dir]==!current
        const int MAX_SLOPE = 4;
        int FindTransition(XBitArray bits, int start, bool current, int dir)
        {
            int pos = start;
            int inc = 1;
            if (pos + dir < 0 || pos + dir >= bits.Size) return -1;
            if (bits[pos] == current && bits[pos + dir] == !current) return pos; //tribial solution
            if (bits[pos] == !current) //search backwards
            {
                pos -= dir;
                while (inc < MAX_SLOPE && pos >= 0 && pos < bits.Size && bits[pos] != current) { pos -= dir; inc++; }
                if (pos < 0 || pos >= bits.Size || inc >= MAX_SLOPE) return -1;
            }
            else //search forward
            {
                pos += dir;
                while (inc < MAX_SLOPE && pos >= 0 && pos < bits.Size && bits[pos] == current) { pos += dir; inc++; }
                if (pos <= 0 || pos >= bits.Size || inc >= MAX_SLOPE) return -1;
                pos -= dir; //one pixel back to point agains to current
            }
            return pos;
        }


        //Percetage of fixels of onBlack color from point a to b, for continuous side
        public float GetPercentOn(bool onBlack, SKPoint a, SKPoint b)
        {
            int total = 0, on = 0;
            Bresenham br = new Bresenham(a, b);
            while (!br.End())
            {
                bool next = isBlack(br.Current.X, br.Current.Y);
                if (!next && !onBlack || next && onBlack) on++;
                total++;
                br.Next();
            }
            return (float)on / (float)total;
        }

        //Percetage of fixels of onBlack color from point a to b, for dashed side
        public float GetPercentOn(bool onBlack, bool skipFirsts, MyPoint a, MyPoint b, out int nPairs, out int most, out float regularity)
        {
            int total = 0, on = 0;
            SortedDictionary<int, int> bars = new SortedDictionary<int, int>();
            int n = 0;
            nPairs = 0;
            bool current = false, first = false, next = false;
            Bresenham br = new Bresenham(a, b);
            if (skipFirsts) while (!br.End() && isBlack(br.Current) != onBlack) { br.Next(); total++; }//move to the first pixel onBlack
            if (!br.End())
            {
                first = current = isBlack(br.Current.X, br.Current.Y);
                while (!br.End())
                {
                    next = isBlack(br.Current.X, br.Current.Y);
                    //histogram pairs
                    if (next != current) //new transition
                    {
                        if (next == first) //only record each pair to avoid different saturation of black or white
                        {
                            if (bars.ContainsKey(n)) bars[n]++;
                            else bars.Add(n, 1);
                            nPairs += 1; //increment total number of pairs
                            n = 0;
                        }
                        current = next;
                    }

                    //count pixels on 
                    if (!next && !onBlack || next && onBlack) on++;
                    n++;
                    total++;
                    br.Next();
                }
            }
            if (n > 0 && next != first) //add last pair
            {
                if (bars.ContainsKey(n)) bars[n]++;
                else bars.Add(n, 1);
                nPairs += 1;
            }

            //find the most common
            most = -1;
            ArrayList repeated = new ArrayList();
            foreach (int i in bars.Keys)
                if (most == -1 || bars[i] > bars[most])
                {
                    most = i;
                    repeated.Clear();
                    repeated.Add(i);
                }
                else if (most != -1 && bars[i] == bars[most]) repeated.Add(i);
            if (most != -1)
            {
                most = (int)repeated[repeated.Count / 2]; //if repeated, take the middle
                int inc = most * 10 / 100 + 1;
                float m = 0f;
                for (int i = most - inc; i <= most + inc; i++)
                    if (bars.ContainsKey(i))
                    {
                        var weight = 1f / (Math.Abs(i - most) + 1);
                        m += bars[i] * weight;
                    }
                regularity = (float)m / (float)nPairs;
            }
            else regularity = 0.0F;

            return (float)on / (float)total;
        }


        public void FindEnd(MyPoint A, MyPoint B, MyPoint C, out MyPoint AB)
        {
            MyVector vdAB = B - A;
            MyVector vdAC = C - A;
            int sAB = (int)vdAB.Length;
            int sAC = (int)vdAC.Length;
            int sideAB = (int)(vdAB.Length * 2.5F);
            int sideAC = (int)(vdAC.Length * 2.5F);
            MyVector normAC = vdAC.NormInt;
            MyVector normAB = vdAB.NormInt;
            Bresenham ab = new Bresenham(A, B), abW = new Bresenham(A, B);
            Bresenham ac = new Bresenham(A, C), acW = new Bresenham(A, C);
            ab.MoveTo(B);
            ac.MoveTo(C);
            int n;
            AB = A;
            bool found = false;
            while (!found && In(ab.Current))
            {
                AB = ab.Current;
                ab.Next();
                while (isBlack(ab.Current)) ab.MoveTo(ab.Current - normAC);
                acW.MoveTo(ab.Current);
                n = sideAC;
                while (n > 0 && !isBlack(acW.Current)) { acW.Next(); n--; }
                found = (n == 0);
            }
            if (!found) AB = A;
            else
            {
                //adjust to fall on black
                n = sAC;
                bool black = false;
                while (n > 0 && !black)
                {
                    ab.MoveTo(AB);
                    int m = sAB;
                    while (m > 0 && !isBlack(ab.Current)) { ab.Previous(); m--; }
                    if (m == 0) { AB += normAC; n--; }
                    else black = true;
                }
            }
        }

        public void AdjustToWhite(MyPoint A, ref MyPoint B, MyPoint C, int N, int MAX)
        {
            MyVector vdCB = C - B;
            MyVector normCB = vdCB.NormInt;
            Bresenham ab = new Bresenham(A, B);

            //adjust to fall on white
            int n = MAX;
            bool white = false;
            while (n > 0 && !white)
            {
                ab.MoveTo(B);
                int m = N;
                int count = 0;
                while (m > 0 && count < MAX)
                {
                    if (isBlack(ab.Current)) count++;
                    ab.Previous(); 
                    m--; 
                }
                if (m == 0) white = true;
                else { B -= normCB; n--; } //move away
            }
        }

        public void AdjuntToEdge(MyPoint A, ref MyPoint B, MyPoint C, int N)
        {
            MyVector vdCB = C - B;
            MyVector normCB = vdCB.NormInt;
            Bresenham ab = new Bresenham(A, B);

            //adjust to fall on black
            int n = N;
            bool black = false;
            while (n > 0 && !black)
            {
                ab.MoveTo(B);
                int m = N;
                while (m > 0 && !isBlack(ab.Current)) { ab.Previous(); m--; }
                if (m == 0) { B += normCB; n--; }
                else black = true;
            }
        }

        //A and B define an edge on one of the DM dashed L pattern sides. 
        //These points are adjusted to the border of this DM side, moving them to the dir direction.
        //Uses linear regression.
        public void Regression(ref MyPointF A, ref MyPointF B, bool current, bool isHorizontal, int dir)
        {
            //order points
            bool reverse = false;
            MyPointF a = A, b = B;
            if (isHorizontal && B.X < A.X || !isHorizontal && B.Y < A.Y) { reverse = true; b = A; a = B; };

            //get function
            MyPoint iA = a, iB = b; //convert to int coords
            int incX = iB.X > iA.X ? iB.X - iA.X : iA.X - iB.X;
            int incY = iB.Y > iA.Y ? iB.Y - iA.Y : iA.Y - iB.Y;
            int inc = (incY < incX ? incX : incY) + 1;
            int[] f = new int[inc];
            int i = 0;
            XBitArray pixels;
            Bresenham br = new Bresenham(iA, iB);
            while (!br.End())
            {
                if (isHorizontal) pixels = InX(br.Current.X) ? image.GetColumn(br.Current.X) : null;
                else pixels = InY(br.Current.Y) ? image.GetRow(br.Current.Y) : null;
                int start = isHorizontal ? br.Current.Y : br.Current.X;
                int N = pixels == null ? 0 : pixels.Size;
                if (pixels != null && start >= 0 && start < N && pixels[start] == current)
                {
                    start += dir;
                    int n = start;
                    while (n >= 0 && n < pixels.Size && pixels[n] == current) n += dir;
                    if (n-start<5) f[i++] = n - start;
                } //else does not take into acount (discontinuos L pattern)
                br.Next();
            }
            inc = i; //use only black pixels

            //linear regression
            int xy, x, y, x2;
            xy = x = y = x2 = 0;
            for (i = 0; i < inc; i++)
            {
                x += i;
                y += f[i];
                xy += i * f[i];
                x2 += i * i;
            }
            float fx = (float)x / (float)inc;
            float fy = (float)y / (float)inc;
            float fxy = (float)xy / (float)inc;
            float fx2 = (float)x2 / (float)inc;

            float m = (fxy - fx * fy) / (fx2 - fx * fx);
            float k = fy - m * fx + 0.5F * (float)dir;

            if (Calc.Around(m, 0.0F, 0.5F))
            {
                if (isHorizontal) { a.Y += k; b.Y += k + m * (float)inc; }
                else { a.X += k; b.X += k + m * (float)inc; }
                if (reverse) { A = b; B = a; } else { A = a; B = b; }
            }
        }


        public MyPointF NextBlack(MyPointF p, MyVectorF dir, int minLength)
        {
            Bresenham br = new Bresenham(p, p + dir * 20);
            MyPoint last = br.Current;
            int count = 0;
            bool prev = isBlack(br.Current);
            while (In(br.Current))
            {
                bool current = isBlack(br.Current);
                if (prev == current) count = 0;
                else if (!prev && current) count++;
                if (count > minLength) break; 
                else if (count == 0)
                {
                    prev = current;
                    last = br.Current;
                }
                br.Next();
            }
            MyPointF r = last;
            MyVector d = last - br.Current;
            if (d.X < 0) r.X += 1.0F;
            else if (d.X == 0) r.X += 0.5F;
            if (d.Y < 0) r.Y += 1.0F;
            else if (d.Y == 0) r.Y += 0.5F;
            return r;
        }

        public MyPointF NextTransition(MyPointF p, MyVectorF dir, float moduleLength, bool currentIsBlack)
        {
            Bresenham br = new Bresenham(p, dir);
            MyPointF last;
            //if it is wrong placed
            if (getGrayIsBlack(br.Current) != currentIsBlack)
            {
                br.Next();
                while (In(br.Current) && getGrayIsBlack(br.Current) != currentIsBlack) br.Next(); 
            }
            last = br.CurrentF; 

            //now find transition
            br.Next();
            while (In(br.Current) && getGrayIsBlack(br.Current) == currentIsBlack)
            { last = br.CurrentF; br.Next(); }

            return (br.CurrentF + last) / 2F;
        }

        public MyPoint FindTransition(MyPoint p, MyVector dir, bool currentIsBlack, out MyPointF fP)
        {
            fP = MyPointF.Empty;
            Bresenham br = new Bresenham(p, p + dir);
            MyPoint last = MyPoint.Empty;
            if (!In(p)) //try to move again on the image
            {
                int n = 10; // stop after 10 pixels
                while (!In(br.Current) && n >0) { last = br.Current;  br.Next(); n--; }
                if (n == 0) return MyPoint.Empty; //we are far away from the image

                if (getGrayIsBlack(br.Current) != currentIsBlack)
                {
                    fP = SideOut(last, dir);
                    return last;
                }
            }
            if (getGrayIsBlack(br.Current) == currentIsBlack)
            {
                while (In(br.Current) && getGrayIsBlack(br.Current) == currentIsBlack) { last = br.Current; br.Next(); }
                if (!In(last)) return MyPoint.Empty;
                fP = SideOut(last, dir);
                return last;
            }
            else
            {
                while (In(br.Current) && getGrayIsBlack(br.Current) != currentIsBlack) { br.Previous(); }
                if (!In(br.Current)) return MyPoint.Empty;
                fP = SideOut(br.Current, dir);
                return br.Current;
            }
        }

        MyPointF SideOut(MyPoint p, MyVector vd)
        {
            MyPointF fp = (MyPointF)p;
            if (vd.X == -1) fp.Y += 0.5F;
            else if (vd.X == 1) fp = fp + new MyVectorF(1F, 0.5F);
            else if (vd.Y == 1) fp = fp + new MyVectorF(0.5F, 1F);
            else fp.X += 0.5F;
            return fp;
        }

        MyPointF SideOut(MyPoint p, MyVectorF vd)
        {
            MyPointF fp = (MyPointF)p;
            if (vd.X < 0) { }
            else if (vd.X == 0F) fp.X += 0.5F;
            else  fp.X+=1F;
            if (vd.Y < 0) { }
            else if (vd.Y == 0F) fp.Y += 0.5F;
            else fp.Y+=1F;
            return fp;
        }
        //TODO do more samples
        public bool ModuleIsBlack(MyPointF p, float width, float height, MyVectorF right, MyVectorF down)
        {
            return isBlackSample(p,0f);
        }

        const float LIM = 0.85F;
        public MyPointF FindModuleCenter(MyPointF p, bool isBlack, float width, float height, MyVectorF right, MyVectorF down)
        {
            //X center
            bool isBlackLeft = ModuleIsBlack(p - right, width, height, right, down);
            bool isBlackRight = ModuleIsBlack(p + right, width, height, right, down);

            if (isBlackLeft != isBlack && isBlackRight != isBlack)
            {
                MyPointF pLeft = NextTransition(p, -right, width, isBlack);
                MyPointF pRight = NextTransition(p, right, width, isBlack);
                if ((pLeft - p).Length < width * LIM && (pRight - p).Length < width * LIM)
                    p = (pLeft + pRight) / 2F;
            }
            else if (isBlackLeft != isBlack)
            {
                MyPointF pLeft = NextTransition(p, -right, width, isBlack);
                if ((pLeft - p).Length < width * LIM )
                    p = pLeft + right / 2F;
            }
            else if (isBlackRight != isBlack)
            {
                MyPointF pRight = NextTransition(p, right, width, isBlack);
                if ((pRight - p).Length < width * LIM)
                    p = pRight - right / 2F;
            }


            //Y center
            bool isBlackUp = ModuleIsBlack(p - down, width, height, right, down);
            bool isBlackDown = ModuleIsBlack(p + down, width, height, right, down);
            if (isBlackUp!=isBlack && isBlackDown!=isBlack)
            {
                MyPointF pUp = NextTransition(p, -down, height, isBlack);
                MyPointF pDown = NextTransition(p, down, height, isBlack);
                if ((pUp - p).Length < height*LIM && (pDown - p).Length < height*LIM)
                    p = (pUp + pDown) / 2F;
            }
            else if (isBlackUp!=isBlack)
            {
                MyPointF pUp = NextTransition(p, -down, height, isBlack);
                if ((pUp - p).Length < height*LIM)
                    p = pUp + down / 2F;
            }
            else if (isBlackDown!=isBlack)
            {
                MyPointF pDown = NextTransition(p, down, height, isBlack);
                if ((pDown - p).Length < height*LIM)
                    p = pDown - down / 2F;
            }
            return p;
        }

        public void setBWThreshold(MyPoint a, MyPoint b)
        {
            Bresenham br = new Bresenham(a, b);
            float grayWhite = 0F, grayBlack = 1F;
            while (!br.End())
            {
                float g = getGray(br.Current);
                if (g < grayBlack) grayBlack = g;
                if (g > grayWhite) grayWhite = g;
                br.Next();
            }
            BWThreshold = (grayWhite + grayBlack) / 2F;
        }

        public void setBWThreshold(float f)
        {
            BWThreshold = f;
        }

        //dir =(-1,0) or (0,-1)...
        public void FindModule(MyPoint center, MyVector dir, bool currentIsBlack, out MyPoint left, out MyPoint right, out MyPoint middle, out float dist)
        {
            left = center;
            right = center;
            while (In(left) && getGrayIsBlack(left) == currentIsBlack) left += dir; //move left
            while (In(right) && getGrayIsBlack(right) == currentIsBlack) right -= dir; //move right
            middle = (left + right) / 2;
            dist = (left - right).Length;
        }

        public float[] GetPixels(MyPoint a, MyPoint b)
        {
            Bresenham br = new Bresenham(a, b);
            var res = new float[br.Steps];

            var i = 0;
            while (br.Steps > 0)
            {
                var p = br.Current;
                res[i++] = image.GetRow(p.Y)[p.X] ? -1 : 1;
                br.Next();
            }

            return res;
        }

        public float[] GetPixels(MyPoint a, MyPoint b, float scale)
        {
            var dir = (b - a);
            var len = dir.Length;
            var steps = (int)(len * scale);
            var res = new float[steps];
            float x = a.X;
            float y = a.Y;
            var dx = dir.X / (float)steps;
            var dy = dir.Y / (float)steps;
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = image.GetPixelInterpolated(x, y);
                x += dx;
                y += dy;
            }

            return res;
        }

        public float[] GetPixels3Lines(MyPoint a, MyPoint b, float scale, float width = 1)
        {
            var dir = (b - a);
            var len = dir.Length;
            var steps = (int)(len * scale);
            var res = new float[steps];
            float x = a.X;
            float y = a.Y;
            var dx = dir.X / (float)steps;
            var dy = dir.Y / (float)steps;
            var n = (dir / len).Perpendicular * width;

            for (int i = 0; i < res.Length; i++)
            {
                var v1 = image.GetPixelInterpolated(x + n.X, y+ n.Y);
                var v2 = image.GetPixelInterpolated(x, y);
                var v3 = image.GetPixelInterpolated(x - n.X, y - n.Y);
                res[i] = (v1 + v2 + v3);
                x += dx;
                y += dy;
            }

            return res;
        }
    }
}
