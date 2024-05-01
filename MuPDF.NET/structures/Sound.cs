namespace MuPDF.NET
{
    public class Sound
    {
        public float Rate { get; set; }

        public int Channels { get; set; }

        public int Bps { get; set; }

        public string Encoding { get; set; }

        public string Compression { get; set; }

        public byte[] Stream { get; set; }
    }
}
