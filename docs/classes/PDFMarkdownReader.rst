.. include:: ../header.rst

.. _PDFMarkdownReader:

=======================
PDFMarkdownReader
=======================

A LlamaIndex-compatible reader. Create one with :meth:`Utils.LlamaMarkdownReader`, then call :meth:`PDFMarkdownReader.LoadData` to get one :class:`LlamaIndexDocument` per page.


================================================ =======================================================
**Method / Attribute**                           **Short Description**
================================================ =======================================================
:meth:`PDFMarkdownReader.LoadData`               Load one :class:`LlamaIndexDocument` per page
:attr:`LlamaIndexDocument.Text`                  That page's Markdown
:attr:`LlamaIndexDocument.ExtraInfo`             Page metadata
================================================ =======================================================


**Class API**

.. class:: PDFMarkdownReader

  .. method:: PDFMarkdownReader(Func<Dictionary<string, object>, Dictionary<string, object>> metaFilter: null)

     Create a reader. Prefer :meth:`Utils.LlamaMarkdownReader`.

     :arg metaFilter: optional callback that transforms per-page metadata before documents are returned.

  .. method:: LoadData(object filePath, object extraInfo: null, Dictionary<string, object> loadKwargs: null)

     Load a supported document and return one :class:`LlamaIndexDocument` per page.

     :arg object filePath: path to the document.
     :arg object extraInfo: optional dictionary merged into each page's :attr:`LlamaIndexDocument.ExtraInfo`.
     :arg Dictionary<string, object> loadKwargs: optional arguments forwarded to the Markdown extractor.

     :rtype: List<LlamaIndexDocument>
     :returns: one document per page. :attr:`LlamaIndexDocument.Text` is that page's Markdown. :attr:`LlamaIndexDocument.ExtraInfo` includes ``page`` (1-based), ``total_pages``, ``file_path``, and PDF metadata.


.. class:: LlamaIndexDocument

  .. attribute:: Text

     That page's Markdown.

     :type: string

  .. attribute:: ExtraInfo

     Page metadata (``page``, ``total_pages``, ``file_path``, and PDF metadata).

     :type: Dictionary<string, object>


.. include:: ../footer.rst
