<#
.SYNOPSIS
    Merges several CSLibrary2026 .nupkg files into a single package.

.DESCRIPTION
    The NuGet package is built from two CI jobs:

      - the Linux job produces every target framework except net10.0-ios
      - the macOS job produces net10.0-ios (Apple targets only build on macOS)

    NuGet has no built-in "merge packages" command, so this script merges them:
    the lib/<tfm> folders of every input are copied into one staging folder and the
    nuspec dependency groups are unioned (one group per target framework).

    All inputs must have been packed from the same commit with the same version.

.PARAMETER PackagePath
    One or more .nupkg files to merge.

.PARAMETER OutputPath
    The .nupkg file to write.

.EXAMPLE
    pwsh -File Merge-NuGetPackages.ps1 `
        -PackagePath ./out/linux/CSLibrary2026.2.0.0-beta.3.nupkg,./out/macos/CSLibrary2026.2.0.0-beta.3.nupkg `
        -OutputPath ./out/CSLibrary2026.2.0.0-beta.3.nupkg
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$PackagePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$WorkDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) ("cslibrary2026-merge-" + [guid]::NewGuid().ToString('N')))
)

$ErrorActionPreference = 'Stop'

# System.IO.Compression.FileSystem is a separate assembly on .NET Framework, but it is
# part of the shared framework on .NET (PowerShell 7) - the Add-Type would fail there.
if (-not ([System.Management.Automation.PSTypeName]'System.IO.Compression.ZipFile').Type) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
}

function Expand-Package {
    param([string]$Package, [string]$Destination, [string]$FolderName)

    if (-not (Test-Path $Package)) {
        throw "Package not found: $Package"
    }

    $zipPath = [System.IO.Path]::ChangeExtension($Package, '.zip')
    Copy-Item $Package $zipPath -Force

    $extractDir = Join-Path $Destination $FolderName
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $extractDir)
    Remove-Item $zipPath -Force

    return $extractDir
}

function Get-NuspecPath {
    param([string]$ExtractedDir)

    $nuspec = Get-ChildItem -Path $ExtractedDir -Filter '*.nuspec' -File | Select-Object -First 1
    if (-not $nuspec) {
        throw "No .nuspec found in $ExtractedDir"
    }
    return $nuspec.FullName
}

function Get-NuspecNamespaceManager {
    param([xml]$Nuspec)

    $nsManager = [System.Xml.XmlNamespaceManager]::new($Nuspec.NameTable)
    $nsManager.AddNamespace('nuspec', $Nuspec.DocumentElement.NamespaceURI)

    # XmlNamespaceManager implements IEnumerable, so it must be returned with the
    # unary comma - otherwise PowerShell unrolls it into the prefix strings.
    return ,$nsManager
}

function Get-DependencyGroups {
    param([xml]$Nuspec)

    # The nuspec uses a default namespace, which the PowerShell XML adapter does not
    # resolve - an explicit namespace manager is required.
    $nsManager = Get-NuspecNamespaceManager -Nuspec $Nuspec
    $nodes = $Nuspec.SelectNodes('/nuspec:package/nuspec:metadata/nuspec:dependencies/nuspec:group', $nsManager)
    return @($nodes)
}

function Copy-PackageContent {
    param([string]$Source, [string]$Destination)

    # Get-ChildItem is used instead of a wildcard path because "[Content_Types].xml"
    # contains characters that PowerShell interprets as a wildcard.
    foreach ($item in (Get-ChildItem -LiteralPath $Source -Force)) {
        Copy-Item -LiteralPath $item.FullName -Destination $Destination -Recurse -Force
    }
}

function Copy-FolderInto {
    param([string]$SourceFolder, [string]$Destination)

    # Copies the folder itself (e.g. "lib") into the destination, so that the result
    # is <destination>/lib/<tfm>/... - not <destination>/<tfm>/...
    Copy-Item -LiteralPath $SourceFolder -Destination $Destination -Recurse -Force
}

