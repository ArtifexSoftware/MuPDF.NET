.. include:: ../header.rst

.. _ChunkedDocument:

================
ChunkedDocument
================


The result of :meth:`Document.ToChunks`: a document split into retrieval-ready chunks for RAG (retrieval-augmented generation) pipelines.

A *ChunkedDocument* is a read-only list of :ref:`Chunk <ChunkedDocument_Chunk>` objects, so it can be indexed and iterated directly. It also gives access to the layout elements the chunks were built from, and to separate views of the tables, figures and sections found in the document.

.. note::

   :meth:`Document.ToChunks` requires the layout pipeline. Switch it on with :meth:`Utils.UseLayout` (`true`) before calling it.

.. note::

   Page numbers in a *ChunkedDocument* (for example `ChunkMetadata.PageStart`, element IDs such as `p1.b0` and the first value of each bounding box) are **1-based**. The *pages* argument of :meth:`Document.ToChunks` is 0-based.


================================================ =======================================================
**Method / Attribute**                           **Short Description**
================================================ =======================================================
:meth:`ChunkedDocument.Get`                      Look up a chunk, table, figure, section or element by ID
:meth:`ChunkedDocument.ReassembleChunks`         Re-chunk with different settings, without re-parsing
:meth:`ChunkedDocument.ToDicts`                  Chunks as a list of dictionaries
:meth:`ChunkedDocument.ToJson`                   Chunks as a JSON string
:meth:`ChunkedDocument.ToString`                 Short summary of the counts
:attr:`ChunkedDocument.Chunks`                   The list of chunks
:attr:`ChunkedDocument.Count`                    Number of chunks
:attr:`ChunkedDocument.Diagnostics`              Counts and quality checks for the chunking run
:attr:`ChunkedDocument.Elements`                 All layout elements (boxes) of the document
:attr:`ChunkedDocument.Figures`                  All figures and formulas
:attr:`ChunkedDocument.Hierarchy`                Section headings as a tree
:attr:`ChunkedDocument.Params`                   The chunking parameters used
:attr:`ChunkedDocument.ReassemblyParams`         Parameter names :meth:`ChunkedDocument.ReassembleChunks` can change
:attr:`ChunkedDocument.Sections`                 All sections (heading spans)
:attr:`ChunkedDocument.Tables`                   All tables
:attr:`ChunkedDocument.Text`                     Text of all chunks joined together
================================================ =======================================================


**Class API**

