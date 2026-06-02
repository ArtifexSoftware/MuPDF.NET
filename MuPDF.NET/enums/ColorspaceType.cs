namespace MuPDF.NET
{
    /// <summary>
    /// Device colorspace identifiers (same values as <see cref="Utils.CS_RGB"/> and related constants).
    /// </summary>
    public enum ColorspaceType
    {
        /// <summary>RGB / RGBA device space (<see cref="Utils.CS_RGB"/> = 1).</summary>
        Rgb = 1,

        /// <summary>Grayscale device space (<see cref="Utils.CS_GRAY"/> = 2).</summary>
        Gray = 2,

        /// <summary>CMYK device space (<see cref="Utils.CS_CMYK"/> = 3).</summary>
        Cmyk = 3,
    }
}
