using System;
using MuPDFLLMUtils = MuPDF.NET.LLM.Helpers.Utils;

namespace MuPDF.NET.LLM.Test
{
    [TestFixture]
    public class UtilsTest
    {
        [Test]
        public void WhiteChars_ContainsExpectedCharacters()
        {
            Assert.That(MuPDFLLMUtils.WHITE_CHARS.Contains(' '), Is.True);
            Assert.That(MuPDFLLMUtils.WHITE_CHARS.Contains('\t'), Is.True);
            Assert.That(MuPDFLLMUtils.WHITE_CHARS.Contains('\n'), Is.True);
            Assert.That(MuPDFLLMUtils.WHITE_CHARS.Contains('\u00a0'), Is.True); // Non-breaking space
            Assert.That(MuPDFLLMUtils.WHITE_CHARS.Contains('a'), Is.False);
        }

        [Test]
        public void IsWhite_WithWhiteString_ReturnsTrue()
        {
            Assert.That(MuPDFLLMUtils.IsWhite("   "), Is.True);
            Assert.That(MuPDFLLMUtils.IsWhite("\t\n"), Is.True);
            Assert.That(MuPDFLLMUtils.IsWhite("\u00a0"), Is.True); // Non-breaking space
            Assert.That(MuPDFLLMUtils.IsWhite(""), Is.True);
        }

        [Test]
        public void IsWhite_WithNonWhiteString_ReturnsFalse()
        {
            Assert.That(MuPDFLLMUtils.IsWhite("hello"), Is.False);
            Assert.That(MuPDFLLMUtils.IsWhite("  hello  "), Is.False);
            Assert.That(MuPDFLLMUtils.IsWhite("a"), Is.False);
        }

        [Test]
        public void Bullets_ContainsExpectedCharacters()
        {
            Assert.That(MuPDFLLMUtils.BULLETS.Contains('*'), Is.True);
            Assert.That(MuPDFLLMUtils.BULLETS.Contains('-'), Is.True);
            Assert.That(MuPDFLLMUtils.BULLETS.Contains('>'), Is.True);
            Assert.That(MuPDFLLMUtils.BULLETS.Contains('o'), Is.True);
        }

        [Test]
        public void ReplacementCharacter_IsCorrect()
        {
            Assert.That(MuPDFLLMUtils.REPLACEMENT_CHARACTER, Is.EqualTo('\uFFFD'));
        }

        [Test]
        public void Type3FontName_IsCorrect()
        {
            Assert.That(MuPDFLLMUtils.TYPE3_FONT_NAME, Is.EqualTo("Unnamed-T3"));
        }
    }
}
