.. include:: header.rst

.. _Xml:

================
Xml
================

.. role:: htmlTag(emphasis)

* New in v1.21.0

This represents an HTML or an XML node. It is a helper class intended to access the DOM (Document Object Model) content of a :ref:`Story` object.

There is no need to ever directly construct an :ref:`Xml` object: after creating a :ref:`Story`, simply take :attr:`Story.body` -- which is an Xml node -- and use it to navigate your way through the story's DOM.


================================ ===========================================================================================
**Method / Attribute**             **Description**
================================ ===========================================================================================
:meth:`~.AddBulletList`          Add a :htmlTag:`ul` tag - bulleted list, context manager.
:meth:`~.AddCodeBlock`           Add a :htmlTag:`pre` tag, context manager.
:meth:`~.AddDescriptionList`     Add a :htmlTag:`dl` tag, context manager.
:meth:`~.AddDivision`            add a :htmlTag:`div` tag (renamed from “section”), context manager.
:meth:`~.AddHeader`              Add a header tag (one of :htmlTag:`h1` to :htmlTag:`h6`), context manager.
:meth:`~.AddHorizontalLine`      Add a :htmlTag:`hr` tag.
:meth:`~.AddImage`               Add a :htmlTag:`img` tag.
:meth:`~.AddLink`                Add a :htmlTag:`a` tag.
:meth:`~.addnumberlist`          Add a :htmlTag:`ol` tag, context manager.
:meth:`~.AddParagraph`           Add a :htmlTag:`p` tag.
:meth:`~.AddSpan`                Add a :htmlTag:`span` tag, context manager.
:meth:`~.AddSubscript`           Add subscript text(:htmlTag:`sub` tag) - inline element, treated like text.
:meth:`~.AddSuperscript`         Add subscript text (:htmlTag:`sup` tag) - inline element, treated like text.
:meth:`~.AddCode`                Add code text (:htmlTag:`code` tag) - inline element, treated like text.
:meth:`~.AddVar`                 Add code text (:htmlTag:`code` tag) - inline element, treated like text.
:meth:`~.AddSamp`                Add code text (:htmlTag:`code` tag) - inline element, treated like text.
:meth:`~.AddKbd`                 Add code text (:htmlTag:`code` tag) - inline element, treated like text.
:meth:`~.AddText`                Add a text string. Line breaks `\n` are honored as :htmlTag:`br` tags.
:meth:`~.AppendChild`            Append a child node.
:meth:`~.Clone`                  Make a copy if this node.
:meth:`~.CreateElement`          Make a new node with a given tag name.
:meth:`~.CreateTextNode`         Create direct text for the current node.
:meth:`~.Find`                   Find a sub-node with given properties.
:meth:`~.FindNext`               Repeat previous "find" with the same criteria.
:meth:`~.InsertAfter`            Insert an element after current node.
:meth:`~.InsertBefore`           Insert an element before current node.
:meth:`~.Remove`                 Remove this node.
:meth:`~.SetAlign`               Set the alignment using a CSS style spec. Only works for block-level tags.
:meth:`~.SetAttribute`           Set an arbitrary key to some value (which may be empty).
:meth:`~.SetBgColor`             Set the background color. Only works for block-level tags.
:meth:`~.SetBold`                Set bold on or off or to some string value.
:meth:`~.SetColor`               Set text color.
:meth:`~.SetColumns`             Set the number of columns. Argument may be any valid number or string.
:meth:`~.SetFont`                Set the font-family, e.g. “sans-serif”.
:meth:`~.SetFontSize`            Set the font size. Either a float or a valid HTML/CSS string.
:meth:`~.SetId`                  Set a :htmlTag:`id`. A check for uniqueness is performed.
:meth:`~.SetItalic`              Set italic on or off or to some string value.
:meth:`~.SetLeading`             Set inter-block text distance (`-mupdf-leading`), only works on block-level nodes.
:meth:`~.SetLineHeight`          Set height of a line. Float like 1.5, which sets to `1.5 * fontsize`.
:meth:`~.SetMargins`             Set the margin(s), float or string with up to 4 values.
:meth:`~.SetPageBreakAfter`      Insert a page break after this node.
:meth:`~.SetPageBreakBefore`     Insert a page break before this node.
:meth:`~.SetProperties`          Set any or all desired properties in one call.
:meth:`~.AddStyle`               Set (add) a “style” that is not supported by its own `set_` method.
:meth:`~.AddClass`               Set (add) a “class” attribute.
:meth:`~.SetTextIndent`          Set indentation for first textblock line. Only works for block-level nodes.
:attr:`~.TagName`                Either the HTML tag name like :htmlTag:`p` or `null` if a text node.
:attr:`~.Text`                   Either the node's text or `null` if a tag node.
:attr:`~.IsText`                 Check if the node is a text.
:attr:`~.FirstChild`             Contains the first node one level below this one (or `null`).
:attr:`~.LastChild`              Contains the last node one level below this one (or `null`).
:attr:`~.Next`                   The next node at the same level (or `null`).
:attr:`~.Previous`               The previous node at the same level.
:attr:`~.Root`                   The top node of the DOM, which hence has the tagname :htmlTag:`html`.
================================ ===========================================================================================



