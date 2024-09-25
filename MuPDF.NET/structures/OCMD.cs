namespace MuPDF.NET
{
    public class OCMD
    {
        /// <summary>
        /// xref of the OCMD to be updated, or 0 for a new OCMD
        /// </summary>
        public int Xref { get; set; }

        /// <summary>
        /// a sequence of xref numbers of existing OCG PDF objects
        /// </summary>
        public int[] Ocgs { get; set; }

        /// <summary>
        /// one of "AnyOn" (default), "AnyOff", "AllOn", "AllOff" 
        /// </summary>
        public string Policy { get; set; }

        /// <summary>
        /// visibility expression
        /// </summary>
        public dynamic[] Ve { get; set; }
    }
}
