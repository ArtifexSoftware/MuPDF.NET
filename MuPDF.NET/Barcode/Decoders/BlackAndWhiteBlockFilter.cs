using System;
using System.Diagnostics;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core
{
    internal class  BlackAndWhiteBlockFilterOld: BlackAndWhiteBlockFilter
    {
		public BlackAndWhiteBlockFilterOld(IPreparedImage image, int thresholdLevelAdjustment)
            : base(image, thresholdLevelAdjustment)
        {
            // setting the super block size to 1 so it will not be used actually
            SUPERBLOCK_SIZE = 1;
        }
    }

#if !OLD_GRAYSCALEIMAGE
    /// <summary>
    /// Converts given grayscale image into a black and white (1 bit) image.
    /// This filter operates on blocks of 8x8 pixels and calculates
    /// threshold on 5x5 groups of blocks. This technique allows proper
    /// binarization of images with severe luminance differences.
    /// </summary>
    internal class BlackAndWhiteBlockFilter : IBlackAndWhiteFilter, IParallelSupporting
    {

        // size of the block where average luminance is calculated
        private const int BLOCK_SIZE = 8;
        // superblock size to normalize block luminance 
        // set to 1 or 0 to disable
        protected int SUPERBLOCK_SIZE = 6;
        //difference between the darkest and brighter pixel in a block to consider 
        //it as an homogeneous block (all black or all white).
        protected int ThresholdLevelAdjustment;

        protected IPreparedImage _image;
        protected XBitArray[] _rows;
        private XBitArray[] _columns;

        public BlackAndWhiteBlockFilter(IPreparedImage image, int thresholdLevelAdjustment)
        {
            _image = image;
	        ThresholdLevelAdjustment = thresholdLevelAdjustment;
	        
			_rows = new XBitArray[_image.Height];
            _columns = new XBitArray[_image.Width];

            for (int i = 0; i < _rows.Length; i++)
				_rows[i] = new XBitArray(_image.Width);

			binarizeEntireImage();
        }

		public XBitArray GetRow(int y)
	    {
		    return _rows[y];
	    }

        public void ResetColumns()
        {
            lock (_columns)
            {
                for (int x = 0; x < _image.Width; x++)
                    _columns[x] = null;
            }
        }

        public XBitArray GetColumn(int x)
	    {
			lock (_columns)
			{
			    if (_columns[x] == null)
			    {
			        XBitArray column = new XBitArray(_image.Height);

			        //now fill the column
			        for (int j = 0; j < column.Size; j++)
			        {
			            column[j] = GetRow(j)[x];
			        }

			        _columns[x] = column;
			    }

			    return _columns[x];
            }
	    }

        protected virtual void binarizeEntireImage()
        {
            if (IsSourceImageAlreadyBinarized())
            {
                return;
            }

            int width = _image.Width;
            int height = _image.Height;
            int blockCountX = width / BLOCK_SIZE;
            if ((width % BLOCK_SIZE) != 0)
                blockCountX++;

            int blockCountY = height / BLOCK_SIZE;
            if ((height % BLOCK_SIZE) != 0)
                blockCountY++;

            int[][] blackPoints = calculateBlackPoints(blockCountX, blockCountY);

            calculateThresholdForBlock(blockCountX, blockCountY, blackPoints);
        }

        private bool IsSourceImageAlreadyBinarized()
        {
            var isBinarized = true;

            //try to read pixels and fill rows
            Parallel.For(0, _image.Height, -1, 100, (pair) =>
            {
                for (var y = pair.From; y < pair.To; y++)
                {
                    var row = _image.GetRow(y);
                    var outRow = _rows[y];

                    for (var x = 0; x < row.Length; x++)
                    {
                        var pixel = row[x];
                        if (pixel > 0 && pixel < 255)
                        {
                            isBinarized = false;
                            break;
                        }
                        else
                        {
                            outRow[x] = pixel == 0;
                        }
                    }
                }
            });

            return isBinarized;
        }

        private int[][] calculateBlackPoints(int blockCountX, int blockCountY)
        {
            int width = _image.Width;
            int height = _image.Height;


            int[][] blackPoints = new int[blockCountY][];
            for (int i = 0; i < blackPoints.Length; i++)
                blackPoints[i] = new int[blockCountX];

            Parallel.For(0, blockCountY, (blockY) =>
            {
                int startRow = blockY * BLOCK_SIZE;
                if ((startRow + BLOCK_SIZE) >= height)
                    startRow = height - BLOCK_SIZE;

                int startY = startRow < 0 ? -startRow : 0;

                int min = 255;
                int max = 0;

                for (int blockX = 0; blockX < blockCountX; blockX++)
                {
                    int startColumn = blockX * BLOCK_SIZE;
                    if ((startColumn + BLOCK_SIZE) >= width)
                        startColumn = width - BLOCK_SIZE;

                    int startX = startColumn < 0 ? -startColumn : 0;

                    int sum = 0;
                    min = 255;
                    max = 0;
                    for (int y = startY; y < BLOCK_SIZE; y++)
                    {
                        byte[] row = _image.GetRow(startRow + y);
                        for (int x = startX; x < BLOCK_SIZE; x++)
                        {
                            int pixel = row[startColumn + x] & 0xff;
                            sum += pixel;
                            if (pixel < min)
                                min = pixel;

                            if (pixel > max)
                                max = pixel;
                        }
                    }

                    int average;

                    var count = (BLOCK_SIZE - startX) * (BLOCK_SIZE - startY);

                    if (max - min > ThresholdLevelAdjustment)
                    {
                        average = sum / count; // calculating average from blocks 
                    }
                    else
                    {
                        // When min == max == 0, let average be 1 so all is black
                        //average = max == 0 ? 1 : min / 2;
                        if (max < 50) average=max*2+1;
                        else if (min > 200) average = min/2-1;
                        else average = min / 2;
                        //average = 100;
                    }                    

                    blackPoints[blockY][blockX] = average;
                }
            }
            );

            return blackPoints;
        }

        // For each 8x8 block in the image, calculate the average black point using a 5x5 grid
        // of the blocks around it. Also handles the corner cases.
        private void calculateThresholdForBlock(int blockCountX, int blockCountY, int[][] blackPoints)
        {
            int width = _image.Width;
            int height = _image.Height;

            double[,] blocksAverages = new double[blockCountY, blockCountX];
            double[,] superBlocksAverages = null;
            //int[,] blocksAverages = new int[blockCountY, blockCountX];
            //int[,] superBlocksAverages = null;

            
            // if superblock size defined > 1
            if (SUPERBLOCK_SIZE>1)
                superBlocksAverages = new double[blockCountY / SUPERBLOCK_SIZE +1, blockCountX / SUPERBLOCK_SIZE + 1];
            //superBlocksAverages = new int[blockCountY / SUPERBLOCK_SIZE + 1, blockCountX / SUPERBLOCK_SIZE + 1];

            // calculating averages in each block
            Parallel.For(0, blockCountY, (y) =>
            {
                int startRow = y * BLOCK_SIZE;
                if ((startRow + BLOCK_SIZE) >= height)
                    startRow = height - BLOCK_SIZE;

                for (int x = 0; x < blockCountX; x++)
                {
                    int startColumn = x * BLOCK_SIZE;
                    if ((startColumn + BLOCK_SIZE) >= width)
                        startColumn = width - BLOCK_SIZE;

                    int left = (x > 1) ? x : 2;
                    left = (left < blockCountX - 2) ? left : blockCountX - 3;

                    int top = (y > 1) ? y : 2;
                    top = (top < blockCountY - 2) ? top : blockCountY - 3;

                    int sum = 0;
                    int sumCount = 0;
                    for (int z = -2; z <= 2; z++)
                    {
                        if ((top + z) < 1 || left-2 <0)
                            continue;

                        int[] blackRow = blackPoints[top + z];
                        sum += blackRow[left - 2];
                        sum += blackRow[left - 1];
                        sum += blackRow[left];
                        sum += blackRow[left + 1];
                        sum += blackRow[left + 2];
                        // count how much members we have for the sum
                        sumCount += 5;
                    }

                    double divider = 25d; // emperic value! with default settings we should divide by 20 not 25 but it is not working for some noisty images so we use 25

                    // if using BlockOld type then finding the exact average value
                    if (SUPERBLOCK_SIZE == 1)
                        divider = sumCount * 1d;

                    double average = sum / divider; 

                    if(sumCount == 0)
                        blocksAverages[y, x] = blackPoints[y][x];
                    else
                        //double average = sum / sumCount * 1d; // finding the average value from all components in the sum 
                        blocksAverages[y, x] = average;
                }
            }
            );

            int actualSuperBlockCountY = blockCountY / SUPERBLOCK_SIZE;
            int actualSuperBlockCountX = blockCountX / SUPERBLOCK_SIZE;

            bool useSuperBlocks = SUPERBLOCK_SIZE > 1 && actualSuperBlockCountY > 2 && actualSuperBlockCountX > 2;

            if (useSuperBlocks)
            {
                // calculating averages in superblocks (1 superblock per 4 blocks)
                for (int y = 0; y < blockCountY; y++)
                {               
                    for (int x = 0; x < blockCountX; x++)
                    { 
                        double aver = blocksAverages[y,x];
                        //int aver = blocksAverages[y, x];
                        int superX = x / SUPERBLOCK_SIZE;
                        int superY = y / SUPERBLOCK_SIZE;
                        superBlocksAverages[superY, superX] += aver;
                    }
                }

                // set averages in superblocks to average value (average per 4 blocks)
                for (int y = 0; y < blockCountY / SUPERBLOCK_SIZE + 1; y++)
                {
                    for (int x = 0; x < blockCountX / SUPERBLOCK_SIZE + 1; x++)
                    {
                        superBlocksAverages[y, x] = superBlocksAverages[y, x] / SUPERBLOCK_SIZE / SUPERBLOCK_SIZE;
                    }
                }

            }

            // now binarizing image
            Parallel.For(0, blockCountY, (y) =>
            {
                int startRow = y * BLOCK_SIZE;
                if ((startRow + BLOCK_SIZE) >= height)
                    startRow = height - BLOCK_SIZE;

                for (int x = 0; x < blockCountX; x++)
                {
                    int startColumn = x * BLOCK_SIZE;
                    if ((startColumn + BLOCK_SIZE) >= width)
                        startColumn = width - BLOCK_SIZE;

                    double average = blocksAverages[y, x];
                    //int average = blocksAverages[y, x];

                    // if superblock size > 1 
                    if (useSuperBlocks)
                    {
                        // read superblock average
                        int superX = x / SUPERBLOCK_SIZE;
                        int superY = y / SUPERBLOCK_SIZE;
                        double delta = superBlocksAverages[superY, superX];

                        // calculating average between block  and superblock
                        // and rounding to greatest value
                        average = (int)Math.Round((average + delta) / 2d, 0, MidpointRounding.AwayFromZero);
                    }

                     binarizeBlock(startRow, startColumn, average);
                }
            }
            );
        }

        private void binarizeBlock(int startRow, int startColumn, double threshold)
        //private void binarizeBlock(int startRow, int startColumn, int threshold)
        {
            int startY = startRow < 0 ? -startRow : 0;
            int startX = startColumn < 0 ? -startColumn : 0;

            for (int y = startY; y < BLOCK_SIZE; y++)
            {
                byte[] row = _image.GetRow(startRow + y);
				XBitArray bits = _rows[startRow + y];
                for (int x = startX; x < BLOCK_SIZE; x++)
                {
                    int pixel = row[startColumn + x] & 0xff;
                    bits[startColumn + x] = (threshold - pixel * 1d) > double.Epsilon;
                    //bits[startColumn + x] = (pixel < threshold);                    
                }
            }
        }

        public bool IsParallelSupported
        {
            get { return true; }
        }
    }
#endif
}
