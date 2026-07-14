#Requires -Version 5.1
<#
.SYNOPSIS
  Generate MuPDF.NET/runtime.json from Versions.props and platforms.json.
#>
[CmdletBinding()]
param(
    [string] $Version,
    [string] $PlatformsPath,
    [string] $VersionsPropsPath,
    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if (-not $PlatformsPath) {
    $PlatformsPath = Join-Path $root 'platforms.json'
}
if (-not $VersionsPropsPath) {
    $VersionsPropsPath = Join-Path $root '..\Versions.props'
}
if (-not $OutputPath) {
    $OutputPath = Join-Path $root '..\MuPDF.NET\runtime.json'
}

if (-not $Version) {
    $versionsProps = [xml](Get-Content $VersionsPropsPath)
    foreach ($propertyGroup in $versionsProps.Project.PropertyGroup) {
        if ($propertyGroup.ArtifexMuPDFVersion) {
            $Version = [string]$propertyGroup.ArtifexMuPDFVersion
            break
        }
    }
}

if (-not $Version) {
    throw "ArtifexMuPDFVersion not found in $VersionsPropsPath"
}

$manifest = Get-Content $PlatformsPath -Raw | ConvertFrom-Json

$runtimeLines = New-Object System.Collections.Generic.List[string]
$runtimeLines.Add('{')
$runtimeLines.Add('  "runtimes": {')
$ridEntries = @()
foreach ($platform in $manifest.platforms) {
    $ridEntries += "    `"$($platform.rid)`": { `"$($platform.packageId)`": `"$Version`" }"
}
$runtimeLines.Add(($ridEntries -join ",`n"))
$runtimeLines.Add('  }')
$runtimeLines.Add('}')

[System.IO.File]::WriteAllLines($OutputPath, $runtimeLines, [System.Text.UTF8Encoding]::new($false))
Write-Host "Generated $OutputPath (version $Version)"
