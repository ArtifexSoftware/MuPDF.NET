using System;
using System.Collections.Generic;

namespace BarcodeReader.Core.Common
{
    // Faster version of PatternFinderNoiseRow
    [Obsolete("This class does not pass all tests.")]
    class PatternFinderNoiseRowEx : IPatternFinderNoiseRow
    {
        private bool _hasQuietZone;
        private int _first;
        private int _last;
        private int _center;
        private int[] pattern;
        private StartPatternFinder finder;
        private bool UseE;
        private IEnumerator<Pattern> enumerator;

        /// <summary>
        ///004
        ///010
        ///017
        ///021
        ///022
        ///023
        /// </summary>
        /// <param name="pattern"></param>

        public PatternFinderNoiseRowEx(int[] pattern)
        {
            this.pattern = pattern;
            finder = new StartPatternFinder(pattern);
        }

        public void NewSearch(XBitArray row)
        {
            NewSearch(row, 0, row.Size, 1, 1);
        }

        public void NewSearch(XBitArray row, int startX, int endX, int inc, int minModuleLength)
        {
            var line = new LineScanner(startX, endX);
            line.FindBars(row, 0);
            enumerator = finder.FindPattern(line).GetEnumerator();
        }

        public FoundPattern NextPattern()
        {
            if (enumerator.MoveNext())
                return enumerator.Current.foundPattern;

            return null;
        }

        public bool HasQuietZone
        {
            get { return true; }
        }

        public int First
        {
            get { return enumerator.Current.xIn; }
        }

        public int Last
        {
            get { return enumerator.Current.xEnd; }
        }

        public int Center
        {
            get
            {
                var p = enumerator.Current;
                return (p.xIn + p.xEnd) / 2;
            }
        }
    }
}
