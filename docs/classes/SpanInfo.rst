.. include:: ../header.rst

.. _SpanInfo:

================
SpanInfo
================


Low-level text span returned by :meth:`Page.GetTextTrace`.


================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`SpanInfo.Dir`                             Writing direction
:attr:`SpanInfo.Font`                            Font name
:attr:`SpanInfo.WMode`                           Write mode
:attr:`SpanInfo.Flags`                           Font property flags
:attr:`SpanInfo.BidiLevel`                       Bidirectional level
:attr:`SpanInfo.BidiDir`                         Bidirectional direction
:attr:`SpanInfo.Ascender`                        Font ascender
:attr:`SpanInfo.Descender`                       Font descender
:attr:`SpanInfo.ColorSpace`                      Colorspace components
:attr:`SpanInfo.Color`                           Color values
:attr:`SpanInfo.Size`                            Font size
:attr:`SpanInfo.Opacity`                         Opacity
:attr:`SpanInfo.LineWidth`                       Stroke width, or ``null``
:attr:`SpanInfo.SpaceWidth`                      Width of a space
:attr:`SpanInfo.Type`                            Span type
:attr:`SpanInfo.Bbox`                            Bounding box
:attr:`SpanInfo.Layer`                           Layer name
:attr:`SpanInfo.SeqNo`                           Sequence number
:attr:`SpanInfo.Chars`                           Characters in the span
================================================ =======================================================


**Class API**

.. class:: SpanInfo

   .. attribute:: Dir

      Writing direction of the span.

      :type: :class:`Point`

   .. attribute:: Font

      Font of the span.

      :type: string

   .. attribute:: WMode

      Write mode.

      :type: uint

   .. attribute:: Flags

      Font property flags.

      :type: float

   .. attribute:: BidiLevel

      Bidirectional level.

      :type: uint

   .. attribute:: BidiDir

      Bidirectional direction.

      :type: uint

   .. attribute:: Ascender

      Font ascender.

      :type: float

   .. attribute:: Descender

      Font descender.

      :type: float

   .. attribute:: ColorSpace

      Number of colorspace components.

      :type: int

   .. attribute:: Color

      Color values.

      :type: float[]

   .. attribute:: Size

      Font size.

      :type: float

   .. attribute:: Opacity

      Span opacity.

      :type: float

   .. attribute:: LineWidth

      Stroke width for stroked text, or ``null`` for fill text.

      :type: float?

   .. attribute:: SpaceWidth

      Width of a space.

      :type: float

   .. attribute:: Type

      Span type.

      :type: int

   .. attribute:: Bbox

      Span bounding box.

      :type: :class:`Rect`

   .. attribute:: Layer

      Layer name.

      :type: string

   .. attribute:: SeqNo

      Sequence number.

      :type: int

   .. attribute:: Chars

      Characters in the span.

      :type: List

.. include:: ../footer.rst
