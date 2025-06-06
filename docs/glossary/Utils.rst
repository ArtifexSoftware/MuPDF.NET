.. include:: header.rst

============
Utils
============

The following are miscellaneous functions and attributes on a fairly low-level technical detail.

Some functions provide detail access to PDF structures. Others are stripped-down, high performance versions of other functions which provide more information.

Yet others are handy, general-purpose utilities.


==================================== ==============================================================
**Function**                         **Short Description**
==================================== ==============================================================
:meth:`ConversionHeader`             Return header string for *get_text* methods
:meth:`sRGB2rgb`                     Convenience function returning a color (red, green, blue) for a given *sRGB* color integer
:meth:`ConversionTrailer`            Return trailer string for *get_text* methods
:meth:`EMPTY_IRECT`                  Return the (standard) empty / invalid rectangle
:meth:`EMPTY_QUAD`                   Return the (standard) empty / invalid quad
:meth:`EMPTY_RECT`                   Return the (standard) empty / invalid rectangle
:meth:`INFINITE_IRECT`               Return the (only existing) infinite rectangle
:meth:`INFINITE_QUAD`                Return the (only existing) infinite quad
:meth:`INFINITE_RECT`                Return the (only existing) infinite rectangle
:meth:`GetPdfNow`                    Return the current timestamp in PDF format
:meth:`GetPdfString`                 Return PDF-compatible string
:meth:`GetTextLength`                Return string length for a given font & :data:`fontsize`
:meth:`GlyphName2Unicode`            Return unicode from a glyph name
:meth:`GetImageProfile`              Return a dictionary of basic image properties
:meth:`INFINITE_RECT`                Return the (only existing) infinite rectangle
:meth:`PaperRect`                    Return rectangle for a known paper format
:meth:`PaperSize`                    Return width, height for a known paper format
:meth:`PaperSizes`                   Dictionary of pre-defined paper formats
:meth:`PlanishLine`                  Matrix to map a line to the x-axis
:meth:`RecoverCharQuad`              Compute the quad of a char ("rawdict")
:meth:`RecoverLineQuad`              Compute the quad of a subset of line spans
:meth:`RecoverQuad`                  Compute the quad of a span ("dict", "rawdict")
:meth:`RecoverSpanQuad`              Compute the quad of a subset of span characters
:meth:`Unicode2GlyphName`            Return glyph name from a unicode
:meth:`MakeAnnotDA`                  Passing color, fontname, fontsize into the annotation
:meth:`AddAnnotId`                   Add a unique /NM key to an annotation or widget
:meth:`AddOcObject`                  Add OC object reference to a dictionary
:meth:`ColorCount`                   Return count of each color
:meth:`BinFromBuffer`                Turn FzBuffer into a byte[]
:meth:`BufferFromBytes`              Make FzBuffer from a byte[] object
:meth:`CalcImageMatrix`              Compute image insertion matrix
:meth:`CompressBuffer`               Compress FzBuffer into a new buffer
:meth:`GetPdfString`                 Make a PDF-compatible string
:meth:`ConstructLabel`               Construct a label based on style, prefix and page number
:meth:`DecodeRawUnicodeEscape`       Decode raw unicode
:meth:`DoLinks`                      Insert links contained in copied page range into destination PDF
:meth:`EnsureIdentity`               Store ID in PDF trailer
:meth:`ExpandFontName`               Make /DA string of annotation
:meth:`GetId`                        Count numbers and return unique id on one process
:meth:`GetAllContents`               All /Contents streams concatenated to one bytes object
:meth:`GetAnnotByName`               Retrieve annotation by name (/NM key)
:meth:`GetArea`                      Calculate area of rectangle
:meth:`GetBorderStyle`               Return `int` meaning PdfObj "border style" from string type
:meth:`GetColors`                    Retrieve the red, green, blue triple of a color name
:meth:`GetColorHSV`                  Retrieve the hue, saturation, value triple of a color name
:meth:`GetColorInfoList`             Returns Tuples containing color name, red, green, blue color values
:meth:`GetDestString`                Calculate the PDF action string
:meth:`GetFieldTypeText`             Returns field type string from `int` type
:meth:`GetFontProperties`            Returns properties of the font having xref in PDF
:meth:`GetGlyphText`                 Adobe Glyph List function
:meth:`GetImageExtension`            Return extension for MuPDF image type
:meth:`GetLinkText`                  Define skeletons for `/Annots` object texts
:meth:`GetTextWithLayout`            Retrieves the text content of a page that retains layout.
:meth:`GetTables`                    Return the tables detected from a `Page` instance
:meth:`GetWidgetProperties`          Populate a Widget object with the values from a PDF form field
:meth:`InsertContents`               Insert a buffer as a new separate `/Contents` object of a page
:meth:`Integer2Letter`               Return letter sequence string for integer `i`
:meth:`Integer2Roman`                Return roman numeral for integer `i`
:meth:`MeasureString`                Calculate the width of the text
:meth:`MergeRange`                   Copy a range of pages (spage, epage) from a source PDF to a specified location (apage) of the target PDF
:meth:`NormalizeRotation`            Return normalized /Rotate value:one of 0, 90, 180, 270
:meth:`RuleDict`                     Make a Label from a PDF page label rule
:meth:`WriteBarcode`                 Writes a barcode to an image file
:meth:`ReplaceBytes`                 Replaces bytes froma source buffer
:attr:`TESSDATA_PREFIX`              A copy of `os.environ["TESSDATA_PREFIX"]`
==================================== ==============================================================


   .. method:: PaperSize

      Convenience function to return width and height of a known paper format code. These values are given in pixels for the standard resolution 72 pixels = 1 inch.

      Currently defined formats include **'A0'** through **'A10'**, **'B0'** through **'B10'**, **'C0'** through **'C10'**, **'Card-4x6'**, **'Card-5x7'**, **'Commercial'**, **'Executive'**, **'Invoice'**, **'Ledger'**, **'Legal'**, **'Legal-13'**, **'Letter'**, **'Monarch'** and **'Tabloid-Extra'**, each in either portrait or landscape format.

      A format name must be supplied as a string (case **in** \sensitive), optionally suffixed with "-L" (landscape) or "-P" (portrait). No suffix defaults to portrait.

      :arg s: any format name from above in upper or lower case, like *"A4"* or *"letter-l"*.

      :rtype: Tuple
      :returns: *(width, height)* of the paper format. For an unknown format *(-1, -1)* is returned. Examples: *PaperSize("A4")* returns *(595, 842)* and *PaperSize("letter-l")* delivers *(792, 612)*.

