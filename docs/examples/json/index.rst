.. include:: ../../header.rst

==============================
33-ToJsonLayout
==============================

Convert a PDF to layout JSON with ``Document.ToJson``.

GitHub: `33-ToJsonLayout <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/33-ToJsonLayout>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``bulletin.pdf`` is the same one-page North Pier bulletin as :doc:`32-ToMarkdown </examples/markdown/index>` (500×460). ``Utils.UseLayout(true)`` selects the layout path. ``Document.ToJson`` writes ``bulletin.json`` with per-page ``boxes`` (``boxclass``, bbox, text). ``useOcr`` is ``false`` because this PDF is already text. Without a layout worker, the method prints ``LAYOUT_UNAVAILABLE`` and skips JSON.

To **convert the same PDF to Markdown** with layout off, see :doc:`32-ToMarkdown </examples/markdown/index>`. For **layout plain text**, see :doc:`34-ToText </examples/totext/index>`. For **retrieval chunks**, see :doc:`35-ToChunks </examples/chunks/index>`.

.. code-block:: cs

    /// <summary>Convert a PDF to layout JSON (requires the optional layout worker)</summary>
    static void ToJsonLayout(string inputPath, string outputPath)
    {
        // Remember prior layout flag so we restore it for other examples in the same process.
        bool prior = Utils.LayoutEnabled;
        try
        {
            // ToJson requires UseLayout=true and a registered layout worker.
            if (!Utils.LayoutAvailable)
            {
                Console.WriteLine("LAYOUT_UNAVAILABLE");
                return;
            }

            Utils.UseLayout(true);

            using var doc = Document.Open(inputPath);
            string json = doc.ToJson(showProgress: false, useOcr: false) ?? "";
            File.WriteAllText(outputPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Console.WriteLine($"pageCount={doc.PageCount}");
            Console.WriteLine($"layoutEnabled={Utils.LayoutEnabled}");
            WriteBoxes(json);
        }
        finally
        {
            Utils.UseLayout(prior);
        }
    }

API: :meth:`Document.Open`, :attr:`Utils.LayoutAvailable`, :meth:`Utils.UseLayout`, :meth:`Document.ToJson`.

``bulletin.pdf``:

.. image:: ../../images/examples-json-page.png
   :alt: North Pier bulletin used as ToJson input
   :width: 360

Layout boxes written to ``bulletin.json``:

.. literalinclude:: layout.txt
   :language: text


.. include:: ../../footer.rst
