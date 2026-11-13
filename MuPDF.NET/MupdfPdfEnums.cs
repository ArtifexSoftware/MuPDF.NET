namespace mupdf
{
    /// <summary>
    /// PDF boolean/null <see cref="PdfObj"/> singletons on the <c>mupdf</c> module
    /// (<c>PDF_NULL</c>, <c>PDF_TRUE</c>, <c>PDF_FALSE</c>), not on the SWIG class itself.
    /// </summary>
    public static class MupdfPdfEnums
    {

        /// <summary>
        /// Wrap a PDF object enum value as <see cref="PdfObj"/>.
        /// </summary>
        /// <param name="n">Enum handle (e.g. <c>PDF_ENUM_NULL</c>).</param>
        public static PdfObj ObjEnumToObj(int n) => new PdfObj((System.IntPtr)n, cMemoryOwn: false);

        /// <summary>PDF null object (/ <c>PDF_ENUM_NULL</c>).</summary>
        public static readonly PdfObj PdfNull = ObjEnumToObj(mupdf.PDF_ENUM_NULL);

        /// <summary>PDF true object (/ <c>PDF_ENUM_TRUE</c>).</summary>
        public static readonly PdfObj PdfTrue = ObjEnumToObj(mupdf.PDF_ENUM_TRUE);

        /// <summary>PDF false object (/ <c>PDF_ENUM_FALSE</c>).</summary>
        public static readonly PdfObj PdfFalse = ObjEnumToObj(mupdf.PDF_ENUM_FALSE);

        // ─── MuPDF names (internal, same assembly) ─────────────────────

        internal static PdfObj obj_enum_to_obj(int n) => ObjEnumToObj(n);

        internal static readonly PdfObj PDF_NULL = PdfNull;

        internal static readonly PdfObj PDF_TRUE = PdfTrue;

        internal static readonly PdfObj PDF_FALSE = PdfFalse;
    }
}