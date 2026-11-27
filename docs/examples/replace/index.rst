.. include:: ../../header.rst

==============================
17-ReplaceImage
==============================

Replace the first image on a page with :meth:`Page.ReplaceImage`.

GitHub: `17-ReplaceImage <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/17-ReplaceImage>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``images.pdf`` is a one-page fixture with two rasters (``triangle.png`` and ``rectangle.png``). The method replaces the first image xref with ``circle.png``. ``ReplaceImage`` keeps the old draw matrix.

.. code-block:: cs

    /// <summary>Replace the first image on a page</summary>
    static void ReplaceImage(string inputPath, string replacementPath, string outputPath)
    {
        using (var doc = Document.Open(inputPath))
        using (Page page = doc[0])
        {
            // images.pdf has two rasters; replace the first xref (triangle.png).
            int xref = page.GetImages(full: true)[0].Xref;

            Console.WriteLine("---before---");
            DumpImage(page, xref);

            // ReplaceImage copies the new image onto the same xref (draw matrix stays).
            page.ReplaceImage(xref, filename: replacementPath);

            Console.WriteLine("---after---");
            DumpImage(page, xref);

            doc.Save(outputPath);
        }

        static void DumpImage(Page page, int xref)
        {
            Entry img = page.GetImages(full: true).First(i => i.Xref == xref);
            Console.WriteLine($"xref={img.Xref} name={img.Name} width={img.Width} height={img.Height} bpc={img.Bpc} cs={img.CsName}");
        }
    }

API: :meth:`Document.Open`, :meth:`Page.GetImages`, :meth:`Page.ReplaceImage`, :meth:`Document.Save`.

``Page.GetImages`` before ``ReplaceImage``:

.. literalinclude:: replaced.txt
   :language: text
   :start-after: ---before---
   :end-before: ---after---

``Page.GetImages`` after ``ReplaceImage``:

.. literalinclude:: replaced.txt
   :language: text
   :start-after: ---after---

Page 1 before and after ``ReplaceImage``:

.. list-table::
   :header-rows: 1
   :widths: 50 50

   * - Before
     - After
   * - .. image:: ../../images/examples-replace-before.png
          :alt: images.pdf with triangle.png and rectangle.png
          :width: 360
     - .. image:: ../../images/examples-replace-after.png
          :alt: images.pdf after replacing triangle.png with circle.png
          :width: 360


.. include:: ../../footer.rst
