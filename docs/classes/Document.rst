.. include:: ../header.rst

.. _Document:

================
Document
================



This class represents a document. It can be constructed from a file or from memory.


=============================================== ==========================================================
**Method / Attribute**                          **Short Description**
=============================================== ==========================================================
:meth:`Document.AddLayer`                       PDF only: make new optional content configuration
:meth:`Document.AddOcg`                         PDF only: add new optional content group
:meth:`Document.Authenticate`                   Gain access to an encrypted document
:meth:`Document.Bake`                           PDF only: make annotations / fields permanent content
:meth:`Document.CanSaveIncrementally`           Check if incremental save is possible
:meth:`Document.GetChapterPageCount`            Number of pages in chapter
:meth:`Document.Close`                          Close the document
:meth:`Document.Convert2Pdf`                    Write a PDF version to memory
:meth:`Document.CopyPage`                       PDF only: copy a page reference
:meth:`Document.DeleteTocItem`                  PDF only: remove a single TOC item
:meth:`Document.DeletePage`                     PDF only: delete a page
:meth:`Document.DeletePages`                    PDF only: delete multiple pages
:meth:`Document.AddEmbfile`                     PDF only: add a new embedded file from buffer
:meth:`Document.GetEmbfileCount`                PDF only: get an embedded file from buffer
:meth:`Document.DeleteEmbfile`                  PDF only: delete an embedded file entry
:meth:`Document.GetEmbfile`                     PDF only: extract an embedded file buffer
:meth:`Document.GetEmbfileInfo`                 PDF only: metadata of an embedded file
:meth:`Document.GetEmbfileNames`                PDF only: list of embedded files
:meth:`Document.GetEmbfileUpd`                  PDF only: change an embedded file
:meth:`Document.GetNewXref`                     Make new `xref`
:meth:`Document.GetXrefLength`                  Get length of `xref` table
:meth:`Document.GetCharWidths`                  PDF only: return a list of glyph widths of a font
:meth:`Document.ExtractFont`                    PDF only: extract a font by `xref`
:meth:`Document.ExtractImage`                   PDF only: extract an embedded image by `xref`       
:meth:`Document.FindBookmark`                   Retrieve page location after laid out document
:meth:`Document.CopyFullPage`                   PDF only: duplicate a page
:meth:`Document.GetLayer`                       PDF only: lists of OCGs in ON, OFF, RBGroups
:meth:`Document.GetLayers`                      PDF only: list of optional content configurations
:meth:`Document.GetOC`                          PDF only: get OCG / OCMD `xref` of image / form xobject
:meth:`Document.GetOcgs`                        PDF only: info on all optional content groups
:meth:`Document.GetOCMD`                        PDF only: retrieve definition of an `OCMD`
:meth:`Document.GetOutlineXrefs`                Get list of outline xref numbers
:meth:`Document.GetPageFonts`                   PDF only: list of fonts referenced by a page
:meth:`Document.GetPageImages`                  PDF only: list of images referenced by a page
:meth:`Document.GetPageLabels`                  PDF only: list of page label definitions
:meth:`Document.GetPageNumbers`                 PDF only: get page numbers having a given label
:meth:`Document.GetPagePixmap`                  Create a pixmap of a page by page number
:meth:`Document.GetPageText`                    Extract the text of a page by page number
:meth:`Document.GetPageXObjects`                PDF only: list of XObjects referenced by a page
:meth:`Document.GetSigFlags`                    PDF only: determine signature state
:meth:`Document.GetToc`                         Extract the table of contents
:meth:`Document.GetXmlMetadata`                 PDF only: read the XML metadata
:meth:`Document.DeleteXmlMetadata`              PDF only: remove XML metadata
:meth:`Document.HasAnnots`                      PDF only: check if PDF contains any annots
:meth:`Document.HasLinks`                       PDF only: check if PDF contains any links
:meth:`Document.InsertPage`                     PDF only: insert a new page
:meth:`Document.InsertPdf`                      PDF only: insert pages from another PDF
:meth:`Document.InsertFile`                     PDF only: insert pages from arbitrary document
:meth:`Document.JournalCanDo`                   PDF only: which journal actions are possible
:meth:`Document.JournalEnable`                  PDF only: enables journalling for the document
:meth:`Document.IsEnabledJournal`               Check if journalling is enabled
:meth:`Document.JournalLoad`                    PDF only: enables journalling for the document
:meth:`Document.JournalOpName`                  PDF only: return name of a journalling step
:meth:`Document.JournalPosition`                PDF only: return journalling status
:meth:`Document.JournalRedo`                    PDF only: redo current operation
:meth:`Document.JournalSave`                    PDF only: save journal to a file
:meth:`Document.JournalStartOp`                 PDF only: start an “operation” giving it a name
:meth:`Document.JournalStopOp`                  PDF only: end current operation
:meth:`Document.JournalUndo`                    PDF only: undo current operation
:meth:`Document.LayerUIConfigs`                 PDF only: list of optional content intents
:meth:`Document.SetLayout`                      Re-paginate the document (if supported)
:meth:`Document.SetLanguage`                    Set language       
:meth:`Document.LoadPage`                       Load a page
:meth:`Document.MakeBookmark`                   Create a page pointer in reflowable documents
:meth:`Document.MovePage`                       PDF only: move a page to different location in doc
:meth:`Document.NeedAppearances`                PDF only: get/set /NeedAppearances property
:meth:`Document.NewPage`                        PDF only: insert a new empty page
:meth:`Document.NextLocation`                   Return (chapter, pno) of following page
:meth:`Document.PageCropBox`                    PDF only: the unrotated page rectangle
:meth:`Document.PageXref`                       PDF only: `xref` of a page number
:meth:`Document.GetPages`                       Iterator over a page range
:meth:`Document.GetPdfCatelog`                  PDF only: `xref` of catalog (root)
:meth:`Document.GetPdfTrailer`                  PDF only: trailer source
:meth:`Document.PrevLocation`                   Return (chapter, pno) of preceding page
:meth:`Document.GetLocationFromPageNumber`      Convert pno to (chapter, page)
:meth:`Document.GetPageNumberFromLocation`      Convert (chapter, pno) to page number
:meth:`Document.GetMetadata`                    Get metadata
:meth:`Document.GetPageXref`                    Get `xref` of page number
:meth:`Document.LoadOutline`                    Load first outline
:meth:`Document.ReloadPage`                     PDF only: provide a new copy of a page
:meth:`Document.Recolor`                        PDF only: recolor pages of document
:meth:`Document.ForgetPage`                     Remove a page from document page dict
:meth:`Document.ResolveNames`                   PDF only: Convert destination names into a Dictionary 
:meth:`Document.ExtendTocItems`                 Add color info to all items of an extended TOC list
:meth:`Document.Save`                           PDF only: save the document
:meth:`Document.SaveIncremental`                PDF only: save the document incrementally
:meth:`Document.Scrub`                          PDF only: remove sensitive data
:meth:`Document.SearchPageFor`                  Search for a string on a page
:meth:`Document.Select`                         PDF only: select a subset of pages
:meth:`Document.SetLayerUIConfig`               PDF only: set OCG visibility temporarily
:meth:`Document.SetLayer`                       PDF only: mass changing OCG states
:meth:`Document.SetMarkInfo`                    PDF only: set the MarkInfo values
:meth:`Document.SetMetadata`                    PDF only: set the metadata
:meth:`Document.SetOC`                          PDF only: attach OCG/OCMD to image / form xobject
:meth:`Document.SetOCMD`                        PDF only: create or update an `OCMD`
:meth:`Document.SetPageLabels`                  PDF only: add/update page label definitions
:meth:`Document.SetPageMode`                    PDF only: set the PageMode
:meth:`Document.SetPageLayout`                  PDF only: set the PageLayout
:meth:`Document.SetToc`                         PDF only: set the table of contents (TOC)
:meth:`Document.SetTocItem`                     Update TOC item by index
:meth:`Document.SetXmlMetaData`                 PDF only: create or update document XML metadata
:meth:`Document.SubsetFonts`                    PDF only: create font subsets
:meth:`Document.SwitchLayer`                    PDF only: activate OC configuration
:meth:`Document.Write`                          Write document
:meth:`Document.CopyXref`                       PDF only: copy a PDF dictionary to another `xref`
:meth:`Document.GetKeyXref`                     PDF only: get the value of a dictionary key
:meth:`Document.GetKeysXref`                    PDF only: list the keys of object at `xref`
:meth:`Document.GetXrefObject`                  PDF only: get the definition source of `xref`
:meth:`Document.XrefIsFont`                     Check if `xref` is a font object
:meth:`Document.XrefIsImage`                    Check if `xref` is a image object
:meth:`Document.XrefIsStream`                   Check if `xref` is a stream object
:meth:`Document.XrefIsXObject`                  Check if `xref` is a form xobject
:meth:`Document.SetKeyXRef`                     PDF only: set the value of a dictionary key
:meth:`Document.GetXrefStream`                  Get decompressed `xref` stream
:meth:`Document.GetXrefStreamRaw`               PDF only: raw stream source at `xref`
:meth:`Document.XrefXmlMetaData`                PDF only: return XML metadata :data:`xref` number
:meth:`Document.UpdateTocItem`                  Update bookmark by letting it point to nowhere
:meth:`Document.UpdateObject`                   PDF only: Replace object definition of :data:`xref` with the provided string
:meth:`Document.UpdateStream`                   Replace the stream of an object identified by *xref*, which must be a PDF dictionary
:meth:`Document.IsStream`                       PDF only: check whether an :data:`xref` is a stream object
:attr:`Document.ChapterCount`                   Number of chapters
:attr:`Document.FormFonts`                      Number of chapters
:attr:`Document.IsClosed`                       Has document been closed?
:attr:`Document.IsDirty`                        PDF only: has document been changed yet?
:attr:`Document.IsEncrypted`                    Document (still) encrypted?
:attr:`Document.IsFastWebaccess`                Is PDF linearized?
:attr:`Document.IsFormPDF`                      Is this a Form PDF?
:attr:`Document.IsPDF`                          Is this a PDF?
:attr:`Document.IsReflowable`                   Is this a reflowable document?
:attr:`Document.IsRepaired`                     PDF only: has this PDF been repaired during open?
:attr:`Document.Language`                       Document language
:attr:`Document.LastLocation`                   (chapter, pno) of last page
:attr:`Document.MetaData`                       Metadata
:attr:`Document.MarkInfo`                       PDF MarkInfo value
:attr:`Document.Name`                           Filename of document
:attr:`Document.NeedsPass`                      Require password to access data?
:attr:`Document.Outline`                        First Outline item
:attr:`Document.PageCount`                      Number of pages
:attr:`Document.Permissions`                    Permissions to access the document
:attr:`Document.PageMode`                       PDF PageMode value
:attr:`Document.PageLayout`                     PDF PageMode value
:attr:`Document.VersionCount`                   PDF count of versions
=============================================== ==========================================================

**Class API**

