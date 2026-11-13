using System;
using System.Collections.Generic;
using System.Linq;

namespace MuPDF.NET.PDF4LLM.Helpers.Chunking
{
    internal sealed class BoundaryScorer
    {
        readonly Dictionary<string, double> _weights;
        double _medianGap = 12.0;
        double _medianFont = 10.0;

        public BoundaryScorer(Dictionary<string, double> weights)
        {
            _weights = weights ?? new Dictionary<string, double>();
        }

        public List<double> ScoreAll(List<SentenceUnit> sents)
        {
            if (sents == null || sents.Count < 2)
                return new List<double>();
            var gaps = sents.Where(s => s.LineGapBefore != null && s.LineGapBefore.Value > 0)
                .Select(s => s.LineGapBefore.Value).ToList();
            _medianGap = Median(gaps, 12.0);
            var sizes = sents.Where(s => s.FontSizeDominant > 0).Select(s => s.FontSizeDominant).ToList();
            _medianFont = Median(sizes, 10.0);
            var scores = new List<double>(sents.Count - 1);
            for (int i = 0; i < sents.Count - 1; i++)
                scores.Add(ScorePair(sents[i], sents[i + 1]));
            return scores;
        }

        public double ScorePair(SentenceUnit left, SentenceUnit right)
        {
            double W(string key) => _weights.TryGetValue(key, out double v) ? v : 0;
            double score = 0;
            bool boxChanged = left.BoxIndex != right.BoxIndex || left.PageNo != right.PageNo;
            score += W("w_box") * (boxChanged ? 1 : 0);
            score += W("w_class") * (left.BoxClass != right.BoxClass ? 1 : 0);
            score += W("w_page") * (left.PageNo != right.PageNo ? 1 : 0);
            score += W("w_gap") * ComputeGapJump(right);
            score += W("w_font") * ComputeFontJump(left, right);
            score += W("w_head") * (right.IsHeadingHint && !left.IsHeadingHint ? 1 : 0);
            score += W("w_foot") * (!left.IsFootnote && right.IsFootnote ? 1 : 0);
            score += W("w_hgap") * ComputeHgapJump(left, right);
            score -= W("w_list") * (left.IsListItem && right.IsListItem && !boxChanged ? 1 : 0);
            score -= W("w_table") * (left.IsTableContent && right.IsTableContent ? 1 : 0);
            score -= W("w_caption") * (IsCaptionAdjacent(left, right) ? 1 : 0);
            return score;
        }

        double ComputeGapJump(SentenceUnit right)
        {
            if (right.LineGapBefore == null || _medianGap <= 0)
                return 0;
            double ratio = right.LineGapBefore.Value / _medianGap;
            if (ratio <= 1.5)
                return 0;
            return Math.Min(1.0, (ratio - 1.5) / 2.0);
        }

        double ComputeHgapJump(SentenceUnit left, SentenceUnit right)
        {
            if (left.PageNo != right.PageNo)
                return 0;
            double ratio = ChunkGeometry.HorizontalOverlapRatio(left.Bbox, right.Bbox);
            if (ratio >= 0.5)
                return 0;
            return 1.0 - ratio * 2.0;
        }

        static bool IsCaptionAdjacent(SentenceUnit left, SentenceUnit right)
        {
            if (left.PageNo != right.PageNo)
                return false;
            return ChunkGeometry.CaptionMatchesElement(left, right)
                || ChunkGeometry.CaptionMatchesElement(right, left);
        }

        double ComputeFontJump(SentenceUnit left, SentenceUnit right)
        {
            if (left.FontSizeDominant <= 0 || right.FontSizeDominant <= 0)
                return 0;
            double diff = Math.Abs(left.FontSizeDominant - right.FontSizeDominant);
            if (diff < 0.5)
                return 0;
            if (_medianFont > 0)
                return Math.Min(1.0, diff / _medianFont);
            return Math.Min(1.0, diff / 10.0);
        }

        static double Median(List<double> xs, double fallback)
        {
            if (xs == null || xs.Count == 0)
                return fallback;
            xs.Sort();
            int m = xs.Count / 2;
            if ((xs.Count % 2) == 1)
                return xs[m];
            return (xs[m - 1] + xs[m]) / 2.0;
        }
    }
}
