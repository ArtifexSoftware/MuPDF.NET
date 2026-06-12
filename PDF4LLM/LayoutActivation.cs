using MuPDF.NET;

namespace PDF4LLM
{
    internal static class LayoutActivation
    {
        public static void Deactivate() => Page.GetLayoutProvider = null;
    }
}
