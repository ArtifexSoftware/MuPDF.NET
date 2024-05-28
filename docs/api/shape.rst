.. include:: ../header.rst

.. _Shape:

Shape
================

|pdf_only_class|

This class allows creating interconnected graphical elements on a PDF page. Its methods have the same meaning and name as the corresponding :ref:`Page` methods.

In fact, each :ref:`Page` draw method is just a convenience wrapper for (1) one shape draw method, (2) the :meth:`Shape.Finish` method, and (3) the :meth:`Shape.Commit` method. For page text insertion, only the :meth:`Shape.Commit` method is invoked. If many draw and text operations are executed for a page, you should always consider using a Shape object.

Several draw methods can be executed in a row and each one of them will contribute to one drawing. Once the drawing is complete, the :meth:`Shape.Finish` method must be invoked to apply color, dashing, width, morphing and other attributes.

**Draw** methods of this class (and :meth:`Shape.InsertTextbox`) are logging the area they are covering in a rectangle (:attr:`Shape.rect`). This property can for instance be used to set :attr:`Page.cropbox_position`.

**Text insertions** :meth:`Shape.InsertText` and :meth:`Shape.InsertTextbox` implicitly execute a "finish" and therefore only require :meth:`Shape.Commit` to become effective. As a consequence, both include parameters for controlling properties like colors, etc.

================================ =====================================================
**Method / Attribute**             **Description**
================================ =====================================================
:meth:`Shape.Commit`             update the page's contents
:meth:`Shape.DrawBezier`         draw a cubic Bezier curve
:meth:`Shape.DrawCircle`         draw a circle around a point
:meth:`Shape.DrawCurve`          draw a cubic Bezier using one helper point
:meth:`Shape.DrawLine`           draw a line
:meth:`Shape.DrawOval`           draw an ellipse
:meth:`Shape.DrawPolyline`       connect a sequence of points
:meth:`Shape.DrawQuad`           draw a quadrilateral
:meth:`Shape.DrawRect`           draw a rectangle
:meth:`Shape.DrawSector`         draw a circular sector or piece of pie
:meth:`Shape.DrawSquiggle`       draw a squiggly line
:meth:`Shape.DrawZigzag`         draw a zigzag line
:meth:`Shape.Finish`             finish a set of draw commands
:meth:`Shape.InsertText`         insert text lines
:meth:`Shape.InsertTextbox`      fit text into a rectangle
:attr:`Shape.Doc`                stores the page's document
:attr:`Shape.DrawCont`           draw commands since last :meth:`Shape.Finish`
:attr:`Shape.Height`             stores the page's height
:attr:`Shape.LastPoint`          stores the current point
:attr:`Shape.Page`               stores the owning page
:attr:`Shape.Rect`               rectangle surrounding drawings
:attr:`Shape.TextCont`           accumulated text insertions
:attr:`Shape.TotalCont`          accumulated string to be stored in :data:`contents`
:attr:`Shape.Width`              stores the page's width
================================ =====================================================

**Class API**

