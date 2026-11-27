.. include:: ../../header.rst

==============================
28-ImageFilters
==============================

Apply a median :class:`ImageFilterPipeline` to a pixmap and save the result with :meth:`Pixmap.ApplyImageFilters`.

GitHub: `28-ImageFilters <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/28-ImageFilters>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``image-filters.png`` is a 480×320 greyscale postcard with salt-and-pepper speckle. The method loads it as a :class:`Pixmap`, adds :meth:`ImageFilterPipeline.AddMedian` with a 3×3 kernel, and writes ``filtered.png``. :meth:`Pixmap.ApplyImageFilters` may dispose the source pixmap — do not use it afterward.

To **change a page colorspace** instead, see :doc:`05-Recolor </examples/recolor/index>`. To **downsample images inside a PDF**, see :doc:`20-RewriteImages </examples/rewrite/index>`.

.. code-block:: cs

    /// <summary>Apply a median filter pipeline to a pixmap and save the result</summary>
    static void ImageFilters(string inputPath, string outputPath)
    {
        using var source = new Pixmap(inputPath);
        int width = source.Width;
        int height = source.Height;

        var pipeline = new ImageFilterPipeline();
        // Light denoise — stable, docs-friendly filter.
        pipeline.AddMedian(3);

        // ApplyImageFilters may consume/dispose the source pixmap — do not use source afterward.
        using Pixmap filtered = Pixmap.ApplyImageFilters(source, pipeline);
        Console.WriteLine($"width={width}");
        Console.WriteLine($"height={height}");
        Console.WriteLine($"n={filtered.N}");
        Console.WriteLine($"filterCount={pipeline.Filters.Count}");
        Console.WriteLine($"filter={pipeline.Filters[0].Type}");
        filtered.Save(outputPath);
    }

API: :class:`Pixmap`, :class:`ImageFilterPipeline`, :meth:`ImageFilterPipeline.AddMedian`, :meth:`Pixmap.ApplyImageFilters`, :meth:`Pixmap.Save`.

Result of ``ImageFilters``:

.. literalinclude:: image-filters.txt
   :language: text

Source with speckle (left) and after ``AddMedian(3)`` (right):

.. list-table::
   :header-rows: 1
   :widths: 50 50

   * - image-filters.png
     - filtered.png
   * - .. image:: ../../images/examples-filters-source.png
          :alt: greyscale North Pier postcard with salt-and-pepper speckle
          :width: 240
     - .. image:: ../../images/examples-filters-filtered.png
          :alt: same postcard after a 3 by 3 median filter
          :width: 240


.. include:: ../../footer.rst
