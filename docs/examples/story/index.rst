.. include:: ../../header.rst

==============================
06-StoryHtmlBox
==============================

Lay out HTML into a page rectangle using :ref:`Story` / :meth:`Page.InsertHtmlBox`.

GitHub: `06-StoryHtmlBox <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/06-StoryHtmlBox>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

The method creates an empty PDF, opens a default page, and inserts HTML into ``Rect(72, 72, 520, 720)``. ``scaleLow: 0`` lets the Story engine shrink content so it fits the box.

.. code-block:: cs

    /// <summary>Lay out HTML into a page rectangle via Story / InsertHtmlBox</summary>
    static void StoryHtmlBox(string output)
    {
        // HTML content for the Story engine (CSS inline styles are supported).
        const string html = """
        <html><body style="font-family:sans-serif;font-size:14pt;color:#111;">
        <h1>MuPDF.NET</h1>
        <p>Story / <b>Page.InsertHtmlBox</b> example for customer samples.</p>
        <ul>
        <li>Headings and paragraphs</li>
        <li>Bold inline markup</li>
        <li>Simple lists</li>
        </ul>
        </body></html>
        """;

        // Create an empty PDF and a default A4-ish page.
        using (var doc = Document.Open())
        {
            using Page page = doc.NewPage();

            // Target rectangle in PDF points (72 pt = 1 inch).
            var rect = new Rect(72, 72, 520, 720);

            // scaleLow: 0 = allow shrinking so content fits the box.
            (float spare, float scale) = page.InsertHtmlBox(rect, html, scaleLow: 0f);
            Console.WriteLine($"spare={spare}");
            Console.WriteLine($"scale={scale}");

            doc.Save(output);
        }
    }

API: :meth:`Document.Document`, :meth:`Document.NewPage`, :meth:`Page.InsertHtmlBox`, :meth:`Document.Save`.

Result of ``InsertHtmlBox`` (remaining height below the content, and the chosen scale):

.. literalinclude:: story-info.txt
   :language: text

Result of ``story.pdf`` (page 1 rendered at 2×):

.. image:: ../../images/examples-story-htmlbox.png
   :alt: HTML laid out into a page rectangle with InsertHtmlBox
   :width: 420


.. include:: ../../footer.rst
