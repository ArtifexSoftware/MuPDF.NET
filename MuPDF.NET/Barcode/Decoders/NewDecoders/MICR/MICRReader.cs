using System;
using System.Collections;
using System.Drawing;
using SkiaSharp;
using System.Runtime.InteropServices;
using BarcodeReader.Core.Common;
using MuPDF.NET;

namespace BarcodeReader.Core.MICR
{
#if CORE_DEV
    public
#else
    internal
#endif
    class MICRReader : SymbologyReader2D
    {
        //Max accepted differencte between the height of symbols in a MICR code
        protected float maxHeightDifferenceBetweenSymbols = 0.3F;

        //Max accepted differencte between the width of symbols in a MICR code
        protected float maxWidthDifferenceBetweenSymbols = 0.3F;

        ImageScaner scan;
        Slicer slicer;
        MICR micrOcr;
        MICRSimpleReader simpleReader;
        ArrayList result;

        int width, height;
        int minRad = 5, //minRad = 15, // changed to 5 on 19 Feb 2014 by Eugene
            maxRad = 100,
            minDist = 1,
            minDigitCount = 15;
        bool ocr = true, 
            hough = true,
            restrictHorizontal = false;  //restrictHorizontal=true; // changed to false on 19 Feb 2014 by Eugene
        string spaceCharacter = " ", unknownCharacter = "?";

        SKBitmap imgHough;
        public SKBitmap GetImage() { return imgHough; }

        int mainAngle;

        public MICRReader() : base()
        {
            // change the default block filte to .BlockOld filter type
            base.ThresholdFilterMethodToUse = ThresholdFilterMethod.BlockOld;
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.MICR;
		}

		protected override FoundBarcode[] DecodeBarcode()
        {
			this.width = BWImage.Width;
			this.height = BWImage.Height;
			scan = new ImageScaner(BWImage);
            // calling Slicer with AspectMax = 0f (that means no need to check for max aspect ratio as we need it in OMR)
            slicer = new Slicer(minDist, minRad, maxRad, 450, 3F / 9F, 0f , false);
            micrOcr = new MICR();
            simpleReader = new MICRSimpleReader(slicer, micrOcr, minDigitCount, spaceCharacter, unknownCharacter);
            result = new ArrayList();
#if DEBUG_IMAGE
            //BWImage.GetAsBitmap().Save(@"out.png");
#endif
            //Get an array of all segments meeting minDist, minRad, maxRad restrictions
			ArrayList segments = slicer.GetParts(BWImage);

            // timeout check
            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();

            // Process all segments
            FoundBarcode[] res = hough ? HoughScan(segments) : ScanParts(segments);

            // Calculate confidence
            foreach (FoundBarcode barcode in res)
            {
                int countOfUnknown = barcode.Value.Split(UnknownCharacter.ToCharArray()).Length - 1;
                if (countOfUnknown > 0)
                    barcode.Confidence = (barcode.Value.Length - countOfUnknown) / (float) barcode.Value.Length;
            }

            return res;
        }


