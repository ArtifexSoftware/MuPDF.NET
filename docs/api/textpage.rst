.. include:: ../header.rst

.. _TextPage:

================
TextPage
================

This class represents text and images shown on a document page. All :ref:`MuPDF document types<Supported_File_Types>` are supported.

The usual ways to create a textpage are :meth:`DisplayList.GetTextPage` and :meth:`Page.GetTextPage`. Because there is a limited set of methods in this class, there exist wrappers in :ref:`Page` which are handier to use. The last column of this table shows these corresponding :ref:`Page` methods.

For a description of what this class is all about, see Appendix 2.

==========================    ===================================== ==============================
**Method**                    **Description**                       page get_text or search method
==========================    ===================================== ==============================
:meth:`~.ExtractText`         extract plain text                    "text"
:meth:`~.ExtractText`         synonym of previous                   "text"
:meth:`~.ExtractBlocks`       plain text grouped in blocks          "blocks"
:meth:`~.ExtractWords`        all words with their bbox             "words"
:meth:`~.ExtractHtml`         page content in HTML format           "html"
:meth:`~.ExtractXHtml`        page content in XHTML format          "xhtml"
:meth:`~.ExtractXML`          page text in XML format               "xml"
:meth:`~.ExtractDict`         page content in PageInfo format       "dict"
:meth:`~.ExtractJSON`         page content in JSON format           "json"
:meth:`~.ExtractRAWDict`      page content in PageInfo format       "rawdict"
:meth:`~.ExtractRawJSON`      page content in JSON format           "rawjson"
:meth:`~.Search`              Search for a string in the page       :meth:`Page.SearchFor`
:meth:`~.ExtractSelection`    Extract selection in format of string  
==========================    ===================================== ==============================

**Class API**

