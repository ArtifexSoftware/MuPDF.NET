.. include:: ../../header.rst

==============================
25-CompressSave
==============================

Compare a plain :meth:`Document.Save` with a compressed save (``garbage``, ``deflate``, ``useObjstms``).

GitHub: `25-CompressSave <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/25-CompressSave>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``compress-save.pdf`` is a one-page bulletin: header, checklist text, three option cards, a ruled table, vector shapes, and an uncompressed harbor raster, plus 40 duplicate unused objects. A default ``Save`` stays 216,067 bytes. ``garbage: 4`` drops unused/duplicate objects, ``deflate: 1`` Flate-compresses streams, and ``useObjstms: 1`` packs object definitions into those streams — 10,910 bytes (5.0%).

The **page looks the same**. Compression does not change pixels or layout. For downsampling embedded images instead, see :doc:`20-RewriteImages </examples/rewrite/index>`.

.. code-block:: cs

    /// <summary>Compare a plain save vs a compressed save (garbage + deflate + object streams)</summary>
    static void CompressSave(string inputPath, string plainPath, string compressedPath)
    {
        using (var doc = Document.Open(inputPath))
            doc.Save(plainPath);

        using (var doc = Document.Open(inputPath))
        {
            // garbage:4 drops unused/duplicate objects; deflate compresses streams;
            // useObjstms packs object definitions into those streams.
            doc.Save(
                compressedPath,
                garbage: 4,
                deflate: 1,
                deflateImages: 1,
                deflateFonts: 1,
                useObjstms: 1);
        }

        long inputBytes = new System.IO.FileInfo(inputPath).Length;
        long plainBytes = new System.IO.FileInfo(plainPath).Length;
        long compressedBytes = new System.IO.FileInfo(compressedPath).Length;
        string fileSizePct = (100.0 * compressedBytes / plainBytes)
            .ToString("0.0", CultureInfo.InvariantCulture);
        Console.WriteLine($"inputBytes={inputBytes}");
        Console.WriteLine($"plainBytes={plainBytes}");
        Console.WriteLine($"compressedBytes={compressedBytes}");
        Console.WriteLine($"fileSizePct={fileSizePct}");
        Console.WriteLine($"smallerOrEqual={compressedBytes <= plainBytes}");
    }

API: :meth:`Document.Open`, :meth:`Document.Save`.

Result of ``CompressSave``:

.. literalinclude:: compress.txt
   :language: text

Plain save (left) and compressed save (right). File size 100% vs 5.0%; the rendered pages match.

.. list-table::
   :header-rows: 1
   :widths: 50 50

   * - plain.pdf (216,067 bytes)
     - compressed.pdf (10,910 bytes)
   * - .. image:: ../../images/examples-compress-plain.png
          :alt: North Pier bulletin after a default Save
          :width: 240
     - .. image:: ../../images/examples-compress-compressed.png
          :alt: same bulletin after garbage, deflate, and object streams
          :width: 240


.. include:: ../../footer.rst
