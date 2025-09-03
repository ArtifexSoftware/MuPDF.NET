using System;
using System.IO;
using System.Reflection;

namespace BarcodeReader.Core.Datamatrix
{
    // stores the placement matrices used in ECC000-140
    class ModulePlacementNonECC200
    {
        private const int SmallestGrid = 7;
        private const int LargestGrid = 47;
        private const int GridCount = (LargestGrid - SmallestGrid) / 2 + 1;
		private readonly ushort[][][] placementGrids = new ushort[GridCount][][];
        private static ModulePlacementNonECC200 instance;

        public static void Init(string resourceName)
        {
            if (instance == null)
            {
                instance = new ModulePlacementNonECC200(resourceName);
            }
        }

        private ModulePlacementNonECC200(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourcePath = null;
            foreach (string n in assembly.GetManifestResourceNames())
                if (n.EndsWith(resourceName, StringComparison.Ordinal))
                {
                    resourcePath = n;
                    break;
                }
            
            var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
                throw new Exception("Could not load embedded resource.");
            BinaryReader bw = new BinaryReader(stream);
            for (int i = 0; i < GridCount; ++i)
            {
                int size = bw.ReadUInt16();
                placementGrids[i] = new ushort[size][];
                for (int y = 0; y < size; ++y)
                {
					placementGrids[i][y] = new ushort[size];

                    for (int x = 0; x < size; ++x)
                    {
						placementGrids[i][y][x] = bw.ReadUInt16();
                    }
                }
            }
        }

		public static ushort[][] GetPlacementGrid(int rows, int columns)
        {
            if (rows != columns || rows % 2 != 1)
            {
                return null;
            }
            return instance.placementGrids[(rows-7)/2];
        }


