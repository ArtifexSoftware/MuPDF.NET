.. include:: ../../header.rst

==============================
01-UnlockFonts
==============================

Unlock Office document support and read the font directories MuPDF.NET Office will search.

GitHub: `01-UnlockFonts <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET.Office/01-UnlockFonts>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

.. code-block:: cs

    using MuPDF.NET.Office;

    string? key = Environment.GetEnvironmentVariable("MUPDF_OFFICE_KEY")?.Trim();
    if (string.IsNullOrEmpty(key))
        key = null;
    int flags = MuPDFOffice.Unlock(key, fontPathAuto: true);
    var fonts = MuPDFOffice.GetFontPath();

API: :meth:`MuPDFOffice.Unlock`, :meth:`MuPDFOffice.GetFontPath`.


Unlock Office document support and inspect font search paths
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Unlock Office document support and inspect font search paths</summary>
    static void UnlockFonts()
    {
        string? key = Environment.GetEnvironmentVariable("MUPDF_OFFICE_KEY")?.Trim();
        if (string.IsNullOrEmpty(key))
            key = null;

        // fontPathAuto: discover system font directories (Windows Fonts, fc-list on Linux, …).
        int flags = MuPDFOffice.Unlock(key, fontPathAuto: true);
        var fonts = MuPDFOffice.GetFontPath();
    }

API: :meth:`MuPDFOffice.Unlock`, :meth:`MuPDFOffice.GetFontPath`.

Result:

.. literalinclude:: unlock.summary.txt
   :language: text


.. include:: ../../footer.rst
