.. include:: ../../header.rst

==============================
05-ToMarkdown
==============================

Unlock Office, then run ``Document.ToMarkdown`` on an HWPX path.

GitHub: `05-ToMarkdown <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET.Office/05-ToMarkdown>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``pier-notice.hwpx`` is the same one-page Hancom HWPX berth notice as :doc:`02-OpenHwpxDocx </examples/office/openhwpx>`. Call :meth:`MuPDFOffice.Unlock` first so the Office document handler is registered. Empty / unset ``MUPDF_OFFICE_KEY`` is restricted mode (first 3 pages). The sample never prints the key. ``fontPathAuto`` is ``true``. ``Utils.UseLayout`` is ``false`` (classic extract, no layout worker). ``useOcr`` is ``false`` because this document is already text. This is :meth:`Document.ToMarkdown` after Unlock, not :meth:`MuPDFOffice.ToMarkdown`. The ``.md`` file is UTF-8 without a BOM.

To **open DOCX and HWPX without converting**, see :doc:`02-OpenHwpxDocx </examples/office/openhwpx>`. To **export a DOCX with the Office helpers**, see :doc:`04-ExportToMarkdownJson </examples/office/exportmdjson>`. To **convert a PDF to Markdown**, see :doc:`32-ToMarkdown </examples/markdown/index>`.

.. code-block:: cs

    /// <summary>Unlock Office, then run Document.ToMarkdown on an HWPX path</summary>
    static void ToMarkdown(string inputPath, string outputPath)
    {
        // Empty / unset MUPDF_OFFICE_KEY = restricted mode (first 3 pages). Never print the key.
        string? key = Environment.GetEnvironmentVariable("MUPDF_OFFICE_KEY")?.Trim();
        if (string.IsNullOrEmpty(key))
            key = null;
        MuPDFOffice.Unlock(key, fontPathAuto: true);

        // Remember prior layout flag so we restore it for other examples in the same process.
        bool prior = Utils.LayoutEnabled;
        try
        {
            // false = classic extract (no layout worker required).
            Utils.UseLayout(false);

            using var doc = Document.Open(inputPath);
            string markdown = doc.ToMarkdown(showProgress: false, useOcr: false) ?? "";
            File.WriteAllText(outputPath, markdown, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Console.WriteLine($"pageCount={doc.PageCount}");
            Console.WriteLine($"layoutEnabled={Utils.LayoutEnabled}");
            Console.WriteLine($"charCount={markdown.Length}");
            Console.WriteLine(markdown);
        }
        finally
        {
            Utils.UseLayout(prior);
        }
    }

API: :meth:`MuPDFOffice.Unlock`, :meth:`Utils.UseLayout`, :meth:`Document.Open`, :meth:`Document.ToMarkdown`.

``pier-notice.hwpx``:

.. image:: ../../images/examples-tomarkdown-hwpx.png
   :alt: North Pier berth notice opened from pier-notice.hwpx
   :width: 420

Markdown written to ``pier-notice.md``:

.. literalinclude:: pier-notice.md
   :language: markdown


.. include:: ../../footer.rst
