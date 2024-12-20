.. include:: ../header.rst

.. _TheBasics:


==============================
The Basics
==============================


.. _Supported_File_Types:

Supported File Types
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


|MuPDF.NET| can open files other than just |PDF|.

The following file types are supported:

Document Formats
""""""""""""""""""

- PDF
- XPS
- EPUB
- MOBI
- FB2
- CBZ
- SVG
- TXT


Image Formats
""""""""""""""""""

**Input formats**
JPG/JPEG, PNG, BMP, GIF, TIFF, PNM, PGM, PBM, PPM, PAM, JXR, JPX/JP2, PSD

**Output formats**
JPG/JPEG, PNG, PNM, PGM, PBM, PPM, PAM, PSD, PS



.. _The_Basics_Opening_Files:

Opening a File
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


To open a file, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("a.pdf"); // open a document



----------


.. _The_Basics_Extracting_Text:

Extract text from a |PDF|
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

To extract all the text from a |PDF| file, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("example.pdf");
    FileStream wstream = File.OpenWrite("1.txt");

    for (int i = 0; i < doc.PageCount; i ++)
    {
        string text = doc[i].GetText("html");
        Console.WriteLine(text);
        if (!string.IsNullOrEmpty(text))
            wstream.Write(Encoding.UTF8.GetBytes(text));
    }

    wstream.Close();

Of course it is not just |PDF| which can have text extracted - all the :ref:`supported document file formats <About_Feature_Matrix>` such as :title:`MOBI`, :title:`EPUB`, :title:`TXT` can have their text extracted.

.. note::

    **Taking it further**

    If your document contains image based text content then use OCR on the page for subsequent text extraction:

    .. code-block:: cs

        TextPage tp = page.GetTextPageOcr();
        string text = page.GetText(textpage: tp);


    **API reference**

    - :meth:`Page.GetText`





----------


.. _The_Basics_Extracting_Images:

Extract images from a |PDF|
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

To extract all the images from a |PDF| file, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("test.pdf"); // open a document

    for (int i = 0; i < doc.PageCount; i++)
    {
        Page page = doc[i];
        List<Entry> images = page.GetImages();

        for (int j = 0; j < images.Count; j++)
        {
            int xref = images[j].Xref; // get Xref
            Pixmap pix = new Pixmap(doc, xref);

            if (pix.N - pix.Alpha > 3)
                pix = new Pixmap(Utils.csRGB, pix);

            pix.Save($"page_{i}-image_{j}.png");
            pix = null;
        }
    }



.. note::

    **API reference**

    - :meth:`Page.GetImages`
    - :ref:`Pixmap<Pixmap>`



.. _The_Basics_Extracting_Vector_Graphics:

Extract vector graphics
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

To extract all the vector graphics from a document page, do the following:


.. code-block:: cs

    Document doc = new Document("some.file");
    Page page = doc[0];
    List<PathInfo> paths = page.GetDrawings();


This will return a dictionary of paths for any vector drawings found on the page.

.. note::

    **API reference**

    - :meth:`Page.GetDrawings`



----------



.. _The_Basics_Merging_PDF:
.. _merge PDF:
.. _join PDF:

Merging |PDF| files
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

To merge |PDF| files, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc_a = new Document("a.pdf"); // open the 1st document
    Document doc_b = new Document("b.pdf"); // open the 2nd document

    doc_a.InsertPdf(doc_b); // merge the docs
    doc_a.Save("a+b.pdf"); // save the merged document with a new filename


