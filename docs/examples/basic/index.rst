.. include:: ../../header.rst

==============================
01-Basic
==============================

Open a PDF and read document and page information.

GitHub: `01-Basic <https://github.com/ArtifexSoftware/MuPDF.NET.Examples/tree/main/MuPDF.NET/01-Basic>`_ in `MuPDF.NET.Examples <https://github.com/ArtifexSoftware/MuPDF.NET.Examples>`_.

.. code-block:: cs

    using MuPDF.NET;

    using (var doc = Document.Open(@"D:\basic.pdf"))
    {
        Encryption(doc);
        doc.Authenticate("user");
        DocumentIdentity(doc);
        ...
    }

API: :meth:`Document.Document`.


See whether the PDF is encrypted or still needs a password
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

``basic.pdf`` is AES-256 encrypted. ``Basic()`` calls this method before ``doc.Authenticate("user")``.

.. code-block:: cs

    /// <summary>See whether the PDF is encrypted or still needs a password</summary>
    static void Encryption(Document doc)
    {
        Console.WriteLine($"doc.IsEncrypted = {doc.IsEncrypted}");
        Console.WriteLine($"doc.NeedsPass = {doc.NeedsPass}");
    }

API: :attr:`Document.IsEncrypted`, :attr:`Document.NeedsPass`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 1-2


Open a PDF and read its page count and kind
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Open a PDF and read its page count, kind, and file name</summary>
    static void DocumentIdentity(Document doc)
    {
        Console.WriteLine($"doc.PageCount = {doc.PageCount}");
        Console.WriteLine($"doc.IsPdf = {doc.IsPdf}");
    }

API: :attr:`Document.PageCount`, :attr:`Document.IsPDF`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 3-4


Read document state flags
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Read document state flags</summary>
    static void DocumentFlags(Document doc)
    {
        Console.WriteLine($"doc.IsReflowable = {doc.IsReflowable}");
        Console.WriteLine($"doc.IsDirty = {doc.IsDirty}");
        Console.WriteLine($"doc.IsFormPdf = {doc.IsFormPdf}");
        Console.WriteLine($"doc.IsRepaired = {doc.IsRepaired}");
        Console.WriteLine($"doc.IsFastWebaccess = {doc.IsFastWebaccess}");
        Console.WriteLine($"doc.IsClosed = {doc.IsClosed}");
        Console.WriteLine($"doc.CanSaveIncrementally() = {doc.CanSaveIncrementally()}");
        Console.WriteLine($"doc.JournalIsEnabled = {doc.JournalIsEnabled}");
    }

API: :attr:`Document.IsReflowable`, :attr:`Document.IsDirty`, :attr:`Document.IsFormPDF`, :attr:`Document.IsRepaired`, :attr:`Document.IsFastWebaccess`, :attr:`Document.IsClosed`, :meth:`Document.CanSaveIncrementally`, :meth:`Document.IsEnabledJournal`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 5-12


Read PDF structure: chapters, versions, xref length, and catalog
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Read PDF structure: chapters, versions, xref length, and catalog</summary>
    static void PdfStructure(Document doc)
    {
        Console.WriteLine($"doc.ChapterCount = {doc.ChapterCount}");
        for (int c = 0; c < doc.ChapterCount; c++)
            Console.WriteLine($"doc.ChapterPageCount({c}) = {doc.ChapterPageCount(c)}");
        Console.WriteLine($"doc.VersionCount = {doc.VersionCount}");
        Console.WriteLine($"doc.XrefLength = {doc.XrefLength}");
        Console.WriteLine($"doc.PdfCatalog = {doc.PdfCatalog}");
    }

API: :attr:`Document.ChapterCount`, :meth:`Document.GetChapterPageCount`, :attr:`Document.VersionCount`, :meth:`Document.GetXrefLength`, :meth:`Document.GetPdfCatelog`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 13-17


