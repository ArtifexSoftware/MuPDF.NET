# Changelog

### [3.2.10] - 2025-10-07
- Replaced all Windows System.Drawing dependencies with SkiaSharp.
- Added support for 32-bit .NET projects in Visual Studio 2019.
- Fixed issues with Unicode file names.

### [3.2.10-rc.5] - 2025-09-16
- Removed margin parameter in write barcode.
- Added new marginLeft,marginTop,marginRight,marginBottom parameters to barcode creation.

### [3.2.10-rc.3] - 2025-09-12
- Upgrade MuPDF.NativeAssets to 1.26.8.
- Added a new method "Utils.GetBarcodePixmap" into barcode write module.

### [3.2.10-rc.2] - 2025-09-11
- Added a new parameter "narrowBarWidth" into barcode write module.

### [3.2.10-rc.1] - 2025-09-03
- Added a new barcode rendering engine.
- Removed all dependencies on ZXing.

### [3.2.9] - 2025-08-28
- Deployed new release

### [3.2.9-rc.15] - 2025-08-22
- Fixed PDF file handle leak caused by unreleased FzPage in DisplayList.
- Resolved memory leak in DocumentWriter (caused by missing FilePtrOutput).
- Added Dispose() in Story.
- Added Dispose() in TextPage.

### [3.2.9-rc.11] - 2025-08-12
- Updated barcode reader for low quality images/pages.