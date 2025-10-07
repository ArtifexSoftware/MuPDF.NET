using System;
using System.Collections;
using System.Collections.Generic;
using SkiaSharp;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Aztec
{
#if CORE_DEV
    public
#else
    internal
#endif
    partial class AztecReader
    {
        //object that holds the bw image + methods to sample, follow vertices,...
        ImageScaner scan;
        //list of found codes found and correctly decoded.
        LinkedList<BarCodeRegion> candidates;
        //list of found and rejected finders, and codes to avoid exploring twice the same barcode
        LinkedList<BarCodeRegion> exclusion; 

        //patternFinder is used in the main scan loop (scanning horizontal lines).  
        //crossFinder is used for each horizontal pattern found, to check if it is also 
        //a pattern verically
        IPatternFinderNoiseRow patternFinder, crossFinder;
        PatternFinderNoise crossFinder2, crossFinder3;

        LinkedList<Pattern> foundPatterns;

        FoundBarcode[] Scan()
        {
            scan = new ImageScaner(BWImage);
            patternFinder = new PatternFinderNoiseRow(AztecFinder.finder, true, false, 2);
            //crossFinder uses the same hash table
            crossFinder = new PatternFinderNoiseRow(AztecFinder.finder, true, false, 2);
            crossFinder2 = new PatternFinderNoise(BWImage, AztecFinder.finder[0], false, 2);
            crossFinder3 = new PatternFinderNoise(BWImage, AztecFinder.finder[0], false, 2);
            candidates = new LinkedList<BarCodeRegion>();
            exclusion = new LinkedList<BarCodeRegion>();
            foundPatterns = new LinkedList<Pattern>();


#if DEBUG_IMAGE
			//bwSourceImage.GetAsBitmap().Save(@"out.png");
#endif
            //main loop to scan horizontal lines
            for (int y = 0; y < height && (ExpectedNumberOfBarcodes <= 0 || candidates.Count < ExpectedNumberOfBarcodes); y += scanRowStep)
            {
                // timeout check
                if (IsTimeout())
                    throw new SymbologyReader2DTimeOutException();

                ScanRow(y, BWImage.GetRow(y));
            }

            ArrayList result = new ArrayList();
            foreach (BarCodeRegion c in candidates)
            {
                FoundBarcode foundBarcode = new FoundBarcode();

                String data = "";
                if (c.Data != null) foreach (ABarCodeData d in c.Data) data += d.ToString();

				foundBarcode.BarcodeFormat = SymbologyType.Aztec;
				foundBarcode.Value = data;

                foundBarcode.Polygon = new SKPointI[5] { c.A, c.B, c.D, c.C, c.A };
                foundBarcode.Color = SKColors.Blue;
				//byte[] pointTypes = new byte[5] { (byte) PathPointType.Start, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line, (byte) PathPointType.Line };
				//GraphicsPath path = new GraphicsPath(foundBarcode.Polygon, pointTypes);
				//foundBarcode.Rect = Rectangle.Round(path.GetBounds());
                foundBarcode.Rect = Utils.DrawPath(foundBarcode.Polygon);
                foundBarcode.Confidence = c.Confidence;
                result.Add(foundBarcode);

            }
            return (FoundBarcode[])result.ToArray(typeof(FoundBarcode));
        }

        //Scans a horizontal line looking for finder patterns (101010101). 
        //For each finder pattern found checks if it is a pattern vertically.
        //Then tries to find the finder.
		private void ScanRow(int y, XBitArray row)
		{
			//look for the finder
			patternFinder.NewSearch(row);
            while (patternFinder.NextPattern() != null)
			{
				MyPoint a = new MyPoint(patternFinder.First, y);
				MyPoint b = new MyPoint(patternFinder.Last, y);
                MyPoint center = new MyPoint(patternFinder.Center, a.Y); //center of the mid black segment of the finder
                int xCenter=(a.X+b.X)/2;
				int d = xCenter - center.X;
				if (d < 0) d = -d;
                //if the center of the finder falls close to the center of the mid black segment of the finder
                if (Calc.Around(d, 0F, (float)(b.X-a.X)*finderMaxCentersDifference) || d<finderMaxCentersDistanceInPixels)
				{
					//Check if the same pattern was processed in the last row. 
                    Pattern p = new Pattern(null, a.X, b.X, a.Y);
					LinkedListNode<Pattern> prev = foundPatterns.Find(p);
					if (prev != null) prev.Value = p;
					else
					{
						foundPatterns.AddLast(p);

						//If the finder pattern is found in a place where another finder was found previously, it is skipped.
						bool done = false;
						foreach (BarCodeRegion c in exclusion)
							if (c.In(center)) { done = true; break; }

						if (!done)
						{
#if FIND_PATTERN
                        MyPoint Y = new MyPoint(0, 1);
                        SquareFinder f = new SquareFinder(a + Y,b + Y, a, b, 7);
                        candidates.AddLast(f);
#else
							//checks if a vertical pattern cross the horizontal pattern in the middle
							MyPoint pUp, pDown;
                            SmallAztecFactory factory = new SmallAztecFactory();
							AztecFinder finder = null;
							if (SquareFinder.CheckCrossPattern(scan, a, b, center, crossFinder, factory, out pUp, out pDown))
							{
								//calculates black - white threshold using the pixels in the pattern
                                scan.setBWThreshold(a,b);
                                finder = (AztecFinder)SquareFinder.IsFinder(scan, a, b, center, crossFinder2, crossFinder3, factory);
								if (finder != null)
								{
									exclusion.AddFirst(finder);
#if FIND_FINDER
									candidates.AddLast(finder);
#else
									AztecSymbol symbol = CountModules(scan, ref finder);
									if (symbol != null)
									{
										BitArray stream = null;
										float confidence = 1F;
										if (symbol.Type != AztecType.Rune) stream = ReadSymbol(symbol, out confidence);
										ABarCodeData[] data = DecodeData(symbol, stream);
										if (data != null)
										{
											finder.Data = data;
											finder.Confidence = confidence;
											candidates.AddFirst(finder);
											exclusion.AddFirst(finder);
										}
									}
#endif
								}
							}
#endif
						}
					}
				}
			}

			//clean old patterns
			Pattern.RemoveOldPatterns(foundPatterns, y);
		}

        AztecSymbol CountModules(ImageScaner scan, ref AztecFinder finder)
        {
#if DEBUG_IMAGE
            scan.Reset();
#endif
            bool[][] centralPoints = Common.Utils.NewBoolArray(15, 15);
            Grid centralGrid = new Grid(7, 7, finder.C, finder.A, finder.D , finder.B , true);
            centralGrid.ExtractPoints(scan, centralPoints, new MyPoint(-2, -2), new MyPoint(0, 0), 11, 11);
#if DEBUG_IMAGE
            scan.setPixel(finder.A, Color.Blue);
            scan.setPixel(finder.B, Color.Blue);
            scan.setPixel(finder.C, Color.Blue);
            scan.setPixel(finder.D, Color.Blue);
            scan.Save(@"outSamples.png");
#endif

            // determining if the symbol is compact or not
            int darkModuleCount = 0;
            for (int i = 0; i < 10; ++i)
            {
                if (centralPoints[0][i])
                {
                    darkModuleCount++;
                }
                if (centralPoints[i][10])
                {
                    darkModuleCount++;
                }
                if (centralPoints[10][10 - i])
                {
                    darkModuleCount++;
                }
                if (centralPoints[10 - i][0])
                {
                    darkModuleCount++;
                }
            }

            AztecSymbol symbol = null;
            if (darkModuleCount > 3)
            {
                symbol = AztecSymbol.GetCompactSymbol(centralPoints, finder);
            }

            if (symbol==null) //try 11x11
            {
                //centralGrid.ExtractPoints(scan, centralPoints, new MyPoint(-4, -4), new MyPoint(0, 0), 15, 15);
                //symbol = AztecSymbol.GetFullRangeSymbol(centralPoints);
                //if (symbol == null)
                {
                    //Recalc corners
                    MyPoint left = centralGrid.GetSamplePoint(-2, 3);
                    MyPoint right = centralGrid.GetSamplePoint(8, 3);
                    MyPoint top = centralGrid.GetSamplePoint(3, -2);
                    MyPoint bottom = centralGrid.GetSamplePoint(3, 8);

                    left = scan.NextTransition(left, new MyVectorF(-1F, 0F), finder.ModuleWidth, false);
                    right = scan.NextTransition(right, new MyVectorF(1F, 0F), finder.ModuleWidth, false);
                    top = scan.NextTransition(top, new MyVectorF(0F, -1F), finder.ModuleHeight, false);
                    bottom = scan.NextTransition(bottom, new MyVectorF(0F, 1F), finder.ModuleHeight, false);

                    BigAztecFactory factory = new BigAztecFactory();
                    finder = (AztecFinder)SquareFinder.IsFinder(scan, left, right, top, bottom, factory);
                    if (finder != null)
                    {
                        centralGrid = new Grid(11, 11, finder.C, finder.A, finder.D, finder.B, true);
#if DEBUG_IMAGE
                        scan.Reset();
                        scan.setPixel(finder.A, Color.Blue);
                        scan.setPixel(finder.B, Color.Blue);
                        scan.setPixel(finder.C, Color.Blue);
                        scan.setPixel(finder.D, Color.Blue);
#endif
                        centralGrid.ExtractPoints(scan, centralPoints, new MyPoint(-2, -2), new MyPoint(0, 0), 15, 15);

			symbol = AztecSymbol.GetFullRangeSymbol(centralPoints, finder);
                    }
                }
            }
#if DEBUG_IMAGE
            scan.Save(@"outSamples.png");
#endif

            if (symbol == null)
            {
                return null;
            }

            if (symbol.Type == AztecType.Rune) return symbol;

            int gridsPerSide = symbol.Grids.Length;
            // if we have only one grid, use the central one we estabilished earlier
            if (gridsPerSide == 1)
            {
                Grid grid = new Grid(finder.Center(), finder.ModuleRight, finder.ModuleDown);
                int N=symbol.SideModuleCount;
                grid.ExtractPointsRegular(scan, symbol.Bitarray, new MyPoint(-N/2, -N/2), new MyPoint(0,0), N, N);
                finder.A = grid.GetSamplePointRegular(-(float)N / 2F , (float)N / 2F);
                finder.B = grid.GetSamplePointRegular((float)N / 2F, (float)N / 2F);
                finder.C = grid.GetSamplePointRegular(-(float)N / 2F, -(float)N / 2F);
                finder.D = grid.GetSamplePointRegular((float)N / 2F, -(float)N / 2F);
            }
            else // setup subgrids otherwise, for each quadplane
            {
                int N = symbol.SideModuleCount;
                int M = (N - 1)/2;
                MyVectorF vdX = finder.ModuleRight; 
                MyVectorF vdY = finder.ModuleDown;
                int nGridsPerQuad=M/16 + (M%16!=0?1:0);
                int nGrids = nGridsPerQuad * 2 + 1;

                int[] mapCoord = new int[nGrids];
                MyPointF[][] alignment=new MyPointF[nGrids][];
                mapCoord[0] = 0; 
                int remainderModules = (N - 1 - (nGrids -3) * 16) / 2;
                for (int i = 0; i < nGrids; i++)
                {
                    if (i == 1) mapCoord[i] = remainderModules; 
                    else if (i>1) mapCoord[i]= mapCoord[i - 1] + 16;
                    alignment[i] = new MyPointF[nGrids];
                    for (int j = 0; j < nGrids; j++) alignment[i][j] = MyPointF.Empty;
                }
                mapCoord[nGrids - 1] = mapCoord[nGrids - 2] + remainderModules;

                MyPoint C = new MyPoint(nGridsPerQuad, nGridsPerQuad);
                MyPointF p0 = alignment[nGridsPerQuad][nGridsPerQuad] = finder.Center();
                float moduleWidth=finder.ModuleWidth;
                float moduleHeight=finder.ModuleHeight;

                //new grid using possibly rotated finder 
                Grid grid = new Grid(11, 11, finder.C, finder.A, finder.D, finder.B, true);

                FindGrids(p0, grid.GetSamplePoint(11, 5), vdX, vdY, moduleWidth, moduleHeight, M, alignment, C, new MyVector(1, 0));
                FindGrids(p0, grid.GetSamplePoint(5, 11), vdY, vdX, moduleHeight, moduleWidth, M, alignment, C, new MyVector(0, 1));
                FindGrids(p0, grid.GetSamplePoint(-1, 5), -vdX, vdY, moduleWidth, moduleHeight, M, alignment, C, new MyVector(-1, 0));
                FindGrids(p0, grid.GetSamplePoint(5, -1), -vdY, vdX, moduleHeight, moduleWidth, M, alignment, C, new MyVector(0, -1));

                FindQuadGrids(p0, -vdX, -vdY, moduleWidth, moduleHeight, nGridsPerQuad, alignment, C, new MyVector(-1, -1));
                FindQuadGrids(p0, vdX, -vdY, moduleWidth, moduleHeight, nGridsPerQuad, alignment, C, new MyVector(-1, 1));
                FindQuadGrids(p0, -vdX, vdY, moduleWidth, moduleHeight, nGridsPerQuad, alignment, C, new MyVector(1, -1));
                FindQuadGrids(p0, vdX, vdY, moduleWidth, moduleHeight, nGridsPerQuad, alignment, C, new MyVector(1, 1));

                finder.C = alignment[0][0] -vdX*0.5F -vdY*0.5F;
                finder.D = alignment[0][nGrids - 1] + vdX * 0.5F - vdY * 0.5F;
                finder.A = alignment[nGrids - 1][0] - vdX * 0.5F + vdY * 0.5F;
                finder.B = alignment[nGrids - 1][nGrids - 1] + vdX * 0.5F + vdY * 0.5F;
#if DEBUG_IMAGE
                scan.Save(@"outGrids.png");
#endif
                for (int y=1; y<nGrids; y++)
                    for (int x = 1; x < nGrids; x++)
                    {
                        int cols = mapCoord[x] - mapCoord[x - 1];
                        int rows = mapCoord[y] - mapCoord[y - 1];
                        Grid g = new Grid(cols, rows, alignment[y - 1][x - 1], alignment[y][x - 1], alignment[y - 1][x], alignment[y][x], false);
                        g.ExtractPoints(scan, symbol.Bitarray, new MyPoint(0,0), new MyPoint(mapCoord[x-1], mapCoord[y-1]), cols +1, rows +1);
                    }
            }
#if DEBUG_IMAGE
            scan.Save(@"outSamples.png");
#endif
            return symbol;
        }

        MyPointF FindCorner7x7(MyPointF p, MyVectorF vd, float mWidth, float mHeight, MyVectorF mRight, MyVectorF mDown)
        {
            p = p + vd / 2F; 
            p = scan.FindModuleCenter(p, true, mWidth, mHeight, mRight, mDown);
            p = p + vd; 
            p = scan.FindModuleCenter(p, false, mWidth, mHeight, mRight, mDown);
            return p;
        }


        void FindGrids(MyPointF p0, MyPointF p, MyVectorF vdX, MyVectorF vdY, float moduleWidth, float moduleHeight, int N, MyPointF[][] grids, MyPoint start, MyVector inc)
        {
            start = start + inc;
            p = p + vdX;
            MyPointF q = p;
            for (int i = 6; i < N; i++)
            {
#if DEBUG_IMAGE
                scan.setPixel(p, Color.Red);
#endif
                q = scan.FindModuleCenter(p, i % 2 != 0, moduleWidth, moduleHeight, vdX, vdY);
                if ((i + 1) % 16 == 0) { grids[start.Y][start.X] = q; start = start + inc; }
                //update vector length
                vdX = vdX.Normalized * (q - p0).Length / (float)(i+1);
                p = q + vdX;
#if DEBUG_IMAGE
                scan.setPixel(q, Color.Blue);
#endif
            }
            grids[start.Y][start.X] = q;
        }

        void FindQuadGrids(MyPointF p, MyVectorF vdX, MyVectorF vdY, float moduleWidth, float moduleHeight, int nGridsPerQuad, MyPointF[][] alignment, MyPoint C, MyVector inc)
        {
            for (int y = 0; y < nGridsPerQuad; y++)
            {
                for (int x = 0; x < nGridsPerQuad; x++)
                {
                    int xx = nGridsPerQuad + x * inc.X;
                    int yy = nGridsPerQuad + y * inc.Y;
                    MyPointF q = alignment[yy + inc.Y][xx + inc.X] = alignment[yy][xx + inc.X] + (alignment[yy + inc.Y][xx] - alignment[yy][xx]);
                    MyPointF r = MyPointF.Empty;
                    if (x!=nGridsPerQuad-1 && y!=nGridsPerQuad-1) 
                        r=alignment[yy + inc.Y][xx + inc.X] = scan.FindModuleCenter(q, true, moduleWidth, moduleHeight, vdX, vdY);

#if DEBUG_IMAGE
                    scan.setPixel(q, Color.Red);
                    if (!r.IsEmpty) scan.setPixel(r, Color.Blue);
#endif
                }
            }
        }


        BitArray ReadSymbol(AztecSymbol symbol, out float confidence)
        {
            confidence = 1F;
            if (symbol.Type == AztecType.Rune) return null; // nothing to do for aztec runes
            BitArray dataStream= new BitArray(symbol.BitCount);
            int index = 0;
            MyPoint point = new MyPoint(0, 0);
            MyVector neighborDir = new MyVector(1, 0);
            MyVector nextDir = new MyVector(0, 1);
            // side growth offset for layer width calculations
            int offset = symbol.Type == AztecType.FullRange ? 12 : 9;
            for (int l = symbol.LayerCount; l > 0; --l) // walk through the bit data, and asseble the original bitstream
            {
                for (int s = 0; s < 4; ++s)
                {
                    for (int i = 0; i < l * 4 + offset; ++i)
                    {
                        if (index < dataStream.Count)
                        {
                            dataStream[index++] = symbol.Bitarray[point.Y][point.X];
                            MyPoint neighbor = point + neighborDir;
                            if (symbol.IsFinderBit(neighbor))
                            {
                                neighbor += neighborDir;
                            }
                            dataStream[index++] = symbol.Bitarray[neighbor.Y][neighbor.X];
                            point += nextDir;
                            if (symbol.IsFinderBit(point))
                            {
                                point += nextDir;
                            }
                        }
                    }
                    point += nextDir;
                    if (symbol.IsFinderBit(point))
                    {
                        point += nextDir;
                    }
                    neighborDir = AztecUtils.RotateClockwise(neighborDir);
                    nextDir = AztecUtils.RotateClockwise(nextDir);
                }
                for (int i = 0; i < 2; ++i)
                {
                    do
                    {
                        point += new MyVector(1, 1);
                    } while (symbol.IsFinderBit(point));
                }
            }

            /*for (int i = 0; i < dataStream.Count; i++)
            {
                if (i % symbol.RsWordWidth == 0) Console.Write("|");
                Console.Write(dataStream[i] ? "o" : ".");
            }*/

            // slice up the bitstream into RS codewords
            int[] rsWords = AztecUtils.SliceBitStream(dataStream, symbol.RsWordWidth, symbol.BitCount%symbol.RsWordWidth);
            ReedSolomon corrector = new ReedSolomon(rsWords, rsWords.Length - symbol.DataCodewordCount,
                                                    symbol.RsWordWidth, AztecUtils.Polynoms[symbol.RsWordWidth], 1);
            corrector.Correct(out confidence); // correct any errors           

            // if confidence is zero 
            if (!corrector.CorrectionSucceeded)
                return null;

            // false-positive correction
            if (corrector.CorrectedData.Length < symbol.DataCodewordCount)
                return null;

            int[] dataWords = new int[symbol.DataCodewordCount];
            Array.Copy(corrector.CorrectedData, dataWords, symbol.DataCodewordCount);

	     /*RS rs = new RS(new GF(2, symbol.RsWordWidth, AztecUtils.Polynoms[symbol.RsWordWidth]), rsWords, rsWords.Length - symbol.DataCodewordCount, false);
            rs.correct();
            bool equals = true;
            for (int i = 0; i < rs.correctedData.Length; i++) if (rs.correctedData[i] != dataWords[i])
                { equals = false; break; }
            if (!equals)
            {
                confidence = 0.5F;
            }*/

            // create the error-corrected bitstream, without pad bits and rw words this time
            dataStream = AztecUtils.AssembleBitStream(dataWords, symbol.RsWordWidth);
            //dataStream = AztecUtils.AssembleBitStream(rs.correctedData, symbol.RsWordWidth);
            /*Console.WriteLine("Corrected:");
            for (int i = 0; i < dataStream.Count; i++)
            {
                if (i % symbol.RsWordWidth == 0) Console.Write("|");
                Console.Write(dataStream[i] ? "o" : ".");
            }*/
            return dataStream;
        }

        ABarCodeData[] DecodeData(AztecSymbol symbol, BitArray stream)
        {
            if (symbol.Type == AztecType.Rune) // only one byte for runes
                return new ABarCodeData[] { new Base256BarCodeData(new byte[] { (byte)symbol.ModeWord }, Encoding) };
            if (stream!=null) return AztecSymbolDecoder.DecodeText(stream, Encoding); // decode otherwise
            return null;
        }
    }
}