Read catalog viewer settings
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Read catalog viewer settings</summary>
    static void ViewerSettings(Document doc)
    {
        Console.WriteLine($"doc.Language = {doc.Language}");
        Console.WriteLine($"doc.PageLayout = {doc.PageLayout}");
        Console.WriteLine($"doc.PageMode = {doc.PageMode}");
        Console.WriteLine($"doc.NeedAppearances = {doc.NeedAppearances}");
        Console.WriteLine($"doc.GetSigFlags() = {doc.GetSigFlags()}");
    }

API: :attr:`Document.Language`, :attr:`Document.PageLayout`, :attr:`Document.PageMode`, :meth:`Document.NeedAppearances`, :meth:`Document.GetSigFlags`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 18-22


Read the permission bitmask
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Read the permission bitmask</summary>
    static void Permissions(Document doc)
    {
        int perm = doc.Permissions;
        Console.WriteLine($"doc.Permissions = {perm}");
        Console.WriteLine($"Constants.PDF_PERM_PRINT = {(perm & Constants.PDF_PERM_PRINT) != 0}");
        Console.WriteLine($"Constants.PDF_PERM_MODIFY = {(perm & Constants.PDF_PERM_MODIFY) != 0}");
        Console.WriteLine($"Constants.PDF_PERM_COPY = {(perm & Constants.PDF_PERM_COPY) != 0}");
        Console.WriteLine($"Constants.PDF_PERM_ANNOTATE = {(perm & Constants.PDF_PERM_ANNOTATE) != 0}");
        Console.WriteLine($"Constants.PDF_PERM_FORM = {(perm & Constants.PDF_PERM_FORM) != 0}");
        Console.WriteLine($"Constants.PDF_PERM_ACCESSIBILITY = {(perm & Constants.PDF_PERM_ACCESSIBILITY) != 0}");
        Console.WriteLine($"Constants.PDF_PERM_ASSEMBLE = {(perm & Constants.PDF_PERM_ASSEMBLE) != 0}");
        Console.WriteLine($"Constants.PDF_PERM_PRINT_HQ = {(perm & Constants.PDF_PERM_PRINT_HQ) != 0}");
    }

API: :attr:`Document.Permissions`, :ref:`PermissionCodes`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 23-31


List AcroForm font names
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>List AcroForm font names</summary>
    static void FormFonts(Document doc)
    {
        List<string> formFonts = doc.FormFonts;
        Console.WriteLine($"doc.FormFonts = {(formFonts == null ? "(null)" : formFonts.Count + " item(s)")}");
        if (formFonts != null)
        {
            foreach (string name in formFonts)
                Console.WriteLine($"  {name}");
        }
    }

API: :attr:`Document.FormFonts`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 32-33


List embedded file names
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>List embedded file names</summary>
    static void EmbeddedNames(Document doc)
    {
        Console.WriteLine($"doc.EmbeddedFileCount = {doc.EmbeddedFileCount}");
        foreach (string name in doc.GetEmbeddedFileNames())
            Console.WriteLine($"doc.GetEmbeddedFileNames(): {name}");
    }

API: :meth:`Document.GetEmbfileCount`, :meth:`Document.GetEmbfileNames`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 34-35


Check whether the PDF has any annotations or links
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Check whether the PDF has any annotations or links</summary>
    static void AnnotsAndLinks(Document doc)
    {
        Console.WriteLine($"doc.HasAnnots() = {doc.HasAnnots()}");
        Console.WriteLine($"doc.HasLinks() = {doc.HasLinks()}");
    }

API: :meth:`Document.HasAnnots`, :meth:`Document.HasLinks`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 36-37


Read MarkInfo flags from the catalog
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Read MarkInfo flags from the catalog</summary>
    static void MarkInfo(Document doc)
    {
        Console.WriteLine($"doc.MarkInfo.Count = {doc.MarkInfo.Count}");
        foreach (var kv in doc.MarkInfo)
            Console.WriteLine($"doc.MarkInfo[\"{kv.Key}\"] = {kv.Value}");
    }

