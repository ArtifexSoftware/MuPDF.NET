.. include:: ../header.rst

.. _Story:

================
Story
================


.. role:: htmlTag(emphasis)

=========================================== =============================================================
**Method / Attribute**                      **Short Description**
=========================================== =============================================================
:meth:`Story.Reset`                         "rewind" story output to its beginning
:meth:`Story.Place`                         Compute story content to fit in provided rectangle
:meth:`Story.Draw`                          Write the computed content to current page
:meth:`Story.ElementPositions`              Callback function logging currently processed story content
:attr:`Story.Body`                          The story's underlying :htmlTag:`Body`
:meth:`Story.Write`                         Places and draws Story to a DocumentWriter
:meth:`Story.WriteStabilized`               Iterative layout of html content to a DocumentWriter
:meth:`Story.WriteWithLinks`                Like `Write()` but also creates PDF links
:meth:`Story.WriteStabilizedWithLinks`      Like `WriteStabilized()` but also creates PDF links
:meth:`Story.Fit`                           Finds optimal rect that contains the story `this`
:meth:`Story.FitScale`                      
:meth:`Story.FitHeight`
:meth:`Story.FitWidth`
=========================================== =============================================================

**Class API**

.. class:: Story

   .. method:: Story(string html: "", string userCss: null, float em: 12, Archive archive: null)

      Create a **story**, optionally providing HTML and CSS source.
      The HTML is parsed, and held within the Story as a DOM (Document Object Model).

      This structure may be modified: content (text, images) may be added,
      copied, modified or removed by using methods of the :ref:`Xml` class.

      When finished, the **story** can be written to any device;
      in typical usage the device may be provided by a :ref:`DocumentWriter` to make new pages.

      Here are some general remarks:

      * The :ref:`Story` constructor parses and validates the provided HTML to create the DOM.
      * There are a number of ways to manipulate the HTML source by
        providing access to the *nodes* of the underlying DOM.
        Documents can be completely built from ground up programmatically,
        or the existing DOM can be modified pretty arbitrarily.
        For details of this interface, please see the :ref:`Xml` class.
      * If no (or no more) changes to the DOM are required,
        the story is ready to be laid out and to be fed to a series of devices
        (typically devices provided by a :ref:`DocumentWriter` to produce new pages).
      * The next step is to place the story and write it out.
        This can either be done directly, by looping around calling `Place()` and `Draw()`,
        or alternatively,
        the looping can handled for you using the `Write()` or `WriteStabilized()` methods.
        Which method you choose is largely a matter of taste.
        
        * To work in the first of these styles, the following loop should be used:
        
          1. Obtain a suitable device to write to;
             typically by requesting a new,
             empty page from a :ref:`DocumentWriter`.
          2. Determine one or more rectangles on the page,
             that should receive **story** data.
             Note that not every page needs to have the same set of rectangles.
          3. Pass each rectangle to the **story** to place it,
             learning what part of that rectangle has been filled,
             and whether there is more story data that did not fit.
             This step can be repeated several times with adjusted rectangles
             until the caller is happy with the results. 
          4. Optionally, at this point,
             we can request details of where interesting items have been placed,
             by calling the `ElementPositions()` method.
             Items are deemed to be interesting if their integer `heading` attribute is a non-zero
             (corresponding to HTML tags :htmlTag:`h1` - :htmlTag:`h6`),
             if their `id` attribute is not `null` (corresponding to HTML tag :htmlTag:`id`),
             or if their `href` attribute is not `null` (responding to HTML tag :htmlTag:`href`).
             This can conveniently be used for automatic generation of a Table of Contents,
             an index of images or the like.
          5. Next, draw that rectangle out to the device with the `Draw()` method.
          6. If the most recent call to `Place()` indicated that all the story data had fitted,
             stop now.
          7. Otherwise, we can loop back.
             If there are more rectangles to be placed on the current device (page),
             we jump back to step 3 - if not, we jump back to step 1 to get a new device.
        * Alternatively, in the case where you are using a :ref:`DocumentWriter`,
          the `Write()` or `WriteStabilized()` methods can be used.
          These handle all the looping for you,
          in exchange for being provided with callbacks that control the behaviour
          (notably a callback that enumerates the rectangles/pages to use).
      * Which part of the **story** will land on which rectangle / which page,
        is fully under control of the :ref:`Story` object and cannot be predicted.
      * Images may be part of a **story**. They will be placed together with any surrounding text.
      * Multiple stories may - independently from each other - write to the same page.
        For example, one may have separate stories for page header,
        page footer, regular text, comment boxes, etc.

      :arg string html: HTML source code. If omitted, a basic minimum is generated (see below).
        If provided, not a complete HTML document is needed.
        The in-built source parser will forgive (many / most)
        HTML syntax errors and also accepts HTML fragments like
        `"<b>Hello, <i>World!</i></b>"`.
      :arg string userCss: CSS source code. If provided, must contain valid CSS specifications.
      :arg float em: the default text font size.
      :arg archive: an :ref:`Archive` from which to load resources for rendering. Currently supported resource types are images and text fonts. If omitted, the story will not try to look up any such data and may thus produce incomplete output.
      
         .. note:: Instead of an actual archive, valid arguments for **creating** an :ref:`Archive` can also be provided -- in which case an archive will temporarily be constructed. So, instead of `story = new Story(archive: new Archive("myfolder"))`.

   .. method:: Place(Rect where)

      Calculate that part of the story's content, that will fit in the provided rectangle. The method maintains a pointer which part of the story's content has already been written and upon the next invocation resumes from that pointer's position.

      :arg Rect where: layout the current part of the content to fit into this rectangle. This must be a sub-rectangle of the page's :ref:`MediaBox<Glossary_MediaBox>`.

      :rtype: Tuple(bool, Rect)
      :returns: a bool (int) `more` and a rectangle `filled`. If `more == 0`, all content of the story has been written, otherwise more is waiting to be written to subsequent rectangles / pages. Rectangle `filled` is the part of `where` that has actually been filled.

   .. method:: Draw(DeviceWrapper dev, Matrix matrix: null)

      Write the content part prepared by :meth:`Story.Place` to the page.

      :arg dev: the :ref:`Device` created by `dev = writer.BeginPage(Rect mediabox)`. The device knows how to call all MuPDF functions needed to write the content.
      :arg Matrix matrix: a matrix for transforming content when writing to the page. An example may be writing rotated text. The default means no transformation (i.e. the :ref:`Identity` matrix).

   .. method:: ElementPositions(Action<Position> function, Position arg: null)

      Let the Story provide positioning information about certain HTML elements once their place on the current page has been computed - i.e. invoke this method **directly after** :meth:`Story.Place`.

      `Story` will pass position information to `function`. This information can for example be used to generate a Table of Contents.

      :arg callable function: a function accepting an :class:`ElementPosition` object. It will be invoked by the Story object to process positioning information. The function **must** be a callable accepting exactly one argument.
      :arg Position args: an optional dictionary with any **additional** information
        that should be added to the :class:`ElementPosition` instance passed to `function`.
        Like for example the current output page number.
        Every key in this dictionary must be a string that conforms to the rules for a valid dictionary identifier.
        The complete set of information is explained below.


   .. method:: Reset()

      Rewind the story's document to the beginning for starting over its output.

   .. attribute:: Body

      The :htmlTag:`body` part of the story's DOM. This attribute contains the :ref:`Xml` node of :htmlTag:`body`. All relevant content for PDF production is contained between "<body>" and "</body>".

   .. method:: Write(DocumentWriter writer, RectFunction rectfn, Action<Position> positionfn: null, Action<int, Rect, DeviceWrapper, bool> pagefn: null)

        Places and draws Story to a `DocumentWriter`. Avoids the need for
        calling code to implement a loop that calls `Story.Place()` and
        `Story.Draw()` etc, at the expense of having to provide at least the
        `rectfn()` callback.
       
        :arg writer: a `DocumentWriter` or null.
        :arg rectfn: a callable taking `(rectN: int, filled: Rect)` and
            returning `(mediabox, rect, ctm)`:
            
            * mediabox: null or rect for new page.
            * rect: The next rect into which content should be placed.
            * ctm: null or a `Matrix`.
        :arg positionfn: null, or a callable taking `Action<Position>`:
            
            * position:
                An `ElementPosition` with an extra `.page_num` member.
            Typically called multiple times as we generate elements that
            are headings or have an id.
        :arg pagefn:
            null, or a callable taking `Action<int, Rect, DeviceWrapper, bool>`;
            called at start (`after=0`) and end (`after=1`) of each page.

   .. staticmethod:: WriteStabilized(DocumentWriter writer, ContentFunction contentfn, RectFunction rectfn, string userCss: null, int em: 12, Action<Position> positionfn: null, Action<int, Rect, DeviceWrapper, bool> pagefn: null, Archive archive: null, bool addHeaderIds: true)
   
        Static method that does iterative layout of html content to a
        `DocumentWriter`.

        For example this allows one to add a table of contents section
        while ensuring that page numbers are patched up until stable.

        Repeatedly creates a new `Story` from `(contentfn(),
        userCss, em, archive)` and lays it out with internal call
        to `Story.Write()`; uses a null writer and extracts the list
        of `ElementPosition`'s which is passed to the next call of
        `contentfn()`.

        When the html from `contentfn()` becomes unchanged, we do a
        final iteration using `writer`.

        :arg writer:
            A `DocumentWriter`.
        :arg contentfn:
            A function taking a list of `ElementPositions` and
            returning a string containing html. The returned html
            can depend on the list of positions, for example with a
            table of contents near the start.
        :arg rectfn:
            A callable taking `(int rectN, Rect filled)` and
            returning `(Rect mediabox, Rect rect, Matrix ctm)`:

            * mediabox: null or rect for new page.
            * rect: The next rect into which content should be placed.
            * ctm: A `Matrix`.
        :arg pagefn:
            null, or a callable taking `(int pageN, Rect medibox,
            DeviceWrapper dev, bool after)`; called at start (`after=0`) and end
            (`after=1`) of each page.
        :arg archive:
        :arg addHeaderIds:
            If true, we add unique ids to all header tags that
            don't already have an id. This can help automatic
            generation of tables of contents.
        Returns:
            null.
       
   .. method:: WriteWithLinks(RectFunction rectfn, Action<Position> positionFn: null, Action<int, Rect, DeviceWrapper, bool> pageFn: null)

        Similar to `Write()` except that we don't have a `writer` arg
        and we return a PDF `Document` in which links have been created
        for each internal html link.

   .. staticmethod:: WriteStabilizedWithLinks(ContentFunction contentfn, RectFunction rectfn, string userCss: null, int em=12, Action<Position> positionFn: null, Action<int, Rect, DeviceWrapper, bool> pageFn=null, Archive archive=null, bool addHeaderIds: true)

        Similar to `WriteStabilized()` except that we don't have a `writer`
        arg and instead return a PDF `Document` in which links have been
        created for each internal html link.
    
   .. class:: Story.FitResult
    
        The result from a `Story.Fit*()` method.
        
        Members:
        
        `BigEnough`:
            `true` if the fit succeeded.
        `Filled`:
            From the last call to `Story.Place()`.
        `More`:
            `false` if the fit succeeded.
        `NumCalls`:
            Number of calls made to `self.Place()`.
        `Parameter`:
            The successful parameter value, or the largest failing value.
        `Rect`:
            The rect created from `parameter`.
        
   .. method:: Fit(Func<Rect, float, Rect> fn, Rect rect, float pmin: null, float pmax: null, float delta: 0.001, bool verbose: false)

        Finds optimal rect that contains the story `self`.
        
        Returns a `Story.FitResult` instance.
            
        On success, the last call to `self.Place()` will have been with the
        returned rectangle, so `self.Draw()` can be used directly.
        
        :arg fn:
            A callable taking a floating point `parameter` and returning a
            `Rect()`. If the rect is empty, we assume the story will
            not fit and do not call `self.Place()`.

            Must guarantee that `self.Place()` behaves monotonically when
            given rect `fn(parameter`) as `parameter` increases. This
            usually means that both width and height increase or stay
            unchanged as `parameter` increases.
        :arg pmin:
            Minimum parameter to consider; `null` for -infinity.
        :arg pmax:
            Maximum parameter to consider; `null` for +infinity.
        :arg delta:
            Maximum error in returned `parameter`.
        :arg verbose:
            If true we output diagnostics.

   .. method:: FitScale(Rect rect, float scaleMin: 0, float scaleMax: 0, float delta: 0.001, bool verbose: false)

        Finds smallest value `scale` in range `scaleMin..scaleMax` where
        `scale * rect` is large enough to contain the story `self`.

        Returns a `Story.FitResult` instance.

        :arg width:
            width of rect.
        :arg height:
            height of rect.
        :arg scaleMin:
            Minimum scale to consider; must be >= 0.
        :arg scaleMax:
            Maximum scale to consider, must be >= scaleMin or `null` for
            infinite.
        :arg delta:
            Maximum error in returned scale.
        :arg verbose:
            If true we output diagnostics.

   .. method:: FitHeight(float width, float heightMin: 0, float heightMax: null, Point origin: null, float delta: 0.001, bool verbose: false)

        Finds smallest height in range `heightMin..heightMax` where a rect
        with size `(width, height)` is large enough to contain the story
        `self`.

        Returns a `Story.FitResult` instance.

        :arg width:
            width of rect.
        :arg heightMin:
            Minimum height to consider; must be >= 0.
        :arg heightMax:
            Maximum height to consider, must be >= heightMin or `null` for
            infinite.
        :arg origin:
            `(x0, y0)` of rect.
        :arg delta:
            Maximum error in returned height.
        :arg verbose:
            If true we output diagnostics.

   .. method:: FitWidth(float height, float widthMin: 0, float widthMax: 0, Point origin: null, float delta: 0.001, bool verbose: false)

        Finds smallest width in range `widthMin..widthMax` where a rect with size
        `(width, height)` is large enough to contain the story `self`.

        Returns a `Story.FitResult` instance.

        :arg height:
            height of rect.
        :arg widthMin:
            Minimum width to consider; must be >= 0.
        :arg widthMax:
            Maximum width to consider, must be >= widthMin or `null` for
            infinite.
        :arg origin:
            `(x0, y0)` of rect.
        :arg delta:
            Maximum error in returned width.
        :arg verbose:
            If true we output diagnostics.


