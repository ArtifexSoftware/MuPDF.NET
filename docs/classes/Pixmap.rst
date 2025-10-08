.. include:: ../header.rst

.. _Pixmap:

================
Pixmap
================


Pixmaps ("pixel maps") are objects at the heart of MuPDF's rendering capabilities. They represent plane rectangular sets of pixels. Each pixel is described by a number of bytes ("components") defining its color, plus an optional alpha byte defining its transparency.

There exist several ways to create a pixmap. Except the first one, all of them are available as overloaded constructors. A pixmap can be created ...

1. from a document page (method :meth:`Page.GetPixmap`)
2. empty, based on :ref:`ColorSpace` and :ref:`IRect` information
3. from a file
4. from an in-memory image
5. from a memory area of plain pixels
6. from an image inside a PDF document
7. as a copy of another pixmap

.. note:: A number of image formats is supported as input for points 3. and 4. above. See section :ref:`ImageFiles`.

Have a look at the :ref:`FAQ` section to see some pixmap usage "at work".

================================ ===================================================
**Method / Attribute**           **Short Description**
================================ ===================================================
:meth:`Pixmap.ClearWith`         Clear parts of the pixmap
:meth:`Pixmap.ColorCount`        Determine used colors
:meth:`Pixmap.ColorTopUsage`     Determine share of most used color
:meth:`Pixmap.Copy`              Copy parts of another pixmap
:meth:`Pixmap.GammaWith`         Apply a gamma factor to the pixmap
:meth:`Pixmap.InvertIrect`       Invert the pixels of a given area
:meth:`Pixmap.SavePdfOCR`        Save the pixmap as an OCRed 1-page PDF
:meth:`Pixmap.PdfOCR2Bytes`      Save the pixmap as an OCRed 1-page PDF
:meth:`Pixmap.GetPixel`          Return the value of a pixel
:meth:`Pixmap.Save`              Save the pixmap in a variety of formats
:meth:`Pixmap.SetAlpha`          Set alpha values
:meth:`Pixmap.SetDpi`            Set the image resolution
:meth:`Pixmap.SetOrigin`         Set pixmap x,y values
:meth:`Pixmap.SetPixel`          Set color and alpha of a pixel
:meth:`Pixmap.SetRect`           Set color and alpha of all pixels in a rectangle
:meth:`Pixmap.Shrink`            Reduce size keeping proportions
:meth:`Pixmap.TintWith`          Tint the pixmap
:meth:`Pixmap.ToBytes`           Return a memory area in a variety of formats
:meth:`Pixmap.Warp`              Return a pixmap made from a quad inside
:meth:`Pixmap.Dispose`           Disposes of the pixmap
:attr:`Pixmap.Alpha`             Transparency indicator
:attr:`Pixmap.ColorSpace`        Pixmap's :ref:`Colorspace`
:attr:`Pixmap.Digest`            MD5 hashcode of the pixmap
:attr:`Pixmap.IsMonoChrome`      Check if only black and white occur
:attr:`Pixmap.IsUniColor`        Check if only one color occurs
:attr:`Pixmap.IRect`             :ref:`IRect` of the pixmap
:attr:`Pixmap.N`                 Bytes per pixel
:attr:`Pixmap.SAMPLES_MV`        `memoryview` of pixel area
:attr:`Pixmap.SamplesPtr`        Pointer to pixel area
:attr:`Pixmap.SAMPLES`           `bytes` copy of pixel area
:attr:`Pixmap.Size`              Pixmap's total length
:attr:`Pixmap.Stride`            Size of one image row
:attr:`Pixmap.W`                 Pixmap width
:attr:`Pixmap.H`                 Pixmap height
:attr:`Pixmap.X`                 X-coordinate of top-left corner
:attr:`Pixmap.Xres`              Resolution in X-direction
:attr:`Pixmap.Y`                 Y-coordinate of top-left corner
:attr:`Pixmap.Yres`              Resolution in Y-direction
================================ ===================================================

**Class API**

