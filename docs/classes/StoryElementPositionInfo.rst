.. include:: ../header.rst

.. _StoryElementPositionInfo:

=============================
StoryElementPositionInfo
=============================


Placement record passed to the :meth:`Story.ElementPositions` overload that takes ``Action<StoryElementPositionInfo>``. :class:`Position` is the other callback type, with the same fields except :attr:`Position.OpenClose` is a ``bool``.


================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`StoryElementPositionInfo.Depth`           Depth of this element in the box
:attr:`StoryElementPositionInfo.Heading`         Header level (0 if none, 1–6)
:attr:`StoryElementPositionInfo.Id`              Value of the ``id`` attribute
:attr:`StoryElementPositionInfo.Rect`            Element position on the page
:attr:`StoryElementPositionInfo.Text`            Immediate text of the element
:attr:`StoryElementPositionInfo.OpenClose`       Bit 0 opens, bit 1 closes
:attr:`StoryElementPositionInfo.RectNum`         Rectangles filled so far
:attr:`StoryElementPositionInfo.Href`            Value of the ``href`` attribute
:attr:`StoryElementPositionInfo.PageNum`         Page number
================================================ =======================================================


**Class API**

.. class:: StoryElementPositionInfo

   .. attribute:: Depth

      Nesting depth in the box tree.

      :type: int

   .. attribute:: Heading

      Header level, 0 if none, 1–6 for ``h1`` – ``h6``.

      :type: int

   .. attribute:: Id

      Value of the ``id`` attribute, or ``null``.

      :type: string

   .. attribute:: Rect

      Bounding rectangle of the element on the page.

      :type: :class:`Rect`

   .. attribute:: Text

      Immediate text of the element.

      :type: string

   .. attribute:: OpenClose

      Bit 0: element opens; bit 1: element closes.

      :type: int

   .. attribute:: RectNum

      Count of rectangles filled by the story so far.

      :type: int

   .. attribute:: Href

      Value of the ``href`` attribute, or ``null``.

      :type: string

   .. attribute:: PageNum

      1-based page number during :meth:`Story.Write` and the stabilized writers.

      :type: int

.. include:: ../footer.rst
