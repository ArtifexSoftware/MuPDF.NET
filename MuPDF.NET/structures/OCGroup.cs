namespace MuPDF.NET
{
    public class OCGroup
    {
        /// <summary>
        /// the name of the group
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// the intended use of the group
        /// </summary>
        public List<string> Intents { get; set; }

        /// <summary>
        /// the state of the group
        /// </summary>
        public int On { get; set; }

        /// <summary>
        /// the usage of the group
        /// </summary>
        public string Usage { get; set; }
    }
}
