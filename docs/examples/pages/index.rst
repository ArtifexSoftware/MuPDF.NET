.. include:: ../../header.rst

==============================
02-PagesMergeSplit
==============================

Merge a page from one PDF into another, then copy the first page into a new document.

GitHub: `02-PagesMergeSplit <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/02-PagesMergeSplit>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``pages-a.pdf`` is the destination (3 pages). ``pages-b.pdf`` is the source; only page 0 is inserted at position 1.

.. code-block:: cs

    /// <summary>Merge pages from two PDFs, then extract the first page</summary>
    static void PagesMergeSplit(string a, string b, string merged, string firstPage)
    {
        // Open both source PDFs.
        using (var docA = Document.Open(a))
        using (var docB = Document.Open(b))
        {
            // Insert page 0 of B into A at position 1 (0-based startAt).
            docA.InsertPdf(docB, fromPage: 0, toPage: 0, startAt: 1);
            docA.Save(merged);
        }

        // Split: copy only the first page of the merged file into a new document.
        using (var src = Document.Open(merged))
        using (var one = new Document())
        {
            one.InsertPdf(src, fromPage: 0, toPage: 0);
            one.Save(firstPage);
        }
    }

API: :meth:`Document.Document`, :meth:`Document.InsertPdf`, :meth:`Document.Save`.

Result of ``merged.pdf``:

.. literalinclude:: merged.summary.txt
   :language: text

Result of ``first-page.pdf``:

.. literalinclude:: first-page.summary.txt
   :language: text


.. include:: ../../footer.rst
