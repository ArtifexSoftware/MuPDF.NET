.. include:: ../../header.rst

==============================
05-Recolor
==============================

Recolor page 1 of a PDF to CMYK (4 components) and save.

GitHub: `05-Recolor <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/05-Recolor>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``color-shapes.pdf`` is a one-page RGB board (hue bar, swatches, circle, round rect, triangle). ``Recolor(0, 4)`` converts page 0 to CMYK. The after image is a bit duller because CMYK cannot match every RGB hue.

.. code-block:: cs

    /// <summary>Recolor page 1 to CMYK (4 components) and save</summary>
    static void Recolor(string inputPath, string outputPath)
    {
        using (var doc = Document.Open(inputPath))
        {
            // Recolor page 0 → 4 components (DeviceCMYK / ICC-based CMYK).
            doc.Recolor(0, 4);
            doc.Save(outputPath);
        }
    }

API: :meth:`Document.Open`, :meth:`Document.Recolor`, :meth:`Document.Save`.

Result of ``recolor.pdf``:

.. literalinclude:: recolor.summary.txt
   :language: text

Page 1 before and after ``Recolor(0, 4)``:

.. list-table::
   :header-rows: 1
   :widths: 50 50

   * - Before
     - After
   * - .. image:: ../../images/examples-recolor-before.png
          :alt: Page 1 of color-shapes.pdf before Recolor
          :width: 360
     - .. image:: ../../images/examples-recolor-after.png
          :alt: Page 1 of color-shapes.pdf after Recolor to CMYK
          :width: 360


.. include:: ../../footer.rst