.. class:: TextPage

   .. method:: ExtractText(bool sort: false)

   .. method:: ExtractText(bool sort: false)

      Return a string of the page's complete text. The text is UTF-8 unicode and in the same sequence as specified at the time of document creation.

      :arg bool sort: Sort the output by vertical, then horizontal coordinates. In many cases, this should suffice to generate a "natural" reading order.

      :rtype: string


   .. method:: ExtractBlocks

      Textpage content as a list of text lines grouped by block. Each list items looks like this::

         (x0, y0, x1, y1, "lines in the block", block_no, block_type)

      The first four entries are the block's bbox coordinates, *block_type* is 1 for an image block, 0 for text. *block_no* is the block sequence number. Multiple text lines are joined via line breaks.

      For an image block, its bbox and a text line with some image meta information is included -- **not the image content**.

      This is a high-speed method with just enough information to output plain text in desired reading sequence.

      :rtype: list of TextBlock

   .. method:: ExtractWords(char[] delimiters: null)

      Textpage content as a list of single words with bbox information. An item of this list looks like this::

         (x0, y0, x1, y1, "word", block_no, line_no, word_no)

      :arg str delimiters: Use these characters as *additional* word separators. By default, all white spaces (including the non-breaking space `0xA0`) indicate start and end of a word. Now you can specify more characters causing this. For instance, the default will return `"john.doe@outlook.com"` as **one** word. If you specify `delimiters="@."` then the **four** words `"john"`, `"doe"`, `"outlook"`, `"com"` will be returned. Other possible uses include ignoring punctuation characters `delimiters=string.punctuation`. The "word" strings will not contain any delimiting character.

      This is a high-speed method which e.g. allows extracting text from within given areas or recovering the text reading sequence.

      :rtype: list of WordBlock

   .. method:: ExtractHtml

      Textpage content as a string in HTML format. This version contains complete formatting and positioning information. Images are included (encoded as base64 strings). You need an HTML package to interpret the output. Your internet browser should be able to adequately display this information, but see :ref:`HTMLQuality`.

      :rtype: str

   .. method:: ExtractDict(bool sort: false)

      Textpage content as a dictionary. Provides same information detail as HTML. See below for the structure.

      :arg bool sort: Sort the output by vertical, then horizontal coordinates. In many cases, this should suffice to generate a "natural" reading order.

      :rtype: dict

   .. method:: ExtractJSON(bool sort: false)

      Textpage content as a JSON string. Created by `JsonConvert.SerializeObject(TextPage.ExtractDict())`. It is included for backlevel compatibility. You will probably use this method ever only for outputting the result to some file. The  method detects binary image data and converts them to base64 encoded strings.

      :arg bool sort: Sort the output by vertical, then horizontal coordinates. In many cases, this should suffice to generate a "natural" reading order.

      :rtype: string

   .. method:: ExtractXHtml

      Textpage content as a string in XHTML format. Text information detail is comparable with :meth:`ExtractTEXT`, but also contains images (base64 encoded). This method makes no attempt to re-create the original visual appearance.

      :rtype: string

   .. method:: ExtractXML

      Textpage content as a string in XML format. This contains complete formatting information about every single character on the page: font, size, line, paragraph, location, color, etc. Contains no images. You need an XML package to interpret the output.

      :rtype: string

   .. method:: ExtractRAWDict(bool sort: false)

      Textpage content as a dictionary -- technically similar to :meth:`ExtractDict`, and it contains that information as a subset (including any images). It provides additional detail down to each character, which makes using XML obsolete in many cases. See below for the structure.

      :arg bool sort: Sort the output by vertical, then horizontal coordinates. In many cases, this should suffice to generate a "natural" reading order.

      :rtype: PageInfo

   .. method:: ExtractRawJSON(bool sort: false)

      Textpage content as a JSON string. Created by `JsonConvert.SerializeObject(TextPage.ExtractRAWDict())`. You will probably use this method ever only for outputting the result to some file. The  method detects binary image data and converts them to base64 encoded strings.

      :arg bool sort: Sort the output by vertical, then horizontal coordinates. In many cases, this should suffice to generate a "natural" reading order.

      :rtype: string

   .. method:: Search(string needle, bool quads: false)

      Search for *string* and return a list of found locations.

      :arg string needle: the string to search for. Upper and lower cases will all match if needle consists of ASCII letters only -- it does not yet work for "Ä" versus "ä", etc.
      :arg bool quads: return quadrilaterals instead of rectangles.
      
      :rtype: list of Quad
      :returns: a list of :ref:`Rect` or :ref:`Quad` objects, each surrounding a found *needle* occurrence. As the search string may contain spaces, its parts may be found on different lines. In this case, more than one rectangle (resp. quadrilateral) are returned. The method **now supports dehyphenation**, so it will find e.g. "method", even if it was hyphenated in two parts "meth-" and "od" across two lines. The two returned rectangles will contain "meth" (no hyphen) and "od".

      .. note:: **Overview of changes in v1.18.2:**

        1. The `hitMax` parameter has been removed: all hits are always returned.
        2. The `rect` parameter of the :ref:`TextPage` is now respected: only text inside this area is examined. Only characters with fully contained bboxes are considered. The wrapper method :meth:`Page.search_for` correspondingly supports a *clip* parameter.
        3. **Hyphenated words** are now found.
        4. **Overlapping rectangles** in the same line are now automatically joined. We assume that such separations are an artifact created by multiple marked content groups, containing parts of the same search needle.

      Example Quad versus Rect: when searching for needle "pymupdf", then the corresponding entry will either be the blue rectangle, or, if *quads* was specified, the quad *Quad(ul, ur, ll, lr)*.

      .. image:: ../images/img-quads.*

   .. attribute:: Rect

      The rectangle associated with the text page. This either equals the rectangle of the creating page or the `clip` parameter of :meth:`Page.GetTextPage` and text extraction / searching methods.

      .. note:: The output of text searching and most text extractions **is restricted to this rectangle**. (X)HTML and XML output will however always extract the full page.

.. _textpagedict:

