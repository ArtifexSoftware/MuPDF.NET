using System;

namespace BarcodeReader.Core.Common
{
    /// <summary>
    /// Scans image horizontally, finds bar's positions and lengthes
    /// Even (0, 2, 4, ...) - white bars
    /// Odd (1, 3, 5, ...) - black bars
    /// </summary>
#if CORE_DEV
    public
#else
    internal
#endif
    class LineScanner
    {
        // Even indexes (0, 2, 4, ...) - white bars
        // Odd indexes (1, 3, 5, ...) - black bars
        public int[] BarLength;
        public int[] BarPos;
        public int BarsCount = 0;
        public int Y;
        public bool Reverse;

        private int fromX;
        private int toX;

        /// <param name="fromX">Inclusive start X</param>
        /// <param name="toX">Exclusive end X</param>
        public LineScanner(int fromX, int toX)
        {
            BarPos = new int[toX + 4];
            BarLength = new int[toX + 4];
            this.fromX = fromX;
            this.toX = toX;
        }

        /// <summary>
        /// Finds bar's lengthes and positions.
        /// </summary>
        /// <param name="y"></param>
        public void FindBars(XBitArray row, int y)
        {
            this.Y = y;           
            int iBar = -1;

            var prevBarX = -10000;
            bool isBlack = false;

            for (int x = fromX; x < toX; x++)
            {
                if (row[x] ^ isBlack)//change color
                {
                    iBar++;
                    BarLength[iBar] = x - prevBarX;
                    BarPos[iBar] = prevBarX;
                    prevBarX = x;
                    isBlack = !isBlack;
                }
            }

            //close last black bar
            if (isBlack)
            {
                iBar++;
                BarLength[iBar] = toX - prevBarX;
                BarPos[iBar] = prevBarX;
                prevBarX = toX;
            }

            //add long right white bar
            iBar++;
            BarLength[iBar] = 20000;
            BarPos[iBar] = prevBarX;

            BarsCount = iBar + 1;
        }

        /// <summary>
        /// !!!!!
        /// Finds bars, allowing noise removing. Noise level definies the number of pixels considered as noise.
        /// noiseLevel=1: accept modules > 1 pixel wide.
        /// noiseLevel=2: accept modules > 2 pixel wide.
        /// </summary>
        public void FindBars(int y, int noiseLevel)
        {
            throw new NotImplementedException();
        }
    }
}