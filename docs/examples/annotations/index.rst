.. include:: ../../header.rst

==============================
07-AnnotationsRedact
==============================

Add a text note, a rectangle annotation, and apply a redaction.

GitHub: `07-AnnotationsRedact <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/07-AnnotationsRedact>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``Blank.pdf`` is a one-page empty fixture. The method adds a sticky-note and a red rectangle, then applies a redaction (the redaction annotation is consumed; the text and rect annotations remain).

.. code-block:: cs

    /// <summary>Add text/rect annotations and apply a redaction</summary>
    static void AnnotationsRedact(string inputPath, string outputPath)
    {
        using (var doc = Document.Open(inputPath))
        using (Page page = doc[0])
        {
            var rect = new Rect(72, 72, 300, 120);

            // Sticky-note style text annotation.
            Annot text = page.AddTextAnnot(new Point(rect.X0, rect.Y0), "Example note");
            text.SetInfo(
                content: "Hello from 07-AnnotationsRedact",
                title: "MuPDF.NET.Examples");
            text.Update(); // rebuild appearance stream

            // Rectangle markup annotation (red stroke).
            Annot box = page.AddRectAnnot(rect);
            box.SetColors(stroke: new[] { 1f, 0f, 0f });
            box.Update();

            // Redaction: mark area, then ApplyRedactions() permanently removes content.
            Annot redact = page.AddRedactAnnot(new Rect(72, 200, 250, 230), text: "REDACTED");
            redact.Update();
            page.ApplyRedactions();

            int annotCount = page.Annots().Count();
            Console.WriteLine($"annotCount={annotCount}");
            doc.Save(outputPath);
        }
    }

API: :meth:`Document.Open`, :meth:`Page.AddTextAnnot`, :meth:`Annot.SetInfo`, :meth:`Page.AddRectAnnot`, :meth:`Annot.SetColors`, :meth:`Annot.Update`, :meth:`Page.AddRedactAnnot`, :meth:`Page.ApplyRedactions`, :meth:`Document.Save`.

Result of ``page.Annots()`` after ``ApplyRedactions`` (the redaction is gone; the note and rectangle remain):

.. literalinclude:: annot-info.txt
   :language: text

Result of ``annotated.pdf`` (page 1 rendered at 2×):

.. image:: ../../images/examples-annotations-annotated.png
   :alt: Blank.pdf after a text note, a red rectangle annotation, and an applied redaction
   :width: 420


.. include:: ../../footer.rst
