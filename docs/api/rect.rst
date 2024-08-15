.. include:: ../header.rst

.. _Rect:

==========
Rect
==========

*Rect* represents a rectangle defined by four floating point numbers x0, y0, x1, y1. They are treated as being coordinates of two diagonally opposite points. The first two numbers are regarded as the "top left" corner P\ :sub:`(x0,y0)` and P\ :sub:`(x1,y1)` as the "bottom right" one. However, these two properties need not coincide with their intuitive meanings -- read on.

The following remarks are also valid for :ref:`IRect` objects:

* A rectangle in the sense of MuPDF **(and PDF)** always has **borders parallel to the x- resp. y-axis**. A general orthogonal tetragon **is not a rectangle** -- in contrast to the mathematical definition.
* The constructing points can be (almost! -- see below) anywhere in the plane -- they need not even be different, and e.g. "top left" need not be the geometrical "north-western" point.
* Units are in points, where 72 points is 1 inch.
* For any given quadruple of numbers, the geometrically "same" rectangle can be defined in four different ways:
   1. Rect(P\ :sub:`(x0,y0)`, P\ :sub:`(x1,y1)`\ )
   2. Rect(P\ :sub:`(x1,y1)`, P\ :sub:`(x0,y0)`\ )
   3. Rect(P\ :sub:`(x0,y1)`, P\ :sub:`(x1,y0)`\ )
   4. Rect(P\ :sub:`(x1,y0)`, P\ :sub:`(x0,y1)`\ )

**(Changed in v1.19.0)** Hence some classification:

* A rectangle is called **valid** if `x0 <= x1` and `y0 <= y1` (i.e. the bottom right point is "south-eastern" to the top left one), otherwise **invalid**. Of the four alternatives above, **only the first** is valid. Please take into account, that in MuPDF's coordinate system, the y-axis is oriented from **top to bottom**. Invalid rectangles have been called infinite in earlier versions.

* A rectangle is called **empty** if `x0 >= x1` or `y0 >= y1`. This implies, that **invalid rectangles are also always empty.** And `width` (resp. `height`) is **set to zero** if `x0 > x1` (resp. `y0 > y1`). In previous versions, a rectangle was empty only if one of width or height was zero.

* Rectangle coordinates **cannot be outside** the number range from `FZ_MIN_INF_RECT = -2147483648` to `FZ_MAX_INF_RECT = 2147483520`. Both values have been chosen, because they are the smallest / largest 32bit integers that survive C float conversion roundtrips. In previous versions there was no limit for coordinate values.

* There is **exactly one "infinite" rectangle**, defined by `x0 = y0 = FZ_MIN_INF_RECT` and `x1 = y1 = FZ_MAX_INF_RECT`. It contains every other rectangle. It is mainly used for technical purposes -- e.g. when a function call should ignore a formally required rectangle argument. This rectangle is not empty.

* **Rectangles are (semi-) open:** The right and the bottom edges (including the resp. corners) are not considered part of the rectangle. This implies, that only the top-left corner `(x0, y0)` can ever belong to the rectangle - the other three corners never do. An empty rectangle contains no corners at all.

   .. image:: ../images/img-rect-contains.*
      :scale: 30
      :align: center

* Here is an overview of the changes.

   ================= =================================== ==================================================
   Notion            Versions < 1.19.0                   Versions 1.19.*
   ================= =================================== ==================================================
   empty             x0 = x1 or y0 = y1                  x0 >= x1 or y0 >= y1 -- includes invalid rects
   valid             n/a                                 x0 <= x1 and y0 <= y1
   infinite          all rects where x0 > x1 or y1 > y0  **exactly one infinite rect / irect!**
   coordinate values all numbers                         `FZ_MIN_INF_RECT <= number <= FZ_MAX_INF_RECT`
   borders, corners  are parts of the rectangle          right and bottom corners and edges **are outside**
   ================= =================================== ==================================================

