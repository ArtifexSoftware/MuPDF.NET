namespace MuPDF.NET
{
    public class Color
    {
        /// <summary>
        /// fill color, each item is between 0 and 1
        /// </summary>
        public float[] Fill { get; set; }

        /// <summary>
        /// stroke color
        /// </summary>
        public float[] Stroke { get; set; }
    }
}