Structure of Dictionary Outputs
--------------------------------
Methods :meth:`TextPage.ExtractDict`, :meth:`TextPage.ExtractJSON`, :meth:`TextPage.ExtractRAWDict`, and :meth:`TextPage.ExtractRawJSON` return dictionaries, containing the page's text and image content. The dictionary structures of all four methods are almost equal. They strive to map the text page's information hierarchy of blocks, lines, spans and characters as precisely as possible, by representing each of these by its own sub-dictionary:

* A **page** consists of a list of **block dictionaries**.
* A (text) **block** consists of a list of **line dictionaries**.
* A **line** consists of a list of **span dictionaries**.
* A **span** either consists of the text itself or, for the RAW variants, a list of **character dictionaries**.
* RAW variants: a **character** is a dictionary of its origin, bbox and unicode.

Please note, that only **bboxes** (= :data:`Rect` 4-tuples) are returned, whereas a :ref:`TextPage` actually has the **full position information** -- in :ref:`Quad` format. The reason for this decision is a memory consideration: a :data:`Quad` needs 488 bytes (3 times the size of a :data:`Rect`). Given the mentioned amounts of generated bboxes, returning :data:`Quad` information would have a significant impact.

In the vast majority of cases, we are dealing with **horizontal text only**, where bboxes provide entirely sufficient information.

As mentioned, using these functions is ever only needed, if the text is **not written horizontally** -- `line["dir"] != (1, 0)` -- and you need the quad for text marker annotations (:meth:`Page.AddHighlightAnnot` and friends).


.. image:: ../images/img-textpage.*



Page Dictionary
~~~~~~~~~~~~~~~~~

=============== ============================================
**Key**         **Value**
=============== ============================================
Width           width of the `clip` rectangle *(float)*
Height          height of the `clip` rectangle *(float)*
Blocks          *list* of block dictionaries
=============== ============================================

Block Dictionaries
~~~~~~~~~~~~~~~~~~
Block dictionaries come in two different formats for **image blocks** and for **text blocks**.

**Image block:**

=============== ===============================================================
**Key**             **Value**
=============== ===============================================================
Type            1 = image *(int)*
Bbox            image bbox on page (:data:`Rect`)
Number          block count *(int)*
Ext             image type *(str)*, as file extension, see below
Width           original image width *(int)*
Height          original image height *(int)*
ColorSpace      colorspace component count *(int)*
Xres            resolution in x-direction *(int)*
Yres            resolution in y-direction *(int)*
Bpc             bits per component *(int)*
Transform       matrix transforming image rect to bbox (:data:`Matrix`)
Size            size of the image in bytes *(int)*
Image           image content *(bytes)*
=============== ===============================================================

Possible values of the "ext" key are "bmp", "gif", "jpeg", "jpx" (JPEG 2000), "jxr" (JPEG XR), "png", "pnm", and "tiff".

.. note::

   1. An image block is generated for **all and every image occurrence** on the page. Hence there may be duplicates, if an image is shown at different locations.

   2. :ref:`TextPage` and corresponding method :meth:`Page.GetText` are **available for all document types**. Only for PDF documents, methods :meth:`Document.GetPageImages` / :meth:`Page.GetImages` offer some overlapping functionality as far as image lists are concerned. But both lists **may or may not** contain the same items. Any differences are most probably caused by one of the following:

   3. The image's "transformation matrix" is defined as the matrix, for which the expression `bbox / transform == Rect(0, 0, 1, 1)` is true, lookup details here: :ref:`ImageTransformation`.


**Text block:**

=============== ====================================================
**Key**             **Value**
=============== ====================================================
Type            0 = text *(int)*
Bbox            block rectangle, :data:`Rect`
Number          block count *(int)*
Lines           *list* of text line dictionaries
=============== ====================================================

Line Dictionary
~~~~~~~~~~~~~~~~~

=============== =====================================================
**Key**             **Value**
=============== =====================================================
Bbox            line rectangle, :data:`Rect`
WMode           writing mode *(int)*: 0 = horizontal, 1 = vertical
Dir             writing direction, :data:`Point`
Spans           *list* of span dictionaries
=============== =====================================================

