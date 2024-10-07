namespace MuPDF.NET
{
    public class AnnotInfo
    {
        /// <summary>
        /// a string containing the text for type Text and FreeText annotations
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// name of annot's icon *string*
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// a string containing the title of the annotation pop-up window.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// creation timestamp
        /// </summary>
        public string CreationDate { get; set; }

        /// <summary>
        /// last modified timestamp
        /// </summary>
        public string ModDate { get; set; }

        /// <summary>
        /// subject string
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// a unique identification of the annotation
        /// </summary>
        public string Id { get; set; }
    }
}
