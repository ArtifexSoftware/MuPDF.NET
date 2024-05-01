namespace MuPDF.NET
{
    public class OCLayerConfig
    {
        public int Number { get; set; }

        public string Name { get; set; }

        public string Creator { get; set; }

        public OCLayerConfig(int number, string name, string creator)
        {
            Number = number;
            Name = name;
            Creator = creator;
        }
    }
}
