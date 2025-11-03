.. include:: ../header.rst

.. _Getting_Started:

Getting Started
===================

Prerequisites
-----------------

- Visual Studio 2019, 2022
- .NET 8.0 or later
- Windows OS / Linux


Usage with Nuget
---------------------


The steps to install and run MuPDF.NET in a standard .NET console environment from the command line are described below.


Install DotNet
~~~~~~~~~~~~~~~~~

First, make sure you have the .NET SDK installed:

.. code-block:: bash

    # Check if .NET is installed
    dotnet --version

    # If not installed, install it (Ubuntu/Debian example)
    sudo apt update
    sudo apt install dotnet-sdk-8.0

Or, if on Windows, go to `the Official Installer from Microsoft <https://dotnet.microsoft.com/download>`_.


Create a project
~~~~~~~~~~~~~~~~~

Let's create a simple example project called "MyProject" and run it:

.. code-block:: bash

    # Create a new console app
    dotnet new console -n MyProject

    # Navigate to the project directory
    cd MyProject

    # Run the project
    dotnet run

This starter project should just print `"Hello, World!"` once run.


Add the package
~~~~~~~~~~~~~~~~~

Now add the **MuPDF.NET** package to your existing project by adding the `MuPDF.NET package from NuGet`_:

.. code-block:: bash

    dotnet add package MuPDF.NET


Use the package
~~~~~~~~~~~~~~~~~

Let's try creating a PDF and adding some text.

Open your project and edit the `Program.cs` file


Edit the `Program.cs` file
"""""""""""""""""""""""""""

Add the following code:

.. code-block:: cs

    using MuPDF.NET;

    Document doc = new Document();
    Page page = doc.NewPage();
    string text = "Made with MuPDF.NET"; // define some text!
    MuPDF.NET.Font font = new MuPDF.NET.Font("helv"); // define a font to use, in this case Helvetica
    MuPDF.NET.TextWriter tw = new MuPDF.NET.TextWriter(page.Rect); // define the rectangle for the text
    tw.Append(new(50, 100), text, font); // define the point where you want to add the text
    tw.WriteText(page); // use the TextWriter to write the text to the page
    doc.Save("hello_world.pdf"); // save the result!


Save & Run
""""""""""""""""

Save the file and again run with:

.. code-block:: bash

    dotnet run


You should now have a PDF file in the project folder ready to view - all created with MuPDF.NET!


Try more
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

See further examples in :doc:`../the-basics/index` for the typical kind of things you may want to do. If you get stuck, have new ideas, find something that you can't do or have a feature request please visit the `MuPDF Blog`_ and let us know.



How to build
----------------------------------

- Clone `the GitHub project`_.
- Open the `MuPDF.NET` solution (`MuPDF.NET.sln`).
- Set the following in **Visual Studio**: `Release`, `x64` and `Build`
- The result is `MuPDF.NET.dll` and `Demo.exe`. Users can use `MuPDF.NET.dll` as a reference in `C#` project and use defined classes and functions. `Demo.exe` is one of examples.



.. _AdobeManual:

Adobe PDF References
---------------------------

.. note:: This PDF Reference manual published by **Adobe** is frequently quoted throughout this documentation. It can be viewed and downloaded from `opensource.adobe.com <https://opensource.adobe.com/dc-acrobat-sdk-docs/standards/pdfstandards/pdf/PDF32000_2008.pdf>`_.




License and Copyright
----------------------------------

**MuPDF.NET** is available under the `Artifex Community License`_ and commercial license agreements. If you determine you cannot meet the requirements of the `Artifex Community License`_, please `contact Artifex`_ for more information regarding a commercial license.




.. include:: ../footer.rst


