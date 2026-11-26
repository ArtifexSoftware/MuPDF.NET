.. include:: ../header.rst

.. _ImageInfo:

================
ImageInfo
================


Image metadata returned by :meth:`Document.ExtractImage`, :meth:`Page.GetImageInfo`, and :meth:`Utils.GetImageProfile`.


================================================ =======================================================
**Attribute**                                    **Short Description**
================================================ =======================================================
:attr:`ImageInfo.Ext`                            Image type / file extension
:attr:`ImageInfo.Smask`                          Stencil (/SMask) xref, or 0
:attr:`ImageInfo.Width`                          Image width
:attr:`ImageInfo.Height`                         Image height
:attr:`ImageInfo.ColorSpace`                     Number of colorspace components
:attr:`ImageInfo.Bpc`                            Bits per component
:attr:`ImageInfo.Xres`                           Horizontal resolution
:attr:`ImageInfo.Yres`                           Vertical resolution
:attr:`ImageInfo.CsName`                         Colorspace name
:attr:`ImageInfo.Image`                          Image bytes when present
:attr:`ImageInfo.Orientation`                    Exif orientation
:attr:`ImageInfo.Matrix`                         Transform matrix
================================================ =======================================================


**Class API**

.. class:: ImageInfo

   .. attribute:: Ext

      Image type (for example ``"jpeg"``), usable as a file extension.

      :type: string

   .. attribute:: Smask

      :data:`xref` of a stencil (/SMask) image, or 0.

      :type: int

   .. attribute:: Width

      Image width.

      :type: float

   .. attribute:: Height

      Image height.

      :type: float

   .. attribute:: ColorSpace

      Number of colorspace components.

      :type: int

   .. attribute:: Bpc

      Bits per component.

      :type: int

   .. attribute:: Xres

      Horizontal resolution.

      :type: float

   .. attribute:: Yres

      Vertical resolution.

      :type: float

   .. attribute:: CsName

      Colorspace name.

      :type: string

   .. attribute:: Image

      Image bytes when the caller requested content.

      :type: byte[]

   .. attribute:: Orientation

      Exif orientation.

      :type: byte

   .. attribute:: Matrix

      Transform matrix.

      :type: :class:`Matrix`

.. include:: ../footer.rst