.. class:: Document

  .. index::
    pair: filename; open
    pair: stream; open
    pair: filetype; open
    pair: rect; open
    pair: width; open
    pair: height; open
    pair: fontSize; open
    pair: open; Document
    pair: filename; Document
    pair: stream; Document
    pair: filetype; Document
    pair: rect; Document
    pair: fontSize; Document

  .. method:: Document(string filename: null, byte[] stream: null, filetype: null, rect: null, width: 0, height: 0, fontSize: 11)

    Creates a *Document* object.

    * With default parameters, a **new empty PDF** document will be created.
    * If *stream* is given, then the document is created from memory and, if not a PDF, either *filename* or *filetype* must indicate its type.
    * If *stream* is `null`, then a document is created from the file given by *filename*. Its type is inferred from the extension. This can be overruled by *filetype.*

    :arg string filename: A UTF-8 string containing a file path. The document type is inferred from the filename extension. If not present or not matching :ref:`a supported type<Supported_File_Types>`, a PDF document is assumed. For memory documents, this argument may be used instead of `filetype`, see below.

    :arg byte[] stream: A memory area containing a supported document. If not a PDF, its type **must** be specified by either `filename` or `filetype`.

    :arg string filetype: A string specifying the type of document. This may be anything looking like a filename (e.g. "x.pdf"), in which case MuPDF uses the extension to determine the type, or a mime type like *application/pdf*. Just using strings like "pdf"  or ".pdf" will also work. May be omitted for PDF documents, otherwise must match :ref:`a supported document type<Supported_File_Types>`.

    :arg Rect rect: a rectangle specifying the desired page size. This parameter is only meaningful for documents with a variable page layout ("reflowable" documents), like e-books or HTML, and ignored otherwise. If specified, it must be a non-empty, finite rectangle with top-left coordinates (0, 0). Together with parameter *fontSize*, each page will be accordingly laid out and hence also determine the number of pages.

    :arg float width: may used together with *height* as an alternative to *rect* to specify layout information.

    :arg float height: may used together with *width* as an alternative to *rect* to specify layout information.

    :arg float fontSize: the default :data:`fontSize` for reflowable document types. This parameter is ignored if none of the parameters *rect* or *width* and *height* are specified. Will be used to calculate the page layout.

    :return: A document object. If the document cannot be created, an exception is raised.

    .. note:: Not all document types are checked for valid formats already at open time. Raster images for example will raise exceptions only later, when trying to access the content. Other types (notably with non-binary content) may also be opened (and sometimes **accessed**) successfully -- sometimes even when having invalid content for the format:

      * HTM, HTML, XHTML: **always** opened, `metadata["format"]` is "HTML5", resp. "XHTML".
      * XML, FB2: **always** opened, `metadata["format"]` is "FictionBook2".

    Overview of possible forms, note: `open` is a synonym of `Document`:

    .. code-block:: cs

        // from a file
        Document doc = new Document("some.pdf");
        // handle wrong extension
        doc = new Document("some.file", filetype: "xps")

        // from memory, filetype is required if not a PDF
        doc = new Document(filetype: "xps", mem_area)
        doc = new Document(null, stream: mem_area, filetype: "xps")
        doc = new Document(stream: mem_area, filetype: "xps")

        // new empty PDF
        doc = new Document()
        doc = new Document("")

    .. note:: Raster images with a wrong (but supported) file extension **are no problem**. MuPDF will determine the correct image type when file **content** is actually accessed and will process it without complaint. So `new Document("file.jpg")` will work even for a PNG image.


  .. method:: GetOC(int xref)

    Return the cross reference number of an :data:`OCG` or :data:`OCMD` attached to an image or form XObject.

    :arg int xref: the :data:`xref` of an image or form XObject. Valid such cross reference numbers are returned by :meth:`Document.GetPageImages`, resp. :meth:`Document.GetPageXObjects`. For invalid numbers, an exception is raised.
    :rtype: int
    :returns: the cross reference number of an optional contents object or zero if there is none.

  .. method:: SetOC(int xref, int ocxref)

    If `xref` represents an image or form XObject, set or remove the cross reference number `oc` of an optional contents object.

    :arg int xref: the :data:`xref` of an image or form xobject [#f5]_. Valid such cross reference numbers are returned by :meth:`Document.GetPageImages`, resp. :meth:`Document.GetPageXobjects`. For invalid numbers, an exception is raised.
    :arg int ocxref: the :data:`xref` number of an :data:`OCG` / :data:`OCMD`. If not zero, an invalid reference raises an exception. If zero, any OC reference is removed.


  .. method:: GetLayers()

    Show optional layer configurations. There always is a standard one, which is not included in the response.

  .. method:: AddLayer(string name, string creator: null, OCLayerConfig on: null)

    Add an optional content configuration. Layers serve as a collection of ON / OFF states for optional content groups and allow fast visibility switches between different views on the same document.

    :arg string name: arbitrary name.
    :arg string creator: (optional) creating software.
    :arg OCLayerConfig on: a sequence of OCG :data:`xref` numbers which should be set to ON when this layer gets activated. All OCGs not listed here will be set to OFF.


  .. method:: Write()

    Writes the document to a bytes array.

    :rtype: byte[]


  .. method:: SwitchLayer(int config, int asDefault: 0)

    Switch to a document view as defined by the optional layer's configuration number. This is temporary, except if established as default.

    :arg int number: config number as returned by :meth:`Document.LayerUIConfigs`.
    :arg int asDefault: make this the default configuration.

    Activates the ON / OFF states of OCGs as defined in the identified layer. If *asDefault=1*, then additionally all layers, including the standard one, are merged and the result is written back to the standard layer, and **all optional layers are deleted**.


  .. method:: AddOcg(string name, int config: -1, bool on: true, string intent: null, string usage: null)

    Add an optional content group. An OCG is the most important unit of information to determine object visibility. For a PDF, in order to be regarded as having optional content, at least one OCG must exist.

    :arg string name: arbitrary name. Will show up in supporting PDF viewers.
    :arg int config: layer configuration number. Default -1 is the standard configuration.
    :arg bool on: standard visibility status for objects pointing to this OCG.
    :arg string intent: a string or list of strings declaring the visibility intents. There are two PDF standard values to choose from: "View" and "Design". Default is "View". Correct **spelling is important**.
    :arg string usage: another influencer for OCG visibility. This will become part of the OCG's `/Usage` key. There are two PDF standard values to choose from: "Artwork" and "Technical". Default is "Artwork". Please only change when required.

    :returns: :data:`xref` of the created OCG. Use as entry for `oc` parameter in supporting objects.

    .. note:: Multiple OCGs with identical parameters may be created. This will not cause problems. Garbage option 3 of :meth:`Document.save` will get rid of any duplicates.


  .. method:: SetOCMD(OCMD ocmd: null, int xref: 0, int[] ocgs: null, string policy: null, dynamic[] ve: null)

    Create or update an :data:`OCMD`, **Optional Content Membership Dictionary.**

    :arg OCMD ocmd: :data:`OCMD`
    :arg int xref: :data:`xref` of the OCMD to be updated, or 0 for a new OCMD.
    :arg int[] ocgs: a sequence of :data:`xref` numbers of existing :data:`OCG` PDF objects.
    :arg string policy: one of "AnyOn" (default), "AnyOff", "AllOn", "AllOff" (mixed or lower case).
    :arg dynamic[] ve: a "visibility expression". This is a list of arbitrarily nested other lists -- see explanation below. Use as an alternative to the combination *ocgs* / *policy* if you need to formulate more complex conditions.

    :rtype: int
    :returns: :data:`xref` of the OCMD. Use as `oc=xref` parameter in supporting objects, and respectively in :meth:`Document.SetOC` or :meth:`Annot.SetOC`.

    .. note::

      Like an OCG, an OCMD has a visibility state ON or OFF, and it can be used like an OCG. In contrast to an OCG, the OCMD state is determined by evaluating the state of one or more OCGs via special forms of **boolean expressions.** If the expression evaluates to true, the OCMD state is ON and OFF for false.

      There are two ways to formulate OCMD visibility:

      1. Use the combination of *ocgs* and *policy*: The *policy* value is interpreted as follows:

        - AnyOn -- (default) true if at least one OCG is ON.
        - AnyOff -- true if at least one OCG is OFF.
        - AllOn -- true if all OCGs are ON.
        - AllOff -- true if all OCGs are OFF.

        Suppose you want two PDF objects be displayed exactly one at a time (if one is ON, then the other one must be OFF):

        Solution: use an **OCG** for object 1 and an **OCMD** for object 2. Create the OCMD via `SetOmd(ocgs: [xref], policy: "AllOff")`, with the :data:`xref` of the OCG.

      2. Use the **visibility expression** *ve*: This is a list of two or more items. The **first item** is a logical keyword: one of the strings **"and"**, **"or"**, or **"not"**. The **second** and all subsequent items must either be an integer or another list. An integer must be the :data:`xref` number of an OCG. A list must again have at least two items starting with one of the boolean keywords. This syntax is a bit awkward, but quite powerful:

        - Each list must start with a logical keyword.
        - If the keyword is a **"not"**, then the list must have exactly two items. If it is **"and"** or **"or"**, any number of other items may follow.
        - Items following the logical keyword may be either integers or again a list. An *integer* must be the xref of an OCG. A *list* must conform to the previous rules.

        **Examples:**

        - `SetOCMD(ve=["or", 4, ["not", 5], ["and", 6, 7]])`. This delivers ON if the following is true: **"4 is ON, or 5 is OFF, or 6 and 7 are both ON"**.
        - `SetOCMD(ve=["not", xref])`. This has the same effect as the OCMD example created under 1.

        For more details and examples see page 224 of :ref:`AdobeManual`.

        Visibility expressions, `/VE`, are part of PDF specification version 1.6. So not all PDF viewers / readers may already support this feature and hence will react in some standard way for those cases.

  .. method:: GetOutlineXrefs()

    Return the `xref` of the outline item. Mainly used for internal purposes.

    :rtype: `List<int>`

  .. method:: GetOCMD(int xref)

    Retrieve the definition of an :data:`OCMD`.

    :arg int xref: the :data:`xref` of the OCMD.
    :rtype: OCMD
    :returns: a OCMD with the keys *xref*, *ocgs*, *policy* and *ve*.


  .. method:: GetLayer(int config: -1)

    List of optional content groups by status in the specified configuration. This is a dictionary with lists of cross reference numbers for OCGs that occur in the arrays `/ON`, `/OFF` or in some radio button group (`/RBGroups`).

    :arg int config: the configuration layer (default is the standard config layer).

  .. method:: SetLayer(int config, string baseState: null, int[] on: null, int[] off: null, List<int[]> rbgroups: null, int[] locked: null)

    Mass status changes of optional content groups. **Permanently** sets the status of OCGs.

    :arg int config: desired configuration layer, choose -1 for the default one.
    :arg int[] on: list of :data:`xref` of OCGs to set ON. Replaces previous values. An empty list will cause no OCG being set to ON anymore. Should be specified if `baseState="ON"` is used.
    :arg int[] off: list of :data:`xref` of OCGs to set OFF. Replaces previous values. An empty list will cause no OCG being set to OFF anymore. Should be specified if `baseState="OFF"` is used.
    :arg string baseState: state of OCGs that are not mentioned in *on* or *off*. Possible values are "ON", "OFF" or "Unchanged". Upper / lower case possible.
    :arg List<int[]> rbgroups: a list of lists. Replaces previous values. Each sublist should contain two or more OCG xrefs. OCGs in the same sublist are handled like buttons in a radio button group: setting one to ON automatically sets all other group members to OFF.
    :arg int[] locked: a list of OCG `xref` number that cannot be changed by the user interface.

    Values `null` will not change the corresponding PDF array.


  .. method:: GetOcgs()

    Details of all optional content groups. This is a dictionary of dictionaries, key is the OCG's :data:`xref`.


  .. method:: LayerUIConfigs()

    Show the visibility status of optional content that is modifiable by the user interface of supporting PDF viewers.

        * Only reports items contained in the currently selected layer configuration.

        * The meaning of the `LayerConfigUI` keys is as follows:
           - *Depth:* item's nesting level in the `/Order` array
           - *IsLocked:* true if cannot be changed via user interfaces
           - *Number:* running sequence number
           - *On:* item state
           - *Text:* text string or name field of the originating OCG
           - *Type:* one of "label" (set by a text string), "checkbox" (set by a single OCG) or "radiobox" (set by a set of connected OCGs)

  .. method:: SetLayerUIConfig(dynamic number, int action: 0)

    Modify OC visibility status of content groups. This is analog to what supporting PDF viewers would offer.

      Please note that visibility is **not** a property stored with the OCG. It is not even information necessarily present in the PDF document at all. Instead, the current visibility is **temporarily** set using the user interface of some supporting PDF consumer software. The same type of functionality is offered by this method.

      To make **permanent** changes, use :meth:`Document.SetLayer`.

    :arg int/string number: either the sequence number of the item in list :meth:`Document.LayerUIConfigs` or the "text" of one of these items.
    :arg int action: `PDF_OC_ON` = set on (default), `PDF_OC_TOGGLE` = toggle on/off, `PDF_OC_OFF` = set off.


  .. method:: Authenticate(string password)

    Decrypts the document with the string *password*. If successful, document data can be accessed. For PDF documents, the "owner" and the "user" have different privileges, and hence different passwords may exist for these authorization levels. The method will automatically establish the appropriate (owner or user) access rights for the provided password.

    :arg string password: owner or user password.

    :rtype: int
    :returns: a positive value if successful, zero otherwise (the string does not match either password). If positive, the indicator :attr:`Document.IsEncrypted` is set to *false*. **Positive** return codes carry the following information detail:

      * 1 => authenticated, but the PDF has neither owner nor user passwords.
      * 2 => authenticated with the **user** password.
      * 4 => authenticated with the **owner** password.
      * 6 => authenticated and both passwords are equal -- probably a rare situation.

      .. note::

        The document may be protected by an owner, but **not** by a user password. Detect this situation via `doc.Authenticate("") == 2`. This allows opening and reading the document without authentication, but, depending on the :attr:`Document.Permissions` value, other actions may be prohibited. MuPDF.NET (like MuPDF) in this case **ignores those restrictions**. So, -- in contrast to any PDF viewers -- you can for example extract text and add or modify content, even if the respective permission flags `PDF_PERM_COPY`, `PDF_PERM_MODIFY`, `PDF_PERM_ANNOTATE`, etc. are set off! It is your responsibility building a legally compliant application where applicable.

  .. method:: GetPageNumbers(string label, bool onlyOne: false)

     PDF only: Return a list of page numbers that have the specified label -- note that labels may not be unique in a PDF. This implies a sequential search through **all page numbers** to compare their labels.

     .. note:: Implementation detail -- pages are **not loaded** for this purpose.

     :arg string label: the label to look for, e.g. "vii" (Roman number 7).
     :arg bool onlyOne: stop after first hit. Useful e.g. if labelling is known to be unique, or there are many pages, etc. The default will check every page number.
     :rtype: list of int
     :returns: list of page numbers that have this label. Empty if none found, no labels defined, etc.


  .. method:: GetPageLabels()

     PDF only: Extract the list of page label definitions. Typically used for modifications before feeding it into :meth:`Document.SetPageLabels`.

     :returns: a list of Label as defined in :meth:`Document.SetPageLabels`.

  .. method:: SetPageLabels(string labels)

     PDF only: Add or update the page label definitions of the PDF.

     :arg List<Label> labels: a list of `Label`. Each dictionary defines a label building rule and a 0-based "start" page number. That start page is the first for which the label definition is valid. Each dictionary has up to 4 items and looks like `{'startpage': int, 'prefix': str, 'style': str, 'firstpagenum': int}` and has the following items.

        - `StartPage`: (int) the first page number (0-based) to apply the label rule. This key **must be present**. The rule is applied to all subsequent pages until either end of document or superseded by the rule with the next larger page number.
        - `Prefix`: (string) an arbitrary string to start the label with, e.g. "A-". Default is "".
        - `Style`: (string) the numbering style. Available are "D" (decimal), "r"/"R" (Roman numbers, lower / upper case), and "a"/"A" (lower / upper case alphabetical numbering: "a" through "z", then "aa" through "zz", etc.). Default is "". If "", no numbering will take place and the pages in that range will receive the same label consisting of the `prefix` value. If prefix is also omitted, then the label will be "".
        - `FirstPageNum`: (int) start numbering with this value. Default is 1, smaller values are ignored.

     For example::

      [{'StartPage': 6, 'Prefix': 'A-', 'Style': 'D', 'FirstPageNum': 10},
       {'StartPage': 10, 'Prefix': '', 'Style': 'D', 'FirstPageNum': 1}]

     will generate the labels "A-10", "A-11", "A-12", "A-13", "1", "2", "3", ... for pages 6, 7 and so on until end of document. Pages 0 through 5 will have the label "".


  .. method:: MakeBookmark((int, int) locNumbers)

    Return a page pointer in a reflowable document. After re-layouting the document, the result of this method can be used to find the new location of the page.

    .. note:: Do not confuse with items of a table of contents, TOC.

    :arg Tuple(int, int) locNumbers: page location. Must be a valid *(chapter, pno)*.

    :rtype: pointer
    :returns: a long integer in pointer format. To be used for finding the new location of the page after re-layouting the document. Do not touch or re-assign.


  .. method:: FindBookmark(int bm)

    Return the new page location after re-layouting the document.

    :arg int bookmark: created by :meth:`Document.MakeBookmark`.

    :rtype: Location
    :returns: the new Location of the page.


  .. method:: GetChapterPageCount(int chapter)

    Return the number of pages of a chapter.

    :arg int chapter: the 0-based chapter number.

    :rtype: int
    :returns: number of pages in chapter. Relevant only for document types with chapter support (EPUB currently).


  .. method:: NextLocation((int, int) pageId)

    Return the location of the following page.

    :arg Tuple(int, int) pageId: the current page id. This must be a tuple *(chapter, pno)* identifying an existing page.

    :returns: The tuple of the following page, i.e. either *(chapter, pno + 1)* or *(chapter + 1, 0)*, **or** the empty tuple *()* if the argument was the last page. Relevant only for document types with chapter support (EPUB currently).


  .. method:: PrevLocation((int, int) pageId)

    Return the locator of the preceding page.

    :arg Tuple(int, int) pageId: the current page id. This must be a tuple *(chapter, pno)* identifying an existing page.

    :returns: The tuple of the preceding page, i.e. either *(chapter, pno - 1)* or the last page of the preceding chapter, **or** the empty tuple *()* if the argument was the first page. Relevant only for document types with chapter support (EPUB currently).


  .. method:: GetPageNumberFromLocation(int chapter, int pno)

    Gets the page number from the supplied `chapter` and `pno`.

    :arg int chapter:
    :arg int pno:

    :rtype: int





  .. method:: LoadPage(int pageId)


    Create a :ref:`Page` object for further processing (like rendering, text searching, etc.).

    :arg int pageId:

        Either a 0-based page number, or a tuple *(chapter, pno)*. For an **integer**, any `-∞ < pageId < PageCount` is acceptable. While page_id is negative, :attr:`PageCount` will be added to it. For example: to load the last page, you can use *doc.LoadPage(-1)*. After this you have page.number = doc.PageCount - 1.

        For a tuple, *chapter* must be in range :attr:`Document.ChapterCount`, and *pno* must be in range :meth:`Document.GetChapterPageCount` of that chapter. Both values are 0-based. Using this notation, :attr:`Page.Number` will equal the given tuple. Relevant only for document types with chapter support (EPUB currently).

    :rtype: :ref:`Page`

  .. note::


  .. method:: ReloadPage(Page page)

    PDF only: Provide a new copy of a page after finishing and updating all pending changes.

    :arg page: page object.
    :type page: :ref:`Page`

    :rtype: :ref:`Page`

    :returns: a new copy of the same page. All pending updates (e.g. to annotations or widgets) will be finalized and a fresh copy of the page will be loaded.

      .. note:: In a typical use case, a page :ref:`Pixmap` should be taken after annotations / widgets have been added or changed. To force all those changes being reflected in the page structure, this method re-instates a fresh copy while keeping the object hierarchy "document -> page -> annotations/widgets" intact.


  .. method:: ForgetPage(Page page)

    Remove a page from a document page dictionary.

    :arg page: page object.


   .. method:: Recolor(int pageNum, int colorNum)
   .. method:: Recolor(int pageNum, string colorSpaceName)
    
      Recolor specific page of PDF with specific color mode.

      :arg int pageNum: the number of specific page between 0 and PageCount.
      :arg int colorNum: the number of colorspace, which means bytes of pixel. "GRAY" = `1`, "RGB" = `3`, "CMYK" = `4`.
      :arg string colorSpaceName: the name of the colorspace, "GRAY", "RGB", "CMYK".


  .. method:: ExtendTocItems(List<Toc> items)

    Add info the TOC list.

    :arg List<Toc> items): A list of TOC items.


  .. method:: ResolveNames()

    PDF only: Convert destination names into a dictionary.

    :returns:
        A dictionary with the following layout:

        * *key*: (string) the name.
        * *value*: (dict) with the following layout:
            * "page":  target page number (0-based). If no page number found -1.
            * "to": (x, y) target point on page. Currently in PDF coordinates,
              i.e. point (0,0) is the bottom-left of the page.
            * "zoom": (float) the zoom factor.
            * "dest": (string) only present if the target location on the page has
              not been provided as "/XYZ" or if no page number was found.

    All names found in the catalog under keys "/Dests" and "/Names/Dests" are
    included.


  .. method:: PageCropBox(int pno)

    PDF only: Return the unrotated page rectangle -- **without loading the page** (via :meth:`Document.LoadPage`). This is meant for internal purpose requiring best possible performance.

    :arg int pno: 0-based page number.

    :returns: :ref:`Rect` of the page like :meth:`Page.Rect`, but ignoring any rotation.

  .. method:: PageXref(int pno)

    PDF only: Return the :data:`xref` of the page -- **without loading the page** (via :meth:`Document.LoadPage`). This is meant for internal purpose requiring best possible performance.

    :arg int pno: 0-based page number.

    :returns: :data:`xref` of the page like :attr:`Page.Xref`.

  .. method:: GetPageXref(int pno)

    :arg int pno: 0-based page number.

    :returns: :data:`xref` of the page like :attr:`Page.Xref`.


  .. method:: LoadOutline()

    Loads the document outline.

    :returns: :doc:`Outline` instance.

  .. method:: GetPages(int start, int stop, int step)

    A generator for a range of pages. Parameters have the same meaning as in the built-in function *range()*. Intended for expressions of the form *"for page in doc.pages(start, stop, step): ..."*.

    :arg int start: start iteration with this page number. Default is zero, allowed values are `-∞ < start < PageCount`. While this is negative, :attr:`PageCount` is added **before** starting the iteration.
    :arg int stop: stop iteration at this page number. Default is :attr:`PageCount`, possible are `-∞ < stop <= PageCount`. Larger values are **silently replaced** by the default. Negative values will cyclically emit the pages in reversed order.
    :arg int step: stepping value. Defaults are 1 if start < stop and -1 if start > stop. Zero is not allowed.

    :returns: a generator iterator over the document's pages. Some examples:

        * "doc.GetPages()" emits all pages.
        * "doc.GetPages(4, 9, 2)" emits pages 4, 6, 8.
        * "doc.GetPages(0, None, 2)" emits all pages with even numbers.
        * "doc.GetPages(-2)" emits the last two pages.
        * "doc.GetPages(-1, -1)" emits all pages in reversed order.
        * "doc.GetPages(-1, -10)" always emits 10 pages in reversed order, starting with the last page -- **repeatedly** if the document has less than 10 pages. So for a 4-page document the following page numbers are emitted: 3, 2, 1, 0, 3, 2, 1, 0, 3, 2, 1, 0, 3.

  .. index::
     pair: fromPage; Document.Convert2Pdf
     pair: toPage; Document.Convert2Pdf
     pair: rotate; Document.Convert2Pdf

  .. method:: Convert2Pdf(int fromPage: 0, int toPage: -1, int rotate: 0)

    Create a PDF version of the current document and write it to memory. **All document types** are supported. The parameters have the same meaning as in :meth:`InsertPdf`. In essence, you can restrict the conversion to a page subset, specify page rotation, and revert page sequence.

    :arg int fromPage: first page to copy (0-based). Default is first page.

    :arg int toPage: last page to copy (0-based). Default is last page.

    :arg int rotate: rotation angle. Default is 0 (no rotation). Should be *n * 90* with an integer n (not checked).

    :rtype: byte[]
    :returns: a *byte[]* object containing a PDF file image. It is created by internally using `ToBytes(garbage: 4, deflate: true)`. See :meth:`ToBytes`. You can output it directly to disk or open it as a PDF.

    .. note:: The method uses the same logic as the *mutool convert* CLI. This works very well in most cases -- however, beware of the following limitations.

      * Image files: perfect, no issues detected. However, image transparency is ignored. If you need that (like for a watermark), use :meth:`Page.InsertImage` instead. Otherwise, this method is recommended for its much better performance.
      * XPS: appearance very good. Links work fine, outlines (bookmarks) are lost, but can easily be recovered [#f2]_.
      * EPUB, CBZ, FB2: similar to XPS.
      * SVG: medium. Roughly comparable to `svglib <https://github.com/deeplook/svglib>`_.

  .. method:: GetToc(bool simple: true)

    Creates a table of contents (TOC) out of the document's outline chain.

    :arg bool simple: Indicates whether a simple or a detailed TOC is required. If *false*, each item of the list also contains a dictionary with :ref:`linkDest` details for each outline entry.

    :rtype: list

    :returns: a list of lists. Each entry has the form *[lvl, title, page, dest]*. Its entries have the following meanings:

      * *lvl* -- hierarchy level (positive *int*). The first entry is always 1. Entries in a row are either **equal**, **increase** by 1, or **decrease** by any number.
      * *title* -- title (*string*)
      * *page* -- 1-based source page number (*int*). `-1` if no destination or outside document.
      * *dest* -- (*dict*) included only if *simple=false*. Contains details of the TOC item as follows:

        - kind: destination kind, see :ref:`LinkDest Kinds`.
        - file: filename if kind is :data:`LINK_GOTOR` or :data:`LINK_LAUNCH`.
        - page: target page, 0-based, :data:`LINK_GOTOR` or :data:`LINK_GOTO` only.
        - to: position on target page (:ref:`Point`).
        - zoom: (float) zoom factor on target page.
        - xref: :data:`xref` of the item (0 if no PDF).
        - color: item color in PDF RGB format `(red, green, blue)`, or omitted (always omitted if no PDF).
        - bold: true if bold item text or omitted. PDF only.
        - italic: true if italic item text, or omitted. PDF only.
        - collapse: true if sub-items are folded, or omitted. PDF only.
        - nameddest: target name if kind=4. PDF only.


  .. method:: GetKeysXref(int xref)

    PDF only: Return the PDF dictionary keys of the :data:`dictionary` object provided by its xref number.

    :arg int xref: the :data:`xref`. Use `-1` to access the special dictionary "PDF trailer".

    :returns: a tuple of dictionary keys present in object :data:`xref`.

  .. method:: GetKeyXref(int xref, string key)

    PDF only: Return type and value of a PDF dictionary key of a :data:`dictionary` object given by its xref.

    :arg int xref: the :data:`xref`. Use `-1` to access the special dictionary "PDF trailer".

    :arg string key: the desired PDF key. Must **exactly** match (case-sensitive) one of the keys contained in :meth:`Document.GetKeysXref`.

    :rtype: Tuple

    :returns: A Tuple (string type, string value) of strings, where type is one of "xref", "array", "dict", "int", "float", "null", "bool", "name", "string" or "unknown" (should not occur). Independent of "type", the value of the key is **always** formatted as a string -- see the following example -- and (almost always) a faithful reflection of what is stored in the PDF. In most cases, the format of the value string also gives a clue about the key type:

    * A "name" always starts with a "/" slash.
    * An "xref" always ends with " 0 R".
    * An "array" is always enclosed in "[...]" brackets.
    * A "dict" is always enclosed in "<<...>>" brackets.
    * A "bool", resp. "null" always equal either "true", "false", resp. "null".
    * "float" and "int" are represented by their string format -- and are thus not always distinguishable.
    * A "string" is converted to UTF-8 and may therefore deviate from what is stored in the PDF. For example, the PDF key "Author" may have a value of "<FEFF004A006F0072006A00200058002E0020004D0063004B00690065>" in the file, but the method will return `('string', 'Jorj X. McKie')`.
  
  .. method:: XrefIsXObject(int xref)

    Check if xref is a form xobject.
    :arg int xref: the :data:`xref`.

    :rtype: `bool`

  .. method:: SetKeyXRef(int xref, string key, string value)

    PDF only: Set (add, update, delete) the value of a PDF key for the :data:`dictionary` object given by its `xref`.

    .. caution:: This is an expert function: if you do not know what you are doing, there is a high risk to render (parts of) the PDF unusable. Please do consult :ref:`AdobeManual` about object specification formats (page 18) and the structure of special dictionary types like page objects.

    :arg int xref: the :data:`xref`. To update the PDF trailer, specify -1.
    :arg string key: the desired PDF key (without leading "/"). Must not be empty. Any valid PDF key -- whether already present in the object (which will be overwritten) -- or new. It is possible to use PDF path notation like `"Resources/ExtGState"` -- which sets the value for key `"/ExtGState"` as a sub-object of `"/Resources"`.
    :arg string value: the value for the key. It must be a non-empty string and, depending on the desired PDF object type, the following rules must be observed. There is some syntax checking, but **no type checking** and no checking if it makes sense PDF-wise, i.e. **no semantics checking**. Upper / lower case is important!

    * **xref** -- must be provided as `"nnn 0 R"` with a valid :data:`xref` number nnn of the PDF. The suffix "`0 R`" is required to be recognizable as an `xref` by PDF applications.
    * **array** -- a string like `"[a b c d e f]"`. The brackets are required. Array items must be separated by at least one space (not commas). An empty array `"[]"` is possible and *equivalent* to removing the key. Array items may be any PDF objects, like dictionaries, `xrefs`, other arrays, etc. Array items may be of different types.
    * **dict** -- a string like `"<< ... >>"`. The brackets are required and must enclose a valid PDF dictionary definition. The empty dictionary `"<<>>"` is possible and *equivalent* to removing the key.
    * **int** -- an integer formatted **as a string**.
    * **float** -- a float formatted **as a string**. Scientific notation (with exponents) is **not allowed by PDF**.
    * **null** -- the string `"null"`. This is the PDF equivalent to `null` in C# and causes the key to be ignored -- however not necessarily removed, resp. removed on saves with garbage collection. If the key is no path hierarchy (i.e. contains no slash "/"), then it will be completely removed.
    * **bool** -- one of the strings `"true"` or `"false"`.
    * **name** -- a valid PDF name with a leading slash like this: `"/PageLayout"`. See page 16 of the :ref:`AdobeManual`.
    * **string** -- a valid PDF string. **All PDF strings must be enclosed by brackets**. Denote the empty string as `"()"`. Depending on its content, the possible brackets are

      - "(...)" for ASCII-only text. Reserved PDF characters must be backslash-escaped and non-ASCII characters must be provided as 3-digit backslash-escaped octals -- including leading zeros. Example: 12 = 0x0C must be encoded as `\014`.
      - "<...>" for hex-encoded text. Every character must be represented by two hex-digits (lower or upper case).

      - If in doubt, we **strongly recommend** to use :meth:`GetPdfStr`! This function automatically generates the right brackets, escapes, and overall format.

  .. method:: GetPagePixmap(int pno, Matrix matrix: IdentityMatrix, int dpi: 0, string colorSpace: null, Rect clip: null, bool alpha: false, bool annots: true)

    Creates a pixmap from page *pno* (zero-based). Invokes :meth:`Page.GetPixmap`.

    All parameters except `pno` are *keyword-only.*

    :arg int pno: page number, 0-based in `-∞ < pno < PageCount`.

    :rtype: :ref:`Pixmap`

  .. method:: GetPageXObjects(int pno)

    PDF only: Return a list of all XObjects referenced by a page.

    :arg int pno: page number, 0-based, `-∞ < pno < PageCount`.

    :rtype: List
    :returns: a list of Entry. These objects typically represent pages *embedded* (not copied) from other PDFs. For example, :meth:`Page.ShowPdfPage` will create this type of object. An item of this list has the following layout: `(xref, name, invoker, bbox)`, where

      * **xref** (*int*) is the XObject's :data:`xref`.
      * **name** (*string*) is the symbolic name to reference the XObject.
      * **invoker** (*int*) the :data:`xref` of the invoking XObject or zero if the page directly invokes it.
      * **bbox** (:ref:`Rect`) the boundary box of the XObject's location on the page **in untransformed coordinates**. To get actual, non-rotated page coordinates, multiply with the page's transformation matrix :attr:`Page.TransformationMatrix`. The bbox is now formatted as :ref:`Rect`.


  .. method:: GetPageImages(int pno, bool full: false)

    PDF only: Return a list of Entry containing images (directly or indirectly) referenced by the page object definition. *Please note that this does not mean, that the page actually displays any of these images.*

    :arg int pno: page number, 0-based, `-∞ < pno < PageCount`.
    :arg bool full: whether to also include the referencer's :data:`xref` (which is zero if this is the page).

    :rtype: List

    :returns: a list of images **referenced** by this page object definition. Each item looks like

        `(Xref, Smask, Width, Height, Bpc, colorspace, alt. colorspace, name, filter, referencer)`

        Where

          * **Xref** (*int*) is the image object number
          * **Smask** (*int*) is the object number of its soft-mask image
          * **Width** and **Height** (*ints*) are the image dimensions
          * **Bpc** (*int*) denotes the number of bits per component (normally 8)
          * **CsName** (*string*) a string naming the colorspace (like **DeviceRGB**)
          * **AltCsName** (*string*) is any alternate colorspace depending on the value of **CsName**
          * **Name** (*string*) is the symbolic name by which the image is referenced
          * **Filter** (*string*) is the decode filter of the image (:ref:`AdobeManual`, pp. 22).

    .. note:: In general, this is not the list of images that are **actually displayed**. This method only parses several PDF objects to collect references to embedded images. It does not analyse the page's :data:`contents`, where all the actual image display commands are defined. To get this information, please use :meth:`Page.get_image_info`. Also have a look at the discussion in section :ref:`textpagedict`.


  .. method:: GetPageFonts(int pno, bool full: false)

    PDF only: Return a list of all fonts (directly or indirectly) referenced by the page. *Please note that this does not mean, that the text on the page actually uses all of these fonts.*

    :arg int pno: page number, 0-based, `-∞ < pno < PageCount`.
    :arg bool full: whether to also include the referencer's :data:`xref`. If *true*, the returned items are one entry longer. Use this option if you need to know, whether the page directly references the font. In this case the last entry is 0. If the font is referenced by an `/XObject` of the page, you will find its :data:`xref` here.

    :rtype: List

    :returns: a list of fonts referenced by this page. Each entry looks like

    **(Xref, Ext, Type, BaseFont, Name, Encoding, RefName)**,

    where

        * **Xref** (*int*) is the font object number (may be zero if the PDF uses one of the built-in fonts directly)
        * **Ext** (*string*) font file extension (e.g. "ttf", see :ref:`FontExtensions`)
        * **Type** (*string*) is the font type (like "Type1" or "TrueType" etc.)
        * **BaseFont** (*string*) is the base font name,
        * **Name** (*string*) is the symbolic name, by which the font is referenced
        * **Encoding** (*string*) the font's character encoding if different from its built-in encoding (:ref:`AdobeManual`, p. 254):

    .. note::
        * This list has no duplicate entries: the combination of :data:`xref`, *name* and *referencer* is unique.
        * In general, this is a superset of the fonts actually in use by this page. The PDF creator may e.g. have specified some global list, of which each page only makes partial use.

  .. method:: GetPageText(int pno, string option: "text", Rect clip: null, int flags: 0, TextPage textPage: null, bool sort: false)

    Extracts the text of a page given its page number *pno* (zero-based). Invokes :meth:`Page.GetText`.

    :arg int pno: page number, 0-based, any value `-∞ < pno < PageCount`.

    For other parameter refer to the page method.

    :rtype: `string`

  .. index::
     pair: fontSize; Document.SetLayout
     pair: rect; Document.SetLayout
     pair: width; Document.SetLayout
     pair: height; Document.SetLayout

  .. method:: SetLayout(Rect rect: null, float width: 0, float height: 0, int fontSize: 11)
    
    Re-paginate ("reflow") the document based on the given page dimension and fontSize. This only affects some document types like e-books and HTML. Ignored if not supported. Supported documents have *true* in property :attr:`is_reflowable`.

    :arg Rect rect: desired page size. Must be finite, not empty and start at point (0, 0).
    :arg float width: use it together with *height* as alternative to *rect*.
    :arg float height: use it together with *width* as alternative to *rect*.
    :arg float fontSize: the desired default fontSize.

  .. method:: SetLanguage(string language)

    Sets the document language.

    :arg string language: the language locale you want to set.


  .. method:: Select(List<int> list)

    PDF only: Keeps only those pages of the document whose numbers occur in the list. Empty sequences or elements outside `range(doc.PageCount)` will cause a *ValueError*. For more details see remarks at the bottom or this chapter.

    :arg List<int> s: The sequence (see :ref:`SequenceTypes`) of page numbers (zero-based) to be included. Pages not in the sequence will be deleted (from memory) and become unavailable until the document is reopened. **Page numbers can occur multiple times and in any order:** the resulting document will reflect the sequence exactly as specified.

    .. note::

        * Page numbers in the sequence need not be unique nor be in any particular order. This makes the method a versatile utility to e.g. select only the even or the odd pages or meeting some other criteria and so forth.

        * On a technical level, the method will always create a new :data:`pagetree`.

        * When dealing with only a few pages, methods :meth:`CopyPage`, :meth:`MovePage`, :meth:`DeletePage` are easier to use. In fact, they are also **much faster** -- by at least one order of magnitude when the document has many pages.


  .. method:: SetMetadata(Dictionary<string, string> m)

    PDF only: Sets or updates the metadata of the document as specified in *m*, a dictionary.

    :arg Dictionary<string, string> m: A dictionary with the same keys as *metadata* (see below). All keys are optional. A PDF's format and encryption method cannot be set or changed and will be ignored. If any value should not contain data, do not specify its key or set the value to `None`. If you use an empty dictionary all metadata information will be cleared to the string *"none"*. If you want to selectively change only some values, modify a copy of *doc.metadata* and use it as the argument. Arbitrary unicode values are possible if specified as UTF-8-encoded.

    Empty values or "none" are not written, but completely omitted.

  .. method:: GetMetadata(string key)

    Returns the metadata for the supplied `key`.

    :rtype: string

  .. method:: DeleteXmlMetadata()

    Deletes the XML metadata on a document.

  .. method:: GetXmlMetadata()

    PDF only: Get the document XML metadata.

    :rtype: string
    :returns: XML metadata of the document. Empty string if not present or not a PDF.

  .. method:: SetXmlMetaData(string metadata)

    PDF only: Sets or updates XML metadata of the document.

    :arg string metadata: the new XML metadata. Should be XML syntax, however no checking is done by this method and any string is accepted.


  .. method:: SetPageLayout(string value)

    PDF only: Set the `/PageLayout`.

    :arg string value: one of the strings "SinglePage", "OneColumn", "TwoColumnLeft", "TwoColumnRight", "TwoPageLeft", "TwoPageRight". Lower case is supported.


  .. method:: SetPageMode(string value)

    PDF only: Set the `/PageMode`.

    :arg string value: one of the strings "UseNone", "UseOutlines", "UseThumbs", "FullScreen", "UseOC", "UseAttachments". Lower case is supported.


  .. method:: SetMarkInfo(Dictionary<string, bool> value)

    PDF only: Set the `/MarkInfo` values.

    :arg Dictionary<string, bool> value: a dictionary like this one: `{"Marked": false, "UserProperties": false, "Suspects": false}`. This dictionary contains information about the usage of Tagged PDF conventions. For details please see the `PDF specifications <https://opensource.adobe.com/dc-acrobat-sdk-docs/standards/pdfstandards/pdf/PDF32000_2008.pdf>`_.


  .. method:: SetToc(List<Toc> tocs, int collapse: 1)

    PDF only: Replaces the **complete current outline** tree (table of contents) with the one provided as the argument. After successful execution, the new outline tree can be accessed as usual via :meth:`Document.GetToc` or via :attr:`Document.Outline`. Like with other output-oriented methods, changes become permanent only via :meth:`Save` (incremental save supported). Internally, this method consists of the following two steps. For a demonstration see example below.

    - Step 1 deletes all existing bookmarks.

    - Step 2 creates a new TOC from the entries contained in *toc*.

    :arg List<Toc> toc:

        A list / tuple with **all bookmark entries** that should form the new table of contents. Output variants of :meth:`GetToc` are acceptable. To completely remove the table of contents specify an empty sequence or None. Each item must be a list with the following format.

        * [Level, Title, Page [, Dest]] where

          - **Level** is the hierarchy level (int > 0) of the item, which **must be 1** for the first item and at most 1 larger than the previous one.

          - **Title** (string) is the title to be displayed. It is assumed to be UTF-8-encoded (relevant for multibyte code points only).

          - **Page** (int) is the target page number **(attention: 1-based)**. Must be in valid range if positive. Set it to -1 if there is no target, or the target is external.

          - **Dest** (optional) is a dictionary or a number. If a number, it will be interpreted as the desired height (in points) this entry should point to on the page. Use a dictionary (like the one given as output by `GetToc(false)`) for a detailed control of the bookmark's properties, see :meth:`Document.GetToc` for a description.

    :arg int collapse: controls the hierarchy level beyond which outline entries should initially show up collapsed. The default 1 will hence only display level 1, higher levels must be unfolded using the PDF viewer. To unfold everything, specify either a large integer, 0 or None.

    :rtype: int
    :returns: the number of inserted, resp. deleted items.

  .. method:: DeleteTocItem(int idx)

    PDF only: Remove this TOC item. This is a high-speed method, which **disables** the respective item, but leaves the overall TOC structure intact. Physically, the item still exists in the TOC tree, but is shown grayed-out and will no longer point to any destination.

    This also implies that you can reassign the item to a new destination using :meth:`Document.SetTocItem`, when required.

    :arg int idx: the index of the item in list :meth:`Document.GetToc`.


  .. method:: SetTocItem(int idx, Link dest, int kind: 0, int pno: 0, string uri: null, string title: null, Point to: null, string filename: null, float zoom: 0)

    PDF only: Changes the TOC item identified by its index. Change the item **title**, **destination**, **appearance** (color, bold, italic) or collapsing sub-items -- or to remove the item altogether.

    Use this method if you need specific changes for selected entries only and want to avoid replacing the complete TOC. This is beneficial especially when dealing with large table of contents.

    :arg int idx: the index of the entry in the list created by :meth:`Document.GetToc`.
    :arg Link dest: the new destination. A dictionary like the last entry of an item in `doc.GetToc(false)`. Using this as a template is recommended. When given, **all other parameters are ignored** -- except title.
    :arg int kind: the link kind, see :ref:`LinkDest Kinds`. If :data:`LINK_NONE`, then all remaining parameter will be ignored, and the TOC item will be removed -- same as :meth:`Document.DeleteTocItem`. If None, then only the title is modified and the remaining parameters are ignored. All other values will lead to making a new destination dictionary using the subsequent arguments.
    :arg int pno: the 1-based page number, i.e. a value 1 <= pno <= doc.PageCount. Required for LINK_GOTO.
    :arg string uri: the URL text. Required for LINK_URI.
    :arg string title: the desired new title. None if no change.
    :arg Point to: (optional) points to a coordinate on the target page. Relevant for LINK_GOTO. If omitted, a point near the page's top is chosen.
    :arg string filename: required for LINK_GOTOR and LINK_LAUNCH.
    :arg float zoom: use this zoom factor when showing the target page.

    **Example use:** Change the TOC of the SWIG manual to achieve this:

    Collapse everything below top level and show the chapter on Python support in red, bold and italic::

    In the previous example, we have changed only 42 of the 1240 TOC items of the file.

  .. method:: Bake(bool annots: true, bool widgets: true)

    PDF only: Convert annotations and / or widgets to become permanent parts of the pages. The PDF **will be changed** by this method. If `widgets` is `true`, the document will also no longer be a "Form PDF".
    
    All pages will look the same, but will no longer have annotations, respectively fields. The visible parts will be converted to standard text, vector graphics or images as required.

    The method may thus be a viable **alternative for PDF-to-PDF conversions** using :meth:`Document.Convert2Pdf`.

    Please consider that annotations are complex objects and may consist of more data "underneath" their visual appearance. Examples are "Text" and "FileAttachment" annotations. When "baking in" annotations / widgets with this method, all this underlying information (attached files, comments, associated PopUp annotations, etc.) will be lost irrevocably and be removed on next garbage collection.

    Use this feature for instance for methods :meth:`Document.InsertPdf` (which supports no copying of widgets) or :meth:`Page.ShowPdfPage` (which supports neither annotations nor widgets) when the source pages should look exactly the same in the target.


    :arg bool annots: convert annotations.
    :arg bool widgets: convert fields / widgets. After execution, the document will no longer be a "Form PDF".


  .. method:: CanSaveIncrementally()

    Check whether the document can be saved incrementally. Use it to choose the right option without encountering exceptions.

  .. method:: Scrub(bool attachedFiles: true, bool cleanPages: true, bool embeddedFiles: true, bool hiddenText: true, bool javascript: true, bool metadata: true, bool redactions: true, int redactImages: 0, bool removeLinks: true, bool resetFields: true, bool resetResponses: true, bool thumbnails: true, bool xmlMetadata: true)

    PDF only: Remove potentially sensitive data from the PDF. This function is inspired by the similar "Sanitize" function in Adobe Acrobat products. The process is configurable by a number of options.

    :arg bool attachedFiles: Search for 'FileAttachment' annotations and remove the file content.
    :arg bool cleanPages: Remove any comments from page painting sources. If this option is set to *false*, then this is also done for *hidden_text* and *redactions*.
    :arg bool embeddedFiles: Remove embedded files.
    :arg bool hiddenText: Remove OCRed text and invisible text [#f7]_.
    :arg bool javascript: Remove JavaScript sources.
    :arg bool metadata: Remove PDF standard metadata.
    :arg bool redactions: Apply redaction annotations.
    :arg int redactImages: how to handle images if applying redactions. One of 0 (ignore), 1 (blank out overlaps) or 2 (remove).
    :arg bool removeLinks: Remove all links.
    :arg bool resetFields: Reset all form fields to their defaults.
    :arg bool resetResponses: Remove all responses from all annotations.
    :arg bool thumbnails: Remove thumbnail images from pages.
    :arg bool xmlMetadata: Remove XML metadata.


  .. method:: Save(string filename, int garbage: 0, int clean: 0, int deflate: 0, int deflateImages: 0, int deflateFonts: false, int incremental: 0, int ascii: 0, int expand: 0, int linear: 0, int pretty: 0, int noNewId: 0, int encryption: PDF_ENCRYPT_NONE, int permissions: -1, string ownerPW: null, string userPW: null, int useObjstms: 0)

    PDF only: Saves the document in its **current state**.

    :arg string filename: The file path to save to. A file object must have been created before via `open(...)`.

    :arg int garbage: Do garbage collection. Positive values exclude "incremental".

     * 0 = none
     * 1 = remove unused (unreferenced) objects.
     * 2 = in addition to 1, compact the :data:`xref` table.
     * 3 = in addition to 2, merge duplicate objects.
     * 4 = in addition to 3, check :data:`stream` objects for duplication. This may be slow because such data are typically large.

    :arg int clean: Clean and sanitize content streams [#f1]_. Corresponds to "mutool clean -sc".

    :arg int deflate: Deflate (compress) uncompressed streams.
    :arg int deflateImages: Deflate (compress) uncompressed image streams [#f4]_.
    :arg int deflateFonts: Deflate (compress) uncompressed fontFile streams [#f4]_.

    :arg int incremental: Only save changes to the PDF. Excludes "garbage" and "linear". Can only be used if *outfile* is a string or a `pathlib.Path` and equal to :attr:`Document.name`. Cannot be used for files that are decrypted or repaired and also in some other cases. To be sure, check :meth:`Document.CanSaveIncrementally`. If this is false, saving to a new file is required.

    :arg int ascii: convert binary data to ASCII.

    :arg int expand: Decompress objects. Generates versions that can be better read by some other programs and will lead to larger files.

     * 0 = none
     * 1 = images
     * 2 = fonts
     * 255 = all

    :arg int linear: Save a linearised version of the document. This option creates a file format for improved performance for Internet access. Excludes "incremental".

    :arg int pretty: Prettify the document source for better readability. PDF objects will be reformatted to look like the default output of :meth:`Document.GetXrefObject`.

    :arg int noNewId: Suppress the update of the file's `/ID` field. If the file happens to have no such field at all, also suppress creation of a new one. Default is `false`, so every save will lead to an updated file identification.

    :arg int permissions: Set the desired permission levels. See :ref:`PermissionCodes` for possible values. Default is granting all.

    :arg int encryption: Set the desired encryption method. See :ref:`EncryptionMethods` for possible values.

    :arg string ownerPW: Set the document's owner password. If not provided, the user password is taken if provided. The string length must not exceed 40 characters.

    :arg string userPW: Set the document's user password. The string length must not exceed 40 characters.

    :arg int useObjstms: Compression option that converts eligible PDF object definitions to information that is stored in some other object's :data:`stream` data. Depending on the `deflate` parameter value, the converted object definitions will be compressed -- which can lead to very significant file size reductions.

    .. warning:: The method does not check, whether a file of that name already exists, will hence not ask for confirmation, and overwrite the file. It is your responsibility as a programmer to handle this.

    .. note::

      **File size reduction**

      1. Use the save options like `garbage: 3|4, deflate: true, useObjstms: true|1`. Do not touch the default values `expand: false|0, clean: false|0, incremental: false|0`.
      This is a "lossless" file size reduction. There is a convenience version of this method with these values set by default, :meth:`Document.ez_save` -- please see below. 

      1. "Lossy" file size reduction in essence must give up something with respect to images, like (a) remove all images (b) replace images by their grayscale versions (c) reduce image resolutions.


  .. method:: SaveIncremental()

    PDF only: saves the document incrementally. This is a convenience abbreviation for `doc.Save(doc.Name, incremental: 1, encryption: PDF_ENCRYPT_KEEP)`.

  .. note::

      Saving incrementally may be required if the document contains verified signatures which would be invalidated by saving to a new file.


  .. method:: ToBytes(int garbage: 0, int clean: 0, int deflate: 0, int deflateImages: 0, int deflateFonts: 0, int ascii: 0, int expand: 0, int linear: 0, int pretty: 0, int noNewId: 0, int encryption: PDF_ENCRYPT_NONE, int permissions: -1, string ownerPW: null, string userPW: null, int useObjstms: 0)

    PDF only: Writes the **current content of the document** to a bytes object instead of to a file. Obviously, you should be wary about memory requirements. The meanings of the parameters exactly equal those in :meth:`save`.

    :rtype: byte[]
    :returns: a bytes object containing the complete document.

  .. method:: SearchPageFor(int pno, string text, bool quads: false, Rect clip: null, int flags: (int)(TextFlags.TEXT_DEHYPHENATE | TextFlags.TEXT_PRESERVE_WHITESPACE | TextFlags.TEXT_PRESERVE_LIGATURES | TextFlags.TEXT_MEDIABOX_CLIP)

     Search for "text" on page number "pno". Works exactly like the corresponding :meth:`Page.SearchFor`. Any integer `-∞ < pno < PageCount` is acceptable.

  .. index::
     pair: append; Document.InsertPdf
     pair: join; Document.InsertPdf
     pair: merge; Document.InsertPdf
     pair: fromPage; Document.InsertPdf
     pair: toPage; Document.InsertPdf
     pair: startAt; Document.InsertPdf
     pair: rotate; Document.InsertPdf
     pair: links; Document.InsertPdf
     pair: annots; Document.InsertPdf
     pair: showProgress; Document.InsertPdf

  .. method:: InsertPdf(Document docsrc, int fromPage: -1, int toPage: -1, int startAt: -1, int rotate: -1, bool links: true, bool annots: true, int showProgress: 0, int final: 1, GraftMap gmap: null)

    PDF only: Copy the page range **[fromPage, toPage]** (including both) of PDF document *docsrc* into the current one. Inserts will start with page number *startAt*. Value -1 indicates default values. All pages thus copied will be rotated as specified. Links and annotations can be excluded in the target, see below. All page numbers are 0-based.

    :arg docsrc: An opened PDF *Document* which must not be the current document. However, it may refer to the same underlying file.
    :type docsrc: *Document*

    :arg int fromPage: First page number in *docsrc*. Default is zero.

    :arg int toPage: Last page number in *docsrc* to copy. Defaults to last page.

    :arg int startAt: First copied page, will become page number *startAt* in the target. Default -1 appends the page range to the end. If zero, the page range will be inserted before current first page.

    :arg int rotate: All copied pages will be rotated by the provided value (degrees, integer multiple of 90).

    :arg bool links: Choose whether (internal and external) links should be included in the copy. Default is `true`. *Named* links (:data:`LINK_NAMED`) and internal links to outside the copied page range are **always excluded**. 
    :arg bool annots: Choose whether annotations should be included in the copy. Form **fields can never be copied** -- see below.
    :arg int showProgress: Specify an interval size greater zero to see progress messages on `sys.stdout`. After each interval, a message like `Inserted 30 of 47 pages.` will be printed.
    :arg int final: Controls whether the list of already copied objects should be **dropped** after this method, default *true*. Set it to 0 except for the last one of multiple insertions from the same source PDF. This saves target file size and speeds up execution considerably.

  .. note::

     1. This is a page-based method. Document-level information of source documents is therefore ignored. Examples include Optional Content, Embedded Files, `StructureElem`, `AcroForm`, table of contents, page labels, metadata, named destinations (and other named entries) and some more. As a consequence, specifically, **Form Fields (widgets) can never be copied** -- although they seem to appear on pages only. Look at :meth:`Document.Bake` for converting a source document if you need to retain at least widget **appearances.**

     2. If `fromPage > toPage`, pages will be **copied in reverse order**. If `0 <= fromPage == toPage`, then one page will be copied.

     3. `docsrc` TOC entries **will not be copied**. It is easy however, to recover a table of contents for the resulting document. Look at the examples below and at program `https://github.com/ArtifexSoftware/MuPDF.NET/tree/main/Examples/JoinDoc`_ in the *examples* directory: it can join PDF documents and at the same time piece together respective parts of the tables of contents.


  .. index::
     pair: append; Document.InsertFile
     pair: join; Document.InsertFile
     pair: merge; Document.InsertFile
     pair: fromPage; Document.InsertFile
     pair: toPage; Document.InsertFile
     pair: startAt; Document.InsertFile
     pair: rotate; Document.InsertFile
     pair: links; Document.InsertFile
     pair: annots; Document.InsertFile
     pair: showProgress; Document.InsertFile

  .. method:: InsertFile(string infile, int fromPage: -1, int toPage: -1, int startAt: -1, int rotate: -1, bool links: true, bool annots: true, int showProgress: 0, int final: 1)

    PDF only: Add an arbitrary supported document to the current PDF. Opens "infile" as a document, converts it to a PDF and then invokes :meth:`Document.InsertPdf`. Parameters are the same as for that method. Among other things, this features an easy way to append images as full pages to an output PDF.

    :arg string infile: the input document to insert. May be a filename specification as is valid for creating a :ref:`Document` or a :ref:`Pixmap`.


  .. index::
     pair: width; Document.NewPage
     pair: height; Document.NewPage

  .. method:: NewPage(int pno: -1, float width: 595, float height: 842)

    PDF only: Insert an empty page.

    :arg int pno: page number in front of which the new page should be inserted. Must be in *1 < pno <= PageCount*. Special values -1 and *doc.PageCount* insert **after** the last page.

    :arg float width: page width.
    :arg float height: page height.

    :rtype: :ref:`Page`
    :returns: the created page object.

  .. index::
     pair: fontSize; Document.InsertPage
     pair: width; Document.InsertPage
     pair: height; Document.InsertPage
     pair: fontName; Document.InsertPage
     pair: fontFile; Document.InsertPage
     pair: color; Document.InsertPage

  .. method:: InsertPage(int pno, string text, string fontName, string fontFile, float fontSize: 11, float width: 595, float height: 842, float[] color: null)

    PDF only: Insert a new page and insert some text. Convenience function which combines :meth:`Document.NewPage` and (parts of) :meth:`Page.InsertText`.

    :arg int pno: page number (0-based) **in front of which** to insert. Must be in `range(-1, doc.PageCount + 1)`. Special values -1 and `doc.PageCount` insert **after** the last page.

    For the other parameters, please consult the aforementioned methods.

    :rtype: int
    :returns: the result of :meth:`Page.InsertText` (number of successfully inserted lines).

  .. method:: DeletePage(int pno: -1)

    PDF only: Delete a page given by its 0-based number in `-∞ < pno < PageCount - 1`.

    :arg int pno: the page to be deleted. Negative number count backwards from the end of the document (like with indices). Default is the last page.

  .. method:: DeletePages(*args, **kwds)

    PDF only: Delete multiple pages given as 0-based numbers.

    **Format 1:** Use keywords. Represents the old format. A contiguous range of pages is removed.
      * "fromPage": first page to delete. Zero if omitted.
      * "toPage": last page to delete. Last page in document if omitted. Must not be less then "from_page".

    **Format 2:** Two page numbers as positional parameters. Handled like Format 1.

    **Format 3:** One positional integer parameter. Equivalent to :meth:`Page.DeletePage`.

    **Format 4:** One positional parameter of type *list*, *tuple* or *range()* of page numbers. The items of this sequence may be in any order and may contain duplicates.

    .. note::

      In an effort to maintain a valid PDF structure, this method and :meth:`DeletePage` will also deactivate items in the table of contents which point to deleted pages. "Deactivation" here means, that the bookmark will point to nowhere and the title will be shown grayed-out by supporting PDF viewers. The overall TOC structure is left intact.

      It will also remove any **links on remaining pages** which point to a deleted one. This action may have an extended response time for documents with many pages.

      Following examples will all delete pages 500 through 519:

      * `doc.DeletePage(500, 519)`
      * `doc.DeletePage(fromPage: 500, toPage: 519)`
      * `doc.DeletePage(new List<int>(){ 500, 501, 502, ... , 519})`

      For the :ref:`AdobeManual` the above takes about 0.6 seconds, because the remaining 1290 pages must be cleaned from invalid links.

      In general, the performance of this method is dependent on the number of remaining pages -- **not** on the number of deleted pages: in the above example, **deleting all pages except** those 20, will need much less time.


  .. method:: CopyPage(int pno, int to: -1)

    PDF only: Copy a page reference within the document.

    :arg int pno: the page to be copied. Must be in range `0 <= pno < PageCount`.

    :arg int to: the page number in front of which to copy. The default inserts **after** the last page.

    .. note:: Only a new **reference** to the page object will be created -- not a new page object, all copied pages will have identical attribute values, including the :attr:`Page.xref`. This implies that any changes to one of these copies will appear on all of them.

  .. method:: CopyFullPage(int pno, int to=-1)

    PDF only: Make a full copy (duplicate) of a page.

    :arg int pno: the page to be duplicated. Must be in range `0 <= pno < PageCount`.

    :arg int to: the page number in front of which to copy. The default inserts **after** the last page.

    .. note::

        * In contrast to :meth:`CopyPage`, this method creates a new page object (with a new :data:`xref`), which can be changed independently from the original.

        * Any Popup and "IRT" ("in response to") annotations are **not copied** to avoid potentially incorrect situations.

  .. method:: MovePage(int pno, int to: -1)

    PDF only: Move (copy and then delete original) a page within the document.

    :arg int pno: the page to be moved. Must be in range `0 <= pno < PageCount`.

    :arg int to: the page number in front of which to insert the moved page. The default moves **after** the last page.


  .. method:: NeedAppearances(int value: 0)

    PDF only: Get or set the */NeedAppearances* property of Form PDFs. Quote: *"(Optional) A flag specifying whether to construct appearance streams and appearance dictionaries for all widget annotations in the document ... Default value: false."* This may help controlling the behavior of some readers / viewers.

    :arg bool value: set the property to this value. If omitted or `null`, inquire the current value.

    :rtype: bool
    :returns:
       * None: not a Form PDF, or property not defined.
       * true / false: the value of the property (either just set or existing for inquiries). Has no effect if no Form PDF.

  .. method:: GetSigFlags()

    PDF only: Return whether the document contains signature fields. This is an optional PDF property: if not present (return value -1), no conclusions can be drawn -- the PDF creator may just not have bothered using it.

    :rtype: int
    :returns:
       * -1: not a Form PDF / no signature fields recorded / no *SigFlags* found.
       * 1: at least one signature field exists.
       * 3:  contains signatures that may be invalidated if the file is saved (written) in a way that alters its previous contents, as opposed to an incremental update.

  .. index::
     pair: filename; Document.AddEmbfile
     pair: ufilename; Document.AddEmbfile
     pair: desc; Document.AddEmbfile

  .. method:: AddEmbfile(string name, byte[] buffer, string filename: null, string ufilename: null, string desc: null)

    PDF only: Embed a new file. All string parameters except the name may be unicode (in previous versions, only ASCII worked correctly). File contents will be compressed (where beneficial).

    :arg string name: entry identifier, **must not already exist**.
    :arg byte[]: file contents.

    :arg string filename: optional filename. Documentation only, will be set to *name* if `null`.
    :arg string ufilename: optional unicode filename. Documentation only, will be set to *filename* if `null`.
    :arg string desc: optional description. Documentation only, will be set to *name* if `null`.

    :rtype: int
    :returns: The method now returns the :data:`xref` of the inserted file. In addition, the file object now will be automatically given the PDF keys `/CreationDate` and `/ModDate` based on the current date-time.


  .. method:: GetEmbfileCount()

    PDF only: Return the number of embedded files.

  .. method:: GetEmbfile(int item)

    PDF only: Retrieve the content of embedded file by its entry number or name. If the document is not a PDF, or entry cannot be found, an exception is raised.

    :arg int item: index or name of entry. An integer must be in `range(GetEmbfileCount())`.

    :rtype: byte[]

  .. method:: DeleteEmbfile(string item)

    PDF only: Remove an entry from `/EmbeddedFiles`. As always, physical deletion of the embedded file content (and file space regain) will occur only when the document is saved to a new file with a suitable garbage option.

    :arg string item: index or name of entry.

    .. warning:: When specifying an entry name, this function will only **delete the first item** with that name. Be aware that PDFs not created with MuPDF.NET may contain duplicate names. So you may want to take appropriate precautions.

  .. method:: GetEmbfileInfo(dynamic item)

    PDF only: Retrieve information of an embedded file given by its number or by its name.

    :arg int/string item: index or name of entry. An integer must be in `Enumerable.Range(GetEmbfileCount())`.

    :rtype: EmbfileInfo
    :returns: `EmbfileInfo` with the following keys:

        * *Name* -- (*string*) name under which this entry is stored
        * *FileName* -- (*string*) filename
        * *UFileName* -- (*unicode*) filename
        * *Desc* -- (*string*) description
        * *Size* -- (*int*) original file size
        * *Length* -- (*int*) compressed file length
        * *CreationDate* -- (*string*) date-time of item creation in PDF format
        * *ModDate* -- (*string*) date-time of last change in PDF format
        * *Collection* -- (*int*) :data:`xref` of the associated PDF portfolio item if any, else zero.
        * *Checksum* -- (*string*) a hashcode of the stored file content as a hexadecimal string. Should be MD5 according to PDF specifications, but be prepared to see other hashing algorithms.

  .. method:: GetEmbfileNames()

    PDF only: Return a list of embedded file names. The sequence of the names equals the physical sequence in the document.

    :rtype: list

  .. index::
     pair: filename; Document.GetEmbfileUpd
     pair: ufilename; Document.GetEmbfileUpd
     pair: desc; Document.GetEmbfileUpd

  .. method:: GetEmbfileUpd(dynamic item, byte[] buffer: null, string filename: null, string ufilename: null, string desc: null)

    PDF only: Change an embedded file given its entry number or name. All parameters are optional. Letting them default leads to a no-operation.

    :arg int/string item: index or name of entry. An integer must be in `Enumerable.Range(GetEmbfileCount())`.
    :arg byte[] buffer: the new file content.

    :arg string filename: the new filename.
    :arg string ufilename: the new unicode filename.
    :arg string desc: the new description.

    :rtype: int
    :returns: `xref` of the file object. Automatically, its `/ModDate` PDF key will be updated with the current date-time.

  .. method:: GetXrefLength()

    Get length of xref table.

  .. method:: GetNewXref()

    :returns: new `xref`

  .. method:: GetCharWidths(int xref, int limit: 256, int idx: 0, FontInfo fontDict: null)

    Return a list of character glyphs and their widths for a font that is present in the document. A font must be specified by its PDF cross reference number :data:`xref`. This function is called automatically from :meth:`Page.InsertText` and :meth:`Page.InsertTextbox`. So you should rarely need to do this yourself.

    :arg int xref: cross reference number of a font embedded in the PDF. To find a font :data:`xref`, use e.g. *doc.GetPageFonts(pno)* of page number *pno* and take the first entry of one of the returned list entries.

    :arg int limit: limits the number of returned entries. The default of 256 is enforced for all fonts that only support 1-byte characters, so-called "simple fonts" (checked by this method).

    :rtype: List
    :returns: a list of (int, double). Each character *c* has an entry  *(g, w)* in this list with an index of *Convert.Int32(c)*. Entry *g* (integer) of the tuple is the glyph id of the character, and float *w* is its normalized width. The actual width for some :data:`fontsize` can be calculated as *w * fontsize*. For simple fonts, the *g* entry can always be safely ignored. In all other cases *g* is the basis for graphically representing *c*.

  .. method:: Close()

    Release objects and space allocations associated with the document. If created from a file, also closes *filename* (releasing control to the OS). Explicitly closing a document is equivalent to deleting it, `del doc`, or assigning it to something else like `doc = None`.

  .. method:: GetXrefObject(int xref, int compressed: 0, int ascii: 0)

    PDF only: Return the definition source of a PDF object.

    :arg int xref: the object's :data:`xref`. A value of `-1` returns the PDF trailer source.
    :arg int compressed: whether to generate a compact output with no line breaks or spaces.
    :arg int ascii: whether to ASCII-encode binary data.

    :rtype: string
    :returns: The object definition source.

  .. method:: GetPdfCatelog()

    PDF only: Return the :data:`xref` number of the PDF catalog (or root) object. Use that number with :meth:`Document.GetXrefObject` to see its source.


  .. method:: GetPdfTrailer(int compressed: 0, int ascii: 0)

    PDF only: Return the trailer source of the PDF,  which is usually located at the PDF file's end. This is :meth:`Document.GetXrefObject` with an *xref* argument of -1.

  .. method:: XrefIsFont(int xref)

    Check if the `xref` is a font or not.

    :arg int xref: :data:`xref` number.

    :rtype: `bool`
    :returns: `true` if the xref is a font.

  .. method:: XrefIsImage(int xref)

    Check if the `xref` is an image or not.

    :arg int xref: :data:`xref` number.

    :rtype: `bool`
    :returns: `true` if the xref is a image.

  .. method:: XrefIsStream(int xref: 0)

    Returns if the `xref` is a stream or not.

    :arg int xref: :data:`xref` number.

    :rtype: `bool`
    :returns: `true`if the xref is a stream.


  .. method:: GetXrefStream(int xref)

    PDF only: Return the **decompressed** contents of the :data:`xref` stream object.

    :arg int xref: :data:`xref` number.

    :rtype: byte[]
    :returns: the (decompressed) stream of the object.

  .. method:: GetXrefStreamRaw(int xref)

    PDF only: Return the **unmodified** (esp. **not decompressed**) contents of the :data:`xref` stream object. Otherwise equal to :meth:`Document.GetXrefStream`.

    :arg int xref: :data:`xref` number.

    :rtype: byte[]
    :returns: the (original, unmodified) stream of the object.

  .. method:: UpdateObject(int xref, string text, PdfPage page: null)

    PDF only: Replace object definition of :data:`xref` with the provided string. The xref may also be new, in which case this instruction completes the object definition. If a page object is also given, its links and annotations will be reloaded afterwards.

    :arg int xref: :data:`xref` number.

    :arg string text: a string containing a valid PDF object definition.

    :arg page: a page object. If provided, indicates, that annotations of this page should be refreshed (reloaded) to reflect changes incurred with links and / or annotations.
    :type page: :ref:`PdfPage`

    :rtype: int
    :returns: zero if successful, otherwise an exception will be raised.

  .. method:: UpdateTocItem(int xref, string action: null, string title: null, int flags: 0, bool collapse: false, float[] color: null)

    Update bookmark by letting it point to nowhere

    :arg int xref: :data:`xref` number

  .. method:: UpdateStream(int xref, byte[] stream: null, int _new: 1, int compress: 1)

    Replace the stream of an object identified by *xref*, which must be a PDF dictionary. If the object is no :data:`stream`, it will be turned into one. The function automatically performs a compress operation ("deflate") where beneficial.

    :arg int xref: :data:`xref` number.

    :arg byte[] stream: the new content of the stream.

    :arg int _new: *deprecated* and ignored. Will be removed some time.
    :arg int compress: whether to compress the inserted stream. If `1` (default), the stream will be inserted using `/FlateDecode` compression (if beneficial), otherwise the stream will inserted as is.

    :raises ValueError: if `xref` does not represent a PDF :data:`dict`. An empty dictionary ``<<>>`` is accepted. So if you just created the `xref` and want to give it a stream, first execute `doc.UpdateObject(xref, "<<>>")`, and then insert the stream data with this method.

    The method is primarily (but not exclusively) intended to manipulate streams containing PDF operator syntax (see pp. 643 of the :ref:`AdobeManual`) as it is the case for e.g. page content streams.

    If you update a contents stream, consider using save parameter *clean=true* to ensure consistency between PDF operator source and the object structure.

    Example: Let us assume that you no longer want a certain image appear on a page. This can be achieved by deleting the respective reference in its contents source(s) -- and indeed: the image will be gone after reloading the page. But the page's :data:`resources` object would still show the image as being referenced by the page. This save option will clean up any such mismatches.

  .. method:: Document.CopyXref(int source, int target, List<string> keep = null)

    PDF Only: Make *target* `xref` an exact copy of *source*. If *source* is a :data:`stream`, then these data are also copied.

    :arg int source: the source :data:`xref`. It must be an existing **dictionary** object.
    :arg int target: the target `xref`. Must be an existing **dictionary** object. If the `xref` has just been created, make sure to initialize it as a PDF dictionary with the minimum specification ``<<>>``.
    :arg List<string> keep: an optional list of top-level keys in ``target``, that should not be removed in preparation of the copy process.

    .. note::

        * Both `xref` numbers must represent existing dictionaries.
        * Before data is copied from ``source``, all ``target`` dictionary keys are deleted. You can specify exceptions from this in the ``keep`` list. If ``source`` however has a same-named key, its value will still replace the target.
        * If ``source`` is a :data:`stream` object, then these data will also be copied over, and ``target`` will be converted to a stream object.
        * A typical use case is to replace or remove an existing image without using redaction annotations. See: `example scripts for ReplaceImage <https://github.com/ArtifexSoftware/MuPDF.NET/tree/main/Examples/ReplaceImage>`_.

  .. method:: Document.ExtractImage(int xref)

    PDF Only: Extract data and meta information of an image stored in the document. The output can directly be used to be stored as an image file, as input for PIL, :ref:`Pixmap` creation, etc. This method avoids using pixmaps wherever possible to present the image in its original format (e.g. as JPEG).

    :arg int xref: :data:`xref` of an image object. If this is not in `range(1, doc.GetXrefLength())`, or the object is no image or other errors occur, `null` is returned and no exception is raised.

    :rtype: ImageInfo
    :returns: a dictionary with the following keys

      * *Ext* (*string*) image type (e.g. *'jpeg'*), usable as image file extension
      * *Smask* (*int*) :data:`xref` number of a stencil (/SMask) image or zero
      * *Width* (*int*) image width
      * *Height* (*int*) image height
      * *ColorSpace* (*int*) the image's *colorspace.n* number.
      * *CsNamee* (*string*) the image's *colorspace.name*.
      * *Xres* (*int*) resolution in x direction. Please also see :data:`resolution`.
      * *Yres* (*int*) resolution in y direction. Please also see :data:`resolution`.
      * *Image* (*byte[]*) image data, usable as image file content

    .. code-block:: cs

      ImageInfo d = doc.ExtractImage(1373)
      Console.WriteLine(d)
      {'ext': 'png', 'smask': 2934, 'width': 5, 'height': 629, 'colorspace': 3, 'xres': 96,
      'yres': 96, 'cs-name': 'DeviceRGB',
      'image': b'\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR\x00\x00\x00\x05\ ...'}

    .. note:: There is a functional overlap with *pix = new Pixmap(doc, xref)*, followed by a *pix.ToBytes()*. Main differences are that extract_image, **(1)** does not always deliver PNG image formats, **(2)** is **very** much faster with non-PNG images, **(3)** usually results in much less disk storage for extracted images, **(4)** returns `None` in error cases (generates no exception). Look at the following example images within the same PDF.

       * `xref` 1186 is a JPEG -- :meth:`Document.ExtractImage` is **many times faster** and produces a **much smaller** output (2.48 MB vs. 0.35 MB)::

          In [27]: %timeit pix =  new Pixmap(doc, 1186);pix.ToBytes()
          341 ms ± 2.86 ms per loop (mean ± std. dev. of 7 runs, 1 loop each)
          In [28]: len(pix.ToBytes())
          Out[28]: 2599433

          In [29]: %timeit img = doc.extract_image(1186)
          15.7 µs ± 116 ns per loop (mean ± std. dev. of 7 runs, 100000 loops each)
          In [30]: len(img["image"])
          Out[30]: 371177


  .. method:: Document.ExtractFont(int xref, int infoOnly: 0, string named: null)

    PDF Only: Return an embedded font file's data and appropriate file extension. This can be used to store the font as an external file. The method does not throw exceptions (other than via checking for PDF and valid :data:`xref`).

    :arg int xref: PDF object number of the font to extract.
    :arg bool infoOnly: only return font information, not the buffer. To be used for information-only purposes, avoids allocation of large buffer areas.
    :arg bool named: If true, a dictionary with the following keys is returned: 'name' (font base name), 'ext' (font file extension), 'type' (font type), 'content' (font file content).

    :rtype: FontInfo
    :returns: a tuple `(basename, ext, type, content)`, where *ext* is a 3-byte suggested file extension (*string*), *basename* is the font's name (*string*), *type* is the font's type (e.g. "Type1") and *content* is a bytes object containing the font file's content (or *b""*). For possible extension values and their meaning see :ref:`FontExtensions`. Return details on error:

          * `("", "", "", b"")` -- invalid `xref` or `xref` is not a (valid) font object.
          * `(basename, "n/a", "Type1", b"")` -- *basename* is not embedded and thus cannot be extracted. This is the case for e.g. the :ref:`Base-14-Fonts` and Type 3 fonts.


    .. warning:: The basename is returned unchanged from the PDF. So it may contain characters (such as blanks) which may disqualify it as a filename for your operating system. Take appropriate action.

    .. note::
       * The returned *basename* in general is **not** the original file name, but it probably has some similarity.
       * If parameter `named == true`, a dictionary with the following keys is returned: `{'name': 'T1', 'ext': 'n/a', 'type': 'Type3', 'content': b''}`.


  .. method:: XrefXmlMetaData()

    PDF only: Return the :data:`xref` of the document's XML metadata.


  .. method:: HasLinks()

  .. method:: HasAnnots()

    PDF only: Check whether there are links, resp. annotations anywhere in the document.

    :returns: *true* / *false*. As opposed to fields, which are also stored in a central place of a PDF document, the existence of links / annotations can only be detected by parsing each page. These methods are tuned to do this efficiently and will immediately return, if the answer is *true* for a page. For PDFs with many thousand pages however, an answer may take some time [#f6]_ if no link, resp. no annotation is found.


  .. method:: SubsetFonts()

    PDF only: Investigate eligible fonts for their use by text in the document. If a font is supported and a size reduction is possible, that font is replaced by a version with a subset of its characters.

    Use this method immediately before saving the document.

    The greatest benefit can be achieved when creating new PDFs using large fonts like is typical for Asian scripts. When using the :ref:`Story` class or method :meth:`Page.InsertHtmlbox`, multiple fonts may automatically be included -- without the programmer becoming aware of it.
    
    In all these cases, the set of actually used unicodes mostly is very small compared to the number of glyphs available in the used fonts. Using this method can easily reduce the embedded font binaries by two orders of magnitude -- from several megabytes down to a low two-digit kilobyte amount.

    Creating font subsets leaves behind a large number of large, now unused PDF objects ("ghosts"). Therefore, make sure to compress and garbage-collect when saving the file. We recommend to use garbage option 3 or 4 and deflate.


  .. method:: IsEnabledJournal()

    Check if journlling is enabled.

    :rtype: `bool`


  .. method:: JournalEnable()

    PDF only: Enable journalling. Use this before you start logging operations.

  .. method:: JournalStartOp(string name)

    PDF only: Start journalling an *"operation"* identified by a string "name". Updates will fail for a journal-enabled PDF, if no operation has been started.


  .. method:: JournalStopOp()

    PDF only: Stop the current operation. The updates between start and stop of an operation belong to the same unit of work and will be undone / redone together.


  .. method:: JournalPosition()

    PDF only: Return the numbers of the current operation and the total operation count.

    :returns: a Tuple `(int, int)` containing the current operation number and the total number of operations in the journal. If **step** is 0, we are at the top of the journal. If **step** equals **steps**, we are at the bottom. Updating the PDF with anything other than undo or redo will automatically remove all journal entries after the current one and the new update will become the new last entry in the journal. The updates corresponding to the removed journal entries will be permanently lost.


  .. method:: JournalOpName(int step)

    PDF only: Return the name of operation number *step.*


  .. method:: JournalCanDo()

    PDF only: Show whether forward ("redo") and / or backward ("undo") executions are possible from the current journal position.

    :returns: a Tuple `{"undo": bool, "redo": bool}`. The respective method is available if its value is `true`.


  .. method:: JournalUndo()

    PDF only: Revert (undo) the current step in the journal. This moves towards the journal's top.


  .. method:: JournalRedo()

    PDF only: Re-apply (redo) the current step in the journal. This moves towards the journal's bottom.


  .. method:: JournalSave(string filename)

    PDF only: Save the journal to a file.

    :arg string filename: either a filename as string.


  .. method:: JournalLoad(string filename)

    PDF only: Load journal from a file. Enables journalling for the document. If journalling is already enabled, an exception is raised.

    :arg string filename: the filename (string) of the journal.


  .. method:: SaveSnapshot(string filename)

    PDF only: Saves a "snapshot" of the document. This is a PDF document with a special, incremental-save format compatible with journalling -- therefore no save options are available. Saving a snapshot is not possible for new documents.

    This is a normal PDF document with no usage restrictions whatsoever. If it is not being changed in any way, it can be used together with its journal to undo / redo operations or continue updating.

  .. method:: IsStream

    PDF only: Check whether the object represented by :data:`xref` is a :data:`stream` type. Return is *False* if not a PDF or if the number is outside the valid xref range.

    :arg int xref: :data:`xref` number.

    :returns: *true* if the object definition is followed by data wrapped in keyword pair *stream*, *endstream*.


  .. attribute:: PageCount

      Gets the page count of the document.

      :rtype: int

  .. attribute:: Language

      The language of the document.

      :rtype: string

  .. attribute:: Outline

    Contains the first :ref:`Outline` entry of the document (or `null`). Can be used as a starting point to walk through all outline items. Accessing this property for encrypted, not authenticated documents will raise an *AttributeError*.

    :type: :ref:`Outline`

  .. attribute:: IsClosed

    *false* if document is still open. If closed, most other attributes and methods will have been deleted / disabled. In addition, :ref:`Page` objects referring to this document (i.e. created with :meth:`Document.LoadPage`) and their dependent objects will no longer be usable. For reference purposes, :attr:`Document.Name` still exists and will contain the filename of the original document (if applicable).

    :type: bool

  .. attribute:: IsDirty

    *true* if this is a PDF document and contains unsaved changes, else *false*.

    :type: bool

  .. attribute:: IsPDF

    *true* if this is a PDF document, else *false*.

    :type: bool

  .. attribute:: IsFormPDF

    *false* if this is not a PDF or has no form fields, otherwise the number of root form fields (fields with no ancestors).

    Returns the total number of (root) form fields.

    :type: int

  .. attribute:: IsReflowable

    *true* if document has a variable page layout (like e-books or HTML). In this case you can set the desired page dimensions during document creation (open) or via method :meth:`Layout`.

    :type: bool

  .. attribute:: IsRepaired

    *true* if PDF has been repaired during open (because of major structure issues). Always *false* for non-PDF documents. If true, :meth:`Document.CanSaveIncrementally` will return *false*.

    :type: bool

  .. attribute:: IsFastWebaccess

    *true* if PDF is in linearized format. *false* for non-PDF documents.

    :type: bool

  .. attribute:: MarkInfo

    A `Dictionary<string, bool>` indicating the `/MarkInfo` value. If not specified, the empty dictionary is returned. If not a PDF, `None` is returned.

    :type: Dictionary

  .. attribute:: PageMode

    A string containing the `/PageMode` value. If not specified, the default "UseNone" is returned. If not a PDF, `null` is returned.

    :type: string

  .. attribute:: PageLayout

    A string containing the `/PageLayout` value. If not specified, the default "SinglePage" is returned. If not a PDF, `null` is returned.

    :type: string

  .. attribute:: VersionCount

    An integer counting the number of versions present in the document. Zero if not a PDF, otherwise the number of incremental saves plus one.

    :type: int

  .. attribute:: NeedsPass

    Indicates whether the document is password-protected against access. This indicator remains unchanged -- **even after the document has been authenticated**. Precludes incremental saves if true.

    :type: bool

  .. attribute:: IsEncrypted

    This indicator initially equals :attr:`Document.NeedsPass`. After successful authentication, it is set to *false* to reflect the situation.

    :type: bool

  .. attribute:: Permissions

    Contains the permissions to access the document. This is an integer containing bool values in respective bit positions. For example, if *doc.Permissions & PermissionCodes.PDF_PERM_MODIFY > 0*, you may change the document. See :ref:`PermissionCodes` for details.

    :type: int

  .. attribute:: MetaData

    Contains the document's meta data as a dictionary or `null` (if *IsEncrypted=true* and *NeedPass=true*). Keys are *format*, *encryption*, *title*, *author*, *subject*, *keywords*, *creator*, *producer*, *creationDate*, *modDate*, *trapped*. All item values are strings or `null`.

    Except *format* and *encryption*, for PDF documents, the key names correspond in an obvious way to the PDF keys */Creator*, */Producer*, */CreationDate*, */ModDate*, */Title*, */Author*, */Subject*, */Trapped* and */Keywords* respectively.

    - *format* contains the document format (e.g. 'PDF-1.6', 'XPS', 'EPUB').

    - *encryption* either contains `null` (no encryption), or a string naming an encryption method (e.g. *'Standard V4 R4 128-bit RC4'*). Note that an encryption method may be specified **even if** *needs_pass=false*. In such cases not all permissions will probably have been granted. Check :attr:`Document.Permissions` for details.

    - If the date fields contain valid data (which need not be the case at all!), they are strings in the PDF-specific timestamp format "D:<TS><TZ>", where

        - <TS> is the 12 character ISO timestamp *YYYYMMDDhhmmss* (*YYYY* - year, *MM* - month, *DD* - day, *hh* - hour, *mm* - minute, *ss* - second), and

        - <TZ> is a time zone value (time interval relative to GMT) containing a sign ('+' or '-'), the hour (*hh*), and the minute (*'mm'*, note the apostrophes!).

    - A Paraguayan value might hence look like *D:20150415131602-04'00'*, which corresponds to the timestamp April 15, 2015, at 1:16:02 pm local time Asuncion.

    :type: Dictionary<string, string>

  .. Attribute:: Name

    Contains the *filename* or *filetype* value with which *Document* was created.

    :type: string

  .. Attribute:: Len

    Contains the number of pages of the document. May return 0 for documents with no pages.

    :type: int

  .. Attribute:: ChapterCount

    Contains the number of chapters in the document. Always at least 1. Relevant only for document types with chapter support (EPUB currently). Other documents will return 1.

    :type: int

  .. Attribute:: LastLocation

    Contains (chapter, pno) of the document's last page. Relevant only for document types with chapter support (EPUB currently). Other documents will return `(0, PageCount - 1)` and `(0, -1)` if it has no pages.

    :type: int

  .. Attribute:: FormFonts

    A list of form field font names defined in the */AcroForm* object. `null` if not a PDF.

    :type: List<string>

.. NOTE:: For methods that change the structure of a PDF (:meth:`InsertPdf`, :meth:`Select`, :meth:`CopyPage`, :meth:`DeletePage` and others), be aware that objects or properties in your program may have been invalidated or orphaned. Examples are :ref:`Page` objects and their children (links, annotations, widgets), variables holding old page counts, tables of content and the like. Remember to keep such variables up to date or delete orphaned objects. Also refer to :ref:`ReferenialIntegrity`.


Output structures
------------------

Entry structure
~~~~~~~~~~~~~~~
Entry structure includes Image info, font info and form info.

========== ===================================================================================================
**Key**    **Value**
========== ===================================================================================================
Xref       *int* is the image, font and form object number
Name       *string* name under which this entry is stored or basefont name of font entry
Ext        *string* font file extension. (e.g. "ttf")
Smask      *int* is the object number of its soft-mask image
Width      *int* the image dimension
Height     *int* the image dimension
Bpc        *int* denotes the number of bits per componenet, normally 8
CsName     *string* a string naming the colorspace like `DeviceRGB`
AltCsName  *string* is any alternate colorspace depending on the value of colorspace
Filter     *string* is the decode filter of the image.
Type       *string* is the font type like "Type1" or "TrueType" etc.
RefName    *string* string referencer
Encoding   *string* the font's character encoding if different from its built-in encoding (:ref:`AdobeManual`)
StreamXref *int* stream number, normally 0
Bbox       the area of images or forms. :ref:`Rect`
========== ===================================================================================================

Location structure
~~~~~~~~~~~~~~~~~~

========== ==========================
**Key**    **Value**
========== ==========================
Chapter    number of pages in chapter
Page       number of page
========== ==========================

OCGroup structure
~~~~~~~~~~~~~~~~~

========== =============================================================
**Key**    **Value**
========== =============================================================
Name       text string or name field of the originating OCG
Intents    a string or list of strings declaring the visibility intents.
On         item state
Usage      another influencer for OCG visibility.
========== =============================================================

OCLayer structure
~~~~~~~~~~~~~~~~~

========== =====================================================================
**Key**    **Value**
========== =====================================================================
On         list of :data:`xref` of OCGs to set ON.
Off        list of :data:`xref` of OCGs to set OFF.
Locked     a list of OCG xref number that cannot be changed by the user interface.
RBGroups   a list of lists. Replaces previous values.
BaseState  state of OCGs that are not mentioned in *on* or *off*.
========== =====================================================================

Toc structure
~~~~~~~~~~~~~

======== ============================================
**Key**    **Value**
======== ============================================
Level    hierarchy level (positive *int*)
Title    title (*string*)
Page     1-based source page number (*int*)
Link     included only if *simple=False* (*dynamic*)
======== ============================================

Other Examples
----------------
**Extract all page-referenced images of a PDF into separate PNG files**:

.. code-block:: cs

  MuPDFDocument doc = new MuPDFDocument("input.pdf");

  for (int i = 0; i < doc.PageCount; i ++)
  {
      List<Entry> imgs = doc.GetPageImages(i);
      for (int j = 0; j < imgs.Count; j ++)
      {
          int xref = imgs[j].Xref;            // xref number
          Pixmap pix = new Pixmap(doc, xref); // make pixmap from image
          if ((pix.N - pix.Alpha) < 4)        // can be saved as PNG
              pix.Save($"p{i}-{xref}.png");
          else                                // CMYK: must convert first
          {
              Pixmap pix0 = new Pixmap(Utils.csRGB, pix);
              pix0.Save($"p{i}-{xref}.png");
              pix0 = null;                    // free Pixmap resources
          }
          pix = null;                         // free Pixmap resources
      }
  }

**Rotate all pages of a PDF:**

.. code-block:: cs

  for (int i = 0; i < doc.PageCount; i ++)
  {
      doc[i].SetRotation(90);
  }

.. rubric:: Footnotes

.. [#f1] Content streams describe what (e.g. text or images) appears where and how on a page. PDF uses a specialized mini language similar to PostScript to do this (pp. 643 in :ref:`AdobeManual`), which gets interpreted when a page is loaded.

.. [#f2] However, you **can** use :meth:`Document.GetToc` and :meth:`Page.GetLinks` (which are available for all document types) and copy this information over to the output PDF.

.. [#f3] For applicable (EPUB) document types, loading a page via its absolute number may result in layouting a large part of the document, before the page can be accessed. To avoid this performance impact, prefer chapter-based access. Use convenience methods and attributes :meth:`Document.NextLocation`, :meth:`Document.PrevLocation` and :attr:`Document.LastLocation` for maintaining a high level of coding efficiency.

.. [#f4] These parameters cause separate handling of stream categories: use it together with `expand` to restrict decompression to streams other than images / fontfiles.

.. [#f5] Examples for "Form XObjects" are created by :meth:`Page.ShowPDFPage`.

.. [#f6] For a *false* the **complete document** must be scanned. Both methods **do not load pages,** but only scan object definitions. This makes them at least 10 times faster than application-level loops (where total response time roughly equals the time for loading all pages). For the :ref:`AdobeManual` (756 pages) and the Pandas documentation (over 3070 pages) -- both have no annotations -- the method needs about 11 ms for the answer *false*. So response times will probably become significant only well beyond this order of magnitude.

.. [#f7] This only works under certain conditions. For example, if there is normal text covered by some image on top of it, then this is undetectable and the respective text is **not** removed. Similar is true for white text on white background, and so on.

.. include:: ../footer.rst