        FoundBarcode[] HoughScan(ArrayList segments)
        {
            //Add all segments and get a list of hough cells sorted by the number of segments.
            //A hough cell defines a line based on its angle and distance, and  contains 
            //all segments that are near that line.
            Hough h = new Hough(width, height, 100, height / 4);
            foreach (Segment p in segments)
                h.Add(p.Center, 1, p); // weight could be p.Width * p.Height to sort by size
            imgHough = h.GetImage();
            HoughCell[] cells = h.Sort();

            //for each aligned subset of segments
            //mainAngle: once a MICR code has been found, the rest must be +/- in the same direction.
            //      So, initially mainAngle=-1 and when the first code is found is assigned to its angle
            //MIN_DIGITS: minimun number of digits of an acceptable MICR code.            
            mainAngle = restrictHorizontal ? 50 : -1;
            foreach (HoughCell c in cells)
                if (c.Objects.Count > minDigitCount && (mainAngle == -1 || Calc.Around(c.Angle, mainAngle, 3)))
                {
                    //If any of the segments of this cell belongs to a previously read code, skip it.
                    //This is important because there are several hough cells crossing the same code.
                    bool scaned = false;
                    foreach (Segment s in c.Objects) if (s.Scaned) { scaned = true; break; }

                    if (!scaned) 
                    {
                        //Sort all parts from left to right
                        Segment[] sortedsegments = new Segment[c.Objects.Count];
                        int ii = 0;
                        foreach (Segment s in c.Objects) { s.X = h.XDist(c, s.Center); sortedsegments[ii++] = s; }
                        Array.Sort(sortedsegments);

                        FindClusters(sortedsegments, c.Angle);

                        if (this.ExpectedNumberOfBarcodes>0 && result.Count >= this.ExpectedNumberOfBarcodes) break;
                    }

                    // timeout check
                    if (IsTimeout())
                        throw new SymbologyReader2DTimeOutException();
                }
            return (FoundBarcode[])result.ToArray(typeof(FoundBarcode));
        }

        //Find subsets of segments that are close enough to be a possible code, and 
        //have a similiar height, i.e. find clusters of segments.
        //It also calculates the mean distance between symbols, needed to scan 
        //intermediate non numeric symbols. 
        void FindClusters(Segment[] sortedsegments, int angle)
        {
            float D = 0F;
            int iIn = 0, iEnd = 0, count = 1, dCount = 0;
            Segment prev = sortedsegments[0];
            int meanHeight = sortedsegments[1].Height; //meanHeight is used to check height similarity
            for (int i = 1; i < sortedsegments.Length && (this.ExpectedNumberOfBarcodes <= 0 || result.Count < this.ExpectedNumberOfBarcodes); i++)
            {
                Segment segment = sortedsegments[i];
                float mH = (float)meanHeight / (float)(count);
                //if the current segment has a similar height from previous ones
                if (Calc.Around(mH / (float)segment.Height, 1F, maxHeightDifferenceBetweenSymbols) ||
                    Calc.Around((float)segment.Height / mH, 1F, maxHeightDifferenceBetweenSymbols))
                {
                    float d = prev.X - segment.X;
                    float tmpD = (dCount > 0 ? D / (float)dCount : d);
                    //if the current segment is close enough from the previous one
                    if (d < 15F * tmpD)
                    {
                        iEnd = i;
                        meanHeight += segment.Height;
                        count++;
                        //if there is no white space between the current segment and the previous
                        //used their distance to calculate the average distance between symbols
                        if (Calc.Around(d / tmpD, 1F, maxWidthDifferenceBetweenSymbols) ||
                            Calc.Around(tmpD / d, 1F, maxWidthDifferenceBetweenSymbols))
                        {
                            dCount++;
                            D += d;
                        }
                    }
                    //if the current segment if far away from the previous one, finalize the 
                    //cluster and start a new one.
                    else
                    {
                        DecodeCluster(sortedsegments, iIn, iEnd, tmpD, angle);
                        iIn = iEnd = i;
                        count = 1; meanHeight = sortedsegments[i].Height;
                        dCount = 0; D = 0F;
                    }
                    prev = segment;
                }
                //if the current segment's height is too different from the previous one
                else if (segment.Height > mH) //stop if bigger
                {
                    DecodeCluster(sortedsegments, iIn, iEnd, D / (float)dCount, angle);
                    iIn = iEnd = i;
                    count = 1; meanHeight = sortedsegments[i].Height;
                    dCount = 0; D = 0F;
                    prev = segment;
                }
                //else consider as noise, just skip
            }
            DecodeCluster(sortedsegments, iIn, iEnd, D / (float)dCount, angle);
        }

