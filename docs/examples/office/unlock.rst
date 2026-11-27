.. include:: ../../header.rst

==============================
01-Unlock
==============================

Unlock Office document support and inspect font search directories.

GitHub: `01-Unlock <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET.Office/01-Unlock>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``MuPDFOffice.Unlock`` enables DOCX/HWPX (and other Office) handling on the shared MuPDF context. Empty / unset ``MUPDF_OFFICE_KEY`` is restricted mode (first 3 pages). The sample never prints the key. ``fontPathAuto`` is ``true`` so Unlock adds system font directories (Windows Fonts, fontconfig on Linux, and known Unix roots). ``GetFontPath`` returns those directories; the listing prints each path. Paths are machine-specific (this page shows a Windows run). ``IsUnlocked`` is ``true`` after Unlock succeeds, including restricted mode. The listing is UTF-8 without a BOM.

.. code-block:: cs

    /// <summary>Unlock Office document support and inspect font search paths</summary>
    static void Unlock(string outputPath)
    {
        // Empty / unset MUPDF_OFFICE_KEY = restricted mode (first 3 pages). Never print the key.
        string? key = Environment.GetEnvironmentVariable("MUPDF_OFFICE_KEY")?.Trim();
        if (string.IsNullOrEmpty(key))
            key = null;

        // fontPathAuto: discover system font directories (Windows Fonts, fc-list on Linux, …).
        MuPDFOffice.Unlock(key, fontPathAuto: true);
        var fonts = MuPDFOffice.GetFontPath();

        var listing = new StringBuilder();
        listing.AppendLine($"unlocked={MuPDFOffice.IsUnlocked}");
        foreach (string dir in fonts)
            listing.AppendLine($"fontPath={dir}");

        string text = listing.ToString();
        File.WriteAllText(outputPath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.Write(text);
    }

API: :meth:`MuPDFOffice.Unlock`, :meth:`MuPDFOffice.GetFontPath`, :meth:`MuPDFOffice.IsUnlocked`.

Listing written to ``unlock.txt``:

.. literalinclude:: unlock.txt
   :language: text


.. include:: ../../footer.rst