**Class API**

.. class:: Xml

    .. method:: AddBulletList

       Add an :htmlTag:`ul` tag - bulleted list, context manager. See `ul <https://developer.mozilla.org/en-US/docs/Web/HTML/Element/ul>`_.

    .. method:: AddCodeBlock

       Add a :htmlTag:`pre` tag, context manager. See `pre <https://developer.mozilla.org/en-US/docs/Web/HTML/Element/pre>`_.

    .. method:: AddDescriptionList

       Add a :htmlTag:`dl` tag, context manager. See `dl <https://developer.mozilla.org/en-US/docs/Web/HTML/Element/dl>`_.

    .. method:: AddDivision

       Add a :htmlTag:`div` tag, context manager. See `div <https://developer.mozilla.org/en-US/docs/Web/HTML/Element/div>`_.

    .. method:: AddHeader(int level = 1)

       Add a header tag (one of :htmlTag:`h1` to :htmlTag:`h6`), context manager. See `headings <https://developer.mozilla.org/en-US/docs/Web/HTML/Element/Heading_Elements>`_.

       :arg int value: a value 1 - 6.

    .. method:: AddHorizontalLine

       Add a :htmlTag:`hr` tag. See `hr <https://developer.mozilla.org/en-US/docs/Web/HTML/Element/hr>`_.

    .. method:: AddImage(string name, string width=null, string height=null, string imgFloat = null, string align = null)

       Add an :htmlTag:`img` tag. This causes the inclusion of the named image in the DOM.

       :arg str name: the filename of the image. This **must be the member name** of some entry of the :ref:`Archive` parameter of the :ref:`Story` constructor.
       :arg width: if provided, either an absolute (int) value, or a percentage string like "30%". A percentage value refers to the width of the specified `where` rectangle in :meth:`Story.place`. If this value is provided and `height` is omitted, the image will be included keeping its aspect ratio.
       :arg height: if provided, either an absolute (int) value, or a percentage string like "30%". A percentage value refers to the height of the specified `where` rectangle in :meth:`Story.place`. If this value is provided and `width` is omitted, the image's aspect ratio will be honored.

    .. method:: AddLink(string href, string text=null)

       Add an :htmlTag:`a` tag - inline element, treated like text.

       :arg str href: the URL target.
       :arg str text: the text to display. If omitted, the `href` text is shown instead.

    .. method:: AddNumberList

       Add an :htmlTag:`ol` tag, context manager.

    .. method:: AddParagraph

       Add a :htmlTag:`p` tag, context manager.

    .. method:: AddSpan

       Add a :htmlTag:`span` tag, context manager. See `span`_

    .. method:: AddSubscript(string text)

       Add "subscript" text(:htmlTag:`sub` tag) - inline element, treated like text.

    .. method:: AddSuperscript(string text)

       Add "superscript" text (:htmlTag:`sup` tag) - inline element, treated like text.

    .. method:: AddCode(string text)

       Add "code" text (:htmlTag:`code` tag) - inline element, treated like text.

    .. method:: AddVar(string text)

       Add "variable" text (:htmlTag:`var` tag) - inline element, treated like text.

    .. method:: AddSamp(string text)

       Add "sample output" text (:htmlTag:`samp` tag) - inline element, treated like text.

    .. method:: AddKbd(string text)

       Add "keyboard input" text (:htmlTag:`kbd` tag) - inline element, treated like text.

    .. method:: AddText(string text)

       Add a text string. Line breaks `\n` are honored as :htmlTag:`br` tags.

    .. method:: SetAlign(dynamic value)

       Set the text alignment. Only works for block-level tags.

       :arg value: either one of the :ref:`TextAlign` or the `text-align <https://developer.mozilla.org/en-US/docs/Web/CSS/text-align>`_ values.

    .. method:: SetAttribute(string key, string value=null)

       Set an arbitrary key to some value (which may be empty).

       :arg str key: the name of the attribute.
       :arg str value: the (optional) value of the attribute.

    .. method:: GetAttributes()

       Retrieve all attributes of the current nodes as a dictionary.

       :returns: a dictionary with the attributes and their values of the node.

    .. method:: GetAttributeValue(string key)

       Get the attribute value of `key`.

       :arg str key: the name of the attribute.

       :returns: a string with the value of `key`.

    .. method:: RemoveAttribute(string key)

       Remove the attribute `key` from the node.

       :arg str key: the name of the attribute.

    .. method:: SetBgColor(int value)
    .. method:: SetBgColor(float[] value)
    .. method:: SetBgColor(string value)

       Set the background color. Only works for block-level tags.

       :arg value: either an RGB value like (255, 0, 0) (for "red") or a valid `background-color <https://developer.mozilla.org/en-US/docs/Web/CSS/background-color>`_ value.

    .. method:: SetBold(bool value)

       Set bold on or off or to some string value.

       :arg value: `true`, `false` or a valid `font-weight <https://developer.mozilla.org/en-US/docs/Web/CSS/font-weight>`_ value.

    .. method:: SetColor(dynamic value)

       Set the color of the text following.

       :arg value: either an RGB value like (255, 0, 0) (for "red") or a valid `color <https://developer.mozilla.org/en-US/docs/Web/CSS/color_value>`_ value.

    .. method:: SetColumns(value)

       Set the number of columns.

       :arg value: a valid `columns <https://developer.mozilla.org/en-US/docs/Web/CSS/columns>`_ value.

       .. note:: Currently ignored - supported in a future MuPDF version.

    .. method:: SetFont(int value)

       Set the font-family.

       :arg str value: e.g. "sans-serif".

    .. method:: SetFontSize(float value)

       Set the font size for text following.

       :arg value: a float or a valid `font-size <https://developer.mozilla.org/en-US/docs/Web/CSS/font-size>`_ value.

    .. method:: SetId(string unqid)

       Set a :htmlTag:`id`. This serves as a unique identification of the node within the DOM. Use it to easily locate the node to inspect or modify it. A check for uniqueness is performed.

       :arg str unqid: id string of the node.

    .. method:: SetItalic(bool value)

       Set italic on or off or to some string value for the text following it.

       :arg value: `true`, `false` or some valid `font-style <https://developer.mozilla.org/en-US/docs/Web/CSS/font-style>`_ value.

    .. method:: SetLeading(string leading)

       Set inter-block text distance (`-mupdf-leading`), only works on block-level nodes.

       :arg float leading: the distance in points to the previous block.

    .. method:: SetLineHeight(string value)

       Set height of a line.

       :arg value:  a float like 1.5 (which sets to `1.5 * fontsize`), or some valid `line-height <https://developer.mozilla.org/en-US/docs/Web/CSS/line-height>`_ value.

    .. method:: SetMargins(string value)

       Set the margin(s).

       :arg value: float or string with up to 4 values. See `CSS documentation <https://developer.mozilla.org/en-US/docs/Web/CSS/margin>`_.

    .. method:: SetPageBreakAfter

       Insert a page break after this node.

    .. method:: SetPageBreakBefore

       Insert a page break before this node.

    .. method:: SetProperties(string align=null, string bgcolor=null, bool bold=false, string color=null, int columns=0, string font=null, int fontsize=10, string indent=null, bool italic=false, string leading=null, string lineheight=null, string margins=null, string pageBreakAfter=false, string pageBreakBefore=false, string wordSpacing = null, string unqid=null, string cls=null)

       Set any or all desired properties in one call. The meaning of argument values equal the values of the corresponding `set_` methods.

       .. note:: The properties set by this method are directly attached to the node, whereas every `set_` method generates a new :htmlTag:`span` below the current node that has the respective property. So to e.g. "globally" set some property for the :htmlTag:`body`, this method must be used.

    .. method:: AddStyle(string value)

       Set (add) some style attribute not supported by its own `set_` method.

       :arg str value: any valid CSS style value.

    .. method:: AddClass(string value)

       Set (add) some "class" attribute.

       :arg str value: the name of the class. Must have been defined in either the HTML or the CSS source of the DOM.

    .. method:: SetTextIndent(string value)

       Set indentation for the first textblock line. Only works for block-level nodes.

       :arg value: a valid `text-indent <https://developer.mozilla.org/en-US/docs/Web/CSS/text-indent>`_ value. Please note that negative values do not work.


    .. method:: AppendChild(MuPDFXml node)

       Append a child node. This is a low-level method used by other methods like :meth:`Xml.AddParagraph`.

       :arg node: the :ref:`Xml` node to append.

    .. method:: CreateTextNode(string text)

       Create direct text for the current node.

       :arg str text: the text to append.

       :rtype: :ref:`Xml`
       :returns: the created element.

    .. method:: CreateElement(string tag)

       Create a new node with a given tag. This a low-level method used by other methods like :meth:`Xml.AddParagraph`.

       :arg str tag: the element tag.

       :rtype: :ref:`Xml`
       :returns: the created element. To actually bind it to the DOM, use :meth:`Xml.AppendChild`.

    .. method:: InsertBefore(MuPDFXml elem)

       Insert the given element `elem` before this node.

       :arg elem: some :ref:`Xml` element.

    .. method:: InsertAfter(MuPDFXml elem)

       Insert the given element `elem` after this node.

       :arg elem: some :ref:`Xml` element.

    .. method:: Clone()

       Make a copy of this node, which then may be appended (using :meth:`Xml.AppendChild`) or inserted (using one of :meth:`Xml.InsertBefore`, :meth:`Xml.InsertAfter`) in this DOM.

       :returns: the clone (:ref:`Xml`) of the current node.

    .. method:: Remove()

       Remove this node from the DOM.


    .. method:: Debug()

       For debugging purposes, print this node's structure in a simplified form.

    .. method:: Find(string tag, string att, string match)

       Under the current node, find the first node with the given `tag`, attribute `att` and value `match`.

       :arg str tag: restrict search to this tag. May be `null` for unrestricted searches.
       :arg str att: check this attribute. May be `null`.
       :arg str match: the desired attribute value to match. May be `null`.

       :rtype: :ref:`Xml`.
       :returns: `null` if nothing found, otherwise the first matching node.

    .. method:: FindNext( string tag, string att, string match)

       Continue a previous :meth:`Xml.Find` (or :meth:`FindNext`) with the same values.

       :rtype: :ref:`Xml`.
       :returns: `null` if none more found, otherwise the next matching node.


    .. attribute:: TagName

       Either the HTML tag name like :htmlTag:`p` or `null` if a text node.

    .. attribute:: Text

       Either the node's text or `null` if a tag node.

    .. attribute:: IsText

       Check if a text node.

    .. attribute:: FirstChild

       Contains the first node one level below this one (or `null`).

    .. attribute:: LastChild

       Contains the last node one level below this one (or `null`).

    .. attribute:: Next

       The next node at the same level (or `null`).

    .. attribute:: Previous

       The previous node at the same level.

    .. attribute:: Root

       The top node of the DOM, which hence has the tagname :htmlTag:`html`.


