.. include:: ../../header.rst

==============================
20-RewriteImages
==============================

Downsample and recompress images in a PDF with :meth:`Document.RewriteImages`.

GitHub: `20-RewriteImages <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/20-RewriteImages>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``rewrite-images.pdf`` is a one-page fixture with four 360 DPI rasters (color PNG, color JPEG, gray PNG, bitonal PNG). ``RewriteImages`` subsamples images above 100 DPI down to 72 DPI and recompresses at JPEG quality 33.

The **page rectangle stays 420×456 pt**, and each picture is still drawn in the same **160×160 pt** box (``draw=``). Only the **pixel grid** inside that box changes: color and gray rasters go from 800×800 to 200×200 (25% width), so they look the same size on the page, just softer. The PDF file size drops to 0.5% of the input. Bitonal stays 800×800.

.. code-block:: cs

    /// <summary>Downsample and recompress images in a PDF</summary>
    static void RewriteImages(string inputPath, string outputPath)
    {
        long size0 = new System.IO.FileInfo(inputPath).Length;
        int beforeW;

        using (var doc = Document.Open(inputPath))
        using (Page page = doc[0])
        {
            Console.WriteLine("---before---");
            Console.WriteLine($"page={page.Width:0}x{page.Height:0}");
            Console.WriteLine($"inputBytes={size0}");
            DumpImages(page);
            beforeW = (int)page.GetImages(full: true)[0].Width;

            // Subsample images above 100 DPI down to 72 DPI; JPEG quality 33.
            doc.RewriteImages(dpiThreshold: 100, dpiTarget: 72, quality: 33);
            doc.Save(outputPath, garbage: 3, deflate: 1);
        }

        long size1 = new System.IO.FileInfo(outputPath).Length;
        using (var doc = Document.Open(outputPath))
        using (Page page = doc[0])
        {
            int afterW = (int)page.GetImages(full: true)[0].Width;
            int pixelPct = (int)Math.Round(100.0 * afterW / beforeW);
            string fileSizePct = (100.0 * size1 / size0).ToString("0.0", CultureInfo.InvariantCulture);
            Console.WriteLine("---after---");
            Console.WriteLine($"page={page.Width:0}x{page.Height:0}");
            Console.WriteLine($"pixelPct={pixelPct}");
            Console.WriteLine($"fileSizePct={fileSizePct}");
            DumpImages(page);
        }

        static void DumpImages(Page page)
        {
            List<Entry> images = page.GetImages(full: true);
            Console.WriteLine($"imageCount={images.Count}");
            foreach (Entry img in images)
            {
                Rect bbox = page.GetImageBbox((object)img.Name);
                Console.WriteLine($"xref={img.Xref} name={img.Name} pixels={img.Width:0}x{img.Height:0} draw={bbox.Width:0}x{bbox.Height:0} bpc={img.Bpc} cs={img.CsName}");
            }
        }
    }

API: :meth:`Document.Open`, :meth:`Page.GetImages`, :meth:`Page.GetImageBbox`, :meth:`Document.RewriteImages`, :meth:`Document.Save`.

``Page.GetImages`` before ``RewriteImages``:

.. literalinclude:: rewritten.txt
   :language: text
   :start-after: ---before---
   :end-before: ---after---

``Page.GetImages`` after ``RewriteImages``:

.. literalinclude:: rewritten.txt
   :language: text
   :start-after: ---after---

First color raster, drawn at the pixel-size ratio 800:200 (100% vs 25%). File size is 0.5% of the input.

.. list-table::
   :header-rows: 1
   :widths: 80 20

   * - Before 800×800 (100%)
     - After 200×200 (25%)
   * - .. image:: ../../images/examples-rewrite-raster-before.png
          :alt: RGB PNG raster before RewriteImages, 800 by 800 pixels
          :width: 320
     - .. image:: ../../images/examples-rewrite-raster-after.png
          :alt: RGB PNG raster after RewriteImages, 200 by 200 pixels
          :width: 80

Page 1 still 420×456 pt (layout unchanged):

.. list-table::
   :header-rows: 1
   :widths: 50 50

   * - Before
     - After
   * - .. image:: ../../images/examples-rewrite-before.png
          :alt: rewrite-images.pdf page 1 before RewriteImages
          :width: 360
     - .. image:: ../../images/examples-rewrite-after.png
          :alt: rewrite-images.pdf page 1 after RewriteImages
          :width: 360


.. include:: ../../footer.rst
