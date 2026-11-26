.. include:: ../header.rst

.. _LinkInfo:

================
LinkInfo
================


One link (or TOC item) as returned by :meth:`Page.GetLinks`.


================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`LinkInfo.From`                            Hot-spot rectangle on the page
:attr:`LinkInfo.Kind`                            Link kind
:attr:`LinkInfo.To`                              Destination point
:attr:`LinkInfo.Page`                            Target page number
:attr:`LinkInfo.Uri`                             Internet resource
:attr:`LinkInfo.File`                            Destination file
:attr:`LinkInfo.Name`                            Named destination name
:attr:`LinkInfo.NamedDest`                       Named destination key
:attr:`LinkInfo.Dest`                            Raw PDF dest string
:attr:`LinkInfo.Zoom`                            Zoom value
:attr:`LinkInfo.Id`                              Item id
:attr:`LinkInfo.Xref`                            PDF object number
:attr:`LinkInfo.Italic`                          Italic TOC text
:attr:`LinkInfo.Bold`                            Bold TOC text
:attr:`LinkInfo.Collapse`                        Folded TOC children
:attr:`LinkInfo.Color`                           RGB color
================================================ =======================================================


**Class API**

.. class:: LinkInfo

   .. attribute:: From

      Hot-spot location on the page.

      :type: :class:`Rect`

   .. attribute:: Kind

      Kind of link.

      :type: LinkType

   .. attribute:: To

      Destination location on the target page.

      :type: :class:`Point`

   .. attribute:: ToStr

      Destination as a string when no point is used.

      :type: string

   .. attribute:: Page

      Target page number.

      :type: int

   .. attribute:: Name

      Named destination name.

      :type: string

   .. attribute:: NamedDest

      Named destination key for ``LINK_NAMED``.

      :type: string

   .. attribute:: Dest

      Raw PDF dest string when the named target has no page.

      :type: string

   .. attribute:: Uri

      Destination internet resource.

      :type: string

   .. attribute:: Zoom

      Zoom value.

      :type: float

   .. attribute:: File

      Destination file.

      :type: string

   .. attribute:: Id

      Item id.

      :type: string

   .. attribute:: Xref

      :data:`xref` of the item (0 if not a PDF).

      :type: int

   .. attribute:: Italic

      True if italic TOC text. PDF only.

      :type: bool

   .. attribute:: Bold

      True if bold TOC text. PDF only.

      :type: bool

   .. attribute:: Collapse

      True if sub-items are folded. PDF only.

      :type: bool

   .. attribute:: Color

      Item color in PDF RGB format.

      :type: float[]

.. include:: ../footer.rst
