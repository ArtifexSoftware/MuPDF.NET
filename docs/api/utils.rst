.. include:: header.rst

============
Functions
============
The following are miscellaneous functions and attributes on a fairly low-level technical detail.

Some functions provide detail access to PDF structures. Others are stripped-down, high performance versions of other functions which provide more information.

Yet others are handy, general-purpose utilities.


==================================== ==============================================================
**Function**                         **Short Description**
==================================== ==============================================================
:meth:`adobe_glyph_unicodes`         list of unicodes defined in **Adobe Glyph List**
:meth:`ConversionHeader`             return header string for *get_text* methods
:meth:`sRGB2rgb`                     Convenience function returning a color (red, green, blue) for a given *sRGB* color integer.
:meth:`ConversionTrailer`            return trailer string for *get_text* methods
:meth:`EMPTY_IRECT`                  return the (standard) empty / invalid rectangle
:meth:`EMPTY_QUAD`                   return the (standard) empty / invalid quad
:meth:`EMPTY_RECT`                   return the (standard) empty / invalid rectangle
:meth:`GetPdfNow`                    return the current timestamp in PDF format
:meth:`GetPdfString`                 return PDF-compatible string
:meth:`GetTextLength`                return string length for a given font & :data:`fontsize`
:meth:`GlyphName2Unicode`            return unicode from a glyph name
:meth:`GetImageProfile`              return a dictionary of basic image properties
:meth:`INFINITE_RECT`                return the (only existing) infinite rectangle
:meth:`PaperRect`                    return rectangle for a known paper format
:meth:`PaperSize`                    return width, height for a known paper format
:meth:`PaperSizes`                   dictionary of pre-defined paper formats
:meth:`PlanishLine`                  matrix to map a line to the x-axis
:meth:`RecoverCharQuad`              compute the quad of a char ("rawdict")
:meth:`RecoverLineQuad`              compute the quad of a subset of line spans
:meth:`RecoverQuad`                  compute the quad of a span ("dict", "rawdict")
:meth:`RecoverSpanQuad`              compute the quad of a subset of span characters
:meth:`Unicode2GlyphName`            return glyph name from a unicode
:meth:`MakeAnnotDA`                  Passing color, fontname, fontsize into the annot.
:meth:`AddAnnotId`                   Add a unique /NM key to an annotation or widget.
:meth:`AddOcObject`                  Add OC object reference to a dictionary
:meth:`ColorCount`                   Return count of each color.
:meth:`BinFromBuffer`                Turn FzBuffer into a byte[]
:meth:`BufferFromBytes`              Make FzBuffer from a byte[] object.
:meth:`CalcImageMatrix`              Compute image insertion matrix
:meth:`CompressBuffer`               Compress FzBuffer into a new buffer
:attr:`TESSDATA_PREFIX`              a copy of `os.environ["TESSDATA_PREFIX"]`
==================================== ==============================================================

   .. method:: paper_size(s)

      Convenience function to return width and height of a known paper format code. These values are given in pixels for the standard resolution 72 pixels = 1 inch.

      Currently defined formats include **'A0'** through **'A10'**, **'B0'** through **'B10'**, **'C0'** through **'C10'**, **'Card-4x6'**, **'Card-5x7'**, **'Commercial'**, **'Executive'**, **'Invoice'**, **'Ledger'**, **'Legal'**, **'Legal-13'**, **'Letter'**, **'Monarch'** and **'Tabloid-Extra'**, each in either portrait or landscape format.

      A format name must be supplied as a string (case **in** \sensitive), optionally suffixed with "-L" (landscape) or "-P" (portrait). No suffix defaults to portrait.

      :arg str s: any format name from above in upper or lower case, like *"A4"* or *"letter-l"*.

      :rtype: tuple
      :returns: *(width, height)* of the paper format. For an unknown format *(-1, -1)* is returned. Examples: *pymupdf.paper_size("A4")* returns *(595, 842)* and *pymupdf.paper_size("letter-l")* delivers *(792, 612)*.

-----

   .. method:: paper_rect(s)

      Convenience function to return a :ref:`Rect` for a known paper format.

      :arg str s: any format name supported by :meth:`paper_size`.

      :rtype: :ref:`Rect`
      :returns: *pymupdf.Rect(0, 0, width, height)* with *width, height=pymupdf.paper_size(s)*.

      >>> import pymupdf
      >>> pymupdf.paper_rect("letter-l")
      pymupdf.Rect(0.0, 0.0, 792.0, 612.0)
      >>>

