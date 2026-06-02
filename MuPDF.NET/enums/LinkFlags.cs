namespace MuPDF.NET
{
    public enum LinkFlags
    {
        LINK_FLAG_L_VALID = 1,
        LValid = LINK_FLAG_L_VALID,

        LINK_FLAG_T_VALID = 2,
        TValid = LINK_FLAG_T_VALID,

        LINK_FLAG_R_VALID = 4,
        RValid = LINK_FLAG_R_VALID,

        LINK_FLAG_B_VALID = 8,
        BValid = LINK_FLAG_B_VALID,

        LINK_FLAG_FIT_H = 16,
        FitH = LINK_FLAG_FIT_H,

        LINK_FLAG_FIT_V = 32,
        FitV = LINK_FLAG_FIT_V,

        LINK_FLAG_R_IS_ZOOM = 64,
        RIsZoom = LINK_FLAG_R_IS_ZOOM,
    }
}
