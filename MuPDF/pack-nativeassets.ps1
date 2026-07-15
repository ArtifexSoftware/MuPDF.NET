#Requires -Version 5.1
<#
.SYNOPSIS
  Pack MuPDF.NativeAssets platform packages and the meta package.

.DESCRIPTION
  Reads platforms.json and creates one NuGet package per runtime with binaries under
  runtimes/{rid}/native/. Also packs MuPDF.NativeAssets (meta) that depends on every
  packed platform package, and refreshes MuPDF.NET/runtime.json for RID-specific restore.

.PARAMETER Version
  Package version (default: ArtifexMuPDFVersion from Versions.props).

.PARAMETER OutputDirectory
  Folder for .nupkg files (default: MuPDF/packages).

.PARAMETER IncludeEmpty
  Also pack platform entries that have no binaries yet (placeholder packages).
#>
[CmdletBinding()]
param(
    [string] $Version,
    [string] $OutputDirectory,
    [switch] $IncludeEmpty
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$manifestPath = Join-Path $root 'platforms.json'
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$versionsPropsPath = Join-Path $root '..\Versions.props'

if (-not $Version -and $env:MUPDF_NATIVEASSETS_PACKAGE_VERSION) {
    $Version = $env:MUPDF_NATIVEASSETS_PACKAGE_VERSION
}
if (-not $Version) {
    $versionsProps = [xml](Get-Content $versionsPropsPath)
    foreach ($propertyGroup in $versionsProps.Project.PropertyGroup) {
        if ($propertyGroup.ArtifexMuPDFVersion) {
            $Version = [string]$propertyGroup.ArtifexMuPDFVersion
            break
        }
    }
}
if (-not $Version) {
    throw "ArtifexMuPDFVersion not found in $versionsPropsPath"
}

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $root 'packages'
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$nugetExe = Join-Path $root 'NuGet.exe'
if (-not (Test-Path $nugetExe)) {
    throw "NuGet.exe not found at $nugetExe"
}

$stagingRoot = Join-Path $root '_pack_staging'
if (Test-Path $stagingRoot) {
    Remove-Item $stagingRoot -Recurse -Force
}

function Test-HasBinaries([string] $sourceDir) {
    $full = Join-Path $root $sourceDir
    if (-not (Test-Path $full)) { return $false }
    $files = @(Get-ChildItem -Path $full -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notmatch '^\.' })
    return $files.Count -gt 0
}

function New-PlatformTargetsContent([string] $packageId, [string] $rid) {
    @"
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <!-- .NET Framework: copy runtimes/$rid/native from this package to output. -->
  <ItemGroup Condition="'`$(TargetFrameworkIdentifier)' == '.NETFramework'">
    <None Include="`$(MSBuildThisFileDirectory)..\..\runtimes\$rid\native\*.*"
          CopyToOutputDirectory="PreserveNewest"
          Link="%(Filename)%(Extension)" />
  </ItemGroup>
</Project>
"@
}

$packedPlatforms = New-Object System.Collections.Generic.List[object]