-----

   .. method:: PaperRect

      Convenience function to return a :ref:`Rect` for a known paper format.

      :arg string size: any format name supported by :meth:`PaperSize`.

      :rtype: :ref:`Rect`
      :returns: *Rect(0, 0, width, height)* with *width, height = Utils.PaperSize(size)*.

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

      :rtype: `int`
      :returns: the unicode. Invalid *name* entries return `0xfffd (65533)`.

-----

   .. method:: Unicode2GlyphName(int ch)

      Return the glyph name of a unicode number, based on the **Adobe Glyph List**.

      :arg int ch: the unicode given by e.g. `ord("ß")`. The function is based on the `Adobe Glyph List <https://github.com/adobe-type-tools/agl-aglfn/blob/master/glyphlist.txt>`_.

      :rtype: `string`
      :returns: the glyph name. E.g. `Utils.Unicode2GlyphName(Convert.Int32("Ä"))` returns `'Adieresis'`.

-----

   .. method:: PlanishLine(Point p1, Point p2)

      Return a matrix which maps the line from `p1` to `p2` to the x-axis such that `p1` will become (0,0) and `p2` a point with the same distance to (0,0).

      :arg Point p1: starting point of the line.
      :arg Point p2: end point of the line.

      :rtype: :ref:`Matrix`
      :returns: a matrix which combines a rotation and a translation

         .. image:: images/img-planish.png
            :scale: 40

-----

   .. attribute:: PaperSizes

      A dictionary of pre-defines paper formats. Used as basis for :meth:`PaperSize`.

-----

   .. attribute:: TESSDATA_PREFIX

      Copy of `os.environ["TESSDATA_PREFIX"]` for convenient checking whether there is integrated Tesseract OCR support.

      If this attribute is `None`, Tesseract-OCR is either not installed, or the environment variable is not set to point to Tesseract's language support folder.

      .. note:: This variable is now checked before OCR functions are tried. This prevents verbose messages from MuPDF.

-----

   .. method:: GetPdfNow()

      Convenience function to return the current local timestamp in PDF compatible format, e.g. *D:20170501121525-04'00'* for local datetime May 1, 2017, 12:15:25 in a timezone 4 hours westward of the UTC meridian.

      :rtype: `string`
      :returns: current local PDF timestamp.

