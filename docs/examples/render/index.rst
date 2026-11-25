.. include:: ../../header.rst

==============================
03-RenderPixmap
==============================

Render page 1 of a PDF to a PNG pixmap at 2× zoom.

GitHub: `03-RenderPixmap <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/03-RenderPixmap>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``color-shapes.pdf`` is a 2-page fixture of color gradients and meshes. This method renders page 0 (the first page). ``Matrix(2, 2)`` is about 144 dpi from a 72 dpi page.

.. code-block:: cs

    /// <summary>Render page 1 to a PNG pixmap at 2× zoom</summary>
    static void RenderPixmap(string input, string output)
    {
        // Open document and first page.
        using (var doc = Document.Open(input))
        using (Page page = doc[0])
        // Matrix(2,2) = 2× resolution (about 144 dpi from a 72 dpi page).
        using (Pixmap pix = page.GetPixmap(matrix: new Matrix(2, 2)))
        {
            // Save the rendered bitmap as PNG.
            pix.Save(output);
        }
    }

API: :meth:`Document.Document`, :meth:`Page.GetPixmap`, :meth:`Pixmap.Save`.

Result of ``page-1.png``:

.. image:: ../../images/examples-render-page-1.png
   :alt: Page 1 of color-shapes.pdf rendered at 2× zoom
   :width: 420


.. include:: ../../footer.rst
