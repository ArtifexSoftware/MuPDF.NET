.. include:: ../header.rst

.. _Quad:

==========
Quad
==========

Represents a four-sided mathematical shape (also called "quadrilateral" or "tetragon") in the plane, defined as a sequence of four :ref:`Point` objects ul, ur, ll, lr (conveniently called upper left, upper right, lower left, lower right).

Quads can **be obtained** as results of text search methods (:meth:`Page.SearchFor`), and they **are used** to define text marker annotations (see e.g. :meth:`Page.AddSquigglyAnnot` and friends), and in several draw methods (like :meth:`Page.DrawQuad` / :meth:`Shape.DrawQuad`, :meth:`Page.DrawOval`/ :meth:`Shape.DrawQuad`).

.. note::

   * If the corners of a rectangle are transformed with a **rotation**, **scale** or **translation** :ref:`Matrix`, then the resulting quad is **rectangular** (= congruent to a rectangle), i.e. all of its corners again enclose angles of 90 degrees. Property :attr:`Quad.IsRectangular` checks whether a quad can be thought of being the result of such an operation.

   * This is not true for all matrices: e.g. shear matrices produce parallelograms, and non-invertible matrices deliver "degenerate" tetragons like triangles or lines.

   * Attribute :attr:`Quad.Rect` obtains the enveloping rectangle. Vice versa, rectangles now have attributes :attr:`Rect.Quad`, resp. :attr:`IRect.quad` to obtain their respective tetragon versions.


============================= =======================================================
**Methods / Attributes**      **Short Description**
============================= =======================================================
:meth:`Quad.Transform`        Transform with a matrix
:meth:`Quad.Morph`            Transform with a point and matrix
:attr:`Quad.UpperLeft`        Upper left point
:attr:`Quad.UpperRight`       Upper right point
:attr:`Quad.LowerLeft`        Lower left point
:attr:`Quad.LowerRight`       Lower right point
:attr:`Quad.IsConvex`         True if quad is a convex set
:attr:`Quad.IsEmpty`          True if quad is an empty set
:attr:`Quad.IsRectangular`    True if quad is congruent to a rectangle
:attr:`Quad.Rect`             Smallest containing :ref:`Rect`
:attr:`Quad.Width`            The longest width value
:attr:`Quad.Height`           The longest height value
============================= =======================================================

**Class API**

.. class:: Quad

   .. method:: Quad()

   .. method:: Quad(Point ul, Point ur, Point ll, Point lr)

   .. method:: Quad(Quad quad)

      Overloaded constructors: "ul", "ur", "ll", "lr" stand for :data:`Point` objects (the four corners), "sequence" is a sequence with four :data:`Point` objects.

      If "quad" is specified, the constructor creates a **new copy** of it.

      Without parameters, a quad consisting of 4 copies of *Point(0, 0)* is created.


   .. method:: Transform(Matrix matrix)

      Modify the quadrilateral by transforming each of its corners with a matrix.

      :arg Matrix matrix: the matrix.

   .. method:: Morph(Point p, Matrix m)

      "Morph" the quad with a matrix-like using a point-like as fixed point.

      :arg Point p: the point.
      :arg Matrix m: the matrix.
      :returns: a new quad (no operation if this is the infinite quad).


   .. attribute:: Rect

      The smallest rectangle containing the quad, represented by the blue area in the following picture.

      .. image:: ../images/img-quads.*

      :type: :ref:`Rect`

   .. attribute:: UpperLeft

      Upper left point.

      :type: :ref:`Point`

   .. attribute:: UpperRight

      Upper right point.

      :type: :ref:`Point`

   .. attribute:: LowerLeft

      Lower left point.

      :type: :ref:`Point`

   .. attribute:: LowerRight

      Lower right point.

      :type: :ref:`Point`

   .. attribute:: IsConvex

      Checks if for any two points of the quad, all points on their connecting line also belong to the quad.

         .. image:: ../images/img-convexity.*
            :scale: 30

      :type: bool

   .. attribute:: IsEmpty

      true if enclosed area is zero, which means that at least three of the four corners are on the same line. If this is false, the quad may still be degenerate or not look like a tetragon at all (triangles, parallelograms, trapezoids, ...).

      :type: bool

   .. attribute:: IsRectangular

      true if all corner angles are 90 degrees. This implies that the quad is **convex and not empty**.

      :type: bool

   .. attribute:: Width

      The maximum length of the top and the bottom side.

      :type: float

   .. attribute:: Height

      The maximum length of the left and the right side.

      :type: float


.. include:: ../footer.rst