.. class:: Shape

   .. method:: Shape(Page page)

      Create a new drawing. During importing PyMuPDF, the *fitz.Page* object is being given the convenience method *new_shape()* to construct a *Shape* object. During instantiation, a check will be made whether we do have a PDF page. An exception is otherwise raised.

      :arg page: an existing page of a PDF document.
      :type page: :ref:`Page`

   .. method:: DrawLine(Point p1, Point p2)

      Draw a line from :data:`Point` objects *p1* to *p2*.

      :arg Point p1: starting point

      :arg Point p2: end point

      :rtype: :ref:`Point`
      :returns: the end point, *p2*.

   .. index::
      pair: breadth; DrawSquiggle

   .. method:: DrawSquiggle(Point p1, Point p2, float breadth=2)

      Draw a squiggly (wavy, undulated) line from :data:`point_like` objects *p1* to *p2*. An integer number of full wave periods will always be drawn, one period having a length of *4 * breadth*. The breadth parameter will be adjusted as necessary to meet this condition. The drawn line will always turn "left" when leaving *p1* and always join *p2* from the "right".

      :arg point_like p1: starting point

      :arg point_like p2: end point

      :arg float breadth: the amplitude of each wave. The condition *2 * breadth < abs(p2 - p1)* must be true to fit in at least one wave. See the following picture, which shows two points connected by one full period.

      :rtype: :ref:`Point`
      :returns: the end point, *p2*.

      .. image:: ../images/img-breadth.*

      Here is an example of three connected lines, forming a closed, filled triangle. Little arrows indicate the stroking direction.

      .. image:: ../images/img-squiggly.*

      .. note:: Waves drawn are **not** trigonometric (sine / cosine). If you need that, have a look at `draw.py <https://github.com/pymupdf/PyMuPDF-Utilities/blob/master/examples/draw-sines/draw.py>`_.

   .. index::
      pair: breadth; DrawZigzag

   .. method:: DrawZigzag(Point p1, Point p2, float breadth=2)

      Draw a zigzag line from :data:`Point` objects *p1* to *p2*. Otherwise works exactly like :meth:`Shape.DrawSquiggle`.

      :arg Point p1: starting point

      :arg Point p2: end point

      :arg float breadth: the amplitude of the movement. The condition *2 * breadth < abs(p2 - p1)* must be true to fit in at least one period.

      :rtype: :ref:`Point`
      :returns: the end point, *p2*.

   .. method:: DrawPolyline(Point[] points)

      Draw several connected lines between points contained in the sequence *points*. This can be used for creating arbitrary polygons by setting the last item equal to the first one.

      :arg Point[] points: a sequence of :data:`Point` objects. Its length must at least be 2 (in which case it is equivalent to *DrawLine()*).

      :rtype: :ref:`Point`
      :returns: *points[-1]* -- the last point in the argument sequence.

   .. method:: DrawBezier(Point p1, Point p2, Point p3, Point p4)

      Draw a standard cubic Bézier curve from *p1* to *p4*, using *p2* and *p3* as control points.

      All arguments are :data:`point_like` \s.

      :rtype: :ref:`Point`
      :returns: the end point, *p4*.

      .. note:: The points do not need to be different -- experiment a bit with some of them being equal!

      Example:

      .. image:: ../images/img-drawBezier.*

   .. method:: DrawOval(Rect tetra)

      Draw an "ellipse" inside the given tetragon (quadrilateral). If it is a square, a regular circle is drawn, a general rectangle will result in an ellipse. If a quadrilateral is used instead, a plethora of shapes can be the result.

      The drawing starts and ends at the middle point of the line `bottom-left -> top-left` corners in an anti-clockwise movement.

      :arg rect_like,quad_like tetra: :data:`Rect` or :data:`Quad`.

      :rtype: :ref:`Point`
      :returns: the middle point of line `rect.BottomLeft -> rect.TopLeft`, or resp. `quad.LowLeft -> quad.UpperLeft`. Look at just a few examples here, or at the *quad-show?.py* scripts in the PyMuPDF-Utilities repository.

      .. image:: ../images/img-drawquad.*
         :scale: 50

   .. method:: DrawCircle(Point center, float radius)

      Draw a circle given its center and radius. The drawing starts and ends at point `center - (radius, 0)` in an **anti-clockwise** movement. This point is the middle of the enclosing square's left side.

      This is a shortcut for `DrawSector(center, start, 360, fullSector=false)`. To draw the same circle in a **clockwise** movement, use `-360` as degrees.

      :arg point_like center: the center of the circle.

      :arg float radius: the radius of the circle. Must be positive.

      :rtype: :ref:`Point`
      :returns: `Point(center.x - radius, center.y)`.

         .. image:: ../images/img-drawcircle.*
            :scale: 60

   .. method:: DrawCurve(Point p1, Point p2, Point p3)

      A special case of *DrawBezier()*: Draw a cubic Bezier curve from *p1* to *p3*. On each of the two lines `p1 -> p2` and `p3 -> p2` one control point is generated. Both control points will therefore be on the same side of the line `p1 -> p3`. This guaranties that the curve's curvature does not change its sign. If the lines to p2 intersect with an angle of 90 degrees, then the resulting curve is a quarter ellipse (resp. quarter circle, if of same length).

      All arguments are :data:`point_like`.

      :rtype: :ref:`Point`
      :returns: the end point, *p3*. The following is a filled quarter ellipse segment. The yellow area is oriented **clockwise:**

         .. image:: ../images/img-drawCurve.png
            :align: center


   .. index::
      pair: fullSector; DrawSector

   .. method:: DrawSector(Point center, Point point, float angle, bool fullSector=true)

      Draw a circular sector, optionally connecting the arc to the circle's center (like a piece of pie).

      :arg point_like center: the center of the circle.

      :arg point_like point: one of the two end points of the pie's arc segment. The other one is calculated from the *angle*.

      :arg float angle: the angle of the sector in degrees. Used to calculate the other end point of the arc. Depending on its sign, the arc is drawn anti-clockwise (positive) or clockwise.

      :arg bool fullSector: whether to draw connecting lines from the ends of the arc to the circle center. If a fill color is specified, the full "pie" is colored, otherwise just the sector.

      :rtype: :ref:`Point`
      :returns: the other end point of the arc. Can be used as starting point for a following invocation to create logically connected pies charts. Examples:

         .. image:: ../images/img-drawSector1.*

         .. image:: ../images/img-drawSector2.*


   .. method:: DrawRect(Rect rect, float radius=0)

      Draw a rectangle. The drawing starts and ends at the top-left corner in an anti-clockwise movement.

      :arg rect_like rect: where to put the rectangle on the page.
      :arg multiple radius: draw rounded rectangle corners. If not `null`, specifies the radius of the curvature as a percentage of a rectangle side length. This must one or (a tuple of) two floats `0 < radius <= 0.5`, where 0.5 corresponds to 50% of the respective side. If a float, the radius of the curvature is computed as `radius * min(width, height)`, drawing the corner's perimeter as a quarter circle. If a tuple `(rx, ry)` is given, then the curvature is asymmetric with respect to the horizontal and vertical directions. A value of `radius=(0.5, 0.5)` draws an ellipse.

      :rtype: :ref:`Point`
      :returns: top-left corner of the rectangle.

   .. method:: DrawQuad(Quad quad)

      Draw a quadrilateral. The drawing starts and ends at the top-left corner (:attr:`Quad.UpperLeft`) in an anti-clockwise movement. It is a shortcut of :meth:`Shape.DrawPolyline` with the argument `(ul, ll, lr, ur, ul)`.

      :arg quad_like quad: where to put the tetragon on the page.

      :rtype: :ref:`Point`
      :returns: :attr:`Quad.UpperLeft`.

   .. index::
      pair: closePath; Finish
      pair: color; Finish
      pair: dashes; Finish
      pair: even_odd; Finish
      pair: fill; Finish
      pair: lineCap; Finish
      pair: lineJoin; Finish
      pair: morph; Finish
      pair: width; Finish
      pair: strokeOpacity; Finish
      pair: fillOpacity; Finish
      pair: oc; Finish


   .. method:: Finish(float width=1, float[] color=null, float fill=null, int lineCap=0, int lineJoin=0, string dashes=null, bool closePath=true, bool evenOdd=false, Morph morph=null, float strokeOpacity=1, float fillOpacity=1, int oc=0)

      Finish a set of *draw*()* methods by applying :ref:`CommonParms` to all of them.
      
      It has **no effect on** :meth:`Shape.InsertText` and :meth:`Shape.InsertTextbox`.

      The method also supports **morphing the compound drawing** using :ref:`Point` *fixpoint* and :ref:`Matrix` *Matrix*.

      :arg sequence morph: morph the text or the compound drawing around some arbitrary :ref:`Point` *fixpoint* by applying :ref:`Matrix` *matrix* to it. This implies that *fixpoint* is a **fixed point** of this operation: it will not change its position. Default is no morphing (*null*). The matrix can contain any values in its first 4 components, *matrix.e == matrix.f == 0* must be true, however. This means that any combination of scaling, shearing, rotating, flipping, etc. is possible, but translations are not.

      :arg float stroke_opacity: set transparency for stroke colors. Value < 0 or > 1 will be ignored. Default is 1 (intransparent).
      :arg float fill_opacity: set transparency for fill colors. Default is 1 (intransparent).

      :arg bool even_odd: request the **"even-odd rule"** for filling operations. Default is *false*, so that the **"nonzero winding number rule"** is used. These rules are alternative methods to apply the fill color where areas overlap. Only with fairly complex shapes a different behavior is to be expected with these rules. For an in-depth explanation, see :ref:`AdobeManual`, pp. 137 ff. Here is an example to demonstrate the difference.

      :arg int oc: the :data:`xref` number of an :data:`OCG` or :data:`OCMD` to make this drawing conditionally displayable.

      .. image:: ../images/img-even-odd.*

      .. note:: For each pixel in a shape, the following will happen:

         1. Rule **"eveOdd"** counts, how many areas contain the pixel. If this count is **odd,** the pixel is regarded **inside** the shape, if it is **even**, the pixel is **outside**.

         2. The default rule **"nonzero winding"** in addition looks at the *"orientation"* of each area containing the pixel: it **adds 1** if an area is drawn anti-clockwise and it **subtracts 1** for clockwise areas. If the result is zero, the pixel is regarded **outside,** pixels with a non-zero count are **inside** the shape.

         Of the four shapes in above image, the top two each show three circles drawn in standard manner (anti-clockwise, look at the arrows). The lower two shapes contain one (the top-left) circle drawn clockwise. As can be seen, area orientation is irrelevant for the right column (even-odd rule).

   .. index::
      pair: borderWidth; InsertText
      pair: color; InsertText
      pair: encoding; InsertText
      pair: fill; InsertText
      pair: fontFile; InsertText
      pair: fontName; InsertText
      pair: fontSize; InsertText
      pair: lineheight; InsertText
      pair: morph; InsertText
      pair: renderMode; InsertText
      pair: rotate; InsertText
      pair: strokeOpacity; InsertText
      pair: fillOpacity; InsertText
      pair: oc; InsertText

   .. method:: InsertText(Point point, string text, float fontSize=11, string fontName="helv", string fontFile=null, bool setSample=false, int encoding=TEXT_ENCODING_LATIN, float[] color=null, float lineheight=0, float[] fill=null, int renderMode=0, float borderWidth=0.05, int rotate=0, Morph morph=null, float strokeOpacity=1, float fillOpacity=1, int oc=0)

      Insert text lines start at *point*.

      :arg Point point: the bottom-left position of the first character of *text* in pixels. It is important to understand, how this works in conjunction with the *rotate* parameter. Please have a look at the following picture. The small red dots indicate the positions of *point* in each of the four possible cases.

         .. image:: ../images/img-inserttext.*
            :scale: 33

      :arg str/sequence text: the text to be inserted. May be specified as either a string type or as a sequence type. For sequences, or strings containing line breaks *\n*, several lines will be inserted. No care will be taken if lines are too wide, but the number of inserted lines will be limited by "vertical" space on the page (in the sense of reading direction as established by the *rotate* parameter). Any rest of *text* is discarded -- the return code however contains the number of inserted lines.

      :arg float lineHeight: a factor to override the line height calculated from font properties. If not `null`, a line height of `fontsize * lineheight` will be used.
      :arg float strokeOpacity: set transparency for stroke colors (the **border line** of a character). Only  `0 <= value <= 1` will be considered. Default is 1 (intransparent).
      :arg float fillOpacity: set transparency for fill colors. Default is 1 (intransparent). Use this value to control transparency of the text color. Stroke opacity **only** affects the border line of characters.

      :arg int rotate: determines whether to rotate the text. Acceptable values are multiples of 90 degrees. Default is 0 (no rotation), meaning horizontal text lines oriented from left to right. 180 means text is shown upside down from **right to left**. 90 means anti-clockwise rotation, text running **upwards**. 270 (or -90) means clockwise rotation, text running **downwards**. In any case, *point* specifies the bottom-left coordinates of the first character's rectangle. Multiple lines, if present, always follow the reading direction established by this parameter. So line 2 is located **above** line 1 in case of `rotate = 180`, etc.

      :arg int oc: the :data:`xref` number of an :data:`OCG` or :data:`OCMD` to make this text conditionally displayable.

      :rtype: int
      :returns: number of lines inserted.

      For a description of the other parameters see :ref:`CommonParms`.

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
      pair: lineHeight; InsertTextbox
      pair: morph; InsertTextbox
      pair: renderMode; InsertTextbox
      pair: rotate; InsertTextbox
      pair: oc; InsertTextbox

   .. method:: InsertTextbox(Rect rect, string buffer, float fontSize=11, string fontName="helv", string fontFile=null, bool setSimple=false, int encoding=TEXT_ENCODING_LATIN, float[] color=null, float[] fill=null, int renderMode=0, float borderWidth=0.05, int expandTabs=1, int align=TEXT_ALIGN_LEFT, int rotate=0, float lineHeight=0, Morph morph=null, float strokeOpacity=1, float fillOpacity=1, int oc=0)

      PDF only: Insert text into the specified rectangle. The text will be split into lines and words and then filled into the available space, starting from one of the four rectangle corners, which depends on `rotate`. Line feeds and multiple space will be respected.

      :arg rect_like rect: the area to use. It must be finite and not empty.

      :arg str/sequence buffer: the text to be inserted. Must be specified as a string or a sequence of strings. Line breaks are respected also when occurring in a sequence entry.

      :arg int align: align each text line. Default is 0 (left). Centered, right and justified are the other supported options, see :ref:`TextAlign`. Please note that the effect of parameter value *TEXT_ALIGN_JUSTIFY* is only achievable with "simple" (single-byte) fonts (including the :ref:`Base-14-Fonts`).

      :arg float lineheight: a factor to override the line height calculated from font properties. If not `null`, a line height of `fontsize * lineheight` will be used.

      :arg int expandtabs: controls handling of tab characters ``\t`` using the `string.expandtabs()` method **per each line**.

      :arg float stroke_opacity: set transparency for stroke colors. Negative values and values > 1 will be ignored. Default is 1 (intransparent).
      :arg float fill_opacity: set transparency for fill colors. Default is 1 (intransparent). Use this value to control transparency of the text color. Stroke opacity **only** affects the border line of characters.

      :arg int rotate: requests text to be rotated in the rectangle. This value must be a multiple of 90 degrees. Default is 0 (no rotation). Effectively, the four values `0`, `90`, `180` and `270` (= `-90`) are processed, each causing the text to start in a different rectangle corner. Bottom-left is `90`, bottom-right is `180`, and `-90 / 270` is top-right. See the example how text is filled in a rectangle. This argument takes precedence over morphing. See the second example, which shows text first rotated left by `90` degrees and then the whole rectangle rotated clockwise around is lower left corner.

      :arg int oc: *(new in v1.18.4)* the :data:`xref` number of an :data:`OCG` or :data:`OCMD` to make this text conditionally displayable.

      :rtype: float
      :returns:
          **If positive or zero**: successful execution. The value returned is the unused rectangle line space in pixels. This may safely be ignored -- or be used to optimize the rectangle, position subsequent items, etc.

          **If negative**: no execution. The value returned is the space deficit to store text lines. Enlarge rectangle, decrease *fontsize*, decrease text amount, etc.

      .. image:: ../images/img-rotate.*

      .. image:: ../images/img-rot+morph.*

      For a description of the other parameters see :ref:`CommonParms`.


   .. index::
      pair: overlay; commit

   .. method:: Commit(bool overlay=true)

      Update the page's :data:`contents` with the accumulated drawings, followed by any text insertions. If text overlaps drawings, it will be written on top of the drawings.
      
      .. warning:: **Do not forget to execute this method:**
      
            If a shape is **not committed, it will be ignored and the page will not be changed!**

      The method will reset attributes :attr:`Shape.Rect`, :attr:`LastPoint`, :attr:`DrawCont`, :attr:`TextCont` and :attr:`TotalCont`. Afterwards, the shape object can be reused for the **same page**.

      :arg bool overlay: determine whether to put content in foreground (default) or background. Relevant only, if the page already has a non-empty :data:`contents` object.

   **---------- Attributes ----------**

   .. attribute:: Doc

      For reference only: the page's document.

      :type: :ref:`Document`

   .. attribute:: Page

      For reference only: the owning page.

      :type: :ref:`Page`

   .. attribute:: Height

      Copy of the page's height

      :type: float

   .. attribute:: Width

      Copy of the page's width.

      :type: float

   .. attribute:: DrawCont

      Accumulated command buffer for **draw methods** since last finish. Every finish method will append its commands to :attr:`Shape.TotalCont`.

      :type: str

   .. attribute:: TextCont

      Accumulated text buffer. All **text insertions** go here. This buffer will be appended to :attr:`totalcont` :meth:`Shape.Commit`, so that text will never be covered by drawings in the same Shape.

      :type: str

   .. attribute:: Rect

      Rectangle surrounding drawings. This attribute is at your disposal and may be changed at any time. Its value is set to *null* when a shape is created or committed. Every *draw** method, and :meth:`Shape.InsertTextbox` update this property (i.e. **enlarge** the rectangle as needed). **Morphing** operations, however (:meth:`Shape.Finish`, :meth:`Shape.InsertTextbox`) are ignored.

      A typical use of this attribute would be setting :attr:`Page.CropboxPosition` to this value, when you are creating shapes for later or external use. If you have not manipulated the attribute yourself, it should reflect a rectangle that contains all drawings so far.

      If you have used morphing and need a rectangle containing the morphed objects, use the following code::

         >>> # assuming ...
         >>> morph = (point, matrix)
         >>> # ... recalculate the shape rectangle like so:
         >>> shape.rect = (shape.rect - fitz.Rect(point, point)) * ~matrix + fitz.Rect(point, point)

      :type: :ref:`Rect`

   .. attribute:: TotalCont

      Total accumulated command buffer for draws and text insertions. This will be used by :meth:`Shape.Commit`.

      :type: str

   .. attribute:: LastPoint

      For reference only: the current point of the drawing path. It is *null* at *Shape* creation and after each *finish()* and *commit()*.

      :type: :ref:`Point`

