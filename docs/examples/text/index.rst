.. include:: ../../header.rst

==============================
04-TextExtractSearch
==============================

Extract plain text from page 1 and search for a string.

GitHub: `04-TextExtractSearch <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/04-TextExtractSearch>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``unicode.pdf`` has one page of labeled lines in English, German, French, Spanish, Russian, Greek, Vietnamese, Chinese, Japanese, Arabic, Hebrew, Thai, and Hindi. ``SearchFor`` looks for ``こんにちは``.

.. code-block:: cs

    /// <summary>Extract plain text from page 1 and search for a string</summary>
    static void TextExtractSearch(string input, string needle)
    {
        using (var doc = Document.Open(input))
        using (Page page = doc[0])
        {
            // "text" = plain text extraction (also: "blocks", "words", "html", "dict", …).
            string text = page.GetText("text") ?? "";
            Console.WriteLine(text);

            // SearchFor returns hit quads on the page.
            var hits = page.SearchFor(needle);
            Console.WriteLine($"hitCount={hits.Count}");
            Console.WriteLine($"needle={needle}");
            foreach (Quad hit in hits)
                Console.WriteLine($"  {hit}");
        }
    }

API: :meth:`Document.Document`, :meth:`Page.GetText`, :meth:`Page.SearchFor`.

Result of ``page.GetText("text")``:

.. literalinclude:: page-1.txt
   :language: text

Result of ``page.SearchFor("こんにちは")``:

.. literalinclude:: search.summary.txt
   :language: text


.. include:: ../../footer.rst
