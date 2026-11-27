.. include:: ../../header.rst

==============================
13-EmbeddedFiles
==============================

Attach a file to a PDF and list embedded attachments with :meth:`Document.AddEmbeddedFile` (also AddEmbfile) and :meth:`Document.GetEmbfileNames` (also GetEmbeddedFileNames).

GitHub: `13-EmbeddedFiles <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/13-EmbeddedFiles>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

``Blank.pdf`` is the host document. ``note.txt`` is a 42-byte payload. ``AddEmbeddedFile`` stores the bytes in the PDF EmbeddedFiles name tree. The listing is name, filename, size, and SHA-256 of the extracted bytes.

To **attach a file as a page annotation** (paperclip) instead, see :doc:`30-FileAnnot </examples/fileannot/index>`.

.. code-block:: cs

    /// <summary>Attach a file to a PDF and list embedded attachments</summary>
    static void EmbeddedFiles(string blankPath, string notePath, string outputPath)
    {
        byte[] payload = File.ReadAllBytes(notePath);
        const string embName = "note.txt";

        using (var doc = Document.Open(blankPath))
        {
            // Replace any prior attachment with the same logical name.
            if (doc.GetEmbeddedFileNames().Contains(embName))
                doc.DeleteEmbeddedFile(embName);

            // AddEmbeddedFile stores bytes in the PDF EmbeddedFiles name tree.
            doc.AddEmbeddedFile(
                name: embName,
                buffer: payload,
                filename: embName,
                uFileName: embName,
                desc: "Example attachment");

            using (Page page = doc[0])
            {
                var writer = new TextWriter(page.Rect);
                writer.FillTextbox(
                    new Rect(72, 72, 500, 120),
                    "Embedded file: note.txt",
                    new Font(fontName: "helv"),
                    fontSize: 14);
                writer.WriteText(page);
            }

            doc.Save(outputPath, garbage: 4, deflate: 1);
        }

        using (var doc = Document.Open(outputPath))
        {
            var names = doc.GetEmbeddedFileNames();
            Console.WriteLine($"embeddedCount={names.Count}");
            foreach (string name in names.OrderBy(n => n, StringComparer.Ordinal))
            {
                var info = doc.GetEmbeddedFileInfo(name);
                byte[] data = doc.GetEmbeddedFile(name);
                string sha = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
                Console.WriteLine($"{name}\t{info["filename"]}\t{data.Length}\t{sha}");
            }
        }
    }

API: :meth:`Document.Open`, :meth:`Document.AddEmbeddedFile` (also AddEmbfile), :meth:`Document.DeleteEmbfile` (also DeleteEmbeddedFile), :meth:`Document.GetEmbfileNames` (also GetEmbeddedFileNames), :meth:`Document.GetEmbfileInfo` (also GetEmbeddedFileInfo), :meth:`Document.GetEmbfile` (also GetEmbeddedFile), :meth:`Document.Save`.

Result of ``Document.GetEmbeddedFileNames`` / ``GetEmbeddedFile``:

.. literalinclude:: embedded.txt
   :language: text

Result of ``with-attachment.pdf`` (page 1 rendered at 2×):

.. image:: ../../images/examples-embedded-files.png
   :alt: Blank.pdf host page noting the embedded note.txt attachment
   :width: 420


.. include:: ../../footer.rst