        // Use this code snippet if you need to convert the module placement txt file to dat format.
        // Shouldn't be needed though.
        public static void Convert(string from, string to)
        {
            FileStream fs = new FileStream(from, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
			short[][][] placementGrids = new short[GridCount][][];
            for (int i = 0; i < GridCount; ++i)
            {
				int size = int.Parse(sr.ReadLine());
                placementGrids[i] = new short[size][];
            	for (int y = 0; y < size; y++)
            		placementGrids[i][y] = new short[size];

                for (int y = size - 1; y >= 0; --y)
                {
                    string row = sr.ReadLine();
                    string[] idxs = row.Split(' ');
                    for (int x = 0; x < size; ++x)
                    {
						placementGrids[i][y][x] = short.Parse(idxs[x]);
                    }
                }
            }

            FileStream os = new FileStream(to, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(os);
            for (int i = 0; i < GridCount; ++i)
            {
                short size = (short) placementGrids[i].Length;
                bw.Write(size);
                for (int y = 0; y < size; ++y)
                {
                    for (int x = 0; x < size; ++x)
                    {
						bw.Write(placementGrids[i][y][x]);
                    }
                }
            }
            bw.Flush();
            os.Close();
        }
    }

    // implements the module placement matrix generator algorithm introduced in the Datamatrix specification
    class ModulePlacementECC200
    {
        private int rowCount;
        private int columnCount;
        private int[] array;


        /* "Module" places "chr+bit" with appropriate wrapping within array[] */
        private void Module(int row, int col, int chr, int bit)
        {
            if (row < 0) { row += rowCount; col += 4 - ((rowCount + 4) % 8); }
            if (col < 0) { col += columnCount; row += 4 - ((columnCount + 4) % 8); }
            array[row * columnCount + col] = 10 * chr + bit;
        }
        /* "Utah" places the 8 bits of a Utah-shaped symbol character in ECC200 */
        private void Utah(int row, int col, int chr)
        {
            Module(row - 2, col - 2, chr, 1);
            Module(row - 2, col - 1, chr, 2);
            Module(row - 1, col - 2, chr, 3);
            Module(row - 1, col - 1, chr, 4);
            Module(row - 1, col, chr, 5);
            Module(row, col - 2, chr, 6);
            Module(row, col - 1, chr, 7);
            Module(row, col, chr, 8);
        }
        /* "cornerN" places 8 bits of the four special corner cases in ECC200 */
        private void Corner1(int chr)
        {
            Module(rowCount - 1, 0, chr, 1);
            Module(rowCount - 1, 1, chr, 2);
            Module(rowCount - 1, 2, chr, 3);
            Module(0, columnCount - 2, chr, 4);
            Module(0, columnCount - 1, chr, 5);
            Module(1, columnCount - 1, chr, 6);
            Module(2, columnCount - 1, chr, 7);
            Module(3, columnCount - 1, chr, 8);
        }
        private void Corner2(int chr)
        {
            Module(rowCount - 3, 0, chr, 1);
            Module(rowCount - 2, 0, chr, 2);
            Module(rowCount - 1, 0, chr, 3);
            Module(0, columnCount - 4, chr, 4);
            Module(0, columnCount - 3, chr, 5);
            Module(0, columnCount - 2, chr, 6);
            Module(0, columnCount - 1, chr, 7);
            Module(1, columnCount - 1, chr, 8);
        }
        private void Corner3(int chr)
        {
            Module(rowCount - 3, 0, chr, 1);
            Module(rowCount - 2, 0, chr, 2);
            Module(rowCount - 1, 0, chr, 3);
            Module(0, columnCount - 2, chr, 4);
            Module(0, columnCount - 1, chr, 5);
            Module(1, columnCount - 1, chr, 6);
            Module(2, columnCount - 1, chr, 7);
            Module(3, columnCount - 1, chr, 8);
        }
        private void Corner4(int chr)
        {
            Module(rowCount - 1, 0, chr, 1);
            Module(rowCount - 1, columnCount - 1, chr, 2);
            Module(0, columnCount - 3, chr, 3);
            Module(0, columnCount - 2, chr, 4);
            Module(0, columnCount - 1, chr, 5);
            Module(1, columnCount - 3, chr, 6);
            Module(1, columnCount - 2, chr, 7);
            Module(1, columnCount - 1, chr, 8);
        }
        /* "ECC200" fills an nrow x ncol array with appropriate values for ECC200 */
        private void ECC200()
        {
            int row, col;
            /* First, fill the array[] with invalid entries */
            for (row = 0; row < rowCount; row++)
            {
                for (col = 0; col < columnCount; col++)
                {
                    array[row * columnCount + col] = 0;
                }
            }
            /* Starting in the correct location for character #1, bit 8,... */
            int chr = 1; row = 4; col = 0;
            do
            {
                /* repeatedly first check for one of the special corner cases, then... */
                if ((row == rowCount) && (col == 0)) Corner1(chr++);
                if ((row == rowCount - 2) && (col == 0) && (columnCount % 4 != 0)) Corner2(chr++);
                if ((row == rowCount - 2) && (col == 0) && (columnCount % 8 == 4)) Corner3(chr++);
                if ((row == rowCount + 4) && (col == 2) && (!(columnCount % 8 != 0))) Corner4(chr++);
                /* sweep upward diagonally, inserting successive characters,... */
                do
                {
                    if ((row < rowCount) && (col >= 0) && (array[row * columnCount + col] == 0))
                        Utah(row, col, chr++);
                    row -= 2; col += 2;
                } while ((row >= 0) && (col < columnCount));
                row += 1; col += 3;
                /* & then sweep downward diagonally, inserting successive characters,... */
                do
                {
                    if ((row >= 0) && (col < columnCount) && (array[row * columnCount + col] == 0))
                        Utah(row, col, chr++);
                    row += 2; col -= 2;
                } while ((row < rowCount) && (col >= 0));
                row += 3; col += 1;
                /* ... until the entire array is scanned */
            } while ((row < rowCount) || (col < columnCount));
            /* Lastly, if the lower righthand corner is untouched, fill in fixed pattern */
            if (array[rowCount * columnCount - 1] == 0) //!=0  23/12/2013
            {
                array[rowCount * columnCount - 1] = array[rowCount * columnCount - columnCount - 2] = 1;
            }
        }

	    public MyPoint[][] GetPlacementGridP(int rows, int columns)
        {
            rowCount = rows;
            columnCount = columns;
            if ((rowCount < 6) || (rowCount % 2 != 0) || (columnCount < 6) || (columnCount % 2 != 0)) return null;
            array = new int[rowCount * columnCount];
            ECC200();
			
#if DEBUG
            //for (int i = 0; i < rowCount; i++)
            //{
            //    for (int j = 0; j < columnCount; j++) Console.Write(" " + array[i * columnCount + j]);
            //    Console.WriteLine();
            //}
#endif


			MyPoint[][] grid = new MyPoint[rows][];
			for (int y = 0; y < rows; y++)
				grid[y] = new MyPoint[columns];

			for (int y = 0; y < rowCount; y++)
            {
                for (int x = 0; x < columnCount; x++)
                {
                    int z = array[y * columnCount + x];
                    if (z == 0)
                    {
						grid[rowCount - y - 1][x] = MyPoint.Empty;
                        // "WHITE"
                    }
                    else if (z == 1)
                    {
						grid[rowCount - y - 1][x] = MyPoint.Empty;
                        // "BLACK"
                    }
                    else
                    {
                        // byte.bit format stored in MyPoint
						grid[rowCount - y - 1][x] = new MyPoint(z / 10 - 1, 8 - z % 10);
                    }
                }
            }

            return grid;
        }
    }
}
