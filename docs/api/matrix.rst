.. include:: ../header.rst

.. _Matrix:

============
Matrix
============

Matrix is a row-major 3x3 matrix used by image transformations in MuPDF (which complies with the respective concepts laid down in the :ref:`AdobeManual`). With matrices you can manipulate the rendered image of a page in a variety of ways: (parts of) the page can be rotated, zoomed, flipped, sheared and shifted by setting some or all of just six float values.


Since all points or pixels live in a two-dimensional space, one column vector of that matrix is a constant unit vector, and only the remaining six elements are used for manipulations. These six elements are usually represented by `[a, b, c, d, e, f]`. Here is how they are positioned in the matrix:

.. image:: ../images/img-matrix.*


Please note:

    * the below methods are just convenience functions -- everything they do, can also be achieved by directly manipulating the six numerical values
    * all manipulations can be combined -- you can construct a matrix that rotates **and** shears **and** scales **and** shifts, etc. in one go. If you however choose to do this, do have a look at the **remarks** further down or at the :ref:`AdobeManual`.

================================ ==============================================
**Method / Attribute**             **Description**
================================ ==============================================
:meth:`Matrix.Prerotate`         perform a rotation
:meth:`Matrix.Prescale`          perform a scaling
:meth:`Matrix.Preshear`          perform a shearing (skewing)
:meth:`Matrix.Pretranslate`      perform a translation (shifting)
:meth:`Matrix.Concat`            perform a matrix multiplication
:meth:`Matrix.Invert`            calculate the inverted matrix
:meth:`Matrix.Norm`              the Euclidean norm
:attr:`Matrix.A`                 zoom factor X direction
:attr:`Matrix.B`                 shearing effect Y direction
:attr:`Matrix.C`                 shearing effect X direction
:attr:`Matrix.D`                 zoom factor Y direction
:attr:`Matrix.E`                 horizontal shift
:attr:`Matrix.F`                 vertical shift
:attr:`Matrix.IsRectilinear`     true if rect corners will remain rect corners
================================ ==============================================

**Class API**

