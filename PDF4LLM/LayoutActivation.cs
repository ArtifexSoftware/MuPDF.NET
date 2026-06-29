using MuPDF.NET;
using PDF4LLM.Layout;

namespace PDF4LLM
{
    internal static class LayoutActivation
    {
        public static void Activate() => PyMuPdfLayoutBridge.TryActivate();

        public static void Deactivate() => PyMuPdfLayoutBridge.Deactivate();
    }
}