        //Given a set of segments (with similar height and close enough from each other)
        //the estimated distance between symbols, and the direction of the segments (angle)
        //find the matching symbol for each segment.
        //Since non-numeric symbols usually lead to a more than one segment, they are 
        //rarely matched. Now, we will scan empty areas to find those non-numeric symbols.
        void DecodeCluster(Segment[] group, int iIn, int iEnd, float d, int angle)
        {
            if (iEnd - iIn > minDigitCount) //decode
            {
                string micr = null;
                SKPointI[] rect = null;
                Segment a = group[iIn];
                Segment b = group[iEnd];

                if (restrictHorizontal)
                {
                    Segment[] parts = new Segment[iEnd - iIn + 1];
                    for (int i = iIn, j = 0; i <= iEnd; i++, j++) parts[j] = group[i];
                    if (ocr) micr = simpleReader.Read(BWImage, parts, d);
                    else micr = "";
                    rect = new SKPointI[] { a.LU, a.LD, b.RD, b.RU, a.LU};
                }
                else
                {
                    //find bounding box aligned in the segments direction.
                    //We also check that all segments are vertically aligned. We calculate
                    //the mean height and then compare this mean height with the height of 
                    //the bounding box. 
                    BBox bbox = new BBox((MyPoint)a.Center, (MyPoint)b.Center - (MyPoint)a.Center, d);
                    int blackPixelCount = 0;
                    float sumHeight = 0; int countH = 0;
                    for (int i = iIn; i <= iEnd; i++)
                    {
                        Segment p = group[i];

                        float maxY = float.MinValue, minY = float.MaxValue;
                        bbox.Update(p.LU, ref maxY, ref minY);
                        bbox.Update(p.LD, ref maxY, ref minY);
                        bbox.Update(p.RU, ref maxY, ref minY);
                        bbox.Update(p.RD, ref maxY, ref minY);

                        float hh = maxY - minY;
                        if (countH == 0) { sumHeight = hh; countH = 1; }
                        else
                        {
                            float tmpH = sumHeight / (float)countH;
                            if (Calc.Around(tmpH, hh, tmpH * 0.25F))
                            {
                                sumHeight += hh;
                                countH++;
                            }
                            else if (hh > tmpH) { sumHeight = hh; countH = 1; }
                        }
                        blackPixelCount += p.BlackPixelCounter;
                    }
                    float meanH = sumHeight / (float)countH;

                    //if mean height and bounding box are not similar exit.
                    if (!Calc.Around(bbox.GetHeight(), meanH, meanH * 0.5F))
                        return;
                    rect = bbox.GetBBox(); 
                    if (ocr)
                    {
                        //resample the bounding box to a horizontal rectangle
                        SKRect r = bbox.GetRectangle();
                        if (r.Height < 8) r.Bottom = r.Top+8;
                        
						using (SKBitmap rotated = new SKBitmap((int)r.Width, (int)r.Height, SKColorType.Rgb888x, SKAlphaType.Opaque))
						{
                            /*
							BitmapData data = rotated.LockBits(new Rectangle(0, 0, r.Width, r.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
							int stride = data.Stride;
							byte[] scanBytes = new byte[stride];
							IntPtr ptr = data.Scan0;
							for (int y = 0; y < r.Height; y++)
							{
								for (int x = 0, xx = 0; x < r.Width; x++)
								{
									MyPointF p = bbox.GetPoint(x, y);
									int gray = scan.isBlack(p) ? 0 : 255; //sample a point using 1 pixel. Seems to be enough (and faster).
									//int gray = (int)(scan.getSample(p) * 255F); //sample a point using 4 pixel neighbours
									if (gray > 255) gray = 255;
									scanBytes[xx++] = scanBytes[xx++] = scanBytes[xx++] = (byte)gray;
								}
								Marshal.Copy(scanBytes, 0, ptr, stride);
								ptr = new IntPtr(ptr.ToInt64() + stride);
							}
							rotated.UnlockBits(data);
                            */
                            for (int y = 0; y < (int)r.Height; y++)
                            {
                                for (int x = 0; x < (int)r.Width; x++)
                                {
                                    MyPointF p = bbox.GetPoint(x, y);
                                    int gray = scan.isBlack(p) ? 0 : 255; // or use scan.getSample(p) * 255F for interpolated sample
                                    if (gray > 255) gray = 255;

                                    // Write RGB triplet
                                    rotated.SetPixel(x, y, new SKColor((byte)gray, (byte)gray, (byte)gray));
                                }
                            }
#if DEBUG_IMAGE
							//rotated.Save(@"d:\part.png");
#endif

                            //rescan horizontal image
                            using (BlackAndWhiteImage bwr = new BlackAndWhiteImage(rotated, 1, ThresholdFilterMethodToUse, BWImage.ThresholdLevelAdjustment))
							{
#if DEBUG_IMAGE
								//bwr.GetAsBitmap().Save(@"d:\partBW.png");
#endif
								micr = simpleReader.Read(bwr, d);
							}
						}
                    }
                    else micr = "";
                }

                if (micr != null)
                {
                    if (ocr)
                    {
                        //set the main angle to avoid scanning hough cells of different angles.
                        if (result.Count == 0) { mainAngle = angle; }
                        //mark segments as used, to avoid read the same code more than once.
                        for (int i = iIn; i <= iEnd; i++) group[i].Scaned = true;
                    }
                    FoundBarcode foundBarcode = new FoundBarcode();
					foundBarcode.BarcodeFormat = SymbologyType.MICR;
					foundBarcode.Value = micr;
                    
					foundBarcode.Polygon = rect;
                    foundBarcode.Color = SKColors.Blue;

                    foundBarcode.Rect = new SKRect(rect[0].X, rect[0].Y, rect[3].X, rect[1].Y);

					result.Add(foundBarcode);
                }
            }
        }

