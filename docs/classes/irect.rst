.. include:: ../header.rst

.. _IRect:

==========
IRect
==========

`IRect` is a rectangular bounding box, very similar to :ref:`Rect`, except that all corner coordinates are integers. `IRect` is used to specify an area of pixels, e.g. to receive image data during rendering. Otherwise, e.g. considerations concerning emptiness and validity of rectangles also apply to this class. Methods and attributes have the same names, and in many cases are implemented by re-using the respective :ref:`Rect` counterparts.

============================== ==============================================
**Attribute / Method**          **Short Description**
============================== ==============================================
:meth:`IRect.Contains`         Checks containment of another object
:meth:`IRect.GetArea`          Calculate rectangle area
:meth:`IRect.Intersect`        Common part with another rectangle
:meth:`IRect.Intersects`       Checks for non-empty intersection
:meth:`IRect.Morph`            Transform with a point and a matrix
:meth:`IRect.ToRect`           Matrix that transforms to another rectangle
:meth:`IRect.Norm`             The Euclidean norm
:meth:`IRect.Normalize`        Makes a rectangle finite
:attr:`IRect.BottomLeft`       Bottom left point, synonym *bl*
:attr:`IRect.BottomRight`      Bottom right point, synonym *br*
:attr:`IRect.Height`           Height of the rectangle
:attr:`IRect.IsEmpty`          Whether rectangle is empty
:attr:`IRect.IsInfinite`       Whether rectangle is infinite
:attr:`IRect.Rect`             The :ref:`Rect` equivalent
:attr:`IRect.TopLeft`          Top left point, synonym *tl*
:attr:`IRect.TopRight`         Top right point, synonym *tr*
:attr:`IRect.Quad`             :ref:`Quad` made from rectangle corners
:attr:`IRect.Width`            Width of the rectangle
:attr:`IRect.X0`               X coordinate of the top left corner
:attr:`IRect.X1`               X coordinate of the bottom right corner
:attr:`IRect.Y0`               Y coordinate of the top left corner
:attr:`IRect.Y1`               Y coordinate of the bottom right corner
============================== ==============================================

**Class API**

.. class:: IRect

   .. method:: IRect()

   .. method:: IRect(float x0, float y0, float x1, float y1)

   .. method:: IRect(IRect irect)

      Overloaded constructors. Also see examples below and those for the :ref:`Rect` class.

      If another irect is specified, a **new copy** will be made.

      If sequence is specified, it must be a sequence type of 4 numbers (see :ref:`SequenceTypes`). Non-integer numbers will be truncated, non-numeric values will raise an exception.

      The other parameters mean integer coordinates.


   .. method:: GetArea([unit])

      Calculates the area of the rectangle and, with no parameter, equals `abs(IRect)`. Like an empty rectangle, the area of an infinite rectangle is also zero.

      :arg string unit: Specify required unit: respective squares of "px" (pixels, default), "in" (inches), "cm" (centimeters), or "mm" (millimeters).

      :rtype: float

   .. method:: Intersect(ir)

      The intersection (common rectangular area) of the current rectangle and *ir* is calculated and replaces the current rectangle. If either rectangle is empty, the result is also empty. If either rectangle is infinite, the other one is taken as the result -- and hence also infinite if both rectangles were infinite.

      :arg Rect ir: Second rectangle.

   .. method:: Contains(x)

      Checks whether *x* is contained in the rectangle. It may be :data:`Rect`, :data:`Point` or a number. If *x* is an empty rectangle, this is always true. Conversely, if the rectangle is empty this is always `false`, if *x* is not an empty rectangle and not a number. If *x* is a number, it will be checked to be one of the four components. *x in irect* and `irect.contains(x)` are equivalent.

      :arg x: the object to check.
      :type x: :ref:`IRect` or :ref:`Rect` or :ref:`Point` or int

      :rtype: bool

   .. method:: Intersects(Rect r)

      Checks whether the rectangle and the :data:`Rect` "r" contain a common non-empty :ref:`IRect`. This will always be `false` if either is infinite or empty.

      :arg Rect r: the rectangle to check.

      :rtype: bool

   .. method:: ToRect(rect)

      Compute the matrix which transforms this rectangle to a given one. See :meth:`MuPDFRect.ToRect`.

      :arg Rect rect: the target rectangle. Must not be empty or infinite.
      
      :rtype: :ref:`Matrix`
      :returns: a matrix `mat` such that `self * mat = rect`. Can for example be used to transform between the page and the pixmap coordinates.


   .. method:: Morph(Point p, Matrix m)
      
      Return a new quad after applying a matrix to it using a fixed point.

      :arg Point p: the fixed point.
      :arg Matrix m: the matrix.
      :returns: a new :ref:`Quad`. This a wrapper of the same-named quad method. If infinite, the infinite quad is returned.

   .. method:: Norm()
      
      Return the Euclidean norm of the rectangle treated as a vector of four numbers.

   .. method:: Normalize()

      Make the rectangle finite. This is done by shuffling rectangle corners. After this, the bottom right corner will indeed be south-eastern to the top left one. See :ref:`Rect` for a more details.

   .. attribute:: TopLeft

      Equals `Point(x0, y0)`.

      :type: :ref:`Point`

   .. attribute:: TopRight

      Equals `Point(x1, y0)`.

      :type: :ref:`Point`

   .. attribute:: BottomLeft

      Equals `Point(x0, y1)`.

      :type: :ref:`Point`

   .. attribute:: BottomRight

      Equals `Point(x1, y1)`.

      :type: :ref:`Point`

   .. attribute:: Rect

      The :ref:`Rect` with the same coordinates as floats.

      :type: :ref:`Rect`

   .. attribute:: Quad

      The quadrilateral `Quad(irect.tl, irect.tr, irect.bl, irect.br)`.

      :type: :ref:`Quad`

   .. attribute:: Width

      Contains the width of the bounding box. Equals `abs(x1 - x0)`.

      :type: int

   .. attribute:: Height

      Contains the height of the bounding box. Equals `abs(y1 - y0)`.

      :type: int

   .. attribute:: X0

      X-coordinate of the left corners.

      :type: int

   .. attribute:: Y0

      Y-coordinate of the top corners.

      :type: int

   .. attribute:: X1

      X-coordinate of the right corners.

      :type: int

   .. attribute:: Y1

      Y-coordinate of the bottom corners.

      :type: int

   .. attribute:: IsInfinite

      *true* if rectangle is infinite, `false` otherwise.

      :type: bool

   .. attribute:: IsEmpty

      *true* if rectangle is empty, `false` otherwise.

      :type: bool


.. include:: ../footer.rst