.. class:: ChunkedDocument

  .. method:: ChunkedDocument(IEnumerable<Chunk> chunks, IEnumerable<ChunkElement> elements: null, IEnumerable<TableChunk> tables: null, IEnumerable<FigureChunk> figures: null, IEnumerable<SectionChunk> sections: null, Dictionary<string, object> parms: null, ParsedDocument doc: null)

    Creates a *ChunkedDocument* from existing parts. You do not normally need this: use :meth:`Document.ToChunks` instead.

    :arg IEnumerable<Chunk> chunks: the chunks.
    :arg IEnumerable<ChunkElement> elements: the layout elements.
    :arg IEnumerable<TableChunk> tables: the tables view.
    :arg IEnumerable<FigureChunk> figures: the figures view.
    :arg IEnumerable<SectionChunk> sections: the sections view.
    :arg Dictionary<string,object> parms: the chunking parameters, stored in :attr:`Params`.
    :arg ParsedDocument doc: the parsed layout document the chunks came from.

    .. note::

       A *ChunkedDocument* created this way cannot be re-chunked with :meth:`ChunkedDocument.ReassembleChunks`, because it does not hold the sentence data the chunker needs.

  .. method:: Get(string id, object defaultValue: null, bool throwIfMissing: true)

    Looks up any item by its ID. IDs have these forms:

    * `c0`, `c1`, ... : a :ref:`Chunk <ChunkedDocument_Chunk>`
    * `t0`, `t1`, ... : a :ref:`TableChunk <ChunkedDocument_TableChunk>`
    * `f0`, `f1`, ... : a :ref:`FigureChunk <ChunkedDocument_FigureChunk>`
    * `s0`, `s1`, ... : a :ref:`SectionChunk <ChunkedDocument_SectionChunk>`
    * `p{page}.b{box}`, for example `p1.b3` : a :ref:`ChunkElement <ChunkedDocument_ChunkElement>`

    :arg string id: the ID to look up.
    :arg object defaultValue: value to return if the ID is not found.
    :arg bool throwIfMissing: if `true` and *defaultValue* is `null`, a missing ID raises `KeyNotFoundException`. Set it to `false` to get `null` instead.

    :rtype: `object`
    :returns: the matching item. Cast it to the type that matches the ID.

    .. code-block:: cs

        var table = (TableChunk)chunks.Get("t0");
        var element = (ChunkElement)chunks.Get("p1.b3", throwIfMissing: false);

  .. method:: ReassembleChunks(int? maxTokens: null, int? minTokens: null, double? breakpointThreshold: null, string tableMode: null, bool? mergeSmallChunks: null, bool? respectSectionStarts: null)

    Builds a new *ChunkedDocument* with different chunking settings, reusing the already parsed document. This is much faster than calling :meth:`Document.ToChunks` again. Parameters left as `null` keep their current value from :attr:`Params`. See :meth:`Document.ToChunks` for what each parameter does.

    :arg int? maxTokens: maximum tokens per chunk.
    :arg int? minTokens: target minimum tokens per chunk.
    :arg double? breakpointThreshold: boundary score (0 to 1) above which a new chunk may start.
    :arg string tableMode: `"preserve"` or `"isolate"`.
    :arg bool? mergeSmallChunks: merge chunks smaller than *minTokens* into a neighbour.
    :arg bool? respectSectionStarts: always start a new chunk at a section heading.

    :rtype: :ref:`ChunkedDocument`

    .. code-block:: cs

        ChunkedDocument smaller = chunks.ReassembleChunks(maxTokens: 200);

  .. method:: ToDicts(bool includeTagged: true)

    Returns the chunks as a list of dictionaries, for example to store in a vector database. Each dictionary has the keys `"id"`, `"text"`, `"content_hash"`, `"metadata"` and, if *includeTagged* is `true`, `"tagged_content"`. The `"metadata"` dictionary holds the values of :ref:`ChunkMetadata <ChunkedDocument_ChunkMetadata>` with snake_case keys (`"page_start"`, `"section_path"`, `"token_count"` and so on).

    :arg bool includeTagged: include the :attr:`Chunk.TaggedContent` of each chunk.

    :rtype: `List<Dictionary<string, object>>`

  .. method:: ToJson(bool includeTagged: true)

    Returns the result of :meth:`ToDicts` as a JSON string.

    :arg bool includeTagged: include the :attr:`Chunk.TaggedContent` of each chunk.

    :rtype: `string`

  .. method:: ToString()

    Returns a short summary, for example `ChunkedDocument(chunks=12, tables=2, figures=1, sections=5)`.

    :rtype: `string`

  .. attribute:: Chunks

    The chunks, in document order. A *ChunkedDocument* can also be indexed and iterated directly, so `chunks[0]` is the same as `chunks.Chunks[0]`.

    :type: `IReadOnlyList<Chunk>`

  .. attribute:: Count

    Number of chunks.

    :type: `int`

  .. attribute:: Text

    The text of all chunks, separated by blank lines.

    :type: `string`

  .. attribute:: Elements

    All layout elements (text blocks, titles, tables, pictures and so on) of the parsed pages, including headers and footers.

    :type: `List<ChunkElement>`

  .. attribute:: Tables

    All tables found in the document.

    :type: `List<TableChunk>`

  .. attribute:: Figures

    All figures and formulas found in the document.

    :type: `List<FigureChunk>`

  .. attribute:: Sections

    All sections. A section starts at a heading and runs until the next heading of the same or a higher level.

    :type: `List<SectionChunk>`

  .. attribute:: Hierarchy

    The section headings as a tree. The root node has level 0 and an empty title; its children are the top-level sections.

    :type: `SectionNode`

  .. attribute:: Params

    The chunking parameters used to build this result, with snake_case keys such as `"max_tokens"` and `"table_mode"`.

    :type: `IReadOnlyDictionary<string, object>`

  .. attribute:: ReassemblyParams

    Static. The names of the parameters that :meth:`ChunkedDocument.ReassembleChunks` can change: ``max_tokens``, ``min_tokens``, ``breakpoint_threshold``, ``table_mode``, ``merge_small_chunks`` and ``respect_section_starts``.

    :type: `HashSet<string>`

  .. attribute:: Diagnostics

    Counts and quality checks for the chunking run, useful for spotting pages or content that did not make it into any chunk. The dictionary has these keys:

    * `"chunk_count"`, `"element_count"`, `"table_count"`, `"figure_count"`, `"section_count"`, `"page_count"`: `int` counts.
    * `"pages_without_chunks"`: `List<int>` of pages that are not covered by any chunk.
    * `"zero_chunk_causes"`: `List<string>`, only filled when there are no chunks at all. One of `"no_text_extracted"`, `"all_units_header_footer"` or `"all_units_filtered"`.
    * `"figures_without_text"`: `List<string>` of figure IDs with no OCR text.
    * `"degenerate_tables"`: `List<string>` of table IDs with no content.
    * `"header_footer_excluded"`: `int` number of header and footer sentences that were left out.

    :type: `Dictionary<string, object>`


**Example**

