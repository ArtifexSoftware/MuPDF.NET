.. include:: ../header.rst

.. _Position:

================
Position
================


Placement record passed to :meth:`Story.ElementPositions` for headings, elements with an ``id``, and elements with an ``href``.


================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`Position.Depth`                           Depth of this element in the box
:attr:`Position.Heading`                         Header level (0 if none, 1–6 for h1–h6)
:attr:`Position.Href`                            Value of the ``href`` attribute, or ``null``
:attr:`Position.Id`                              Value of the ``id`` attribute, or ``null``
:attr:`Position.Rect`                            Element position on the page
:attr:`Position.Text`                            Immediate text of the element
:attr:`Position.OpenClose`                       Whether the element opens or closes
:attr:`Position.RectNum`                         Count of rectangles filled by the story so far
:attr:`Position.PageNum`                         Page number
================================================ =======================================================


**Class API**

.. class:: Position

   .. attribute:: Depth

      Depth of this element in the box structure.

      :type: int

   .. attribute:: Heading

      The header level, 0 if no header, 1–6 for ``h1`` – ``h6``.

      :type: int

   .. attribute:: Href

      Value of the ``href`` attribute, or ``null`` if not defined.

      :type: string

   .. attribute:: Id

      Value of the ``id`` attribute, or ``null`` if not defined.

      :type: string

   .. attribute:: Rect

      Element position on the page.

      :type: :ref:`Rect`

   .. attribute:: Text

      Immediate text of the element.

      :type: string

   .. attribute:: OpenClose

      ``true`` when the element opens or closes. Relevant for elements that may contain other elements and thus may not immediately be closed after being opened.

      :type: bool

   .. attribute:: RectNum

      Count of rectangles filled by the story so far.

      :type: int

   .. attribute:: PageNum

      Page number. Set this on the optional :class:`Position` argument to :meth:`Story.ElementPositions`, or when using :meth:`Story.Write`, :meth:`Story.WriteStabilized`, :meth:`Story.WriteWithLinks`, or :meth:`Story.WriteStabilizedWithLinks`.

      :type: int

.. include:: ../footer.rst
