using MuPDF.NET;
using MuPDF.NET.PDF4LLM.Layout;

namespace MuPDF.NET.PDF4LLM
{
    internal static class LayoutActivation
    {
        public static void Activate() => PyMuPdfLayoutBridge.TryActivate();

        public static void Deactivate() => PyMuPdfLayoutBridge.Deactivate();
    }
}
