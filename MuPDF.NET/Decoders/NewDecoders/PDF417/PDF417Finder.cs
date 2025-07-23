namespace BarcodeReader.Core.PDF417
{
    class PDF417Finder
    {
        //Pattern of PDF417 finder
        //start and reversed stop patterns: used in the main horizontal scan method.
        public static readonly int[][] start = new int[][] { new int[] { 8, 1, 1, 1, 1, 1, 1, 3 }, new int[] { 1, 2, 1, 1, 1, 3, 1, 1, 7 } };

        //stop pattern: used in the secondary scan method, once start pattern has been found
        public static readonly int[] stop = { 7, 1, 1, 3, 1, 1, 1, 2, 1 };
        //reversed start pattern: used in the secondary scan method, once reversed stop pattern has been found
        public static readonly int[] startReverse = { 3, 1, 1, 1, 1, 1, 1, 8 };

        //start pattern in E format (adding 2 consecutive modules). Used to scan a given barcode region.
        public static readonly int[] startE = { 9, 2, 2, 2, 2, 2, 4 };

        public static float ModuleLength(float d)
        {
            return d / 17F;
        }

    }
}
