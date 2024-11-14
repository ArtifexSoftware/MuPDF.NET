.. include:: ../header.rst

.. _DocumentWriter:

====================
DocumentWriter
====================

|pdf_only_class|


This class represents a utility which can output various document types supported by MuPDF.

Only used for outputting PDF documents whose pages are populated by :ref:`Story` DOMs.

Using DocumentWriter_ also for other document types might happen in the future.

====================================== ===================================================
**Method / Attribute**                 **Short Description**
====================================== ===================================================
:meth:`DocumentWriter.BeginPage`       Start a new output page
:meth:`DocumentWriter.EndPage`         Finish the current output page
:meth:`DocumentWriter.Close`           Flush pending output and close the file
====================================== ===================================================

**Class API**

.. class:: DocumentWriter

   .. method:: DocumentWriter(string path, string options: null)

      Create a document writer object, passing file path. Options to use when saving the file may also be passed.

      This class can also be used as a context manager.

      :arg path: the output file. This may be a string file name.
      
      :arg string options: specify saving options for the output PDF. Typical are "compress" or "clean". More possible values may be taken from help output of the `mutool convert` CLI utility.

   .. method:: BeginPage(Rect mediabox)

      Start a new output page of a given dimension.

      :arg Rect mediabox: a rectangle specifying the page size. After this method, output operations may write content to the page.

   .. method:: EndPage()

      Finish a page. This flushes any pending data and appends the page to the output document.

   .. method:: Close()

      Close the output file. This method is required for writing any pending data.

   For usage examples consult the section of :ref:`Story`.

.. include:: ../footer.rst
