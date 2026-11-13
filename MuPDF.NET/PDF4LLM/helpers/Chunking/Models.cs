using System;
using System.Collections.Generic;

namespace MuPDF.NET.PDF4LLM.Helpers.Chunking
{
    internal static class ChunkIds
    {
        public static string ElementId(int page, int box) => $"p{page}.b{box}";
    }

    internal sealed class SentenceUnit
    {
        public int SentId { get; set; }
        public string Text { get; set; }
        public string NormText { get; set; }
        public int PageNo { get; set; }
        public int BoxIndex { get; set; }
        public string BoxClass { get; set; }
        public double[] Bbox { get; set; }
        public int TokenCount { get; set; }
        public double FontSizeDominant { get; set; }
        public int FontFlagsDominant { get; set; }
        public double? LineGapBefore { get; set; }
        public double? LineGapAfter { get; set; }
        public bool IsHeadingHint { get; set; }
        public int? HeadingLevelHint { get; set; }
        public bool IsListItem { get; set; }
        public bool IsTableContent { get; set; }
        public bool IsFigureRelated { get; set; }
        public bool IsFootnote { get; set; }
        public bool IsHeaderFooter { get; set; }
        public bool IsCaption { get; set; }
        public string CaptionTargetType { get; set; }
        public string TableMarkdown { get; set; }
        public string TableHtml { get; set; }
        public int? FigureNumber { get; set; }
        public byte[] ImageData { get; set; }
        public List<int> SourceBoxIndices { get; set; } = new List<int>();
    }

    internal sealed class ProtoChunk
    {
        public int ChunkId { get; set; }
        public List<int> SentIds { get; set; } = new List<int>();
        public string Text { get; set; } = "";
        public int TokenCount { get; set; }
        public int[] UnitRange { get; set; } = new[] { 0, 0 };
        public int PageStart { get; set; }
        public int PageEnd { get; set; }
        public List<(int page, int box)> BoxIndices { get; set; } = new List<(int, int)>();
        public List<double[]> Bboxes { get; set; } = new List<double[]>();
        public string PrimaryType { get; set; }
        public List<string> Types { get; set; } = new List<string>();
        public List<SentenceUnit> Sentences { get; set; } = new List<SentenceUnit>();
    }

    internal static class ChunkGeometry
    {
        public static bool CaptionMatchesElement(SentenceUnit caption, SentenceUnit element)
        {
            if (caption == null || !caption.IsCaption)
                return false;
            if (element == null || !(element.IsTableContent || element.IsFigureRelated || element.IsListItem))
                return false;
            if (caption.CaptionTargetType == null)
                return true;
            if (caption.CaptionTargetType == "table" && element.IsTableContent)
                return true;
            if (caption.CaptionTargetType == "figure" && element.IsFigureRelated)
                return true;
            return false;
        }

        public static double HorizontalOverlapRatio(double[] bboxA, double[] bboxB)
        {
            if (bboxA == null || bboxB == null || bboxA.Length < 4 || bboxB.Length < 4)
                return 1.0;
            double ax0 = bboxA[0], ax1 = bboxA[2];
            double bx0 = bboxB[0], bx1 = bboxB[2];
            double minWidth = Math.Min(ax1 - ax0, bx1 - bx0);
            if (minWidth <= 0)
                return 1.0;
            double overlap = Math.Max(0.0, Math.Min(ax1, bx1) - Math.Max(ax0, bx0));
            return overlap / minWidth;
        }

        public static double[] UnionBbox(IEnumerable<double[]> bboxes)
        {
            double x0 = double.PositiveInfinity, y0 = double.PositiveInfinity;
            double x1 = double.NegativeInfinity, y1 = double.NegativeInfinity;
            bool any = false;
            if (bboxes != null)
            {
                foreach (var b in bboxes)
                {
                    if (b == null || b.Length < 4)
                        continue;
                    any = true;
                    x0 = Math.Min(x0, b[0]);
                    y0 = Math.Min(y0, b[1]);
                    x1 = Math.Max(x1, b[2]);
                    y1 = Math.Max(y1, b[3]);
                }
            }
            return any ? new[] { x0, y0, x1, y1 } : new[] { 0.0, 0.0, 0.0, 0.0 };
        }

        static bool HasSharedEdge(double[] a, double[] b, double tol)
        {
            bool xPair = Math.Abs(a[0] - b[0]) <= tol && Math.Abs(a[2] - b[2]) <= tol;
            bool yPair = Math.Abs(a[1] - b[1]) <= tol && Math.Abs(a[3] - b[3]) <= tol;
            return xPair || yPair;
        }

        public static List<double[]> GroupBboxes(IEnumerable<double[]> bboxes, double tolerance = 10.0)
        {
            var boxes = new List<double[]>();
            if (bboxes != null)
            {
                foreach (var b in bboxes)
                {
                    if (b != null && b.Length >= 4)
                        boxes.Add(b);
                }
            }
            if (boxes.Count == 0)
                return new List<double[]>();
            if (boxes.Count == 1)
                return new List<double[]> { boxes[0] };

            var groups = new List<List<double[]>> { new List<double[]> { boxes[0] } };
            for (int i = 1; i < boxes.Count; i++)
            {
                var lastUnion = UnionBbox(groups[groups.Count - 1]);
                if (HasSharedEdge(lastUnion, boxes[i], tolerance))
                    groups[groups.Count - 1].Add(boxes[i]);
                else
                    groups.Add(new List<double[]> { boxes[i] });
            }
            var result = new List<double[]>();
            foreach (var g in groups)
                result.Add(UnionBbox(g));
            return result;
        }
    }
}