-----

   .. method:: sRGB2Pdf(int srgb)

      Convenience function returning a PDF color triple (red, green, blue) for a given sRGB color integer as it occurs in :meth:`Page.GetText` dictionaries "dict" and "rawdict".

      :arg int srgb: an integer of format RRGGBB, where each color component is an integer in range(255).

      :returns: a tuple (red, green, blue) with float items in interval *0 <= item <= 1* representing the same color. Example `sRGB2Pdf(0xff0000) = (1, 0, 0)` (red).

-----

   .. method:: sRGB2Rgb(int srgb)

      Convenience function returning a color (red, green, blue) for a given *sRGB* color integer.

      :arg int srgb: an integer of format RRGGBB, where each color component is an integer in range(255).

      :returns: a tuple (red, green, blue) with integer items in `range(256)` representing the same color. Example `sRGB2Pdf(0xff0000) = (255, 0, 0)` (red).

-----

   .. method:: GlyphName2Unicode(string name)

      Return the unicode number of a glyph name based on the **Adobe Glyph List**.

      :arg string name: the name of some glyph. The function is based on the `Adobe Glyph List <https://github.com/adobe-type-tools/agl-aglfn/blob/master/glyphlist.txt>`_.

      :rtype: int
      :returns: the unicode. Invalid *name* entries return `0xfffd (65533)`.

-----

   .. method:: Unicode2GlyphName(int ch)

      Return the glyph name of a unicode number, based on the **Adobe Glyph List**.

      :arg int ch: the unicode given by e.g. `ord("ß")`. The function is based on the `Adobe Glyph List <https://github.com/adobe-type-tools/agl-aglfn/blob/master/glyphlist.txt>`_.

      :rtype: string
      :returns: the glyph name. E.g. `Utils.Unicode2GlyphName(Convert.Int32("Ä"))` returns `'Adieresis'`.

-----

   .. method:: adobe_glyph_names()

      *New in v1.18.0*

      Return a list of glyph names defined in the **Adobe Glyph List**.

      :rtype: list
      :returns: list of strings.

      .. note:: A similar functionality is provided by package `fontTools <https://pypi.org/project/fonttools/>`_ in its *agl* sub-package.

-----

   .. method:: adobe_glyph_unicodes()

      *New in v1.18.0*

      Return a list of unicodes for there exists a glyph name in the **Adobe Glyph List**.

      :rtype: list
      :returns: list of integers.

      .. note:: A similar functionality is provided by package `fontTools <https://pypi.org/project/fonttools/>`_ in its *agl* sub-package.

-----

   .. method:: css_for_pymupdf_font(fontcode, *, CSS=None, archive=None, name=None)

      *New in v1.21.0*

      **Utility function for use with "Story" applications.**

      Create CSS `@font-face` items for the given fontcode in pymupdf-fonts. Creates a CSS font-family for all fonts starting with string "fontcode".

      The font naming convention in package pymupdf-fonts is "fontcode<sf>", where the suffix "sf" is one of "" (empty), "it"/"i", "bo"/"b" or "bi". These suffixes thus represent the regular, italic, bold or bold-italic variants of that font.

      For example, font code "notos" refers to fonts

      *  "notos" - "Noto Sans Regular"
      *  "notosit" - "Noto Sans Italic"
      *  "notosbo" - "Noto Sans Bold"
      *  "notosbi" - "Noto Sans Bold Italic"

      The function creates (up to) four CSS `@font-face` definitions and collectively assigns the `font-family` name "notos" to them (or the "name" value if provided). Associated font buffers are placed / added to the provided archive.

      To use the font in the Python API for :ref:`Story`, execute `.set_font(fontcode)` (or "name" if given). The correct font weight or style will automatically be selected as required.

      For example to replace the "sans-serif" HTML standard (i.e. Helvetica) with the above "notos", execute the following. Whenever "sans-serif" is used (whether explicitly or implicitly), the Noto Sans fonts will be selected.

      `CSS = pymupdf.css_for_pymupdf_font("notos", name="sans-serif", archive=...)`

      Expects and returns the CSS source, with the new CSS definitions appended.

      :arg str fontcode: one of the font codes present in package `pymupdf-fonts <https://pypi.org/project/pymupdf-fonts/>`_ (usually) representing the regular version of the font family.
      :arg str CSS: any already existing CSS source, or `None`. The function will append its new definitions to this. This is the string that **must be used** as `user_css` when creating the :ref:`Story`.
      :arg archive: :ref:`Archive`, **mandatory**. All font binaries (i.e. up to four) found for "fontcode" will be added to the archive. This is the archive that **must be used** as `archive` when creating the :ref:`Story`.
      :arg str name: the name under which the "fontcode" fonts should be found. If omitted, "fontcode" will be used.

      :rtype: str
      :returns: Modified CSS, with appended `@font-face` statements for each font variant of fontcode. Fontbuffers associated with "fontcode" will have been added to 'archive'. The function will automatically find up to 4 font variants. All pymupdf-fonts (that are no special purpose like math or music, etc.) have regular, bold, italic and bold-italic variants. To see currently available font codes check `pymupdf.fitz_fontdescriptors.keys()`. This will show something like `dict_keys(['cascadia', 'cascadiai', 'cascadiab', 'cascadiabi', 'figbo', 'figo', 'figbi', 'figit', 'fimbo', 'fimo', 'spacembo', 'spacembi', 'spacemit', 'spacemo', 'math', 'music', 'symbol1', 'symbol2', 'notosbo', 'notosbi', 'notosit', 'notos', 'ubuntu', 'ubuntubo', 'ubuntubi', 'ubuntuit', 'ubuntm', 'ubuntmbo', 'ubuntmbi', 'ubuntmit'])`.

      Here is a complete snippet for using the "Noto Sans" font instead of "Helvetica"::

         arch = pymupdf.Archive()
         CSS = pymupdf.css_for_pymupdf_font("notos", name="sans-serif", archive=arch)
         story = pymupdf.Story(user_css=CSS, archive=arch)


