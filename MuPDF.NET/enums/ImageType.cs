namespace MuPDF.NET
{
    public enum ImageType
    {
        FZ_IMAGE_UNKNOWN = 0,

        /* Uncompressed samples */
        FZ_IMAGE_RAW,

        /* Compressed samples */
        FZ_IMAGE_FAX,
        FZ_IMAGE_FLATE,
        FZ_IMAGE_LZW,
        FZ_IMAGE_RLD,

        /* Full image formats */
        FZ_IMAGE_BMP,
        FZ_IMAGE_GIF,
        FZ_IMAGE_JBIG2,
        FZ_IMAGE_JPEG,
        FZ_IMAGE_JPX,
        FZ_IMAGE_JXR,
        FZ_IMAGE_PNG,
        FZ_IMAGE_PNM,
        FZ_IMAGE_TIFF,
        FZ_IMAGE_PSD,
    }
}