$resolvedPackages = @()
foreach ($p in $PackagePath) {
    # A comma separated list is accepted as well, so that the script can be called
    # with "-File" from a shell that does not expand arrays.
    foreach ($single in ($p -split ',')) {
        if ([string]::IsNullOrWhiteSpace($single)) { continue }
        $resolvedPackages += (Resolve-Path $single.Trim()).Path
    }
}

if ($resolvedPackages.Count -lt 2) {
    throw "At least two packages are required to merge (got $($resolvedPackages.Count))."
}

Write-Host "Merging $($resolvedPackages.Count) packages into $OutputPath"

New-Item -ItemType Directory -Force -Path $WorkDirectory | Out-Null
try {
    $staging = Join-Path $WorkDirectory 'package'
    New-Item -ItemType Directory -Force -Path $staging | Out-Null

    # The nuspec of the first package is the base; its dependency groups are extended
    # with the groups of the remaining packages.
    $baseNuspec = $null
    $seenFrameworks = @{}

    foreach ($package in $resolvedPackages) {
        # The packages are usually built from different jobs and therefore share the
        # same file name - extract them into separate folders.
        $extractDir = Expand-Package -Package $package -Destination $WorkDirectory -FolderName ("package-" + [guid]::NewGuid().ToString('N'))
        $nuspecPath = Get-NuspecPath -ExtractedDir $extractDir

        if (-not $baseNuspec) {
            $baseNuspec = $nuspecPath
            # Copy the whole base package (metadata, README, [Content_Types].xml, ...).
            Copy-PackageContent -Source $extractDir -Destination $staging

            # The groups the base package already declares must not be added again.
            [xml]$baseXml = Get-Content $baseNuspec -Raw
            foreach ($group in (Get-DependencyGroups -Nuspec $baseXml)) {
                $seenFrameworks[$group.GetAttribute('targetFramework')] = $true
            }
        }
        else {
            # Merge the dependency groups of this package into the base nuspec.
            [xml]$baseXml = Get-Content $baseNuspec -Raw
            [xml]$otherXml = Get-Content $nuspecPath -Raw

            $ns = $baseXml.DocumentElement.NamespaceURI
            $nsManager = Get-NuspecNamespaceManager -Nuspec $baseXml
            $dependencies = $baseXml.SelectSingleNode('/nuspec:package/nuspec:metadata/nuspec:dependencies', $nsManager)

            foreach ($group in (Get-DependencyGroups -Nuspec $otherXml)) {
                $tfm = $group.GetAttribute('targetFramework')
                if ($seenFrameworks.ContainsKey($tfm)) {
                    Write-Host "  dependency group '$tfm' already present - skipped"
                    continue
                }
                $seenFrameworks[$tfm] = $true

                $newGroup = $baseXml.CreateElement('group', $ns)
                $newGroup.SetAttribute('targetFramework', $tfm)
                foreach ($dep in $group.ChildNodes) {
                    if ($dep.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }
                    $newDep = $baseXml.CreateElement($dep.LocalName, $ns)
                    foreach ($attr in $dep.Attributes) {
                        $newDep.SetAttribute($attr.LocalName, $attr.Value)
                    }
                    $newGroup.AppendChild($newDep) | Out-Null
                }
                $dependencies.AppendChild($newGroup) | Out-Null
            }

            $baseXml.Save($baseNuspec)
        }

        # Copy the lib folders of this package into the staging area.
        $libDir = Join-Path $extractDir 'lib'
        if (Test-Path -LiteralPath $libDir) {
            Copy-FolderInto -SourceFolder $libDir -Destination $staging
        }
    }

    # The merged nuspec lives in the first package's extraction folder - copy the
    # updated version over the one in the staging area.
    $stagedNuspec = Get-NuspecPath -ExtractedDir $staging
    Copy-Item -LiteralPath $baseNuspec -Destination $stagedNuspec -Force

    $outputDir = Split-Path -Parent $OutputPath
    if ($outputDir -and -not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
    }
    if (Test-Path $OutputPath) {
        Remove-Item $OutputPath -Force
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory($staging, $OutputPath)
    Write-Host "Wrote $OutputPath"
}
finally {
    if (Test-Path $WorkDirectory) {
        Remove-Item $WorkDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
}