Merging |PDF| files with other types of file
"""""""""""""""""""""""""""""""""""""""""""""""""""""

With :meth:`Document.InsertFile` you can invoke the method to merge :ref:`supported files<Supported_File_Types>` with |PDF|. For example:

.. code-block:: cs

    using MuPDF.NET;

    Document doc_a = new Document("a.pdf"); // open the 1st document
    Document doc_b = new Document("b.svg"); // open the 2nd document

    doc_a.InsertFile(doc_b); // merge the docs
    doc_a.Save("a+b.pdf"); // save the merged document with a new filename


.. note::

    **Taking it further**

    It is easy to join PDFs with :meth:`Document.InsertPdf` & :meth:`Document.InsertFile`. Given open |PDF| documents, you can copy page ranges from one to the other. You can select the point where the copied pages should be placed, you can revert the page sequence and also change page rotation.

    **API reference**

    - :meth:`Document.InsertPdf`
    - :meth:`Document.InsertFile`


----------



.. _The_Basics_Watermarks:

Adding a watermark to a |PDF|
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

To add a watermark to a |PDF| file, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("document.pdf"); // open a document

    for (int page_index; page_index < doc.PageCount; page_index ++) // iterate over pdf pages
    {
        Page page = doc[page_index]; // get the page

        // insert an image watermark from a file name to fit the page bounds
        page.InsertImage(page.bound(), filename: "watermark.png", overlay: false)
    }
    doc.Save("watermarked-document.pdf"); // save the document with a new filename

.. note::

    **Taking it further**

    Adding watermarks is essentially as simple as adding an image at the base of each |PDF| page. You should ensure that the image has the required opacity and aspect ratio to make it look the way you need it to.

    In the example above a new image is created from each file reference, but to be more performant (by saving memory and file size) this image data should be referenced only once.

    **API reference**

    - :meth:`Page.GetBound`
    - :meth:`Page.InsertImage`


----------


.. _The_Basics_Images:

Adding an image to a |PDF|
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

To add an image to a |PDF| file, for example a logo, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("document.pdf") // open a document

    for (int page_index; page_index < doc.PageCount; page_index ++) // iterate over pdf pages
    {
        Page page = doc[page_index]; // get the page

        // insert an image logo from a file name at the top left of the document
        page.InsertImage(new Rect(0,0,50,50),filename: "my-logo.png");
    }

    doc.Save("logo-document.pdf"); // save the document with a new filename

.. note::

    **Taking it further**

    As with the watermark example you should ensure to be more performant by only referencing the image once if possible - see the code example and explanation on :meth:`Page.InsertImage`.

    **API reference**

    - :ref:`Rect<Rect>`
    - :meth:`Page.InsertImage`


----------

.. _The_Basics_Extracting_and_Drawing_Vector_Graphics:

Extracting & Drawing vector graphics
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The following example shows how to extract drawings from a page in a |PDF| and then recreate them in a new |PDF|.

Essentially the process is:

- Load the |PDF| with the drawing information you want to extract.
- Extract drawing info from the page as a list of path information by using :meth:`Page.GetDrawings`.
- Create a blank document for the output.
- Add a :doc:`../classes/Shape` to the page to hold the drawing info.
- Iterate the path information and look for lines, bezier, rectangle or quad objects.
- Draw the required paths onto the new :doc:`../classes/Shape`.
- Decorate the shape with the required styling.
- Commit the shape.
- Save the output document.



.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("pdf-with-vector-drawings-on-page-one.pdf");
    Page page = doc[0];
    List<PathInfo> paths = page.GetDrawings();

    var outpdf = new Document();
    var outpage = outpdf.NewPage(width: page.Rect.Width, height: page.Rect.Height);
    var shape = outpage.NewShape();

    foreach(PathInfo path in paths)
    {
        foreach (Item item in path.Items)
        {
            if (item.Type == "l")
                shape.DrawLine(item.P1, item.LastPoint);
            else if (item.Type == "c")
                shape.DrawBezier(item.P1, item.P2, item.P3, item.LastPoint);
            else if (item.Type == "re")
                shape.DrawRect(item.Rect, item.Orientation);
            else if (item.Type == "qu")
                shape.DrawQuad(item.Quad);
            else
                throw new Exception("unhandled drawing");
        }
        shape.Finish(
            fill: path.Fill,
            color: path.Color,
            dashes: path.Dashes,
            evenOdd: path.EvenOdd,
            closePath: path.ClosePath,
            lineJoin: (int)path.LineJoin,
            lineCap: ((int)path.LineCap.ElementAt(0)),
            width: path.Width,
            strokeOpacity: path.StrokeOpacity,
            fillOpacity: path.FillOpacity
            );
    }

    shape.Commit();
    outpdf.Save("graphics-redrawn.pdf");



----------





.. _The_Basics_Rotating:

Rotating a |PDF|
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

To add a rotation to a page, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("test.pdf"); // open document
    Page page = doc[0]; // get the 1st page of the document
    page.SetRotation(90); // rotate the page
    doc.Save("rotated-page-1.pdf");

.. note::

    **API reference**

    - :meth:`Page.SetRotation`


----------

.. _The_Basics_Cropping:

Cropping a |PDF|
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

To crop a page to a defined :ref:`Rect<Rect>`, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("test.pdf"); // open document
    Page page = doc[0]; // get the 1st page of the document
    page.SetCropBox(new Rect(100, 100, 400, 400)); // set a cropbox for the page
    doc.Save("cropped-page-1.pdf");

.. note::

    **API reference**

    - :meth:`Page.SetCropBox`


----------


.. _The_Basics_Attaching_Files:

Attaching Files
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

To attach another file to a page, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("test.pdf") // open main document
    Document attachment = new Document("my-attachment.pdf"); // open document you want to attach

    Page page = doc[0]; // get the 1st page of the document
    Point point = new Point(100, 100); // create the point where you want to add the attachment
    byte[] attachment_data = attachment.Write(); // get the document byte data as a buffer
    
    // add the file annotation with the point, data and the file name
    Annot file_annotation = page.AddFileAnnot(point, attachment_data, "attachment.pdf");

    doc.Save("document-with-attachment.pdf"); // save the document


.. note::

    **Taking it further**

    When adding the file with :meth:`Page.AddFileAnnot` note that the third parameter for the `filename` should include the actual file extension. Without this the attachment possibly will not be able to be recognized as being something which can be opened. For example, if the `filename` is just *"attachment"* when view the resulting PDF and attempting to open the attachment you may well get an error. However, with *"attachment.pdf"* this can be recognized and opened by PDF viewers as a valid file type.

    The default icon for the attachment is by default a "push pin", however you can change this by setting the `icon` parameter.

    **API reference**

    - :ref:`Point<Point>`
    - :meth:`Document.Write`
    - :meth:`Page.AddFileAnnot`


----------


.. _The_Basics_Embedding_Files:

:index:`Embedding Files <triple: attach;embed;file>`
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

To embed a file to a document, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("test.pdf") // open main document
    Document embedded_doc = new Document("my-embed.pdf") // open document you want to embed

    byte[] embedded_data = embedded_doc.Write(); // get the document byte data as a buffer

    // embed with the file name and the data
    doc.AddEmbfile("my-embedded_file.pdf", embedded_data);

    doc.Save("document-with-embed.pdf"); // save the document

.. note::

    **Taking it further**

    As with :ref:`attaching files<The_Basics_Attaching_Files>`, when adding the file with :meth:`Document.AddEmbfile` note that the first parameter for the `filename` should include the actual file extension.

    **API reference**

    - :meth:`Document.Write`
    - :meth:`Document.AddEmbfile`


----------



.. _The_Basics_Deleting_Pages:

Deleting Pages
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

To delete a page from a document, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("test.pdf"); // open a document
    doc.DeletePage(0); // delete the 1st page of the document
    doc.Save("test-deleted-page-one.pdf"); // save the document

To delete a multiple pages from a document, do the following:

.. raw:: html

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("test.pdf"); // open a document
    doc.DeletePages(from: 9, to: 14); // delete a page range from the document
    doc.Save("test-deleted-pages.pdf"); // save the document


What happens if I delete a page referred to by bookmarks or hyperlinks?
""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""

- A bookmark (entry in the Table of Contents) will become inactive and will no longer navigate to any page.

- A hyperlink will be removed from the page that contains it. The visible content on that page will not otherwise be changed in any way.

.. note::

    **Taking it further**

    The page index is zero-based, so to delete page 10 of a document you would do the following `doc.DeletePage(9)`.

    Similarly, `doc.DeletePages(from: 9, to: 14)` will delete pages 10 - 15 inclusive.


    **API reference**

    - :meth:`Document.DeletePage`
    - :meth:`Document.DeletePages`

----------


.. _The_Basics_Rearrange_Pages:

Re-Arranging Pages
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

To change the sequence of pages, i.e. re-arrange pages, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("test.pdf"); // open a document
    doc.MovePage(1, 0); // move the 2nd page of the document to the start of the document
    doc.Save("test-page-moved.pdf"); // save the document


.. note::

    **API reference**

    - :meth:`Document.MovePage`

----------



.. _The_Basics_Copying_Pages:

Copying Pages
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


To copy pages, do the following:


.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("test.pdf"); // open a document
    doc.CopyPage(0); // copy the 1st page and puts it at the end of the document
    doc.save("test-page-copied.pdf"); // save the document

.. note::

    **API reference**

    - :meth:`Document.CopyPage`


----------

.. _The_Basics_Selecting_Pages:

Selecting Pages
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


To select pages, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("test.pdf"); // open a document
    doc.Select(new List<int>([0, 1])); // select the 1st & 2nd page of the document
    doc.Save("just-page-one-and-two.pdf"); // save the document


.. note::

    **Taking it further**

    With |MuPDF.NET| you have all options to copy, move, delete or re-arrange the pages of a |PDF|. Intuitive methods exist that allow you to do this on a page-by-page level, like the :meth:`Document.CopyPage` method.

    Or you alternatively prepare a complete new page layout in form of a list, that contains the page numbers you want, in the sequence you want, and as many times as you want each page. The following may illustrate what can be done with :meth:`Document.Select`

    .. code-block:: cs

        doc.Select(new List<int>([1, 1, 1, 5, 4, 9, 9, 9, 0, 2, 2, 2]))


    Now let's prepare a PDF for double-sided printing (on a printer not directly supporting this):

    The number of pages is given by `len(doc)` (equal to `doc.PageCount`).

    This snippet creates the respective sub documents which can then be used to print the document:

    .. code-block:: cs

        doc.Select(p_even) // only the even pages left over
        doc.Save("even.pdf") // save the "even" PDF
        doc.close() // recycle the file
        doc = new Document(doc.name) // re-open
        doc.Select(p_odd) // and do the same with the odd pages
        doc.Save("odd.pdf")


    For more information also have a look at this Wiki `article <https://github.com/pymupdf/PyMuPDF/wiki/Rearranging-Pages-of-a-PDF>`_.


    The following example will reverse the order of all pages (**extremely fast:** sub-second time for the 756 pages of the :ref:`AdobeManual`):

    .. code-block:: cs

        int lastPage = doc.PageCount - 1;
        for(int i = 0; i < lastPage; i ++)
            doc.MovePage(lastPage, i) // move current last page to the front



    This snippet duplicates the PDF with itself so that it will contain the pages *0, 1, ..., n, 0, 1, ..., n* **(extremely fast and without noticeably increasing the file size!)**:

    .. code-block:: cs

        int pageCount = doc.PageCount;
        for(int i = 0; i < pageCount; i ++)
            doc.CopyPage(i) // copy this page to after last page



    **API reference**

    - :meth:`Document.Select`

----------


.. _The_Basics_Adding_Blank_Pages:




Adding Blank Pages
~~~~~~~~~~~~~~~~~~~~~

To add a blank page, do the following:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document(...) // some new or existing PDF document
    Page page = doc.NewPage(-1, // insertion point: end of document
                        width: 595, // page dimension: A4 portrait
                        height: 842)
    doc.Save("doc-with-new-blank-page.pdf") // save the document


.. note::

    **Taking it further**

    Use this to create the page with another pre-defined paper format:

    .. code-block:: cs

        (int w, int h) = Utils.PageSize("letter-l"); // 'Letter' landscape
        Page page = doc.NewPage(width: w, height: h);


    The convenience function :meth:`PageSize` knows over 40 industry standard paper formats to choose from. To see them, inspect dictionary :attr:`paperSizes`. Pass the desired dictionary key to :meth:`PageSize` to retrieve the paper dimensions. Upper and lower case is supported. If you append "-L" to the format name, the landscape version is returned.

    Here is a 3-liner that creates a |PDF|: with one empty page. Its file size is 460 bytes:

    .. code-block:: cs

        doc = new Document();
        doc.NewPage();
        doc.Save("A4.pdf");


    **API reference**

    - :meth:`Document.NewPage`
    - :meth:`Utils.PageSize`


----------


.. _The_Basics_Inserting_Pages:

Inserting Pages with Text Content
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Using the :meth:`Document.InsertPage` method also inserts a new page and accepts the same `width` and `height` parameters. But it lets you also insert arbitrary text into the new page and returns the number of inserted lines.

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document(...)  // some new or existing PDF document
    int n = doc.InsertPage(-1, // default insertion point
                        text: "The quick brown fox jumped over the lazy dog",
                        fontsize: 11,
                        width: 595,
                        height: 842,
                        fontname: "Helvetica", // default font
                        fontfile: None, // any font file name
                        color: new float[]{0, 0, 0}) // text color (RGB)




.. note::

    **Taking it further**

    The text parameter can be a (sequence of) string (assuming UTF-8 encoding). Insertion will start at :ref:`Point` (50, 72), which is one inch below top of page and 50 points from the left. The number of inserted text lines is returned.

    **API reference**

    - :meth:`Document.InsertPage`



----------



.. _The_Basics_Spliting_Single_Pages:

Splitting Single Pages
~~~~~~~~~~~~~~~~~~~~~~~~~~

This deals with splitting up pages of a |PDF| in arbitrary pieces. For example, you may have a |PDF| with *Letter* format pages which you want to print with a magnification factor of four: each page is split up in 4 pieces which each going to a separate |PDF| page in *Letter* format again.



.. code-block:: cs

    using MuPDF.NET;

    Document src = pymupdf.open("test.pdf");
    Document doc = pymupdf.open(); // empty output PDF

    for (int i = 0; i < src.PageCount; i ++) // for each page in input
        Page spage = doc[i];
        r = spage.rect;  // input page rectangle
        d = new Rect(spage.CropboxPosition,  // CropBox displacement if not
                      spage.CropboxPosition)  // starting at (0, 0)
        //--------------------------------------------------------------------------
        // example: cut input page into 2 x 2 parts
        //--------------------------------------------------------------------------
        r1 = r / 2;  // top left rect
        r2 = r1 + (r1.width, 0, r1.width, 0);  // top right rect
        r3 = r1 + (0, r1.height, 0, r1.height);  // bottom left rect
        r4 = new Rect(r1.br, r.br);  // bottom right rect
        List<Rect> rect_list = new List<Rect>([r1, r2, r3, r4]);  // put them in a list

        for(Rect rx in rect_list)  // run thru rect list
        {
            rx += d;  // add the CropBox displacement
            page = doc.NewPage(-1,  // new output page with rx dimensions
                               width: rx.width,
                               height: rx.height);
            page.ShowPdfPage(
                    page.Rect,  // fill all new page with the image
                    src,  // input document
                    spage.Number,  // input page number
                    clip: rx,  // which part to use of input page
                );
        }

    // that's it, save output file
    doc.Save("poster-" + src.Name,
             garbage: 3,  // eliminate duplicate objects
             deflate: true,  // compress stuff where possible
    );


Example:

.. image:: ../images/img-posterize.png

.. note::

    **API reference**

    - :meth:`Page.CropboxPosition`
    - :meth:`Page.ShowPdfPage`


--------------------------


.. _The_Basics_Combining_Single_Pages:


Combining Single Pages
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

This deals with joining |PDF| pages to form a new |PDF| with pages each combining two or four original ones (also called "2-up", "4-up", etc.). This could be used to create booklets or thumbnail-like overviews.


.. code-block:: cs

    using MuPDF.NET;

    Document src = new Document("test.pdf");
    Document doc = new Document();  // empty output PDF

    (int width, int height) = pymupdf.PageSize("a4");  // A4 portrait output page format
    Rect r = new Rect(0, 0, width, height);

    // define the 4 rectangles per page
    Rect r1 = r / 2;  // top left rect
    Rect r2 = r1 + (r1.width, 0, r1.width, 0);  // top right
    Rect r3 = r1 + (0, r1.height, 0, r1.height);  // bottom left
    Rect r4 = pymupdf.Rect(r1.br, r.br);  // bottom right

    // put them in a list
    Rect[] r_tab = new Rect[]{ r1, r2, r3, r4 };

    // now copy input pages to output
    for (int i = 0; i < src.PageCount; i++)
    {
        Page spage = doc[i]
        if (spage.Number % 4 == 0)  // create new output page
            page = doc.NewPage(-1,
                          width: width,
                          height: height)
        // insert input page into the correct rectangle
        page.ShowPdfPage(r_tab[spage.Number % 4],  // select output rect
                         src,  // input document
                         spage.Number)  // input page number
    }
    // by all means, save new file using garbage collection and compression
    doc.Save("4up.pdf", garbage: 3, deflate: true)


Example:

.. image:: ../images/img-4up.png


.. note::

    **API reference**

    - :meth:`Page.CropboxPosition`
    - :meth:`Page.ShowPdfPage`

--------------------------


.. _The_Basics_Encryption_and_Decryption:


|PDF| Encryption & Decryption
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


Starting with version 1.16.0, |PDF| decryption and encryption (using passwords) are fully supported. You can do the following:

* Check whether a document is password protected / (still) encrypted (:attr:`Document.NeedsPass`, :attr:`Document.IsEncrypted`).
* Gain access authorization to a document (:meth:`Document.Authenticate`).
* Set encryption details for PDF files using :meth:`Document.Save` or :meth:`Document.Write` and

    - decrypt or encrypt the content
    - set password(s)
    - set the encryption method
    - set permission details

.. note:: A |PDF| document may have two different passwords:

   * The **owner password** provides full access rights, including changing passwords, encryption method, or permission detail.
   * The **user password** provides access to document content according to the established permission details. If present, opening the |PDF| in a viewer will require providing it.

   Method :meth:`Document.Authenticate` will automatically establish access rights according to the password used.

The following snippet creates a new |PDF| and encrypts it with separate user and owner passwords. Permissions are granted to print, copy and annotate, but no changes are allowed to someone authenticating with the user password.


.. code-block:: cs

    using MuPDF.NET;

    string text = "some secret information"; // keep this data secret
    int perm = int(
        PdfAccess.PDF_PERM_ACCESSIBILITY // always use this
        | PdfAccess.PDF_PERM_PRINT // permit printing
        | PdfAccess.PDF_PERM_COPY // permit copying
        | PdfAccess.PDF_PERM_ANNOTATE // permit annotations
    );
    string owner_pass = "owner"; // owner password
    string user_pass = "user"; // user password
    int encrypt_meth = (int)PdfCrypt.PDF_ENCRYPT_AES_256; // strongest algorithm
    Document doc = new Document(); // empty pdf
    Page page = doc.NewPage(); // empty page
    page.InsertText((50, 72), text); // insert the data
    doc.Save(
        "secret.pdf",
        encryption=encrypt_meth, // set the encryption method
        owner_pw=owner_pass, // set the owner password
        user_pw=user_pass, // set the user password
        permissions=perm, // set permissions
    );



.. note::

    **Taking it further**

    Opening this document with some viewer (Nitro Reader 5) reflects these settings:

    .. image:: ../images/img-encrypting.*

    **Decrypting** will automatically happen on save as before when no encryption parameters are provided.

    To **keep the encryption method** of a PDF save it using `encryption=PdfCrypt.PDF_ENCRYPT_KEEP`. If `doc.CanSaveIncrementally() == true`, an incremental save is also possible.

    To **change the encryption method** specify the full range of options above (`encryption`, `owner_pw`, `user_pw`, `permissions`). An incremental save is **not possible** in this case.

    **API reference**

    - :meth:`Document.Save`

--------------------------




.. _The_Basics_Get_Page_Links:

Getting Page Links
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Links can be extracted from a :ref:`Page` to return :ref:`Link` objects.


.. code-block:: cs

    using MuPDF.NET;

    for (int i = 0; i < doc.PageCount; i ++) // iterate the document pages
    { 
        Page page = doc[i];
        link = page.FirstLink;  // a `Link` object or `None`

        while(link != null) // iterate over the links on page
            // do something with the link, then:
            link = link.Next // get next link, last one has `None` in its `next`
    }



.. note::

    **API reference**

    - :meth:`Page.FirstLink`


-----------------------------


.. _The_Basics_Get_All_Annotations:

Getting All Annotations from a Document
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Annotations (:ref:`Annot`) on pages can be retrieved with the `Page.GetAnnots()` method.

.. code-block:: cs

    using MuPDF.NET;

    for (int i = 0; i < doc.PageCount; i ++)
    { 
        Page page = doc[i];

        List<Entry> annots = page.GetAnnots();

        for (int j = 0; j < annots.Count; j++)
        {
            Console.WriteLine("Annotation on page:"+annots[j]);
        }

    }


.. note::

    **API reference**

    - :meth:`Page.GetAnnots`


--------------------------



.. _The_Basics_Redacting:

Redacting content from a **PDF**
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Redactions are special types of annotations which can be marked onto a document page to denote an area on the page which should be securely removed. After marking an area with a rectangle then this area will be marked for *redaction*, once the redaction is *applied* then the content is securely removed.

For example if we wanted to redact all instances of the name "Jane Doe" from a document we could do the following:


.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("test.pdf"); // open a document

    // Iterate over each page of the document
    for (int i = 0; i < doc.PageCount; i ++)
    {
        Page page = doc[0]

        // Find all instances of "Jane Doe" on the current page
        List<Entry> instances = page.SearchFor("Jane Doe");

        // Redact each instance of "Jane Doe" on the current page
        for (int j = 0; j < instances.Count; j ++) {
            page.AddRedactAnnot(instances[j]);
        }

        // Apply the redactions to the current page
        page.ApplyRedactions();
    }

    doc.Save("redacted_document.pdf");

    doc.Close();

Another example could be redacting an area of a page, but not to redact any line art (i.e. vector graphics) within the defined area, by setting a parameter flag as follows:


.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("test.pdf"); // open a document

    // Get the first page
    Page page = doc[0];

    // Add an area to redact
    rect = new Rect(0,0,200,200);

    // Add a redacction annotation which will have a red fill color
    page.AddRedactAnnot(rect, fill: new float[]{1,0,0});

    // Apply the redactions to the current page, but ignore vector graphics
    page.ApplyRedactions(graphics: 0);

    // Save the modified document
    doc.Save("redacted_document.pdf");

    // Close the document
    doc.Close();


.. warning::

    Once a redacted version of a document is saved then the redacted content in the **PDF** is *irretrievable*. Thus, a redacted area in a document removes text and graphics completely from that area.


.. note::

    **Taking it further**

    The are a few options for creating and applying redactions to a page, for the full API details to understand the parameters to control these options refer to the API reference.

    **API reference**

    - :meth:`Page.AddRedactAnnot`

    - :meth:`Page.ApplyRedactions`


--------------------------








.. include:: ../footer.rst
