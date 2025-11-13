using System;

namespace BarcodeReader.Core.QR
{
    class QRSymbol
    {
        public readonly bool isMicroQR;

        public readonly int Version;

        public readonly int SideModuleCount;

        public readonly int CodewordCount;

        public int ECCodewordCount;

        public int DataCodewordCount;

        public readonly MyPoint[,] AlignmentPatternCoordinates;

        public readonly int[] patternCoords;

        public readonly bool[][] MaskedBitarray;

        public readonly bool[][] UnmaskedBitarray;

        public readonly int[] RawCodewords;

        private readonly bool[][] exclusionMask;

        public MyPointF[][] align;
        public float[][] moduleW;
        public float[][] moduleH;


        public ErrorCorrectionLevel ECLevel;

        public byte MaskCode;

        /// <summary>
        /// QR code is mirrorred
        /// </summary>
        public bool IsMirrored { get; private set; }


        public QRSymbol(int version)
        {
            Version = version;
            isMicroQR = Version > 40;
            SideModuleCount = isMicroQR ? 9 + (Version - 40) * 2 : 17 + 4 * Version;
            CodewordCount = QRUtils.CodewordCount[Version - 1];
            RawCodewords = new int[CodewordCount];

            if (!isMicroQR)
            {
                // generate the location of alignment patterns
                patternCoords = QRUtils.AlignmentPatternLocations[Version - 1];
                int patternSideCount = patternCoords.Length;
                AlignmentPatternCoordinates = new MyPoint[patternSideCount, patternSideCount];
                for (int y = 0; y < patternSideCount; ++y)
                {
                    for (int x = 0; x < patternSideCount; ++x)
                    {
                        AlignmentPatternCoordinates[y, x] = new MyPoint(patternCoords[x], patternCoords[y]);
                    }
                }
            }

            MaskedBitarray = new bool[SideModuleCount][];
            UnmaskedBitarray = new bool[SideModuleCount][];
            exclusionMask = new bool[SideModuleCount][];
            for (int i = 0; i < SideModuleCount; ++i)
            {
                MaskedBitarray[i] = new bool[SideModuleCount];
                UnmaskedBitarray[i] = new bool[SideModuleCount];
                exclusionMask[i] = new bool[SideModuleCount];
            }
        }

        public bool EnrichFormatInformation()
        {
            var dist = 0;
            var distMirror = 0;
            Int16 formatInfo = GetFormatInformation(false, out dist);
            GetFormatInformation(true, out distMirror);//try mirrored

            //is it mirrored image ?
            if (distMirror < dist && distMirror <= 3)
            {
                IsMirrored = true;
                return false;
            }

            if (formatInfo == 0)
            {
                return false;
            }

            if (!isMicroQR) // QR code
            {
                formatInfo ^= QRUtils.FormatMask;
                formatInfo >>= 10;
                ECLevel = QRUtils.ErrorCorrectionLevels[(formatInfo >> 3)];
                MaskCode = (byte)(formatInfo & 0x07);
            }
            else // Micro QR code
            {
                formatInfo ^= QRUtils.FormatMaskMicro;
                formatInfo >>= 10;
                ECLevel = QRUtils.MicroErrorCorrectionLevels[(formatInfo >> 2)];
                MaskCode = (byte)(formatInfo & 0x03);
            }

            if (Version != (int)MicroQRVersion.M1 && ECLevel == ErrorCorrectionLevel.DetectionOnly)
            {
                return false;
            }

            ECCodewordCount = ECLevel == ErrorCorrectionLevel.DetectionOnly ? 2 : QRUtils.ErrorCorrectionCodewordCount[Version - 1][(int)ECLevel];
            if (ECCodewordCount < 0) return false;
            DataCodewordCount = CodewordCount - ECCodewordCount;

            return true;
        }

        private Int16 GetFormatInformation(bool mirror, out int distance)
        {
            MyPoint[] formatInfoLocation = Version <= 40
                                               ? QRUtils.PrimaryFormatInfoLocation
                                               : QRUtils.MicroFormatInfoLocation;
            int[] formatData = Version <= 40 ? QRUtils.FormatData : QRUtils.FormatDataMicro;
            int formatInfo = ReadMaskedArray(formatInfoLocation, mirror);
            formatInfo = formatData[QRUtils.FindClosestMatchIndex(formatData, formatInfo, out distance)];
            if (distance > 3 && Version <= 40)
            {
                formatInfoLocation = new MyPoint[15];
                for (int i = 0; i < 8; ++i)
                {
                    formatInfoLocation[i] = new MyPoint(SideModuleCount - 1 - i, 8);
                }
                for (int i = 0; i < 7; ++i)
                {
                    formatInfoLocation[i + 8] = new MyPoint(8, SideModuleCount - 7 + i);
                }

                formatInfo = ReadMaskedArray(formatInfoLocation, mirror);
                formatInfo = formatData[QRUtils.FindClosestMatchIndex(formatData, formatInfo, out distance)];
                if (distance > 3)
                {
                    return 0;
                }
            }

            return (Int16)formatInfo;
        }

        private int ReadMaskedArray(MyPoint[] points, bool mirror)
        {
            int formatInfo = 0;
            for (int i = 0; i < 15; ++i)
            {
                var ii = mirror ? 14 - i : i;
                MyPoint bitCoords = points[ii];
                //Console.WriteLine("("+bitCoords.X+","+bitCoords.Y+")->"+MaskedBitarray[bitCoords.Y][bitCoords.X]);
                if (MaskedBitarray[bitCoords.Y][bitCoords.X])
                {
                    formatInfo |= (1 << i);
                }
            }

            return formatInfo;
        }

