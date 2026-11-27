.. include:: ../../header.rst

==============================
31-ParallelPixmap
==============================

Render the first page of several independent PDFs in parallel with ``Parallel.ForEach`` and :meth:`Page.GetPixmap`.

GitHub: `31-ParallelPixmap <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/31-ParallelPixmap>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``parallel-a.pdf``, ``parallel-b.pdf``, and ``parallel-c.pdf`` are one-page hosts with different sizes (480×280, 360×400, 320×320). Each worker **opens its own** :class:`Document`. Do not share one document across threads — MuPDF serializes page load on the same document. Cap ``MaxDegreeOfParallelism`` at ``Environment.ProcessorCount``. You may see ``warning: found duplicate fz_icc_link in the store``; that is harmless.

To **render one page sequentially**, see :doc:`03-RenderPixmap </examples/render/index>`.

.. code-block:: cs

    /// <summary>Render independent PDFs in parallel. Each worker opens its own Document</summary>
    static void ParallelPixmap(string[] inputPaths, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        // Cap workers to CPU count. Unbounded Task.Run can spike RAM on large pages.
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
        };

        Parallel.ForEach(inputPaths, options, inputPath =>
        {
            // One Document per worker. Do not share a Document across threads.
            using var doc = Document.Open(inputPath);
            using Page page = doc[0];
            using Pixmap pix = page.GetPixmap(matrix: new Matrix(2, 2));
            string outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(inputPath) + ".png");
            pix.Save(outputPath);
        });

        Console.WriteLine($"fileCount={inputPaths.Length}");
        foreach (string inputPath in inputPaths)
        {
            string stem = Path.GetFileNameWithoutExtension(inputPath);
            string outputPath = Path.Combine(outputDir, stem + ".png");
            using var pix = new Pixmap(outputPath);
            Console.WriteLine($"{stem} width={pix.Width} height={pix.Height} n={pix.N}");
        }
    }

API: :meth:`Document.Open`, :meth:`Page.GetPixmap`, :meth:`Pixmap.Save`.

Result of ``ParallelPixmap`` (pixmap sizes are 2× the page size):

.. literalinclude:: parallel.txt
   :language: text

Each file rendered at 2×:

.. list-table::
   :header-rows: 1
   :widths: 34 33 33

   * - parallel-a.pdf (960×560)
     - parallel-b.pdf (720×800)
     - parallel-c.pdf (640×640)
   * - .. image:: ../../images/examples-parallel-a.png
          :alt: Harbor minutes page rendered in parallel
          :width: 180
     - .. image:: ../../images/examples-parallel-b.png
          :alt: Berth 4 checklist page rendered in parallel
          :width: 180
     - .. image:: ../../images/examples-parallel-c.png
          :alt: Appendix A page rendered in parallel
          :width: 180


.. include:: ../../footer.rst
