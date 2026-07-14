using System;
using Xunit;

namespace PDF4LLM.Test
{
    /// <summary>(Python-source lint; optional in CI).</summary>
    [Collection("PDF4LLM")]
    public class TestLint
    {
        [Fact]
        public void test_pylint()
        {
            //     return
            if (!string.Equals(
                    Environment.GetEnvironmentVariable("PYMUPDF4LLM_TEST_PYLINT"),
                    "1",
                    StringComparison.Ordinal))
                return;

            // pylint targets layout package Python sources; run from layout package-1.28.0 when needed.
        }
    }
}