* There are new top level functions defining infinite and standard empty rectangles and quads, see :meth:`INFINITE_RECT` and friends.


============================= =======================================================
**Methods / Attributes**      **Short Description**
============================= =======================================================
:meth:`Rect.Contains`         checks containment of point_likes and rect_likes
:meth:`Rect.GetArea`          calculate rectangle area
:meth:`Rect.IncludePoint`     enlarge rectangle to also contain a point
:meth:`Rect.IncludeRect`      enlarge rectangle to also contain another one
:meth:`Rect.Intersect`        common part with another rectangle
:meth:`Rect.Intersects`       checks for non-empty intersections
:meth:`Rect.Morph`            transform with a point and a matrix
:meth:`Rect.ToRect`           the matrix that transforms to another rectangle
:meth:`Rect.Norm`             the Euclidean norm
:meth:`Rect.Normalize`        makes a rectangle valid
:meth:`Rect.Round`            create smallest :ref:`IRect` containing rectangle
:meth:`Rect.Transform`        transform rectangle with a matrix
:attr:`Rect.BottomLeft`       bottom left point, synonym *bl*
:attr:`Rect.BottomRight`      bottom right point, synonym *br*
:attr:`Rect.Height`           rectangle height
:attr:`Rect.IsEmpty`          whether rectangle is empty
:attr:`Rect.IsValid`          whether rectangle is valid
:attr:`Rect.IsInfinite`       whether rectangle is infinite
:attr:`Rect.TopLeft`          top left point, synonym *tl*
:attr:`Rect.TopRight`         top_right point, synonym *tr*
:attr:`Rect.Quad`             :ref:`Quad` made from rectangle corners
:attr:`Rect.Width`            rectangle width
:attr:`Rect.X0`               left corners' x coordinate
:attr:`Rect.X1`               right corners' x -coordinate
:attr:`Rect.Y0`               top corners' y coordinate
:attr:`Rect.Y1`               bottom corners' y coordinate
============================= =======================================================

**Class API**

