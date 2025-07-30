using System;
using System.Collections;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Datamatrix
{
	internal class DMEncoded
    {
        public int[] symbols;
        public BitArray bitArray;
        public int dataLength;
        public NonECC200DataFormat dataFormat;
        public ushort crc;
        public float Confidence;
        public DMEncoded() { }
        public DMEncoded(int[] symbols) { this.symbols = symbols; }
        public DMEncoded(BitArray bitArray, int dataLength, NonECC200DataFormat dataFormat, ushort crc)
        {
            this.bitArray = bitArray;
            this.dataLength = dataLength;
            this.dataFormat = dataFormat;
            this.crc = crc;
        }
    }

	internal class DMEncoder
    {
        Configuration cfg;
        Grid[][] regions;
        bool[][] symbolData;
        int bitmapHeight, bitmapWidth;
        private ImageScaner scan = null;


        public DMEncoder(ImageScaner scan, Configuration cfg, Grid[][] regions)
        {
            this.scan = scan;
            this.bitmapHeight = scan.Height ;
            this.bitmapWidth = scan.Width;

            this.cfg = cfg;
            this.regions = regions;

        }

        float[] GetWidths(MyPoint a, MyPoint b, int nModules)
        {
            float moduleLength = (a - b).Length / nModules;
            int minLength=(int)Math.Round(moduleLength*0.5f);
            Bresenham br = new Bresenham(a, b);
            while (!br.End() && !scan.isBlack(br.Current)) br.Next();

            bool processingBlack = true, isNoise = false;
            int n = 0, N=0, l=0;
            int[] widths = new int[nModules];
            while (!br.End() && N<nModules)
            {
                l++;
                if (scan.isBlack(br.Current) ^ processingBlack)
                {
                    if (isNoise) { 
                        isNoise = false;
                        n++;
                    }
                    else if (n >= minLength)
                    {
                        widths[N++] = n;
                        n = 1;
                    }
                    else
                    {
                        isNoise = true;
                        n++;
                    }
                    processingBlack = !processingBlack;
                }
                else n++;
                br.Next();
            }
            if (N < nModules && n > 0) widths[N++] = n;

            float fl = (float)l, fModules = (float)nModules;
            float[] coords=new float[nModules];
            if (N < nModules)
                for (int i = 0; i < nModules; i++) coords[i] = (float)(i) / (float)nModules;
            else
            {
                float sum = 0f;
                for (int i = 0; i < nModules; i++)
                {
                    coords[i] = fModules * sum / fl;
                    sum += (float)widths[i];
                }
            }
            return coords;
        }

        // Read in the symbol from the image according to the available configuration(s). Once the bits are read,
        // try to perform error correction. If the correction was unsuccessful, try the next configuration.
        public DMEncoded Encode(bool adaptiveSampling)
        {
#if DEBUG_IMAGE
            scan.Reset();
#endif
            symbolData = new bool[cfg.FullDataY][];
            for (int y = 0; y < cfg.FullDataY; y++)
                symbolData[y] = new bool[cfg.FullDataX];

            Grid dmLocation = regions[0][0];

            float[] widths = null, heights = null;
            if (adaptiveSampling)
            {
                //Prepare adaptive sampling
                MyPoint a = dmLocation.GetSamplePoint(0f, (float)cfg.FullY - 0.5f);
                MyPoint b = dmLocation.GetSamplePoint(cfg.FullX, (float)cfg.FullY - 0.5f);
                widths = GetWidths(a, b, cfg.FullX);
                MyPoint va = dmLocation.GetSamplePoint((float)cfg.FullX - 0.5f, 0f);
                MyPoint vb = dmLocation.GetSamplePoint((float)cfg.FullX - 0.5f, cfg.FullY);
                heights = GetWidths(va, vb, cfg.FullY);
            }

            //Sample each region            
            for (int ry = 0; ry < cfg.RegionCountY; ++ry)
            {
                for (int rx = 0; rx < cfg.RegionCountX; ++rx)
                {
                    float[][] regionPoints = new float[cfg.SubY][];
                    for (int y = 0; y < cfg.SubY; ++y)
                    {
                        MyPoint P0 = new MyPoint(rx * cfg.SubX, ry * cfg.SubY);
                        regionPoints[y] = new float[cfg.SubX];
                        for (int x = 0; x < cfg.SubX; ++x)
                        {
                            MyPointF point=adaptiveSampling?dmLocation.GetSamplePoint(widths[P0.X + x] + 0.5f, heights[P0.Y + y] + 0.5f):
                                dmLocation.GetSamplePoint(P0.X + x, P0.Y + y);
                            float c = scan.getSample(point,0f);
                            regionPoints[y][x] = c;
                        }
                    }

                    // fill the data matrix
                    for (int y = 1; y < cfg.SubY-1 ; ++y)
                    {
                        for (int x = 1; x < cfg.SubX -1; ++x)
                        {
                            symbolData[ry * (cfg.SubY - 2) + y - 1][rx * (cfg.SubX - 2) + x - 1] = (regionPoints[y][x] < 0.5F);
                        }
                    }
                }
            }

#if DEBUG_IMAGE
            /*scan.Save(@"outscan.png");
            for (int i = symbolData.Length-1; i >=0 ; i--)
            {
                bool[] r=symbolData[i];
                for (int j = 0; j < r.Length; j++)
                    Console.Write(r[j] ? "X" : " ");
                Console.WriteLine();
            }
            scan.Reset();*/
#endif

            if (cfg.Type == DatamatrixType.ECC200) return EncodeECC200();
            else if (cfg.Type == DatamatrixType.NonECC200) return EncodeNonECC200();
            return null;
        }


        DMEncoded EncodeECC200()
        {
            // get the placement grid for the given module size
            ModulePlacementECC200 ecc200=new ModulePlacementECC200();
            MyPoint[][] placementGrid = ecc200.GetPlacementGridP(cfg.FullDataY, cfg.FullDataX);
            if (placementGrid == null) return null;

            // fill the array of symbols
            DMEncoded result = new DMEncoded(new int[cfg.FullSymbolCount]);
            for (int y = 0; y < cfg.FullDataY; ++y)
            {
                for (int x = 0; x < cfg.FullDataX; ++x)
                {
                    if (placementGrid[y][x].IsEmpty)
                    {
                        continue;
                    }
                    int symbolIndex = placementGrid[y][x].X;
                    int bitIndex = placementGrid[y][x].Y;
                    byte bitValue = (byte)(symbolData[y][x] ? 1 : 0);
                    result.symbols[symbolIndex] = result.symbols[symbolIndex] | (bitValue << bitIndex);
                }
            }

            // do the RS correction on the symbols
            int[][] dataBlocks = cfg.GetDataBlocks();
            int[][] rsBlocks = cfg.GetRSBlocks();
            float meanConfidence = 0F, confidence;
            int successfulDecodes = 0;

            // run 2 tries (2nd one is for 144x144 barcodes only)
            for (int tries = 0; tries < 2; tries++)
            {
                // if we are runing second time then we should check for 
                // possible need for RS data code inversion, see #159
                if (tries == 1)
                {
                    // see #159
                    // [0123456789]
                    // becomes 
                    // [234567801]
                    // copying 2 first bytes as last 2 bytes in the dest array

                    // check if we have configurations with 144x144 DM size (10 or more blocks)
                    int blockCount = cfg.ReedSolomonBlocks;
                    if (blockCount >= 10)
                    {
                        int dataLengthOffset = cfg.DataSymbolCount;

                        int numOfIterations = (cfg.FullSymbolCount - dataLengthOffset) / blockCount;

                        for (int n = 0; n < numOfIterations; n++)
                        {
                            
                            // save first 2 codewords from the block
                            int c1 = result.symbols[dataLengthOffset + n * blockCount];
                            int c2 = result.symbols[dataLengthOffset + n * blockCount + 1];
                            // copying 2 first bytes as last 2 bytes in the dest array
                            Array.Copy(result.symbols, dataLengthOffset + n * blockCount + 2, result.symbols, dataLengthOffset + n * blockCount, blockCount - 2);
                            // set 1st and 2nd symbols from saved vars
                            // as last and pre-last codewords
                            result.symbols[dataLengthOffset + n * blockCount + blockCount - 2] = c1;
                            result.symbols[dataLengthOffset + n * blockCount + blockCount - 1] = c2;
                            
                        }
                    }
                    else return null; // if not supported size for byte swaps with RS then exit
                }

                successfulDecodes = 0;
                for (int i = 0; i < dataBlocks.Length; ++i)
                {
                    // DM ECC200 can have multiple RS blocks, handle each of them separately
                    int[] data = Select(result.symbols, dataBlocks[i]);
                    int[] rs = Select(result.symbols, rsBlocks[i]);
                    int[] rsData = new int[data.Length + rs.Length];
                    Array.Copy(data, 0, rsData, 0, data.Length);
                    Array.Copy(rs, 0, rsData, data.Length, rs.Length);
                    ReedSolomon corrector = new ReedSolomon(rsData, rs.Length, 8, 301, 1);
                    corrector.Correct(out confidence);
                    meanConfidence += confidence;

                    if (!corrector.CorrectionSucceeded)
                    {
                        // reset successful decodes!
                        successfulDecodes = 0;
                        break;
                    }

                    successfulDecodes++;
	                Common.Utils.ReverseSelect(corrector.CorrectedData, result.symbols, dataBlocks[i]);
                }

                if (successfulDecodes > 0)
                    break;
                else
                    continue;
            }

            if (successfulDecodes == 0)
                return null;

            meanConfidence /= (float)dataBlocks.Length;
            result.Confidence = meanConfidence;

            // happy case: correction suceeded, proceed to the next step
            int[] finalData = new int[cfg.DataSymbolCount];
            Array.Copy(result.symbols, 0, finalData, 0, finalData.Length);
            result.symbols = finalData;

            // check if we have valid data
            bool nonZero = false;
            foreach (int i in result.symbols) 
                if (i != 0)
                    nonZero = true;
            if (!nonZero) 
                return null;
            return result;
        }
        
        DMEncoded EncodeNonECC200()
        {
            // load up the placement grids (if haven't done so already)
            ModulePlacementNonECC200.Init("mpnonecc200.dat");
            ushort[][] placementGrid = ModulePlacementNonECC200.GetPlacementGrid(cfg.FullY-2, cfg.FullX-2);
            if (placementGrid == null) return null;

            // get the binary data
            DMEncoded result = new DMEncoded();
            result.bitArray = new BitArray((cfg.FullX-2) * (cfg.FullY-2));
            for (int y = 0; y < cfg.FullY-2; ++y)
            {
                for (int x = 0; x < cfg.FullX-2; ++x)
                {
                    ushort bitIndex = placementGrid[y][x];
                    result.bitArray[bitIndex] = symbolData[y][x];
                }
            }

            // unrandomise it
            result.bitArray = UnRandomise(result.bitArray);
            // try to correct it
            ConvolutionDecoder corrector = new ConvolutionDecoder(result.bitArray);
            corrector.Correct();
            if (!corrector.CorrectionSucceeded) return null;
            result.Confidence = 1F;

            // extract user data & other paramterers
            result.bitArray = ExtractNonECC200Data(corrector.CorrectedData, out result.dataFormat, out result.crc, out result.dataLength);
            if (result.bitArray == null) return null;
            return result;
        }


        // Selects the elements at the given indices in the source array.
        public static int[] Select(int[] sourceArray, int[] indices)
        {
            int[] result = new int[indices.Length];
            for (int i = 0; i < indices.Length; ++i)
            {
                result[i] = sourceArray[indices[i]];
            }

            return result;
        }

        // search order of the regions in case of multi-region DM
        public static readonly MyPoint[] RegionSearchOrder = new MyPoint[]
                                                        {
                                                            new MyPoint(0, 0), new MyPoint(1, 0), new MyPoint(0, 1),
                                                            new MyPoint(1, 1), new MyPoint(2, 0), new MyPoint(2, 1),
                                                            new MyPoint(0, 2), new MyPoint(1, 2), new MyPoint(2, 2),
                                                            new MyPoint(3, 0), new MyPoint(3, 1), new MyPoint(3, 2),
                                                            new MyPoint(0, 3), new MyPoint(1, 3), new MyPoint(2, 3),
                                                            new MyPoint(3, 3), new MyPoint(4, 0), new MyPoint(4, 1),
                                                            new MyPoint(4, 2), new MyPoint(4, 3), new MyPoint(0, 4),
                                                            new MyPoint(1, 4), new MyPoint(2, 4), new MyPoint(3, 4),
                                                            new MyPoint(4, 4), new MyPoint(5, 0), new MyPoint(5, 1),
                                                            new MyPoint(5, 2), new MyPoint(5, 3), new MyPoint(5, 4),
                                                            new MyPoint(0, 5), new MyPoint(1, 5), new MyPoint(2, 5),
                                                            new MyPoint(3, 5), new MyPoint(4, 5), new MyPoint(5, 5)
                                                        };

        // used to un/randomise the protected bitstream
        private static readonly byte[] MasterRandomBytestream = new byte[]
                                                   {
                                                       0x05, 0xff, 0xc7, 0x31, 0x88, 0xa8, 0x83, 0x9c, 0x64, 0x87, 0x9f,
                                                       0x64, 0xb3, 0xe0, 0x4d, 0x9c, 0x80, 0x29, 0x3a, 0x90, 0xb3, 0x8b,
                                                       0x9e, 0x90, 0x45, 0xbf, 0xf5, 0x68, 0x4b, 0x08, 0xcf, 0x44, 0xb8,
                                                       0xd4, 0x4c, 0x5b, 0xa0, 0xab, 0x72, 0x52, 0x1c, 0xe4, 0xd2, 0x74,
                                                       0xa4, 0xda, 0x8a, 0x08, 0xfa, 0xa7, 0xc7, 0xdd, 0x00, 0x30, 0xa9,
                                                       0xe6, 0x64, 0xab, 0xd5, 0x8b, 0xed, 0x9c, 0x79, 0xf8, 0x08, 0xd1,
                                                       0x8b, 0xc6, 0x22, 0x64, 0x0b, 0x33, 0x43, 0xd0, 0x80, 0xd4, 0x44,
                                                       0x95, 0x2e, 0x6f, 0x5e, 0x13, 0x8d, 0x47, 0x62, 0x06, 0xeb, 0x80,
                                                       0x82, 0xc9, 0x41, 0xd5, 0x73, 0x8a, 0x30, 0x23, 0x24, 0xe3, 0x7f,
                                                       0xb2, 0xa8, 0x0b, 0xed, 0x38, 0x42, 0x4c, 0xd7, 0xb0, 0xce, 0x98,
                                                       0xbd, 0xe1, 0xd5, 0xe4, 0xc3, 0x1d, 0x15, 0x4a, 0xcf, 0xd1, 0x1f,
                                                       0x39, 0x26, 0x18, 0x93, 0xfc, 0x19, 0xb2, 0x2d, 0xab, 0xf2, 0x6e,
                                                       0xa1, 0x9f, 0xaf, 0xd0, 0x8a, 0x2b, 0xa0, 0x56, 0xb0, 0x41, 0x6d,
                                                       0x43, 0xa4, 0x63, 0xf3, 0xaa, 0x7d, 0xaf, 0x35, 0x57, 0xc2, 0x94,
                                                       0x4a, 0x65, 0x0b, 0x41, 0xde, 0xb8, 0xe2, 0x30, 0x12, 0x27, 0x9b,
                                                       0x66, 0x2b, 0x34, 0x5b, 0xb8, 0x99, 0xe8, 0x28, 0x71, 0xd0, 0x95,
                                                       0x6b, 0x07, 0x4d, 0x3c, 0x7a, 0xb3, 0xe5, 0x29, 0xb3, 0xba, 0x8c,
                                                       0xcc, 0x2d, 0xe0, 0xc9, 0xc0, 0x22, 0xec, 0x4c, 0xde, 0xf8, 0x58,
                                                       0x07, 0xfc, 0x19, 0xf2, 0x64, 0xe2, 0xc3, 0xe2, 0xd8, 0xb9, 0xfd,
                                                       0x67, 0xa0, 0xbc, 0xf5, 0x2e, 0xc9, 0x49, 0x75, 0x62, 0x82, 0x27,
                                                       0x10, 0xf4, 0x19, 0x6f, 0x49, 0xf7, 0xb3, 0x84, 0x14, 0xea, 0xeb,
                                                       0xe1, 0x2a, 0x31, 0xab, 0x47, 0x7d, 0x08, 0x29, 0xac, 0xbb, 0x72,
                                                       0xfa, 0xfa, 0x62, 0xb8, 0xc8, 0xd3, 0x86, 0x89, 0x95, 0xfd, 0xdf,
                                                       0xcc, 0x9c, 0xad, 0xf1, 0xd4, 0x6c, 0x64, 0x23, 0x24, 0x2a, 0x56,
                                                       0x1f, 0x36, 0xeb, 0xb7, 0xd6, 0xff, 0xda, 0x57, 0xf4, 0x50, 0x79,
                                                       0x08, 0x0
                                                   };


        private static readonly BitArray MasterRandomBitstream = new BitArray(Common.Utils.ReverseBitsPerByte(MasterRandomBytestream));

        // restore the protected bitstream using the MasterRandomBitstream
        public static BitArray UnRandomise(BitArray randomStream)
        {
            BitArray masterChunck = new BitArray(MasterRandomBitstream);
            masterChunck.Length = randomStream.Length;
            return randomStream.Xor(masterChunck);
        }

        // extracts the user data from the unprotected bitstream of non-ecc200
        public static BitArray ExtractNonECC200Data(BitArray bitArray, out NonECC200DataFormat dataFormat, out ushort crc, out int dataLength)
        {
            dataFormat = NonECC200DataFormat.Base11;
            crc = 0;
            dataLength = 0;
            if (bitArray.Length < 30)
            {
                return null;
            }

            int formatId = 0;
            for (int i = 0; i < 5; ++i)
            {
                if (bitArray[i])
                {
                    formatId |= (1 << (4 - i));
                }
            }

            dataFormat = (NonECC200DataFormat)formatId;
            if (formatId < 0 || formatId > 5)
            {
                return null;
            }

            for (int i = 0; i < 16; ++i)
            {
                if (bitArray[i + 5])
                {
                    crc = (ushort)(crc | (1 << i));
                }
            }

            for (int i = 0; i < 9; ++i)
            {
                if (bitArray[i + 21])
                {
                    dataLength |= (1 << i);
                }
            }

            return Common.Utils.BitArrayPart(bitArray, 30, bitArray.Length - 30);
        }

    }
}
