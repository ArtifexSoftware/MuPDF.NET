# MuPDF.NativeAssets packaging

Native SWIG binaries are split into **platform-specific NuGet packages**. `MuPDF.NET` depends on the meta-package `MuPDF.NativeAssets`, which pulls in every published platform package.

**Version:** set `ArtifexMuPDFVersion` in `Versions.props` only. `pack-nativeassets.ps1` and `dotnet pack` on `MuPDF.NET` read that value and regenerate `runtime.json` automatically.

## Platform packages

| Package | RID | Source folder |
|---------|-----|----------------|
| `MuPDF.NativeAssets.Windows.x86` | `win-x86` | `native/win-x86/` |
| `MuPDF.NativeAssets.Windows.x64` | `win-x64` | `native/win-x64/` |
| `MuPDF.NativeAssets.Windows.arm64` | `win-arm64` | `native/win-arm64/` |
| `MuPDF.NativeAssets.Linux.x64` | `linux-x64` | `native/linux-x64/` |
| `MuPDF.NativeAssets.Linux.arm` | `linux-arm` | `native/linux-arm/` |
| `MuPDF.NativeAssets.Linux.arm64` | `linux-arm64` | `native/linux-arm64/` |
| `MuPDF.NativeAssets.macOS.x64` | `osx-x64` | `native/osx-x64/` |
| `MuPDF.NativeAssets.macOS.arm64` | `osx-arm64` | `native/osx-arm64/` |

Each package ships files under `runtimes/{rid}/native/`.

### Linux source layout (`linux-x64`, `linux-arm`, `linux-arm64`)

Use the **same file set** under each Linux source folder:

| Source folder | Package |
|---------------|---------|
| `native/linux-x64/` | `MuPDF.NativeAssets.Linux.x64` |
| `native/linux-arm/` | `MuPDF.NativeAssets.Linux.arm` |
| `native/linux-arm64/` | `MuPDF.NativeAssets.Linux.arm64` |

Each folder should contain **all** files from the MuPDF build for that architecture:

```text
mupdfcsharp.so
libmupdf.so.28.0
libmupdfcpp.so.28.0
libmupdf.so          (symlink to libmupdf.so.28.0)
libmupdfcpp.so       (symlink to libmupdfcpp.so.28.0)
```

The `.28.0` suffix follows the MuPDF release (1.28.x). `pack-nativeassets.ps1` copies every file into the NuGet package. `MuPDF.NET` copies the host RID's files flat into the app output at build time.

## Meta package

`MuPDF.NativeAssets` has no binaries. It depends on every platform package that was packed, so a plain restore downloads all platform packages. The build copies only the matching runtime into the output (host OS when no RID is set).

## Build packages

```powershell
cd MuPDF
.\pack-nativeassets.ps1
```

Produces `MuPDF/packages/MuPDF.NativeAssets.*.nupkg` and updates `MuPDF.NET/runtime.json`.

## Consumer project

```xml
<ItemGroup>
  <PackageReference Include="MuPDF.NET" Version="3.28.1" />
</ItemGroup>
```

### Runtime Identifier (RID)

To target a specific platform (for example cross-compiling or publishing):

```bash
dotnet publish -r linux-x64
dotnet publish -r linux-arm
dotnet publish -r linux-arm64
dotnet publish -r win-arm64
```

`MuPDF.NET` ships `runtime.json` so restore with an explicit RID resolves the matching `MuPDF.NativeAssets.*` package.

Without `-r`, restore still pulls all platform packages via `MuPDF.NativeAssets`. **Build** selects the host platform (for example `linux-x64` on Ubuntu x64), copies only those natives into the output folder, and removes other RID folders. Use `DisableMuPDFNativeAssetSelection=true` to skip host selection.

## Publishing

Upload **all** packages to your feed:

- `MuPDF.NativeAssets` (meta)
- Every `MuPDF.NativeAssets.{OS}.{arch}` that was packed
- `MuPDF.NET`