Setting Text properties
------------------------

In HTML tags can be nested such that innermost text **inherits properties** from the tag enveloping its parent tag. For example `<p><b>some bold text<i>this is bold and italic</i></b>regular text</p>`.

To achieve the same effect, methods like :meth:`Xml.SetBold` and :meth:`Xml.SetItalic` each open a temporary :htmlTag:`span` with the desired property underneath the current node.

In addition, these methods return there parent node, so they can be concatenated with each other.



Context Manager support
------------------------
The standard way to add nodes to a DOM is this:


.. code-block:: c

   Xml body = story.Body;
   Xml para = body.AddParagraph();  // add a paragraph
   para.SetBold();  // text that follows will be bold
   para.AddText("some bold text");
   para.SetItalic();  // text that follows will additionally be italic
   para.add_txt("this is bold and italic");
   para.SetItalic(false).SetBold(false);  // all following text will be regular
   para.AddText("regular text");



Methods that are flagged as "context managers" can conveniently be used in this way:

.. code-block:: c

   Xml body = story.Body;
   Xml para = body.AddParagraph();
   para.SetBold().AddText("some bold text");
   para.SetItalic().AddText("this is bold and italic");
   para.SetItalic(false).SetBold(false).AddText("regular text");
   para.AddText("more regular text");

.. include:: footer.rst

.. External links:

.. _span: https://developer.mozilla.org/en-US/docs/Web/HTML/Element/span
