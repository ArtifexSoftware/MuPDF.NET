namespace MuPDF.NET
{
    public class OCLayer
    {
        public int[] On { get; set; }

        public int[] Off { get; set; }

        public int[] Locked { get; set; }

        public List<int[]> RBGroups { get; set; }

        public string BaseState { get; set; }
    }
}
