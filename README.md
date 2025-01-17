# MuPDF.NET

## About
MuPDF.NET adds C# bindings and abstractions to MuPDF, a lightweight PDF, XPS, and eBook viewer, renderer, and toolkit. Both MuPDF.NET and MuPDF are maintained and developed by Artifex Software, Inc.

## Prerequisites

While being portable to other platforms, this documentation is targeted to the Windows operating system and Visual Studio only.

- Visual Studio Community version 2019 or version 2022
- .NET v8 or later
- Support for Windows and Linux

## Generating MuPDF.NET
This is only required if you want to create a local version of the package. For creating an application that **uses** MuPDF.NET, please skip to the next section.

- Clone this repository.

#### on Windows

- Expand folder `MuPDF.NET` and double-click on file `MuPDF.NET.sln`. This will start your Visual Studio application.

- Select `Release` and `x64` and select `Build|Solution`. Look at the window "Solution Explorer". If you see warnings like in this picture, click on "Install" as indicated to install any missing components.
![alt text](install-image.png)

- Again select `Build|Solution` and make sure the generation is successful.

- Folder `MuPDF.NET` will now contain some DLL files of which you need `mupdfcpp64.dll` and `mupdfcsharp.dll` for all your future mupdf.net applications. DLL `mupdfcpp64.dll` contains the C library MuPDF wrapped with a C++ binding, and `mupdfcsharp.dll` contains the C# bindings for MuPDF.

- Your system administration may determine to put these DLLs in a system folder or provide access to it via a `path` environment variable. If neither of this is the case, both files must be present in the project `bin` folder of your applications.

#### on Linux

- Use the following command to build the project in Release mode:
```sh
dotnet build MuPDF.NET.csproj -c Release
```

- Ensure that you have the native libraries required for the C# bindings. These libraries should be placed in the `bin` folder for Linux support, similar to how they are organized on Windows.

## Creating a MuPDF.NET Application

- Create a C# application using Visual Studio.

- Download and add MuPDF.NET package from [NuGet](https://www.nuget.org/packages/MuPDF.NET/2.0.5-alpha). Refer to it via instruction `using MuPDF.NET;` in your C# source.

- Code your program and build it using the VS menu items `Build|Solution`.

- The generated executable of your app is the `.exe` file in the `bin` folder.


## License and Copyright
MuPDF.NET is available under the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md) (included in this repository as `LICENSE.md` file) and commercial license agreements. If you determine you cannot meet the requirements of the [Artifex Community License](https://github.com/ArtifexSoftware/MuPDF.NET/blob/main/LICENSE.md), please [contact Artifex](https://artifex.com/contact/mupdf-net-inquiry.php) for more information regarding a commercial license.
