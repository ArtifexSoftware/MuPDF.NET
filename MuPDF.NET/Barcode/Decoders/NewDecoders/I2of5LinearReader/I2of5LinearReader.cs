using System;
using System.Collections.Generic;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.I2of5LinearReader
{
    /// <summary>
    /// I2of5 reader.
    /// </summary>
    [Obsolete("This class does not pass all tests.")]
#if CORE_DEV
    public
#else
    internal
#endif
    class I2of5LinearReader : LinearReader
    {
        private bool HighPrecision = false;
        protected I2OF5SubMode SubMode = I2OF5SubMode.Interleaved2of5;

        #region Patterns

        private static int[][] template = new []
        {
            new int[]{1,1,2,2,1},
            new int[]{2,1,1,1,2},
            new int[]{1,2,1,1,2},
            new int[]{2,2,1,1,1},
            new int[]{1,1,2,1,2},
            new int[]{2,1,2,1,1},
            new int[]{1,2,2,1,1},
            new int[]{1,1,1,2,2},
            new int[]{2,1,1,2,1},
            new int[]{1,2,1,2,1},
        };

        private static int[][] _patterns = new int[100][];

        #endregion

        static I2of5LinearReader()
        {
            //generate pattern
            for (int i = 0; i < 10; i++)
            for (int j = 0; j < 10; j++)
            {
                var blacks = template[i];
                var whites = template[j];
                var pattern = _patterns[j * 10 + i] = new int[10];

                for (int k = 0; k < 10; k+=2)
                {
                    pattern[k] = blacks[k / 2];
                    pattern[k + 1] = whites[k / 2];
                }
            }
        }

        public I2of5LinearReader()
        {
            UseE = true;
            MinConfidence = 0.4f;//0.4
            MaxReadError = 5;
            MaxClusterDistanceY = 7;

            MaxPatternSymbolDifference = 0.9f;
            MaxPatternAverageSymbolDifference = 0.6f;
        }

        public override SymbologyType GetBarCodeType()
        {
            switch (SubMode)
            {
                case I2OF5SubMode.Interleaved2of5: return SymbologyType.I2of5;
                case I2OF5SubMode.ITF14: return SymbologyType.ITF14;
                case I2OF5SubMode.GTIN14: return SymbologyType.GTIN14;
                case I2OF5SubMode.Circular: return SymbologyType.CircularI2of5;
            }
            return SymbologyType.I2of5;
        }

        private BarSymbolReaderLooseProjection reader2;
        private BarSymbolReaderNaive ReaderNaive;

        internal override void GetParams(out int[] startPattern, out int[] stopPattern, out int minModulesPerBarcode, out int maxModulesPerBarcode)
        {
            startPattern = new int[] { 1, 1, 1 };
            stopPattern = new int[] { 2, 1, 1 };

            switch (SubMode)
            {
                default:
                    minModulesPerBarcode = 21;
                    maxModulesPerBarcode = int.MaxValue;
                    break;
                case I2OF5SubMode.ITF14:
                case I2OF5SubMode.GTIN14:
                    maxModulesPerBarcode = minModulesPerBarcode = 4 + 14 * 14 + 4;
                    break;
            }

            if (Reader == null)
            {
                Reader = new BarSymbolReader(Scan, 10, 14, false, true, 1, _patterns, -1, UseE, null);
                //reader2 = new BarSymbolReaderLooseProjection(Scan, 6, 11, true, _patterns, 106, true, UseE);
                ReaderNaive = new BarSymbolReaderNaive(10, 14, _patterns);
            }
        }

        internal override bool ReadSymbols(BarCodeRegion region, Pattern startPattern, Pattern stopPattern, MyPoint from, MyPoint to, float module, bool firstPass)
        {
            float error, maxError, confidence;

            var from0 = from;
            var to0 = to;

            //go 3 bars back from end of line
            to = GotoBar(to, from, 3);
            //go 4 bars from start of line
            from = GotoBar(from, to, 4);

            int[] row = ReadSymbols(startPattern, stopPattern, from, to, module, out error, out maxError, out confidence);

#if DEBUG
            var line = Scan.GetPixels(from, to);
            DebugHelper.AddDebugItem(region.A.ToString() + " err" + error, line, from, to);
#endif

            if (error < MaxReadError)
            {
                if (confidence >= MinConfidence)
                    if (confidence > region.Confidence)
                    {
                        if (Decode(region, row)) //try to decode
                        {
                            region.Confidence = confidence;
                            return true;
                        }
                    }
            }

            //try naive reader
            row = ReaderNaive.Read(Scan.GetPixels(from, to), out error, out maxError, out confidence);
            //var row = ReaderNaive.Read(Scan.GetPixels(from, to, 1.4f), out error, out maxError, out confidence);
            //row = ReaderNaive.Read(Scan.GetPixels3Lines(from, to, 1.4f, 3.5f), out error, out maxError, out confidence);

            if (confidence >= MinConfidence && maxError < 0.5f)
            if (confidence > region.Confidence)
                if (Decode(region, row)) //try to decode
                {
                    region.Confidence = confidence;
                    return true;
                }

            //try BarSymbolReaderLooseProjection
            if (HighPrecision)
            if (confidence > 0.6f)
            {
                float[] widths = new float[] { 0.5f, 0.9f };
                foreach (float w in widths)
                {
                    row = reader2.Read(region, module, w);
                    if (row != null && row.Length > 0)
                    {
                        if (Decode(region, row))
                        {
                            region.Confidence = confidence;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private MyPoint GotoBar(MyPoint from, MyPoint to, int barsCount)
        {
            var br = new Bresenham(from, to);
            //go to first black bar
            while (!br.End())
            {
                if (Scan.isBlack(br.Current))
                    break;
                br.Next();
            }

            //skip barsCount
            var isBlack = true;
            while (!br.End() && barsCount > 0)
            {
                if (Scan.isBlack(br.Current) ^ isBlack)
                {
                    barsCount--;
                    isBlack = !isBlack;
                    if (barsCount == 0)
                        break;
                }
                br.Next();
            }

            return br.Current;
        }

        #region Decoders

        //Method to decode bytecodes into the final string.
        internal override bool Decode(BarCodeRegion r, int[] row)
        {
            var data = new byte[row.Length * 2];
            for (int i = 0; i < row.Length; i++)
            {
                data[i * 2] = (byte)(row[i] % 10);
                data[i * 2 + 1] = (byte)(row[i] / 10);
            }

            switch (SubMode)
            {
                case I2OF5SubMode.ITF14:
                case I2OF5SubMode.GTIN14:
                    if (data.Length != 14)
                        return false;
                    break;
            }

            var conf = VerifyCheckSumAndGetConfidence(data);

            if (conf < 0.5f)
                return false;

            r.Data = new ABarCodeData[] { new NumericBarCodeData(data) };

            return true;
        }

        private float VerifyCheckSumAndGetConfidence(byte[] data)
        {
            int datalength = data.Length; // length of data without the very last one 
            // output result flag
            bool checksumMatched = false;
            // 2 cases:
            // 1) EVEN number of digits with checksum:
            // - should contain ODD number of digits + single checksum digit
            // if checksum matches then return confidence 1.00
            // 2) EVEN number of digits without checksum:
            // return 0.5 as confidence
            int iRetries = 0;
            for (iRetries = 0; iRetries < 2; iRetries++)
            {
                int sum = 0;
                for (int i = 0; i < datalength - 1; i++)
                    sum += (int)(data[i]) * (i % 2 == 0 ? 3 : 1);
                sum = sum % 10;
                if (sum != 0)
                    sum = 10 - sum;

                // getting the result
                checksumMatched = sum == (int)data[datalength - 1];

                if (checksumMatched)
                    break;

                // when trying for the first time then we may try to remove the very last symbol and try again
                // if checksum is not matching then we should check if we have odd number of symbols
                // like 13 symbols or 15
                // but in I2of5 we should have even number 
                // for example we may have: 16444118888780 (14 digits) and this is not correct with checksum (checksum is not zero)
                // but 1644411888878 is correct (checksum = 8 and with 13 digits)

                if (iRetries == 0)
                {
                    // try to decrease the data length the very last digit and try again
                    if (data.Length > 2 && (int)data[data.Length - 1] == 0)
                        datalength--;
                    else
                        break;
                }
                else
                    break;
            }

            if (checksumMatched)
            {
                return 1.00f;
            }
            else
            {
                if (data.Length % 2 > 0) // if ODD number of digits
                    return 0.0f;
                else
                    return 0.7f;

            }
        }

        #endregion
    }


#if CORE_DEV
    public
#else
    internal
#endif
    enum I2OF5SubMode
    {
        /// <summary>
        /// interleaved 2 of 5
        /// </summary>
        Interleaved2of5,
        /// <summary>
        /// itf-14 (
        /// </summary>
        ITF14,
        GTIN14,
        Circular
    };
}
