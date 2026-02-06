using System;
using MuPDF4LLMUtils = MuPDF.NET4LLM.Helpers.Utils;

namespace MuPDF.NET4LLM.Test
{
    [TestFixture]
    public class UtilsTest
    {
        [Test]
        public void WhiteChars_ContainsExpectedCharacters()
        {
            Assert.That(MuPDF4LLMUtils.WHITE_CHARS.Contains(' '), Is.True);
            Assert.That(MuPDF4LLMUtils.WHITE_CHARS.Contains('\t'), Is.True);
            Assert.That(MuPDF4LLMUtils.WHITE_CHARS.Contains('\n'), Is.True);
            Assert.That(MuPDF4LLMUtils.WHITE_CHARS.Contains('\u00a0'), Is.True); // Non-breaking space
            Assert.That(MuPDF4LLMUtils.WHITE_CHARS.Contains('a'), Is.False);
        }

        [Test]
        public void IsWhite_WithWhiteString_ReturnsTrue()
        {
            Assert.That(MuPDF4LLMUtils.IsWhite("   "), Is.True);
            Assert.That(MuPDF4LLMUtils.IsWhite("\t\n"), Is.True);
            Assert.That(MuPDF4LLMUtils.IsWhite("\u00a0"), Is.True); // Non-breaking space
            Assert.That(MuPDF4LLMUtils.IsWhite(""), Is.True);
        }

        [Test]
        public void IsWhite_WithNonWhiteString_ReturnsFalse()
        {
            Assert.That(MuPDF4LLMUtils.IsWhite("hello"), Is.False);
            Assert.That(MuPDF4LLMUtils.IsWhite("  hello  "), Is.False);
            Assert.That(MuPDF4LLMUtils.IsWhite("a"), Is.False);
        }

        [Test]
        public void Bullets_ContainsExpectedCharacters()
        {
            Assert.That(MuPDF4LLMUtils.BULLETS.Contains('*'), Is.True);
            Assert.That(MuPDF4LLMUtils.BULLETS.Contains('-'), Is.True);
            Assert.That(MuPDF4LLMUtils.BULLETS.Contains('>'), Is.True);
            Assert.That(MuPDF4LLMUtils.BULLETS.Contains('o'), Is.True);
        }

        [Test]
        public void ReplacementCharacter_IsCorrect()
        {
            Assert.That(MuPDF4LLMUtils.REPLACEMENT_CHARACTER, Is.EqualTo('\uFFFD'));
        }

        [Test]
        public void Type3FontName_IsCorrect()
        {
            Assert.That(MuPDF4LLMUtils.TYPE3_FONT_NAME, Is.EqualTo("Unnamed-T3"));
        }
    }
}
