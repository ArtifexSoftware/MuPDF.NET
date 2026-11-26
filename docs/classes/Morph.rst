.. include:: ../header.rst

.. _Morph:

================
Morph
================


A fixpoint plus matrix used to morph text or drawings. Passed to :meth:`Page.InsertText` and :meth:`TextWriter.WriteText`.


================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`Morph.P`                                  Fixpoint
:attr:`Morph.M`                                  Transform matrix
================================================ =======================================================


**Class API**

.. class:: Morph

   .. method:: Morph()
               Morph(Point p, Matrix m)

      Create a morph from a fixpoint and a matrix. The no-argument form uses the origin and an identity matrix.

   .. attribute:: P

      Fixpoint.

      :type: :class:`Point`

   .. attribute:: M

      Transform matrix.

      :type: :class:`Matrix`

.. include:: ../footer.rst
