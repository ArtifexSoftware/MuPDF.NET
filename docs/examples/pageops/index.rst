.. include:: ../../header.rst

==============================
29-PageOps
==============================

Delete, move, and select pages with :meth:`Document.InsertPdf`, :meth:`Document.MovePage`, :meth:`Document.DeletePage`, and :meth:`Document.Select`.

GitHub: `29-PageOps <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/29-PageOps>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``page-ops.pdf`` is three labeled pages (A, B, C). ``page-ops-insert.pdf`` is page X. The method inserts X at index 1, copies A again at index 2 (``A,X,A,B,C``), moves the last page to the front (``C,A,X,A,B``), deletes index 1 (``C,X,A,B``), and ``Select`` keeps only page 0 (``C``).

To **merge / extract via InsertPdf only**, see :doc:`02-PagesMergeSplit </examples/pages/index>`.

.. code-block:: cs

    /// <summary>Delete, move, and select pages in a multi-page PDF</summary>
    static void PageOps(string inputPath, string insertPath, string builtPath, string selectedPath)
    {
        static string Labels(Document doc)
        {
            var parts = new List<string>();
            for (int i = 0; i < doc.PageCount; i++)
            {
                using Page page = doc[i];
                string text = (page.GetText("text") ?? "").Replace("\r\n", "\n").Trim();
                string first = text.Split('\n')[0].Trim();
                parts.Add(first);
            }
            return string.Join(",", parts);
        }

        using (var doc = Document.Open(inputPath))
        {
            Console.WriteLine($"before={Labels(doc)}");

            // Insert X at index 1, then copy page A again at index 2.
            using (var extra = Document.Open(insertPath))
                doc.InsertPdf(extra, fromPage: 0, toPage: 0, startAt: 1);
            using (var again = Document.Open(inputPath))
                doc.InsertPdf(again, fromPage: 0, toPage: 0, startAt: 2);
            Console.WriteLine($"afterInsert={Labels(doc)}");
            Console.WriteLine($"afterInsert.pageCount={doc.PageCount}");

            // Move last page to the front.
            doc.MovePage(doc.PageCount - 1, 0);
            Console.WriteLine($"afterMove={Labels(doc)}");
            Console.WriteLine($"afterMove.pageCount={doc.PageCount}");

            // Delete the middle page (index 1).
            doc.DeletePage(1);
            Console.WriteLine($"afterDelete={Labels(doc)}");
            Console.WriteLine($"afterDelete.pageCount={doc.PageCount}");
            doc.Save(builtPath);
        }

        // Select keeps only listed page numbers (0-based), in that order.
        using (var doc = Document.Open(builtPath))
        {
            doc.Select(new[] { 0 });
            Console.WriteLine($"afterSelect={Labels(doc)}");
            Console.WriteLine($"afterSelect.pageCount={doc.PageCount}");
            doc.Save(selectedPath);
        }
    }

API: :meth:`Document.Open`, :meth:`Document.InsertPdf`, :meth:`Document.MovePage`, :meth:`Document.DeletePage`, :meth:`Document.Select`, :meth:`Document.Save`.

Result of ``PageOps``:

.. literalinclude:: page-ops.txt
   :language: text

Original ``page-ops.pdf`` (A, B, C):

.. list-table::
   :header-rows: 1
   :widths: 34 33 33

   * - page 1 — A
     - page 2 — B
     - page 3 — C
   * - .. image:: ../../images/examples-pageops-orig-0.png
          :alt: page A Harbor minutes
          :width: 180
     - .. image:: ../../images/examples-pageops-orig-1.png
          :alt: page B Berth 4 checklist
          :width: 180
     - .. image:: ../../images/examples-pageops-orig-2.png
          :alt: page C Appendix A
          :width: 180

``built.pdf`` after insert, move, and delete (C, X, A, B):

.. list-table::
   :header-rows: 1
   :widths: 25 25 25 25

   * - C
     - X
     - A
     - B
   * - .. image:: ../../images/examples-pageops-built-0.png
          :alt: built page 1 is C
          :width: 140
     - .. image:: ../../images/examples-pageops-built-1.png
          :alt: built page 2 is inserted X
          :width: 140
     - .. image:: ../../images/examples-pageops-built-2.png
          :alt: built page 3 is A
          :width: 140
     - .. image:: ../../images/examples-pageops-built-3.png
          :alt: built page 4 is B
          :width: 140

``selected.pdf`` keeps only page 0 of ``built.pdf`` (C):

.. image:: ../../images/examples-pageops-selected-0.png
   :alt: selected.pdf is page C
   :width: 240


.. include:: ../../footer.rst
