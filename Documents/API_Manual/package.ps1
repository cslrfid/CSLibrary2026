<#
.SYNOPSIS
    Packages the API Reference Manual for distribution.

.DESCRIPTION
    Creates a zip archive of the manual that can be handed over to a
    colleague, who opens index.html in a browser. The manual works without a
    web server and without any installation.

    The tools folder is left out: it holds the generator of the API reference
    pages and is only needed by a developer of the library.

.PARAMETER OutputPath
    The path of the zip archive. Defaults to API_Manual.zip next to the manual.

.EXAMPLE
    pwsh -File Documents/API_Manual/package.ps1

.EXAMPLE
    pwsh -File Documents/API_Manual/package.ps1 -OutputPath C:\Temp\CSLibrary2026-API.zip
#>
[CmdletBinding()]
param(
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$manualRoot = $PSScriptRoot

if (-not $OutputPath) {
    $OutputPath = Join-Path $manualRoot 'API_Manual.zip'
}

$staging = Join-Path ([System.IO.Path]::GetTempPath()) ('csl-api-manual-' + [Guid]::NewGuid().ToString('N'))

try {
    New-Item -ItemType Directory -Force -Path $staging | Out-Null

    # Everything except the generator and the build output.
    Get-ChildItem $manualRoot -Force | Where-Object {
        $_.Name -notin @('tools', 'API_Manual.zip')
    } | ForEach-Object {
        Copy-Item $_.FullName -Destination $staging -Recurse -Force
    }

    if (Test-Path $OutputPath) {
        Remove-Item $OutputPath -Force
    }

    Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $OutputPath -Force

    $size = [math]::Round((Get-Item $OutputPath).Length / 1kb, 1)
    Write-Host ("Created {0} ({1} KB)" -f $OutputPath, $size) -ForegroundColor Green
    Write-Host 'The recipient opens index.html in a browser; no web server is needed.'
}
finally {
    Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
}
