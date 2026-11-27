.. include:: ../../header.rst

==============================
22-GetDrawings
==============================

Draw vector shapes, then extract path info with :meth:`Page.GetDrawings`.

GitHub: `22-GetDrawings <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/22-GetDrawings>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

The method creates a 540×320 page with a dashed line, a stroked blue rectangle, and a filled green circle. ``GetDrawings`` returns one ``PathInfo`` per path: ``type`` is ``s`` (stroke) or ``fs`` (fill and stroke), and ``items`` lists the draw commands (``l`` line, ``re`` rectangle, ``c`` cubic Bézier).

``PathInfo.Rect`` is the path bounding box. A horizontal or vertical stroke has zero width or height, so ``Rect.IsEmpty`` is true — the sample draws that bbox as a line instead of skipping it.

.. code-block:: cs

    /// <summary>Draw vector shapes, then extract path info with Page.GetDrawings</summary>
    static void GetDrawings(string drawnPath, string redrawnPath)
    {
        using (var doc = Document.Open())
        {
            using Page page = doc.NewPage(width: 540, height: 320);
            page.DrawLine(new Point(72, 100), new Point(468, 100), width: 1f, dashes: "[5] 0");
            page.DrawRect(new Rect(72, 180, 220, 280), color: new[] { 0f, 0f, 1f }, width: 1.5f);
            page.DrawCircle(
                new Point(350, 230),
                radius: 40,
                color: new[] { 0f, 0f, 0f },
                fill: new[] { 0f, 0.6f, 0f },
                width: 1f);
            doc.Save(drawnPath);
        }

        using (var src = Document.Open(drawnPath))
        using (Page page = src[0])
        {
            List<PathInfo> paths = page.GetDrawings();
            Console.WriteLine($"pathCount={paths.Count}");
            foreach (PathInfo path in paths)
            {
                string itemTypes = path.Items == null
                    ? ""
                    : string.Join(",", path.Items.Select(i => i.Type));
                string rect = path.Rect == null
                    ? "none"
                    : $"{path.Rect.X0:0},{path.Rect.Y0:0},{path.Rect.X1:0},{path.Rect.Y1:0}";
                Console.WriteLine($"seq={path.SeqNo} type={path.Type} items={itemTypes} rect={rect}");
            }

            using var outDoc = Document.Open();
            using Page outPage = outDoc.NewPage(width: 540, height: 320);
            int redrawnRects = 0;
            foreach (PathInfo path in paths)
            {
                Rect box = path.Rect;
                if (box == null)
                    continue;
                // A horizontal/vertical stroke has a degenerate bbox (IsEmpty). Draw that
                // segment instead of skipping it so every GetDrawings path is visible.
                if (box.IsEmpty)
                    outPage.DrawLine(
                        new Point(box.X0, box.Y0),
                        new Point(box.X1, box.Y1),
                        color: new[] { 1f, 0f, 0f },
                        width: 0.5f);
                else
                    outPage.DrawRect(box, color: new[] { 1f, 0f, 0f }, width: 0.5f);
                redrawnRects++;
            }
            Console.WriteLine($"redrawnRects={redrawnRects}");
            outDoc.Save(redrawnPath);
        }
    }

API: :meth:`Document.Open`, :meth:`Document.NewPage`, :meth:`Page.DrawLine`, :meth:`Page.DrawRect`, :meth:`Page.DrawCircle`, :meth:`Page.GetDrawings`, :meth:`Document.Save`.

Result of ``GetDrawings``:

.. literalinclude:: drawings.txt
   :language: text

Drawn page (left) and path bounding boxes from ``GetDrawings`` (right):

.. list-table::
   :header-rows: 1
   :widths: 50 50

   * - drawn.pdf
     - redrawn.pdf
   * - .. image:: ../../images/examples-drawings-drawn.png
          :alt: dashed line, blue rectangle, filled green circle
          :width: 360
     - .. image:: ../../images/examples-drawings-redrawn.png
          :alt: red bounding boxes for each GetDrawings path
          :width: 360


.. include:: ../../footer.rst
