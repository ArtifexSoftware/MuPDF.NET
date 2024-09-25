namespace MuPDF.NET
{
    public class Border
    {
        /// <summary>
        /// border thickness in points
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// 1-byte border style: `S`, others include `B`, `U`, `I` and `D`.
        /// </summary>
        public string Style { get; set; }

        /// <summary>
        /// a sequence of *int* specifying a line dashing pattern
        /// </summary>
        public int[] Dashes { get; set; }

        /// <summary>
        /// an integer indicating a "cloudy" border
        /// </summary>
        public float Clouds { get; set; }

    }
}
