using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Datamatrix
{
	internal class DMLPatternReader
    {
        ImageScaner scan;
        Encoding encoding;
        bool checkMirrored;

        public DMLPatternReader(ImageScaner scan, Encoding encoding, bool checkMirrored)
        {
            this.scan = scan;
            this.encoding = encoding;
            this.checkMirrored = checkMirrored;
        }

        //pA is the L pattern Edge. pB is the right end of the L pattern. 
        //pC is the top of the L pattern. pD is the estimated fourth corner.
        //This methods tries to adjust pD to the right position and reads the barcode.
        //Moves pD until the edges pD-pB and pD-pC lays on white pixels.
        public BarCodeRegion ReadBarcode(MyPointF pA, MyPointF pB, MyPointF pC, MyPointF pD)
        {
            MyPointF oD = pD;
            MyVectorF vdX = (pB - pA).Normalized;
            MyVectorF vdY = (pC - pA).Normalized;

            if (vdX.isNaN() || vdY.isNaN()) return null;

            int d = (int)((pB - pA).Length);
            if (d > scan.Width && d > scan.Height) return null;


            int comptExp, maxComptExp = (int)((pB - pA).Length * 0.15F); //maximum of pixels adjustments, proportional to the edge length. 
            if (maxComptExp < 5) maxComptExp = 5; //for short edges, set 5 pixels of adjustments.
            bool onWhite = false;
            int nPairs;
            float pH = 0F, pV = 0F;

          

            //expand D to lay on white
            comptExp = maxComptExp;
            onWhite = false;
            while (!onWhite && comptExp > 0)
            {
                int w;
                float r;
                onWhite = true;
                pV = scan.GetPercentOn(true, false, pD.Truncate(), Calc.Middle(pD, pB), out nPairs, out w, out r);
                if (pV > 0.2F && scan.In(pD + vdX)) { pD += vdX; onWhite = false; }
                pH = scan.GetPercentOn(true, false, pD.Truncate(), Calc.Middle(pD, pC), out nPairs, out w, out r);
                if (pH > 0.2F && scan.In(pD + vdY)) { pD += vdY; onWhite = false;  }
                comptExp--;
            }

            if (comptExp == 0) pD = oD;

            BarCodeRegion reg=null;
            float fRows=0f,fCols=0f;
            if (EstimateRowsCols(pA,pB,pC,pD, vdX, vdY, ref fCols, ref fRows)) 
            {
                reg = ReadBarcodeWithOrientation(pA, pB, pC, pD, vdX, vdY, fCols, fRows, maxComptExp);
                if (reg == null && checkMirrored) reg=ReadBarcodeWithOrientation(pA, pC, pB, pD, vdY, vdX, fRows, fCols, maxComptExp);
            }
            return reg;
        }

        BarCodeRegion ReadBarcodeWithOrientation(MyPointF pA, MyPointF pB, MyPointF pC, MyPointF pD, MyVectorF vdX, MyVectorF vdY, float fCols, float fRows, int maxComptExp)
        {
            BarCodeRegion reg=ReadBarcodeAdjusted(pA, pB, pC, pD, vdX, vdY, fCols, fRows);

            if (reg == null)
            {
                float moduleLength = (pB - pA).Length / fCols;
                if (moduleLength < 5)
                {
                    //for low resolution barcodes, try a small offset 
                    if (reg == null) reg = ReadBarcodeAdjusted(pA, pB, pC, pD - vdX - vdY, vdX, vdY, fCols, fRows);
                } 
                else
                {
                    bool onWhite;
                    int comptExp;
                    //for good resolution barcodes, try adjust B and C corners to lay on white
                    //since noisy edges have B and C on the black area

                    //expand B to lay on white
                    comptExp = maxComptExp;
                    onWhite = false;
                    while (!onWhite && comptExp > 0)
                    {
                        int w,nPairs;
                        float r;
                        onWhite = true;
                        float pH = this.scan.GetPercentOn(true, false, pB.Truncate(), Calc.Middle(pB, pA), out nPairs, out w, out r);
                        if (pH > 0.8F && scan.In(pB + vdY)) { pB -= vdY; onWhite = false; }
                        comptExp--;
                    }


                    //expand C to lay on white
                    comptExp = maxComptExp;
                    onWhite = false;
                    while (!onWhite && comptExp > 0)
                    {
                        int w,nPairs;
                        float r;
                        onWhite = true;
                        float pV = scan.GetPercentOn(true, false, pC.Truncate(), Calc.Middle(pC, pA), out nPairs, out w, out r);
                        if (pV > 0.8F && scan.In(pC + vdX)) { pC -= vdX; onWhite = false; }
                        comptExp--;
                    }

                    reg = ReadBarcodeAdjusted(pA, pB, pC, pD, vdX, vdY, fCols, fRows);
                }
            }
            return reg;
        }

        bool EstimateRowsCols(MyPointF pA, MyPointF pB, MyPointF pC, MyPointF pD, MyVectorF vdX, MyVectorF vdY, ref float fCols, ref float fRows) {
            //estimate rows and cols
            fCols = CountModules(pC, pD, vdY);
            if (fCols != 0F)
            {
                fRows = CountModules(pB, pD, vdX);
                if (fRows != 0F)
                {       
                    if (Calc.Around(fRows / fCols, 1.0F)) //for square DM's take average of them
                    {
                        float f = (fCols + fRows) / 2.0F;
                        fCols = fRows = f;
                    }
                    return true;
                }   
            }
            return false;
        }


        //Critical method to read the barcode. First tries to get the number of modules
        //of the DM. It is done for vertical and horizontal edges. 
        BarCodeRegion ReadBarcodeAdjusted(MyPointF pA, MyPointF pB, MyPointF pC, MyPointF pD, MyVectorF vdX, MyVectorF vdY, float fCols, float fRows)
        {
            int cols = (int)Math.Round(fCols);
            int rows = (int)Math.Round(fRows);

            //sample the upper-right module to see if it is a ECC200 DM or non ECC200
            Grid l = new Grid(cols, rows, pA, pC, pB, pD, true);
            MyPointF upRightCorner = l.GetSamplePoint(cols - 1, rows - 1);
            MyPoint upRC = (MyPoint)upRightCorner;
            bool isECC200 = !scan.isBlack(upRC.X, upRC.Y);

            //Find the best matching configuration, thus defining the final number of 
            //rows and cols
            foreach (Configuration cfg in Configuration.FindConfiguration(fCols, fRows, isECC200))
            {
                //initilize the object to calculate intermediate x,y sample coordinates
                Grid[][] loc = new Grid[][] { new Grid[] { new Grid(cfg.FullX, cfg.FullY, pA, pC, pB, pD, true) } };

                //and start the sampler. In ECC200 the result is an array of bytes (symbols)
                // For non ECC200 the result is a bitStream, crc...
                DMEncoder encoder = new DMEncoder(scan, cfg, loc);
                DMEncoded encoded = encoder.Encode(false);
                if (encoded == null) encoded = encoder.Encode(true);
                if (encoded != null && (!isECC200 || encoded.symbols != null))
                {
                    ABarCodeData[] r = null;
                    if (isECC200) r = DMDecoder.DecodeECC200Data(encoded.symbols, encoding);
                    else r = DMDecoder.DecodeNonECC200Data(encoded.bitArray, encoded.dataLength, encoded.dataFormat, encoded.crc, encoding);
                    if (r != null)
                    {
                        BarCodeRegion reg = new BarCodeRegion(pA, pB, pC, pD);
                        reg.Data = r;
                        reg.Confidence = encoded.Confidence;
                        return reg;
                    }
                }
            }
            return null;
        }

        //look for module width: scan a dashed DM side
        //PRE: a and b are the corners
        const int MIN_MODULES = 8; //actually the smallest DM is 9x9
        float CountModules(MyPointF a, MyPointF b, MyVectorF vdY)
        {
            int N = 10; //max iterations
            int most,nPairs;
            float regularity = 0F, tpcOn = 0F;
            MyPointF bestA = a, bestB = b;
            float length = (b - a).Length;
            int nGood = 0;
            float maxReg = 0f, maxOn = 0f;
            while (N > 0 && nGood < 20)
            {
                tpcOn = scan.GetPercentOn(true, true, a, b, out nPairs, out most, out regularity);
                float aproxCount = length * 2f / (float)most;
                if (nPairs>3 && aproxCount > MIN_MODULES && (regularity > maxReg || regularity == maxReg && tpcOn > maxOn))
                {
                    maxReg = regularity;
                    maxOn = tpcOn;
                    bestA = a; bestB = b;
                    nGood++;
                }
                /*
                if (regularity > 0.5F && Calc.Around(tpcOn, 0.5F, 0.15F))
                {
                    float dd = (float)Math.Abs(tpcOn - 0.5F);
                    if (dd < minDist) { minDist = dd; bestA = a; bestB = b; }
                    nGood++;
                }*/
                N--;
                a -= vdY; b -= vdY;
            }
            if (nGood != 0) return MeanColsOrRows(bestA, bestB);
            return 0F;
        }

        //Finds the most common module width between a and b. It is not an average, but
        // an average of the most common widths. 
        // ---- This is quite a dumb algorithm. Here can be errors. ----
        float MeanColsOrRows(MyPoint a, MyPoint b)
        {
            SortedDictionary<int, int> bars = new SortedDictionary<int, int>();
            int length = 0, n = 0, total = 0;
            bool current = false, first = false;
            Bresenham br = new Bresenham(a, b);

            //skip two pixels
            if (!br.End()) br.Next();
            if (!br.End()) br.Next();
            //
            if (!br.End())
            {
                first = current = scan.isBlack(br.Current.X, br.Current.Y);
                while (!br.End())
                {
                    bool next = scan.isBlack(br.Current.X, br.Current.Y);
                    if (next != current) //new transition
                    {
                        if (next == first) //only record each pair to avoid different saturation of black or white
                        {
                            if (bars.ContainsKey(n)) bars[n]++;
                            else bars.Add(n, 1);
                            total += 1; //increment total number of pairs
                            n = 0;
                        }
                        current = next;
                    }
                    n++;
                    br.Next();
                    length++;
                }
            }
            if (n > 0)
            {
                if (bars.ContainsKey(n)) bars[n]++;
                else bars.Add(n, 1);
                total += 1;
            }

            //find the most common
            int most = -1;
            foreach (int i in bars.Keys)
                if (most == -1 || bars[i] > bars[most]) most = i;
            //count repetitions of most
            ArrayList reps = new ArrayList(10);
            int nMost = bars[most];
            foreach (int i in bars.Keys) if (bars[i] == nMost) reps.Add(i);
            if (reps.Count % 2 != 0) most = (int)reps[reps.Count / 2];
            else most = ((int)reps[reps.Count / 2] + (int)reps[reps.Count / 2 - 1]) / 2;

            //average with neighbourhood depending on the approximated module width.
            //The larger is the module with, the more are the neighbours taken into account.
            float w0, w1, w2, w3, w4;
            w0 = w1 = w2 = w3 = w4 = 0.0F;
            w2 = (float)nMost;
            if (most > 2)
            {
                w1 = (float)(bars.ContainsKey(most - 1) ? bars[most - 1] : 0);
                w3 = (float)(bars.ContainsKey(most + 1) ? bars[most + 1] : 0);
                if (most > 10)
                {
                    w0 = (float)(bars.ContainsKey(most - 2) ? bars[most - 2] : 0);
                    w4 = (float)(bars.ContainsKey(most + 2) ? bars[most + 2] : 0);
                }
            }

            //w0 *= w0; w1 *= w1; w2 *= w2; w3 *= w3; w4 *= w4;
            float w = (float)((most - 2) * w0 + (float)(most - 1) * w1 + most * w2 +
                (most + 1) * w3 + (most + 2) * w4) / (float)(w0 + w1 + w2 + w3 + w4);

            //divide by 2 since we were counting pairs
            w = w / 2;
            return (float)length / w; //approximated number of cols/rows
        }

    }
}