-----

   .. method:: GetTextLength(string text, string fontName: "helv", float fontSize: 11, int encoding: 0)

      Calculate the length of text on output with a given **built in** font, :data:`fontSize` and encoding.

      :arg string text: the text string.
      :arg string fontName: the fontName. Must be one of either the :ref:`Base-14-Fonts` or the CJK fonts, identified by their "reserved" fontnames (see table in :meth:`Page.InsertFont`).
      :arg float fontSize: the :data:`fontSize`.
      :arg int encoding: the encoding to use. Besides 0 = Latin, 1 = Greek and 2 = Cyrillic (Russian) are available. Relevant for Base-14 fonts "Helvetica", "Courier" and "Times" and their variants only. Make sure to use the same value as in the corresponding text insertion.
      :rtype: `float`
      :returns: the length in points the string will have (e.g. when used in :meth:`Page.InsertText`).

      .. note:: This function will only do the calculation -- it won't insert font nor text.

      .. note:: The :ref:`Font` class offers a similar method, :meth:`Font.text_length`, which supports Base-14 fonts and any font with a character map (CMap, Type 0 fonts).

      .. warning:: If you use this function to determine the required rectangle width for the (:ref:`Page` or :ref:`Shape`) *InsertTextbox* methods, be aware that they calculate on a **by-character level**.

-----

   .. method:: GetPdfString(string text)

      Make a PDF-compatible string: if the text contains code points *Convert.ToInt32(c) > 255*, then it will be converted to UTF-16BE with BOM as a hexadecimal character string enclosed in "<>" brackets like *<feff...>*. Otherwise, it will return the string enclosed in (round) brackets, replacing any characters outside the ASCII range with some special code. Also, every "(", ")" or backslash is escaped with a backslash.

      :arg string text: the object to convert

      :rtype: `string`
      :returns: PDF-compatible string enclosed in either *()* or *<>*.

-----

   .. method:: GetImageProfile(byte[] stream)

      Show important properties of an image provided as a memory area. Its main purpose is to avoid using other libraries just to determine them.

      :arg byte[] stream: either an image in memory or an **opened** file. An image in memory may be any of the formats `bytes`.

      :rtype: `ImageInfo`
      :returns:
         No exception is ever raised. In case of an error, `None` is returned. Otherwise, there are the following items::

         There is the following relation to **Exif** information encoded in `orientation`, and correspondingly in the `transform` matrix-like (quoted from MuPDF documentation, *ccw* = counter-clockwise):

            0. Undefined
            1. 0 degree ccw rotation. (Exif = 1)
            2. 90 degree ccw rotation. (Exif = 8)
            3. 180 degree ccw rotation. (Exif = 3)
            4. 270 degree ccw rotation. (Exif = 6)
            5. Flip on X. (Exif = 2)
            6. Flip on X, then rotate ccw by 90 degrees. (Exif = 5)
            7. Flip on X, then rotate ccw by 180 degrees. (Exif = 4)
            8. Flip on X, then rotate ccw by 270 degrees. (Exif = 7)


         .. note::

            * For some "exotic" images (FAX encodings, RAW formats and the like), this method will not work. You can however still work with such images in MupDF.NET, e.g. by using :meth:`Document.ExtractImage` or create pixmaps via `Pixmap(doc, xref)`. These methods will automatically convert exotic images to the PNG format before returning results.
            * You can also get the properties of images embedded in a PDF, via their :data:`xref`. In this case make sure to extract the raw stream: `Utils.GetImageProfile(doc.GetXrefStreamRaw(xref))`.
            * Images as returned by the image blocks of :meth:`Page.GetText` using "dict" or "rawdict" options are also supported.


-----

   .. method:: ConversionHeader(string i: "text", string filename: "UNKNOWN")

      Return the header string required to make a valid document out of page text outputs.

      :arg string i: type of document. Use the same as the output parameter of *GetText()*.

      :arg string filename: optional arbitrary name to use in output types "json" and "xml".

      :rtype: `string`

-----

   .. method:: ConversionTrailer(string i)

      Return the trailer string required to make a valid document out of page text outputs. See :meth:`Page.GetText` for an example.

      :arg string i: type of document. Use the same as the output parameter of *GetText()*.

      :rtype: `string`

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

   .. method:: CalcImageMatrix(int width, int height, Rect tr, float rotate, bool keep)

      Compute image insertion matrix

      :arg int width: image width
      :arg int height: image height
      :arg Rect tr: rect of target image
      :arg float rotate: rotate to be set for target image
      :arg bool keep: calc size of target image keeping origin image's ratio
      
      :returns: `Matrix`

