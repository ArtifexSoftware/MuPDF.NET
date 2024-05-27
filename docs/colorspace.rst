.. include:: header.rst

.. _Colorspace:

================
ColorSpace
================

Represents the color space of a :ref:`Pixmap`.


**Class API**

.. class:: ColorSpace

   .. method:: ColorSpace(int type)
   .. method:: ColorSpace(ColorSpace cs)

      Constructor

      :arg int n: A number identifying the colorspace. Possible values are :data:`CS_RGB`, :data:`CS_GRAY` and :data:`CS_CMYK`.

   .. attribute:: Name

      The name identifying the colorspace.

      :type: str

   .. attribute:: N

      The number of bytes required to define the color of one pixel.

      :type: int


    **Predefined ColorSpaces**

    For saving some typing effort, there exist predefined colorspace objects for the three available cases.

    * :data:`csRGB`  = *new ColorSpace(Utils.CS_RGB)*
    * :data:`csGRAY` = *new ColorSpace(Utils.CS_GRAY)*
    * :data:`csCMYK` = *new ColorSpace(Utils.CS_CMYK)*

.. include:: footer.rst