Usage
------
A drawing object is constructed by *shape = page.NewShape()*. After this, as many draw, finish and text insertions methods as required may follow. Each sequence of draws must be finished before the drawing is committed. The overall coding pattern looks like this::

.. note::

   1. Each *finish()* combines the preceding draws into one logical shape, giving it common colors, line width, morphing, etc. If *closePath* is specified, it will also connect the end point of the last draw with the starting point of the first one.

   2. To successfully create compound graphics, let each draw method use the end point of the previous one as its starting point. In the above pseudo code, *draw2* should hence use the returned :ref:`Point` of *draw1* as its starting point. Failing to do so, would automatically start a new path and *finish()* may not work as expected (but it won't complain either).

   3. Text insertions may occur anywhere before the commit (they neither touch :attr:`Shape.DrawCont` nor :attr:`Shape.lastPoint`). They are appended to *Shape.totalcont* directly, whereas draws will be appended by *Shape.Finish*.

   4. Each *commit* takes all text insertions and shapes and places them in foreground or background on the page -- thus providing a way to control graphical layers.

   5. **Only** *commit* **will update** the page's contents, the other methods are basically string manipulations.

Examples
---------
1. Create a full circle of pieces of pie in different colors::

      shape = page.new_shape()  # start a new shape
      cols = (...)  # a sequence of RGB color triples
      pieces = len(cols)  # number of pieces to draw
      beta = 360. / pieces  # angle of each piece of pie
      center = fitz.Point(...)  # center of the pie
      p0 = fitz.Point(...)  # starting point
      for i in range(pieces):
          p0 = shape.DrawSector(center, p0, beta,
                                fullSector=true) # draw piece
          # now fill it but do not connect ends of the arc
          shape.Finish(fill=cols[i], closePath=false)
      shape.Commit()  # update the page

Here is an example for 5 colors:

.. image:: ../images/img-cake.*

2. Create a regular n-edged polygon (fill yellow, red border). We use *DrawSector()* only to calculate the points on the circumference, and empty the draw command buffer again before drawing the polygon::

      shape = page.new_shape() # start a new shape
      beta = -360.0 / n  # our angle, drawn clockwise
      center = fitz.Point(...)  # center of circle
      p0 = fitz.Point(...)  # start here (1st edge)
      points = [p0]  # store polygon edges
      for i in range(n):  # calculate the edges
          p0 = shape.DrawSector(center, p0, beta)
          points.append(p0)
      shape.DrawCont = ""  # do not draw the circle sectors
      shape.DrawPolyline(points)  # draw the polygon
      shape.Finish(color=(1,0,0), fill=(1,1,0), closePath=false)
      shape.Commit()

Here is the polygon for n = 7:

.. image:: ../images/img-7edges.*

.. _CommonParms:

Common Parameters
-------------------

**FontName** (*string*)

  In general, there are three options:

  1. Use one of the standard :ref:`Base-14-Fonts`. In this case, *fontfile* **must not** be specified and *"Helvetica"* is used if this parameter is omitted, too.
  2. Choose a font already in use by the page. Then specify its **reference** name prefixed with a slash "/", see example below.
  3. Specify a font file present on your system. In this case choose an arbitrary, but new name for this parameter (without "/" prefix).

  If inserted text should re-use one of the page's fonts, use its reference name appearing in :meth:`Page.get_fonts` like so:

  Suppose the font list has the item *[1024, 0, 'Type1', 'NimbusMonL-Bold', 'R366']*, then specify *fontname = "/R366", fontfile = null* to use font *NimbusMonL-Bold*.

----

**FontFile** (*string*)

  File path of a font existing on your computer. If you specify *fontfile*, make sure you use a *fontname* **not occurring** in the above list. This new font will be embedded in the PDF upon *doc.save()*. Similar to new images, a font file will be embedded only once. A table of MD5 codes for the binary font contents is used to ensure this.

----

**SetSimple** (*bool*)

  Fonts installed from files are installed as **Type0** fonts by default. If you want to use 1-byte characters only, set this to true. This setting cannot be reverted. Subsequent changes are ignored.

----

**FontSize** (*float*)

  Font size of text, see: :data:`fontsize`.

----

**Dashes** (*string*)

  Causes lines to be drawn dashed. The general format is `"[n m] p"` of (up to) 3 floats denoting pixel lengths. `n` is the dash length, `m` (optional) is the subsequent gap length, and `p` (the "phase" - **required**, even if 0!) specifies how many pixels should be skipped before the dashing starts. If `m` is omitted, it defaults to `n`.
  
  A continuous line (no dashes) is drawn with `"[] 0"` or *null* or `""`. Examples:
  
  * Specifying `"[3 4] 0"` means dashes of 3 and gaps of 4 pixels following each other.
  * `"[3 3] 0"` and `"[3] 0"` do the same thing.
  
  For (the rather complex) details on how to achieve sophisticated dashing effects, see :ref:`AdobeManual`, page 217.

----

**Color / Fill** (*list, tuple*)

  Stroke and fill colors can be specified as tuples or list of of floats from 0 to 1. These sequences must have a length of 1 (GRAY), 3 (RGB) or 4 (CMYK). For GRAY colorspace, a single float instead of the unwieldy *(float,)* or *[float]* is also accepted. Accept (default) or use `null` to not use the parameter.

  To simplify color specification, method *getColor()* in *fitz.utils* may be used to get predefined RGB color triples by name. It accepts a string as the name of the color and returns the corresponding triple. The method knows over 540 color names -- see section :ref:`ColorDatabase`.

  Please note that the term *color* usually means "stroke" color when used in conjunction with fill color.

  If letting default a color parameter to `null`, then no resp. color selection command will be generated. If *fill* and *color* are both `null`, then the drawing will contain no color specification. But it will still be "stroked", which causes PDF's default color "black" be used by Adobe Acrobat and all other viewers.

----

**Width** (*float*)

  The stroke ("border") width of the elements in a shape (if applicable). The default value is 1. The values width, color and fill have the following relationship / dependency:

  * If `fill=null` shape elements will always be drawn with a border - even if `color=null` (in which case black is taken) or `width=0` (in which case 1 is taken).
  * Shapes without border can only be achieved if a fill color is specified (which may be white of course). To achieve this, specify `width=0`. In this case, the `color` parameter is ignored.

----

**StrokeOpacity / FillOpacity** (*floats*)

  Both values are floats in range [0, 1]. Negative values or values > 1 will ignored (in most cases). Both set the transparency such that a value 0.5 corresponds to 50% transparency, 0 means invisible and 1 means intransparent. For e.g. a rectangle the stroke opacity applies to its border and fill opacity to its interior.

  For text insertions (:meth:`Shape.InsertText` and :meth:`Shape.InsertTextbox`), use *fill_opacity* for the text. At first sight this seems surprising, but it becomes obvious when you look further down to *render_mode*: *fill_opacity* applies to the yellow and *stroke_opacity* applies to the blue color.

----

**BorderWidth** (*float*)

  Set the border width for text insertions. New in v1.14.9. Relevant only if the render mode argument is used with a value greater zero.

----

**RenderMode** (*int*)

  *New in version 1.14.9:* Integer in `range(8)` which controls the text appearance (:meth:`Shape.InsertText` and :meth:`Shape.InsertTextbox`). See page 246 in :ref:`AdobeManual`. New in v1.14.9. These methods now also differentiate between fill and stroke colors.

  * For default 0, only the text fill color is used to paint the text. For backward compatibility, using the *color* parameter instead also works.
  * For render mode 1, only the border of each glyph (i.e. text character) is drawn with a thickness as set in argument *border_width*. The color chosen in the *color* argument is taken for this, the *fill* parameter is ignored.
  * For render mode 2, the glyphs are filled and stroked, using both color parameters and the specified border width. You can use this value to simulate **bold text** without using another font: choose the same value for *fill* and *color* and an appropriate value for *border_width*.
  * For render mode 3, the glyphs are neither stroked nor filled: the text becomes invisible.

  The following examples use border_width=0.3, together with a fontsize of 15. Stroke color is blue and fill color is some yellow.

  .. image:: ../images/img-rendermode.*

----

**Overlay** (*bool*)

  Causes the item to appear in foreground (default) or background.

----

**Morph** (*Morph*)

  Causes "morphing" of either a shape, created by the *draw*()* methods, or the text inserted by page methods *InsertTextbox()* / *InsertText()*. If not *null*, it must be a pair *(fixpoint, matrix)*, where *fixpoint* is a :ref:`Point` and *matrix* is a :ref:`Matrix`. The matrix can be anything except translations, i.e. *matrix.e == matrix.f == 0* must be true. The point is used as a fixed point for the matrix operation. For example, if *matrix* is a rotation or scaling, then *fixpoint* is its center. Similarly, if *matrix* is a left-right or up-down flip, then the mirroring axis will be the vertical, respectively horizontal line going through *fixpoint*, etc.

  .. note:: Several methods contain checks whether the to be inserted items will actually fit into the page (like :meth:`Shape.InsertText`, or :meth:`Shape.DrawRect`). For the result of a morphing operation there is however no such guaranty: this is entirely the programmer's responsibility.

----

**LineCap (deprecated: "roundCap")** (*int*)

  Controls the look of line ends. The default value 0 lets each line end at exactly the given coordinate in a sharp edge. A value of 1 adds a semi-circle to the ends, whose center is the end point and whose diameter is the line width. Value 2 adds a semi-square with an edge length of line width and a center of the line end.

  *Changed in version 1.14.15*

----

**LineJoin** (*int*)

  *New in version 1.14.15:* Controls the way how line connections look like. This may be either as a sharp edge (0), a rounded join (1), or a cut-off edge (2, "butt").

----

**ClosePath** (*bool*)

  Causes the end point of a drawing to be automatically connected with the starting point (by a straight line).

.. include:: ../footer.rst
