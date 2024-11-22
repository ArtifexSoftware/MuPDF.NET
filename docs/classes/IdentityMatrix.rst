.. include:: ../header.rst

.. _Identity:
.. _IdentityMatrix:

=====================
IdentityMatrix
=====================

`IdentityMatrix` is a :ref:`Matrix` that performs no action -- to be used whenever the syntax requires a matrix, but no actual transformation should take place. It has the form `Matrix(1, 0, 0, 1, 0, 0)`.

`IdentityMatrix`  is a constant, an "immutable" object. So, all of its matrix properties are read-only and its methods are disabled.

If you need a **mutable** identity matrix as a starting point, use one of the following statements:


.. code-block:: cs

    Matrix m = new Matrix(1, 0, 0, 1, 0, 0);  // specify the values
    Matrix m = new Matrix(1, 1);              // use scaling by factor 1
    Matrix m = new Matrix(0);                 // use rotation by zero degrees
    Matrix m = new Matrix(new IdentityMatrix());     // make a copy of IdentityMatrix

.. include:: ../footer.rst
