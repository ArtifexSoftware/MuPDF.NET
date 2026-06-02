// Port of PyMuPDF-1.27.2.2/tests/test_crypting.py
using System.IO;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Check PDF encryption:
    /// * make a PDF with owner and user passwords
    /// * open and decrypt as owner or user
    /// Output: <c>TestDocuments/_Output/TestCrypting/</c>.
    /// </summary>
    [Collection("MuPDF.NET native")]
    public class TestCrypting
    {
        private static readonly string outDocPath = _Path.ForOutput("test_encryption.pdf", nameof(TestCrypting));

        /// <summary>
        /// PyMuPDF <c>tests/test_crypting.py::test_encryption</c>.
        /// </summary>
        [Fact]
        public void test_encryption()
        {
            string text = "some secret information"; // keep this data secret
            int perm = mupdf.mupdf.PDF_PERM_ACCESSIBILITY // always use this
                | mupdf.mupdf.PDF_PERM_PRINT // permit printing
                | mupdf.mupdf.PDF_PERM_COPY // permit copying
                | mupdf.mupdf.PDF_PERM_ANNOTATE; // permit annotations
            string owner_pass = "owner"; // owner password
            string user_pass = "user"; // user password
            int encrypt_meth = mupdf.mupdf.PDF_ENCRYPT_AES_256; // strongest algorithm
            byte[] tobytes;
            using (var doc = new Document()) // empty pdf
            {
                var page = doc.NewPage(); // empty page
                page.InsertText(new Point(50, 72), text); // insert the data
                using var ms = new MemoryStream();
                // Python: tobytes = doc.tobytes(encryption=..., owner_pw=..., user_pw=..., permissions=perm)
                doc.Save(
                    ms,
                    encryption: encrypt_meth, // set the encryption method
                    permissions: perm, // set permissions
                    owner_pw: owner_pass, // set the owner password
                    user_pw: user_pass); // set the user password
                tobytes = ms.ToArray();
                doc.Save(outDocPath);
            }

            using (var doc = new Document(tobytes, "pdf")) // Python: doc = pymupdf.open("pdf", tobytes)
            {
                Assert.True(doc.NeedsPass);
                Assert.True(doc.IsEncrypted);
                // Python: rc = doc.authenticate("owner")  # returns fz_authenticate_password bitmask
                int rc = mupdf.mupdf.fz_authenticate_password(doc.NativeDocument, "owner");
                Assert.Equal(4, rc);
                Assert.NotEqual(0, doc.Authenticate("owner")); // sync Document state (init_doc, is_encrypted)
                Assert.False(doc.IsEncrypted);
            }

            using (var doc = new Document(tobytes, "pdf")) // Python: doc = pymupdf.open("pdf", tobytes)
            {
                // Python: rc = doc.authenticate("user")
                int rc = mupdf.mupdf.fz_authenticate_password(doc.NativeDocument, "user");
                Assert.Equal(2, rc);
                Assert.NotEqual(0, doc.Authenticate("user"));
            }
        }
    }
}
