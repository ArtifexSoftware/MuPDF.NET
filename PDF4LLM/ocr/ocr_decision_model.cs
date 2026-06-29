using System;
using System.IO;

namespace PDF4LLM.Ocr
{
    /// <summary>
    /// ONNX model path resolution for <c>ocr_decision_model.onnx</c>.
    /// </summary>
    internal static class OcrDecisionModel
    {
        public const string ModelFileName = "ocr_decision_model.onnx";

        /// <summary>
        /// Mirror <c>Path(__file__).parent / "ocr_decision_model.onnx"</c>.
        /// </summary>
        public static string ResolveModelPath()
        {
            string baseDir = Path.GetDirectoryName(typeof(OcrDecisionModel).Assembly.Location);
            if (string.IsNullOrEmpty(baseDir))
                baseDir = AppContext.BaseDirectory;

            string[] candidates =
            {
                Path.Combine(baseDir, ModelFileName),
                Path.Combine(baseDir, "ocr", ModelFileName),
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }
    }
}
