namespace MuPDF.NET.Test
{
    [TestClass]
    public class UtilsTest
    {
        [TestMethod]
        public void EnsureIdentity()
        {
            MuPDFDocument doc = new MuPDFDocument("1.pdf");
            string ret = Utils.EnsureIdentity(doc);

            Assert.AreNotEqual(ret, "");
        }
    }
}