.. include:: ../../header.rst

.. _Migration_ByteScoutSDK:


Migrating from ByteScout SDK
====================================


This guide is designed to help you transition your application from |ByteScout SDK| to |MuPDF.NET| by finding the equivalent methods and techniques to achieve your goals.





Setting Up the Environment
---------------------------------------------------------



.. tabs::

   .. tab:: |ByteScout|

      The |ByteScout SDK| will already be linked and integrated into your application for example:

      .. code-block:: cs

        // Sample setup with ByteScout SDK
        using Bytescout.PDFExtractor;


   .. tab:: |MuPDF|

      To start using |MuPDF.NET|, install the package via `NuGet`_:

      .. code-block:: shell

        dotnet add package MuPDF.NET --version 2.1.1


      To make available to your application code, use it with:

      .. code-block:: cs

        using MuPDF.NET


Loading a PDF Document
-----------------------------

.. tabs::

   .. tab:: |ByteScout|

      .. code-block:: cs

        TextExtractor textExtractor = new TextExtractor();
        textExtractor.LoadDocumentFromFile("sample.pdf");


   .. tab:: |MuPDF|

      .. code-block:: cs

        Document doc = new Document("sample.pdf");



Extracting Text from a Page
-------------------------------------

.. tabs::

   .. tab:: |ByteScout|

      .. code-block:: cs

        TextExtractor textExtractor = new TextExtractor();
        textExtractor.LoadDocumentFromFile("sample.pdf");
        string extractedText = textExtractor.GetText();
        Console.WriteLine(extractedText);


   .. tab:: |MuPDF|

      |MuPDF.NET| provides several ways to extract text, but two methods are introduced below:

      .. code-block:: cs

        Document doc = new Document("sample.pdf");
        TextPage textPage = doc.LoadPage(0).GetTextPage();
        string extractedText = textPage.ExtractText();
        Console.WriteLine(extractedText);

      .. code-block:: cs

        Document doc = new Document("sample.pdf");
        Page page = doc[0];
        foreach (var textBlock in page.GetTextBlocks())
        {
            Console.WriteLine(textBlock.Text);
        }



Converting a PDF Page to an Image
--------------------------------------------------------------------------


.. tabs::

   .. tab:: |ByteScout|

      .. code-block:: cs

        RasterRenderer renderer = new RasterRenderer();
        renderer.LoadDocumentFromFile("sample.pdf");
        renderer.Save("sample.png", RasterImageFormat.PNG, 0, 75);


   .. tab:: |MuPDF|

      MuPDF.NET requires rendering a page to a specific resolution. You can specify the DPI for higher quality.

      .. code-block:: cs

        Document doc = new Document("sample.pdf");
        Page page = doc[0]; // get the first page of the document
        Pixmap pixmap = page.GetPixmap(IdentityMatrix(), 300); // requests a DPI of 300 for the Pixmap
        pixmap.Save("sample.png", "PNG");







.. include:: ../../footer.rst
