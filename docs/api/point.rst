.. include:: ../header.rst

.. _Point:

================
Point
================

*Point* represents a point in the plane, defined by its x and y coordinates.

============================ ============================================
**Attribute / Method**       **Description**
============================ ============================================
:meth:`Point.distance_to`    calculate distance to point or rect
:meth:`Point.Transform`      transform point with a matrix
:attr:`Point.Abs`            same as unit, but positive coordinates
:attr:`Point.Unit`           point coordinates divided by *abs(point)*
:attr:`Point.X`              the X-coordinate
:attr:`Point.Y`              the Y-coordinate
============================ ============================================

**Class API**

.. class:: Point

   .. method:: Point()

   .. method:: Point(float x, float y)

   .. method:: Point(Point point)

   .. method:: Point(FzPoint point)

      Overloaded constructors.

      Without parameters, *Point(0, 0)* will be created.

      With another point or FzPoint specified, a **new copy** will be created.

     :arg float x: x coordinate of the point

     :arg float y: y coordinate of the point

   .. method:: DistanceTo(Point x [, string unit])
   .. method:: DistanceTo(Rect x [, string unit])

      Calculate the distance to *x*, which may be :data:`point_like` or :data:`rect_like`. The distance is given in units of either pixels (default), inches, centimeters or millimeters.

     :arg point_like,rect_like x: to which to compute the distance.

     :arg str unit: the unit to be measured in. One of "px", "in", "cm", "mm".

     :rtype: float
     :returns: the distance to *x*. If this is :data:`rect_like`, then the distance

         * is the length of the shortest line connecting to one of the rectangle sides
         * is calculated to the **finite version** of it
         * is zero if it **contains** the point

   .. method:: Transform(m)

      Apply a matrix to the point and replace it with the result.

     :arg matrix_like m: The matrix to be applied.

     :rtype: :ref:`Point`

   .. attribute:: Unit

      Result of dividing each coordinate by *norm(point)*, the distance of the point to (0,0). This is a vector of length 1 pointing in the same direction as the point does. Its x, resp. y values are equal to the cosine, resp. sine of the angle this vector (and the point itself) has with the x axis.

      .. image:: ../images/img-point-unit.*

      :type: :ref:`Point`

   .. attribute:: Abs

      Same as :attr:`unit` above, replacing the coordinates with their absolute values.

      :type: :ref:`Point`

   .. attribute:: X

      The x coordinate

      :type: float

   .. attribute:: Y

      The y coordinate

      :type: float


.. include:: ../footer.rst
