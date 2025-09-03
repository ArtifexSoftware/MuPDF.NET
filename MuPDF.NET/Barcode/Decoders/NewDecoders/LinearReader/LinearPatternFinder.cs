using System;
using System.Collections.Generic;
using System.Text;

namespace BarcodeReader.Core.Common
{
    /// <summary>
    /// Base class for pattern finders 
    /// </summary>
    abstract class LinearPatternFinder
    {
        protected float maxModuleSize;
        protected float minQuietZone;
        protected float maxPatternSymbolDifference;
        protected float maxPatternAverageSymbolDifference;
        protected int[] pattern;

        protected int[] moduleWidths;
        protected float sumPatternModules;

        public LinearPatternFinder(LinearReader reader, int[] pattern)
        {
            this.minQuietZone = reader.MinQuietZone;
            this.maxModuleSize = reader.maxModuleSize;
            this.maxPatternSymbolDifference = reader.MaxPatternSymbolDifference;
            this.maxPatternAverageSymbolDifference = reader.MaxPatternAverageSymbolDifference;
            this.pattern = pattern;

            //calc sum of bar modules
            sumPatternModules = Utils.Sum(pattern);

            if (minQuietZone <= 0)
                throw new Exception("QuietZone must be more 0");

            moduleWidths = new int[pattern.Length];
        }

        public LinearPatternFinder(int[] pattern)
        {
            this.minQuietZone = 5;
            this.maxModuleSize = 200;
            this.maxPatternSymbolDifference = 0.85f;//0.83f;
            this.maxPatternAverageSymbolDifference = 0.55f;//0.55f;
            this.pattern = pattern;

            //calc sum of bar modules
            sumPatternModules = Utils.Sum(pattern);

            if (minQuietZone <= 0)
                throw new Exception("QuietZone must be more 0");

            moduleWidths = new int[pattern.Length];
        }

        public abstract IEnumerable<Pattern> FindPattern(LineScanner line);
    }

    /// <summary>
    /// Finds start patterns with Quiet Zone
    /// </summary>
    class StartPatternFinder : LinearPatternFinder
    {
        public StartPatternFinder(LinearReader reader, int[] pattern) : base(reader, pattern)
        {
        }

        public StartPatternFinder(int[] pattern) : base(pattern)
        {
        }

        public override IEnumerable<Pattern> FindPattern(LineScanner line)
        {
            var barPos = line.BarPos;
            var barLength = line.BarLength;
            var barCount = line.BarsCount;
            int nBar = pattern.Length;
            int addWhiteBar = 0;
            if (pattern.Length % 2 == 0)
            {
                nBar--;
                addWhiteBar = 1;
            }

            //enumerate black bars
            for (int i = nBar; i < barCount - 5; i += 2)
            {
                //get quietZone white bar
                var iQuietZone = i - nBar;
                var iPatternStart = iQuietZone + 1;
                var lengthQuietZone = barLength[iQuietZone];
                var patternStartPos = barPos[iPatternStart];
                var patternEndPos = barPos[i + 1 + addWhiteBar];
                var lengthPattern = patternEndPos - patternStartPos;
                var moduleLength = 1f * lengthPattern / sumPatternModules;
                if (moduleLength > maxModuleSize)
                    continue;

                //quietZone must be more MinQuietZone modules
                if (lengthQuietZone >= minQuietZone * moduleLength)
                {
                    //var Next3BarsLength = barLength[i + 1] + barLength[i + 2] + barLength[i + 3];
                    //if (Next3BarsLength > 9 * moduleLength)
                    //    continue;

                    var sumBars = 0;
                    for (int j = 0; j < moduleWidths.Length; j++)
                        moduleWidths[j] = barLength[iPatternStart + j];

                    if (SymbologyReader.calcDifference(moduleWidths, pattern, maxPatternSymbolDifference, false) < maxPatternAverageSymbolDifference)
                    {
                        yield return new Pattern(new FoundPattern(0, 0, 0, moduleLength), patternStartPos, patternEndPos, line.Y);
                    }
                }
            }
        }
    }


    /// <summary>
    /// Finds start patterns with Quiet Zone
    /// </summary>
    class StartPatternFinderSimple : LinearPatternFinder
    {
        public StartPatternFinderSimple(LinearReader reader, int[] pattern) : base(reader, pattern)
        {
        }

        public StartPatternFinderSimple(int[] pattern) : base(pattern)
        {
        }

        public override IEnumerable<Pattern> FindPattern(LineScanner line)
        {
            var barPos = line.BarPos;
            var barLength = line.BarLength;
            var barCount = line.BarsCount;
            int nBar = pattern.Length;
            int addWhiteBar = 0;
            if (pattern.Length % 2 == 0)
            {
                nBar--;
                addWhiteBar = 1;
            }

            //enumerate black bars
            for (int i = nBar; i < barCount - 5; i += 2)
            {
                //get quietZone white bar
                var iQuietZone = i - nBar;
                var iPatternStart = iQuietZone + 1;
                var lengthQuietZone = barLength[iQuietZone];
                var patternStartPos = barPos[iPatternStart];
                var patternEndPos = barPos[i + 1 + addWhiteBar];
                var lengthPattern = patternEndPos - patternStartPos;
                var moduleLength = 1f * lengthPattern / sumPatternModules;
                if (moduleLength > maxModuleSize)
                    continue;

                //quietZone must be more MinQuietZone modules
                if (lengthQuietZone >= minQuietZone * moduleLength)
                {
                    yield return new Pattern(new FoundPattern(0, 0, 0, moduleLength), patternStartPos, patternEndPos, line.Y);
                }
            }
        }
    }

