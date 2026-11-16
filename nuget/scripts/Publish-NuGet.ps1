<#
.SYNOPSIS
    Builds and publishes CSLibrary2026 to NuGet.org.

.DESCRIPTION
    Packs the library for every target framework the current host supports and pushes
    the resulting package to NuGet.org.

    The target frameworks are selected through the PACK_TARGET_FRAMEWORKS environment
    variable, because MSBuild's command line treats ';' as a property separator
    ("-p:TargetFrameworks=a;b" fails with MSB1006).

    net10.0-ios can only be built on macOS, so the default set here excludes it.
    Use the CI workflow (.github/workflows/publish-nuget.yml) to build the iOS
    flavour on a macOS runner and merge both packages.

.PARAMETER Version
    The package version, e.g. 2.0.0-beta.3. Defaults to the version in the csproj.

.PARAMETER ApiKey
    The NuGet.org API key. Defaults to the NUGET_API_KEY environment variable, or
    to the contents of nuget/api-key.txt (gitignored).

.PARAMETER OutputPath
    Directory the .nupkg is written to. Defaults to ./nupkg.

.PARAMETER Source
    The NuGet feed. Defaults to https://api.nuget.org/v3/index.json.

.PARAMETER SkipPush
    Only build the package, do not push it.

.EXAMPLE
    pwsh -File Publish-NuGet.ps1 -Version 2.0.0-beta.3 -ApiKey $env:NUGET_API_KEY
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$ApiKey = $env:NUGET_API_KEY,
    [string]$OutputPath,
    [string]$Source = 'https://api.nuget.org/v3/index.json',
    [switch]$SkipPush
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$project = Join-Path $repoRoot 'CSLibrary2026.csproj'

if (-not $OutputPath) {
    $OutputPath = Join-Path $repoRoot 'nupkg'
}

if (-not $ApiKey) {
    $keyFile = Join-Path $repoRoot 'nuget\api-key.txt'
    if (Test-Path -LiteralPath $keyFile) {
        $ApiKey = (Get-Content -LiteralPath $keyFile -Raw).Trim()
    }
}

if (-not $Version) {
    # Read <Version> from the csproj so the caller does not have to repeat it.
    [xml]$csproj = Get-Content $project -Raw
    $Version = ($csproj.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
    if (-not $Version) { throw 'Could not determine the package version - pass -Version explicitly.' }
}

# net10.0-ios can only be built on macOS; see the script help.
$targetFrameworks = @(
    'netstandard2.0'
    'netstandard2.1'
    'net10.0'
    'net10.0-windows10.0.19041.0'
    'net10.0-android'
) -join ';'

$env:PACK_TARGET_FRAMEWORKS = $targetFrameworks

Write-Host "Packing CSLibrary2026 $Version for: $targetFrameworks"

New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

& dotnet pack $project `
    --configuration Release `
    --output $OutputPath `
    -p:Version=$Version `
    -p:ContinuousIntegrationBuild=true `
    --nologo

if ($LASTEXITCODE -ne 0) {
    throw "dotnet pack failed with exit code $LASTEXITCODE"
}

$package = Join-Path $OutputPath "CSLibrary2026.$Version.nupkg"
if (-not (Test-Path $package)) {
    throw "Expected package not found: $package"
}

Write-Host "Built $package"

if ($SkipPush) {
    Write-Host 'Skipping push (-SkipPush).'
    return
}

if (-not $ApiKey) {
    throw 'No NuGet API key. Pass -ApiKey or set the NUGET_API_KEY environment variable.'
}

Write-Host "Pushing $package to $Source"

& dotnet nuget push $package `
    --api-key $ApiKey `
    --source $Source `
    --skip-duplicate `
    --no-symbols

if ($LASTEXITCODE -ne 0) {
    throw "dotnet nuget push failed with exit code $LASTEXITCODE"
}

Write-Host "Published CSLibrary2026 $Version to $Source"
