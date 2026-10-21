namespace MuPDF.NET.PDF4LLM.AI.Services;

/// <summary>
/// Lexical overlap for names, identifiers, versions, and dates, combined with
/// a vector score for hybrid retrieval.
/// </summary>
internal static class LexicalScore
{
    static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "of", "to", "and", "or", "in", "on", "for", "with",
        "which", "what", "who", "where", "when", "why", "how",
        "is", "are", "was", "were", "be", "been",
        "have", "has", "had", "do", "does", "did",
        "than", "more", "their", "from", "that", "this", "into", "by", "not",
        "about", "over", "under",
    };

    public static double Score(string query, string text)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(text))
            return 0;

        List<string> tokens = Tokens(query);
        if (tokens.Count == 0)
            return 0;

        string haystack = text.ToLowerInvariant();
        int hits = 0;
        foreach (string token in tokens)
        {
            if (haystack.Contains(token, StringComparison.Ordinal) || haystack.Contains(Stem(token), StringComparison.Ordinal))
                hits++;
        }

        double score = hits / (double)tokens.Count;
        string needle = query.Trim().ToLowerInvariant();
        if (needle.Length > 0 && haystack.Contains(needle, StringComparison.Ordinal))
            score = 1;

        return score;
    }

    /// <summary>
    /// Hybrid score in roughly 0..1.5. Lexical overlap (names, versions, dates)
    /// is weighted above vector similarity so exact identifiers still rank first.
    /// </summary>
    public static double Combine(double lexical, double vector)
    {
        double v = Math.Clamp(vector, 0, 1);
        double l = Math.Clamp(lexical, 0, 1);
        return l + (0.5 * v);
    }

    static List<string> Tokens(string query)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (char c in query.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || (current.Length > 0 && c is '.' or '-'))
            {
                current.Append(c);
                continue;
            }

            AddToken(tokens, current);
        }

        AddToken(tokens, current);
        return tokens;
    }

    static void AddToken(List<string> tokens, System.Text.StringBuilder current)
    {
        if (current.Length == 0)
            return;
        string token = current.ToString().Trim('.', '-');
        current.Clear();
        if (token.Length == 0)
            return;
        if ((token.Length < 3 && !IsIdentifierToken(token)) || Stopwords.Contains(token))
            return;
        tokens.Add(token);
    }

    static bool IsIdentifierToken(string token)
    {
        bool digit = false;
        foreach (char c in token)
        {
            if (char.IsDigit(c))
                digit = true;
            else if (c is not '.' and not '-')
                return token.Length >= 2;
        }

        return digit;
    }

    static string Stem(string token)
    {
        if (token.EndsWith("ies", StringComparison.Ordinal) && token.Length > 4)
            return token.Substring(0, token.Length - 3) + "y";
        if (token.EndsWith("s", StringComparison.Ordinal) && !token.EndsWith("ss", StringComparison.Ordinal) && token.Length > 4)
            return token.Substring(0, token.Length - 1);
        return token;
    }
}
