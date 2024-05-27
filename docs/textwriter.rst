.. include:: header.rst

.. _TextWriter:

================
TextWriter
================

|pdf_only_class|

This class represents a MuPDF *text* object. The basic idea is to **decouple (1) text preparation, and (2) text output** to PDF pages.

During **preparation**, a text writer stores any number of text pieces ("spans") together with their positions and individual font information. The **output** of the writer's prepared content may happen multiple times to any PDF page with a compatible page size.

A text writer is an elegant alternative to methods :meth:`Page.InsertText` and friends:

* **Improved text positioning:** Choose any point where insertion of text should start. Storing text returns the "cursor position" after the *last character* of the span.
* **Free font choice:** Each text span has its own font and :data:`fontSize`. This lets you easily switch when composing a larger text.
* **Automatic fallback fonts:** If a character is not supported by the chosen font, alternative fonts are automatically searched. This significantly reduces the risk of seeing unprintable symbols in the output ("TOFUs" -- looking like a small rectangle). PyMuPDF now also comes with the **universal font "Droid Sans Fallback Regular"**, which supports **all Latin** characters (including Cyrillic and Greek), and **all CJK** characters (Chinese, Japanese, Korean).
* **Cyrillic and Greek Support:** The :ref:`Base-14-fonts` have integrated support of Cyrillic and Greek characters **without specifying encoding.** Your text may be a mixture of Latin, Greek and Cyrillic.
* **Transparency support:** Parameter *opacity* is supported. This offers a handy way to create watermark-style text.
* **Justified text:** Supported for any font -- not just simple fonts as in :meth:`Page.InsertTextbox`.
* **Reusability:** A TextWriter object exists independent from PDF pages. It can be written multiple times, either to the same or to other pages, in the same or in different PDFs, choosing different colors or transparency.

Using this object entails three steps:

1. When **created**, a TextWriter requires a fixed **page rectangle** in relation to which it calculates text positions. A text writer can write to pages of this size only.
2. Store text in the TextWriter using methods :meth:`TextWriter.Append`, :meth:`TextWriter.Appendv` and :meth:`TextWriter.FillTextbox` as often as is desired.
3. Output the TextWriter object on some PDF page(s).

.. note::

   * Starting with version 1.17.0, TextWriters **do support** text rotation via the *morph* parameter of :meth:`TextWriter.WriteText`.

   * There also exists :meth:`Page.WriteText` which combines one or more TextWriters and jointly writes them to a given rectangle and with a given rotation angle -- much like :meth:`Page.ShowPdfPage`.


================================ ============================================
**Method / Attribute**           **Short Description**
================================ ============================================
:meth:`~TextWriter.Append`       Add text in horizontal write mode
:meth:`~TextWriter.Appendv`      Add text in vertical write mode
:meth:`~TextWriter.FillTextbox`  Fill rectangle (horizontal write mode)
:meth:`~TextWriter.WriteText`    Output TextWriter to a PDF page
:attr:`~TextWriter.Color`        Text color (can be changed)
:attr:`~TextWriter.LastPoint`    Last written character ends here
:attr:`~TextWriter.Opacity`      Text opacity (can be changed)
:attr:`~TextWriter.Rect`         Page rectangle used by this TextWriter
:attr:`~TextWriter.TextRect`     Area occupied so far
================================ ============================================


**Class API**

