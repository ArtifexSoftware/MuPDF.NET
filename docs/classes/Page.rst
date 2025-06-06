.. include:: ../header.rst

.. _Page:

================
Page
================


Class representing a document page. A page object is created by :meth:`Document.LoadPage` or, equivalently, via indexing the document like `doc[n]` - it has no independent constructor.

There is a parent-child relationship between a document and its pages. If the document is closed or deleted, all page objects (and their respective children, too) in existence will become unusable ("orphaned"): If a page property or method is being used, an exception is raised.

Several page methods have a :ref:`Document` counterpart for convenience. At the end of this chapter you will find a synopsis.

.. note:: Many times in this chapter we are using the term **coordinate**. It is of high importance to have at least a basic understanding of what that is and that you feel comfortable with the section :ref:`Coordinates`.

Modifying Pages
---------------
Changing page properties and adding or changing page content is available for PDF documents only.

In a nutshell, this is what you can do with MuPDF.NET:

* Modify page rotation and the visible part ("cropbox") of the page.
* Insert images, other PDF pages, text and simple geometrical objects.
* Add annotations and form fields.

.. note::

   Methods require coordinates (points, rectangles) to put content in desired places. Please be aware that these coordinates **must always** be provided relative to the **unrotated** page. The reverse is also true: except :attr:`Page.Rect`, resp. :meth:`Page.GetBound` (both *reflect* when the page is rotated), all coordinates returned by methods and attributes pertain to the unrotated page.

   So the returned value of e.g. :meth:`Page.GetImageBbox` will not change if you do a :meth:`Page.SetRotation`. The same is true for coordinates returned by :meth:`Page.GetText`, annotation rectangles, and so on. If you want to find out, where an object is located in **rotated coordinates**, multiply the coordinates with :attr:`Page.RotationMatrix`. There also is its inverse, :attr:`Page.DerotationMatrix`, which you can use when interfacing with other readers, which may behave differently in this respect.

.. note::

   If you add or update annotations, links or form fields on the page and immediately afterwards need to work with them (i.e. **without leaving the page**), you should reload the page using :meth:`Document.ReloadPage` before referring to these new or updated items.

   Reloading the page is generally recommended -- although not strictly required in all cases. However, some annotation and widget types have extended features in MuPDF.NET compared to MuPDF. More of these extensions may also be added in the future.

   Releoading the page ensures all your changes have been fully applied to PDF structures, so you can safely create Pixmaps or successfully iterate over annotations, links and form fields.

================================== =======================================================
**Method / Attribute**             **Short Description**
================================== =======================================================
:meth:`Page.AddCaretAnnot`         PDF only: add a caret annotation
:meth:`Page.AddCircleAnnot`        PDF only: add a circle annotation
:meth:`Page.AddFileAnnot`          PDF only: add a file attachment annotation
:meth:`Page.AddFreeTextAnnot`      PDF only: add a text annotation
:meth:`Page.AddHighlightAnnot`     PDF only: add a "highlight" annotation
:meth:`Page.AddInkAnnot`           PDF only: add an ink annotation
:meth:`Page.AddLineAnnot`          PDF only: add a line annotation
:meth:`Page.AddPolygonAnnot`       PDF only: add a polygon annotation
:meth:`Page.AddPolylineAnnot`      PDF only: add a multi-line annotation
:meth:`Page.AddRectAnnot`          PDF only: add a rectangle annotation
:meth:`Page.AddRedactAnnot`        PDF only: add a redaction annotation
:meth:`Page.AddSquigglyAnnot`      PDF only: add a "squiggly" annotation
:meth:`Page.AddStampAnnot`         PDF only: add a "rubber stamp" annotation
:meth:`Page.AddStrikeoutAnnot`     PDF only: add a "strike-out" annotation
:meth:`Page.AddTextAnnot`          PDF only: add a comment
:meth:`Page.AddUnderlineAnnot`     PDF only: add an "underline" annotation
:meth:`Page.AddWidget`             PDF only: add a PDF Form field
:meth:`Page.GetAnnotNames`         PDF only: a list of annotation (and widget) names
:meth:`Page.GetAnnotXrefs`         PDF only: a list of annotation (and widget) xrefs
:meth:`Page.GetAnnots`             Return a generator over the annots on the page
:meth:`Page.ApplyRedactions`       PDF only: process the redactions of the page
:meth:`Page.GetBound`              Rectangle of the page
:meth:`Page.GetBboxlog`            List of rectangles that envelop text, drawing or image objects
:meth:`Page.GetContents`           PDF only: return a list of content :data:`xref` numbers
:meth:`Page.GetDisplayList`        Create the page's display list
:meth:`Page.GetTables`             Extract the page's tables as a list
:meth:`Page.GetTextBlocks`         Extract text blocks as a list
:meth:`Page.GetTextWords`          Extract text words as a list
:meth:`Page.GetTextWithLayout`     Retrieves the text content of a page that retains layout.
:meth:`Page.ClusterDrawings`       PDF only: bounding boxes of vector graphics
:meth:`Page.CleanContents`         PDF only: clean the page's :data:`contents` objects
:meth:`Page.DeleteAnnot`           PDF only: delete an annotation
:meth:`Page.DeleteImage`           PDF only: delete an image
:meth:`Page.DeleteLink`            PDF only: delete a link
:meth:`Page.DeleteWidget`          PDF only: delete a widget / field
:meth:`Page.DrawBezier`            PDF only: draw a cubic Bezier curve
:meth:`Page.DrawCircle`            PDF only: draw a circle
:meth:`Page.DrawCurve`             PDF only: draw a special Bezier curve
:meth:`Page.DrawLine`              PDF only: draw a line
:meth:`Page.DrawOval`              PDF only: draw an oval / ellipse
:meth:`Page.DrawPolyline`          PDF only: connect a point sequence
:meth:`Page.DrawQuad`              PDF only: draw a quad
:meth:`Page.DrawRect`              PDF only: draw a rectangle
:meth:`Page.DrawSector`            PDF only: draw a circular sector
:meth:`Page.DrawSquiggle`          PDF only: draw a squiggly line
:meth:`Page.DrawZigzag`            PDF only: draw a zig-zagged line
:meth:`Page.GetDrawings`           Get vector graphics on page
:meth:`Page.GetFonts`              PDF only: get list of referenced fonts
:meth:`Page.GetImageBbox`          PDF only: get bbox and matrix of embedded image
:meth:`Page.GetImageInfo`          Get list of meta information for all used images
:meth:`Page.GetImageRects`         PDF only: improved version of :meth:`Page.GetImageBbox`
:meth:`Page.GetImages`             PDF only: get list of referenced images
:meth:`Page.GetLabel`              PDF only: return the label of the page
:meth:`Page.GetLinks`              Get all links
:meth:`Page.GetPixmap`             Create a page image in raster format
:meth:`Page.GetSvgImage`           Create a page image in SVG format
:meth:`Page.GetText`               Extract the page's text
:meth:`Page.GetTextbox`            Extract text contained in a rectangle
:meth:`Page.GetTextPageOcr`        Create a TextPage with OCR for the page
:meth:`Page.GetTextPage`           Create a TextPage for the page
:meth:`Page.GetXObjects`           PDF only: get list of referenced xobjects
:meth:`Page.InsertFont`            PDF only: insert a font for use by the page
:meth:`Page.InsertImage`           PDF only: insert an image
:meth:`Page.InsertLink`            PDF only: insert a link
:meth:`Page.InsertText`            PDF only: insert text
:meth:`Page.InsertHtmlBox`         PDF only: insert html text in a rectangle
:meth:`Page.InsertTextbox`         PDF only: insert a text box
:meth:`Page.GetLinks`              Return a generator of the links on the page
:meth:`Page.LoadAnnot`             PDF only: load a specific annotation
:meth:`Page.LoadWidget`            PDF only: load a specific field
:meth:`Page.LoadLinks`             Return the first link on a page
:meth:`Page.NewShape`              PDF only: create a new :ref:`Shape`
:meth:`Page.ReadBarcodes`          Reads the barcodes from a supplied :ref:`Rect`
:meth:`Page.WriteBarcode`          Creates a barcode from the supplied parameters
:meth:`Page.RemoveRotation`        PDF only: set page rotation to 0
:meth:`Page.ReplaceImage`          PDF only: replace an image
:meth:`Page.Recolor`               PDF only: recolor page
:meth:`Page.ReadContents`          PDF only: get complete, concatenated /Contents source
:meth:`Page.Run`                   Run a page through a device
:meth:`Page.SearchFor`             Search for a string
:meth:`Page.SetCropBox`            PDF only: modify the `/Cropbox` (visible page)
:meth:`Page.SetArtBox`             PDF only: modify `/ArtBox`
:meth:`Page.SetBleedBox`           PDF only: modify `/BleedBox`
:meth:`Page.SetMediaBox`           PDF only: modify `/MediaBox`
:meth:`Page.SetTrimBox`            PDF only: modify `/TrimBox`
:meth:`Page.SetRotation`           PDF only: set page rotation
:meth:`Page.ShowPdfPage`           PDF only: display PDF page image
:meth:`Page.SetContents`           PDF only: set page's :data:`contents` to some :data:`xref`
:meth:`Page.UpdateLink`            PDF only: modify a link
:meth:`Page.GetWidgets`            Return a generator over the fields on the page
:meth:`Page.WriteText`             Write one or more :ref:`Textwriter` objects
:meth:`Page.WrapContents`          Wrap contents with stacking commands
:attr:`Page.CropBoxPosition`       Displacement of the :data:`cropbox`
:attr:`Page.CropBox`               The page's :data:`cropbox`
:attr:`Page.ArtBox`                The page's `/ArtBox`
:attr:`Page.BleedBox`              The page's `/BleedBox`
:attr:`Page.TrimBox`               The page's `/TrimBox`
:attr:`Page.DerotationMatrix`      PDF only: get coordinates in unrotated page space
:attr:`Page.FirstAnnot`            First :ref:`Annot` on the page
:attr:`Page.FirstLink`             First :ref:`Link` on the page
:attr:`Page.FirstWidget`           First widget (form field) on the page
:attr:`Page.MediaBoxSize`          Bottom-right point of :data:`mediabox`
:attr:`Page.MediaBox`              The page's :data:`mediabox`
:attr:`Page.Number`                Page number
:attr:`Page.Parent`                Owning document object
:attr:`Page.Rect`                  Rectangle of the page
:attr:`Page.RotationMatrix`        PDF only: get coordinates in rotated page space
:attr:`Page.Rotation`              PDF only: page rotation
:attr:`Page.TransformationMatrix`  PDF only: translate between PDF and MuPDF space
:attr:`Page.Xref`                  PDF only: page :data:`xref`
:attr:`Page.IsWrapped`             PDF only: check whether contents wrapping is present
================================== =======================================================

**Class API**

