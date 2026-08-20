.. include:: header.rst

.. _CompressingFiles:

==============================
Compressing Files
==============================

 
MuPDF.NET & Compression
-------------------------

There are various ways to reduce the size of a PDF file and a variety of options for doing so with the :meth:`Document.Save()` method parameters.

By using the `mupdf_explored <https://mupdf.readthedocs.io/en/1.28.2/cookbook/mupdf-explored.html>`_ PDF as a control file let's look at the effect of various save-time parameters on the file size of a PDF document.

The original file is 1.8 MB in size. Let's see what happens when we save it with various optional parameters defined.


`useObjstms`
~~~~~~~~~~~~~~~~~~~~~~~~~

This boolean option packs object definitions into compressible streams. 

**Example**

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("mupdf_explored.pdf");

    doc.Save(
        "output.pdf",
        useObjstms:true,  // pack object definitions into compressible streams
    );

    doc.Close();

The result is a file size of 1.5 MB. However, we can achieve a better result by using the `deflate` parameter to compress the uncompressed streams.



`deflate`
~~~~~~~~~~~~~~~~~~~~

By setting this option we can compress uncompressed streams.

Available options are:

.. list-table::
   :header-rows: 1

   * - **Value**
     - **Meaning**
   * - `0`
     - No compression (this is the default)
   * - `1`
     - Use standard `Flate compression <https://en.wikipedia.org/wiki/Deflate>`_ 
   * - `2`
     - Use `Brotli compression <https://en.wikipedia.org/wiki/Brotli>`_ (slowest, smallest output, but experimental and unsupported in many tools and viewers) [1]_ 


Used in combination with `useObjstms` to pack object definitions into compressible streams we can achieve even better results.

Flate
"""""""""""""""

**Flate Example**

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("mupdf_explored.pdf");

    doc.Save(
        "output.pdf",
        useObjstms:true,    // pack object definitions into compressible streams
        deflate:1           // compress uncompressed streams with Flate compression
    );
    doc.Close();

The result is a file size of 903 KB.


Brotli
"""""""""""""""

**Brotli Example**

.. warning::

    Brotli is experimental and unsupported in many tools and viewers. [1]_ 


.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("mupdf_explored.pdf");

    doc.Save(
        "output.pdf",
        useObjstms:true,    // pack object definitions into compressible streams
        deflate:2          // compress uncompressed streams with Brotli compression
    );
    doc.Close();

The result is a file size of 863 KB.

Brotli is expected to **yield meaningful differences** on text- and vector-heavy documents, and close to
**none** on scanned or photo-heavy ones.

`compression_effort`
'''''''''''''''''''''

When using Brotli (`deflate:2`) by setting a further parameter `compressionEffort` we can control how hard MuPDF works when compressing stream data. It trades CPU time for file size and *never changes what the document looks like*.

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("mupdf_explored.pdf");

    doc.Save(
        "output.pdf",
        useObjstms:true,      // pack object definitions into compressible streams
        deflate:2,            // compress uncompressed streams with Brotli compression
        compressionEffort:100 // ask Brotli to work hard at it
    );
    doc.Close();

The result now is a file size of 834 KB.


`compressionEffort`: What it does and does not affect
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

Artifex published effort guidance in `Brotli is Here! <https://artifex.com/blog/brotli-is-here>`_
for the equivalent `-e` flag on `mutool clean`:

- **40** — roughly the speed of default Flate compression.
- **75** — roughly the speed of maximum Flate compression.
- **100** — squeezes out everything available, at a noticeably longer runtime.

That guidance is written in the context of Brotli compression, so treat the exact
numbers as indicative.


`compressionEffort` tunes the compressor applied to stream objects as the file is
written. It does not decide *whether* streams are compressed using Brotli — that is `deflate:2`
— and it does not repack object definitions — that is `useObjstms:true`. Ensures that you have correctly set those two parameters before you start tuning `compressionEffort`.

It has an effect on:

- content streams (text and vector graphics),
- font programs and other uncompressed binary streams,
- object streams produced by `useObjstms:true`.

It has little or no effect on:

- images already stored in a compressed format (JPEG, JPEG2000, JBIG2). Re-running
  a general-purpose compressor over them buys nothing. Use
  :meth:`Document.RewriteImages()` for those.
- documents whose bulk is unreferenced objects. Use `garbage:3` or `garbage:4`.


`garbage`
~~~~~~~~~~~~~~~~~~~~

Documents with unreferenced objects can be reduced in size by setting the `garbage` parameter to `3` or `4`. This will de-duplicate and drop unreferenced objects.

.. list-table::
   :header-rows: 1

   * - **Value**
     - **Meaning**
   * - `0`
     - none (default)
   * - `1`
     - remove unused (unreferenced) objects.
   * - `2`
     - in addition to 1, compact the :data:`xref` table.
   * - `3`
     - in addition to 2, merge duplicate objects.
   * - `4`
     - in addition to 3, check :data:`stream` objects for duplication. This may be slow because such data are typically large.


How much is saved here depends on the document, and how much "garbage" it may contain. For example, if we set `garbage:4` we only save a few KB:


.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document("mupdf_explored.pdf"); // open a document

    doc.Save(
        "output.pdf",
        useObjstms:true,       // pack object definitions into compressible streams
        deflate:2,             // compress uncompressed streams with Brotli compression
        compressionEffort:100, // ask Brotli to work hard at it
        garbage:4              // drop unreferenced objects, compact xref table, merge duplicate objects, check streams for duplication
    );
    doc.Close();


Now we have a resulting PDF with a file size of 831 KB.


Results Summary
--------------------

.. list-table::
   :header-rows: 1

   * - **Description**
     - **File size result**
     - **Comments**
   * - Original file
     - 1.8 MB
     -
   * - `use_objstms:true`
     - 1.5 MB
     -
   * - `use_objstms:true`, `deflate:1`
     - 903 KB
     -
   * - `use_objstms:true`, `deflate:2`
     - 863 KB
     - Warning: Brotli is experimental and unsupported in many tools and viewers [1]_ 
   * - `use_objstms:true`, `deflate:2`, `compressionEffort:100`
     - 834 KB
     - Warning: Brotli is experimental and unsupported in many tools and viewers [1]_ 
   * - `use_objstms:true`, `deflate:2`, `compressionEffort:100`, `garbage:4`
     - 831 KB
     - Warning: Brotli is experimental and unsupported in many tools and viewers [1]_ 

See also
--------------------

- :meth:`Document.EzSave()`
- :meth:`Document.RewriteImages()`
- :meth:`Document.SubsetFonts()`
- `Brotli is Here! <https://artifex.com/blog/brotli-is-here>`_



.. [1] If you've used Brotli to compress your PDFs and found that they don't work in your favorite viewer, please try `MUPDF GL <https://mupdf.readthedocs.io/en/1.28.2/tools/mupdf-gl.html>`_. If you still find a problem viewing the file then please file an issue on our `Github tracker <https://github.com/ArtifexSoftware/MuPDF.NET/issues>`_.


.. include:: footer.rst
