using System;
using System.Drawing;

namespace BarcodeReader.Core.Common
{
    /// <summary>
    /// Filter of candidates
    /// </summary>
    partial class LinearReader
    {
        private bool CheckAndPrepare(Candidate cand)
        {
            var start = cand.From;
            var stop = cand.To;

            //check angle
            var p1 = start.GetMiddlePoint();
            var p2 = stop.GetMiddlePoint();
            var axis = p2 - p1;
            var angle = axis.Angle;
            if (angle > MaxBarcodeAngle || angle < -MaxBarcodeAngle)
                return false;

            var m1 = start.AvgModule;
            var m2 = stop.AvgModule;
            if (m2 <= float.Epsilon) m2 = m1;//empty stop pattern
            if (m1 <= float.Epsilon) m1 = m2;//empty start pattern


            //check difference of modules of stop and start pattern
            if (m1 > 0 & m2 > 0)
            if (m1 / (m2 + 1) > MaxLeftAndRightModulesDifference || 
                m2 / (m1 + 1) > MaxLeftAndRightModulesDifference)
                return false;

            //calc module
            var m = (m1 + m2) / 2;
            m = m * (float)Math.Cos(angle);//correct average module
            cand.ModuleEstimate = m;

            //check distance between start and stop points
            var len = axis.Length;
            var modulesCount = len / m;
            if (modulesCount < minModulesPerBarcode * 0.7f ||
                modulesCount > maxModulesPerBarcode * 1.7f)
                return false;

            //check width of barcode
            if (len < MinAllowedBarcodeSideSize ||
               len > MaxAllowedBarcodeSideSize)
                return false;

            //check skewing
            if (!CheckSkewing(start, stop, angle))
                return false;

            //estimate module
            if (!CheckAndPrepareBars(cand))
                return false;

            //create region
            cand.Region = new BarCodeRegion(cand.From.A, cand.To.A, cand.To.B, cand.From.B);

#if DEBUG
            var from = cand.From.GetMiddlePoint();
            var to = cand.To.GetMiddlePoint();
            //DebugHelper.AddDebugItem(from.ToString(), line, from, to);
#endif

            return true;
        }

        private bool CheckSkewing(PatternCluster cl1, PatternCluster cl2, float axisAngle)
        {
            if (cl1.Count < cl2.Count)
                return CheckSkewing(cl2, cl1, axisAngle);

            var maxSkewAngle = MaxSkewAngle;

            //clac skewing
            //max distance
            var maxDist = cl1.Count * MaxSkew / 2;
            if (cl1.Length <= 15)
            {
                maxDist *= 2.5f;
                maxSkewAngle *= 1.2f;
            }

            //calc dist from ends of cl2 to axis
            var O = cl1.GetMiddlePoint();
            var A = new MyVectorF(cl2.A.X - O.X, cl2.A.Y - O.Y);
            var dA = A.NormalFromLine(cl1.Normal).Length;//distance from A to axis

            var B = new MyVectorF(cl2.B.X - O.X, cl2.B.Y - O.Y);
            var dB = B.NormalFromLine(cl1.Normal).Length;//distance from B to axis

            if (Math.Min(dA, dB) > maxDist) return false;

            //check skew angle
            if (Math.Abs(cl1.Normal.Angle - axisAngle) > maxSkewAngle)
                return false;
            if (Math.Abs(cl2.Normal.Angle - axisAngle) > maxSkewAngle)
                return false;

            return true;
        }

        private bool CheckAndPrepareBars(Candidate cand)
        {
            return true;

            //get middle points
            var from = cand.From.GetMiddlePoint();
            var to = cand.To.GetMiddlePoint();

            //calc bars 
            int BarsCount = 0;
            int SumWhiteLength = 0;
            int SumBlackLength = 0;
            int MinBarLength = int.MaxValue;
            int MaxBarLength = 0;

            var br = new Bresenham(from, to);
            var isBlack = Scan.isBlack(br.Current);
            var count = 0;
            var prevBar = 0;

            while (!br.End())
            {
                if (isBlack ^ Scan.isBlack(br.Current))
                {
                    var len = count - prevBar;
                    if (isBlack)
                        SumBlackLength += len;
                    else
                        SumWhiteLength += len;
                    
                    if (len > MaxBarLength) MaxBarLength = len;
                    if (len < MinBarLength) MinBarLength = len;

                    BarsCount++;
                    prevBar = count;
                    isBlack = !isBlack;
                }
                count++;
                br.Next();
            }

            //check difference between min and max bars
            var min = MinBarLength + 2f;
            var max = MaxBarLength + 2f;
            if (max / min > MaxBarLengthDifference)
                return false;

            cand.LineLength = count;
            cand.BarsCount = BarsCount;
            cand.MinBarLength = MinBarLength;
            cand.MaxBarLength = MaxBarLength;
            cand.SumWhiteLength = SumWhiteLength;
            cand.SumBlackLength = SumBlackLength;

            //cand.ModuleEstimate = cand.MaxBarLength / 2;//!!!!!! temp

            return true;
        }
    }
}