.. class:: Matrix

   .. method:: Matrix()

   .. method:: Matrix(float zoom-x, float zoom-y)

   .. method:: Matrix(float shear-x, float shear-y, 1)

   .. method:: Matrix(float a, float b, float c, float d, float e, float f)

   .. method:: Matrix(Matrix matrix)

   .. method:: Matrix(int degree)

   .. method:: Matrix(Rect rect)

      Overloaded constructors.

      Without parameters, the zero matrix *Matrix(0.0, 0.0, 0.0, 0.0, 0.0, 0.0)* will be created.

      *zoom-** and *shear-** specify zoom or shear values (float) and create a zoom or shear matrix, respectively.

      For "matrix" a **new copy** of another matrix will be made.

      Float value "degree" specifies the creation of a rotation matrix which rotates anti-clockwise.

      A "rect" must be any Rect object.

      *Matrix(1, 1)* and *Matrix(IdentityMatrix)* create modifiable versions of the :ref:`Identity` matrix, which looks like *[1, 0, 0, 1, 0, 0]*.

   .. method:: Norm()

      Return the Euclidean norm of the matrix as a vector.

   .. method:: Prerotate(float deg)

      Modify the matrix to perform a counter-clockwise rotation for positive *deg* degrees, else clockwise. The matrix elements of an identity matrix will change in the following way:

      *[1, 0, 0, 1, 0, 0] -> [cos(deg), sin(deg), -sin(deg), cos(deg), 0, 0]*.

      :arg float deg: The rotation angle in degrees (use conventional notation based on Pi = 180 degrees).

   .. method:: Prescale(float sx, float sy)

      Modify the matrix to scale by the zoom factors sx and sy. Has effects on attributes *a* thru *d* only: *[a, b, c, d, e, f] -> [a*sx, b*sx, c*sy, d*sy, e, f]*.

      :arg float sx: Zoom factor in X direction. For the effect see description of attribute *a*.

      :arg float sy: Zoom factor in Y direction. For the effect see description of attribute *d*.

   .. method:: Preshear(float sx, float sy)

      Modify the matrix to perform a shearing, i.e. transformation of rectangles into parallelograms (rhomboids). Has effects on attributes *a* thru *d* only: *[a, b, c, d, e, f] -> [c*sy, d*sy, a*sx, b*sx, e, f]*.

      :arg float sx: Shearing effect in X direction. See attribute *c*.

      :arg float sy: Shearing effect in Y direction. See attribute *b*.

   .. method:: Pretranslate(float tx, float ty)

      Modify the matrix to perform a shifting / translation operation along the x and / or y axis. Has effects on attributes *e* and *f* only: *[a, b, c, d, e, f] -> [a, b, c, d, tx*a + ty*c, tx*b + ty*d]*.

      :arg float tx: Translation effect in X direction. See attribute *e*.

      :arg float ty: Translation effect in Y direction. See attribute *f*.

   .. method:: Concat(Matrix m1, Matrix m2)

      Calculate the matrix product *m1 * m2* and store the result in the current matrix. Any of *m1* or *m2* may be the current matrix. Be aware that matrix multiplication is not commutative. So the sequence of *m1*, *m2* is important.

      :arg m1: First (left) matrix.
      :type m1: :ref:`Matrix`

      :arg m2: Second (right) matrix.
      :type m2: :ref:`Matrix`

   .. method:: Invert(Matrix src: null)

      Calculate the matrix inverse of *m* and store the result in the current matrix. Returns *1* if *m* is not invertible ("degenerate"). In this case the current matrix **will not change**. Returns *0* if *m* is invertible, and the current matrix is replaced with the inverted *m*.

      :arg m: Matrix to be inverted. If not provided, the current matrix will be used.
      :type m: :ref:`Matrix`

      :rtype: int

   .. attribute:: A

      Scaling in X-direction **(width)**. For example, a value of 0.5 performs a shrink of the **width** by a factor of 2. If a < 0, a left-right flip will (additionally) occur.

      :type: float

   .. attribute:: B

      Causes a shearing effect: each `Point(x, y)` will become `Point(x, y - b*x)`. Therefore, horizontal lines will be "tilt".

      :type: float

   .. attribute:: C

      Causes a shearing effect: each `Point(x, y)` will become `Point(x - c*y, y)`. Therefore, vertical lines will be "tilt".

      :type: float

   .. attribute:: D

      Scaling in Y-direction **(height)**. For example, a value of 1.5 performs a stretch of the **height** by 50%. If d < 0, an up-down flip will (additionally) occur.

      :type: float

   .. attribute:: E

      Causes a horizontal shift effect: Each *Point(x, y)* will become *Point(x + e, y)*. Positive (negative) values of *e* will shift right (left).

      :type: float

   .. attribute:: F

      Causes a vertical shift effect: Each *Point(x, y)* will become *Point(x, y - f)*. Positive (negative) values of *f* will shift down (up).

      :type: float

   .. attribute:: IsRectilinear

      Rectilinear means that no shearing is present and that any rotations are integer multiples of 90 degrees. Usually this is used to confirm that (axis-aligned) rectangles before the transformation are still axis-aligned rectangles afterwards.

      :type: bool

.. note::

   * Matrix multiplication is **not commutative** -- changing the sequence of the multiplicands will change the result in general. So it can quickly become unclear which result a transformation will yield.


Examples
-------------
Here are examples that illustrate some of the achievable effects. All pictures show some text, inserted under control of some matrix and relative to a fixed reference point (the red dot).

1. The :ref:`Identity` matrix performs no operation.

.. image:: ../images/img-matrix-0.*
   :scale: 66

2. The scaling matrix `Matrix(2, 0.5)` stretches by a factor of 2 in horizontal, and shrinks by factor 0.5 in vertical direction.

.. image:: ../images/img-matrix-1.*
   :scale: 66

3. Attributes :attr:`Matrix.E` and :attr:`Matrix.F` shift horizontally and, respectively vertically. In the following 10 to the right and 20 down.

.. image:: ../images/img-matrix-2.*
   :scale: 66

4. A negative :attr:`Matrix.A` causes a left-right flip.

.. image:: ../images/img-matrix-3.*
   :scale: 66

5. A negative :attr:`Matrix.D` causes an up-down flip.

.. image:: ../images/img-matrix-4.*
   :scale: 66

6. Attribute :attr:`Matrix.B` tilts upwards / downwards along the x-axis.

.. image:: ../images/img-matrix-5.*
   :scale: 66

7. Attribute :attr:`Matrix.C` tilts left / right along the y-axis.

.. image:: ../images/img-matrix-6.*
   :scale: 66

8. Matrix `Matrix(beta)` performs counterclockwise rotations for positive angles `beta`.

.. image:: ../images/img-matrix-7.*
   :scale: 66



.. include:: ../footer.rst