-----

   .. method:: ColorCount(FzPixmap pm, dynamic clip)

      Return count of each color.

      :arg FzPixmap pm: source pixmap
      :arg Rect clip: count colors in clip area of source pixmaps.

      :returns: return count of each color.

-----

   .. method:: ConstructLabel(string style, string prefix, int pno)

      Construct a label based on style, prefix and page number.

      :arg string style: type of style for label. That includes `D`, `r`, `R`, `a`, `A`.
      :arg string prefix: added prefix to label
      :arg int pno: translate `pno` to letter according to the style

      :rtype: `string`
      :returns: styled label

-----

   .. method:: DecodeRawUnicodeEscape(FzBuffer s)
   .. method:: DecodeRawUnicodeEscape(string s)

      Decode raw unicode

-----

   .. method:: DoLinks(Document doc1, Document doc2, int fromPage: -1, int toPage: -1, int startAt: -1)

      Insert links contained in copied page range into destination PDF.

-----

   .. method:: EnsureIdentity(Document pdf)

      Store ID in PDF trailer

-----

   .. method:: ExpandFontName(string fontname)

      Make `/DA` string of annotation

      :returns: expand font name. For example, if `fontname` starts with `co` or `Co`, returns `Cour`

-----

   .. method:: GetId()

      Count numbers and return unique id on one process

      :rtype: `int`
      :returns: unique number

-----

   .. method:: GetAllContents(Page page)

      All `/Contents` streams concatenated to one bytes object.

      :arg Page page: Page object to get all streams

      :rtype: `byte[]`

-----

   .. method:: GetAnnotByName(Page page, string name)

      Retrieve annotation by name (/NM key)

      :arg Page page: Page object containing annotations.
      :arg string name: annotation name. Looping annotations in page, that find the annotation which has name as `ID`.

      :rtype: `PdfAnnot`
      :returns: PdfAnnot object that has the name.

-----

   .. method:: GetArea(Rect rect, string unit: "px")

      Calculate area of rectangle. Parameter is one of 'px' (default), 'in', 'cm', or 'mm'.

      :arg Rect rect: rectangle calculated area
      :arg string unit: unit used in rect. default is `px`, there are other units like `cm`, `mm`.

      :rtype: `float`
      :returns: area of rectangle

-----

   .. method:: GetBorderStyle(string style)

      Return int meaning `PdfObj` "border style" from string type.

      :arg string style: border type in format of style. style can be one of 'B', 'D', 'I', 'U', 'S' or lowercases.
      :returns: return int from border pdfobj.

-----

   .. method:: GetColors(string name)

      Retrieve the red, green, blue triple of a color name.

      :arg string name: color name
      :rtype: float[]
      :returns: return float array equal to color name. If invalid color name, return (1, 1, 1) - `white`.

-----

   .. method:: GetColorHSV(string name)

      Retrieve the hue, saturation, value triple of a color name.

      :returns: a triple (degree, percent, percent). If not found (-1, -1, -1) is returned.

-----

   .. method:: GetColorInfoList()

      Return Tuples containing of color name, red, green, blue color values.

-----

   .. method:: GetDestString(int xref, int dDict)
   .. method:: GetDestString(int xref, float dDict)
   .. method:: GetDestString(int xref, LinkInfo dDict)

      Calculate the PDF action string.

      :arg int xref: the :data:`xref` of the link pdfobj.
      :arg int dDict: one parameter of coordinate positioned in PDF
      :arg float dDict: one parameter of coordinate positioned in PDF
      :arg LinkInfo dDict: link info contains 'Kind', 'From', 'To', 'Page', 'Xref', ...

      :returns: string of PDF action

-----

   .. method:: GetFieldTypeText(int wtype)

      Return field type string from int type.

-----

   .. method:: GetFontProperties(Document doc, int xref)

      Return properties of the font having xref in PDF. Properties are 'Name', 'Extension', 'Type', 'Asc', 'Desc'.

      :arg int xref: the :data:`xref` of the font
      :arg Document doc: source PDF that has the font.

      :rtype: Tuple(string, string, string, float, float)
      :returns: properties of the font.

-----

   .. method:: GetGlyphText()

      Adobe Glyph List function

