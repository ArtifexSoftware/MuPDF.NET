using System;
using System.Collections;
using System.Drawing;
using BarcodeReader.Core.Common;
using SkiaSharp;

namespace BarcodeReader.Core.MICR
{
    //This class receives a rotated region of the original image (i.e a small horizontal image)
    //and slices its parts again to find the right bounding boxes for each segment (Part) (since 
    //bounding boxes of rotated segments are oversized!). Then it finds a match for each segment
    //and, for too separated segments, try to read symbols in the middle. It also reads the first
    //and last non-numeric symbols of a MICR code, since they are splitted in different segments 
    //and never matched.
	internal class MICRSimpleReader
    {
        int minDigitCount;
        string spaceCharacter, unknownCharacter;
        Slicer slicer;
        MICR micrOcr;
        BlackAndWhiteImage img;

        public MICRSimpleReader(Slicer slicer, MICR micrOcr, int minDigitCount, string spaceCharacter, string unknownCharacter)
        {
            this.slicer = slicer;
            this.micrOcr = micrOcr;
            this.minDigitCount=minDigitCount;
            this.spaceCharacter=spaceCharacter;
            this.unknownCharacter=unknownCharacter;
        }

        public string Read(BlackAndWhiteImage img, float d)
        {
            ArrayList parts = slicer.GetParts(img);
            Segment[] sortedparts = new Segment[parts.Count];
            int ii = 0;
            foreach (Segment p in parts) { p.X = -p.XIn; sortedparts[ii++] = p; }
            Array.Sort(sortedparts);
            return Read(img, sortedparts, d);
        }

        public string Read(BlackAndWhiteImage img, Segment[] sortedparts, float d)
        {
            this.img = img;
            string[] chars = new string[sortedparts.Length];
            ArrayList decoded = new ArrayList();
            for (int i = 0; i < sortedparts.Length; i++)
            {
                SKRect rr = sortedparts[i].GetRectangle();
                string ch = micrOcr.GetChar(img, ref rr);
                chars[decoded.Count] = ch;
                if (ch != null) decoded.Add(sortedparts[i]);
            }
            string micr = null;
            if (decoded.Count > minDigitCount)
            {
                bool isWhite;
                Segment first = (Segment)decoded[0];
                micr = SorroundingChar(first, -d * 1.5F, d, out isWhite);

                Segment prev = null;
                int cont = 1, maxCont = 1;
                for (int i = 0; i < decoded.Count; i++)
                {
                    Segment p = (Segment)decoded[i];
                    if (prev != null)
                    {
                        float dist = p.Dist(prev);
                        if (dist > d * 1.2F)
                        {
                            float dd = d * 0.5F;
                            while (dd < dist - d)
                            {
                                string ch = SorroundingChar(prev, dd, d, out isWhite);
                                if (ch == null) ch = isWhite ? spaceCharacter : unknownCharacter;
                                micr += ch;
                                dd += d;
                            }
                            cont = 1;
                        }
                        else
                        {
                            cont++;
                            if (maxCont < cont) maxCont = cont;
                        } 
                    }
                    else cont = 1;
                    micr += chars[i];
                    prev = p;
                }
                if (maxCont < cont) maxCont = cont;
                if (maxCont < 3) micr = null;
                else micr += SorroundingChar(prev, d * 0.5F, d, out isWhite);
            }
            return micr;
        }


        //If to matched segments are too distant, tries to find intermediate symbols
        //based on the estimated symbols width (MICR are fixed pitch). Uses a crop
        //method to adjust the bounding box before matching it.
        static readonly int[] offsets = new int[] { 0, -2, +2 };
        public string SorroundingChar(Segment p, float d, float w, out bool isWhite)
        {
            isWhite = true;
            SKPointI center = p.Center;
            foreach (int off in offsets)
            {
                int xIn = center.X + off + (int)d;
                SKRect r = new SKRect(xIn, p.LU.Y, (int)(w - xIn - 1), p.Height- p.LU.Y);
                Crop(ref r);
                string ch = micrOcr.GetChar(img, ref r);
                if (ch != null) return ch;
                else if (r.Width > (int)w/2) isWhite = false;
            }
            return null;
        }

        public void Crop(ref SKRect r)
        {
            //return;
            int width = (int)r.Width;
            int height = (int)r.Height;
            int xIn = (int)r.Left, yIn = (int)r.Top, xEnd = (int)r.Right - 1, yEnd = (int)r.Bottom - 1;
            for (int i = 0; i < width; i++, xIn++) if (!isWhiteV(xIn, yIn, height)) break;
            for (int i = 0; i < width; i++, xEnd--) if (!isWhiteV(xEnd, yIn, height)) break;
            width = xEnd - xIn + 1;
            for (int i = 0; i < height; i++, yIn++) if (!isWhiteH(xIn, yIn, width)) break;
            for (int i = 0; i < height; i++, yEnd--) if (!isWhiteH(xIn, yEnd, width)) break;

            r.Left = xIn;
            r.Top = yIn;
            r.Right = xEnd + 1;
            r.Bottom = yEnd + 1;
        }

        bool isWhiteV(int x, int y, int h)
        {
            if (x < 0 || x >= img.Width) return true;
            int max = 2;
            int count = max;
            XBitArray col = img.GetColumn(x);
            for (int i = 0; i < h; i++, y++)
                if (col[y]) { if (--count == 0) return false; }
                else count = max;
            return true;
        }

        bool isWhiteH(int x, int y, int w)
        {
            if (y < 0 || y >= img.Height) return true;
            int max = 2;
            int count = max;
            XBitArray row = img.GetRow(y);
            for (int i = 0; i < w; i++, x++)
                if (row[x]) { if (--count == 0) return false; }
                else count = max;
            return true;
        }

    }
}
