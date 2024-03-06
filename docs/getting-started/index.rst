.. include:: ../header.rst

.. _Getting_Started:

Getting Started
===================

Prerequisites
-----------------

- Visual Studio 2019, 2022
- .NET v7 or later
- Windows OS


Usage with Nuget
---------------------

Add **MuPDF.NET** to your existing project by adding the `MuPDF.NET package from NuGet`_.


How to build
----------------------------------

- Clone `the GitHub project`_.
- Open the `MuPDF.NET` solution (`MuPDF.NET.sln`).
- Set the following in **Visual Studio**: `Release`, `x64` and `Build`
- The result is `MuPDF.NET.dll` and `Demo.exe`. Users can use `MuPDF.NET.dll` as a reference in `C#` project and use defined classes and functions. `Demo.exe` is one of examples.

How to use **MuPDF.NET**
----------------------------------

- You need `mupdfcpp64.dll` and `mupdfcsharp.dll`.
- You have to select MuPDF.NET.dll as a reference and use with `using MuPDF.NET;`. For this, you have to copy these files in same level as your **MuPDF.NET** project.

License and Copyright
----------------------------------

**MuPDF.NET** is available under open-source AGPL and commercial license agreements. If you determine that you cannot meet the requirements of the AGPL, please `contact Artifex`_ for more information regarding a commercial license.




.. include:: ../footer.rst


