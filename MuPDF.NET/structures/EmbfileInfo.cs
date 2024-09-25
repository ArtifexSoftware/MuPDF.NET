namespace MuPDF.NET
{
    public class EmbfileInfo
    {
        /// <summary>
        /// name under which this entry is stored
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// date-time of item creation in PDF format
        /// </summary>
        public string CreationDate { get; set; }

        /// <summary>
        /// date-time of last change in PDF format
        /// </summary>
        public string ModDate { get; set; }

        /// <summary>
        /// a hashcode of the stored file content as a hexadecimal string
        /// </summary>
        public string CheckSum { get; set; }

        /// <summary>
        /// xref of the associated PDF portfolio item if any, else zero
        /// </summary>
        public int Collection { get; set; }

        /// <summary>
        /// filename
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// filename
        /// </summary>
        public string UFileName { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// original file size
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// compressed file length
        /// </summary>
        public int Length { get; set; }
    }
}
