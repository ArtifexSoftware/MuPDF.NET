.. include:: ../header.rst

.. _Glossary:

==============
Glossary
==============

Array Types
----------------

.. data:: coordinate

        This is an essential general mathematical / geometrical term for understanding this documentation.

.. data:: matrix_like

        A sequence of 6 numbers.

.. data:: rect_like

        A sequence of 4 numbers.

.. data:: irect_like

        A sequence of 4 integers.

.. data:: point_like

        A sequence of 2 numbers.

.. data:: quad_like

        A sequence of 4 :data:`point_like` items.

Number Types
----------------

.. data:: fontsize

    When referring to font size this metric is measured in points where 1 inch = 72 points.

.. data:: resolution

    Images and :ref:`Pixmap` objects may contain resolution information provided as "dots per inch", dpi, in each direction (horizontal and vertical). When MuPDF reads an image from a file or from a PDF object, it will parse this information and put it in :attr:`Pixmap.Xres`, :attr:`Pixmap.Yres`, respectively. If it finds no meaningful information in the input (like non-positive values or values exceeding 4800), it will use "sane" defaults instead. The usual default value is 96, but it may also be 72 in some cases (e.g. for JPX images).

.. data:: xref

    Abbreviation for cross-reference number: this is an integer for unique identification of objects in a PDF. A PDF contains cross-reference table in each PDF, which stores the relative position of each object for quick lookup. The cross-reference table is one entry longer than the number of existing object: item zero is reserved and must not be used in any way. Many classes have an :data:`xref` attribute (which is zero for non-PDFs), and one can find out the total number of objects in a PDF via :meth:`Document.GetXrefLength` *- 1*.

.. include:: ../footer.rst