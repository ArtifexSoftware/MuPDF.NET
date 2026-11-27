.. include:: ../../header.rst

==============================
23-RotateCrop
==============================

Rotate a page with :meth:`Page.SetRotation` and crop it with :meth:`Page.SetCropBox`.

GitHub: `23-RotateCrop <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/23-RotateCrop>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``rotate-crop.pdf`` is a 360×480 portrait page with four labeled color quadrants and a TOP marker. ``SetRotation(90)`` is PDF ``/Rotate`` clockwise, so the page rectangle becomes 480×360 and TOP points right. ``SetCropBox`` uses **unrotated** coordinates: the upper-left half of ``page.Rect`` is the red TL quadrant.

The crop opens the original file again, so rotation and crop are independent.

.. code-block:: cs

    /// <summary>Rotate page 1 and set a CropBox, then save</summary>
    static void RotateCrop(string inputPath, string rotatedPath, string croppedPath)
    {
        static string Box(Rect r) =>
            r == null ? "null" : $"{r.X0:0},{r.Y0:0},{r.X1:0},{r.Y1:0}";

        using (var doc = Document.Open(inputPath))
        using (Page page = doc[0])
        {
            Console.WriteLine($"before.rotation={page.Rotation}");
            Console.WriteLine($"before.rect={Box(page.Rect)}");
            Console.WriteLine($"before.cropbox={Box(page.CropBox)}");

            // Rotate clockwise 90 degrees (PDF /Rotate).
            page.SetRotation(90);
            Console.WriteLine($"after.rotation={page.Rotation}");
            Console.WriteLine($"after.rect={Box(page.Rect)}");
            doc.Save(rotatedPath);
        }

        using (var doc = Document.Open(inputPath))
        using (Page page = doc[0])
        {
            // CropBox is unrotated. Upper-left of page.Rect is the TL quadrant.
            Rect media = page.Rect;
            var crop = new Rect(
                media.X0,
                media.Y0,
                media.X0 + media.Width / 2f,
                media.Y0 + media.Height / 2f);
            Console.WriteLine($"crop.rect={Box(crop)}");
            page.SetCropBox(crop);
            doc.Save(croppedPath);
        }

        using (var doc = Document.Open(croppedPath))
        using (Page page = doc[0])
            Console.WriteLine($"after.cropbox={Box(page.CropBox)}");
    }

API: :meth:`Document.Open`, :meth:`Page.SetRotation`, :meth:`Page.SetCropBox`, :meth:`Document.Save`.

Result of ``RotateCrop``:

.. literalinclude:: rotate-crop.txt
   :language: text

Original, rotated 90° clockwise, and cropped to the TL quadrant:

.. list-table::
   :header-rows: 1
   :widths: 34 33 33

   * - rotate-crop.pdf
     - rotated.pdf
     - cropped.pdf
   * - .. image:: ../../images/examples-rotate-original.png
          :alt: four labeled quadrants with TOP marker
          :width: 160
     - .. image:: ../../images/examples-rotate-rotated.png
          :alt: same page after SetRotation 90
          :width: 200
     - .. image:: ../../images/examples-rotate-cropped.png
          :alt: TL quadrant after SetCropBox
          :width: 120


.. include:: ../../footer.rst