API: :attr:`Document.MarkInfo`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 38-41


Read the standard PDF Info dictionary
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Read the standard PDF Info dictionary</summary>
    static void ReadMetadata(Document doc)
    {
        Dictionary<string, string> meta = doc.Metadata;
        Console.WriteLine($"doc.Metadata.Count = {meta.Count}");
        foreach (var kv in meta)
            Console.WriteLine($"doc.Metadata[\"{kv.Key}\"] = {kv.Value}");
    }

API: :attr:`Document.MetaData`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 42-53


Read the XMP metadata stream
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Read the XMP metadata stream</summary>
    static void XmlMetadata(Document doc)
    {
        Console.WriteLine($"doc.XrefXmlMetadata = {doc.XrefXmlMetadata}");
        string xml = doc.GetXmlMetadata();
        Console.WriteLine($"doc.GetXmlMetadata() = {xml}");
    }

API: :meth:`Document.XrefXmlMetaData`, :meth:`Document.GetXmlMetadata`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 54-69


Read page label rules
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Read page label rules</summary>
    static void PageLabels(Document doc)
    {
        List<Dictionary<string, object>> labels = doc.GetPageLabels();
        Console.WriteLine($"doc.GetPageLabels().Count = {labels.Count}");
        foreach (Dictionary<string, object> rule in labels)
        {
            foreach (var kv in rule)
                Console.WriteLine($"  {kv.Key} = {kv.Value}");
        }
    }

API: :meth:`Document.GetPageLabels`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 70-82


Read the table of contents
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Read the table of contents</summary>
    static void TableOfContents(Document doc)
    {
        TocResult toc = doc.GetToc(simple: true);
        Console.WriteLine($"doc.GetToc().Count = {toc.Count}");
        foreach (var item in toc)
            Console.WriteLine($"  level={item.level} title={item.title} page={item.page}");
    }

API: :meth:`Document.GetToc`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 83-95


Resolve named destinations
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Resolve named destinations</summary>
    static void NamedDestinations(Document doc)
    {
        Dictionary<string, Dictionary<string, object>> names = doc.ResolveNames();
        Console.WriteLine($"doc.ResolveNames().Count = {names.Count}");
        foreach (var dest in names)
        {
            Console.WriteLine($"  {dest.Key}");
            foreach (var kv in dest.Value)
                Console.WriteLine($"    {kv.Key} = {kv.Value}");
        }
    }

API: :meth:`Document.ResolveNames`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 96-112


Read optional content groups and layer UI
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Read optional content groups and layer UI</summary>
    static void OptionalContent(Document doc)
    {
        Dictionary<int, Dictionary<string, object>> ocgs = doc.GetOcgs();
        Console.WriteLine($"doc.GetOcgs().Count = {ocgs.Count}");
        foreach (var ocg in ocgs)
        {
            Console.WriteLine($"  xref={ocg.Key}");
            foreach (var kv in ocg.Value)
                Console.WriteLine($"    {kv.Key} = {kv.Value}");
        }
        List<Dictionary<string, object>> layers = doc.GetLayers();
        Console.WriteLine($"doc.GetLayers().Count = {layers.Count}");
        foreach (Dictionary<string, object> layer in layers)
        {
            foreach (var kv in layer)
                Console.WriteLine($"  {kv.Key} = {kv.Value}");
        }
        List<Dictionary<string, object>> layerUi = doc.LayerUiConfigs();
        Console.WriteLine($"doc.LayerUiConfigs().Count = {layerUi.Count}");
        foreach (Dictionary<string, object> ui in layerUi)
        {
            foreach (var kv in ui)
                Console.WriteLine($"  {kv.Key} = {kv.Value}");
        }
    }

API: :meth:`Document.GetOcgs`, :meth:`Document.GetLayers`, :meth:`Document.LayerUIConfigs`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 113-140


