using System.Collections.Generic;

namespace PDF4LLM.Ocr
{
    /// <summary>
    /// ONNX OCR decision model loader (<c>ocr_decision_model.onnx</c>).
    /// </summary>
    /// <remarks>
    /// Place <c>ocr_decision_model.onnx</c> next to the PDF4LLM assembly (or under
    /// <c>ocr/</c>) and reference <c>Microsoft.ML.OnnxRuntime</c> to enable ML-based
    /// OCR page selection. Without the model, <see cref="AnalyzePage.PredictOcrProbability"/>
    /// returns 0 and heuristic checks still apply.
    /// </remarks>
    internal static class OcrDecisionModel
    {
        public static float Predict(Dictionary<string, float> features) => 0f;
    }
}
