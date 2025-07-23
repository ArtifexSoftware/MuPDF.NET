using BarcodeReader.Core.AustraliaPostCode;
using BarcodeReader.Core.IntelligentMail;
using BarcodeReader.Core.PostNet;
using BarcodeReader.Core.RoyalMail;
using BarcodeReader.Core.RoyalMailKIX;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BarcodeReader.Core.Common
{
    //Abstract class to find an horizontal finder, check if left side is blank, and then try
    // to read the barcode. Used for 4-state barcodes, IM, RM, KIX, PostCodeAus,or 2-state barcodes as PostNet
#if CORE_DEV
    public
#else
    internal
#endif
    abstract class Reader2D : SymbologyReader2D
    {
        protected bool startsWithBlack = true;
        protected ImageScaner scan;
        protected LinkedList<BarCodeRegion> candidates = new LinkedList<BarCodeRegion>(); //list of found barcodes

        abstract protected int getFinderNElements(); //number of black, or white bars of the finder
        abstract protected bool IsFinder(int[] widths, out float meanWidth); //check if widths are a valid finder
        abstract protected bool LeftFree(int x0, int y, float meanWidth); //check if finder left side is blank
        abstract protected BarCodeRegion FindBarcode(int x0, int y, float meanWidth); //try to read the barcode

        public bool StartsWithBlack { get { return startsWithBlack; } set { startsWithBlack = value; } }


        //main method: scans the image, row by row looking for the finder. Once found, call FindBarcode to try to read it.
		protected override FoundBarcode[] DecodeBarcode()
        {
#if DEBUG_IMAGE
            //image.Save(@"d:\out.png");
#endif
			scan = new ImageScaner(BWImage);
			candidates.Clear();

			for (int y = 0; y < BWImage.Height; y += ScanStep)
            {
                // timeout check
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();

                XBitArray row = BWImage.GetRow(y);
				int startX = 0, endX = BWImage.Width - 10;

                //skip black (if finder starts with white) or white(if black) pixels
                while (startX < endX && (row[startX] ^ (startsWithBlack))) startX++;

                //look for the finder
                int currentElement = 0, n = 0;
                int[] elementWidths = new int[getFinderNElements()];
                int symbolStart = startX;
                float mean;
                bool processingWhite = !startsWithBlack;
                for (int x = startX; x < endX; x++)
                {
                    if (row[x] ^ processingWhite) n++;
                    else
                    {
                        elementWidths[currentElement++] = n;
                        if (currentElement == getFinderNElements())
                        {
                            bool done = false; MyPoint p = new MyPoint(x, y);
                            foreach (BarCodeRegion r in candidates) if (r.In(p)) { done = true; break; }
                            if (!done && IsFinder(elementWidths, out mean))
                                if(LeftFree(symbolStart,y,mean))
                                {
#if FINDERS
                                    candidates.AddLast(new BarCodeRegion(new MyPoint(startX, y), new MyPoint(startX+2, y), new MyPoint(startX+2, y + 1), new MyPoint(startX, y + 1)));  
#else
                                    BarCodeRegion result = FindBarcode(symbolStart, y, mean);
                                    if (result != null) candidates.AddLast(result);
#endif
                                }
                            SkipTwoModules(ref symbolStart, elementWidths);
                            currentElement -= 2;
                        }
                        n = 1;
                        processingWhite = !processingWhite;
                    }
                }
            }

            //set up results
            FoundBarcode[] results = new FoundBarcode[candidates.Count];
            int nn = 0;
            foreach (BarCodeRegion r in candidates)
            {
                FoundBarcode f = new FoundBarcode();

				if (this is PostCodeReader)
					f.BarcodeType = SymbologyType.AustralianPostCode;
				else if (this is KIXReader)
					f.BarcodeType = SymbologyType.RoyalMailKIX;
				else if (this is RMReader)
					f.BarcodeType = SymbologyType.RoyalMail;
				else if (this is IMReader)
					f.BarcodeType = SymbologyType.IntelligentMail;
                else if (this is PostNetReader)
					f.BarcodeType = SymbologyType.PostNet;
				
                f.Polygon = new SKPoint[] { r.A, r.B, r.C, r.D, r.A };
                f.Color = Color.Blue;
                // Create an SKPath from the polygon
                var path = new SKPath();
                path.MoveTo(f.Polygon[0]);

                for (int i = 1; i < f.Polygon.Length; i++)
                    path.LineTo(f.Polygon[i]);

                path.Close();

                // Calculate bounds
                SKRect bounds = path.Bounds;

                // Assign rectangle (convert SKRect to System.Drawing.Rectangle if needed)
                f.Rect = new System.Drawing.Rectangle(
                    (int)Math.Floor(bounds.Left),
                    (int)Math.Floor(bounds.Top),
                    (int)Math.Ceiling(bounds.Width),
                    (int)Math.Ceiling(bounds.Height)
                );
                f.Value = (r.Data != null ? r.Data[0].ToString() : "?");
				f.Confidence = r.Confidence;
                results[nn++] = f;
            }
            return results;
        }                   
    }
}