.. class:: Rect

   .. method:: Rect()

   .. method:: Rect(float x0, float y0, float x1, float y1)

   .. method:: Rect(Point tl, Point br)

   .. method:: Rect(Point tl, float x1, float y1)

   .. method:: Rect(float x0, float y0, Point br)

   .. method:: Rect(Rect rect)

   .. method:: Rect(FzRect rect)

      Overloaded constructors: *tl*, *br* stand for :data:`Point` objects, "rect" is a FzRect or Rect type, while the other parameters mean coordinates.

      If "rect" is specified, the constructor creates a **new copy** of it.

      Without parameters, the empty rectangle *Rect(0.0, 0.0, 0.0, 0.0)* is created.

   .. method:: Round()

      Creates the smallest containing :ref:`IRect`. This is **not** the same as simply rounding the rectangle's edges: The top left corner is rounded upwards and to the left while the bottom right corner is rounded downwards and to the right.

      
   .. method:: Transform(Matrix m)

      Transforms the rectangle with a matrix and **replaces the original**. If the rectangle is empty or infinite, this is a no-operation.

      :arg m: The matrix for the transformation.
      :type m: :ref:`Matrix`

      :rtype: *Rect*
      :returns: the smallest rectangle that contains the transformed original.

   .. method:: Intersect(Rect r)

      The intersection (common rectangular area, largest rectangle contained in both) of the current rectangle and *r* is calculated and **replaces the current** rectangle. If either rectangle is empty, the result is also empty. If *r* is infinite, this is a no-operation. If the rectangles are (mathematically) disjoint sets, then the result is invalid. If the result is valid but empty, then the rectangles touch each other in a corner or (part of) a side.

      :arg r: Second rectangle
      :type r: :ref:`Rect`

   .. method:: IncludeRect(Rect r)

      The smallest rectangle containing the current one and *r* is calculated and **replaces the current** one. If either rectangle is infinite, the result is also infinite. If one is empty, the other one will be taken as the result.

      :arg r: Second rectangle
      :type r: :ref:`Rect`

   .. method:: IncludePoint(Point p)

      The smallest rectangle containing the current one and point *p* is calculated and **replaces the current** one. **The infinite rectangle remains unchanged.** To create a rectangle containing a series of points, start with (the empty) `Rect(p1, p1)` and successively include the remaining points.

      :arg p: Point to include.
      :type p: :ref:`Point`


   .. method:: GetArea(string unit = null)

      Calculate the area of the rectangle and, with no parameter, equals *Abs(rect)*. Like an empty rectangle, the area of an infinite rectangle is also zero. So, at least one of *Rect(p1, p2)* and *Rect(p2, p1)* has a zero area.

      :arg string unit: Specify required unit: respective squares of *px* (pixels, default), *in* (inches), *cm* (centimeters), or *mm* (millimeters).
      :rtype: float

   .. method:: Contains(float x)

      Checks whether *x* is contained in the rectangle. It may be an *IRect*, *Rect*, *Point* or number. If *x* is an empty rectangle, this is always true. If the rectangle is empty this is always *false* for all non-empty rectangles and for all points. `x in rect` and `rect.Contains(x)` are equivalent.

      :arg x: the object to check.
      :type x: :data:`Rect` or :data:`Point`.

      :rtype: bool

   .. method:: Intersects(Rect r)

      Checks whether the rectangle and a :data:`Rect` "r" contain a common non-empty :ref:`Rect`. This will always be *false* if either is infinite or empty.

      :arg Rect r: the rectangle to check.

      :rtype: bool

   .. method:: ToRect(Rect rect)

      * New in version 1.19.3
      
      Compute the matrix which transforms this rectangle to a given one.

      :arg Rect rect: the target rectangle. Must not be empty or infinite.
      :rtype: :ref:`Matrix`
      :returns: a matrix `mat` such that `self * mat = rect`. Can for example be used to transform between the page and the pixmap coordinates. See an example use here :ref:`RecipesImages_P`.

   .. method:: Morph(Point fixpoint, Matrix matrix)

      * New in version 1.17.0
      
      Return a new quad after applying a matrix to the rectangle using the fixed point `fixpoint`.

      :arg Point fixpoint: the fixed point.
      :arg Matrix matrix: the matrix.
      :returns: a new :ref:`Quad`. This a wrapper for the same-named quad method. If infinite, the infinite quad is returned.

   .. method:: Norm()
      
      Return the Euclidean norm of the rectangle treated as a vector of four numbers.

   .. method:: Normalize()

      **Replace** the rectangle with its valid version. This is done by shuffling the rectangle corners. After completion of this method, the bottom right corner will indeed be south-eastern to the top left one (but may still be empty).

   .. attribute:: TopLeft

      Equals *Point(x0, y0)*.

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

   .. attribute:: Quad

      The quadrilateral `Quad(rect.tl, rect.tr, rect.bl, rect.br)`.

      :type: :ref:`Quad`

   .. attribute:: Width

      Width of the rectangle. Equals `max(x1 - x0, 0)`.

      :rtype: float

   .. attribute:: Height

      Height of the rectangle. Equals `max(y1 - y0, 0)`.

      :rtype: float

   .. attribute:: X0

      X-coordinate of the left corners.

      :type: float

   .. attribute:: Y0

      Y-coordinate of the top corners.

      :type: float

   .. attribute:: X1

      X-coordinate of the right corners.

      :type: float

   .. attribute:: Y1

      Y-coordinate of the bottom corners.

      :type: float

   .. attribute:: IsInfinite

      `true` if this is the infinite rectangle.

      :type: bool

   .. attribute:: IsEmpty

      `true` if rectangle is empty.

      :type: bool

   .. attribute:: IsValid

      `true` if rectangle is valid.

      :type: bool


.. include:: ../footer.rst
