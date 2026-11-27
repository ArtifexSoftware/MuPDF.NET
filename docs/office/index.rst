
.. include:: ../header.rst

.. _mupdf-net-office:

MuPDF.NET Office
=============

Enhance |MuPDF.NET| capability with **Office** document support.

- Enables Office document handling, including ``doc``, ``docx``, ``ppt``, ``pptx``, ``xls``, ``xlsx``, ``hwp``, ``hwpx``.
- Supports text and table extraction, document conversion and more.
- Includes everything from |MuPDF.NET|

To enquire about obtaining a commercial license, then `use this contact page <https://artifex.com/contact/?utm_source=rtd-mupdfnet&utm_medium=referral&utm_content=page-link&utm_campaign=docs>`_.

Trial license keys are available for evaluation purposes. Please `fill out the form on this page <https://mupdf.com/mupdf-dotnet/office/try/?utm_source=rtd-mupdfnet&utm_medium=rtd&utm_content=cta-button-mupdfnet-pro-page&utm_campaign=docs>`_ to obtain a trial key.


Office file support
----------------------

In addition to the `standard file types supported by MuPDF.NET <Supported_File_Types>`, |MuPDF.NET Office| supports:

.. list-table::
   :header-rows: 1

   * - **DOC/DOCX**
     - **XLS/XLSX**
     - **PPT/PPTX**
     - **HWP/HWPX**
   * - .. image:: ../images/icons/icon-docx.svg
          :width: 40
          :height: 40
     - .. image:: ../images/icons/icon-xlsx.svg
          :width: 40
          :height: 40
     - .. image:: ../images/icons/icon-pptx.svg
          :width: 40
          :height: 40
     - .. image:: ../images/icons/icon-hangul.svg
          :width: 40
          :height: 40



Usage
--------------

Installation
~~~~~~~~~~~~~~~~~~

Install via Nuget with:

.. code-block:: bash

    dotnet add package MuPDF.NET.Office

Loading an **Office** document
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Import |MuPDF.NET Office| and you can then reference **Office** documents directly, e.g.:

.. code-block:: cs

    using MuPDF.NET;
    using MuPDF.NET.Office;

    MuPDFOffice.Unlock();
    byte[]? pdf = MuPDFOffice.ToPdf("report.docx");
    Document doc = Document.Open(pdf, "pdf");

.. note::

    All standard |MuPDF.NET| functionality is exposed as expected - |MuPDF.NET Office| handles the extended **Office** file types


From then on you can work with document pages just as you would do normally, but with respect to the `restrictions <MuPDFNETOffice_Restrictions>`.


.. _MuPDFNETOffice_Restrictions:

Restrictions
~~~~~~~~~~~~~~~~~~~~


|MuPDF.NET Office| functionality is restricted without a license key as follows:

    **Only the first 3 pages of any document will be available.**

    To unlock full functionality you should `obtain a trial key <https://mupdf.com/mupdf-dotnet/office/try/?utm_source=rtd-mupdfnet&utm_medium=rtd&utm_content=cta-button-mupdfnet-pro-page&utm_campaign=docs>`_.


.. _MuPDFNETOffice_TrialKeys:

Trial keys
-----------------------

To obtain a license key `please fill out the form on this page <https://mupdf.com/mupdf-dotnet/office/try/?utm_source=rtd-mupdfnet&utm_medium=rtd&utm_content=cta-button-mupdfnet-pro-page&utm_campaign=docs>`_. You will then have the trial key emailed to the address you submitted.


Using a key
~~~~~~~~~~~~~~~~


Initialize |MuPDF.NET Office| with a key as follows:

.. code-block:: cs

    using MuPDF.NET.Office;
    MuPDFOffice.Unlock(my_key);
    // MuPDF.NET has now been extended with MuPDF.NET Office features.

This will allow you to evaluate the product for a limited time. If you want to use |MuPDF.NET Office| after this time you should then `enquire about obtaining a commercial license <https://artifex.com/contact/?utm_source=rtd-mupdfnet&utm_medium=referral&utm_content=page-link&utm_campaign=docs>`_.


Converting
-------------

**Office** document to |PDF|
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Use the :meth:`MuPDFOffice.ToPdf()` method to convert an **Office** document to |PDF|, e.g.:

.. code-block:: cs

    using MuPDF.NET.Office;
    MuPDFOffice.Unlock(my_key);

    MuPDFOffice.ToPdf("input.docx", outputPath: "output.pdf");
    

If you require a byte representation of the PDF data, you can use the :meth:`MuPDFOffice.ToPdf` without specifying an output file, e.g.:

.. code-block:: cs

    using MuPDF.NET.Office;
    MuPDFOffice.Unlock(my_key);

    var pdfdata = MuPDFOffice.ToPdf("input.docx");


**Office** document to Images
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

In order to convert an **Office** document to images, you should iterate the document pages, convert to :doc:`../classes/Pixmap` and save, e.g.:

.. code-block:: cs

    using MuPDF.NET;
    using MuPDF.NET.Office;

    var doc = Document.Open("input.docx");

    for (int i = 0; i < doc.PageCount; i++)
    {
        var page = doc[i];
        var pix = page.GetPixmap(dpi: 200);
        pix.Save($"page-{i+1}.png");
    }

    doc.Close();

**Office** document to |Markdown|
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Use the :meth:`MuPDFOffice.ToMarkdown` method to convert an **Office** document to |Markdown|, e.g.:

