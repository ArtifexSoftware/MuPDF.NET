.. include:: ../../header.rst

==============================
24-EncryptDecrypt
==============================

Save an AES-256 encrypted PDF with :meth:`Document.Save`, open it with :meth:`Document.Authenticate`, then save decrypted.

GitHub: `24-EncryptDecrypt <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/24-EncryptDecrypt>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``encrypt-decrypt.pdf`` is a one-page unencrypted fixture. ``Save`` with ``Constants.PDF_ENCRYPT_AES_256`` writes ``encrypted.pdf`` using user password ``user-secret`` and owner password ``owner-secret``. Opening that file sets :attr:`Document.NeedsPass` and :attr:`Document.IsEncrypted` until ``Authenticate`` succeeds. Return value ``2`` is the **user** password. ``Save`` with ``Constants.PDF_ENCRYPT_NONE`` writes ``decrypted.pdf`` with no password.

Encrypted PDF bytes are not stable across saves, so the harness fingerprints the decrypted file only.

.. code-block:: cs

    /// <summary>Save an AES-256 encrypted PDF, open it with a password, then save decrypted</summary>
    static void EncryptDecrypt(string inputPath, string encryptedPath, string decryptedPath)
    {
        const string userPassword = "user-secret";
        const string ownerPassword = "owner-secret";

        using (var doc = Document.Open(inputPath))
        {
            Console.WriteLine($"before.needsPass={doc.NeedsPass}");
            Console.WriteLine($"before.isEncrypted={doc.IsEncrypted}");
            doc.Save(
                encryptedPath,
                encryption: Constants.PDF_ENCRYPT_AES_256,
                ownerPW: ownerPassword,
                userPW: userPassword);
            Console.WriteLine("encrypted=ok");
        }

        using (var doc = Document.Open(encryptedPath))
        {
            Console.WriteLine($"needsPass={doc.NeedsPass}");
            Console.WriteLine($"isEncrypted={doc.IsEncrypted}");

            // Authenticate with the user password before reading / saving.
            int auth = doc.Authenticate(userPassword);
            Console.WriteLine($"authenticate={auth}");
            Console.WriteLine($"pages={doc.PageCount}");
            Console.WriteLine($"encryption={EncryptionName(doc)}");

            doc.Save(
                decryptedPath,
                encryption: Constants.PDF_ENCRYPT_NONE,
                garbage: 3,
                deflate: 1);
            Console.WriteLine("decrypted=ok");
        }

        using (var doc = Document.Open(decryptedPath))
        {
            Console.WriteLine($"after.needsPass={doc.NeedsPass}");
            Console.WriteLine($"after.isEncrypted={doc.IsEncrypted}");
            Console.WriteLine($"after.pages={doc.PageCount}");
            Console.WriteLine($"after.encryption={EncryptionName(doc)}");
        }

        static string EncryptionName(Document doc)
        {
            if (doc.Metadata != null && doc.Metadata.TryGetValue("encryption", out string? value)
                && !string.IsNullOrEmpty(value))
                return value;
            return "none";
        }
    }

API: :meth:`Document.Open`, :attr:`Document.NeedsPass`, :attr:`Document.IsEncrypted`, :meth:`Document.Authenticate`, :meth:`Document.Save`.

Result of ``EncryptDecrypt``:

.. literalinclude:: encrypt-decrypt.txt
   :language: text

Original (no password) and decrypted output. The pages match; ``encrypted.pdf`` needs ``user-secret`` before it can be opened.

.. list-table::
   :header-rows: 1
   :widths: 50 50

   * - encrypt-decrypt.pdf
     - decrypted.pdf
   * - .. image:: ../../images/examples-encrypt-original.png
          :alt: unencrypted source page labeled plain
          :width: 360
     - .. image:: ../../images/examples-encrypt-decrypted.png
          :alt: decrypted page, same content as the source
          :width: 360


.. include:: ../../footer.rst