        //debug method to show all segments, and matching symbol.
        FoundBarcode[] ScanParts(ArrayList parts)
        {
            foreach (Segment p in parts)
                if (p != null && (p.Width > minRad || p.Height > minRad) && 
                    (p.Width < maxRad && p.Height < maxRad))
                {
                    String data = "";
                    SKRect r = p.GetRectangle();
                    if (ocr) data = micrOcr.GetChar(BWImage, ref r);
                    FoundBarcode foundBarcode = new FoundBarcode();
					foundBarcode.BarcodeFormat = SymbologyType.MICR;
                    if (data != null)
                    {
                        p.SetRectangle(r);
                        foundBarcode.Color = SKColors.Blue;
                    }
                    else
                    {
                        data = "?";
                        foundBarcode.Color = SKColors.Orange;
                    }
					foundBarcode.Value = data;
                    foundBarcode.Polygon = p.GetBBox();
                    SKPointI[] bBox = p.GetBBox();
                    foundBarcode.Rect = new SKRect(bBox[0].X, bBox[0].Y, bBox[3].X, bBox[1].Y);
                    result.Add(foundBarcode);
                }
            return (FoundBarcode[])result.ToArray(typeof(FoundBarcode));
        }
        
        public int MinRad { get { return minRad; } set { minRad = value; } }
        public int MaxRad { get { return maxRad; } set { maxRad = value; } }
        public int MinDist { get { return minDist; } set { minDist = value; } }
        public int MinDigitCount { get { return minDigitCount; } set { minDigitCount = value; } }
        public bool OCR { get { return ocr; } set { ocr = value; } }
        public bool Hough { get { return hough; } set { hough = value; } }
        public bool RestrictHorizontal { get { return restrictHorizontal; } set { restrictHorizontal = value; } }
        public string SpaceCharacter { get { return spaceCharacter; } set { spaceCharacter = value; } }
        public string UnknownCharacter { get { return unknownCharacter; } set { unknownCharacter = value; } }
    }

}
