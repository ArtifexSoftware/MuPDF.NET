using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using BarcodeReader.Core.Code39;
using BarcodeReader.Core.Code93;

namespace BarcodeReader.Core.Common
{
    /// <summary>
    /// Base class to read linear 1D barcodes.
    /// Scans image, finds start and stop patterns.
    /// Finds barcode regions with different angles (from -45 to 45 degrees).
    /// Calls abstract methods GetBarSymbolReader and Decode to decode found barcode area.
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    abstract partial class LinearReader : SymbologyReader2D
    {
        internal float maxModuleSize;
        protected int minModulesPerBarcode;
        protected int maxModulesPerBarcode;
        protected bool stopAndStartPatternAreMirrored;

        protected ImageScaner Scan { get; private set; }

        /// <summary>
        /// To scan barcode in both forward and reverse order
        /// </summary>
        public bool ReverseEnabled { get; set; } = false;

        /// <summary>
        /// Region Of Interest.
        /// Reader finds barcodes only inside ROI.
        /// If empty - reads whole image.
        /// </summary>
        public Rectangle ROI { get; set; } = Rectangle.Empty;

        #region Parameters that affect on quality and speed of recognition

        //min QuietZone size (in mudules)
        public float MinQuietZone { get; set; } = 5; //5
        //Max distance Y between patterns to create new cluster
        public int MaxClusterDistanceY { get; set; } = 8; //8
        //Max distance X between patterns to create new cluster
        public int MaxClusterDistanceX { get; set; } = 5; //5
        //Min count of patterns in cluster to start recognition process for the barcode
        public int MinClusterSize { get; set; } = 13; // default = 15, for barcodes with small height = 6
        //Scan different lines inside barcode region
        public bool TryDifferentLinesOfBarcodeRegion { get; set; } = true;//enable for bad(crumpled, noised, skewed) barcodes
        //Different lines to scan region
        public float[] MidPoints { get; set; } = new float[] { 0.5f, 0.3f, 0.7f, 0.4f, 0.6f, 0.1f, 0.9f, 0.97f/*, 0.2f, 0.8f*/ };
        public float[] ModuleKoeffs { get; set; } = new float[] { 1f };
        //public float[] ModuleKoeffs { get; set; } = new float[] { 1f, 1.2f, 1.3f, 1.5f, 0.9f, 0.7f };
        //public float[] midPoints  { get; set; } = new float[] { 0.5f, 0.3f, 0.7f, 0.4f, 0.6f/*, 0.1f, 0.9f, 0.2f, 0.8f*/};
        public float MaxPatternSymbolDifference { get; set; } = 0.83f; // 0.65f, increse this value if black and white bars have too diff width
        public float MaxPatternAverageSymbolDifference { get; set; } = 0.55f; // 0.45f, increse this value if black and white bars have too diff width
        public float MaxBarcodeAngle { get; set; } = 60 * (float)Math.PI / 180;
        public float MaxSkewAngle = 20 * (float)Math.PI / 180;
        public float MaxSkew = 5f;
        public bool UseE { get; set; } = false;
        public float MaxReadError = 1f; // 1
        public float MinConfidence { get; set; } = 0.6f;//0.6
        public float MinRobustConfidence { get; set; } = 0.8f;
        public float MaxLeftAndRightModulesDifference = 1.5f;
        public float MaxBarLengthDifference = 5f;//for barcodes with max length of bar = 2, if more - you need to increse this value

        #endregion

        #region Barcode specific methods. Must be overrided in inherited class

        // Reader of symbols (must be assigned in inherited classes)
        internal BarSymbolReader Reader { get; set; }
        //minModulesPerBarcode - min modules per barcode (for example for EAN13 it is 95)
        internal abstract void GetParams(out int[] startPattern, out int[] stopPattern, out int minModulesPerBarcode, out int maxModulesPerBarcode);
        //decode specific barcode symbols
        internal abstract bool Decode(BarCodeRegion r, int[] row);

        #endregion

        #region ctors

        public LinearReader()
        {
            MinAllowedBarcodeSideSize = 20;
            MaxAllowedBarcodeSideSize = 6000;
        }

        public void CopyTo(LinearReader other)
        {
            other.MinAllowedBarcodeSideSize = MinAllowedBarcodeSideSize;
            other.MaxAllowedBarcodeSideSize = MaxAllowedBarcodeSideSize;
            other.Scan = Scan;
            other.ReverseEnabled = ReverseEnabled;
            other.ROI = ROI;

            other.MinQuietZone = MinQuietZone;
            other.MaxClusterDistanceY = MaxClusterDistanceY;
            other.MaxClusterDistanceX = MaxClusterDistanceX;
            other.MinClusterSize = MinClusterSize;
            other.TryDifferentLinesOfBarcodeRegion = TryDifferentLinesOfBarcodeRegion;
            other.MidPoints = MidPoints;
            other.MaxPatternSymbolDifference = MaxPatternSymbolDifference;
            other.MaxPatternAverageSymbolDifference = MaxPatternAverageSymbolDifference;
            other.MaxBarcodeAngle = MaxBarcodeAngle;
            other.UseE = UseE;


            other.ThresholdFilterMethodToUse = ThresholdFilterMethodToUse;
            other.ScanStep = ScanStep;
            other.MinAllowedBarcodeSideSize = MinAllowedBarcodeSideSize;
            other.MaxAllowedBarcodeSideSize = MaxAllowedBarcodeSideSize;
            other.ExpectedNumberOfBarcodes = ExpectedNumberOfBarcodes;
            other.StopOnFirstFoundBarcodeInTheRow = StopOnFirstFoundBarcodeInTheRow;
            other.Encoding = Encoding;
            other.TimeoutTimeInTicks = TimeoutTimeInTicks;
        }
        #endregion

        public override FoundBarcode[] Decode(BlackAndWhiteImage bwImage)
        {
            Scan = new ImageScaner(bwImage);
            return base.Decode(bwImage);
        }

        protected override FoundBarcode[] DecodeBarcode()
        {
            var result = new List<FoundBarcode>();

            //get specific parameters
            int[] startPattern;
            int[] stopPattern;

            GetParams(out startPattern, out stopPattern, out minModulesPerBarcode, out maxModulesPerBarcode);
            stopAndStartPatternAreMirrored = Utils.IsMirrored(startPattern, stopPattern);

            //pass in forward direction
            DecodeBarcode(result, startPattern, stopPattern, false);

            //pass in reverse direction
            if (ReverseEnabled && !stopAndStartPatternAreMirrored)
            {
                DecodeBarcode(result, Utils.Reverse(stopPattern), Utils.Reverse(startPattern), true);
            }

            // Sort results by Y, then X
            result.Sort((x, y) =>
            {
                int r = x.Rect.Top - y.Rect.Top;

                if (Math.Abs(r) < 50)
                    r = x.Rect.Left - y.Rect.Left;

                return r;
            });

            return result.ToArray();
        }

        /// <summary>
        /// Main method to find and recognize barcodes
        /// </summary>
        protected virtual void DecodeBarcode(List<FoundBarcode> result, int[] startPattern, int[] stopPattern, bool reverseReading)
        {
            //define area of scanning
            var fromX = 0;
            var toX = Scan.Width;
            var fromY = 0;
            var toY = Scan.Height;

            if (ROI != Rectangle.Empty)
            {
                fromX = Math.Max(0, ROI.Left);
                fromY = Math.Max(0, ROI.Top);
                toX = Math.Min(toX, ROI.Right);
                toY = Math.Min(toY, ROI.Bottom);
            }

            //line scanner
            LineScanner line = new LineScanner(fromX, toX);

            //calc max module size
            var imgSize = Math.Max(BWImage.Width, BWImage.Height);
            maxModuleSize = 1.4f * imgSize  / (float)minModulesPerBarcode;

            //create clusters stuffs
            var startClustersMap = new PatternCluster[BWImage.Width + 20];
            var startClusters = new LinkedList<PatternCluster>();
            var stopClustersMap = new PatternCluster[BWImage.Width + 20];
            var stopClusters = new LinkedList<PatternCluster>();

            //create pattern finders
            LinearPatternFinder startFinder;
            if (startPattern.Length > 0)
                startFinder = new StartPatternFinder(this, startPattern);
            else
                startFinder = new StartEmptyPatternFinder(this);

            LinearPatternFinder stopFinder;
            if (stopPattern.Length > 0)
                stopFinder = new StopPatternFinder(this, stopPattern);
            else
                //if stop pattern is empty, create special finder
                stopFinder = new StopEmptyPatternFinder(this);


            //scan lines, find patterns, create clusters
            for (int y = fromY; y < toY; y += ScanStep)
            {
                //calc bars
                line.FindBars(BWImage.GetRow(y), y);

                //find start patterns
                foreach (var p in startFinder.FindPattern(line))
                {
                    CreateClusterForPattern(startClustersMap, startClusters, p);
                    #if DEBUG
                    DebugHelper.DrawSquare(p.xIn, p.y, Color.MistyRose);
                    #endif
                }

                //find stop patterns
                foreach (var p in stopFinder.FindPattern(line))
                {
                    CreateClusterForPattern(stopClustersMap, stopClusters, p);
                    #if DEBUG
                    DebugHelper.DrawSquare(p.xIn, p.y, Color.LightBlue);
                    #endif
                }
            }

            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();

            //filter small clusters, generate common list of clusters
            var clusters = new List<PatternCluster>();
            PrepareClusters(clusters, startClusters, false);
            PrepareClusters(clusters, stopClusters, true);

            //sort clusters by X
            //clusters.Sort((c1, c2) => c1.A.X.CompareTo(c2.A.X));

            //sort clusters by Size
            clusters.Sort((c1, c2) => -c1.Count.CompareTo(c2.Count));

            //enumerate start clusters, find stop cluster for them, try to recognize
            for (int iStart=0; iStart < clusters.Count;iStart++)
            if (!clusters[iStart].IsStopPattern)//is it start pattern ?
            {
                var start = clusters[iStart];
                //choose opposite stop cluster
                for (int iStop = 0; iStop < clusters.Count; iStop++)
                if (clusters[iStop].IsStopPattern)//is it stop pattern ?
                if (clusters[iStop].OppositeCluster == null)//is not used ?
                {
                    var stop = clusters[iStop];

                    if (start.A.X > stop.A.X)
                        continue;//stop pattern con not be left from start pattern

                    //create candidate
                    var cand = new Candidate(start, stop);

                    //check and prepare candidate
                    if (!CheckAndPrepare(cand))
                        continue;//do not match

                    //try to recognize
                    var res = FindBarcode(cand, reverseReading);

                    //try reverse direction
                    if ((res == null || res.Confidence < 0.7f) && ReverseEnabled && stopAndStartPatternAreMirrored)
                    {
                        //make reverse pass
                        var reverseRes = FindBarcode(cand, !reverseReading);
                        if (res == null || res.Confidence < reverseRes.Confidence)
                            res = reverseRes;
                    }

                    //recognized?
                    if (res != null)
                    {
                        //check - is it already exists in result list?
                        //add to result list
                        AddIfNotExists(result, res);

                        if (res.Confidence > MinRobustConfidence)
                        {
                            //stop searching other barcodes with these clusters
                            clusters[iStart].OppositeCluster = clusters[iStop];
                            clusters[iStop].OppositeCluster = clusters[iStart];
                            break;
                        }
                    }
                }

                if (ExpectedNumberOfBarcodes > 0 && result.Count >= ExpectedNumberOfBarcodes)
                    return;

                if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();
            }
        }

        private void AddIfNotExists(List<FoundBarcode> result, FoundBarcode res)
        {
            var temp = new List<FoundBarcode>(result);

            foreach (var r in temp)
            {
                if (r.ParentRegion.IntersectsWith(res.ParentRegion, 5))
                {
                    if (r.Value == res.Value)
                        return;//do not add - exists

                    if (res.Confidence < r.Confidence * 0.9f)
                        return;//small confidence

                    //inside ?
                    var size1 = (r.ParentRegion.A - r.ParentRegion.B).Length;
                    var size2 = (res.ParentRegion.A - res.ParentRegion.B).Length;
                    if (size1 > size2)
                        return;//small size

                    //remove old
                    result.Remove(r);
                }
            }

            result.Add(res);
        }

        /// <summary>
        /// Try to read and recognize barcode in found region
        /// </summary>
        protected virtual FoundBarcode FindBarcode(Candidate cand, bool reverse)
        {
#if DEBUG
            //DebugHelper.DrawRegion(System.Drawing.Color.Red, region);
            //DebugHelper.Counter0++;
#endif
            var start = cand.From;
            var stop = cand.To;
            var region = cand.Region;
            var m = cand.ModuleEstimate;
            var iStart = start.Count / 2;
            var iStop = stop.Count / 2;

            var firstPass = true;

#if DEBUG
            //!!!!!
            var midFrom = start.GetMiddlePoint();
            var midTo = stop.GetMiddlePoint();
            DebugHelper.DrawArrow(midFrom.X, midFrom.Y, midTo.X, midTo.Y, Color.MistyRose);
#endif

            //scan different lines of region
            foreach (var mid in MidPoints)
            {
                //calc points
                iStart = (int)(start.Count  * mid);
                iStop = (int)(stop.Count * mid);

                var from = start.GetPoint(iStart);
                var to = stop.GetPoint(iStop);

                //swap if reverse
                if (reverse)
                Utils.Swap(ref from, ref to);

                //var from = MyPointF.Lerp(region.A, region.D, mid);
                //var to = MyPointF.Lerp(region.B, region.C, mid);

                var startPattern = start[iStart];
                var stopPattern = stop[iStop];

                //try to read and decode symbols between From and To
                if (ReadSymbols(region, startPattern, stopPattern, from, to, m, firstPass))
                    break; //successfully recognized

                if (!TryDifferentLinesOfBarcodeRegion)
                    break; //do not try other lines

                firstPass = false;
            }

            if (region.Confidence <= float.Epsilon)
                return null;//is not recognized

            //expand region
            Track(start, m, ref region.A, ref region.D);
            Track(stop, m, ref region.B, ref region.C);

            region.Reversed = reverse;

            return new FoundBarcode(GetBarCodeType(), region);
        }

        //try to read and decode symbols between From and To
        internal virtual bool ReadSymbols(BarCodeRegion region, Pattern startPattern, Pattern stopPattern, MyPoint from, MyPoint to, float module, bool firstPass)
        {
            float error, maxError, confidence;
            int[] row = ReadSymbols(startPattern, stopPattern, from, to, module, out error, out maxError, out confidence);

            if (error < MaxReadError)
            {
                if (confidence >= MinConfidence)
                if (confidence > region.Confidence)
                if (Decode(region, row)) //try to decode
                {
                    region.Confidence = confidence;
                    return true;
                }
            }
            return false;
        }

        //try to read symbols between From and To
        internal virtual int[] ReadSymbols(Pattern leftPattern, Pattern rightPattern, MyPointF from, MyPointF to, float module, out float error, out float maxError, out float confidence)
        {
            return Reader.Read(Scan, module, from, to, out error, out maxError, out confidence);
        }

        private void PrepareClusters(List<PatternCluster> result, LinkedList<PatternCluster> clusters, bool isStopPattern)
        {
            var minClusterSize = Math.Max(4, this.MinClusterSize / ScanStep);
            foreach (var cl in clusters)
            {
                //Calc adaptive cluster size
                //for thin lines (module < 1.5px) - reduce cluster size by 2 times
                var minSize = cl.AvgModule <= 1.5f ? minClusterSize / 2 : minClusterSize;
                if (minSize < 5) minSize = 5;
                if (cl.Count >= minSize)
                {
                    cl.CalcParams(isStopPattern);
                    result.Add(cl);

#if DEBUG
                    //!!!!
                    foreach (var p in cl)
                        DebugHelper.DrawSquare(isStopPattern ? p.xEnd : p.xIn, p.y,
                            isStopPattern ? Color.Blue : Color.Red);
                    //DebugHelper.DrawArrow(cl.A.X, cl.A.Y, cl.B.X, cl.B.Y, isStopPattern ? Color.Blue : Color.Red);
#endif
                }
            }
        }

        private void CreateClusterForPattern(PatternCluster[] clustersMap, LinkedList<PatternCluster> clusters, Pattern pattern)
        {
            var w = clustersMap.Length;
            //
            var myY = pattern.y;
            var myX = Math.Min(pattern.xIn + MaxClusterDistanceX / 2, w - 1);

            //find my cluster above
            PatternCluster foundCluster = null;
            var cl = clustersMap[myX];

            //check cluster and module size
            if (cl != null)
            {
                var last = cl.Last.Value;
                if (myY - last.y < MaxClusterDistanceY
                    && Math.Abs(pattern.xIn - last.xIn) < MaxClusterDistanceX
                    && Calc.Around(pattern.foundPattern.moduleLength, last.foundPattern.moduleLength, 1f)
                )
                {
                    //join to cluster
                    cl.AddLast(pattern);
                    foundCluster = cl;
                }
            }

            //create new cluster
            if(foundCluster == null)
            {
                foundCluster = new PatternCluster();
                foundCluster.AddLast(pattern);
                clusters.AddLast(foundCluster);
            }

            foundCluster.SumModule += pattern.foundPattern.moduleLength;

            //set my cluster to map
            var toX = Math.Min(w, pattern.xIn + MaxClusterDistanceX);
            for (int x = pattern.xIn; x < toX; x++)
            {
                clustersMap[x] = foundCluster;
            }
        }

        void Track(PatternCluster cluster, float module, ref MyPointF A, ref MyPointF B)
        {
            var i = (int)(cluster.Count / 2);
            var p = cluster[i];
            var x1 = p.xIn;
            var x2 = p.xEnd;
            var patternLengthInModuls = 0;
            if (x1 >= x2)
            {
                x1 = x2 - 1;
                patternLengthInModuls = 1;
            }
            else
            {
                patternLengthInModuls = (int)Math.Round((x2 - x1) / p.foundPattern.moduleLength);
            }

            MyPointF up, down;
            EdgeTrack et = new EdgeTrack(Scan);

            //first try using the first bar (thin bar)
            et.Track(new MyPoint(x1, p.y), new MyVector(-1, 0), module, true);

            //find the top and bottom points of the edge
            up = et.Up();
            down = et.Down();

            if (cluster.IsStopPattern)
            {
                MyVectorF n = (up - down).Normalized;
                n = new MyVectorF(-n.Y, n.X);
                up += n * module * patternLengthInModuls;
                down += n * module * patternLengthInModuls;
            }
            
            if ((up - down).LengthSq > (A - B).LengthSq)
            {
                A = up;
                B = down;
            }
        }
    }
}