-----

   .. method:: GetImageExtension(int type)

      Return extension for MuPDF image type.

-----
   
   .. method:: GetLinkText(Page page, LinkInfo link)

      Define skeletons for /Annots object texts

      :returns: annot string.

-----

   .. method:: GetTextWithLayout(Page page, Rect clip: null, int flags: 0, int tolerance: 5)

      Retrieves the text content of a page that retains layout. Positioning of text is adjusted by spaces.

      :arg Page page: The page instance.
      :arg Rect clip: Specify a region to consider within the page rectangle and ignore the rest. Default `null` is the full page.
      :arg int flags: Indicator bits to control whether to include images or how text should be handled with respect to white spaces and ligatures.
      :arg int tolerance: Neighborhood threshold.

      :rtype: string
      :return: a string containing the text with layout applied.

-----

   .. method:: GetTables(Page page, Rect clip: null,  string strategy: null, string vertical_strategy: "lines", string horizontal_strategy: "lines", List<Line> add_lines: null, List<Edge> vertical_lines: null, List<Edge> horizontal_lines: null, float snap_tolerance: TableFlags.TABLE_DEFAULT_SNAP_TOLERANCE, float snap_x_tolerance: 0.0f, float snap_y_tolerance: 0.0f, float join_tolerance: TableFlags.TABLE_DEFAULT_JOIN_TOLERANCE, float join_x_tolerance: 0.0f, float join_y_tolerance: 0.0f, float edge_min_length: 3.0f, float min_words_vertical: TableFlags.TABLE_DEFAULT_MIN_WORDS_VERTICAL, float min_words_horizontal: TableFlags.TABLE_DEFAULT_MIN_WORDS_HORIZONTAL, float intersection_tolerance: 3.0f, float intersection_x_tolerance: 0.0f, float intersection_y_tolerance: 0.0f, float text_tolerance: 3.0f, float text_x_tolerance: 3.0f, float text_y_tolerance: 3.0f)

      Find tables on the page and return a list with related information. Typically, the default values of the many parameters will be sufficient. Adjustments should ever only be needed in corner case situations.

      :arg Page page: The page instance to use for table detection.

      :arg Rect clip: specify a region to consider within the page rectangle and ignore the rest. Default `null` is the full page.

      :arg str strategy: Request a **table detection** strategy. Valid values are "lines", "lines_strict" and "text".

         Default is **"lines"** which uses all vector graphics on the page to detect grid lines.

         Strategy **"lines_strict"** ignores borderless rectangle vector graphics. Sometimes single text pieces have background colors which may lead to false columns or lines. This strategy ignores them and can thus increase detection precision.

         If **"text"** is specified, text positions are used to generate "virtual" column and / or row boundaries. Use `min_words_*` to request the number of words for considering their coordinates.

         Use parameters `vertical_strategy` and `horizontal_strategy` **instead** for a more fine-grained treatment of the dimensions.

      :arg List<Line> add_lines: Specify a list of "lines" (i.e. pairs of `Line` objects) as **additional**, "virtual" vector graphics. These lines may help with table and / or cell detection and will not otherwise influence the detection strategy. Especially, in contrast to parameters `horizontal_lines` and `vertical_lines`, they will not prevent detecting rows or columns in other ways. These lines will be treated exactly like "real" vector graphics in terms of joining, snapping, intersectiing, minimum length and containment in the `clip` rectangle. Similarly, lines not parallel to any of the coordinate axes will be ignored.

      :arg float snap_tolerance: Any two horizontal lines whose y-values differ by no more than this value will be **snapped** into one. Accordingly for vertical lines. Default is 3. Separate values can be specified instead for the dimensions, using `snap_x_tolerance` and `snap_y_tolerance`.

      :arg float join_tolerance: Any two lines will be **joined** to one if the end and the start points differ by no more than this value (in points). Default is 3. Instead of this value, separate values can be specified for the dimensions using `join_x_tolerance` and `join_y_tolerance`.

      :arg float edge_min_length: Ignore a line if its length does not exceed this value (points). Default is 3.

      :arg int min_words_vertical: relevant for vertical strategy option "text": at least this many words must coincide to establish a **virtual column** boundary.

      :arg int min_words_horizontal: relevant for horizontal strategy option "text": at least this many words must coincide to establish a **virtual row** boundary.

      :arg float intersection_tolerance: When combining lines into cell borders, orthogonal lines must be within this value (points) to be considered intersecting. Default is 3. Instead of this value, separate values can be specified for the dimensions using `intersection_x_tolerance` and `intersection_y_tolerance`.

      :arg float text_tolerance: Characters will be combined into words only if their distance is no larger than this value (points). Default is 3. Instead of this value, separate values can be specified for the dimensions using `text_x_tolerance` and `text_y_tolerance`.

      :rtype: List
      :return: a list of `Table`

