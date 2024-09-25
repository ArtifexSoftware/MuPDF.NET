namespace MuPDF.NET
{
    public class OCLayer
    {
        /// <summary>
        /// list of xref of OCGs to set ON.
        /// </summary>
        public int[] On { get; set; }

        /// <summary>
        /// list of xref of OCGs to set OFF
        /// </summary>
        public int[] Off { get; set; }

        /// <summary>
        /// a list of OCG xref number that cannot be changed by the user interface
        /// </summary>
        public int[] Locked { get; set; }

        /// <summary>
        /// a list of lists. Replaces previous values
        /// </summary>
        public List<int[]> RBGroups { get; set; }

        /// <summary>
        /// state of OCGs that are not mentioned in on or off
        /// </summary>
        public string BaseState { get; set; }
    }
}
