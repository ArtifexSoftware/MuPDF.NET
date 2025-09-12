using System;
using System.Collections.Generic;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.MaxiCode
{
    //Class that allows to sample circles around the finder, even if it is a skewed circle.
    //The finder is defined on a set of border pixel coordinates (more than 2 pixel coord).
    //Radius are linearly interpolated between samples.
    class MaxiCodeFinder
    {
        ImageScaner scan;
        public MyPointF center;
        Sample[] samples;

        public MaxiCodeFinder(ImageScaner scan, MyPoint center, Sample[] samples)
        {
            this.scan = scan;
            this.center = center;
            this.samples = samples;
            foreach (Sample s in samples)
                s.dist = (s.point - center).Length + (float)s.rect;
        }

        public void getRadius(float angle, out float d, out float V, out float W, out float H)
        {
            int current = 0;
            while (current < samples.Length && samples[current].angle < angle) current++;
            if (current == samples.Length) current = 0;
            int prev = (current - 1 + samples.Length) % samples.Length;
            float prevAngle = samples[prev].angle;
            float currentAngle = samples[current].angle;
            if (current < prev) currentAngle += 360F;
            float incAngle = currentAngle - prevAngle;
            if (angle < prevAngle) angle += 360F;

            d = samples[prev].dist * (currentAngle - angle)/incAngle + 
                 samples[current].dist * (angle - prevAngle)/incAngle;

            V = d * 1.02F / 3.87F;
            W = (float)Math.Sqrt(3) / 2F * V;
            H = 3F * V / 4F;
        }

        //A circle is sampled dividing 360º by a value proportional to its radius
        //and then tracing bresenham segments between them.
        public SampledCircle[] traceCircle(float[] radius)
        {
            LinkedList<bool>[] result = new LinkedList<bool>[radius.Length];
            LinkedList<MyPoint>[] tpoints = new LinkedList<MyPoint>[radius.Length];
            MyPoint[] prevPoints = new MyPoint[radius.Length];
            MyPoint[] startPoints = new MyPoint[radius.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new LinkedList<bool>();
                tpoints[i] = new LinkedList<MyPoint>();
                prevPoints[i] = MyPoint.Empty;
                startPoints[i] = MyPoint.Empty;
            }

            float inc = 360F / samples[0].dist;
            for (float angle = 0; angle < 360F; angle += inc)
            {
                float d, v, w, h;
                getRadius(angle, out d, out v, out w, out h);
                float cosA = w * (float)Math.Cos(angle * (float)Math.PI / 180F);
                float sinA = -w * (float)Math.Sin(angle * (float)Math.PI / 180F);
                for(int i=0;i<radius.Length; i++)
                {
                    MyPointF p = center + new MyPointF(cosA, sinA) * radius[i];
                    if (angle == 0)
                    {
                        startPoints[i] = p; //remember start point
                        tpoints[i].AddLast(p);
                        result[i].AddLast(scan.isBlack(p));
                    }
                    else traceLine(result[i], prevPoints[i], p, tpoints[i]);
                    prevPoints[i] = p;
                    if (angle+inc>=360F) traceLine(result[i], p, startPoints[i], tpoints[i]); //join with the start point
#if DEBUG_IMAGE
                    scan.setPixel(p, System.Drawing.Color.Blue);
#endif
                }
            }

            SampledCircle[] circles = new SampledCircle[radius.Length];
            for (int i = 0; i < radius.Length; i++) circles[i] = new SampledCircle(center, result[i], tpoints[i]);
            return circles;
        }

        void traceLine(LinkedList<bool> l, MyPoint start, MyPoint end, LinkedList<MyPoint> points)
        {
            Bresenham br = new Bresenham(start, end);
            br.Next();
            while (!br.End())
            {
                points.AddLast(br.Current);
                l.AddLast(scan.isBlack(br.Current));
#if DEBUG_IMAGE
                scan.setPixel(br.Current, System.Drawing.Color.Orange);
#endif
                br.Next();
            }
        }
    }


	internal class Sample{
        public MyPoint point;
        public float angle;
        public int rect;
        public float dist;

        public Sample(MyPoint p, float angle, int rect)
        {
            this.point = p;
            this.angle = angle;
            this.rect=rect;
        }

    }

    //Class that stores sampled pixels from a circle around the finder.
	internal class SampledCircle
    {
        public XBitArray samples;
        public float[] angles;
        public MyPoint[] points;

        public SampledCircle(MyPoint center, LinkedList<bool> lSamples, LinkedList<MyPoint> lPoints)
        {
            samples = new XBitArray(lSamples.Count - 1);
            int j = 0;
            foreach (bool isBlack in lSamples)
                if (j == lSamples.Count - 1) break;
                else samples[j++] = isBlack;

            points = new MyPoint[lPoints.Count - 1];
            angles = new float[lPoints.Count - 1];
            j = 0;
            foreach (MyPoint p in lPoints)
                if (j == lPoints.Count - 1) break;
                else
                {
                    MyVector vd = p - center;
                    float angle = -vd.Angle;
                    if (angle<0) angle+=PI2;
                    angles[j] = angle;
                    points[j++] = p;
                }
        }

        public static readonly float PI2 = (float)Math.PI * 2F;
    }
}