.. code-block:: cs

    Utils.UseLayout(true);
    Document doc = Document.Open("report.pdf");
    ChunkedDocument chunks = doc.ToChunks(maxTokens: 300);

    Console.WriteLine(chunks); // ChunkedDocument(chunks=..., tables=..., ...)

    foreach (Chunk chunk in chunks)
    {
        var meta = chunk.Metadata;
        Console.WriteLine($"{chunk.Id} pages {meta.PageStart}-{meta.PageEnd}, {meta.TokenCount} tokens");
        Console.WriteLine($"  section: {string.Join(" > ", meta.SectionPath)}");
    }

    File.WriteAllText("chunks.json", chunks.ToJson());


----


.. _ChunkedDocument_Chunk:

Chunk
---------

One retrieval-ready chunk of text.

.. class:: Chunk

  .. attribute:: Id

    The chunk ID: `c0`, `c1`, ...

    :type: `string`

  .. attribute:: Text

    The chunk text, in Markdown.

    :type: `string`

  .. attribute:: TaggedContent

    The chunk text with a context header in front of it, for embedding models that benefit from extra context. For example:

    .. code-block:: text

        [Section] Introduction > Scope
        [Page] 3
        [Type] table
        [Markdown]
        ...chunk text...

    :type: `string`

  .. attribute:: Metadata

    Where the chunk comes from and what it contains.

    :type: :ref:`ChunkMetadata <ChunkedDocument_ChunkMetadata>`

  .. attribute:: ContentHash

    SHA-256 hash (hex) of the chunk text with whitespace normalized. Use it to detect unchanged chunks when re-indexing a document.

    :type: `string`


.. _ChunkedDocument_ChunkMetadata:

ChunkMetadata
--------------

Metadata of a :ref:`Chunk <ChunkedDocument_Chunk>`.

.. class:: ChunkMetadata

  .. attribute:: PageStart

    First page of the chunk (1-based).

    :type: `int`

  .. attribute:: PageEnd

    Last page of the chunk (1-based).

    :type: `int`

  .. attribute:: Bboxes

    One bounding box per page the chunk covers, each as `[page, x0, y0, x1, y1]`.

    :type: `List<double[]>`

  .. attribute:: Types

    The kinds of content in the chunk, for example `"paragraph"`, `"table"` or `"list"`.

    :type: `List<string>`

  .. attribute:: SectionId

    ID of the innermost section containing the chunk, or `null`.

    :type: `string`

  .. attribute:: SectionPath

    Titles of the enclosing sections, from outermost to innermost.

    :type: `List<string>`

  .. attribute:: TokenCount

    Number of tokens in the chunk.

    :type: `int`

  .. attribute:: ElementIds

    IDs of the layout elements in the chunk (see :attr:`ChunkedDocument.Elements`).

    :type: `List<string>`

  .. attribute:: TableIds

    IDs of the tables in the chunk (see :attr:`ChunkedDocument.Tables`).

    :type: `List<string>`

  .. attribute:: FigureIds

    IDs of the figures in the chunk (see :attr:`ChunkedDocument.Figures`).

    :type: `List<string>`

  .. attribute:: Lists

    The lists in the chunk. Each entry is a dictionary with `"items"` (a list of dictionaries with `"text"`, `"page"` and `"bbox"`) and `"bboxes"` (one `[page, x0, y0, x1, y1]` box per page).

    :type: `List<Dictionary<string, object>>`

  .. attribute:: Ocr

    `true` if any part of the chunk comes from an OCR'd page.

    :type: `bool`

  .. attribute:: FilePath

    File name of the source document.

    :type: `string`

  .. attribute:: PageCount

    Number of pages in the source document.

    :type: `int?`


.. _ChunkedDocument_TableChunk:

TableChunk
-----------

A table, as listed in :attr:`ChunkedDocument.Tables`.

.. class:: TableChunk

  .. attribute:: Id

    The table ID: `t0`, `t1`, ...

    :type: `string`

  .. attribute:: ChunkId

    ID of the chunk that contains the table.

    :type: `string`

  .. attribute:: ElementId

    ID of the layout element of the table.

    :type: `string`

  .. attribute:: Page

    Page number of the table (1-based).

    :type: `int`

  .. attribute:: Bbox

    Bounding box of the table, as `[x0, y0, x1, y1]`.

    :type: `double[]`

  .. attribute:: Markdown

    The table as Markdown.

    :type: `string`

  .. attribute:: Html

    The table as HTML, when `tableOutput: "html"` was used.

    :type: `string`

  .. attribute:: Headers

    The column headers.

    :type: `List<string>`

  .. attribute:: Caption

    The table caption, if one was found.

    :type: `string`

  .. attribute:: CaptionElementId

    ID of the layout element of the caption.

    :type: `string`

  .. attribute:: SectionId

    ID of the section containing the table.

    :type: `string`

  .. attribute:: TokenCount

    Number of tokens in the table.

    :type: `int`

  .. attribute:: Text

    The table content: :attr:`Html` if set, otherwise :attr:`Markdown`.

    :type: `string`