        private void Unmask()
        {
            MaskFunction maskFunction = isMicroQR ? QRUtils.MicroMasks[MaskCode] : QRUtils.Masks[MaskCode];
            for (int y = 0; y < SideModuleCount; ++y)
            {
                for (int x = 0; x < SideModuleCount; ++x)
                {
                    UnmaskedBitarray[y][x] = MaskedBitarray[y][x] ^ maskFunction(x, y);
                }
            }
        }

        public void ReadCodewords()
        {
            Array.Clear(RawCodewords, 0, RawCodewords.Length);

            Unmask();
            GenerateExclusionMask();
            CodeWalker cw = new CodeWalker(SideModuleCount, exclusionMask, isMicroQR);
#if DEBUG
            double[][] moduledata = new double[SideModuleCount][];
            for (int i = 0; i < moduledata.Length; i++)
            {
                moduledata[i] = new double[SideModuleCount];
            }
#endif
            for (int i = 0; i < CodewordCount; ++i)
            {
                int firstBit = 0;
                if (Version == (int)MicroQRVersion.M1 && i == 2
                    || Version == (int)MicroQRVersion.M3 && ECLevel == ErrorCorrectionLevel.L && i == 10
                    || Version == (int)MicroQRVersion.M3 && ECLevel == ErrorCorrectionLevel.M && i == 8)
                {
                    firstBit = 4;
                }
                for (int b = 7; b >= firstBit; --b)
                {
                    MyPoint coord = cw.NextFreePosition();
                    if (UnmaskedBitarray[coord.Y][coord.X])
                    {
                        RawCodewords[i] |= (1 << b);
                    }
#if DEBUG
                    if (coord.X >= 0 && coord.Y >= 0)
                        moduledata[coord.Y][coord.X] = (i * 90) % 255;
#endif
                }
            }
#if DEBUG
            //Debug.joinBitmap = Common.ImageDataToImage(moduledata);
#endif
        }

        private void GenerateExclusionMask()
        {
            // Upper left locator pattern + primary format info
            for (int y = 0; y <= 8; ++y)
            {
                for (int x = 0; x <= 8; ++x)
                {
                    exclusionMask[y][x] = true;
                }
            }

            if (isMicroQR)
            {
                for (int i = 0; i < SideModuleCount; ++i)
                {
                    exclusionMask[0][i] = true;
                    exclusionMask[i][0] = true;
                }

                return;
            }
            // else:

            // Upper right and bottom left locator patterns + secondary format info
            for (int y = 0; y <= 8; ++y)
            {
                for (int x = SideModuleCount - 8; x < SideModuleCount; ++x)
                {
                    exclusionMask[y][x] = true;
                    exclusionMask[x][y] = true;
                }
            }

            // fine version information
            if (Version > 6)
            {
                for (int y = 0; y <= 5; ++y)
                {
                    for (int x = SideModuleCount - 11; x < SideModuleCount - 8; ++x)
                    {
                        exclusionMask[y][x] = true;
                        exclusionMask[x][y] = true;
                    }
                }
            }

            // timing pattern
            for (int x = 8; x < SideModuleCount - 8; ++x)
            {
                exclusionMask[6][x] = true;
                exclusionMask[x][6] = true;
            }

            // alignment patterns
            int alignmentCount = AlignmentPatternCoordinates.GetLength(0);
            for (int y = 0; y < alignmentCount; ++y)
            {
                for (int x = 0; x < alignmentCount; ++x)
                {
                    if ((x == 0 && y == 0) || (x == 0 && y == alignmentCount - 1) || (y == 0 && x == alignmentCount - 1))
                    {
                        continue;
                    }

                    MyPoint center = AlignmentPatternCoordinates[y, x];
                    for (int cy = center.Y - 2; cy <= center.Y + 2; ++cy)
                    {
                        for (int cx = center.X - 2; cx <= center.X + 2; ++cx)
                        {
                            exclusionMask[cy][cx] = true;
                        }
                    }
                }
            }
        }

        // Returns an info array about Error correction blocks.
        // The format is: {{1st block data #, 1st block ec #},{2nd block data #, 2nd block ec #},...}
        public int[][] GenerateECBlockInfo()
        {
            if (Version == (int)MicroQRVersion.M1)
            {
                return new int[][] { new int[] { 3, 2 } };
            }
            int ecBlockCount = QRUtils.ErrorCorrectionBlockCount[Version - 1][(int)ECLevel];
            if (ecBlockCount == 1)
            {
                return new int[][] { new int[] { DataCodewordCount, ECCodewordCount } };
            }

            int[][] ecInfo = new int[ecBlockCount][];
            int longBlockCount = DataCodewordCount % ecBlockCount;
            int shortBlockCount = ecBlockCount - longBlockCount;
            int ecsPerBlock = ECCodewordCount / ecBlockCount;
            int dataPerBlock = DataCodewordCount / ecBlockCount;
            for (int i = 0; i < shortBlockCount; ++i)
            {
                ecInfo[i] = new int[] { dataPerBlock, ecsPerBlock };
            }

            dataPerBlock++;
            for (int i = shortBlockCount; i < ecBlockCount; ++i)
            {
                ecInfo[i] = new int[] { dataPerBlock, ecsPerBlock };
            }

            return ecInfo;
        }
    }
}
