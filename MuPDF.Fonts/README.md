# MuPDF.Fonts

Font buffers for [MuPDF.NET](https://github.com/ArtifexSoftware/MuPDF.NET), derived from the [pymupdf-fonts](https://github.com/pymupdf/pymupdf-fonts) collection.

## Usage

Reference this package from your project (or use it transitively via `MuPDF.NET`). Font data is exposed through `MuPDF.Fonts.FontRegistry.Descriptors`, which maps short font codes (for example `notos`, `figo`, `casc`) to bold/italic flags and lazy `byte[]` loaders.

```csharp
using MuPDF.Fonts;

if (FontRegistry.Descriptors.TryGetValue("notos", out var entry))
{
    byte[] fontBytes = entry.Loader();
}
```

MuPDF.NET uses these descriptors when resolving pymupdf-fonts style font names.

## Contents

The package embeds compressed font data for families including FiraGO, FiraMono, Noto Sans, Ubuntu, Cascadia Mono, Space Mono, and related variants. See `FontRegistry.Descriptors` for the full list of codes.

## License

Font software is subject to the licenses described in `LICENSE.txt` (primarily SIL Open Font License 1.1 and related notices from the upstream pymupdf-fonts project).

## Dependencies

- [SharpCompress](https://www.nuget.org/packages/SharpCompress) — XZ decompression of embedded font buffers
