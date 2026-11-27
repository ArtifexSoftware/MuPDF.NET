.. include:: ../../header.rst

==============================
03-ExportToPdf
==============================

Export an Office document to PDF with ``MuPDFOffice.ToPdf``.

GitHub: `03-ExportToPdf <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET.Office/03-ExportToPdf>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``pier-yard.docx`` is a one-page North Pier yard board: a heading, an embedded mark image, status shapes (circle, rounded rectangle, rectangle), and a three-row berth table. Call :meth:`MuPDFOffice.Unlock` first so the Office document handler is registered. Empty / unset ``MUPDF_OFFICE_KEY`` is restricted mode (first 3 pages). The sample never prints the key. ``fontPathAuto`` is ``true``. When ``outputPath`` is set, ``ToPdf`` writes the PDF and returns ``null``. PDF bytes embed OS fonts, so the listing is page count and collapsed per-page text, not a byte hash. The listing is UTF-8 without a BOM.

To **open DOCX and HWPX without exporting**, see :doc:`02-OpenHwpxDocx </examples/office/openhwpx>`.

.. code-block:: cs

    /// <summary>Export an Office document to PDF</summary>
    static void ExportToPdf(string inputPath, string outputPath, string listingPath)
    {
        // Empty / unset MUPDF_OFFICE_KEY = restricted mode (first 3 pages). Never print the key.
        string? key = Environment.GetEnvironmentVariable("MUPDF_OFFICE_KEY")?.Trim();
        if (string.IsNullOrEmpty(key))
            key = null;
        MuPDFOffice.Unlock(key, fontPathAuto: true);

        // When outputPath is set, ToPdf writes the file and returns null.
        // PDF bytes embed OS fonts, so the listing is page count + collapsed text.
        MuPDFOffice.ToPdf(inputPath, outputPath);

        var listing = new StringBuilder();
        using (var doc = Document.Open(outputPath))
        {
            listing.AppendLine($"pageCount={doc.PageCount}");
            for (int i = 0; i < doc.PageCount; i++)
            {
                using Page page = doc[i];
                listing.AppendLine($"page{i}={CollapseText(page.GetText())}");
            }
        }

        string text = listing.ToString();
        File.WriteAllText(listingPath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
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

API: :meth:`MuPDFOffice.Unlock`, :meth:`MuPDFOffice.ToPdf`, :meth:`Document.Open`.

``pier-yard.docx`` (left) and the exported ``pier-yard.pdf`` (right):

.. list-table::
   :header-rows: 1
   :widths: 50 50

   * - DOCX
     - PDF
   * - .. image:: ../../images/examples-exportpdf-docx-0.png
          :alt: pier-yard.docx with heading, mark image, status shapes, and berth table
          :width: 360
     - .. image:: ../../images/examples-exportpdf-pdf-0.png
          :alt: Exported PDF of the North Pier yard board
          :width: 360

Listing written to ``export.txt``:

.. literalinclude:: export.txt
   :language: text


.. include:: ../../footer.rst
