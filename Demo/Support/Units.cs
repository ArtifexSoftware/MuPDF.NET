namespace Demo
{
    public static class Units
    {
        public const float InchesPerMm = 1.0f / 25.4f;
        public const float PointsPerInch = 72.0f;

        public static float MmToPoints(float mm) => mm * InchesPerMm * PointsPerInch;
        public static float PointsToMm(float points) => points / PointsPerInch / InchesPerMm;

        public static float MmToPixels(float mm, float dpi) => mm * InchesPerMm * dpi;
        public static float PixelsToMm(float px, float dpi) => px / dpi / InchesPerMm;
    }
}