-----

   .. method:: PlanishLine(Point p1, Point p2)

      Return a matrix which maps the line from p1 to p2 to the x-axis such that p1 will become (0,0) and p2 a point with the same distance to (0,0).

      :arg Point p1: starting point of the line.
      :arg Point p2: end point of the line.

      :rtype: :ref:`Matrix`
      :returns: a matrix which combines a rotation and a translation::

         .. image:: images/img-planish.png
            :scale: 40


-----

   .. method:: PaperSize

      A dictionary of pre-defines paper formats. Used as basis for :meth:`PaperSize`.

-----

   .. attribute:: TESSDATA_PREFIX

      * New in v1.19.4

      Copy of `os.environ["TESSDATA_PREFIX"]` for convenient checking whether there is integrated Tesseract OCR support.

      If this attribute is `None`, Tesseract-OCR is either not installed, or the environment variable is not set to point to Tesseract's language support folder.

      .. note:: This variable is now checked before OCR functions are tried. This prevents verbose messages from MuPDF.

-----

   .. attribute:: pdfcolor

      * New in v1.19.6

      Contains about 500 RGB colors in PDF format with the color name as key. To see what is there, you can obviously look at `pymupdf.pdfcolor.keys()`.

      Examples:

        * `pymupdf.pdfcolor["red"] = (1.0, 0.0, 0.0)`
        * `pymupdf.pdfcolor["skyblue"] = (0.5294117647058824, 0.807843137254902, 0.9215686274509803)`
        * `pymupdf.pdfcolor["wheat"] = (0.9607843137254902, 0.8705882352941177, 0.7019607843137254)`

-----

   .. method:: GetPdfNow()

      Convenience function to return the current local timestamp in PDF compatible format, e.g. *D:20170501121525-04'00'* for local datetime May 1, 2017, 12:15:25 in a timezone 4 hours westward of the UTC meridian.

      :rtype: string
      :returns: current local PDF timestamp.

-----

   .. method:: GetTextLength(string text, string fontName: "helv", float fontSize: 11, int encoding: 0)

      Calculate the length of text on output with a given **builtin** font, :data:`fontSize` and encoding.

      :arg str text: the text string.
      :arg str fontName: the fontName. Must be one of either the :ref:`Base-14-Fonts` or the CJK fonts, identified by their "reserved" fontnames (see table in :meth:`Page.InsertFont`).
      :arg float fontSize: the :data:`fontSize`.
      :arg int encoding: the encoding to use. Besides 0 = Latin, 1 = Greek and 2 = Cyrillic (Russian) are available. Relevant for Base-14 fonts "Helvetica", "Courier" and "Times" and their variants only. Make sure to use the same value as in the corresponding text insertion.
      :rtype: float
      :returns: the length in points the string will have (e.g. when used in :meth:`Page.InsertText`).

      .. note:: This function will only do the calculation -- it won't insert font nor text.

      .. note:: The :ref:`Font` class offers a similar method, :meth:`Font.text_length`, which supports Base-14 fonts and any font with a character map (CMap, Type 0 fonts).

      .. warning:: If you use this function to determine the required rectangle width for the (:ref:`Page` or :ref:`Shape`) *InsertTextbox* methods, be aware that they calculate on a **by-character level**.

