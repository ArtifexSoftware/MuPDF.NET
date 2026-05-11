# Python -> C# Parity Matrix (Phase 1)

This document tracks strict traceability for `src/__init__.py` class conversion into `MuPDF.NET`.

## Scope

- Python `Document` (`src/__init__.py`) -> C# `Document` (`MuPDF.NET/Document.cs`, `MuPDF.NET/Document.PythonCompat.cs`)
- Python `Page` (`src/__init__.py`) -> C# `Page` (`MuPDF.NET/Page.cs`, `MuPDF.NET/Page.PythonCompat.cs`)
- SWIG helper surface review for `src/extra.i` -> `MuPDF.NET/Helpers.cs` and C# wrappers

## Method Inventory Snapshot

- `Document`
  - Python methods discovered: `180`
  - C# public methods pre-compat: `93`
  - Name-normalized matches pre-compat: `83`
  - Action: added Python-name wrappers in `Document.PythonCompat.cs` for major public API paths (loading, save/write, xref, metadata, embfile, journalling, insert_pdf, scrub/rewrite, page helpers).
- `Page`
  - Python methods discovered: `148`
  - C# public methods pre-compat: `87`
  - Name-normalized matches pre-compat: `88`
  - Action: added Python-name wrappers in `Page.PythonCompat.cs` for major public API paths (annots/links/widgets, add_* annot methods, text extraction, insert_* methods, box setters, content/graphics helpers, show_pdf_page, drawing/table helpers).

## Comment/Traceability Mapping

- Added dedicated compatibility partial classes with explicit top-level comments:
  - `Page.PythonCompat.cs`: "preserve naming traceability to src/__init__.py:class Page"
  - `Document.PythonCompat.cs`: "preserve naming traceability to src/__init__.py:class Document"
- Existing method-order in `Page.cs` / `Document.cs` remains unchanged; wrappers provide line-by-line comparison anchors for Python names while retaining existing .NET API.

## `extra.i` Dependency Notes

Observed helper families in `src/extra.i` that are relevant to strict parity:

