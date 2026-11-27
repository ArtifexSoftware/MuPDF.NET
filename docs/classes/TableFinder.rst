.. include:: ../header.rst

.. _TableFinder:

================
TableFinder
================


Result of :meth:`Page.FindTables`: the tables detected on a page, plus the settings and text page used for detection.


================================================ =======================================================
**Method / Attribute**                           **Short Description**
================================================ =======================================================
:meth:`TableFinder.TableFinder`                  Create a finder and detect tables on a page
:meth:`TableFinder.FindTables`                   Detect tables on a page (static)
:attr:`TableFinder.Page`                         The page being analysed
:attr:`TableFinder.Settings`                     Settings used during detection
:attr:`TableFinder.Tables`                       Detected tables
:attr:`TableFinder.TextPage`                     Text page used by table detection
================================================ =======================================================


**Class API**

.. class:: TableFinder

  .. method:: TableFinder(Page page)
              TableFinder(Page page, TableSettings settings: null)

     Create a :class:`TableFinder` and detect tables on *page*. ``null`` *settings* uses defaults via :meth:`TableSettings.Resolve`.

     :arg Page page: the page to analyse.
     :arg TableSettings settings: table-finder settings. ``null`` uses defaults.

  .. method:: FindTables(Page page, TableSettings settings: null)

     Static entry that calls :meth:`Page.FindTables`.

     :arg Page page: the page to analyse.
     :arg TableSettings settings: table-finder settings. ``null`` uses defaults.

     :rtype: :class:`TableFinder`

  .. attribute:: Page

     The page being analysed.

     :type: :ref:`Page`

  .. attribute:: Tables

     Detected tables, sorted top-to-bottom then left-to-right. Index with ``finder[i]`` (negative indexes count from the end).

     :type: List<Table>

  .. attribute:: Settings

     Active settings used during detection.

     :type: :class:`TableSettings`

  .. attribute:: TextPage

     Text page used by table detection.

     :type: :ref:`TextPage`


.. include:: ../footer.rst
