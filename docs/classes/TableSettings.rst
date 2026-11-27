.. include:: ../header.rst

.. _TableSettings:

================
TableSettings
================


Configuration for :meth:`Page.FindTables`. Strategy values are ``"lines"``, ``"lines_strict"``, ``"text"`` and ``"explicit"``.


================================================ =======================================================
**Method / Attribute**                           **Short Description**
================================================ =======================================================
:meth:`TableSettings.Resolve`                    Validated settings from an instance or defaults
:meth:`TableSettings.Validate`                   Fill defaults and reject invalid values
:attr:`TableSettings.VerticalStrategy`           Vertical grid detection strategy
:attr:`TableSettings.HorizontalStrategy`         Horizontal grid detection strategy
:attr:`TableSettings.ExplicitVerticalLines`      Explicit vertical lines (``"explicit"``)
:attr:`TableSettings.ExplicitHorizontalLines`    Explicit horizontal lines (``"explicit"``)
:attr:`TableSettings.SnapTolerance`              Snap nearby lines into one
:attr:`TableSettings.SnapXTolerance`             Horizontal snap (default: :attr:`TableSettings.SnapTolerance`)
:attr:`TableSettings.SnapYTolerance`             Vertical snap (default: :attr:`TableSettings.SnapTolerance`)
:attr:`TableSettings.JoinTolerance`              Join almost-touching lines
:attr:`TableSettings.JoinXTolerance`             Horizontal join (default: :attr:`TableSettings.JoinTolerance`)
:attr:`TableSettings.JoinYTolerance`             Vertical join (default: :attr:`TableSettings.JoinTolerance`)
:attr:`TableSettings.EdgeMinLength`              Ignore shorter edges
:attr:`TableSettings.MinWordsVertical`           Words needed for a virtual column
:attr:`TableSettings.MinWordsHorizontal`         Words needed for a virtual row
:attr:`TableSettings.IntersectionTolerance`      How close orthogonal lines must be
:attr:`TableSettings.IntersectionXTolerance`     Horizontal intersection (default: :attr:`TableSettings.IntersectionTolerance`)
:attr:`TableSettings.IntersectionYTolerance`     Vertical intersection (default: :attr:`TableSettings.IntersectionTolerance`)
:attr:`TableSettings.TextSettings`               Options for the ``"text"`` strategy
================================================ =======================================================


**Class API**

.. class:: TableSettings

  .. method:: Resolve(TableSettings settings: null)

     Create a validated :class:`TableSettings` from an existing instance, or defaults when *settings* is ``null``.

     :arg TableSettings settings: settings to validate, or ``null`` for defaults.

     :rtype: :class:`TableSettings`

  .. method:: Validate()

     Reject negative tolerances and unknown strategies, then fill NaN snap / join / intersection values from the corresponding combined setting.

  .. attribute:: VerticalStrategy

     Strategy for detecting vertical lines: ``"lines"`` (default), ``"lines_strict"``, ``"text"``, ``"explicit"``.

     :type: string

  .. attribute:: HorizontalStrategy

     Strategy for detecting horizontal lines. Same values as :attr:`TableSettings.VerticalStrategy`. Default ``"lines"``.

     :type: string

  .. attribute:: ExplicitVerticalLines

     Explicit vertical line x-coordinates when :attr:`TableSettings.VerticalStrategy` is ``"explicit"``. Must have at least two entries in that case.

     :type: List<float>

  .. attribute:: ExplicitHorizontalLines

     Explicit horizontal line y-coordinates when :attr:`TableSettings.HorizontalStrategy` is ``"explicit"``. Must have at least two entries in that case.

     :type: List<float>

  .. attribute:: SnapTolerance

     Any two horizontal lines whose y-values differ by no more than this are snapped into one. Same for vertical lines. Default 3. Separate values: :attr:`TableSettings.SnapXTolerance`, :attr:`TableSettings.SnapYTolerance`.

     :type: float

  .. attribute:: SnapXTolerance

     Horizontal snap. ``NaN`` (default) means use :attr:`TableSettings.SnapTolerance`.

     :type: float

  .. attribute:: SnapYTolerance

     Vertical snap. ``NaN`` (default) means use :attr:`TableSettings.SnapTolerance`.

     :type: float

  .. attribute:: JoinTolerance

     Join two lines if end and start differ by no more than this (points). Default 3.

     :type: float

  .. attribute:: JoinXTolerance

     Horizontal join. ``NaN`` (default) means use :attr:`TableSettings.JoinTolerance`.

     :type: float

  .. attribute:: JoinYTolerance

     Vertical join. ``NaN`` (default) means use :attr:`TableSettings.JoinTolerance`.

     :type: float

  .. attribute:: EdgeMinLength

     Ignore a line if its length does not exceed this value (points). Default 3.

     :type: float

  .. attribute:: MinWordsVertical

     For vertical strategy ``"text"``: at least this many words must coincide to establish a virtual column boundary. Default 3.

     :type: float

  .. attribute:: MinWordsHorizontal

     For horizontal strategy ``"text"``: at least this many words must coincide to establish a virtual row boundary. Default 1.

     :type: float

  .. attribute:: IntersectionTolerance

     Orthogonal lines must be within this value (points) to be considered intersecting. Default 3.

     :type: float

  .. attribute:: IntersectionXTolerance

     Horizontal intersection. ``NaN`` (default) means use :attr:`TableSettings.IntersectionTolerance`.

     :type: float

  .. attribute:: IntersectionYTolerance

     Vertical intersection. ``NaN`` (default) means use :attr:`TableSettings.IntersectionTolerance`.

     :type: float

  .. attribute:: TextSettings

     Options for the ``"text"`` strategy (for example ``x_tolerance``, ``y_tolerance``).

     :type: Dictionary<string, object>


.. include:: ../footer.rst
