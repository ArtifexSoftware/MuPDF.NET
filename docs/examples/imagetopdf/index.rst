.. include:: ../../header.rst

==============================
27-ImageToPdf
==============================

Create a one-page PDF sized to an image and place the image full-bleed with :meth:`Document.NewPage` and :meth:`Page.InsertImage`.

GitHub: `27-ImageToPdf <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/27-ImageToPdf>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``image-to-pdf.png`` is a 480×320 RGB postcard. The method reads its pixel size with :class:`Pixmap`, opens an empty PDF, adds a page whose width and height in points match those pixels (72 dpi → 1 point per pixel), and inserts the PNG into ``page.Rect``. The PDF page is the image, with no margin.

To **place an image on a page that already has a size**, see :doc:`09-InsertImage </examples/insert/index>`.

.. code-block:: cs

    /// <summary>Create a one-page PDF sized to an image and place the image full-bleed</summary>
    static void ImageToPdf(string imagePath, string outputPath)
    {
        using var pixmap = new Pixmap(imagePath);
        // PDF page size in points: map 72 dpi → 1 point per pixel.
        float width = pixmap.Width;
        float height = pixmap.Height;

        using var doc = Document.Open();
        using Page page = doc.NewPage(width: width, height: height);
        int xref = page.InsertImage(page.Rect, filename: imagePath);
        Console.WriteLine($"width={pixmap.Width}");
        Console.WriteLine($"height={pixmap.Height}");
        Console.WriteLine($"n={pixmap.N}");
        Console.WriteLine($"pageCount={doc.PageCount}");
        Console.WriteLine($"xref={xref}");
        Console.WriteLine($"sizedToImage={page.Rect.Width == width && page.Rect.Height == height}");
        doc.Save(outputPath);
    }

API: :class:`Pixmap`, :meth:`Document.Open`, :meth:`Document.NewPage`, :meth:`Page.InsertImage`, :meth:`Document.Save`.

Result of ``ImageToPdf``:

.. literalinclude:: image-to-pdf.txt
   :language: text

Source PNG (left) and the one-page PDF rendered at 2× (right). The page is 480×320 pt — the same numbers as the raster.

.. list-table::
   :header-rows: 1
   :widths: 50 50

   * - image-to-pdf.png (480×320)
     - wrapped.pdf page 1
   * - .. image:: ../../images/examples-imagetopdf-source.png
          :alt: North Pier postcard PNG
          :width: 240
     - .. image:: ../../images/examples-imagetopdf-page.png
          :alt: same postcard as a full-bleed PDF page
          :width: 240


.. include:: ../../footer.rst
