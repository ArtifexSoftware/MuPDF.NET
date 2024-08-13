namespace MuPDF.NET
{
    public enum PermissionCodes
    {
        PDF_PERM_PRINT = 1 << 2,
        PDF_PERM_MODIFY = 1 << 3,
        PDF_PERM_COPY = 1 << 4,
        PDF_PERM_ANNOTATE = 1 << 5,
        PDF_PERM_FORM = 1 << 8,
        PDF_PERM_ACCESSIBILITY = 1 << 9, /* deprecated in pdf 2.0 (this permission is always granted) */
        PDF_PERM_ASSEMBLE = 1 << 10,
        PDF_PERM_PRINT_HQ = 1 << 11,
    }
}
