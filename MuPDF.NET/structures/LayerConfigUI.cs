namespace MuPDF.NET
{
    public class LayerConfigUI
    {
        /// <summary>
        /// running sequence number
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// text string or name field of the originating OCG
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// one of "label" (set by a text string), "checkbox" (set by a single OCG) or "radiobox" (set by a set of connected OCGs)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// item's nesting level in the `/Order` array
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// item state
        /// </summary>
        public bool On { get; set; }

        /// <summary>
        /// true if cannot be changed via user interfaces
        /// </summary>
        public bool IsLocked { get; set; }
    }
}
