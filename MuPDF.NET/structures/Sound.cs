namespace MuPDF.NET
{
    public class Sound
    {
        /// <summary>
        /// samples per second
        /// </summary>
        public float Rate { get; set; }

        /// <summary>
        /// number of sound channels
        /// </summary>
        public int Channels { get; set; }

        /// <summary>
        /// bits per sample value per channel
        /// </summary>
        public int Bps { get; set; }

        /// <summary>
        /// encoding format: Raw, Signed, muLaw, ALaw
        /// </summary>
        public string Encoding { get; set; }

        /// <summary>
        /// name of compression filter
        /// </summary>
        public string Compression { get; set; }

        /// <summary>
        /// the sound file content
        /// </summary>
        public byte[] Stream { get; set; }
    }
}
