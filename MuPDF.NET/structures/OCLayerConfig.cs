namespace MuPDF.NET
{
    public class OCLayerConfig
    {
        /// <summary>
        /// numbers to set ON in the layer
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// the layer name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// creation date
        /// </summary>
        public string Creator { get; set; }

        public OCLayerConfig(int number, string name, string creator)
        {
            Number = number;
            Name = name;
            Creator = creator;
        }
    }
}