foreach ($platform in $manifest.platforms) {
    $hasBinaries = Test-HasBinaries $platform.sourceDir
    if (-not $hasBinaries -and -not $IncludeEmpty) {
        Write-Host "Skip $($platform.packageId): no binaries in $($platform.sourceDir)"
        continue
    }

    $packageId = $platform.packageId
    $rid = $platform.rid
    $stage = Join-Path $stagingRoot $packageId
    New-Item -ItemType Directory -Force -Path $stage | Out-Null

    $fileLines = New-Object System.Collections.Generic.List[string]

    if ($hasBinaries) {
        $dest = Join-Path $stage "runtimes\$rid\native"
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        # Copy every file from source (e.g. libmupdf.so.28.0, symlinks, mupdfcsharp.so).
        Copy-Item -Path (Join-Path $root "$($platform.sourceDir)\*") -Destination $dest -Force
        $packedFiles = @(Get-ChildItem -Path $dest -File | Select-Object -ExpandProperty Name)
        $fileLines.Add("    <file src=`"runtimes\$rid\native\**\*`" target=`"runtimes\$rid\native\`" />")
        Write-Host "Packed $packageId from $($platform.sourceDir) ($($packedFiles -join ', '))"
    }

    $targetsDir = Join-Path $stage "buildTransitive"
    New-Item -ItemType Directory -Force -Path $targetsDir | Out-Null
    $targetsFileName = "$packageId.targets"
    $targetsPath = Join-Path $targetsDir $targetsFileName
    Set-Content -Path $targetsPath -Value (New-PlatformTargetsContent $packageId $rid) -Encoding UTF8
    $fileLines.Add("    <file src=`"buildTransitive\$targetsFileName`" target=`"buildTransitive\`" />")

    Copy-Item (Join-Path $root 'LICENSE.md') $stage
    $fileLines.Add('    <file src="LICENSE.md" />')

    $filesSection = ($fileLines -join "`n")
    $nuspec = @"
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>$packageId</id>
    <version>$Version</version>
    <authors>Artifex Software Inc.</authors>
    <license type="file">LICENSE.md</license>
    <description>$($platform.description)</description>
    <copyright>Artifex</copyright>
    <tags>C# MuPDF DotNet PDF nativeassets $rid</tags>
    <repository type="git" url="https://github.com/ArtifexSoftware/MuPDF.NET" />
  </metadata>
  <files>
$filesSection
  </files>
</package>
"@

    $nuspecPath = Join-Path $stage "$packageId.nuspec"
    Set-Content -Path $nuspecPath -Value $nuspec -Encoding UTF8

    & $nugetExe pack $nuspecPath -OutputDirectory $OutputDirectory -Version $Version
    if ($LASTEXITCODE -ne 0) { throw "nuget pack failed for $packageId" }

    $packedPlatforms.Add($platform) | Out-Null
}

if ($packedPlatforms.Count -eq 0) {
    throw 'No platform binaries were packed. Place files under MuPDF/native/{folder}/ and retry.'
}

# Meta package: depends on every packed platform package (restore all when no RID).
$metaStage = Join-Path $stagingRoot 'MuPDF.NativeAssets'
New-Item -ItemType Directory -Force -Path $metaStage | Out-Null
Copy-Item (Join-Path $root 'LICENSE.md') $metaStage
Copy-Item (Join-Path $root 'Description.md') $metaStage
Copy-Item (Join-Path $root 'logo.png') $metaStage

$depLines = ($packedPlatforms | ForEach-Object {
    "    <dependency id=`"$($_.packageId)`" version=`"$Version`" />"
}) -join "`n"

$metaNuspec = @"
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>MuPDF.NativeAssets</id>
    <version>$Version</version>
    <authors>Artifex Software Inc.</authors>
    <license type="file">LICENSE.md</license>
    <icon>logo.png</icon>
    <readme>Description.md</readme>
    <description>Meta-package for MuPDF native libraries. Pulls in all platform-specific MuPDF.NativeAssets.* packages. Referenced automatically by MuPDF.NET.</description>
    <copyright>Artifex</copyright>
    <tags>C# MuPDF DotNet PDF nativeassets</tags>
    <repository type="git" url="https://github.com/ArtifexSoftware/MuPDF.NET" />
    <dependencies>
$depLines
    </dependencies>
  </metadata>
  <files>
    <file src="logo.png" />
    <file src="Description.md" />
    <file src="LICENSE.md" />
  </files>
</package>
"@

$metaNuspecPath = Join-Path $metaStage 'MuPDF.NativeAssets.nuspec'
Set-Content -Path $metaNuspecPath -Value $metaNuspec -Encoding UTF8

& $nugetExe pack $metaNuspecPath -OutputDirectory $OutputDirectory -Version $Version
if ($LASTEXITCODE -ne 0) { throw 'nuget pack failed for MuPDF.NativeAssets (meta)' }

Write-Host "Packed MuPDF.NativeAssets (meta) with $($packedPlatforms.Count) platform dependencies"

& (Join-Path $root 'generate-runtimejson.ps1') -Version $Version

Remove-Item $stagingRoot -Recurse -Force
Write-Host "Done. Packages in $OutputDirectory"
