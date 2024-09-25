namespace MuPDF.NET
{
    public class FileInfo
    {
        /// <summary>
        /// file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// description of the file
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// compressed length
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// uncompressed file size
        /// </summary>
        public int Size { get; set; }
    }
}
