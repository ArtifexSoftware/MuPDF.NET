.. include:: ../../header.rst

==============================
09-InsertImage
==============================

Create a page and insert a PNG into a rectangle with :meth:`Page.InsertImage`.

GitHub: `09-InsertImage <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/09-InsertImage>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``logo.png`` is a small raster fixture. The method opens an empty PDF, adds a 400×300 pt page, and places the image in ``Rect(40, 40, 140, 140)``. ``InsertImage`` returns the image :data:`xref`.

.. code-block:: cs

    /// <summary>Create a page and insert a PNG into a rectangle</summary>
    static void InsertImage(string imagePath, string outputPath)
    {
        // Empty document + custom page size (points).
        using (var doc = Document.Open())
        {
            using Page page = doc.NewPage(width: 400, height: 300);

            // Destination rectangle for the image on the page (100×100 pt).
            var rect = new Rect(40, 40, 140, 140);

            // Insert from file path; returns the image xref number.
            int xref = page.InsertImage(rect, filename: imagePath);
            Console.WriteLine($"xref={xref}");

            doc.Save(outputPath);
        }
    }

API: :meth:`Document.Open`, :meth:`Document.NewPage`, :meth:`Page.InsertImage`, :meth:`Document.Save`.

Result of ``page.InsertImage``:

.. literalinclude:: insert-info.txt
   :language: text

Result of ``with-logo.pdf`` (page 1 rendered at 2×):

.. image:: ../../images/examples-insert-image.png
   :alt: logo.png inserted into a 400 by 300 point page
   :width: 420


.. include:: ../../footer.rst
