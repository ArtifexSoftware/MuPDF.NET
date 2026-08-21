// Copyright (C) 2023 Artifex Software, Inc.
//
// This file is part of MuPDF.NET.
//
// MuPDF.NET is free software: you can redistribute it and/or modify it under the
// terms of the GNU Affero General Public License as published by the Free
// Software Foundation, either version 3 of the License, or (at your option)
// any later version.
//
// MuPDF.NET is distributed in the hope that it will be useful, but WITHOUT ANY
// WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with MuPDF. If not, see <https://www.gnu.org/licenses/agpl-3.0.en.html>
//
// Alternative licensing terms are available from the licensor.
// For commercial licensing, see <https://www.artifex.com/> or contact
// Artifex Software, Inc., 39 Mesa Street, Suite 108A, San Francisco,
// CA 94129, USA, for further information.
//
// ---------------------------------------------------------------------
//
// Port of PyMuPDF 1.28.2 src/_table_headers.py
//
// MuPDF.NET table header detection and HTML serialization (opt-in extension).
// Pure text-grid module: the header-region rules operate on a row-major
// [[cell text]] grid, and the serializer turns a tagged placement grid into
// an HTML <table>. Used only by FindTables(refine: true) (via Table.cs) and
// Table.ToHtml(); never runs on the default detection path.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MuPDF.NET
{
    /// <summary>
    /// Table header detection and HTML serialization (opt-in extension).
    /// Pure text-grid port of PyMuPDF <c>_table_headers</c>: header-region rules
    /// on a row-major <c>[[cell text]]</c> grid, plus tagged-placement HTML
    /// serialization. Used only by <c>FindTables(refine: true)</c> and
    /// <see cref="Table.ToHtml"/>; never runs on the default detection path.
    /// </summary>
    internal static partial class TableHeaders
    {
        static readonly Regex TokenRe = new Regex(
            @"[A-Za-z]+|\d+(?:[.,:/-]\d+)*|[%$€£¥()–—-]+",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex MonthRe = new Regex(
            @"\b(?:jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\.?\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex MoneyRe = new Regex(
            @"(?:[$€£¥]\s*[-(]?\d|(?:usd|eur|gbp|jpy)\b)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex PercentRe = new Regex(
            @"[-(]?\d+(?:\.\d+)?\s*%",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex YearRe = new Regex(
            @"\b(?:19|20)\d{2}\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex CodeRe = new Regex(
            @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z0-9][A-Za-z0-9._/# -]{0,24}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex RangeRe = new Regex(
            @"(?:>=|<=|<|>| thru | through | to |\b\d+\s*[-\u2013]\s*\d+\b)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex DottedEnumRe = new Regex(
            @"^\s*\d+(?:\.\d+){2,}\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex RefTokenRe = new Regex(
            @"[\[(]\s*\d{1,2}\s*[\])]|\[\s*\d{1,2}\s*\)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex RefFormulaAllowedRe = new Regex(
            @"^[\s\d\[\]\(\)=+\-*/xX.]+$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly string[] UnitRowTerms =
            { "in million", "in thousand", "in billion", "$ in", "usd", "'000", "%" };
        static readonly HashSet<string> ValueTypes =
            new HashSet<string>(StringComparer.Ordinal) { "number", "money", "percent" };
        static readonly HashSet<string> LabelTypes =
            new HashSet<string>(StringComparer.Ordinal)
            { "text_label", "long_text", "code_id", "date_period", "unit" };

        static readonly Regex PeriodKeywordRe = new Regex(
            @"\b(?:q[1-4]|fy|year|month|period)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        static readonly Regex SimpleIntRe = new Regex(
            @"^\d{1,2}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        static bool FullMatch(Regex re, string text)
        {
            var m = re.Match(text ?? "");
            return m.Success && m.Index == 0 && m.Length == text.Length;
        }

        static Dictionary<string, float> AsVector(object obj)
            => (Dictionary<string, float>)obj;

        internal static int CountAlpha(string text)
            => (text ?? "").Count(char.IsLetter);

        internal static int CountDigit(string text)
            => (text ?? "").Count(char.IsDigit);

        internal static List<string> Tokens(string text)
            => TokenRe.Matches(text ?? "").Cast<Match>().Select(m => m.Value).ToList();

        internal static bool NumericLike(string text)
        {
            var stripped = (text ?? "").Trim();
            if (stripped.Length == 0)
                return false;
            int alpha = CountAlpha(stripped);
            int digit = CountDigit(stripped);
            if (digit == 0)
                return false;
            if (MonthRe.IsMatch(stripped))
                return false;
            return alpha / (float)Math.Max(1, alpha + digit) <= 0.30f;
        }

        internal static bool DateOrPeriodLike(string text)
        {
            var stripped = (text ?? "").Trim();
            if (stripped.Length == 0)
                return false;
            return MonthRe.IsMatch(stripped) || PeriodKeywordRe.IsMatch(stripped);
        }

        internal static string CellType(string text)
        {
            var stripped = string.Join(" ", (text ?? "").Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
            if (stripped.Length == 0)
                return "empty";

            int alpha = CountAlpha(stripped);
            int digit = CountDigit(stripped);
            int tokenCount = Tokens(stripped).Count;
            var lowered = stripped.ToLowerInvariant();

            if (MoneyRe.IsMatch(stripped))
                return "money";
            if (FullMatch(PercentRe, stripped) || (stripped.EndsWith("%") && digit > 0 && alpha == 0))
                return "percent";
            if (DateOrPeriodLike(stripped) || (FullMatch(YearRe, stripped) && tokenCount == 1))
                return "date_period";
            if (RangeRe.IsMatch(stripped) && digit > 0 && tokenCount <= 8)
                return "range";
            if (NumericLike(stripped))
                return "number";
            if (lowered == "%" || lowered == "$" || lowered == "$000" || lowered == "$m"
                || lowered == "usd" || lowered == "eur" || lowered == "gbp"
                || lowered == "amount" || lowered == "rate" || lowered == "ratio")
                return "unit";
            if (CodeRe.IsMatch(stripped) && tokenCount <= 4)
                return "code_id";
            if (alpha > 0 && digit > 0 && tokenCount <= 6)
                return "code_id";
            if (tokenCount >= 8)
                return "long_text";
            return "text_label";
        }

        internal static Dictionary<string, float> CellTypeRatios(IList<string> texts)
        {
            var nonemptyTypes = (texts ?? Array.Empty<string>())
                .Where(t => (t ?? "").Trim().Length > 0)
                .Select(CellType)
                .ToList();
            if (nonemptyTypes.Count == 0)
            {
                return new Dictionary<string, float>
                {
                    ["value_type_ratio"] = 0.0f,
                    ["label_type_ratio"] = 0.0f,
                    ["long_text_type_ratio"] = 0.0f,
                    ["code_type_ratio"] = 0.0f,
                    ["date_period_type_ratio"] = 0.0f,
                    ["range_type_ratio"] = 0.0f,
                };
            }
            float total = nonemptyTypes.Count;
            return new Dictionary<string, float>
            {
                ["value_type_ratio"] = nonemptyTypes.Count(item => ValueTypes.Contains(item)) / total,
                ["label_type_ratio"] = nonemptyTypes.Count(item => LabelTypes.Contains(item)) / total,
                ["long_text_type_ratio"] = nonemptyTypes.Count(item => item == "long_text") / total,
                ["code_type_ratio"] = nonemptyTypes.Count(item => item == "code_id") / total,
                ["date_period_type_ratio"] = nonemptyTypes.Count(item => item == "date_period") / total,
                ["range_type_ratio"] = nonemptyTypes.Count(item => item == "range") / total,
            };
        }

        internal static Dictionary<string, float> RowFeatures(IList<string> texts)
        {
            texts = texts ?? Array.Empty<string>();
            var nonempty = texts.Where(t => (t ?? "").Trim().Length > 0).ToList();
            if (texts.Count == 0)
            {
                var empty = new Dictionary<string, float>
                {
                    ["cell_count"] = 0.0f,
                    ["nonempty_count"] = 0.0f,
                    ["nonempty_ratio"] = 0.0f,
                    ["empty_ratio"] = 0.0f,
                    ["numeric_ratio"] = 0.0f,
                    ["alpha_ratio"] = 0.0f,
                    ["date_period_ratio"] = 0.0f,
                    ["avg_tokens"] = 0.0f,
                };
                foreach (var kv in CellTypeRatios(Array.Empty<string>()))
                    empty[kv.Key] = kv.Value;
                return empty;
            }
            var features = new Dictionary<string, float>
            {
                ["cell_count"] = (float)texts.Count,
                ["nonempty_count"] = (float)nonempty.Count,
                ["nonempty_ratio"] = nonempty.Count / (float)texts.Count,
                ["empty_ratio"] = (texts.Count - nonempty.Count) / (float)texts.Count,
                ["numeric_ratio"] = nonempty.Count(t => NumericLike(t)) / (float)Math.Max(1, nonempty.Count),
                ["alpha_ratio"] = nonempty.Count(t => CountAlpha(t) > 0) / (float)Math.Max(1, nonempty.Count),
                ["date_period_ratio"] = nonempty.Count(t => DateOrPeriodLike(t)) / (float)Math.Max(1, nonempty.Count),
                ["avg_tokens"] = nonempty.Count > 0
                    ? (float)nonempty.Select(t => Tokens(t).Count).Average()
                    : 0.0f,
            };
            foreach (var kv in CellTypeRatios(texts))
                features[kv.Key] = kv.Value;
            return features;
        }

        internal static string ColumnText(IList<string> row, int col)
            => col < row.Count ? (row[col] ?? "") : "";

        internal static Dictionary<string, float> RowVector(IList<string> texts)
        {
            var features = RowFeatures(texts);
            var first = ColumnText(texts, 0);
            var rest = texts.Skip(1).Where(t => (t ?? "").Trim().Length > 0).ToList();
            var restTypes = rest.Select(CellType).ToList();
            bool firstNonempty = first.Trim().Length > 0;
            var vector = new Dictionary<string, float>(features)
            {
                ["first_empty"] = firstNonempty ? 0.0f : 1.0f,
                ["first_alpha"] = CountAlpha(first) > 0 ? 1.0f : 0.0f,
                ["first_numeric"] = NumericLike(first) ? 1.0f : 0.0f,
                ["first_value_type"] = ValueTypes.Contains(CellType(first)) ? 1.0f : 0.0f,
                ["first_label_type"] = LabelTypes.Contains(CellType(first)) ? 1.0f : 0.0f,
                ["rest_numeric_ratio"] = rest.Count(t => NumericLike(t)) / (float)Math.Max(1, rest.Count),
                ["rest_alpha_ratio"] = rest.Count(t => CountAlpha(t) > 0) / (float)Math.Max(1, rest.Count),
                ["rest_value_type_ratio"] = restTypes.Count(item => ValueTypes.Contains(item)) / (float)Math.Max(1, restTypes.Count),
                ["rest_label_type_ratio"] = restTypes.Count(item => LabelTypes.Contains(item)) / (float)Math.Max(1, restTypes.Count),
                ["rest_long_text_type_ratio"] = restTypes.Count(item => item == "long_text") / (float)Math.Max(1, restTypes.Count),
                ["avg_tokens_norm"] = Math.Min(1.0f, features["avg_tokens"] / 8.0f),
            };
            return vector;
        }

        internal static bool HeaderCandidateRow(IList<string> row)
        {
            var features = RowFeatures(row);
            if (features["nonempty_count"] == 0)
                return false;
            bool shortOrSparse = features["avg_tokens"] <= 6.0f || features["empty_ratio"] >= 0.20f;
            return shortOrSparse;
        }

        internal static bool GroupedHeaderScaffold(IList<string> row)
        {
            var features = RowFeatures(row);
            if (features["cell_count"] == 0)
                return false;
            var texts = row.Select(t => (t ?? "").Trim()).ToList();
            var nonempty = texts.Where(t => t.Length > 0).ToList();
            var uniqueNonempty = new HashSet<string>(nonempty.Select(t => t.ToLowerInvariant()), StringComparer.Ordinal);
            bool repeatedLabels = nonempty.Count >= 3 && uniqueNonempty.Count <= Math.Max(1, nonempty.Count / 2);
            bool sparseLabels = features["empty_ratio"] >= 0.20f || features["nonempty_ratio"] <= 0.70f;
            return sparseLabels || repeatedLabels;
        }

        internal static bool DataRowLike(IList<string> row)
        {
            var vector = RowVector(row);
            return vector["first_alpha"] > 0
                && vector["first_numeric"] == 0
                && vector["rest_numeric_ratio"] >= 0.35f
                && vector["numeric_ratio"] >= 0.25f;
        }

        internal static bool DenseNumericRowLike(IList<string> row)
        {
            var vector = RowVector(row);
            return vector["numeric_ratio"] >= 0.70f && vector["alpha_ratio"] <= 0.35f;
        }

        internal static bool MoneyOrPercentHeavy(IList<string> row)
        {
            var types = row.Where(t => (t ?? "").Trim().Length > 0).Select(CellType).ToList();
            if (types.Count == 0)
                return false;
            return types.Count(item => item == "money" || item == "percent") / (float)types.Count >= 0.25f;
        }

        internal static float RestDatePeriodRatio(IList<string> row)
        {
            var rest = row.Skip(1).Where(c => (c ?? "").Trim().Length > 0).ToList();
            if (rest.Count == 0)
                return 0.0f;
            return rest.Count(c => CellType(c) == "date_period") / (float)rest.Count;
        }

        internal static bool FirstRowPeriodRestoreGuard(IList<string> row)
        {
            var features = RowFeatures(row);
            return features["date_period_type_ratio"] >= 0.25f || RestDatePeriodRatio(row) >= 0.35f;
        }

        internal static bool FirstRowDateOrUnitRestoreGuard(IList<string> row)
        {
            var rest = row.Skip(1).Where(c => (c ?? "").Trim().Length > 0).ToList();
            if (rest.Count == 0)
                return false;
            var restTypes = rest.Select(CellType).ToList();
            float dpUnitRatio = restTypes.Count(item => item == "date_period" || item == "unit") / (float)restTypes.Count;
            bool moneyOrPercent = restTypes.Any(item => item == "money" || item == "percent");
            var rowText = string.Join(" ", row).ToLowerInvariant();
            bool unitRow = UnitRowTerms.Any(term => rowText.Contains(term));
            return (dpUnitRatio >= 0.50f || unitRow) && !moneyOrPercent;
        }

        internal static List<string> FirstRowRestoreGuards(IList<string> row)
        {
            var guards = new List<string>();
            if (FirstRowPeriodRestoreGuard(row))
                guards.Add("first_row_period_restore");
            if (FirstRowDateOrUnitRestoreGuard(row))
                guards.Add("first_row_date_or_unit_restore");
            return guards;
        }

        internal static bool HeaderlessDottedEnumGuard(IList<List<string>> rows)
        {
            if (rows.Count < 3)
                return false;
            var row = rows[0];
            var cells = row.Where(cell => cell != null && cell.Trim().Length > 0).Select(c => c.Trim()).ToList();
            if (cells.Count == 0 || !DottedEnumRe.IsMatch(cells[0]))
                return false;
            var features = RowFeatures(row);
            return features["long_text_type_ratio"] >= 0.25f || features["avg_tokens"] >= 5.0f;
        }

        internal static bool SimpleIntegerEnumHeaderLike(IList<string> row)
        {
            var vector = RowVector(row);
            if (vector["first_label_type"] <= 0 || vector["first_value_type"] > 0)
                return false;
            var values = new List<int>();
            foreach (var text in row.Skip(1).Select(t => (t ?? "").Trim()).Where(t => t.Length > 0))
            {
                if (!SimpleIntRe.IsMatch(text))
                    return false;
                values.Add(int.Parse(text));
            }
            if (values.Count < 2)
                return false;
            bool ordered = values.SequenceEqual(values.OrderBy(v => v));
            bool unique = values.Distinct().Count() == values.Count;
            bool contiguous = values.Max() - values.Min() == values.Count - 1;
            return ordered && unique && contiguous;
        }

        /// <summary>
        /// A bracket/formula reference row under a grouped header, e.g. [3] = [1] * [2].
        /// </summary>
        internal static bool ReferenceFormulaHeaderRowLike(IList<string> candidate, IList<List<string>> followingRows)
        {
            var nonempty = candidate.Where(t => (t ?? "").Trim().Length > 0).Select(t => t.Trim()).ToList();
            if (nonempty.Count < 3)
                return false;
            if (nonempty.Any(text => text.Any(char.IsLetter)))
                return false;
            if (nonempty.Any(text => text.Contains("$") || text.Contains("%") || text.Contains(",")))
                return false;
            if (!nonempty.All(text => RefFormulaAllowedRe.IsMatch(text)))
                return false;
            var refCells = nonempty.Where(text => RefTokenRe.IsMatch(text)).ToList();
            var formulaCells = nonempty
                .Where(text => text.Contains("=") && RefTokenRe.Matches(text).Count >= 2)
                .ToList();
            if (refCells.Count / (float)nonempty.Count < 0.75f)
                return false;
            if (formulaCells.Count == 0 && refCells.Count < 4)
                return false;
            if (followingRows.Count < 2)
                return false;

            var followingProfiles = RowProfiles(followingRows);
            var bodyFeatures = BodyWindowFeaturesFromProfiles(followingProfiles, 0);
            if (BodyWindowIsStable(bodyFeatures))
                return true;
            if ((bool)followingProfiles[0]["section_label_row"] && followingProfiles.Count >= 3)
            {
                var shifted = BodyWindowFeaturesFromProfiles(followingProfiles, 1);
                return BodyWindowIsStable(shifted);
            }
            return false;
        }

        internal static bool DenseIntegerAxisRowLike(IList<string> row)
        {
            var values = new List<int>();
            int alphaCells = 0;
            var nonempty = row.Where(t => (t ?? "").Trim().Length > 0).Select(t => t.Trim()).ToList();
            foreach (var text in nonempty)
            {
                if (SimpleIntRe.IsMatch(text))
                    values.Add(int.Parse(text));
                else if (text.Any(char.IsLetter))
                    alphaCells += 1;
                else
                    return false;
            }
            if (values.Count < 5)
                return false;
            var unique = values.Distinct().OrderBy(v => v).ToList();
            if (!unique.SequenceEqual(Enumerable.Range(unique[0], unique[unique.Count - 1] - unique[0] + 1)))
                return false;
            if (unique[0] != 0 && unique[0] != 1)
                return false;
            if (alphaCells > Math.Max(4, nonempty.Count / 5))
                return false;
            return true;
        }

        internal static bool SparsePrefixExtensionPrefixLike(IList<string> row)
        {
            var features = RowFeatures(row);
            if (features["nonempty_count"] == 0)
                return false;
            if (features["nonempty_count"] <= 2)
                return features["alpha_ratio"] >= 0.50f || features["date_period_ratio"] >= 0.20f;
            if (features["empty_ratio"] < 0.25f)
                return false;
            return features["alpha_ratio"] >= 0.50f || features["date_period_ratio"] >= 0.20f;
        }

        internal static bool NextHeaderRowValueGuard(IList<string> candidate, IList<List<string>> followingRows)
        {
            var features = RowFeatures(candidate);
            var vector = RowVector(candidate);
            if (features["nonempty_count"] < 2)
                return false;
            if (features["avg_tokens"] > 7.0f || features["long_text_type_ratio"] >= 0.35f)
                return false;
            if (MoneyOrPercentHeavy(candidate))
                return false;
            if (ReferenceFormulaHeaderRowLike(candidate, followingRows))
                return true;
            if (features["value_type_ratio"] > 0.85f)
                return false;

            // date_period_type_ratio recognises a bare-year header row that the
            // keyword-based date_period_ratio misses; combine them only in this guard.
            float datePeriodSignal = Math.Max(features["date_period_ratio"], features["date_period_type_ratio"]);
            bool hasLabelSignal =
                features["alpha_ratio"] >= 0.30f
                || datePeriodSignal >= 0.20f
                || vector["first_label_type"] > 0;
            if (!hasLabelSignal)
                return false;

            if (followingRows.Count < 2)
                return false;
            var followingProfiles = RowProfiles(followingRows);
            var bodyFeatures = BodyWindowFeaturesFromProfiles(followingProfiles, 0);
            if (!BodyWindowIsStable(bodyFeatures))
                return false;

            if (features["numeric_ratio"] >= 0.50f || features["value_type_ratio"] >= 0.50f)
            {
                if (vector["first_label_type"] <= 0 && datePeriodSignal < 0.20f)
                    return false;
                if (vector["first_value_type"] > 0)
                    return false;
            }

            var types = candidate.Where(t => (t ?? "").Trim().Length > 0).Select(CellType).ToList();
            int typeCount = types.Count;
            if (typeCount == 0)
                return false;
            float dateTypeRatio = types.Count(item => item == "date_period") / (float)typeCount;
            float codeRangeNumberRatio = types.Count(item => item == "code_id" || item == "range" || item == "number") / (float)typeCount;

            if (features["value_type_ratio"] > 0.25f)
                return SimpleIntegerEnumHeaderLike(candidate);
            if (codeRangeNumberRatio >= 0.50f && dateTypeRatio < 0.30f)
                return SimpleIntegerEnumHeaderLike(candidate);
            return true;
        }

        internal static bool SparsePrefixConservativeExtensionGuard(IList<List<string>> rows, int topHeaderRows)
        {
            if (topHeaderRows != 1 || rows.Count < 3)
                return false;
            var prefix = rows[0];
            var candidate = rows[1];
            var prefixFeatures = RowFeatures(prefix);
            var candidateFeatures = RowFeatures(candidate);
            if (prefixFeatures["empty_ratio"] < 0.50f)
                return false;
            if (candidateFeatures["nonempty_count"] < 3 && candidateFeatures["date_period_ratio"] < 0.50f)
                return false;
            if (!SparsePrefixExtensionPrefixLike(prefix))
                return false;
            if (candidateFeatures["label_type_ratio"] < 0.50f)
                return false;
            if (candidateFeatures["value_type_ratio"] >= 0.50f)
                return false;
            if (SectionLabelRowLike(candidate))
                return false;
            var followingProfiles = RowProfiles(rows.Skip(2).ToList());
            var bodyFeatures = BodyWindowFeaturesFromProfiles(followingProfiles, 0);
            return BodyWindowIsStable(bodyFeatures);
        }

        /// <summary>
        /// Return true when the first body row should be included as a second header row.
        /// </summary>
        internal static bool SparsePrefixNextHeaderGuard(IList<List<string>> rows, int topHeaderRows)
        {
            if (topHeaderRows < 1 || topHeaderRows >= rows.Count - 1)
                return false;
            return SparsePrefixConservativeExtensionGuard(rows, topHeaderRows);
        }

        /// <summary>
        /// Promote a leaf-label row under a sparse/grouped rowspan header scaffold.
        ///
        /// An empty first cell in the candidate row is the textual shadow of the row-0
        /// rowspan label; treat it as header only when later rows form a stable body.
        /// </summary>
        internal static bool RowspanLeafHeaderGuard(IList<List<string>> rows, int topHeaderRows)
        {
            if (topHeaderRows != 1 || rows.Count < 3)
                return false;
            int maxCols = rows.Count > 0 ? rows.Max(r => r.Count) : 0;
            if (maxCols < 3)
                return false;

            var prefix = rows[0];
            var candidate = rows[1];
            var prefixFeatures = RowFeatures(prefix);
            var candidateFeatures = RowFeatures(candidate);
            var candidateVector = RowVector(candidate);
            bool shiftedByRowspan = candidate.Count < maxCols;
            if (ColumnText(candidate, 0).Trim().Length > 0 && !shiftedByRowspan)
                return false;
            bool sparseRowspanPrefix = prefix.Count < maxCols && prefix.Count <= Math.Max(2, (int)(maxCols * 0.50));
            if (!(GroupedHeaderScaffold(prefix) || prefixFeatures["empty_ratio"] >= 0.35f || sparseRowspanPrefix))
                return false;
            if (candidateFeatures["nonempty_count"] < Math.Max(2, (int)(maxCols * 0.45)))
                return false;
            if (candidateFeatures["value_type_ratio"] > 0.25f || candidateFeatures["numeric_ratio"] > 0.35f)
                return false;
            if (candidateVector["rest_label_type_ratio"] < 0.50f && candidateFeatures["alpha_ratio"] < 0.50f)
                return false;
            if (SectionLabelRowLike(candidate) || DenseNumericRowLike(candidate))
                return false;

            var followingProfiles = RowProfiles(rows.Skip(2).ToList());
            var bodyFeatures = BodyWindowFeaturesFromProfiles(followingProfiles, 0);
            return BodyWindowIsStable(bodyFeatures);
        }

        internal static bool LeadingBodyRecordLike(IList<string> row)
        {
            var vector = RowVector(row);
            if (vector["nonempty_count"] < 2)
                return false;
            bool firstIsLabel = vector["first_label_type"] > 0 && vector["first_value_type"] == 0;
            bool restIsValues = vector["rest_numeric_ratio"] >= 0.50f || vector["rest_value_type_ratio"] >= 0.50f;
            bool valueHeavy = vector["numeric_ratio"] >= 0.35f || vector["value_type_ratio"] >= 0.35f;
            return firstIsLabel && restIsValues && valueHeavy && !GroupedHeaderScaffold(row);
        }

        internal static bool LeadingLongFormRowLike(IList<string> row)
        {
            var vector = RowVector(row);
            return vector["nonempty_count"] >= 4
                && vector["long_text_type_ratio"] >= 0.30f
                && vector["value_type_ratio"] <= 0.20f
                && vector["empty_ratio"] <= 0.35f;
        }

        internal static bool SectionLabelRowLike(IList<string> row)
        {
            if (row.Count < 2)
                return false;
            var features = RowFeatures(row);
            var first = ColumnText(row, 0).Trim();
            var nonempty = row.Where(t => (t ?? "").Trim().Length > 0).ToList();
            bool firstOnly = first.Length > 0 && !row.Skip(1).Any(t => (t ?? "").Trim().Length > 0);
            bool sparseLabel =
                features["nonempty_count"] <= 3
                && features["empty_ratio"] >= 0.60f
                && features["alpha_ratio"] >= 0.50f;
            return firstOnly || (nonempty.Count > 0 && sparseLabel);
        }

        internal static bool CenteredSectionLabelRowLike(IList<string> row)
        {
            if (!SectionLabelRowLike(row))
                return false;
            var nonemptyIndices = new List<int>();
            for (int idx = 0; idx < row.Count; idx++)
            {
                if ((row[idx] ?? "").Trim().Length > 0)
                    nonemptyIndices.Add(idx);
            }
            return nonemptyIndices.Count == 1 && nonemptyIndices[0] > 0 && row.Count >= 3;
        }

        /// <summary>
        /// Return sparse section-label rows inside the detected top header prefix.
        /// </summary>
        internal static List<int> SectionHeaderRows(IList<List<string>> rows, int topHeaderRows)
        {
            var sectionRows = new List<int>();
            int limit = Math.Min(topHeaderRows, rows.Count);
            for (int rowIdx = 0; rowIdx < limit; rowIdx++)
            {
                var row = rows[rowIdx];
                if (rowIdx == 0 || !CenteredSectionLabelRowLike(row))
                    continue;
                var nonempty = row.Where(t => (t ?? "").Trim().Length > 0).Select(t => t.Trim()).ToList();
                if (nonempty.Count != 1)
                    continue;
                if (row.Count < 2)
                    continue;
                sectionRows.Add(rowIdx);
            }
            return sectionRows;
        }

        internal static Dictionary<string, object> RowProfile(IList<string> row, int index)
        {
            var vector = RowVector(row);
            bool coordinateHeaderRow = DenseIntegerAxisRowLike(row);
            bool bodyValueRow =
                vector["first_alpha"] > 0
                && vector["first_numeric"] == 0
                && vector["rest_numeric_ratio"] >= 0.35f
                && vector["numeric_ratio"] >= 0.25f;
            bool bodyDenseRow = vector["numeric_ratio"] >= 0.70f && vector["alpha_ratio"] <= 0.35f;
            bool bodyLongFormRow =
                vector["first_label_type"] > 0
                && vector["first_value_type"] == 0
                && vector["rest_long_text_type_ratio"] >= 0.50f
                && vector["value_type_ratio"] <= 0.25f
                && vector["empty_ratio"] <= 0.35f;
            if (coordinateHeaderRow)
            {
                bodyValueRow = false;
                bodyDenseRow = false;
                bodyLongFormRow = false;
            }
            return new Dictionary<string, object>
            {
                ["index"] = index,
                ["vector"] = vector,
                ["nonempty"] = vector["nonempty_count"] > 0,
                ["body_value_row"] = bodyValueRow,
                ["body_dense_row"] = bodyDenseRow,
                ["body_long_form_row"] = bodyLongFormRow,
                ["body_like_row"] = bodyValueRow || bodyDenseRow || bodyLongFormRow,
                ["coordinate_header_row"] = coordinateHeaderRow,
                ["leading_body_record_row"] = LeadingBodyRecordLike(row),
                ["leading_long_form_row"] = LeadingLongFormRowLike(row),
                ["section_label_row"] = SectionLabelRowLike(row),
                ["grouped_header_scaffold"] = GroupedHeaderScaffold(row),
                ["header_candidate"] = HeaderCandidateRow(row),
            };
        }

        internal static List<Dictionary<string, object>> RowProfiles(IList<List<string>> rows)
            => rows.Select((row, index) => RowProfile(row, index)).ToList();

        internal static Dictionary<string, float> CombineBodyVectors(IList<Dictionary<string, float>> vectors)
        {
            if (vectors == null || vectors.Count == 0)
            {
                return new Dictionary<string, float>
                {
                    ["count"] = 0.0f,
                    ["first_label_ratio"] = 0.0f,
                    ["rest_numeric_ratio"] = 0.0f,
                    ["numeric_ratio"] = 0.0f,
                    ["alpha_ratio"] = 0.0f,
                    ["avg_tokens_norm"] = 0.0f,
                    ["body_row_ratio"] = 0.0f,
                    ["dense_numeric_row_ratio"] = 0.0f,
                    ["long_form_row_ratio"] = 0.0f,
                    ["value_type_ratio"] = 0.0f,
                    ["rest_value_type_ratio"] = 0.0f,
                    ["rest_long_text_type_ratio"] = 0.0f,
                    ["long_text_type_ratio"] = 0.0f,
                };
            }
            float n = vectors.Count;
            return new Dictionary<string, float>
            {
                ["count"] = n,
                ["first_label_ratio"] = vectors.Count(item => item["first_label_type"] > 0 && item["first_value_type"] == 0) / n,
                ["rest_numeric_ratio"] = vectors.Sum(item => item["rest_numeric_ratio"]) / n,
                ["numeric_ratio"] = vectors.Sum(item => item["numeric_ratio"]) / n,
                ["alpha_ratio"] = vectors.Sum(item => item["alpha_ratio"]) / n,
                ["avg_tokens_norm"] = vectors.Sum(item => item["avg_tokens_norm"]) / n,
                ["value_type_ratio"] = vectors.Sum(item => item["value_type_ratio"]) / n,
                ["rest_value_type_ratio"] = vectors.Sum(item => item["rest_value_type_ratio"]) / n,
                ["rest_long_text_type_ratio"] = vectors.Sum(item => item["rest_long_text_type_ratio"]) / n,
                ["long_text_type_ratio"] = vectors.Sum(item => item["long_text_type_ratio"]) / n,
                ["body_row_ratio"] = vectors.Count(item =>
                    (item["first_alpha"] > 0
                        && item["first_numeric"] == 0
                        && item["rest_numeric_ratio"] >= 0.35f
                        && item["numeric_ratio"] >= 0.25f)
                    || (item["numeric_ratio"] >= 0.70f && item["alpha_ratio"] <= 0.35f)) / n,
                ["dense_numeric_row_ratio"] = vectors.Count(item =>
                    item["numeric_ratio"] >= 0.70f && item["alpha_ratio"] <= 0.35f) / n,
                ["long_form_row_ratio"] = vectors.Count(item =>
                    item["first_label_type"] > 0
                    && item["first_value_type"] == 0
                    && item["rest_long_text_type_ratio"] >= 0.50f
                    && item["value_type_ratio"] <= 0.25f
                    && item["empty_ratio"] <= 0.35f) / n,
            };
        }

        internal static Dictionary<string, float> BodyWindowFeaturesFromProfiles(
            IList<Dictionary<string, object>> profiles,
            int start,
            int size = 4)
        {
            var vectors = new List<Dictionary<string, float>>();
            int end = Math.Min(profiles.Count, start + size);
            for (int i = start; i < end; i++)
            {
                if ((bool)profiles[i]["nonempty"])
                    vectors.Add(AsVector(profiles[i]["vector"]));
            }
            return CombineBodyVectors(vectors);
        }

        internal static bool BodyWindowIsStable(Dictionary<string, float> features)
        {
            if (features["count"] < 2)
                return false;
            bool valueBody =
                features["body_row_ratio"] >= 0.50f
                && features["rest_numeric_ratio"] >= 0.45f
                && features["first_label_ratio"] >= 0.35f;
            bool denseNumericBody =
                features["dense_numeric_row_ratio"] >= 0.50f
                && features["numeric_ratio"] >= 0.60f
                && features["alpha_ratio"] <= 0.55f;
            bool longFormBody =
                features["long_form_row_ratio"] >= 0.50f
                && features["first_label_ratio"] >= 0.50f
                && features["rest_long_text_type_ratio"] >= 0.50f
                && features["value_type_ratio"] <= 0.25f
                && features["numeric_ratio"] <= 0.25f;
            return valueBody || denseNumericBody || longFormBody;
        }

        internal static float BoundaryDistance(IList<string> headerRow, Dictionary<string, float> bodyFeatures)
        {
            var header = RowVector(headerRow);
            float distance = 0.0f;
            distance += Math.Abs(header["numeric_ratio"] - bodyFeatures["numeric_ratio"]);
            distance += Math.Abs(header["alpha_ratio"] - bodyFeatures["alpha_ratio"]);
            distance += Math.Abs(header["rest_numeric_ratio"] - bodyFeatures["rest_numeric_ratio"]);
            distance += Math.Abs(header["first_alpha"] - bodyFeatures["first_label_ratio"]);
            distance += 0.5f * Math.Abs(header["avg_tokens_norm"] - bodyFeatures["avg_tokens_norm"]);
            if (header["empty_ratio"] >= 0.20f)
                distance += 0.25f;
            return distance;
        }

        internal static bool HeaderPrefixOk(
            IList<List<string>> rows,
            IList<Dictionary<string, object>> profiles,
            int bodyStart,
            Dictionary<string, float> bodyFeatures)
        {
            var reasons = new List<string>();
            if (bodyStart <= 1)
                return true;
            if (bodyStart > 5)
                return false;

            var candidateProfiles = profiles.Skip(1).Take(bodyStart - 1).ToList();
            if (candidateProfiles.Count == 0)
                return true;

            bool allowedSectionPrefix = candidateProfiles.All(
                profile => CenteredSectionLabelRowLike(rows[(int)profile["index"]]));
            bool coordinatePrefix = candidateProfiles.Any(
                profile => profile.TryGetValue("coordinate_header_row", out var ch) && ch is bool b && b);
            var disallowedSectionProfiles = new List<Dictionary<string, object>>();
            foreach (var profile in candidateProfiles)
            {
                if (!(bool)profile["section_label_row"])
                    continue;
                int idx = (int)profile["index"];
                bool sparseCoordinateTail =
                    coordinatePrefix
                    && idx == bodyStart - 1
                    && (bool)profile["header_candidate"]
                    && !(bool)profile["body_like_row"]
                    && AsVector(profile["vector"])["nonempty_count"] <= 3
                    && idx > 1
                    && (bool)profiles[idx - 1]["header_candidate"];
                if (!CenteredSectionLabelRowLike(rows[idx]) && !sparseCoordinateTail)
                    disallowedSectionProfiles.Add(profile);
            }
            if (candidateProfiles.Any(profile => (bool)profile["section_label_row"]) && !allowedSectionPrefix)
            {
                if (disallowedSectionProfiles.Count > 0)
                    reasons.Add("section_label_before_body");
            }
            else if (disallowedSectionProfiles.Count > 0)
            {
                reasons.Add("section_label_before_body");
            }
            if (candidateProfiles.Any(profile => (bool)profile["body_like_row"]))
                reasons.Add("body_like_row_before_body");
            if (!candidateProfiles.All(profile => (bool)profile["header_candidate"]))
                reasons.Add("non_header_candidate_before_body");

            var previous = profiles[bodyStart - 2];
            var lastCandidate = profiles[bodyStart - 1];
            float contrast = BoundaryDistance(rows[bodyStart - 1], bodyFeatures);
            if (bodyStart == 2
                && AsVector(lastCandidate["vector"])["first_empty"] == 0
                && AsVector(lastCandidate["vector"])["numeric_ratio"] > 0.0f
                && !(bool)previous["grouped_header_scaffold"])
            {
                reasons.Add("numeric_first_row_without_group_scaffold");
            }
            if ((bool)lastCandidate["body_dense_row"] && !(bool)previous["grouped_header_scaffold"])
                reasons.Add("dense_candidate_without_group_scaffold");
            if (AsVector(lastCandidate["vector"])["alpha_ratio"] < 0.20f && !(bool)previous["grouped_header_scaffold"])
                reasons.Add("low_alpha_candidate_without_group_scaffold");
            if (contrast < 1.05f)
                reasons.Add("weak_header_body_contrast");

            return reasons.Count == 0;
        }

        internal static List<Dictionary<string, object>> BodyStartCandidates(
            IList<Dictionary<string, object>> profiles)
        {
            var candidates = new List<Dictionary<string, object>>();
            int maxStart = Math.Min(5, profiles.Count - 2);
            for (int start = 1; start <= maxStart; start++)
            {
                var bodyFeatures = BodyWindowFeaturesFromProfiles(profiles, start);
                bool startRowBodyLike = (bool)profiles[start]["body_like_row"];
                bool stable = startRowBodyLike && BodyWindowIsStable(bodyFeatures);
                bool priorBodySeen = profiles.Skip(1).Take(start - 1).Any(profile =>
                    (bool)profile["body_like_row"]
                    && !(profile.TryGetValue("coordinate_header_row", out var ch) && ch is bool b && b));
                candidates.Add(new Dictionary<string, object>
                {
                    ["body_start"] = start,
                    ["body_like"] = stable,
                    ["start_row_body_like"] = startRowBodyLike,
                    ["prior_body_seen"] = priorBodySeen,
                    ["body"] = bodyFeatures,
                });
            }
            return candidates;
        }

        static HashSet<int> NonemptyColIndices(IList<string> row, int maxCols)
        {
            var set = new HashSet<int>();
            int limit = Math.Min(maxCols, row.Count);
            for (int idx = 0; idx < limit; idx++)
            {
                if (ColumnText(row, idx).Trim().Length > 0)
                    set.Add(idx);
            }
            return set;
        }

        static bool HeaderColumnIncomplete(IList<List<string>> rows, int topHeaderRows)
        {
            if (topHeaderRows <= 0)
                return false;
            int maxCols = rows.Count > 0 ? rows.Max(r => r.Count) : 0;
            if (maxCols < 3)
                return false;
            var filled = new HashSet<int>();
            for (int i = 0; i < topHeaderRows && i < rows.Count; i++)
                filled.UnionWith(NonemptyColIndices(rows[i], maxCols));
            return filled.Count < maxCols && filled.Count <= Math.Max(1, (int)(maxCols * 0.75));
        }

        static bool BareYearValueRowLike(IList<string> row)
        {
            var nonempty = row.Where(t => (t ?? "").Trim().Length > 0).Select(t => t.Trim()).ToList();
            if (nonempty.Count < 2)
                return false;
            return nonempty.All(text => FullMatch(YearRe, text));
        }

        static bool UndertagExtensionCandidate(IList<string> row, IList<List<string>> followingRows, int maxCols)
        {
            var features = RowFeatures(row);
            var profile = RowProfile(row, 0);
            var filledCols = NonemptyColIndices(row, maxCols).OrderBy(x => x).ToList();
            var nonemptyAfterStub = new List<string>();
            int endCol = Math.Min(maxCols, row.Count);
            for (int col = 1; col < endCol; col++)
            {
                var text = ColumnText(row, col);
                if (text.Trim().Length > 0)
                    nonemptyAfterStub.Add(text);
            }
            var reasons = new List<string>();

            if (features["nonempty_count"] < 2 || filledCols.Count < 2)
                reasons.Add("single_or_sparse_row");
            if (nonemptyAfterStub.Count < 1)
                reasons.Add("no_column_header_cells_after_stub");
            if ((bool)profile["body_like_row"] || DataRowLike(row) || DenseNumericRowLike(row))
                reasons.Add("body_like_row");
            if (MoneyOrPercentHeavy(row) || features["value_type_ratio"] > 0.35f || features["numeric_ratio"] > 0.50f)
                reasons.Add("strong_value_row");
            if (BareYearValueRowLike(row))
                reasons.Add("bare_year_value_row");
            if (SectionLabelRowLike(row))
                reasons.Add("section_label_row");
            if (features["avg_tokens"] > 8.0f || features["long_text_type_ratio"] >= 0.35f)
                reasons.Add("long_text_row");
            if (followingRows.Count >= 2)
            {
                var followingProfiles = RowProfiles(followingRows);
                var bodyFeatures = BodyWindowFeaturesFromProfiles(followingProfiles, 0);
                if (!BodyWindowIsStable(bodyFeatures))
                    reasons.Add("following_body_not_stable");
            }

            bool labelSignal = features["alpha_ratio"] >= 0.25f || features["date_period_ratio"] >= 0.20f;
            if (!labelSignal)
                reasons.Add("weak_label_signal");

            return reasons.Count == 0;
        }

        /// <summary>
        /// Promote under-tagged column-header rows immediately below the base boundary.
        /// </summary>
        internal static int ExtendHeaderUndertag(IList<List<string>> rows, int topHeaderRows, int cap = 3)
        {
            int maxCols = rows.Count > 0 ? rows.Max(r => r.Count) : 0;
            if (topHeaderRows <= 0)
                return topHeaderRows;
            if (topHeaderRows >= rows.Count)
                return topHeaderRows;
            if (!HeaderColumnIncomplete(rows, topHeaderRows))
                return topHeaderRows;

            int newTop = topHeaderRows;
            int end = Math.Min(rows.Count, topHeaderRows + cap);
            for (int rowIdx = topHeaderRows; rowIdx < end; rowIdx++)
            {
                if (rowIdx >= rows.Count - 1)
                    break;
                var following = rows.Skip(rowIdx + 1).ToList();
                bool ok = UndertagExtensionCandidate(rows[rowIdx], following, maxCols);
                if (!ok)
                    break;
                newTop = rowIdx + 1;
            }
            return newTop;
        }

        internal static int FindTopHeaderRowsByBodyChange(IList<List<string>> rows)
        {
            if (rows == null || rows.Count == 0)
                return 0;
            if (rows.Count == 1)
                return 1;

            var profiles = RowProfiles(rows);
            var candidates = BodyStartCandidates(profiles);
            foreach (var candidate in candidates)
            {
                int bodyStart = (int)candidate["body_start"];
                if (!(bool)candidate["body_like"] || (bool)candidate["prior_body_seen"])
                    continue;

                if (bodyStart == 1)
                {
                    if ((bool)profiles[0]["leading_body_record_row"])
                    {
                        var restoreGuards = FirstRowRestoreGuards(rows[0]);
                        if (restoreGuards.Count > 0)
                            return 1;
                        return 0;
                    }
                    if (HeaderlessDottedEnumGuard(rows))
                        return 0;
                    if (SparsePrefixNextHeaderGuard(rows, 1))
                        return 2;
                    if (RowspanLeafHeaderGuard(rows, 1))
                        return 2;
                    // A dense period/label header (row 0) over a value-like sub-row (bare
                    // years, enumerated headers) is not caught by the sparse-prefix guards.
                    if (rows.Count >= 3 && NextHeaderRowValueGuard(rows[1], rows.Skip(2).ToList()))
                        return 2;
                    return 1;
                }

                if (HeaderPrefixOk(rows, profiles, bodyStart, AsVector(candidate["body"])))
                    return bodyStart;
            }

            if (HeaderlessDottedEnumGuard(rows))
                return 0;

            if (SparsePrefixNextHeaderGuard(rows, 1))
                return 2;

            if (RowspanLeafHeaderGuard(rows, 1))
                return 2;

            return 1;
        }
    }

    /// <summary>
    /// Header region detected by refine header rules
    /// (PyMuPDF <c>HeaderRegion</c>: top_header_rows + section_header_rows).
    /// </summary>
    public sealed class HeaderRegion
    {
        public int TopHeaderRows { get; }
        public IReadOnlyList<int> SectionHeaderRows { get; }

        public HeaderRegion(int topHeaderRows, IReadOnlyList<int> sectionHeaderRows)
        {
            TopHeaderRows = topHeaderRows;
            SectionHeaderRows = sectionHeaderRows ?? Array.Empty<int>();
        }

        public int top_header_rows => TopHeaderRows;
        public IReadOnlyList<int> section_header_rows => SectionHeaderRows;
    }

    internal static partial class TableHeaders
    {
        /// <summary>
        /// Detect top header rows and in-prefix section-header rows
        /// (PyMuPDF <c>find_header_region</c>).
        /// </summary>
        internal static HeaderRegion FindHeaderRegion(IList<List<string>> rows)
        {
            rows = rows ?? Array.Empty<List<string>>();
            int topHeaderRows = FindTopHeaderRowsByBodyChange(rows);
            // Promote under-tagged column-header rows immediately below the detected
            // header/body boundary.
            topHeaderRows = ExtendHeaderUndertag(rows, topHeaderRows);
            var sectionRows = SectionHeaderRows(rows, topHeaderRows);
            return new HeaderRegion(topHeaderRows, sectionRows);
        }

        // ---------------------------------------------------------------------------
        // HTML serialization: tagged placement grid -> <table>
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Whitespace-collapse a cell's text (runs of whitespace/newlines -> one space).
        /// </summary>
        internal static string CollapseCellWs(string text)
            => string.Join(" ", (text ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

        /// <summary>
        /// Escape a cell's text for the serializer: only <c>&amp; &lt; &gt;</c> (quotes left literal).
        /// </summary>
        static string EscapeHtmlText(string text)
            => (text ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        /// <summary>
        /// A cell's inner HTML: escaped non-empty lines joined by <c>&lt;br/&gt;</c>.
        /// </summary>
        static string CellInner(string text)
        {
            var parts = (text ?? "").Split('\n')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .Select(EscapeHtmlText);
            return string.Join("<br/>", parts);
        }

        /// <summary>
        /// Serialize a tagged placement grid to its final <c>&lt;table&gt;</c> HTML in one pass.
        ///
        /// <paramref name="rows"/> is a row-major grid of cells duck-typed with
        /// <c>text</c> / <c>colspan</c> / <c>rowspan</c> / <c>tag</c> (e.g.
        /// <see cref="SpanCell"/>): each cell emits its tag, its colspan/rowspan
        /// attributes and its <c>&lt;br/&gt;</c>-joined escaped inner HTML. A row
        /// whose index is in <paramref name="sectionHeaderRows"/> and that carries
        /// a single non-empty label collapses to one <c>&lt;th colspan=N&gt;</c>
        /// spanning the row.
        /// </summary>
        internal static string RenderTableHtml(
            IList<List<SpanCell>> rows,
            IEnumerable<int> sectionHeaderRows = null)
        {
            var sectionRows = new HashSet<int>(sectionHeaderRows ?? Array.Empty<int>());
            var parts = new List<string> { "<table>" };
            for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
            {
                var cells = rows[rowIdx];
                if (sectionRows.Contains(rowIdx))
                {
                    var nonempty = cells
                        .Select(c => CollapseCellWs(c.Text))
                        .Where(t => t.Length > 0)
                        .ToList();
                    if (nonempty.Count == 1 && cells.Count >= 2)
                    {
                        parts.Add(
                            $"<tr><th colspan=\"{cells.Count}\">{EscapeHtmlText(nonempty[0])}</th></tr>");
                        continue;
                    }
                }
                parts.Add("<tr>");
                foreach (var cell in cells)
                {
                    var attrs = "";
                    if (cell.Colspan > 1)
                        attrs += $" colspan=\"{cell.Colspan}\"";
                    if (cell.Rowspan > 1)
                        attrs += $" rowspan=\"{cell.Rowspan}\"";
                    parts.Add($"<{cell.Tag}{attrs}>{CellInner(cell.Text)}</{cell.Tag}>");
                }
                parts.Add("</tr>");
            }
            parts.Add("</table>");
            return string.Concat(parts);
        }
    }
}
