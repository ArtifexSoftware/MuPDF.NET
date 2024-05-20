.. include:: header.rst

.. _Font:

================
Font
================

* New in v1.16.18

This class represents a font as defined in MuPDF (*fz_font_s* structure). It is required for the new class :ref:`TextWriter` and the new :meth:`Page.write_text`. Currently, it has no connection to how fonts are used in methods :meth:`Page.insert_text` or :meth:`Page.insert_textbox`, respectively.

A Font object also contains useful general information, like the font bbox, the number of defined glyphs, glyph names or the bbox of a single glyph.


==================================== ============================================
**Method / Attribute**               **Short Description**
==================================== ============================================
:meth:`~Font.GlyphAdvance`           Width of a character
:meth:`~Font.GlyphBbox`              Glyph rectangle
:meth:`~Font.GlyphName2Unicode`      Get unicode from glyph name
:meth:`~Font.HasGlyph`               Return glyph id of unicode
:meth:`~Font.TextLength`             Compute string length
:meth:`~Font.GetCharLengths`         Tuple of char widths of a string
:meth:`~Font.Unicode2GlyphName`      Get glyph name of a unicode
:meth:`~Font.GetValidCodePoints`     Array of supported unicodes
:attr:`~Font.Ascender`               Font ascender
:attr:`~Font.Descender`              Font descender
:attr:`~Font.Bbox`                   Font rectangle
:attr:`~Font.Buffer`                 Copy of the font's binary image
:attr:`~Font.Flags`                  Collection of font properties
:attr:`~Font.GlyphCount`             Number of supported glyphs
:attr:`~Font.Name`                   Name of font
:attr:`~Font.IsWriteable`            Font usable with :ref:`TextWriter`
==================================== ============================================


**Class API**