- Implemented or partially represented in C#:
  - `JM_add_oc_object` -> `Helpers.JM_add_oc_object`
  - Annotation id/xref list behavior -> represented by `Page.AnnotNames()` and `Page.AnnotXrefs()`
  - Buffer extraction semantics -> represented with `fz_buffer_extract` extension usage
  - Link table refresh / listing parity -> `Helpers.JM_refresh_links`, `Helpers.JM_get_annot_xref_list`, `Helpers.PdfAnnotNmForXref`; `Page.DeleteLink(Link)`, `Page.DeleteLink(Dictionary<...>)` (dict path mirrors `delete_link` + `finished()`), `InsertLink(..., mark: bool = true)` exposes Python’s unused `mark` flag; `InsertLink` / `SetLinks` refresh paths unchanged. `Page._addAnnot_FromString` delegates each string to `Helpers.AppendPdfAnnotFromObjectString` then runs `JM_refresh_links` + `SyncLinkWrapperCache` once per batch (C# keeps the MuPDF link list in sync; PyMuPDF’s `extra.i` path does not call `JM_refresh_links`). Dicts with `kind` use `Helpers.TryBuildInsertLinkAnnotObjectString` (port of `utils.getLinkText` + `/NM` allocation) and `AppendPdfAnnotFromObjectString`; legacy dicts without `kind` still use the small URI/Dest builder. `Page.FirstLink` delegates to `LoadLinks()` (Python `first_link` -> `load_links()`). `Page.LoadLinks`, `InsertLink` (return value), and `Link.Next` use `Link.SetLinkAnnotIdentity` so xref/`/NM` follow the `JM_get_annot_xref_list` link-annot order (Python `load_links` first row and `Link.next` when prior `xref > 0`); otherwise `Link.Xref` / `Link.Id` fall back to rect match + `PdfAnnotNmForXref`. `Link.ToDictionary` / `GetLinks` call `Helpers.EnrichLinkDictFromPdfAnnot` when `xref > 0` to fill `kind`, `uri`, `page`, `to`, `zoom`, `file`, `name` from `/A` and `/Dest` like `utils.getLinkDict`; `GetLinks` still merges `xref` / `id` when annot xref counts match link count (same gate as Python `get_links`). `Link.IsExternal` uses `mupdf.fz_is_external_link` on the URI (Python `Link.is_external`), with a narrow `http`/`mailto`/`ftp` fallback if the native call throws. `Link.Dest` / `link.dest` builds `LinkDest` using `Document.ResolveLink` when not external and not `#...` (Python `Link.dest`); `Outline.LinkDest` / `outline.dest` and `Outline.Destination(Document)` mirror `Outline.dest` / `Outline.destination`; `Outline.IsExternal` uses `fz_is_external_link` like Python. `LinkDest` ports `linkDest` (`#page=` / `#nameddest=` / `file:` / URI vs launch). `Link._erase` and per-page weak caches (`_linkRefsByXref`, `_linkRefsByNm` via `TryGetCachedLinkByAnnotNm`) support wrapper teardown; Python `finished()` indexes `_annot_refs` with `linkdict["id"]` (PDF `/NM`) while `load_links` / `Link.next` register under `id(link)` — C# keys wrappers by xref and by non-empty `/NM` so dict-based deletion can detach live `Link` instances when those caches are warm.
- Still missing direct helper equivalents (or only behaviorally approximated):
  - `_show_pdf_page` Form XObject pipeline is now helper-backed, but still differs in API typing shape from Python internals
  - Several low-level TOC/outline and merge internals that exist in `extra.i` but not as direct C# helper names

## Phase 1 Remaining Gaps

- `show_pdf_page` internals now call explicit helper ports (`JM_xobject_from_page`, `JM_insert_contents`) but can still be tightened for exact Python private-API signatures.
- Python-private helper names coverage improved (`_set_resource_property`, `_show_pdf_page`, `_pdf_page`, `_count_q_balance`, `_get_resource_properties`, `_get_optional_content`, `_set_pagebox`, `_other_box`, `_reset_annot_refs`, `_erase`, `_get_textpage`, `_set_opacity`, `_apply_redactions`, `_load_annot`, `_insertFont`, `_makePixmap`, `_insert_image`, `_addWidget`, `_addAnnot_FromString`, `_add_*` annot helper family added). `Page._annot_refs` is now represented by a per-page weak wrapper map in `Page.cs` (register on `Annot` construction, clear on `_reset_annot_refs`); other private names may still need explicit ports.
- `Document` private helper-name coverage has been extended in `Document.PythonCompat.cs` (`_delToC`, `_delete_page`, `_deleteObject`, `_embeddedFileGet`, `_embeddedFileIndex`, `_embfile_*`, `_get_page_labels`, `_set_page_labels`, `_getMetadata`, `_getOLRootNumber`, `_getPDFfileid`, `_getPageInfo`, `_loadOutline`, `_newPage`, `_remove_toc_item`, `_update_toc_item`, `_addFormFont`, `_insert_font`, `_remove_links_to`, `_forget_page`, `_reset_page_refs`). Page-ref tracking is implemented in `Document.cs` to mirror Python `Document._page_refs` + `_reset_page_refs` invalidation semantics (weak-value-dictionary analogue + `Page` teardown).
- Added parser-backed helper `Helpers.JM_pdf_obj_from_str`, and wired it into `Document.XrefSetKey`, `Document.UpdateObject`, and TOC action update paths for closer Python object-string parity.
- Python comment bodies are not yet copied verbatim for every mapped method.

## Validation Status

- `dotnet build MuPDF.NET/MuPDF.NET.csproj` succeeds for **`net8.0`**, **`net472`**, **`net48`**, and **`netstandard2.0`** (large pre-existing XML-doc warning volume from generated SWIG sources).
- Older TFMs use small shims in-repo: **`MemberNotNullPolyfill.cs`** (`MemberNotNullAttribute`), **`Helpers.PythonTupleLikeCount` / `PythonTupleLikeItem`** (reflection over `ITuple` for `_addAnnot_FromString`), and BCL-safe **`IndexOf` / `Replace`** patterns where newer overloads are unavailable.
- Phase-1 touched sources (`Document.cs`, `Page.cs`, `*.PythonCompat.cs`, `Helpers.cs`, polyfills) compile cleanly aside from those repo-wide warnings.