-----

   .. method:: GetPdfString(string text)

      Make a PDF-compatible string: if the text contains code points *ord(c) > 255*, then it will be converted to UTF-16BE with BOM as a hexadecimal character string enclosed in "<>" brackets like *<feff...>*. Otherwise, it will return the string enclosed in (round) brackets, replacing any characters outside the ASCII range with some special code. Also, every "(", ")" or backslash is escaped with a backslash.

      :arg str text: the object to convert

      :rtype: string
      :returns: PDF-compatible string enclosed in either *()* or *<>*.

-----

   .. method:: GetImageProfile(byte[] stream)

      Show important properties of an image provided as a memory area. Its main purpose is to avoid using other Python packages just to determine them.

      :arg bytes stream: either an image in memory or an **opened** file. An image in memory may be any of the formats `bytes`.

      :rtype: ImageInfo
      :returns:
         No exception is ever raised. In case of an error, `None` is returned. Otherwise, there are the following items::

         There is the following relation to **Exif** information encoded in `orientation`, and correspondingly in the `transform` matrix-like (quoted from MuPDF documentation, *ccw* = counter-clockwise):

            0. Undefined
            1. 0 degree ccw rotation. (Exif = 1)
            2. 90 degree ccw rotation. (Exif = 8)
            3. 180 degree ccw rotation. (Exif = 3)
            4. 270 degree ccw rotation. (Exif = 6)
            5. flip on X. (Exif = 2)
            6. flip on X, then rotate ccw by 90 degrees. (Exif = 5)
            7. flip on X, then rotate ccw by 180 degrees. (Exif = 4)
            8. flip on X, then rotate ccw by 270 degrees. (Exif = 7)


         .. note::

            * For some "exotic" images (FAX encodings, RAW formats and the like), this method will not work. You can however still work with such images in PyMuPDF, e.g. by using :meth:`Document.ExtractImage` or create pixmaps via `Pixmap(doc, xref)`. These methods will automatically convert exotic images to the PNG format before returning results.
            * You can also get the properties of images embedded in a PDF, via their :data:`xref`. In this case make sure to extract the raw stream: `pymupdf.GetImageProfile(doc.GetXrefStreamRaw(xref))`.
            * Images as returned by the image blocks of :meth:`Page.GetText` using "dict" or "rawdict" options are also supported.


-----

   .. method:: ConversionHeader(string i: "text", string filename: "UNKNOWN")

      Return the header string required to make a valid document out of page text outputs.

      :arg string i: type of document. Use the same as the output parameter of *GetText()*.

      :arg string filename: optional arbitrary name to use in output types "json" and "xml".

      :rtype: string

-----

   .. method:: ConversionTrailer(string i)

      Return the trailer string required to make a valid document out of page text outputs. See :meth:`Page.GetText` for an example.

      :arg string i: type of document. Use the same as the output parameter of *GetText()*.

      :rtype: string

-----

   .. method:: RecoverQuad((float, float) lineDir, Span span)

      Compute the quadrilateral of a text span extracted via options "dict" or "rawdict" of :meth:`Page.GetText`.

      :arg (float, float) lineDir: `line["dir"]` of the owning line.  Use `None` for a span from :meth:`Page.GetTextTrace`.
      :arg Span span: the span.
      :returns: the :ref:`Quad` of the span, usable for text marker annotations ('Highlight', etc.).

-----

   .. method:: RecoverCharQuad((float, float) lineDir, Span span, Char char)

      Compute the quadrilateral of a text character extracted via option "rawdict" of :meth:`Page.GetText`.

      :arg (float, float) lineDir: `line["dir"]` of the owning line. Use `None` for a span from :meth:`Page.GetTextTrace`.
      :arg Span span: the span.
      :arg Char char: the character.
      :returns: the :ref:`Quad` of the character, usable for text marker annotations ('Highlight', etc.).

