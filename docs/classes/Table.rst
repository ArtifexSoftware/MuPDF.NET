.. include:: ../header.rst

.. _Table:

================
Table
================


A single table found on a PDF page by :meth:`Page.FindTables` or :meth:`Page.GetTables`.


================================================ =======================================================
**Method / Attribute**                           **Short Description**
================================================ =======================================================
:meth:`Table.Extract`                            Cell text row by row
:meth:`Table.ToMarkdown`                         GitHub-flavoured Markdown for this table
:meth:`Table.ToHtml`                             HTML ``<table>`` for this table
:attr:`Table.Bbox`                               Bounding box of the table
:attr:`Table.Cells`                              Cell bounding boxes
:attr:`Table.ColCount`                           Number of columns
:attr:`Table.Header`                             Detected header
:attr:`Table.HeaderRows`                         Leading header-row count (refine)
:attr:`Table.Placements`                         Span placements from refine
:attr:`Table.RowCount`                           Number of rows
:attr:`Table.Rows`                               Rows of cell boxes
:attr:`Table.SectionRows`                        Section-label row indexes (refine)
================================================ =======================================================


**Class API**

.. class:: Table

  .. method:: Extract(Dictionary<string, object> kwargs: null)

     Extract cell text row by row.

     :arg Dictionary<string, object> kwargs: optional extract options passed per cell (for example ``x_shift``, ``y_shift``, ``layout``).

     :rtype: List<List<string>>
     :returns: one inner list per row. A spanned cell may be ``null``.

  .. method:: ToMarkdown(bool clean: false, bool fillEmpty: true)

     GitHub-flavoured Markdown for this table.

     :arg bool clean: when ``true``, escape Markdown-sensitive characters in cells and header.
     :arg bool fillEmpty: when ``true``, copy content into null cells from left/top to approximate spans.

     :rtype: string

  .. method:: ToHtml()

     HTML ``<table>`` string. With span placements from :meth:`Page.FindTables` (``refine: true``), colspan / rowspan and header tags are honoured. Otherwise a flat table is built from :meth:`Table.Extract`.

     :rtype: string

  .. attribute:: Cells

     Cell bounding boxes as ``(x0, y0, x1, y1)``. ``y0`` is top, ``y1`` is bottom.

     :type: List<(float x0, float y0, float x1, float y1)>

  .. attribute:: Bbox

     Bounding box of the entire table as ``(x0, y0, x1, y1)``.

     :type: (float x0, float y0, float x1, float y1)

  .. attribute:: Rows

     Table rows. Each :class:`TableRow` holds the cell boxes for that row.

     :type: List<TableRow>

  .. attribute:: RowCount

     Number of rows.

     :type: int

  .. attribute:: ColCount

     Number of columns (widest row).

     :type: int

  .. attribute:: Header

     Detected header for this table.

     :type: :class:`TableHeader`

  .. attribute:: HeaderRows

     Leading header-row count from :meth:`Page.FindTables` with ``refine: true``.

     :type: int

  .. attribute:: SectionRows

     Section-label row indexes from refine.

     :type: IReadOnlyList<int>

  .. attribute:: Placements

     Tagged span placements from :meth:`Page.FindTables` with ``refine: true``; ``null`` on the default path.

     :type: List<List<SpanCell>>


.. _TableHeader:

TableHeader
================

Detected table header.

================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`TableHeader.Bbox`                         Header bounding box
:attr:`TableHeader.Cells`                        Per-column header cell boxes
:attr:`TableHeader.External`                     ``true`` when the header is outside the table cells
:attr:`TableHeader.Names`                        Column header text
================================================ =======================================================


**Class API**

.. class:: TableHeader

  .. attribute:: Bbox

     Header bounding box as ``(x0, y0, x1, y1)``.

     :type: (float x0, float y0, float x1, float y1)

  .. attribute:: Cells

     Per-column header cell boxes. ``null`` means spanned.

     :type: List<(float x0, float y0, float x1, float y1)?>

  .. attribute:: Names

     Column header text.

     :type: List<string>

  .. attribute:: External

     ``true`` when the header is outside the table cells; ``false`` when the first table row is the header.

     :type: bool


.. _TableRow:

TableRow
================

One row of a :class:`Table`.

================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`TableRow.Bbox`                            Bounding box of the row
:attr:`TableRow.Cells`                           Cell boxes in this row
================================================ =======================================================


**Class API**

.. class:: TableRow

  .. attribute:: Cells

     Individual cell bounding boxes. A ``null`` entry means the cell is spanned by a neighbour.

     :type: List<(float x0, float y0, float x1, float y1)?>

  .. attribute:: Bbox

     Bounding box enclosing all non-null cells as ``(x0, y0, x1, y1)``.

     :type: (float x0, float y0, float x1, float y1)


.. include:: ../footer.rst