.. class:: Page

   .. method:: GetBound()

      Determine the rectangle of the page. Same as property :attr:`Page.Rect`. For PDF documents this **usually** also coincides with :data:`Mediabox` and :data:`CropBox`, but not always. For example, if the page is rotated, then this is reflected by this method -- the :attr:`Page.CropBox` however will not change.

      :rtype: :ref:`Rect`

   .. method:: AddCaretAnnot(Point point)

      PDF only: Add a caret icon. A caret annotation is a visual symbol normally used to indicate the presence of text edits on the page.

      :arg Point point: the top left point of a 20 x 20 rectangle containing the MuPDF-provided icon.

      :rtype: :ref:`Annot`
      :returns: the created annotation. Stroke color blue = (0, 0, 1), no fill color support.

      .. image:: ../images/img-caret-annot.*
         :scale: 70


   .. method:: AddTextAnnot(Point point, string text, string icon: "Note")

      PDF only: Add a comment icon ("sticky note") with accompanying text. Only the icon is visible, the accompanying text is hidden and can be visualized by many PDF viewers by hovering the mouse over the symbol.

      :arg Point point: the top left point of a 20 x 20 rectangle containing the MuPDF-provided "note" icon.

      :arg string text: the commentary text. This will be shown on double clicking or hovering over the icon. May contain any Latin characters.
      :arg string icon: choose one of "Note" (default), "Comment", "Help", "Insert", "Key", "NewParagraph", "Paragraph" as the visual symbol for the embodied text [#f4]_.

      :rtype: :ref:`Annot`
      :returns: the created annotation. Stroke color yellow = (1, 1, 0), no fill color support.

   .. index::
      pair: color; AddFreeTextAnnot
      pair: fontName; AddFreeTextAnnot
      pair: fontSize; AddFreeTextAnnot
      pair: rect; AddFreeTextAnnot
      pair: rotate; AddFreeTextAnnot
      pair: align; AddFreeTextAnnot
      pair: textColor; AddFreeTextAnnot
      pair: borderColor; AddFreeTextAnnot
      pair: fillColor; AddFreeTextAnnot

   .. method:: AddFreeTextAnnot(Rect rect, string text, int fontSize = 11, string fontName = null, float[] textColor = null, float[] fillColor = null, float[] borderColor = null, float borderWidth = 0.0f, int[] dashes = null, Point[] callout = null, PdfLineEnding lineEnd = PdfLineEnding.PDF_ANNOT_LE_OPEN_ARROW, float opacity = 1.0f, int align = 0, int rotate = 0, bool richtext = false, string style = null)

      PDF only: Add text in a given rectangle.

      :arg Rect rect: the rectangle into which the text should be inserted. Text is automatically wrapped to a new line at box width. Lines not fitting into the box will be invisible.

      :arg string text: the text. May contain any mixture of Latin, Greek, Cyrillic, Chinese, Japanese and Korean characters. The respective required font is automatically determined.
      :arg float fontSize: the :data:`fontSize`. Default is 11.
      :arg str fontName: the font name.
        Accepted alternatives are "Cour", "TiRo", "ZaDb" and "Symb".
        The name may be abbreviated to the first two characters, like "Co" for "Cour".
        Lower case is also accepted.
        Bold or italic variants of the fonts are **not accepted**.
        A user-contributed script provides a circumvention for this restriction -- see section *Using Buttons and JavaScript* in chapter :ref:`FAQ`.
        The actual font to use is now determined on a by-character level, and all required fonts (or sub-fonts) are automatically included.
        Therefore, you should rarely ever need to care about this parameter and let it default (except you insist on a serifed font for your non-CJK text parts).
        
      :arg float[] textColor: the text color. Default is black.

      :arg float[] fillColor: the fill color. Default is white.
      :arg float[] textColor: the text color. Default is black.
      :arg float[] borderColor: the border color. Default is `null`. 
      :arg float borderWidth: the border width. Default is `0.0f`, no border width.
      :arg int[] dashes: the dash pattern for a border.
      :arg Point[] callout: define end, knee, start points.
      :arg PdfLineEnding lineEnd: symbol shown at point 3. (NONE, SQUARE, CIRCLE, DIAMOND, OPEN_ARROW, CLOSED_ARROW, BUTT, R_OPEN_ARROW, R_CLOSED_ARROW, SLASH)
      :arg float opacity: the opacity. Default is `1.0f`.
      :arg int align: text alignment, one of TEXT_ALIGN_LEFT, TEXT_ALIGN_CENTER, TEXT_ALIGN_RIGHT - justify is **not supported**.

      :arg int rotate: the text orientation. Accepted values are 0, 90, 270, invalid entries are set to zero.
      :arg bool richtext: if this is rich text or not.
      :arg string style: customized style string.

      :rtype: :ref:`Annot`
      :returns: the created annotation. Color properties **can only be changed** using special parameters of :meth:`Annot.Update`. There, you can also set a border color different from the text color.


   .. method:: AddFileAnnot(Point pos, byte[] buffer, string fileName, dynamic uFilename: null, string desc: null, string icon: "PushPin")

      PDF only: Add a file attachment annotation with a "PushPin" icon at the specified location.

      :arg Point pos: the top-left point of a 18x18 rectangle containing the MuPDF-provided "PushPin" icon.

      :arg byte[] buffer: the data to be stored (actual file content, any data, etc.).

      :arg string fileName: the filename to associate with the data.
      :arg string uFilename: the optional PDF unicode version of filename. Defaults to filename.
      :arg string desc: an optional description of the file. Defaults to filename.
      :arg string icon: choose one of "PushPin" (default), "Graph", "Paperclip", "Tag" as the visual symbol for the attached data [#f4]_.

      :rtype: :ref:`Annot`
      :returns: the created annotation.  Stroke color yellow = (1, 1, 0), no fill color support.

   .. method:: AddInkAnnot(List<List<Point>> list)

      PDF only: Add a "freehand" scribble annotation.

      :arg List<List<Point>> list: a list of one or more lists, each containing :data:`Point` items. Each item in these sublists is interpreted as a :ref:`Point` through which a connecting line is drawn. Separate sublists thus represent separate drawing lines.

      :rtype: :ref:`Annot`
      :returns: the created annotation in default appearance black =(0, 0, 0),line width 1. No fill color support.

   .. method:: AddLineAnnot(Point p1, Point p2)

      PDF only: Add a line annotation.

      :arg Point p1: the starting point of the line.

      :arg Point p2: the end point of the line.

      :rtype: :ref:`Annot`
      :returns: the created annotation. It is drawn with line (stroke) color red = (1, 0, 0) and line width 1. No fill color support. The **annot rectangle** is automatically created to contain both points, each one surrounded by a circle of radius 3 * line width to make room for any line end symbols.

   .. method:: AddRectAnnot(Rect rect)

   .. method:: AddCircleAnnot(Rect rect)

      PDF only: Add a rectangle, resp. circle annotation.

      :arg Rect rect: the rectangle in which the circle or rectangle is drawn, must be finite and not empty. If the rectangle is not equal-sided, an ellipse is drawn.

      :rtype: :ref:`Annot`
      :returns: the created annotation. It is drawn with line (stroke) color red = (1, 0, 0), line width 1, fill color is supported.

   ---------

   Redactions
   ~~~~~~~~~~~

   .. method:: AddRedactAnnot(Quad quad, string text, string fontName, int fontSize: 11, int align: TEXT_ALIGN_LEFT, float[] fill: (1, 1, 1), float[] textColor: (0, 0, 0), bool crossOut: true)
      
      **PDF only**: Add a redaction annotation. A redaction annotation identifies content to be removed from the document. Adding such an annotation is the first of two steps. It makes visible what will be removed in the subsequent step, :meth:`Page.ApplyRedactions`.

      :arg Quad quad: specifies the (rectangular) area to be removed which is always equal to the annotation rectangle. This may be a :data:`Rect` or :data:`Quad` object. If a quad is specified, then the enveloping rectangle is taken.

      :arg string text: text to be placed in the rectangle after applying the redaction (and thus removing old content).

      :arg string fontName: the font to use when *text* is given, otherwise ignored. The same rules apply as for :meth:`Page.InsertTextbox` -- which is the method :meth:`Page.ApplyRedactions` internally invokes. The replacement text will be **vertically centered**, if this is one of the CJK or :ref:`Base-14-Fonts`.

         .. note::

            * For an **existing** font of the page, use its reference name as *fontName* (this is *item[4]* of its entry in :meth:`Page.GetFonts`).


      :arg float fontSize: the :data:`fontSize` to use for the replacing text. If the text is too large to fit, several insertion attempts will be made, gradually reducing the :data:`fontSize` to no less than 4. If then the text will still not fit, no text insertion will take place at all.
      :arg int align: the horizontal alignment for the replacing text. See :meth:`InsertTextbox` for available values. The vertical alignment is (approximately) centered if a PDF built-in font is used (CJK or :ref:`Base-14-Fonts`).

      :arg float[] fill: the fill color of the rectangle **after applying** the redaction. The default is *white = (1, 1, 1)*, which is also taken if *null* is specified. To suppress a fill color altogether, specify *false*. In this cases the rectangle remains transparent.

      :arg float[] textColor: the color of the replacing text. Default is *black = (0, 0, 0)*.

      :arg bool crossOut: add two diagonal lines to the annotation rectangle.

      :rtype: :ref:`Annot`
      :returns: the created annotation. Its standard appearance looks like a red rectangle (no fill color), optionally showing two diagonal lines. Colors, line width, dashing, opacity and blend mode can now be set and applied via :meth:`Annot.update` like with other annotations.

      .. image:: ../images/img-redact.*


      .. method:: ApplyRedactions(int images: PDF_REDACT_IMAGE_PIXELS|2, int graphics: PDF_REDACT_LINE_ART_IF_TOUCHED|2, int text: PDF_REDACT_TEXT_REMOVE|0)

      **PDF only**: Remove all **content** contained in any redaction rectangle on the page.

      **This method applies and then deletes all redactions from the page.**

      :arg int images: How to redact overlapping images. The default (2) blanks out overlapping pixels. `PDF_REDACT_IMAGE_NONE | 0` ignores, and `PDF_REDACT_IMAGE_REMOVE | 1` completely removes images overlapping any redaction annotation. Option `PDF_REDACT_IMAGE_REMOVE_UNLESS_INVISIBLE | 3` only removes images that are actually visible.

      :arg int graphics: How to redact overlapping vector graphics (also called "line-art" or "drawings"). The default (2) removes any overlapping vector graphics. `PDF_REDACT_LINE_ART_NONE | 0` ignores, and `PDF_REDACT_LINE_ART_IF_COVERED | 1` removes graphics fully contained in a redaction annotation. When removing line-art, please be aware that **stroked** vector graphics (i.e. type "s" or "sf") have a **larger wrapping rectangle** than one might expect: first of all, at least 50% of the path's line width have to be added in each direction to truly include all of the drawing. If a so-called "miter limit" is provided (see page 121 of the PDF specification), the enlarging value is `miter * width / 2`. So, when letting everything default (width = 1, miter = 10), the redaction rectangle should be at least 5 points larger in every direction.

      :arg int text: Whether to redact overlapping text. The default `PDF_REDACT_TEXT_REMOVE | 0` removes all characters whose boundary box overlaps any redaction rectangle. This complies with the original legal / data protection intentions of redaction annotations. Other use cases however may require to **keep text** while redacting vector graphics or images. This can be achieved by setting `text=true|PDF_REDACT_TEXT_NONE | 1`. This does **not comply** with the data protection intentions of redaction annotations. **Do so at your own risk.**

      :returns: `true` if at least one redaction annotation has been processed, `false` otherwise.

      .. note::
         * Text contained in a redaction rectangle will be **physically** removed from the page (assuming :meth:`Document.Save` with a suitable garbage option) and will no longer appear in e.g. text extractions or anywhere else. All redaction annotations will also be removed. Other annotations are unaffected.

         * All overlapping links will be removed. If the rectangle of the link was covering text, then only the overlapping part of the text is being removed. Similar applies to images covered by link rectangles.

         * The overlapping parts of **images** will be blanked-out for default option `PDF_REDACT_IMAGE_PIXELS`. Option 0 does not touch any images and 1 will remove any image with an overlap.

         * For option `images=PDF_REDACT_IMAGE_REMOVE` only this page's **references to the images** are removed - not necessarily the images themselves. Images are completely removed from the file only, if no longer referenced at all (assuming suitable garbage collection options).

         * For option `images=PDF_REDACT_IMAGE_PIXELS` a new image of format PNG is created, which the page will use in place of the original one. The original image is not deleted or replaced as part of this process, so other pages may still show the original. In addition, the new, modified PNG image currently is **stored uncompressed**. Do keep these aspects in mind when choosing the right garbage collection method and compression options during save.

         * **Text removal** is done by character: A character is removed if its bbox has a **non-empty overlap** with a redaction rectangle. Depending on the font properties and / or the chosen line height, deletion may occur for undesired text parts.

         * Redactions are a simple way to replace single words in a PDF, or to just physically remove them. Locate the word "secret" using some text extraction or search method and insert a redaction using "xxxxxx" as replacement text for each occurrence.

           - Be wary if the replacement is longer than the original -- this may lead to an awkward appearance, line breaks or no new text at all.

           - For a number of reasons, the new text may not exactly be positioned on the same line like the old one -- especially true if the replacement font was not one of CJK or :ref:`Base-14-Fonts`.


      ---------

   .. method:: AddPolylineAnnot(List<Point> points)

   .. method:: AddPolygonAnnot(List<Point> points)

      PDF only: Add an annotation consisting of lines which connect the given points. A **Polygon's** first and last points are automatically connected, which does not happen for a **PolyLine**. The **rectangle** is automatically created as the smallest rectangle containing the points, each one surrounded by a circle of radius 3 (= 3 * line width). The following shows a 'PolyLine' that has been modified with colors and line ends.

      :arg List<Point> points: a list of :data:`Point` objects.

      :rtype: :ref:`Annot`
      :returns: the created annotation. It is drawn with line color black, line width 1 no fill color but fill color support. Use methods of :ref:`Annot` to make any changes to achieve something like this:

      .. image:: ../images/img-polyline.*
         :scale: 70

   .. method:: AddUnderlineAnnot(dynamic quads: null, Point start: null, Pint stop: null, Rect clip: null)

   .. method:: AddStrikeoutAnnot(dynamic quads: null, Point start: null, Point stop: null, Rect clip: null)

   .. method:: AddSquigglyAnnot(dynamic quads: null, Point start: null, Point stop: null, Rect clip: null)

   .. method:: AddHighlightAnnot(dynamic quads: null, Point start: null, Point stop: null, Rect clip: null)

      PDF only: These annotations are normally used for **marking text** which has previously been somehow located (for example via :meth:`Page.SearchFor`). But this is not required: you are free to "mark" just anything.

      Standard (stroke only -- no fill color support) colors are chosen per annotation type: **yellow** for highlighting, **red** for striking out, **green** for underlining, and **magenta** for wavy underlining.

      All these four methods convert the arguments into a list of :ref:`Quad` objects. The **annotation** rectangle is then calculated to envelop all these quadrilaterals.

      .. note::
        Obviously, text marker annotations need to know what is the top, the bottom, the left, and the right side of the area(s) to be marked. If the arguments are quads, this information is given by the sequence of the quad points. In contrast, a rectangle delivers much less information -- this is illustrated by the fact, that 4! = 24 different quads can be constructed with the four corners of a rectangle.

        Therefore, we **strongly recommend** to use the `quads` option for text searches, to ensure correct annotations. A similar consideration applies to marking **text spans** extracted with the "dict" / "rawdict" options of :meth:`Page.GetText`. For more details on how to compute quadrilaterals in this case, see section "How to Mark Non-horizontal Text" of :ref:`FAQ`.

      :arg Rect, Quad, List<Quad>:
        the location(s) -- rectangle(s) or quad(s) -- to be marked.
        A list or tuple must consist of :data:`Rect` or :data:`Quad` items (or even a mixture of either).
        Every item must be finite, convex and not empty (as applicable).
        **Set this parameter to** *null* if you want to use the following arguments.
        And vice versa: if not *null*, the remaining parameters must be *null*.
        
      :arg Point start: start text marking at this point. Defaults to the top-left point of *clip*. Must be provided if `quads` is *null*. 
      :arg Point stop: stop text marking at this point. Defaults to the bottom-right point of *clip*. Must be used if `quads` is *null*. 
      :arg Rect clip: only consider text lines intersecting this area. Defaults to the page rectangle. Only use if `start` and `stop` are provided.

      :rtype: :ref:`Annot` or *null*.
      :returns: the created annotation. If *quads* is an empty list, **no annotation** is created.

      .. note::
        You can use parameters *start*, *stop* and *clip* to highlight consecutive lines between the points *start* and *stop* (starting with v1.16.14).
        Make use of *clip* to further reduce the selected line bboxes and thus deal with e.g. multi-column pages.
        The following multi-line highlight on a page with three text columns was created by specifying the two red points and setting clip accordingly.

      .. image:: ../images/img-markers.*
         :scale: 100

   .. method:: ClusterDrawings(Rect clip: null, List<PathInfo> drawings: null, float xTolerance=3, float yTolerance=3)

      Cluster vector graphics (synonyms are line-art or drawings) based on their geometrical vicinity. The method walks through the output of :meth:`Page.GetDrawings` and joins paths whose `path["rect"]` are closer to each other than some tolerance values (given in the arguments). The result is a list of rectangles that each wrap things like tables (with gridlines), pie charts, bar charts, etc.

      :arg Rect clip: only consider paths inside this area. The default is the full page.

      :arg List<PathInfo> drawings: (optional) provide a previously generated output of :meth:`Page.GetDrawings`. If `null` the method will execute the method.

      :arg float xTolerance:

      :arg float yTolerance:

   .. method:: CleanContents(int sanitize: 1)

      PDF only: Clean and concatenate all :data:`contents` objects associated with this page. "Cleaning" includes syntactical corrections, standardizations and "pretty printing" of the contents stream. Discrepancies between :data:`contents` and :data:`resources` objects will also be corrected if sanitize is true. See :meth:`Page.GetContents` for more details.

      :arg bool sanitize: if true, synchronization between resources and their actual use in the contents object is snychronized. For example, if a font is not actually used for any text of the page, then it will be deleted from the `/Resources/Font` object.

      .. warning:: This is a complex function which may generate large amounts of new data and render old data unused. It is **not recommended** using it together with the **incremental save** option. Also note that the resulting singleton new */Contents* object is **uncompressed**. So you should save to a **new file** using options *"deflate=True, garbage=3"*.
   
   .. method:: AddStampAnnot(Rect rect, object stamp: null)

      PDF only: Add a "rubber stamp" like annotation to e.g. indicate the document's intended use ("DRAFT", "CONFIDENTIAL", etc.).

      :arg Rect rect: rectangle where to place the annotation.

      :arg object stamp: stamp object, can be as ``int``, ``pixmap``, ``filepath``, ``byte[]``, ``MemoryStream``.

      .. note::

         * The stamp's text and its border line will automatically be sized and be put horizontally and vertically centered in the given rectangle. :attr:`Annot.Rect` is automatically calculated to fit the given **width** and will usually be smaller than this parameter.
         * The font chosen is "Times Bold" and the text will be upper case.
         * The appearance can be changed using :meth:`Annot.SetOpacity` and by setting the "stroke" color (no "fill" color supported).
         * This can be used to create watermark images: on a temporary PDF page create a stamp annotation with a low opacity value, make a pixmap from it with *alpha=true* (and potentially also rotate it), discard the temporary PDF page and use the pixmap with :meth:`InsertImage` for your target PDF.


      .. image:: ../images/img-stampannot.*
         :scale: 80

   .. method:: AddWidget(widget)

      PDF only: Add a PDF Form field ("widget") to a page. This also **turns the PDF into a Form PDF**. Because of the large amount of different options available for widgets, we have developed a new class :ref:`Widget`, which contains the possible PDF field attributes. It must be used for both, form field creation and updates.

      :arg widget: a :ref:`Widget` object which must have been created upfront.
      :type widget: :ref:`Widget`

      :returns: a widget annotation.

   .. method:: DeleteAnnot(Annot annot)

      * The removal will now include any bound 'Popup' or response annotations and related objects.

      PDF only: Delete annotation from the page and return the next one.

      :arg annot: the annotation to be deleted.
      :type annot: :ref:`Annot`

      :rtype: :ref:`Annot`
      :returns: the annotation following the deleted one. Please remember that physical removal requires saving to a new file with garbage > 0.

   .. method:: DeleteWidget(Widget widget)

      PDF only: Delete field from the page and return the next one.

      :arg widget: the widget to be deleted.
      :type widget: :ref:`Widget`

      :rtype: :ref:`Widget`
      :returns: the widget following the deleted one. Please remember that physical removal requires saving to a new file with garbage > 0.



   .. method:: DeleteLink(Link linkdict)

      PDF only: Delete the specified link from the page. The parameter must be an **original item** of :meth:`GetLinks()`, see :ref:`link_dict_description`. The reason for this is the dictionary's *"xref"* key, which identifies the PDF object to be deleted.

      :arg Link linkdict: the link to be deleted.

   .. method:: InsertLink(Link linkdict)

      PDF only: Insert a new link on this page. The parameter must be a dictionary of format as provided by :meth:`GetLinks()`, see :ref:`link_dict_description`.

      :arg dict Link: the link to be inserted.

   .. method:: UpdateLink(Link linkdict)

      PDF only: Modify the specified link. The parameter must be a (modified) **original item** of :meth:`GetLinks()`, see :ref:`link_dict_description`. The reason for this is the dictionary's *"xref"* key, which identifies the PDF object to be changed.

      :arg Link linkdict: the link to be modified.

      .. warning:: If updating / inserting a URI link (`"kind": LINK_URI`), please make sure to start the value for the `"uri"` key with a disambiguating string like `"http://"`, `"https://"`, `"file://"`, `"ftp://"`, `"mailto:"`, etc. Otherwise -- depending on your browser or other "consumer" software -- unexpected default assumptions may lead to unwanted behaviours.


   .. method:: GetLabel()

      PDF only: Return the label for the page.

      :rtype: string

      :returns: the label string like "vii" for Roman numbering or "" if not defined.


   .. method:: GetLinks()

      Retrieves **all** links of a page.

      :rtype: List
      :returns: A list of `LinkInfo`. For a description of the dictionary entries, see :ref:`link_dict_description`. Always use this or the :meth:`Page.GetLinks` method if you intend to make changes to the links of a page.

   .. method:: GetLinks()

      Return a generator over the page's links. The results equal the entries of :meth:`Page.GetLinks`.

      :arg sequence kinds: a sequence of integers to down-select to one or more link kinds. Default is all links. Example: *kinds=(MuPDFLINK_GOTO,)* will only return internal links.

      :rtype: generator
      :returns: an entry of :meth:`Page.GetLinks()` for each iteration.


   .. method:: GetAnnots(List<PdfAnnotType> types: null)

      Return a generator over the page's annotations.

      :arg List<PdfAnnotType> types: a sequence of integers to down-select to one or more annotation types. Default is all annotations. Example: `types=(PDF_ANNOT_FREETEXT, PDF_ANNOT_TEXT)` will only return 'FreeText' and 'Text' annotations.

      :rtype: IEnumerable
      :returns: an :ref:`Annot` for each iteration.

   .. method:: GetBboxlog(bool layer: false)

      :returns: a list of :ref:`BoxLog` that envelop text, image or drawing objects. Each item is a BoxLog `(type, (x0, y0, x1, y1), layername)` where the second tuple consists of rectangle coordinates, and *type* is one of the following values. If `layers = true`, there is a third item containing the OCG name or `null`: `(type, (x0, y0, x1, y1), null)`.

         * `"fill-text"` -- normal text (painted without character borders)
         * `"stroke-text"` -- text showing character borders only
         * `"ignore-text"` -- text that should not be displayed (e.g. as used by OCR text layers)
         * `"fill-path"` -- drawing with fill color (and no border)
         * `"stroke-path"` -- drawing with border (and no fill color)
         * `"fill-image"` -- display an image
         * `"fill-shade"` -- display a shading

         The item sequence represents the **sequence in which these commands are executed** to build the page's appearance. Therefore, if an item's bbox intersects or contains that of a previous item, then the previous item may be (partially) covered / hidden.

         So this list can be used to detect such situations. An item's index in this list equals the value of a `"SeqNo"` in dictionaries as returned by :meth:`Page.GetDrawings` and :meth:`Page.GetTexttrace`.

   .. method:: GetContents()

      PDF only: Retrieve a list of :data:`xref` of :data:`contents` objects of a page. May be empty or contain multiple integers. If the page is cleaned (:meth:`Page.CleanContents`), it will be one entry at most. The "source" of each `/Contents` object can be individually read by :meth:`Document.GetXrefStream` using an item of this list. Method :meth:`Page.ReadContents` in contrast walks through this list and concatenates the corresponding sources into one `bytes` object.

      :rtype: List
      :return: a list of `int`, content's xref

   .. method:: GetDisplayList(int annots: 1)

      Run a page through a list device and return its display list.

      :rtype: :ref:`DisplayList`
      :returns: the display list of the page.

   .. method:: GetTables(Rect clip: null,  string strategy: null, string vertical_strategy: "lines", string horizontal_strategy: "lines", List<Line> add_lines: null, List<Edge> vertical_lines: null, List<Edge> horizontal_lines: null, float snap_tolerance: TableFlags.TABLE_DEFAULT_SNAP_TOLERANCE, float snap_x_tolerance: 0.0f, float snap_y_tolerance: 0.0f, float join_tolerance: TableFlags.TABLE_DEFAULT_JOIN_TOLERANCE, float join_x_tolerance: 0.0f, float join_y_tolerance: 0.0f, float edge_min_length: 3.0f, float min_words_vertical: TableFlags.TABLE_DEFAULT_MIN_WORDS_VERTICAL, float min_words_horizontal: TableFlags.TABLE_DEFAULT_MIN_WORDS_HORIZONTAL, float intersection_tolerance: 3.0f, float intersection_x_tolerance: 0.0f, float intersection_y_tolerance: 0.0f, float text_tolerance: 3.0f, float text_x_tolerance: 3.0f, float text_y_tolerance: 3.0f)

      Find tables on the page and return a list with related information. Typically, the default values of the many parameters will be sufficient. Adjustments should ever only be needed in corner case situations.

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
   
   .. method:: GetTextBlocks(Rect clip: null, int flags: 0, TextPage textPage: null, bool sort: false)

      Deprecated wrapper for :meth:`TextPage.ExtractBlocks`.  Use :meth:`Page.GetText` with the "blocks" option instead.

      :rtype: List
      :return: a list of `TextBlock`

   .. method:: GetTextWords(Rect clip: null, int flags: 0, TextPage textPage: null, bool sort: false, char[] delimiters: null)

      Deprecated wrapper for :meth:`TextPage.ExtractWords`. Use :meth:`Page.GetText` with the "words" option instead.

      :rtype: List
      :return: a list of `WordBlock`

   .. method:: GetTextWithLayout(Rect clip: null, int flags: 0, int tolerance: 5)

      Retrieves the text content of a page that retains layout. Positioning of text is adjusted by spaces.

      :arg Rect clip: Specify a region to consider within the page rectangle and ignore the rest. Default `null` is the full page.
      :arg int flags: Indicator bits to control whether to include images or how text should be handled with respect to white spaces and ligatures.
      :arg int tolerance: Neighborhood threshold.

      :rtype: string
      :return: a string containing the text with layout applied.

   .. method:: GetTextTrace()

      Return low-level text information of the page. The method is available for **all** document types. The result is a list of SpanInfo.

      :rtype: List
      :return: a list of `SpanInfo`

   .. method:: ReadContents()

      Return the concatenation of all :data:`contents` objects associated with the page -- without cleaning or otherwise modifying them. Use this method whenever you need to parse this source in its entirety without having to bother how many separate contents objects exist.

      :rtype: byte[]

   .. method:: Run()

      Run a page through a device.

      :arg dev: Device, obtained from one of the :ref:`Device` constructors.
      :type dev: :ref:`Device`

      :arg transform: Transformation to apply to the page. Set it to :ref:`Identity` if no transformation is desired.
      :type transform: :ref:`Matrix`

   .. method:: GetWidgets(int[] type: null)

      Return a generator over the page's form fields.

      :arg int[] types: a sequence of integers to down-select to one or more widget types. Default is all form fields. Example: `types=(PDF_WIDGET_TYPE_TEXT,)` will only return 'Text' fields.

      :rtype: IEnumerable<Widget>
      :returns: a :ref:`Widget` for each iteration.

   .. method:: WriteText(Rect rect: null, List<TextWriter> writers: null, bool overlay: true, float[] color: null, float opacity: -1, bool keepProportion: true, int rotate: 0, int oc: 0)

      PDF only: Write the text of one or more :ref:`TextWriter` objects to the page.

      :arg Rect rect: where to place the text. If omitted, the rectangle union of the text writers is used.
      :arg List<TextWriter> writers: a non-empty tuple / list of :ref:`TextWriter` objects or a single :ref:`TextWriter`.
      :arg float opacity: set transparency, overwrites resp. value in the text writers.
      :arg float[] color: set the text color, overwrites  resp. value in the text writers.
      :arg bool overlay: put the text in foreground or background.
      :arg bool keepProportion: maintain the aspect ratio.
      :arg float rotate: rotate the text by an arbitrary angle.
      :arg int oc: the :data:`xref` of an :data:`OCG` or :data:`OCMD`. 

      .. note:: Parameters *overlay, keepProportion, rotate* and *oc* have the same meaning as in :meth:`Page.ShowPdfPage`.


   .. index::
      pair: borderWidth; InsertText
      pair: color; InsertText
      pair: encoding; InsertText
      pair: fill; InsertText
      pair: fontFile; InsertText
      pair: fontName; InsertText
      pair: fontSize; InsertText
      pair: morph; InsertText
      pair: overlay; InsertText
      pair: renderMode; InsertText
      pair: rotate; InsertText
      pair: strokeOpacity; InsertText
      pair: fillOpacity; InsertText
      pair: oc; InsertText

   .. method:: InsertText( Point point, dynamic text, string fontName, string fontFile, float fontSize: 11, float lineHeight: 0, int setSimple: 0, int encoding: 0, float[] color = null, float[] fill = null, float borderWidth = 0.05f, int renderMode = 0, int rotate = 0, Morph morph = null, bool overlay = true, float strokeOpacity: 1, float fillOpacity: 1, int oc: 0)

      PDF only: Insert text starting at :data:`Point` *point*. See :meth:`Shape.InsertText`.


   .. index::
      pair: align; InsertTextbox
      pair: borderWidth; InsertTextbox
      pair: color; InsertTextbox
      pair: encoding; InsertTextbox
      pair: expandTabs; InsertTextbox
      pair: fill; InsertTextbox
      pair: fontFile; InsertTextbox
      pair: fontName; InsertTextbox
      pair: fontSize; InsertTextbox
      pair: morph; InsertTextbox
      pair: overlay; InsertTextbox
      pair: renderMode; InsertTextbox
      pair: rotate; InsertTextbox
      pair: strokeOpacity; InsertTextbox
      pair: fillOpacity; InsertTextbox
      pair: oc; InsertTextbox

   .. method:: InsertTextbox(Rect rect, dynamic buffer, string fontName, string fontFile, float fontSize: 11, int idx: 0, float[] color: null, float[] fill: null, int renderMode: 0, float borderWidth: 0.05f, int encoding: TEXT_ENCODING_LATIN, int expandTabs: 1, int align: TEXT_ALIGN_LEFT, int rotate: 0, Morph morph: null, float strokeOpacity: 1, float fillOpacity: 1, int oc: 0, bool overlay: true)

      PDF only: Insert text into the specified :data:`Rect` *rect*. See :meth:`Shape.InsertTextbox`.


   .. index::
      pair: rect; InsertHtmlBox
      pair: text; InsertHtmlBox
      pair: css; InsertHtmlBox
      pair: adjust; InsertHtmlBox
      pair: archive; InsertHtmlBox
      pair: overlay; InsertHtmlBox
      pair: rotate; InsertHtmlBox
      pair: oc; InsertHtmlBox
      pair: opacity; InsertHtmlBox
      pair: morph; InsertHtmlBox

   .. method:: InsertHtmlBox(Rect rect, dynamic text, string css: null, float scaleLow: 0, Archive archive: null, int rotate: 0, int oc: 0, float opacity: 1, bool overlay: true)

      **PDF only:** Insert text into the specified rectangle. The method has similarities with methods :meth:`Page.InsertTextbox` and :meth:`TextWriter.FillTextbox`, but is **much more powerful**. This is achieved by letting a :ref:`Story` object do all the required processing.

      * Parameter `text` may be a string as in the other methods. But it will be **interpreted as HTML source** and may therefore also contain HTML language elements -- including styling. The `css` parameter may be used to pass in additional styling instructions.

      * Automatic line breaks are generated at word boundaries. The "soft hyphen" character `"&#173;"` (or `&shy;`) can be used to cause hyphenation and thus may also cause line breaks. **Forced** line breaks however are only achievable via the HTML tag `<br>` - `"\\n"` is ignored and will be treated like a space.

      * With this method the following can be achieved:

        - Styling effects like bold, italic, text color, text alignment, font size or font switching.
        - The text may include arbitrary languages -- **including right-to-left** languages.
        - Scripts like `Devanagari <https://en.wikipedia.org/wiki/Devanagari>`_ and several others in Asia have a highly complex system of ligatures, where two or more unicodes together yield one glyph. The Story uses the software package `HarfBuzz <https://harfbuzz.github.io/>`_ , to deal with these things and produce correct output.
        - One can also **include images** via HTML tag `<img>` -- the Story will take care of the appropriate layout. This is an alternative option to insert images, compared to :meth:`Page.InsertImage`.
        - HTML tables (tag `<table>`) may be included in the text and will be handled appropriately.
        - Links are automatically generated when present.

      * If content does not fit in the rectangle, the developer has two choices:
         
        - **either** only be informed about this (and accept a no-op, just like with the other textbox insertion methods), 
        - **or** (`scaleLow=0` - the default) scale down the content until it fits.

      :arg Rect rect: rectangle on page to receive the text.
      :arg string,Story text: the text to be written. Can contain a mixture of plain text and HTML tags with styling instructions. Alternatively, a :ref:`Story` object may be specified (in which case the internal Story generation step will be omitted). A Story must have been generated with all required styling and Archive information.
      :arg string css: optional string containing additional CSS instructions. This parameter is ignored if `text` is a Story.
      :arg float scaleLow: if necessary, scale down the content until it fits in the target rectangle. This sets the down scaling limit. Default is 0, no limit. A value of 1 means no down-scaling permitted. A value of e.g. 0.2 means maximum down-scaling by 80%.
      :arg Archive archive: an Archive object that points to locations where to find images or non-standard fonts. If `text` refers to images or non-standard fonts, this parameter is required. This parameter is ignored if `text` is a Story.
      :arg int rotate: one of the values 0, 90, 180, 270. Depending on this, text will be filled:
      
          - 0: top-left to bottom-right.
          - 90: bottom-left to top-right.
          - 180: bottom-right to top-left.
          - 270: top-right to bottom-left.

          .. image:: ../images/img-rotate.*

      :arg int oc:  the xref of an :data:`OCG` / :data:`OCMD` or 0. Please refer to :meth:`Page.ShowPdfPage` for details.
      :arg float opacity: set the fill and stroke opacity of the content. Only values `0 <= opacity < 1` are considered.
      :arg bool overlay: put the text in front of other content. Please refer to :meth:`Page.ShowPdfPage` for details.

      :returns: A tuple of floats `(spare_height, scale)`.

         - `spare_height`: -1 if content did not fit, else >= 0. It is the height of the unused (still available) rectangle stripe. Positive only if scale = 1 (no down-scaling happened).
         - `scale`: down-scaling factor, 0 < scale <= 1.

         Please refer to examples in this section of the recipes: :ref:`RecipesText_I_c`.

      

   **Drawing Methods**

   .. index::
      pair: closePath; DrawLine
      pair: color; DrawLine
      pair: dashes; DrawLine
      pair: fill; DrawLine
      pair: lineCap; DrawLine
      pair: lineJoin; DrawLine
      pair: lineJoin; DrawLine
      pair: morph; DrawLine
      pair: overlay; DrawLine
      pair: width; DrawLine
      pair: strokeOpacity; DrawLine
      pair: fillOpacity; DrawLine
      pair: oc; DrawLine

   .. method:: DrawLine(Point p1, Point p2, float[] color: 0, float[] fill: null, float width: 1, string dashes: null, float lineCap: 0, int lineJoin: 0, bool overlay: true, Morph morph: null, float strokeOpacity: 1, float fillOpacity: 1, int oc: 0)

      PDF only: Draw a line from *p1* to *p2* (:data:`Point` \s). See :meth:`Shape.DrawLine`.


   .. index::
      pair: breadth; DrawZigzag
      pair: closePath; DrawZigzag
      pair: color; DrawZigzag
      pair: dashes; DrawZigzag
      pair: fill; DrawZigzag
      pair: lineCap; DrawZigzag
      pair: lineJoin; DrawZigzag
      pair: morph; DrawZigzag
      pair: overlay; DrawZigzag
      pair: width; DrawZigzag
      pair: strokeOpacity; DrawZigzag
      pair: fillOpacity; DrawZigzag
      pair: oc; DrawZigzag

   .. method:: DrawZigzag(Point p1, Point p2, float breadth: 2, float[] color: 0, float[] fill: null, int width: 1, string dashes: null, int lineCap: 0, int lineJoin: 0, bool overlay: true, Morph morph: null, float strokeOpacity: 1, float fillOpacity: 1, int oc: 0)

      PDF only: Draw a zigzag line from *p1* to *p2* (:data:`Point` \s). See :meth:`Shape.DrawZigzag`.


   .. index::
      pair: breadth; DrawSquiggle
      pair: closePath; DrawSquiggle
      pair: color; DrawSquiggle
      pair: dashes; DrawSquiggle
      pair: fill; DrawSquiggle
      pair: lineCap; DrawSquiggle
      pair: lineJoin; DrawSquiggle
      pair: morph; DrawSquiggle
      pair: overlay; DrawSquiggle
      pair: width; DrawSquiggle
      pair: strokeOpacity; DrawSquiggle
      pair: fillOpacity; DrawSquiggle
      pair: oc; DrawSquiggle

   .. method:: DrawSquiggle(Point p1, Point p2, float breadth: 2, float[] color: 0, float width: 1, string dashes: null, int lineCap: 0, int lineJoin: 0, bool overlay: true, Morph morph: null, float strokeOpacity: 1, float fillOpacity: 1, int oc: 0)

      PDF only: Draw a squiggly (wavy, undulated) line from *p1* to *p2* (:data:`Point` \s). See :meth:`Shape.DrawSquiggle`.


   .. index::
      pair: closePath; DrawCircle
      pair: color; DrawCircle
      pair: dashes; DrawCircle
      pair: fill; DrawCircle
      pair: lineCap; DrawCircle
      pair: lineJoin; DrawCircle
      pair: morph; DrawCircle
      pair: overlay; DrawCircle
      pair: width; DrawCircle
      pair: strokeOpacity; DrawCircle
      pair: fillOpacity; DrawCircle
      pair: oc; DrawCircle

   .. method:: DrawCircle(Point center, float radius, float[] color: null, float[] fill: null, float width: 1, string dashes: null, int lineCap: 0, int lineJoin: 0, bool overlay: true, Morph morph: null, float strokeOpacity: 1, float fillOpacity: 1, int oc: 0)

      PDF only: Draw a circle around *center* (:data:`Point`) with a radius of *radius*. See :meth:`Shape.DrawCircle`.


   .. index::
      pair: rect; DrawOval
      pair: color; DrawOval
      pair: dashes; DrawOval
      pair: fill; DrawOval
      pair: lineCap; DrawOval
      pair: lineJoin; DrawOval
      pair: morph; DrawOval
      pair: overlay; DrawOval
      pair: width; DrawOval
      pair: strokeOpacity; DrawOval
      pair: fillOpacity; DrawOval
      pair: oc; DrawOval

   .. method:: DrawOval(Rect rect, float[] color: null, float[] fill: null, float width: 1, string dashes: null, int lineCap: 0, float lineJoin: 0, bool overlay: true, Morph morph: null, float strokeOpacity: 1, float fillOpacity: 1, int oc: 0)

      PDF only: Draw an oval (ellipse) within the given :data:`Rect` or :data:`Quad`. See :meth:`Shape.DrawOval`.


   .. index::
      pair: center; DrawSector
      pair: point; DrawSector
      pair: color; DrawSector
      pair: dashes; DrawSector
      pair: fill; DrawSector
      pair: fullSector; DrawSector
      pair: lineCap; DrawSector
      pair: lineJoin; DrawSector
      pair: morph; DrawSector
      pair: overlay; DrawSector
      pair: width; DrawSector
      pair: strokeOpacity; DrawSector
      pair: fillOpacity; DrawSector
      pair: oc; DrawSector

   .. method:: DrawSector(Point center, Point point, int beta, float[] color: null, float[] fill: null, float width: 1, string dashes: null, int lineCap: 0, int lineJoin: 0, bool fullSector: true, bool overlay: true, bool closePath: false, Morph morph: null, float strokeOpacity: 1, float fillOpacity: 1, int oc: 0)

      PDF only: Draw a circular sector, optionally connecting the arc to the circle's center (like a piece of pie). See :meth:`Shape.DrawSector`.


   .. index::
      pair: points; DrawPolyline
      pair: color; DrawPolyline
      pair: closePath; DrawCurve
      pair: dashes; DrawPolyline
      pair: fill; DrawPolyline
      pair: lineCap; DrawPolyline
      pair: lineJoin; DrawPolyline
      pair: morph; DrawPolyline
      pair: overlay; DrawPolyline
      pair: width; DrawPolyline
      pair: strokeOpacity; DrawPolyline
      pair: fillOpacity; DrawPolyline
      pair: oc; DrawPolyline

   .. method:: DrawPolyline(Point[] points, float[] color=(0,), float[] fill=null, float width=1, string dashes=null, int lineCap=0, int lineJoin=0, bool overlay=true, bool closePath=false, Morph morph=null, float strokeOpacity=1, float fillOpacity=1, int oc=0)

      PDF only: Draw several connected lines defined by a sequence of :data:`Point` \s. See :meth:`Shape.DrawPolyline`.


   .. index::
      pair: p1; DrawBezier
      pair: p2; DrawBezier
      pair: p3; DrawBezier
      pair: p4; DrawBezier
      pair: color; DrawBezier
      pair: dashes; DrawBezier
      pair: fill; DrawBezier
      pair: lineCap; DrawBezier
      pair: lineJoin; DrawBezier
      pair: morph; DrawBezier
      pair: overlay; DrawBezier
      pair: width; DrawBezier
      pair: strokeOpacity; DrawBezier
      pair: fillOpacity; DrawBezier
      pair: oc; DrawBezier
      pair: closePath; DrawCurve

   .. method:: DrawBezier(Point p1, Point p2, Point p3, Point p4, float[] color: 0, float[] fill: 0, float width: 1, string dashes: null, float lineCap: 0, float lineJoin: 0, bool overlay: true, bool closePath: false, Morph morph: null, float strokeOpacity: 1, float fillOpacity: 1, int oc: 0)

      PDF only: Draw a cubic Bézier curve from *p1* to *p4* with the control points *p2* and *p3* (all are :data:`Point` \s). See :meth:`Shape.DrawBezier`.

   .. index::
      pair: p1; DrawCurve
      pair: p2; DrawCurve
      pair: p3; DrawCurve
      pair: color; DrawCurve
      pair: dashes; DrawCurve
      pair: fill; DrawCurve
      pair: lineCap; DrawCurve
      pair: lineJoin; DrawCurve
      pair: morph; DrawCurve
      pair: overlay; DrawCurve
      pair: width; DrawCurve
      pair: strokeOpacity; DrawCurve
      pair: fillOpacity; DrawCurve
      pair: oc; DrawCurve
      pair: closePath; DrawCurve

   .. method:: DrawCurve(Point p1, Point p2, Point p3, float[] color: null, float[] fill: null, float width: 1, string dashes: null, int lineCap: 0, int lineJoin: 0, bool overlay: true, bool closePath: false, Morph morph: null, float strokeOpacity: 1, float fillOpacity: 1, int oc: 0)

      PDF only: This is a special case of *DrawBezier()*. See :meth:`Shape.DrawCurve`.


   .. index::
      pair: rect; DrawRect
      pair: closePath; DrawRect
      pair: color; DrawRect
      pair: dashes; DrawRect
      pair: fill; DrawRect
      pair: lineCap; DrawRect
      pair: lineJoin; DrawRect
      pair: morph; DrawRect
      pair: overlay; DrawRect
      pair: width; DrawRect
      pair: strokeOpacity; DrawRect
      pair: fillOpacity; DrawRect
      pair: radius; DrawRect
      pair: oc; DrawRect

   .. method:: DrawRect(Rect rect, float[] color: null, float[] fill: null, float width: 1, string dashes: null, int lineCap: 0, int lineJoin: 0, bool overlay: true, Morph morph: null, float strokeOpacity: 1, float fillOpacity: 1, float radius: 0, int oc: 0)

      PDF only: Draw a rectangle. See :meth:`Shape.DrawRect`.


   .. index::
      pair: closePath; DrawQuad
      pair: color; DrawQuad
      pair: dashes; DrawQuad
      pair: fill; DrawQuad
      pair: lineCap; DrawQuad
      pair: lineJoin; DrawQuad
      pair: morph; DrawQuad
      pair: overlay; DrawQuad
      pair: width; DrawQuad
      pair: strokeOpacity; DrawQuad
      pair: fillOpacity; DrawQuad
      pair: oc; DrawQuad

   .. method:: DrawQuad(Quad quad, float[] color: null, float[] fill: null, float width: 1, string dashes: null, float lineCap: 0, float lineJoin: 0, bool overlay: true, Morph morph: null, float strokeOpacity: 1, float fillOpacity: 1, int oc: 0)

      PDF only: Draw a quadrilateral. See :meth:`Shape.DrawQuad`.

   .. index::
      pair: encoding; InsertFont
      pair: fontBuffer; InsertFont
      pair: fontFile; InsertFont
      pair: fontName; InsertFont
      pair: setSimple; InsertFont

   .. method:: InsertFont(string fontName, string fontFile, byte[] fontBuffer: null, bool setSimple: false, int encoding: TEXT_ENCODING_LATIN)

      PDF only: Add a new font to be used by text output methods and return its :data:`xref`. If not already present in the file, the font definition will be added. Supported are the built-in :data:`Base14_Fonts` and the CJK fonts via **"reserved"** fontnames. Fonts can also be provided as a file path or a memory area containing the image of a font file.

      :arg string fontName: The name by which this font shall be referenced when outputting text on this page. In general, you have a "free" choice here (but consult the :ref:`AdobeManual`, page 16, section 7.3.5 for a formal description of building legal PDF names). However, if it matches one of the :data:`Base14_Fonts` or one of the CJK fonts, *fontFile* and *fontBuffer* **are ignored**.

        In other words, you cannot insert a font via *fontFile* / *fontBuffer* and also give it a reserved *fontName*.

        .. note:: A reserved fontName can be specified in any mixture of upper or lower case and still match the right built-in font definition: fontnames "helv", "Helv", "HELV", "Helvetica", etc. all lead to the same font definition "Helvetica". But from a :ref:`Page` perspective, these are **different references**. You can exploit this fact when using different *encoding* variants (Latin, Greek, Cyrillic) of the same font on a page.

      :arg string fontFile: a path to a font file. If used, *fontName* must be **different from all reserved names**.

      :arg byte[] fontBuffer: the memory image of a font file. If used, *fontName* must be **different from all reserved names**. This parameter would typically be used with :attr:`Font.Buffer` for fonts supported / available via :ref:`Font`.

      :arg int setSimple: applicable for *fontFile* / *fontBuffer* cases only: enforce treatment as a "simple" font, i.e. one that only uses character codes up to 255.

      :arg int encoding: applicable for the "Helvetica", "Courier" and "Times" sets of :data:`Base14_Fonts` only. Select one of the available encodings Latin (0), Cyrillic (2) or Greek (1). Only use the default (0 = Latin) for "Symbol" and "ZapfDingBats".

      :rytpe: int
      :returns: the :data:`xref` of the installed font.

      .. note:: Built-in fonts will not lead to the inclusion of a font file. So the resulting PDF file will remain small. However, your PDF viewer software is responsible for generating an appropriate appearance -- and there **exist** differences on whether or how each one of them does this. This is especially true for the CJK fonts. But also Symbol and ZapfDingbats are incorrectly handled in some cases. Following are the **Font Names** and their correspondingly installed **Base Font** names:

   .. index::
      pair: fileName; InsertImage
      pair: keepProportion; InsertImage
      pair: overlay; InsertImage
      pair: pixmap; InsertImage
      pair: rotate; InsertImage
      pair: stream; InsertImage
      pair: mask; InsertImage
      pair: oc; InsertImage
      pair: xref; InsertImage

   .. method:: InsertImage(rect, *, alpha=-1, filename=None, height=0, keepProportion=true, mask=None, oc=0, overlay=true, pixmap=None, rotate=0, stream=None, width=0, xref=0)

      PDF only: Put an image inside the given rectangle. The image may already
      exist in the PDF or be taken from a pixmap, a file, or a memory area.

      :arg Rect rect: where to put the image. Must be finite and not empty.
      :arg int alpha: deprecated and ignored.
      :arg string fileName:
        name of an image file (all formats supported by MuPDF -- see
        :ref:`ImageFiles`).
      :arg int height:
      :arg bool keepProportion:
        maintain the aspect ratio of the image.
      :arg byte[] mask:
        image in memory -- to be used as image mask (alpha values) for the base
        image. When specified, the base image must be provided as a filename or
        a stream -- and must not be an image that already has a mask.
      :arg int oc:
        (:data:`xref`) make image visibility dependent on this :data:`OCG`
        or :data:`OCMD`. Ignored after the first of multiple insertions. The
        property is stored with the generated PDF image object and therefore
        controls the image's visibility throughout the PDF.
      :arg overlay: see :ref:`CommonParms`.
      :arg pixmap: a pixmap containing the image.
      :arg int rotate: rotate the image.
        Must be an integer multiple of 90 degrees.
        Positive values rotate anti-clockwise.
        If you need a rotation by an arbitrary angle,
        consider converting the image to a PDF
        (:meth:`Document.Convert2Pdf`)
        first and then use :meth:`Page.ShowPdfPage` instead.
      :arg byte[] stream:
        image in memory (all formats supported by MuPDF -- see :ref:`ImageFiles`).
      :arg int width:
      :arg int xref:
        the :data:`xref` of an image already present in the PDF. If given,
        parameters `filename`, `pixmap`, `stream`, `alpha` and `mask` are
        ignored. The page will simply receive a reference to the existing
        image.

      :type pixmap: :ref:`Pixmap`
      
      :returns:
        The `xref` of the embedded image. This can be used as the `xref`
        argument for very significant performance boosts, if the image is
        inserted again.

      .. note::

         1.
           The method detects multiple insertions of the same image (like
           in the above example) and will store its data only on the first
           execution.  This is even true (although less performant), if using
           the default `xref=0`.
         2.
           The method cannot detect if the same image had already been part of
           the file before opening it.

         3.
           You can use this method to provide a background or foreground image
           for the page, like a copyright or a watermark. Please remember, that
           watermarks require a transparent image if put in foreground ...

         4.
           The image may be inserted uncompressed, e.g. if a `Pixmap` is used
           or if the image has an alpha channel. Therefore, consider using
           `deflate=true` when saving the file. In addition, there are ways to
           control the image size -- even if transparency comes into play. Have
           a look at :ref:`RecipesImages_O`.

         5.
           The image is stored in the PDF at its original quality level. This
           may be much better than what you need for your display. Consider
           **decreasing the image size** before insertion -- e.g. by using
           the pixmap option and then shrinking it or scaling it down (see
           :ref:`Pixmap` chapter). The PIL method `Image.thumbnail()` can
           also be used for that purpose. The file size savings can be very
           significant.

         6.
           Another efficient way to display the same image on multiple
           pages is another method: :meth:`ShowPdfPage`. Consult
           :meth:`Document.Convert2Pdf` for how to obtain intermediary PDFs
           usable for that method.

   
   .. index::
      pair: filename; ReplaceImage
      pair: pixmap; ReplaceImage
      pair: stream; ReplaceImage
      pair: xref; ReplaceImage

   .. method:: ReplaceImage(int xref, string filename: null, Pixmap pixmap: null, byte[] stream: null)

      Replace the image at xref with another one.

      :arg int xref: the :data:`xref` of the image.
      :arg filename: the filename of the new image.
      :arg pixmap: the :ref:`Pixmap` of the new image.
      :arg stream: the memory area containing the new image.

      Arguments `filename`, `pixmap`, `stream` have the same meaning as in :meth:`Page.InsertImage`, especially exactly one of these must be provided.

      This is a **global replacement:** the new image will also be shown wherever the old one has been displayed throughout the file.

      This method mainly exists for technical purposes. Typical uses include replacing large images by smaller versions, like a lower resolution, graylevel instead of colored, etc., or changing transparency.

   .. index::
      pair: pageNum; Recolor
      pair: colorSpaceName; Recolor 

   .. method:: Recolor(int colorNum)
   .. method:: Recolor(string colorSpaceName)
      
      Recolor specific page of PDF with specific color mode.

      :arg int colorNum: the number of colorspace, which means bytes of pixel. "GRAY" = `1`, "RGB" = `3`, "CMYK" = `4`.
      :arg string colorSpaceName: the name of the colorspace, "GRAY", "RGB", "CMYK".

   .. index::
      pair: xref; DeleteImage

   .. method:: DeleteImage(int xref)

      Delete the image at xref. This is slightly misleading: actually the image is being replaced with a small transparent :ref:`Pixmap` using above :meth:`Page.ReplaceImage`. The visible effect however is equivalent.

      :arg int xref: the :data:`xref` of the image.

      This is a **global replacement:** the image will disappear wherever the old one has been displayed throughout the file.
   
      If you inspect / extract a page's images by methods like :meth:`Page.GetImages`, :meth:`Page.GetImageInfo` or :meth:`Page.GetText`, the replacing "dummy" image will be detected like so `(45, 47, 1, 1, 8, 'DeviceGray', '', 'Im1', 'FlateDecode')` and also seem to "cover" the same boundary box on the page.
   
   .. index::
      pair: blocks; Page.GetText
      pair: dict; Page.GetText
      pair: clip; Page.GetText
      pair: flags; Page.GetText
      pair: html; Page.GetText
      pair: json; Page.GetText
      pair: rawdict; Page.GetText
      pair: text; Page.GetText
      pair: words; Page.GetText
      pair: xhtml; Page.GetText
      pair: xml; Page.GetText
      pair: textpage; Page.GetText
      pair: sort; Page.GetText
      pair: delimiters; Page.GetText

   .. method:: GetText(string option, Rect clip: null, int flags: 0, TextPage textpage: null, bool sort: false, char[] delimiters: null)

      Retrieves the content of a page in a variety of formats. This is a wrapper for multiple :ref:`TextPage` methods by choosing the output option `opt` as follows:

      * "text" -- :meth:`TextPage.extractTEXT`, default
      * "blocks" -- :meth:`TextPage.extractBLOCKS`
      * "words" -- :meth:`TextPage.extractWORDS`
      * "html" -- :meth:`TextPage.extractHTML`
      * "xhtml" -- :meth:`TextPage.extractXHTML`
      * "xml" -- :meth:`TextPage.extractXML`
      * "dict" -- :meth:`TextPage.extractDICT`
      * "json" -- :meth:`TextPage.extractJSON`
      * "rawdict" -- :meth:`TextPage.extractRAWDICT`
      * "rawjson" -- :meth:`TextPage.extractRAWJSON`

      :arg str opt: A string indicating the requested format, one of the above. A mixture of upper and lower case is supported.

        Values "words" and "blocks" are also accepted.

      :arg Rect clip: restrict extracted text to this rectangle. If None, the full page is taken. Has **no effect** for options "html", "xhtml" and "xml".

      :arg int flags: indicator bits to control whether to include images or how text should be handled with respect to white spaces and :data:`ligatures`. See :ref:`TextPreserve` for available indicators and :ref:`text_extraction_flags` for default settings. 

      :arg TextPage textpage: use a previously created :ref:`TextPage`. This reduces execution time **very significantly:** by more than 50% and up to 95%, depending on the extraction option. If specified, the 'flags' and 'clip' arguments are ignored, because they are textpage-only properties. If omitted, a new, temporary textpage will be created. 

      :arg bool sort: sort the output by vertical, then horizontal coordinates. In many cases, this should suffice to generate a "natural" reading order. Has no effect on (X)HTML and XML. Output option **"words"** sorts by `(y1, x0)` of the words' bboxes. Similar is true for "blocks", "dict", "json", "rawdict", "rawjson": they all are sorted by `(y1, x0)` of the resp. block bbox. If specified for "text", then internally "blocks" is used. 

      :arg string delimiters: use these characters as *additional* word separators with the "words" output option (ignored otherwise). By default, all white spaces (including non-breaking space `0xA0`) indicate start and end of a word. Now you can specify more characters causing this. For instance, the default will return `"john.doe@outlook.com"` as **one** word. If you specify `delimiters="@."` then the **four** words `"john"`, `"doe"`, `"outlook"`, `"com"` will be returned. Other possible uses include ignoring punctuation characters `delimiters=string.punctuation`. The "word" strings will not contain any delimiting character.
      
      :rtype: *string, list, dict*
      :returns: The page's content as a string, a list or a dictionary. Refer to the corresponding :ref:`TextPage` method for details.

      .. note::

        1. You can use this method as a **document conversion tool** from :ref:`any supported document type<Supported_File_Types>` to one of TEXT, HTML, XHTML or XML documents.
        2. The inclusion of text via the *clip* parameter is decided on a by-character level: a character becomes part of the output, if its bbox is contained in *clip*. This **deviates** from the algorithm used in redaction annotations: a character will be **removed if its bbox intersects** any redaction annotation.

   .. index::
      pair: rect; GetTextbox
      pair: textpage; GetTextbox

   .. method:: GetTextbox(Rect rect, TextPage textpage: null)

      Retrieve the text contained in a rectangle.

      :arg Rect rect: Rect.
      :arg textpage: a :ref:`TextPage` to use. If omitted, a new, temporary textpage will be created.

      :returns: a string with interspersed linebreaks where necessary. It is based on dedicated code. A tyical use is checking the result of :meth:`Page.SearchFor`:

        List<Rect> rl = page.SearchFor("currency:");
        page.GetTextbox(rl[0]);


   .. index::
      pair: flags; GetTextPage
      pair: clip; GetTextPage

   .. method:: GetTextPage(Rect clip: null, int flags: 3, Matrix matrix: null)

      Create a :ref:`TextPage` for the page.

      :arg int flags: indicator bits controlling the content available for subsequent text extractions and searches -- see the parameter of :meth:`Page.GetText`.

      :arg Rect clip: restrict extracted text to this area.

      :returns: :ref:`TextPage`


   .. index::
      pair: flags; GetTextPageOcr
      pair: language; GetTextPageOcr
      pair: dpi; GetTextPageOcr
      pair: full; GetTextPageOcr
      pair: tessdata; GetTextPageOcr

   .. method:: GetTextPageOcr(int flags: 3, string language: "eng", int dpi: 72, bool full: false, string tessdata: null)

      **Optical Character Recognition** (**OCR**) technology can be used to extract text data for documents where text is in a raster image format throughout the page. Use this method to **OCR** a page for text extraction.

      This method returns a :ref:`TextPage` for the page that includes OCRed text. MuPDF will invoke Tesseract-OCR if this method is used. Otherwise this is a normal :ref:`TextPage` object.

      :arg int flags: indicator bits controlling the content available for subsequent test extractions and searches -- see the parameter of :meth:`Page.GetText`.
      :arg string language: the expected language(s). Use "+"-separated values if multiple languages are expected, "eng+spa" for English and Spanish.
      :arg int dpi: the desired resolution in dots per inch. Influences recognition quality (and execution time).
      :arg bool full: whether to OCR the full page, or just the displayed images.
      :arg string tessdata: The name of Tesseract's language support folder `tessdata`. If omitted, this information must be present as environment variable `TESSDATA_PREFIX`.

      .. note:: This method does **not** support a clip parameter -- OCR will always happen for the complete page rectangle.

      :returns:
      
         a :ref:`TextPage`. Execution may be significantly longer than :meth:`Page.GetTextPage`.

         For a full page OCR, **all text** will have the font "GlyphlessFont" from Tesseract. In case of partial OCR, normal text will keep its properties, and only text coming from images will have the GlyphlessFont.

         .. note:: **OCRed text is only available** to text extractions and searches if their `textpage` parameter specifies the output of this method.


   .. method:: GetDrawings(bool extended: false)

      Return the vector graphics of the page. These are instructions which draw lines, rectangles, quadruples or curves, including properties like colors, transparency, line width and dashing, etc. Alternative terms are "line art" and "drawings".

      :returns: a list of PathInfo. Each dictionary item contains one or more single draw commands belonging together: they have the same properties (colors, dashing, etc.). This is called a **"path"** in PDF, so we adopted that name here, but the method **works for all document types**.

      The path dictionary for fill, stroke and fill-stroke paths has been designed to be compatible with class :ref:`Shape`. There are the following keys:

      ============== ============================================================================
      Key            Value
      ============== ============================================================================
      ClosePath      Same as the parameter in :ref:`Shape`.
      Color          Stroke color (see :ref:`Shape`).
      Dashes         Dashed line specification (see :ref:`Shape`).
      EvenOdd        Fill colors of area overlaps -- same as the parameter in :ref:`Shape`.
      Fill           Fill color  (see :ref:`Shape`).
      Items          List of draw commands: lines, rectangles, quads or curves.
      LineCap        Number 3-tuple, use its max value on output with :ref:`Shape`.
      LineJoin       Same as the parameter in :ref:`Shape`.
      FillOpacity    fill color transparency (see :ref:`Shape`).
      StrokeOpacity  stroke color transparency  (see :ref:`Shape`).
      Rect           Page area covered by this path. Information only.
      Layer          name of applicable Optional Content Group.
      Level          the hierarchy level if `extended=true`.
      SeqNo          command number when building page appearance.
      Type           type of this path.
      Width          Stroke line width. (see :ref:`Shape`).
      ============== ============================================================================

      Key `"opacity"` has been replaced by the new keys `"fillOpacity"` and `"strokeOpacity"`. This is now compatible with the corresponding parameters of :meth:`Shape.finish`.


      For paths other than groups or clips, key `"type"` takes one of the following values:

      * **"f"** -- this is a *fill-only* path. Only key-values relevant for this operation have a meaning, not applicable ones are present with a value of *null*: `"color"`, `"lineCap"`, `"lineJoin"`, `"width"`, `"closePath"`, `"dashes"` and should be ignored.
      * **"s"** -- this is a *stroke-only* path. Similar to previous, key `"fill"` is present with value *null*.
      * **"fs"** -- this is a path performing combined *fill* and *stroke* operations.

      Each item in `path["items"]` is one of the following:

      * `("l", p1, p2)` - a line from p1 to p2 (:ref:`Point` objects).
      * `("c", p1, p2, p3, p4)` - cubic Bézier curve **from p1 to p4** (p2 and p3 are the control points). All objects are of type :ref:`Point`.
      * `("re", rect, orientation)` - a :ref:`Rect`. Multiple rectangles within the same path are now detected. Integer `orientation` is 1 resp. -1 indicating whether the enclosed area is rotated left (1 = anti-clockwise), or resp. right [#f7]_.
      * `("qu", quad)` - a :ref:`Quad`. 3 or 4 consecutive lines are detected to actually represent a :ref:`Quad`.

      .. note::, quads and rectangles are more reliably recognized as such.

      Using class :ref:`Shape`, you should be able to recreate the original drawings on a separate (PDF) page with high fidelity under normal, not too sophisticated circumstances. A basic example can be found in :ref:`Extracting & Drawing vector graphics <The_Basics_Extracting_and_Drawing_Vector_Graphics>`.

      Specifying `extended=true` significantly alters the output. Most importantly, new dictionary types are present: "clip" and "group". All paths will now be organized in a hierarchic structure which is encoded by the new integer key "level", the hierarchy level. Each group or clip establishes a new hierarchy, which applies to all subsequent paths having a *larger* level value.

      Any path with a smaller level value than its predecessor will end the scope of (at least) the preceeding hierarchy level. A "clip" path with the same level as the preceding clip will end the scope of that clip. Same is true for groups. This is best explained by an example::

         +------+------+--------+------+--------+
         | line | lvl0 | lvl1   | lvl2 |  lvl3  |
         +------+------+--------+------+--------+
         |  0   | clip |        |      |        |
         |  1   |      | fill   |      |        |
         |  2   |      | group  |      |        |
         |  3   |      |        | clip |        |
         |  4   |      |        |      | stroke |
         |  5   |      |        | fill |        |  ends scope of clip in line 3
         |  6   |      | stroke |      |        |  ends scope of group in line 2
         |  7   |      | clip   |      |        |
         |  8   | fill |        |      |        |  ends scope of line 0
         +------+------+--------+------+--------+

      The clip in line 0 applies to line including line 7. Group in line 2 applies to lines 3 to 5, clip in line 3 only applies to line 4.

      "stroke" in line 4 is under control of "group" in line 2 and "clip" in line 3 (which in turn is a subset of line 0 clip).

      * **"clip"** dictionary. Its values (most importantly "scissor") remain valid / apply as long as following dictionaries have a **larger "level"** value.

        ============== ============================================================================
        Key            Value
        ============== ============================================================================
        closePath      Same as in "stroke" or "fill" dictionaries
        even_odd       Same as in "stroke" or "fill" dictionaries
        items          Same as in "stroke" or "fill" dictionaries
        rect           Same as in "stroke" or "fill" dictionaries
        layer          Same as in "stroke" or "fill" dictionaries
        level          Same as in "stroke" or "fill" dictionaries
        scissor        the clip rectangle
        type           "clip"
        ============== ============================================================================

      * "group" dictionary. Its values remain valid (apply) as long as following dictionaries have a **larger "level"** value. Any dictionary with an equal or lower level end this group.

        ============== ============================================================================
        Key            Value
        ============== ============================================================================
        rect           Same as in "stroke" or "fill" dictionaries
        layer          Same as in "stroke" or "fill" dictionaries
        level          Same as in "stroke" or "fill" dictionaries
        isolated       (bool) Whether this group is isolated
        knockout       (bool) Whether this is a "Knockout Group"
        blendmode      Name of the BlendMode, default is "Normal"
        opacity        Float value in range [0, 1].
        type           "group"
        ============== ============================================================================

      .. note:: The method is based on the output of :meth:`Page.GetCDrawings` -- which is much faster, but requires somewhat more attention processing its output.

   .. method:: GetFonts(bool full: false)

      PDF only: Return a list of fonts referenced by the page. Wrapper for :meth:`Document.GetPageFonts`.


   .. method:: GetImages(bool full: false)

      PDF only: Return a list of images referenced by the page. Wrapper for :meth:`Document.GetPageImages`.


   .. index::
      pair: hashes; GetImageInfo
      pair: xrefs; GetImageInfo

   .. method:: GetImageInfo(bool hashes: false, bool xrefs: false)

      Return a list of meta information `Block` for all images shown on the page. This works for all document types. Technically, this is a subset of the dictionary output of :meth:`Page.GetText`: the image binary content and any text on the page are ignored.

      :arg bool hashes: Compute the MD5 hashcode for each encountered image, which allows identifying image duplicates. This adds the key `"digest"` to the output, whose value is a 16 byte `bytes` object.

      :arg bool xrefs: **PDF only.** Try to find the :data:`xref` for each image. Implies `hashes=true`. Adds the `"xref"` key to the dictionary. If not found, the value is 0, which means, the image is either "inline" or otherwise undetectable. Please note that this option has an extended response time, because the MD5 hashcode will be computed at least two times for each image with an xref.

      :rtype: list of ImageInfo
      :returns: A list of ImageInfo. This includes information for **exactly those** images, that are shown on the page -- including *"inline images"*. In contrast to images included in :meth:`Page.GetText`, image **binary content** is not loaded, which drastically reduces memory usage. The dictionary layout is similar to that of image blocks in `page.GetText("dict")`.

         =============== ===============================================================
         **Key**             **Value**
         =============== ===============================================================
         number          block number *(int)*
         bbox            image bbox on page, :data:`Rect`
         width           original image width *(int)*
         height          original image height *(int)*
         cs-name         colorspace name *(str)*
         colorspace      colorspace.n *(int)*
         xres            resolution in x-direction *(int)*
         yres            resolution in y-direction *(int)*
         bpc             bits per component *(int)*
         size            storage occupied by image *(int)*
         digest          MD5 hashcode *(bytes)*, if *hashes* is true
         xref            image :data:`xref` or 0, if *xrefs* is true
         transform       matrix transforming image rect to bbox, :data:`matrix_like`
         =============== ===============================================================

         Multiple occurrences of the same image are always reported. You can detect duplicates by comparing their `digest` values.


   .. method:: GetXObjects()

      PDF only: Return a list of Form XObjects referenced by the page. Wrapper for :meth:`Document.GetPageXObjects`.


   .. index::
      pair: transform; GetImageRects

   .. method:: GetImageRects(string name, bool transform: false)

      PDF only: Return boundary boxes and transformation matrices of an embedded image. This is an improved version of :meth:`Page.GetImageBbox` with the following differences:

      * There is no restriction on **how** the image is invoked (by the page or one of its Form XObjects). The result is always complete and correct.
      * The result is a list of :ref:`Rect` or (:ref:`Rect`, :ref:`Matrix`) objects -- depending on *transform*. Each list item represents one location of the image on the page. Multiple occurrences might not be detectable by :meth:`Page.GetImageBbox`.
      * The method invokes :meth:`Page.GetImageInfo` with `xrefs=true` and therefore has a noticeably longer response time than :meth:`Page.GetImageBbox`.

      :arg string item: an item of the list :meth:`Page.GetImages`, or the reference **name** entry of such an item (item[7]), or the image :data:`xref`.
      :arg bool transform: also return the matrix used to transform the image rectangle to the bbox on the page. If true, then tuples `(bbox, matrix)` are returned.

      :rtype: List
      :returns: Boundary boxes and respective transformation matrices for each image occurrence on the page. If the item is not on the page, an empty list `[]` is returned.


   .. index::
      pair: transform; GetImageBbox

   .. method:: GetImageBbox(string name, bool transform: false)

      PDF only: Return boundary box and transformation matrix of an embedded image.

      :arg string item: an item of the list :meth:`Page.GetImages` with *full=true* specified, or the reference **name** entry of such an item, which is item[-3] (or item[7] respectively).
      :arg bool transform: return the matrix used to transform the image rectangle to the bbox on the page. Default is just the bbox. If true, then a tuple `(bbox, matrix)` is returned.

      :rtype: :ref:`Rect` or (:ref:`Rect`, :ref:`Matrix`)
      :returns: the boundary box of the image -- optionally also its transformation matrix.


      .. note::

         1. Be aware that :meth:`Page.GetImages` may contain "dead" entries i.e. images, which the page **does not display**. This is no error, but intended by the PDF creator. No exception will be raised in this case, but an infinite rectangle is returned. You can avoid this from happening by executing :meth:`Page.CleanContents` before this method.
         2. The image's "transformation matrix" is defined as the matrix, for which the expression `bbox / transform == Rect(0, 0, 1, 1)` is true, lookup details here: :ref:`ImageTransformation`.


   .. index::
      pair: matrix; GetSvgImage

   .. method:: GetSvgImage(Matrix matrix: Identity, int textAsPath: 1)

     Create an SVG image from the page. Only full page images are currently supported.

     :arg Matrix matrix: a matrix, default is :ref:`Identity`.
     :arg bool textAsPath: -- controls how text is represented. *true* outputs each character as a series of elementary draw commands, which leads to a more precise text display in browsers, but a **very much larger** output for text-oriented pages. Display quality for *false* relies on the presence of the referenced fonts on the current system. For missing fonts, the internet browser will fall back to some default -- leading to unpleasant appearances. Choose *false* if you want to parse the text of the SVG.

     :returns: a UTF-8 encoded string that contains the image. Because SVG has XML syntax it can be saved in a text file, the standard extension is `.svg`.

         .. note:: In case of a PDF, you can circumvent the "full page image only" restriction by modifying the page's CropBox before using the method.

   .. index::
      pair: alpha; GetPixmap
      pair: annots; GetPixmap
      pair: clip; GetPixmap
      pair: colorSpace; GetPixmap
      pair: matrix; GetPixmap
      pair: dpi; GetPixmap

   .. method:: GetPixmap(Matrix matrix: Identity, int dpi: 0, string colorSpace: "RGB", Rect clip: null, bool alpha: false, bool annots: true)

     Create a pixmap from the page. This is probably the most often used method to create a :ref:`Pixmap`.

     All parameters are *keyword-only.*

     :arg Matrix matrix: default is :ref:`Identity`.
     :arg int dpi: desired resolution in x and y direction. If not `null`, the `"matrix"` parameter is ignored.
     :arg colorspace: The desired colorspace, one of "GRAY", "RGB" or "CMYK" (case insensitive). Or specify a :ref:`ColorSpace`, ie. one of the predefined ones: :data:`csGRAY`, :data:`csRGB` or :data:`csCMYK`.
     :type colorspace: string or :ref:`ColorSpace`
     :arg IRect clip: restrict rendering to the intersection of this area with the page's rectangle.
     :arg bool alpha: whether to add an alpha channel. Always accept the default *false* if you do not really need transparency. This will save a lot of memory (25% in case of RGB ... and pixmaps are typically **large**!), and also processing time. Also note an **important difference** in how the image will be rendered: with *true* the pixmap's samples area will be pre-cleared with *0x00*. This results in **transparent** areas where the page is empty. With *false* the pixmap's samples will be pre-cleared with *0xff*. This results in **white** where the page has nothing to show.


     :arg bool annots: whether to also render annotations or to suppress them. You can create pixmaps for annotations separately.

     :rtype: :ref:`Pixmap`
     :returns: Pixmap of the page. For fine-controlling the generated image, the by far most important parameter is **matrix**. E.g. you can increase or decrease the image resolution by using **Matrix(xzoom, yzoom)**. If zoom > 1, you will get a higher resolution: zoom=2 will double the number of pixels in that direction and thus generate a 2 times larger image. Non-positive values will flip horizontally, resp. vertically. Similarly, matrices also let you rotate or shear, and you can combine effects via e.g. matrix multiplication. See the :ref:`Matrix` section to learn more.

     .. note::

         * The pixmap will have *"premultiplied"* pixels if `alpha: true`. To learn about some background, e.g. look for "Premultiplied alpha" `here <https://en.wikipedia.org/wiki/Glossary_of_computer_graphics#P>`_.



   .. method:: GetAnnotNames()

      PDF only: return a list of the names of annotations, widgets and links. Technically, these are the */NM* values of every PDF object found in the page's */Annots*  array.

      :rtype: list of string

   .. method:: GetAnnotXrefs()

      PDF only: return a list of the :data`xref` numbers of annotations, widgets and links -- technically of all entries found in the page's */Annots*  array.

      :rtype: list of AnnotXref
      :returns: a list of items *(xref, type)* where type is the annotation type. Use the type to tell apart links, fields and annotations, see :ref:`AnnotationTypes`.

   .. method:: LoadAnnot(int xref)

      PDF only: return the annotation identified by *xref*. This may be its unique name (PDF `/NM` key), or its :data:`xref`.

      :arg int xref: the annotation name or xref.

      :rtype: :ref:`Annot`
      :returns: the annotation or *null*.

      .. note:: Methods :meth:`Page.GetAnnotNames`, :meth:`Page.GetAnnotXrefs` provide lists of names or xrefs, respectively, from where an item may be picked and loaded via this method.

   .. method:: LoadWidget(int xref)

      PDF only: return the field identified by *xref*.

      :arg int xref: the field's xref.

      :rtype: :ref:`Widget`
      :returns: the field or *null*.

      .. note:: This is similar to the analogous method :meth:`Page.LoadAnnot` -- except that here only the xref is supported as identifier.


   .. method:: LoadLinks()

      Return the first link on a page. Synonym of property :attr:`FirstLink`.

      :rtype: :ref:`Link`
      :returns: first link on the page (or *null*).

   .. index::
      pair: rotate; SetRotation

   .. method:: SetRotation(int rotate)

      PDF only: Set the rotation of the page.

      :arg int rotate: An integer specifying the required rotation in degrees. Must be an integer multiple of 90. Values will be converted to one of 0, 90, 180, 270.

   .. method:: RemoveRotation()

      PDF only: Set page rotation to 0 while maintaining appearance and page content.

      :returns: The inverted matrix used to achieve this change. If the page was not rotated (rotation 0), :ref:`Identity` is returned. The method automatically recomputes the rectangles of any annotations, links and widgets present on the page.

         This method may come in handy when e.g. used with :meth:`Page.ShowPdfPage`.

   .. index::
      pair: clip; ShowPdfPage
      pair: keepProportion; ShowPdfPage
      pair: overlay; ShowPdfPage
      pair: rotate; ShowPdfPage

   .. method:: ShowPdfPage(Rect rect, Document docsrc, int pno: 0, bool keepProportion: true, bool overlay: true, int oc: 0, int rotate: 0, Rect clip: null)

      PDF only: Display a page of another PDF as a **vector image** (otherwise similar to :meth:`Page.InsertImage`). This is a multi-purpose method. For example, you can use it to

      :arg Rect rect: where to place the image on current page. Must be finite and its intersection with the page must not be empty.
      :arg docsrc: source PDF document containing the page. Must be a different document object, but may be the same file.
      :type docsrc: :ref:`Document`

      :arg int pno: page number (0-based, in `-∞ < pno < docsrc.PageCount`) to be shown.

      :arg bool keepProportion: whether to maintain the width-height-ratio (default). If false, all 4 corners are always positioned on the border of the target rectangle -- whatever the rotation value. In general, this will deliver distorted and /or non-rectangular images.

      :arg bool overlay: put image in foreground (default) or background.

      :arg int oc: (:data:`xref`) make visibility dependent on this :data:`OCG` / :data:`OCMD` (which must be defined in the target PDF) [#f9]_.
      :arg float rotate: show the source rectangle rotated by some angle. Any angle is supported.

      :arg Rect clip: choose which part of the source page to show. Default is the full page, else must be finite and its intersection with the source page must not be empty.

      .. note:: In contrast to method :meth:`Document.InsertPdf`, this method does not copy annotations, widgets or links, so these are not included in the target [#f6]_. But all its **other resources (text, images, fonts, etc.)** will be imported into the current PDF. They will therefore appear in text extractions and in :meth:`GetFonts` and :meth:`GetImages` lists -- even if they are not contained in the visible area given by *clip*.

      Example: Show the same source page, rotated by 90 and by -90 degrees:

      .. code-block:: cs

         Document doc = new Document()  // new empty PDF
         Page page=doc.NewPage()  // new page in A4 format

         // upper half page
         Rect r1 = new Rect(0, 0, page.rect.width, page.rect.height/2)

         // lower half page
         Rect r2 = r1 + new Rect(0, page.rect.height/2, 0, page.rect.height/2)

         Document src = new Document("MuPDF.pdf")  // show page 0 of this

         page.ShowPdfPage(r1, src, 0, rotate=90)
         page.ShowPdfPage(r2, src, 0, rotate=-90)
         doc.Save("show.pdf")

      .. image:: ../images/img-showpdfpage.*
         :scale: 70


   .. method:: NewShape()

      PDF only: Create a new :ref:`Shape` object for the page.

      :rtype: :ref:`Shape`
      :returns: a new :ref:`Shape` to use for compound drawings. See description there.

   .. method:: ReadBarcodes(Rect clip=null, bool tryHarder = true, bool tryInverted = false, bool pureBarcode = false, bool multi = true, bool autoRotate = true)

      Retrieves any barcode information contained within the supplied rectangle.

      :arg Rect clip: The area to scan for barcodes. If `null` the whole page will be scanned.
      :arg bool tryHarder: Spend more time to try to find a barcode; optimize for accuracy, not speed.
      :arg bool tryInverted: Try to decode as inverted image.
      :arg bool pureBarcode: Image is a pure monochrome image of a barcode.
      :arg bool multi: Try to read multi barcodes on page.
      :arg bool autoRotate: Indicate whether the image should be automatically rotated. Rotation is supported for 90, 180 and 270 degrees.

      :rtype: `List<Barcode>`
      :returns: a list of :doc:`Barcode` objects.


   .. method:: WriteBarcode(Rect clip, string text, BarcodeFormat barcodeFormat, string characterSet = null, bool disableEci = false)

      Creates a barcode at the supplied rectangle `clip` on the page.

      :arg Rect clip: The area to create the barcode on the page.
      :arg string text: Contents to write.
      :arg BarcodeFormat barcodeFormat: Format to encode; For supported formats see :ref:`BarcodeFormat <Barcode_BarcodeFormat>`.
      :arg string characterSet: Use a specific character set for binary encoding (if supported by the selected barcode format).
      :arg bool disableEci: Don't generate ECI segment if non-default character set is used.



   .. method:: SetContents(int xref)

      PDF only: Let the page's `/Contents` key point to this xref. Any previously used contents objects will be ignored and can be removed via garbage collection.

   .. method:: WrapContents()

      Ensures that the page's so-called graphics state is balanced and new content can be inserted correctly.

      This method obsoletes the use of :meth:`Page.CleanContents` in most cases.      
      The advantage this method is a small footprint in terms of processing time and a low impact on the data size of incremental saves.

   .. index::
      pair: flags; SearchFor
      pair: quads; SearchFor
      pair: clip; SearchFor
      pair: textpage; SearchFor

   .. method:: SearchFor(string needle, Rect clip: null, bool quads: false, int flags: TEXT_DEHYPHENATE | TEXT_PRESERVE_WHITESPACE | TEXT_PRESERVE_LIGATURES | TEXT_MEDIABOX_CLIP, TextPage textpage: None)

      Search for *needle* on a page. Wrapper for :meth:`TextPage.Search`.

      :arg string needle: Text to search for. May contain spaces. Upper / lower case is ignored, but only works for ASCII characters: For example, "COMPÉTENCES" will not be found if needle is "compétences" -- "compÉtences" however will. Similar is true for German umlauts and the like.
      :arg Rect clip: only search within this area.
      :arg bool quads: Return object type :ref:`Quad` instead of :ref:`Rect`.
      :arg int flags: Control the data extracted by the underlying :ref:`TextPage`. By default, ligatures and white spaces are kept, and hyphenation [#f8]_ is detected.
      :arg textpage: use a previously created :ref:`TextPage`. This reduces execution time **significantly.** If specified, the 'flags' and 'clip' arguments are ignored. If omitted, a temporary textpage will be created.

      :rtype: list

      :returns:

        A list of :ref:`Rect` or  :ref:`Quad` objects, each of which  -- **normally!** -- surrounds one occurrence of *needle*. **However:** if parts of *needle* occur on more than one line, then a separate item is generated for each these parts. So, if `needle = "search string"`, two rectangles may be generated.


      .. note:: The method supports multi-line text marker annotations: you can use the full returned list as **one single** parameter for creating the annotation.

      .. caution::

         * There is a tricky aspect: the search logic regards **contiguous multiple occurrences** of *needle* as one: assuming *needle* is "abc", and the page contains "abc" and "abcabc", then only **two** rectangles will be returned, one for "abc", and a second one for "abcabc".
         * You can always use :meth:`Page.GetTextbox` to check what text actually is being surrounded by each rectangle.


   .. method:: SetMediaBox(r)

      PDF only: Change the physical page dimension by setting :data:`mediabox` in the page's object definition.

      :arg Rect r: the new :data:`mediabox` value.

      .. note:: This method also removes the page's other (optional) rectangles (:data:`cropbox`, ArtBox, TrimBox and Bleedbox) to prevent inconsistent situations. This will cause those to assume their default values.

      .. caution:: For non-empty pages this may have undesired effects, because the location of all content depends on this value and will therefore change position or even disappear.


   .. method:: SetCropBox(Rect r)

      PDF only: change the visible part of the page.

      :arg Rect r: the new visible area of the page. Note that this **must** be specified in **unrotated coordinates**, not empty, nor infinite and be completely contained in the :attr:`Page.mediabox`.

      After execution **(if the page is not rotated)**, :attr:`Page.rect` will equal this rectangle, but be shifted to the top-left position (0, 0) if necessary.

      
   .. method:: SetArtBox(Rect r)

      PDF only: sets the art box for the page.

      :arg Rect r:


   .. method:: SetBleedBox(Rect r)

      PDF only: sets the bleed box for the page.

      :arg Rect r:

   .. method:: SetTrimBox(Rect r)

      PDF only: sets the trim box for the page.

      :arg Rect r:


   .. attribute:: Rotation

      Contains the rotation of the page in degrees (always 0 for non-PDF types). This is a copy of the value in the PDF file. The PDF documentation says:
      
         *"The number of degrees by which the page should be rotated clockwise when displayed or printed. The value must be a multiple of 90. Default value: 0."*

         In MuPDF.NET, we make sure that this attribute is always one of 0, 90, 180 or 270.

      :type: int

   .. attribute:: CropBoxPosition

      Contains the top-left point of the page's `/CropBox` for a PDF, otherwise *Point(0, 0)*.

      :type: :ref:`Point`

   .. attribute:: CropBox

      The page's `/CropBox` for a PDF. Always the **unrotated** page rectangle is returned. For a non-PDF this will always equal the page rectangle.

      .. note:: In PDF, the relationship between `/MediaBox`, `/CropBox` and page rectangle may sometimes be confusing, please do lookup the glossary for :data:`MediaBox`.

      :type: :ref:`Rect`

   .. attribute:: ArtBox

   .. attribute:: BleedBox

   .. attribute:: TrimBox

      The page's `/ArtBox`, `/BleedBox`, `/TrimBox`, respectively. If not provided, defaulting to :attr:`Page.CropBox`.

      :type: :ref:`Rect`

   .. attribute:: MediaBoxSize

      Contains the width and height of the page's :attr:`Page.MediaBox` for a PDF, otherwise the bottom-right coordinates of :attr:`Page.Rect`.

      :type: :ref:`Point`

   .. attribute:: MediaBox

      The page's :data:`mediabox` for a PDF, otherwise :attr:`Page.Rect`.

      :type: :ref:`Rect`

      .. note:: For most PDF documents and for **all other document types**, `page.Rect == page.CropBox == page.MediaBox` is true. However, for some PDFs the visible page is a true subset of :data:`mediabox`. Also, if the page is rotated, its `Page.rect` may not equal `Page.CropBox`. In these cases the above attributes help to correctly locate page elements.

   .. attribute:: TransformationMatrix

      This matrix translates coordinates from the PDF space to the MuPDF space. For example, in PDF `/Rect [x0 y0 x1 y1]` the pair (x0, y0) specifies the **bottom-left** point of the rectangle -- in contrast to MuPDF's system, where (x0, y0) specify top-left. Multiplying the PDF coordinates with this matrix will deliver the MuPDF rectangle version. Obviously, the inverse matrix will again yield the PDF rectangle.

      :type: :ref:`Matrix`

   .. attribute:: RotationMatrix

   .. attribute:: DerotationMatrix

      These matrices may be used for dealing with rotated PDF pages. When adding / inserting anything to a PDF page, the coordinates of the **unrotated** page are always used. These matrices help translating between the two states. Example: if a page is rotated by 90 degrees -- what would then be the coordinates of the top-left Point(0, 0) of an A4 page?

         page.SetRotation(90)  # rotate an ISO A4 page
         page.Rect
         
         Point p = new Point(0, 0)  # where did top-left point land?
         p * page.RotationMatrix
         

      :type: :ref:`Matrix`

   .. attribute:: FirstLink

      Contains the first :ref:`Link` of a page (or *null*).

      :type: :ref:`Link`

   .. attribute:: FirstAnnot

      Contains the first :ref:`Annot` of a page (or *null*).

      :type: :ref:`Annot`

   .. attribute:: FirstWidget

      Contains the first :ref:`Widget` of a page (or *null*).

      :type: :ref:`Widget`

   .. attribute:: Number

      The page number.

      :type: int

   .. attribute:: Parent

      The owning document object.

      :type: :ref:`Document`


   .. attribute:: Rect

      Contains the rectangle of the page. Same as result of :meth:`Page.GetBound`.

      :type: :ref:`Rect`

   .. attribute:: Xref

      The page's PDF :data:`xref`. Zero if not a PDF.

      :type: :ref:`Rect`
   
   .. attribute:: IsWrapped

      Check whether contents wrapping is present

      :type: `bool`

-----

.. _link_dict_description:

Description of *GetLinks()* Entries
----------------------------------------
Each entry of the :meth:`Page.GetLinks` list is a dictionary with the following keys:

* *kind*:  (required) an integer indicating the kind of link. This is one of *LINK_NONE*, *LINK_GOTO*, *LINK_GOTOR*, *LINK_LAUNCH*, or *LINK_URI*. For values and meaning of these names refer to :ref:`linkDest Kinds`.

* *from*:  (required) a :ref:`Rect` describing the "hot spot" location on the page's visible representation (where the cursor changes to a hand image, usually).

* *page*:  a 0-based integer indicating the destination page. Required for *LINK_GOTO* and *LINK_GOTOR*, else ignored.

* *to*:   either a *MuPDFPoint*, specifying the destination location on the provided page, default is *MuPDFPoint(0, 0)*, or a symbolic (indirect) name. If an indirect name is specified, *page = -1* is required and the name must be defined in the PDF in order for this to work. Required for *LINK_GOTO* and *LINK_GOTOR*, else ignored.

* *file*: a string specifying the destination file. Required for *LINK_GOTOR* and *LINK_LAUNCH*, else ignored.

* *uri*:  a string specifying the destination internet resource. Required for *LINK_URI*, else ignored. You should make sure to start this string with an unambiguous substring, that classifies the subtype of the URL, like `"http://"`, `"https://"`, `"file://"`, `"ftp://"`, `"mailto:"`, etc. Otherwise your browser will try to interpret the text and come to unwanted / unexpected conclusions about the intended URL type.

* *xref*: an integer specifying the PDF :data:`xref` of the link object. Do not change this entry in any way. Required for link deletion and update, otherwise ignored. For non-PDF documents, this entry contains *-1*. It is also *-1* for **all** entries in the *GetLinks()* list, if **any** of the links is not supported by MuPDF - see :ref:`notes_on_supporting_links`.

.. _notes_on_supporting_links:

Notes on Supporting Links
---------------------------

If MuPDF detects a link to another file, it will supply either a *LINK_GOTOR* or a *LINK_LAUNCH* link kind. In case of *LINK_GOTOR* destination details may either be given as page number (eventually including position information), or as an indirect destination.

If an indirect destination is given, then this is indicated by *page = -1*, and *link.dest.dest* will contain this name. The dictionaries in the *GetLinks()* list will contain this information as the *to* value.

**Internal links are always** of kind *LINK_GOTO*. If an internal link specifies an indirect destination, it **will always be resolved** and the resulting direct destination will be returned. Names are **never returned for internal links**, and undefined destinations will cause the link to be ignored.

Writing
~~~~~~~~~

MuPDF.NET writes (updates, inserts) links by constructing and writing the appropriate PDF object **source**. This makes it possible to specify indirect destinations for *LINK_GOTOR* **and** *LINK_GOTO* link kinds (pre *PDF 1.2* file formats are **not supported**).

.. warning:: If a *LINK_GOTO* indirect destination specifies an undefined name, this link can later on not be found / read again with MuPDF / MuPDF.NET. Other readers however **will** detect it, but flag it as erroneous.

Indirect *LINK_GOTOR* destinations can in general of course not be checked for validity and are therefore **always accepted**.

**Example: How to insert a link pointing to another page in the same document**

1. Determine the rectangle on the current page, where the link should be placed. This may be the bbox of an image or some text.

2. Determine the target page number ("pno", 0-based) and a :ref:`Point` on it, where the link should be directed to.

3. Create a dictionary `d = {"kind": LINK_GOTO, "page": pno, "from": bbox, "to": point}`.

4. Execute `page.InsertLink(d)`.


Homologous Methods of :ref:`Document` and :ref:`Page`
--------------------------------------------------------
This is an overview of homologous methods on the :ref:`Document` and on the :ref:`Page` level.

========================================= =======================
**Document Level**                        **Page Level**
========================================= =======================
*Document.GetPageFonts(pno)*              :meth:`Page.GetFonts`
*Document.GetPageImages(pno)*             :meth:`Page.GetImages`
*Document.GetPagePixmap(pno, ...)*        :meth:`Page.GetPixmap`
*Document.GetPageText(pno, ...)*          :meth:`Page.GetText`
*Document.SearchPageFor(pno, ...)*        :meth:`Page.SearchFor`
========================================= =======================

The page number "pno" is a 0-based integer `-∞ < pno < PageCount`.

.. note::

   Most document methods (left column) exist for convenience reasons, and are just wrappers for: *Document[pno].<page method>*. So they **load and discard the page** on each execution.

   However, the first two methods work differently. They only need a page's object definition statement - the page itself will **not** be loaded. So e.g. :meth:`Page.GetFonts` is a wrapper the other way round and defined as follows: *page.GetFonts == page.parent.GetPageFonts(page.number)*.


Output Structures
--------------------------------

Box Structure
~~~~~~~~~~~~~~

=============== ================================================
**Key**         **Value**
=============== ================================================
Rect            boundary box, :ref:`Rect`
Matrix          respective transformation matricx, :ref:`Matrix`
=============== ================================================

BoxLog Structure
~~~~~~~~~~~~~~~~

=============== ===================================
**Key**         **Value**
=============== ===================================
Type            a type of rectangle, *string*
Box             rectangle coordinates, :ref:`Rect`
LayerName       optional. layer name, *string*
=============== ===================================

LinkInfo Structure
~~~~~~~~~~~~~~~~~~

========= ====================================================================================================
**Key**         **Value**
========= ====================================================================================================
Kind      (*required*) an integer indicating the kind of link.
From      (*required*) a :ref:`Rect` describing the "hot spot" location on the page's visible representation.
To        either a point, specifiying the destination location on the provided page, default is (0, 0). 
Page      *int* indicate page number
Name      
Uri       a string specifying the destination internet resource.
Zoom      if flag is `LINK_FLAG_R_IS_ZOOM`, indicate *float* zoom value.
File      a string specifying the destination file.
Id
Xref      :data:`xref` of the item (0 if no PDF).
Italic    true if italic item text, or omitted. PDF only.
Bold      true if bold item text or omitted. PDF only.
Collapse  true if sub-items are folded, or omitted in toc. PDF only.
Color     item color in PDF RGB format `(red, green, blue)`, or omitted (always omitted if no PDF).
========= ====================================================================================================


.. rubric:: Footnotes

.. [#f2] Not all PDF reader software (including internet browsers and office software) display all of these fonts. And if they do, the difference between the **serifed** and the **non-serifed** version may hardly be noticeable. But serifed and non-serifed versions lead to different installed base fonts, thus providing an option to be displayable with your specific PDF viewer.

.. [#f3] Not all PDF readers display these fonts at all. Some others do, but use a wrong character spacing, etc.

.. [#f4] You are generally free to choose any of the :ref:`mupdficons` you consider adequate.

.. [#f5] The previous algorithm caused images to be **shrunk** to this intersection. Now the image can be anywhere on :attr:`Page.MediaBox`, potentially being invisible or only partially visible if the cropbox (representing the visible page part) is smaller.

.. [#f6] If you need to also see annotations or fields in the target page, you can convert the source PDF using :meth:`Document.Bake`. The underlying MuPDF function of that method will convert these objects to normal page content. Then use :meth:`Page.ShowPdfPage` with the converted PDF page.

.. [#f7] In PDF, an area enclosed by some lines or curves can have a property called "orientation". This is significant for switching on or off the fill color of that area when there exist multiple area overlaps - see discussion in method :meth:`Shape.Finish` using the "non-zero winding number" rule. While orientation of curves, quads, triangles and other shapes enclosed by lines always was detectable, this has been impossible for "re" (rectangle) items in the past. Adding the orientation parameter now delivers the missing information.

.. [#f8] Hyphenation detection simply means that if the last character of a line is "-", it will be assumed to be a continuation character. That character will not be found by text searching with its default flag setting. Please take note, that a MuPDF *line* may not always be what you expect: words separated by overly large gaps (e.g. caused by text justification) may constitute separate MuPDF lines. If then any of these words ends with a hyphen, it will only be found by text searching if hyphenation is switched off.

.. [#f9] Objects inside the source page, like images, text or drawings, are never aware of whether their owning page now is under OC control inside the target PDF. If source page objects are OC-controlled in the source PDF, then this will not be retained on the target: they will become unconditionally visible.

.. include:: ../footer.rst
