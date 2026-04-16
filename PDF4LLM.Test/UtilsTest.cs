using System;
using PDF4LLMUtils = PDF4LLM.Helpers.Utils;

namespace PDF4LLM.Test
{
    [TestFixture]
    public class UtilsTest
    {
        [Test]
        public void WhiteChars_ContainsExpectedCharacters()
        {
            Assert.That(PDF4LLMUtils.WHITE_CHARS.Contains(' '), Is.True);
            Assert.That(PDF4LLMUtils.WHITE_CHARS.Contains('\t'), Is.True);
            Assert.That(PDF4LLMUtils.WHITE_CHARS.Contains('\n'), Is.True);
            Assert.That(PDF4LLMUtils.WHITE_CHARS.Contains('\u00a0'), Is.True); // Non-breaking space
            Assert.That(PDF4LLMUtils.WHITE_CHARS.Contains('a'), Is.False);
        }

        [Test]
        public void IsWhite_WithWhiteString_ReturnsTrue()
        {
            Assert.That(PDF4LLMUtils.IsWhite("   "), Is.True);
            Assert.That(PDF4LLMUtils.IsWhite("\t\n"), Is.True);
            Assert.That(PDF4LLMUtils.IsWhite("\u00a0"), Is.True); // Non-breaking space
            Assert.That(PDF4LLMUtils.IsWhite(""), Is.True);
        }

        [Test]
        public void IsWhite_WithNonWhiteString_ReturnsFalse()
        {
            Assert.That(PDF4LLMUtils.IsWhite("hello"), Is.False);
            Assert.That(PDF4LLMUtils.IsWhite("  hello  "), Is.False);
            Assert.That(PDF4LLMUtils.IsWhite("a"), Is.False);
        }

        [Test]
        public void Bullets_ContainsExpectedCharacters()
        {
            Assert.That(PDF4LLMUtils.BULLETS.Contains('*'), Is.True);
            Assert.That(PDF4LLMUtils.BULLETS.Contains('-'), Is.True);
            Assert.That(PDF4LLMUtils.BULLETS.Contains('>'), Is.True);
            Assert.That(PDF4LLMUtils.BULLETS.Contains('o'), Is.True);
        }

        [Test]
        public void ReplacementCharacter_IsCorrect()
        {
            Assert.That(PDF4LLMUtils.REPLACEMENT_CHARACTER, Is.EqualTo('\uFFFD'));
        }

        [Test]
        public void Type3FontName_IsCorrect()
        {
            Assert.That(PDF4LLMUtils.TYPE3_FONT_NAME, Is.EqualTo("Unnamed-T3"));
        }
    }
}
