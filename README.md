# MuPDF.NET

## Prerequistion
- Visual Studio 2019, 2022
- .Net Framework v7 or later
- Windows OS

## How to build
- Clone this project from https://github.com/greendreamer/MuPDF.NET.
- Open `MuPDF.NET` solution.
- Maybe all settings are ready.
- Release, x64 `Build`
- The result is `MuPDF.NET.dll` and `Demo.exe`. Users can use `MuPDF.NET.dll` as a reference in C# project and use defined classes and functions. `Demo.exe` is one of examples.

## How to use CSharpMuPDF
- You need `mupdfcpp64.dll` and `mupdfcsharp.dll`.
- You have to select CSharpMuPDF.dll as a reference and use with `using mupdf;` and `using MuPDF.NET;`. For this, you have to copy these files in same level with MuPDF.NET project.

## After build, you can run on `bin/Release/Demo.exe` for testing