The value of key *"dir"* is the **unit vector** `dir = (cosine, -sine)` of the angle, which the text has relative to the x-axis [#f2]_. See the following picture: The word in each quadrant (counter-clockwise from top-right to bottom-right) is rotated by 30, 120, 210 and 300 degrees respectively.

.. image:: ../images/img-line-dir.*
   :scale: 100

Span Dictionary
~~~~~~~~~~~~~~~~~

Spans contain the actual text. A line contains **more than one span only**, if it contains text with different font properties.

=============== =====================================================================
**Key**             **Value**
=============== =====================================================================
Bbox            span rectangle, :data:`Rect`
Origin          the first character's origin, :data:`point_like`
font            font name *(string)*
Asc             ascender of the font *(float)*
Desc            descender of the font *(float)*
Size            font size *(float)*
Flags           font characteristics *(int)*
Color           text color in sRGB format *(int)*
Text            (only for :meth:`ExtractDict`) text *(string)*
Chars           (only for :meth:`ExtractRAWDict`) *list* of character dictionaries
=============== =====================================================================


.. image:: ../images/img-asc-desc.*
   :scale: 60

These numbers may be used to compute the minimum height of a character (or span) -- as opposed to the standard height provided in the "bbox" values (which actually represents the **line height**). The following code recalculates the span bbox to have a height of **fontSize** exactly fitting the text inside:

float a = span.Asc
float d = span.Desc
Rect r = new Rect(span.Bbox)
Point o = new Point(span.Origin)  # its y-value is the baseline
r.y1 = o.y - span.Size * d / (a - d)
r.y0 = r.y1 - span.Size

.. caution:: The above calculation may deliver a **larger** height! This may e.g. happen for OCRed documents, where the risk of text artifacts is high. MuPDF tries to come up with a reasonable bbox height, independently from the :data:`fontSize` found in the PDF. So please ensure that the height of `span["bbox"]` is **larger** than `span["size"]`.

The following shows the original span rectangle in red and the rectangle with re-computed height in blue.

.. image:: ../images/img-span-rect.*
   :scale: 200

*"flags"* is an integer, which represents font properties except for the first bit 0. They are to be interpreted like this:

* bit 0: superscripted (2\ :sup:`0`) -- not a font property, detected by MuPDF code.
* bit 1: italic (2\ :sup:`1`)
* bit 2: serifed (2\ :sup:`2`)
* bit 3: monospaced (2\ :sup:`3`)
* bit 4: bold (2\ :sup:`4`)

Bits 1 thru 4 are font properties, i.e. encoded in the font program. Please note, that this information is not necessarily correct or complete: fonts quite often contain wrong data here.

Character Dictionary for :meth:`ExtractRAWDict`
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

=============== ===========================================================
**Key**             **Value**
=============== ===========================================================
Origin          character's left baseline point, :data:`Point`
Bbox            character rectangle, :data:`Rect`
C               the character (unicode)
=============== ===========================================================

This image shows the relationship between a character's bbox and its quad: |textpagechar|

.. |textpagechar| image:: images/img-textpage-char.*
   :align: top
   :scale: 66


.. rubric:: Footnotes

.. [#f1] Image specifications for a PDF page are done in a page's (sub-) :data:`dictionary`, called `/Resources`. Resource dictionaries can be **inherited** from any of the page's parent objects (usually the :data:`catalog` -- the top-level parent). The PDF creator may e.g. define one `/Resources` on file level, naming all images and / or all fonts ever used by any page. In these cases, :meth:`Page.get_images` and :meth:`Page.get_fonts` will consequently return the same lists for all pages. If desired, this situation can be reverted using :meth:`Page.clean_contents`. After execution, the page's object definition will show fonts and images that are actually used.

.. [#f2] The coordinate systems of MuPDF and PDF are different in that MuPDF uses the page's top-left point as `(0, 0)`. In PDF, this is the bottom-left point. Therefore, the positive direction for MuPDF's y-axis is **from top to bottom**. This causes the sign change for the sine value here: a **negative** value indicates anti-clockwise rotation of the text.

.. include:: ../footer.rst