-----

   .. method:: RecoverSpanQuad((float, float) lineDir, Span span, Char[] chars: None)

      Compute the quadrilateral of a subset of characters of a span extracted via option "rawdict" of :meth:`Page.GetText`.

      :arg (float, float) lineDir: `line["dir"]` of the owning line. Use `None` for a span from :meth:`Page.GetTextTrace`.
      :arg Span span: the span.
      :arg Char[] chars: the characters to consider. If given, the selected extraction option must be "rawdict".
      :returns: the :ref:`Quad` of the selected characters, usable for text marker annotations ('Highlight', etc.).

-----

   .. method:: RecoverLineQuad(Line line, List<Span> spans: None)

      Compute the quadrilateral of a subset of spans of a text line extracted via options "dict" or "rawdict" of :meth:`Page.GetText`.

      :arg Line line: the line.
      :arg Span[] spans: a sub-list of `line.Spans`. If omitted, the full line quad will be returned.
      :returns: the :ref:`Quad` of the selected line spans, usable for text marker annotations ('Highlight', etc.).

-----

   .. attr:: TESSDATA_PREFIX

      Return the name of Tesseract's language support folder. Use this function if the environment variable `TESSDATA_PREFIX` has not been set.

      :returns: `os.getenv("TESSDATA_PREFIX")` if not `None`. Otherwise, if Tesseract-OCR is installed, locate the name of `tessdata`. If no installation is found, return `false`.

         The folder name can be used as parameter `tessdata` in methods :meth:`Page.GetTextPageOcr`, :meth:`Pixmap.SavePdfOCR` and :meth:`Pixmap.PdfOCR2Bytes`.

-----

   .. method:: INFINITE_QUAD()

   .. method:: INFINITE_RECT()

   .. method:: INFINITE_IRECT()

      Return the (unique) infinite rectangle `Rect(-2147483648.0, -2147483648.0, 2147483520.0, 2147483520.0)`, resp. the :ref:`IRect` and :ref:`Quad` counterparts. It is the largest possible rectangle: all valid rectangles are contained in it.

-----

   .. method:: EMPTY_QUAD()

   .. method:: EMPTY_RECT()

   .. method:: EMPTY_IRECT()

      Return the "standard" empty and invalid rectangle `Rect(2147483520.0, 2147483520.0, -2147483648.0, -2147483648.0)` resp. quad. Its top-left and bottom-right point values are reversed compared to the infinite rectangle. It will e.g. be used to indicate empty bboxes in `page.get_text("dict")` dictionaries. There are however infinitely many empty or invalid rectangles.

-----

   .. method:: MakeAnnotDA(PdfAnnot annot, int nCol, float[] col, string font)

      Passing color, fontname, fontsize into the annot.

-----

   .. method:: AddAnnotId(PdfAnnot annot, string stem)

      Add a unique /NM key to an annotation or widget. Append a number to 'stem' such that the result is a unique name.

-----

   .. method:: AddOcObject(PdfDocument pdf, PdfObj ref, int xref)

      Add OC object reference to a dictionary

      :arg PdfDocument pdf: PdfDocument object to add OC-controlled
      :arg PdfObj ref: PdfObj to be added into pdf

-----

   .. method:: BinFromBuffer(FzBuffer buffer)

      Turn FzBuffer into a byte[]

      :arg FzBuffer buffer: FzBuffer used to convert to bytes
      :return: byte[] converted from FzBuffer

-----

   .. method:: BufferFromBytes(byte[] bytes)

      Make FzBuffer from a byte[] object.

      :return: FzBuffer from byte[]

-----

   .. method:: CalcImageMatrix(int width, int height, Rect tr, float rotate, bool keep)

      Compute image insertion matrix

      :arg int width: image width
      :arg int height: image height
      :arg Rect tr: rect of target image
      :arg float rotate: rotate to be set for target image
      :arg bool keep: calc size of target image keeping origin image's ratio
      
      :return: Matrix mat

-----

   .. method:: ColorCount(FzPixmap pm, dynamic clip)

      Return count of each color.

      :arg FzPixmap pm: source pixmap
      :arg Rect clip: count colors in clip area of source pixmaps.

      :return: return count of each color.

-----

   .. method:: CompressBuffer(FzBuffer buffer)

      Compress FzBuffer into a new buffer

      :arg FzBuffer buffer: input FzBuffer

      :return: output compressed FzBuffer object

-----

   .. method:: Construct

.. include:: footer.rst
