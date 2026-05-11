# Python -> C# Parity Matrix (All Modules)

This matrix extends the conversion traceability beyond `Page`/`Document`.

## Module Mapping

- `src/__init__.py`
  - `Annot` -> `MuPDF.NET/Annot.cs`
  - `Archive` -> `MuPDF.NET/Archive.cs`
  - `Colorspace` -> `MuPDF.NET/Colorspace.cs`
  - `DisplayList` -> `MuPDF.NET/DisplayList.cs`
  - `Document` -> `MuPDF.NET/Document.cs` + `MuPDF.NET/Document.PythonCompat.cs`
  - `DocumentWriter` -> `MuPDF.NET/DocumentWriter.cs`
  - `Font` -> `MuPDF.NET/Font.cs`
  - `Graftmap` -> `MuPDF.NET/Graftmap.cs`
  - `Link` -> `MuPDF.NET/Link.cs` (snake_case mirrors: `is_external`, `id`, `xref`, `next`, `uri`, `page`, `rect`, `dest`, `to_dict`, `set_border` / `set_colors` / `set_flags`, etc.; use `Flags` for the flags getter; `Dest` / `dest` -> `LinkDest`)
  - `linkDest` -> `MuPDF.NET/LinkDest.cs`; `Outline` also exposes `LinkDest` / `dest` / `Destination(Document)` (native x/y rect remains `Outline.Dest`)
  - `Matrix` -> `MuPDF.NET/Geometry/Matrix.cs`
  - `Outline` -> `MuPDF.NET/Outline.cs`
  - `Page` -> `MuPDF.NET/Page.cs` + `MuPDF.NET/Page.PythonCompat.cs`
  - `Pixmap` -> `MuPDF.NET/Pixmap.cs`
  - `Point` -> `MuPDF.NET/Geometry/Point.cs`
  - `Quad` -> `MuPDF.NET/Geometry/Quad.cs`
  - `Rect` -> `MuPDF.NET/Geometry/Rect.cs`
  - `IRect` -> `MuPDF.NET/Geometry/IRect.cs`
  - `Shape` -> `MuPDF.NET/Shape.cs`
  - `Story` -> `MuPDF.NET/Story.cs`
  - `TextPage` -> `MuPDF.NET/TextPage.cs`
  - `TextWriter` -> `MuPDF.NET/TextWriter.cs`
  - `Widget` -> `MuPDF.NET/Widget.cs`
  - Python exception names (`FileDataError`, `FileNotFoundError`, `EmptyFileError`) -> aliases added in `MuPDF.NET/Exceptions.cs`

- `src/table.py`
  - `Table`, `TableSettings`, `TableFinder`, related row/header/group classes -> `MuPDF.NET/Table.cs`

- `src/utils.py`, `src/fitz_utils.py`
  - Utility/helper behavior -> `MuPDF.NET/Utils.cs`, `MuPDF.NET/Helpers.cs`

- `src/_wxcolors.py`
  - Color map helpers -> `MuPDF.NET/WxColors.cs`

## `extra.i` Alignment Summary

- Directly represented helper names:
  - `JM_add_oc_object` exists in `Helpers.cs`
  - `JM_insert_contents` exists in `Helpers.cs`
  - `JM_set_resource_property` exists in `Helpers.cs`
  - `JM_xobject_from_page` exists in `Helpers.cs`
  - `JM_refresh_links`, `JM_get_annot_xref_list` (annot xref / type / `/NM` list) exist in `Helpers.cs` for PDF link lifecycle parity with `Page.get_links` / `Page.delete_link` flows
- Behavior represented via C# APIs:
  - annotation id/xref listing (`AnnotNames`, `AnnotXrefs`)
  - stream extraction helpers (`fz_buffer_extract` paths)
- Known outstanding strict-parity internals:
  - exact `_show_pdf_page` Form XObject chain parameter/typing parity still differs in public surface shape

## Current Compatibility Additions

- `MuPDF.NET/Page.PythonCompat.cs`
  - Python snake_case wrappers for major `Page` APIs, including `add_*`, `get_*`, `insert_*`, `show_pdf_page`, `set_*`, and drawing/content helpers.
- `MuPDF.NET/Document.PythonCompat.cs`
  - Python snake_case wrappers for major `Document` APIs, including `load_page`, `save/write`, xref/object functions, metadata, journaling, and cleanup helpers.
- `MuPDF.NET/Exceptions.cs`
  - Python exception aliases added.

## Validation Notes

- `dotnet build MuPDF.NET/MuPDF.NET.csproj` succeeds for **`net8.0`**, **`net472`**, **`net48`**, and **`netstandard2.0`** (many pre-existing XML-doc warnings from generated SWIG sources).
- Added compatibility wrappers integrate in the current code structure and provide side-by-side traceability anchors for Python method names.
