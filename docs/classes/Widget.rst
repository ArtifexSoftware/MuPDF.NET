.. include:: ../header.rst

.. _Widget:

================
Widget
================


|pdf_only_class|

This class represents a PDF Form field, also called a "widget". Throughout this documentation, we are using these terms synonymously. Fields technically are a special case of PDF annotations, which allow users with limited permissions to enter information in a PDF. This is primarily used for filling out forms.

Like annotations, widgets live on PDF pages. Similar to annotations, the first widget on a page is accessible via :attr:`Page.FirstWidget` and subsequent widgets can be accessed via the :attr:`Widget.Next` property.

MuPDF no longer treats widgets as a subset of general annotations. Consequently, :attr:`Page.FirstAnnot` and :meth:`Annot.Next` will deliver **non-widget annotations exclusively**, and be *None* if only form fields exist on a page. Vice versa, :attr:`Page.FirstWidget` and :meth:`Widget.Next` will only show widgets. This design decision is purely internal to MuPDF; technically, links, annotations and fields have a lot in common and also continue to share the better part of their code within MuPDF.


**Class API**

.. class:: Widget

    .. method:: ButtonStates

       Return the names of On / Off (i.e. selected / clicked or not) states a button field may have. While the 'Off' state usually is also named like so, the 'On' state is often given a name relating to the functional context, for example 'Yes', 'Female', etc.

       This method helps finding out the possible values of :attr:`FieldValue` in these cases.

       :returns: a dictionary with the names of 'On' and 'Off' for the *normal* and the *pressed-down* appearance of button widgets. The following example shows that the "selected" value is "Male":

         Console.WriteLine(field.FieldName, field.ButtonStates())
         Gender Second person {'down': ['Male', 'Off'], 'normal': ['Male', 'Off']}


    .. method:: OnState

       Return the value of the "ON" state of check boxes and radio buttons. For check boxes this is always the value "Yes". For radio buttons, this is the value to select / activate the button.

       :returns: the value that sets the button to "selected". For non-checkbox, non-radiobutton fields, always `None` is returned. For check boxes the return is `true`. For radio buttons this is the value "Male" in the following example:

         >>> Console.WriteLine(field., field.ButtonStates())
         Gender Second person {'down': ['Male', 'Off'], 'normal': ['Male', 'Off']}
         >>> Console.WriteLine(field.OnState())
         Male

        So for check boxes and radio buttons, the recommended method to set them to "selected", or to check the state is the following:
        
         >>> field.FieldValue = field.OnState()
         >>> field.FieldValue == field.OnState()
         true


    .. method:: Update

       After any changes to a widget, this method **must be used** to store them in the PDF [#f1]_.

    .. method:: Reset

       Reset the field's value to its default -- if defined -- or remove it. Do not forget to issue :meth:`Update` afterwards.

    .. attribute:: Next

       Point to the next form field on the page. The last widget returns *None*.

    .. attribute:: BorderColor

       A list of up to 4 floats defining the field's border color. Default value is *None* which causes border style and border width to be ignored.

    .. attribute:: BorderStyle

       A string defining the line style of the field's border. See :attr:`Annot.Border`. Default is "s" ("Solid") -- a continuous line. Only the first character (upper or lower case) will be regarded when creating a widget.

    .. attribute:: BorderWidth

       A float defining the width of the border line. Default is 1.

    .. attribute:: BorderDashes

       A list/tuple of integers defining the dash properties of the border line. This is only meaningful if `BorderStyle` equals "D" and :attr:`BorderColor` is provided.

    .. attribute:: ChoiceValues

       Sequence of strings defining the valid choices of list boxes and combo boxes. For these widget types, this property is mandatory and must contain at least two items. Ignored for other types.

    .. attribute:: FieldName

       A mandatory string defining the field's name. No checking for duplicates takes place.

    .. attribute:: FieldLabel

       An optional string containing an "alternate" field name. Typically used for any notes, help on field usage, etc. Default is the field name.

    .. attribute:: FieldValue

       The value of the field.

    .. attribute:: FieldFlags

       An integer defining a large amount of properties of a field. Be careful when changing this attribute as this may change the field type.

    .. attribute:: FieldType

       A mandatory integer defining the field type. This is a value in the range of 0 to 6. It cannot be changed when updating the widget.

    .. attribute:: FieldTypeString

       A string describing (and derived from) the field type.

    .. attribute:: FillColor

       A list of up to 4 floats defining the field's background color.

    .. attribute:: ButtonCaption

       The caption string of a button-type field.

    .. attribute:: IsSigned

       A bool indicating the signing status of a signature field, else *None*.

    .. attribute:: Rect

       The rectangle containing the field.

    .. attribute:: TextColor

       A list of **1, 3 or 4 floats** defining the text color. Default value is black (`[0, 0, 0]`).

    .. attribute:: TextFont

       A string defining the font to be used.

    .. attribute:: TextFontSize

       A float defining the text :data:`fontSize`. Default value is zero, which causes PDF viewer software to dynamically choose a size suitable for the annotation's rectangle and text amount.

    .. attribute:: TextMaxLen

       An integer defining the maximum number of text characters. PDF viewers will (should) not accept a longer text.

    .. attribute:: TextFormat

    .. attribute:: Xref

       The PDF :data:`xref` of the widget.

    .. attribute:: Script
       
       JavaScript text (unicode) for an action associated with the widget, or *None*. This is the only script action supported for **button type** widgets.

    .. attribute:: ScriptStroke
       
       JavaScript text (unicode) to be performed when the user types a key-stroke into a text field or combo box or modifies the selection in a scrollable list box. This action can check the keystroke for validity and reject or modify it. *None* if not present.

    .. attribute:: ScriptFormat
       
       JavaScript text (unicode) to be performed before the field is formatted to display its current value. This action can modify the field's value before formatting. *None* if not present.

    .. attribute:: ScriptChange

       JavaScript text (unicode) to be performed when the field's value is changed. This action can check the new value for validity. *None* if not present.

    .. attribute:: ScriptCalc

       JavaScript text (unicode) to be performed to recalculate the value of this field when that of another field changes. *None* if not present.

    .. attribute:: ScriptBlur
       
       JavaScript text (unicode) to be performed on losing the focus of this field. *None* if not present.

    .. attribute:: ScriptFocus
       
       JavaScript text (unicode) to be performed on focusing this field. *None* if not present.

    .. note::

       1. For **adding** or **changing** one of the above scripts,
         just put the appropriate JavaScript source code in the widget attribute.
         To **remove** a script, set the respective attribute to *None*.

       2. Button fields only support :attr:`Script`.
         Other script entries will automatically be set to *None*.

       3. It is worthwhile to look at
          `this <https://experienceleague.adobe.com/docs/experience-manager-learn/assets/FormsAPIReference.pdf?lang=en>`_
          manual with lots of information about Adobe's standard scripts for various field types.
          For example, if you want to add a text field representing a date,
          you may want to store the following scripts.
          They will ensure pattern-compatible date formats and display date pickers in supporting viewers::

              widget.ScriptFormat = 'AFDate_FormatEx("mm/dd/yyyy");'
              widget.ScriptStroke = 'AFDate_KeystrokeEx("mm/dd/yyyy");'


Standard Fonts for Widgets
----------------------------------
Widgets use their own resources object */DR*. A widget resources object must at least contain a */Font* object. Widget fonts are independent from page fonts. We currently support the 14 PDF base fonts using the following fixed reference names, or any name of an already existing field font. When specifying a text font for new or changed widgets, **either** choose one in the first table column (upper and lower case supported), **or** one of the already existing form fonts. In the latter case, spelling must exactly match.

To find out already existing field fonts, inspect the list :attr:`Document.FormFonts`.

============= =======================
**Reference** **Base14 Fontname**
============= =======================
CoBI          Courier-BoldOblique
CoBo          Courier-Bold
CoIt          Courier-Oblique
Cour          Courier
HeBI          Helvetica-BoldOblique
HeBo          Helvetica-Bold
HeIt          Helvetica-Oblique
Helv          Helvetica **(default)**
Symb          Symbol
TiBI          Times-BoldItalic
TiBo          Times-Bold
TiIt          Times-Italic
TiRo          Times-Roman
ZaDb          ZapfDingbats
============= =======================

You are generally free to use any font for every widget. However, we recommend using *ZaDb* ("ZapfDingbats") and :data:`fontSize` 0 for check boxes: typical viewers will put a correctly sized tickmark in the field's rectangle, when it is clicked.

Supported Widget Types
-----------------------
MuPDF.NET supports the creation and update of many, but not all widget types.

* text (`PDF_WIDGET_TYPE_TEXT`)
* push button (`PDF_WIDGET_TYPE_BUTTON`)
* check box (`PDF_WIDGET_TYPE_CHECKBOX`)
* combo box (`PDF_WIDGET_TYPE_COMBOBOX`)
* list box (`PDF_WIDGET_TYPE_LISTBOX`)
* radio button (`PDF_WIDGET_TYPE_RADIOBUTTON`): MuPDF.NET does not currently support the **creation** of groups of (interconnected) radio buttons, where setting one automatically unsets the other buttons in the group. The widget object also does not reflect the presence of a button group. However: consistently selecting (or unselecting) a radio button is supported. This includes correctly setting the value maintained in the owning button group. Selecting a radio button may be done by either assigning `true` or `field.OnState()` to the field value. **De-selecting** the button should be done assigning `false`.
* signature (`PDF_WIDGET_TYPE_SIGNATURE`) **read only**.

.. rubric:: Footnotes

.. [#f1] If you intend to re-access a new or updated field (e.g. for making a pixmap), make sure to reload the page first. Either close and re-open the document, or load another page first, or simply do `page = doc.ReloadPage(page)`.

.. include:: ../footer.rst
