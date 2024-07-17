.. include:: ../header.rst

.. _Link:

================
Link
================
Represents a pointer to somewhere (this document, other documents, the internet). Links exist per document page, and they are forward-chained to each other, starting from an initial link which is accessible by the :attr:`MuPDFPage.FirstLink` property.

There is a parent-child relationship between a link and its page. If the page object becomes unusable (closed document, any document structure change, etc.), then so does every of its existing link objects -- an exception is raised saying that the object is "orphaned", whenever a link property or method is accessed.

============================= ============================================
**Method / Attribute**                 **Short Description**
============================= ============================================
:meth:`Link.SetBorder`        modify border properties
:meth:`Link.SetColors`        modify color properties
:meth:`Link.SetFlags`         modify link flags
:attr:`Link.Border`           border characteristics
:attr:`Link.Colors`           border line color
:attr:`Link.Dest`             points to destination details
:attr:`Link.IsExternal`       checks if the link is an external destination
:attr:`Link.Flags`            link annotation flags
:attr:`Link.Next`             points to next link
:attr:`Link.Rect`             clickable area in untransformed coordinates
:attr:`Link.Uri`              link destination
:attr:`Link.Xref`             :data:`xref` number of the entry
============================= ============================================

**Class API**

.. class:: Link

   .. method:: SetBorder(Border border: null, float width: 0, string style: null, int[] dashes: null)

      PDF only: Change border width and dashing properties.

      :arg Border border: a dictionary as returned by the :attr:`Border` property, with keys *"width"* (*float*), *"style"* (*string*) and *"dashes"* (*int[]*). Omitted keys will leave the resp. property unchanged. To e.g. remove dashing use: *"dashes": []*. If dashes is not an empty sequence, "style" will automatically be set to "D" (dashed).

      :arg float width: see above.
      :arg string style: see above.
      :arg int[] dashes: see above.

   .. method:: SetColors(Color colors: null, float[] stroke: null, float[] fill: null)

      PDF only: Changes the "stroke" color.

      .. note:: In PDF, links are a subtype of annotations technically and **do not support fill colors**. However, to keep a consistent API, we do allow specifying a `fill=` parameter like with all annotations, which will be ignored with a warning.

      :arg Color colors: a dictionary containing color specifications. For accepted dictionary keys and values see below. The most practical way should be to first make a copy of the *colors* property and then modify this dictionary as required.
      :arg float[] stroke: see above.
      :arg float[] fill: see above.

   .. method:: SetFlags(int flags)

      Set the PDF `/F` property of the link annotation. See :meth:`Annot.SetFlags` for details. If not a PDF, this method is a no-op.


   .. attribute:: Flags

      Return the link annotation flags, an integer (see :attr:`Annot.Flags` for details). Zero if not a PDF.


   .. attribute:: Color

      Meaningful for PDF only: A dictionary of two tuples of floats in range `0 <= float <= 1` specifying the *stroke* and the interior (*fill*) colors. If not a PDF, *null* is returned. As mentioned above, the fill color is always `null` for links. The stroke color is used for the border of the link rectangle. The length of the tuple implicitly determines the colorspace: 1 = GRAY, 3 = RGB, 4 = CMYK. So `(1.0, 0.0, 0.0)` stands for RGB color red. The value of each float *f* is mapped to the integer value *i* in range 0 to 255 via the computation *f = i / 255*.

      :rtype: Color

   .. attribute:: Border

      Meaningful for PDF only: A dictionary containing border characteristics. It will be *null* for non-PDFs and an empty dictionary if no border information exists. The following keys can occur:

      * *width* -- a float indicating the border thickness in points. The value is -1.0 if no width is specified.

      * *dashes* -- a sequence of integers specifying a line dash pattern. *[]* means no dashes, *[n]* means equal on-off lengths of *n* points, longer lists will be interpreted as specifying alternating on-off length values. See the :ref:`AdobeManual` page 126 for more detail.

      * *style* -- 1-byte border style: *S* (Solid) = solid rectangle surrounding the annotation, *D* (Dashed) = dashed rectangle surrounding the link, the dash pattern is specified by the *dashes* entry, *B* (Beveled) = a simulated embossed rectangle that appears to be raised above the surface of the page, *I* (Inset) = a simulated engraved rectangle that appears to be recessed below the surface of the page, *U* (Underline) = a single line along the bottom of the annotation rectangle.

      :rtype: dict

   .. attribute:: Rect

      The area that can be clicked in untransformed coordinates.

      :type: :ref:`Rect`

   .. attribute:: IsExternal

      A bool specifying whether the link target is outside of the current document.

      :type: bool

   .. attribute:: Uri

      A string specifying the link target. The meaning of this property should
      be evaluated in conjunction with property `IsExternal`:
      
      *
        `IsExternal` is true: `uri` points to some target outside the current
        PDF, which may be an internet resource (`uri` starts with ``http://`` or
        similar), another file (`uri` starts with "file:" or ``file://``) or some
        other service like an e-mail address (`uri` starts with ``mailto:``).

      *
        `IsExternal` is false: `uri` will be `null` or point to an
        internal location. In case of PDF documents, this should either be
        *#nnnn* to indicate a 1-based (!) page number *nnnn*, or a named
        location. The format varies for other document types, for example
        "../FixedDoc.fdoc#PG_2_LNK_1" for page number 2 (1-based) in an XPS
        document.

      :type: string

   .. attribute:: Xref

      An integer specifying the PDF :data:`Xref`. Zero if not a PDF.

      :type: int

   .. attribute:: Next

      The next link or *null*.

      :type: *Link*

   .. attribute:: Dest

      The link destination details object.

      :type: :ref:`LinkDest`

.. include:: ../footer.rst