    /// <summary>
    /// Finds stop patterns with Quiet Zone
    /// </summary>
    class StopPatternFinder : LinearPatternFinder
    {
        public StopPatternFinder(LinearReader reader, int[] pattern) : base(reader, pattern)
        {
            if (pattern.Length % 2 == 0)
                throw new Exception("Pattern length must be Odd (1, 3, 5, ...)");
        }

        public override IEnumerable<Pattern> FindPattern(LineScanner line)
        {
            var barPos = line.BarPos;
            var barLength = line.BarLength;
            var barCount = line.BarsCount;
            int nBar = pattern.Length;

            //enumerate white bars
            for (int i = nBar + 7; i < barCount; i += 2)
            {
                //get quietZone white bar
                var iQuietZone = i;
                var lengthQuietZone = barLength[iQuietZone];
                if (lengthQuietZone < minQuietZone) continue;//too small white bar
                var iPatternStart = iQuietZone - nBar;
                var patternStartPos = barPos[iPatternStart];
                var patternEndPos = barPos[iQuietZone];
                var lengthPattern = patternEndPos - patternStartPos;
                var moduleLength = 1f * lengthPattern / sumPatternModules;
                if (moduleLength > maxModuleSize) continue;//too small module
                //quietZone must be more MinQuietZone modules
                if (lengthQuietZone >= minQuietZone * moduleLength)
                {
                    //var Prev3BarsLength = barLength[iPatternStart - 1] + barLength[iPatternStart - 2] + barLength[iPatternStart - 3];
                    //if (Prev3BarsLength > 9 * moduleLength)
                    //    continue;

                    for (int j = 0; j < moduleWidths.Length; j++)
                        moduleWidths[j] = barLength[iPatternStart + j];

                    var maxDiff = maxPatternSymbolDifference;
                    var maxAvgDiff = maxPatternAverageSymbolDifference;
                    if (moduleLength < 1.6f)
                    {
                        maxDiff *= 1.5f;
                        maxAvgDiff *= 1.3f;
                    }

                    if (SymbologyReader.calcDifference(moduleWidths, pattern, maxDiff, false) < maxAvgDiff)
                    {
                        yield return new Pattern(new FoundPattern(1, 0, 0, moduleLength), patternStartPos, patternEndPos, line.Y);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds empty stop pattern with Quiet Zone
    /// In fact it finds black line with right hand Quiet Zone
    /// </summary>
    class StopEmptyPatternFinder : LinearPatternFinder
    {
        public StopEmptyPatternFinder(LinearReader reader) : base(reader, new int[0])
        {
        }

        public override IEnumerable<Pattern> FindPattern(LineScanner line)
        {
            var barPos = line.BarPos;
            var barLength = line.BarLength;
            var barCount = line.BarsCount;
            const int nBar = 1;

            //enumerate white bars
            for (int i = nBar + 7; i < barCount; i += 2)
            {
                //get quietZone white bar
                var iQuietZone = i;
                var lengthQuietZone = barLength[iQuietZone];
                var lengthPattern = barLength[iQuietZone - 1];
                if (lengthQuietZone < 6) continue;//too small white bar
                if (lengthPattern > 4 * maxModuleSize) continue;//too big black bar
                var module = lengthPattern / 1.5f;
                if (module < 2) module = 2;
                //quietZone must be more MinQuietZone modules
                if (lengthQuietZone >= minQuietZone * module)
                {
                    //check size diapason of previous bars
                    var low = lengthPattern / 5;
                    var high = lengthPattern * 5;

                    var b = barLength[i - 2];
                    if (b < low || b > high) continue;

                    b = barLength[i - 3];
                    if (b < low || b > high) continue;

                    b = barLength[i - 4];
                    if (b < low || b > high) continue;

                    var pos = barPos[i];
                    yield return new Pattern(new FoundPattern(-1, 0, 0, 0), pos, pos, line.Y);
                }
            }
        }
    }

    /// <summary>
    /// Finds empty start pattern with Quiet Zone
    /// In fact it finds black line with left hand Quiet Zone
    /// </summary>
    class StartEmptyPatternFinder : LinearPatternFinder
    {
        public StartEmptyPatternFinder(LinearReader reader) : base(reader, new int[0])
        {
        }

        public override IEnumerable<Pattern> FindPattern(LineScanner line)
        {
            var barPos = line.BarPos;
            var barLength = line.BarLength;
            var barCount = line.BarsCount;

            //enumerate white bars
            for (int i = 0; i < barCount - 5; i += 2)
            {
                //get quietZone white bar
                var iQuietZone = i;
                var lengthQuietZone = barLength[iQuietZone];
                var lengthPattern = barLength[iQuietZone + 1];
                if (lengthQuietZone < 6) continue;//too small white bar
                if (lengthPattern > 4 * maxModuleSize) continue;//too big black bar
                var module = lengthPattern / 1.5f;
                if (module < 2) module = 2;
                //quietZone must be more MinQuietZone modules
                if (lengthQuietZone >= minQuietZone * module)
                {
                    //check size diapason of next bars
                    var low = lengthPattern / 5;
                    var high = lengthPattern * 5;

                    var b = barLength[i + 2];
                    if (b < low || b > high) continue;

                    b = barLength[i + 3];
                    if (b < low || b > high) continue;

                    b = barLength[i + 4];
                    if (b < low || b > high) continue;

                    var pos = barPos[i + 1];
                    yield return new Pattern(new FoundPattern(-1, 0, 0, 0), pos, pos, line.Y);
                }
            }
        }
    }
}