.. class:: TextWriter

   .. method:: TextWriter(Rect rect, float opacity=1, float[] color=null)

      :arg Rect rect: rectangle internally used for text positioning computations.
      :arg float opacity: sets the transparency for the text to store here. Values outside the interval `[0, 1)` will be ignored. A value of e.g. 0.5 means 50% transparency.
      :arg float[] color: the color of the text. All colors are specified as floats *0 <= color <= 1*. A single float represents some gray level, an array of float implies the colorspace via its length.


   .. method:: Append(Point pos, string text, string font=null, int fontSize=11, string language=null, bool right2left=false, int smallCaps=0)

      Add some new text in horizontal writing.

      :arg point_like pos: start position of the text, the bottom left point of the first character.
      :arg str text: a string of arbitrary length. It will be written starting at position "pos".
      :arg font: a :ref:`Font`. If omitted, `new Font("helv")` will be used.
      :arg float fontsize: the :data:`fontSize`, a positive number, default 11.
      :arg str language: the language to use, e.g. "en" for English. Meaningful values should be compliant with the ISO 639 standards 1, 2, 3 or 5. Reserved for future use: currently has no effect as far as we know.
      :arg bool right_to_left: whether the text should be written from right to left. Applicable for languages like Arabian or Hebrew. Default is *false*. If *true*, any Latin parts within the text will automatically converted. There are no other consequences, i.e. :attr:`TextWriter.LastPoint` will still be the rightmost character, and there neither is any alignment taking place. Hence you may want to use :meth:`TextWriter.FillTextbox` instead.
      :arg bool smallCaps: look for the character's Small Capital version in the font. If present, take that value instead. Otherwise the original character (this font or the fallback font) will be taken. The fallback font will never return small caps. For example, this snippet::

         Document doc = new Document();
         Page page = doc.NewPage();
         string text = "PyMuPDF: the Python bindings for MuPDF";
         MuPDFFont font = new Font("figo");                       // choose a font with small caps
         MuPDFTextWriter tw = new MuPDFTextWriter(page.rect);
         tw.Append((50, 100), text, font=font, small_caps=true);
         tw.WriteText(page);
         doc.Save("x.pdf");

         will produce this PDF text:

         .. image:: images/img-smallcaps.*


      :returns: :attr:`TextRect` and :attr:`LastPoint`. Raises an exception for an unsupported font -- checked via :attr:`Font.IsWritable`.


   .. method:: Appendv(Point pos, string text, string font=null, float fontSize=11, string language=null, int smallCaps=0)

      Add some new text in vertical, top-to-bottom writing.

      :arg Point pos: start position of the text, the bottom left point of the first character.
      :arg str text: a string. It will be written starting at position "pos".
      :arg font: a :ref:`Font`. If omitted, `new Font("helv")` will be used.
      :arg float fontsize: the :data:`fontSize`, a positive float, default 11.
      :arg str language: the language to use, e.g. "en" for English. Meaningful values should be compliant with the ISO 639 standards 1, 2, 3 or 5. Reserved for future use: currently has no effect as far as we know.
      :arg bool smallCaps: see :meth:`Append`.

      :returns: :attr:`TextRect` and :attr:`LastPoint`. Raises an exception for an unsupported font -- checked via :attr:`Font.IsWritable`.

   .. method:: FillTextbox(Rect rect, string text, Point pos=null, MuPDFFont font=null, float fontsize=11, float lineHeight = 0, int align=0, bool right2left=false, bool warn=false, bool smallCaps=false)

      Fill a given rectangle with text in horizontal writing mode. This is a convenience method to use as an alternative for :meth:`Append`.

      :arg Rect rect: the area to fill. No part of the text will appear outside of this.
      :arg string text: the text. Can be specified as a (UTF-8) string or a list / tuple of strings. A string will first be converted to a list using *splitlines()*. Every list item will begin on a new line (forced line breaks).
      :arg Point pos: start storing at this point. Default is a point near rectangle top-left.
      :arg MuPDFFont font: the :ref:`Font`, default `new Font("helv")`.
      :arg float fontsize: the :data:`fontSize`.
      :arg int align: text alignment. Use one of TEXT_ALIGN_LEFT, TEXT_ALIGN_CENTER, TEXT_ALIGN_RIGHT or TEXT_ALIGN_JUSTIFY.
      :arg bool right2left: whether the text should be written from right to left. Applicable for languages like Arabian or Hebrew. Default is *false*. If *true*, any Latin parts are automatically reverted. You must still set the alignment (if you want right alignment), it does not happen automatically -- the other alignment options remain available as well.
      :arg bool warn: on text overflow do nothing, warn, or raise an exception. Overflow text will never be written.

        * Default is *null*.
        * The list of overflow lines will be returned.

      :arg bool smallCaps: see :meth:`Append`.

      :rtype: list
      :returns: List of lines that did not fit in the rectangle. Each item is a tuple `(text, length)` containing a string and its length (on the page).

   .. note:: Use these methods as often as is required -- there is no technical limit (except memory constraints of your system). You can also mix :meth:`Append` and text boxes and have multiple of both. Text positioning is exclusively controlled by the insertion point. Therefore there is no need to adhere to any order. Raise an exception for an unsupported font -- checked via :attr:`Font.IsWritable`.

   .. method:: WriteText(Page page, float opacity = -1, float[] color = null, Morph morph = null, int overlay = 1, int oc = 0, int renderMode = 0)

      Write the TextWriter text to a page, which is the only mandatory parameter. The other parameters can be used to temporarily override the values used when the TextWriter was created.

      :arg page: write to this :ref:`Page`.
      :arg float opacity: override the value of the TextWriter for this output.
      :arg float[] color: override the value of the TextWriter for this output.
      :arg Morph morph: modify the text appearance by applying a matrix to it. If provided, this must be a sequence *(fixpoint, matrix)* with a point-like *fixpoint* and a matrix-like *matrix*. A typical example is rotating the text around *fixpoint*.
      :arg int overlay: put in foreground (default) or background.
      :arg int oc: the :data:`Xref` of an :data:`OCG` or :data:`OCMD`.
      :arg int renderMode: The PDF `Tr` operator value. Values: 0 (default), 1, 2, 3 (invisible).

         .. image:: images/img-rendermode.*


   .. attribute:: TextRect

      The area currently occupied.

      :rtype: :ref:`Rect`

   .. attribute:: LastPoint

      The "cursor position" -- a :ref:`Point` -- after the last written character (its bottom-right).

      :rtype: :ref:`Point`

   .. attribute:: Opacity

      The text opacity (modifiable).

      :rtype: float

   .. attribute:: Color

      The text color (modifiable).

      :rtype: float[]

   .. attribute:: Rect

      The page rectangle for which this TextWriter was created. Must not be modified.

      :rtype: :ref:`Rect`


.. note:: To see some demo scripts dealing with TextWriter, have a look at `this <https://github.com/pymupdf/PyMuPDF-Utilities/tree/master/textwriter>`_ repository.

  1. Opacity and color apply to **all the text** in this object.
  2. If you need different colors / transparency, you must create a separate TextWriter. Whenever you determine the color should change, simply append the text to the respective TextWriter using the previously returned :attr:`LastPoint` as position for the new text span.
  3. Appending items or text boxes can occur in arbitrary order: only the position parameter controls where text appears.
  4. Font and :data:`fontSize` can freely vary within the same TextWriter. This can be used to let text with different properties appear on the same displayed line: just specify *pos* accordingly, and e.g. set it to :attr:`LastPoint` of the previously added item.
  5. You can use the *pos* argument of :meth:`TextWriter.FillTextbox` to set the position of the first text character. This allows filling the same textbox with contents from different :ref:`TextWriter` objects, thus allowing for multiple colors, opacities, etc.
  6. MuPDF does not support all fonts with this feature, e.g. no Type3 fonts. Starting with v1.18.0 this can be checked via the font attribute :attr:`Font.IsWritable`. This attribute is also checked when using :ref:`TextWriter` methods.

.. include:: footer.rst
