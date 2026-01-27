using MuPDF.NET.LLM;

namespace MuPDF.NET.LLM.Test
{
    [TestFixture]
    public class VersionInfoTest
    {
        [Test]
        public void Version_IsNotNull()
        {
            Assert.That(VersionInfo.Version, Is.Not.Null);
            Assert.That(VersionInfo.Version, Is.Not.Empty);
        }

        [Test]
        public void MinimumMuPDFVersion_IsValid()
        {
            var (major, minor, patch) = VersionInfo.MinimumMuPDFVersion;
            Assert.That(major, Is.GreaterThanOrEqualTo(1));
            Assert.That(minor, Is.GreaterThanOrEqualTo(0));
            Assert.That(patch, Is.GreaterThanOrEqualTo(0));
        }
    }
}
