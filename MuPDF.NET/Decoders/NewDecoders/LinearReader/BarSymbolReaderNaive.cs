using System;
using System.Collections.Generic;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Common
{
    internal class BarSymbolReaderNaive
    {
        struct SymbolInfo
        {
            public int[] Pattern;
            public float AvgWhite;
            public float StdDevWhite;
            public float AvgBlack;
            public float StdDevBlack;
        }

        private readonly int[] _nBars;
        private readonly int[] _nLength;
        private readonly int[] _tableIndexes;
        private readonly SymbolInfo[][] _patterns;
        private readonly int maxBarSize = 0;//max width of bar in modules

        private readonly int _fixedBars;
        private readonly int _fixedLength;
        private readonly bool noStartStop = false;

        public BarSymbolReaderNaive(int[] nBars, int[] nLength, int[] tableIndexes, int[][][] patterns)
        {
            _nBars = nBars;
            _nLength = nLength;
            _tableIndexes = tableIndexes;
            PreparePatterns(patterns, out _patterns, out maxBarSize);
        }

        public BarSymbolReaderNaive(int nBars, int nLength, int[][] patterns)
        {
            _fixedBars = nBars;
            _fixedLength = nLength;
            var p = new int[1][][];
            p[0] = patterns;
            PreparePatterns(p, out _patterns, out maxBarSize);
            noStartStop = true;
        }

        private void PreparePatterns(int[][][] patterns, out SymbolInfo[][] prepared, out int maxBarSize)
        {
            prepared = new SymbolInfo[patterns.Length][];
            maxBarSize = 0;

            for (int iTable = 0; iTable < patterns.Length; iTable++)
            {
                var table = patterns[iTable];
                var preapredTable = prepared[iTable] = new SymbolInfo[table.Length];

                for (var iSymbol = 0; iSymbol < table.Length; iSymbol++)
                {
                    //calc avg of white and black symbols
                    var symbol = table[iSymbol];
                    var sumBlack = 0f;
                    var sumWhite = 0f;
                    var sumBlackSq = 0f;
                    var sumWhiteSq = 0f;
                    var blackCount = 0;
                    var whiteCount = 0;
                    var preparedSym = new SymbolInfo();
                    preparedSym.Pattern = symbol;
                    var symLen = symbol.Length % 2 == 0 ? symbol.Length - 1 : symbol.Length;

                    for (int i = 0; i < symLen; i++)
                    {
                        if (i % 2 == 0)
                        {
                            sumBlack += symbol[i];
                            sumBlackSq += symbol[i] * symbol[i];
                            blackCount++;
                        }
                        else
                        {
                            sumWhite += symbol[i];
                            sumWhiteSq += symbol[i] * symbol[i];
                            whiteCount++;
                        }

                        if (symbol[i] > maxBarSize)
                            maxBarSize = symbol[i];
                    }

                    preparedSym.AvgBlack = sumBlack / blackCount;
                    preparedSym.StdDevBlack = 0.001f + (float)Math.Sqrt(sumBlackSq / blackCount - (sumBlack / blackCount) * (sumBlack / blackCount));

                    preparedSym.AvgWhite = sumWhite / whiteCount;
                    preparedSym.StdDevWhite = 0.001f + (float)Math.Sqrt(sumWhiteSq / whiteCount - (sumWhite / whiteCount) * (sumWhite / whiteCount));

                    preapredTable[iSymbol] = preparedSym;
                }
            }
        }

        public int[] Read(float[] pixels, out float error, out float maxError, out float confidence)
        {
            if (noStartStop)
                return ReadNoStartStop(pixels, out error, out maxError, out confidence);

            confidence = 1;//temp
            error = 0;
            maxError = 0;

            var res = new int[_nBars.Length];
            int iPixel = 0;
            GotoBlack(pixels, ref iPixel);
            var isBlack = true;
            var first = true;
            int iSymbol;
            for (iSymbol = 0; iSymbol < _nBars.Length; iSymbol++)
            {
                int iStart = iPixel;
                //read n bars
                var n = _nBars[iSymbol];
                for (int i = 0; i < n; i++)
                {
                    if (iPixel >= pixels.Length)
                        break; //no more bars
                    GotoNextBar(pixels, ref iPixel);
                    isBlack = !isBlack;
                }
                //
                var iStop = iPixel;
                //find best match
                var iTable = _tableIndexes[iSymbol];
                //FindBestMatch(pixels, iStart, iStop, _patterns[iTable], isBlack ^ first ? 1 : -1);
                first = false;
            }

            //fill -1
            for (; iSymbol < res.Length; iSymbol++)
                res[iSymbol] = -1;

            return res;
        }

        private int[] ReadNoStartStop(float[] pixels, out float error, out float maxError, out float confidence)
        {
            var nSymbols = 0;
            //prepare and check bars
            CheckAndPrepare(pixels, out confidence, out nSymbols);
            if (confidence < 0.01f)
            {
                error = 10;
                maxError = 10;
                return new int[0];
            }
#if DEBUG
            DebugHelper.AddDebugItem("sym" + nSymbols, pixels);
#endif
            var res = new int[nSymbols];
            var offset = _fixedBars % 2 == 0 ? _fixedBars - 1 : _fixedBars;
            var patterns = _patterns[0];
            var sumCorr = 0f;
            var worstCorr = 1f;

            //get start and stop position for each symbol
            for (int iSymbol = 0; iSymbol < nSymbols; iSymbol++)
            {
                var i = iSymbol * _fixedBars;

                //find best match
                int sym;
                float corr;
                FindBestMatch(i, i + offset, patterns, out sym , out corr);
                res[iSymbol] = sym;
                sumCorr += corr;
                if (corr < worstCorr)
                    worstCorr = corr;
            }

            //confidence = sumCorr / nSymbols;
            confidence = sumCorr / nSymbols;
            error = 1 - confidence;
            maxError = 1 - worstCorr;

            return res;
        }

        private List<int> barWidth = new List<int>(200);
        private int barCount;

        private void CheckAndPrepare(float[] pixels, out float confidence, out int nSymbols)
        {
            barWidth.Clear();

            confidence = 0;
            nSymbols = 0;

            //go first black
            var iPixel = 0;
            GotoBlack(pixels, ref iPixel);
            if (iPixel >= pixels.Length) return;

            var minBlack = int.MaxValue;
            var maxBlack = 0;
            var minWhite = int.MaxValue;
            var maxWhite = 0;

            //calc bar positions, bars count , min/max bar length
            var prevIndex = iPixel;
            bool isBlack = true;
            for (; iPixel < pixels.Length; iPixel++)
            {
                var v = pixels[iPixel];
                var changed = false;

                if (v < -0.3f)
                    changed = !isBlack;
                else if (v < 0.3f)
                    changed = true;
                else
                    changed = isBlack;

                if (changed)
                {
                    var len = iPixel - prevIndex;
                    if (isBlack)
                    {
                        if (len > maxBlack) maxBlack = len;
                        if (len < minBlack) minBlack = len;
                    }
                    else
                    {
                        if (len > maxWhite) maxWhite = len;
                        if (len < minWhite) minWhite = len;
                    }
                    barWidth.Add(len);
                    prevIndex = iPixel;
                    isBlack = !isBlack;
                }
            }

            //add last
            {
                var len = iPixel - prevIndex;
                if (isBlack)
                {
                    if (len > maxBlack) maxBlack = len;
                    if (len < minBlack) minBlack = len;
                }
                else
                {
                    if (len > maxWhite) maxWhite = len;
                    if (len < minWhite) minWhite = len;
                }
                barWidth.Add(len); //add ficitve white bar
            }

            //check total bar count
            var barCount = barWidth.Count;
            if (barCount % 2 == 1) //add last fictive white bar
                barCount++;

            nSymbols = barCount / _fixedBars;//symbols count


            if (nSymbols * _fixedBars != barCount) //ups... is not barcode of my type
            {
#if DEBUG
                DebugHelper.AddDebugItem(""+ barCount, pixels);
#endif
                return;
            }

            //check difference between max and min length of bar
            if (minBlack < 2) minBlack = 2;
            if (maxBlack / (float)minBlack > maxBarSize * 3f)
                return;//too wide or short bars

            if (minWhite < 2) minWhite = 2;
            if (maxWhite / (float)minWhite > maxBarSize * 3f)
                return;//too wide or short bars

            //all right
            confidence = 1;
        }

        private void FindBestMatch(int iStart, int iStop, SymbolInfo[] table, out int bestSymbolIndex, out float bestCorr)
        {
            //calc avg and stdDev for pixels
            var sumBlackX = 0f;
            var nBlack = 0;
            var sumBlackXsq = 0f;
            var sumWhiteX = 0f;
            var nWhite = 0;
            var sumWhiteXsq = 0f;
            var isBlack = true;
            for (int i = iStart; i < iStop; i++)
            {
                var v = barWidth[i];

                if (isBlack)
                {
                    sumBlackX += v;
                    sumBlackXsq += v * v;
                    nBlack++;
                }
                else
                {
                    sumWhiteX += v;
                    sumWhiteXsq += v * v;
                    nWhite++;
                }
                isBlack = !isBlack;
            }

            var avgBlack = sumBlackX / nBlack;
            var stdDevBlack = 0.001f + (float)Math.Sqrt(sumBlackXsq / nBlack - avgBlack * avgBlack);
            var avgWhite = sumWhiteX / nWhite;
            var stdDevWhite = 0.001f + (float)Math.Sqrt(sumWhiteXsq / nWhite - avgWhite * avgWhite);

            ////
            bestSymbolIndex = -1;
            bestCorr = -2;

            for (int iSymbol = 0; iSymbol < table.Length; iSymbol++)
            {
                var symbol = table[iSymbol];
                var pattern = symbol.Pattern;
                var count = nBlack + nWhite;
                //calc black correlation with symbol
                var sumXY = 0f;
                for (int i = 0; i < count; i += 2)
                    sumXY += barWidth[iStart + i] * pattern[i];

                var corrBlack = (sumXY / nBlack - avgBlack * symbol.AvgBlack + 0.001f * 0.001f) / (stdDevBlack * symbol.StdDevBlack);

                //calc white corr
                sumXY = 0f;
                for (int i = 1; i < count; i += 2)
                    sumXY += barWidth[iStart + i] * pattern[i];

                var corrWhite = (sumXY / nWhite - avgWhite * symbol.AvgWhite + 0.001f * 0.001f) / (stdDevWhite * symbol.StdDevWhite);

                //total corr
                var corr = Math.Min(corrBlack , corrWhite) - iSymbol / 1000f;

                if (corr > bestCorr)
                {
                    bestCorr = corr;
                    bestSymbolIndex = iSymbol;
                }
            }
        }


        private void GotoBlack(float[] pixels, ref int iPixel)
        {
            for(;iPixel < pixels.Length;iPixel++)
                if (pixels[iPixel] < 0)
                    break;
        }

        void GotoNextBar(float[] pixels, ref int iPixel)
        {
            var isBlack = pixels[iPixel] < 0;
            for (; iPixel < pixels.Length; iPixel++)
            {
                if ((pixels[iPixel] < 0) ^ isBlack)
                    return;
            }
        }
    }
}