.. code-block:: cs

    using MuPDF.NET.Office;
    MuPDFOffice.Unlock(my_key);

    MuPDFOffice.ToMarkdown("input.docx", "output.md");

If you require a string representation of the Markdown data, you can use the :meth:`MuPDFOffice.ToMarkdown` method without specifying an output file.

**Office** document to |JSON|
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Use the :meth:`MuPDFOffice.ToJson` method to convert an **Office** document to |JSON|, e.g.:

.. code-block:: cs

    using MuPDF.NET.Office;
    MuPDFOffice.Unlock(my_key);

    MuPDFOffice.ToJson("input.docx", "output.json");

If you require a string representation of the JSON data, you can use the :meth:`MuPDFOffice.ToJson` method without specifying an output file.



API
-----------------------



.. method:: MuPDFOffice.Unlock(string? keyString = null, string? fontPath = null, bool? fontPathAuto = null) -> int

    Unlocks the MuPDF.NET Office functionality with the provided key.

    :arg string? keyString: the license key for |MuPDF.NET Office|.
    :arg string? fontPath: identify specific font directories.
    :arg bool? fontPathAuto: whether to append system font directories.

    :returns: key flags as int. 0 if no key (restricted, 3 pages). :attr:`MuPDFOffice.KeyFlagVolumeLicense` (1) = volume license. :attr:`MuPDFOffice.KeyFlagDisableSo` (2) = SmartOffice disabled. The return can be ignored. Invalid keys throw exceptions.

    Grants access to the full functionality of |MuPDF.NET Office|. For a free trial key please visit: `https://mupdf.com/mupdf-dotnet/office/try/ <https://mupdf.com/mupdf-dotnet/office/try/?utm_source=rtd-mupdfnet&utm_medium=rtd&utm_content=cta-button-mupdfnet-pro-page&utm_campaign=docs>`_.

.. _pymupdf_office_api_to_pdf:

.. method:: MuPDFOffice.ToPdf(string inputPath, string? outputPath = null) -> byte[] | null

    Reads the input file and converts its contents into |PDF| format.

    :arg string inputPath: the input file path.

    :arg string? outputPath: the path to the output file. If `null` (the default), the method returns the PDF content as ``byte[]``.
    
    :returns: ``byte[]`` of the PDF content or `null` if `outputPath` is specified.


.. _pymupdf_office_api_to_markdown:

.. method:: MuPDFOffice.ToMarkdown(string inputPath, string? outputPath = null) -> string | null

    Reads the input file and outputs the text of its pages in |Markdown| format.

    :arg string inputPath: the input file path.

    :arg string outputPath: the path to the output file. If `null`, the method returns the content as a string.

    :returns: string of the Markdown representation or `null` if `outputPath` is specified.



.. _pymupdf_office_api_to_json:

.. method:: MuPDFOffice.ToJson(string inputPath, string? outputPath = null) -> string | null

    Reads the input file and outputs the text of its pages in |JSON| format.

    :arg string inputPath: the input file path.

    :arg string outputPath: the path to the output file. If `null`, the method returns the content as a string.

    :returns: string of the JSON representation or `null` if `outputPath` is specified.


.. method:: MuPDFOffice.Export(string inputPath, string? outputPath = null, string? suffix = null) -> object | null

    Convert an Office document to JSON, Markdown or PDF. :meth:`MuPDFOffice.ToJson`, :meth:`MuPDFOffice.ToMarkdown` and :meth:`MuPDFOffice.ToPdf` call this method.

    :arg string inputPath: the input file path (doc, docx, ppt, pptx, xls, xlsx, hwp, hwpx, and related types).
    :arg string? outputPath: the path to the output file. If `null`, *suffix* must be set and the method returns the converted content.
    :arg string? suffix: `".json"`, `".md"` or `".pdf"`. If `null`, the suffix of *outputPath* is used.

    :returns: Markdown/JSON as ``string``, or PDF as ``byte[]``, when *outputPath* is `null`. Returns `null` when *outputPath* is set. :meth:`MuPDFOffice.Unlock` must have been called first.


.. method:: MuPDFOffice.GetFontPath() -> IReadOnlyList<string>

    Retrieves the list of font paths currently recognized by |MuPDF.NET Office|.

    :returns: an array ``IReadOnlyList<string>`` representing the font paths.


.. attribute:: MuPDFOffice.Version

    Package version string.

    :type: string


.. attribute:: MuPDFOffice.LastFlags

    Flags from the last successful :meth:`MuPDFOffice.Unlock`, or 0 for restricted mode.

    :type: int


.. attribute:: MuPDFOffice.IsUnlocked

    `true` after a successful :meth:`MuPDFOffice.Unlock` (trial, licensed, or restricted mode).

    :type: bool


.. attribute:: MuPDFOffice.KeyFlagVolumeLicense

    Unlock return flag value `1`: volume license.

    :type: int


.. attribute:: MuPDFOffice.KeyFlagDisableSo

    Unlock return flag value `2`: SmartOffice disabled.

    :type: int


.. important::

    See also :meth:`Document.ToMarkdown`, :meth:`Document.ToJson`, :meth:`Document.ToText` and :meth:`Document.ToChunks`.

.. raw:: html

   <button id="findOutAboutMuPDFNETOffice" class="cta orange" onclick="window.location='https://mupdf.com/mupdf-dotnet/office/try/?utm_source=rtd-mupdfnet&utm_medium=rtd&utm_content=cta-button-mupdfnet-office-page&utm_campaign=docs'">Ready to try MuPDF.NET Office?</button>



.. include:: ../footer.rst