.. _ChunkedDocument_FigureChunk:

FigureChunk
------------

A figure or formula, as listed in :attr:`ChunkedDocument.Figures`.

.. class:: FigureChunk

  .. attribute:: Id

    The figure ID: `f0`, `f1`, ...

    :type: `string`

  .. attribute:: ChunkId

    ID of the chunk that contains the figure.

    :type: `string`

  .. attribute:: ElementId

    ID of the layout element of the figure.

    :type: `string`

  .. attribute:: Page

    Page number of the figure (1-based).

    :type: `int`

  .. attribute:: Bbox

    Bounding box of the figure, as `[x0, y0, x1, y1]`.

    :type: `double[]`

  .. attribute:: BoxClass

    The layout class of the figure, for example `"picture"` or `"formula"`.

    :type: `string`

  .. attribute:: OcrText

    Text recognized inside the figure, if any.

    :type: `string`

  .. attribute:: Placeholder

    The placeholder text that stands for the figure in the chunk text.

    :type: `string`

  .. attribute:: Caption

    The figure caption, if one was found.

    :type: `string`

  .. attribute:: CaptionElementId

    ID of the layout element of the caption.

    :type: `string`

  .. attribute:: SectionId

    ID of the section containing the figure.

    :type: `string`

  .. attribute:: Image

    The image bytes, when `extractImages: true` was passed to :meth:`Document.ToChunks`. Otherwise `null`.

    :type: `byte[]`

  .. attribute:: HasText

    `true` if :attr:`OcrText` is not empty.

    :type: `bool`


.. _ChunkedDocument_SectionChunk:

SectionChunk
-------------

A section, as listed in :attr:`ChunkedDocument.Sections`. A section starts at a heading and runs until the next heading of the same or a higher level.

.. class:: SectionChunk

  .. attribute:: Id

    The section ID: `s0`, `s1`, ...

    :type: `string`

  .. attribute:: Title

    The heading text.

    :type: `string`

  .. attribute:: Level

    The heading level, 1 being the top level.

    :type: `int`

  .. attribute:: PageStart

    First page of the section (1-based).

    :type: `int`

  .. attribute:: PageEnd

    Last page of the section (1-based).

    :type: `int`

  .. attribute:: HeadingElementId

    ID of the layout element of the heading.

    :type: `string`

  .. attribute:: Path

    Titles of this section and its enclosing sections, from outermost to innermost.

    :type: `List<string>`

  .. attribute:: ElementSpan

    The range of :attr:`ChunkedDocument.Elements` covered by the section, as `[start, end]` with *end* exclusive.

    :type: `int[]`

  .. attribute:: ChildChunkIds

    IDs of the chunks in the section.

    :type: `List<string>`

  .. attribute:: TokenCount

    Number of tokens in the section.

    :type: `int`

  .. attribute:: Text

    The text of all elements in the section, without headers and footers.

    :type: `string`


.. _ChunkedDocument_SectionNode:

SectionNode
------------

A node of :attr:`ChunkedDocument.Hierarchy`.

.. class:: SectionNode

  .. attribute:: Title

    The heading text. Empty for the root node.

    :type: `string`

  .. attribute:: Level

    The heading level. 0 for the root node.

    :type: `int`

  .. attribute:: SectionId

    ID of the matching :ref:`SectionChunk <ChunkedDocument_SectionChunk>`. `null` for the root node.

    :type: `string`

  .. attribute:: Children

    The sub-sections.

    :type: `List<SectionNode>`


.. _ChunkedDocument_ChunkElement:

ChunkElement
-------------

One layout element (box) of a page, as listed in :attr:`ChunkedDocument.Elements`.

.. class:: ChunkElement

  .. attribute:: Id

    The element ID, as `p{page}.b{box}`, for example `p1.b3`.

    :type: `string`

  .. attribute:: Page

    Page number (1-based).

    :type: `int`

  .. attribute:: Box

    Index of the box on its page (0-based).

    :type: `int`

  .. attribute:: BoxClass

    The layout class of the box, for example `"text"`, `"title"`, `"section-header"`, `"list-item"`, `"table"`, `"picture"`, `"formula"`, `"caption"`, `"footnote"`, `"page-header"` or `"page-footer"`.

    :type: `string`

  .. attribute:: Bbox

    Bounding box of the element, as `[x0, y0, x1, y1]`.

    :type: `double[]`

  .. attribute:: Text

    The text of the element.

    :type: `string`

  .. attribute:: IsHeaderFooter

    `true` if the element is a page header or footer.

    :type: `bool`


.. include:: ../footer.rst
