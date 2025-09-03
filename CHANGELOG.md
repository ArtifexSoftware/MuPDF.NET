# Changelog

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