using System;

namespace MuPDF.NET
{
    /// <summary>tiktoken is skipped in C#; null tokenizer uses char/4 truncate estimate.</summary>
    internal sealed class TokenCounter
    {
        const double CharsPerTokenApprox = 4.0;
        readonly Func<string, int> _countFn;

        public TokenCounter(Func<string, int> tokenizer = null)
        {
            _countFn = tokenizer ?? CharEstimate;
        }

        static int CharEstimate(string text) =>
            Math.Max(1, (int)(text.Length / CharsPerTokenApprox));

        public int Count(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            return _countFn(text);
        }
    }
}
