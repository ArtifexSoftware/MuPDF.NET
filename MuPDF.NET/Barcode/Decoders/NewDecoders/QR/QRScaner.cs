using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using SkiaSharp;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.QR
{

#if !OLD_QRReader

#if CORE_DEV
    public
#else
    internal
#endif
    partial class QRReader
    {
        /// <summary>
        /// Need to recognize mirrored QR codes?
        /// </summary>
        public bool CheckMirrors { get; set; } = true;

        //Scan image rows main step
        protected int scanRowStep = 1;

        //object that holds the bw image + methods to sample, follow vertices,...
        ImageScaner scan;

        //list of found QR and QRMicro codes found and correctly decoded.
        LinkedList<BarCodeRegion> candidates;

        FoundBarcode[] Scan()
        {
            scan = new ImageScaner(BWImage);
            candidates = new LinkedList<BarCodeRegion>();

#if DEBUG_IMAGE
            //BWImage.GetAsBitmap().Save(@"out.png");
#endif
            //to cache rows
            var rows = new XBitArray[height];
            for (int y = 0; y < height; y += scanRowStep)
                rows[y] = BWImage.GetRow(y);

            bool isMirroredFound = false;

            //scan
            Scan(rows, out isMirroredFound);

            //check mirrors
            //If some barcodes are mirrored => reverse rows and try scan again
            if (CheckMirrors && isMirroredFound)
            {
                //reverse rows
                for (int i = 0; i < rows.Length; i++)
                    rows[i].ReverseMe();

                //reset columns for reverse rows
                BWImage.ResetColumns();

                //mark no mirrored candidates
                foreach (var c in candidates)
                    c.Reversed = true;

                //repeat scan for mirrored
                Scan(rows, out isMirroredFound);

                //reverse mirrored candidates
                foreach (BarCodeRegion c in candidates)
                {
                    if (!c.Reversed)
                    {
                        //reverse candidate
                        c.A = ReverseX(c.A, scan.Width);
                        c.B = ReverseX(c.B, scan.Width);
                        c.C = ReverseX(c.C, scan.Width);
                        c.D = ReverseX(c.D, scan.Width);
                    }
                    else
                    {
                        c.Reversed = false;
                    }
                }
            }

            //
            var result = new List<FoundBarcode>();
            foreach (BarCodeRegion c in candidates)
            {
                //skip duplicates
                if (AlreadyExists(c))
                    continue;

                FoundBarcode foundBarcode = new FoundBarcode();
				foundBarcode.BarcodeFormat = SymbologyType.QRCode;
                
                StringBuilder data = new StringBuilder();
                if (c.Data!=null)
                foreach (ABarCodeData d in c.Data)
                {
                    string s=d.ToString();
                    data.Append(s);
                    if (s.StartsWith("]Q2\\MI"))
                    {
                        int pos1=s.IndexOf("\\MO1");
                        int pos2=s.IndexOf("\\MF001\\MY");
                        foundBarcode.StructureAppendIndex=Convert.ToInt32(s.Substring(6,pos1-6));
                        foundBarcode.StructureAppendCount=Convert.ToInt32(s.Substring(pos1+4,pos2-(pos1+4)));
                    }
                }
                foundBarcode.Value = data.ToString();
                foundBarcode.Color = SKColors.Blue;
                foundBarcode.Polygon = new SKPointI[5] { c.A, c.B, c.D, c.C, c.A };
				//byte[] pointTypes = new byte[5] { (byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line };
				//GraphicsPath path = new GraphicsPath(foundBarcode.Polygon, pointTypes);
				//foundBarcode.Rect = Rectangle.Round(path.GetBounds());
                foundBarcode.Rect = Utils.DrawPath(foundBarcode.Polygon);
                foundBarcode.Confidence = c.Confidence;
                result.Add(foundBarcode);
            }

            //sort by coordinates
            result.Sort((r1, r2) =>
            {
                var p1 = r1.Polygon[0];
                var p2 = r2.Polygon[0];
                var res = p1.Y.CompareTo(p2.Y);
                if (res == 0)
                    res = p1.X.CompareTo(p2.X);
                return res;
            });

            if (this.mergePartialBarcodes)
            {
                //group partial barcodes by their count
                var mergedResults = new List<FoundBarcode>();
                SortedDictionary<int, FoundBarcode[]> structureAppend = new SortedDictionary<int, FoundBarcode[]>();
                foreach (FoundBarcode f in result) 
                    if (f.StructureAppendCount!=-1)
                    {
                        if (!structureAppend.ContainsKey(f.StructureAppendCount))
                        {
                            structureAppend.Add(f.StructureAppendCount, new FoundBarcode[f.StructureAppendCount]);
                        }
                        if (f.StructureAppendIndex >= 1 && f.StructureAppendIndex <= f.StructureAppendCount)
                        {
                            structureAppend[f.StructureAppendCount][f.StructureAppendIndex - 1] = f;
                        }
                    }
                    else
                    {
                        mergedResults.Add(f);
                    }

                foreach(FoundBarcode[] mergedResult in structureAppend.Values) {
                    bool hasAllParts = true;
                    foreach (FoundBarcode f in mergedResult) if (f == null) hasAllParts = false;
                    if (hasAllParts)
                    {
                        FoundBarcode m = new FoundBarcode();
                        int minX=Int32.MaxValue, minY=Int32.MaxValue, maxX=Int32.MinValue, maxY=Int32.MinValue;
                        m.Value = "";
                        m.Confidence = 1f;
                        foreach (FoundBarcode f in mergedResult)
                        {
                            int pos2 = f.Value.IndexOf("\\MF001\\MY");
                            m.Value += f.Value.Substring(pos2+9);
                            this.updateMinMaxPolygon(f.Polygon, ref minX, ref minY, ref maxX, ref maxY);
                            m.Confidence *= f.Confidence;
                        }
                        m.Polygon = new SKPointI[5] { new SKPointI(minX, minY), new SKPointI(maxX, minY), new SKPointI(maxX, maxY), new SKPointI(minX, maxY), new SKPointI(minX, minY) };
                        m.Color = SKColors.Blue;
                        mergedResults.Add(m);
                    }
                    else
                    {
                        foreach (FoundBarcode f in mergedResult) if (f!=null) mergedResults.Add(f);
                    }
                }

                result = mergedResults; 
            }
            return (FoundBarcode[])result.ToArray();
        }

        private void Scan(XBitArray[] rows, out bool isMirroredFound)
        {
            var mirroredFound = false;

            var parallelSupported = BWImage.IsParallelSupported;
#if DEBUG
            //parallelSupported = false;
#endif

            var maxIndexY = height / scanRowStep;
            var minPartsize = 30;
            var threadCount = -1;

            if (!parallelSupported)
            {
                threadCount = 1;
                minPartsize = int.MaxValue;
            }

            //main loop to scan horizontal line
            Parallel.For(0, maxIndexY, threadCount, minPartsize, (part) =>
                {
                    var rowScanner = new QRReaderRow(scan, BWImage, candidates, DefaultEncoding == Encoding ? null : Encoding);

                    for (int iY = Math.Max(0, part.From - 5); iY < part.To; iY++)
                    {
                        if (ExpectedNumberOfBarcodes <= 0 || candidates.Count < ExpectedNumberOfBarcodes)
                        {
                            var y = iY * scanRowStep;
                            rowScanner.ScanRow(y, rows[y]);
                        }
                        // timeout check
                        if (IsTimeout())
                            break;
                    }
                    if (rowScanner.IsMirrored)
                        mirroredFound = true;
                }
            );

            // check for timeout
            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();

            isMirroredFound = mirroredFound;
        }

        private MyPointF ReverseX(MyPointF p, int scanWidth)
        {
            p.X = scanWidth - p.X - 1;
            return p;
        }

        void updateMinMaxPolygon(SKPointI[] p, ref int minX, ref int minY, ref int maxX, ref int maxY)
        {
            foreach (SKPointI q in p)
            {
                if (minX > q.X) minX = q.X;
                if (minY > q.Y) minY = q.Y;
                if (maxX < q.X) maxX = q.X;
                if (maxY < q.Y) maxY = q.Y;
            }
        }

        bool AlreadyExists(BarCodeRegion barcode)
        {
            var node = candidates.First;
            while (node != null)
            {
                if (node.Value == barcode)
                    break;

                if (node.Value.In((barcode.A + barcode.D) / 2))
                    return true;

                node = node.Next;
            }

            return false;
        }
    }
#endif

}
