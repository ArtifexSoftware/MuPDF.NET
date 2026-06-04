using MuPDF.NET;

namespace PDF4LLM
{
    /// <summary>Clears the optional layout provider (<c>pymupdf._get_layout = None</c> equivalent).</summary>
    internal static class LayoutActivation
    {
        public static void Deactivate() => Page.GetLayoutProvider = null;
    }
}
