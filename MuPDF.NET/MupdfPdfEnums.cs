namespace mupdf
{
    /// <summary>
    /// PDF boolean/null <see cref="PdfObj"/> singletons exposed on PyMuPDF's <c>mupdf</c> module.
    /// <para>Ports <c>obj_enum_to_obj</c>, <c>PDF_NULL</c>, <c>PDF_TRUE</c>, and <c>PDF_FALSE</c> from generated <c>mupdf.py</c>
    /// (not on the SWIG <see cref="mupdf"/> class itself).</para>
    /// </summary>
    public static class MupdfPdfEnums
    {
        /// <summary>
        /// Wrap a PDF object enum value as <see cref="PdfObj"/> (PyMuPDF <c>obj_enum_to_obj</c>).
        /// </summary>
        /// <param name="n">Enum handle (e.g. <c>PDF_ENUM_NULL</c>).</param>
        public static PdfObj ObjEnumToObj(int n) => new PdfObj((System.IntPtr)n, cMemoryOwn: false);

        /// <summary>PDF null object (PyMuPDF <c>mupdf.PDF_NULL</c> / <c>PDF_ENUM_NULL</c>).</summary>
        public static readonly PdfObj PdfNull = ObjEnumToObj(mupdf.PDF_ENUM_NULL);

        /// <summary>PDF true object (PyMuPDF <c>mupdf.PDF_TRUE</c> / <c>PDF_ENUM_TRUE</c>).</summary>
        public static readonly PdfObj PdfTrue = ObjEnumToObj(mupdf.PDF_ENUM_TRUE);

        /// <summary>PDF false object (PyMuPDF <c>mupdf.PDF_FALSE</c> / <c>PDF_ENUM_FALSE</c>).</summary>
        public static readonly PdfObj PdfFalse = ObjEnumToObj(mupdf.PDF_ENUM_FALSE);

        // ─── PyMuPDF names (internal, same assembly) ─────────────────────

        internal static PdfObj obj_enum_to_obj(int n) => ObjEnumToObj(n);

        internal static readonly PdfObj PDF_NULL = PdfNull;

        internal static readonly PdfObj PDF_TRUE = PdfTrue;

        internal static readonly PdfObj PDF_FALSE = PdfFalse;
    }
}