-----

   .. method:: GetWidgetProperties(Annot annot, Widget widget)

      Populate a Widget object with the values from a PDF form field.
      
      :returns: Widget object.

-----

   .. method:: InsertContents(Page page, byte[] newCont, int overlay: 1)

      Insert a buffer as a new separate /Contents object of a page.

      1. Create a new stream object from buffer 'newcont'
      2. If /Contents already is an array, then just prepend or append this object
      3. Else, create new array and put old content obj and this object into it.
         If the page had no /Contents before, just create a 1-item array.

      :returns: xref of the content.

-----

   .. method:: Integer2Letter(int i)

      Return letter sequence string for integer i.

-----

   .. method:: Integer2Roman(int i)

      Return roman numeral for integer i.

-----

   .. method:: MeasureString(string text, string fontFile, string fontName, float fontSize: 11.0f, int encoding: 0)

      Calculate the width of the text.

      :arg string text: target text.
      :arg string fontFile: must pass font file to calculate the width. MuPDF.NET doesn't support inner font resource, so must specify external font path.
      :arg string fontName: must specify with font file together.
      :arg float fontSize: font size. default is 11.0f.
      :arg int encoding: encoding type.

-----

   .. method:: MergeRange(Document docDes, Document docSrc, int spage, int epage, int apage, int rotate, bool links, bool annots, int showProgress, GraftMap graftmap)

      Copy a range of pages (spage, epage) from a source PDF to a specified location (apage) of the target PDF. If spage > epage, the sequence of source pages is reversed.
      
-----

   .. method:: NormalizeRotation(int rotate)

      Return normalized /Rotate value:one of 0, 90, 180, 270.

-----

   .. method:: RuleDict((int, string) item)

      Make a Label from a PDF page label rule.

      :arg Tuple(int, string) item: a tuple (pno, rule) with the start page number and the rule string like <</S/D...>>.
      :returns: a label struct :data:`Label`


-----

   .. method:: WriteBarcode(Page page, Rect clip, string text, BarcodeFormat barcodeFormat, string characterSet = null, bool disableEci = false)

      Creates a barcode at the supplied rectangle `clip` on the supplied `page`.

      :arg Page page: The page save the barcode image to.
      :arg Rect clip: The area to create the barcode on the page.
      :arg string text: Contents to write.
      :arg BarcodeFormat barcodeFormat: Format to encode; Supported formats: `QR_CODE`, `EAN_8`, `EAN_13`, `UPC_A`, `CODE_39`, `CODE_128`, `ITF`, `PDF_417`, `CODABAR`.
      :arg string characterSet: Use a specific character set for binary encoding (if supported by the selected barcode format).
      :arg bool disableEci: Don't generate ECI segment if non-default character set is used.

-----


   .. method:: ReplaceBytes(byte[] src, byte[] search, byte[] replace, int limit=1)

      Replaces bytes in a source buffer against the supplied search and the replacement parameters.

      :arg byte[] src: The source bytes buffer.
      :arg byte[] search: The bytes buffer to search against.
      :arg byte[] replace: The bytes buffer to replace with.
      :arg int limit: The number of matches to make, defaults to 1.

-----

.. _Utils_WriteBarcode:

   .. method:: WriteBarcode(string imageFile, string text, BarcodeFormat barcodeFormat, int width = 300, int height = 300, string characterSet = null, bool disableEci = false)

      Creates a barcode at the supplied rectangle `clip` on the page.

      :arg string imageFile: The image file to save the barcode image to.
      :arg string text: Contents to write.
      :arg BarcodeFormat barcodeFormat: Format to encode; Supported formats: `QR_CODE`, `EAN_8`, `EAN_13`, `UPC_A`, `CODE_39`, `CODE_128`, `ITF`, `PDF_417`, `CODABAR`.
      :arg int width: Width of image file.
      :arg int height: Width of image file.
      :arg string characterSet: Use a specific character set for binary encoding (if supported by the selected barcode format).
      :arg bool disableEci: Don't generate ECI segment if non-default character set is used.

      
-----
.. include:: footer.rst