Read the PDF trailer dictionary
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Read the PDF trailer dictionary</summary>
    static void Trailer(Document doc)
    {
        Console.WriteLine($"doc.PdfTrailer() = {doc.PdfTrailer()}");
    }

API: :meth:`Document.GetPdfTrailer`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 141-168


Write a full copy of the open PDF
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Write a full copy of the open PDF</summary>
    static void SaveCopy(Document doc, string output)
    {
        doc.Save(output);
    }

API: :meth:`Document.Save`.


Read each page's boxes, fonts, images, XObjects, and annotation xrefs
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code-block:: cs

    /// <summary>Read each page's boxes, fonts, images, XObjects, and annotation xrefs</summary>
    static void PageProperties(Document doc)
    {
        for (int i = 0; i < doc.PageCount; i++)
        {
            using Page page = doc[i];
            Console.WriteLine($"doc[{i}].GetLabel() = {page.GetLabel()}");
            Console.WriteLine($"doc[{i}].Xref = {page.Xref}");
            Console.WriteLine($"doc[{i}].Rotation = {page.Rotation}");
            Console.WriteLine($"doc[{i}].Width = {page.Width}");
            Console.WriteLine($"doc[{i}].Height = {page.Height}");
            Console.WriteLine($"doc[{i}].Rect = {page.Rect}");
            Console.WriteLine($"doc[{i}].MediaBox = {page.MediaBox}");
            Console.WriteLine($"doc[{i}].CropBox = {page.CropBox}");
            Console.WriteLine($"doc[{i}].BleedBox = {page.BleedBox}");
            Console.WriteLine($"doc[{i}].TrimBox = {page.TrimBox}");
            Console.WriteLine($"doc[{i}].ArtBox = {page.ArtBox}");

            List<(int xref, string ext, string type, string baseName, string name, string encoding, int? referencer)> fonts =
                doc.GetPageFonts(i);
            Console.WriteLine($"doc.GetPageFonts({i}).Count = {fonts.Count}");
            foreach (var font in fonts)
                Console.WriteLine($"  xref={font.xref} ext={font.ext} type={font.type} baseName={font.baseName} name={font.name} encoding={font.encoding}");

            List<Entry> images = doc.GetPageImages(i);
            Console.WriteLine($"doc.GetPageImages({i}).Count = {images.Count}");
            foreach (Entry img in images)
                Console.WriteLine($"  xref={img.Xref} name={img.Name} width={img.Width} height={img.Height} bpc={img.Bpc} cs={img.CsName}");

            List<Dictionary<string, object>> xobjects = doc.GetPageXobjects(i);
            Console.WriteLine($"doc.GetPageXobjects({i}).Count = {xobjects.Count}");
            foreach (Dictionary<string, object> xo in xobjects)
            {
                foreach (var kv in xo)
                    Console.WriteLine($"  {kv.Key} = {kv.Value}");
            }

            List<(int xref, AnnotationType type, string id)> annots = doc.GetPageAnnotXrefs(i);
            Console.WriteLine($"doc.GetPageAnnotXrefs({i}).Count = {annots.Count}");
            foreach (var annot in annots)
                Console.WriteLine($"  xref={annot.xref} type={annot.type} id={annot.id}");
        }
    }

API: :meth:`Document.LoadPage`, :meth:`Page.GetLabel`, :attr:`Page.Xref`, :attr:`Page.Rotation`, :attr:`Page.Rect`, :attr:`Page.MediaBox`, :attr:`Page.CropBox`, :attr:`Page.BleedBox`, :attr:`Page.TrimBox`, :attr:`Page.ArtBox`, :meth:`Document.GetPageFonts`, :meth:`Document.GetPageImages`, :meth:`Document.GetPageXObjects`, :meth:`Page.GetAnnotXrefs`.

Result:

.. literalinclude:: document-info.txt
   :language: text
   :lines: 169-396


.. include:: ../../footer.rst