Element Positioning CallBack function
--------------------------------------

The callback function can be used to log information about story output. The function's access to the information is read-only: it has no way to influence the story's output.

A typical loop for executing a story with using this method would look like this:

.. code-block:: cs

    string html = "<html><head></head><body><h1>Header level 1</h1><h2>Header level 2</h2></body><p>Hello MuPDF</p></html>";

    Rect box = Utils.PageRect("letter");
    Rect where = box + new Rect(36, 36, -36, -36);
    Story story = new Story(html: html);
    DocumentWriter writer = new DocumentWriter("output.pdf");

    int pno = 0;
    bool more = true;

    while (more)
    {
        Rect filled = new Rect();
        DeviceWrapper dev = writer.BeginPage(box);
        (more, filled) = story.Place(where);
        story.ElementPositions(null, new Position() { PageNum = pno });
        story.Draw(dev);
        writer.EndPage();
        pno += 1;
    }
    writer.Close();


Attributes of the ElementPosition class
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Exactly one parameter must be passed to the function provided by :meth:`Story.ElementPositions`. It is an object with the following attributes:

The parameter passed to the `recorder` function is an object with the following attributes:

* `elpos.Depth` (int) -- depth of this element in the box structure.

* `elpos.Heading` (int) -- the header level, 0 if no header, 1-6 for :htmlTag:`h1` - :htmlTag:`h6`.

* `elpos.Href` (string) -- value of the `href` attribute, or null if not defined.

* `elpos.Id` (string) -- value of the `id` attribute, or null if not defined.

* `elpos.Rect` (Rect) -- element position on page.

* `elpos.Text` (string) -- immediate text of the element.

* `elpos.OpenClose` (int bit field) -- bit 0 set: opens element, bit 1 set: closes element. Relevant for elements that may contain other elements and thus may not immediately be closed after being created / opened.

* `elpos.RectNum` (int) -- count of rectangles filled by the story so far.

* `elpos.PageNum` (int) -- page number; only present when using `Story.Write*()` functions.

.. include:: ../footer.rst
