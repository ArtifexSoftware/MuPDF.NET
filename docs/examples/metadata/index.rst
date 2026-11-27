.. include:: ../../header.rst

==============================
14-Metadata
==============================

Read and update the PDF Info dictionary with :attr:`Document.Metadata` and :meth:`Document.SetMetadata`.

GitHub: `14-Metadata <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/14-Metadata>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``basic.pdf`` is the AES-256 fixture from 01-Basic (user password ``user``). ``Authenticate`` is required before reading or writing the Info dictionary. ``SetMetadata`` updates only the keys you pass; ``creationDate`` / ``modDate`` are omitted from the dump because they are timestamps.

.. code-block:: cs

    /// <summary>Read and update PDF document metadata</summary>
    static void Metadata(string inputPath, string outputPath)
    {
        using (var doc = Document.Open(inputPath))
        {
            // basic.pdf is AES-256; authenticate before reading the Info dictionary.
            doc.Authenticate("user");

            Console.WriteLine("---before---");
            DumpMetadata(doc.Metadata);

            // SetMetadata updates the PDF Info keys that are present in the dictionary.
            doc.SetMetadata(new Dictionary<string, string>
            {
                ["title"] = "MuPDF.NET.Examples — Metadata",
                ["author"] = "Artifex",
                ["subject"] = "14-Metadata sample",
                ["creator"] = "MuPDF.NET.Examples",
            });

            Console.WriteLine("---after---");
            DumpMetadata(doc.Metadata);

            doc.Save(outputPath);
        }

        static void DumpMetadata(Dictionary<string, string> meta)
        {
            foreach (string key in meta.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                // creationDate / modDate are timestamps; omit them from the stable dump.
                if (key.Equals("creationDate", StringComparison.OrdinalIgnoreCase)
                    || key.Equals("modDate", StringComparison.OrdinalIgnoreCase))
                    continue;
                Console.WriteLine($"{key}={meta[key]}");
            }
        }
    }

API: :meth:`Document.Open`, :meth:`Document.Authenticate`, :attr:`Document.Metadata` (also MetaData), :meth:`Document.SetMetadata`, :meth:`Document.Save`.

``Document.Metadata`` before ``SetMetadata``:

.. literalinclude:: metadata.txt
   :language: text
   :start-after: ---before---
   :end-before: ---after---

``Document.Metadata`` after ``SetMetadata``:

.. literalinclude:: metadata.txt
   :language: text
   :start-after: ---after---

Result of ``with-metadata.pdf`` (page 1 rendered at 2×):

.. image:: ../../images/examples-metadata.png
   :alt: Page 1 of basic.pdf after SetMetadata
   :width: 420


.. include:: ../../footer.rst
