.. include:: ../../header.rst

==============================
02-OpenHwpxDocx
==============================

Unlock Office, then open DOCX and HWPX with ``Document.Open``.

GitHub: `02-OpenHwpxDocx <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET.Office/02-OpenHwpxDocx>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``pier-notice.docx`` is a three-page North Pier notice (standing orders, berth schedule, tide note). Restricted mode still loads all three pages. ``pier-notice.hwpx`` is a one-page Hancom HWPX berth notice. Call :meth:`MuPDFOffice.Unlock` first so the Office document handler is registered. Empty / unset ``MUPDF_OFFICE_KEY`` is restricted mode (first 3 pages). The sample never prints the key. ``fontPathAuto`` is ``true``. Page text in the listing is collapsed whitespace so the baseline stays portable. The listing is UTF-8 without a BOM.

To **unlock and inspect font directories**, see :doc:`01-Unlock </examples/office/unlock>`.

.. code-block:: cs

    /// <summary>Unlock Office, then open DOCX and HWPX via Document.Open</summary>
    static void OpenHwpxDocx(string docxPath, string hwpxPath, string outputPath)
    {
        // Empty / unset MUPDF_OFFICE_KEY = restricted mode (first 3 pages). Never print the key.
        string? key = Environment.GetEnvironmentVariable("MUPDF_OFFICE_KEY")?.Trim();
        if (string.IsNullOrEmpty(key))
            key = null;
        MuPDFOffice.Unlock(key, fontPathAuto: true);

        var listing = new StringBuilder();
        using (var docx = Document.Open(docxPath))
        using (Page docxPage = docx[0])
        {
            listing.AppendLine($"docxPages={docx.PageCount}");
            listing.AppendLine($"docxPage0={CollapseText(docxPage.GetText())}");
        }

        using (var hwpx = Document.Open(hwpxPath))
        using (Page hwpxPage = hwpx[0])
        {
            listing.AppendLine($"hwpxPages={hwpx.PageCount}");
            listing.AppendLine($"hwpxPage0={CollapseText(hwpxPage.GetText())}");
        }

        string text = listing.ToString();
        File.WriteAllText(outputPath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.Write(text);
    }

    static string CollapseText(object? value)
    {
        string s = value?.ToString() ?? "";
        var sb = new StringBuilder(s.Length);
        bool space = false;
        foreach (char c in s)
        {
            if (char.IsWhiteSpace(c))
            {
                space = true;
                continue;
            }
            if (space && sb.Length > 0)
                sb.Append(' ');
            space = false;
            sb.Append(c);
        }
        return sb.ToString();
    }

API: :meth:`MuPDFOffice.Unlock`, :meth:`Document.Open`, :meth:`Page.GetText`.

``pier-notice.docx`` page 1:

.. image:: ../../images/examples-openhwpx-docx.png
   :alt: North Pier standing orders opened from pier-notice.docx
   :width: 420

``pier-notice.hwpx``:

.. image:: ../../images/examples-openhwpx-hwpx.png
   :alt: North Pier berth notice opened from pier-notice.hwpx
   :width: 420

Listing written to ``open.txt``:

.. literalinclude:: open.txt
   :language: text


.. include:: ../../footer.rst