.. class:: Font

   .. index::
      pair: Font, fontFile
      pair: Font, fontBuffer
      pair: Font, script
      pair: Font, ordering
      pair: Font, isBold
      pair: Font, isItalic
      pair: Font, isSerif
      pair: Font, fontName
      pair: Font, language

   .. method:: MuPDFFont(string fontName = null, string fontFile = null, byte[] fontBuffer = null, int script = 0, string language = null, int ordering = -1, int isBold = 0, int isItalic = 0, int isSerif = 0, int embed = 1)

      Font constructor. The large number of parameters are used to locate font, which most closely resembles the requirements. Not all parameters are ever required -- see the below pseudo code explaining the logic how the parameters are evaluated.

      :arg str fontname: one of the :ref:`Base-14-Fonts` or CJK fontnames. Also possible are a select few other names like (watch the correct spelling): "Arial", "Times", "Times Roman".
      
         *(Changed in v1.17.5)*

         If you have installed `pymupdf-fonts <https://pypi.org/project/pymupdf-fonts/>`_, there are also new "reserved" fontnames available, which are listed in :attr:`fitz_fonts` and in the table further down.

      :arg str fontfile: the filename of a fontfile somewhere on your system [#f1]_.
      :arg bytes,bytearray,io.BytesIO fontbuffer: a fontfile loaded in memory [#f1]_.
      :arg in script: the number of a UCDN script. Currently supported in PyMuPDF are numbers 24, and 32 through 35.
      :arg str language: one of the values "zh-Hant" (traditional Chinese), "zh-Hans" (simplified Chinese), "ja" (Japanese) and "ko" (Korean). Otherwise, all ISO 639 codes from the subsets 1, 2, 3 and 5 are also possible, but are currently documentary only.
      :arg int ordering: an alternative selector for one of the CJK fonts.
      :arg bool isBold: look for a bold font.
      :arg bool isItalic: look for an italic font.
      :arg bool isSerif: look for a serifed font.

      :returns: a MuPDF font if successful. This is the overall sequence of checks to determine an appropriate font:

         =========== ============================================================
         Argument    Action
         =========== ============================================================
         fontFile?   Create font from file, exception if failure.
         fontBuffer? Create font from buffer, exception if failure.
         ordering>=0 Create universal font, always succeeds.
         fontName?   Create a Base-14 font, universal font, or font
                     provided by `pymupdf-fonts <https://pypi.org/project/pymupdf-fonts/>`_. See table below.
         =========== ============================================================


      .. note::

        With the usual reserved names "helv", "tiro", etc., you will create fonts with the expected names "Helvetica", "Times-Roman" and so on. **However**, and in contrast to :meth:`Page.insert_font` and friends,

         * a font file will **always** be embedded in your PDF,
         * Greek and Cyrillic characters are supported without needing the *encoding* parameter.

        Using *ordering >= 0*, or fontnames "cjk", "china-t", "china-s", "japan" or "korea" will **always create the same "universal"** font **"Droid Sans Fallback Regular"**. This font supports **all Chinese, Japanese, Korean and Latin characters**, including Greek and Cyrillic. This is a sans-serif font.

        Actually, you would rarely ever need another sans-serif font than **"Droid Sans Fallback Regular"**. **Except** that this font file is relatively large and adds about 1.65 MB (compressed) to your PDF file size. If you do not need CJK support, stick with specifying "helv", "tiro" etc., and you will get away with about 35 KB compressed.

        If you **know** you have a mixture of CJK and Latin text, consider just using `Font("cjk")` because this supports everything and also significantly (by a factor of up to three) speeds up execution: MuPDF will always find any character in this single font and never needs to check fallbacks.

        But if you do use some other font, you will still automatically be able to also write CJK characters: MuPDF detects this situation and silently falls back to the universal font (which will then of course also be embedded in your PDF).

        *(New in v1.17.5)* Optionally, some new "reserved" fontname codes become available if you install `pymupdf-fonts <https://pypi.org/project/pymupdf-fonts/>`_, `pip install pymupdf-fonts`. **"Fira Mono"** is a mono-spaced sans font set and **FiraGO** is another non-serifed "universal" font set which supports all Latin (including Cyrillic and Greek) plus Thai, Arabian, Hewbrew and Devanagari -- but none of the CJK languages. The size of a FiraGO font is only a quarter of the "Droid Sans Fallback" size (compressed 400 KB vs. 1.65 MB) -- **and** it provides the weights bold, italic, bold-italic -- which the universal font doesn't.

        **"Space Mono"** is another nice and small mono-spaced font from Google Fonts, which supports Latin Extended characters and comes with all 4 important weights.

        The following table maps a fontname code to the corresponding font. For the current content of the package please see its documentation:

            =========== =========================== ======= =============================
            Code        Fontname                    New in  Comment
            =========== =========================== ======= =============================
            figo        FiraGO Regular              v1.0.0  narrower than Helvetica
            figbo       FiraGO Bold                 v1.0.0
            figit       FiraGO Italic               v1.0.0
            figbi       FiraGO Bold Italic          v1.0.0
            fimo        Fira Mono Regular           v1.0.0
            fimbo       Fira Mono Bold              v1.0.0
            spacemo     Space Mono Regular          v1.0.1
            spacembo    Space Mono Bold             v1.0.1
            spacemit    Space Mono Italic           v1.0.1
            spacembi    Space Mono Bold-Italic      v1.0.1
            math        Noto Sans Math Regular      v1.0.2  math symbols
            music       Noto Music Regular          v1.0.2  musical symbols
            symbol1     Noto Sans Symbols Regular   v1.0.2  replacement for "symb"
            symbol2     Noto Sans Symbols2 Regular  v1.0.2  extended symbol set
            notos       Noto Sans Regular           v1.0.3  alternative to Helvetica
            notosit     Noto Sans Italic            v1.0.3
            notosbo     Noto Sans Bold              v1.0.3
            notosbi     Noto Sans BoldItalic        v1.0.3
            =========== =========================== ======= =============================

   .. index::
      pair: Font.HasGlyph, language
      pair: Font.HasGlyph, script
      pair: Font.HasGlyph, fallback
      pair: Font.HasGlyph, smallCaps


   .. method:: HasGlyph(int chr, string language=null, int script=0, int fallback=0, int smallCaps = 0)

      Check whether the unicode *chr* exists in the font or (option) some fallback font. May be used to check whether any "TOFU" symbols will appear on output.

      :arg int chr: the unicode of the character (i.e. *ord()*).
      :arg str language: the language -- currently unused.
      :arg int script: the UCDN script number.
      :arg bool fallback: *(new in v1.17.5)* perform an extended search in fallback fonts or restrict to current font (default).
      :returns: *(changed in 1.17.7)* the glyph number. Zero indicates no glyph found.

   .. method:: GetValidCodePoints()

      * New in v1.17.5

      Return an array of unicodes supported by this font.

      :returns: an *array.array* [#f2]_ of length at most :attr:`Font.glyph_count`. I.e. *chr()* of every item in this array has a glyph in the font without using fallbacks. This is an example display of the supported glyphs:

         MuPDFFont font =  new MuPDFFont("math")
         List<int> vuc = font.GetValidCodePoints()
         foreach(int i in vuc)
            print($"%04X %s (%s)" % (i, chr(i), font.Unicode2GlyphName(i)))
         0000
         000D   (CR)
         0020   (space)
         0021 ! (exclam)
         0022 " (quotedbl)
         0023 # (numbersign)
         0024 $ (dollar)
         0025 % (percent)
         ...
         00AC ¬ (logicalnot)
         00B1 ± (plusminus)
         ...
         21D0 ⇐ (arrowdblleft)
         21D1 ⇑ (arrowdblup)
         21D2 ⇒ (arrowdblright)
         21D3 ⇓ (arrowdbldown)
         21D4 ⇔ (arrowdblboth)
         ...
         221E ∞ (infinity)
         ...

      .. note:: This method only returns meaningful data for fonts having a CMAP (character map, charmap, the `/ToUnicode` PDF key). Otherwise, this array will have length 1 and contain zero only.

   .. index::
      pair: Font.GlyphAdvance, language
      pair: Font.GlyphAdvance, script
      pair: Font.GlyphAdvance, wmode
      pair: Font.GlyphAdvance, smallCaps

   .. method:: GlyphAdvance(int chr, string language=null, int script=0, int wmode=0, int smallCaps = 0 )

      Calculate the "width" of the character's glyph (visual representation).

      :arg int chr: the unicode number of the character. Use *ord()*, not the character itself. Again, this should normally work even if a character is not supported by that font, because fallback fonts will be checked where necessary.
      :arg int wmode: write mode, 0 = horizontal, 1 = vertical.

      The other parameters are not in use currently.

      :returns: a float representing the glyph's width relative to **fontsize 1**.

   .. method:: GlyphName2Unicode(string name)

      Return the unicode value for a given glyph name. Use it in conjunction with `chr()` if you want to output e.g. a certain symbol.

      :arg str name: The name of the glyph.

      :returns: The unicode integer, or 65533 = 0xFFFD if the name is unknown. Examples: `font.glyph_name_to_unicode("Sigma") = 931`, `font.glyph_name_to_unicode("sigma") = 963`. Refer to the `Adobe Glyph List <https://github.com/adobe-type-tools/agl-aglfn/blob/master/glyphlist.txt>`_ publication for a list of glyph names and their unicode numbers. Example:

         >>> font = fitz.Font("helv")
         >>> font.HasGlyph(font.glyph_name_to_unicode("infinity"))
         True

   .. index::
      pair: Font.GlyphBbox, language
      pair: Font.GlyphBbox, script
      pair: Font.GlyphBbox, smallCaps

   .. method:: glyph_bbox(chr, string language=null, int script=0, int smallCaps = 0 )

      The glyph rectangle relative to :data:`fontsize` 1.

      :arg int chr: *ord()* of the character.

      :returns: a :ref:`Rect`.


   .. method:: Unicode2GlyphName(ch)

      Show the name of the character's glyph.

      :arg int ch: the unicode number of the character. Use *ord()*, not the character itself.

      :returns: a string representing the glyph's name. E.g. `font.glyph_name(ord("#")) = "numbersign"`. For an invalid code ".notfound" is returned.
      
        .. note:: *(Changed in v1.18.0)* This method and :meth:`Font.glyph_name_to_unicode` no longer depend on a font and instead retrieve information from the **Adobe Glyph List**. Also available as `fitz.Unicode2GlyphName()` and resp. `fitz.glyph_name_to_unicode()`.

   .. index::
      pair: TextLength, fontSize
      pair: TextLength, language
      pair: TextLength, script
      pair: TextLength, wmode
      pair: TextLength, smallCaps

   .. method:: TextLength(string text, float fontsize=11, string language=null, int wmode = 0, int script=0, int smallCaps=0)

      Calculate the length in points of a unicode string.

      .. note:: There is a functional overlap with :meth:`Utils.GetTextLength` for Base-14 fonts only.

      :arg str text: a text string, UTF-8 encoded.

      :arg float fontsize: the :data:`fontSize`.

      :rtype: float

      :returns: the length of the string in points when stored in the PDF. If a character is not contained in the font, it will automatically be looked up in a fallback font.

         .. note:: This method was originally implemented in Python, based on calling :meth:`Font.GlyphAdvance`. For performance reasons, it has been rewritten in C for v1.18.14. To compute the width of a single character, you can now use either of the following without performance penalty:

            1. `font.GlyphAdvance(ord("Ä")) * fontsize`
            2. `font.TextLength("Ä", fontSize=fontsize)`

            For multi-character strings, the method offers a huge performance advantage compared to the previous implementation: instead of about 0.5 microseconds for each character, only 12.5 nanoseconds are required for the second and subsequent ones.

   .. index::
      pair: GetCharLengths, fontSize
      pair: GetCharLengths, language
      pair: GetCharLengths, script
      pair: GetCharLengths, wmode
      pair: GetCharLengths, smallCaps

   .. method:: GetCharLengths(string text, float fontSize=11, string language = null, int script = 0, int wmode = 0, int smallCaps = 0)

      *New in v1.18.14*

      Sequence of character lengths in points of a unicode string.

      :arg str text: a text string, UTF-8 encoded.

      :arg float fontSize: the :data:`FontSize`.

      :rtype: tuple

      :returns: the lengths in points of the characters of a string when stored in the PDF. It works like :meth:`Font.Textlength` broken down to single characters. This is a high speed method, used e.g. in :meth:`TextWriter.FillTextbox`. The following is true (allowing rounding errors): `font.TextLength(text) == sum(font.GetCharLengths(text))`.

         MuPDFFont font = new MuPDFFont("helv");
         string text = "PyMuPDF";
         font.text_length(text);
         Utils.GetTextLength(text, fontName="helv");
         sum(font.GetCharLengths(text));
         Console.WriteLine(font.GetCharLengths(text));

   .. attribute:: Buffer

      * New in v1.17.6

      Copy of the binary font file content.
      
      :rtype: bytes

   .. attribute:: Flags

      A dictionary with various font properties, each represented as bools. Example for Helvetica::

      :rtype: dict

   .. attribute:: Name

      :rtype: str

      Name of the font. May be "" or "(null)".

   .. attribute:: Bbox

      The font bbox. This is the maximum of its glyph bboxes.

      :rtype: :ref:`Rect`

   .. attribute:: GlyphCount

      :rtype: int

      The number of glyphs defined in the font.

   .. attribute:: Ascender

      * New in v1.18.0

      The ascender value of the font, see `here <https://en.wikipedia.org/wiki/Ascender_(typography)>`_ for details. Please note that there is a difference to the strict definition: our value includes everything above the baseline -- not just the height difference between upper case "A" and and lower case "a".

      :rtype: float

   .. attribute:: Descender

      * New in v1.18.0

      The descender value of the font, see `here <https://en.wikipedia.org/wiki/Descender>`_ for details. This value always is negative and is the portion that some glyphs descend below the base line, for example "g" or "y". As a consequence, the value `Ascender - Descender` is the total height, that every glyph of the font fits into. This is true at least for most fonts -- as always, there are exceptions, especially for calligraphic fonts, etc.

      :rtype: float

   .. attribute:: IsWritable

      * New in v1.18.0

      Indicates whether this font can be used with :ref:`TextWriter`.

      :rtype: bool

.. rubric:: Footnotes

.. [#f1] MuPDF does not support all fontfiles with this feature and will raise exceptions like *"mupdf: FT_New_Memory_Face((null)): unknown file format"*, if it encounters issues. The :ref:`TextWriter` methods check :attr:`Font.IsWritable`.

.. [#f2] The built-in module *array* has been chosen for its speed and its compact representation of values.

.. include:: footer.rst
