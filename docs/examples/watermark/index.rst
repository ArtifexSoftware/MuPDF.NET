.. include:: ../../header.rst

==============================
26-Watermark
==============================

Insert a stamp image as a watermark on every page with :meth:`Page.InsertImage`.

GitHub: `26-Watermark <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/26-Watermark>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``watermark.pdf`` is a two-page host. ``watermark.png`` is a transparent DRAFT stamp. The method inserts the PNG once (``overlay: "true"`` puts it in front of the page content), then reuses that image :data:`xref` on every later page so the PDF stores the raster only once.

To **place a new image** without repeating it as a watermark, see :doc:`09-InsertImage </examples/insert/index>`.

.. code-block:: cs

    /// <summary>Insert a stamp image as a watermark on every page</summary>
    static void Watermark(string inputPath, string stampPath, string outputPath)
    {
        using var doc = Document.Open(inputPath);
        // Insert once as a reusable xref, then place on each page.
        int imageXref = -1;
        for (int i = 0; i < doc.PageCount; i++)
        {
            using Page page = doc[i];
            Rect bounds = page.Rect;
            // Centered watermark rectangle (~40% of page width).
            float w = bounds.Width * 0.4f;
            float h = bounds.Height * 0.4f;
            float x0 = bounds.X0 + (bounds.Width - w) / 2f;
            float y0 = bounds.Y0 + (bounds.Height - h) / 2f;
            var rect = new Rect(x0, y0, x0 + w, y0 + h);

            if (imageXref < 0)
                imageXref = page.InsertImage(rect, filename: stampPath, keepProportion: true, overlay: "true");
            else
                page.InsertImage(rect, xref: imageXref, keepProportion: true, overlay: "true");
            Console.WriteLine($"page={i} xref={imageXref}");
        }

        Console.WriteLine($"pageCount={doc.PageCount}");
        Console.WriteLine($"xref={imageXref}");
        doc.Save(outputPath);
    }

API: :meth:`Document.Open`, :meth:`Page.InsertImage`, :meth:`Document.Save`.

Result of ``Watermark``:

.. literalinclude:: watermark.txt
   :language: text

Page 1 before the stamp, then both watermarked pages (same xref 20):

.. list-table::
   :header-rows: 1
   :widths: 34 33 33

   * - watermark.pdf page 1
     - watermarked.pdf page 1
     - watermarked.pdf page 2
   * - .. image:: ../../images/examples-watermark-before.png
          :alt: Harbor minutes without a stamp
          :width: 240
     - .. image:: ../../images/examples-watermark-page-0.png
          :alt: Harbor minutes with DRAFT watermark
          :width: 240
     - .. image:: ../../images/examples-watermark-page-1.png
          :alt: Appendix A with the same DRAFT xref
          :width: 240


.. include:: ../../footer.rst
