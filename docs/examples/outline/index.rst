.. include:: ../../header.rst

==============================
10-OutlineLinks
==============================

Build a table of contents (bookmarks) and an internal go-to link with :meth:`Document.SetToc` and :meth:`Page.InsertLink`.

GitHub: `10-OutlineLinks <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/10-OutlineLinks>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

The method creates a two-page PDF. Page 1 has a blue rectangle that jumps to page 2. ``SetToc`` writes bookmarks (page numbers are 1-based). ``InsertLink`` uses a 0-based destination page.

.. code-block:: cs

    /// <summary>Build a TOC (bookmarks) and an internal go-to link</summary>
    static void OutlineLinks(string outputPath)
    {
        using (var doc = Document.Open())
        {
            // Page 1: heading + blue hot-spot rectangle (link target area).
            using (Page p0 = doc.NewPage())
            {
                var linkFrom = new Rect(72, 200, 200, 240);
                var writer = new TextWriter(p0.Rect);
                writer.FillTextbox(new Rect(72, 72, 500, 120), "Chapter 1 — Overview", new Font(fontName: "helv"));
                writer.FillTextbox(linkFrom, "Go to Chapter 2", new Font(fontName: "helv"), fontSize: 11);
                writer.WriteText(p0);
                p0.DrawRect(linkFrom, color: new[] { 0f, 0f, 1f });
            }

            // Page 2: second chapter.
            using (Page p1 = doc.NewPage())
            {
                var writer = new TextWriter(p1.Rect);
                writer.FillTextbox(new Rect(72, 72, 500, 120), "Chapter 2 — Details", new Font(fontName: "helv"));
                writer.WriteText(p1);
                p1.DrawRect(new Rect(72, 200, 200, 240), color: new[] { 0f, 0.5f, 0f });
            }

            // TOC rows: [level, title, 1-based page]. Level 2 nests under the previous level 1.
            doc.SetToc(new List<object>
            {
                new List<object> { 1, "Overview", 1 },
                new List<object> { 1, "Details", 2 },
                new List<object> { 2, "Details subsection", 2 },
            });

            // Internal link: click the blue rect on page 1 → jump to page 2 (0-based page index).
            using (Page p0 = doc[0])
            {
                p0.InsertLink(new Dictionary<string, object>
                {
                    ["kind"] = Constants.LinkGoto,
                    ["from"] = new Rect(72, 200, 200, 240),
                    ["page"] = 1,
                    ["to"] = new Point(72, 72),
                });
            }

            List<Toc> toc = doc.GetToc();
            foreach (Toc item in toc)
                Console.WriteLine($"{item.Level}\t{item.Title}\t{item.Page}");
            Console.WriteLine("---links---");
            for (int i = 0; i < doc.PageCount; i++)
            {
                using Page page = doc[i];
                foreach (LinkInfo link in page.GetLinks().OrderBy(l => l.From?.Y0 ?? 0).ThenBy(l => l.From?.X0 ?? 0))
                {
                    Rect r = link.From;
                    string from = string.Create(CultureInfo.InvariantCulture,
                        $"{r.X0:0.##},{r.Y0:0.##},{r.X1:0.##},{r.Y1:0.##}");
                    Console.WriteLine($"p{i + 1}\t{(int)link.Kind}\t{link.Page}\t{from}");
                }
            }

            doc.Save(outputPath);
        }
    }

API: :meth:`Document.Open`, :meth:`Document.NewPage`, :meth:`TextWriter.FillTextbox`, :meth:`TextWriter.WriteText`, :meth:`Page.DrawRect`, :meth:`Document.SetToc`, :meth:`Page.InsertLink`, :data:`LINK_GOTO` (also LinkGoto), :meth:`Document.GetToc`, :meth:`Page.GetLinks`, :meth:`Document.Save`.

Result of ``Document.GetToc`` and ``Page.GetLinks``:

.. literalinclude:: outline-links.txt
   :language: text

Result of ``outline-links.pdf`` (page 1 rendered at 2×):

.. image:: ../../images/examples-outline-links.png
   :alt: Page 1 with a blue go-to-Chapter-2 link rectangle
   :width: 420


.. include:: ../../footer.rst
