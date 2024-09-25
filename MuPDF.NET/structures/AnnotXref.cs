namespace MuPDF.NET
{
    public class AnnotXref
    {
        /// <summary>
        /// a unique identification of the annotation
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// annot's xref
        /// </summary>
        public int Xref { get; set; }

        /// <summary>
        /// annotation type
        /// </summary>
        public PdfAnnotType AnnotType { get; set; }
    }
}
