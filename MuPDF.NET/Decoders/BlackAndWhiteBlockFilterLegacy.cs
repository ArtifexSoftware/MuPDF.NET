using System;
using System.Text;

namespace BarcodeReader.Core
{
    /// <summary>
    /// Converts given grayscale image into a black and white (1 bit) image.
    /// This filter operates on blocks of 8x8 pixels and calculates
    /// threshold on 5x5 groups of blocks. This technique allows proper
    /// binarization of images with severe luminance differences.
    /// </summary>
    internal class BlackAndWhiteBlockFilterLegacy : IBlackAndWhiteFilter
    {
        // size of the block where average luminance is calculated
        private const int BLOCK_SIZE = 8;

        // superblock size to normalize block luminance 
        // set to 1 or 0 to disable
        private const int SUPERBLOCK_SIZE = 6;

        private IPreparedImage _image;
        private XBitArray[] _rows;
        private XBitArray[] _columns;

        public BlackAndWhiteBlockFilterLegacy(IPreparedImage image, int thresholdLevelAdjustment)
        {
            _image = image;
        }

        public XBitArray GetRow(int y)
        {
            if (_rows == null)
            {
                _rows = new XBitArray[_image.Height];
                for (int i = 0; i < _rows.Length; i++)
                    _rows[i] = new XBitArray(_image.Width);

                binarizeEntireImage();
            }

            return _rows[y];
        }

        public XBitArray GetColumn(int x)
        {
            if (_columns == null)
            {
                _columns = new XBitArray[_image.Width];
            }

            if (_columns[x] == null)
                _columns[x] = new XBitArray(_image.Height);
            else
                return _columns[x];

            XBitArray column = _columns[x];

            // now fill the column
            for (int j = 0; j < column.Size; j++)
            {
                column[j] = GetRow(j)[x];
            }

            return column;
        }

        public void ResetColumns()
        {
            lock (_columns)
            {
                for (int x = 0; x < _image.Width; x++)
                    _columns[x] = null;
            }
        }

        private void binarizeEntireImage()
        {
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

        private int[][] calculateBlackPoints(int blockCountX, int blockCountY)
        {
            int width = _image.Width;
            int height = _image.Height;


            int[][] blackPoints = new int[blockCountY][];
            for (int i = 0; i < blackPoints.Length; i++)
                blackPoints[i] = new int[blockCountX];

            for (int blockY = 0; blockY < blockCountY; blockY++)
            {
                int startRow = blockY * BLOCK_SIZE;
                if ((startRow + BLOCK_SIZE) >= height)
                    startRow = height - BLOCK_SIZE;

                int min = 255;
                int max = 0;

                for (int blockX = 0; blockX < blockCountX; blockX++)
                {
                    int startColumn = blockX * BLOCK_SIZE;
                    if ((startColumn + BLOCK_SIZE) >= width)
                        startColumn = width - BLOCK_SIZE;

                    int sum = 0;
                    min = 255;
                    max = 0;
                    for (int y = 0; y < BLOCK_SIZE; y++)
                    {
                        byte[] row = _image.GetRow(startRow + y);
                        for (int x = 0; x < BLOCK_SIZE; x++)
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

                    if (max - min > 24)
                    {
                        average = sum >> 6;
                    }
                    else
                    {
                        // When min == max == 0, let average be 1 so all is black
                        //average = max == 0 ? 1 : min >> 1;
                        if (max < 50) average = max * 2 + 1;
                        else if (min > 200) average = min / 2 - 1;
                        else average = min >> 1;
                        //average = 100;
                    }

                    blackPoints[blockY][blockX] = average;
                }
            }

            return blackPoints;
        }

        // For each 8x8 block in the image, calculate the average black point using a 5x5 grid
        // of the blocks around it. Also handles the corner cases.
        private void calculateThresholdForBlock(int blockCountX, int blockCountY, int[][] blackPoints)
        {
            int width = _image.Width;
            int height = _image.Height;

            int[,] blocksAverages = new int[blockCountY, blockCountX];
            int[,] superBlocksAverages = null;

            // if superblock size defined > 1
            if (SUPERBLOCK_SIZE > 1)
                superBlocksAverages = new int[blockCountY / SUPERBLOCK_SIZE + 1, blockCountX / SUPERBLOCK_SIZE + 1];

            // calculating averages in each block
            for (int y = 0; y < blockCountY; y++)
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
                    for (int z = -2; z <= 2; z++)
                    {
                        if ((top + z) < 1 || left - 2 < 0)
                            continue;

                        int[] blackRow = blackPoints[top + z];
                        sum += blackRow[left - 2];
                        sum += blackRow[left - 1];
                        sum += blackRow[left];
                        sum += blackRow[left + 1];
                        sum += blackRow[left + 2];
                    }

                    int average = sum / 25;
                    blocksAverages[y, x] = average;
                }
            }

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
                        int aver = blocksAverages[y, x];
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
            for (int y = 0; y < blockCountY; y++)
            {
                int startRow = y * BLOCK_SIZE;
                if ((startRow + BLOCK_SIZE) >= height)
                    startRow = height - BLOCK_SIZE;

                for (int x = 0; x < blockCountX; x++)
                {
                    int startColumn = x * BLOCK_SIZE;
                    if ((startColumn + BLOCK_SIZE) >= width)
                        startColumn = width - BLOCK_SIZE;

                    int average = blocksAverages[y, x];
                    // if superblock size > 1 
                    if (useSuperBlocks)
                    {
                        // read superblock average
                        int superX = x / SUPERBLOCK_SIZE;
                        int superY = y / SUPERBLOCK_SIZE;
                        float delta = superBlocksAverages[superY, superX];

                        // calculating average between block  and superblock
                        average = (int) Math.Round((average + delta) / 2f, 0, MidpointRounding.AwayFromZero);
                    }

                    binarizeBlock(startRow, startColumn, average);
                }
            }
        }

        private void binarizeBlock(int startRow, int startColumn, int threshold)
        {
            for (int y = 0; y < BLOCK_SIZE; y++)
            {
                byte[] row = _image.GetRow(startRow + y);
                XBitArray bits = _rows[startRow + y];
                for (int x = 0; x < BLOCK_SIZE; x++)
                {
                    int pixel = row[startColumn + x] & 0xff;
                    bits[startColumn + x] = (pixel < threshold);
                }
            }
        }
    }
}