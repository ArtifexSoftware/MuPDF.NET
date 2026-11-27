.. include:: ../../header.rst

==============================
30-FileAnnot
==============================

Attach a file as a page annotation with :meth:`Page.AddFileAnnot`. This is a paperclip on the page, not a document-level EmbeddedFiles name-tree entry.

GitHub: `30-FileAnnot <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/30-FileAnnot>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``file-annot.pdf`` is a one-page host. ``spare-list.txt`` is a 50-byte payload. The method places a ``Paperclip`` icon at ``Point(448, 16)``, stores the bytes on that annotation, then reopens the PDF and reads them back with :meth:`Annot.GetFile`.

To **embed a file in the document name tree** instead, see :doc:`13-EmbeddedFiles </examples/embedded/index>`. For **text / rect / redact** annotations, see :doc:`07-AnnotationsRedact </examples/annotations/index>`.

.. code-block:: cs

    /// <summary>Attach a file as a page annotation (paperclip), distinct from EmbeddedFiles</summary>
    static void FileAnnot(string inputPath, string notePath, string outputPath)
    {
        byte[] payload = File.ReadAllBytes(notePath);

        using (var doc = Document.Open(inputPath))
        using (Page page = doc[0])
        {
            // Page-level file attachment annotation (vs Document.AddEmbeddedFile).
            Annot annot = page.AddFileAnnot(
                point: new Point(448, 16),
                buffer_: payload,
                filename: "spare-list.txt",
                uFileName: "spare-list.txt",
                desc: "Harbor spare list",
                icon: "Paperclip");
            annot.Update();

            var fileInfo = annot.GetFileInfo();
            Console.WriteLine($"annotType={annot.TypeString}");
            Console.WriteLine($"filename={fileInfo["filename"]}");
            Console.WriteLine($"desc={fileInfo["desc"]}");
            Console.WriteLine($"size={payload.Length}");
            doc.Save(outputPath, garbage: 3, deflate: 1);
        }

        using (var doc = Document.Open(outputPath))
        using (Page page = doc[0])
        {
            var annots = page.Annots().ToList();
            Console.WriteLine($"annotCount={annots.Count}");
            Console.WriteLine($"type={annots[0].TypeString}");
            byte[] extracted = annots[0].GetFile();
            Console.WriteLine($"extractedSize={extracted.Length}");
            Console.WriteLine($"roundtrip={extracted.SequenceEqual(payload)}");
        }
    }

API: :meth:`Document.Open`, :meth:`Page.AddFileAnnot`, :attr:`Annot.FileInfo` (also GetFileInfo), :meth:`Annot.GetFile`, :meth:`Page.Annots`, :meth:`Document.Save`.

Result of ``FileAnnot``:

.. literalinclude:: file-annot.txt
   :language: text

Host page before the annotation, then with the paperclip. ``GetFile`` returns the same 50 bytes:

.. list-table::
   :header-rows: 1
   :widths: 50 50

   * - file-annot.pdf
     - with-file-annot.pdf
   * - .. image:: ../../images/examples-fileannot-before.png
          :alt: Harbor minutes without a file annotation
          :width: 240
     - .. image:: ../../images/examples-fileannot-after.png
          :alt: Harbor minutes with a paperclip file annotation
          :width: 240


.. include:: ../../footer.rst
