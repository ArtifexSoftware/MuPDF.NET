using System;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.MaxiCode
{
    //Class that represents a set of 6 alignment patterns (pixel coordinates + total quality)
    class MaxiCodeAlignedPatterns
    {
        public float quality;
        public MyPointF[] points;
        int n = 0;

        public MaxiCodeAlignedPatterns() { quality = 0F; points = new MyPointF[6]; n = 0; }

        public void Add(float q, MyPointF p)
        {
            quality += q;
            points[n++] = p;
        }

        //This is the more important method! Alignment patterns are not completely centered, 
        //barcodes are skewed, sometimes the finder is not centered,... These things make 
        //really difficult to find the 4 corners of the barcode.
        //The solution has been to track modules from the "center" to the corners. Actually,
        //4 tracks are traced, 2 starting at the alignment pattern 0, and 2 more at the alignment
        //pattern 3. 
        public BarCodeRegion ScanRegion(ImageScaner scan, float trackCoef)
        {
            MyVectorF vdX = (points[0] - points[3]) / 12F;
            MyVectorF vdY = vdX.Perpendicular * (2F / (float)Math.Sqrt(3)); // H = 2W/sqrt(3)
            MyVectorF vd60 = (points[1] - points[4]) / 12F;
            MyVectorF vd120 = (points[2] - points[5]) / 12F;

            float W = vdX.Length;

            MyPointF ru = points[0];
            MyPointF lu = points[3];
            MyPointF rd = points[0];
            MyPointF ld = points[3];

            //Track 16 modules to find the corners
            if (!Track(scan, ref ru, vd60, vdX, vd60, vd120, W, 16, trackCoef)) ru = points[0] + vd60 * 16F;
            if (!Track(scan, ref lu, vd120, vdX, vd60, vd120, W, 16, trackCoef)) lu = points[3] + vd120 * 16F;
            if (!Track(scan, ref rd, -vd120, vdX, vd60, vd120, W, 16, trackCoef)) rd = points[0] - vd120 * 16F;
            if (!Track(scan, ref ld, -vd60, vdX, vd60, vd120, W, 16, trackCoef)) ld = points[3] - vd60 * 16F;
#if DEBUG_IMAGE
            scan.Save(@"d:\outTrack.png");
#endif
            //Left down and left up corners are 1 module (1W) left more!
            //And corners are 0.5W from the center of the module!
            return new BarCodeRegion(ld - vdX * 0.5F - vdY * 0.5F, rd + vdX * 1.5F - vdY * 0.5F,
                lu - vdX * 0.5F + vdY * 0.5F, ru + vdX * 1.5F + vdY * 0.5F);
        }

        static readonly MyVectorF[] offsets = new MyVectorF[] { new MyVectorF(0, 0), new MyVectorF(1, 0), 
                new MyVectorF(1, 1), new MyVectorF(0, 1), new MyVectorF(-1, 1), new MyVectorF(-1, 0), 
                new MyVectorF(-1, -1), new MyVectorF(0, -1), new MyVectorF(1, -1)};

        //At the end, a very short method that is able to move through the barcode modules 
        //even for skewed ones. The idea is to move to the theoric position and then calculate
        //the quality of this position and 6 positions around (at k distance from the theoric position). 
        //The best quality will be the adjusted next position. An so on, nModules times.
        public bool Track(ImageScaner scan, ref MyPointF p, MyVectorF vd, MyVectorF vd0, MyVectorF vd60, MyVectorF vd120, float W, int nModules, float k)
        {
#if DEBUG_IMAGE
            //if (trace) 
                scan.Reset();
            scan.setPixel(p, System.Drawing.Color.Yellow);
#endif
            MyVectorF vdX = vd.Normalized;
            MyVectorF vdY = vdX.Perpendicular;
            for (int i = 0; i < nModules; i++)
            {
                p += vd;
                float max = 0F;
                MyVectorF offsetMax = offsets[0];
                float[] qualities = new float[offsets.Length * 5];
                int n = 0;
                foreach (MyVectorF offset in offsets)
                {
                    float q = GetQuality(scan, p + offset * (W * k), W, vd0, vd60, vd120);
                    qualities[n++] = q;
                    if (q > max) { max = q; offsetMax = offset * (W * k); }
                }
#if DEBUG_IMAGE
                scan.setPixel(p, System.Drawing.Color.Orange);
#endif
                //if (max < 4.5F) return false; //get lost
                p += offsetMax;
#if DEBUG_IMAGE
                scan.setPixel(p, System.Drawing.Color.Blue);
                //if (trace)
                    scan.Save(@"d:\outTrack.png");
#endif
            }
            return true;
        }

        //The quality of a position is the quality of its module and 6 modules around (at 0, 60, 120, 180, 240 and 300º).
        //Adding the quality of modules around, gives better results that only taking into account the main position.
        float GetQuality(ImageScaner scan, MyPointF p, float W, MyVectorF vd0, MyVectorF vd60, MyVectorF vd120)
        {
            float q0 = scan.getModuleGrayLevel(p, W); if (q0 < 0.5F) q0 = 1F - q0;
            float q1 = scan.getModuleGrayLevel(p + vd0, W); if (q1 < 0.5F) q1 = 1F - q0;
            float q2 = scan.getModuleGrayLevel(p + vd60, W); if (q2 < 0.5F) q2 = 1F - q2;
            float q3 = scan.getModuleGrayLevel(p + vd120, W); if (q3 < 0.5F) q3 = 1F - q3;
            float q4 = scan.getModuleGrayLevel(p - vd0, W); if (q4 < 0.5F) q4 = 1F - q4;
            float q5 = scan.getModuleGrayLevel(p - vd60, W); if (q5 < 0.5F) q5 = 1F - q5;
            float q6 = scan.getModuleGrayLevel(p - vd120, W); if (q6 < 0.5F) q6 = 1F - q6;
            return (q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4 + q5 * q5 + q6 * q6);
        }
    }
}
