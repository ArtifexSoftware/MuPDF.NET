using mupdf;

namespace MuPDF.NET
{
    public class Block
    {
        public int Xref { get; set; }

        /// <summary>
        /// block count
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// block type, 0 = text, 1 = image
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// block rectangle
        /// </summary>
        public Rect Bbox { get; set; }

        /// <summary>
        /// original image width
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// original image height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// image type, as file extension
        /// </summary>
        public string Ext { get; set; }

        /// <summary>
        /// colorspace component count
        /// </summary>
        public int ColorSpace { get; set; }

        /// <summary>
        /// resolution in x-direction
        /// </summary>
        public int Xres { get; set; }

        /// <summary>
        /// resolution in y-direction
        /// </summary>
        public int Yres { get; set; }

        /// <summary>
        /// bits per component
        /// </summary>
        public byte Bpc { get; set; }

        /// <summary>
        /// matrix transforming image rect to bbox
        /// </summary>
        public Matrix Transform { get; set; }

        /// <summary>
        /// size of the image in bytes
        /// </summary>
        public uint Size { get; set; }

        /// <summary>
        /// image content
        /// </summary>
        public byte[] Image { get; set; }

        public string CsName { get; set; }

        public vectoruc Digest { get; set; }

        public List<Line> Lines { get; set; }
    }
}
