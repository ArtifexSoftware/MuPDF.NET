.. include:: ../header.rst

.. _Label:

================
Label
================


One PDF page-label rule. Pass a list of :class:`Label` to :meth:`Document.SetPageLabels`. :meth:`Document.GetPageLabels` returns dictionaries with the same fields under keys ``startpage``, ``prefix``, ``style``, and ``firstpagenum``.


================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`Label.StartPage`                          First 0-based page for this rule
:attr:`Label.Prefix`                             Prefix string (for example ``"A-"``)
:attr:`Label.Style`                              Numbering style (``D``, ``r``, ``R``, ``a``, ``A``)
:attr:`Label.FirstPageNum`                       First number in this range
================================================ =======================================================


**Class API**

.. class:: Label

   .. attribute:: StartPage

      First page number (0-based) to apply the label rule. Required. The rule applies until the next larger start page or the end of the document.

      :type: int

   .. attribute:: Prefix

      Arbitrary string to start the label with, for example ``"A-"``. Default is ``""``.

      :type: string

   .. attribute:: Style

      Numbering style: ``"D"`` (decimal), ``"r"`` / ``"R"`` (Roman), ``"a"`` / ``"A"`` (alphabetical). ``""`` means no numbering.

      :type: string

   .. attribute:: FirstPageNum

      Start numbering with this value. Default is 1; smaller values are ignored.

      :type: int

.. include:: ../footer.rst