.. class:: Pixmap

   .. method:: Pixmap(ColorSpace colorspace, IRect irect, bool alpha)

      **New empty pixmap:** Create an empty pixmap of size and origin given by the rectangle. So, *irect.TopLeft* designates the top left corner of the pixmap, and its width and height are *irect.Width* resp. *irect.Height*. Note that the image area is **not initialized** and will contain crap data -- use eg. :meth:`ClearWith` or :meth:`SetRect` to be sure.

      :arg colorspace: colorspace.
      :type colorspace: :ref:`Colorspace`

      :arg IRect irect: The pixmap's position and dimension.

      :arg bool alpha: Specifies whether transparency bytes should be included. Default is *false*.

   .. method:: Pixmap(ColorSpace colorspace, Pixmap source)

      **Copy and set colorspace:** Copy *source* pixmap converting colorspace. Any colorspace combination is possible, but source colorspace must not be *null*.

      :arg colorspace: desired **target** colorspace. This **may also be** *null*. In this case, a "masking" pixmap is created: its :attr:`Pixmap.Samples` will consist of the source's alpha bytes only.
      :type colorspace: :ref:`ColorSpace`

      :arg source: the source pixmap.
      :type source: *Pixmap*

   .. method:: Pixmap(self, Pixmap source, Pixmap mask)

      **Copy and add image mask:** Copy *source* pixmap, add an alpha channel with transparency data from a mask pixmap.

      :arg source: pixmap without alpha channel.
      :type source: :ref:`Pixmap`

      :arg mask: a mask pixmap. Must be a graysale pixmap.
      :type mask: :ref:`Pixmap`

   .. method:: Pixmap(Pixmap source, float width, float height, Rect clip: null)

      **Copy and scale:** Copy *source* pixmap, scaling new width and height values -- the image will appear stretched or shrunk accordingly. Supports partial copying. The source colorspace may be *null*.

      :arg source: the source pixmap.
      :type source: *Pixmap*

      :arg float width: desired target width.

      :arg float height: desired target height.

      :arg IRect clip: restrict the resulting pixmap to this region of the **scaled** pixmap.

      .. note:: If width or height do not *represent* integers, then the resulting pixmap **will have an alpha channel**.

   .. method:: Pixmap(Pixmap source, int alpha: 1)

      **Copy and add or drop alpha:** Copy *source* and add or drop its alpha channel. Identical copy if *alpha* equals *source.Alpha*. If an alpha channel is added, its values will be set to 255.

      :arg source: source pixmap.
      :type source: *Pixmap*

      :arg int alpha: whether the target will have an alpha channel, default and mandatory if source colorspace is *null*.

   .. method:: Pixmap(string filename)

      **From a file:** Create a pixmap from *filename*. All properties are inferred from the input. The origin of the resulting pixmap is *(0, 0)*.

      :arg str filename: Path of the image file.

   .. method:: Pixmap(byte[] stream)

      **From memory:** Create a pixmap from a memory area. All properties are inferred from the input. The origin of the resulting pixmap is *(0, 0)*.

      :arg byte[] stream: Data containing a complete, valid image.

   .. method:: Pixmap(ColorSpace colorspace, float width, float height, byte[] samples, bool alpha)

      **From plain pixels:** Create a pixmap from *samples*. Each pixel must be represented by a number of bytes as controlled by the *colorspace* and *alpha* parameters. The origin of the resulting pixmap is *(0, 0)*. This method is useful when raw image data are provided by some other program -- see :ref:`FAQ`.

      :arg colorspace: ColorSpace of image.
      :type colorspace: :ref:`ColorSpace`

      :arg float width: image width

      :arg float height: image height

      :arg byte[] samples:  an area containing all pixels of the image. Must include alpha values if specified.

      :arg bool alpha: whether a transparency channel is included.

      .. note::

         1. The following equation **must be true**: *(colorspace.n + alpha) * width * height == len(samples)*.
         2. Starting with version 1.14.13, the samples data are **copied** to the pixmap.


   .. method:: Pixmap(Document doc, int xref)

      **From a PDF image:** Create a pixmap from an image **contained in PDF** *doc* identified by its :data:`xref`. All pixmap properties are set by the image.

      :arg doc: an opened **PDF** document.
      :type doc: :ref:`Document`

      :arg int xref: the :data:`xref` of an image object. For example, you can make a list of images used on a particular page with :meth:`Document.GetPageImages`, which also shows the :data:`xref` numbers of each image.

   .. method:: ClearWith(int value, IRect irect)

      Initialize the samples area.

      :arg int value: if specified, values from 0 to 255 are valid. Each color byte of each pixel will be set to this value, while alpha will be set to 255 (non-transparent) if present. If omitted, then all bytes (including any alpha) are cleared to *0x00*.

      :arg IRect irect: the area to be cleared. Omit to clear the whole pixmap. Can only be specified, if *value* is also specified.

   .. method:: TintWith(int black, int white)

      Colorize a pixmap by replacing black and / or white with colors given as **sRGB integer** values. Only colorspaces :data:`CS_GRAY` and :data:`CS_RGB` are supported, others are ignored with a warning.

      If the colorspace is :data:`CS_GRAY`, the average *(red + green + blue)/3* will be taken. The pixmap will be changed in place.

      :arg int black: replace black with this value. Specifying 0x000000 makes no changes.
      :arg int white: replace white with this value. Specifying 0xFFFFFF makes no changes.

      Examples:

         * `TintWith(0x000000, 0xFFFFFF)` is a no-op.
         * `TintWith(0x00FF00, 0xFFFFFF)` changes black to green, leaves white intact.
         * `TintWith(0xFF0000, 0x0000FF)` changes black to red and white to blue.


   .. method:: GammaWith(float gamma)

      Apply a gamma factor to a pixmap, i.e. lighten or darken it. Pixmaps with colorspace *null* are ignored with a warning.

      :arg float gamma: *gamma = 1.0* does nothing, *gamma < 1.0* lightens, *gamma > 1.0* darkens the image.

   .. method:: Shrink(n)

      Shrink the pixmap by dividing both, its width and height by 2\ :sup:``n``.

      :arg int n: determines the new pixmap (samples) size. For example, a value of 2 divides width and height by 4 and thus results in a size of one 16\ :sup:`th` of the original. Values less than 1 are ignored with a warning.

      .. note:: Use this methods to reduce a pixmap's size retaining its proportion. The pixmap is changed "in place". If you want to keep original and also have more granular choices, use the resp. copy constructor above.

   .. method:: GetPixel(int x, int y)

      Return the value of the pixel at location (x, y) (column, line).

      :arg int x: the column number of the pixel. Must be in `range(pix.Width)`.
      :arg int y: the line number of the pixel, Must be in `range(pix.Height)`.

      :rtype: list
      :returns: a list of color values and, potentially the alpha value. Its length and content depend on the pixmap's colorspace and the presence of an alpha. For RGBA pixmaps the result would e.g. be *[r, g, b, a]*. All items are integers in `range(256)`.

   .. method:: SetPixel(int x, int y, float[] color)

      :arg int x: the column number of the pixel. Must be in `range(pix.Width)`.
      :arg int y: the line number of the pixel. Must be in `range(pix.Height)`.
      :arg float[] color: the desired pixel value given as a sequence of integers in `range(256)`. The length of the sequence must equal :attr:`Pixmap.N`, which includes any alpha byte.

   .. method:: SetRect(IRect bbox, byte[] color)

      :arg IRect irect: the rectangle to be filled with the value. The actual area is the intersection of this parameter and :attr:`Pixmap.IRect`. For an empty intersection (or an invalid parameter), no change will happen.
      :arg byte[] color: the desired value, given as a sequence of integers in `range(256)`. The length of the sequence must equal :attr:`Pixmap.N`, which includes any alpha byte.

      :rtype: bool
      :returns: *false* if the rectangle was invalid or had an empty intersection with :attr:`Pixmap.IRect`, else *true*.

      .. note::

         1. This method is equivalent to :meth:`Pixmap.SetPixel` executed for each pixel in the rectangle, but is obviously **very much faster** if many pixels are involved.
         2. This method can be used similar to :meth:`Pixmap.ClearWith` to initialize a pixmap with a certain color like this: *pix.set_rect(pix.irect, (255, 255, 0))* (RGB example, colors the complete pixmap with yellow).

   .. method:: SetOrigin(x, y)

      Set the x and y values of the pixmap's top-left point.

      :arg int x: x coordinate
      :arg int y: y coordinate


   .. method:: SetDpi(int xres, int yres)

      Set the resolution (dpi) in x and y direction.

      :arg int xres: resolution in x direction.
      :arg int yres: resolution in y direction.


   .. method:: SetAlpha(dynamic alphavalues: null, int premultiply: 1, dyanmic opaque: null, dynamic matte: null)

      Change the alpha values. The pixmap must have an alpha channel.

      :arg byte[] alphavalues: the new alpha values. If provided, its length must be at least *width * height*. If omitted (`null`), all alpha values are set to 255 (no transparency).
      :arg bool premultiply: Whether to premultiply color components with the alpha value.
      :arg list,tuple opaque: ignore the alpha value and set this color to fully transparent. A sequence of integers in `range(256)` with a length of :attr:`Pixmap.N`. Default is *null*. For example, a typical choice for RGB would be `opaque=(255, 255, 255)` (white).
      :arg list,tuple matte: preblending background color.


   .. method:: InvertIrect(IRect bbox: null)

      Invert the color of all pixels in :ref:`IRect` *irect*. Will have no effect if colorspace is *null*.

      :arg IRect irect: The area to be inverted. Omit to invert everything.

   .. method:: Copy(Pixmap source, IRect bbox)

      Copy the *irect* part of the *source* pixmap into the corresponding area of this one. The two pixmaps may have different dimensions and can each have :data:`CS_GRAY` or :data:`CS_RGB` colorspaces, but they currently **must** have the same alpha property [#f2]_. The copy mechanism automatically adjusts discrepancies between source and target like so:

      If copying from :data:`CS_GRAY` to :data:`CS_RGB`, the source gray-shade value will be put into each of the three rgb component bytes. If the other way round, *(r + g + b) / 3* will be taken as the gray-shade value of the target.

      Between *irect* and the target pixmap's rectangle, an "intersection" is calculated at first. This takes into account the rectangle coordinates and the current attribute values :attr:`Pixmap.x` and :attr:`Pixmap.y` (which you are free to modify for this purpose via :meth:`Pixmap.set_origin`). Then the corresponding data of this intersection are copied. If the intersection is empty, nothing will happen.

      :arg source: source pixmap.
      :type source: :ref:`Pixmap`

      :arg IRect irect: The area to be copied.

      .. note:: Example: Suppose you have two pixmaps, `pix1` and `pix2` and you want to copy the lower right quarter of `pix2` to `pix1` such that it starts at the top-left point of `pix1`. Use the following snippet:

      .. code-block:: cs

         // safeguard: set top-left of pix1 and pix2 to (0, 0)
         pix1.SetOrigin(0, 0)
         pix2.SetOrigin(0, 0)
         // compute top-left coordinates of pix2 region to copy
         int x1 = int(pix2.Width / 2)
         int y1 = int(pix2.Height / 2)
         pix2.SetOrigin(-x1, -y1)
         pix1.Copy(pix2, (0, 0, x1, y1))

      .. image:: ../images/img-pixmapcopy.*
         :scale: 20

   .. method:: Save(string filename, string output: null, int jpgQuality: 95)

      Save pixmap as an image file. Depending on the output chosen, only some or all colorspaces are supported and different file extensions can be chosen. Please see the table below.

      :arg string filename: The file to save to. May be provided as a string. In the latter two cases, the filename is taken from the resp. object. The filename's extension determines the image format, which can be overruled by the output parameter.

      :arg string output: The desired image format. The default is the filename's extension. If both, this value and the file extension are unsupported, an exception is raised. For possible values see :ref:`PixmapOutput`.
      :arg int jpgQuality: The desired image quality, default 95. Only applies to JPEG images, else ignored. This parameter trades quality against file size. A value of 98 is close to lossless. Higher values should not lead to better quality.

      :raises ValueError: For unsupported image formats.

   .. method:: ToBytes(string output: "png", int jpgQuality: 95)

      :arg string output: The desired image format. The default is "png". For possible values see :ref:`PixmapOutput`.
      :arg int jpgQuality: The desired image quality, default 95. Only applies to JPEG images, else ignored. This parameter trades quality against file size. A value of 98 is close to lossless. Higher values should not lead to better quality.

      :raises ValueError: For unsupported image formats.
      :rtype: byte[]

      :arg str output: The requested image format. The default is "png". For other possible values see :ref:`PixmapOutput`.

   .. method:: SavePdfOCR(string filename, bool compress: true, string language: "eng", string tessdata: null)

      Perform text recognition using Tesseract and save the image as a 1-page PDF with an OCR text layer.

      :arg string filename: identifies the file to save to. May be either a string or a pointer to a file opened with "wb".
      :arg bool compress: whether to compress the resulting PDF, default is `true`.
      :arg string language: the languages occurring in the image. This must be specified in Tesseract format. Default is "eng" for English. Use "+"-separated Tesseract language codes for multiple languages, like "eng+spa" for English and Spanish.
      :arg string tessdata: folder name of Tesseract's language support. If omitted, this information must be present as environment variable `TESSDATA_PREFIX`.

      .. note:: **Will fail** if Tesseract is not installed or if the environment variable "TESSDATA_PREFIX" is not set to the `tessdata` folder name and not provided as parameter.

   .. method:: PdfOCR2Bytes(bool compress: true, string language: "eng", string tessdata: null)

      Perform text recognition using Tesseract and convert the image to a 1-page PDF with an OCR text layer. Internally invokes :meth:`Pixmap.SavePdfOCR`.

      :returns: A 1-page PDF file in memory. Could be opened like `doc=new Document("pdf", pix.PdfOCR2Bytes())`, and text extractions could be performed on its `page=doc[0]`.
      
         .. note::
         
            Another possible use is insertion into some pdf. The following snippet reads the images of a folder and stores them as pages in a new PDF that contain an OCR text layer:

         .. code-block:: cs

            Document doc = new Document();
            foreach (string imgfile in Directory.GetFiles(folder))
            {
               Pixmap pix = new Pixmap(imgfile);
               Document imgpdf = new Document("pdf", pix.PdfOCR2Bytes());
               doc.InsertPdf(imgpdf);
               pix = null;
               imgpdf.Close();
            }
            doc.Save("ocr-images.pdf")

   ..  method:: Warp(Quad quad, float width, float height)

      Return a new pixmap by "warping" the quad such that the quad corners become the new pixmap's corners. The target pixmap's `IRect` will be `(0, 0, Width, Height)`.

      :arg Quad quad: a convex quad with coordinates inside :attr:`Pixmap.IRect` (including the border points).
      :arg int width: desired resulting width.
      :arg int height: desired resulting height.
      :returns: A new pixmap where the quad corners are mapped to the pixmap corners in a clockwise fashion: `quad.UpperLeft -> irect.TopLeft`, `quad.UpperRight -> irect.TopRight`, etc.
      :rtype: :ref:`Pixmap`

         .. image:: ../images/img-warp.*
              :scale: 40
              :align: center


   ..  method:: ColorCount(bool colors: false, dynamic clip: null)

      Determine the pixmap's unique colors and their count.

      :arg bool colors: If `true` return a dictionary of color pixels and their usage count, else just the number of unique colors.
      :arg Rect, Tuple clip: a rectangle inside :attr:`Pixmap.IRect`. If provided, only those pixels are considered. This allows inspecting sub-rectangles of a given pixmap directly -- instead of building sub-pixmaps.
      :rtype: Dictionary<string, int>
      :returns: either the number of colors, or a dictionary with the items `pixel: count`. The pixel key is a `bytes` object of length :attr:`Pixmap.N`.
      
         .. note:: To recover the **tuple** of a pixel, use `tuple(colors.keys()[i])` for the i-th item.

            * The response time depends on the pixmap's samples size and may be more than a second for very large pixmaps.
            * Where applicable, pixels with different alpha values will be treated as different colors.


   ..  method:: ColorTopUsage(dynamic clip: null)

      Return the most frequently used color and its relative frequency.

      :arg Rect, Tuple clip: A rectangle inside :attr:`Pixmap.Rect`. If provided, only those pixels are considered. This allows inspecting sub-rectangles of a given pixmap directly -- instead of building sub-pixmaps.
      :rtype: Tuple(float, byte[])
      :returns: A Tuple `(ratio, pixel)` where `0 < ratio <= 1` and *pixel* is the pixel value of the color. Use this to decide if the image is "almost" unicolor: a response `(0.95, b"\x00\x00\x00")` means that 95% of all pixels are black.

   .. method:: Dispose()

      Disposes of the pixmap.


   .. attribute:: Alpha

      Indicates whether the pixmap contains transparency information.

      :type: bool

   .. attribute:: Digest

      The MD5 hashcode (16 bytes) of the pixmap. This is a technical value used for unique identifications.

      :type: byte[]

   .. attribute:: ColorSpace

      The colorspace of the pixmap. This value may be *null* if the image is to be treated as a so-called *image mask* or *stencil mask* (currently happens for extracted PDF document images only).

      :type: :ref:`Colorspace`

   .. attribute:: Stride

      Contains the length of one row of image data in :attr:`Pixmap.samples`. This is primarily used for calculation purposes. The following expressions are true:

      * `len(samples) == height * stride`
      * `width * n == stride`

      :type: int


   .. attribute:: IsMonoChrome

      Is `true` for a gray pixmap which only has the colors black and white.

      :type: bool


   .. attribute:: IsUniColor

      Is `true` if all pixels are identical (any colorspace). Where applicable, pixels with different alpha values will be treated as different colors.

      :type: bool


   .. attribute:: IRect

      Contains the :ref:`IRect` of the pixmap.

      :type: :ref:`IRect`

   .. attribute:: SAMPLES

      The color and (if :attr:`Pixmap.Alpha` is true) transparency values for all pixels. It is an area of `width * height * n` bytes. Each n bytes define one pixel. Each successive n bytes yield another pixel in scanline order. Subsequent scanlines follow each other with no padding. E.g. for an RGBA colorspace this means, *samples* is a sequence of bytes like *..., R, G, B, A, ...*, and the four byte values R, G, B, A define one pixel.


      .. note::
         * The underlying data is typically a **large** memory area, from which a `bytes` copy is made for this attribute ... each time you access it: for example an RGB-rendered letter page has a samples size of almost 1.4 MB. So consider assigning a new variable to it or use the `memoryview` version :attr:`Pixmap.SAMPLES_MV`.
         * Any changes to the underlying data are available only after accessing this attribute again. This is different from using the memoryview version.

      :type: bytes

   .. attribute:: SAMPLES_MV

      `Memory<byte>` format. It is built pointing to the memory in the pixmap -- not from a copy of it. So its creation speed is independent from the pixmap size, and any changes to pixels will be available immediately.
      
      We also have `len(pix.SAMPLES) == len(pix.SAMPLES_MV)`.

      :type: Memory<byte>

   .. attribute:: SamplesPtr

      Both of the above lead to the same Qt image, but (2) can be **many hundred times faster**, because it avoids an additional copy of the pixel area.

      :type: long

   .. attribute:: Size

      Contains *len(pixmap)*. This will generally equal *len(pix.samples)* plus some platform-specific value for defining other attributes of the object.

      :type: float

   .. attribute:: W

   .. attribute:: w

      Width of the region in pixels.

      :type: float

   .. attribute:: H

   .. attribute:: h

      Height of the region in pixels.

      :type: float

   .. attribute:: X

      X-coordinate of top-left corner in pixels. Cannot directly be changed -- use :meth:`Pixmap.SetOrigin`.

      :type: float

   .. attribute:: Y

      Y-coordinate of top-left corner in pixels. Cannot directly be changed -- use :meth:`Pixmap.SetOrigin`.

      :type: float

   .. attribute:: N

      Number of components per pixel. This number depends on colorspace and alpha. If colorspace is not *null* (stencil masks), then *Pixmap.n - Pixmap.alpha == pixmap.colorspace.n* is true. If colorspace is *null*, then *n == alpha == 1*.

      :type: float

   .. attribute:: Xres

      Horizontal resolution in dpi (dots per inch). Please also see :data:`Resolution`. Cannot directly be changed -- use :meth:`Pixmap.SetDpi`.

      :type: float

   .. attribute:: Yres

      Vertical resolution in dpi (dots per inch). Please also see :data:`Resolution`. Cannot directly be changed -- use :meth:`Pixmap.SetDpi`.

      :type: float

.. _ImageFiles:

Supported Input Image Formats
-----------------------------------------------
The following file types are supported as **input** to construct pixmaps: **BMP, JPEG, GIF, TIFF, JXR, JPX**, **PNG**, **PAM** and all of the **Portable Anymap** family (**PBM, PGM, PNM, PPM**). This support is two-fold:

1. Directly create a pixmap with *Pixmap(filename)* or *Pixmap(byte[])*. The pixmap will then have properties as determined by the image.

2. Open such files with *Document(filename)*. The result will then appear as a document containing one single page. Creating a pixmap of this page offers all the options available in this context: apply a matrix, choose colorspace and alpha, confine the pixmap to a clip area, etc.

**SVG images** are only supported via method 2 above, not directly as pixmaps. But remember: the result of this is a **raster image** as is always the case with pixmaps [#f1]_.

.. _PixmapOutput:

Supported Output Image Formats
---------------------------------------------------------------------------
A number of image **output** formats are supported. You have the option to either write an image directly to a file (:meth:`Pixmap.Save`), or to generate a byte[] object (:meth:`Pixmap.ToBytes`). Both methods accept a string identifying the desired format (**Format** column below). Please note that not all combinations of pixmap colorspace, transparency support (alpha) and image format are possible.

========== =============== ========= ============== =================================
**Format** **Colorspaces** **alpha** **Extensions** **Description**
========== =============== ========= ============== =================================
jpg, jpeg  gray, rgb, cmyk no        .jpg, .jpeg    Joint Photographic Experts Group 
pam        gray, rgb, cmyk yes       .pam           Portable Arbitrary Map
pbm        gray, rgb       no        .pbm           Portable Bitmap
pgm        gray, rgb       no        .pgm           Portable Graymap
png        gray, rgb       yes       .png           Portable Network Graphics
pnm        gray, rgb       no        .pnm           Portable Anymap
ppm        gray, rgb       no        .ppm           Portable Pixmap
ps         gray, rgb, cmyk no        .ps            Adobe PostScript Image
psd        gray, rgb, cmyk yes       .psd           Adobe Photoshop Document
========== =============== ========= ============== =================================

.. note::
    * Not all image file types are supported (or at least common) on all OS platforms. E.g. PAM and the Portable Anymap formats are rare or even unknown on Windows.
    * Especially pertaining to CMYK colorspaces, you can always convert a CMYK pixmap to an RGB pixmap with *rgbPix = new Pixmap(Utils.csRGB, cmyk_pix)* and then save that in the desired format.
    * As can be seen, MuPDF's image support range is different for input and output. Among those supported both ways, PNG and JPEG are probably the most popular.
    * We also recommend using "ppm" formats as input to tkinter's *PhotoImage* method like this: *tkimg = tkinter.PhotoImage(data=pix.ToBytes("ppm"))* (also see the tutorial). This is **very** fast (**60 times** faster than PNG).



.. rubric:: Footnotes

.. [#f1] If you need a **vector image** from the SVG, you must first convert it to a PDF. Try :meth:`Document.Convert2Pdf`.

.. [#f2] To also set the alpha property, add an additional step to this method by dropping or adding an alpha channel to the result.

.. include:: ../footer.rst
