namespace MuPDF.NET
{
    public class ImageInfo
    {
        public string Ext { get; set; }

        public int Smask { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        public int ColorSpace { get; set; }

        public int Bpc { get; set; }

        public float Xres { get; set; }

        public float Yres { get; set; }

        public string CsName { get; set; }

        public byte[] Image { get; set; }

        public byte Orientation { get; set; }

        public Matrix Matrix { get; set; }
    }
}
