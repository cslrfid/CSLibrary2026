<#
.SYNOPSIS
    Builds the API Reference Manual of CSLibrary2026.

.DESCRIPTION
    The manual has two kinds of content:

    * The API reference pages (Documents/API_Manual/api) are generated from the
      compiled library and its XML documentation file by the CslApiDump tool.
    * The guides (Documents/API_Manual/guides) and the appendix are written by
      hand and are not touched by the build.

    The script renders the API pages, refreshes the navigation and rebuilds the
    search index. The result can be opened directly from the file system: no web
    server is needed.

.PARAMETER Configuration
    The build configuration of the library. Defaults to Release.

.PARAMETER Serve
    Starts a small web server on the given port and opens the manual in the
    browser. Useful to check the manual on a phone or on another machine.

.EXAMPLE
    pwsh -File Documents/API_Manual/build-docs.ps1

.EXAMPLE
    pwsh -File Documents/API_Manual/build-docs.ps1 -Serve 8080
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [int]$Serve = 0
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$manualRoot = $PSScriptRoot
$toolProject = Join-Path $manualRoot 'tools\CslApiDump\CslApiDump.csproj'
$tool = Join-Path $manualRoot ('tools\CslApiDump\bin\{0}\net10.0\CslApiDump.exe' -f $Configuration)
$layout = Join-Path $manualRoot 'api-layout.json'

Push-Location $repoRoot
try {
    Write-Host 'Building the library with the XML documentation ...' -ForegroundColor Cyan
    & dotnet build 'CSLibrary2026.csproj' `
        -c $Configuration `
        '-p:TargetFrameworks=net10.0' `
        '-p:GenerateDocumentationFile=true' `
        '-p:NoWarn=CS1591' `
        -v q --nologo
    if ($LASTEXITCODE -ne 0) {
        throw 'The library build failed.'
    }

    Write-Host 'Building the API reference generator ...' -ForegroundColor Cyan
    & dotnet build $toolProject -c $Configuration -v q --nologo
    if ($LASTEXITCODE -ne 0) {
        throw 'The build of CslApiDump failed.'
    }

    Write-Host 'Rendering the API reference pages ...' -ForegroundColor Cyan
    & $tool --root $manualRoot --layout $layout
    if ($LASTEXITCODE -ne 0) {
        throw 'The rendering of the API reference failed.'
    }

    Write-Host 'Rebuilding the search index ...' -ForegroundColor Cyan
    & $tool --root $manualRoot --index
    if ($LASTEXITCODE -ne 0) {
        throw 'The build of the search index failed.'
    }

    Write-Host 'Checking the links of the manual ...' -ForegroundColor Cyan
    & $tool --root $manualRoot --check-links
    if ($LASTEXITCODE -ne 0) {
        throw 'The manual holds broken links.'
    }

    Write-Host ''
    Write-Host 'The manual is ready:' -ForegroundColor Green
    Write-Host ("  {0}" -f (Join-Path $manualRoot 'index.html'))
    Write-Host ("  {0}" -f (Join-Path $manualRoot 'search.html'))

    if ($Serve -gt 0) {
        Write-Host ''
        Write-Host ("Serving the manual on http://localhost:{0}/ ..." -f $Serve) -ForegroundColor Green
        & python -m http.server $Serve --directory $manualRoot
    }
}
finally {
    Pop-Location
}
